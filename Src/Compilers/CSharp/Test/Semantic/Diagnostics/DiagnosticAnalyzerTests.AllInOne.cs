// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
            var analyzer = new CSharpTrackingDiagnosticAnalyzer();
            CreateCompilationWithMscorlib45(source).VerifyAnalyzerDiagnostics(new[] { analyzer });
            analyzer.VerifyAllInterfaceMembersWereCalled();
            analyzer.VerifyAnalyzeSymbolCalledForAllSymbolKinds();
            analyzer.VerifyAnalyzeNodeCalledForAllSyntaxKinds();
            analyzer.VerifyOnCodeBlockCalledForAllSymbolAndMethodKinds();

            analyzer = new CSharpTrackingDiagnosticAnalyzer();
            CreateCompilationWithMscorlib45(source).VerifyAnalyzerDiagnostics3(new[] { analyzer });
            analyzer.VerifyAllInterfaceMembersWereCalled();
            analyzer.VerifyAnalyzeSymbolCalledForAllSymbolKinds();
            analyzer.VerifyAnalyzeNodeCalledForAllSyntaxKinds();
            analyzer.VerifyOnCodeBlockCalledForAllSymbolAndMethodKinds();
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

        [Fact]
        public void AnalyzerDriverIsSafeAgainstAnalyzerExceptions()
        {
            var compilation = CreateCompilationWithMscorlib45(TestResource.AllInOneCSharpCode);
            ThrowingDiagnosticAnalyzer<SyntaxKind>.VerifyAnalyzerEngineIsSafeAgainstExceptions(analyzer => 
                AnalyzerDriver.GetDiagnostics(compilation, new[] { analyzer }, CancellationToken.None), typeof(AnalyzerDriver).Name);
        }
    }
}
