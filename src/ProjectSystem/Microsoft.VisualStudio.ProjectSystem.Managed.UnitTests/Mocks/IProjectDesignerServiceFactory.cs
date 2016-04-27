// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.Designers
{
    internal static class IProjectDesignerServiceFactory
    {
        public static IProjectDesignerService Create()
        {
            return Mock.Of<IProjectDesignerService>();
        }

        public static IProjectDesignerService ImplementSupportsProjectDesigner(Func<bool> action)
        {
            var mock = new Mock<IProjectDesignerService>();

            mock.SetupGet(f => f.SupportsProjectDesigner)
                .Returns(action);

            return mock.Object;
        }

        public static IProjectDesignerService ImplementShowProjectDesignerAsync(Action action)
        {
            var mock = new Mock<IProjectDesignerService>();
            mock.Setup(s => s.ShowProjectDesignerAsync())
                .ReturnsAsync(action);

            return mock.Object;

        }
    }
}
