// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace CSharpSyntaxGenerator
{
    public class AbstractNode : TreeType
    {
        [XmlElement(ElementName = "Field", Type = typeof(Field))]
        [XmlElement(ElementName = "Choice", Type = typeof(Choice))]
        [XmlElement(ElementName = "Sequence", Type = typeof(Sequence))]
        public List<TreeTypeChild> Children;

        public readonly List<Field> Fields = new List<Field>();
    }
}
