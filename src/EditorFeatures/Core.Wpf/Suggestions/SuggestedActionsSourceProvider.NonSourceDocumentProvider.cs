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
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    internal partial class SuggestedActionsSourceProvider
    {
        /// <summary>
        /// <see cref="ISuggestedActionsSourceProvider"/> for non-source documents,
        /// i.e. <see cref="AdditionalDocument"/> and <see cref="AnalyzerConfigDocument"/>.
        /// </summary>
        [Export(typeof(ISuggestedActionsSourceProvider))]
        [Export(typeof(NonSourceDocumentProvider))]
        // ContentType("text") requires DeferCreation(IsRoslynPackageLoadedOption.OptionName).
        // See https://github.com/dotnet/roslyn/issues/62877#issuecomment-1271493105 for more details.
        // TODO: Uncomment the below attribute, tracked with https://github.com/dotnet/roslyn/issues/64567
        // [ContentType("text")]
        [DeferCreation(OptionName = NonSourceDocumentProviderEditorOption.OptionName)]
        [Name("Roslyn Code Fix for Non-Source Documents")]
        [Order]
        [SuggestedActionPriority(DefaultOrderings.Highest)]
        [SuggestedActionPriority(DefaultOrderings.Default)]
        [SuggestedActionPriority(DefaultOrderings.Lowest)]
        private sealed class NonSourceDocumentProvider : SuggestedActionsSourceProvider
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public NonSourceDocumentProvider(
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
