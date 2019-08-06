// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

namespace IOperationGenerator
{
    [XmlRoot]
    public class Tree
    {
        [XmlAttribute]
        public string Root;

        [XmlElement(ElementName = "UnusedOperationKinds")]
        public OperationKind UnusedOperationKinds;

        [XmlElement(ElementName = "Node", Type = typeof(Node))]
        [XmlElement(ElementName = "AbstractNode", Type = typeof(AbstractNode))]
        public List<TreeType> Types;
    }

    public class TreeType
    {
        [XmlAttribute]
        public string Name;

        [XmlAttribute]
        public string Base;

        [XmlAttribute]
        public string? Namespace;

        [XmlAttribute(AttributeName = "Internal")]
        public string? InternalText;

        public bool IsInternal => InternalText is "true";

        [XmlElement(ElementName = "Comments", Type = typeof(Comments))]
        public Comments Comments;
    }

    public class AbstractNode : TreeType
    {
        [XmlElement(ElementName = "Property", Type = typeof(Property))]
        public List<Property> Properties;

        [XmlElement(ElementName = "Obsolete", Type = typeof(ObsoleteTag))]
        public ObsoleteTag? Obsolete;

        [XmlElement(ElementName = "OperationKind", Type = typeof(OperationKind))]
        public OperationKind? OperationKind;

        [XmlAttribute(AttributeName = "SkipInVisitor")]
        public string SkipInVisitorText;
        public bool SkipInVisitor => SkipInVisitorText == "true";

        public virtual bool IsAbstract => true;

    }

    public class Node : AbstractNode
    {
        public override bool IsAbstract => false;
    }

    public class Kind
    {
        [XmlAttribute]
        public string Name;
    }

    public class Property
    {
        [XmlAttribute]
        public string Name;

        [XmlAttribute]
        public string Type;

        [XmlAttribute(AttributeName = "New")]
        public string NewText;
        public bool IsNew => NewText == "true";

        [XmlElement(ElementName = "Comments", Type = typeof(Comments))]
        public Comments? Comments;
    }

    public class OperationKind
    {
        [XmlAttribute(AttributeName = "Include")]
        public string? IncludeText;
        public bool? Include => IncludeText is null ? (bool?)null : IncludeText == "true";

        [XmlAttribute]
        public string? ExtraDescription;

        [XmlElement(ElementName = "Entry", Type = typeof(OperationKindEntry))]
        public List<OperationKindEntry> Entries;
    }

    public class OperationKindEntry
    {
        [XmlAttribute]
        public string Name;

        [XmlAttribute(AttributeName = "Value")]
        public string ValueText;
        public int Value => int.Parse(ValueText.Substring(2), System.Globalization.NumberStyles.HexNumber);

        [XmlAttribute(AttributeName = "EditorBrowsable")]
        public string? EditorBrowsableText;
        public bool? EditorBrowsable => EditorBrowsableText is null ? (bool?)null : EditorBrowsableText == "true";

        [XmlAttribute]
        public string? ExtraDescription;
    }

    public class ObsoleteTag
    {
        [XmlAttribute(AttributeName = "Error")]
        public string ErrorText;

        [XmlText]
        public string Message;
    }

    public class Comments
    {
        [XmlAnyElement]
        public XmlElement[] Elements;
    }
}
