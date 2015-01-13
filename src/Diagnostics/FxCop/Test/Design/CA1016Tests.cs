// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Design;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class CA1016Tests : DiagnosticAnalyzerTestBase
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
        public void CA1016BasicTestWithNoComplianceAttribute()
        {
            VerifyBasic(
@"
imports System.IO
imports System.Reflection
imports System

< Assembly: CLSCompliant(true)>
    class Program
    
        Sub Main
        End Sub
    End class
",
                diagnostic);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1016CSharpTestWithVersionAttributeNotFromBCL()
        {
            VerifyCSharp(
@"
using System;
[assembly:System.CLSCompliantAttribute(true)]
[assembly:AssemblyVersion(""1.2.3.4"")]
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
class AssemblyVersionAttribute : Attribute {
    public AssemblyVersionAttribute(string s) {}
}
",
                diagnostic);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1016CSharpTestWithNoVersionAttribute()
        {
            VerifyCSharp(
@"
[assembly:System.CLSCompliantAttribute(true)]

    class Program
    {
        static void Main(string[] args)
        {
        }
    }
",
                diagnostic);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1016CSharpTestWithVersionAttribute()
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
        public void CA1016CSharpTestWithTwoFilesWithAttribute()
        {
            VerifyCSharp(new string[]
                {
@"
[assembly:System.CLSCompliantAttribute(true)]

    class Program
    {
        static void Main(string[] args)
        {
        }
    }
",
@"
using System.Reflection;
[assembly: AssemblyVersionAttribute(""1.2.3.4"")]
"
                });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1016CSharpTestWithVersionAttributeTruncated()
        {
            VerifyCSharp(
@"
using System.Reflection;
[assembly:AssemblyVersion(""1.2.3.4"")]
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
        public void CA1016CSharpTestWithVersionAttributeFullyQualified()
        {
            VerifyCSharp(
@"
[assembly:System.CLSCompliantAttribute(true)]

[assembly:System.Reflection.AssemblyVersion(""1.2.3.4"")]
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
");
        }

        private static string number = "CA1016";
        private static string message = "Assemblies should be marked with AssemblyVersionAttribute";

        private static DiagnosticResult diagnostic = new DiagnosticResult
        {
            Id = number,
            Severity = DiagnosticSeverity.Warning,
            Message = message
        };
    }
}
