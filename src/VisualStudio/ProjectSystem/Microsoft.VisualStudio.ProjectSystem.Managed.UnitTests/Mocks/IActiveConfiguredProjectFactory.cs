// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IActiveConfiguredProjectFactory
    {
        public static ActiveConfiguredProject<T> ImplementValue<T>(Func<T> action)
        {
            var mock = new Mock<ActiveConfiguredProject<T>>();

            mock.SetupGet(p => p.Value)
                .Returns(action);

            return mock.Object;
        }
    }
}
