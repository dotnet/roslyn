// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundExpression
    {
        public SimpleNameSyntax? InterceptableNameSyntax
        {
            get
            {
                // When this assertion fails, it means a new syntax is being used which corresponds to a BoundCall.
                // The developer needs to determine how this new syntax should interact with interceptors (produce an error, permit intercepting the call, etc...)
                Debug.Assert(this.WasCompilerGenerated || this.Syntax is InvocationExpressionSyntax or ConstructorInitializerSyntax or PrimaryConstructorBaseTypeSyntax { ArgumentList: { } },
                    $"Unexpected syntax kind for BoundCall: {this.Syntax.Kind()}");

                if (this.WasCompilerGenerated || this.Syntax is not InvocationExpressionSyntax syntax)
                {
                    return null;
                }

                // If a qualified name is used as a valid receiver of an invocation syntax at some point,
                // we probably want to treat it similarly to a MemberAccessExpression.
                // However, we don't expect to encounter it.
                Debug.Assert(syntax.Expression is not QualifiedNameSyntax);

                return syntax.Expression switch
                {
                    MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
                    SimpleNameSyntax name => name,
                    _ => null
                };
            }
        }

        internal BoundExpression WithSuppression(bool suppress = true)
        {
            if (this.IsSuppressed == suppress)
            {
                return this;
            }

            // There is no scenario where suppression goes away
            Debug.Assert(suppress || !this.IsSuppressed);

            var result = (BoundExpression)MemberwiseClone();
            result.IsSuppressed = suppress;
            return result;
        }

        internal BoundExpression WithWasConverted()
        {
#if DEBUG
            // We track the WasConverted flag for locals and parameters only, as many other
            // kinds of bound nodes have special behavior that prevents this from working for them.
            // Also we want to minimize the GC pressure, even in Debug, and we have excellent
            // test coverage for locals and parameters.
            if ((Kind != BoundKind.Local && Kind != BoundKind.Parameter) || this.WasConverted)
                return this;

            var result = (BoundExpression)MemberwiseClone();
            result.WasConverted = true;
            return result;
#else
            return this;
#endif
        }

        internal new BoundExpression WithHasErrors()
        {
            return (BoundExpression)base.WithHasErrors();
        }

        internal bool NeedsToBeConverted()
        {
            switch (Kind)
            {
                case BoundKind.TupleLiteral:
                case BoundKind.UnconvertedSwitchExpression:
                case BoundKind.UnconvertedObjectCreationExpression:
                case BoundKind.UnconvertedConditionalOperator:
                case BoundKind.DefaultLiteral:
                case BoundKind.UnconvertedInterpolatedString:
                case BoundKind.UnconvertedCollectionExpression:
                    return true;
                case BoundKind.StackAllocArrayCreation:
                    // A BoundStackAllocArrayCreation is given a null type when it is in a
                    // syntactic context where it could be either a pointer or a span, and
                    // in that case it requires conversion to one or the other.
                    return this.Type is null;
                case BoundKind.BinaryOperator:
                    return ((BoundBinaryOperator)this).IsUnconvertedInterpolatedStringAddition;
#if DEBUG
                case BoundKind.Local when !WasConverted:
                case BoundKind.Parameter when !WasConverted:
                    return !WasCompilerGenerated;
#endif
                default:
                    return false;
            }
        }

        public virtual ConstantValue? ConstantValueOpt
        {
            get
            {
                return null;
            }
        }

        public virtual Symbol? ExpressionSymbol
        {
            get
            {
                return null;
            }
        }

        // Indicates any problems with lookup/symbol binding that should be reported via GetSemanticInfo.
        public virtual LookupResultKind ResultKind
        {
            get
            {
                return LookupResultKind.Viable;
            }
        }

        /// <summary>
        /// Returns true if calls and delegate invocations with this
        /// expression as the receiver should be non-virtual calls.
        /// </summary>
        public virtual bool SuppressVirtualCalls
        {
            get
            {
                return false;
            }
        }

        public new NullabilityInfo TopLevelNullability
        {
            get => base.TopLevelNullability;
            set => base.TopLevelNullability = value;
        }

        public CodeAnalysis.ITypeSymbol? GetPublicTypeSymbol()
            => Type?.GetITypeSymbol(TopLevelNullability.FlowState.ToAnnotation());

        public virtual bool IsEquivalentToThisReference => false;
    }

    internal partial class BoundValuePlaceholderBase
    {
        public abstract override bool IsEquivalentToThisReference { get; }
    }

    internal partial class BoundValuePlaceholder
    {
        public sealed override bool IsEquivalentToThisReference => throw ExceptionUtilities.Unreachable();
    }

    internal partial class BoundInterpolatedStringHandlerPlaceholder
    {
        public sealed override bool IsEquivalentToThisReference => false;
    }

    internal partial class BoundCollectionExpressionSpreadExpressionPlaceholder
    {
        public sealed override bool IsEquivalentToThisReference => false;
    }

    internal partial class BoundDeconstructValuePlaceholder
    {
        public sealed override bool IsEquivalentToThisReference => false; // Preserving old behavior
    }

    internal partial class BoundTupleOperandPlaceholder
    {
        public sealed override bool IsEquivalentToThisReference => throw ExceptionUtilities.Unreachable();
    }

    internal partial class BoundAwaitableValuePlaceholder
    {
        public sealed override bool IsEquivalentToThisReference => false; // Preserving old behavior
    }

    internal partial class BoundDisposableValuePlaceholder
    {
        public sealed override bool IsEquivalentToThisReference => false;
    }

    internal partial class BoundObjectOrCollectionValuePlaceholder
    {
        public sealed override bool IsEquivalentToThisReference => false;
    }

    internal partial class BoundImplicitIndexerValuePlaceholder
    {
        public sealed override bool IsEquivalentToThisReference => throw ExceptionUtilities.Unreachable();
    }

    internal partial class BoundListPatternReceiverPlaceholder
    {
        public sealed override bool IsEquivalentToThisReference => false;
    }

    internal partial class BoundListPatternIndexPlaceholder
    {
        public sealed override bool IsEquivalentToThisReference => throw ExceptionUtilities.Unreachable();
    }

    internal partial class BoundSlicePatternReceiverPlaceholder
    {
        public sealed override bool IsEquivalentToThisReference => false;
    }

    internal partial class BoundSlicePatternRangePlaceholder
    {
        public sealed override bool IsEquivalentToThisReference => throw ExceptionUtilities.Unreachable();
    }

    internal partial class BoundCapturedReceiverPlaceholder
    {
        public sealed override bool IsEquivalentToThisReference
        {
            get
            {
                Debug.Assert(false); // Getting here is unexpected.
                return false;
            }
        }
    }

    internal partial class BoundThisReference
    {
        public sealed override bool IsEquivalentToThisReference => true;
    }

    internal partial class BoundPassByCopy
    {
        public override ConstantValue? ConstantValueOpt
        {
            get
            {
                Debug.Assert(Expression.ConstantValueOpt == null);
                return null;
            }
        }

        public override Symbol? ExpressionSymbol
        {
            get
            {
                return Expression.ExpressionSymbol;
            }
        }
    }

    internal partial class BoundCall
    {
        public override Symbol ExpressionSymbol
        {
            get
            {
                return this.Method;
            }
        }
    }

    internal partial class BoundTypeExpression
    {
        public override Symbol ExpressionSymbol
        {
            get { return this.AliasOpt ?? (Symbol)this.Type; }
        }

        public override LookupResultKind ResultKind
        {
            get
            {
                ErrorTypeSymbol? errorType = this.Type.OriginalDefinition as ErrorTypeSymbol;
                if (errorType is { })
                    return errorType.ResultKind;
                else
                    return LookupResultKind.Viable;
            }
        }
    }

    internal partial class BoundNamespaceExpression
    {
        public override Symbol ExpressionSymbol
        {
            get { return this.AliasOpt ?? (Symbol)this.NamespaceSymbol; }
        }
    }

    internal partial class BoundLocal
    {
        public override Symbol ExpressionSymbol
        {
            get { return this.LocalSymbol; }
        }

        public BoundLocal(SyntaxNode syntax, LocalSymbol localSymbol, ConstantValue? constantValueOpt, TypeSymbol type, bool hasErrors = false)
            : this(syntax, localSymbol, BoundLocalDeclarationKind.None, constantValueOpt, false, type, hasErrors)
        {
        }

        public BoundLocal Update(LocalSymbol localSymbol, ConstantValue? constantValueOpt, TypeSymbol type)
        {
            return this.Update(localSymbol, this.DeclarationKind, constantValueOpt, this.IsNullableUnknown, type);
        }
    }

    internal partial class BoundFieldAccess
    {
        public override Symbol? ExpressionSymbol
        {
            get { return this.FieldSymbol; }
        }
    }

    internal partial class BoundPropertyAccess
    {
        public override Symbol? ExpressionSymbol
        {
            get { return this.PropertySymbol; }
        }
    }

    internal partial class BoundIndexerAccess
    {
        public override Symbol? ExpressionSymbol
        {
            get { return this.Indexer; }
        }

        public override LookupResultKind ResultKind
        {
            get
            {
                return !this.OriginalIndexersOpt.IsDefault ? LookupResultKind.OverloadResolutionFailure : base.ResultKind;
            }
        }
    }

    internal partial class BoundDynamicIndexerAccess
    {
        internal string? TryGetIndexedPropertyName()
        {
            foreach (var indexer in ApplicableIndexers)
            {
                if (!indexer.IsIndexer && indexer.IsIndexedProperty)
                {
                    return indexer.Name;
                }
            }

            return null;
        }
    }

    internal partial class BoundEventAccess
    {
        public override Symbol ExpressionSymbol
        {
            get { return this.EventSymbol; }
        }
    }

    internal partial class BoundParameter
    {
        public override Symbol ExpressionSymbol
        {
            get { return this.ParameterSymbol; }
        }
    }

    internal partial class BoundBinaryOperator
    {
        public override ConstantValue? ConstantValueOpt => Data?.ConstantValue;

        public override Symbol? ExpressionSymbol => this.Method;

        internal MethodSymbol? Method => Data?.Method;

        internal TypeSymbol? ConstrainedToType => Data?.ConstrainedToType;

        internal bool IsUnconvertedInterpolatedStringAddition => Data?.IsUnconvertedInterpolatedStringAddition ?? false;

        internal InterpolatedStringHandlerData? InterpolatedStringHandlerData => Data?.InterpolatedStringHandlerData;

        internal ImmutableArray<MethodSymbol> OriginalUserDefinedOperatorsOpt => Data?.OriginalUserDefinedOperatorsOpt ?? default(ImmutableArray<MethodSymbol>);
    }

    internal partial class BoundUserDefinedConditionalLogicalOperator
    {
        public override Symbol ExpressionSymbol
        {
            get { return this.LogicalOperator; }
        }
    }

    internal partial class BoundUnaryOperator
    {
        public override Symbol? ExpressionSymbol
        {
            get { return this.MethodOpt; }
        }
    }

    internal partial class BoundIncrementOperator
    {
        public override Symbol? ExpressionSymbol
        {
            get { return this.MethodOpt; }
        }
    }

    internal partial class BoundCompoundAssignmentOperator
    {
        public override Symbol? ExpressionSymbol
        {
            get { return this.Operator.Method; }
        }
    }

    internal partial class BoundConversion
    {
        public ConversionKind ConversionKind
        {
            get { return this.Conversion.Kind; }
        }

        public bool IsExtensionMethod
        {
            get { return this.Conversion.IsExtensionMethod; }
        }

        public MethodSymbol? SymbolOpt
        {
            get { return this.Conversion.Method; }
        }

        public override Symbol? ExpressionSymbol
        {
            get { return this.SymbolOpt; }
        }

        public override bool SuppressVirtualCalls
        {
            get { return this.IsBaseConversion; }
        }

        public BoundConversion UpdateOperand(BoundExpression operand)
        {
            return this.Update(operand: operand, this.Conversion, this.IsBaseConversion, this.Checked, this.ExplicitCastInCode, this.ConstantValueOpt, this.ConversionGroupOpt, this.OriginalUserDefinedConversionsOpt, this.Type);
        }

        /// <summary>
        /// Returns true when conversion itself (not the operand) may have side-effects
        /// A typical side-effect of a conversion is an exception when conversion is unsuccessful.
        /// </summary>
        /// <returns></returns>
        internal bool ConversionHasSideEffects()
        {
            // only some intrinsic conversions are side effect free
            // the only side effect of an intrinsic conversion is a throw when we fail to convert.
            // and some intrinsic conversion always succeed
            switch (this.ConversionKind)
            {
                case ConversionKind.Identity:
                // NOTE: even explicit float/double identity conversion does not have side
                // effects since it does not throw
                case ConversionKind.ImplicitNumeric:
                case ConversionKind.ImplicitEnumeration:
                // implicit ref cast does not throw ...
                case ConversionKind.ImplicitReference:
                case ConversionKind.Boxing:
                    return false;

                // unchecked numeric conversion does not throw
                case ConversionKind.ExplicitNumeric:
                    return this.Checked;
            }

            return true;
        }
    }

    internal partial class BoundObjectCreationExpression
    {
        public override Symbol ExpressionSymbol
        {
            get { return this.Constructor; }
        }

        /// <summary>
        /// Build an object creation expression without performing any rewriting
        /// </summary>
        internal BoundObjectCreationExpression UpdateArgumentsAndInitializer(
            ImmutableArray<BoundExpression> newArguments,
            ImmutableArray<RefKind> newRefKinds,
            BoundObjectInitializerExpressionBase? newInitializerExpression,
            TypeSymbol? changeTypeOpt = null)
        {
            return Update(
                constructor: Constructor,
                arguments: newArguments,
                argumentNamesOpt: default(ImmutableArray<string?>),
                argumentRefKindsOpt: newRefKinds,
                expanded: false,
                argsToParamsOpt: default(ImmutableArray<int>),
                defaultArguments: default(BitVector),
                constantValueOpt: ConstantValueOpt,
                initializerExpressionOpt: newInitializerExpression,
                type: changeTypeOpt ?? Type);
        }
    }

    internal partial class BoundAnonymousObjectCreationExpression
    {
        public override Symbol ExpressionSymbol
        {
            get { return this.Constructor; }
        }
    }

    internal partial class BoundAnonymousPropertyDeclaration
    {
        public override Symbol ExpressionSymbol
        {
            get { return this.Property; }
        }
    }

    internal partial class BoundLambda
    {
        public override Symbol ExpressionSymbol
        {
            get { return this.Symbol; }
        }
    }

    internal partial class BoundAttribute
    {
        public override Symbol? ExpressionSymbol
        {
            get { return this.Constructor; }
        }
    }

    internal partial class BoundDefaultLiteral
    {
        public override ConstantValue? ConstantValueOpt
        {
            get { return null; }
        }
    }

    internal partial class BoundConditionalOperator
    {
        public bool IsDynamic
        {
            get
            {
                // IsTrue dynamic operator is invoked at runtime if the condition is of the type dynamic.
                // The type of the operator itself is Boolean, so we need to check its kind.
                return this.Condition.Kind == BoundKind.UnaryOperator && ((BoundUnaryOperator)this.Condition).OperatorKind.IsDynamic();
            }
        }
    }

    internal partial class BoundRangeVariable
    {
        public override Symbol ExpressionSymbol
        {
            get
            {
                return this.RangeVariableSymbol;
            }
        }
    }

    internal partial class BoundLabel
    {
        public override Symbol ExpressionSymbol
        {
            get
            {
                return this.Label;
            }
        }
    }

    internal partial class BoundObjectInitializerMember
    {
        public override Symbol? ExpressionSymbol
        {
            get
            {
                return this.MemberSymbol;
            }
        }
    }

    internal partial class BoundCollectionElementInitializer
    {
        public override Symbol ExpressionSymbol
        {
            get
            {
                return this.AddMethod;
            }
        }
    }

    internal partial class BoundBaseReference
    {
        public override bool SuppressVirtualCalls
        {
            get { return true; }
        }
    }

    // NOTE: this type exists in order to hide the presence of {Value,Type}Expression inside of a
    //       BoundTypeOrValueExpression from the bound tree generator, which would otherwise generate
    //       a constructor that may spuriously set hasErrors to true if either field had errors.
    //       A BoundTypeOrValueExpression should never have errors if it is present in the tree.
    internal readonly struct BoundTypeOrValueData : System.IEquatable<BoundTypeOrValueData>
    {
        public Symbol ValueSymbol { get; }
        public BoundExpression ValueExpression { get; }
        public ReadOnlyBindingDiagnostic<AssemblySymbol> ValueDiagnostics { get; }
        public BoundExpression TypeExpression { get; }
        public ReadOnlyBindingDiagnostic<AssemblySymbol> TypeDiagnostics { get; }

        public BoundTypeOrValueData(Symbol valueSymbol, BoundExpression valueExpression, ReadOnlyBindingDiagnostic<AssemblySymbol> valueDiagnostics, BoundExpression typeExpression, ReadOnlyBindingDiagnostic<AssemblySymbol> typeDiagnostics)
        {
            Debug.Assert(valueSymbol != null, "Field 'valueSymbol' cannot be null (use Null=\"allow\" in BoundNodes.xml to remove this check)");
            Debug.Assert(valueExpression != null, "Field 'valueExpression' cannot be null (use Null=\"allow\" in BoundNodes.xml to remove this check)");
            Debug.Assert(typeExpression != null, "Field 'typeExpression' cannot be null (use Null=\"allow\" in BoundNodes.xml to remove this check)");

            this.ValueSymbol = valueSymbol;
            this.ValueExpression = valueExpression;
            this.ValueDiagnostics = valueDiagnostics;
            this.TypeExpression = typeExpression;
            this.TypeDiagnostics = typeDiagnostics;
        }

        // operator==, operator!=, GetHashCode, and Equals are needed by the generated bound tree.

        public static bool operator ==(BoundTypeOrValueData a, BoundTypeOrValueData b)
        {
            return (object)a.ValueSymbol == (object)b.ValueSymbol &&
                (object)a.ValueExpression == (object)b.ValueExpression &&
                a.ValueDiagnostics == b.ValueDiagnostics &&
                (object)a.TypeExpression == (object)b.TypeExpression &&
                a.TypeDiagnostics == b.TypeDiagnostics;
        }

        public static bool operator !=(BoundTypeOrValueData a, BoundTypeOrValueData b)
        {
            return !(a == b);
        }

        public override bool Equals(object? obj)
        {
            return obj is BoundTypeOrValueData && (BoundTypeOrValueData)obj == this;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(ValueSymbol.GetHashCode(),
                Hash.Combine(ValueExpression.GetHashCode(),
                Hash.Combine(ValueDiagnostics.GetHashCode(),
                Hash.Combine(TypeExpression.GetHashCode(), TypeDiagnostics.GetHashCode()))));
        }

        bool System.IEquatable<BoundTypeOrValueData>.Equals(BoundTypeOrValueData b)
        {
            return b == this;
        }
    }

    internal partial class BoundTupleExpression
    {
        /// <summary>
        /// Applies action to all the nested elements of this tuple.
        /// </summary>
        internal void VisitAllElements<T>(Action<BoundExpression, T> action, T args)
        {
            foreach (var argument in this.Arguments)
            {
                if (argument.Kind == BoundKind.TupleLiteral)
                {
                    ((BoundTupleExpression)argument).VisitAllElements(action, args);
                }
                else
                {
                    action(argument, args);
                }
            }
        }
    }
}
