#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using CharacterManager.Runtime;
using UnityEditor;
using UnityEngine;

namespace CharacterManager.Editor
{
    // ────────────────────────────────────────────────────────────────────────────
    // Character JSON Editor Window
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Editor window for creating and editing <c>characters.json</c> in StreamingAssets.
    /// Open via <b>JSON Editors → Character Manager</b> or via the Manager Inspector button.
    /// </summary>
    public class CharacterJsonEditorWindow : EditorWindow
    {
        private const string JsonFileName = "characters.json";

        private CharacterEditorBridge    _bridge;
        private UnityEditor.Editor       _bridgeEditor;
        private Vector2                  _scroll;
        private string                   _status;
        private bool                     _statusError;

        [MenuItem("JSON Editors/Character Manager")]
        public static void ShowWindow() =>
            GetWindow<CharacterJsonEditorWindow>("Character JSON");

        private void OnEnable()
        {
            _bridge = CreateInstance<CharacterEditorBridge>();
            Load();
        }

        private void OnDisable()
        {
            if (_bridgeEditor != null) DestroyImmediate(_bridgeEditor);
            if (_bridge      != null) DestroyImmediate(_bridge);
        }

        private void OnGUI()
        {
            DrawToolbar();

            EditorGUILayout.HelpBox(
                "Note: Sprite/AudioClip (UnityEngine.Object) fields like 'portrait' cannot be stored in JSON " +
                "and will be null after a Load or Save round-trip.",
                MessageType.Warning);

            if (!string.IsNullOrEmpty(_status))
                EditorGUILayout.HelpBox(_status, _statusError ? MessageType.Error : MessageType.Info);

            if (_bridge == null) return;
            if (_bridgeEditor == null)
                _bridgeEditor = UnityEditor.Editor.CreateEditor(_bridge);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            _bridgeEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField(
                Path.Combine("StreamingAssets", JsonFileName),
                EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(50))) Load();
            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50))) Save();
            EditorGUILayout.EndHorizontal();
        }

        private void Load()
        {
            var path = Path.Combine(Application.streamingAssetsPath, JsonFileName);
            try
            {
                if (!File.Exists(path))
                {
                    File.WriteAllText(path, JsonUtility.ToJson(new CharacterEditorWrapper(), true));
                    AssetDatabase.Refresh();
                }

                var w = JsonUtility.FromJson<CharacterEditorWrapper>(File.ReadAllText(path));
                _bridge.characters = new List<CharacterDefinition>(
                    w.characters ?? Array.Empty<CharacterDefinition>());

                if (_bridgeEditor != null) { DestroyImmediate(_bridgeEditor); _bridgeEditor = null; }

                _status     = $"Loaded {_bridge.characters.Count} characters.";
                _statusError = false;
            }
            catch (Exception e)
            {
                _status     = $"Load error: {e.Message}";
                _statusError = true;
            }
        }

        private void Save()
        {
            try
            {
                var w    = new CharacterEditorWrapper { characters = _bridge.characters.ToArray() };
                var path = Path.Combine(Application.streamingAssetsPath, JsonFileName);
                File.WriteAllText(path, JsonUtility.ToJson(w, true));
                AssetDatabase.Refresh();
                _status     = $"Saved {_bridge.characters.Count} characters to {JsonFileName}.";
                _statusError = false;
            }
            catch (Exception e)
            {
                _status     = $"Save error: {e.Message}";
                _statusError = true;
            }
        }
    }

    // ── ScriptableObject bridge ──────────────────────────────────────────────
    internal class CharacterEditorBridge : ScriptableObject
    {
        public List<CharacterDefinition> characters = new List<CharacterDefinition>();
    }

    // ── Local wrapper mirrors the internal CharacterRosterJson ───────────────
    [Serializable]
    internal class CharacterEditorWrapper
    {
        public CharacterDefinition[] characters = Array.Empty<CharacterDefinition>();
    }
}
#endif
