// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IUnconfiguredProjectVsServicesFactory
    {
        public static IUnconfiguredProjectVsServices Create()
        {
            return Mock.Of<IUnconfiguredProjectVsServices>();
        }

        public static IUnconfiguredProjectVsServices ImplementHierarchy(Func<IVsHierarchy> action)
        {
            var mock = new Mock<IUnconfiguredProjectVsServices>();
            mock.SetupGet(h => h.Hierarchy)
                .Returns(action);

            return mock.Object;
        }
    }
}
