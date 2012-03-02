// (c) 2012 vlad001

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

namespace flver {

  public class ModelParam {
    public string Name;
    public string Filename;
    public int InstanceCount;
    public uint Type;
    public uint LocalID;
    public readonly List<InstanceParam> InstanceParams = new List<InstanceParam>();
  }

  public class InstanceParam {
    public string Name;
    public uint Type;
    public uint LocalID;
    public Vec Translation, Euler, Scale;
    public ModelParam model;
  }

  public class PointParam {
    public uint Id;
    public string Name;
    public Vec Pos;
  }

  public class NEOParser {
    private string name;
    private DataStream ds;
    private AutoAdvanceDataReader od;

    public List<ModelParam> ModelParams = new List<ModelParam>();
    public List<InstanceParam> PartParams = new List<InstanceParam>();
    public List<PointParam> PointParams = new List<PointParam>();

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

    public void Dump(TextWriter w) {
      int index = 0;
      foreach (ModelParam mp in ModelParams) {
        w.WriteLine("model      {0:X04} {1,-20} type={2:X04} localid={3:X04} instance-count={4} {5}", 
          index, mp.Name, mp.Type, mp.LocalID, mp.InstanceCount,
          mp.Filename);
        int iindex = 0;
        foreach (InstanceParam ip in mp.InstanceParams) {
          w.WriteLine("  instance {0:X04} {1,-20} type={2:X04} localid={3:X04} trafo={4} {5} {6}", iindex, ip.Name, ip.Type, ip.LocalID,
            ip.Translation.ToString(", ", CultureInfo.InvariantCulture, "+000.000;-000.000"),
            ip.Euler.ToString(", ", CultureInfo.InvariantCulture, "+000.000;-000.000"),
            ip.Scale.ToString(", ", CultureInfo.InvariantCulture, "+000.000;-000.000")
            );
          ++iindex;
        }
        ++index;
      }

      index = 0;
      foreach (PointParam pp in PointParams) {
        w.WriteLine("point {0:X04} {1:X04} {2} {3}", index, pp.Id, 
          pp.Pos.ToString(", ", CultureInfo.InvariantCulture, "+000.000;-000.000"),
          pp.Name);
        ++index;
      }
    }

    private void ParseModelParam() {
      ModelParam mp = new ModelParam();
      mp.Name = ds.ReadStr(od.Offset + od.ReadUInt());
      mp.Type = od.ReadUInt();
      mp.LocalID = od.ReadUInt();
      mp.Filename = ds.ReadStr(od.Offset + od.ReadUInt()); // filename
      mp.InstanceCount = (int)od.ReadUInt();
      ModelParams.Add(mp);
    }

    private void ParseEventParam() {
    }

    private void ParsePointParam() {
      PointParam pp = new PointParam();
      pp.Name = ds.ReadStr(od.Offset + od.ReadUInt());
      od.ReadUInt();
      pp.Id = od.ReadUInt();
      od.ReadUInt();
      pp.Pos = od.ReadVec3();
      PointParams.Add(pp);
    }

    private void ParsePartParam() {
      InstanceParam ip = new InstanceParam();
      ip.Name = ds.ReadStr(od.Offset + od.ReadUInt());
      ip.Type = od.ReadUInt();
      ip.LocalID = od.ReadUInt();
      uint modelindex = od.ReadUInt();
      ModelParam mp = ModelParams[(int)modelindex];
      mp.InstanceParams.Add(ip);
      ip.model = mp;
      od.ReadUInt();
      ip.Translation = od.ReadVec3();
      ip.Euler = od.ReadVec3();
      ip.Scale = od.ReadVec3();
      PartParams.Add(ip);
    }

    private string GetSectionName() {
      uint nofs = od.ReadUInt();
      return ds.ReadStr(nofs);
    }

    private delegate void DetailParser();
    private bool ParseTable(DetailParser dp) {
      uint count = od.ReadUInt();
      long tableoffset = od.Offset;
      for (uint i = 0; i < count - 1; ++i) {
        uint dataoffset = ds.ReadUInt(tableoffset + i * 4);
        od.Offset = dataoffset;
        dp();
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
