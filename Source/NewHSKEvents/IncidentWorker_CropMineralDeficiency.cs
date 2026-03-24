using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace NewHSKEvents
{
    /// <summary>
    /// Few tame animals (&lt;5): crops die from mineral deficiency.
    /// Many tame animals (&gt;5): crops get a 30% growth boost from natural fertilization.
    /// </summary>
    public class IncidentWorker_CropMineralDeficiency : IncidentWorker
    {
        private const int AnimalThreshold = 5;
        private const float CropKillPercent = 0.3f;
        private const float GrowthBoost = 0.3f;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            return HasCrops(map);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            int tameAnimals = CountTameAnimals(map);

            if (tameAnimals > AnimalThreshold)
            {
                return TryBoostCrops(map, tameAnimals, parms);
            }
            else
            {
                return TryKillCrops(map, tameAnimals, parms);
            }
        }

        private bool TryBoostCrops(Map map, int tameAnimals, IncidentParms parms)
        {
            List<Plant> crops = GetCrops(map)
                .Where(p => p.Growth < 0.9f)
                .ToList();

            if (crops.Count == 0)
            {
                return false;
            }

            int boosted = 0;
            foreach (Plant crop in crops)
            {
                crop.Growth += GrowthBoost;
                boosted++;
            }

            SendStandardLetter(
                "HSK_CropBoostLabel".Translate(),
                "HSK_CropBoostDesc".Translate(
                    boosted.ToString(),
                    tameAnimals.ToString()),
                LetterDefOf.PositiveEvent,
                parms,
                crops.FirstOrDefault());

            return true;
        }

        private bool TryKillCrops(Map map, int tameAnimals, IncidentParms parms)
        {
            List<Plant> crops = GetCrops(map)
                .Where(p => p.Growth > 0.2f)
                .ToList();

            if (crops.Count == 0)
            {
                return false;
            }

            float killRatio = CropKillPercent;
            if (tameAnimals == 0)
            {
                killRatio = 0.5f;
            }
            else if (tameAnimals <= 2)
            {
                killRatio = 0.4f;
            }

            int toKill = GenMath.RoundRandom(crops.Count * killRatio);
            if (toKill == 0)
            {
                toKill = 1;
            }

            crops.Shuffle();
            int killed = 0;
            for (int i = 0; i < toKill && i < crops.Count; i++)
            {
                crops[i].Kill();
                killed++;
            }

            SendStandardLetter(
                "HSK_CropDeficiencyLabel".Translate(),
                "HSK_CropDeficiencyDesc".Translate(
                    killed.ToString(),
                    tameAnimals.ToString()),
                LetterDefOf.NegativeEvent,
                parms,
                crops.FirstOrDefault());

            return true;
        }

        private int CountTameAnimals(Map map)
        {
            return map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer)
                .Count(p => p.RaceProps.Animal && !p.Destroyed);
        }

        private IEnumerable<Plant> GetCrops(Map map)
        {
            return map.listerThings.ThingsInGroup(ThingRequestGroup.Plant)
                .OfType<Plant>()
                .Where(p => !p.Destroyed && p.IsCrop);
        }

        private bool HasCrops(Map map)
        {
            return GetCrops(map).Any();
        }
    }
}
