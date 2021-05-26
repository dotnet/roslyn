// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers;
using static Microsoft.CodeAnalysis.CSharp.CodeGeneration.CSharpCodeGenerationHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal static class NamedTypeGenerator
    {
        public static TypeDeclarationSyntax AddNamedTypeTo(
            ICodeGenerationService service,
            TypeDeclarationSyntax destination,
            INamedTypeSymbol namedType,
            CodeGenerationOptions options,
            IList<bool> availableIndices,
            CancellationToken cancellationToken)
        {
            var declaration = GenerateNamedTypeDeclaration(service, namedType, GetDestination(destination), options, cancellationToken);
            var members = Insert(destination.Members, declaration, options, availableIndices);

            return AddMembersTo(destination, members);
        }

        public static NamespaceDeclarationSyntax AddNamedTypeTo(
            ICodeGenerationService service,
            NamespaceDeclarationSyntax destination,
            INamedTypeSymbol namedType,
            CodeGenerationOptions options,
            IList<bool> availableIndices,
            CancellationToken cancellationToken)
        {
            var declaration = GenerateNamedTypeDeclaration(service, namedType, CodeGenerationDestination.Namespace, options, cancellationToken);
            var members = Insert(destination.Members, declaration, options, availableIndices);
            return ConditionallyAddFormattingAnnotationTo(
                destination.WithMembers(members),
                members);
        }

        public static CompilationUnitSyntax AddNamedTypeTo(
            ICodeGenerationService service,
            CompilationUnitSyntax destination,
            INamedTypeSymbol namedType,
            CodeGenerationOptions options,
            IList<bool> availableIndices,
            CancellationToken cancellationToken)
        {
            var declaration = GenerateNamedTypeDeclaration(service, namedType, CodeGenerationDestination.CompilationUnit, options, cancellationToken);
            var members = Insert(destination.Members, declaration, options, availableIndices);
            return destination.WithMembers(members);
        }

        public static MemberDeclarationSyntax GenerateNamedTypeDeclaration(
            ICodeGenerationService service,
            INamedTypeSymbol namedType,
            CodeGenerationDestination destination,
            CodeGenerationOptions options,
            CancellationToken cancellationToken)
        {
            options ??= CodeGenerationOptions.Default;

            var declaration = GetDeclarationSyntaxWithoutMembers(namedType, destination, options);

            // If we are generating members then make sure to exclude properties that cannot be generated.
            // Reason: Calling AddProperty on a propertysymbol that can't be generated (like one with params) causes
            // the getter and setter to get generated instead. Since the list of members is going to include
            // the method symbols for the getter and setter, we don't want to generate them twice.

            var members = GetMembers(namedType).Where(s => s.Kind != SymbolKind.Property || PropertyGenerator.CanBeGenerated((IPropertySymbol)s))
                                               .ToImmutableArray();
            if (namedType.IsRecord)
            {
                declaration = GenerateRecordMembers(service, options, (RecordDeclarationSyntax)declaration, members, cancellationToken);
            }
            else
            {
                // If we're generating a ComImport type, then do not attempt to do any
                // reordering of members.
                if (namedType.IsComImport)
                    options = options.With(autoInsertionLocation: false, sortMembers: false);

                if (options.GenerateMembers && namedType.TypeKind != TypeKind.Delegate)
                    declaration = service.AddMembers(declaration, members, options, cancellationToken);
            }

            return AddFormatterAndCodeGeneratorAnnotationsTo(ConditionallyAddDocumentationCommentTo(declaration, namedType, options, cancellationToken));
        }

        private static RecordDeclarationSyntax GenerateRecordMembers(
            ICodeGenerationService service,
            CodeGenerationOptions options,
            RecordDeclarationSyntax recordDeclaration,
            ImmutableArray<ISymbol> members,
            CancellationToken cancellationToken)
        {
            if (!options.GenerateMembers)
                members = ImmutableArray<ISymbol>.Empty;

            // For a record, add record parameters if we have a primary constructor.
            var primaryConstructor = members.OfType<IMethodSymbol>().FirstOrDefault(m => CodeGenerationConstructorInfo.GetIsPrimaryConstructor(m));
            if (primaryConstructor != null)
            {
                var parameterList = ParameterGenerator.GenerateParameterList(primaryConstructor.Parameters, isExplicit: false, options);
                recordDeclaration = recordDeclaration.WithParameterList(parameterList);

                // remove the primary constructor from the list of members to generate.
                members = members.Remove(primaryConstructor);

                // remove any fields/properties that were created by the primary constructor
                members = members.WhereAsArray(m => m is not IPropertySymbol and not IFieldSymbol || !primaryConstructor.Parameters.Any(p => p.Name == m.Name));
            }

            // remove any implicit overrides to generate.
            members = members.WhereAsArray(m => !m.IsImplicitlyDeclared);

            // If there are no members, just make a simple record with no body
            if (members.Length == 0)
                return recordDeclaration.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            // Otherwise, give the record a body and add the members to it.
            recordDeclaration = recordDeclaration.WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken))
                                                 .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken))
                                                 .WithSemicolonToken(default);
            return service.AddMembers(recordDeclaration, members, options, cancellationToken);
        }

        public static MemberDeclarationSyntax UpdateNamedTypeDeclaration(
            ICodeGenerationService service,
            MemberDeclarationSyntax declaration,
            IList<ISymbol> newMembers,
            CodeGenerationOptions options,
            CancellationToken cancellationToken)
        {
            declaration = RemoveAllMembers(declaration);
            declaration = service.AddMembers(declaration, newMembers, options, cancellationToken);
            return AddFormatterAndCodeGeneratorAnnotationsTo(declaration);
        }

        private static MemberDeclarationSyntax GetDeclarationSyntaxWithoutMembers(
            INamedTypeSymbol namedType,
            CodeGenerationDestination destination,
            CodeGenerationOptions options)
        {
            var reusableDeclarationSyntax = GetReuseableSyntaxNodeForSymbol<MemberDeclarationSyntax>(namedType, options);
            return reusableDeclarationSyntax == null
                ? GetDeclarationSyntaxWithoutMembersWorker(namedType, destination, options)
                : RemoveAllMembers(reusableDeclarationSyntax);
        }

        private static MemberDeclarationSyntax RemoveAllMembers(MemberDeclarationSyntax declaration)
        {
            switch (declaration.Kind())
            {
                case SyntaxKind.EnumDeclaration:
                    return ((EnumDeclarationSyntax)declaration).WithMembers(default);

                case SyntaxKind.StructDeclaration:
                case SyntaxKind.RecordStructDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.RecordDeclaration:
                    return ((TypeDeclarationSyntax)declaration).WithMembers(default);

                default:
                    return declaration;
            }
        }

        private static MemberDeclarationSyntax GetDeclarationSyntaxWithoutMembersWorker(
            INamedTypeSymbol namedType,
            CodeGenerationDestination destination,
            CodeGenerationOptions options)
        {
            if (namedType.TypeKind == TypeKind.Enum)
            {
                return GenerateEnumDeclaration(namedType, destination, options);
            }
            else if (namedType.TypeKind == TypeKind.Delegate)
            {
                return GenerateDelegateDeclaration(namedType, destination, options);
            }

            TypeDeclarationSyntax typeDeclaration;
            if (namedType.IsRecord)
            {
                var isRecordClass = namedType.TypeKind is TypeKind.Class;
                var declarationKind = isRecordClass ? SyntaxKind.RecordDeclaration : SyntaxKind.RecordStructDeclaration;
                var classOrStructKeyword = SyntaxFactory.Token(isRecordClass ? default : SyntaxKind.StructKeyword);

                typeDeclaration = SyntaxFactory.RecordDeclaration(kind: declarationKind, attributeLists: default, modifiers: default,
                    SyntaxFactory.Token(SyntaxKind.RecordKeyword), classOrStructKeyword, namedType.Name.ToIdentifierToken(),
                    typeParameterList: null, parameterList: null, baseList: null, constraintClauses: default, openBraceToken: default, members: default, closeBraceToken: default,
                    SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            }
            else
            {
                var kind = namedType.TypeKind == TypeKind.Struct ? SyntaxKind.StructDeclaration :
                           namedType.TypeKind == TypeKind.Interface ? SyntaxKind.InterfaceDeclaration : SyntaxKind.ClassDeclaration;

                typeDeclaration = SyntaxFactory.TypeDeclaration(kind, namedType.Name.ToIdentifierToken());
            }

            var result = typeDeclaration
                .WithAttributeLists(GenerateAttributeDeclarations(namedType, options))
                .WithModifiers(GenerateModifiers(namedType, destination, options))
                .WithTypeParameterList(GenerateTypeParameterList(namedType, options))
                .WithBaseList(GenerateBaseList(namedType))
                .WithConstraintClauses(GenerateConstraintClauses(namedType));

            return result;
        }

        private static DelegateDeclarationSyntax GenerateDelegateDeclaration(
            INamedTypeSymbol namedType,
            CodeGenerationDestination destination,
            CodeGenerationOptions options)
        {
            var invokeMethod = namedType.DelegateInvokeMethod;
            Contract.ThrowIfNull(invokeMethod);

            return SyntaxFactory.DelegateDeclaration(
                GenerateAttributeDeclarations(namedType, options),
                GenerateModifiers(namedType, destination, options),
                invokeMethod.ReturnType.GenerateTypeSyntax(),
                namedType.Name.ToIdentifierToken(),
                TypeParameterGenerator.GenerateTypeParameterList(namedType.TypeParameters, options),
                ParameterGenerator.GenerateParameterList(invokeMethod.Parameters, isExplicit: false, options: options),
                namedType.TypeParameters.GenerateConstraintClauses());
        }

        private static EnumDeclarationSyntax GenerateEnumDeclaration(
            INamedTypeSymbol namedType,
            CodeGenerationDestination destination,
            CodeGenerationOptions options)
        {
            var baseList = namedType.EnumUnderlyingType != null && namedType.EnumUnderlyingType.SpecialType != SpecialType.System_Int32
                ? SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(SyntaxFactory.SimpleBaseType(namedType.EnumUnderlyingType.GenerateTypeSyntax())))
                : null;

            return SyntaxFactory.EnumDeclaration(
                GenerateAttributeDeclarations(namedType, options),
                GenerateModifiers(namedType, destination, options),
                namedType.Name.ToIdentifierToken(),
                baseList: baseList,
                members: default);
        }

        private static SyntaxList<AttributeListSyntax> GenerateAttributeDeclarations(
            INamedTypeSymbol namedType, CodeGenerationOptions options)
        {
            return AttributeGenerator.GenerateAttributeLists(namedType.GetAttributes(), options);
        }

        private static SyntaxTokenList GenerateModifiers(
            INamedTypeSymbol namedType,
            CodeGenerationDestination destination,
            CodeGenerationOptions options)
        {
            var tokens = ArrayBuilder<SyntaxToken>.GetInstance();

            var defaultAccessibility = destination == CodeGenerationDestination.CompilationUnit || destination == CodeGenerationDestination.Namespace
                ? Accessibility.Internal
                : Accessibility.Private;

            AddAccessibilityModifiers(namedType.DeclaredAccessibility, tokens, options, defaultAccessibility);

            if (namedType.IsStatic)
            {
                tokens.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
            }
            else
            {
                if (namedType.TypeKind == TypeKind.Class)
                {
                    if (namedType.IsAbstract)
                    {
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.AbstractKeyword));
                    }

                    if (namedType.IsSealed)
                    {
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.SealedKeyword));
                    }
                }
            }

            if (namedType.IsReadOnly)
            {
                tokens.Add(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));
            }

            if (namedType.IsRefLikeType)
            {
                tokens.Add(SyntaxFactory.Token(SyntaxKind.RefKeyword));
            }

            return tokens.ToSyntaxTokenListAndFree();
        }

        private static TypeParameterListSyntax GenerateTypeParameterList(
            INamedTypeSymbol namedType, CodeGenerationOptions options)
        {
            return TypeParameterGenerator.GenerateTypeParameterList(namedType.TypeParameters, options);
        }

        private static BaseListSyntax? GenerateBaseList(INamedTypeSymbol namedType)
        {
            var types = new List<BaseTypeSyntax>();
            if (namedType.TypeKind == TypeKind.Class && namedType.BaseType != null && namedType.BaseType.SpecialType != Microsoft.CodeAnalysis.SpecialType.System_Object)
                types.Add(SyntaxFactory.SimpleBaseType(namedType.BaseType.GenerateTypeSyntax()));

            foreach (var type in namedType.Interfaces)
                types.Add(SyntaxFactory.SimpleBaseType(type.GenerateTypeSyntax()));

            if (types.Count == 0)
                return null;

            return SyntaxFactory.BaseList(SyntaxFactory.SeparatedList(types));
        }

        private static SyntaxList<TypeParameterConstraintClauseSyntax> GenerateConstraintClauses(INamedTypeSymbol namedType)
            => namedType.TypeParameters.GenerateConstraintClauses();
    }
}
