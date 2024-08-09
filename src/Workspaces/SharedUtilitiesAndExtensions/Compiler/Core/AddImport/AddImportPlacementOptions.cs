// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Options;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Host;
#endif

namespace Microsoft.CodeAnalysis.AddImport;

[DataContract]
internal sealed record class AddImportPlacementOptions
{
    public static readonly CodeStyleOption2<AddImportPlacement> s_outsideNamespacePlacementWithSilentEnforcement =
       new(AddImportPlacement.OutsideNamespace, NotificationOption2.Silent);

    [DataMember]
    public bool PlaceSystemNamespaceFirst { get; init; } = true;

    /// <summary>
    /// Where to place C# usings relative to namespace declaration, ignored by VB.
    /// </summary>
    [DataMember]
    public CodeStyleOption2<AddImportPlacement> UsingDirectivePlacement { get; init; } = s_outsideNamespacePlacementWithSilentEnforcement;

    [DataMember]
    public bool AllowInHiddenRegions { get; init; } = false;

    public bool PlaceImportsInsideNamespaces => UsingDirectivePlacement.Value == AddImportPlacement.InsideNamespace;

    public static readonly AddImportPlacementOptions Default = new();
}
