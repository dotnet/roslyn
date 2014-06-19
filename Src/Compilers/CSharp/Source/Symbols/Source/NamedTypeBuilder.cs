using System;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Compilers.Collections;

namespace Roslyn.Compilers.CSharp
{
    internal class NamedTypeBuilder : NamespaceOrTypeBuilder
    {
        internal readonly SingleDeclaration Declaration;

        internal Location NameLocation
        {
            get
            {
                var syntaxNode = Declaration.Syntax.GetSyntax();

                if (syntaxNode is TypeDeclarationSyntax)
                {
                    return Location(((TypeDeclarationSyntax)syntaxNode).Identifier);
                }
                else if (syntaxNode is EnumDeclarationSyntax)
                {
                    return Location(((EnumDeclarationSyntax)syntaxNode).Identifier);
                }
                else if (syntaxNode is DelegateDeclarationSyntax)
                {
                    return Location(((DelegateDeclarationSyntax)syntaxNode).Identifier);
                }

                return null;
            }
        }

        internal override MemberDeclarationSyntax Syntax
        {
            get
            {
                var syntaxNode = Declaration.Syntax.GetSyntax();

                if (syntaxNode is TypeDeclarationSyntax)
                {
                    return (TypeDeclarationSyntax)syntaxNode;
                }
                else if (syntaxNode is EnumDeclarationSyntax)
                {
                    return (EnumDeclarationSyntax)syntaxNode;
                }
                else if (syntaxNode is DelegateDeclarationSyntax)
                {
                    return (DelegateDeclarationSyntax)syntaxNode;
                }

                return null;
            }
        }

        internal override IEnumerable<SyntaxToken> SyntaxModifiers
        {
            get
            {
                var syntaxNode = Declaration.Syntax.GetSyntax();

                if (syntaxNode is TypeDeclarationSyntax)
                {
                    return ((TypeDeclarationSyntax)syntaxNode).Modifiers;
                }
                else if (syntaxNode is EnumDeclarationSyntax)
                {
                    return ((EnumDeclarationSyntax)syntaxNode).Modifiers;
                }
                else if (syntaxNode is DelegateDeclarationSyntax)
                {
                    return ((DelegateDeclarationSyntax)syntaxNode).Modifiers;
                }

                return SpecializedCollections.EmptyEnumerable<SyntaxToken>();
            }
        }

        internal NamedTypeBuilder(Symbol accessor, SingleDeclaration declaration, BinderContext enclosing)
            : base(declaration.Location, accessor, enclosing)
        {
            this.Declaration = declaration;
        }

        internal override IEnumerable<NamespaceOrTypeBuilder> TypeOrNamespaceBuilders(NamespaceOrTypeSymbol current)
        {
            BinderContext bodyContext = BodyContext(current);
            foreach (var d in Declaration.SingleChildren)
            {
                yield return new NamedTypeBuilder(current, d, bodyContext);
            }
        }

        internal IEnumerable<TypeParameterBuilder> TypeParameterBinders(NamespaceOrTypeSymbol current)
        {
            var type = current as NamedTypeSymbol;
            if (type == null)
            {
                return Enumerable.Empty<TypeParameterBuilder>();
            }

            var baseBinder = BaseContext(type) as WithClassTypeParametersBinderContext;
            if (baseBinder == null)
            {
                return Enumerable.Empty<TypeParameterBuilder>(); // no params in declaration
            }

            return
                baseBinder.TypeParameters
                .Select(ta => new TypeParameterBuilder(ta, type, baseBinder));
        }

        internal BinderContext BaseContext(NamespaceOrTypeSymbol current)
        {
            var typeContainer = current as NamedTypeSymbol;
            if (typeContainer == null)
            {
                return Next;
            }

            var decl = Declaration.Syntax.GetSyntax() as TypeDeclarationSyntax;
            if (decl == null)
            {
                return Next;
            }

            return new WithClassTypeParametersBinderContext(Next.Location(Declaration.Syntax.GetSyntax()), Declaration.Syntax, typeContainer, Next);
        }

        private BinderContext BodyContext(NamespaceOrTypeSymbol current)
        {
            return new InContainerBinderContext(Declaration, current, BaseContext(current));
        }

        private class MemberBinderVisitor : SyntaxVisitor<object, IEnumerable<MemberBuilder>>
        {
            private readonly NamedTypeSymbol container;
            private readonly BinderContext context;

            internal MemberBinderVisitor(NamedTypeSymbol container, BinderContext context)
            {
                this.container = container;
                this.context = context;
            }

