// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Rename;

internal interface IRenameIssuesService : ILanguageService
{
    bool CheckLanguageSpecificIssues(
        SemanticModel semantic, ISymbol symbol, SyntaxToken triggerToken, [NotNullWhen(true)] out string? langError);

    bool CheckDeclarationConflict(
        ISymbol symbol, string newName, [NotNullWhen(true)] out string? message);
}
