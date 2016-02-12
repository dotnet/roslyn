// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpDiagnosticAnalyzerFieldsAnalyzer : DiagnosticAnalyzerFieldsAnalyzer<ClassDeclarationSyntax, FieldDeclarationSyntax, TypeSyntax, VariableDeclarationSyntax>
    {
    }
}
