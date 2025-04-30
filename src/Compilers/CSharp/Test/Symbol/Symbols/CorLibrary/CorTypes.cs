// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.CorLibrary
{
    public class CorTypes : CSharpTestBase
    {
        private static readonly SymbolDisplayFormat s_languageNameFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

        [Fact]
        public void MissingCorLib()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[] { TestReferences.SymbolsTests.CorLibrary.NoMsCorLibRef });

            var noMsCorLibRef = assemblies[0];

            for (int i = 1; i <= (int)SpecialType.Count; i++)
            {
                var t = noMsCorLibRef.GetSpecialType((SpecialType)i);
                Assert.Equal((SpecialType)i, t.SpecialType);
                Assert.Equal(TypeKind.Error, t.TypeKind);
                Assert.NotNull(t.ContainingAssembly);
                Assert.Equal("<Missing Core Assembly>", t.ContainingAssembly.Identity.Name);
            }

            var p = noMsCorLibRef.GlobalNamespace.GetTypeMembers("I1").Single().
                GetMembers("M1").OfType<MethodSymbol>().Single().
                Parameters[0].TypeWithAnnotations;

            Assert.Equal(TypeKind.Error, p.Type.TypeKind);
            Assert.Equal(SpecialType.System_Int32, p.SpecialType);
        }

        [Fact]
        public void PresentCorLib()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[] { NetCoreApp.SystemRuntime });

            MetadataOrSourceAssemblySymbol msCorLibRef = (MetadataOrSourceAssemblySymbol)assemblies[0];

            var knownMissingSpecialTypes = new HashSet<SpecialType>()
            {
                SpecialType.System_Runtime_CompilerServices_InlineArrayAttribute,
            };

            var knownMissingInternalSpecialTypes = new HashSet<InternalSpecialType>()
            {
                InternalSpecialType.System_Runtime_CompilerServices_AsyncHelpers,
            };

            for (int i = 1; i <= (int)SpecialType.Count; i++)
            {
                var specialType = (SpecialType)i;
                var t = msCorLibRef.GetSpecialType(specialType);
                Assert.Equal(specialType, t.SpecialType);
                Assert.Equal((ExtendedSpecialType)i, t.ExtendedSpecialType);
                Assert.Same(msCorLibRef, t.ContainingAssembly);
                if (knownMissingSpecialTypes.Contains(specialType))
                {
                    // not present on dotnet core 3.1
                    Assert.Equal(TypeKind.Error, t.TypeKind);
                }
                else
                {
                    Assert.NotEqual(TypeKind.Error, t.TypeKind);
                }
            }

            for (int i = (int)InternalSpecialType.First; i < (int)InternalSpecialType.NextAvailable; i++)
            {
                var internalSpecialType = (InternalSpecialType)i;
                var t = msCorLibRef.GetSpecialType(internalSpecialType);
                Assert.Equal(SpecialType.None, t.SpecialType);
                Assert.Equal((ExtendedSpecialType)i, t.ExtendedSpecialType);
                Assert.Same(msCorLibRef, t.ContainingAssembly);
                if (knownMissingInternalSpecialTypes.Contains(internalSpecialType))
                {
                    // not present on dotnet core 3.1
                    Assert.Equal(TypeKind.Error, t.TypeKind);
                }
                else
                {
                    Assert.NotEqual(TypeKind.Error, t.TypeKind);
                }
            }

            Assert.False(msCorLibRef.KeepLookingForDeclaredSpecialTypes);

            assemblies = MetadataTestHelpers.GetSymbolsForReferences(mrefs: new[] { MetadataReference.CreateFromImage(Net50.Resources.SystemRuntime) });

            msCorLibRef = (MetadataOrSourceAssemblySymbol)assemblies[0];
            Assert.True(msCorLibRef.KeepLookingForDeclaredSpecialTypes);

            Queue<NamespaceSymbol> namespaces = new Queue<NamespaceSymbol>();

            namespaces.Enqueue(msCorLibRef.Modules[0].GlobalNamespace);
            int count = 0;

            while (namespaces.Count > 0)
            {
                foreach (var m in namespaces.Dequeue().GetMembers())
                {
                    NamespaceSymbol ns = m as NamespaceSymbol;

                    if (ns != null)
                    {
                        namespaces.Enqueue(ns);
                    }
                    else if (((NamedTypeSymbol)m).SpecialType != SpecialType.None)
                    {
                        count++;
                    }

                    if (count >= (int)SpecialType.Count)
                    {
                        Assert.False(msCorLibRef.KeepLookingForDeclaredSpecialTypes);
                    }
                }
            }

            Assert.Equal((int)SpecialType.Count, count + knownMissingSpecialTypes.Count);
            Assert.Equal(knownMissingSpecialTypes.Any(), msCorLibRef.KeepLookingForDeclaredSpecialTypes);
        }

        [Fact]
        public void FakeCorLib()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[] { TestReferences.SymbolsTests.CorLibrary.FakeMsCorLib.dll });

            MetadataOrSourceAssemblySymbol msCorLibRef = (MetadataOrSourceAssemblySymbol)assemblies[0];

            for (int i = 1; i <= (int)SpecialType.Count; i++)
            {
                Assert.True(msCorLibRef.KeepLookingForDeclaredSpecialTypes);
                var t = msCorLibRef.GetSpecialType((SpecialType)i);
                Assert.Equal((SpecialType)i, t.SpecialType);
                Assert.Equal((ExtendedSpecialType)i, t.ExtendedSpecialType);

                if (t.SpecialType == SpecialType.System_Object)
                {
                    Assert.NotEqual(TypeKind.Error, t.TypeKind);
                }
                else
                {
                    Assert.Equal(TypeKind.Error, t.TypeKind);
                }

                Assert.Same(msCorLibRef, t.ContainingAssembly);
            }

            for (int i = (int)InternalSpecialType.First; i < (int)InternalSpecialType.NextAvailable; i++)
            {
                Assert.True(msCorLibRef.KeepLookingForDeclaredSpecialTypes);
                var t = msCorLibRef.GetSpecialType((InternalSpecialType)i);
                Assert.Equal(SpecialType.None, t.SpecialType);
                Assert.Equal((ExtendedSpecialType)i, t.ExtendedSpecialType);

                Assert.Equal(TypeKind.Error, t.TypeKind);
                Assert.Same(msCorLibRef, t.ContainingAssembly);
            }

            Assert.False(msCorLibRef.KeepLookingForDeclaredSpecialTypes);
        }

        [Fact]
        public void SourceCorLib()
        {
            string source = @"
namespace System
{
    public class Object
    {
    }
}
";

            var c1 = CSharpCompilation.Create("CorLib", syntaxTrees: new[] { Parse(source) });

            Assert.Same(c1.Assembly, c1.Assembly.CorLibrary);

            MetadataOrSourceAssemblySymbol msCorLibRef = (MetadataOrSourceAssemblySymbol)c1.Assembly;

            for (int i = 1; i <= (int)SpecialType.Count; i++)
            {
                if (i != (int)SpecialType.System_Object)
                {
                    Assert.True(msCorLibRef.KeepLookingForDeclaredSpecialTypes);
                    var t = c1.GetSpecialType((SpecialType)i);
                    Assert.Equal((SpecialType)i, t.SpecialType);
                    Assert.Equal((ExtendedSpecialType)i, t.ExtendedSpecialType);

                    Assert.Equal(TypeKind.Error, t.TypeKind);
                    Assert.Same(msCorLibRef, t.ContainingAssembly);
                }
            }

            for (int i = (int)InternalSpecialType.First; i < (int)InternalSpecialType.NextAvailable; i++)
            {
                Assert.True(msCorLibRef.KeepLookingForDeclaredSpecialTypes);
                var t = c1.GetSpecialType((InternalSpecialType)i);
                Assert.Equal(SpecialType.None, t.SpecialType);
                Assert.Equal((ExtendedSpecialType)i, t.ExtendedSpecialType);

                Assert.Equal(TypeKind.Error, t.TypeKind);
                Assert.Same(msCorLibRef, t.ContainingAssembly);
            }

            var system_object = msCorLibRef.Modules[0].GlobalNamespace.GetMembers("System").
                Select(m => (NamespaceSymbol)m).Single().GetTypeMembers("Object").Single();

            Assert.Equal(SpecialType.System_Object, system_object.SpecialType);
            Assert.Equal((ExtendedSpecialType)SpecialType.System_Object, system_object.ExtendedSpecialType);

            Assert.False(msCorLibRef.KeepLookingForDeclaredSpecialTypes);

            Assert.Same(system_object, c1.GetSpecialType(SpecialType.System_Object));

            Assert.Throws<ArgumentOutOfRangeException>(() => c1.GetSpecialType(SpecialType.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => ((Compilation)c1).GetSpecialType(SpecialType.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => c1.GetSpecialType(InternalSpecialType.NextAvailable));
            Assert.Throws<ArgumentOutOfRangeException>(() => ((Compilation)c1).GetSpecialType(SpecialType.Count + 1));
        }

        [WorkItem(697521, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/697521")]
        [Fact]
        public void SubclassSystemArray()
        {
            var source1 = @"
namespace System
{
    public class Object
    {
    }

    public class Void
    {
    }

    public class Array : Object
    {
    }
}
";

            var source2 = @"
namespace System
{
    internal class ArrayContract : Array
    {
    }
}
";

            // Fine in corlib.
            CreateEmptyCompilation(source1 + source2).VerifyDiagnostics();

            // Error elsewhere.
            CreateCompilation(source2).VerifyDiagnostics(
                // (4,36): error CS0644: 'System.ArrayContract' cannot derive from special class 'System.Array'
                //     internal class ArrayContract : Array
                Diagnostic(ErrorCode.ERR_DeriveFromEnumOrValueType, "Array").WithArguments("System.ArrayContract", "System.Array"));
        }

        [Fact]
        public void System_Type__WellKnownVsSpecial_01()
        {
            var source = @"
class Program
{
    static void Main()
    {
        var x = typeof(Program);
        System.Console.WriteLine(x);
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.MakeMemberMissing(WellKnownMember.System_Type__GetTypeFromHandle);

            Assert.False(comp.GetSpecialType(InternalSpecialType.System_Type).IsErrorType());

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<TypeOfExpressionSyntax>().Single();
            var model = comp.GetSemanticModel(tree);

            Assert.Equal(InternalSpecialType.System_Type, model.GetTypeInfo(node).Type.GetSymbol().ExtendedSpecialType);

            CompileAndVerify(comp, expectedOutput: "Program");

            comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.MakeMemberMissing(SpecialMember.System_Type__GetTypeFromHandle);
            comp.VerifyEmitDiagnostics(
                // (6,17): error CS0656: Missing compiler required member 'System.Type.GetTypeFromHandle'
                //         var x = typeof(Program);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "typeof(Program)").WithArguments("System.Type", "GetTypeFromHandle").WithLocation(6, 17)
                );
        }

        [Fact]
        public void System_Type__WellKnownVsSpecial_02()
        {
            var corLib_v1 = @"
namespace System
{
    public class Object
    {}

    public class Void
    {}

    public class ValueType
    {}

    public struct RuntimeTypeHandle
    {}
}
";
            var corLib_v1_Comp = CreateEmptyCompilation(corLib_v1, assemblyName: "corLib");

            var typeLib_v1 = @"
namespace System
{
    public class Type
    {
        public static Type GetTypeFromHandle(RuntimeTypeHandle handle) => null;
    }
}
";

            var typeLib_v1_Comp = CreateEmptyCompilation(typeLib_v1, references: [corLib_v1_Comp.ToMetadataReference()], assemblyName: "typeLib");

            var source1 = @"
#nullable disable

public class Test
{
    public static System.Type TypeOf() => typeof(Test);
}
";
            var comp1 = CreateEmptyCompilation(
                source1, references: [corLib_v1_Comp.ToMetadataReference(), typeLib_v1_Comp.ToMetadataReference()],
                parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute());

            Assert.True(comp1.GetSpecialType(InternalSpecialType.System_Type).IsErrorType());
            comp1.MakeMemberMissing(SpecialMember.System_Type__GetTypeFromHandle);

            var tree = comp1.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<TypeOfExpressionSyntax>().Single();
            var model = comp1.GetSemanticModel(tree);

            Assert.Equal((ExtendedSpecialType)0, model.GetTypeInfo(node).Type.GetSymbol().ExtendedSpecialType);

            var comp1Ref = comp1.EmitToImageReference();

            var corLib_v2 = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Object))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(void))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.ValueType))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.RuntimeTypeHandle))]
