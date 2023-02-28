// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    internal static class FindUsagesPresentationOptionsStorage
    {
        /// <summary>
        /// Used to store the user's explicit 'grouping priority' for the 'Definition' column.
        /// We store this because we'll disable this grouping sometimes (i.e. for GoToImplementation),
        /// and we want to restore the value back to its original state when the user does the
        /// next FindReferences call.
        /// </summary>
        public static readonly Option2<int> DefinitionGroupingPriority = new(
            "dotnet_find_usage_definition_grouping_priority", defaultValue: -1);
    }
}
