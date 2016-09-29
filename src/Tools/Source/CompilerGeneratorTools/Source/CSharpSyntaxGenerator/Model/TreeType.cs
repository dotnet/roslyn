// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Xml.Serialization;

namespace CSharpSyntaxGenerator
{
    public class TreeType
    {
        [XmlAttribute]
        public string Name;

        [XmlAttribute]
        public string Base;

        [XmlElement]
        public Comment TypeComment;

        [XmlElement]
        public Comment FactoryComment;
    }
}
