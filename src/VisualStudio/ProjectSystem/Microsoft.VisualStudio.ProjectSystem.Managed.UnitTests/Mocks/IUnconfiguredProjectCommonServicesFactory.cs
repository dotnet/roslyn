// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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

        public static IUnconfiguredProjectCommonServices Create(IProjectFeatures features, IThreadHandling threadingPolicy, ConfiguredProject configuredProject, ProjectProperties projectProperties)
        {
            var mock = new Mock<IUnconfiguredProjectCommonServices>();
            mock.Setup(s => s.Features)
                .Returns(features);

            mock.Setup(s => s.ThreadingPolicy)
                .Returns(threadingPolicy);

            mock.Setup(s => s.ActiveConfiguredProject)
                .Returns(configuredProject);

            mock.Setup(s => s.ActiveConfiguredProjectProperties)
                .Returns(projectProperties);

            return mock.Object;
        }
    }
}
