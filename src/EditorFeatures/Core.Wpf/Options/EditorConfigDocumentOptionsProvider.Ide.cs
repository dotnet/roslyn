// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.CodingConventions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    internal sealed partial class EditorConfigDocumentOptionsProvider
    {
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

            var foregroundNotificationService = ((IMefHostExportProvider)_workspace.Services.HostServices).GetExports<IForegroundNotificationService>().Single().Value;
            foregroundNotificationService.RegisterNotification(UpdateProject, _listener.BeginAsyncOperation(nameof(HandleCodingConventionsChangedAsync)), CancellationToken.None);

            return Task.CompletedTask;

            void UpdateProject()
            {
                var compilationOptions = _workspace.CurrentSolution.GetProject(projectId)?.CompilationOptions;
                if (compilationOptions == null)
                {
                    return;
                }

                var parseOptions = _workspace.CurrentSolution.GetProject(projectId).ParseOptions;
                if (parseOptions.Features.TryGetValue("EditorConfigWorkaround", out _))
                {
                    parseOptions = parseOptions.WithFeatures(parseOptions.Features.Where(feature => feature.Key != "EditorConfigWorkaround"));
                }
                else
                {
                    parseOptions = parseOptions.WithFeatures(parseOptions.Features.Concat(new[] { KeyValuePair.Create("EditorConfigWorkaround", "true") }));
                }

                _workspace.OnCompilationOptionsChanged(projectId, compilationOptions);
                _workspace.OnParseOptionsChanged(projectId, parseOptions);
            }
        }
    }
}
