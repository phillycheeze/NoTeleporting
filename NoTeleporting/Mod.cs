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

            try
            {
                log.Debug($"Injecting patched systems into updateSystem...");
                updateSystem.UpdateAt<PatchedResourceBuyerSystem>(SystemUpdatePhase.GameSimulation);
                updateSystem.UpdateAt<PatchedCompanyInitializeSystem>(SystemUpdatePhase.Modification5);
                updateSystem.UpdateAt<PatchedResourcesInitializeSystem>(SystemUpdatePhase.Modification5);
            }
            catch (System.Exception e)
            {
                log.Error($"Failed to inject patched systems into updateSystem");
                log.Error(e.Message);
                return;
            }

            try
            {
                log.Debug($"Disabling original default systems via updateSystem...");
                updateSystem.World.GetOrCreateSystemManaged<ResourceBuyerSystem>().Enabled = false;
                updateSystem.World.GetOrCreateSystemManaged<Game.Citizens.CompanyInitializeSystem>().Enabled = false;
                updateSystem.World.GetOrCreateSystemManaged<ResourcesInitializeSystem>().Enabled = false;
            }
            catch (System.Exception e)
            {
                log.Error($"Failed to disable default systems");
                log.Error(e.Message);
                throw e;
            }
            

            void updateSettings(Game.Settings.Setting _setting)
            {
                log.Debug($"Attempting to apply settings values to patched systems...");
                PatchedCompanyInitializeSystem.StartingInputResourceAmount = m_Setting.ProcessorStartingResourceAmount;
                PatchedCompanyInitializeSystem.StartingOutputResourceAmount = m_Setting.ProcessorStartingOutputResourceAmount;
                PatchedCompanyInitializeSystem.StartingServiceResourceAmount = m_Setting.ServiceStartingResourceAmount;
                PatchedResourcesInitializeSystem.ServiceBuildingStartingResourcePercentage = 0.01f * m_Setting.ServiceBuildingStartingResourcePercentage;
                log.Debug($"Finished applying settings");
            }

            m_Setting.onSettingsApplied += updateSettings;

            AssetDatabase.global.LoadSettings(nameof(NoTeleporting), m_Setting, new Setting(this));

            updateSettings(null);
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
