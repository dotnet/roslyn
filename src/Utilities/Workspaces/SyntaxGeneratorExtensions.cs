// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace Analyzer.Utilities
{
    internal static class SyntaxGeneratorExtensions
    {
        private const string LeftIdentifierName = "left";
        private const string RightIdentifierName = "right";
        private const string ReferenceEqualsMethodName = "ReferenceEquals";
        private const string EqualsMethodName = "Equals";
        private const string CompareToMethodName = "CompareTo";
        private const string SystemNotImplementedExceptionTypeName = "System.NotImplementedException";

        /// <summary>
        /// Creates a default declaration for an operator equality overload.
        /// </summary>
        /// <param name="generator">
        /// The <see cref="SyntaxGenerator"/> used to create the declaration.
        /// </param>
        /// <param name="containingType">
        /// A symbol specifying the type of the operands of the comparison operator.
        /// </param>
        /// <returns>
        /// A <see cref="SyntaxNode"/> representing the declaration.
        /// </returns>
        public static SyntaxNode DefaultOperatorEqualityDeclaration(this SyntaxGenerator generator,
            INamedTypeSymbol containingType)
        {
            var leftArgument = generator.IdentifierName(LeftIdentifierName);
            var rightArgument = generator.IdentifierName(RightIdentifierName);

            List<SyntaxNode> statements = new List<SyntaxNode>();

            if (containingType.TypeKind == TypeKind.Class)
            {
                statements.Add(
                    generator.IfStatement(
                        generator.InvocationExpression(
                            generator.IdentifierName(ReferenceEqualsMethodName),
                            leftArgument,
                            generator.NullLiteralExpression()),
                        new[]
                        {
                            generator.ReturnStatement(
                                generator.InvocationExpression(
                                    generator.IdentifierName(ReferenceEqualsMethodName),
                                    rightArgument,
                                    generator.NullLiteralExpression()))
                        }));
            }

            statements.Add(
                generator.ReturnStatement(
                    generator.InvocationExpression(
                        generator.MemberAccessExpression(
                            leftArgument, EqualsMethodName),
                        rightArgument)));

            return generator.ComparisonOperatorDeclaration(OperatorKind.Equality, containingType, statements.ToArray());
        }

        /// <summary>
        /// Creates a reference to a named type suitable for use in accessing a static member of the type.
        /// </summary>
        /// <param name="generator">The <see cref="SyntaxGenerator"/> used to create the type reference.</param>
        /// <param name="typeSymbol">The named type to reference.</param>
        /// <returns>A <see cref="SyntaxNode"/> representing the type reference expression.</returns>
        public static SyntaxNode TypeExpressionForStaticMemberAccess(this SyntaxGenerator generator, INamedTypeSymbol typeSymbol)
        {
            var qualifiedNameSyntaxKind = generator.QualifiedName(generator.IdentifierName("ignored"), generator.IdentifierName("ignored")).RawKind;
            var memberAccessExpressionSyntaxKind = generator.MemberAccessExpression(generator.IdentifierName("ignored"), "ignored").RawKind;

            var typeExpression = generator.TypeExpression(typeSymbol);
            return QualifiedNameToMemberAccess(qualifiedNameSyntaxKind, memberAccessExpressionSyntaxKind, typeExpression, generator);

            // Local function
            static SyntaxNode QualifiedNameToMemberAccess(int qualifiedNameSyntaxKind, int memberAccessExpressionSyntaxKind, SyntaxNode expression, SyntaxGenerator generator)
            {
                if (expression.RawKind == qualifiedNameSyntaxKind)
                {
                    var left = QualifiedNameToMemberAccess(qualifiedNameSyntaxKind, memberAccessExpressionSyntaxKind, expression.ChildNodes().First(), generator);
                    var right = expression.ChildNodes().Last();
                    return generator.MemberAccessExpression(left, right);
                }

                return expression;
            }
        }

        /// <summary>
        /// Creates a default declaration for an operator inequality overload.
        /// </summary>
        /// <param name="generator">
        /// The <see cref="SyntaxGenerator"/> used to create the declaration.
        /// </param>
        /// <param name="containingType">
        /// A symbol specifying the type of the operands of the comparison operator.
        /// </param>
        /// <returns>
        /// A <see cref="SyntaxNode"/> representing the declaration.
        /// </returns>
        public static SyntaxNode DefaultOperatorInequalityDeclaration(this SyntaxGenerator generator, INamedTypeSymbol containingType)
        {
            var leftArgument = generator.IdentifierName(LeftIdentifierName);
            var rightArgument = generator.IdentifierName(RightIdentifierName);

            var returnStatement = generator.ReturnStatement(
                    generator.LogicalNotExpression(
                        generator.ValueEqualsExpression(
                            leftArgument,
                            rightArgument)));

            return generator.ComparisonOperatorDeclaration(OperatorKind.Inequality, containingType, returnStatement);
        }

        /// <summary>
        /// Creates a default declaration for an operator less than overload.
        /// </summary>
        /// <param name="generator">
        /// The <see cref="SyntaxGenerator"/> used to create the declaration.
        /// </param>
        /// <param name="containingType">
        /// A symbol specifying the type of the operands of the comparison operator.
        /// </param>
        /// <returns>
        /// A <see cref="SyntaxNode"/> representing the declaration.
        /// </returns>
        public static SyntaxNode DefaultOperatorLessThanDeclaration(this SyntaxGenerator generator, INamedTypeSymbol containingType)
        {
            var leftArgument = generator.IdentifierName(LeftIdentifierName);
            var rightArgument = generator.IdentifierName(RightIdentifierName);

            SyntaxNode expression;

            if (containingType.TypeKind == TypeKind.Class)
            {
                expression =
                    generator.ConditionalExpression(
                        generator.InvocationExpression(
                            generator.IdentifierName(ReferenceEqualsMethodName),
                            leftArgument,
                            generator.NullLiteralExpression()),
                        generator.LogicalNotExpression(
                            generator.InvocationExpression(
                                generator.IdentifierName(ReferenceEqualsMethodName),
                                rightArgument,
                                generator.NullLiteralExpression())),
                        generator.LessThanExpression(
                            generator.InvocationExpression(
                                generator.MemberAccessExpression(leftArgument, generator.IdentifierName(CompareToMethodName)),
                                rightArgument),
                            generator.LiteralExpression(0)));
            }
            else
            {
                expression =
                    generator.LessThanExpression(
                        generator.InvocationExpression(
                            generator.MemberAccessExpression(leftArgument, generator.IdentifierName(CompareToMethodName)),
                            rightArgument),
                        generator.LiteralExpression(0));
            }

            var returnStatement = generator.ReturnStatement(expression);
            return generator.ComparisonOperatorDeclaration(OperatorKind.LessThan, containingType, returnStatement);
        }

        /// <summary>
        /// Creates a default declaration for an operator less than or equal overload.
        /// </summary>
        /// <param name="generator">
        /// The <see cref="SyntaxGenerator"/> used to create the declaration.
        /// </param>
        /// <param name="containingType">
        /// A symbol specifying the type of the operands of the comparison operator.
        /// </param>
        /// <returns>
        /// A <see cref="SyntaxNode"/> representing the declaration.
        /// </returns>
        public static SyntaxNode DefaultOperatorLessThanOrEqualDeclaration(this SyntaxGenerator generator, INamedTypeSymbol containingType)
        {
            var leftArgument = generator.IdentifierName(LeftIdentifierName);
            var rightArgument = generator.IdentifierName(RightIdentifierName);

            SyntaxNode expression;

            if (containingType.TypeKind == TypeKind.Class)
            {
                expression =
                    generator.LogicalOrExpression(
                        generator.InvocationExpression(
                            generator.IdentifierName(ReferenceEqualsMethodName),
                            leftArgument,
                            generator.NullLiteralExpression()),
                        generator.LessThanOrEqualExpression(
                            generator.InvocationExpression(
                                generator.MemberAccessExpression(leftArgument, generator.IdentifierName(CompareToMethodName)),
                                rightArgument),
                            generator.LiteralExpression(0)));
            }
            else
            {
                expression =
                    generator.LessThanOrEqualExpression(
                        generator.InvocationExpression(
                            generator.MemberAccessExpression(leftArgument, generator.IdentifierName(CompareToMethodName)),
                            rightArgument),
                        generator.LiteralExpression(0));
            }

            var returnStatement = generator.ReturnStatement(expression);
            return generator.ComparisonOperatorDeclaration(OperatorKind.LessThanOrEqual, containingType, returnStatement);
        }

        /// <summary>
        /// Creates a default declaration for an operator greater than overload.
        /// </summary>
        /// <param name="generator">
        /// The <see cref="SyntaxGenerator"/> used to create the declaration.
        /// </param>
        /// <param name="containingType">
        /// A symbol specifying the type of the operands of the comparison operator.
        /// </param>
        /// <returns>
        /// A <see cref="SyntaxNode"/> representing the declaration.
        /// </returns>
        public static SyntaxNode DefaultOperatorGreaterThanDeclaration(this SyntaxGenerator generator, INamedTypeSymbol containingType)
        {
            var leftArgument = generator.IdentifierName(LeftIdentifierName);
            var rightArgument = generator.IdentifierName(RightIdentifierName);

            SyntaxNode expression;

            if (containingType.TypeKind == TypeKind.Class)
            {
                expression =
                    generator.LogicalAndExpression(
                        generator.LogicalNotExpression(
                            generator.InvocationExpression(
                                generator.IdentifierName(ReferenceEqualsMethodName),
                                leftArgument,
                                generator.NullLiteralExpression())),
                        generator.GreaterThanExpression(
                            generator.InvocationExpression(
                                generator.MemberAccessExpression(leftArgument, generator.IdentifierName(CompareToMethodName)),
                                rightArgument),
                            generator.LiteralExpression(0)));
            }
            else
            {
                expression =
                    generator.GreaterThanExpression(
                        generator.InvocationExpression(
                            generator.MemberAccessExpression(leftArgument, generator.IdentifierName(CompareToMethodName)),
                            rightArgument),
                        generator.LiteralExpression(0));
            }

            var returnStatement = generator.ReturnStatement(expression);
            return generator.ComparisonOperatorDeclaration(OperatorKind.GreaterThan, containingType, returnStatement);
        }

        /// <summary>
        /// Creates a default declaration for an operator greater than or equal overload.
        /// </summary>
        /// <param name="generator">
        /// The <see cref="SyntaxGenerator"/> used to create the declaration.
        /// </param>
        /// <param name="containingType">
        /// A symbol specifying the type of the operands of the comparison operator.
        /// </param>
        /// <returns>
        /// A <see cref="SyntaxNode"/> representing the declaration.
        /// </returns>
        public static SyntaxNode DefaultOperatorGreaterThanOrEqualDeclaration(this SyntaxGenerator generator, INamedTypeSymbol containingType)
        {
            var leftArgument = generator.IdentifierName(LeftIdentifierName);
            var rightArgument = generator.IdentifierName(RightIdentifierName);

            SyntaxNode expression;

            if (containingType.TypeKind == TypeKind.Class)
            {
                expression =
                    generator.ConditionalExpression(
                            generator.InvocationExpression(
                                generator.IdentifierName(ReferenceEqualsMethodName),
                                leftArgument,
                                generator.NullLiteralExpression()),
                            generator.InvocationExpression(
                                generator.IdentifierName(ReferenceEqualsMethodName),
                                rightArgument,
                                generator.NullLiteralExpression()),
                        generator.GreaterThanOrEqualExpression(
                            generator.InvocationExpression(
                                generator.MemberAccessExpression(leftArgument, generator.IdentifierName(CompareToMethodName)),
                                rightArgument),
                            generator.LiteralExpression(0)));
            }
            else
            {
                expression =
                    generator.GreaterThanOrEqualExpression(
                        generator.InvocationExpression(
                            generator.MemberAccessExpression(leftArgument, generator.IdentifierName(CompareToMethodName)),
                            rightArgument),
                        generator.LiteralExpression(0));
            }

            var returnStatement = generator.ReturnStatement(expression);
            return generator.ComparisonOperatorDeclaration(OperatorKind.GreaterThanOrEqual, containingType, returnStatement);
        }

        private static SyntaxNode ComparisonOperatorDeclaration(this SyntaxGenerator generator, OperatorKind operatorKind, INamedTypeSymbol containingType, params SyntaxNode[] statements)
        {
            return generator.OperatorDeclaration(
                operatorKind,
                new[]
                {
                    generator.ParameterDeclaration(LeftIdentifierName, generator.TypeExpression(containingType)),
                    generator.ParameterDeclaration(RightIdentifierName, generator.TypeExpression(containingType))
                },
                generator.TypeExpression(SpecialType.System_Boolean),
                Accessibility.Public,
                DeclarationModifiers.Static,
                statements);
        }

        /// <summary>
        /// Creates a default declaration for an override of <see cref="object.Equals(object)"/>.
        /// </summary>
        /// <param name="generator">
        /// The <see cref="SyntaxGenerator"/> used to create the declaration.
        /// </param>
        /// <param name="compilation">The compilation</param>
        /// <param name="containingType">
        /// A symbol specifying the type in which the declaration is to be created.
        /// </param>
        /// <returns>
        /// A <see cref="SyntaxNode"/> representing the declaration.
        /// </returns>
        public static SyntaxNode DefaultEqualsOverrideDeclaration(this SyntaxGenerator generator, Compilation compilation, INamedTypeSymbol containingType)
        {
            var argumentName = generator.IdentifierName("obj");

            List<SyntaxNode> statements = new List<SyntaxNode>();

            if (containingType.TypeKind == TypeKind.Class)
            {
                statements.AddRange(new[]
                {
                    generator.IfStatement(
                        generator.InvocationExpression(
                            generator.IdentifierName(ReferenceEqualsMethodName),
                            generator.ThisExpression(),
                            argumentName),
                        new[]
                        {
                            generator.ReturnStatement(generator.TrueLiteralExpression())
                        }),
                    generator.IfStatement(
                        generator.InvocationExpression(
                            generator.IdentifierName(ReferenceEqualsMethodName),
                            argumentName,
                            generator.NullLiteralExpression()),
                        new[]
                        {
                            generator.ReturnStatement(generator.FalseLiteralExpression())
                        })
                });
            }

            statements.AddRange(generator.DefaultMethodBody(compilation));

            return generator.MethodDeclaration(
                WellKnownMemberNames.ObjectEquals,
                new[]
                {
                    generator.ParameterDeclaration(argumentName.ToString(), generator.TypeExpression(SpecialType.System_Object))
                },
                returnType: generator.TypeExpression(SpecialType.System_Boolean),
                accessibility: Accessibility.Public,
                modifiers: DeclarationModifiers.Override,
                statements: statements);
        }

        /// <summary>
        /// Creates a default declaration for an override of <see cref="object.GetHashCode()"/>.
        /// </summary>
        /// <param name="generator">
        /// The <see cref="SyntaxGenerator"/> used to create the declaration.
        /// </param>
        /// <param name="compilation">The compilation</param>
        /// <returns>
        /// A <see cref="SyntaxNode"/> representing the declaration.
        /// </returns>
        public static SyntaxNode DefaultGetHashCodeOverrideDeclaration(
            this SyntaxGenerator generator, Compilation compilation)
        {
            return generator.MethodDeclaration(
                WellKnownMemberNames.ObjectGetHashCode,
                returnType: generator.TypeExpression(SpecialType.System_Int32),
                accessibility: Accessibility.Public,
                modifiers: DeclarationModifiers.Override,
                statements: generator.DefaultMethodBody(compilation));
        }

        /// <summary>
        /// Creates a default set of statements to place within a generated method body.
        /// </summary>
        /// <param name="generator">
        /// The <see cref="SyntaxGenerator"/> used to create the statements.
        /// </param>
        /// <param name="compilation">The compilation</param>
        /// <returns>
        /// An sequence containing a single statement that throws <see cref="System.NotImplementedException"/>.
        /// </returns>
        public static IEnumerable<SyntaxNode> DefaultMethodBody(
            this SyntaxGenerator generator, Compilation compilation)
        {
            yield return DefaultMethodStatement(generator, compilation);
        }

        public static SyntaxNode DefaultMethodStatement(this SyntaxGenerator generator, Compilation compilation)
        {
            return generator.ThrowStatement(generator.ObjectCreationExpression(
                generator.TypeExpression(
                    compilation.GetOrCreateTypeByMetadataName(SystemNotImplementedExceptionTypeName))));
        }
    }
}