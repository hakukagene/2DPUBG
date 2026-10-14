using UnityEngine;
namespace ClimbBattle {
public sealed class WeaponProfile {
 public readonly string Name,Sprite;
 public readonly int Capacity,PickupReserve,Pellets,Damage;
 public readonly float Width,ShotInterval,ReloadSeconds,Recoil,Kick,WalkBob,RunBob;
 public readonly Vector2 Grip,Support;
 public WeaponProfile(string name,string sprite,int capacity,int reserve,int pellets,int damage,float width,float interval,float reload,float recoil,float kick,float walkBob,float runBob,Vector2 grip,Vector2 support){
  Name=name;Sprite=sprite;Capacity=capacity;PickupReserve=reserve;Pellets=pellets;Damage=damage;
  Width=width;ShotInterval=interval;ReloadSeconds=reload;Recoil=recoil;Kick=kick;WalkBob=walkBob;RunBob=runBob;Grip=grip;Support=support;
 }
}
public static class WeaponCatalog {
 public const int Unarmed=-1;
 public static readonly WeaponProfile[] All={
  new WeaponProfile("PISTOL","Pistol",12,36,1,35,.72f,.25f,1.1f,.10f,10,.025f,.075f,new Vector2(-.15f,-.13f),Vector2.zero),
  new WeaponProfile("RIFLE","Rifle",30,60,1,35,1.18f,.11f,1.5f,.055f,4,.018f,.045f,new Vector2(-.12f,-.11f),new Vector2(.28f,-.06f)),
  new WeaponProfile("SHOTGUN","Shotgun",6,18,5,25,1.12f,.7f,2.0f,.19f,15,.03f,.055f,new Vector2(-.16f,-.1f),new Vector2(.26f,-.05f))
 };
 public static WeaponProfile Get(int id)=>id>=0&&id<All.Length?All[id]:null;
}
}
