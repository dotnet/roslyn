// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.Designers.Imaging
{
    internal static class IProjectImageProviderFactory
    {
        public static IProjectImageProvider Create()
        {
            return Mock.Of<IProjectImageProvider>();
        }

        public static IProjectImageProvider ImplementGetProjectImage(Func<string, ProjectImageMoniker> action)
        {
            var mock = new Mock<IProjectImageProvider>();
            mock.Setup(p => p.GetProjectImage(It.IsAny<string>()))
                .Returns(action);

            return mock.Object;
        }

        public static IProjectImageProvider ImplementGetProjectImage(string key, ProjectImageMoniker moniker)
        {
            return IProjectImageProviderFactory.ImplementGetProjectImage((string k) => {

                if (k == key)
                {
                    return moniker;
                }

                return null;
            });
        }
    }
}
