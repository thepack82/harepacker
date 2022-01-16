using System;
using System.IO;
using System.Security.Cryptography;

public class AESEncryption
{

  /// <summary>
  /// Encrypt data using MapleStory's AES algorithm
  /// </summary>
  /// <param name="IV">IV to use for encryption</param>
  /// <param name="data">Data to encrypt</param>
  /// <param name="length">Length of data</param>
  /// <returns>Crypted data</returns>
  public static byte[] aesCrypt(byte[] IV, byte[] data, int length)
  {
    return aesCrypt(IV, data, length, CryptoConstants.getTrimmedUserKey());
  }

  /// <summary>
  /// Encrypt data using MapleStory's AES method
  /// </summary>
  /// <param name="IV">IV to use for encryption</param>
  /// <param name="data">data to encrypt</param>
  /// <param name="length">length of data</param>
  /// <param name="key">the AES key to use</param>
  /// <returns>Crypted data</returns>
  public static byte[] aesCrypt(byte[] IV, byte[] data, int length, byte[] key)
  {
    AesManaged crypto = new AesManaged();
    crypto.KeySize = 256; //in bits
    crypto.Key = key;
    crypto.Mode = CipherMode.ECB; // Should be OFB, but this works too

    MemoryStream memStream = new MemoryStream();
    CryptoStream cryptoStream = new CryptoStream(memStream, crypto.CreateEncryptor(), CryptoStreamMode.Write);

    int remaining = length;
    int llength = 0x5B0;
    int start = 0;
    while (remaining > 0)
    {
      byte[] myIV = MapleCrypto.multiplyBytes(IV, 4, 4);
      if (remaining < llength)
      {
        llength = remaining;
      }
      for (int x = start; x < (start + llength); x++)
      {
        if ((x - start) % myIV.Length == 0)
        {
          cryptoStream.Write(myIV, 0, myIV.Length);
          byte[] newIV = memStream.ToArray();
          Array.Copy(newIV, myIV, myIV.Length);
          memStream.Position = 0;
        }
        data[x] ^= myIV[(x - start) % myIV.Length];
      }
      start += llength;
      remaining -= llength;
      llength = 0x5B4;
    }

    try
    {
      cryptoStream.Dispose();
      memStream.Dispose();
    }
    catch (Exception e)
    {
      Console.WriteLine("Error disposing AES streams" + e);
    }

    return data;
  }
}