";
            var corLib_v2_Comp = CreateCompilation(corLib_v2, assemblyName: "corLib");

            var typeLib_v2 = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Type))]
";

            var typeLib_v2_Comp = CreateCompilation(typeLib_v2, assemblyName: "typeLib");

            var source2 = @"
class Program
{
    static void Main()
    {
        System.Console.WriteLine(Test.TypeOf());
    }
}
";

            var comp = CreateCompilation(source2, references: [corLib_v2_Comp.ToMetadataReference(), typeLib_v2_Comp.ToMetadataReference(), comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "Test");

            comp1 = CreateEmptyCompilation(
                source1, references: [corLib_v1_Comp.ToMetadataReference(), typeLib_v1_Comp.ToMetadataReference()],
                parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute());

            comp1.MakeMemberMissing(WellKnownMember.System_Type__GetTypeFromHandle);
            comp1.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // (6,43): error CS0656: Missing compiler required member 'System.Type.GetTypeFromHandle'
                //     public static System.Type TypeOf() => typeof(Test);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "typeof(Test)").WithArguments("System.Type", "GetTypeFromHandle").WithLocation(6, 43)
                );
        }

        [Fact]
        public void System_Type__WellKnownVsSpecial_03()
        {
            var source = @"
record R
{
    public static System.Type TypeOf() => new R().EqualityContract;
}

class Program
{
    static void Main()
    {
        System.Console.WriteLine(R.TypeOf());
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.MakeMemberMissing(WellKnownMember.System_Type__GetTypeFromHandle);

            Assert.False(comp.GetSpecialType(InternalSpecialType.System_Type).IsErrorType());

            CompileAndVerify(comp, expectedOutput: "R");

            comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.MakeMemberMissing(SpecialMember.System_Type__GetTypeFromHandle);
            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0656: Missing compiler required member 'System.Type.GetTypeFromHandle'
                // record R
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"record R
{
    public static System.Type TypeOf() => new R().EqualityContract;
}").WithArguments("System.Type", "GetTypeFromHandle").WithLocation(2, 1)
                );
        }

        [Fact]
        public void System_Type__WellKnownVsSpecial_04()
        {
            var corLib_v1 = @"
namespace System
{
    public class Object
    {
        public virtual string ToString() => null;
        public virtual int GetHashCode() => 0;
        public virtual bool Equals(object obj) => false;
    }

    public class Void
    {}

    public class ValueType
    {}

    public struct RuntimeTypeHandle
    {}

    public struct Byte
    {}

    public struct Int32
    {}

    public struct Boolean
    {}

    public class String
    {}

    public interface IEquatable<T>
    {
        bool Equals(T other);
    }

    public class Attribute
    {}

    public enum AttributeTargets
    {
    }
    public class AttributeUsageAttribute
    {
        public AttributeUsageAttribute(AttributeTargets validOn){}
        public bool AllowMultiple => false;
        public bool Inherited => false;
    }

    public class Exception
    {}

    namespace Text
    {
        public sealed class StringBuilder
        {}
    
    }
}
";
            var corLib_v1_Comp = CreateEmptyCompilation(corLib_v1, assemblyName: "corLib");

            var typeLib_v1 = @"
namespace System
{
    public class Type
    {
        public static Type GetTypeFromHandle(RuntimeTypeHandle handle) => null;
    }
}
";

            var typeLib_v1_Comp = CreateEmptyCompilation(typeLib_v1, references: [corLib_v1_Comp.ToMetadataReference()], assemblyName: "typeLib");

            var source1 = @"
#nullable disable

sealed public record R
{
    public static System.Type TypeOf() => new R().EqualityContract;
    public override string ToString() => null;
    public override int GetHashCode() => 0;
    public bool Equals(R obj) => false;
}
";
            var comp1 = CreateEmptyCompilation(
                source1, references: [corLib_v1_Comp.ToMetadataReference(), typeLib_v1_Comp.ToMetadataReference()],
                parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute());

            Assert.True(comp1.GetSpecialType(InternalSpecialType.System_Type).IsErrorType());
            comp1.MakeMemberMissing(SpecialMember.System_Type__GetTypeFromHandle);

            var comp1Ref = comp1.EmitToImageReference();

            var corLib_v2 = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Object))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(void))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.ValueType))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.RuntimeTypeHandle))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Byte))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Int32))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Boolean))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.String))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Attribute))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.AttributeTargets))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.AttributeUsageAttribute))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Exception))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.IEquatable<>))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Text.StringBuilder))]
