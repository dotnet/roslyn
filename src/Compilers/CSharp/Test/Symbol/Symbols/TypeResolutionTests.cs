// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Basic.Reference.Assemblies;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class TypeResolutionTests : CSharpTestBase
    {
        [Fact]
        public void TestGetTypeByNameAndArity()
        {
            string source1 = @"
namespace System
{
    public class TestClass
    {
    }

    public class TestClass<T>
    {
    }
}
";

            string source2 = @"
namespace System
{
    public class TestClass
    {
    }
}
";

            var c1 = CSharpCompilation.Create("Test1",
                syntaxTrees: new[] { Parse(source1) },
                references: new[] { Net40.References.mscorlib });

            Assert.Null(c1.GetTypeByMetadataName("DoesntExist"));
            Assert.Null(c1.GetTypeByMetadataName("DoesntExist`1"));
            Assert.Null(c1.GetTypeByMetadataName("DoesntExist`2"));

            NamedTypeSymbol c1TestClass = c1.GetTypeByMetadataName("System.TestClass");
            Assert.NotNull(c1TestClass);
            NamedTypeSymbol c1TestClassT = c1.GetTypeByMetadataName("System.TestClass`1");
            Assert.NotNull(c1TestClassT);
            Assert.Null(c1.GetTypeByMetadataName("System.TestClass`2"));

            var c2 = CSharpCompilation.Create("Test2",
                syntaxTrees: new[] { SyntaxFactory.ParseSyntaxTree(source2) },
                references: new MetadataReference[]
                {
                    new CSharpCompilationReference(c1),
                    Net40.References.mscorlib
                });

            NamedTypeSymbol c2TestClass = c2.GetTypeByMetadataName("System.TestClass");
            Assert.Same(c2.Assembly, c2TestClass.ContainingAssembly);

            var c3 = CSharpCompilation.Create("Test3",
                references: new MetadataReference[]
                {
                    new CSharpCompilationReference(c2),
                    Net40.References.mscorlib
                });

            NamedTypeSymbol c3TestClass = c3.GetTypeByMetadataName("System.TestClass");
            Assert.NotSame(c2TestClass, c3TestClass);
            Assert.True(c3TestClass.ContainingAssembly.RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(c2TestClass.ContainingAssembly));

            Assert.Null(c3.GetTypeByMetadataName("System.TestClass`1"));

            var c4 = CSharpCompilation.Create("Test4",
                references: new MetadataReference[]
                {
                    new CSharpCompilationReference(c1),
                    new CSharpCompilationReference(c2),
                    Net40.References.mscorlib
                });

            NamedTypeSymbol c4TestClass = c4.GetTypeByMetadataName("System.TestClass");
            Assert.Null(c4TestClass);

            Assert.Same(c1TestClassT, c4.GetTypeByMetadataName("System.TestClass`1"));
        }

        public class C<S, T>
        {
            public class D
            {
                public class E<U, V>
                {
                    public class F<W>
                    {
                    }
                }
            }
        }

        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        public void TypeSymbolFromReflectionType()
        {
            var c = CSharpCompilation.Create("TypeSymbolFromReflectionType",
                syntaxTrees: new[] { SyntaxFactory.ParseSyntaxTree("class C { }") },
                references: new[] {
                    MscorlibRef,
                    MetadataReference.CreateFromImage(File.ReadAllBytes(typeof(TypeTests).GetTypeInfo().Assembly.Location))
                });

            var intSym = c.Assembly.GetTypeByReflectionType(typeof(int));
            Assert.NotNull(intSym);
            Assert.Equal(SpecialType.System_Int32, intSym.SpecialType);

            var strcmpSym = c.Assembly.GetTypeByReflectionType(typeof(StringComparison));
            Assert.NotNull(strcmpSym);
            Assert.Equal("System.StringComparison", strcmpSym.ToDisplayString());

            var arraySym = c.Assembly.GetTypeByReflectionType(typeof(List<int>[][,,]));
            Assert.NotNull(arraySym);
            Assert.Equal("System.Collections.Generic.List<int>[][*,*,*]", arraySym.ToDisplayString());

            var ptrSym = c.Assembly.GetTypeByReflectionType(typeof(char).MakePointerType().MakePointerType());
            Assert.NotNull(ptrSym);
            Assert.Equal("char**", ptrSym.ToDisplayString());

            string testType1 = typeof(C<,>).DeclaringType.FullName;
            var nestedSym1 = c.Assembly.GetTypeByReflectionType(typeof(C<int, bool>.D.E<double, float>.F<byte>));
            Assert.Equal(testType1 + ".C<int, bool>.D.E<double, float>.F<byte>", nestedSym1.ToDisplayString());

            // Not supported atm:
            //string testType2 = typeof(C<,>).DeclaringType.FullName;
            //var nestedSym2 = c.Assembly.GetTypeByReflectionType(typeof(C<,>.D.E<,>.F<>), includeReferences: true);
            //Assert.Equal(testType2 + ".C<int, bool>.D.E<double, float>.F<byte>", nestedSym2.ToDisplayString());

            // Process is defined in System, which isn't referenced:
            var err = c.Assembly.GetTypeByReflectionType(typeof(C<Process, bool>.D.E<double, float>.F<byte>));
            Assert.Null(err);

            err = c.Assembly.GetTypeByReflectionType(typeof(C<int, bool>.D.E<double, Process>.F<byte>));
            Assert.Null(err);

            err = c.Assembly.GetTypeByReflectionType(typeof(Process[]));
            Assert.Null(err);

            err = c.Assembly.GetTypeByReflectionType(typeof(SyntaxKind).MakePointerType());
            Assert.Null(err);
        }

        [Fact]
        public void AmbiguousNestedTypeSymbolFromMetadata()
        {
            var code = "class A { class B { } }";
            var c1 = CSharpCompilation.Create("Asm1", syntaxTrees: new[] { SyntaxFactory.ParseSyntaxTree(code) });
            var c2 = CSharpCompilation.Create("Asm2", syntaxTrees: new[] { SyntaxFactory.ParseSyntaxTree(code) });
            var c3 = CSharpCompilation.Create("Asm3",
                references: new[] {
                    new CSharpCompilationReference(c1),
                    new CSharpCompilationReference(c2)
                });

            Assert.Null(c3.GetTypeByMetadataName("A+B"));
        }

        [Fact]
        public void DuplicateNestedTypeSymbol()
        {
            var code = "class A { class B { } class B { } }";
            var c1 = CSharpCompilation.Create("Asm1",
                syntaxTrees: new[] { SyntaxFactory.ParseSyntaxTree(code) });

            Assert.Equal("A.B", c1.GetTypeByMetadataName("A+B").ToTestDisplayString());
        }
    }
}
