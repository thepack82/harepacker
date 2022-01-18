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

  public static void setListWzUsed(WzImageProperty imgProp, HashSet<string> fullPathsOfListWzPngs) {
    if (imgProp is WzCanvasProperty) {
      WzCanvasProperty canvasProp = (WzCanvasProperty)imgProp;
      if (fullPathsOfListWzPngs.Contains(canvasProp.PngProperty.FullPath)) {
        canvasProp.PngProperty.ListWzUsed = true;
      }
      foreach (WzImageProperty subImgProp in canvasProp.WzProperties) {
        setListWzUsed(subImgProp, fullPathsOfListWzPngs);
      }
    } else if (imgProp is WzSubProperty) {
      WzSubProperty subProp = (WzSubProperty)imgProp;
      foreach (WzImageProperty subImgProp in subProp.WzProperties) {
        setListWzUsed(subImgProp, fullPathsOfListWzPngs);
      }
    } else if (imgProp is WzConvexProperty) {
      WzConvexProperty convexProp = (WzConvexProperty)imgProp;
      foreach (WzImageProperty subImgProp in convexProp.WzProperties) {
        setListWzUsed(subImgProp, fullPathsOfListWzPngs);
      }
    }
  }

  public static void setListWzUsed(WzDirectory dir, HashSet<string> fullPathsOfListWzPngs) {
    foreach (WzImage img in dir.WzImages) {
      foreach (WzImageProperty imgProp in img.WzProperties) {
        setListWzUsed(imgProp, fullPathsOfListWzPngs);
      }
    }
    foreach (WzDirectory subDir in dir.WzDirectories) {
      setListWzUsed(subDir, fullPathsOfListWzPngs);
    }
  }

  public static void populateFullPathsOfListWzPngs(WzImageProperty imgProp, HashSet<string> fullPathsOfListWzPngs) {
    if (imgProp is WzCanvasProperty) {
      WzCanvasProperty canvasProp = (WzCanvasProperty)imgProp;
      canvasProp.PngProperty.GetPNG(false);
      if (canvasProp.PngProperty.ListWzUsed) {
        fullPathsOfListWzPngs.Add(canvasProp.PngProperty.FullPath);
      }
      foreach (WzImageProperty subImgProp in canvasProp.WzProperties) {
        populateFullPathsOfListWzPngs(subImgProp, fullPathsOfListWzPngs);
      }
    } else if (imgProp is WzSubProperty) {
      WzSubProperty subProp = (WzSubProperty)imgProp;
      foreach (WzImageProperty subImgProp in subProp.WzProperties) {
        populateFullPathsOfListWzPngs(subImgProp, fullPathsOfListWzPngs);
      }
    } else if (imgProp is WzConvexProperty) {
      WzConvexProperty convexProp = (WzConvexProperty)imgProp;
      foreach (WzImageProperty subImgProp in convexProp.WzProperties) {
        populateFullPathsOfListWzPngs(subImgProp, fullPathsOfListWzPngs);
      }
    }
  }

  public static void populateFullPathsOfListWzPngs(WzDirectory dir, HashSet<string> fullPathsOfListWzPngs) {
    foreach (WzImage img in dir.WzImages) {
      img.ParseImage();
      foreach (WzImageProperty imgProp in img.WzProperties) {
        populateFullPathsOfListWzPngs(imgProp, fullPathsOfListWzPngs);
      }
    }
    foreach (WzDirectory subDir in dir.WzDirectories) {
      populateFullPathsOfListWzPngs(subDir, fullPathsOfListWzPngs);
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

      // find all png's that use List.wz
      HashSet<string> fullPathsOfListWzPngs = new HashSet<string>();
      populateFullPathsOfListWzPngs(wzf.WzDirectory, fullPathsOfListWzPngs);

      // read xml dumps
      WzXmlDeserializer deserializer = new WzXmlDeserializer(false, CryptoConstants.WZ_GMSIV);
      List<WzObject> deserialized = deserializer.ParseXML("xml/" + f + ".xml");
      WzDirectory wzd = (WzDirectory)deserialized[0];

      // prevent null pointer exception
      setIv(wzd, wzf.WzDirectory.WzIv);

      // Set ListWzUsed=true on png's that originally used List.wz
      setListWzUsed(wzd, fullPathsOfListWzPngs);

      // overwrite original WzDirectory with the one read from XML dump
      wzf.WzDirectory = wzd;

      // create .wz file.
      Directory.CreateDirectory("compiled_wz");
      wzf.SaveToDisk("compiled_wz/" + f);
    }
  }
}
