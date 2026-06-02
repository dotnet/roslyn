// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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

        [XmlAttribute]
        public string UpdateMethodModifiers;
    }

    public class PredefinedNode : TreeType
    {
    }

    public class AbstractNode : TreeType
    {
        [XmlElement(ElementName = "Field", Type = typeof(Field))]
        public List<Field> Fields;
    }

    public class Node : AbstractNode
    {
        [XmlAttribute]
        public string Root;

        [XmlAttribute]
        public string Errors;

        /// <summary>
        /// For nodes such as BoundBinaryOperators where we use an iterative algorithm instead of the standard
        /// recursive algorithm to deal with deeply-nested stacks
        /// </summary>
        [XmlAttribute]
        public string SkipInNullabilityRewriter;

        /// <summary>
        /// See PipelinePhase enum
        /// </summary>
        [XmlAttribute]
        public string DoesNotSurvive;
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

        [XmlAttribute]
        public string SkipInNullabilityRewriter;
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
