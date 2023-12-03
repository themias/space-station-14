namespace Content.Server.Cryotron.Components;

[RegisterComponent]
public sealed partial class InCryoSleepComponent : Component
{
    /// <summary>
    /// Are they permanently in cryo sleep (ghosted, can't return)
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("permanentSleep")]
    public bool PermanentSleep = false;

    /// <summary>
    /// EndTime for when they can wake up
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("endTime")]
    public TimeSpan EndTime = TimeSpan.Zero;
}
