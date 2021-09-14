// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServices;

#if CODE_STYLE
using Microsoft.CodeAnalysis.Internal.Editing;
#else
using Microsoft.CodeAnalysis.Editing;
#endif

namespace Microsoft.CodeAnalysis.CSharp.LanguageServices
{
    internal class CSharpFileBanner : AbstractFileBanner
    {
        public static readonly IFileBanner Instance = new CSharpFileBanner();

        protected CSharpFileBanner()
        {
        }

        protected override ISyntaxFacts SyntaxFacts => CSharpSyntaxFacts.Instance;

        protected override IDocumentationCommentService DocumentationCommentService => CSharpDocumentationCommentService.Instance;
    }
}
