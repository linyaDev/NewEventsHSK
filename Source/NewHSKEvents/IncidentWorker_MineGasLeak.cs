using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace NewHSKEvents
{
    public class IncidentWorker_MineGasLeak : IncidentWorker
    {
        private const int MinRoomSize = 30;
        private const int MinRooms = 2;
        private const int MaxRooms = 3;
        private const int GasDensity = 150;

        private static readonly GasType[] GasTypes = { GasType.ToxGas, GasType.RotStink };

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            return FindRoomsUnderMountain(map).Count >= MinRooms;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            var rooms = FindRoomsUnderMountain(map);
            if (rooms.Count < MinRooms)
                return false;

            rooms.Shuffle();
            int count = Rand.RangeInclusive(MinRooms, System.Math.Min(MaxRooms, rooms.Count));
            GasType gasType = GasTypes.RandomElement();

            IntVec3 firstCell = IntVec3.Invalid;

            for (int i = 0; i < count; i++)
            {
                var room = rooms[i];
                foreach (IntVec3 cell in room.Cells)
                {
                    if (cell.GetRoof(map)?.isThickRoof == true)
                    {
                        GasUtility.AddGas(cell, map, gasType, GasDensity);
                        if (!firstCell.IsValid)
                            firstCell = cell;
                    }
                }
            }

            if (!firstCell.IsValid)
                return false;

            SendStandardLetter(
                "NewHSKEvents_MineGasLeak_LetterLabel".Translate(),
                "NewHSKEvents_MineGasLeak_LetterText".Translate(count),
                def.letterDef ?? LetterDefOf.ThreatSmall,
                parms,
                new TargetInfo(firstCell, map));

            return true;
        }

        private List<Room> FindRoomsUnderMountain(Map map)
        {
            var result = new List<Room>();

            foreach (Room room in map.regionGrid.allRooms)
            {
                if (room.TouchesMapEdge || room.UsesOutdoorTemperature)
                    continue;
                if (room.CellCount < MinRoomSize)
                    continue;

                bool hasThickRoof = false;
                foreach (IntVec3 cell in room.Cells)
                {
                    if (cell.GetRoof(map)?.isThickRoof == true)
                    {
                        hasThickRoof = true;
                        break;
                    }
                }

                if (hasThickRoof)
                    result.Add(room);
            }

            return result;
        }
    }
}
