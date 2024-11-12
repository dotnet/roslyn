// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal interface OptionsProvider<TOptions>
{
    ValueTask<TOptions> GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken);
}

internal static class OptionsProvider
{
    private sealed class OptionsReaderProvider<TOptions>(IOptionsReader optionsReader, Func<IOptionsReader, string, TOptions> reader) : OptionsProvider<TOptions>
    {
        public ValueTask<TOptions> GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(reader(optionsReader, languageServices.Language));
    }

    public static OptionsProvider<TOptions> GetProvider<TOptions>(this IOptionsReader optionsReader, Func<IOptionsReader, string, TOptions> reader)
        => new OptionsReaderProvider<TOptions>(optionsReader, reader);
}
