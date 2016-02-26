using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    internal static class SerializationHelpers
    {
        public static string SerializeObject<T>(this T toSerialize)
        {
            using (var stream = new MemoryStream())
            {
                var serializer = new DataContractSerializer(typeof(T));
                serializer.WriteObject(stream, toSerialize);

                stream.Seek(0, SeekOrigin.Begin);
                using (var streamReader = new StreamReader(stream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }

        public static object DeserializeObject<T>(this string toDeserialize)
        {
            using (var stream = new MemoryStream())
            {
                var data = Encoding.UTF8.GetBytes(toDeserialize);
                stream.Write(data, 0, data.Length);
                stream.Seek(0, SeekOrigin.Begin);

                DataContractSerializer serializer = new DataContractSerializer(typeof(T));
                return serializer.ReadObject(stream);
            }
        }
    }
}
