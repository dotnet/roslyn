using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Syntax.InternalSyntax
{
    internal interface IBinaryExpressionSyntax
    {
        GreenNode Left { get; }
        GreenNode OperatorToken { get; }
        GreenNode Right { get; }

        void BaseWriteTo(TextWriter writer, bool leading, bool trailing);
    }

    internal static class BinaryExpressionSyntaxHelpers
    {
        public static void WriteTo(
            IBinaryExpressionSyntax @this, TextWriter writer, bool leading, bool trailing)
        {
            // Do not blow the stack due to a deep recursion on the left. 
            // This is consistent with Parser.ParseSubExpressionCore implementation.

            var childAsBinary = @this.Left as IBinaryExpressionSyntax;

            if (childAsBinary == null)
            {
                @this.BaseWriteTo(writer, leading, trailing);
                return;
            }

            var stack = ArrayBuilder<IBinaryExpressionSyntax>.GetInstance();
            stack.Push(@this);

            IBinaryExpressionSyntax binary = childAsBinary;
            GreenNode child;

            while (true)
            {
                stack.Push(binary);
                child = binary.Left;
                childAsBinary = child as IBinaryExpressionSyntax;

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
                binary.Right.WriteTo(writer, leading: true, trailing: trailing | ((object)binary != @this));
            }
            while ((object)binary != @this);

            Debug.Assert(stack.Count == 0);
            stack.Free();
        }
    }
}
