// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
        public static readonly PerLanguageOption2<bool> ShowSignatureHelp = new PerLanguageOption2<bool>(nameof(SignatureHelpOptions), nameof(ShowSignatureHelp), defaultValue: true);
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
