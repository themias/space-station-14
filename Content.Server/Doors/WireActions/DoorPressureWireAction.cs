using Content.Server.Wires;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Wires;

namespace Content.Server.Doors.WireActions
{
    public sealed partial class DoorPressureWireAction : ComponentWireAction<FirelockComponent>
    {
        public override Color Color { get; set; } = Color.Orange;
        public override string Name { get; set; } = "wire-name-door-pressure";

        [DataField("timeout")]
        private int _timeout = 30;

        public override StatusLightState? GetLightState(Wire wire, FirelockComponent comp)
        {
            switch (comp.PressureEnabled)
            {
                case false:
                    return StatusLightState.Off;
                default:
                    return StatusLightState.On;
            }
        }

        public override object StatusKey { get; } = AirlockWireStatus.PressureIndicator;

        public override bool Cut(EntityUid user, Wire wire, FirelockComponent door)
        {
            door.PressureEnabled = false;
            return true;
        }

        public override bool Mend(EntityUid user, Wire wire, FirelockComponent door)
        {
            door.PressureEnabled = true;
            return true;
        }

        public override void Pulse(EntityUid user, Wire wire, FirelockComponent door)
        {
            door.PressureEnabled = false;
            WiresSystem.StartWireAction(wire.Owner, _timeout, PulseTimeoutKey.Key, new TimedWireEvent(AwaitPressureTimerFinish, wire));
        }

        public override void Update(Wire wire)
        {
            if (!IsPowered(wire.Owner))
            {
                WiresSystem.TryCancelWireAction(wire.Owner, PulseTimeoutKey.Key);
            }
        }

        private void AwaitPressureTimerFinish(Wire wire)
        {
            if (!wire.IsCut)
            {
                if (EntityManager.TryGetComponent<FirelockComponent>(wire.Owner, out var door))
                {
                    door.PressureEnabled = true;
                }
            }
        }

        private enum PulseTimeoutKey : byte
        {
            Key
        }
    }
}
