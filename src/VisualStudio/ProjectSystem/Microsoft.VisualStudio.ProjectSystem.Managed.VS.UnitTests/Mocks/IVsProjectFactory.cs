// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsProjectFactory
    {
        public static void ImplementOpenItemWithSpecific(this IVsProject4 project, Guid editorType, Guid logicalView, int hr)
        {
            IVsWindowFrame frame;
            var mock = Mock.Get(project);
            mock.Setup(h => h.OpenItemWithSpecific(It.IsAny<uint>(), It.IsAny<uint>(), ref editorType, It.IsAny<string>(), ref logicalView, It.IsAny<IntPtr>(), out frame))
                .Returns(hr);
        }

        public static void ImplementOpenItemWithSpecific(this IVsProject4 project, Guid editorType, Guid logicalView, IVsWindowFrame frame)
        {
            var mock = Mock.Get(project);
            mock.Setup(h => h.OpenItemWithSpecific(It.IsAny<uint>(), It.IsAny<uint>(), ref editorType, It.IsAny<string>(), ref logicalView, It.IsAny<IntPtr>(), out frame))
                .Returns(0);
        }
    }
}
