using UnityEditor;
using UnityEngine;
namespace ClimbBattle.Editor {
public sealed class PrototypeTextureImporter:AssetPostprocessor {
 void OnPreprocessTexture() {
  if(!assetPath.StartsWith("Assets/ClimbBattlePrototype/Resources/ClimbBattleArt/"))return;
  var importer=(TextureImporter)assetImporter;
  importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
  importer.spritePixelsPerUnit=100;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
  importer.filterMode=assetPath.Contains("/Characters/")||assetPath.Contains("/NeonCity/")||assetPath.EndsWith("White.png")?FilterMode.Point:FilterMode.Bilinear;
  importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
  var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
  settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteAlignment=(int)SpriteAlignment.Center;
  importer.SetTextureSettings(settings);
  if(assetPath.Contains("/Tactical/")) {
   string metadata=System.IO.Path.ChangeExtension(assetPath,".json");
   if(!System.IO.File.Exists(metadata))return;
   var atlas=JsonUtility.FromJson<TacticalArt.Atlas>(System.IO.File.ReadAllText(metadata).TrimStart('\uFEFF'));
   importer.spriteImportMode=SpriteImportMode.Multiple;
   var regions=new SpriteMetaData[atlas.frames.Length];
   for(int i=0;i<regions.Length;i++){
    var f=atlas.frames[i];regions[i]=new SpriteMetaData{name=f.name,rect=new Rect(f.x,f.y,f.width,f.height),alignment=(int)SpriteAlignment.Center,pivot=new Vector2(.5f,.5f)};
   }
   #pragma warning disable 0618
   importer.spritesheet=regions;
   #pragma warning restore 0618
  }
 }
}
}
