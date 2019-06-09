// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    internal static class BraceCompletionOptions
    {
        // This is serialized by the Visual Studio-specific LanguageSettingsPersister
        public static readonly PerLanguageOption<bool> Enable = new PerLanguageOption<bool>(nameof(BraceCompletionOptions), nameof(Enable), defaultValue: true);
    }

    [ExportOptionProvider, Shared]
    internal class BraceCompletionOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        public BraceCompletionOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            BraceCompletionOptions.Enable);
    }
}
