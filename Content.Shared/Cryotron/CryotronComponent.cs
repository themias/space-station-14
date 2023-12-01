using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Server.Cryotron.Components;

[RegisterComponent]
public sealed partial class CryotronComponent : Component
{
    //turn into a list later
    public ContainerSlot BodyContainer = default!;

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
