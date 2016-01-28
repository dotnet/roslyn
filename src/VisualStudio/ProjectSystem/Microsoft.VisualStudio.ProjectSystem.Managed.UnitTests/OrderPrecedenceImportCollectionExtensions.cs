// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.Utilities
{
    internal static class OrderPrecedenceImportCollectionExtensions
    {
        public static void Add<T>(this OrderPrecedenceImportCollection<T> collection, T value, string appliesTo, int orderPrecedence = 0)
        {
            var metadata = IOrderPrecedenceMetadataViewFactory.Create(appliesTo, orderPrecedence);

            var export = new Lazy<T, IOrderPrecedenceMetadataView>(() => value, metadata);

            collection.Add(export);
        }
    }
}
