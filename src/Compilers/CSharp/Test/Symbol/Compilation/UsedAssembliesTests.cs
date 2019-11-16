// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using ICSharpCode.Decompiler.IL;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class UsedAssembliesTests : CSharpTestBase
    {
        [Fact]
        public void NoReferences_01()
        {
            var source =
@"
interface I1
{
    public I1 M();
}
";
            var comp1 = CreateEmptyCompilation(source);
            CompileAndVerify(comp1);

            Assert.Empty(comp1.GetUsedAssemblyReferences());

            var comp2 = CreateCompilation(source);
            CompileAndVerify(comp2);

            AssertUsedAssemblyReferences(comp2);
        }

        [Fact]
        public void NoReferences_02()
        {
            var source =
@"
public interface I1
{
    public I1 M();
}
";
            var comp1 = CreateEmptyCompilation(source);
            CompileAndVerify(comp1);

            var source2 =
@"
public class C2
{
    public static void Main(I1 x)
    {
        x.M();
    }
}
";

            verify<PEAssemblySymbol>(source2, comp1.EmitToImageReference());
            verify<RetargetingAssemblySymbol>(source2, comp1.ToMetadataReference());
            Assert.Empty(comp1.GetUsedAssemblyReferences());

            static void verify<TAssemblySymbol>(string source2, MetadataReference reference) where TAssemblySymbol : AssemblySymbol
            {
                Compilation comp2 = AssertUsedAssemblyReferences(source2, reference);
                Assert.IsType<TAssemblySymbol>(((CSharpCompilation)comp2).GetAssemblyOrModuleSymbol(reference));
            }
        }

        private static void AssertUsedAssemblyReferences(Compilation comp, MetadataReference[] expected, DiagnosticDescription[] before, DiagnosticDescription[] after)
        {
            comp.VerifyDiagnostics(before);

            bool hasCoreLibraryRef = !comp.ObjectType.IsErrorType();
            Assert.True(comp.References.Count() > expected.Length + (hasCoreLibraryRef ? 1 : 0));

            var used = comp.GetUsedAssemblyReferences();

            if (hasCoreLibraryRef)
            {
                Assert.Same(comp.ObjectType.ContainingAssembly, comp.GetAssemblyOrModuleSymbol(used[0]));
                AssertEx.Equal(expected, used.Skip(1));
            }
            else
            {
                AssertEx.Equal(expected, used);
            }

            Assert.Empty(used.Where(r => r.Properties.Kind == MetadataImageKind.Module));

            comp.RemoveAllReferences().AddReferences(used.Concat(comp.References.Where(r => r.Properties.Kind == MetadataImageKind.Module))).VerifyDiagnostics(after);
        }

        private static void AssertUsedAssemblyReferences(Compilation comp, params MetadataReference[] expected)
        {
            AssertUsedAssemblyReferences(comp, expected, new DiagnosticDescription[] { }, new DiagnosticDescription[] { });
        }

        private static Compilation AssertUsedAssemblyReferences(string source, MetadataReference[] references, params MetadataReference[] expected)
        {
            Compilation comp = CreateCompilation(source, references: references);
            AssertUsedAssemblyReferences(comp, expected);
            return comp;
        }

        private static Compilation AssertUsedAssemblyReferences(string source, params MetadataReference[] references)
        {
            return AssertUsedAssemblyReferences(source, references, references);
        }

        private static void AssertUsedAssemblyReferences(string source, MetadataReference[] references, params DiagnosticDescription[] expected)
        {
            Compilation comp = CreateCompilation(source, references: references);
            var diagnostics = comp.GetDiagnostics();
            diagnostics.Verify(expected);

            Assert.True(diagnostics.Any(d => d.DefaultSeverity == DiagnosticSeverity.Error));
            AssertEx.Equal(comp.References.Where(r => r.Properties.Kind == MetadataImageKind.Assembly), comp.GetUsedAssemblyReferences());
        }

        [Fact]
        public void NoReferences_03()
        {
            var source =
@"
namespace System
{
    public class Object {}
    public class ValueType {}
    public struct Void {}
}

public interface I1
{
    public I1 M();
}
";
            var comp1 = CreateEmptyCompilation(source);
            comp1.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1)
                );

            var source2 =
