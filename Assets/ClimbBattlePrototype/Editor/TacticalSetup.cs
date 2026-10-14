using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace ClimbBattle.Editor {
[InitializeOnLoad]
public static class TacticalSetup {
 const string Root="Assets/ClimbBattlePrototype";
 const string Marker=Root+"/TacticalProps-v1.txt";
 static TacticalSetup(){EditorApplication.delayCall+=Auto;}
 static void Auto(){
  if(Application.isBatchMode||File.Exists(Marker)||EditorApplication.isPlayingOrWillChangePlaymode)return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Auto;return;}
  for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)return;
  try{Apply();}catch(Exception e){Debug.LogException(e);}
 }
 [MenuItem("2DPUBG/Add Tactical Props to Maps",priority=12)]
 public static void Apply(){
  if(File.Exists(Marker))return;
  for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save open scenes before adding tactical props.");
  foreach(var key in MapCatalog.Keys)AssetDatabase.ImportAsset(Root+"/Resources/ClimbBattleArt/Tactical/"+key+".png",ImportAssetOptions.ForceUpdate);
  ValidateAssets();
  string prefabs=Root+"/Prefabs/Tactical";Directory.CreateDirectory(prefabs);AssetDatabase.Refresh();
  for(int map=0;map<4;map++){
   var gun=TacticalWorld.CreateGun(null,map,Vector2.zero,1);
   SavePrefab(gun.gameObject,prefabs+"/"+MapCatalog.Keys[map]+"_MountedGun.prefab");
   for(int i=0;i<2;i++){
    var trap=TacticalWorld.CreateTrap(null,map,(TacticalTrap.TrapKind)(map*2+i),Vector2.zero);
    SavePrefab(trap.gameObject,prefabs+"/"+trap.kind+".prefab");
   }
  }
  Directory.CreateDirectory("Art/SceneBackups");var previous=SceneManager.GetActiveScene();
  for(int map=0;map<4;map++){
   string path=MapCatalog.SceneFolder+MapCatalog.SceneName(map)+".unity";
   var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
   if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
   try{
    var mapRoot=scene.GetRootGameObjects().First(g=>g.name=="Map_"+MapCatalog.Keys[map]);
    if(mapRoot.transform.Find(TacticalWorld.RootName))continue;
    string backup="Art/SceneBackups/"+MapCatalog.SceneName(map)+".before-tactical-props.unity";
    if(!File.Exists(backup))File.Copy(path,backup);
    TacticalWorld.Ensure(mapRoot.transform,map);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
  }
  if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
  File.WriteAllText(Marker,"Four map upgrades, eight mounted guns, sixteen traps and twelve reusable prefabs.\n");
  AssetDatabase.Refresh();AssetDatabase.SaveAssets();
  Debug.Log("Tactical props installed: four maps, 36 sprites, 12 prefabs.");
 }
 static void SavePrefab(GameObject go,string path){try{if(!File.Exists(path))PrefabUtility.SaveAsPrefabAsset(go,path);}finally{UnityEngine.Object.DestroyImmediate(go);}}
 public static void ValidateAssets(){foreach(var key in MapCatalog.Keys){var sprites=Resources.LoadAll<Sprite>("ClimbBattleArt/Tactical/"+key);if(sprites.Length!=9)throw new Exception("Expected nine sliced sprites for "+key+", got "+sprites.Length);}}
}
}
