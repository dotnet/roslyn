// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.MoveDeclarationNearReference
{
    internal interface IMoveDeclarationNearReferenceService: ILanguageService
    {
        Task MoveDeclarationNearReferenceAsync(SyntaxNode statement, Document document, SyntaxEditor editor, CancellationToken cancellationToken);
    }
}
