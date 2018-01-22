// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;

namespace Microsoft.CodeAnalysis.UnitTests.TestFiles
{
    public static class Resources
    {
        private static Stream GetResourceStream(string name)
        {
            var resourceName = $"Microsoft.CodeAnalysis.UnitTests.Resources.{name}";

            var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (resourceStream != null)
            {
                return resourceStream;
            }

            throw new InvalidOperationException($"Cannot find resource named: '{resourceName}'");
        }

        public static byte[] LoadBytes(string name)
        {
            using (var resourceStream = GetResourceStream(name))
            {
                var bytes = new byte[resourceStream.Length];
                resourceStream.Read(bytes, 0, (int)resourceStream.Length);
                return bytes;
            }
        }

        public static string LoadText(string name)
        {
            using (var streamReader = new StreamReader(GetResourceStream(name)))
            {
                return streamReader.ReadToEnd();
            }
        }
    }
}
