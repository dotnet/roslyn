// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.FindUsages
{
    internal class SerializableSourceReferenceItem
    {
        internal SourceReferenceItem Rehydrate(Workspace workspace)
        {
            throw new NotImplementedException();
        }

        internal static SerializableSourceReferenceItem Dehydrate(SourceReferenceItem referenceItem)
        {
            throw new NotImplementedException();
        }
    }
}