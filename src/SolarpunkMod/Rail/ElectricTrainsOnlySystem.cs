using System.Collections.Generic;
using Colossal.Serialization.Entities;
using Game;
using Game.City;
using Game.Economy;
using Game.Net;
using Game.Prefabs;
using Game.Settings;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;

namespace SolarpunkMod.Rail
{
    /// <summary>
    /// "Electric trains only": train depots get Electricity as their only energy, which the game's
    /// vehicle selection checks before the model chosen on a line. Fuel-only carriages get no energy
    /// at all, as some (Tigon's garbage wagon) would otherwise block a whole use. Only applied
    /// when passengers, freight and garbage can each still draw an electric train for the city theme:
    /// a depot that cannot draw a train sends nothing, silently.
    /// Prefab data only, rebuilt on every launch: nothing reaches a save.
    /// </summary>
    public partial class ElectricTrainsOnlySystem : GameSystemBase
    {
        private PrefabSystem m_PrefabSystem;
        private ModSettings m_Settings;
        private CityConfigurationSystem m_CityConfigurationSystem;
        private TransportVehicleSelectData m_VehicleSelectData;
        private EntityQuery m_VehiclePrefabQuery;
        private EntityQuery m_DepotQuery;
        private EntityQuery m_CarriageQuery;
        private EntityQuery m_EngineQuery;
        private bool m_InGame;
        private bool m_AppliedSetting;
        // Whether prefab data currently carries our changes: untouched prefabs are left to other mods.
        private bool m_Changed;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_CityConfigurationSystem = World.GetOrCreateSystemManaged<CityConfigurationSystem>();
            m_VehicleSelectData = new TransportVehicleSelectData(this);
            m_VehiclePrefabQuery = GetEntityQuery(TransportVehicleSelectData.GetEntityQueryDesc());
            m_DepotQuery = GetEntityQuery(ComponentType.ReadWrite<TransportDepotData>(), ComponentType.ReadOnly<PrefabData>());
            m_CarriageQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadWrite<TrainData>(), ComponentType.ReadOnly<TrainCarriageData>(), ComponentType.ReadOnly<PrefabData>() },
                None = new[] { ComponentType.ReadOnly<MultipleUnitTrainData>() },
            });
            m_EngineQuery = GetEntityQuery(ComponentType.ReadOnly<TrainData>(), ComponentType.ReadOnly<TrainEngineData>(), ComponentType.ReadOnly<PrefabData>());
            // Kept: the mod is disposed (Mod.Settings = null) before the world's systems are destroyed.
            m_Settings = Mod.Settings;
            m_Settings.onSettingsApplied += OnSettingsApplied;
        }

        protected override void OnDestroy()
        {
            m_Settings.onSettingsApplied -= OnSettingsApplied;
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            m_InGame = mode.IsGame();
            if (m_InGame)
                Refresh();
        }

        // Fires for every option of the page, the Local Hub slider included: only this option matters here.
        private void OnSettingsApplied(Setting setting)
        {
            if (m_InGame && m_Settings.ElectricTrainsOnly != m_AppliedSetting)
                Refresh();
        }

        private void Refresh()
        {
            m_AppliedSetting = m_Settings.ElectricTrainsOnly;
            Restore();
            LogEngineCensus();
            if (!m_Settings.ElectricTrainsOnly)
                return;

            m_Changed = true;
            var carriageCount = ClearCarriageEnergy();
            var missingUse = FindUseWithoutElectricTrain();
            if (missingUse != null)
            {
                Restore();
                Mod.Log.Warn($"Electric trains only: not applied, no electric train for {missingUse}");
                return;
            }

            var depotCount = 0;
            using var depots = m_DepotQuery.ToEntityArray(Allocator.Temp);
            foreach (var depot in depots)
            {
                var data = EntityManager.GetComponentData<TransportDepotData>(depot);
                if (data.m_TransportType != TransportType.Train)
                    continue;
                data.m_EnergyTypes = EnergyTypes.Electricity;
                EntityManager.SetComponentData(depot, data);
                depotCount++;
            }
            Mod.Log.Info($"Electric trains only: applied to {depotCount} train depots and {carriageCount} carriages");
        }

        /// <returns>How many carriages were changed.</returns>
        private int ClearCarriageEnergy()
        {
            var count = 0;
            using var carriages = m_CarriageQuery.ToEntityArray(Allocator.Temp);
            foreach (var carriage in carriages)
            {
                var data = EntityManager.GetComponentData<TrainData>(carriage);
                // Only carriages an electric depot would turn down.
                if (data.m_TrackType != TrackTypes.Train || data.m_EnergyType == EnergyTypes.None
                    || (data.m_EnergyType & EnergyTypes.Electricity) != 0)
                    continue;
                data.m_EnergyType = EnergyTypes.None;
                EntityManager.SetComponentData(carriage, data);
                count++;
            }
            return count;
        }

        /// <summary>
        /// Puts back the energy each prefab declares, read from the prefab itself rather than from a
        /// copy kept earlier: a prefab regenerated by another mod, or written after us, stays right.
        /// </summary>
        private void Restore()
        {
            if (!m_Changed)
                return;
            m_Changed = false;

            using var depots = m_DepotQuery.ToEntityArray(Allocator.Temp);
            foreach (var depot in depots)
            {
                var data = EntityManager.GetComponentData<TransportDepotData>(depot);
                if (data.m_TransportType != TransportType.Train
                    || !m_PrefabSystem.TryGetPrefab<PrefabBase>(depot, out var prefab)
                    || !prefab.TryGet<TransportDepot>(out var depotComponent))
                    continue;
                data.m_EnergyTypes = depotComponent.m_EnergyTypes;
                EntityManager.SetComponentData(depot, data);
            }

            using var carriages = m_CarriageQuery.ToEntityArray(Allocator.Temp);
            foreach (var carriage in carriages)
            {
                if (!m_PrefabSystem.TryGetPrefab<TrainPrefab>(carriage, out var train))
                    continue;
                var data = EntityManager.GetComponentData<TrainData>(carriage);
                data.m_EnergyType = train.m_EnergyType;
                EntityManager.SetComponentData(carriage, data);
            }
        }

        /// <summary>Name of the first use (passengers, garbage, a freight resource) left without an electric train, or null.</summary>
        private string FindUseWithoutElectricTrain()
        {
            m_VehicleSelectData.PreUpdate(this, m_CityConfigurationSystem, m_VehiclePrefabQuery, Allocator.TempJob, out var jobHandle);
            jobHandle.Complete();
            try
            {
                if (!CanDrawElectricTrain(PublicTransportPurpose.TransportLine, Resource.NoResource))
                    return "passengers";
                foreach (var resource in FreightResources())
                {
                    if (!CanDrawElectricTrain(0, resource))
                        return resource.ToString();
                }
                return null;
            }
            finally
            {
                m_VehicleSelectData.PostUpdate(default);
            }
        }

        private bool CanDrawElectricTrain(PublicTransportPurpose purpose, Resource resource)
        {
            using var primaries = new NativeList<Entity>(Allocator.Temp);
            using var secondaries = new NativeList<Entity>(Allocator.Temp);
            // Train selection ignores the size class.
            m_VehicleSelectData.ListVehicles(TransportType.Train, EnergyTypes.Electricity, SizeClass.Large, purpose, resource, primaries, secondaries);
            foreach (var primary in primaries)
            {
                // A multiple unit runs on its own; a carriage needs an engine.
                if (EntityManager.HasComponent<MultipleUnitTrainData>(primary) || !secondaries.IsEmpty)
                    return true;
            }
            return false;
        }

        /// <summary>Each resource some train carriage carries, garbage included.</summary>
        private IEnumerable<Resource> FreightResources()
        {
            var carried = Resource.NoResource;
            using var carriages = m_CarriageQuery.ToEntityArray(Allocator.Temp);
            foreach (var carriage in carriages)
            {
                if (EntityManager.GetComponentData<TrainData>(carriage).m_TrackType == TrackTypes.Train
                    && EntityManager.HasComponent<CargoTransportVehicleData>(carriage))
                    carried |= EntityManager.GetComponentData<CargoTransportVehicleData>(carriage).m_Resources;
            }
            var iterator = ResourceIterator.GetIterator();
            while (iterator.Next())
            {
                if ((carried & iterator.resource) != Resource.NoResource)
                    yield return iterator.resource;
            }
        }

        private void LogEngineCensus()
        {
            int electric = 0, fuel = 0, other = 0;
            using var engines = m_EngineQuery.ToComponentDataArray<TrainData>(Allocator.Temp);
            foreach (var engine in engines)
            {
                if (engine.m_TrackType != TrackTypes.Train)
                    continue;
                switch (engine.m_EnergyType)
                {
                    case EnergyTypes.Electricity: electric++; break;
                    case EnergyTypes.Fuel: fuel++; break;
                    default: other++; break;
                }
            }
            Mod.Log.Info($"Train engines: {electric} electric, {fuel} diesel, {other} other");
        }
    }
}
