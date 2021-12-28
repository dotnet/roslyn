// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.BraceCompletion
{
    internal class BraceCompletionOptions
    {
        public static readonly PerLanguageOption2<bool> AutoFormattingOnCloseBrace = new(
            nameof(BraceCompletionOptions), nameof(AutoFormattingOnCloseBrace), defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Close Brace"));

        [ExportSolutionOptionProvider, Shared]
        internal class BraceCompletionOptionsProvider : IOptionProvider
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public BraceCompletionOptionsProvider()
            {
            }

            public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
                AutoFormattingOnCloseBrace);
        }
    }
}
