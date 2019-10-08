// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests
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
            VerifyCSharp(source, referenceFlags: ReferenceFlags.RemoveCodeAnalysis);
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
// Causes many compile errors, because not all members are implemented.
class Foo : Microsoft.CodeAnalysis.ISymbol { }
class Bar : Microsoft.CodeAnalysis.IAssemblySymbol { }
";
            DiagnosticResult[] expected = new[] { GetCSharpExpectedDiagnostic(3, 7, "Foo", "ISymbol"), GetCSharpExpectedDiagnostic(4, 7, "Bar", "ISymbol") };

            // Verify that ISymbol is not implementable.
            VerifyCSharp(source, referenceFlags: ReferenceFlags.None, validationMode: TestValidationMode.AllowCompileErrors, expected: expected);
        }

        [Fact]
        public void CSharp_VerifyIOperation()
        {
            var source = @"
// Causes many compile errors, because not all members are implemented.
class Foo : Microsoft.CodeAnalysis.IOperation { }
class Bar : Microsoft.CodeAnalysis.Operations.IInvocationOperation { }
";
            DiagnosticResult[] expected = new[] { GetCSharpExpectedDiagnostic(3, 7, "Foo", "IOperation"), GetCSharpExpectedDiagnostic(4, 7, "Bar", "IOperation") };

            // Verify that IOperation is not implementable.
            VerifyCSharp(source, referenceFlags: ReferenceFlags.None, validationMode: TestValidationMode.AllowCompileErrors, expected: expected);
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
            VerifyBasic(source, referenceFlags: ReferenceFlags.RemoveCodeAnalysis);
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
' Causes many compile errors, because not all members are implemented.
Class Foo 
    Implements Microsoft.CodeAnalysis.ISymbol
End Class
Class Bar
    Implements Microsoft.CodeAnalysis.IAssemblySymbol
End Class
";
            DiagnosticResult[] expected = new[] { GetBasicExpectedDiagnostic(3, 7, "Foo", "ISymbol"), GetBasicExpectedDiagnostic(6, 7, "Bar", "ISymbol") };

            // Verify that ISymbol is not implementable.
            VerifyBasic(source, referenceFlags: ReferenceFlags.None, validationMode: TestValidationMode.AllowCompileErrors, expected: expected);
        }

        [Fact]
        public void Basic_VerifyIOperation()
        {
            var source = @"
' Causes many compile errors, because not all members are implemented.
Class Foo 
    Implements Microsoft.CodeAnalysis.IOperation
End Class
Class Bar
    Implements Microsoft.CodeAnalysis.Operations.IInvocationOperation
End Class
";
            DiagnosticResult[] expected = new[] { GetBasicExpectedDiagnostic(3, 7, "Foo", "IOperation"), GetBasicExpectedDiagnostic(6, 7, "Bar", "IOperation") };

            // Verify that IOperation is not implementable.
            VerifyBasic(source, referenceFlags: ReferenceFlags.None, validationMode: TestValidationMode.AllowCompileErrors, expected: expected);
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
            return GetExpectedDiagnostic(line, column, typeName, interfaceName);
        }

        private static DiagnosticResult GetBasicExpectedDiagnostic(int line, int column, string typeName, string interfaceName)
        {
            return GetExpectedDiagnostic(line, column, typeName, interfaceName);
        }

        private static DiagnosticResult GetExpectedDiagnostic(int line, int column, string typeName, string interfaceName)
        {
            return new DiagnosticResult(DiagnosticIds.InternalImplementationOnlyRuleId, DiagnosticSeverity.Error)
                .WithLocation(line, column)
                .WithMessageFormat(CodeAnalysisDiagnosticsResources.InternalImplementationOnlyMessage)
                .WithArguments(typeName, interfaceName);
        }
    }
}
