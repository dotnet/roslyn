// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This pass detects and reports diagnostics that do not affect lambda convertibility.
    /// This part of the partial class focuses on features that cannot be used in expression trees.
    /// CAVEAT: Errors may be produced for ObsoleteAttribute, but such errors don't affect lambda convertibility.
    /// </summary>
    internal sealed partial class DiagnosticsPass
    {
        private readonly BindingDiagnosticBag _diagnostics;
        private readonly CSharpCompilation _compilation;
        private bool _inExpressionLambda;
        private bool _reportedUnsafe;
        private readonly MethodSymbol _containingSymbol;

        // Containing static local function, static anonymous function, or static lambda.
        private SourceMethodSymbol _staticLocalOrAnonymousFunction;

        public static void IssueDiagnostics(CSharpCompilation compilation, BoundNode node, BindingDiagnosticBag diagnostics, MethodSymbol containingSymbol)
        {
            Debug.Assert(node != null);
            Debug.Assert((object)containingSymbol != null);

            ExecutableCodeBinder.ValidateIteratorMethod(compilation, containingSymbol, diagnostics);

            try
            {
                var diagnosticPass = new DiagnosticsPass(compilation, diagnostics, containingSymbol);
                diagnosticPass.Visit(node);
            }
            catch (CancelledByStackGuardException ex)
            {
                ex.AddAnError(diagnostics);
            }
        }

        private DiagnosticsPass(CSharpCompilation compilation, BindingDiagnosticBag diagnostics, MethodSymbol containingSymbol)
        {
            Debug.Assert(diagnostics != null);
            Debug.Assert((object)containingSymbol != null);

            _compilation = compilation;
            _diagnostics = diagnostics;
            _containingSymbol = containingSymbol;
        }

        private void Error(ErrorCode code, BoundNode node, params object[] args)
        {
            _diagnostics.Add(code, node.Syntax.Location, args);
        }

        private void CheckUnsafeType(BoundExpression e)
        {
            if (e != null && (object)e.Type != null && e.Type.IsPointerOrFunctionPointer()) NoteUnsafe(e);
        }

        private void NoteUnsafe(BoundNode node)
        {
            if (_inExpressionLambda && !_reportedUnsafe)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsPointerOp, node);
                _reportedUnsafe = true;
            }
        }

        public override BoundNode VisitArrayCreation(BoundArrayCreation node)
        {
            var arrayType = (ArrayTypeSymbol)node.Type;
            if (_inExpressionLambda && node.InitializerOpt != null && !arrayType.IsSZArray)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsMultiDimensionalArrayInitializer, node);
            }

            return base.VisitArrayCreation(node);
        }

        public override BoundNode VisitArrayAccess(BoundArrayAccess node)
        {
            if (_inExpressionLambda &&
                node.Indices.Length == 1 &&
                !node.Indices[0].Type!.SpecialType.CanOptimizeBehavior())
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsPatternImplicitIndexer, node);
            }

            return base.VisitArrayAccess(node);
        }

        public override BoundNode VisitImplicitIndexerAccess(BoundImplicitIndexerAccess node)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsPatternImplicitIndexer, node);
            }

            return base.VisitImplicitIndexerAccess(node);
        }

        public override BoundNode VisitInlineArrayAccess(BoundInlineArrayAccess node)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsInlineArrayOperation, node);
            }

            return base.VisitInlineArrayAccess(node);
        }

        public override BoundNode VisitFromEndIndexExpression(BoundFromEndIndexExpression node)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsFromEndIndexExpression, node);
            }

            return base.VisitFromEndIndexExpression(node);
        }

        public override BoundNode VisitRangeExpression(BoundRangeExpression node)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsRangeExpression, node);
            }

            return base.VisitRangeExpression(node);
        }

        public override BoundNode VisitSizeOfOperator(BoundSizeOfOperator node)
        {
            if (_inExpressionLambda && node.ConstantValueOpt == null)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsPointerOp, node);
            }

            return base.VisitSizeOfOperator(node);
        }

        public override BoundNode VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
        {
            ExecutableCodeBinder.ValidateIteratorMethod(_compilation, node.Symbol, _diagnostics);

            var outerLocalFunction = _staticLocalOrAnonymousFunction;
            if (node.Symbol.IsStatic)
            {
                _staticLocalOrAnonymousFunction = (SourceMethodSymbol)node.Symbol;
            }
            var result = base.VisitLocalFunctionStatement(node);
            _staticLocalOrAnonymousFunction = outerLocalFunction;
            return result;
        }

        public override BoundNode VisitThisReference(BoundThisReference node)
        {
            CheckReferenceToThisOrBase(node);
            return base.VisitThisReference(node);
        }

        public override BoundNode VisitBaseReference(BoundBaseReference node)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsBaseAccess, node);
            }
            CheckReferenceToThisOrBase(node);
            return base.VisitBaseReference(node);
        }

        public override BoundNode VisitLocal(BoundLocal node)
        {
            CheckReferenceToVariable(node, node.LocalSymbol);
            return base.VisitLocal(node);
        }

        public override BoundNode VisitParameter(BoundParameter node)
        {
            CheckReferenceToVariable(node, node.ParameterSymbol);
            return base.VisitParameter(node);
        }

        private void CheckReferenceToThisOrBase(BoundExpression node)
        {
            if (_staticLocalOrAnonymousFunction is object)
            {
                var diagnostic = _staticLocalOrAnonymousFunction.MethodKind == MethodKind.LocalFunction
                    ? ErrorCode.ERR_StaticLocalFunctionCannotCaptureThis
                    : ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureThis;

                Error(diagnostic, node);
            }
        }

