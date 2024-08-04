using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game;
using Game.Buildings;
using Game.Common;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine.Scripting;

namespace NoTeleporting.Systems
{
    public partial class PatchedResourcesInitializeSystem : GameSystemBase
    {
        public static float ServiceBuildingStartingResourcePercentage = 1f;

        private struct InitializeCityServiceJob : IJobChunk
        {
            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> m_PrefabType;

            [ReadOnly]
            public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

            [ReadOnly]
            public ComponentTypeHandle<Created> m_CreatedType;

            public BufferTypeHandle<Resources> m_ResourcesType;

            public ComponentTypeHandle<Game.Buildings.ResourceConsumer> m_ResourceConsumerType;

            [ReadOnly]
            public ComponentLookup<PrefabRef> m_Prefabs;

            [ReadOnly]
            public ComponentLookup<ResourceData> m_ResourceDatas;

            [ReadOnly]
            public BufferLookup<InitialResourceData> m_InitialResourceDatas;

            [ReadOnly]
            public BufferLookup<ServiceUpkeepData> m_ServiceUpkeepDatas;

            [ReadOnly]
            public ComponentLookup<Created> m_CreatedDatas;

            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<Game.Buildings.ResourceConsumer> m_ResourceConsumers;

            [ReadOnly]
            public ResourcePrefabs m_ResourcePrefabs;

