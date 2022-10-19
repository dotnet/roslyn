// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.ExternalAccess.LegacyCodeAnalysis.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.Suppression;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.ExternalAccess.LegacyCodeAnalysis
{
    [Export(typeof(ILegacyCodeAnalysisVisualStudioSuppressionFixServiceAccessor))]
    [Shared]
    internal sealed class LegacyCodeAnalysisVisualStudioSuppressionFixServiceAccessor
        : ILegacyCodeAnalysisVisualStudioSuppressionFixServiceAccessor
    {
        private readonly VisualStudioWorkspace _workspace;
        private readonly IVisualStudioSuppressionFixService _implementation;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LegacyCodeAnalysisVisualStudioSuppressionFixServiceAccessor(
            VisualStudioWorkspace workspace,
            IVisualStudioSuppressionFixService implementation)
        {
            _workspace = workspace;
            _implementation = implementation;
        }

        public bool AddSuppressions(IVsHierarchy? projectHierarchy)
        {
            var errorReportingService = _workspace.Services.GetRequiredService<IErrorReportingService>();

            try
            {
                return _implementation.AddSuppressions(projectHierarchy);
            }
            catch (Exception ex)
            {
                errorReportingService.ShowGlobalErrorInfo(
                    string.Format(ServicesVSResources.Error_updating_suppressions_0, ex.Message),
                    TelemetryFeatureName.LegacySuppressionFix,
                    ex,
                    new InfoBarUI(
                        WorkspacesResources.Show_Stack_Trace,
                        InfoBarUI.UIKind.HyperLink,
                        () => errorReportingService.ShowDetailedErrorInfo(ex), closeAfterAction: true));
                return false;
            }
        }

        public bool AddSuppressions(bool selectedErrorListEntriesOnly, bool suppressInSource, IVsHierarchy? projectHierarchy)
        {
            var errorReportingService = _workspace.Services.GetRequiredService<IErrorReportingService>();

            try
            {
                return _implementation.AddSuppressions(selectedErrorListEntriesOnly, suppressInSource, projectHierarchy);
            }
            catch (Exception ex)
            {
                errorReportingService.ShowGlobalErrorInfo(
                    message: string.Format(ServicesVSResources.Error_updating_suppressions_0, ex.Message),
                    TelemetryFeatureName.LegacySuppressionFix,
                    ex,
                    new InfoBarUI(
                        WorkspacesResources.Show_Stack_Trace,
                        InfoBarUI.UIKind.HyperLink,
                        () => errorReportingService.ShowDetailedErrorInfo(ex), closeAfterAction: true));
                return false;
            }
        }

        public bool RemoveSuppressions(bool selectedErrorListEntriesOnly, IVsHierarchy? projectHierarchy)
        {
            var errorReportingService = _workspace.Services.GetRequiredService<IErrorReportingService>();

            try
            {
                return _implementation.RemoveSuppressions(selectedErrorListEntriesOnly, projectHierarchy);
            }
            catch (Exception ex)
            {
                errorReportingService.ShowGlobalErrorInfo(
                    message: string.Format(ServicesVSResources.Error_updating_suppressions_0, ex.Message),
                    TelemetryFeatureName.LegacySuppressionFix,
                    ex,
                    new InfoBarUI(
                        WorkspacesResources.Show_Stack_Trace,
                        InfoBarUI.UIKind.HyperLink,
                        () => errorReportingService.ShowDetailedErrorInfo(ex), closeAfterAction: true));
                return false;
            }
        }
    }
}
