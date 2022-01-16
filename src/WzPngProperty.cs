using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

public class WzPngProperty : WzImageProperty
{
    #region Fields
    internal int width, height, format, format2;
    internal byte[] compressedBytes;
    internal Bitmap png;
    internal WzObject parent;
    //internal WzImage imgParent;
    internal bool listWzUsed = false;

    internal WzBinaryReader wzReader;
    internal long offs;
    #endregion

    #region Inherited Members
    public override void SetValue(object value)
    {
        if (value is Bitmap) SetPNG((Bitmap)value);
        else compressedBytes = (byte[])value;
    }

    public override WzImageProperty DeepClone()
    {
        WzPngProperty clone = new WzPngProperty();
        clone.SetPNG(GetPNG(false));
        return clone;
    }

    public override object WzValue { get { return GetPNG(false); } }
    /// <summary>
    /// The parent of the object
    /// </summary>
    public override WzObject Parent { get { return parent; } internal set { parent = value; } }
    /*/// <summary>
    /// The image that this property is contained in
    /// </summary>
    public override WzImage ParentImage { get { return imgParent; } internal set { imgParent = value; } }*/
    /// <summary>
    /// The name of the property
    /// </summary>
    public override string Name { get { return "PNG"; } set { } }
    /// <summary>
    /// The WzPropertyType of the property
    /// </summary>
    public override WzPropertyType PropertyType { get { return WzPropertyType.PNG; } }
    public override void WriteValue(WzBinaryWriter writer)
    {
        throw new NotImplementedException("Cannot write a PngProperty");
    }
    /// <summary>
    /// Disposes the object
    /// </summary>
    public override void Dispose()
    {
        compressedBytes = null;
        if (png != null)
        {
            png.Dispose();
            png = null;
        }
    }
    #endregion

    #region Custom Members
    /// <summary>
    /// The width of the bitmap
    /// </summary>
    public int Width { get { return width; } set { width = value; } }
    /// <summary>
    /// The height of the bitmap
    /// </summary>
    public int Height { get { return height; } set { height = value; } }
    /// <summary>
    /// The format of the bitmap
    /// </summary>
    public int Format { get { return format + format2; } set { format = value; format2 = 0; } }

    public bool ListWzUsed { get { return listWzUsed; } set { if (value != listWzUsed) { listWzUsed = value; CompressPng(GetPNG(false)); } } }
    /// <summary>
    /// The actual bitmap
    /// </summary>
    public Bitmap PNG
    {
        set
        {
            png = value;
            CompressPng(value);
        }
    }

    [Obsolete("To enable more control over memory usage, this property was superseded by the GetCompressedBytes method and will be removed in the future")]
    public byte[] CompressedBytes
    {
        get
        {
            return GetCompressedBytes(false);
        }
    }

    /// <summary>
    /// Creates a blank WzPngProperty
    /// </summary>
    public WzPngProperty() { }
    internal WzPngProperty(WzBinaryReader reader, bool parseNow)
    {
        // Read compressed bytes
        width = reader.ReadCompressedInt();
        height = reader.ReadCompressedInt();
        format = reader.ReadCompressedInt();
        format2 = reader.ReadByte();
        reader.BaseStream.Position += 4;
        offs = reader.BaseStream.Position;
        int len = reader.ReadInt32() - 1;
        reader.BaseStream.Position += 1;

        if (len > 0)
        {
            if (parseNow)
            {
                compressedBytes = wzReader.ReadBytes(len);
                ParsePng();
            }
            else 
                reader.BaseStream.Position += len;
        }
        wzReader = reader;
    }
    #endregion

    #region Parsing Methods
    public byte[] GetCompressedBytes(bool saveInMemory)
    {
        if (compressedBytes == null)
        {
            long pos = wzReader.BaseStream.Position;
            wzReader.BaseStream.Position = offs;
            int len = wzReader.ReadInt32() - 1;
            wzReader.BaseStream.Position += 1;
            if (len > 0)
                compressedBytes = wzReader.ReadBytes(len);
            wzReader.BaseStream.Position = pos;
            if (!saveInMemory)
            {
                //were removing the referance to compressedBytes, so a backup for the ret value is needed
                byte[] returnBytes = compressedBytes;
                compressedBytes = null;
                return returnBytes;
            }
        }
        return compressedBytes;
    }

