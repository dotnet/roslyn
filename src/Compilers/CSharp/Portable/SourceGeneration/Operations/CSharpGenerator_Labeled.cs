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
        private LabeledStatementSyntax? TryGenerateLabeledStatement(ILabeledOperation? operation, SyntaxType type)
        {
            if (operation == null || operation.IsImplicit)
                return null;

            var statement = TryGenerateStatement(operation.Operation);
            if (statement == null)
                return null;

            return LabeledStatement(
                Identifier(operation.Label.Name),
                statement);
        }
    }
}
