using Content.Server.Actions;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Bed.Sleep;
using Content.Server.Cryotron.Components;
using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body.Components;
using Content.Shared.Climbing.Systems;
using Content.Shared.Cryotron;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.DragDrop;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;
using System.Text;

namespace Content.Server.Cryotron;

public sealed class CryotronSystem : SharedCryotronSystem
{
    [Dependency] private readonly StorageSystem _storageSystem = default!;
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
    [Dependency] private readonly SleepingSystem _sleepingSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryotronComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CryotronComponent, DragDropTargetEvent>(OnDragDropOn);
        SubscribeLocalEvent<CryotronComponent, DestructionEventArgs>(OnDestroyed);
        SubscribeLocalEvent<CryotronComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CryotronComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerb);
        SubscribeLocalEvent<CryotronComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<CryotronComponent, BoundUIOpenedEvent>(OnCryotronBUIOpened);
        SubscribeLocalEvent<CryotronComponent, CryotronPermanentSleepButtonPressedEvent>(OnPermanentSleepButtonPressed);
        SubscribeLocalEvent<CryotronComponent, CryotronTemporarySleepButtonPressedEvent>(OnTemporarySleepButtonPressed);
        SubscribeLocalEvent<InCryoSleepComponent, WakeActionEvent>(OnWakeAction);
    }

    private void OnComponentInit(EntityUid uid, CryotronComponent component, ComponentInit args)
    {
        base.Initialize();
        component.BodyContainer = _containerSystem.EnsureContainer<Container>(uid, $"cryotron-bodyContainer");

        _appearanceSystem.SetData(uid, Visuals.VisualState, CryotronVisuals.Up);
    }

    private void OnCryotronBUIOpened(EntityUid uid, CryotronComponent component, BoundUIOpenedEvent args)
    {
        TimeSpan buttonEnableEndTime = _timing.CurTime + component.EnterDelay;
        UpdateUserInterface(uid, component, null, buttonEnableEndTime);
    }

    public bool CanCryotronInsert(EntityUid uid, EntityUid target, EntityUid user, CryotronComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (target == user)
            return false;

        if(!HasComp<BodyComponent>(target) || !HasComp<HumanoidAppearanceComponent>(target))
        {
            _popupSystem.PopupEntity(Loc.GetString("cryotron-insert-failure-not-humanoid", ("body", target)), user);
            return false;
        }

        return true;
    }

    private void InsertBody(EntityUid uid, EntityUid to_insert, CryotronComponent? cryoComponent, EntityUid? inserter, bool permanent = false)
    {
        if (!Resolve(uid, ref cryoComponent))
            return;

        if (cryoComponent.BodyContainer.ContainedEntities.Contains(to_insert))
            return;

        if (!HasComp<BodyComponent>(to_insert))
            return;

        if (inserter != null)
        {
            if (CompOrNull<MindComponent>(to_insert)?.Session != null)
            {
                _popupSystem.PopupEntity(Loc.GetString("cryotron-insert-failure-not-ssd", ("body", to_insert)), inserter.Value);
                return;
            }

            //insert with no timer
            var inCryotronComp = EnsureComp<InCryoSleepComponent>(to_insert);
            inCryotronComp.EndTime = _timing.CurTime;
            _containerSystem.Insert(to_insert, cryoComponent.BodyContainer);
            EnterCryoSleep(to_insert, inCryotronComp.EndTime);

            _adminLogger.Add(LogType.Action, $"{_entityManager.ToPrettyString(inserter):user} put {_entityManager.ToPrettyString(to_insert):target} into cryotron (id:{uid.Id})");
        }
        else
        {
            var inCryotronComp = EnsureComp<InCryoSleepComponent>(to_insert);
            inCryotronComp.PermanentSleep = permanent;
            inCryotronComp.EndTime = _timing.CurTime + (permanent ? TimeSpan.Zero : cryoComponent.MinimumSleepTime);

            _containerSystem.Insert(to_insert, cryoComponent.BodyContainer);
            EnterCryoSleep(to_insert, inCryotronComp.EndTime);

            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{_entityManager.ToPrettyString(to_insert):user} put themself into cryotron (id:{uid.Id})");
        }
    }

    private void EjectBody(EntityUid uid, CryotronComponent? cryoComponent, EntityUid body)
    {
        if (!Resolve(uid, ref cryoComponent))
            return;

        if (!cryoComponent.BodyContainer.Contains(body))
            return;

        //LeaveCryoSleep(contained);

        RemComp<InCryoSleepComponent>(body);

        _containerSystem.Remove(body, cryoComponent.BodyContainer);

        _climbSystem.ForciblySetClimbing(body, uid);

        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{_entityManager.ToPrettyString(body):user} was ejected from cryotron (id:{uid.Id})");
    }

    private void OnDragDropOn(EntityUid uid, CryotronComponent component, DragDropTargetEvent args)
    {
        if (CanCryotronInsert(uid, args.Dragged, args.User, component))
        {
            InsertBody(uid, args.Dragged, component, args.User);
        }

        args.Handled = true;
    }

    private void OnDestroyed(EntityUid uid, CryotronComponent component, DestructionEventArgs args)
    {
        foreach(EntityUid body in component.BodyContainer.ContainedEntities)
            EjectBody(uid, component, body);
    }

    private void OnExamined(EntityUid uid, CryotronComponent component, ExaminedEvent args)
    {
        //list out all the people in there
        StringBuilder list = new StringBuilder();
        list.AppendLine(Loc.GetString("cryotron-display"));
        foreach(EntityUid body in component.BodyContainer.ContainedEntities)
        {
            if(TryComp<MetaDataComponent>(body, out var metadata))
                list.AppendLine(metadata.EntityName);
        }

        if (component.BodyContainer.ContainedEntities.Count == 0)
            list.AppendLine("cryotron-display-empty");
        args.PushMarkup(list.ToString());
    }

    private void UpdateUserInterface(EntityUid uid, CryotronComponent cryotronComponent, InCryoSleepComponent? insideComponent, TimeSpan? buttonEnableEndTime, bool isPowered = true)
    {
        CryotronUiState state;

        if(insideComponent != null)
        {
            state = new CryotronUiState(true, isPowered, cryotronComponent.MinimumSleepTime, insideComponent.EndTime, buttonEnableEndTime);
        }
        else
        {
            state = new CryotronUiState(false, isPowered, cryotronComponent.MinimumSleepTime, null, buttonEnableEndTime);
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

        if(TryComp<InCryoSleepComponent>(args.Session.AttachedEntity.Value, out var inCryotronComp))
            UpdateUserInterface(uid, component, inCryotronComp, null);
    }

    private void OnWakeAction(EntityUid uid, InCryoSleepComponent component, WakeActionEvent args)
    {
        if (_containerSystem.TryGetContainingContainer(uid, out var container)
            && TryComp<CryotronComponent>(container.Owner, out var cryotronComp))
            EjectBody(container.Owner, cryotronComp, uid);
    }

    private void OnAltVerb(EntityUid uid, CryotronComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        /*if (_adminManager.IsAdmin(actor.PlayerSession))
        {
            AlternativeVerb verb = new()
            {
                Act = () => EjectBody(uid, component),
                Text = Loc.GetString("cryotron-admin-eject-all"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/open.svg.192dpi.png")),
            };
        }*/
    }

    private void OnPowerChanged(EntityUid uid, CryotronComponent component, PowerChangedEvent args)
    {
        /*if (component.BodyContainer.ContainedEntity.HasValue)
        {
            //_metadataSystem.SetEntityPaused(component.BodyContainer.ContainedEntity.Value, args.Powered);
            if(TryComp<InCryoSleepComponent>(component.BodyContainer.ContainedEntity.Value, out var inCryotronComp))
                UpdateUserInterface(uid, component, inCryotronComp, null, args.Powered);
        }*/
    }

    private void EnterCryoSleep(EntityUid sleeper, TimeSpan endTime)
    {
        _sleepingSystem.TrySleeping(sleeper);
        if (TryComp<SleepingComponent>(sleeper, out var sleepComp))
        {
            //sleepComp.CoolDownEnd = endTime;
            _actionsSystem.SetCooldown(sleepComp.WakeAction, endTime - _timing.CurTime);
        }
        //_metadataSystem.SetEntityPaused(sleeper, true);
    }

    private void LeaveCryoSleep(EntityUid sleeper)
    {
        //_metadataSystem.SetEntityPaused(sleeper, false);
        _sleepingSystem.TryWaking(sleeper);
    }
}
