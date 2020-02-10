using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.XPath;
using System.Xml.Linq;

namespace isaac_levelgen
{
    public class RoomsProvider
    {
        public Dictionary<int, List<RoomDescriptor>> StageRooms;
        public void Load(string path) {
            var xmlFiles = new DirectoryInfo(path).GetFiles("*.xml");
            StageRooms = new Dictionary<int, List<RoomDescriptor>>();
            for (var i = 0; i < xmlFiles.Length; i++) {
                var stageIdx = int.Parse(Regex.Match(xmlFiles[i].Name, "^(\\d\\d)").Groups[1].Value);
                var doc = XDocument.Load(xmlFiles[i].FullName);
                var xmlRooms = doc.XPathSelectElements("//rooms/room").ToList();

                var rooms = new List<RoomDescriptor>();
                for (var j = 0; j < xmlRooms.Count; j++) {
                    var id = xmlRooms[j].Get<int>("variant");
                    var type = (RoomType)xmlRooms[j].Get<int>("type");
                    var shape = (RoomShape)xmlRooms[j].Get<int>("shape");
                    var weight = xmlRooms[j].Get<float>("weight");
                    var width = xmlRooms[j].Get<int>("width");
                    var height = xmlRooms[j].Get<int>("height");

                    //Get existing doors
                    var doors = xmlRooms[j].XPathSelectElements("door")
                        .Select(d => (
                            x: d.Get<int>("x"),
                            y: d.Get<int>("y"),
                            exists: d.Get<int>("exists")))
                        .Where(d => d.exists != 0)
                        .ToList();

                    var doorFlags = DoorsToBits(shape, doors);

                    rooms.Add(new RoomDescriptor {
                        RoomType = type,
                        RoomId = id,
                        RoomSubType = xmlRooms[j].Get<int>("subtype"),
                        RoomShape = shape,
                        Difficulty = xmlRooms[j].Get<int>("difficulty"),
                        OriginalWeight = weight,
                        Weight = weight,
                        Doors = doorFlags,
                        Width = width,
                        Height = height,
                    });

                }
                StageRooms[stageIdx] = rooms;
            }
        }

        public void ResetWeights(int stageid) {
            foreach (var r in StageRooms[stageid])
                r.Weight = r.OriginalWeight;
        }

        List<RoomDescriptor> GetRoomCandidates(int stageId, RoomType type, RoomShape shape, uint minVariant, uint maxVariant, uint minDiff, uint maxDiff, int requiredDoors, int subtype) {
            var ret = new List<RoomDescriptor>();
            foreach (var rd in StageRooms[stageId]) {
                if (rd.RoomType != type)
                    continue;
                //NUM_ROOMSHAPES is used to get all shapes
                if (shape != RoomShape.NUM_ROOMSHAPES && shape != rd.RoomShape)
                    continue;

                if (rd.RoomId < minVariant || rd.RoomId > maxVariant)
                    continue;

                if (rd.Difficulty < minDiff || rd.Difficulty > maxDiff)
                    continue;

                if ((rd.Doors & requiredDoors) != requiredDoors)
                    continue;

                if (subtype != -1 && subtype != rd.RoomSubType)
                    continue;

                ret.Add(rd);
            }
            return ret;
        }

        public RoomDescriptor GetRandomRoom(uint seed, bool adjustWeight, int stageId, RoomType type, RoomShape shape, uint minVariant, uint maxVariant, uint minDiff, uint maxDiff, int requiredDoors, int subtype) {
            //Todo: Void

            var rng = new Rng(seed, 0x1, 0x15, 0x14);

            var matches = new List<RoomDescriptor>();
            var cands = GetRoomCandidates(stageId, type, shape, minVariant, maxVariant, minDiff, maxDiff, requiredDoors, subtype);
            var matchWeights = 0f;
            var totalWeight = 0f;

            var randFloat = rng.NextFloat();

            foreach (var cand in cands) {
                totalWeight += cand.Weight;
                if (cand.Doors == requiredDoors) {
                    matches.Add(cand);
                    matchWeights += cand.Weight;
                }
            }

            RoomDescriptor room = null;
            if (matches.Count != 0 && matchWeights / totalWeight * 9.99 > rng.NextFloat()) {
                var current = 0f;
                var target = randFloat * matchWeights;
                foreach (var cand in matches) {
                    current += cand.Weight;
                    if (current > target) {
                        room = cand;
                        break;
                    }
                }
            } else {
                var current = 0f;
                var target = randFloat * totalWeight;
                foreach (var cand in cands) {
                    current += cand.Weight;
                    if (current > target) {
                        room = cand;
                        break;
                    }
                }
            }

            if (room != null && adjustWeight) {
                room.Weight = Math.Max(room.Weight * 0.1f, 0.0000001f);
            }

            return room;
        }

