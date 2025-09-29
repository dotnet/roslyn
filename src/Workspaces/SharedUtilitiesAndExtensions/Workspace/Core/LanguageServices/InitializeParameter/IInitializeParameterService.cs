// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.InitializeParameter;

internal interface IInitializeParameterService : ILanguageService
{
    bool IsThrowNotImplementedProperty(Compilation compilation, IPropertySymbol property, CancellationToken cancellationToken);

    void InsertStatement(
        SyntaxEditor editor, SyntaxNode functionDeclaration, bool returnsVoid, SyntaxNode? statementToAddAfter, SyntaxNode statement);

    Task<Solution> AddAssignmentAsync(
        Document document, IParameterSymbol parameter, ISymbol fieldOrProperty, CancellationToken cancellationToken);

    bool TryGetBlockForSingleParameterInitialization(
        SyntaxNode functionDeclaration,
        SemanticModel semanticModel,
        ISyntaxFactsService syntaxFacts,
        CancellationToken cancellationToken,
        out IBlockOperation? blockStatement);
}
