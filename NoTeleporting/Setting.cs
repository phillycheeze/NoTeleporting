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
    [SettingsUIGroupOrder(kProcessorSettings, kServiceCompanies, kServiceBuildings)]
    [SettingsUIShowGroupName(kProcessorSettings, kServiceCompanies, kServiceBuildings)]
    public class Setting : ModSetting
    {
        public const string kSection = "Main";

        public const string kProcessorSettings = "Processing companies";
        public const string kServiceCompanies = "Service companies";
        public const string kServiceBuildings = "City services";

        public Setting(IMod mod) : base(mod)
        {

        }

        [SettingsUISlider(min = 0, max = 50000, step = 500, scalarMultiplier = 1, unit = Unit.kWeight)]
        [SettingsUISection(kSection, kProcessorSettings)]
        public int ProcessorStartingResourceAmount { get; set; }

        [SettingsUISlider(min = 0, max = 10000, step = 100, scalarMultiplier = 1, unit = Unit.kWeight)]
        [SettingsUISection(kSection, kProcessorSettings)]
        public int ProcessorStartingOutputResourceAmount { get; set; }

        [SettingsUISlider(min = 10, max = 500, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage )]
        [SettingsUISection(kSection, kProcessorSettings)]
        public int ProcessorEffeciencyFactorAmount { get; set; }

        [SettingsUISlider(min = 0, max = 10000, step = 100, scalarMultiplier = 1, unit = Unit.kWeight)]
        [SettingsUISection(kSection, kServiceCompanies)]
        public int ServiceStartingResourceAmount { get; set; }

        [SettingsUISlider(min = 0, max = 200, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(kSection, kServiceBuildings)]
        public int ServiceBuildingStartingResourcePercentage { get; set; }

        public override void SetDefaults()
        {
            ProcessorStartingResourceAmount = 0;
            ProcessorStartingOutputResourceAmount = 1000;
            ProcessorEffeciencyFactorAmount = 100;
            ServiceStartingResourceAmount = 0;
            ServiceBuildingStartingResourcePercentage = 100;
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

                { m_Setting.GetOptionGroupLocaleID(Setting.kProcessorSettings), "Industrial companies" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ProcessorStartingResourceAmount)), "Starting input resource amount" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ProcessorStartingResourceAmount)), "The vanilla value is 15 t. Set to 0 to require companies to import resources immediately after spawning." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ProcessorStartingOutputResourceAmount)), "Starting output resource amount" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ProcessorStartingOutputResourceAmount)), "The vanilla value is 1 t." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ProcessorEffeciencyFactorAmount)), "Industry efficiency amount" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ProcessorEffeciencyFactorAmount)), "This adjusts the industrial efficiency of your entire city. The vanilla value is 100%." +
                "\n" +
                "Increasing this value will likely:  1) Speed up production of input resources into output resources. 2) Increase industry profits. 3) Increase traffic to/from industry. 4) Lower demand for industry relative to other zoning types." +
                "\n" +
                "For example: setting this to 300% will cause all Industry (that processes resources) to convert input resources to output resources 3 times faster."
                },

                { m_Setting.GetOptionGroupLocaleID(Setting.kServiceCompanies), "Commercial companies" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ServiceStartingResourceAmount)), "Starting resource amount" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ServiceStartingResourceAmount)), "The vanilla value is 3 t. Set to 0 to require companies to import resources immediately after spawning." },

                { m_Setting.GetOptionGroupLocaleID(Setting.kServiceBuildings), "City services" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ServiceBuildingStartingResourcePercentage)), "Starting resource amount" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ServiceBuildingStartingResourcePercentage)), "How many percents of the default initial resource amount is given to each city service building." },
            };
        }

        public void Unload()
        {

        }
    }
}
