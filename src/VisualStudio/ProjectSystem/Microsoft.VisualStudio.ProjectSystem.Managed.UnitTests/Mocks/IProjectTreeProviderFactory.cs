// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.Designers
{
    internal class IProjectTreeProviderFactory
    {
        public static IProjectTreeProvider Create()
        {
            var mock = new Mock<IProjectTreeProvider>();

            return mock.Object;
        }
    }
}
