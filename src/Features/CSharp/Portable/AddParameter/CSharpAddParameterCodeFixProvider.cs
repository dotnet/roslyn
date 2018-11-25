// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.AddParameter;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.GenerateConstructor;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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

        private const string CS1501 = nameof(CS1501); // error CS1501: No overload for method 'M' takes 1 arguments
        private const string CS1503 = nameof(CS1503); // error CS1503: Argument 1: cannot convert from 'double' to 'int'
        private const string CS1660 = nameof(CS1660); // error CS1660: Cannot convert lambda expression to type 'string[]' because it is not a delegate type
        private const string CS1729 = nameof(CS1729); // error CS1729: 'C' does not contain a constructor that takes n arguments
        private const string CS1739 = nameof(CS1739); // error CS1739: The best overload for 'M' does not have a parameter named 'x'

        private static readonly ImmutableArray<string> AddParameterFixableDiagnosticIds = ImmutableArray.Create(
            CS1501, CS1503, CS1660, CS1729, CS1739,
            IDEDiagnosticIds.UnboundConstructorId);

        public override ImmutableArray<string> FixableDiagnosticIds
            => AddParameterFixableDiagnosticIds;

        protected override ImmutableArray<string> TooManyArgumentsDiagnosticIds
            => GenerateConstructorDiagnosticIds.TooManyArgumentsDiagnosticIds;

        protected override ImmutableArray<string> CannotConvertDiagnosticIds
            => GenerateConstructorDiagnosticIds.CannotConvertDiagnosticIds;
    }
}
