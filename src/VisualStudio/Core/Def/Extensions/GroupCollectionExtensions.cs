// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Microsoft.VisualStudio.LanguageServices.Extensions;

internal static class GroupCollectionExtensions
{
    public static bool TryGetValue(this GroupCollection groupCollection, string key, [NotNullWhen(true)] out Group? group)
    {
        group = groupCollection[key];
        if (group == null)
        {
            group = null;
            return false;
        }

        if (group.Captures.Count == 0 && group.Length == 0)
        {
            group = null;
            return false;
        }

        return true;
    }
}
