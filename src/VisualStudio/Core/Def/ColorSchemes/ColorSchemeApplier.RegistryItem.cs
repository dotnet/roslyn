// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ColorSchemes;

internal partial class ColorSchemeApplier
{
    private class RegistryItem
    {
        public string SectionName { get; }
        public static string ValueName => "Data";
        public byte[] ValueData { get; }

        public RegistryItem(string sectionName, byte[] valueData)
        {
            SectionName = sectionName;
            ValueData = valueData;
        }
    }
}
