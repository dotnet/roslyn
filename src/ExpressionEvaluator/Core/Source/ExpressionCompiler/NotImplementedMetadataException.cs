// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class NotImplementedMetadataException : Exception
    {
        internal NotImplementedMetadataException(NotImplementedException inner) : base(string.Empty, inner)
        {
        }
    }
}
