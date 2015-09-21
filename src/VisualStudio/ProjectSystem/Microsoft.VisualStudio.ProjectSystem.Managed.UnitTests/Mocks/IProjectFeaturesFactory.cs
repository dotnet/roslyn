// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectFeaturesFactory
    {
        public static IProjectFeatures Create()
        {
            var mock = new Mock<IProjectFeatures>();

            return mock.Object;
        }

        public static IProjectFeatures ImplementSupportsProjectDesigner(Func<bool> action)
        {
            var mock = new Mock<IProjectFeatures>();

            mock.SetupGet(f => f.SupportsProjectDesigner)
                .Returns(action);

            return mock.Object;
        }
    }
}