#nullable enable
        private void CheckReferenceToVariable(BoundExpression node, Symbol symbol)
        {
            Debug.Assert(symbol.Kind == SymbolKind.Local || symbol.Kind == SymbolKind.Parameter || symbol is LocalFunctionSymbol);

            if (_staticLocalOrAnonymousFunction is object && Symbol.IsCaptured(symbol, _staticLocalOrAnonymousFunction))
            {
                var diagnostic = _staticLocalOrAnonymousFunction.MethodKind == MethodKind.LocalFunction
                    ? ErrorCode.ERR_StaticLocalFunctionCannotCaptureVariable
                    : ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureVariable;

                Error(diagnostic, node, new FormattedSymbol(symbol, SymbolDisplayFormat.ShortFormat));
            }
        }
#nullable disable

        private void CheckReferenceToMethodIfLocalFunction(BoundExpression node, MethodSymbol method)
        {
            if (method?.OriginalDefinition is LocalFunctionSymbol localFunction)
            {
                CheckReferenceToVariable(node, localFunction);
            }
        }

        public override BoundNode VisitConvertedSwitchExpression(BoundConvertedSwitchExpression node)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsSwitchExpression, node);
            }

            return base.VisitConvertedSwitchExpression(node);
        }

        public override BoundNode VisitDeconstructionAssignmentOperator(BoundDeconstructionAssignmentOperator node)
        {
            if (!node.HasAnyErrors)
            {
                CheckForDeconstructionAssignmentToSelf((BoundTupleExpression)node.Left, node.Right);
            }

            return base.VisitDeconstructionAssignmentOperator(node);
        }

        public override BoundNode VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            CheckForAssignmentToSelf(node);

            if (_inExpressionLambda && node.Left.Kind != BoundKind.ObjectInitializerMember && node.Left.Kind != BoundKind.DynamicObjectInitializerMember)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsAssignment, node);
            }

            return base.VisitAssignmentOperator(node);
        }

        public override BoundNode VisitDynamicObjectInitializerMember(BoundDynamicObjectInitializerMember node)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, node);
            }

            return base.VisitDynamicObjectInitializerMember(node);
        }

        public override BoundNode VisitEventAccess(BoundEventAccess node)
        {
            // Don't bother reporting an obsolete diagnostic if the access is already wrong for other reasons
            // (specifically, we can't use it as a field here).
            if (node.IsUsableAsField)
            {
                bool hasBaseReceiver = node.ReceiverOpt != null && node.ReceiverOpt.Kind == BoundKind.BaseReference;
                Binder.ReportDiagnosticsIfObsolete(_diagnostics, node.EventSymbol.AssociatedField, node.Syntax, hasBaseReceiver, _containingSymbol, _containingSymbol.ContainingType, BinderFlags.None);
            }
            CheckReceiverIfField(node.ReceiverOpt);
            return base.VisitEventAccess(node);
        }

        public override BoundNode VisitEventAssignmentOperator(BoundEventAssignmentOperator node)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsAssignment, node);
            }

            bool hasBaseReceiver = node.ReceiverOpt != null && node.ReceiverOpt.Kind == BoundKind.BaseReference;
            Binder.ReportDiagnosticsIfObsolete(_diagnostics, node.Event, ((AssignmentExpressionSyntax)node.Syntax).Left, hasBaseReceiver, _containingSymbol, _containingSymbol.ContainingType, BinderFlags.None);
            CheckReceiverIfField(node.ReceiverOpt);
            return base.VisitEventAssignmentOperator(node);
        }

        public override BoundNode VisitCompoundAssignmentOperator(BoundCompoundAssignmentOperator node)
        {
            CheckCompoundAssignmentOperator(node);

            return base.VisitCompoundAssignmentOperator(node);
        }

        private void VisitCall(
            MethodSymbol method,
            PropertySymbol propertyAccess,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> argumentRefKindsOpt,
            ImmutableArray<string> argumentNamesOpt,
            ImmutableArray<int> argsToParamsOpt,
            BitVector defaultArguments,
            BoundNode node)
        {
            Debug.Assert((object)method != null);
            Debug.Assert(((object)propertyAccess == null) ||
                (method == propertyAccess.GetOwnOrInheritedGetMethod()) ||
                (method == propertyAccess.GetOwnOrInheritedSetMethod()) ||
                propertyAccess.MustCallMethodsDirectly);

            CheckArguments(argumentRefKindsOpt, arguments, method);

            if (_inExpressionLambda)
            {
                if (method.CallsAreOmitted(node.SyntaxTree))
                {
                    Error(ErrorCode.ERR_PartialMethodInExpressionTree, node);
                }
                else if ((object)propertyAccess != null && propertyAccess.IsIndexedProperty() && !propertyAccess.IsIndexer)
                {
                    Error(ErrorCode.ERR_ExpressionTreeContainsIndexedProperty, node);
                }
                else if (hasDefaultArgument(arguments, defaultArguments) &&
                    !_compilation.IsFeatureEnabled(MessageID.IDS_FeatureExpressionOptionalAndNamedArguments))
                {
                    Error(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, node);
                }
                else if (!argumentNamesOpt.IsDefaultOrEmpty &&
                    !_compilation.IsFeatureEnabled(MessageID.IDS_FeatureExpressionOptionalAndNamedArguments))
                {
                    Error(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, node);
                }
                else if (!argumentNamesOpt.IsDefaultOrEmpty &&
                    hasNamedArgumentOutOfOrder(argsToParamsOpt))
                {
                    Debug.Assert(_compilation.IsFeatureEnabled(MessageID.IDS_FeatureExpressionOptionalAndNamedArguments));
                    Error(ErrorCode.ERR_ExpressionTreeContainsNamedArgumentOutOfPosition, node);
                }
                else if (IsComCallWithRefOmitted(method, arguments, argumentRefKindsOpt))
                {
                    Error(ErrorCode.ERR_ComRefCallInExpressionTree, node);
                }
                else if (method.MethodKind == MethodKind.LocalFunction)
                {
                    Error(ErrorCode.ERR_ExpressionTreeContainsLocalFunction, node);
                }
                else if (method.RefKind != RefKind.None)
                {
                    Error(ErrorCode.ERR_RefReturningCallInExpressionTree, node);
                }
                else if ((method.IsAbstract || method.IsVirtual) && method.IsStatic)
                {
                    Error(ErrorCode.ERR_ExpressionTreeContainsAbstractStaticMemberAccess, node);
                }
            }

            static bool hasDefaultArgument(ImmutableArray<BoundExpression> arguments, BitVector defaultArguments)
            {
                for (int i = 0; i < arguments.Length; i++)
                {
                    if (defaultArguments[i])
                    {
                        return true;
                    }
                }

                return false;
            }

            static bool hasNamedArgumentOutOfOrder(ImmutableArray<int> argsToParamsOpt)
            {
                if (argsToParamsOpt.IsDefaultOrEmpty)
                {
                    return false;
                }
                for (int i = 0; i < argsToParamsOpt.Length; i++)
                {
                    if (argsToParamsOpt[i] != i)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public override BoundNode Visit(BoundNode node)
        {
            if (_inExpressionLambda &&
                // Ignoring BoundConversion nodes prevents redundant diagnostics
                !(node is BoundConversion) &&
                node is BoundExpression expr &&
                expr.Type is TypeSymbol type &&
                type.IsRestrictedType())
            {
                Error(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, node, type.Name);
            }
            return base.Visit(node);
        }

        public override BoundNode VisitRefTypeOperator(BoundRefTypeOperator node)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_FeatureNotValidInExpressionTree, node, "__reftype");
            }

            return base.VisitRefTypeOperator(node);
        }

        public override BoundNode VisitRefValueOperator(BoundRefValueOperator node)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_FeatureNotValidInExpressionTree, node, "__refvalue");
            }

            return base.VisitRefValueOperator(node);
        }

        public override BoundNode VisitMakeRefOperator(BoundMakeRefOperator node)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_FeatureNotValidInExpressionTree, node, "__makeref");
            }

            return base.VisitMakeRefOperator(node);
        }

        public override BoundNode VisitArgListOperator(BoundArgListOperator node)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_VarArgsInExpressionTree, node);
            }

            return base.VisitArgListOperator(node);
        }

        public override BoundNode VisitConditionalAccess(BoundConditionalAccess node)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_NullPropagatingOpInExpressionTree, node);
            }

            return base.VisitConditionalAccess(node);
        }

        public override BoundNode VisitObjectInitializerMember(BoundObjectInitializerMember node)
        {
            if (_inExpressionLambda && !node.Arguments.IsDefaultOrEmpty)
            {
                Error(ErrorCode.ERR_DictionaryInitializerInExpressionTree, node);
            }

            if (node.MemberSymbol is PropertySymbol property)
            {
                if (_inExpressionLambda && property.IsExtensionBlockMember())
                {
                    Error(ErrorCode.ERR_ExpressionTreeContainsExtensionPropertyAccess, node);
                }
                else
                {
                    CheckRefReturningPropertyAccess(node, property);
                }
            }

            return base.VisitObjectInitializerMember(node);
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            if (node.ReceiverOpt is BoundCall receiver1)
            {
                var calls = ArrayBuilder<BoundCall>.GetInstance();

                calls.Push(node);
                node = receiver1;

                while (node.ReceiverOpt is BoundCall receiver2)
                {
                    calls.Push(node);
                    node = receiver2;
                }

                CheckReceiverIfField(node.ReceiverOpt);
                this.Visit(node.ReceiverOpt);

                do
                {
                    visitCall(node);
                    CheckReferenceToMethodIfLocalFunction(node, node.Method);
                    this.VisitList(node.Arguments);
                }
                while (calls.TryPop(out node));

                calls.Free();
            }
            else
            {
                visitCall(node);
                CheckReceiverIfField(node.ReceiverOpt);
                CheckReferenceToMethodIfLocalFunction(node, node.Method);
                this.Visit(node.ReceiverOpt);
                this.VisitList(node.Arguments);
            }

            return null;

            void visitCall(BoundCall node)
            {
                VisitCall(node.Method, null, node.Arguments, node.ArgumentRefKindsOpt, node.ArgumentNamesOpt, node.ArgsToParamsOpt, node.DefaultArguments, node);
            }
        }

        /// <summary>
        /// Called when a local represents an out variable declaration. Its syntax is of type DeclarationExpressionSyntax.
        /// </summary>
        private void CheckOutDeclaration(BoundLocal local)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsOutVariable, local);
            }
        }

        private void CheckDiscard(BoundDiscardExpression argument)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsDiscard, argument);
            }
        }

        public override BoundNode VisitCollectionElementInitializer(BoundCollectionElementInitializer node)
        {
            if (_inExpressionLambda && (node.AddMethod.IsStatic || node.AddMethod.IsExtensionBlockMember()))
            {
                Error(ErrorCode.ERR_ExtensionCollectionElementInitializerInExpressionTree, node);
            }

            VisitCall(node.AddMethod, null, node.Arguments, default(ImmutableArray<RefKind>), default(ImmutableArray<string>), default(ImmutableArray<int>), node.DefaultArguments, node);
            return base.VisitCollectionElementInitializer(node);
        }

        public override BoundNode VisitObjectCreationExpression(BoundObjectCreationExpression node)
        {
            VisitCall(node.Constructor, null, node.Arguments, node.ArgumentRefKindsOpt, node.ArgumentNamesOpt, node.ArgsToParamsOpt, node.DefaultArguments, node);
            return base.VisitObjectCreationExpression(node);
        }

        public override BoundNode VisitIndexerAccess(BoundIndexerAccess node)
        {
            var indexer = node.Indexer;
            var method = indexer.GetOwnOrInheritedGetMethod() ?? indexer.GetOwnOrInheritedSetMethod();
            if ((object)method != null)
            {
                VisitCall(method, indexer, node.Arguments, node.ArgumentRefKindsOpt, node.ArgumentNamesOpt, node.ArgsToParamsOpt, node.DefaultArguments, node);
            }
            CheckReceiverIfField(node.ReceiverOpt);
            return base.VisitIndexerAccess(node);
        }

        private void CheckRefReturningPropertyAccess(BoundNode node, PropertySymbol property)
        {
            if (_inExpressionLambda && property.RefKind != RefKind.None)
            {
                Error(ErrorCode.ERR_RefReturningCallInExpressionTree, node);
            }
        }

        public override BoundNode VisitPropertyAccess(BoundPropertyAccess node)
        {
            var property = node.PropertySymbol;
            CheckRefReturningPropertyAccess(node, property);
            CheckReceiverIfField(node.ReceiverOpt);

            if (_inExpressionLambda)
            {
                if ((property.IsAbstract || property.IsVirtual) && property.IsStatic)
                {
                    Error(ErrorCode.ERR_ExpressionTreeContainsAbstractStaticMemberAccess, node);
                }
                else if (property.IsExtensionBlockMember())
                {
                    Error(ErrorCode.ERR_ExpressionTreeContainsExtensionPropertyAccess, node);
                }
            }

            return base.VisitPropertyAccess(node);
        }

        public override BoundNode VisitLambda(BoundLambda node)
        {
            if (_inExpressionLambda)
            {
                var lambda = node.Symbol;
                bool reportedAttributes = false;

                if (!lambda.GetAttributes().IsEmpty || !lambda.GetReturnTypeAttributes().IsEmpty)
                {
                    Error(ErrorCode.ERR_LambdaWithAttributesToExpressionTree, node);
                    reportedAttributes = true;
                }

                foreach (var p in lambda.Parameters)
                {
                    if (p.RefKind != RefKind.None && p.TryGetFirstLocation() is Location location)
                    {
                        _diagnostics.Add(ErrorCode.ERR_ByRefParameterInExpressionTree, location);
                    }
                    if (p.TypeWithAnnotations.IsRestrictedType())
                    {
                        _diagnostics.Add(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, p.GetFirstLocation(), p.Type.Name);
                    }

                    if (!reportedAttributes && !p.GetAttributes().IsEmpty)
                    {
                        _diagnostics.Add(ErrorCode.ERR_LambdaWithAttributesToExpressionTree, p.GetFirstLocation());
                        reportedAttributes = true;
                    }
                }

                switch (node.Syntax.Kind())
                {
                    case SyntaxKind.ParenthesizedLambdaExpression:
                        {
                            var lambdaSyntax = (ParenthesizedLambdaExpressionSyntax)node.Syntax;
                            if (lambdaSyntax.AsyncKeyword.Kind() == SyntaxKind.AsyncKeyword)
                            {
                                Error(ErrorCode.ERR_BadAsyncExpressionTree, node);
                            }
                            else if (lambdaSyntax.Body.Kind() == SyntaxKind.Block)
                            {
                                Error(ErrorCode.ERR_StatementLambdaToExpressionTree, node);
                            }
                            else if (lambdaSyntax.Body.Kind() == SyntaxKind.RefExpression)
                            {
                                Error(ErrorCode.ERR_BadRefReturnExpressionTree, node);
                            }
                        }
                        break;

                    case SyntaxKind.SimpleLambdaExpression:
                        {
                            var lambdaSyntax = (SimpleLambdaExpressionSyntax)node.Syntax;
                            if (lambdaSyntax.AsyncKeyword.Kind() == SyntaxKind.AsyncKeyword)
                            {
                                Error(ErrorCode.ERR_BadAsyncExpressionTree, node);
                            }
                            else if (lambdaSyntax.Body.Kind() == SyntaxKind.Block)
                            {
                                Error(ErrorCode.ERR_StatementLambdaToExpressionTree, node);
                            }
                            else if (lambdaSyntax.Body.Kind() == SyntaxKind.RefExpression)
                            {
                                Error(ErrorCode.ERR_BadRefReturnExpressionTree, node);
                            }
                        }
                        break;

                    case SyntaxKind.AnonymousMethodExpression:
                        Error(ErrorCode.ERR_ExpressionTreeContainsAnonymousMethod, node);
                        break;

                    default:
                        // other syntax forms arise from query expressions, and always result from implied expression-lambda-like forms
                        break;
                }
            }

            var outerLocalFunction = _staticLocalOrAnonymousFunction;
            if (node.Symbol.IsStatic)
            {
                _staticLocalOrAnonymousFunction = (SourceMethodSymbol)node.Symbol;
            }
            var result = base.VisitLambda(node);
            _staticLocalOrAnonymousFunction = outerLocalFunction;
            return result;
        }

        public override BoundNode VisitBinaryOperator(BoundBinaryOperator node)
        {
            // It is very common for bound trees to be left-heavy binary operators, eg,
            // a + b + c + d + ...
            // To avoid blowing the stack, do not recurse down the left hand side.

            // In order to avoid blowing the stack, we end up visiting right children
            // before left children; this should not be a problem in the diagnostics 
            // pass.

            BoundBinaryOperator current = node;
            while (true)
            {
                CheckBinaryOperator(current);

                Visit(current.Right);
                if (current.Left.Kind == BoundKind.BinaryOperator)
                {
                    current = (BoundBinaryOperator)current.Left;
                }
                else
                {
                    Visit(current.Left);
                    break;
                }
            }

            return null;
        }

        public override BoundNode VisitBinaryPattern(BoundBinaryPattern node)
        {
            // Do not use left recursion because we can have many nested binary patterns.

            BoundBinaryPattern current = node;
            while (true)
            {
                Visit(current.Right);
                if (current.Left is BoundBinaryPattern left)
                {
                    current = left;
                }
                else
                {
                    Visit(current.Left);
                    break;
                }
            }

            return null;
        }

        public override BoundNode VisitUserDefinedConditionalLogicalOperator(BoundUserDefinedConditionalLogicalOperator node)
        {
            CheckLiftedUserDefinedConditionalLogicalOperator(node);

            if (_inExpressionLambda)
            {
                var binary = node.LogicalOperator;
                var unary = node.OperatorKind.Operator() == BinaryOperatorKind.And ? node.FalseOperator : node.TrueOperator;

                if (((binary.IsAbstract || binary.IsVirtual) && binary.IsStatic) || ((unary.IsAbstract || unary.IsVirtual) && unary.IsStatic))
                {
                    Error(ErrorCode.ERR_ExpressionTreeContainsAbstractStaticMemberAccess, node);
                }

                if (binary.IsExtensionBlockMember())
                {
                    // An expression tree factory isn't happy in this case. It throws
                    //            System.ArgumentException : The user-defined operator method 'op_BitwiseOr' for operator 'OrElse' must have associated boolean True and False operators.
                    // or
                    //            System.ArgumentException : The user-defined operator method 'op_BitwiseAnd' for operator 'AndAlso' must have associated boolean True and False operators.
                    //
                    // from Expression.ValidateUserDefinedConditionalLogicOperator(ExpressionType nodeType, Type left, Type right, MethodInfo method)
                    Error(ErrorCode.ERR_ExpressionTreeContainsExtensionBasedConditionalLogicalOperator, node);
                }
                else
                {
                    Debug.Assert(!node.TrueOperator.IsExtensionBlockMember());
                    Debug.Assert(!node.FalseOperator.IsExtensionBlockMember());
                }
            }

            return base.VisitUserDefinedConditionalLogicalOperator(node);
        }

        private void CheckDynamic(BoundUnaryOperator node)
        {
            if (_inExpressionLambda && node.OperatorKind.IsDynamic())
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, node);
            }
        }

        private void CheckDynamic(BoundBinaryOperator node)
        {
            if (_inExpressionLambda && node.OperatorKind.IsDynamic())
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, node);
            }
        }

        public override BoundNode VisitUnaryOperator(BoundUnaryOperator node)
        {
            CheckUnsafeType(node);
            CheckLiftedUnaryOp(node);
            CheckDynamic(node);

            if (_inExpressionLambda && node.MethodOpt is MethodSymbol method && (method.IsAbstract || method.IsVirtual) && method.IsStatic)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsAbstractStaticMemberAccess, node);
            }

            return base.VisitUnaryOperator(node);
        }

        public override BoundNode VisitAddressOfOperator(BoundAddressOfOperator node)
        {
            CheckUnsafeType(node);
            BoundExpression operand = node.Operand;
            if (operand.Kind == BoundKind.FieldAccess)
            {
                CheckFieldAddress((BoundFieldAccess)operand, consumerOpt: null);
            }
            return base.VisitAddressOfOperator(node);
        }

        public override BoundNode VisitIncrementOperator(BoundIncrementOperator node)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsAssignment, node);
            }

            return base.VisitIncrementOperator(node);
        }

        public override BoundNode VisitPointerElementAccess(BoundPointerElementAccess node)
        {
            NoteUnsafe(node);
            return base.VisitPointerElementAccess(node);
        }

        public override BoundNode VisitPointerIndirectionOperator(BoundPointerIndirectionOperator node)
        {
            NoteUnsafe(node);
            return base.VisitPointerIndirectionOperator(node);
        }

        public override BoundNode VisitConversion(BoundConversion node)
        {
            CheckUnsafeType(node.Operand);
            CheckUnsafeType(node);
            bool wasInExpressionLambda = _inExpressionLambda;
            bool oldReportedUnsafe = _reportedUnsafe;
            switch (node.ConversionKind)
            {
                case ConversionKind.MethodGroup:
                    CheckMethodGroup((BoundMethodGroup)node.Operand, node.Conversion.Method, node.IsExtensionMethod, parentIsConversion: true, node.Type);

                    return node;

                case ConversionKind.AnonymousFunction:
                    if (!wasInExpressionLambda && node.Type.IsExpressionTree())
                    {
                        _inExpressionLambda = true;
                        // we report "unsafe in expression tree" at most once for each expression tree
                        _reportedUnsafe = false;
                    }
                    break;

                case ConversionKind.ImplicitDynamic:
                case ConversionKind.ExplicitDynamic:
                    if (_inExpressionLambda)
                    {
                        Error(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, node);
                    }
                    break;

                case ConversionKind.ExplicitTuple:
                case ConversionKind.ExplicitTupleLiteral:
                case ConversionKind.ImplicitTuple:
                case ConversionKind.ImplicitTupleLiteral:
                    if (_inExpressionLambda)
                    {
                        Error(ErrorCode.ERR_ExpressionTreeContainsTupleConversion, node);
                    }
                    break;

                case ConversionKind.InlineArray:
                    if (_inExpressionLambda)
                    {
                        Error(ErrorCode.ERR_ExpressionTreeContainsInlineArrayOperation, node);
                    }
                    break;

                case ConversionKind.InterpolatedStringHandler:
                    if (_inExpressionLambda)
                    {
                        Error(ErrorCode.ERR_ExpressionTreeContainsInterpolatedStringHandlerConversion, node);
                    }
                    break;

                default:

                    if (_inExpressionLambda && node.Conversion.Method is MethodSymbol method && (method.IsAbstract || method.IsVirtual) && method.IsStatic)
                    {
                        Error(ErrorCode.ERR_ExpressionTreeContainsAbstractStaticMemberAccess, node);
                    }
                    break;
            }

            var result = base.VisitConversion(node);
            _inExpressionLambda = wasInExpressionLambda;
            _reportedUnsafe = oldReportedUnsafe;
            return result;
        }

        public override BoundNode VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
        {
            if (node.Argument.Kind != BoundKind.MethodGroup)
            {
                this.Visit(node.Argument);
            }
            else
            {
                CheckMethodGroup((BoundMethodGroup)node.Argument, node.MethodOpt, node.IsExtensionMethod, parentIsConversion: true, convertedToType: node.Type);
            }

            return null;
        }

        public override BoundNode VisitMethodGroup(BoundMethodGroup node)
        {
            CheckMethodGroup(node, method: null, isExtensionMethod: false, parentIsConversion: false, convertedToType: null);
            return null;
        }

        private void CheckMethodGroup(BoundMethodGroup node, MethodSymbol method, bool isExtensionMethod, bool parentIsConversion, TypeSymbol convertedToType)
        {
            // Formerly reported ERR_MemGroupInExpressionTree when this occurred, but the expanded 
            // ERR_LambdaInIsAs makes this impossible (since the node will always be wrapped in
            // a failed conversion).
            Debug.Assert(!(!parentIsConversion && _inExpressionLambda));

            if (_inExpressionLambda)
            {
                if ((node.LookupSymbolOpt as MethodSymbol)?.MethodKind == MethodKind.LocalFunction)
                {
                    Error(ErrorCode.ERR_ExpressionTreeContainsLocalFunction, node);
                }
                else if (parentIsConversion && convertedToType.IsFunctionPointer())
                {
                    Error(ErrorCode.ERR_AddressOfMethodGroupInExpressionTree, node);
                }
                else if (method is not null && (method.IsAbstract || method.IsVirtual) && method.IsStatic)
                {
                    Error(ErrorCode.ERR_ExpressionTreeContainsAbstractStaticMemberAccess, node);
                }
            }

            CheckReceiverIfField(node.ReceiverOpt);
            CheckReferenceToMethodIfLocalFunction(node, method);

            if (method is null || method.RequiresInstanceReceiver || isExtensionMethod)
            {
                Visit(node.ReceiverOpt);
            }
        }

        public override BoundNode VisitNameOfOperator(BoundNameOfOperator node)
        {
            // The nameof(...) operator collapses to a constant in an expression tree,
            // so it does not matter what is recursively within it.
            return node;
        }

        public override BoundNode VisitNullCoalescingOperator(BoundNullCoalescingOperator node)
        {
            if (_inExpressionLambda && (node.LeftOperand.IsLiteralNull() || node.LeftOperand.IsLiteralDefault()))
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsBadCoalesce, node.LeftOperand);
            }

            return base.VisitNullCoalescingOperator(node);
        }

        public override BoundNode VisitNullCoalescingAssignmentOperator(BoundNullCoalescingAssignmentOperator node)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_ExpressionTreeCantContainNullCoalescingAssignment, node);
            }

            return base.VisitNullCoalescingAssignmentOperator(node);
        }

        public override BoundNode VisitDynamicInvocation(BoundDynamicInvocation node)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, node);

                // avoid reporting errors for the method group:
                if (node.Expression.Kind == BoundKind.MethodGroup)
                {
                    return base.VisitMethodGroup((BoundMethodGroup)node.Expression);
                }
            }

            return base.VisitDynamicInvocation(node);
        }

        public override BoundNode VisitDynamicIndexerAccess(BoundDynamicIndexerAccess node)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, node);
            }

            CheckReceiverIfField(node.Receiver);
            return base.VisitDynamicIndexerAccess(node);
        }

        public override BoundNode VisitDynamicMemberAccess(BoundDynamicMemberAccess node)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, node);
            }

            return base.VisitDynamicMemberAccess(node);
        }

        public override BoundNode VisitDynamicCollectionElementInitializer(BoundDynamicCollectionElementInitializer node)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, node);
            }

            return base.VisitDynamicCollectionElementInitializer(node);
        }

        public override BoundNode VisitDynamicObjectCreationExpression(BoundDynamicObjectCreationExpression node)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, node);
            }

            return base.VisitDynamicObjectCreationExpression(node);
        }

        public override BoundNode VisitIsPatternExpression(BoundIsPatternExpression node)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsIsMatch, node);
            }

            return base.VisitIsPatternExpression(node);
        }

        public override BoundNode VisitConvertedTupleLiteral(BoundConvertedTupleLiteral node)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsTupleLiteral, node);
            }

            return base.VisitConvertedTupleLiteral(node);
        }

        public override BoundNode VisitTupleLiteral(BoundTupleLiteral node)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsTupleLiteral, node);
            }

            return base.VisitTupleLiteral(node);
        }

        public override BoundNode VisitTupleBinaryOperator(BoundTupleBinaryOperator node)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsTupleBinOp, node);
            }

            return base.VisitTupleBinaryOperator(node);
        }

        public override BoundNode VisitThrowExpression(BoundThrowExpression node)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsThrowExpression, node);
            }

            return base.VisitThrowExpression(node);
        }

        public override BoundNode VisitWithExpression(BoundWithExpression node)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsWithExpression, node);
            }

            return base.VisitWithExpression(node);
        }

        public override BoundNode VisitFunctionPointerInvocation(BoundFunctionPointerInvocation node)
        {
            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsPointerOp, node);
            }

            return base.VisitFunctionPointerInvocation(node);
        }

        public override BoundNode VisitCollectionExpression(BoundCollectionExpression node)
        {
            if (_inExpressionLambda)
            {
                Error(
                    node.IsParamsArrayOrCollection ?
                        ErrorCode.ERR_ParamsCollectionExpressionTree :
                        ErrorCode.ERR_ExpressionTreeContainsCollectionExpression,
                    node);
            }

            return base.VisitCollectionExpression(node);
        }

        public override BoundNode VisitIfStatement(BoundIfStatement node)
        {
            while (true)
            {
                this.Visit(node.Condition);
                this.Visit(node.Consequence);

                var alternative = node.AlternativeOpt;
                if (alternative is null)
                {
                    break;
                }

                if (alternative is BoundIfStatement elseIfStatement)
                {
                    node = elseIfStatement;
                }
                else
                {
                    this.Visit(alternative);
                    break;
                }
            }

            return null;
        }

        public override BoundNode VisitInterpolatedString(BoundInterpolatedString node)
        {
            Visit(node.InterpolationData?.Construction);
            return base.VisitInterpolatedString(node);
        }
    }
}
