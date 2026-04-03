# CharacterManager

A modular character roster manager for Unity.  
Tracks character definitions, availability unlock state, and the active character.  
Supports JSON roster files for modding.


## Features

- **Roster definitions** — define characters (id, display name, bio, portrait, stats) in the Inspector
- **Availability tracking** — unlock state persisted via `PlayerPrefs`; `alwaysAvailable` flag for default characters
- **Active character** — `SetActive(id)` / `GetActiveCharacter()` / `ActiveCharacterId`
- **Character stats** — `CharacterStats` struct (health, speed, attackPower) on every definition; extend as needed
- **JSON / Modding** — load and merge roster definitions from `StreamingAssets/characters.json` at startup
- **SaveManager integration** — checks `char_<id>` save flags as an additional availability source (activated via `CHARACTERMANAGER_SM`)
- **GalleryManager integration** — calls `GalleryManager.UnlockStatic(id)` when a character is unlocked (activated via `CHARACTERMANAGER_GM`)
- **EventManager integration** — fires `CharacterUnlocked` and `ActiveCharacterChanged` as GameEvents (activated via `CHARACTERMANAGER_EM`)
- **Custom Inspector** — per-character availability status with Unlock / Lock / Set Active buttons at runtime


## Installation

### Option A — Unity Package Manager (Git URL)

1. Open **Window → Package Manager**
2. Click **+** → **Add package from git URL…**
3. Enter:

   ```
   https://github.com/RolandKaechele/CharacterManager.git
   ```

### Option B — Clone into Assets

```bash
git clone https://github.com/RolandKaechele/CharacterManager.git Assets/CharacterManager
```

### Option C — npm / postinstall

```bash
cd Assets/CharacterManager
npm install
```

`postinstall.js` creates the required `StreamingAssets/` folder under `Assets/` and optionally copies example JSON files.


## Scene Setup

1. Attach `CharacterManager` to a persistent GameObject.
2. Define roster entries in the Inspector.
3. Set `defaultCharacterId` to the protagonist's id.


## Quick Start

### 1. Inspector fields

| Field | Default | Description |
| ----- | ------- | ----------- |
| `characters` | *(empty)* | All character definitions |
| `defaultCharacterId` | *(empty)* | Active character set on Awake |
| `loadFromJson` | `false` | Merge from JSON on Awake |
| `jsonPath` | `"characters.json"` | Path relative to `StreamingAssets/` |

### 2. Unlock and query characters

```csharp
var cm = FindFirstObjectByType<CharacterManager.Runtime.CharacterManager>();

// Check availability
bool available = cm.IsAvailable("jan_tenner");

// Unlock (persists to PlayerPrefs)
cm.Unlock("commander_fox");

// Set active character
cm.SetActive("jan_tenner");

// Query
var def = cm.GetActiveCharacter();
Debug.Log($"{def.displayName}  HP:{def.stats.health}");
```

### 3. React to events

```csharp
cm.OnCharacterUnlocked     += id => Debug.Log($"Unlocked: {id}");
cm.OnActiveCharacterChanged += id => Debug.Log($"Active:   {id}");
```

### 4. Unlock from anywhere (without scene reference)

```csharp
CharacterManager.Runtime.CharacterManager.UnlockStatic("commander_fox");
```


## JSON / Modding

Enable `loadFromJson` and place `characters.json` in `StreamingAssets/`.

```json
{
  "characters": [
    {
      "id": "jan_tenner",
      "displayName": "Jan Tenner",
      "bio": "Der galaktische Held.",
      "stats": { "health": 120, "speed": 6.0, "attackPower": 15 },
      "alwaysAvailable": true
    },
    {
      "id": "commander_fox",
      "displayName": "Commander Fox",
      "bio": "Janns treuer Verbündeter.",
      "stats": { "health": 100, "speed": 5.0, "attackPower": 12 },
      "alwaysAvailable": false
    }
  ]
}
```

JSON and Inspector entries are merged by `id`. JSON entries with a matching id override Inspector data; new ids are appended.


## SaveManager Integration (`CHARACTERMANAGER_SM`)

Add `CHARACTERMANAGER_SM` to **Edit → Project Settings → Player → Scripting Define Symbols**.

`IsAvailable(id)` additionally checks `SaveManager.IsSet("char_<id>")`.

Requires [SaveManager](https://github.com/RolandKaechele/SaveManager) in the project.


## Runtime API

| Member | Description |
| ------ | ----------- |
| `IsAvailable(id)` | True if the character is available (`alwaysAvailable` → PlayerPrefs → SaveManager) |
| `Unlock(id)` | Persist availability to PlayerPrefs; fires `OnCharacterUnlocked`. Idempotent |
| `Lock(id)` | Remove PlayerPrefs flag. For testing/reset only |
| `SetActive(id)` | Set the active character; fires `OnActiveCharacterChanged` |
| `GetActiveCharacter()` | Returns the `CharacterDefinition` for the active character, or null |
| `GetDefinition(id)` | Returns a `CharacterDefinition` by id, or null |
| `GetAvailableCharacters()` | List of all currently available characters |
| `UnlockStatic(id)` | *(static)* Persist availability without a scene reference |
| `ActiveCharacterId` | ID of the currently active character |
| `Characters` | `IReadOnlyList<CharacterDefinition>` (merged) |
| `OnCharacterUnlocked` | `event Action<string>` — character id |
| `OnActiveCharacterChanged` | `event Action<string>` — new character id |


## PlayerPrefs Keys

| Key | Value | Description |
| --- | ----- | ----------- |
| `char_unlock_<id>` | `0` / `1` | Availability state for each character |


## Optional Integrations

### SaveManager (`CHARACTERMANAGER_SM`)

Requires `CHARACTERMANAGER_SM` define and [SaveManager](https://github.com/RolandKaechele/SaveManager).

### GalleryManager (`CHARACTERMANAGER_GM`)

Requires `CHARACTERMANAGER_GM` define and [GalleryManager](https://github.com/RolandKaechele/GalleryManager).  
Calls `GalleryManager.UnlockStatic(id)` whenever a character is unlocked — useful when character portraits are gallery entries with matching ids.

### EventManager (`CHARACTERMANAGER_EM`)

Requires `CHARACTERMANAGER_EM` define. The following named GameEvents are fired:

| Event name | When |
| ---------- | ---- |
| `CharacterUnlocked` | `Unlock(id)`; value = character id |
| `ActiveCharacterChanged` | `SetActive(id)`; value = new character id |


## Dependencies

| Dependency | Required | Notes |
| ---------- | -------- | ----- |
| Unity 2022.3+ | ✓ | |
| SaveManager | optional | Required when `CHARACTERMANAGER_SM` is defined |
| GalleryManager | optional | Required when `CHARACTERMANAGER_GM` is defined |
| EventManager | optional | Required when `CHARACTERMANAGER_EM` is defined |


## Repository

[https://github.com/RolandKaechele/CharacterManager](https://github.com/RolandKaechele/CharacterManager)


## License

MIT — see [LICENSE](LICENSE).
