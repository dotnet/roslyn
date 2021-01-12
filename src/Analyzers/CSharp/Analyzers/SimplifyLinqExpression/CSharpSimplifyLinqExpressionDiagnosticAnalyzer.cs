// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SimplifyLinqExpression;
#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#endif

namespace Microsoft.CodeAnalysis.CSharp.SimplifyLinqExpression
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpSimplifyLinqExpressionDiagnosticAnalyzer : AbstractSimplifyLinqExpressionDiagnosticAnalyzer
    {
    }
}
