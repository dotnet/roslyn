// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Threading;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IConfiguredProjectFactory
    {
        public static ConfiguredProject Create()
        {
            // Need the following services/members filled out 
            // so that StronglyTypedPropertyAccess doesn't throw

            var threadingPolicy = new Mock<IThreadHandling>();
            
            var projectServices = new Mock<IProjectServices>();
            projectServices.Setup(u => u.ThreadingPolicy)
                               .Returns(threadingPolicy.Object);

            var service = new Mock<ProjectService>();
            service.Setup(u => u.Services)
                               .Returns(projectServices.Object);

            var unconfiguredProject = new Mock<UnconfiguredProject>();
            unconfiguredProject.Setup(u => u.ProjectService)
                               .Returns(service.Object);

            var catalog = new Mock<IPropertyPagesCatalogProvider>();

            var services = new Mock<IConfiguredProjectServices>();
            services.Setup(s => s.PropertyPagesCatalog)
                    .Returns(catalog.Object);

            var mock = new Mock<ConfiguredProject>();
            mock.Setup(p => p.Services)
                .Returns(services.Object);

            mock.Setup(p => p.UnconfiguredProject)
                .Returns(unconfiguredProject.Object);

            return mock.Object;
        }
    }
}
