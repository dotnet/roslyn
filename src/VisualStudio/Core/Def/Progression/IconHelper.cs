// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression;

internal static class IconHelper
{
    private static string GetIconName(string groupName, string itemName)
        => string.Format("Microsoft.VisualStudio.{0}.{1}", groupName, itemName);

    public static string GetIconName(string groupName, Accessibility symbolAccessibility)
    {
        switch (symbolAccessibility)
        {
            case Accessibility.Private:
                return GetIconName(groupName, "Private");

            case Accessibility.Protected:
            case Accessibility.ProtectedAndInternal:
            case Accessibility.ProtectedOrInternal:
                return GetIconName(groupName, "Protected");

            case Accessibility.Internal:
                return GetIconName(groupName, "Internal");

            case Accessibility.Public:
            case Accessibility.NotApplicable:
                return GetIconName(groupName, "Public");

            default:
                throw new ArgumentException();
        }
    }
}
