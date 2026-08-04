namespace SubnauticaSpeedrunningMod.Runtime.Ui
{
    internal sealed class ModUiCustomizationModule : IModModule
    {
        public string Name
        {
            get { return "Ranked UI"; }
        }

        public void Initialize(RuntimeContext context)
        {
            ModPrivateRaceRoomRuntimeHost.Install(context);
            ModOptionsPanelRuntime.Install();
            ModHardcoreQuitRuntime.Install();
            ModMainMenuRuntimeHost.Install(context);
        }
    }
}
