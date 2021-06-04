// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal readonly struct RazorDocumentOptions
    {
        public readonly bool UseTabs;
        public readonly int TabSize;

        public RazorDocumentOptions(bool useTabs, int tabSize)
        {
            UseTabs = useTabs;
            TabSize = tabSize;
        }
    }
}
