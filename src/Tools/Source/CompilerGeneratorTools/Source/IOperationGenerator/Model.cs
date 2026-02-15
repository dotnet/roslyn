// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
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

    [DebuggerDisplay("{Name, nq}")]
    public class TreeType
    {
        [XmlAttribute]
        public string Name;

        [XmlAttribute]
        public string Base;

        [XmlAttribute]
        public string? ExperimentalUrl;

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

        [XmlAttribute(AttributeName = "SkipClassGeneration")]
        public string SkipClassGenerationText;
        public bool SkipClassGeneration => SkipClassGenerationText == "true";

        public virtual bool IsAbstract => true;
    }

    public class Node : AbstractNode
    {
        [XmlAttribute]
        public string? VisitorName;

        [XmlAttribute(AttributeName = "SkipInVisitor")]
        public string? SkipInVisitorText;
        public bool SkipInVisitor => SkipInVisitorText == "true";

        [XmlAttribute(AttributeName = "SkipChildrenGeneration")]
        public string? SkipChildrenGenerationText;
        public bool SkipChildrenGeneration => SkipChildrenGenerationText == "true";

        [XmlAttribute(AttributeName = "SkipInCloner")]
        public string? SkipInClonerText;
        public bool SkipInCloner => SkipInClonerText == "true";

        [XmlAttribute]
        public string? ChildrenOrder;

        public override bool IsAbstract => false;

        [XmlAttribute(AttributeName = "HasType")]
        public string HasTypeText;
        public bool HasType => HasTypeText == "true";

        [XmlAttribute(AttributeName = "HasConstantValue")]
        public string HasConstantValueText;
        public bool HasConstantValue => HasConstantValueText == "true";
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

        [XmlAttribute]
        public string? ExperimentalUrl;

        [XmlAttribute(AttributeName = "New")]
        public string NewText;
        public bool IsNew => NewText == "true";

        [XmlAttribute(AttributeName = "Internal")]
        public string? IsInternalText;
        public bool IsInternal => IsInternalText == "true";

        [XmlAttribute(AttributeName = "Override")]
        public string? IsOverrideText;
        public bool IsOverride => IsOverrideText == "true";

        [XmlAttribute(AttributeName = "SkipGeneration")]
        public string? SkipGenerationText;
        public bool SkipGeneration => SkipGenerationText == "true";

        [XmlAttribute(AttributeName = "MakeAbstract")]
        public string? MakeAbstractText;
        public bool MakeAbstract => MakeAbstractText == "true";

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
