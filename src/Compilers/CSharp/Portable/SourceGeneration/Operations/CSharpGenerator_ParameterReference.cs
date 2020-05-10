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
        private IdentifierNameSyntax? TryGenerateParameterReference(IParameterReferenceOperation? operation, SyntaxType type)
        {
            if (operation == null || operation.IsImplicit)
                return null;

            if (type == SyntaxType.Statement)
                throw new ArgumentException($"{nameof(IParameterReferenceOperation)} cannot be converted to a {nameof(StatementSyntax)}");

            return IdentifierName(operation.Parameter.Name);
        }
    }
}
