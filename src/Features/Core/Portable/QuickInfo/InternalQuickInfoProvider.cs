// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    /// <summary>
    /// <see cref="SemanticModel"/> based provider that produces <see cref="QuickInfoItem"/>'s. 
    /// All our internal C# and VB providers are <see cref="InternalQuickInfoProvider"/>'s.
    /// External, non-SemanticModel based providers are <see cref="QuickInfoProvider"/>'s.
    /// Providers are used with some <see cref="QuickInfoService"/> implementations.
    /// </summary>
    internal abstract class InternalQuickInfoProvider
    {
        /// <summary>
        /// Gets the <see cref="QuickInfoItem"/> for the position.
        /// </summary>
        /// <returns>The <see cref="QuickInfoItem"/> or null if no item is available.</returns>
        public abstract Task<QuickInfoItem?> GetQuickInfoAsync(InternalQuickInfoContext context);
    }
}
