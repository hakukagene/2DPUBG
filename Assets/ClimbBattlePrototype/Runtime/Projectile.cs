using UnityEngine;
namespace ClimbBattle {
public sealed class Projectile:MonoBehaviour {
 public Vector2 velocity;public SoloPlayer owner;public int damage=35;float lifetime=2;
 void Update(){
  if(!PrototypeGame.IsPlaying)return;
  Vector2 start=transform.position,delta=velocity*Time.deltaTime;
  foreach(var hit in Physics2D.RaycastAll(start,delta.normalized,delta.magnitude)) {
   if(hit.collider.isTrigger||hit.collider.GetComponent<SoloPlayer>()==owner)continue;
   var target=hit.collider.GetComponent<WorldInteractable>();if(target)target.Damage(damage);
   Destroy(gameObject);return;
  }
  transform.position+=(Vector3)delta;lifetime-=Time.deltaTime;if(lifetime<=0)Destroy(gameObject);
 }
}
}
