// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class BinaryExpressionSyntax
    {
        protected internal override void WriteTo(TextWriter writer, bool leading, bool trailing)
        {
            // Do not blow the stack due to a deep recursion on the left. 
            // This is consistent with Parser.ParseSubExpressionCore implementation.

            var childAsBinary = this.Left as BinaryExpressionSyntax;

            if (childAsBinary == null)
            {
                base.WriteTo(writer, leading, trailing);
                return;
            }

            var stack = ArrayBuilder<BinaryExpressionSyntax>.GetInstance();
            stack.Push(this);

            BinaryExpressionSyntax binary = childAsBinary;
            ExpressionSyntax child;

            while (true)
            {
                stack.Push(binary);
                child = binary.Left;
                childAsBinary = child as BinaryExpressionSyntax;

                if (childAsBinary == null)
                {
                    break;
                }

                binary = childAsBinary;
            }

            child.WriteTo(writer, leading: leading, trailing: true);

            do
            {
                binary = stack.Pop();

                binary.OperatorToken.WriteTo(writer, leading: true, trailing: true);
                binary.Right.WriteTo(writer, leading: true, trailing: trailing | ((object)binary != this));
            }
            while ((object)binary != this);

            Debug.Assert(stack.Count == 0);
            stack.Free();
        }
    }
}