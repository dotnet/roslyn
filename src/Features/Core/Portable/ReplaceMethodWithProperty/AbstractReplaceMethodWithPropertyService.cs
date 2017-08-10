// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;

namespace Microsoft.CodeAnalysis.ReplaceMethodWithProperty
{
    internal abstract class AbstractReplaceMethodWithPropertyService
    {
        protected static string GetWarning(GetAndSetMethods getAndSetMethods)
        {
            if (OverridesMetadataSymbol(getAndSetMethods.GetMethod) ||
                OverridesMetadataSymbol(getAndSetMethods.SetMethod))
            {
                return FeaturesResources.Warning_Method_overrides_symbol_from_metadata;
            }

            return null;
        }

        private static bool OverridesMetadataSymbol(IMethodSymbol method)
        {
            for (var current = method; current != null; current = current.OverriddenMethod)
            {
                if (current.Locations.Any(loc => loc.IsInMetadata))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
