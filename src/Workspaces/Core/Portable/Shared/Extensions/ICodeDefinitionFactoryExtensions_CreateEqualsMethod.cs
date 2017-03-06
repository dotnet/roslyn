// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class ICodeDefinitionFactoryExtensions
    {
        private const string EqualsName = "Equals";
        private const string DefaultName = "Default";
        private const string ObjName = "obj";

        public static IMethodSymbol CreateEqualsMethod(
            this SyntaxGenerator factory,
            Compilation compilation,
            INamedTypeSymbol containingType,
            ImmutableArray<ISymbol> symbols,
            SyntaxAnnotation statementAnnotation,
            CancellationToken cancellationToken)
        {
            var statements = CreateEqualsMethodStatements(
                factory, compilation, containingType, symbols, cancellationToken);
            statements = statements.SelectAsArray(s => s.WithAdditionalAnnotations(statementAnnotation));

            return CodeGenerationSymbolFactory.CreateMethodSymbol(
                attributes: default(ImmutableArray<AttributeData>),
                accessibility: Accessibility.Public,
                modifiers: new DeclarationModifiers(isOverride: true),
                returnType: compilation.GetSpecialType(SpecialType.System_Boolean),
                returnsByRef: false,
                explicitInterfaceSymbol: null,
                name: EqualsName,
                typeParameters: default(ImmutableArray<ITypeParameterSymbol>),
                parameters: ImmutableArray.Create(CodeGenerationSymbolFactory.CreateParameterSymbol(compilation.GetSpecialType(SpecialType.System_Object), ObjName)),
                statements: statements);
        }

        private static ImmutableArray<SyntaxNode> CreateEqualsMethodStatements(
            SyntaxGenerator factory,
            Compilation compilation,
            INamedTypeSymbol containingType,
            IEnumerable<ISymbol> members,
            CancellationToken cancellationToken)
        {
            var iequatableType = compilation.GetTypeByMetadataName("System.IEquatable`1");
            var statements = ArrayBuilder<SyntaxNode>.GetInstance();

            // Come up with a good name for the local variable we're going to compare against.
            // For example, if the class name is "CustomerOrder" then we'll generate:
            //
            //      var order = obj as CustomerOrder;

            var parts = StringBreaker.BreakIntoWordParts(containingType.Name);
            var localName = "v";
            for (var i = parts.Count - 1; i >= 0; i--)
            {
                var p = parts[i];
                if (char.IsLetter(containingType.Name[p.Start]))
                {
                    localName = containingType.Name.Substring(p.Start, p.Length).ToCamelCase();
                    break;
                }
            }

            var localNameExpression = factory.IdentifierName(localName);

            var objNameExpression = factory.IdentifierName(ObjName);

            // These will be all the expressions that we'll '&&' together inside the final
            // return statement of 'Equals'.
            var expressions = new List<SyntaxNode>();

            if (containingType.IsValueType)
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

                var localDeclaration = factory.LocalDeclarationStatement(localName, factory.CastExpression(containingType, objNameExpression));

                statements.Add(ifStatement);
                statements.Add(localDeclaration);
            }
            else
            {
                // It's not a value type, we can just use "as" to test the parameter is the right type:
                //
                //      var myType = obj as MyType;

                var localDeclaration = factory.LocalDeclarationStatement(localName, factory.TryCastExpression(objNameExpression, containingType));

                statements.Add(localDeclaration);

                // Ensure that the parameter we got was not null (which also ensures the 'as' test
                // succeeded):
                //
                //      myType != null
                expressions.Add(factory.ReferenceNotEqualsExpression(localNameExpression, factory.NullLiteralExpression()));
                if (HasExistingBaseEqualsMethod(containingType, cancellationToken))
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
            }

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

                if (IsPrimitiveValueType(memberType))
                {
                    // If we have one of the well known primitive types, then just use '==' to compare
                    // the values.
                    //
                    //      this.a == other.a
                    var expression = factory.ValueEqualsExpression(thisSymbol, otherSymbol);
                    expressions.Add(expression);
                }
                else if (memberType?.IsValueType == true &&
                         ImplementsIEquatable(memberType, iequatableType))
                {
                    // If it's a value type and implements IEquatable<T>, then just call directly
                    // into .Equals.  This keeps the code simple and avoids an unnecessary null
                    // check
                    //
                    //      this.a.Equals(other.a)
                    var expression = factory.InvocationExpression(
                        factory.MemberAccessExpression(thisSymbol, nameof(object.Equals)),
                        otherSymbol);
                    expressions.Add(expression);
                }
                else
                {
                    // Otherwise call EqualityComparer<SType>.Default.Equals(this.a, other.a).
                    // This will do the appropriate null checks as well as calling directly
                    // into IEquatable<T>.Equals implementations if avaliable.
                    var expression =
                        factory.InvocationExpression(
                            factory.MemberAccessExpression(
                                GetDefaultEqualityComparer(factory, compilation, member),
                                factory.IdentifierName(EqualsName)),
                            thisSymbol,
                            otherSymbol);

                    expressions.Add(expression);
                }
            }

            // Now combine all the comparison expressions together into one final statement like:
            //
            //      return myType != null &&
            //             base.Equals(obj) &&
            //             this.S1 == myType.S1;
            statements.Add(factory.ReturnStatement(
                expressions.Aggregate(factory.LogicalAndExpression)));

            return statements.ToImmutableAndFree();
        }

        private static bool ImplementsIEquatable(ITypeSymbol memberType, INamedTypeSymbol iequatableType)
        {
            if (iequatableType != null)
            {
                var constructed = iequatableType.Construct(memberType);
                return memberType.AllInterfaces.Contains(constructed);
            }

            return false;
        }

        private static bool IsPrimitiveValueType(ITypeSymbol typeSymbol)
        {
            if (typeSymbol != null)
            {
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
                    case SpecialType.System_Nullable_T:
                    case SpecialType.System_DateTime:
                        return true;
                }
            }

            return false;
        }

        private static SyntaxNode GetDefaultEqualityComparer(
            SyntaxGenerator factory,
            Compilation compilation,
            ISymbol member)
        {
            var equalityComparerType = compilation.EqualityComparerOfTType();
            var constructedType = equalityComparerType.Construct(GetType(compilation, member));
            return factory.MemberAccessExpression(
                factory.TypeExpression(constructedType),
                factory.IdentifierName(DefaultName));
        }

        private static ITypeSymbol GetType(Compilation compilation, ISymbol symbol)
        {
            switch (symbol)
            {
                case IFieldSymbol field: return field.Type;
                case IPropertySymbol property: return property.Type;
                default: return compilation.GetSpecialType(SpecialType.System_Object);
            }
        }

        private static bool HasExistingBaseEqualsMethod(INamedTypeSymbol containingType, CancellationToken cancellationToken)
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
                      method.Parameters[0].Type.SpecialType == SpecialType.System_Object
                select method;

            return existingMethods.Any();
        }
    }
}
