// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class ICodeDefinitionFactoryExtensions
    {
        private const string GetHashCodeName = nameof(object.GetHashCode);

        public static IMethodSymbol CreateGetHashCodeMethod(
            this SyntaxGenerator factory,
            Compilation compilation,
            INamedTypeSymbol containingType,
            ImmutableArray<ISymbol> symbols,
            CancellationToken cancellationToken)
        {
            var statements = CreateGetHashCodeMethodStatements(factory, compilation, containingType, symbols, cancellationToken);

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

        /// <summary>
        /// Generates an override of <see cref="object.Equals(object)"/> similar to the one
        /// generated for anonymous types.
        /// </summary>
        private static ImmutableArray<SyntaxNode> CreateGetHashCodeMethodStatements(
            SyntaxGenerator factory,
            Compilation compilation,
            INamedTypeSymbol containingType,
            ImmutableArray<ISymbol> members,
            CancellationToken cancellationToken)
        {
            var hasBaseGetHashCode = HasExistingBaseGetHashCodeMethod(containingType, cancellationToken);
            var baseHashCode = factory.InvocationExpression(
                factory.MemberAccessExpression(factory.BaseExpression(), GetHashCodeName));

            if (members.Length == 0)
            {
                // Trivial case.  Just directly:
                //
                //      return 0; or
                //      return base.GetHashCode();

                return ImmutableArray.Create(factory.ReturnStatement(
                    hasBaseGetHashCode ? baseHashCode : factory.LiteralExpression(0)));
            }

            const int hashFactor = -1521134295;

            var initHash = 0;
            foreach (var symbol in members)
            {
                initHash = initHash * hashFactor + Hash.GetFNVHashCode(symbol.Name);
            }

            if (members.Length == 1 && !hasBaseGetHashCode)
            {
                // If there's just one value to hash, then we can compute and directly
                // return it.  i.e.  The full computation is:
                //
                //      return initHash * hashfactor + ...
                //
                // But as we know the values of initHash and hashFactor we can just compute
                // is here and directly inject the result value, producing:
                //
                //      return someHash + this.S1.GetHashCode();    // or

                var multiplyResult = initHash * hashFactor;
                return ImmutableArray.Create(factory.ReturnStatement(
                    factory.AddExpression(
                        CreateLiteralExpression(factory, multiplyResult),
                        ComputeHashValue(factory, compilation, members[0]))));
            }

            var statements = ArrayBuilder<SyntaxNode>.GetInstance();

            // initialize the initial hashCode:
            //
            //      var hashCode = initialHashCode;
            const string HashCodeName = "hashCode";
            statements.Add(factory.LocalDeclarationStatement(HashCodeName, CreateLiteralExpression(factory, initHash)));

            var hashCodeNameExpression = factory.IdentifierName(HashCodeName);

            // -1521134295
            var permuteValue = CreateLiteralExpression(factory, hashFactor);

            // If our base type overrode GetHashCode, then include it's value in our hashCode
            // as well.
            if (hasBaseGetHashCode)
            {
                //  hashCode = hashCode * -1521134295 + base.GetHashCode();
                statements.Add(factory.ExpressionStatement(
                    factory.AssignmentStatement(hashCodeNameExpression,
                        factory.AddExpression(
                            factory.MultiplyExpression(hashCodeNameExpression, permuteValue),
                            baseHashCode))));
            }

            foreach (var member in members)
            {
                // hashCode = hashCode * -1521134295 + this.S.GetHashCode();
                statements.Add(factory.ExpressionStatement(
                    factory.AssignmentStatement(hashCodeNameExpression,
                        factory.AddExpression(
                            factory.MultiplyExpression(hashCodeNameExpression, permuteValue),
                            ComputeHashValue(factory, compilation, member)))));
            }

            // And finally, the "return hashCode;" statement.
            statements.Add(factory.ReturnStatement(hashCodeNameExpression));

            return statements.ToImmutableAndFree();
        }

        private static SyntaxNode CreateLiteralExpression(SyntaxGenerator factory, int value)
            => value < 0
                ? factory.NegateExpression(factory.LiteralExpression(-value))
                : factory.LiteralExpression(value);

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
            var primitiveValue = IsPrimitiveValueType(memberType) && memberType.SpecialType != SpecialType.System_String;
            var isTupleType = memberType?.IsTupleType == true;
            if (primitiveValue || isTupleType)
            {
                return factory.InvocationExpression(
                    factory.MemberAccessExpression(thisSymbol, nameof(object.GetHashCode)));
            }
            else
            {
                return factory.InvocationExpression(
                    factory.MemberAccessExpression(
                        GetDefaultEqualityComparer(factory, compilation, GetType(compilation, member)),
                        getHashCodeNameExpression),
                    thisSymbol);
            }
        }
    }
}
