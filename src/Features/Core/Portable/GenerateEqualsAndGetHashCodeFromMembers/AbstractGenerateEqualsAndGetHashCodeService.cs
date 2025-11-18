// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers;

internal abstract partial class AbstractGenerateEqualsAndGetHashCodeService : IGenerateEqualsAndGetHashCodeService
{
    private const string GetHashCodeName = nameof(object.GetHashCode);
    private static readonly SyntaxAnnotation s_specializedFormattingAnnotation = new();

    protected abstract bool TryWrapWithUnchecked(
        ImmutableArray<SyntaxNode> statements, out ImmutableArray<SyntaxNode> wrappedStatements);

    public async Task<Document> FormatDocumentAsync(Document document, SyntaxFormattingOptions options, CancellationToken cancellationToken)
    {
        var formatBinaryRule = new FormatLargeBinaryExpressionRule(document.GetRequiredLanguageService<ISyntaxFactsService>());
        var formattedDocument = await Formatter.FormatAsync(
            document, s_specializedFormattingAnnotation,
            options,
            [formatBinaryRule, .. Formatter.GetDefaultFormattingRules(document)],
            cancellationToken).ConfigureAwait(false);
        return formattedDocument;
    }

    public async Task<IMethodSymbol> GenerateEqualsMethodAsync(
        Document document, INamedTypeSymbol namedType, ImmutableArray<ISymbol> members,
        string? localNameOpt, CancellationToken cancellationToken)
    {
        var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
        var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var generator = document.GetRequiredLanguageService<SyntaxGenerator>();
        var generatorInternal = document.GetRequiredLanguageService<SyntaxGeneratorInternal>();
        return generator.CreateEqualsMethod(
            generatorInternal, compilation, tree.Options, namedType, members, localNameOpt, s_specializedFormattingAnnotation);
    }

    public async Task<IMethodSymbol> GenerateIEquatableEqualsMethodAsync(
        Document document, INamedTypeSymbol namedType,
        ImmutableArray<ISymbol> members, INamedTypeSymbol constructedEquatableType, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var generator = document.GetRequiredLanguageService<SyntaxGenerator>();
        var generatorInternal = document.GetRequiredLanguageService<SyntaxGeneratorInternal>();
        return generator.CreateIEquatableEqualsMethod(
            generatorInternal, semanticModel, namedType, members, constructedEquatableType, s_specializedFormattingAnnotation);
    }

    public async Task<IMethodSymbol> GenerateEqualsMethodThroughIEquatableEqualsAsync(
        Document document, INamedTypeSymbol containingType, CancellationToken cancellationToken)
    {
        var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
        var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var generator = document.GetRequiredLanguageService<SyntaxGenerator>();

        using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var expressions);
        var objName = generator.IdentifierName("obj");
        if (containingType.IsValueType)
        {
            if (generator.SyntaxGeneratorInternal.SupportsPatterns(tree.Options))
            {
                // return obj is T t && this.Equals(t);
                var localName = containingType.GetLocalName();

                expressions.Add(
                    generator.SyntaxGeneratorInternal.IsPatternExpression(objName,
                        generator.SyntaxGeneratorInternal.DeclarationPattern(containingType, localName)));
                expressions.Add(
                    generator.InvocationExpression(
                        generator.MemberAccessExpression(
                            generator.ThisExpression(),
                            generator.IdentifierName(nameof(Equals))),
                        generator.IdentifierName(localName)));
            }
            else
            {
                // return obj is T && this.Equals((T)obj);
                expressions.Add(generator.IsTypeExpression(objName, containingType));
                expressions.Add(
                    generator.InvocationExpression(
                        generator.MemberAccessExpression(
                            generator.ThisExpression(),
                            generator.IdentifierName(nameof(Equals))),
                        generator.CastExpression(containingType, objName)));
            }
        }
        else
        {
            // return this.Equals(obj as T);
            expressions.Add(
                generator.InvocationExpression(
                    generator.MemberAccessExpression(
                        generator.ThisExpression(),
                        generator.IdentifierName(nameof(Equals))),
                    generator.TryCastExpression(objName, containingType)));
        }

