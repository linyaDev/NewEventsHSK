using System.Collections.Generic;
using Verse;

namespace NewHSKEvents
{
    /// <summary>
    /// Periodically calls StartFlying() on registered birds every 4 seconds (240 ticks),
    /// up to 4 times total per bird, to keep them airborne during assault.
    /// </summary>
    public class MapComponent_BirdFlightKeeper : MapComponent
    {
        private const int IntervalTicks = 240; // 4 seconds
        private const int MaxBoosts = 4;

        private List<Pawn> trackedBirds = new List<Pawn>();
        private Dictionary<Pawn, int> boostCounts = new Dictionary<Pawn, int>();
        private int tickCounter;

        public MapComponent_BirdFlightKeeper(Map map) : base(map)
        {
        }

        public void RegisterBirds(List<Pawn> birds)
        {
            foreach (Pawn bird in birds)
            {
                if (!trackedBirds.Contains(bird))
                {
                    trackedBirds.Add(bird);
                    boostCounts[bird] = 0;
                }
            }
        }

        public override void MapComponentTick()
        {
            if (trackedBirds.Count == 0)
                return;

            tickCounter++;
            if (tickCounter < IntervalTicks)
                return;

            tickCounter = 0;

            for (int i = trackedBirds.Count - 1; i >= 0; i--)
            {
                Pawn bird = trackedBirds[i];

                // Remove dead/despawned birds or those that got all boosts
                if (bird == null || bird.Dead || bird.Destroyed || !bird.Spawned
                    || boostCounts[bird] >= MaxBoosts)
                {
                    trackedBirds.RemoveAt(i);
                    if (bird != null) boostCounts.Remove(bird);
                    continue;
                }

#if !V15
                bird.flight?.StartFlying();
#endif
                boostCounts[bird]++;
            }
        }
    }
}
