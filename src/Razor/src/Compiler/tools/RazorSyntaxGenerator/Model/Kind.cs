// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Xml.Serialization;

namespace RazorSyntaxGenerator;

public class Kind
{
    [XmlAttribute]
    public string Name;
}
