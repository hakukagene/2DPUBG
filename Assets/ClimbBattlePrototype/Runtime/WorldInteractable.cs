using UnityEngine;
namespace ClimbBattle {
public sealed class WorldInteractable:MonoBehaviour {
 // Append new kinds to preserve the enum values in existing baked scenes.
 public enum Kind { Ladder,Cover,Health,Shield,Ammo,Rifle,Shotgun,Target,Goal,Lift,Pistol }
 public Kind kind;public float travel=3,speed=.8f;
 public int health=100;Vector3 origin;float age;SpriteRenderer visual;Rigidbody2D body;
 void Awake(){origin=transform.position;visual=GetComponent<SpriteRenderer>();body=GetComponent<Rigidbody2D>();}
 void Start(){body=GetComponent<Rigidbody2D>();}
 void FixedUpdate(){
  if(kind!=Kind.Lift||!PrototypeGame.IsPlaying)return;
  age+=Time.fixedDeltaTime;
  var desired=(Vector2)origin+Vector2.up*((1-Mathf.Cos(age*speed))*travel*.5f);
  if(body) body.MovePosition(desired);else transform.position=desired;
 }
 void Update(){
  if(!PrototypeGame.IsPlaying)return;
  if(kind==Kind.Cover&&visual&&PrototypeGame.Instance.Player) {
   var p=PrototypeGame.Instance.Player.transform.position;
   float alpha=Mathf.Abs(p.x-transform.position.x)<1.8f&&Mathf.Abs(p.y-transform.position.y)<2.3f?.22f:1;
   var c=visual.color;c.a=Mathf.Lerp(c.a,alpha,Time.deltaTime*12);visual.color=c;
  }
 }
 void OnTriggerEnter2D(Collider2D other){
  var player=other.GetComponent<SoloPlayer>();if(!player||!PrototypeGame.IsPlaying)return;
  bool used=true;
  switch(kind) {
   case Kind.Health:if(player.Health>=100)used=false;else player.Health=Mathf.Min(100,player.Health+35);break;
   case Kind.Shield:player.Shield=60;break;
   case Kind.Ammo:player.Reserve+=40;break;
   case Kind.Pistol:player.Equip(0);break;
   case Kind.Rifle:player.Equip(1);break;
   case Kind.Shotgun:player.Equip(2);break;
   default:used=false;break;
  }
  if(used){PrototypeGame.Instance.Notify("PICKUP  /  "+kind);Destroy(gameObject);}
 }
 void OnTriggerStay2D(Collider2D other){
  if(kind==Kind.Goal&&other.GetComponent<SoloPlayer>()&&PrototypeGame.IsPlaying) {
   PrototypeGame.Instance.NearGoal=true;
   if(UnityEngine.InputSystem.Keyboard.current?.eKey.wasPressedThisFrame==true)PrototypeGame.Instance.Finish(true);
  }
 }
 public void Damage(int damage) {
  if(kind!=Kind.Target)return;health-=damage;PrototypeGame.Instance.Notify("TARGET HIT  /  "+Mathf.Max(0,health)+" HP");
  if(health<=0){PrototypeGame.Instance.TargetsDestroyed++;Destroy(gameObject);}
 }
}
}
