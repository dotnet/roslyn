// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SourceGeneration;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpCodeGenerator
    {
        private static DelegateDeclarationSyntax GenerateDelegateDeclaration(INamedTypeSymbol symbol)
        {
            var invoke = symbol.DelegateInvokeMethod;
            if (invoke == null)
                throw new ArgumentException("Delegates must have a DelegateInvokeMethod");

            return DelegateDeclaration(
                GenerateAttributeLists(invoke.GetAttributes()),
                GenerateModifiers(invoke.DeclaredAccessibility, invoke.GetModifiers()),
                invoke.ReturnType.GenerateTypeSyntax(),
                Identifier(invoke.Name),
                GenerateTypeParameterList(invoke.TypeArguments),
                GenerateParameterList(invoke.Parameters),
                GenerateTypeParameterConstraintClauses(invoke.TypeArguments));
        }
    }
}
