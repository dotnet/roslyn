// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class PatternParsingTexts : CSharpTestBase
    {
        [Fact]
        public void CasePatternVersusFeatureFlag()
        {
            var test = @"
class C 
{
    public static void Main(string[] args)
    {
        switch ((int) args[0][0])
        {
            case 1:
            case 2 when args.Length == 2:
            case 1<<2:
            case string s:
            default:
                break;
        }
        bool b = args[0] is string s;
    }
}
";
            CreateCompilationWithMscorlib(test).VerifyDiagnostics(
                // (9,13): error CS8058: Feature 'pattern matching' is experimental and unsupported; use '/features:patterns' to enable.
                //             case 2 when args.Length == 2:
                Diagnostic(ErrorCode.ERR_FeatureIsExperimental, "case 2 when args.Length == 2:").WithArguments("pattern matching", "patterns").WithLocation(9, 13),
                // (11,13): error CS8058: Feature 'pattern matching' is experimental and unsupported; use '/features:patterns' to enable.
                //             case string s:
                Diagnostic(ErrorCode.ERR_FeatureIsExperimental, "case string s:").WithArguments("pattern matching", "patterns").WithLocation(11, 13),
                // (15,18): error CS8058: Feature 'pattern matching' is experimental and unsupported; use '/features:patterns' to enable.
                //         bool b = args[0] is string s;
                Diagnostic(ErrorCode.ERR_FeatureIsExperimental, "args[0] is string s").WithArguments("pattern matching", "patterns").WithLocation(15, 18)
            );
        }
    }
}
