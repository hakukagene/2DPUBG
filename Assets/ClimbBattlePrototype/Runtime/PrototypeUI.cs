using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
namespace ClimbBattle {
public static class PrototypeUI {
 static Font font;
 public static RectTransform Canvas() {
  var go=new GameObject("PrototypeUI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
  go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
  var scale=go.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1280,720);scale.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
  if(!Object.FindAnyObjectByType<EventSystem>()){
   var events=new GameObject("EventSystem",typeof(EventSystem));events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
  }
  return go.GetComponent<RectTransform>();
 }
 public static RectTransform Rect(Transform parent,string name,float x,float y,float width,float height) {
  var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);
  r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);return r;
 }
 public static Image Panel(Transform parent,string name,float x,float y,float width,float height,Color color){
  var r=Rect(parent,name,x,y,width,height);var image=r.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;
 }
 public static Image Fill(Transform parent,string name,Color color){
  var image=Panel(parent,name,0,0,0,0,color);var r=image.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return image;
 }
 public static Text Label(Transform parent,string value,float x,float y,float width,float height,int size,Color? color=null,FontStyle style=FontStyle.Normal){
  var r=Rect(parent,"Label",x,y,width,height);var text=r.gameObject.AddComponent<Text>();
  if(!font)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
  text.font=font;text.text=value;text.fontSize=size;text.fontStyle=style;text.color=color??Color.white;text.raycastTarget=false;
  text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;return text;
 }
 public static Button Button(Transform parent,string label,float x,float y,float width,float height,UnityEngine.Events.UnityAction click,Color? color=null){
  var image=Panel(parent,"Button_"+label,x,y,width,height,color??new Color(.15f,.8f,.75f));image.raycastTarget=true;
  var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;b.onClick.AddListener(click);
  var palette=b.colors;palette.highlightedColor=new Color(1.2f,1.2f,1.2f);palette.pressedColor=new Color(.7f,.8f,.9f);b.colors=palette;
  var t=Label(image.transform,label,12,0,width-24,height,18,Color.white,FontStyle.Bold);t.alignment=TextAnchor.MiddleCenter;
  return b;
 }
 public static void Background(Transform parent,int map,float shade=.6f){
  var image=Fill(parent,"Backdrop",Color.white);image.sprite=PrototypeArt.Load("Backgrounds/"+MapCatalog.Keys[map]);
  Fill(parent,"BackdropShade",new Color(.025f,.04f,.075f,shade));
 }
}
}
