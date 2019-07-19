// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers
{
    internal abstract partial class AbstractGenerateEqualsAndGetHashCodeService : IGenerateEqualsAndGetHashCodeService
    {
        private const string GetHashCodeName = nameof(object.GetHashCode);
        private static readonly SyntaxAnnotation s_specializedFormattingAnnotation = new SyntaxAnnotation();

        protected abstract bool TryWrapWithUnchecked(
            ImmutableArray<SyntaxNode> statements, out ImmutableArray<SyntaxNode> wrappedStatements);

        public async Task<Document> FormatDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            var rules = new List<AbstractFormattingRule> { new FormatLargeBinaryExpressionRule(document.GetLanguageService<ISyntaxFactsService>()) };
            rules.AddRange(Formatter.GetDefaultFormattingRules(document));

            var formattedDocument = await Formatter.FormatAsync(
                document, s_specializedFormattingAnnotation,
                options: null, rules: rules, cancellationToken: cancellationToken).ConfigureAwait(false);
            return formattedDocument;
        }

        public async Task<IMethodSymbol> GenerateEqualsMethodAsync(
            Document document, INamedTypeSymbol namedType, ImmutableArray<ISymbol> members,
            string localNameOpt, CancellationToken cancellationToken)
        {
            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            return document.GetLanguageService<SyntaxGenerator>().CreateEqualsMethod(
                compilation, tree.Options, namedType, members, localNameOpt,
                s_specializedFormattingAnnotation, cancellationToken);
        }

        public async Task<IMethodSymbol> GenerateIEquatableEqualsMethodAsync(
            Document document, INamedTypeSymbol namedType,
            ImmutableArray<ISymbol> members, CancellationToken cancellationToken)
        {
            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            return document.GetLanguageService<SyntaxGenerator>().CreateIEqutableEqualsMethod(
                compilation, namedType, members,
                s_specializedFormattingAnnotation, cancellationToken);
        }

        public async Task<IMethodSymbol> GenerateEqualsMethodThroughIEquatableEqualsAsync(
            Document document, INamedTypeSymbol containingType, CancellationToken cancellationToken)
        {
            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var generator = document.GetLanguageService<SyntaxGenerator>();

            var expressions = ArrayBuilder<SyntaxNode>.GetInstance();
            var objName = generator.IdentifierName("obj");
            if (containingType.IsValueType)
            {
                if (generator.SupportsPatterns(tree.Options))
                {
                    // return obj is T t && this.Equals(t);
                    var localName = containingType.GetLocalName();

                    expressions.Add(
                        generator.IsPatternExpression(objName,
                            generator.DeclarationPattern(containingType, localName)));
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

            expressions.Free();
            return compilation.CreateEqualsMethod(
                ImmutableArray.Create(statement));
        }

        public async Task<IMethodSymbol> GenerateGetHashCodeMethodAsync(
            Document document, INamedTypeSymbol namedType,
            ImmutableArray<ISymbol> members, CancellationToken cancellationToken)
        {
            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var factory = document.GetLanguageService<SyntaxGenerator>();
            return CreateGetHashCodeMethod(
                factory, compilation, namedType, members, cancellationToken);
        }

        private IMethodSymbol CreateGetHashCodeMethod(
            SyntaxGenerator factory, Compilation compilation,
            INamedTypeSymbol namedType, ImmutableArray<ISymbol> members,
            CancellationToken cancellationToken)
        {
            var statements = CreateGetHashCodeStatements(
                factory, compilation, namedType, members, cancellationToken);

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
            SyntaxGenerator factory, Compilation compilation,
            INamedTypeSymbol namedType, ImmutableArray<ISymbol> members,
            CancellationToken cancellationToken)
        {
            // If we have access to System.HashCode, then just use that.
            var hashCodeType = compilation.GetTypeByMetadataName("System.HashCode");

            var components = factory.GetGetHashCodeComponents(
                compilation, namedType, members,
                justMemberReference: true, cancellationToken);

            if (components.Length > 0 && hashCodeType != null)
            {
                return CreateGetHashCodeStatementsUsingSystemHashCode(
                    factory, compilation, hashCodeType, components);
            }

            // Otherwise, try to just spit out a reasonable hash code for these members.
            var statements = factory.CreateGetHashCodeMethodStatements(
                compilation, namedType, members, useInt64: false, cancellationToken);

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
            var valueTupleType = compilation.GetTypeByMetadataName(typeof(ValueTuple).FullName);
            if (components.Length >= 2 && valueTupleType != null)
            {
                return ImmutableArray.Create(factory.ReturnStatement(
                    factory.InvocationExpression(
                        factory.MemberAccessExpression(
                            factory.TupleExpression(components),
                            GetHashCodeName))));
            }

            // Otherwise, use 64bit math to compute the hash.  Importantly, if we always clamp
            // the hash to be 32 bits or less, then the following cannot ever overflow in 64
            // bits: hashCode = hashCode * -1521134295 + j.GetHashCode()
            //
            // So we'll generate lines like: hashCode = (hashCode * -1521134295 + j.GetHashCode()) & 0x7FFFFFFF
            //
            // This does mean all hashcodes will be positive.  But it will avoid the overflow problem.
            return factory.CreateGetHashCodeMethodStatements(
                compilation, namedType, members, useInt64: true, cancellationToken);
        }

        private ImmutableArray<SyntaxNode> CreateGetHashCodeStatementsUsingSystemHashCode(
            SyntaxGenerator factory, Compilation compilation, INamedTypeSymbol hashCodeType,
            ImmutableArray<SyntaxNode> memberReferences)
        {
            if (memberReferences.Length <= 8)
            {
                var statement = factory.ReturnStatement(
                    factory.InvocationExpression(
                        factory.MemberAccessExpression(factory.TypeExpression(hashCodeType), "Combine"),
                        memberReferences));
                return ImmutableArray.Create(statement);
            }

            const string hashName = "hash";
            var statements = ArrayBuilder<SyntaxNode>.GetInstance();
            statements.Add(factory.LocalDeclarationStatement(hashName,
                factory.ObjectCreationExpression(hashCodeType)));

            var localReference = factory.IdentifierName(hashName);
            foreach (var member in memberReferences)
            {
                statements.Add(factory.ExpressionStatement(
                    factory.InvocationExpression(
                        factory.MemberAccessExpression(localReference, "Add"),
                        member)));
            }

            statements.Add(factory.ReturnStatement(
                factory.InvocationExpression(
                    factory.MemberAccessExpression(localReference, "ToHashCode"))));

            return statements.ToImmutableAndFree();
        }
    }
}
