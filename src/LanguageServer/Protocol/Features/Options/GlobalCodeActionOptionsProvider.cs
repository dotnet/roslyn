// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Options;

internal static class CodeActionOptionsStorage
{
    public static Provider CreateProvider(this IGlobalOptionService globalOptions)
        => new(globalOptions);

    // TODO: we can implement providers directly on IGlobalOptionService once it moves to LSP layer
    public sealed class Provider :
        CodeActionOptionsProvider
    {
        private readonly IGlobalOptionService _globalOptions;

        public Provider(IGlobalOptionService globalOptions)
            => _globalOptions = globalOptions;

        CodeActionOptions CodeActionOptionsProvider.GetOptions(LanguageServices languageServices)
            => _globalOptions.GetCodeActionOptions(languageServices);
    }
}
