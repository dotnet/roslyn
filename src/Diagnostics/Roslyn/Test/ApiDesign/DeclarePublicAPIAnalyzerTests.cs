// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Diagnostics.Analyzers;
using Roslyn.Diagnostics.Analyzers.ApiDesign;
using Roslyn.Diagnostics.Analyzers.CSharp.ApiDesign;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.ApiDesign
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

        private DeclarePublicAPIAnalyzer CreateAnalyzer(string shippedApiText = "", string unshippedApiText = "")
        {
            var shippedText = new TestAdditionalText(DeclarePublicAPIAnalyzer.ShippedFileName, shippedApiText);
            var unshippedText = new TestAdditionalText(DeclarePublicAPIAnalyzer.UnshippedFileName, unshippedApiText);
            var array = ImmutableArray.Create<AdditionalText>(shippedText, unshippedText);
            return new DeclarePublicAPIAnalyzer(array);
        }

        private void VerifyCSharp(string source, string shippedApiText, string unshippedApiText, params DiagnosticResult[] expected)
        {
            var analyzer = CreateAnalyzer(shippedApiText, unshippedApiText);
            Verify(source, LanguageNames.CSharp, analyzer, expected);
        }

        [Fact]
        public void SimpleMissingType()
        {
            var source = @"
public class C
{
}
";

            var shippedText = @"";
            var unshippedText = @"";

            VerifyCSharp(source, shippedText, unshippedText, GetCSharpResultAt(2, 14, DeclarePublicAPIAnalyzer.DeclareNewApiRule, "C"));
        }

        [Fact]
        public void SimpleMissingMember()
        {
            var source = @"
public class C
{
    public int Field;
    public int Property { get; set; }
    public void Method() { } 
}
";

            var shippedText = @"";
            var unshippedText = @"";

            VerifyCSharp(source, shippedText, unshippedText,
                // Test0.cs(2,14): error RS0016: Symbol 'C' is not part of the declared API.
                GetCSharpResultAt(2, 14, DeclarePublicAPIAnalyzer.DeclareNewApiRule, "C"),
                // Test0.cs(4,16): error RS0016: Symbol 'Field' is not part of the declared API.
                GetCSharpResultAt(4, 16, DeclarePublicAPIAnalyzer.DeclareNewApiRule, "Field"),
                // Test0.cs(5,27): error RS0016: Symbol 'Property.get' is not part of the declared API.
                GetCSharpResultAt(5, 27, DeclarePublicAPIAnalyzer.DeclareNewApiRule, "Property.get"),
                // Test0.cs(5,32): error RS0016: Symbol 'Property.set' is not part of the declared API.
                GetCSharpResultAt(5, 32, DeclarePublicAPIAnalyzer.DeclareNewApiRule, "Property.set"),
                // Test0.cs(6,17): error RS0016: Symbol 'Method' is not part of the declared API.
                GetCSharpResultAt(6, 17, DeclarePublicAPIAnalyzer.DeclareNewApiRule, "Method"));
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
C.Field -> int
C.Property.get -> int
C.Property.set -> void
";

            var unshippedText = $@"
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

            var shippedText = $@"
C
C.Field -> int
C.Property.get -> int
C.Property.set -> void
{DeclarePublicAPIAnalyzer.RemovedApiPrefix}C.Method() -> void
";

            var unshippedText = $@"";

            VerifyCSharp(source, shippedText, unshippedText,
                // error RS0024: The contents of the public API files are invalid: The shipped API file can't have removed members
                GetGlobalResult(DeclarePublicAPIAnalyzer.PublicApiFilesInvalid, DeclarePublicAPIAnalyzer.InvalidReasonShippedCantHaveRemoved));
        }
    }
}
