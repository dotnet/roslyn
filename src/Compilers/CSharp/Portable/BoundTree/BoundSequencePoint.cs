// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundSequencePoint
    {
        public static BoundStatement Create(CSharpSyntaxNode syntax, TextSpan? part, BoundStatement statement, bool hasErrors = false)
        {
            if (part.HasValue)
            {
                return new BoundSequencePointWithSpan(syntax, statement, part.Value, hasErrors);
            }
            else
            {
                return new BoundSequencePoint(syntax, statement, hasErrors);
            }
        }
    }
}
