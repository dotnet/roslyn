// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class BoundExpression
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

    partial class BoundCall
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

    partial class BoundTypeExpression
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

    partial class BoundNamespaceExpression
    {
        public override Symbol ExpressionSymbol
        {
            get { return this.AliasOpt ?? (Symbol)this.NamespaceSymbol; }
        }
    }

    partial class BoundLocal
    {
        public override ConstantValue ConstantValue
        {
            get { return this.ConstantValueOpt; }
        }

        public override Symbol ExpressionSymbol
        {
            get { return this.LocalSymbol; }
        }
    }

    partial class BoundFieldAccess
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

    partial class BoundPropertyAccess
    {
        public override Symbol ExpressionSymbol
        {
            get { return this.PropertySymbol; }
        }
    }

    partial class BoundIndexerAccess
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

        public override LookupResultKind ResultKind
        {
            get
            {
                return !this.OriginalIndexersOpt.IsDefault ? LookupResultKind.OverloadResolutionFailure : base.ResultKind;
            }
        }
    }

    partial class BoundDynamicIndexerAccess
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

    partial class BoundEventAccess
    {
        public override Symbol ExpressionSymbol
        {
            get { return this.EventSymbol; }
        }
    }

    partial class BoundParameter
    {
        public override Symbol ExpressionSymbol
        {
            get { return this.ParameterSymbol; }
        }
    }

    partial class BoundBinaryOperator
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
        public ImmutableArray<MethodSymbol> OriginalUserDefinedOperatorsOpt { get; private set; }
    }

    partial class BoundUserDefinedConditionalLogicalOperator
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
        public ImmutableArray<MethodSymbol> OriginalUserDefinedOperatorsOpt { get; private set; }
    }

    partial class BoundUnaryOperator
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
        public ImmutableArray<MethodSymbol> OriginalUserDefinedOperatorsOpt { get; private set; }
    }

    partial class BoundIncrementOperator
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
        public ImmutableArray<MethodSymbol> OriginalUserDefinedOperatorsOpt { get; private set; }
    }

    partial class BoundCompoundAssignmentOperator
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
        public ImmutableArray<MethodSymbol> OriginalUserDefinedOperatorsOpt { get; private set; }
    }

    partial class BoundLiteral
    {
        public override ConstantValue ConstantValue
        {
            get { return this.ConstantValueOpt; }
        }
    }

    partial class BoundConversion
    {
        public override ConstantValue ConstantValue
        {
            get { return this.ConstantValueOpt; }
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
        public ImmutableArray<MethodSymbol> OriginalUserDefinedConversionsOpt { get; private set; }

        public override bool SuppressVirtualCalls
        {
            get { return this.IsBaseConversion; }
        }
    }

    partial class BoundObjectCreationExpression
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
            BoundExpression newInitializerExpression,
            TypeSymbol changeTypeOpt = null)
        {
            return Update(
                constructor: Constructor,
                arguments: newArguments,
                argumentNamesOpt: default(ImmutableArray<string>),
                argumentRefKindsOpt: ArgumentRefKindsOpt,
                expanded: false,
                argsToParamsOpt: default(ImmutableArray<int>),
                constantValueOpt: ConstantValueOpt,
                initializerExpressionOpt: newInitializerExpression,
                type: changeTypeOpt ?? Type);
        }
    }

    partial class BoundAnonymousObjectCreationExpression
    {
        public override Symbol ExpressionSymbol
        {
            get { return this.Constructor; }
        }
    }

    partial class BoundAnonymousPropertyDeclaration
    {
        public override Symbol ExpressionSymbol
        {
            get { return this.Property; }
        }
    }

    partial class BoundLambda
    {
        public override Symbol ExpressionSymbol
        {
            get { return this.Symbol; }
        }
    }

    partial class BoundAttribute
    {
        public override Symbol ExpressionSymbol
        {
            get { return this.Constructor; }
        }
    }

    partial class BoundDefaultOperator
    {
        public override ConstantValue ConstantValue
        {
            get { return this.ConstantValueOpt; }
        }
    }

    partial class BoundConditionalOperator
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

    partial class BoundSizeOfOperator
    {
        public override ConstantValue ConstantValue
        {
            get
            {
                return this.ConstantValueOpt;
            }
        }
    }

    partial class BoundRangeVariable
    {
        public override Symbol ExpressionSymbol
        {
            get
            {
                return this.RangeVariableSymbol;
            }
        }
    }

    partial class BoundLabel
    {
        public override Symbol ExpressionSymbol
        {
            get
            {
                return this.Label;
            }
        }
    }

    partial class BoundObjectInitializerMember
    {
        public override Symbol ExpressionSymbol
        {
            get
            {
                return this.MemberSymbol;
            }
        }
    }

    partial class BoundAwaitExpression : BoundExpression
    {
        internal bool IsDynamic
        {
            get { return (object)this.GetResult == null; }
        }
    }

    partial class BoundCollectionElementInitializer
    {
        public override Symbol ExpressionSymbol
        {
            get
            {
                return this.AddMethod;
            }
        }
    }

    partial class BoundBaseReference
    {
        public override bool SuppressVirtualCalls
        {
            get { return true; }
        }
    }

    partial class BoundNameOfOperator
    {
        public override ConstantValue ConstantValue
        {
            get
            {
                return this.ConstantValueOpt;
            }
        }
    }

    internal sealed partial class BoundTypeOrValueExpression : BoundExpression
    {
        // NOTE: this constructor should always be used instead of the generated constructor, since the generated
        //       constructor may spuriously set hasErrors to true if valueExpression or typeExpression have errors.
        //       This node should never have errors if it is present in the tree.
        public BoundTypeOrValueExpression(CSharpSyntaxNode syntax, Symbol valueSymbol, BoundExpression valueExpression, ImmutableArray<Diagnostic> valueDiagnostics, BoundExpression typeExpression, ImmutableArray<Diagnostic> typeDiagnostics, TypeSymbol type)
            : base(BoundKind.TypeOrValueExpression, syntax, type, hasErrors: false)
        {
            Debug.Assert(valueSymbol != null, "Field 'valueSymbol' cannot be null (use Null=\"allow\" in BoundNodes.xml to remove this check)");
            Debug.Assert(valueExpression != null, "Field 'valueExpression' cannot be null (use Null=\"allow\" in BoundNodes.xml to remove this check)");
            Debug.Assert(!valueDiagnostics.IsDefault, "Field 'valueDiagnostics' cannot be null (use Null=\"allow\" in BoundNodes.xml to remove this check)");
            Debug.Assert(typeExpression != null, "Field 'typeExpression' cannot be null (use Null=\"allow\" in BoundNodes.xml to remove this check)");
            Debug.Assert(!typeDiagnostics.IsDefault, "Field 'typeDiagnostics' cannot be null (use Null=\"allow\" in BoundNodes.xml to remove this check)");
            Debug.Assert(type != null, "Field 'type' cannot be null (use Null=\"allow\" in BoundNodes.xml to remove this check)");

            this.ValueSymbol = valueSymbol;
            this.ValueExpression = valueExpression;
            this.ValueDiagnostics = valueDiagnostics;
            this.TypeExpression = typeExpression;
            this.TypeDiagnostics = typeDiagnostics;
        }
    }
}
