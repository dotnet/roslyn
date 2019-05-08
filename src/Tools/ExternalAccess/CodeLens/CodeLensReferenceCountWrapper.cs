// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeLens;

namespace Microsoft.CodeAnalysis.ExternalAccess.CodeLens
{
    public readonly struct CodeLensReferenceCountWrapper
    {
        internal CodeLensReferenceCountWrapper(ReferenceCount underlyingObject)
        {
            UnderlyingObject = underlyingObject;
        }

        internal ReferenceCount UnderlyingObject { get; }

        public int Count => UnderlyingObject.Count;
        public bool IsCapped => UnderlyingObject.IsCapped;
    }
}
