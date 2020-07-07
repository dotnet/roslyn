// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.ExtractClass;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ExtractClass
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(PredefinedCodeRefactoringProviderNames.ExtractClass)), Shared]
    internal class CSharpExtractClassCodeRefactoringProvider : AbstractExtractClassRefactoringProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpExtractClassCodeRefactoringProvider()
            : base()
        {
        }

        /// <summary>
        /// Test purpose only.
        /// </summary>
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0034:Exported parts should have [ImportingConstructor]", Justification = "Used incorrectly by tests")]
        internal CSharpExtractClassCodeRefactoringProvider(IExtractClassOptionsService optionsService)
            : base(optionsService)
        {
        }

        protected override Task<SyntaxNode> GetSelectedNodeAsync(CodeRefactoringContext context) => CSharpPullMemberUpCodeRefactoringProvider.GetSelectedDeclarationOrVariableAsync(context);
    }
}
