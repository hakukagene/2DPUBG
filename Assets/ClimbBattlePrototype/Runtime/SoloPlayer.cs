using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
namespace ClimbBattle {
[RequireComponent(typeof(Rigidbody2D),typeof(CapsuleCollider2D))]
public sealed class SoloPlayer:MonoBehaviour {
 public enum Posture {Standing,Crouching,Prone}
 public Posture Stance {get;private set;}
 public CharacterMotion Motion {get;private set;}
 public WeaponMotion WeaponAnimation {get;private set;}
 public MountedGun Mounted {get;private set;}
 public Vector2 AimDirection {get;private set;}=Vector2.right;
 public bool HasWeapon=>WeaponCatalog.Get(Weapon)!=null;
 public WeaponProfile Profile=>WeaponCatalog.Get(Weapon);
 public float ReloadProgress=>Reloading?1-(reloadUntil-Time.time)/Profile.ReloadSeconds:0;
 public bool Sprinting {get;private set;}
 public Vector2 Velocity=>body?body.linearVelocity:Vector2.zero;
 public int Health=100,Shield,Ammo,Reserve,Weapon=WeaponCatalog.Unarmed;
 public bool Grounded,Climbing;public string WeaponName=>HasWeapon?Profile.Name:"UNARMED";
 public bool Reloading=>reloadUntil>Time.time;
 public int CharacterIndex {get;private set;}
 public SpriteRenderer CharacterRenderer {get;private set;}
 public bool FacingLeft=>visual&&visual.localScale.x<0;
 Rigidbody2D body;CapsuleCollider2D capsule;Transform visual;SpriteRenderer shieldVisual;
 float move,vertical,jumpUntil,groundUntil,nextShot,reloadUntil,climbLockUntil;
 readonly RaycastHit2D[] groundHits=new RaycastHit2D[8];
 readonly Collider2D[] clearanceHits=new Collider2D[32];
 Posture? requestedStance;
 public void Initialize(){
  body=GetComponent<Rigidbody2D>();body.gravityScale=3;body.constraints=RigidbodyConstraints2D.FreezeRotation;
  body.interpolation=RigidbodyInterpolation2D.Interpolate;body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
  capsule=GetComponent<CapsuleCollider2D>();capsule.size=new Vector2(.8f,1.7f);
  var friction=new PhysicsMaterial2D("PlayerNoFriction"){friction=0,bounciness=0};capsule.sharedMaterial=friction;
  visual=PrototypeArt.Node(transform,"PilotVisual",transform.position).transform;
  CharacterIndex=CharacterCatalog.Selected;
  var characterSprite=PrototypeArt.Load(CharacterCatalog.BodyPath(CharacterIndex));
  float fullHeight=1.85f/CharacterCatalog.BodyHeightFractions[CharacterIndex];
  float fullWidth=fullHeight*characterSprite.rect.width/characterSprite.rect.height;
  var skin=PrototypeArt.Sprite(visual,"CharacterBody",CharacterCatalog.BodyPath(CharacterIndex),transform.position,new Vector2(fullWidth,fullHeight),21);
  skin.transform.localPosition=new Vector3(0,-.85f+(.5f-CharacterCatalog.BodyBottomMargins[CharacterIndex])*fullHeight,0);
  CharacterRenderer=skin.GetComponent<SpriteRenderer>();
  Motion=gameObject.AddComponent<CharacterMotion>();Motion.Initialize(this,CharacterRenderer);
  WeaponAnimation=gameObject.AddComponent<WeaponMotion>();WeaponAnimation.Initialize(this);
  var s=PrototypeArt.Sprite(transform,"Shield","Sprites/Shared/ShieldBubble",transform.position,new Vector2(2.4f,2.4f),26);
  shieldVisual=s.GetComponent<SpriteRenderer>();shieldVisual.enabled=false;
 }
 void Update(){
  if(!PrototypeGame.IsPlaying)return;
  var k=Keyboard.current;if(k==null)return;
  shieldVisual.enabled=Shield>0;
  if(Mounted){var gun=Mounted;gun.Control();if(Mounted){AimDirection=gun.Aim;visual.localScale=new Vector3(gun.direction,1,1);}return;}
  if(k.eKey.wasPressedThisFrame){
   MountedGun nearest=null;float distance=1.8f;
   foreach(var gun in FindObjectsByType<MountedGun>()){float d=Vector2.Distance(transform.position,gun.Seat);if(!gun.Rider&&d<distance){nearest=gun;distance=d;}}
   if(nearest&&nearest.Mount(this))return;
  }
  move=(k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0);
  vertical=(k.wKey.isPressed||k.upArrowKey.isPressed?1:0)-(k.sKey.isPressed||k.downArrowKey.isPressed?1:0);
  if(k.spaceKey.wasPressedThisFrame)jumpUntil=Time.time+.13f;
  if(k.cKey.wasPressedThisFrame)requestedStance=Stance==Posture.Crouching?Posture.Standing:Posture.Crouching;
  if(k.zKey.wasPressedThisFrame)requestedStance=Stance==Posture.Prone?Posture.Standing:Posture.Prone;
  if(k.rKey.wasPressedThisFrame)Reload();
  if(Reloading==false&&reloadUntil>0){int count=Mathf.Min(Capacity()-Ammo,Reserve);Ammo+=count;Reserve-=count;reloadUntil=0;}
  shieldVisual.transform.localPosition=new Vector3(0,capsule.offset.y,0);
  shieldVisual.transform.localScale=new Vector3((capsule.size.x+.8f)/shieldVisual.sprite.bounds.size.x,(capsule.size.y+.6f)/shieldVisual.sprite.bounds.size.y,1);
  if(Mouse.current!=null){
   var aim=(Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue())-(Vector2)Motion.ShoulderPosition;
   if(aim.sqrMagnitude>.01f)AimDirection=aim.normalized;
   visual.localScale=new Vector3(aim.x<0?-1:1,1,1);
   if(Mouse.current.leftButton.isPressed&&!(EventSystem.current&&EventSystem.current.IsPointerOverGameObject()))Shoot(aim.normalized);
  }
  shieldVisual.enabled=Shield>0;
  if(transform.position.y<-12)Hurt(999,true);
 }
 void FixedUpdate(){
  if(!PrototypeGame.IsPlaying){if(body)body.linearVelocity=Vector2.zero;return;}
  if(Mounted){body.gravityScale=0;body.linearVelocity=Vector2.zero;body.position=Mounted.Seat;Grounded=true;Climbing=false;Sprinting=false;return;}
  var filter=new ContactFilter2D{useTriggers=false};int n=capsule.Cast(Vector2.down,filter,groundHits,.12f);Grounded=false;
  for(int i=0;i<n;i++)if(groundHits[i].normal.y>.5f)Grounded=true;
  if(Grounded)groundUntil=Time.time+.1f;
  if(requestedStance.HasValue){
   if(Grounded&&!Climbing)TryPosture(requestedStance.Value);
   requestedStance=null;
  }
  bool ladder=false;
  foreach(var c in Physics2D.OverlapBoxAll(transform.position,new Vector2(.7f,1.5f),0)){
   var actor=c.GetComponent<WorldInteractable>();if(actor&&actor.kind==WorldInteractable.Kind.Ladder){ladder=true;break;}
  }
  if(!ladder)Climbing=false;
  if(ladder&&Mathf.Abs(vertical)>.1f&&Time.time>=climbLockUntil&&TryPosture(Posture.Standing,false))Climbing=true;
  Sprinting=Stance==Posture.Standing&&!Climbing&&Keyboard.current?.leftShiftKey.isPressed==true;
  float run=Stance==Posture.Prone?1.8f:Stance==Posture.Crouching?3.5f:Sprinting?10:7.5f;
  run*=TacticalTrap.MovementScale(this);
  var velocity=body.linearVelocity;velocity.x=move*run;
  body.gravityScale=Climbing?0:3;
  if(Climbing)velocity.y=vertical*4.8f;
  if(jumpUntil>=Time.time&&(Climbing||Time.time<=groundUntil)){
   if(TryPosture(Posture.Standing)){
    velocity.y=12.5f;Climbing=false;Grounded=false;body.gravityScale=3;groundUntil=0;climbLockUntil=Time.time+.25f;
   }
   jumpUntil=0;
  }
  body.linearVelocity=velocity;
 }
 bool TryPosture(Posture next,bool notify=true){
  if(next==Stance)return true;
  var size=next==Posture.Prone?new Vector2(1.5f,.5f):next==Posture.Crouching?new Vector2(.8f,1.05f):new Vector2(.8f,1.7f);
  var offset=new Vector2(0,-.85f+size.y*.5f);
  var direction=next==Posture.Prone?CapsuleDirection2D.Horizontal:CapsuleDirection2D.Vertical;
  // Keep feet fixed; query the complete target shape, including the wider prone body.
  int count=Physics2D.OverlapCapsule(body.position+offset,size-Vector2.one*.04f,direction,0,
   new ContactFilter2D{useTriggers=false},clearanceHits);
  bool blocked=count==clearanceHits.Length;
  for(int i=0;i<count;i++)if(clearanceHits[i]&&clearanceHits[i].attachedRigidbody!=body)blocked=true;
  if(blocked){if(notify)PrototypeGame.Instance.Notify("Not enough room to change posture");return false;}
  capsule.direction=direction;capsule.size=size;capsule.offset=offset;Stance=next;return true;
 }
 int Capacity()=>HasWeapon?Profile.Capacity:0;
 public bool BeginMount(MountedGun gun){
  if(!TryPosture(Posture.Standing))return false;
  Mounted=gun;Climbing=false;move=vertical=jumpUntil=0;requestedStance=null;reloadUntil=0;
  body.position=gun.Seat;body.linearVelocity=Vector2.zero;body.gravityScale=0;
  visual.localScale=new Vector3(gun.direction,1,1);return true;
 }
 public void EndMount(bool jump){Mounted=null;if(!body)return;body.gravityScale=3;body.linearVelocity=jump?Vector2.up*12.5f:Vector2.zero;if(jump){Grounded=false;groundUntil=0;}}
 public void Equip(int weapon){
  var next=WeaponCatalog.Get(weapon);if(next==null)return;
  bool same=Weapon==weapon;Weapon=weapon;Ammo=next.Capacity;Reserve=same?Reserve+next.PickupReserve:next.PickupReserve;reloadUntil=0;nextShot=0;
  WeaponAnimation.OnEquip();
 }
 void Reload(){if(HasWeapon&&!Reloading&&Ammo<Capacity()&&Reserve>0)reloadUntil=Time.time+Profile.ReloadSeconds;}
 void Shoot(Vector2 direction){
  if(!HasWeapon)return;
  if(Reloading||Time.time<nextShot)return;
  if(Ammo<=0){Reload();return;}Ammo--;nextShot=Time.time+Profile.ShotInterval;
  WeaponAnimation.OnFire();
  int pellets=Profile.Pellets;
  for(int i=0;i<pellets;i++){
   Vector2 dir=Quaternion.Euler(0,0,pellets==1?0:(i-2)*5)*direction;
   var pos=WeaponAnimation.MuzzlePosition;
   var shot=PrototypeArt.Sprite(null,"Projectile","Sprites/Shared/Bullet",pos,new Vector2(.4f,.14f),24);
   shot.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(dir.y,dir.x)*Mathf.Rad2Deg);
   var p=shot.AddComponent<Projectile>();p.owner=this;p.velocity=dir*32;p.damage=Profile.Damage;
  }
 }
 public void Hurt(int amount,bool environmental=false){
  if(!PrototypeGame.IsPlaying)return;
  if(!environmental){int absorb=Mathf.Min(Shield,amount);Shield-=absorb;amount-=absorb;}
  Health=Mathf.Max(0,Health-amount);if(Health==0)PrototypeGame.Instance.Finish(false);
 }
}
}
