// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Syntax.InternalSyntax
{
    /// <summary>
    /// Interface that allows us to operate generically over VB or C# SyntaxLists.
    /// </summary>
    internal interface ISyntaxList<TGreenNode>
        where TGreenNode : GreenNode
    {
        TGreenNode this[int index] { get; }
        int Count { get; }
    }
}