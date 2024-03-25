// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MoveStaticMembers;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.MoveStaticMembers;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.MoveStaticMembers), Shared]
internal class CSharpMoveStaticMembersRefactoringProvider : AbstractMoveStaticMembersRefactoringProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpMoveStaticMembersRefactoringProvider() : base()
    {
    }

    protected override Task<ImmutableArray<SyntaxNode>> GetSelectedNodesAsync(CodeRefactoringContext context)
        => NodeSelectionHelpers.GetSelectedDeclarationsOrVariablesAsync(context);
}
