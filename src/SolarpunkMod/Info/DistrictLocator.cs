using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Areas;
using Game.Creatures;
using Game.Vehicles;
using Unity.Entities;
using Unity.Mathematics;

namespace SolarpunkMod.Info
{
    /// <summary>
    /// Whether a moving vehicle or a person is in a district, from the middle of the lane it is on,
    /// tested against the district's triangles as the game does for buildings
    /// (CurrentDistrictSystem.DistrictIterator). Covers road lanes, intersections, paths, bike lanes,
    /// tracks and lanes inside lots alike; a border road's lanes fall on either side.
    /// </summary>
    internal class DistrictLocator
    {
        private ComponentLookup<Game.Net.Curve> m_CurveLookup;
        private ComponentLookup<CarCurrentLane> m_CarCurrentLaneLookup;
        private ComponentLookup<TrainCurrentLane> m_TrainCurrentLaneLookup;
        private ComponentLookup<HumanCurrentLane> m_HumanCurrentLaneLookup;

        private readonly List<Triangle2> m_DistrictTriangles = new List<Triangle2>();
        private Bounds2 m_DistrictBounds;

        // Many people and vehicles share a lane: resolve each lane once per sample.
        private readonly Dictionary<Entity, bool> m_LaneInDistrict = new Dictionary<Entity, bool>();

        public DistrictLocator(SystemBase system)
        {
            m_CurveLookup = system.GetComponentLookup<Game.Net.Curve>(true);
            m_CarCurrentLaneLookup = system.GetComponentLookup<CarCurrentLane>(true);
            m_TrainCurrentLaneLookup = system.GetComponentLookup<TrainCurrentLane>(true);
            m_HumanCurrentLaneLookup = system.GetComponentLookup<HumanCurrentLane>(true);
        }

        /// <summary>Starts a sample: refreshes the lookups and reads the outline of the district the questions are about.</summary>
        public void Update(SystemBase system, Entity district)
        {
            m_LaneInDistrict.Clear();
            m_CurveLookup.Update(system);
            m_CarCurrentLaneLookup.Update(system);
            m_TrainCurrentLaneLookup.Update(system);
            m_HumanCurrentLaneLookup.Update(system);

            m_DistrictTriangles.Clear();
            m_DistrictBounds = new Bounds2(float.MaxValue, float.MinValue);
            var entityManager = system.EntityManager;
            if (!entityManager.HasBuffer<Node>(district) || !entityManager.HasBuffer<Triangle>(district))
                return;
            var nodes = entityManager.GetBuffer<Node>(district, true);
            foreach (var triangle in entityManager.GetBuffer<Triangle>(district, true))
            {
                var triangle2 = AreaUtils.GetTriangle2(nodes, triangle);
                m_DistrictTriangles.Add(triangle2);
                m_DistrictBounds |= MathUtils.Bounds(triangle2);
            }
        }

        /// <param name="vehicle">The vehicle the person is in, or Entity.Null when on foot.</param>
        public bool IsPersonIn(Entity person, Entity vehicle)
        {
            if (vehicle == Entity.Null)
                return m_HumanCurrentLaneLookup.TryGetComponent(person, out var humanLane) && IsLaneIn(humanLane.m_Lane);
            if (m_CarCurrentLaneLookup.TryGetComponent(vehicle, out var carLane))
                return IsLaneIn(carLane.m_Lane);
            return m_TrainCurrentLaneLookup.TryGetComponent(vehicle, out var trainLane) && IsLaneIn(trainLane.m_Front.m_Lane);
        }

        public bool IsLaneIn(Entity lane)
        {
            if (!m_LaneInDistrict.TryGetValue(lane, out var isIn))
            {
                isIn = m_CurveLookup.TryGetComponent(lane, out var curve) && IsPositionIn(MathUtils.Position(curve.m_Bezier, 0.5f).xz);
                m_LaneInDistrict.Add(lane, isIn);
            }
            return isIn;
        }

        private bool IsPositionIn(float2 position)
        {
            if (!MathUtils.Intersect(m_DistrictBounds, position))
                return false;
            foreach (var triangle in m_DistrictTriangles)
            {
                if (MathUtils.Intersect(triangle, position))
                    return true;
            }
            return false;
        }
    }
}
