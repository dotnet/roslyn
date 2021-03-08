// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Roslyn.ComponentDebugger
{
    [Export]
    public class LaunchSettingsManager
    {
        private readonly UnconfiguredProject _owningProject;
        private readonly IDebugTokenReplacer _tokenReplacer;

        [ImportingConstructor]
        public LaunchSettingsManager(UnconfiguredProject owningProject, IDebugTokenReplacer tokenReplacer)
        {
            _owningProject = owningProject;
            _tokenReplacer = tokenReplacer;
        }

        public async Task<UnconfiguredProject?> TryGetProjectForLaunchAsync(ILaunchProfile? profile)
        {
            UnconfiguredProject? targetProject = null;
            object? value = null;
            profile?.OtherSettings?.TryGetValue(Constants.TargetProjectKeyName, out value);

            if (value is string targetProjectPath)
            {
                // expand any variables in the path, and root it based on this project
                var replacedProjectPath = await _tokenReplacer.ReplaceTokensInStringAsync(targetProjectPath, true).ConfigureAwait(true);
                replacedProjectPath = _owningProject.MakeRooted(replacedProjectPath);

                targetProject = ((IProjectService2)_owningProject.Services.ProjectService).GetLoadedProject(replacedProjectPath);
            }
            return targetProject;
        }

        public void WriteProjectForLaunch(IWritableLaunchProfile profile, UnconfiguredProject targetProject)
        {
            if (profile is null)
            {
                throw new System.ArgumentNullException(nameof(profile));
            }

            if (targetProject is null)
            {
                throw new System.ArgumentNullException(nameof(targetProject));
            }

            var rootedPath = _owningProject.MakeRelative(targetProject.FullPath);
            profile.OtherSettings[Constants.TargetProjectKeyName] = rootedPath;
        }

    }
}
