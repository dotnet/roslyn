// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Metadata.PE
{
    public class LoadingGenericTypeParameters : CSharpTestBase
    {
        [Fact]
        public void Test1()
        {
            var assembly = MetadataTestHelpers.GetSymbolForReference(TestMetadata.Net40.mscorlib);
            var module0 = assembly.Modules[0];

            var objectType = module0.GlobalNamespace.GetMembers("System").
                OfType<NamespaceSymbol>().Single().
                GetTypeMembers("Object").Single();

            Assert.Equal(0, objectType.Arity);
            Assert.Equal(0, objectType.TypeParameters.Length);
            Assert.Equal(0, objectType.TypeArguments().Length);

            assembly = MetadataTestHelpers.GetSymbolForReference(TestReferences.SymbolsTests.MDTestLib1);
            module0 = assembly.Modules[0];

            var varC1 = module0.GlobalNamespace.GetTypeMembers("C1").Single();

            Assert.Equal(1, varC1.Arity);
            Assert.Equal(1, varC1.TypeParameters.Length);
            Assert.Equal(1, varC1.TypeArguments().Length);

            var varC1_T = varC1.TypeParameters[0];

            Assert.Equal(varC1_T, varC1.TypeArguments()[0]);

            Assert.NotNull(varC1_T.EffectiveBaseClassNoUseSiteDiagnostics);
            Assert.Equal(assembly, varC1_T.ContainingAssembly);
            Assert.Equal(module0.GlobalNamespace, varC1_T.ContainingNamespace); // Null(C1_T.ContainingNamespace)
            Assert.Equal(varC1, varC1_T.ContainingSymbol);
            Assert.Equal(varC1, varC1_T.ContainingType);
            Assert.Equal(Accessibility.NotApplicable, varC1_T.DeclaredAccessibility);
            Assert.Equal("C1_T", varC1_T.Name);
            Assert.Equal("C1_T", varC1_T.ToTestDisplayString());
            Assert.Equal(0, varC1_T.GetMembers().Length);
            Assert.Equal(0, varC1_T.GetMembers("goo").Length);
            Assert.Equal(0, varC1_T.GetTypeMembers().Length);
            Assert.Equal(0, varC1_T.GetTypeMembers("goo").Length);
            Assert.Equal(0, varC1_T.GetTypeMembers("goo", 1).Length);
            Assert.False(varC1_T.HasConstructorConstraint);
            Assert.False(varC1_T.HasReferenceTypeConstraint);
            Assert.False(varC1_T.HasValueTypeConstraint);
            Assert.Equal(0, varC1_T.EffectiveInterfacesNoUseSiteDiagnostics.Length);
            Assert.True(varC1_T.IsDefinition);
            Assert.False(varC1_T.IsAbstract);
            Assert.False(varC1_T.IsNamespace);
            Assert.False(varC1_T.IsSealed);
            Assert.False(varC1_T.IsVirtual);
            Assert.False(varC1_T.IsOverride);
            Assert.False(varC1_T.IsStatic);
            Assert.True(varC1_T.IsType);
            Assert.Equal(SymbolKind.TypeParameter, varC1_T.Kind);
            Assert.Equal(0, varC1_T.Ordinal);
            Assert.Equal(varC1_T, varC1_T.OriginalDefinition);
            Assert.Equal(TypeKind.TypeParameter, varC1_T.TypeKind);
            Assert.Equal(VarianceKind.None, varC1_T.Variance);
            Assert.Same(module0, varC1_T.Locations.Single().MetadataModuleInternal);
            Assert.Equal(0, varC1_T.ConstraintTypes().Length);

            var varC2 = varC1.GetTypeMembers("C2").Single();
            Assert.Equal(1, varC2.Arity);
            Assert.Equal(1, varC2.TypeParameters.Length);
            Assert.Equal(1, varC2.TypeArguments().Length);

            var varC2_T = varC2.TypeParameters[0];

            Assert.Equal("C2_T", varC2_T.Name);
            Assert.Equal(varC2, varC2_T.ContainingType);

            var varC3 = varC1.GetTypeMembers("C3").Single();
            Assert.Equal(0, varC3.Arity);
            Assert.Equal(0, varC3.TypeParameters.Length);
            Assert.Equal(0, varC3.TypeArguments().Length);

            var varC4 = varC3.GetTypeMembers("C4").Single();
            Assert.Equal(1, varC4.Arity);
            Assert.Equal(1, varC4.TypeParameters.Length);
            Assert.Equal(1, varC4.TypeArguments().Length);

            var varC4_T = varC4.TypeParameters[0];

            Assert.Equal("C4_T", varC4_T.Name);
            Assert.Equal(varC4, varC4_T.ContainingType);

            var varTC2 = module0.GlobalNamespace.GetTypeMembers("TC2").Single();

            Assert.Equal(2, varTC2.Arity);
            Assert.Equal(2, varTC2.TypeParameters.Length);
            Assert.Equal(2, varTC2.TypeArguments().Length);

            var varTC2_T1 = varTC2.TypeParameters[0];
            var varTC2_T2 = varTC2.TypeParameters[1];

            Assert.Equal(varTC2_T1, varTC2.TypeArguments()[0]);
            Assert.Equal(varTC2_T2, varTC2.TypeArguments()[1]);

            Assert.Equal("TC2_T1", varTC2_T1.Name);
            Assert.Equal(varTC2, varTC2_T1.ContainingType);
            Assert.Equal(0, varTC2_T1.Ordinal);

            Assert.Equal("TC2_T2", varTC2_T2.Name);
            Assert.Equal(varTC2, varTC2_T2.ContainingType);
            Assert.Equal(1, varTC2_T2.Ordinal);

            var varC100 = module0.GlobalNamespace.GetTypeMembers("C100").Single();
            var varT = varC100.TypeParameters[0];
            Assert.False(varT.HasConstructorConstraint);
            Assert.False(varT.HasReferenceTypeConstraint);
            Assert.False(varT.HasValueTypeConstraint);
            Assert.Equal(VarianceKind.Out, varT.Variance);

            var varC101 = module0.GlobalNamespace.GetTypeMembers("C101").Single();
            varT = varC101.TypeParameters[0];
            Assert.False(varT.HasConstructorConstraint);
            Assert.False(varT.HasReferenceTypeConstraint);
            Assert.False(varT.HasValueTypeConstraint);
            Assert.Equal(VarianceKind.In, varT.Variance);

            var varC102 = module0.GlobalNamespace.GetTypeMembers("C102").Single();
            varT = varC102.TypeParameters[0];
            Assert.True(varT.HasConstructorConstraint);
            Assert.False(varT.HasReferenceTypeConstraint);
            Assert.False(varT.HasValueTypeConstraint);
            Assert.Equal(VarianceKind.None, varT.Variance);
            Assert.Equal(0, varT.ConstraintTypes().Length);

            var varC103 = module0.GlobalNamespace.GetTypeMembers("C103").Single();
            varT = varC103.TypeParameters[0];
            Assert.False(varT.HasConstructorConstraint);
            Assert.True(varT.HasReferenceTypeConstraint);
            Assert.False(varT.HasValueTypeConstraint);
            Assert.Equal(VarianceKind.None, varT.Variance);
            Assert.Equal(0, varT.ConstraintTypes().Length);

            var varC104 = module0.GlobalNamespace.GetTypeMembers("C104").Single();
            varT = varC104.TypeParameters[0];
            Assert.False(varT.HasConstructorConstraint);
            Assert.False(varT.HasReferenceTypeConstraint);
            Assert.True(varT.HasValueTypeConstraint);
            Assert.Equal(VarianceKind.None, varT.Variance);
            Assert.Equal(0, varT.ConstraintTypes().Length);

            var varC105 = module0.GlobalNamespace.GetTypeMembers("C105").Single();
            varT = varC105.TypeParameters[0];
            Assert.True(varT.HasConstructorConstraint);
            Assert.True(varT.HasReferenceTypeConstraint);
            Assert.False(varT.HasValueTypeConstraint);
            Assert.Equal(VarianceKind.None, varT.Variance);

            var varC106 = module0.GlobalNamespace.GetTypeMembers("C106").Single();
            varT = varC106.TypeParameters[0];
            Assert.True(varT.HasConstructorConstraint);
            Assert.True(varT.HasReferenceTypeConstraint);
            Assert.False(varT.HasValueTypeConstraint);
            Assert.Equal(VarianceKind.Out, varT.Variance);

            var varI101 = module0.GlobalNamespace.GetTypeMembers("I101").Single();
            var varI102 = module0.GlobalNamespace.GetTypeMembers("I102").Single();

            var varC201 = module0.GlobalNamespace.GetTypeMembers("C201").Single();
            varT = varC201.TypeParameters[0];
            Assert.Equal(1, varT.ConstraintTypes().Length);
            Assert.Same(varI101, varT.ConstraintTypes().ElementAt(0));

            var localC202 = module0.GlobalNamespace.GetTypeMembers("C202").Single();
            varT = localC202.TypeParameters[0];
            Assert.Equal(2, varT.ConstraintTypes().Length);
            Assert.Same(varI101, varT.ConstraintTypes().ElementAt(0));
            Assert.Same(varI102, varT.ConstraintTypes().ElementAt(1));
        }

        [Fact, WorkItem(619267, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/619267")]
        public void InvalidNestedArity()
        {
            // .class public C`2<T1,T2>
            // .class nested public D<S1>
            var mdRef = MetadataReference.CreateFromImage(TestResources.MetadataTests.Invalid.InvalidGenericType.AsImmutableOrNull());
            string source = "class X : C<int, int>.D { }";
            CreateCompilation(source, new[] { mdRef }).VerifyDiagnostics(
                // (2,11): error CS0648: 'C<T1, T2>.D' is a type not supported by the language
                // class X : C<int, int>.D { }
                Diagnostic(ErrorCode.ERR_BogusType, "C<int, int>.D").WithArguments("C<T1, T2>.D")
                );
        }

        [WorkItem(528859, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528859")]
        [Fact]
        public void InvalidNestedArity_2()
        {
            var ilSource =
@".class interface public abstract I0
{
  .class interface abstract nested public IT<T>
  {
    .class interface abstract nested public I0 { }
  }
}
.class interface public abstract IT<T>
{
  .class interface abstract nested public I0
  {
    .class interface abstract nested public I0 { }
    .class interface abstract nested public IT<T> { }
  }
  .class interface abstract nested public IT<T>
  {
    .class interface abstract nested public I0 { }
  }
  .class interface abstract nested public ITU<T, U>
  {
    .class interface abstract nested public IT<T> { }
  }
}";
            var csharpSource =
@"class C0_T : I0.IT<object> { }
class C0_T_0 : I0.IT<object>.I0 { }
class CT_0 : IT<object>.I0 { }
class CT_0_0 : IT<object>.I0.I0 { }
class CT_0_T : IT<object>.I0.IT { }
class CT_T_0 : IT<object>.IT.I0 { }
class CT_TU_T : IT<object>.ITU<int>.IT { }
";
            var compilation1 = CreateCompilationWithILAndMscorlib40(csharpSource, ilSource);
            compilation1.VerifyDiagnostics(
                // (2,7): error CS0648: 'I0.IT<T>.I0' is a type not supported by the language
                // class C0_T_0 : I0.IT<object>.I0 { }
                Diagnostic(ErrorCode.ERR_BogusType, "C0_T_0").WithArguments("I0.IT<T>.I0"),
                // (3,7): error CS0648: 'IT<T>.I0' is a type not supported by the language
                // class CT_0 : IT<object>.I0 { }
                Diagnostic(ErrorCode.ERR_BogusType, "CT_0").WithArguments("IT<T>.I0"),
                // (4,27): error CS0648: 'IT<T>.I0' is a type not supported by the language
                // class CT_0_0 : IT<object>.I0.I0 { }
                Diagnostic(ErrorCode.ERR_BogusType, "I0").WithArguments("IT<T>.I0"),
                // (4,7): error CS0648: 'IT<T>.I0.I0' is a type not supported by the language
                // class CT_0_0 : IT<object>.I0.I0 { }
                Diagnostic(ErrorCode.ERR_BogusType, "CT_0_0").WithArguments("IT<T>.I0.I0"),
                // (5,27): error CS0648: 'IT<T>.I0' is a type not supported by the language
                // class CT_0_T : IT<object>.I0.IT { }
                Diagnostic(ErrorCode.ERR_BogusType, "I0").WithArguments("IT<T>.I0"),
                // (6,7): error CS0648: 'IT<T>.IT.I0' is a type not supported by the language
                // class CT_T_0 : IT<object>.IT.I0 { }
                Diagnostic(ErrorCode.ERR_BogusType, "CT_T_0").WithArguments("IT<T>.IT.I0"),
                // (7,7): error CS0648: 'IT<T>.ITU<U>.IT' is a type not supported by the language
                // class CT_TU_T : IT<object>.ITU<int>.IT { }
                Diagnostic(ErrorCode.ERR_BogusType, "CT_TU_T").WithArguments("IT<T>.ITU<U>.IT"));
        }
    }
}
