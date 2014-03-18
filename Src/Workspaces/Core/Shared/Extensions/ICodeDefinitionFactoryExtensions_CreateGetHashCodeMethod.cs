// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class ICodeDefinitionFactoryExtensions
    {
        private const string GetHashCodeName = "GetHashCode";

        public static IMethodSymbol CreateGetHashCodeMethod(
            this ISyntaxFactoryService factory,
            Compilation compilation,
            INamedTypeSymbol containingType,
            IList<ISymbol> symbols,
            CancellationToken cancellationToken)
        {
            var statements = CreateGetHashCodeMethodStatements(factory, compilation, containingType, symbols, cancellationToken);

            return CodeGenerationSymbolFactory.CreateMethodSymbol(
                attributes: null,
                accessibility: Accessibility.Public,
                modifiers: new SymbolModifiers(isOverride: true),
                returnType: compilation.GetSpecialType(SpecialType.System_Int32),
                explicitInterfaceSymbol: null,
                name: GetHashCodeName,
                typeParameters: null,
                parameters: null,
                statements: statements);
        }

        private static IList<SyntaxNode> CreateGetHashCodeMethodStatements(
            ISyntaxFactoryService factory,
            Compilation compilation,
            INamedTypeSymbol containingType,
            IList<ISymbol> members,
            CancellationToken cancellationToken)
        {
            const string HashCodeName = "hashCode";

            // -1521134295
            var permuteValue = factory.CreateNegateExpression(
                factory.CreateConstantExpression(1521134295));

            var statements = new List<SyntaxNode>();

            var hashCodeNameExpression = factory.CreateIdentifierName(HashCodeName);

            var firstHashValue = ComputeHashValue(factory, compilation, members[0]);
            if (members.Count == 1)
            {
#if false
                return this.S1.GetHashCode();
#endif
                statements.Add(factory.CreateReturnStatement(firstHashValue));
            }
            else
            {
#if false
                var hashCode = this.S1.GetHashCode();
#endif
                statements.Add(factory.CreateLocalDeclarationStatement(
                    factory.CreateVariableDeclarator(HashCodeName, firstHashValue)));

                for (var i = 1; i < members.Count; i++)
                {
#if false
                    hashCode = hashCode * 0xA5555529 + value
#endif
                    statements.Add(factory.CreateExpressionStatement(
                        factory.CreateAssignExpression(hashCodeNameExpression,
                            factory.CreateAddExpression(
                                factory.CreateMultiplyExpression(hashCodeNameExpression, permuteValue),
                                ComputeHashValue(factory, compilation, members[i])))));
                }

#if false
                return hashCode;
#endif
                statements.Add(factory.CreateReturnStatement(hashCodeNameExpression));
            }

            return statements;
        }

        private static SyntaxNode ComputeHashValue(
            ISyntaxFactoryService factory,
            Compilation compilation,
            ISymbol member)
        {
            var getHashCodeNameExpression = factory.CreateIdentifierName(GetHashCodeName);
            var thisSymbol = factory.CreateMemberAccessExpression(factory.CreateThisExpression(),
                factory.CreateIdentifierName(member.Name)).WithAdditionalAnnotations(Simplification.Simplifier.Annotation);

#if false
            EqualityComparer<SType>.Default.GetHashCode(this.S1)
#endif

            return factory.CreateInvocationExpression(
                factory.CreateMemberAccessExpression(
                    GetDefaultEqualityComparer(factory, compilation, member),
                    getHashCodeNameExpression),
                thisSymbol);
        }
    }
}