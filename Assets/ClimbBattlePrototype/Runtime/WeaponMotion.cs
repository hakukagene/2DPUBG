using UnityEngine;
namespace ClimbBattle {
// Weapon-specific upper-body animation layered over the shared locomotion frames.
// Hands follow grip anchors through aiming, movement, recoil, pumping and reloading.
[DefaultExecutionOrder(100)]
public sealed class WeaponMotion:MonoBehaviour {
 public string AnimationName {get;private set;}="Unarmed";
 public bool Visible=>weapon&&weapon.enabled;
 public SpriteRenderer Renderer=>weapon;
 public Vector2 MuzzlePosition {get;private set;}
 public Vector2 TriggerHand {get;private set;}
 public Vector2 SupportHand {get;private set;}
 public float RecoilAmount {get;private set;}
 public float PumpAmount {get;private set;}
 SoloPlayer player;Transform root;
 SpriteRenderer weapon,flash,reloadProp;
 readonly SpriteRenderer[,] arms=new SpriteRenderer[2,3];
 float fireAt=-100,equipAt=-100,clock;
 Quaternion weaponRotation;
 Vector2 center;
 static Sprite[][] parts;

 public void Initialize(SoloPlayer owner){
  player=owner;root=PrototypeArt.Node(transform,"WeaponAnimationRig",transform.position).transform;
  weapon=PrototypeArt.Sprite(root,"HeldWeapon","Sprites/Shared/Pistol",transform.position,Vector2.one,24).GetComponent<SpriteRenderer>();
  flash=PrototypeArt.Sprite(root,"MuzzleFlash","Sprites/Shared/MuzzleFlash",transform.position,new Vector2(.36f,.26f),29).GetComponent<SpriteRenderer>();
  reloadProp=PrototypeArt.Sprite(root,"ReloadMagazine","Sprites/Shared/AmmoBox",transform.position,new Vector2(.12f,.22f),26).GetComponent<SpriteRenderer>();
  LoadParts();
  for(int side=0;side<2;side++)for(int part=0;part<3;part++){
   var go=new GameObject((side==0?"Support":"Trigger")+new[]{"UpperArm","Forearm","Hand"}[part]);go.transform.SetParent(root,false);
   var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=parts[player.CharacterIndex][part];renderer.sharedMaterial=weapon.sharedMaterial;
   renderer.sortingOrder=side==0?19+part:23+part*2;arms[side,part]=renderer;
  }
  SetVisible(false);
 }
 static void LoadParts(){
  if(parts!=null&&parts[0][0])return;
  const string path="ClimbBattleArt/Characters/Armed/ArmParts";
  var sheet=JsonUtility.FromJson<CharacterMotion.Sheet>(Resources.Load<TextAsset>(path).text.TrimStart('\uFEFF'));
  var texture=Resources.Load<Sprite>(path).texture;parts=new Sprite[5][];
  for(int row=0;row<5;row++){
   var clip=sheet.clips[row];float sx=(float)texture.width/clip.textureWidth,sy=(float)texture.height/clip.textureHeight;parts[row]=new Sprite[3];
   for(int col=0;col<3;col++){
    var f=clip.frames[col];parts[row][col]=Sprite.Create(texture,new Rect(f.x*sx,f.y*sy,f.width*sx,f.height*sy),new Vector2(col==0?.25f:col==1?.08f:.5f,.5f),100,0,SpriteMeshType.FullRect);
   }
  }
 }
 void SetVisible(bool visible){
  weapon.enabled=visible;flash.enabled=false;reloadProp.enabled=false;
  for(int side=0;side<2;side++)for(int part=0;part<3;part++)arms[side,part].enabled=visible;
 }
 public void OnEquip(){
  fireAt=-100;equipAt=Time.time;SetVisible(true);
  weapon.sprite=PrototypeArt.Load("Sprites/Shared/"+player.Profile.Sprite);
  float scale=player.Profile.Width/weapon.sprite.bounds.size.x;weapon.transform.localScale=Vector3.one*scale;
  reloadProp.sprite=PrototypeArt.Load("Sprites/Shared/"+(player.Weapon==2?"Bullet":"AmmoBox"));
  SetSize(reloadProp,player.Weapon==2?new Vector2(.15f,.06f):new Vector2(.1f,.2f));
  Pose();
 }
 public void OnFire(){fireAt=Time.time;}
 void LateUpdate(){
  if(!PrototypeGame.IsPlaying)return;
  if(player.Mounted){
   SetVisible(true);weapon.enabled=false;
   var gun=player.Mounted;weaponRotation=Quaternion.Euler(0,0,Mathf.Atan2(gun.Aim.y,gun.Aim.x)*Mathf.Rad2Deg);
   TriggerHand=gun.TriggerGrip;SupportHand=gun.SupportGrip;
   DrawArm(0,player.Motion.ShoulderPosition,SupportHand,gun.direction);
   DrawArm(1,player.Motion.ShoulderPosition,TriggerHand,gun.direction);
   AnimationName=gun.Firing?"Mounted/Fire":"Mounted/Aim";return;
  }
  if(!player.HasWeapon){SetVisible(false);AnimationName="Unarmed";return;}
  weapon.enabled=true;
  clock+=Time.deltaTime;Pose();
 }
 void Pose(){
  var profile=player.Profile;if(profile==null)return;
  var state=player.Motion.State;float sign=player.FacingLeft?-1:1;
  Vector2 shoulder=player.Motion.ShoulderPosition;
  Vector2 aim=player.AimDirection;float aimAngle=Mathf.Atan2(aim.y,aim.x)*Mathf.Rad2Deg;
  float age=Time.time-fireAt;
  RecoilAmount=age<.24f?profile.Recoil*Mathf.Exp(-age*20):0;
  PumpAmount=player.Weapon==2&&age>.13f&&age<.55f?Mathf.Sin((age-.13f)/.42f*Mathf.PI)*.15f:0;
  float bob=0,sway=0;
  if(state==CharacterMotion.MotionState.Walk){bob=Mathf.Sin(clock*18)*profile.WalkBob;sway=Mathf.Sin(clock*9)*2;}
  if(state==CharacterMotion.MotionState.Run){bob=Mathf.Sin(clock*25)*profile.RunBob;sway=Mathf.Sin(clock*12.5f)*(player.Weapon==0?10:5);}
  if(state==CharacterMotion.MotionState.CrouchWalk){bob=Mathf.Sin(clock*14)*profile.WalkBob*.55f;sway=Mathf.Sin(clock*7)*2;}
  if(state==CharacterMotion.MotionState.Crawl){bob=Mathf.Sin(clock*12)*.012f;sway=Mathf.Sin(clock*6)*2;}
  if(state==CharacterMotion.MotionState.Idle)bob=Mathf.Sin(clock*3)*.008f;
  float jump=state==CharacterMotion.MotionState.Jump?Mathf.Clamp(player.Velocity.y,-12,12)*.3f:0;
  float equip=Mathf.Clamp01((Time.time-equipAt)/.28f);
  float tilt=(sway+jump+(1-equip)*-32+RecoilAmount/profile.Recoil*profile.Kick)*sign;
  float reload=player.ReloadProgress;
  if(player.Reloading)tilt+=(player.Weapon==0?38:player.Weapon==1?23:-18)*Mathf.Sin(reload*Mathf.PI)*sign;
  weaponRotation=Quaternion.Euler(0,0,aimAngle+tilt);
  // Prone places the muzzle forward of the helmet; long guns stay close to the shoulder.
  float reach=player.Stance==SoloPlayer.Posture.Prone?.44f:player.Weapon==0?.40f:.36f;
  center=shoulder+aim*(reach-RecoilAmount)+new Vector2(0,-.10f+bob);
  if(state==CharacterMotion.MotionState.Run)center+=Vector2.down*(player.Weapon==0?.08f:.04f);
  weapon.transform.position=center;weapon.transform.rotation=weaponRotation;weapon.flipY=sign<0;
  weapon.sortingOrder=player.Stance==SoloPlayer.Posture.Prone?20:24;
  TriggerHand=Point(profile.Grip,sign);SupportHand=Point(profile.Support+Vector2.left*PumpAmount,sign);
  if(player.Weapon==0)SupportHand=shoulder+new Vector2(-.14f*sign,(player.Stance==SoloPlayer.Posture.Prone?-.07f:-.42f)+Mathf.Sin(clock*9)*bob);
  if(player.Climbing){
   SupportHand=shoulder+new Vector2(-.12f*sign,.35f+Mathf.Sin(clock*8)*.13f);
  }
  if(player.Reloading){
   if(player.Weapon==2){float insert=Mathf.Sin(reload*Mathf.PI*4);SupportHand=Point(new Vector2(-.16f,-.13f-Mathf.Abs(insert)*.25f),sign);}
   else {float lower=Mathf.Sin(reload*Mathf.PI);SupportHand=Point(profile.Grip+new Vector2(0,-.1f-lower*.28f),sign);}
  }
  DrawArm(0,shoulder+new Vector2(.07f*sign,-.02f),SupportHand,sign);
  DrawArm(1,shoulder,TriggerHand,sign);
  MuzzlePosition=Point(new Vector2(profile.Width*.5f+.06f,.04f),sign);
  flash.enabled=age>=0&&age<.055f;flash.transform.position=MuzzlePosition;flash.transform.rotation=weaponRotation;
  reloadProp.enabled=player.Reloading&&reload>.12f&&reload<.88f;
  reloadProp.transform.position=SupportHand;reloadProp.transform.rotation=weaponRotation*Quaternion.Euler(0,0,player.Weapon==2?0:90);
  AnimationName=profile.Name+"/"+(player.Reloading?"Reload":PumpAmount>0?"Pump":age<.12f?"Fire":equip<1?"Equip":state.ToString());
 }
 Vector2 Point(Vector2 local,float sign)=>center+(Vector2)(weaponRotation*new Vector3(local.x,local.y*sign,0));
 void DrawArm(int side,Vector2 shoulder,Vector2 hand,float sign){
  var delta=hand-shoulder;float distance=Mathf.Max(.01f,delta.magnitude);var direction=delta/distance;
  bool prone=player.Stance==SoloPlayer.Posture.Prone;
  float length=Mathf.Max(prone?.23f:.30f,distance*.515f);float bend=Mathf.Sqrt(Mathf.Max(0,length*length-distance*distance*.25f));
  var elbow=(shoulder+hand)*.5f+new Vector2(-direction.y,direction.x)*(-sign*bend);
  if(prone)elbow.y=Mathf.Max(elbow.y,player.transform.position.y-.75f);
  DrawBone(arms[side,0],shoulder,elbow,(player.CharacterIndex==1?.25f:.20f)*(prone?.85f:1),sign,.63f);
  DrawBone(arms[side,1],elbow,hand,.145f*(prone?.85f:1),sign,.82f);
  var glove=arms[side,2];glove.transform.position=hand;glove.transform.rotation=weaponRotation;glove.flipY=sign<0;
  SetSize(glove,new Vector2(.16f,.13f));
 }
 static void DrawBone(SpriteRenderer sprite,Vector2 start,Vector2 end,float thickness,float sign,float jointSpan){
  var delta=end-start;sprite.transform.position=start;
  sprite.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);sprite.flipY=sign<0;
  SetSize(sprite,new Vector2(delta.magnitude/jointSpan,thickness));
 }
 static void SetSize(SpriteRenderer sprite,Vector2 size){sprite.transform.localScale=new Vector3(size.x/sprite.sprite.bounds.size.x,size.y/sprite.sprite.bounds.size.y,1);}
}
}
