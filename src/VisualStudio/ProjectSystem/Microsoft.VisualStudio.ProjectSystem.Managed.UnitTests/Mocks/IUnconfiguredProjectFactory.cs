// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IUnconfiguredProjectFactory
    {
        public static UnconfiguredProject Create()
        {
            var threadingPolicy = IThreadHandlingFactory.Create();

            var projectServices = new Mock<IProjectServices>();
            projectServices.Setup(u => u.ThreadingPolicy)
                               .Returns(threadingPolicy);

            var service = new Mock<ProjectService>();
            service.Setup(u => u.Services)
                               .Returns(projectServices.Object);

            var unconfiguredProject = new Mock<UnconfiguredProject>();
            unconfiguredProject.Setup(u => u.ProjectService)
                               .Returns(service.Object);

            return unconfiguredProject.Object;
        }
    }
}
