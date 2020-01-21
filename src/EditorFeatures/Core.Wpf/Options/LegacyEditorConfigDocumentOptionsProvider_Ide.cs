// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.CodingConventions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    internal sealed partial class LegacyEditorConfigDocumentOptionsProvider
    {
        /// <summary>
        /// This maps <see cref="CodingConventionsChangedEventArgs"/> instances to sets of projects which were updated
        /// in response to an event notification.
        /// </summary>
        /// <remarks>
        /// <para>The coding conventions library creates one instance of
        /// <see cref="CodingConventionsChangedEventArgs"/> in response to a notification from the file system (through
        /// file watcher APIs), and then dispatches this instance to all <see cref="ICodingConventionContext"/> that
        /// need to be updated in response to the change. Since <see cref="HandleCodingConventionsChangedAsync"/>
        /// updates a full project, and a project can have multiple documents that each have their own
        /// <see cref="ICodingConventionContext"/>, the sets in this map avoid refreshing projects multiple times in
        /// response to the same underlying file system event.</para>
        /// 
        /// <para>Since the event notifications from the coding conventions library are asynchronous, uses of the values
        /// stored in this map need to be synchronized by a <see langword="lock"/> construct.</para>
        /// </remarks>
        private static readonly ConditionalWeakTable<CodingConventionsChangedEventArgs, HashSet<ProjectId>> s_projectNotifications =
            new ConditionalWeakTable<CodingConventionsChangedEventArgs, HashSet<ProjectId>>();

        partial void OnCodingConventionContextCreated(DocumentId documentId, ICodingConventionContext context)
        {
            context.CodingConventionsChangedAsync += (sender, e) => HandleCodingConventionsChangedAsync(documentId, sender, e);
        }

        private Task HandleCodingConventionsChangedAsync(DocumentId documentId, object sender, CodingConventionsChangedEventArgs e)
        {
            var projectId = documentId.ProjectId;
            var projectsAlreadyNotified = s_projectNotifications.GetOrCreateValue(e);
            lock (projectsAlreadyNotified)
            {
                if (!projectsAlreadyNotified.Add(projectId))
                {
                    return Task.CompletedTask;
                }
            }

            var diagnosticAnalyzerService = ((IMefHostExportProvider)_workspace.Services.HostServices).GetExports<IDiagnosticAnalyzerService>().Single().Value;
            var foregroundNotificationService = ((IMefHostExportProvider)_workspace.Services.HostServices).GetExports<IForegroundNotificationService>().Single().Value;
            foregroundNotificationService.RegisterNotification(UpdateProject, _listener.BeginAsyncOperation(nameof(HandleCodingConventionsChangedAsync)), CancellationToken.None);

            return Task.CompletedTask;

            void UpdateProject()
            {
                if (!_workspace.CurrentSolution.ContainsProject(projectId))
                {
                    // The project or solution was closed before running this update.
                    return;
                }

                // Send a notification that project options have changed. This ensures the options used by commands,
                // e.g. Format Document, are correct following a change to .editorconfig. Unlike a change to compilation
                // or parse options, this does not discard the syntax and semantics already gathered for the solution.
                _workspace.OnProjectOptionsChanged(projectId);

                // Request diagnostics be run on the project again.
                diagnosticAnalyzerService.Reanalyze(_workspace, SpecializedCollections.SingletonEnumerable(projectId));
            }
        }
    }
}
