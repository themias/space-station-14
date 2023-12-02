namespace Content.Server.Cryotron.Components;

[RegisterComponent]
public sealed partial class InCryotronComponent : Component
{
    /// <summary>
    /// Are they permanently in cryo sleep (ghosted, can't return)
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("permanentSleep")]
    public bool PermanentSleep = false;

    /// <summary>
    /// How much time to wait before they can exit temporary sleep
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("sleepTime")]
    public TimeSpan SleepTime = TimeSpan.FromSeconds(300);

    /// <summary>
    /// EndTime for when they can wake up
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("endTime")]
    public TimeSpan EndTime = TimeSpan.Zero;
}
