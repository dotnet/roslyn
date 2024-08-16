// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// Enum which represents the origin of an item.
    /// </summary>
    internal enum VSInternalItemOrigin
    {
        /// <summary>
        /// The entry is contained in exact code.
        /// </summary>
        Exact = 0,

        /// <summary>
        /// The entry is contained in metadata generated from exact information.
        /// </summary>
        ExactMetadata = 1000,

        /// <summary>
        /// The entry is contained in indexed code.
        /// </summary>
        Indexed = 2000,

        /// <summary>
        /// The entry is contained in indexed code in the repo where the request originated.
        /// </summary>
        IndexedInRepo = 2100,

        /// <summary>
        /// The entry is contained in indexed code in the same organization but different repo where the request originated.
        /// </summary>
        IndexedInOrganization = 2200,

        /// <summary>
        /// The entry is contained in indexed code in a different organization and repo where request originated.
        /// </summary>
        IndexedInThirdParty = 2300,

        /// <summary>
        /// The entry is of lesser quality than all other choices.
        /// </summary>
        Other = int.MaxValue,
    }
}
