# ThirdDoor Curated Asset Pack / 《第三扇门》精选模型包

This pack is a curated subset of the uploaded free/CC0 low-poly room and door resources, selected for a Unity URP first-person symbolic narrative puzzle prototype.

## Contents

- `Assets/Kenney_ModularDungeon_FBX/`  
  Main architectural kit: rooms, corridors, walls, floors, gates, doorway-like pieces. Best for the middle room and basic left/right/exit spatial structure.

- `Assets/DoorsAsset_FBX/`  
  17 low-poly doors + 5 windows. Best for the black exit door and optional left/right room doors. Included selected DoorSet textures: Dark, Red, Blue, White, default, AO, Metallic, Normal, Roughness.

- `Assets/Quaternius_ModularSciFi_UnityFBX/`  
  Unity FBX subset from the sci-fi kit: simple door frames, symbolic platform/floor pieces, simple wall trims, light props, vents/cables, simple columns. Use only when the Kenney dungeon pieces are too fantasy-like.

- `Assets/Quaternius_HouseInterior_StageProps_FBX/`  
  Minimal stage props: lights, chairs, round tables, shelves, columns, carpet, one drawer/nightstand. Use sparingly so the scene stays abstract.

- `Licenses/`  
  Original license/readme files from all source packs. All included source packs state CC0 / public domain dedication in their bundled license/readme files.

## Recommended use in The Third Door

### First replacement batch

1. Replace the black exit door with one of `DoorsAsset_FBX/Door_*_grp.fbx`; start with Door_1, Door_3, Door_10, or Door_13. Override material to near-black. Add a separate narrow emissive white cube/plane for the glowing slit.
2. Replace left/right room doors or thresholds with `Kenney_ModularDungeon_FBX/gate-door.fbx`, `gate.fbx`, or SciFi `Door_Frame_Square.fbx` / `Door_Frame_SquareTall.fbx`.
3. Replace central room boundaries with Kenney `room-small.fbx`, `room-wide.fbx`, `template-wall.fbx`, `template-wall-corner.fbx`, `template-floor.fbx`.

### Second replacement batch

1. Add one chair/table/light per NPC room from `Quaternius_HouseInterior_StageProps_FBX/` if you need emotional anchors.
2. Add SciFi light strips or simple trims from `Quaternius_ModularSciFi_UnityFBX/Props/` and `Walls/` only near the black exit door or threshold zones.
3. Keep large red/blue/yellow wall planes as Unity cubes/planes with simple URP materials.

## Unity URP import notes

- Prefer FBX import into `Assets/ThirdDoor_Curated_AssetPack/`.
- If any imported material is pink, create fresh URP/Lit or URP/Unlit materials and assign them manually.
- Use simple override materials: MotherRoom_Red, ChildRoom_Blue, MiddleRoom_YellowGrey, ExitDoor_Black, White_Emission.
- Use Box Collider for walls/doors instead of Mesh Collider.
- For glowing door slit: use a thin Cube or Plane with emission + Bloom. Do not rely on a dedicated model.
- These files are intentionally not a UnityPackage; this is a clean source-asset zip that can be copied into any Unity project.

## Selection policy

I excluded:
- extra sci-fi aliens/weapons/complex props,
- most kitchen/bathroom/bedroom clutter,
- duplicate OBJ/glTF/Blend variants when FBX exists,
- high-detail or theme-heavy objects that would reduce the abstract stage-space feeling.

Total selected source entries: 156
