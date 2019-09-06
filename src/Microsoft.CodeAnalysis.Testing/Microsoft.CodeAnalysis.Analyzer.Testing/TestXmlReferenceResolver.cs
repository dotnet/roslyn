// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public override Stream? OpenRead(string resolvedPath)
        {
            if (!XmlReferences.TryGetValue(resolvedPath, out var content))
            {
                return null;
            }

            return new MemoryStream(Encoding.UTF8.GetBytes(content));
        }

        public override string ResolveReference(string path, string baseFilePath)
        {
            return path;
        }
    }
}
