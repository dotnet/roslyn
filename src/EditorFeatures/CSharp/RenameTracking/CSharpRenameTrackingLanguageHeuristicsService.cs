// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.CSharp.RenameTracking;

[ExportLanguageService(typeof(IRenameTrackingLanguageHeuristicsService), LanguageNames.CSharp), Shared]
internal sealed class CSharpRenameTrackingLanguageHeuristicsService : IRenameTrackingLanguageHeuristicsService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpRenameTrackingLanguageHeuristicsService()
    {
    }

    public bool IsIdentifierValidForRenameTracking(string name)
        => name is not "var" and not "dynamic" and not "_";
}
