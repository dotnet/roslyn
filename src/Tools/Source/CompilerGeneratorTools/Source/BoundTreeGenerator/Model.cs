﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace BoundTreeGenerator
{
    [XmlRoot]
    public class Tree
    {
        [XmlAttribute]
        public string Root;

        [XmlElement(ElementName = "Node", Type = typeof(Node))]
        [XmlElement(ElementName = "AbstractNode", Type = typeof(AbstractNode))]
        [XmlElement(ElementName = "PredefinedNode", Type = typeof(PredefinedNode))]
        [XmlElement(ElementName = "Enum", Type = typeof(EnumType))]
        [XmlElement(ElementName = "ValueType", Type = typeof(ValueType))]
        public List<TreeType> Types;
    }

    public class TreeType
    {
        [XmlAttribute]
        public string Name;

        [XmlAttribute]
        public string Base;

        [XmlAttribute]
        public string HasValidate;
    }

    public class PredefinedNode : TreeType
    {
    }

    public class AbstractNode : TreeType
    {
        [XmlElement(ElementName = "Field", Type = typeof(Field))]
        public List<Field> Fields;
    }

    public class Node : TreeType
    {
        [XmlAttribute]
        public string Root;

        [XmlAttribute]
        public string Errors;

        /// <summary>
        /// For nodes that have additional fields defined in code, it is necessary to provide
        /// a hand-written implementation of ShallowClone.
        /// </summary>
        [XmlAttribute]
        public string SkipShallowClone;

        [XmlElement(ElementName = "Kind", Type = typeof(Kind))]
        public List<Kind> Kinds;

        [XmlElement(ElementName = "Field", Type = typeof(Field))]
        public List<Field> Fields;
    }

    public class Kind
    {
        [XmlAttribute]
        public string Name;
    }

    public class Field
    {
        [XmlAttribute]
        public string Name;

        [XmlAttribute]
        public string Type;

        [XmlAttribute]
        public string Null;

        [XmlAttribute]
        public bool Override;

        [XmlAttribute]
        public string New;

        [XmlAttribute]
        public string PropertyOverrides;

        [XmlAttribute]
        public string SkipInVisitor;
    }

    public class EnumType : TreeType
    {
        [XmlAttribute]
        public string Flags;

        [XmlElement(ElementName = "Field", Type = typeof(EnumField))]
        public List<EnumField> Fields;
    }

    public class EnumField
    {
        [XmlAttribute]
        public string Name;

        [XmlAttribute]
        public string Value;
    }

    public class ValueType : TreeType
    {
    }
}
