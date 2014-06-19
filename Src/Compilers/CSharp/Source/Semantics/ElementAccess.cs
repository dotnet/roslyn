// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Roslyn.Compilers.CSharp
{
    using Roslyn.Compilers.Internal;
    using Symbols.Source;

    internal sealed partial class SemanticAnalyzer
    {
        private BoundExpression BindElementAccess(ElementAccessExpressionSyntax node)
        {
            Debug.Assert(node != null);

            var expr = BindExpression(node.Expression); // , BIND_RVALUEREQUIRED);

            var arguments = node.ArgumentList.Arguments
                                    .Select(arg => BindExpression(arg.Expression))
                                    .ToList();

            var argumentNames = node.ArgumentList.Arguments
                                          .Select(arg => arg.NameColonOpt)
                                          .Select(namecolon => namecolon != null ? namecolon.Identifier.GetText() : null)
                                          .ToList();

            if (expr.GetExpressionType() == null)
            {
                return BoundArrayAccess.AsError(node, expr, arguments, null);
            }

            if (!expr.IsOK || arguments.Any(x => !x.IsOK))
            {
                // At this point we definitely have reported an error, but we still might be 
                // able to get more semantic analysis of the indexing operation. We do not
                // want to report cascading errors.

                // UNDONE: Set the error bit on the result?
                return GetErrorSuppressingAnalyzer().BindElementAccessCore(node, expr, arguments, argumentNames);
            }

            return BindElementAccessCore(node, expr, arguments, argumentNames);
        }

        private BoundExpression BindElementAccessCore(ElementAccessExpressionSyntax node, BoundExpression expr, IList<BoundExpression> arguments, IList<string> argumentNames)
        {
            Debug.Assert(node != null);
            Debug.Assert(expr != null);
            Debug.Assert(arguments != null);
            Debug.Assert(argumentNames != null);

            // UNDONE: Suppose we have an indexed property P on an instance c. Suppose the 
            // UNDONE: type of the property is int[].  When binding c.P[123] we must 
            // UNDONE: treat this as an invocation of the indexed property, not as a
            // UNDONE: dereference of the array returned by the property.

            //if (expr.isPROP() && expr.asPROP().IsUnboundIndexedProperty() && 
            //    !IsPropertyBindingInsideBindIndexerContext(node.Expression))
            //{
            //    return BindIndexerAccess(node, expr, arguments, argumentNames);
            //}

            var exprType = expr.GetExpressionType();
            // UNDONE: Ensure that the type of the expression has members defined.
            if (exprType is ArrayTypeSymbol)
            {
                return BindArrayAccess(node, expr, arguments, argumentNames);
            }
            else if (exprType is PointerTypeSymbol)
            {
                return BindPointerElementAccess(node, expr, arguments, argumentNames);
            }
            else
            {
                return BindIndexerAccess(node, expr, arguments, argumentNames);
            }
        }

        private BoundExpression BindArrayAccess(ElementAccessExpressionSyntax node, BoundExpression expr, IList<BoundExpression> arguments, IList<string> argumentNames)
        {
            Debug.Assert(node != null);
            Debug.Assert(expr != null);
            Debug.Assert(arguments != null);
            Debug.Assert(argumentNames != null);

            // For an array access, the primary-no-array-creation-expression of the element-access must
            // be a value of an array-type. Furthermore, the argument-list of an array access is not
            // allowed to contain named arguments.The number of expressions in the argument-list must 
            // be the same as the rank of the array-type, and each expression must be of type 
            // int, uint, long, ulong, or must be implicitly convertible to one or more of these types.

            if (argumentNames.Any(x => x != null))
            {
                Error(ErrorCode.ERR_NamedArgumentForArray, node);
            }

            var arrayType = (ArrayTypeSymbol)expr.GetExpressionType();

            // Note that the spec says to determine which of {int, uint, long, ulong} *each*
            // index expression is convertible to. That is not what C# 1 through 4
            // did; the implementations instead determined which of those four
            // types *all* of the index expressions converted to. 



            int rank = arrayType.Rank;

            if (arguments.Count != arrayType.Rank)
            {
                Error(ErrorCode.ERR_BadIndexCount, node, rank);
                return BoundArrayAccess.AsError(node, expr, arguments, arrayType.ElementType);
            }

            var convertedArguments = arguments.Select(x => ConvertToArrayIndex(x)).ToList();

            return new BoundArrayAccess(node, expr, convertedArguments, arrayType.ElementType);
        }

        private BoundExpression ConvertToArrayIndex(BoundExpression index)
        {
            Debug.Assert(index != null);

            var result =
                TryImplicitConversion(index, System_Int32) ??
                TryImplicitConversion(index, System_UInt32) ??
                TryImplicitConversion(index, System_Int64) ??
                TryImplicitConversion(index, System_UInt64);

            if (result == null)
            {
                // UNDONE: Give the error that would be given upon conversion to int32.
                return BoundConversion.AsError(index.Syntax, index, ConversionKind.NoConversion, false, false, System_Int32);
            }

            return result;
        }

        private BoundExpression BindPointerElementAccess(ElementAccessExpressionSyntax node, BoundExpression expr, IList<BoundExpression> arguments, IList<string> argumentNames)
        {
            Debug.Assert(node != null);
            Debug.Assert(expr != null);
            Debug.Assert(arguments != null);
            Debug.Assert(argumentNames != null);

            // UNDONE: This is the error reported by the original compiler, but it seems wrong. We should not be
            // UNDONE: giving the error "named argument used in array access" for a pointer access.
            if (argumentNames.Any(x => x != null))
            {
                Error(ErrorCode.ERR_NamedArgumentForArray, node);
            }

            throw new NotImplementedException();
        }

        private BoundExpression BindIndexerAccess(ElementAccessExpressionSyntax node, BoundExpression expr, IList<BoundExpression> arguments, IList<String> argumentNames)
        {
            Debug.Assert(node != null);
            Debug.Assert(expr != null);
            Debug.Assert(arguments != null);
            Debug.Assert(argumentNames != null);

            // UNDONE: Make sure BindUserDefinedIndexerAccess handles the case where left is "base". 

            throw new NotImplementedException();
        }
    }
}
