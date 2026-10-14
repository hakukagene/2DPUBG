#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.Object;

namespace ClimbBattle {
// Runs from the 2DPUBG menu or -executeMethod; excluded from player builds.
[InitializeOnLoad]
public static class PrototypePlayCheck {
 const string ActiveKey="ClimbBattle.PlayCheck";
 const string Output="Logs/PrototypeChecks";
 static readonly List<string> passed=new List<string>();
 static readonly List<string> errors=new List<string>();
 static Keyboard keyboard; static Mouse mouse;
 static readonly List<InputDevice> suspendedDevices=new List<InputDevice>();
 static bool completed;
 static IEnumerator flow;
 static WaitForSecondsRealtime pendingWait;
 static InputSettings.BackgroundBehavior previousBackground;
 static InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
 static bool previousRunInBackground;
 static PrototypePlayCheck(){EditorApplication.playModeStateChanged+=OnPlayState;}
 [MenuItem("2DPUBG/Run Play Mode Checks",priority=20)]
 public static void Run() {
  for(int i=0;i<SceneManager.sceneCount;i++)
   if(SceneManager.GetSceneAt(i).isDirty){Debug.LogWarning("Save open scenes before running prototype checks.");return;}
  Editor.PrototypeSetup.BuildMissing();
  Editor.TacticalSetup.Apply();
  Editor.PrototypeSetup.Validate();
  EditorSceneManager.OpenScene(MapCatalog.SceneFolder+MapCatalog.SceneName(-1)+".unity");
  Directory.CreateDirectory(Output);
  SessionState.SetBool(ActiveKey+".hadCharacter",PlayerPrefs.HasKey(CharacterCatalog.PreferenceKey));
  SessionState.SetInt(ActiveKey+".character",PlayerPrefs.GetInt(CharacterCatalog.PreferenceKey,0));
  SessionState.SetBool(ActiveKey,true);
  EditorApplication.isPlaying=true;
 }
 static void OnPlayState(PlayModeStateChange state) {
  if(state!=PlayModeStateChange.EnteredPlayMode)return;
  if(!SessionState.GetBool(ActiveKey,false))return;
  SessionState.SetBool(ActiveKey,false);
  Start();
 }
 static void Start() {
  passed.Clear();errors.Clear();completed=false;pendingWait=null;
  previousRunInBackground=Application.runInBackground;
  previousBackground=InputSystem.settings.backgroundBehavior;
  previousEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
  Application.runInBackground=true;
  InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
  InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
  suspendedDevices.Clear();
  foreach(var device in InputSystem.devices.ToArray())if(device.enabled&&(device is Keyboard||device is Mouse)){
   suspendedDevices.Add(device);InputSystem.DisableDevice(device);
  }
  keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();
  Application.logMessageReceived+=OnLog;
  flow=CheckFlow();EditorApplication.update+=Tick;
 }
 static void OnLog(string message,string stack,LogType type) {
  if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+stack);
 }
 static void Tick() {
  try {
   if(pendingWait!=null&&pendingWait.keepWaiting)return;
   pendingWait=null;
   if(!flow.MoveNext()){Finish(errors.Count==0?null:string.Join("\n",errors));return;}
   pendingWait=flow.Current as WaitForSecondsRealtime;
  }catch(Exception exception){Finish(exception.ToString());}
 }
 static WaitForSecondsRealtime Wait(float seconds)=>new WaitForSecondsRealtime(seconds);
 static void Require(bool condition,string description){if(!condition)throw new Exception(description);passed.Add(description);}
 static void Keys(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));}
 static void Place(Vector2 position) {
  var player=PrototypeGame.Instance.Player;var body=player.GetComponent<Rigidbody2D>();
  body.position=position;body.linearVelocity=Vector2.zero;player.transform.position=position;
  Physics2D.SyncTransforms();
 }
 static Button FindButton(string text)=>FindObjectsByType<Button>()
  .First(b=>b.GetComponentsInChildren<Text>().Any(t=>t.text==text||t.text.Contains(" / "+text)));
 static void Click(string text) {
  var button=FindButton(text);var rect=(RectTransform)button.transform;
  var data=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,
   position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center))};
  var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
  Require(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,"Clickable UI: "+text);
  ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);
 }
 static void Capture(string name) {
  // Use a temporary target so verification does not depend on the Game view size.
  var camera=Camera.main;var canvas=FindObjectsByType<Canvas>().First();
  var texture=new RenderTexture(1280,720,24);var previous=RenderTexture.active;
  var target=camera.targetTexture;var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;
  canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
  camera.targetTexture=texture;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=texture;
  var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
  image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
  File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());
  camera.targetTexture=target;canvas.renderMode=mode;canvas.worldCamera=oldCamera;
  RenderTexture.active=previous;texture.Release();Destroy(texture);Destroy(image);
 }
 static void CapturePose(string name){
  var camera=Camera.main;float zoom=camera.orthographicSize;var position=camera.transform.position;
  camera.orthographicSize=2.5f;camera.transform.position=PrototypeGame.Instance.Player.transform.position+new Vector3(0,.4f,-10);
  Capture(name);camera.orthographicSize=zoom;camera.transform.position=position;camera.ResetProjectionMatrix();camera.ResetWorldToCameraMatrix();
 }
 static IEnumerator CheckFlow() {
  yield return Wait(1);
  Require(PrototypeGame.Instance.CurrentState==PrototypeGame.State.MainMenu,"Main menu opened");
  Capture("01-main-menu");Click("START");yield return Wait(.3f);
  Require(PrototypeGame.Instance.CurrentState==PrototypeGame.State.CharacterSelect,"Start opens character selection");
  Capture("02-character-selection");
  PrototypeGame.HazardEnabled=false;
  for(int character=0;character<CharacterCatalog.Keys.Length;character++) {
   int map=character%4;
   Click(CharacterCatalog.Names[character]);yield return Wait(.15f);
   Require(CharacterCatalog.Selected==character&&PlayerPrefs.GetInt(CharacterCatalog.PreferenceKey)==character,"Character selection saved: "+CharacterCatalog.Names[character]);
   Click("CONTINUE");yield return Wait(.2f);
   Require(PrototypeGame.Instance.CurrentState==PrototypeGame.State.MapSelect,"Character confirmation opens maps");
   if(character==0)Capture("02-map-selection");
   Click(MapCatalog.Names[map]);yield return Wait(1.2f);
   var game=PrototypeGame.Instance;var player=game.Player;
   Require(SceneManager.GetActiveScene().name==MapCatalog.SceneName(map),"Loaded "+MapCatalog.Keys[map]);
   Require(FindObjectsByType<SoloPlayer>().Length==1,"One player in "+MapCatalog.Keys[map]);
   Require(player.CharacterIndex==character&&player.Motion.Ready&&player.CharacterRenderer.sprite.name.StartsWith(CharacterCatalog.Keys[character]),"Animated selected character spawned: "+CharacterCatalog.Names[character]);
   Require(player.GetComponent<CapsuleCollider2D>().size==new Vector2(.8f,1.7f),"Consistent character collider");
   Require(!player.HasWeapon&&player.Weapon==-1&&player.Ammo==0&&player.Reserve==0&&!player.WeaponAnimation.Visible&&!player.Motion.ArmedBody,"Starts unarmed with no weapon or ammo");
   InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(500,300)}.WithButton(MouseButton.Left));Keys(Key.R);yield return Wait(.2f);
   InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(500,300)});Keys();yield return Wait(.1f);
   Require(!player.Reloading&&FindObjectsByType<Projectile>().Length==0,"Unarmed fire and reload do nothing");
   Capture("06-character-"+CharacterCatalog.Keys[character]);
   var root=GameObject.Find("Map_"+MapCatalog.Keys[map]);
   Require(root&&root.transform.Cast<Transform>().Count(t=>t.name.StartsWith("Floor_"))==7,"Seven floors in "+MapCatalog.Keys[map]);
   Require(player.Grounded&&player.transform.position.y>.5f,"Player lands on floor");
   Capture("03-"+MapCatalog.Keys[map]+"-play");
   var follow=Camera.main.GetComponent<FollowCamera>();follow.overview=true;yield return Wait(.8f);
   Capture("04-"+MapCatalog.Keys[map]+"-overview");follow.overview=false;
   float x=player.transform.position.x;Keys(Key.D);yield return Wait(.17f);
   Require(player.Motion.State==CharacterMotion.MotionState.Walk,"Walk animation");
   var firstFrame=player.CharacterRenderer.sprite;yield return Wait(.15f);
   Require(firstFrame!=player.CharacterRenderer.sprite,"Walk advances sprite frames");
   CapturePose("07-walk-"+character);Keys();yield return Wait(.1f);
   Require(player.transform.position.x>x+1,"Keyboard movement: "+x+" -> "+player.transform.position.x);
   InputSystem.QueueStateEvent(mouse,new MouseState{position=Camera.main.WorldToScreenPoint(player.transform.position+Vector3.left*4)});yield return Wait(.15f);
   Require(player.FacingLeft,"Character faces left");
   InputSystem.QueueStateEvent(mouse,new MouseState{position=Camera.main.WorldToScreenPoint(player.transform.position+Vector3.right*4)});yield return Wait(.15f);
   Require(!player.FacingLeft,"Character faces right (mouse="+Mouse.current?.displayName+", screen="+Mouse.current?.position.ReadValue()+")");
   Place(new Vector2(-7,1));yield return Wait(.3f);CapturePose("22-"+character+"-unarmed-standing");
   Keys(Key.C);yield return Wait(.1f);Keys();yield return Wait(.1f);
   Require(player.Motion.State==CharacterMotion.MotionState.Crouch&&!player.Motion.ArmedBody,"Unarmed crouch pose");
   CapturePose("23-"+character+"-unarmed-crouch");
   Keys(Key.Z);yield return Wait(.1f);Keys();yield return Wait(.1f);
   Require(player.Motion.State==CharacterMotion.MotionState.Prone&&!player.Motion.ArmedBody,"Unarmed prone pose");
   CapturePose("24-"+character+"-unarmed-prone");
   Keys(Key.Z);yield return Wait(.1f);Keys();yield return Wait(.1f);
   var pistol=FindObjectsByType<WorldInteractable>().First(a=>a.kind==WorldInteractable.Kind.Pistol);
   Place(pistol.transform.position);yield return Wait(.4f);
   Require(player.Weapon==0&&player.Ammo==12&&player.WeaponAnimation.Visible&&player.Motion.ArmedBody,"First-floor pistol pickup enables armed animation");
   Place(new Vector2(-3,1));yield return Wait(.3f);
   Keys(Key.D,Key.LeftShift);yield return Wait(.25f);
   Require(player.Motion.State==CharacterMotion.MotionState.Run&&player.Velocity.x>9,"Sprint speed and run animation");
   CapturePose("08-run-"+character);Keys();yield return Wait(.1f);
   // Stay clear of the moving lift at the center when testing unobstructed jumps.
   Place(new Vector2(-7,1));yield return Wait(.3f);
   Keys(Key.C);yield return Wait(.1f);Keys();yield return Wait(.1f);
   var capsule=player.GetComponent<CapsuleCollider2D>();float feet=capsule.bounds.min.y;
   Require(player.Stance==SoloPlayer.Posture.Crouching&&Mathf.Abs(capsule.size.y-1.05f)<.01f,"C crouches and lowers hitbox");
   Require(player.Motion.State==CharacterMotion.MotionState.Crouch,"Stationary crouch pose");
   CapturePose("09-crouch-"+character);
   Keys(Key.D,Key.LeftShift);yield return Wait(.25f);
   Require(player.Motion.State==CharacterMotion.MotionState.CrouchWalk&&Mathf.Abs(player.Velocity.x-3.5f)<.1f,"Crouch walk has reduced speed even with Shift");
   Keys();yield return Wait(.1f);
   Keys(Key.Z);yield return Wait(.1f);Keys();yield return Wait(.1f);
   Require(player.Stance==SoloPlayer.Posture.Prone&&capsule.direction==CapsuleDirection2D.Horizontal&&capsule.size==new Vector2(1.5f,.5f),"Z enters prone with horizontal hitbox");
   Require(Mathf.Abs(capsule.bounds.min.y-feet)<.08f,"Postures preserve ground contact");
   Require(player.Motion.State==CharacterMotion.MotionState.Prone,"Stationary prone pose");
   CapturePose("10-prone-"+character);
   Keys(Key.D,Key.LeftShift);yield return Wait(.25f);
   Require(player.Motion.State==CharacterMotion.MotionState.Crawl&&Mathf.Abs(player.Velocity.x-1.8f)<.1f,"Crawl speed and animation");
   Keys();yield return Wait(.1f);
   // A real solid ceiling leaves space for prone but not crouching or standing.
   var ceiling=new GameObject("Check_LowCeiling");var ceilingBox=ceiling.AddComponent<BoxCollider2D>();
   ceilingBox.size=new Vector2(4,.3f);ceiling.transform.position=new Vector2(player.transform.position.x,feet+.9f);Physics2D.SyncTransforms();
   Keys(Key.Z);yield return Wait(.1f);Keys();yield return Wait(.1f);
   Require(player.Stance==SoloPlayer.Posture.Prone,"Ceiling blocks standing from prone");
   Keys(Key.C);yield return Wait(.1f);Keys();yield return Wait(.1f);
   Require(player.Stance==SoloPlayer.Posture.Prone,"Ceiling blocks crouch when too low");
   Keys(Key.Space);yield return Wait(.1f);Keys();yield return Wait(.1f);
   Require(player.Stance==SoloPlayer.Posture.Prone&&Mathf.Abs(player.Velocity.y)<.5f,"Jump cannot expand into ceiling");
   int ammo=player.Ammo;Vector2 lowAim=Camera.main.WorldToScreenPoint(player.transform.position+new Vector3(4,-.6f,0));
   InputSystem.QueueStateEvent(mouse,new MouseState{position=lowAim}.WithButton(MouseButton.Left));yield return Wait(.1f);
   InputSystem.QueueStateEvent(mouse,new MouseState{position=lowAim});
   Require(player.Ammo<ammo,"Fire while prone under ceiling");
   Keys(Key.R);yield return Wait(.1f);Keys();yield return Wait(1.2f);
   Require(player.Ammo==12,"Reload while prone");
   Destroy(ceiling);yield return Wait(.1f);
   Keys(Key.Z);yield return Wait(.1f);Keys();yield return Wait(.15f);
   Require(player.Stance==SoloPlayer.Posture.Standing&&capsule.size==new Vector2(.8f,1.7f)&&capsule.offset==Vector2.zero,"Stand restores original collider");
   // Prone widens the body, so a close wall must block that transition too.
   var wall=new GameObject("Check_CloseWall");var wallBox=wall.AddComponent<BoxCollider2D>();wallBox.size=new Vector2(.2f,2);
   wall.transform.position=player.transform.position+new Vector3(.65f,0,0);Physics2D.SyncTransforms();
   Keys(Key.Z);yield return Wait(.1f);Keys();yield return Wait(.1f);
   Require(player.Stance==SoloPlayer.Posture.Standing,"Wall blocks widening prone hitbox");Destroy(wall);yield return Wait(.1f);
   Keys(Key.C);yield return Wait(.1f);Keys();yield return Wait(.1f);
   Require(player.Stance==SoloPlayer.Posture.Crouching&&player.Grounded,"Crouched on ground before jump");
   float y=player.transform.position.y;Keys(Key.Space);yield return Wait(.08f);Keys();yield return Wait(.2f);
   Require(player.transform.position.y>y+1&&player.Stance==SoloPlayer.Posture.Standing,"Stand and jump from crouch (y="+y+" -> "+player.transform.position.y+", stance="+player.Stance+", grounded="+player.Grounded+", velocity="+player.Velocity+")");
   Require(player.Motion.State==CharacterMotion.MotionState.Jump,"Jump animation");CapturePose("11-jump-"+character);
   Keys(Key.Z);yield return Wait(.1f);Keys();yield return Wait(.1f);
   Require(player.Stance==SoloPlayer.Posture.Standing,"Cannot enter prone midair");yield return Wait(.7f);
   float ladderX=-MapCatalog.Widths[map][1]/2+5;
   Place(new Vector2(ladderX,1.1f));Keys(Key.W);yield return Wait(1);Keys();yield return Wait(.1f);
   Require(player.Climbing&&player.transform.position.y>4,"Ladder climbing");
   Keys(Key.Space);yield return Wait(.08f);Keys();yield return Wait(.1f);
   Require(!player.Climbing,"Jump off ladder");
   var actors=FindObjectsByType<WorldInteractable>();
   var pickup=actors.First(a=>a.kind==WorldInteractable.Kind.Rifle);
   Place(pickup.transform.position);yield return Wait(.2f);
   Require(player.Weapon==1,"Weapon pickup equips rifle");
   var targetActor=actors.First(a=>a&&a.kind==WorldInteractable.Kind.Target&&a.transform.position.y<2);
   var targetPosition=(Vector2)targetActor.transform.position;
   Place(targetPosition+new Vector2(-2,.2f));yield return Wait(.4f);
   Vector2 aim=Camera.main.WorldToScreenPoint(targetPosition);
   float firingUntil=Time.realtimeSinceStartup+.8f;
   while(Time.realtimeSinceStartup<firingUntil){
    aim=Camera.main.WorldToScreenPoint(targetPosition);
    InputSystem.QueueStateEvent(mouse,new MouseState{position=aim}.WithButton(MouseButton.Left));yield return Wait(.04f);
   }
   InputSystem.QueueStateEvent(mouse,new MouseState{position=aim});
   Require(!targetActor&&game.TargetsDestroyed==1,"Mouse firing destroys target (remaining="+(targetActor?targetActor.health:0)+", destroyed="+game.TargetsDestroyed+")");
   var weaponChecks=CheckWeapons(player,character);
   while(weaponChecks.MoveNext())yield return weaponChecks.Current;
   var cover=actors.First(a=>a&&a.kind==WorldInteractable.Kind.Cover);
   Place(cover.transform.position);yield return Wait(.35f);
   Require(cover.GetComponent<SpriteRenderer>().color.a<.5f,"Cover fades behind player");
   var shield=actors.First(a=>a&&a.kind==WorldInteractable.Kind.Shield);
   Place(shield.transform.position);yield return Wait(.2f);
   Require(player.Shield==60,"Shield pickup");
   if(character<4){var tactical=CheckTactical(player,map);while(tactical.MoveNext())yield return tactical.Current;}
   Click("II");yield return Wait(.1f);
   Require(game.CurrentState==PrototypeGame.State.Paused&&Time.timeScale==0,"Pause");
   var pausedFrame=player.CharacterRenderer.sprite;yield return Wait(.3f);
   Require(pausedFrame==player.CharacterRenderer.sprite,"Pause freezes animation");
   var pausedWeapon=player.WeaponAnimation.Renderer.transform.position;yield return Wait(.2f);
   Require(pausedWeapon==player.WeaponAnimation.Renderer.transform.position,"Pause freezes weapon rig");
   Click("RESUME");yield return Wait(.1f);
   Require(game.CurrentState==PrototypeGame.State.Playing&&Time.timeScale==1,"Resume");
   var hazard=FindAnyObjectByType<RisingHazard>();
   PrototypeGame.HazardEnabled=true;hazard.graceSeconds=0;
   hazard.transform.position=new Vector2(0,player.transform.position.y+1);yield return Wait(.45f);
   Require(player.Health<100,"Rising hazard damages player");PrototypeGame.HazardEnabled=false;
   Place(new Vector2(0,37));yield return Wait(.1f);Keys(Key.E);yield return Wait(.1f);Keys();
   Require(game.CurrentState==PrototypeGame.State.Victory,"Summit completion");
   if(map==3) {
    Capture("05-summit");Click("PLAY AGAIN");yield return Wait(.8f);
    Require(PrototypeGame.Instance.CurrentState==PrototypeGame.State.Playing,"Retry restarts map");
    Require(PrototypeGame.Instance.Player.CharacterIndex==character,"Character persists through retry");
    Require(!PrototypeGame.Instance.Player.HasWeapon,"Retry starts unarmed again");
    var newHazard=FindAnyObjectByType<RisingHazard>();newHazard.graceSeconds=0;
    newHazard.transform.position=new Vector2(0,10);PrototypeGame.HazardEnabled=true;
    yield return Wait(3.7f);
    Require(PrototypeGame.Instance.CurrentState==PrototypeGame.State.Defeat,"Hazard ends round");
    PrototypeGame.HazardEnabled=false;
   }
   Click("CHOOSE MAP");yield return Wait(.5f);
   Require(PrototypeGame.Instance.CurrentState==PrototypeGame.State.MapSelect,"Return to map selection");
   Require(CharacterCatalog.Selected==character,"Character persists through scene change");
   Click("BACK");yield return Wait(.2f);
   Require(PrototypeGame.Instance.CurrentState==PrototypeGame.State.CharacterSelect,"Map back opens character selection");
  }
  Click("BACK");yield return Wait(.2f);
  Require(PrototypeGame.Instance.CurrentState==PrototypeGame.State.MainMenu,"Return to main menu");
 }
 static IEnumerator CheckWeapons(SoloPlayer player,int character){
  for(int id=0;id<3;id++){
   if(id==2){
    var pickup=FindObjectsByType<WorldInteractable>().First(a=>a.kind==WorldInteractable.Kind.Shotgun);
    Place(pickup.transform.position);yield return Wait(.2f);
    Require(player.Weapon==2,"Shotgun pickup equips shotgun");
   }else player.Equip(id);
   Place(new Vector2(-7,1));yield return Wait(.5f);
   var rig=player.WeaponAnimation;string name=WeaponCatalog.All[id].Name;
   InputSystem.QueueStateEvent(mouse,new MouseState{position=Camera.main.WorldToScreenPoint(player.transform.position+new Vector3(5,.4f,0))});yield return Wait(.1f);
   Require(rig.Visible&&rig.Renderer.sprite==PrototypeArt.Load("Sprites/Shared/"+player.Profile.Sprite)&&player.Motion.ArmedBody,"Armed body and correct weapon: "+name);
   Require(rig.AnimationName==name+"/Idle","Weapon idle pose: "+name);CapturePose("12-"+character+"-"+name+"-idle");
   InputSystem.QueueStateEvent(mouse,new MouseState{position=Camera.main.WorldToScreenPoint(player.transform.position+new Vector3(-5,.4f,0))});yield return Wait(.12f);
   Require(player.FacingLeft&&rig.Renderer.flipY&&rig.MuzzlePosition.x<player.Motion.ShoulderPosition.x,"Weapon and arms face left: "+name);
   CapturePose("21-"+character+"-"+name+"-left");
   InputSystem.QueueStateEvent(mouse,new MouseState{position=Camera.main.WorldToScreenPoint(player.transform.position+new Vector3(5,.4f,0))});yield return Wait(.12f);
   Keys(Key.D);yield return Wait(.22f);
   Require(rig.AnimationName==name+"/Walk","Weapon walk animation: "+name);
   var frame=player.CharacterRenderer.sprite;var hand=rig.TriggerHand;yield return Wait(.15f);
   Require(player.CharacterRenderer.sprite!=frame&&rig.TriggerHand!=hand,"Armed locomotion and hands animate: "+name);
   CapturePose("13-"+character+"-"+name+"-walk");Keys();yield return Wait(.1f);
   Place(new Vector2(-7,1));yield return Wait(.3f);Keys(Key.D,Key.LeftShift);yield return Wait(.2f);
   Require(rig.AnimationName==name+"/Run","Weapon run animation: "+name);CapturePose("14-"+character+"-"+name+"-run");Keys();yield return Wait(.1f);
   Place(new Vector2(-7,1));yield return Wait(.3f);Keys(Key.C);yield return Wait(.1f);Keys();yield return Wait(.1f);
   Require(rig.AnimationName==name+"/Crouch","Weapon crouch pose: "+name);CapturePose("15-"+character+"-"+name+"-crouch");
   Keys(Key.D);yield return Wait(.2f);Require(rig.AnimationName==name+"/CrouchWalk","Weapon crouch walk: "+name);Keys();yield return Wait(.1f);
   Keys(Key.Z);yield return Wait(.1f);Keys();yield return Wait(.1f);
   Require(rig.AnimationName==name+"/Prone","Weapon prone pose: "+name);CapturePose("16-"+character+"-"+name+"-prone");
   Keys(Key.D);yield return Wait(.2f);Require(rig.AnimationName==name+"/Crawl","Weapon crawl animation: "+name);Keys();yield return Wait(.1f);
   Keys(Key.Space);yield return Wait(.08f);Keys();yield return Wait(.18f);
   Require(rig.AnimationName==name+"/Jump"&&player.Stance==SoloPlayer.Posture.Standing,"Weapon jump from prone: "+name);CapturePose("17-"+character+"-"+name+"-jump");yield return Wait(.85f);
   Vector2 aim=Camera.main.WorldToScreenPoint(player.transform.position+new Vector3(4,3,0));int ammo=player.Ammo;
   InputSystem.QueueStateEvent(mouse,new MouseState{position=aim}.WithButton(MouseButton.Left));yield return Wait(.07f);
   InputSystem.QueueStateEvent(mouse,new MouseState{position=aim});
   Require(player.Ammo<ammo&&rig.RecoilAmount>0,"Weapon firing and recoil: "+name);CapturePose("18-"+character+"-"+name+"-fire");
   if(id==2){yield return Wait(.16f);Require(rig.PumpAmount>0,"Shotgun pump cycle");CapturePose("19-"+character+"-pump");}
   yield return Wait(.6f);Keys(Key.R);yield return Wait(.1f);Keys();yield return Wait(.35f);
   Require(player.Reloading&&rig.AnimationName==name+"/Reload","Weapon-specific reload: "+name);CapturePose("20-"+character+"-"+name+"-reload");
   yield return Wait(player.Profile.ReloadSeconds);
   Require(!player.Reloading&&player.Ammo==player.Profile.Capacity,"Reload completes with correct capacity: "+name);
  }
 }
 static IEnumerator CheckTactical(SoloPlayer player,int map){
  var guns=FindObjectsByType<MountedGun>();var traps=FindObjectsByType<TacticalTrap>();
  Require(guns.Length==2&&traps.Length==4,"Two mounted guns and four traps in "+MapCatalog.Keys[map]);
  var gun=guns.OrderBy(g=>g.transform.position.y).First();int carriedWeapon=player.Weapon,carriedAmmo=player.Ammo;
  Place(gun.Seat);yield return Wait(.3f);Keys(Key.E);yield return Wait(.1f);Keys();yield return Wait(.1f);
  Require(player.Mounted==gun&&gun.Rider==player,"E mounts station");
  Keys(Key.D);yield return Wait(.2f);Keys();Require(Vector2.Distance(player.transform.position,gun.Seat)<.1f,"Mounted player stays at station");
  Require(!player.WeaponAnimation.Visible&&player.Motion.ArmedBody,"Mounted hands replace carried weapon");
  float until=Time.realtimeSinceStartup+5;
  while(!gun.Overheated&&Time.realtimeSinceStartup<until){InputSystem.QueueStateEvent(mouse,new MouseState{position=Camera.main.WorldToScreenPoint(gun.Pivot+Vector2.right*8)}.WithButton(MouseButton.Left));yield return Wait(.08f);}
  InputSystem.QueueStateEvent(mouse,new MouseState{position=Camera.main.WorldToScreenPoint(gun.Pivot+Vector2.right*8)});
  Require(gun.Overheated&&gun.ShotsFired>10&&gun.Ammo<180,"Mounted firing consumes ammo and overheats");CapturePose("30-"+map+"-mounted");
  int remaining=gun.Ammo;Require(!gun.Fire()&&gun.Ammo==remaining,"Overheated gun cannot fire");
  PrototypeGame.Instance.Pause();float heat=gun.Heat;yield return Wait(.25f);Require(Mathf.Approximately(heat,gun.Heat),"Pause freezes gun heat");PrototypeGame.Instance.Resume();
  yield return Wait(3.3f);Require(!gun.Overheated,"Mounted gun cools and unlocks");
  Keys(Key.E);yield return Wait(.1f);Keys();yield return Wait(.1f);
  Require(!player.Mounted&&!gun.Rider&&player.Weapon==carriedWeapon&&player.Ammo==carriedAmmo,"E dismounts and preserves inventory");
  foreach(var trap in traps.GroupBy(t=>t.kind).Select(g=>g.First())){
   player.Health=100;player.Shield=100;
   if(trap.kind==TacticalTrap.TrapKind.Sand){
    Place((Vector2)trap.transform.position+Vector2.up*.9f);yield return Wait(.35f);Keys(Key.D);yield return Wait(.15f);
    Require(player.Velocity.x>0&&player.Velocity.x<3.4f,"Quicksand slows actual movement");Keys();CapturePose("31-"+map+"-sand");
    Keys(Key.Space);yield return Wait(.08f);Keys();yield return Wait(.2f);
    Require(player.Velocity.y>0&&TacticalTrap.MovementScale(player)==1,"Jump escapes quicksand slowdown");yield return Wait(.7f);continue;
   }
   // Stay away until cooldown ends; triggered traps wait for a player.
   Place(new Vector2(-7,1));float deadline=Time.realtimeSinceStartup+12;
   while(trap.State!=TacticalTrap.Phase.Idle&&Time.realtimeSinceStartup<deadline)yield return Wait(.05f);
   Require(trap.State==TacticalTrap.Phase.Idle,"Trap reaches idle: "+trap.kind);
   Place((Vector2)trap.transform.position+Vector2.up*.85f);yield return Wait(.1f);
   deadline=Time.realtimeSinceStartup+6;
   while(trap.State!=TacticalTrap.Phase.Warning&&Time.realtimeSinceStartup<deadline)yield return Wait(.03f);
   Require(trap.State==TacticalTrap.Phase.Warning,"Trap warns before activation: "+trap.kind);
   int protection=player.Health+player.Shield;yield return Wait(.2f);
   Require(player.Health+player.Shield==protection,"Warning does not damage player: "+trap.kind);CapturePose("32-"+map+"-"+trap.kind+"-warning");
   if(trap.kind==TacticalTrap.TrapKind.Darts){Keys(Key.C);yield return Wait(.1f);Keys();}
   deadline=Time.realtimeSinceStartup+3;
   while(trap.State!=TacticalTrap.Phase.Active&&Time.realtimeSinceStartup<deadline)yield return Wait(.03f);
   Require(trap.State==TacticalTrap.Phase.Active,"Trap activates: "+trap.kind);yield return Wait(.35f);
   if(trap.kind==TacticalTrap.TrapKind.Breakaway){
    Require(!trap.breakawayCollider.enabled&&player.transform.position.y<trap.transform.position.y+.5f,"Breakaway removes collision and player falls");
   }else if(trap.kind==TacticalTrap.TrapKind.Darts){
    Require(player.Health+player.Shield==protection,"Crouching avoids high dart lane");Keys(Key.C);yield return Wait(.1f);Keys();yield return Wait(.4f);
    Require(player.Health+player.Shield<protection,"Standing in dart lane takes damage");
   }else Require(player.Health+player.Shield<protection,"Active trap deals damage: "+trap.kind);
   CapturePose("33-"+map+"-"+trap.kind+"-active");
   if(trap.hasSwitch){
    Place(trap.SwitchPosition);yield return Wait(.2f);Keys(Key.F);yield return Wait(.1f);Keys();
    Require(trap.State==TacticalTrap.Phase.Disabled,"F disables nearby trap: "+trap.kind);
    Place((Vector2)trap.transform.position+Vector2.up*.85f);yield return Wait(.2f);int safe=player.Health+player.Shield;yield return Wait(.2f);
    Require(player.Health+player.Shield==safe,"Disabled trap is safe: "+trap.kind);
   }
   Place(new Vector2(-7,1));yield return Wait(.2f);
  }
  player.Health=100;player.Shield=0;Place(new Vector2(-7,1));yield return Wait(.2f);
 }
 static void Finish(string failure) {
  if(completed)return;completed=true;Application.logMessageReceived-=OnLog;EditorApplication.update-=Tick;
  Keys();SessionState.SetBool(ActiveKey,false);
  if(SessionState.GetBool(ActiveKey+".hadCharacter",false))PlayerPrefs.SetInt(CharacterCatalog.PreferenceKey,SessionState.GetInt(ActiveKey+".character",0));
  else PlayerPrefs.DeleteKey(CharacterCatalog.PreferenceKey);
  PlayerPrefs.Save();
  InputSystem.settings.backgroundBehavior=previousBackground;Application.runInBackground=previousRunInBackground;
  InputSystem.settings.editorInputBehaviorInPlayMode=previousEditorInput;
  InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);
  foreach(var device in suspendedDevices)if(device.added)InputSystem.EnableDevice(device);
  suspendedDevices.Clear();
  File.WriteAllText(Output+"/result.txt",(failure==null?"PASS":"FAIL")+"\n"+
   string.Join("\n",passed)+(failure==null?"":"\n\n"+failure));
  if(failure==null)Debug.Log("Prototype Play Mode checks passed: "+passed.Count);
  else Debug.LogError("Prototype Play Mode check failed: "+failure);
  if(Application.isBatchMode)EditorApplication.Exit(failure==null?0:1);
  else EditorApplication.isPlaying=false;
 }
}
}
#endif
