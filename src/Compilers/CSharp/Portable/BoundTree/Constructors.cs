// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class BoundFieldAccess
    {
        public BoundFieldAccess(
            CSharpSyntaxNode syntax,
            BoundExpression receiver,
            FieldSymbol fieldSymbol,
            ConstantValue constantValueOpt,
            bool hasErrors = false)
            : this(syntax, receiver, fieldSymbol, constantValueOpt, LookupResultKind.Viable, fieldSymbol.Type, hasErrors)
        {
        }

        public BoundFieldAccess(
            CSharpSyntaxNode syntax,
            BoundExpression receiver,
            FieldSymbol fieldSymbol,
            ConstantValue constantValueOpt,
            LookupResultKind resultKind,
            TypeSymbol type,
            bool hasErrors = false)
            : this(syntax, receiver, fieldSymbol, constantValueOpt, resultKind, NeedsByValueFieldAccess(receiver, fieldSymbol), type, hasErrors)
        {
        }

        public BoundFieldAccess Update(
            BoundExpression receiver,
            FieldSymbol fieldSymbol,
            ConstantValue constantValueOpt,
            LookupResultKind resultKind,
            TypeSymbol typeSymbol)
        {
            return this.Update(receiver, fieldSymbol, constantValueOpt, resultKind, this.IsByValue, typeSymbol);
        }

        private static bool NeedsByValueFieldAccess(BoundExpression receiver, FieldSymbol fieldSymbol)
        {
            if (fieldSymbol.IsStatic ||
                !fieldSymbol.ContainingType.IsValueType ||
                (object)receiver == null) // receiver may be null in error cases
            {
                return false;
            }

            switch (receiver.Kind)
            {
                case BoundKind.FieldAccess:
                    return ((BoundFieldAccess)receiver).IsByValue;

                case BoundKind.Local:
                    return !((BoundLocal)receiver).LocalSymbol.IsWritable;

                default:
                    return false;
            }
        }
    }

    internal partial class BoundCall
    {
        public static BoundCall ErrorCall(
            CSharpSyntaxNode node,
            BoundExpression receiverOpt,
            MethodSymbol method,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<string> namedArguments,
            ImmutableArray<RefKind> refKinds,
            bool isDelegateCall,
            bool invokedAsExtensionMethod,
            ImmutableArray<MethodSymbol> originalMethods,
            LookupResultKind resultKind)
        {
            if (!originalMethods.IsEmpty)
                resultKind = resultKind.WorseResultKind(LookupResultKind.OverloadResolutionFailure);

            Debug.Assert(arguments.IsDefaultOrEmpty || (object)receiverOpt != (object)arguments[0]);

            var call = new BoundCall(node, receiverOpt, method, arguments, namedArguments,
                refKinds, isDelegateCall: isDelegateCall, expanded: false, invokedAsExtensionMethod: invokedAsExtensionMethod, argsToParamsOpt: default(ImmutableArray<int>),
                resultKind: resultKind, type: method.ReturnType, hasErrors: true);
            call.OriginalMethodsOpt = originalMethods;
            return call;
        }

        public BoundCall Update(BoundExpression receiverOpt, MethodSymbol method, ImmutableArray<BoundExpression> arguments)
        {
            return this.Update(receiverOpt, method, arguments, ArgumentNamesOpt, ArgumentRefKindsOpt, IsDelegateCall, Expanded, InvokedAsExtensionMethod, ArgsToParamsOpt, ResultKind, Type);
        }

        public static BoundCall Synthesized(CSharpSyntaxNode syntax, BoundExpression receiverOpt, MethodSymbol method)
        {
            return Synthesized(syntax, receiverOpt, method, ImmutableArray<BoundExpression>.Empty);
        }

        public static BoundCall Synthesized(CSharpSyntaxNode syntax, BoundExpression receiverOpt, MethodSymbol method, BoundExpression arg0)
        {
            return Synthesized(syntax, receiverOpt, method, ImmutableArray.Create(arg0));
        }

        public static BoundCall Synthesized(CSharpSyntaxNode syntax, BoundExpression receiverOpt, MethodSymbol method, BoundExpression arg0, BoundExpression arg1)
        {
            return Synthesized(syntax, receiverOpt, method, ImmutableArray.Create(arg0, arg1));
        }

        public static BoundCall Synthesized(CSharpSyntaxNode syntax, BoundExpression receiverOpt, MethodSymbol method, ImmutableArray<BoundExpression> arguments)
        {
            return new BoundCall(syntax,
                    receiverOpt,
                    method,
                    arguments,
                    argumentNamesOpt: default(ImmutableArray<string>),
                    argumentRefKindsOpt: method.ParameterRefKinds,
                    isDelegateCall: false,
                    expanded: false,
                    invokedAsExtensionMethod: false,
                    argsToParamsOpt: default(ImmutableArray<int>),
                    resultKind: LookupResultKind.Viable,
                    type: method.ReturnType,
                    hasErrors: false
                )
            { WasCompilerGenerated = true };
        }
    }

    internal sealed partial class BoundObjectCreationExpression
    {
        public BoundObjectCreationExpression(CSharpSyntaxNode syntax, MethodSymbol constructor, params BoundExpression[] arguments)
            : this(syntax, constructor, ImmutableArray.Create<BoundExpression>(arguments), default(ImmutableArray<string>), default(ImmutableArray<RefKind>), false, default(ImmutableArray<int>), null, null, constructor.ContainingType)
        {
        }
        public BoundObjectCreationExpression(CSharpSyntaxNode syntax, MethodSymbol constructor, ImmutableArray<BoundExpression> arguments)
            : this(syntax, constructor, arguments, default(ImmutableArray<string>), default(ImmutableArray<RefKind>), false, default(ImmutableArray<int>), null, null, constructor.ContainingType)
        {
        }
    }

    internal partial class BoundIndexerAccess
    {
        public static BoundIndexerAccess ErrorAccess(
            CSharpSyntaxNode node,
            BoundExpression receiverOpt,
            PropertySymbol indexer,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<string> namedArguments,
            ImmutableArray<RefKind> refKinds,
            ImmutableArray<PropertySymbol> originalIndexers)
        {
            return new BoundIndexerAccess(
                node,
                receiverOpt,
                indexer,
                arguments,
                namedArguments,
                refKinds,
                expanded: false,
                argsToParamsOpt: default(ImmutableArray<int>),
                type: indexer.Type,
                hasErrors: true)
            {
                OriginalIndexersOpt = originalIndexers
            };
        }
    }

    internal sealed partial class BoundConversion
    {
        /// <remarks>
        /// This method is intended for passes other than the LocalRewriter.
        /// Use MakeConversion helper method in the LocalRewriter instead,
        /// it generates a synthesized conversion in its lowered form.
        /// </remarks>
        public static BoundConversion SynthesizedNonUserDefined(CSharpSyntaxNode syntax, BoundExpression operand, ConversionKind kind, TypeSymbol type, ConstantValue constantValueOpt = null)
        {
            // We need more information than just the conversion kind for creating a synthesized user defined conversion.
            Debug.Assert(!kind.IsUserDefinedConversion(), "Use the BoundConversion.Synthesized overload that takes a 'Conversion' parameter for generating synthesized user defined conversions.");

            return new BoundConversion(
                syntax,
                operand,
                kind,
                resultKind: LookupResultKind.Viable, //not used
                isBaseConversion: false,
                symbolOpt: null,
                @checked: false,
                explicitCastInCode: false,
                isExtensionMethod: false,
                isArrayIndex: false,
                constantValueOpt: constantValueOpt,
                type: type)
            { WasCompilerGenerated = true };
        }

        /// <remarks>
        /// NOTE:    This method is intended for passes other than the LocalRewriter.
        /// NOTE:    Use MakeConversion helper method in the LocalRewriter instead,
        /// NOTE:    it generates a synthesized conversion in its lowered form.
        /// </remarks>
        public static BoundConversion Synthesized(
            CSharpSyntaxNode syntax,
            BoundExpression operand,
            Conversion conversion,
            bool @checked,
            bool explicitCastInCode,
            ConstantValue constantValueOpt,
            TypeSymbol type,
            bool hasErrors = false)
        {
            return new BoundConversion(
                syntax,
                operand,
                conversion,
                @checked,
                explicitCastInCode,
                constantValueOpt,
                type,
                hasErrors || !conversion.IsValid)
            {
                WasCompilerGenerated = true
            };
        }

        public BoundConversion(
            CSharpSyntaxNode syntax,
            BoundExpression operand,
            Conversion conversion,
            bool @checked,
            bool explicitCastInCode,
            ConstantValue constantValueOpt,
            TypeSymbol type,
            bool hasErrors = false)
            : this(
                syntax,
                operand,
                conversion.Kind,
                resultKind: conversion.ResultKind,
                isBaseConversion: false,
                symbolOpt: conversion.Method,
                @checked: @checked,
                explicitCastInCode: explicitCastInCode,
                isExtensionMethod: conversion.IsExtensionMethod,
                isArrayIndex: conversion.IsArrayIndex,
                constantValueOpt: constantValueOpt,
                type: type,
                hasErrors: hasErrors || !conversion.IsValid)
        {
            OriginalUserDefinedConversionsOpt = conversion.OriginalUserDefinedConversions;
        }
    }

    internal sealed partial class BoundUnaryOperator
    {
        internal BoundUnaryOperator(
            CSharpSyntaxNode syntax,
            UnaryOperatorKind operatorKind,
            BoundExpression operand,
            ConstantValue constantValueOpt,
            MethodSymbol methodOpt,
            LookupResultKind resultKind,
            ImmutableArray<MethodSymbol> originalUserDefinedOperatorsOpt,
            TypeSymbol type,
            bool hasErrors = false)
            : this(
                syntax,
                operatorKind,
                operand,
                constantValueOpt,
                methodOpt,
                resultKind,
                type,
                hasErrors)
        {
            this.OriginalUserDefinedOperatorsOpt = originalUserDefinedOperatorsOpt;
        }
    }

    internal sealed partial class BoundIncrementOperator
    {
        public BoundIncrementOperator(
            CSharpSyntaxNode syntax,
            UnaryOperatorKind operatorKind,
            BoundExpression operand,
            MethodSymbol methodOpt,
            Conversion operandConversion,
            Conversion resultConversion,
            LookupResultKind resultKind,
            ImmutableArray<MethodSymbol> originalUserDefinedOperatorsOpt,
            TypeSymbol type,
            bool hasErrors = false)
            : this(
                syntax,
                operatorKind,
                operand,
                methodOpt,
                operandConversion,
                resultConversion,
                resultKind,
                type,
                hasErrors)
        {
            this.OriginalUserDefinedOperatorsOpt = originalUserDefinedOperatorsOpt;
        }
    }

    internal sealed partial class BoundBinaryOperator
    {
        public BoundBinaryOperator(
            CSharpSyntaxNode syntax,
            BinaryOperatorKind operatorKind,
            BoundExpression left,
            BoundExpression right,
            ConstantValue constantValueOpt,
            MethodSymbol methodOpt,
            LookupResultKind resultKind,
            ImmutableArray<MethodSymbol> originalUserDefinedOperatorsOpt,
            TypeSymbol type,
            bool hasErrors = false)
            : this(
                syntax,
                operatorKind,
                left,
                right,
                constantValueOpt,
                methodOpt,
                resultKind,
                type,
                hasErrors)
        {
            this.OriginalUserDefinedOperatorsOpt = originalUserDefinedOperatorsOpt;
        }
    }

    internal sealed partial class BoundUserDefinedConditionalLogicalOperator
    {
        public BoundUserDefinedConditionalLogicalOperator(
            CSharpSyntaxNode syntax,
            BinaryOperatorKind operatorKind,
            BoundExpression left,
            BoundExpression right,
            MethodSymbol logicalOperator,
            MethodSymbol trueOperator,
            MethodSymbol falseOperator,
            LookupResultKind resultKind,
            ImmutableArray<MethodSymbol> originalUserDefinedOperatorsOpt,
            TypeSymbol type,
            bool hasErrors = false)
            : this(
                syntax,
                operatorKind,
                left,
                right,
                logicalOperator,
                trueOperator,
                falseOperator,
                resultKind,
                type,
                hasErrors)
        {
            Debug.Assert(operatorKind.IsUserDefined() && operatorKind.IsLogical());

            this.OriginalUserDefinedOperatorsOpt = originalUserDefinedOperatorsOpt;
        }
    }


    internal sealed partial class BoundCompoundAssignmentOperator
    {
        public BoundCompoundAssignmentOperator(
            CSharpSyntaxNode syntax,
            BinaryOperatorSignature @operator,
            BoundExpression left,
            BoundExpression right,
            Conversion leftConversion,
            Conversion finalConversion,
            LookupResultKind resultKind,
            ImmutableArray<MethodSymbol> originalUserDefinedOperatorsOpt,
            TypeSymbol type,
            bool hasErrors = false)
            : this(
                syntax,
                @operator,
                left,
                right,
                leftConversion,
                finalConversion,
                resultKind,
                type,
                hasErrors)
        {
            this.OriginalUserDefinedOperatorsOpt = originalUserDefinedOperatorsOpt;
        }
    }

    internal sealed partial class BoundParameter
    {
        public BoundParameter(CSharpSyntaxNode syntax, ParameterSymbol parameterSymbol, bool hasErrors = false)
            : this(syntax, parameterSymbol, parameterSymbol.Type, hasErrors)
        {
        }

        public BoundParameter(CSharpSyntaxNode syntax, ParameterSymbol parameterSymbol)
            : this(syntax, parameterSymbol, parameterSymbol.Type)
        {
        }
    }

    internal sealed partial class BoundTypeExpression
    {
        public BoundTypeExpression(CSharpSyntaxNode syntax, AliasSymbol aliasOpt, TypeSymbol type, bool hasErrors = false)
            : this(syntax, aliasOpt, false, null, type, hasErrors)
        {
        }

        public BoundTypeExpression(CSharpSyntaxNode syntax, AliasSymbol aliasOpt, TypeSymbol type)
            : this(syntax, aliasOpt, false, null, type)
        {
        }

        public BoundTypeExpression(CSharpSyntaxNode syntax, AliasSymbol aliasOpt, bool inferredType, TypeSymbol type, bool hasErrors = false)
            : this(syntax, aliasOpt, inferredType, null, type, hasErrors)
        {
        }
    }

    internal sealed partial class BoundNamespaceExpression
    {
        public BoundNamespaceExpression(CSharpSyntaxNode syntax, NamespaceSymbol namespaceSymbol, bool hasErrors = false)
            : this(syntax, namespaceSymbol, null, hasErrors)
        {
        }

        public BoundNamespaceExpression(CSharpSyntaxNode syntax, NamespaceSymbol namespaceSymbol)
            : this(syntax, namespaceSymbol, null)
        {
        }

        public BoundNamespaceExpression Update(NamespaceSymbol namespaceSymbol)
        {
            return Update(namespaceSymbol, this.AliasOpt);
        }
    }

    internal sealed partial class BoundAssignmentOperator
    {
        public BoundAssignmentOperator(CSharpSyntaxNode syntax, BoundExpression left, BoundExpression right,
            TypeSymbol type, RefKind refKind = RefKind.None, bool hasErrors = false)
            : this(syntax, left, right, refKind, type, hasErrors)
        {
        }
    }

    internal sealed partial class BoundBadExpression
    {
        public BoundBadExpression(CSharpSyntaxNode syntax, LookupResultKind resultKind, ImmutableArray<Symbol> symbols, ImmutableArray<BoundNode> childBoundNodes, TypeSymbol type)
            : this(syntax, resultKind, symbols, childBoundNodes, type, true)
        {
            Debug.Assert((object)type != null);
        }
    }

    internal partial class BoundStatementList
    {
        public static BoundStatementList Synthesized(CSharpSyntaxNode syntax, params BoundStatement[] statements)
        {
            return Synthesized(syntax, false, statements.AsImmutableOrNull());
        }

        public static BoundStatementList Synthesized(CSharpSyntaxNode syntax, bool hasErrors, params BoundStatement[] statements)
        {
            return Synthesized(syntax, hasErrors, statements.AsImmutableOrNull());
        }

        public static BoundStatementList Synthesized(CSharpSyntaxNode syntax, ImmutableArray<BoundStatement> statements)
        {
            return Synthesized(syntax, false, statements);
        }

        public static BoundStatementList Synthesized(CSharpSyntaxNode syntax, bool hasErrors, ImmutableArray<BoundStatement> statements)
        {
            return new BoundStatementList(syntax, statements, hasErrors) { WasCompilerGenerated = true };
        }
    }

    internal sealed partial class BoundReturnStatement
    {
        public static BoundReturnStatement Synthesized(CSharpSyntaxNode syntax, RefKind refKind, BoundExpression expression, bool hasErrors = false)
        {
            return new BoundReturnStatement(syntax, refKind, expression, hasErrors) { WasCompilerGenerated = true };
        }
    }

    internal sealed partial class BoundYieldBreakStatement
    {
        public static BoundYieldBreakStatement Synthesized(CSharpSyntaxNode syntax, bool hasErrors = false)
        {
            return new BoundYieldBreakStatement(syntax, hasErrors) { WasCompilerGenerated = true };
        }
    }

    internal sealed partial class BoundGotoStatement
    {
        public BoundGotoStatement(CSharpSyntaxNode syntax, LabelSymbol label, bool hasErrors = false)
            : this(syntax, label, caseExpressionOpt: null, labelExpressionOpt: null, hasErrors: hasErrors)
        {
        }
    }

    internal sealed partial class BoundSwitchLabel
    {
        public BoundSwitchLabel(CSharpSyntaxNode syntax, LabelSymbol label, bool hasErrors = false)
            : this(syntax, label, expressionOpt: null, hasErrors: hasErrors)
        {
        }
    }

    internal partial class BoundBlock
    {
        public static BoundBlock SynthesizedNoLocals(CSharpSyntaxNode syntax, BoundStatement statement)
        {
            return new BoundBlock(syntax, ImmutableArray<LocalSymbol>.Empty, ImmutableArray<LocalFunctionSymbol>.Empty,
                ImmutableArray.Create(statement))
            { WasCompilerGenerated = true };
        }

        public static BoundBlock SynthesizedNoLocals(CSharpSyntaxNode syntax, ImmutableArray<BoundStatement> statements)
        {
            return new BoundBlock(syntax, ImmutableArray<LocalSymbol>.Empty, ImmutableArray<LocalFunctionSymbol>.Empty, statements) { WasCompilerGenerated = true };
        }

        public static BoundBlock SynthesizedNoLocals(CSharpSyntaxNode syntax, params BoundStatement[] statements)
        {
            return new BoundBlock(syntax, ImmutableArray<LocalSymbol>.Empty, ImmutableArray<LocalFunctionSymbol>.Empty, statements.AsImmutableOrNull()) { WasCompilerGenerated = true };
        }
    }

    internal partial class BoundDefaultOperator
    {
        public BoundDefaultOperator(CSharpSyntaxNode syntax, TypeSymbol type)
            : this(syntax, type.GetDefaultValue(), type)
        {
        }
    }

    internal partial class BoundTryStatement
    {
        public BoundTryStatement(CSharpSyntaxNode syntax, BoundBlock tryBlock, ImmutableArray<BoundCatchBlock> catchBlocks, BoundBlock finallyBlockOpt)
            : this(syntax, tryBlock, catchBlocks, finallyBlockOpt, preferFaultHandler: false, hasErrors: false)
        {
        }
    }
}
