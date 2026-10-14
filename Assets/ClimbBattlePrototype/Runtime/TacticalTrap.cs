using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace ClimbBattle {
public sealed class TacticalTrap:MonoBehaviour {
 public enum TrapKind {Laser,Shock,Steam,Press,Breakaway,Swing,Darts,Sand}
 public enum Phase {Idle,Warning,Active,Cooldown,Disabled}
 public TrapKind kind;
 public Vector2 area=new Vector2(3,1.8f),areaOffset=new Vector2(0,.9f),switchOffset=new Vector2(-2.5f,1);
 public float idleSeconds=2.5f,warningSeconds=1.2f,activeSeconds=1.5f,cooldownSeconds=2.5f,phaseOffset;
 public int damage=20,mapIndex;
 public bool hasSwitch=true;
 public SpriteRenderer visual,effect,warning,switchVisual;
 public Transform movingPart;
 public Collider2D breakawayCollider;
 public Phase State {get;private set;}
 public float PhaseTime {get;private set;}
 public float DisabledUntil {get;private set;}
 public Vector2 Center=>(Vector2)transform.position+areaOffset;
 public Vector2 SwitchPosition=>(Vector2)transform.position+switchOffset;
 public static readonly List<TacticalTrap> All=new List<TacticalTrap>();
 Vector3 movingOrigin;float nextDamage;bool started;
 void OnEnable(){if(!All.Contains(this))All.Add(this);}
 void OnDisable(){All.Remove(this);}
 void Start(){if(movingPart)movingOrigin=movingPart.localPosition;PhaseTime=-phaseOffset;started=true;}
 public bool Contains(SoloPlayer player){return player&&player.GetComponent<Collider2D>().bounds.Intersects(new Bounds(Center,area));}
 public static float MovementScale(SoloPlayer player){foreach(var trap in All)if(trap&&trap.kind==TrapKind.Sand&&trap.State==Phase.Active&&trap.Contains(player))return .42f;return 1;}
 public bool DisableFor(float seconds){if(!hasSwitch||State==Phase.Disabled)return false;DisabledUntil=Time.time+seconds;SetPhase(Phase.Disabled);return true;}
 void SetPhase(Phase phase){State=phase;PhaseTime=0;}
 void Update(){
  if(!PrototypeGame.IsPlaying||!started)return;
  var player=PrototypeGame.Instance.Player;
  if(hasSwitch&&player&&Vector2.Distance(player.transform.position,SwitchPosition)<1.6f&&Keyboard.current?.fKey.wasPressedThisFrame==true){
   if(DisableFor(6))PrototypeGame.Instance.Notify("TRAP DISABLED / 6 SECONDS");
  }
  PhaseTime+=Time.deltaTime;
  if(State==Phase.Disabled){if(Time.time>=DisabledUntil)SetPhase(Phase.Idle);}
  else if(kind==TrapKind.Sand){if(State!=Phase.Active)SetPhase(Phase.Active);}
  else switch(State){
   case Phase.Idle:
    bool triggered=kind==TrapKind.Breakaway||kind==TrapKind.Darts;
    if(PhaseTime>=idleSeconds&&(!triggered||Contains(player)))SetPhase(Phase.Warning);break;
   case Phase.Warning:if(PhaseTime>=warningSeconds)SetPhase(Phase.Active);break;
   case Phase.Active:if(PhaseTime>=activeSeconds)SetPhase(Phase.Cooldown);break;
   case Phase.Cooldown:
    if(PhaseTime>=cooldownSeconds&&(kind!=TrapKind.Breakaway||!Contains(player)))SetPhase(Phase.Idle);break;
  }
  Animate();
  if(State==Phase.Active&&kind!=TrapKind.Sand&&kind!=TrapKind.Breakaway&&Time.time>=nextDamage&&Hits(player)){
   nextDamage=Time.time+.65f;player.Hurt(damage);PrototypeGame.Instance.Notify("TRAP HIT / "+kind.ToString().ToUpper());
  }
 }
 bool Hits(SoloPlayer player){
  if(!Contains(player))return false;
  if(kind==TrapKind.Press)return PhaseTime>.25f&&PhaseTime<activeSeconds-.2f;
  if(kind==TrapKind.Swing){var b=player.GetComponent<Collider2D>().bounds;return visual&&b.Intersects(visual.bounds);}
  if(kind==TrapKind.Darts){
   // All darts travel above crouch height; collision follows the visible flight lane.
   return player.GetComponent<Collider2D>().bounds.max.y>transform.position.y+1.15f;
  }
  return true;
 }
 void Animate(){
  bool active=State==Phase.Active,telegraph=State==Phase.Warning;
  if(warning){warning.enabled=telegraph;var c=warning.color;c.a=.2f+Mathf.PingPong(PhaseTime*2,.4f);warning.color=c;}
  if(effect){effect.enabled=active;
   if(kind==TrapKind.Darts)effect.transform.position=(Vector2)transform.position+new Vector2(-area.x*.5f+Mathf.Repeat(PhaseTime*9,area.x),1.3f);
   if(kind==TrapKind.Sand)effect.color=new Color(1,1,1,.65f+Mathf.Sin(PhaseTime*3)*.2f);
  }
  if(visual)visual.color=State==Phase.Disabled?new Color(.35f,.5f,.55f):telegraph?Color.Lerp(Color.white,new Color(1,.4f,.2f),Mathf.PingPong(PhaseTime*4,1)):Color.white;
  if(switchVisual)switchVisual.color=State==Phase.Disabled?Color.cyan:Color.white;
  if(kind==TrapKind.Shock&&visual)visual.color=active?Color.white:new Color(.35f,.45f,.5f);
  if(movingPart){
   if(kind==TrapKind.Press){float drop=active?Mathf.Clamp01(PhaseTime/.25f)*2.5f:State==Phase.Cooldown?Mathf.Max(0,1-PhaseTime/.65f)*2.5f:0;movingPart.localPosition=movingOrigin+Vector3.down*drop;}
   if(kind==TrapKind.Swing){float swing=active?Mathf.Sin(PhaseTime/activeSeconds*Mathf.PI*2)*37:0;movingPart.localRotation=Quaternion.Euler(0,0,swing);}
   if(kind==TrapKind.Breakaway){bool falling=active||State==Phase.Cooldown;float fallTime=PhaseTime+(State==Phase.Cooldown?activeSeconds:0);movingPart.localPosition=movingOrigin+Vector3.down*(falling?Mathf.Min(fallTime*6,5):0);if(visual)visual.enabled=!falling||fallTime<.6f;if(breakawayCollider)breakawayCollider.enabled=!falling;}
  }
 }
}
}
