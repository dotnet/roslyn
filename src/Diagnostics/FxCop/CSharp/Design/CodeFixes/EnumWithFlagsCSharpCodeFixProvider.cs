// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Design;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.FxCopAnalyzers.Design
{
    /// <summary>
    /// CA1027: Mark enums with FlagsAttribute
    /// CA2217: Do not mark enums with FlagsAttribute
    /// </summary>
    [ExportCodeFixProvider(EnumWithFlagsDiagnosticAnalyzer.RuleNameForExportAttribute, LanguageNames.CSharp), Shared]
    public class EnumWithFlagsCSharpCodeFixProvider : EnumWithFlagsCodeFixProviderBase
    {
    }
}