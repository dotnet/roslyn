// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class BoundFieldAccess
    {
        public BoundFieldAccess(
            SyntaxNode syntax,
            BoundExpression? receiver,
            FieldSymbol fieldSymbol,
            ConstantValue? constantValueOpt,
            bool hasErrors = false)
            : this(syntax, receiver, fieldSymbol, constantValueOpt, LookupResultKind.Viable, fieldSymbol.Type, hasErrors)
        {
        }

        public BoundFieldAccess(
            SyntaxNode syntax,
            BoundExpression? receiver,
            FieldSymbol fieldSymbol,
            ConstantValue? constantValueOpt,
            LookupResultKind resultKind,
            TypeSymbol type,
            bool hasErrors = false)
            : this(syntax, receiver, fieldSymbol, constantValueOpt, resultKind, NeedsByValueFieldAccess(receiver, fieldSymbol), isDeclaration: false, type: type, hasErrors: hasErrors)
        {
        }

        public BoundFieldAccess(
            SyntaxNode syntax,
            BoundExpression? receiver,
            FieldSymbol fieldSymbol,
            ConstantValue? constantValueOpt,
            LookupResultKind resultKind,
            bool isDeclaration,
            TypeSymbol type,
            bool hasErrors = false)
            : this(syntax, receiver, fieldSymbol, constantValueOpt, resultKind, NeedsByValueFieldAccess(receiver, fieldSymbol), isDeclaration: isDeclaration, type: type, hasErrors: hasErrors)
        {
        }

        public BoundFieldAccess Update(
            BoundExpression? receiver,
            FieldSymbol fieldSymbol,
            ConstantValue? constantValueOpt,
            LookupResultKind resultKind,
            TypeSymbol typeSymbol)
        {
            return this.Update(receiver, fieldSymbol, constantValueOpt, resultKind, this.IsByValue, this.IsDeclaration, typeSymbol);
        }

        private static bool NeedsByValueFieldAccess(BoundExpression? receiver, FieldSymbol fieldSymbol)
        {
            if (fieldSymbol.IsStatic ||
                !fieldSymbol.ContainingType.IsValueType ||
                receiver == null) // receiver may be null in error cases
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
            BitVector defaultArguments,
            LookupResultKind resultKind,
            TypeSymbol type,
            bool hasErrors = false) :
            this(syntax, receiverOpt, method, arguments, argumentNamesOpt, argumentRefKindsOpt, isDelegateCall, expanded, invokedAsExtensionMethod, argsToParamsOpt, defaultArguments, resultKind, originalMethodsOpt: default, type, hasErrors)
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
                                BitVector defaultArguments,
                                LookupResultKind resultKind,
                                TypeSymbol type)
            => Update(receiverOpt, method, arguments, argumentNamesOpt, argumentRefKindsOpt, isDelegateCall, expanded, invokedAsExtensionMethod, argsToParamsOpt, defaultArguments, resultKind, this.OriginalMethodsOpt, type);

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

            return new BoundCall(
                syntax: node,
                receiverOpt: binder.BindToTypeForErrorRecovery(receiverOpt),
                method: method,
                arguments: arguments.SelectAsArray((e, binder) => binder.BindToTypeForErrorRecovery(e), binder),
                argumentNamesOpt: namedArguments,
                argumentRefKindsOpt: refKinds,
                isDelegateCall: isDelegateCall,
                expanded: false,
                invokedAsExtensionMethod: invokedAsExtensionMethod,
                argsToParamsOpt: default(ImmutableArray<int>),
                defaultArguments: default(BitVector),
                resultKind: resultKind,
                originalMethodsOpt: originalMethods,
                type: method.ReturnType,
                hasErrors: true);
        }

        public BoundCall Update(ImmutableArray<BoundExpression> arguments)
        {
            return this.Update(ReceiverOpt, Method, arguments, ArgumentNamesOpt, ArgumentRefKindsOpt, IsDelegateCall, Expanded, InvokedAsExtensionMethod, ArgsToParamsOpt, DefaultArguments, ResultKind, OriginalMethodsOpt, Type);
        }

        public BoundCall Update(BoundExpression? receiverOpt, MethodSymbol method, ImmutableArray<BoundExpression> arguments)
        {
            return this.Update(receiverOpt, method, arguments, ArgumentNamesOpt, ArgumentRefKindsOpt, IsDelegateCall, Expanded, InvokedAsExtensionMethod, ArgsToParamsOpt, DefaultArguments, ResultKind, OriginalMethodsOpt, Type);
        }

        public static BoundCall Synthesized(SyntaxNode syntax, BoundExpression? receiverOpt, MethodSymbol method)
        {
            return Synthesized(syntax, receiverOpt, method, ImmutableArray<BoundExpression>.Empty);
        }

        public static BoundCall Synthesized(SyntaxNode syntax, BoundExpression? receiverOpt, MethodSymbol method, BoundExpression arg0)
        {
            return Synthesized(syntax, receiverOpt, method, ImmutableArray.Create(arg0));
        }

        public static BoundCall Synthesized(SyntaxNode syntax, BoundExpression? receiverOpt, MethodSymbol method, BoundExpression arg0, BoundExpression arg1)
        {
            return Synthesized(syntax, receiverOpt, method, ImmutableArray.Create(arg0, arg1));
        }

        public static BoundCall Synthesized(SyntaxNode syntax, BoundExpression? receiverOpt, MethodSymbol method, ImmutableArray<BoundExpression> arguments, ImmutableArray<RefKind> argumentRefKindsOpt = default)
        {
            argumentRefKindsOpt = argumentRefKindsOpt.IsDefault ? method.ParameterRefKinds : argumentRefKindsOpt;

#if DEBUG
            for (int i = 0; i < arguments.Length; i++)
            {
                if (i < method.ParameterCount)
                {
                    var parameterRefKind = method.ParameterRefKinds.IsDefault ? RefKind.None : method.ParameterRefKinds[i];
                    var argumentRefKind = argumentRefKindsOpt.IsDefault ? RefKind.None : argumentRefKindsOpt[i];
                    Debug.Assert(argumentRefKind is RefKind.None or RefKind.Ref or RefKind.In or RefKind.Out or RefKindExtensions.StrictIn &&
                        (argumentRefKind == parameterRefKind ||
                        parameterRefKind switch
                        {
                            RefKind.In => argumentRefKind == RefKindExtensions.StrictIn,
                            RefKind.RefReadOnlyParameter => argumentRefKind is RefKind.In or RefKindExtensions.StrictIn,
                            _ => false,
                        }),
                        $"argument ref kind {argumentRefKind} should be compatible with the corresponding parameter ref kind {parameterRefKind}");
                }
                else
                {
                    Debug.Assert(arguments[i].Kind == BoundKind.ArgListOperator);
                }
            }
#endif

            return new BoundCall(syntax,
                    receiverOpt,
                    method,
                    arguments,
                    argumentNamesOpt: default(ImmutableArray<string>),
                    argumentRefKindsOpt: argumentRefKindsOpt,
                    isDelegateCall: false,
                    expanded: false,
                    invokedAsExtensionMethod: false,
                    argsToParamsOpt: default(ImmutableArray<int>),
                    defaultArguments: default(BitVector),
                    resultKind: LookupResultKind.Viable,
                    originalMethodsOpt: default,
                    type: method.ReturnType,
                    hasErrors: method.OriginalDefinition is ErrorMethodSymbol
                )
            { WasCompilerGenerated = true };
        }
    }

    internal sealed partial class BoundObjectCreationExpression
    {
        public BoundObjectCreationExpression(SyntaxNode syntax, MethodSymbol constructor, params BoundExpression[] arguments)
            : this(syntax, constructor, ImmutableArray.Create<BoundExpression>(arguments), default(ImmutableArray<string>), default(ImmutableArray<RefKind>), false, default(ImmutableArray<int>), default(BitVector), null, null, constructor.ContainingType)
        {
        }
        public BoundObjectCreationExpression(SyntaxNode syntax, MethodSymbol constructor, ImmutableArray<BoundExpression> arguments)
            : this(syntax, constructor, arguments, default(ImmutableArray<string>), default(ImmutableArray<RefKind>), false, default(ImmutableArray<int>), default(BitVector), null, null, constructor.ContainingType)
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
                defaultArguments: default(BitVector),
                originalIndexers,
                type: indexer.Type,
                hasErrors: true);
        }
        public BoundIndexerAccess(
            SyntaxNode syntax,
            BoundExpression? receiverOpt,
            PropertySymbol indexer,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<string> argumentNamesOpt,
            ImmutableArray<RefKind> argumentRefKindsOpt,
            bool expanded,
            ImmutableArray<int> argsToParamsOpt,
            BitVector defaultArguments,
            TypeSymbol type,
            bool hasErrors = false) :
            this(syntax, receiverOpt, indexer, arguments, argumentNamesOpt, argumentRefKindsOpt, expanded, argsToParamsOpt, defaultArguments, originalIndexersOpt: default, type, hasErrors)
        { }

        public BoundIndexerAccess Update(BoundExpression? receiverOpt,
                                         PropertySymbol indexer,
                                         ImmutableArray<BoundExpression> arguments,
                                         ImmutableArray<string> argumentNamesOpt,
                                         ImmutableArray<RefKind> argumentRefKindsOpt,
                                         bool expanded,
                                         ImmutableArray<int> argsToParamsOpt,
                                         BitVector defaultArguments,
                                         TypeSymbol type)
            => Update(receiverOpt, indexer, arguments, argumentNamesOpt, argumentRefKindsOpt, expanded, argsToParamsOpt, defaultArguments, this.OriginalIndexersOpt, type);
    }

    internal sealed partial class BoundConversion
    {
        /// <remarks>
        /// This method is intended for passes other than the LocalRewriter.
        /// Use MakeConversion helper method in the LocalRewriter instead,
        /// it generates a synthesized conversion in its lowered form.
        /// </remarks>
        public static BoundConversion SynthesizedNonUserDefined(SyntaxNode syntax, BoundExpression operand, Conversion conversion, TypeSymbol type, ConstantValue? constantValueOpt = null)
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
            ConversionGroup? conversionGroupOpt,
            ConstantValue? constantValueOpt,
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
            ConversionGroup? conversionGroupOpt,
            ConstantValue? constantValueOpt,
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
    }

    internal sealed partial class BoundBinaryOperator
    {
        public BoundBinaryOperator(
            SyntaxNode syntax,
            BinaryOperatorKind operatorKind,
            BoundExpression left,
            BoundExpression right,
            ConstantValue? constantValueOpt,
            MethodSymbol? methodOpt,
            TypeSymbol? constrainedToTypeOpt,
            LookupResultKind resultKind,
            ImmutableArray<MethodSymbol> originalUserDefinedOperatorsOpt,
            TypeSymbol type,
            bool hasErrors = false)
            : this(
                syntax,
                operatorKind,
                UncommonData.CreateIfNeeded(constantValueOpt, methodOpt, constrainedToTypeOpt, originalUserDefinedOperatorsOpt),
                resultKind,
                left,
                right,
                type,
                hasErrors)
        {
        }
        public BoundBinaryOperator(
            SyntaxNode syntax,
            BinaryOperatorKind operatorKind,
            ConstantValue? constantValueOpt,
            MethodSymbol? methodOpt,
            TypeSymbol? constrainedToTypeOpt,
            LookupResultKind resultKind,
            BoundExpression left,
            BoundExpression right,
            TypeSymbol type,
            bool hasErrors = false) :
            this(syntax, operatorKind, UncommonData.CreateIfNeeded(constantValueOpt, methodOpt, constrainedToTypeOpt, originalUserDefinedOperatorsOpt: default), resultKind, left, right, type, hasErrors)
        {
        }

        public BoundBinaryOperator Update(BinaryOperatorKind operatorKind,
                                          ConstantValue? constantValueOpt,
                                          MethodSymbol? methodOpt,
                                          TypeSymbol? constrainedToTypeOpt,
                                          LookupResultKind resultKind,
                                          BoundExpression left,
                                          BoundExpression right,
                                          TypeSymbol type)
        {
            var uncommonData = UncommonData.CreateIfNeeded(constantValueOpt, methodOpt, constrainedToTypeOpt, OriginalUserDefinedOperatorsOpt);
            return Update(operatorKind, uncommonData, resultKind, left, right, type);
        }

        public BoundBinaryOperator Update(UncommonData uncommonData)
        {
            return Update(OperatorKind, uncommonData, ResultKind, Left, Right, Type);
        }
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
            TypeSymbol? constrainedToTypeOpt,
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
                constrainedToTypeOpt,
                resultKind,
                originalUserDefinedOperatorsOpt,
                left,
                right,
                type,
                hasErrors)
        {
            Debug.Assert(operatorKind.IsUserDefined() && operatorKind.IsLogical());
        }

        public BoundUserDefinedConditionalLogicalOperator Update(BinaryOperatorKind operatorKind,
                                                                 MethodSymbol logicalOperator,
                                                                 MethodSymbol trueOperator,
                                                                 MethodSymbol falseOperator,
                                                                 TypeSymbol? constrainedToTypeOpt,
                                                                 LookupResultKind resultKind,
                                                                 BoundExpression left,
                                                                 BoundExpression right,
                                                                 TypeSymbol type)
            => Update(operatorKind, logicalOperator, trueOperator, falseOperator, constrainedToTypeOpt, resultKind, this.OriginalUserDefinedOperatorsOpt, left, right, type);
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
        public BoundTypeExpression(SyntaxNode syntax, AliasSymbol? aliasOpt, BoundTypeExpression? boundContainingTypeOpt, ImmutableArray<BoundExpression> boundDimensionsOpt, TypeWithAnnotations typeWithAnnotations, bool hasErrors = false)
            : this(syntax, aliasOpt, boundContainingTypeOpt, boundDimensionsOpt, typeWithAnnotations, typeWithAnnotations.Type, hasErrors)
        {
            Debug.Assert((object)typeWithAnnotations.Type != null, "Field 'type' cannot be null");
        }

        public BoundTypeExpression(SyntaxNode syntax, AliasSymbol? aliasOpt, BoundTypeExpression? boundContainingTypeOpt, TypeWithAnnotations typeWithAnnotations, bool hasErrors = false)
            : this(syntax, aliasOpt, boundContainingTypeOpt, ImmutableArray<BoundExpression>.Empty, typeWithAnnotations, hasErrors)
        {
        }

        public BoundTypeExpression(SyntaxNode syntax, AliasSymbol? aliasOpt, TypeWithAnnotations typeWithAnnotations, bool hasErrors = false)
            : this(syntax, aliasOpt, null, typeWithAnnotations, hasErrors)
        {
        }

        public BoundTypeExpression(SyntaxNode syntax, AliasSymbol? aliasOpt, TypeSymbol type, bool hasErrors = false)
            : this(syntax, aliasOpt, null, TypeWithAnnotations.Create(type), hasErrors)
        {
        }

        public BoundTypeExpression(SyntaxNode syntax, AliasSymbol? aliasOpt, ImmutableArray<BoundExpression> dimensionsOpt, TypeWithAnnotations typeWithAnnotations, bool hasErrors = false)
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
        public BoundBadExpression(SyntaxNode syntax, LookupResultKind resultKind, ImmutableArray<Symbol?> symbols, ImmutableArray<BoundExpression> childBoundNodes, TypeSymbol type)
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
        public BoundBlock(SyntaxNode syntax, ImmutableArray<LocalSymbol> locals, ImmutableArray<BoundStatement> statements, bool hasErrors = false)
            : this(syntax, locals, ImmutableArray<LocalFunctionSymbol>.Empty, hasUnsafeModifier: false, instrumentation: null, statements, hasErrors)
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

    internal sealed partial class BoundDefaultExpression
    {
        public BoundDefaultExpression(SyntaxNode syntax, TypeSymbol type, bool hasErrors = false)
            : this(syntax, targetType: null, type.GetDefaultValue(), type, hasErrors)
        {
        }
    }

    internal partial class BoundTryStatement
    {
        public BoundTryStatement(SyntaxNode syntax, BoundBlock tryBlock, ImmutableArray<BoundCatchBlock> catchBlocks, BoundBlock? finallyBlockOpt, LabelSymbol? finallyLabelOpt = null)
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
        public BoundDagTemp(SyntaxNode syntax, TypeSymbol type, BoundDagEvaluation? source)
            : this(syntax, type, source, index: 0, hasErrors: false)
        {
        }

        public static BoundDagTemp ForOriginalInput(BoundExpression expr) => new BoundDagTemp(expr.Syntax, expr.Type!, source: null);
    }

    internal partial class BoundCompoundAssignmentOperator
    {
        public BoundCompoundAssignmentOperator(SyntaxNode syntax,
            BinaryOperatorSignature @operator,
            BoundExpression left,
            BoundExpression right,
            BoundValuePlaceholder? leftPlaceholder,
            BoundExpression? leftConversion,
            BoundValuePlaceholder? finalPlaceholder,
            BoundExpression? finalConversion,
            LookupResultKind resultKind,
            TypeSymbol type,
            bool hasErrors = false)
            : this(syntax, @operator, left, right, leftPlaceholder, leftConversion, finalPlaceholder, finalConversion, resultKind, originalUserDefinedOperatorsOpt: default, type, hasErrors)
        {
        }

        public BoundCompoundAssignmentOperator Update(BinaryOperatorSignature @operator,
                                                      BoundExpression left,
                                                      BoundExpression right,
                                                      BoundValuePlaceholder? leftPlaceholder,
                                                      BoundExpression? leftConversion,
                                                      BoundValuePlaceholder? finalPlaceholder,
                                                      BoundExpression? finalConversion,
                                                      LookupResultKind resultKind,
                                                      TypeSymbol type)
            => Update(@operator, left, right, leftPlaceholder, leftConversion, finalPlaceholder, finalConversion, resultKind, this.OriginalUserDefinedOperatorsOpt, type);
    }

    internal partial class BoundUnaryOperator
    {
        public BoundUnaryOperator(
            SyntaxNode syntax,
            UnaryOperatorKind operatorKind,
            BoundExpression operand,
            ConstantValue? constantValueOpt,
            MethodSymbol? methodOpt,
            TypeSymbol? constrainedToTypeOpt,
            LookupResultKind resultKind,
            TypeSymbol type,
            bool hasErrors = false) :
            this(syntax, operatorKind, operand, constantValueOpt, methodOpt, constrainedToTypeOpt, resultKind, originalUserDefinedOperatorsOpt: default, type, hasErrors)
        {
        }

        public BoundUnaryOperator Update(UnaryOperatorKind operatorKind,
                                         BoundExpression operand,
                                         ConstantValue? constantValueOpt,
                                         MethodSymbol? methodOpt,
                                         TypeSymbol? constrainedToTypeOpt,
                                         LookupResultKind resultKind,
                                         TypeSymbol type)
            => Update(operatorKind, operand, constantValueOpt, methodOpt, constrainedToTypeOpt, resultKind, this.OriginalUserDefinedOperatorsOpt, type);
    }

    internal partial class BoundIncrementOperator
    {
        public BoundIncrementOperator(
            CSharpSyntaxNode syntax,
            UnaryOperatorKind operatorKind,
            BoundExpression operand,
            MethodSymbol? methodOpt,
            TypeSymbol? constrainedToTypeOpt,
            BoundValuePlaceholder? operandPlaceholder,
            BoundExpression? operandConversion,
            BoundValuePlaceholder? resultPlaceholder,
            BoundExpression? resultConversion,
            LookupResultKind resultKind,
            TypeSymbol type,
            bool hasErrors = false) :
            this(syntax, operatorKind, operand, methodOpt, constrainedToTypeOpt, operandPlaceholder, operandConversion, resultPlaceholder, resultConversion, resultKind, originalUserDefinedOperatorsOpt: default, type, hasErrors)
        {
        }

        public BoundIncrementOperator Update(UnaryOperatorKind operatorKind, BoundExpression operand, MethodSymbol? methodOpt, TypeSymbol? constrainedToTypeOpt, BoundValuePlaceholder? operandPlaceholder, BoundExpression? operandConversion, BoundValuePlaceholder? resultPlaceholder, BoundExpression? resultConversion, LookupResultKind resultKind, TypeSymbol type)
        {
            return Update(operatorKind, operand, methodOpt, constrainedToTypeOpt, operandPlaceholder, operandConversion, resultPlaceholder, resultConversion, resultKind, this.OriginalUserDefinedOperatorsOpt, type);
        }
    }
}
