// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml.Serialization;

namespace CSharpSyntaxGenerator
{
    public class Kind
    {
        [XmlAttribute]
        public string Name;

        public override bool Equals(object obj)
            => obj is Kind kind &&
               Name == kind.Name;

        public override int GetHashCode()
            => Name.GetHashCode();
    }
}
