using StackExchange.Redis;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace MvcMovie
{
public static class SampleStackExchangeRedisExtensions
{
   public static T Get<T>(this IDatabase cache, string key)
   {
      return Deserialize<T>(cache.StringGet(key));
   }

   public static object Get(this IDatabase cache, string key)
   {
      return Deserialize<object>(cache.StringGet(key));
   }

   public static void Set(this IDatabase cache, string key, object value)
   {
      cache.StringSet(key, Serialize(value));
   }

   static byte[] Serialize(object o)
   {
      if (o == null)
      {
         return null;
      }
      BinaryFormatter binaryFormatter = new BinaryFormatter();
      using (MemoryStream memoryStream = new MemoryStream())
      {
         binaryFormatter.Serialize(memoryStream, o);
         byte[] objectDataAsStream = memoryStream.ToArray();
         return objectDataAsStream;
      }
   }

   static T Deserialize<T>(byte[] stream)
   {
      BinaryFormatter binaryFormatter = new BinaryFormatter();
      if (stream == null)
         return default(T);

      using (MemoryStream memoryStream = new MemoryStream(stream))
      {
         T result = (T)binaryFormatter.Deserialize(memoryStream);
         return result;
      }
   }
}
}