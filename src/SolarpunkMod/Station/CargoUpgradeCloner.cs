using System;
using System.Collections.Generic;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;

namespace SolarpunkMod.Station
{
    /// <summary>
    /// Builds upgrades that graft cargo storage onto a vanilla building, like a harbor's railway
    /// connection: copies of the vanilla cargo train terminal, whose cargo station parts add the
    /// storage to the building they are placed on (CargoTransportStation.GetUpgradeComponents).
    /// Shared by the station platforms and the waste rail sidings.
    /// </summary>
    internal class CargoUpgradeCloner
    {
        public const string TemplateName = "CargoTrainTerminal01";

        // Like Tigon's rail extensions: placed near the host rather than snapped to its lot.
        private const float MaxPlacementDistance = 200f;

        private readonly PrefabSystem m_PrefabSystem;
        private readonly EntityManager m_EntityManager;

        public CargoUpgradeCloner(PrefabSystem prefabSystem, EntityManager entityManager)
        {
            m_PrefabSystem = prefabSystem;
            m_EntityManager = entityManager;
        }

        public bool TryGetTemplate(out PrefabBase template) =>
            m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(BuildingPrefab), TemplateName), out template);

        /// <summary>The building prefabs matched by the query that the filter accepts: the upgrade's hosts.</summary>
        public BuildingPrefab[] FindHosts(EntityQuery query, Predicate<BuildingPrefab> accept)
        {
            var hosts = new List<BuildingPrefab>();
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                if (m_PrefabSystem.GetPrefab<PrefabBase>(entity) is BuildingPrefab host && accept(host))
                    hosts.Add(host);
            }
            return hosts.ToArray();
        }

        public bool AddUpgrade(PrefabBase template, string name, uint cost, BuildingPrefab[] hosts, Action<PrefabBase> specialise)
        {
            var upgradePrefab = template.Clone(name);
            upgradePrefab.Remove<ObsoleteIdentifiers>();
            if (upgradePrefab.TryGet<UIObject>(out var uiObject))
                uiObject.m_Group = null;
            var upgrade = upgradePrefab.AddComponent<ServiceUpgrade>();
            upgrade.m_Buildings = hosts;
            upgrade.m_UpgradeCost = cost;
            upgrade.m_MaxPlacementDistance = MaxPlacementDistance;
            specialise(upgradePrefab);

            if (m_PrefabSystem.AddPrefab(upgradePrefab))
                return true;
            Mod.Log.Error($"Could not register the {name} upgrade");
            return false;
        }

        /// <summary>Upgrades that only trade garbage.</summary>
        public static void TradeGarbageOnly(PrefabBase upgradePrefab)
        {
            if (upgradePrefab.TryGet<CargoTransportStation>(out var cargoStation))
                cargoStation.m_TradedResources = new[] { ResourceInEditor.Garbage };
        }

        /// <summary>
        /// The game's storage and cargo systems read these from the prefab of whatever building holds
        /// the storage (StorageCompanySystem indexes StorageLimitData and StorageCompanyData of the
        /// building's own prefab before adding its upgrades), and the hosts have none: give each a
        /// neutral base the upgrades then combine onto (resources OR-ed, limit and transports added),
        /// so an upgrade's own data decides what is stored and how much. The transport interval is
        /// not combined, hence kept from the terminal. Prefab data only, rebuilt every launch;
        /// queries that look for storage company prefabs also require IndustrialProcessData, which
        /// the hosts lack.
        /// </summary>
        public void GiveNeutralCargoData(PrefabBase template, BuildingPrefab[] hosts)
        {
            var templateEntity = m_PrefabSystem.GetEntity(template);
            var storage = m_EntityManager.GetComponentData<StorageCompanyData>(templateEntity);
            storage.m_StoredResources = Resource.NoResource;
            var cargoStation = m_EntityManager.GetComponentData<CargoTransportStationData>(templateEntity);
            foreach (var host in hosts)
            {
                var entity = m_PrefabSystem.GetEntity(host);
                AddIfMissing(entity, storage);
                AddIfMissing(entity, new StorageLimitData { m_Limit = 0 });
                AddIfMissing(entity, cargoStation);
                AddIfMissing(entity, new TransportCompanyData { m_MaxTransports = 0 });
            }
        }

        public void AddIfMissing<T>(Entity entity, T data) where T : unmanaged, IComponentData
        {
            if (!m_EntityManager.HasComponent<T>(entity))
                m_EntityManager.AddComponentData(entity, data);
        }
    }
}
