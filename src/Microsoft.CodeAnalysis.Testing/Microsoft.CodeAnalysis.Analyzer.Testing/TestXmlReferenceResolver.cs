// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.CodeAnalysis.Testing
{
    internal class TestXmlReferenceResolver : XmlReferenceResolver
    {
        public Dictionary<string, string> XmlReferences { get; } =
            new Dictionary<string, string>();

        public override bool Equals(object other)
        {
            return ReferenceEquals(this, other);
        }

        public override int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(this);
        }

        public override Stream OpenRead(string resolvedPath)
        {
            if (resolvedPath is null)
            {
                throw new ArgumentNullException(nameof(resolvedPath));
            }

            if (!XmlReferences.TryGetValue(resolvedPath, out var content))
            {
                throw new IOException($"Unable to read XML file: {resolvedPath}");
            }

            return new MemoryStream(Encoding.UTF8.GetBytes(content));
        }

        public override string? ResolveReference(string path, string baseFilePath)
        {
            if (!XmlReferences.ContainsKey(path))
            {
                return null;
            }

            return path;
        }
    }
}
