using UnityEngine;
namespace ClimbBattle {
public sealed class FollowCamera:MonoBehaviour {
 public Transform target;public float mapWidth=68;public bool overview;
 Camera cam;
 void Awake(){cam=GetComponent<Camera>();}
 void LateUpdate(){
  if(!target)return;
  float zoom=overview?27:7.8f;
  cam.orthographicSize=Mathf.Lerp(cam.orthographicSize,zoom,1-Mathf.Exp(-Time.unscaledDeltaTime*7));
  float edge=Mathf.Max(0,mapWidth/2-cam.orthographicSize*cam.aspect+4);
  var goal=overview?new Vector3(0,20,-10):new Vector3(Mathf.Clamp(target.position.x,-edge,edge),Mathf.Clamp(target.position.y+2.3f,4,40),-10);
  transform.position=Vector3.Lerp(transform.position,goal,1-Mathf.Exp(-Time.unscaledDeltaTime*6));
 }
}
}
