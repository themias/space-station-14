using Content.Server.Cryotron.Components;
using Content.Server.GameTicking;
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
using Robust.Shared.Timing;
using System.Text;

namespace Content.Server.Cryotron;

public sealed class CryotronSystem : SharedCryotronSystem
{
    [Dependency] public readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly ClimbSystem _climbSystem = default!;
    [Dependency] private readonly MetaDataSystem _metadataSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] protected readonly IGameTiming _timing = default!;
    [Dependency] protected readonly SharedMindSystem _mindSystem = default!;
    [Dependency] protected readonly GameTicker _gameTicker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryotronComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CryotronComponent, CanDropTargetEvent>(OnCanDragDropOn);
        SubscribeLocalEvent<CryotronComponent, DragDropTargetEvent>(OnDragDropOn);
        SubscribeLocalEvent<CryotronComponent, DestructionEventArgs>(OnDestroyed);
        SubscribeLocalEvent<CryotronComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<CryotronComponent, BoundUIOpenedEvent>(OnCryotronBUIOpened);
        SubscribeLocalEvent<CryotronComponent, CryotronPermanentSleepButtonPressedEvent>(OnPermanentSleepButtonPressed);
        SubscribeLocalEvent<CryotronComponent, CryotronTemporarySleepButtonPressedEvent>(OnTemporarySleepButtonPressed);
        SubscribeLocalEvent<CryotronComponent, CryotronWakeUpButtonPressedEvent>(OnWakeUpButtonPressed);
    }

    private void OnComponentInit(EntityUid uid, CryotronComponent component, ComponentInit args)
    {
        base.Initialize();
        component.BodyContainer = _containerSystem.EnsureContainer<ContainerSlot>(uid, $"cryotron-bodyContainer");

        _appearanceSystem.SetData(uid, Visuals.VisualState, CryotronVisuals.Up);
    }

    private void OnCryotronBUIOpened(EntityUid uid, CryotronComponent component, BoundUIOpenedEvent args)
    {
        if (TryComp<InCryotronComponent>(args.Entity, out var inCrytronComp))
        {
            UpdateUserInterface(uid, component, inCrytronComp, null);
        }
        else
        {
            TimeSpan buttonEnableEndTime = _timing.CurTime + component.EnterDelay;
            UpdateUserInterface(uid, component, null, buttonEnableEndTime);
        }
    }

    public bool CanCryotronInsert(EntityUid uid, EntityUid target, CryotronComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        return HasComp<BodyComponent>(target);
    }

    private void InsertBody(EntityUid uid, EntityUid to_insert, CryotronComponent? cryoComponent, EntityUid? inserter, bool permanent = false)
    {
        if (!Resolve(uid, ref cryoComponent))
            return;

        if (cryoComponent.BodyContainer.ContainedEntity != null)
            return;

        if (!HasComp<BodyComponent>(to_insert))
            return;

        if (inserter != null)
        {
            if (CompOrNull<MindComponent>(to_insert)?.UserId != null)
            {
                //Tried to put a non-SSD person into the sleeper!
                _popupSystem.PopupEntity("You can't put them into the Cryotron!", inserter.Value); //TODO: localize
                return;
            }

            //insert with no timer
            var inCryotronComp = EnsureComp<InCryotronComponent>(to_insert);
            inCryotronComp.EndTime = _timing.CurTime;
            _containerSystem.Insert(to_insert, cryoComponent.BodyContainer);
        }
        else
        {
            var inCryotronComp = EnsureComp<InCryotronComponent>(to_insert);
            inCryotronComp.PermanentSleep = permanent;
            inCryotronComp.EndTime = _timing.CurTime + (permanent ? TimeSpan.Zero : inCryotronComp.SleepTime);

            _containerSystem.Insert(to_insert, cryoComponent.BodyContainer);
        }

        //_metadataSystem.SetEntityPaused(to_insert, true); //begin cryo sleep
    }

    private void EjectBody(EntityUid uid, CryotronComponent? cryoComponent)
    {
        if (!Resolve(uid, ref cryoComponent))
            return;

        if (cryoComponent.BodyContainer.ContainedEntity is not { Valid: true } contained)
            return;

        //_metadataSystem.SetEntityPaused(contained, false); //end cryo sleep

        EnsureComp<InCryotronComponent>(contained);

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

    private void UpdateUserInterface(EntityUid uid, CryotronComponent cryotronComponent, InCryotronComponent? insideComponent, TimeSpan? buttonEnableEndTime)
    {
        TimeSpan remainingTime = TimeSpan.Zero;
        bool insideCryotron = false;

        if(insideComponent != null)
        {
            insideCryotron = true;
            remainingTime = insideComponent.EndTime - _timing.CurTime;
        }

        var state = new CryotronUiState(insideCryotron, remainingTime, buttonEnableEndTime);
        _userInterface.TrySetUiState(uid, CryotronUiKey.Key, state);
    }

    private void OnPermanentSleepButtonPressed(EntityUid uid, CryotronComponent cryotronComponent, CryotronPermanentSleepButtonPressedEvent args)
    {
        if (args.Session.AttachedEntity == null)
            return;

        InsertBody(uid, args.Session.AttachedEntity.Value, cryotronComponent, null);

        if (!_mindSystem.TryGetMind(args.Session, out var mindId, out var mind))
        {
            //give some error
            //return?
        }

        if (!_gameTicker.OnGhostAttempt(mindId, true, true, mind))
        {
            //give some error
        }
    }

    private void OnTemporarySleepButtonPressed(EntityUid uid, CryotronComponent cryotronComponent, CryotronTemporarySleepButtonPressedEvent args)
    {
        if (args.Session.AttachedEntity == null)
            return;

        InsertBody(uid, args.Session.AttachedEntity.Value, cryotronComponent, null);
    }

    private void OnWakeUpButtonPressed(EntityUid uid, CryotronComponent cryotronComponent, CryotronWakeUpButtonPressedEvent args)
    {
        EjectBody(uid, cryotronComponent);
    }
}
