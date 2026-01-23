// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis.ExternalAccess.FSharp;
using Microsoft.VisualStudio.ExternalAccess.FSharp.Internal;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Completion;
#else
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Completion;
#endif

internal class FSharpFileSystemCompletionHelper
{
    private readonly FileSystemCompletionHelper _fileSystemCompletionHelper;

    public FSharpFileSystemCompletionHelper(
        FSharpGlyph folderGlyph,
        FSharpGlyph fileGlyph,
        ImmutableArray<string> searchPaths,
        string baseDirectoryOpt,
        ImmutableArray<string> allowableExtensions,
        CompletionItemRules itemRules)
    {
        _fileSystemCompletionHelper =
            new FileSystemCompletionHelper(
                FSharpGlyphHelpers.ConvertTo(folderGlyph),
                FSharpGlyphHelpers.ConvertTo(fileGlyph),
                searchPaths,
                baseDirectoryOpt,
                allowableExtensions,
                itemRules);
    }

    public Task<ImmutableArray<CompletionItem>> GetItemsAsync(string directoryPath, CancellationToken cancellationToken)
    {
        return _fileSystemCompletionHelper.GetItemsAsync(directoryPath, cancellationToken);
    }
}
