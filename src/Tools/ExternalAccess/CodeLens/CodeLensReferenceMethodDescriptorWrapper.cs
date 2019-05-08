// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeLens;

namespace Microsoft.CodeAnalysis.ExternalAccess.CodeLens
{
    public readonly struct CodeLensReferenceMethodDescriptorWrapper
    {
        internal CodeLensReferenceMethodDescriptorWrapper(ReferenceMethodDescriptor underlyingObject)
        {
            UnderlyingObject = underlyingObject;
        }

        internal ReferenceMethodDescriptor UnderlyingObject { get; }

        public string FullName => UnderlyingObject.FullName;
        public string FilePath => UnderlyingObject.FilePath;
    }
}
