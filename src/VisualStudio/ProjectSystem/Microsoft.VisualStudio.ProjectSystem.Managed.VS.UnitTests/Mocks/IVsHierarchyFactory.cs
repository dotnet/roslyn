// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsHierarchyFactory
    {
        public static IVsHierarchy Create()
        {
            var mock = new Mock<IVsProject4>();

            return mock.As<IVsHierarchy>().Object;
        }

        public static IVsHierarchy ImplementGetProperty(int hr)
        {
            object result;
            var mock = new Mock<IVsHierarchy>();
            mock.Setup(h => h.GetProperty(It.IsAny<uint>(), It.IsAny<int>(), out result))
                .Returns(hr);

            mock.As<IVsProject4>();

            return mock.Object;
        }

        public static IVsHierarchy ImplementGetProperty(object result)
        {
            var mock = new Mock<IVsHierarchy>();
            mock.Setup(h => h.GetProperty(It.IsAny<uint>(), It.IsAny<int>(), out result))
                .Returns(0);

            mock.As<IVsProject4>();

            return mock.Object;
        }
    }
}
