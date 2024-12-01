// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.CSharp.BlockCommentEditing;

internal class BlockCommentEditingOptionsStorage
{
    public static readonly PerLanguageOption2<bool> AutoInsertBlockCommentStartString = new("csharp_insert_block_comment_start_string", defaultValue: true);
}
