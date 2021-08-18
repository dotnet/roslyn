﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class SyntaxGeneratorExtensions
    {
        public static IMethodSymbol CreateEqualsMethod(
            this SyntaxGenerator factory,
            Compilation compilation,
            ParseOptions parseOptions,
            INamedTypeSymbol containingType,
            ImmutableArray<ISymbol> symbols,
            string localNameOpt,
            SyntaxAnnotation statementAnnotation)
        {
            var statements = CreateEqualsMethodStatements(
                factory, compilation, parseOptions, containingType, symbols, localNameOpt);
            statements = statements.SelectAsArray(s => s.WithAdditionalAnnotations(statementAnnotation));

            return CreateEqualsMethod(compilation, statements);
        }

        public static IMethodSymbol CreateEqualsMethod(this Compilation compilation, ImmutableArray<SyntaxNode> statements)
        {
            return CodeGenerationSymbolFactory.CreateMethodSymbol(
                attributes: default,
                accessibility: Accessibility.Public,
                modifiers: new DeclarationModifiers(isOverride: true),
                returnType: compilation.GetSpecialType(SpecialType.System_Boolean),
                refKind: RefKind.None,
                explicitInterfaceImplementations: default,
                name: EqualsName,
                typeParameters: default,
                parameters: ImmutableArray.Create(CodeGenerationSymbolFactory.CreateParameterSymbol(compilation.GetSpecialType(SpecialType.System_Object).WithNullableAnnotation(NullableAnnotation.Annotated), ObjName)),
                statements: statements);
        }

        public static IMethodSymbol CreateIEquatableEqualsMethod(
            this SyntaxGenerator factory,
            SemanticModel semanticModel,
            INamedTypeSymbol containingType,
            ImmutableArray<ISymbol> symbols,
            INamedTypeSymbol constructedEquatableType,
            SyntaxAnnotation statementAnnotation)
        {
            var statements = CreateIEquatableEqualsMethodStatements(
                factory, semanticModel.Compilation, containingType, symbols);
            statements = statements.SelectAsArray(s => s.WithAdditionalAnnotations(statementAnnotation));

            var methodSymbol = constructedEquatableType
                .GetMembers(EqualsName)
                .OfType<IMethodSymbol>()
                .Single(m => containingType.Equals(m.Parameters.FirstOrDefault()?.Type));

            var originalParameter = methodSymbol.Parameters.First();

            // Replace `[AllowNull] Foo` with `Foo` or `Foo?` (no longer needed after https://github.com/dotnet/roslyn/issues/39256?)
            var parameters = ImmutableArray.Create(CodeGenerationSymbolFactory.CreateParameterSymbol(
                originalParameter,
                type: constructedEquatableType.GetTypeArguments()[0],
                attributes: ImmutableArray<AttributeData>.Empty));

            if (factory.RequiresExplicitImplementationForInterfaceMembers)
            {
                return CodeGenerationSymbolFactory.CreateMethodSymbol(
                    methodSymbol,
                    modifiers: new DeclarationModifiers(),
                    explicitInterfaceImplementations: ImmutableArray.Create(methodSymbol),
                    parameters: parameters,
                    statements: statements);
            }
            else
            {
                return CodeGenerationSymbolFactory.CreateMethodSymbol(
                    methodSymbol,
                    modifiers: new DeclarationModifiers(),
                    parameters: parameters,
                    statements: statements);
            }
        }

        private static ImmutableArray<SyntaxNode> CreateEqualsMethodStatements(
            SyntaxGenerator factory,
            Compilation compilation,
            ParseOptions parseOptions,
            INamedTypeSymbol containingType,
            ImmutableArray<ISymbol> members,
            string localNameOpt)
        {
            using var _1 = ArrayBuilder<SyntaxNode>.GetInstance(out var statements);

            // A ref like type can not be boxed. Because of this an overloaded Equals taking object in the general case
            // can never be true, because an equivalent object can never be boxed into the object itself. Therefore only
            // need to return false.
            if (containingType.IsRefLikeType)
            {
                statements.Add(factory.ReturnStatement(factory.FalseLiteralExpression()));
                return statements.ToImmutable();
            }

            // Come up with a good name for the local variable we're going to compare against.
            // For example, if the class name is "CustomerOrder" then we'll generate:
            //
            //      var order = obj as CustomerOrder;

            var localName = localNameOpt ?? GetLocalName(containingType);

            var localNameExpression = factory.IdentifierName(localName);
            var objNameExpression = factory.IdentifierName(ObjName);

            // These will be all the expressions that we'll '&&' together inside the final
            // return statement of 'Equals'.
            using var _2 = ArrayBuilder<SyntaxNode>.GetInstance(out var expressions);

            if (factory.SyntaxGeneratorInternal.SupportsPatterns(parseOptions))
            {
                // If we support patterns then we can do "return obj is MyType myType && ..."
                expressions.Add(
                    factory.SyntaxGeneratorInternal.IsPatternExpression(objNameExpression,
                        factory.SyntaxGeneratorInternal.DeclarationPattern(containingType, localName)));
            }
            else if (containingType.IsValueType)
            {
                // If we're a value type, then we need an is-check first to make sure
                // the object is our type:
                //
                //      if (!(obj is MyType))
                //      {
                //          return false;
                //      }
                var ifStatement = factory.IfStatement(
                    factory.LogicalNotExpression(
                        factory.IsTypeExpression(
                            objNameExpression,
                            containingType)),
                    new[] { factory.ReturnStatement(factory.FalseLiteralExpression()) });

                // Next, we cast the argument to our type:
                //
                //      var myType = (MyType)obj;

                var localDeclaration = factory.SimpleLocalDeclarationStatement(factory.SyntaxGeneratorInternal,
                    containingType, localName, factory.CastExpression(containingType, objNameExpression));

                statements.Add(ifStatement);
                statements.Add(localDeclaration);
            }
            else
            {
                // It's not a value type, we can just use "as" to test the parameter is the right type:
                //
                //      var myType = obj as MyType;

                var localDeclaration = factory.SimpleLocalDeclarationStatement(factory.SyntaxGeneratorInternal,
                    containingType, localName, factory.TryCastExpression(objNameExpression, containingType));

                statements.Add(localDeclaration);

                // Ensure that the parameter we got was not null (which also ensures the 'as' test
                // succeeded):
                //
                //      myType != null
                expressions.Add(factory.ReferenceNotEqualsExpression(localNameExpression, factory.NullLiteralExpression()));
            }

            if (!containingType.IsValueType && HasExistingBaseEqualsMethod(containingType))
            {
                // If we're overriding something that also provided an overridden 'Equals',
                // then ensure the base type thinks it is equals as well.
                //
                //      base.Equals(obj)
                expressions.Add(factory.InvocationExpression(
                    factory.MemberAccessExpression(
                        factory.BaseExpression(),
                        factory.IdentifierName(EqualsName)),
                    objNameExpression));
            }

            AddMemberChecks(factory, compilation, members, localNameExpression, expressions);

            // Now combine all the comparison expressions together into one final statement like:
            //
            //      return myType != null &&
            //             base.Equals(obj) &&
            //             this.S1 == myType.S1;
            statements.Add(factory.ReturnStatement(
                expressions.Aggregate(factory.LogicalAndExpression)));

            return statements.ToImmutable();
        }

        private static void AddMemberChecks(
            SyntaxGenerator factory, Compilation compilation,
            ImmutableArray<ISymbol> members, SyntaxNode localNameExpression,
            ArrayBuilder<SyntaxNode> expressions)
        {
            var iequatableType = compilation.GetTypeByMetadataName(typeof(IEquatable<>).FullName);

            // Now, iterate over all the supplied members and ensure that our instance
            // and the parameter think they are equals.  Specialize how we do this for
            // common types.  Fall-back to EqualityComparer<SType>.Default.Equals for
            // everything else.
            foreach (var member in members)
            {
                var symbolNameExpression = factory.IdentifierName(member.Name);
                var thisSymbol = factory.MemberAccessExpression(factory.ThisExpression(), symbolNameExpression)
                                        .WithAdditionalAnnotations(Simplification.Simplifier.Annotation);
                var otherSymbol = factory.MemberAccessExpression(localNameExpression, symbolNameExpression);

                var memberType = member.GetSymbolType();

                if (ShouldUseEqualityOperator(memberType))
                {
                    expressions.Add(factory.ValueEqualsExpression(thisSymbol, otherSymbol));
                    continue;
                }

                var valueIEquatable = memberType?.IsValueType == true && ImplementsIEquatable(memberType, iequatableType);
                if (valueIEquatable || memberType?.IsTupleType == true)
                {
                    // If it's a value type and implements IEquatable<T>, Or if it's a tuple, then
                    // just call directly into .Equals. This keeps the code simple and avoids an
                    // unnecessary null check.
                    //
                    //      this.a.Equals(other.a)
                    expressions.Add(factory.InvocationExpression(
                        factory.MemberAccessExpression(thisSymbol, nameof(object.Equals)),
                        otherSymbol));
                    continue;
                }

                // Otherwise call EqualityComparer<SType>.Default.Equals(this.a, other.a).
                // This will do the appropriate null checks as well as calling directly
                // into IEquatable<T>.Equals implementations if available.

                expressions.Add(factory.InvocationExpression(
                        factory.MemberAccessExpression(
                            GetDefaultEqualityComparer(factory, compilation, GetType(compilation, member)),
                            factory.IdentifierName(EqualsName)),
                        thisSymbol,
                        otherSymbol));
            }
        }

        private static ImmutableArray<SyntaxNode> CreateIEquatableEqualsMethodStatements(
            SyntaxGenerator factory,
            Compilation compilation,
            INamedTypeSymbol containingType,
            ImmutableArray<ISymbol> members)
        {
            var statements = ArrayBuilder<SyntaxNode>.GetInstance();

            var otherNameExpression = factory.IdentifierName(OtherName);

            // These will be all the expressions that we'll '&&' together inside the final
            // return statement of 'Equals'.
            using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var expressions);

            if (!containingType.IsValueType)
            {
                // It's not a value type. Ensure that the parameter we got was not null.
                //
                //      other != null
                expressions.Add(factory.ReferenceNotEqualsExpression(otherNameExpression, factory.NullLiteralExpression()));
                if (HasExistingBaseEqualsMethod(containingType))
                {
                    // If we're overriding something that also provided an overridden 'Equals',
                    // then ensure the base type thinks it is equals as well.
                    //
                    //      base.Equals(obj)
                    expressions.Add(factory.InvocationExpression(
                        factory.MemberAccessExpression(
                            factory.BaseExpression(),
                            factory.IdentifierName(EqualsName)),
                        otherNameExpression));
                }
            }

            AddMemberChecks(factory, compilation, members, otherNameExpression, expressions);

            // Now combine all the comparison expressions together into one final statement like:
            //
            //      return other != null &&
            //             base.Equals(other) &&
            //             this.S1 == other.S1;
            statements.Add(factory.ReturnStatement(
                expressions.Aggregate(factory.LogicalAndExpression)));

            return statements.ToImmutableAndFree();
        }

        public static string GetLocalName(this ITypeSymbol containingType)
        {
            var name = containingType.Name;
            if (name.Length > 0)
            {
                var parts = StringBreaker.GetWordParts(name);
                for (var i = parts.Count - 1; i >= 0; i--)
                {
                    var p = parts[i];
                    if (p.Length > 0 && char.IsLetter(name[p.Start]))
                    {
                        return name.Substring(p.Start, p.Length).ToCamelCase();
                    }
                }
            }

            return "v";
        }

        private static bool ImplementsIEquatable(ITypeSymbol memberType, INamedTypeSymbol iequatableType)
        {
            if (iequatableType != null)
            {
                // We compare ignoring nested nullability here, as it's possible the underlying object could have implemented IEquatable<Type>
                // or IEquatable<Type?>. From the perspective of this, either is allowable.
                var constructed = iequatableType.Construct(memberType);
                return memberType.AllInterfaces.Contains(constructed, equalityComparer: SymbolEqualityComparer.Default);
            }

            return false;
        }

        private static bool ShouldUseEqualityOperator(ITypeSymbol typeSymbol)
        {
            if (typeSymbol != null)
            {
                if (typeSymbol.IsNullable(out var underlyingType))
                {
                    typeSymbol = underlyingType;
                }

                if (typeSymbol.IsEnumType())
                {
                    return true;
                }

                switch (typeSymbol.SpecialType)
                {
                    case SpecialType.System_Boolean:
                    case SpecialType.System_Char:
                    case SpecialType.System_SByte:
                    case SpecialType.System_Byte:
                    case SpecialType.System_Int16:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_UInt64:
                    case SpecialType.System_Decimal:
                    case SpecialType.System_Single:
                    case SpecialType.System_Double:
                    case SpecialType.System_String:
                    case SpecialType.System_DateTime:
                        return true;
                }
            }

            return false;
        }

        private static bool HasExistingBaseEqualsMethod(INamedTypeSymbol containingType)
        {
            // Check if any of our base types override Equals.  If so, first check with them.
            var existingMethods =
                from baseType in containingType.GetBaseTypes()
                from method in baseType.GetMembers(EqualsName).OfType<IMethodSymbol>()
                where method.IsOverride &&
                      method.DeclaredAccessibility == Accessibility.Public &&
                      !method.IsStatic &&
                      method.Parameters.Length == 1 &&
                      method.ReturnType.SpecialType == SpecialType.System_Boolean &&
                      method.Parameters[0].Type.SpecialType == SpecialType.System_Object &&
                      !method.IsAbstract
                select method;

            return existingMethods.Any();
        }
    }
}
