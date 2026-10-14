using UnityEngine;
namespace ClimbBattle {
public static class MapCatalog {
 public static readonly string[] Keys = { "NeonCity", "IndustrialLava", "CollapseTower", "EgyptPyramid" };
 public static readonly string[] Names = { "NEON CITY", "MOLTEN WORKS", "FALLING DISTRICT", "SANDS OF ANUBIS" };
 public static readonly string[] Descriptions = {
  "Rain, neon and rising water. Climb the rooftop escape routes.",
  "Rust, steel and molten lava. Use the industrial lifts.",
  "Broken walls and rising debris. Stay above the dust.",
  "Ancient stone and rising sand. Ascend the seven terraces." };
 public static readonly Color[] Colors = { new Color(.18f,.85f,1), new Color(1,.43f,.16f), new Color(.94f,.69f,.42f), new Color(1,.8f,.3f) };
 public static readonly float[][] Widths = {
  new float[]{64,58,52,46,40,34,28}, new float[]{68,60,54,46,40,32,26},
  new float[]{66,58,50,44,38,30,24}, new float[]{68,58,48,40,32,24,16} };
 public const float FloorSpacing = 6f;
 public const int FloorCount = 7;
 public const string SceneFolder = "Assets/ClimbBattlePrototype/Scenes/";
 public static string SceneName(int index) => index < 0 ? "CB_MainMenu" : "CB_" + Keys[index];
}
}
