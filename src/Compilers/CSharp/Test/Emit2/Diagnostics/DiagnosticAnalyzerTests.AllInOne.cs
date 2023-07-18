// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
            // https://github.com/dotnet/roslyn/issues/44682 Add to all in one
            missingSyntaxKinds.Add(SyntaxKind.WithExpression);
            missingSyntaxKinds.Add(SyntaxKind.RecordDeclaration);
            missingSyntaxKinds.Add(SyntaxKind.CollectionExpression);
            missingSyntaxKinds.Add(SyntaxKind.ExpressionElement);
            missingSyntaxKinds.Add(SyntaxKind.SpreadElement);

            var analyzer = new CSharpTrackingDiagnosticAnalyzer();
            var options = new AnalyzerOptions(new[] { new TestAdditionalText() }.ToImmutableArray<AdditionalText>());
            CreateCompilationWithMscorlib45(source).VerifyAnalyzerDiagnostics(new[] { analyzer }, options);
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
            var options = new AnalyzerOptions(new[] { new TestAdditionalText() }.ToImmutableArray<AdditionalText>());

            ThrowingDiagnosticAnalyzer<SyntaxKind>.VerifyAnalyzerEngineIsSafeAgainstExceptions(analyzer =>
                compilation.GetAnalyzerDiagnostics(new[] { analyzer }, options));
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
    }
}
