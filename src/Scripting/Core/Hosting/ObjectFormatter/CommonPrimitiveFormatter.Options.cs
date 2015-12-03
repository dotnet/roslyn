// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    public abstract partial class CommonPrimitiveFormatter
    {
        public struct Options
        {
            public readonly bool UseHexadecimalNumbers;
            public readonly bool IncludeCharacterCodePoints;
            public readonly bool OmitStringQuotes;

            public Options(bool useHexadecimalNumbers, bool includeCodePoints, bool omitStringQuotes)
            {
                UseHexadecimalNumbers = useHexadecimalNumbers;
                IncludeCharacterCodePoints = includeCodePoints;
                OmitStringQuotes = omitStringQuotes;
            }
        }
    }
}