            internal IEnumerable<MemberBuilder> BinderForMember(MemberDeclarationSyntax syntax)
            {
                return this.Visit(syntax, null) ?? Enumerable.Empty<MemberBuilder>();
            }

            protected override IEnumerable<MemberBuilder> DefaultVisit(SyntaxNode node, object unused)
            {
                context.ReportDiagnostic(node, ErrorCode.ERR_UnimplementedOp);
                return Enumerable.Empty<MemberBuilder>();
            }

            public override IEnumerable<MemberBuilder> VisitFieldDeclaration(FieldDeclarationSyntax node, object unused)
            {
                TypeSymbol type = context.BindType(node.Type, context);
                return node.Variables.Select(d => new FieldMemberBuilder(container, context, node, type, d));
            }

            public override IEnumerable<MemberBuilder> VisitMethodDeclaration(MethodDeclarationSyntax node, object unused)
            {
                return ConsList.Singleton(new MethodMemberBuilder(container, context, node));
            }

            public override IEnumerable<MemberBuilder> VisitTypeDeclaration(TypeDeclarationSyntax node, object unused) { return Enumerable.Empty<MemberBuilder>(); } // type declarations are handled by ContainerDeclaration et al.
            public override IEnumerable<MemberBuilder> VisitEnumDeclaration(EnumDeclarationSyntax node, object unused) { return Enumerable.Empty<MemberBuilder>(); } // type declarations are handled by ContainerDeclaration et al.
            public override IEnumerable<MemberBuilder> VisitDelegateDeclaration(DelegateDeclarationSyntax node, object unused) { return Enumerable.Empty<MemberBuilder>(); } // type declarations are handled by ContainerDeclaration et al.

            // public override IEnumerable<MemberBinderContext> VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node, object arg)
            // public override IEnumerable<MemberBinderContext> VisitEventFieldDeclaration(EventFieldDeclarationSyntax node, object unused)
            // public override IEnumerable<MemberBinderContext> VisitOperatorDeclaration(OperatorDeclarationSyntax node, object unused)
            // public override IEnumerable<MemberBinderContext> VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node, object unused)

            public override IEnumerable<MemberBuilder> VisitConstructorDeclaration(ConstructorDeclarationSyntax node, object unused)
            {
                return ConsList.Singleton(new MethodMemberBuilder(container, context, node));
            }

            // public override IEnumerable<MemberBinderContext> VisitConstructorInitializer(ConstructorInitializerSyntax node, object unused)
            // public override IEnumerable<MemberBinderContext> VisitDestructorDeclaration(DestructorDeclarationSyntax node, object unused)
            // public override IEnumerable<MemberBinderContext> VisitPropertyDeclaration(PropertyDeclarationSyntax node, object unused)
            // public override IEnumerable<MemberBinderContext> VisitEventDeclaration(EventDeclarationSyntax node, object unused)
            // public override IEnumerable<MemberBinderContext> VisitIndexerDeclaration(IndexerDeclarationSyntax node, object unused)
            public override IEnumerable<MemberBuilder> VisitIncompleteMember(IncompleteMemberSyntax node, object unused) { return Enumerable.Empty<MemberBuilder>(); }
        }

        internal override IEnumerable<MemberBuilder> NonContainerBuilders(NamespaceOrTypeSymbol current)
        {
            SyntaxNode syntax = Declaration.Syntax.GetSyntax();
            switch (syntax.Kind)
            {
                case SyntaxKind.EnumDeclaration:
                case SyntaxKind.DelegateDeclaration:
                    ReportDiagnostic(syntax, ErrorCode.ERR_UnimplementedOp);
                    return Enumerable.Empty<MemberBuilder>();
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.StructDeclaration:
                    var visitor = new MemberBinderVisitor(current as NamedTypeSymbol, BodyContext(current));
                    TypeDeclarationSyntax typeDecl = syntax as TypeDeclarationSyntax;
                    return typeDecl.Members.SelectMany(member => visitor.BinderForMember(member)).Where(b => b != null);
                default:
                    throw new NotSupportedException();
            }
        }

        internal override object Key()
        {
            // TODO: should declaration.kind be part of the key?  It depends on how we want to diagnose
            // "partial struct Foo {} partial class Foo {}"
            return Declaration.Name + "<" + Declaration.Arity + ">";
        }

        internal override Symbol MakeSymbol(Symbol parent, IEnumerable<MemberBuilder> contexts)
        {
            return new SourceNamedTypeSymbol((NamespaceOrTypeSymbol)parent, contexts.Select(c => (NamedTypeBuilder)c));
        }
    }
}
