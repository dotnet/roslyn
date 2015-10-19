// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    internal interface IActionHolder<TArgument>
    {
        Action<int, List<TArgument>, SyntaxNode, SyntaxToken, NextAction<TArgument>> NextOperation { get; }
        Action<int, List<TArgument>, SyntaxNode, SyntaxToken, IActionHolder<TArgument>> Continuation { get; }
    }
}
