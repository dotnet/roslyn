// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class BoundFieldAccess
    {
        public BoundFieldAccess(
            SyntaxNode syntax,
            BoundExpression receiver,
            FieldSymbol fieldSymbol,
            ConstantValue constantValueOpt,
            bool hasErrors = false)
            : this(syntax, receiver, fieldSymbol, constantValueOpt, LookupResultKind.Viable, fieldSymbol.Type, hasErrors)
        {
        }

        public BoundFieldAccess(
            SyntaxNode syntax,
            BoundExpression receiver,
            FieldSymbol fieldSymbol,
            ConstantValue constantValueOpt,
            LookupResultKind resultKind,
            TypeSymbol type,
            bool hasErrors = false)
            : this(syntax, receiver, fieldSymbol, constantValueOpt, resultKind, NeedsByValueFieldAccess(receiver, fieldSymbol), isDeclaration: false, type: type, hasErrors: hasErrors)
        {
        }

        public BoundFieldAccess(
            SyntaxNode syntax,
            BoundExpression receiver,
            FieldSymbol fieldSymbol,
            ConstantValue constantValueOpt,
            LookupResultKind resultKind,
            bool isDeclaration,
            TypeSymbol type,
            bool hasErrors = false)
            : this(syntax, receiver, fieldSymbol, constantValueOpt, resultKind, NeedsByValueFieldAccess(receiver, fieldSymbol), isDeclaration: isDeclaration, type: type, hasErrors: hasErrors)
        {
        }

        public BoundFieldAccess Update(
            BoundExpression receiver,
            FieldSymbol fieldSymbol,
            ConstantValue constantValueOpt,
            LookupResultKind resultKind,
            TypeSymbol typeSymbol)
        {
            return this.Update(receiver, fieldSymbol, constantValueOpt, resultKind, this.IsByValue, this.IsDeclaration, typeSymbol);
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
                    var localSymbol = ((BoundLocal)receiver).LocalSymbol;
                    return !(localSymbol.IsWritableVariable || localSymbol.IsRef);

                default:
                    return false;
            }
        }
    }

    internal partial class BoundCall
    {
#nullable enable
        public BoundCall(
            SyntaxNode syntax,
            BoundExpression? receiverOpt,
            MethodSymbol method,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<string> argumentNamesOpt,
            ImmutableArray<RefKind> argumentRefKindsOpt,
            bool isDelegateCall,
            bool expanded,
            bool invokedAsExtensionMethod,
            ImmutableArray<int> argsToParamsOpt,
            LookupResultKind resultKind,
            Binder? binderOpt,
            TypeSymbol type,
            bool hasErrors = false) :
            this(syntax, receiverOpt, method, arguments, argumentNamesOpt, argumentRefKindsOpt, isDelegateCall, expanded, invokedAsExtensionMethod, argsToParamsOpt, resultKind, originalMethodsOpt: default, binderOpt, type, hasErrors)
        {
        }

        public BoundCall Update(BoundExpression? receiverOpt,
                                MethodSymbol method,
                                ImmutableArray<BoundExpression> arguments,
                                ImmutableArray<string> argumentNamesOpt,
                                ImmutableArray<RefKind> argumentRefKindsOpt,
                                bool isDelegateCall,
                                bool expanded,
                                bool invokedAsExtensionMethod,
                                ImmutableArray<int> argsToParamsOpt,
                                LookupResultKind resultKind,
                                Binder? binderOpt,
                                TypeSymbol type)
            => Update(receiverOpt, method, arguments, argumentNamesOpt, argumentRefKindsOpt, isDelegateCall, expanded, invokedAsExtensionMethod, argsToParamsOpt, resultKind, this.OriginalMethodsOpt, binderOpt, type);
#nullable restore

        public static BoundCall ErrorCall(
            SyntaxNode node,
            BoundExpression receiverOpt,
            MethodSymbol method,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<string> namedArguments,
            ImmutableArray<RefKind> refKinds,
            bool isDelegateCall,
            bool invokedAsExtensionMethod,
            ImmutableArray<MethodSymbol> originalMethods,
            LookupResultKind resultKind,
            Binder binder)
        {
            if (!originalMethods.IsEmpty)
                resultKind = resultKind.WorseResultKind(LookupResultKind.OverloadResolutionFailure);

            Debug.Assert(arguments.IsDefaultOrEmpty || (object)receiverOpt != (object)arguments[0]);

            return new BoundCall(node, receiverOpt, method, arguments, namedArguments,
                refKinds, isDelegateCall: isDelegateCall, expanded: false, invokedAsExtensionMethod: invokedAsExtensionMethod, argsToParamsOpt: default(ImmutableArray<int>),
                resultKind: resultKind, originalMethods, binderOpt: binder, type: method.ReturnType, hasErrors: true);
        }

        public BoundCall Update(ImmutableArray<BoundExpression> arguments)
        {
            return this.Update(ReceiverOpt, Method, arguments, ArgumentNamesOpt, ArgumentRefKindsOpt, IsDelegateCall, Expanded, InvokedAsExtensionMethod, ArgsToParamsOpt, ResultKind, OriginalMethodsOpt, BinderOpt, Type);
        }

        public BoundCall Update(BoundExpression receiverOpt, MethodSymbol method, ImmutableArray<BoundExpression> arguments)
        {
            return this.Update(receiverOpt, method, arguments, ArgumentNamesOpt, ArgumentRefKindsOpt, IsDelegateCall, Expanded, InvokedAsExtensionMethod, ArgsToParamsOpt, ResultKind, OriginalMethodsOpt, BinderOpt, Type);
        }

        public static BoundCall Synthesized(SyntaxNode syntax, BoundExpression receiverOpt, MethodSymbol method)
        {
            return Synthesized(syntax, receiverOpt, method, ImmutableArray<BoundExpression>.Empty);
        }

        public static BoundCall Synthesized(SyntaxNode syntax, BoundExpression receiverOpt, MethodSymbol method, BoundExpression arg0)
        {
            return Synthesized(syntax, receiverOpt, method, ImmutableArray.Create(arg0));
        }

        public static BoundCall Synthesized(SyntaxNode syntax, BoundExpression receiverOpt, MethodSymbol method, BoundExpression arg0, BoundExpression arg1)
        {
            return Synthesized(syntax, receiverOpt, method, ImmutableArray.Create(arg0, arg1));
        }

        public static BoundCall Synthesized(SyntaxNode syntax, BoundExpression receiverOpt, MethodSymbol method, ImmutableArray<BoundExpression> arguments)
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
                    originalMethodsOpt: default,
                    binderOpt: null,
                    type: method.ReturnType,
                    hasErrors: method.OriginalDefinition is ErrorMethodSymbol
                )
            { WasCompilerGenerated = true };
        }
    }

    internal sealed partial class BoundObjectCreationExpression
    {
        public BoundObjectCreationExpression(SyntaxNode syntax, MethodSymbol constructor, Binder binderOpt, params BoundExpression[] arguments)
            : this(syntax, constructor, ImmutableArray.Create<BoundExpression>(arguments), default(ImmutableArray<string>), default(ImmutableArray<RefKind>), false, default(ImmutableArray<int>), null, null, binderOpt, constructor.ContainingType)
        {
        }
        public BoundObjectCreationExpression(SyntaxNode syntax, MethodSymbol constructor, Binder binderOpt, ImmutableArray<BoundExpression> arguments)
            : this(syntax, constructor, arguments, default(ImmutableArray<string>), default(ImmutableArray<RefKind>), false, default(ImmutableArray<int>), null, null, binderOpt, constructor.ContainingType)
        {
        }
    }

    internal partial class BoundIndexerAccess
    {
        public static BoundIndexerAccess ErrorAccess(
            SyntaxNode node,
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
                binderOpt: null,
                useSetterForDefaultArgumentGeneration: false,
                originalIndexers,
                type: indexer.Type,
                hasErrors: true);
        }
