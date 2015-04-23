// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Reflection;

namespace TestResources
{
    internal static class Z
    {
        public static readonly byte[] dll = ResourceHelper.GetResource("z.dll");
        public static readonly byte[] pdb = ResourceHelper.GetResource("z.pdbx");
    }

    internal static class ResourceHelper
    {
        public static Stream GetResourceStream(string name)
        {
            string fullName = $"{nameof(Microsoft)}.{nameof(Microsoft.DiaSymReader)}.{nameof(Microsoft.DiaSymReader.PortablePdb)}.{nameof(Microsoft.DiaSymReader.PortablePdb.UnitTests)}.Resources." + name;
            return typeof(ResourceHelper).GetTypeInfo().Assembly.GetManifestResourceStream(fullName);
        }

        public static byte[] GetResource(string name)
        {
            using (var stream = GetResourceStream(name))
            {
                var bytes = new byte[stream.Length];
                using (var memoryStream = new MemoryStream(bytes))
                {
                    stream.CopyTo(memoryStream);
                }

                return bytes;
            }
        }
    }
}
