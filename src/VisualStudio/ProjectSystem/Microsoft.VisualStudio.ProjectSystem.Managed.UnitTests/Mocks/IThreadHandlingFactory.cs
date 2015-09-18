// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IThreadHandlingFactory
    {
        public static IThreadHandling Create()
        {
            var mock = new Mock<IThreadHandling>();

            return mock.Object;
        }
    }
}
