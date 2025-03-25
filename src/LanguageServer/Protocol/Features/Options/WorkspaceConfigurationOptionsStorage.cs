// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Host;

internal static class WorkspaceConfigurationOptionsStorage
{
    public static WorkspaceConfigurationOptions GetWorkspaceConfigurationOptions(this IGlobalOptionService globalOptions)
        => new(
            SourceGeneratorExecution: globalOptions.GetOption(SourceGeneratorExecution),
            ValidateCompilationTrackerStates: globalOptions.GetOption(ValidateCompilationTrackerStates));

    public static readonly Option2<bool> ValidateCompilationTrackerStates = new(
        "dotnet_validate_compilation_tracker_states", WorkspaceConfigurationOptions.Default.ValidateCompilationTrackerStates);

    public static readonly Option2<SourceGeneratorExecutionPreference> SourceGeneratorExecution = new(
        "dotnet_source_generator_execution",
        defaultValue: SourceGeneratorExecutionPreference.Balanced,
        isEditorConfigOption: true,
        serializer: new EditorConfigValueSerializer<SourceGeneratorExecutionPreference>(
            s => SourceGeneratorExecutionPreferenceUtilities.Parse(s, SourceGeneratorExecutionPreference.Balanced),
            SourceGeneratorExecutionPreferenceUtilities.GetEditorConfigString));
}
