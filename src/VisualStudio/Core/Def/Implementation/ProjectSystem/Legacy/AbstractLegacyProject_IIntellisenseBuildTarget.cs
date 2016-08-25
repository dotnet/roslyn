// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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

        // set default to true so that we maintain old behavior when project system doesn't
        // implement IIntellisenseBuildTarget
        private bool _intellisenseBuildSucceeded = true;
        private string _intellisenseBuildFailureReason = null;

        protected override bool LastDesignTimeBuildSucceeded => _intellisenseBuildSucceeded;

        public void SetIntellisenseBuildResult(bool succeeded, string reason)
        {
            // set intellisense related info
            _intellisenseBuildSucceeded = succeeded;
            _intellisenseBuildFailureReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

            UpdateHostDiagnostics(succeeded, reason);

            if (PushingChangesToWorkspaceHosts)
            {
                // set workspace reference info
                ProjectTracker.NotifyWorkspaceHosts(host => (host as IVisualStudioWorkspaceHost2)?.OnHasAllInformation(Id, succeeded));
            }
        }

        private void UpdateHostDiagnostics(bool succeeded, string reason)
        {
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
        }

        private DiagnosticData CreateIntellisenseBuildFailureDiagnostic(string reason)
        {
            // log intellisense build failure
            Logger.Log(FunctionId.IntellisenseBuild_Failed, KeyValueLogMessage.Create(m => m["Reason"] = reason ?? string.Empty));

            return new DiagnosticData(
                IDEDiagnosticIds.IntellisenseBuildFailedDiagnosticId,
                FeaturesResources.Roslyn_HostError,
                ServicesVSResources.Error_encountered_while_loading_the_project_Some_project_features_such_as_full_solution_analysis_for_the_failed_project_and_projects_that_depend_on_it_have_been_disabled,
                ServicesVSResources.ResourceManager.GetString(nameof(ServicesVSResources.Error_encountered_while_loading_the_project_Some_project_features_such_as_full_solution_analysis_for_the_failed_project_and_projects_that_depend_on_it_have_been_disabled), CodeAnalysis.Diagnostics.Extensions.s_USCultureInfo),
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                warningLevel: 0,
                workspace: Workspace,
                projectId: Id,
                title: ServicesVSResources.Project_loading_failed,
                description: GetDescription(reason),
                helpLink: "http://go.microsoft.com/fwlink/p/?LinkID=734719");
        }

        private string GetDescription(string reason)
        {
            var logFilePath = $"{Path.GetTempPath()}\\{Path.GetFileNameWithoutExtension(this.ProjectFilePath)}_*.designtime.log";

            var logFileDescription = string.Format(ServicesVSResources.To_see_what_caused_the_issue_please_try_below_1_Close_Visual_Studio_2_Open_a_Visual_Studio_Developer_Command_Prompt_3_Set_environment_variable_TraceDesignTime_to_true_set_TraceDesignTime_true_4_Delete_vs_directory_suo_file_5_Restart_VS_from_the_command_prompt_you_set_the_environment_varaible_devenv_6_Open_the_solution_7_Check_0_and_look_for_the_failed_tasks_FAILED, logFilePath);
            if (string.IsNullOrWhiteSpace(reason))
            {
                return logFileDescription;
            }

            return string.Join(Environment.NewLine, logFileDescription, string.Empty, ServicesVSResources.Additional_information_colon, reason);
        }
    }
}
