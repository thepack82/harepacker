using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

public class Program {

  public static void setIv(WzDirectory dir, byte[] iv) {
    dir.WzIv = iv;
    foreach (WzDirectory subDir in dir.WzDirectories) {
      setIv(subDir, iv);
    }
  }

  public static void Main() {
    string[] files = new string[] {
      "TamingMob.wz",
      "Base.wz",
      "Etc.wz",
      "String.wz",
      "Quest.wz",
      "Morph.wz",
      "Effect.wz",
      "Item.wz",
      "UI.wz",
      "Reactor.wz",
      "Npc.wz",
      "Skill.wz",
      "Character.wz",
      "Mob.wz",
      "Sound.wz",
      "Map.wz",
    };

    // convert all wz files to xml dumps first
    foreach (string f in files) {
      WzFile w = new WzFile("MapleStory/" + f, WzMapleVersion.GMS);
      w.ParseWzFile();

      Directory.CreateDirectory("xml");

      WzNewXmlSerializer serializer = new WzNewXmlSerializer(2, LineBreak.Unix);
      serializer.ExportCombinedXml(new List<WzObject>{w.WzDirectory}, "xml/" + w.Name);
    }

    // then convert xml dumps to wz files
    foreach (string f in files) {
      // WzFile seems like the only way to produce encrypted .wz files
      WzFile wzf = new WzFile("MapleStory/" + f, WzMapleVersion.GMS);
      wzf.ParseWzFile();
      WzXmlDeserializer deserializer = new WzXmlDeserializer(false, CryptoConstants.WZ_GMSIV);
      List<WzObject> deserialized = deserializer.ParseXML("xml/" + f + ".xml");
      WzDirectory wzd = (WzDirectory)deserialized[0];

      // prevent null pointer exception
      setIv(wzd, wzf.WzDirectory.WzIv);

      // overwrite original WzDirectory with the one read from XML dump
      wzf.WzDirectory = wzd;

      // create .wz file.
      Directory.CreateDirectory("compiled_wz");
      wzf.SaveToDisk("compiled_wz/" + f);
    }
  }
}
