// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace CSharpSyntaxGenerator
{
    public class Node : TreeType
    {
        [XmlAttribute]
        public string Root;

        [XmlAttribute]
        public string Errors;

        /// <summary>
        /// Even if the Node only has optional or struct fields, don't treat it as AutoCreatable.
        /// Don't introduce a factory method with no arguments.
        /// </summary>
        [XmlAttribute]
        public bool AvoidAutoCreation = false;

        /// <summary>
        /// The factory method with all the fields should be internal. The corresponding Update method as well.
        /// Other factory methods are not generated and have to be written by hand.
        /// </summary>
        [XmlAttribute]
        public bool InternalFactory = false;

        [XmlElement(ElementName = "Kind", Type = typeof(Kind))]
        public List<Kind> Kinds;

        [XmlElement(ElementName = "Field", Type = typeof(Field))]
        public List<Field> Fields;
    }
}
