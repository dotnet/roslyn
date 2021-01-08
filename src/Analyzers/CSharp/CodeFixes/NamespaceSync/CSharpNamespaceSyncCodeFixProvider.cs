// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.NamespaceSync;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.NamespaceSync
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.SyncNamespace), Shared]
    internal sealed class CSharpNamespaceSyncCodeFixProvider : AbstractSyncNamespaceCodeFixProvider
    {

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpNamespaceSyncCodeFixProvider()
        {
        }

        protected override CodeAction CreateCodeAction(Func<CancellationToken, Task<Solution>> createChangedSolution)
            => new MyCodeAction(createChangedSolution);

        private sealed class MyCodeAction : CustomCodeActions.SolutionChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Solution>> createChangedSolution)
                : base(CSharpAnalyzersResources.Namespace_does_not_match_folder_structure, createChangedSolution)
            {

            }
        }
    }
}
