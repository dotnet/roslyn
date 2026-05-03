// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Xml.Serialization;

namespace CSharpSyntaxGenerator
{
    public class TreeType
    {
        [XmlAttribute]
        public string Name;

        [XmlAttribute]
        public string Base;

        [XmlAttribute]
        public string ExperimentalUrl;

        [XmlAttribute]
        public string SkipConvenienceFactories;

        [XmlElement]
        public Comment TypeComment;

        [XmlElement]
        public Comment FactoryComment;

        [XmlAttribute]
        public string HasValidate;

        [XmlElement(ElementName = "Field", Type = typeof(Field))]
        [XmlElement(ElementName = "Choice", Type = typeof(Choice))]
        [XmlElement(ElementName = "Sequence", Type = typeof(Sequence))]
        public List<TreeTypeChild> Children = new List<TreeTypeChild>();
    }
}
