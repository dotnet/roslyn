// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Editor.InlineErrors
{
    internal static class InlineErrorsOptions
    {
        public static readonly PerLanguageOption2<bool> EnableInlineDiagnostics =
            new(nameof(InlineErrorsOptions),
                nameof(EnableInlineDiagnostics),
                defaultValue: false,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineDiagnostics"));

        public static readonly PerLanguageOption2<InlineErrorsLocations> LocationOption =
            new(nameof(InlineErrorsOptions),
                nameof(LocationOption),
                defaultValue: InlineErrorsLocations.Default,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineDiagnostics.LocationOption"));
    }

    [ExportOptionProvider, Shared]
    internal sealed class InlineErrorsOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InlineErrorsOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            InlineErrorsOptions.EnableInlineDiagnostics,
            InlineErrorsOptions.LocationOption);
    }
}
