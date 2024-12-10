// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;

namespace Microsoft.CodeAnalysis.Test.Utilities;

internal static class NamingStyleTestUtilities
{
    public static string Inspect(this NamingStylePreferences preferences, string[]? excludeNodes = null)
    {
        var xml = preferences.CreateXElement();

        // filter out insignificant elements:
        var elementsToRemove = new List<XElement>();
        foreach (var element in xml.DescendantsAndSelf())
        {
            if (excludeNodes != null && excludeNodes.Contains(element.Name.LocalName))
            {
                elementsToRemove.Add(element);
            }
        }

        foreach (var element in elementsToRemove)
        {
            element.Remove();
        }

        // replaces GUIDs with unique deterministic numbers:
        var ordinal = 0;
        var guidMap = new Dictionary<Guid, int>();
        foreach (var element in xml.DescendantsAndSelf())
        {
            foreach (var attribute in element.Attributes())
            {
                if (Guid.TryParse(attribute.Value, out var guid))
                {
                    if (!guidMap.TryGetValue(guid, out var existingOrdinal))
                    {
                        existingOrdinal = ordinal++;
                    }

                    attribute.Value = existingOrdinal.ToString();
                }
            }
        }

        return xml.ToString();
    }
}
