// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Usage;

namespace Microsoft.CodeAnalysis.CSharp.FxCopAnalyzers.Usage
{
    [DiagnosticAnalyzer]
    [ExportDiagnosticAnalyzer(RuleNameForExportAttribute, LanguageNames.CSharp)]
    public class CSharpSerializationRulesDiagnosticAnalyzer : SerializationRulesDiagnosticAnalyzer
    {
    }
}