#nullable enable
        public BoundIndexerAccess(
            SyntaxNode syntax,
            BoundExpression? receiverOpt,
            PropertySymbol indexer,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<string> argumentNamesOpt,
            ImmutableArray<RefKind> argumentRefKindsOpt,
            bool expanded,
            ImmutableArray<int> argsToParamsOpt,
            Binder? binderOpt,
            bool useSetterForDefaultArgumentGeneration,
            TypeSymbol type,
            bool hasErrors = false) :
            this(syntax, receiverOpt, indexer, arguments, argumentNamesOpt, argumentRefKindsOpt, expanded, argsToParamsOpt, binderOpt, useSetterForDefaultArgumentGeneration, originalIndexersOpt: default, type, hasErrors)
        { }


        public BoundIndexerAccess Update(BoundExpression? receiverOpt,
                                         PropertySymbol indexer,
                                         ImmutableArray<BoundExpression> arguments,
                                         ImmutableArray<string> argumentNamesOpt,
                                         ImmutableArray<RefKind> argumentRefKindsOpt,
                                         bool expanded,
                                         ImmutableArray<int> argsToParamsOpt,
                                         Binder? binderOpt,
                                         bool useSetterForDefaultArgumentGeneration,
                                         TypeSymbol type)
            => Update(receiverOpt, indexer, arguments, argumentNamesOpt, argumentRefKindsOpt, expanded, argsToParamsOpt, binderOpt, useSetterForDefaultArgumentGeneration, this.OriginalIndexersOpt, type);
