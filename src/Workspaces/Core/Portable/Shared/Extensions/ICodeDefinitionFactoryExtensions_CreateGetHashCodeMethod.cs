// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class ICodeDefinitionFactoryExtensions
    {
        private const string GetHashCodeName = nameof(object.GetHashCode);

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
                returnsByRef: false,
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
            var statements = new List<SyntaxNode>();

            var hasBaseGetHashCode = HasExistingBaseGetHashCodeMethod(containingType, cancellationToken);
            var baseHashCode = factory.InvocationExpression(
                factory.MemberAccessExpression(factory.BaseExpression(), GetHashCodeName));

            if (members.Count == 0)
            {
#if false
                return 0; // or
                return base.GetHashCode();
#endif
                var expression = hasBaseGetHashCode
                    ? baseHashCode
                    : factory.LiteralExpression(0);

                statements.Add(factory.ReturnStatement(expression));
            }
            else
            {
                const string HashCodeName = "hashCode";

                // -1521134295
                var permuteValue = factory.NegateExpression(
                    factory.LiteralExpression(1521134295));

                var hashCodeNameExpression = factory.IdentifierName(HashCodeName);

                var firstMemberHashValue = ComputeHashValue(factory, compilation, members[0]);
                if (members.Count == 1 && !hasBaseGetHashCode)
                {
#if false
                    return this.S1.GetHashCode();
#endif
                    statements.Add(factory.ReturnStatement(firstMemberHashValue));
                }
                else
                {
#if false
                    var hashCode = this.S1.GetHashCode(); // or
                    var hashCode = base.GetHashCode();
#endif

                    var firstInit = hasBaseGetHashCode
                        ? baseHashCode
                        : firstMemberHashValue;
                    statements.Add(factory.LocalDeclarationStatement(HashCodeName, firstInit));

                    var startingIndex = hasBaseGetHashCode ? 0 : 1;
                    for (var i = startingIndex; i < members.Count; i++)
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
            }

            return statements;
        }

        private static bool HasExistingBaseGetHashCodeMethod(INamedTypeSymbol containingType, CancellationToken cancellationToken)
        {
            // Check if any of our base types override GetHashCode.  If so, first check with them.
            var existingMethods =
                from baseType in containingType.GetBaseTypes()
                from method in baseType.GetMembers(GetHashCodeName).OfType<IMethodSymbol>()
                where method.IsOverride &&
                      method.DeclaredAccessibility == Accessibility.Public &&
                      !method.IsStatic &&
                      method.Parameters.Length == 0 &&
                      method.ReturnType.SpecialType == SpecialType.System_Int32
                select method;

            return existingMethods.Any();
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

            var memberType = member.GetSymbolType();
            if (IsPrimitiveValueType(memberType) && memberType.SpecialType != SpecialType.System_String)
            {
                return factory.InvocationExpression(
                    factory.MemberAccessExpression(thisSymbol, nameof(object.GetHashCode)));
            }
            else
            {
                return factory.InvocationExpression(
                    factory.MemberAccessExpression(
                        GetDefaultEqualityComparer(factory, compilation, member),
                        getHashCodeNameExpression),
                    thisSymbol);
            }
        }
    }
}