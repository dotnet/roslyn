// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CommentSelection;
using Microsoft.CodeAnalysis.Host.Mef;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ExternalAccess.FSharp.Classification;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Internal.Classification;
#else
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Classification;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Classification;
#endif

[Shared]
[ExportLanguageService(typeof(ICommentSelectionService), LanguageNames.FSharp)]
internal class FSharpCommentSelectionService : ICommentSelectionService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public FSharpCommentSelectionService()
    {
    }

    public CommentSelectionInfo GetInfo()
        => new(
            supportsSingleLineComment: true,
            supportsBlockComment: true,
            singleLineCommentString: "//",
            blockCommentStartString: "(*",
            blockCommentEndString: "*)");
}
