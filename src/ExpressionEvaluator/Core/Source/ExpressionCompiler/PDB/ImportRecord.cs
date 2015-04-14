// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal abstract class ImportRecord
    {
        public abstract ImportTargetKind TargetKind { get; }
        public abstract string Alias { get; }
        public abstract string TargetString { get; }
    }
}
