// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ExtensionEverythingTests : CompilingTestBase
    {
        private static readonly CSharpParseOptions parseOptions = TestOptions.Regular.WithExtensionEverythingFeature();

        [Fact]
        public void SuccessTest()
        {
            var text = @"
#define __DEMO__

class Bar
{
}

extension class Foo
{
    public static void Ext(Bar one)
    {
        System.Console.WriteLine(""Hello, world!"");
    }
}

class Program
{
    static void Main(string[] args)
    {
        new Bar().Ext();
    }
}
";

            var comp = CreateCompilationWithMscorlibAndSystemCore(text, parseOptions: parseOptions);
            comp.VerifyDiagnostics();
            CompileAndVerify(source: text,
                parseOptions: parseOptions,
                additionalRefs: new[] { SystemCoreRef }, expectedOutput: "");
        }

        [Fact]
        public void DiagnosticTest()
        {
            var text = @"
partial extension class Foo {
}
";

            var comp = CreateCompilationWithMscorlibAndSystemCore(text);
            comp.VerifyDiagnostics(
                );
        }
    }
}
