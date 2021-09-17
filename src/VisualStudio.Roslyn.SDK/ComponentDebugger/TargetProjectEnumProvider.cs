// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Roslyn.ComponentDebugger
{
    [ExportDynamicEnumValuesProvider(nameof(TargetProjectEnumProvider))]
    [AppliesTo(Constants.RoslynComponentCapability)]
    public class TargetProjectEnumProvider : IDynamicEnumValuesProvider
    {
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly LaunchSettingsManager _launchSettingsManager;

        [ImportingConstructor]
        public TargetProjectEnumProvider(UnconfiguredProject unconfiguredProject, LaunchSettingsManager launchSettingsManager)
        {
            _unconfiguredProject = unconfiguredProject;
            _launchSettingsManager = launchSettingsManager;
        }

        public async Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair>? options)
        {
            // get the targets for this project
            var projects = await _unconfiguredProject.GetComponentReferencingProjectsAsync().ConfigureAwait(false);

            // convert to display values of friendly name + relative path
            var displayValues = projects.Select(p => (Path.GetFileNameWithoutExtension(p.FullPath), _unconfiguredProject.MakeRelative(p.FullPath))).ToImmutableArray();

            return new TargetProjectEnumValuesGenerator(displayValues);
        }

        private class TargetProjectEnumValuesGenerator : IDynamicEnumValuesGenerator
        {
            private readonly ImmutableArray<(string display, string path)> _referencingProjects;

            public bool AllowCustomValues => false;

            public TargetProjectEnumValuesGenerator(ImmutableArray<(string display, string path)> referencingProjects)
            {
                _referencingProjects = referencingProjects;
            }

            public Task<ICollection<IEnumValue>> GetListedValuesAsync()
            {
                var values = _referencingProjects.Select(p => new PageEnumValue(new EnumValue() { DisplayName = p.display, Name = p.path})).Cast<IEnumValue>().ToImmutableArray();
                return Task.FromResult<ICollection<IEnumValue>>(values);
            }

            /// <summary>
            /// The user can't add arbitrary projects from the UI, so this is unsupported
            /// </summary>
            public Task<IEnumValue?> TryCreateEnumValueAsync(string userSuppliedValue) => Task.FromResult<IEnumValue?>(null);
        }
    }
}
