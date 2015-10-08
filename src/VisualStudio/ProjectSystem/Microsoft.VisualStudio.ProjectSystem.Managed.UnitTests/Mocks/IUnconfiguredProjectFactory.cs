// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IUnconfiguredProjectFactory
    {
        public static UnconfiguredProject Create(object hostObject = null, IEnumerable<string> capabilities = null)
        {
            capabilities = capabilities ?? Enumerable.Empty<string>();

            var threadingPolicy = IThreadHandlingFactory.Create();

            var projectServices = new Mock<IProjectServices>();
            projectServices.Setup(u => u.ThreadingPolicy)
                               .Returns(threadingPolicy);

            var service = new Mock<ProjectService>();
            service.Setup(p => p.Services)
                   .Returns(projectServices.Object);

            var unconfiguredProjectServices = new Mock<IUnconfiguredProjectServices>();
            unconfiguredProjectServices.Setup(u => u.HostObject)
                                       .Returns(hostObject);

            var unconfiguredProject = new Mock<UnconfiguredProject>();
            unconfiguredProject.Setup(u => u.ProjectService)
                               .Returns(service.Object);

            unconfiguredProject.Setup(u => u.Services)
                               .Returns(unconfiguredProjectServices.Object);

            unconfiguredProject.Setup(u => u.IsProjectCapabilityPresent(It.IsIn(capabilities)))
                               .Returns(true);

            var comparable = Mock.Of<IComparable>();

            unconfiguredProject.Setup(u => u.Version)
                               .Returns(comparable);

            return unconfiguredProject.Object;
        }
    }
}
