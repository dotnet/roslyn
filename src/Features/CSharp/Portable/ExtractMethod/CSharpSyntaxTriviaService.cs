// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
