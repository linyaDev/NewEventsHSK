using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace NewHSKEvents
{
    /// <summary>
    /// Someone left a piece of glass near straw matting.
    /// The sun's reflection ignites it.
    /// </summary>
    public class IncidentWorker_StrawFire : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            return TryFindStrawCell(map, out _);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            if (!TryFindStrawCell(map, out IntVec3 fireCell))
                return false;

            FireUtility.TryStartFireIn(fireCell, map, 1.0f, null);

            SendStandardLetter(
                "NewHSKEvents_StrawFire_LetterLabel".Translate(),
                "NewHSKEvents_StrawFire_LetterText".Translate(),
                def.letterDef ?? LetterDefOf.NegativeEvent,
                parms,
                new TargetInfo(fireCell, map));

            return true;
        }

        private bool TryFindStrawCell(Map map, out IntVec3 cell)
        {
            List<IntVec3> strawCells = map.AllCells
                .Where(c => c.GetTerrain(map).defName == "StrawMatting"
                            && !c.Fogged(map))
                .ToList();

            if (strawCells.Any())
                return strawCells.TryRandomElement(out cell);

            cell = IntVec3.Invalid;
            return false;
        }
    }
}
