// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Language.Suggestions;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.DocumentationComments
{
    [Export(typeof(CopilotGenerateDocumentationCommentManager))]
    internal class CopilotGenerateDocumentationCommentManager
    {
        private readonly SuggestionServiceBase _suggestionServiceBase;
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CopilotGenerateDocumentationCommentManager(SuggestionServiceBase suggestionServiceBase, IThreadingContext threadingContext)
        {
            _suggestionServiceBase = suggestionServiceBase;
            _threadingContext = threadingContext;
        }

        public async Task<CopilotGenerateDocumentationCommentProvider?> CreateProviderAsync(Document document, ITextView textView, CancellationToken cancellationToken)
        {
            var copilotService = await IsGenerateDocumentationAvailableAsync(document, cancellationToken).ConfigureAwait(false);

            if (copilotService is null)
            {
                return null;
            }

            var provider = textView.Properties.GetOrCreateSingletonProperty(typeof(CopilotGenerateDocumentationCommentProvider),
                () => new CopilotGenerateDocumentationCommentProvider(_threadingContext, copilotService));

            await provider!.InitializeAsync(textView, _suggestionServiceBase, cancellationToken).ConfigureAwait(false);

            return provider;
        }

        public static async Task<ICopilotCodeAnalysisService?> IsGenerateDocumentationAvailableAsync(Document document, CancellationToken cancellationToken)
        {
            // Bailing out if copilot is not available or the option is not enabled.
            if (document.GetLanguageService<ICopilotOptionsService>() is not { } copilotOptionService ||
                !await copilotOptionService.IsGenerateDocumentationCommentOptionEnabledAsync().ConfigureAwait(false))
            {
                return null;
            }

            if (document.GetLanguageService<ICopilotCodeAnalysisService>() is not { } copilotService ||
                    await copilotService.IsAvailableAsync(cancellationToken).ConfigureAwait(false) is false)
            {
                return null;
            }

            return copilotService;
        }
    }
}
