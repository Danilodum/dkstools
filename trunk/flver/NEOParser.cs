// (c) 2012 vlad001

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

namespace flver {

  public class Param {
    public uint Length;
    public long Offset;
  }

  public class ModelParam : Param {
    public string Name;
    public string Filename;
    public int InstanceCount;
    public uint Type;
    public uint LocalID;
    public readonly List<InstanceParam> InstanceParams = new List<InstanceParam>();
  }

  public class InstanceParam : Param {
    public string Name;
    public uint Type;
    public uint LocalID;
    public Vec Translation, Euler, Scale;
    public ModelParam model;
  }

  public class PointParam : Param {
    public uint Id;
    public string Name;
    public Vec Pos;
  }

  public class EventParam : Param {
    public uint Id;
    public uint Type;
    public uint LocalID;
    public uint[] P1;
    public uint[] P2;
    public string Name;
  }

  public class NEOParser
  {
    private string name;
    private DataStream ds;
    private AutoAdvanceDataReader od;

    public List<ModelParam> ModelParams = new List<ModelParam>();
    public List<InstanceParam> PartParams = new List<InstanceParam>();
    public List<PointParam> PointParams = new List<PointParam>();
    public List<EventParam> EventParams = new List<EventParam>();

    public NEOParser(string name, DataStream ds) {
      this.ds = ds;
      this.name = name;
    }

    public static NEOParser ParseNEO(string s) {
      FileDataStream fds = new FileDataStream(s);
      NEOParser p = new NEOParser(Path.GetFileNameWithoutExtension(s), fds);
      p.Parse();
      fds.Close();
      return p;
    }

    private void DumpUInts(TextWriter w, uint[] us) {
      if (us != null) {
        foreach (uint n in us) {
          w.Write("{0:X08} ", n);
        }
      }
    }

    public void Dump(TextWriter w) {
      int index = 0;
      foreach (ModelParam mp in ModelParams) {
        w.WriteLine("model      {0:X04} len={6:X04}/ofs={7:X08} {1,-20} type={2:X04} localid={3:X04} instance-count={4} {5}", 
          index, mp.Name, mp.Type, mp.LocalID, mp.InstanceCount,
          mp.Filename, mp.Length, mp.Offset);
        int iindex = 0;
        foreach (InstanceParam ip in mp.InstanceParams) {
          w.WriteLine("  instance {0:X04} len={7:X04}/ofs={8:X08} {1,-20} type={2:X04} localid={3:X04} trafo={4} {5} {6}", iindex, ip.Name, ip.Type, ip.LocalID,
            ip.Translation.ToString(", ", CultureInfo.InvariantCulture, "+000.000;-000.000"),
            ip.Euler.ToString(", ", CultureInfo.InvariantCulture, "+000.000;-000.000"),
            ip.Scale.ToString(", ", CultureInfo.InvariantCulture, "+000.000;-000.000"),
            ip.Length, ip.Offset
            );
          ++iindex;
        }
        ++index;
      }

      index = 0;
      foreach (PointParam pp in PointParams) {
        w.WriteLine("point {0:X04} len={4:X04}/ofs={5:X08} {1:X04} {2} {3}", index, pp.Id, 
          pp.Pos.ToString(", ", CultureInfo.InvariantCulture, "+000.000;-000.000"),
          pp.Name,
          pp.Length,pp.Offset);
        ++index;
      }
      index = 0;

      foreach (EventParam ep in EventParams) {
        w.Write("event {0:X04} len={1:X04}/ofs={2:X08} id={3:X04} type={4:X04} localid={5:X04} P1=", 
          index, ep.Length, ep.Offset,
          ep.Id, ep.Type, ep.LocalID);
        DumpUInts(w, ep.P1);
        w.Write("P2=");
        DumpUInts(w, ep.P2);

        w.WriteLine("{0}", ep.Name);
        ++index;
      }
    }

