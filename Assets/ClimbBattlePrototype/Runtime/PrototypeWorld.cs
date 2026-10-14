using UnityEngine;
namespace ClimbBattle {
public static class PrototypeWorld {
 public static GameObject Build(int map) {
  var key=MapCatalog.Keys[map];var root=new GameObject("Map_"+key);
  var bg=PrototypeArt.Sprite(root.transform,"Background","Backgrounds/"+key,new Vector2(0,22),new Vector2(120,60),-100);
  bg.GetComponent<SpriteRenderer>().color=new Color(.66f,.71f,.78f,1);
  for(int level=0;level<7;level++) {
   float y=level*6, width=MapCatalog.Widths[map][level], ladderX=width/2-5;
   var tier=PrototypeArt.Node(root.transform,"Floor_"+(level+1),Vector2.zero).transform;
   for(float x=-width/2+2;x<width/2;x+=4) {
    var wall=PrototypeArt.Sprite(tier,"RearWall", "Sprites/"+key+"/WallPanel",new Vector2(x,y+2.5f),new Vector2(4,4.9f),-30);
    wall.GetComponent<SpriteRenderer>().color=new Color(.45f,.48f,.55f,.72f);
   }
   if(level==0) Span(tier,key,-width/2,width/2,y);
   else {
    Span(tier,key,-width/2,-ladderX-1,y);
    Span(tier,key,-ladderX+1,ladderX-1,y);
    Span(tier,key,ladderX+1,width/2,y);
    Ladder(tier,key,new Vector2(-ladderX,y-3),"Ladder");
    Ladder(tier,key,new Vector2(ladderX,y-3),map==3?"RopeChain":"Ladder");
    for(int side=-1;side<=1;side+=2) {
     Platform(tier,key,new Vector2(side*(ladderX-3),y-4),2,"Ledge");
     Platform(tier,key,new Vector2(side*(ladderX-1.5f),y-2),2,"Ledge");
    }
   }
   for(int side=-1;side<=1;side+=2) {
    PrototypeArt.Sprite(tier,"Pillar","Sprites/"+key+"/Pillar",new Vector2(side*(width/2-1),y+2.5f),new Vector2(1.1f,5),-15);
    PrototypeArt.Sprite(tier,"Railing","Sprites/"+key+"/Railing",new Vector2(side*(width/2-2.2f),y+.5f),new Vector2(3,1),8);
    var cover=PrototypeArt.Sprite(tier,"Cover","Sprites/"+key+"/ForegroundCover",new Vector2(side*(width*.23f),y+1.5f),new Vector2(2.7f,3),30);
    PrototypeArt.Box(cover,new Vector2(2.7f,3),true);cover.AddComponent<WorldInteractable>().kind=WorldInteractable.Kind.Cover;
    PrototypeArt.Sprite(tier,"Lamp","Sprites/"+key+"/Lamp",new Vector2(side*(width*.28f),y+4),new Vector2(1,1.3f),5);
   }
   PrototypeArt.Sprite(tier,"Door","Sprites/"+key+"/Door",new Vector2(0,y+1.4f),new Vector2(1.8f,2.8f),-12);
   PrototypeArt.Sprite(tier,"Rubble","Sprites/"+key+"/Rubble",new Vector2(width*.36f,y+.35f),new Vector2(2,.8f),4);
   if(level<6) {
    if(level==0)SpawnPickup(tier,"Pistol",new Vector2(-8,.7f),WorldInteractable.Kind.Pistol);
    SpawnPickup(tier,"HealthKit",new Vector2(-3,y+1.05f),WorldInteractable.Kind.Health);
    SpawnPickup(tier,level%2==0?"ShieldPickup":"AmmoBox",new Vector2(3,y+1.1f),level%2==0?WorldInteractable.Kind.Shield:WorldInteractable.Kind.Ammo);
    if(level==1||level==3) SpawnPickup(tier,level==1?"Rifle":"Shotgun",new Vector2(0,y+3),level==1?WorldInteractable.Kind.Rifle:WorldInteractable.Kind.Shotgun);
    for(int side=-1;side<=1;side+=2) {
     var target=PrototypeArt.Sprite(tier,"PracticeTarget","Sprites/"+key+"/Crate",new Vector2(side*(width*.37f),y+.75f),new Vector2(1.4f,1.5f),12);
     PrototypeArt.Box(target,new Vector2(1.3f,1.5f));target.AddComponent<WorldInteractable>().kind=WorldInteractable.Kind.Target;
    }
   }
   if(level>0 && level<6 && level%2==1) {
    var lift=Platform(tier,key,new Vector2(0,y-4.5f),3,"LiftPlatform");
    var moving=lift.AddComponent<WorldInteractable>();moving.kind=WorldInteractable.Kind.Lift;moving.travel=3.3f;moving.speed=.7f;
    var rb=lift.AddComponent<Rigidbody2D>();rb.bodyType=RigidbodyType2D.Kinematic;rb.interpolation=RigidbodyInterpolation2D.Interpolate;
    PrototypeArt.Sprite(tier,"LiftFrame","Sprites/"+key+"/LiftFrame",new Vector2(0,y-2.5f),new Vector2(3.5f,5),-10);
   }
  }
  var goal=PrototypeArt.Sprite(root.transform,"SummitExit","Sprites/Shared/ShieldPickup",new Vector2(0,38),new Vector2(1.6f,1.8f),25);
  PrototypeArt.Box(goal,new Vector2(3,3),true);goal.AddComponent<WorldInteractable>().kind=WorldInteractable.Kind.Goal;
  var hazard=PrototypeArt.Node(root.transform,"RisingHazard",new Vector2(0,-9));
  var controller=hazard.AddComponent<RisingHazard>();controller.mapIndex=map;
  PrototypeArt.Rect(hazard.transform,"HazardFill",new Vector2(0,-39),new Vector2(100,60),MapCatalog.Colors[map]*new Color(.3f,.3f,.3f,1),35);
  for(int i=0;i<17;i++) PrototypeArt.Sprite(hazard.transform,"HazardSurface","Sprites/"+key+"/HazardSurface",new Vector2(-48+i*6,-9),new Vector2(6.15f,2),36);
  return root;
 }
 static void Span(Transform root,string key,float left,float right,float y) {
  for(float x=left;x<right-.01f;x+=4) {float width=Mathf.Min(4,right-x);Platform(root,key,new Vector2(x+width/2,y),width,"FloorBeam");}
 }
 public static GameObject Platform(Transform root,string key,Vector2 top,float width,string asset) {
  var holder=PrototypeArt.Node(root,asset,top);
  PrototypeArt.Sprite(holder.transform,"Visual","Sprites/"+key+"/"+asset,top+Vector2.down*.46f,new Vector2(width,.92f),0);
  var c=holder.AddComponent<BoxCollider2D>();c.size=new Vector2(width+.03f,.35f);c.offset=Vector2.down*.175f;c.usedByEffector=true;
  var e=holder.AddComponent<PlatformEffector2D>();e.useOneWay=true;e.useOneWayGrouping=true;e.surfaceArc=175;e.useSideFriction=false;e.useSideBounce=false;
  return holder;
 }
 static void Ladder(Transform parent,string key,Vector2 pos,string asset) {
  var go=PrototypeArt.Sprite(parent,"ClimbRoute","Sprites/"+key+"/"+asset,pos,new Vector2(.95f,6.7f),3);
  PrototypeArt.Box(go,new Vector2(1.35f,7.2f),true);go.AddComponent<WorldInteractable>().kind=WorldInteractable.Kind.Ladder;
 }
 public static void SpawnPickup(Transform parent,string sprite,Vector2 pos,WorldInteractable.Kind kind) {
  var go=PrototypeArt.Sprite(parent,"Pickup_"+sprite,"Sprites/Shared/"+sprite,pos,new Vector2(.95f,.95f),18);
  PrototypeArt.Box(go,new Vector2(1.3f,1.3f),true);go.AddComponent<WorldInteractable>().kind=kind;
  PrototypeArt.Sprite(go.transform,"Glow","Sprites/Shared/PickupGlow",pos+Vector2.down*.35f,new Vector2(1.3f,.5f),14);
 }
}
}
