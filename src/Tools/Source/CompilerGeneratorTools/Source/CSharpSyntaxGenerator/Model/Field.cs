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
        // Note: 'Choice's should not be children of a 'Choice'.  It's not necessary, and the child
        // choice can just be inlined into the parent.
        [XmlElement(ElementName = "Field", Type = typeof(Field))]
        [XmlElement(ElementName = "Sequence", Type = typeof(Sequence))]
        public List<TreeTypeChild> Children;
    }

    public class Sequence : TreeTypeChild
    {
        // Note: 'Sequence's should not be children of a 'Sequence'.  It's not necessary, and the
        // child choice can just be inlined into the parent.
        [XmlElement(ElementName = "Field", Type = typeof(Field))]
        [XmlElement(ElementName = "Choice", Type = typeof(Choice))]
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

        [XmlAttribute]
        public string AllowTrailingSeparator;

        [XmlElement(ElementName = "Kind", Type = typeof(Kind))]
        public List<Kind> Kinds;

        [XmlElement]
        public Comment PropertyComment;

        public bool IsToken => Type == "SyntaxToken";
        public bool IsOptional => Optional == "true";
    }
}
