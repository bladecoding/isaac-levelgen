using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace isaac_levelgen
{
	public static class PointExt
	{
		public static Point Add(this Point l, Point r) {
			return new Point(l.X + r.X, l.Y + r.Y);
		}
		public static Point Add(this Point l, int x, int y) {
			return new Point(l.X + x, l.Y + y);
		}

		public static readonly Point Invalid = new Point(-1, -1);
	}

	public static class ListExt
	{
		public static void PushFront<T>(this List<T> list, T item)
		{
			list.Add(item);
		}
		public static void PushBack<T>(this List<T> list, T item)
		{
			list.Insert(0, item);
		}

		public static T Pop<T>(this List<T> list)
		{
			var e = list[list.Count - 1];
			list.RemoveAt(list.Count - 1);
			return e;
		}
	}
}
