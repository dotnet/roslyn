// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using System.Threading;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class PatternMatchingTests : CSharpTestBase
    {
        private static CSharpParseOptions patternParseOptions =
            TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6).WithFeature("patterns", "true");

        [Fact]
        public void SimplePatternTest()
        {
            var source =
@"using System;
public class X
{
    public static void Main()
    {
        var s = nameof(Main);
        if (s is string t) Console.WriteLine(t);
        Console.WriteLine(null is string t ? t : nameof(X));
    }
}";
            var expectedOutput =
@"Main
X";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            compilation.VerifyDiagnostics();
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void PatternScopeErrors()
        {
            var source =
@"using System;
public class X
{
    public static void Main()
    {
        var s = nameof(Main);
        if (s is string t) { } else Console.WriteLine(t);
        if (null is dynamic t) { } else Console.WriteLine(t);
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            compilation.VerifyDiagnostics(
                // (7,55): error CS0103: The name 't' does not exist in the current context
                //         if (s is string t) { } else Console.WriteLine(t);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "t").WithArguments("t").WithLocation(7, 55),
                // (8,59): error CS0103: The name 't' does not exist in the current context
                //         if (null is dynamic t) { } else Console.WriteLine(t);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "t").WithArguments("t").WithLocation(8, 59)
                );
        }
    }
}
