// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.GenerateFromMembers.GenerateConstructor
{
    internal interface IGenerateConstructorService : ILanguageService
    {
        Task<IGenerateConstructorResult> GenerateConstructorAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken);
    }
}
