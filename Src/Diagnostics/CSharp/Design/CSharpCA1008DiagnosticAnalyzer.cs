// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Design;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.FxCopAnalyzers.Design
{
    /// <summary>
    /// CA1008: Enums should have zero value
    /// 
    /// Cause:
    /// An enumeration without an applied System.FlagsAttribute does not define a member that has a value of zero;
    /// or an enumeration that has an applied FlagsAttribute defines a member that has a value of zero but its name is not 'None',
    /// or the enumeration defines multiple zero-valued members.
    /// </summary>
    [DiagnosticAnalyzer]
    [ExportDiagnosticAnalyzer(RuleId, LanguageNames.CSharp)]
    public class CSharpCA1008DiagnosticAnalyzer : CA1008DiagnosticAnalyzer
    {
    }
}
