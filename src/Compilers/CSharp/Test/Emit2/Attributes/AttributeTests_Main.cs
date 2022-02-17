// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_Main : CSharpTestBase
    {
        [Fact]
        public void TestMainAttributes()
        {
            var source = @"
using System;

[main: My(""one"")]
Console.WriteLine(""Hello World"");

public class MyAttribute : Attribute { public MyAttribute(string name) {} }
";

            CompileAndVerify(source,
                options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: verifyAttributes,
                sourceSymbolValidator: verifyAttributes,
                expectedOutput: "Hello World");

            static void verifyAttributes(ModuleSymbol module)
            {
                var attributes = GetSimpleEntryPointAttributes(module);
                var attribute = Assert.Single(attributes);

                attribute.VerifyValue(0, TypedConstantKind.Primitive, "one");
                Assert.Equal(@"MyAttribute(""one"")", attribute.ToString());
            }
        }

        [Fact]
        public void TestMultipleMainAttributes()
        {
            var source = @"
using System;

[main: My(""one"")]
[main: My(""two"")]
[main: My(""three"")]
Console.WriteLine(""Hello World"");

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class MyAttribute : Attribute { public MyAttribute(string name) {} }
";
            CompileAndVerify(source,
                options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: verifyAttributes,
                sourceSymbolValidator: verifyAttributes,
                expectedOutput: "Hello World");

            static void verifyAttributes(ModuleSymbol module)
            {
                var attributes = GetSimpleEntryPointAttributes(module);

                Assert.Collection(attributes,
                    verifyAttribute("one"),
                    verifyAttribute("two"),
                    verifyAttribute("three"));

                static Action<CSharpAttributeData> verifyAttribute(string expectedValue) => (attribute) =>
                {
                    attribute.VerifyValue(0, TypedConstantKind.Primitive, expectedValue);
                    Assert.Equal($@"MyAttribute(""{expectedValue}"")", attribute.ToString());
                };

            }
        }

        [Fact]
        public void TestMultipleMainAttributesDisallowed()
        {
            var source = @"
using System;

[main: My(""one"")]
[main: My(""two"")]
[main: My(""three"")]
Console.WriteLine(""Hello World"");

public class MyAttribute : Attribute { public MyAttribute(string name) {} }
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (5,8): error CS0579: Duplicate 'My' attribute
                // [main: My("two")]
                Diagnostic(ErrorCode.ERR_DuplicateAttribute, "My").WithArguments("My").WithLocation(5, 8),
                // (6,8): error CS0579: Duplicate 'My' attribute
                // [main: My("three")]
                Diagnostic(ErrorCode.ERR_DuplicateAttribute, "My").WithArguments("My").WithLocation(6, 8)
                );

            var attributes = GetSimpleEntryPointAttributes(compilation.Assembly.Modules[0]);

            Assert.Collection(attributes,
                verifyAttribute("one"),
                verifyAttribute("two"),
                verifyAttribute("three"));

            static Action<CSharpAttributeData> verifyAttribute(string expectedValue) => (attribute) =>
            {
                attribute.VerifyValue(0, TypedConstantKind.Primitive, expectedValue);
                Assert.Equal($@"MyAttribute(""{expectedValue}"")", attribute.ToString());
            };
        }

        [Fact]
        public void TestMainAttributesWithAssemblyAndModuleAttributes()
        {
            var source = @"
using System;

[main: My(""one"")]
[assembly: My(""two"")]
[module: My(""three"")]
Console.WriteLine(""Hello World"");

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class MyAttribute : Attribute { public MyAttribute(string name) {} }
";

            CompileAndVerify(source,
                sourceSymbolValidator: verifyAttributes,
                expectedOutput: "Hello World");

            static void verifyAttributes(ModuleSymbol module)
            {
                verifySingleAttribute(GetSimpleEntryPoint(module), "one");
                verifySingleAttribute(module.ContainingAssembly, "two");
                verifySingleAttribute(module, "three");
            }

            static void verifySingleAttribute(Symbol symbol, string expectedValue)
            {
                var attributes = symbol.GetAttributes();
                var attribute = Assert.Single(attributes);

                attribute.VerifyValue(0, TypedConstantKind.Primitive, expectedValue);
                Assert.Equal($@"MyAttribute(""{expectedValue}"")", attribute.ToString());
            };
        }

        [Fact]
        public void TestMainAttributesWithInvalidAttributesAfter()
        {
            var source = @"
using System;

[main: My(""one"")]
[My(""two"")]
Console.WriteLine(""Hello World"");

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class MyAttribute : Attribute { public MyAttribute(string name) {} }
";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (5,1): error CS7014: Attributes are not valid in this context.
                // [My("two")]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, @"[My(""two"")]").WithLocation(5, 1)
                );

            var entryPoint = GetSimpleEntryPoint(compilation.Assembly.Modules[0]);

            // one is on main
            var attribute = Assert.Single(entryPoint.GetAttributes());
            attribute.VerifyValue(0, TypedConstantKind.Primitive, "one");
            Assert.Equal($@"MyAttribute(""one"")", attribute.ToString());
        }

        [Fact]
        public void TestMainAttributesWithInvalidAttributesBefore()
        {
            var source = @"
using System;

[My(""two"")]
[main: My(""one"")]
Console.WriteLine(""Hello World"");

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class MyAttribute : Attribute { public MyAttribute(string name) {} }
";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (4,1): error CS7014: Attributes are not valid in this context.
                // [My("two")]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, @"[My(""two"")]").WithLocation(4, 1)
                );

            var entryPoint = GetSimpleEntryPoint(compilation.Assembly.Modules[0]);

            // one is ignored
            Assert.Empty(entryPoint.GetAttributes());
        }

        [Fact]
        public void TestMainAttributesAfterCode()
        {
            var source = @"
using System;
Console.WriteLine(""Hello World"");

[main: My(""one"")]
public class MyAttribute : Attribute { public MyAttribute(string name) {} }
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (5,2): error CS1730: Assembly, module, and main attributes must precede all other elements defined in a file except using clauses and extern alias declarations
                // [main: My("one")]
                Diagnostic(ErrorCode.ERR_GlobalAttributesNotFirst, "main").WithLocation(5, 2)
                );

            var entryPoint = GetSimpleEntryPoint(compilation.Assembly.Modules[0]);

            // one is ignored
            Assert.Empty(entryPoint.GetAttributes());
        }

        [Fact]
        public void TestMainAttributesInADifferentFile()
        {
            var source = new[] {@"
using System;
Console.WriteLine(""Hello World"");

public class MyAttribute : Attribute { public MyAttribute(string name) {} }
",
@" 
[main: My(""one"")]
" };

            CompileAndVerify(source,
               options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All),
               sourceSymbolValidator: verifyAttributes,
               expectedOutput: "Hello World");

            static void verifyAttributes(ModuleSymbol module)
            {
                var attributes = GetSimpleEntryPointAttributes(module);

                var attribute = Assert.Single(attributes);
                attribute.VerifyValue(0, TypedConstantKind.Primitive, "one");
                Assert.Equal(@"MyAttribute(""one"")", attribute.ToString());
            }
        }

        [Fact]
        public void TestInvalidMainAttributesInADifferentFile()
        {
            var source = new[] {@"
using System;
Console.WriteLine(""Hello World"");

public class MyAttribute : Attribute { public MyAttribute(string name) {} }
",
@" 
class C {}
[main: My(""one"")]
" };

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (3,2): error CS1730: Assembly, module, and main attributes must precede all other elements defined in a file except using clauses and extern alias declarations
                // [main: My("one")]
                Diagnostic(ErrorCode.ERR_GlobalAttributesNotFirst, "main").WithLocation(3, 2)
                );

            var entryPoint = GetSimpleEntryPoint(compilation.Assembly.Modules[0]);

            // one is ignored
            Assert.Empty(entryPoint.GetAttributes());
        }

        [Fact]
        public void TestMainAttributesWithMultipleEntryPoints()
        {
            var source = new[] {@"
using System;
[main: My(""one"")]

Console.WriteLine(""Hello World"");

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class MyAttribute : Attribute { public MyAttribute(string name) {} }
",
@"
[main: My(""two"")]
System.Console.WriteLine(""Hello World 2"");
",
@"
System.Console.WriteLine(""Hello World 3"");
",};

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (2,1): error CS8802: Only one compilation unit can have top-level statements.
                // System.Console.WriteLine("Hello World 3");
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithTopLevelStatements, "System").WithLocation(2, 1),
                // (3,1): error CS8802: Only one compilation unit can have top-level statements.
                // System.Console.WriteLine("Hello World 2");
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithTopLevelStatements, "System").WithLocation(3, 1)
                );

            var entryPoints = GetSimpleEntryPoints(compilation.Assembly.Modules[0]);

            Assert.Equal(3, entryPoints.Length);

            // first entry point gets all attributes
            Assert.Collection(entryPoints[0].GetAttributes(),
                verifyAttribute("one"),
                verifyAttribute("two"));

            // other entry points get none
            Assert.Empty(entryPoints[1].GetAttributes());
            Assert.Empty(entryPoints[2].GetAttributes());

            static Action<CSharpAttributeData> verifyAttribute(string expectedValue) => (attribute) =>
            {
                attribute.VerifyValue(0, TypedConstantKind.Primitive, expectedValue);
                Assert.Equal($@"MyAttribute(""{expectedValue}"")", attribute.ToString());
            };
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

        private static ImmutableArray<CSharpAttributeData> GetSimpleEntryPointAttributes(ModuleSymbol module) => GetSimpleEntryPoint(module).GetAttributes();

        private static MethodSymbol GetSimpleEntryPoint(ModuleSymbol module) => Assert.Single(GetSimpleEntryPoints(module));

        private static ImmutableArray<MethodSymbol> GetSimpleEntryPoints(ModuleSymbol module)
        {
            var program = module.GlobalNamespace.GetMember<NamedTypeSymbol>(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName);
            var methods = module switch
            {
                SourceModuleSymbol => program.GetMembers().Where(m => m is SynthesizedSimpleProgramEntryPointSymbol),
                _ => program.GetMembers(WellKnownMemberNames.TopLevelStatementsEntryPointMethodName)
            };
            return methods.Cast<MethodSymbol>().ToImmutableArray();
        }
    }
}
