// Zamjena za uGUI, EventSystems, Editor i Test Framework (samo za provjeru kompajliranja). Vidi UnityEngineStubs.cs.
#pragma warning disable CS0067, CS0414, CS0649, CS0108
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace UnityEngine.Events
{
    public class UnityEvent { public void AddListener(Action a) { } }
    public class UnityEvent<T> { public void AddListener(Action<T> a) { } }
}

namespace UnityEngine.EventSystems
{
    public class UIBehaviour : MonoBehaviour { }
    public class BaseEventData { }
    public class PointerEventData : BaseEventData
    {
        public Vector2 position { get; set; }
        public Vector2 delta { get; set; }
        public Vector2 scrollDelta { get; set; }
        public Camera pressEventCamera => null;
        public Camera enterEventCamera => null;
    }
    public interface IEventSystemHandler { }
    public interface IPointerDownHandler : IEventSystemHandler { void OnPointerDown(PointerEventData e); }
    public interface IPointerUpHandler : IEventSystemHandler { void OnPointerUp(PointerEventData e); }
    public interface IPointerClickHandler : IEventSystemHandler { void OnPointerClick(PointerEventData e); }
    public interface IPointerExitHandler : IEventSystemHandler { void OnPointerExit(PointerEventData e); }
    public interface IDragHandler : IEventSystemHandler { void OnDrag(PointerEventData e); }
    public interface IScrollHandler : IEventSystemHandler { void OnScroll(PointerEventData e); }
    public class EventSystem : UIBehaviour { public static EventSystem current { get; set; } }
    public class BaseInputModule : UIBehaviour { }
    public class StandaloneInputModule : BaseInputModule { }
}

namespace UnityEngine.UI
{
    public abstract class Graphic : UIBehaviour
    {
        public Color color { get; set; }
        public bool raycastTarget { get; set; }
        public virtual Material material { get; set; }
        public RectTransform rectTransform => null;
    }
    public abstract class MaskableGraphic : Graphic { }
    public class Image : MaskableGraphic
    {
        public enum Type { Simple, Sliced, Tiled, Filled }
        public Sprite sprite { get; set; }
        public Type type { get; set; }
        public float pixelsPerUnitMultiplier { get; set; }
        public bool preserveAspect { get; set; }
    }
    public class RawImage : MaskableGraphic { public Texture texture { get; set; } public Rect uvRect { get; set; } }
    public class Text : MaskableGraphic
    {
        public float preferredHeight => 0;
        public float preferredWidth => 0;
        public string text { get; set; }
        public Font font { get; set; }
        public int fontSize { get; set; }
        public TextAnchor alignment { get; set; }
        public HorizontalWrapMode horizontalOverflow { get; set; }
        public VerticalWrapMode verticalOverflow { get; set; }
        public bool supportRichText { get; set; }
        public float lineSpacing { get; set; }
        public bool resizeTextForBestFit { get; set; }
        public int resizeTextMinSize { get; set; }
        public int resizeTextMaxSize { get; set; }
        public FontStyle fontStyle { get; set; }
    }
    public class BaseMeshEffect : UIBehaviour { }
    public class Shadow : BaseMeshEffect { public Color effectColor { get; set; } public Vector2 effectDistance { get; set; } }
    public class Outline : Shadow { }
    public class Canvas : Behaviour { public RenderMode renderMode { get; set; } public int sortingOrder { get; set; } public static void ForceUpdateCanvases() { } }
    public class CanvasScaler : UIBehaviour
    {
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }
        public enum ScreenMatchMode { MatchWidthOrHeight, Expand, Shrink }
        public ScaleMode uiScaleMode { get; set; }
        public Vector2 referenceResolution { get; set; }
        public ScreenMatchMode screenMatchMode { get; set; }
        public float matchWidthOrHeight { get; set; }
    }
    public class GraphicRaycaster : UIBehaviour { }
    public class LayoutElement : UIBehaviour
    {
        public float minWidth { get; set; } public float minHeight { get; set; }
        public float preferredWidth { get; set; } public float preferredHeight { get; set; }
        public float flexibleWidth { get; set; } public float flexibleHeight { get; set; }
    }
    public abstract class LayoutGroup : UIBehaviour
    {
        public RectOffset padding { get; set; }
        public TextAnchor childAlignment { get; set; }
    }
    public abstract class HorizontalOrVerticalLayoutGroup : LayoutGroup
    {
        public float spacing { get; set; }
        public bool childForceExpandWidth { get; set; }
        public bool childForceExpandHeight { get; set; }
        public bool childControlWidth { get; set; }
        public bool childControlHeight { get; set; }
    }
    public class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class GridLayoutGroup : LayoutGroup
    {
        public enum Constraint { Flexible, FixedColumnCount, FixedRowCount }
        public Vector2 cellSize { get; set; }
        public Vector2 spacing { get; set; }
        public Constraint constraint { get; set; }
        public int constraintCount { get; set; }
    }
    public class ContentSizeFitter : UIBehaviour
    {
        public enum FitMode { Unconstrained, MinSize, PreferredSize }
        public FitMode horizontalFit { get; set; }
        public FitMode verticalFit { get; set; }
    }
    public class AspectRatioFitter : UIBehaviour
    {
        public enum AspectMode { None, WidthControlsHeight, HeightControlsWidth, FitInParent, EnvelopeParent }
        public AspectMode aspectMode { get; set; }
        public float aspectRatio { get; set; }
    }
    public class RectMask2D : UIBehaviour { }
    public class Mask : UIBehaviour { }
    public class ScrollRect : UIBehaviour
    {
        public enum MovementType { Unrestricted, Elastic, Clamped }
        public bool horizontal { get; set; }
        public MovementType movementType { get; set; }
        public float scrollSensitivity { get; set; }
        public RectTransform content { get; set; }
        public RectTransform viewport { get; set; }
    }
    public class Selectable : UIBehaviour { public bool interactable { get; set; } public Graphic targetGraphic { get; set; } }
    public class Slider : Selectable
    {
        public class SliderEvent : UnityEngine.Events.UnityEvent<float> { }
        public RectTransform fillRect { get; set; }
        public RectTransform handleRect { get; set; }
        public float minValue { get; set; }
        public float maxValue { get; set; }
        public float value { get; set; }
        public SliderEvent onValueChanged { get; } = new SliderEvent();
        public void SetValueWithoutNotify(float v) { }
    }
    public class InputField : Selectable
    {
        public enum ContentType { Standard, Autocorrected, IntegerNumber, DecimalNumber }
        public class SubmitEvent : UnityEngine.Events.UnityEvent<string> { }
        public Text textComponent { get; set; }
        public Graphic placeholder { get; set; }
        public int characterLimit { get; set; }
        public string text { get; set; }
        public ContentType contentType { get; set; }
        public SubmitEvent onEndEdit { get; } = new SubmitEvent();
        public void SetTextWithoutNotify(string s) { }
    }
}

