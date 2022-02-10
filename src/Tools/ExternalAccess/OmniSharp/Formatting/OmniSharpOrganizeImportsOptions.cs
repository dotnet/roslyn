// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.OrganizeImports;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Formatting
{
    internal readonly record struct OmniSharpOrganizeImportsOptions(
        bool PlaceSystemNamespaceFirst,
        bool SeparateImportDirectiveGroups,
        string NewLine)
    {
        internal OrganizeImportsOptions ToOrganizeImportsOptions()
            => new(
                PlaceSystemNamespaceFirst,
                SeparateImportDirectiveGroups,
                NewLine);
    }
}
