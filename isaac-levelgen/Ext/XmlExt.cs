using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace isaac_levelgen
{
	public static class XmlExt
	{
		public static string Get(this XElement e, string name, string defVal = default(string)) {
			var attr = e.Attribute(name);
			return attr != null ? attr.Value : defVal;
		}
		public static T Get<T>(this XElement e, string name, T defVal = default(T)) {
			var attr = e.Attribute(name);
			return attr != null ? (T)Convert.ChangeType(attr.Value, typeof(T)) : defVal;
		}
	}
}