";
            var corLib_v2_Comp = CreateCompilation(corLib_v2, assemblyName: "corLib");

            var typeLib_v2 = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Type))]
";

            var typeLib_v2_Comp = CreateCompilation(typeLib_v2, assemblyName: "typeLib");

            var source2 = @"
class Program
{
    static void Main()
    {
        System.Console.WriteLine(R.TypeOf());
    }
}
";

            var comp = CreateCompilation(source2, references: [corLib_v2_Comp.ToMetadataReference(), typeLib_v2_Comp.ToMetadataReference(), comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "R");

            comp1 = CreateEmptyCompilation(
                source1, references: [corLib_v1_Comp.ToMetadataReference(), typeLib_v1_Comp.ToMetadataReference()],
                parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute());

            comp1.MakeMemberMissing(WellKnownMember.System_Type__GetTypeFromHandle);
            comp1.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // (4,1): error CS0656: Missing compiler required member 'System.Type.GetTypeFromHandle'
                // sealed public record R
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"sealed public record R
{
    public static System.Type TypeOf() => new R().EqualityContract;
    public override string ToString() => null;
    public override int GetHashCode() => 0;
    public bool Equals(R obj) => false;
}").WithArguments("System.Type", "GetTypeFromHandle").WithLocation(4, 1)
                );
        }

        [Fact]
        public void CreateDelegate__MethodInfoVsDelegate_01()
        {
            var source = @"
class Program
{
    static void Main()
    {
        System.Linq.Expressions.Expression<System.Func<System.Action>> x = () => C1.M1;
        System.Console.WriteLine(x);
    }
}

class C1
{
    public static void M1() {}
}
";

            var comp = CreateCompilation(source, targetFramework: TargetFramework.Mscorlib40AndSystemCore, options: TestOptions.DebugExe);
            comp.MakeMemberMissing(WellKnownMember.System_Reflection_MethodInfo__CreateDelegate);
            comp.MakeMemberMissing(SpecialMember.System_Reflection_MethodBase__GetMethodFromHandle2);
            comp.MakeMemberMissing(WellKnownMember.System_Reflection_MethodBase__GetMethodFromHandle);
            comp.MakeMemberMissing(WellKnownMember.System_Reflection_MethodBase__GetMethodFromHandle2);

            CompileAndVerify(comp, expectedOutput: "() => Convert(CreateDelegate(System.Action, null, Void M1())" +
                                      (ExecutionConditionUtil.IsMonoOrCoreClr ? ", Action" : "") +
                                      ")");

            comp = CreateCompilation(source, targetFramework: TargetFramework.Mscorlib40AndSystemCore, options: TestOptions.DebugExe);
            comp.MakeMemberMissing(SpecialMember.System_Delegate__CreateDelegate);
            comp.VerifyEmitDiagnostics(
                // (6,82): error CS0656: Missing compiler required member 'System.Delegate.CreateDelegate'
                //         System.Linq.Expressions.Expression<System.Func<System.Action>> x = () => C1.M1;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "C1.M1").WithArguments("System.Delegate", "CreateDelegate").WithLocation(6, 82)
                );

            comp = CreateCompilation(source, targetFramework: TargetFramework.Mscorlib40AndSystemCore, options: TestOptions.DebugExe);
            comp.MakeMemberMissing(SpecialMember.System_Reflection_MethodBase__GetMethodFromHandle);
            comp.VerifyEmitDiagnostics(
                // (6,82): error CS0656: Missing compiler required member 'System.Reflection.MethodBase.GetMethodFromHandle'
                //         System.Linq.Expressions.Expression<System.Func<System.Action>> x = () => C1.M1;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "C1.M1").WithArguments("System.Reflection.MethodBase", "GetMethodFromHandle").WithLocation(6, 82)
                );
        }

        [Fact]
        public void CreateDelegate__MethodInfoVsDelegate_02()
        {
            var source = @"
class Program
{
    static void Main()
    {
        System.Linq.Expressions.Expression<System.Func<System.Action>> x = () => C1<int>.M1;
        System.Console.WriteLine(x);
    }
}

class C1<T>
{
    public static void M1() {}
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.MakeMemberMissing(SpecialMember.System_Delegate__CreateDelegate);
            comp.MakeMemberMissing(WellKnownMember.System_Reflection_MethodBase__GetMethodFromHandle);
            comp.MakeMemberMissing(WellKnownMember.System_Reflection_MethodBase__GetMethodFromHandle2);

            CompileAndVerify(
                comp, expectedOutput: "() => Convert(Void M1().CreateDelegate(System.Action, null)" +
                                      (ExecutionConditionUtil.IsMonoOrCoreClr ? ", Action" : "") +
                                      ")");

            comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.MakeMemberMissing(SpecialMember.System_Reflection_MethodBase__GetMethodFromHandle2);
            comp.VerifyEmitDiagnostics(
                // (6,82): error CS0656: Missing compiler required member 'System.Reflection.MethodBase.GetMethodFromHandle'
                //         System.Linq.Expressions.Expression<System.Func<System.Action>> x = () => C1<int>.M1;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "C1<int>.M1").WithArguments("System.Reflection.MethodBase", "GetMethodFromHandle").WithLocation(6, 82)
                );
        }

        [Fact]
        public void GetMethodFromHandle_WellKnown_01()
        {
            var corLib_v1 = @"
namespace System
{
    public class Object
    {}

    public class Void
    {}

    public class ValueType
    {}

    public struct RuntimeTypeHandle
    {}

    public struct RuntimeMethodHandle
    {}

    public struct Int32
    {}

    public abstract class Delegate
    {}

    public abstract class MulticastDelegate : Delegate
    {}

    public delegate void Action();

    public delegate TResult Func<out TResult>();

    public struct Nullable<T>
    {}
}

namespace System.Collections.Generic
{
    public interface IEnumerable<T>
    {}
}
";
            var corLib_v1_Comp = CreateEmptyCompilation(corLib_v1, assemblyName: "corLib");

            var typeLib_v1 = @"
namespace System
{
    public class Type
    {
        public static Type GetTypeFromHandle(RuntimeTypeHandle handle) => null;
    }
}
namespace System.Reflection
{
    public abstract partial class MethodBase
    {
        public static MethodBase GetMethodFromHandle(RuntimeMethodHandle handle) => null;
    }

    public abstract partial class MethodInfo : MethodBase
    {
        public virtual Delegate CreateDelegate(Type delegateType) => null;
        public virtual Delegate CreateDelegate(Type delegateType, object target) => null;
    }
}

namespace System.Linq.Expressions
{
    public abstract class Expression
    {
        public static ConstantExpression Constant (object value) => null;
        public static ConstantExpression Constant (object value, Type type) => null;

        public static MethodCallExpression Call (Expression instance, System.Reflection.MethodInfo method, Expression[] arguments) => null;

        public static UnaryExpression Convert (Expression expression, Type type) => null;
        public static UnaryExpression Convert (Expression expression, Type type, System.Reflection.MethodInfo method) => null;

        public static Expression<TDelegate> Lambda<TDelegate> (Expression body, ParameterExpression[] parameters) => null;
    }

    public abstract class LambdaExpression : Expression
    {}

    public abstract class Expression<T> : LambdaExpression
    {}

    public class ConstantExpression : Expression
    {}

    public class ParameterExpression : Expression
    {}

    public class MethodCallExpression : Expression
    {}

    public sealed class UnaryExpression : Expression
    {}
}
";

            var typeLib_v1_Comp = CreateEmptyCompilation(typeLib_v1, references: [corLib_v1_Comp.ToMetadataReference()], assemblyName: "typeLib");

            typeLib_v1_Comp.VerifyDiagnostics();

            var source1 = @"
#nullable disable

public class Test
{
    public static System.Linq.Expressions.Expression<System.Func<System.Action>> Expression()
    {
        return () => C1.M1;
    }
}

class C1
{
    public static void M1() {}
}
";
            var comp1 = CreateEmptyCompilation(
                source1, references: [corLib_v1_Comp.ToMetadataReference(), typeLib_v1_Comp.ToMetadataReference()],
                parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute());

            comp1.MakeMemberMissing(SpecialMember.System_Reflection_MethodBase__GetMethodFromHandle);
            comp1.MakeMemberMissing(SpecialMember.System_Reflection_MethodBase__GetMethodFromHandle2);
            comp1.MakeMemberMissing(WellKnownMember.System_Reflection_MethodBase__GetMethodFromHandle2);

            var comp1Ref = comp1.EmitToImageReference();

            var corLib_v2 = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Object))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(void))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.ValueType))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.RuntimeTypeHandle))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.RuntimeMethodHandle))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Int32))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Action))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Func<>))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Nullable<>))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Collections.Generic.IEnumerable<>))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Delegate))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.MulticastDelegate))]