#nullable restore
    }

    internal sealed partial class BoundConversion
    {
        /// <remarks>
        /// This method is intended for passes other than the LocalRewriter.
        /// Use MakeConversion helper method in the LocalRewriter instead,
        /// it generates a synthesized conversion in its lowered form.
        /// </remarks>
        public static BoundConversion SynthesizedNonUserDefined(SyntaxNode syntax, BoundExpression operand, Conversion conversion, TypeSymbol type, ConstantValue constantValueOpt = null)
        {
            return new BoundConversion(
                syntax,
                operand,
                conversion,
                isBaseConversion: false,
                @checked: false,
                explicitCastInCode: false,
                conversionGroupOpt: null,
                constantValueOpt: constantValueOpt,
                originalUserDefinedConversionsOpt: default,
                type: type)
            { WasCompilerGenerated = true };
        }

        /// <remarks>
        /// NOTE:    This method is intended for passes other than the LocalRewriter.
        /// NOTE:    Use MakeConversion helper method in the LocalRewriter instead,
        /// NOTE:    it generates a synthesized conversion in its lowered form.
        /// </remarks>
        public static BoundConversion Synthesized(
            SyntaxNode syntax,
            BoundExpression operand,
            Conversion conversion,
            bool @checked,
            bool explicitCastInCode,
            ConversionGroup conversionGroupOpt,
            ConstantValue constantValueOpt,
            TypeSymbol type,
            bool hasErrors = false)
        {
            return new BoundConversion(
                syntax,
                operand,
                conversion,
                @checked,
                explicitCastInCode: explicitCastInCode,
                conversionGroupOpt,
                constantValueOpt,
                type,
                hasErrors || !conversion.IsValid)
            {
                WasCompilerGenerated = true
            };
        }

        public BoundConversion(
            SyntaxNode syntax,
            BoundExpression operand,
            Conversion conversion,
            bool @checked,
            bool explicitCastInCode,
            ConversionGroup conversionGroupOpt,
            ConstantValue constantValueOpt,
            TypeSymbol type,
            bool hasErrors = false)
            : this(
                syntax,
                operand,
                conversion,
                isBaseConversion: false,
                @checked: @checked,
                explicitCastInCode: explicitCastInCode,
                constantValueOpt: constantValueOpt,
                conversionGroupOpt,
                conversion.OriginalUserDefinedConversions,
                type: type,
                hasErrors: hasErrors || !conversion.IsValid)
        { }


#nullable enable
        public BoundConversion(
            SyntaxNode syntax,
            BoundExpression operand,
            Conversion conversion,
            bool isBaseConversion,
            bool @checked,
            bool explicitCastInCode,
            ConstantValue? constantValueOpt,
            ConversionGroup? conversionGroupOpt,
            TypeSymbol type,
            bool hasErrors = false) :
            this(syntax, operand, conversion, isBaseConversion, @checked, explicitCastInCode, constantValueOpt, conversionGroupOpt, originalUserDefinedConversionsOpt: default, type, hasErrors)
        {
        }

        public BoundConversion Update(BoundExpression operand,
                                      Conversion conversion,
                                      bool isBaseConversion,
                                      bool @checked,
                                      bool explicitCastInCode,
                                      ConstantValue? constantValueOpt,
                                      ConversionGroup? conversionGroupOpt,
                                      TypeSymbol type)
            => Update(operand, conversion, isBaseConversion, @checked, explicitCastInCode, constantValueOpt, conversionGroupOpt, this.OriginalUserDefinedConversionsOpt, type);
#nullable restore
    }

    internal sealed partial class BoundBinaryOperator
    {
        public BoundBinaryOperator(
            SyntaxNode syntax,
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
                constantValueOpt,
                methodOpt,
                resultKind,
                originalUserDefinedOperatorsOpt,
                left,
                right,
                type,
                hasErrors)
        {
        }
#nullable enable
        public BoundBinaryOperator(
            SyntaxNode syntax,
            BinaryOperatorKind operatorKind,
            ConstantValue? constantValueOpt,
            MethodSymbol? methodOpt,
            LookupResultKind resultKind,
            BoundExpression left,
            BoundExpression right,
            TypeSymbol type,
            bool hasErrors = false) :
            this(syntax, operatorKind, constantValueOpt, methodOpt, resultKind, originalUserDefinedOperatorsOpt: default, left, right, type, hasErrors)
        {
        }

        public BoundBinaryOperator Update(BinaryOperatorKind operatorKind,
                                          ConstantValue? constantValueOpt,
                                          MethodSymbol? methodOpt,
                                          LookupResultKind resultKind,
                                          BoundExpression left,
                                          BoundExpression right,
                                          TypeSymbol type)
            => Update(operatorKind, constantValueOpt, methodOpt, resultKind, this.OriginalUserDefinedOperatorsOpt, left, right, type);
#nullable restore
    }

    internal sealed partial class BoundUserDefinedConditionalLogicalOperator
    {
        public BoundUserDefinedConditionalLogicalOperator(
            SyntaxNode syntax,
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
                logicalOperator,
                trueOperator,
                falseOperator,
                resultKind,
                originalUserDefinedOperatorsOpt,
                left,
                right,
                type,
                hasErrors)
        {
            Debug.Assert(operatorKind.IsUserDefined() && operatorKind.IsLogical());
        }

#nullable enable

        public BoundUserDefinedConditionalLogicalOperator Update(BinaryOperatorKind operatorKind,
                                                                 MethodSymbol logicalOperator,
                                                                 MethodSymbol trueOperator,
                                                                 MethodSymbol falseOperator,
                                                                 LookupResultKind resultKind,
                                                                 BoundExpression left,
                                                                 BoundExpression right,
                                                                 TypeSymbol type)
            => Update(operatorKind, logicalOperator, trueOperator, falseOperator, resultKind, this.OriginalUserDefinedOperatorsOpt, left, right, type);
#nullable restore
    }

    internal sealed partial class BoundParameter
    {
        public BoundParameter(SyntaxNode syntax, ParameterSymbol parameterSymbol, bool hasErrors = false)
            : this(syntax, parameterSymbol, parameterSymbol.Type, hasErrors)
        {
        }

        public BoundParameter(SyntaxNode syntax, ParameterSymbol parameterSymbol)
            : this(syntax, parameterSymbol, parameterSymbol.Type)
        {
        }
    }

    internal sealed partial class BoundTypeExpression
    {
        public BoundTypeExpression(SyntaxNode syntax, AliasSymbol aliasOpt, BoundTypeExpression boundContainingTypeOpt, ImmutableArray<BoundExpression> boundDimensionsOpt, TypeWithAnnotations typeWithAnnotations, bool hasErrors = false)
            : this(syntax, aliasOpt, boundContainingTypeOpt, boundDimensionsOpt, typeWithAnnotations, typeWithAnnotations.Type, hasErrors)
        {
            Debug.Assert((object)typeWithAnnotations.Type != null, "Field 'type' cannot be null");
        }

        public BoundTypeExpression(SyntaxNode syntax, AliasSymbol aliasOpt, BoundTypeExpression boundContainingTypeOpt, TypeWithAnnotations typeWithAnnotations, bool hasErrors = false)
            : this(syntax, aliasOpt, boundContainingTypeOpt, ImmutableArray<BoundExpression>.Empty, typeWithAnnotations, hasErrors)
        {
        }

        public BoundTypeExpression(SyntaxNode syntax, AliasSymbol aliasOpt, TypeWithAnnotations typeWithAnnotations, bool hasErrors = false)
            : this(syntax, aliasOpt, null, typeWithAnnotations, hasErrors)
        {
        }

        public BoundTypeExpression(SyntaxNode syntax, AliasSymbol aliasOpt, TypeSymbol type, bool hasErrors = false)
            : this(syntax, aliasOpt, null, TypeWithAnnotations.Create(type), hasErrors)
        {
        }

        public BoundTypeExpression(SyntaxNode syntax, AliasSymbol aliasOpt, ImmutableArray<BoundExpression> dimensionsOpt, TypeWithAnnotations typeWithAnnotations, bool hasErrors = false)
            : this(syntax, aliasOpt, null, dimensionsOpt, typeWithAnnotations, hasErrors)
        {
        }
    }

    internal sealed partial class BoundNamespaceExpression
    {
        public BoundNamespaceExpression(SyntaxNode syntax, NamespaceSymbol namespaceSymbol, bool hasErrors = false)
            : this(syntax, namespaceSymbol, null, hasErrors)
        {
        }

        public BoundNamespaceExpression(SyntaxNode syntax, NamespaceSymbol namespaceSymbol)
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
        public BoundAssignmentOperator(SyntaxNode syntax, BoundExpression left, BoundExpression right,
            TypeSymbol type, bool isRef = false, bool hasErrors = false)
            : this(syntax, left, right, isRef, type, hasErrors)
        {
        }
    }

    internal sealed partial class BoundBadExpression
    {
        public BoundBadExpression(SyntaxNode syntax, LookupResultKind resultKind, ImmutableArray<Symbol> symbols, ImmutableArray<BoundExpression> childBoundNodes, TypeSymbol type)
            : this(syntax, resultKind, symbols, childBoundNodes, type, true)
        {
            Debug.Assert((object)type != null);
        }
    }

    internal partial class BoundStatementList
    {
        public static BoundStatementList Synthesized(SyntaxNode syntax, params BoundStatement[] statements)
        {
            return Synthesized(syntax, false, statements.AsImmutableOrNull());
        }

        public static BoundStatementList Synthesized(SyntaxNode syntax, bool hasErrors, params BoundStatement[] statements)
        {
            return Synthesized(syntax, hasErrors, statements.AsImmutableOrNull());
        }

        public static BoundStatementList Synthesized(SyntaxNode syntax, ImmutableArray<BoundStatement> statements)
        {
            return Synthesized(syntax, false, statements);
        }

        public static BoundStatementList Synthesized(SyntaxNode syntax, bool hasErrors, ImmutableArray<BoundStatement> statements)
        {
            return new BoundStatementList(syntax, statements, hasErrors) { WasCompilerGenerated = true };
        }
    }

    internal sealed partial class BoundReturnStatement
    {
        public static BoundReturnStatement Synthesized(SyntaxNode syntax, RefKind refKind, BoundExpression expression, bool hasErrors = false)
        {
            return new BoundReturnStatement(syntax, refKind, expression, hasErrors) { WasCompilerGenerated = true };
        }
    }

    internal sealed partial class BoundYieldBreakStatement
    {
        public static BoundYieldBreakStatement Synthesized(SyntaxNode syntax, bool hasErrors = false)
        {
            return new BoundYieldBreakStatement(syntax, hasErrors) { WasCompilerGenerated = true };
        }
    }

    internal sealed partial class BoundGotoStatement
    {
        public BoundGotoStatement(SyntaxNode syntax, LabelSymbol label, bool hasErrors = false)
            : this(syntax, label, caseExpressionOpt: null, labelExpressionOpt: null, hasErrors: hasErrors)
        {
        }
    }

    internal partial class BoundBlock
    {
        public BoundBlock(SyntaxNode syntax, ImmutableArray<LocalSymbol> locals, ImmutableArray<BoundStatement> statements, bool hasErrors = false) : this(syntax, locals, ImmutableArray<LocalFunctionSymbol>.Empty, statements, hasErrors)
        {
        }

        public static BoundBlock SynthesizedNoLocals(SyntaxNode syntax, BoundStatement statement)
        {
            return new BoundBlock(syntax, ImmutableArray<LocalSymbol>.Empty, ImmutableArray.Create(statement))
            { WasCompilerGenerated = true };
        }

        public static BoundBlock SynthesizedNoLocals(SyntaxNode syntax, ImmutableArray<BoundStatement> statements)
        {
            return new BoundBlock(syntax, ImmutableArray<LocalSymbol>.Empty, statements) { WasCompilerGenerated = true };
        }

        public static BoundBlock SynthesizedNoLocals(SyntaxNode syntax, params BoundStatement[] statements)
        {
            return new BoundBlock(syntax, ImmutableArray<LocalSymbol>.Empty, statements.AsImmutableOrNull()) { WasCompilerGenerated = true };
        }
    }

    internal partial class BoundDefaultExpression
    {
        public BoundDefaultExpression(SyntaxNode syntax, TypeSymbol type, bool hasErrors = false)
            : this(syntax, targetType: null, type?.GetDefaultValue(), type, hasErrors)
        {
        }
    }

    internal partial class BoundTryStatement
    {
        public BoundTryStatement(SyntaxNode syntax, BoundBlock tryBlock, ImmutableArray<BoundCatchBlock> catchBlocks, BoundBlock finallyBlockOpt, LabelSymbol finallyLabelOpt = null)
            : this(syntax, tryBlock, catchBlocks, finallyBlockOpt, finallyLabelOpt, preferFaultHandler: false, hasErrors: false)
        {
        }
    }

    internal partial class BoundAddressOfOperator
    {
        public BoundAddressOfOperator(SyntaxNode syntax, BoundExpression operand, TypeSymbol type, bool hasErrors = false)
             : this(syntax, operand, isManaged: false, type, hasErrors)
        {
        }
    }

    internal partial class BoundDagTemp
    {
        public BoundDagTemp(SyntaxNode syntax, TypeSymbol type, BoundDagEvaluation source)
            : this(syntax, type, source, index: 0, hasErrors: false)
        {
        }

        public static BoundDagTemp ForOriginalInput(BoundExpression expr) => new BoundDagTemp(expr.Syntax, expr.Type, source: null);
    }

