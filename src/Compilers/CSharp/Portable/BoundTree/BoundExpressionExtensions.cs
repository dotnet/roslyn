// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CSharp.Binder;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class BoundExpressionExtensions
    {
        /// <summary>
        /// Returns the RefKind if the expression represents a symbol
        /// that has a RefKind, or RefKind.None otherwise.
        /// </summary>
        public static RefKind GetRefKind(this BoundExpression node)
        {
            switch (node.Kind)
            {
                case BoundKind.Local:
                    return ((BoundLocal)node).LocalSymbol.RefKind;

                case BoundKind.Parameter:
                    return ((BoundParameter)node).ParameterSymbol.RefKind;

                case BoundKind.Call:
                    return ((BoundCall)node).Method.RefKind;

                case BoundKind.PropertyAccess:
                    return ((BoundPropertyAccess)node).PropertySymbol.RefKind;

                default:
                    return RefKind.None;
            }
        }

        public static bool IsLiteralNull(this BoundExpression node)
        {
            return node.Kind == BoundKind.Literal && node.ConstantValue.Discriminator == ConstantValueTypeDiscriminator.Null;
        }

        public static bool IsLiteralDefault(this BoundExpression node)
        {
            return node.Kind == BoundKind.DefaultExpression && node.Syntax.Kind() == SyntaxKind.DefaultLiteralExpression;
        }

        public static bool IsLiteralNullOrDefault(this BoundExpression node)
        {
            return node.IsLiteralNull() || node.IsLiteralDefault();
        }

        // returns true when expression has no side-effects and produces
        // default value (null, zero, false, default(T) ...)
        //
        // NOTE: This method is a very shallow check.
        //       It does not make any assumptions about what this node could become 
        //       after some folding/propagation/algebraic transformations.
        public static bool IsDefaultValue(this BoundExpression node)
        {
            if (node.Kind == BoundKind.DefaultExpression)
            {
                return true;
            }

            var constValue = node.ConstantValue;
            if (constValue != null)
            {
                return constValue.IsDefaultValue;
            }

            return false;
        }

        public static bool HasExpressionType(this BoundExpression node)
        {
            // null literal, method group, and anonymous function expressions have no type.
            return (object)node.Type != null;
        }

        public static bool HasDynamicType(this BoundExpression node)
        {
            var type = node.Type;
            return (object)type != null && type.IsDynamic();
        }

        public static bool MethodGroupReceiverIsDynamic(this BoundMethodGroup node)
        {
            return node.InstanceOpt != null && node.InstanceOpt.HasDynamicType();
        }

        public static bool HasExpressionSymbols(this BoundExpression node)
        {
            switch (node.Kind)
            {
                case BoundKind.Call:
                case BoundKind.Local:
                case BoundKind.FieldAccess:
                case BoundKind.PropertyAccess:
                case BoundKind.IndexerAccess:
                case BoundKind.EventAccess:
                case BoundKind.MethodGroup:
                case BoundKind.ObjectCreationExpression:
                case BoundKind.TypeExpression:
                case BoundKind.NamespaceExpression:
                    return true;
                case BoundKind.BadExpression:
                    return ((BoundBadExpression)node).Symbols.Length > 0;
                default:
                    return false;
            }
        }

        public static void GetExpressionSymbols(this BoundExpression node, ArrayBuilder<Symbol> symbols, BoundNode parent, Binder binder)
        {
            switch (node.Kind)
            {
                case BoundKind.MethodGroup:
                    // Special case: if we are looking for info on "M" in "new Action(M)" in the context of a parent 
                    // then we want to get the symbol that overload resolution chose for M, not on the whole method group M.
                    var delegateCreation = parent as BoundDelegateCreationExpression;
                    if (delegateCreation != null && (object)delegateCreation.MethodOpt != null)
                    {
                        symbols.Add(delegateCreation.MethodOpt);
                    }
                    else
                    {
                        symbols.AddRange(CSharpSemanticModel.GetReducedAndFilteredMethodGroupSymbols(binder, (BoundMethodGroup)node));
                    }
                    break;

                case BoundKind.BadExpression:
                    symbols.AddRange(((BoundBadExpression)node).Symbols);
                    break;

                case BoundKind.DelegateCreationExpression:
                    var expr = (BoundDelegateCreationExpression)node;
                    var ctor = expr.Type.GetMembers(WellKnownMemberNames.InstanceConstructorName).FirstOrDefault();
                    if ((object)ctor != null)
                    {
                        symbols.Add(ctor);
                    }
                    break;

                case BoundKind.Call:
                    // Either overload resolution succeeded for this call or it did not. If it did not
                    // succeed then we've stashed the original method symbols from the method group,
                    // and we should use those as the symbols displayed for the call. If it did succeed
                    // then we did not stash any symbols; just fall through to the default case.

                    var originalMethods = ((BoundCall)node).OriginalMethodsOpt;
                    if (originalMethods.IsDefault)
                    {
                        goto default;
                    }
                    symbols.AddRange(originalMethods);
                    break;

                case BoundKind.IndexerAccess:
                    // Same behavior as for a BoundCall: if overload resolution failed, pull out stashed candidates;
                    // otherwise use the default behavior.

                    var originalIndexers = ((BoundIndexerAccess)node).OriginalIndexersOpt;
                    if (originalIndexers.IsDefault)
                    {
                        goto default;
                    }
                    symbols.AddRange(originalIndexers);
                    break;

                default:
                    var symbol = node.ExpressionSymbol;
                    if ((object)symbol != null)
                    {
                        symbols.Add(symbol);
                    }
                    break;
            }
        }

        // Get the conversion associated with a bound node, or else Identity.
        public static Conversion GetConversion(this BoundExpression boundNode)
        {
            switch (boundNode.Kind)
            {
                case BoundKind.Conversion:
                    BoundConversion conversionNode = (BoundConversion)boundNode;
                    return conversionNode.Conversion;

                default:
                    return Conversion.Identity;
            }
        }

        internal static bool IsExpressionOfComImportType(this BoundExpression expressionOpt)
        {
            // NOTE: Dev11 also returns false if expressionOpt is a TypeExpression.  Unfortunately,
            // that makes it impossible to handle TypeOrValueExpression in a consistent way, since
            // we don't know whether it's a type until after overload resolution and we can't do
            // overload resolution without knowing whether 'ref' can be omitted (which is what this
            // method is used to determine).  Since there is no intuitive reason to disallow
            // omitting 'ref' for static methods, we'll drop the restriction on TypeExpression.
            if (expressionOpt == null) return false;

            TypeSymbol receiverType = expressionOpt.Type;
            return (object)receiverType != null && receiverType.Kind == SymbolKind.NamedType && ((NamedTypeSymbol)receiverType).IsComImport;
        }

        // https://github.com/dotnet/roslyn/issues/29618 Remove this method. Initial binding should not infer nullability.
        internal static TypeSymbolWithAnnotations GetTypeAndNullability(this BoundExpression expr)
        {
            var type = expr.Type;
            if ((object)type == null)
            {
                return default;
            }
            var annotation = expr.GetNullableAnnotation();
            return TypeSymbolWithAnnotations.Create(type, annotation);
        }

        // https://github.com/dotnet/roslyn/issues/29618 Remove this method. Initial binding should not infer nullability.
        /// <summary>
        /// Returns the top-level nullability of the expression if the nullability can be determined statically,
        /// and returns null otherwise. (May return null even in cases where the nullability is explicit,
        /// say a reference to an unannotated field.) This method does not visit child nodes unless
        /// the nullability of this expression can be determined trivially from the nullability of a child node.
        /// This method is not a replacement for the actual calculation of nullability through flow analysis
        /// which is handled in NullableWalker.
        /// </summary>
        private static NullableAnnotation GetNullableAnnotation(this BoundExpression expr)
        {
            switch (expr.Kind)
            {
                case BoundKind.SuppressNullableWarningExpression:
                    return NullableAnnotation.Unknown;
                case BoundKind.Local:
                    {
                        var local = (BoundLocal)expr;
                        return local.IsNullableUnknown ? NullableAnnotation.Unknown : local.LocalSymbol.Type.NullableAnnotation;
                    }
                case BoundKind.Parameter:
                    return ((BoundParameter)expr).ParameterSymbol.Type.NullableAnnotation;
                case BoundKind.FieldAccess:
                    return ((BoundFieldAccess)expr).FieldSymbol.Type.NullableAnnotation;
                case BoundKind.PropertyAccess:
                    return ((BoundPropertyAccess)expr).PropertySymbol.Type.NullableAnnotation;
                case BoundKind.Call:
                    return ((BoundCall)expr).Method.ReturnType.NullableAnnotation;
                case BoundKind.Conversion:
                    return ((BoundConversion)expr).ConversionGroupOpt?.ExplicitType.NullableAnnotation ?? NullableAnnotation.Unknown;
                case BoundKind.BinaryOperator:
                    return ((BoundBinaryOperator)expr).MethodOpt?.ReturnType.NullableAnnotation ?? NullableAnnotation.Unknown;
                case BoundKind.NullCoalescingOperator:
                    {
                        var op = (BoundNullCoalescingOperator)expr;
                        var left = op.LeftOperand.GetNullableAnnotation();
                        var right = op.RightOperand.GetNullableAnnotation();
                        return left.IsAnyNullable() ? right : left;
                    }
                case BoundKind.ThisReference:
                case BoundKind.BaseReference:
                case BoundKind.NewT:
                case BoundKind.ObjectCreationExpression:
                case BoundKind.DelegateCreationExpression:
                case BoundKind.NoPiaObjectCreationExpression:
                case BoundKind.InterpolatedString:
                case BoundKind.TypeOfOperator:
                case BoundKind.NameOfOperator:
                case BoundKind.TupleLiteral:
                    return NullableAnnotation.NotNullable;
                case BoundKind.DefaultExpression:
                case BoundKind.Literal:
                case BoundKind.UnboundLambda:
                    break;
                case BoundKind.ExpressionWithNullability:
                    return ((BoundExpressionWithNullability)expr).NullableAnnotation;
                default:
                    break;
            }

            var constant = expr.ConstantValue;
            if (constant != null)
            {
                if (constant.IsNull)
                {
                    return NullableAnnotation.Nullable;
                }
                if (expr.Type?.IsReferenceType == true)
                {
                    return NullableAnnotation.NotNullable;
                }
            }

            return NullableAnnotation.Unknown;
        }
    }
}
