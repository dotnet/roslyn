// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace CSharpSyntaxGenerator
{
    public class FieldOrChoice
    {
    }

    public class Choice : FieldOrChoice
    {
        [XmlAttribute]
        public string Optional;

        [XmlElement(ElementName = "Field", Type = typeof(Field))]
        [XmlElement(ElementName = "Choice", Type = typeof(Choice))]
        public List<FieldOrChoice> FieldsAndChoices;
    }

    public class Field : FieldOrChoice
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

        [XmlElement(ElementName = "Kind", Type = typeof(Kind))]
        public List<Kind> Kinds;

        [XmlElement]
        public Comment PropertyComment;
    }
}
