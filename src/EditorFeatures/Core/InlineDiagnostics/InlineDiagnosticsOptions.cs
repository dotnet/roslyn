// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Editor.InlineDiagnostics
{
    [ExportGlobalOptionProvider, Shared]
    internal sealed class InlineDiagnosticsOptions : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InlineDiagnosticsOptions()
        {
        }

        ImmutableArray<IOption> IOptionProvider.Options { get; } = ImmutableArray.Create<IOption>(
            EnableInlineDiagnostics,
            Location);

        public static readonly PerLanguageOption2<bool> EnableInlineDiagnostics =
            new("InlineDiagnosticsOptions",
                "EnableInlineDiagnostics",
                defaultValue: false,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineDiagnostics"));

        public static readonly PerLanguageOption2<InlineDiagnosticsLocations> Location =
            new("InlineDiagnosticsOptions",
                "Location",
                defaultValue: InlineDiagnosticsLocations.PlacedAtEndOfCode,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineDiagnostics.LocationOption"));
    }
}
