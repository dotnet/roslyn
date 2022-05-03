// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.AddImport;

internal class CSharpAddImportPlacementOptionsStorage
{
    [ExportLanguageService(typeof(IAddImportPlacementOptionsStorage), LanguageNames.CSharp), Shared]
    private sealed class Service : IAddImportPlacementOptionsStorage
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public Service()
        {
        }

        public AddImportPlacementOptions GetOptions(IGlobalOptionService globalOptions)
            => GetCSharpAddImportPlacementOptions(globalOptions);
    }

    internal static AddImportPlacementOptions GetCSharpAddImportPlacementOptions(IGlobalOptionService globalOptions)
        => new(
            PlaceSystemNamespaceFirst: globalOptions.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, LanguageNames.CSharp),
            PlaceImportsInsideNamespaces: globalOptions.GetOption(CSharpCodeStyleOptions.PreferredUsingDirectivePlacement).Value == AddImportPlacement.InsideNamespace,
            AllowInHiddenRegions: false); // no global option available);
}
