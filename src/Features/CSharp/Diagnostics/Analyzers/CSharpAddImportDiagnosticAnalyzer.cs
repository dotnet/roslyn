// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.AddImport;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.AddImport
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpAddImportDiagnosticAnalyzer : AddImportDiagnosticAnalyzerBase<SyntaxKind, SimpleNameSyntax, QualifiedNameSyntax, IncompleteMemberSyntax>
    {
        private const string NameNotInContext = "CS0103";
        private const string MessageFormat = "The name '{0}' does not exist in the current context";

        private static readonly ImmutableArray<SyntaxKind> s_kindsOfInterest = ImmutableArray.Create(SyntaxKind.IncompleteMember);

        protected override ImmutableArray<SyntaxKind> SyntaxKindsOfInterest
        {
            get
            {
                return s_kindsOfInterest;
            }
        }

        protected override DiagnosticDescriptor DiagnosticDescriptor
        {
            get
            {
                return GetDiagnosticDescriptor(NameNotInContext, MessageFormat);
            }
        }
    }
}
