// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class DiagnosticAnalyzerTests
    {
        [Fact]
        public void DiagnosticAnalyzerAllInOne()
        {
            var source = TestResource.AllInOneCSharpCode;

            // AllInOneCSharpCode has no properties with initializers/attributes.
            var symbolKindsWithNoCodeBlocks = new HashSet<SymbolKind>();
            symbolKindsWithNoCodeBlocks.Add(SymbolKind.Property);

            // Add nodes that are not yet in AllInOneCSharpCode to this list.
            var missingSyntaxKinds = new HashSet<SyntaxKind>();

            var analyzer = new CSharpTrackingDiagnosticAnalyzer();
            CreateCompilationWithMscorlib45(source).VerifyAnalyzerDiagnostics(new[] { analyzer });
            analyzer.VerifyAllAnalyzerMembersWereCalled();
            analyzer.VerifyAnalyzeSymbolCalledForAllSymbolKinds();
            analyzer.VerifyAnalyzeNodeCalledForAllSyntaxKinds(missingSyntaxKinds);
            analyzer.VerifyOnCodeBlockCalledForAllSymbolAndMethodKinds(symbolKindsWithNoCodeBlocks);
        }

        [WorkItem(896075, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/896075")]
        [Fact]
        public void DiagnosticAnalyzerIndexerDeclaration()
        {
            var source = @"
public class C
{
    public string this[int index]
    {
        get { return string.Empty; }
        set { value = value + string.Empty; }
    }
}
";
            CreateCompilationWithMscorlib45(source).VerifyAnalyzerDiagnostics(new[] { new CSharpTrackingDiagnosticAnalyzer() });
        }

        // AllInOne does not include experimental features.
        #region Experimental Features

        [Fact]
        public void DiagnosticAnalyzerConditionalAccess()
        {
            var source = @"
public class C
{
    public string this[int index]
    {
        get { return string.Empty ?. ToString() ?[1] .ToString() ; }
        set { value = value + string.Empty; }
    }
}
";
            CreateCompilationWithMscorlib45(source).VerifyAnalyzerDiagnostics(new[] { new CSharpTrackingDiagnosticAnalyzer() });
        }

        [Fact]
        public void DiagnosticAnalyzerExpressionBodiedProperty()
        {
            var comp = CreateCompilationWithMscorlib45(@"
public class C
{
    public int P => 10;
}").VerifyAnalyzerDiagnostics(new[] { new CSharpTrackingDiagnosticAnalyzer() });
        }

        #endregion

        [Fact]
        [WorkItem(759, "https://github.com/dotnet/roslyn/issues/759")]
        public void AnalyzerDriverIsSafeAgainstAnalyzerExceptions()
        {
            var compilation = CreateCompilationWithMscorlib45(TestResource.AllInOneCSharpCode);
            ThrowingDiagnosticAnalyzer<SyntaxKind>.VerifyAnalyzerEngineIsSafeAgainstExceptions(analyzer =>
                compilation.GetAnalyzerDiagnostics(new[] { analyzer }, null, logAnalyzerExceptionAsDiagnostics: true));
        }

        [Fact]
        public void AnalyzerOptionsArePassedToAllAnalyzers()
        {
            var text = new StringText(string.Empty, encodingOpt: null);
            AnalyzerOptions options = new AnalyzerOptions
            (
                new[] { new TestAdditionalText("myfilepath", text) }.ToImmutableArray<AdditionalText>()
            );

            var compilation = CreateCompilationWithMscorlib45(TestResource.AllInOneCSharpCode);
            var analyzer = new OptionsDiagnosticAnalyzer<SyntaxKind>(options);
            compilation.GetAnalyzerDiagnostics(new[] { analyzer }, options);
            analyzer.VerifyAnalyzerOptions();
        }

        private sealed class TestAdditionalText : AdditionalText
        {
            private readonly SourceText _text;

            public TestAdditionalText(string path, SourceText text)
            {
                this.Path = path;
                _text = text;
            }

            public override string Path { get; }

            public override SourceText GetText(CancellationToken cancellationToken = default(CancellationToken)) => _text;
        }
    }
}
