// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace CSharpSyntaxGenerator
{
    public class TreeTypeChild
    {
    }

    public class Choice : TreeTypeChild
    {
        [XmlElement(ElementName = "Field", Type = typeof(Field))]
        [XmlElement(ElementName = "Choice", Type = typeof(Choice))]
        [XmlElement(ElementName = "Sequence", Type = typeof(Sequence))]
        public List<TreeTypeChild> Children;
    }

    public class Sequence : TreeTypeChild
    {
        [XmlElement(ElementName = "Field", Type = typeof(Field))]
        [XmlElement(ElementName = "Choice", Type = typeof(Choice))]
        [XmlElement(ElementName = "Sequence", Type = typeof(Sequence))]
        public List<TreeTypeChild> Children;
    }

    public class Field : TreeTypeChild
    {
        [XmlAttribute]
        public string Name;

        [XmlAttribute]
        public string Type;

        [XmlAttribute]
        public string Optional;

        [XmlAttribute]
        public string Override;

        [XmlAttribute]
        public string New;

        [XmlAttribute]
        public string MinCount;

        [XmlElement(ElementName = "Kind", Type = typeof(Kind))]
        public List<Kind> Kinds;

        [XmlElement]
        public Comment PropertyComment;
    }
}
