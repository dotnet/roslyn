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
    internal static class InlineDiagnosticsOptions
    {
        public static readonly PerLanguageOption2<bool> EnableInlineDiagnostics =
            new(nameof(InlineDiagnosticsOptions),
                nameof(EnableInlineDiagnostics),
                defaultValue: false,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineDiagnostics"));

        public static readonly PerLanguageOption2<InlineDiagnosticsLocations> Location =
            new(nameof(InlineDiagnosticsOptions),
                nameof(Location),
                defaultValue: InlineDiagnosticsLocations.PlacedAtEndOfCode,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineDiagnostics.LocationOption"));
    }

    [ExportOptionProvider, Shared]
    internal sealed class InlineDiagnosticsOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InlineDiagnosticsOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            InlineDiagnosticsOptions.EnableInlineDiagnostics,
            InlineDiagnosticsOptions.Location);
    }
}
