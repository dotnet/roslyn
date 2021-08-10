// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MoveStaticMembers;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.MoveStaticMembers
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.MoveStaticMembers), Shared]
    internal class CSharpMoveStaticMembersRefactoringProvider : AbstractMoveStaticMembersRefactoringProvider
    {
        /// <summary>
        /// Test purpose only.
        /// </summary>
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0034:Exported parts should have [ImportingConstructor]", Justification = "Used incorrectly by tests")]
        public CSharpMoveStaticMembersRefactoringProvider(IMoveStaticMembersOptionsService? service) : base(service)
        {
        }

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpMoveStaticMembersRefactoringProvider() : this(service: null)
        {
        }

        protected override Task<SyntaxNode> GetSelectedNodeAsync(CodeRefactoringContext context)
            => NodeSelectionHelpers.GetSelectedDeclarationOrVariableAsync(context);
    }
}
