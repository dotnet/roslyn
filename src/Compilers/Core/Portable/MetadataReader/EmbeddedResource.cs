// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Reflection;

namespace Microsoft.CodeAnalysis
{
    internal readonly struct EmbeddedResource
    {
        public readonly uint Offset;
        public readonly ManifestResourceAttributes Attributes;
        public readonly string Name;

        internal EmbeddedResource(uint offset, ManifestResourceAttributes attributes, string name)
        {
            this.Offset = offset;
            this.Attributes = attributes;
            this.Name = name;
        }
    }
}
