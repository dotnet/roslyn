// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.CodingConventions;

namespace Microsoft.CodeAnalysis.ExternalAccess.Format.Options
{
    internal sealed partial class CachingEditorConfigDocumentOptionsProvider : IDocumentOptionsProvider
    {
        private readonly object _gate = new object();

        /// <summary>
        /// The map of cached contexts for currently open documents. Should only be accessed if holding a monitor lock
        /// on <see cref="_gate"/>.
        /// </summary>
        private readonly Dictionary<DocumentId, Task<DocumentOptions>> _openDocumentOptions = new Dictionary<DocumentId, Task<DocumentOptions>>();
        private readonly EditorConfigOptionsApplier _optionsApplier = new EditorConfigOptionsApplier();

        private readonly Workspace _workspace;
        private readonly ICodingConventionsManager _codingConventionsManager;

        internal CachingEditorConfigDocumentOptionsProvider(Workspace workspace, ICodingConventionsManager codingConventionsManager)
        {
            _workspace = workspace;
            _codingConventionsManager = codingConventionsManager;
        }

        public async Task<IDocumentOptions> GetOptionsForDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            Task<DocumentOptions> optionsTask;

            lock (_gate)
            {
                _openDocumentOptions.TryGetValue(document.Id, out optionsTask);
            }

            if (optionsTask is object)
            {
                // The file is open, let's reuse our cached data for that file. That task might be running, but we don't want to await
                // it as awaiting it wouldn't respect the cancellation of our caller. By creating a trivial continuation like this
                // that uses eager cancellation, if the cancellationToken is cancelled our await will end early.
                var cancellableOptionsTask = optionsTask.ContinueWith(
                    t => t.Result,
                    cancellationToken,
                    TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);

                var options = await cancellableOptionsTask.ConfigureAwait(false);
                return options;
            }
            else
            {
                var path = document.FilePath;

                // The file might not actually have a path yet, if it's a file being proposed by a code action. We'll guess a file path to use
                if (path == null)
                {
                    if (document.Name != null && document.Project.FilePath != null)
                    {
                        path = Path.Combine(Path.GetDirectoryName(document.Project.FilePath), document.Name);
                    }
                    else
                    {
                        // Really no idea where this is going, so bail
                        return null;
                    }
                }

                // We don't have anything cached, so we'll just get it now lazily and not hold onto it. The workspace layer will ensure
                // that we maintain snapshot rules for the document options. We'll also run it on the thread pool
                // as in some builds the ICodingConventionsManager captures the thread pool.
                var conventionsAsync = Task.Run(() => GetConventionContextAsync(document, path, document.Project.Language, cancellationToken));

                var options = await conventionsAsync.ConfigureAwait(false);
                return options;
            }
        }

        private async Task<DocumentOptions> GetConventionContextAsync(Document document, string path, string language, CancellationToken cancellationToken)
        {
            var context = await IOUtilities.PerformIOAsync<ICodingConventionContext>(
                async () =>
                {
                    var analyzerConfig = await document.GetAnalyzerOptionsAsync(cancellationToken).ConfigureAwait(false);
                    return new AnalyzerConfigCodingConventionsContext(analyzerConfig.IsEmpty ? null : analyzerConfig);
                },
                defaultValue: EmptyCodingConventionContext.Instance).ConfigureAwait(false);
            var options = _optionsApplier.ApplyConventions(_workspace.Options, context.CurrentConventions, language);
            return new DocumentOptions(options);
        }
    }
}
