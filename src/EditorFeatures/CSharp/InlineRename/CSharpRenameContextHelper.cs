// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Copied from VisualStudio.Conversations repo src/Copilot.Vsix/QuickActions/CSharp/CSharpTypeSignatureHelper.cs
// with minor changes

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.InlineRename
{
    internal static class RenameContextHelper
    {
        private const string Space = " ";
        private const string Indentation = "  ";
        private const string OpeningBrace = "{";
        private const string ClosingBrace = "}";
        private const string Semicolon = ";";
        private const string ColonSeparator = " : ";
        private const string CommaSeparator = ", ";
        private const string Unknown = "?";

        private const SymbolDisplayGenericsOptions GenericsOptions =
            SymbolDisplayGenericsOptions.IncludeVariance |
            SymbolDisplayGenericsOptions.IncludeTypeParameters;

        private const SymbolDisplayMiscellaneousOptions MiscellaneousOptions =
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
            SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral |
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
            SymbolDisplayMiscellaneousOptions.RemoveAttributeSuffix |
            SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName;

        private static readonly SymbolDisplayFormat FormatForTypeDefinitions =
            new(typeQualificationStyle:
                    SymbolDisplayTypeQualificationStyle.NameOnly,
                genericsOptions:
                    GenericsOptions |
                    SymbolDisplayGenericsOptions.IncludeTypeConstraints,
                delegateStyle:
                    SymbolDisplayDelegateStyle.NameAndSignature,
                kindOptions:
                    SymbolDisplayKindOptions.IncludeTypeKeyword,
                miscellaneousOptions:
                    MiscellaneousOptions);

        private static readonly SymbolDisplayFormat FormatForBaseTypeAndInterfacesInTypeDefinition =
            new(typeQualificationStyle:
                    SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                genericsOptions:
                    GenericsOptions,
                miscellaneousOptions:
                    MiscellaneousOptions);

        private static readonly SymbolDisplayFormat FormatForMemberDefinitions =
            new(typeQualificationStyle:
                    SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                genericsOptions:
                    GenericsOptions |
                    SymbolDisplayGenericsOptions.IncludeTypeConstraints,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeAccessibility |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeRef |
                    SymbolDisplayMemberOptions.IncludeType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface |
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeConstantValue,
                extensionMethodStyle:
                    SymbolDisplayExtensionMethodStyle.StaticMethod,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeModifiers |
                    SymbolDisplayParameterOptions.IncludeExtensionThis |
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeName |
                    SymbolDisplayParameterOptions.IncludeOptionalBrackets |
                    SymbolDisplayParameterOptions.IncludeDefaultValue,
                propertyStyle:
                    SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
                kindOptions:
                    SymbolDisplayKindOptions.IncludeMemberKeyword,
                miscellaneousOptions:
                    MiscellaneousOptions |
                    SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        private static StringBuilder AppendKeyword(this StringBuilder builder, SyntaxKind keywordKind)
            => builder.Append(SyntaxFacts.GetText(keywordKind));

        // SymbolDisplay does not support displaying accessibility for types at the moment.
        // See https://github.com/dotnet/roslyn/issues/28297.
        private static void AppendAccessibility(this StringBuilder builder, ITypeSymbol type)
        {
            switch (type.DeclaredAccessibility)
            {
                case Accessibility.Private:
                    builder.AppendKeyword(SyntaxKind.PrivateKeyword);
                    break;

                case Accessibility.Internal:
                    builder.AppendKeyword(SyntaxKind.InternalKeyword);
                    break;

                case Accessibility.ProtectedAndInternal:
                    builder.AppendKeyword(SyntaxKind.PrivateKeyword);
                    builder.Append(Space);
                    builder.AppendKeyword(SyntaxKind.ProtectedKeyword);
                    break;

                case Accessibility.Protected:
                    builder.AppendKeyword(SyntaxKind.ProtectedKeyword);
                    break;

                case Accessibility.ProtectedOrInternal:
                    builder.AppendKeyword(SyntaxKind.ProtectedKeyword);
                    builder.Append(Space);
                    builder.AppendKeyword(SyntaxKind.InternalKeyword);
                    break;

                case Accessibility.Public:
                    builder.AppendKeyword(SyntaxKind.PublicKeyword);
                    break;

                default:
                    builder.Append(Unknown);
                    break;
            }

            builder.Append(Space);
        }

        // SymbolDisplay does not support displaying modifiers for types at the moment.
        // See https://github.com/dotnet/roslyn/issues/28297.
        private static void AppendModifiers(this StringBuilder builder, ITypeSymbol type)
        {
            if (type.TypeKind is TypeKind.Class && type.IsAbstract)
            {
                builder.AppendKeyword(SyntaxKind.AbstractKeyword);
                builder.Append(Space);
            }

            if (type.TypeKind is TypeKind.Class && type.IsSealed)
            {
                builder.AppendKeyword(SyntaxKind.SealedKeyword);
                builder.Append(Space);
            }

            if (type.IsStatic)
            {
                builder.AppendKeyword(SyntaxKind.StaticKeyword);
                builder.Append(Space);
            }
        }

        private static void AppendBaseTypeAndInterfaces(
            this StringBuilder builder,
            ITypeSymbol type,
            CancellationToken cancellationToken)
        {
            var baseType = type.BaseType;

            var baseTypeAndInterfaces =
                baseType is null ||
                baseType.SpecialType is SpecialType.System_Object ||
                baseType.SpecialType is SpecialType.System_ValueType
                    ? type.AllInterfaces
                    : [baseType, .. type.AllInterfaces];

            if (baseTypeAndInterfaces.IsDefaultOrEmpty)
            {
                return;
            }

            builder.Append(ColonSeparator);

            for (var i = 0; i < baseTypeAndInterfaces.Length; ++i)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var baseTypeOrInterface = baseTypeAndInterfaces[i];

                builder.Append(
                    Microsoft.CodeAnalysis.CSharp.SymbolDisplay.ToDisplayString(baseTypeOrInterface, FormatForBaseTypeAndInterfacesInTypeDefinition));

                if (i < baseTypeAndInterfaces.Length - 1)
                {
                    builder.Append(CommaSeparator);
                }
            }
        }

        private static void AppendTypeDefinition(
            this StringBuilder builder,
            ITypeSymbol type,
            CancellationToken cancellationToken)
        {
            builder.AppendAccessibility(type);
            builder.AppendModifiers(type);
            builder.Append(Microsoft.CodeAnalysis.CSharp.SymbolDisplay.ToDisplayString(type, FormatForTypeDefinitions));

            if (type.TypeKind is TypeKind.Delegate)
            {
                builder.Append(Semicolon);
            }
            else
            {
                builder.AppendBaseTypeAndInterfaces(type, cancellationToken);
            }
        }

        private static void AppendMemberDefinition(this StringBuilder builder, ISymbol member)
        {
            Assumes.True(
                member is IEventSymbol ||
                member is IFieldSymbol ||
                member is IPropertySymbol ||
                member is IMethodSymbol);

            var line = Microsoft.CodeAnalysis.CSharp.SymbolDisplay.ToDisplayString(member, FormatForMemberDefinitions);
            builder.Append(line);

            if (!line.EndsWith(ClosingBrace))
            {
                builder.Append(Semicolon);
            }
        }

        private static string GetIndentation(int level)
        {
            if (level is 0)
            {
                return string.Empty;
            }

            if (level is 1)
            {
                return Indentation;
            }

            var builder = PooledStringBuilder.GetInstance();

            for (var i = 0; i < level; ++i)
            {
                builder.Builder.Append(Indentation);
            }

            return builder.ToStringAndFree();
        }

        /// <summary>
        /// Returns the type signature for the type specified by the supplied <see cref="ITypeSymbol"/>.
        /// </summary>
        /// <remarks>
        /// Signatures for all contained members are also included within a type's signature but method bodies are not.
        /// </remarks>
        public static string GetSignature(
            this ITypeSymbol type,
            int indentLevel,
            CancellationToken cancellationToken)
        {
            var builder = PooledStringBuilder.GetInstance();
            var indentation = GetIndentation(indentLevel);
            var memberIndentation = GetIndentation(++indentLevel);

            builder.Builder.Append(indentation);
            builder.Builder.AppendTypeDefinition(type, cancellationToken);

            if (type.TypeKind is not TypeKind.Delegate)
            {
                builder.Builder.AppendLine();
                builder.Builder.Append(indentation);
                builder.Builder.AppendLine(OpeningBrace);
            }

            foreach (var member in type.GetMembers())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (member.CanBeReferencedByName &&
                    !member.IsImplicitlyDeclared &&
                    !member.HasUnsupportedMetadata)
                {
                    if (member is IEventSymbol ||
                        member is IFieldSymbol ||
                        member is IPropertySymbol ||
                        member is IMethodSymbol)
                    {
                        builder.Builder.Append(memberIndentation);
                        builder.Builder.AppendMemberDefinition(member);
                        builder.Builder.AppendLine();
                    }
                    else if (member is ITypeSymbol nestedType)
                    {
                        var nestedTypeSignature = nestedType.GetSignature(indentLevel, cancellationToken);
                        builder.Builder.AppendLine(nestedTypeSignature);
                    }
                }
            }

            if (type.TypeKind is not TypeKind.Delegate)
            {
                builder.Builder.Append(indentation);
                builder.Builder.Append(ClosingBrace);
            }

            return builder.ToStringAndFree();
        }

        /// <summary>
        /// Returns the file path(s) that contains the definition of the type specified by the supplied
        /// <see cref="ITypeSymbol"/>.
        /// </summary>
        /// <remarks>
        /// A type's definition may be split across multiple files if the type is a <see langword="partial"/> type.
        /// </remarks>
        public static async Task<ImmutableHashSet<string>> GetDeclarationFilePathsAsync(
            this ITypeSymbol type,
            Document referencingDocument,
            CancellationToken cancellationToken)
        {
            if (type.Locations.Length is 0)
            {
                return [];
            }

            var builder = PooledHashSet<string>.GetInstance();

            foreach (var location in type.Locations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (location.IsInSource &&
                    location.SourceTree.FilePath is { } sourceFilePath &&
                    !string.IsNullOrWhiteSpace(sourceFilePath))
                {
                    builder.Add(sourceFilePath);
                }
                else if (location.IsInMetadata && location.MetadataModule?.ContainingAssembly is { } assembly)
                {
                    var referencingProject = referencingDocument.Project;
                    var compilation = await referencingProject.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                    var metadataReference = compilation?.GetMetadataReference(assembly);

                    if (metadataReference is PortableExecutableReference portableExecutableReference &&
                        portableExecutableReference.FilePath is { } portableExecutableFilePath &&
                        !string.IsNullOrWhiteSpace(portableExecutableFilePath))
                    {
                        builder.Add(portableExecutableFilePath);
                    }
                    else if (metadataReference?.Display is { } filePath && !string.IsNullOrWhiteSpace(filePath))
                    {
                        builder.Add(filePath);
                    }
                }
            }

            var filePaths = builder.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
            builder.Free();

            return filePaths;
        }

        /// <summary>
        /// Returns the <see cref="TextSpan"/> of the nearest encompassing <see cref="CSharpSyntaxNode"/> of type
        /// <typeparamref name="T"/> of which the supplied <paramref name="span"/> is a part within the supplied
        /// <paramref name="document"/>.
        /// </summary>
        public static async Task<TextSpan?> TryGetSurroundingNodeSpanAsync<T>(
            this Document document,
            TextSpan span,
            CancellationToken cancellationToken)
                where T : CSharpSyntaxNode
        {
            if (document.Project.Language is not LanguageNames.CSharp)
            {
                return null;
            }

            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (model is null)
            {
                return null;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is null)
            {
                return null;
            }

            var containingNode = root.FindNode(span);
            var targetNode = containingNode.FirstAncestorOrSelf<T>() ?? containingNode;

            return targetNode.Span;
        }

        private static string GetOutermostNamespaceName(this ITypeSymbol type, CancellationToken cancellationToken)
        {
            var outermostNamespaceName = string.Empty;
            var currentNamespace = type.ContainingNamespace;

            while (currentNamespace is not null && !currentNamespace.IsGlobalNamespace)
            {
                cancellationToken.ThrowIfCancellationRequested();

                outermostNamespaceName = currentNamespace.Name;
                currentNamespace = currentNamespace.ContainingNamespace;
            }

            return outermostNamespaceName;
        }

        private static bool IsWellKnownType(this ITypeSymbol type, CancellationToken cancellationToken)
            => type.GetOutermostNamespaceName(cancellationToken) is "System";

        private static ImmutableArray<TextSpan> RemoveDuplicatesAndPreserveOrder(this ImmutableArray<TextSpan> spans)
        {
            if (spans.IsDefaultOrEmpty || spans.Length is 1)
            {
                return spans;
            }

            if (spans.Length is 2)
            {
                return spans[0] == spans[1] ? [spans[0]] : spans;
            }

            var seen = PooledHashSet<TextSpan>.GetInstance();
            var builder = ArrayBuilder<TextSpan>.GetInstance();

            foreach (var span in spans)
            {
                if (seen.Add(span))
                {
                    builder.Add(span);
                }
            }

            seen.Free();
            return builder.ToImmutableAndFree();
        }

        private static bool AddType(
            this ArrayBuilder<ITypeSymbol> relevantTypes,
            ITypeSymbol type,
            HashSet<ITypeSymbol> seenTypes,
            CancellationToken cancellationToken)
        {
            if (type.TypeKind is TypeKind.Error || !seenTypes.Add(type))
            {
                return false;
            }

            if (type.ContainingType is { } containingType)
            {
                return relevantTypes.AddType(containingType, seenTypes, cancellationToken);
            }

            if (type is IArrayTypeSymbol arrayType)
            {
                return relevantTypes.AddType(arrayType.ElementType, seenTypes, cancellationToken);
            }

            if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                foreach (var typeArgument in namedType.TypeArguments)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    relevantTypes.AddType(typeArgument, seenTypes, cancellationToken);
                }

                if (!namedType.Equals(namedType.ConstructedFrom, SymbolEqualityComparer.Default))
                {
                    return relevantTypes.AddType(namedType.ConstructedFrom, seenTypes, cancellationToken);
                }
            }

            if (type.TypeKind is TypeKind.Pointer ||
                type.TypeKind is TypeKind.TypeParameter ||
                type.TypeKind is TypeKind.Module ||
                type.TypeKind is TypeKind.Unknown ||
                type.SpecialType is not SpecialType.None ||
                type.IsWellKnownType(cancellationToken))
            {
                return false;
            }

            relevantTypes.Add(type);
            return true;
        }

        private static void AddTypeAlongWithBaseTypesAndInterfaces(
            this ArrayBuilder<ITypeSymbol> relevantTypes,
            ITypeSymbol type,
            HashSet<ITypeSymbol> seenTypes,
            CancellationToken cancellationToken)
        {
            if (!relevantTypes.AddType(type, seenTypes, cancellationToken))
            {
                return;
            }

            if (type.BaseType is { } baseType)
            {
                relevantTypes.AddTypeAlongWithBaseTypesAndInterfaces(baseType, seenTypes, cancellationToken);
            }

            foreach (var @interface in type.Interfaces)
            {
                cancellationToken.ThrowIfCancellationRequested();

                relevantTypes.AddTypeAlongWithBaseTypesAndInterfaces(@interface, seenTypes, cancellationToken);
            }
        }

        /// <summary>
        /// Retrieves the <see cref="ITypeSymbol"/>s for the types that are referenced within the specified set of spans
        /// within a document.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Also returns <see cref="ITypeSymbol"/>s for related types such as base types and interfaces implemented by the
        /// types that are directly referenced in the supplied set of spans.
        /// </para>
        /// <para>
        /// The relative order in which the type references are encountered in the specified set of spans is preserved in
        /// the returned collection of <see cref="ITypeSymbol"/>s.
        /// </para>
        /// </remarks>
        public static async Task<ImmutableArray<ITypeSymbol>> GetRelevantTypesAsync(
            this Document document,
            ImmutableArray<TextSpan> spansOfInterest,
            CancellationToken cancellationToken)
        {
            if (document.Project.Language is not LanguageNames.CSharp)
            {
                return [];
            }

            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (model is null)
            {
                return [];
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is null)
            {
                return [];
            }

            var relevantTypes = ArrayBuilder<ITypeSymbol>.GetInstance();
            var seenTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            var seenSpans = PooledHashSet<TextSpan>.GetInstance();

            spansOfInterest = spansOfInterest.RemoveDuplicatesAndPreserveOrder();
            foreach (var spanOfInterest in spansOfInterest)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var containingNode = root.FindNode(spanOfInterest);

                var tokensOfInterest =
                    containingNode
                        .DescendantTokens()
                        .SkipWhile(t => t.Span.End < spanOfInterest.Start)
                        .TakeWhile(t => t.Span.Start <= spanOfInterest.End);

                foreach (var token in tokensOfInterest)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var kind = token.Kind();

                    bool isInterestingKind =
                        kind is SyntaxKind.IdentifierToken ||
                        kind is SyntaxKind.BaseKeyword ||
                        kind is SyntaxKind.ThisKeyword;

                    if (isInterestingKind &&
                        spanOfInterest.Contains(token.Span) &&
                        token.Parent is { } node &&
                        seenSpans.Add(token.FullSpan) &&
                        (node.FullSpan == token.FullSpan || seenSpans.Add(node.FullSpan)))
                    {
                        if (node is BaseTypeDeclarationSyntax typeSyntax &&
                            model.GetDeclaredSymbol(typeSyntax, cancellationToken) is INamedTypeSymbol declaredType)
                        {
                            relevantTypes.AddTypeAlongWithBaseTypesAndInterfaces(
                                declaredType,
                                seenTypes,
                                cancellationToken);
                        }
                        else if (node is MemberDeclarationSyntax memberSyntax &&
                            memberSyntax.FirstAncestorOrSelf<TypeDeclarationSyntax>() is { } containingTypeSyntax &&
                            seenSpans.Add(containingTypeSyntax.FullSpan) &&
                            model.GetDeclaredSymbol(containingTypeSyntax, cancellationToken) is INamedTypeSymbol containingDeclaredType)
                        {
                            relevantTypes.AddTypeAlongWithBaseTypesAndInterfaces(
                                containingDeclaredType,
                                seenTypes,
                                cancellationToken);
                        }
                        else if (model.GetTypeInfo(node, cancellationToken).Type is ITypeSymbol typeInfoType)
                        {
                            relevantTypes.AddTypeAlongWithBaseTypesAndInterfaces(
                                typeInfoType,
                                seenTypes,
                                cancellationToken);
                        }
                        else if (model.GetSymbolInfo(node, cancellationToken).Symbol is ITypeSymbol symbolInfoType)
                        {
                            relevantTypes.AddTypeAlongWithBaseTypesAndInterfaces(
                                symbolInfoType,
                                seenTypes,
                                cancellationToken);
                        }
                    }
                }
            }

            seenSpans.Free();
            return relevantTypes.ToImmutableAndFree();
        }
    }
}
