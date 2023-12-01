using Content.Client.Disposal.Systems;
using Content.Server.Cryotron.Components;
using Content.Shared.Cryotron;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Audio.Systems;

namespace Content.Client.Cryotron;

public sealed class CryotronSystem : SharedCryotronSystem
{
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly AnimationPlayerSystem _animationSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        //SubscribeLocalEvent<CryotronComponent, ComponentInit>(OnComponentInit); is this needed?
        SubscribeLocalEvent<CryotronComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, CryotronComponent cryotron, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        UpdateState(uid, cryotron, args.Sprite, args.Component);
    }

    /// <summary>
    /// Update visuals and tick animation
    /// </summary>
    private void UpdateState(EntityUid uid, CryotronComponent cryotron, SpriteComponent sprite, AppearanceComponent appearance)
    {
        if (!_appearanceSystem.TryGetData<CryotronVisuals>(uid, Visuals.VisualState, out var state, appearance))
        {
            return;
        }

        if (state == CryotronVisuals.GoingUp)
        {
            if (!_animationSystem.HasRunningAnimation(uid, "cryotron_go_up")) //TODO: fix hard coded string
            {
                var animState = new RSI.StateId("cryotron_go_up");

                // Setup the flush animation to play
                var anim = new Animation
                {
                    Length = cryotron.OpenDelay,
                    AnimationTracks =
                    {
                        new AnimationTrackSpriteFlick
                        {
                            LayerKey = CryotronVisualLayers.GoUp,
                            KeyFrames =
                            {
                                // Play the flush animation
                                new AnimationTrackSpriteFlick.KeyFrame(animState, 0),
                                // Return to base state (though, depending on how the unit is
                                // configured we might get an appearance change event telling
                                // us to go to charging state)
                                new AnimationTrackSpriteFlick.KeyFrame("cryotron_up", (float) cryotron.OpenDelay.TotalSeconds)
                            }
                        },
                    }
                };

                if (cryotron.OpenSound != null)
                {
                    anim.AnimationTracks.Add(
                        new AnimationTrackPlaySound
                        {
                            KeyFrames =
                            {
                                new AnimationTrackPlaySound.KeyFrame(_audioSystem.GetSound(cryotron.OpenSound), 0)
                            }
                        });
                }

                _animationSystem.Play(uid, anim, "cryotron_go_up"); //TODO: remove hard coded
            }
        }
        else if (state == CryotronVisuals.GoingDown)
        {
            if (!_animationSystem.HasRunningAnimation(uid, "cryotron_down")) //TODO: fix hard coded string
            {
                var animState = new RSI.StateId("cryotron_go_down");

                // Setup the flush animation to play
                var anim = new Animation
                {
                    Length = cryotron.OpenDelay,
                    AnimationTracks =
                    {
                        new AnimationTrackSpriteFlick
                        {
                            LayerKey = CryotronVisualLayers.GoUp,
                            KeyFrames =
                            {
                                // Play the flush animation
                                new AnimationTrackSpriteFlick.KeyFrame(animState, 0),
                                // Return to base state (though, depending on how the unit is
                                // configured we might get an appearance change event telling
                                // us to go to charging state)
                                new AnimationTrackSpriteFlick.KeyFrame("cryotron_go_down", (float) cryotron.OpenDelay.TotalSeconds)
                            }
                        },
                    }
                };

                if (cryotron.OpenSound != null)
                {
                    anim.AnimationTracks.Add(
                        new AnimationTrackPlaySound
                        {
                            KeyFrames =
                            {
                                new AnimationTrackPlaySound.KeyFrame(_audioSystem.GetSound(cryotron.OpenSound), 0)
                            }
                        });
                }

                _animationSystem.Play(uid, anim, "cryotron_go_down"); //TODO: remove hard coded
            }
        }
    }
}
public enum CryotronVisualLayers : byte
{
    Down,
    Up,
    GoDown,
    GoUp,
}
