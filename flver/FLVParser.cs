﻿// (c) 2012 vlad001

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace flver {

  public class Mesh {
    public readonly List<MeshFaceSet> FaceSets = new List<MeshFaceSet>();

    // number of vertices in VertexBuffer
    public uint NVertices = 0;

    // size of one vertex+attribs
    public uint VertexSize = 0;

    // offset of vertex buffer
    public uint VertexBufferOfs = 0;

    // size of vertex buffer in bytes
    public uint VertexBufferSize = 0;

    // index of stream descriptor
    public uint VertexDescriptorIndex = 0;

    // vertex buffer
    public Vec[] VertexBuffer;

    // stride of vertex attributes
    public uint VertexAttribStride;

    // vertex attributes [ v0a0 v0a1 ... v0an v1a0 v1a1 ... v1an v2a0 ... ]
    public ushort[] VertexAttribs;

    // attrib buffer semantic -> uv attrib buffer
    // it seems there are 
    public readonly List<UVVertexAttribBuffer> UVVertexAttribBuffers = new List<UVVertexAttribBuffer>();

    // attrib buffer semantic -> argb attrib buffer
    // so far 4 semantic values were seen:
    //  0x03 : seems to be vertex normal information in rgb-part
    //  0x06, 0x07, 0x0A: color(rgb) + texture blending(a) data
    //                    0x0A seems to be present always, so it may be tied to the first diffuse texture
    public readonly Dictionary<uint, ARGBVertexAttribBuffer> ARGBVertexAttribBuffers = new Dictionary<uint, ARGBVertexAttribBuffer>();

    public BoneWeightVertexAttribBuffer BoneWeightVertexAttribBuffer;

    public uint Unknown0;
    public uint Unknown1;
    public uint Unknown2;

    // Mostly unknown partinfo
    // [0]   ... unknown
    // [1]   ... MaterialIndex
    // [2,3] ... unknown but mostly 0
    // [4]   ... unknown but seldom 0
    // [5]   ... unknown, often 1
    // [6]   ... unknown
    public uint[] Partinfo = new uint[7];

    //public Mat mat = new Mat();

    // copied from Partinfo[1]
    public uint MaterialIndex;
    public Material Material;

    public string Diffuse1TexName;
    public string Diffuse2TexName;
    public string LightmapTexName;
    public string Bumpmap1TexName;
    public string Bumpmap2TexName;

    public string GetConfigString() {
      return string.Format("[{0}{2}{1}{3}{4}]",
        Diffuse1TexName != null ? "D1" : "  ",
        Diffuse2TexName != null ? "D2" : "  ",
        Bumpmap1TexName != null ? "B1" : "  ",
        Bumpmap2TexName != null ? "B2" : "  ",
        LightmapTexName != null ? "L" : " ");
    }

    public void Dump(TextWriter w) {
      w.Write("meshinfo {0} {1:X08} {2:X08} {3:X08} sz={6:X02} argb#={4} uv#={5} ", GetConfigString(),
        Unknown0, Unknown1, Unknown2,
        ARGBVertexAttribBuffers.Count,
        UVVertexAttribBuffers.Count,
        VertexSize
        );
      HelperFns.DumpUInts(Partinfo, w);
      w.WriteLine();
    }
  }

  public class MeshFaceSet {
    public uint Unknown0;
    public uint NIndices;
    public uint IndexBufferOffset;
    public uint[] Indices;
    public uint Unknown1;

    public void Dump(TextWriter w) {
      w.WriteLine("faceset {0:X08} {1:X08} {2} indices", Unknown0, Unknown1, NIndices);
    }
  }

  public class Material {
    public string Name1;
    public string MTDName;
    public uint NParams;
    public uint ParamStartIndex;
    public uint Unknown0;
    public Dictionary<string, MaterialParameter> Params = new Dictionary<string, MaterialParameter>();

    public void Dump(TextWriter w) {
      w.WriteLine("material {0:X08} {1}", Unknown0, MTDName);
    }
  }

  public class MaterialParameter {
    public string Value;
    public string Name;
    public float Unknown0, Unknown1;
    public uint Unknown2, Unknown3;

    public void Dump(TextWriter w) {
      w.WriteLine("matparam {0:000.000} {1:000.000} {2:X08} {3:X08} {4}={5}", Unknown0, Unknown1, Unknown2, Unknown3, Name, Value);
    }
  }

  public class Part {
    public string Name;
    public Vec Translation;
    public Vec Euler, Scale;
    public Vec BBLower, BBUpper;
    public uint Unknown0;
    public uint Unknown1;
    public uint Unknown2;

    public void Dump(TextWriter w) {
      w.WriteLine("part {0:X08} {1:X08} {2:X08} [t={3}, r={4}, s={5} {7} {8}] {6}", 
        Unknown0, Unknown1, Unknown2, Translation, Euler, Scale, Name, BBLower, BBUpper);
    }
  }

  public class Bone {
    public uint[] Data;

    public void Dump(TextWriter w) {
      HelperFns.DumpUInts(Data, w);
      w.WriteLine();
    }
  }

  public class VertexStreamDescriptor {
    public uint Unknown0;
    public uint Offset;
    public uint Datatype;
    public uint Semantic;
    public uint Index;
  }

  public class VertexDescriptor {
    public uint NStreamDescriptors;
    public uint Offset;
    public List<VertexStreamDescriptor> StreamDescriptors = new List<VertexStreamDescriptor>();
  }

  abstract public class VertexAttribBuffer {
    protected readonly ushort[] vertexAttribs;
    protected readonly uint vertexAttribStride;
    protected readonly uint offset;

    protected VertexAttribBuffer(ushort[] vertexAttribs, uint vertexAttribStride, uint offset) {
      this.vertexAttribs = vertexAttribs;
      this.vertexAttribStride = vertexAttribStride;
      this.offset = offset;
    }

    public ushort GetUShort(uint vertexIndex, uint offset) {
      return vertexAttribs[vertexIndex * vertexAttribStride + this.offset + offset];
    }
    public short GetShort(uint vertexIndex, uint offset) {
      return (short)GetUShort(vertexIndex, offset);
    }
  }

  public class UVVertexAttribBuffer : VertexAttribBuffer {
    private const float uvDiv = 1024.0f;
    public UVVertexAttribBuffer(ushort[] vertexAttribs, uint vertexAttribStride, uint offset)
      : base(vertexAttribs, vertexAttribStride,offset) {
    }

    public void GetUV(uint vertexIndex, out float u, out float v) {
      u = (float)GetShort(vertexIndex, 0) / uvDiv;
      v = (float)GetShort(vertexIndex, 1) / uvDiv;
    }
  }

  public class ARGBVertexAttribBuffer : VertexAttribBuffer {
    public readonly uint Semantic;
    public ARGBVertexAttribBuffer(ushort[] vertexAttribs, uint vertexAttribStride, uint offset, uint semantic)
      : base(vertexAttribs, vertexAttribStride, offset) {
        this.Semantic = semantic;
    }

    public void GetARGB(uint vertexIndex, out byte a, out byte r, out byte g, out byte b) {
      uint ar = GetUShort(vertexIndex, 0);
      uint gb = GetUShort(vertexIndex, 1);
      a = (byte)(ar >> 8);
      r = (byte)(ar & 0xff);
      g = (byte)(gb >> 8);
      b = (byte)(gb & 0xff);
    }
  }

  public class BoneWeightVertexAttribBuffer : VertexAttribBuffer {
    public readonly uint Semantic;
    public BoneWeightVertexAttribBuffer(ushort[] vertexAttribs, uint vertexAttribStride, uint offset, uint semantic)
      : base(vertexAttribs, vertexAttribStride, offset) {
      this.Semantic = semantic;
    }

    public void GetWeights(uint vertexIndex, out ushort b0, out ushort b1) {
      b0 = GetUShort(vertexIndex, 0);
      b1 = GetUShort(vertexIndex, 1);
    }
  }

  public class FLVERParser {
    private DataStream d;
    private AutoAdvanceDataReader od;
    private uint dataOffset;
    public bool LoadData = true;

    public List<Bone> Bones = new List<Bone>();
    public List<Mesh> Meshes = new List<Mesh>();
    public List<Part> Parts = new List<Part>();
    public List<VertexDescriptor> VertexDescriptors = new List<VertexDescriptor>();
    public List<MaterialParameter> MaterialParameters = new List<MaterialParameter>();
    public List<Material> Materials = new List<Material>();

    public FLVERParser(DataStream d) {
      this.d = d;
      this.od = new AutoAdvanceDataReader(d);
    }

    public void Dump(TextWriter w) {
      int i;

      i = 0;
      foreach (Bone bone in Bones) {
        w.Write("bone{0:X02} ", i++);
        bone.Dump(w);
      }

      i = 0;
      foreach (Part part in Parts) {
        w.Write("part{0:X02} ", i++);
        part.Dump(w);
      }

      i = 0;
      foreach (Mesh mesh in Meshes) {
        w.Write("mesh{0:X02} ", i++);
        mesh.Dump(w);
      }

      i = 0;
      foreach (Mesh mesh in Meshes) {
        w.Write("mesh{0:X02} ", i++);
        mesh.Material.Dump(w);
      }

      i = 0;
      foreach (Mesh mesh in Meshes) {
        foreach (MeshFaceSet fs in mesh.FaceSets) {
          w.Write("mesh{0:X02} ", i);
          fs.Dump(w);
        }
        ++i;
      }

      i = 0;
      foreach (Mesh mesh in Meshes) {
        foreach (MaterialParameter mp in mesh.Material.Params.Values) {
          w.Write("mesh{0:X02} ", i);
          mp.Dump(w);
        }
        ++i;
      }

      uint[] bacount = new uint[255];
      i = 0;
      foreach (Mesh mesh in Meshes) {
        for (uint v = 0; v < mesh.NVertices; ++v) {
          ushort w0, w1;
          mesh.BoneWeightVertexAttribBuffer.GetWeights(v, out w0, out w1);

          bacount[w0 & 0xFF]++;
          bacount[w0 >> 8]++;
          bacount[w1 & 0xFF]++;
          bacount[w1 >> 8]++;
        }

        w.Write("mesh{0:X02} boneasn ", i);
        for (int j = 0; j < bacount.Length; ++j) {
          if (bacount[j] > 0) {
            w.Write("{0:X02}:{1}  ", j, bacount[j]);
          }
        }
        w.WriteLine();
        ++i;
      }
    }

    public static FLVERParser ParseFLVER(string s, bool loaddata = true) {
      FileDataStream fds = new FileDataStream(s);
      FLVERParser p = new FLVERParser(fds);
      p.LoadData = loaddata;
      p.Parse();
      fds.Close();
      return p;
    }

    private void AddNItems<T>(List<T> list, uint n) where T : new() {
      for (int i = 0; i < n; ++i) {
        list.Add(new T());
      }
    }

    private uint[] dummy = new uint[100];
    private void ReadUInts(string name, int n, uint[] skipped = null) {
      bool dbg = false;
      for (uint i = 0; i < n; ++i) {
        uint a = od.ReadUInt();
        if (skipped != null) {
          skipped[i] = a;
        }
        else if (a != 0) {
          if (!dbg) {
            Debug.Write(name);
          }
          Debug.Print(" {0:X08}", a);
          dbg = true;
        }
      }

    }

    public void ParseHeader() {
      od.Skip(6 + 3 * 2);
      dataOffset = od.ReadUInt();
      od.ReadUInt(); // data size
      uint nbones = od.ReadUInt();
      uint nmaterials = od.ReadUInt();
      uint nparts = od.ReadUInt();
      uint nunknown1 = od.ReadUInt();
      uint nmesh = od.ReadUInt();

      // bounding box?
      Vec bba = od.ReadVec3();
      Vec bbb = od.ReadVec3();

      od.Skip(4 * 4);

      uint nunknown0 = od.ReadUInt();
      uint nvertdescs = od.ReadUInt();
      uint nmatparams = od.ReadUInt();

      for (uint i = 0; i < nbones; ++i) Bones.Add(new Bone());
      for (uint i = 0; i < nmesh; ++i) Meshes.Add(new Mesh());
      for (uint i = 0; i < nmaterials; ++i) Materials.Add(new Material());
      for (uint i = 0; i < nparts; ++i) Parts.Add(new Part());
      for (uint i = 0; i < nvertdescs; ++i) VertexDescriptors.Add(new VertexDescriptor());
      for (uint i = 0; i < nmatparams; ++i) MaterialParameters.Add(new MaterialParameter());

      od.Offset = 0x80;

      ParseBones(nbones);
      ParseMaterials();
      ParseParts(nparts);
      ParseMeshInfo2();
      ParseFaceInfo();
      ParseVertexInfo();

      ParseVertexDescriptors();
      ParseMaterialParameters();
    }

    public void ParseData() {
      ParseFaces();
      ParseVertices();

      UpdateMaterials();
      UpdateMeshes();      
    }

    public void Parse() {
      ParseHeader();
      ParseData();
    }

    private void UpdateMaterials() {
      foreach (Material mat in Materials) {
        for (uint i = 0; i < mat.NParams; ++i) {
          MaterialParameter u = MaterialParameters[(int)(i + mat.ParamStartIndex)];
          mat.Params[u.Name] = u;
        }
      }
    }

    private string Texname(string s) {
      if (s == null) return null;
      return Path.GetFileNameWithoutExtension(s);
    }

    private string MatParam(Material mat, string pn) {
      MaterialParameter p = null;
      if (!mat.Params.TryGetValue(pn, out p)) {
        return null;
      }
      else {
        return p.Value;
      }
    }

    private void UpdateMeshes() {
      foreach (Mesh m in Meshes) {

        Material mat = Materials[(int)m.MaterialIndex];
        m.Material = mat;

        m.Diffuse1TexName = Texname(MatParam(mat, "g_Diffuse"));
        m.Diffuse2TexName = Texname(MatParam(mat, "g_Diffuse_2"));

        m.Bumpmap1TexName = Texname(MatParam(mat, "g_Bumpmap"));
        m.Bumpmap2TexName = Texname(MatParam(mat, "g_Bumpmap_2"));
        m.LightmapTexName = Texname(MatParam(mat, "g_Lightmap"));

        // todo: the following is not very smart ...
        //       could also use ub.Offset-0x0c directly

        int ofs = 0;
        VertexDescriptor b = VertexDescriptors[(int)m.VertexDescriptorIndex];
        foreach (VertexStreamDescriptor ub in b.StreamDescriptors) {
          int len = 0;
          // assert ofs == ub.Offset-0xc, except for ub.Datatype==0x02

          switch (ub.Datatype) {              
            case 0x02:  // position attribute; already in VertexBuffer, ignore
              break;

            case 0x11:  // bone assignments?
              m.BoneWeightVertexAttribBuffer = new BoneWeightVertexAttribBuffer(m.VertexAttribs, m.VertexAttribStride, (uint)ofs, ub.Semantic);
              len = 2;
              break;

            case 0x13:  // argb buffer
              ARGBVertexAttribBuffer argbvab = new ARGBVertexAttribBuffer(m.VertexAttribs, m.VertexAttribStride, (uint)ofs, ub.Semantic);
              m.ARGBVertexAttribBuffers[ub.Semantic] = argbvab;
              len = 2;
              break;

            case 0x15:  // uv buffer
              UVVertexAttribBuffer uvvab = new UVVertexAttribBuffer(m.VertexAttribs, m.VertexAttribStride, (uint)ofs);
              m.UVVertexAttribBuffers.Add(uvvab);
              len = 2;
              break;

            case 0x16:  // two uv buffers
              UVVertexAttribBuffer uv0vab = new UVVertexAttribBuffer(m.VertexAttribs, m.VertexAttribStride, (uint)ofs);
              m.UVVertexAttribBuffers.Add(uv0vab);
              UVVertexAttribBuffer uv1vab = new UVVertexAttribBuffer(m.VertexAttribs, m.VertexAttribStride, (uint)ofs+2);
              m.UVVertexAttribBuffers.Add(uv1vab);
              len = 4;
              break;

            case 0x1a:
              // unknown
              len = 4;
              break;

            default:
              Debug.WriteLine("unknown type: {0:X02}", ub.Semantic);
              break;
          }
          ofs += len;
        }
      }
    }

    private void ParseMaterialParameters() {
      foreach (MaterialParameter ub in MaterialParameters) {
        ub.Value = d.ReadUC(od.ReadUInt());
        ub.Name = d.ReadUC(od.ReadUInt());
        ub.Unknown0 = od.ReadFloat();
        ub.Unknown1 = od.ReadFloat();
        ub.Unknown2 = od.ReadUInt();
        od.Skip(3 * 4);
      }
    }

    private void ParseVertexDescriptors() {
      foreach (VertexDescriptor ub in VertexDescriptors) {
        ub.NStreamDescriptors = od.ReadUInt();
        od.Skip(2 * 4);
        ub.Offset = od.ReadUInt();

        for (uint i = 0; i < ub.NStreamDescriptors; ++i) {
          VertexStreamDescriptor b = new VertexStreamDescriptor();
          long ofs = ub.Offset + i * 5 * 4;
          b.Unknown0 = d.ReadUInt(ofs + 0);
          b.Offset = d.ReadUInt(ofs + 4);
          b.Datatype = d.ReadUInt(ofs + 8);
          b.Semantic = d.ReadUInt(ofs + 12);
          b.Index = d.ReadUInt(ofs + 16);
          ub.StreamDescriptors.Add(b);
        }
      }
    }

    private void ParseBones(uint nbones) {
      // todo
      foreach (Bone b in Bones) {
        b.Data = new uint[16];
        for (int i = 0; i < b.Data.Length; ++i) {
          b.Data[i] = od.ReadUInt();
        }
      }
      //od.Skip(nbones * 64);
    }

    private void ParseMaterials() {
      foreach (Material mat in Materials) {
        mat.Name1 = d.ReadUC(od.ReadUInt());
        mat.MTDName = d.ReadUC(od.ReadUInt());
        mat.NParams = od.ReadUInt();
        mat.ParamStartIndex = od.ReadUInt();
        mat.Unknown0 = od.ReadUInt();
        ReadUInts("mat", 3); //od.Skip(3 * 4);
      }
    }

    private void ParseParts(uint count) {
      foreach (Part p in Parts) {
        p.Translation = od.ReadVec3(); 
        p.Name = d.ReadUC(od.ReadUInt());
        p.Euler = od.ReadVec3();
        p.Unknown0 = od.ReadUInt();
        p.Scale = od.ReadVec3();
        p.Unknown1 = od.ReadUInt();
        p.BBLower = od.ReadVec3();
        p.Unknown2 = od.ReadUInt();
        p.BBUpper = od.ReadVec3();

        ReadUInts("parts5", 1); //od.Skip(4);
        ReadUInts("parts6", (128 - 20 * 4) / 4); //od.Skip(128 - 20 * 4);
      }
    }

    private void ParseMeshInfo2() {
      foreach (Mesh mesh in Meshes) {
        ReadUInts("partinfo", 7, mesh.Partinfo);
        mesh.MaterialIndex = mesh.Partinfo[1];
        mesh.Unknown0 = d.ReadUInt(od.ReadUInt());
        uint nfacesets = od.ReadUInt();
        AddNItems(mesh.FaceSets, nfacesets);
        mesh.Unknown1 = d.ReadUInt(od.ReadUInt());
        od.ReadUInt();
        mesh.Unknown2 = d.ReadUInt(od.ReadUInt());
      }
    }

    private void ParseFaceInfo() {
      int mi = 0;
      foreach (Mesh mesh in Meshes) {
        foreach (MeshFaceSet fs in mesh.FaceSets) {
          //for (uint i = 0; i < mesh.NFaceGroups; ++i) {
          fs.Unknown0 = od.ReadUInt();
          fs.Unknown1 = od.ReadUInt();
          fs.NIndices = od.ReadUInt();
          fs.IndexBufferOffset = od.ReadUInt() + dataOffset;
          uint nindices2 = od.ReadUInt();
          od.Skip(3 * 4);
        }
        ++mi;
      }
    }

    private void ParseVertexInfo() {
      foreach (Mesh mesh in Meshes) {
        ReadUInts("vertexinfo1", 1);
        mesh.VertexDescriptorIndex = od.ReadUInt();

        mesh.VertexSize = od.ReadUInt();
        mesh.NVertices = od.ReadUInt();

        ReadUInts("vertexinfo2", 2);

        mesh.VertexBufferSize = od.ReadUInt();
        mesh.VertexBufferOfs = od.ReadUInt() + dataOffset;
      }
    }

    private void ParseFaces() {
      if (!LoadData) return;

      foreach (Mesh mesh in Meshes) {
        foreach (MeshFaceSet fs in mesh.FaceSets) {
          uint n = fs.NIndices;
          uint ofs = fs.IndexBufferOffset;
          fs.Indices = new uint[n];
          for (uint i = 0; i < n; ++i) {
            fs.Indices[i] = d.ReadUShort(ofs + i * 2);
          }
        }
      }
    }

    private void ParseVertices() {
      if (!LoadData) return;

      foreach (Mesh mesh in Meshes) {
        long ofs = mesh.VertexBufferOfs;
        mesh.VertexBuffer = new Vec[mesh.NVertices];
        mesh.VertexAttribStride = (mesh.VertexSize - 12) / 2;
        mesh.VertexAttribs = new ushort[mesh.NVertices * mesh.VertexAttribStride];

        uint nv = mesh.NVertices;
        for (uint i = 0; i < nv; ++i) {
          mesh.VertexBuffer[i] = d.ReadVec3(ofs);

          for (uint ii = 0; ii < mesh.VertexAttribStride; ++ii) {
            ushort u = d.ReadUShort(ofs + 2 * ii + 12);
            mesh.VertexAttribs[i * mesh.VertexAttribStride + ii] = u;
          }

          ofs += mesh.VertexSize;
        }
      }
    }
  }
}
