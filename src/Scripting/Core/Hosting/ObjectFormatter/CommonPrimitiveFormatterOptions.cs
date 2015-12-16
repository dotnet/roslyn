// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    public struct CommonPrimitiveFormatterOptions
    {
        public bool UseHexadecimalNumbers { get; }
        public bool IncludeCharacterCodePoints { get; }
        public bool OmitStringQuotes { get; }

        public CommonPrimitiveFormatterOptions(bool useHexadecimalNumbers, bool includeCodePoints, bool omitStringQuotes)
        {
            UseHexadecimalNumbers = useHexadecimalNumbers;
            IncludeCharacterCodePoints = includeCodePoints;
            OmitStringQuotes = omitStringQuotes;
        }
    }
}
