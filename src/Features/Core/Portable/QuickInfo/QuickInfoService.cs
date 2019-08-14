// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    /// <summary>
    /// A service that is used to determine the appropriate quick info for a position in a document.
    /// </summary>
    public abstract class QuickInfoService : ILanguageService
    {
        /// <summary>
        /// Gets the appropriate <see cref="QuickInfoService"/> for the specified document.
        /// </summary>
        public static QuickInfoService GetService(Document document)
            => document?.GetLanguageService<QuickInfoService>();

        /// <summary>
        /// Gets the <see cref="QuickInfoItem"/> associated with position in the document.
        /// </summary>
        public virtual Task<QuickInfoItem> GetQuickInfoAsync(
            Document document,
            int position,
            CancellationToken cancellationToken = default)
        {
            return SpecializedTasks.Default<QuickInfoItem>();
        }
    }
}
