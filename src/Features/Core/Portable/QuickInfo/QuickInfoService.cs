// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    /// <summary>
    /// A service that is used to determine the appropriate quick info for a position in a document.
    /// </summary>
    internal abstract class QuickInfoService : ILanguageService
    {
        /// <summary>
        /// Gets the <see cref="QuickInfoService"/> associated with the document.
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public static QuickInfoService GetService(Document document)
        {
            var workspace = document.Project.Solution.Workspace;
            return workspace.Services.GetLanguageServices(document.Project.Language).GetService<QuickInfoService>();
        }

        /// <summary>
        /// Gets the <see cref="QuickInfoItem"/> associated with position in the document.
        /// </summary>
        public abstract Task<QuickInfoItem> GetQuickInfoAsync(Document document, int position, CancellationToken cancellationToken);
    }
}