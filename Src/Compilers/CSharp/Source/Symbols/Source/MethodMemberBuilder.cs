
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Roslyn.Compilers.Internal;
using Roslyn.Compilers.Collections;

namespace Roslyn.Compilers.CSharp
{
    internal class MethodMemberBuilder : MemberBuilder
    {
        private readonly MemberDeclarationSyntax syntax;

        // Until we've merged the parts of a partial method, we can't create the type parameter symbols
        // because we need the (merged) method symbol to be set as its parent.  But we can't merge the
        // partials until we've analyzed the types appearing in the signature.  But the signature contains
        // references to the type parameters.  We break this cycle by creating signature type parameters,
        // and binding the signature using them.  We merge partial
        // methods based on the signature.  When we finally create the method symbol, we
        // substitute the method's actual type parameters for the signature ones, thereby fixing up
        // proper type parameter lists, return types, and parameter types.  The method builder
        // only deals with the signature.
        private readonly TypeSymbol declaredReturnType;
        private readonly List<TypeSymbol> declaredParameterTypes;
        private readonly MethodSignature signature;
        private readonly TypeSymbol explicitInterfaceType;

        internal MethodMemberBuilder(NamedTypeSymbol container, Binder enclosing, MethodDeclarationSyntax syntax, DiagnosticBag diagnostics)
            : this(container, enclosing, (MemberDeclarationSyntax)syntax, diagnostics)
        {
        }

        internal MethodMemberBuilder(NamedTypeSymbol container, Binder enclosing, ConstructorDeclarationSyntax syntax, DiagnosticBag diagnostics)
            : this(container, enclosing, (MemberDeclarationSyntax)syntax, diagnostics)
        {
        }

        private MethodMemberBuilder(NamedTypeSymbol container, Binder enclosing, MemberDeclarationSyntax syntax, DiagnosticBag diagnostics)
            : base(enclosing.Location(syntax) as SourceLocation, container, enclosing)
        {
            Debug.Assert(syntax != null);
            this.syntax = syntax;

            // Make a binder context in which each type parameter binds to a corresponding numbered type parameter
            Binder parametersContext = Enclosing;
            if (syntax.Kind == SyntaxKind.MethodDeclaration)
            {
                var methodSyntax = syntax as MethodDeclarationSyntax;
                int arity = methodSyntax.Arity;
                if (arity != 0)
                {
                    var typeParamMap = new MultiDictionary<string, TypeParameterSymbol>();
                    var typeParams = methodSyntax.TypeParameterListOpt.Parameters;

                    for (int iParam = 0; iParam < typeParams.Count; iParam++)
                    {
                        var arg = typeParams[iParam];
                        var symbol = IndexedTypeParameterSymbol.GetTypeParameter(iParam);
                        typeParamMap.Add(arg.Identifier.ValueText, symbol);
                    }

                    parametersContext = new WithDummyTypeParametersBinder(typeParamMap, Enclosing);
                }

                if (methodSyntax.ExplicitInterfaceSpecifierOpt != null)
                {
                    this.explicitInterfaceType = enclosing.BindType(methodSyntax.ExplicitInterfaceSpecifierOpt.Name, diagnostics);
                }
            }

            // TODOngafter 1: recast this code using ReadOnlyArray.
            IEnumerable<ParameterSyntax> parameters = SyntaxParameters.HasValue ? SyntaxParameters.Value : SpecializedCollections.EmptyEnumerable<ParameterSyntax>();
            declaredParameterTypes = parameters.Select(p =>
            {
                if (p.TypeOpt == null)
                    return new CSErrorTypeSymbol(enclosing.Compilation.GlobalNamespace, "ErrorType", 0, diagnostics.Add(ErrorCode.ERR_NotYetImplementedInRoslyn, new SourceLocation(Tree, p)));
                return parametersContext.BindType(p.TypeOpt, diagnostics);
            }).ToList();

            var parameterRefs = parameters.Select(p => p.Modifiers.GetRefKind()).ToList();

            switch (syntax.Kind)
            {
                case SyntaxKind.ConstructorDeclaration:
                    Binder original = parametersContext; // TODOngafter 1: worry about diagnostic reporting and suppression here.
                    declaredReturnType = Enclosing.GetSpecialType(SpecialType.System_Void, diagnostics, syntax);
                    break;
                default:
                    declaredReturnType = parametersContext.BindType(SyntaxReturnType, diagnostics);
                    break;
            }

            TypeSymbol explType = null;
            var explSyntax = ExplicitInterface;
            if (explSyntax != null)
            {
                explType = parametersContext.BindType(explSyntax, diagnostics);
            }

            // TODOngafter 3: map dynamic->object for the signature
            this.signature = new MethodSignature(Name, SyntaxArity, declaredParameterTypes, parameterRefs, explType);
        }

