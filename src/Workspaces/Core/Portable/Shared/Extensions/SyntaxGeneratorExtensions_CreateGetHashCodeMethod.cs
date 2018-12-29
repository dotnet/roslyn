﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class SyntaxGeneratorExtensions
    {
        private const string GetHashCodeName = nameof(object.GetHashCode);

        public static ImmutableArray<SyntaxNode> GetGetHashCodeComponents(
            this SyntaxGenerator factory,
            Compilation compilation,
            INamedTypeSymbol containingType,
            ImmutableArray<ISymbol> members,
            bool justMemberReference,
            CancellationToken cancellationToken)
        {
            var result = ArrayBuilder<SyntaxNode>.GetInstance();

            if (GetBaseGetHashCodeMethod(containingType, cancellationToken) != null)
            {
                result.Add(factory.InvocationExpression(
                    factory.MemberAccessExpression(factory.BaseExpression(), GetHashCodeName)));
            }

            foreach (var member in members)
            {
                result.Add(GetMemberForGetHashCode(factory, compilation, member, justMemberReference));
            }

            return result.ToImmutableAndFree();
        }

        /// <summary>
        /// Generates an override of <see cref="object.GetHashCode()"/> similar to the one
        /// generated for anonymous types.
        /// </summary>
        public static ImmutableArray<SyntaxNode> CreateGetHashCodeMethodStatements(
            this SyntaxGenerator factory,
            Compilation compilation,
            INamedTypeSymbol containingType,
            ImmutableArray<ISymbol> members,
            bool useInt64,
            CancellationToken cancellationToken)
        {
            var components = GetGetHashCodeComponents(
                factory, compilation, containingType, members, justMemberReference: false, cancellationToken);

            if (components.Length == 0)
            {
                return ImmutableArray.Create(factory.ReturnStatement(factory.LiteralExpression(0)));
            }

            const int hashFactor = -1521134295;

            var initHash = 0;
            var baseHashCode = GetBaseGetHashCodeMethod(containingType, cancellationToken);
            if (baseHashCode != null)
            {
                initHash = initHash * hashFactor + Hash.GetFNVHashCode(baseHashCode.Name);
            }

            foreach (var symbol in members)
            {
                initHash = initHash * hashFactor + Hash.GetFNVHashCode(symbol.Name);
            }

            if (components.Length == 1 && !useInt64)
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
                        components[0])));
            }

            var statements = ArrayBuilder<SyntaxNode>.GetInstance();

            // initialize the initial hashCode:
            //
            //      var hashCode = initialHashCode;

            const string HashCodeName = "hashCode";
            statements.Add(!useInt64
                ? factory.LocalDeclarationStatement(HashCodeName, CreateLiteralExpression(factory, initHash))
                : factory.LocalDeclarationStatement(compilation.GetSpecialType(SpecialType.System_Int64), HashCodeName, CreateLiteralExpression(factory, initHash)));

            var hashCodeNameExpression = factory.IdentifierName(HashCodeName);

            // -1521134295
            var permuteValue = CreateLiteralExpression(factory, hashFactor);
            foreach (var component in components)
            {
                // hashCode = hashCode * -1521134295 + this.S.GetHashCode();
                var rightSide =
                    factory.AddExpression(
                        factory.MultiplyExpression(hashCodeNameExpression, permuteValue),
                        component);

                if (useInt64)
                {
                    rightSide = factory.InvocationExpression(
                        factory.MemberAccessExpression(rightSide, GetHashCodeName));
                }

                statements.Add(factory.ExpressionStatement(
                    factory.AssignmentStatement(hashCodeNameExpression, rightSide)));
            }

            // And finally, the "return hashCode;" statement.
            statements.Add(!useInt64
                ? factory.ReturnStatement(hashCodeNameExpression)
                : factory.ReturnStatement(
                    factory.ConvertExpression(
                        compilation.GetSpecialType(SpecialType.System_Int32),
                        hashCodeNameExpression)));

            return statements.ToImmutableAndFree();
        }

        private static SyntaxNode CreateLiteralExpression(SyntaxGenerator factory, int value)
            => value < 0
                ? factory.NegateExpression(factory.LiteralExpression(-value))
                : factory.LiteralExpression(value);

        public static IMethodSymbol GetBaseGetHashCodeMethod(INamedTypeSymbol containingType, CancellationToken cancellationToken)
        {
            if (containingType.IsValueType)
            {
                // Don't want to produce base.GetHashCode for a value type.  The point with value
                // types is to produce a good, fast, hash ourselves, avoiding the built in slow
                // one in System.ValueType.
                return null;
            }

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

            return existingMethods.FirstOrDefault();
        }

        private static SyntaxNode GetMemberForGetHashCode(
            SyntaxGenerator factory,
            Compilation compilation,
            ISymbol member,
            bool justMemberReference)
        {
            var getHashCodeNameExpression = factory.IdentifierName(GetHashCodeName);
            var thisSymbol = factory.MemberAccessExpression(factory.ThisExpression(),
                factory.IdentifierName(member.Name)).WithAdditionalAnnotations(Simplification.Simplifier.Annotation);

            // Caller only wanted the reference to the member, nothing else added.
            if (justMemberReference)
            {
                return thisSymbol;
            }

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
