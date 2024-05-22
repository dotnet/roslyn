// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking;

internal static class RenameTrackingOptionsStorage
{
    public static readonly Option2<bool> RenameTracking = new("dotnet_enable_rename_tracking", defaultValue: true);

    public static readonly PerLanguageOption2<bool> RenameTrackingPreview = new("dotnet_show_preview_for_rename_tracking", defaultValue: true);
}