        internal override MemberDeclarationSyntax Syntax
        {
            get
            {
                return syntax;
            }
        }

        internal override SyntaxTree Tree
        {
            get
            {
                return Enclosing.SourceTree;
            }
        }

        internal SeparatedSyntaxList<ParameterSyntax>? SyntaxParameters
        {
            get
            {
                switch (syntax.Kind)
                {
                    case SyntaxKind.MethodDeclaration:
                        return (Syntax as MethodDeclarationSyntax).ParameterList.Parameters;
                    case SyntaxKind.ConstructorDeclaration:
                        return (Syntax as ConstructorDeclarationSyntax).ParameterList.Parameters;
                    default:
                        return null;
                }
            }
        }

        internal MethodKind Kind
        {
            get
            {
                switch (syntax.Kind)
                {
                    case SyntaxKind.MethodDeclaration:
                        return (syntax as MethodDeclarationSyntax).ExplicitInterfaceSpecifierOpt == null ? MethodKind.Ordinary : MethodKind.ExplicitInterfaceImplementation;
                    case SyntaxKind.ConstructorDeclaration:
                        return SyntaxModifiers.Any(TokenIsStatic) ? MethodKind.StaticConstructor : MethodKind.Constructor;
                    default:
                        return MethodKind.Ordinary; // TODO: implement the other method kinds.
                }
            }
        }
        private static bool TokenIsStatic(SyntaxToken token)
        {
            return token.Kind == SyntaxKind.StaticKeyword;
        }

        public NameSyntax ExplicitInterface
        {
            get
            {
                switch (Syntax.Kind)
                {
                    case SyntaxKind.MethodDeclaration:
                    {
                        var expl = (Syntax as MethodDeclarationSyntax).ExplicitInterfaceSpecifierOpt;
                        return expl == null ? null : expl.Name;
                    }
                    case SyntaxKind.ConstructorDeclaration:
                    default:
                        return null;
                }
            }
        }

        internal override SyntaxTokenList SyntaxModifiers
        {
            get
            {
                switch (syntax.Kind)
                {
                    case SyntaxKind.MethodDeclaration:
                        return (Syntax as MethodDeclarationSyntax).Modifiers;
                    case SyntaxKind.ConstructorDeclaration:
                        return (Syntax as ConstructorDeclarationSyntax).Modifiers;
                    default:
                        return default(SyntaxTokenList);
                }
            }
        } 

        internal TypeSyntax SyntaxReturnType
        {
            get
            {
                switch (syntax.Kind)
                {
                    case SyntaxKind.MethodDeclaration:
                        return (Syntax as MethodDeclarationSyntax).ReturnType;
                    case SyntaxKind.ConstructorDeclaration:
                        return null;
                    default:
                        return null;
                }
            }
        }

        internal string Name
        {
            get
            {
                switch (syntax.Kind)
                {
                    case SyntaxKind.MethodDeclaration:
                        var md = (MethodDeclarationSyntax)syntax;
                        if (this.explicitInterfaceType != null)
                        {
                            return this.explicitInterfaceType.GetFullName() + "." + md.Identifier.ValueText;
                        }
                        else
                        {
                            return md.Identifier.ValueText;
                        }

                    case SyntaxKind.ConstructorDeclaration:
                        return SyntaxModifiers.Any(SyntaxKind.StaticKeyword)
                            ? MethodSymbol.StaticConstructorName :
                            MethodSymbol.InstanceConstructorName;

                    default:
                        // TODO(cyrusn): What about operators, destructors and the rest?
                        return null;
                }
            }
        }

