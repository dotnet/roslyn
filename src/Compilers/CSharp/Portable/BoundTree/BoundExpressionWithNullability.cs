// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class BoundExpressionWithNullability : BoundExpression
    {
        public override bool IsSuppressed
        {
            get => Expression.IsSuppressed;
            protected set => throw ExceptionUtilities.Unreachable;
        }
    }
}

