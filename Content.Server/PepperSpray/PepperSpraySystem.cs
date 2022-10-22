using Content.Server.Jittering;
using Content.Server.Speech;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Eye.Blinding;
using Content.Shared.Inventory;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;

namespace Content.Server.PepperSpray
{
    public sealed class PepperSpraySystem : EntitySystem
    {
        [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
        [Dependency] private readonly SharedStunSystem _stunSystem = default!;
        [Dependency] private readonly VocalSystem _vocalSystem = default!;
        [Dependency] private readonly JitteringSystem _jitteringSystem = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;

        public void TryPepperSpray(EntityUid target, float blindDuration, float stunDuration, float slowdownDuration, float slowdownRatio)
        {
            //check for protection
            //equipment can offer partial protection, like goggles for eyes and oxygen mask for mouth
            bool eyesProtected = false,
                mouthProtected = false;

            foreach (var slot in new string[] { "head", "eyes", "mask" })
            {
                if (_inventorySystem.TryGetSlotEntity(target, slot, out var item)
                    && TryComp<PepperProtectionComponent>(item, out var comp))
                {
                    eyesProtected = comp.ProtectsEyes | comp.Enabled;
                    mouthProtected = comp.ProtectsMouth | comp.Enabled;
                }
            }

            //blind if eyes aren't protected
            if (!eyesProtected)
            {
                _statusEffectsSystem.TryAddStatusEffect(target, SharedBlindingSystem.BlindingStatusEffect, TimeSpan.FromSeconds(blindDuration), false, "TemporaryBlindness");
            }

            //stun and scream if either not protected, because pain
            if (!eyesProtected || !mouthProtected)
            {
                _stunSystem.TryStun(target, TimeSpan.FromSeconds(stunDuration), true);
                _stunSystem.TryKnockdown(target, TimeSpan.FromSeconds(stunDuration), true);

                _stunSystem.TrySlowdown(target, TimeSpan.FromSeconds(slowdownDuration), true, slowdownRatio);

                _jitteringSystem.DoJitter(target, TimeSpan.FromSeconds(stunDuration), true);
                _vocalSystem.TryScream(target);
            }
        }
    }
}
