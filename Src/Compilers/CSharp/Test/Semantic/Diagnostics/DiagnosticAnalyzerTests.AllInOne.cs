// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class DiagnosticAnalyzerTests
    {
        [Fact(Skip="906237")]
        public void DiagnosticAnalyzerAllInOne()
        {
            var source = TestResource.AllInOneCSharpCode;
            var analyzer = new CSharpTrackingDiagnosticAnalyzer();
            CreateCompilationWithMscorlib45(source).VerifyAnalyzerDiagnostics(new[] { analyzer });
            analyzer.VerifyAllInterfaceMembersWereCalled();
            analyzer.VerifyAnalyzeSymbolCalledForAllSymbolKinds();
            analyzer.VerifyAnalyzeNodeCalledForAllSyntaxKinds();
            analyzer.VerifyAnalyzeCodeBlockCalledForAllSymbolAndMethodKinds();
        }

        [WorkItem(896075)]
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

        [Fact]
        public void AnalyzerDriverIsSafeAgainstAnalyzerExceptions()
        {
            var compilation = CreateCompilationWithMscorlib45(TestResource.AllInOneCSharpCode);
            ThrowingDiagnosticAnalyzer<SyntaxKind>.VerifyAnalyzerEngineIsSafeAgainstExceptions(compilation);
        }

        class CSharpTrackingDiagnosticAnalyzer : TrackingDiagnosticAnalyzer<SyntaxKind>
        {
            static readonly Regex omittedSyntaxKindRegex =
                new Regex(@"Using|Extern|Parameter|Constraint|Specifier|Initializer|Global|Method|Destructor");

            protected override bool IsAnalyzeCodeBlockSupported(SymbolKind symbolKind, MethodKind methodKind, bool returnsVoid)
            {
                return base.IsAnalyzeCodeBlockSupported(symbolKind, methodKind, returnsVoid) && methodKind != MethodKind.EventRaise;
            }

            protected override bool IsAnalyzeNodeSupported(SyntaxKind syntaxKind)
            {
                return base.IsAnalyzeNodeSupported(syntaxKind) && !omittedSyntaxKindRegex.IsMatch(syntaxKind.ToString());
            }
        }
    }
}
