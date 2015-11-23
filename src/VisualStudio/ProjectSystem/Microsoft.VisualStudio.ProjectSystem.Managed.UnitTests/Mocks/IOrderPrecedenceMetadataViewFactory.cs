// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.Utilities
{
    internal static class IOrderPrecedenceMetadataViewFactory
    {
        public static IOrderPrecedenceMetadataView Create(string appliesTo, int orderPrecedence = 0)
        {
            var mock = new Mock<IOrderPrecedenceMetadataView>();
            mock.SetupGet(v => v.AppliesTo)
                .Returns(appliesTo);

            mock.SetupGet(v => v.OrderPrecedence)
                .Returns(orderPrecedence);

            return mock.Object;
        }
    }
}
