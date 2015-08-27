// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.MethodXml;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel.MethodXml
{
    internal class MethodXmlBuilder : AbstractMethodXmlBuilder
    {
        private MethodXmlBuilder(IMethodSymbol symbol, SemanticModel semanticModel)
            : base(symbol, semanticModel)
        {
        }

        private void GenerateBlock(BlockSyntax block)
        {
            using (BlockTag())
            {
                foreach (var statement in block.Statements)
                {
                    GenerateComments(statement.GetLeadingTrivia());
                    GenerateStatement(statement);
                }

                // Handle any additional comments now, but only comments within the extent of this block.
                GenerateComments(block.CloseBraceToken.LeadingTrivia);
            }
        }

        private void GenerateComments(SyntaxTriviaList triviaList)
        {
            foreach (var trivia in triviaList)
            {
                // Multi-line comment forms are ignored.
                if (trivia.Kind() == SyntaxKind.SingleLineCommentTrivia)
                {
                    // In order to be valid, the comment must appear on its own line.
                    var line = Text.Lines.GetLineFromPosition(trivia.SpanStart);
                    var firstNonWhitespacePosition = line.GetFirstNonWhitespacePosition() ?? -1;
                    if (firstNonWhitespacePosition == trivia.SpanStart)
                    {
                        using (var tag = CommentTag())
                        {
                            // Skip initial slashes
                            var trimmedComment = trivia.ToString().Substring(2);
                            EncodedText(trimmedComment);
                        }
                    }
                }
            }
        }

        private void GenerateStatement(StatementSyntax statement)
        {
            var success = false;
            int mark = GetMark();

            switch (statement.Kind())
            {
                case SyntaxKind.LocalDeclarationStatement:
                    success = TryGenerateLocal((LocalDeclarationStatementSyntax)statement);
                    break;
                case SyntaxKind.Block:
                    success = true;
                    GenerateBlock((BlockSyntax)statement);
                    break;
                case SyntaxKind.ExpressionStatement:
                    success = TryGenerateExpressionStatement((ExpressionStatementSyntax)statement);
                    break;
            }

            if (!success)
            {
                Rewind(mark);
                GenerateUnknown(statement);
            }

            // Just for readability
            LineBreak();
        }

        private bool TryGenerateLocal(LocalDeclarationStatementSyntax localDeclarationStatement)
        {
            /*
              - <ElementType name="Local" content="eltOnly">
                    <attribute type="id" /> 
                    <attribute type="static" /> 
                    <attribute type="instance" /> 
                    <attribute type="implicit" /> 
                    <attribute type="constant" /> 
                - <group order="one">
                        <element type="Type" /> 
                        <element type="ArrayType" /> 
                    </group>
                - <group minOccurs="1" maxOccurs="*" order="seq">
                        <element type="LocalName" /> 
                        <element type="Expression" minOccurs="0" maxOccurs="1" /> 
                    </group>
                </ElementType>
            */

            using (LocalTag(GetLineNumber(localDeclarationStatement)))
            {
                // Spew the type first
                if (!TryGenerateType(localDeclarationStatement.Declaration.Type))
                {
                    return false;
                }

                // Now spew the list of variables
                foreach (var variable in localDeclarationStatement.Declaration.Variables)
                {
                    GenerateName(variable.Identifier.ToString());

                    if (variable.Initializer != null)
                    {
                        if (!TryGenerateExpression(variable.Initializer.Value))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private bool TryGenerateExpressionStatement(ExpressionStatementSyntax expressionStatement)
        {
            using (ExpressionStatementTag(GetLineNumber(expressionStatement)))
            {
                return TryGenerateExpression(expressionStatement.Expression);
            }
        }

        private bool TryGenerateType(TypeSyntax type)
        {
            var typeSymbol = SemanticModel.GetTypeInfo(type).Type;
            if (typeSymbol == null)
            {
                return false;
            }

            GenerateType(typeSymbol);
            return true;
        }

        private bool TryGenerateExpression(ExpressionSyntax expression)
        {
            using (ExpressionTag())
            {
                return TryGenerateExpressionSansTag(expression);
            }
        }

        private bool TryGenerateExpressionSansTag(ExpressionSyntax expression)
        {
            switch (expression.Kind())
            {
                case SyntaxKind.CharacterLiteralExpression:
                    return TryGenerateCharLiteral(expression);

                case SyntaxKind.UnaryMinusExpression:
                case SyntaxKind.NumericLiteralExpression:
                case SyntaxKind.StringLiteralExpression:
                case SyntaxKind.TrueLiteralExpression:
                case SyntaxKind.FalseLiteralExpression:
                    return TryGenerateLiteral(expression);

                case SyntaxKind.NullLiteralExpression:
                    GenerateNullLiteral();
                    return true;

                case SyntaxKind.ParenthesizedExpression:
                    return TryGenerateParentheses((ParenthesizedExpressionSyntax)expression);

                case SyntaxKind.AddExpression:
                case SyntaxKind.BitwiseOrExpression:
                case SyntaxKind.BitwiseAndExpression:
                    return TryGenerateBinaryOperation((BinaryExpressionSyntax)expression);

                case SyntaxKind.SimpleAssignmentExpression:
                case SyntaxKind.AddAssignmentExpression:
                    return TryGenerateAssignment((AssignmentExpressionSyntax)expression);

                case SyntaxKind.CastExpression:
                    return TryGenerateCast((CastExpressionSyntax)expression);

                case SyntaxKind.ObjectCreationExpression:
                    return TryGenerateNewClass((ObjectCreationExpressionSyntax)expression);

                case SyntaxKind.ArrayCreationExpression:
                    return TryGenerateNewArray((ArrayCreationExpressionSyntax)expression);

                case SyntaxKind.ArrayInitializerExpression:
                    return TryGenerateArrayLiteral((InitializerExpressionSyntax)expression);

                case SyntaxKind.SimpleMemberAccessExpression:
                    return TryGenerateNameRef((MemberAccessExpressionSyntax)expression);

                case SyntaxKind.IdentifierName:
                    return TryGenerateNameRef((IdentifierNameSyntax)expression);

                case SyntaxKind.InvocationExpression:
                    return GenerateMethodCall((InvocationExpressionSyntax)expression);

                case SyntaxKind.ElementAccessExpression:
                    return TryGenerateArrayElementAccess((ElementAccessExpressionSyntax)expression);

                case SyntaxKind.TypeOfExpression:
                    return TryGenerateTypeOfExpression((TypeOfExpressionSyntax)expression);

                case SyntaxKind.ThisExpression:
                    GenerateThisReference();
                    return true;

                case SyntaxKind.BaseExpression:
                    GenerateBaseReference();
                    return true;
            }

            return false;
        }

        private bool TryGenerateLiteral(ExpressionSyntax expression)
        {
            /*
                <ElementType name="Literal" content="eltOnly">
                - <group order="one">
                    <element type="Null" /> 
                    <element type="Number" /> 
                    <element type="Boolean" /> 
                    <element type="Char" /> 
                    <element type="String" /> 
                    <element type="Array" /> 
                    <element type="Type" /> 
                    </group>
                </ElementType>
            */

            using (LiteralTag())
            {
                var constantValue = SemanticModel.GetConstantValue(expression);
                if (!constantValue.HasValue)
                {
                    return false;
                }

                var type = SemanticModel.GetTypeInfo(expression).Type;
                if (type == null)
                {
                    return false;
                }

                switch (expression.Kind())
                {
                    case SyntaxKind.UnaryMinusExpression:
                    case SyntaxKind.NumericLiteralExpression:
                        GenerateNumber(constantValue.Value, type);
                        return true;

                    case SyntaxKind.StringLiteralExpression:
                        GenerateString((string)constantValue.Value);
                        return true;

                    case SyntaxKind.TrueLiteralExpression:
                    case SyntaxKind.FalseLiteralExpression:
                        GenerateBoolean((bool)constantValue.Value);
                        return true;
                }

                return false;
            }
        }

        private bool TryGenerateCharLiteral(ExpressionSyntax expression)
        {
            // For non-letters and digits, generate a cast of the numeric value to a char.
            // Otherwise, we might end up generating invalid XML.
            if (expression.Kind() != SyntaxKind.CharacterLiteralExpression)
            {
                return false;
            }

            var constantValue = SemanticModel.GetConstantValue(expression);
            if (!constantValue.HasValue)
            {
                return false;
            }

            var ch = (char)constantValue.Value;

            if (!char.IsLetterOrDigit(ch))
            {
                using (CastTag())
                {
                    GenerateType(SpecialType.System_Char);

                    using (ExpressionTag())
                    using (LiteralTag())
                    {
                        GenerateNumber((ushort)ch, SpecialType.System_UInt16);
                    }
                }
            }
            else
            {
                using (LiteralTag())
                {
                    GenerateChar(ch);
                }
            }

            return true;
        }

        private bool TryGenerateParentheses(ParenthesizedExpressionSyntax parenthesizedExpression)
        {
            using (ParenthesesTag())
            {
                return TryGenerateExpression(parenthesizedExpression.Expression);
            }
        }

        private bool TryGenerateBinaryOperation(BinaryExpressionSyntax binaryExpression)
        {
            BinaryOperatorKind kind;
            switch (binaryExpression.Kind())
            {
                case SyntaxKind.AddExpression:
                    kind = BinaryOperatorKind.Plus;
                    break;
                case SyntaxKind.BitwiseOrExpression:
                    kind = BinaryOperatorKind.BitwiseOr;
                    break;
                case SyntaxKind.BitwiseAndExpression:
                    kind = BinaryOperatorKind.BitwiseAnd;
                    break;
                default:
                    return false;
            }

            using (BinaryOperationTag(kind))
            {
                return TryGenerateExpression(binaryExpression.Left)
                    && TryGenerateExpression(binaryExpression.Right);
            }
        }

        private bool TryGenerateAssignment(AssignmentExpressionSyntax binaryExpression)
        {
            var kind = BinaryOperatorKind.None;
            switch (binaryExpression.Kind())
            {
                case SyntaxKind.AddAssignmentExpression:
                    kind = BinaryOperatorKind.AddDelegate;
                    break;
            }

            using (AssignmentTag(kind))
            {
                return TryGenerateExpression(binaryExpression.Left)
                    && TryGenerateExpression(binaryExpression.Right);
            }
        }

        private bool TryGenerateCast(CastExpressionSyntax castExpression)
        {
            var type = SemanticModel.GetTypeInfo(castExpression.Type).Type;
            if (type == null)
            {
                return false;
            }

            using (CastTag())
            {
                GenerateType(type);

                return TryGenerateExpression(castExpression.Expression);
            }
        }

        private bool TryGenerateNewClass(ObjectCreationExpressionSyntax objectCreationExpression)
        {
            var type = SemanticModel.GetSymbolInfo(objectCreationExpression.Type).Symbol as ITypeSymbol;
            if (type == null)
            {
                return false;
            }

            using (NewClassTag())
            {
                GenerateType(type);

                foreach (var argument in objectCreationExpression.ArgumentList.Arguments)
                {
                    if (!TryGenerateArgument(argument))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool TryGenerateNewArray(ArrayCreationExpressionSyntax arrayCreationExpression)
        {
            var type = SemanticModel.GetTypeInfo(arrayCreationExpression).Type;
            if (type == null)
            {
                return false;
            }

            using (NewArrayTag())
            {
                GenerateType(type);

                if (arrayCreationExpression.Initializer != null)
                {
                    using (BoundTag())
                    using (ExpressionTag())
                    using (LiteralTag())
                    using (NumberTag())
                    {
                        EncodedText(arrayCreationExpression.Initializer.Expressions.Count.ToString());
                    }

                    if (!TryGenerateExpression(arrayCreationExpression.Initializer))
                    {
                        return false;
                    }
                }
                else
                {
                    foreach (var rankSpecifier in arrayCreationExpression.Type.RankSpecifiers)
                    {
                        foreach (var size in rankSpecifier.Sizes)
                        {
                            using (BoundTag())
                            {
                                if (!TryGenerateExpression(size))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }

        private bool TryGenerateArrayLiteral(InitializerExpressionSyntax initializerExpression)
        {
            using (LiteralTag())
            using (ArrayTag())
            {
                foreach (var expression in initializerExpression.Expressions)
                {
                    if (!TryGenerateExpression(expression))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool TryGenerateNameRef(MemberAccessExpressionSyntax memberAccessExpression)
        {
            var symbol = SemanticModel.GetSymbolInfo(memberAccessExpression).Symbol;

            // No null check for 'symbol' here. If 'symbol' unknown, we'll
            // generate an "unknown" name ref.

            using (NameRefTag(GetVariableKind(symbol)))
            {
                var leftHandSymbol = SemanticModel.GetSymbolInfo(memberAccessExpression.Expression).Symbol;
                if (leftHandSymbol != null)
                {
                    if (leftHandSymbol.Kind == SymbolKind.Alias)
                    {
                        leftHandSymbol = ((IAliasSymbol)leftHandSymbol).Target;
                    }
                }

                // If the left-hand side is a named type, we generate a literal expression
                // with the type name. Otherwise, we generate the expression normally.
                if (leftHandSymbol != null && leftHandSymbol.Kind == SymbolKind.NamedType)
                {
                    using (ExpressionTag())
                    using (LiteralTag())
                    {
                        GenerateType((ITypeSymbol)leftHandSymbol);
                    }
                }
                else if (!TryGenerateExpression(memberAccessExpression.Expression))
                {
                    return false;
                }

                GenerateName(memberAccessExpression.Name.Identifier.ValueText);
            }

            return true;
        }

        private bool TryGenerateNameRef(IdentifierNameSyntax identifierName)
        {
            var symbol = SemanticModel.GetSymbolInfo(identifierName).Symbol;

            // No null check for 'symbol' here. If 'symbol' unknown, we'll
            // generate an "unknown" name ref.

            var variableKind = GetVariableKind(symbol);

            using (NameRefTag(variableKind))
            {
                if (symbol != null && variableKind != VariableKind.Local)
                {
                    using (ExpressionTag())
                    {
                        GenerateThisReference();
                    }
                }

                GenerateName(identifierName.Identifier.ToString());
            }

            return true;
        }

        private bool GenerateMethodCall(InvocationExpressionSyntax invocationExpression)
        {
            using (MethodCallTag())
            {
                if (!TryGenerateExpression(invocationExpression.Expression))
                {
                    return false;
                }

                foreach (var argument in invocationExpression.ArgumentList.Arguments)
                {
                    if (!TryGenerateArgument(argument))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool TryGenerateTypeOfExpression(TypeOfExpressionSyntax typeOfExpression)
        {
            if (typeOfExpression.Type == null)
            {
                return false;
            }

            var type = SemanticModel.GetTypeInfo(typeOfExpression.Type).Type;
            if (type == null)
            {
                return false;
            }

            GenerateType(type);

            return true;
        }

        private bool TryGenerateArrayElementAccess(ElementAccessExpressionSyntax elementAccessExpression)
        {
            using (ArrayElementAccessTag())
            {
                if (!TryGenerateExpression(elementAccessExpression.Expression))
                {
                    return false;
                }

                foreach (var argument in elementAccessExpression.ArgumentList.Arguments)
                {
                    if (!TryGenerateExpression(argument.Expression))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool TryGenerateArgument(ArgumentSyntax argument)
        {
            using (ArgumentTag())
            {
                return TryGenerateExpression(argument.Expression);
            }
        }

        public static string Generate(MethodDeclarationSyntax methodDeclaration, SemanticModel semanticModel)
        {
            var symbol = semanticModel.GetDeclaredSymbol(methodDeclaration);
            var builder = new MethodXmlBuilder(symbol, semanticModel);

            builder.GenerateBlock(methodDeclaration.Body);

            return builder.ToString();
        }
    }
}
