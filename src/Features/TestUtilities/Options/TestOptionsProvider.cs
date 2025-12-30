// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities;

internal static class TestOptionsProvider
{
    internal sealed class Provider<TOptions>(TOptions options) : OptionsProvider<TOptions>
    {
        public async ValueTask<TOptions> GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => options;
    }

    public static OptionsProvider<TOptions> Create<TOptions>(TOptions options)
        => new Provider<TOptions>(options);
}
