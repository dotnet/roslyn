// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IUnconfiguredProjectCommonServicesFactory
    {
        public static IUnconfiguredProjectCommonServices Create()
        {
            var mock = new Mock<IUnconfiguredProjectCommonServices>();

            return mock.Object;
        }

        public static IUnconfiguredProjectCommonServices Create(IThreadHandling threadingPolicy = null, ConfiguredProject configuredProject = null, ProjectProperties projectProperties = null)
        {
            var mock = new Mock<IUnconfiguredProjectCommonServices>();

            if (threadingPolicy != null)
                mock.Setup(s => s.ThreadingPolicy)
                    .Returns(threadingPolicy);

            if (configuredProject != null)
                mock.Setup(s => s.ActiveConfiguredProject)
                    .Returns(configuredProject);

            if (projectProperties != null)
                mock.Setup(s => s.ActiveConfiguredProjectProperties)
                    .Returns(projectProperties);

            return mock.Object;
        }
    }
}