            [ReadOnly]
            public float m_ResourcePercentage;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<int> nativeArray = new NativeArray<int>(EconomyUtils.ResourceCount, Allocator.Temp);
                NativeList<ServiceUpkeepData> nativeList = new NativeList<ServiceUpkeepData>(4, Allocator.Temp);
                NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabType);
                BufferAccessor<InstalledUpgrade> bufferAccessor = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
                bool flag = chunk.Has(ref m_CreatedType);
                BufferAccessor<Resources> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ResourcesType);
                NativeArray<Game.Buildings.ResourceConsumer> nativeArray3 = chunk.GetNativeArray(ref m_ResourceConsumerType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    PrefabRef prefabRef = nativeArray2[i];
                    DynamicBuffer<Resources> resources = bufferAccessor2[i];
                    if (flag)
                    {
                        ProcessAddition(prefabRef, resources);
                    }
                    if (bufferAccessor.Length != 0)
                    {
                        foreach (InstalledUpgrade item in bufferAccessor[i])
                        {
                            if (m_CreatedDatas.HasComponent(item.m_Upgrade) && m_Prefabs.TryGetComponent(item.m_Upgrade, out var componentData))
                            {
                                ProcessAddition(componentData, resources);
                            }
                        }
                    }
                    nativeArray.Fill(0);
                    nativeList.Clear();
                    if (m_ServiceUpkeepDatas.TryGetBuffer(prefabRef, out var bufferData))
                    {
                        AddStorageTargets(nativeArray, bufferData);
                        nativeList.AddRange(bufferData.AsNativeArray());
                    }
                    if (bufferAccessor.Length != 0)
                    {
                        foreach (InstalledUpgrade item2 in bufferAccessor[i])
                        {
                            if (!BuildingUtils.CheckOption(item2, BuildingOption.Inactive) && m_Prefabs.TryGetComponent(item2, out var componentData2) && m_ServiceUpkeepDatas.TryGetBuffer(componentData2, out var bufferData2))
                            {
                                AddStorageTargets(nativeArray, bufferData2);
                                if (!m_ResourceConsumers.HasComponent(item2))
                                {
                                    UpgradeUtils.CombineStats(nativeList, bufferData2);
                                }
                            }
                        }
                    }
                    if (nativeArray3.Length != 0)
                    {
                        nativeArray3.ElementAt(i).m_ResourceAvailability = CityServiceUpkeepSystem.GetResourceAvailability(nativeList, resources, nativeArray);
                    }
                    if (bufferAccessor.Length == 0)
                    {
                        continue;
                    }
                    foreach (InstalledUpgrade item3 in bufferAccessor[i])
                    {
                        if (!BuildingUtils.CheckOption(item3, BuildingOption.Inactive) && m_ResourceConsumers.TryGetComponent(item3, out var componentData3))
                        {
                            if (m_Prefabs.TryGetComponent(item3, out var componentData4) && m_ServiceUpkeepDatas.TryGetBuffer(componentData4, out var bufferData3))
                            {
                                NativeArray<ServiceUpkeepData> other = bufferData3.AsNativeArray();
                                nativeList.CopyFrom(in other);
                                componentData3.m_ResourceAvailability = CityServiceUpkeepSystem.GetResourceAvailability(nativeList, resources, nativeArray);
                            }
                            else
                            {
                                componentData3.m_ResourceAvailability = byte.MaxValue;
                            }
                            m_ResourceConsumers[item3] = componentData3;
                        }
                    }
                }
            }

            private void ProcessAddition(Entity prefab, DynamicBuffer<Resources> resources)
            {
                if (!m_InitialResourceDatas.TryGetBuffer(prefab, out var bufferData))
                {
                    return;
                }
                foreach (InitialResourceData item in bufferData)
                {
                    EconomyUtils.AddResources(item.m_Value.m_Resource, (int) (m_ResourcePercentage * item.m_Value.m_Amount), resources);
                }
            }

            private void AddStorageTargets(NativeArray<int> storageTargets, DynamicBuffer<ServiceUpkeepData> upkeeps)
            {
                foreach (ServiceUpkeepData item in upkeeps)
                {
                    if (EconomyUtils.IsMaterial(item.m_Upkeep.m_Resource, m_ResourcePrefabs, ref m_ResourceDatas))
                    {
                        storageTargets[EconomyUtils.GetResourceIndex(item.m_Upkeep.m_Resource)] += item.m_Upkeep.m_Amount;
                    }
                }
            }

            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
            }
        }

        private struct TypeHandle
        {
            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

            [ReadOnly]
            public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

            [ReadOnly]
            public ComponentTypeHandle<Created> __Game_Common_Created_RO_ComponentTypeHandle;

            public BufferTypeHandle<Resources> __Game_Economy_Resources_RW_BufferTypeHandle;

            public ComponentTypeHandle<Game.Buildings.ResourceConsumer> __Game_Buildings_ResourceConsumer_RW_ComponentTypeHandle;

            [ReadOnly]
            public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

            [ReadOnly]
            public BufferLookup<InitialResourceData> __Game_Prefabs_InitialResourceData_RO_BufferLookup;

            [ReadOnly]
            public BufferLookup<ServiceUpkeepData> __Game_Prefabs_ServiceUpkeepData_RO_BufferLookup;

            public ComponentLookup<Game.Buildings.ResourceConsumer> __Game_Buildings_ResourceConsumer_RW_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Created> __Game_Common_Created_RO_ComponentLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
                __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
                __Game_Common_Created_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Created>(isReadOnly: true);
                __Game_Economy_Resources_RW_BufferTypeHandle = state.GetBufferTypeHandle<Resources>();
                __Game_Buildings_ResourceConsumer_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.ResourceConsumer>();
                __Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
                __Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
                __Game_Prefabs_InitialResourceData_RO_BufferLookup = state.GetBufferLookup<InitialResourceData>(isReadOnly: true);
                __Game_Prefabs_ServiceUpkeepData_RO_BufferLookup = state.GetBufferLookup<ServiceUpkeepData>(isReadOnly: true);
                __Game_Buildings_ResourceConsumer_RW_ComponentLookup = state.GetComponentLookup<Game.Buildings.ResourceConsumer>();
                __Game_Common_Created_RO_ComponentLookup = state.GetComponentLookup<Created>(isReadOnly: true);
            }
        }

        private EntityQuery m_Additions;

        private ResourceSystem m_ResourceSystem;

        private TypeHandle __TypeHandle;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
            m_Additions = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[2]
                {
                ComponentType.ReadWrite<Resources>(),
                ComponentType.ReadWrite<PrefabRef>()
                },
                Any = new ComponentType[2]
                {
                ComponentType.ReadOnly<Created>(),
                ComponentType.ReadOnly<Updated>()
                },
                None = new ComponentType[2]
                {
                ComponentType.ReadOnly<Temp>(),
                ComponentType.ReadOnly<Game.Buildings.ServiceUpgrade>()
                }
            });
            RequireForUpdate(m_Additions);
        }

        [Preserve]
        protected override void OnUpdate()
        {
            __TypeHandle.__Game_Common_Created_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Buildings_ResourceConsumer_RW_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_ServiceUpkeepData_RO_BufferLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_InitialResourceData_RO_BufferLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Buildings_ResourceConsumer_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Economy_Resources_RW_BufferTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            InitializeCityServiceJob initializeCityServiceJob = default(InitializeCityServiceJob);
            initializeCityServiceJob.m_PrefabType = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
            initializeCityServiceJob.m_InstalledUpgradeType = __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;
            initializeCityServiceJob.m_CreatedType = __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle;
            initializeCityServiceJob.m_ResourcesType = __TypeHandle.__Game_Economy_Resources_RW_BufferTypeHandle;
            initializeCityServiceJob.m_ResourceConsumerType = __TypeHandle.__Game_Buildings_ResourceConsumer_RW_ComponentTypeHandle;
            initializeCityServiceJob.m_Prefabs = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
            initializeCityServiceJob.m_ResourceDatas = __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup;
            initializeCityServiceJob.m_InitialResourceDatas = __TypeHandle.__Game_Prefabs_InitialResourceData_RO_BufferLookup;
            initializeCityServiceJob.m_ServiceUpkeepDatas = __TypeHandle.__Game_Prefabs_ServiceUpkeepData_RO_BufferLookup;
            initializeCityServiceJob.m_ResourceConsumers = __TypeHandle.__Game_Buildings_ResourceConsumer_RW_ComponentLookup;
            initializeCityServiceJob.m_CreatedDatas = __TypeHandle.__Game_Common_Created_RO_ComponentLookup;
            initializeCityServiceJob.m_ResourcePrefabs = m_ResourceSystem.GetPrefabs();
            initializeCityServiceJob.m_ResourcePercentage = ServiceBuildingStartingResourcePercentage;
            InitializeCityServiceJob jobData = initializeCityServiceJob;
            base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_Additions, base.Dependency);
            m_ResourceSystem.AddPrefabsReader(base.Dependency);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
        }

        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            __AssignQueries(ref base.CheckedStateRef);
            __TypeHandle.__AssignHandles(ref base.CheckedStateRef);
        }

        [Preserve]
        public PatchedResourcesInitializeSystem()
        {
        }
    }

}
