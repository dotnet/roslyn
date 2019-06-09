// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    internal interface IMoveTypeService : ILanguageService
    {
        Task<ImmutableArray<CodeAction>> GetRefactoringAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken);

        Task<Solution> GetModifiedSolutionAsync(Document document, TextSpan textSpan, MoveTypeOperationKind operationKind, CancellationToken cancellationToken);
    }
}
