using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace ClimbBattle.Editor {
public static class WeaponPickupUpgrade {
 const string Marker="Assets/ClimbBattlePrototype/WeaponPickups-v1.txt";
 public static void Apply(){
  if(File.Exists(Marker))return;
  for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)
   throw new InvalidOperationException("Save open scenes before adding the weapon pickups.");
  Directory.CreateDirectory("Art/SceneBackups");var previous=SceneManager.GetActiveScene();
  for(int map=0;map<4;map++){
   string path=MapCatalog.SceneFolder+MapCatalog.SceneName(map)+".unity";
   if(!File.Exists(path))continue;
   var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
   if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
   try{
    if(scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<WorldInteractable>(true)).Any(a=>a.kind==WorldInteractable.Kind.Pistol))continue;
    string backup="Art/SceneBackups/"+MapCatalog.SceneName(map)+".before-weapon-pickups.unity";
    if(!File.Exists(backup))File.Copy(path,backup);
    var root=scene.GetRootGameObjects().First(g=>g.name=="Map_"+MapCatalog.Keys[map]);
    PrototypeWorld.SpawnPickup(root.transform.Find("Floor_1"),"Pistol",new Vector2(-8,.7f),WorldInteractable.Kind.Pistol);
    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
  }
  if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
  File.WriteAllText(Marker,"First-floor pistol added. Existing map edits preserved.\n");AssetDatabase.Refresh();
 }
}
}
