using UnityEngine;

namespace ClimbBattle {
public static class CharacterCatalog {
 public const string PreferenceKey="ClimbBattle.Character";
 public static readonly string[] Keys={"01-neon-runner","02-forge-guard","03-ruin-scout","04-dune-warden","05-aegis-mage"};
 public static readonly string[] Names={"NEON RUNNER","FORGE GUARD","RUIN SCOUT","DUNE WARDEN","AEGIS MAGE"};
 public static readonly string[] Descriptions={"Rooftop climbing gear","Industrial armor","Rope and scavenger kit","Ancient desert armor","Arcane explorer outfit"};
 public static readonly Color[] Colors={new Color(.15f,.86f,1),new Color(1,.48f,.16f),new Color(.72f,.8f,.33f),new Color(1,.8f,.34f),new Color(.77f,.53f,1)};
 // Measured alpha bounds keep all five sprites the same visible height with boots on the floor.
 public static readonly float[] BodyHeightFractions={.95706f,.87760f,.88542f,.90690f,.96143f};
 public static readonly float[] BodyBottomMargins={.02329f,.05404f,.06380f,.06576f,.02111f};
 public static readonly float[] HandHeights={.16f,.15f,.19f,.13f,.3f};
 public static int Selected {
  get=>Mathf.Clamp(PlayerPrefs.GetInt(PreferenceKey,0),0,Keys.Length-1);
  set {PlayerPrefs.SetInt(PreferenceKey,Mathf.Clamp(value,0,Keys.Length-1));PlayerPrefs.Save();}
 }
 public static string BodyPath(int index)=>"Characters/Bodies/"+Keys[Mathf.Clamp(index,0,Keys.Length-1)];
 public static string PortraitPath(int index)=>"Characters/Portraits/"+Keys[Mathf.Clamp(index,0,Keys.Length-1)];
}
}
