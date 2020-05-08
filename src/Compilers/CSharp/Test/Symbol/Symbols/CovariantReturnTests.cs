// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class CovariantReturnTests : CSharpTestBase
    {
        private static void VerifyOverride(
            CSharpCompilation comp,
            string methodName,
            string overridingMethodDisplay,
            string overriddenMethodDisplay,
            bool binaryCompat = false)
        {
            var member = comp.GlobalNamespace.GetMember(methodName);
            Assert.Equal(overridingMethodDisplay, member.ToTestDisplayString());
            var overriddenMember = member.GetOverriddenMember();
            Assert.Equal(overriddenMethodDisplay, overriddenMember?.ToTestDisplayString());
            if (member is MethodSymbol method && overriddenMember is MethodSymbol overriddenMethod)
            {
                Assert.True(method.IsOverride);
                Assert.False(method.IsVirtual);
                Assert.True(method.IsMetadataVirtual(ignoreInterfaceImplementationChanges: true));
                var isCovariant = !method.ReturnType.Equals(overriddenMethod.ReturnType, TypeCompareKind.AllIgnoreOptions);
                var checkMetadata = !binaryCompat && hasReturnConversion(method.ReturnType, overriddenMethod.ReturnType);
                if (checkMetadata)
                {
                    Assert.Equal(isCovariant, method.IsMetadataNewSlot(ignoreInterfaceImplementationChanges: true));
                    Assert.Equal(isCovariant, method.RequiresExplicitOverride()); // implies the presence of a methodimpl
                }
            }
            else if (member is PropertySymbol property && overriddenMember is PropertySymbol overriddenProperty)
            {
                var isCovariant = !property.Type.Equals(overriddenProperty.Type, TypeCompareKind.AllIgnoreOptions);
                if (property.GetMethod is MethodSymbol getMethod && overriddenProperty.GetMethod is MethodSymbol overriddenGetMethod)
                {
                    Assert.True(getMethod.GetOverriddenMember().Equals(overriddenGetMethod));
                    var checkMetadata = !binaryCompat && hasReturnConversion(property.Type, overriddenProperty.Type);
                    if (checkMetadata)
                    {
                        Assert.Equal(isCovariant, getMethod.IsMetadataNewSlot(ignoreInterfaceImplementationChanges: true));
                        Assert.Equal(isCovariant, getMethod.RequiresExplicitOverride()); // implies the presence of a methodimpl
                    }
                }
                if (property.SetMethod is MethodSymbol setMethod && overriddenProperty.SetMethod is MethodSymbol overriddenSetMethod)
                {
                    Assert.False(setMethod.IsMetadataNewSlot(ignoreInterfaceImplementationChanges: true));
                    Assert.False(setMethod.RequiresExplicitOverride());
                    Assert.Equal(!isCovariant, overriddenSetMethod.Equals(setMethod.GetOverriddenMember(), TypeCompareKind.AllIgnoreOptions));
                }
            }
            else if (member is EventSymbol eventSymbol && overriddenMember is EventSymbol overriddenEvent)
            {
                var isCovariant = !eventSymbol.Type.Equals(overriddenEvent.Type, TypeCompareKind.AllIgnoreOptions);
                if (eventSymbol.AddMethod is MethodSymbol addMethod && overriddenEvent.AddMethod is MethodSymbol overriddenAddMethod)
                {
                    Assert.Equal(!isCovariant, overriddenAddMethod.Equals(addMethod.GetOverriddenMember(), TypeCompareKind.AllIgnoreOptions));
                }
                if (eventSymbol.RemoveMethod is MethodSymbol removeMethod && overriddenEvent.RemoveMethod is MethodSymbol overriddenRemoveMethod)
                {
                    Assert.Equal(!isCovariant, overriddenRemoveMethod.Equals(removeMethod.GetOverriddenMember(), TypeCompareKind.AllIgnoreOptions));
                }
            }
            else
            {
                Assert.True(false);
            }

            bool hasReturnConversion(TypeSymbol fromType, TypeSymbol toType)
            {
                HashSet<DiagnosticInfo> ignoredUseSiteDiagnostics = null;
                return comp.Conversions.HasIdentityOrImplicitReferenceConversion(fromType, toType, ref ignoredUseSiteDiagnostics);
            }
        }

        private static void VerifyNoOverride(CSharpCompilation comp, string methodName)
        {
            var method = comp.GlobalNamespace.GetMember(methodName);
            var overridden = method.GetOverriddenMember();
            Assert.Null(overridden);
        }

        /// <summary>
        /// Verify that all assignments in the compilation's source have the same type and converted type.
        /// </summary>
        private static void VerifyAssignments(CSharpCompilation comp, int expectedAssignments)
        {
            int foundAssignments = 0;
            foreach (var tree in comp.SyntaxTrees)
            {
                var model = comp.GetSemanticModel(tree);
                foreach (var declaration in tree.GetRoot().DescendantNodes().OfType<LocalDeclarationStatementSyntax>())
                {
                    foreach (var declarator in declaration.Declaration.Variables)
                    {
                        if (declarator.Initializer is { Value: ExpressionSyntax right })
                        {
                            var typeInfo = model.GetTypeInfo(right);
                            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
                            foundAssignments++;
                        }
                    }
                }
                foreach (var assignment in tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>())
                {
                    var right = assignment.Right;
                    var typeInfo = model.GetTypeInfo(right);
                    Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
                    foundAssignments++;
                }
            }

            Assert.Equal(expectedAssignments, foundAssignments);
        }

        private CSharpCompilation CreateCompilationWithCovariantReturns(
            string source, MetadataReference[] references = null,
            TargetFramework targetFramework = TargetFramework.Standard,
            string assemblyName = "")
        {
            return CreateCompilation(
                source,
                parseOptions: TestOptions.WithCovariantReturns,
                references: references,
                targetFramework: targetFramework,
                assemblyName: assemblyName);
        }

        private CSharpCompilation CreateCompilationWithoutCovariantReturns(
            string source,
            MetadataReference[] references = null,
            TargetFramework targetFramework = TargetFramework.Standard,
            string assemblyName = "")
        {
            return CreateCompilation(
                source,
                parseOptions: TestOptions.WithoutCovariantReturns,
                references: references,
                targetFramework: targetFramework,
                assemblyName: assemblyName);
        }

        private static CSharpCompilation CompilationReferenceView(CSharpCompilation comp, params MetadataReference[] references)
        {
            references = references.Append(comp.ToMetadataReference());
            return CreateCompilation("", references: references);
        }

        private static CSharpCompilation MetadataView(CSharpCompilation comp, params MetadataReference[] references)
        {
            references = references.Append(comp.EmitToImageReference());
            return CreateCompilation("", references: references);
        }

        private static CSharpCompilation RetargetedView(CSharpCompilation comp, params MetadataReference[] references)
        {
            var someType = comp.GlobalNamespace.GetMembers().OfType<NamedTypeSymbol>().FirstOrDefault();
            references = references.Append(comp.ToMetadataReference());
            var result = CreateCompilation("", references: references, targetFramework: TargetFramework.NetStandardLatest);
            result.VerifyDiagnostics();
            var originalCorLib = comp.Assembly.CorLibrary;
            var newCorLib = result.Assembly.CorLibrary;
            Assert.NotEqual(originalCorLib, newCorLib);

            if (someType is { })
            {
                // confirm that it is retargeted
                var someRetargetedType = result.GlobalNamespace.GetMembers(someType.Name).OfType<NamedTypeSymbol>().FirstOrDefault();
                var someRetargetedMember = someRetargetedType.GetMembers().OfType<MethodSymbol>().FirstOrDefault();
                Assert.True(someRetargetedMember is null || someRetargetedMember is CSharp.Symbols.Retargeting.RetargetingMethodSymbol);
            }

            return result;
        }

        [Fact]
        public void CovariantReturns_00()
        {
            var source = @"
public class Base
{
    public virtual string M() => null;
}
public class Derived : Base
{
    public override string M() => null;
}
public class Program
{
    void M(Base b, Derived d)
    {
        string s1 = b.M();
        string s2 = d.M();
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            verify(CompilationReferenceView(comp));
            verify(MetadataView(comp));
            verify(RetargetedView(comp));

            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            verify(CompilationReferenceView(comp));
            verify(MetadataView(comp));
            verify(RetargetedView(comp));
            CompileAndVerify(comp).VerifyIL("Program.M(Base, Derived)", source: source, sequencePoints: "Program.M", expectedIL: @"
    {
      // Code size       15 (0xf)
      .maxstack  1
      // sequence point: string s1 = b.M();
      IL_0000:  ldarg.1
      IL_0001:  callvirt   ""string Base.M()""
      IL_0006:  pop
      // sequence point: string s2 = d.M();
      IL_0007:  ldarg.2
      IL_0008:  callvirt   ""string Base.M()""
      IL_000d:  pop
      // sequence point: }
      IL_000e:  ret
    }
");

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M", "System.String Derived.M()", "System.String Base.M()");
            }
        }

        [Fact]
        public void CovariantReturns_01()
        {
            var source = @"
public class Base
{
    public virtual object M() => null;
}
public class Derived : Base
{
    public override string M() => null;
}
public class Program
{
    void M(Base b, Derived d)
    {
        object s1 = b.M();
        string s2 = d.M();
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (8,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string M() => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("covariant returns").WithLocation(8, 28)
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            verify(CompilationReferenceView(comp));
            verify(MetadataView(comp));
            verify(RetargetedView(comp));
            CompileAndVerify(comp, verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived)", source: source, sequencePoints: "Program.M", expectedIL: @"
{
  // Code size       15 (0xf)
  .maxstack  1
  // sequence point: object s1 = b.M();
  IL_0000:  ldarg.1
  IL_0001:  callvirt   ""object Base.M()""
  IL_0006:  pop
  // sequence point: string s2 = d.M();
  IL_0007:  ldarg.2
  IL_0008:  callvirt   ""string Derived.M()""
  IL_000d:  pop
  // sequence point: }
  IL_000e:  ret
}
");

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M", "System.String Derived.M()", "System.Object Base.M()");
            }
        }

        [Fact]
        public void CovariantReturns_02()
        {
            var source = @"
public class Base
{
    public virtual T M<T, U>() where T : class where U : class, T => null;
}
public class Derived : Base
{
    public override U M<T, U>() => null;
}
public class Program
{
    void M(Base b, Derived d)
    {
        object s1 = b.M<object, string>();
        string s2 = d.M<object, string>();
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (8,23): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override U M<T, U>() => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("covariant returns").WithLocation(8, 23)
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            verify(CompilationReferenceView(comp));
            verify(MetadataView(comp));
            verify(RetargetedView(comp));
            CompileAndVerify(comp, verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived)", source: source, sequencePoints: "Program.M", expectedIL: @"
{
  // Code size       15 (0xf)
  .maxstack  1
  // sequence point: object s1 = b.M<object, string>();
  IL_0000:  ldarg.1
  IL_0001:  callvirt   ""object Base.M<object, string>()""
  IL_0006:  pop
  // sequence point: string s2 = d.M<object, string>();
  IL_0007:  ldarg.2
  IL_0008:  callvirt   ""string Derived.M<object, string>()""
  IL_000d:  pop
  // sequence point: }
  IL_000e:  ret
}
");

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M", "U Derived.M<T, U>()", "T Base.M<T, U>()");
            }
        }

        [Fact]
        public void CovariantReturns_03()
        {
            var source = @"
public class Base<T> where T : class
{
    public virtual T M() => null;
}
public class Derived<T, U> : Base<T> where T : class where U : class, T
{
    public override U M() => null;
}
public class Program
{
    void M(Base<object> b, Derived<object, string> d)
    {
        object s1 = b.M();
        string s2 = d.M();
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (8,23): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override U M() => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("covariant returns").WithLocation(8, 23)
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            verify(CompilationReferenceView(comp));
            verify(MetadataView(comp));
            verify(RetargetedView(comp));
            CompileAndVerify(comp, verify: Verification.Skipped).VerifyIL("Program.M(Base<object>, Derived<object, string>)", source: source, sequencePoints: "Program.M", expectedIL: @"
{
  // Code size       15 (0xf)
  .maxstack  1
  // sequence point: object s1 = b.M();
  IL_0000:  ldarg.1
  IL_0001:  callvirt   ""object Base<object>.M()""
  IL_0006:  pop
  // sequence point: string s2 = d.M();
  IL_0007:  ldarg.2
  IL_0008:  callvirt   ""string Derived<object, string>.M()""
  IL_000d:  pop
  // sequence point: }
  IL_000e:  ret
}
");

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M", "U Derived<T, U>.M()", "T Base<T>.M()");
            }
        }

        [Fact]
        public void CovariantReturns_04()
        {
            var source = @"
public class N { }
public class Base
{
    public virtual N M() => null;
}
public class Derived<T> : Base where T : N
{
    public override T M() => null;
}
public class Q : N { }
public class Program
{
    void M(Base b, Derived<Q> d)
    {
        N s1 = b.M();
        Q s2 = d.M();
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (9,23): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override T M() => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("covariant returns").WithLocation(9, 23)
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            verify(CompilationReferenceView(comp));
            verify(MetadataView(comp));
            verify(RetargetedView(comp));
            CompileAndVerify(comp, verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived<Q>)", source: source, sequencePoints: "Program.M", expectedIL: @"
{
  // Code size       15 (0xf)
  .maxstack  1
  // sequence point: N s1 = b.M();
  IL_0000:  ldarg.1
  IL_0001:  callvirt   ""N Base.M()""
  IL_0006:  pop
  // sequence point: Q s2 = d.M();
  IL_0007:  ldarg.2
  IL_0008:  callvirt   ""Q Derived<Q>.M()""
  IL_000d:  pop
  // sequence point: }
  IL_000e:  ret
}
");

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M", "T Derived<T>.M()", "N Base.M()");
            }
        }

        [Fact]
        public void CovariantReturns_05()
        {
            var source = @"
public class Base
{
    public virtual object M => null;
}
public class Derived : Base
{
    public override string M => null;
}
public class Program
{
    void M(Base b, Derived d)
    {
        object s1 = b.M;
        string s2 = d.M;
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (8,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string M => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("covariant returns").WithLocation(8, 28)
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            verify(CompilationReferenceView(comp));
            verify(MetadataView(comp));
            verify(RetargetedView(comp));
            CompileAndVerify(comp, verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived)", source: source, sequencePoints: "Program.M", expectedIL: @"
{
  // Code size       15 (0xf)
  .maxstack  1
  // sequence point: object s1 = b.M;
  IL_0000:  ldarg.1
  IL_0001:  callvirt   ""object Base.M.get""
  IL_0006:  pop
  // sequence point: string s2 = d.M;
  IL_0007:  ldarg.2
  IL_0008:  callvirt   ""string Derived.M.get""
  IL_000d:  pop
  // sequence point: }
  IL_000e:  ret
}
");

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M", "System.String Derived.M { get; }", "System.Object Base.M { get; }");
                VerifyOverride(comp, "Derived.get_M", "System.String Derived.M.get", "System.Object Base.M.get");
            }
        }

        [Fact]
        public void CovariantReturns_06()
        {
            var source = @"
public class Base<T> where T : class
{
    public virtual T M => null;
}
public class Derived<T, U> : Base<T> where T : class where U : class, T
{
    public override U M => null;
}
public class Program
{
    void M(Base<object> b, Derived<object, string> d)
    {
        object s1 = b.M;
        string s2 = d.M;
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (8,23): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override U M => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("covariant returns").WithLocation(8, 23)
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            verify(CompilationReferenceView(comp));
            verify(MetadataView(comp));
            verify(RetargetedView(comp));
            CompileAndVerify(comp, verify: Verification.Skipped).VerifyIL("Program.M(Base<object>, Derived<object, string>)", source: source, sequencePoints: "Program.M", expectedIL: @"
{
  // Code size       15 (0xf)
  .maxstack  1
  // sequence point: object s1 = b.M;
  IL_0000:  ldarg.1
  IL_0001:  callvirt   ""object Base<object>.M.get""
  IL_0006:  pop
  // sequence point: string s2 = d.M;
  IL_0007:  ldarg.2
  IL_0008:  callvirt   ""string Derived<object, string>.M.get""
  IL_000d:  pop
  // sequence point: }
  IL_000e:  ret
}
");

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M", "U Derived<T, U>.M { get; }", "T Base<T>.M { get; }");
                VerifyOverride(comp, "Derived.get_M", "U Derived<T, U>.M.get", "T Base<T>.M.get");
            }
        }

        [Fact]
        public void CovariantReturns_07()
        {
            var source = @"
public class N { }
public class Base
{
    public virtual N M => null;
}
public class Derived<T> : Base where T : N
{
    public override T M => null;
}
public class Q : N { }
public class Program
{
    void M(Base b, Derived<Q> d)
    {
        N s1 = b.M;
        Q s2 = d.M;
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (9,23): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override T M => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("covariant returns").WithLocation(9, 23)
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            verify(CompilationReferenceView(comp));
            verify(MetadataView(comp));
            verify(RetargetedView(comp));
            CompileAndVerify(comp, verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived<Q>)", source: source, sequencePoints: "Program.M", expectedIL: @"
{
  // Code size       15 (0xf)
  .maxstack  1
  // sequence point: N s1 = b.M;
  IL_0000:  ldarg.1
  IL_0001:  callvirt   ""N Base.M.get""
  IL_0006:  pop
  // sequence point: Q s2 = d.M;
  IL_0007:  ldarg.2
  IL_0008:  callvirt   ""Q Derived<Q>.M.get""
  IL_000d:  pop
  // sequence point: }
  IL_000e:  ret
}
");

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M", "T Derived<T>.M { get; }", "N Base.M { get; }");
                VerifyOverride(comp, "Derived.get_M", "T Derived<T>.M.get", "N Base.M.get");
            }
        }

        [Fact]
        public void CovariantReturns_08()
        {
            var source = @"
public class Base
{
    public virtual object this[int i] => null;
}
public class Derived : Base
{
    public override string this[int i] => null;
}
public class Program
{
    void M(Base b, Derived d)
    {
        object s1 = b[0];
        string s2 = d[0];
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (8,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string this[int i] => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "this").WithArguments("covariant returns").WithLocation(8, 28)
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            verify(CompilationReferenceView(comp));
            verify(MetadataView(comp));
            verify(RetargetedView(comp));
            CompileAndVerify(comp, verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived)", source: source, sequencePoints: "Program.M", expectedIL: @"
{
  // Code size       17 (0x11)
  .maxstack  2
  // sequence point: object s1 = b[0];
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.0
  IL_0002:  callvirt   ""object Base.this[int].get""
  IL_0007:  pop
  // sequence point: string s2 = d[0];
  IL_0008:  ldarg.2
  IL_0009:  ldc.i4.0
  IL_000a:  callvirt   ""string Derived.this[int].get""
  IL_000f:  pop
  // sequence point: }
  IL_0010:  ret
}
");

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.this[]", "System.String Derived.this[System.Int32 i] { get; }", "System.Object Base.this[System.Int32 i] { get; }");
                VerifyOverride(comp, "Derived.get_Item", "System.String Derived.this[System.Int32 i].get", "System.Object Base.this[System.Int32 i].get");
            }
        }

        [Fact]
        public void CovariantReturns_09()
        {
            var source = @"
public class Base<T> where T : class
{
    public virtual T this[int i] => null;
}
public class Derived<T, U> : Base<T> where T : class where U : class, T
{
    public override U this[int i] => null;
}
public class Program
{
    void M(Base<object> b, Derived<object, string> d)
    {
        object s1 = b[0];
        string s2 = d[0];
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (8,23): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override U this[int i] => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "this").WithArguments("covariant returns").WithLocation(8, 23)
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            verify(CompilationReferenceView(comp));
            verify(MetadataView(comp));
            verify(RetargetedView(comp));
            CompileAndVerify(comp, verify: Verification.Skipped).VerifyIL("Program.M(Base<object>, Derived<object, string>)", source: source, sequencePoints: "Program.M", expectedIL: @"
{
  // Code size       17 (0x11)
  .maxstack  2
  // sequence point: object s1 = b[0];
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.0
  IL_0002:  callvirt   ""object Base<object>.this[int].get""
  IL_0007:  pop
  // sequence point: string s2 = d[0];
  IL_0008:  ldarg.2
  IL_0009:  ldc.i4.0
  IL_000a:  callvirt   ""string Derived<object, string>.this[int].get""
  IL_000f:  pop
  // sequence point: }
  IL_0010:  ret
}
");

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.this[]", "U Derived<T, U>.this[System.Int32 i] { get; }", "T Base<T>.this[System.Int32 i] { get; }");
                VerifyOverride(comp, "Derived.get_Item", "U Derived<T, U>.this[System.Int32 i].get", "T Base<T>.this[System.Int32 i].get");
            }
        }

        [Fact]
        public void CovariantReturns_10()
        {
            var source = @"
public class N { }
public class Base
{
    public virtual N this[int i] => null;
}
public class Derived<T> : Base where T : N
{
    public override T this[int i] => null;
}
public class Q : N { }
public class Program
{
    void M(Base b, Derived<Q> d)
    {
        N s1 = b[0];
        Q s2 = d[0];
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (9,23): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override T this[int i] => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "this").WithArguments("covariant returns").WithLocation(9, 23)
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            verify(CompilationReferenceView(comp));
            verify(MetadataView(comp));
            verify(RetargetedView(comp));
            CompileAndVerify(comp, verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived<Q>)", source: source, sequencePoints: "Program.M", expectedIL: @"
{
  // Code size       17 (0x11)
  .maxstack  2
  // sequence point: N s1 = b[0];
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.0
  IL_0002:  callvirt   ""N Base.this[int].get""
  IL_0007:  pop
  // sequence point: Q s2 = d[0];
  IL_0008:  ldarg.2
  IL_0009:  ldc.i4.0
  IL_000a:  callvirt   ""Q Derived<Q>.this[int].get""
  IL_000f:  pop
  // sequence point: }
  IL_0010:  ret
}
");

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.this[]", "T Derived<T>.this[System.Int32 i] { get; }", "N Base.this[System.Int32 i] { get; }");
                VerifyOverride(comp, "Derived.get_Item", "T Derived<T>.this[System.Int32 i].get", "N Base.this[System.Int32 i].get");
            }
        }

        [Fact]
        public void CovariantReturns_Events()
        {
            var source = @"
using System;
public class Base
{
    public virtual event Func<object> E;
    private void SuppressUnusedWarning() => E?.Invoke();
}
public class Derived : Base
{
    public override event Func<string> E;
    private void SuppressUnusedWarning() => E?.Invoke();
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (10,40): error CS1715: 'Derived.E': type must be 'Func<object>' to match overridden member 'Base.E'
                //     public override event Func<string> E;
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "E").WithArguments("Derived.E", "Base.E", "System.Func<object>").WithLocation(10, 40)
                );
            verify(comp);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                // (10,40): error CS1715: 'Derived.E': type must be 'Func<object>' to match overridden member 'Base.E'
                //     public override event Func<string> E;
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "E").WithArguments("Derived.E", "Base.E", "System.Func<object>").WithLocation(10, 40)
                );
            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.E", "event System.Func<System.String> Derived.E", "event System.Func<System.Object> Base.E");
            }
        }

        [Fact]
        public void CovariantReturns_WritableProperties()
        {
            var source = @"
using System;
public class Base
{
    public virtual Func<object> P { get; set; }
}
public class Derived : Base
{
    public override Func<string> P { get; set; }
}
public class Program
{
    void M(Base b, Derived d)
    {
        Func<object> s1 = b.P;
        Func<string> s2 = d.P;
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (9,34): error CS1715: 'Derived.P': type must be 'Func<object>' to match overridden member 'Base.P'
                //     public override Func<string> P { get; set; }
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "P").WithArguments("Derived.P", "Base.P", "System.Func<object>").WithLocation(9, 34)
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                // (9,34): error CS1715: 'Derived.P': type must be 'Func<object>' to match overridden member 'Base.P'
                //     public override Func<string> P { get; set; }
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "P").WithArguments("Derived.P", "Base.P", "System.Func<object>").WithLocation(9, 34)
                );
            verify(comp);
            VerifyAssignments(comp, 2);

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.P", "System.Func<System.String> Derived.P { get; set; }", "System.Func<System.Object> Base.P { get; set; }");
                VerifyNoOverride(comp, "Derived.set_P");
                VerifyOverride(comp, "Derived.get_P", "System.Func<System.String> Derived.P.get", "System.Func<System.Object> Base.P.get");
            }
        }

        [Fact]
        public void CovariantReturns_MetadataVsSource_01()
        {
            var s0 = @"
public class Base
{
    public virtual object M() => null;
}
";
            var baseMetadata = CreateCompilation(s0).EmitToImageReference();
            var source = @"
public class Derived : Base
{
    public override string M() => null;
}
public class Program
{
    void M(Base b, Derived d)
    {
        object s1 = b.M();
        string s2 = d.M();
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source, references: new[] { baseMetadata }).VerifyDiagnostics(
                // (4,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string M() => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("covariant returns").WithLocation(4, 28)
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            comp = CreateCompilationWithCovariantReturns(source, references: new[] { baseMetadata }).VerifyDiagnostics(
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            verify(CompilationReferenceView(comp, baseMetadata));
            verify(MetadataView(comp, baseMetadata));
            verify(RetargetedView(comp, baseMetadata));
            CompileAndVerify(comp, verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived)", source: source, sequencePoints: "Program.M", expectedIL: @"
{
  // Code size       15 (0xf)
  .maxstack  1
  // sequence point: object s1 = b.M();
  IL_0000:  ldarg.1
  IL_0001:  callvirt   ""object Base.M()""
  IL_0006:  pop
  // sequence point: string s2 = d.M();
  IL_0007:  ldarg.2
  IL_0008:  callvirt   ""string Derived.M()""
  IL_000d:  pop
  // sequence point: }
  IL_000e:  ret
}
");

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M", "System.String Derived.M()", "System.Object Base.M()");
            }
        }

        [Fact]
        public void CovariantReturns_MetadataVsSource_02()
        {
            var s0 = @"
public class Base
{
    public virtual object M => null;
}
";
            var baseMetadata = CreateCompilation(s0).EmitToImageReference();
            var source = @"
public class Derived : Base
{
    public override string M => null;
}
public class Program
{
    void M(Base b, Derived d)
    {
        object s1 = b.M;
        string s2 = d.M;
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source, references: new[] { baseMetadata }).VerifyDiagnostics(
                // (4,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string M => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("covariant returns").WithLocation(4, 28)
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            comp = CreateCompilationWithCovariantReturns(source, references: new[] { baseMetadata }).VerifyDiagnostics(
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            verify(CompilationReferenceView(comp, baseMetadata));
            verify(MetadataView(comp, baseMetadata));
            verify(RetargetedView(comp, baseMetadata));
            CompileAndVerify(comp, verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived)", source: source, sequencePoints: "Program.M", expectedIL: @"
{
  // Code size       15 (0xf)
  .maxstack  1
  // sequence point: object s1 = b.M;
  IL_0000:  ldarg.1
  IL_0001:  callvirt   ""object Base.M.get""
  IL_0006:  pop
  // sequence point: string s2 = d.M;
  IL_0007:  ldarg.2
  IL_0008:  callvirt   ""string Derived.M.get""
  IL_000d:  pop
  // sequence point: }
  IL_000e:  ret
}
");

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M", "System.String Derived.M { get; }", "System.Object Base.M { get; }");
                VerifyOverride(comp, "Derived.get_M", "System.String Derived.M.get", "System.Object Base.M.get");
            }
        }

        [Fact]
        public void CovariantReturns_MetadataVsSource_03()
        {
            var s0 = @"
public class Base
{
    public virtual object this[int i] => null;
}
";
            var baseMetadata = CreateCompilation(s0).EmitToImageReference();
            var source = @"
public class Derived : Base
{
    public override string this[int i] => null;
}
public class Program
{
    void M(Base b, Derived d)
    {
        object s1 = b[0];
        string s2 = d[0];
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source, references: new[] { baseMetadata }).VerifyDiagnostics(
                // (4,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string this[int i] => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "this").WithArguments("covariant returns").WithLocation(4, 28)
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            comp = CreateCompilationWithCovariantReturns(source, references: new[] { baseMetadata }).VerifyDiagnostics(
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            verify(CompilationReferenceView(comp, baseMetadata));
            verify(MetadataView(comp, baseMetadata));
            verify(RetargetedView(comp, baseMetadata));
            CompileAndVerify(comp, verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived)", source: source, sequencePoints: "Program.M", expectedIL: @"
{
  // Code size       17 (0x11)
  .maxstack  2
  // sequence point: object s1 = b[0];
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.0
  IL_0002:  callvirt   ""object Base.this[int].get""
  IL_0007:  pop
  // sequence point: string s2 = d[0];
  IL_0008:  ldarg.2
  IL_0009:  ldc.i4.0
  IL_000a:  callvirt   ""string Derived.this[int].get""
  IL_000f:  pop
  // sequence point: }
  IL_0010:  ret
}
");

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.this[]", "System.String Derived.this[System.Int32 i] { get; }", "System.Object Base.this[System.Int32 i] { get; }");
                VerifyOverride(comp, "Derived.get_Item", "System.String Derived.this[System.Int32 i].get", "System.Object Base.this[System.Int32 i].get");
            }
        }

        [Fact]
        public void CovariantReturns_11()
        {
            var source = @"
public abstract class Base
{
    public abstract object M();
}
public class Derived : Base
{
    public override string M() => null;
}
public class Program
{
    void M(Base b, Derived d)
    {
        object s1 = b.M();
        string s2 = d.M();
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (8,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string M() => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("covariant returns").WithLocation(8, 28)
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            verify(CompilationReferenceView(comp));
            verify(MetadataView(comp));
            verify(RetargetedView(comp));
            CompileAndVerify(comp, verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived)", source: source, sequencePoints: "Program.M", expectedIL: @"
{
  // Code size       15 (0xf)
  .maxstack  1
  // sequence point: object s1 = b.M();
  IL_0000:  ldarg.1
  IL_0001:  callvirt   ""object Base.M()""
  IL_0006:  pop
  // sequence point: string s2 = d.M();
  IL_0007:  ldarg.2
  IL_0008:  callvirt   ""string Derived.M()""
  IL_000d:  pop
  // sequence point: }
  IL_000e:  ret
}
");

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M", "System.String Derived.M()", "System.Object Base.M()");
            }
        }

        [Fact]
        public void CovariantReturns_WrongReturnType()
        {
            var source = @"
public class Base
{
    public virtual string M() => null;
}
public class Derived : Base
{
    public override object M() => null;
}
public class Program
{
    void M(Base b, Derived d)
    {
        string s1 = b.M();
        object s2 = d.M();
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (8,28): error CS0508: 'Derived.M()': return type must be 'string' to match overridden member 'Base.M()'
                //     public override object M() => null;
                Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "M").WithArguments("Derived.M()", "Base.M()", "string").WithLocation(8, 28)
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                // (8,28): error CS0508: 'Derived.M()': return type must be 'string' to match overridden member 'Base.M()'
                //     public override object M() => null;
                Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "M").WithArguments("Derived.M()", "Base.M()", "string").WithLocation(8, 28)
                );
            verify(comp);
            VerifyAssignments(comp, 2);

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M", "System.Object Derived.M()", "System.String Base.M()");
            }
        }

        [Fact]
        public void NonOverrideTests_01()
        {
            var source = @"
public class Base
{
    public virtual object M1 => null;
    public virtual object M2 => null;
}
public class Derived : Base
{
    public new string M1 => null;
    public string M2 => null;    // A
}
public class Derived2 : Derived
{
    public new string M1 => null;
    public string M2 => null;    // B
}
public class Derived3 : Derived
{
    public new object M1 => null;
    public object M2 => null;
}
public class Program
{
    void M(Base b, Derived d1, Derived2 d2, Derived3 d3)
    {
        object x1 = b.M1;
        object x2 = b.M2;
        string x3 = d1.M1;
        string x4 = d1.M2;
        string x5 = d2.M1;
        string x6 = d2.M2;
        object x7 = d3.M1;
        object x8 = d3.M2;
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (10,19): warning CS0114: 'Derived.M2' hides inherited member 'Base.M2'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public string M2 => null;    // A
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "M2").WithArguments("Derived.M2", "Base.M2").WithLocation(10, 19),
                // (15,19): warning CS0108: 'Derived2.M2' hides inherited member 'Derived.M2'. Use the new keyword if hiding was intended.
                //     public string M2 => null;    // B
                Diagnostic(ErrorCode.WRN_NewRequired, "M2").WithArguments("Derived2.M2", "Derived.M2").WithLocation(15, 19),
                // (20,19): warning CS0108: 'Derived3.M2' hides inherited member 'Derived.M2'. Use the new keyword if hiding was intended.
                //     public object M2 => null;
                Diagnostic(ErrorCode.WRN_NewRequired, "M2").WithArguments("Derived3.M2", "Derived.M2").WithLocation(20, 19)
                );
            verify(comp);
            VerifyAssignments(comp, 8);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                // (10,19): warning CS0114: 'Derived.M2' hides inherited member 'Base.M2'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public string M2 => null;    // A
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "M2").WithArguments("Derived.M2", "Base.M2").WithLocation(10, 19),
                // (15,19): warning CS0108: 'Derived2.M2' hides inherited member 'Derived.M2'. Use the new keyword if hiding was intended.
                //     public string M2 => null;    // B
                Diagnostic(ErrorCode.WRN_NewRequired, "M2").WithArguments("Derived2.M2", "Derived.M2").WithLocation(15, 19),
                // (20,19): warning CS0108: 'Derived3.M2' hides inherited member 'Derived.M2'. Use the new keyword if hiding was intended.
                //     public object M2 => null;
                Diagnostic(ErrorCode.WRN_NewRequired, "M2").WithArguments("Derived3.M2", "Derived.M2").WithLocation(20, 19)
                );
            verify(comp);
            VerifyAssignments(comp, 8);

            static void verify(CSharpCompilation comp)
            {
                VerifyNoOverride(comp, "Derived.M1");
                VerifyNoOverride(comp, "Derived.M2");
                VerifyNoOverride(comp, "Derived2.M1");
                VerifyNoOverride(comp, "Derived2.M2");
                VerifyNoOverride(comp, "Derived3.M1");
                VerifyNoOverride(comp, "Derived3.M2");
            }
        }

        [Fact]
        public void ChainedOverrides_01()
        {
            var source = @"
public class Base
{
    public virtual object M1 => null;
    public virtual object M2 => null;
    public virtual object M3 => null;
}
public class Derived : Base
{
    public override string M1 => null; // A
    public override string M2 => null; // B
    public override string M3 => null; // C
}
public class Derived2 : Derived
{
    public override string M1 => null;
    public override object M2 => null; // 1
    public override Base M3 => null;   // 2
}
public class Program
{
    void M(Base b, Derived d1, Derived2 d2)
    {
        object x1 = b.M1;
        object x2 = b.M2;
        object x3 = b.M3;
        string x4 = d1.M1;
        string x5 = d1.M2;
        string x6 = d1.M3;
        string x7 = d2.M1;
        object x8 = d2.M2;
        Base x9 = d2.M3;
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (10,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string M1 => null; // A
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M1").WithArguments("covariant returns").WithLocation(10, 28),
                // (11,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string M2 => null; // B
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M2").WithArguments("covariant returns").WithLocation(11, 28),
                // (12,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string M3 => null; // C
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M3").WithArguments("covariant returns").WithLocation(12, 28),
                // (17,28): error CS1715: 'Derived2.M2': type must be 'string' to match overridden member 'Derived.M2'
                //     public override object M2 => null; // 1
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "M2").WithArguments("Derived2.M2", "Derived.M2", "string").WithLocation(17, 28),
                // (18,26): error CS1715: 'Derived2.M3': type must be 'string' to match overridden member 'Derived.M3'
                //     public override Base M3 => null;   // 2
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "M3").WithArguments("Derived2.M3", "Derived.M3", "string").WithLocation(18, 26)
                );
            verify(comp);
            VerifyAssignments(comp, 9);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                // (17,28): error CS1715: 'Derived2.M2': type must be 'string' to match overridden member 'Derived.M2'
                //     public override object M2 => null; // 1
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "M2").WithArguments("Derived2.M2", "Derived.M2", "string").WithLocation(17, 28),
                // (18,26): error CS1715: 'Derived2.M3': type must be 'string' to match overridden member 'Derived.M3'
                //     public override Base M3 => null;   // 2
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "M3").WithArguments("Derived2.M3", "Derived.M3", "string").WithLocation(18, 26)
                );
            verify(comp);
            VerifyAssignments(comp, 9);

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M1", "System.String Derived.M1 { get; }", "System.Object Base.M1 { get; }");
                VerifyOverride(comp, "Derived.get_M1", "System.String Derived.M1.get", "System.Object Base.M1.get");
                VerifyOverride(comp, "Derived.M2", "System.String Derived.M2 { get; }", "System.Object Base.M2 { get; }");
                VerifyOverride(comp, "Derived.get_M2", "System.String Derived.M2.get", "System.Object Base.M2.get");
                VerifyOverride(comp, "Derived.M3", "System.String Derived.M3 { get; }", "System.Object Base.M3 { get; }");
                VerifyOverride(comp, "Derived.get_M3", "System.String Derived.M3.get", "System.Object Base.M3.get");

                VerifyOverride(comp, "Derived2.M1", "System.String Derived2.M1 { get; }", "System.String Derived.M1 { get; }");
                VerifyOverride(comp, "Derived2.get_M1", "System.String Derived2.M1.get", "System.String Derived.M1.get");
                VerifyOverride(comp, "Derived2.M2", "System.Object Derived2.M2 { get; }", "System.String Derived.M2 { get; }");
                VerifyOverride(comp, "Derived2.get_M2", "System.Object Derived2.M2.get", "System.String Derived.M2.get");
                VerifyOverride(comp, "Derived2.M3", "Base Derived2.M3 { get; }", "System.String Derived.M3 { get; }");
                VerifyOverride(comp, "Derived2.get_M3", "Base Derived2.M3.get", "System.String Derived.M3.get");
            }
        }

        [Fact]
        public void NestedVariance_01()
        {
            var source = @"
public class Base
{
    public virtual IIn<string> M1 => null;
    public virtual IOut<object> M2 => null;
}
public class Derived : Base
{
    public override IIn<object> M1 => null;
    public override IOut<string> M2 => null;
}
public interface IOut<out T> { }
public interface IIn<in T> { }
public class Program
{
    void M(Base b, Derived d)
    {
        IIn<string> x1 = b.M1;
        IOut<object> x2 = b.M2;
        IIn<object> x3 = d.M1;
        IOut<string> x4 = d.M2;
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (9,33): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override IIn<object> M1 => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M1").WithArguments("covariant returns").WithLocation(9, 33),
                // (10,34): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override IOut<string> M2 => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M2").WithArguments("covariant returns").WithLocation(10, 34)
                );
            verify(comp);
            VerifyAssignments(comp, 4);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(comp);
            VerifyAssignments(comp, 4);
            verify(CompilationReferenceView(comp));
            verify(MetadataView(comp));
            verify(RetargetedView(comp));
            CompileAndVerify(comp, verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived)", source: source, sequencePoints: "Program.M", expectedIL: @"
{
  // Code size       29 (0x1d)
  .maxstack  1
  // sequence point: IIn<string> x1 = b.M1;
  IL_0000:  ldarg.1
  IL_0001:  callvirt   ""IIn<string> Base.M1.get""
  IL_0006:  pop
  // sequence point: IOut<object> x2 = b.M2;
  IL_0007:  ldarg.1
  IL_0008:  callvirt   ""IOut<object> Base.M2.get""
  IL_000d:  pop
  // sequence point: IIn<object> x3 = d.M1;
  IL_000e:  ldarg.2
  IL_000f:  callvirt   ""IIn<object> Derived.M1.get""
  IL_0014:  pop
  // sequence point: IOut<string> x4 = d.M2;
  IL_0015:  ldarg.2
  IL_0016:  callvirt   ""IOut<string> Derived.M2.get""
  IL_001b:  pop
  // sequence point: }
  IL_001c:  ret
}
");

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M1", "IIn<System.Object> Derived.M1 { get; }", "IIn<System.String> Base.M1 { get; }");
                VerifyOverride(comp, "Derived.get_M1", "IIn<System.Object> Derived.M1.get", "IIn<System.String> Base.M1.get");
                VerifyOverride(comp, "Derived.M2", "IOut<System.String> Derived.M2 { get; }", "IOut<System.Object> Base.M2 { get; }");
                VerifyOverride(comp, "Derived.get_M2", "IOut<System.String> Derived.M2.get", "IOut<System.Object> Base.M2.get");
            }
        }

        [Fact]
        public void NestedVariance_02()
        {
            var source = @"
public class Base
{
    public virtual IIn<object> M1 => null;
    public virtual IOut<string> M2 => null;
}
public class Derived : Base
{
    public override IIn<string> M1 => null;
    public override IOut<object> M2 => null;
}
public interface IOut<out T> { }
public interface IIn<in T> { }
public class Program
{
    void M(Base b, Derived d)
    {
        IIn<object> x1 = b.M1;
        IOut<string> x2 = b.M2;
        IIn<string> x3 = d.M1;
        IOut<object> x4 = d.M2;
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (9,33): error CS1715: 'Derived.M1': type must be 'IIn<object>' to match overridden member 'Base.M1'
                //     public override IIn<string> M1 => null;
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "M1").WithArguments("Derived.M1", "Base.M1", "IIn<object>").WithLocation(9, 33),
                // (10,34): error CS1715: 'Derived.M2': type must be 'IOut<string>' to match overridden member 'Base.M2'
                //     public override IOut<object> M2 => null;
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "M2").WithArguments("Derived.M2", "Base.M2", "IOut<string>").WithLocation(10, 34)
                );
            verify(comp);
            VerifyAssignments(comp, 4);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                // (9,33): error CS1715: 'Derived.M1': type must be 'IIn<object>' to match overridden member 'Base.M1'
                //     public override IIn<string> M1 => null;
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "M1").WithArguments("Derived.M1", "Base.M1", "IIn<object>").WithLocation(9, 33),
                // (10,34): error CS1715: 'Derived.M2': type must be 'IOut<string>' to match overridden member 'Base.M2'
                //     public override IOut<object> M2 => null;
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "M2").WithArguments("Derived.M2", "Base.M2", "IOut<string>").WithLocation(10, 34)
                );
            verify(comp);
            VerifyAssignments(comp, 4);

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M1", "IIn<System.String> Derived.M1 { get; }", "IIn<System.Object> Base.M1 { get; }");
                VerifyOverride(comp, "Derived.get_M1", "IIn<System.String> Derived.M1.get", "IIn<System.Object> Base.M1.get");
                VerifyOverride(comp, "Derived.M2", "IOut<System.Object> Derived.M2 { get; }", "IOut<System.String> Base.M2 { get; }");
                VerifyOverride(comp, "Derived.get_M2", "IOut<System.Object> Derived.M2.get", "IOut<System.String> Base.M2.get");
            }
        }

        [Fact]
        public void BadCovariantReturnType_01()
        {
            var source = @"
public class Base
{
    public virtual int M1 => 1;
    public virtual A M2 => null;
}
public class Derived : Base
{
    public override short M1 => 1;
    public override B M2 => null;
}
public class A { }
public class B
{
    public static implicit operator A(B b) => null;
}
public class Program
{
    void M(Base b, Derived d)
    {
        int x1 = b.M1;
        A x2 = b.M2;
        short x3 = d.M1;
        B x4 = d.M2;
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (9,27): error CS1715: 'Derived.M1': type must be 'int' to match overridden member 'Base.M1'
                //     public override short M1 => 1;
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "M1").WithArguments("Derived.M1", "Base.M1", "int").WithLocation(9, 27),
                // (10,23): error CS1715: 'Derived.M2': type must be 'A' to match overridden member 'Base.M2'
                //     public override B M2 => null;
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "M2").WithArguments("Derived.M2", "Base.M2", "A").WithLocation(10, 23)
                );
            verify(comp);
            VerifyAssignments(comp, 4);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                // (9,27): error CS1715: 'Derived.M1': type must be 'int' to match overridden member 'Base.M1'
                //     public override short M1 => 1;
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "M1").WithArguments("Derived.M1", "Base.M1", "int").WithLocation(9, 27),
                // (10,23): error CS1715: 'Derived.M2': type must be 'A' to match overridden member 'Base.M2'
                //     public override B M2 => null;
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "M2").WithArguments("Derived.M2", "Base.M2", "A").WithLocation(10, 23)
                );
            verify(comp);
            VerifyAssignments(comp, 4);

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M1", "System.Int16 Derived.M1 { get; }", "System.Int32 Base.M1 { get; }");
                VerifyOverride(comp, "Derived.get_M1", "System.Int16 Derived.M1.get", "System.Int32 Base.M1.get");
                VerifyOverride(comp, "Derived.M2", "B Derived.M2 { get; }", "A Base.M2 { get; }");
                VerifyOverride(comp, "Derived.get_M2", "B Derived.M2.get", "A Base.M2.get");
            }
        }

        [Fact]
        public void CovariantReturns_12()
        {
            var source = @"
public class Base
{
    public virtual System.IComparable M => null;
}
public class Derived : Base
{
    public override string M => null;
}
public class Program
{
    void M(Base b, Derived d)
    {
        System.IComparable x1 = b.M;
        string x2 = d.M;
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (8,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string M => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("covariant returns").WithLocation(8, 28)
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            verify(CompilationReferenceView(comp));
            verify(MetadataView(comp));
            verify(RetargetedView(comp));
            CompileAndVerify(comp, verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived)", source: source, sequencePoints: "Program.M", expectedIL: @"
{
  // Code size       15 (0xf)
  .maxstack  1
  // sequence point: System.IComparable x1 = b.M;
  IL_0000:  ldarg.1
  IL_0001:  callvirt   ""System.IComparable Base.M.get""
  IL_0006:  pop
  // sequence point: string x2 = d.M;
  IL_0007:  ldarg.2
  IL_0008:  callvirt   ""string Derived.M.get""
  IL_000d:  pop
  // sequence point: }
  IL_000e:  ret
}
");

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M", "System.String Derived.M { get; }", "System.IComparable Base.M { get; }");
                VerifyOverride(comp, "Derived.get_M", "System.String Derived.M.get", "System.IComparable Base.M.get");
            }
        }

        [Fact]
        public void NoCovariantImplementations_01()
        {
            var source = @"
public interface Base
{
    public virtual object M1 => null;
    public virtual object M2() => null;
}
public interface Derived : Base
{
    string Base.M1 => null;   // 1
    string Base.M2() => null; // 2
}
public class C : Base
{
    string Base.M1 => null;   // 3
    string Base.M2() => null; // 4
}
public class Program
{
    void M(Base b, Derived d, C c)
    {
        object x1 = b.M1;
        object x2 = b.M2();
        object x3 = d.M1;
        object x4 = d.M2();
    }
}
";
            // these are poor diagnostics; see https://github.com/dotnet/roslyn/issues/43719
            var comp = CreateCompilationWithoutCovariantReturns(source, targetFramework: TargetFramework.NetStandardLatest).VerifyDiagnostics(
                // (9,17): error CS0539: 'Derived.M1' in explicit interface declaration is not found among members of the interface that can be implemented
                //     string Base.M1 => null;   // 1
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "M1").WithArguments("Derived.M1").WithLocation(9, 17),
                // (10,17): error CS0539: 'Derived.M2()' in explicit interface declaration is not found among members of the interface that can be implemented
                //     string Base.M2() => null; // 2
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "M2").WithArguments("Derived.M2()").WithLocation(10, 17),
                // (14,17): error CS0539: 'C.M1' in explicit interface declaration is not found among members of the interface that can be implemented
                //     string Base.M1 => null;   // 3
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "M1").WithArguments("C.M1").WithLocation(14, 17),
                // (15,17): error CS0539: 'C.M2()' in explicit interface declaration is not found among members of the interface that can be implemented
                //     string Base.M2() => null; // 4
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "M2").WithArguments("C.M2()").WithLocation(15, 17)
                );
            verify(comp);
            VerifyAssignments(comp, 4);
            comp = CreateCompilationWithCovariantReturns(source, targetFramework: TargetFramework.NetStandardLatest).VerifyDiagnostics(
                // (9,17): error CS0539: 'Derived.M1' in explicit interface declaration is not found among members of the interface that can be implemented
                //     string Base.M1 => null;   // 1
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "M1").WithArguments("Derived.M1").WithLocation(9, 17),
                // (10,17): error CS0539: 'Derived.M2()' in explicit interface declaration is not found among members of the interface that can be implemented
                //     string Base.M2() => null; // 2
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "M2").WithArguments("Derived.M2()").WithLocation(10, 17),
                // (14,17): error CS0539: 'C.M1' in explicit interface declaration is not found among members of the interface that can be implemented
                //     string Base.M1 => null;   // 3
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "M1").WithArguments("C.M1").WithLocation(14, 17),
                // (15,17): error CS0539: 'C.M2()' in explicit interface declaration is not found among members of the interface that can be implemented
                //     string Base.M2() => null; // 4
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "M2").WithArguments("C.M2()").WithLocation(15, 17)
                );
            verify(comp);
            VerifyAssignments(comp, 4);

            static void verify(CSharpCompilation comp)
            {
                VerifyNoOverride(comp, "Derived.Base.M1");
                VerifyNoOverride(comp, "Derived.Base.M2");
                VerifyNoOverride(comp, "C.Base.M1");
                VerifyNoOverride(comp, "C.Base.M2");
            }
        }

        [Fact]
        public void CovariantReturns_13()
        {
            var source = @"
public class Base
{
    public virtual object P { get; set; }
}
public class Derived : Base
{
    public override string P { get => string.Empty; }
}
public class Program
{
    void M(Base b, Derived d)
    {
        object x1 = b.P;
        string x2 = d.P;
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (8,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string P { get; }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "P").WithArguments("covariant returns").WithLocation(8, 28)
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            verify(CompilationReferenceView(comp));
            verify(MetadataView(comp));
            verify(RetargetedView(comp));
            CompileAndVerify(comp, verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived)", source: source, sequencePoints: "Program.M", expectedIL: @"
{
  // Code size       15 (0xf)
  .maxstack  1
  // sequence point: object x1 = b.P;
  IL_0000:  ldarg.1
  IL_0001:  callvirt   ""object Base.P.get""
  IL_0006:  pop
  // sequence point: string x2 = d.P;
  IL_0007:  ldarg.2
  IL_0008:  callvirt   ""string Derived.P.get""
  IL_000d:  pop
  // sequence point: }
  IL_000e:  ret
}
");

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.P", "System.String Derived.P { get; }", "System.Object Base.P { get; set; }");
                VerifyOverride(comp, "Derived.get_P", "System.String Derived.P.get", "System.Object Base.P.get");
            }
        }

        [Fact]
        public void CovariantReturns_14()
        {
            var source = @"
public class Base
{
    public virtual object P { get; set; }
}
public class Derived : Base
{
    public override string P { get => string.Empty; }
}
public class Derived2 : Derived
{
    public override string P { get => string.Empty; set { } }
}
public class Program
{
    void M(Base b, Derived d1, Derived2 d2)
    {
        object x1 = b.P;
        string x2 = d1.P;
        string x3 = d2.P;
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (8,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string P { get => string.Empty; }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "P").WithArguments("covariant returns").WithLocation(8, 28),
                // (12,53): error CS0546: 'Derived2.P.set': cannot override because 'Derived.P' does not have an overridable set accessor
                //     public override string P { get => string.Empty; set { } }
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "set").WithArguments("Derived2.P.set", "Derived.P").WithLocation(12, 53)
                );
            verify(comp);
            VerifyAssignments(comp, 3);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                // (12,53): error CS0546: 'Derived2.P.set': cannot override because 'Derived.P' does not have an overridable set accessor
                //     public override string P { get => string.Empty; set { } }
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "set").WithArguments("Derived2.P.set", "Derived.P").WithLocation(12, 53)
                );
            verify(comp);
            VerifyAssignments(comp, 3);

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.P", "System.String Derived.P { get; }", "System.Object Base.P { get; set; }");
                VerifyOverride(comp, "Derived.get_P", "System.String Derived.P.get", "System.Object Base.P.get");
                VerifyOverride(comp, "Derived2.P", "System.String Derived2.P { get; set; }", "System.String Derived.P { get; }");
                VerifyOverride(comp, "Derived2.get_P", "System.String Derived2.P.get", "System.String Derived.P.get");
                VerifyNoOverride(comp, "Derived2.set_P");
            }
        }

        [Fact]
        public void CovariantReturns_15()
        {
            var source = @"
public class Base
{
    public virtual object P { get; set; }
}
public class Derived : Base
{
    public override string P { get => string.Empty; }
}
public class Derived2 : Derived
{
    public override string P { set { } }
}
public class Program
{
    void M(Base b, Derived d1, Derived2 d2)
    {
        object x1 = b.P;
        string x2 = d1.P;
        string x3 = d2.P;
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (8,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string P { get => string.Empty; }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "P").WithArguments("covariant returns").WithLocation(8, 28),
                // (12,32): error CS0546: 'Derived2.P.set': cannot override because 'Derived.P' does not have an overridable set accessor
                //     public override string P { set { } }
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "set").WithArguments("Derived2.P.set", "Derived.P").WithLocation(12, 32)
                );
            verify(comp);
            VerifyAssignments(comp, 3);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                // (12,32): error CS0546: 'Derived2.P.set': cannot override because 'Derived.P' does not have an overridable set accessor
                //     public override string P { set { } }
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "set").WithArguments("Derived2.P.set", "Derived.P").WithLocation(12, 32)
                );
            verify(comp);
            VerifyAssignments(comp, 3);

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.P", "System.String Derived.P { get; }", "System.Object Base.P { get; set; }");
                VerifyOverride(comp, "Derived.get_P", "System.String Derived.P.get", "System.Object Base.P.get");
                VerifyOverride(comp, "Derived2.P", "System.String Derived2.P { set; }", "System.String Derived.P { get; }");
                VerifyNoOverride(comp, "Derived2.set_P");
            }
        }

        [Fact]
        public void CovariantReturns_16()
        {
            var source = @"
public class Base
{
    public virtual object P { get; set; }
}
public class Derived : Base
{
    public override System.IComparable P { get => string.Empty; }
}
public class Derived2 : Derived
{
    public override string P { get => string.Empty; }
}
public class Program
{
    void M(Base b, Derived d1, Derived2 d2)
    {
        object x1 = b.P;
        System.IComparable x2 = d1.P;
        string x3 = d2.P;
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (8,40): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override System.IComparable P { get => string.Empty; }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "P").WithArguments("covariant returns").WithLocation(8, 40),
                // (12,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string P { get => string.Empty; }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "P").WithArguments("covariant returns").WithLocation(12, 28)
                );
            verify(comp);
            VerifyAssignments(comp, 3);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(comp);
            VerifyAssignments(comp, 3);
            verify(CompilationReferenceView(comp));
            verify(MetadataView(comp));
            verify(RetargetedView(comp));
            CompileAndVerify(comp, verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived, Derived2)", source: source, sequencePoints: "Program.M", expectedIL: @"
{
  // Code size       22 (0x16)
  .maxstack  1
  // sequence point: object x1 = b.P;
  IL_0000:  ldarg.1
  IL_0001:  callvirt   ""object Base.P.get""
  IL_0006:  pop
  // sequence point: System.IComparable x2 = d1.P;
  IL_0007:  ldarg.2
  IL_0008:  callvirt   ""System.IComparable Derived.P.get""
  IL_000d:  pop
  // sequence point: string x3 = d2.P;
  IL_000e:  ldarg.3
  IL_000f:  callvirt   ""string Derived2.P.get""
  IL_0014:  pop
  // sequence point: }
  IL_0015:  ret
}
");

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.P", "System.IComparable Derived.P { get; }", "System.Object Base.P { get; set; }");
                VerifyOverride(comp, "Derived.get_P", "System.IComparable Derived.P.get", "System.Object Base.P.get");
                VerifyOverride(comp, "Derived2.P", "System.String Derived2.P { get; }", "System.IComparable Derived.P { get; }");
                VerifyOverride(comp, "Derived2.get_P", "System.String Derived2.P.get", "System.IComparable Derived.P.get");
            }
        }

        [Fact]
        public void CovariantReturns_17()
        {
            var source = @"
public class Base<T>
{
    public virtual object M(string s) => null;
    public virtual System.IComparable M(T s) => null;
}
public class Derived : Base<string>
{
    public override string M(string s) => null;
}
public class Program
{
    void M(Base<string> b, Derived d, string s)
    {
        object x1 = b.M(s);
        string x2 = d.M(s);
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (9,28): error CS0462: The inherited members 'Base<T>.M(string)' and 'Base<T>.M(T)' have the same signature in type 'Derived', so they cannot be overridden
                //     public override string M(string s) => null;
                Diagnostic(ErrorCode.ERR_AmbigOverride, "M").WithArguments("Base<T>.M(string)", "Base<T>.M(T)", "Derived").WithLocation(9, 28)
                );
            verify(comp);
            VerifyAssignments(comp, 2);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                // (9,28): error CS0462: The inherited members 'Base<T>.M(string)' and 'Base<T>.M(T)' have the same signature in type 'Derived', so they cannot be overridden
                //     public override string M(string s) => null;
                Diagnostic(ErrorCode.ERR_AmbigOverride, "M").WithArguments("Base<T>.M(string)", "Base<T>.M(T)", "Derived").WithLocation(9, 28)
                );
            verify(comp);
            VerifyAssignments(comp, 2);

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M", "System.String Derived.M(System.String s)", "System.Object Base<System.String>.M(System.String s)");
            }
        }

        [Fact]
        public void CovariantReturns_18()
        {
            var source = @"
public class Base
{
    public virtual object M() => null;
}
public abstract class Derived : Base
{
    public abstract override System.IComparable M();
}
public class Derived2 : Derived
{
    public override string M() => null;
}
public class Program
{
    void M(Base b, Derived d1, Derived2 d2)
    {
        object x1 = b.M();
        System.IComparable x2 = d1.M();
        string x3 = d2.M();
    }
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (8,49): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public abstract override System.IComparable M();
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("covariant returns").WithLocation(8, 49),
                // (12,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string M() => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("covariant returns").WithLocation(12, 28)
                );
            verify(comp);
            VerifyAssignments(comp, 3);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(comp);
            VerifyAssignments(comp, 3);
            verify(CompilationReferenceView(comp));
            verify(MetadataView(comp));
            verify(RetargetedView(comp));
            CompileAndVerify(comp, verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived, Derived2)", source: source, sequencePoints: "Program.M", expectedIL: @"
{
  // Code size       22 (0x16)
  .maxstack  1
  // sequence point: object x1 = b.M();
  IL_0000:  ldarg.1
  IL_0001:  callvirt   ""object Base.M()""
  IL_0006:  pop
  // sequence point: System.IComparable x2 = d1.M();
  IL_0007:  ldarg.2
  IL_0008:  callvirt   ""System.IComparable Derived.M()""
  IL_000d:  pop
  // sequence point: string x3 = d2.M();
  IL_000e:  ldarg.3
  IL_000f:  callvirt   ""string Derived2.M()""
  IL_0014:  pop
  // sequence point: }
  IL_0015:  ret
}
");

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M", "System.IComparable Derived.M()", "System.Object Base.M()");
                VerifyOverride(comp, "Derived2.M", "System.String Derived2.M()", "System.IComparable Derived.M()");
            }
        }

        [Fact]
        public void TestVBConsumption_01()
        {
            var source0 = @"
public class Base
{
    public virtual object M() => null;
    public virtual object P => null;
    public virtual object this[int i] => null;
}
public abstract class Derived : Base
{
    public override string M() => null;
    public override string P => null;
    public override string this[int i] => null;
}
";
            var csComp = CreateCompilationWithCovariantReturns(source0).VerifyDiagnostics(
                );
            csComp.VerifyDiagnostics();
            var csRef = csComp.EmitToImageReference();

            var vbSource = @"
Public Class Derived2 : Inherits Derived
    Public Overrides Function M() As String
        Return Nothing
    End Function
    Public Overrides ReadOnly Property P As String
        Get
            Return Nothing
        End Get
    End Property
    Public Overrides Default ReadOnly Property Item(i As Integer) As String
        Get
            Return Nothing
        End Get
    End Property
    
    Public Sub T(b as Base, d as Derived, d2 as Derived2)
        Dim x1 As Object = b.M()
        Dim x2 As Object = b.P
        Dim x3 As Object = b(0)
        Dim x4 As String = d.M()
        Dim x5 As String = d.P
        Dim x6 As String = d(0)
        Dim x7 As String = d2.M()
        Dim x8 As String = d2.P
        Dim x9 As String = d2(0)
    End Sub
End Class
";
            var vbComp = CreateVisualBasicCompilation(code: vbSource, referencedAssemblies: csComp.References.Append(csRef));
            vbComp.VerifyDiagnostics();
            var vbTree = vbComp.SyntaxTrees[0];
            var model = vbComp.GetSemanticModel(vbTree);
            int count = 0;
            foreach (var localDeclaration in vbTree.GetRoot().DescendantNodes().OfType<VisualBasic.Syntax.LocalDeclarationStatementSyntax>())
            {
                foreach (var declarator in localDeclaration.Declarators)
                {
                    count++;
                    var initialValue = declarator.Initializer.Value;
                    var typeInfo = model.GetTypeInfo(initialValue);
                    Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
                }
            }

            Assert.Equal(9, count);
        }

        [Fact]
        public void BinaryCompatibility_01()
        {
            var s0 = @"
public class Base
{
    public virtual object M() => null;
}
";
            var ref0 = CreateCompilationWithoutCovariantReturns(s0).EmitToImageReference();

            var s1a = @"
public class Mid : Base
{
}
";
            var ref1a = CreateCompilationWithoutCovariantReturns(s1a, references: new[] { ref0 }, assemblyName: "ref1").EmitToImageReference();

            var s1b = @"
public class Mid : Base
{
    public override string M() => null;
}
";
            var ref1b = CreateCompilationWithCovariantReturns(s1b, references: new[] { ref0 }, assemblyName: "ref1").EmitToImageReference();

            var s2 = @"
public class Derived : Mid
{
    public override string M() => null;
}
";
            var comp = CreateCompilationWithoutCovariantReturns(s2, references: new[] { ref0, ref1a }).VerifyDiagnostics(
                // (4,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string M() => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("covariant returns").WithLocation(4, 28)
                );
            verify1(comp);

            comp = CreateCompilationWithCovariantReturns(s2, references: new[] { ref0, ref1a }).VerifyDiagnostics(
                );
            verify1(comp);
            verify1(CompilationReferenceView(comp, ref0, ref1a));
            verify1(MetadataView(comp, ref0, ref1a));
            verify1(RetargetedView(comp, ref0, ref1a));

            verify2(CompilationReferenceView(comp, ref0, ref1b));
            verify2(MetadataView(comp, ref0, ref1b));
            verify2(RetargetedView(comp, ref0, ref1b));

            static void verify1(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M", "System.String Derived.M()", "System.Object Base.M()");
            }

            static void verify2(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M", "System.String Derived.M()", "System.String Mid.M()", binaryCompat: true);
                VerifyOverride(comp, "Mid.M", "System.String Mid.M()", "System.Object Base.M()");
            }
        }

        [Fact]
        public void BinaryCompatibility_02()
        {
            var s0 = @"
public class Base
{
    public virtual object P => null;
}
";
            var ref0 = CreateCompilationWithoutCovariantReturns(s0).EmitToImageReference();

            var s1a = @"
public class Mid : Base
{
}
";
            var ref1a = CreateCompilationWithoutCovariantReturns(s1a, references: new[] { ref0 }, assemblyName: "ref1").EmitToImageReference();

            var s1b = @"
public class Mid : Base
{
    public override string P => null;
}
";
            var ref1b = CreateCompilationWithCovariantReturns(s1b, references: new[] { ref0 }, assemblyName: "ref1").EmitToImageReference();

            var s2 = @"
public class Derived : Mid
{
    public override string P => null;
}
";
            var comp = CreateCompilationWithoutCovariantReturns(s2, references: new[] { ref0, ref1a }).VerifyDiagnostics(
                // (4,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string P => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "P").WithArguments("covariant returns").WithLocation(4, 28)
                );
            verify1(comp);

            comp = CreateCompilationWithCovariantReturns(s2, references: new[] { ref0, ref1a }).VerifyDiagnostics(
                );
            verify1(comp);
            verify1(CompilationReferenceView(comp, ref0, ref1a));
            verify1(MetadataView(comp, ref0, ref1a));
            verify1(RetargetedView(comp, ref0, ref1a));

            verify2(CompilationReferenceView(comp, ref0, ref1b));
            verify2(MetadataView(comp, ref0, ref1b));
            verify2(RetargetedView(comp, ref0, ref1b));

            static void verify1(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.P", "System.String Derived.P { get; }", "System.Object Base.P { get; }");
                VerifyOverride(comp, "Derived.get_P", "System.String Derived.P.get", "System.Object Base.P.get");
            }

            static void verify2(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.P", "System.String Derived.P { get; }", "System.String Mid.P { get; }", binaryCompat: true);
                VerifyOverride(comp, "Derived.get_P", "System.String Derived.P.get", "System.String Mid.P.get", binaryCompat: true);
                VerifyOverride(comp, "Mid.P", "System.String Mid.P { get; }", "System.Object Base.P { get; }");
                VerifyOverride(comp, "Mid.get_P", "System.String Mid.P.get", "System.Object Base.P.get");
            }
        }

        [Fact]
        public void BinaryCompatibility_03()
        {
            var s0 = @"
public class Base
{
    public virtual object M() => null;
}
";
            var ref0 = CreateCompilationWithoutCovariantReturns(s0).EmitToImageReference();

            var s1a = @"
public class Mid : Base
{
}
";
            var ref1a = CreateCompilationWithoutCovariantReturns(s1a, references: new[] { ref0 }, assemblyName: "ref1").EmitToImageReference();

            var s1b = @"
public class Mid : Base
{
    public override object M() => null;
}
";
            var ref1b = CreateCompilationWithCovariantReturns(s1b, references: new[] { ref0 }, assemblyName: "ref1").EmitToImageReference();

            var s2 = @"
public class Derived : Mid
{
    public override string M() => null;
}
";
            var comp = CreateCompilationWithoutCovariantReturns(s2, references: new[] { ref0, ref1a }).VerifyDiagnostics(
                // (4,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string M() => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("covariant returns").WithLocation(4, 28)
                );
            verify1(comp);

            comp = CreateCompilationWithCovariantReturns(s2, references: new[] { ref0, ref1a }).VerifyDiagnostics(
                );
            verify1(comp);
            verify1(CompilationReferenceView(comp, ref0, ref1a));
            verify1(MetadataView(comp, ref0, ref1a));
            verify1(RetargetedView(comp, ref0, ref1a));

            verify2(CompilationReferenceView(comp, ref0, ref1b));
            verify2(MetadataView(comp, ref0, ref1b));
            verify2(RetargetedView(comp, ref0, ref1b));

            static void verify1(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M", "System.String Derived.M()", "System.Object Base.M()");
            }

            static void verify2(CSharpCompilation comp)
            {
                // When viewed from metadata, we do not see a relationship between Derived.M and Mid.M because
                // there is nothing in the metadata to indicate there is relationship. The compiler
                // does no simulate the covariant language rules on metadata nor does the compiler simulate the
                // virtual slot unification that the runtime does.
                VerifyOverride(comp, "Derived.M", "System.String Derived.M()", "System.Object Base.M()");
                VerifyOverride(comp, "Mid.M", "System.Object Mid.M()", "System.Object Base.M()");
            }
        }

        [Fact]
        public void OverlappingMethodimplRequirements_01()
        {
            var source = @"
public class A
{
    public virtual object get_P() => null;
}

public class B : A
{
    public virtual object P => null;
}

public class C : B
{
    public override string get_P() => null;
}

public class D : C
{
    public override string P => null;
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (14,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string get_P() => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "get_P").WithArguments("covariant returns").WithLocation(14, 28),
                // (19,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string P => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "P").WithArguments("covariant returns").WithLocation(19, 28)
                );
            verify(comp);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(comp);
            verify(CompilationReferenceView(comp));
            verify(MetadataView(comp));
            verify(RetargetedView(comp));

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "C.get_P", "System.String C.get_P()", "System.Object A.get_P()");
                VerifyOverride(comp, "D.P", "System.String D.P { get; }", "System.Object B.P { get; }");
                VerifyOverride(comp, "D.get_P", "System.String D.P.get", "System.Object B.P.get");
            }
        }

        [Fact]
        public void OverlappingMethodimplRequirements_02()
        {
            var source = @"
public class A
{
    public virtual string P => null;
}
public class B : A
{
    public virtual new object P => null;
}
public class C : B
{
    public override string P => null;
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (12,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string P => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "P").WithArguments("covariant returns").WithLocation(12, 28)
                );
            verify(comp);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(comp);
            verify(CompilationReferenceView(comp));
            verify(MetadataView(comp));
            verify(RetargetedView(comp));

            static void verify(CSharpCompilation comp)
            {
                VerifyNoOverride(comp, "A.P");
                VerifyNoOverride(comp, "A.get_P");
                VerifyNoOverride(comp, "B.P");
                VerifyNoOverride(comp, "B.get_P");
                VerifyOverride(comp, "C.P", "System.String C.P { get; }", "System.Object B.P { get; }");
                VerifyOverride(comp, "C.get_P", "System.String C.P.get", "System.Object B.P.get");
            }
        }

        [Fact]
        public void OverlappingMethodimplRequirements_03()
        {
            var source = @"
public class A
{
    public virtual string M() => null;
}
public class B : A
{
    public virtual new object M() => null;
}
public class C : B
{
    public override string M() => null;
}
";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (12,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string M() => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("covariant returns").WithLocation(12, 28)
                );
            verify(comp);
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(comp);
            verify(CompilationReferenceView(comp));
            verify(MetadataView(comp));
            verify(RetargetedView(comp));

            static void verify(CSharpCompilation comp)
            {
                VerifyNoOverride(comp, "A.M");
                VerifyNoOverride(comp, "B.M");
                VerifyOverride(comp, "C.M", "System.String C.M()", "System.Object B.M()");
            }
        }
    }
}
