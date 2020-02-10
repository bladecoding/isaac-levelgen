using System;
using System.Collections.Generic;
using System.Drawing;

namespace isaac_levelgen
{
    public class StageLayout
    {
        public Room[][] RoomGrid;
        public List<Room> Rooms;
        public List<Room> DeadEnds;

        public StageLayout() {
            Rooms = new List<Room>();
            RoomGrid = new Room[13][];
            for (var j = 0; j < RoomGrid.Length; j++)
                RoomGrid[j] = new Room[13];
        }

        public Room PlaceRoom(Room room) {
            room.Number = Rooms.Count;
            var points = Room.ShapePoints[room.Shape];
            for (var i = 0; i < points.Length; i++) {
                var p = room.Coords.Add(points[i]);
                if (!StageLayout.InBounds(p))
                    continue;

                RoomGrid[p.Y][p.X] = room;
            }
            Rooms.Add(room);
            return room;
        }

        public void ReshapeRoom(Room room, RoomShape newShape, Point newPos) {
            var oldPoints = Room.ShapePoints[room.Shape];
            //Remove old shape from grid
            for (var i = 0; i < oldPoints.Length; i++) {
                var p = room.Coords.Add(oldPoints[i]);
                if (!InBounds(p)) {
                    Console.WriteLine("Room outside bounds!!!");
                    continue;
                }

                RoomGrid[p.Y][p.X] = null;
            }
            //Add new shape to grid
            var newPoints = Room.ShapePoints[newShape];
            for (var i = 0; i < newPoints.Length; i++) {
                var p = newPos.Add(newPoints[i]);
                if (!InBounds(p)) {
                    Console.WriteLine("Room outside bounds!!!");
                    continue;
                }

                RoomGrid[p.Y][p.X] = room;
            }
            room.Coords = newPos;
            room.Shape = newShape;

            //foreach (var neighbor in room.Neighbors.Values)
            //    CalculateNeighbors(neighbor);
            //CalculateNeighbors(room);
        }

        public void ReaddDeadEnd(Room r) {
            if (r == null)
                return;
            this.DeadEnds.Add(r);
        }

        public void CalculateNeighbors(Room room) {
            room.Neighbors = new Dictionary<int, Room>();
            for (var j = 0; j < 8; j++) {
                var neighbor = GetRoom(room.GetNeighborCoords(j));
                if (neighbor != null)
                    room.Neighbors.Add(j, neighbor);
            }
        }
        //This is normally calculated and stored on layout rooms during the calculate neighbors function
        public int CalculateDoorBits(Room room) {
            var bits = 0;
            for (var j = 0; j < 8; j++) {
                var neighbor = GetRoom(room.GetNeighborCoords(j));
                if (neighbor != null)
                    bits |= (1 << j);
            }
            return bits;
        }

        public static bool InBounds(Point p) {
            return p.X >= 0 && p.Y >= 0 && p.X < 13 && p.Y < 13;
        }

        public Room GetRoom(int x, int y) {
            if (x < 0 || y < 0 || x >= 13 || y >= 13)
                return null;

            return RoomGrid[y][x];
        }
        public Room GetRoom(Point p) {
            return GetRoom(p.X, p.Y);
        }

        /// <summary>
        /// Get the number of neighboring cells
        /// </summary>
        public int GetNeighborCount(Point p, Room ignore) {
            int num = 0;
            var dirs = Room.ShapeDoors[RoomShape.ROOMSHAPE_1x1];
            for (var i = 0; i < 4; i++) {
                var t = p.Add(dirs[i].Value);
                var n = GetRoom(t.X, t.Y);
                if (InBounds(t) && n != null && (ignore != null && n != ignore))
                    num++;
            }
            return num;
        }
        public int GetNeighborCount(Room r) {
            return GetNeighborCount(r.PlacedCoords, r);
        }
    }

}
