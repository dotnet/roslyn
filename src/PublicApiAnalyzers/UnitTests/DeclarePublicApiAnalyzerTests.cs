// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.VisualBasic.Testing;
using Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.PublicApiAnalyzers.UnitTests
{
    public class DeclarePublicApiAnalyzerTests
    {
        private static DiagnosticResult GetAdditionalFileResultAt(int line, int column, string path, DiagnosticDescriptor descriptor, params object[] arguments)
        {
            return new DiagnosticResult(descriptor)
                .WithLocation(path, line, column)
                .WithArguments(arguments);
        }

        private static DiagnosticResult GetCSharpResultAt(int line, int column, DiagnosticDescriptor descriptor, params object[] arguments)
        {
            return new DiagnosticResult(descriptor)
                .WithLocation(line, column)
                .WithArguments(arguments);
        }

        private static DiagnosticResult GetBasicResultAt(int line, int column, DiagnosticDescriptor descriptor, params object[] arguments)
        {
            return new DiagnosticResult(descriptor)
                .WithLocation(line, column)
                .WithArguments(arguments);
        }

        private async Task VerifyBasicAsync(string source, string shippedApiText, string unshippedApiText, params DiagnosticResult[] expected)
        {
            var test = new VisualBasicCodeFixTest<DeclarePublicApiAnalyzer, DeclarePublicApiFix, XUnitVerifier>
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles = { },
                },
                TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck,
            };

            if (shippedApiText != null)
            {
                test.TestState.AdditionalFiles.Add((DeclarePublicApiAnalyzer.ShippedFileName, shippedApiText));
            }

            if (unshippedApiText != null)
            {
                test.TestState.AdditionalFiles.Add((DeclarePublicApiAnalyzer.UnshippedFileName, unshippedApiText));
            }

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        private async Task VerifyCSharpAsync(string source, string shippedApiText, string unshippedApiText, params DiagnosticResult[] expected)
        {
            var test = new CSharpCodeFixTest<DeclarePublicApiAnalyzer, DeclarePublicApiFix, XUnitVerifier>
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles = { },
                },
                TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck,
            };

            if (shippedApiText != null)
            {
                test.TestState.AdditionalFiles.Add((DeclarePublicApiAnalyzer.ShippedFileName, shippedApiText));
            }

            if (unshippedApiText != null)
            {
                test.TestState.AdditionalFiles.Add((DeclarePublicApiAnalyzer.UnshippedFileName, unshippedApiText));
            }

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        private async Task VerifyCSharpAsync(string source, string shippedApiText, string unshippedApiText, string shippedApiFilePath, string unshippedApiFilePath, params DiagnosticResult[] expected)
        {
            var test = new CSharpCodeFixTest<DeclarePublicApiAnalyzer, DeclarePublicApiFix, XUnitVerifier>
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles = { },
                }
            };

            if (shippedApiText != null)
            {
                test.TestState.AdditionalFiles.Add((shippedApiFilePath, shippedApiText));
            }

            if (unshippedApiText != null)
            {
                test.TestState.AdditionalFiles.Add((unshippedApiFilePath, unshippedApiText));
            }

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        private async Task VerifyCSharpAdditionalFileFixAsync(string source, string shippedApiText, string oldUnshippedApiText, string newUnshippedApiText)
        {
            await VerifyAdditionalFileFixAsync(LanguageNames.CSharp, source, shippedApiText, oldUnshippedApiText, newUnshippedApiText);
        }

        private async Task VerifyAdditionalFileFixAsync(string language, string source, string shippedApiText, string oldUnshippedApiText, string newUnshippedApiText)
        {
            var test = language == LanguageNames.CSharp
                ? new CSharpCodeFixTest<DeclarePublicApiAnalyzer, DeclarePublicApiFix, XUnitVerifier>()
                : (CodeFixTest<XUnitVerifier>)new VisualBasicCodeFixTest<DeclarePublicApiAnalyzer, DeclarePublicApiFix, XUnitVerifier>();

            test.TestBehaviors |= TestBehaviors.SkipGeneratedCodeCheck;

            test.TestState.Sources.Add(source);
            test.TestState.AdditionalFiles.Add((DeclarePublicApiAnalyzer.ShippedFileName, shippedApiText));
            test.TestState.AdditionalFiles.Add((DeclarePublicApiAnalyzer.UnshippedFileName, oldUnshippedApiText));

            test.FixedState.AdditionalFiles.Add((DeclarePublicApiAnalyzer.ShippedFileName, shippedApiText));
            test.FixedState.AdditionalFiles.Add((DeclarePublicApiAnalyzer.UnshippedFileName, newUnshippedApiText));

            await test.RunAsync();
        }

        #region Diagnostic tests

        [Fact(Skip = "https://github.com/dotnet/roslyn-analyzers/issues/2622")]
        [WorkItem(2622, "https://github.com/dotnet/roslyn-analyzers/issues/2622")]
        public async Task AnalyzerFileMissing_Shipped()
        {
            var source = @"
public class C
{
    private C() { }
}
";

            string shippedText = null;
            string unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText, GetCSharpResultAt(2, 14, DeclarePublicApiAnalyzer.DeclareNewApiRule, "C"));
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn-analyzers/issues/2622")]
        [WorkItem(2622, "https://github.com/dotnet/roslyn-analyzers/issues/2622")]
        public async Task AnalyzerFileMissing_Unshipped()
        {
            var source = @"
public class C
{
    private C() { }
}
";

            string shippedText = @"";
            string unshippedText = null;

            await VerifyCSharpAsync(source, shippedText, unshippedText, GetCSharpResultAt(2, 14, DeclarePublicApiAnalyzer.DeclareNewApiRule, "C"));
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn-analyzers/issues/2622")]
        [WorkItem(2622, "https://github.com/dotnet/roslyn-analyzers/issues/2622")]
        public async Task AnalyzerFileMissing_Both()
        {
            var source = @"
public class C
{
    private C() { }
}
";

            string shippedText = null;
            string unshippedText = null;

            await VerifyCSharpAsync(source, shippedText, unshippedText, GetCSharpResultAt(2, 14, DeclarePublicApiAnalyzer.DeclareNewApiRule, "C"));
        }

        [Fact]
        public async Task EmptyPublicAPIFiles()
        {
            var source = @"";

            var shippedText = @"";
            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact]
        public async Task SimpleMissingType()
        {
            var source = @"
public class C
{
    private C() { }
}
";

            var shippedText = @"";
            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText, GetCSharpResultAt(2, 14, DeclarePublicApiAnalyzer.DeclareNewApiRule, "C"));
        }

        [Fact]
        public async Task SimpleMissingMember_CSharp()
        {
            var source = @"
public class C
{
    public int Field;
    public int Property { get; set; }
    public void Method() { } 
    public int ArrowExpressionProperty => 0;
}
";

            var shippedText = @"";
            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText,
                // Test0.cs(2,14): error RS0016: Symbol 'C' is not part of the declared API.
                GetCSharpResultAt(2, 14, DeclarePublicApiAnalyzer.DeclareNewApiRule, "C"),
                // Test0.cs(2,14): warning RS0016: Symbol 'implicit constructor for C' is not part of the declared API.
                GetCSharpResultAt(2, 14, DeclarePublicApiAnalyzer.DeclareNewApiRule, "implicit constructor for C"),
                // Test0.cs(4,16): error RS0016: Symbol 'Field' is not part of the declared API.
                GetCSharpResultAt(4, 16, DeclarePublicApiAnalyzer.DeclareNewApiRule, "Field"),
                // Test0.cs(5,27): error RS0016: Symbol 'Property.get' is not part of the declared API.
                GetCSharpResultAt(5, 27, DeclarePublicApiAnalyzer.DeclareNewApiRule, "Property.get"),
                // Test0.cs(5,32): error RS0016: Symbol 'Property.set' is not part of the declared API.
                GetCSharpResultAt(5, 32, DeclarePublicApiAnalyzer.DeclareNewApiRule, "Property.set"),
                // Test0.cs(6,17): error RS0016: Symbol 'Method' is not part of the declared API.
                GetCSharpResultAt(6, 17, DeclarePublicApiAnalyzer.DeclareNewApiRule, "Method"),
                // Test0.cs(7,43): error RS0016: Symbol 'ArrowExpressionProperty.get' is not part of the declared API.
                GetCSharpResultAt(7, 43, DeclarePublicApiAnalyzer.DeclareNewApiRule, "ArrowExpressionProperty.get"));
        }

        [Fact, WorkItem(821, "https://github.com/dotnet/roslyn-analyzers/issues/821")]
        public async Task SimpleMissingMember_Basic()
        {
            var source = @"
Imports System

Public Class C
    Public Field As Integer
    
    Public Property [Property]() As Integer
        Get
            Return m_Property
        End Get
        Set
            m_Property = Value
        End Set
    End Property
    Private m_Property As Integer

    Public Sub Method()
    End Sub

    Public ReadOnly Property ReadOnlyAutoProperty As Integer = 0
    Public Property NormalAutoProperty As Integer = 0
End Class
";

            var shippedText = @"";
            var unshippedText = @"";

            await VerifyBasicAsync(source, shippedText, unshippedText,
                // Test0.vb(4,14): warning RS0016: Symbol 'C' is not part of the declared API.
                GetBasicResultAt(4, 14, DeclarePublicApiAnalyzer.DeclareNewApiRule, "C"),
                // Test0.cs(2,14): warning RS0016: Symbol 'implicit constructor for C' is not part of the declared API.
                GetBasicResultAt(4, 14, DeclarePublicApiAnalyzer.DeclareNewApiRule, "implicit constructor for C"),
                // Test0.vb(5,12): warning RS0016: Symbol 'Field' is not part of the declared API.
                GetBasicResultAt(5, 12, DeclarePublicApiAnalyzer.DeclareNewApiRule, "Field"),
                // Test0.vb(8,9): warning RS0016: Symbol 'Property' is not part of the declared API.
                GetBasicResultAt(8, 9, DeclarePublicApiAnalyzer.DeclareNewApiRule, "Property"),
                // Test0.vb(11,9): warning RS0016: Symbol 'Property' is not part of the declared API.
                GetBasicResultAt(11, 9, DeclarePublicApiAnalyzer.DeclareNewApiRule, "Property"),
                // Test0.vb(17,16): warning RS0016: Symbol 'Method' is not part of the declared API.
                GetBasicResultAt(17, 16, DeclarePublicApiAnalyzer.DeclareNewApiRule, "Method"),
                // Test0.vb(20,30): warning RS0016: Symbol 'implicit get-accessor for ReadOnlyAutoProperty' is not part of the declared API.
                GetBasicResultAt(20, 30, DeclarePublicApiAnalyzer.DeclareNewApiRule, "implicit get-accessor for ReadOnlyAutoProperty"),
                // Test0.vb(21,21): warning RS0016: Symbol 'implicit get-accessor for NormalAutoProperty' is not part of the declared API.
                GetBasicResultAt(21, 21, DeclarePublicApiAnalyzer.DeclareNewApiRule, "implicit get-accessor for NormalAutoProperty"),
                // Test0.vb(21,21): warning RS0016: Symbol 'implicit set-accessor for NormalAutoProperty' is not part of the declared API.
                GetBasicResultAt(21, 21, DeclarePublicApiAnalyzer.DeclareNewApiRule, "implicit set-accessor for NormalAutoProperty"));
        }

        [Fact(), WorkItem(821, "https://github.com/dotnet/roslyn-analyzers/issues/821")]
        public async Task SimpleMissingMember_Basic1()
        {
            var source = @"
Imports System
Public Class C
    Private m_Property As Integer
    Public Property [Property]() As Integer
    '   Get
    '      Return m_Property
    '   End Get
    '   Set
    '       m_Property = Value
    '  End Set
    ' End Property
    Public ReadOnly Property ReadOnlyProperty0() As Integer
        Get
            Return m_Property
        End Get
    End Property
    Public WriteOnly Property WriteOnlyProperty0() As Integer
        Set
           m_Property = Value
        End Set
    End Property
    Public ReadOnly Property ReadOnlyProperty1 As Integer = 0
    Public ReadOnly Property ReadOnlyProperty2 As Integer
    Public Property Property1 As Integer
End Class
";

            var shippedText = @"
C
C.New() -> Void
C.Property() -> Integer
C.Property(AutoPropertyValue As Integer) -> Void
C.Property1() -> Integer
C.Property1(AutoPropertyValue As Integer) -> Void
C.ReadOnlyProperty0() -> Integer
C.ReadOnlyProperty1() -> Integer
C.ReadOnlyProperty2() -> Integer
C.WriteOnlyProperty0(Value As Integer) -> Void
";
            var unshippedText = @"";
            await VerifyBasicAsync(source, shippedText, unshippedText);
        }

        [Fact, WorkItem(806, "https://github.com/dotnet/roslyn-analyzers/issues/806")]
        public async Task ShippedTextWithImplicitConstructor()
        {
            var source = @"
public class C
{
    private C() { }
}
";

            var shippedText = @"
C
C -> void()";
            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText,
                // PublicAPI.Shipped.txt(3,1): warning RS0017: Symbol 'C -> void()' is part of the declared API, but is either not public or could not be found
                GetAdditionalFileResultAt(3, 1, DeclarePublicApiAnalyzer.ShippedFileName, DeclarePublicApiAnalyzer.RemoveDeletedApiRule, "C -> void()"));
        }

        [Fact, WorkItem(806, "https://github.com/dotnet/roslyn-analyzers/issues/806")]
        public async Task ShippedTextForImplicitConstructor()
        {
            var source = @"
public class C
{
}
";

            var shippedText = @"
C
C.C() -> void";
            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact, WorkItem(806, "https://github.com/dotnet/roslyn-analyzers/issues/806")]
        public async Task UnshippedTextForImplicitConstructor()
        {
            var source = @"
public class C
{
}
";

            var shippedText = @"
C";
            var unshippedText = @"
C.C() -> void";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact, WorkItem(806, "https://github.com/dotnet/roslyn-analyzers/issues/806")]
        public async Task ShippedTextWithMissingImplicitConstructor()
        {
            var source = @"
public class C
{
}
";

            var shippedText = @"
C";
            var unshippedText = @"";

            var arg = string.Format(PublicApiAnalyzerResources.PublicImplicitConstructorErrorMessageName, "C");
            await VerifyCSharpAsync(source, shippedText, unshippedText,
                // Test0.cs(2,14): warning RS0016: Symbol 'implicit constructor for C' is not part of the declared API.
                GetCSharpResultAt(2, 14, DeclarePublicApiAnalyzer.DeclareNewApiRule, arg));
        }

        [Fact, WorkItem(806, "https://github.com/dotnet/roslyn-analyzers/issues/806")]
        public async Task ShippedTextWithImplicitConstructorAndBreakingCodeChange()
        {
            var source = @"
public class C
{
    private C() { }
}
";

            var shippedText = @"
C
C.C() -> void";
            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText,
                // PublicAPI.Shipped.txt(3,1): warning RS0017: Symbol 'C.C() -> void' is part of the declared API, but is either not public or could not be found
                GetAdditionalFileResultAt(3, 1, DeclarePublicApiAnalyzer.ShippedFileName, DeclarePublicApiAnalyzer.RemoveDeletedApiRule, "C.C() -> void"));
        }

        [Fact]
        public async Task SimpleMember()
        {
            var source = @"
public class C
{
    public int Field;
    public int Property { get; set; }
    public void Method() { } 
}
";

            var shippedText = @"
C
C.C() -> void
C.Field -> int
C.Property.get -> int
C.Property.set -> void
C.Method() -> void
";
            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact]
        public async Task SplitBetweenShippedUnshipped()
        {
            var source = @"
public class C
{
    public int Field;
    public int Property { get; set; }
    public void Method() { } 
}
";

            var shippedText = @"
C
C.C() -> void
C.Field -> int
C.Property.get -> int
C.Property.set -> void
";
            var unshippedText = @"
C.Method() -> void
";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact]
        public async Task EnumSplitBetweenFiles()
        {
            var source = @"
public enum E 
{
    V1 = 1,
    V2 = 2,
    V3 = 3,
}
";

            var shippedText = @"
E
E.V1 = 1 -> E
E.V2 = 2 -> E
";

            var unshippedText = @"
E.V3 = 3 -> E
";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact]
        public async Task SimpleRemovedMember()
        {
            var source = @"
public class C
{
    public int Field;
    public int Property { get; set; }
}
";

            var shippedText = @"
C
C.C() -> void
C.Field -> int
C.Property.get -> int
C.Property.set -> void
C.Method() -> void
";

            string unshippedText = $@"
{DeclarePublicApiAnalyzer.RemovedApiPrefix}C.Method() -> void
";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact]
        public async Task ApiFileShippedWithRemoved()
        {
            var source = @"
public class C
{
    public int Field;
    public int Property { get; set; }
}
";

            string shippedText = $@"
C
C.Field -> int
C.Property.get -> int
C.Property.set -> void
{DeclarePublicApiAnalyzer.RemovedApiPrefix}C.Method() -> void
";

            string unshippedText = $@"";

            var expected = new DiagnosticResult(DeclarePublicApiAnalyzer.PublicApiFilesInvalid)
                .WithArguments(DeclarePublicApiAnalyzer.InvalidReasonShippedCantHaveRemoved);
            await VerifyCSharpAsync(source, shippedText, unshippedText, expected);
        }

        [Fact]
        [WorkItem(312, "https://github.com/dotnet/roslyn-analyzers/issues/312")]
        public async Task DuplicateSymbolInSameAPIFile()
        {
            var source = @"
public class C
{
    public int Field;
    public int Property { get; set; }
}
";

            var shippedText = @"
C
C.Field -> int
C.Property.get -> int
C.Property.set -> void
C.Property.get -> int
";

            var unshippedText = @"";

            var expected = new DiagnosticResult(DeclarePublicApiAnalyzer.DuplicateSymbolInApiFiles)
                .WithLocation(DeclarePublicApiAnalyzer.ShippedFileName, 6, 1)
                .WithLocation(DeclarePublicApiAnalyzer.ShippedFileName, 4, 1)
                .WithArguments("C.Property.get -> int");
            await VerifyCSharpAsync(source, shippedText, unshippedText, expected);
        }

        [Fact]
        [WorkItem(312, "https://github.com/dotnet/roslyn-analyzers/issues/312")]
        public async Task DuplicateSymbolInDifferentAPIFiles()
        {
            var source = @"
public class C
{
    public int Field;
    public int Property { get; set; }
}
";

            var shippedText = @"
C
C.C() -> void
C.Field -> int
C.Property.get -> int
C.Property.set -> void
";

            var unshippedText = @"
C.Property.get -> int";

            var expected = new DiagnosticResult(DeclarePublicApiAnalyzer.DuplicateSymbolInApiFiles)
                .WithLocation(DeclarePublicApiAnalyzer.UnshippedFileName, 2, 1)
                .WithLocation(DeclarePublicApiAnalyzer.ShippedFileName, 5, 1)
                .WithArguments("C.Property.get -> int");
            await VerifyCSharpAsync(source, shippedText, unshippedText, expected);
        }

        [Fact, WorkItem(773, "https://github.com/dotnet/roslyn-analyzers/issues/773")]
        public async Task ApiFileShippedWithNonExistentMembers()
        {
            // Type C has no public member "Method", but the shipped API has an entry for it.
            var source = @"
public class C
{
    public int Field;
    public int Property { get; set; }
    private void Method() { }
}
";

            string shippedText = $@"
C
C.C() -> void
C.Field -> int
C.Property.get -> int
C.Property.set -> void
C.Method() -> void
";
            string unshippedText = $@"";

            await VerifyCSharpAsync(source, shippedText, unshippedText,
                // PublicAPI.Shipped.txt(7,1): warning RS0017: Symbol 'C.Method() -> void' is part of the declared API, but is either not public or could not be found
                GetAdditionalFileResultAt(7, 1, DeclarePublicApiAnalyzer.ShippedFileName, DeclarePublicApiAnalyzer.RemoveDeletedApiRule, "C.Method() -> void"));
        }

        [Fact, WorkItem(773, "https://github.com/dotnet/roslyn-analyzers/issues/773")]
        public async Task ApiFileShippedWithNonExistentMembers_TestFullPath()
        {
            // Type C has no public member "Method", but the shipped API has an entry for it.
            var source = @"
public class C
{
    public int Field;
    public int Property { get; set; }
    private void Method() { }
}
";

            var tempPath = Path.GetTempPath();
            string shippedText = $@"
C
C.C() -> void
C.Field -> int
C.Property.get -> int
C.Property.set -> void
C.Method() -> void
";
            var shippedFilePath = Path.Combine(tempPath, DeclarePublicApiAnalyzer.ShippedFileName);

            string unshippedText = $@"";
            var unshippedFilePath = Path.Combine(tempPath, DeclarePublicApiAnalyzer.UnshippedFileName);

            await VerifyCSharpAsync(source, shippedText, unshippedText, shippedFilePath, unshippedFilePath,
                // <%TEMP_PATH%>\PublicAPI.Shipped.txt(7,1): warning RS0017: Symbol 'C.Method() -> void' is part of the declared API, but is either not public or could not be found
                GetAdditionalFileResultAt(7, 1, shippedFilePath, DeclarePublicApiAnalyzer.RemoveDeletedApiRule, "C.Method() -> void"));
        }

        [Fact]
        public async Task TypeForwardsAreProcessed1()
        {
            var source = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.StringComparison))]
";

            string shippedText = $@"
System.StringComparison (forwarded, contained in mscorlib)
System.StringComparison.CurrentCulture = 0 -> System.StringComparison (forwarded, contained in mscorlib)
System.StringComparison.CurrentCultureIgnoreCase = 1 -> System.StringComparison (forwarded, contained in mscorlib)
System.StringComparison.InvariantCulture = 2 -> System.StringComparison (forwarded, contained in mscorlib)
System.StringComparison.InvariantCultureIgnoreCase = 3 -> System.StringComparison (forwarded, contained in mscorlib)
System.StringComparison.Ordinal = 4 -> System.StringComparison (forwarded, contained in mscorlib)
System.StringComparison.OrdinalIgnoreCase = 5 -> System.StringComparison (forwarded, contained in mscorlib)
";
            string unshippedText = $@"";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact]
        public async Task TypeForwardsAreProcessed2()
        {
            var source = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.StringComparer))]
