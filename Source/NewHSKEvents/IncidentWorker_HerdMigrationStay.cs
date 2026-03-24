using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace NewHSKEvents
{
    public class IncidentWorker_HerdMigrationStay : IncidentWorker
    {
        private static readonly IntRange AnimalsCount = new IntRange(3, 5);
        private const float MinTotalBodySize = 4f;
        private const int StayDurationMin = 100000; // ~1.7 days
        private const int StayDurationMax = 140000; // ~2.3 days

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!TryFindAnimalKind(map, out _))
            {
                return false;
            }
            IntVec3 entry;
            return RCellFinder.TryFindRandomPawnEntryCell(out entry, map, CellFinder.EdgeRoadChance_Animal);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!TryFindAnimalKind(map, out PawnKindDef animalKind))
            {
                return false;
            }
            if (!RCellFinder.TryFindRandomPawnEntryCell(out IntVec3 spawnCenter, map, CellFinder.EdgeRoadChance_Animal))
            {
                return false;
            }

            Rot4 rot = Rot4.FromAngleFlat((map.Center - spawnCenter).AngleFlat);
            List<Pawn> animals = GenerateAnimals(animalKind, map.Tile);

            // Find a spot near the center of the map for animals to wander around
            IntVec3 chillSpot = map.Center;
            RCellFinder.TryFindRandomSpotJustOutsideColony(map.Center, map, out chillSpot);

            for (int i = 0; i < animals.Count; i++)
            {
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(spawnCenter, map, 10);
                GenSpawn.Spawn(animals[i], loc, map, rot);
            }

            int stayDuration = Rand.Range(StayDurationMin, StayDurationMax);
            LordJob_AnimalVisit lordJob = new LordJob_AnimalVisit(chillSpot, stayDuration);
            LordMaker.MakeNewLord(null, lordJob, map, animals);

            SendStandardLetter(
                "NewHSKEvents_HerdStay_LetterLabel".Translate(animalKind.GetLabelPlural().CapitalizeFirst()),
                "NewHSKEvents_HerdStay_LetterText".Translate(animalKind.GetLabelPlural()),
                def.letterDef ?? LetterDefOf.PositiveEvent,
                parms,
                animals[0]);

            return true;
        }

        private bool TryFindAnimalKind(Map map, out PawnKindDef animalKind)
        {
            return (from k in map.Biome.AllWildAnimals
                    where k.RaceProps.CanDoHerdMigration
                          && map.mapTemperature.SeasonAndOutdoorTemperatureAcceptableFor(k.race)
                    select k)
                .TryRandomElementByWeight(
                    x => Mathf.Lerp(0.2f, 1f, x.race.GetStatValueAbstract(StatDefOf.Wildness)),
                    out animalKind);
        }

        private List<Pawn> GenerateAnimals(PawnKindDef animalKind, int tile)
        {
            int count = AnimalsCount.RandomInRange;
            count = Mathf.Max(count, Mathf.CeilToInt(MinTotalBodySize / animalKind.RaceProps.baseBodySize));
            List<Pawn> list = new List<Pawn>();
            for (int i = 0; i < count; i++)
            {
                Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    animalKind, null, PawnGenerationContext.NonPlayer, tile));
                list.Add(pawn);
            }
            return list;
        }
    }
}
