// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.AddParameter;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateMethod;
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
        private static readonly ImmutableArray<string> AddParameterFixableDiagnosticIds =
            GenerateConstructorDiagnosticIds.AllDiagnosticIds.Union(
            GenerateMethodDiagnosticIds.FixableDiagnosticIds).ToImmutableArray();

        public override ImmutableArray<string> FixableDiagnosticIds => AddParameterFixableDiagnosticIds;

        protected override ImmutableArray<string> TooManyArgumentsDiagnosticIds { get; } =
            GenerateConstructorDiagnosticIds.TooManyArgumentsDiagnosticIds;
    }
}
