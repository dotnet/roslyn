// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.DocumentationComments;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.CSharp.DocumentationComments;

internal static class OmniSharpDocCommentConverter
{
    public static SyntaxNode ConvertToRegularComments(SyntaxNode node, Project project, CancellationToken cancellationToken)
    {
        var formattingService = project.GetRequiredLanguageService<IDocumentationCommentFormattingService>();
        return DocCommentConverter.ConvertToRegularComments(node, formattingService, cancellationToken);
    }
}
