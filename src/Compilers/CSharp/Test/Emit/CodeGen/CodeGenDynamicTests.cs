// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGen_DynamicTests : CSharpTestBase
    {
        #region Helpers

        private CompilationVerifier CompileAndVerifyIL(
            string source,
            string methodName,
            string expectedOptimizedIL = null,
            string expectedUnoptimizedIL = null,
            MetadataReference[] references = null,
            bool allowUnsafe = false,
            [CallerFilePath]string callerPath = null,
            [CallerLineNumber]int callerLine = 0)
        {
            references = references ?? new[] { SystemCoreRef, CSharpRef };

            // verify that we emit correct optimized and unoptimized IL:
            var unoptimizedCompilation = CreateCompilationWithMscorlib45(source, references, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All).WithAllowUnsafe(allowUnsafe));
            var optimizedCompilation = CreateCompilationWithMscorlib45(source, references, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All).WithAllowUnsafe(allowUnsafe));

            var unoptimizedVerifier = CompileAndVerify(unoptimizedCompilation);
            var optimizedVerifier = CompileAndVerify(optimizedCompilation);

            // check what IL we emit exactly:
            if (expectedUnoptimizedIL != null)
            {
                unoptimizedVerifier.VerifyIL(methodName, expectedUnoptimizedIL, realIL: true, sequencePoints: methodName, callerPath: callerPath, callerLine: callerLine);
            }

            if (expectedOptimizedIL != null)
            {
                optimizedVerifier.VerifyIL(methodName, expectedOptimizedIL, realIL: true, callerPath: callerPath, callerLine: callerLine);
            }

            // return null if ambiguous
            return (expectedUnoptimizedIL != null) ^ (expectedOptimizedIL != null) ? (unoptimizedVerifier ?? optimizedVerifier) : null;
        }

        #endregion

        #region C# Runtime and System.Core sources

        private const string CSharpBinderTemplate = @"
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Microsoft.CSharp.RuntimeBinder
{{
{0}
}}
";

        private const string CSharpBinderFlagsSource = @"
public enum CSharpBinderFlags
{
    None = 0,
    CheckedContext = 1,
    InvokeSimpleName = 2,
    InvokeSpecialName = 4,
    BinaryOperationLogical = 8,
    ConvertExplicit = 16,
    ConvertArrayIndex = 32,
    ResultIndexed = 64,
    ValueFromCompoundAssignment = 128,
    ResultDiscarded = 256
}
";

        private const string CSharpArgumentInfoFlagsSource = @"
public enum CSharpArgumentInfoFlags
{
    None = 0,
    UseCompileTimeType = 1,
    Constant = 2,
    NamedArgument = 4,
    IsRef = 8,
    IsOut = 16,
    IsStaticType = 32
}
";
        private const string CSharpArgumentInfoSource = @"
public sealed class CSharpArgumentInfo
{
    public static CSharpArgumentInfo Create(CSharpArgumentInfoFlags flags, string name) { return null; }
}
";
        private readonly string[] _binderFactoriesSource = new[]
        {
            "CallSiteBinder BinaryOperation(CSharpBinderFlags flags, ExpressionType operation, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)",
            "CallSiteBinder Convert(CSharpBinderFlags flags, Type type, Type context)",
            "CallSiteBinder GetIndex(CSharpBinderFlags flags, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)",
            "CallSiteBinder GetMember(CSharpBinderFlags flags, string name, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)",
            "CallSiteBinder Invoke(CSharpBinderFlags flags, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)",
            "CallSiteBinder InvokeMember(CSharpBinderFlags flags, string name, IEnumerable<Type> typeArguments, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)",
            "CallSiteBinder InvokeConstructor(CSharpBinderFlags flags, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)",
            "CallSiteBinder IsEvent(CSharpBinderFlags flags, string name, Type context)",
            "CallSiteBinder SetIndex(CSharpBinderFlags flags, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)",
            "CallSiteBinder SetMember(CSharpBinderFlags flags, string name, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)",
            "CallSiteBinder UnaryOperation(CSharpBinderFlags flags, ExpressionType operation, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)",
        };

        private MetadataReference MakeCSharpRuntime(string excludeBinder = null, bool excludeBinderFlags = false, bool excludeArgumentInfoFlags = false, MetadataReference systemCore = null)
        {
            var sb = new StringBuilder();

            sb.AppendLine(excludeBinderFlags ? "public enum CSharpBinderFlags { A }" : CSharpBinderFlagsSource);
            sb.AppendLine(excludeArgumentInfoFlags ? "public enum CSharpArgumentInfoFlags { A }" : CSharpArgumentInfoFlagsSource);
            sb.AppendLine(CSharpArgumentInfoSource);

            foreach (var src in excludeBinder == null ? _binderFactoriesSource : _binderFactoriesSource.Where(src => src.IndexOf(excludeBinder, StringComparison.Ordinal) == -1))
            {
                sb.AppendFormat("public partial class Binder {{ public static {0} {{ return null; }} }}", src);
                sb.AppendLine();
            }

            string source = string.Format(CSharpBinderTemplate, sb.ToString());
            return CreateCompilationWithMscorlib(source, new[] { systemCore ?? SystemCoreRef }, assemblyName: GetUniqueName()).EmitToImageReference();
        }

        private const string ExpressionTypeSource = @"
namespace System.Linq.Expressions
{
    public enum ExpressionType
    {
        Add, AddChecked, And, AndAlso, ArrayLength, ArrayIndex, Call, Coalesce, Conditional, Constant, Convert, ConvertChecked, 
        Divide, Equal, ExclusiveOr, GreaterThan, GreaterThanOrEqual, Invoke, Lambda, LeftShift, LessThan, LessThanOrEqual, ListInit,
        MemberAccess, MemberInit, Modulo, Multiply, MultiplyChecked, Negate, UnaryPlus, NegateChecked, New, NewArrayInit, NewArrayBounds, 
        Not, NotEqual, Or, OrElse, Parameter, Power, Quote, RightShift, Subtract, SubtractChecked, TypeAs, TypeIs, Assign, Block, DebugInfo,
        Decrement, Dynamic, Default, Extension, Goto, Increment, Index, Label, RuntimeVariables, Loop, Switch, Throw, Try, Unbox, AddAssign, 
        AndAssign, DivideAssign, ExclusiveOrAssign, LeftShiftAssign, ModuloAssign, MultiplyAssign, OrAssign, PowerAssign, RightShiftAssign, 
        SubtractAssign, AddAssignChecked, MultiplyAssignChecked, SubtractAssignChecked, PreIncrementAssign, PreDecrementAssign, PostIncrementAssign, 
        PostDecrementAssign, TypeEqual, OnesComplement, IsTrue, IsFalse
    }
}
";
        private const string DynamicAttributeSource = @"
namespace System.Runtime.CompilerServices
{
    public sealed class DynamicAttribute : Attribute
    {
        public DynamicAttribute() { }
        public DynamicAttribute(bool[] transformFlags) { }
    }
}";

        private const string CallSiteSource = @"
namespace System.Runtime.CompilerServices
{
    public class CallSite { }

    public class CallSite<T> : CallSite where T : class
    {
        public T Target;

        public static CallSite<T> Create(CallSiteBinder binder)
        {
            return null;
        }
    }

    public abstract class CallSiteBinder { }
}";

        private const string SystemCoreSource = ExpressionTypeSource + DynamicAttributeSource + CallSiteSource;

        #endregion

        #region Missing Well-Known Members

        [Fact]
        public void Missing_CSharpArgumentInfo()
        {
            string source = @"
class C
{
    public event System.Action e;
    public C(dynamic d) { }

    void F(dynamic d) 
    { 
        var a1 = d * d;
        var a2 = (int)d;
        var a3 = d[d];
        var a4 = d.M;
        var a5 = d();
        var a6 = d.M();
        var a7 = new C(d);
        e += d;
        var a9 = d[d] = d;
        var a10 = d.M = d;
        var a11 = -d;
        e();
    }
}
";
            CreateCompilationWithMscorlibAndSystemCore(source).VerifyEmitDiagnostics(
    // (9,18): error CS0656: Missing compiler required member 'Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create'
    //         var a1 = d * d;
    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "d").WithArguments("Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo", "Create").WithLocation(9, 18)
            );
        }

        [Fact]
        public void Missing_Binder()
        {
            var csrtRef = MakeCSharpRuntime(excludeBinder: "InvokeConstructor");

            string source = @"
class C
{
    public C(int a) { }

    void F(dynamic d) 
    {
        new C(d.M(d.M = d[-d], d[(int)d()] = d * d.M));
    }
}
";
            CreateCompilationWithMscorlib(source, new[] { SystemCoreRef, csrtRef }).VerifyEmitDiagnostics(
                // (8,9): error CS0656: Missing compiler required member 'Microsoft.CSharp.RuntimeBinder.Binder.InvokeConstructor'
                //         new C(d.M(d.M = d[-d], d[(int)d()] = d * d.M));
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "new C(d.M(d.M = d[-d], d[(int)d()] = d * d.M))").WithArguments("Microsoft.CSharp.RuntimeBinder.Binder", "InvokeConstructor")
                );
        }

        [Fact]
        public void Missing_Flags()
        {
            var csrtRef = MakeCSharpRuntime(excludeBinderFlags: true, excludeArgumentInfoFlags: true);

            string source = @"
class C
{
    public static void G(int a) { }

    void F(dynamic d) 
    {
        G(d); // CSharpBinderFlags.InvokeSimpleName, CSharpBinderFlags.ResultDiscarded
              // CSharpArgumentInfoFlags.None, CSharpArgumentInfoFlags.UseCompileTimeType
    }
}
";
            // the compiler ignores the enum values, uses hardcoded values:
            CreateCompilationWithMscorlib(source, new[] { SystemCoreRef, csrtRef }).VerifyEmitDiagnostics();
        }

        [Fact]
        public void Missing_Func()
        {
            var systemCoreRef = CreateCompilationWithMscorlib(SystemCoreSource, assemblyName: GetUniqueName()).EmitToImageReference();
            var csrtRef = MakeCSharpRuntime(systemCore: systemCoreRef);

            string source = @"
class C
{
    dynamic F(dynamic d) 
    {
        return d(1,2,3,4,5,6,7,8,9,10); // Func`13
    }
}
";
            // the delegate is generated, no error is reported
            CompileAndVerify(source, new[] { systemCoreRef, csrtRef });
        }

        [Fact]
        public void InvalidFunc_Arity()
        {
            var systemCoreRef = CreateCompilationWithMscorlib(SystemCoreSource, assemblyName: GetUniqueName()).EmitToImageReference();
            var csrtRef = MakeCSharpRuntime(systemCore: systemCoreRef);
            var funcRef = MetadataReference.CreateFromImage(TestResources.MetadataTests.Invalid.InvalidFuncDelegateName.AsImmutableOrNull());

            string source = @"
class C
{
    dynamic F(dynamic d) 
    {
        return d(1,2,3,4,5,6,7,8,9,10); // Func`13
    }
}
";
            // the delegate is generated, no error is reported
            var c = CompileAndVerify(source, new[] { systemCoreRef, csrtRef, funcRef });
            Assert.Equal(1, ((CSharpCompilation)c.Compilation).GlobalNamespace.GetMember<NamespaceSymbol>("System").GetMember<NamedTypeSymbol>("Func`13").Arity);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/6190"), WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")]
        public void InvalidFunc_Constraints()
        {
            var systemCoreRef = CreateCompilationWithMscorlib(SystemCoreSource, assemblyName: GetUniqueName()).EmitToImageReference();
            var csrtRef = MakeCSharpRuntime(systemCore: systemCoreRef);

            string source = @"
namespace System
{
    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12)
        where T4 : class;
}

class C
{
    dynamic F(dynamic d) 
    {
        return d(1,2,3,4,5,6,7,8,9,10); // Func`13
    }
}
";
            // Desired: the delegate is generated, no error is reported.
            // Actual: use the malformed Func`13 time and failed to PEVerify.  Not presently worthwhile to fix.
            Assert.Throws<PeVerifyException>(() => CompileAndVerify(source, new[] { systemCoreRef, csrtRef }));
        }

        [Fact]
        public void Missing_CallSite()
        {
            string systemCoreSource = ExpressionTypeSource + DynamicAttributeSource + @"
namespace System.Runtime.CompilerServices
{
    public class CallSite<T> where T : class
    {
        public T Target;

        public static CallSite<T> Create(CallSiteBinder binder)
        {
            return null;
        }
    }

    public abstract class CallSiteBinder { }
}";

            var systemCoreRef = CreateCompilationWithMscorlib(systemCoreSource, assemblyName: GetUniqueName()).EmitToImageReference();
            var csrtRef = MakeCSharpRuntime(systemCore: systemCoreRef);

            string source = @"
class C 
{
    dynamic F(dynamic d)
    {
        return d * d;
    }
}
";
            CreateCompilationWithMscorlib(source, new[] { systemCoreRef, csrtRef }).VerifyEmitDiagnostics(
                // (6,16): error CS0518: Predefined type 'System.Runtime.CompilerServices.CallSite' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "d").WithArguments("System.Runtime.CompilerServices.CallSite"),
                // error CS1969: One or more types required to compile a dynamic expression cannot be found. Are you missing a reference?
                Diagnostic(ErrorCode.ERR_DynamicRequiredTypesMissing));
        }

        [Fact]
        public void Missing_CallSiteOfT()
        {
            string systemCoreSource = ExpressionTypeSource + DynamicAttributeSource + @"
namespace System.Runtime.CompilerServices
{
    public class CallSite { }
    public class CallSiteBinder {}
}";

            var systemCoreRef = CreateCompilationWithMscorlib(systemCoreSource, assemblyName: GetUniqueName()).EmitToImageReference();
            var csrtRef = MakeCSharpRuntime(systemCore: systemCoreRef);

            string source = @"
class C 
{
    dynamic F(dynamic d)
    {
        return d * d;
    }
}
";
            CreateCompilationWithMscorlib(source, new[] { systemCoreRef, csrtRef }).VerifyEmitDiagnostics(
    // (6,16): error CS0518: Predefined type 'System.Runtime.CompilerServices.CallSite`1' is not defined or imported
    //         return d * d;
    Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "d").WithArguments("System.Runtime.CompilerServices.CallSite`1").WithLocation(6, 16),
    // (6,16): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.CallSite`1.Create'
    //         return d * d;
    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "d").WithArguments("System.Runtime.CompilerServices.CallSite`1", "Create").WithLocation(6, 16)
                );
        }

        #endregion

        #region Generated Metadata (call-site containers, delegates)

        /// <summary>
        /// Dev11 doesn't include name of explicit interface implementation method into site container name.
        /// </summary>
        [Fact]
        public void ExplicitInterfaceImplementation()
        {
            string source = @"
public interface I
{
    dynamic M(dynamic d);
}

public class C : I
{
    dynamic I.M(dynamic d)
    {
        return checked(d * d);
    }
}";
            CompileAndVerifyIL(source, "C.I.M", @"
{
  // Code size       84 (0x54)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_003d
  IL_0007:  ldc.i4.1
  IL_0008:  ldc.i4.s   26
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.2
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  dup
  IL_0025:  ldc.i4.1
  IL_0026:  ldc.i4.0
  IL_0027:  ldnull
  IL_0028:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002d:  stelem.ref
  IL_002e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0033:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0038:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0042:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_004c:  ldarg.1
  IL_004d:  ldarg.1
  IL_004e:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0053:  ret
}
");
        }

        [Fact]
        public void CallSiteContainers()
        {
            string source = @"
public class C
{
    public void M1(dynamic d)
    {
        d.m(1,2,3);
    }

    public void M2(dynamic d)
    {
        d.m(1,2,3);
    }
}";
            var verifier = CompileAndVerify(source, additionalRefs: new[] { SystemCoreRef, CSharpRef }, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: peModule =>
            {
                var c = peModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var containers = c.GetMembers().OfType<NamedTypeSymbol>().ToArray();
                Assert.Equal(2, containers.Length);

                foreach (var container in containers)
                {
                    Assert.Equal(Accessibility.Private, container.DeclaredAccessibility);
                    Assert.True(container.IsStatic);
                    Assert.Equal(SpecialType.System_Object, container.BaseType.SpecialType);
                    AssertEx.SetEqual(new[] { "CompilerGeneratedAttribute" }, GetAttributeNames(container.GetAttributes()));

                    var members = container.GetMembers();
                    Assert.Equal(1, members.Length);
                    var field = (FieldSymbol)members[0];

                    Assert.Equal(Accessibility.Public, field.DeclaredAccessibility);
                    Assert.True(field.IsStatic);
                    Assert.False(field.IsReadOnly);

                    Assert.Equal("System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int, int, int>>",
                        field.Type.ToDisplayString());

                    Assert.Equal(0, field.GetAttributes().Length);

                    switch (container.Name)
                    {
                        case "<>o__0":
                            Assert.Equal("<>p__0", field.Name);
                            break;

                        case "<>o__1":
                            Assert.Equal("<>p__0", field.Name);
                            break;

                        default:
                            throw TestExceptionUtilities.UnexpectedValue(container.Name);
                    }
                }
            });
        }

        [Fact]
        public void CallSiteContainers_MultipleSitesInMethod_DisplayClass()
        {
            string source = @"
public class C
{
    public void M1(dynamic d)
    {
        d.m(1);
        d.m(1,2);
        var x = new System.Action(() => d.m());
    }
}";
            var verifier = CompileAndVerify(source, additionalRefs: new[] { SystemCoreRef, CSharpRef }, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: peModule =>
            {
                var c = peModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                Assert.Equal(2, c.GetMembers().OfType<NamedTypeSymbol>().Count());

                var container = c.GetMember<NamedTypeSymbol>("<>o__0");

                // all call-site storage fields of the method are added to a single container:
                var memberNames = container.GetMembers().Select(m => m.Name);
                AssertEx.SetEqual(new[] { "<>p__0", "<>p__1", "<>p__2" }, memberNames);

                var displayClass = c.GetMember<NamedTypeSymbol>("<>c__DisplayClass0_0");
                var d = displayClass.GetMember<FieldSymbol>("d");
                Assert.Equal(0, d.GetAttributes().Length);
            });
        }

        [Fact]
        public void Iterator()
        {
            string source = @"
using System.Collections.Generic;

public class C
{
    public IEnumerable<dynamic> M1()
    {
        dynamic d = 1;
        yield return 1;
        d = d + 2;
        yield return d;        
    }
}";
            var verifier = CompileAndVerify(source, additionalRefs: new[] { SystemCoreRef, CSharpRef }, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: peModule =>
            {
                var c = peModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var iteratorClass = c.GetMember<NamedTypeSymbol>("<M1>d__0");

                foreach (var member in iteratorClass.GetMembers())
                {
                    switch (member.Kind)
                    {
                        case SymbolKind.Field:
                            // no field is marked with DynamicAttribute
                            Assert.Equal(0, member.GetAttributes().Length);
                            break;

                        case SymbolKind.Method:
                            // Dev11 marks return type of GetEnumerator with DynamicAttribute, we don't
                            Assert.Equal(0, ((MethodSymbol)member).GetReturnTypeAttributes().Length);
                            break;

                        case SymbolKind.Property:
                            // "Current" properties or return types are not marked with attributes
                            break;

                        default:
                            throw TestExceptionUtilities.UnexpectedValue(member.Kind);
                    }
                }

                var container = c.GetMember<NamedTypeSymbol>("<>o__0");
                Assert.Equal(1, container.GetMembers().Length);
            });
        }

        [Fact]
        [WorkItem(625282, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/625282")]
        public void GenericIterator()
        {
            string source = @"
using System.Collections.Generic;

class C
{
    dynamic d = null;
    
    public IEnumerable<T> Run<T>()
    {
        yield return d;
    }
}
";
            CompileAndVerifyIL(source, "C.<Run>d__1<T>.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size      121 (0x79)
  .maxstack  4
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<Run>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0010
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  beq.s      IL_0070
  IL_000e:  ldc.i4.0
  IL_000f:  ret
  IL_0010:  ldarg.0
  IL_0011:  ldc.i4.m1
  IL_0012:  stfld      ""int C.<Run>d__1<T>.<>1__state""
  IL_0017:  ldarg.0
  IL_0018:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, T>> C.<>o__1<T>.<>p__0""
  IL_001d:  brtrue.s   IL_0043
  IL_001f:  ldc.i4.0
  IL_0020:  ldtoken    ""T""
  IL_0025:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_002a:  ldtoken    ""C""
  IL_002f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0034:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0039:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, T>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, T>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_003e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, T>> C.<>o__1<T>.<>p__0""
  IL_0043:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, T>> C.<>o__1<T>.<>p__0""
  IL_0048:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, T> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, T>>.Target""
  IL_004d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, T>> C.<>o__1<T>.<>p__0""
  IL_0052:  ldarg.0
  IL_0053:  ldfld      ""C C.<Run>d__1<T>.<>4__this""
  IL_0058:  ldfld      ""dynamic C.d""
  IL_005d:  callvirt   ""T System.Func<System.Runtime.CompilerServices.CallSite, object, T>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0062:  stfld      ""T C.<Run>d__1<T>.<>2__current""
  IL_0067:  ldarg.0
  IL_0068:  ldc.i4.1
  IL_0069:  stfld      ""int C.<Run>d__1<T>.<>1__state""
  IL_006e:  ldc.i4.1
  IL_006f:  ret
  IL_0070:  ldarg.0
  IL_0071:  ldc.i4.m1
  IL_0072:  stfld      ""int C.<Run>d__1<T>.<>1__state""
  IL_0077:  ldc.i4.0
  IL_0078:  ret
}
");
        }

        [Fact]
        public void NoDynamicAttributeOnCallSiteStorageField()
        {
            string source = @"
using System.Collections.Generic;

public class C
{
    public dynamic M(dynamic d, List<dynamic> a, Dictionary<dynamic, dynamic[]>[] b)
    {
        return d(a, b);
    }
}";
            var verifier = CompileAndVerify(source, additionalRefs: new[] { SystemCoreRef, CSharpRef }, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: peModule =>
            {
                var container = peModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<NamedTypeSymbol>("<>o__0");
                Assert.Equal(0, container.GetMembers().Single().GetAttributes().Length);
            });
        }

        [Fact]
        public void GeneratedDelegates()
        {
            string source = @"
using System.Collections.Generic;

public class C
{
    public dynamic M(dynamic d)
    {
        return d(ref d);
    }
}";
            var verifier = CompileAndVerify(source, additionalRefs: new[] { SystemCoreRef, CSharpRef }, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: peModule =>
            {
                var d = peModule.GlobalNamespace.GetMember<NamedTypeSymbol>("<>F{00000004}");

                // the type:
                Assert.Equal(Accessibility.Internal, d.DeclaredAccessibility);
                Assert.Equal(4, d.TypeParameters.Length);
                Assert.True(d.IsSealed);
                Assert.Equal(CharSet.Ansi, d.MarshallingCharSet);
                Assert.Equal(SpecialType.System_MulticastDelegate, d.BaseType.SpecialType);
                Assert.Equal(0, d.Interfaces.Length);
                AssertEx.SetEqual(new[] { "CompilerGeneratedAttribute" }, GetAttributeNames(d.GetAttributes()));

                // members:
                var members = d.GetMembers();
                Assert.Equal(2, members.Length);
                foreach (var member in members)
                {
                    Assert.Equal(Accessibility.Public, member.DeclaredAccessibility);

                    switch (member.Name)
                    {
                        case ".ctor":
                            Assert.False(member.IsStatic);
                            Assert.False(member.IsSealed);
                            Assert.False(member.IsVirtual);
                            break;

                        case "Invoke":
                            Assert.False(member.IsStatic);
                            Assert.False(member.IsSealed);
                            Assert.True(member.IsVirtual);
                            break;

                        default:
                            throw TestExceptionUtilities.UnexpectedValue(member.Name);
                    }
                }
            });
        }

        [Fact]
        public void GenericContainer1()
        {
            string source = @"
public class C
{
    public T M<T>(dynamic d)
    {
        return (T)d;
    }
}
";
            CompileAndVerifyIL(source, "C.M<T>", @"
{
  // Code size       66 (0x42)
  .maxstack  3
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, T>> C.<>o__0<T>.<>p__0""
  IL_0005:  brtrue.s   IL_002c
  IL_0007:  ldc.i4.s   16
  IL_0009:  ldtoken    ""T""
  IL_000e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0013:  ldtoken    ""C""
  IL_0018:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0022:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, T>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, T>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0027:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, T>> C.<>o__0<T>.<>p__0""
  IL_002c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, T>> C.<>o__0<T>.<>p__0""
  IL_0031:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, T> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, T>>.Target""
  IL_0036:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, T>> C.<>o__0<T>.<>p__0""
  IL_003b:  ldarg.1
  IL_003c:  callvirt   ""T System.Func<System.Runtime.CompilerServices.CallSite, object, T>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0041:  ret
}
");
        }

        [Fact]
        public void GenericContainer2()
        {
            string source = @"
public class E<P, Q, R> {}

public class C<U>
{
    public dynamic M<S,T>(dynamic d)
    {
        E<S, T, U> dict = null;
        return d(ref dict);
    }
}
";
            CompileAndVerifyIL(source, "C<U>.M<S, T>", @"
{
  // Code size       86 (0x56)
  .maxstack  7
  .locals init (E<S, T, U> V_0) //dict
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F{00000004}<System.Runtime.CompilerServices.CallSite, object, E<S, T, U>, object>> C<U>.<>o__0<S, T>.<>p__0""
  IL_0007:  brtrue.s   IL_003e
  IL_0009:  ldc.i4.0
  IL_000a:  ldtoken    ""C<U>""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.2
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  dup
  IL_0025:  ldc.i4.1
  IL_0026:  ldc.i4.s   9
  IL_0028:  ldnull
  IL_0029:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002e:  stelem.ref
  IL_002f:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Invoke(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0034:  call       ""System.Runtime.CompilerServices.CallSite<<>F{00000004}<System.Runtime.CompilerServices.CallSite, object, E<S, T, U>, object>> System.Runtime.CompilerServices.CallSite<<>F{00000004}<System.Runtime.CompilerServices.CallSite, object, E<S, T, U>, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0039:  stsfld     ""System.Runtime.CompilerServices.CallSite<<>F{00000004}<System.Runtime.CompilerServices.CallSite, object, E<S, T, U>, object>> C<U>.<>o__0<S, T>.<>p__0""
  IL_003e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F{00000004}<System.Runtime.CompilerServices.CallSite, object, E<S, T, U>, object>> C<U>.<>o__0<S, T>.<>p__0""
  IL_0043:  ldfld      ""<>F{00000004}<System.Runtime.CompilerServices.CallSite, object, E<S, T, U>, object> System.Runtime.CompilerServices.CallSite<<>F{00000004}<System.Runtime.CompilerServices.CallSite, object, E<S, T, U>, object>>.Target""
  IL_0048:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F{00000004}<System.Runtime.CompilerServices.CallSite, object, E<S, T, U>, object>> C<U>.<>o__0<S, T>.<>p__0""
  IL_004d:  ldarg.1
  IL_004e:  ldloca.s   V_0
  IL_0050:  callvirt   ""object <>F{00000004}<System.Runtime.CompilerServices.CallSite, object, E<S, T, U>, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, ref E<S, T, U>)""
  IL_0055:  ret
}
");
        }

        [Fact]
        public void GenericContainer3()
        {
            string source = @"
public class C<U>
{
    public dynamic M(dynamic d)
    {
        return d();
    }
}
";
            CompileAndVerifyIL(source, "C<U>.M", @"
{
  // Code size       71 (0x47)
  .maxstack  7
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C<U>.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0031
  IL_0007:  ldc.i4.0
  IL_0008:  ldtoken    ""C<U>""
  IL_000d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0012:  ldc.i4.1
  IL_0013:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0018:  dup
  IL_0019:  ldc.i4.0
  IL_001a:  ldc.i4.0
  IL_001b:  ldnull
  IL_001c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0021:  stelem.ref
  IL_0022:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Invoke(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0027:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_002c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C<U>.<>o__0.<>p__0""
  IL_0031:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C<U>.<>o__0.<>p__0""
  IL_0036:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_003b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C<U>.<>o__0.<>p__0""
  IL_0040:  ldarg.1
  IL_0041:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0046:  ret
}
");
        }

        [Fact]
        public void GenericContainer4()
        {
            string source = @"
using System;

class C
{
    public static int F<T>(dynamic d, Type t, T x) where T : struct
    {
        if (d.GetType() == t && ((T)d).Equals(x)) 
        {
            return 1;
        }
        
        return 2;
    }
}
";
            CompileAndVerify(source, new[] { CSharpRef, SystemCoreRef });
        }

        [Fact]
        [WorkItem(627091, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627091")]
        public void GenericContainer_Lambda()
        {
            string source = @"
class C
{
    static void Foo<T>(T a, dynamic b)
    {
        System.Action f = () => Foo(a, b);
    }
}
";
            CompileAndVerifyIL(source, "C.<>c__DisplayClass0_0<T>.<Foo>b__0", @"
{
  // Code size      123 (0x7b)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, T, object>> C.<>o__0<T>.<>p__0""
  IL_0005:  brtrue.s   IL_0050
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldstr      ""Foo""
  IL_0011:  ldnull
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.3
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.s   33
  IL_0026:  ldnull
  IL_0027:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002c:  stelem.ref
  IL_002d:  dup
  IL_002e:  ldc.i4.1
  IL_002f:  ldc.i4.1
  IL_0030:  ldnull
  IL_0031:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0036:  stelem.ref
  IL_0037:  dup
  IL_0038:  ldc.i4.2
  IL_0039:  ldc.i4.0
  IL_003a:  ldnull
  IL_003b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0040:  stelem.ref
  IL_0041:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0046:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, T, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, T, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_004b:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, T, object>> C.<>o__0<T>.<>p__0""
  IL_0050:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, T, object>> C.<>o__0<T>.<>p__0""
  IL_0055:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, System.Type, T, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, T, object>>.Target""
  IL_005a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, T, object>> C.<>o__0<T>.<>p__0""
  IL_005f:  ldtoken    ""C""
  IL_0064:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0069:  ldarg.0
  IL_006a:  ldfld      ""T C.<>c__DisplayClass0_0<T>.a""
  IL_006f:  ldarg.0
  IL_0070:  ldfld      ""object C.<>c__DisplayClass0_0<T>.b""
  IL_0075:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, System.Type, T, object>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, T, object)""
  IL_007a:  ret
}
");
        }

        [Fact]
        public void DynamicErasure_MetadataConstant()
        {
            string source = @"
public class C 
{
    const dynamic f = null;

    public void M(dynamic arg = null)
    { 
        const dynamic local = null;
    }
}
";
            CompileAndVerify(source, new[] { SystemCoreRef });
        }

        [Fact, WorkItem(16, "http://roslyn.codeplex.com/workitem/16")]
        public void RemoveAtOfKeywordAsDynamicMemberName()
        {
            string source = @"
using System;

class C
{
    // field
    public int @default = 123;

    // prop
    protected string @if
    {
        get; set;
    }

    int @else
    {
        get { return 1; }
    }

    // event
    public event Action @event
    {
        add { }
        remove { }
    }

    // Method
    internal int @while(dynamic @void) { return 456; }

    static void Main()
    {
        dynamic dyn = new C();

        if (dyn.@default == 123)
        {
            dynamic @static = 12;
            dyn.@default = dyn.@while(@static);          
        }

        dyn.@if = dyn.@else.ToString();
        dyn.@event += (Action)( () => { dyn.@if = dyn.@else.ToString(); });
    }
}
";
            CompileAndVerifyIL(source, "C.Main", @"
{
  // Code size     1118 (0x45e)
  .maxstack  13
  .locals init (C.<>c__DisplayClass11_0 V_0, //CS$<>8__locals0
                object V_1, //static
                System.Action V_2,
                object V_3)
  IL_0000:  newobj     ""C.<>c__DisplayClass11_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  newobj     ""C..ctor()""
  IL_000c:  stfld      ""object C.<>c__DisplayClass11_0.dyn""
  IL_0011:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__11.<>p__2""
  IL_0016:  brtrue.s   IL_0044
  IL_0018:  ldc.i4.0
  IL_0019:  ldc.i4.s   83
  IL_001b:  ldtoken    ""C""
  IL_0020:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0025:  ldc.i4.1
  IL_0026:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_002b:  dup
  IL_002c:  ldc.i4.0
  IL_002d:  ldc.i4.0
  IL_002e:  ldnull
  IL_002f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0034:  stelem.ref
  IL_0035:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003a:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_003f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__11.<>p__2""
  IL_0044:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__11.<>p__2""
  IL_0049:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Target""
  IL_004e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__11.<>p__2""
  IL_0053:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__11.<>p__1""
  IL_0058:  brtrue.s   IL_0090
  IL_005a:  ldc.i4.0
  IL_005b:  ldc.i4.s   13
  IL_005d:  ldtoken    ""C""
  IL_0062:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0067:  ldc.i4.2
  IL_0068:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_006d:  dup
  IL_006e:  ldc.i4.0
  IL_006f:  ldc.i4.0
  IL_0070:  ldnull
  IL_0071:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0076:  stelem.ref
  IL_0077:  dup
  IL_0078:  ldc.i4.1
  IL_0079:  ldc.i4.3
  IL_007a:  ldnull
  IL_007b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0080:  stelem.ref
  IL_0081:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0086:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_008b:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__11.<>p__1""
  IL_0090:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__11.<>p__1""
  IL_0095:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>>.Target""
  IL_009a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__11.<>p__1""
  IL_009f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__11.<>p__0""
  IL_00a4:  brtrue.s   IL_00d5
  IL_00a6:  ldc.i4.0
  IL_00a7:  ldstr      ""default""
  IL_00ac:  ldtoken    ""C""
  IL_00b1:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00b6:  ldc.i4.1
  IL_00b7:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00bc:  dup
  IL_00bd:  ldc.i4.0
  IL_00be:  ldc.i4.0
  IL_00bf:  ldnull
  IL_00c0:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00c5:  stelem.ref
  IL_00c6:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00cb:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00d0:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__11.<>p__0""
  IL_00d5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__11.<>p__0""
  IL_00da:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_00df:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__11.<>p__0""
  IL_00e4:  ldloc.0
  IL_00e5:  ldfld      ""object C.<>c__DisplayClass11_0.dyn""
  IL_00ea:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_00ef:  ldc.i4.s   123
  IL_00f1:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, int)""
  IL_00f6:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, object, bool>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_00fb:  brfalse    IL_01bf
  IL_0100:  ldc.i4.s   12
  IL_0102:  box        ""int""
  IL_0107:  stloc.1
  IL_0108:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__11.<>p__4""
  IL_010d:  brtrue.s   IL_0148
  IL_010f:  ldc.i4.0
  IL_0110:  ldstr      ""default""
  IL_0115:  ldtoken    ""C""
  IL_011a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_011f:  ldc.i4.2
  IL_0120:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0125:  dup
  IL_0126:  ldc.i4.0
  IL_0127:  ldc.i4.0
  IL_0128:  ldnull
  IL_0129:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_012e:  stelem.ref
  IL_012f:  dup
  IL_0130:  ldc.i4.1
  IL_0131:  ldc.i4.0
  IL_0132:  ldnull
  IL_0133:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0138:  stelem.ref
  IL_0139:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_013e:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0143:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__11.<>p__4""
  IL_0148:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__11.<>p__4""
  IL_014d:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0152:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__11.<>p__4""
  IL_0157:  ldloc.0
  IL_0158:  ldfld      ""object C.<>c__DisplayClass11_0.dyn""
  IL_015d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__11.<>p__3""
  IL_0162:  brtrue.s   IL_019e
  IL_0164:  ldc.i4.0
  IL_0165:  ldstr      ""while""
  IL_016a:  ldnull
  IL_016b:  ldtoken    ""C""
  IL_0170:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0175:  ldc.i4.2
  IL_0176:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_017b:  dup
  IL_017c:  ldc.i4.0
  IL_017d:  ldc.i4.0
  IL_017e:  ldnull
  IL_017f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0184:  stelem.ref
  IL_0185:  dup
  IL_0186:  ldc.i4.1
  IL_0187:  ldc.i4.0
  IL_0188:  ldnull
  IL_0189:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_018e:  stelem.ref
  IL_018f:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0194:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0199:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__11.<>p__3""
  IL_019e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__11.<>p__3""
  IL_01a3:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_01a8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__11.<>p__3""
  IL_01ad:  ldloc.0
  IL_01ae:  ldfld      ""object C.<>c__DisplayClass11_0.dyn""
  IL_01b3:  ldloc.1
  IL_01b4:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_01b9:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_01be:  pop
  IL_01bf:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__11.<>p__7""
  IL_01c4:  brtrue.s   IL_01ff
  IL_01c6:  ldc.i4.0
  IL_01c7:  ldstr      ""if""
  IL_01cc:  ldtoken    ""C""
  IL_01d1:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_01d6:  ldc.i4.2
  IL_01d7:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_01dc:  dup
  IL_01dd:  ldc.i4.0
  IL_01de:  ldc.i4.0
  IL_01df:  ldnull
  IL_01e0:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_01e5:  stelem.ref
  IL_01e6:  dup
  IL_01e7:  ldc.i4.1
  IL_01e8:  ldc.i4.0
  IL_01e9:  ldnull
  IL_01ea:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_01ef:  stelem.ref
  IL_01f0:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_01f5:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_01fa:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__11.<>p__7""
  IL_01ff:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__11.<>p__7""
  IL_0204:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0209:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__11.<>p__7""
  IL_020e:  ldloc.0
  IL_020f:  ldfld      ""object C.<>c__DisplayClass11_0.dyn""
  IL_0214:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__11.<>p__6""
  IL_0219:  brtrue.s   IL_024b
  IL_021b:  ldc.i4.0
  IL_021c:  ldstr      ""ToString""
  IL_0221:  ldnull
  IL_0222:  ldtoken    ""C""
  IL_0227:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_022c:  ldc.i4.1
  IL_022d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0232:  dup
  IL_0233:  ldc.i4.0
  IL_0234:  ldc.i4.0
  IL_0235:  ldnull
  IL_0236:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_023b:  stelem.ref
  IL_023c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0241:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0246:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__11.<>p__6""
  IL_024b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__11.<>p__6""
  IL_0250:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_0255:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__11.<>p__6""
  IL_025a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__11.<>p__5""
  IL_025f:  brtrue.s   IL_0290
  IL_0261:  ldc.i4.0
  IL_0262:  ldstr      ""else""
  IL_0267:  ldtoken    ""C""
  IL_026c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0271:  ldc.i4.1
  IL_0272:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0277:  dup
  IL_0278:  ldc.i4.0
  IL_0279:  ldc.i4.0
  IL_027a:  ldnull
  IL_027b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0280:  stelem.ref
  IL_0281:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0286:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_028b:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__11.<>p__5""
  IL_0290:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__11.<>p__5""
  IL_0295:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_029a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__11.<>p__5""
  IL_029f:  ldloc.0
  IL_02a0:  ldfld      ""object C.<>c__DisplayClass11_0.dyn""
  IL_02a5:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_02aa:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_02af:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_02b4:  pop
  IL_02b5:  ldloc.0
  IL_02b6:  ldftn      ""void C.<>c__DisplayClass11_0.<Main>b__0()""
  IL_02bc:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_02c1:  stloc.2
  IL_02c2:  ldloc.0
  IL_02c3:  ldfld      ""object C.<>c__DisplayClass11_0.dyn""
  IL_02c8:  stloc.3
  IL_02c9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__11.<>p__14""
  IL_02ce:  brtrue.s   IL_02ef
  IL_02d0:  ldc.i4.0
  IL_02d1:  ldstr      ""event""
  IL_02d6:  ldtoken    ""C""
  IL_02db:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_02e0:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_02e5:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_02ea:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__11.<>p__14""
  IL_02ef:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__11.<>p__14""
  IL_02f4:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Target""
  IL_02f9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__11.<>p__14""
  IL_02fe:  ldloc.3
  IL_02ff:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, object, bool>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0304:  brtrue     IL_0401
  IL_0309:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__11.<>p__13""
  IL_030e:  brtrue.s   IL_034d
  IL_0310:  ldc.i4     0x80
  IL_0315:  ldstr      ""event""
  IL_031a:  ldtoken    ""C""
  IL_031f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0324:  ldc.i4.2
  IL_0325:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_032a:  dup
  IL_032b:  ldc.i4.0
  IL_032c:  ldc.i4.0
  IL_032d:  ldnull
  IL_032e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0333:  stelem.ref
  IL_0334:  dup
  IL_0335:  ldc.i4.1
  IL_0336:  ldc.i4.0
  IL_0337:  ldnull
  IL_0338:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_033d:  stelem.ref
  IL_033e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0343:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0348:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__11.<>p__13""
  IL_034d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__11.<>p__13""
  IL_0352:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0357:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__11.<>p__13""
  IL_035c:  ldloc.3
  IL_035d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action, object>> C.<>o__11.<>p__12""
  IL_0362:  brtrue.s   IL_039a
  IL_0364:  ldc.i4.0
  IL_0365:  ldc.i4.s   63
  IL_0367:  ldtoken    ""C""
  IL_036c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0371:  ldc.i4.2
  IL_0372:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0377:  dup
  IL_0378:  ldc.i4.0
  IL_0379:  ldc.i4.0
  IL_037a:  ldnull
  IL_037b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0380:  stelem.ref
  IL_0381:  dup
  IL_0382:  ldc.i4.1
  IL_0383:  ldc.i4.1
  IL_0384:  ldnull
  IL_0385:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_038a:  stelem.ref
  IL_038b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0390:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0395:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action, object>> C.<>o__11.<>p__12""
  IL_039a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action, object>> C.<>o__11.<>p__12""
  IL_039f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action, object>>.Target""
  IL_03a4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action, object>> C.<>o__11.<>p__12""
  IL_03a9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__11.<>p__11""
  IL_03ae:  brtrue.s   IL_03df
  IL_03b0:  ldc.i4.0
  IL_03b1:  ldstr      ""event""
  IL_03b6:  ldtoken    ""C""
  IL_03bb:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_03c0:  ldc.i4.1
  IL_03c1:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_03c6:  dup
  IL_03c7:  ldc.i4.0
  IL_03c8:  ldc.i4.0
  IL_03c9:  ldnull
  IL_03ca:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_03cf:  stelem.ref
  IL_03d0:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_03d5:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_03da:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__11.<>p__11""
  IL_03df:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__11.<>p__11""
  IL_03e4:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_03e9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__11.<>p__11""
  IL_03ee:  ldloc.3
  IL_03ef:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_03f4:  ldloc.2
  IL_03f5:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, System.Action)""
  IL_03fa:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_03ff:  pop
  IL_0400:  ret
  IL_0401:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action, object>> C.<>o__11.<>p__15""
  IL_0406:  brtrue.s   IL_0446
  IL_0408:  ldc.i4     0x104
  IL_040d:  ldstr      ""add_event""
  IL_0412:  ldnull
  IL_0413:  ldtoken    ""C""
  IL_0418:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_041d:  ldc.i4.2
  IL_041e:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0423:  dup
  IL_0424:  ldc.i4.0
  IL_0425:  ldc.i4.0
  IL_0426:  ldnull
  IL_0427:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_042c:  stelem.ref
  IL_042d:  dup
  IL_042e:  ldc.i4.1
  IL_042f:  ldc.i4.1
  IL_0430:  ldnull
  IL_0431:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0436:  stelem.ref
  IL_0437:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_043c:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0441:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action, object>> C.<>o__11.<>p__15""
  IL_0446:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action, object>> C.<>o__11.<>p__15""
  IL_044b:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action, object>>.Target""
  IL_0450:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action, object>> C.<>o__11.<>p__15""
  IL_0455:  ldloc.3
  IL_0456:  ldloc.2
  IL_0457:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, System.Action)""
  IL_045c:  pop
  IL_045d:  ret
}
");
        }

        #endregion

        #region Conversions

        [Fact]
        public void Conversion_Assignment()
        {
            string source = @"
public class C
{
    dynamic d = null;

    void M()
    {
        int i = d;
    }
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       71 (0x47)
  .maxstack  3
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__1.<>p__0""
  IL_0005:  brtrue.s   IL_002b
  IL_0007:  ldc.i4.0
  IL_0008:  ldtoken    ""int""
  IL_000d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0021:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0026:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__1.<>p__0""
  IL_002b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__1.<>p__0""
  IL_0030:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>>.Target""
  IL_0035:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__1.<>p__0""
  IL_003a:  ldarg.0
  IL_003b:  ldfld      ""dynamic C.d""
  IL_0040:  callvirt   ""int System.Func<System.Runtime.CompilerServices.CallSite, object, int>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0045:  pop
  IL_0046:  ret
}
");
        }

        [Fact]
        public void Conversion_Implicit()
        {
            string source = @"
public class C
{
    public int M(dynamic d)
    {
        return d;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       65 (0x41)
  .maxstack  3
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_002b
  IL_0007:  ldc.i4.0
  IL_0008:  ldtoken    ""int""
  IL_000d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0021:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0026:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__0""
  IL_002b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__0""
  IL_0030:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>>.Target""
  IL_0035:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__0""
  IL_003a:  ldarg.1
  IL_003b:  callvirt   ""int System.Func<System.Runtime.CompilerServices.CallSite, object, int>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0040:  ret
}
");
        }

        [Fact]
        public void Conversion_Explicit()
        {
            string source = @"
public class C
{
    public int M(dynamic d)
    {
        return (int)d;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       66 (0x42)
  .maxstack  3
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_002c
  IL_0007:  ldc.i4.s   16
  IL_0009:  ldtoken    ""int""
  IL_000e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0013:  ldtoken    ""C""
  IL_0018:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0022:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0027:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__0""
  IL_002c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__0""
  IL_0031:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>>.Target""
  IL_0036:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__0""
  IL_003b:  ldarg.1
  IL_003c:  callvirt   ""int System.Func<System.Runtime.CompilerServices.CallSite, object, int>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0041:  ret
}");
        }

        [Fact]
        public void Conversion_Implicit_Checked()
        {
            string source = @"
public class C
{
    public int M(dynamic d)
    {
        checked { return d; }
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       65 (0x41)
  .maxstack  3
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_002b
  IL_0007:  ldc.i4.1
  IL_0008:  ldtoken    ""int""
  IL_000d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0021:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0026:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__0""
  IL_002b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__0""
  IL_0030:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>>.Target""
  IL_0035:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__0""
  IL_003a:  ldarg.1
  IL_003b:  callvirt   ""int System.Func<System.Runtime.CompilerServices.CallSite, object, int>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0040:  ret
}");
        }

        [Fact]
        public void Conversion_Explicit_Checked()
        {
            string source = @"
public class C
{
    public int M(dynamic d)
    {
        checked { return (int)d; }
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       66 (0x42)
  .maxstack  3
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_002c
  IL_0007:  ldc.i4.s   17
  IL_0009:  ldtoken    ""int""
  IL_000e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0013:  ldtoken    ""C""
  IL_0018:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0022:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0027:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__0""
  IL_002c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__0""
  IL_0031:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>>.Target""
  IL_0036:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__0""
  IL_003b:  ldarg.1
  IL_003c:  callvirt   ""int System.Func<System.Runtime.CompilerServices.CallSite, object, int>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0041:  ret
}");
        }

        [Fact]
        public void Conversion_Implicit_Reference_Return()
        {
            string source = @"
class D { }

class C
{
    public D M(dynamic d) 
    {
        return d;
    }   
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       65 (0x41)
  .maxstack  3
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, D>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_002b
  IL_0007:  ldc.i4.0
  IL_0008:  ldtoken    ""D""
  IL_000d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0021:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, D>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, D>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0026:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, D>> C.<>o__0.<>p__0""
  IL_002b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, D>> C.<>o__0.<>p__0""
  IL_0030:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, D> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, D>>.Target""
  IL_0035:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, D>> C.<>o__0.<>p__0""
  IL_003a:  ldarg.1
  IL_003b:  callvirt   ""D System.Func<System.Runtime.CompilerServices.CallSite, object, D>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0040:  ret
}
");
        }

        [Fact]
        public void Conversion_Implicit_Reference_Assignment()
        {
            string source = @"
class D { }

class C
{
    D x;

    public void M(dynamic d) 
    {
        x = d;        
    }   
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       71 (0x47)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, D>> C.<>o__1.<>p__0""
  IL_0006:  brtrue.s   IL_002c
  IL_0008:  ldc.i4.0
  IL_0009:  ldtoken    ""D""
  IL_000e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0013:  ldtoken    ""C""
  IL_0018:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0022:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, D>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, D>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0027:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, D>> C.<>o__1.<>p__0""
  IL_002c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, D>> C.<>o__1.<>p__0""
  IL_0031:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, D> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, D>>.Target""
  IL_0036:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, D>> C.<>o__1.<>p__0""
  IL_003b:  ldarg.1
  IL_003c:  callvirt   ""D System.Func<System.Runtime.CompilerServices.CallSite, object, D>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0041:  stfld      ""D C.x""
  IL_0046:  ret
}
");
        }

        [Fact]
        public void Conversion_Explicit_Reference()
        {
            string source = @"
class D { }

class C
{
    public D M(dynamic d) 
    {
        return (D)d;
    }   
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       66 (0x42)
  .maxstack  3
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, D>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_002c
  IL_0007:  ldc.i4.s   16
  IL_0009:  ldtoken    ""D""
  IL_000e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0013:  ldtoken    ""C""
  IL_0018:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0022:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, D>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, D>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0027:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, D>> C.<>o__0.<>p__0""
  IL_002c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, D>> C.<>o__0.<>p__0""
  IL_0031:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, D> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, D>>.Target""
  IL_0036:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, D>> C.<>o__0.<>p__0""
  IL_003b:  ldarg.1
  IL_003c:  callvirt   ""D System.Func<System.Runtime.CompilerServices.CallSite, object, D>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0041:  ret
}
");
        }

        [Fact]
        public void Conversion_ArrayIndex()
        {
            string source = @"
public class C
{
    public object M(object[] a, dynamic d)
    {
        return a[d];
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       68 (0x44)
  .maxstack  4
  IL_0000:  ldarg.1
  IL_0001:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__0""
  IL_0006:  brtrue.s   IL_002d
  IL_0008:  ldc.i4.s   32
  IL_000a:  ldtoken    ""int""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldtoken    ""C""
  IL_0019:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0023:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0028:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__0""
  IL_002d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__0""
  IL_0032:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>>.Target""
  IL_0037:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__0""
  IL_003c:  ldarg.2
  IL_003d:  callvirt   ""int System.Func<System.Runtime.CompilerServices.CallSite, object, int>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0042:  ldelem.ref
  IL_0043:  ret
}
");
        }

        [Fact]
        public void Conversion_ArrayIndex_Checked()
        {
            string source = @"
public class C
{
    public object M(object[] a, dynamic d)
    {
        return checked(a[d]);
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       68 (0x44)
  .maxstack  4
  IL_0000:  ldarg.1
  IL_0001:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__0""
  IL_0006:  brtrue.s   IL_002d
  IL_0008:  ldc.i4.s   33
  IL_000a:  ldtoken    ""int""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldtoken    ""C""
  IL_0019:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0023:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0028:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__0""
  IL_002d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__0""
  IL_0032:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>>.Target""
  IL_0037:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__0""
  IL_003c:  ldarg.2
  IL_003d:  callvirt   ""int System.Func<System.Runtime.CompilerServices.CallSite, object, int>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0042:  ldelem.ref
  IL_0043:  ret
}");
        }

        [Fact]
        public void Conversion_ArrayIndex_Explicit()
        {
            string source = @"
public class C
{
    public object M(object[] a, dynamic d)
    {
        return a[(int)d];
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       68 (0x44)
  .maxstack  4
  IL_0000:  ldarg.1
  IL_0001:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__0""
  IL_0006:  brtrue.s   IL_002d
  IL_0008:  ldc.i4.s   16
  IL_000a:  ldtoken    ""int""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldtoken    ""C""
  IL_0019:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0023:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0028:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__0""
  IL_002d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__0""
  IL_0032:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>>.Target""
  IL_0037:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__0""
  IL_003c:  ldarg.2
  IL_003d:  callvirt   ""int System.Func<System.Runtime.CompilerServices.CallSite, object, int>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0042:  ldelem.ref
  IL_0043:  ret
}");
        }

        [Fact]
        public void IdentityConversion1()
        {
            string source = @"
public class C
{
    public object M(dynamic d)
    {
        return d;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ret
}
");
        }

        [Fact]
        public void IdentityConversion2()
        {
            string source = @"
public class C
{
    public static void M()
    {
        dynamic d = new object();
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  newobj     ""object..ctor()""
  IL_0005:  pop
  IL_0006:  ret
}
");
        }

        [Fact]
        public void IdentityConversion3()
        {
            string source = @"
public class C
{
    public dynamic Null()
    {
        return null;
    }
}";

            CompileAndVerifyIL(source, "C.Null", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  ret
}
");
        }

        [Fact]
        public void Boxing_ReturnValue()
        {
            string source = @"
public class C
{
    public dynamic Int32()
    {
        return 1;
    }
}";

            CompileAndVerifyIL(source, "C.Int32", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  box        ""int""
  IL_0006:  ret
}
");
        }

        [Fact]
        public void Boxing_AssignmentToDynamicField1()
        {
            string source = @"
class C
{
    dynamic d;

    void M()
    {
        d = true;
    }
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  box        ""bool""
  IL_0007:  stfld      ""dynamic C.d""
  IL_000c:  ret
}
");
        }

        [Fact]
        public void Boxing_AssignmentToDynamicField2()
        {
            string source = @"
class C
{
    dynamic d;

    void M()
    {
        new C { d = true };
    }
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  ldc.i4.1
  IL_0006:  box        ""bool""
  IL_000b:  stfld      ""dynamic C.d""
  IL_0010:  ret
}
");
        }

        [Fact]
        public void Boxing_AssignmentToDynamicIndex()
        {
            string source = @"
class C
{
    dynamic this[int i] { get { return 1; } set { } }

    void M()
    {
        this[1] = true;
    }
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       14 (0xe)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  ldc.i4.1
  IL_0003:  box        ""bool""
  IL_0008:  call       ""void C.this[int].set""
  IL_000d:  ret
}
");
        }

        [Fact]
        public void Boxing_AssignmentToProperty1()
        {
            string source = @"
class C
{
    dynamic d { get; set; }

    void M()
    {
        this.d = true;
    }
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  box        ""bool""
  IL_0007:  call       ""void C.d.set""
  IL_000c:  ret
}
");
        }

        [Fact]
        public void Boxing_AssignmentToProperty2()
        {
            string source = @"
class C
{
    dynamic d { get; set; }

    void M()
    {
        new C { d = true };
    }
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  ldc.i4.1
  IL_0006:  box        ""bool""
  IL_000b:  callvirt   ""void C.d.set""
  IL_0010:  ret
}
");
        }

        [Fact]
        public void IsAs()
        {
            string source = @"
using System.Collections.Generic;

public class C
{
    public bool IsObject(dynamic d)
    {
        return d is List<object>;
    }

    public bool IsDynamic(dynamic d)
    {
        return d is List<dynamic>;
    }

    public List<dynamic> As(dynamic d)
    {
        return d as List<dynamic>;
    }
}";
            // TODO: Why does RefEmit use fat header with maxstack = 2?
            var verifier = CompileAndVerify(source, additionalRefs: new[] { SystemCoreRef, CSharpRef }, symbolValidator: module =>
            {
                var pe = (PEModuleSymbol)module;

                // all occurrences of List<dynamic> and List<object> should be unified to a single TypeSpec:
                Assert.Equal(1, pe.Module.GetMetadataReader().GetTableRowCount(TableIndex.TypeSpec));
            });

            verifier.VerifyIL("C.IsObject", @"
{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  isinst     ""System.Collections.Generic.List<object>""
  IL_0006:  ldnull
  IL_0007:  cgt.un
  IL_0009:  ret
}
", realIL: true);

            verifier.VerifyIL("C.IsDynamic", @"
{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  isinst     ""System.Collections.Generic.List<object>""
  IL_0006:  ldnull
  IL_0007:  cgt.un
  IL_0009:  ret
}
", realIL: true);

            verifier.VerifyIL("C.As", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  isinst     ""System.Collections.Generic.List<object>""
  IL_0006:  ret
}
", realIL: true);
        }

        [Fact]
        public void DelegateCreation()
        {
            string source = @"
using System;

public class C
{
    dynamic d = null;
    Action a;	    

    public virtual void M()
    {
        a = new System.Action(d);
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       87 (0x57)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action>> C.<>o__2.<>p__0""
  IL_0006:  brtrue.s   IL_002c
  IL_0008:  ldc.i4.0
  IL_0009:  ldtoken    ""System.Action""
  IL_000e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0013:  ldtoken    ""C""
  IL_0018:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0022:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0027:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action>> C.<>o__2.<>p__0""
  IL_002c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action>> C.<>o__2.<>p__0""
  IL_0031:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action>>.Target""
  IL_0036:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action>> C.<>o__2.<>p__0""
  IL_003b:  ldarg.0
  IL_003c:  ldfld      ""dynamic C.d""
  IL_0041:  callvirt   ""System.Action System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0046:  ldftn      ""void System.Action.Invoke()""
  IL_004c:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0051:  stfld      ""System.Action C.a""
  IL_0056:  ret
}
");
        }

        #endregion

        #region Operators

        [Fact]
        public void Multiplication_Dynamic_Dynamic()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return d * d;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       84 (0x54)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_003d
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   26
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.2
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  dup
  IL_0025:  ldc.i4.1
  IL_0026:  ldc.i4.0
  IL_0027:  ldnull
  IL_0028:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002d:  stelem.ref
  IL_002e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0033:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0038:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0042:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_004c:  ldarg.1
  IL_004d:  ldarg.1
  IL_004e:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0053:  ret
}
");
        }

        [Fact]
        public void Multiplication_Dynamic_Static()
        {
            string source = @"
public class C
{
    public dynamic M(C c, dynamic d)
    {
        return c * d;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       84 (0x54)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_003d
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   26
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.2
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.1
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  dup
  IL_0025:  ldc.i4.1
  IL_0026:  ldc.i4.0
  IL_0027:  ldnull
  IL_0028:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002d:  stelem.ref
  IL_002e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0033:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0038:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, object>> C.<>o__0.<>p__0""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, object>> C.<>o__0.<>p__0""
  IL_0042:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, C, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, object>>.Target""
  IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, object>> C.<>o__0.<>p__0""
  IL_004c:  ldarg.1
  IL_004d:  ldarg.2
  IL_004e:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, C, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, C, object)""
  IL_0053:  ret
}
");
        }

        [Fact]
        public void Multiplication_Dynamic_Literal()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return d * 1;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       84 (0x54)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_003d
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   26
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.2
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  dup
  IL_0025:  ldc.i4.1
  IL_0026:  ldc.i4.3
  IL_0027:  ldnull
  IL_0028:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002d:  stelem.ref
  IL_002e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0033:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0038:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__0.<>p__0""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__0.<>p__0""
  IL_0042:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>>.Target""
  IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__0.<>p__0""
  IL_004c:  ldarg.1
  IL_004d:  ldc.i4.1
  IL_004e:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, int)""
  IL_0053:  ret
}
");
        }

        [Fact]
        public void Multiplication_Checked()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return checked(d * d);
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       84 (0x54)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_003d
  IL_0007:  ldc.i4.1
  IL_0008:  ldc.i4.s   26
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.2
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  dup
  IL_0025:  ldc.i4.1
  IL_0026:  ldc.i4.0
  IL_0027:  ldnull
  IL_0028:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002d:  stelem.ref
  IL_002e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0033:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0038:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0042:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_004c:  ldarg.1
  IL_004d:  ldarg.1
  IL_004e:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0053:  ret
}
");
        }

        [Fact]
        public void Division()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return d / d;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       84 (0x54)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_003d
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   12
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.2
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  dup
  IL_0025:  ldc.i4.1
  IL_0026:  ldc.i4.0
  IL_0027:  ldnull
  IL_0028:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002d:  stelem.ref
  IL_002e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0033:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0038:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0042:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_004c:  ldarg.1
  IL_004d:  ldarg.1
  IL_004e:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0053:  ret
}
");
        }

        [Fact]
        public void Remainder()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return d % d;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       84 (0x54)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_003d
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   25
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.2
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  dup
  IL_0025:  ldc.i4.1
  IL_0026:  ldc.i4.0
  IL_0027:  ldnull
  IL_0028:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002d:  stelem.ref
  IL_002e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0033:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0038:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0042:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_004c:  ldarg.1
  IL_004d:  ldarg.1
  IL_004e:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0053:  ret
}
");
        }

        [Fact]
        public void LeftShift()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return d << d;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       84 (0x54)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_003d
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   19
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.2
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  dup
  IL_0025:  ldc.i4.1
  IL_0026:  ldc.i4.0
  IL_0027:  ldnull
  IL_0028:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002d:  stelem.ref
  IL_002e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0033:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0038:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0042:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_004c:  ldarg.1
  IL_004d:  ldarg.1
  IL_004e:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0053:  ret
}
");
        }

        [Fact]
        public void RightShift()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return d >> d;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       84 (0x54)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_003d
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   41
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.2
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  dup
  IL_0025:  ldc.i4.1
  IL_0026:  ldc.i4.0
  IL_0027:  ldnull
  IL_0028:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002d:  stelem.ref
  IL_002e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0033:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0038:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0042:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_004c:  ldarg.1
  IL_004d:  ldarg.1
  IL_004e:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0053:  ret
}
");
        }

        [Fact]
        public void And()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return d & d;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       83 (0x53)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_003c
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.2
  IL_0009:  ldtoken    ""C""
  IL_000e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0013:  ldc.i4.2
  IL_0014:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0019:  dup
  IL_001a:  ldc.i4.0
  IL_001b:  ldc.i4.0
  IL_001c:  ldnull
  IL_001d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0022:  stelem.ref
  IL_0023:  dup
  IL_0024:  ldc.i4.1
  IL_0025:  ldc.i4.0
  IL_0026:  ldnull
  IL_0027:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002c:  stelem.ref
  IL_002d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0032:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0037:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_003c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0041:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0046:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_004b:  ldarg.1
  IL_004c:  ldarg.1
  IL_004d:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0052:  ret
}
");
        }

        [Fact]
        public void Or()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return d | d;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       84 (0x54)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_003d
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   36
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.2
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  dup
  IL_0025:  ldc.i4.1
  IL_0026:  ldc.i4.0
  IL_0027:  ldnull
  IL_0028:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002d:  stelem.ref
  IL_002e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0033:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0038:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0042:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_004c:  ldarg.1
  IL_004d:  ldarg.1
  IL_004e:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0053:  ret
}
");
        }

        [Fact]
        public void Xor()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return d ^ d;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       84 (0x54)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_003d
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   14
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.2
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  dup
  IL_0025:  ldc.i4.1
  IL_0026:  ldc.i4.0
  IL_0027:  ldnull
  IL_0028:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002d:  stelem.ref
  IL_002e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0033:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0038:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0042:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_004c:  ldarg.1
  IL_004d:  ldarg.1
  IL_004e:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0053:  ret
}
");
        }

        [Fact]
        public void Equal()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d1, dynamic d2)
    {
        return d1 == d2;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       84 (0x54)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_003d
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   13
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.2
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  dup
  IL_0025:  ldc.i4.1
  IL_0026:  ldc.i4.0
  IL_0027:  ldnull
  IL_0028:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002d:  stelem.ref
  IL_002e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0033:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0038:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0042:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_004c:  ldarg.1
  IL_004d:  ldarg.2
  IL_004e:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0053:  ret
}");
        }

        [Fact]
        public void NotEqual()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d1, dynamic d2)
    {
        return d1 != d2;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       84 (0x54)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_003d
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   35
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.2
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  dup
  IL_0025:  ldc.i4.1
  IL_0026:  ldc.i4.0
  IL_0027:  ldnull
  IL_0028:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002d:  stelem.ref
  IL_002e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0033:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0038:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0042:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_004c:  ldarg.1
  IL_004d:  ldarg.2
  IL_004e:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0053:  ret
}");
        }

        [Fact]
        public void LessThan()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d1, dynamic d2)
    {
        return d1 < d2;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       84 (0x54)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_003d
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   20
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.2
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  dup
  IL_0025:  ldc.i4.1
  IL_0026:  ldc.i4.0
  IL_0027:  ldnull
  IL_0028:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002d:  stelem.ref
  IL_002e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0033:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0038:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0042:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_004c:  ldarg.1
  IL_004d:  ldarg.2
  IL_004e:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0053:  ret
}");
        }

        [Fact]
        public void GreaterThan()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d1, dynamic d2)
    {
        return d1 > d2;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       84 (0x54)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_003d
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   15
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.2
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  dup
  IL_0025:  ldc.i4.1
  IL_0026:  ldc.i4.0
  IL_0027:  ldnull
  IL_0028:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002d:  stelem.ref
  IL_002e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0033:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0038:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0042:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_004c:  ldarg.1
  IL_004d:  ldarg.2
  IL_004e:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0053:  ret
}");
        }

        [Fact]
        public void LessThanEquals()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d1, dynamic d2)
    {
        return d1 <= d2;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       84 (0x54)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_003d
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   21
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.2
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  dup
  IL_0025:  ldc.i4.1
  IL_0026:  ldc.i4.0
  IL_0027:  ldnull
  IL_0028:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002d:  stelem.ref
  IL_002e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0033:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0038:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0042:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_004c:  ldarg.1
  IL_004d:  ldarg.2
  IL_004e:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0053:  ret
}");
        }

        [Fact]
        public void GreaterThanEquals()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d1, dynamic d2)
    {
        return d1 >= d2;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       84 (0x54)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_003d
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   16
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.2
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  dup
  IL_0025:  ldc.i4.1
  IL_0026:  ldc.i4.0
  IL_0027:  ldnull
  IL_0028:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002d:  stelem.ref
  IL_002e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0033:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0038:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0042:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_004c:  ldarg.1
  IL_004d:  ldarg.2
  IL_004e:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0053:  ret
}");
        }

        [Fact]
        public void UnaryPlus()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return +d;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       73 (0x49)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0033
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   29
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.1
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0029:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_002e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0033:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0038:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0042:  ldarg.1
  IL_0043:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0048:  ret
}
");
        }

        [Fact]
        public void UnaryMinus()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return -d;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       73 (0x49)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0033
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   28
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.1
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0029:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_002e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0033:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0038:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0042:  ldarg.1
  IL_0043:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0048:  ret
}");
        }

        [Fact]
        public void BitwiseComplement()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return ~d;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       73 (0x49)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0033
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   82
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.1
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0029:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_002e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0033:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0038:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0042:  ldarg.1
  IL_0043:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0048:  ret
}
");
        }

        [Fact]
        public void BooleanOperation_IsTrue()
        {
            string source = @"
class C
{
    public static int M()
    {
        dynamic dy = null;
        if (dy.Property_bool)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
}";
            var verifier = CompileAndVerifyIL(source, "C.M", expectedUnoptimizedIL: @"
{
  // Code size      169 (0xa9)
  .maxstack  10
  .locals init (object V_0, //dy
                bool V_1,
                int V_2)
 -IL_0000:  nop
 -IL_0001:  ldnull
  IL_0002:  stloc.0
 -IL_0003:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_0008:  brfalse.s  IL_000c
  IL_000a:  br.s       IL_0038
  IL_000c:  ldc.i4.0
  IL_000d:  ldc.i4.s   83
  IL_000f:  ldtoken    ""C""
  IL_0014:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0019:  ldc.i4.1
  IL_001a:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001f:  dup
  IL_0020:  ldc.i4.0
  IL_0021:  ldc.i4.0
  IL_0022:  ldnull
  IL_0023:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0028:  stelem.ref
  IL_0029:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_002e:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0033:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_0038:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_003d:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Target""
  IL_0042:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_004c:  brfalse.s  IL_0050
  IL_004e:  br.s       IL_007f
  IL_0050:  ldc.i4.0
  IL_0051:  ldstr      ""Property_bool""
  IL_0056:  ldtoken    ""C""
  IL_005b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0060:  ldc.i4.1
  IL_0061:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0066:  dup
  IL_0067:  ldc.i4.0
  IL_0068:  ldc.i4.0
  IL_0069:  ldnull
  IL_006a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_006f:  stelem.ref
  IL_0070:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0075:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_007a:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_007f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0084:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_0089:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_008e:  ldloc.0
  IL_008f:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0094:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, object, bool>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0099:  stloc.1
 ~IL_009a:  ldloc.1
  IL_009b:  brfalse.s  IL_00a2
 -IL_009d:  nop
 -IL_009e:  ldc.i4.1
  IL_009f:  stloc.2
  IL_00a0:  br.s       IL_00a7
 -IL_00a2:  nop
 -IL_00a3:  ldc.i4.0
  IL_00a4:  stloc.2
  IL_00a5:  br.s       IL_00a7
 -IL_00a7:  ldloc.2
  IL_00a8:  ret
}
",
 expectedOptimizedIL: @"
{
  // Code size      154 (0x9a)
  .maxstack  10
  .locals init (object V_0) //dy
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_0007:  brtrue.s   IL_0035
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.s   83
  IL_000c:  ldtoken    ""C""
  IL_0011:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0016:  ldc.i4.1
  IL_0017:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001c:  dup
  IL_001d:  ldc.i4.0
  IL_001e:  ldc.i4.0
  IL_001f:  ldnull
  IL_0020:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0025:  stelem.ref
  IL_0026:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_002b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0030:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_0035:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_003a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Target""
  IL_003f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_0044:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0049:  brtrue.s   IL_007a
  IL_004b:  ldc.i4.0
  IL_004c:  ldstr      ""Property_bool""
  IL_0051:  ldtoken    ""C""
  IL_0056:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005b:  ldc.i4.1
  IL_005c:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0061:  dup
  IL_0062:  ldc.i4.0
  IL_0063:  ldc.i4.0
  IL_0064:  ldnull
  IL_0065:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_006a:  stelem.ref
  IL_006b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0070:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0075:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_007a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_007f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_0084:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0089:  ldloc.0
  IL_008a:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_008f:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, object, bool>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0094:  brfalse.s  IL_0098
  IL_0096:  ldc.i4.1
  IL_0097:  ret
  IL_0098:  ldc.i4.0
  IL_0099:  ret
}
");
        }

        [Fact]
        public void BooleanOperation_Not()
        {
            string source = @"
public class C
{
    public static int M(dynamic d)
    {
        if (!d)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      149 (0x95)
  .maxstack  10
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_0005:  brtrue.s   IL_0033
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   83
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.1
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0029:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_002e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_0033:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_0038:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Target""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_0042:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0047:  brtrue.s   IL_0075
  IL_0049:  ldc.i4.0
  IL_004a:  ldc.i4.s   34
  IL_004c:  ldtoken    ""C""
  IL_0051:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0056:  ldc.i4.1
  IL_0057:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_005c:  dup
  IL_005d:  ldc.i4.0
  IL_005e:  ldc.i4.0
  IL_005f:  ldnull
  IL_0060:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0065:  stelem.ref
  IL_0066:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_006b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0070:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0075:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_007a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_007f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0084:  ldarg.0
  IL_0085:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_008a:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, object, bool>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_008f:  brfalse.s  IL_0093
  IL_0091:  ldc.i4.1
  IL_0092:  ret
  IL_0093:  ldc.i4.0
  IL_0094:  ret
}
");
        }

        [Fact]
        public void BooleanOperation_And()
        {
            string source = @"
public class C
{
    public static int M(dynamic d1, dynamic d2)
    {
        if (d1 && d2)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      236 (0xec)
  .maxstack  10
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__2""
  IL_0005:  brtrue.s   IL_0033
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   83
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.1
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0029:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_002e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__2""
  IL_0033:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__2""
  IL_0038:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Target""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__2""
  IL_0042:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_0047:  brtrue.s   IL_0075
  IL_0049:  ldc.i4.0
  IL_004a:  ldc.i4.s   84
  IL_004c:  ldtoken    ""C""
  IL_0051:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0056:  ldc.i4.1
  IL_0057:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_005c:  dup
  IL_005d:  ldc.i4.0
  IL_005e:  ldc.i4.0
  IL_005f:  ldnull
  IL_0060:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0065:  stelem.ref
  IL_0066:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_006b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0070:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_0075:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_007a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Target""
  IL_007f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_0084:  ldarg.0
  IL_0085:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, object, bool>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_008a:  brtrue.s   IL_00e0
  IL_008c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0091:  brtrue.s   IL_00c8
  IL_0093:  ldc.i4.8
  IL_0094:  ldc.i4.2
  IL_0095:  ldtoken    ""C""
  IL_009a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_009f:  ldc.i4.2
  IL_00a0:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00a5:  dup
  IL_00a6:  ldc.i4.0
  IL_00a7:  ldc.i4.0
  IL_00a8:  ldnull
  IL_00a9:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00ae:  stelem.ref
  IL_00af:  dup
  IL_00b0:  ldc.i4.1
  IL_00b1:  ldc.i4.0
  IL_00b2:  ldnull
  IL_00b3:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00b8:  stelem.ref
  IL_00b9:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00be:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00c3:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_00c8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_00cd:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_00d2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_00d7:  ldarg.0
  IL_00d8:  ldarg.1
  IL_00d9:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_00de:  br.s       IL_00e1
  IL_00e0:  ldarg.0
  IL_00e1:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, object, bool>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_00e6:  brfalse.s  IL_00ea
  IL_00e8:  ldc.i4.1
  IL_00e9:  ret
  IL_00ea:  ldc.i4.0
  IL_00eb:  ret
}");
        }

        [WorkItem(547676, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547676")]
        [Fact]
        public void BooleanOperation_Bug547676()
        {
            string source = @"
public class C
{
    public static int M(dynamic d1, dynamic d2)
    {
        if ((d1 == 1) && d2 && d2)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      480 (0x1e0)
  .maxstack  10
  .locals init (object V_0,
  object V_1)
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__5""
  IL_0005:  brtrue.s   IL_0033
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   83
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.1
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0029:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_002e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__5""
  IL_0033:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__5""
  IL_0038:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Target""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__5""
  IL_0042:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__0.<>p__0""
  IL_0047:  brtrue.s   IL_007f
  IL_0049:  ldc.i4.0
  IL_004a:  ldc.i4.s   13
  IL_004c:  ldtoken    ""C""
  IL_0051:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0056:  ldc.i4.2
  IL_0057:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_005c:  dup
  IL_005d:  ldc.i4.0
  IL_005e:  ldc.i4.0
  IL_005f:  ldnull
  IL_0060:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0065:  stelem.ref
  IL_0066:  dup
  IL_0067:  ldc.i4.1
  IL_0068:  ldc.i4.3
  IL_0069:  ldnull
  IL_006a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_006f:  stelem.ref
  IL_0070:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0075:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_007a:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__0.<>p__0""
  IL_007f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__0.<>p__0""
  IL_0084:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>>.Target""
  IL_0089:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__0.<>p__0""
  IL_008e:  ldarg.0
  IL_008f:  ldc.i4.1
  IL_0090:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, int)""
  IL_0095:  stloc.1
  IL_0096:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__2""
  IL_009b:  brtrue.s   IL_00c9
  IL_009d:  ldc.i4.0
  IL_009e:  ldc.i4.s   84
  IL_00a0:  ldtoken    ""C""
  IL_00a5:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00aa:  ldc.i4.1
  IL_00ab:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00b0:  dup
  IL_00b1:  ldc.i4.0
  IL_00b2:  ldc.i4.0
  IL_00b3:  ldnull
  IL_00b4:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00b9:  stelem.ref
  IL_00ba:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00bf:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00c4:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__2""
  IL_00c9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__2""
  IL_00ce:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Target""
  IL_00d3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__2""
  IL_00d8:  ldloc.1
  IL_00d9:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, object, bool>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_00de:  brtrue.s   IL_0134
  IL_00e0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__1""
  IL_00e5:  brtrue.s   IL_011c
  IL_00e7:  ldc.i4.8
  IL_00e8:  ldc.i4.2
  IL_00e9:  ldtoken    ""C""
  IL_00ee:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00f3:  ldc.i4.2
  IL_00f4:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00f9:  dup
  IL_00fa:  ldc.i4.0
  IL_00fb:  ldc.i4.0
  IL_00fc:  ldnull
  IL_00fd:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0102:  stelem.ref
  IL_0103:  dup
  IL_0104:  ldc.i4.1
  IL_0105:  ldc.i4.0
  IL_0106:  ldnull
  IL_0107:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_010c:  stelem.ref
  IL_010d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0112:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0117:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__1""
  IL_011c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__1""
  IL_0121:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0126:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__1""
  IL_012b:  ldloc.1
  IL_012c:  ldarg.1
  IL_012d:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0132:  br.s       IL_0135
  IL_0134:  ldloc.1
  IL_0135:  stloc.0
  IL_0136:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__4""
  IL_013b:  brtrue.s   IL_0169
  IL_013d:  ldc.i4.0
  IL_013e:  ldc.i4.s   84
  IL_0140:  ldtoken    ""C""
  IL_0145:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_014a:  ldc.i4.1
  IL_014b:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0150:  dup
  IL_0151:  ldc.i4.0
  IL_0152:  ldc.i4.0
  IL_0153:  ldnull
  IL_0154:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0159:  stelem.ref
  IL_015a:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_015f:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0164:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__4""
  IL_0169:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__4""
  IL_016e:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Target""
  IL_0173:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__4""
  IL_0178:  ldloc.0
  IL_0179:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, object, bool>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_017e:  brtrue.s   IL_01d4
  IL_0180:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__3""
  IL_0185:  brtrue.s   IL_01bc
  IL_0187:  ldc.i4.8
  IL_0188:  ldc.i4.2
  IL_0189:  ldtoken    ""C""
  IL_018e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0193:  ldc.i4.2
  IL_0194:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0199:  dup
  IL_019a:  ldc.i4.0
  IL_019b:  ldc.i4.0
  IL_019c:  ldnull
  IL_019d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_01a2:  stelem.ref
  IL_01a3:  dup
  IL_01a4:  ldc.i4.1
  IL_01a5:  ldc.i4.0
  IL_01a6:  ldnull
  IL_01a7:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_01ac:  stelem.ref
  IL_01ad:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_01b2:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_01b7:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__3""
  IL_01bc:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__3""
  IL_01c1:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_01c6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__3""
  IL_01cb:  ldloc.0
  IL_01cc:  ldarg.1
  IL_01cd:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_01d2:  br.s       IL_01d5
  IL_01d4:  ldloc.0
  IL_01d5:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, object, bool>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_01da:  brfalse.s  IL_01de
  IL_01dc:  ldc.i4.1
  IL_01dd:  ret
  IL_01de:  ldc.i4.0
  IL_01df:  ret
}");
        }

        [Fact]
        public void BooleanOperation_Or_Dynamic_Dynamic()
        {
            string source = @"
public class C
{
    public static int M(dynamic d1, dynamic d2)
    {
        if (d1 || d2)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
}";
            // Dev11 emits less efficient code:
            //   IsTrue(IsTrue(d1) ? d1 : Or(d1, d2)) 
            //
            // Roslyn optimizes away the second dynamic call IsTrue on d1:
            //   IsTrue(d1) || IsTrue(Or(d1, d2))

            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      237 (0xed)
  .maxstack  10
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__2""
  IL_0005:  brtrue.s   IL_0033
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   83
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.1
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0029:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_002e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__2""
  IL_0033:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__2""
  IL_0038:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Target""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__2""
  IL_0042:  ldarg.0
  IL_0043:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, object, bool>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0048:  brtrue     IL_00e9
  IL_004d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_0052:  brtrue.s   IL_0080
  IL_0054:  ldc.i4.0
  IL_0055:  ldc.i4.s   83
  IL_0057:  ldtoken    ""C""
  IL_005c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0061:  ldc.i4.1
  IL_0062:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0067:  dup
  IL_0068:  ldc.i4.0
  IL_0069:  ldc.i4.0
  IL_006a:  ldnull
  IL_006b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0070:  stelem.ref
  IL_0071:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0076:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_007b:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_0080:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_0085:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Target""
  IL_008a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_008f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0094:  brtrue.s   IL_00cc
  IL_0096:  ldc.i4.8
  IL_0097:  ldc.i4.s   36
  IL_0099:  ldtoken    ""C""
  IL_009e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00a3:  ldc.i4.2
  IL_00a4:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00a9:  dup
  IL_00aa:  ldc.i4.0
  IL_00ab:  ldc.i4.0
  IL_00ac:  ldnull
  IL_00ad:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00b2:  stelem.ref
  IL_00b3:  dup
  IL_00b4:  ldc.i4.1
  IL_00b5:  ldc.i4.0
  IL_00b6:  ldnull
  IL_00b7:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00bc:  stelem.ref
  IL_00bd:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00c2:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00c7:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_00cc:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_00d1:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_00d6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_00db:  ldarg.0
  IL_00dc:  ldarg.1
  IL_00dd:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_00e2:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, object, bool>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_00e7:  brfalse.s  IL_00eb
  IL_00e9:  ldc.i4.1
  IL_00ea:  ret
  IL_00eb:  ldc.i4.0
  IL_00ec:  ret
}
");
        }

        [Fact]
        public void BooleanOperation_Or_Static_Dynamic()
        {
            string source = @"
public class C
{
    public static int M(bool b, dynamic d)
    {
        if (b || d)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      166 (0xa6)
  .maxstack  10
  IL_0000:  ldarg.0
  IL_0001:  brtrue     IL_00a2
  IL_0006:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_000b:  brtrue.s   IL_0039
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.s   83
  IL_0010:  ldtoken    ""C""
  IL_0015:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001a:  ldc.i4.1
  IL_001b:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0020:  dup
  IL_0021:  ldc.i4.0
  IL_0022:  ldc.i4.0
  IL_0023:  ldnull
  IL_0024:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0029:  stelem.ref
  IL_002a:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_002f:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0034:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_0039:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_003e:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Target""
  IL_0043:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_0048:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> C.<>o__0.<>p__0""
  IL_004d:  brtrue.s   IL_0085
  IL_004f:  ldc.i4.8
  IL_0050:  ldc.i4.s   36
  IL_0052:  ldtoken    ""C""
  IL_0057:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005c:  ldc.i4.2
  IL_005d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0062:  dup
  IL_0063:  ldc.i4.0
  IL_0064:  ldc.i4.1
  IL_0065:  ldnull
  IL_0066:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_006b:  stelem.ref
  IL_006c:  dup
  IL_006d:  ldc.i4.1
  IL_006e:  ldc.i4.0
  IL_006f:  ldnull
  IL_0070:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0075:  stelem.ref
  IL_0076:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_007b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0080:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> C.<>o__0.<>p__0""
  IL_0085:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> C.<>o__0.<>p__0""
  IL_008a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>>.Target""
  IL_008f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> C.<>o__0.<>p__0""
  IL_0094:  ldarg.0
  IL_0095:  ldarg.1
  IL_0096:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, bool, object)""
  IL_009b:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, object, bool>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_00a0:  brfalse.s  IL_00a4
  IL_00a2:  ldc.i4.1
  IL_00a3:  ret
  IL_00a4:  ldc.i4.0
  IL_00a5:  ret
}");
        }

        [Fact]
        public void BooleanOperation_Or_ConstantOperand_False()
        {
            string source = @"
class C
{  
    int M()
    {
        dynamic d = null;
        return (false || d) ? 1 : 2;
    }
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      162 (0xa2)
  .maxstack  10
  .locals init (object V_0) //d
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_0007:  brtrue.s   IL_0035
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.s   83
  IL_000c:  ldtoken    ""C""
  IL_0011:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0016:  ldc.i4.1
  IL_0017:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001c:  dup
  IL_001d:  ldc.i4.0
  IL_001e:  ldc.i4.0
  IL_001f:  ldnull
  IL_0020:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0025:  stelem.ref
  IL_0026:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_002b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0030:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_0035:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_003a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Target""
  IL_003f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_0044:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> C.<>o__0.<>p__0""
  IL_0049:  brtrue.s   IL_0081
  IL_004b:  ldc.i4.8
  IL_004c:  ldc.i4.s   36
  IL_004e:  ldtoken    ""C""
  IL_0053:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0058:  ldc.i4.2
  IL_0059:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_005e:  dup
  IL_005f:  ldc.i4.0
  IL_0060:  ldc.i4.3
  IL_0061:  ldnull
  IL_0062:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0067:  stelem.ref
  IL_0068:  dup
  IL_0069:  ldc.i4.1
  IL_006a:  ldc.i4.0
  IL_006b:  ldnull
  IL_006c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0071:  stelem.ref
  IL_0072:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0077:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_007c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> C.<>o__0.<>p__0""
  IL_0081:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> C.<>o__0.<>p__0""
  IL_0086:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>>.Target""
  IL_008b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> C.<>o__0.<>p__0""
  IL_0090:  ldc.i4.0
  IL_0091:  ldloc.0
  IL_0092:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, bool, object)""
  IL_0097:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, object, bool>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_009c:  brtrue.s   IL_00a0
  IL_009e:  ldc.i4.2
  IL_009f:  ret
  IL_00a0:  ldc.i4.1
  IL_00a1:  ret
}
");
        }

        [Fact]
        public void BooleanOperation_Or_ConstantOperand_True()
        {
            string source = @"
class C
{  
    int M()
    {
        dynamic d = null;
        return (true || d) ? 1 : 2;
    }
}
";
            // Dev11 emits: 
            // IsTrue(IsTrue(true) ? true : Or(true, d)) ? 1 : 2
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  ret
}
");
        }

        [Fact]
        public void BooleanOperation_Add_ConstantOperand_False()
        {
            string source = @"
class C
{  
    int M()
    {
        dynamic d = null;
        return (false && d) ? 1 : 2;
    }
}
";
            // Dev11:  IsTrue(IsFalse(false) ? false : And(false, d)) ? 1 : 2
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.2
  IL_0001:  ret
}
");
        }

        [Fact]
        public void BooleanOperation_ConstantPropagation()
        {
            string source = @"
class C
{  
    int M()
    {
        dynamic d = null;
        return (!(false && d) || d) ? 1 : 2;
    }
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  ret
}
");
        }

        [Fact]
        public void BooleanOperation_ConstantPropagation2()
        {
            string source = @"
class C
{  
    int M()
    {
        dynamic d = null;
        return ((dynamic)false) ? 1 : 2;
    }
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.2
  IL_0001:  ret
}
");
        }

        [Fact]
        public void BooleanOperation_Add_ConstantOperand_True()
        {
            string source = @"
class C
{  
    int M()
    {
        dynamic d = null;
        return (true && d) ? 1 : 2;
    }
}
";
            // Dev11:  IsTrue(IsFalse(true) ? true : And(true, d)) ? 1 : 2
            // Roslyn: IsTrue(And(true, d)) ? 1 : 2 
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      161 (0xa1)
  .maxstack  10
  .locals init (object V_0) //d
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_0007:  brtrue.s   IL_0035
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.s   83
  IL_000c:  ldtoken    ""C""
  IL_0011:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0016:  ldc.i4.1
  IL_0017:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001c:  dup
  IL_001d:  ldc.i4.0
  IL_001e:  ldc.i4.0
  IL_001f:  ldnull
  IL_0020:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0025:  stelem.ref
  IL_0026:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_002b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0030:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_0035:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_003a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Target""
  IL_003f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__1""
  IL_0044:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> C.<>o__0.<>p__0""
  IL_0049:  brtrue.s   IL_0080
  IL_004b:  ldc.i4.8
  IL_004c:  ldc.i4.2
  IL_004d:  ldtoken    ""C""
  IL_0052:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0057:  ldc.i4.2
  IL_0058:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_005d:  dup
  IL_005e:  ldc.i4.0
  IL_005f:  ldc.i4.3
  IL_0060:  ldnull
  IL_0061:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0066:  stelem.ref
  IL_0067:  dup
  IL_0068:  ldc.i4.1
  IL_0069:  ldc.i4.0
  IL_006a:  ldnull
  IL_006b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0070:  stelem.ref
  IL_0071:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0076:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_007b:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> C.<>o__0.<>p__0""
  IL_0080:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> C.<>o__0.<>p__0""
  IL_0085:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>>.Target""
  IL_008a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> C.<>o__0.<>p__0""
  IL_008f:  ldc.i4.1
  IL_0090:  ldloc.0
  IL_0091:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, bool, object)""
  IL_0096:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, object, bool>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_009b:  brtrue.s   IL_009f
  IL_009d:  ldc.i4.2
  IL_009e:  ret
  IL_009f:  ldc.i4.1
  IL_00a0:  ret
}
");
        }

        [Fact]
        public void BooleanOperation_Or_BoxedOperand()
        {
            string source = @"
class C
{  
    bool b = false;

    dynamic M()
    {
        dynamic d = null;
        return b || d;
    }
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      103 (0x67)
  .maxstack  8
  .locals init (object V_0, //d
                bool V_1)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  ldfld      ""bool C.b""
  IL_0008:  stloc.1
  IL_0009:  ldloc.1
  IL_000a:  brtrue.s   IL_0060
  IL_000c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> C.<>o__1.<>p__0""
  IL_0011:  brtrue.s   IL_0049
  IL_0013:  ldc.i4.8
  IL_0014:  ldc.i4.s   36
  IL_0016:  ldtoken    ""C""
  IL_001b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0020:  ldc.i4.2
  IL_0021:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0026:  dup
  IL_0027:  ldc.i4.0
  IL_0028:  ldc.i4.1
  IL_0029:  ldnull
  IL_002a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002f:  stelem.ref
  IL_0030:  dup
  IL_0031:  ldc.i4.1
  IL_0032:  ldc.i4.0
  IL_0033:  ldnull
  IL_0034:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0039:  stelem.ref
  IL_003a:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003f:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0044:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> C.<>o__1.<>p__0""
  IL_0049:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> C.<>o__1.<>p__0""
  IL_004e:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>>.Target""
  IL_0053:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> C.<>o__1.<>p__0""
  IL_0058:  ldloc.1
  IL_0059:  ldloc.0
  IL_005a:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, bool, object)""
  IL_005f:  ret
  IL_0060:  ldloc.1
  IL_0061:  box        ""bool""
  IL_0066:  ret
}
");
        }

        [Fact]
        public void BooleanOperation_And_BoxedOperand()
        {
            string source = @"
class C
{  
    bool b = false;

    dynamic M()
    {
        dynamic d = null;
        return b && d;
    }
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      102 (0x66)
  .maxstack  8
  .locals init (object V_0, //d
                bool V_1)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  ldfld      ""bool C.b""
  IL_0008:  stloc.1
  IL_0009:  ldloc.1
  IL_000a:  brfalse.s  IL_005f
  IL_000c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> C.<>o__1.<>p__0""
  IL_0011:  brtrue.s   IL_0048
  IL_0013:  ldc.i4.8
  IL_0014:  ldc.i4.2
  IL_0015:  ldtoken    ""C""
  IL_001a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001f:  ldc.i4.2
  IL_0020:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0025:  dup
  IL_0026:  ldc.i4.0
  IL_0027:  ldc.i4.1
  IL_0028:  ldnull
  IL_0029:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002e:  stelem.ref
  IL_002f:  dup
  IL_0030:  ldc.i4.1
  IL_0031:  ldc.i4.0
  IL_0032:  ldnull
  IL_0033:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0038:  stelem.ref
  IL_0039:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003e:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0043:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> C.<>o__1.<>p__0""
  IL_0048:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> C.<>o__1.<>p__0""
  IL_004d:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>>.Target""
  IL_0052:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> C.<>o__1.<>p__0""
  IL_0057:  ldloc.1
  IL_0058:  ldloc.0
  IL_0059:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, bool, object)""
  IL_005e:  ret
  IL_005f:  ldloc.1
  IL_0060:  box        ""bool""
  IL_0065:  ret
}
");
        }

        [Fact]
        public void BooleanOperation_Or_UserDefinedTrue_Dynamic()
        {
            string source = @"
class B
{
    public static bool operator true(B t) { return true; }
    public static bool operator false(B t) { return false; }
}

class C
{  
    B b = new B();
    
    int M(dynamic d)
    {
        if (b || d)
        {
            return 1;
        }

        return 2;
    }
}
";
            // Dev11:  IsTrue(B.op_True(b) ? b : Or(b, d)) ? 1 : 2
            // Roslyn: B.op_True(b) || IsTrue(Or(b,d)) ? 1 : 2 
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      178 (0xb2)
  .maxstack  10
  .locals init (B V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""B C.b""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  call       ""bool B.op_True(B)""
  IL_000d:  brtrue     IL_00ae
  IL_0012:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__1.<>p__1""
  IL_0017:  brtrue.s   IL_0045
  IL_0019:  ldc.i4.0
  IL_001a:  ldc.i4.s   83
  IL_001c:  ldtoken    ""C""
  IL_0021:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0026:  ldc.i4.1
  IL_0027:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_002c:  dup
  IL_002d:  ldc.i4.0
  IL_002e:  ldc.i4.0
  IL_002f:  ldnull
  IL_0030:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0035:  stelem.ref
  IL_0036:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0040:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__1.<>p__1""
  IL_0045:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__1.<>p__1""
  IL_004a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Target""
  IL_004f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__1.<>p__1""
  IL_0054:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>> C.<>o__1.<>p__0""
  IL_0059:  brtrue.s   IL_0091
  IL_005b:  ldc.i4.8
  IL_005c:  ldc.i4.s   36
  IL_005e:  ldtoken    ""C""
  IL_0063:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0068:  ldc.i4.2
  IL_0069:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_006e:  dup
  IL_006f:  ldc.i4.0
  IL_0070:  ldc.i4.1
  IL_0071:  ldnull
  IL_0072:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0077:  stelem.ref
  IL_0078:  dup
  IL_0079:  ldc.i4.1
  IL_007a:  ldc.i4.0
  IL_007b:  ldnull
  IL_007c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0081:  stelem.ref
  IL_0082:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0087:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_008c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>> C.<>o__1.<>p__0""
  IL_0091:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>> C.<>o__1.<>p__0""
  IL_0096:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, B, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>>.Target""
  IL_009b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>> C.<>o__1.<>p__0""
  IL_00a0:  ldloc.0
  IL_00a1:  ldarg.1
  IL_00a2:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, B, object)""
  IL_00a7:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, object, bool>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_00ac:  brfalse.s  IL_00b0
  IL_00ae:  ldc.i4.1
  IL_00af:  ret
  IL_00b0:  ldc.i4.2
  IL_00b1:  ret
}
");
        }

        /// <summary>
        /// Implicit conversion has precedence over operator true/false.
        /// </summary>
        [Fact]
        public void BooleanOperation_Or_UserDefinedImplicitConversion_Dynamic()
        {
            string source = @"
class B
{
    public static implicit operator bool(B t) { return true; }
    public static bool operator true(B t) { return true; }
    public static bool operator false(B t) { return false; }
}

class C
{  
    B b = new B();
    
    int M(dynamic d)
    {
        if (b || d)
        {
            return 1;
        }

        return 2;
    }
}
";
            // Dev11:  IsTrue(B.op_Implicit(b) ? b : Or(b, d)) ? 1 : 2
            // Roslyn: B.op_Implicit(b) || IsTrue(Or(b,d)) ? 1 : 2 
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      178 (0xb2)
  .maxstack  10
  .locals init (B V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""B C.b""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  call       ""bool B.op_Implicit(B)""
  IL_000d:  brtrue     IL_00ae
  IL_0012:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__1.<>p__1""
  IL_0017:  brtrue.s   IL_0045
  IL_0019:  ldc.i4.0
  IL_001a:  ldc.i4.s   83
  IL_001c:  ldtoken    ""C""
  IL_0021:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0026:  ldc.i4.1
  IL_0027:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_002c:  dup
  IL_002d:  ldc.i4.0
  IL_002e:  ldc.i4.0
  IL_002f:  ldnull
  IL_0030:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0035:  stelem.ref
  IL_0036:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0040:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__1.<>p__1""
  IL_0045:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__1.<>p__1""
  IL_004a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Target""
  IL_004f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__1.<>p__1""
  IL_0054:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>> C.<>o__1.<>p__0""
  IL_0059:  brtrue.s   IL_0091
  IL_005b:  ldc.i4.8
  IL_005c:  ldc.i4.s   36
  IL_005e:  ldtoken    ""C""
  IL_0063:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0068:  ldc.i4.2
  IL_0069:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_006e:  dup
  IL_006f:  ldc.i4.0
  IL_0070:  ldc.i4.1
  IL_0071:  ldnull
  IL_0072:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0077:  stelem.ref
  IL_0078:  dup
  IL_0079:  ldc.i4.1
  IL_007a:  ldc.i4.0
  IL_007b:  ldnull
  IL_007c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0081:  stelem.ref
  IL_0082:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0087:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_008c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>> C.<>o__1.<>p__0""
  IL_0091:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>> C.<>o__1.<>p__0""
  IL_0096:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, B, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>>.Target""
  IL_009b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>> C.<>o__1.<>p__0""
  IL_00a0:  ldloc.0
  IL_00a1:  ldarg.1
  IL_00a2:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, B, object)""
  IL_00a7:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, object, bool>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_00ac:  brfalse.s  IL_00b0
  IL_00ae:  ldc.i4.1
  IL_00af:  ret
  IL_00b0:  ldc.i4.2
  IL_00b1:  ret
}");
        }

        [Fact]
        public void BooleanOperation_And_UserDefinedTrue_Dynamic()
        {
            string source = @"
class B
{
    public static bool operator true(B t) { return true; }
    public static bool operator false(B t) { return false; }
}

class C
{  
    B b = new B();
    
    int M(dynamic d)
    {
        if (b && d)
        {
            return 1;
        }

        return 2;
    }
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      177 (0xb1)
  .maxstack  10
  .locals init (B V_0)
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__1.<>p__1""
  IL_0005:  brtrue.s   IL_0033
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   83
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.1
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0029:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_002e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__1.<>p__1""
  IL_0033:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__1.<>p__1""
  IL_0038:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Target""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__1.<>p__1""
  IL_0042:  ldarg.0
  IL_0043:  ldfld      ""B C.b""
  IL_0048:  stloc.0
  IL_0049:  ldloc.0
  IL_004a:  call       ""bool B.op_False(B)""
  IL_004f:  brtrue.s   IL_00a5
  IL_0051:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>> C.<>o__1.<>p__0""
  IL_0056:  brtrue.s   IL_008d
  IL_0058:  ldc.i4.8
  IL_0059:  ldc.i4.2
  IL_005a:  ldtoken    ""C""
  IL_005f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0064:  ldc.i4.2
  IL_0065:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_006a:  dup
  IL_006b:  ldc.i4.0
  IL_006c:  ldc.i4.1
  IL_006d:  ldnull
  IL_006e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0073:  stelem.ref
  IL_0074:  dup
  IL_0075:  ldc.i4.1
  IL_0076:  ldc.i4.0
  IL_0077:  ldnull
  IL_0078:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_007d:  stelem.ref
  IL_007e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0083:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0088:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>> C.<>o__1.<>p__0""
  IL_008d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>> C.<>o__1.<>p__0""
  IL_0092:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, B, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>>.Target""
  IL_0097:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>> C.<>o__1.<>p__0""
  IL_009c:  ldloc.0
  IL_009d:  ldarg.1
  IL_009e:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, B, object)""
  IL_00a3:  br.s       IL_00a6
  IL_00a5:  ldloc.0
  IL_00a6:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, object, bool>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_00ab:  brfalse.s  IL_00af
  IL_00ad:  ldc.i4.1
  IL_00ae:  ret
  IL_00af:  ldc.i4.2
  IL_00b0:  ret
}
");
        }

        /// <summary>
        /// Implicit conversion has precedence over operator true/false.
        /// </summary>
        [Fact]
        public void BooleanOperation_And_UserDefinedImplicitConversion_Dynamic()
        {
            string source = @"
class B
{
    public static implicit operator bool(B t) { return true; }
    public static bool operator true(B t) { return true; }
    public static bool operator false(B t) { return false; }
}

class C
{  
    B b = new B();
    
    int M(dynamic d)
    {
        if (b && d)
        {
            return 1;
        }

        return 2;
    }
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      177 (0xb1)
  .maxstack  10
  .locals init (B V_0)
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__1.<>p__1""
  IL_0005:  brtrue.s   IL_0033
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   83
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.1
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0029:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_002e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__1.<>p__1""
  IL_0033:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__1.<>p__1""
  IL_0038:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Target""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__1.<>p__1""
  IL_0042:  ldarg.0
  IL_0043:  ldfld      ""B C.b""
  IL_0048:  stloc.0
  IL_0049:  ldloc.0
  IL_004a:  call       ""bool B.op_Implicit(B)""
  IL_004f:  brfalse.s  IL_00a5
  IL_0051:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>> C.<>o__1.<>p__0""
  IL_0056:  brtrue.s   IL_008d
  IL_0058:  ldc.i4.8
  IL_0059:  ldc.i4.2
  IL_005a:  ldtoken    ""C""
  IL_005f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0064:  ldc.i4.2
  IL_0065:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_006a:  dup
  IL_006b:  ldc.i4.0
  IL_006c:  ldc.i4.1
  IL_006d:  ldnull
  IL_006e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0073:  stelem.ref
  IL_0074:  dup
  IL_0075:  ldc.i4.1
  IL_0076:  ldc.i4.0
  IL_0077:  ldnull
  IL_0078:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_007d:  stelem.ref
  IL_007e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0083:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0088:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>> C.<>o__1.<>p__0""
  IL_008d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>> C.<>o__1.<>p__0""
  IL_0092:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, B, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>>.Target""
  IL_0097:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>> C.<>o__1.<>p__0""
  IL_009c:  ldloc.0
  IL_009d:  ldarg.1
  IL_009e:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, B, object)""
  IL_00a3:  br.s       IL_00a6
  IL_00a5:  ldloc.0
  IL_00a6:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, object, bool>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_00ab:  brfalse.s  IL_00af
  IL_00ad:  ldc.i4.1
  IL_00ae:  ret
  IL_00af:  ldc.i4.2
  IL_00b0:  ret
}
");
        }

        private const string StructWithUserDefinedBooleanOperators = @"
struct S
{
    private int num;
    private string str;

    public S(int num, char chr) 
    { 
        this.num = num;
        this.str = chr.ToString();
    }

    public S(int num, string str) 
    { 
        this.num = num;
        this.str = str;
    }

    public static S operator & (S x, S y) 
    { 
        return new S(x.num & y.num, '(' + x.str + '&' + y.str + ')'); 
    }

    public static S operator | (S x, S y) 
    { 
        return new S(x.num | y.num, '(' + x.str + '|' + y.str + ')'); 
    }

    public static bool operator true(S s) 
    { 
        return s.num != 0; 
    }

    public static bool operator false(S s) 
    { 
        return s.num == 0; 
    }

    public override string ToString() 
    { 
        return this.num.ToString() + ':' + this.str; 
    }
}
";

        [Fact]
        public void BooleanOperation_NestedOperators1()
        {
            // Analogue to OperatorTests.TestUserDefinedLogicalOperators1.

            string source = @"
using System;

class C
{
    static void Main()
    {
        dynamic f = new S(0, 'f');
        dynamic t = new S(1, 't');
        Console.WriteLine((f && f) && f);
        Console.WriteLine((f && f) && t);
        Console.WriteLine((f && t) && f);
        Console.WriteLine((f && t) && t);
        Console.WriteLine((t && f) && f);
        Console.WriteLine((t && f) && t);
        Console.WriteLine((t && t) && f);
        Console.WriteLine((t && t) && t);
        Console.WriteLine('-');
        Console.WriteLine((f && f) || f);
        Console.WriteLine((f && f) || t);
        Console.WriteLine((f && t) || f);
        Console.WriteLine((f && t) || t);
        Console.WriteLine((t && f) || f);
        Console.WriteLine((t && f) || t);
        Console.WriteLine((t && t) || f);
        Console.WriteLine((t && t) || t);
        Console.WriteLine('-');
        Console.WriteLine((f || f) && f);
        Console.WriteLine((f || f) && t);
        Console.WriteLine((f || t) && f);
        Console.WriteLine((f || t) && t);
        Console.WriteLine((t || f) && f);
        Console.WriteLine((t || f) && t);
        Console.WriteLine((t || t) && f);
        Console.WriteLine((t || t) && t);
        Console.WriteLine('-');
        Console.WriteLine((f || f) || f);
        Console.WriteLine((f || f) || t);
        Console.WriteLine((f || t) || f);
        Console.WriteLine((f || t) || t);
        Console.WriteLine((t || f) || f);
        Console.WriteLine((t || f) || t);
        Console.WriteLine((t || t) || f);
        Console.WriteLine((t || t) || t);
        Console.WriteLine('-');
        Console.WriteLine(f && (f && f));
        Console.WriteLine(f && (f && t));
        Console.WriteLine(f && (t && f));
        Console.WriteLine(f && (t && t));
        Console.WriteLine(t && (f && f));
        Console.WriteLine(t && (f && t));
        Console.WriteLine(t && (t && f));
        Console.WriteLine(t && (t && t));
        Console.WriteLine('-');
        Console.WriteLine(f && (f || f));
        Console.WriteLine(f && (f || t));
        Console.WriteLine(f && (t || f));
        Console.WriteLine(f && (t || t));
        Console.WriteLine(t && (f || f));
        Console.WriteLine(t && (f || t));
        Console.WriteLine(t && (t || f));
        Console.WriteLine(t && (t || t));
        Console.WriteLine('-');
        Console.WriteLine(f || (f && f));
        Console.WriteLine(f || (f && t));
        Console.WriteLine(f || (t && f));
        Console.WriteLine(f || (t && t));
        Console.WriteLine(t || (f && f));
        Console.WriteLine(t || (f && t));
        Console.WriteLine(t || (t && f));
        Console.WriteLine(t || (t && t));
        Console.WriteLine('-');
        Console.WriteLine(f || (f || f));
        Console.WriteLine(f || (f || t));
        Console.WriteLine(f || (t || f));
        Console.WriteLine(f || (t || t));
        Console.WriteLine(t || (f || f));
        Console.WriteLine(t || (f || t));
        Console.WriteLine(t || (t || f));
        Console.WriteLine(t || (t || t));
    }
}

" + StructWithUserDefinedBooleanOperators;

            string output = @"0:f
0:f
0:f
0:f
0:(t&f)
0:(t&f)
0:((t&t)&f)
1:((t&t)&t)
-
0:(f|f)
1:(f|t)
0:(f|f)
1:(f|t)
0:((t&f)|f)
1:((t&f)|t)
1:(t&t)
1:(t&t)
-
0:(f|f)
0:(f|f)
0:((f|t)&f)
1:((f|t)&t)
0:(t&f)
1:(t&t)
0:(t&f)
1:(t&t)
-
0:((f|f)|f)
1:((f|f)|t)
1:(f|t)
1:(f|t)
1:t
1:t
1:t
1:t
-
0:f
0:f
0:f
0:f
0:(t&f)
0:(t&f)
0:(t&(t&f))
1:(t&(t&t))
-
0:f
0:f
0:f
0:f
0:(t&(f|f))
1:(t&(f|t))
1:(t&t)
1:(t&t)
-
0:(f|f)
0:(f|f)
0:(f|(t&f))
1:(f|(t&t))
1:t
1:t
1:t
1:t
-
0:(f|(f|f))
1:(f|(f|t))
1:(f|t)
1:(f|t)
1:t
1:t
1:t
1:t";

            CompileAndVerify(source: source, expectedOutput: output, additionalRefs: new[] { SystemCoreRef, CSharpRef });
        }


        [Fact]
        public void BooleanOperation_NestedOperators2()
        {
            // Analogue to OperatorTests.TestUserDefinedLogicalOperators2.

            string source = @"
using System;

class C
{
    static void Main()
    {
        dynamic f = new S(0, 'f');
        dynamic t = new S(1, 't');
        Console.Write((f && f) && f ? 1 : 0);
        Console.Write((f && f) && t ? 1 : 0);
        Console.Write((f && t) && f ? 1 : 0);
        Console.Write((f && t) && t ? 1 : 0);
        Console.Write((t && f) && f ? 1 : 0);
        Console.Write((t && f) && t ? 1 : 0);
        Console.Write((t && t) && f ? 1 : 0);
        Console.Write((t && t) && t ? 1 : 0);
        Console.WriteLine('-');
        Console.Write((f && f) || f ? 1 : 0);
        Console.Write((f && f) || t ? 1 : 0);
        Console.Write((f && t) || f ? 1 : 0);
        Console.Write((f && t) || t ? 1 : 0);
        Console.Write((t && f) || f ? 1 : 0);
        Console.Write((t && f) || t ? 1 : 0);
        Console.Write((t && t) || f ? 1 : 0);
        Console.Write((t && t) || t ? 1 : 0);
        Console.WriteLine('-');
        Console.Write((f || f) && f ? 1 : 0);
        Console.Write((f || f) && t ? 1 : 0);
        Console.Write((f || t) && f ? 1 : 0);
        Console.Write((f || t) && t ? 1 : 0);
        Console.Write((t || f) && f ? 1 : 0);
        Console.Write((t || f) && t ? 1 : 0);
        Console.Write((t || t) && f ? 1 : 0);
        Console.Write((t || t) && t ? 1 : 0);
        Console.WriteLine('-');
        Console.Write((f || f) || f ? 1 : 0);
        Console.Write((f || f) || t ? 1 : 0);
        Console.Write((f || t) || f ? 1 : 0);
        Console.Write((f || t) || t ? 1 : 0);
        Console.Write((t || f) || f ? 1 : 0);
        Console.Write((t || f) || t ? 1 : 0);
        Console.Write((t || t) || f ? 1 : 0);
        Console.Write((t || t) || t ? 1 : 0);
        Console.WriteLine('-');       
        Console.Write(f && (f && f) ? 1 : 0);
        Console.Write(f && (f && t) ? 1 : 0);
        Console.Write(f && (t && f) ? 1 : 0);
        Console.Write(f && (t && t) ? 1 : 0);
        Console.Write(t && (f && f) ? 1 : 0);
        Console.Write(t && (f && t) ? 1 : 0);
        Console.Write(t && (t && f) ? 1 : 0);
        Console.Write(t && (t && t) ? 1 : 0);
        Console.WriteLine('-');      
        Console.Write(f && (f || f) ? 1 : 0);
        Console.Write(f && (f || t) ? 1 : 0);
        Console.Write(f && (t || f) ? 1 : 0);
        Console.Write(f && (t || t) ? 1 : 0);
        Console.Write(t && (f || f) ? 1 : 0);
        Console.Write(t && (f || t) ? 1 : 0);
        Console.Write(t && (t || f) ? 1 : 0);
        Console.Write(t && (t || t) ? 1 : 0);
        Console.WriteLine('-');        
        Console.Write(f || (f && f) ? 1 : 0);
        Console.Write(f || (f && t) ? 1 : 0);
        Console.Write(f || (t && f) ? 1 : 0);
        Console.Write(f || (t && t) ? 1 : 0);
        Console.Write(t || (f && f) ? 1 : 0);
        Console.Write(t || (f && t) ? 1 : 0);
        Console.Write(t || (t && f) ? 1 : 0);
        Console.Write(t || (t && t) ? 1 : 0);
        Console.WriteLine('-');        
        Console.Write(f || (f || f) ? 1 : 0);
        Console.Write(f || (f || t) ? 1 : 0);
        Console.Write(f || (t || f) ? 1 : 0);
        Console.Write(f || (t || t) ? 1 : 0);
        Console.Write(t || (f || f) ? 1 : 0);
        Console.Write(t || (f || t) ? 1 : 0);
        Console.Write(t || (t || f) ? 1 : 0);
        Console.Write(t || (t || t) ? 1 : 0);
    }
}
" + StructWithUserDefinedBooleanOperators;

            string output = @"
00000001-
01010111-
00010101-
01111111-
00000001-
00000111-
00011111-
01111111";

            CompileAndVerify(source: source, expectedOutput: output, additionalRefs: new[] { SystemCoreRef, CSharpRef });
        }

        [Fact]
        public void BooleanOperation_EvaluationOrder()
        {
            string source = @"
public class C
{
    public static dynamic f(ref dynamic d) 
    {
        d = false;
        return d;
    }
    
    public static void Main()
    {
        dynamic d = true;

        if (d || f(ref d))
        {
            return;
        }
        else
        {
            throw null;
        }
    }
}
";
            CompileAndVerify(source, expectedOutput: "", additionalRefs: new[] { SystemCoreRef, CSharpRef });
        }

        [Fact]
        public void Multiplication_CompoundAssignment()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return d *= d;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       87 (0x57)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_003d
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   69
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.2
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  dup
  IL_0025:  ldc.i4.1
  IL_0026:  ldc.i4.0
  IL_0027:  ldnull
  IL_0028:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002d:  stelem.ref
  IL_002e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0033:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0038:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0042:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_004c:  ldarg.1
  IL_004d:  ldarg.1
  IL_004e:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0053:  dup
  IL_0054:  starg.s    V_1
  IL_0056:  ret
}
");
        }

        [Fact]
        public void Multiplication_CompoundAssignment_Checked()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return checked(d *= d);
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       87 (0x57)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_003d
  IL_0007:  ldc.i4.1
  IL_0008:  ldc.i4.s   69
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.2
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  dup
  IL_0025:  ldc.i4.1
  IL_0026:  ldc.i4.0
  IL_0027:  ldnull
  IL_0028:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002d:  stelem.ref
  IL_002e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0033:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0038:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0042:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_004c:  ldarg.1
  IL_004d:  ldarg.1
  IL_004e:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0053:  dup
  IL_0054:  starg.s    V_1
  IL_0056:  ret
}
");
        }

        [Fact]
        public void Multiplication_CompoundAssignment_DiscardResult()
        {
            string source = @"
public class C
{
    public void M(dynamic d)
    {
        d *= d;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       86 (0x56)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_003d
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   69
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.2
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  dup
  IL_0025:  ldc.i4.1
  IL_0026:  ldc.i4.0
  IL_0027:  ldnull
  IL_0028:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002d:  stelem.ref
  IL_002e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0033:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0038:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0042:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_004c:  ldarg.1
  IL_004d:  ldarg.1
  IL_004e:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0053:  starg.s    V_1
  IL_0055:  ret
}
");
        }

        [Fact]
        public void Addition_CompoundAssignment()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d, dynamic v)
    {
        return d += v;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       87 (0x57)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_003d
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   63
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.2
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  dup
  IL_0025:  ldc.i4.1
  IL_0026:  ldc.i4.0
  IL_0027:  ldnull
  IL_0028:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002d:  stelem.ref
  IL_002e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0033:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0038:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0042:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_004c:  ldarg.1
  IL_004d:  ldarg.2
  IL_004e:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0053:  dup
  IL_0054:  starg.s    V_1
  IL_0056:  ret
}
");
        }

        [Fact]
        public void Addition_EventHandler_SimpleConversion()
        {
            string source = @"
public class C
{    
    event System.Action e;
    dynamic v;

    public void M()
    {
        e += v;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       76 (0x4c)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action>> C.<>o__4.<>p__0""
  IL_0006:  brtrue.s   IL_002c
  IL_0008:  ldc.i4.0
  IL_0009:  ldtoken    ""System.Action""
  IL_000e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0013:  ldtoken    ""C""
  IL_0018:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0022:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0027:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action>> C.<>o__4.<>p__0""
  IL_002c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action>> C.<>o__4.<>p__0""
  IL_0031:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action>>.Target""
  IL_0036:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action>> C.<>o__4.<>p__0""
  IL_003b:  ldarg.0
  IL_003c:  ldfld      ""dynamic C.v""
  IL_0041:  callvirt   ""System.Action System.Func<System.Runtime.CompilerServices.CallSite, object, System.Action>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0046:  call       ""void C.e.add""
  IL_004b:  ret
}");
        }

        [Fact]
        public void PostIncrement()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return d++;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       78 (0x4e)
  .maxstack  8
  .locals init (object V_0)
  IL_0000:  ldarg.1
  IL_0001:  stloc.0
  IL_0002:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0007:  brtrue.s   IL_0035
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.s   54
  IL_000c:  ldtoken    ""C""
  IL_0011:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0016:  ldc.i4.1
  IL_0017:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001c:  dup
  IL_001d:  ldc.i4.0
  IL_001e:  ldc.i4.0
  IL_001f:  ldnull
  IL_0020:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0025:  stelem.ref
  IL_0026:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_002b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0030:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0035:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_003a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_003f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0044:  ldloc.0
  IL_0045:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_004a:  starg.s    V_1
  IL_004c:  ldloc.0
  IL_004d:  ret
}");
        }

        [Fact]
        public void PostDecrement()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return d--;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       78 (0x4e)
  .maxstack  8
  .locals init (object V_0)
  IL_0000:  ldarg.1
  IL_0001:  stloc.0
  IL_0002:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0007:  brtrue.s   IL_0035
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.s   49
  IL_000c:  ldtoken    ""C""
  IL_0011:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0016:  ldc.i4.1
  IL_0017:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001c:  dup
  IL_001d:  ldc.i4.0
  IL_001e:  ldc.i4.0
  IL_001f:  ldnull
  IL_0020:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0025:  stelem.ref
  IL_0026:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_002b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0030:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0035:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_003a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_003f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0044:  ldloc.0
  IL_0045:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_004a:  starg.s    V_1
  IL_004c:  ldloc.0
  IL_004d:  ret
}");
        }

        [Fact]
        public void PreIncrement()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return ++d;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       76 (0x4c)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0033
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   54
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.1
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0029:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_002e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0033:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0038:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0042:  ldarg.1
  IL_0043:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0048:  dup
  IL_0049:  starg.s    V_1
  IL_004b:  ret
}");
        }

        [Fact]
        public void PreDecrement()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return --d;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       76 (0x4c)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0033
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   49
  IL_000a:  ldtoken    ""C""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldc.i4.1
  IL_0015:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0023:  stelem.ref
  IL_0024:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0029:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_002e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0033:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0038:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0042:  ldarg.1
  IL_0043:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0048:  dup
  IL_0049:  starg.s    V_1
  IL_004b:  ret
}
");
        }

        [Fact]
        public void PostIncrement_DynamicArrayElementAccess()
        {
            string source = @"
public class C
{
    static dynamic[] d;
        
    static void M()
    {
        d[0]++;
    }
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       84 (0x54)
  .maxstack  10
  .locals init (object V_0)
  IL_0000:  ldsfld     ""dynamic[] C.d""
  IL_0005:  dup
  IL_0006:  ldc.i4.0
  IL_0007:  ldelem.ref
  IL_0008:  stloc.0
  IL_0009:  ldc.i4.0
  IL_000a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__1.<>p__0""
  IL_000f:  brtrue.s   IL_003d
  IL_0011:  ldc.i4.0
  IL_0012:  ldc.i4.s   54
  IL_0014:  ldtoken    ""C""
  IL_0019:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001e:  ldc.i4.1
  IL_001f:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0024:  dup
  IL_0025:  ldc.i4.0
  IL_0026:  ldc.i4.0
  IL_0027:  ldnull
  IL_0028:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002d:  stelem.ref
  IL_002e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0033:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0038:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__1.<>p__0""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__1.<>p__0""
  IL_0042:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__1.<>p__0""
  IL_004c:  ldloc.0
  IL_004d:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0052:  stelem.ref
  IL_0053:  ret
}");
        }

        [Fact]
        public void PreIncrement_DynamicArrayElementAccess()
        {
            string source = @"
public class C
{
    static dynamic[] d;
        
    static void M()
    {
        ++d[0];
    }
}
";
            // TODO (tomat): IL_0050 ... IL_0053 and V_1 could be optimized away
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       86 (0x56)
  .maxstack  8
  .locals init (object[] V_0,
                object V_1)
  IL_0000:  ldsfld     ""dynamic[] C.d""
  IL_0005:  stloc.0
  IL_0006:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__1.<>p__0""
  IL_000b:  brtrue.s   IL_0039
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.s   54
  IL_0010:  ldtoken    ""C""
  IL_0015:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001a:  ldc.i4.1
  IL_001b:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0020:  dup
  IL_0021:  ldc.i4.0
  IL_0022:  ldc.i4.0
  IL_0023:  ldnull
  IL_0024:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0029:  stelem.ref
  IL_002a:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_002f:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0034:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__1.<>p__0""
  IL_0039:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__1.<>p__0""
  IL_003e:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_0043:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__1.<>p__0""
  IL_0048:  ldloc.0
  IL_0049:  ldc.i4.0
  IL_004a:  ldelem.ref
  IL_004b:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0050:  stloc.1
  IL_0051:  ldloc.0
  IL_0052:  ldc.i4.0
  IL_0053:  ldloc.1
  IL_0054:  stelem.ref
  IL_0055:  ret
}");
        }

        [Fact]
        public void PreIncrement_DynamicMemberAccess()
        {
            string source = @"
class C
{
    dynamic d = null;

    dynamic M()
    {   
        return ++d.P;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      243 (0xf3)
  .maxstack  10
  .locals init (object V_0,
                object V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""dynamic C.d""
  IL_0006:  stloc.0
  IL_0007:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__1.<>p__1""
  IL_000c:  brtrue.s   IL_003a
  IL_000e:  ldc.i4.0
  IL_000f:  ldc.i4.s   54
  IL_0011:  ldtoken    ""C""
  IL_0016:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001b:  ldc.i4.1
  IL_001c:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0021:  dup
  IL_0022:  ldc.i4.0
  IL_0023:  ldc.i4.0
  IL_0024:  ldnull
  IL_0025:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002a:  stelem.ref
  IL_002b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0030:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0035:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__1.<>p__1""
  IL_003a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__1.<>p__1""
  IL_003f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_0044:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__1.<>p__1""
  IL_0049:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__1.<>p__0""
  IL_004e:  brtrue.s   IL_007f
  IL_0050:  ldc.i4.0
  IL_0051:  ldstr      ""P""
  IL_0056:  ldtoken    ""C""
  IL_005b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0060:  ldc.i4.1
  IL_0061:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0066:  dup
  IL_0067:  ldc.i4.0
  IL_0068:  ldc.i4.0
  IL_0069:  ldnull
  IL_006a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_006f:  stelem.ref
  IL_0070:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0075:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_007a:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__1.<>p__0""
  IL_007f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__1.<>p__0""
  IL_0084:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_0089:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__1.<>p__0""
  IL_008e:  ldloc.0
  IL_008f:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0094:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0099:  stloc.1
  IL_009a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__1.<>p__2""
  IL_009f:  brtrue.s   IL_00da
  IL_00a1:  ldc.i4.0
  IL_00a2:  ldstr      ""P""
  IL_00a7:  ldtoken    ""C""
  IL_00ac:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00b1:  ldc.i4.2
  IL_00b2:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00b7:  dup
  IL_00b8:  ldc.i4.0
  IL_00b9:  ldc.i4.0
  IL_00ba:  ldnull
  IL_00bb:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00c0:  stelem.ref
  IL_00c1:  dup
  IL_00c2:  ldc.i4.1
  IL_00c3:  ldc.i4.0
  IL_00c4:  ldnull
  IL_00c5:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00ca:  stelem.ref
  IL_00cb:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00d0:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00d5:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__1.<>p__2""
  IL_00da:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__1.<>p__2""
  IL_00df:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_00e4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__1.<>p__2""
  IL_00e9:  ldloc.0
  IL_00ea:  ldloc.1
  IL_00eb:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_00f0:  pop
  IL_00f1:  ldloc.1
  IL_00f2:  ret
}
");
        }

        [Fact]
        public void PostIncrement_DynamicMemberAccess()
        {
            string source = @"
class C
{
    dynamic d = null;

    dynamic M()
    {   
        return d.P++;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      243 (0xf3)
  .maxstack  11
  .locals init (object V_0,
                object V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""dynamic C.d""
  IL_0006:  stloc.0
  IL_0007:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__1.<>p__1""
  IL_000c:  brtrue.s   IL_003d
  IL_000e:  ldc.i4.0
  IL_000f:  ldstr      ""P""
  IL_0014:  ldtoken    ""C""
  IL_0019:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001e:  ldc.i4.1
  IL_001f:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0024:  dup
  IL_0025:  ldc.i4.0
  IL_0026:  ldc.i4.0
  IL_0027:  ldnull
  IL_0028:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002d:  stelem.ref
  IL_002e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0033:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0038:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__1.<>p__1""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__1.<>p__1""
  IL_0042:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__1.<>p__1""
  IL_004c:  ldloc.0
  IL_004d:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0052:  stloc.1
  IL_0053:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__1.<>p__2""
  IL_0058:  brtrue.s   IL_0093
  IL_005a:  ldc.i4.0
  IL_005b:  ldstr      ""P""
  IL_0060:  ldtoken    ""C""
  IL_0065:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_006a:  ldc.i4.2
  IL_006b:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0070:  dup
  IL_0071:  ldc.i4.0
  IL_0072:  ldc.i4.0
  IL_0073:  ldnull
  IL_0074:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0079:  stelem.ref
  IL_007a:  dup
  IL_007b:  ldc.i4.1
  IL_007c:  ldc.i4.0
  IL_007d:  ldnull
  IL_007e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0083:  stelem.ref
  IL_0084:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0089:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_008e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__1.<>p__2""
  IL_0093:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__1.<>p__2""
  IL_0098:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_009d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__1.<>p__2""
  IL_00a2:  ldloc.0
  IL_00a3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__1.<>p__0""
  IL_00a8:  brtrue.s   IL_00d6
  IL_00aa:  ldc.i4.0
  IL_00ab:  ldc.i4.s   54
  IL_00ad:  ldtoken    ""C""
  IL_00b2:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00b7:  ldc.i4.1
  IL_00b8:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00bd:  dup
  IL_00be:  ldc.i4.0
  IL_00bf:  ldc.i4.0
  IL_00c0:  ldnull
  IL_00c1:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00c6:  stelem.ref
  IL_00c7:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00cc:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00d1:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__1.<>p__0""
  IL_00d6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__1.<>p__0""
  IL_00db:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_00e0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__1.<>p__0""
  IL_00e5:  ldloc.1
  IL_00e6:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_00eb:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_00f0:  pop
  IL_00f1:  ldloc.1
  IL_00f2:  ret
}

");
        }

        [Fact]
        public void PreIncrement_DynamicIndexerAccess()
        {
            string source = @"
class C
{
    dynamic d = null;

    int F() { return 0; }

    dynamic M()
    {   
        return ++d[F()];
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      262 (0x106)
  .maxstack  9
  .locals init (object V_0,
                int V_1,
                object V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""dynamic C.d""
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  call       ""int C.F()""
  IL_000d:  stloc.1
  IL_000e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__2.<>p__1""
  IL_0013:  brtrue.s   IL_0041
  IL_0015:  ldc.i4.0
  IL_0016:  ldc.i4.s   54
  IL_0018:  ldtoken    ""C""
  IL_001d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0022:  ldc.i4.1
  IL_0023:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0028:  dup
  IL_0029:  ldc.i4.0
  IL_002a:  ldc.i4.0
  IL_002b:  ldnull
  IL_002c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0031:  stelem.ref
  IL_0032:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0037:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_003c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__2.<>p__1""
  IL_0041:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__2.<>p__1""
  IL_0046:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_004b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__2.<>p__1""
  IL_0050:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__2.<>p__0""
  IL_0055:  brtrue.s   IL_008b
  IL_0057:  ldc.i4.0
  IL_0058:  ldtoken    ""C""
  IL_005d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0062:  ldc.i4.2
  IL_0063:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0068:  dup
  IL_0069:  ldc.i4.0
  IL_006a:  ldc.i4.0
  IL_006b:  ldnull
  IL_006c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0071:  stelem.ref
  IL_0072:  dup
  IL_0073:  ldc.i4.1
  IL_0074:  ldc.i4.1
  IL_0075:  ldnull
  IL_0076:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_007b:  stelem.ref
  IL_007c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0081:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0086:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__2.<>p__0""
  IL_008b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__2.<>p__0""
  IL_0090:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>>.Target""
  IL_0095:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__2.<>p__0""
  IL_009a:  ldloc.0
  IL_009b:  ldloc.1
  IL_009c:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, int)""
  IL_00a1:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_00a6:  stloc.2
  IL_00a7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object, object>> C.<>o__2.<>p__2""
  IL_00ac:  brtrue.s   IL_00ec
  IL_00ae:  ldc.i4.0
  IL_00af:  ldtoken    ""C""
  IL_00b4:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00b9:  ldc.i4.3
  IL_00ba:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00bf:  dup
  IL_00c0:  ldc.i4.0
  IL_00c1:  ldc.i4.0
  IL_00c2:  ldnull
  IL_00c3:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00c8:  stelem.ref
  IL_00c9:  dup
  IL_00ca:  ldc.i4.1
  IL_00cb:  ldc.i4.1
  IL_00cc:  ldnull
  IL_00cd:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00d2:  stelem.ref
  IL_00d3:  dup
  IL_00d4:  ldc.i4.2
  IL_00d5:  ldc.i4.0
  IL_00d6:  ldnull
  IL_00d7:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00dc:  stelem.ref
  IL_00dd:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00e2:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00e7:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object, object>> C.<>o__2.<>p__2""
  IL_00ec:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object, object>> C.<>o__2.<>p__2""
  IL_00f1:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object, object>>.Target""
  IL_00f6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object, object>> C.<>o__2.<>p__2""
  IL_00fb:  ldloc.0
  IL_00fc:  ldloc.1
  IL_00fd:  ldloc.2
  IL_00fe:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, int, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, int, object)""
  IL_0103:  pop
  IL_0104:  ldloc.2
  IL_0105:  ret
}");
        }

        [Fact]
        public void PostIncrement_DynamicIndexerAccess()
        {
            string source = @"
class C
{
    dynamic d = null;

    int F() { return 0; }

    dynamic M()
    {   
        return d[F()]++;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      262 (0x106)
  .maxstack  12
  .locals init (object V_0,
                int V_1,
                object V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""dynamic C.d""
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  call       ""int C.F()""
  IL_000d:  stloc.1
  IL_000e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__2.<>p__1""
  IL_0013:  brtrue.s   IL_0049
  IL_0015:  ldc.i4.0
  IL_0016:  ldtoken    ""C""
  IL_001b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0020:  ldc.i4.2
  IL_0021:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0026:  dup
  IL_0027:  ldc.i4.0
  IL_0028:  ldc.i4.0
  IL_0029:  ldnull
  IL_002a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002f:  stelem.ref
  IL_0030:  dup
  IL_0031:  ldc.i4.1
  IL_0032:  ldc.i4.1
  IL_0033:  ldnull
  IL_0034:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0039:  stelem.ref
  IL_003a:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003f:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0044:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__2.<>p__1""
  IL_0049:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__2.<>p__1""
  IL_004e:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>>.Target""
  IL_0053:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__2.<>p__1""
  IL_0058:  ldloc.0
  IL_0059:  ldloc.1
  IL_005a:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, int)""
  IL_005f:  stloc.2
  IL_0060:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object, object>> C.<>o__2.<>p__2""
  IL_0065:  brtrue.s   IL_00a5
  IL_0067:  ldc.i4.0
  IL_0068:  ldtoken    ""C""
  IL_006d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0072:  ldc.i4.3
  IL_0073:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0078:  dup
  IL_0079:  ldc.i4.0
  IL_007a:  ldc.i4.0
  IL_007b:  ldnull
  IL_007c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0081:  stelem.ref
  IL_0082:  dup
  IL_0083:  ldc.i4.1
  IL_0084:  ldc.i4.1
  IL_0085:  ldnull
  IL_0086:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_008b:  stelem.ref
  IL_008c:  dup
  IL_008d:  ldc.i4.2
  IL_008e:  ldc.i4.0
  IL_008f:  ldnull
  IL_0090:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0095:  stelem.ref
  IL_0096:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_009b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00a0:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object, object>> C.<>o__2.<>p__2""
  IL_00a5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object, object>> C.<>o__2.<>p__2""
  IL_00aa:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object, object>>.Target""
  IL_00af:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object, object>> C.<>o__2.<>p__2""
  IL_00b4:  ldloc.0
  IL_00b5:  ldloc.1
  IL_00b6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__2.<>p__0""
  IL_00bb:  brtrue.s   IL_00e9
  IL_00bd:  ldc.i4.0
  IL_00be:  ldc.i4.s   54
  IL_00c0:  ldtoken    ""C""
  IL_00c5:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00ca:  ldc.i4.1
  IL_00cb:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00d0:  dup
  IL_00d1:  ldc.i4.0
  IL_00d2:  ldc.i4.0
  IL_00d3:  ldnull
  IL_00d4:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00d9:  stelem.ref
  IL_00da:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00df:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00e4:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__2.<>p__0""
  IL_00e9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__2.<>p__0""
  IL_00ee:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_00f3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__2.<>p__0""
  IL_00f8:  ldloc.2
  IL_00f9:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_00fe:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, int, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, int, object)""
  IL_0103:  pop
  IL_0104:  ldloc.2
  IL_0105:  ret
}");
        }

        [Fact]
        public void NullCoalescing()
        {
            string source = @"
class C
{
    dynamic d = null;
    object o = null;

    void M()
    {
        var x = d ?? o;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""dynamic C.d""
  IL_0006:  brtrue.s   IL_000f
  IL_0008:  ldarg.0
  IL_0009:  ldfld      ""object C.o""
  IL_000e:  pop
  IL_000f:  ret
}
");
        }

        #endregion

        #region Invoke, InvokeMember, InvokeConstructor

        [Fact]
        public void InvokeMember_Dynamic()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return d.m(null, this, d);
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      110 (0x6e)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, C, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0055
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      ""m""
  IL_000d:  ldnull
  IL_000e:  ldtoken    ""C""
  IL_0013:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0018:  ldc.i4.4
  IL_0019:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001e:  dup
  IL_001f:  ldc.i4.0
  IL_0020:  ldc.i4.0
  IL_0021:  ldnull
  IL_0022:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0027:  stelem.ref
  IL_0028:  dup
  IL_0029:  ldc.i4.1
  IL_002a:  ldc.i4.2
  IL_002b:  ldnull
  IL_002c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0031:  stelem.ref
  IL_0032:  dup
  IL_0033:  ldc.i4.2
  IL_0034:  ldc.i4.1
  IL_0035:  ldnull
  IL_0036:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_003b:  stelem.ref
  IL_003c:  dup
  IL_003d:  ldc.i4.3
  IL_003e:  ldc.i4.0
  IL_003f:  ldnull
  IL_0040:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0045:  stelem.ref
  IL_0046:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_004b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, C, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, C, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0050:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, C, object, object>> C.<>o__0.<>p__0""
  IL_0055:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, C, object, object>> C.<>o__0.<>p__0""
  IL_005a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, C, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, C, object, object>>.Target""
  IL_005f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, C, object, object>> C.<>o__0.<>p__0""
  IL_0064:  ldarg.1
  IL_0065:  ldnull
  IL_0066:  ldarg.0
  IL_0067:  ldarg.1
  IL_0068:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, C, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object, C, object)""
  IL_006d:  ret
}
");
        }

        [Fact]
        public void InvokeMember_Static()
        {
            string source = @"
public class C
{
    public dynamic M(C c, dynamic d)
    {
        return c.F(null, this, d);
    }

    public int F(C a, C b, double c) 
    {
        return 1; 
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      110 (0x6e)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, C, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0055
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      ""F""
  IL_000d:  ldnull
  IL_000e:  ldtoken    ""C""
  IL_0013:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0018:  ldc.i4.4
  IL_0019:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001e:  dup
  IL_001f:  ldc.i4.0
  IL_0020:  ldc.i4.1
  IL_0021:  ldnull
  IL_0022:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0027:  stelem.ref
  IL_0028:  dup
  IL_0029:  ldc.i4.1
  IL_002a:  ldc.i4.2
  IL_002b:  ldnull
  IL_002c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0031:  stelem.ref
  IL_0032:  dup
  IL_0033:  ldc.i4.2
  IL_0034:  ldc.i4.1
  IL_0035:  ldnull
  IL_0036:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_003b:  stelem.ref
  IL_003c:  dup
  IL_003d:  ldc.i4.3
  IL_003e:  ldc.i4.0
  IL_003f:  ldnull
  IL_0040:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0045:  stelem.ref
  IL_0046:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_004b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, C, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, C, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0050:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, C, object, object>> C.<>o__0.<>p__0""
  IL_0055:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, C, object, object>> C.<>o__0.<>p__0""
  IL_005a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, C, object, C, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, C, object, object>>.Target""
  IL_005f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, C, object, object>> C.<>o__0.<>p__0""
  IL_0064:  ldarg.1
  IL_0065:  ldnull
  IL_0066:  ldarg.0
  IL_0067:  ldarg.2
  IL_0068:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, C, object, C, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, C, object, C, object)""
  IL_006d:  ret
}
");
        }

        [Fact]
        public void InvokeMember_Static_SimpleName()
        {
            string source = @"
public class C
{
    public dynamic M(C c, dynamic d)
    {
        return F(null, this, d);
    }

    public int F(C a, C b, double c) 
    {
        return 1; 
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      110 (0x6e)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, C, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0055
  IL_0007:  ldc.i4.2
  IL_0008:  ldstr      ""F""
  IL_000d:  ldnull
  IL_000e:  ldtoken    ""C""
  IL_0013:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0018:  ldc.i4.4
  IL_0019:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001e:  dup
  IL_001f:  ldc.i4.0
  IL_0020:  ldc.i4.1
  IL_0021:  ldnull
  IL_0022:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0027:  stelem.ref
  IL_0028:  dup
  IL_0029:  ldc.i4.1
  IL_002a:  ldc.i4.2
  IL_002b:  ldnull
  IL_002c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0031:  stelem.ref
  IL_0032:  dup
  IL_0033:  ldc.i4.2
  IL_0034:  ldc.i4.1
  IL_0035:  ldnull
  IL_0036:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_003b:  stelem.ref
  IL_003c:  dup
  IL_003d:  ldc.i4.3
  IL_003e:  ldc.i4.0
  IL_003f:  ldnull
  IL_0040:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0045:  stelem.ref
  IL_0046:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_004b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, C, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, C, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0050:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, C, object, object>> C.<>o__0.<>p__0""
  IL_0055:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, C, object, object>> C.<>o__0.<>p__0""
  IL_005a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, C, object, C, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, C, object, object>>.Target""
  IL_005f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, C, object, object>> C.<>o__0.<>p__0""
  IL_0064:  ldarg.0
  IL_0065:  ldnull
  IL_0066:  ldarg.0
  IL_0067:  ldarg.2
  IL_0068:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, C, object, C, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, C, object, C, object)""
  IL_006d:  ret
}
");
        }

        [Fact]
        public void InvokeMember_Static_ResultDiscarded()
        {
            string source = @"
public class C
{
    public void M(C c, dynamic d)
    {
        F(null, this, d);
    }

    public int F(C a, C b, double c) 
    {
        return 1; 
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      114 (0x72)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, C, object, C, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0059
  IL_0007:  ldc.i4     0x102
  IL_000c:  ldstr      ""F""
  IL_0011:  ldnull
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.4
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.1
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  dup
  IL_002d:  ldc.i4.1
  IL_002e:  ldc.i4.2
  IL_002f:  ldnull
  IL_0030:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0035:  stelem.ref
  IL_0036:  dup
  IL_0037:  ldc.i4.2
  IL_0038:  ldc.i4.1
  IL_0039:  ldnull
  IL_003a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_003f:  stelem.ref
  IL_0040:  dup
  IL_0041:  ldc.i4.3
  IL_0042:  ldc.i4.0
  IL_0043:  ldnull
  IL_0044:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0049:  stelem.ref
  IL_004a:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_004f:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, C, object, C, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, C, object, C, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0054:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, C, object, C, object>> C.<>o__0.<>p__0""
  IL_0059:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, C, object, C, object>> C.<>o__0.<>p__0""
  IL_005e:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, C, object, C, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, C, object, C, object>>.Target""
  IL_0063:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, C, object, C, object>> C.<>o__0.<>p__0""
  IL_0068:  ldarg.0
  IL_0069:  ldnull
  IL_006a:  ldarg.0
  IL_006b:  ldarg.2
  IL_006c:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, C, object, C, object>.Invoke(System.Runtime.CompilerServices.CallSite, C, object, C, object)""
  IL_0071:  ret
}
");
        }

        [Fact]
        [WorkItem(622532, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/622532")]
        public void InvokeMember_Static_Outer()
        {
            string source = @"
using System;

public class A
{
    public static void M(int x) { }
  
    public class B
    {
        public void F()
        {
            dynamic d = null;
            M(d);
        }
    } 
}";
            // Dev11 passes "this" to the site, which is wrong.
            CompileAndVerifyIL(source, "A.B.F", @"
{
  // Code size      104 (0x68)
  .maxstack  9
  .locals init (object V_0) //d
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> A.B.<>o__0.<>p__0""
  IL_0007:  brtrue.s   IL_0048
  IL_0009:  ldc.i4     0x100
  IL_000e:  ldstr      ""M""
  IL_0013:  ldnull
  IL_0014:  ldtoken    ""A.B""
  IL_0019:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001e:  ldc.i4.2
  IL_001f:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0024:  dup
  IL_0025:  ldc.i4.0
  IL_0026:  ldc.i4.s   33
  IL_0028:  ldnull
  IL_0029:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002e:  stelem.ref
  IL_002f:  dup
  IL_0030:  ldc.i4.1
  IL_0031:  ldc.i4.0
  IL_0032:  ldnull
  IL_0033:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0038:  stelem.ref
  IL_0039:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003e:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0043:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> A.B.<>o__0.<>p__0""
  IL_0048:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> A.B.<>o__0.<>p__0""
  IL_004d:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>>.Target""
  IL_0052:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> A.B.<>o__0.<>p__0""
  IL_0057:  ldtoken    ""A""
  IL_005c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0061:  ldloc.0
  IL_0062:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, object)""
  IL_0067:  ret
}
");
        }

        [Fact]
        [WorkItem(622532, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/622532")]
        public void InvokeMember_Static_Outer_AmbiguousAtRuntime()
        {
            string source = @"
using System;

public class A
{
    public static void M(A x) { }
    public void M(string x) { }
  
    public class B
    {
        public void F()
        {
            dynamic d = null;
            M(d);
        }
    }  
    
    public static void Main() 
    {
        System.Globalization.CultureInfo saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

        try 
        {
            new A.B().F();
        }
        catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
        {
            System.Console.WriteLine(e.Message);
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture;
        }
    }
}";
            // Dev11 passes "this" to the site, which is wrong.
            CompileAndVerifyIL(source, "A.B.F", @"
{
  // Code size      104 (0x68)
  .maxstack  9
  .locals init (object V_0) //d
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> A.B.<>o__0.<>p__0""
  IL_0007:  brtrue.s   IL_0048
  IL_0009:  ldc.i4     0x100
  IL_000e:  ldstr      ""M""
  IL_0013:  ldnull
  IL_0014:  ldtoken    ""A.B""
  IL_0019:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001e:  ldc.i4.2
  IL_001f:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0024:  dup
  IL_0025:  ldc.i4.0
  IL_0026:  ldc.i4.s   33
  IL_0028:  ldnull
  IL_0029:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002e:  stelem.ref
  IL_002f:  dup
  IL_0030:  ldc.i4.1
  IL_0031:  ldc.i4.0
  IL_0032:  ldnull
  IL_0033:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0038:  stelem.ref
  IL_0039:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003e:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0043:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> A.B.<>o__0.<>p__0""
  IL_0048:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> A.B.<>o__0.<>p__0""
  IL_004d:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>>.Target""
  IL_0052:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> A.B.<>o__0.<>p__0""
  IL_0057:  ldtoken    ""A""
  IL_005c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0061:  ldloc.0
  IL_0062:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, object)""
  IL_0067:  ret
}");

            CompileAndVerify(source,
                new[] { SystemCoreRef, CSharpRef },
                expectedOutput: "The call is ambiguous between the following methods or properties: 'A.M(A)' and 'A.M(string)'");
        }

        [Fact, WorkItem(649805, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/649805")]
        public void InvokeMember_ColorColor_StaticContext_StaticProperty()
        {
            string source = @"
public class Color
{
    public static void M1(string s) { }
    public static void M1(int s) { }
    
    public static void M2(string s) { }
    public void M2(int s) { }
    public void M2(int s, int q) { }
  
    public void M3(int s) { }
    public static void M3(int s, int q) { }
}

public class C
{
    public static Color Color { get; set; }
    
    public static void F(dynamic d)
    {
        Color.M1(d); 
        Color.M2(d); 
        Color.M3(d);
    }
}                    
";
            CompileAndVerifyIL(source, "C.F", @"
{
  // Code size      286 (0x11e)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__0""
  IL_0005:  brtrue.s   IL_0045
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldstr      ""M1""
  IL_0011:  ldnull
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.1
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  dup
  IL_002d:  ldc.i4.1
  IL_002e:  ldc.i4.0
  IL_002f:  ldnull
  IL_0030:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0035:  stelem.ref
  IL_0036:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003b:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0040:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__0""
  IL_0045:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__0""
  IL_004a:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, Color, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Target""
  IL_004f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__0""
  IL_0054:  call       ""Color C.Color.get""
  IL_0059:  ldarg.0
  IL_005a:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, Color, object>.Invoke(System.Runtime.CompilerServices.CallSite, Color, object)""
  IL_005f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__1""
  IL_0064:  brtrue.s   IL_00a4
  IL_0066:  ldc.i4     0x100
  IL_006b:  ldstr      ""M2""
  IL_0070:  ldnull
  IL_0071:  ldtoken    ""C""
  IL_0076:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_007b:  ldc.i4.2
  IL_007c:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0081:  dup
  IL_0082:  ldc.i4.0
  IL_0083:  ldc.i4.1
  IL_0084:  ldnull
  IL_0085:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_008a:  stelem.ref
  IL_008b:  dup
  IL_008c:  ldc.i4.1
  IL_008d:  ldc.i4.0
  IL_008e:  ldnull
  IL_008f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0094:  stelem.ref
  IL_0095:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_009a:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_009f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__1""
  IL_00a4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__1""
  IL_00a9:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, Color, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Target""
  IL_00ae:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__1""
  IL_00b3:  call       ""Color C.Color.get""
  IL_00b8:  ldarg.0
  IL_00b9:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, Color, object>.Invoke(System.Runtime.CompilerServices.CallSite, Color, object)""
  IL_00be:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__2""
  IL_00c3:  brtrue.s   IL_0103
  IL_00c5:  ldc.i4     0x100
  IL_00ca:  ldstr      ""M3""
  IL_00cf:  ldnull
  IL_00d0:  ldtoken    ""C""
  IL_00d5:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00da:  ldc.i4.2
  IL_00db:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00e0:  dup
  IL_00e1:  ldc.i4.0
  IL_00e2:  ldc.i4.1
  IL_00e3:  ldnull
  IL_00e4:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00e9:  stelem.ref
  IL_00ea:  dup
  IL_00eb:  ldc.i4.1
  IL_00ec:  ldc.i4.0
  IL_00ed:  ldnull
  IL_00ee:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00f3:  stelem.ref
  IL_00f4:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00f9:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00fe:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__2""
  IL_0103:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__2""
  IL_0108:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, Color, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Target""
  IL_010d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__2""
  IL_0112:  call       ""Color C.Color.get""
  IL_0117:  ldarg.0
  IL_0118:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, Color, object>.Invoke(System.Runtime.CompilerServices.CallSite, Color, object)""
  IL_011d:  ret
}
");
        }

        [Fact, WorkItem(649805, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/649805")]
        public void InvokeMember_ColorColor_StaticContext_InstanceProperty()
        {
            string source = @"
public class Color
{
    public static void M1(string s) { }
    public static void M1(int s) { }
    
    public static void M2(string s) { }
    public void M2(int s) { }
    public void M2(int s, int q) { }
  
    public void M3(int s) { }
    public static void M3(int s, int q) { }
}

public class C
{
    public Color Color { get; set; }
    
    public static void F(dynamic d)
    {
        Color.M1(d);
        Color.M2(d);
        Color.M3(d);   // Dev11 crashes on this one
    }
}                    
";
            CompileAndVerifyIL(source, "C.F", @"
{
  // Code size      304 (0x130)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> C.<>o__4.<>p__0""
  IL_0005:  brtrue.s   IL_0046
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldstr      ""M1""
  IL_0011:  ldnull
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.s   33
  IL_0026:  ldnull
  IL_0027:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002c:  stelem.ref
  IL_002d:  dup
  IL_002e:  ldc.i4.1
  IL_002f:  ldc.i4.0
  IL_0030:  ldnull
  IL_0031:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0036:  stelem.ref
  IL_0037:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003c:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0041:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> C.<>o__4.<>p__0""
  IL_0046:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> C.<>o__4.<>p__0""
  IL_004b:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>>.Target""
  IL_0050:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> C.<>o__4.<>p__0""
  IL_0055:  ldtoken    ""Color""
  IL_005a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005f:  ldarg.0
  IL_0060:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, object)""
  IL_0065:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> C.<>o__4.<>p__1""
  IL_006a:  brtrue.s   IL_00ab
  IL_006c:  ldc.i4     0x100
  IL_0071:  ldstr      ""M2""
  IL_0076:  ldnull
  IL_0077:  ldtoken    ""C""
  IL_007c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0081:  ldc.i4.2
  IL_0082:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0087:  dup
  IL_0088:  ldc.i4.0
  IL_0089:  ldc.i4.s   33
  IL_008b:  ldnull
  IL_008c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0091:  stelem.ref
  IL_0092:  dup
  IL_0093:  ldc.i4.1
  IL_0094:  ldc.i4.0
  IL_0095:  ldnull
  IL_0096:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_009b:  stelem.ref
  IL_009c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00a1:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00a6:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> C.<>o__4.<>p__1""
  IL_00ab:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> C.<>o__4.<>p__1""
  IL_00b0:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>>.Target""
  IL_00b5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> C.<>o__4.<>p__1""
  IL_00ba:  ldtoken    ""Color""
  IL_00bf:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00c4:  ldarg.0
  IL_00c5:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, object)""
  IL_00ca:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> C.<>o__4.<>p__2""
  IL_00cf:  brtrue.s   IL_0110
  IL_00d1:  ldc.i4     0x100
  IL_00d6:  ldstr      ""M3""
  IL_00db:  ldnull
  IL_00dc:  ldtoken    ""C""
  IL_00e1:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00e6:  ldc.i4.2
  IL_00e7:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00ec:  dup
  IL_00ed:  ldc.i4.0
  IL_00ee:  ldc.i4.s   33
  IL_00f0:  ldnull
  IL_00f1:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00f6:  stelem.ref
  IL_00f7:  dup
  IL_00f8:  ldc.i4.1
  IL_00f9:  ldc.i4.0
  IL_00fa:  ldnull
  IL_00fb:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0100:  stelem.ref
  IL_0101:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0106:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_010b:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> C.<>o__4.<>p__2""
  IL_0110:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> C.<>o__4.<>p__2""
  IL_0115:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>>.Target""
  IL_011a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> C.<>o__4.<>p__2""
  IL_011f:  ldtoken    ""Color""
  IL_0124:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0129:  ldarg.0
  IL_012a:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, object)""
  IL_012f:  ret
}
");
        }

        [Fact, WorkItem(649805, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/649805")]
        public void InvokeMember_ColorColor_StaticContext_Parameter()
        {
            string source = @"
public class Color
{
    public static void M1(string s) { }
    public static void M1(int s) { }
    
    public static void M2(string s) { }
    public void M2(int s) { }
    public void M2(int s, int q) { }
  
    public void M3(int s) { }
    public static void M3(int s, int q) { }
}

public class C
{
    public static void F(Color Color, dynamic d)
    {
        Color.M1(d);
        Color.M2(d);
        Color.M3(d);   // Dev11 crashes on this one
    }
}                    
";
            CompileAndVerifyIL(source, "C.F", @"
{
  // Code size      274 (0x112)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0045
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldstr      ""M1""
  IL_0011:  ldnull
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.1
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  dup
  IL_002d:  ldc.i4.1
  IL_002e:  ldc.i4.0
  IL_002f:  ldnull
  IL_0030:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0035:  stelem.ref
  IL_0036:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003b:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0040:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__0.<>p__0""
  IL_0045:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__0.<>p__0""
  IL_004a:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, Color, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Target""
  IL_004f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__0.<>p__0""
  IL_0054:  ldarg.0
  IL_0055:  ldarg.1
  IL_0056:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, Color, object>.Invoke(System.Runtime.CompilerServices.CallSite, Color, object)""
  IL_005b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__0.<>p__1""
  IL_0060:  brtrue.s   IL_00a0
  IL_0062:  ldc.i4     0x100
  IL_0067:  ldstr      ""M2""
  IL_006c:  ldnull
  IL_006d:  ldtoken    ""C""
  IL_0072:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0077:  ldc.i4.2
  IL_0078:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_007d:  dup
  IL_007e:  ldc.i4.0
  IL_007f:  ldc.i4.1
  IL_0080:  ldnull
  IL_0081:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0086:  stelem.ref
  IL_0087:  dup
  IL_0088:  ldc.i4.1
  IL_0089:  ldc.i4.0
  IL_008a:  ldnull
  IL_008b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0090:  stelem.ref
  IL_0091:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0096:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_009b:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__0.<>p__1""
  IL_00a0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__0.<>p__1""
  IL_00a5:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, Color, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Target""
  IL_00aa:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__0.<>p__1""
  IL_00af:  ldarg.0
  IL_00b0:  ldarg.1
  IL_00b1:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, Color, object>.Invoke(System.Runtime.CompilerServices.CallSite, Color, object)""
  IL_00b6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__0.<>p__2""
  IL_00bb:  brtrue.s   IL_00fb
  IL_00bd:  ldc.i4     0x100
  IL_00c2:  ldstr      ""M3""
  IL_00c7:  ldnull
  IL_00c8:  ldtoken    ""C""
  IL_00cd:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00d2:  ldc.i4.2
  IL_00d3:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00d8:  dup
  IL_00d9:  ldc.i4.0
  IL_00da:  ldc.i4.1
  IL_00db:  ldnull
  IL_00dc:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00e1:  stelem.ref
  IL_00e2:  dup
  IL_00e3:  ldc.i4.1
  IL_00e4:  ldc.i4.0
  IL_00e5:  ldnull
  IL_00e6:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00eb:  stelem.ref
  IL_00ec:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00f1:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00f6:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__0.<>p__2""
  IL_00fb:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__0.<>p__2""
  IL_0100:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, Color, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Target""
  IL_0105:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__0.<>p__2""
  IL_010a:  ldarg.0
  IL_010b:  ldarg.1
  IL_010c:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, Color, object>.Invoke(System.Runtime.CompilerServices.CallSite, Color, object)""
  IL_0111:  ret
}
");
        }

        [Fact, WorkItem(649805, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/649805")]
        public void InvokeMember_ColorColor_InstanceContext_StaticProperty()
        {
            string source = @"
public class Color
{
    public static void M1(string s) { }
    public static void M1(int s) { }
    
    public static void M2(string s) { }
    public void M2(int s) { }
    public void M2(int s, int q) { }
  
    public void M3(int s) { }
    public static void M3(int s, int q) { }
}

public class C
{
    public static Color Color { get; set; }
    
    public void F(dynamic d)
    {
        Color.M1(d);
        Color.M2(d);
        Color.M3(d);
    }
}                    
";
            CompileAndVerifyIL(source, "C.F", @"
{
  // Code size      286 (0x11e)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__0""
  IL_0005:  brtrue.s   IL_0045
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldstr      ""M1""
  IL_0011:  ldnull
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.1
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  dup
  IL_002d:  ldc.i4.1
  IL_002e:  ldc.i4.0
  IL_002f:  ldnull
  IL_0030:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0035:  stelem.ref
  IL_0036:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003b:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0040:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__0""
  IL_0045:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__0""
  IL_004a:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, Color, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Target""
  IL_004f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__0""
  IL_0054:  call       ""Color C.Color.get""
  IL_0059:  ldarg.1
  IL_005a:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, Color, object>.Invoke(System.Runtime.CompilerServices.CallSite, Color, object)""
  IL_005f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__1""
  IL_0064:  brtrue.s   IL_00a4
  IL_0066:  ldc.i4     0x100
  IL_006b:  ldstr      ""M2""
  IL_0070:  ldnull
  IL_0071:  ldtoken    ""C""
  IL_0076:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_007b:  ldc.i4.2
  IL_007c:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0081:  dup
  IL_0082:  ldc.i4.0
  IL_0083:  ldc.i4.1
  IL_0084:  ldnull
  IL_0085:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_008a:  stelem.ref
  IL_008b:  dup
  IL_008c:  ldc.i4.1
  IL_008d:  ldc.i4.0
  IL_008e:  ldnull
  IL_008f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0094:  stelem.ref
  IL_0095:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_009a:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_009f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__1""
  IL_00a4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__1""
  IL_00a9:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, Color, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Target""
  IL_00ae:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__1""
  IL_00b3:  call       ""Color C.Color.get""
  IL_00b8:  ldarg.1
  IL_00b9:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, Color, object>.Invoke(System.Runtime.CompilerServices.CallSite, Color, object)""
  IL_00be:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__2""
  IL_00c3:  brtrue.s   IL_0103
  IL_00c5:  ldc.i4     0x100
  IL_00ca:  ldstr      ""M3""
  IL_00cf:  ldnull
  IL_00d0:  ldtoken    ""C""
  IL_00d5:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00da:  ldc.i4.2
  IL_00db:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00e0:  dup
  IL_00e1:  ldc.i4.0
  IL_00e2:  ldc.i4.1
  IL_00e3:  ldnull
  IL_00e4:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00e9:  stelem.ref
  IL_00ea:  dup
  IL_00eb:  ldc.i4.1
  IL_00ec:  ldc.i4.0
  IL_00ed:  ldnull
  IL_00ee:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00f3:  stelem.ref
  IL_00f4:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00f9:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00fe:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__2""
  IL_0103:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__2""
  IL_0108:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, Color, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Target""
  IL_010d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__2""
  IL_0112:  call       ""Color C.Color.get""
  IL_0117:  ldarg.1
  IL_0118:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, Color, object>.Invoke(System.Runtime.CompilerServices.CallSite, Color, object)""
  IL_011d:  ret
}
");
        }

        [Fact, WorkItem(649805, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/649805")]
        public void InvokeMember_ColorColor_InstanceContext_Parameter()
        {
            string source = @"
public class Color
{
    public static void M1(string s) { }
    public static void M1(int s) { }
    
    public static void M2(string s) { }
    public void M2(int s) { }
    public void M2(int s, int q) { }
  
    public void M3(int s) { }
    public static void M3(int s, int q) { }
}

public class C
{
    public void F(Color Color, dynamic d)
    {
        Color.M1(d);
        Color.M2(d);
        Color.M3(d);
    }
}                    
";
            CompileAndVerifyIL(source, "C.F", @"
{
  // Code size      274 (0x112)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0045
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldstr      ""M1""
  IL_0011:  ldnull
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.1
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  dup
  IL_002d:  ldc.i4.1
  IL_002e:  ldc.i4.0
  IL_002f:  ldnull
  IL_0030:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0035:  stelem.ref
  IL_0036:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003b:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0040:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__0.<>p__0""
  IL_0045:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__0.<>p__0""
  IL_004a:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, Color, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Target""
  IL_004f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__0.<>p__0""
  IL_0054:  ldarg.1
  IL_0055:  ldarg.2
  IL_0056:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, Color, object>.Invoke(System.Runtime.CompilerServices.CallSite, Color, object)""
  IL_005b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__0.<>p__1""
  IL_0060:  brtrue.s   IL_00a0
  IL_0062:  ldc.i4     0x100
  IL_0067:  ldstr      ""M2""
  IL_006c:  ldnull
  IL_006d:  ldtoken    ""C""
  IL_0072:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0077:  ldc.i4.2
  IL_0078:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_007d:  dup
  IL_007e:  ldc.i4.0
  IL_007f:  ldc.i4.1
  IL_0080:  ldnull
  IL_0081:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0086:  stelem.ref
  IL_0087:  dup
  IL_0088:  ldc.i4.1
  IL_0089:  ldc.i4.0
  IL_008a:  ldnull
  IL_008b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0090:  stelem.ref
  IL_0091:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0096:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_009b:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__0.<>p__1""
  IL_00a0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__0.<>p__1""
  IL_00a5:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, Color, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Target""
  IL_00aa:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__0.<>p__1""
  IL_00af:  ldarg.1
  IL_00b0:  ldarg.2
  IL_00b1:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, Color, object>.Invoke(System.Runtime.CompilerServices.CallSite, Color, object)""
  IL_00b6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__0.<>p__2""
  IL_00bb:  brtrue.s   IL_00fb
  IL_00bd:  ldc.i4     0x100
  IL_00c2:  ldstr      ""M3""
  IL_00c7:  ldnull
  IL_00c8:  ldtoken    ""C""
  IL_00cd:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00d2:  ldc.i4.2
  IL_00d3:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00d8:  dup
  IL_00d9:  ldc.i4.0
  IL_00da:  ldc.i4.1
  IL_00db:  ldnull
  IL_00dc:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00e1:  stelem.ref
  IL_00e2:  dup
  IL_00e3:  ldc.i4.1
  IL_00e4:  ldc.i4.0
  IL_00e5:  ldnull
  IL_00e6:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00eb:  stelem.ref
  IL_00ec:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00f1:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00f6:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__0.<>p__2""
  IL_00fb:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__0.<>p__2""
  IL_0100:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, Color, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Target""
  IL_0105:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__0.<>p__2""
  IL_010a:  ldarg.1
  IL_010b:  ldarg.2
  IL_010c:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, Color, object>.Invoke(System.Runtime.CompilerServices.CallSite, Color, object)""
  IL_0111:  ret
}
");
        }

        [Fact, WorkItem(649805, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/649805")]
        public void InvokeMember_ColorColor_InstanceContext_InstanceProperty()
        {
            string source = @"
public class Color
{
    public static void M1(string s) { }
    public static void M1(int s) { }
    
    public static void M2(string s) { }
    public void M2(int s) { }
    public void M2(int s, int q) { }
  
    public void M3(int s) { }
    public static void M3(int s, int q) { }
}

public class C
{
    public Color Color { get; set; }
    
    public void F(dynamic d)
    {
        Color.M1(d);
        Color.M2(d);
        Color.M3(d);
    }
}                    
";
            CompileAndVerifyIL(source, "C.F", @"
{
  // Code size      289 (0x121)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__0""
  IL_0005:  brtrue.s   IL_0045
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldstr      ""M1""
  IL_0011:  ldnull
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.1
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  dup
  IL_002d:  ldc.i4.1
  IL_002e:  ldc.i4.0
  IL_002f:  ldnull
  IL_0030:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0035:  stelem.ref
  IL_0036:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003b:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0040:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__0""
  IL_0045:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__0""
  IL_004a:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, Color, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Target""
  IL_004f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__0""
  IL_0054:  ldarg.0
  IL_0055:  call       ""Color C.Color.get""
  IL_005a:  ldarg.1
  IL_005b:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, Color, object>.Invoke(System.Runtime.CompilerServices.CallSite, Color, object)""
  IL_0060:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__1""
  IL_0065:  brtrue.s   IL_00a5
  IL_0067:  ldc.i4     0x100
  IL_006c:  ldstr      ""M2""
  IL_0071:  ldnull
  IL_0072:  ldtoken    ""C""
  IL_0077:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_007c:  ldc.i4.2
  IL_007d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0082:  dup
  IL_0083:  ldc.i4.0
  IL_0084:  ldc.i4.1
  IL_0085:  ldnull
  IL_0086:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_008b:  stelem.ref
  IL_008c:  dup
  IL_008d:  ldc.i4.1
  IL_008e:  ldc.i4.0
  IL_008f:  ldnull
  IL_0090:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0095:  stelem.ref
  IL_0096:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_009b:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00a0:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__1""
  IL_00a5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__1""
  IL_00aa:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, Color, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Target""
  IL_00af:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__1""
  IL_00b4:  ldarg.0
  IL_00b5:  call       ""Color C.Color.get""
  IL_00ba:  ldarg.1
  IL_00bb:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, Color, object>.Invoke(System.Runtime.CompilerServices.CallSite, Color, object)""
  IL_00c0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__2""
  IL_00c5:  brtrue.s   IL_0105
  IL_00c7:  ldc.i4     0x100
  IL_00cc:  ldstr      ""M3""
  IL_00d1:  ldnull
  IL_00d2:  ldtoken    ""C""
  IL_00d7:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00dc:  ldc.i4.2
  IL_00dd:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00e2:  dup
  IL_00e3:  ldc.i4.0
  IL_00e4:  ldc.i4.1
  IL_00e5:  ldnull
  IL_00e6:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00eb:  stelem.ref
  IL_00ec:  dup
  IL_00ed:  ldc.i4.1
  IL_00ee:  ldc.i4.0
  IL_00ef:  ldnull
  IL_00f0:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00f5:  stelem.ref
  IL_00f6:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00fb:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0100:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__2""
  IL_0105:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__2""
  IL_010a:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, Color, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>>.Target""
  IL_010f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, Color, object>> C.<>o__4.<>p__2""
  IL_0114:  ldarg.0
  IL_0115:  call       ""Color C.Color.get""
  IL_011a:  ldarg.1
  IL_011b:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, Color, object>.Invoke(System.Runtime.CompilerServices.CallSite, Color, object)""
  IL_0120:  ret
}");
        }

        [Fact, WorkItem(649805, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/649805")]
        public void InvokeMember_ColorColor_InFieldInitializer()
        {
            string source = @"
public class Color
{
	public int F(int a) { return 1; } 
}

public class C
{
	Color Color;
	dynamic x = Color.F((dynamic)1);
}
";
            CompileAndVerifyIL(source, "C..ctor", @"
{
  // Code size      115 (0x73)
  .maxstack  10
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, object, object>> C.<>o__2.<>p__0""
  IL_0006:  brtrue.s   IL_0043
  IL_0008:  ldc.i4.0
  IL_0009:  ldstr      ""F""
  IL_000e:  ldnull
  IL_000f:  ldtoken    ""C""
  IL_0014:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0019:  ldc.i4.2
  IL_001a:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001f:  dup
  IL_0020:  ldc.i4.0
  IL_0021:  ldc.i4.s   33
  IL_0023:  ldnull
  IL_0024:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0029:  stelem.ref
  IL_002a:  dup
  IL_002b:  ldc.i4.1
  IL_002c:  ldc.i4.0
  IL_002d:  ldnull
  IL_002e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0033:  stelem.ref
  IL_0034:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0039:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_003e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, object, object>> C.<>o__2.<>p__0""
  IL_0043:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, object, object>> C.<>o__2.<>p__0""
  IL_0048:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, System.Type, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, object, object>>.Target""
  IL_004d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, object, object>> C.<>o__2.<>p__0""
  IL_0052:  ldtoken    ""Color""
  IL_0057:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005c:  ldc.i4.1
  IL_005d:  box        ""int""
  IL_0062:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, System.Type, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, object)""
  IL_0067:  stfld      ""dynamic C.x""
  IL_006c:  ldarg.0
  IL_006d:  call       ""object..ctor()""
  IL_0072:  ret
}
");
        }

        [Fact, WorkItem(649805, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/649805")]
        public void InvokeMember_ColorColor_InScriptVariableInitializer()
        {
            var sourceLib = @"
public class Color
{
	public int F(int a) { return 1; } 
}
";
            var lib = CreateCompilationWithMscorlib(sourceLib);

            string sourceScript = @"
Color Color;
dynamic x = Color.F((dynamic)1);
";
            var script = CreateCompilationWithMscorlib45(
                new[] { Parse(sourceScript, options: TestOptions.Script) },
                new[] { new CSharpCompilationReference(lib), SystemCoreRef, CSharpRef });

            var verifier = CompileAndVerify(script);
            verifier.VerifyIL("<<Initialize>>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()",
@"{
  // Code size      161 (0xa1)
  .maxstack  10
  .locals init (object V_0,
                System.Exception V_1)
  .try
  {
    IL_0000:  ldarg.0
    IL_0001:  ldfld      ""Script Script.<<Initialize>>d__0.<>4__this""
    IL_0006:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, Color, object, object>> Script.<>o__0.<>p__0""
    IL_000b:  brtrue.s   IL_0047
    IL_000d:  ldc.i4.0
    IL_000e:  ldstr      ""F""
    IL_0013:  ldnull
    IL_0014:  ldtoken    ""Script""
    IL_0019:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_001e:  ldc.i4.2
    IL_001f:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
    IL_0024:  dup
    IL_0025:  ldc.i4.0
    IL_0026:  ldc.i4.1
    IL_0027:  ldnull
    IL_0028:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
    IL_002d:  stelem.ref
    IL_002e:  dup
    IL_002f:  ldc.i4.1
    IL_0030:  ldc.i4.0
    IL_0031:  ldnull
    IL_0032:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
    IL_0037:  stelem.ref
    IL_0038:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
    IL_003d:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, Color, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, Color, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_0042:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, Color, object, object>> Script.<>o__0.<>p__0""
    IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, Color, object, object>> Script.<>o__0.<>p__0""
    IL_004c:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, Color, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, Color, object, object>>.Target""
    IL_0051:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, Color, object, object>> Script.<>o__0.<>p__0""
    IL_0056:  ldarg.0
    IL_0057:  ldfld      ""Script Script.<<Initialize>>d__0.<>4__this""
    IL_005c:  ldfld      ""Color Script.Color""
    IL_0061:  ldc.i4.1
    IL_0062:  box        ""int""
    IL_0067:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, Color, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, Color, object)""
    IL_006c:  stfld      ""dynamic Script.x""
    IL_0071:  ldnull
    IL_0072:  stloc.0
    IL_0073:  leave.s    IL_008c
  }
  catch System.Exception
  {
    IL_0075:  stloc.1
    IL_0076:  ldarg.0
    IL_0077:  ldc.i4.s   -2
    IL_0079:  stfld      ""int Script.<<Initialize>>d__0.<>1__state""
    IL_007e:  ldarg.0
    IL_007f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> Script.<<Initialize>>d__0.<>t__builder""
    IL_0084:  ldloc.1
    IL_0085:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.SetException(System.Exception)""
    IL_008a:  leave.s    IL_00a0
  }
  IL_008c:  ldarg.0
  IL_008d:  ldc.i4.s   -2
  IL_008f:  stfld      ""int Script.<<Initialize>>d__0.<>1__state""
  IL_0094:  ldarg.0
  IL_0095:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> Script.<<Initialize>>d__0.<>t__builder""
  IL_009a:  ldloc.0
  IL_009b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.SetResult(object)""
  IL_00a0:  ret
}", realIL: true);
        }

        [Fact, WorkItem(649805, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/649805")]
        public void InvokeMember_ColorColor_InScriptMethod()
        {
            var sourceLib = @"
public class Color
{
	public int F(int a) { return 1; } 
}
";
            var lib = CreateCompilationWithMscorlib(sourceLib);

            string sourceScript = @"
Color Color;

void Foo() 
{
    dynamic x = Color.F((dynamic)1);
}
";
            var script = CreateCompilationWithMscorlib45(
                new[] { Parse(sourceScript, options: TestOptions.Script) },
                new[] { new CSharpCompilationReference(lib), SystemCoreRef, CSharpRef },
                TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));

            CompileAndVerify(script).VerifyIL("Foo", @"
{
  // Code size       99 (0x63)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, Color, object, object>> Script.<>o__2.<>p__0""
  IL_0005:  brtrue.s   IL_0041
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      ""F""
  IL_000d:  ldnull
  IL_000e:  ldtoken    ""Script""
  IL_0013:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0018:  ldc.i4.2
  IL_0019:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001e:  dup
  IL_001f:  ldc.i4.0
  IL_0020:  ldc.i4.1
  IL_0021:  ldnull
  IL_0022:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0027:  stelem.ref
  IL_0028:  dup
  IL_0029:  ldc.i4.1
  IL_002a:  ldc.i4.0
  IL_002b:  ldnull
  IL_002c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0031:  stelem.ref
  IL_0032:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0037:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, Color, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, Color, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_003c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, Color, object, object>> Script.<>o__2.<>p__0""
  IL_0041:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, Color, object, object>> Script.<>o__2.<>p__0""
  IL_0046:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, Color, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, Color, object, object>>.Target""
  IL_004b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, Color, object, object>> Script.<>o__2.<>p__0""
  IL_0050:  ldarg.0
  IL_0051:  ldfld      ""Color Script.Color""
  IL_0056:  ldc.i4.1
  IL_0057:  box        ""int""
  IL_005c:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, Color, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, Color, object)""
  IL_0061:  pop
  IL_0062:  ret
}
", realIL: true);
        }

        [Fact]
        public void InvokeMember_UseCompileTimeType()
        {
            string source = @"
class C
{
    static char? nChar = null;
    static char Char = '\0';
    static C c = new C();
    static object obj = null;	
    static dynamic d = new C();

    static void M()
    {
        d.f(nChar);
        d.f(Char);        
        d.f(c);         
        d.f(obj);           
        d.f(d);  
        d.f(null);
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      591 (0x24f)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, char?>> C.<>o__5.<>p__0""
  IL_0005:  brtrue.s   IL_0045
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldstr      ""f""
  IL_0011:  ldnull
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.0
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  dup
  IL_002d:  ldc.i4.1
  IL_002e:  ldc.i4.1
  IL_002f:  ldnull
  IL_0030:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0035:  stelem.ref
  IL_0036:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003b:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, char?>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, char?>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0040:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, char?>> C.<>o__5.<>p__0""
  IL_0045:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, char?>> C.<>o__5.<>p__0""
  IL_004a:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, object, char?> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, char?>>.Target""
  IL_004f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, char?>> C.<>o__5.<>p__0""
  IL_0054:  ldsfld     ""dynamic C.d""
  IL_0059:  ldsfld     ""char? C.nChar""
  IL_005e:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, object, char?>.Invoke(System.Runtime.CompilerServices.CallSite, object, char?)""
  IL_0063:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, char>> C.<>o__5.<>p__1""
  IL_0068:  brtrue.s   IL_00a8
  IL_006a:  ldc.i4     0x100
  IL_006f:  ldstr      ""f""
  IL_0074:  ldnull
  IL_0075:  ldtoken    ""C""
  IL_007a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_007f:  ldc.i4.2
  IL_0080:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0085:  dup
  IL_0086:  ldc.i4.0
  IL_0087:  ldc.i4.0
  IL_0088:  ldnull
  IL_0089:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_008e:  stelem.ref
  IL_008f:  dup
  IL_0090:  ldc.i4.1
  IL_0091:  ldc.i4.1
  IL_0092:  ldnull
  IL_0093:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0098:  stelem.ref
  IL_0099:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_009e:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, char>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, char>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00a3:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, char>> C.<>o__5.<>p__1""
  IL_00a8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, char>> C.<>o__5.<>p__1""
  IL_00ad:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, object, char> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, char>>.Target""
  IL_00b2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, char>> C.<>o__5.<>p__1""
  IL_00b7:  ldsfld     ""dynamic C.d""
  IL_00bc:  ldsfld     ""char C.Char""
  IL_00c1:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, object, char>.Invoke(System.Runtime.CompilerServices.CallSite, object, char)""
  IL_00c6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, C>> C.<>o__5.<>p__2""
  IL_00cb:  brtrue.s   IL_010b
  IL_00cd:  ldc.i4     0x100
  IL_00d2:  ldstr      ""f""
  IL_00d7:  ldnull
  IL_00d8:  ldtoken    ""C""
  IL_00dd:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00e2:  ldc.i4.2
  IL_00e3:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00e8:  dup
  IL_00e9:  ldc.i4.0
  IL_00ea:  ldc.i4.0
  IL_00eb:  ldnull
  IL_00ec:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00f1:  stelem.ref
  IL_00f2:  dup
  IL_00f3:  ldc.i4.1
  IL_00f4:  ldc.i4.1
  IL_00f5:  ldnull
  IL_00f6:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00fb:  stelem.ref
  IL_00fc:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0101:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, C>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, C>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0106:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, C>> C.<>o__5.<>p__2""
  IL_010b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, C>> C.<>o__5.<>p__2""
  IL_0110:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, object, C> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, C>>.Target""
  IL_0115:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, C>> C.<>o__5.<>p__2""
  IL_011a:  ldsfld     ""dynamic C.d""
  IL_011f:  ldsfld     ""C C.c""
  IL_0124:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, object, C>.Invoke(System.Runtime.CompilerServices.CallSite, object, C)""
  IL_0129:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__5.<>p__3""
  IL_012e:  brtrue.s   IL_016e
  IL_0130:  ldc.i4     0x100
  IL_0135:  ldstr      ""f""
  IL_013a:  ldnull
  IL_013b:  ldtoken    ""C""
  IL_0140:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0145:  ldc.i4.2
  IL_0146:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_014b:  dup
  IL_014c:  ldc.i4.0
  IL_014d:  ldc.i4.0
  IL_014e:  ldnull
  IL_014f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0154:  stelem.ref
  IL_0155:  dup
  IL_0156:  ldc.i4.1
  IL_0157:  ldc.i4.1
  IL_0158:  ldnull
  IL_0159:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_015e:  stelem.ref
  IL_015f:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0164:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0169:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__5.<>p__3""
  IL_016e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__5.<>p__3""
  IL_0173:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_0178:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__5.<>p__3""
  IL_017d:  ldsfld     ""dynamic C.d""
  IL_0182:  ldsfld     ""object C.obj""
  IL_0187:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_018c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__5.<>p__4""
  IL_0191:  brtrue.s   IL_01d1
  IL_0193:  ldc.i4     0x100
  IL_0198:  ldstr      ""f""
  IL_019d:  ldnull
  IL_019e:  ldtoken    ""C""
  IL_01a3:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_01a8:  ldc.i4.2
  IL_01a9:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_01ae:  dup
  IL_01af:  ldc.i4.0
  IL_01b0:  ldc.i4.0
  IL_01b1:  ldnull
  IL_01b2:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_01b7:  stelem.ref
  IL_01b8:  dup
  IL_01b9:  ldc.i4.1
  IL_01ba:  ldc.i4.0
  IL_01bb:  ldnull
  IL_01bc:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_01c1:  stelem.ref
  IL_01c2:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_01c7:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_01cc:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__5.<>p__4""
  IL_01d1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__5.<>p__4""
  IL_01d6:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_01db:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__5.<>p__4""
  IL_01e0:  ldsfld     ""dynamic C.d""
  IL_01e5:  ldsfld     ""dynamic C.d""
  IL_01ea:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_01ef:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__5.<>p__5""
  IL_01f4:  brtrue.s   IL_0234
  IL_01f6:  ldc.i4     0x100
  IL_01fb:  ldstr      ""f""
  IL_0200:  ldnull
  IL_0201:  ldtoken    ""C""
  IL_0206:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_020b:  ldc.i4.2
  IL_020c:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0211:  dup
  IL_0212:  ldc.i4.0
  IL_0213:  ldc.i4.0
  IL_0214:  ldnull
  IL_0215:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_021a:  stelem.ref
  IL_021b:  dup
  IL_021c:  ldc.i4.1
  IL_021d:  ldc.i4.2
  IL_021e:  ldnull
  IL_021f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0224:  stelem.ref
  IL_0225:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_022a:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_022f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__5.<>p__5""
  IL_0234:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__5.<>p__5""
  IL_0239:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_023e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__5.<>p__5""
  IL_0243:  ldsfld     ""dynamic C.d""
  IL_0248:  ldnull
  IL_0249:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_024e:  ret
}
");
        }

        [Fact]
        public void InvokeMember_UseCompileTimeType_ConvertedReceiver()
        {
            string source = @"
class C
{
    void M(object o) 
    {
        (o as dynamic).f();
    }
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       81 (0x51)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_003b
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldstr      ""f""
  IL_0011:  ldnull
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.1
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.0
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0031:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0036:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object>> C.<>o__0.<>p__0""
  IL_003b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object>> C.<>o__0.<>p__0""
  IL_0040:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object>>.Target""
  IL_0045:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object>> C.<>o__0.<>p__0""
  IL_004a:  ldarg.1
  IL_004b:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0050:  ret
}
");
        }

        [Fact]
        public void InvokeMember_Dynamic_Generic()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return d.m<C, int>(d);
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      119 (0x77)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0060
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      ""m""
  IL_000d:  ldc.i4.2
  IL_000e:  newarr     ""System.Type""
  IL_0013:  dup
  IL_0014:  ldc.i4.0
  IL_0015:  ldtoken    ""C""
  IL_001a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001f:  stelem.ref
  IL_0020:  dup
  IL_0021:  ldc.i4.1
  IL_0022:  ldtoken    ""int""
  IL_0027:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_002c:  stelem.ref
  IL_002d:  ldtoken    ""C""
  IL_0032:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0037:  ldc.i4.2
  IL_0038:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_003d:  dup
  IL_003e:  ldc.i4.0
  IL_003f:  ldc.i4.0
  IL_0040:  ldnull
  IL_0041:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0046:  stelem.ref
  IL_0047:  dup
  IL_0048:  ldc.i4.1
  IL_0049:  ldc.i4.0
  IL_004a:  ldnull
  IL_004b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0050:  stelem.ref
  IL_0051:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0056:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_005b:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0060:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0065:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_006a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_006f:  ldarg.1
  IL_0070:  ldarg.1
  IL_0071:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0076:  ret
}
");
        }

        [Fact]
        public void InvokeMember_Static_Generic()
        {
            string source = @"
public class C
{
    public dynamic M(C c, dynamic d)
    {
        return c.F<int, dynamic>(null, this, d);
    }

    public int F<T1, T2>(C a, C b, double c) 
    {
        return 1; 
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      141 (0x8d)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, C, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0074
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      ""F""
  IL_000d:  ldc.i4.2
  IL_000e:  newarr     ""System.Type""
  IL_0013:  dup
  IL_0014:  ldc.i4.0
  IL_0015:  ldtoken    ""int""
  IL_001a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001f:  stelem.ref
  IL_0020:  dup
  IL_0021:  ldc.i4.1
  IL_0022:  ldtoken    ""object""
  IL_0027:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_002c:  stelem.ref
  IL_002d:  ldtoken    ""C""
  IL_0032:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0037:  ldc.i4.4
  IL_0038:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_003d:  dup
  IL_003e:  ldc.i4.0
  IL_003f:  ldc.i4.1
  IL_0040:  ldnull
  IL_0041:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0046:  stelem.ref
  IL_0047:  dup
  IL_0048:  ldc.i4.1
  IL_0049:  ldc.i4.2
  IL_004a:  ldnull
  IL_004b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0050:  stelem.ref
  IL_0051:  dup
  IL_0052:  ldc.i4.2
  IL_0053:  ldc.i4.1
  IL_0054:  ldnull
  IL_0055:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_005a:  stelem.ref
  IL_005b:  dup
  IL_005c:  ldc.i4.3
  IL_005d:  ldc.i4.0
  IL_005e:  ldnull
  IL_005f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0064:  stelem.ref
  IL_0065:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_006a:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, C, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, C, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_006f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, C, object, object>> C.<>o__0.<>p__0""
  IL_0074:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, C, object, object>> C.<>o__0.<>p__0""
  IL_0079:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, C, object, C, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, C, object, object>>.Target""
  IL_007e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, C, object, object>> C.<>o__0.<>p__0""
  IL_0083:  ldarg.1
  IL_0084:  ldnull
  IL_0085:  ldarg.0
  IL_0086:  ldarg.2
  IL_0087:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, C, object, C, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, C, object, C, object)""
  IL_008c:  ret
}
");
        }

        [Fact]
        public void InvokeMember_Dynamic_NamedArguments()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d, int a)
    {
        return d.m(foo: d, bar: a, baz: 123);
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      123 (0x7b)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, int, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0061
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      ""m""
  IL_000d:  ldnull
  IL_000e:  ldtoken    ""C""
  IL_0013:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0018:  ldc.i4.4
  IL_0019:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001e:  dup
  IL_001f:  ldc.i4.0
  IL_0020:  ldc.i4.0
  IL_0021:  ldnull
  IL_0022:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0027:  stelem.ref
  IL_0028:  dup
  IL_0029:  ldc.i4.1
  IL_002a:  ldc.i4.4
  IL_002b:  ldstr      ""foo""
  IL_0030:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0035:  stelem.ref
  IL_0036:  dup
  IL_0037:  ldc.i4.2
  IL_0038:  ldc.i4.5
  IL_0039:  ldstr      ""bar""
  IL_003e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0043:  stelem.ref
  IL_0044:  dup
  IL_0045:  ldc.i4.3
  IL_0046:  ldc.i4.7
  IL_0047:  ldstr      ""baz""
  IL_004c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0051:  stelem.ref
  IL_0052:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0057:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, int, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, int, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_005c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, int, object>> C.<>o__0.<>p__0""
  IL_0061:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, int, object>> C.<>o__0.<>p__0""
  IL_0066:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, int, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, int, object>>.Target""
  IL_006b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, int, object>> C.<>o__0.<>p__0""
  IL_0070:  ldarg.1
  IL_0071:  ldarg.1
  IL_0072:  ldarg.2
  IL_0073:  ldc.i4.s   123
  IL_0075:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, int, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object, int, int)""
  IL_007a:  ret
}
");
        }

        [Fact]
        [WorkItem(598043, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/598043")]
        public void InvokeMember_NamedArguments_PartialMethods()
        {
            string source = @"
partial class C
{
    static partial void F(int i); 
}

partial class C
{
    static partial void F(int j) { System.Console.WriteLine(j); }

    public static void Main() 
    {
        dynamic d = 2;
        F(i: d);
    }
}
";
            CompileAndVerify(source, additionalRefs: new[] { CSharpRef, SystemCoreRef }, expectedOutput: "2");
        }

        [Fact]
        public void InvokeMember_ManyArgs_F14()
        {
            string source = @"
public class C
{
    public dynamic M14(dynamic d)
    {
        return d.m(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14);
    }
}";
            CompileAndVerifyIL(source, "C.M14", @"
{
  // Code size      247 (0xf7)
  .maxstack  17
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue     IL_00cd
  IL_000a:  ldc.i4.0
  IL_000b:  ldstr      ""m""
  IL_0010:  ldnull
  IL_0011:  ldtoken    ""C""
  IL_0016:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001b:  ldc.i4.s   15
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.0
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  dup
  IL_002d:  ldc.i4.1
  IL_002e:  ldc.i4.3
  IL_002f:  ldnull
  IL_0030:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0035:  stelem.ref
  IL_0036:  dup
  IL_0037:  ldc.i4.2
  IL_0038:  ldc.i4.3
  IL_0039:  ldnull
  IL_003a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_003f:  stelem.ref
  IL_0040:  dup
  IL_0041:  ldc.i4.3
  IL_0042:  ldc.i4.3
  IL_0043:  ldnull
  IL_0044:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0049:  stelem.ref
  IL_004a:  dup
  IL_004b:  ldc.i4.4
  IL_004c:  ldc.i4.3
  IL_004d:  ldnull
  IL_004e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0053:  stelem.ref
  IL_0054:  dup
  IL_0055:  ldc.i4.5
  IL_0056:  ldc.i4.3
  IL_0057:  ldnull
  IL_0058:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_005d:  stelem.ref
  IL_005e:  dup
  IL_005f:  ldc.i4.6
  IL_0060:  ldc.i4.3
  IL_0061:  ldnull
  IL_0062:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0067:  stelem.ref
  IL_0068:  dup
  IL_0069:  ldc.i4.7
  IL_006a:  ldc.i4.3
  IL_006b:  ldnull
  IL_006c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0071:  stelem.ref
  IL_0072:  dup
  IL_0073:  ldc.i4.8
  IL_0074:  ldc.i4.3
  IL_0075:  ldnull
  IL_0076:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_007b:  stelem.ref
  IL_007c:  dup
  IL_007d:  ldc.i4.s   9
  IL_007f:  ldc.i4.3
  IL_0080:  ldnull
  IL_0081:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0086:  stelem.ref
  IL_0087:  dup
  IL_0088:  ldc.i4.s   10
  IL_008a:  ldc.i4.3
  IL_008b:  ldnull
  IL_008c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0091:  stelem.ref
  IL_0092:  dup
  IL_0093:  ldc.i4.s   11
  IL_0095:  ldc.i4.3
  IL_0096:  ldnull
  IL_0097:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_009c:  stelem.ref
  IL_009d:  dup
  IL_009e:  ldc.i4.s   12
  IL_00a0:  ldc.i4.3
  IL_00a1:  ldnull
  IL_00a2:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00a7:  stelem.ref
  IL_00a8:  dup
  IL_00a9:  ldc.i4.s   13
  IL_00ab:  ldc.i4.3
  IL_00ac:  ldnull
  IL_00ad:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00b2:  stelem.ref
  IL_00b3:  dup
  IL_00b4:  ldc.i4.s   14
  IL_00b6:  ldc.i4.3
  IL_00b7:  ldnull
  IL_00b8:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00bd:  stelem.ref
  IL_00be:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00c3:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00c8:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, object>> C.<>o__0.<>p__0""
  IL_00cd:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, object>> C.<>o__0.<>p__0""
  IL_00d2:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, object>>.Target""
  IL_00d7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, object>> C.<>o__0.<>p__0""
  IL_00dc:  ldarg.1
  IL_00dd:  ldc.i4.1
  IL_00de:  ldc.i4.2
  IL_00df:  ldc.i4.3
  IL_00e0:  ldc.i4.4
  IL_00e1:  ldc.i4.5
  IL_00e2:  ldc.i4.6
  IL_00e3:  ldc.i4.7
  IL_00e4:  ldc.i4.8
  IL_00e5:  ldc.i4.s   9
  IL_00e7:  ldc.i4.s   10
  IL_00e9:  ldc.i4.s   11
  IL_00eb:  ldc.i4.s   12
  IL_00ed:  ldc.i4.s   13
  IL_00ef:  ldc.i4.s   14
  IL_00f1:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int)""
  IL_00f6:  ret
}");
        }

        [Fact]
        public void InvokeMember_ManyArgs_F15()
        {
            // TODO: verify metadata of the synthesized delegate
            // TODO: use VerifyRealIL to check that dynamic is erased

            string source = @"
public class C
{
    public dynamic M15(dynamic d)
    {
        return d.m(1, true, 1.0, 'c', ""s"", 
                   (byte)1, (sbyte)1, (uint)1, (long)1, (ulong)1, 
                   (float)1.0, (decimal)1, default(System.DateTime), d, d);
    }
}";
            CompileAndVerifyIL(source, "C.M15", @"
{
  // Code size      284 (0x11c)
  .maxstack  18
  .locals init (System.DateTime V_0)
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F<System.Runtime.CompilerServices.CallSite, object, int, bool, double, char, string, byte, sbyte, uint, long, ulong, float, decimal, System.DateTime, object, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue     IL_00d8
  IL_000a:  ldc.i4.0
  IL_000b:  ldstr      ""m""
  IL_0010:  ldnull
  IL_0011:  ldtoken    ""C""
  IL_0016:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001b:  ldc.i4.s   16
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.0
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  dup
  IL_002d:  ldc.i4.1
  IL_002e:  ldc.i4.3
  IL_002f:  ldnull
  IL_0030:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0035:  stelem.ref
  IL_0036:  dup
  IL_0037:  ldc.i4.2
  IL_0038:  ldc.i4.3
  IL_0039:  ldnull
  IL_003a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_003f:  stelem.ref
  IL_0040:  dup
  IL_0041:  ldc.i4.3
  IL_0042:  ldc.i4.3
  IL_0043:  ldnull
  IL_0044:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0049:  stelem.ref
  IL_004a:  dup
  IL_004b:  ldc.i4.4
  IL_004c:  ldc.i4.3
  IL_004d:  ldnull
  IL_004e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0053:  stelem.ref
  IL_0054:  dup
  IL_0055:  ldc.i4.5
  IL_0056:  ldc.i4.3
  IL_0057:  ldnull
  IL_0058:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_005d:  stelem.ref
  IL_005e:  dup
  IL_005f:  ldc.i4.6
  IL_0060:  ldc.i4.3
  IL_0061:  ldnull
  IL_0062:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0067:  stelem.ref
  IL_0068:  dup
  IL_0069:  ldc.i4.7
  IL_006a:  ldc.i4.3
  IL_006b:  ldnull
  IL_006c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0071:  stelem.ref
  IL_0072:  dup
  IL_0073:  ldc.i4.8
  IL_0074:  ldc.i4.3
  IL_0075:  ldnull
  IL_0076:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_007b:  stelem.ref
  IL_007c:  dup
  IL_007d:  ldc.i4.s   9
  IL_007f:  ldc.i4.3
  IL_0080:  ldnull
  IL_0081:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0086:  stelem.ref
  IL_0087:  dup
  IL_0088:  ldc.i4.s   10
  IL_008a:  ldc.i4.3
  IL_008b:  ldnull
  IL_008c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0091:  stelem.ref
  IL_0092:  dup
  IL_0093:  ldc.i4.s   11
  IL_0095:  ldc.i4.3
  IL_0096:  ldnull
  IL_0097:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_009c:  stelem.ref
  IL_009d:  dup
  IL_009e:  ldc.i4.s   12
  IL_00a0:  ldc.i4.3
  IL_00a1:  ldnull
  IL_00a2:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00a7:  stelem.ref
  IL_00a8:  dup
  IL_00a9:  ldc.i4.s   13
  IL_00ab:  ldc.i4.1
  IL_00ac:  ldnull
  IL_00ad:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00b2:  stelem.ref
  IL_00b3:  dup
  IL_00b4:  ldc.i4.s   14
  IL_00b6:  ldc.i4.0
  IL_00b7:  ldnull
  IL_00b8:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00bd:  stelem.ref
  IL_00be:  dup
  IL_00bf:  ldc.i4.s   15
  IL_00c1:  ldc.i4.0
  IL_00c2:  ldnull
  IL_00c3:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00c8:  stelem.ref
  IL_00c9:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00ce:  call       ""System.Runtime.CompilerServices.CallSite<<>F<System.Runtime.CompilerServices.CallSite, object, int, bool, double, char, string, byte, sbyte, uint, long, ulong, float, decimal, System.DateTime, object, object, object>> System.Runtime.CompilerServices.CallSite<<>F<System.Runtime.CompilerServices.CallSite, object, int, bool, double, char, string, byte, sbyte, uint, long, ulong, float, decimal, System.DateTime, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00d3:  stsfld     ""System.Runtime.CompilerServices.CallSite<<>F<System.Runtime.CompilerServices.CallSite, object, int, bool, double, char, string, byte, sbyte, uint, long, ulong, float, decimal, System.DateTime, object, object, object>> C.<>o__0.<>p__0""
  IL_00d8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F<System.Runtime.CompilerServices.CallSite, object, int, bool, double, char, string, byte, sbyte, uint, long, ulong, float, decimal, System.DateTime, object, object, object>> C.<>o__0.<>p__0""
  IL_00dd:  ldfld      ""<>F<System.Runtime.CompilerServices.CallSite, object, int, bool, double, char, string, byte, sbyte, uint, long, ulong, float, decimal, System.DateTime, object, object, object> System.Runtime.CompilerServices.CallSite<<>F<System.Runtime.CompilerServices.CallSite, object, int, bool, double, char, string, byte, sbyte, uint, long, ulong, float, decimal, System.DateTime, object, object, object>>.Target""
  IL_00e2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F<System.Runtime.CompilerServices.CallSite, object, int, bool, double, char, string, byte, sbyte, uint, long, ulong, float, decimal, System.DateTime, object, object, object>> C.<>o__0.<>p__0""
  IL_00e7:  ldarg.1
  IL_00e8:  ldc.i4.1
  IL_00e9:  ldc.i4.1
  IL_00ea:  ldc.r8     1
  IL_00f3:  ldc.i4.s   99
  IL_00f5:  ldstr      ""s""
  IL_00fa:  ldc.i4.1
  IL_00fb:  ldc.i4.1
  IL_00fc:  ldc.i4.1
  IL_00fd:  ldc.i4.1
  IL_00fe:  conv.i8
  IL_00ff:  ldc.i4.1
  IL_0100:  conv.i8
  IL_0101:  ldc.r4     1
  IL_0106:  ldsfld     ""decimal decimal.One""
  IL_010b:  ldloca.s   V_0
  IL_010d:  initobj    ""System.DateTime""
  IL_0113:  ldloc.0
  IL_0114:  ldarg.1
  IL_0115:  ldarg.1
  IL_0116:  callvirt   ""object <>F<System.Runtime.CompilerServices.CallSite, object, int, bool, double, char, string, byte, sbyte, uint, long, ulong, float, decimal, System.DateTime, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, int, bool, double, char, string, byte, sbyte, uint, long, ulong, float, decimal, System.DateTime, object, object)""
  IL_011b:  ret
}
"
                );
        }

        [Fact]
        public void InvokeMember_ManyArgs_A14()
        {
            string source = @"
public class C
{
    public void M14(dynamic d)
    {
        d.m(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14);
    }
}";
            CompileAndVerifyIL(source, "C.M14", @"
{
  // Code size      251 (0xfb)
  .maxstack  17
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int>> C.<>o__0.<>p__0""
  IL_0005:  brtrue     IL_00d1
  IL_000a:  ldc.i4     0x100
  IL_000f:  ldstr      ""m""
  IL_0014:  ldnull
  IL_0015:  ldtoken    ""C""
  IL_001a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001f:  ldc.i4.s   15
  IL_0021:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0026:  dup
  IL_0027:  ldc.i4.0
  IL_0028:  ldc.i4.0
  IL_0029:  ldnull
  IL_002a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002f:  stelem.ref
  IL_0030:  dup
  IL_0031:  ldc.i4.1
  IL_0032:  ldc.i4.3
  IL_0033:  ldnull
  IL_0034:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0039:  stelem.ref
  IL_003a:  dup
  IL_003b:  ldc.i4.2
  IL_003c:  ldc.i4.3
  IL_003d:  ldnull
  IL_003e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0043:  stelem.ref
  IL_0044:  dup
  IL_0045:  ldc.i4.3
  IL_0046:  ldc.i4.3
  IL_0047:  ldnull
  IL_0048:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_004d:  stelem.ref
  IL_004e:  dup
  IL_004f:  ldc.i4.4
  IL_0050:  ldc.i4.3
  IL_0051:  ldnull
  IL_0052:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0057:  stelem.ref
  IL_0058:  dup
  IL_0059:  ldc.i4.5
  IL_005a:  ldc.i4.3
  IL_005b:  ldnull
  IL_005c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0061:  stelem.ref
  IL_0062:  dup
  IL_0063:  ldc.i4.6
  IL_0064:  ldc.i4.3
  IL_0065:  ldnull
  IL_0066:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_006b:  stelem.ref
  IL_006c:  dup
  IL_006d:  ldc.i4.7
  IL_006e:  ldc.i4.3
  IL_006f:  ldnull
  IL_0070:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0075:  stelem.ref
  IL_0076:  dup
  IL_0077:  ldc.i4.8
  IL_0078:  ldc.i4.3
  IL_0079:  ldnull
  IL_007a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_007f:  stelem.ref
  IL_0080:  dup
  IL_0081:  ldc.i4.s   9
  IL_0083:  ldc.i4.3
  IL_0084:  ldnull
  IL_0085:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_008a:  stelem.ref
  IL_008b:  dup
  IL_008c:  ldc.i4.s   10
  IL_008e:  ldc.i4.3
  IL_008f:  ldnull
  IL_0090:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0095:  stelem.ref
  IL_0096:  dup
  IL_0097:  ldc.i4.s   11
  IL_0099:  ldc.i4.3
  IL_009a:  ldnull
  IL_009b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00a0:  stelem.ref
  IL_00a1:  dup
  IL_00a2:  ldc.i4.s   12
  IL_00a4:  ldc.i4.3
  IL_00a5:  ldnull
  IL_00a6:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00ab:  stelem.ref
  IL_00ac:  dup
  IL_00ad:  ldc.i4.s   13
  IL_00af:  ldc.i4.3
  IL_00b0:  ldnull
  IL_00b1:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00b6:  stelem.ref
  IL_00b7:  dup
  IL_00b8:  ldc.i4.s   14
  IL_00ba:  ldc.i4.3
  IL_00bb:  ldnull
  IL_00bc:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00c1:  stelem.ref
  IL_00c2:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00c7:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00cc:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int>> C.<>o__0.<>p__0""
  IL_00d1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int>> C.<>o__0.<>p__0""
  IL_00d6:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int>>.Target""
  IL_00db:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int>> C.<>o__0.<>p__0""
  IL_00e0:  ldarg.1
  IL_00e1:  ldc.i4.1
  IL_00e2:  ldc.i4.2
  IL_00e3:  ldc.i4.3
  IL_00e4:  ldc.i4.4
  IL_00e5:  ldc.i4.5
  IL_00e6:  ldc.i4.6
  IL_00e7:  ldc.i4.7
  IL_00e8:  ldc.i4.8
  IL_00e9:  ldc.i4.s   9
  IL_00eb:  ldc.i4.s   10
  IL_00ed:  ldc.i4.s   11
  IL_00ef:  ldc.i4.s   12
  IL_00f1:  ldc.i4.s   13
  IL_00f3:  ldc.i4.s   14
  IL_00f5:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int>.Invoke(System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int)""
  IL_00fa:  ret
}
");
        }

        [Fact]
        public void InvokeMember_ManyArgs_A15()
        {
            string source = @"
public class C
{
    public void M15(dynamic d)
    {
        d.m(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);
    }
}";
            CompileAndVerifyIL(source, "C.M15", @"
{
  // Code size      264 (0x108)
  .maxstack  18
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>> C.<>o__0.<>p__0""
  IL_0005:  brtrue     IL_00dc
  IL_000a:  ldc.i4     0x100
  IL_000f:  ldstr      ""m""
  IL_0014:  ldnull
  IL_0015:  ldtoken    ""C""
  IL_001a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001f:  ldc.i4.s   16
  IL_0021:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0026:  dup
  IL_0027:  ldc.i4.0
  IL_0028:  ldc.i4.0
  IL_0029:  ldnull
  IL_002a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002f:  stelem.ref
  IL_0030:  dup
  IL_0031:  ldc.i4.1
  IL_0032:  ldc.i4.3
  IL_0033:  ldnull
  IL_0034:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0039:  stelem.ref
  IL_003a:  dup
  IL_003b:  ldc.i4.2
  IL_003c:  ldc.i4.3
  IL_003d:  ldnull
  IL_003e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0043:  stelem.ref
  IL_0044:  dup
  IL_0045:  ldc.i4.3
  IL_0046:  ldc.i4.3
  IL_0047:  ldnull
  IL_0048:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_004d:  stelem.ref
  IL_004e:  dup
  IL_004f:  ldc.i4.4
  IL_0050:  ldc.i4.3
  IL_0051:  ldnull
  IL_0052:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0057:  stelem.ref
  IL_0058:  dup
  IL_0059:  ldc.i4.5
  IL_005a:  ldc.i4.3
  IL_005b:  ldnull
  IL_005c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0061:  stelem.ref
  IL_0062:  dup
  IL_0063:  ldc.i4.6
  IL_0064:  ldc.i4.3
  IL_0065:  ldnull
  IL_0066:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_006b:  stelem.ref
  IL_006c:  dup
  IL_006d:  ldc.i4.7
  IL_006e:  ldc.i4.3
  IL_006f:  ldnull
  IL_0070:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0075:  stelem.ref
  IL_0076:  dup
  IL_0077:  ldc.i4.8
  IL_0078:  ldc.i4.3
  IL_0079:  ldnull
  IL_007a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_007f:  stelem.ref
  IL_0080:  dup
  IL_0081:  ldc.i4.s   9
  IL_0083:  ldc.i4.3
  IL_0084:  ldnull
  IL_0085:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_008a:  stelem.ref
  IL_008b:  dup
  IL_008c:  ldc.i4.s   10
  IL_008e:  ldc.i4.3
  IL_008f:  ldnull
  IL_0090:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0095:  stelem.ref
  IL_0096:  dup
  IL_0097:  ldc.i4.s   11
  IL_0099:  ldc.i4.3
  IL_009a:  ldnull
  IL_009b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00a0:  stelem.ref
  IL_00a1:  dup
  IL_00a2:  ldc.i4.s   12
  IL_00a4:  ldc.i4.3
  IL_00a5:  ldnull
  IL_00a6:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00ab:  stelem.ref
  IL_00ac:  dup
  IL_00ad:  ldc.i4.s   13
  IL_00af:  ldc.i4.3
  IL_00b0:  ldnull
  IL_00b1:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00b6:  stelem.ref
  IL_00b7:  dup
  IL_00b8:  ldc.i4.s   14
  IL_00ba:  ldc.i4.3
  IL_00bb:  ldnull
  IL_00bc:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00c1:  stelem.ref
  IL_00c2:  dup
  IL_00c3:  ldc.i4.s   15
  IL_00c5:  ldc.i4.3
  IL_00c6:  ldnull
  IL_00c7:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00cc:  stelem.ref
  IL_00cd:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00d2:  call       ""System.Runtime.CompilerServices.CallSite<<>A<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>> System.Runtime.CompilerServices.CallSite<<>A<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00d7:  stsfld     ""System.Runtime.CompilerServices.CallSite<<>A<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>> C.<>o__0.<>p__0""
  IL_00dc:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>> C.<>o__0.<>p__0""
  IL_00e1:  ldfld      ""<>A<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int> System.Runtime.CompilerServices.CallSite<<>A<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>>.Target""
  IL_00e6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>> C.<>o__0.<>p__0""
  IL_00eb:  ldarg.1
  IL_00ec:  ldc.i4.1
  IL_00ed:  ldc.i4.2
  IL_00ee:  ldc.i4.3
  IL_00ef:  ldc.i4.4
  IL_00f0:  ldc.i4.5
  IL_00f1:  ldc.i4.6
  IL_00f2:  ldc.i4.7
  IL_00f3:  ldc.i4.8
  IL_00f4:  ldc.i4.s   9
  IL_00f6:  ldc.i4.s   10
  IL_00f8:  ldc.i4.s   11
  IL_00fa:  ldc.i4.s   12
  IL_00fc:  ldc.i4.s   13
  IL_00fe:  ldc.i4.s   14
  IL_0100:  ldc.i4.s   15
  IL_0102:  callvirt   ""void <>A<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>.Invoke(System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int)""
  IL_0107:  ret
}
");
        }

        [Fact]
        public void InvokeMember_ByRefArgs()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return d.m(ref d, out d);
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      103 (0x67)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F{0000000c}<System.Runtime.CompilerServices.CallSite, object, object, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_004d
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      ""m""
  IL_000d:  ldnull
  IL_000e:  ldtoken    ""C""
  IL_0013:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0018:  ldc.i4.3
  IL_0019:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001e:  dup
  IL_001f:  ldc.i4.0
  IL_0020:  ldc.i4.0
  IL_0021:  ldnull
  IL_0022:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0027:  stelem.ref
  IL_0028:  dup
  IL_0029:  ldc.i4.1
  IL_002a:  ldc.i4.s   9
  IL_002c:  ldnull
  IL_002d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0032:  stelem.ref
  IL_0033:  dup
  IL_0034:  ldc.i4.2
  IL_0035:  ldc.i4.s   17
  IL_0037:  ldnull
  IL_0038:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_003d:  stelem.ref
  IL_003e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0043:  call       ""System.Runtime.CompilerServices.CallSite<<>F{0000000c}<System.Runtime.CompilerServices.CallSite, object, object, object, object>> System.Runtime.CompilerServices.CallSite<<>F{0000000c}<System.Runtime.CompilerServices.CallSite, object, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0048:  stsfld     ""System.Runtime.CompilerServices.CallSite<<>F{0000000c}<System.Runtime.CompilerServices.CallSite, object, object, object, object>> C.<>o__0.<>p__0""
  IL_004d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F{0000000c}<System.Runtime.CompilerServices.CallSite, object, object, object, object>> C.<>o__0.<>p__0""
  IL_0052:  ldfld      ""<>F{0000000c}<System.Runtime.CompilerServices.CallSite, object, object, object, object> System.Runtime.CompilerServices.CallSite<<>F{0000000c}<System.Runtime.CompilerServices.CallSite, object, object, object, object>>.Target""
  IL_0057:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F{0000000c}<System.Runtime.CompilerServices.CallSite, object, object, object, object>> C.<>o__0.<>p__0""
  IL_005c:  ldarg.1
  IL_005d:  ldarga.s   V_1
  IL_005f:  ldarga.s   V_1
  IL_0061:  callvirt   ""object <>F{0000000c}<System.Runtime.CompilerServices.CallSite, object, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, ref object, ref object)""
  IL_0066:  ret
}
");
        }

        [Fact]
        public void InvokeMember_ByRefArgs_Runtime()
        {
            string source = @"
public class C
{
    public static dynamic d = new C();

    public static void Main()
    {
        d.m(ref d, out d);
    }

    public void m(ref object a, out object b) { b = null; }
}
";
            CompileAndVerify(source, expectedOutput: "", additionalRefs: new[] { SystemCoreRef, CSharpRef });
        }

        /// <summary>
        /// By-ref dynamic argument doesn't make the call dynamic.
        /// </summary>
        [Fact]
        public void InvokeMember_ByRefDynamic()
        {
            string source = @"
public class C
{
    static dynamic d = true;

    public static void f(ref dynamic d) 
    {
    }
    
    public static void M()
    {
        f(ref d);
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldsflda    ""dynamic C.d""
  IL_0005:  call       ""void C.f(ref dynamic)""
  IL_000a:  ret
}
");
        }

        /// <summary>
        /// ref/out can be omitted at call-site.
        /// </summary>
        [Fact]
        public void InvokeMember_CallSiteRefOutOmitted()
        {
            string source = @"
public class C
{
    dynamic d = true;

    public void f(ref int a, out int b, ref dynamic c, out object d) 
    {
        b = 1;
        d = null;
    }
    
    public void M()
    {
        object lo = null;
        dynamic ld;

        f(d, d, ref lo, out ld);
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      141 (0x8d)
  .maxstack  9
  .locals init (object V_0, //lo
                object V_1) //ld
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000030}<System.Runtime.CompilerServices.CallSite, C, object, object, object, object>> C.<>o__2.<>p__0""
  IL_0007:  brtrue.s   IL_0067
  IL_0009:  ldc.i4     0x102
  IL_000e:  ldstr      ""f""
  IL_0013:  ldnull
  IL_0014:  ldtoken    ""C""
  IL_0019:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001e:  ldc.i4.5
  IL_001f:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0024:  dup
  IL_0025:  ldc.i4.0
  IL_0026:  ldc.i4.1
  IL_0027:  ldnull
  IL_0028:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002d:  stelem.ref
  IL_002e:  dup
  IL_002f:  ldc.i4.1
  IL_0030:  ldc.i4.0
  IL_0031:  ldnull
  IL_0032:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0037:  stelem.ref
  IL_0038:  dup
  IL_0039:  ldc.i4.2
  IL_003a:  ldc.i4.0
  IL_003b:  ldnull
  IL_003c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0041:  stelem.ref
  IL_0042:  dup
  IL_0043:  ldc.i4.3
  IL_0044:  ldc.i4.s   9
  IL_0046:  ldnull
  IL_0047:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_004c:  stelem.ref
  IL_004d:  dup
  IL_004e:  ldc.i4.4
  IL_004f:  ldc.i4.s   17
  IL_0051:  ldnull
  IL_0052:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0057:  stelem.ref
  IL_0058:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_005d:  call       ""System.Runtime.CompilerServices.CallSite<<>A{00000030}<System.Runtime.CompilerServices.CallSite, C, object, object, object, object>> System.Runtime.CompilerServices.CallSite<<>A{00000030}<System.Runtime.CompilerServices.CallSite, C, object, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0062:  stsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000030}<System.Runtime.CompilerServices.CallSite, C, object, object, object, object>> C.<>o__2.<>p__0""
  IL_0067:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000030}<System.Runtime.CompilerServices.CallSite, C, object, object, object, object>> C.<>o__2.<>p__0""
  IL_006c:  ldfld      ""<>A{00000030}<System.Runtime.CompilerServices.CallSite, C, object, object, object, object> System.Runtime.CompilerServices.CallSite<<>A{00000030}<System.Runtime.CompilerServices.CallSite, C, object, object, object, object>>.Target""
  IL_0071:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000030}<System.Runtime.CompilerServices.CallSite, C, object, object, object, object>> C.<>o__2.<>p__0""
  IL_0076:  ldarg.0
  IL_0077:  ldarg.0
  IL_0078:  ldfld      ""dynamic C.d""
  IL_007d:  ldarg.0
  IL_007e:  ldfld      ""dynamic C.d""
  IL_0083:  ldloca.s   V_0
  IL_0085:  ldloca.s   V_1
  IL_0087:  callvirt   ""void <>A{00000030}<System.Runtime.CompilerServices.CallSite, C, object, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, C, object, object, ref object, ref object)""
  IL_008c:  ret
}
");
        }

        [Fact]
        public void InvokeStaticMember1()
        {
            string source = @"
public class C
{
    public void M(dynamic d)
    {
        D.F(d);
    }
}

public class D
{
    public static void F(int a) {}
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      102 (0x66)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0046
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldstr      ""F""
  IL_0011:  ldnull
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.s   33
  IL_0026:  ldnull
  IL_0027:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002c:  stelem.ref
  IL_002d:  dup
  IL_002e:  ldc.i4.1
  IL_002f:  ldc.i4.0
  IL_0030:  ldnull
  IL_0031:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0036:  stelem.ref
  IL_0037:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003c:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0041:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> C.<>o__0.<>p__0""
  IL_0046:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> C.<>o__0.<>p__0""
  IL_004b:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>>.Target""
  IL_0050:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> C.<>o__0.<>p__0""
  IL_0055:  ldtoken    ""D""
  IL_005a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005f:  ldarg.1
  IL_0060:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, object)""
  IL_0065:  ret
}");
        }

        [Fact]
        public void InvokeStaticMember2()
        {
            string source = @"
public class C
{
    static dynamic d = true;

    public static void f(dynamic d) 
    {
    }
    
    public static void M()
    {
        f(d);
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      106 (0x6a)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> C.<>o__2.<>p__0""
  IL_0005:  brtrue.s   IL_0046
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldstr      ""f""
  IL_0011:  ldnull
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.s   33
  IL_0026:  ldnull
  IL_0027:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002c:  stelem.ref
  IL_002d:  dup
  IL_002e:  ldc.i4.1
  IL_002f:  ldc.i4.0
  IL_0030:  ldnull
  IL_0031:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0036:  stelem.ref
  IL_0037:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003c:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0041:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> C.<>o__2.<>p__0""
  IL_0046:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> C.<>o__2.<>p__0""
  IL_004b:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>>.Target""
  IL_0050:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> C.<>o__2.<>p__0""
  IL_0055:  ldtoken    ""C""
  IL_005a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005f:  ldsfld     ""dynamic C.d""
  IL_0064:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, object)""
  IL_0069:  ret
}
");
        }

        [Fact]
        [WorkItem(627091, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627091")]
        public void InvokeStaticMember_InLambda()
        {
            string source = @"
class C
{
    static void Foo(dynamic x)
    {
        System.Action a = () => Foo(x);
    }
}
";
            CompileAndVerifyIL(source, "C.<>c__DisplayClass0_0.<Foo>b__0", @"
{
  // Code size      107 (0x6b)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0046
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldstr      ""Foo""
  IL_0011:  ldnull
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.s   33
  IL_0026:  ldnull
  IL_0027:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002c:  stelem.ref
  IL_002d:  dup
  IL_002e:  ldc.i4.1
  IL_002f:  ldc.i4.0
  IL_0030:  ldnull
  IL_0031:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0036:  stelem.ref
  IL_0037:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003c:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0041:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> C.<>o__0.<>p__0""
  IL_0046:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> C.<>o__0.<>p__0""
  IL_004b:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>>.Target""
  IL_0050:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> C.<>o__0.<>p__0""
  IL_0055:  ldtoken    ""C""
  IL_005a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005f:  ldarg.0
  IL_0060:  ldfld      ""object C.<>c__DisplayClass0_0.x""
  IL_0065:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, object)""
  IL_006a:  ret
}
");
        }

        [Fact]
        public void InvokeMember_ValueTypeReceiver_Local()
        {
            string source = @"
public class C
{
    public void M(dynamic d) 
    {
        S s = new S();
        s.foo(d);
    }
}

public struct S 
{ 
    public int X;
    public void foo(int a) {}
}
";
            // Dev11 produces more efficient code, see bug 547265:

            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      102 (0x66)
  .maxstack  9
  .locals init (S V_0) //s
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__0.<>p__0""
  IL_000d:  brtrue.s   IL_004e
  IL_000f:  ldc.i4     0x100
  IL_0014:  ldstr      ""foo""
  IL_0019:  ldnull
  IL_001a:  ldtoken    ""C""
  IL_001f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0024:  ldc.i4.2
  IL_0025:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_002a:  dup
  IL_002b:  ldc.i4.0
  IL_002c:  ldc.i4.s   9
  IL_002e:  ldnull
  IL_002f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0034:  stelem.ref
  IL_0035:  dup
  IL_0036:  ldc.i4.1
  IL_0037:  ldc.i4.0
  IL_0038:  ldnull
  IL_0039:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_003e:  stelem.ref
  IL_003f:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0044:  call       ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0049:  stsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__0.<>p__0""
  IL_004e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__0.<>p__0""
  IL_0053:  ldfld      ""<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object> System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>>.Target""
  IL_0058:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__0.<>p__0""
  IL_005d:  ldloca.s   V_0
  IL_005f:  ldarg.1
  IL_0060:  callvirt   ""void <>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>.Invoke(System.Runtime.CompilerServices.CallSite, ref S, object)""
  IL_0065:  ret
}");
        }

        [Fact]
        public void InvokeMember_ValueTypeReceiver_Parameter()
        {
            string source = @"
public class C
{
    public void M(S s, dynamic d) 
    {
        s.foo(d);
    }
}

public struct S 
{ 
    public int X;
    public void foo(int a) {}
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       94 (0x5e)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0046
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldstr      ""foo""
  IL_0011:  ldnull
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.s   9
  IL_0026:  ldnull
  IL_0027:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002c:  stelem.ref
  IL_002d:  dup
  IL_002e:  ldc.i4.1
  IL_002f:  ldc.i4.0
  IL_0030:  ldnull
  IL_0031:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0036:  stelem.ref
  IL_0037:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003c:  call       ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0041:  stsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__0.<>p__0""
  IL_0046:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__0.<>p__0""
  IL_004b:  ldfld      ""<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object> System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>>.Target""
  IL_0050:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__0.<>p__0""
  IL_0055:  ldarga.s   V_1
  IL_0057:  ldarg.2
  IL_0058:  callvirt   ""void <>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>.Invoke(System.Runtime.CompilerServices.CallSite, ref S, object)""
  IL_005d:  ret
}
");
        }

        [Fact]
        public void InvokeMember_ValueTypeReceiver_This()
        {
            string source = @"
public struct S
{
    int a;

    public void M(dynamic d) 
    {
        this.Equals(d);
    }
}
";
            CompileAndVerifyIL(source, "S.M", @"
{
  // Code size       93 (0x5d)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> S.<>o__1.<>p__0""
  IL_0005:  brtrue.s   IL_0046
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldstr      ""Equals""
  IL_0011:  ldnull
  IL_0012:  ldtoken    ""S""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.s   9
  IL_0026:  ldnull
  IL_0027:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002c:  stelem.ref
  IL_002d:  dup
  IL_002e:  ldc.i4.1
  IL_002f:  ldc.i4.0
  IL_0030:  ldnull
  IL_0031:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0036:  stelem.ref
  IL_0037:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003c:  call       ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0041:  stsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> S.<>o__1.<>p__0""
  IL_0046:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> S.<>o__1.<>p__0""
  IL_004b:  ldfld      ""<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object> System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>>.Target""
  IL_0050:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> S.<>o__1.<>p__0""
  IL_0055:  ldarg.0
  IL_0056:  ldarg.1
  IL_0057:  callvirt   ""void <>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>.Invoke(System.Runtime.CompilerServices.CallSite, ref S, object)""
  IL_005c:  ret
}
");
        }

        [Fact]
        public void InvokeMember_ValueTypeReceiver_FieldAccess()
        {
            string source = @"
public class C
{
    private S s;

    public void M(dynamic d) 
    {
        s.foo(d);
    }
}

public struct S 
{ 
    public int X;
    public void foo(int a) {}
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       97 (0x61)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__1.<>p__0""
  IL_0005:  brtrue.s   IL_0045
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldstr      ""foo""
  IL_0011:  ldnull
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.1
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  dup
  IL_002d:  ldc.i4.1
  IL_002e:  ldc.i4.0
  IL_002f:  ldnull
  IL_0030:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0035:  stelem.ref
  IL_0036:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003b:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0040:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__1.<>p__0""
  IL_0045:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__1.<>p__0""
  IL_004a:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, S, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>>.Target""
  IL_004f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__1.<>p__0""
  IL_0054:  ldarg.0
  IL_0055:  ldfld      ""S C.s""
  IL_005a:  ldarg.1
  IL_005b:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, S, object>.Invoke(System.Runtime.CompilerServices.CallSite, S, object)""
  IL_0060:  ret
}
");
        }

        [Fact]
        public void InvokeMember_ValueTypeReceiver_ArrayAccess()
        {
            string source = @"
public class C
{
    private S[] s;

    public void M(dynamic d) 
    {
        s[0].foo(d);
    }
}

public struct S 
{ 
    public int X;
    public void foo(int a) {}
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      104 (0x68)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__1.<>p__0""
  IL_0005:  brtrue.s   IL_0046
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldstr      ""foo""
  IL_0011:  ldnull
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.s   9
  IL_0026:  ldnull
  IL_0027:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002c:  stelem.ref
  IL_002d:  dup
  IL_002e:  ldc.i4.1
  IL_002f:  ldc.i4.0
  IL_0030:  ldnull
  IL_0031:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0036:  stelem.ref
  IL_0037:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003c:  call       ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0041:  stsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__1.<>p__0""
  IL_0046:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__1.<>p__0""
  IL_004b:  ldfld      ""<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object> System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>>.Target""
  IL_0050:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__1.<>p__0""
  IL_0055:  ldarg.0
  IL_0056:  ldfld      ""S[] C.s""
  IL_005b:  ldc.i4.0
  IL_005c:  ldelema    ""S""
  IL_0061:  ldarg.1
  IL_0062:  callvirt   ""void <>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>.Invoke(System.Runtime.CompilerServices.CallSite, ref S, object)""
  IL_0067:  ret
}
");
        }

        [Fact]
        public void InvokeMember_ValueTypeReceiver_PointerIndirectionOperator1()
        {
            string source = @"
public unsafe class C
{
    private S s;

    public void M(dynamic d) 
    {
        S s = new S();
        S* ptr = &s;
        (*ptr).foo(d);
    }
}

public struct S 
{ 
    public int X;
    public void foo(int a) {}
}
";
            // Dev11 produces more efficient code, see bug 547265:
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      105 (0x69)
  .maxstack  9
  .locals init (S V_0, //s
                S* V_1) //ptr
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  conv.u
  IL_000b:  stloc.1
  IL_000c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__1.<>p__0""
  IL_0011:  brtrue.s   IL_0052
  IL_0013:  ldc.i4     0x100
  IL_0018:  ldstr      ""foo""
  IL_001d:  ldnull
  IL_001e:  ldtoken    ""C""
  IL_0023:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0028:  ldc.i4.2
  IL_0029:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_002e:  dup
  IL_002f:  ldc.i4.0
  IL_0030:  ldc.i4.s   9
  IL_0032:  ldnull
  IL_0033:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0038:  stelem.ref
  IL_0039:  dup
  IL_003a:  ldc.i4.1
  IL_003b:  ldc.i4.0
  IL_003c:  ldnull
  IL_003d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0042:  stelem.ref
  IL_0043:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0048:  call       ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_004d:  stsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__1.<>p__0""
  IL_0052:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__1.<>p__0""
  IL_0057:  ldfld      ""<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object> System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>>.Target""
  IL_005c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__1.<>p__0""
  IL_0061:  ldloc.1
  IL_0062:  ldarg.1
  IL_0063:  callvirt   ""void <>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>.Invoke(System.Runtime.CompilerServices.CallSite, ref S, object)""
  IL_0068:  ret
}
", allowUnsafe: true);
        }

        [Fact]
        public void InvokeMember_ValueTypeReceiver_PointerIndirectionOperator2()
        {
            string source = @"
public unsafe class C
{
    private S s;

    public void M(dynamic d) 
    {
        S s = new S();
        S* ptr = &s;
        ptr->foo(d);
    }
}

public struct S 
{ 
    public int X;
    public void foo(int a) {}
}
";
            // Dev11 produces more efficient code, see bug 547265:
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      105 (0x69)
  .maxstack  9
  .locals init (S V_0, //s
                S* V_1) //ptr
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  conv.u
  IL_000b:  stloc.1
  IL_000c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__1.<>p__0""
  IL_0011:  brtrue.s   IL_0052
  IL_0013:  ldc.i4     0x100
  IL_0018:  ldstr      ""foo""
  IL_001d:  ldnull
  IL_001e:  ldtoken    ""C""
  IL_0023:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0028:  ldc.i4.2
  IL_0029:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_002e:  dup
  IL_002f:  ldc.i4.0
  IL_0030:  ldc.i4.s   9
  IL_0032:  ldnull
  IL_0033:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0038:  stelem.ref
  IL_0039:  dup
  IL_003a:  ldc.i4.1
  IL_003b:  ldc.i4.0
  IL_003c:  ldnull
  IL_003d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0042:  stelem.ref
  IL_0043:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0048:  call       ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_004d:  stsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__1.<>p__0""
  IL_0052:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__1.<>p__0""
  IL_0057:  ldfld      ""<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object> System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>>.Target""
  IL_005c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__1.<>p__0""
  IL_0061:  ldloc.1
  IL_0062:  ldarg.1
  IL_0063:  callvirt   ""void <>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>.Invoke(System.Runtime.CompilerServices.CallSite, ref S, object)""
  IL_0068:  ret
}
", allowUnsafe: true);
        }

        [Fact]
        public void InvokeMember_ValueTypeReceiver_PointerElementAccess()
        {
            string source = @"
public unsafe class C
{
    private S s;

    public void M(dynamic d) 
    {
        S* ptr = stackalloc S[2];
        ptr[1].foo(d);
    }
}

public struct S 
{ 
    public int X;
    public void foo(int a) {}
}
";
            // Dev11 produces more efficient code, see bug 547265:
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      112 (0x70)
  .maxstack  9
  .locals init (S* V_0) //ptr
  IL_0000:  ldc.i4.2
  IL_0001:  conv.u
  IL_0002:  sizeof     ""S""
  IL_0008:  mul.ovf.un
  IL_0009:  localloc
  IL_000b:  stloc.0
  IL_000c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__1.<>p__0""
  IL_0011:  brtrue.s   IL_0052
  IL_0013:  ldc.i4     0x100
  IL_0018:  ldstr      ""foo""
  IL_001d:  ldnull
  IL_001e:  ldtoken    ""C""
  IL_0023:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0028:  ldc.i4.2
  IL_0029:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_002e:  dup
  IL_002f:  ldc.i4.0
  IL_0030:  ldc.i4.s   9
  IL_0032:  ldnull
  IL_0033:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0038:  stelem.ref
  IL_0039:  dup
  IL_003a:  ldc.i4.1
  IL_003b:  ldc.i4.0
  IL_003c:  ldnull
  IL_003d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0042:  stelem.ref
  IL_0043:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0048:  call       ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_004d:  stsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__1.<>p__0""
  IL_0052:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__1.<>p__0""
  IL_0057:  ldfld      ""<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object> System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>>.Target""
  IL_005c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__1.<>p__0""
  IL_0061:  ldloc.0
  IL_0062:  sizeof     ""S""
  IL_0068:  add
  IL_0069:  ldarg.1
  IL_006a:  callvirt   ""void <>A{00000002}<System.Runtime.CompilerServices.CallSite, S, object>.Invoke(System.Runtime.CompilerServices.CallSite, ref S, object)""
  IL_006f:  ret
}
", allowUnsafe: true);
        }

        [Fact]
        public void InvokeMember_ValueTypeReceiver_TypeReference()
        {
            string source = @"
using System;

public class C
{
    public void M(dynamic d)
    {   
        int a = 1;
        TypedReference tr = __makeref(a);
        __refvalue(tr, int).Equals(d);
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      108 (0x6c)
  .maxstack  9
  .locals init (int V_0, //a
  System.TypedReference V_1) //tr
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  mkrefany   ""int""
  IL_0009:  stloc.1
  IL_000a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, int, object>> C.<>o__0.<>p__0""
  IL_000f:  brtrue.s   IL_0050
  IL_0011:  ldc.i4     0x100
  IL_0016:  ldstr      ""Equals""
  IL_001b:  ldnull
  IL_001c:  ldtoken    ""C""
  IL_0021:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0026:  ldc.i4.2
  IL_0027:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_002c:  dup
  IL_002d:  ldc.i4.0
  IL_002e:  ldc.i4.s   9
  IL_0030:  ldnull
  IL_0031:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0036:  stelem.ref
  IL_0037:  dup
  IL_0038:  ldc.i4.1
  IL_0039:  ldc.i4.0
  IL_003a:  ldnull
  IL_003b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0040:  stelem.ref
  IL_0041:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0046:  call       ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, int, object>> System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, int, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_004b:  stsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, int, object>> C.<>o__0.<>p__0""
  IL_0050:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, int, object>> C.<>o__0.<>p__0""
  IL_0055:  ldfld      ""<>A{00000002}<System.Runtime.CompilerServices.CallSite, int, object> System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, int, object>>.Target""
  IL_005a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000002}<System.Runtime.CompilerServices.CallSite, int, object>> C.<>o__0.<>p__0""
  IL_005f:  ldloc.1
  IL_0060:  refanyval  ""int""
  IL_0065:  ldarg.1
  IL_0066:  callvirt   ""void <>A{00000002}<System.Runtime.CompilerServices.CallSite, int, object>.Invoke(System.Runtime.CompilerServices.CallSite, ref int, object)""
  IL_006b:  ret
}
");
        }

        [Fact]
        public void InvokeMember_ValueTypeReceiver_Literal()
        {
            string source = @"
public class C
{
    public void M(dynamic d) 
    {
        ""a"".Equals(d);
    }
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       96 (0x60)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, string, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0045
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldstr      ""Equals""
  IL_0011:  ldnull
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.3
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  dup
  IL_002d:  ldc.i4.1
  IL_002e:  ldc.i4.0
  IL_002f:  ldnull
  IL_0030:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0035:  stelem.ref
  IL_0036:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003b:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, string, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, string, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0040:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, string, object>> C.<>o__0.<>p__0""
  IL_0045:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, string, object>> C.<>o__0.<>p__0""
  IL_004a:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, string, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, string, object>>.Target""
  IL_004f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, string, object>> C.<>o__0.<>p__0""
  IL_0054:  ldstr      ""a""
  IL_0059:  ldarg.1
  IL_005a:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, string, object>.Invoke(System.Runtime.CompilerServices.CallSite, string, object)""
  IL_005f:  ret
}
");
        }

        [Fact]
        public void InvokeMember_ValueTypeReceiver_Assignment()
        {
            string source = @"
public class C
{
    public void M(dynamic d, S s, S t) 
    {
        (s = t).foo(d);
    }
}

public struct S 
{ 
    public int X;
    public void foo(int a) {}
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       95 (0x5f)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0045
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldstr      ""foo""
  IL_0011:  ldnull
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.1
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  dup
  IL_002d:  ldc.i4.1
  IL_002e:  ldc.i4.0
  IL_002f:  ldnull
  IL_0030:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0035:  stelem.ref
  IL_0036:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003b:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0040:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__0.<>p__0""
  IL_0045:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__0.<>p__0""
  IL_004a:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, S, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>>.Target""
  IL_004f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__0.<>p__0""
  IL_0054:  ldarg.3
  IL_0055:  dup
  IL_0056:  starg.s    V_2
  IL_0058:  ldarg.1
  IL_0059:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, S, object>.Invoke(System.Runtime.CompilerServices.CallSite, S, object)""
  IL_005e:  ret
}");
        }

        [Fact]
        public void InvokeMember_ValueTypeReceiver_PropertyAccess()
        {
            string source = @"
public class C
{
    private S P { get; set; }

    public void M(C c, dynamic d) 
    {
        c.P.foo(d);
    }
}

public struct S 
{ 
    public int X;
    public void foo(int a) {}
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       97 (0x61)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__4.<>p__0""
  IL_0005:  brtrue.s   IL_0045
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldstr      ""foo""
  IL_0011:  ldnull
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.1
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  dup
  IL_002d:  ldc.i4.1
  IL_002e:  ldc.i4.0
  IL_002f:  ldnull
  IL_0030:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0035:  stelem.ref
  IL_0036:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003b:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0040:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__4.<>p__0""
  IL_0045:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__4.<>p__0""
  IL_004a:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, S, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>>.Target""
  IL_004f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__4.<>p__0""
  IL_0054:  ldarg.1
  IL_0055:  callvirt   ""S C.P.get""
  IL_005a:  ldarg.2
  IL_005b:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, S, object>.Invoke(System.Runtime.CompilerServices.CallSite, S, object)""
  IL_0060:  ret
}
");
        }

        [Fact]
        public void InvokeMember_ValueTypeReceiver_IndexerAccess()
        {
            string source = @"
public class C
{
    private S this[int index] { get { return new S(); } set { } }

    public void M(C c, dynamic d) 
    {
        c[0].foo(d);
    }
}

public struct S 
{ 
    public int X;
    public void foo(int a) {}
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       98 (0x62)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__3.<>p__0""
  IL_0005:  brtrue.s   IL_0045
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldstr      ""foo""
  IL_0011:  ldnull
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.1
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  dup
  IL_002d:  ldc.i4.1
  IL_002e:  ldc.i4.0
  IL_002f:  ldnull
  IL_0030:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0035:  stelem.ref
  IL_0036:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003b:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0040:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__3.<>p__0""
  IL_0045:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__3.<>p__0""
  IL_004a:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, S, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>>.Target""
  IL_004f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__3.<>p__0""
  IL_0054:  ldarg.1
  IL_0055:  ldc.i4.0
  IL_0056:  callvirt   ""S C.this[int].get""
  IL_005b:  ldarg.2
  IL_005c:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, S, object>.Invoke(System.Runtime.CompilerServices.CallSite, S, object)""
  IL_0061:  ret
}");
        }

        [Fact]
        public void InvokeMember_ValueTypeReceiver_Invocation()
        {
            string source = @"
public class C
{
    public void M(System.Func<S> f, dynamic d) 
    {
        f().foo(d);
    }
}

public struct S 
{ 
    public int X;
    public void foo(int a) {}
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       97 (0x61)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0045
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldstr      ""foo""
  IL_0011:  ldnull
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.1
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  dup
  IL_002d:  ldc.i4.1
  IL_002e:  ldc.i4.0
  IL_002f:  ldnull
  IL_0030:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0035:  stelem.ref
  IL_0036:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003b:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0040:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__0.<>p__0""
  IL_0045:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__0.<>p__0""
  IL_004a:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, S, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>>.Target""
  IL_004f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, object>> C.<>o__0.<>p__0""
  IL_0054:  ldarg.1
  IL_0055:  callvirt   ""S System.Func<S>.Invoke()""
  IL_005a:  ldarg.2
  IL_005b:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, S, object>.Invoke(System.Runtime.CompilerServices.CallSite, S, object)""
  IL_0060:  ret
}");
        }

        [Fact]
        public void InvokeMember_InvokeMember()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return d.f().g();
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      152 (0x98)
  .maxstack  11
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__1""
  IL_0005:  brtrue.s   IL_0037
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      ""g""
  IL_000d:  ldnull
  IL_000e:  ldtoken    ""C""
  IL_0013:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0018:  ldc.i4.1
  IL_0019:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001e:  dup
  IL_001f:  ldc.i4.0
  IL_0020:  ldc.i4.0
  IL_0021:  ldnull
  IL_0022:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0027:  stelem.ref
  IL_0028:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_002d:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0032:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__1""
  IL_0037:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__1""
  IL_003c:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_0041:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__1""
  IL_0046:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_004b:  brtrue.s   IL_007d
  IL_004d:  ldc.i4.0
  IL_004e:  ldstr      ""f""
  IL_0053:  ldnull
  IL_0054:  ldtoken    ""C""
  IL_0059:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005e:  ldc.i4.1
  IL_005f:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0064:  dup
  IL_0065:  ldc.i4.0
  IL_0066:  ldc.i4.0
  IL_0067:  ldnull
  IL_0068:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_006d:  stelem.ref
  IL_006e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0073:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0078:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_007d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0082:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_0087:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_008c:  ldarg.1
  IL_008d:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0092:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0097:  ret
}");
        }

        [Fact]
        public void InvokeConstructor()
        {
            string source = @"
public class C
{
    public D M(dynamic d)
    {
        return new D(d);
    }
}

public class D
{
    public D(int x) {}
    public D(string x) {}
    public D(string x, string y) {}
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       92 (0x5c)
  .maxstack  7
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, object, D>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_003c
  IL_0007:  ldc.i4.0
  IL_0008:  ldtoken    ""C""
  IL_000d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0012:  ldc.i4.2
  IL_0013:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0018:  dup
  IL_0019:  ldc.i4.0
  IL_001a:  ldc.i4.s   33
  IL_001c:  ldnull
  IL_001d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0022:  stelem.ref
  IL_0023:  dup
  IL_0024:  ldc.i4.1
  IL_0025:  ldc.i4.0
  IL_0026:  ldnull
  IL_0027:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002c:  stelem.ref
  IL_002d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeConstructor(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0032:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, object, D>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, object, D>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0037:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, object, D>> C.<>o__0.<>p__0""
  IL_003c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, object, D>> C.<>o__0.<>p__0""
  IL_0041:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, System.Type, object, D> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, object, D>>.Target""
  IL_0046:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, object, D>> C.<>o__0.<>p__0""
  IL_004b:  ldtoken    ""D""
  IL_0050:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0055:  ldarg.1
  IL_0056:  callvirt   ""D System.Func<System.Runtime.CompilerServices.CallSite, System.Type, object, D>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, object)""
  IL_005b:  ret
}
");
        }

        [Fact]
        public void Invoke_Dynamic()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d, int a)
    {
        return d(foo: d, bar: a, baz: 123);
    }
}";

            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      117 (0x75)
  .maxstack  7
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, int, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_005b
  IL_0007:  ldc.i4.0
  IL_0008:  ldtoken    ""C""
  IL_000d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0012:  ldc.i4.4
  IL_0013:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0018:  dup
  IL_0019:  ldc.i4.0
  IL_001a:  ldc.i4.0
  IL_001b:  ldnull
  IL_001c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0021:  stelem.ref
  IL_0022:  dup
  IL_0023:  ldc.i4.1
  IL_0024:  ldc.i4.4
  IL_0025:  ldstr      ""foo""
  IL_002a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002f:  stelem.ref
  IL_0030:  dup
  IL_0031:  ldc.i4.2
  IL_0032:  ldc.i4.5
  IL_0033:  ldstr      ""bar""
  IL_0038:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_003d:  stelem.ref
  IL_003e:  dup
  IL_003f:  ldc.i4.3
  IL_0040:  ldc.i4.7
  IL_0041:  ldstr      ""baz""
  IL_0046:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_004b:  stelem.ref
  IL_004c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Invoke(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0051:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, int, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, int, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0056:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, int, object>> C.<>o__0.<>p__0""
  IL_005b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, int, object>> C.<>o__0.<>p__0""
  IL_0060:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, int, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, int, object>>.Target""
  IL_0065:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, int, object>> C.<>o__0.<>p__0""
  IL_006a:  ldarg.1
  IL_006b:  ldarg.1
  IL_006c:  ldarg.2
  IL_006d:  ldc.i4.s   123
  IL_006f:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, int, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object, int, int)""
  IL_0074:  ret
}
");
        }

        [Fact]
        public void Invoke_Dynamic_DiscardResult()
        {
            string source = @"
public class C
{
    public void M(dynamic d, int a)
    {
        d(foo: d, bar: a, baz: 123);
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      121 (0x79)
  .maxstack  7
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, object, int, int>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_005f
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldtoken    ""C""
  IL_0011:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0016:  ldc.i4.4
  IL_0017:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001c:  dup
  IL_001d:  ldc.i4.0
  IL_001e:  ldc.i4.0
  IL_001f:  ldnull
  IL_0020:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0025:  stelem.ref
  IL_0026:  dup
  IL_0027:  ldc.i4.1
  IL_0028:  ldc.i4.4
  IL_0029:  ldstr      ""foo""
  IL_002e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0033:  stelem.ref
  IL_0034:  dup
  IL_0035:  ldc.i4.2
  IL_0036:  ldc.i4.5
  IL_0037:  ldstr      ""bar""
  IL_003c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0041:  stelem.ref
  IL_0042:  dup
  IL_0043:  ldc.i4.3
  IL_0044:  ldc.i4.7
  IL_0045:  ldstr      ""baz""
  IL_004a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_004f:  stelem.ref
  IL_0050:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Invoke(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0055:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, object, int, int>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, object, int, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_005a:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, object, int, int>> C.<>o__0.<>p__0""
  IL_005f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, object, int, int>> C.<>o__0.<>p__0""
  IL_0064:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, object, object, int, int> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, object, int, int>>.Target""
  IL_0069:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, object, int, int>> C.<>o__0.<>p__0""
  IL_006e:  ldarg.1
  IL_006f:  ldarg.1
  IL_0070:  ldarg.2
  IL_0071:  ldc.i4.s   123
  IL_0073:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, object, object, int, int>.Invoke(System.Runtime.CompilerServices.CallSite, object, object, int, int)""
  IL_0078:  ret
}
");
        }

        [Fact]
        public void Invoke_DynamicMember()
        {
            const string source = @"
class C
{
    dynamic d = null;
    
    void M(C c)
    {
        c.d(1);
    }
}";

            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       91 (0x5b)
  .maxstack  7
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__1.<>p__0""
  IL_0005:  brtrue.s   IL_003f
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldtoken    ""C""
  IL_0011:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0016:  ldc.i4.2
  IL_0017:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001c:  dup
  IL_001d:  ldc.i4.0
  IL_001e:  ldc.i4.0
  IL_001f:  ldnull
  IL_0020:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0025:  stelem.ref
  IL_0026:  dup
  IL_0027:  ldc.i4.1
  IL_0028:  ldc.i4.3
  IL_0029:  ldnull
  IL_002a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002f:  stelem.ref
  IL_0030:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Invoke(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0035:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_003a:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__1.<>p__0""
  IL_003f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__1.<>p__0""
  IL_0044:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, object, int> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int>>.Target""
  IL_0049:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__1.<>p__0""
  IL_004e:  ldarg.1
  IL_004f:  ldfld      ""dynamic C.d""
  IL_0054:  ldc.i4.1
  IL_0055:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, object, int>.Invoke(System.Runtime.CompilerServices.CallSite, object, int)""
  IL_005a:  ret
}");
        }

        [Fact]
        public void Invoke_Static()
        {
            string source = @"
public delegate int F(int a, bool b, C c);

public class C
{
    public dynamic M(F f, dynamic d)
    {
        return f(d, d, d);
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      104 (0x68)
  .maxstack  7
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, F, object, object, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_004f
  IL_0007:  ldc.i4.0
  IL_0008:  ldtoken    ""C""
  IL_000d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0012:  ldc.i4.4
  IL_0013:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0018:  dup
  IL_0019:  ldc.i4.0
  IL_001a:  ldc.i4.1
  IL_001b:  ldnull
  IL_001c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0021:  stelem.ref
  IL_0022:  dup
  IL_0023:  ldc.i4.1
  IL_0024:  ldc.i4.0
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  dup
  IL_002d:  ldc.i4.2
  IL_002e:  ldc.i4.0
  IL_002f:  ldnull
  IL_0030:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0035:  stelem.ref
  IL_0036:  dup
  IL_0037:  ldc.i4.3
  IL_0038:  ldc.i4.0
  IL_0039:  ldnull
  IL_003a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_003f:  stelem.ref
  IL_0040:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Invoke(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0045:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, F, object, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, F, object, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_004a:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, F, object, object, object, object>> C.<>o__0.<>p__0""
  IL_004f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, F, object, object, object, object>> C.<>o__0.<>p__0""
  IL_0054:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, F, object, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, F, object, object, object, object>>.Target""
  IL_0059:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, F, object, object, object, object>> C.<>o__0.<>p__0""
  IL_005e:  ldarg.1
  IL_005f:  ldarg.2
  IL_0060:  ldarg.2
  IL_0061:  ldarg.2
  IL_0062:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, F, object, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, F, object, object, object)""
  IL_0067:  ret
}
");
        }

        [Fact]
        public void Invoke_Invoke()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return d()();
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      140 (0x8c)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__1""
  IL_0005:  brtrue.s   IL_0031
  IL_0007:  ldc.i4.0
  IL_0008:  ldtoken    ""C""
  IL_000d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0012:  ldc.i4.1
  IL_0013:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0018:  dup
  IL_0019:  ldc.i4.0
  IL_001a:  ldc.i4.0
  IL_001b:  ldnull
  IL_001c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0021:  stelem.ref
  IL_0022:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Invoke(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0027:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_002c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__1""
  IL_0031:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__1""
  IL_0036:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_003b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__1""
  IL_0040:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0045:  brtrue.s   IL_0071
  IL_0047:  ldc.i4.0
  IL_0048:  ldtoken    ""C""
  IL_004d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0052:  ldc.i4.1
  IL_0053:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0058:  dup
  IL_0059:  ldc.i4.0
  IL_005a:  ldc.i4.0
  IL_005b:  ldnull
  IL_005c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0061:  stelem.ref
  IL_0062:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Invoke(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0067:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_006c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0071:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0076:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_007b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0080:  ldarg.1
  IL_0081:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0086:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_008b:  ret
}");
        }

        [Fact]
        public void TypeInferenceGenericParameterTainting()
        {
            string source = @"
using System.Collections.Generic;

class C
{
    public void F<T>(Dictionary<T, dynamic> d)
    {
    }

    static void M(C c, Dictionary<dynamic, dynamic> d)
    {
        c.F(d);                                          
    }
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  callvirt   ""void C.F<object>(System.Collections.Generic.Dictionary<object, dynamic>)""
  IL_0007:  ret
}
");
        }

        [Fact]
        public void InvokeMember_InConstructorInitializer()
        {
            string source = @"
class B 
{
    protected B(int x) { }
}

class C : B
{
    C(dynamic x) : base((int)Foo(x)) { }
 
    static object Foo(object x)
    {
        return x;
    }
}";
            CompileAndVerifyIL(source, "C..ctor", @"
{
  // Code size      168 (0xa8)
  .maxstack  12
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__1""
  IL_0006:  brtrue.s   IL_002d
  IL_0008:  ldc.i4.s   16
  IL_000a:  ldtoken    ""int""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldtoken    ""C""
  IL_0019:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0023:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0028:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__1""
  IL_002d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__1""
  IL_0032:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>>.Target""
  IL_0037:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__1""
  IL_003c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, object, object>> C.<>o__0.<>p__0""
  IL_0041:  brtrue.s   IL_007e
  IL_0043:  ldc.i4.0
  IL_0044:  ldstr      ""Foo""
  IL_0049:  ldnull
  IL_004a:  ldtoken    ""C""
  IL_004f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0054:  ldc.i4.2
  IL_0055:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_005a:  dup
  IL_005b:  ldc.i4.0
  IL_005c:  ldc.i4.s   33
  IL_005e:  ldnull
  IL_005f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0064:  stelem.ref
  IL_0065:  dup
  IL_0066:  ldc.i4.1
  IL_0067:  ldc.i4.0
  IL_0068:  ldnull
  IL_0069:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_006e:  stelem.ref
  IL_006f:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0074:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0079:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, object, object>> C.<>o__0.<>p__0""
  IL_007e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, object, object>> C.<>o__0.<>p__0""
  IL_0083:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, System.Type, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, object, object>>.Target""
  IL_0088:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, object, object>> C.<>o__0.<>p__0""
  IL_008d:  ldtoken    ""C""
  IL_0092:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0097:  ldarg.1
  IL_0098:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, System.Type, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, object)""
  IL_009d:  callvirt   ""int System.Func<System.Runtime.CompilerServices.CallSite, object, int>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_00a2:  call       ""B..ctor(int)""
  IL_00a7:  ret
}
");
        }

        [Fact]
        public void Invoke_Field_InConstructorInitializer()
        {
            string source = @"
using System;

class B 
{
    protected B(int x) { }
}

class C : B
{
    C(dynamic x) : base((int)Foo(x)) { }
 
    static Action<object> Foo;
}
";
            CompileAndVerifyIL(source, "C..ctor", @"
{
  // Code size      156 (0x9c)
  .maxstack  10
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__1""
  IL_0006:  brtrue.s   IL_002d
  IL_0008:  ldc.i4.s   16
  IL_000a:  ldtoken    ""int""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldtoken    ""C""
  IL_0019:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0023:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0028:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__1""
  IL_002d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__1""
  IL_0032:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>>.Target""
  IL_0037:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__1""
  IL_003c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Action<object>, object, object>> C.<>o__0.<>p__0""
  IL_0041:  brtrue.s   IL_0077
  IL_0043:  ldc.i4.0
  IL_0044:  ldtoken    ""C""
  IL_0049:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_004e:  ldc.i4.2
  IL_004f:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0054:  dup
  IL_0055:  ldc.i4.0
  IL_0056:  ldc.i4.1
  IL_0057:  ldnull
  IL_0058:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_005d:  stelem.ref
  IL_005e:  dup
  IL_005f:  ldc.i4.1
  IL_0060:  ldc.i4.0
  IL_0061:  ldnull
  IL_0062:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0067:  stelem.ref
  IL_0068:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Invoke(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_006d:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Action<object>, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Action<object>, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0072:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Action<object>, object, object>> C.<>o__0.<>p__0""
  IL_0077:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Action<object>, object, object>> C.<>o__0.<>p__0""
  IL_007c:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, System.Action<object>, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Action<object>, object, object>>.Target""
  IL_0081:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Action<object>, object, object>> C.<>o__0.<>p__0""
  IL_0086:  ldsfld     ""System.Action<object> C.Foo""
  IL_008b:  ldarg.1
  IL_008c:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, System.Action<object>, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, System.Action<object>, object)""
  IL_0091:  callvirt   ""int System.Func<System.Runtime.CompilerServices.CallSite, object, int>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0096:  call       ""B..ctor(int)""
  IL_009b:  ret
}
");
        }

        [Fact]
        public void Invoke_Property_InConstructorInitializer()
        {
            string source = @"
using System;

class B 
{
    protected B(int x) { }
}

class C : B
{
    C(dynamic x) : base((int)Foo(x)) { }
 
    static Action<object> Foo { get; set; }
}
";
            CompileAndVerifyIL(source, "C..ctor", @"
{
  // Code size      156 (0x9c)
  .maxstack  10
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__1""
  IL_0006:  brtrue.s   IL_002d
  IL_0008:  ldc.i4.s   16
  IL_000a:  ldtoken    ""int""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldtoken    ""C""
  IL_0019:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0023:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0028:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__1""
  IL_002d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__1""
  IL_0032:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>>.Target""
  IL_0037:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__1""
  IL_003c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Action<object>, object, object>> C.<>o__0.<>p__0""
  IL_0041:  brtrue.s   IL_0077
  IL_0043:  ldc.i4.0
  IL_0044:  ldtoken    ""C""
  IL_0049:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_004e:  ldc.i4.2
  IL_004f:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0054:  dup
  IL_0055:  ldc.i4.0
  IL_0056:  ldc.i4.1
  IL_0057:  ldnull
  IL_0058:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_005d:  stelem.ref
  IL_005e:  dup
  IL_005f:  ldc.i4.1
  IL_0060:  ldc.i4.0
  IL_0061:  ldnull
  IL_0062:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0067:  stelem.ref
  IL_0068:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Invoke(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_006d:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Action<object>, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Action<object>, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0072:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Action<object>, object, object>> C.<>o__0.<>p__0""
  IL_0077:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Action<object>, object, object>> C.<>o__0.<>p__0""
  IL_007c:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, System.Action<object>, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Action<object>, object, object>>.Target""
  IL_0081:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Action<object>, object, object>> C.<>o__0.<>p__0""
  IL_0086:  call       ""System.Action<object> C.Foo.get""
  IL_008b:  ldarg.1
  IL_008c:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, System.Action<object>, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, System.Action<object>, object)""
  IL_0091:  callvirt   ""int System.Func<System.Runtime.CompilerServices.CallSite, object, int>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0096:  call       ""B..ctor(int)""
  IL_009b:  ret
}
");
        }

        #endregion

        #region GetMember, GetIndex, SetMember, SetIndex

        [Fact]
        public void GetMember()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return d.m;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       76 (0x4c)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0036
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      ""m""
  IL_000d:  ldtoken    ""C""
  IL_0012:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0017:  ldc.i4.1
  IL_0018:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001d:  dup
  IL_001e:  ldc.i4.0
  IL_001f:  ldc.i4.0
  IL_0020:  ldnull
  IL_0021:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0026:  stelem.ref
  IL_0027:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_002c:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0031:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0036:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_003b:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_0040:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0045:  ldarg.1
  IL_0046:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_004b:  ret
}
");
        }

        [Fact]
        public void GetMember_GetMember()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return d.m.n;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      150 (0x96)
  .maxstack  10
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__1""
  IL_0005:  brtrue.s   IL_0036
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      ""n""
  IL_000d:  ldtoken    ""C""
  IL_0012:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0017:  ldc.i4.1
  IL_0018:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001d:  dup
  IL_001e:  ldc.i4.0
  IL_001f:  ldc.i4.0
  IL_0020:  ldnull
  IL_0021:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0026:  stelem.ref
  IL_0027:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_002c:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0031:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__1""
  IL_0036:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__1""
  IL_003b:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_0040:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__1""
  IL_0045:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_004a:  brtrue.s   IL_007b
  IL_004c:  ldc.i4.0
  IL_004d:  ldstr      ""m""
  IL_0052:  ldtoken    ""C""
  IL_0057:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005c:  ldc.i4.1
  IL_005d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0062:  dup
  IL_0063:  ldc.i4.0
  IL_0064:  ldc.i4.0
  IL_0065:  ldnull
  IL_0066:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_006b:  stelem.ref
  IL_006c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0071:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0076:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_007b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0080:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_0085:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_008a:  ldarg.1
  IL_008b:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0090:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0095:  ret
}
");
        }

        [Fact]
        public void GetIndex()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return d[1];
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       82 (0x52)
  .maxstack  7
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_003b
  IL_0007:  ldc.i4.0
  IL_0008:  ldtoken    ""C""
  IL_000d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0012:  ldc.i4.2
  IL_0013:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0018:  dup
  IL_0019:  ldc.i4.0
  IL_001a:  ldc.i4.0
  IL_001b:  ldnull
  IL_001c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0021:  stelem.ref
  IL_0022:  dup
  IL_0023:  ldc.i4.1
  IL_0024:  ldc.i4.3
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0031:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0036:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__0.<>p__0""
  IL_003b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__0.<>p__0""
  IL_0040:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>>.Target""
  IL_0045:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__0.<>p__0""
  IL_004a:  ldarg.1
  IL_004b:  ldc.i4.1
  IL_004c:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, int)""
  IL_0051:  ret
}
");
        }

        [Fact]
        public void GetMember_GetIndex()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return d.m[1];
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      157 (0x9d)
  .maxstack  10
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__0.<>p__1""
  IL_0005:  brtrue.s   IL_003b
  IL_0007:  ldc.i4.0
  IL_0008:  ldtoken    ""C""
  IL_000d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0012:  ldc.i4.2
  IL_0013:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0018:  dup
  IL_0019:  ldc.i4.0
  IL_001a:  ldc.i4.0
  IL_001b:  ldnull
  IL_001c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0021:  stelem.ref
  IL_0022:  dup
  IL_0023:  ldc.i4.1
  IL_0024:  ldc.i4.3
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0031:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0036:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__0.<>p__1""
  IL_003b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__0.<>p__1""
  IL_0040:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>>.Target""
  IL_0045:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__0.<>p__1""
  IL_004a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_004f:  brtrue.s   IL_0081
  IL_0051:  ldc.i4.s   64
  IL_0053:  ldstr      ""m""
  IL_0058:  ldtoken    ""C""
  IL_005d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0062:  ldc.i4.1
  IL_0063:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0068:  dup
  IL_0069:  ldc.i4.0
  IL_006a:  ldc.i4.0
  IL_006b:  ldnull
  IL_006c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0071:  stelem.ref
  IL_0072:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0077:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_007c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0081:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0086:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_008b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
  IL_0090:  ldarg.1
  IL_0091:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0096:  ldc.i4.1
  IL_0097:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, int)""
  IL_009c:  ret
}
");
        }

        [Fact]
        public void GetIndex_GetIndex()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return d[1][2];
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      162 (0xa2)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__0.<>p__1""
  IL_0005:  brtrue.s   IL_003b
  IL_0007:  ldc.i4.0
  IL_0008:  ldtoken    ""C""
  IL_000d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0012:  ldc.i4.2
  IL_0013:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0018:  dup
  IL_0019:  ldc.i4.0
  IL_001a:  ldc.i4.0
  IL_001b:  ldnull
  IL_001c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0021:  stelem.ref
  IL_0022:  dup
  IL_0023:  ldc.i4.1
  IL_0024:  ldc.i4.3
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0031:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0036:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__0.<>p__1""
  IL_003b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__0.<>p__1""
  IL_0040:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>>.Target""
  IL_0045:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__0.<>p__1""
  IL_004a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__0.<>p__0""
  IL_004f:  brtrue.s   IL_0085
  IL_0051:  ldc.i4.0
  IL_0052:  ldtoken    ""C""
  IL_0057:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005c:  ldc.i4.2
  IL_005d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0062:  dup
  IL_0063:  ldc.i4.0
  IL_0064:  ldc.i4.0
  IL_0065:  ldnull
  IL_0066:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_006b:  stelem.ref
  IL_006c:  dup
  IL_006d:  ldc.i4.1
  IL_006e:  ldc.i4.3
  IL_006f:  ldnull
  IL_0070:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0075:  stelem.ref
  IL_0076:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_007b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0080:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__0.<>p__0""
  IL_0085:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__0.<>p__0""
  IL_008a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>>.Target""
  IL_008f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__0.<>p__0""
  IL_0094:  ldarg.1
  IL_0095:  ldc.i4.1
  IL_0096:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, int)""
  IL_009b:  ldc.i4.2
  IL_009c:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, int)""
  IL_00a1:  ret
}
");
        }

        [Fact]
        public void GetIndex_ManyArgs_F15()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return d[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15];
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      254 (0xfe)
  .maxstack  18
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue     IL_00d2
  IL_000a:  ldc.i4.0
  IL_000b:  ldtoken    ""C""
  IL_0010:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0015:  ldc.i4.s   16
  IL_0017:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001c:  dup
  IL_001d:  ldc.i4.0
  IL_001e:  ldc.i4.0
  IL_001f:  ldnull
  IL_0020:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0025:  stelem.ref
  IL_0026:  dup
  IL_0027:  ldc.i4.1
  IL_0028:  ldc.i4.3
  IL_0029:  ldnull
  IL_002a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002f:  stelem.ref
  IL_0030:  dup
  IL_0031:  ldc.i4.2
  IL_0032:  ldc.i4.3
  IL_0033:  ldnull
  IL_0034:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0039:  stelem.ref
  IL_003a:  dup
  IL_003b:  ldc.i4.3
  IL_003c:  ldc.i4.3
  IL_003d:  ldnull
  IL_003e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0043:  stelem.ref
  IL_0044:  dup
  IL_0045:  ldc.i4.4
  IL_0046:  ldc.i4.3
  IL_0047:  ldnull
  IL_0048:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_004d:  stelem.ref
  IL_004e:  dup
  IL_004f:  ldc.i4.5
  IL_0050:  ldc.i4.3
  IL_0051:  ldnull
  IL_0052:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0057:  stelem.ref
  IL_0058:  dup
  IL_0059:  ldc.i4.6
  IL_005a:  ldc.i4.3
  IL_005b:  ldnull
  IL_005c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0061:  stelem.ref
  IL_0062:  dup
  IL_0063:  ldc.i4.7
  IL_0064:  ldc.i4.3
  IL_0065:  ldnull
  IL_0066:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_006b:  stelem.ref
  IL_006c:  dup
  IL_006d:  ldc.i4.8
  IL_006e:  ldc.i4.3
  IL_006f:  ldnull
  IL_0070:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0075:  stelem.ref
  IL_0076:  dup
  IL_0077:  ldc.i4.s   9
  IL_0079:  ldc.i4.3
  IL_007a:  ldnull
  IL_007b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0080:  stelem.ref
  IL_0081:  dup
  IL_0082:  ldc.i4.s   10
  IL_0084:  ldc.i4.3
  IL_0085:  ldnull
  IL_0086:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_008b:  stelem.ref
  IL_008c:  dup
  IL_008d:  ldc.i4.s   11
  IL_008f:  ldc.i4.3
  IL_0090:  ldnull
  IL_0091:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0096:  stelem.ref
  IL_0097:  dup
  IL_0098:  ldc.i4.s   12
  IL_009a:  ldc.i4.3
  IL_009b:  ldnull
  IL_009c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00a1:  stelem.ref
  IL_00a2:  dup
  IL_00a3:  ldc.i4.s   13
  IL_00a5:  ldc.i4.3
  IL_00a6:  ldnull
  IL_00a7:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00ac:  stelem.ref
  IL_00ad:  dup
  IL_00ae:  ldc.i4.s   14
  IL_00b0:  ldc.i4.3
  IL_00b1:  ldnull
  IL_00b2:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00b7:  stelem.ref
  IL_00b8:  dup
  IL_00b9:  ldc.i4.s   15
  IL_00bb:  ldc.i4.3
  IL_00bc:  ldnull
  IL_00bd:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00c2:  stelem.ref
  IL_00c3:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00c8:  call       ""System.Runtime.CompilerServices.CallSite<<>F<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, object>> System.Runtime.CompilerServices.CallSite<<>F<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00cd:  stsfld     ""System.Runtime.CompilerServices.CallSite<<>F<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, object>> C.<>o__0.<>p__0""
  IL_00d2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, object>> C.<>o__0.<>p__0""
  IL_00d7:  ldfld      ""<>F<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, object> System.Runtime.CompilerServices.CallSite<<>F<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, object>>.Target""
  IL_00dc:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, object>> C.<>o__0.<>p__0""
  IL_00e1:  ldarg.1
  IL_00e2:  ldc.i4.1
  IL_00e3:  ldc.i4.2
  IL_00e4:  ldc.i4.3
  IL_00e5:  ldc.i4.4
  IL_00e6:  ldc.i4.5
  IL_00e7:  ldc.i4.6
  IL_00e8:  ldc.i4.7
  IL_00e9:  ldc.i4.8
  IL_00ea:  ldc.i4.s   9
  IL_00ec:  ldc.i4.s   10
  IL_00ee:  ldc.i4.s   11
  IL_00f0:  ldc.i4.s   12
  IL_00f2:  ldc.i4.s   13
  IL_00f4:  ldc.i4.s   14
  IL_00f6:  ldc.i4.s   15
  IL_00f8:  callvirt   ""object <>F<System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int)""
  IL_00fd:  ret
}
");
        }

        [Fact]
        public void GetIndex_ByRef()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return d[ref d];
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       84 (0x54)
  .maxstack  7
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F{00000004}<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_003c
  IL_0007:  ldc.i4.0
  IL_0008:  ldtoken    ""C""
  IL_000d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0012:  ldc.i4.2
  IL_0013:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0018:  dup
  IL_0019:  ldc.i4.0
  IL_001a:  ldc.i4.0
  IL_001b:  ldnull
  IL_001c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0021:  stelem.ref
  IL_0022:  dup
  IL_0023:  ldc.i4.1
  IL_0024:  ldc.i4.s   9
  IL_0026:  ldnull
  IL_0027:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002c:  stelem.ref
  IL_002d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0032:  call       ""System.Runtime.CompilerServices.CallSite<<>F{00000004}<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<<>F{00000004}<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0037:  stsfld     ""System.Runtime.CompilerServices.CallSite<<>F{00000004}<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_003c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F{00000004}<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0041:  ldfld      ""<>F{00000004}<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<<>F{00000004}<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0046:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F{00000004}<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_004b:  ldarg.1
  IL_004c:  ldarga.s   V_1
  IL_004e:  callvirt   ""object <>F{00000004}<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, ref object)""
  IL_0053:  ret
}
");
        }

        [Fact]
        public void GetIndex_NamedArguments()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return d[a: 1, b: d, c: null, d: ref d];
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      133 (0x85)
  .maxstack  7
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F{00000020}<System.Runtime.CompilerServices.CallSite, object, int, object, object, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_006a
  IL_0007:  ldc.i4.0
  IL_0008:  ldtoken    ""C""
  IL_000d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0012:  ldc.i4.5
  IL_0013:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0018:  dup
  IL_0019:  ldc.i4.0
  IL_001a:  ldc.i4.0
  IL_001b:  ldnull
  IL_001c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0021:  stelem.ref
  IL_0022:  dup
  IL_0023:  ldc.i4.1
  IL_0024:  ldc.i4.7
  IL_0025:  ldstr      ""a""
  IL_002a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002f:  stelem.ref
  IL_0030:  dup
  IL_0031:  ldc.i4.2
  IL_0032:  ldc.i4.4
  IL_0033:  ldstr      ""b""
  IL_0038:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_003d:  stelem.ref
  IL_003e:  dup
  IL_003f:  ldc.i4.3
  IL_0040:  ldc.i4.6
  IL_0041:  ldstr      ""c""
  IL_0046:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_004b:  stelem.ref
  IL_004c:  dup
  IL_004d:  ldc.i4.4
  IL_004e:  ldc.i4.s   13
  IL_0050:  ldstr      ""d""
  IL_0055:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_005a:  stelem.ref
  IL_005b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0060:  call       ""System.Runtime.CompilerServices.CallSite<<>F{00000020}<System.Runtime.CompilerServices.CallSite, object, int, object, object, object, object>> System.Runtime.CompilerServices.CallSite<<>F{00000020}<System.Runtime.CompilerServices.CallSite, object, int, object, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0065:  stsfld     ""System.Runtime.CompilerServices.CallSite<<>F{00000020}<System.Runtime.CompilerServices.CallSite, object, int, object, object, object, object>> C.<>o__0.<>p__0""
  IL_006a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F{00000020}<System.Runtime.CompilerServices.CallSite, object, int, object, object, object, object>> C.<>o__0.<>p__0""
  IL_006f:  ldfld      ""<>F{00000020}<System.Runtime.CompilerServices.CallSite, object, int, object, object, object, object> System.Runtime.CompilerServices.CallSite<<>F{00000020}<System.Runtime.CompilerServices.CallSite, object, int, object, object, object, object>>.Target""
  IL_0074:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F{00000020}<System.Runtime.CompilerServices.CallSite, object, int, object, object, object, object>> C.<>o__0.<>p__0""
  IL_0079:  ldarg.1
  IL_007a:  ldc.i4.1
  IL_007b:  ldarg.1
  IL_007c:  ldnull
  IL_007d:  ldarga.s   V_1
  IL_007f:  callvirt   ""object <>F{00000020}<System.Runtime.CompilerServices.CallSite, object, int, object, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, int, object, object, ref object)""
  IL_0084:  ret
}
");
        }

        [Fact]
        public void GetIndex_StaticReceiver()
        {
            string source = @"
public class C
{
    C a, b;

    int this[int i] { get { return 0; } set { } }

    public dynamic M(dynamic d)
    {
        return a.b[d];
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       92 (0x5c)
  .maxstack  7
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, object>> C.<>o__5.<>p__0""
  IL_0005:  brtrue.s   IL_003b
  IL_0007:  ldc.i4.0
  IL_0008:  ldtoken    ""C""
  IL_000d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0012:  ldc.i4.2
  IL_0013:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0018:  dup
  IL_0019:  ldc.i4.0
  IL_001a:  ldc.i4.1
  IL_001b:  ldnull
  IL_001c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0021:  stelem.ref
  IL_0022:  dup
  IL_0023:  ldc.i4.1
  IL_0024:  ldc.i4.0
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0031:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0036:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, object>> C.<>o__5.<>p__0""
  IL_003b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, object>> C.<>o__5.<>p__0""
  IL_0040:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, C, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, object>>.Target""
  IL_0045:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, object>> C.<>o__5.<>p__0""
  IL_004a:  ldarg.0
  IL_004b:  ldfld      ""C C.a""
  IL_0050:  ldfld      ""C C.b""
  IL_0055:  ldarg.1
  IL_0056:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, C, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, C, object)""
  IL_005b:  ret
}
");
        }

        [Fact]
        public void GetIndex_IndexedProperty()
        {
            string vbSource = @"
Imports System.Runtime.InteropServices

<ComImport, Guid(""0002095E-0000-0000-C000-000000000046"")>
Public Class B
    Public ReadOnly Property IndexedProperty(arg As Integer) As Integer
        Get
            Return arg
        End Get
    End Property
End Class
";
            var vb = CreateVisualBasicCompilation(GetUniqueName(), vbSource);
            var vbRef = vb.EmitToImageReference();

            string source = @"
class C
{
    B b;
    dynamic d;

    object M() 
    {
        return b.IndexedProperty[d];
    }
}
";
            // Dev11 - the receiver of GetMember is typed to Object. That seems like a bug, since InvokeMember on an early bound receiver uses the compile time type.
            // Roslyn: Use strongly typed receiver.
            // Note that accessing an indexed property is the only way how to get strongly typed receiver.

            CompileAndVerifyIL(source, "C.M", references: new MetadataReference[] { SystemCoreRef, CSharpRef, vbRef }, expectedOptimizedIL: @"
{
  // Code size      167 (0xa7)
  .maxstack  10
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__1""
  IL_0005:  brtrue.s   IL_003b
  IL_0007:  ldc.i4.0
  IL_0008:  ldtoken    ""C""
  IL_000d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0012:  ldc.i4.2
  IL_0013:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0018:  dup
  IL_0019:  ldc.i4.0
  IL_001a:  ldc.i4.0
  IL_001b:  ldnull
  IL_001c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0021:  stelem.ref
  IL_0022:  dup
  IL_0023:  ldc.i4.1
  IL_0024:  ldc.i4.0
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0031:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0036:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__1""
  IL_003b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__1""
  IL_0040:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0045:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__1""
  IL_004a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object>> C.<>o__2.<>p__0""
  IL_004f:  brtrue.s   IL_0081
  IL_0051:  ldc.i4.s   64
  IL_0053:  ldstr      ""IndexedProperty""
  IL_0058:  ldtoken    ""C""
  IL_005d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0062:  ldc.i4.1
  IL_0063:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0068:  dup
  IL_0069:  ldc.i4.0
  IL_006a:  ldc.i4.1
  IL_006b:  ldnull
  IL_006c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0071:  stelem.ref
  IL_0072:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0077:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_007c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object>> C.<>o__2.<>p__0""
  IL_0081:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object>> C.<>o__2.<>p__0""
  IL_0086:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, B, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object>>.Target""
  IL_008b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object>> C.<>o__2.<>p__0""
  IL_0090:  ldarg.0
  IL_0091:  ldfld      ""B C.b""
  IL_0096:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, B, object>.Invoke(System.Runtime.CompilerServices.CallSite, B)""
  IL_009b:  ldarg.0
  IL_009c:  ldfld      ""dynamic C.d""
  IL_00a1:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_00a6:  ret
}");
        }

        [Fact]
        public void SetIndex_IndexedProperty()
        {
            string vbSource = @"
Imports System.Runtime.InteropServices

<ComImport, Guid(""0002095E-0000-0000-C000-000000000046"")>
Public Class B
    Public Property IndexedProperty(arg As Integer) As Integer
        Get
            Return arg
        End Get
        Set
        End Set
    End Property
End Class
";
            var vb = CreateVisualBasicCompilation(GetUniqueName(), vbSource);
            var vbRef = vb.EmitToImageReference();

            string source = @"
class C
{
    B b;
    dynamic d;

    object M() 
    {
        return b.IndexedProperty[d] = 42;
    }
}
";
            // Dev11 - the receiver of GetMember is typed to Object. That seems like a bug, since InvokeMember on an early bound receiver uses the compile time type.
            // Roslyn: Use strongly typed receiver.
            // Note that accessing an indexed property is the only way how to get strongly typed receiver.

            CompileAndVerifyIL(source, "C.M", references: new MetadataReference[] { SystemCoreRef, CSharpRef, vbRef }, expectedOptimizedIL: @"
{
  // Code size      179 (0xb3)
  .maxstack  10
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object>> C.<>o__2.<>p__1""
  IL_0005:  brtrue.s   IL_0045
  IL_0007:  ldc.i4.0
  IL_0008:  ldtoken    ""C""
  IL_000d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0012:  ldc.i4.3
  IL_0013:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0018:  dup
  IL_0019:  ldc.i4.0
  IL_001a:  ldc.i4.0
  IL_001b:  ldnull
  IL_001c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0021:  stelem.ref
  IL_0022:  dup
  IL_0023:  ldc.i4.1
  IL_0024:  ldc.i4.0
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  dup
  IL_002d:  ldc.i4.2
  IL_002e:  ldc.i4.3
  IL_002f:  ldnull
  IL_0030:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0035:  stelem.ref
  IL_0036:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0040:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object>> C.<>o__2.<>p__1""
  IL_0045:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object>> C.<>o__2.<>p__1""
  IL_004a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object>>.Target""
  IL_004f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object>> C.<>o__2.<>p__1""
  IL_0054:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object>> C.<>o__2.<>p__0""
  IL_0059:  brtrue.s   IL_008b
  IL_005b:  ldc.i4.s   64
  IL_005d:  ldstr      ""IndexedProperty""
  IL_0062:  ldtoken    ""C""
  IL_0067:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_006c:  ldc.i4.1
  IL_006d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0072:  dup
  IL_0073:  ldc.i4.0
  IL_0074:  ldc.i4.1
  IL_0075:  ldnull
  IL_0076:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_007b:  stelem.ref
  IL_007c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0081:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0086:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object>> C.<>o__2.<>p__0""
  IL_008b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object>> C.<>o__2.<>p__0""
  IL_0090:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, B, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object>>.Target""
  IL_0095:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object>> C.<>o__2.<>p__0""
  IL_009a:  ldarg.0
  IL_009b:  ldfld      ""B C.b""
  IL_00a0:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, B, object>.Invoke(System.Runtime.CompilerServices.CallSite, B)""
  IL_00a5:  ldarg.0
  IL_00a6:  ldfld      ""dynamic C.d""
  IL_00ab:  ldc.i4.s   42
  IL_00ad:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object, int)""
  IL_00b2:  ret
}");
        }

        [Fact]
        public void SetIndex_IndexedProperty_CompoundAssignment()
        {
            string vbSource = @"
Imports System.Runtime.InteropServices

<ComImport, Guid(""0002095E-0000-0000-C000-000000000046"")>
Public Class B
    Public Property IndexedProperty(arg As Integer) As Integer
        Get
            Return arg
        End Get
        Set
        End Set
    End Property
End Class
";
            var vb = CreateVisualBasicCompilation(GetUniqueName(), vbSource);
            var vbRef = vb.EmitToImageReference();

            string source = @"
class C
{
    B b;
    dynamic d;

    object M() 
    {
        return b.IndexedProperty[d] += 42;
    }
}
";
            // Dev11 - the receiver of GetMember is typed to Object. That seems like a bug, since InvokeMember on an early bound receiver uses the compile time type.
            // Roslyn: Use strongly typed receiver.
            // Note that accessing an indexed property is the only way how to get strongly typed receiver.

            CompileAndVerifyIL(source, "C.M", references: new MetadataReference[] { SystemCoreRef, CSharpRef, vbRef }, expectedOptimizedIL: @"
{
  // Code size      424 (0x1a8)
  .maxstack  16
  .locals init (B V_0,
                object V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""B C.b""
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldfld      ""dynamic C.d""
  IL_000d:  stloc.1
  IL_000e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object, object>> C.<>o__2.<>p__4""
  IL_0013:  brtrue.s   IL_0057
  IL_0015:  ldc.i4     0x80
  IL_001a:  ldtoken    ""C""
  IL_001f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0024:  ldc.i4.3
  IL_0025:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_002a:  dup
  IL_002b:  ldc.i4.0
  IL_002c:  ldc.i4.0
  IL_002d:  ldnull
  IL_002e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0033:  stelem.ref
  IL_0034:  dup
  IL_0035:  ldc.i4.1
  IL_0036:  ldc.i4.0
  IL_0037:  ldnull
  IL_0038:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_003d:  stelem.ref
  IL_003e:  dup
  IL_003f:  ldc.i4.2
  IL_0040:  ldc.i4.0
  IL_0041:  ldnull
  IL_0042:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0047:  stelem.ref
  IL_0048:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_004d:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0052:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object, object>> C.<>o__2.<>p__4""
  IL_0057:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object, object>> C.<>o__2.<>p__4""
  IL_005c:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object, object>>.Target""
  IL_0061:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object, object>> C.<>o__2.<>p__4""
  IL_0066:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object>> C.<>o__2.<>p__3""
  IL_006b:  brtrue.s   IL_009d
  IL_006d:  ldc.i4.s   64
  IL_006f:  ldstr      ""IndexedProperty""
  IL_0074:  ldtoken    ""C""
  IL_0079:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_007e:  ldc.i4.1
  IL_007f:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0084:  dup
  IL_0085:  ldc.i4.0
  IL_0086:  ldc.i4.1
  IL_0087:  ldnull
  IL_0088:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_008d:  stelem.ref
  IL_008e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0093:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0098:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object>> C.<>o__2.<>p__3""
  IL_009d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object>> C.<>o__2.<>p__3""
  IL_00a2:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, B, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object>>.Target""
  IL_00a7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object>> C.<>o__2.<>p__3""
  IL_00ac:  ldloc.0
  IL_00ad:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, B, object>.Invoke(System.Runtime.CompilerServices.CallSite, B)""
  IL_00b2:  ldloc.1
  IL_00b3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__2.<>p__2""
  IL_00b8:  brtrue.s   IL_00f0
  IL_00ba:  ldc.i4.0
  IL_00bb:  ldc.i4.s   63
  IL_00bd:  ldtoken    ""C""
  IL_00c2:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00c7:  ldc.i4.2
  IL_00c8:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00cd:  dup
  IL_00ce:  ldc.i4.0
  IL_00cf:  ldc.i4.0
  IL_00d0:  ldnull
  IL_00d1:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00d6:  stelem.ref
  IL_00d7:  dup
  IL_00d8:  ldc.i4.1
  IL_00d9:  ldc.i4.3
  IL_00da:  ldnull
  IL_00db:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00e0:  stelem.ref
  IL_00e1:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00e6:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00eb:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__2.<>p__2""
  IL_00f0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__2.<>p__2""
  IL_00f5:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>>.Target""
  IL_00fa:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__2.<>p__2""
  IL_00ff:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__1""
  IL_0104:  brtrue.s   IL_013a
  IL_0106:  ldc.i4.0
  IL_0107:  ldtoken    ""C""
  IL_010c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0111:  ldc.i4.2
  IL_0112:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0117:  dup
  IL_0118:  ldc.i4.0
  IL_0119:  ldc.i4.0
  IL_011a:  ldnull
  IL_011b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0120:  stelem.ref
  IL_0121:  dup
  IL_0122:  ldc.i4.1
  IL_0123:  ldc.i4.0
  IL_0124:  ldnull
  IL_0125:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_012a:  stelem.ref
  IL_012b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0130:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0135:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__1""
  IL_013a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__1""
  IL_013f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0144:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__1""
  IL_0149:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object>> C.<>o__2.<>p__0""
  IL_014e:  brtrue.s   IL_0180
  IL_0150:  ldc.i4.s   64
  IL_0152:  ldstr      ""IndexedProperty""
  IL_0157:  ldtoken    ""C""
  IL_015c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0161:  ldc.i4.1
  IL_0162:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0167:  dup
  IL_0168:  ldc.i4.0
  IL_0169:  ldc.i4.1
  IL_016a:  ldnull
  IL_016b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0170:  stelem.ref
  IL_0171:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0176:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_017b:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object>> C.<>o__2.<>p__0""
  IL_0180:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object>> C.<>o__2.<>p__0""
  IL_0185:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, B, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object>>.Target""
  IL_018a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object>> C.<>o__2.<>p__0""
  IL_018f:  ldloc.0
  IL_0190:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, B, object>.Invoke(System.Runtime.CompilerServices.CallSite, B)""
  IL_0195:  ldloc.1
  IL_0196:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_019b:  ldc.i4.s   42
  IL_019d:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, int)""
  IL_01a2:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object, object)""
  IL_01a7:  ret
}
");
        }

        [Fact]
        public void GetIndex_IndexerWithByRefParam()
        {
            var ilRef = MetadataReference.CreateFromImage(TestResources.MetadataTests.Interop.IndexerWithByRefParam.AsImmutableOrNull());

            string source = @"
class C
{
    B b;
    dynamic d;

    object M() 
    {
        return b[d];
    }
}
";
            CompileAndVerifyIL(source, "C.M", references: new MetadataReference[] { SystemCoreRef, CSharpRef, ilRef }, expectedOptimizedIL: @"
{
  // Code size       92 (0x5c)
  .maxstack  7
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>> C.<>o__2.<>p__0""
  IL_0005:  brtrue.s   IL_003b
  IL_0007:  ldc.i4.0
  IL_0008:  ldtoken    ""C""
  IL_000d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0012:  ldc.i4.2
  IL_0013:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0018:  dup
  IL_0019:  ldc.i4.0
  IL_001a:  ldc.i4.1
  IL_001b:  ldnull
  IL_001c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0021:  stelem.ref
  IL_0022:  dup
  IL_0023:  ldc.i4.1
  IL_0024:  ldc.i4.0
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0031:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0036:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>> C.<>o__2.<>p__0""
  IL_003b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>> C.<>o__2.<>p__0""
  IL_0040:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, B, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>>.Target""
  IL_0045:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>> C.<>o__2.<>p__0""
  IL_004a:  ldarg.0
  IL_004b:  ldfld      ""B C.b""
  IL_0050:  ldarg.0
  IL_0051:  ldfld      ""dynamic C.d""
  IL_0056:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, B, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, B, object)""
  IL_005b:  ret
}
");
        }

        [Fact]
        public void SetIndex_SetIndex_Receiver()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return (d[1] = d)[2];
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      173 (0xad)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__0.<>p__1""
  IL_0005:  brtrue.s   IL_003b
  IL_0007:  ldc.i4.0
  IL_0008:  ldtoken    ""C""
  IL_000d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0012:  ldc.i4.2
  IL_0013:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0018:  dup
  IL_0019:  ldc.i4.0
  IL_001a:  ldc.i4.0
  IL_001b:  ldnull
  IL_001c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0021:  stelem.ref
  IL_0022:  dup
  IL_0023:  ldc.i4.1
  IL_0024:  ldc.i4.3
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0031:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0036:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__0.<>p__1""
  IL_003b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__0.<>p__1""
  IL_0040:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>>.Target""
  IL_0045:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__0.<>p__1""
  IL_004a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object, object>> C.<>o__0.<>p__0""
  IL_004f:  brtrue.s   IL_008f
  IL_0051:  ldc.i4.0
  IL_0052:  ldtoken    ""C""
  IL_0057:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005c:  ldc.i4.3
  IL_005d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0062:  dup
  IL_0063:  ldc.i4.0
  IL_0064:  ldc.i4.0
  IL_0065:  ldnull
  IL_0066:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_006b:  stelem.ref
  IL_006c:  dup
  IL_006d:  ldc.i4.1
  IL_006e:  ldc.i4.3
  IL_006f:  ldnull
  IL_0070:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0075:  stelem.ref
  IL_0076:  dup
  IL_0077:  ldc.i4.2
  IL_0078:  ldc.i4.0
  IL_0079:  ldnull
  IL_007a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_007f:  stelem.ref
  IL_0080:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0085:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_008a:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object, object>> C.<>o__0.<>p__0""
  IL_008f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object, object>> C.<>o__0.<>p__0""
  IL_0094:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object, object>>.Target""
  IL_0099:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object, object>> C.<>o__0.<>p__0""
  IL_009e:  ldarg.1
  IL_009f:  ldc.i4.1
  IL_00a0:  ldarg.1
  IL_00a1:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, int, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, int, object)""
  IL_00a6:  ldc.i4.2
  IL_00a7:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, int)""
  IL_00ac:  ret
}
");
        }

        [Fact]
        public void SetIndex_SetIndex_Argument()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return d[d[d] = d] = d;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      184 (0xb8)
  .maxstack  10
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object, object>> C.<>o__0.<>p__1""
  IL_0005:  brtrue.s   IL_0045
  IL_0007:  ldc.i4.0
  IL_0008:  ldtoken    ""C""
  IL_000d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0012:  ldc.i4.3
  IL_0013:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0018:  dup
  IL_0019:  ldc.i4.0
  IL_001a:  ldc.i4.0
  IL_001b:  ldnull
  IL_001c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0021:  stelem.ref
  IL_0022:  dup
  IL_0023:  ldc.i4.1
  IL_0024:  ldc.i4.0
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  dup
  IL_002d:  ldc.i4.2
  IL_002e:  ldc.i4.0
  IL_002f:  ldnull
  IL_0030:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0035:  stelem.ref
  IL_0036:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0040:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object, object>> C.<>o__0.<>p__1""
  IL_0045:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object, object>> C.<>o__0.<>p__1""
  IL_004a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object, object>>.Target""
  IL_004f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object, object>> C.<>o__0.<>p__1""
  IL_0054:  ldarg.1
  IL_0055:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object, object>> C.<>o__0.<>p__0""
  IL_005a:  brtrue.s   IL_009a
  IL_005c:  ldc.i4.0
  IL_005d:  ldtoken    ""C""
  IL_0062:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0067:  ldc.i4.3
  IL_0068:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_006d:  dup
  IL_006e:  ldc.i4.0
  IL_006f:  ldc.i4.0
  IL_0070:  ldnull
  IL_0071:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0076:  stelem.ref
  IL_0077:  dup
  IL_0078:  ldc.i4.1
  IL_0079:  ldc.i4.0
  IL_007a:  ldnull
  IL_007b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0080:  stelem.ref
  IL_0081:  dup
  IL_0082:  ldc.i4.2
  IL_0083:  ldc.i4.0
  IL_0084:  ldnull
  IL_0085:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_008a:  stelem.ref
  IL_008b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0090:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0095:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object, object>> C.<>o__0.<>p__0""
  IL_009a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object, object>> C.<>o__0.<>p__0""
  IL_009f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object, object>>.Target""
  IL_00a4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object, object>> C.<>o__0.<>p__0""
  IL_00a9:  ldarg.1
  IL_00aa:  ldarg.1
  IL_00ab:  ldarg.1
  IL_00ac:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object, object)""
  IL_00b1:  ldarg.1
  IL_00b2:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object, object)""
  IL_00b7:  ret
}
");
        }

        [Fact]
        public void SetIndex_NamedArguments()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return d[a: d, b: 0, c: null, d: out d] = null;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      144 (0x90)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F{00000020}<System.Runtime.CompilerServices.CallSite, object, object, int, object, object, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0074
  IL_0007:  ldc.i4.0
  IL_0008:  ldtoken    ""C""
  IL_000d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0012:  ldc.i4.6
  IL_0013:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0018:  dup
  IL_0019:  ldc.i4.0
  IL_001a:  ldc.i4.0
  IL_001b:  ldnull
  IL_001c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0021:  stelem.ref
  IL_0022:  dup
  IL_0023:  ldc.i4.1
  IL_0024:  ldc.i4.4
  IL_0025:  ldstr      ""a""
  IL_002a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002f:  stelem.ref
  IL_0030:  dup
  IL_0031:  ldc.i4.2
  IL_0032:  ldc.i4.7
  IL_0033:  ldstr      ""b""
  IL_0038:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_003d:  stelem.ref
  IL_003e:  dup
  IL_003f:  ldc.i4.3
  IL_0040:  ldc.i4.6
  IL_0041:  ldstr      ""c""
  IL_0046:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_004b:  stelem.ref
  IL_004c:  dup
  IL_004d:  ldc.i4.4
  IL_004e:  ldc.i4.s   21
  IL_0050:  ldstr      ""d""
  IL_0055:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_005a:  stelem.ref
  IL_005b:  dup
  IL_005c:  ldc.i4.5
  IL_005d:  ldc.i4.2
  IL_005e:  ldnull
  IL_005f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0064:  stelem.ref
  IL_0065:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_006a:  call       ""System.Runtime.CompilerServices.CallSite<<>F{00000020}<System.Runtime.CompilerServices.CallSite, object, object, int, object, object, object, object>> System.Runtime.CompilerServices.CallSite<<>F{00000020}<System.Runtime.CompilerServices.CallSite, object, object, int, object, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_006f:  stsfld     ""System.Runtime.CompilerServices.CallSite<<>F{00000020}<System.Runtime.CompilerServices.CallSite, object, object, int, object, object, object, object>> C.<>o__0.<>p__0""
  IL_0074:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F{00000020}<System.Runtime.CompilerServices.CallSite, object, object, int, object, object, object, object>> C.<>o__0.<>p__0""
  IL_0079:  ldfld      ""<>F{00000020}<System.Runtime.CompilerServices.CallSite, object, object, int, object, object, object, object> System.Runtime.CompilerServices.CallSite<<>F{00000020}<System.Runtime.CompilerServices.CallSite, object, object, int, object, object, object, object>>.Target""
  IL_007e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F{00000020}<System.Runtime.CompilerServices.CallSite, object, object, int, object, object, object, object>> C.<>o__0.<>p__0""
  IL_0083:  ldarg.1
  IL_0084:  ldarg.1
  IL_0085:  ldc.i4.0
  IL_0086:  ldnull
  IL_0087:  ldarga.s   V_1
  IL_0089:  ldnull
  IL_008a:  callvirt   ""object <>F{00000020}<System.Runtime.CompilerServices.CallSite, object, object, int, object, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object, int, object, ref object, object)""
  IL_008f:  ret
}
");
        }

        [Fact]
        public void SetIndex_ValueTypeReceiver_Local()
        {
            string source = @"
public class C
{
    public void M(dynamic d) 
    {
        S s = new S();
        s[d] = 1;
    }
}

public struct S 
{ 
    public int X;
    public int this[int index] { get { return index; } set { } }
}
";
            // Dev11 produces more efficient code, see bug 547265:
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      104 (0x68)
  .maxstack  7
  .locals init (S V_0) //s
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F{00000002}<System.Runtime.CompilerServices.CallSite, S, object, int, object>> C.<>o__0.<>p__0""
  IL_000d:  brtrue.s   IL_004e
  IL_000f:  ldc.i4.0
  IL_0010:  ldtoken    ""C""
  IL_0015:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001a:  ldc.i4.3
  IL_001b:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0020:  dup
  IL_0021:  ldc.i4.0
  IL_0022:  ldc.i4.s   9
  IL_0024:  ldnull
  IL_0025:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002a:  stelem.ref
  IL_002b:  dup
  IL_002c:  ldc.i4.1
  IL_002d:  ldc.i4.0
  IL_002e:  ldnull
  IL_002f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0034:  stelem.ref
  IL_0035:  dup
  IL_0036:  ldc.i4.2
  IL_0037:  ldc.i4.3
  IL_0038:  ldnull
  IL_0039:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_003e:  stelem.ref
  IL_003f:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0044:  call       ""System.Runtime.CompilerServices.CallSite<<>F{00000002}<System.Runtime.CompilerServices.CallSite, S, object, int, object>> System.Runtime.CompilerServices.CallSite<<>F{00000002}<System.Runtime.CompilerServices.CallSite, S, object, int, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0049:  stsfld     ""System.Runtime.CompilerServices.CallSite<<>F{00000002}<System.Runtime.CompilerServices.CallSite, S, object, int, object>> C.<>o__0.<>p__0""
  IL_004e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F{00000002}<System.Runtime.CompilerServices.CallSite, S, object, int, object>> C.<>o__0.<>p__0""
  IL_0053:  ldfld      ""<>F{00000002}<System.Runtime.CompilerServices.CallSite, S, object, int, object> System.Runtime.CompilerServices.CallSite<<>F{00000002}<System.Runtime.CompilerServices.CallSite, S, object, int, object>>.Target""
  IL_0058:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F{00000002}<System.Runtime.CompilerServices.CallSite, S, object, int, object>> C.<>o__0.<>p__0""
  IL_005d:  ldloca.s   V_0
  IL_005f:  ldarg.1
  IL_0060:  ldc.i4.1
  IL_0061:  callvirt   ""object <>F{00000002}<System.Runtime.CompilerServices.CallSite, S, object, int, object>.Invoke(System.Runtime.CompilerServices.CallSite, ref S, object, int)""
  IL_0066:  pop
  IL_0067:  ret
}");
        }

        [Fact]
        public void SetMember()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return d.m = d;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       87 (0x57)
  .maxstack  8
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0040
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      ""m""
  IL_000d:  ldtoken    ""C""
  IL_0012:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0017:  ldc.i4.2
  IL_0018:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001d:  dup
  IL_001e:  ldc.i4.0
  IL_001f:  ldc.i4.0
  IL_0020:  ldnull
  IL_0021:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0026:  stelem.ref
  IL_0027:  dup
  IL_0028:  ldc.i4.1
  IL_0029:  ldc.i4.0
  IL_002a:  ldnull
  IL_002b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0030:  stelem.ref
  IL_0031:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0036:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_003b:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0040:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0045:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_004a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_004f:  ldarg.1
  IL_0050:  ldarg.1
  IL_0051:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0056:  ret
}
");
        }

        [Fact]
        public void SetMember_SetMember()
        {
            string source = @"
public class C
{
    public dynamic M(dynamic d)
    {
        return (d.a = d).b = d;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      172 (0xac)
  .maxstack  10
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__1""
  IL_0005:  brtrue.s   IL_0040
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      ""b""
  IL_000d:  ldtoken    ""C""
  IL_0012:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0017:  ldc.i4.2
  IL_0018:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001d:  dup
  IL_001e:  ldc.i4.0
  IL_001f:  ldc.i4.0
  IL_0020:  ldnull
  IL_0021:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0026:  stelem.ref
  IL_0027:  dup
  IL_0028:  ldc.i4.1
  IL_0029:  ldc.i4.0
  IL_002a:  ldnull
  IL_002b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0030:  stelem.ref
  IL_0031:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0036:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_003b:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__1""
  IL_0040:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__1""
  IL_0045:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_004a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__1""
  IL_004f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0054:  brtrue.s   IL_008f
  IL_0056:  ldc.i4.0
  IL_0057:  ldstr      ""a""
  IL_005c:  ldtoken    ""C""
  IL_0061:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0066:  ldc.i4.2
  IL_0067:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_006c:  dup
  IL_006d:  ldc.i4.0
  IL_006e:  ldc.i4.0
  IL_006f:  ldnull
  IL_0070:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0075:  stelem.ref
  IL_0076:  dup
  IL_0077:  ldc.i4.1
  IL_0078:  ldc.i4.0
  IL_0079:  ldnull
  IL_007a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_007f:  stelem.ref
  IL_0080:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0085:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_008a:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_008f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0094:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0099:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_009e:  ldarg.1
  IL_009f:  ldarg.1
  IL_00a0:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_00a5:  ldarg.1
  IL_00a6:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_00ab:  ret
}
");
        }

        #endregion

        #region Assignment

        [Fact]
        public void AssignmentRhsConversion()
        {
            string source = @"
public class C
{
    public void M(dynamic p) 
    {
        dynamic d = 1;
        p = 2;
        d.f = 1.0;
        p[2] = 'c';
    }
}
";
            // Dev11 produces more efficient code, see bug 547265:
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      205 (0xcd)
  .maxstack  8
  .locals init (object V_0) //d
  IL_0000:  ldc.i4.1
  IL_0001:  box        ""int""
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.2
  IL_0008:  box        ""int""
  IL_000d:  starg.s    V_1
  IL_000f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, double, object>> C.<>o__0.<>p__0""
  IL_0014:  brtrue.s   IL_004f
  IL_0016:  ldc.i4.0
  IL_0017:  ldstr      ""f""
  IL_001c:  ldtoken    ""C""
  IL_0021:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0026:  ldc.i4.2
  IL_0027:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_002c:  dup
  IL_002d:  ldc.i4.0
  IL_002e:  ldc.i4.0
  IL_002f:  ldnull
  IL_0030:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0035:  stelem.ref
  IL_0036:  dup
  IL_0037:  ldc.i4.1
  IL_0038:  ldc.i4.3
  IL_0039:  ldnull
  IL_003a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_003f:  stelem.ref
  IL_0040:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0045:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, double, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, double, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_004a:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, double, object>> C.<>o__0.<>p__0""
  IL_004f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, double, object>> C.<>o__0.<>p__0""
  IL_0054:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, double, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, double, object>>.Target""
  IL_0059:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, double, object>> C.<>o__0.<>p__0""
  IL_005e:  ldloc.0
  IL_005f:  ldc.r8     1
  IL_0068:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, double, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, double)""
  IL_006d:  pop
  IL_006e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, char, object>> C.<>o__0.<>p__1""
  IL_0073:  brtrue.s   IL_00b3
  IL_0075:  ldc.i4.0
  IL_0076:  ldtoken    ""C""
  IL_007b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0080:  ldc.i4.3
  IL_0081:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0086:  dup
  IL_0087:  ldc.i4.0
  IL_0088:  ldc.i4.0
  IL_0089:  ldnull
  IL_008a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_008f:  stelem.ref
  IL_0090:  dup
  IL_0091:  ldc.i4.1
  IL_0092:  ldc.i4.3
  IL_0093:  ldnull
  IL_0094:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0099:  stelem.ref
  IL_009a:  dup
  IL_009b:  ldc.i4.2
  IL_009c:  ldc.i4.3
  IL_009d:  ldnull
  IL_009e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00a3:  stelem.ref
  IL_00a4:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00a9:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, char, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, char, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00ae:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, char, object>> C.<>o__0.<>p__1""
  IL_00b3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, char, object>> C.<>o__0.<>p__1""
  IL_00b8:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int, char, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, char, object>>.Target""
  IL_00bd:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, char, object>> C.<>o__0.<>p__1""
  IL_00c2:  ldarg.1
  IL_00c3:  ldc.i4.2
  IL_00c4:  ldc.i4.s   99
  IL_00c6:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, int, char, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, int, char)""
  IL_00cb:  pop
  IL_00cc:  ret
}
");
        }

        [Fact]
        public void CompoundDynamicMemberAssignment()
        {
            string source = @"
public class C
{
    dynamic d, v;

    public dynamic M()
    {
        return d.m *= v;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      259 (0x103)
  .maxstack  13
  .locals init (object V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""dynamic C.d""
  IL_0006:  stloc.0
  IL_0007:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__2""
  IL_000c:  brtrue.s   IL_004b
  IL_000e:  ldc.i4     0x80
  IL_0013:  ldstr      ""m""
  IL_0018:  ldtoken    ""C""
  IL_001d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0022:  ldc.i4.2
  IL_0023:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0028:  dup
  IL_0029:  ldc.i4.0
  IL_002a:  ldc.i4.0
  IL_002b:  ldnull
  IL_002c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0031:  stelem.ref
  IL_0032:  dup
  IL_0033:  ldc.i4.1
  IL_0034:  ldc.i4.0
  IL_0035:  ldnull
  IL_0036:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_003b:  stelem.ref
  IL_003c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0041:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0046:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__2""
  IL_004b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__2""
  IL_0050:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0055:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__2""
  IL_005a:  ldloc.0
  IL_005b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__1""
  IL_0060:  brtrue.s   IL_0098
  IL_0062:  ldc.i4.0
  IL_0063:  ldc.i4.s   69
  IL_0065:  ldtoken    ""C""
  IL_006a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_006f:  ldc.i4.2
  IL_0070:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0075:  dup
  IL_0076:  ldc.i4.0
  IL_0077:  ldc.i4.0
  IL_0078:  ldnull
  IL_0079:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_007e:  stelem.ref
  IL_007f:  dup
  IL_0080:  ldc.i4.1
  IL_0081:  ldc.i4.0
  IL_0082:  ldnull
  IL_0083:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0088:  stelem.ref
  IL_0089:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_008e:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0093:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__1""
  IL_0098:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__1""
  IL_009d:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_00a2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__1""
  IL_00a7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__2.<>p__0""
  IL_00ac:  brtrue.s   IL_00dd
  IL_00ae:  ldc.i4.0
  IL_00af:  ldstr      ""m""
  IL_00b4:  ldtoken    ""C""
  IL_00b9:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00be:  ldc.i4.1
  IL_00bf:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00c4:  dup
  IL_00c5:  ldc.i4.0
  IL_00c6:  ldc.i4.0
  IL_00c7:  ldnull
  IL_00c8:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00cd:  stelem.ref
  IL_00ce:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00d3:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00d8:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__2.<>p__0""
  IL_00dd:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__2.<>p__0""
  IL_00e2:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_00e7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__2.<>p__0""
  IL_00ec:  ldloc.0
  IL_00ed:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_00f2:  ldarg.0
  IL_00f3:  ldfld      ""dynamic C.v""
  IL_00f8:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_00fd:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0102:  ret
}");
        }

        [Fact]
        public void CompoundDynamicMemberAssignment_PossibleAddHandler()
        {
            string source = @"
public class C
{
    dynamic d, v;

    public dynamic M()
    {
        return d.m += v;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      417 (0x1a1)
  .maxstack  13
  .locals init (object V_0,
                object V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""dynamic C.v""
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldfld      ""dynamic C.d""
  IL_000d:  stloc.1
  IL_000e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__2.<>p__3""
  IL_0013:  brtrue.s   IL_0034
  IL_0015:  ldc.i4.0
  IL_0016:  ldstr      ""m""
  IL_001b:  ldtoken    ""C""
  IL_0020:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0025:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_002a:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_002f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__2.<>p__3""
  IL_0034:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__2.<>p__3""
  IL_0039:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Target""
  IL_003e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__2.<>p__3""
  IL_0043:  ldloc.1
  IL_0044:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, object, bool>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0049:  brtrue     IL_0145
  IL_004e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__2""
  IL_0053:  brtrue.s   IL_0092
  IL_0055:  ldc.i4     0x80
  IL_005a:  ldstr      ""m""
  IL_005f:  ldtoken    ""C""
  IL_0064:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0069:  ldc.i4.2
  IL_006a:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_006f:  dup
  IL_0070:  ldc.i4.0
  IL_0071:  ldc.i4.0
  IL_0072:  ldnull
  IL_0073:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0078:  stelem.ref
  IL_0079:  dup
  IL_007a:  ldc.i4.1
  IL_007b:  ldc.i4.0
  IL_007c:  ldnull
  IL_007d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0082:  stelem.ref
  IL_0083:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0088:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_008d:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__2""
  IL_0092:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__2""
  IL_0097:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_009c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__2""
  IL_00a1:  ldloc.1
  IL_00a2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__1""
  IL_00a7:  brtrue.s   IL_00df
  IL_00a9:  ldc.i4.0
  IL_00aa:  ldc.i4.s   63
  IL_00ac:  ldtoken    ""C""
  IL_00b1:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00b6:  ldc.i4.2
  IL_00b7:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00bc:  dup
  IL_00bd:  ldc.i4.0
  IL_00be:  ldc.i4.0
  IL_00bf:  ldnull
  IL_00c0:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00c5:  stelem.ref
  IL_00c6:  dup
  IL_00c7:  ldc.i4.1
  IL_00c8:  ldc.i4.0
  IL_00c9:  ldnull
  IL_00ca:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00cf:  stelem.ref
  IL_00d0:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00d5:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00da:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__1""
  IL_00df:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__1""
  IL_00e4:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_00e9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__1""
  IL_00ee:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__2.<>p__0""
  IL_00f3:  brtrue.s   IL_0124
  IL_00f5:  ldc.i4.0
  IL_00f6:  ldstr      ""m""
  IL_00fb:  ldtoken    ""C""
  IL_0100:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0105:  ldc.i4.1
  IL_0106:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_010b:  dup
  IL_010c:  ldc.i4.0
  IL_010d:  ldc.i4.0
  IL_010e:  ldnull
  IL_010f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0114:  stelem.ref
  IL_0115:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_011a:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_011f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__2.<>p__0""
  IL_0124:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__2.<>p__0""
  IL_0129:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_012e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__2.<>p__0""
  IL_0133:  ldloc.1
  IL_0134:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0139:  ldloc.0
  IL_013a:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_013f:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0144:  ret
  IL_0145:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__4""
  IL_014a:  brtrue.s   IL_018a
  IL_014c:  ldc.i4     0x104
  IL_0151:  ldstr      ""add_m""
  IL_0156:  ldnull
  IL_0157:  ldtoken    ""C""
  IL_015c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0161:  ldc.i4.2
  IL_0162:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0167:  dup
  IL_0168:  ldc.i4.0
  IL_0169:  ldc.i4.0
  IL_016a:  ldnull
  IL_016b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0170:  stelem.ref
  IL_0171:  dup
  IL_0172:  ldc.i4.1
  IL_0173:  ldc.i4.0
  IL_0174:  ldnull
  IL_0175:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_017a:  stelem.ref
  IL_017b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0180:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0185:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__4""
  IL_018a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__4""
  IL_018f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0194:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__4""
  IL_0199:  ldloc.1
  IL_019a:  ldloc.0
  IL_019b:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_01a0:  ret
}
");
        }

        [Fact]
        public void CompoundDynamicMemberAssignment_PossibleRemoveHandlerNull()
        {
            string source = @"
public class C
{
    dynamic d;

    public void M()
    {
        d.m -= null;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      412 (0x19c)
  .maxstack  13
  .locals init (object V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""dynamic C.d""
  IL_0006:  stloc.0
  IL_0007:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__1.<>p__3""
  IL_000c:  brtrue.s   IL_002d
  IL_000e:  ldc.i4.0
  IL_000f:  ldstr      ""m""
  IL_0014:  ldtoken    ""C""
  IL_0019:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0023:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0028:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__1.<>p__3""
  IL_002d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__1.<>p__3""
  IL_0032:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Target""
  IL_0037:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__1.<>p__3""
  IL_003c:  ldloc.0
  IL_003d:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, object, bool>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0042:  brtrue     IL_013f
  IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__1.<>p__2""
  IL_004c:  brtrue.s   IL_008b
  IL_004e:  ldc.i4     0x80
  IL_0053:  ldstr      ""m""
  IL_0058:  ldtoken    ""C""
  IL_005d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0062:  ldc.i4.2
  IL_0063:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0068:  dup
  IL_0069:  ldc.i4.0
  IL_006a:  ldc.i4.0
  IL_006b:  ldnull
  IL_006c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0071:  stelem.ref
  IL_0072:  dup
  IL_0073:  ldc.i4.1
  IL_0074:  ldc.i4.0
  IL_0075:  ldnull
  IL_0076:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_007b:  stelem.ref
  IL_007c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0081:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0086:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__1.<>p__2""
  IL_008b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__1.<>p__2""
  IL_0090:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0095:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__1.<>p__2""
  IL_009a:  ldloc.0
  IL_009b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__1.<>p__1""
  IL_00a0:  brtrue.s   IL_00d8
  IL_00a2:  ldc.i4.0
  IL_00a3:  ldc.i4.s   73
  IL_00a5:  ldtoken    ""C""
  IL_00aa:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00af:  ldc.i4.2
  IL_00b0:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00b5:  dup
  IL_00b6:  ldc.i4.0
  IL_00b7:  ldc.i4.0
  IL_00b8:  ldnull
  IL_00b9:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00be:  stelem.ref
  IL_00bf:  dup
  IL_00c0:  ldc.i4.1
  IL_00c1:  ldc.i4.2
  IL_00c2:  ldnull
  IL_00c3:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00c8:  stelem.ref
  IL_00c9:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00ce:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00d3:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__1.<>p__1""
  IL_00d8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__1.<>p__1""
  IL_00dd:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_00e2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__1.<>p__1""
  IL_00e7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__1.<>p__0""
  IL_00ec:  brtrue.s   IL_011d
  IL_00ee:  ldc.i4.0
  IL_00ef:  ldstr      ""m""
  IL_00f4:  ldtoken    ""C""
  IL_00f9:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00fe:  ldc.i4.1
  IL_00ff:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0104:  dup
  IL_0105:  ldc.i4.0
  IL_0106:  ldc.i4.0
  IL_0107:  ldnull
  IL_0108:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_010d:  stelem.ref
  IL_010e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0113:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0118:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__1.<>p__0""
  IL_011d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__1.<>p__0""
  IL_0122:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_0127:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__1.<>p__0""
  IL_012c:  ldloc.0
  IL_012d:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0132:  ldnull
  IL_0133:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0138:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_013d:  pop
  IL_013e:  ret
  IL_013f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__1.<>p__4""
  IL_0144:  brtrue.s   IL_0184
  IL_0146:  ldc.i4     0x104
  IL_014b:  ldstr      ""remove_m""
  IL_0150:  ldnull
  IL_0151:  ldtoken    ""C""
  IL_0156:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_015b:  ldc.i4.2
  IL_015c:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0161:  dup
  IL_0162:  ldc.i4.0
  IL_0163:  ldc.i4.0
  IL_0164:  ldnull
  IL_0165:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_016a:  stelem.ref
  IL_016b:  dup
  IL_016c:  ldc.i4.1
  IL_016d:  ldc.i4.2
  IL_016e:  ldnull
  IL_016f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0174:  stelem.ref
  IL_0175:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_017a:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_017f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__1.<>p__4""
  IL_0184:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__1.<>p__4""
  IL_0189:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_018e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__1.<>p__4""
  IL_0193:  ldloc.0
  IL_0194:  ldnull
  IL_0195:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_019a:  pop
  IL_019b:  ret
}
");
        }

        [Fact]
        public void CompoundDynamicMemberAssignment_PossibleRemoveHandler()
        {
            string source = @"
public class C
{
    dynamic d, v;

    public dynamic M()
    {
        return d.m -= v;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      417 (0x1a1)
  .maxstack  13
  .locals init (object V_0,
                object V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""dynamic C.v""
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldfld      ""dynamic C.d""
  IL_000d:  stloc.1
  IL_000e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__2.<>p__3""
  IL_0013:  brtrue.s   IL_0034
  IL_0015:  ldc.i4.0
  IL_0016:  ldstr      ""m""
  IL_001b:  ldtoken    ""C""
  IL_0020:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0025:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_002a:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_002f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__2.<>p__3""
  IL_0034:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__2.<>p__3""
  IL_0039:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Target""
  IL_003e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__2.<>p__3""
  IL_0043:  ldloc.1
  IL_0044:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, object, bool>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0049:  brtrue     IL_0145
  IL_004e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__2""
  IL_0053:  brtrue.s   IL_0092
  IL_0055:  ldc.i4     0x80
  IL_005a:  ldstr      ""m""
  IL_005f:  ldtoken    ""C""
  IL_0064:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0069:  ldc.i4.2
  IL_006a:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_006f:  dup
  IL_0070:  ldc.i4.0
  IL_0071:  ldc.i4.0
  IL_0072:  ldnull
  IL_0073:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0078:  stelem.ref
  IL_0079:  dup
  IL_007a:  ldc.i4.1
  IL_007b:  ldc.i4.0
  IL_007c:  ldnull
  IL_007d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0082:  stelem.ref
  IL_0083:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0088:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_008d:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__2""
  IL_0092:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__2""
  IL_0097:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_009c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__2""
  IL_00a1:  ldloc.1
  IL_00a2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__1""
  IL_00a7:  brtrue.s   IL_00df
  IL_00a9:  ldc.i4.0
  IL_00aa:  ldc.i4.s   73
  IL_00ac:  ldtoken    ""C""
  IL_00b1:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00b6:  ldc.i4.2
  IL_00b7:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00bc:  dup
  IL_00bd:  ldc.i4.0
  IL_00be:  ldc.i4.0
  IL_00bf:  ldnull
  IL_00c0:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00c5:  stelem.ref
  IL_00c6:  dup
  IL_00c7:  ldc.i4.1
  IL_00c8:  ldc.i4.0
  IL_00c9:  ldnull
  IL_00ca:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00cf:  stelem.ref
  IL_00d0:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00d5:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00da:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__1""
  IL_00df:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__1""
  IL_00e4:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_00e9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__1""
  IL_00ee:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__2.<>p__0""
  IL_00f3:  brtrue.s   IL_0124
  IL_00f5:  ldc.i4.0
  IL_00f6:  ldstr      ""m""
  IL_00fb:  ldtoken    ""C""
  IL_0100:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0105:  ldc.i4.1
  IL_0106:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_010b:  dup
  IL_010c:  ldc.i4.0
  IL_010d:  ldc.i4.0
  IL_010e:  ldnull
  IL_010f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0114:  stelem.ref
  IL_0115:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_011a:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_011f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__2.<>p__0""
  IL_0124:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__2.<>p__0""
  IL_0129:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_012e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__2.<>p__0""
  IL_0133:  ldloc.1
  IL_0134:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0139:  ldloc.0
  IL_013a:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_013f:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0144:  ret
  IL_0145:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__4""
  IL_014a:  brtrue.s   IL_018a
  IL_014c:  ldc.i4     0x104
  IL_0151:  ldstr      ""remove_m""
  IL_0156:  ldnull
  IL_0157:  ldtoken    ""C""
  IL_015c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0161:  ldc.i4.2
  IL_0162:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0167:  dup
  IL_0168:  ldc.i4.0
  IL_0169:  ldc.i4.0
  IL_016a:  ldnull
  IL_016b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0170:  stelem.ref
  IL_0171:  dup
  IL_0172:  ldc.i4.1
  IL_0173:  ldc.i4.0
  IL_0174:  ldnull
  IL_0175:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_017a:  stelem.ref
  IL_017b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0180:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0185:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__4""
  IL_018a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__4""
  IL_018f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0194:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__2.<>p__4""
  IL_0199:  ldloc.1
  IL_019a:  ldloc.0
  IL_019b:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_01a0:  ret
}
");
        }

        [Fact]
        public void CompoundStaticFieldAssignment()
        {
            string source = @"
public class C
{
    public dynamic Field;
    C c;
    dynamic v;

    public dynamic M()
    {
        return c.Field *= v;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      110 (0x6e)
  .maxstack  9
  .locals init (C V_0,
                object V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.c""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__3.<>p__0""
  IL_000d:  brtrue.s   IL_0045
  IL_000f:  ldc.i4.0
  IL_0010:  ldc.i4.s   69
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.0
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  dup
  IL_002d:  ldc.i4.1
  IL_002e:  ldc.i4.0
  IL_002f:  ldnull
  IL_0030:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0035:  stelem.ref
  IL_0036:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0040:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__3.<>p__0""
  IL_0045:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__3.<>p__0""
  IL_004a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_004f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__3.<>p__0""
  IL_0054:  ldloc.0
  IL_0055:  ldfld      ""dynamic C.Field""
  IL_005a:  ldarg.0
  IL_005b:  ldfld      ""dynamic C.v""
  IL_0060:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0065:  dup
  IL_0066:  stloc.1
  IL_0067:  stfld      ""dynamic C.Field""
  IL_006c:  ldloc.1
  IL_006d:  ret
}");
        }

        [Fact]
        public void CompoundStaticPropertyAssignment()
        {
            string source = @"
public class C
{
    public dynamic Property { get; set; }
    C c;
    dynamic v;

    public dynamic M()
    {
        return c.Property *= v;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      110 (0x6e)
  .maxstack  9
  .locals init (C V_0,
                object V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.c""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__6.<>p__0""
  IL_000d:  brtrue.s   IL_0045
  IL_000f:  ldc.i4.0
  IL_0010:  ldc.i4.s   69
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.0
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  dup
  IL_002d:  ldc.i4.1
  IL_002e:  ldc.i4.0
  IL_002f:  ldnull
  IL_0030:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0035:  stelem.ref
  IL_0036:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0040:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__6.<>p__0""
  IL_0045:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__6.<>p__0""
  IL_004a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_004f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__6.<>p__0""
  IL_0054:  ldloc.0
  IL_0055:  callvirt   ""dynamic C.Property.get""
  IL_005a:  ldarg.0
  IL_005b:  ldfld      ""dynamic C.v""
  IL_0060:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0065:  dup
  IL_0066:  stloc.1
  IL_0067:  callvirt   ""void C.Property.set""
  IL_006c:  ldloc.1
  IL_006d:  ret
}
");
        }

        [Fact]
        public void CompoundDynamicIndexerAssignment()
        {
            string source = @"
public class C
{
    public int f() { return 1; }

    public dynamic M(dynamic d, dynamic i, dynamic v)
    {
        return d[i, f()] *= v;
    }
}";
            CompileAndVerifyIL(source, "C.M",
@"
{
  // Code size      292 (0x124)
  .maxstack  14
  .locals init (object V_0,
                object V_1,
                int V_2)
  IL_0000:  ldarg.1
  IL_0001:  stloc.0
  IL_0002:  ldarg.2
  IL_0003:  stloc.1
  IL_0004:  ldarg.0
  IL_0005:  call       ""int C.f()""
  IL_000a:  stloc.2
  IL_000b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object, object>> C.<>o__1.<>p__2""
  IL_0010:  brtrue.s   IL_005e
  IL_0012:  ldc.i4     0x80
  IL_0017:  ldtoken    ""C""
  IL_001c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0021:  ldc.i4.4
  IL_0022:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0027:  dup
  IL_0028:  ldc.i4.0
  IL_0029:  ldc.i4.0
  IL_002a:  ldnull
  IL_002b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0030:  stelem.ref
  IL_0031:  dup
  IL_0032:  ldc.i4.1
  IL_0033:  ldc.i4.0
  IL_0034:  ldnull
  IL_0035:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_003a:  stelem.ref
  IL_003b:  dup
  IL_003c:  ldc.i4.2
  IL_003d:  ldc.i4.1
  IL_003e:  ldnull
  IL_003f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0044:  stelem.ref
  IL_0045:  dup
  IL_0046:  ldc.i4.3
  IL_0047:  ldc.i4.0
  IL_0048:  ldnull
  IL_0049:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_004e:  stelem.ref
  IL_004f:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0054:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0059:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object, object>> C.<>o__1.<>p__2""
  IL_005e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object, object>> C.<>o__1.<>p__2""
  IL_0063:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object, object>>.Target""
  IL_0068:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object, object>> C.<>o__1.<>p__2""
  IL_006d:  ldloc.0
  IL_006e:  ldloc.1
  IL_006f:  ldloc.2
  IL_0070:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__1.<>p__1""
  IL_0075:  brtrue.s   IL_00ad
  IL_0077:  ldc.i4.0
  IL_0078:  ldc.i4.s   69
  IL_007a:  ldtoken    ""C""
  IL_007f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0084:  ldc.i4.2
  IL_0085:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_008a:  dup
  IL_008b:  ldc.i4.0
  IL_008c:  ldc.i4.0
  IL_008d:  ldnull
  IL_008e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0093:  stelem.ref
  IL_0094:  dup
  IL_0095:  ldc.i4.1
  IL_0096:  ldc.i4.0
  IL_0097:  ldnull
  IL_0098:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_009d:  stelem.ref
  IL_009e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00a3:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00a8:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__1.<>p__1""
  IL_00ad:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__1.<>p__1""
  IL_00b2:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_00b7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__1.<>p__1""
  IL_00bc:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object>> C.<>o__1.<>p__0""
  IL_00c1:  brtrue.s   IL_0101
  IL_00c3:  ldc.i4.0
  IL_00c4:  ldtoken    ""C""
  IL_00c9:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00ce:  ldc.i4.3
  IL_00cf:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00d4:  dup
  IL_00d5:  ldc.i4.0
  IL_00d6:  ldc.i4.0
  IL_00d7:  ldnull
  IL_00d8:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00dd:  stelem.ref
  IL_00de:  dup
  IL_00df:  ldc.i4.1
  IL_00e0:  ldc.i4.0
  IL_00e1:  ldnull
  IL_00e2:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00e7:  stelem.ref
  IL_00e8:  dup
  IL_00e9:  ldc.i4.2
  IL_00ea:  ldc.i4.1
  IL_00eb:  ldnull
  IL_00ec:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00f1:  stelem.ref
  IL_00f2:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00f7:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00fc:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object>> C.<>o__1.<>p__0""
  IL_0101:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object>> C.<>o__1.<>p__0""
  IL_0106:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object>>.Target""
  IL_010b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object>> C.<>o__1.<>p__0""
  IL_0110:  ldloc.0
  IL_0111:  ldloc.1
  IL_0112:  ldloc.2
  IL_0113:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object, int)""
  IL_0118:  ldarg.3
  IL_0119:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_011e:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object, int, object)""
  IL_0123:  ret
}
");
#if TODO // locals and parameters shouldn't be spilled
@"{
  // Code size      288 (0x120)
  .maxstack  14
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""int C.f()""
  IL_0006:  stloc.0
  IL_0007:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object, object>> C.<>o__0.<>p__2""
  IL_000c:  brtrue.s   IL_005a
  IL_000e:  ldc.i4     0x80
  IL_0013:  ldtoken    ""C""
  IL_0018:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001d:  ldc.i4.4
  IL_001e:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0023:  dup
  IL_0024:  ldc.i4.0
  IL_0025:  ldc.i4.0
  IL_0026:  ldnull
  IL_0027:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002c:  stelem.ref
  IL_002d:  dup
  IL_002e:  ldc.i4.1
  IL_002f:  ldc.i4.0
  IL_0030:  ldnull
  IL_0031:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0036:  stelem.ref
  IL_0037:  dup
  IL_0038:  ldc.i4.2
  IL_0039:  ldc.i4.1
  IL_003a:  ldnull
  IL_003b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0040:  stelem.ref
  IL_0041:  dup
  IL_0042:  ldc.i4.3
  IL_0043:  ldc.i4.0
  IL_0044:  ldnull
  IL_0045:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_004a:  stelem.ref
  IL_004b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0050:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0055:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object, object>> C.<>o__0.<>p__2""
  IL_005a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object, object>> C.<>o__0.<>p__2""
  IL_005f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object, object>>.Target""
  IL_0064:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object, object>> C.<>o__0.<>p__2""
  IL_0069:  ldarg.1
  IL_006a:  ldarg.2
  IL_006b:  ldloc.0
  IL_006c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__1""
  IL_0071:  brtrue.s   IL_00a9
  IL_0073:  ldc.i4.0
  IL_0074:  ldc.i4.s   69
  IL_0076:  ldtoken    ""C""
  IL_007b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0080:  ldc.i4.2
  IL_0081:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0086:  dup
  IL_0087:  ldc.i4.0
  IL_0088:  ldc.i4.0
  IL_0089:  ldnull
  IL_008a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_008f:  stelem.ref
  IL_0090:  dup
  IL_0091:  ldc.i4.1
  IL_0092:  ldc.i4.0
  IL_0093:  ldnull
  IL_0094:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0099:  stelem.ref
  IL_009a:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_009f:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00a4:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__1""
  IL_00a9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__1""
  IL_00ae:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_00b3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__1""
  IL_00b8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object>> C.<>o__0.<>p__0""
  IL_00bd:  brtrue.s   IL_00fd
  IL_00bf:  ldc.i4.0
  IL_00c0:  ldtoken    ""C""
  IL_00c5:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00ca:  ldc.i4.3
  IL_00cb:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00d0:  dup
  IL_00d1:  ldc.i4.0
  IL_00d2:  ldc.i4.0
  IL_00d3:  ldnull
  IL_00d4:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00d9:  stelem.ref
  IL_00da:  dup
  IL_00db:  ldc.i4.1
  IL_00dc:  ldc.i4.0
  IL_00dd:  ldnull
  IL_00de:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00e3:  stelem.ref
  IL_00e4:  dup
  IL_00e5:  ldc.i4.2
  IL_00e6:  ldc.i4.1
  IL_00e7:  ldnull
  IL_00e8:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00ed:  stelem.ref
  IL_00ee:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00f3:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00f8:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object>> C.<>o__0.<>p__0""
  IL_00fd:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object>> C.<>o__0.<>p__0""
  IL_0102:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object>>.Target""
  IL_0107:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object>> C.<>o__0.<>p__0""
  IL_010c:  ldarg.1
  IL_010d:  ldarg.2
  IL_010e:  ldloc.0
  IL_010f:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object, int)""
  IL_0114:  ldarg.3
  IL_0115:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_011a:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, int, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object, int, object)""
  IL_011f:  ret
}
");
#endif
        }

        [Fact]
        public void CompoundStaticIndexerAssignment()
        {
            string source = @"
public class C
{
    public dynamic this[int a, object o] { get { return null; } set { } }

    public int f() { return 1; }

    public dynamic M(C c, dynamic v)
    {
        return c[f(), null] *= v;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      111 (0x6f)
  .maxstack  11
  .locals init (C V_0,
                int V_1,
                object V_2)
  IL_0000:  ldarg.1
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  call       ""int C.f()""
  IL_0008:  stloc.1
  IL_0009:  ldloc.0
  IL_000a:  ldloc.1
  IL_000b:  ldnull
  IL_000c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__4.<>p__0""
  IL_0011:  brtrue.s   IL_0049
  IL_0013:  ldc.i4.0
  IL_0014:  ldc.i4.s   69
  IL_0016:  ldtoken    ""C""
  IL_001b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0020:  ldc.i4.2
  IL_0021:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0026:  dup
  IL_0027:  ldc.i4.0
  IL_0028:  ldc.i4.0
  IL_0029:  ldnull
  IL_002a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002f:  stelem.ref
  IL_0030:  dup
  IL_0031:  ldc.i4.1
  IL_0032:  ldc.i4.0
  IL_0033:  ldnull
  IL_0034:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0039:  stelem.ref
  IL_003a:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003f:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0044:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__4.<>p__0""
  IL_0049:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__4.<>p__0""
  IL_004e:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0053:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__4.<>p__0""
  IL_0058:  ldloc.0
  IL_0059:  ldloc.1
  IL_005a:  ldnull
  IL_005b:  callvirt   ""dynamic C.this[int, object].get""
  IL_0060:  ldarg.2
  IL_0061:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0066:  dup
  IL_0067:  stloc.2
  IL_0068:  callvirt   ""void C.this[int, object].set""
  IL_006d:  ldloc.2
  IL_006e:  ret
}");
#if TODO // locals and parameters shouldn't be spilled       
@"
{
  // Code size      109 (0x6d)
  .maxstack  11
  .locals init (int V_0,
  object V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""int C.f()""
  IL_0006:  stloc.0
  IL_0007:  ldarg.1
  IL_0008:  ldloc.0
  IL_0009:  ldnull
  IL_000a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_000f:  brtrue.s   IL_0047
  IL_0011:  ldc.i4.0
  IL_0012:  ldc.i4.s   69
  IL_0014:  ldtoken    ""C""
  IL_0019:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001e:  ldc.i4.2
  IL_001f:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0024:  dup
  IL_0025:  ldc.i4.0
  IL_0026:  ldc.i4.0
  IL_0027:  ldnull
  IL_0028:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002d:  stelem.ref
  IL_002e:  dup
  IL_002f:  ldc.i4.1
  IL_0030:  ldc.i4.0
  IL_0031:  ldnull
  IL_0032:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0037:  stelem.ref
  IL_0038:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003d:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0042:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_004c:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0051:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_0056:  ldarg.1
  IL_0057:  ldloc.0
  IL_0058:  ldnull
  IL_0059:  callvirt   ""dynamic C.this[int, object].get""
  IL_005e:  ldarg.2
  IL_005f:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0064:  dup
  IL_0065:  stloc.1
  IL_0066:  callvirt   ""void C.this[int, object].set""
  IL_006b:  ldloc.1
  IL_006c:  ret
}
");
#endif
        }

        [Fact]
        public void CompoundDynamicIndexerAssignment_ByRef()
        {
            string source = @"
public class C
{
    public dynamic Field;
    public C c;

    public C f()
    {
        return this;
    }

    public dynamic M(dynamic d, dynamic v)
    {
        int[] b = null;
        return d[ref f().Field, out b[10], c.c] *= v;
    }
}";
            // Dev11 emits different (unverifiable) code

            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      342 (0x156)
  .maxstack  15
  .locals init (object V_0,
                object& V_1,
                int& V_2,
                C V_3)
  IL_0000:  ldnull
  IL_0001:  ldarg.1
  IL_0002:  stloc.0
  IL_0003:  ldarg.0
  IL_0004:  call       ""C C.f()""
  IL_0009:  ldflda     ""dynamic C.Field""
  IL_000e:  stloc.1
  IL_000f:  ldc.i4.s   10
  IL_0011:  ldelema    ""int""
  IL_0016:  stloc.2
  IL_0017:  ldarg.0
  IL_0018:  ldfld      ""C C.c""
  IL_001d:  ldfld      ""C C.c""
  IL_0022:  stloc.3
  IL_0023:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F{0000000c}<System.Runtime.CompilerServices.CallSite, object, object, int, C, object, object>> C.<>o__3.<>p__2""
  IL_0028:  brtrue.s   IL_0082
  IL_002a:  ldc.i4     0x80
  IL_002f:  ldtoken    ""C""
  IL_0034:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0039:  ldc.i4.5
  IL_003a:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_003f:  dup
  IL_0040:  ldc.i4.0
  IL_0041:  ldc.i4.0
  IL_0042:  ldnull
  IL_0043:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0048:  stelem.ref
  IL_0049:  dup
  IL_004a:  ldc.i4.1
  IL_004b:  ldc.i4.s   9
  IL_004d:  ldnull
  IL_004e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0053:  stelem.ref
  IL_0054:  dup
  IL_0055:  ldc.i4.2
  IL_0056:  ldc.i4.s   17
  IL_0058:  ldnull
  IL_0059:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_005e:  stelem.ref
  IL_005f:  dup
  IL_0060:  ldc.i4.3
  IL_0061:  ldc.i4.1
  IL_0062:  ldnull
  IL_0063:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0068:  stelem.ref
  IL_0069:  dup
  IL_006a:  ldc.i4.4
  IL_006b:  ldc.i4.0
  IL_006c:  ldnull
  IL_006d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0072:  stelem.ref
  IL_0073:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0078:  call       ""System.Runtime.CompilerServices.CallSite<<>F{0000000c}<System.Runtime.CompilerServices.CallSite, object, object, int, C, object, object>> System.Runtime.CompilerServices.CallSite<<>F{0000000c}<System.Runtime.CompilerServices.CallSite, object, object, int, C, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_007d:  stsfld     ""System.Runtime.CompilerServices.CallSite<<>F{0000000c}<System.Runtime.CompilerServices.CallSite, object, object, int, C, object, object>> C.<>o__3.<>p__2""
  IL_0082:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F{0000000c}<System.Runtime.CompilerServices.CallSite, object, object, int, C, object, object>> C.<>o__3.<>p__2""
  IL_0087:  ldfld      ""<>F{0000000c}<System.Runtime.CompilerServices.CallSite, object, object, int, C, object, object> System.Runtime.CompilerServices.CallSite<<>F{0000000c}<System.Runtime.CompilerServices.CallSite, object, object, int, C, object, object>>.Target""
  IL_008c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F{0000000c}<System.Runtime.CompilerServices.CallSite, object, object, int, C, object, object>> C.<>o__3.<>p__2""
  IL_0091:  ldloc.0
  IL_0092:  ldloc.1
  IL_0093:  ldloc.2
  IL_0094:  ldloc.3
  IL_0095:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__3.<>p__1""
  IL_009a:  brtrue.s   IL_00d2
  IL_009c:  ldc.i4.0
  IL_009d:  ldc.i4.s   69
  IL_009f:  ldtoken    ""C""
  IL_00a4:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00a9:  ldc.i4.2
  IL_00aa:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00af:  dup
  IL_00b0:  ldc.i4.0
  IL_00b1:  ldc.i4.0
  IL_00b2:  ldnull
  IL_00b3:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00b8:  stelem.ref
  IL_00b9:  dup
  IL_00ba:  ldc.i4.1
  IL_00bb:  ldc.i4.0
  IL_00bc:  ldnull
  IL_00bd:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00c2:  stelem.ref
  IL_00c3:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00c8:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00cd:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__3.<>p__1""
  IL_00d2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__3.<>p__1""
  IL_00d7:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_00dc:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__3.<>p__1""
  IL_00e1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F{0000000c}<System.Runtime.CompilerServices.CallSite, object, object, int, C, object>> C.<>o__3.<>p__0""
  IL_00e6:  brtrue.s   IL_0132
  IL_00e8:  ldc.i4.0
  IL_00e9:  ldtoken    ""C""
  IL_00ee:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00f3:  ldc.i4.4
  IL_00f4:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00f9:  dup
  IL_00fa:  ldc.i4.0
  IL_00fb:  ldc.i4.0
  IL_00fc:  ldnull
  IL_00fd:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0102:  stelem.ref
  IL_0103:  dup
  IL_0104:  ldc.i4.1
  IL_0105:  ldc.i4.s   9
  IL_0107:  ldnull
  IL_0108:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_010d:  stelem.ref
  IL_010e:  dup
  IL_010f:  ldc.i4.2
  IL_0110:  ldc.i4.s   17
  IL_0112:  ldnull
  IL_0113:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0118:  stelem.ref
  IL_0119:  dup
  IL_011a:  ldc.i4.3
  IL_011b:  ldc.i4.1
  IL_011c:  ldnull
  IL_011d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0122:  stelem.ref
  IL_0123:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0128:  call       ""System.Runtime.CompilerServices.CallSite<<>F{0000000c}<System.Runtime.CompilerServices.CallSite, object, object, int, C, object>> System.Runtime.CompilerServices.CallSite<<>F{0000000c}<System.Runtime.CompilerServices.CallSite, object, object, int, C, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_012d:  stsfld     ""System.Runtime.CompilerServices.CallSite<<>F{0000000c}<System.Runtime.CompilerServices.CallSite, object, object, int, C, object>> C.<>o__3.<>p__0""
  IL_0132:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F{0000000c}<System.Runtime.CompilerServices.CallSite, object, object, int, C, object>> C.<>o__3.<>p__0""
  IL_0137:  ldfld      ""<>F{0000000c}<System.Runtime.CompilerServices.CallSite, object, object, int, C, object> System.Runtime.CompilerServices.CallSite<<>F{0000000c}<System.Runtime.CompilerServices.CallSite, object, object, int, C, object>>.Target""
  IL_013c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>F{0000000c}<System.Runtime.CompilerServices.CallSite, object, object, int, C, object>> C.<>o__3.<>p__0""
  IL_0141:  ldloc.0
  IL_0142:  ldloc.1
  IL_0143:  ldloc.2
  IL_0144:  ldloc.3
  IL_0145:  callvirt   ""object <>F{0000000c}<System.Runtime.CompilerServices.CallSite, object, object, int, C, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, ref object, ref int, C)""
  IL_014a:  ldarg.2
  IL_014b:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_0150:  callvirt   ""object <>F{0000000c}<System.Runtime.CompilerServices.CallSite, object, object, int, C, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, ref object, ref int, C, object)""
  IL_0155:  ret
}");
        }

        [Fact]
        public void CompoundDynamicArrayElementAccess()
        {
            string source = @"
public class C
{
    static dynamic[] d;
    static string s;
        
    static void M()
    {
        d[0] += s;
    }
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       99 (0x63)
  .maxstack  10
  .locals init (object[] V_0)
  IL_0000:  ldsfld     ""dynamic[] C.d""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.0
  IL_0008:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, string, object>> C.<>o__2.<>p__0""
  IL_000d:  brtrue.s   IL_0045
  IL_000f:  ldc.i4.0
  IL_0010:  ldc.i4.s   63
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.0
  IL_0025:  ldnull
  IL_0026:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002b:  stelem.ref
  IL_002c:  dup
  IL_002d:  ldc.i4.1
  IL_002e:  ldc.i4.1
  IL_002f:  ldnull
  IL_0030:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0035:  stelem.ref
  IL_0036:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, string, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, string, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0040:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, string, object>> C.<>o__2.<>p__0""
  IL_0045:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, string, object>> C.<>o__2.<>p__0""
  IL_004a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, string, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, string, object>>.Target""
  IL_004f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, string, object>> C.<>o__2.<>p__0""
  IL_0054:  ldloc.0
  IL_0055:  ldc.i4.0
  IL_0056:  ldelem.ref
  IL_0057:  ldsfld     ""string C.s""
  IL_005c:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, string, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, string)""
  IL_0061:  stelem.ref
  IL_0062:  ret
}
");
        }

        [Fact]
        public void CompoundAssignment_WithDynamicConversion_ToStruct()
        {
            string source = @"
class C
{
    dynamic d = null;

    public void M()
    {
        bool ret = true;		
        ret &= (1 == d);		
    }
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      238 (0xee)
  .maxstack  13
  .locals init (bool V_0) //ret
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__1.<>p__2""
  IL_0007:  brtrue.s   IL_002e
  IL_0009:  ldc.i4.s   16
  IL_000b:  ldtoken    ""bool""
  IL_0010:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0015:  ldtoken    ""C""
  IL_001a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001f:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0024:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0029:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__1.<>p__2""
  IL_002e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__1.<>p__2""
  IL_0033:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Target""
  IL_0038:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__1.<>p__2""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> C.<>o__1.<>p__1""
  IL_0042:  brtrue.s   IL_007a
  IL_0044:  ldc.i4.0
  IL_0045:  ldc.i4.s   64
  IL_0047:  ldtoken    ""C""
  IL_004c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0051:  ldc.i4.2
  IL_0052:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0057:  dup
  IL_0058:  ldc.i4.0
  IL_0059:  ldc.i4.1
  IL_005a:  ldnull
  IL_005b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0060:  stelem.ref
  IL_0061:  dup
  IL_0062:  ldc.i4.1
  IL_0063:  ldc.i4.0
  IL_0064:  ldnull
  IL_0065:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_006a:  stelem.ref
  IL_006b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0070:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0075:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> C.<>o__1.<>p__1""
  IL_007a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> C.<>o__1.<>p__1""
  IL_007f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>>.Target""
  IL_0084:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> C.<>o__1.<>p__1""
  IL_0089:  ldloc.0
  IL_008a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, int, object, object>> C.<>o__1.<>p__0""
  IL_008f:  brtrue.s   IL_00c7
  IL_0091:  ldc.i4.0
  IL_0092:  ldc.i4.s   13
  IL_0094:  ldtoken    ""C""
  IL_0099:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_009e:  ldc.i4.2
  IL_009f:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00a4:  dup
  IL_00a5:  ldc.i4.0
  IL_00a6:  ldc.i4.3
  IL_00a7:  ldnull
  IL_00a8:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00ad:  stelem.ref
  IL_00ae:  dup
  IL_00af:  ldc.i4.1
  IL_00b0:  ldc.i4.0
  IL_00b1:  ldnull
  IL_00b2:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00b7:  stelem.ref
  IL_00b8:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00bd:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, int, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, int, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00c2:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, int, object, object>> C.<>o__1.<>p__0""
  IL_00c7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, int, object, object>> C.<>o__1.<>p__0""
  IL_00cc:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, int, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, int, object, object>>.Target""
  IL_00d1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, int, object, object>> C.<>o__1.<>p__0""
  IL_00d6:  ldc.i4.1
  IL_00d7:  ldarg.0
  IL_00d8:  ldfld      ""dynamic C.d""
  IL_00dd:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, int, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, int, object)""
  IL_00e2:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, bool, object)""
  IL_00e7:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, object, bool>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_00ec:  stloc.0
  IL_00ed:  ret
}
");
        }

        [Fact]
        public void CompoundAssignment_WithDynamicConversion_ToClass()
        {
            string source = @"
class C
{
    dynamic d = null;

    public void M()
    {
        C ret = null;		
        ret &= (1 == d);		
    }
}
";
            CompileAndVerifyIL(source, "C.M", @"      
{
  // Code size      238 (0xee)
  .maxstack  13
  .locals init (C V_0) //ret
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, C>> C.<>o__1.<>p__2""
  IL_0007:  brtrue.s   IL_002e
  IL_0009:  ldc.i4.s   16
  IL_000b:  ldtoken    ""C""
  IL_0010:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0015:  ldtoken    ""C""
  IL_001a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001f:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0024:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, C>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, C>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0029:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, C>> C.<>o__1.<>p__2""
  IL_002e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, C>> C.<>o__1.<>p__2""
  IL_0033:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, C> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, C>>.Target""
  IL_0038:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, C>> C.<>o__1.<>p__2""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, object>> C.<>o__1.<>p__1""
  IL_0042:  brtrue.s   IL_007a
  IL_0044:  ldc.i4.0
  IL_0045:  ldc.i4.s   64
  IL_0047:  ldtoken    ""C""
  IL_004c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0051:  ldc.i4.2
  IL_0052:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0057:  dup
  IL_0058:  ldc.i4.0
  IL_0059:  ldc.i4.1
  IL_005a:  ldnull
  IL_005b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0060:  stelem.ref
  IL_0061:  dup
  IL_0062:  ldc.i4.1
  IL_0063:  ldc.i4.0
  IL_0064:  ldnull
  IL_0065:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_006a:  stelem.ref
  IL_006b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0070:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0075:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, object>> C.<>o__1.<>p__1""
  IL_007a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, object>> C.<>o__1.<>p__1""
  IL_007f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, C, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, object>>.Target""
  IL_0084:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, C, object, object>> C.<>o__1.<>p__1""
  IL_0089:  ldloc.0
  IL_008a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, int, object, object>> C.<>o__1.<>p__0""
  IL_008f:  brtrue.s   IL_00c7
  IL_0091:  ldc.i4.0
  IL_0092:  ldc.i4.s   13
  IL_0094:  ldtoken    ""C""
  IL_0099:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_009e:  ldc.i4.2
  IL_009f:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00a4:  dup
  IL_00a5:  ldc.i4.0
  IL_00a6:  ldc.i4.3
  IL_00a7:  ldnull
  IL_00a8:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00ad:  stelem.ref
  IL_00ae:  dup
  IL_00af:  ldc.i4.1
  IL_00b0:  ldc.i4.0
  IL_00b1:  ldnull
  IL_00b2:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00b7:  stelem.ref
  IL_00b8:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00bd:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, int, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, int, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00c2:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, int, object, object>> C.<>o__1.<>p__0""
  IL_00c7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, int, object, object>> C.<>o__1.<>p__0""
  IL_00cc:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, int, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, int, object, object>>.Target""
  IL_00d1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, int, object, object>> C.<>o__1.<>p__0""
  IL_00d6:  ldc.i4.1
  IL_00d7:  ldarg.0
  IL_00d8:  ldfld      ""dynamic C.d""
  IL_00dd:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, int, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, int, object)""
  IL_00e2:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, C, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, C, object)""
  IL_00e7:  callvirt   ""C System.Func<System.Runtime.CompilerServices.CallSite, object, C>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_00ec:  stloc.0
  IL_00ed:  ret
}
");
        }

        [Fact]
        public void CompoundAssignment_WithDynamicConversion_ToPointer()
        {
            string source = @"
class C
{
    dynamic d = null;

    public unsafe void M()
    {
        int* ret = null;
        ret &= (1 == d);		
    }
}
";
            CreateCompilationWithMscorlib(source, new[] { CSharpRef, SystemCoreRef }, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (9,3): error CS0019: Operator '&=' cannot be applied to operands of type 'int*' and 'dynamic'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "ret &= (1 == d)").WithArguments("&=", "int*", "dynamic"));
        }

        [Fact]
        public void CompoundAssignment_WithNoDynamicConversion_Object()
        {
            string source = @"
class C
{
    dynamic d = null;

    public void M()
    {
        object ret = null;		
        ret &= (1 == d);		
    }
}
";
            CompileAndVerifyIL(source, "C.M", @" 
{
  // Code size      174 (0xae)
  .maxstack  11
  .locals init (object V_0) //ret
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__1.<>p__1""
  IL_0007:  brtrue.s   IL_003f
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.s   64
  IL_000c:  ldtoken    ""C""
  IL_0011:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0016:  ldc.i4.2
  IL_0017:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001c:  dup
  IL_001d:  ldc.i4.0
  IL_001e:  ldc.i4.1
  IL_001f:  ldnull
  IL_0020:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0025:  stelem.ref
  IL_0026:  dup
  IL_0027:  ldc.i4.1
  IL_0028:  ldc.i4.0
  IL_0029:  ldnull
  IL_002a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002f:  stelem.ref
  IL_0030:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0035:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_003a:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__1.<>p__1""
  IL_003f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__1.<>p__1""
  IL_0044:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_0049:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__1.<>p__1""
  IL_004e:  ldloc.0
  IL_004f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, int, object, object>> C.<>o__1.<>p__0""
  IL_0054:  brtrue.s   IL_008c
  IL_0056:  ldc.i4.0
  IL_0057:  ldc.i4.s   13
  IL_0059:  ldtoken    ""C""
  IL_005e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0063:  ldc.i4.2
  IL_0064:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0069:  dup
  IL_006a:  ldc.i4.0
  IL_006b:  ldc.i4.3
  IL_006c:  ldnull
  IL_006d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0072:  stelem.ref
  IL_0073:  dup
  IL_0074:  ldc.i4.1
  IL_0075:  ldc.i4.0
  IL_0076:  ldnull
  IL_0077:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_007c:  stelem.ref
  IL_007d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0082:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, int, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, int, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0087:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, int, object, object>> C.<>o__1.<>p__0""
  IL_008c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, int, object, object>> C.<>o__1.<>p__0""
  IL_0091:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, int, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, int, object, object>>.Target""
  IL_0096:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, int, object, object>> C.<>o__1.<>p__0""
  IL_009b:  ldc.i4.1
  IL_009c:  ldarg.0
  IL_009d:  ldfld      ""dynamic C.d""
  IL_00a2:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, int, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, int, object)""
  IL_00a7:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_00ac:  stloc.0
  IL_00ad:  ret
}
");
        }

        [Fact]
        public void CompoundAssignment_UserDefinedOperator()
        {
            string source = @"
class C 
{
    public static dynamic operator +(C lhs, int rhs)
    {
        return null;
    }

    static void M()
    {
        C c = new C();
        c += 2;
    }
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       78 (0x4e)
  .maxstack  4
  .locals init (C V_0) //c
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, C>> C.<>o__1.<>p__0""
  IL_000b:  brtrue.s   IL_0031
  IL_000d:  ldc.i4.0
  IL_000e:  ldtoken    ""C""
  IL_0013:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0018:  ldtoken    ""C""
  IL_001d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0022:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0027:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, C>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, C>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_002c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, C>> C.<>o__1.<>p__0""
  IL_0031:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, C>> C.<>o__1.<>p__0""
  IL_0036:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, C> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, C>>.Target""
  IL_003b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, C>> C.<>o__1.<>p__0""
  IL_0040:  ldloc.0
  IL_0041:  ldc.i4.2
  IL_0042:  call       ""dynamic C.op_Addition(C, int)""
  IL_0047:  callvirt   ""C System.Func<System.Runtime.CompilerServices.CallSite, object, C>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_004c:  stloc.0
  IL_004d:  ret
}
");
        }

        [Fact]
        public void CompoundAssignment_Result()
        {
            string source = @"
class C
{
    bool a = true;
    bool b = true;
    dynamic d = null;
    
    int M()
    {
        if ((a &= d) != b)
        {
            return 1;
        }

        return 2;
    }
}
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      178 (0xb2)
  .maxstack  11
  .locals init (bool V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__3.<>p__1""
  IL_0006:  brtrue.s   IL_002d
  IL_0008:  ldc.i4.s   16
  IL_000a:  ldtoken    ""bool""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldtoken    ""C""
  IL_0019:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0023:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0028:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__3.<>p__1""
  IL_002d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__3.<>p__1""
  IL_0032:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Target""
  IL_0037:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__3.<>p__1""
  IL_003c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> C.<>o__3.<>p__0""
  IL_0041:  brtrue.s   IL_0079
  IL_0043:  ldc.i4.0
  IL_0044:  ldc.i4.s   64
  IL_0046:  ldtoken    ""C""
  IL_004b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0050:  ldc.i4.2
  IL_0051:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0056:  dup
  IL_0057:  ldc.i4.0
  IL_0058:  ldc.i4.1
  IL_0059:  ldnull
  IL_005a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_005f:  stelem.ref
  IL_0060:  dup
  IL_0061:  ldc.i4.1
  IL_0062:  ldc.i4.0
  IL_0063:  ldnull
  IL_0064:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0069:  stelem.ref
  IL_006a:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_006f:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0074:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> C.<>o__3.<>p__0""
  IL_0079:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> C.<>o__3.<>p__0""
  IL_007e:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>>.Target""
  IL_0083:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>> C.<>o__3.<>p__0""
  IL_0088:  ldarg.0
  IL_0089:  ldfld      ""bool C.a""
  IL_008e:  ldarg.0
  IL_008f:  ldfld      ""dynamic C.d""
  IL_0094:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, bool, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, bool, object)""
  IL_0099:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, object, bool>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_009e:  dup
  IL_009f:  stloc.0
  IL_00a0:  stfld      ""bool C.a""
  IL_00a5:  ldloc.0
  IL_00a6:  ldarg.0
  IL_00a7:  ldfld      ""bool C.b""
  IL_00ac:  beq.s      IL_00b0
  IL_00ae:  ldc.i4.1
  IL_00af:  ret
  IL_00b0:  ldc.i4.2
  IL_00b1:  ret
}");
        }

        [Fact]
        public void CompoundAssignment_Nullable()
        {
            string source = @"
class C
{
    int M()
    {
        bool? b = true;
        dynamic d = null;

        if ((d &= null) != (b &= null))
        {
            return 1;
        }
        
        return 2;
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      278 (0x116)
  .maxstack  12
  .locals init (bool? V_0, //b
  object V_1, //d
  bool? V_2,
  bool? V_3)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  call       ""bool?..ctor(bool)""
  IL_0008:  ldnull
  IL_0009:  stloc.1
  IL_000a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__2""
  IL_000f:  brtrue.s   IL_003d
  IL_0011:  ldc.i4.0
  IL_0012:  ldc.i4.s   83
  IL_0014:  ldtoken    ""C""
  IL_0019:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001e:  ldc.i4.1
  IL_001f:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0024:  dup
  IL_0025:  ldc.i4.0
  IL_0026:  ldc.i4.0
  IL_0027:  ldnull
  IL_0028:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002d:  stelem.ref
  IL_002e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0033:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0038:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__2""
  IL_003d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__2""
  IL_0042:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Target""
  IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>o__0.<>p__2""
  IL_004c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool?, object>> C.<>o__0.<>p__1""
  IL_0051:  brtrue.s   IL_0089
  IL_0053:  ldc.i4.0
  IL_0054:  ldc.i4.s   35
  IL_0056:  ldtoken    ""C""
  IL_005b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0060:  ldc.i4.2
  IL_0061:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0066:  dup
  IL_0067:  ldc.i4.0
  IL_0068:  ldc.i4.0
  IL_0069:  ldnull
  IL_006a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_006f:  stelem.ref
  IL_0070:  dup
  IL_0071:  ldc.i4.1
  IL_0072:  ldc.i4.1
  IL_0073:  ldnull
  IL_0074:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0079:  stelem.ref
  IL_007a:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_007f:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool?, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool?, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0084:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool?, object>> C.<>o__0.<>p__1""
  IL_0089:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool?, object>> C.<>o__0.<>p__1""
  IL_008e:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool?, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool?, object>>.Target""
  IL_0093:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool?, object>> C.<>o__0.<>p__1""
  IL_0098:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_009d:  brtrue.s   IL_00d5
  IL_009f:  ldc.i4.0
  IL_00a0:  ldc.i4.s   64
  IL_00a2:  ldtoken    ""C""
  IL_00a7:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00ac:  ldc.i4.2
  IL_00ad:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00b2:  dup
  IL_00b3:  ldc.i4.0
  IL_00b4:  ldc.i4.0
  IL_00b5:  ldnull
  IL_00b6:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00bb:  stelem.ref
  IL_00bc:  dup
  IL_00bd:  ldc.i4.1
  IL_00be:  ldc.i4.2
  IL_00bf:  ldnull
  IL_00c0:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00c5:  stelem.ref
  IL_00c6:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00cb:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00d0:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_00d5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_00da:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>>.Target""
  IL_00df:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>> C.<>o__0.<>p__0""
  IL_00e4:  ldloc.1
  IL_00e5:  ldnull
  IL_00e6:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, object)""
  IL_00eb:  dup
  IL_00ec:  stloc.1
  IL_00ed:  ldloc.0
  IL_00ee:  stloc.2
  IL_00ef:  ldloca.s   V_2
  IL_00f1:  call       ""bool bool?.GetValueOrDefault()""
  IL_00f6:  brtrue.s   IL_00fb
  IL_00f8:  ldloc.2
  IL_00f9:  br.s       IL_0104
  IL_00fb:  ldloca.s   V_3
  IL_00fd:  initobj    ""bool?""
  IL_0103:  ldloc.3
  IL_0104:  dup
  IL_0105:  stloc.0
  IL_0106:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, bool?, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, bool?)""
  IL_010b:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, object, bool>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0110:  brfalse.s  IL_0114
  IL_0112:  ldc.i4.1
  IL_0113:  ret
  IL_0114:  ldc.i4.2
  IL_0115:  ret
}
");
        }

        #endregion

        #region Object And Collection Initializers

        [Fact]
        public void DynamicObjectInitializer_Level2()
        {
            string source = @"
using System;

class C
{
    public dynamic A { get; set; }
        
    static void M()
    {
        var x = new C          
        {
            A = { B = 1 }
        };                                                   
    }
} 
";
            // Bug in Dev11: it boxes the constant literal (1) and the corresponding call-site parameter is typed to object.

            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       99 (0x63)
  .maxstack  8
  .locals init (C V_0)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__4.<>p__0""
  IL_000b:  brtrue.s   IL_0046
  IL_000d:  ldc.i4.0
  IL_000e:  ldstr      ""B""
  IL_0013:  ldtoken    ""C""
  IL_0018:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001d:  ldc.i4.2
  IL_001e:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0023:  dup
  IL_0024:  ldc.i4.0
  IL_0025:  ldc.i4.0
  IL_0026:  ldnull
  IL_0027:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002c:  stelem.ref
  IL_002d:  dup
  IL_002e:  ldc.i4.1
  IL_002f:  ldc.i4.3
  IL_0030:  ldnull
  IL_0031:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0036:  stelem.ref
  IL_0037:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003c:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0041:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__4.<>p__0""
  IL_0046:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__4.<>p__0""
  IL_004b:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>>.Target""
  IL_0050:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__4.<>p__0""
  IL_0055:  ldloc.0
  IL_0056:  callvirt   ""dynamic C.A.get""
  IL_005b:  ldc.i4.1
  IL_005c:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, int)""
  IL_0061:  pop
  IL_0062:  ret
}");
        }

        /// <summary>
        /// We can shared dynamic sites for GetMembers of level n-1 on level n for n >= 3.
        /// </summary>
        [Fact]
        public void DynamicObjectInitializer_Level3()
        {
            string source = @"
using System;

class C
{
    public dynamic A { get; set; }
        
    static void M()
    {
        var x = new C          
        {
            A = { B = { P = 1, Q = 2 } }
        };                                                   
    }
} 
";
            // Dev11 creates 4 call sites: GetMember[B] #1, SetMember[P], GetMember[B] #2, SetMember[Q] and calls them as follows:
            // var c = new C();
            // SetMember[P](GetMember[B](c) #1, 1)
            // SetMember[Q](GetMember[B](c) #2, 2)
            //
            // To maintain runtime compatibility we have to invoke GetMember[B] twice, but we can reuse the call-site:
            // We create 3 call sites: GetMember[B] #1, SetMember[P], SetMember[Q]
            // var c = new C();
            // SetMember[P](GetMember[B](c) #1, 1)
            // SetMember[Q](GetMember[B](c) #1, 2)
            // 
            // We initialize all sites up-front so that we are able to avoid duplication of call-site initialization.
            //
            // Also Dev11 emits flags (None, None) for SetMember[P], while Roslyn emits (None, UseCompileTimeType | Constant) since the RHS is a constant.

            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      285 (0x11d)
  .maxstack  8
  .locals init (C V_0)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__4.<>p__0""
  IL_000b:  brtrue.s   IL_003c
  IL_000d:  ldc.i4.0
  IL_000e:  ldstr      ""B""
  IL_0013:  ldtoken    ""C""
  IL_0018:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001d:  ldc.i4.1
  IL_001e:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0023:  dup
  IL_0024:  ldc.i4.0
  IL_0025:  ldc.i4.0
  IL_0026:  ldnull
  IL_0027:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002c:  stelem.ref
  IL_002d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0032:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0037:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__4.<>p__0""
  IL_003c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__4.<>p__1""
  IL_0041:  brtrue.s   IL_007c
  IL_0043:  ldc.i4.0
  IL_0044:  ldstr      ""P""
  IL_0049:  ldtoken    ""C""
  IL_004e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0053:  ldc.i4.2
  IL_0054:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0059:  dup
  IL_005a:  ldc.i4.0
  IL_005b:  ldc.i4.0
  IL_005c:  ldnull
  IL_005d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0062:  stelem.ref
  IL_0063:  dup
  IL_0064:  ldc.i4.1
  IL_0065:  ldc.i4.3
  IL_0066:  ldnull
  IL_0067:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_006c:  stelem.ref
  IL_006d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0072:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0077:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__4.<>p__1""
  IL_007c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__4.<>p__2""
  IL_0081:  brtrue.s   IL_00bc
  IL_0083:  ldc.i4.0
  IL_0084:  ldstr      ""Q""
  IL_0089:  ldtoken    ""C""
  IL_008e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0093:  ldc.i4.2
  IL_0094:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0099:  dup
  IL_009a:  ldc.i4.0
  IL_009b:  ldc.i4.0
  IL_009c:  ldnull
  IL_009d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00a2:  stelem.ref
  IL_00a3:  dup
  IL_00a4:  ldc.i4.1
  IL_00a5:  ldc.i4.3
  IL_00a6:  ldnull
  IL_00a7:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00ac:  stelem.ref
  IL_00ad:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00b2:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00b7:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__4.<>p__2""
  IL_00bc:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__4.<>p__1""
  IL_00c1:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>>.Target""
  IL_00c6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__4.<>p__1""
  IL_00cb:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__4.<>p__0""
  IL_00d0:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_00d5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__4.<>p__0""
  IL_00da:  ldloc.0
  IL_00db:  callvirt   ""dynamic C.A.get""
  IL_00e0:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_00e5:  ldc.i4.1
  IL_00e6:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, int)""
  IL_00eb:  pop
  IL_00ec:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__4.<>p__2""
  IL_00f1:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>>.Target""
  IL_00f6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>> C.<>o__4.<>p__2""
  IL_00fb:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__4.<>p__0""
  IL_0100:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_0105:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__4.<>p__0""
  IL_010a:  ldloc.0
  IL_010b:  callvirt   ""dynamic C.A.get""
  IL_0110:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0115:  ldc.i4.2
  IL_0116:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, int, object>.Invoke(System.Runtime.CompilerServices.CallSite, object, int)""
  IL_011b:  pop
  IL_011c:  ret
}
");
        }

        [Fact]
        public void DynamicCollectionInitializer_DynamicReceiver_InStaticMethod()
        {
            string source = @"
class C
{
    public dynamic A { get; set; }
        
    static void M()
    {
        var x = new C          
        {
            A = { { 1 } }
        };                                                   
    }
} 
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      103 (0x67)
  .maxstack  9
  .locals init (C V_0)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__4.<>p__0""
  IL_000b:  brtrue.s   IL_004b
  IL_000d:  ldc.i4     0x100
  IL_0012:  ldstr      ""Add""
  IL_0017:  ldnull
  IL_0018:  ldtoken    ""C""
  IL_001d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0022:  ldc.i4.2
  IL_0023:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0028:  dup
  IL_0029:  ldc.i4.0
  IL_002a:  ldc.i4.0
  IL_002b:  ldnull
  IL_002c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0031:  stelem.ref
  IL_0032:  dup
  IL_0033:  ldc.i4.1
  IL_0034:  ldc.i4.3
  IL_0035:  ldnull
  IL_0036:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_003b:  stelem.ref
  IL_003c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0041:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0046:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__4.<>p__0""
  IL_004b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__4.<>p__0""
  IL_0050:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, object, int> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int>>.Target""
  IL_0055:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__4.<>p__0""
  IL_005a:  ldloc.0
  IL_005b:  callvirt   ""dynamic C.A.get""
  IL_0060:  ldc.i4.1
  IL_0061:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, object, int>.Invoke(System.Runtime.CompilerServices.CallSite, object, int)""
  IL_0066:  ret
}
");
        }

        [Fact]
        public void DynamicCollectionInitializer_DynamicReceiver_InInstanceMethod()
        {
            string source = @"
class C
{
    public dynamic A { get; set; }
        
    void M()
    {
        var x = new C          
        {
            A = { { 1 } }
        };                                                   
    }
} 
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      103 (0x67)
  .maxstack  9
  .locals init (C V_0)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__4.<>p__0""
  IL_000b:  brtrue.s   IL_004b
  IL_000d:  ldc.i4     0x100
  IL_0012:  ldstr      ""Add""
  IL_0017:  ldnull
  IL_0018:  ldtoken    ""C""
  IL_001d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0022:  ldc.i4.2
  IL_0023:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0028:  dup
  IL_0029:  ldc.i4.0
  IL_002a:  ldc.i4.0
  IL_002b:  ldnull
  IL_002c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0031:  stelem.ref
  IL_0032:  dup
  IL_0033:  ldc.i4.1
  IL_0034:  ldc.i4.3
  IL_0035:  ldnull
  IL_0036:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_003b:  stelem.ref
  IL_003c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0041:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0046:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__4.<>p__0""
  IL_004b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__4.<>p__0""
  IL_0050:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, object, int> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int>>.Target""
  IL_0055:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__4.<>p__0""
  IL_005a:  ldloc.0
  IL_005b:  callvirt   ""dynamic C.A.get""
  IL_0060:  ldc.i4.1
  IL_0061:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, object, int>.Invoke(System.Runtime.CompilerServices.CallSite, object, int)""
  IL_0066:  ret
}
");
        }

        [Fact]
        public void DynamicCollectionInitializer_StaticReceiver()
        {
            string source = @"
using System.Collections;

class C : IEnumerable
{
    public IEnumerator GetEnumerator() { return null; }
    public void Add(int a) { }

    void M(dynamic d)
    {
        var x = new C          
        {
            { d }
        };                                                   
    }
}  
";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       98 (0x62)
  .maxstack  9
  .locals init (C V_0)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, C, object>> C.<>o__2.<>p__0""
  IL_000b:  brtrue.s   IL_004b
  IL_000d:  ldc.i4     0x100
  IL_0012:  ldstr      ""Add""
  IL_0017:  ldnull
  IL_0018:  ldtoken    ""C""
  IL_001d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0022:  ldc.i4.2
  IL_0023:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0028:  dup
  IL_0029:  ldc.i4.0
  IL_002a:  ldc.i4.1
  IL_002b:  ldnull
  IL_002c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0031:  stelem.ref
  IL_0032:  dup
  IL_0033:  ldc.i4.1
  IL_0034:  ldc.i4.0
  IL_0035:  ldnull
  IL_0036:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_003b:  stelem.ref
  IL_003c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0041:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, C, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, C, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0046:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, C, object>> C.<>o__2.<>p__0""
  IL_004b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, C, object>> C.<>o__2.<>p__0""
  IL_0050:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, C, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, C, object>>.Target""
  IL_0055:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, C, object>> C.<>o__2.<>p__0""
  IL_005a:  ldloc.0
  IL_005b:  ldarg.1
  IL_005c:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, C, object>.Invoke(System.Runtime.CompilerServices.CallSite, C, object)""
  IL_0061:  ret
}
");
        }

        [Fact]
        public void ObjectAndCollectionInitializer()
        {
            string source = @"
using System;
using System.Collections.Generic;
 
class Program
{
    static void Main()
    {
        var c = new C { X = { Y = { 1 } } };
    }
}
 
class C
{
    public dynamic X = new X();
}
 
class X
{
    public List<int> Y;
}
";
            CompileAndVerifyIL(source, "Program.Main", @"
{
  // Code size      177 (0xb1)
  .maxstack  9
  .locals init (C V_0)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> Program.<>o__0.<>p__0""
  IL_000b:  brtrue.s   IL_003c
  IL_000d:  ldc.i4.0
  IL_000e:  ldstr      ""Y""
  IL_0013:  ldtoken    ""Program""
  IL_0018:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001d:  ldc.i4.1
  IL_001e:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0023:  dup
  IL_0024:  ldc.i4.0
  IL_0025:  ldc.i4.0
  IL_0026:  ldnull
  IL_0027:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002c:  stelem.ref
  IL_002d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0032:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0037:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> Program.<>o__0.<>p__0""
  IL_003c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int>> Program.<>o__0.<>p__1""
  IL_0041:  brtrue.s   IL_0081
  IL_0043:  ldc.i4     0x100
  IL_0048:  ldstr      ""Add""
  IL_004d:  ldnull
  IL_004e:  ldtoken    ""Program""
  IL_0053:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0058:  ldc.i4.2
  IL_0059:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_005e:  dup
  IL_005f:  ldc.i4.0
  IL_0060:  ldc.i4.0
  IL_0061:  ldnull
  IL_0062:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0067:  stelem.ref
  IL_0068:  dup
  IL_0069:  ldc.i4.1
  IL_006a:  ldc.i4.3
  IL_006b:  ldnull
  IL_006c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0071:  stelem.ref
  IL_0072:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0077:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_007c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int>> Program.<>o__0.<>p__1""
  IL_0081:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int>> Program.<>o__0.<>p__1""
  IL_0086:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, object, int> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int>>.Target""
  IL_008b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, object, int>> Program.<>o__0.<>p__1""
  IL_0090:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> Program.<>o__0.<>p__0""
  IL_0095:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
  IL_009a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> Program.<>o__0.<>p__0""
  IL_009f:  ldloc.0
  IL_00a0:  ldfld      ""dynamic C.X""
  IL_00a5:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_00aa:  ldc.i4.1
  IL_00ab:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, object, int>.Invoke(System.Runtime.CompilerServices.CallSite, object, int)""
  IL_00b0:  ret
}
");
        }

        #endregion

        #region Foreach

        [Fact]
        public void ForEach_StaticallyTypedVariable()
        {
            string source = @"
class C
{
    void M(dynamic d)
    {
        foreach (int x in d) 
        {
            System.Console.WriteLine(x);
    }
    }
}";

            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      175 (0xaf)
  .maxstack  3
  .locals init (System.Collections.IEnumerator V_0,
  System.IDisposable V_1)
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Collections.IEnumerable>> C.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_002b
  IL_0007:  ldc.i4.0
  IL_0008:  ldtoken    ""System.Collections.IEnumerable""
  IL_000d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0021:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Collections.IEnumerable>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Collections.IEnumerable>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0026:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Collections.IEnumerable>> C.<>o__0.<>p__0""
  IL_002b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Collections.IEnumerable>> C.<>o__0.<>p__0""
  IL_0030:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, System.Collections.IEnumerable> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Collections.IEnumerable>>.Target""
  IL_0035:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Collections.IEnumerable>> C.<>o__0.<>p__0""
  IL_003a:  ldarg.1
  IL_003b:  callvirt   ""System.Collections.IEnumerable System.Func<System.Runtime.CompilerServices.CallSite, object, System.Collections.IEnumerable>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0040:  callvirt   ""System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()""
  IL_0045:  stloc.0
  .try
{
  IL_0046:  br.s       IL_0093
  IL_0048:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__1""
  IL_004d:  brtrue.s   IL_0074
  IL_004f:  ldc.i4.s   16
  IL_0051:  ldtoken    ""int""
  IL_0056:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005b:  ldtoken    ""C""
  IL_0060:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0065:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_006a:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_006f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__1""
  IL_0074:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__1""
  IL_0079:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, int> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>>.Target""
  IL_007e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, int>> C.<>o__0.<>p__1""
  IL_0083:  ldloc.0
  IL_0084:  callvirt   ""object System.Collections.IEnumerator.Current.get""
  IL_0089:  callvirt   ""int System.Func<System.Runtime.CompilerServices.CallSite, object, int>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_008e:  call       ""void System.Console.WriteLine(int)""
  IL_0093:  ldloc.0
  IL_0094:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
  IL_0099:  brtrue.s   IL_0048
  IL_009b:  leave.s    IL_00ae
}
  finally
{
  IL_009d:  ldloc.0
  IL_009e:  isinst     ""System.IDisposable""
  IL_00a3:  stloc.1
  IL_00a4:  ldloc.1
  IL_00a5:  brfalse.s  IL_00ad
  IL_00a7:  ldloc.1
  IL_00a8:  callvirt   ""void System.IDisposable.Dispose()""
  IL_00ad:  endfinally
}
  IL_00ae:  ret
}
");
        }

        [Fact]
        public void ForEach_ImplicitlyTypedVariable()
        {
            string source = @"
class C
{
    void M(dynamic d)
    {
        foreach (var x in d) 
        {
            System.Console.WriteLine(x);
        }
    }
}";

            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size      208 (0xd0)
  .maxstack  9
  .locals init (System.Collections.IEnumerator V_0,
  object V_1, //x
  System.IDisposable V_2)
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Collections.IEnumerable>> C.<>o__0.<>p__1""
  IL_0005:  brtrue.s   IL_002b
  IL_0007:  ldc.i4.0
  IL_0008:  ldtoken    ""System.Collections.IEnumerable""
  IL_000d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0021:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Collections.IEnumerable>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Collections.IEnumerable>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0026:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Collections.IEnumerable>> C.<>o__0.<>p__1""
  IL_002b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Collections.IEnumerable>> C.<>o__0.<>p__1""
  IL_0030:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, System.Collections.IEnumerable> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Collections.IEnumerable>>.Target""
  IL_0035:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.Collections.IEnumerable>> C.<>o__0.<>p__1""
  IL_003a:  ldarg.1
  IL_003b:  callvirt   ""System.Collections.IEnumerable System.Func<System.Runtime.CompilerServices.CallSite, object, System.Collections.IEnumerable>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0040:  callvirt   ""System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()""
  IL_0045:  stloc.0
  .try
{
  IL_0046:  br.s       IL_00b4
  IL_0048:  ldloc.0
  IL_0049:  callvirt   ""object System.Collections.IEnumerator.Current.get""
  IL_004e:  stloc.1
  IL_004f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> C.<>o__0.<>p__0""
  IL_0054:  brtrue.s   IL_0095
  IL_0056:  ldc.i4     0x100
  IL_005b:  ldstr      ""WriteLine""
  IL_0060:  ldnull
  IL_0061:  ldtoken    ""C""
  IL_0066:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_006b:  ldc.i4.2
  IL_006c:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0071:  dup
  IL_0072:  ldc.i4.0
  IL_0073:  ldc.i4.s   33
  IL_0075:  ldnull
  IL_0076:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_007b:  stelem.ref
  IL_007c:  dup
  IL_007d:  ldc.i4.1
  IL_007e:  ldc.i4.0
  IL_007f:  ldnull
  IL_0080:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0085:  stelem.ref
  IL_0086:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_008b:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0090:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> C.<>o__0.<>p__0""
  IL_0095:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> C.<>o__0.<>p__0""
  IL_009a:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>>.Target""
  IL_009f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>> C.<>o__0.<>p__0""
  IL_00a4:  ldtoken    ""System.Console""
  IL_00a9:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00ae:  ldloc.1
  IL_00af:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, System.Type, object>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, object)""
  IL_00b4:  ldloc.0
  IL_00b5:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
  IL_00ba:  brtrue.s   IL_0048
  IL_00bc:  leave.s    IL_00cf
}
  finally
{
  IL_00be:  ldloc.0
  IL_00bf:  isinst     ""System.IDisposable""
  IL_00c4:  stloc.2
  IL_00c5:  ldloc.2
  IL_00c6:  brfalse.s  IL_00ce
  IL_00c8:  ldloc.2
  IL_00c9:  callvirt   ""void System.IDisposable.Dispose()""
  IL_00ce:  endfinally
}
  IL_00cf:  ret
}");
        }

        [WorkItem(2720, "https://github.com/dotnet/roslyn/issues/2720")]
        [Fact]
        public void ContextTypeInAsyncLambda()
        {
            string source = @"
using System;
using System.Threading.Tasks;

class C
{
    static void Main()
    {
        dynamic d = Task.FromResult(""a"");
        G(async () => await d());
    }

    static void G(Func<Task<object>> f)
    {
    }
}";

            CompileAndVerifyIL(source, "C.<>c__DisplayClass0_0.<<Main>b__0>d.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      534 (0x216)
  .maxstack  10
  .locals init (int V_0,
                object V_1,
                object V_2,
                System.Runtime.CompilerServices.ICriticalNotifyCompletion V_3,
                System.Runtime.CompilerServices.INotifyCompletion V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.<<Main>b__0>d.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse    IL_0180
    IL_000d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>c__DisplayClass0_0.<<Main>b__0>d.<>o.<>p__0""
    IL_0012:  brtrue.s   IL_0044
    IL_0014:  ldc.i4.0
    IL_0015:  ldstr      ""GetAwaiter""
    IL_001a:  ldnull
    IL_001b:  ldtoken    ""C""
    IL_0020:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_0025:  ldc.i4.1
    IL_0026:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
    IL_002b:  dup
    IL_002c:  ldc.i4.0
    IL_002d:  ldc.i4.0
    IL_002e:  ldnull
    IL_002f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
    IL_0034:  stelem.ref
    IL_0035:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
    IL_003a:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_003f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>c__DisplayClass0_0.<<Main>b__0>d.<>o.<>p__0""
    IL_0044:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>c__DisplayClass0_0.<<Main>b__0>d.<>o.<>p__0""
    IL_0049:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
    IL_004e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>c__DisplayClass0_0.<<Main>b__0>d.<>o.<>p__0""
    IL_0053:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
    IL_0058:  brtrue.s   IL_0084
    IL_005a:  ldc.i4.0
    IL_005b:  ldtoken    ""C""
    IL_0060:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_0065:  ldc.i4.1
    IL_0066:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
    IL_006b:  dup
    IL_006c:  ldc.i4.0
    IL_006d:  ldc.i4.0
    IL_006e:  ldnull
    IL_006f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
    IL_0074:  stelem.ref
    IL_0075:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Invoke(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
    IL_007a:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_007f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
    IL_0084:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
    IL_0089:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
    IL_008e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>o__0.<>p__0""
    IL_0093:  ldarg.0
    IL_0094:  ldfld      ""C.<>c__DisplayClass0_0 C.<>c__DisplayClass0_0.<<Main>b__0>d.<>4__this""
    IL_0099:  ldfld      ""object C.<>c__DisplayClass0_0.d""
    IL_009e:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
    IL_00a3:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
    IL_00a8:  stloc.2
    IL_00a9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>c__DisplayClass0_0.<<Main>b__0>d.<>o.<>p__2""
    IL_00ae:  brtrue.s   IL_00d5
    IL_00b0:  ldc.i4.s   16
    IL_00b2:  ldtoken    ""bool""
    IL_00b7:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_00bc:  ldtoken    ""C""
    IL_00c1:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_00c6:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
    IL_00cb:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_00d0:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>c__DisplayClass0_0.<<Main>b__0>d.<>o.<>p__2""
    IL_00d5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>c__DisplayClass0_0.<<Main>b__0>d.<>o.<>p__2""
    IL_00da:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Target""
    IL_00df:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<>c__DisplayClass0_0.<<Main>b__0>d.<>o.<>p__2""
    IL_00e4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>c__DisplayClass0_0.<<Main>b__0>d.<>o.<>p__1""
    IL_00e9:  brtrue.s   IL_011a
    IL_00eb:  ldc.i4.0
    IL_00ec:  ldstr      ""IsCompleted""
    IL_00f1:  ldtoken    ""C""
    IL_00f6:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_00fb:  ldc.i4.1
    IL_00fc:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
    IL_0101:  dup
    IL_0102:  ldc.i4.0
    IL_0103:  ldc.i4.0
    IL_0104:  ldnull
    IL_0105:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
    IL_010a:  stelem.ref
    IL_010b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
    IL_0110:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_0115:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>c__DisplayClass0_0.<<Main>b__0>d.<>o.<>p__1""
    IL_011a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>c__DisplayClass0_0.<<Main>b__0>d.<>o.<>p__1""
    IL_011f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
    IL_0124:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>c__DisplayClass0_0.<<Main>b__0>d.<>o.<>p__1""
    IL_0129:  ldloc.2
    IL_012a:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
    IL_012f:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, object, bool>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
    IL_0134:  brtrue.s   IL_0197
    IL_0136:  ldarg.0
    IL_0137:  ldc.i4.0
    IL_0138:  dup
    IL_0139:  stloc.0
    IL_013a:  stfld      ""int C.<>c__DisplayClass0_0.<<Main>b__0>d.<>1__state""
    IL_013f:  ldarg.0
    IL_0140:  ldloc.2
    IL_0141:  stfld      ""object C.<>c__DisplayClass0_0.<<Main>b__0>d.<>u__1""
    IL_0146:  ldloc.2
    IL_0147:  isinst     ""System.Runtime.CompilerServices.ICriticalNotifyCompletion""
    IL_014c:  stloc.3
    IL_014d:  ldloc.3
    IL_014e:  brtrue.s   IL_016b
    IL_0150:  ldloc.2
    IL_0151:  castclass  ""System.Runtime.CompilerServices.INotifyCompletion""
    IL_0156:  stloc.s    V_4
    IL_0158:  ldarg.0
    IL_0159:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> C.<>c__DisplayClass0_0.<<Main>b__0>d.<>t__builder""
    IL_015e:  ldloca.s   V_4
    IL_0160:  ldarg.0
    IL_0161:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.AwaitOnCompleted<System.Runtime.CompilerServices.INotifyCompletion, C.<>c__DisplayClass0_0.<<Main>b__0>d>(ref System.Runtime.CompilerServices.INotifyCompletion, ref C.<>c__DisplayClass0_0.<<Main>b__0>d)""
    IL_0166:  ldnull
    IL_0167:  stloc.s    V_4
    IL_0169:  br.s       IL_0179
    IL_016b:  ldarg.0
    IL_016c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> C.<>c__DisplayClass0_0.<<Main>b__0>d.<>t__builder""
    IL_0171:  ldloca.s   V_3
    IL_0173:  ldarg.0
    IL_0174:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ICriticalNotifyCompletion, C.<>c__DisplayClass0_0.<<Main>b__0>d>(ref System.Runtime.CompilerServices.ICriticalNotifyCompletion, ref C.<>c__DisplayClass0_0.<<Main>b__0>d)""
    IL_0179:  ldnull
    IL_017a:  stloc.3
    IL_017b:  leave      IL_0215
    IL_0180:  ldarg.0
    IL_0181:  ldfld      ""object C.<>c__DisplayClass0_0.<<Main>b__0>d.<>u__1""
    IL_0186:  stloc.2
    IL_0187:  ldarg.0
    IL_0188:  ldnull
    IL_0189:  stfld      ""object C.<>c__DisplayClass0_0.<<Main>b__0>d.<>u__1""
    IL_018e:  ldarg.0
    IL_018f:  ldc.i4.m1
    IL_0190:  dup
    IL_0191:  stloc.0
    IL_0192:  stfld      ""int C.<>c__DisplayClass0_0.<<Main>b__0>d.<>1__state""
    IL_0197:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>c__DisplayClass0_0.<<Main>b__0>d.<>o.<>p__3""
    IL_019c:  brtrue.s   IL_01ce
    IL_019e:  ldc.i4.0
    IL_019f:  ldstr      ""GetResult""
    IL_01a4:  ldnull
    IL_01a5:  ldtoken    ""C""
    IL_01aa:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_01af:  ldc.i4.1
    IL_01b0:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
    IL_01b5:  dup
    IL_01b6:  ldc.i4.0
    IL_01b7:  ldc.i4.0
    IL_01b8:  ldnull
    IL_01b9:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
    IL_01be:  stelem.ref
    IL_01bf:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
    IL_01c4:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_01c9:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>c__DisplayClass0_0.<<Main>b__0>d.<>o.<>p__3""
    IL_01ce:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>c__DisplayClass0_0.<<Main>b__0>d.<>o.<>p__3""
    IL_01d3:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
    IL_01d8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<>c__DisplayClass0_0.<<Main>b__0>d.<>o.<>p__3""
    IL_01dd:  ldloc.2
    IL_01de:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
    IL_01e3:  ldnull
    IL_01e4:  stloc.2
    IL_01e5:  stloc.1
    IL_01e6:  leave.s    IL_0201
  }
  catch System.Exception
  {
    IL_01e8:  stloc.s    V_5
    IL_01ea:  ldarg.0
    IL_01eb:  ldc.i4.s   -2
    IL_01ed:  stfld      ""int C.<>c__DisplayClass0_0.<<Main>b__0>d.<>1__state""
    IL_01f2:  ldarg.0
    IL_01f3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> C.<>c__DisplayClass0_0.<<Main>b__0>d.<>t__builder""
    IL_01f8:  ldloc.s    V_5
    IL_01fa:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.SetException(System.Exception)""
    IL_01ff:  leave.s    IL_0215
  }
  IL_0201:  ldarg.0
  IL_0202:  ldc.i4.s   -2
  IL_0204:  stfld      ""int C.<>c__DisplayClass0_0.<<Main>b__0>d.<>1__state""
  IL_0209:  ldarg.0
  IL_020a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> C.<>c__DisplayClass0_0.<<Main>b__0>d.<>t__builder""
  IL_020f:  ldloc.1
  IL_0210:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.SetResult(object)""
  IL_0215:  ret
}");
        }

        [WorkItem(5323, "https://github.com/dotnet/roslyn/issues/5323")]
        [Fact]
        public void DynamicUsingWithYield1()
        {
            var source =
@"
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        foreach(var i in Iter())
        {
            System.Console.WriteLine(i);
        }
    }

    static IEnumerable<Task> Iter()
    {
        dynamic d = 123;

        using (var t = D(d))    
        {
            yield return t;
            t.Wait();
        }
    }

    static Task D(dynamic arg)
    {
            return Task.FromResult(1);
    }
}";
            var comp = CreateCompilationWithMscorlib45(source, references: new[] { SystemCoreRef, CSharpRef }, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"System.Threading.Tasks.Task`1[System.Int32]");

            comp = CreateCompilationWithMscorlib45(source, references: new[] { SystemCoreRef, CSharpRef }, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: @"System.Threading.Tasks.Task`1[System.Int32]");
        }

        [WorkItem(5323, "https://github.com/dotnet/roslyn/issues/5323")]
        [Fact]
        public void DynamicUsingWithYield2()
        {
            var source =
@"
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        foreach(var i in Iter())
        {
            System.Console.WriteLine(i);
        }
    }

    static IEnumerable<Task> Iter()
    {
        dynamic d = 123;

        var t = D(d);
        using (t)    
        {
            yield return t;
            t.Wait();
        }
    }

    static Task D(dynamic arg)
    {
            return Task.FromResult(1);
    }
}";
            var comp = CreateCompilationWithMscorlib45(source, references: new[] { SystemCoreRef, CSharpRef }, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"System.Threading.Tasks.Task`1[System.Int32]");

            comp = CreateCompilationWithMscorlib45(source, references: new[] { SystemCoreRef, CSharpRef }, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: @"System.Threading.Tasks.Task`1[System.Int32]");
        }

        #endregion

        #region Using

        [Fact]
        public void UsingStatement()
        {
            string source = @"
using System;

class C
{
    dynamic d = null;
        
    void M()
    {
        using (dynamic u = d)  
        {
            Console.WriteLine();
        }
    }
}";
            CompileAndVerifyIL(source, "C.M", @"
{
  // Code size       90 (0x5a)
  .maxstack  3
  .locals init (object V_0, //u
                System.IDisposable V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""dynamic C.d""
  IL_0006:  stloc.0
  IL_0007:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.IDisposable>> C.<>o__1.<>p__0""
  IL_000c:  brtrue.s   IL_0032
  IL_000e:  ldc.i4.0
  IL_000f:  ldtoken    ""System.IDisposable""
  IL_0014:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0019:  ldtoken    ""C""
  IL_001e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0023:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0028:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.IDisposable>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.IDisposable>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_002d:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.IDisposable>> C.<>o__1.<>p__0""
  IL_0032:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.IDisposable>> C.<>o__1.<>p__0""
  IL_0037:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, System.IDisposable> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.IDisposable>>.Target""
  IL_003c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, System.IDisposable>> C.<>o__1.<>p__0""
  IL_0041:  ldloc.0
  IL_0042:  callvirt   ""System.IDisposable System.Func<System.Runtime.CompilerServices.CallSite, object, System.IDisposable>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
  IL_0047:  stloc.1
  .try
  {
    IL_0048:  call       ""void System.Console.WriteLine()""
    IL_004d:  leave.s    IL_0059
  }
  finally
  {
    IL_004f:  ldloc.1
    IL_0050:  brfalse.s  IL_0058
    IL_0052:  ldloc.1
    IL_0053:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0058:  endfinally
  }
  IL_0059:  ret
}
");
        }

        #endregion

        #region Async

        [Fact]
        public void AwaitAwait()
        {
            string source = @"
using System;
using System.Threading.Tasks;

class C
{
    static dynamic d;

    static async void M() 
    {
        var x = await await d; 
    }
}";
            CompileAndVerifyIL(source, "C.<M>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      859 (0x35b)
  .maxstack  10
  .locals init (int V_0,
                object V_1,
                object V_2,
                System.Runtime.CompilerServices.ICriticalNotifyCompletion V_3,
                System.Runtime.CompilerServices.INotifyCompletion V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<M>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse    IL_013c
    IL_000d:  ldloc.0
    IL_000e:  ldc.i4.1
    IL_000f:  beq        IL_02c6
    IL_0014:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<M>d__1.<>o__1.<>p__0""
    IL_0019:  brtrue.s   IL_004b
    IL_001b:  ldc.i4.0
    IL_001c:  ldstr      ""GetAwaiter""
    IL_0021:  ldnull
    IL_0022:  ldtoken    ""C""
    IL_0027:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_002c:  ldc.i4.1
    IL_002d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
    IL_0032:  dup
    IL_0033:  ldc.i4.0
    IL_0034:  ldc.i4.0
    IL_0035:  ldnull
    IL_0036:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
    IL_003b:  stelem.ref
    IL_003c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
    IL_0041:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_0046:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<M>d__1.<>o__1.<>p__0""
    IL_004b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<M>d__1.<>o__1.<>p__0""
    IL_0050:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
    IL_0055:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<M>d__1.<>o__1.<>p__0""
    IL_005a:  ldsfld     ""dynamic C.d""
    IL_005f:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
    IL_0064:  stloc.2
    IL_0065:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<M>d__1.<>o__1.<>p__2""
    IL_006a:  brtrue.s   IL_0091
    IL_006c:  ldc.i4.s   16
    IL_006e:  ldtoken    ""bool""
    IL_0073:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_0078:  ldtoken    ""C""
    IL_007d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_0082:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
    IL_0087:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_008c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<M>d__1.<>o__1.<>p__2""
    IL_0091:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<M>d__1.<>o__1.<>p__2""
    IL_0096:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Target""
    IL_009b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<M>d__1.<>o__1.<>p__2""
    IL_00a0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<M>d__1.<>o__1.<>p__1""
    IL_00a5:  brtrue.s   IL_00d6
    IL_00a7:  ldc.i4.0
    IL_00a8:  ldstr      ""IsCompleted""
    IL_00ad:  ldtoken    ""C""
    IL_00b2:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_00b7:  ldc.i4.1
    IL_00b8:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
    IL_00bd:  dup
    IL_00be:  ldc.i4.0
    IL_00bf:  ldc.i4.0
    IL_00c0:  ldnull
    IL_00c1:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
    IL_00c6:  stelem.ref
    IL_00c7:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
    IL_00cc:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_00d1:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<M>d__1.<>o__1.<>p__1""
    IL_00d6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<M>d__1.<>o__1.<>p__1""
    IL_00db:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
    IL_00e0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<M>d__1.<>o__1.<>p__1""
    IL_00e5:  ldloc.2
    IL_00e6:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
    IL_00eb:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, object, bool>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
    IL_00f0:  brtrue.s   IL_0153
    IL_00f2:  ldarg.0
    IL_00f3:  ldc.i4.0
    IL_00f4:  dup
    IL_00f5:  stloc.0
    IL_00f6:  stfld      ""int C.<M>d__1.<>1__state""
    IL_00fb:  ldarg.0
    IL_00fc:  ldloc.2
    IL_00fd:  stfld      ""object C.<M>d__1.<>u__1""
    IL_0102:  ldloc.2
    IL_0103:  isinst     ""System.Runtime.CompilerServices.ICriticalNotifyCompletion""
    IL_0108:  stloc.3
    IL_0109:  ldloc.3
    IL_010a:  brtrue.s   IL_0127
    IL_010c:  ldloc.2
    IL_010d:  castclass  ""System.Runtime.CompilerServices.INotifyCompletion""
    IL_0112:  stloc.s    V_4
    IL_0114:  ldarg.0
    IL_0115:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder C.<M>d__1.<>t__builder""
    IL_011a:  ldloca.s   V_4
    IL_011c:  ldarg.0
    IL_011d:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitOnCompleted<System.Runtime.CompilerServices.INotifyCompletion, C.<M>d__1>(ref System.Runtime.CompilerServices.INotifyCompletion, ref C.<M>d__1)""
    IL_0122:  ldnull
    IL_0123:  stloc.s    V_4
    IL_0125:  br.s       IL_0135
    IL_0127:  ldarg.0
    IL_0128:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder C.<M>d__1.<>t__builder""
    IL_012d:  ldloca.s   V_3
    IL_012f:  ldarg.0
    IL_0130:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ICriticalNotifyCompletion, C.<M>d__1>(ref System.Runtime.CompilerServices.ICriticalNotifyCompletion, ref C.<M>d__1)""
    IL_0135:  ldnull
    IL_0136:  stloc.3
    IL_0137:  leave      IL_035a
    IL_013c:  ldarg.0
    IL_013d:  ldfld      ""object C.<M>d__1.<>u__1""
    IL_0142:  stloc.2
    IL_0143:  ldarg.0
    IL_0144:  ldnull
    IL_0145:  stfld      ""object C.<M>d__1.<>u__1""
    IL_014a:  ldarg.0
    IL_014b:  ldc.i4.m1
    IL_014c:  dup
    IL_014d:  stloc.0
    IL_014e:  stfld      ""int C.<M>d__1.<>1__state""
    IL_0153:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<M>d__1.<>o__1.<>p__3""
    IL_0158:  brtrue.s   IL_018a
    IL_015a:  ldc.i4.0
    IL_015b:  ldstr      ""GetResult""
    IL_0160:  ldnull
    IL_0161:  ldtoken    ""C""
    IL_0166:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_016b:  ldc.i4.1
    IL_016c:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
    IL_0171:  dup
    IL_0172:  ldc.i4.0
    IL_0173:  ldc.i4.0
    IL_0174:  ldnull
    IL_0175:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
    IL_017a:  stelem.ref
    IL_017b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
    IL_0180:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_0185:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<M>d__1.<>o__1.<>p__3""
    IL_018a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<M>d__1.<>o__1.<>p__3""
    IL_018f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
    IL_0194:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<M>d__1.<>o__1.<>p__3""
    IL_0199:  ldloc.2
    IL_019a:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
    IL_019f:  ldnull
    IL_01a0:  stloc.2
    IL_01a1:  stloc.1
    IL_01a2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<M>d__1.<>o__1.<>p__4""
    IL_01a7:  brtrue.s   IL_01d9
    IL_01a9:  ldc.i4.0
    IL_01aa:  ldstr      ""GetAwaiter""
    IL_01af:  ldnull
    IL_01b0:  ldtoken    ""C""
    IL_01b5:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_01ba:  ldc.i4.1
    IL_01bb:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
    IL_01c0:  dup
    IL_01c1:  ldc.i4.0
    IL_01c2:  ldc.i4.0
    IL_01c3:  ldnull
    IL_01c4:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
    IL_01c9:  stelem.ref
    IL_01ca:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
    IL_01cf:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_01d4:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<M>d__1.<>o__1.<>p__4""
    IL_01d9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<M>d__1.<>o__1.<>p__4""
    IL_01de:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
    IL_01e3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<M>d__1.<>o__1.<>p__4""
    IL_01e8:  ldloc.1
    IL_01e9:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
    IL_01ee:  stloc.2
    IL_01ef:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<M>d__1.<>o__1.<>p__6""
    IL_01f4:  brtrue.s   IL_021b
    IL_01f6:  ldc.i4.s   16
    IL_01f8:  ldtoken    ""bool""
    IL_01fd:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_0202:  ldtoken    ""C""
    IL_0207:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_020c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
    IL_0211:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_0216:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<M>d__1.<>o__1.<>p__6""
    IL_021b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<M>d__1.<>o__1.<>p__6""
    IL_0220:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>>.Target""
    IL_0225:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, bool>> C.<M>d__1.<>o__1.<>p__6""
    IL_022a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<M>d__1.<>o__1.<>p__5""
    IL_022f:  brtrue.s   IL_0260
    IL_0231:  ldc.i4.0
    IL_0232:  ldstr      ""IsCompleted""
    IL_0237:  ldtoken    ""C""
    IL_023c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_0241:  ldc.i4.1
    IL_0242:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
    IL_0247:  dup
    IL_0248:  ldc.i4.0
    IL_0249:  ldc.i4.0
    IL_024a:  ldnull
    IL_024b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
    IL_0250:  stelem.ref
    IL_0251:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
    IL_0256:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_025b:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<M>d__1.<>o__1.<>p__5""
    IL_0260:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<M>d__1.<>o__1.<>p__5""
    IL_0265:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
    IL_026a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<M>d__1.<>o__1.<>p__5""
    IL_026f:  ldloc.2
    IL_0270:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
    IL_0275:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, object, bool>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
    IL_027a:  brtrue.s   IL_02dd
    IL_027c:  ldarg.0
    IL_027d:  ldc.i4.1
    IL_027e:  dup
    IL_027f:  stloc.0
    IL_0280:  stfld      ""int C.<M>d__1.<>1__state""
    IL_0285:  ldarg.0
    IL_0286:  ldloc.2
    IL_0287:  stfld      ""object C.<M>d__1.<>u__1""
    IL_028c:  ldloc.2
    IL_028d:  isinst     ""System.Runtime.CompilerServices.ICriticalNotifyCompletion""
    IL_0292:  stloc.3
    IL_0293:  ldloc.3
    IL_0294:  brtrue.s   IL_02b1
    IL_0296:  ldloc.2
    IL_0297:  castclass  ""System.Runtime.CompilerServices.INotifyCompletion""
    IL_029c:  stloc.s    V_4
    IL_029e:  ldarg.0
    IL_029f:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder C.<M>d__1.<>t__builder""
    IL_02a4:  ldloca.s   V_4
    IL_02a6:  ldarg.0
    IL_02a7:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitOnCompleted<System.Runtime.CompilerServices.INotifyCompletion, C.<M>d__1>(ref System.Runtime.CompilerServices.INotifyCompletion, ref C.<M>d__1)""
    IL_02ac:  ldnull
    IL_02ad:  stloc.s    V_4
    IL_02af:  br.s       IL_02bf
    IL_02b1:  ldarg.0
    IL_02b2:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder C.<M>d__1.<>t__builder""
    IL_02b7:  ldloca.s   V_3
    IL_02b9:  ldarg.0
    IL_02ba:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ICriticalNotifyCompletion, C.<M>d__1>(ref System.Runtime.CompilerServices.ICriticalNotifyCompletion, ref C.<M>d__1)""
    IL_02bf:  ldnull
    IL_02c0:  stloc.3
    IL_02c1:  leave      IL_035a
    IL_02c6:  ldarg.0
    IL_02c7:  ldfld      ""object C.<M>d__1.<>u__1""
    IL_02cc:  stloc.2
    IL_02cd:  ldarg.0
    IL_02ce:  ldnull
    IL_02cf:  stfld      ""object C.<M>d__1.<>u__1""
    IL_02d4:  ldarg.0
    IL_02d5:  ldc.i4.m1
    IL_02d6:  dup
    IL_02d7:  stloc.0
    IL_02d8:  stfld      ""int C.<M>d__1.<>1__state""
    IL_02dd:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<M>d__1.<>o__1.<>p__7""
    IL_02e2:  brtrue.s   IL_0314
    IL_02e4:  ldc.i4.0
    IL_02e5:  ldstr      ""GetResult""
    IL_02ea:  ldnull
    IL_02eb:  ldtoken    ""C""
    IL_02f0:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_02f5:  ldc.i4.1
    IL_02f6:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
    IL_02fb:  dup
    IL_02fc:  ldc.i4.0
    IL_02fd:  ldc.i4.0
    IL_02fe:  ldnull
    IL_02ff:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
    IL_0304:  stelem.ref
    IL_0305:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
    IL_030a:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_030f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<M>d__1.<>o__1.<>p__7""
    IL_0314:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<M>d__1.<>o__1.<>p__7""
    IL_0319:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, object, object> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>>.Target""
    IL_031e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, object, object>> C.<M>d__1.<>o__1.<>p__7""
    IL_0323:  ldloc.2
    IL_0324:  callvirt   ""object System.Func<System.Runtime.CompilerServices.CallSite, object, object>.Invoke(System.Runtime.CompilerServices.CallSite, object)""
    IL_0329:  ldnull
    IL_032a:  stloc.2
    IL_032b:  pop
    IL_032c:  leave.s    IL_0347
  }
  catch System.Exception
  {
    IL_032e:  stloc.s    V_5
    IL_0330:  ldarg.0
    IL_0331:  ldc.i4.s   -2
    IL_0333:  stfld      ""int C.<M>d__1.<>1__state""
    IL_0338:  ldarg.0
    IL_0339:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder C.<M>d__1.<>t__builder""
    IL_033e:  ldloc.s    V_5
    IL_0340:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)""
    IL_0345:  leave.s    IL_035a
  }
  IL_0347:  ldarg.0
  IL_0348:  ldc.i4.s   -2
  IL_034a:  stfld      ""int C.<M>d__1.<>1__state""
  IL_034f:  ldarg.0
  IL_0350:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder C.<M>d__1.<>t__builder""
  IL_0355:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()""
  IL_035a:  ret
}
");
        }

        #endregion
    }
}
