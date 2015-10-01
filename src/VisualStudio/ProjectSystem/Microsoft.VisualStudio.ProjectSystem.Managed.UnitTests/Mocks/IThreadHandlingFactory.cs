// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IThreadHandlingFactory
    {
        public static IThreadHandling Create()
        {
            var mock = new Mock<IThreadHandling>();
            
            mock.Setup(h => h.ExecuteSynchronously(It.IsAny<Func<Task<string>>>()))
                .Returns<Func<Task<string>>>(f => f().Result);

            return mock.Object;
        }
    }
}
