using System.Collections.Generic;
using System.Drawing;

namespace isaac_levelgen
{
    public class Room
    {
        public int StageIndex;
        public int RoomOffset;

        //TODO: Make sure LTL rooms are handled correctly. The Room.Coords of an LTL are outside the room.

        //Room top left
        public int RoomX;
        public int RoomY;
        //Used when calculating neighbor cells
        public int PlacedX;
        public int PlacedY;

        //Parent's direction to this cell
        public int DirectionFromParent;

        public bool IsDeadEnd;

        //Distance from start room
        public int Distance;

        // Key is door
        public Dictionary<int, Room> Neighbors;

        public RoomType RoomType;
        public int RoomSubType;
        public int RoomId;
        public RoomShape Shape;
        public int Index { get { return RoomX + (RoomY * 13); } }
        public int Number;
        public uint RoomSeed;
        public RoomDescriptor Descriptor { get; set; }
        //This exists on layout rooms. There is a short period when a
        //secret room is added to the layout but doesn't have a RoomType.
        //This might be unnecessary.
        public bool Invisible;
        public Point Coords { get { return new Point(RoomX, RoomY); } set { RoomX = value.X; RoomY = value.Y; } }
        public Point PlacedCoords { get { return new Point(PlacedX, PlacedY); } }
        public Room(RoomShape shape, Point xy, Point placedXY, int parentDir, int dist)
            : this(shape, xy.X, xy.Y, placedXY.X, placedXY.Y, parentDir, dist) {
        }
        public Room(RoomShape shape, Point xy, int parentDir)
            : this(shape, xy.X, xy.Y, -1, -1, parentDir, 0) {
        }
        public Room(RoomShape shape, int x, int y, int placedX, int placedY, int parentDir, int dist) {
            Shape = shape;
            RoomX = x;
            RoomY = y;
            PlacedX = placedX;
            PlacedY = placedY;
            DirectionFromParent = parentDir;
            Distance = dist;
        }

        public void ApplyDescriptor(RoomDescriptor rd, Rng rng) {
            RoomType = rd.RoomType;
            RoomSubType = rd.RoomSubType;
            RoomId = rd.RoomId;
            RoomSeed = rng.Seed;
            Descriptor = rd;
        }
        public static Point GetNeighborCoords(Point coords, RoomShape shape, int door) {
            var doors = ShapeDoors[shape];
            var off = doors[door];
            if (off == null)
                return PointExt.Invalid;
            return coords.Add(off.Value);
        }

        public static Point GetDoorOrigin(Point coords, RoomShape shape, int door) {
            var doors = ShapeDoors[shape];
            var off = doors[door];
            if (off == null)
                return PointExt.Invalid;
            var dst = coords.Add(off.Value);
            switch (door) {
                case 0:
                case 4:
                    return dst.Add(1, 0);
                case 1:
                case 5:
                    return dst.Add(0, 1);
                case 2:
                case 6:
                    return dst.Add(-1, 0);
                case 3:
                case 7:
                    return dst.Add(0, -1);
                default:
                    return PointExt.Invalid;
            }
        }

        public Point GetNeighborCoords(int door) {
            return GetNeighborCoords(Coords, Shape, door);
        }

        public Point GetPlacedNeighborCellCoords(int direction) {
            return GetNeighborCellCoords(PlacedCoords, direction);
        }

        public static Point GetNeighborCellCoords(Point p, int direction) {
            var doors = ShapeDoors[RoomShape.ROOMSHAPE_1x1];
            var off = doors[direction];
            if (off == null)
                return PointExt.Invalid;
            return p.Add(off.Value);
        }

        //Destination offsets
        public static Dictionary<RoomShape, Point?[]> ShapeDoors = new Dictionary<RoomShape, System.Drawing.Point?[]>
        {
            // L U R D, L U R D
			{RoomShape.ROOMSHAPE_1x1, new Point?[] { new Point(-1, 0), new Point(0, -1),  new Point(1, 0),  new Point(0, 1),    null, null, null, null  } },
            {RoomShape.ROOMSHAPE_1x2, new Point?[] { new Point(-1, 0), new Point(0, -1),  new Point(1, 0),  new Point(0, 2),    new Point(-1, 1), null, new Point(1, 1), null  } },
            {RoomShape.ROOMSHAPE_2x1, new Point?[] { new Point(-1, 0), new Point(0, -1),  new Point(2, 0),  new Point(0, 1),    null, new Point(1, -1), null, new Point(1, 1) } },
            {RoomShape.ROOMSHAPE_2x2, new Point?[] { new Point(-1, 0), new Point(0, -1),  new Point(2, 0),  new Point(0, 2),    new Point(-1, 1), new Point(1, -1), new Point(2, 1), new Point(1, 2) } },
            {RoomShape.ROOMSHAPE_IH,  new Point?[] { new Point(-1, 0), null, new Point(1, 0), null,                             null, null, null, null } },
            {RoomShape.ROOMSHAPE_IIH, new Point?[] { new Point(-1, 0), null, new Point(2, 0), null,                             null, null, null, null } },
            {RoomShape.ROOMSHAPE_IV,  new Point?[] { null, new Point(0, -1), null,  new Point(0, 1),                            null, null, null, null } },
            {RoomShape.ROOMSHAPE_IIV, new Point?[] { null, new Point(0, -1), null,  new Point(0, 2),                            null, null, null, null } },
            {RoomShape.ROOMSHAPE_LTL, new Point?[] { new Point(0, 0), new Point(0, 0), new Point(2, 0), new Point(0, 2),        new Point(-1, 1), new Point(1, -1), new Point(2, 1), new Point(1, 2) } },
            {RoomShape.ROOMSHAPE_LTR, new Point?[] { new Point(-1, 0), new Point(0, -1), new Point(1, 0), new Point(0, 2),      new Point(-1, 1), new Point(1, 0), new Point(2, 1), new Point(1, 2) } },
            {RoomShape.ROOMSHAPE_LBL, new Point?[] { new Point(-1, 0), new Point(0, -1), new Point(2, 0), new Point(0, 1),      new Point(0, 1), new Point(1, -1), new Point(2, 1), new Point(1, 2)} },
            {RoomShape.ROOMSHAPE_LBR, new Point?[] { new Point(-1, 0), new Point(0, -1), new Point(2, 0), new Point(0, 2),      new Point(-1, 1),  new Point(1, -1), new Point(1, 1), new Point(1, 1) } },
        };

