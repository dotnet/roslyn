// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Editing;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#else
using Microsoft.CodeAnalysis.Options;
#endif

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    /// <summary>
    /// Document-specific options for controlling the code produced by code generation.
    /// </summary>
    internal abstract class CodeGenerationPreferences
    {
        protected readonly OptionSet Options;

        public CodeGenerationPreferences(OptionSet options)
        {
            Options = options;
        }

        public abstract string Language { get; }
        public abstract bool PlaceImportsInsideNamespaces { get; }

        public bool PlaceSystemNamespaceFirst
            => Options.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, Language);

#if !CODE_STYLE
        public abstract CodeGenerationOptions GetOptions(CodeGenerationContext context);

        public static async Task<CodeGenerationPreferences> FromDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            var parseOptions = document.Project.ParseOptions;
            Contract.ThrowIfNull(parseOptions);

            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var codeGenerationService = document.GetRequiredLanguageService<ICodeGenerationService>();
            return codeGenerationService.GetPreferences(parseOptions, documentOptions);
        }
#endif
    }
}
