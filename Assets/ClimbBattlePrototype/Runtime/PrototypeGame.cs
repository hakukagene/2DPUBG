using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace ClimbBattle {
public sealed class PrototypeGame:MonoBehaviour {
 public enum State { MainMenu,MapSelect,Playing,Paused,Victory,Defeat,CharacterSelect }
 public int MapIndex=-1;
 public State CurrentState {get;private set;}
 public SoloPlayer Player {get;private set;}
 public static PrototypeGame Instance {get;private set;}
 public static bool IsPlaying=>Instance&&Instance.CurrentState==State.Playing;
 public static bool HazardEnabled=true;
 public static bool ReturnToSelection;
 public int TargetsDestroyed;public bool NearGoal;
 RectTransform canvas;Text healthText,weaponText,stageText,message;Image hpBar,shieldBar;
 FollowCamera follow;RisingHazard hazard;float played,messageUntil;
 public float Elapsed=>played;
 void Awake(){Instance=this;Time.timeScale=1;}
 void Start(){
  var cam=Camera.main;
  if(!cam){var cameraObject=new GameObject("Main Camera");cameraObject.tag="MainCamera";cam=cameraObject.AddComponent<Camera>();cameraObject.AddComponent<AudioListener>();}
  cam.orthographic=true;cam.orthographicSize=7.8f;cam.transform.position=new Vector3(0,4,-10);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.025f,.04f,.07f);
  canvas=PrototypeUI.Canvas();
  if(MapIndex<0){if(ReturnToSelection){ReturnToSelection=false;ShowSelection();}else ShowMenu();return;}
  if(!GameObject.Find("Map_"+MapCatalog.Keys[MapIndex]))PrototypeWorld.Build(MapIndex);
  TacticalWorld.Ensure(GameObject.Find("Map_"+MapCatalog.Keys[MapIndex]).transform,MapIndex);
  var player=new GameObject("SoloPlayer");player.transform.position=new Vector3(-3,1.3f,0);Player=player.AddComponent<SoloPlayer>();Player.Initialize();
  follow=cam.gameObject.AddComponent<FollowCamera>();follow.target=player.transform;follow.mapWidth=MapCatalog.Widths[MapIndex][0];
  hazard=FindAnyObjectByType<RisingHazard>();
  CurrentState=State.Playing;BuildHud();
 }
 void OnDestroy(){Time.timeScale=1;if(Instance==this)Instance=null;}
 void ClearUI(){
  for(int i=canvas.childCount-1;i>=0;i--){canvas.GetChild(i).gameObject.SetActive(false);Destroy(canvas.GetChild(i).gameObject);}
 }
 public void ShowMenu(){
  CurrentState=State.MainMenu;ClearUI();PrototypeUI.Background(canvas,0,.6f);
  PrototypeUI.Label(canvas,"CLIMB  /  FIGHT  /  SURVIVE",72,70,800,28,17,new Color(.4f,.9f,.86f),FontStyle.Bold);
  PrototypeUI.Label(canvas,"2D PUBG\nASCENT",66,132,850,214,88,Color.white,FontStyle.Bold);
  PrototypeUI.Label(canvas,"Four worlds. Seven floors. One way up.\nExplore the arena in single player.",72,370,700,75,23,new Color(.75f,.82f,.9f));
  PrototypeUI.Button(canvas,"START",72,480,290,62,ShowCharacters);
  PrototypeUI.Button(canvas,"CONTROLS",72,560,180,48,ShowControls,new Color(.13f,.2f,.3f));
  PrototypeUI.Button(canvas,"QUIT",270,560,92,48,Quit,new Color(.13f,.2f,.3f));
  CharacterPortrait(canvas,CharacterCatalog.Selected,830,100,330,500);
  var selected=PrototypeUI.Label(canvas,CharacterCatalog.Names[CharacterCatalog.Selected],810,605,380,32,22,CharacterCatalog.Colors[CharacterCatalog.Selected],FontStyle.Bold);selected.alignment=TextAnchor.MiddleCenter;
  PrototypeUI.Label(canvas,"SINGLE PLAYER PROTOTYPE   •   4 MAPS   •   OFFLINE",72,664,1100,28,14,new Color(.61f,.7f,.79f));
 }
 static void CharacterPortrait(Transform parent,int index,float x,float y,float width,float height){
  var portrait=PrototypeUI.Panel(parent,"CharacterPortrait",x,y,width,height,Color.white);
  portrait.sprite=PrototypeArt.Load(CharacterCatalog.PortraitPath(index));portrait.preserveAspect=true;
 }
 public void ShowCharacters(){
  CurrentState=State.CharacterSelect;ClearUI();PrototypeUI.Background(canvas,0,.88f);
  PrototypeUI.Label(canvas,"CHOOSE YOUR ADVENTURER",40,40,1200,64,43,Color.white,FontStyle.Bold);
  PrototypeUI.Label(canvas,"Choose your look. Every adventurer can use all weapons.",40,108,1190,35,20,new Color(.72f,.8f,.88f));
  for(int i=0;i<CharacterCatalog.Keys.Length;i++){
   int index=i;bool selected=i==CharacterCatalog.Selected;
   var card=PrototypeUI.Button(canvas,"",40+i*242,160,232,400,()=>{CharacterCatalog.Selected=index;ShowCharacters();},selected?new Color(.17f,.26f,.36f):new Color(.065f,.1f,.17f));
   card.name="Character_"+i;
   CharacterPortrait(card.transform,i,12,12,208,284);
   PrototypeUI.Panel(card.transform,"Accent",0,297,232,selected?5:2,CharacterCatalog.Colors[i]);
   var title=PrototypeUI.Label(card.transform,CharacterCatalog.Names[i],10,313,212,28,18,Color.white,FontStyle.Bold);title.alignment=TextAnchor.MiddleCenter;
   var details=PrototypeUI.Label(card.transform,CharacterCatalog.Descriptions[i],10,347,212,24,14,new Color(.72f,.8f,.88f));details.alignment=TextAnchor.MiddleCenter;
   var status=PrototypeUI.Label(card.transform,selected?"SELECTED":"SELECT",10,377,212,20,13,CharacterCatalog.Colors[i],FontStyle.Bold);status.alignment=TextAnchor.MiddleCenter;
  }
  PrototypeUI.Button(canvas,"BACK",40,616,155,52,ShowMenu,new Color(.13f,.2f,.3f));
  PrototypeUI.Label(canvas,CharacterCatalog.Names[CharacterCatalog.Selected],225,629,640,32,22,CharacterCatalog.Colors[CharacterCatalog.Selected],FontStyle.Bold);
  PrototypeUI.Button(canvas,"CONTINUE",940,610,300,62,ShowSelection);
 }
 public void ShowSelection(){
  CurrentState=State.MapSelect;ClearUI();PrototypeUI.Background(canvas,3,.86f);
  PrototypeUI.Label(canvas,"CHOOSE YOUR ASCENT",60,60,1150,66,44,Color.white,FontStyle.Bold);
  PrototypeUI.Label(canvas,CharacterCatalog.Names[CharacterCatalog.Selected]+"  /  Reach floor 7. Press E at the summit beacon to finish.",60,130,1160,35,19,new Color(.7f,.77f,.85f));
  for(int i=0;i<4;i++){
   int map=i;float x=60+i*294;
   var card=PrototypeUI.Button(canvas,"",x,195,274,337,()=>LoadMap(map),new Color(.09f,.14f,.22f));
   var cover=PrototypeUI.Panel(card.transform,"Image",0,0,274,172,Color.white);cover.sprite=PrototypeArt.Load("Backgrounds/"+MapCatalog.Keys[i]);cover.preserveAspect=true;cover.color=new Color(.85f,.9f,1);
   PrototypeUI.Panel(card.transform,"Accent",0,172,274,4,MapCatalog.Colors[i]);
   PrototypeUI.Label(card.transform,"0"+(i+1)+" / "+MapCatalog.Names[i],16,197,244,49,20,Color.white,FontStyle.Bold);
   PrototypeUI.Label(card.transform,MapCatalog.Descriptions[i],16,250,242,66,15,new Color(.68f,.75f,.84f));
  }
  var toggle=PrototypeUI.Button(canvas,"RISING HAZARD: "+(HazardEnabled?"ON":"OFF"),60,565,310,48,()=>{HazardEnabled=!HazardEnabled;ShowSelection();},new Color(.16f,.24f,.3f));
  PrototypeUI.Label(canvas,"Turn off for a relaxed map exploration.",390,575,700,35,17,new Color(.66f,.75f,.85f));
  PrototypeUI.Button(canvas,"BACK",60,637,140,46,ShowCharacters,new Color(.13f,.2f,.3f));
 }
 void ShowControls(){
  ClearUI();PrototypeUI.Background(canvas,0,.9f);
  PrototypeUI.Label(canvas,"CONTROLS",80,75,1000,70,46,Color.white,FontStyle.Bold);
  PrototypeUI.Label(canvas,"A / D or arrows     Walk / crouch-walk / crawl\nW / S or arrows     Climb ladder / rope\nSpace     Stand and jump / leave ladder\nLeft Shift     Sprint while standing\nC     Toggle crouch     |     Z     Toggle prone\nMouse + left click     Aim and shoot\nR     Reload\nM     Map overview\nE     Use / leave mounted gun; finish at summit\nF     Disable nearby trap for 6 seconds\nEscape     Pause / resume",80,175,1100,420,25,new Color(.8f,.86f,.92f));
  PrototypeUI.Button(canvas,"BACK",80,625,200,52,ShowMenu);
 }
 void BuildHud(){
  ClearUI();PrototypeUI.Panel(canvas,"TopBar",0,0,1280,86,new Color(.025f,.045f,.075f,.93f));
  PrototypeUI.Label(canvas,MapCatalog.Names[MapIndex],24,12,365,24,18,MapCatalog.Colors[MapIndex],FontStyle.Bold);
  healthText=PrototypeUI.Label(canvas,"",24,41,370,22,15);
  hpBar=PrototypeUI.Panel(canvas,"Health",24,69,175,5,new Color(.2f,.88f,.64f));
  shieldBar=PrototypeUI.Panel(canvas,"Shield",210,69,140,5,new Color(.3f,.68f,1));
  stageText=PrototypeUI.Label(canvas,"",420,15,430,60,18);stageText.alignment=TextAnchor.MiddleCenter;
  weaponText=PrototypeUI.Label(canvas,"",916,19,260,54,17);
  PrototypeUI.Button(canvas,"II",1198,18,52,48,Pause,new Color(.12f,.22f,.29f));
  message=PrototypeUI.Label(canvas,"REACH FLOOR 7  /  FIND THE SUMMIT BEACON",200,104,880,42,18,MapCatalog.Colors[MapIndex],FontStyle.Bold);message.alignment=TextAnchor.MiddleCenter;
  PrototypeUI.Panel(canvas,"BottomBar",0,672,1280,48,new Color(.025f,.045f,.075f,.85f));
  PrototypeUI.Label(canvas,"A/D MOVE   SHIFT RUN   SPACE JUMP   C CROUCH   Z PRONE   W/S CLIMB   MOUSE FIRE   R RELOAD   E USE   F SWITCH   M MAP   ESC PAUSE",24,686,1230,26,13,new Color(.72f,.8f,.88f));
 }
 void Update(){
  var keyboard=Keyboard.current;
  if(MapIndex<0){if(keyboard?.escapeKey.wasPressedThisFrame==true){if(CurrentState==State.MapSelect)ShowCharacters();else ShowMenu();}return;}
  if(keyboard?.escapeKey.wasPressedThisFrame==true){if(CurrentState==State.Playing)Pause();else if(CurrentState==State.Paused)Resume();}
  if(!IsPlaying||!Player)return;
  played+=Time.deltaTime;
  if(keyboard?.mKey.wasPressedThisFrame==true)follow.overview=!follow.overview;
  NearGoal=Vector2.Distance(Player.transform.position,new Vector2(0,38))<2.6f;
  if(NearGoal&&keyboard?.eKey.wasPressedThisFrame==true){Finish(true);return;}
  healthText.text="HP "+Player.Health+" / 100     SHIELD "+Player.Shield+" / 60";
  hpBar.rectTransform.sizeDelta=new Vector2(175*Player.Health/100f,5);shieldBar.rectTransform.sizeDelta=new Vector2(140*Player.Shield/60f,5);
  int floor=Mathf.Clamp(Mathf.FloorToInt((Player.transform.position.y+.2f)/6)+1,1,7);
  string risk=!HazardEnabled?"EXPLORATION":hazard.GraceLeft>0?"HAZARD IN "+Mathf.CeilToInt(hazard.GraceLeft)+"s":"HAZARD RISING";
  stageText.text="FLOOR "+floor+" / 7   •   "+Mathf.FloorToInt(played/60).ToString("00")+":"+Mathf.FloorToInt(played%60).ToString("00")+"\n"+risk;
  weaponText.text=Player.Mounted?"MOUNTED MG / "+Player.Mounted.Ammo+"\n"+(Player.Mounted.Overheated?"OVERHEATED": "HEAT "+Mathf.RoundToInt(Player.Mounted.Heat*100)+"%"):Player.HasWeapon?Player.WeaponName+"  "+(Player.Reloading?"RELOADING":Player.Ammo+" / "+Player.Reserve)+"\nTARGETS  "+TargetsDestroyed+" / 12":"UNARMED\nFIND A WEAPON";
  if(NearGoal)message.text="PRESS E  /  COMPLETE THE ASCENT";
  else if(Time.time>messageUntil)message.text=!Player.HasWeapon?"FIND A WEAPON  /  FIRST PISTOL IS TO YOUR LEFT":Player.Climbing?"W / S CLIMB   •   SPACE TO JUMP OFF":Player.Stance==SoloPlayer.Posture.Prone?"PRONE  /  A-D CRAWL  /  Z STAND":Player.Stance==SoloPlayer.Posture.Crouching?"CROUCHING  /  A-D MOVE  /  C STAND":"REACH FLOOR 7  /  FIND THE SUMMIT BEACON";
  if(Player.Mounted)message.text="MOUSE FIRE / E LEAVE / SPACE JUMP OFF / WATCH HEAT";
  else if(!NearGoal&&Time.time>messageUntil){
   foreach(var gun in FindObjectsByType<MountedGun>())if(Vector2.Distance(Player.transform.position,gun.Seat)<1.8f){message.text="PRESS E / USE MOUNTED GUN";break;}
   foreach(var trap in TacticalTrap.All)if(trap&&trap.hasSwitch&&Vector2.Distance(Player.transform.position,trap.SwitchPosition)<1.6f){message.text="PRESS F / DISABLE TRAP FOR 6 SECONDS";break;}
   if(TacticalTrap.MovementScale(Player)<1)message.text="QUICKSAND / SPACE TO JUMP / W ON ESCAPE ROPE";
  }
 }
 public void Notify(string value){if(message){message.text=value;messageUntil=Time.time+2.3f;}}
 public void Pause(){if(!IsPlaying)return;CurrentState=State.Paused;Time.timeScale=0;Overlay("PAUSED","Your ascent is waiting.",true);}
 public void Resume(){Time.timeScale=1;CurrentState=State.Playing;BuildHud();}
 public void Finish(bool won){if(!IsPlaying)return;CurrentState=won?State.Victory:State.Defeat;Time.timeScale=0;Overlay(won?"SUMMIT REACHED":"ASCENT ENDED",won?"Seven floors cleared. "+TargetsDestroyed+" practice targets destroyed.":"The hazard caught you. Try a different route.",false);}
 void Overlay(string title,string subtitle,bool paused){
  ClearUI();PrototypeUI.Fill(canvas,"Veil",new Color(.025f,.04f,.07f,.94f));
  PrototypeUI.Label(canvas,title,150,145,1000,85,62,Color.white,FontStyle.Bold);
  PrototypeUI.Label(canvas,subtitle,154,248,980,70,23,new Color(.73f,.81f,.9f));
  if(paused)PrototypeUI.Button(canvas,"RESUME",155,365,290,58,Resume);
  else PrototypeUI.Button(canvas,"PLAY AGAIN",155,365,290,58,()=>LoadMap(MapIndex));
  PrototypeUI.Button(canvas,"CHOOSE MAP",155,445,290,58,GoToSelection,new Color(.17f,.27f,.36f));
  PrototypeUI.Button(canvas,"MAIN MENU",155,525,290,58,()=>{Time.timeScale=1;ReturnToSelection=false;SceneManager.LoadScene(MapCatalog.SceneName(-1));},new Color(.13f,.2f,.28f));
 }
 public static void LoadMap(int map){Time.timeScale=1;SceneManager.LoadScene(MapCatalog.SceneName(map));}
 public static void GoToSelection(){Time.timeScale=1;ReturnToSelection=true;SceneManager.LoadScene(MapCatalog.SceneName(-1));}
 static void Quit(){
 #if UNITY_EDITOR
 UnityEditor.EditorApplication.isPlaying=false;
 #else
 Application.Quit();
 #endif
 }
}
}

