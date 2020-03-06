// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.FileHeaders;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp.FileHeaders
{
    /// <summary>
    /// Implements a code fix for file header diagnostics.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpFileHeaderCodeFixProvider))]
    [Shared]
    internal class CSharpFileHeaderCodeFixProvider : AbstractFileHeaderCodeFixProvider
    {
        [ImportingConstructor]
        public CSharpFileHeaderCodeFixProvider()
        {
        }

        protected override AbstractFileHeaderHelper FileHeaderHelper => CSharpFileHeaderHelper.Instance;

        protected override ISyntaxFacts SyntaxFacts => CSharpSyntaxFacts.Instance;

        protected override ISyntaxKinds SyntaxKinds => CSharpSyntaxKinds.Instance;
    }
}
