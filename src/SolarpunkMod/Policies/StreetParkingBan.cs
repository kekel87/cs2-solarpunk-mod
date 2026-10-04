using Colossal.Serialization.Entities;
using Unity.Entities;

namespace SolarpunkMod.Policies
{
    /// <summary>
    /// On a district with the "End residential parking" policy: when the ban started, which starts
    /// the notice before towing. Lost without harm if the mod is removed.
    /// </summary>
    public struct StreetParkingBan : IComponentData, ISerializable
    {
        private const int FormatVersion = 1;

        public uint m_SinceFrame;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(FormatVersion);
            writer.Write(m_SinceFrame);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out int _);
            reader.Read(out m_SinceFrame);
        }
    }
}
