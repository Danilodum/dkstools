// (c) 2012 vlad001

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ComponentAce.Compression.Libs.zlib;

namespace flver {

  public class BDFExtractorHelper {
    public TextWriter w = Console.Out;

    // database for matching bhf's to bdf's
    public readonly BDFDB bdfdb = new BDFDB();

    public BHFParser ParseBHF(string s) {
      FileDataStream fds = new FileDataStream(s);
      BHFParser p = new BHFParser(Path.GetFileNameWithoutExtension(s), fds);
      p.DB = bdfdb;
      p.w = w;
      p.Parse();
      fds.Close();
      return p;
    }

    public void ParseBDF(string s, bool extract = true) {
      FileDataStream fds = new FileDataStream(s);
      BDFParser p = new BDFParser(Path.GetFileNameWithoutExtension(s), fds);
      p.DB = bdfdb;
      p.w = w;
      p.Extract = extract;
      p.Parse();
      fds.Close();
    }

    public void ExtractBDFs(string bhfpath, string bdfpath, bool extract = true) {
      w.WriteLine("parsing bhfs...");
      foreach (string s in Directory.EnumerateFiles(bhfpath, "*.bhf", SearchOption.TopDirectoryOnly)) {
        w.WriteLine(s);
        ParseBHF(s);
      }

      w.WriteLine("parsing bdfs...");
      foreach (string s in Directory.EnumerateFiles(bdfpath, "*.bdf", SearchOption.TopDirectoryOnly)) {
        w.WriteLine(s);
        ParseBDF(s, extract);
      }
    }
  }

  public class BDFEntry {
    public string Name;
    public uint Size;
    public uint ZSizeBHF;
    public uint ZSizeBDF;
    public int Index;
    public uint Offset;
  }

  public class BDF {
    public string Filename;
    public readonly List<BDFEntry> Entries = new List<BDFEntry>();
  }

  public class BDFDB {
    public readonly List<BDF> BDFs = new List<BDF>();
  }

  public class BHFParser {
    private string name;
    public TextWriter w = Console.Out;
    public BDFDB DB;
    public BDF bdf;

    private DataStream ds;

    public BHFParser(string name, DataStream ds) {
      this.ds = ds;
      this.name = name;
    }

    public void Parse() {
      uint count = ds.ReadUInt(4 * 4);
      bdf = new BDF();
      bdf.Filename = name;
      for (uint i = 0; i < count; ++i) {
        BDFEntry be = new BDFEntry();
        be.Index = (int)i;
        long baseofs = 0x20 + i * 0x18;
        uint nofs = ds.ReadUInt(baseofs + 4 * 4);
        be.Name = ds.ReadStr(nofs);
        be.ZSizeBHF = ds.ReadUInt(baseofs + 1 * 4);
        be.Offset = ds.ReadUInt(baseofs + 2 * 4);
        bdf.Entries.Add(be);
      }
      DB.BDFs.Add(bdf);
    }
  }

  public class BDFParser {
    public TextWriter w = Console.Out;
    public BDFDB DB;
    public bool Extract = false;
    private static byte[] fcbuffer = new byte[0x10000];
    private string name;
    private DataStream ds;

    public BDFParser(string name, DataStream ds) {
      this.ds = ds;
      this.name = name;
    }

    public void Parse() {
      string outdir = Path.GetFullPath("bdfout__");
      long off = 0;
      int index = 0;

      List<BDFEntry> bes = new List<BDFEntry>();

      while (off < ds.Size) {
        uint dcx = ds.ReadUInt(off);
        if (dcx == 0x44435800) {
          // dcx header found

          BDFEntry be = new BDFEntry();
          be.Index = index;
          be.Offset = (uint)off;
          be.Size = ds.ReadUInt(off + 7 * 4);
          be.ZSizeBDF = ds.ReadUInt(off + 8 * 4);
          be.Name = "__undefined__" + name + "_" + index;
          bes.Add(be);

          ++index;
          off += be.ZSizeBDF;

          // next dcx starts at 16-byte aligned offset
          off &= 0xFFFFFFF0;
        }
        else {
          // next dcx starts at 16-byte aligned offset
          off += 16;
        }
      }

      if (DB != null) {
        // try to find BDF heuristically by matching number of entries and offsets
        BDF cand = null;
        foreach (BDF bdf in DB.BDFs.Where(b => b.Entries.Count == bes.Count)) {
          bool ok = true;
          for (int i = 0; ok && i < bes.Count; ++i) {
            if (bdf.Entries[i].Offset != bes[i].Offset) {
              ok = false;
            }
            bdf.Entries[i].Size = bes[i].Size;
            bdf.Entries[i].ZSizeBDF = bes[i].ZSizeBDF;
          }
          if (ok) {
            cand = bdf;
            break;
          }
        }

        if (cand != null) {
          bes = cand.Entries;
          w.WriteLine("found match");
        }
      }

      foreach (BDFEntry be in bes) {
        Stream s = ds.Stream;
        try {
          s.Seek(be.Offset + 0x4c, SeekOrigin.Begin);
          ZInputStream zis = new ZInputStream(ds.Stream);

          int hdrsz = 0;

          string dest = be.Name;
          if (!be.Name.Contains('.')) {

            // decompress/read first 3 bytes from file for guessing extension
            hdrsz = zis.Read(fcbuffer, 0, 3);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hdrsz; ++i) {
              if (Char.IsLetterOrDigit((char)fcbuffer[i])) {
                if (sb.Length == 0) sb.Append(".");
                sb.Append((char)fcbuffer[i]);
              }
            }
            dest = dest + sb.ToString();
          }
          else if (dest.EndsWith(".dcx")) {
            dest = dest.Substring(0, dest.Length - 4);
          }

          w.WriteLine("{0,-16} {1,4} {2:X08} size={3:X08} zsbhf={4:X08} zsbdf={5:X08} ({6:X02}) {7} {8}",
            name, be.Index, be.Offset, be.Size, be.ZSizeBHF, be.ZSizeBDF, be.ZSizeBHF - be.ZSizeBDF, be.Name, dest);

          if (Extract) {

            if (dest.StartsWith("\\")) dest = dest.Substring(1);
            string filename1 = Path.Combine(outdir, dest);
            string dirname = Path.GetDirectoryName(filename1);
            if (!Directory.Exists(dirname)) {
              Directory.CreateDirectory(dirname);
            }

            int tryn=0;
            string filename = filename1;
            while (File.Exists(filename)) {
              filename = filename1 + string.Format("_ovr-{0:00}", tryn++);
            }

            using (FileStream fs = new FileStream(filename, FileMode.CreateNew, FileAccess.Write)) {
              int r = 0;
              // write first 3 bytes 
              fs.Write(fcbuffer, 0, hdrsz);
              int sz = hdrsz;
              do {
                r = zis.Read(fcbuffer, 0, fcbuffer.Length);
                sz += r;

                if (sz > be.Size) {
                  r -= sz - (int)be.Size;
                  fs.Write(fcbuffer, 0, r);
                  r = 0;
                }
                else if (r > 0) {
                  fs.Write(fcbuffer, 0, r);
                }
              } while (r > 0);
              if (sz == 0) {
                Console.WriteLine("zero size");
              }
            }

          }
        }
        catch (Exception ex) {
          Console.WriteLine(ex);
        }
      }
    }
  }
}

