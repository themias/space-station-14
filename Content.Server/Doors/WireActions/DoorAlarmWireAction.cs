using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Wires;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Wires;

namespace Content.Server.Doors.WireActions
{
    public sealed partial class DoorAlarmWireAction : ComponentWireAction<FirelockComponent>
    {
        public override Color Color { get; set; } = Color.Orange;
        public override string Name { get; set; } = "wire-name-door-alarm";

        [DataField("timeout")]
        private int _timeout = 30;

        public override StatusLightState? GetLightState(Wire wire, FirelockComponent comp)
        {
            switch (comp.AlarmEnabled)
            {
                case false:
                    return StatusLightState.Off;
                default:
                    return StatusLightState.On;
            }
        }

        public override object StatusKey { get; } = AirlockWireStatus.AlarmIndicator;

        public override bool Cut(EntityUid user, Wire wire, FirelockComponent door)
        {
            door.AlarmEnabled = false;
            return true;
        }

        public override bool Mend(EntityUid user, Wire wire, FirelockComponent door)
        {
            door.AlarmEnabled = true;
            return true;
        }

        public override void Pulse(EntityUid user, Wire wire, FirelockComponent door)
        {
            //send alarm message to itself (to whole network in the future?)
            EntityManager.System<AtmosAlarmableSystem>().ForceAlert(wire.Owner, Shared.Atmos.Monitor.AtmosAlarmType.Danger);
            WiresSystem.StartWireAction(wire.Owner, _timeout, PulseTimeoutKey.Key, new TimedWireEvent(AwaitAlarmTimerFinish, wire));
        }

        public override void Update(Wire wire)
        {
            if (!IsPowered(wire.Owner))
            {
                WiresSystem.TryCancelWireAction(wire.Owner, PulseTimeoutKey.Key);
            }
        }

        private void AwaitAlarmTimerFinish(Wire wire)
        {
            if (!wire.IsCut)
            {
                if (EntityManager.TryGetComponent<FirelockComponent>(wire.Owner, out var door))
                {
                    EntityManager.System<AtmosAlarmableSystem>().Reset(wire.Owner);
                }
            }
        }

        private enum PulseTimeoutKey : byte
        {
            Key
        }
    }
}
