// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Snippets
{
    [Export(typeof(IWpfTextViewConnectionListener))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal class CSharpCreateServicesOnTextViewConnection : AbstractCreateServicesOnTextViewConnection
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpCreateServicesOnTextViewConnection(
            VisualStudioWorkspace workspace,
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider listenerProvider,
            IThreadingContext threadingContext)
            : base(workspace, globalOptions, listenerProvider, threadingContext, LanguageNames.CSharp)
        {
        }

        protected override async Task InitializeServiceForProjectWithOpenedDocumentAsync(Project project)
        {
            // Only pre-populate cache if import completion is enabled
            if (GlobalOptions.GetOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp) != true)
                return;

            var service = project.GetRequiredLanguageService<ITypeImportCompletionService>();
            service.QueueCacheWarmUpTask(project);
            await ExtensionMethodImportCompletionHelper.WarmUpCacheAsync(project, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
