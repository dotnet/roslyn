// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Analyzers;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Analyzers
{
    public class InternalImplementationOnlyTests : DiagnosticAnalyzerTestBase
    {
        private const string AttributeStringCSharp = @"
namespace System.Runtime.CompilerServices
{
    internal class InternalImplementationOnlyAttribute : System.Attribute {}
}
";
        [Fact]
        public void CSharp_VerifySameAssembly()
        {
            string source = AttributeStringCSharp + @"

[System.Runtime.CompilerServices.InternalImplementationOnly]
public interface IFoo { }

class Foo : IFoo { }
";

            // Verify no diagnostic since interface is in the same assembly.
            VerifyCSharp(source, addLanguageSpecificCodeAnalysisReference: false);
        }

        [Fact]
        public void CSharp_VerifyDifferentAssembly()
        {
            string source1 = AttributeStringCSharp + @"

[System.Runtime.CompilerServices.InternalImplementationOnly]
public interface IFoo { }

public interface IBar : IFoo { }
";

            var source2 = @"
class Foo : IFoo { }

class Boo : IBar { }";

            DiagnosticResult[] expected = new[] { GetCSharpExpectedDiagnostic(2, 7, "Foo", "IFoo"), GetCSharpExpectedDiagnostic(4, 7, "Boo", "IFoo") };

            // Verify errors since interface is not in a friend assembly.
            VerifyCSharpAcrossTwoAssemblies(source1, source2, expected);
        }

        [Fact]
        public void CSharp_VerifyDifferentFriendAssembly()
        {
            string source1 = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""TestProject"")]
" + AttributeStringCSharp + @"

[System.Runtime.CompilerServices.InternalImplementationOnly]
public interface IFoo { }

public interface IBar : IFoo { }
";

            var source2 = @"
class Foo : IFoo { }

class Boo : IBar { }";

            // Verify no diagnostic since interface is in a friend assembly.
            VerifyCSharpAcrossTwoAssemblies(source1, source2);
        }

        [Fact]
        public void CSharp_VerifyISymbol()
        {
            var source = @"
class Foo : Microsoft.CodeAnalysis.ISymbol { }
class Bar : Microsoft.CodeAnalysis.IAssemblySymbol { }
";
            DiagnosticResult[] expected = new[] { GetCSharpExpectedDiagnostic(2, 7, "Foo", "ISymbol"), GetCSharpExpectedDiagnostic(3, 7, "Bar", "ISymbol") };

            // Verify that ISymbol is not implementable.
            VerifyCSharp(source, addLanguageSpecificCodeAnalysisReference: true, expected: expected);
        }

        private const string AttributeStringBasic = @"
Namespace System.Runtime.CompilerServices
    Friend Class InternalImplementationOnlyAttribute 
        Inherits System.Attribute
    End Class
End Namespace
";

        [Fact]
        public void Basic_VerifySameAssembly()
        {
            string source = AttributeStringBasic + @"

<System.Runtime.CompilerServices.InternalImplementationOnly>
Public Interface IFoo
End Interface

Class Foo 
    Implements IFoo 
End Class
";

            // Verify no diagnostic since interface is in the same assembly.
            VerifyBasic(source, addLanguageSpecificCodeAnalysisReference: false);
        }

        [Fact]
        public void Basic_VerifyDifferentAssembly()
        {
            string source1 = AttributeStringBasic + @"

<System.Runtime.CompilerServices.InternalImplementationOnly>
Public Interface IFoo
End Interface

Public Interface IBar
    Inherits IFoo
End Interface
";

            var source2 = @"
Class Foo 
    Implements IFoo 
End Class

Class Bar
    Implements IBar
End Class
";
            DiagnosticResult[] expected = new[] { GetBasicExpectedDiagnostic(2, 7, "Foo", "IFoo"), GetBasicExpectedDiagnostic(6, 7, "Bar", "IFoo") };

            // Verify errors since interface is not in a friend assembly.
            VerifyBasicAcrossTwoAssemblies(source1, source2, expected);
        }

        [Fact]
        public void Basic_VerifyDifferentFriendAssembly()
        {
            string source1 = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""TestProject"")>
" + AttributeStringBasic + @"

<System.Runtime.CompilerServices.InternalImplementationOnly>
Public Interface IFoo
End Interface

Public Interface IBar
    Inherits IFoo
End Interface
";

            var source2 = @"
Class Foo 
    Implements IFoo 
End Class

Class Bar
    Implements IBar
End Class
";

            // Verify no diagnostic since interface is in a friend assembly.
            VerifyBasicAcrossTwoAssemblies(source1, source2);
        }

        [Fact]
        public void Basic_VerifyISymbol()
        {
            var source = @"
Class Foo 
    Implements Microsoft.CodeAnalysis.ISymbol
End Class
Class Bar
    Implements Microsoft.CodeAnalysis.IAssemblySymbol
End Class
";
            DiagnosticResult[] expected = new[] { GetBasicExpectedDiagnostic(2, 7, "Foo", "ISymbol"), GetBasicExpectedDiagnostic(5, 7, "Bar", "ISymbol") };

            // Verify that ISymbol is not implementable.
            VerifyBasic(source, addLanguageSpecificCodeAnalysisReference: true, expected: expected);
        }

        private void VerifyAcrossTwoAssemblies(string source1, string source2, string language, params DiagnosticResult[] expected)
        {
            Debug.Assert(language == LanguageNames.CSharp || language == LanguageNames.VisualBasic);

            Project project1 = CreateProject(new string[] { source1 }, language: language, addLanguageSpecificCodeAnalysisReference: false);
            Project project2 = CreateProject(new string[] { source2 }, language: language, addLanguageSpecificCodeAnalysisReference: false, addToSolution: project1.Solution)
                           .AddProjectReference(new ProjectReference(project1.Id));

            DiagnosticAnalyzer analyzer = language == LanguageNames.CSharp ? GetCSharpDiagnosticAnalyzer() : GetBasicDiagnosticAnalyzer();
            GetSortedDiagnostics(analyzer, project2.Documents.ToArray()).Verify(analyzer, expected);
        }

        private void VerifyCSharpAcrossTwoAssemblies(string source1, string source2, params DiagnosticResult[] expected)
        {
            VerifyAcrossTwoAssemblies(source1, source2, LanguageNames.CSharp, expected);
        }

        private void VerifyBasicAcrossTwoAssemblies(string source1, string source2, params DiagnosticResult[] expected)
        {
            VerifyAcrossTwoAssemblies(source1, source2, LanguageNames.VisualBasic, expected);
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new InternalImplementationOnlyAnalyzer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new InternalImplementationOnlyAnalyzer();
        }

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int line, int column, string typeName, string interfaceName)
        {
            return GetExpectedDiagnostic(LanguageNames.CSharp, line, column, typeName, interfaceName);
        }

        private static DiagnosticResult GetBasicExpectedDiagnostic(int line, int column, string typeName, string interfaceName)
        {
            return GetExpectedDiagnostic(LanguageNames.VisualBasic, line, column, typeName, interfaceName);
        }

        private static DiagnosticResult GetExpectedDiagnostic(string language, int line, int column, string typeName, string interfaceName)
        {
            string fileName = language == LanguageNames.CSharp ? "Test0.cs" : "Test0.vb";
            return new DiagnosticResult
            {
                Id = DiagnosticIds.InternalImplementationOnlyRuleId,
                Message = string.Format(CodeAnalysisDiagnosticsResources.InternalImplementationOnlyMessage, typeName, interfaceName),
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation(fileName, line, column)
                }
            };
        }
    }
}