        public bool[] GetEnabledShapes(int stageId, uint minDiff, uint maxDiff) {
            //Todo: There is a special case when there are less than 20 rooms
            var cands = GetRoomCandidates(stageId, RoomType.ROOM_DEFAULT, RoomShape.NUM_ROOMSHAPES, 0, uint.MaxValue, minDiff, maxDiff, 0, -1);
            if (cands.Count < 20)
                throw new NotImplementedException();

            var shapeCounts = new int[(int)RoomShape.NUM_ROOMSHAPES];
            foreach (var cand in cands)
                shapeCounts[(int)cand.RoomShape]++;

            return shapeCounts.Select(i => i > 0).ToArray();
        }

        static int DoorsToBits(RoomShape shape, IList<(int x, int y, int exists)> doors) {
            var locs = ShapeDoorLocations[shape];
            var bits = 0;
            for (var i = 0; i < locs.Length; i++) {
                if (locs[i] == Point.Empty)
                    continue;

                //locs should be a dictionary for quick lookups
                if (doors.Any(d => d.x == locs[i].X && d.y == locs[i].Y))
                    bits |= (1 << i);
            }
            return bits;
        }
        //Used for converting door xml elements into bit flags
        public static Dictionary<RoomShape, Point[]> ShapeDoorLocations = new Dictionary<RoomShape, Point[]> {

            // Point.Empy means that door doesn't exist in that shape
            { RoomShape.ROOMSHAPE_1x1, new [] { new Point(-1, 3), new Point(6, -1), new Point(13, 3), new Point(6, 7) } },
            { RoomShape.ROOMSHAPE_IH,  new [] { new Point(-1, 3), Point.Empty,      new Point(13, 3) } },
            { RoomShape.ROOMSHAPE_IV,  new [] { Point.Empty,      new Point(6, -1), Point.Empty,      new Point(6, 7) } },
            { RoomShape.ROOMSHAPE_IIV, new [] { Point.Empty,      new Point(6, -1), Point.Empty,      new Point(6, 14) } },
            { RoomShape.ROOMSHAPE_IIH, new [] { new Point(-1, 3), Point.Empty,      new Point(26, 3) } },

            { RoomShape.ROOMSHAPE_1x2, new [] { new Point(-1, 3), new Point(6, -1), new Point(13, 3), new Point(6, 14),    new Point(-1, 10), Point.Empty,       new Point(13, 10), Point.Empty } },
            { RoomShape.ROOMSHAPE_2x1, new [] { new Point(-1, 3), new Point(6, -1), new Point(26, 3), new Point(6, 7),     Point.Empty,       new Point(19, -1), Point.Empty,       new Point(19, 7) } },
            { RoomShape.ROOMSHAPE_2x2, new [] { new Point(-1, 3), new Point(6, -1), new Point(26, 3), new Point(6, 14),    new Point(-1, 10), new Point(19, -1), new Point(26, 10), new Point(19, 14) } },

            { RoomShape.ROOMSHAPE_LTL, new [] { new Point(12, 3), new Point(6, 6),  new Point(26, 3), new Point(6, 14),    new Point(-1, 10), new Point(19, -1), new Point(26, 10), new Point(19, 14) } },
            { RoomShape.ROOMSHAPE_LTR, new [] { new Point(-1, 3), new Point(6, -1), new Point(13, 3), new Point(6, 14),    new Point(-1, 10), new Point(19, 6),  new Point(26, 10), new Point(19, 14) } },

            { RoomShape.ROOMSHAPE_LBL, new [] { new Point(-1, 3), new Point(6, -1), new Point(26, 3), new Point(6, 7),     new Point(12, 10), new Point(19, -1), new Point(26, 10), new Point(19, 14) } },
            { RoomShape.ROOMSHAPE_LBR, new [] { new Point(-1, 3), new Point(6, -1), new Point(26, 3), new Point(6, 14),    new Point(-1, 10), new Point(19, -1), new Point(13, 10), new Point(19, 7) } },

        };
    }

}
