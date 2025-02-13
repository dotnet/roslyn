// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentationComments;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot
{
    internal enum CopilotDocumentationCommentTagType
    {
        Summary = DocumentationCommentTagType.Summary,
        Param = DocumentationCommentTagType.Param,
        Returns = DocumentationCommentTagType.Returns,
        Exception = DocumentationCommentTagType.Exception,
    }
}