@"
public class C2
{
    public static object Main(I1 x)
    {
        x.M();
        return null;
    }
}
";

            verify<PEAssemblySymbol>(source2, comp1.EmitToImageReference());
            verify<SourceAssemblySymbol>(source2, comp1.ToMetadataReference());
            Assert.Empty(comp1.GetUsedAssemblyReferences());

            static void verify<TAssemblySymbol>(string source2, MetadataReference reference) where TAssemblySymbol : AssemblySymbol
            {
                Compilation comp2 = CreateEmptyCompilation(source2, references: new[] { reference, SystemCoreRef, SystemDrawingRef });
                AssertUsedAssemblyReferences(comp2);
                Assert.IsType<TAssemblySymbol>(((CSharpCompilation)comp2).GetAssemblyOrModuleSymbol(reference));
            }
        }

        [Fact]
        public void NoReferences_04()
        {
            var source =
@"
public interface I1
{
    public I1 M1();
}
";
            var comp1 = CreateEmptyCompilation(source);
            CompileAndVerify(comp1);

            var source2 =
@"
public interface I2
{
    public I1 M2();
}
";

            verify<PEAssemblySymbol>(source2, comp1.EmitToImageReference());
            verify<RetargetingAssemblySymbol>(source2, comp1.ToMetadataReference());
            Assert.Empty(comp1.GetUsedAssemblyReferences());

            static void verify<TAssemblySymbol>(string source2, MetadataReference reference) where TAssemblySymbol : AssemblySymbol
            {
                Compilation comp2 = CreateEmptyCompilation(source2, references: new[] { reference, SystemCoreRef, SystemDrawingRef });
                AssertUsedAssemblyReferences(comp2, reference);
                Assert.IsType<TAssemblySymbol>(((CSharpCompilation)comp2).GetAssemblyOrModuleSymbol(reference));
            }
        }

        [Fact]
        public void FieldReference_01()
        {
            var source1 =
@"
public class C1
{
    public static int F1 = 0;
    public int F2 = 0;
}
";
            var comp1 = CreateCompilation(source1);
            var comp1Ref = comp1.ToMetadataReference();
            var comp1ImageRef = comp1.EmitToImageReference();

            var source2 =
@"
public class C2
{
    public static void Main()
    {
        _ = C1.F1;
    }
}
";

            verify<PEAssemblySymbol>(source2, comp1ImageRef);
            verify<SourceAssemblySymbol>(source2, comp1Ref);

            var source3 =
@"
public class C2
{
    public static void Main()
    {
        C1 x = null;
        _ = x.F2;
    }
}
";

            verify<PEAssemblySymbol>(source3, comp1ImageRef);
            verify<SourceAssemblySymbol>(source3, comp1Ref);

            static void verify<TAssemblySymbol>(string source2, MetadataReference reference) where TAssemblySymbol : AssemblySymbol
            {
                Compilation comp2 = AssertUsedAssemblyReferences(source2, reference);
                Assert.IsType<TAssemblySymbol>(((CSharpCompilation)comp2).GetAssemblyOrModuleSymbol(reference));
            }
        }

        [Fact]
        public void FieldReference_02()
        {
            var source0 =
@"
public class C0
{
    public static C0 F0 = new C0();
}
";
            var comp0 = CreateCompilation(source0);
            comp0.VerifyDiagnostics();

            var comp0Ref = comp0.ToMetadataReference();
            var comp0ImageRef = comp0.EmitToImageReference();

            var source1 =
@"
public class C1
{
    public static C0 F0 = C0.F0;
    public static int F1 = 0;
}
";
            var comp1 = CreateCompilation(source1, references: new[] { comp0Ref });
            comp1.VerifyDiagnostics();

            var comp1Ref = comp1.ToMetadataReference();
            var comp1ImageRef = comp1.EmitToImageReference();

            var source2 =
@"
public class C2
{
    public static void Main()
    {
        _ = C1.F1;
    }
}
";

            verify<PEAssemblySymbol>(source2, comp0ImageRef, comp1ImageRef);
            verify<PEAssemblySymbol>(source2, comp0Ref, comp1ImageRef);
            verify<SourceAssemblySymbol>(source2, comp0Ref, comp1Ref);
            verify<RetargetingAssemblySymbol>(source2, comp0ImageRef, comp1Ref);

            static void verify<TAssemblySymbol>(string source2, MetadataReference reference0, MetadataReference reference1) where TAssemblySymbol : AssemblySymbol
            {
                Compilation comp2 = AssertUsedAssemblyReferences(source2, reference0, reference1);
                Assert.IsType<TAssemblySymbol>(((CSharpCompilation)comp2).GetAssemblyOrModuleSymbol(reference1));
            }
        }

        [Fact]
        public void FieldReference_03()
        {
            var source0 =
@"
public class C0
{
}
";
            var comp0 = CreateCompilation(source0);
            comp0.VerifyDiagnostics();

            var comp0Ref = comp0.ToMetadataReference();
            var comp0ImageRef = comp0.EmitToImageReference();

            var source1 =
@"
class C1
{
    static C0 F0 = new C0();
    public static C1 F1 = new C1();
}
";

            var comp1 = CreateCompilation(source1, references: new[] { comp0Ref }, options: TestOptions.DebugModule);
            comp1.VerifyDiagnostics();

            var comp1Ref = comp1.EmitToImageReference();

            var source2 =
@"
public class C2
{
    static C1 F1 = C1.F1;
    public static int F2 = 0;
}
";
            var comp2 = verify2<SourceAssemblySymbol>(source2, comp0Ref, comp1Ref);

            var comp2Ref = comp2.ToMetadataReference();
            var comp2ImageRef = comp2.EmitToImageReference();

            var source3 =
@"
public class C3
{
    public static void Main()
    {
        _ = C2.F2;
    }
}
";

            verify3<PEAssemblySymbol>(source3, comp0ImageRef, comp2ImageRef);
            verify3<PEAssemblySymbol>(source3, comp0Ref, comp2ImageRef);
            verify3<SourceAssemblySymbol>(source3, comp0Ref, comp2Ref);
            verify3<RetargetingAssemblySymbol>(source3, comp0ImageRef, comp2Ref);
            verify3<PEAssemblySymbol>(source3, comp2ImageRef);
            verify3<RetargetingAssemblySymbol>(source3, comp2Ref);

            comp2 = verify2<PEAssemblySymbol>(source2, comp0ImageRef, comp1Ref);
            comp2Ref = comp2.ToMetadataReference();
            comp2ImageRef = comp2.EmitToImageReference();

            verify3<PEAssemblySymbol>(source3, comp0ImageRef, comp2ImageRef);
            verify3<PEAssemblySymbol>(source3, comp0Ref, comp2ImageRef);
            verify3<RetargetingAssemblySymbol>(source3, comp0Ref, comp2Ref);
            verify3<SourceAssemblySymbol>(source3, comp0ImageRef, comp2Ref);
            verify3<PEAssemblySymbol>(source3, comp2ImageRef);
            verify3<RetargetingAssemblySymbol>(source3, comp2Ref);

            comp2 = CreateCompilation(source2, references: new[] { comp1Ref });
            comp2.VerifyDiagnostics();

            Assert.True(comp2.References.Count() > 1);

            var used = comp2.GetUsedAssemblyReferences();

            Assert.Equal(1, used.Length);
            Assert.Same(comp2.ObjectType.ContainingAssembly, comp2.GetAssemblyOrModuleSymbol(used[0]));

            comp2Ref = comp2.ToMetadataReference();
            comp2ImageRef = comp2.EmitToImageReference();

            verify3<PEAssemblySymbol>(source3, comp0ImageRef, comp2ImageRef);
            verify3<PEAssemblySymbol>(source3, comp0Ref, comp2ImageRef);
            verify3<RetargetingAssemblySymbol>(source3, comp0Ref, comp2Ref);
            verify3<RetargetingAssemblySymbol>(source3, comp0ImageRef, comp2Ref);
            verify3<PEAssemblySymbol>(source3, comp2ImageRef);
            verify3<SourceAssemblySymbol>(source3, comp2Ref);

            static Compilation verify2<TAssemblySymbol>(string source2, MetadataReference reference0, MetadataReference reference1) where TAssemblySymbol : AssemblySymbol
            {
                var comp2 = AssertUsedAssemblyReferences(source2, new[] { reference0, reference1 }, reference0);
                Assert.IsType<TAssemblySymbol>(((CSharpCompilation)comp2).GetAssemblyOrModuleSymbol(reference0));
                return comp2;
            }

            static void verify3<TAssemblySymbol>(string source3, params MetadataReference[] references) where TAssemblySymbol : AssemblySymbol
            {
                Compilation comp3 = AssertUsedAssemblyReferences(source3, references: references);
                Assert.IsType<TAssemblySymbol>(((CSharpCompilation)comp3).GetAssemblyOrModuleSymbol(references.Last()));
            }
        }

        [Fact]
        public void FieldReference_04()
        {
            var source1 =
@"
namespace N1
{
    public enum E1
    {
        F1 = 0
    }
}
";
            var comp1 = CreateCompilation(source1);

            var comp1Ref = comp1.ToMetadataReference();
            verify(comp1Ref,
@"
public class C2
{
    public static void Main()
    {
        _ = N1.E1.F1 + 1;
    }
}
");

            verify(comp1Ref,
@"
using N1;
public class C2
{
    public static void Main()
    {
        _ = E1.F1 + 1;
    }
}
");

            verify(comp1Ref,
@"
using static N1.E1;
public class C2
{
    public static void Main()
    {
        _ = F1 + 1;
    }
}
");

            verify(comp1Ref,
@"
using alias = N1.E1;
public class C2
{
    public static void Main()
    {
        _ = alias.F1 + 1;
    }
}
");

            static void verify(MetadataReference reference, string source)
            {
                AssertUsedAssemblyReferences(source, reference);
            }
        }

        [Fact]
        public void FieldReference_05()
        {
            var source0 =
@"
public class C0 {}
";
            var comp0 = CreateCompilation(source0);
            var comp0Ref = comp0.ToMetadataReference();

            var source1 =
@"
public class C1<T>
{
    public enum E1
    {
        F1 = 0
    }

    public class C3
    {
        public int F3 = 0;
    }
}
";
            var comp1 = CreateCompilation(source1);
            comp1.VerifyDiagnostics();
            var comp1Ref = comp1.ToMetadataReference();

            verify(comp0Ref, comp1Ref,
@"
public class C2
{
    public static void Main()
    {
        _ = C1<C0>.E1.F1 + 1;
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using static C1<C0>;
public class C2
{
    public static void Main()
    {
        _ = E1.F1 + 1;
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using static C1<C0>.E1;
public class C2
{
    public static void Main()
    {
        _ = F1 + 1;
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using alias = C1<C0>.E1;
public class C2
{
    public static void Main()
    {
        _ = alias.F1 + 1;
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using alias = C1<C0>;
public class C2
{
    public static void Main()
    {
        _ = alias.E1.F1 + 1;
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
public class C2
{
    public static void Main()
    {
        _ = nameof(C1<C0>.E1.F1);
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using static C1<C0>;
public class C2
{
    public static void Main()
    {
        _ = nameof(E1.F1);
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using static C1<C0>.E1;
public class C2
{
    public static void Main()
    {
        _ = nameof(F1);
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using alias = C1<C0>.E1;
public class C2
{
    public static void Main()
    {
        _ = nameof(alias.F1);
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using alias = C1<C0>;
public class C2
{
    public static void Main()
    {
        _ = nameof(alias.E1.F1);
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
public class C2
{
    public static void Main()
    {
        _ = nameof(C1<C0>.C3.F3);
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using static C1<C0>;
public class C2
{
    public static void Main()
    {
        _ = nameof(C3.F3);
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using alias = C1<C0>.C3;
public class C2
{
    public static void Main()
    {
        _ = nameof(alias.F3);
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using alias = C1<C0>;
public class C2
{
    public static void Main()
    {
        _ = nameof(alias.C3.F3);
    }
}
");

            static void verify(MetadataReference reference0, MetadataReference reference1, string source)
            {
                AssertUsedAssemblyReferences(source, reference0, reference1);
            }
        }

        [Fact]
        public void FieldReference_06()
        {
            var source0 =
@"
public class C0 {}
";
            var comp0 = CreateCompilation(source0);
            var comp0Ref = comp0.ToMetadataReference();

            var source1 =
@"
public class C1<T>
{
    public enum E1
    {
        F1 = 0
    }

    public class C3
    {
        public int F3;
    }
}
";
            var comp1 = CreateCompilation(source1);
            comp1.VerifyDiagnostics();
            var comp1Ref = comp1.ToMetadataReference();

            verify(comp0Ref, comp1Ref,
@"
class C2
{
    /// <summary>
    /// <see cref=""C1{C0}.E1.F1""/>
    /// </summary>
    static void Main()
    {
    }
}
",
                hasTypeReferensesInUsing: false);

            verify(comp0Ref, comp1Ref,
@"
using static C1<C0>;
class C2
{
    /// <summary>
    /// <see cref=""E1.F1""/>
    /// </summary>
    static void Main()
    {
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using static C1<C0>.E1;
class C2
{
    /// <summary>
    /// <see cref=""F1""/>
    /// </summary>
    static void Main()
    {
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using alias = C1<C0>.E1;
class C2
{
    /// <summary>
    /// <see cref=""alias.F1""/>
    /// </summary>
    static void Main()
    {
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using alias = C1<C0>;
class C2
{
    /// <summary>
    /// <see cref=""alias.E1.F1""/>
    /// </summary>
    static void Main()
    {
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
class C2
{
    /// <summary>
    /// <see cref=""C1{C0}.C3.F3""/>
    /// </summary>
    static void Main()
    {
    }
}
",
                hasTypeReferensesInUsing: false);

            verify(comp0Ref, comp1Ref,
@"
using static C1<C0>;
class C2
{
    /// <summary>
    /// <see cref=""C3.F3""/>
    /// </summary>
    static void Main()
    {
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using alias = C1<C0>.C3;
class C2
{
    /// <summary>
    /// <see cref=""alias.F3""/>
    /// </summary>
    static void Main()
    {
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using alias = C1<C0>;
class C2
{
    /// <summary>
    /// <see cref=""alias.C3.F3""/>
    /// </summary>
    static void Main()
    {
    }
}
");

            static void verify(MetadataReference reference0, MetadataReference reference1, string source, bool hasTypeReferensesInUsing = true)
            {
                var references = new[] { reference0, reference1 };
                Compilation comp2 = CreateCompilation(source, references: references, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.None));
                AssertUsedAssemblyReferences(comp2, hasTypeReferensesInUsing ? references : new MetadataReference[] { });

                var expected = hasTypeReferensesInUsing ? references : new[] { reference1 };

                Compilation comp3 = CreateCompilation(source, references: references, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.Parse));
                AssertUsedAssemblyReferences(comp3, expected);

                Compilation comp4 = CreateCompilation(source, references: references, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.Diagnose));
                AssertUsedAssemblyReferences(comp4, expected);
            }
        }

        [Fact]
        public void FieldReference_07()
        {
            var source0 =
@"
public class C0 {}
";
            var comp0 = CreateCompilation(source0);
            var comp0Ref = comp0.ToMetadataReference();

            var source1 =
@"
public class C1<T>
{
    public enum E1
    {
        F1 = 0
    }
}
";
            var comp1 = CreateCompilation(source1);
            comp1.VerifyDiagnostics();
            var comp1Ref = comp1.ToMetadataReference();

            var attribute =
@"
class TestAttribute : System.Attribute
{
    public TestAttribute()
    { }
    public TestAttribute(int value)
    { }
    public int Value = 0;
}
";

            verify(comp0Ref, comp1Ref,
@"
public class C2
{
    [Test((int)C1<C0>.E1.F1 + 1)]
    public static void Main()
    {
    }
}
" + attribute);

            verify(comp0Ref, comp1Ref,
@"
using static C1<C0>;
public class C2
{
    [Test((int)E1.F1 + 1)]
    public static void Main()
    {
    }
}
" + attribute);

            verify(comp0Ref, comp1Ref,
@"
using static C1<C0>.E1;
public class C2
{
    [Test((int)F1 + 1)]
    public static void Main()
    {
    }
}
" + attribute);

            verify(comp0Ref, comp1Ref,
@"
using alias = C1<C0>.E1;
public class C2
{
    [Test((int)alias.F1 + 1)]
    public static void Main()
    {
    }
}
" + attribute);

            verify(comp0Ref, comp1Ref,
@"
using alias = C1<C0>;
public class C2
{
    [Test((int)alias.E1.F1 + 1)]
    public static void Main()
    {
    }
}
" + attribute);

            verify(comp0Ref, comp1Ref,
@"
public class C2
{
    [Test(Value = (int)C1<C0>.E1.F1 + 1)]
    public static void Main()
    {
    }
}
" + attribute);

            verify(comp0Ref, comp1Ref,
@"
using static C1<C0>;
public class C2
{
    [Test(Value = (int)E1.F1 + 1)]
    public static void Main()
    {
    }
}
" + attribute);

            verify(comp0Ref, comp1Ref,
@"
using static C1<C0>.E1;
public class C2
{
    [Test(Value = (int)F1 + 1)]
    public static void Main()
    {
    }
}
" + attribute);

            verify(comp0Ref, comp1Ref,
@"
using alias = C1<C0>.E1;
public class C2
{
    [Test(Value = (int)alias.F1 + 1)]
    public static void Main()
    {
    }
}
" + attribute);

            verify(comp0Ref, comp1Ref,
@"
using alias = C1<C0>;
public class C2
{
    [Test(Value = (int)alias.E1.F1 + 1)]
    public static void Main()
    {
    }
}
" + attribute);

            static void verify(MetadataReference reference0, MetadataReference reference1, string source)
            {
                AssertUsedAssemblyReferences(source, reference0, reference1);
            }
        }

        [Fact]
        public void FieldReference_08()
        {
            var source0 =
@"
public class C0 {}
";
            var comp0 = CreateCompilation(source0);
            var comp0Ref = comp0.ToMetadataReference();

            var source1 =
@"
public class C1<T>
{
    public enum E1
    {
        F1 = 0
    }
}
";
            var comp1 = CreateCompilation(source1);
            comp1.VerifyDiagnostics();
            var comp1Ref = comp1.ToMetadataReference();

            verify(comp0Ref, comp1Ref,
@"
public class C2
{
    public static void Main(int p = (int)C1<C0>.E1.F1 + 1)
    {
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using static C1<C0>;
public class C2
{
    public static void Main(int p = (int)E1.F1 + 1)
    {
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using static C1<C0>.E1;
public class C2
{
    public static void Main(int p = (int)F1 + 1)
    {
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using alias = C1<C0>.E1;
public class C2
{
    public static void Main(int p = (int)alias.F1 + 1)
    {
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using alias = C1<C0>;
public class C2
{
    public static void Main(int p = (int)alias.E1.F1 + 1)
    {
    }
}
");

            static void verify(MetadataReference reference0, MetadataReference reference1, string source)
            {
                AssertUsedAssemblyReferences(source, reference0, reference1);
            }
        }

        [Fact]
        public void MethodReference_01()
        {
            var source1 =
@"
public class C1
{
    public static void M1(){}
}
";
            var comp1 = CreateCompilation(source1);

            var source2 =
@"
public class C2
{
    public static void Main()
    {
        C1.M1();
    }
}
";

            verify<PEAssemblySymbol>(source2, comp1.EmitToImageReference());
            verify<SourceAssemblySymbol>(source2, comp1.ToMetadataReference());

            static void verify<TAssemblySymbol>(string source2, MetadataReference reference) where TAssemblySymbol : AssemblySymbol
            {
                Compilation comp2 = AssertUsedAssemblyReferences(source2, reference);
                Assert.IsType<TAssemblySymbol>(((CSharpCompilation)comp2).GetAssemblyOrModuleSymbol(reference));
            }
        }

        [Fact]
        public void MethodReference_02()
        {
            var source0 =
@"
public class C0
{
}
";
            var comp0 = CreateCompilation(source0);
            comp0.VerifyDiagnostics();

            var source1 =
@"
public class C1
{
    public static void M1<T>(){}
}

public class C2<T> {}

public class C3<T>
{
    public class C4 {}
}
";
            var comp1 = CreateCompilation(source1);
            comp1.VerifyDiagnostics();

            var reference0 = comp0.ToMetadataReference();
            var reference1 = comp1.ToMetadataReference();

            verify(reference0, reference1,
@"
public class C5
{
    public static void Main()
    {
        C1.M1<C0>();
    }
}
");

            verify(reference0, reference1,
@"
public class C5
{
    public static void Main()
    {
        C1.M1<C2<C0>>();
    }
}
");

            verify(reference1, reference0,
@"
public class C5
{
    public static void Main()
    {
        C1.M1<C3<C0>.C4>();
    }
}
");

            void verify(MetadataReference reference0, MetadataReference reference1, string source)
            {
                AssertUsedAssemblyReferences(source, reference0, reference1);
            }
        }

        [Fact]
        public void MethodReference_03()
        {
            var source0 =
@"
public static class C0
{
    public static void M1(this string x, int y) { }
}
";
            var comp0 = CreateCompilation(source0);
            var comp0Ref = comp0.ToMetadataReference();

            var source1 =
@"
public static class C1
{
    public static void M1(this string x, string y) { }
}
";
            var comp1 = CreateCompilation(source1);
            var comp1Ref = comp1.ToMetadataReference();

            var source2 =
@"
public class C2
{
    public static void Main()
    {
        var x = ""a"";
        x.M1(""b"");
    }
}
";

            AssertUsedAssemblyReferences(source2, references: new[] { comp0Ref, comp1Ref }, comp1Ref);
        }

        [Fact]
        public void MethodReference_04()
        {
            var source0 =
@"
public static class C0
{
    public static void M1(this string x, string y) { }
}
";
            var comp0 = CreateCompilation(source0);
            var comp0Ref = comp0.ToMetadataReference();

            var source1 =
@"
public static class C1
{
    public static void M1(this string x, string y) { }
}
";
            var comp1 = CreateCompilation(source1);
            var comp1Ref = comp1.ToMetadataReference();

            var source2 =
@"
public class C2
{
    public static void Main()
    {
        var x = ""a"";
        x.M1(""b"");
    }
}
";

            AssertUsedAssemblyReferences(source2, references: new[] { comp0Ref, comp1Ref },
                // (7,11): error CS0121: The call is ambiguous between the following methods or properties: 'C0.M1(string, string)' and 'C1.M1(string, string)'
                //         x.M1("b");
                Diagnostic(ErrorCode.ERR_AmbigCall, "M1").WithArguments("C0.M1(string, string)", "C1.M1(string, string)").WithLocation(7, 11)
                );
        }

        [Fact]
        public void MethodReference_05()
        {
            var source0 =
@"
public class C0
{
}
";
            var comp0 = CreateCompilation(source0, assemblyName: "MethodReference_05_0");
            var comp0Ref = comp0.ToMetadataReference();

            var source1 =
@"
public static class C1
{
    public static void M1(this string x, C0 y) { }
}

public interface I1 {}
";
            var comp1 = CreateCompilation(source1, references: new[] { comp0Ref });
            var comp1Ref = comp1.ToMetadataReference();

            var source2 =
@"
public static class C2
{
    public static void M1(this string x, string y) { }
}
";
            var comp2 = CreateCompilation(source2);
            var comp2Ref = comp2.ToMetadataReference();

            var source3 =
@"
public class C3
{
    public static void Main()
    {
        var x = ""a"";
        x.M1(""b"");
    }
}
";

            AssertUsedAssemblyReferences(source3, references: new[] { comp0Ref, comp1Ref, comp2Ref }, comp2Ref);

            AssertUsedAssemblyReferences(source3, references: new[] { comp1Ref, comp2Ref },
                // (7,9): error CS0012: The type 'C0' is defined in an assembly that is not referenced. You must add a reference to assembly 'MethodReference_05_0, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         x.M1("b");
                Diagnostic(ErrorCode.ERR_NoTypeDef, "x.M1").WithArguments("C0", "MethodReference_05_0, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 9)
                );

            var source4 =
@"
public class C3
{
    public static void Main()
    {
        var x = ""a"";
        x.M1(""b"");
    }

    void M1(I1 x) {}
}
";
            AssertUsedAssemblyReferences(source4, references: new[] { comp0Ref, comp1Ref, comp2Ref }, comp0Ref, comp1Ref, comp2Ref);

            AssertUsedAssemblyReferences(source4, references: new[] { comp1Ref, comp2Ref },
                // (7,9): error CS0012: The type 'C0' is defined in an assembly that is not referenced. You must add a reference to assembly 'MethodReference_05_0, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         x.M1("b");
                Diagnostic(ErrorCode.ERR_NoTypeDef, "x.M1").WithArguments("C0", "MethodReference_05_0, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 9)
                );
        }

        [Fact]
        public void MethodReference_06()
        {
            var source0 =
@"
public class C0
{
}
";
            var comp0 = CreateCompilation(source0);
            var comp0Ref = comp0.ToMetadataReference();

            var source1 =
@"
public class C1
{
    public void M1(C0 y) { }
}
";
            var comp1 = CreateCompilation(source1, references: new[] { comp0Ref });
            var comp1Ref = comp1.ToMetadataReference();
            var comp1ImageRef = comp1.EmitToImageReference();

            var source2 =
@"
public class C2 : C1
{
    public void M1(string y) { }
}
";
            var comp2 = CreateCompilation(source2, references: new[] { comp1Ref });
            var comp2Ref = comp2.ToMetadataReference();
            var comp2ImageRef = comp2.EmitToImageReference();

            var source3 =
@"
public class C3
{
    public static void Main()
    {
        var x = ""a"";
        new C2().M1(x);
    }
}
";

            AssertUsedAssemblyReferences(source3, references: new[] { comp0Ref, comp1Ref, comp2Ref }, comp0Ref, comp1Ref, comp2Ref);
            AssertUsedAssemblyReferences(source3, references: new[] { comp1Ref, comp2Ref }, comp1Ref, comp2Ref);
            AssertUsedAssemblyReferences(source3, references: new[] { comp1ImageRef, comp2Ref }, comp1ImageRef, comp2Ref);
            AssertUsedAssemblyReferences(source3, references: new[] { comp1Ref, comp2ImageRef }, comp1Ref, comp2ImageRef);
            AssertUsedAssemblyReferences(source3, references: new[] { comp1ImageRef, comp2ImageRef }, comp1ImageRef, comp2ImageRef);
        }

        [Fact]
        public void MethodReference_07()
        {
            var source0 =
@"
public class C0
{
}
";
            var comp0 = CreateCompilation(source0, assemblyName: "MethodReference_07_0");
            var comp0Ref = comp0.ToMetadataReference();

            var source1 =
@"
public class C1
{
    public void M1(string y) { }
}
";
            var comp1 = CreateCompilation(source1);
            var comp1Ref = comp1.ToMetadataReference();

            var source2 =
@"
public class C2 : C1
{
    public void M1(C0 y) { }
}
";
            var comp2 = CreateCompilation(source2, references: new[] { comp0Ref, comp1Ref });
            var comp2Ref = comp2.ToMetadataReference();

            var source3 =
@"
public class C3
{
    public static void Main()
    {
        var x = ""a"";
        new C2().M1(x);
    }
}
";

            AssertUsedAssemblyReferences(source3, references: new[] { comp0Ref, comp1Ref, comp2Ref }, comp0Ref, comp1Ref, comp2Ref);

            AssertUsedAssemblyReferences(source3, references: new[] { comp1Ref, comp2Ref },
                // (7,9): error CS0012: The type 'C0' is defined in an assembly that is not referenced. You must add a reference to assembly 'MethodReference_07_0, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         new C2().M1(x);
                Diagnostic(ErrorCode.ERR_NoTypeDef, "new C2().M1").WithArguments("C0", "MethodReference_07_0, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 9)
                );
        }

        [Fact]
        public void FieldDeclaration_01()
        {
            var source1 =
@"
namespace N1
{
    public class C1
    {
        public class C11
        {
        }
    }
}
";
            var comp1 = CreateCompilation(source1);

            var comp1Ref = comp1.ToMetadataReference();
            verify(comp1Ref,
@"
public class C2
{
    public static N1.C1.C11 F1 = null;
}
");
            verify(comp1Ref,
@"
using N2 = N1;
public class C2
{
    public static N2.C1.C11 F1 = null;
}
");
            verify(comp1Ref,
@"
using N1;
public class C2
{
    public static C1.C11 F1 = null;
}
");
            verify(comp1Ref,
@"
using static N1.C1;
public class C2
{
    public static C11 F1 = null;
}
");
            verify(comp1Ref,
@"
using C111 = N1.C1.C11;
public class C2
{
    public static C111 F1 = null;
}
");

            static void verify(MetadataReference reference, string source2)
            {
                AssertUsedAssemblyReferences(source2, reference);
            }
        }

        [Fact]
        public void UnusedUsings_01()
        {
            var source1 =
@"
namespace N1
{
    public static class C1
    {
    }
}
";
            var comp1 = CreateCompilation(source1);
            var comp1Ref = comp1.ToMetadataReference();

            verify1(comp1Ref,
@"
using N1;

public class C2
{
}
",
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using N1;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N1;").WithLocation(2, 1)
                );

            verify1(comp1Ref,
@"
using static N1.C1;

public class C2
{
}
",
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using static N1.C1;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using static N1.C1;").WithLocation(2, 1)
                );

            verify1(comp1Ref,
@"
using alias = N1.C1;

public class C2
{
}
",
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using alias = N1.C1;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using alias = N1.C1;").WithLocation(2, 1)
                );

            verify1(comp1Ref.WithAliases(new[] { "N1C1" }),
@"
extern alias N1C1;

public class C2
{
}
",
                // (2,1): hidden CS8020: Unused extern alias.
                // extern alias N1C1;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias N1C1;").WithLocation(2, 1)
                );

            verify1(comp1Ref,
@"namespace N2 {
using N1;

public class C2
{
}
}",
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using N1;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N1;").WithLocation(2, 1)
                );

            verify1(comp1Ref,
@"namespace N2 {
using static N1.C1;

public class C2
{
}
}",
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using static N1.C1;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using static N1.C1;").WithLocation(2, 1)
                );

            verify1(comp1Ref,
@"namespace N2 {
using alias = N1.C1;

public class C2
{
}
}",
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using alias = N1.C1;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using alias = N1.C1;").WithLocation(2, 1)
                );

            verify1(comp1Ref.WithAliases(new[] { "N1C1" }),
@"namespace N2 {
extern alias N1C1;

public class C2
{
}
}",
                // (2,1): hidden CS8020: Unused extern alias.
                // extern alias N1C1;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias N1C1;").WithLocation(2, 1)
                );

            verify2(comp1Ref,
@"
public class C2
{
}
",
                "N1");

            verify2(comp1Ref,
@"
public class C2
{
}
",
                "N1.C1");

            static void verify1(MetadataReference reference, string source, params DiagnosticDescription[] expected)
            {
                Compilation comp = CreateCompilation(source, references: new[] { reference });
                comp.VerifyDiagnostics(expected);

                Assert.True(comp.References.Count() > 1);

                var used = comp.GetUsedAssemblyReferences();

                Assert.Equal(1, used.Length);
                Assert.Same(comp.ObjectType.ContainingAssembly, comp.GetAssemblyOrModuleSymbol(used[0]));
            }

            static void verify2(MetadataReference reference, string source, string @using)
            {
                AssertUsedAssemblyReferences(CreateCompilation(Parse(source, options: TestOptions.Script), references: new[] { reference }, options: TestOptions.DebugDll.WithUsings(@using)),
                                             reference);
            }
        }

        [Fact]
        public void MethodDeclaration_01()
        {
            var source1 =
@"
namespace N1
{
    public class C1
    {
        public class C11
        {
        }
    }
}
";
            var comp1 = CreateCompilation(source1);

            var comp1Ref = comp1.ToMetadataReference();
            verify(comp1Ref,
@"
public class C2
{
    public static N1.C1.C11 M1() => null;
}
");
            verify(comp1Ref,
@"
using N2 = N1;
public class C2
{
    public static N2.C1.C11 M1() => null;
}
");
            verify(comp1Ref,
@"
using N1;
public class C2
{
    public static C1.C11 M1() => null;
}
");
            verify(comp1Ref,
@"
using static N1.C1;
public class C2
{
    public static C11 M1() => null;
}
");
            verify(comp1Ref,
@"
using C111 = N1.C1.C11;
public class C2
{
    public static C111 M1() => null;
}
");

            static void verify(MetadataReference reference, string source2)
            {
                AssertUsedAssemblyReferences(source2, reference);
            }
        }

        [Fact]
        public void NoPia_01()
        {
            var source0 =
@"
using System;
using System.Runtime.InteropServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest33
{
}
";
            var comp0 = CreateCompilation(source0);
            comp0.VerifyDiagnostics();

            var comp0Ref = comp0.ToMetadataReference(embedInteropTypes: true);
            var comp0ImageRef = comp0.EmitToImageReference(embedInteropTypes: true);

            var source1 =
@"
public class C1
{
    public static ITest33 F0 = null;
    public static int F1 = 0;
}
";
            var comp1 = AssertUsedAssemblyReferences(source1, references: new[] { comp0Ref });

            var comp1Ref = comp1.ToMetadataReference();
            var comp1ImageRef = comp1.EmitToImageReference();

            var source2 =
@"
public class C2
{
    public static void Main()
    {
        _ = C1.F1;
    }
}
";

            verify<PEAssemblySymbol>(source2, comp0ImageRef, comp1ImageRef);
            verify<PEAssemblySymbol>(source2, comp0Ref, comp1ImageRef);
            verify<RetargetingAssemblySymbol>(source2, comp0Ref, comp1Ref);
            verify<RetargetingAssemblySymbol>(source2, comp0ImageRef, comp1Ref);

            static void verify<TAssemblySymbol>(string source2, MetadataReference reference0, MetadataReference reference1) where TAssemblySymbol : AssemblySymbol
            {
                Compilation comp2 = AssertUsedAssemblyReferences(source2, new[] { reference0, reference1 }, reference1);
                Assert.IsType<TAssemblySymbol>(((CSharpCompilation)comp2).GetAssemblyOrModuleSymbol(reference1));
            }
        }

        [Fact]
        public void ArraysAndPointers_01()
        {
            var source0 =
@"
public class C0 {}
";
            var comp0 = CreateCompilation(source0);
            var comp0Ref = comp0.ToMetadataReference();

            var source1 =
@"
public class C1<T>
{
    public enum E1
    {
        F1 = 0
    }
}

public struct S<T>
{ }
";
            var comp1 = CreateCompilation(source1);
            comp1.VerifyDiagnostics();
            var comp1Ref = comp1.ToMetadataReference();

            verify(comp0Ref, comp1Ref,
@"
public class C2
{
    public static void Main()
    {
        _ = C1<S<C0>*[]>.E1.F1 + 1;
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using static C1<S<C0>*[]>;
public class C2
{
    public static void Main()
    {
        _ = E1.F1 + 1;
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using static C1<S<C0>*[]>.E1;
public class C2
{
    public static void Main()
    {
        _ = F1 + 1;
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using alias = C1<S<C0>*[]>.E1;
public class C2
{
    public static void Main()
    {
        _ = alias.F1 + 1;
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using alias = C1<S<C0>*[]>;
public class C2
{
    public static void Main()
    {
        _ = alias.E1.F1 + 1;
    }
}
");

            static void verify(MetadataReference reference0, MetadataReference reference1, string source)
            {
                AssertUsedAssemblyReferences(source, reference0, reference1);
            }
        }

        [Fact]
        public void TypeReference_01()
        {
            var source0 =
@"
public class C0 {}
";
            var comp0 = CreateCompilation(source0);
            var comp0Ref = comp0.ToMetadataReference();

            var source1 =
@"
public class C1<T>
{
    public enum E1
    {
    }
}
";
            var comp1 = CreateCompilation(source1);
            comp1.VerifyDiagnostics();
            var comp1Ref = comp1.ToMetadataReference();

            verify(comp0Ref, comp1Ref,
@"
public class C2
{
    public static void Main()
    {
        _ = nameof(C1<C0>.E1);
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using static C1<C0>;
public class C2
{
    public static void Main()
    {
        _ = nameof(E1);
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using alias = C1<C0>.E1;
public class C2
{
    public static void Main()
    {
        _ = nameof(alias);
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using alias = C1<C0>;
public class C2
{
    public static void Main()
    {
        _ = nameof(alias.E1);
    }
}
");

            static void verify(MetadataReference reference0, MetadataReference reference1, string source)
            {
                AssertUsedAssemblyReferences(source, reference0, reference1);
            }
        }

        [Fact]
        public void TypeReference_02()
        {
            var source0 =
@"
public class C0 {}
";
            var comp0 = CreateCompilation(source0);
            var comp0Ref = comp0.ToMetadataReference();

            var source1 =
@"
public class C1<T>
{
    public enum E1
    {
    }
}
";
            var comp1 = CreateCompilation(source1);
            comp1.VerifyDiagnostics();
            var comp1Ref = comp1.ToMetadataReference();

            verify(comp0Ref, comp1Ref,
@"
class C2
{
    /// <summary>
    /// <see cref=""C1{C0}.E1""/>
    /// </summary>
    static void Main()
    {
    }
}
",
                hasTypeReferensesInUsing: false);

            verify(comp0Ref, comp1Ref,
@"
using static C1<C0>;
class C2
{
    /// <summary>
    /// <see cref=""E1""/>
    /// </summary>
    static void Main()
    {
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using alias = C1<C0>.E1;
class C2
{
    /// <summary>
    /// <see cref=""alias""/>
    /// </summary>
    static void Main()
    {
    }
}
");

            verify(comp0Ref, comp1Ref,
@"
using alias = C1<C0>;
class C2
{
    /// <summary>
    /// <see cref=""alias.E1""/>
    /// </summary>
    static void Main()
    {
    }
}
");

            var source2 =
@"
class C2
{
    static void Main1()
    {
    }
}
";

            var references = new[] { comp0Ref, comp1Ref };
            AssertUsedAssemblyReferences(CreateCompilation(source2, references: references,
                                                           parseOptions: TestOptions.Script.WithDocumentationMode(DocumentationMode.None),
                                                           options: TestOptions.DebugDll.WithUsings("C0")),
                                         comp0Ref);
            AssertUsedAssemblyReferences(CreateCompilation(source2, references: references,
                                                           parseOptions: TestOptions.Script.WithDocumentationMode(DocumentationMode.Parse),
                                                           options: TestOptions.DebugDll.WithUsings("C0")),
                                         comp0Ref);

            static void verify(MetadataReference reference0, MetadataReference reference1, string source, bool hasTypeReferensesInUsing = true)
            {
                var references = new[] { reference0, reference1 };
                Compilation comp2 = CreateCompilation(source, references: references, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.None));
                AssertUsedAssemblyReferences(comp2, hasTypeReferensesInUsing ? references : new MetadataReference[] { });

                var expected = hasTypeReferensesInUsing ? references : new[] { reference1 };

                Compilation comp3 = CreateCompilation(source, references: references, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.Parse));
                AssertUsedAssemblyReferences(comp3, expected);

                Compilation comp4 = CreateCompilation(source, references: references, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.Diagnose));
                AssertUsedAssemblyReferences(comp4, expected);
            }
        }

        [Fact]
        public void TypeReference_03()
        {
            var source0 =
@"
public class C0 {}
";
            var comp0 = CreateCompilation(source0);
            var comp0Ref = comp0.ToMetadataReference();

            var source1 =
@"
public class C1<T>
{
    public enum E1
    {
    }
}
";
            var comp1 = CreateCompilation(source1);
            comp1.VerifyDiagnostics();
            var comp1Ref = comp1.ToMetadataReference();

            verify(comp0Ref, comp1Ref,
@"
class C2
{
    /// <summary>
    /// <see cref=""M(C1{C0}.E1)""/>
    /// </summary>
    static void Main()
    {
    }

    void M(int x) {}
}
",
                hasTypeReferensesInUsing: false);

            verify(comp0Ref, comp1Ref,
@"
using static C1<C0>;
class C2
{
    /// <summary>
    /// <see cref=""M(E1)""/>
    /// </summary>
    static void Main()
    {
    }

    void M(int x) {}
}
");

            verify(comp0Ref, comp1Ref,
@"
using alias = C1<C0>.E1;
class C2
{
    /// <summary>
    /// <see cref=""M(alias)""/>
    /// </summary>
    static void Main()
    {
    }

    void M(int x) {}
}
");

            verify(comp0Ref, comp1Ref,
@"
using alias = C1<C0>;
class C2
{
    /// <summary>
    /// <see cref=""M(alias.E1)""/>
    /// </summary>
    static void Main()
    {
    }

    void M(int x) {}
}
");

            static void verify(MetadataReference reference0, MetadataReference reference1, string source, bool hasTypeReferensesInUsing = true)
            {
                var references = new[] { reference0, reference1 };
                Compilation comp2 = CreateCompilation(source, references: references, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.None));
                AssertUsedAssemblyReferences(comp2, hasTypeReferensesInUsing ? references : new MetadataReference[] { });

                Compilation comp3 = CreateCompilation(source, references: references, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.Parse));
                AssertUsedAssemblyReferences(comp3, references);
            }
        }

        [Fact]
        public void NamespaceReference_01()
        {
            var source0 =
@"
namespace N1.N2
{
    public enum E0
    {
    }
}
";
            var comp0 = CreateCompilation(source0);
            var comp0Ref = comp0.ToMetadataReference();

            var source1 =
@"
namespace N1.N2
{
    public enum E1
    {
    }
}
";
            var comp1 = CreateCompilation(source1);
            comp1.VerifyDiagnostics();
            var comp1Ref = comp1.ToMetadataReference();

            var source2 =
@"
namespace N1
{
    public enum E2
    {
    }
}
";
            var comp2 = CreateCompilation(source2);
            comp2.VerifyDiagnostics();
            var comp2Ref = comp2.ToMetadataReference();

            verify(comp0Ref, comp1Ref, comp2Ref,
@"
public class C2
{
    public static void Main()
    {
        _ = nameof(N1.N2);
    }
}
");

            verify(comp0Ref, comp1Ref, comp2Ref,
@"
using alias = N1.N2;
public class C2
{
    public static void Main()
    {
        _ = nameof(alias);
    }
}
");

            verify(comp0Ref, comp1Ref, comp2Ref,
@"
using alias = N1;
public class C2
{
    public static void Main()
    {
        _ = nameof(alias.N2);
    }
}
");

            static void verify(MetadataReference reference0, MetadataReference reference1, MetadataReference reference2, string source)
            {
                AssertUsedAssemblyReferences(source, new[] { reference0, reference1, reference2 }, reference0, reference1);
            }
        }

        [Fact]
        public void NamespaceReference_02()
        {
            var source0 =
@"
namespace N1.N2
{
    public enum E0
    {
        F0
    }
}
";
            var comp0 = CreateCompilation(source0);
            var comp0Ref = comp0.ToMetadataReference();

            var source1 =
@"
namespace N1.N2
{
    public enum E1
    {
    }
}
";
            var comp1 = CreateCompilation(source1);
            comp1.VerifyDiagnostics();
            var comp1Ref = comp1.ToMetadataReference();

            var source2 =
@"
namespace N1
{
    public enum E2
    {
    }
}
";
            var comp2 = CreateCompilation(source2);
            comp2.VerifyDiagnostics();
            var comp2Ref = comp2.ToMetadataReference();

            verify(comp0Ref, comp1Ref, comp2Ref,
@"
public class C2
{
    public static void Main()
    {
        _ = N1.N2.E0.F0;
    }
}
");

            verify(comp0Ref, comp1Ref, comp2Ref,
@"
using alias = N1.N2.E0;
public class C2
{
    public static void Main()
    {
        _ = alias.F0;
    }
}
");

            verify(comp0Ref, comp1Ref, comp2Ref,
@"
using static N1.N2.E0;
public class C2
{
    public static void Main()
    {
        _ = F0;
    }
}
");

            verify(comp0Ref, comp1Ref, comp2Ref,
@"
using alias = N1.N2;
public class C2
{
    public static void Main()
    {
        _ = alias.E0.F0;
    }
}
");

            verify(comp0Ref, comp1Ref, comp2Ref,
@"
using N1.N2;
public class C2
{
    public static void Main()
    {
        _ = E0.F0;
    }
}
");

            verify(comp0Ref, comp1Ref, comp2Ref,
@"
using alias = N1;
public class C2
{
    public static void Main()
    {
        _ = alias.N2.E0.F0;
    }
}
");

            static void verify(MetadataReference reference0, MetadataReference reference1, MetadataReference reference2, string source)
            {
                AssertUsedAssemblyReferences(source, new[] { reference0, reference1, reference2 }, reference0);
            }
        }

        [Fact]
        public void NamespaceReference_03()
        {
            var source0 =
@"
namespace N1.N2
{
    public enum E0
    {
    }
}
";
            var comp0 = CreateCompilation(source0);
            var comp0Ref = comp0.ToMetadataReference();

            var source1 =
@"
namespace N1.N2
{
    public enum E1
    {
    }
}
";
            var comp1 = CreateCompilation(source1);
            comp1.VerifyDiagnostics();
            var comp1Ref = comp1.ToMetadataReference();

            var source2 =
@"
namespace N1
{
    public enum E2
    {
    }
}
";
            var comp2 = CreateCompilation(source2);
            comp2.VerifyDiagnostics();
            var comp2Ref = comp2.ToMetadataReference();

            verify(comp0Ref, comp1Ref, comp2Ref,
@"
class C2
{
    /// <summary>
    /// <see cref=""N1.N2""/>
    /// </summary>
    static void Main()
    {
    }
}
");

            verify(comp0Ref, comp1Ref, comp2Ref,
@"
using alias = N1.N2;
class C2
{
    /// <summary>
    /// <see cref=""alias""/>
    /// </summary>
    static void Main()
    {
    }
}
",
                namespaceOrdinalReferencedInUsings: 2
                );

            verify(comp0Ref, comp1Ref, comp2Ref,
@"
using alias = N1;
class C2
{
    /// <summary>
    /// <see cref=""alias.N2""/>
    /// </summary>
    static void Main()
    {
    }
}
",
                namespaceOrdinalReferencedInUsings: 1
                );

            var source3 =
@"
using N1.N2;
class C2
{
    static void Main()
    {
    }
}
";

            var references = new[] { comp0Ref, comp1Ref, comp2Ref };
            AssertUsedAssemblyReferences(CreateCompilation(source3, references: references, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.None)),
                                         comp0Ref, comp1Ref);
            AssertUsedAssemblyReferences(CreateCompilation(source3, references: references, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.Parse)),
                                         new MetadataReference[] { },
                                         new[] {
                                             // (2,1): hidden CS8019: Unnecessary using directive.
                                             // using N1.N2;
                                             Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N1.N2;").WithLocation(2, 1)
                                         },
                                         new[] {
                                             // (2,1): hidden CS8019: Unnecessary using directive.
                                             // using N1.N2;
                                             Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N1.N2;").WithLocation(2, 1),
                                             // (2,7): error CS0246: The type or namespace name 'N1' could not be found (are you missing a using directive or an assembly reference?)
                                             // using N1.N2;
                                             Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N1").WithArguments("N1").WithLocation(2, 7)
                                         });

            var source4 =
@"
using N1;
class C2
{
    static void Main()
    {
    }
}
";

            AssertUsedAssemblyReferences(CreateCompilation(source4, references: references, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.None)),
                                         references);
            AssertUsedAssemblyReferences(CreateCompilation(source4, references: references, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.Parse)),
                                         new MetadataReference[] { },
                                         new[] {
                                             // (2,1): hidden CS8019: Unnecessary using directive.
                                             // using N1;
                                             Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N1;").WithLocation(2, 1)
                                         },
                                         new[] {
                                             // (2,1): hidden CS8019: Unnecessary using directive.
                                             // using N1;
                                             Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N1;").WithLocation(2, 1),
                                             // (2,7): error CS0246: The type or namespace name 'N1' could not be found (are you missing a
                                             // using N1;
                                             Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N1").WithArguments("N1").WithLocation(2, 7)
                                         });

            var source5 =
@"
class C2
{
    static void Main1()
    {
    }
}
";

            AssertUsedAssemblyReferences(CreateCompilation(source5, references: references,
                                                           parseOptions: TestOptions.Script.WithDocumentationMode(DocumentationMode.None),
                                                           options: TestOptions.DebugDll.WithUsings("N1.N2")),
                                         comp0Ref, comp1Ref);
            AssertUsedAssemblyReferences(CreateCompilation(source5, references: references,
                                                           parseOptions: TestOptions.Script.WithDocumentationMode(DocumentationMode.Parse),
                                                           options: TestOptions.DebugDll.WithUsings("N1.N2")),
                                         comp0Ref, comp1Ref);
            AssertUsedAssemblyReferences(CreateCompilation(source5, references: references,
                                                           parseOptions: TestOptions.Script.WithDocumentationMode(DocumentationMode.None),
                                                           options: TestOptions.DebugDll.WithUsings("N1")),
                                         references);
            AssertUsedAssemblyReferences(CreateCompilation(source5, references: references,
                                                           parseOptions: TestOptions.Script.WithDocumentationMode(DocumentationMode.Parse),
                                                           options: TestOptions.DebugDll.WithUsings("N1")),
                                         references);

            static void verify(MetadataReference reference0, MetadataReference reference1, MetadataReference reference2, string source, int namespaceOrdinalReferencedInUsings = 0)
            {
                var references = new[] { reference0, reference1, reference2 };
                var expected = new[] { reference0, reference1 };
                Compilation comp2 = CreateCompilation(source, references: references, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.None));
                AssertUsedAssemblyReferences(comp2, namespaceOrdinalReferencedInUsings switch { 1 => references, 2 => expected, _ => new MetadataReference[] { } });

                Compilation comp3 = CreateCompilation(source, references: references, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.Parse));
                AssertUsedAssemblyReferences(comp3, expected);

                Compilation comp4 = CreateCompilation(source, references: references, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.Diagnose));
                AssertUsedAssemblyReferences(comp4, expected);
            }
        }

        [Fact]
        public void NamespaceReference_04()
        {
            var source0 =
@"
namespace N1.N2
{
    public enum E0
    {
        F0
    }
}
";
            var comp0 = CreateCompilation(source0);
            var comp0Ref = comp0.ToMetadataReference();

            var source1 =
@"
namespace N1.N2
{
    public enum E1
    {
    }
}
";
            var comp1 = CreateCompilation(source1);
            comp1.VerifyDiagnostics();
            var comp1Ref = comp1.ToMetadataReference();

            var source2 =
@"
namespace N1
{
    public enum E2
    {
    }
}
";
            var comp2 = CreateCompilation(source2);
            comp2.VerifyDiagnostics();
            var comp2Ref = comp2.ToMetadataReference();

            verify(comp0Ref, comp1Ref, comp2Ref,
@"
class C2
{
    /// <summary>
    /// <see cref=""N1.N2.E0""/>
    /// </summary>
    static void Main()
    {
    }
}
");

            verify(comp0Ref, comp1Ref, comp2Ref,
@"
using alias = N1.N2;
class C2
{
    /// <summary>
    /// <see cref=""alias.E0""/>
    /// </summary>
    static void Main()
    {
    }
}
",
                namespaceOrdinalReferencedInUsings: 2
                );

            verify(comp0Ref, comp1Ref, comp2Ref,
@"
using alias = N1.N2.E0;
class C2
{
    /// <summary>
    /// <see cref=""alias""/>
    /// </summary>
    static void Main()
    {
    }
}
",
                namespaceOrdinalReferencedInUsings: 3
                );

            verify(comp0Ref, comp1Ref, comp2Ref,
@"
using static N1.N2.E0;
class C2
{
    /// <summary>
    /// <see cref=""F0""/>
    /// </summary>
    static void Main()
    {
    }
}
",
                namespaceOrdinalReferencedInUsings: 3
                );

            verify(comp0Ref, comp1Ref, comp2Ref,
@"
using alias = N1;
class C2
{
    /// <summary>
    /// <see cref=""alias.N2.E0""/>
    /// </summary>
    static void Main()
    {
    }
}
",
                namespaceOrdinalReferencedInUsings: 1
                );

            var source3 =
@"
using static N1.N2.E0;
class C2
{
    static void Main()
    {
    }
}
";

            var references = new[] { comp0Ref, comp1Ref, comp2Ref };
            AssertUsedAssemblyReferences(CreateCompilation(source3, references: references, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.None)),
                                         comp0Ref);
            AssertUsedAssemblyReferences(CreateCompilation(source3, references: references, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.Parse)),
                                         new MetadataReference[] { },
                                         new[] {
                                             // (2,1): hidden CS8019: Unnecessary using directive.
                                             // using static N1.N2.E0;
                                             Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using static N1.N2.E0;").WithLocation(2, 1)
                                         },
                                         new[] {
                                             // (2,1): hidden CS8019: Unnecessary using directive.
                                             // using static N1.N2.E0;
                                             Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using static N1.N2.E0;").WithLocation(2, 1),
                                             // (2,14): error CS0246: The type or namespace name 'N1' could not be found (are you missing a using directive or an assembly reference?)
                                             // using static N1.N2.E0;
                                             Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N1").WithArguments("N1").WithLocation(2, 14)
                                         });

            var source5 =
@"
class C2
{
    static void Main1()
    {
    }
}
";

            AssertUsedAssemblyReferences(CreateCompilation(source5, references: references,
                                                           parseOptions: TestOptions.Script.WithDocumentationMode(DocumentationMode.None),
                                                           options: TestOptions.DebugDll.WithUsings("N1.N2.E0")),
                                         comp0Ref);
            AssertUsedAssemblyReferences(CreateCompilation(source5, references: references,
                                                           parseOptions: TestOptions.Script.WithDocumentationMode(DocumentationMode.Parse),
                                                           options: TestOptions.DebugDll.WithUsings("N1.N2.E0")),
                                         comp0Ref);

            static void verify(MetadataReference reference0, MetadataReference reference1, MetadataReference reference2, string source, int namespaceOrdinalReferencedInUsings = 0)
            {
                var references = new[] { reference0, reference1, reference2 };
                Compilation comp2 = CreateCompilation(source, references: references, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.None));
                AssertUsedAssemblyReferences(comp2, namespaceOrdinalReferencedInUsings switch { 1 => references, 2 => new[] { reference0, reference1 }, 3 => new[] { reference0 }, _ => new MetadataReference[] { } });

                Compilation comp3 = CreateCompilation(source, references: references, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.Parse));
                AssertUsedAssemblyReferences(comp3, reference0);

                Compilation comp4 = CreateCompilation(source, references: references, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.Diagnose));
                AssertUsedAssemblyReferences(comp4, reference0);
            }
        }

        [Fact]
        public void NamespaceReference_05()
        {
            var source1 =
@"
using global;
class C2
{
    static void Main()
    {
    }
}
";

            CreateCompilation(source1, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.None)).VerifyDiagnostics(
                // (2,7): error CS0246: The type or namespace name 'global' could not be found (are you missing a using directive or an assembly reference?)
                // using global;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "global").WithArguments("global").WithLocation(2, 7)
                );

            var source2 =
@"
using alias = global;
class C2
{
    static void Main()
    {
    }
}
";

            CreateCompilation(source2, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.None)).VerifyDiagnostics(
                // (2,15): error CS0246: The type or namespace name 'global' could not be found (are you missing a using directive or an assembly reference?)
                // using alias = global;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "global").WithArguments("global").WithLocation(2, 15)
                );
        }

        [Fact]
        public void NamespaceReference_06()
        {
            var source1 =
@"
using global::;
class C2
{
    static void Main()
    {
    }
}
";

            CreateCompilation(source1, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.None)).VerifyDiagnostics(
                // (2,15): error CS1001: Identifier expected
                // using global::;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(2, 15)
                );

            var source2 =
@"
using alias = global::;
class C2
{
    static void Main()
    {
    }
}
";

            CreateCompilation(source2, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.None)).VerifyDiagnostics(
                // (2,23): error CS1001: Identifier expected
                // using alias = global::;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(2, 23)
                );
        }

        [Fact]
        public void ExternAlias_01()
        {
            var source1 =
@"
namespace N1
{
    public static class C1
    {
    }
}
";
            var comp1 = CreateCompilation(source1);
            var comp1Ref = comp1.ToMetadataReference().WithAliases(new[] { "N1C1" });

            var source2 =
@"
namespace N1
{
    public static class C2
    {
    }
}
";
            var comp2 = CreateCompilation(source2);
            var comp2Ref = comp2.ToMetadataReference();

            var source3 =
@"
extern alias N1C1;

public class C2
{
}
";
            var references = new[] { comp1Ref, comp2Ref };
            AssertUsedAssemblyReferences(CreateCompilation(source3, references: references, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.None)),
                                         comp1Ref);
            AssertUsedAssemblyReferences(CreateCompilation(source3, references: references, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.Parse)),
                                         new MetadataReference[] { },
                                         new[] {
                                             // (2,1): hidden CS8020: Unused extern alias.
                                             // extern alias N1C1;
                                             Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias N1C1;").WithLocation(2, 1)
                                         },
                                         new[] {
                                             // (2,1): hidden CS8020: Unused extern alias.
                                             // extern alias N1C1;
                                             Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias N1C1;").WithLocation(2, 1),
                                             // (2,14): error CS0430: The extern alias 'N1C1' was not specified in a /reference option
                                             // extern alias N1C1;
                                             Diagnostic(ErrorCode.ERR_BadExternAlias, "N1C1").WithArguments("N1C1").WithLocation(2, 14)
                                         });

            comp2Ref = comp2.ToMetadataReference().WithAliases(new[] { "N1C1" });
            references = new[] { comp1Ref, comp2Ref };

            AssertUsedAssemblyReferences(CreateCompilation(source3, references: references, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.None)),
                                         references);
            AssertUsedAssemblyReferences(CreateCompilation(source3, references: references, parseOptions: TestOptions.Regular.WithDocumentationMode(DocumentationMode.Parse)),
                                         new MetadataReference[] { },
                                         new[] {
                                             // (2,1): hidden CS8020: Unused extern alias.
                                             // extern alias N1C1;
                                             Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias N1C1;").WithLocation(2, 1)
                                         },
                                         new[] {
                                             // (2,1): hidden CS8020: Unused extern alias.
                                             // extern alias N1C1;
                                             Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias N1C1;").WithLocation(2, 1),
                                             // (2,14): error CS0430: The extern alias 'N1C1' was not specified in a /reference option
                                             // extern alias N1C1;
                                             Diagnostic(ErrorCode.ERR_BadExternAlias, "N1C1").WithArguments("N1C1").WithLocation(2, 14)
                                         });
        }

    }
}