namespace UnityEngine
{
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
    public sealed class CanvasGroup : Behaviour { public float alpha { get; set; } public bool blocksRaycasts { get; set; } public bool interactable { get; set; } }
    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }
    public enum FontStyle { Normal, Bold }
    public class RectOffset { public RectOffset() { } public RectOffset(int l, int r, int t, int b) { } }
    public enum FullScreenMode { ExclusiveFullScreen, FullScreenWindow, MaximizedWindow, Windowed }
    public enum UIOrientation { Portrait, PortraitUpsideDown, LandscapeRight, LandscapeLeft, AutoRotation }
}

namespace UnityEngine.TestTools
{
    [AttributeUsage(AttributeTargets.Method)] public sealed class UnityTestAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public sealed class UnityTearDownAttribute : Attribute { }
}

namespace NUnit.Framework
{
    [AttributeUsage(AttributeTargets.Method)] public sealed class TimeoutAttribute : Attribute { public TimeoutAttribute(int ms) { } }
}

namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class InitializeOnLoadAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public sealed class MenuItemAttribute : Attribute { public MenuItemAttribute(string p) { } public int priority; }
    public static class EditorApplication { public static Action delayCall; }
    public static class SessionState { public static bool GetBool(string k, bool d) => d; public static void SetBool(string k, bool v) { } }
    public static class AssetDatabase
    {
        public static T LoadAssetAtPath<T>(string p) where T : UnityEngine.Object => null;
        public static void CreateAsset(UnityEngine.Object o, string p) { }
        public static void SaveAssets() { }
    }
    public static class EditorUtility
    {
        public static bool DisplayDialog(string t, string m, string ok) => true;
        public static void SetDirty(UnityEngine.Object o) { }
        public static void RevealInFinder(string p) { }
    }
    public class SerializedProperty { public bool boolValue { get; set; } public int intValue { get; set; } }
    public class SerializedObject
    {
        public SerializedObject(UnityEngine.Object o) { }
        public SerializedProperty FindProperty(string n) => null;
        public bool ApplyModifiedPropertiesWithoutUndo() => true;
    }
    public enum ScriptingImplementation { Mono2x, IL2CPP }
    [Flags] public enum AndroidArchitecture { None = 0, ARMv7 = 1, ARM64 = 2 }
    public enum AndroidSdkVersions { AndroidApiLevel23 = 23, AndroidApiLevel24 = 24, AndroidApiLevel26 = 26 }
    public static class PlayerSettings
    {
        public static string companyName { get; set; }
        public static string productName { get; set; }
        public static UnityEngine.ColorSpace colorSpace { get; set; }
        public static UnityEngine.UIOrientation defaultInterfaceOrientation { get; set; }
        public static bool allowedAutorotateToPortrait { get; set; }
        public static bool allowedAutorotateToPortraitUpsideDown { get; set; }
        public static bool allowedAutorotateToLandscapeLeft { get; set; }
        public static bool allowedAutorotateToLandscapeRight { get; set; }
        public static bool runInBackground { get; set; }
        public static UnityEngine.FullScreenMode fullScreenMode { get; set; }
        public static int defaultScreenWidth { get; set; }
        public static int defaultScreenHeight { get; set; }
        public static bool resizableWindow { get; set; }
        public static string bundleVersion { get; set; }
        public static void SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget t, string id) { }
        public static void SetScriptingBackend(UnityEditor.Build.NamedBuildTarget t, ScriptingImplementation s) { }
        public static class Android
        {
            public static AndroidArchitecture targetArchitectures { get; set; }
            public static AndroidSdkVersions minSdkVersion { get; set; }
            public static bool useCustomKeystore { get; set; }
        }
    }
    public sealed class EditorBuildSettingsScene { public EditorBuildSettingsScene(string p, bool e) { } }
    public static class EditorBuildSettings { public static EditorBuildSettingsScene[] scenes { get; set; } }
    public class AssetImporter : UnityEngine.Object { }
    public sealed class TextureImporter : AssetImporter
    {
        public TextureImporterType textureType { get; set; }
        public bool mipmapEnabled { get; set; }
        public UnityEngine.TextureWrapMode wrapMode { get; set; }
        public UnityEngine.FilterMode filterMode { get; set; }
        public int maxTextureSize { get; set; }
        public bool alphaIsTransparency { get; set; }
        public TextureImporterNPOTScale npotScale { get; set; }
        public TextureImporterCompression textureCompression { get; set; }
    }
    public enum TextureImporterNPOTScale { None, ToNearest, ToLarger, ToSmaller }
    public enum TextureImporterCompression { Uncompressed, Compressed, CompressedHQ, CompressedLQ }
    public enum TextureImporterType { Default, NormalMap, GUI, Sprite }
    public sealed class TrueTypeFontImporter : AssetImporter { public bool includeFontData { get; set; } public string[] fontNames { get; set; } }
    public class AssetPostprocessor { public virtual uint GetVersion() => 0; public string assetPath => ""; public AssetImporter assetImporter => null; }
}

