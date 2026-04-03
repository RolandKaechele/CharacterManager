#if UNITY_EDITOR
using CharacterManager.Runtime;
using UnityEditor;
using UnityEngine;

namespace CharacterManager.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="CharacterManager.Runtime.CharacterManager"/>.
    /// Validates configuration and shows per-character availability controls at runtime.
    /// </summary>
    [CustomEditor(typeof(CharacterManager.Runtime.CharacterManager))]
    public class CharacterManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(6);

            // ── Validation ──────────────────────────────────────────────────────

            var defaultIdProp   = serializedObject.FindProperty("defaultCharacterId");
            var charactersProp  = serializedObject.FindProperty("characters");

            if (defaultIdProp != null && string.IsNullOrEmpty(defaultIdProp.stringValue))
                EditorGUILayout.HelpBox(
                    "Default Character ID is empty — no active character will be set on start.",
                    MessageType.Info);

            if (charactersProp != null && charactersProp.arraySize == 0)
                EditorGUILayout.HelpBox(
                    "No characters defined. Add characters in the Inspector or enable JSON loading.",
                    MessageType.Info);

            // ── Runtime controls (Play Mode only) ───────────────────────────────

            if (!Application.isPlaying) return;

            var mgr = (CharacterManager.Runtime.CharacterManager)target;

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Active Character", mgr.ActiveCharacterId ?? "(none)", EditorStyles.boldLabel);

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Roster", EditorStyles.miniBoldLabel);

            var characters = mgr.Characters;
            if (characters.Count == 0)
            {
                EditorGUILayout.LabelField("  (none)");
            }
            else
            {
                foreach (var c in characters)
                {
                    if (c == null) continue;
                    bool available = c.alwaysAvailable || mgr.IsAvailable(c.id);
                    bool isActive  = mgr.ActiveCharacterId == c.id;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"  {c.displayName ?? c.id}" + (isActive ? "  ★" : ""));
                    EditorGUILayout.LabelField(c.alwaysAvailable ? "always" : (available ? "✓" : "—"), GUILayout.Width(55));
                    GUI.enabled = !available && !c.alwaysAvailable;
                    if (GUILayout.Button("Unlock",     GUILayout.Width(60))) mgr.Unlock(c.id);
                    GUI.enabled = available && !c.alwaysAvailable;
                    if (GUILayout.Button("Lock",       GUILayout.Width(50))) mgr.Lock(c.id);
                    GUI.enabled = true;
                    if (GUILayout.Button("Set Active", GUILayout.Width(75))) mgr.SetActive(c.id);
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
    }
}
#endif
