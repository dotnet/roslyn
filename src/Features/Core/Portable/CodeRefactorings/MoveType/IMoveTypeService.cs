// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType;

internal interface IMoveTypeService : ILanguageService
{
    Task<ImmutableArray<CodeAction>> GetRefactoringAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken);

    Task<Solution> GetModifiedSolutionAsync(Document document, TextSpan textSpan, MoveTypeOperationKind operationKind, CancellationToken cancellationToken);
}
