// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpGenerator
    {
        private ExpressionSyntax? TryGenerateFieldReference(IFieldReferenceOperation? operation, SyntaxType type)
        {
            if (operation == null)
                return null;

            if (type == SyntaxType.Statement)
                throw new ArgumentException("Field reference cannot be converted to a statement");

            var field = operation.Field;
            var instance = operation.Instance != null
                ? TryGenerateExpression(operation.Instance)
                : field.IsStatic ? field.Type.GenerateTypeSyntax() : null;

            var name = IdentifierName(field.Name);
            var result = instance == null
                ? (ExpressionSyntax)name
                : MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, instance, name);

            return result;
        }
    }
}
