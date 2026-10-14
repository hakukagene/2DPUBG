using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClimbBattle {
// Frame rectangles are registered to the feet so differently sized poses do not jitter.
public sealed class CharacterMotion:MonoBehaviour {
 public enum MotionState {Idle,Walk,Run,Jump,Crouch,CrouchWalk,Prone,Crawl,Climb,Land}
 [Serializable] public sealed class Frame {public int x,y,width,height;public float shoulderX,shoulderY;}
 [Serializable] public sealed class Clip {public string texture;public int textureWidth,textureHeight;public float pixelsPerUnit;public Frame[] frames;}
 [Serializable] public sealed class Sheet {public Clip[] clips;}
 static readonly Dictionary<int,Sprite[][]> cache=new Dictionary<int,Sprite[][]>();
 static readonly Dictionary<int,string> manifests=new Dictionary<int,string>();
 static readonly Dictionary<Sprite,Vector2> shoulders=new Dictionary<Sprite,Vector2>();
 public MotionState State {get;private set;}
 public int FrameIndex {get;private set;}
 public int Row {get;private set;}=5;
 public bool ArmedBody {get;private set;}
 static readonly float[] ShoulderX={.50f,.67f,.5f,.55f,.72f,.43f};
 static readonly float[] ShoulderY={.66f,.65f,.64f,.57f,.42f,.67f};
 public Vector3 ShoulderPosition {
  get {
   var bounds=target.sprite.bounds;
   var anchor=shoulders.TryGetValue(target.sprite,out var point)?point:new Vector2(ShoulderX[Row],ShoulderY[Row]);
   return target.transform.TransformPoint(new Vector3(bounds.min.x+bounds.size.x*anchor.x,bounds.min.y+bounds.size.y*anchor.y,0));
  }
 }
 public bool Ready {get;private set;}
 SoloPlayer player;SpriteRenderer target;Sprite[][] clips;
 float elapsed,landingUntil;bool wasAirborne;

 public void Initialize(SoloPlayer owner,SpriteRenderer renderer){
  player=owner;target=renderer;
  clips=LoadFrames(owner.CharacterIndex,false);
  target.transform.localPosition=new Vector3(0,-.85f,0);target.transform.localScale=Vector3.one;
  target.sprite=clips[5][0];State=MotionState.Idle;Ready=true;
 }
 public static Sprite[][] LoadFrames(int character,bool armed){
  int key=character+(armed?CharacterCatalog.Keys.Length:0);
  string root="ClimbBattleArt/Characters/"+(armed?"Armed/":"Animations/");
  var asset=Resources.Load<TextAsset>(root+CharacterCatalog.Keys[character]);
  if(!asset)throw new InvalidOperationException("Missing character animation manifest: "+character);
  string manifest=asset.text.TrimStart('\uFEFF');
  if(!cache.TryGetValue(key,out var frames)||!frames[0][0]||!manifests.TryGetValue(key,out var previous)||previous!=manifest){
   var sheet=JsonUtility.FromJson<Sheet>(manifest);
   frames=new Sprite[sheet.clips.Length][];
   for(int row=0;row<sheet.clips.Length;row++){
    var clip=sheet.clips[row];var texture=Resources.Load<Sprite>(root+clip.texture).texture;
    frames[row]=new Sprite[clip.frames.Length];
    float sx=(float)texture.width/clip.textureWidth,sy=(float)texture.height/clip.textureHeight;
    for(int col=0;col<clip.frames.Length;col++){
     var f=clip.frames[col];var sprite=Sprite.Create(texture,new Rect(f.x*sx,f.y*sy,f.width*sx,f.height*sy),new Vector2(.5f,0),clip.pixelsPerUnit*sy,0,SpriteMeshType.FullRect);
     sprite.name=CharacterCatalog.Keys[character]+(armed?"_Armed":"")+"_"+row+"_"+col;frames[row][col]=sprite;
     if(armed&&f.shoulderX>0&&f.shoulderY>0)shoulders[sprite]=new Vector2(f.shoulderX,f.shoulderY);
    }
   }
   cache[key]=frames;
   manifests[key]=manifest;
  }
  return frames;
 }
 void LateUpdate(){
  if(!Ready||!PrototypeGame.IsPlaying)return;
  bool armed=player.HasWeapon||player.Mounted;
  if(ArmedBody!=armed){ArmedBody=armed;clips=LoadFrames(player.CharacterIndex,ArmedBody);}
  var velocity=player.Velocity;bool moving=Mathf.Abs(velocity.x)>.15f;
  bool airborne=!player.Grounded||velocity.y>.5f;
  if(wasAirborne&&!airborne&&!player.Climbing)landingUntil=Time.time+.1f;
  wasAirborne=airborne&&!player.Climbing;
  MotionState next;
  if(player.Stance==SoloPlayer.Posture.Prone)next=moving?MotionState.Crawl:MotionState.Prone;
  else if(player.Stance==SoloPlayer.Posture.Crouching)next=moving?MotionState.CrouchWalk:MotionState.Crouch;
  else if(player.Climbing)next=MotionState.Climb;
  else if(airborne)next=MotionState.Jump;
  else if(Time.time<landingUntil)next=MotionState.Land;
  else if(moving)next=player.Sprinting?MotionState.Run:MotionState.Walk;
  else next=MotionState.Idle;
  if(next!=State){State=next;elapsed=0;}else elapsed+=Time.deltaTime;
  int row=5;float fps=4;bool hold=false;
  switch(State){
   case MotionState.Walk:row=0;fps=9;break;
   case MotionState.Run:row=1;fps=13;break;
   case MotionState.Jump:row=2;hold=true;break;
   case MotionState.Land:row=2;hold=true;break;
   case MotionState.Crouch:row=3;hold=true;break;
   case MotionState.CrouchWalk:row=3;fps=7;break;
   case MotionState.Prone:row=4;hold=true;break;
   case MotionState.Crawl:row=4;fps=6;break;
   case MotionState.Climb:row=0;fps=6;hold=Mathf.Abs(velocity.y)<.1f;break;
  }
  FrameIndex=hold?0:(int)(elapsed*fps)%clips[row].Length;
  if(State==MotionState.Jump)FrameIndex=velocity.y>7?1:velocity.y>1?2:velocity.y> -2?3:4;
  if(State==MotionState.Land)FrameIndex=5;
  Row=row;target.sprite=clips[row][FrameIndex];
 }
}
}
