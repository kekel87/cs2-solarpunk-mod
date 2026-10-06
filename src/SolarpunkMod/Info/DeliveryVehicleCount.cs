using System;
using Game.Vehicles;
using SolarpunkMod.Hub;
using Unity.Collections;
using Unity.Entities;

namespace SolarpunkMod.Info
{
    /// <summary>
    /// Delivery vehicles on the move, split into trucks and the small vehicles of the Local Hub (our
    /// cargo bikes, trikes and kei trucks, or the game's smallest van it falls back on). Shared by the
    /// city panel and the district section. The query needs DeliveryTruck, CarCurrentLane and PrefabRef.
    /// </summary>
    internal readonly struct DeliveryVehicleCount
    {
        public readonly int Trucks;
        public readonly int SmallVehicles;

        private DeliveryVehicleCount(int trucks, int smallVehicles)
        {
            Trucks = trucks;
            SmallVehicles = smallVehicles;
        }

        /// <param name="isLaneCounted">Which lanes count (a district), or null for the whole city.</param>
        public static DeliveryVehicleCount Of(EntityQuery query, HubDeliveryVehicles hubVehicles, Func<Entity, bool> isLaneCounted)
        {
            using var trucks = query.ToComponentDataArray<DeliveryTruck>(Allocator.Temp);
            using var lanes = query.ToComponentDataArray<CarCurrentLane>(Allocator.Temp);
            using var prefabs = query.ToComponentDataArray<Game.Prefabs.PrefabRef>(Allocator.Temp);
            int truckCount = 0, smallCount = 0;
            for (var i = 0; i < trucks.Length; i++)
            {
                if ((trucks[i].m_State & DeliveryTruckFlags.DummyTraffic) != 0
                    || (isLaneCounted != null && !isLaneCounted(lanes[i].m_Lane)))
                    continue;
                if (hubVehicles.IsSmallVehicle(prefabs[i].m_Prefab))
                    smallCount++;
                else
                    truckCount++;
            }
            return new DeliveryVehicleCount(truckCount, smallCount);
        }
    }
}
