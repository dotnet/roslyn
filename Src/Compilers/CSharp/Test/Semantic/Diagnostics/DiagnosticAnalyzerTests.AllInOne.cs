// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
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

            // AllInOneCSharpCode has no properties with initializers or named types with primary constructors.
            var symbolKindsWithNoCodeBlocks = new HashSet<SymbolKind>();
            symbolKindsWithNoCodeBlocks.Add(SymbolKind.Property);
            symbolKindsWithNoCodeBlocks.Add(SymbolKind.NamedType);

            var analyzer = new CSharpTrackingDiagnosticAnalyzer();
            CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental)).VerifyAnalyzerDiagnostics(new[] { analyzer });
            analyzer.VerifyAllAnalyzerMembersWereCalled();
            analyzer.VerifyAnalyzeSymbolCalledForAllSymbolKinds();
            analyzer.VerifyAnalyzeNodeCalledForAllSyntaxKinds();
            analyzer.VerifyOnCodeBlockCalledForAllSymbolAndMethodKinds(symbolKindsWithNoCodeBlocks);
        }

        [WorkItem(896075, "DevDiv")]
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
            CreateExperimentalCompilationWithMscorlib45(source).VerifyAnalyzerDiagnostics(new[] { new CSharpTrackingDiagnosticAnalyzer() });
        }

        [Fact]
        public void DiagnosticAnalyzerExpressionBodiedProperty()
        {
            var comp = CreateExperimentalCompilationWithMscorlib45(@"
public class C
{
    public int P => 10;
}").VerifyAnalyzerDiagnostics(new[] { new CSharpTrackingDiagnosticAnalyzer() });
        }

#endregion

        [Fact]
        public void AnalyzerDriverIsSafeAgainstAnalyzerExceptions()
        {
            var compilation = CreateCompilationWithMscorlib45(TestResource.AllInOneCSharpCode, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));
            ThrowingDiagnosticAnalyzer<SyntaxKind>.VerifyAnalyzerEngineIsSafeAgainstExceptions(analyzer => 
                compilation.GetCSharpAnalyzerDiagnostics(new[] { analyzer }, null, CodeAnalysis.DiagnosticExtensions.AlwaysCatchAnalyzerExceptions), typeof(AnalyzerDriver).Name);
        }

        [Fact]
        public void AnalyzerOptionsArePassedToAllAnalyzers()
        {
            AnalyzerOptions options = new AnalyzerOptions
            (
                new[] { new AdditionalFileStream("myfilepath") }.ToImmutableArray<AdditionalStream>(),
                new Dictionary<string, string> { { "optionName", "optionValue" } }.ToImmutableDictionary()
            );

            var compilation = CreateCompilationWithMscorlib45(TestResource.AllInOneCSharpCode, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));
            var analyzer = new OptionsDiagnosticAnalyzer<SyntaxKind>(options);
            compilation.GetCSharpAnalyzerDiagnostics(new[] { analyzer }, options);
            analyzer.VerifyAnalyzerOptions();
        }
    }
}
