using System;
using UnityEngine;

public enum SafetyType
{
    Safe,
    DangerS,
    DangerM,
    DangerL,
    DangerXL
}

public enum StageType
{
    First,
    Second
}

public class FloorCubeStater : MonoBehaviour
{
    public SafetyType CurrentSafety { get; private set; }

    public StageType CurrentStage { get; private set; }

    public event Action<SafetyType, SafetyType> OnSafetyChanged;

    public event Action<StageType, StageType> OnStageChanged;

    public void ChangeSafetyType(SafetyType newState)
    {
        if (CurrentSafety == newState)
            return;

        SafetyType oldState = CurrentSafety;

        CurrentSafety = newState;

        OnSafetyChanged?.Invoke(oldState, newState);
    }

    public void ChangeStageType(StageType newState)
    {
        if (CurrentStage == newState)
            return;

        StageType oldState = CurrentStage;

        CurrentStage = newState;

        OnStageChanged?.Invoke(oldState, newState);
    }
}