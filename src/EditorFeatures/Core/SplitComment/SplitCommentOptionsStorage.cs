// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Implementation.SplitComment;

internal sealed class SplitCommentOptionsStorage
{
    public static PerLanguageOption2<bool> Enabled =
       new("dotnet_split_comments", defaultValue: true);
}
