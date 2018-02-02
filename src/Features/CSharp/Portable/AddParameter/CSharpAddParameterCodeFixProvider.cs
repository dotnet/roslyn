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

        private const string CS0103 = nameof(CS0103); // error CS0103: Error The name 'Goo' does not exist in the current context
        private const string CS0117 = nameof(CS0117); // error CS0117: 'Class' does not contain a definition for 'Goo'
        private const string CS0118 = nameof(CS0118); // error CS0118: 'X' is a namespace but is used like a variable
        private const string CS0122 = nameof(CS0122); // error CS0122: 'Class' is inaccessible due to its protection level.
        private const string CS1061 = nameof(CS1061); // error CS1061: Error 'Class' does not contain a definition for 'Goo' and no extension method 'Goo' 
        private const string CS1501 = nameof(CS1501); // error CS1501: No overload for method 'M' takes 1 arguments
        private const string CS1503 = nameof(CS1503); // error CS1503: Argument 1: cannot convert from 'double' to 'int'
        private const string CS1660 = nameof(CS1660); // error CS1660: Cannot convert lambda expression to type 'string[]' because it is not a delegate type
        private const string CS1739 = nameof(CS1739); // error CS1739: The best overload for 'M' does not have a parameter named 'x'
        private const string CS7036 = nameof(CS7036); // error CS7036: There is no argument given that corresponds to the required formal parameter 'x' of 'C.M(int)'
        private const string CS1729 = nameof(CS1729); // error CS1729: 'C' does not contain a constructor that takes n arguments
        private const string CS1593 = nameof(CS1593); // error CS1593: C# Delegate 'Action' does not take 1 arguments

        private static readonly ImmutableArray<string> AddParameterFixableDiagnosticIds =
            ImmutableArray.Create(CS0122, CS1729, CS1739, CS1503, CS1660, CS7036, IDEDiagnosticIds.UnboundConstructorId,
                CS0103, CS0117, CS0118, CS1061, CS1501);

        public override ImmutableArray<string> FixableDiagnosticIds
            => AddParameterFixableDiagnosticIds;

        protected override ImmutableArray<string> TooManyArgumentsDiagnosticIds
            => GenerateConstructorDiagnosticIds.TooManyArgumentsDiagnosticIds;

        protected override ImmutableArray<string> CannotConvertDiagnosticIds
            => GenerateConstructorDiagnosticIds.CannotConvertDiagnosticIds;
    }
}
