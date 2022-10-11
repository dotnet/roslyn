// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tags;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    internal partial class SuggestedActionsSourceProvider
    {
        [Export(typeof(ISuggestedActionsSourceProvider))]
        [Export(typeof(SourceDocumentProvider))]
        [ContentType(ContentTypeNames.RoslynContentType)]
        [ContentType(ContentTypeNames.XamlContentType)]
        [Name("Roslyn Code Fix")]
        [Order]
        [SuggestedActionPriority(DefaultOrderings.Highest)]
        [SuggestedActionPriority(DefaultOrderings.Default)]
        [SuggestedActionPriority(DefaultOrderings.Lowest)]
        private sealed class SourceDocumentProvider : SuggestedActionsSourceProvider
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public SourceDocumentProvider(
                IThreadingContext threadingContext,
                ICodeRefactoringService codeRefactoringService,
                ICodeFixService codeFixService,
                ICodeActionEditHandlerService editHandler,
                IUIThreadOperationExecutor uiThreadOperationExecutor,
                ISuggestedActionCategoryRegistryService suggestedActionCategoryRegistry,
                IAsynchronousOperationListenerProvider listenerProvider,
                IGlobalOptionService globalOptions,
                [ImportMany] IEnumerable<Lazy<IImageIdService, OrderableMetadata>> imageIdServices)
                : base(threadingContext, codeRefactoringService, codeFixService, editHandler, uiThreadOperationExecutor,
                       suggestedActionCategoryRegistry, listenerProvider, globalOptions, imageIdServices)
            {
            }
        }
    }
}
