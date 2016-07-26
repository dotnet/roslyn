// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class ICodeDefinitionFactoryExtensions
    {
        private const string GetHashCodeName = "GetHashCode";

        public static IMethodSymbol CreateGetHashCodeMethod(
            this SyntaxGenerator factory,
            Compilation compilation,
            INamedTypeSymbol containingType,
            IList<ISymbol> symbols,
            CancellationToken cancellationToken)
        {
            var statements = CreateGetHashCodeMethodStatements(factory, compilation, containingType, symbols, cancellationToken);

            return CodeGenerationSymbolFactory.CreateMethodSymbol(
                attributes: null,
                accessibility: Accessibility.Public,
                modifiers: new DeclarationModifiers(isOverride: true),
                returnType: compilation.GetSpecialType(SpecialType.System_Int32),
                explicitInterfaceSymbol: null,
                name: GetHashCodeName,
                typeParameters: null,
                parameters: null,
                statements: statements);
        }

        private static IList<SyntaxNode> CreateGetHashCodeMethodStatements(
            SyntaxGenerator factory,
            Compilation compilation,
            INamedTypeSymbol containingType,
            IList<ISymbol> members,
            CancellationToken cancellationToken)
        {
            const string HashCodeName = "hashCode";

            // -1521134295
            var permuteValue = factory.NegateExpression(
                factory.LiteralExpression(1521134295));

            var statements = new List<SyntaxNode>();

            var hashCodeNameExpression = factory.IdentifierName(HashCodeName);

            var firstHashValue = ComputeHashValue(factory, compilation, members[0]);
            if (members.Count == 1)
            {
#if false
                return this.S1.GetHashCode();
#endif
                statements.Add(factory.ReturnStatement(firstHashValue));
            }
            else
            {
#if false
                var hashCode = this.S1.GetHashCode();
#endif
                statements.Add(factory.LocalDeclarationStatement(HashCodeName, firstHashValue));

                for (var i = 1; i < members.Count; i++)
                {
#if false
                    hashCode = hashCode * 0xA5555529 + value
#endif
                    statements.Add(factory.ExpressionStatement(
                        factory.AssignmentStatement(hashCodeNameExpression,
                            factory.AddExpression(
                                factory.MultiplyExpression(hashCodeNameExpression, permuteValue),
                                ComputeHashValue(factory, compilation, members[i])))));
                }

#if false
                return hashCode;
#endif
                statements.Add(factory.ReturnStatement(hashCodeNameExpression));
            }

            return statements;
        }

        private static SyntaxNode ComputeHashValue(
            SyntaxGenerator factory,
            Compilation compilation,
            ISymbol member)
        {
            var getHashCodeNameExpression = factory.IdentifierName(GetHashCodeName);
            var thisSymbol = factory.MemberAccessExpression(factory.ThisExpression(),
                factory.IdentifierName(member.Name)).WithAdditionalAnnotations(Simplification.Simplifier.Annotation);

#if false
            EqualityComparer<SType>.Default.GetHashCode(this.S1)
#endif

            return factory.InvocationExpression(
                factory.MemberAccessExpression(
                    GetDefaultEqualityComparer(factory, compilation, member),
                    getHashCodeNameExpression),
                thisSymbol);
        }
    }
}
