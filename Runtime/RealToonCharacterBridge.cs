#if CHARACTERMANAGER_REALTOON
using UnityEngine;
using DG.Tweening;

namespace CharacterManager.Runtime
{
    /// <summary>
    /// Optional bridge that applies RealToon Pro shader properties via
    /// <see cref="MaterialPropertyBlock"/> on renderers of the active character whenever
    /// <see cref="CharacterManager.OnActiveCharacterChanged"/> fires.
    /// Enable define <c>CHARACTERMANAGER_REALTOON</c> in Player Settings › Scripting Define Symbols.
    /// Requires <b>RealToon Pro</b>.
    /// <para>
    /// Assign a <see cref="characterRoot"/> transform to search for renderers, or leave it
    /// unassigned to search the entire scene. RealToon shader property values are configured
    /// in the Inspector and applied per-renderer using a shared <see cref="MaterialPropertyBlock"/>.
    /// </para>
    /// </summary>
    [AddComponentMenu("CharacterManager/RealToon Bridge")]
    [DisallowMultipleComponent]
    public class RealToonCharacterBridge : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Root transform of the character hierarchy to search for Renderers. " +
                 "Leave unassigned to locate renderers via FindFirstObjectByType each time the character changes.")]
        [SerializeField] private Transform characterRoot;

        [Header("RealToon — Outline")]
        [Tooltip("Outline width applied to all character renderers.")]
        [Range(0f, 0.01f)]
        [SerializeField] private float outlineWidth = 0.003f;

        [Tooltip("Outline color.")]
        [SerializeField] private Color outlineColor = Color.black;

        [Header("RealToon — Rim Light")]
        [Tooltip("Rim light power (higher = tighter rim).")]
        [Range(0f, 10f)]
        [SerializeField] private float rimLightPower = 3f;

        [Tooltip("Rim light color.")]
        [SerializeField] private Color rimLightColor = new Color(0.8f, 0.9f, 1f, 1f);

        [Header("RealToon — Self Shadow")]
        [Tooltip("Self-shadow intensity.")]
        [Range(0f, 1f)]
        [SerializeField] private float selfShadowIntensity = 0.5f;

        [Header("Transition")]
        [Tooltip("DOTween duration for blending RealToon properties in when the active character changes.")]
        [SerializeField] private float blendInDuration = 0.3f;

        [Tooltip("DOTween ease for property blend-in.")]
        [SerializeField] private Ease blendEase = Ease.OutSine;

        // RealToon Pro shader property IDs
        private static readonly int PropOutlineWidth        = Shader.PropertyToID("_OutlineWidth");
        private static readonly int PropOutlineColor        = Shader.PropertyToID("_OutlineColor");
        private static readonly int PropRimLightPower       = Shader.PropertyToID("_RimLightPower");
        private static readonly int PropRimLightColor       = Shader.PropertyToID("_RimLightColor");
        private static readonly int PropSelfShadowIntensity = Shader.PropertyToID("_SelfShadowIntensity");

        // -------------------------------------------------------------------------

        private CharacterManager _cm;
        private MaterialPropertyBlock _mpb;

        private void Awake()
        {
            _cm  = GetComponent<CharacterManager>() ?? FindFirstObjectByType<CharacterManager>();
            _mpb = new MaterialPropertyBlock();

            if (_cm == null) Debug.LogWarning("[CharacterManager/RealToonCharacterBridge] CharacterManager not found.");
        }

        private void OnEnable()
        {
            if (_cm != null) _cm.OnActiveCharacterChanged += OnActiveCharacterChanged;
        }

        private void OnDisable()
        {
            if (_cm != null) _cm.OnActiveCharacterChanged -= OnActiveCharacterChanged;
        }

        // -------------------------------------------------------------------------

        private void OnActiveCharacterChanged(string characterId)
        {
            Renderer[] renderers = ResolveRenderers();
            if (renderers == null || renderers.Length == 0) return;

            // Tween outline width from 0 → target over blendInDuration.
            float targetWidth = outlineWidth;
            DOVirtual.Float(0f, targetWidth, blendInDuration, w =>
            {
                foreach (var r in renderers)
                {
                    if (r == null) continue;
                    r.GetPropertyBlock(_mpb);
                    _mpb.SetFloat(PropOutlineWidth,        w);
                    _mpb.SetColor(PropOutlineColor,        outlineColor);
                    _mpb.SetFloat(PropRimLightPower,       rimLightPower);
                    _mpb.SetColor(PropRimLightColor,       rimLightColor);
                    _mpb.SetFloat(PropSelfShadowIntensity, selfShadowIntensity);
                    r.SetPropertyBlock(_mpb);
                }
            }).SetEase(blendEase);
        }

        private Renderer[] ResolveRenderers()
        {
            if (characterRoot != null)
                return characterRoot.GetComponentsInChildren<Renderer>(false);

            var go = FindFirstObjectByType<CharacterManager>()?.gameObject;
            return go != null ? go.GetComponentsInChildren<Renderer>(false) : null;
        }
    }
}
#else
namespace CharacterManager.Runtime
{
    /// <summary>No-op stub — enable define <c>CHARACTERMANAGER_REALTOON</c> to activate.</summary>
    [UnityEngine.AddComponentMenu("CharacterManager/RealToon Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class RealToonCharacterBridge : UnityEngine.MonoBehaviour { }
}
#endif
