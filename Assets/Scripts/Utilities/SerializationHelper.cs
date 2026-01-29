using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace PlunkAndPlunder.Utilities
{
    /// <summary>
    /// Helper for serializing game state for networking
    /// </summary>
    public static class SerializationHelper
    {
        public static string SerializeToJson<T>(T obj)
        {
            return JsonUtility.ToJson(obj);
        }

        public static T DeserializeFromJson<T>(string json)
        {
            return JsonUtility.FromJson<T>(json);
        }

        // Note: BinaryFormatter is deprecated in .NET 6+ but kept for MVP compatibility
        // TODO: Replace with System.Text.Json before production
#pragma warning disable SYSLIB0011
        public static byte[] SerializeToBytes<T>(T obj)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public static T DeserializeFromBytes<T>(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return (T)formatter.Deserialize(ms);
            }
        }
#pragma warning restore SYSLIB0011
    }
}
