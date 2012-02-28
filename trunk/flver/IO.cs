﻿// (c) 2012 vlad001

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using System.Collections.Specialized;
using System.Collections;

namespace flver {

  public static class HelperFns {

    public static byte[] SwapShortN(byte[] b) {
      byte[] n = new byte[b.Length];
      Array.Copy(b, n, b.Length);
      SwapShort(n);
      return n;
    }

    public static void SwapShort(byte[] b) {
      for (int i = 0; i < b.Length / 2; ++i) {
        Swap(b, i * 2, i * 2 + 1);
      }
    }

    public static void Swap<T>(T[] b, int i, int j) {
      T t = b[i];
      b[i] = b[j];
      b[j] = t;
    }

    public static void Swap<T>(ref T i, ref T j) {
      T t = i;
      i = j;
      j = t;
    }


    public static string Hexdump(byte[] b, int mod = 4) {
      StringBuilder sb = new StringBuilder();
      int x = 0;
      foreach (byte i in b) {
        sb.Append(string.Format("{0:X02}", i));
        ++x;
        if ((x % mod) == 0) sb.Append(" ");
      }
      return sb.ToString();
    }
  }

  public interface DataStream {
    uint ReadUInt(long off);
    ushort ReadUShort(long off);
    short ReadShort(long off);
    float ReadFloat(long off);
    string ReadUC(long off);
    string ReadStr(long off);
    Vec ReadVec3(long off);
    byte[] ReadBytes(long off, uint count);
    long Size { get; }
    Stream Stream { get; }
  }

  public class OffsetDataStream : DataStream {
    public DataStream Backend;
    public long Offset;

    public uint ReadUInt(long off) {
      return Backend.ReadUInt(off + Offset);
    }

    public ushort ReadUShort(long off) {
      return Backend.ReadUShort(off + Offset);
    }

    public short ReadShort(long off) {
      return Backend.ReadShort(off + Offset);
    }

    public float ReadFloat(long off) {
      return Backend.ReadFloat(off + Offset);
    }

    public string ReadUC(long off) {
      return Backend.ReadUC(off + Offset);
    }

    public string ReadStr(long off) {
      return Backend.ReadStr(off + Offset);
    }

    public Vec ReadVec3(long off) {
      return Backend.ReadVec3(off + Offset);
    }
    public byte[] ReadBytes(long off, uint count) {
      return Backend.ReadBytes(off + Offset, count);
    }

    public long Size {
      get { return Backend.Size - Offset; }
    }

    public Stream Stream {
      get { return Backend.Stream; }
    }
  }

  public class FileDataStream : DataStream {
    private FileStream s;
    public FileDataStream(string filename) {
      s = new FileStream(filename, FileMode.Open, FileAccess.Read);
    }
    private void Swap(byte[] b, int i, int j) {
      byte t = b[i];
      b[i] = b[j];
      b[j] = t;
    }

    private void Swap(byte[] b, params int[] ij) {
      for (int i = 0; i < ij.Length; i += 2) {
        Swap(b, ij[i], ij[i + 1]);
      }
    }

    public uint ReadUInt(long off, string name) {
      uint z = ReadUInt(off);
      Console.WriteLine("{0} @ {1:X08} = {2}", name, off, z);
      return z;
    }

    public uint ReadUInt(long off) {
      s.Seek(off, SeekOrigin.Begin);
      byte[] b = new byte[4];
      s.Read(b, 0, 4);
      Swap(b, 0, 3, 1, 2);
      return BitConverter.ToUInt32(b, 0);
    }

    public ushort ReadUShort(long off) {
      s.Seek(off, SeekOrigin.Begin);
      byte[] b = new byte[2];
      s.Read(b, 0, 2);
      Swap(b, 0, 1);
      return BitConverter.ToUInt16(b, 0);
    }

    public short ReadShort(long off) {
      s.Seek(off, SeekOrigin.Begin);
      byte[] b = new byte[2];
      s.Read(b, 0, 2);
      Swap(b, 0, 1);
      return BitConverter.ToInt16(b, 0);
    }

    public float ReadFloat(long off) {
      s.Seek(off, SeekOrigin.Begin);
      byte[] b = new byte[4];
      s.Read(b, 0, 4);
      Swap(b, 0, 3, 1, 2);
      return BitConverter.ToSingle(b, 0);
    }

    public string ReadUC(long off) {
      StringBuilder sb = new StringBuilder();
      uint z = ReadUShort(off);
      while (z != 0) {
        sb.Append((char)z);
        off += 2;
        z = ReadUShort(off);
      }
      return sb.ToString();
    }

    public Vec ReadVec3(long off) {
      float x, y, z;
      z = ReadFloat(off + 0);
      y = ReadFloat(off + 4);
      x = ReadFloat(off + 8);
      return new Vec(x, y, z);
    }

    public string ReadStr(long off) {
      StringBuilder sb = new StringBuilder();
      int z;
      s.Seek(off, SeekOrigin.Begin);
      while ((z = s.ReadByte()) > 0) {
        //char c = (char)((char)z & (char)0xff);
        if (z > 127) sb.AppendFormat("{0:X02}", z);
        else sb.Append((char)z);
      }
      return sb.ToString();
    }

    public byte[] ReadBytes(long off, uint count) {
      byte[] b = new byte[count];
      s.Seek(off, SeekOrigin.Begin);
      s.Read(b, 0, (int)count);
      return b;
    }

    public void Close() {
      s.Close();
    }

    public long Size {
      get { return s.Length; }
    }

    public Stream Stream {
      get { return s; }
    }
  }

  public class AutoAdvanceDataReader {
    public long Offset = 0;
    public readonly DataStream Stream;

    public AutoAdvanceDataReader(DataStream stream) {
      this.Stream = stream;
    }

    public void Skip(long bytes) {
      Offset += bytes;
    }

    public uint ReadUInt() {
      uint z = Stream.ReadUInt(Offset);
      Offset += 4;
      return z;
    }

    public ushort ReadUShort() {
      ushort z = Stream.ReadUShort(Offset);
      Offset += 2;
      return z;
    }

    public float ReadFloat() {
      float z = Stream.ReadFloat(Offset);
      Offset += 4;
      return z;
    }

    public Vec ReadVec3() {
      Vec v = Stream.ReadVec3(Offset);
      Offset += 3 * 4;
      return v;
    }

    public byte[] ReadBytes(uint count) {
      byte[] d = Stream.ReadBytes(Offset, count);
      Offset += d.Length;
      return d;
    }
  }

}

