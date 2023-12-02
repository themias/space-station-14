using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Cryotron.Components;
using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Shared.Body.Components;
using Content.Shared.Climbing.Systems;
using Content.Shared.Cryotron;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.DragDrop;
using Content.Shared.Examine;
using Content.Shared.Mind;
using Content.Shared.Movement.Events;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
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
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryotronComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CryotronComponent, CanDropTargetEvent>(OnCanDragDropOn);
        SubscribeLocalEvent<CryotronComponent, DragDropTargetEvent>(OnDragDropOn);
        SubscribeLocalEvent<CryotronComponent, DestructionEventArgs>(OnDestroyed);
        SubscribeLocalEvent<CryotronComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CryotronComponent, ContainerRelayMovementEntityEvent>(OnRelayMovement);
        SubscribeLocalEvent<CryotronComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerb);

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
            _adminLogger.Add(LogType.Action, $"{_entityManager.ToPrettyString(inserter):user} put {_entityManager.ToPrettyString(to_insert):target} into cryotron (id:{uid.Id})");
        }
        else
        {
            var inCryotronComp = EnsureComp<InCryotronComponent>(to_insert);
            inCryotronComp.PermanentSleep = permanent;
            inCryotronComp.EndTime = _timing.CurTime + (permanent ? TimeSpan.Zero : inCryotronComp.SleepTime);

            _containerSystem.Insert(to_insert, cryoComponent.BodyContainer);
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{_entityManager.ToPrettyString(to_insert):user} put themself into cryotron (id:{uid.Id})");
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

        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{_entityManager.ToPrettyString(contained):user} was ejected from cryotron (id:{uid.Id})");
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
        CryotronUiState state;

        if(insideComponent != null)
        {
            var remainingTime = insideComponent.EndTime - _timing.CurTime;
            state = new CryotronUiState(true, remainingTime, buttonEnableEndTime);
        }
        else
        {
            state = new CryotronUiState(false, null, buttonEnableEndTime);
        }

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

    private void OnTemporarySleepButtonPressed(EntityUid uid, CryotronComponent component, CryotronTemporarySleepButtonPressedEvent args)
    {
        if (args.Session.AttachedEntity == null)
            return;

        InsertBody(uid, args.Session.AttachedEntity.Value, component, null);
    }

    private void OnWakeUpButtonPressed(EntityUid uid, CryotronComponent component, CryotronWakeUpButtonPressedEvent args)
    {
        EjectBody(uid, component);
    }

    private void OnRelayMovement(EntityUid uid, CryotronComponent component, ref ContainerRelayMovementEntityEvent args)
    {
        if (TryComp<InCryotronComponent>(args.Entity, out var inCryotronComp)
            && _timing.CurTime < inCryotronComp.EndTime)
        {
            _popupSystem.PopupEntity("Can't wake up!", args.Entity); //wake me up
            return;
        }

        EjectBody(uid, component);
    }

    private void OnAltVerb(EntityUid uid, CryotronComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        if (_adminManager.IsAdmin(actor.PlayerSession))
        {
            AlternativeVerb verb = new()
            {
                Act = () => EjectBody(uid, component),
                Text = Loc.GetString("cryotron-admin-eject-all"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/open.svg.192dpi.png")),
            };
        }
    }
}
