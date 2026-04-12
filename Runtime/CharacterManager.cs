using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace CharacterManager.Runtime
{
    // -------------------------------------------------------------------------
    // CharacterStats
    // -------------------------------------------------------------------------

    /// <summary>Base numeric stats for a character. Extend by subclassing or adding fields.</summary>
    [Serializable]
    public class CharacterStats
    {
        [Tooltip("Maximum health points.")]
        public int health = 100;

        [Tooltip("Movement speed.")]
        public float speed = 5f;

        [Tooltip("Base attack power.")]
        public int attackPower = 10;
    }

    // -------------------------------------------------------------------------
    // CharacterDefinition
    // -------------------------------------------------------------------------

    /// <summary>
    /// Defines a single character in the roster.
    /// Serializable so it can be defined in the Inspector and loaded from JSON.
    /// </summary>
    [Serializable]
    public class CharacterDefinition
    {
        [Tooltip("Unique identifier (e.g. 'jan_tenner').")]
        public string id;

        [Tooltip("Display name shown in the UI.")]
        public string displayName;

        [Tooltip("Character biography or description.")]
        [TextArea(2, 5)]
        public string bio;

        [Tooltip("Portrait sprite used in the UI.")]
        public Sprite portrait;

        public CharacterStats stats;

        [Tooltip("Always available without an explicit unlock. E.g. the main protagonist.")]
        public bool alwaysAvailable;
    }

    // -------------------------------------------------------------------------
    // JSON wrapper
    // -------------------------------------------------------------------------

    [Serializable]
    internal class CharacterRosterJson
    {
        public CharacterDefinition[] characters;
    }

    // -------------------------------------------------------------------------
    // CharacterManager
    // -------------------------------------------------------------------------

    /// <summary>
    /// <b>CharacterManager</b> maintains the character roster: definitions, availability state, and the active character.
    ///
    /// <para><b>Responsibilities:</b>
    /// <list type="number">
    ///   <item>Store character definitions (id, name, bio, portrait, stats).</item>
    ///   <item>Track which characters are available using <c>PlayerPrefs</c>.</item>
    ///   <item>Track the currently active (player-controlled) character.</item>
    ///   <item>Optionally merge definitions from a JSON file for modding.</item>
    /// </list>
    /// </para>
    ///
    /// <para><b>Modding / JSON:</b> Enable <c>loadFromJson</c> and place a
    /// <c>characters.json</c> in <c>StreamingAssets/</c>.
    /// JSON entries are <b>merged by id</b>: JSON overrides Inspector entries with the same id and can add new ones.</para>
    ///
    /// <para><b>Optional integration defines:</b>
    /// <list type="bullet">
    ///   <item><c>CHARACTERMANAGER_SM</c> — SaveManager: checks <c>char_&lt;id&gt;</c> save flags as an additional availability source.</item>
    ///   <item><c>CHARACTERMANAGER_EM</c> — EventManager: fires <c>CharacterUnlocked</c> and <c>ActiveCharacterChanged</c> as GameEvents.</item>
    ///   <item><c>CHARACTERMANAGER_GM</c> — GalleryManager: calls <c>GalleryManager.UnlockStatic(id)</c> when a character is unlocked.</item>
    /// </list>
    /// </para>
    /// </summary>
    [AddComponentMenu("CharacterManager/Character Manager")]
    [DisallowMultipleComponent]
#if ODIN_INSPECTOR
    public class CharacterManager : SerializedMonoBehaviour
#else
    public class CharacterManager : MonoBehaviour
#endif
    {
        // -------------------------------------------------------------------------
        // Inspector
        // -------------------------------------------------------------------------

        [Header("Roster")]
        [Tooltip("All character definitions for this game.")]
        [SerializeField] private CharacterDefinition[] characters = Array.Empty<CharacterDefinition>();

        [Header("Active Character")]
        [Tooltip("ID of the default active character on start. Leave empty for none.")]
        [SerializeField] private string defaultCharacterId;

        [Header("Modding / JSON")]
        [Tooltip("When enabled, merge roster definitions from a JSON file in StreamingAssets/ at startup.")]
        [SerializeField] private bool loadFromJson = false;

        [Tooltip("Path relative to StreamingAssets/ (e.g. 'characters/' or 'characters.json').")]
        [SerializeField] private string jsonPath = "characters/";

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------

        /// <summary>Fired when a character is unlocked. Parameter: character id.</summary>
        public event Action<string> OnCharacterUnlocked;

        /// <summary>Fired when the active character changes. Parameter: new character id.</summary>
        public event Action<string> OnActiveCharacterChanged;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------

        private const string UnlockPrefix = "char_unlock_";

        private readonly List<CharacterDefinition> _roster = new();
        private readonly Dictionary<string, CharacterDefinition> _index = new();
        private string _activeCharacterId;

        /// <summary>ID of the currently active character (null if none set).</summary>
        public string ActiveCharacterId => _activeCharacterId;

        /// <summary>Read-only character roster (merged Inspector + JSON).</summary>
        public IReadOnlyList<CharacterDefinition> Characters => _roster;

        // -------------------------------------------------------------------------
        // Unity lifecycle
        // -------------------------------------------------------------------------

        private void Awake()
        {
            BuildIndex();
            if (loadFromJson) LoadJson();
            _activeCharacterId = defaultCharacterId;
        }

        private void BuildIndex()
        {
            _roster.Clear();
            _index.Clear();
            foreach (var c in characters)
            {
                if (c == null || string.IsNullOrEmpty(c.id)) continue;
                _roster.Add(c);
                _index[c.id] = c;
            }
        }

        private void LoadJson()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, jsonPath);
            if (Directory.Exists(fullPath))
            {
                foreach (var file in Directory.GetFiles(fullPath, "*.json", SearchOption.TopDirectoryOnly))
                    MergeCharactersFromFile(file);
            }
            else if (File.Exists(fullPath))
            {
                MergeCharactersFromFile(fullPath);
            }
            else
            {
                Debug.LogWarning($"[CharacterManager] JSON not found: {fullPath}");
            }
        }

        private void MergeCharactersFromFile(string path)
        {
            try
            {
                var wrapper = JsonUtility.FromJson<CharacterRosterJson>(File.ReadAllText(path));
                if (wrapper?.characters == null) return;
                foreach (var c in wrapper.characters)
                {
                    if (c == null || string.IsNullOrEmpty(c.id)) continue;
                    if (_index.ContainsKey(c.id))
                    {
                        int i = _roster.FindIndex(x => x.id == c.id);
                        if (i >= 0) _roster[i] = c;
                        _index[c.id] = c;
                    }
                    else
                    {
                        _roster.Add(c);
                        _index[c.id] = c;
                    }
                }
                Debug.Log($"[CharacterManager] Merged from {path}.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CharacterManager] Failed to load JSON: {ex.Message}");
            }
        }

        // -------------------------------------------------------------------------
        // Availability
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns true if the character identified by <paramref name="id"/> is available.
        /// Check order: <c>alwaysAvailable</c> flag → PlayerPrefs → SaveManager flag (if <c>CHARACTERMANAGER_SM</c>).
        /// </summary>
        public bool IsAvailable(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (_index.TryGetValue(id, out var c) && c.alwaysAvailable) return true;
            if (PlayerPrefs.GetInt(UnlockPrefix + id, 0) == 1) return true;
#if CHARACTERMANAGER_SM
            var sm = FindFirstObjectByType<SaveManager.Runtime.SaveManager>();
            if (sm != null && sm.IsSet("char_" + id)) return true;
#endif
            return false;
        }

        /// <summary>
        /// Make the character with <paramref name="id"/> available and persist the state.
        /// Fires <see cref="OnCharacterUnlocked"/>. Idempotent.
        /// </summary>
        public void Unlock(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            bool wasLocked = !IsAvailable(id);
            PlayerPrefs.SetInt(UnlockPrefix + id, 1);
            PlayerPrefs.Save();
            if (!wasLocked) return;

            OnCharacterUnlocked?.Invoke(id);
#if CHARACTERMANAGER_EM
            FindFirstObjectByType<EventManager.Runtime.EventManager>()?.Fire("CharacterUnlocked", id);
#endif
#if CHARACTERMANAGER_GM
            GalleryManager.Runtime.GalleryManager.UnlockStatic(id);
#endif
            Debug.Log($"[CharacterManager] Unlocked character: {id}");
        }

        /// <summary>Remove the availability flag for <paramref name="id"/> from PlayerPrefs. For testing/reset only.</summary>
        public void Lock(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            PlayerPrefs.DeleteKey(UnlockPrefix + id);
            PlayerPrefs.Save();
        }

        // -------------------------------------------------------------------------
        // Active character
        // -------------------------------------------------------------------------

        /// <summary>
        /// Set the currently active character. Fires <see cref="OnActiveCharacterChanged"/>.
        /// </summary>
        public void SetActive(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (!_index.ContainsKey(id))
            {
                Debug.LogWarning($"[CharacterManager] Character '{id}' not found.");
                return;
            }
            _activeCharacterId = id;
            OnActiveCharacterChanged?.Invoke(id);
#if CHARACTERMANAGER_EM
            FindFirstObjectByType<EventManager.Runtime.EventManager>()?.Fire("ActiveCharacterChanged", id);
#endif
        }

        /// <summary>Returns the <see cref="CharacterDefinition"/> for the active character, or null.</summary>
        public CharacterDefinition GetActiveCharacter() =>
            string.IsNullOrEmpty(_activeCharacterId) ? null : GetDefinition(_activeCharacterId);

        // -------------------------------------------------------------------------
        // Queries
        // -------------------------------------------------------------------------

        /// <summary>Returns the <see cref="CharacterDefinition"/> for <paramref name="id"/>, or null.</summary>
        public CharacterDefinition GetDefinition(string id) =>
            _index.TryGetValue(id, out var c) ? c : null;

        /// <summary>Returns all characters that are currently available.</summary>
        public List<CharacterDefinition> GetAvailableCharacters()
        {
            var result = new List<CharacterDefinition>();
            foreach (var c in _roster)
                if (c != null && IsAvailable(c.id))
                    result.Add(c);
            return result;
        }

        // -------------------------------------------------------------------------
        // Static helper
        // -------------------------------------------------------------------------

        /// <summary>
        /// Unlock a character via PlayerPrefs without a scene reference.
        /// Use from gameplay scripts when CharacterManager may not be in the same scene.
        /// </summary>
        public static void UnlockStatic(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            PlayerPrefs.SetInt(UnlockPrefix + id, 1);
            PlayerPrefs.Save();
        }
    }
}
