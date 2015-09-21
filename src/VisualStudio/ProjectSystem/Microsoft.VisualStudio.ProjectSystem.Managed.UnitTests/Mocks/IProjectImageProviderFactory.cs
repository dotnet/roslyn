// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Designers.Imaging;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.Designers.Imaging
{
    internal static class IProjectImageProviderFactory
    {
        public static IProjectImageProvider Create()
        {
            var mock = new Mock<IProjectImageProvider>();

            return mock.Object;
        }
    }
}
