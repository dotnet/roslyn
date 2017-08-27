// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember
{
    internal interface IGenerateParameterizedMemberService : ILanguageService
    {
        Task<ImmutableArray<CodeAction>> GenerateMethodAsync(Document document, SyntaxNode node, CancellationToken cancellationToken);
    }
}
