using Content.Shared.Cuffs.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Cuffs;

public sealed class HandcuffVisualsSystem : VisualizerSystem<HandcuffComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HandcuffComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    private void OnAfterAutoHandleState(Entity<HandcuffComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateAppearance(ent);
    }

    protected override void OnAppearanceChange(EntityUid uid, HandcuffComponent component, ref AppearanceChangeEvent args)
    {
        UpdateAppearance((uid, component), args.Sprite);
    }

    private void UpdateAppearance(Entity<HandcuffComponent> ent, SpriteComponent? sprite = null)
    {
        if (!Resolve(ent.Owner, ref sprite))
            return;

        sprite.LayerSetVisible(HandcuffVisualLayers.Unbroken, !ent.Comp.Broken);
        sprite.LayerSetVisible(HandcuffVisualLayers.Broken, ent.Comp.Broken);
    }
}

public enum HandcuffVisualLayers : byte
{
    Unbroken,
    Broken
}
