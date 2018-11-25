// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundExpression
    {
        public virtual ConstantValue ConstantValue
        {
            get
            {
                return null;
            }
        }

        public virtual Symbol ExpressionSymbol
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
    }

    internal partial class BoundPassByCopy
    {
        public override ConstantValue ConstantValue
        {
            get
            {
                Debug.Assert(Expression.ConstantValue == null);
                return null;
            }
        }

        public override Symbol ExpressionSymbol
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

        /// <summary>
        /// The set of method symbols from which this call's method was chosen. 
        /// Only kept in the tree if the call was an error and overload resolution
        /// was unable to choose a best method.
        /// </summary>
        // (Note that this property is not automatically generated; we typically
        // will not be visiting or rewriting this error-recovery information.)
        //
        // DevDiv 1087283 tracks deciding whether or not to refactor this into BoundNodes.xml.
        public ImmutableArray<MethodSymbol> OriginalMethodsOpt { get; private set; }
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
                ErrorTypeSymbol errorType = this.Type.OriginalDefinition as ErrorTypeSymbol;
                if ((object)errorType != null)
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
        public override ConstantValue ConstantValue
        {
            get { return this.ConstantValueOpt; }
        }

        public override Symbol ExpressionSymbol
        {
            get { return this.LocalSymbol; }
        }

        public BoundLocal(SyntaxNode syntax, LocalSymbol localSymbol, ConstantValue constantValueOpt, TypeSymbol type, bool hasErrors)
            : this(syntax, localSymbol, false, constantValueOpt, type, hasErrors)
        {
        }

        public BoundLocal(SyntaxNode syntax, LocalSymbol localSymbol, ConstantValue constantValueOpt, TypeSymbol type)
            : this(syntax, localSymbol, false, constantValueOpt, type)
        {
        }

        public BoundLocal Update(LocalSymbol localSymbol, ConstantValue constantValueOpt, TypeSymbol type)
        {
            return this.Update(localSymbol, this.IsDeclaration, constantValueOpt, type);
        }
    }

    internal partial class BoundFieldAccess
    {
        public override ConstantValue ConstantValue
        {
            get { return this.ConstantValueOpt; }
        }

        public override Symbol ExpressionSymbol
        {
            get { return this.FieldSymbol; }
        }
    }

    internal partial class BoundPropertyAccess
    {
        public override Symbol ExpressionSymbol
        {
            get { return this.PropertySymbol; }
        }
    }

    internal partial class BoundIndexerAccess
    {
        public override Symbol ExpressionSymbol
        {
            get { return this.Indexer; }
        }

        /// <summary>
        /// The set of indexer symbols from which this call's indexer was chosen. 
        /// Only kept in the tree if the call was an error and overload resolution
        /// was unable to choose a best indexer.
        /// </summary>
        // (Note that this property is not automatically generated; we typically
        // will not be visiting or rewriting this error-recovery information.)
        //
        // DevDiv 1087283 tracks deciding whether or not to refactor this into BoundNodes.xml.
        public ImmutableArray<PropertySymbol> OriginalIndexersOpt { get; private set; }

        public BoundIndexerAccess Update(bool useSetterForDefaultArgumentGeneration)
        {
            if (useSetterForDefaultArgumentGeneration != this.UseSetterForDefaultArgumentGeneration)
            {
                var result = new BoundIndexerAccess(
                   this.Syntax,
                   this.ReceiverOpt,
                   this.Indexer,
                   this.Arguments,
                   this.ArgumentNamesOpt,
                   this.ArgumentRefKindsOpt,
                   this.Expanded,
                   this.ArgsToParamsOpt,
                   this.BinderOpt,
                   useSetterForDefaultArgumentGeneration,
                   this.Type,
                   this.HasErrors)
                {
                    WasCompilerGenerated = this.WasCompilerGenerated,
                    OriginalIndexersOpt = this.OriginalIndexersOpt
                };

                return result;
            }

            return this;
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
        internal string TryGetIndexedPropertyName()
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
        public override ConstantValue ConstantValue
        {
            get { return this.ConstantValueOpt; }
        }

        public override Symbol ExpressionSymbol
        {
            get { return this.MethodOpt; }
        }

        /// <summary>
        /// The set of method symbols from which this operator's method was chosen. 
        /// Only kept in the tree if the operator was an error and overload resolution
        /// was unable to choose a best method.
        /// </summary>
        // (Note that this property is not automatically generated; we typically
        // will not be visiting or rewriting this error-recovery information.)
        //
        // DevDiv 1087283 tracks deciding whether or not to refactor this into BoundNodes.xml.
        public ImmutableArray<MethodSymbol> OriginalUserDefinedOperatorsOpt { get; }
    }

    internal partial class BoundUserDefinedConditionalLogicalOperator
    {
        public override Symbol ExpressionSymbol
        {
            get { return this.LogicalOperator; }
        }

        /// <summary>
        /// The set of method symbols from which this operator's method was chosen. 
        /// Only kept in the tree if the operator was an error and overload resolution
        /// was unable to choose a best method.
        /// </summary>
        // (Note that this property is not automatically generated; we typically
        // will not be visiting or rewriting this error-recovery information.)
        //
        // DevDiv 1087283 tracks deciding whether or not to refactor this into BoundNodes.xml.
        public ImmutableArray<MethodSymbol> OriginalUserDefinedOperatorsOpt { get; }
    }

    internal partial class BoundUnaryOperator
    {
        public override ConstantValue ConstantValue
        {
            get { return this.ConstantValueOpt; }
        }

        public override Symbol ExpressionSymbol
        {
            get { return this.MethodOpt; }
        }

        /// <summary>
        /// The set of method symbols from which this operator's method was chosen. 
        /// Only kept in the tree if the operator was an error and overload resolution
        /// was unable to choose a best method.
        /// </summary>
        // (Note that this property is not automatically generated; we typically
        // will not be visiting or rewriting this error-recovery information.)
        //
        // DevDiv 1087283 tracks deciding whether or not to refactor this into BoundNodes.xml.
        public ImmutableArray<MethodSymbol> OriginalUserDefinedOperatorsOpt { get; }
    }

    internal partial class BoundIncrementOperator
    {
        public override Symbol ExpressionSymbol
        {
            get { return this.MethodOpt; }
        }

        /// <summary>
        /// The set of method symbols from which this operator's method was chosen. 
        /// Only kept in the tree if the operator was an error and overload resolution
        /// was unable to choose a best method.
        /// </summary>
        // (Note that this property is not automatically generated; we typically
        // will not be visiting or rewriting this error-recovery information.)
        //
        // DevDiv 1087283 tracks deciding whether or not to refactor this into BoundNodes.xml.
        public ImmutableArray<MethodSymbol> OriginalUserDefinedOperatorsOpt { get; }
    }

    internal partial class BoundCompoundAssignmentOperator
    {
        public override Symbol ExpressionSymbol
        {
            get { return this.Operator.Method; }
        }

        /// <summary>
        /// The set of method symbols from which this operator's method was chosen. 
        /// Only kept in the tree if the operator was an error and overload resolution
        /// was unable to choose a best method.
        /// </summary>
        // (Note that this property is not automatically generated; we typically
        // will not be visiting or rewriting this error-recovery information.)
        //
        // DevDiv 1087283 tracks deciding whether or not to refactor this into BoundNodes.xml.
        public ImmutableArray<MethodSymbol> OriginalUserDefinedOperatorsOpt { get; }
    }

    internal partial class BoundLiteral
    {
        public override ConstantValue ConstantValue
        {
            get { return this.ConstantValueOpt; }
        }
    }

    internal partial class BoundConversion
    {
        public override ConstantValue ConstantValue
        {
            get { return this.ConstantValueOpt; }
        }

        public ConversionKind ConversionKind
        {
            get { return this.Conversion.Kind; }
        }

        public bool IsExtensionMethod
        {
            get { return this.Conversion.IsExtensionMethod; }
        }

        public MethodSymbol SymbolOpt
        {
            get { return this.Conversion.Method; }
        }

        public override Symbol ExpressionSymbol
        {
            get { return this.SymbolOpt; }
        }

        /// <summary>
        /// The set of method symbols from which this conversion's method was chosen. 
        /// Only kept in the tree if the conversion was an error and overload resolution
        /// was unable to choose a best method.
        /// </summary>
        // (Note that this property is not automatically generated; we typically
        // will not be visiting or rewriting this error-recovery information.)
        //
        // DevDiv 1087283 tracks deciding whether or not to refactor this into BoundNodes.xml.
        public ImmutableArray<MethodSymbol> OriginalUserDefinedConversionsOpt { get; }

        public override bool SuppressVirtualCalls
        {
            get { return this.IsBaseConversion; }
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
        public override ConstantValue ConstantValue
        {
            get { return this.ConstantValueOpt; }
        }

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
            BoundObjectInitializerExpressionBase newInitializerExpression,
            TypeSymbol changeTypeOpt = null)
        {
            return Update(
                constructor: Constructor,
                arguments: newArguments,
                argumentNamesOpt: default(ImmutableArray<string>),
                argumentRefKindsOpt: newRefKinds,
                expanded: false,
                argsToParamsOpt: default(ImmutableArray<int>),
                constantValueOpt: ConstantValueOpt,
                initializerExpressionOpt: newInitializerExpression,
                binderOpt: BinderOpt,
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
        public override Symbol ExpressionSymbol
        {
            get { return this.Constructor; }
        }
    }

    internal partial class BoundDefaultExpression
    {
        public override ConstantValue ConstantValue
        {
            get { return this.ConstantValueOpt; }
        }
    }

    internal partial class BoundConditionalOperator
    {
        public override ConstantValue ConstantValue
        {
            get
            {
                return this.ConstantValueOpt;
            }
        }

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

    internal partial class BoundSizeOfOperator
    {
        public override ConstantValue ConstantValue
        {
            get
            {
                return this.ConstantValueOpt;
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
        public override Symbol ExpressionSymbol
        {
            get
            {
                return this.MemberSymbol;
            }
        }
    }

    internal partial class BoundAwaitExpression : BoundExpression
    {
        internal bool IsDynamic
        {
            get { return (object)this.GetResult == null; }
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

    internal partial class BoundNameOfOperator
    {
        public override ConstantValue ConstantValue
        {
            get
            {
                return this.ConstantValueOpt;
            }
        }
    }

    // NOTE: this type exists in order to hide the presence of {Value,Type}Expression inside of a
    //       BoundTypeOrValueExpression from the bound tree generator, which would otherwise generate
    //       a constructor that may spuriously set hasErrors to true if either field had errors.
    //       A BoundTypeOrValueExpression should never have errors if it is present in the tree.
    internal struct BoundTypeOrValueData : System.IEquatable<BoundTypeOrValueData>
    {
        public Symbol ValueSymbol { get; }
        public BoundExpression ValueExpression { get; }
        public DiagnosticBag ValueDiagnostics { get; }
        public BoundExpression TypeExpression { get; }
        public DiagnosticBag TypeDiagnostics { get; }

        public BoundTypeOrValueData(Symbol valueSymbol, BoundExpression valueExpression, DiagnosticBag valueDiagnostics, BoundExpression typeExpression, DiagnosticBag typeDiagnostics)
        {
            Debug.Assert(valueSymbol != null, "Field 'valueSymbol' cannot be null (use Null=\"allow\" in BoundNodes.xml to remove this check)");
            Debug.Assert(valueExpression != null, "Field 'valueExpression' cannot be null (use Null=\"allow\" in BoundNodes.xml to remove this check)");
            Debug.Assert(valueDiagnostics != null, "Field 'valueDiagnostics' cannot be null (use Null=\"allow\" in BoundNodes.xml to remove this check)");
            Debug.Assert(typeExpression != null, "Field 'typeExpression' cannot be null (use Null=\"allow\" in BoundNodes.xml to remove this check)");
            Debug.Assert(typeDiagnostics != null, "Field 'typeDiagnostics' cannot be null (use Null=\"allow\" in BoundNodes.xml to remove this check)");

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
                (object)a.ValueDiagnostics == (object)b.ValueDiagnostics &&
                (object)a.TypeExpression == (object)b.TypeExpression &&
                (object)a.TypeDiagnostics == (object)b.TypeDiagnostics;
        }

        public static bool operator !=(BoundTypeOrValueData a, BoundTypeOrValueData b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
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
