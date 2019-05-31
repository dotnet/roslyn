// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Editor.CSharp.SplitStringLiteral
{
    internal class SplitStringLiteralOptions
    {
        public static PerLanguageOption<bool> Enabled =
            new PerLanguageOption<bool>(nameof(SplitStringLiteralOptions), nameof(Enabled), defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.SplitStringLiterals"));
    }

    [ExportOptionProvider, Shared]
    internal class SplitStringLiteralOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        public SplitStringLiteralOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            SplitStringLiteralOptions.Enabled);
    }
}
