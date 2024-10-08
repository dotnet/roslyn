// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.CSharp.LanguageService;

internal class CSharpFileBannerFacts : AbstractFileBannerFacts
{
    public static readonly IFileBannerFacts Instance = new CSharpFileBannerFacts();

    protected CSharpFileBannerFacts()
    {
    }

    protected override ISyntaxFacts SyntaxFacts => CSharpSyntaxFacts.Instance;

    protected override IDocumentationCommentService DocumentationCommentService => CSharpDocumentationCommentService.Instance;
}
