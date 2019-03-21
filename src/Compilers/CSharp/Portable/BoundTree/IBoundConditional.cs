// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp
{
    internal interface IBoundConditional
    {
        BoundNode Condition { get; }
        BoundNode Consequence { get; }
        BoundNode AlternativeOpt { get; }
    }
}
