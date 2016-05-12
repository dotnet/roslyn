// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Diagnostics.Test.Utilities;
using Xunit;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class DeclarePublicAPIAnalyzerTests : CodeFixTestBase
    {
        private sealed class TestAdditionalText : AdditionalText
        {
            private readonly StringText _text;

            public TestAdditionalText(string path, string text)
            {
                this.Path = path;
                _text = new StringText(text, encodingOpt: null);
            }

            public override string Path { get; }

            public override SourceText GetText(CancellationToken cancellationToken = default(CancellationToken)) => _text;
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return null;
        }

        protected override CodeFixProvider GetBasicCodeFixProvider()
        {
            return null;
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return null;
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return null;
        }

        private DeclarePublicAPIAnalyzer CreateAnalyzer(string shippedApiText = "", string unshippedApiText = "", string shippedApiFilePath = null, string unshippedApiFilePath = null)
        {
            var shippedText = new TestAdditionalText(shippedApiFilePath ?? DeclarePublicAPIAnalyzer.ShippedFileName, shippedApiText);
            var unshippedText = new TestAdditionalText(unshippedApiFilePath ?? DeclarePublicAPIAnalyzer.UnshippedFileName, unshippedApiText);
            ImmutableArray<AdditionalText> array = ImmutableArray.Create<AdditionalText>(shippedText, unshippedText);
            return new DeclarePublicAPIAnalyzer(array);
        }

        private void VerifyBasic(string source, string shippedApiText, string unshippedApiText, params DiagnosticResult[] expected)
        {
            DeclarePublicAPIAnalyzer analyzer = CreateAnalyzer(shippedApiText, unshippedApiText);
            Verify(source, LanguageNames.VisualBasic, analyzer, expected);
        }

        private void VerifyCSharp(string source, string shippedApiText, string unshippedApiText, params DiagnosticResult[] expected)
        {
            DeclarePublicAPIAnalyzer analyzer = CreateAnalyzer(shippedApiText, unshippedApiText);
            Verify(source, LanguageNames.CSharp, analyzer, expected);
        }

        private void VerifyCSharp(string source, string shippedApiText, string unshippedApiText, string shippedApiFilePath, string unshippedApiFilePath, params DiagnosticResult[] expected)
        {
            DeclarePublicAPIAnalyzer analyzer = CreateAnalyzer(shippedApiText, unshippedApiText, shippedApiFilePath, unshippedApiFilePath);
            Verify(source, LanguageNames.CSharp, analyzer, expected);
        }

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

            var arg = string.Format(RoslynDiagnosticsAnalyzersResources.PublicImplicitConstructorErroMessageName, "C");
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

            var arg = string.Format(RoslynDiagnosticsAnalyzersResources.PublicImplicitConstructorErroMessageName, "C");
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

            var arg = string.Format(RoslynDiagnosticsAnalyzersResources.PublicImplicitConstructorErroMessageName, "C");
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

            var arg = string.Format(RoslynDiagnosticsAnalyzersResources.PublicImplicitConstructorErroMessageName, "C");
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
    }
}
