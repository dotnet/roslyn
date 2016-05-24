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

        // TODO(t-evhau): Test extending struct/interface/other odd types

        [Fact]
        public void SuccessTest()
        {
            var text = @"
#define __DEMO__

class Bar
{
}

extension class Foo : Bar
{
    public void Ext()
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
                additionalRefs: new[] { SystemCoreRef }, expectedOutput: "Hello, world!");
        }

        [Fact]
        public void ExtensionMethodInExtensionClass()
        {
            var text = @"
class Base
{
}

extension class Ext : Base {
    public static void ExtMethod(this Base param)
    {
    }
}
";

            var comp = CreateCompilationWithMscorlibAndSystemCore(text, parseOptions: parseOptions);
            comp.VerifyDiagnostics(
                // (7,24): error CS8207: An extension method cannot be defined in an extension class.
                //     public static void ExtMethod(this Base param)
                Diagnostic(ErrorCode.ERR_ExtensionMethodInExtensionClass, "ExtMethod").WithLocation(7, 24)
            );
        }
    }
}
