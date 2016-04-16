// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateEnumMember
{
    internal interface IGenerateEnumMemberService : ILanguageService
    {
        Task<IEnumerable<CodeAction>> GenerateEnumMemberAsync(Document document, SyntaxNode node, CancellationToken cancellationToken);
    }
}
