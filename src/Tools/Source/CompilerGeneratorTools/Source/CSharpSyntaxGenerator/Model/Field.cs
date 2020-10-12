// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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

        [XmlAttribute]
        public bool Optional;
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
        public int MinCount;

        [XmlAttribute]
        public bool AllowTrailingSeparator;

        [XmlElement(ElementName = "Kind", Type = typeof(Kind))]
        public List<Kind> Kinds = new List<Kind>();

        [XmlElement]
        public Comment PropertyComment;

        public bool IsToken => Type == "SyntaxToken";
        public bool IsOptional => Optional == "true";
    }
}
