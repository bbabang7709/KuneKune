using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class PauseMapTransitionTests
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly List<GameObject> _objects = new();
    private readonly Dictionary<FieldInfo, object> _singletons = new();
    private Component _map;
    private Component _pause;
    private float _previousTimeScale;

    [SetUp]
    public void SetUp()
    {
        _previousTimeScale = Time.timeScale;
        foreach (string name in new[] { "MapManager", "PauseMenuUI", "ControlLockManager", "UIManager",
                     "SaveManager", "DialogSystem", "CutsceneManager", "SoundManager", "LightingEventManager" })
        {
            FieldInfo field = RuntimeType(name).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
            _singletons.Add(field, field.GetValue(null));
            field.SetValue(null, null);
        }

        _map = CreateComponent("MapManager");
        _pause = CreateComponent("PauseMenuUI");
        SetSingleton(_map);
        SetSingleton(_pause);
        SetSingleton(CreateComponent("ControlLockManager"));
        SetField(_pause, "pauseMenuPanel", CreateObject("Pause panel"));
        SetField(_pause, "loadWarningPanel", CreateObject("Load warning"));

        Component area = CreateComponent("MapArea");
        ((IList)GetField(_map, "allMapAreas")).Add(area);
        var entries = (IDictionary)GetField(area, "_entryPointDict");
        entries.Add("test-entry", CreateObject("Entry").transform);
        Time.timeScale = 1f;
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = _objects.Count - 1; i >= 0; i--)
            UnityEngine.Object.DestroyImmediate(_objects[i]);
        _objects.Clear();
        foreach (var singleton in _singletons)
            singleton.Key.SetValue(null, singleton.Value);
        _singletons.Clear();
        Time.timeScale = _previousTimeScale;
    }

    [Test]
    public void PauseDuringArrival_RemainsPausedAfterFadeCompletes()
    {
        bool completed = false;
        IEnumerator transition = StartTransition(() => completed = true);
        Assert.That(transition.MoveNext(), Is.True); // 도착 후 페이드 인 대기
        Invoke(_pause, "PauseGame");

        Assert.That(transition.MoveNext(), Is.False);
        Assert.That(completed, Is.True);
        Assert.That(GetProperty(_map, "IsChangingMap"), Is.False);
        Assert.That(GetField(_pause, "IsPaused"), Is.True);
        Assert.That(Time.timeScale, Is.Zero);

        Invoke(_pause, "ResumeGame");
        Assert.That(Time.timeScale, Is.EqualTo(1f));
    }

    [Test]
    public void ResumeDuringTransition_RemainsFrozenUntilFadeCompletes()
    {
        IEnumerator transition = StartTransition();
        Invoke(_pause, "PauseGame");
        Invoke(_pause, "ResumeGame");
        Assert.That(GetField(_pause, "IsPaused"), Is.False);
        Assert.That(Time.timeScale, Is.Zero);

        Assert.That(transition.MoveNext(), Is.True);
        Assert.That(transition.MoveNext(), Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
    }

    [Test]
    public void TransitionWithoutPause_ResumesAndReleasesItsControlLocks()
    {
        IEnumerator transition = StartTransition();
        Component locks = (Component)RuntimeType("ControlLockManager").GetProperty("Instance").GetValue(null);
        Assert.That(GetProperty(locks, "IsMovementLocked"), Is.True);
        Assert.That(transition.MoveNext(), Is.True);
        Assert.That(transition.MoveNext(), Is.False);
        Assert.That(GetProperty(locks, "IsMovementLocked"), Is.False);
        Assert.That(GetProperty(locks, "IsInputLocked"), Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
    }

    [TestCase("FixedUpdate", "time")]
    [TestCase("FixedUpdate", "menu")]
    [TestCase("FixedUpdate", "transition")]
    [TestCase("OnTriggerEnter2D", "time")]
    [TestCase("OnTriggerEnter2D", "menu")]
    [TestCase("OnTriggerEnter2D", "transition")]
    [TestCase("OnTriggerStay2D", "time")]
    [TestCase("OnTriggerStay2D", "menu")]
    [TestCase("OnTriggerStay2D", "transition")]
    public void PausedChase_DoesNotMoveOrStartDeathSequence(string callback, string pauseSource)
    {
        Component kune = CreateComponent("KuneKune");
        SetField(kune, "_state", Enum.Parse(RuntimeType("ECharacterState"), "Run"));
        var collider = CreateObject("Player").AddComponent<BoxCollider2D>();
        collider.gameObject.tag = "Player";
        SetField(kune, "_mapTransitionWaitRemaining", 0.5f);
        Vector3 position = kune.transform.position;

        if (pauseSource == "time")
            Time.timeScale = 0f;
        else if (pauseSource == "menu")
            SetField(_pause, "IsPaused", true);
        else
            StartTransition();

        if (callback == "FixedUpdate")
            Invoke(kune, callback);
        else
            Invoke(kune, callback, collider);

        Assert.That(kune.transform.position, Is.EqualTo(position));
        Assert.That(GetField(kune, "_mapTransitionWaitRemaining"), Is.EqualTo(0.5f));
        Assert.That(GetField(kune, "_isCatchingPlayer"), Is.False);
        Assert.That(GetField(kune, "_isInDeathSequence"), Is.False);
    }

    private IEnumerator StartTransition(Action onComplete = null)
    {
        object mapType = Enum.ToObject(RuntimeType("EMapType"), 0);
        var transition = (IEnumerator)Invoke(_map, "ChangeMapRoutine", mapType, "test-entry", onComplete);
        Assert.That(transition.MoveNext(), Is.True); // 페이드 아웃 대기
        Assert.That(GetProperty(_map, "IsChangingMap"), Is.True);
        Assert.That(Time.timeScale, Is.Zero);
        return transition;
    }

    private GameObject CreateObject(string name)
    {
        var obj = new GameObject(name);
        obj.SetActive(false); // 실제 씬 초기화와 리소스 생성을 피하고 콜백을 직접 검증합니다.
        _objects.Add(obj);
        return obj;
    }

    private Component CreateComponent(string type) => CreateObject(type).AddComponent(RuntimeType(type));
    private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static void SetSingleton(Component value) => value.GetType()
        .GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, value);
    private static object GetField(object target, string name) => target.GetType().GetField(name, InstanceFlags).GetValue(target);
    private static void SetField(object target, string name, object value) => target.GetType().GetField(name, InstanceFlags).SetValue(target, value);
    private static object GetProperty(object target, string name) => target.GetType().GetProperty(name, InstanceFlags).GetValue(target);
    private static object Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, InstanceFlags).Invoke(target, args);
}