        var statement = generator.ReturnStatement(
            expressions.Aggregate(generator.LogicalAndExpression));

        return compilation.CreateEqualsMethod([statement]);
    }

    public async Task<IMethodSymbol> GenerateGetHashCodeMethodAsync(
        Document document, INamedTypeSymbol namedType,
        ImmutableArray<ISymbol> members, CancellationToken cancellationToken)
    {
        var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
        var factory = document.GetRequiredLanguageService<SyntaxGenerator>();
        var generatorInternal = document.GetRequiredLanguageService<SyntaxGeneratorInternal>();
        return CreateGetHashCodeMethod(factory, generatorInternal, compilation, namedType, members);
    }

    private IMethodSymbol CreateGetHashCodeMethod(
        SyntaxGenerator factory, SyntaxGeneratorInternal generatorInternal, Compilation compilation,
        INamedTypeSymbol namedType, ImmutableArray<ISymbol> members)
    {
        var statements = CreateGetHashCodeStatements(
            factory, generatorInternal, compilation, namedType, members);

        return CodeGenerationSymbolFactory.CreateMethodSymbol(
            attributes: default,
            accessibility: Accessibility.Public,
            modifiers: new DeclarationModifiers(isOverride: true),
            returnType: compilation.GetSpecialType(SpecialType.System_Int32),
            refKind: RefKind.None,
            explicitInterfaceImplementations: default,
            name: GetHashCodeName,
            typeParameters: default,
            parameters: default,
            statements: statements);
    }

    private ImmutableArray<SyntaxNode> CreateGetHashCodeStatements(
        SyntaxGenerator factory, SyntaxGeneratorInternal generatorInternal, Compilation compilation,
        INamedTypeSymbol namedType, ImmutableArray<ISymbol> members)
    {
        // See if there's an accessible System.HashCode we can call into to do all the work.
        var hashCodeType = compilation.GetTypeByMetadataName("System.HashCode");
        if (hashCodeType != null && !hashCodeType.IsAccessibleWithin(namedType))
            hashCodeType = null;

        var components = factory.GetGetHashCodeComponents(
            generatorInternal, compilation, namedType, members, justMemberReference: true);

        if (components.Length > 0 && hashCodeType != null)
        {
            return factory.CreateGetHashCodeStatementsUsingSystemHashCode(
                factory.SyntaxGeneratorInternal, hashCodeType, components);
        }

        // Otherwise, try to just spit out a reasonable hash code for these members.
        var statements = factory.CreateGetHashCodeMethodStatements(
            factory.SyntaxGeneratorInternal, compilation, namedType, members, useInt64: false);

        // Unfortunately, our 'reasonable' hash code may overflow in checked contexts.
        // C# can handle this by adding 'checked{}' around the code, VB has to jump
        // through more hoops.
        if (!compilation.Options.CheckOverflow)
        {
            return statements;
        }

        if (TryWrapWithUnchecked(statements, out var wrappedStatements))
        {
            return wrappedStatements;
        }

        // If tuples are available, use (a, b, c).GetHashCode to simply generate the tuple.
        var valueTupleType = compilation.GetTypeByMetadataName(typeof(ValueTuple).FullName!);
        if (components.Length >= 2 && valueTupleType != null)
        {
            return [factory.ReturnStatement(
                factory.InvocationExpression(
                    factory.MemberAccessExpression(
                        factory.TupleExpression(components),
                        GetHashCodeName)))];
        }

        // Otherwise, use 64bit math to compute the hash.  Importantly, if we always clamp
        // the hash to be 32 bits or less, then the following cannot ever overflow in 64
        // bits: hashCode = hashCode * -1521134295 + j.GetHashCode()
        //
        // So we'll generate lines like: hashCode = (hashCode * -1521134295 + j.GetHashCode()) & 0x7FFFFFFF
        //
        // This does mean all hashcodes will be positive.  But it will avoid the overflow problem.
        return factory.CreateGetHashCodeMethodStatements(
            factory.SyntaxGeneratorInternal, compilation, namedType, members, useInt64: true);
    }
}
