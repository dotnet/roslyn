// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    public abstract partial class CommonTypeNameFormatter
    {
        public struct Options
        {
            public readonly bool UseHexadecimalArrayBounds;
            public readonly bool ShowNamespaces;

            public Options(bool useHexadecimalArrayBounds, bool showNamespaces)
            {
                UseHexadecimalArrayBounds = useHexadecimalArrayBounds;
                ShowNamespaces = showNamespaces;
            }
        }
    }
}