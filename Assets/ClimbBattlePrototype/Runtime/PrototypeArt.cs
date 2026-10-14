using System;
using System.Collections.Generic;
using UnityEngine;
namespace ClimbBattle {
public static class PrototypeArt {
 static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
 static Material fallbackMaterial;
 public static Sprite Load(string path) {
  if (!Cache.TryGetValue(path, out var sprite) || !sprite) {
   sprite = Resources.Load<Sprite>("ClimbBattleArt/" + path);
   if (!sprite) throw new InvalidOperationException("Missing sprite: ClimbBattleArt/" + path);
   Cache[path] = sprite;
  }
  return sprite;
 }
 public static GameObject Sprite(Transform parent,string name,string path,Vector2 position,Vector2 size,int order,Color? color=null) {
  var go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.position=position;
  var r=go.AddComponent<SpriteRenderer>(); r.sprite=Load(path); r.sortingOrder=order;
  r.color=color??Color.white;
  var material=Resources.Load<Material>("ClimbBattleArt/SpriteUnlit");
  if(!material){
   if(!fallbackMaterial){var shader=Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")??Shader.Find("Sprites/Default");if(shader)fallbackMaterial=new Material(shader);}
   material=fallbackMaterial;
  }
  if(material)r.sharedMaterial=material;
  var parentScale=parent?parent.lossyScale:Vector3.one;
  go.transform.localScale=new Vector3(size.x/r.sprite.bounds.size.x/parentScale.x,size.y/r.sprite.bounds.size.y/parentScale.y,1);
  return go;
 }
 public static GameObject Rect(Transform parent,string name,Vector2 position,Vector2 size,Color color,int order) =>
  Sprite(parent,name,"White",position,size,order,color);
 public static BoxCollider2D Box(GameObject go,Vector2 worldSize,bool trigger=false) {
  var c=go.AddComponent<BoxCollider2D>();
  c.size=new Vector2(worldSize.x/Mathf.Abs(go.transform.lossyScale.x),worldSize.y/Mathf.Abs(go.transform.lossyScale.y));
  c.isTrigger=trigger; return c;
 }
 public static GameObject Node(Transform parent,string name,Vector2 position) {
  var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=position;return go;
 }
}
}
