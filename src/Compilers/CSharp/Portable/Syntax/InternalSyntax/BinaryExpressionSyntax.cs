// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using Microsoft.CodeAnalysis.Syntax.InternalSyntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class BinaryExpressionSyntax : IBinaryExpressionSyntax
    {
        GreenNode IBinaryExpressionSyntax.Left => Left;

        GreenNode IBinaryExpressionSyntax.OperatorToken => OperatorToken;

        GreenNode IBinaryExpressionSyntax.Right => Right;

        public void BaseWriteTo(TextWriter writer, bool leading, bool trailing)
        {
            base.WriteTo(writer, leading, trailing);
        }

        protected internal override void WriteTo(TextWriter writer, bool leading, bool trailing)
        {
            BinaryExpressionSyntaxHelpers.WriteTo(this, writer, leading, trailing);
        }
    }
}