#nullable enable
    internal partial class BoundCompoundAssignmentOperator
    {
        public BoundCompoundAssignmentOperator(SyntaxNode syntax,
            BinaryOperatorSignature @operator,
            BoundExpression left,
            BoundExpression right,
            Conversion leftConversion,
            Conversion finalConversion,
            LookupResultKind resultKind,
            TypeSymbol type,
            bool hasErrors = false)
            : this(syntax, @operator, left, right, leftConversion, finalConversion, resultKind, originalUserDefinedOperatorsOpt: default, type, hasErrors)
        {
        }

        public BoundCompoundAssignmentOperator Update(BinaryOperatorSignature @operator,
                                                      BoundExpression left,
                                                      BoundExpression right,
                                                      Conversion leftConversion,
                                                      Conversion finalConversion,
                                                      LookupResultKind resultKind,
                                                      TypeSymbol type)
            => Update(@operator, left, right, leftConversion, finalConversion, resultKind, this.OriginalUserDefinedOperatorsOpt, type);
    }

    internal partial class BoundUnaryOperator
    {
        public BoundUnaryOperator(
            SyntaxNode syntax,
            UnaryOperatorKind operatorKind,
            BoundExpression operand,
            ConstantValue? constantValueOpt,
            MethodSymbol? methodOpt,
            LookupResultKind resultKind,
            TypeSymbol type,
            bool hasErrors = false) :
            this(syntax, operatorKind, operand, constantValueOpt, methodOpt, resultKind, originalUserDefinedOperatorsOpt: default, type, hasErrors)
        {
        }

        public BoundUnaryOperator Update(UnaryOperatorKind operatorKind,
                                         BoundExpression operand,
                                         ConstantValue? constantValueOpt,
                                         MethodSymbol? methodOpt,
                                         LookupResultKind resultKind,
                                         TypeSymbol type)
            => Update(operatorKind, operand, constantValueOpt, methodOpt, resultKind, this.OriginalUserDefinedOperatorsOpt, type);
    }

    internal partial class BoundIncrementOperator
    {
        public BoundIncrementOperator(
            CSharpSyntaxNode syntax,
            UnaryOperatorKind operatorKind,
            BoundExpression operand,
            MethodSymbol? methodOpt,
            Conversion operandConversion,
            Conversion resultConversion,
            LookupResultKind resultKind,
            TypeSymbol type,
            bool hasErrors = false) :
            this(syntax, operatorKind, operand, methodOpt, operandConversion, resultConversion, resultKind, originalUserDefinedOperatorsOpt: default, type, hasErrors)
        {
        }

        public BoundIncrementOperator Update(UnaryOperatorKind operatorKind, BoundExpression operand, MethodSymbol? methodOpt, Conversion operandConversion, Conversion resultConversion, LookupResultKind resultKind, TypeSymbol type)
        {
            return Update(operatorKind, operand, methodOpt, operandConversion, resultConversion, resultKind, this.OriginalUserDefinedOperatorsOpt, type);
        }
    }

    internal partial class BoundCollectionElementInitializer
    {
        public BoundCollectionElementInitializer(
            SyntaxNode syntax,
            MethodSymbol addMethod,
            ImmutableArray<BoundExpression> arguments,
            BoundExpression? implicitReceiverOpt,
            bool expanded,
            ImmutableArray<int> argsToParamsOpt,
            bool invokedAsExtensionMethod,
            LookupResultKind resultKind,
            Binder? binderOpt,
            TypeSymbol type,
            bool hasErrors = false) :
            this(syntax, addMethod, arguments, implicitReceiverOpt, expanded, argsToParamsOpt, invokedAsExtensionMethod, resultKind, binderOpt, originalMethodsOpt: default, type, hasErrors)
        { }

        public BoundCollectionElementInitializer Update(MethodSymbol addMethod,
                                                        ImmutableArray<BoundExpression> arguments,
                                                        BoundExpression? implicitReceiverOpt,
                                                        bool expanded,
                                                        ImmutableArray<int> argsToParamsOpt,
                                                        bool invokedAsExtensionMethod,
                                                        LookupResultKind resultKind,
                                                        Binder? binderOpt,
                                                        TypeSymbol type)
            => Update(addMethod, arguments, implicitReceiverOpt, expanded, argsToParamsOpt, invokedAsExtensionMethod, resultKind, binderOpt, this.OriginalMethodsOpt, type);
    }
#nullable restore
}
