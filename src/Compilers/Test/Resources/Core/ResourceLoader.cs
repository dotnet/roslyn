// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace TestResources
{
    internal static class ResourceLoader
    {
        private static Stream GetResourceStream(string name)
        {
            var assembly = typeof(ResourceLoader).GetTypeInfo().Assembly;

            var stream = assembly.GetManifestResourceStream(name);
            if (stream == null)
            {
                throw new InvalidOperationException($"Resource '{name}' not found in {assembly.FullName}.");
            }

            return stream;
        }

        public static byte[] GetResourceBlob(string name)
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

        public static byte[] GetOrCreateResource(ref byte[] resource, string name)
        {
            if (resource == null)
            {
                resource = GetResourceBlob(name);
            }

            return resource;
        }

        public static string GetOrCreateResource(ref string resource, string name)
        {
            if (resource == null)
            {
                resource = GetResource(name);
            }

            return resource;
        }

        public static string GetResource(string name)
        {
            using (var stream = GetResourceStream(name))
            {
                using (var streamReader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }
    }
}
