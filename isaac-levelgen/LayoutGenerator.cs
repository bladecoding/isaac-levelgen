using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace isaac_levelgen
{
    public class LayoutGenerator
    {
        public GameState Game;
        public LayoutState State;

        public LayoutGenerator(GameState game, LayoutState state) {
            Game = game;
            State = state;
        }

        public StageLayout Create(int maxRooms) {
            var layout = Generate(maxRooms);

            CalculateDeadEnds(layout);
            if (layout.DeadEnds.Count < 5) {
                for (var i = 0; i < 5; i++) {
                    AddDeadEnd(layout);
                    CalculateDeadEnds(layout);
                    if (layout.DeadEnds.Count >= 5)
                        break;
                }

            }

            SortList(layout.DeadEnds); //Isaac doesn't use a stable sort

            return layout;
        }

        static void SortList(List<Room> list) {
            for (int i = 0; i < list.Count; i++) {
                var sIdx = i;
                for (var j = i + 1; j < list.Count; j++) {
                    if (list[sIdx].Distance < list[j].Distance)
                        sIdx = j;
                }
                var t = list[sIdx];
                list[sIdx] = list[i];
                list[i] = t;
            }
        }

        void AddDeadEnd(StageLayout layout) {
            for (var i = 0; i < layout.Rooms.Count; i++) {
                var room = layout.Rooms[i];
                if (TryAddDeadend(layout, room, false) != null)
                    return;
            }
        }

        public Room TryAddDeadend(StageLayout layout, Room room, bool shuffle) {
            var cands = GetNeighborCandidates(layout, room, true);
            if (shuffle) {
                State.Seed.Shuffle(cands);
            }
            for (var o = 0; o < cands.Count; o++) {
                var cand = cands[o];
                if (DoesRoomShapeFit(layout, cand.Coords, cand.Shape) && layout.GetNeighborCount(cand) < 2) {
                    return layout.PlaceRoom(cand);
                }
            }
            return null;
        }

        public Room TryAddBossDeadend(StageLayout layout, Room room, bool shuffle) {
            var cands = GetNeighborCandidates(layout, room, false);
            if (shuffle) {
                State.Seed.Shuffle(cands);
            }
            for (var o = 0; o < cands.Count; o++) {
                var cand = cands[o];
                if (DoesRoomShapeFit(layout, cand.Coords, cand.Shape) && layout.GetNeighborCount(cand) < 2 && (room.Descriptor.Doors & (1 << cand.DirectionFromParent)) != 0) {
                    return layout.PlaceRoom(cand);
                }
            }
            return null;
        }

        StageLayout Generate(int maxRooms) {
            var placed = 0;
            var layout = new StageLayout();
            var rooms = new List<Room>();

            //Place start room. Doesn't count towards placed count
            var startRoom = layout.PlaceRoom(new Room(RoomShape.ROOMSHAPE_1x1, new Point(6, 6), -1));
            rooms.PushBack(startRoom);

            while (rooms.Count > 0 && placed < maxRooms) {
                var next = rooms.Pop();
                var cands = GetNeighborCandidates(layout, next, false);
                State.Seed.Shuffle(cands);

                int candsToPlace = 0;

                if (startRoom.Index == next.Index) {
                    if (maxRooms > 0xF)
                        candsToPlace = cands.Count;
                    else
                        candsToPlace = 2 + (State.Seed.NextInt() & 1);
                } else {
                    for (var i = 0; i < 4; i++)
                        candsToPlace += State.Seed.NextInt() & 1;
                    candsToPlace = Math.Min(candsToPlace, cands.Count);
                }

                for (var i = 0; i < cands.Count; i++) {
                    var cand = cands[i];
                    if (!DoesRoomShapeFit(layout, cand.Coords, cand.Shape))
                        continue;

                    var neighborCells = layout.GetNeighborCount(cand);
                    if (neighborCells < 2 || ((State.IsXL || State.IsVoid) && State.Seed.NextInt(10) == 0)) {
                        if (!IsShapeLocValid(layout, cand.Coords, cand.Shape))
                            continue;

                        var r = layout.PlaceRoom(cand);
                        placed++;
                        if (State.Seed.NextInt(3) == 0) {
                            rooms.PushBack(r);
                        } else {
                            rooms.PushFront(r);
                        }
                        if (--candsToPlace <= 0 || placed >= maxRooms)
                            break;
                    }
                }
            }

            return layout;
        }
        //Check if the neighboring shapes have valid doors to this location
        static bool IsShapeLocValid(StageLayout layout, Point origin, RoomShape shape) {
            //testShape is used to get neighboring cells for shapes that are missing doors.
            var testShape = shape;
            if (testShape == RoomShape.ROOMSHAPE_IH)
                testShape = RoomShape.ROOMSHAPE_1x1;
            else if (testShape == RoomShape.ROOMSHAPE_IIH)
                testShape = RoomShape.ROOMSHAPE_2x1;
            else if (testShape == RoomShape.ROOMSHAPE_IV)
                testShape = RoomShape.ROOMSHAPE_1x1;
            else if (testShape == RoomShape.ROOMSHAPE_IIV)
                testShape = RoomShape.ROOMSHAPE_1x2;

            for(var door = 0; door < 8; door++) {                
                var nbCoords = Room.GetNeighborCoords(origin, testShape, door);
                if (nbCoords == PointExt.Invalid)
                    continue;
                if (!StageLayout.InBounds(nbCoords))
                    continue;

                var neighbor = layout.GetRoom(nbCoords);
                if (neighbor == null)
                    continue;

                var src = Room.GetDoorOrigin(origin, shape, door);
                if (src == PointExt.Invalid)
                    return false;

                var hasConnDoor = false;
                for (var nbDoor = 0; nbDoor < 8; nbDoor++) {
                    var coords = neighbor.GetNeighborCoords(nbDoor);
                    if (coords == src) {
                        hasConnDoor = true;
                        break;
                    }
                }
                if (!hasConnDoor)
                    return false;
            }
            return true;            
        }

        static void CalculateDeadEnds(StageLayout layout) {
            //Start room cannot be a dead end
            for (var i = 1; i < layout.Rooms.Count; i++)
                layout.Rooms[i].IsDeadEnd = layout.GetNeighborCount(layout.Rooms[i]) < 2;

            for (var i = 1; i < layout.Rooms.Count; i++) {
                var room = layout.Rooms[i];
                var dirToParent = (room.DirectionFromParent + 2) % 4;
                var parent = layout.GetRoom(room.GetPlacedNeighborCellCoords(dirToParent));
                if (parent != null)
                    parent.IsDeadEnd = false;
            }

            //for (var i = 0; i < layout.Rooms.Count; i++) {
            //    layout.CalculateNeighbors(layout.Rooms[i]);
            //}

            layout.DeadEnds = layout.Rooms.Where(r => r.IsDeadEnd).ToList();
        }

        List<Room> GetNeighborCandidates(StageLayout layout, Room room, bool enableAll) {
            var rooms = new List<Room>();
            for (var door = 0; door < 8; door++) {
                var nbCoords = room.GetNeighborCoords(door);
                if (nbCoords == PointExt.Invalid) //Door doesn't exist
                    continue;

                if (!StageLayout.InBounds(nbCoords))
                    continue;

                if (!DoesRoomShapeFit(layout, nbCoords, RoomShape.ROOMSHAPE_1x1))
                    continue;

                var direction = door % 4;
                var shapes = Room.DirectionToShape[direction];
                var offsets = Room.DirectionShapeOffsets[direction];

                if (State.IsShapeEnabled(RoomShape.ROOMSHAPE_1x1))
                    rooms.Add(new Room(RoomShape.ROOMSHAPE_1x1, nbCoords, nbCoords, direction, room.Distance + 1));


                for (int i = 0; i < (int)RoomShape.NUM_ROOMSHAPES; i++) {
                    var candidate = false;
                    if (Room.ShapeWeights[i] != 0 && (State.Seed.Next() % Room.ShapeWeights[i]) == 0)
                        candidate = true;
                    else if (!State.IsShapeEnabled(RoomShape.ROOMSHAPE_1x1) || enableAll)
                        candidate = true;

                    var offset = nbCoords.Add(offsets[i]);
                    var shape = (RoomShape)shapes[i];
                    if (State.IsShapeEnabled(shape) && candidate && DoesRoomShapeFit(layout, offset, shape))
                        rooms.Add(new Room(shape, offset, nbCoords, direction, room.Distance + 1));
                }
            }
            return rooms;
        }

        public bool DoesRoomShapeFit(StageLayout layout, Point origin, RoomShape shape) {
            var megaCoords = layout.Rooms[0].Coords.Add(new Point(0, -1));

            var points = Room.ShapePoints[shape];
            for (var i = 0; i < points.Length; i++) {
                var test = origin.Add(points[i]);
                if (!StageLayout.InBounds(test))
                    return false;

                if (State.MegaSatanDoorExists && test == megaCoords)
                    return false;
                if (layout.RoomGrid[test.Y][test.X] != null)
                    return false;
            }
            return true;
        }
    }

}
