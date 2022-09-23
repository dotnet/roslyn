// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.CSharp.BraceCompletion
{
    internal abstract class AbstractCSharpBraceCompletionService : AbstractBraceCompletionService
    {
        protected override ISyntaxFacts SyntaxFacts => CSharpSyntaxFacts.Instance;
    }
}
