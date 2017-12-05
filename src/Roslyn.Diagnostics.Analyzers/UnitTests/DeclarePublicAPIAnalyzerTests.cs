// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Test.Utilities;
using Xunit;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class DeclarePublicAPIAnalyzerTests : CodeFixTestBase
    {
        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new DeclarePublicAPIFix();
        }

        protected override CodeFixProvider GetBasicCodeFixProvider()
        {
            return new DeclarePublicAPIFix();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new DeclarePublicAPIAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new DeclarePublicAPIAnalyzer();
        }

        private TestAdditionalDocument GetShippedAdditionalTextFile(string shippedApiText = "", string shippedApiFilePath = DeclarePublicAPIAnalyzer.ShippedFileName)
        {
            return new TestAdditionalDocument(
                filePath: shippedApiFilePath,
                fileName: DeclarePublicAPIAnalyzer.ShippedFileName,
                text: shippedApiText);
        }

        private TestAdditionalDocument GetUnshippedAdditionalTextFile(string unshippedApiText = "", string unshippedApiFilePath = DeclarePublicAPIAnalyzer.UnshippedFileName)
        {
            return new TestAdditionalDocument(
                filePath: unshippedApiFilePath,
                fileName: DeclarePublicAPIAnalyzer.UnshippedFileName,
                text: unshippedApiText);
        }

        private IEnumerable<TestAdditionalDocument> GetAdditionalTextFiles(
            string shippedApiText = "",
            string unshippedApiText = "",
            string shippedApiFilePath = DeclarePublicAPIAnalyzer.ShippedFileName,
            string unshippedApiFilePath = DeclarePublicAPIAnalyzer.UnshippedFileName)
        {
            yield return GetShippedAdditionalTextFile(shippedApiText, shippedApiFilePath);
            yield return GetUnshippedAdditionalTextFile(unshippedApiText, unshippedApiFilePath);
        }

        private void VerifyBasic(string source, string shippedApiText, string unshippedApiText, params DiagnosticResult[] expected)
        {
            var additionalFiles = GetAdditionalTextFiles(shippedApiText, unshippedApiText);
            Verify(source, LanguageNames.VisualBasic, GetBasicDiagnosticAnalyzer(), additionalFiles, compilationOptions: null, expected: expected);
        }

        private void VerifyCSharp(string source, string shippedApiText, string unshippedApiText, params DiagnosticResult[] expected)
        {
            var additionalFiles = GetAdditionalTextFiles(shippedApiText, unshippedApiText);
            Verify(source, LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), additionalFiles, compilationOptions: null, expected: expected);
        }

        private void VerifyCSharp(string source, string shippedApiText, string unshippedApiText, string shippedApiFilePath, string unshippedApiFilePath, params DiagnosticResult[] expected)
        {
            var additionalFiles = GetAdditionalTextFiles(shippedApiText, unshippedApiText, shippedApiFilePath, unshippedApiFilePath);
            Verify(source, LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), additionalFiles, compilationOptions: null, expected: expected);
        }

        private void VerifyCSharpAdditionalFileFix(string source, string shippedApiText, string oldUnshippedApiText, string newUnshippedApiText, bool onlyFixFirstFixableDiagnostic = false)
        {
            VerifyAdditionalFileFix(LanguageNames.CSharp, source, shippedApiText, oldUnshippedApiText, newUnshippedApiText, onlyFixFirstFixableDiagnostic);
        }

        private void VerifyAdditionalFileFix(string language, string source, string shippedApiText, string oldUnshippedApiText, string newUnshippedApiText, bool onlyFixFirstFixableDiagnostic)
        {
            var analyzer = language == LanguageNames.CSharp ? GetCSharpDiagnosticAnalyzer() : GetBasicDiagnosticAnalyzer();
            var fixer = language == LanguageNames.CSharp ? GetCSharpCodeFixProvider() : GetBasicCodeFixProvider();
            var additionalFiles = GetAdditionalTextFiles(shippedApiText, oldUnshippedApiText);
            var newAdditionalFileToVerify = GetUnshippedAdditionalTextFile(newUnshippedApiText);
            VerifyAdditionalFileFix(language, analyzer, fixer, source, additionalFiles, newAdditionalFileToVerify, onlyFixFirstFixableDiagnostic: onlyFixFirstFixableDiagnostic);
        }

        #region Diagnostic tests

        [Fact]
        public void SimpleMissingType()
        {
            var source = @"
public class C
{
    private C() { }
}
";

            var shippedText = @"";
            var unshippedText = @"";

            VerifyCSharp(source, shippedText, unshippedText, GetCSharpResultAt(2, 14, DeclarePublicAPIAnalyzer.DeclareNewApiRule, "C"));
        }

        [Fact]
        public void SimpleMissingMember_CSharp()
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

            VerifyCSharp(source, shippedText, unshippedText,
                // Test0.cs(2,14): error RS0016: Symbol 'C' is not part of the declared API.
                GetCSharpResultAt(2, 14, DeclarePublicAPIAnalyzer.DeclareNewApiRule, "C"),
                // Test0.cs(2,14): warning RS0016: Symbol 'implicit constructor for C' is not part of the declared API.
                GetCSharpResultAt(2, 14, DeclarePublicAPIAnalyzer.DeclareNewApiRule, "implicit constructor for C"),
                // Test0.cs(4,16): error RS0016: Symbol 'Field' is not part of the declared API.
                GetCSharpResultAt(4, 16, DeclarePublicAPIAnalyzer.DeclareNewApiRule, "Field"),
                // Test0.cs(5,27): error RS0016: Symbol 'Property.get' is not part of the declared API.
                GetCSharpResultAt(5, 27, DeclarePublicAPIAnalyzer.DeclareNewApiRule, "Property.get"),
                // Test0.cs(5,32): error RS0016: Symbol 'Property.set' is not part of the declared API.
                GetCSharpResultAt(5, 32, DeclarePublicAPIAnalyzer.DeclareNewApiRule, "Property.set"),
                // Test0.cs(6,17): error RS0016: Symbol 'Method' is not part of the declared API.
                GetCSharpResultAt(6, 17, DeclarePublicAPIAnalyzer.DeclareNewApiRule, "Method"),
                // Test0.cs(7,43): error RS0016: Symbol 'ArrowExpressionProperty.get' is not part of the declared API.
                GetCSharpResultAt(7, 43, DeclarePublicAPIAnalyzer.DeclareNewApiRule, "ArrowExpressionProperty.get"));
        }

        [Fact(Skip = "821"), WorkItem(821, "https://github.com/dotnet/roslyn-analyzers/issues/821")]
        public void SimpleMissingMember_Basic()
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

    Public ReadOnly Property ReadOnlyProperty As Integer = 0
