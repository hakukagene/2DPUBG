using System;
using System.Collections.Generic;
using UnityEngine;
namespace ClimbBattle {
public static class TacticalArt {
 [Serializable] public class Region {public string name;public int x,y,width,height;}
 [Serializable] public class Atlas {public int width,height;public Region[] frames;}
 static readonly Dictionary<string,Sprite> cache=new Dictionary<string,Sprite>();
 public static Sprite Load(int map,string part){
  string key=MapCatalog.Keys[map]+"/"+part;
  if(!cache.TryGetValue(key,out var sprite)||!sprite){
   foreach(var asset in Resources.LoadAll<Sprite>("ClimbBattleArt/Tactical/"+MapCatalog.Keys[map]))cache[MapCatalog.Keys[map]+"/"+asset.name]=asset;
   if(!cache.TryGetValue(key,out sprite)||!sprite)throw new InvalidOperationException("Missing tactical sprite "+key);
  }
  return sprite;
 }
 public static SpriteRenderer Draw(Transform parent,int map,string part,Vector2 position,Vector2 size,int order=15){
  var go=new GameObject(part);go.transform.SetParent(parent,false);go.transform.position=position;
  var r=go.AddComponent<SpriteRenderer>();r.sprite=Load(map,part);r.sortingOrder=order;
  r.sharedMaterial=Resources.Load<Material>("ClimbBattleArt/SpriteUnlit");Size(r,size);return r;
 }
 public static void Size(SpriteRenderer r,Vector2 size){r.transform.localScale=new Vector3(size.x/r.sprite.bounds.size.x,size.y/r.sprite.bounds.size.y,1);}
}
}
