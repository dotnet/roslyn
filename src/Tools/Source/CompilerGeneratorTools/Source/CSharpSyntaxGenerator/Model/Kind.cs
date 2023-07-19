// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Serialization;

namespace CSharpSyntaxGenerator
{
    public class Kind : IEquatable<Kind>
    {
        [XmlAttribute]
        public string? Name;

        public override bool Equals(object? obj)
            => Equals(obj as Kind);

        public bool Equals(Kind? other)
            => Name == other?.Name;

        public override int GetHashCode()
            => Name == null ? 0 : Name.GetHashCode();
    }
}
