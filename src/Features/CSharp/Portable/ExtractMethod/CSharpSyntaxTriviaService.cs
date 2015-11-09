// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod
{
    internal class CSharpSyntaxTriviaService : AbstractSyntaxTriviaService
    {
        public CSharpSyntaxTriviaService(HostLanguageServices provider)
            : base(provider.GetService<ISyntaxFactsService>(), (int)SyntaxKind.EndOfLineTrivia)
        {
        }
    }
}
