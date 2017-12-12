// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Test.Utilities;
using Xunit;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class PartsExportedWithMEFv2MustBeMarkedAsSharedTests : DiagnosticAnalyzerTestBase
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

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new PartsExportedWithMEFv2MustBeMarkedAsSharedAnalyzer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new PartsExportedWithMEFv2MustBeMarkedAsSharedAnalyzer();
        }

        #region No Diagnostic Tests

        [Fact]
        public void NoDiagnosticCases_ResolvedTypes()
        {
            VerifyCSharp(@"
using System;
using System.Composition;

[Export(typeof(C)), Shared]
public class C
{
}
" + CSharpWellKnownAttributesDefinition);

            VerifyBasic(@"
Imports System
Imports System.Composition

<Export(GetType(C)), [Shared]> _
Public Class C
End Class
" + BasicWellKnownAttributesDefinition);
        }

        [Fact]
        public void NoDiagnosticCases_UnresolvedTypes()
        {
            VerifyCSharp(@"
using System;
using System.Composition;

[Export(typeof(C)), Shared]
public class C
{
}
", TestValidationMode.AllowCompileErrors);

            VerifyBasic(@"
Imports System
Imports System.Composition

<Export(GetType(C)), [Shared]> _
Public Class C
End Class
", TestValidationMode.AllowCompileErrors);
        }

        #endregion

        #region Diagnostic Tests

        [Fact]
        public void DiagnosticCases_NoSharedAttribute()
        {
            VerifyCSharp(@"
using System;
using System.Composition;

[Export(typeof(C))]
public class C
{
}
" + CSharpWellKnownAttributesDefinition,
    // Test0.cs(5,2): warning RS0023: 'C' is exported with MEFv2 and hence must be marked as Shared
    GetCSharpResultAt(5, 2, "C"));

            VerifyBasic(@"
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
        public void DiagnosticCases_DifferentSharedAttribute()
        {
            VerifyCSharp(@"
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

            VerifyBasic(@"
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
            var message = string.Format(RoslynDiagnosticsAnalyzersResources.PartsExportedWithMEFv2MustBeMarkedAsSharedMessage, typeName);
            return GetCSharpResultAt(line, column, RoslynDiagnosticIds.MissingSharedAttributeRuleId, message);
        }

        private static DiagnosticResult GetBasicResultAt(int line, int column, string typeName)
        {
            var message = string.Format(RoslynDiagnosticsAnalyzersResources.PartsExportedWithMEFv2MustBeMarkedAsSharedMessage, typeName);
            return GetBasicResultAt(line, column, RoslynDiagnosticIds.MissingSharedAttributeRuleId, message);
        }
    }
}