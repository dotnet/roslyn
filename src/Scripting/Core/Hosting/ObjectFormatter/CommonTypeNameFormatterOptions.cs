// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    public struct CommonTypeNameFormatterOptions
    {
        public bool UseHexadecimalArrayBounds { get; }
        public bool ShowNamespaces { get; }

        public CommonTypeNameFormatterOptions(bool useHexadecimalArrayBounds, bool showNamespaces)
        {
            UseHexadecimalArrayBounds = useHexadecimalArrayBounds;
            ShowNamespaces = showNamespaces;
        }
    }
}