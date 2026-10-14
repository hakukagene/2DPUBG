using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace ClimbBattle.Editor {
[InitializeOnLoad]
public static class PrototypeSetup {
 const string Root="Assets/ClimbBattlePrototype";
 const string Marker=Root+"/PrototypeGenerated-v1.txt";
 static PrototypeSetup(){EditorApplication.delayCall+=AutoSetup;}
 static void AutoSetup(){
  if(Application.isBatchMode||File.Exists(Marker)||EditorApplication.isPlayingOrWillChangePlaymode)return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=AutoSetup;return;}
  try{BuildMissing();}catch(Exception e){Debug.LogException(e);}
 }
 [MenuItem("2DPUBG/Build and Open Prototype",priority=0)]
 public static void OpenPrototype(){
  BuildMissing();
  if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
   EditorSceneManager.OpenScene(MapCatalog.SceneFolder+MapCatalog.SceneName(-1)+".unity");
 }
 [MenuItem("2DPUBG/Validate Prototype",priority=10)]
 public static void Validate(){
  foreach(var key in CharacterCatalog.Keys)
   foreach(var folder in new[]{"Bodies","Portraits"})
    if(!Resources.Load<Sprite>("ClimbBattleArt/Characters/"+folder+"/"+key))throw new Exception("Missing character "+folder+"/"+key);
  foreach(var key in CharacterCatalog.Keys)foreach(var folder in new[]{"Animations","Armed"}){
   string animationRoot="ClimbBattleArt/Characters/"+folder+"/";
   var manifest=Resources.Load<TextAsset>(animationRoot+key);
   if(!manifest)throw new Exception("Missing animation manifest "+key);
   var sheet=JsonUtility.FromJson<CharacterMotion.Sheet>(manifest.text.TrimStart('\uFEFF'));
   if(sheet.clips==null||sheet.clips.Length!=6)throw new Exception("Expected six animation clips for "+key);
   foreach(var clip in sheet.clips){
    if(!Resources.Load<Sprite>(animationRoot+clip.texture)||clip.pixelsPerUnit<=0||clip.textureWidth<=0||clip.textureHeight<=0||clip.frames.Length<5)
     throw new Exception("Invalid animation clip "+key);
    foreach(var frame in clip.frames)
     if(frame.x<0||frame.y<0||frame.width<=0||frame.height<=0||frame.x+frame.width>clip.textureWidth||frame.y+frame.height>clip.textureHeight)
      throw new Exception("Animation frame outside source texture "+key);
   }
  }
  var armData=Resources.Load<TextAsset>("ClimbBattleArt/Characters/Armed/ArmParts");
  if(!armData||!Resources.Load<Sprite>("ClimbBattleArt/Characters/Armed/ArmParts"))throw new Exception("Missing arm rig assets");
  var arms=JsonUtility.FromJson<CharacterMotion.Sheet>(armData.text.TrimStart('\uFEFF'));
  if(arms.clips.Length!=5||arms.clips.Any(c=>c.frames.Length!=3))throw new Exception("Expected three arm parts for five characters");
  foreach(var key in MapCatalog.Keys){
   if(!Resources.Load<Sprite>("ClimbBattleArt/Backgrounds/"+key))throw new Exception("Missing background "+key);
   foreach(var name in new[]{"FloorBeam","Ladder","ForegroundCover","HazardSurface","LiftPlatform"})
    if(!Resources.Load<Sprite>("ClimbBattleArt/Sprites/"+key+"/"+name))throw new Exception("Missing asset "+key+"/"+name);
  }
  for(int i=-1;i<4;i++){
   string path=MapCatalog.SceneFolder+MapCatalog.SceneName(i)+".unity";
   if(!File.Exists(path))throw new Exception("Missing scene "+path);
   if(!EditorBuildSettings.scenes.Any(s=>s.path==path&&s.enabled))throw new Exception("Scene disabled in build settings "+path);
  }
  Debug.Log("2DPUBG: five scenes and required map assets validated.");
 }
 public static void BuildMissing(){
  if(File.Exists(Marker)){RegisterScenes();WeaponPickupUpgrade.Apply();return;}
  Directory.CreateDirectory(MapCatalog.SceneFolder);
  foreach(var png in Directory.GetFiles(Root+"/Resources/ClimbBattleArt","*.png",SearchOption.AllDirectories))
   AssetDatabase.ImportAsset(png.Replace('\\','/'),ImportAssetOptions.ForceUpdate);
  string materialPath=Root+"/Resources/ClimbBattleArt/SpriteUnlit.mat";
  if(!File.Exists(materialPath)){
   var shader=Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")??Shader.Find("Sprites/Default");
   if(!shader)throw new Exception("No compatible sprite shader is installed.");
   AssetDatabase.CreateAsset(new Material(shader),materialPath);
  }
  var previous=SceneManager.GetActiveScene();
  for(int index=-1;index<4;index++){
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
   SceneManager.SetActiveScene(scene);
   try{
    var game=new GameObject("PrototypeGame").AddComponent<PrototypeGame>();game.MapIndex=index;
    var camera=new GameObject("Main Camera");camera.tag="MainCamera";
    var cam=camera.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=27;cam.transform.position=new Vector3(0,20,-10);
    camera.AddComponent<AudioListener>();
    if(index>=0){
     var world=PrototypeWorld.Build(index);
     if(world.GetComponentsInChildren<WorldInteractable>().Count(x=>x.kind==WorldInteractable.Kind.Ladder)!=12)
      throw new Exception("Expected twelve climb routes.");
     if(world.transform.Cast<Transform>().Count(x=>x.name.StartsWith("Floor_"))!=7)
      throw new Exception("Expected seven floors.");
    }
    EditorSceneManager.SaveScene(scene,MapCatalog.SceneFolder+MapCatalog.SceneName(index)+".unity");
   }finally{EditorSceneManager.CloseScene(scene,true);}
  }
  if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
  RegisterScenes();File.WriteAllText(Marker,"Generated prototype scenes. Keep this file to preserve your map edits.\n");
  AssetDatabase.Refresh();AssetDatabase.SaveAssets();Validate();
  Debug.Log("2DPUBG prototype ready. Open Assets/ClimbBattlePrototype/Scenes/CB_MainMenu.unity and press Play.");
 }
 static void RegisterScenes(){
  var ours=Enumerable.Range(-1,5).Select(i=>new EditorBuildSettingsScene(MapCatalog.SceneFolder+MapCatalog.SceneName(i)+".unity",true)).ToArray();
  var existing=EditorBuildSettings.scenes.Where(s=>!ours.Any(o=>o.path==s.path));
  EditorBuildSettings.scenes=ours.Concat(existing).ToArray();
 }
}
}