    public void SetPNG(Bitmap png)
    {
        this.png = png;
        CompressPng(png);
    }

    public Bitmap GetPNG(bool saveInMemory)
    {
        if (png == null)
        {
            long pos = wzReader.BaseStream.Position;
            wzReader.BaseStream.Position = offs;
            int len = wzReader.ReadInt32() - 1;
            wzReader.BaseStream.Position += 1;
            if (len > 0)
                compressedBytes = wzReader.ReadBytes(len);
            ParsePng();
            wzReader.BaseStream.Position = pos;
            if (!saveInMemory)
            {
                Bitmap pngImage = png;
                png = null;
                compressedBytes = null;
                return pngImage;
            }
        }
        return png;
    }

    internal byte[] Decompress(byte[] compressedBuffer, int decompressedSize)
    {
        MemoryStream memStream = new MemoryStream();
        memStream.Write(compressedBuffer, 2, compressedBuffer.Length - 2);
        byte[] buffer = new byte[decompressedSize];
        memStream.Position = 0;
        DeflateStream zip = new DeflateStream(memStream, CompressionMode.Decompress);
        zip.Read(buffer, 0, buffer.Length);
        zip.Close();
        zip.Dispose();
        memStream.Close();
        memStream.Dispose();
        return buffer;
    }
    internal byte[] Compress(byte[] decompressedBuffer)
    {
        MemoryStream memStream = new MemoryStream();
        DeflateStream zip = new DeflateStream(memStream, CompressionMode.Compress, true);
        zip.Write(decompressedBuffer, 0, decompressedBuffer.Length);
        zip.Close();
        memStream.Position = 0;
        byte[] buffer = new byte[memStream.Length + 2];
        memStream.Read(buffer, 2, buffer.Length - 2);
        memStream.Close();
        memStream.Dispose();
        zip.Dispose();
        System.Buffer.BlockCopy(new byte[] { 0x78, 0x9C }, 0, buffer, 0, 2);
        return buffer;
    }
  public static byte[] rgb565_to_argb8888(byte[] rgb565) {
    byte r5 = (byte) ((rgb565[1] & 0xf8) >> 3);
    byte g6 = (byte) (((rgb565[1] & 0x07) << 3) | ((rgb565[0] & 0xe0) >> 5));
    byte b5 = (byte) (rgb565[0] & 0x1f);

    byte[] ret = new byte[4];
    ret[0] = (byte) Math.Round(((double) 0xff / (double) 0x1f) * b5);
    ret[1] = (byte) Math.Round(((double) 0xff / (double) 0x3f) * g6);
    ret[2] = (byte) Math.Round(((double) 0xff / (double) 0x1f) * r5);
    ret[3] = 0xff;

    return ret;
  }
    internal void ParsePng()
    {
        DeflateStream zlib;
        int uncompressedSize = 0;
        int b = 0, g = 0;
        Bitmap bmp = null;
        BitmapData bmpData;
        WzImage imgParent = ParentImage;
        byte[] decBuf;

        BinaryReader reader = new BinaryReader(new MemoryStream(compressedBytes));
        ushort header = reader.ReadUInt16();
        listWzUsed = header != 0x9C78 && header != 0xDA78 && header != 0x0178 && header != 0x5E78;
        if (!listWzUsed)
        {
            zlib = new DeflateStream(reader.BaseStream, CompressionMode.Decompress);
        }
        else
        {
            reader.BaseStream.Position -= 2;
            MemoryStream dataStream = new MemoryStream();
            int blocksize = 0;
            int endOfPng = compressedBytes.Length;

            while (reader.BaseStream.Position < endOfPng)
            {
                blocksize = reader.ReadInt32();
                for (int i = 0; i < blocksize; i++)
                {
                    dataStream.WriteByte((byte)(reader.ReadByte() ^ imgParent.reader.WzKey[i]));
                }
            }
            dataStream.Position = 2;
            zlib = new DeflateStream(dataStream, CompressionMode.Decompress);
        }

        switch (format + format2)
        {
            case 1:
                bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                uncompressedSize = width * height * 2;
                decBuf = new byte[uncompressedSize];
                zlib.Read(decBuf, 0, uncompressedSize);
                byte[] argb = new Byte[uncompressedSize * 2];
                for (int i = 0; i < uncompressedSize; i++)
                {
                    b = decBuf[i] & 0x0F; b |= (b << 4); argb[i * 2] = (byte)b;
                    g = decBuf[i] & 0xF0; g |= (g >> 4); argb[i * 2 + 1] = (byte)g;
                }
                Marshal.Copy(argb, 0, bmpData.Scan0, argb.Length);
                bmp.UnlockBits(bmpData);
                break;
            case 2:
                bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                uncompressedSize = width * height * 4;
                decBuf = new byte[uncompressedSize];
                zlib.Read(decBuf, 0, uncompressedSize);
                Marshal.Copy(decBuf, 0, bmpData.Scan0, decBuf.Length);
                bmp.UnlockBits(bmpData);
                break;
            case 513:
                bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                uncompressedSize = width * height * 2;
                decBuf = new byte[uncompressedSize];
                zlib.Read(decBuf, 0, uncompressedSize);
                byte[] argbEquivalent = new byte[uncompressedSize * 2];
                for (int i = 0; i < uncompressedSize; i += 2) {
                  byte[] rgb565 = new byte[2];
                  Array.Copy(decBuf, i, rgb565, 0, 2);
                  byte[] argb8888 = rgb565_to_argb8888(rgb565);
                  Array.Copy(argb8888, 0, argbEquivalent, i*2, 4);
                }
                Marshal.Copy(argbEquivalent, 0, bmpData.Scan0, argbEquivalent.Length);
                bmp.UnlockBits(bmpData);
                break;
            case 517:
                bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                decBuf = new byte[2];
                zlib.Read(decBuf, 0, 2);
                byte[] argbEquivalent1 = new byte[width * height * 4];
                for (int i = 0; i < width * height; i += 1) {
                  byte[] argb8888 = rgb565_to_argb8888(decBuf);
                  Array.Copy(argb8888, 0, argbEquivalent1, i*4, 4);
                }
                Marshal.Copy(argbEquivalent1, 0, bmpData.Scan0, argbEquivalent1.Length);
                bmp.UnlockBits(bmpData);
                break;
            default:
                Console.WriteLine(string.Format("Unknown PNG format {0} {1}", format, format2));
                break;
        }
        png = bmp;
    }
    internal void CompressPng(Bitmap bmp)
    {
        byte[] buf = new byte[bmp.Width * bmp.Height * 8];
        format = 2;
        format2 = 0;
        width = bmp.Width;
        height = bmp.Height;

        int curPos = 0;
        for (int i = 0; i < height; i++)
            for (int j = 0; j < width; j++)
            {
                Color curPixel = bmp.GetPixel(j, i);
                buf[curPos] = curPixel.B;
                buf[curPos + 1] = curPixel.G;
                buf[curPos + 2] = curPixel.R;
                buf[curPos + 3] = curPixel.A;
                curPos += 4;
            }
        compressedBytes = Compress(buf);
        if (listWzUsed)
        {
            MemoryStream memStream = new MemoryStream();
            WzBinaryWriter writer = new WzBinaryWriter(memStream, WzTool.GetIvByMapleVersion(WzMapleVersion.GMS));
            writer.Write(2);
            for (int i = 0; i < 2; i++)
            {
                writer.Write((byte)(compressedBytes[i] ^ writer.WzKey[i]));
            }
            writer.Write(compressedBytes.Length - 2);
            for (int i = 2; i < compressedBytes.Length; i++)
                writer.Write((byte)(compressedBytes[i] ^ writer.WzKey[i - 2]));
            compressedBytes = memStream.GetBuffer();
            writer.Close();
        }
    }
    #endregion

    #region Cast Values

    public override Bitmap GetBitmap()
    {
        return GetPNG(false);
    }
    #endregion
}
