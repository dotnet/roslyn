// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.OrganizeImports;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Formatting
{
    internal readonly struct OmniSharpOrganizeImportsOptionsWrapper
    {
        internal readonly OrganizeImportsOptions UnderlyingObject;

        private OmniSharpOrganizeImportsOptionsWrapper(OrganizeImportsOptions underlyingObject)
        {
            UnderlyingObject = underlyingObject;
        }

        public OmniSharpOrganizeImportsOptionsWrapper(
            bool placeSystemNamespaceFirst,
            bool separateImportDirectiveGroups,
            string newLine) : this(new OrganizeImportsOptions(
                placeSystemNamespaceFirst,
                separateImportDirectiveGroups,
                newLine))
        {
        }
    }
}
