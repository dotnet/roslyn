// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
{
    public class ISymbolExtensionsTests : TestBase
    {
        [Fact]
        public void GetGlyphGroupTests()
        {
            TestGlyph(
                StandardGlyphGroup.GlyphAssembly,
                SymbolKind.Assembly);
            TestGlyph(
                StandardGlyphGroup.GlyphAssembly,
                SymbolKind.NetModule);
            TestGlyph(
                StandardGlyphGroup.GlyphGroupNamespace,
                SymbolKind.Namespace);
            TestGlyph(
                StandardGlyphGroup.GlyphGroupType,
                SymbolKind.TypeParameter);
            TestGlyph(
                StandardGlyphGroup.GlyphGroupClass,
                SymbolKind.DynamicType);

            TestGlyph(
                StandardGlyphGroup.GlyphExtensionMethodPrivate,
                SymbolKind.Method,
                Accessibility.Private);
            TestGlyph(
                StandardGlyphGroup.GlyphExtensionMethodPrivate,
                SymbolKind.Method,
                Accessibility.Private,
                isExtensionMethod: false,
                methodKind: MethodKind.ReducedExtension);
            TestGlyph(
                StandardGlyphGroup.GlyphExtensionMethodProtected,
                declaredAccessibility: Accessibility.ProtectedAndInternal);
            TestGlyph(
                StandardGlyphGroup.GlyphExtensionMethodProtected,
                declaredAccessibility: Accessibility.Protected);
            TestGlyph(
                StandardGlyphGroup.GlyphExtensionMethodProtected,
                declaredAccessibility: Accessibility.ProtectedOrInternal);
            TestGlyph(
                StandardGlyphGroup.GlyphExtensionMethodInternal,
                declaredAccessibility: Accessibility.Internal);
            TestGlyph(
                StandardGlyphGroup.GlyphExtensionMethod,
                declaredAccessibility: Accessibility.Public);
            TestGlyph(
                StandardGlyphGroup.GlyphGroupMethod,
                declaredAccessibility: Accessibility.Public,
                isExtensionMethod: false);

            TestGlyph(
                StandardGlyphGroup.GlyphGroupClass,
                SymbolKind.PointerType,
                pointedAtType: (INamedTypeSymbol)CreateSymbolMock(SymbolKind.NamedType, typeKind: TypeKind.Class));

            TestGlyph(
                StandardGlyphGroup.GlyphGroupProperty,
                SymbolKind.Property);

            TestGlyph(
                StandardGlyphGroup.GlyphGroupEnumMember,
                SymbolKind.Field,
                containingType: (INamedTypeSymbol)CreateSymbolMock(SymbolKind.NamedType, typeKind: TypeKind.Enum));

            TestGlyph(
                StandardGlyphGroup.GlyphGroupConstant,
                SymbolKind.Field,
                isConst: true);

            TestGlyph(
                StandardGlyphGroup.GlyphGroupField,
                SymbolKind.Field);

            TestGlyph(StandardGlyphGroup.GlyphGroupVariable, SymbolKind.Parameter);
            TestGlyph(StandardGlyphGroup.GlyphGroupVariable, SymbolKind.Local);
            TestGlyph(StandardGlyphGroup.GlyphGroupVariable, SymbolKind.RangeVariable);
            TestGlyph(StandardGlyphGroup.GlyphGroupIntrinsic, SymbolKind.Label);
            TestGlyph(StandardGlyphGroup.GlyphGroupEvent, SymbolKind.Event);

            TestGlyph(
                StandardGlyphGroup.GlyphGroupClass,
                SymbolKind.ArrayType,
                elementType: (INamedTypeSymbol)CreateSymbolMock(SymbolKind.NamedType, typeKind: TypeKind.Class));

            TestGlyph(
                StandardGlyphGroup.GlyphGroupClass,
                SymbolKind.Alias,
                target: (INamedTypeSymbol)CreateSymbolMock(SymbolKind.NamedType, typeKind: TypeKind.Class));

            Assert.ThrowsAny<ArgumentException>(() =>
                TestGlyph(
                    StandardGlyphGroup.GlyphGroupClass,
                    (SymbolKind)1000));

            TestGlyph(
                StandardGlyphGroup.GlyphGroupClass,
                SymbolKind.NamedType,
                typeKind: TypeKind.Class);
            TestGlyph(
                StandardGlyphGroup.GlyphGroupDelegate,
                SymbolKind.NamedType,
                typeKind: TypeKind.Delegate);
            TestGlyph(
                StandardGlyphGroup.GlyphGroupEnum,
                SymbolKind.NamedType,
                typeKind: TypeKind.Enum);
            TestGlyph(
                StandardGlyphGroup.GlyphGroupModule,
                SymbolKind.NamedType,
                typeKind: TypeKind.Module);
            TestGlyph(
                StandardGlyphGroup.GlyphGroupInterface,
                SymbolKind.NamedType,
                typeKind: TypeKind.Interface);
            TestGlyph(
                StandardGlyphGroup.GlyphGroupStruct,
                SymbolKind.NamedType,
                typeKind: TypeKind.Struct);
            TestGlyph(
                StandardGlyphGroup.GlyphGroupError,
                SymbolKind.NamedType,
                typeKind: TypeKind.Error);

            Assert.ThrowsAny<Exception>(() =>
                TestGlyph(
                    StandardGlyphGroup.GlyphGroupClass,
                    SymbolKind.NamedType,
                    typeKind: TypeKind.Unknown));
        }

        [Fact, WorkItem(545015)]
        public void TestRegularOperatorGlyph()
        {
            TestGlyph(
                StandardGlyphGroup.GlyphGroupOperator,
                SymbolKind.Method,
                methodKind: MethodKind.UserDefinedOperator);
        }

        [Fact, WorkItem(545015)]
        public void TestConversionOperatorGlyph()
        {
            TestGlyph(
                StandardGlyphGroup.GlyphGroupOperator,
                SymbolKind.Method,
                methodKind: MethodKind.Conversion);
        }

        [Fact]
        public void TestWithEventsMemberGlyph()
        {
            TestGlyph(
                StandardGlyphGroup.GlyphGroupField,
                SymbolKind.Property,
                isWithEvents: true);
        }

        private void TestGlyph(
            StandardGlyphGroup expectedGlyphGroup,
            SymbolKind kind = SymbolKind.Method,
            Accessibility declaredAccessibility = Accessibility.NotApplicable,
            bool isExtensionMethod = true,
            MethodKind methodKind = MethodKind.Ordinary,
            INamedTypeSymbol containingType = null,
            bool isConst = false,
            ITypeSymbol elementType = null,
            INamespaceOrTypeSymbol target = null,
            ITypeSymbol pointedAtType = null,
            bool isWithEvents = false,
            TypeKind typeKind = TypeKind.Unknown)
        {
            var symbol = CreateSymbolMock(kind, declaredAccessibility, isExtensionMethod, methodKind, containingType, isConst, elementType, target, pointedAtType, isWithEvents, typeKind);
            Assert.Equal(expectedGlyphGroup, symbol.GetGlyph().GetStandardGlyphGroup());
        }

        private static ISymbol CreateSymbolMock(
            SymbolKind kind,
            Accessibility declaredAccessibility = Accessibility.NotApplicable,
            bool isExtensionMethod = false,
            MethodKind methodKind = MethodKind.Ordinary,
            INamedTypeSymbol containingType = null,
            bool isConst = false,
            ITypeSymbol elementType = null,
            INamespaceOrTypeSymbol target = null,
            ITypeSymbol pointedAtType = null,
            bool isWithEvents = false,
            TypeKind typeKind = TypeKind.Unknown)
        {
            var symbolMock = new Mock<ISymbol>();

            symbolMock.SetupGet(s => s.Kind).Returns(kind);
            symbolMock.SetupGet(s => s.DeclaredAccessibility).Returns(declaredAccessibility);
            symbolMock.SetupGet(s => s.ContainingType).Returns(containingType);

            if (kind == SymbolKind.ArrayType)
            {
                var arrayTypeMock = symbolMock.As<IArrayTypeSymbol>();
                arrayTypeMock.SetupGet(s => s.ElementType).Returns(elementType);
            }

            if (kind == SymbolKind.Alias)
            {
                var aliasMock = symbolMock.As<IAliasSymbol>();
                aliasMock.SetupGet(s => s.Target).Returns(target);
            }

            if (kind == SymbolKind.Method)
            {
                var methodTypeMock = symbolMock.As<IMethodSymbol>();
                methodTypeMock.SetupGet(s => s.MethodKind).Returns(methodKind);
                methodTypeMock.SetupGet(s => s.IsExtensionMethod).Returns(isExtensionMethod);
            }

            if (kind == SymbolKind.NamedType)
            {
                var namedTypeMock = symbolMock.As<INamedTypeSymbol>();
                namedTypeMock.SetupGet(s => s.TypeKind).Returns(typeKind);
            }

            if (kind == SymbolKind.Field)
            {
                var fieldMock = symbolMock.As<IFieldSymbol>();
                fieldMock.SetupGet(s => s.IsConst).Returns(isConst);
            }

            if (kind == SymbolKind.PointerType)
            {
                var pointerTypeMock = symbolMock.As<IPointerTypeSymbol>();
                pointerTypeMock.SetupGet(s => s.PointedAtType).Returns(pointedAtType);
            }

            if (kind == SymbolKind.Property)
            {
                var propertyMock = symbolMock.As<IPropertySymbol>();
                propertyMock.SetupGet(s => s.IsWithEvents).Returns(isWithEvents);
            }

            return symbolMock.Object;
        }
    }
}
