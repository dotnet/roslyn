// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
