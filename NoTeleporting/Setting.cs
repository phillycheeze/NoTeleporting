using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using Game.UI.Widgets;

namespace NoTeleporting
{
    [FileLocation(nameof(NoTeleporting))]
    [SettingsUIGroupOrder(kProcessorSettings)]
    [SettingsUIShowGroupName()]
    public class Setting : ModSetting
    {
        public const string kSection = "Main";

        public const string kProcessorSettings = "Processing companies";

        public Setting(IMod mod) : base(mod)
        {

        }

        [SettingsUISlider(min = 0, max = 50000, step = 500, scalarMultiplier = 1, unit = Unit.kWeight)]
        [SettingsUISection(kSection, kProcessorSettings)]
        public int ProcessorStartingResourceAmount { get; set; }

        public override void SetDefaults()
        {
            ProcessorStartingResourceAmount = 0;
        }
    }

    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "No Teleporting" },
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Main" },

                { m_Setting.GetOptionGroupLocaleID(Setting.kProcessorSettings), "Processing companies" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ProcessorStartingResourceAmount)), "Starting resource amount" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ProcessorStartingResourceAmount)), "The vanilla value is 15 t. Set to 0 to require companies to import resources immediately after spawning." },
            };
        }

        public void Unload()
        {

        }
    }
}
