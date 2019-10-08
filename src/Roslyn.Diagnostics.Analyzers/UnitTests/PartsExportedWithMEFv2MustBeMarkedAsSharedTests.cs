// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.CodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.PartsExportedWithMEFv2MustBeMarkedAsSharedAnalyzer,
    Roslyn.Diagnostics.CSharp.Analyzers.CSharpPartsExportedWithMEFv2MustBeMarkedAsSharedFixer>;
using VerifyVB = Microsoft.CodeAnalysis.VisualBasic.Testing.XUnit.CodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.PartsExportedWithMEFv2MustBeMarkedAsSharedAnalyzer,
    Roslyn.Diagnostics.VisualBasic.Analyzers.BasicPartsExportedWithMEFv2MustBeMarkedAsSharedFixer>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class PartsExportedWithMEFv2MustBeMarkedAsSharedTests
    {
        private const string CSharpWellKnownAttributesDefinition = @"
namespace System.Composition
{
    public class ExportAttribute : System.Attribute
    {
        public ExportAttribute(System.Type contractType){ }
    }

    public class SharedAttribute : System.Attribute
    {
    }
}
";
        private const string BasicWellKnownAttributesDefinition = @"
Namespace System.Composition
	Public Class ExportAttribute
		Inherits System.Attribute
		Public Sub New(contractType As System.Type)
		End Sub
	End Class

	Public Class SharedAttribute
		Inherits System.Attribute
	End Class
End Namespace

";

        #region No Diagnostic Tests

        [Fact]
        public async Task NoDiagnosticCases_ResolvedTypes()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using System.Composition;

[Export(typeof(C)), Shared]
public class C
{
}
" + CSharpWellKnownAttributesDefinition);

            await VerifyVB.VerifyAnalyzerAsync(@"
Imports System
Imports System.Composition

<Export(GetType(C)), [Shared]> _
Public Class C
End Class
" + BasicWellKnownAttributesDefinition);
        }

        [Fact]
        public async Task NoDiagnosticCases_UnresolvedTypes()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using System.{|CS0234:Composition|};

[{|CS0246:{|CS0246:Export|}|}(typeof(C)), {|CS0246:{|CS0246:Shared|}|}]
public class C
{
}
");

            await VerifyVB.VerifyAnalyzerAsync(@"
Imports System
Imports System.Composition

<{|BC30002:Export|}(GetType(C)), {|BC30002:[Shared]|}> _
Public Class C
End Class
");
        }

        #endregion

        #region Diagnostic Tests

        [Fact]
        public async Task DiagnosticCases_NoSharedAttribute()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using System.Composition;

[Export(typeof(C))]
public class C
{
}
" + CSharpWellKnownAttributesDefinition,
    // Test0.cs(5,2): warning RS0023: 'C' is exported with MEFv2 and hence must be marked as Shared
    GetCSharpResultAt(5, 2, "C"));

            await VerifyVB.VerifyAnalyzerAsync(@"
Imports System
Imports System.Composition

<Export(GetType(C))> _
Public Class C
End Class
" + BasicWellKnownAttributesDefinition,
    // Test0.vb(5,2): warning RS0023: 'C' is exported with MEFv2 and hence must be marked as Shared
    GetBasicResultAt(5, 2, "C"));
        }

        [Fact]
        public async Task DiagnosticCases_DifferentSharedAttribute()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;

[System.Composition.Export(typeof(C)), Shared]
public class C
{
}

public class SharedAttribute: Attribute
{
}
" + CSharpWellKnownAttributesDefinition,
    // Test0.cs(4,2): warning RS0023: 'C' is exported with MEFv2 and hence must be marked as Shared
    GetCSharpResultAt(4, 2, "C"));

            await VerifyVB.VerifyAnalyzerAsync(@"
Imports System

<System.Composition.Export(GetType(C)), [Shared]> _
Public Class C
End Class

Public Class SharedAttribute
    Inherits Attribute
End Class
" + BasicWellKnownAttributesDefinition,
    // Test0.vb(4,2): warning RS0023: 'C' is exported with MEFv2 and hence must be marked as Shared
    GetBasicResultAt(4, 2, "C"));
        }

        #endregion

        private static DiagnosticResult GetCSharpResultAt(int line, int column, string typeName)
        {
            return new DiagnosticResult(RoslynDiagnosticIds.MissingSharedAttributeRuleId, DiagnosticHelpers.DefaultDiagnosticSeverity)
                .WithLocation(line, column)
                .WithMessageFormat(RoslynDiagnosticsAnalyzersResources.PartsExportedWithMEFv2MustBeMarkedAsSharedMessage)
                .WithArguments(typeName);
        }

        private static DiagnosticResult GetBasicResultAt(int line, int column, string typeName)
        {
            return new DiagnosticResult(RoslynDiagnosticIds.MissingSharedAttributeRuleId, DiagnosticHelpers.DefaultDiagnosticSeverity)
                .WithLocation(line, column)
                .WithMessageFormat(RoslynDiagnosticsAnalyzersResources.PartsExportedWithMEFv2MustBeMarkedAsSharedMessage)
                .WithArguments(typeName);
        }
    }
}