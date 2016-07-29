// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateDefaultConstructors
{
    internal interface IGenerateDefaultConstructorsService : ILanguageService
    {
        Task<ImmutableArray<CodeAction>> GenerateDefaultConstructorsAsync(
            Document document, TextSpan textSpan, CancellationToken cancellationToken);
    }
}