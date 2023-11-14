using Robust.Shared.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.Power.Components
{
    [RegisterComponent]
    public sealed partial class BatteryHandCrankComponent : Component
    {
        /// <summary>
        /// How much to recharge after each crank
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("chargePerCrank")]
        public float ChargePerCrank { get; set; }

        /// <summary>
        /// Optional sound to play after each crank
        /// </summary>
        [DataField("sound")]
        public SoundSpecifier? Sound { get; set; }
    }
}
