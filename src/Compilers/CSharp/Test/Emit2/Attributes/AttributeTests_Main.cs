// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_Main : CSharpTestBase
    {
        [Fact]
        public void TestMainAttributes()
        {
            var source = CreateCompilation(@"
using System;

[main: My(""one"")]
Console.WriteLine(""Hello World"");

public class MyAttribute : Attribute { public MyAttribute(string name) {} }
");

            var mainAttrs = source.GetEntryPoint(default)!.GetAttributes();

            var attribute = Assert.Single(mainAttrs);
            attribute.VerifyValue(0, TypedConstantKind.Primitive, "one");
            Assert.Equal(@"MyAttribute(""one"")", attribute.ToString());

            CompileAndVerify(source, expectedOutput: "Hello World");
        }

        [Fact]
        public void TestMultipleMainAttributes()
        {
            var source = CreateCompilation(@"
using System;

[main: My(""one"")]
[main: My(""two"")]
[main: My(""three"")]
Console.WriteLine(""Hello World"");

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class MyAttribute : Attribute { public MyAttribute(string name) {} }
");

            var mainAttrs = source.GetEntryPoint(default).GetAttributes();

            Assert.Collection(mainAttrs,
                verifyAttribute("one"),
                verifyAttribute("two"),
                verifyAttribute("three"));

            CompileAndVerify(source, expectedOutput: "Hello World");

            static Action<CSharpAttributeData> verifyAttribute(string expectedValue) => (attribute) =>
            {
                attribute.VerifyValue(0, TypedConstantKind.Primitive, expectedValue);
                Assert.Equal($@"MyAttribute(""{expectedValue}"")", attribute.ToString());
            };
        }

        [Fact]
        public void TestMainAttributesWithAssemblyAndModuleAttributes()
        {
            var source = CreateCompilation(@"
using System;

[main: My(""one"")]
[assembly: My(""two"")]
[module: My(""three"")]
Console.WriteLine(""Hello World"");

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class MyAttribute : Attribute { public MyAttribute(string name) {} }
");

            verifySingleAttribute(source.GetEntryPoint(default), "one");
            verifySingleAttribute(source.Assembly, "two");
            verifySingleAttribute(source.Assembly.Modules[0], "three");

            CompileAndVerify(source, expectedOutput: "Hello World");

            static void verifySingleAttribute(Symbol symbol, string expectedValue)
            {
                var attributes = symbol.GetAttributes();
                var attribute = Assert.Single(attributes);

                attribute.VerifyValue(0, TypedConstantKind.Primitive, expectedValue);
                Assert.Equal($@"MyAttribute(""{expectedValue}"")", attribute.ToString());
            };
        }

        [Fact]
        public void TestMainAttributesInADifferentFile()
        {
            var source = CreateCompilation(new[] { @"
using System;
Console.WriteLine(""Hello World"");

public class MyAttribute : Attribute { public MyAttribute(string name) {} }
",
@" 
[main: My(""one"")]
" }
            );

            var mainAttrs = source.GetEntryPoint(default).GetAttributes();

            var attribute = Assert.Single(mainAttrs);
            attribute.VerifyValue(0, TypedConstantKind.Primitive, "one");
            Assert.Equal(@"MyAttribute(""one"")", attribute.ToString());

            CompileAndVerify(source, expectedOutput: "Hello World");
        }

        [Fact]
        public void TestMainAttributesAfterCode()
        {
            var source = CreateCompilation(@"
using System;
Console.WriteLine(""Hello World"");

[main: My(""one"")]
public class MyAttribute : Attribute { public MyAttribute(string name) {} }
");
            source.VerifyDiagnostics(
                // (5,2): error CS1730: Assembly, module, and main attributes must precede all other elements defined in a file except using clauses and extern alias declarations
                // [main: My("one")]
                Diagnostic(ErrorCode.ERR_GlobalAttributesNotFirst, "main").WithLocation(5, 2)
                );
        }

        [Theory]
        [InlineData("All", true)]
        [InlineData("Method", true)]
        [InlineData("Class", false)]
        [InlineData("Field", false)]
        [InlineData("Assembly", false)]
        [InlineData("Module", false)]
        public void TestMainAttributesWithSpecificLocation(string location, bool valid)
        {
            var source = CreateCompilation($@"
using System;

[main: My(""one"")]
Console.WriteLine(""Hello World"");

[AttributeUsage(AttributeTargets.{location}, AllowMultiple = true)]
public class MyAttribute : Attribute {{ public MyAttribute(string name) {{}} }}
");

            source.VerifyDiagnostics(valid ? Array.Empty<DiagnosticDescription>() :
                new[]
                {
                    // (4,8): error CS0592: Attribute 'My' is not valid on this declaration type. It is only valid on '...' declarations.
                    // [main: My("one")]
                    Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "My").WithArguments("My", location.ToLower()).WithLocation(4, 8)
                });
        }

        [Fact]
        public void TestMainAttributesInScript()
        {
            var compilation = CreateSubmission(@"
using System;
[main: My(""one"")]

public class C {}
public class MyAttribute : Attribute { public MyAttribute(string name) {} }",
            parseOptions: TestOptions.Script.WithLanguageVersion(LanguageVersion.Preview));

            compilation.VerifyDiagnostics(
                // (3,2): error CS7026: Assembly and module attributes are not allowed in this context
                // [main: My("one")]
                Diagnostic(ErrorCode.ERR_GlobalAttributesNotAllowed, "main").WithLocation(3, 2)
                );
        }
    }
}
