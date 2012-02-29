// (c) 2012 vlad001

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace flver {

  [Serializable]
  public class Texture2D {
    public uint DataOffset, DataLen, Subtype;
    public uint Width, Height;
    
    // 0x0000: dxt1, 
    // 0x0500: dxt5
    public uint Type;

    // big endian!
    public byte[] Data;
    public string Name;

    public override string ToString() {
      return Name + " (" + Width + "x" + Height + ") " + string.Format("{0:X04}", Type);
    }
  }

  public class TPFParser {
    public int LoadTex = -1;
    public List<Texture2D> Tex = new List<Texture2D>();

    private string name;
    private DataStream ds;

    public TPFParser(string name, DataStream ds) {
      this.ds = ds;
      this.name = name;
    }

    public static TPFParser ParseTPF(string s) {
      FileDataStream fds = new FileDataStream(s);
      TPFParser p = new TPFParser(Path.GetFileNameWithoutExtension(s), fds);
      p.Parse();
      fds.Close();
      return p;
    }


    public void Dump(TextWriter w) {
      int i = 0;
      foreach (Texture2D t in Tex) {
        w.WriteLine("{0,-40} {1,2}/{9,2} type={2:X08} dofs={3:X08} dlen={4:X08} {5,4}x{6,-4} {7,7:0.00} bpp {8} ",
          this.name, i, t.Type, t.DataOffset, t.DataLen, t.Width, t.Height, (float)t.DataLen / (t.Width * t.Height), t.Name, Tex.Count, t.Subtype);
        ++i;
      }
    }

    public void Parse() {
      AutoAdvanceDataReader od = new AutoAdvanceDataReader(ds);
      od.ReadUInt();
      od.ReadUInt();
      uint count = od.ReadUInt();

      for (uint i = 0; i < count; ++i) {
        Texture2D t = new Texture2D();

        od.Offset = i * 0x1C + 0x10;
        t.DataOffset = od.ReadUInt();
        t.DataLen = od.ReadUInt();
        t.Type = od.ReadUShort();
        t.Subtype = od.ReadUShort();

        t.Width = od.ReadUShort();
        t.Height = od.ReadUShort();
        od.ReadUInt();
        uint nofs = od.ReadUInt();
        t.Name = ds.ReadStr(nofs);

        if (LoadTex == (int)i || LoadTex == -1) {
          t.Data = ds.ReadBytes(t.DataOffset, t.DataLen);
        }

        Tex.Add(t);
      }
    }
  }

}
