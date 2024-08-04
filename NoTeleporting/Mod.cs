using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Buildings;
using Game.Modding;
using Game.SceneFlow;
using Game.Simulation;
using LeanBusinesses.Systems;
using NoTeleporting.Systems;

namespace NoTeleporting
{
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(NoTeleporting)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        private Setting m_Setting;

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));

            updateSystem.UpdateAt<PatchedResourceBuyerSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<PatchedCompanyInitializeSystem>(SystemUpdatePhase.Modification5);
            updateSystem.UpdateAt<PatchedResourcesInitializeSystem>(SystemUpdatePhase.Modification5);

            updateSystem.World.GetOrCreateSystemManaged<ResourceBuyerSystem>().Enabled = false;
            updateSystem.World.GetOrCreateSystemManaged<Game.Citizens.CompanyInitializeSystem>().Enabled = false;
            updateSystem.World.GetOrCreateSystemManaged<ResourcesInitializeSystem>().Enabled = false;

            void updateSettings(Game.Settings.Setting _setting)
            {
                PatchedCompanyInitializeSystem.StartingInputResourceAmount = m_Setting.ProcessorStartingResourceAmount;
                PatchedCompanyInitializeSystem.StartingOutputResourceAmount = m_Setting.ProcessorStartingOutputResourceAmount;
                PatchedCompanyInitializeSystem.StartingServiceResourceAmount = m_Setting.ServiceStartingResourceAmount;
                PatchedResourcesInitializeSystem.ServiceBuildingStartingResourcePercentage = 0.01f * m_Setting.ServiceBuildingStartingResourcePercentage;
            }

            updateSettings(null);

            m_Setting.onSettingsApplied += updateSettings;

            AssetDatabase.global.LoadSettings(nameof(NoTeleporting), m_Setting, new Setting(this));
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
            if (m_Setting != null)
            {
                m_Setting.UnregisterInOptionsUI();
                m_Setting = null;
            }
        }
    }
}
