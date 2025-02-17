// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.DocumentationComments;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot
{
    internal enum CopilotDocumentationCommentTagType
    {
        Summary = DocumentationCommentTagType.Summary,
        TypeParam = DocumentationCommentTagType.TypeParam,
        Param = DocumentationCommentTagType.Param,
        Returns = DocumentationCommentTagType.Returns,
        Exception = DocumentationCommentTagType.Exception,
    }
}
