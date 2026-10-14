using UnityEngine;
namespace ClimbBattle {
public static class TacticalWorld {
 public const string RootName="TacticalProps-v1";
 public static GameObject Ensure(Transform mapRoot,int map){
  var existing=mapRoot.Find(RootName);if(existing)return existing.gameObject;
  var root=PrototypeArt.Node(mapRoot,RootName,Vector2.zero).transform;
  CreateGun(root,map,new Vector2(-15,0),1);
  CreateGun(root,map,new Vector2(8,18),-1);
  var first=new[]{TacticalTrap.TrapKind.Laser,TacticalTrap.TrapKind.Steam,TacticalTrap.TrapKind.Breakaway,TacticalTrap.TrapKind.Darts}[map];
  var second=new[]{TacticalTrap.TrapKind.Shock,TacticalTrap.TrapKind.Press,TacticalTrap.TrapKind.Swing,TacticalTrap.TrapKind.Sand}[map];
  for(int i=0;i<4;i++){
   int level=new[]{1,2,4,5}[i];float x=new[]{7f,-5f,3.5f,-2f}[i],y=level*6;
   var kind=i%2==0?first:second;
   float width=3.2f;
   if(kind==TacticalTrap.TrapKind.Breakaway){
    // Replace one existing tile, retaining it disabled for easy recovery.
    var tier=mapRoot.Find("Floor_"+(level+1));Transform tile=null;float distance=float.MaxValue;
    foreach(Transform child in tier)if(child.name=="FloorBeam"&&child.gameObject.activeSelf){float d=Mathf.Abs(child.position.x-x);if(d<distance){distance=d;tile=child;}}
    if(tile){x=tile.position.x;width=tile.GetComponent<BoxCollider2D>().size.x-.03f;tile.gameObject.SetActive(false);}
   }
   var trap=CreateTrap(root,map,kind,new Vector2(x,y),width);trap.phaseOffset=i*.65f;
  }
  return root.gameObject;
 }
 public static MountedGun CreateGun(Transform parent,int map,Vector2 origin,int direction){
  var go=PrototypeArt.Node(parent,"MountedGun_"+MapCatalog.Keys[map],origin);var gun=go.AddComponent<MountedGun>();gun.mapIndex=map;gun.direction=direction;
  TacticalArt.Draw(go.transform,map,"Base",origin+Vector2.up*.55f,new Vector2(1.5f,1.1f),18);
  gun.barrel=TacticalArt.Draw(go.transform,map,"Barrel",gun.Pivot,new Vector2(2.2f,.85f),25);gun.barrel.transform.rotation=Quaternion.Euler(0,0,direction<0?180:0);gun.barrel.flipY=direction<0;
  var cover=TacticalArt.Draw(go.transform,map,"Cover",origin+new Vector2(direction*.9f,.5f),new Vector2(1.35f,1),23);
  PrototypeArt.Box(cover.gameObject,new Vector2(1.2f,.95f));
  TacticalArt.Draw(go.transform,map,"Ammo",origin+new Vector2(-direction*1.5f,.3f),new Vector2(.65f,.6f),16);
  gun.flash=PrototypeArt.Sprite(go.transform,"Flash","Sprites/Shared/MuzzleFlash",gun.Pivot,new Vector2(.55f,.38f),29).GetComponent<SpriteRenderer>();gun.flash.enabled=false;
  gun.heatBar=PrototypeArt.Rect(go.transform,"Heat",origin+Vector2.up*2,new Vector2(.02f,.07f),Color.cyan,30).GetComponent<SpriteRenderer>();
  return gun;
 }
 public static TacticalTrap CreateTrap(Transform parent,int map,TacticalTrap.TrapKind kind,Vector2 origin,float width=3.2f){
  var go=PrototypeArt.Node(parent,"Trap_"+kind,origin);var trap=go.AddComponent<TacticalTrap>();trap.kind=kind;trap.mapIndex=map;
  trap.area=new Vector2(width,1.8f);trap.areaOffset=new Vector2(0,.9f);
  trap.switchOffset=new Vector2(-width*.5f-1,1);trap.hasSwitch=kind!=TacticalTrap.TrapKind.Breakaway&&kind!=TacticalTrap.TrapKind.Sand&&kind!=TacticalTrap.TrapKind.Darts;
  var t=go.transform;
  switch(kind){
   case TacticalTrap.TrapKind.Laser:
    trap.visual=TacticalArt.Draw(t,map,"TrapA",origin+new Vector2(-width*.5f,1),new Vector2(.55f,2));
    var end=TacticalArt.Draw(t,map,"TrapA",origin+new Vector2(width*.5f,1),new Vector2(.55f,2));end.flipX=true;
    trap.effect=TacticalArt.Draw(t,map,"EffectA",origin+Vector2.up*.9f,new Vector2(width,.25f),24);trap.area=new Vector2(width,.35f);trap.areaOffset=Vector2.up*.9f;break;
   case TacticalTrap.TrapKind.Shock:
    trap.visual=TacticalArt.Draw(t,map,"TrapB",origin+Vector2.up*.12f,new Vector2(width,.65f));
    trap.effect=TacticalArt.Draw(t,map,"EffectB",origin+Vector2.up*.45f,new Vector2(width,.8f),24);trap.area=new Vector2(width,.55f);trap.areaOffset=Vector2.up*.23f;break;
   case TacticalTrap.TrapKind.Steam:
    trap.visual=TacticalArt.Draw(t,map,"TrapA",origin+new Vector2(-width*.5f,.7f),new Vector2(.9f,1.1f));
    trap.effect=TacticalArt.Draw(t,map,"EffectA",origin+Vector2.up*.7f,new Vector2(width,1.1f),24);trap.area=new Vector2(width,1.1f);trap.areaOffset=Vector2.up*.65f;trap.damage=15;break;
   case TacticalTrap.TrapKind.Press:
    TacticalArt.Draw(t,map,"EffectB",origin+Vector2.up*1.9f,new Vector2(width+1,3.8f),-5);
    trap.visual=TacticalArt.Draw(t,map,"TrapB",origin+Vector2.up*3.15f,new Vector2(width,.95f),24);trap.movingPart=trap.visual.transform;trap.damage=40;break;
   case TacticalTrap.TrapKind.Breakaway:
    var holder=PrototypeArt.Node(t,"FallingSlab",origin);
    trap.visual=TacticalArt.Draw(holder.transform,map,"TrapA",origin+Vector2.down*.25f,new Vector2(width,.5f));trap.movingPart=holder.transform;
    var collider=holder.AddComponent<BoxCollider2D>();collider.size=new Vector2(width,.3f);collider.offset=Vector2.down*.15f;collider.usedByEffector=true;
    var effector=holder.AddComponent<PlatformEffector2D>();effector.useOneWay=true;trap.breakawayCollider=collider;
    trap.effect=TacticalArt.Draw(t,map,"EffectA",origin+Vector2.down*1.2f,new Vector2(width,2),24);trap.idleSeconds=0;trap.warningSeconds=.9f;trap.cooldownSeconds=5;break;
   case TacticalTrap.TrapKind.Swing:
    var swing=PrototypeArt.Node(t,"SwingAssembly",origin+Vector2.up*3.6f);trap.movingPart=swing.transform;
    trap.visual=TacticalArt.Draw(swing.transform,map,"TrapB",origin+Vector2.up*.8f,new Vector2(2.2f,.55f),24);
    for(int side=-1;side<=1;side+=2)TacticalArt.Draw(swing.transform,map,"EffectB",origin+new Vector2(side*.8f,2.25f),new Vector2(.12f,2.5f),18);
    trap.area=new Vector2(6,2);trap.activeSeconds=3.5f;trap.damage=25;break;
   case TacticalTrap.TrapKind.Darts:
    trap.visual=TacticalArt.Draw(t,map,"TrapA",origin+new Vector2(-width*.5f-.3f,1.1f),new Vector2(.85f,2.2f));
    TacticalArt.Draw(t,map,"Switch",origin+new Vector2(0,.08f),new Vector2(1,.16f));
    trap.effect=TacticalArt.Draw(t,map,"EffectA",origin+Vector2.up*1.3f,new Vector2(.7f,.15f),24);trap.idleSeconds=0;trap.damage=18;break;
   case TacticalTrap.TrapKind.Sand:
    trap.visual=TacticalArt.Draw(t,map,"TrapB",origin+Vector2.up*.08f,new Vector2(width,.65f));
    trap.effect=TacticalArt.Draw(t,map,"EffectB",origin+Vector2.up*.15f,new Vector2(width,.45f),24);trap.area=new Vector2(width,.4f);trap.areaOffset=Vector2.up*.1f;
    var rope=PrototypeArt.Sprite(t,"EscapeRope","Sprites/EgyptPyramid/RopeChain",origin+new Vector2(width*.5f-.3f,1.1f),new Vector2(.35f,2.5f),10);
    PrototypeArt.Box(rope,new Vector2(.8f,2.5f),true);rope.AddComponent<WorldInteractable>().kind=WorldInteractable.Kind.Ladder;break;
  }
  trap.warning=PrototypeArt.Rect(t,"WarningZone",trap.Center,trap.area,new Color(1,.35f,.06f,.35f),14).GetComponent<SpriteRenderer>();trap.warning.enabled=false;
  if(trap.effect)trap.effect.enabled=false;
  if(trap.hasSwitch)trap.switchVisual=TacticalArt.Draw(t,map,"Switch",trap.SwitchPosition,new Vector2(.45f,.6f),20);
  return trap;
 }
}
}
