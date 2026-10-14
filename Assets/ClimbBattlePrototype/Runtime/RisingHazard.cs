using UnityEngine;
namespace ClimbBattle {
public sealed class RisingHazard:MonoBehaviour {
 public int mapIndex;public float graceSeconds=22,riseSpeed=.22f;
 float elapsed;float nextDamage;
 public float Surface=>transform.position.y+.85f;
 public float GraceLeft=>Mathf.Max(0,graceSeconds-elapsed);
 void Update(){
  if(!PrototypeGame.IsPlaying||!PrototypeGame.HazardEnabled)return;
  elapsed+=Time.deltaTime;
  if(elapsed>graceSeconds)transform.position+=Vector3.up*riseSpeed*Time.deltaTime;
  var p=PrototypeGame.Instance.Player;
  if(p&&p.transform.position.y-.75f<Surface&&Time.time>=nextDamage){
   nextDamage=Time.time+.35f;p.Hurt(mapIndex==1?28:mapIndex==3?12:10,true);
  }
 }
}
}
