using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace NewHSKEvents
{
    /// <summary>
    /// When there are more than 10 tame small animals, one randomly snaps
    /// and attacks the nearest animal.
    /// </summary>
    public class IncidentWorker_AnimalSnap : IncidentWorker
    {
        private const int MinTameAnimals = 10;
        private const float MaxBodySize = 0.5f;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            return GetSmallTameAnimals(map).Count() > MinTameAnimals;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            List<Pawn> smallAnimals = GetSmallTameAnimals(map).ToList();
            if (smallAnimals.Count <= MinTameAnimals)
            {
                return false;
            }

            // Pick a random small tame animal to snap
            Pawn aggressor = smallAnimals.RandomElement();

            // Find nearest other animal (tame or wild) to attack
            Pawn target = FindNearestAnimal(aggressor, map);
            if (target == null)
            {
                return false;
            }

            // Start manhunter mental state
            aggressor.mindState.mentalStateHandler.TryStartMentalState(
                MentalStateDefOf.Manhunter, forceWake: true);

            SendStandardLetter(
                "HSK_AnimalSnapLabel".Translate(aggressor.LabelShort),
                "HSK_AnimalSnapDesc".Translate(
                    aggressor.LabelShort,
                    aggressor.def.label),
                LetterDefOf.ThreatSmall,
                parms,
                aggressor);

            return true;
        }

        private IEnumerable<Pawn> GetSmallTameAnimals(Map map)
        {
            return map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer)
                .Where(p => p.RaceProps.Animal
                    && !p.Dead
                    && !p.Downed
                    && p.RaceProps.baseBodySize <= MaxBodySize);
        }

        private Pawn FindNearestAnimal(Pawn aggressor, Map map)
        {
            return map.mapPawns.AllPawnsSpawned
                .Where(p => p != aggressor
                    && p.RaceProps.Animal
                    && !p.Dead
                    && !p.Downed
                    && p.Position.DistanceTo(aggressor.Position) < 30f)
                .OrderBy(p => p.Position.DistanceTo(aggressor.Position))
                .FirstOrDefault();
        }
    }
}
