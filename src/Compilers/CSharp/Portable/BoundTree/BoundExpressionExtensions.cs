// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

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

                case BoundKind.FieldAccess:
                    return ((BoundFieldAccess)node).FieldSymbol.RefKind;

                case BoundKind.Call:
                    return ((BoundCall)node).Method.RefKind;

                case BoundKind.PropertyAccess:
                    return ((BoundPropertyAccess)node).PropertySymbol.RefKind;

                case BoundKind.IndexerAccess:
                    return ((BoundIndexerAccess)node).Indexer.RefKind;

                case BoundKind.ImplicitIndexerAccess:
                    return ((BoundImplicitIndexerAccess)node).IndexerOrSliceAccess.GetRefKind();

                case BoundKind.ConditionalOperator:
                    {
                        var cond = (BoundConditionalOperator)node;
                        if (!cond.IsRef)
                        {
                            return RefKind.None;
                        }
                        var conseqRefKind = cond.Consequence.GetRefKind();
                        if (conseqRefKind == RefKind.RefReadOnly)
                        {
                            return RefKind.RefReadOnly;
                        }
                        var altRefKind = cond.Alternative.GetRefKind();
                        if (altRefKind == RefKind.RefReadOnly)
                        {
                            return RefKind.RefReadOnly;
                        }
                        return RefKind.Ref;
                    }

                case BoundKind.InlineArrayAccess:
                    {
                        var elementAccess = (BoundInlineArrayAccess)node;

                        if (!elementAccess.IsValue)
                        {
                            switch (elementAccess.GetItemOrSliceHelper)
                            {
                                case WellKnownMember.System_Span_T__get_Item:
                                    return RefKind.Ref;
                                case WellKnownMember.System_ReadOnlySpan_T__get_Item:
                                    return RefKind.RefReadOnly;
                            }
                        }

                        return RefKind.None;
                    }

                case BoundKind.ObjectInitializerMember:
                    var member = (BoundObjectInitializerMember)node;
                    if (member.HasErrors)
                        return RefKind.None;

                    return member.MemberSymbol switch
                    {
                        FieldSymbol f => f.RefKind,
                        PropertySymbol f => f.RefKind,
                        EventSymbol => RefKind.None,
                        var s => throw ExceptionUtilities.UnexpectedValue(s?.Kind)
                    };

                default:
                    return RefKind.None;
            }
        }

        public static bool IsLiteralNull(this BoundExpression node)
        {
            return node is { Kind: BoundKind.Literal, ConstantValueOpt: { Discriminator: ConstantValueTypeDiscriminator.Null } };
        }

        public static bool IsLiteralDefault(this BoundExpression node)
        {
            return node.Kind == BoundKind.DefaultLiteral;
        }

        public static bool IsImplicitObjectCreation(this BoundExpression node)
        {
            return node.Kind == BoundKind.UnconvertedObjectCreationExpression;
        }

        public static bool IsLiteralDefaultOrImplicitObjectCreation(this BoundExpression node)
        {
            return node.IsLiteralDefault() || node.IsImplicitObjectCreation();
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

            var constValue = node.ConstantValueOpt;
            if (constValue != null)
            {
                return constValue.IsDefaultValue;
            }

            return false;
        }

        public static bool HasExpressionType(this BoundExpression node)
        {
            // null literal, method group, and anonymous function expressions have no type.
            return node.Type is { };
        }

        public static bool HasDynamicType(this BoundExpression node)
        {
            var type = node.Type;
            return type is { } && type.IsDynamic();
        }

        public static NamedTypeSymbol? GetInferredDelegateType(this BoundExpression expr, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            Debug.Assert(expr.Kind is BoundKind.MethodGroup or BoundKind.UnboundLambda);

            var delegateType = expr.GetFunctionType()?.GetInternalDelegateType();
            delegateType?.AddUseSiteInfo(ref useSiteInfo);
            return delegateType;
        }

        public static TypeSymbol? GetTypeOrFunctionType(this BoundExpression expr)
        {
            if (expr.Type is { } type)
            {
                return type;
            }
            return expr.GetFunctionType();
        }

        public static FunctionTypeSymbol? GetFunctionType(this BoundExpression expr)
        {
            return expr switch
            {
                BoundMethodGroup methodGroup => methodGroup.FunctionType,
                UnboundLambda unboundLambda => unboundLambda.FunctionType,
                _ => null
            };
        }

        public static bool MethodGroupReceiverIsDynamic(this BoundMethodGroup node)
        {
            return node.InstanceOpt != null && node.InstanceOpt.HasDynamicType();
        }

        public static void GetExpressionSymbols(this BoundExpression node, ArrayBuilder<Symbol> symbols, BoundNode parent, Binder binder)
        {
            switch (node.Kind)
            {
                case BoundKind.MethodGroup:
                    // Special case: if we are looking for info on "M" in "new Action(M)" in the context of a parent 
                    // then we want to get the symbol that overload resolution chose for M, not on the whole method group M.
                    var delegateCreation = parent as BoundDelegateCreationExpression;
                    if (delegateCreation != null && delegateCreation.MethodOpt is { })
                    {
                        symbols.Add(delegateCreation.MethodOpt);
                    }
                    else
                    {
                        symbols.AddRange(CSharpSemanticModel.GetReducedAndFilteredMethodGroupSymbols(binder, (BoundMethodGroup)node));
                    }
                    break;

                case BoundKind.BadExpression:
                    foreach (var s in ((BoundBadExpression)node).Symbols)
                    {
                        if (s is { })
                            symbols.Add(s);
                    }
                    break;

                case BoundKind.DelegateCreationExpression:
                    var expr = (BoundDelegateCreationExpression)node;
                    var ctor = expr.Type.GetMembers(WellKnownMemberNames.InstanceConstructorName).FirstOrDefault();
                    if (ctor is { })
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
                    if (symbol is { })
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

        internal static bool IsExpressionOfComImportType([NotNullWhen(true)] this BoundExpression? expressionOpt)
        {
            // NOTE: Dev11 also returns false if expressionOpt is a TypeExpression.  Unfortunately,
            // that makes it impossible to handle TypeOrValueExpression in a consistent way, since
            // we don't know whether it's a type until after overload resolution and we can't do
            // overload resolution without knowing whether 'ref' can be omitted (which is what this
            // method is used to determine).  Since there is no intuitive reason to disallow
            // omitting 'ref' for static methods, we'll drop the restriction on TypeExpression.
            if (expressionOpt == null)
                return false;

            TypeSymbol? receiverType = expressionOpt.Type;
            return receiverType is NamedTypeSymbol { Kind: SymbolKind.NamedType, IsComImport: true };
        }

        internal static bool IsDiscardExpression(this BoundExpression expr)
        {
            return expr switch
            {
                BoundDiscardExpression => true,
                OutDeconstructVarPendingInference { IsDiscardExpression: true } => true,
                BoundDeconstructValuePlaceholder { IsDiscardExpression: true } => true,
                _ => false
            };
        }
    }
}
