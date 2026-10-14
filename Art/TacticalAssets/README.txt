TACTICAL PROPS / 2DPUBG

Generated with built-in ImageGen from Art/TrapConcepts references.
prompts.json records the final atlas prompts. PNG files are in:
Assets/ClimbBattlePrototype/Resources/ClimbBattleArt/Tactical/

Four transparent atlases, each with nine named sprite sub-assets:
Base, Barrel, Cover, TrapA, TrapB, Switch, EffectA, EffectB, Ammo.
Build-AtlasMetadata.ps1 scans alpha bounds and writes importer rectangles.
The source PNG pixels remain unchanged. Unity imports each region separately.

NEON CITY: mounted MG, laser lane, shock tiles.
MOLTEN WORKS: heavy MG, steam vent, stamp press.
FALLING DISTRICT: MG nest, breakaway floor tile, swinging steel beam.
SANDS OF ANUBIS: scarab energy MG, dart wall, quicksand with escape rope.

CONTROLS
E near a gun: mount. Mouse aims in a 70-degree forward arc. Left click fires.
E leaves the gun; Space jumps off. Carried gun/ammo are preserved.
Each station starts with 180 rounds. Continuous firing overheats it;
release and cool below 30% to fire again. Station ammo resets with the map.
F near a trap switch: disable it for six seconds.
Orange pulsing areas warn before damage starts. C/Z avoid the high dart lane;
Z also avoids the laser at waist height. Jump over shock tiles and steam.
Quicksand slows movement to 42%; Space or the nearby rope escapes it.
Breakaway slabs warn for 0.9 seconds, drop, then recover after a cooldown.

PLACEMENT
Every map has two mounted guns: floor 1 at x=-15, floor 4 at x=8.
Four traps on floors 2, 3, 5, 6; outer ladder routes remain open.
Two Collapse Tower floor tiles are disabled and replaced with breakaway slabs.
The original tiles remain in the scene and can be re-enabled if desired.

SETUP
TacticalSetup generates 12 reusable prefabs and upgrades the four saved maps.
It runs once on import when not playing and open scenes are saved.
Menu: 2DPUBG > Add Tactical Props to Maps.
TacticalProps-v1.txt prevents reapplying the upgrade over manual map edits.
Original scenes are backed up to Art/SceneBackups/*.before-tactical-props.unity.
Runtime Ensure also adds the props to maps which have not yet been upgraded.

TUNING
MountedGun: ammo, direction, heat/firing profile.
TacticalTrap: area, damage, idle/warning/active/cooldown timings, switch position.
TacticalWorld: placement and visual assembly.
Tests: 2DPUBG > Run Play Mode Checks; includes previous gameplay regression checks.

Single player remains the current mode; stationary guns are player-operated,
not autonomous enemy AI. This package adds no multiplayer or NPCs.
