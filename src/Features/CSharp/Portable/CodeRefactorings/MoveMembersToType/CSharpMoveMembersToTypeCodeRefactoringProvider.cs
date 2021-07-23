// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MoveMembersToType;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.MoveMembersToType
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = "[WIP] Move Members To Type"), Shared]
    internal class CSharpMoveMembersToTypeCodeRefactoringProvider : AbstractMoveMembersToTypeRefactoringProvider
    {
        /// <summary>
        /// Test purpose only.
        /// </summary>
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0034:Exported parts should have [ImportingConstructor]", Justification = "Used incorrectly by tests")]
        public CSharpMoveMembersToTypeCodeRefactoringProvider(IMoveMembersToTypeOptionsService service) : base(service)
        {
        }

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpMoveMembersToTypeCodeRefactoringProvider() : this(service: null)
        {
        }

        protected override Task<SyntaxNode> GetSelectedNodeAsync(CodeRefactoringContext context)
            => NodeSelectionHelpers.GetSelectedDeclarationOrVariableAsync(context);
    }
}
