using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace NewHSKEvents
{
    /// <summary>
    /// If tame animals are kept in a closed, roofed room, they all suddenly go manhunter.
    /// Cabin fever for animals.
    /// </summary>
    public class IncidentWorker_CabinFeverAnimals : IncidentWorker
    {
        private const int MinAnimalsInRoom = 3;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            return FindEnclosedAnimals(map) != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            var group = FindEnclosedAnimals(map);
            if (group == null || group.Count < MinAnimalsInRoom)
            {
                return false;
            }

            int count = 0;
            foreach (Pawn animal in group)
            {
                if (animal.mindState.mentalStateHandler.TryStartMentalState(
                    MentalStateDefOf.Manhunter, forceWake: true))
                {
                    count++;
                }
            }

            if (count == 0)
            {
                return false;
            }

            Pawn firstAnimal = group.First();

            SendStandardLetter(
                "HSK_CabinFeverLabel".Translate(),
                "HSK_CabinFeverDesc".Translate(count.ToString()),
                LetterDefOf.ThreatSmall,
                parms,
                firstAnimal);

            return true;
        }

        /// <summary>
        /// Find a group of tame animals in a closed roofed room.
        /// Returns the largest group, or null if none qualify.
        /// </summary>
        private List<Pawn> FindEnclosedAnimals(Map map)
        {
            var tameAnimals = map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer)
                .Where(p => p.RaceProps.Animal && !p.Dead && !p.Downed)
                .ToList();

            if (tameAnimals.Count == 0)
            {
                return null;
            }

            // Group animals by room
            var roomGroups = new Dictionary<Room, List<Pawn>>();

            foreach (Pawn animal in tameAnimals)
            {
                Room room = animal.GetRoom();
                if (room == null || room.TouchesMapEdge || room.PsychologicallyOutdoors)
                {
                    continue;
                }

                // Must be a proper enclosed room
                if (!room.ProperRoom)
                {
                    continue;
                }

                if (!roomGroups.ContainsKey(room))
                {
                    roomGroups[room] = new List<Pawn>();
                }
                roomGroups[room].Add(animal);
            }

            // Find the largest group with enough animals
            List<Pawn> bestGroup = null;
            foreach (var kvp in roomGroups)
            {
                if (kvp.Value.Count >= MinAnimalsInRoom)
                {
                    if (bestGroup == null || kvp.Value.Count > bestGroup.Count)
                    {
                        bestGroup = kvp.Value;
                    }
                }
            }

            return bestGroup;
        }
    }
}
