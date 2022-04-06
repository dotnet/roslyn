// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal interface ISyntaxFormattingService : ISyntaxFormatting, ILanguageService
    {
    }

    internal abstract partial class SyntaxFormattingOptions
    {
        public static SyntaxFormattingOptions Create(OptionSet options, HostWorkspaceServices services, string language)
        {
            var formattingService = services.GetRequiredLanguageService<ISyntaxFormattingService>(language);
            var configOptions = options.AsAnalyzerConfigOptions(services.GetRequiredService<IOptionService>(), language);
            return formattingService.GetFormattingOptions(configOptions);
        }

        public static async Task<SyntaxFormattingOptions> FromDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            return Create(documentOptions, document.Project.Solution.Workspace.Services, document.Project.Language);
        }
    }
}
