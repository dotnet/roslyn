﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Xml.Serialization;
using System.Xml;

namespace CSharpSyntaxGenerator
{
    public class Field
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
