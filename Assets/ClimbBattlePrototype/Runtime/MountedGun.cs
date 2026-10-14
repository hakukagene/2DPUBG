using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
namespace ClimbBattle {
public sealed class MountedGun:MonoBehaviour {
 public int mapIndex,direction=1,Ammo=180;
 public SpriteRenderer barrel,flash,heatBar;
 public float Heat {get;private set;}
 public bool Overheated {get;private set;}
 public SoloPlayer Rider {get;private set;}
 public int ShotsFired {get;private set;}
 public Vector2 Aim {get;private set;}=Vector2.right;
 public Vector2 Seat=>(Vector2)transform.position+new Vector2(-direction*.65f,.85f);
 public Vector2 Pivot=>(Vector2)transform.position+new Vector2(direction*.15f,1.32f);
 public Vector2 TriggerGrip=>Pivot-Aim*.55f+Vector2.down*.12f;
 public Vector2 SupportGrip=>Pivot+Aim*.35f+Vector2.down*.08f;
 public bool Firing=>Time.time-lastShot<.12f;
 float nextShot,lastShot=-100;
 void Start(){Aim=Vector2.right*direction;}
 public bool Mount(SoloPlayer player){
  if(Rider||player.Mounted||!player.Grounded||Vector2.Distance(player.transform.position,Seat)>1.8f)return false;
  if(!player.BeginMount(this))return false;Rider=player;Aim=Vector2.right*direction;return true;
 }
 public void Dismount(bool jump=false){var rider=Rider;Rider=null;if(rider)rider.EndMount(jump);}
 void OnDisable(){Dismount();}
 public void Control(){
  if(!Rider||!PrototypeGame.IsPlaying)return;
  if(Keyboard.current?.eKey.wasPressedThisFrame==true||Keyboard.current?.spaceKey.wasPressedThisFrame==true){Dismount(Keyboard.current.spaceKey.wasPressedThisFrame);return;}
  if(Mouse.current==null)return;
  Vector2 delta=(Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue())-Pivot;
  float angle=Mathf.Clamp(Mathf.Atan2(delta.y,delta.x*direction)*Mathf.Rad2Deg,-35,35);
  Aim=new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad)*direction,Mathf.Sin(angle*Mathf.Deg2Rad));
  if(Mouse.current.leftButton.isPressed&&!(EventSystem.current&&EventSystem.current.IsPointerOverGameObject()))Fire();
 }
 public bool Fire(){
  if(!PrototypeGame.IsPlaying||!Rider||Overheated||Ammo<=0||Time.time<nextShot)return false;
  nextShot=Time.time+(mapIndex==1?.075f:.10f);lastShot=Time.time;Ammo--;ShotsFired++;
  Heat=Mathf.Min(1,Heat+.055f);if(Heat>=.999f)Overheated=true;
  var shot=PrototypeArt.Sprite(null,"MountedRound","Sprites/Shared/Bullet",Pivot+Aim*1.3f,new Vector2(.55f,.14f),28);
  shot.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(Aim.y,Aim.x)*Mathf.Rad2Deg);
  if(mapIndex==3)shot.GetComponent<SpriteRenderer>().color=Color.cyan;
  var p=shot.AddComponent<Projectile>();p.owner=Rider;p.velocity=Aim*42;p.damage=mapIndex==1?40:30;
  return true;
 }
 void Update(){
  if(!PrototypeGame.IsPlaying)return;
  if(Time.time-lastShot>.2f)Heat=Mathf.Max(0,Heat-Time.deltaTime*.24f);
  if(Overheated&&Heat<=.3f)Overheated=false;
  if(barrel){barrel.transform.position=Pivot-Aim*(Firing?.06f:0);barrel.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(Aim.y,Aim.x)*Mathf.Rad2Deg);barrel.flipY=direction<0;}
  if(flash){flash.enabled=Time.time-lastShot<.055f;flash.transform.position=Pivot+Aim*1.35f;flash.transform.rotation=barrel.transform.rotation;}
  if(heatBar){TacticalArt.Size(heatBar,new Vector2(Mathf.Max(.02f,Heat),.07f));heatBar.color=Overheated?Color.red:Color.Lerp(Color.cyan,Color.red,Heat);}
 }
}
}
