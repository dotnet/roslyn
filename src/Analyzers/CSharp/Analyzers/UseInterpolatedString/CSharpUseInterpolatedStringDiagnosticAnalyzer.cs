// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.UseInterpolatedString;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.CodeStyle;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseInterpolatedString;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal abstract class CSharpUseInterpolatedStringDiagnosticAnalyzer : AbstractUseInterpolatedStringDiagnosticAnalyzer
{
    protected CSharpUseInterpolatedStringDiagnosticAnalyzer() : base()
    {
    }
}
