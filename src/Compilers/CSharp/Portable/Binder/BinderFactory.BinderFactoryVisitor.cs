// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class BinderFactory
    {
        private sealed class BinderFactoryVisitor : CSharpSyntaxVisitor<Binder>
        {
            private int _position;
            private CSharpSyntaxNode _memberDeclarationOpt;
            private Symbol _memberOpt;
            private readonly BinderFactory _factory;

            internal BinderFactoryVisitor(BinderFactory factory)
            {
                _factory = factory;
            }

            internal void Initialize(int position, CSharpSyntaxNode memberDeclarationOpt, Symbol memberOpt)
            {
                Debug.Assert((memberDeclarationOpt == null) == (memberOpt == null));

                _position = position;
                _memberDeclarationOpt = memberDeclarationOpt;
                _memberOpt = memberOpt;
            }

            private CSharpCompilation compilation
            {
                get
                {
                    return _factory._compilation;
                }
            }

            private SyntaxTree syntaxTree
            {
                get
                {
                    return _factory._syntaxTree;
                }
            }

            private BuckStopsHereBinder buckStopsHereBinder
            {
                get
                {
                    return _factory._buckStopsHereBinder;
                }
            }

            private ConcurrentCache<BinderCacheKey, Binder> binderCache
            {
                get
                {
                    return _factory._binderCache;
                }
            }

            private bool InScript
            {
                get
                {
                    return _factory.InScript;
                }
            }

            public override Binder DefaultVisit(SyntaxNode parent)
            {
                return VisitCore(parent.Parent);
            }

            // node, for which we are trying to find a binder is not supposed to be null
            // so we do not need to handle null in the Visit
            public override Binder Visit(SyntaxNode node)
            {
                return VisitCore(node);
            }

            //PERF: nonvirtual implementation of Visit
            private Binder VisitCore(SyntaxNode node)
            {
                return ((CSharpSyntaxNode)node).Accept(this);
            }

            public override Binder VisitGlobalStatement(GlobalStatementSyntax node)
            {
                if (SyntaxFacts.IsSimpleProgramTopLevelStatement(node))
                {
                    var compilationUnit = (CompilationUnitSyntax)node.Parent;

                    if (compilationUnit != syntaxTree.GetRoot())
                    {
                        throw new ArgumentOutOfRangeException(nameof(node), "node not part of tree");
                    }

                    var key = CreateBinderCacheKey(compilationUnit, NodeUsage.MethodBody);

                    Binder result;
                    if (!binderCache.TryGetValue(key, out result))
                    {
                        SynthesizedSimpleProgramEntryPointSymbol simpleProgram = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(compilation, (CompilationUnitSyntax)node.Parent, fallbackToMainEntryPoint: false);
                        ExecutableCodeBinder bodyBinder = simpleProgram.GetBodyBinder(_factory._ignoreAccessibility);
                        result = bodyBinder.GetBinder(compilationUnit);

                        binderCache.TryAdd(key, result);
                    }

                    return result;
                }

                return base.VisitGlobalStatement(node);
            }

            // This is used mainly by the method body binder.  During construction of the method symbol,
            // the contexts are built "by hand" rather than by this builder (see
            // MethodMemberBuilder.EnsureDeclarationBound).
            public override Binder VisitMethodDeclaration(MethodDeclarationSyntax methodDecl)
            {
                if (!LookupPosition.IsInMethodDeclaration(_position, methodDecl))
                {
                    return VisitCore(methodDecl.Parent);
                }

                NodeUsage usage;
                if (LookupPosition.IsInBody(_position, methodDecl))
                {
                    usage = NodeUsage.MethodBody;
                }
                else if (LookupPosition.IsInMethodTypeParameterScope(_position, methodDecl))
                {
                    usage = NodeUsage.MethodTypeParameters;
                }
                else
                {
                    // Normal - is when method itself is not involved (will use outer binder)
                    //          that would be if position is within the return type or method name
                    usage = NodeUsage.Normal;
                }

                var key = CreateBinderCacheKey(methodDecl, usage);

                Binder resultBinder;
                if (!binderCache.TryGetValue(key, out resultBinder))
                {
                    var parentType = methodDecl.Parent as TypeDeclarationSyntax;
                    if (parentType != null)
                    {
                        resultBinder = VisitTypeDeclarationCore(parentType, NodeUsage.NamedTypeBodyOrTypeParameters);
                    }
                    else
                    {
                        resultBinder = VisitCore(methodDecl.Parent);
                    }

                    SourceMemberMethodSymbol method = null;

                    if (usage != NodeUsage.Normal && methodDecl.TypeParameterList != null)
                    {
                        method = GetMethodSymbol(methodDecl, resultBinder);
                        resultBinder = new WithMethodTypeParametersBinder(method, resultBinder);
                    }

                    if (usage == NodeUsage.MethodBody)
                    {
                        method = method ?? GetMethodSymbol(methodDecl, resultBinder);
                        resultBinder = new InMethodBinder(method, resultBinder);
                    }

                    resultBinder = resultBinder.WithUnsafeRegionIfNecessary(methodDecl.Modifiers);
                    binderCache.TryAdd(key, resultBinder);
                }

                return resultBinder;
            }

            public override Binder VisitConstructorDeclaration(ConstructorDeclarationSyntax parent)
            {
                // If the position isn't in the scope of the method, then proceed to the parent syntax node.
                if (!LookupPosition.IsInMethodDeclaration(_position, parent))
                {
                    return VisitCore(parent.Parent);
                }

                bool inBodyOrInitializer = LookupPosition.IsInConstructorParameterScope(_position, parent);
                var extraInfo = inBodyOrInitializer ? NodeUsage.ConstructorBodyOrInitializer : NodeUsage.Normal;  // extra info for the cache.
                var key = CreateBinderCacheKey(parent, extraInfo);

                Binder resultBinder;
                if (!binderCache.TryGetValue(key, out resultBinder))
                {
                    resultBinder = VisitCore(parent.Parent);

                    // NOTE: Don't get the method symbol unless we're sure we need it.
                    if (inBodyOrInitializer)
                    {
                        var method = GetMethodSymbol(parent, resultBinder);
                        if ((object)method != null)
                        {
                            // Ctors cannot be generic
                            //TODO: the error should be given in a different place, but should we ignore or consider the type args?
                            Debug.Assert(method.Arity == 0, "Generic Ctor, What to do?");

                            resultBinder = new InMethodBinder(method, resultBinder);
                        }
                    }

                    resultBinder = resultBinder.WithUnsafeRegionIfNecessary(parent.Modifiers);

                    binderCache.TryAdd(key, resultBinder);
                }

                return resultBinder;
            }

            public override Binder VisitDestructorDeclaration(DestructorDeclarationSyntax parent)
            {
                // If the position isn't in the scope of the method, then proceed to the parent syntax node.
                if (!LookupPosition.IsInBody(_position, parent))
                {
                    return VisitCore(parent.Parent);
                }

                var key = CreateBinderCacheKey(parent, usage: NodeUsage.Normal);

                Binder resultBinder;
                if (!binderCache.TryGetValue(key, out resultBinder))
                {
                    // Destructors have neither parameters nor type parameters, so there's nothing special to do here.
                    resultBinder = VisitCore(parent.Parent);

                    SourceMemberMethodSymbol method = GetMethodSymbol(parent, resultBinder);
                    resultBinder = new InMethodBinder(method, resultBinder);

                    resultBinder = resultBinder.WithUnsafeRegionIfNecessary(parent.Modifiers);

                    binderCache.TryAdd(key, resultBinder);
                }

                return resultBinder;
            }

            public override Binder VisitAccessorDeclaration(AccessorDeclarationSyntax parent)
            {
                // If the position isn't in the scope of the method, then proceed to the parent syntax node.
                if (!LookupPosition.IsInMethodDeclaration(_position, parent))
                {
                    return VisitCore(parent.Parent);
                }

                bool inBody = LookupPosition.IsInBody(_position, parent);
                var extraInfo = inBody ? NodeUsage.AccessorBody : NodeUsage.Normal;  // extra info for the cache.
                var key = CreateBinderCacheKey(parent, extraInfo);

                Binder resultBinder;
                if (!binderCache.TryGetValue(key, out resultBinder))
                {
                    resultBinder = VisitCore(parent.Parent);

                    if (inBody)
                    {
                        var propertyOrEventDecl = parent.Parent.Parent;
                        MethodSymbol accessor = null;

                        switch (propertyOrEventDecl.Kind())
                        {
                            case SyntaxKind.PropertyDeclaration:
                            case SyntaxKind.IndexerDeclaration:
                                {
                                    var propertySymbol = GetPropertySymbol((BasePropertyDeclarationSyntax)propertyOrEventDecl, resultBinder);
                                    if ((object)propertySymbol != null)
                                    {
                                        accessor = (parent.Kind() == SyntaxKind.GetAccessorDeclaration) ? propertySymbol.GetMethod : propertySymbol.SetMethod;
                                    }
                                    break;
                                }
                            case SyntaxKind.EventDeclaration:
                            case SyntaxKind.EventFieldDeclaration:
                                {
                                    // NOTE: it's an error for field-like events to have accessors, 
                                    // but we want to bind them anyway for error tolerance reasons.

                                    var eventSymbol = GetEventSymbol((EventDeclarationSyntax)propertyOrEventDecl, resultBinder);
                                    if ((object)eventSymbol != null)
                                    {
                                        accessor = (parent.Kind() == SyntaxKind.AddAccessorDeclaration) ? eventSymbol.AddMethod : eventSymbol.RemoveMethod;
                                    }
                                    break;
                                }
                            default:
                                throw ExceptionUtilities.UnexpectedValue(propertyOrEventDecl.Kind());
                        }

                        if ((object)accessor != null)
                        {
                            resultBinder = new InMethodBinder(accessor, resultBinder);
                        }
                    }

                    binderCache.TryAdd(key, resultBinder);
                }

                return resultBinder;
            }

            private Binder VisitOperatorOrConversionDeclaration(BaseMethodDeclarationSyntax parent)
            {
                // If the position isn't in the scope of the method, then proceed to the parent syntax node.
                if (!LookupPosition.IsInMethodDeclaration(_position, parent))
                {
                    return VisitCore(parent.Parent);
                }

                bool inBody = LookupPosition.IsInBody(_position, parent);
                var extraInfo = inBody ? NodeUsage.OperatorBody : NodeUsage.Normal;  // extra info for the cache.
                var key = CreateBinderCacheKey(parent, extraInfo);

                Binder resultBinder;
                if (!binderCache.TryGetValue(key, out resultBinder))
                {
                    resultBinder = VisitCore(parent.Parent);

                    MethodSymbol method = GetMethodSymbol(parent, resultBinder);
                    if ((object)method != null && inBody)
                    {
                        resultBinder = new InMethodBinder(method, resultBinder);
                    }

                    resultBinder = resultBinder.WithUnsafeRegionIfNecessary(parent.Modifiers);

                    binderCache.TryAdd(key, resultBinder);
                }

                return resultBinder;
            }

            public override Binder VisitOperatorDeclaration(OperatorDeclarationSyntax parent)
            {
                return VisitOperatorOrConversionDeclaration(parent);
            }

            public override Binder VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax parent)
            {
                return VisitOperatorOrConversionDeclaration(parent);
            }

            public override Binder VisitFieldDeclaration(FieldDeclarationSyntax parent)
            {
                return VisitCore(parent.Parent).WithUnsafeRegionIfNecessary(parent.Modifiers);
            }

            public override Binder VisitEventDeclaration(EventDeclarationSyntax parent)
            {
                return VisitCore(parent.Parent).WithUnsafeRegionIfNecessary(parent.Modifiers);
            }

            public override Binder VisitEventFieldDeclaration(EventFieldDeclarationSyntax parent)
            {
                return VisitCore(parent.Parent).WithUnsafeRegionIfNecessary(parent.Modifiers);
            }

            public override Binder VisitPropertyDeclaration(PropertyDeclarationSyntax parent)
            {
                if (!LookupPosition.IsInBody(_position, parent))
                {
                    return VisitCore(parent.Parent).WithUnsafeRegionIfNecessary(parent.Modifiers);
                }

                return VisitPropertyOrIndexerExpressionBody(parent);
            }

            public override Binder VisitIndexerDeclaration(IndexerDeclarationSyntax parent)
            {
                if (!LookupPosition.IsInBody(_position, parent))
                {
                    return VisitCore(parent.Parent).WithUnsafeRegionIfNecessary(parent.Modifiers);
                }

                return VisitPropertyOrIndexerExpressionBody(parent);
            }

            private Binder VisitPropertyOrIndexerExpressionBody(BasePropertyDeclarationSyntax parent)
            {
                var key = CreateBinderCacheKey(parent, NodeUsage.AccessorBody);

                Binder resultBinder;
                if (!binderCache.TryGetValue(key, out resultBinder))
                {
                    resultBinder = VisitCore(parent.Parent).WithUnsafeRegionIfNecessary(parent.Modifiers);

                    var propertySymbol = GetPropertySymbol(parent, resultBinder);
                    var accessor = propertySymbol.GetMethod;
                    if ((object)accessor != null)
                    {
                        resultBinder = new InMethodBinder(accessor, resultBinder);
                    }

                    binderCache.TryAdd(key, resultBinder);
                }

                return resultBinder;
            }

            private NamedTypeSymbol GetContainerType(Binder binder, CSharpSyntaxNode node)
            {
                Symbol containingSymbol = binder.ContainingMemberOrLambda;
                var container = containingSymbol as NamedTypeSymbol;
                if ((object)container == null)
                {
                    Debug.Assert(containingSymbol is NamespaceSymbol);
                    if (node.Parent.Kind() == SyntaxKind.CompilationUnit && syntaxTree.Options.Kind != SourceCodeKind.Regular)
                    {
                        container = compilation.ScriptClass;
                    }
                    else
                    {
                        container = ((NamespaceSymbol)containingSymbol).ImplicitType;
                    }
                }

                return container;
            }

            /// <summary>
            /// Get the name of the method so that it can be looked up in the containing type.
            /// </summary>
            /// <param name="baseMethodDeclarationSyntax">Non-null declaration syntax.</param>
            /// <param name="outerBinder">Binder for the scope around the method (may be null for operators, constructors, and destructors).</param>
            private static string GetMethodName(BaseMethodDeclarationSyntax baseMethodDeclarationSyntax, Binder outerBinder)
            {
                switch (baseMethodDeclarationSyntax.Kind())
                {
                    case SyntaxKind.ConstructorDeclaration:
                        return (baseMethodDeclarationSyntax.Modifiers.Any(SyntaxKind.StaticKeyword) ? WellKnownMemberNames.StaticConstructorName : WellKnownMemberNames.InstanceConstructorName);
                    case SyntaxKind.DestructorDeclaration:
                        return WellKnownMemberNames.DestructorName;
                    case SyntaxKind.OperatorDeclaration:
                        var operatorDeclaration = (OperatorDeclarationSyntax)baseMethodDeclarationSyntax;
                        return ExplicitInterfaceHelpers.GetMemberName(outerBinder, operatorDeclaration.ExplicitInterfaceSpecifier, OperatorFacts.OperatorNameFromDeclaration(operatorDeclaration));
                    case SyntaxKind.ConversionOperatorDeclaration:
                        var conversionDeclaration = (ConversionOperatorDeclarationSyntax)baseMethodDeclarationSyntax;
                        return ExplicitInterfaceHelpers.GetMemberName(outerBinder, conversionDeclaration.ExplicitInterfaceSpecifier, OperatorFacts.OperatorNameFromDeclaration(conversionDeclaration));
                    case SyntaxKind.MethodDeclaration:
                        MethodDeclarationSyntax methodDeclSyntax = (MethodDeclarationSyntax)baseMethodDeclarationSyntax;
                        return ExplicitInterfaceHelpers.GetMemberName(outerBinder, methodDeclSyntax.ExplicitInterfaceSpecifier, methodDeclSyntax.Identifier.ValueText);
                    default:
                        throw ExceptionUtilities.UnexpectedValue(baseMethodDeclarationSyntax.Kind());
                }
            }

            /// <summary>
            /// Get the name of the property, indexer, or event so that it can be looked up in the containing type.
            /// </summary>
            /// <param name="basePropertyDeclarationSyntax">Non-null declaration syntax.</param>
            /// <param name="outerBinder">Non-null binder for the scope around the member.</param>
            private static string GetPropertyOrEventName(BasePropertyDeclarationSyntax basePropertyDeclarationSyntax, Binder outerBinder)
            {
                ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifierSyntax = basePropertyDeclarationSyntax.ExplicitInterfaceSpecifier;

                switch (basePropertyDeclarationSyntax.Kind())
                {
                    case SyntaxKind.PropertyDeclaration:
                        var propertyDecl = (PropertyDeclarationSyntax)basePropertyDeclarationSyntax;
                        return ExplicitInterfaceHelpers.GetMemberName(outerBinder, explicitInterfaceSpecifierSyntax, propertyDecl.Identifier.ValueText);
                    case SyntaxKind.IndexerDeclaration:
                        return ExplicitInterfaceHelpers.GetMemberName(outerBinder, explicitInterfaceSpecifierSyntax, WellKnownMemberNames.Indexer);
                    case SyntaxKind.EventDeclaration:
                    case SyntaxKind.EventFieldDeclaration:
                        var eventDecl = (EventDeclarationSyntax)basePropertyDeclarationSyntax;
                        return ExplicitInterfaceHelpers.GetMemberName(outerBinder, explicitInterfaceSpecifierSyntax, eventDecl.Identifier.ValueText);
                    default:
                        throw ExceptionUtilities.UnexpectedValue(basePropertyDeclarationSyntax.Kind());
                }
            }

            // Get the correct methods symbol within container that corresponds to the given method syntax.
            private SourceMemberMethodSymbol GetMethodSymbol(BaseMethodDeclarationSyntax baseMethodDeclarationSyntax, Binder outerBinder)
            {
                if (baseMethodDeclarationSyntax == _memberDeclarationOpt)
                {
                    return (SourceMemberMethodSymbol)_memberOpt;
                }

                NamedTypeSymbol container = GetContainerType(outerBinder, baseMethodDeclarationSyntax);
                if ((object)container == null)
                {
                    return null;
                }

                string methodName = GetMethodName(baseMethodDeclarationSyntax, outerBinder);
                return (SourceMemberMethodSymbol)GetMemberSymbol(methodName, baseMethodDeclarationSyntax.FullSpan, container, SymbolKind.Method);
            }

            private SourcePropertySymbol GetPropertySymbol(BasePropertyDeclarationSyntax basePropertyDeclarationSyntax, Binder outerBinder)
            {
                Debug.Assert(basePropertyDeclarationSyntax.Kind() == SyntaxKind.PropertyDeclaration || basePropertyDeclarationSyntax.Kind() == SyntaxKind.IndexerDeclaration);

                if (basePropertyDeclarationSyntax == _memberDeclarationOpt)
                {
                    return (SourcePropertySymbol)_memberOpt;
                }

                NamedTypeSymbol container = GetContainerType(outerBinder, basePropertyDeclarationSyntax);
                if ((object)container == null)
                {
                    return null;
                }

                string propertyName = GetPropertyOrEventName(basePropertyDeclarationSyntax, outerBinder);
                return (SourcePropertySymbol)GetMemberSymbol(propertyName, basePropertyDeclarationSyntax.Span, container, SymbolKind.Property);
            }

            private SourceEventSymbol GetEventSymbol(EventDeclarationSyntax eventDeclarationSyntax, Binder outerBinder)
            {
                if (eventDeclarationSyntax == _memberDeclarationOpt)
                {
                    return (SourceEventSymbol)_memberOpt;
                }

                NamedTypeSymbol container = GetContainerType(outerBinder, eventDeclarationSyntax);
                if ((object)container == null)
                {
                    return null;
                }

                string eventName = GetPropertyOrEventName(eventDeclarationSyntax, outerBinder);
                return (SourceEventSymbol)GetMemberSymbol(eventName, eventDeclarationSyntax.Span, container, SymbolKind.Event);
            }

            private Symbol GetMemberSymbol(string memberName, TextSpan memberSpan, NamedTypeSymbol container, SymbolKind kind)
            {
                Debug.Assert(kind is SymbolKind.Method or SymbolKind.Property or SymbolKind.Event);

                if (container is SourceMemberContainerTypeSymbol { HasPrimaryConstructor: true } sourceMemberContainerTypeSymbol)
                {
                    foreach (Symbol sym in sourceMemberContainerTypeSymbol.GetMembersToMatchAgainstDeclarationSpan())
                    {
                        if (sym.IsAccessor())
                        {
                            continue;
                        }

                        if (sym.Name == memberName && checkSymbol(sym, memberSpan, kind, out Symbol result))
                        {
                            return result;
                        }
                    }
                }
                else
                {
                    foreach (Symbol sym in container.GetMembers(memberName))
                    {
                        if (checkSymbol(sym, memberSpan, kind, out Symbol result))
                        {
                            return result;
                        }
                    }
                }

                return null;

                bool checkSymbol(Symbol sym, TextSpan memberSpan, SymbolKind kind, out Symbol result)
                {
                    result = sym;

                    if (sym.Kind != kind)
                    {
                        return false;
                    }

                    if (sym.Kind == SymbolKind.Method)
                    {
                        if (InSpan(sym.GetFirstLocation(), this.syntaxTree, memberSpan))
                        {
                            return true;
                        }

                        // If this is a partial method, the method represents the defining part,
                        // not the implementation (method.Locations includes both parts). If the
                        // span is in fact in the implementation, return that method instead.
                        var implementation = ((MethodSymbol)sym).PartialImplementationPart;
                        if ((object)implementation != null)
                        {
                            if (InSpan(implementation.GetFirstLocation(), this.syntaxTree, memberSpan))
                            {
                                result = implementation;
                                return true;
                            }
                        }
                    }
                    else if (InSpan(sym.Locations, this.syntaxTree, memberSpan))
                    {
                        return true;
                    }

                    return false;
                }
            }

            /// <summary>
            /// Returns true if the location is within the syntax tree and span.
            /// </summary>
            private static bool InSpan(Location location, SyntaxTree syntaxTree, TextSpan span)
            {
                Debug.Assert(syntaxTree != null);
                return (location.SourceTree == syntaxTree) && span.Contains(location.SourceSpan);
            }

            /// <summary>
            /// Returns true if one of the locations is within the syntax tree and span.
            /// </summary>
            private static bool InSpan(ImmutableArray<Location> locations, SyntaxTree syntaxTree, TextSpan span)
            {
                Debug.Assert(syntaxTree != null);
                foreach (var loc in locations)
                {
                    if (InSpan(loc, syntaxTree, span))
                    {
                        return true;
                    }
                }
                return false;
            }

            public override Binder VisitDelegateDeclaration(DelegateDeclarationSyntax parent)
            {
                if (!LookupPosition.IsInDelegateDeclaration(_position, parent))
                {
                    return VisitCore(parent.Parent);
                }

                var key = CreateBinderCacheKey(parent, usage: NodeUsage.Normal);

                Binder resultBinder;
                if (!binderCache.TryGetValue(key, out resultBinder))
                {
                    Binder outer = VisitCore(parent.Parent); // a binder for the body of the enclosing type or namespace
                    var container = ((NamespaceOrTypeSymbol)outer.ContainingMemberOrLambda).GetSourceTypeMember(parent);

                    // NOTE: Members of the delegate type are in scope in the entire delegate declaration syntax.
                    // NOTE: Hence we can assume that we are in body of the delegate type and explicitly insert the InContainerBinder in the binder chain.
                    resultBinder = new InContainerBinder(container, outer);

                    if (parent.TypeParameterList != null)
                    {
                        resultBinder = new WithClassTypeParametersBinder(container, resultBinder);
                    }

                    resultBinder = resultBinder.WithUnsafeRegionIfNecessary(parent.Modifiers);

                    binderCache.TryAdd(key, resultBinder);
                }

                return resultBinder;
            }

            public override Binder VisitEnumDeclaration(EnumDeclarationSyntax parent)
            {
                // This method has nothing to contribute unless the position is actually inside the enum (i.e. not in the declaration part)
                bool inBody = LookupPosition.IsBetweenTokens(_position, parent.OpenBraceToken, parent.CloseBraceToken) ||
                    LookupPosition.IsInAttributeSpecification(_position, parent.AttributeLists);
                if (!inBody)
                {
                    return VisitCore(parent.Parent);
                }

                var key = CreateBinderCacheKey(parent, usage: NodeUsage.Normal);

                Binder resultBinder;
                if (!binderCache.TryGetValue(key, out resultBinder))
                {
                    Binder outer = VisitCore(parent.Parent); // a binder for the body of the type enclosing this type
                    var container = ((NamespaceOrTypeSymbol)outer.ContainingMemberOrLambda).GetSourceTypeMember(parent.Identifier.ValueText, 0, SyntaxKind.EnumDeclaration, parent);

                    resultBinder = new InContainerBinder(container, outer);

                    resultBinder = resultBinder.WithUnsafeRegionIfNecessary(parent.Modifiers);

                    binderCache.TryAdd(key, resultBinder);
                }

                return resultBinder;
            }

            // PERF: do not override VisitTypeDeclaration,
            //       because C# will not call it and will call least derived one instead
            //       resulting in unnecessary virtual dispatch
            private Binder VisitTypeDeclarationCore(TypeDeclarationSyntax parent)
            {
                if (!LookupPosition.IsInTypeDeclaration(_position, parent))
                {
                    return VisitCore(parent.Parent);
                }

                NodeUsage extraInfo = NodeUsage.Normal;

                // we are visiting type declarations fairly frequently
                // and position is more likely to be in the body, so lets check for "inBody" first.
                if (parent.OpenBraceToken != default &&
                    parent.CloseBraceToken != default &&
                    LookupPosition.IsBetweenTokens(_position, parent.OpenBraceToken, parent.CloseBraceToken))
                {
                    extraInfo = NodeUsage.NamedTypeBodyOrTypeParameters;
                }
                else if (LookupPosition.IsInAttributeSpecification(_position, parent.AttributeLists))
                {
                    extraInfo = NodeUsage.NamedTypeBodyOrTypeParameters;
                }
                else if (LookupPosition.IsInTypeParameterList(_position, parent))
                {
                    extraInfo = NodeUsage.NamedTypeBodyOrTypeParameters;
                }
                else if (LookupPosition.IsBetweenTokens(_position, parent.Keyword, parent.OpenBraceToken))
                {
                    extraInfo = NodeUsage.NamedTypeBaseListOrParameterList;
                }

                return VisitTypeDeclarationCore(parent, extraInfo);
            }

            internal Binder VisitTypeDeclarationCore(TypeDeclarationSyntax parent, NodeUsage extraInfo)
            {
                var key = CreateBinderCacheKey(parent, extraInfo);

                Binder resultBinder;
                if (!binderCache.TryGetValue(key, out resultBinder))
                {
                    // if node is in the optional type parameter list, then members and type parameters are in scope 
                    //     (needed when binding attributes applied to type parameters).
                    // if node is in the base clause, type parameters are in scope.
                    // if node is in the body, then members and type parameters are in scope.

                    // a binder for the body of the type enclosing this type
                    resultBinder = VisitCore(parent.Parent);

                    if (extraInfo != NodeUsage.Normal)
                    {
                        var typeSymbol = ((NamespaceOrTypeSymbol)resultBinder.ContainingMemberOrLambda).GetSourceTypeMember(parent);

                        if (extraInfo == NodeUsage.NamedTypeBaseListOrParameterList)
                        {
                            // even though there could be no type parameter, we need this binder 
                            // for its "IsAccessible"
                            resultBinder = new WithClassTypeParametersBinder(typeSymbol, resultBinder);
                        }
                        else
                        {
                            resultBinder = new WithPrimaryConstructorParametersBinder(typeSymbol, resultBinder);

                            resultBinder = new InContainerBinder(typeSymbol, resultBinder);

                            if (parent.TypeParameterList != null)
                            {
                                resultBinder = new WithClassTypeParametersBinder(typeSymbol, resultBinder);
                            }
                        }
                    }

                    resultBinder = resultBinder.WithUnsafeRegionIfNecessary(parent.Modifiers);

                    binderCache.TryAdd(key, resultBinder);
                }

                return resultBinder;
            }

            public override Binder VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                return VisitTypeDeclarationCore(node);
            }

            public override Binder VisitStructDeclaration(StructDeclarationSyntax node)
            {
                return VisitTypeDeclarationCore(node);
            }

            public override Binder VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
            {
                return VisitTypeDeclarationCore(node);
            }

            public override Binder VisitRecordDeclaration(RecordDeclarationSyntax node)
                => VisitTypeDeclarationCore(node);

            public sealed override Binder VisitNamespaceDeclaration(NamespaceDeclarationSyntax parent)
            {
                if (!LookupPosition.IsInNamespaceDeclaration(_position, parent))
                {
                    return VisitCore(parent.Parent);
                }

                // test for position equality in case the open brace token is missing:
                // namespace X class C { }
                bool inBody = LookupPosition.IsBetweenTokens(_position, parent.OpenBraceToken, parent.CloseBraceToken);

                bool inUsing = IsInUsing(parent);

                return VisitNamespaceDeclaration(parent, _position, inBody, inUsing);
            }

            public override Binder VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax parent)
            {
                if (!LookupPosition.IsInNamespaceDeclaration(_position, parent))
                {
                    return VisitCore(parent.Parent);
                }

                // Anywhere after the `;` is in the 'body' of this namespace.
                bool inBody = _position >= parent.SemicolonToken.EndPosition;

                bool inUsing = IsInUsing(parent);

                return VisitNamespaceDeclaration(parent, _position, inBody, inUsing);
            }

            internal Binder VisitNamespaceDeclaration(BaseNamespaceDeclarationSyntax parent, int position, bool inBody, bool inUsing)
            {
                Debug.Assert(!inUsing || inBody, "inUsing => inBody");

                var extraInfo = inUsing ? NodeUsage.NamespaceUsings : (inBody ? NodeUsage.NamespaceBody : NodeUsage.Normal);  // extra info for the cache.
                var key = CreateBinderCacheKey(parent, extraInfo);

                Binder result;
                if (!binderCache.TryGetValue(key, out result))
                {
                    Binder outer;
                    var container = parent.Parent;

                    if (InScript && container.Kind() == SyntaxKind.CompilationUnit)
                    {
                        // Although namespaces are not allowed in script code we still bind them so that we don't report useless errors.
                        // A namespace in script code is not bound within the scope of a Script class, 
                        // but still within scope of compilation unit extern aliases and usings.
                        outer = VisitCompilationUnit((CompilationUnitSyntax)container, inUsing: false, inScript: false);
                    }
                    else
                    {
                        outer = _factory.GetBinder(parent.Parent, position);
                    }

                    if (!inBody)
                    {
                        // not between the curlies
                        result = outer;
                    }
                    else
                    {
                        // if between the curlies, members are in scope
                        result = MakeNamespaceBinder(parent, parent.Name, outer, inUsing);
                    }

                    binderCache.TryAdd(key, result);
                }

                return result;
            }

            private static Binder MakeNamespaceBinder(CSharpSyntaxNode node, NameSyntax name, Binder outer, bool inUsing)
            {
                if (name is QualifiedNameSyntax dotted)
                {
                    outer = MakeNamespaceBinder(dotted.Left, dotted.Left, outer, inUsing: false);
                    name = dotted.Right;
                    Debug.Assert(name is not QualifiedNameSyntax);
                }

                NamespaceOrTypeSymbol container;

                if (outer is InContainerBinder inContainerBinder)
                {
                    container = inContainerBinder.Container;
                }
                else
                {
                    Debug.Assert(outer is SimpleProgramUnitBinder);
                    container = outer.Compilation.GlobalNamespace;
                }

                NamespaceSymbol ns = ((NamespaceSymbol)container).GetNestedNamespace(name);
                if ((object)ns == null) return outer;

                if (node is BaseNamespaceDeclarationSyntax namespaceDecl)
                {
                    outer = AddInImportsBinders((SourceNamespaceSymbol)outer.Compilation.SourceModule.GetModuleNamespace(ns), namespaceDecl, outer, inUsing);
                }
                else
                {
                    Debug.Assert(!inUsing);
                }

                return new InContainerBinder(ns, outer);
            }

            public override Binder VisitCompilationUnit(CompilationUnitSyntax parent)
            {
                return VisitCompilationUnit(
                    parent,
                    inUsing: IsInUsing(parent),
                    inScript: InScript);
            }

            internal Binder VisitCompilationUnit(CompilationUnitSyntax compilationUnit, bool inUsing, bool inScript)
            {
                if (compilationUnit != syntaxTree.GetRoot())
                {
                    throw new ArgumentOutOfRangeException(nameof(compilationUnit), "node not part of tree");
                }

                var extraInfo = inUsing
                    ? (inScript ? NodeUsage.CompilationUnitScriptUsings : NodeUsage.CompilationUnitUsings)
                    : (inScript ? NodeUsage.CompilationUnitScript : NodeUsage.Normal);  // extra info for the cache.
                var key = CreateBinderCacheKey(compilationUnit, extraInfo);

                Binder result;
                if (!binderCache.TryGetValue(key, out result))
                {
                    result = this.buckStopsHereBinder;

                    if (inScript)
                    {
                        Debug.Assert((object)compilation.ScriptClass != null);

                        //
                        // Binder chain in script/interactive code:
                        //
                        // + global imports
                        //   + current and previous submission imports (except using aliases)
                        //     + global namespace
                        //       + host object members
                        //         + previous submissions and corresponding using aliases
                        //           + script class members and using aliases
                        //

                        bool isSubmissionTree = compilation.IsSubmissionSyntaxTree(compilationUnit.SyntaxTree);
                        var scriptClass = compilation.ScriptClass;
                        bool isSubmissionClass = scriptClass.IsSubmissionClass;

                        if (!inUsing)
                        {
                            result = WithUsingNamespacesAndTypesBinder.Create(compilation.GlobalImports, result, withImportChainEntry: true);

                            if (isSubmissionClass)
                            {
                                // NB: Only the non-alias imports are
                                // ever consumed.  Aliases are actually checked in InSubmissionClassBinder (below).
                                // Note: #loaded trees don't consume previous submission imports.
                                result = WithUsingNamespacesAndTypesBinder.Create((SourceNamespaceSymbol)compilation.SourceModule.GlobalNamespace, compilationUnit, result,
                                                                                  withPreviousSubmissionImports: compilation.PreviousSubmission != null && isSubmissionTree,
                                                                                  withImportChainEntry: true);
                            }
                        }

                        result = new InContainerBinder(compilation.GlobalNamespace, result);

                        if (compilation.HostObjectType != null)
                        {
                            result = new HostObjectModelBinder(result);
                        }

                        if (isSubmissionClass)
                        {
                            result = new InSubmissionClassBinder(scriptClass, result, compilationUnit, inUsing);
                        }
                        else
                        {
                            result = AddInImportsBinders((SourceNamespaceSymbol)compilation.SourceModule.GlobalNamespace, compilationUnit, result, inUsing);
                            result = new InContainerBinder(scriptClass, result);
                        }
                    }
                    else
                    {
                        //
                        // Binder chain in regular code:
                        //
                        // + compilation unit imported namespaces and types
                        //   + compilation unit extern and using aliases
                        //     + global namespace
                        // 
                        var globalNamespace = compilation.GlobalNamespace;
                        result = AddInImportsBinders((SourceNamespaceSymbol)compilation.SourceModule.GlobalNamespace, compilationUnit, result, inUsing);
                        result = new InContainerBinder(globalNamespace, result);

                        if (!inUsing &&
                            SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(compilation, compilationUnit, fallbackToMainEntryPoint: true) is SynthesizedSimpleProgramEntryPointSymbol simpleProgram)
                        {
                            ExecutableCodeBinder bodyBinder = simpleProgram.GetBodyBinder(_factory._ignoreAccessibility);
                            result = new SimpleProgramUnitBinder(result, (SimpleProgramBinder)bodyBinder.GetBinder(simpleProgram.SyntaxNode));
                        }
                    }

                    binderCache.TryAdd(key, result);
                }

                return result;
            }

            private static Binder AddInImportsBinders(SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax, Binder next, bool inUsing)
            {
                Debug.Assert(declarationSyntax.Kind() is SyntaxKind.CompilationUnit or SyntaxKind.NamespaceDeclaration or SyntaxKind.FileScopedNamespaceDeclaration);

                if (inUsing)
                {
                    // Extern aliases are in scope
                    return WithExternAliasesBinder.Create(declaringSymbol, declarationSyntax, next);
                }
                else
                {
                    // All imports are in scope
                    return WithExternAndUsingAliasesBinder.Create(declaringSymbol, declarationSyntax, WithUsingNamespacesAndTypesBinder.Create(declaringSymbol, declarationSyntax, next));
                }
            }

            internal static BinderCacheKey CreateBinderCacheKey(CSharpSyntaxNode node, NodeUsage usage)
            {
                Debug.Assert(BitArithmeticUtilities.CountBits((uint)usage) <= 1, "Not a flags enum.");
                return new BinderCacheKey(node, usage);
            }

            /// <summary>
            /// Returns true if containingNode has a child that contains the specified position
            /// and has kind UsingDirective.
            /// </summary>
            /// <remarks>
            /// Usings can't see other usings, so this is extra info when looking at a namespace
            /// or compilation unit scope.
            /// </remarks>
            private bool IsInUsing(CSharpSyntaxNode containingNode)
            {
                TextSpan containingSpan = containingNode.Span;

                SyntaxToken token;
                if (containingNode.Kind() != SyntaxKind.CompilationUnit && _position == containingSpan.End)
                {
                    // This occurs at EOF
                    token = containingNode.GetLastToken();
                    Debug.Assert(token == this.syntaxTree.GetRoot().GetLastToken());
                }
                else if (_position < containingSpan.Start || _position > containingSpan.End) //NB: > not >=
                {
                    return false;
                }
                else
                {
                    token = containingNode.FindToken(_position);
                }

                var node = token.Parent;
                while (node != null && node != containingNode)
                {
                    // ACASEY: the restriction that we're only interested in children
                    // of containingNode (vs descendants) seems to be required for cases like
                    // GetSemanticInfoTests.BindAliasQualifier, which binds an alias name
                    // within a using directive.
                    if (node.IsKind(SyntaxKind.UsingDirective) && node.Parent == containingNode)
                    {
                        return true;
                    }

                    node = node.Parent;
                }
                return false;
            }

            public override Binder VisitDocumentationCommentTrivia(DocumentationCommentTriviaSyntax parent)
            {
                // Need to step across the structured trivia boundary explicitly - can't just follow Parent references.
                return VisitCore(parent.ParentTrivia.Token.Parent);
            }

            /// <remarks>
            /// Used to detect whether we are in a cref parameter type.
            /// </remarks>
            public override Binder VisitCrefParameter(CrefParameterSyntax parent)
            {
                XmlCrefAttributeSyntax containingAttribute = parent.FirstAncestorOrSelf<XmlCrefAttributeSyntax>(ascendOutOfTrivia: false);
                return VisitXmlCrefAttributeInternal(containingAttribute, NodeUsage.CrefParameterOrReturnType);
            }

            /// <remarks>
            /// Used to detect whether we are in a cref return type.
            /// </remarks>
            public override Binder VisitConversionOperatorMemberCref(ConversionOperatorMemberCrefSyntax parent)
            {
                if (parent.Type.Span.Contains(_position))
                {
                    XmlCrefAttributeSyntax containingAttribute = parent.FirstAncestorOrSelf<XmlCrefAttributeSyntax>(ascendOutOfTrivia: false);
                    return VisitXmlCrefAttributeInternal(containingAttribute, NodeUsage.CrefParameterOrReturnType);
                }

                return base.VisitConversionOperatorMemberCref(parent);
            }

            public override Binder VisitXmlCrefAttribute(XmlCrefAttributeSyntax parent)
            {
                if (!LookupPosition.IsInXmlAttributeValue(_position, parent))
                {
                    return VisitCore(parent.Parent);
                }

                var extraInfo = NodeUsage.Normal;  // extra info for the cache.
                return VisitXmlCrefAttributeInternal(parent, extraInfo);
            }

            private Binder VisitXmlCrefAttributeInternal(XmlCrefAttributeSyntax parent, NodeUsage extraInfo)
            {
                Debug.Assert(extraInfo == NodeUsage.Normal || extraInfo == NodeUsage.CrefParameterOrReturnType,
                    "Unexpected extraInfo " + extraInfo);

                var key = CreateBinderCacheKey(parent, extraInfo);

                Binder result;
                if (!binderCache.TryGetValue(key, out result))
                {
                    CrefSyntax crefSyntax = parent.Cref;
                    MemberDeclarationSyntax memberSyntax = GetAssociatedMemberForXmlSyntax(parent);

                    bool inParameterOrReturnType = extraInfo == NodeUsage.CrefParameterOrReturnType;

                    result = (object)memberSyntax == null
                        ? MakeCrefBinderInternal(crefSyntax, VisitCore(parent.Parent), inParameterOrReturnType)
                        : MakeCrefBinder(crefSyntax, memberSyntax, _factory, inParameterOrReturnType);

                    binderCache.TryAdd(key, result);
                }

                return result;
            }

            public override Binder VisitXmlNameAttribute(XmlNameAttributeSyntax parent)
            {
                if (!LookupPosition.IsInXmlAttributeValue(_position, parent))
                {
                    return VisitCore(parent.Parent);
                }

                XmlNameAttributeElementKind elementKind = parent.GetElementKind();

                NodeUsage extraInfo;
                switch (elementKind)
                {
                    case XmlNameAttributeElementKind.Parameter:
                    case XmlNameAttributeElementKind.ParameterReference:
                        extraInfo = NodeUsage.DocumentationCommentParameter;
                        break;
                    case XmlNameAttributeElementKind.TypeParameter:
                        extraInfo = NodeUsage.DocumentationCommentTypeParameter;
                        break;
                    case XmlNameAttributeElementKind.TypeParameterReference:
                        extraInfo = NodeUsage.DocumentationCommentTypeParameterReference;
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(elementKind);
                }

                // Cleverness: rather than using this node as the key, we're going to use the
                // enclosing doc comment, because all name attributes with the same element
                // kind, in the same doc comment can share the same binder.
                var key = CreateBinderCacheKey(GetEnclosingDocumentationComment(parent), extraInfo);

                Binder result;
                if (!binderCache.TryGetValue(key, out result))
                {
                    result = this.buckStopsHereBinder;

                    Binder outerBinder = VisitCore(GetEnclosingDocumentationComment(parent));
                    if ((object)outerBinder != null)
                    {
                        // The rest of the doc comment is going to report something for containing symbol -
                        // that shouldn't change just because we're in a name attribute.
                        result = result.WithContainingMemberOrLambda(outerBinder.ContainingMemberOrLambda);
                    }

                    MemberDeclarationSyntax memberSyntax = GetAssociatedMemberForXmlSyntax(parent);
                    if ((object)memberSyntax != null)
                    {
                        switch (elementKind)
                        {
                            case XmlNameAttributeElementKind.Parameter:
                            case XmlNameAttributeElementKind.ParameterReference:
                                result = GetParameterNameAttributeValueBinder(memberSyntax, result);
                                break;
                            case XmlNameAttributeElementKind.TypeParameter:
                                result = GetTypeParameterNameAttributeValueBinder(memberSyntax, includeContainingSymbols: false, nextBinder: result);
                                break;
                            case XmlNameAttributeElementKind.TypeParameterReference:
                                result = GetTypeParameterNameAttributeValueBinder(memberSyntax, includeContainingSymbols: true, nextBinder: result);
                                break;
                        }
                    }

                    binderCache.TryAdd(key, result);
                }

                return result;
            }

            /// <summary>
            /// We're in a &lt;param&gt; or &lt;paramref&gt; element, so we want a binder that can see
            /// the parameters of the associated member and nothing else.
            /// </summary>
            private Binder GetParameterNameAttributeValueBinder(MemberDeclarationSyntax memberSyntax, Binder nextBinder)
            {
                if (memberSyntax is BaseMethodDeclarationSyntax { ParameterList: { ParameterCount: > 0 } } baseMethodDeclSyntax)
                {
                    Binder outerBinder = VisitCore(memberSyntax.Parent);
                    MethodSymbol method = GetMethodSymbol(baseMethodDeclSyntax, outerBinder);
                    return new WithParametersBinder(method.Parameters, nextBinder);
                }

                if (memberSyntax is TypeDeclarationSyntax { ParameterList: { ParameterCount: > 0 } } typeDeclaration)
                {
                    _ = typeDeclaration.ParameterList;
                    Binder outerBinder = VisitCore(memberSyntax);
                    SourceNamedTypeSymbol type = ((NamespaceOrTypeSymbol)outerBinder.ContainingMemberOrLambda).GetSourceTypeMember((TypeDeclarationSyntax)memberSyntax);
                    var primaryConstructor = type.PrimaryConstructor;

                    if (primaryConstructor.SyntaxRef.SyntaxTree == memberSyntax.SyntaxTree &&
                        primaryConstructor.GetSyntax() == memberSyntax)
                    {
                        return new WithParametersBinder(primaryConstructor.Parameters, nextBinder);
                    }
                }

                // As in Dev11, we do not allow <param name="value"> on events.
                SyntaxKind memberKind = memberSyntax.Kind();
                if (memberKind == SyntaxKind.PropertyDeclaration || memberKind == SyntaxKind.IndexerDeclaration)
                {
                    Binder outerBinder = VisitCore(memberSyntax.Parent);

                    BasePropertyDeclarationSyntax propertyDeclSyntax = (BasePropertyDeclarationSyntax)memberSyntax;
                    PropertySymbol property = GetPropertySymbol(propertyDeclSyntax, outerBinder);

                    ImmutableArray<ParameterSymbol> parameters = property.Parameters;

                    // BREAK: Dev11 also allows "value" for readonly properties, but that doesn't
                    // make sense and we don't have a symbol.
                    if ((object)property.SetMethod != null)
                    {
                        Debug.Assert(property.SetMethod.ParameterCount > 0);
                        parameters = parameters.Add(property.SetMethod.Parameters.Last());
                    }

                    if (parameters.Any())
                    {
                        return new WithParametersBinder(parameters, nextBinder);
                    }
                }
                else if (memberKind == SyntaxKind.DelegateDeclaration)
                {
                    Binder outerBinder = VisitCore(memberSyntax.Parent);
                    SourceNamedTypeSymbol delegateType = ((NamespaceOrTypeSymbol)outerBinder.ContainingMemberOrLambda).GetSourceTypeMember((DelegateDeclarationSyntax)memberSyntax);
                    Debug.Assert((object)delegateType != null);
                    MethodSymbol invokeMethod = delegateType.DelegateInvokeMethod;
                    Debug.Assert((object)invokeMethod != null);
                    ImmutableArray<ParameterSymbol> parameters = invokeMethod.Parameters;
                    if (parameters.Any())
                    {
                        return new WithParametersBinder(parameters, nextBinder);
                    }
                }

                return nextBinder;
            }

            /// <summary>
            /// We're in a &lt;typeparam&gt; or &lt;typeparamref&gt; element, so we want a binder that can see
            /// the type parameters of the associated member and nothing else.
            /// </summary>
            private Binder GetTypeParameterNameAttributeValueBinder(MemberDeclarationSyntax memberSyntax, bool includeContainingSymbols, Binder nextBinder)
            {
                if (includeContainingSymbols)
                {
                    Binder outerBinder = VisitCore(memberSyntax.Parent);
                    for (NamedTypeSymbol curr = outerBinder.ContainingType; (object)curr != null; curr = curr.ContainingType)
                    {
                        if (curr.Arity > 0)
                        {
                            nextBinder = new WithClassTypeParametersBinder(curr, nextBinder);
                        }
                    }
                }

                // NOTE: don't care about enums, since they don't have type parameters.
                TypeDeclarationSyntax typeDeclSyntax = memberSyntax as TypeDeclarationSyntax;
                if ((object)typeDeclSyntax != null && typeDeclSyntax.Arity > 0)
                {
                    Binder outerBinder = VisitCore(memberSyntax.Parent);
                    SourceNamedTypeSymbol typeSymbol = ((NamespaceOrTypeSymbol)outerBinder.ContainingMemberOrLambda).GetSourceTypeMember(typeDeclSyntax);

                    // NOTE: don't include anything else in the binder chain.
                    return new WithClassTypeParametersBinder(typeSymbol, nextBinder);
                }

                if (memberSyntax.Kind() == SyntaxKind.MethodDeclaration)
                {
                    MethodDeclarationSyntax methodDeclSyntax = (MethodDeclarationSyntax)memberSyntax;
                    if (methodDeclSyntax.Arity > 0)
                    {
                        Binder outerBinder = VisitCore(memberSyntax.Parent);
                        MethodSymbol method = GetMethodSymbol(methodDeclSyntax, outerBinder);
                        return new WithMethodTypeParametersBinder(method, nextBinder);
                    }
                }
                else if (memberSyntax.Kind() == SyntaxKind.DelegateDeclaration)
                {
                    Binder outerBinder = VisitCore(memberSyntax.Parent);
                    SourceNamedTypeSymbol delegateType = ((NamespaceOrTypeSymbol)outerBinder.ContainingMemberOrLambda).GetSourceTypeMember((DelegateDeclarationSyntax)memberSyntax);
                    ImmutableArray<TypeParameterSymbol> typeParameters = delegateType.TypeParameters;
                    if (typeParameters.Any())
                    {
                        return new WithClassTypeParametersBinder(delegateType, nextBinder);
                    }
                }

                return nextBinder;
            }
        }

        #region In outer type - BinderFactory

        /// <summary>
        /// Given a CrefSyntax and an associated member declaration syntax node,
        /// construct an appropriate binder for binding the cref.
        /// </summary>
        /// <param name="crefSyntax">Cref that will be bound.</param>
        /// <param name="memberSyntax">The member to which the documentation comment (logically) containing
        /// the cref syntax applies.</param>
        /// <param name="factory">Corresponding binder factory.</param>
        /// <param name="inParameterOrReturnType">True to get a special binder for cref parameter and return types.</param>
        /// <remarks>
        /// The CrefSyntax does not actually have to be within the documentation comment on the member - it
        /// could be included from another file.
        /// </remarks>
        internal static Binder MakeCrefBinder(CrefSyntax crefSyntax, MemberDeclarationSyntax memberSyntax, BinderFactory factory, bool inParameterOrReturnType = false)
        {
            Debug.Assert(crefSyntax != null);
            Debug.Assert(memberSyntax != null);

            Binder binder = memberSyntax is BaseTypeDeclarationSyntax typeDeclSyntax
                ? getBinder(typeDeclSyntax)
                : factory.GetBinder(memberSyntax);

            return MakeCrefBinderInternal(crefSyntax, binder, inParameterOrReturnType);

            Binder getBinder(BaseTypeDeclarationSyntax baseTypeDeclaration)
            {
                if (baseTypeDeclaration is TypeDeclarationSyntax { SemicolonToken: { RawKind: (int)SyntaxKind.SemicolonToken }, OpenBraceToken: { RawKind: (int)SyntaxKind.None } } noBlockBodyTypeDeclarationWithSemicolon)
                {
                    return factory.GetInTypeBodyBinder(noBlockBodyTypeDeclarationWithSemicolon);
                }

                return factory.GetBinder(baseTypeDeclaration, baseTypeDeclaration.OpenBraceToken.SpanStart);
            }
        }

        /// <summary>
        /// Internal version of MakeCrefBinder that allows the caller to explicitly set the underlying binder.
        /// </summary>
        private static Binder MakeCrefBinderInternal(CrefSyntax crefSyntax, Binder binder, bool inParameterOrReturnType)
        {
            // After much deliberation, we eventually decided to suppress lookup of inherited members within
            // crefs, in order to match dev11's behavior (Changeset #829014).  Unfortunately, it turns out
            // that dev11 does not suppress these members when performing lookup within parameter and return
            // types, within crefs (DevDiv #586815, #598371).
            // NOTE: always allow pointer types.
            BinderFlags flags = BinderFlags.Cref | BinderFlags.SuppressConstraintChecks | BinderFlags.UnsafeRegion;
            if (inParameterOrReturnType)
            {
                flags |= BinderFlags.CrefParameterOrReturnType;
            }

            binder = binder.WithAdditionalFlags(flags);
            binder = new WithCrefTypeParametersBinder(crefSyntax, binder);
            return binder;
        }

        internal static MemberDeclarationSyntax GetAssociatedMemberForXmlSyntax(CSharpSyntaxNode xmlSyntax)
        {
            Debug.Assert(xmlSyntax is XmlAttributeSyntax || xmlSyntax.Kind() == SyntaxKind.XmlEmptyElement || xmlSyntax.Kind() == SyntaxKind.XmlElementStartTag);

            StructuredTriviaSyntax structuredTrivia = GetEnclosingDocumentationComment(xmlSyntax);
            SyntaxTrivia containingTrivia = structuredTrivia.ParentTrivia;
            SyntaxToken associatedToken = containingTrivia.Token;

            CSharpSyntaxNode curr = (CSharpSyntaxNode)associatedToken.Parent;
            while (curr != null)
            {
                MemberDeclarationSyntax memberSyntax = curr as MemberDeclarationSyntax;
                if (memberSyntax != null)
                {
                    // The doc comment must be in the leading trivia of its associated member.
                    if (!memberSyntax.GetLeadingTrivia().Contains(containingTrivia))
                    {
                        return null;
                    }

                    return memberSyntax;
                }
                curr = curr.Parent;
            }

            return null;
        }

        /// <summary>
        /// Walk up from an XML syntax node (attribute or tag) to the enclosing documentation comment trivia.
        /// </summary>
        private static DocumentationCommentTriviaSyntax GetEnclosingDocumentationComment(CSharpSyntaxNode xmlSyntax)
        {
            CSharpSyntaxNode curr = xmlSyntax;
            for (; !SyntaxFacts.IsDocumentationCommentTrivia(curr.Kind()); curr = curr.Parent)
            {
            }
            Debug.Assert(curr != null);

            return (DocumentationCommentTriviaSyntax)curr;
        }

        #endregion
    }
}
