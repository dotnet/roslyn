// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes;

internal sealed class RQOrdinaryMethodPropertyOrEventName : RQMethodPropertyOrEventName
{
    // the construct type should always match the containing member.
    // I don't think we need to expose this, shouldn't you know this from your containing member?
    private readonly string _constructType;

    public readonly string Name;

    internal RQOrdinaryMethodPropertyOrEventName(string constructType, string name)
    {
        _constructType = constructType;
        Name = name;
    }

    public override string OrdinaryNameValue
    {
        get { return Name; }
    }

    public static RQOrdinaryMethodPropertyOrEventName CreateConstructorName()
        => new(RQNameStrings.MethName, RQNameStrings.SpecialConstructorName);

    public static RQOrdinaryMethodPropertyOrEventName CreateDestructorName()
        => new(RQNameStrings.MethName, RQNameStrings.SpecialDestructorName);

    public static RQOrdinaryMethodPropertyOrEventName CreateOrdinaryIndexerName()
        => new(RQNameStrings.PropName, RQNameStrings.SpecialIndexerName);

    public static RQOrdinaryMethodPropertyOrEventName CreateOrdinaryMethodName(string name)
        => new(RQNameStrings.MethName, name);

    public static RQOrdinaryMethodPropertyOrEventName CreateOrdinaryEventName(string name)
        => new(RQNameStrings.EventName, name);

    public static RQOrdinaryMethodPropertyOrEventName CreateOrdinaryPropertyName(string name)
        => new(RQNameStrings.PropName, name);

    public override SimpleGroupNode ToSimpleTree()
        => new(_constructType, Name);
}