        public static Dictionary<RoomShape, Point[]> ShapePoints = new Dictionary<RoomShape, System.Drawing.Point[]>
        {
            {RoomShape.ROOMSHAPE_NULL, new Point[0] },
            {RoomShape.ROOMSHAPE_1x1, new [] { new Point(0, 0) } },
            {RoomShape.ROOMSHAPE_1x2, new [] { new Point(0, 0), new Point(0, 1) } },
            {RoomShape.ROOMSHAPE_2x1, new [] { new Point(0, 0), new Point(1, 0) } },
            {RoomShape.ROOMSHAPE_2x2, new [] { new Point(0, 0), new Point(1, 0), new Point(1, 1), new Point(0, 1) } },
            {RoomShape.ROOMSHAPE_IH,  new [] { new Point(0, 0) } },
            {RoomShape.ROOMSHAPE_IIH, new [] { new Point(0, 0), new Point(1, 0) } },
            {RoomShape.ROOMSHAPE_IV,  new [] { new Point(0, 0) } },
            {RoomShape.ROOMSHAPE_IIV, new [] { new Point(0, 0), new Point(0, 1) } },
            {RoomShape.ROOMSHAPE_LTL, new [] { new Point(1, 0), new Point(1, 1), new Point(0, 1) } },
            {RoomShape.ROOMSHAPE_LTR, new [] { new Point(0, 0), new Point(1, 1), new Point(0, 1) } },
            {RoomShape.ROOMSHAPE_LBL, new [] { new Point(0, 0), new Point(1, 0), new Point(1, 1) } },
            {RoomShape.ROOMSHAPE_LBR, new [] { new Point(0, 0), new Point(1, 0), new Point(0, 1) } },
        };

        public static Point[][] DirectionShapeOffsets = new Point[][] {
            // Left
            new Point[] { new Point(-1, 0), new Point(0, 0), new Point(0, -1), new Point(-1, 0), new Point(-1, -1), new Point(-1, 0), new Point(-1, -1), new Point(-1, 0), new Point(-1, -1), new Point(-1, 0), new Point(-1, -1), new Point(0, 0), new Point(-1, 0) },
            // Up
            new Point[] { new Point(0, -1), new Point(0, 0), new Point(-1, 0), new Point(0, -1), new Point(-1, -1), new Point(0, -1), new Point(-1, -1), new Point(0, -1), new Point(-1, -1), new Point(0, -1), new Point(-1, -1), new Point(0, 0), new Point(0, -1) },
            // Right
            new Point[] { new Point(0, 0), new Point(0, 0), new Point(0, -1), new Point(0, 0), new Point(0, -1), new Point(0, 0), new Point(0, -1), new Point(0, 0), new Point(0, -1), new Point(0, 0), new Point(0, -1), new Point(0, 0), new Point(0, 0) },
            // Down
            new Point[] { new Point(0, 0), new Point(0, 0), new Point(-1, 0), new Point(0, 0), new Point(-1, 0), new Point(0, 0), new Point(-1, 0), new Point(0, 0), new Point(-1, 0), new Point(0, 0), new Point(-1, 0), new Point(0, 0), new Point(0, 0) },
        };

        //Todo: Convert integers to RoomShape
        public static int[][] DirectionToShape = new int[][] {
            // Left
            new int[] { 0x00000006, 0x00000004, 0x00000004, 0x00000008, 0x00000008, 0x0000000C, 0x0000000A, 0x0000000B, 0x0000000B, 0x00000009, 0x00000009, 0x00000002, 0x00000007 },
            // Up
            new int[] { 0x00000004, 0x00000006, 0x00000006, 0x00000008, 0x00000008, 0x0000000C, 0x0000000B, 0x0000000A, 0x0000000A, 0x00000009, 0x00000009, 0x00000003, 0x00000005 },
            // Right
            new int[] { 0x00000006, 0x00000004, 0x00000004, 0x00000008, 0x00000008, 0x0000000B, 0x00000009, 0x0000000A, 0x0000000A, 0x0000000C, 0x0000000C, 0x00000002, 0x00000007 },
            // Down
            new int[] { 0x00000004, 0x00000006, 0x00000006, 0x00000008, 0x00000008, 0x0000000A, 0x00000009, 0x0000000B, 0x0000000B, 0x0000000C, 0x0000000C, 0x00000003, 0x00000005 },
        };

        public static int[] ResizeRoomDoorMask = new int[] {
            0, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 0
        };

        public static int[] ShapeWeights = new int[]
        {
            0x00000030, 0x00000018, 0x00000018, 0x00000018, 0x00000018, 0x00000048, 0x00000048, 0x00000024,
            0x00000024, 0x00000024, 0x00000024, 0x00000018, 0x00000018
        };
    }

}
