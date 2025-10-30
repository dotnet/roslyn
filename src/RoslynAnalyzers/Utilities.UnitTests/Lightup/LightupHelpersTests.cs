// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using Analyzer.Utilities.Lightup;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Analyzer.Utilities.UnitTests.Lightup
{
    public class LightupHelpersTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData(typeof(SyntaxNode))]
        public void TestCanAccessNonExistentSyntaxProperty(Type? type)
        {
            var fallbackResult = new object();

            var propertyAccessor = LightupHelpers.CreateSyntaxPropertyAccessor<SyntaxNode, object>(type, "NonExistentProperty", fallbackResult);
            Assert.NotNull(propertyAccessor);
            Assert.Same(fallbackResult, propertyAccessor(SyntaxFactory.AccessorList()));
            Assert.Throws<NullReferenceException>(() => propertyAccessor(null!));

            var withPropertyAccessor = LightupHelpers.CreateSyntaxWithPropertyAccessor<SyntaxNode, object>(type, "NonExistentProperty", fallbackResult);
            Assert.NotNull(withPropertyAccessor);
            Assert.NotNull(withPropertyAccessor(SyntaxFactory.AccessorList(), fallbackResult));
            Assert.ThrowsAny<NotSupportedException>(() => withPropertyAccessor(SyntaxFactory.AccessorList(), new object()));
            Assert.Throws<NullReferenceException>(() => withPropertyAccessor(null!, new object()));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(typeof(EmptySymbol))]
        public void TestCanAccessNonExistentSymbolProperty(Type? type)
        {
            var fallbackResult = new object();

            var propertyAccessor = LightupHelpers.CreateSymbolPropertyAccessor<ISymbol, object>(type, "NonExistentProperty", fallbackResult);
            Assert.NotNull(propertyAccessor);
            Assert.Same(fallbackResult, propertyAccessor(new EmptySymbol()));
            Assert.Throws<NullReferenceException>(() => propertyAccessor(null!));

            var withPropertyAccessor = LightupHelpers.CreateSymbolWithPropertyAccessor<ISymbol, object>(type, "NonExistentProperty", fallbackResult);
            Assert.NotNull(withPropertyAccessor);
            Assert.NotNull(withPropertyAccessor(new EmptySymbol(), fallbackResult));
            Assert.ThrowsAny<NotSupportedException>(() => withPropertyAccessor(new EmptySymbol(), new object()));
            Assert.Throws<NullReferenceException>(() => withPropertyAccessor(null!, new object()));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(typeof(SyntaxNode))]
        public void TestCanAccessNonExistentMethodWithArgument(Type? type)
        {
            var fallbackResult = new object();

            var accessor = LightupHelpers.CreateAccessorWithArgument<SyntaxNode, int, object?>(type, "parameterName", typeof(int), "argumentName", "NonExistentMethod", fallbackResult);
            Assert.NotNull(accessor);
            Assert.Same(fallbackResult, accessor(SyntaxFactory.AccessorList(), 0));
            Assert.Throws<NullReferenceException>(() => accessor(null!, 0));
        }

        [Fact]
        public void TestCreateSyntaxPropertyAccessor()
        {
            // The call *should* have been made with the first generic argument set to `BaseMethodDeclarationSyntax`
            // instead of `MethodDeclarationSyntax`.
            Assert.ThrowsAny<InvalidOperationException>(() => LightupHelpers.CreateSyntaxPropertyAccessor<MethodDeclarationSyntax, BlockSyntax?>(typeof(BaseMethodDeclarationSyntax), nameof(BaseMethodDeclarationSyntax.Body), fallbackResult: null));

            // The call *should* have been made with the second generic argument set to `ArrowExpressionClauseSyntax`
            // instead of `BlockSyntax`.
            Assert.ThrowsAny<InvalidOperationException>(() => LightupHelpers.CreateSyntaxPropertyAccessor<MethodDeclarationSyntax, BlockSyntax?>(typeof(MethodDeclarationSyntax), nameof(MethodDeclarationSyntax.ExpressionBody), fallbackResult: null));
        }

        [Fact]
        public void TestCreateSyntaxWithPropertyAccessor()
        {
            // The call *should* have been made with the first generic argument set to `BaseMethodDeclarationSyntax`
            // instead of `MethodDeclarationSyntax`.
            Assert.ThrowsAny<InvalidOperationException>(() => LightupHelpers.CreateSyntaxWithPropertyAccessor<MethodDeclarationSyntax, BlockSyntax?>(typeof(BaseMethodDeclarationSyntax), nameof(BaseMethodDeclarationSyntax.Body), fallbackResult: null));

            // The call *should* have been made with the second generic argument set to `ArrowExpressionClauseSyntax`
            // instead of `BlockSyntax`.
            Assert.ThrowsAny<InvalidOperationException>(() => LightupHelpers.CreateSyntaxWithPropertyAccessor<MethodDeclarationSyntax, BlockSyntax?>(typeof(MethodDeclarationSyntax), nameof(MethodDeclarationSyntax.ExpressionBody), fallbackResult: null));
        }

        [SuppressMessage("MicrosoftCodeAnalysisCompatibility", "RS1009:Only internal implementations of this interface are allowed.", Justification = "Stub for testing.")]
        private sealed class EmptySymbol : ISymbol
        {
            SymbolKind ISymbol.Kind => throw new NotImplementedException();
            string ISymbol.Language => throw new NotImplementedException();
            string ISymbol.Name => throw new NotImplementedException();
            string ISymbol.MetadataName => throw new NotImplementedException();
            ISymbol ISymbol.ContainingSymbol => throw new NotImplementedException();
            IAssemblySymbol ISymbol.ContainingAssembly => throw new NotImplementedException();
            IModuleSymbol ISymbol.ContainingModule => throw new NotImplementedException();
            INamedTypeSymbol ISymbol.ContainingType => throw new NotImplementedException();
            INamespaceSymbol ISymbol.ContainingNamespace => throw new NotImplementedException();
            bool ISymbol.IsDefinition => throw new NotImplementedException();
            bool ISymbol.IsStatic => throw new NotImplementedException();
            bool ISymbol.IsVirtual => throw new NotImplementedException();
            bool ISymbol.IsOverride => throw new NotImplementedException();
            bool ISymbol.IsAbstract => throw new NotImplementedException();
            bool ISymbol.IsSealed => throw new NotImplementedException();
            bool ISymbol.IsExtern => throw new NotImplementedException();
            bool ISymbol.IsImplicitlyDeclared => throw new NotImplementedException();
            bool ISymbol.CanBeReferencedByName => throw new NotImplementedException();
            ImmutableArray<Location> ISymbol.Locations => throw new NotImplementedException();
            ImmutableArray<SyntaxReference> ISymbol.DeclaringSyntaxReferences => throw new NotImplementedException();
            Accessibility ISymbol.DeclaredAccessibility => throw new NotImplementedException();
            ISymbol ISymbol.OriginalDefinition => throw new NotImplementedException();
            bool ISymbol.HasUnsupportedMetadata => throw new NotImplementedException();

            int ISymbol.MetadataToken => throw new NotImplementedException();

            void ISymbol.Accept(SymbolVisitor visitor)
                => throw new NotImplementedException();

            TResult ISymbol.Accept<TResult>(SymbolVisitor<TResult> visitor)
                => throw new NotImplementedException();

            TResult ISymbol.Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument)
                => throw new NotImplementedException();

            public bool Equals(ISymbol? other)
                => throw new NotImplementedException();

            ImmutableArray<AttributeData> ISymbol.GetAttributes()
                => throw new NotImplementedException();

            string ISymbol.GetDocumentationCommentId()
                => throw new NotImplementedException();

            string ISymbol.GetDocumentationCommentXml(CultureInfo? preferredCulture, bool expandIncludes, CancellationToken cancellationToken)
                => throw new NotImplementedException();

            ImmutableArray<SymbolDisplayPart> ISymbol.ToDisplayParts(SymbolDisplayFormat? format)
                => throw new NotImplementedException();

            string ISymbol.ToDisplayString(SymbolDisplayFormat? format)
                => throw new NotImplementedException();

            ImmutableArray<SymbolDisplayPart> ISymbol.ToMinimalDisplayParts(SemanticModel semanticModel, int position, SymbolDisplayFormat? format)
                => throw new NotImplementedException();

            string ISymbol.ToMinimalDisplayString(SemanticModel semanticModel, int position, SymbolDisplayFormat? format)
                => throw new NotImplementedException();

            public bool Equals(ISymbol? other, SymbolEqualityComparer equalityComparer)
                => throw new NotImplementedException();
        }
    }
}
