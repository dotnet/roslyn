// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly byte[] _bytes;

        public SerializableVersionStamp(VersionStamp versionStamp)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var objectWriter = new ObjectWriter(memoryStream))
                {
                    versionStamp.WriteTo(objectWriter);
                }

                _bytes = memoryStream.ToArray();
            }
        }

        public VersionStamp VersionStamp
        {
            get
            {
                using (var stream = new MemoryStream(_bytes))
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
