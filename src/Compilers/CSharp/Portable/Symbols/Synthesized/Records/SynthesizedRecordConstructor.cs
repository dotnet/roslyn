// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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

            var containingType = ContainingType;

            if (containingType is { IsRecord: true } or { IsRecordStruct: true } || ParameterCount == 0)
            {
                _capturedParameters = SpecializedCollections.EmptyReadOnlyDictionary<ParameterSymbol, FieldSymbol>();
                return _capturedParameters;
            }

            var namesToCheck = PooledHashSet<string>.GetInstance();
            var captured = ArrayBuilder<ParameterSymbol>.GetInstance(Parameters.Length);
            LookupResult? lookupResult = null;

            addParameterNames(namesToCheck);

            if (namesToCheck.Count != 0)
            {
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
            }

            lookupResult?.Free();
            namesToCheck.Free();

            if (captured.Count == 0)
            {
                _capturedParameters = SpecializedCollections.EmptyReadOnlyDictionary<ParameterSymbol, FieldSymbol>();
            }
            else
            {
                var result = new Dictionary<ParameterSymbol, FieldSymbol>(ReferenceEqualityComparer.Instance);

                foreach (var parameter in captured)
                {
                    // PROTOTYPE(PrimaryConstructors): Figure out naming strategy
                    string name = "<" + parameter.Name + ">PC__BackingField";

                    // PROTOTYPE(PrimaryConstructors): Ever read-only?
                    result.Add(parameter, new SynthesizedFieldSymbol(containingType, parameter.Type, name));
                }

                Interlocked.CompareExchange(ref _capturedParameters, result, null);
            }

            captured.Free();

            return _capturedParameters;

            void addParameterNames(PooledHashSet<string> namesToCheck)
            {
                foreach (var parameter in Parameters)
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

                if ((object)member == this)
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

                var lambdasAndIdentifiers = node.DescendantNodes(descendIntoChildren: childrenNeedChecking, descendIntoTrivia: false).Where(nodeNeedsChecking);

                foreach (var n in lambdasAndIdentifiers)
                {
                    switch (n)
                    {
                        case LambdaExpressionSyntax:
                        case AnonymousMethodExpressionSyntax:
                            // PROTOTYPE(PrimaryConstructors): Dig into lambdas
                            // PROTOTYPE(PrimaryConstructors): Make sure to cover discard parameters
                            break;

                        case IdentifierNameSyntax id:

                            string name = id.Identifier.ValueText;
                            Debug.Assert(namesToCheck.Contains(name));
                            Binder enclosingBinder = getEnclosingBinderForNode(contextNode: node, contextBinder: binder, id);
                            if (!registerCapture(identifierRefersToParameter(enclosingBinder, id)))
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

            bool registerCapture(ParameterSymbol? parameter)
            {
                if (parameter is not null)
                {
                    Debug.Assert(parameter.ContainingSymbol == (object)this);

                    captured.Add(parameter);
                    namesToCheck.Remove(parameter.Name);

                    if (namesToCheck.Count == 0)
                    {
                        return false;
                    }
                }

                return true;
            }

            ParameterSymbol? identifierRefersToParameter(Binder enclosingBinder, IdentifierNameSyntax id)
            {
                // PROTOTYPE(PrimaryConstructors): Identifiers inside nameof are not really references 
                // PROTOTYPE(PrimaryConstructors): Handle "Color Color" scenario when parameter reference is getting reinterpreted as a type reference instead. 

                lookupResult ??= LookupResult.GetInstance();
                lookupResult.Clear();

                LookupOptions options = LookupOptions.AllMethodsOnArityZero;
                if (SyntaxFacts.IsInvoked(id))
                {
                    options |= LookupOptions.MustBeInvocableIfMember;
                }

                var useSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                enclosingBinder.LookupSymbolsWithFallback(lookupResult, id.Identifier.ValueText, arity: 0, useSiteInfo: ref useSiteInfo, options: options);

                if (lookupResult.IsSingleViable && lookupResult.SingleSymbolOrDefault is ParameterSymbol parameter && parameter.ContainingSymbol == (object)this)
                {
                    return parameter;
                }

                return null;
            }

            static Binder getEnclosingBinderForNode(CSharpSyntaxNode contextNode, Binder contextBinder, CSharpSyntaxNode targetNode)
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

                    case LambdaExpressionSyntax:
                    case AnonymousMethodExpressionSyntax:
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
                    case LambdaExpressionSyntax:
                    case AnonymousMethodExpressionSyntax:
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
