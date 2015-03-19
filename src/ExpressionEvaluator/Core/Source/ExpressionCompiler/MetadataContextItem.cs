// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Debugger;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class MetadataContextItem<TMetadataContext> : DkmDataItem
        where TMetadataContext : struct
    {
        internal readonly TMetadataContext MetadataContext;

        internal MetadataContextItem(TMetadataContext metadataContext)
        {
            this.MetadataContext = metadataContext;
        }
    }
}
