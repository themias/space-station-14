namespace Content.Server.PepperSpray
{
    [RegisterComponent, Access(typeof(PepperSpraySystem))]
    public sealed class PepperProtectionComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("enabled")]
        public bool Enabled { get; set; } = true;

        [DataField("protectsEyes")]
        public bool ProtectsEyes { get; set; } = true;

        [DataField("protectsMouth")]
        public bool ProtectsMouth { get; set; } = true;
    }
}
