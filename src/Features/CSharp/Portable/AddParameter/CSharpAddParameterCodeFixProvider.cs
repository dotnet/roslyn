// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.AddParameter;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.GenerateConstructor;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.AddParameter
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddParameter), Shared]
    [ExtensionOrder(Before = PredefinedCodeFixProviderNames.GenerateConstructor)]
    internal class CSharpAddParameterCodeFixProvider : AbstractAddParameterCodeFixProvider<
        ArgumentSyntax,
        AttributeArgumentSyntax,
        ArgumentListSyntax,
        AttributeArgumentListSyntax,
        InvocationExpressionSyntax,
        ObjectCreationExpressionSyntax>
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            GenerateConstructorDiagnosticIds.AllDiagnosticIds;

        protected override ImmutableArray<string> TooManyArgumentsDiagnosticIds { get; } =
            GenerateConstructorDiagnosticIds.TooManyArgumentsDiagnosticIds;
    }
}
