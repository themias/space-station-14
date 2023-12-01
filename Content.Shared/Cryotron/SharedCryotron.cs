using Robust.Shared.Serialization;

namespace Content.Shared.Cryotron;

[Serializable, NetSerializable]
public enum CryotronVisuals : byte
{
    Down,
    Up,
    GoingDown,
    GoingUp,
}

[Serializable, NetSerializable]
public enum Visuals : byte
{
    VisualState,
}
