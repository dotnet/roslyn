// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Legacy
{
    internal partial class AbstractLegacyProject : IIntellisenseBuildTarget
    {
        private static readonly object s_diagnosticKey = new object();

        void IIntellisenseBuildTarget.SetIntellisenseBuildResult(bool succeeded, string reason)
        {
            //            SetIntellisenseBuildResultAndNotifyWorkspace(succeeded);

            UpdateIntellisenseBuildFailureDiagnostic(succeeded, reason);
        }

        private void UpdateIntellisenseBuildFailureDiagnostic(bool succeeded, string reason)
        {
            /*
            if (!succeeded)
            {
                // report intellisense build failure to error list
                this.HostDiagnosticUpdateSource?.UpdateDiagnosticsForProject(
                    Id, s_diagnosticKey, SpecializedCollections.SingletonEnumerable(CreateIntellisenseBuildFailureDiagnostic(reason)));
            }
            else
            {
                // clear intellisense build failure diagnostic from error list.
                this.HostDiagnosticUpdateSource?.ClearDiagnosticsForProject(Id, s_diagnosticKey);
            }
            */
        }

        private DiagnosticData CreateIntellisenseBuildFailureDiagnostic(string reason)
        {
            // log intellisense build failure
            Logger.Log(FunctionId.IntellisenseBuild_Failed, KeyValueLogMessage.Create(m => m["Reason"] = reason ?? string.Empty));

            return new DiagnosticData(
                id: IDEDiagnosticIds.IntellisenseBuildFailedDiagnosticId,
                category: FeaturesResources.Roslyn_HostError,
                message: ServicesVSResources.Error_encountered_while_loading_the_project_Some_project_features_such_as_full_solution_analysis_for_the_failed_project_and_projects_that_depend_on_it_have_been_disabled,
                enuMessageForBingSearch: ServicesVSResources.ResourceManager.GetString(nameof(ServicesVSResources.Error_encountered_while_loading_the_project_Some_project_features_such_as_full_solution_analysis_for_the_failed_project_and_projects_that_depend_on_it_have_been_disabled), CodeAnalysis.Diagnostics.Extensions.s_USCultureInfo),
                severity: DiagnosticSeverity.Warning,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                warningLevel: 0,
                projectId: VisualStudioProject.Id,
                customTags: ImmutableArray<string>.Empty,
                properties: ImmutableDictionary<string, string>.Empty,
                title: ServicesVSResources.Project_loading_failed,
                description: GetDescription(reason),
                helpLink: "http://go.microsoft.com/fwlink/p/?LinkID=734719");
        }

        private string GetDescription(string reason)
        {
            var logFilePath = $"{Path.GetTempPath()}\\{Path.GetFileNameWithoutExtension(this.VisualStudioProject.FilePath)}_*.designtime.log";

            var logFileDescription = string.Format(ServicesVSResources.To_see_what_caused_the_issue_please_try_below_1_Close_Visual_Studio_long_paragraph_follows, logFilePath);
            if (string.IsNullOrWhiteSpace(reason))
            {
                return logFileDescription;
            }

            return string.Join(Environment.NewLine, logFileDescription, string.Empty, ServicesVSResources.Additional_information_colon, reason);
        }
    }
}
