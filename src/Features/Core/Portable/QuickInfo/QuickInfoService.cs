// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    /// <summary>
    /// A service that is used to determine the appropriate quick info for a position in a document.
    /// </summary>
    internal abstract class QuickInfoService : ILanguageService
    {
        /// <summary>
        /// Gets the appropriate <see cref="QuickInfoService"/> for the specified document.
        /// </summary>
        public static QuickInfoService GetService(Document document)
        {
            return GetService(document.Project.Solution.Workspace, document.Project.Language);
        }

        /// <summary>
        /// Gets the appropriate <see cref="QuickInfoService"/> for the specified Workspace and language.
        /// </summary>
        public static QuickInfoService GetService(Workspace workspace, string language)
        {
            return workspace.Services.GetLanguageServices(language)?.GetService<QuickInfoService>() ?? NoOpService.Instance;
        }

        private class NoOpService : QuickInfoService
        {
            public static readonly NoOpService Instance = new NoOpService();
        }

        /// <summary>
        /// Gets the <see cref="QuickInfoItem"/> associated with position in the document.
        /// </summary>
        public virtual Task<QuickInfoItem> GetQuickInfoAsync(Document document, int position, CancellationToken cancellationToken)
        {
            return EmptyTask;
        }

        private readonly Task<QuickInfoItem> EmptyTask = Task.FromResult(QuickInfoItem.Empty);
    }
}