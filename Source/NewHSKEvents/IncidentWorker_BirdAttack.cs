using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace NewHSKEvents
{
    /// <summary>
    /// Aggressive birds attack from 3 sides of the map.
    /// Uses native 1.6 Pawn_FlightTracker for flight animations.
    /// Birds are assigned to a hostile faction and use AssaultColony lord job.
    /// </summary>
    public class IncidentWorker_BirdAttack : IncidentWorker
    {
        private const int MinBirdsPerGroup = 3;
        private const int MaxBirdsPerGroup = 8;
        private const int NumGroups = 3;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            return TryFindBirdKind(map, out _) && FindHostileFaction() != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            if (!TryFindBirdKind(map, out PawnKindDef birdKind))
                return false;

            Faction faction = FindHostileFaction();
            if (faction == null)
                return false;

            // Scale bird count with threat points
            int birdsPerGroup;
            if (parms.points > 0f)
            {
                float combatPower = Mathf.Max(birdKind.combatPower, 10f);
                birdsPerGroup = Mathf.Clamp(
                    Mathf.RoundToInt(parms.points / (NumGroups * combatPower)),
                    MinBirdsPerGroup, MaxBirdsPerGroup);
            }
            else
            {
                birdsPerGroup = Rand.RangeInclusive(MinBirdsPerGroup, MaxBirdsPerGroup);
            }

            // Pick 3 different map edges
            List<Rot4> edges = FindSpawnEdges(map, NumGroups);
            if (edges.Count < NumGroups)
                return false;

            List<Pawn> allBirds = new List<Pawn>();

            for (int g = 0; g < NumGroups; g++)
            {
                IntVec3 spawnCenter = GetEdgeSpawnCell(map, edges[g]);
                Rot4 rot = Rot4.FromAngleFlat((map.Center - spawnCenter).AngleFlat);

                List<Pawn> groupBirds = new List<Pawn>();
                for (int i = 0; i < birdsPerGroup; i++)
                {
                    Pawn bird = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                        birdKind, faction, PawnGenerationContext.NonPlayer, map.Tile,
                        mustBeCapableOfViolence: true));

                    IntVec3 loc = CellFinder.RandomClosewalkCellNear(spawnCenter, map, 8);
                    GenSpawn.Spawn(bird, loc, map, rot);

                    // Force native 1.6 flight animation
                    bird.flight?.StartFlying();

                    groupBirds.Add(bird);
                }

                // Each group assaults the colony
                LordJob lordJob = new LordJob_AssaultColony(faction,
                    canKidnap: false, canTimeoutOrFlee: false,
                    sappers: false, useAvoidGridSmart: false, canSteal: false);
                LordMaker.MakeNewLord(faction, lordJob, map, groupBirds);

                allBirds.AddRange(groupBirds);
            }

            // Register birds for periodic flight boosts (4x every 4 sec)
            MapComponent_BirdFlightKeeper flightKeeper =
                map.GetComponent<MapComponent_BirdFlightKeeper>();
            flightKeeper?.RegisterBirds(allBirds);

            SendStandardLetter(
                "NewHSKEvents_BirdAttack_LetterLabel".Translate(birdKind.GetLabelPlural().CapitalizeFirst()),
                "NewHSKEvents_BirdAttack_LetterText".Translate(
                    faction.Name, birdKind.GetLabelPlural()),
                LetterDefOf.ThreatBig,
                parms,
                allBirds[0]);

            Find.TickManager.slower.SignalForceNormalSpeedShort();

            return true;
        }

        /// <summary>
        /// Find a bird PawnKindDef that has flying animations (native 1.6 flight).
        /// </summary>
        private bool TryFindBirdKind(Map map, out PawnKindDef birdKind)
        {
            return (from k in DefDatabase<PawnKindDef>.AllDefsListForReading
                    where !k.flyingAnimationFramePathPrefix.NullOrEmpty()
                          && k.RaceProps.Animal
                          && k.combatPower > 0
                          && map.mapTemperature.SeasonAndOutdoorTemperatureAcceptableFor(k.race)
                    select k)
                .TryRandomElement(out birdKind);
        }

        private Faction FindHostileFaction()
        {
            return Find.FactionManager.AllFactionsVisible
                .Where(f => f.HostileTo(Faction.OfPlayer) && !f.defeated && !f.temporary)
                .RandomElementWithFallback(null);
        }

        private List<Rot4> FindSpawnEdges(Map map, int count)
        {
            List<Rot4> allEdges = new List<Rot4> { Rot4.North, Rot4.East, Rot4.South, Rot4.West };
            allEdges.Shuffle();

            List<Rot4> result = new List<Rot4>();
            foreach (Rot4 edge in allEdges)
            {
                if (CellFinder.TryFindRandomEdgeCellWith(
                    c => c.Standable(map) && map.reachability.CanReachColony(c),
                    map, edge, CellFinder.EdgeRoadChance_Animal, out _))
                {
                    result.Add(edge);
                    if (result.Count >= count) break;
                }
            }
            return result;
        }

        private IntVec3 GetEdgeSpawnCell(Map map, Rot4 edge)
        {
            CellFinder.TryFindRandomEdgeCellWith(
                c => c.Standable(map) && map.reachability.CanReachColony(c),
                map, edge, CellFinder.EdgeRoadChance_Animal, out IntVec3 cell);
            return cell;
        }
    }
}
