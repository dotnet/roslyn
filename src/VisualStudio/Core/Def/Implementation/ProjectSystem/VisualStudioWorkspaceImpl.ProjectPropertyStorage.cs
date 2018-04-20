// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class VisualStudioWorkspaceImpl
    {
        private abstract class ProjectPropertyStorage
        {
            public static ProjectPropertyStorage Create(Project project, IServiceProvider serviceProvider)
            {
                var solution = (IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
                solution.GetProjectOfUniqueName(project.UniqueName, out var hierarchy);

                return hierarchy.IsCapabilityMatch("CPS")
                    ? new BuildPropertyStorage((IVsBuildPropertyStorage)hierarchy)
                    : new PerConfigurationPropertyStorage(project.ConfigurationManager) as ProjectPropertyStorage;
            }

            public abstract void SetProperty(string buildPropertyName, string configurationPropertyName, string value);

            private sealed class BuildPropertyStorage : ProjectPropertyStorage
            {
                private readonly IVsBuildPropertyStorage propertyStorage;

                public BuildPropertyStorage(IVsBuildPropertyStorage propertyStorage)
                    => this.propertyStorage = propertyStorage;

                public override void SetProperty(string buildPropertyName, string configurationPropertyName, string value)
                {
                    propertyStorage.SetPropertyValue(buildPropertyName, null, (uint)_PersistStorageType.PST_PROJECT_FILE, value);
                }
            }

            private sealed class PerConfigurationPropertyStorage : ProjectPropertyStorage
            {
                private readonly ConfigurationManager configurationManager;

                public PerConfigurationPropertyStorage(ConfigurationManager configurationManager)
                    => this.configurationManager = configurationManager;

                public override void SetProperty(string buildPropertyName, string configurationPropertyName, string value)
                {
                    foreach (Configuration configuration in configurationManager)
                    {
                        configuration.Properties.Item(configurationPropertyName).Value = value;
                    }
                }
            }
        }
    }
}
