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

namespace Microsoft.CodeAnalysis.InlineHints
{
    internal static class InlineHintsOptions
    {
        public static readonly PerLanguageOption2<bool> EnabledForParameters =
            new(nameof(InlineHintsOptions),
                nameof(EnabledForParameters),
                defaultValue: false,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.EnabledForParameters"));
    }

    [ExportOptionProvider, Shared]
    internal sealed class InlineHintsOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InlineHintsOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            InlineHintsOptions.EnabledForParameters);
    }
}
