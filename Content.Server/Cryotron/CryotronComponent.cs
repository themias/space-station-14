using Robust.Shared.Containers;

namespace Content.Server.Cryotron.Components;

[RegisterComponent]
public sealed partial class CryotronComponent : Component
{
    //turn into a list later
    public ContainerSlot BodyContainer = default!;
}