";
            var corLib_v2_Comp = CreateCompilation(corLib_v2, assemblyName: "corLib");

            var typeLib_v2 = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Type))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Reflection.MethodBase))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Reflection.MethodInfo))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Linq.Expressions.Expression))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Linq.Expressions.LambdaExpression))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Linq.Expressions.Expression<>))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Linq.Expressions.ConstantExpression))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Linq.Expressions.ParameterExpression))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Linq.Expressions.MethodCallExpression))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Linq.Expressions.UnaryExpression))]
";

            var typeLib_v2_Comp = CreateCompilation(typeLib_v2, assemblyName: "typeLib");

            var source2 = @"
class Program
{
    static void Main()
    {
        System.Console.WriteLine(Test.Expression());
    }
}
";

            var comp = CreateCompilation(source2, references: [corLib_v2_Comp.ToMetadataReference(), typeLib_v2_Comp.ToMetadataReference(), comp1Ref], options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: "() => Convert(Void M1().CreateDelegate(System.Action, null)" +
                                                   (ExecutionConditionUtil.IsMonoOrCoreClr ? ", Action" : "") +
                                                   ")");

            comp1 = CreateEmptyCompilation(
                source1, references: [corLib_v1_Comp.ToMetadataReference(), typeLib_v1_Comp.ToMetadataReference()],
                parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute());

            comp1.MakeMemberMissing(WellKnownMember.System_Reflection_MethodBase__GetMethodFromHandle);
            comp1.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // (8,22): error CS0656: Missing compiler required member 'System.Reflection.MethodBase.GetMethodFromHandle'
                //         return () => C1.M1;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "C1.M1").WithArguments("System.Reflection.MethodBase", "GetMethodFromHandle").WithLocation(8, 22)
                );
        }

        [Fact]
        public void GetMethodFromHandle_WellKnown_02()
        {
            var corLib_v1 = @"
namespace System
{
    public class Object
    {}

    public class Void
    {}

    public class ValueType
    {}

    public struct RuntimeTypeHandle
    {}

    public struct RuntimeMethodHandle
    {}

    public struct Int32
    {}

    public abstract class Delegate
    {}

    public abstract class MulticastDelegate : Delegate
    {}

    public delegate void Action();

    public delegate TResult Func<out TResult>();

    public struct Nullable<T>
    {}
}

namespace System.Collections.Generic
{
    public interface IEnumerable<T>
    {}
}
";
            var corLib_v1_Comp = CreateEmptyCompilation(corLib_v1, assemblyName: "corLib");

            var typeLib_v1 = @"
namespace System
{
    public class Type
    {
        public static Type GetTypeFromHandle(RuntimeTypeHandle handle) => null;
    }
}
namespace System.Reflection
{
    public abstract partial class MethodBase
    {
        public static MethodBase GetMethodFromHandle(RuntimeMethodHandle handle) => null;
        public static MethodBase GetMethodFromHandle(RuntimeMethodHandle handle, RuntimeTypeHandle declaringType) => null;
    }

    public abstract partial class MethodInfo : MethodBase
    {
        public virtual Delegate CreateDelegate(Type delegateType) => null;
        public virtual Delegate CreateDelegate(Type delegateType, object target) => null;
    }
}

namespace System.Linq.Expressions
{
    public abstract class Expression
    {
        public static ConstantExpression Constant (object value) => null;
        public static ConstantExpression Constant (object value, Type type) => null;

        public static MethodCallExpression Call (Expression instance, System.Reflection.MethodInfo method, Expression[] arguments) => null;

        public static UnaryExpression Convert (Expression expression, Type type) => null;
        public static UnaryExpression Convert (Expression expression, Type type, System.Reflection.MethodInfo method) => null;

        public static Expression<TDelegate> Lambda<TDelegate> (Expression body, ParameterExpression[] parameters) => null;
    }

    public abstract class LambdaExpression : Expression
    {}

    public abstract class Expression<T> : LambdaExpression
    {}

    public class ConstantExpression : Expression
    {}

    public class ParameterExpression : Expression
    {}

    public class MethodCallExpression : Expression
    {}

    public sealed class UnaryExpression : Expression
    {}
}
";

            var typeLib_v1_Comp = CreateEmptyCompilation(typeLib_v1, references: [corLib_v1_Comp.ToMetadataReference()], assemblyName: "typeLib");

            typeLib_v1_Comp.VerifyDiagnostics();

            var source1 = @"
#nullable disable

public class Test
{
    public static System.Linq.Expressions.Expression<System.Func<System.Action>> Expression()
    {
        return () => C1<int>.M1;
    }
}

class C1<T>
{
    public static void M1() {}
}
";
            var comp1 = CreateEmptyCompilation(
                source1, references: [corLib_v1_Comp.ToMetadataReference(), typeLib_v1_Comp.ToMetadataReference()],
                parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute());

            comp1.MakeMemberMissing(SpecialMember.System_Reflection_MethodBase__GetMethodFromHandle);
            comp1.MakeMemberMissing(SpecialMember.System_Reflection_MethodBase__GetMethodFromHandle2);

            var comp1Ref = comp1.EmitToImageReference();

            var corLib_v2 = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Object))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(void))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.ValueType))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.RuntimeTypeHandle))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.RuntimeMethodHandle))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Int32))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Action))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Func<>))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Nullable<>))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Collections.Generic.IEnumerable<>))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Delegate))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.MulticastDelegate))]
