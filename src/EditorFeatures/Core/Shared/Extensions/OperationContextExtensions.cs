// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class OperationContextExtensions
    {
        /// <summary>
        /// Wait until given document is fully loaded and then add scope
        /// </summary>
        public static IUIThreadOperationScope WaitForDocumentAndAddScope(
            this IUIThreadOperationContext context, Document document, bool allowCancellation, string description)
        {
            return WaitAndAddScope(context, document.Project.Solution, document.Name, allowCancellation, description);
        }

        public static IUIThreadOperationScope WaitForProjectAndAddScope(
            this IUIThreadOperationContext context, Project project, bool allowCancellation, string description)
        {
            return WaitAndAddScope(context, project.Solution, project.Name, allowCancellation, description);
        }

        private static IUIThreadOperationScope WaitAndAddScope(
            this IUIThreadOperationContext context, Solution solution, string name, bool allowCancellation, string description)
        {
            IUIThreadOperationScope scope = null;

            try
            {
                // partial mode is always cancellable
                var title = string.Format(EditorFeaturesResources.Operation_is_not_ready_for_0_yet_see_task_center_for_more_detail, name);
                scope = context.AddScope(allowCancellation: true, title);

                var service = solution.Workspace.Services.GetService<ISolutionStatusService>();
                if (service != null)
                {
                    // UIThreadOperation always called from UI thread.
                    // at some point, we should consider moving all Wait to IThreadingContext Run 
                    service.WaitForAsync(solution, scope.Context.UserCancellationToken).Wait(context.UserCancellationToken);
                }

                // now set description and cancellation as asked
                scope.AllowCancellation = allowCancellation;
                scope.Description = description;

                return scope;
            }
            catch (OperationCanceledException)
            {
                // cancellation is exepcted. dispose scopes and rethrow exceptions
                scope?.Dispose();

                throw;
            }
            catch (Exception ex) when (FatalError.ReportWithoutCrash(ex))
            {
                // NFW exception and then clean up
                scope?.Dispose();

                throw;
            }
        }
    }
}