    private void ParseModelParam(uint length) {
      ModelParam mp = new ModelParam();
      mp.Length = length;
      mp.Offset = od.Offset;
      mp.Name = ds.ReadStr(od.Offset + od.ReadUInt());
      mp.Type = od.ReadUInt();
      mp.LocalID = od.ReadUInt();
      mp.Filename = ds.ReadStr(od.Offset + od.ReadUInt()); // filename
      mp.InstanceCount = (int)od.ReadUInt();
      ModelParams.Add(mp);
    }

    private void ParseEventParam(uint length) {
      EventParam ep = new EventParam();
      ep.Length = length;
      ep.Offset = od.Offset;
      long ofs = od.Offset;
      ep.Name = ds.ReadStr(od.Offset + od.ReadUInt());
      ep.Id = od.ReadUInt();
      ep.Type = od.ReadUInt();
      ep.LocalID = od.ReadUInt();

      uint op1 = od.ReadUInt();
      uint op2 = od.ReadUInt();

      ep.P1 = new uint[4];
      ep.P1[0] = ds.ReadUInt(ofs + op1 + 0*4);
      ep.P1[1] = ds.ReadUInt(ofs + op1 + 1*4);
      ep.P1[2] = ds.ReadUInt(ofs + op1 + 2*4);
      ep.P1[3] = ds.ReadUInt(ofs + op1 + 3*4);


      ep.P2 = new uint[(int)(length-op2)/4];
      for (uint i = 0; i < ep.P2.Length; ++i) {
        ep.P2[i] = ds.ReadUInt(ofs + op2 + i * 4);
      }

      EventParams.Add(ep);
    }

    private void ParsePointParam(uint length)
    {
      PointParam pp = new PointParam();
      pp.Length = length;
      pp.Offset = od.Offset;
      pp.Name = ds.ReadStr(od.Offset + od.ReadUInt());
      uint u = od.ReadUInt();
      pp.Id = od.ReadUInt();
      u = od.ReadUInt();
      pp.Pos = od.ReadVec3();
      PointParams.Add(pp);
    }

    private void ParsePartParam(uint length) {
      InstanceParam ip = new InstanceParam();
      ip.Length = length;
      ip.Offset = od.Offset;
      ip.Name = ds.ReadStr(od.Offset + od.ReadUInt());
      ip.Type = od.ReadUInt();
      ip.LocalID = od.ReadUInt();
      uint modelindex = od.ReadUInt();
      ModelParam mp = ModelParams[(int)modelindex];
      mp.InstanceParams.Add(ip);
      ip.model = mp;
      uint u = od.ReadUInt();
      ip.Translation = od.ReadVec3();
      ip.Euler = od.ReadVec3();
      ip.Scale = od.ReadVec3();
      PartParams.Add(ip);
    }

    private string GetSectionName() {
      uint nofs = od.ReadUInt();
      return ds.ReadStr(nofs);
    }

    private delegate void DetailParser(uint length);
    private bool ParseTable(DetailParser dp) {
      uint count = od.ReadUInt();
      long tableoffset = od.Offset;
      for (uint i = 0; i < count - 1; ++i) {
        uint dataoffset = ds.ReadUInt(tableoffset + i * 4);
        uint nextdataoffset = ds.ReadUInt(tableoffset + (i+1) * 4);
        od.Offset = dataoffset;
        dp(nextdataoffset-dataoffset);
      }
      uint nextofs = ds.ReadUInt(tableoffset + (count - 1) * 4);
      od.Offset = nextofs;
      return nextofs > 0;
    }

    public void Parse() {
      od = new AutoAdvanceDataReader(ds);
      bool cont;
      do {
        od.ReadUInt();
        string sn = GetSectionName();
        DetailParser dp;
        if ("MODEL_PARAM_ST".Equals(sn)) {
          dp = ParseModelParam;
        }
        else if ("EVENT_PARAM_ST".Equals(sn)) {
          dp = ParseEventParam;
        }
        else if ("POINT_PARAM_ST".Equals(sn)) {
          dp = ParsePointParam;
        }
        else if ("PARTS_PARAM_ST".Equals(sn)) {
          dp = ParsePartParam;
        }
        else {
          Console.WriteLine("unknown section: {0}", sn);
          return;
        }

        cont = ParseTable(dp);

      } while (cont);
    }
  }

}
