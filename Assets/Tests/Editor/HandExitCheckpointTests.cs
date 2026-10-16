using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class HandExitCheckpointTests
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private GameObject _handObject;
    private Component _hand;
    private object _previousCleared;

    [SetUp]
    public void SetUp()
    {
        _previousCleared = HandType.GetProperty("IsCleared").GetValue(null);
        _handObject = new GameObject("Checkpoint hand test");
        _handObject.SetActive(false);
        _hand = _handObject.AddComponent(HandType);
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(_handObject);
        HandType.GetField("<IsCleared>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic)
            .SetValue(null, _previousCleared);
    }

    [Test]
    public void LegacySave_UsesChapterStartWithoutMapOverride()
    {
        object data = JsonUtility.FromJson("{\"currentChapter\":3}", SaveType);
        Assert.That(string.IsNullOrEmpty((string)GetField(data, "respawnEntryPointID")), Is.True);
        Assert.That(GetField(data, "currentChapter").ToString(), Is.EqualTo("Chapter3_School"));
    }

    [Test]
    public void MapCheckpoint_RoundTripPreservesLocationChapterRewardsAndCompletion()
    {
        object data = NewSave();
        SetField(data, "currentChapter", Enum.Parse(RuntimeType("EChapter"), "Chapter3_School"));
        SetField(data, "respawnMapType", Enum.Parse(RuntimeType("EMapType"), "School_Hallway"));
        SetField(data, "respawnEntryPointID", "school_hallway_3");
        ((System.Collections.IList)GetField(data, "keyItemIDs")).Add("classroom_key");
        SaveType.GetMethod("SetFlag").Invoke(data, new object[] { ClearedFlag, true });

        object restored = JsonUtility.FromJson(JsonUtility.ToJson(data), SaveType);
        Assert.That(GetField(restored, "respawnMapType").ToString(), Is.EqualTo("School_Hallway"));
        Assert.That(GetField(restored, "respawnEntryPointID"), Is.EqualTo("school_hallway_3"));
        Assert.That(GetField(restored, "currentChapter").ToString(), Is.EqualTo("Chapter3_School"));
        Assert.That((System.Collections.IList)GetField(restored, "keyItemIDs"), Does.Contain("classroom_key"));
        HandType.GetMethod("LoadState").Invoke(_hand, new[] { restored });
        Assert.That(HandType.GetProperty("IsCleared").GetValue(null), Is.True);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void HandCompletion_IsRestoredFromSaveInsteadOfPreviousRun(bool cleared)
    {
        HandType.GetField("<IsCleared>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic)
            .SetValue(null, !cleared);
        object data = NewSave();
        SaveType.GetMethod("SetFlag").Invoke(data, new object[] { ClearedFlag, cleared });
        HandType.GetMethod("LoadState").Invoke(_hand, new[] { data });
        Assert.That(HandType.GetProperty("IsCleared").GetValue(null), Is.EqualTo(cleared));

        object snapshot = NewSave();
        HandType.GetMethod("SaveState").Invoke(_hand, new[] { snapshot });
        Assert.That(SaveType.GetMethod("GetFlag").Invoke(snapshot, new object[] { ClearedFlag, false }),
            Is.EqualTo(cleared));
    }

    [Test]
    public void ClearedHand_CannotRestartWhenPlayedAgain()
    {
        object data = NewSave();
        SaveType.GetMethod("SetFlag").Invoke(data, new object[] { ClearedFlag, true });
        HandType.GetMethod("LoadState").Invoke(_hand, new[] { data });
        // 미설정된 이벤트 리소스에 접근하기 전에 완료 상태에서 빠져나와야 합니다.
        Assert.DoesNotThrow(() => HandType.GetMethod("Play").Invoke(_hand, null));
    }

    [Test]
    public void CheckpointLighting_RoundTripRestoresColorAndIntensityAfterChapterDefaults()
    {
        Component lighting = CreateLighting(out Component light);
        Color savedColor = new Color32(19, 12, 12, 255);
        SetLight(light, savedColor, 0.65f);
        object data = NewSave();
        lighting.GetType().GetMethod("SaveCheckpointLighting").Invoke(lighting, new[] { data });
        object restored = JsonUtility.FromJson(JsonUtility.ToJson(data), SaveType);

        SetLight(light, Color.white, 1f); // 불러오기 중 챕터 배경이 기본 조명을 적용한 상황
        lighting.GetType().GetMethod("RestoreCheckpointLighting").Invoke(lighting, new[] { restored });

        Assert.That(GetField(restored, "hasCheckpointLighting"), Is.True);
        AssertLight(light, savedColor, 0.65f);
    }

    [TestCase("Chapter3_School", true, false, true)]
    [TestCase("Chapter3_School", false, false, false)]
    [TestCase("Chapter3_School", true, true, false)]
    [TestCase("Chapter4_Flashback", true, false, false)]
    public void LegacyCheckpoint_RestoresHandLightingOnlyBeforeDirtyDeskReset(
        string chapter, bool exitedToilet, bool completedDirtyDesk, bool restoreHandLighting)
    {
        Component lighting = CreateLighting(out Component light);
        SetLight(light, Color.white, 1f);
        object data = JsonUtility.FromJson("{}", SaveType);
        SetField(data, "currentChapter", Enum.Parse(RuntimeType("EChapter"), chapter));
        string exitFlag = (string)HandType.GetField("ExitCheckpointFlag").GetRawConstantValue();
        string deskFlag = (string)RuntimeType("DirtyDeskCutsceneController").GetField("CompletedFlag").GetRawConstantValue();
        SaveType.GetMethod("SetFlag").Invoke(data, new object[] { exitFlag, exitedToilet });
        SaveType.GetMethod("SetFlag").Invoke(data, new object[] { deskFlag, completedDirtyDesk });

        lighting.GetType().GetMethod("RestoreCheckpointLighting").Invoke(lighting, new[] { data });

        AssertLight(light, restoreHandLighting ? (Color)new Color32(19, 12, 12, 255) : Color.white, 1f);
    }

    [Test]
    public void ExplicitLightingSnapshot_TakesPriorityOverLegacyHandFlag()
    {
        Component lighting = CreateLighting(out Component light);
        object data = NewSave();
        SetField(data, "currentChapter", Enum.Parse(RuntimeType("EChapter"), "Chapter3_School"));
        SetField(data, "hasCheckpointLighting", true);
        SetField(data, "checkpointLightColor", Color.white);
        SetField(data, "checkpointLightIntensity", 0.8f);
        string exitFlag = (string)HandType.GetField("ExitCheckpointFlag").GetRawConstantValue();
        SaveType.GetMethod("SetFlag").Invoke(data, new object[] { exitFlag, true });

        lighting.GetType().GetMethod("RestoreCheckpointLighting").Invoke(lighting, new[] { data });

        AssertLight(light, Color.white, 0.8f);
    }

    private Component CreateLighting(out Component light)
    {
        Type lightType = Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime", true);
        light = _handObject.AddComponent(lightType);
        Component lighting = _handObject.AddComponent(RuntimeType("LightingEventManager"));
        SetField(lighting, "_globalLight", light);
        return lighting;
    }

    private static void SetLight(Component light, Color color, float intensity)
    {
        light.GetType().GetProperty("color").SetValue(light, color);
        light.GetType().GetProperty("intensity").SetValue(light, intensity);
    }

    private static void AssertLight(Component light, Color color, float intensity)
    {
        Assert.That(light.GetType().GetProperty("color").GetValue(light), Is.EqualTo(color));
        Assert.That((float)light.GetType().GetProperty("intensity").GetValue(light), Is.EqualTo(intensity).Within(0.0001f));
    }

    private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static Type HandType => RuntimeType("KuneKuneHand");
    private static Type SaveType => RuntimeType("SaveData");
    private static string ClearedFlag => (string)HandType.GetField("ClearedFlag").GetRawConstantValue();
    private static object NewSave() => Activator.CreateInstance(SaveType);
    private static object GetField(object target, string name) => target.GetType().GetField(name, InstanceFlags).GetValue(target);
    private static void SetField(object target, string name, object value) => target.GetType().GetField(name, InstanceFlags).SetValue(target, value);
}
