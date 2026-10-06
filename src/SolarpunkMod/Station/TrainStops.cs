using Game.Prefabs;

namespace SolarpunkMod.Station
{
    /// <summary>Whether a building prefab holds a train stop among its sub-objects.</summary>
    public static class TrainStops
    {
        public static bool Has(PrefabBase building, bool passengersOnly = false)
        {
            if (!building.TryGet<ObjectSubObjects>(out var subObjects) || subObjects.m_SubObjects == null)
                return false;
            foreach (var subObject in subObjects.m_SubObjects)
            {
                if (subObject.m_Object != null
                    && subObject.m_Object.TryGet<TransportStop>(out var stop)
                    && stop.m_TransportType == TransportType.Train
                    && (!passengersOnly || stop.m_PassengerTransport))
                    return true;
            }
            return false;
        }
    }
}
