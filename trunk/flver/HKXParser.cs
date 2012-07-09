using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace flver {

  public class HKXGeometry {
    public ushort[] Indices;
    public float[] Points;
  }

  public class HKXParser {

    private DataStream d;
    private AutoAdvanceDataReader od;
    
    public readonly List<HKXGeometry> Geoemtries = new List<HKXGeometry>();

    public HKXParser(DataStream d) {
      this.d = d;
      this.od = new AutoAdvanceDataReader(d);
    }

    public static HKXParser ParseHKX(string s) {
      FileDataStream fds = new FileDataStream(s);
      HKXParser p = new HKXParser(fds);
      p.Parse();
      fds.Close();
      return p;
    }

    public void Parse() {
      uint b = d.ReadUInt(0x594) + 0x5a0;

      float m00 = d.ReadFloat(0x3F0 + 0 * 0x10 + 0);
      float m10 = d.ReadFloat(0x3F0 + 0 * 0x10 + 4);
      float m20 = d.ReadFloat(0x3F0 + 0 * 0x10 + 8);
      float m30 = d.ReadFloat(0x3F0 + 0 * 0x10 + 12);

      float m01 = d.ReadFloat(0x3F0 + 1 * 0x10 + 0);
      float m11 = d.ReadFloat(0x3F0 + 1 * 0x10 + 4);
      float m21 = d.ReadFloat(0x3F0 + 1 * 0x10 + 8);
      float m31 = d.ReadFloat(0x3F0 + 1 * 0x10 + 12);

      float m02 = d.ReadFloat(0x3F0 + 2 * 0x10 + 0);
      float m12 = d.ReadFloat(0x3F0 + 2 * 0x10 + 4);
      float m22 = d.ReadFloat(0x3F0 + 2 * 0x10 + 8);
      float m32 = d.ReadFloat(0x3F0 + 2 * 0x10 + 12);

      float m03 = d.ReadFloat(0x3F0 + 3 * 0x10 + 0);
      float m13 = d.ReadFloat(0x3F0 + 3 * 0x10 + 4);
      float m23 = d.ReadFloat(0x3F0 + 3 * 0x10 + 8);
      float m33 = d.ReadFloat(0x3F0 + 3 * 0x10 + 12);

      uint ccount = d.ReadUInt(b + 0xbc);
      if (ccount == 0) {
        return;
      }

      uint vbase = b + 0x220 + (ccount - 1) * 0x70;
      Vec offset = d.ReadVec3(0x420);

      int vofs = 0;
      for (int c = 0; c < ccount; ++c) {
        uint vcount = d.ReadUInt(vbase - 0x70 + 0x0c);
        int vbaseadj = 0;
        while (vcount == 0) {
          vbase += 16;
          vbaseadj++;
          vcount = d.ReadUInt(vbase - 0x70 + 0x0c);
        }

        HKXGeometry g = new HKXGeometry();
        Geoemtries.Add(g);

        g.Points = new float[vcount * 3];
        for (int i = 0; i < vcount; ++i) {
          float x = d.ReadFloat(vbase + i * 0x10 + 0);
          float y = d.ReadFloat(vbase + i * 0x10 + 4);
          float z = d.ReadFloat(vbase + i * 0x10 + 8);

          g.Points[i * 3 + 0] = x * m00 + y * m01 + z * m02 + m03;
          g.Points[i * 3 + 1] = x * m10 + y * m11 + z * m12 + m13;
          g.Points[i * 3 + 2] = x * m20 + y * m21 + z * m22 + m23;
        }

        uint tcount = d.ReadUInt(vbase - 0x70 + 0x24) / 4;
        g.Indices = new ushort[tcount * 3];
        uint tbase = vbase + vcount * 0x10;
        for (int i = 0; i < tcount; ++i) {
          uint tibase = tbase + (uint)i * 8;
          ushort v0 = d.ReadUShort(tibase + 0);
          ushort v1 = d.ReadUShort(tibase + 2);
          ushort v2 = d.ReadUShort(tibase + 4);
          g.Indices[i * 3 + 0] = (ushort)v0;
          g.Indices[i * 3 + 1] = (ushort)v1;
          g.Indices[i * 3 + 2] = (ushort)v2;
        }

        vbase = tbase + tcount * 8 + 0x70;
        if ((vbase % 16) != 0) {
          vbase += 8;
        }
        vofs += (int)vcount;
      }
    }
  }
}
