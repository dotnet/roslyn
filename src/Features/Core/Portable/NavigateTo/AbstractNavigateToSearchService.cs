// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService : INavigateToSearchService
    {
        public async Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentAsync(
            Document document, string searchPattern, CancellationToken cancellationToken)
        {
            var session = await GetRemoteHostSessionAsync(document.Project, cancellationToken).ConfigureAwait(false);
            using (session)
            {
                if (session == null)
                {
                    return await SearchDocumentInCurrentProcessAsync(
                        document, searchPattern, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return await SearchDocumentInRemoteProcessAsync(
                        session, document, searchPattern, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public async Task<ImmutableArray<INavigateToSearchResult>> SearchProjectAsync(
            Project project, string searchPattern, CancellationToken cancellationToken)
        {
            var session  = await GetRemoteHostSessionAsync(project, cancellationToken).ConfigureAwait(false);
            using (session)
            {
                if (session == null)
                {
                    return await SearchProjectInCurrentProcessAsync(
                        project, searchPattern, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return await SearchProjectInRemoteProcessAsync(
                        session, project, searchPattern, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private static readonly bool s_log = false;
        private static readonly object s_logGate = new object();

        public static void Log(string text)
        {
            if (!s_log)
            {
                return;
            }

            lock (s_logGate)
            {
                IOUtilities.PerformIO(() =>
                {
                    File.AppendAllText(@"c:\temp\navtolog.txt", text + "\r\n");
                });
            }
        }
    }
}