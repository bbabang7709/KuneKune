using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class DoorTransitionInterruptionTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly List<GameObject> _objects = new();
    private readonly Dictionary<FieldInfo, object> _singletons = new();
    private Component _door;
    private Component _locks;
    private float _timeScale;

    [SetUp]
    public void SetUp()
    {
        _timeScale = Time.timeScale;
        Time.timeScale = 1f;
        foreach (string type in new[] { "MapManager", "ControlLockManager", "SoundManager" })
        {
            FieldInfo field = RuntimeType(type).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
            _singletons.Add(field, field.GetValue(null));
            field.SetValue(null, null);
        }
        _door = CreateComponent("Door");
        _locks = CreateComponent("ControlLockManager");
        _locks.GetType().GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, _locks);
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = _objects.Count - 1; i >= 0; i--)
            UnityEngine.Object.DestroyImmediate(_objects[i]);
        _objects.Clear();
        foreach (var pair in _singletons)
            pair.Key.SetValue(null, pair.Value);
        _singletons.Clear();
        Time.timeScale = _timeScale;
    }

    [TestCase("chase")]
    [TestCase("event")]
    [TestCase("input")]
    public void LockDuringOpening_CancelsPendingMapTransition(string lockType)
    {
        var routine = (IEnumerator)Invoke(_door, "OpenDoorRoutine");
        Assert.That(routine.MoveNext(), Is.True);
        Assert.That(GetField(_door, "_isTransitioning"), Is.True);

        if (lockType == "chase") Invoke(_door, "SetHouseChaseLocked", true);
        else if (lockType == "event") Invoke(_door, "SetEventLocked", true);
        else Invoke(_locks, "AddInputLock", this);

        // 다음 단계가 맵 이동 대신 종료되어야 합니다. 대상 맵 미설정 경고도 없어야 합니다.
        Assert.That(routine.MoveNext(), Is.False);
        Assert.That(GetField(_door, "_isTransitioning"), Is.False);
        UnityEngine.TestTools.LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public void LockReleasedBeforeDoorDelayEnds_DoesNotReplayOldInteraction()
    {
        var routine = (IEnumerator)Invoke(_door, "OpenDoorRoutine");
        Assert.That(routine.MoveNext(), Is.True);
        Invoke(_locks, "AddInputLock", this);
        Invoke(_locks, "RemoveInputLock", this);

        Assert.That(routine.MoveNext(), Is.False);
        Assert.That(GetField(_door, "_isTransitioning"), Is.False);
        UnityEngine.TestTools.LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public void LateUnlockDialogCallback_DoesNotOpenDoorDuringCaughtSequence()
    {
        Invoke(_locks, "AddInputLock", this);
        Invoke(_door, "SetHouseChaseLocked", false);

        var routine = (IEnumerator)Invoke(_door, "OpenDoorRoutine");
        Assert.That(routine.MoveNext(), Is.False);
        Assert.That(GetField(_door, "_isTransitioning"), Is.False);
        Invoke(_door, "OnInteractPressed");
        UnityEngine.TestTools.LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public void DisposedOpening_ReleasesItsLockSubscription()
    {
        var routine = (IEnumerator)Invoke(_door, "OpenDoorRoutine");
        Assert.That(routine.MoveNext(), Is.True);
        ((IDisposable)routine).Dispose();

        Assert.That(GetField(_door, "_isTransitioning"), Is.False);
        Assert.That(GetField(_locks, "OnLockAdded"), Is.Null);
    }

    private Component CreateComponent(string type)
    {
        var obj = new GameObject(type);
        obj.SetActive(false);
        _objects.Add(obj);
        return obj.AddComponent(RuntimeType(type));
    }

    private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object GetField(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
    private static object Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, Flags).Invoke(target, args);
}
