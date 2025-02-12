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

        public async Task<CopilotGenerateDocumentationCommentProvider> CreateProviderAsync(ITextView textView, CancellationToken cancellationToken)
        {
            var provider = textView.Properties.GetOrCreateSingletonProperty(typeof(CopilotGenerateDocumentationCommentProvider),
                () => new CopilotGenerateDocumentationCommentProvider(_threadingContext));
            await provider!.InitializeAsync(textView, _suggestionServiceBase, cancellationToken).ConfigureAwait(false);

            return provider;
        }
    }
}
