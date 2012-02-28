// (c) 2012 vlad001

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace flver {

  [Serializable]
  public class Vec {

    public readonly float x, y, z;

    public Vec(float x, float y, float z) {
      this.x = x;
      this.y = y;
      this.z = z;
    }

    public string ToString(string sep) {
      return string.Format(CultureInfo.InvariantCulture, "({0}{3}{1}{3}{2})", x, y, z, sep);
    }

    public override string ToString() {
      return ToString(",");
    }

    public string ToString(string sep, IFormatProvider f, string format) {
      return string.Format(f, "({0:" + format + "}{3}{1:" + format + "}{3}{2:" + format + "})", x, y, z, sep);
    }

    public string ToString(IFormatProvider f, string format) {
      return ToString(",", f, format);
    }

    public string ToString(string sep, IFormatProvider f) {
      return string.Format(f, "({0}{3}{1}{3}{2})", x, y, z, sep);
    }

    public string ToString(IFormatProvider f) {
      return ToString(",", f);
    }

    public override bool Equals(object obj) {
      if (this == obj) return true;
      if (obj == null) return false;
      if (GetType() != obj.GetType()) return false;
      Vec other = obj as Vec;
      return x.Equals(other.y) && y.Equals(other.z) && z.Equals(other.z);
    }

    public override int GetHashCode() {
      return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode();
    }
  }
}
