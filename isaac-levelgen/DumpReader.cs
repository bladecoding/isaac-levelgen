using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace isaac_levelgen
{
	public class DumpReader
	{
		Stream stream;
		RoomsProvider RoomsProv;
		Dictionary<int, Dictionary<Tuple<RoomType, int>, RoomDescriptor>> StageRooms;
		public DumpReader(Stream s, RoomsProvider roomsProv) {
			stream = s;
			RoomsProv = roomsProv;
			StageRooms = RoomsProv.StageRooms.ToDictionary(kv => kv.Key, kv => kv.Value.ToDictionary(rd => Tuple.Create(rd.RoomType, rd.RoomId)));
		}

		public DumpStageBlock Read() {
			if (stream.Position >= stream.Length)
				return null;
			var stages = new DumpStage[14];
			var seed = stream.ReadUInt32();
			for (var i = 0; i < 14; i++) {
				var stageId = stream.ReadInt8();
				var stageType = stream.ReadInt8();
				var roomCount = stream.ReadInt8();
				var gridCount = stream.ReadInt8();
				var grid = new DumpRoom[13][];
				for (var j = 0; j < grid.Length; j++)
					grid[j] = new DumpRoom[13];

				var seedStr = Rng.SeedToString(seed);

				var rooms = new DumpRoom[roomCount];
				for (var j = 0; j < gridCount; j++) {
					var sRoom = ReadRoom(stream);
					var room = rooms[sRoom.RoomOffset] ?? sRoom; //Have all of the grid parts of a room point to the same reference.

					grid[sRoom.RoomY][sRoom.RoomX] = room;
					rooms[room.RoomOffset] = room;
				}

				var iAmEror = ReadNonGridRoom(stream);
				var crawlSpace = ReadNonGridRoom(stream);
				var blackMarket = ReadNonGridRoom(stream);
				DumpRoom bossRush = null;
				if (stageId == 6)
					bossRush = ReadNonGridRoom(stream);

				stages[i] = new DumpStage(stageId, stageType, grid, rooms);
			}
			return new DumpStageBlock { Seed = seed, Stages = stages };
		}

		public DumpRoom ReadRoom(Stream s) {
			var r = new DumpRoom {
				RoomOffset = s.ReadInt8(),
				RoomX = s.ReadInt8(),
				RoomY = s.ReadInt8(),
				StageIndex = s.ReadInt8(),
				RoomType = (RoomType)s.ReadInt8(),
				RoomSubType = s.ReadInt8(),
				RoomId = s.ReadInt16(),
			};
			r.Shape = StageRooms[r.StageIndex][Tuple.Create(r.RoomType, r.RoomId)].RoomShape;
			return r;
		}
		public DumpRoom ReadNonGridRoom(Stream s) {
			var r = new DumpRoom {
				RoomOffset = -1,
				RoomX = -1,
				RoomY = -1,
				StageIndex = s.ReadInt8(),
				RoomType = (RoomType)s.ReadInt8(),
				RoomSubType = s.ReadInt8(),
				RoomId = s.ReadInt16(),
			};
			r.Shape = StageRooms[r.StageIndex][Tuple.Create(r.RoomType, r.RoomId)].RoomShape;
			return r;
		}

	}

	public class DumpStage
	{
		public int StageId;
		public int StageType;
		public DumpRoom[][] RoomGrid;
		public DumpRoom[] Rooms;

		public DumpStage(int stageId, int stageType, DumpRoom[][] grid, DumpRoom[] rooms) {
			StageId = stageId;
			StageType = stageType;
			RoomGrid = grid;
			Rooms = rooms;
		}
	}

	public class DumpStageBlock
	{
		public uint Seed;
		public DumpStage[] Stages;
	}
	public class DumpRoom
	{
		public int StageIndex;
		public int RoomOffset;
		public int RoomX;
		public int RoomY;
		public RoomType RoomType;
		public int RoomSubType;
		public int RoomId;
		public RoomShape Shape;
	}
}
