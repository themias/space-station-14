using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Server.Cryotron.Components;

[RegisterComponent]
public sealed partial class CryotronComponent : Component
{
    //turn into a list later
    public ContainerSlot BodyContainer = default!;

    /// <summary>
    /// How much time to wait before they can exit temporary sleep
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("minimumSleepTime")]
    public TimeSpan MinimumSleepTime = TimeSpan.FromSeconds(300);

    /// <summary>
    /// How long it takes the animation to open the cryotron
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("openDelay")]
    public TimeSpan OpenDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How long it takes the animation to close the cryotron
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("closeDelay")]
    public TimeSpan CloseDelay = TimeSpan.FromSeconds(1);


    /// <summary>
    /// Delay until the buttons become clickable. (except cancel)
    /// Makes sure people read it, as well as make it less viable for escape during chase
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("enterDelay")]
    public TimeSpan EnterDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Sound that plays when the cryotron opens
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("openSound")]
    public SoundSpecifier? OpenSound = new SoundPathSpecifier("/Audio/Machines/windoor_open.ogg");

    /// <summary>
    /// Sound that plays when the cryotron closes
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("closeSound")]
    public SoundSpecifier? CloseSound = new SoundPathSpecifier("/Audio/Machines/windoor_open.ogg");
}
