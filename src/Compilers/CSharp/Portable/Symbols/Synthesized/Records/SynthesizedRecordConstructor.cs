// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedPrimaryConstructor : SourceConstructorSymbolBase
    {
        private IReadOnlyDictionary<ParameterSymbol, FieldSymbol>? _capturedParameters = null;

        // PROTOTYPE(PrimaryConstructors): rename file
        public SynthesizedPrimaryConstructor(
             SourceMemberContainerTypeSymbol containingType,
             TypeDeclarationSyntax syntax) :
             base(containingType, syntax.Identifier.GetLocation(), syntax, isIterator: false)
        {
            Debug.Assert(syntax.Kind() is SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration or SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration);

            this.MakeFlags(
                MethodKind.Constructor,
                containingType.IsAbstract ? DeclarationModifiers.Protected : DeclarationModifiers.Public,
                returnsVoid: true,
                isExtensionMethod: false,
                isNullableAnalysisEnabled: false); // IsNullableAnalysisEnabled uses containing type instead.
        }

        internal TypeDeclarationSyntax GetSyntax()
        {
            Debug.Assert(syntaxReferenceOpt != null);
            return (TypeDeclarationSyntax)syntaxReferenceOpt.GetSyntax();
        }

        protected override ParameterListSyntax GetParameterList()
        {
            return GetSyntax().ParameterList!;
        }

        protected override CSharpSyntaxNode? GetInitializer()
        {
            return GetSyntax().PrimaryConstructorBaseTypeIfClass;
        }

        protected override bool AllowRefOrOut => !(ContainingType is { IsRecord: true } or { IsRecordStruct: true });

        internal override bool IsExpressionBodied => false;

        internal override bool IsNullableAnalysisEnabled()
        {
            return ((SourceMemberContainerTypeSymbol)ContainingType).IsNullableEnabledForConstructorsAndInitializers(IsStatic);
        }

        protected override bool IsWithinExpressionOrBlockBody(int position, out int offset)
        {
            offset = -1;
            return false;
        }

        internal override ExecutableCodeBinder TryGetBodyBinder(BinderFactory? binderFactoryOpt = null, bool ignoreAccessibility = false)
        {
            TypeDeclarationSyntax typeDecl = GetSyntax();
            Debug.Assert(typeDecl.ParameterList is not null);
            InMethodBinder result = (binderFactoryOpt ?? this.DeclaringCompilation.GetBinderFactory(typeDecl.SyntaxTree)).GetPrimaryConstructorInMethodBinder(this);
            return new ExecutableCodeBinder(SyntaxNode, this, result.WithAdditionalFlags(ignoreAccessibility ? BinderFlags.IgnoreAccessibility : BinderFlags.None));
        }

        public IEnumerable<FieldSymbol> GetBackingFields()
        {
            IReadOnlyDictionary<ParameterSymbol, FieldSymbol> capturedParameters = GetCapturedParameters();

            if (capturedParameters.Count == 0)
            {
                return SpecializedCollections.EmptyEnumerable<FieldSymbol>();
            }

            return capturedParameters.OrderBy(static pair => pair.Key.Ordinal).Select(static pair => pair.Value);
        }

        public IReadOnlyDictionary<ParameterSymbol, FieldSymbol> GetCapturedParameters()
        {
            if (_capturedParameters != null)
            {
                return _capturedParameters;
            }

            if (ContainingType is { IsRecord: true } or { IsRecordStruct: true } || ParameterCount == 0)
            {
                _capturedParameters = SpecializedCollections.EmptyReadOnlyDictionary<ParameterSymbol, FieldSymbol>();
                return _capturedParameters;
            }

            Interlocked.CompareExchange(ref _capturedParameters, Binder.GetCapturedParameters(this), null);
            return _capturedParameters;
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    // PROTOTYPE(PrimaryConstructors): Move to a Binder specific file?
    partial class Binder
    {
        public static IReadOnlyDictionary<ParameterSymbol, FieldSymbol> GetCapturedParameters(SynthesizedPrimaryConstructor primaryConstructor)
        {
            var namesToCheck = PooledHashSet<string>.GetInstance();
            addParameterNames(namesToCheck);

            if (namesToCheck.Count == 0)
            {
                namesToCheck.Free();
                return SpecializedCollections.EmptyReadOnlyDictionary<ParameterSymbol, FieldSymbol>();
            }

            var captured = ArrayBuilder<ParameterSymbol>.GetInstance(primaryConstructor.Parameters.Length);
            LookupResult? lookupResult = null;
            var containingType = primaryConstructor.ContainingType;

            foreach (var member in containingType.GetMembers())
            {
                Binder? bodyBinder;
                CSharpSyntaxNode? syntaxNode;

                getBodyBinderAndSyntaxIfPossiblyCapturingMethod(member, out bodyBinder, out syntaxNode);
                if (bodyBinder is null)
                {
                    continue;
                }

                Debug.Assert(syntaxNode is not null);

                bool keepChecking = checkParameterReferencesInMethodBody(syntaxNode, bodyBinder);
                if (!keepChecking)
                {
                    break;
                }
            }

            lookupResult?.Free();
            namesToCheck.Free();

            if (captured.Count == 0)
            {
                captured.Free();
                return SpecializedCollections.EmptyReadOnlyDictionary<ParameterSymbol, FieldSymbol>();
            }

            var result = new Dictionary<ParameterSymbol, FieldSymbol>(ReferenceEqualityComparer.Instance);

            foreach (var parameter in captured)
            {
                // PROTOTYPE(PrimaryConstructors): Figure out naming strategy
                string name = "<" + parameter.Name + ">PC__BackingField";

                // PROTOTYPE(PrimaryConstructors): Ever read-only?
                result.Add(parameter, new SynthesizedFieldSymbol(containingType, parameter.Type, name));
            }

            captured.Free();
            return result;

            void addParameterNames(PooledHashSet<string> namesToCheck)
            {
                foreach (var parameter in primaryConstructor.Parameters)
                {
                    if (parameter.Name.Length != 0)
                    {
                        namesToCheck.Add(parameter.Name);
                    }
                }
            }

            void getBodyBinderAndSyntaxIfPossiblyCapturingMethod(Symbol member, out Binder? bodyBinder, out CSharpSyntaxNode? syntaxNode)
            {
                bodyBinder = null;
                syntaxNode = null;

                if ((object)member == primaryConstructor)
                {
                    return;
                }

                if (member.IsStatic ||
                    !(member is MethodSymbol method && MethodCompiler.GetMethodToCompile(method) is SourceMemberMethodSymbol sourceMethod))
                {
                    return;
                }

                if (sourceMethod.IsExtern)
                {
                    return;
                }

                bodyBinder = sourceMethod.TryGetBodyBinder();

                if (bodyBinder is null)
                {
                    return;
                }

                syntaxNode = sourceMethod.SyntaxNode;
            }

            bool checkParameterReferencesInMethodBody(CSharpSyntaxNode syntaxNode, Binder bodyBinder)
            {
                switch (syntaxNode)
                {
                    case ConstructorDeclarationSyntax s:
                        return checkParameterReferencesInNode(s.Initializer, bodyBinder) &&
                               checkParameterReferencesInNode(s.Body, bodyBinder) &&
                               checkParameterReferencesInNode(s.ExpressionBody, bodyBinder);

                    case BaseMethodDeclarationSyntax s:
                        return checkParameterReferencesInNode(s.Body, bodyBinder) &&
                               checkParameterReferencesInNode(s.ExpressionBody, bodyBinder);

                    case AccessorDeclarationSyntax s:
                        return checkParameterReferencesInNode(s.Body, bodyBinder) &&
                               checkParameterReferencesInNode(s.ExpressionBody, bodyBinder);

                    case ArrowExpressionClauseSyntax s:
                        return checkParameterReferencesInNode(s, bodyBinder);

                    default:
                        throw ExceptionUtilities.UnexpectedValue(syntaxNode);
                }
            }

            bool checkParameterReferencesInNode(CSharpSyntaxNode? node, Binder binder)
            {
                if (node == null)
                {
                    return true;
                }

                var nodesOfInterest = node.DescendantNodesAndSelf(descendIntoChildren: childrenNeedChecking, descendIntoTrivia: false).Where(nodeNeedsChecking);

                foreach (var n in nodesOfInterest)
                {
                    Binder enclosingBinder = getEnclosingBinderForNode(contextNode: node, contextBinder: binder, n);

                    switch (n)
                    {
                        case AnonymousFunctionExpressionSyntax lambdaSyntax:
                            if (!checkLambda(lambdaSyntax, enclosingBinder))
                            {
                                return false;
                            }

                            break;

                        case IdentifierNameSyntax id:

                            string name = id.Identifier.ValueText;
                            Debug.Assert(namesToCheck.Contains(name));
                            if (!checkIdentifier(enclosingBinder, id))
                            {
                                return false;
                            }

                            break;

                        case QueryExpressionSyntax query:
                            if (!checkQuery(query, enclosingBinder))
                            {
                                return false;
                            }

                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(n.Kind());
                    }
                }

                return true;
            }

            bool checkLambda(AnonymousFunctionExpressionSyntax lambdaSyntax, Binder enclosingBinder)
            {
                UnboundLambda unboundLambda = enclosingBinder.AnalyzeAnonymousFunction(lambdaSyntax, BindingDiagnosticBag.Discarded);
                var lambdaBodyBinder = createLambdaBodyBinder(enclosingBinder, unboundLambda);
                return checkParameterReferencesInNode(lambdaSyntax.Body, lambdaBodyBinder.GetBinder(lambdaSyntax.Body) ?? lambdaBodyBinder);
            }

            static ExecutableCodeBinder createLambdaBodyBinder(Binder enclosingBinder, UnboundLambda unboundLambda)
            {
                unboundLambda.HasExplicitReturnType(out RefKind refKind, out TypeWithAnnotations returnType);
                var lambdaSymbol = new LambdaSymbol(
                                        enclosingBinder,
                                        enclosingBinder.Compilation,
                                        enclosingBinder.ContainingMemberOrLambda!,
                                        unboundLambda,
                                        ImmutableArray<TypeWithAnnotations>.Empty,
                                        ImmutableArray<RefKind>.Empty,
                                        refKind,
                                        returnType);

                return new ExecutableCodeBinder(unboundLambda.Syntax, lambdaSymbol, unboundLambda.GetWithParametersBinder(lambdaSymbol, enclosingBinder));
            }

            bool checkIdentifier(Binder enclosingBinder, IdentifierNameSyntax id)
            {
                Debug.Assert(lookupResult?.IsClear != false);
                lookupResult ??= LookupResult.GetInstance();

                var useSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                enclosingBinder.LookupIdentifier(lookupResult, id, SyntaxFacts.IsInvoked(id), ref useSiteInfo);

                if (lookupResult.IsMultiViable)
                {
                    bool? isInsideNameof = null;
                    bool detectedCapture = false;

                    foreach (var candidate in lookupResult.Symbols)
                    {
                        if (candidate is ParameterSymbol parameter && parameter.ContainingSymbol == (object)primaryConstructor)
                        {
                            isInsideNameof ??= enclosingBinder.IsInsideNameof;

                            if (isInsideNameof.GetValueOrDefault())
                            {
                                break;
                            }
                            else if (lookupResult.IsSingleViable)
                            {
                                Debug.Assert(lookupResult.SingleSymbolOrDefault == (object)parameter);

                                // Check for left of potential color color member access 
                                if (isTypeOrValueReceiver(enclosingBinder, id, parameter.Type, out SyntaxNode? memberAccessNode, out string? memberName, out int targetMemberArity, out bool invoked))
                                {
                                    lookupResult.Clear();
                                    if (treatAsInstanceMemberAccess(enclosingBinder, parameter.Type, memberAccessNode, memberName, targetMemberArity, invoked))
                                    {
                                        captured.Add(parameter);
                                        detectedCapture = true;
                                    }

                                    // We cleared the lookupResult and the candidate list within it.
                                    // Do not attempt to continue the enclosing foreach
                                    break;
                                }
                            }

                            captured.Add(parameter);
                            detectedCapture = true;
                        }
                    }

                    if (detectedCapture)
                    {
                        namesToCheck.Remove(id.Identifier.ValueText);

                        if (namesToCheck.Count == 0)
                        {
                            return false;
                        }
                    }
                }

                lookupResult.Clear();
                return true;
            }

            bool isTypeOrValueReceiver(
                Binder enclosingBinder,
                IdentifierNameSyntax id,
                TypeSymbol type,
                [NotNullWhen(true)] out SyntaxNode? memberAccessNode,
                [NotNullWhen(true)] out string? memberName,
                out int targetMemberArity,
                out bool invoked)
            {
                memberAccessNode = null;
                memberName = null;
                targetMemberArity = 0;
                invoked = false;

                switch (id.Parent)
                {
                    case MemberAccessExpressionSyntax { RawKind: (int)SyntaxKind.SimpleMemberAccessExpression } memberAccess when memberAccess.Expression == id:
                        var simpleName = memberAccess.Name;
                        memberAccessNode = simpleName;
                        memberName = simpleName.Identifier.ValueText;
                        targetMemberArity = simpleName.Arity;
                        invoked = SyntaxFacts.IsInvoked(memberAccess);
                        break;
                    case QualifiedNameSyntax qualifiedName when qualifiedName.Left == id:
                        simpleName = qualifiedName.Right;
                        memberAccessNode = simpleName;
                        memberName = simpleName.Identifier.ValueText;
                        targetMemberArity = simpleName.Arity;
                        invoked = false;
                        break;
                    case FromClauseSyntax { Parent: QueryExpressionSyntax query } fromClause when query.FromClause == fromClause && fromClause.Expression == id:
                        memberName = GetFirstInvokedMethodName(query, out memberAccessNode);
                        targetMemberArity = 0;
                        invoked = true;
                        break;
                }

                return memberAccessNode is not null && enclosingBinder.IsPotentialColorColorReceiver(id, type);
            }

            // Follows the logic of BindInstanceMemberAccess
            bool treatAsInstanceMemberAccess(
                Binder enclosingBinder,
                TypeSymbol type,
                SyntaxNode memberAccessNode,
                string memberName,
                int targetMemberArity,
                bool invoked)
            {
                Debug.Assert(!type.IsDynamic());
                Debug.Assert(lookupResult.IsClear);

                var useSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                enclosingBinder.LookupInstanceMember(lookupResult, type, leftIsBaseReference: false, memberName, targetMemberArity, invoked, ref useSiteInfo);

                bool treatAsInstanceMemberAccess;
                if (lookupResult.IsMultiViable)
                {
                    // This branch follows the logic of BindMemberOfType
                    Debug.Assert(lookupResult.Symbols.Any());

                    var members = ArrayBuilder<Symbol>.GetInstance();
                    Symbol symbol = enclosingBinder.GetSymbolOrMethodOrPropertyGroup(lookupResult, memberAccessNode, memberName, targetMemberArity, members, BindingDiagnosticBag.Discarded, wasError: out _,
                                                                                     qualifierOpt: null);

                    if ((object)symbol == null)
                    {
                        Debug.Assert(members.Count > 0);

                        bool haveInstanceCandidates;
                        lookupResult.Clear();
                        enclosingBinder.CheckWhatCandidatesWeHave(members, type, memberName, targetMemberArity,
                                                                  ref lookupResult, ref useSiteInfo,
                                                                  out haveInstanceCandidates, out _);

                        treatAsInstanceMemberAccess = haveInstanceCandidates;
                    }
                    else
                    {
                        // methods are special because of extension methods.
                        Debug.Assert(symbol.Kind != SymbolKind.Method);
                        treatAsInstanceMemberAccess = !(symbol.IsStatic || symbol.Kind == SymbolKind.NamedType);
                    }

                    members.Free();
                }
                else
                {
                    // At this point this could only be an extension method access or an error
                    treatAsInstanceMemberAccess = true;
                }

                lookupResult.Clear();
                return treatAsInstanceMemberAccess;
            }

            bool checkQuery(QueryExpressionSyntax query, Binder enclosingBinder)
            {
                if (checkParameterReferencesInNode(query.FromClause.Expression, enclosingBinder))
                {
                    (QueryTranslationState state, _) = enclosingBinder.MakeInitialQueryTranslationState(query, BindingDiagnosticBag.Discarded);

                    bool result = bindQueryInternal(enclosingBinder, state);

                    for (QueryContinuationSyntax? continuation = query.Body.Continuation; continuation != null && result; continuation = continuation.Body.Continuation)
                    {
                        // A query expression with a continuation
                        //     from ... into x ...
                        // is translated into
                        //     from x in ( from ... ) ...
                        enclosingBinder.PrepareQueryTranslationStateForContinuation(state, continuation, BindingDiagnosticBag.Discarded);
                        result = bindQueryInternal(enclosingBinder, state);
                    }

                    state.Free();
                    return result;
                }

                return false;
            }

            bool bindQueryInternal(Binder enclosingBinder, QueryTranslationState state)
            {
                // we continue reducing the query until it is reduced away.
                do
                {
                    if (state.clauses.IsEmpty())
                    {
                        return finalTranslation(enclosingBinder, state);
                    }
                }
                while (reduceQuery(enclosingBinder, state));

                return false;
            }

            bool finalTranslation(Binder enclosingBinder, QueryTranslationState state)
            {
                Debug.Assert(state.clauses.IsEmpty());
                switch (state.selectOrGroup.Kind())
                {
                    case SyntaxKind.SelectClause:
                        {
                            // A query expression of the form
                            //     from x in e select v
                            // is translated into
                            //     ( e ) . Select ( x => v )
                            var selectClause = (SelectClauseSyntax)state.selectOrGroup;
                            var x = state.rangeVariable;
                            var v = selectClause.Expression;
                            return makeQueryUnboundLambda(enclosingBinder, state.RangeVariableMap(), x, v);
                        }
                    case SyntaxKind.GroupClause:
                        {
                            // A query expression of the form
                            //     from x in e group v by k
                            // is translated into
                            //     ( e ) . GroupBy ( x => k , x => v )
                            // except when v is the identifier x, the translation is
                            //     ( e ) . GroupBy ( x => k )
                            var groupClause = (GroupClauseSyntax)state.selectOrGroup;
                            var x = state.rangeVariable;
                            var v = groupClause.GroupExpression;
                            var k = groupClause.ByExpression;

                            return makeQueryUnboundLambda(enclosingBinder, state.RangeVariableMap(), x, k) &&
                                   makeQueryUnboundLambda(enclosingBinder, state.RangeVariableMap(), x, v);
                        }
                    default:
                        {
                            // there should have been a syntax error if we get here.
                            return true;
                        }
                }
            }

            bool reduceQuery(Binder enclosingBinder, QueryTranslationState state)
            {
                var topClause = state.clauses.Pop();
                switch (topClause.Kind())
                {
                    case SyntaxKind.WhereClause:
                        return reduceWhere(enclosingBinder, (WhereClauseSyntax)topClause, state);
                    case SyntaxKind.JoinClause:
                        return reduceJoin(enclosingBinder, (JoinClauseSyntax)topClause, state);
                    case SyntaxKind.OrderByClause:
                        return reduceOrderBy(enclosingBinder, (OrderByClauseSyntax)topClause, state);
                    case SyntaxKind.FromClause:
                        return reduceFrom(enclosingBinder, (FromClauseSyntax)topClause, state);
                    case SyntaxKind.LetClause:
                        return reduceLet(enclosingBinder, (LetClauseSyntax)topClause, state);
                    default:
                        throw ExceptionUtilities.UnexpectedValue(topClause.Kind());
                }
            }

            bool reduceWhere(Binder enclosingBinder, WhereClauseSyntax where, QueryTranslationState state)
            {
                // A query expression with a where clause
                //     from x in e
                //     where f
                //     ...
                // is translated into
                //     from x in ( e ) . Where ( x => f )
                return makeQueryUnboundLambda(enclosingBinder, state.RangeVariableMap(), state.rangeVariable, where.Condition);
            }

            bool reduceJoin(Binder enclosingBinder, JoinClauseSyntax join, QueryTranslationState state)
            {
                if (checkParameterReferencesInNode(join.InExpression, enclosingBinder) &&
                    makeQueryUnboundLambda(enclosingBinder, state.RangeVariableMap(), state.rangeVariable, join.LeftExpression))
                {
                    var x2 = state.AddRangeVariable(enclosingBinder, join.Identifier, BindingDiagnosticBag.Discarded);

                    if (makeQueryUnboundLambda(enclosingBinder, QueryTranslationState.RangeVariableMap(x2), x2, join.RightExpression))
                    {
                        if (join.Into != null)
                        {
                            state.allRangeVariables[x2].Free();
                            state.allRangeVariables.Remove(x2);

                            state.AddRangeVariable(enclosingBinder, join.Into.Identifier, BindingDiagnosticBag.Discarded);
                        }

                        return true;
                    }
                }

                return false;
            }

            bool reduceOrderBy(Binder enclosingBinder, OrderByClauseSyntax orderby, QueryTranslationState state)
            {
                // A query expression with an orderby clause
                //     from x in e
                //     orderby k1 , k2 , ... , kn
                //     ...
                // is translated into
                //     from x in ( e ) . 
                //     OrderBy ( x => k1 ) . 
                //     ThenBy ( x => k2 ) .
                //     ... .
                //     ThenBy ( x => kn )
                //     ...
                // If an ordering clause specifies a descending direction indicator,
                // an invocation of OrderByDescending or ThenByDescending is produced instead.
                foreach (var ordering in orderby.Orderings)
                {
                    if (!makeQueryUnboundLambda(enclosingBinder, state.RangeVariableMap(), state.rangeVariable, ordering.Expression))
                    {
                        return false;
                    }
                }

                return true;
            }

            bool reduceFrom(Binder enclosingBinder, FromClauseSyntax from, QueryTranslationState state)
            {
                var x1 = state.rangeVariable;
                if (makeQueryUnboundLambda(enclosingBinder, state.RangeVariableMap(), x1, from.Expression))
                {
                    state.AddRangeVariable(enclosingBinder, from.Identifier, BindingDiagnosticBag.Discarded);
                    return true;
                }

                return false;
            }

            bool reduceLet(Binder enclosingBinder, LetClauseSyntax let, QueryTranslationState state)
            {
                // A query expression with a let clause
                //     from x in e
                //     let y = f
                //     ...
                // is translated into
                //     from * in ( e ) . Select ( x => new { x , y = f } )
                //     ...
                var x = state.rangeVariable;

                if (makeQueryUnboundLambda(enclosingBinder, state.RangeVariableMap(), x, let.Expression))
                {
                    state.rangeVariable = state.TransparentRangeVariable(enclosingBinder);
                    state.AddTransparentIdentifier(x.Name);
                    var y = state.AddRangeVariable(enclosingBinder, let.Identifier, BindingDiagnosticBag.Discarded);
                    state.allRangeVariables[y].Add(let.Identifier.ValueText);
                    return true;
                }

                return false;
            }

            bool makeQueryUnboundLambda(Binder enclosingBinder, RangeVariableMap qvm, RangeVariableSymbol parameter, ExpressionSyntax expression)
            {
                UnboundLambda unboundLambda = MakeQueryUnboundLambda(
                    expression,
                    new QueryUnboundLambdaState(
                        enclosingBinder, qvm, ImmutableArray.Create(parameter),
                        (LambdaSymbol lambdaSymbol, Binder lambdaBodyBinder, BindingDiagnosticBag diagnostics) => throw ExceptionUtilities.Unreachable()),
                    withDependencies: false);

                var lambdaBodyBinder = createLambdaBodyBinder(enclosingBinder, unboundLambda);
                return checkParameterReferencesInNode(expression, lambdaBodyBinder.GetRequiredBinder(expression));
            }

            static Binder getEnclosingBinderForNode(CSharpSyntaxNode contextNode, Binder contextBinder, SyntaxNode targetNode)
            {
                while (true)
                {
                    Binder? enclosingBinder = contextBinder.GetBinder(targetNode);

                    if (enclosingBinder is not null)
                    {
                        return enclosingBinder;
                    }

                    if (targetNode == contextNode)
                    {
                        return contextBinder;
                    }

                    Debug.Assert(targetNode.Parent is not null);
                    targetNode = targetNode.Parent;
                }
            }

            static bool childrenNeedChecking(SyntaxNode n)
            {
                switch (n)
                {
                    case MemberBindingExpressionSyntax:
                    case BaseExpressionColonSyntax:
                    case NameEqualsSyntax:
                    case GotoStatementSyntax { RawKind: (int)SyntaxKind.GotoStatement }:
                    case TypeParameterConstraintClauseSyntax:
                    case AliasQualifiedNameSyntax:
                        // These nodes do not have anything interesting for us
                        return false;

                    case AttributeListSyntax:
                        // References in attributes, if any, are errors
                        // Skip them
                        return false;

                    case ParameterSyntax:
                        // Same as attributes
                        return false;

                    case AnonymousFunctionExpressionSyntax:
                    case QueryExpressionSyntax:
                        // Lambdas need special handling
                        return false;

                    case ExpressionSyntax expression:
                        if (SyntaxFacts.IsInTypeOnlyContext(expression) &&
                            !(expression.Parent is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.IsExpression } isExpression &&
                                isExpression.Right == expression))
                        {
                            return false;
                        }
                        break;
                }

                return true;
            }

            bool nodeNeedsChecking(SyntaxNode n)
            {
                switch (n)
                {
                    case AnonymousFunctionExpressionSyntax:
                    case QueryExpressionSyntax:
                        return true;

                    case IdentifierNameSyntax id:

                        switch (id.Parent)
                        {
                            case MemberAccessExpressionSyntax memberAccess:
                                if (memberAccess.Expression != id)
                                {
                                    return false;
                                }
                                break;

                            case QualifiedNameSyntax qualifiedName:
                                if (qualifiedName.Left != id)
                                {
                                    return false;
                                }
                                break;

                            case AssignmentExpressionSyntax assignment:
                                if (assignment.Left == id &&
                                    assignment.Parent?.Kind() is SyntaxKind.ObjectInitializerExpression or SyntaxKind.WithInitializerExpression)
                                {
                                    return false;
                                }
                                break;
                        }

                        if (SyntaxFacts.IsInTypeOnlyContext(id) &&
                            !(id.Parent is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.IsExpression } isExpression &&
                                isExpression.Right == id))
                        {
                            return false;
                        }

                        if (namesToCheck.Contains(id.Identifier.ValueText))
                        {
                            return true;
                        }
                        break;
                }

                return false;
            }
        }
    }
}
