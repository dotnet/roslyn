// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

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
            return node is
            {
                Kind: BoundKind.Literal,
                ConstantValue: { Discriminator: ConstantValueTypeDiscriminator.Null }
            };
        }

        public static bool IsLiteralDefault(this BoundExpression node)
        {
            return node.Kind == BoundKind.DefaultLiteral;
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
            if (node.Kind == BoundKind.DefaultExpression || node.Kind == BoundKind.DefaultLiteral)
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
    }
}
