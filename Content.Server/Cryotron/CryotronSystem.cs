using Content.Server.Cryotron.Components;
using Content.Shared.Body.Components;
using Content.Shared.Climbing.Systems;
using Content.Shared.Cryotron;
using Content.Shared.Destructible;
using Content.Shared.DragDrop;
using Robust.Shared.Containers;

namespace Content.Server.Cryotron;

public sealed class CryotronSystem : SharedCryotronSystem
{
    [Dependency] public readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly ClimbSystem _climbSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryotronComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CryotronComponent, DragDropTargetEvent>(OnDragDropOn);
        SubscribeLocalEvent<CryotronComponent, DestructionEventArgs>(OnDestroyed);
    }

    private void OnComponentInit(EntityUid uid, CryotronComponent scannerComponent, ComponentInit args)
    {
        base.Initialize();
        scannerComponent.BodyContainer = _containerSystem.EnsureContainer<ContainerSlot>(uid, $"cryotron-bodyContainer");
    }

    public void InsertBody(EntityUid uid, EntityUid to_insert, CryotronComponent? cryoComponent)
    {
        if (!Resolve(uid, ref cryoComponent))
            return;

        if (cryoComponent.BodyContainer.ContainedEntity != null)
            return;

        if (!HasComp<BodyComponent>(to_insert))
            return;

        _containerSystem.Insert(to_insert, cryoComponent.BodyContainer);
    }

    public void EjectBody(EntityUid uid, CryotronComponent? cryoComponent)
    {
        if (!Resolve(uid, ref cryoComponent))
            return;

        if (cryoComponent.BodyContainer.ContainedEntity is not { Valid: true } contained)
            return;

        _containerSystem.Remove(contained, cryoComponent.BodyContainer);

        _climbSystem.ForciblySetClimbing(contained, uid);
    }

    public void OnDragDropOn(EntityUid uid, CryotronComponent component, DragDropTargetEvent args)
    {
        InsertBody(uid, args.Dragged, component);
    }

    private void OnDestroyed(EntityUid uid, CryotronComponent component, DestructionEventArgs args)
    {
        EjectBody(uid, component);
    }
}
