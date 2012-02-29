// (c) 2012 vlad001

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using flver;
using System.IO;
using System.Globalization;

namespace dksextr {
  class Program {

    // extractbdf <bhf-path> <bdf-path>
    static void ExtractBDFs(string[] args) {
      BDFExtractorHelper e = new BDFExtractorHelper();
      e.ExtractBDFs(args[1], args[2], true);
    }

    static void DumpFLV(string[] args) {
      foreach (string arg in args.Skip(1)) {
        FLVERParser p = FLVERParser.ParseFLVER(arg, true);
        TextWriter w = Console.Out;
        int im = -1;
        w.WriteLine("flv {0}", arg);
        w.WriteLine("  parts = " + p.Parts);
        foreach (Mesh mesh in p.Meshes) {
          ++im;
          w.WriteLine("mesh {0}, {1} face-sets, tex-config: {2}", im, mesh.FaceSets.Count, mesh.GetConfigString());

          for(uint index=0; index<mesh.VertexBuffer.Length; ++index) {
            w.Write("  vertex {0}", mesh.VertexBuffer[index].ToString(CultureInfo.InvariantCulture, "+000.000;-000.000"));
            foreach (UVVertexAttribBuffer uv in mesh.UVVertexAttribBuffers) {
              float u, v;
              uv.GetUV(index, out u, out v);
              w.Write(" uv={0:+00.000;-00.000},{1:+00.000;-00.000}", u, v);
            }
            foreach (ARGBVertexAttribBuffer argb in mesh.ARGBVertexAttribBuffers.Values) {
              byte a, r, g, b;
              argb.GetARGB(index, out a, out r, out g, out b);
              w.Write(" argb[{0:X02}]={1:X02},{2:X02},{3:X02},{4:X02}", argb.Semantic, a, r, g, b);
            }
            w.WriteLine();
          }
        }
      }
    }

    static void Main(string[] args) {
      if ("extractbdf".Equals(args[0])) {
        ExtractBDFs(args);
      }
      else if ("dumpflv".Equals(args[0])) {
        DumpFLV(args);
      }
    }
  }
}
