// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.VisualStudio.LanguageServices.ColorSchemes
{
    internal partial class ColorSchemeApplier
    {
        private class RegistryItem
        {
            public string SectionName { get; }
            public string ValueName => "Data";
            public byte[] ValueData { get; }

            public RegistryItem(string sectionName, byte[] valueData)
            {
                SectionName = sectionName;
                ValueData = valueData;
            }
        }
    }
}
