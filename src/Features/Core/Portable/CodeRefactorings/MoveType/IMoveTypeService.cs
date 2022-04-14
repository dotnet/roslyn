// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    internal interface IMoveTypeService : ILanguageService
    {
        Task<ImmutableArray<CodeAction>> GetRefactoringAsync(Document document, TextSpan textSpan, SyntaxFormattingOptionsProvider fallbackOptions, CancellationToken cancellationToken);

        Task<Solution> GetModifiedSolutionAsync(Document document, TextSpan textSpan, MoveTypeOperationKind operationKind, SyntaxFormattingOptionsProvider fallbackOptions, CancellationToken cancellationToken);
    }
}