        internal int SyntaxArity
        {
            get
            {
                switch (syntax.Kind)
                {
                    case SyntaxKind.MethodDeclaration:
                        return (Syntax as MethodDeclarationSyntax).Arity;
                    case SyntaxKind.ConstructorDeclaration:
                        return 0;
                    default:
                        return 0;
                }
            }
        }

        internal IEnumerable<TypeParameterSyntax> SyntaxTypeParameters
        {
            get
            {
                switch (syntax.Kind)
                {
                    case SyntaxKind.MethodDeclaration:
                        var method = Syntax as MethodDeclarationSyntax;
                        return method.TypeParameterListOpt == null
                            ? SpecializedCollections.EmptyEnumerable<TypeParameterSyntax>()
                            : method.TypeParameterListOpt.Parameters;
                    case SyntaxKind.ConstructorDeclaration:
                        return SpecializedCollections.EmptyEnumerable<TypeParameterSyntax>();
                    default:
                        return SpecializedCollections.EmptyEnumerable<TypeParameterSyntax>();
                }
            }
        }

        internal MethodSignature Signature
        {
            get
            {
                return signature;
            }
        }

        internal NamedTypeSymbol ContainingType
        {
            get
            {
                return (NamedTypeSymbol)Accessor;
            }
        }

        internal override Symbol MakeSymbol(Symbol parent, IEnumerable<MemberBuilder> contexts, DiagnosticBag diagnostics)
        {
            return new SourceMethodSymbol(
                ContainingType, Name, contexts.OfType<MethodMemberBuilder>().AsReadOnly(), diagnostics);
        }

        internal override Location NameLocation
        {
            get
            {
                switch (syntax.Kind)
                {
                    case SyntaxKind.MethodDeclaration:
                        {
                            var meth = (MethodDeclarationSyntax)syntax;
                            return meth.ExplicitInterfaceSpecifierOpt == null ? Location(meth.Identifier) : Location(meth.ExplicitInterfaceSpecifierOpt);
                        }
                    case SyntaxKind.ConstructorDeclaration:
                        return Location(((ConstructorDeclarationSyntax)syntax).Identifier);
                    default:
                        return Location(syntax);
                }
            }
        }

        internal override object Key()
        {
            return Signature;
        }

        IEnumerable<TypeParameterBuilder> typeParameterBuidlers;
        internal IEnumerable<TypeParameterBuilder> TypeParameterBuilders(MethodSymbol current)
        {
            if (typeParameterBuidlers == null)
            {
                Interlocked.CompareExchange(ref typeParameterBuidlers, MakeTypeParameterBuilders(current).ToList(), null);
            }
            return typeParameterBuidlers;
        }
        private IEnumerable<TypeParameterBuilder> MakeTypeParameterBuilders(MethodSymbol current)
        {
            if (SyntaxArity == 0)
            {
                return SpecializedCollections.EmptyEnumerable<TypeParameterBuilder>();
            }

            var withParams = new WithMethodTypeParametersBinder(current, Next);
            return
                SyntaxTypeParameters
                .Select(ta => new TypeParameterBuilder(ta, current, withParams.Location(ta)));
        }

        internal TypeSymbol ReturnType(MethodSymbol method)
        {
            return new TypeMap(
                IndexedTypeParameterSymbol.Take(SyntaxArity).AsReadOnly<TypeSymbol>(),
                method.TypeParameters.Cast<TypeParameterSymbol, TypeSymbol>()).SubstituteType(declaredReturnType);
        }

        internal ReadOnlyArray<TypeSymbol> ArgumentTypes(MethodSymbol method)
        {
            return new TypeMap(
                IndexedTypeParameterSymbol.Take(SyntaxArity).AsReadOnly<TypeSymbol>(),
                method.TypeParameters.Cast<TypeParameterSymbol, TypeSymbol>()).SubstituteTypes(declaredParameterTypes);
        }
    }
}