";
            string shippedText = $@"
System.StringComparer (forwarded, contained in mscorlib)
static System.StringComparer.InvariantCulture.get -> System.StringComparer (forwarded, contained in mscorlib)
static System.StringComparer.InvariantCultureIgnoreCase.get -> System.StringComparer (forwarded, contained in mscorlib)
static System.StringComparer.CurrentCulture.get -> System.StringComparer (forwarded, contained in mscorlib)
static System.StringComparer.CurrentCultureIgnoreCase.get -> System.StringComparer (forwarded, contained in mscorlib)
static System.StringComparer.Ordinal.get -> System.StringComparer (forwarded, contained in mscorlib)
static System.StringComparer.OrdinalIgnoreCase.get -> System.StringComparer (forwarded, contained in mscorlib)
static System.StringComparer.Create(System.Globalization.CultureInfo culture, bool ignoreCase) -> System.StringComparer (forwarded, contained in mscorlib)
System.StringComparer.Compare(object x, object y) -> int (forwarded, contained in mscorlib)
System.StringComparer.Equals(object x, object y) -> bool (forwarded, contained in mscorlib)
System.StringComparer.GetHashCode(object obj) -> int (forwarded, contained in mscorlib)
abstract System.StringComparer.Compare(string x, string y) -> int (forwarded, contained in mscorlib)
abstract System.StringComparer.Equals(string x, string y) -> bool (forwarded, contained in mscorlib)
abstract System.StringComparer.GetHashCode(string obj) -> int (forwarded, contained in mscorlib)
System.StringComparer.StringComparer() -> void (forwarded, contained in mscorlib)
";
            string unshippedText = $@"";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact, WorkItem(851, "https://github.com/dotnet/roslyn-analyzers/issues/851")]
        public async Task TestAvoidMultipleOverloadsWithOptionalParameters()
        {
            var source = @"
public class C
{
    // ok - single overload with optional params, 2 overloads have no public API entries.
    public void Method1(int p1, int p2, int p3 = 0) { }
    public void Method1() { }
    public void Method1(int p1, int p2) { }
    public void Method1(char p1, params int[] p2) { }

    // ok - multiple overloads with optional params, but only one is public.
    public void Method2(int p1 = 0) { }
    internal void Method2(char p1 = '0') { }
    private void Method2(string p1 = null) { }

    // ok - multiple overloads with optional params, but all are shipped.
    public void Method3(int p1 = 0) { }
    public void Method3(string p1 = null) { }

    // fire on unshipped (1) - multiple overloads with optional params, all but first are shipped.
    public void Method4(int p1 = 0) { }
    public void Method4(char p1 = 'a') { }
    public void Method4(string p1 = null) { }

    // fire on all unshipped (3) - multiple overloads with optional params, all are unshipped, 2 have unshipped entries.
    public void Method5(int p1 = 0) { }
    public void Method5(char p1 = 'a') { }
    public void Method5(string p1 = null) { }

    // ok - multiple overloads with optional params, but all have same params (differ only by generic vs non-generic).
    public object Method6(int p1 = 0) { return Method6<object>(p1); }
    public T Method6<T>(int p1 = 0) { return default(T); }
}
";

            string shippedText = $@"
C.Method3(int p1 = 0) -> void
C.Method3(string p1 = null) -> void
C.Method4(char p1 = 'a') -> void
C.Method4(string p1 = null) -> void
";
            string unshippedText = $@"
C
C.C() -> void
C.Method1() -> void
C.Method1(int p1, int p2) -> void
C.Method2(int p1 = 0) -> void
C.Method4(int p1 = 0) -> void
C.Method5(char p1 = 'a') -> void
C.Method5(string p1 = null) -> void
C.Method6(int p1 = 0) -> object
C.Method6<T>(int p1 = 0) -> T
";

            await VerifyCSharpAsync(source, shippedText, unshippedText,
                // Test0.cs(5,17): warning RS0016: Symbol 'Method1' is not part of the declared API.
                GetCSharpResultAt(5, 17, DeclarePublicApiAnalyzer.DeclareNewApiRule, "Method1"),
                // Test0.cs(8,17): warning RS0016: Symbol 'Method1' is not part of the declared API.
                GetCSharpResultAt(8, 17, DeclarePublicApiAnalyzer.DeclareNewApiRule, "Method1"),
                // Test0.cs(20,17): warning RS0026: Symbol 'Method4' violates the backcompat requirement: 'Do not add multiple overloads with optional parameters'. See 'https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                GetCSharpResultAt(20, 17, DeclarePublicApiAnalyzer.AvoidMultipleOverloadsWithOptionalParameters, "Method4", DeclarePublicApiAnalyzer.AvoidMultipleOverloadsWithOptionalParameters.HelpLinkUri),
                // Test0.cs(25,17): warning RS0016: Symbol 'Method5' is not part of the declared API.
                GetCSharpResultAt(25, 17, DeclarePublicApiAnalyzer.DeclareNewApiRule, "Method5"),
                // Test0.cs(25,17): warning RS0026: Symbol 'Method5' violates the backcompat requirement: 'Do not add multiple overloads with optional parameters'. See 'https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                GetCSharpResultAt(25, 17, DeclarePublicApiAnalyzer.AvoidMultipleOverloadsWithOptionalParameters, "Method5", DeclarePublicApiAnalyzer.AvoidMultipleOverloadsWithOptionalParameters.HelpLinkUri),
                // Test0.cs(26,17): warning RS0026: Symbol 'Method5' violates the backcompat requirement: 'Do not add multiple overloads with optional parameters'. See 'https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                GetCSharpResultAt(26, 17, DeclarePublicApiAnalyzer.AvoidMultipleOverloadsWithOptionalParameters, "Method5", DeclarePublicApiAnalyzer.AvoidMultipleOverloadsWithOptionalParameters.HelpLinkUri),
                // Test0.cs(27,17): warning RS0026: Symbol 'Method5' violates the backcompat requirement: 'Do not add multiple overloads with optional parameters'. See 'https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                GetCSharpResultAt(27, 17, DeclarePublicApiAnalyzer.AvoidMultipleOverloadsWithOptionalParameters, "Method5", DeclarePublicApiAnalyzer.AvoidMultipleOverloadsWithOptionalParameters.HelpLinkUri));
        }

        [Fact, WorkItem(851, "https://github.com/dotnet/roslyn-analyzers/issues/851")]
        public async Task TestOverloadWithOptionalParametersShouldHaveMostParameters()
        {
            var source = @"
public class C
{
    // ok - single overload with optional params has most parameters.
    public void Method1(int p1, int p2, int p3 = 0) { }
    public void Method1() { }
    public void Method1(int p1, int p2) { }
    public void Method1(char p1, params int[] p2) { }

    // ok - multiple overloads with optional params violating most params requirement, but only one is public.
    public void Method2(int p1 = 0) { }
    internal void Method2(int p1, char p2 = '0') { }
    private void Method2(string p1 = null) { }

    // ok - multiple overloads with optional params violating most params requirement, but all are shipped.
    public void Method3(int p1 = 0) { }
    public void Method3(string p1 = null) { }
    public void Method3(int p1, int p2) { }

    // fire on unshipped (1) - single overload with optional params and violating most params requirement.
    public void Method4(int p1 = 0) { }     // unshipped
    public void Method4(char p1, int p2) { }        // unshipped
    public void Method4(string p1, int p2) { }      // unshipped

    // fire on shipped (1) - single shipped overload with optional params and violating most params requirement due to a new unshipped API.
    public void Method5(int p1 = 0) { }     // shipped
    public void Method5(char p1) { }        // shipped
    public void Method5(string p1) { }      // unshipped

    // fire on multiple shipped (2) - multiple shipped overloads with optional params and violating most params requirement due to a new unshipped API
    public void Method6(int p1 = 0) { }     // shipped
    public void Method6(char p1 = 'a') { }  // shipped
    public void Method6(string p1) { }      // unshipped
}
";

            string shippedText = $@"
C.Method3(int p1 = 0) -> void
C.Method3(int p1, int p2) -> void
C.Method3(string p1 = null) -> void
C.Method5(char p1) -> void
C.Method5(int p1 = 0) -> void
C.Method6(char p1 = 'a') -> void
C.Method6(int p1 = 0) -> void
";
            string unshippedText = $@"
C
C.C() -> void
C.Method1() -> void
C.Method1(char p1, params int[] p2) -> void
C.Method1(int p1, int p2) -> void
C.Method1(int p1, int p2, int p3 = 0) -> void
C.Method2(int p1 = 0) -> void
C.Method4(char p1, int p2) -> void
C.Method4(int p1 = 0) -> void
C.Method4(string p1, int p2) -> void
C.Method5(string p1) -> void
C.Method6(string p1) -> void
";

            await VerifyCSharpAsync(source, shippedText, unshippedText,
                // Test0.cs(21,17): warning RS0027: Symbol 'Method4' violates the backcompat requirement: 'Public API with optional parameter(s) should have the most parameters amongst its public overloads'. See 'https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                GetCSharpResultAt(21, 17, DeclarePublicApiAnalyzer.OverloadWithOptionalParametersShouldHaveMostParameters, "Method4", DeclarePublicApiAnalyzer.OverloadWithOptionalParametersShouldHaveMostParameters.HelpLinkUri),
                // Test0.cs(26,17): warning RS0027: Symbol 'Method5' violates the backcompat requirement: 'Public API with optional parameter(s) should have the most parameters amongst its public overloads'. See 'https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                GetCSharpResultAt(26, 17, DeclarePublicApiAnalyzer.OverloadWithOptionalParametersShouldHaveMostParameters, "Method5", DeclarePublicApiAnalyzer.OverloadWithOptionalParametersShouldHaveMostParameters.HelpLinkUri),
                // Test0.cs(31,17): warning RS0027: Symbol 'Method6' violates the backcompat requirement: 'Public API with optional parameter(s) should have the most parameters amongst its public overloads'. See 'https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                GetCSharpResultAt(31, 17, DeclarePublicApiAnalyzer.OverloadWithOptionalParametersShouldHaveMostParameters, "Method6", DeclarePublicApiAnalyzer.OverloadWithOptionalParametersShouldHaveMostParameters.HelpLinkUri),
                // Test0.cs(32,17): warning RS0027: Symbol 'Method6' violates the backcompat requirement: 'Public API with optional parameter(s) should have the most parameters amongst its public overloads'. See 'https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                GetCSharpResultAt(32, 17, DeclarePublicApiAnalyzer.OverloadWithOptionalParametersShouldHaveMostParameters, "Method6", DeclarePublicApiAnalyzer.OverloadWithOptionalParametersShouldHaveMostParameters.HelpLinkUri));
        }

        #endregion

        #region Fix tests

        [Fact]
        public async Task TestSimpleMissingMember_Fix()
        {
            var source = @"
public class C
{
    public int Field;
    public int Property { get; set; }
    public void Method() { } 
    public int ArrowExpressionProperty => 0;

    public int {|RS0016:NewField|}; // Newly added field, not in current public API.
}
";

            var shippedText = @"";
            var unshippedText = @"C
C.ArrowExpressionProperty.get -> int
C.C() -> void
C.Field -> int
C.Method() -> void
C.Property.get -> int
C.Property.set -> void";
            var fixedUnshippedText = @"C
C.ArrowExpressionProperty.get -> int
C.C() -> void
C.Field -> int
C.Method() -> void
C.NewField -> int
C.Property.get -> int
C.Property.set -> void";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestAddAndRemoveMembers_CSharp_Fix()
        {
            // Unshipped file has a state 'ObsoleteField' entry and a missing 'NewField' entry.
            var source = @"
public class C
{
    public int Field;
    public int Property { get; set; }
    public void Method() { } 
    public int ArrowExpressionProperty => 0;

    public int {|RS0016:NewField|};
}
";
            var shippedText = @"";
            var unshippedText = @"C
C.ArrowExpressionProperty.get -> int
C.C() -> void
C.Field -> int
C.Method() -> void
{|RS0017:C.ObsoleteField -> int|}
C.Property.get -> int
C.Property.set -> void";
            var fixedUnshippedText = @"C
C.ArrowExpressionProperty.get -> int
C.C() -> void
C.Field -> int
C.Method() -> void
C.NewField -> int
C.Property.get -> int
C.Property.set -> void";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestSimpleMissingType_Fix()
        {
            var source = @"
public class {|RS0016:C|}
{
    private C() { }
}
";

            var shippedText = @"";
            var unshippedText = @"";
            var fixedUnshippedText = @"C";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestMultipleMissingTypeAndMember_Fix()
        {
            var source = @"
public class {|RS0016:C|}
{
    private C() { }
    public int {|RS0016:Field|};
}

public class {|RS0016:{|RS0016:C2|}|} { }
";

            var shippedText = @"";
            var unshippedText = @"";
            var fixedUnshippedText = @"C
C.Field -> int
C2
C2.C2() -> void";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestChangingMethodSignatureForAnUnshippedMethod_Fix()
        {
            var source = @"
public class C
{
    private C() { }
    public void {|RS0016:Method|}(int p1){ }
}
";

            var shippedText = @"C";
            // previously method had no params, so the fix should remove the previous overload.
            var unshippedText = @"{|RS0017:C.Method() -> void|}";
            var fixedUnshippedText = @"C.Method(int p1) -> void";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestChangingMethodSignatureForAnUnshippedMethodWithShippedOverloads_Fix()
        {
            var source = @"
public class C
{
    private C() { }
    public void Method(int p1){ }
    public void Method(int p1, int p2){ }
    public void {|RS0016:Method|}(char p1){ }
}
";

            var shippedText = @"C
C.Method(int p1) -> void
C.Method(int p1, int p2) -> void";
            // previously method had no params, so the fix should remove the previous overload.
            var unshippedText = @"{|RS0017:C.Method() -> void|}";
            var fixedUnshippedText = @"C.Method(char p1) -> void";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestAddingNewPublicOverload_Fix()
        {
            var source = @"
public class C
{
    private C() { }
    public void {|RS0016:Method|}(){ }
    internal void Method(int p1){ }
    internal void Method(int p1, int p2){ }
    public void Method(char p1){ }
}
";

            var shippedText = @"";
            var unshippedText = @"C
C.Method(char p1) -> void";
            var fixedUnshippedText = @"C
C.Method() -> void
C.Method(char p1) -> void";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestMissingTypeAndMemberAndNestedMembers_Fix()
        {
            var source = @"
public class {|RS0016:C|}
{
    private C() { }
    public int {|RS0016:Field|};

    public class CC
    {
        public int {|RS0016:Field|};
    }
}

public class {|RS0016:{|RS0016:C2|}|} { }
";

            var shippedText = @"C.CC
C.CC.CC() -> void";
            var unshippedText = @"";
            var fixedUnshippedText = @"C
C.CC.Field -> int
C.Field -> int
C2
C2.C2() -> void";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestMissingNestedGenericMembersAndStaleMembers_Fix()
        {
            var source = @"
public class {|RS0016:C|}
{
    private C() { }
    public CC<int> {|RS0016:Field|};
    private C3.C4 Field2;
    private C3.C4 Method(C3.C4 p1) { throw new System.NotImplementedException(); }

    public class CC<T>
    {
        public int {|RS0016:Field|};
        public CC<int> {|RS0016:Field2|};
    }
    
    public class C3
    {
        public class C4 { }
    }
}

public class {|RS0016:{|RS0016:C2|}|} { }
";

            var shippedText = @"";
            var unshippedText = @"C.C3
C.C3.C3() -> void
C.C3.C4
C.C3.C4.C4() -> void
C.CC<T>
C.CC<T>.CC() -> void
{|RS0017:C.Field2 -> C.C3.C4|}
{|RS0017:C.Method(C.C3.C4 p1) -> C.C3.C4|}
";
            var fixedUnshippedText = @"C
C.C3
C.C3.C3() -> void
C.C3.C4
C.C3.C4.C4() -> void
C.CC<T>
C.CC<T>.CC() -> void
C.CC<T>.Field -> int
C.CC<T>.Field2 -> C.CC<int>
C.Field -> C.CC<int>
C2
C2.C2() -> void
";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestWithExistingUnshippedNestedMembers_Fix()
        {
            var source = @"
public class {|RS0016:C|}
{
    private C() { }
    public int {|RS0016:Field|};

    public class CC
    {
        public int Field;
    }
}

public class {|RS0016:{|RS0016:C2|}|} { }
";

            var shippedText = @"";
            var unshippedText = @"C.CC
C.CC.CC() -> void
C.CC.Field -> int";
            var fixedUnshippedText = @"C
C.CC
C.CC.CC() -> void
C.CC.Field -> int
C.Field -> int
C2
C2.C2() -> void";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestWithExistingUnshippedNestedGenericMembers_Fix()
        {
            var source = @"
public class C
{
    private C() { }
    public class {|RS0016:CC|}
    {
        public int Field;
    }

    public class CC<T>
    {
        private CC() { }
        public int Field;
    }
}
";

            var shippedText = @"";
            var unshippedText = @"C
C.CC
C.CC.Field -> int
C.CC<T>
C.CC<T>.Field -> int";
            var fixedUnshippedText = @"C
C.CC
C.CC.CC() -> void
C.CC.Field -> int
C.CC<T>
C.CC<T>.Field -> int";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestWithExistingShippedNestedMembers_Fix()
        {
            var source = @"
public class {|RS0016:C|}
{
    private C() { }
    public int {|RS0016:Field|};

    public class CC
    {
        public int Field;
    }
}

public class {|RS0016:{|RS0016:C2|}|} { }
";

            var shippedText = @"C.CC
C.CC.CC() -> void
C.CC.Field -> int";
            var unshippedText = @"";
            var fixedUnshippedText = @"C
C.Field -> int
C2
C2.C2() -> void";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestOnlyRemoveStaleSiblingEntries_Fix()
        {
            var source = @"
public class {|RS0016:C|}
{
    private C() { }
    public int {|RS0016:Field|};

    public class CC
    {
        private int Field; // This has a stale public API entry, but this shouldn't be removed unless we attempt to add a public API entry for a sibling.
    }
}

public class {|RS0016:{|RS0016:C2|}|} { }
";

            var shippedText = @"";
            var unshippedText = @"
C.CC
C.CC.CC() -> void
{|RS0017:C.CC.Field -> int|}";
            var fixedUnshippedText = @"C
C.CC
C.CC.CC() -> void
{|RS0017:C.CC.Field -> int|}
C.Field -> int
C2
C2.C2() -> void";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("\r\n", "\r\n")]
        [InlineData("\r\n\r\n", "\r\n")]
        public async Task TestPreserveTrailingNewline(string originalEndOfFile, string expectedEndOfFile)
        {
            var source = @"
public class C
{
    public int Property { get; }

    public int {|RS0016:NewField|}; // Newly added field, not in current public API.
}
";

            var shippedText = @"";
            var unshippedText = $@"C
C.C() -> void
C.Property.get -> int{originalEndOfFile}";
            var fixedUnshippedText = $@"C
C.C() -> void
C.NewField -> int
C.Property.get -> int{expectedEndOfFile}";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task MissingType_A()
        {
            var source = @"
public class {|RS0016:{|RS0016:A|}|} { }
public class B { }
public class D { }
";

            var unshippedText = @"B
B.B() -> void
D
D.D() -> void";

            var expectedUnshippedText = @"A
A.A() -> void
B
B.B() -> void
D
D.D() -> void";
            await VerifyCSharpAdditionalFileFixAsync(source, shippedApiText: "", oldUnshippedApiText: unshippedText, newUnshippedApiText: expectedUnshippedText);
        }

        [Fact]
        public async Task MissingType_C()
        {
            var source = @"
public class B { }
public class {|RS0016:{|RS0016:C|}|} { }
public class D { }
";

            var unshippedText = @"B
B.B() -> void
D
D.D() -> void";

            var expectedUnshippedText = @"B
B.B() -> void
C
C.C() -> void
D
D.D() -> void";
            await VerifyCSharpAdditionalFileFixAsync(source, shippedApiText: "", oldUnshippedApiText: unshippedText, newUnshippedApiText: expectedUnshippedText);
        }

        [Fact]
        public async Task MissingType_E()
        {
            var source = @"
public class B { }
public class D { }
public class {|RS0016:{|RS0016:E|}|} { }
";

            var unshippedText = @"B
B.B() -> void
D
D.D() -> void";

            var expectedUnshippedText = @"B
B.B() -> void
D
D.D() -> void
E
E.E() -> void";
            await VerifyCSharpAdditionalFileFixAsync(source, shippedApiText: "", oldUnshippedApiText: unshippedText, newUnshippedApiText: expectedUnshippedText);
        }

        [Fact]
        public async Task MissingType_Unordered_A()
        {
            var source = @"
public class {|RS0016:{|RS0016:A|}|} { }
public class B { }
public class D { }
";

            var unshippedText = @"D
D.D() -> void
B
B.B() -> void";

            var expectedUnshippedText = @"A
A.A() -> void
D
D.D() -> void
B
B.B() -> void";
            await VerifyCSharpAdditionalFileFixAsync(source, shippedApiText: "", oldUnshippedApiText: unshippedText, newUnshippedApiText: expectedUnshippedText);
        }

        [Fact]
        public async Task MissingType_Unordered_C()
        {
            var source = @"
public class B { }
public class {|RS0016:{|RS0016:C|}|} { }
public class D { }
";

            var unshippedText = @"D
D.D() -> void
B
B.B() -> void";

            var expectedUnshippedText = @"C
C.C() -> void
D
D.D() -> void
B
B.B() -> void";
            await VerifyCSharpAdditionalFileFixAsync(source, shippedApiText: "", oldUnshippedApiText: unshippedText, newUnshippedApiText: expectedUnshippedText);
        }

        [Fact]
        public async Task MissingType_Unordered_E()
        {
            var source = @"
public class B { }
public class D { }
public class {|RS0016:{|RS0016:E|}|} { }
";

            var unshippedText = @"D
D.D() -> void
B
B.B() -> void";

            var expectedUnshippedText = @"D
D.D() -> void
B
B.B() -> void
E
E.E() -> void";
            await VerifyCSharpAdditionalFileFixAsync(source, shippedApiText: "", oldUnshippedApiText: unshippedText, newUnshippedApiText: expectedUnshippedText);
        }

        #endregion
    }
}
