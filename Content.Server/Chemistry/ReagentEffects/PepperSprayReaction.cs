using Content.Server.PepperSpray;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.ReagentEffects
{
    [UsedImplicitly]
    public sealed class PepperSprayReaction : ReagentEffect
    {
        /// <summary>
        /// Duration to blind the target
        /// </summary>
        [DataField("blindDuration")]
        public float BlindDuration = 5.0f;

        /// <summary>
        /// Duration to stun the target
        /// </summary>
        [DataField("stunDuration")]
        public float StunDuration = 3.0f;

        /// <summary>
        /// Duration to slow down the target
        /// </summary>
        [DataField("slowdownDuration")]
        public float SlowdownDuration = 5.0f;

        /// <summary>
        /// Amount to slow down the target
        /// </summary>
        [DataField("slowdownMagnitude")]
        public float SlowdownMagnitude = 0.5f;

        public override void Effect(ReagentEffectArgs args)
        {
            args.EntityManager.System<PepperSpraySystem>().TryPepperSpray(args.SolutionEntity, BlindDuration, StunDuration, SlowdownDuration, SlowdownMagnitude);
        }
    }
}
