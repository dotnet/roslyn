// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.InlineParameterNameHints
{
    internal static class InlineParameterNameHintsOptions
    {
        public static readonly PerLanguageOption2<bool> Enabled =
            new(nameof(InlineParameterNameHintsOptions),
                nameof(Enabled),
                defaultValue: false,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.Enabled"));
    }

    [ExportOptionProvider, Shared]
    internal sealed class InlineParameterNameHintsOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InlineParameterNameHintsOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            InlineParameterNameHintsOptions.Enabled);
    }
}
