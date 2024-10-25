using Colossal.Logging;
using Game;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using System;
using Unity.Entities;
using System.Runtime.CompilerServices;

namespace LeanBusinesses.Systems
{
    internal partial class IndustrialProcessingEffeciencySystem : GameSystemBase
    {
        public static float IndustrialEfficiencyModifierPercentage = 1f;

        // The system default is 2f as of patch 1.11.1f1
        private static float IndustrialEfficiencyFactorVanilla = 2f;

        private EntityQuery __economyQuery;
        private ILog _log;

        protected override void OnCreate()
        {
            base.OnCreate();
            _log = NoTeleporting.Mod.log;

            RequireForUpdate<EconomyParameterData>();
        }

        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            __AssignQueries(ref base.CheckedStateRef);
        }

        protected override void OnUpdate()
        {
            AdjustEconomyParameterData();

            // Prevents accidental looping if system update phase is changed.
            Enabled = false;
        }

        public void AdjustEconomyParameterData()
        {
            try
            {
                _log.Info($"{nameof(IndustrialProcessingEffeciencySystem)}.{nameof(AdjustEconomyParameterData)} Running");
                int entityCount = __economyQuery.CalculateEntityCount();
                if (entityCount != 1)
                {
                    _log.Error($"{nameof(IndustrialProcessingEffeciencySystem)}.{nameof(AdjustEconomyParameterData)} Entity query returned {entityCount} entities; can't continue since we require exactly 1 singleton entity");
                    return;
                }
                EconomyParameterData componentData = __economyQuery.GetSingleton<EconomyParameterData>();
                Entity economy = __economyQuery.GetSingletonEntity();

                float newIndustrialEfficiencyValue = IndustrialEfficiencyModifierPercentage * IndustrialEfficiencyFactorVanilla;
                if (componentData.m_IndustrialEfficiency != newIndustrialEfficiencyValue)
                {
                    _log.Info($"{nameof(IndustrialProcessingEffeciencySystem)}.{nameof(AdjustEconomyParameterData)} Attempting to set m_IndustrialEfficiency parameter to {newIndustrialEfficiencyValue} (originally {componentData.m_IndustrialEfficiency})");
                    componentData.m_IndustrialEfficiency = newIndustrialEfficiencyValue;
                    EntityManager.SetComponentData(economy, componentData);
                }
                else
                {
                    _log.Info($"{nameof(IndustrialProcessingEffeciencySystem)}.{nameof(AdjustEconomyParameterData)} Skipping; m_IndustrialEfficiency parameter is already set to the correct value");
                }

                _log.Info($"{nameof(IndustrialProcessingEffeciencySystem)}.{nameof(AdjustEconomyParameterData)} Finished");

            }
            catch (Exception e)
            {
                _log.Error($"Failed to set Industrial Effeciency adjustment in EconomyParameterData");
                _log.Error(e.Message);
                _log.Error(e.StackTrace);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
            __economyQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[1] { ComponentType.ReadWrite<EconomyParameterData>() },
                Any = new ComponentType[0],
                None = new ComponentType[2] { ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Temp>() },
                Disabled = new ComponentType[0],
                Absent = new ComponentType[0],
                Options = EntityQueryOptions.IncludeSystems
            });
        }
    }
}
