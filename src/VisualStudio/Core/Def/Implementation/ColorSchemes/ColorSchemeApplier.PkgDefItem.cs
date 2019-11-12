// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.VisualStudio.LanguageServices.ColorSchemes
{
    internal partial class ColorSchemeApplier
    {
        private class PkgDefItem
        {
            public enum PkgDefValueType
            {
                String,
                ExpandSz,
                MultiSz,
                Binary,
                DWord,
                QWord,
            };

            public PkgDefItem(string sectionName, string valueName, object valueData, PkgDefValueType valueType)
            {
                SectionName = sectionName;
                ValueName = valueName;
                ValueData = valueData;
                ValueType = valueType;
            }

            public string SectionName { get; private set; }
            public string ValueName { get; private set; }
            public object ValueData { get; private set; }
            public PkgDefValueType ValueType { get; private set; }
        }
    }
}