";
            var corLib_v2_Comp = CreateCompilation(corLib_v2, assemblyName: "corLib");

            var typeLib_v2 = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Type))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Reflection.MethodBase))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Reflection.MethodInfo))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Linq.Expressions.Expression))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Linq.Expressions.LambdaExpression))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Linq.Expressions.Expression<>))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Linq.Expressions.ConstantExpression))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Linq.Expressions.ParameterExpression))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Linq.Expressions.MethodCallExpression))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Linq.Expressions.UnaryExpression))]
";

            var typeLib_v2_Comp = CreateCompilation(typeLib_v2, assemblyName: "typeLib");

            var source2 = @"
class Program
{
    static void Main()
    {
        System.Console.WriteLine(Test.Expression());
    }
}
";

            var comp = CreateCompilation(source2, references: [corLib_v2_Comp.ToMetadataReference(), typeLib_v2_Comp.ToMetadataReference(), comp1Ref], options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: "() => Convert(Void M1().CreateDelegate(System.Action, null)" +
                                                   (ExecutionConditionUtil.IsMonoOrCoreClr ? ", Action" : "") +
                                                   ")");

            comp1 = CreateEmptyCompilation(
                source1, references: [corLib_v1_Comp.ToMetadataReference(), typeLib_v1_Comp.ToMetadataReference()],
                parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute());

            comp1.MakeMemberMissing(WellKnownMember.System_Reflection_MethodBase__GetMethodFromHandle2);
            comp1.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // (8,22): error CS0656: Missing compiler required member 'System.Reflection.MethodBase.GetMethodFromHandle'
                //         return () => C1<int>.M1;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "C1<int>.M1").WithArguments("System.Reflection.MethodBase", "GetMethodFromHandle").WithLocation(8, 22)
                );
        }
    }
}
