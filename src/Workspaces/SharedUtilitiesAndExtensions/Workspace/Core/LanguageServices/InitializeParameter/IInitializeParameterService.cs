// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.InitializeParameter;

internal interface IInitializeParameterService : ILanguageService
{
    void InsertStatement(
        SyntaxEditor editor, SyntaxNode functionDeclaration, bool returnsVoid, SyntaxNode? statementToAddAfter, SyntaxNode statement);

    void AddAssignment(
        SyntaxNode constructorDeclaration,
        IBlockOperation? blockStatement,
        IParameterSymbol parameter,
        ISymbol fieldOrProperty,
        SyntaxEditor editor);

    SyntaxNode GetBody(SyntaxNode methodNode);
}
