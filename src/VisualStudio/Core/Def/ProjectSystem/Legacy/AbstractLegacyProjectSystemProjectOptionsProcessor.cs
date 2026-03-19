// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.ProjectSystem.Legacy;

internal abstract class AbstractLegacyProjectSystemProjectOptionsProcessor : ProjectSystemProjectOptionsProcessor
{
    public AbstractLegacyProjectSystemProjectOptionsProcessor(
        ProjectSystemProject project,
        SolutionServices workspaceServices)
        : base(project, workspaceServices)
    {
    }

    public string? ExplicitRuleSetFilePath
    {
        get;
        set
        {
            lock (_gate)
            {
                if (field == value)
                {
                    return;
                }

                field = value;

                UpdateProjectOptions_NoLock();
            }
        }
    }

    protected override string? GetEffectiveRulesetFilePath()
        => ExplicitRuleSetFilePath ?? base.GetEffectiveRulesetFilePath();

    protected override bool ShouldSaveCommandLine(ImmutableArray<string> arguments)
    {
        // Legacy projects require this to be kept as it may be needed if ExplicitRuleSetFilePath is changed
        return true;
    }

    /// <summary>
    /// Called by a derived class to notify that we need to update the settings in the project system for something that will be provided
    /// by either <see cref="ProjectSystemProjectOptionsProcessor.ComputeCompilationOptionsWithHostValues(CompilationOptions, IRuleSetFile)"/>
    /// or <see cref="ProjectSystemProjectOptionsProcessor.ComputeParseOptionsWithHostValues(ParseOptions)"/>.
    /// </summary>
    protected void UpdateProjectForNewHostValues()
    {
        lock (_gate)
        {
            UpdateProjectOptions_NoLock();
        }
    }
}
