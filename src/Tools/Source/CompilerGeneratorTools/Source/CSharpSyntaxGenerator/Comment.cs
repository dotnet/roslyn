// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Xml;
using System.Xml.Serialization;

namespace CSharpSyntaxGenerator
{
    public class Comment
    {
        [XmlAnyElement]
        public XmlElement[] Body;
    }
}