End Class
";

            var shippedText = @"";
            var unshippedText = @"";

            VerifyBasic(source, shippedText, unshippedText,
                // Test0.vb(4,14): warning RS0016: Symbol 'C' is not part of the declared API.
                GetBasicResultAt(4, 14, DeclarePublicAPIAnalyzer.DeclareNewApiRule, "C"),
                // Test0.vb(5,12): warning RS0016: Symbol 'Field' is not part of the declared API.
                GetBasicResultAt(5, 12, DeclarePublicAPIAnalyzer.DeclareNewApiRule, "Field"),
                // Test0.vb(8,9): warning RS0016: Symbol 'Property' is not part of the declared API.
                GetBasicResultAt(8, 9, DeclarePublicAPIAnalyzer.DeclareNewApiRule, "Property"),
                // Test0.vb(11,9): warning RS0016: Symbol 'Property' is not part of the declared API.
                GetBasicResultAt(11, 9, DeclarePublicAPIAnalyzer.DeclareNewApiRule, "Property"),
                // Test0.vb(17,16): warning RS0016: Symbol 'Method' is not part of the declared API.
                GetBasicResultAt(17, 16, DeclarePublicAPIAnalyzer.DeclareNewApiRule, "Method"),
                // Test0.vb(17,60): warning RS0016: Symbol 'ReadOnlyProperty' is not part of the declared API.
                GetBasicResultAt(20, 60, DeclarePublicAPIAnalyzer.DeclareNewApiRule, "ReadOnlyProperty"));
        }

        [Fact, WorkItem(806, "https://github.com/dotnet/roslyn-analyzers/issues/806")]
        public void ShippedTextWithImplicitConstructor()
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

            VerifyCSharp(source, shippedText, unshippedText,
                // PublicAPI.Shipped.txt(3,1): warning RS0017: Symbol 'C -> void()' is part of the declared API, but is either not public or could not be found
                GetAdditionalFileResultAt(3, 1, DeclarePublicAPIAnalyzer.ShippedFileName, DeclarePublicAPIAnalyzer.RemoveDeletedApiRule, "C -> void()"));
        }

        [Fact, WorkItem(806, "https://github.com/dotnet/roslyn-analyzers/issues/806")]
        public void ShippedTextForImplicitConstructor()
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

            VerifyCSharp(source, shippedText, unshippedText);
        }

        [Fact, WorkItem(806, "https://github.com/dotnet/roslyn-analyzers/issues/806")]
        public void UnshippedTextForImplicitConstructor()
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

            VerifyCSharp(source, shippedText, unshippedText);
        }

        [Fact, WorkItem(806, "https://github.com/dotnet/roslyn-analyzers/issues/806")]
        public void ShippedTextWithMissingImplicitConstructor()
        {
            var source = @"
public class C
{
}
";

            var shippedText = @"
C";
            var unshippedText = @"";

            var arg = string.Format(RoslynDiagnosticsAnalyzersResources.PublicImplicitConstructorErrorMessageName, "C");
            VerifyCSharp(source, shippedText, unshippedText,
                // Test0.cs(2,14): warning RS0016: Symbol 'implicit constructor for C' is not part of the declared API.
                GetCSharpResultAt(2, 14, DeclarePublicAPIAnalyzer.DeclareNewApiRule, arg));
        }

        [Fact, WorkItem(806, "https://github.com/dotnet/roslyn-analyzers/issues/806")]
        public void ShippedTextWithImplicitConstructorAndBreakingCodeChange()
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

            VerifyCSharp(source, shippedText, unshippedText,
                // PublicAPI.Shipped.txt(3,1): warning RS0017: Symbol 'C.C() -> void' is part of the declared API, but is either not public or could not be found
                GetAdditionalFileResultAt(3, 1, DeclarePublicAPIAnalyzer.ShippedFileName, DeclarePublicAPIAnalyzer.RemoveDeletedApiRule, "C.C() -> void"));
        }

        [Fact]
        public void SimpleMember()
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

            VerifyCSharp(source, shippedText, unshippedText);
        }

        [Fact]
        public void SplitBetweenShippedUnshipped()
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

            VerifyCSharp(source, shippedText, unshippedText);
        }

        [Fact]
        public void EnumSplitBetweenFiles()
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

            VerifyCSharp(source, shippedText, unshippedText);
        }

        [Fact]
        public void SimpleRemovedMember()
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
{DeclarePublicAPIAnalyzer.RemovedApiPrefix}C.Method() -> void
";

            VerifyCSharp(source, shippedText, unshippedText);
        }

        [Fact]
        public void ApiFileShippedWithRemoved()
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
{DeclarePublicAPIAnalyzer.RemovedApiPrefix}C.Method() -> void
";

            string unshippedText = $@"";

            VerifyCSharp(source, shippedText, unshippedText,
                // error RS0024: The contents of the public API files are invalid: The shipped API file can't have removed members
                GetGlobalResult(DeclarePublicAPIAnalyzer.PublicApiFilesInvalid, DeclarePublicAPIAnalyzer.InvalidReasonShippedCantHaveRemoved));
        }

        [Fact]
        [WorkItem(312, "https://github.com/dotnet/roslyn-analyzers/issues/312")]
        public void DuplicateSymbolInSameAPIFile()
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

            VerifyCSharp(source, shippedText, unshippedText,
                // Warning RS0025: The symbol 'C.Property.get -> int' appears more than once in the public API files.
                GetResultAt(
                    DeclarePublicAPIAnalyzer.ShippedFileName,
                    DeclarePublicAPIAnalyzer.DuplicateSymbolInApiFiles.Id,
                    string.Format(DeclarePublicAPIAnalyzer.DuplicateSymbolInApiFiles.MessageFormat.ToString(), "C.Property.get -> int"),
                    DeclarePublicAPIAnalyzer.ShippedFileName + "(6,1)",
                    DeclarePublicAPIAnalyzer.ShippedFileName + "(4,1)"));
        }

        [Fact]
        [WorkItem(312, "https://github.com/dotnet/roslyn-analyzers/issues/312")]
        public void DuplicateSymbolInDifferentAPIFiles()
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

            VerifyCSharp(source, shippedText, unshippedText,
                // Warning RS0025: The symbol 'C.Property.get -> int' appears more than once in the public API files.
                GetResultAt(
                    DeclarePublicAPIAnalyzer.ShippedFileName,
                    DeclarePublicAPIAnalyzer.DuplicateSymbolInApiFiles.Id,
                    string.Format(DeclarePublicAPIAnalyzer.DuplicateSymbolInApiFiles.MessageFormat.ToString(), "C.Property.get -> int"),
                    DeclarePublicAPIAnalyzer.UnshippedFileName + "(2,1)",
                    DeclarePublicAPIAnalyzer.ShippedFileName + "(5,1)"));
        }

        [Fact, WorkItem(773, "https://github.com/dotnet/roslyn-analyzers/issues/773")]
        public void ApiFileShippedWithNonExistentMembers()
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

            VerifyCSharp(source, shippedText, unshippedText,
                // PublicAPI.Shipped.txt(7,1): warning RS0017: Symbol 'C.Method() -> void' is part of the declared API, but is either not public or could not be found
                GetAdditionalFileResultAt(7, 1, DeclarePublicAPIAnalyzer.ShippedFileName, DeclarePublicAPIAnalyzer.RemoveDeletedApiRule, "C.Method() -> void"));
        }

        [Fact, WorkItem(773, "https://github.com/dotnet/roslyn-analyzers/issues/773")]
        public void ApiFileShippedWithNonExistentMembers_TestFullPath()
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
            var shippedFilePath = Path.Combine(tempPath, DeclarePublicAPIAnalyzer.ShippedFileName);

            string unshippedText = $@"";
            var unshippedFilePath = Path.Combine(tempPath, DeclarePublicAPIAnalyzer.UnshippedFileName);

            VerifyCSharp(source, shippedText, unshippedText, shippedFilePath, unshippedFilePath,
                // <%TEMP_PATH%>\PublicAPI.Shipped.txt(7,1): warning RS0017: Symbol 'C.Method() -> void' is part of the declared API, but is either not public or could not be found
                GetAdditionalFileResultAt(7, 1, shippedFilePath, DeclarePublicAPIAnalyzer.RemoveDeletedApiRule, "C.Method() -> void"));
        }


        [Fact]
        public void TypeForwardsAreProcessed()
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

            VerifyCSharp(source, shippedText, unshippedText);
        }

        [Fact, WorkItem(851, "https://github.com/dotnet/roslyn-analyzers/issues/851")]
        public void TestAvoidMultipleOverloadsWithOptionalParameters()
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

            VerifyCSharp(source, shippedText, unshippedText,
                // Test0.cs(5,17): warning RS0016: Symbol 'Method1' is not part of the declared API.
                GetCSharpResultAt(5, 17, DeclarePublicAPIAnalyzer.DeclareNewApiRule, "Method1"),
                // Test0.cs(8,17): warning RS0016: Symbol 'Method1' is not part of the declared API.
                GetCSharpResultAt(8, 17, DeclarePublicAPIAnalyzer.DeclareNewApiRule, "Method1"),
                // Test0.cs(20,17): warning RS0026: Symbol 'Method4' violates the backcompat requirement: 'Do not add multiple overloads with optional parameters'. See 'https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                GetCSharpResultAt(20, 17, DeclarePublicAPIAnalyzer.AvoidMultipleOverloadsWithOptionalParameters, "Method4", DeclarePublicAPIAnalyzer.AvoidMultipleOverloadsWithOptionalParameters.HelpLinkUri),
                // Test0.cs(25,17): warning RS0016: Symbol 'Method5' is not part of the declared API.
                GetCSharpResultAt(25, 17, DeclarePublicAPIAnalyzer.DeclareNewApiRule, "Method5"),
                // Test0.cs(25,17): warning RS0026: Symbol 'Method5' violates the backcompat requirement: 'Do not add multiple overloads with optional parameters'. See 'https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                GetCSharpResultAt(25, 17, DeclarePublicAPIAnalyzer.AvoidMultipleOverloadsWithOptionalParameters, "Method5", DeclarePublicAPIAnalyzer.AvoidMultipleOverloadsWithOptionalParameters.HelpLinkUri),
                // Test0.cs(26,17): warning RS0026: Symbol 'Method5' violates the backcompat requirement: 'Do not add multiple overloads with optional parameters'. See 'https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                GetCSharpResultAt(26, 17, DeclarePublicAPIAnalyzer.AvoidMultipleOverloadsWithOptionalParameters, "Method5", DeclarePublicAPIAnalyzer.AvoidMultipleOverloadsWithOptionalParameters.HelpLinkUri),
                // Test0.cs(27,17): warning RS0026: Symbol 'Method5' violates the backcompat requirement: 'Do not add multiple overloads with optional parameters'. See 'https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                GetCSharpResultAt(27, 17, DeclarePublicAPIAnalyzer.AvoidMultipleOverloadsWithOptionalParameters, "Method5", DeclarePublicAPIAnalyzer.AvoidMultipleOverloadsWithOptionalParameters.HelpLinkUri));
        }

        [Fact, WorkItem(851, "https://github.com/dotnet/roslyn-analyzers/issues/851")]
        public void TestOverloadWithOptionalParametersShouldHaveMostParameters()
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

            VerifyCSharp(source, shippedText, unshippedText,
                // Test0.cs(21,17): warning RS0027: Symbol 'Method4' violates the backcompat requirement: 'Public API with optional parameter(s) should have the most parameters amongst its public overloads'. See 'https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                GetCSharpResultAt(21, 17, DeclarePublicAPIAnalyzer.OverloadWithOptionalParametersShouldHaveMostParameters, "Method4", DeclarePublicAPIAnalyzer.OverloadWithOptionalParametersShouldHaveMostParameters.HelpLinkUri),
                // Test0.cs(26,17): warning RS0027: Symbol 'Method5' violates the backcompat requirement: 'Public API with optional parameter(s) should have the most parameters amongst its public overloads'. See 'https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                GetCSharpResultAt(26, 17, DeclarePublicAPIAnalyzer.OverloadWithOptionalParametersShouldHaveMostParameters, "Method5", DeclarePublicAPIAnalyzer.OverloadWithOptionalParametersShouldHaveMostParameters.HelpLinkUri),
                // Test0.cs(31,17): warning RS0027: Symbol 'Method6' violates the backcompat requirement: 'Public API with optional parameter(s) should have the most parameters amongst its public overloads'. See 'https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                GetCSharpResultAt(31, 17, DeclarePublicAPIAnalyzer.OverloadWithOptionalParametersShouldHaveMostParameters, "Method6", DeclarePublicAPIAnalyzer.OverloadWithOptionalParametersShouldHaveMostParameters.HelpLinkUri),
                // Test0.cs(32,17): warning RS0027: Symbol 'Method6' violates the backcompat requirement: 'Public API with optional parameter(s) should have the most parameters amongst its public overloads'. See 'https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                GetCSharpResultAt(32, 17, DeclarePublicAPIAnalyzer.OverloadWithOptionalParametersShouldHaveMostParameters, "Method6", DeclarePublicAPIAnalyzer.OverloadWithOptionalParametersShouldHaveMostParameters.HelpLinkUri));
        }

        #endregion

        #region Fix tests

        [Fact]
        public void TestSimpleMissingMember_Fix()
        {
            var source = @"
public class C
{
    public int Field;
    public int Property { get; set; }
    public void Method() { } 
    public int ArrowExpressionProperty => 0;

    public int NewField; // Newly added field, not in current public API.
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

            VerifyCSharpAdditionalFileFix(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public void TestAddAndRemoveMembers_CSharp_Fix()
        {
            // Unshipped file has a state 'ObsoleteField' entry and a missing 'NewField' entry.
            var source = @"
public class C
{
    public int Field;
    public int Property { get; set; }
    public void Method() { } 
    public int ArrowExpressionProperty => 0;

    public int NewField;
}
";
            var shippedText = @"";
            var unshippedText = @"C
C.ArrowExpressionProperty.get -> int
C.C() -> void
C.Field -> int
C.Method() -> void
C.ObsoleteField -> int
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

            VerifyCSharpAdditionalFileFix(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public void TestSimpleMissingType_Fix()
        {
            var source = @"
public class C
{
    private C() { }
}
";

            var shippedText = @"";
            var unshippedText = @"";
            var fixedUnshippedText = @"C";

            VerifyCSharpAdditionalFileFix(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public void TestMultipleMissingTypeAndMember_Fix()
        {
            var source = @"
public class C
{
    private C() { }
    public int Field;
}

public class C2 { }
";

            var shippedText = @"";
            var unshippedText = @"";
            var fixedUnshippedText = @"C
C.Field -> int
C2
C2.C2() -> void";

            VerifyCSharpAdditionalFileFix(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public void TestChangingMethodSignatureForAnUnshippedMethod_Fix()
        {
            var source = @"
public class C
{
    private C() { }
    public void Method(int p1){ }
}
";

            var shippedText = @"C";
            // previously method had no params, so the fix should remove the previous overload.
            var unshippedText = @"C.Method() -> void";
            var fixedUnshippedText = @"C.Method(int p1) -> void";

            VerifyCSharpAdditionalFileFix(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public void TestChangingMethodSignatureForAnUnshippedMethodWithShippedOverloads_Fix()
        {
            var source = @"
public class C
{
    private C() { }
    public void Method(int p1){ }
    public void Method(int p1, int p2){ }
    public void Method(char p1){ }
}
";

            var shippedText = @"C
C.Method(int p1) -> void
C.Method(int p1, int p2) -> void";
            // previously method had no params, so the fix should remove the previous overload.
            var unshippedText = @"C.Method() -> void";
            var fixedUnshippedText = @"C.Method(char p1) -> void";

            VerifyCSharpAdditionalFileFix(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public void TestAddingNewPublicOverload_Fix()
        {
            var source = @"
public class C
{
    private C() { }
    public void Method(){ }
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

            VerifyCSharpAdditionalFileFix(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public void TestMissingTypeAndMemberAndNestedMembers_Fix()
        {
            var source = @"
public class C
{
    private C() { }
    public int Field;

    public class CC
    {
        public int Field;
    }
}

public class C2 { }
";

            var shippedText = @"C.CC
C.CC.CC() -> void";
            var unshippedText = @"";
            var fixedUnshippedText = @"C
C.CC.Field -> int
C.Field -> int
C2
C2.C2() -> void";

            VerifyCSharpAdditionalFileFix(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public void TestMissingNestedGenericMembersAndStaleMembers_Fix()
        {
            var source = @"
public class C
{
    private C() { }
    public CC<int> Field;
    private C3.C4 Field2;
    private C3.C4 Method(C3.C4 p1) { throw new System.NotImplementedException(); }

    public class CC<T>
    {
        public int Field;
        public CC<int> Field2;
    }
    
    public class C3
    {
        public class C4 { }
    }
}

public class C2 { }
";

            var shippedText = @"";
            var unshippedText = @"C.C3
C.C3.C3() -> void
C.C3.C4
C.C3.C4.C4() -> void
C.CC<T>
C.CC<T>.CC() -> void
C.Field2 -> C.C3.C4
C.Method(C.C3.C4 p1) -> C.C3.C4
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

            VerifyCSharpAdditionalFileFix(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public void TestWithExistingUnshippedNestedMembers_Fix()
        {
            var source = @"
public class C
{
    private C() { }
    public int Field;

    public class CC
    {
        public int Field;
    }
}

public class C2 { }
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

            VerifyCSharpAdditionalFileFix(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public void TestWithExistingUnshippedNestedGenericMembers_Fix()
        {
            var source = @"
public class C
{
    private C() { }
    public class CC
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

            VerifyCSharpAdditionalFileFix(source, shippedText, unshippedText, fixedUnshippedText, onlyFixFirstFixableDiagnostic: true);
        }

        [Fact]
        public void TestWithExistingShippedNestedMembers_Fix()
        {
            var source = @"
public class C
{
    private C() { }
    public int Field;

    public class CC
    {
        public int Field;
    }
}

public class C2 { }
";

            var shippedText = @"C.CC
C.CC.CC() -> void
C.CC.Field -> int";
            var unshippedText = @"";
            var fixedUnshippedText = @"C
C.Field -> int
C2
C2.C2() -> void";

            VerifyCSharpAdditionalFileFix(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public void TestOnlyRemoveStaleSiblingEntries_Fix()
        {
            var source = @"
public class C
{
    private C() { }
    public int Field;

    public class CC
    {
        private int Field; // This has a stale public API entry, but this shouldn't be removed unless we attempt to add a public API entry for a sibling.
    }
}

public class C2 { }
";

            var shippedText = @"";
            var unshippedText = @"
C.CC
C.CC.CC() -> void
C.CC.Field -> int";
            var fixedUnshippedText = @"C
C.CC
C.CC.CC() -> void
C.CC.Field -> int
C.Field -> int
C2
C2.C2() -> void";

            VerifyCSharpAdditionalFileFix(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("\r\n", "\r\n")]
        [InlineData("\r\n\r\n", "\r\n")]
        public void TestPreserveTrailingNewline(string originalEndOfFile, string expectedEndOfFile)
        {
            var source = @"
public class C
{
    public int Property { get; }

    public int NewField; // Newly added field, not in current public API.
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

            VerifyCSharpAdditionalFileFix(source, shippedText, unshippedText, fixedUnshippedText);
        }

        #endregion
    }
}
