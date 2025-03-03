// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember;

internal interface IGenerateDeconstructMemberService : ILanguageService
{
    Task<ImmutableArray<CodeAction>> GenerateDeconstructMethodAsync(
        Document document, SyntaxNode targetVariables, INamedTypeSymbol typeToGenerateIn, CancellationToken cancellationToken);
}
