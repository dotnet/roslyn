// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.PullMemberUp), Shared]
    internal class CSharpPullMemberUpCodeRefactoringProvider : AbstractPullMemberUpRefactoringProvider
    {
        /// <summary>
        /// Test purpose only.
        /// </summary>
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0034:Exported parts should have [ImportingConstructor]", Justification = "Used incorrectly by tests")]
        public CSharpPullMemberUpCodeRefactoringProvider(IPullMemberUpOptionsService service) : base(service)
        {
        }

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpPullMemberUpCodeRefactoringProvider() : this(service: null)
        {
        }

        protected override async Task<SyntaxNode> GetSelectedNodeAsync(CodeRefactoringContext context)
        {
            var selectedNode = await NodeSelectionHelpers.GetSelectedDeclarationOrVariableAsync(context).ConfigureAwait(false);
            if (selectedNode is BaseFieldDeclarationSyntax { Declaration: { Variables: { Count: 1 } } } baseFieldDeclarationNode)
            {
                return baseFieldDeclarationNode.Declaration.Variables[0];
            }

            return selectedNode;
        }
    }
}
