// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsWindowFrameFactory
    {
        public static IVsWindowFrame ImplementShow(Func<int> action)
        {
            var mock = new Mock<IVsWindowFrame>();
            mock.Setup(h => h.Show())
                .Returns(action());

            return mock.Object;
        }
    }
}