namespace UnityEditor
{
    public enum BuildTarget { StandaloneWindows64, Android, iOS, WebGL }
    public enum BuildTargetGroup { Standalone, Android, iOS, WebGL }
    [System.Flags] public enum BuildOptions { None = 0, Development = 1 }
    public struct BuildPlayerOptions { public string[] scenes; public string locationPathName; public BuildTarget target; public BuildOptions options; }
    public static class EditorUserBuildSettings
    {
        public static BuildTarget activeBuildTarget { get; set; }
        public static bool buildAppBundle { get; set; }
        public static bool SwitchActiveBuildTarget(BuildTargetGroup g, BuildTarget t) => true;
    }
    public static class BuildPipeline { public static UnityEditor.Build.Reporting.BuildReport BuildPlayer(BuildPlayerOptions o) => new UnityEditor.Build.Reporting.BuildReport(); }
}

namespace UnityEditor.Build.Reporting
{
    public enum BuildResult { Unknown, Succeeded, Failed, Cancelled }
    public struct BuildSummary { public BuildResult result; public ulong totalSize; public System.TimeSpan totalTime; public int totalErrors; }
    public class BuildReport { public BuildSummary summary; }
}

namespace UnityEditor.Build
{
    public struct NamedBuildTarget { public static NamedBuildTarget Android => default; public static NamedBuildTarget iOS => default; }
}

namespace UnityEditor.SceneManagement
{
    public enum NewSceneSetup { EmptyScene, DefaultGameObjects }
    public enum NewSceneMode { Single, Additive }
    public static class EditorSceneManager
    {
        public static bool SaveCurrentModifiedScenesIfUserWantsTo() => true;
        public static UnityEngine.SceneManagement.Scene NewScene(NewSceneSetup s, NewSceneMode m) => default;
        public static bool SaveScene(UnityEngine.SceneManagement.Scene s, string p) => true;
    }
}

namespace UnityEditor.TestTools.TestRunner.Api
{
    public enum TestMode { EditMode = 1, PlayMode = 2 }
    public class Filter { public TestMode testMode; public string[] testNames; }
    public class ExecutionSettings { public ExecutionSettings(params Filter[] f) { } }
    public class TestRunnerApi : UnityEngine.ScriptableObject { public string Execute(ExecutionSettings s) => ""; }
}

namespace UnityEngine.UI
{
    public static class LayoutUtility
    {
        public static float GetPreferredHeight(UnityEngine.RectTransform rt) => 0;
        public static float GetMinHeight(UnityEngine.RectTransform rt) => 0;
    }
}
