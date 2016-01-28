// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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

        public static void ImplementGetGuid(this IVsHierarchy hierarchy, VsHierarchyPropID propId, int hr)
        {
            Guid result;
            var mock = Mock.Get<IVsHierarchy>(hierarchy);
            mock.Setup(h => h.GetGuidProperty(It.IsAny<uint>(), It.Is<int>(p => p == (int)propId), out result))
                .Returns(hr);
        }

        public static void ImplementGetGuid(this IVsHierarchy hierarchy, VsHierarchyPropID propId, Guid result)
        {
            var mock = Mock.Get<IVsHierarchy>(hierarchy);
            mock.Setup(h => h.GetGuidProperty(It.IsAny<uint>(), It.Is<int>(p => p == (int)propId), out result))
                .Returns(0);
        }

        public static void ImplementGetProperty(this IVsHierarchy hierarchy, VsHierarchyPropID propId, int hr)
        {
            object result;
            var mock = Mock.Get<IVsHierarchy>(hierarchy);
            mock.Setup(h => h.GetProperty(It.IsAny<uint>(), It.Is<int>(p => p == (int)propId), out result))
                .Returns(hr);
        }

        public static void ImplementGetProperty(this IVsHierarchy hierarchy, VsHierarchyPropID propId, object result)
        {
            var mock = Mock.Get<IVsHierarchy>(hierarchy);
            mock.Setup(h => h.GetProperty(It.IsAny<uint>(), It.Is<int>(p => p == (int)propId), out result))
                .Returns(0);
        }
    }
}
