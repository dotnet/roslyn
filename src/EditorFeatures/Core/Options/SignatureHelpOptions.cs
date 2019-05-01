// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    internal static class SignatureHelpOptions
    {
        public static readonly PerLanguageOption<bool> ShowSignatureHelp = new PerLanguageOption<bool>(nameof(SignatureHelpOptions), nameof(ShowSignatureHelp), defaultValue: true);
    }

    [ExportOptionProvider, Shared]
    internal class SignatureHelpOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SignatureHelpOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            SignatureHelpOptions.ShowSignatureHelp);
    }
}
