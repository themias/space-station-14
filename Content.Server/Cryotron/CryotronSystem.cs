using Content.Server.Cryotron.Components;
using Content.Server.Popups;
using Content.Shared.Body.Components;
using Content.Shared.Climbing.Systems;
using Content.Shared.Cryotron;
using Content.Shared.Destructible;
using Content.Shared.DragDrop;
using Content.Shared.Examine;
using Content.Shared.Mind;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using System.Text;

namespace Content.Server.Cryotron;

public sealed class CryotronSystem : SharedCryotronSystem
{
    [Dependency] public readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly ClimbSystem _climbSystem = default!;
    [Dependency] private readonly MetaDataSystem _metadataSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryotronComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CryotronComponent, CanDropTargetEvent>(OnCanDragDropOn);
        SubscribeLocalEvent<CryotronComponent, DragDropTargetEvent>(OnDragDropOn);
        SubscribeLocalEvent<CryotronComponent, DestructionEventArgs>(OnDestroyed);
        SubscribeLocalEvent<CryotronComponent, ExaminedEvent>(OnExamined);
    }

    private void OnComponentInit(EntityUid uid, CryotronComponent component, ComponentInit args)
    {
        base.Initialize();
        component.BodyContainer = _containerSystem.EnsureContainer<ContainerSlot>(uid, $"cryotron-bodyContainer");

        _appearanceSystem.SetData(uid, Visuals.VisualState, CryotronVisuals.Up);
    }

    public bool CanCryotronInsert(EntityUid uid, EntityUid target, CryotronComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        return HasComp<BodyComponent>(target);
    }

    private void InsertBody(EntityUid uid, EntityUid to_insert, CryotronComponent? cryoComponent, EntityUid? inserter)
    {
        if (!Resolve(uid, ref cryoComponent))
            return;

        if (cryoComponent.BodyContainer.ContainedEntity != null)
            return;

        if (!HasComp<BodyComponent>(to_insert))
            return;

        if (inserter != null && (CompOrNull<MindComponent>(to_insert)?.UserId != null))
        {
            //Tried to put a non-SSD person into the sleeper!
            _popupSystem.PopupEntity("You can't put them into the Cryotron!", inserter.Value); //TODO: localize
            return;
        }

        EnsureComp<InCryoSleepComponent>(to_insert);

        _containerSystem.Insert(to_insert, cryoComponent.BodyContainer);

        _metadataSystem.SetEntityPaused(to_insert, true); //begin cryo sleep
    }

    private void EjectBody(EntityUid uid, CryotronComponent? cryoComponent)
    {
        if (!Resolve(uid, ref cryoComponent))
            return;

        if (cryoComponent.BodyContainer.ContainedEntity is not { Valid: true } contained)
            return;

        _metadataSystem.SetEntityPaused(contained, false); //end cryo sleep

        EnsureComp<InCryoSleepComponent>(contained);

        _containerSystem.Remove(contained, cryoComponent.BodyContainer);

        _climbSystem.ForciblySetClimbing(contained, uid);
    }

    private void OnCanDragDropOn(EntityUid uid, CryotronComponent component, CanDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.CanDrop |= CanCryotronInsert(uid, args.Dragged, component);

        args.Handled = true;
    }

    private void OnDragDropOn(EntityUid uid, CryotronComponent component, DragDropTargetEvent args)
    {
        InsertBody(uid, args.Dragged, component, args.User);
    }

    private void OnDestroyed(EntityUid uid, CryotronComponent component, DestructionEventArgs args)
    {
        EjectBody(uid, component);
    }

    private void OnExamined(EntityUid uid, CryotronComponent component, ExaminedEvent args)
    {
        //list out all the people in there
        StringBuilder list = new StringBuilder();
        list.AppendLine("The display reads: "); //localize
        foreach(EntityUid body in component.BodyContainer.ContainedEntities)
        {
            if(TryComp<MetaDataComponent>(uid, out var metadata))
                list.AppendLine(metadata.EntityName);
        }
        args.PushMarkup(list.ToString()); //TODO: localize
    }
}
