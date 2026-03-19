// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Utilities;

internal abstract class ProjectPropertyStorage
{
    // For CPS projects, we prefer to use IVsBuildPropertyStorage since that allows us to easily modify the project
    // properties independent of configuration. However this is problematic in the legacy project system, where setting
    // the build property would indeed change its value, however that change would only be visible after the project is
    // unloaded and reloaded. The language service would not be updated (the error we're trying to fix would still
    // be visible) and any checkbox in the project properties dialog would not be updated. The language service issue
    // could be worked around by using VSProject.Refresh, but that does not fix the project properties dialog. Therefore
    // for the legacy project system, we choose to use ConfigurationManager to iterate & update each configuration,
    // which works correctly, even though it is a little less convinient because it creates a more verbose project file
    // (although we're dealing with a legacy project file... it's already verbose anyway).

    // It's important to note that the property name may differ in these two implementations. The build property name
    // corresponds to the name of the property in the project file (for example LangVersion), whereas the configuration
    // property name comes from an interface such as CSharpProjectConfigurationProperties3 (for example LanguageVersion).

    public static ProjectPropertyStorage Create(Project project, IServiceProvider serviceProvider)
    {
        var solution = (IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
        solution.GetProjectOfUniqueName(project.UniqueName, out var hierarchy);

        return hierarchy.IsCapabilityMatch("CPS")
            ? new BuildPropertyStorage((IVsBuildPropertyStorage)hierarchy)
            : new PerConfigurationPropertyStorage(project.ConfigurationManager);
    }

    public abstract void SetProperty(string buildPropertyName, string configurationPropertyName, string value);

    public void SetProperty(string buildPropertyName, string configurationPropertyName, bool value)
        => SetProperty(buildPropertyName, configurationPropertyName, value.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());

    private sealed class BuildPropertyStorage : ProjectPropertyStorage
    {
        private readonly IVsBuildPropertyStorage _propertyStorage;

        public BuildPropertyStorage(IVsBuildPropertyStorage propertyStorage)
            => _propertyStorage = propertyStorage;

        public override void SetProperty(string buildPropertyName, string configurationPropertyName, string value)
            => _propertyStorage.SetPropertyValue(buildPropertyName, null, (uint)_PersistStorageType.PST_PROJECT_FILE, value);
    }

    private sealed class PerConfigurationPropertyStorage : ProjectPropertyStorage
    {
        private readonly ConfigurationManager _configurationManager;

        public PerConfigurationPropertyStorage(ConfigurationManager configurationManager)
            => _configurationManager = configurationManager;

        public override void SetProperty(string buildPropertyName, string configurationPropertyName, string value)
        {
            foreach (Configuration configuration in _configurationManager)
            {
                configuration.Properties.Item(configurationPropertyName).Value = value;
            }
        }
    }
}
