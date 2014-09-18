// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Design;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class CA1014Tests : DiagnosticAnalyzerTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new AssemblyAttributesDiagnosticAnalyzer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new AssemblyAttributesDiagnosticAnalyzer();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1014CA1016BasicTestWithCLSCompliantAttributeNone()
        {
            VerifyBasic(
@"
imports System.Reflection

    class Program
    
        Sub Main
        End Sub
    End class
",
            diagnosticCA1016, diagnosticCA1014);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1014BasicTestWithNoVersionAttribute()
        {
            VerifyBasic(
@"
imports System.Reflection

< Assembly: AssemblyVersionAttribute(""1.1.3.4"")>
    class Program
    
        Sub Main
        End Sub
    End class
",
                diagnosticCA1014);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1014CSharpTestWithComplianceAttributeNotFromBCL()
        {
            VerifyCSharp(
@"
using System;
using System.Reflection;
[assembly:AssemblyVersionAttribute(""1.2.3.4"")]

[assembly:CLSCompliant(true)]
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
class CLSCompliantAttribute : Attribute {
    public CLSCompliantAttribute(bool s) {}
}
",
                diagnosticCA1014);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1014CSharpTestWithNoCLSComplianceAttribute()
        {
            VerifyCSharp(
@"
using System.Reflection;
[assembly:AssemblyVersionAttribute(""1.2.3.4"")]

class Program
{
    static void Main(string[] args)
    {
    }
}
",
                diagnosticCA1014);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1014CSharpTestWithCLSCompliantAttribute()
        {
            VerifyCSharp(
@"
using System;
using System.Reflection;
[assembly:AssemblyVersionAttribute(""1.2.3.4"")]

[assembly:CLSCompliantAttribute(true)]
class Program
{
    static void Main(string[] args)
    {
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1014CSharpTestWithTwoFilesWithAttribute()
        {
            VerifyCSharp(new string[]
                {
@"
using System.Reflection;
[assembly:AssemblyVersionAttribute(""1.2.3.4"")]

class Program
{
    static void Main(string[] args)
    {
    }
}
",
@"
using System;
[assembly:CLSCompliantAttribute(true)]
"
                });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1014CSharpTestWithCLSCompliantAttributeTruncated()
        {
            VerifyCSharp(
@"
using System;
using System.Reflection;
[assembly:AssemblyVersionAttribute(""1.2.3.4"")]

[assembly:CLSCompliant(true)]
class Program
{
    static void Main(string[] args)
    {
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1014CSharpTestWithCLSCompliantAttributeFullyQualified()
        {
            VerifyCSharp(
@"
using System.Reflection;
[assembly:AssemblyVersionAttribute(""1.2.3.4"")]
[assembly:System.CLSCompliantAttribute(true)]
class Program
{
    static void Main(string[] args)
    {
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1014CSharpTestWithCLSCompliantAttributeNone()
        {
            VerifyCSharp(
@"
using System.Reflection;
class Program
{
    static void Main(string[] args)
    {
    }
}
",
            diagnosticCA1016, diagnosticCA1014);
        }

        private static DiagnosticResult diagnosticCA1014 = new DiagnosticResult
        {
            Id = AssemblyAttributesDiagnosticAnalyzer.CA1014RuleName,
            Severity = DiagnosticSeverity.Warning,
            Message = AssemblyAttributesDiagnosticAnalyzer.CA1014Rule.MessageFormat
        };

        private static DiagnosticResult diagnosticCA1016 = new DiagnosticResult
        {
            Id = AssemblyAttributesDiagnosticAnalyzer.CA1016RuleName,
            Severity = DiagnosticSeverity.Warning,
            Message = AssemblyAttributesDiagnosticAnalyzer.CA1016Rule.MessageFormat
        };
    }
}
