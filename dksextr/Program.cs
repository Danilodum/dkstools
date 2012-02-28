// (c) 2012 vlad001

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using flver;

namespace dksextr {
  class Program {

    // extractbdf <bhf-path> <bdf-path>
    static void ExtractBDFs(string[] args) {
      BDFExtractorHelper e = new BDFExtractorHelper();
      e.ExtractBDFs(args[1], args[2], true);
    }

    static void Main(string[] args) {
      if ("extractbdf".Equals(args[0])) {
        ExtractBDFs(args);
      }
    }
  }
}
