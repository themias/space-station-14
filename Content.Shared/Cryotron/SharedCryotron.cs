using Robust.Shared.Serialization;

namespace Content.Shared.Cryotron;

[Serializable, NetSerializable]
public enum CryotronVisuals : byte
{
    Down,
    Up,
    GoingDown,
    GoingUp,
}

[Serializable, NetSerializable]
public enum Visuals : byte
{
    VisualState,
}

[Serializable, NetSerializable]
public enum CryotronUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class CryotronPermanentSleepButtonPressedEvent : BoundUserInterfaceMessage
{

}

[Serializable, NetSerializable]
public sealed class CryotronTemporarySleepButtonPressedEvent : BoundUserInterfaceMessage
{

}

[Serializable, NetSerializable]
public sealed class CryotronWakeUpButtonPressedEvent : BoundUserInterfaceMessage
{

}

[Serializable, NetSerializable]
public sealed class CryotronUiState : BoundUserInterfaceState
{
    public bool InsideCryotron { get; }
    public TimeSpan? WakeUpEndTime { get; }
    public TimeSpan? ButtonEnableEndTime { get; }

    public CryotronUiState(bool insideCryotron,
        TimeSpan? wakeUpEndTime,
        TimeSpan? buttonEnableEndTime)
    {
        InsideCryotron = insideCryotron;
        WakeUpEndTime = wakeUpEndTime;
        ButtonEnableEndTime = buttonEnableEndTime;
    }
}
