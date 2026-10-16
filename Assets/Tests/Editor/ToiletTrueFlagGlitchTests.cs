using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class ToiletTrueFlagGlitchTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly List<GameObject> _objects = new();
    private readonly Dictionary<FieldInfo, object> _singletons = new();
    private Component _mapManager;
    private Component _toilet;
    private Component _cutscene;
    private Component _save;
    private Component _inventory;
    private Component _glitch;
    private Material _material;
    private float _timeScale;

    [SetUp]
    public void SetUp()
    {
        _timeScale = Time.timeScale;
        foreach (string name in new[] { "MapManager", "GlitchEffectController", "SaveManager",
                     "UIManager", "SoundManager", "ControlLockManager", "InventoryManager",
                     "LightingEventManager", "CameraManager" })
        {
            FieldInfo field = SingletonField(RuntimeType(name));
            _singletons.Add(field, field.GetValue(null));
            field.SetValue(null, null);
        }

        _mapManager = CreateComponent("MapManager");
        _glitch = CreateComponent("GlitchEffectController");
        _toilet = CreateComponent("MapArea");
        _cutscene = CreateComponent("ToiletTrueFlagCutsceneController");
        _cutscene.transform.SetParent(_toilet.transform);
        ((IList)GetField(_mapManager, "allMapAreas")).Add(_toilet);
        ((IDictionary)GetField(_toilet, "_entryPointDict")).Add("test-entry", _cutscene.transform);
        _save = CreateComponent("SaveManager");
        _inventory = CreateComponent("InventoryManager");
        SingletonField(_mapManager.GetType()).SetValue(null, _mapManager);
        SingletonField(_glitch.GetType()).SetValue(null, _glitch);
        SingletonField(_save.GetType()).SetValue(null, _save);
        SingletonField(_inventory.GetType()).SetValue(null, _inventory);
        Invoke(_inventory, "AddKeyItem", "hanky");
        SetField(_toilet, "mapType", MapType("School_ManToilet"));
        SetField(_mapManager, "_currentArea", _toilet);
        _material = new Material(Shader.Find("FullScreenGlitch"));
        SetField(_glitch, "glitchMaterial", _material);
    }

    [TearDown]
    public void TearDown()
    {
        Invoke(_cutscene, "StopToiletGlitch");
        for (int i = _objects.Count - 1; i >= 0; i--)
            UnityEngine.Object.DestroyImmediate(_objects[i]);
        _objects.Clear();
        UnityEngine.Object.DestroyImmediate(_material);
        foreach (var pair in _singletons)
            pair.Key.SetValue(null, pair.Value);
        _singletons.Clear();
        Time.timeScale = _timeScale;
    }

    [Test]
    public void StartGlitch_AppliesImmediatelyBeforeAnyFrameElapses()
    {
        Invoke(_cutscene, "StartToiletGlitch", _toilet);
        Assert.That(_material.GetFloat("_Intensity"), Is.EqualTo(0.5f));
        Assert.That(GetField(_mapManager, "OnMapChangeStarted"), Is.Not.Null);
    }

    [Test]
    public void LeavingToilet_ClearsGlitchBeforeFirstFadeYieldAndBeforeMapChanges()
    {
        Invoke(_cutscene, "StartToiletGlitch", _toilet);
        var transition = (IEnumerator)Invoke(_mapManager, "ChangeMapRoutine",
            MapType("School_Hallway"), "test-entry", null);
        Assert.That(transition.MoveNext(), Is.True);
        Assert.That(GetField(_mapManager, "_currentArea"), Is.SameAs(_toilet));
        Assert.That(_material.GetFloat("_Intensity"), Is.Zero);
        Assert.That(GetField(_mapManager, "OnMapChangeStarted"), Is.Null);
        ((IDisposable)transition).Dispose();
    }

    [Test]
    public void ActorCleanupAndSameMapTransition_KeepGlitchUntilWipingEnds()
    {
        Invoke(_cutscene, "StartToiletGlitch", _toilet);
        Invoke(_cutscene, "HideKuneKune");
        Invoke(_cutscene, "OnMapChangeStarted", MapType("School_ManToilet"));
        Invoke(_cutscene, "LateUpdate");
        Assert.That(_material.GetFloat("_Intensity"), Is.EqualTo(0.5f));
    }

    [Test]
    public void SharedMaterialReset_DoesNotEraseActiveToiletGlitch()
    {
        Invoke(_cutscene, "StartToiletGlitch", _toilet);
        _material.SetFloat("_Intensity", 0f);
        Invoke(_cutscene, "LateUpdate");
        Assert.That(_material.GetFloat("_Intensity"), Is.EqualTo(0.5f));
    }

    [Test]
    public void ImmediateMapChange_AlsoClearsGlitch()
    {
        Invoke(_cutscene, "StartToiletGlitch", _toilet);
        SetField(_mapManager, "_currentArea", CreateComponent("MapArea"));
        Invoke(_cutscene, "LateUpdate");
        Assert.That(_material.GetFloat("_Intensity"), Is.Zero);
        Assert.That(GetField(_mapManager, "OnMapChangeStarted"), Is.Null);
    }

    [Test]
    public void DisabledCutscene_ClearsGlitchAndUnsubscribes()
    {
        Invoke(_cutscene, "StartToiletGlitch", _toilet);
        Invoke(_cutscene, "OnDisable");
        Assert.That(_material.GetFloat("_Intensity"), Is.Zero);
        Assert.That(GetField(_mapManager, "OnMapChangeStarted"), Is.Null);
    }

    [Test]
    public void EnteringToiletWithTrueFlagConditions_StartsGlitchDuringBlackoutBeforeReveal()
    {
        Component hallway = CreateComponent("MapArea");
        SetField(hallway, "mapType", MapType("School_Hallway"));
        SetField(_mapManager, "_currentArea", hallway);
        var transition = (IEnumerator)Invoke(_mapManager, "ChangeMapRoutine",
            MapType("School_ManToilet"), "test-entry", null);

        Assert.That(transition.MoveNext(), Is.True);
        Assert.That(GetField(_mapManager, "_currentArea"), Is.SameAs(hallway));
        Assert.That(_material.GetFloat("_Intensity"), Is.Zero);

        Assert.That(transition.MoveNext(), Is.True); // 진입 암전 완료, 화장실 페이드 인 대기
        Assert.That(GetField(_mapManager, "_currentArea"), Is.SameAs(_toilet));
        Assert.That(_material.GetFloat("_Intensity"), Is.EqualTo(0.5f));
        Invoke(_cutscene, "LateUpdate");
        Assert.That(_material.GetFloat("_Intensity"), Is.EqualTo(0.5f));
        ((IDisposable)transition).Dispose();
    }

    [TestCase("missing-hanky")]
    [TestCase("already-played")]
    [TestCase("failed")]
    [TestCase("unlocked")]
    [TestCase("dirty-desk")]
    [TestCase("loading")]
    public void EntryWithoutTrueFlagConditions_DoesNotStartGlitch(string reason)
    {
        if (reason == "missing-hanky")
            ((HashSet<string>)GetField(_inventory, "_keyItems")).Clear();
        else if (reason == "already-played")
            SetField(_cutscene, "hasPlayed", true);
        else if (reason == "failed")
            Invoke(_save, "SetFlag", "HasTrueFlagFailed", true);
        else if (reason == "unlocked")
            Invoke(_save, "SetFlag", "HasUnlockedTrueEnding", true);
        else if (reason == "dirty-desk")
            Invoke(_save, "SetFlag", "HasCompletedDirtyDeskCutscene", true);
        else
            SetField(_save, "<IsLoading>k__BackingField", true);

        SetField(_mapManager, "_currentArea", CreateComponent("MapArea"));
        var transition = (IEnumerator)Invoke(_mapManager, "ChangeMapRoutine",
            MapType("School_ManToilet"), "test-entry", null);
        Assert.That(transition.MoveNext(), Is.True);
        Assert.That(transition.MoveNext(), Is.True);
        Assert.That(_material.GetFloat("_Intensity"), Is.Zero);
        Assert.That(GetField(_mapManager, "OnMapChangeStarted"), Is.Null);
        ((IDisposable)transition).Dispose();
    }

    [Test]
    public void WipingEndingFade_ClearsGlitchAfterFadeBeforeScreenReturns()
    {
        Invoke(_cutscene, "StartToiletGlitch", _toilet);
        Component wiping = CreateComponent("ToiletTrueFlagUnlockedCutsceneController");
        SetField(wiping, "_sourceCutscene", _cutscene);
        var fade = (IEnumerator)Invoke(wiping, "FadeOutAndStopGlitch");

        Assert.That(fade.MoveNext(), Is.True);
        Assert.That(_material.GetFloat("_Intensity"), Is.EqualTo(0.5f));
        Assert.That(fade.MoveNext(), Is.False);
        Invoke(_cutscene, "LateUpdate");
        Assert.That(_material.GetFloat("_Intensity"), Is.Zero);
        Assert.That(GetField(_mapManager, "OnMapChangeStarted"), Is.Null);
    }

    [Test]
    public void WipingEndWithoutTimelineSignal_AlsoClearsGlitchDuringEndingBlackout()
    {
        Invoke(_cutscene, "StartToiletGlitch", _toilet);
        Component wiping = CreateComponent("ToiletTrueFlagUnlockedCutsceneController");
        SetField(wiping, "_sourceCutscene", _cutscene);

        Invoke(wiping, "OnCutsceneEndSetup");
        Invoke(_cutscene, "LateUpdate");
        Assert.That(_material.GetFloat("_Intensity"), Is.Zero);
        Assert.That(GetField(_mapManager, "OnMapChangeStarted"), Is.Null);
    }

    private Component CreateComponent(string name)
    {
        var obj = new GameObject(name);
        obj.SetActive(false);
        _objects.Add(obj);
        return obj.AddComponent(RuntimeType(name));
    }

    private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object MapType(string name) => Enum.Parse(RuntimeType("EMapType"), name);
    private static FieldInfo SingletonField(Type type) => type.GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
    private static object GetField(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
    private static void SetField(object target, string name, object value) => target.GetType().GetField(name, Flags).SetValue(target, value);
    private static object Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, Flags).Invoke(target, args);
}
