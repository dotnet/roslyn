using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    [Serializable]
    public sealed class SerializableVersionStamp
    {
        private readonly byte[] bytes;

        public SerializableVersionStamp(VersionStamp versionStamp)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var objectWriter = new ObjectWriter(memoryStream))
                {
                    versionStamp.WriteTo(objectWriter);
                }

                bytes = memoryStream.ToArray();
            }
        }

        public VersionStamp VersionStamp
        {
            get
            {
                using (var stream = new MemoryStream(bytes))
                {
                    using (var objectReader = new ObjectReader(stream))
                    {
                        return VersionStamp.ReadFrom(objectReader);
                    }
                }
            }
        }
    }
}
