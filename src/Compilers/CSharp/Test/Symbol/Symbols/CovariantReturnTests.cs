// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
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
        private static readonly MetadataReference CorelibraryWithCovariantReturnSupport1;
        private static readonly MetadataReference CorelibraryWithCovariantReturnSupport2;
        private static readonly MetadataReference CorelibraryWithoutCovariantReturnSupport1;
        private static readonly MetadataReference CorelibraryWithoutCovariantReturnSupport2;
        private static readonly MetadataReference CorelibraryWithCovariantReturnSupportButWithoutPreserveBaseOverridesAttribute;

        static CovariantReturnTests()
        {
            const string corLibraryCore = @"
namespace System
{
    public class Array
    {
        public static T[] Empty<T>() => throw null;
    }
    public class Attribute { }
    [Flags]
    public enum AttributeTargets
    {
        Assembly = 0x1,
        Module = 0x2,
        Class = 0x4,
        Struct = 0x8,
        Enum = 0x10,
        Constructor = 0x20,
        Method = 0x40,
        Property = 0x80,
        Field = 0x100,
        Event = 0x200,
        Interface = 0x400,
        Parameter = 0x800,
        Delegate = 0x1000,
        ReturnValue = 0x2000,
        GenericParameter = 0x4000,
        All = 0x7FFF
    }
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets validOn) { }
        public bool AllowMultiple
        {
            get => throw null;
            set { }
        }
        public bool Inherited
        {
            get => throw null;
            set { }
        }
        public AttributeTargets ValidOn => throw null;
    }
    public struct Boolean { }
    public struct Byte { }
    public class Delegate
    {
        public static Delegate CreateDelegate(Type type, object firstArgument, Reflection.MethodInfo method) => null;
    }
    public abstract class Enum : IComparable { }
    public class Exception { }
    public class FlagsAttribute : Attribute { }
    public delegate T Func<out T>();
    public delegate U Func<in T, out U>(T arg);
    public interface IComparable { }
    public interface IDisposable
    {
        void Dispose();
    }
    public struct Int16 { }
    public struct Int32 { }
    public struct IntPtr { }
    public class MulticastDelegate : Delegate { }
    public struct Nullable<T> { }
    public class Object { }
    public sealed class ParamArrayAttribute : Attribute { }
    public struct RuntimeMethodHandle { }
    public struct RuntimeTypeHandle { }
    public class String : IComparable { public static String Empty = null; }
    public class Type
    {
        public static Type GetTypeFromHandle(RuntimeTypeHandle handle) => null;
    }
    public class ValueType { }
    public struct Void { }

    namespace Collections
    {
        public interface IEnumerable
        {
            IEnumerator GetEnumerator();
        }
        public interface IEnumerator
        {
            object Current
            {
                get;
            }
            bool MoveNext();
            void Reset();
        }
    }
    namespace Collections.Generic
    {
        public interface IEnumerable<out T> : IEnumerable
        {
            new IEnumerator<T> GetEnumerator();
        }
        public interface IEnumerator<out T> : IEnumerator, IDisposable
        {
            new T Current
            {
                get;
            }
        }
    }
    namespace Linq.Expressions
    {
        public class Expression
        {
            public static ParameterExpression Parameter(Type type) => throw null;
            public static ParameterExpression Parameter(Type type, string name) => throw null;
            public static MethodCallExpression Call(Expression instance, Reflection.MethodInfo method, params Expression[] arguments) => throw null;
            public static Expression<TDelegate> Lambda<TDelegate>(Expression body, params ParameterExpression[] parameters) => throw null;
            public static MemberExpression Property(Expression expression, Reflection.MethodInfo propertyAccessor) => throw null;
            public static ConstantExpression Constant(object value, Type type) => throw null;
            public static UnaryExpression Convert(Expression expression, Type type) => throw null;
        }
        public class ParameterExpression : Expression { }
        public class MethodCallExpression : Expression { }
        public abstract class LambdaExpression : Expression { }
        public class Expression<T> : LambdaExpression { }
        public class MemberExpression : Expression { }
        public class ConstantExpression : Expression { }
        public sealed class UnaryExpression : Expression { }
    }
    namespace Reflection
    {
        public class AssemblyVersionAttribute : Attribute
        {
            public AssemblyVersionAttribute(string version) { }
        }
        public class DefaultMemberAttribute : Attribute
        {
            public DefaultMemberAttribute(string name) { }
        }
        public abstract class MemberInfo { }
        public abstract class MethodBase : MemberInfo
        {
            public static MethodBase GetMethodFromHandle(RuntimeMethodHandle handle) => throw null;
        }
        public abstract class MethodInfo : MethodBase
        {
            public virtual Delegate CreateDelegate(Type delegateType, object? target) => throw null;
        }
    }
    namespace Runtime.CompilerServices
    {
        public static class RuntimeHelpers
        {
            public static object GetObjectValue(object obj) => null;
        }
    }
}
";
            const string corlibWithoutCovariantSupport = corLibraryCore + @"
namespace System.Runtime.CompilerServices
{
    public static class RuntimeFeature
    {
        public const string DefaultImplementationsOfInterfaces = nameof(DefaultImplementationsOfInterfaces);
    }
}
";
            const string corlibWithCovariantSupport = corLibraryCore + @"
namespace System.Runtime.CompilerServices
{
    public static class RuntimeFeature
    {
        public const string CovariantReturnsOfClasses = nameof(CovariantReturnsOfClasses);
        public const string DefaultImplementationsOfInterfaces = nameof(DefaultImplementationsOfInterfaces);
    }
    public sealed class PreserveBaseOverridesAttribute : Attribute { }
}
";
            const string corlibWithCovariantSupportButWithoutPreserveBaseOverridesAttribute = corLibraryCore + @"
namespace System.Runtime.CompilerServices
{
    public static class RuntimeFeature
    {
        public const string CovariantReturnsOfClasses = nameof(CovariantReturnsOfClasses);
        public const string DefaultImplementationsOfInterfaces = nameof(DefaultImplementationsOfInterfaces);
    }
}
";
            CorelibraryWithoutCovariantReturnSupport1 = CreateEmptyCompilation(new string[] {
                corlibWithoutCovariantSupport,
                @"[assembly: System.Reflection.AssemblyVersion(""4.1.0.0"")]"
            }, assemblyName: "mscorlib").EmitToImageReference(options: new CodeAnalysis.Emit.EmitOptions(runtimeMetadataVersion: "v4.1"));
            CorelibraryWithoutCovariantReturnSupport2 = CreateEmptyCompilation(new string[] {
                corlibWithoutCovariantSupport,
                @"[assembly: System.Reflection.AssemblyVersion(""4.2.0.0"")]"
            }, assemblyName: "mscorlib").EmitToImageReference(options: new CodeAnalysis.Emit.EmitOptions(runtimeMetadataVersion: "v4.2"));
            CorelibraryWithCovariantReturnSupport1 = CreateEmptyCompilation(new string[] {
                corlibWithCovariantSupport,
                @"[assembly: System.Reflection.AssemblyVersion(""5.0.0.0"")]"
            }, assemblyName: "mscorlib").EmitToImageReference(options: new CodeAnalysis.Emit.EmitOptions(runtimeMetadataVersion: "v5.0"));
            CorelibraryWithCovariantReturnSupport2 = CreateEmptyCompilation(new string[] {
                corlibWithCovariantSupport,
                @"[assembly: System.Reflection.AssemblyVersion(""5.1.0.0"")]"
            }, assemblyName: "mscorlib").EmitToImageReference(options: new CodeAnalysis.Emit.EmitOptions(runtimeMetadataVersion: "v5.1"));
            CorelibraryWithCovariantReturnSupportButWithoutPreserveBaseOverridesAttribute = CreateEmptyCompilation(new string[] {
                corlibWithCovariantSupportButWithoutPreserveBaseOverridesAttribute,
                @"[assembly: System.Reflection.AssemblyVersion(""4.9.0.0"")]"
            }, assemblyName: "mscorlib").EmitToImageReference(options: new CodeAnalysis.Emit.EmitOptions(runtimeMetadataVersion: "v4.9"));
        }

        private static void VerifyOverride(
            CSharpCompilation comp,
            string methodName,
            string overridingMemberDisplay,
            string overriddenMemberDisplay,
            bool requiresMethodimpl = false)
        {
            var member = comp.GlobalNamespace.GetMember(methodName);
            VerifyOverride(comp, member, overridingMemberDisplay, overriddenMemberDisplay, requiresMethodimpl);
        }

        private static void VerifyOverride(
            CSharpCompilation comp,
            Symbol member,
            string overridingMemberDisplay,
            string overriddenMemberDisplay,
            bool requiresMethodimpl = false)
        {
            Assert.Equal(overridingMemberDisplay, member.ToTestDisplayString());
            var overriddenMember = member.GetOverriddenMember();
            Assert.Equal(overriddenMemberDisplay, overriddenMember?.ToTestDisplayString());
            if (member is MethodSymbol method && overriddenMember is MethodSymbol overriddenMethod)
            {
                Assert.True(method.IsOverride);
                Assert.False(method.IsVirtual);
                Assert.True(method.IsMetadataVirtual(ignoreInterfaceImplementationChanges: true));
                var isCovariant = !method.ReturnType.Equals(overriddenMethod.ReturnType, TypeCompareKind.AllIgnoreOptions);
                var checkMetadata = hasReturnConversion(method.ReturnType, overriddenMethod.ReturnType);
                if (checkMetadata)
                {
                    requiresMethodimpl = isCovariant | requiresMethodimpl;
                    Assert.Equal(requiresMethodimpl, method.IsMetadataNewSlot(ignoreInterfaceImplementationChanges: true));
                    Assert.Equal(requiresMethodimpl, method.RequiresExplicitOverride(out _));
                    if (method.OriginalDefinition is PEMethodSymbol originalMethod &&
                        comp.GetSpecialTypeMember(SpecialMember.System_Runtime_CompilerServices_PreserveBaseOverridesAttribute__ctor) is MethodSymbol attrConstructor)
                    {
                        Assert.Equal(requiresMethodimpl, originalMethod.HasAttribute(attrConstructor));
                    }
                }
                switch (member)
                {
                    case RetargetingMethodSymbol m:
                        {
                            MethodSymbol explicitlyOverriddenClassMethod = m.ExplicitlyOverriddenClassMethod;
                            if (explicitlyOverriddenClassMethod != null)
                            {
                                Assert.True(overriddenMember.Equals(explicitlyOverriddenClassMethod));
                            }
                        }
                        break;
                    case PEMethodSymbol m:
                        {
                            MethodSymbol explicitlyOverriddenClassMethod = m.ExplicitlyOverriddenClassMethod;
                            if (explicitlyOverriddenClassMethod != null)
                            {
                                Assert.True(overriddenMember.Equals(explicitlyOverriddenClassMethod));
                            }
                        }
                        break;
                }
            }
            else if (member is PropertySymbol property && overriddenMember is PropertySymbol overriddenProperty)
            {
                var isCovariant = !property.Type.Equals(overriddenProperty.Type, TypeCompareKind.AllIgnoreOptions);
                if (property.GetMethod is MethodSymbol getMethod && overriddenProperty.GetMethod is MethodSymbol overriddenGetMethod)
                {
                    Assert.True(getMethod.GetOverriddenMember().Equals(overriddenGetMethod));
                    var checkMetadata = hasReturnConversion(property.Type, overriddenProperty.Type);
                    if (checkMetadata)
                    {
                        requiresMethodimpl = isCovariant | requiresMethodimpl;
                        Assert.Equal(requiresMethodimpl, getMethod.IsMetadataNewSlot(ignoreInterfaceImplementationChanges: true));
                        Assert.Equal(requiresMethodimpl, getMethod.RequiresExplicitOverride(out _)); // implies the presence of a methodimpl
                        if (getMethod.OriginalDefinition is PEMethodSymbol originalMethod &&
                            comp.GetSpecialTypeMember(SpecialMember.System_Runtime_CompilerServices_PreserveBaseOverridesAttribute__ctor) is MethodSymbol attrConstructor)
                        {
                            Assert.Equal(requiresMethodimpl, originalMethod.HasAttribute(attrConstructor));
                        }
                    }
                }
                if (property.SetMethod is MethodSymbol setMethod && overriddenProperty.SetMethod is MethodSymbol overriddenSetMethod)
                {
                    Assert.False(setMethod.IsMetadataNewSlot(ignoreInterfaceImplementationChanges: true));
                    Assert.False(setMethod.RequiresExplicitOverride(out _));
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
                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                return comp.Conversions.HasIdentityOrImplicitReferenceConversion(fromType, toType, ref discardedUseSiteInfo);
            }
        }

        private static Symbol MemberOfConstructedType(
            CSharpCompilation comp,
            string memberName,
            string containingTypeName,
            params string[] typeArguments)
        {
            var genericType = (NamedTypeSymbol)comp.GlobalNamespace.GetMember(containingTypeName);
            Assert.Equal(typeArguments.Length, genericType.Arity);
            var constructedType = genericType.Construct(typeArguments.Select(n => (TypeSymbol)comp.GlobalNamespace.GetMember(n)));
            return constructedType.GetMembers(memberName).Single();
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
            }

            Assert.Equal(expectedAssignments, foundAssignments);
        }

        private CSharpCompilation CreateCompilationWithCovariantReturns(
            string source,
            MetadataReference[] references = null,
            string assemblyName = "",
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null)
        {
            parseOptions ??= TestOptions.WithCovariantReturns;
            references = references?.Prepend(CorelibraryWithCovariantReturnSupport1).ToArray() ?? new[] { CorelibraryWithCovariantReturnSupport1 };

            return CreateCompilation(
                source,
                parseOptions: parseOptions ?? TestOptions.Regular9,
                references: references,
                targetFramework: TargetFramework.Empty,
                assemblyName: assemblyName,
                options: options);
        }

        private CSharpCompilation CreateCompilationWithoutCovariantReturns(
            string source,
            MetadataReference[] references = null,
            string assemblyName = "",
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null)
        {
            parseOptions ??= TestOptions.WithoutCovariantReturns;
            references = references?.Prepend(CorelibraryWithoutCovariantReturnSupport1).ToArray() ?? new[] { CorelibraryWithoutCovariantReturnSupport1 };

            return CreateCompilation(
                source,
                parseOptions: parseOptions ?? TestOptions.Regular8,
                references: references,
                targetFramework: TargetFramework.Empty,
                assemblyName: assemblyName,
                options: options);
        }

        private CSharpCompilation CreateCompilation(
            bool withCovariantReturns,
            string source,
            MetadataReference[] references = null,
            string assemblyName = "",
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null)
        {
            return withCovariantReturns
                ? CreateCompilationWithCovariantReturns(source, references, assemblyName, options, parseOptions)
                : CreateCompilationWithoutCovariantReturns(source, references, assemblyName, options, parseOptions);
        }

        private static CSharpCompilation SourceView(
            CSharpCompilation comp,
            string assignments)
        {
            return comp.AddSyntaxTrees(CSharpSyntaxTree.ParseText(assignments, (CSharpParseOptions)comp.SyntaxTrees[0].Options, path: "assignments.cs", encoding: Encoding.UTF8));
        }

        private static CSharpCompilation CompilationReferenceView(
            CSharpCompilation comp,
            string assignments,
            MetadataReference[] references = null,
            bool withoutCorlib = false)
        {
            CompilationReference compAsMetadata = comp.ToMetadataReference();
            references = references?.Append(compAsMetadata) ?? new[] { compAsMetadata };
            var coreLibrary = comp.GetMetadataReference(comp.Assembly.CorLibrary);
            if (!withoutCorlib)
                references = references.Prepend(coreLibrary).ToArray();
            var result = CreateCompilation(assignments, references: references, targetFramework: TargetFramework.Empty);
            result.VerifyDiagnostics();
            var originalCorLib = comp.Assembly.CorLibrary;
            var newCorLib = result.Assembly.CorLibrary;
            Assert.Equal(originalCorLib, newCorLib);
            var sourceAssembly = (AssemblySymbol)result.GetAssemblyOrModuleSymbol(compAsMetadata);
            Assert.True(sourceAssembly is SourceAssemblySymbol);
            return result;
        }

        private static CSharpCompilation MetadataView(
            CSharpCompilation comp,
            string assignments,
            MetadataReference[] references = null,
            bool withoutCorlib = false,
            params DiagnosticDescription[] expectedDiagnostics)
        {
            var compAsImage = comp.EmitToImageReference();
            references = references?.Append(compAsImage) ?? new[] { compAsImage };
            var coreLibrary = comp.GetMetadataReference(comp.Assembly.CorLibrary);
            if (!withoutCorlib)
                references = references.Prepend(coreLibrary).ToArray();
            var result = CreateCompilation(assignments, references: references, targetFramework: TargetFramework.Empty);
            result.VerifyDiagnostics(expectedDiagnostics);
            return result;
        }

        private static CSharpCompilation RetargetingView(
            CSharpCompilation comp,
            string assignments,
            MetadataReference[] references = null,
            bool withoutCorlib = false,
            params DiagnosticDescription[] expectedDiagnostics)
        {
            CompilationReference compAsMetadata = comp.ToMetadataReference();
            references = references?.Append(compAsMetadata) ?? new[] { compAsMetadata };
            if (!withoutCorlib)
            {
                var coreLibrary = comp.GetMetadataReference(comp.Assembly.CorLibrary);
                MetadataReference alternateCorlib =
                    (coreLibrary == CorelibraryWithCovariantReturnSupport1) ? CorelibraryWithCovariantReturnSupport2 :
                    (coreLibrary == CorelibraryWithoutCovariantReturnSupport1) ? CorelibraryWithoutCovariantReturnSupport2 :
                    throw ExceptionUtilities.Unreachable();
                references = references.Prepend(alternateCorlib).ToArray();
            }
            var parseOptions = (CSharpParseOptions)comp.SyntaxTrees[0].Options;
            var result = CreateCompilation(
                assignments,
                references: references,
                targetFramework: TargetFramework.Empty,
                options: TestOptions.ReleaseDll.WithSpecificDiagnosticOptions("CS1701", ReportDiagnostic.Suppress),
                parseOptions: parseOptions);

            result.VerifyDiagnostics(expectedDiagnostics);
            var originalCorLib = comp.Assembly.CorLibrary;
            var newCorLib = result.Assembly.CorLibrary;
            Assert.NotEqual(originalCorLib, newCorLib);
            var retargetingAssembly = (AssemblySymbol)result.GetAssemblyOrModuleSymbol(compAsMetadata);
            Assert.True(retargetingAssembly is RetargetingAssemblySymbol);
            return result;
        }

        [Fact]
        public void RequirePreserveBaseOverridesAttribute()
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
";
            CreateCompilation(
                source,
                parseOptions: TestOptions.WithCovariantReturns,
                references: new[] { CorelibraryWithCovariantReturnSupportButWithoutPreserveBaseOverridesAttribute },
                targetFramework: TargetFramework.Empty)
                .VerifyDiagnostics(
                    // (8,28): error CS8830: 'Derived.M()': Target runtime doesn't support covariant return types in overrides. Return type must be 'object' to match overridden member 'Base.M()'
                    //     public override string M() => null;
                    Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportCovariantReturnsOfClasses, "M").WithArguments("Derived.M()", "Base.M()", "object").WithLocation(8, 28)
                );
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
";
            var assignments = @"
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
            verify(SourceView(comp, assignments));
            verify(CompilationReferenceView(comp, assignments));
            verify(MetadataView(comp, assignments));
            verify(RetargetingView(comp, assignments));

            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(SourceView(comp, assignments));
            verify(CompilationReferenceView(comp, assignments));
            verify(MetadataView(comp, assignments));
            verify(RetargetingView(comp, assignments));
            CompileAndVerify(SourceView(comp, assignments), verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived)", source: assignments, sequencePoints: "Program.M", expectedIL: @"
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
                VerifyAssignments(comp, 2);
            }
        }

        [Fact]
        public void TestCorlibWithCovariantReturnSupport()
        {
            var source = @"
public class Base
{
    public virtual object M1() => null;
    public virtual object P1 => null;
    public virtual object M2() => null;
    public virtual object P2 => null;
    public virtual object this[int index] => null;
}
public class Derived : Base
{
    public override string M1() => null;
    public override string P1 => null;
    public override object M2() => null;
    public override object P2 => null;
    public override string this[int index] => null;
}
public class Derived2 : Base
{
    public override object this[int index] => null;
}
";
            var assignments = @"";

            CreateCompilationWithCovariantReturns(source, parseOptions: TestOptions.WithoutCovariantReturns)
                .VerifyDiagnostics(
                // (12,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string M1() => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M1").WithArguments("covariant returns", "9.0").WithLocation(12, 28),
                // (13,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string P1 => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "P1").WithArguments("covariant returns", "9.0").WithLocation(13, 28),
                // (16,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string this[int index] => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "this").WithArguments("covariant returns", "9.0").WithLocation(16, 28)
                );
            var comp = CreateCompilationWithCovariantReturns(source)
                .VerifyDiagnostics(
                );
            verify(SourceView(comp, assignments));
            verify(CompilationReferenceView(comp, ""));
            verify(MetadataView(comp, ""));
            verify(RetargetingView(comp, ""));

            static void verify(CSharpCompilation comp)
            {
                verifyAttribute((MethodSymbol)comp.GlobalNamespace.GetMember("Derived.M1"), needsAttribute: true);
                verifyAttribute((MethodSymbol)comp.GlobalNamespace.GetMember("Derived.get_P1"), needsAttribute: true);
                verifyAttribute((MethodSymbol)comp.GlobalNamespace.GetMember("Derived.M2"), needsAttribute: false);
                verifyAttribute((MethodSymbol)comp.GlobalNamespace.GetMember("Derived.get_P2"), needsAttribute: false);
                verifyAttribute((MethodSymbol)comp.GlobalNamespace.GetMember("Derived.get_Item"), needsAttribute: true);
                verifyAttribute((MethodSymbol)comp.GlobalNamespace.GetMember("Derived2.get_Item"), needsAttribute: false);
            }

            static void verifyAttribute(MethodSymbol method, bool needsAttribute)
            {
                var isCovariant = !method.ReturnType.Equals(method.OverriddenMethod.ReturnType);
                Assert.Equal(needsAttribute, isCovariant);
                var attributeExpected = isCovariant && !method.Locations[0].IsInSource;
                var attrs = method.GetAttributes("System.Runtime.CompilerServices", "PreserveBaseOverridesAttribute");
                Assert.Equal(attributeExpected, !attrs.IsEmpty());
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
}";
            var assignments = @"
public class Program
{
    void M(Base b, Derived d)
    {
        object s1 = b.M();
        string s2 = d.M();
    }
}
";
            var comp = CreateCompilationWithCovariantReturns(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (8,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string M() => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M").WithArguments("covariant returns", "9.0").WithLocation(8, 28)
                );
            verify(SourceView(comp, assignments));

            // Test against a runtime that does not admit support for covariant returns.
            comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (8,28): error CS8778: 'Derived.M()': Target runtime doesn't support covariant return types in overrides. Return type must be 'object' to match overridden member 'Base.M()'
                //     public override string M() => null;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportCovariantReturnsOfClasses, "M").WithArguments("Derived.M()", "Base.M()", "object").WithLocation(8, 28)
                );
            verify(SourceView(comp, assignments));

            comp = CreateCompilationWithoutCovariantReturns(source, parseOptions: TestOptions.WithCovariantReturns).VerifyDiagnostics(
                // (8,28): error CS8778: 'Derived.M()': Target runtime doesn't support covariant return types in overrides. Return type must be 'object' to match overridden member 'Base.M()'
                //     public override string M() => null;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportCovariantReturnsOfClasses, "M").WithArguments("Derived.M()", "Base.M()", "object").WithLocation(8, 28)
                );
            verify(SourceView(comp, assignments));

            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(SourceView(comp, assignments));
            verify(CompilationReferenceView(comp, assignments));
            verify(MetadataView(comp, assignments));
            verify(RetargetingView(comp, assignments));
            CompileAndVerify(SourceView(comp, assignments), verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived)", source: assignments, sequencePoints: "Program.M", expectedIL: @"
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
                VerifyAssignments(comp, 2);
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
}";
            var assignments = @"
public class Program
{
    void M(Base b, Derived d)
    {
        object s1 = b.M<object, string>();
        string s2 = d.M<object, string>();
    }
}
";
            var comp = CreateCompilationWithCovariantReturns(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (8,23): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override U M<T, U>() => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M").WithArguments("covariant returns", "9.0").WithLocation(8, 23)
                );
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(SourceView(comp, assignments));
            verify(CompilationReferenceView(comp, assignments));
            verify(MetadataView(comp, assignments));
            verify(RetargetingView(comp, assignments));
            CompileAndVerify(SourceView(comp, assignments), verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived)", source: assignments, sequencePoints: "Program.M", expectedIL: @"
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
                VerifyAssignments(comp, 2);
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
";
            var assignments = @"
public class Program
{
    void M(Base<object> b, Derived<object, string> d)
    {
        object s1 = b.M();
        string s2 = d.M();
    }
}
";
            var comp = CreateCompilationWithCovariantReturns(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (8,23): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override U M() => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M").WithArguments("covariant returns", "9.0").WithLocation(8, 23)
                );
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(SourceView(comp, assignments));
            verify(CompilationReferenceView(comp, assignments));
            verify(MetadataView(comp, assignments));
            verify(RetargetingView(comp, assignments));
            CompileAndVerify(SourceView(comp, assignments), verify: Verification.Skipped).VerifyIL("Program.M(Base<object>, Derived<object, string>)", source: assignments, sequencePoints: "Program.M", expectedIL: @"
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
                VerifyOverride(comp, MemberOfConstructedType(comp, "M", "Derived", "System.Object", "System.String"), "System.String Derived<System.Object, System.String>.M()", "System.Object Base<System.Object>.M()");
                VerifyAssignments(comp, 2);
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
";
            var assignments = @"
public class Program
{
    void M(Base b, Derived<Q> d)
    {
        N s1 = b.M();
        Q s2 = d.M();
    }
}
";
            var comp = CreateCompilationWithCovariantReturns(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (9,23): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override T M() => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M").WithArguments("covariant returns", "9.0").WithLocation(9, 23)
                );
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(SourceView(comp, assignments));
            verify(CompilationReferenceView(comp, assignments));
            verify(MetadataView(comp, assignments));
            verify(RetargetingView(comp, assignments));
            CompileAndVerify(SourceView(comp, assignments), verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived<Q>)", source: assignments, sequencePoints: "Program.M", expectedIL: @"
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
                VerifyOverride(comp, MemberOfConstructedType(comp, "M", "Derived", "Q"), "Q Derived<Q>.M()", "N Base.M()");
                VerifyAssignments(comp, 2);
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
";
            var assignments = @"
public class Program
{
    void M(Base b, Derived d)
    {
        object s1 = b.M;
        string s2 = d.M;
    }
}
";
            var comp = CreateCompilationWithCovariantReturns(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (8,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string M => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M").WithArguments("covariant returns", "9.0").WithLocation(8, 28)
                );
            verify(SourceView(comp, assignments));

            // Test against a runtime that does not admit support for covariant returns.
            comp = CreateCompilationWithoutCovariantReturns(source, parseOptions: TestOptions.WithCovariantReturns).VerifyDiagnostics(
                // (8,28): error CS8779: 'Derived.M': Target runtime doesn't support covariant types in overrides. Type must be 'object' to match overridden member 'Base.M'
                //     public override string M => null;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportCovariantPropertiesOfClasses, "M").WithArguments("Derived.M", "Base.M", "object").WithLocation(8, 28)
                );
            verify(SourceView(comp, assignments));

            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(SourceView(comp, assignments));
            verify(CompilationReferenceView(comp, assignments));
            verify(MetadataView(comp, assignments));
            verify(RetargetingView(comp, assignments));
            CompileAndVerify(SourceView(comp, assignments), verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived)", source: assignments, sequencePoints: "Program.M", expectedIL: @"
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
                VerifyAssignments(comp, 2);
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
";
            var assignments = @"
public class Program
{
    void M(Base<object> b, Derived<object, string> d)
    {
        object s1 = b.M;
        string s2 = d.M;
    }
}
";
            var comp = CreateCompilationWithCovariantReturns(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (8,23): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override U M => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M").WithArguments("covariant returns", "9.0").WithLocation(8, 23)
                );
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(SourceView(comp, assignments));
            verify(CompilationReferenceView(comp, assignments));
            verify(MetadataView(comp, assignments));
            verify(RetargetingView(comp, assignments));
            CompileAndVerify(SourceView(comp, assignments), verify: Verification.Skipped).VerifyIL("Program.M(Base<object>, Derived<object, string>)", source: assignments, sequencePoints: "Program.M", expectedIL: @"
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
                VerifyOverride(comp, MemberOfConstructedType(comp, "M", "Derived", "System.Object", "System.String"), "System.String Derived<System.Object, System.String>.M { get; }", "System.Object Base<System.Object>.M { get; }");
                VerifyOverride(comp, MemberOfConstructedType(comp, "get_M", "Derived", "System.Object", "System.String"), "System.String Derived<System.Object, System.String>.M.get", "System.Object Base<System.Object>.M.get");
                VerifyAssignments(comp, 2);
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
";
            var assignments = @"
public class Program
{
    void M(Base b, Derived<Q> d)
    {
        N s1 = b.M;
        Q s2 = d.M;
    }
}
";
            var comp = CreateCompilationWithCovariantReturns(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (9,23): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override T M => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M").WithArguments("covariant returns", "9.0").WithLocation(9, 23)
                );
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(SourceView(comp, assignments));
            verify(CompilationReferenceView(comp, assignments));
            verify(MetadataView(comp, assignments));
            verify(RetargetingView(comp, assignments));
            CompileAndVerify(SourceView(comp, assignments), verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived<Q>)", source: assignments, sequencePoints: "Program.M", expectedIL: @"
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
                VerifyOverride(comp, MemberOfConstructedType(comp, "M", "Derived", "Q"), "Q Derived<Q>.M { get; }", "N Base.M { get; }");
                VerifyOverride(comp, MemberOfConstructedType(comp, "get_M", "Derived", "Q"), "Q Derived<Q>.M.get", "N Base.M.get");
                VerifyAssignments(comp, 2);
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
";
            var assignments = @"
public class Program
{
    void M(Base b, Derived d)
    {
        object s1 = b[0];
        string s2 = d[0];
    }
}
";
            var comp = CreateCompilationWithCovariantReturns(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (8,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string this[int i] => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "this").WithArguments("covariant returns", "9.0").WithLocation(8, 28)
                );
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(SourceView(comp, assignments));
            verify(CompilationReferenceView(comp, assignments));
            verify(MetadataView(comp, assignments));
            verify(RetargetingView(comp, assignments));
            CompileAndVerify(SourceView(comp, assignments), verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived)", source: assignments, sequencePoints: "Program.M", expectedIL: @"
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
                VerifyAssignments(comp, 2);
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
";
            var assignments = @"
public class Program
{
    void M(Base<object> b, Derived<object, string> d)
    {
        object s1 = b[0];
        string s2 = d[0];
    }
}
";
            var comp = CreateCompilationWithCovariantReturns(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (8,23): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override U this[int i] => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "this").WithArguments("covariant returns", "9.0").WithLocation(8, 23)
                );
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(SourceView(comp, assignments));
            verify(CompilationReferenceView(comp, assignments));
            verify(MetadataView(comp, assignments));
            verify(RetargetingView(comp, assignments));
            CompileAndVerify(SourceView(comp, assignments), verify: Verification.Skipped).VerifyIL("Program.M(Base<object>, Derived<object, string>)", source: assignments, sequencePoints: "Program.M", expectedIL: @"
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
                VerifyOverride(comp, MemberOfConstructedType(comp, "this[]", "Derived", "System.Object", "System.String"), "System.String Derived<System.Object, System.String>.this[System.Int32 i] { get; }", "System.Object Base<System.Object>.this[System.Int32 i] { get; }");
                VerifyOverride(comp, MemberOfConstructedType(comp, "get_Item", "Derived", "System.Object", "System.String"), "System.String Derived<System.Object, System.String>.this[System.Int32 i].get", "System.Object Base<System.Object>.this[System.Int32 i].get");

                VerifyAssignments(comp, 2);
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
";
            var assignments = @"
public class Program
{
    void M(Base b, Derived<Q> d)
    {
        N s1 = b[0];
        Q s2 = d[0];
    }
}
";
            var comp = CreateCompilationWithCovariantReturns(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                    // (9,23): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                    //     public override T this[int i] => null;
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "this").WithArguments("covariant returns", "9.0").WithLocation(9, 23)
                );
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(SourceView(comp, assignments));
            verify(CompilationReferenceView(comp, assignments));
            verify(MetadataView(comp, assignments));
            verify(RetargetingView(comp, assignments));
            CompileAndVerify(SourceView(comp, assignments), verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived<Q>)", source: assignments, sequencePoints: "Program.M", expectedIL: @"
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
                VerifyOverride(comp, MemberOfConstructedType(comp, "this[]", "Derived", "Q"), "Q Derived<Q>.this[System.Int32 i] { get; }", "N Base.this[System.Int32 i] { get; }");
                VerifyOverride(comp, MemberOfConstructedType(comp, "get_Item", "Derived", "Q"), "Q Derived<Q>.this[System.Int32 i].get", "N Base.this[System.Int32 i].get");
                VerifyAssignments(comp, 2);
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
            var assignments = @"";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                // (10,40): error CS1715: 'Derived.E': type must be 'Func<object>' to match overridden member 'Base.E'
                //     public override event Func<string> E;
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "E").WithArguments("Derived.E", "Base.E", "System.Func<object>").WithLocation(10, 40)
                );
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                // (10,40): error CS1715: 'Derived.E': type must be 'Func<object>' to match overridden member 'Base.E'
                //     public override event Func<string> E;
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "E").WithArguments("Derived.E", "Base.E", "System.Func<object>").WithLocation(10, 40)
                );
            verify(SourceView(comp, assignments));

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
";
            var assignments = @"
using System;
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
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                // (9,34): error CS1715: 'Derived.P': type must be 'Func<object>' to match overridden member 'Base.P'
                //     public override Func<string> P { get; set; }
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "P").WithArguments("Derived.P", "Base.P", "System.Func<object>").WithLocation(9, 34)
                );
            verify(SourceView(comp, assignments));

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.P", "System.Func<System.String> Derived.P { get; set; }", "System.Func<System.Object> Base.P { get; set; }");
                VerifyNoOverride(comp, "Derived.set_P");
                VerifyOverride(comp, "Derived.get_P", "System.Func<System.String> Derived.P.get", "System.Func<System.Object> Base.P.get");
                VerifyAssignments(comp, 2);
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
            var baseMetadata = CreateCompilationWithCovariantReturns(s0).EmitToImageReference();
            var source = @"
public class Derived : Base
{
    public override string M() => null;
}
";
            var assignments = @"
public class Program
{
    void M(Base b, Derived d)
    {
        object s1 = b.M();
        string s2 = d.M();
    }
}
";
            var comp = CreateCompilationWithCovariantReturns(source, references: new[] { baseMetadata }, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (4,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string M() => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M").WithArguments("covariant returns", "9.0").WithLocation(4, 28)
                );
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source, references: new[] { baseMetadata }).VerifyDiagnostics(
                );
            verify(SourceView(comp, assignments));
            verify(CompilationReferenceView(comp, assignments, new[] { baseMetadata }));
            verify(MetadataView(comp, assignments, new[] { baseMetadata }));
            verify(RetargetingView(comp, assignments, new[] { baseMetadata }));
            CompileAndVerify(SourceView(comp, assignments), verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived)", source: assignments, sequencePoints: "Program.M", expectedIL: @"
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
                VerifyAssignments(comp, 2);
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
            var baseMetadata = CreateCompilationWithCovariantReturns(s0).EmitToImageReference();
            var source = @"
public class Derived : Base
{
    public override string M => null;
}
";
            var assignments = @"
public class Program
{
    void M(Base b, Derived d)
    {
        object s1 = b.M;
        string s2 = d.M;
    }
}
";
            var references = new[] { baseMetadata };
            var comp = CreateCompilationWithCovariantReturns(source, references: references, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (4,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string M => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M").WithArguments("covariant returns", "9.0").WithLocation(4, 28)
                );
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source, references: references).VerifyDiagnostics(
                );
            verify(SourceView(comp, assignments));
            verify(CompilationReferenceView(comp, assignments, references));
            verify(MetadataView(comp, assignments, references));
            verify(RetargetingView(comp, assignments, references));
            CompileAndVerify(SourceView(comp, assignments), verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived)", source: assignments, sequencePoints: "Program.M", expectedIL: @"
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
                VerifyAssignments(comp, 2);
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
            var baseMetadata = CreateCompilationWithCovariantReturns(s0).EmitToImageReference();
            var source = @"
public class Derived : Base
{
    public override string this[int i] => null;
}
";
            var assignments = @"
public class Program
{
    void M(Base b, Derived d)
    {
        object s1 = b[0];
        string s2 = d[0];
    }
}
";
            var references = new[] { baseMetadata };
            var comp = CreateCompilationWithCovariantReturns(source, references: references, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (4,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string this[int i] => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "this").WithArguments("covariant returns", "9.0").WithLocation(4, 28)
                );
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source, references: new[] { baseMetadata }).VerifyDiagnostics(
                );
            verify(SourceView(comp, assignments));
            verify(CompilationReferenceView(comp, assignments, references));
            verify(MetadataView(comp, assignments, references));
            verify(RetargetingView(comp, assignments, references));

            var c = CompileAndVerify(SourceView(comp, assignments), verify: Verification.Skipped);

            c.VerifyMethodBody("Program.M(Base, Derived)", @"
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
                VerifyAssignments(comp, 2);
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
";
            var assignments = @"
public class Program
{
    void M(Base b, Derived d)
    {
        object s1 = b.M();
        string s2 = d.M();
    }
}
";
            var comp = CreateCompilationWithCovariantReturns(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (8,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string M() => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M").WithArguments("covariant returns", "9.0").WithLocation(8, 28)
                );
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(SourceView(comp, assignments));
            verify(CompilationReferenceView(comp, assignments));
            verify(MetadataView(comp, assignments));
            verify(RetargetingView(comp, assignments));
            CompileAndVerify(SourceView(comp, assignments), verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived)", source: assignments, sequencePoints: "Program.M", expectedIL: @"
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
                VerifyAssignments(comp, 2);
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
";
            var assignments = @"
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
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                // (8,28): error CS0508: 'Derived.M()': return type must be 'string' to match overridden member 'Base.M()'
                //     public override object M() => null;
                Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "M").WithArguments("Derived.M()", "Base.M()", "string").WithLocation(8, 28)
                );
            verify(SourceView(comp, assignments));

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M", "System.Object Derived.M()", "System.String Base.M()");
                VerifyAssignments(comp, 2);
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
";
            var assignments = @"
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
            verify(SourceView(comp, assignments));
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
            verify(SourceView(comp, assignments));

            static void verify(CSharpCompilation comp)
            {
                VerifyNoOverride(comp, "Derived.M1");
                VerifyNoOverride(comp, "Derived.M2");
                VerifyNoOverride(comp, "Derived2.M1");
                VerifyNoOverride(comp, "Derived2.M2");
                VerifyNoOverride(comp, "Derived3.M1");
                VerifyNoOverride(comp, "Derived3.M2");
                VerifyAssignments(comp, 8);
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
";
            var assignments = @"
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
            var comp = CreateCompilationWithCovariantReturns(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (10,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string M1 => null; // A
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M1").WithArguments("covariant returns", "9.0").WithLocation(10, 28),
                // (11,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string M2 => null; // B
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M2").WithArguments("covariant returns", "9.0").WithLocation(11, 28),
                // (12,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string M3 => null; // C
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M3").WithArguments("covariant returns", "9.0").WithLocation(12, 28),
                // (17,28): error CS1715: 'Derived2.M2': type must be 'string' to match overridden member 'Derived.M2'
                //     public override object M2 => null; // 1
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "M2").WithArguments("Derived2.M2", "Derived.M2", "string").WithLocation(17, 28),
                // (18,26): error CS1715: 'Derived2.M3': type must be 'string' to match overridden member 'Derived.M3'
                //     public override Base M3 => null;   // 2
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "M3").WithArguments("Derived2.M3", "Derived.M3", "string").WithLocation(18, 26)
                );
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                // (17,28): error CS1715: 'Derived2.M2': type must be 'string' to match overridden member 'Derived.M2'
                //     public override object M2 => null; // 1
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "M2").WithArguments("Derived2.M2", "Derived.M2", "string").WithLocation(17, 28),
                // (18,26): error CS1715: 'Derived2.M3': type must be 'string' to match overridden member 'Derived.M3'
                //     public override Base M3 => null;   // 2
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "M3").WithArguments("Derived2.M3", "Derived.M3", "string").WithLocation(18, 26)
                );
            verify(SourceView(comp, assignments));

            static void verify(CSharpCompilation comp)
            {
                VerifyAssignments(comp, 9);

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
";
            var assignments = @"
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
            var comp = CreateCompilationWithCovariantReturns(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (9,33): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override IIn<object> M1 => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M1").WithArguments("covariant returns", "9.0").WithLocation(9, 33),
                // (10,34): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override IOut<string> M2 => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M2").WithArguments("covariant returns", "9.0").WithLocation(10, 34)
                );
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(SourceView(comp, assignments));
            verify(CompilationReferenceView(comp, assignments));
            verify(MetadataView(comp, assignments));
            verify(RetargetingView(comp, assignments));
            CompileAndVerify(SourceView(comp, assignments), verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived)", source: assignments, sequencePoints: "Program.M", expectedIL: @"
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
                VerifyAssignments(comp, 4);
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
";
            var assignments = @"
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
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                // (9,33): error CS1715: 'Derived.M1': type must be 'IIn<object>' to match overridden member 'Base.M1'
                //     public override IIn<string> M1 => null;
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "M1").WithArguments("Derived.M1", "Base.M1", "IIn<object>").WithLocation(9, 33),
                // (10,34): error CS1715: 'Derived.M2': type must be 'IOut<string>' to match overridden member 'Base.M2'
                //     public override IOut<object> M2 => null;
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "M2").WithArguments("Derived.M2", "Base.M2", "IOut<string>").WithLocation(10, 34)
                );
            verify(SourceView(comp, assignments));

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M1", "IIn<System.String> Derived.M1 { get; }", "IIn<System.Object> Base.M1 { get; }");
                VerifyOverride(comp, "Derived.get_M1", "IIn<System.String> Derived.M1.get", "IIn<System.Object> Base.M1.get");
                VerifyOverride(comp, "Derived.M2", "IOut<System.Object> Derived.M2 { get; }", "IOut<System.String> Base.M2 { get; }");
                VerifyOverride(comp, "Derived.get_M2", "IOut<System.Object> Derived.M2.get", "IOut<System.String> Base.M2.get");
                VerifyAssignments(comp, 4);
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
";
            var assignments = @"
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
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                // (9,27): error CS1715: 'Derived.M1': type must be 'int' to match overridden member 'Base.M1'
                //     public override short M1 => 1;
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "M1").WithArguments("Derived.M1", "Base.M1", "int").WithLocation(9, 27),
                // (10,23): error CS1715: 'Derived.M2': type must be 'A' to match overridden member 'Base.M2'
                //     public override B M2 => null;
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "M2").WithArguments("Derived.M2", "Base.M2", "A").WithLocation(10, 23)
                );
            verify(SourceView(comp, assignments));

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M1", "System.Int16 Derived.M1 { get; }", "System.Int32 Base.M1 { get; }");
                VerifyOverride(comp, "Derived.get_M1", "System.Int16 Derived.M1.get", "System.Int32 Base.M1.get");
                VerifyOverride(comp, "Derived.M2", "B Derived.M2 { get; }", "A Base.M2 { get; }");
                VerifyOverride(comp, "Derived.get_M2", "B Derived.M2.get", "A Base.M2.get");
                VerifyAssignments(comp, 4);
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
";
            var assignments = @"
public class Program
{
    void M(Base b, Derived d)
    {
        System.IComparable x1 = b.M;
        string x2 = d.M;
    }
}
";
            var comp = CreateCompilationWithCovariantReturns(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (8,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string M => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M").WithArguments("covariant returns", "9.0").WithLocation(8, 28)
                );
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(SourceView(comp, assignments));
            verify(CompilationReferenceView(comp, assignments));
            verify(MetadataView(comp, assignments));
            verify(RetargetingView(comp, assignments));
            CompileAndVerify(SourceView(comp, assignments), verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived)", source: assignments, sequencePoints: "Program.M", expectedIL: @"
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
                VerifyAssignments(comp, 2);
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
";
            var assignments = @"
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
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
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
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
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
            verify(SourceView(comp, assignments));

            static void verify(CSharpCompilation comp)
            {
                VerifyNoOverride(comp, "Derived.Base.M1");
                VerifyNoOverride(comp, "Derived.Base.M2");
                VerifyNoOverride(comp, "C.Base.M1");
                VerifyNoOverride(comp, "C.Base.M2");
                VerifyAssignments(comp, 4);
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
";
            var assignments = @"
public class Program
{
    void M(Base b, Derived d)
    {
        object x1 = b.P;
        string x2 = d.P;
    }
}
";
            var comp = CreateCompilationWithCovariantReturns(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (8,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string P { get => string.Empty; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "P").WithArguments("covariant returns", "9.0").WithLocation(8, 28)
                );
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(SourceView(comp, assignments));
            verify(CompilationReferenceView(comp, assignments));
            verify(MetadataView(comp, assignments));
            verify(RetargetingView(comp, assignments));
            CompileAndVerify(SourceView(comp, assignments), verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived)", source: assignments, sequencePoints: "Program.M", expectedIL: @"
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
                VerifyAssignments(comp, 2);
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
";
            var assignments = @"
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
            var comp = CreateCompilationWithCovariantReturns(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (8,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string P { get => string.Empty; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "P").WithArguments("covariant returns", "9.0").WithLocation(8, 28),
                // (12,53): error CS0546: 'Derived2.P.set': cannot override because 'Derived.P' does not have an overridable set accessor
                //     public override string P { get => string.Empty; set { } }
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "set").WithArguments("Derived2.P.set", "Derived.P").WithLocation(12, 53)
                );
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                // (12,53): error CS0546: 'Derived2.P.set': cannot override because 'Derived.P' does not have an overridable set accessor
                //     public override string P { get => string.Empty; set { } }
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "set").WithArguments("Derived2.P.set", "Derived.P").WithLocation(12, 53)
                );
            verify(SourceView(comp, assignments));

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.P", "System.String Derived.P { get; }", "System.Object Base.P { get; set; }");
                VerifyOverride(comp, "Derived.get_P", "System.String Derived.P.get", "System.Object Base.P.get");
                VerifyOverride(comp, "Derived2.P", "System.String Derived2.P { get; set; }", "System.String Derived.P { get; }");
                VerifyOverride(comp, "Derived2.get_P", "System.String Derived2.P.get", "System.String Derived.P.get");
                VerifyNoOverride(comp, "Derived2.set_P");
                VerifyAssignments(comp, 3);
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
";
            var assignments = @"
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
            var comp = CreateCompilationWithCovariantReturns(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (8,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string P { get => string.Empty; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "P").WithArguments("covariant returns", "9.0").WithLocation(8, 28),
                // (12,32): error CS0546: 'Derived2.P.set': cannot override because 'Derived.P' does not have an overridable set accessor
                //     public override string P { set { } }
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "set").WithArguments("Derived2.P.set", "Derived.P").WithLocation(12, 32)
                );
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                // (12,32): error CS0546: 'Derived2.P.set': cannot override because 'Derived.P' does not have an overridable set accessor
                //     public override string P { set { } }
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "set").WithArguments("Derived2.P.set", "Derived.P").WithLocation(12, 32)
                );
            verify(SourceView(comp, assignments));

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.P", "System.String Derived.P { get; }", "System.Object Base.P { get; set; }");
                VerifyOverride(comp, "Derived.get_P", "System.String Derived.P.get", "System.Object Base.P.get");
                VerifyOverride(comp, "Derived2.P", "System.String Derived2.P { set; }", "System.String Derived.P { get; }");
                VerifyNoOverride(comp, "Derived2.set_P");
                VerifyAssignments(comp, 3);
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
";
            var assignments = @"
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
            var comp = CreateCompilationWithCovariantReturns(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (8,40): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override System.IComparable P { get => string.Empty; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "P").WithArguments("covariant returns", "9.0").WithLocation(8, 40),
                // (12,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string P { get => string.Empty; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "P").WithArguments("covariant returns", "9.0").WithLocation(12, 28)
                );
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(SourceView(comp, assignments));
            verify(CompilationReferenceView(comp, assignments));
            verify(MetadataView(comp, assignments));
            verify(RetargetingView(comp, assignments));
            CompileAndVerify(SourceView(comp, assignments), verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived, Derived2)", source: assignments, sequencePoints: "Program.M", expectedIL: @"
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
                VerifyAssignments(comp, 3);
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
";
            var assignments = @"
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
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                // (9,28): error CS0462: The inherited members 'Base<T>.M(string)' and 'Base<T>.M(T)' have the same signature in type 'Derived', so they cannot be overridden
                //     public override string M(string s) => null;
                Diagnostic(ErrorCode.ERR_AmbigOverride, "M").WithArguments("Base<T>.M(string)", "Base<T>.M(T)", "Derived").WithLocation(9, 28)
                );
            verify(SourceView(comp, assignments));

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M", "System.String Derived.M(System.String s)", "System.Object Base<System.String>.M(System.String s)");
                VerifyAssignments(comp, 2);
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
";
            var assignments = @"
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
            var comp = CreateCompilationWithCovariantReturns(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (8,49): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public abstract override System.IComparable M();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M").WithArguments("covariant returns", "9.0").WithLocation(8, 49),
                // (12,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string M() => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M").WithArguments("covariant returns", "9.0").WithLocation(12, 28)
                );
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(SourceView(comp, assignments));
            verify(CompilationReferenceView(comp, assignments));
            verify(MetadataView(comp, assignments));
            verify(RetargetingView(comp, assignments));
            CompileAndVerify(SourceView(comp, assignments), verify: Verification.Skipped).VerifyIL("Program.M(Base, Derived, Derived2)", source: assignments, sequencePoints: "Program.M", expectedIL: @"
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
                VerifyAssignments(comp, 3);
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
Imports System
Imports System.Linq.Expressions
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
        Dim x10 As String = MyBase.M()
        Dim x11 As String = MyBase.P
        Dim x12 As Func(Of Object) = AddressOf b.M
        Dim x13 As Func(Of String) = AddressOf MyBase.M
        Dim x14 As Expression(Of Func(Of Derived, String)) = Function(x As Derived) x.M()
        Dim x15 As Expression(Of Func(Of Derived, Func(Of String))) = Function(x As Derived) AddressOf x.M
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
                    switch (count)
                    {
                        case 14:
                            Assert.Null(typeInfo.Type);
                            Assert.Equal("System.Linq.Expressions.Expression(Of System.Func(Of Derived, String))", typeInfo.ConvertedType.ToDisplayString());
                            break;
                        case 15:
                            Assert.Null(typeInfo.Type);
                            Assert.Equal("System.Linq.Expressions.Expression(Of System.Func(Of Derived, System.Func(Of String)))", typeInfo.ConvertedType.ToDisplayString());
                            break;
                        default:
                            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
                            break;
                    }
                }
            }

            Assert.Equal(15, count);

            CompileAndVerify(vbComp, verify: Verification.Skipped).VerifyIL("Derived2.T(Base, Derived, Derived2)", source: vbSource, sequencePoints: "Derived2.T", expectedIL: @"
{
  // Code size      360 (0x168)
  .maxstack  7
  .locals init (Object V_0, //x1
                Object V_1, //x2
                Object V_2, //x3
                String V_3, //x4
                String V_4, //x5
                String V_5, //x6
                String V_6, //x7
                String V_7, //x8
                String V_8, //x9
                String V_9, //x10
                String V_10, //x11
                System.Func(Of Object) V_11, //x12
                System.Func(Of String) V_12, //x13
                System.Linq.Expressions.Expression(Of System.Func(Of Derived, String)) V_13, //x14
                System.Linq.Expressions.Expression(Of System.Func(Of Derived, System.Func(Of String))) V_14, //x15
                System.Linq.Expressions.ParameterExpression V_15)
  // sequence point: Public Sub T(b as Base, d as Derived, d2 as Derived2)
  IL_0000:  nop
  // sequence point: x1 As Object = b.M()
  IL_0001:  ldarg.1
  IL_0002:  callvirt   ""Function Base.M() As Object""
  IL_0007:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
  IL_000c:  stloc.0
  // sequence point: x2 As Object = b.P
  IL_000d:  ldarg.1
  IL_000e:  callvirt   ""Function Base.get_P() As Object""
  IL_0013:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
  IL_0018:  stloc.1
  // sequence point: x3 As Object = b(0)
  IL_0019:  ldarg.1
  IL_001a:  ldc.i4.0
  IL_001b:  callvirt   ""Function Base.get_Item(Integer) As Object""
  IL_0020:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
  IL_0025:  stloc.2
  // sequence point: x4 As String = d.M()
  IL_0026:  ldarg.2
  IL_0027:  callvirt   ""Function Derived.M() As String""
  IL_002c:  stloc.3
  // sequence point: x5 As String = d.P
  IL_002d:  ldarg.2
  IL_002e:  callvirt   ""Function Derived.get_P() As String""
  IL_0033:  stloc.s    V_4
  // sequence point: x6 As String = d(0)
  IL_0035:  ldarg.2
  IL_0036:  ldc.i4.0
  IL_0037:  callvirt   ""Function Derived.get_Item(Integer) As String""
  IL_003c:  stloc.s    V_5
  // sequence point: x7 As String = d2.M()
  IL_003e:  ldarg.3
  IL_003f:  callvirt   ""Function Derived2.M() As String""
  IL_0044:  stloc.s    V_6
  // sequence point: x8 As String = d2.P
  IL_0046:  ldarg.3
  IL_0047:  callvirt   ""Function Derived2.get_P() As String""
  IL_004c:  stloc.s    V_7
  // sequence point: x9 As String = d2(0)
  IL_004e:  ldarg.3
  IL_004f:  ldc.i4.0
  IL_0050:  callvirt   ""Function Derived2.get_Item(Integer) As String""
  IL_0055:  stloc.s    V_8
  // sequence point: x10 As String = MyBase.M()
  IL_0057:  ldarg.0
  IL_0058:  call       ""Function Derived.M() As String""
  IL_005d:  stloc.s    V_9
  // sequence point: x11 As String = MyBase.P
  IL_005f:  ldarg.0
  IL_0060:  call       ""Function Derived.get_P() As String""
  IL_0065:  stloc.s    V_10
  // sequence point: x12 As Func(Of Object) = AddressOf b.M
  IL_0067:  ldarg.1
  IL_0068:  dup
  IL_0069:  ldvirtftn  ""Function Base.M() As Object""
  IL_006f:  newobj     ""Sub System.Func(Of Object)..ctor(Object, System.IntPtr)""
  IL_0074:  stloc.s    V_11
  // sequence point: x13 As Func(Of String) = AddressOf MyBase.M
  IL_0076:  ldarg.0
  IL_0077:  ldftn      ""Function Derived.M() As String""
  IL_007d:  newobj     ""Sub System.Func(Of String)..ctor(Object, System.IntPtr)""
  IL_0082:  stloc.s    V_12
  // sequence point: x14 As Expression(Of Func(Of Derived, String)) = Function(x As Derived) x.M()
  IL_0084:  ldtoken    ""Derived""
  IL_0089:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_008e:  ldstr      ""x""
  IL_0093:  call       ""Function System.Linq.Expressions.Expression.Parameter(System.Type, String) As System.Linq.Expressions.ParameterExpression""
  IL_0098:  stloc.s    V_15
  IL_009a:  ldloc.s    V_15
  IL_009c:  ldtoken    ""Function Derived.M() As String""
  IL_00a1:  call       ""Function System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle) As System.Reflection.MethodBase""
  IL_00a6:  castclass  ""System.Reflection.MethodInfo""
  IL_00ab:  ldc.i4.0
  IL_00ac:  newarr     ""System.Linq.Expressions.Expression""
  IL_00b1:  call       ""Function System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression, System.Reflection.MethodInfo, ParamArray System.Linq.Expressions.Expression()) As System.Linq.Expressions.MethodCallExpression""
  IL_00b6:  ldc.i4.1
  IL_00b7:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_00bc:  dup
  IL_00bd:  ldc.i4.0
  IL_00be:  ldloc.s    V_15
  IL_00c0:  stelem.ref
  IL_00c1:  call       ""Function System.Linq.Expressions.Expression.Lambda(Of System.Func(Of Derived, String))(System.Linq.Expressions.Expression, ParamArray System.Linq.Expressions.ParameterExpression()) As System.Linq.Expressions.Expression(Of System.Func(Of Derived, String))""
  IL_00c6:  stloc.s    V_13
  // sequence point: x15 As Expression(Of Func(Of Derived, Func(Of String))) = Function(x As Derived) AddressOf x.M
  IL_00c8:  ldtoken    ""Derived""
  IL_00cd:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_00d2:  ldstr      ""x""
  IL_00d7:  call       ""Function System.Linq.Expressions.Expression.Parameter(System.Type, String) As System.Linq.Expressions.ParameterExpression""
  IL_00dc:  stloc.s    V_15
  IL_00de:  ldtoken    ""Function Derived.M() As String""
  IL_00e3:  call       ""Function System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle) As System.Reflection.MethodBase""
  IL_00e8:  castclass  ""System.Reflection.MethodInfo""
  IL_00ed:  ldtoken    ""System.Reflection.MethodInfo""
  IL_00f2:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_00f7:  call       ""Function System.Linq.Expressions.Expression.Constant(Object, System.Type) As System.Linq.Expressions.ConstantExpression""
  IL_00fc:  ldtoken    ""Function System.Reflection.MethodInfo.CreateDelegate(System.Type, Object) As System.Delegate""
  IL_0101:  call       ""Function System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle) As System.Reflection.MethodBase""
  IL_0106:  castclass  ""System.Reflection.MethodInfo""
  IL_010b:  ldc.i4.2
  IL_010c:  newarr     ""System.Linq.Expressions.Expression""
  IL_0111:  dup
  IL_0112:  ldc.i4.0
  IL_0113:  ldtoken    ""System.Func(Of String)""
  IL_0118:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_011d:  ldtoken    ""System.Type""
  IL_0122:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_0127:  call       ""Function System.Linq.Expressions.Expression.Constant(Object, System.Type) As System.Linq.Expressions.ConstantExpression""
  IL_012c:  stelem.ref
  IL_012d:  dup
  IL_012e:  ldc.i4.1
  IL_012f:  ldloc.s    V_15
  IL_0131:  ldtoken    ""Object""
  IL_0136:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_013b:  call       ""Function System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression, System.Type) As System.Linq.Expressions.UnaryExpression""
  IL_0140:  stelem.ref
  IL_0141:  call       ""Function System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression, System.Reflection.MethodInfo, ParamArray System.Linq.Expressions.Expression()) As System.Linq.Expressions.MethodCallExpression""
  IL_0146:  ldtoken    ""System.Func(Of String)""
  IL_014b:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_0150:  call       ""Function System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression, System.Type) As System.Linq.Expressions.UnaryExpression""
  IL_0155:  ldc.i4.1
  IL_0156:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_015b:  dup
  IL_015c:  ldc.i4.0
  IL_015d:  ldloc.s    V_15
  IL_015f:  stelem.ref
  IL_0160:  call       ""Function System.Linq.Expressions.Expression.Lambda(Of System.Func(Of Derived, System.Func(Of String)))(System.Linq.Expressions.Expression, ParamArray System.Linq.Expressions.ParameterExpression()) As System.Linq.Expressions.Expression(Of System.Func(Of Derived, System.Func(Of String)))""
  IL_0165:  stloc.s    V_14
  // sequence point: End Sub
  IL_0167:  ret
}
");
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
            var ref1a = CreateCompilationWithoutCovariantReturns(
                s1a,
                references: new[] { ref0 },
                assemblyName: "ref1").EmitToImageReference();

            var s1b = @"
public class Mid : Base
{
    public override string M() => null;
}
";
            var ref1b = CreateCompilationWithCovariantReturns(
                s1b,
                references: new[] { ref0 },
                assemblyName: "ref1").EmitToImageReference();

            var s2 = @"
public class Derived : Mid
{
    public override string M() => null;
}
";
            var assignments1 = @"
public class Program
{
    void M(Base b, Mid m, Derived d)
    {
        object x1 = b.M();
        object x2 = m.M();
        string x3 = d.M();
    }
}
";
            var assignments2 = @"
public class Program
{
    void M(Base b, Mid m, Derived d)
    {
        object x1 = b.M();
        string x2 = m.M();
        string x3 = d.M();
    }
}
";

            var references = new[] { ref0, ref1a };
            var comp = CreateCompilationWithCovariantReturns(s2, references, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (4,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string M() => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M").WithArguments("covariant returns", "9.0").WithLocation(4, 28)
                );
            verify1(SourceView(comp, assignments1));

            comp = CreateCompilationWithCovariantReturns(s2, references).VerifyDiagnostics(
                );
            verify1(SourceView(comp, assignments1));
            verify1(CompilationReferenceView(comp, assignments1, references));
            verify1(MetadataView(comp, assignments1, references));
            verify1(RetargetingView(comp, assignments1, references));

            references = new[] { ref0, ref1b };
            // we do not test CompilationReferenceView because the changed reference would cause us to retarget

            verify2(MetadataView(comp, assignments2, references));
            verify2(RetargetingView(comp, assignments2, references));

            static void verify1(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M", "System.String Derived.M()", "System.Object Base.M()");
            }

            static void verify2(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M", "System.String Derived.M()", "System.Object Base.M()");
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
            var assignments = "";
            var references = new[] { ref0, ref1a };
            var comp = CreateCompilationWithCovariantReturns(s2, references, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (4,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string P => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "P").WithArguments("covariant returns", "9.0").WithLocation(4, 28)
                );
            verify1(comp);

            comp = CreateCompilationWithCovariantReturns(s2, references).VerifyDiagnostics(
                );
            verify1(comp);
            verify1(CompilationReferenceView(comp, assignments, references));
            verify1(MetadataView(comp, assignments, references));
            verify1(RetargetingView(comp, assignments, references));

            references = new[] { ref0, ref1b };
            // we do not test CompilationReferenceView because the changed reference would cause us to retarget
            verify2(MetadataView(comp, assignments, references));
            verify2(RetargetingView(comp, assignments, references));

            static void verify1(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.P", "System.String Derived.P { get; }", "System.Object Base.P { get; }");
                VerifyOverride(comp, "Derived.get_P", "System.String Derived.P.get", "System.Object Base.P.get");
            }

            static void verify2(CSharpCompilation comp)
            {
                verify1(comp);
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
            var assignments = "";
            var references = new[] { ref0, ref1a };
            var comp = CreateCompilationWithCovariantReturns(s2, references, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (4,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string M() => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M").WithArguments("covariant returns", "9.0").WithLocation(4, 28)
                );
            verify1(comp);

            comp = CreateCompilationWithCovariantReturns(s2, references).VerifyDiagnostics(
                );
            verify1(comp);
            verify1(CompilationReferenceView(comp, assignments, references));
            verify1(MetadataView(comp, assignments, references));
            verify1(RetargetingView(comp, assignments, references));

            references = new[] { ref0, ref1b };
            // we do not test CompilationReferenceView because the changed reference would cause us to retarget
            verify2(MetadataView(comp, assignments, references));
            verify2(RetargetingView(comp, assignments, references));

            static void verify1(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M", "System.String Derived.M()", "System.Object Base.M()");
            }

            static void verify2(CSharpCompilation comp)
            {
                // When viewed from metadata, we do not see a relationship between Derived.M and Mid.M because
                // there is nothing in the metadata to indicate there is relationship. The compiler
                // does not simulate the covariant language rules on metadata nor does the compiler simulate the
                // virtual slot unification that the runtime does.
                VerifyOverride(comp, "Derived.M", "System.String Derived.M()", "System.Object Base.M()");
                VerifyOverride(comp, "Mid.M", "System.Object Mid.M()", "System.Object Base.M()");
            }
        }

        [Fact]
        public void LegacyMethodimplRequirements_01()
        {
            var source = @"
public class A
{
    public virtual string get_P() => null;
}

public class B : A
{
    public virtual string P => null;
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
            var assignments = "";
            var comp = CreateCompilationWithoutCovariantReturns(source).VerifyDiagnostics(
                );
            verify(SourceView(comp, assignments));
            verify(CompilationReferenceView(comp, assignments));
            verify(MetadataView(comp, assignments));
            verify(RetargetingView(comp, assignments));

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "C.get_P", "System.String C.get_P()", "System.String A.get_P()", requiresMethodimpl: true);
                VerifyOverride(comp, "D.P", "System.String D.P { get; }", "System.String B.P { get; }", requiresMethodimpl: true);
                VerifyOverride(comp, "D.get_P", "System.String D.P.get", "System.String B.P.get", requiresMethodimpl: true);
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
            var assignments = "";
            var comp = CreateCompilationWithCovariantReturns(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (14,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string get_P() => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "get_P").WithArguments("covariant returns", "9.0").WithLocation(14, 28),
                // (19,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string P => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "P").WithArguments("covariant returns", "9.0").WithLocation(19, 28)
                );
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(SourceView(comp, assignments));
            verify(CompilationReferenceView(comp, assignments));
            verify(MetadataView(comp, assignments));
            verify(RetargetingView(comp, assignments));

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
            var assignments = "";
            var comp = CreateCompilationWithCovariantReturns(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (12,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string P => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "P").WithArguments("covariant returns", "9.0").WithLocation(12, 28)
                );
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(SourceView(comp, assignments));
            verify(CompilationReferenceView(comp, assignments));
            verify(MetadataView(comp, assignments));
            verify(RetargetingView(comp, assignments));

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
            var assignments = "";
            var comp = CreateCompilationWithCovariantReturns(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (12,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string M() => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M").WithArguments("covariant returns", "9.0").WithLocation(12, 28)
                );
            verify(SourceView(comp, assignments));
            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            verify(SourceView(comp, assignments));
            verify(CompilationReferenceView(comp, assignments));
            verify(MetadataView(comp, assignments));
            verify(RetargetingView(comp, assignments));

            static void verify(CSharpCompilation comp)
            {
                VerifyNoOverride(comp, "A.M");
                VerifyNoOverride(comp, "B.M");
                VerifyOverride(comp, "C.M", "System.String C.M()", "System.Object B.M()");
            }
        }

        [Fact]
        public void InExpressionTree_01()
        {
            var source = @"
using System;
using System.Linq.Expressions;
public class Base
{
    public virtual object M() => null;
    public virtual object P => null;
}
public class Derived : Base
{
    public override string M() => null;
    public override string P => null;
}
public class Program : Derived
{
    Expression<Func<Derived, string>> M1()
    {
        return d => d.M();
    }
    Expression<Func<Derived, string>> M2()
    {
        return d => d.P;
    }
    Expression<Func<Func<string>>> M3()
    {
        return () => M;
    }
    Expression<Func<Func<string>>> M4()
    {
        return () => new Func<string>(M);
    }
}
";
            var comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            var verifier = CompileAndVerify(comp, verify: Verification.Skipped);
            verifier.VerifyIL("Program.M1()", source: source, sequencePoints: "Program.M1", expectedIL: @"
{
  // Code size       63 (0x3f)
  .maxstack  5
  .locals init (System.Linq.Expressions.ParameterExpression V_0)
  // sequence point: return d => d.M();
  IL_0000:  ldtoken    ""Derived""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""d""
  IL_000f:  call       ""System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)""
  IL_0014:  stloc.0
  IL_0015:  ldloc.0
  IL_0016:  ldtoken    ""string Derived.M()""
  IL_001b:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_0020:  castclass  ""System.Reflection.MethodInfo""
  IL_0025:  call       ""System.Linq.Expressions.Expression[] System.Array.Empty<System.Linq.Expressions.Expression>()""
  IL_002a:  call       ""System.Linq.Expressions.MethodCallExpression System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression, System.Reflection.MethodInfo, params System.Linq.Expressions.Expression[])""
  IL_002f:  ldc.i4.1
  IL_0030:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_0035:  dup
  IL_0036:  ldc.i4.0
  IL_0037:  ldloc.0
  IL_0038:  stelem.ref
  IL_0039:  call       ""System.Linq.Expressions.Expression<System.Func<Derived, string>> System.Linq.Expressions.Expression.Lambda<System.Func<Derived, string>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_003e:  ret
}
");
            verifier.VerifyIL("Program.M2()", source: source, sequencePoints: "Program.M2", expectedIL: @"
{
  // Code size       58 (0x3a)
  .maxstack  5
  .locals init (System.Linq.Expressions.ParameterExpression V_0)
  // sequence point: return d => d.P;
  IL_0000:  ldtoken    ""Derived""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""d""
  IL_000f:  call       ""System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)""
  IL_0014:  stloc.0
  IL_0015:  ldloc.0
  IL_0016:  ldtoken    ""string Derived.P.get""
  IL_001b:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_0020:  castclass  ""System.Reflection.MethodInfo""
  IL_0025:  call       ""System.Linq.Expressions.MemberExpression System.Linq.Expressions.Expression.Property(System.Linq.Expressions.Expression, System.Reflection.MethodInfo)""
  IL_002a:  ldc.i4.1
  IL_002b:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_0030:  dup
  IL_0031:  ldc.i4.0
  IL_0032:  ldloc.0
  IL_0033:  stelem.ref
  IL_0034:  call       ""System.Linq.Expressions.Expression<System.Func<Derived, string>> System.Linq.Expressions.Expression.Lambda<System.Func<Derived, string>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_0039:  ret
}
");
            verifier.VerifyIL("Program.M3()", source: source, sequencePoints: "Program.M3", expectedIL: @"
{
  // Code size      129 (0x81)
  .maxstack  7
  // sequence point: return () => M;
  IL_0000:  ldtoken    ""string Derived.M()""
  IL_0005:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_000a:  castclass  ""System.Reflection.MethodInfo""
  IL_000f:  ldtoken    ""System.Reflection.MethodInfo""
  IL_0014:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0019:  call       ""System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)""
  IL_001e:  ldtoken    ""System.Delegate System.Reflection.MethodInfo.CreateDelegate(System.Type, object)""
  IL_0023:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_0028:  castclass  ""System.Reflection.MethodInfo""
  IL_002d:  ldc.i4.2
  IL_002e:  newarr     ""System.Linq.Expressions.Expression""
  IL_0033:  dup
  IL_0034:  ldc.i4.0
  IL_0035:  ldtoken    ""System.Func<string>""
  IL_003a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_003f:  ldtoken    ""System.Type""
  IL_0044:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0049:  call       ""System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)""
  IL_004e:  stelem.ref
  IL_004f:  dup
  IL_0050:  ldc.i4.1
  IL_0051:  ldarg.0
  IL_0052:  ldtoken    ""Program""
  IL_0057:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005c:  call       ""System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)""
  IL_0061:  stelem.ref
  IL_0062:  call       ""System.Linq.Expressions.MethodCallExpression System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression, System.Reflection.MethodInfo, params System.Linq.Expressions.Expression[])""
  IL_0067:  ldtoken    ""System.Func<string>""
  IL_006c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0071:  call       ""System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression, System.Type)""
  IL_0076:  call       ""System.Linq.Expressions.ParameterExpression[] System.Array.Empty<System.Linq.Expressions.ParameterExpression>()""
  IL_007b:  call       ""System.Linq.Expressions.Expression<System.Func<System.Func<string>>> System.Linq.Expressions.Expression.Lambda<System.Func<System.Func<string>>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_0080:  ret
}
");
            verifier.VerifyIL("Program.M4()", source: source, sequencePoints: "Program.M4", expectedIL: @"
{
  // Code size      129 (0x81)
  .maxstack  7
  // sequence point: return () => new Func<string>(M);
  IL_0000:  ldtoken    ""string Derived.M()""
  IL_0005:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_000a:  castclass  ""System.Reflection.MethodInfo""
  IL_000f:  ldtoken    ""System.Reflection.MethodInfo""
  IL_0014:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0019:  call       ""System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)""
  IL_001e:  ldtoken    ""System.Delegate System.Reflection.MethodInfo.CreateDelegate(System.Type, object)""
  IL_0023:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_0028:  castclass  ""System.Reflection.MethodInfo""
  IL_002d:  ldc.i4.2
  IL_002e:  newarr     ""System.Linq.Expressions.Expression""
  IL_0033:  dup
  IL_0034:  ldc.i4.0
  IL_0035:  ldtoken    ""System.Func<string>""
  IL_003a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_003f:  ldtoken    ""System.Type""
  IL_0044:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0049:  call       ""System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)""
  IL_004e:  stelem.ref
  IL_004f:  dup
  IL_0050:  ldc.i4.1
  IL_0051:  ldarg.0
  IL_0052:  ldtoken    ""Program""
  IL_0057:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005c:  call       ""System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)""
  IL_0061:  stelem.ref
  IL_0062:  call       ""System.Linq.Expressions.MethodCallExpression System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression, System.Reflection.MethodInfo, params System.Linq.Expressions.Expression[])""
  IL_0067:  ldtoken    ""System.Func<string>""
  IL_006c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0071:  call       ""System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression, System.Type)""
  IL_0076:  call       ""System.Linq.Expressions.ParameterExpression[] System.Array.Empty<System.Linq.Expressions.ParameterExpression>()""
  IL_007b:  call       ""System.Linq.Expressions.Expression<System.Func<System.Func<string>>> System.Linq.Expressions.Expression.Lambda<System.Func<System.Func<string>>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_0080:  ret
}
");
        }

        [Fact]
        public void InDelegateCreation_01()
        {
            var source = @"
using System;
public class Base
{
    public virtual object M() => null;
}
public class Derived : Base
{
    public override string M() => null;
    Func<string> M1() => M;
    Func<object> M2() => base.M;
    Func<string> M3() => new Func<string>(M);
    Func<object> M4() => new Func<object>(base.M);
}
public class Program : Derived
{
    Func<string> M1() => M;
    Func<string> M2() => base.M;
    Func<string> M3() => new Func<string>(M);
    Func<string> M4() => new Func<string>(base.M);
}
";
            var comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                );
            var verifier = CompileAndVerify(comp, verify: Verification.Skipped);
            verifier.VerifyIL("Derived.M1()", source: source, sequencePoints: "Derived.M1", expectedIL: @"
{
  // Code size       14 (0xe)
  .maxstack  2
  // sequence point: M
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldvirtftn  ""string Derived.M()""
  IL_0008:  newobj     ""System.Func<string>..ctor(object, System.IntPtr)""
  IL_000d:  ret
}
");
            verifier.VerifyIL("Derived.M2()", source: source, sequencePoints: "Derived.M2", expectedIL: @"
{
  // Code size       13 (0xd)
  .maxstack  2
  // sequence point: base.M
  IL_0000:  ldarg.0
  IL_0001:  ldftn      ""object Base.M()""
  IL_0007:  newobj     ""System.Func<object>..ctor(object, System.IntPtr)""
  IL_000c:  ret
}
");
            verifier.VerifyIL("Derived.M3()", source: source, sequencePoints: "Derived.M3", expectedIL: @"
{
  // Code size       14 (0xe)
  .maxstack  2
  // sequence point: new Func<string>(M)
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldvirtftn  ""string Derived.M()""
  IL_0008:  newobj     ""System.Func<string>..ctor(object, System.IntPtr)""
  IL_000d:  ret
}
");
            verifier.VerifyIL("Derived.M4()", source: source, sequencePoints: "Derived.M4", expectedIL: @"
{
  // Code size       13 (0xd)
  .maxstack  2
  // sequence point: new Func<object>(base.M)
  IL_0000:  ldarg.0
  IL_0001:  ldftn      ""object Base.M()""
  IL_0007:  newobj     ""System.Func<object>..ctor(object, System.IntPtr)""
  IL_000c:  ret
}
");
            verifier.VerifyIL("Program.M1()", source: source, sequencePoints: "Program.M1", expectedIL: @"
{
  // Code size       14 (0xe)
  .maxstack  2
  // sequence point: M
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldvirtftn  ""string Derived.M()""
  IL_0008:  newobj     ""System.Func<string>..ctor(object, System.IntPtr)""
  IL_000d:  ret
}
");
            verifier.VerifyIL("Program.M2()", source: source, sequencePoints: "Program.M2", expectedIL: @"
{
  // Code size       13 (0xd)
  .maxstack  2
  // sequence point: base.M
  IL_0000:  ldarg.0
  IL_0001:  ldftn      ""string Derived.M()""
  IL_0007:  newobj     ""System.Func<string>..ctor(object, System.IntPtr)""
  IL_000c:  ret
}
");
            verifier.VerifyIL("Program.M3()", source: source, sequencePoints: "Program.M3", expectedIL: @"
{
  // Code size       14 (0xe)
  .maxstack  2
  // sequence point: new Func<string>(M)
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldvirtftn  ""string Derived.M()""
  IL_0008:  newobj     ""System.Func<string>..ctor(object, System.IntPtr)""
  IL_000d:  ret
}
");
            verifier.VerifyIL("Program.M4()", source: source, sequencePoints: "Program.M4", expectedIL: @"
{
  // Code size       13 (0xd)
  .maxstack  2
  // sequence point: new Func<string>(base.M)
  IL_0000:  ldarg.0
  IL_0001:  ldftn      ""string Derived.M()""
  IL_0007:  newobj     ""System.Func<string>..ctor(object, System.IntPtr)""
  IL_000c:  ret
}
");
        }

        [Fact]
        public void NullableVariance_01()
        {
            var source = @"
#nullable enable
public class Base
{
    public virtual object M1() => null!;
    public virtual object P1 => null!;
    public virtual object? M2() => null;
    public virtual object? P2 => null;
    public virtual IOut<object> M3() => null!;
    public virtual IOut<object> P3 => null!;
    public virtual IOut<object?> M4() => null!;
    public virtual IOut<object?> P4 => null!;
    public virtual IIn<string> M5() => null!;
    public virtual IIn<string> P5 => null!;
    public virtual IIn<string?> M6() => null!;
    public virtual IIn<string?> P6 => null!;
}
public class Derived : Base
{
    public override string? M1() => null;
    public override string? P1 => null;
    public override string M2() => null!;
    public override string P2 => null!;
    public override IOut<string?> M3() => null!;
    public override IOut<string?> P3 => null!;
    public override IOut<string> M4() => null!;
    public override IOut<string> P4 => null!;
    public override IIn<object?> M5() => null!;
    public override IIn<object?> P5 => null!;
    public override IIn<object> M6() => null!;
    public override IIn<object> P6 => null!;
}
public interface IOut<out T> { }
public interface IIn<in T> { }
";
            var assignments = @"
#nullable enable
public class Program
{
    void M(Base b, Derived d)
    {
        object x1 = b.M1();
        string? x2 = d.M1();
        object x3 = b.P1;
        string? x4 = d.P1;
        object? x5 = b.M2();
        string x6 = d.M2();
        object? x7 = b.P2;
        string x8 = d.P2;
        IOut<object> x9 = b.M3();
        IOut<string?> x10 = d.M3();
        IOut<object> x11 = b.P3;
        IOut<string?> x12 = d.P3;
        IOut<object?> x13 = b.M4();
        IOut<string> x14 = d.M4();
        IOut<object?> x15 = b.P4;
        IOut<string> x16 = d.P4;
        IIn<string> x17 = b.M5();
        IIn<object?> x18 = d.M5();
        IIn<string> x19 = b.P5;
        IIn<object?> x20 = d.P5;
        IIn<string?> x21 = b.M6();
        IIn<object> x22 = d.M6();
        IIn<string?> x23 = b.P6;
        IIn<object> x24 = d.P6;
    }
}
";
            var comp = CreateCompilationWithCovariantReturns(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (20,29): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string? M1() => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M1").WithArguments("covariant returns", "9.0").WithLocation(20, 29),
                // (21,29): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string? P1 => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "P1").WithArguments("covariant returns", "9.0").WithLocation(21, 29),
                // (22,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string M2() => null!;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M2").WithArguments("covariant returns", "9.0").WithLocation(22, 28),
                // (23,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string P2 => null!;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "P2").WithArguments("covariant returns", "9.0").WithLocation(23, 28),
                // (24,35): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override IOut<string?> M3() => null!;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M3").WithArguments("covariant returns", "9.0").WithLocation(24, 35),
                // (25,35): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override IOut<string?> P3 => null!;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "P3").WithArguments("covariant returns", "9.0").WithLocation(25, 35),
                // (26,34): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override IOut<string> M4() => null!;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M4").WithArguments("covariant returns", "9.0").WithLocation(26, 34),
                // (27,34): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override IOut<string> P4 => null!;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "P4").WithArguments("covariant returns", "9.0").WithLocation(27, 34),
                // (28,34): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override IIn<object?> M5() => null!;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M5").WithArguments("covariant returns", "9.0").WithLocation(28, 34),
                // (29,34): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override IIn<object?> P5 => null!;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "P5").WithArguments("covariant returns", "9.0").WithLocation(29, 34),
                // (30,33): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override IIn<object> M6() => null!;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M6").WithArguments("covariant returns", "9.0").WithLocation(30, 33),
                // (31,33): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override IIn<object> P6 => null!;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "P6").WithArguments("covariant returns", "9.0").WithLocation(31, 33)
                );
            verify(SourceView(comp, assignments));

            comp = CreateCompilationWithCovariantReturns(source).VerifyDiagnostics(
                // (20,29): warning CS8764: Nullability of return type doesn't match overridden member (possibly because of nullability attributes).
                //     public override string? M1() => null;
                Diagnostic(ErrorCode.WRN_TopLevelNullabilityMismatchInReturnTypeOnOverride, "M1").WithLocation(20, 29),
                // (21,35): warning CS8764: Nullability of return type doesn't match overridden member (possibly because of nullability attributes).
                //     public override string? P1 => null;
                Diagnostic(ErrorCode.WRN_TopLevelNullabilityMismatchInReturnTypeOnOverride, "null").WithLocation(21, 35),
                // (24,35): warning CS8609: Nullability of reference types in return type doesn't match overridden member.
                //     public override IOut<string?> M3() => null!;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverride, "M3").WithLocation(24, 35),
                // (25,41): warning CS8609: Nullability of reference types in return type doesn't match overridden member.
                //     public override IOut<string?> P3 => null!;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverride, "null!").WithLocation(25, 41),
                // (30,33): warning CS8609: Nullability of reference types in return type doesn't match overridden member.
                //     public override IIn<object> M6() => null!;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverride, "M6").WithLocation(30, 33),
                // (31,39): warning CS8609: Nullability of reference types in return type doesn't match overridden member.
                //     public override IIn<object> P6 => null!;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverride, "null!").WithLocation(31, 39)
                );
            verify(SourceView(comp, assignments));
            verify(CompilationReferenceView(comp, assignments));
            verify(MetadataView(comp, assignments));
            verify(RetargetingView(comp, assignments));

            static void verify(CSharpCompilation comp)
            {
                VerifyOverride(comp, "Derived.M1", "System.String? Derived.M1()", "System.Object Base.M1()");
                VerifyOverride(comp, "Derived.P1", "System.String? Derived.P1 { get; }", "System.Object Base.P1 { get; }");
                VerifyOverride(comp, "Derived.M2", "System.String Derived.M2()", "System.Object? Base.M2()");
                VerifyOverride(comp, "Derived.P2", "System.String Derived.P2 { get; }", "System.Object? Base.P2 { get; }");
                VerifyOverride(comp, "Derived.M3", "IOut<System.String?> Derived.M3()", "IOut<System.Object> Base.M3()");
                VerifyOverride(comp, "Derived.P3", "IOut<System.String?> Derived.P3 { get; }", "IOut<System.Object> Base.P3 { get; }");
                VerifyOverride(comp, "Derived.M4", "IOut<System.String> Derived.M4()", "IOut<System.Object?> Base.M4()");
                VerifyOverride(comp, "Derived.P4", "IOut<System.String> Derived.P4 { get; }", "IOut<System.Object?> Base.P4 { get; }");
                VerifyOverride(comp, "Derived.M5", "IIn<System.Object?> Derived.M5()", "IIn<System.String> Base.M5()");
                VerifyOverride(comp, "Derived.P5", "IIn<System.Object?> Derived.P5 { get; }", "IIn<System.String> Base.P5 { get; }");
                VerifyOverride(comp, "Derived.M6", "IIn<System.Object> Derived.M6()", "IIn<System.String?> Base.M6()");
                VerifyOverride(comp, "Derived.P6", "IIn<System.Object> Derived.P6 { get; }", "IIn<System.String?> Base.P6 { get; }");
                VerifyAssignments(comp, 24);
            }
        }

        [Fact]
        public void PEMethodSymbol_ExplicitlyOverriddenClassMethod_WhenAmbiguous()
        {
            // See also related scenario in ExplicitOverrideWithoutCSharpOverride

            var ilSource = @"
.assembly ilSource {}
.assembly extern mscorlib
{
  .ver 4:1:0:0
}

.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance object  M1() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldnull
    IL_0001:  ret
  } // end of method Base::M1

  .method public hidebysig newslot virtual 
          instance object  M2() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldnull
    IL_0001:  ret
  } // end of method Base::M2

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Base::.ctor
} // end of class Base

.class public auto ansi beforefieldinit Derived
       extends Base
{
  .method public hidebysig newslot virtual 
          instance string  M3() cil managed
  {
    .override method instance object class Base::M1() // different name, type
    .override method instance object class Base::M2() // different name, type
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldnull
    IL_0001:  ret
  } // end of method Derived::M3

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Base::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Derived::.ctor
}
";

            var cSharpSource = @"
public class Override : Derived
{
    public override string M1() => null;
    public override string M2() => null;
    public override string M3() => null;
}
";
            var assignments = @"
public class Program
{
    public void M(Derived d, Override o)
    {
        object x1 = d.M1();
        object x2 = d.M2();
        string x3 = d.M3();
        string x4 = o.M1();
        string x5 = o.M2();
        string x6 = o.M3();
    }
}
";
            MetadataReference ilReference = CreateMetadataReferenceFromIlSource(ilSource, prependDefaultHeader: false);
            var references = new[] { ilReference };
            var comp = CreateCompilationWithCovariantReturns(cSharpSource, references, parseOptions: TestOptions.WithoutCovariantReturns);
            comp.VerifyDiagnostics(
                // (4,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string M1() => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M1").WithArguments("covariant returns", "9.0").WithLocation(4, 28),
                // (5,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override string M2() => null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M2").WithArguments("covariant returns", "9.0").WithLocation(5, 28)
                );
            verify(SourceView(comp, assignments));

            comp = CreateCompilationWithCovariantReturns(cSharpSource, references);
            comp.VerifyDiagnostics();
            verify(SourceView(comp, assignments));
            verify(CompilationReferenceView(comp, assignments, references));
            verify(MetadataView(comp, assignments, references));
            verify(RetargetingView(comp, assignments, references));

            void verify(CSharpCompilation comp)
            {
                VerifyNoOverride(comp, "Base.M1");
                VerifyNoOverride(comp, "Base.M2");
                VerifyNoOverride(comp, "Derived.M3");
                VerifyOverride(comp, "Override.M1", "System.String Override.M1()", "System.Object Base.M1()");
                VerifyOverride(comp, "Override.M2", "System.String Override.M2()", "System.Object Base.M2()");
                VerifyOverride(comp, "Override.M3", "System.String Override.M3()", "System.String Derived.M3()");
                VerifyAssignments(comp, 6);

                var globalNamespace = comp.GlobalNamespace;

                var derivedClass = globalNamespace.GetMember<NamedTypeSymbol>("Derived");
                var overrideClass = globalNamespace.GetMember<NamedTypeSymbol>("Override");

                var derivedMethod = derivedClass.GetMember<MethodSymbol>("M3");
                var overrideMethod = overrideClass.GetMember<MethodSymbol>("M3");

                // Note that the following values are inconsistent. That is "legacy" (early Roslyn) behavior that we preserve.
                Assert.True(derivedMethod.IsOverride);
                Assert.Null(derivedMethod.OverriddenMethod);

                Assert.True(overrideMethod.IsOverride);
                Assert.Equal(derivedMethod, overrideMethod.OverriddenMethod);
            }
        }

        [Theory]
        [CombinatorialData]
        public void OverrideAmbiguities_01(
            bool withCovariantReturnFeatureEnabled,
            bool withCovariantCapableRuntime,
            bool methodRuntimeOverriddenSignatureAmbiguity,
            bool overriddenRuntimeSignatureAmbiguity,
            bool useCovariantReturns,
            bool useSeparateCompilation
            )
        {
            var overriddenMethodReturnType = useCovariantReturns ? "object" : "string";
            var baseSource = $@"
public class Base1<Ptring>
{{
    public virtual {overriddenMethodReturnType} M(ref Ptring x, out string y) {{ y = null; return null; }}
    {(overriddenRuntimeSignatureAmbiguity ? @$"public virtual {overriddenMethodReturnType} M(ref Ptring x, ref Ptring y) {{ return null; }}" : "")}
}}
public class Base2<Ptring> : Base1<Ptring>
{{
    public virtual string M(out string x, ref Ptring y) {{ x = null; return null; }}
    {(methodRuntimeOverriddenSignatureAmbiguity ? "public virtual string M(out string x, out string y) { x = y = null; return null; }" : "")}
}}
";
            var source = $@"
public class Derived : Base2<string>
{{
    public override string M(ref string x, out string y) {{ y = null; return null; }}
}}
";
            var parseOptions = withCovariantReturnFeatureEnabled ? TestOptions.WithCovariantReturns : TestOptions.WithoutCovariantReturns;

            var corlibRef = withCovariantCapableRuntime ? CorelibraryWithCovariantReturnSupport1 : CorelibraryWithoutCovariantReturnSupport1;
            var corlib2Ref = withCovariantCapableRuntime ? CorelibraryWithCovariantReturnSupport2 : CorelibraryWithoutCovariantReturnSupport2;

            var expectedDiagnostics = new DiagnosticDescription[0];
            bool anyErrors = false;
            if (useCovariantReturns)
            {
                if (!withCovariantCapableRuntime)
                {
                    expectedDiagnostics = expectedDiagnostics.Append(
                        // (4,28): error CS8778: 'Derived.M(ref string, out string)': Target runtime doesn't support covariant return types in overrides. Return type must be 'object' to match overridden member 'Base1<string>.M(ref string, out string)'
                        //     public override string M(ref string x, out string y) { y = null; return null; }
                        Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportCovariantReturnsOfClasses, "M").WithArguments("Derived.M(ref string, out string)", "Base1<string>.M(ref string, out string)", "object")
                        ).ToArray();
                    anyErrors = true;
                }
                else if (!withCovariantReturnFeatureEnabled)
                {
                    expectedDiagnostics = expectedDiagnostics.Append(
                    // (15,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                    //     public override string M(ref string x, out string y) { y = null; return null; }
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M").WithArguments("covariant returns", "9.0")
                        ).ToArray();
                    anyErrors = true;
                }
            }
            bool warned = false;
            if (overriddenRuntimeSignatureAmbiguity && !withCovariantCapableRuntime && !useCovariantReturns)
            {
                expectedDiagnostics = expectedDiagnostics.Append(
                    // (4,27): warning CS1957: Member 'Derived.M(ref string, out string)' overrides 'Base1<string>.M(ref string, out string)'. There are multiple override candidates at run-time. It is implementation dependent which method will be called. Please use a newer runtime.
                    //     public virtual string M(ref Ptring x, out string y) { y = null; return null; }
                    Diagnostic(ErrorCode.WRN_MultipleRuntimeOverrideMatches, "M").WithArguments("Base1<string>.M(ref string, out string)", "Derived.M(ref string, out string)").WithLocation(4, 27)
                    ).ToArray();
                warned = true;
            }

            // All of the overrides in this test require a methodimpl because they are on a different class from the runtime override.
            bool requiresMethodImpl = true;

            MetadataReference[] references;
            MetadataReference[] retargetReferences;
            string compilationSource;

            if (useSeparateCompilation)
            {
                var baseCompilation = CreateCompilation(baseSource, references: new[] { corlibRef }, targetFramework: TargetFramework.Empty, parseOptions: parseOptions);
                baseCompilation.VerifyDiagnostics();
                var baseMetadata = baseCompilation.ToMetadataReference();

                references = new[] { corlibRef, baseMetadata };
                retargetReferences = new[] { corlib2Ref, baseMetadata };
                compilationSource = source;

                verify(RetargetingView(baseCompilation, compilationSource, expectedDiagnostics: expectedDiagnostics));
            }
            else
            {
                references = new[] { corlibRef };
                retargetReferences = new[] { corlib2Ref };
                compilationSource = baseSource + source;
            }

            var comp = CreateCompilation(compilationSource, references: references, parseOptions: parseOptions, targetFramework: TargetFramework.Empty);
            comp.VerifyDiagnostics(expectedDiagnostics);

            var member = (SourceMethodSymbol)comp.GlobalNamespace.GetMember("Derived.M");
            bool useMethodImpl = member.RequiresExplicitOverride(out bool shouldWarn);
            Assert.Equal(warned, shouldWarn);
            Assert.Equal(requiresMethodImpl, useMethodImpl);

            verify(SourceView(comp, ""));
            verify(CompilationReferenceView(comp, "", references: references, withoutCorlib: true));
            verify(RetargetingView(comp, "", references: retargetReferences, withoutCorlib: true));
            if (!anyErrors)
                verify(MetadataView(comp, "", references: references, withoutCorlib: true));

            void verify(CSharpCompilation compilation)
            {
                VerifyOverride(compilation,
                    methodName: "Derived.M",
                    overridingMemberDisplay: "System.String Derived.M(ref System.String x, out System.String y)",
                    overriddenMemberDisplay: $"System.{(useCovariantReturns ? "Object" : "String")} Base1<System.String>.M(ref System.String x, out System.String y)",
                    requiresMethodimpl: requiresMethodImpl);
            }
        }

        [Theory]
        [CombinatorialData]
        public void OverrideAmbiguities_02(
            bool withCovariantReturnFeatureEnabled,
            bool withCovariantCapableRuntime,
            bool methodRuntimeOverriddenSignatureAmbiguity,
            bool overriddenRuntimeSignatureAmbiguity,
            bool useCovariantReturns,
            bool useSeparateCompilation
            )
        {
            var overriddenMethodReturnType = useCovariantReturns ? "object" : "string";
            var baseSource = $@"
public class Base1<Ptring>
{{
    {(overriddenRuntimeSignatureAmbiguity ? @$"public virtual {overriddenMethodReturnType} M(ref Ptring x, ref Ptring y) {{ return null; }}" : "")}
    public virtual {overriddenMethodReturnType} M(ref Ptring x, out string y) {{ y = null; return null; }}
}}
public class Base2<Ptring> : Base1<Ptring>
{{
    {(methodRuntimeOverriddenSignatureAmbiguity ? "public virtual string M(out string x, out string y) { x = y = null; return null; }" : "")}
    public virtual string M(out string x, ref Ptring y) {{ x = null; return null; }}
}}
";
            var source = $@"
public class Derived : Base2<string>
{{
    public override string M(ref string x, out string y) {{ y = null; return null; }}
}}
";
            var parseOptions = withCovariantReturnFeatureEnabled ? TestOptions.WithCovariantReturns : TestOptions.WithoutCovariantReturns;

            var corlibRef = withCovariantCapableRuntime ? CorelibraryWithCovariantReturnSupport1 : CorelibraryWithoutCovariantReturnSupport1;
            var corlib2Ref = withCovariantCapableRuntime ? CorelibraryWithCovariantReturnSupport2 : CorelibraryWithoutCovariantReturnSupport2;

            var baseCompilation = CreateCompilation(baseSource, references: new[] { corlibRef }, targetFramework: TargetFramework.Empty, parseOptions: parseOptions);
            baseCompilation.VerifyDiagnostics();
            var baseMetadata = baseCompilation.ToMetadataReference();

            var expectedDiagnostics = new DiagnosticDescription[0];
            bool anyErrors = false;
            if (useCovariantReturns)
            {
                if (!withCovariantCapableRuntime)
                {
                    expectedDiagnostics = expectedDiagnostics.Append(
                        // (4,28): error CS8778: 'Derived.M(ref string, out string)': Target runtime doesn't support covariant return types in overrides. Return type must be 'object' to match overridden member 'Base1<string>.M(ref string, out string)'
                        //     public override string M(ref string x, out string y) { y = null; return null; }
                        Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportCovariantReturnsOfClasses, "M").WithArguments("Derived.M(ref string, out string)", "Base1<string>.M(ref string, out string)", "object")
                        ).ToArray();
                    anyErrors = true;
                }
                else if (!withCovariantReturnFeatureEnabled)
                {
                    expectedDiagnostics = expectedDiagnostics.Append(
                        // (15,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                        //     public override string M(ref string x, out string y) { y = null; return null; }
                        Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M").WithArguments("covariant returns", "9.0")
                        ).ToArray();
                    anyErrors = true;
                }
            }
            bool warned = false;
            if (overriddenRuntimeSignatureAmbiguity && !withCovariantCapableRuntime && !useCovariantReturns)
            {
                expectedDiagnostics = expectedDiagnostics.Append(
                    // (5,27): warning CS1957: Member 'Derived.M(ref string, out string)' overrides 'Base1<string>.M(ref string, out string)'. There are multiple override candidates at run-time. It is implementation dependent which method will be called. Please use a newer runtime.
                    //     public virtual string M(ref Ptring x, out string y) { y = null; return null; }
                    Diagnostic(ErrorCode.WRN_MultipleRuntimeOverrideMatches, "M").WithArguments("Base1<string>.M(ref string, out string)", "Derived.M(ref string, out string)").WithLocation(5, 27)
                    ).ToArray();
                warned = true;
            }

            // All of the overrides in this test require a methodimpl because they are on a different class from the runtime override.
            bool requiresMethodImpl = true;

            MetadataReference[] references;
            MetadataReference[] retargetReferences;
            string compilationSource;

            if (useSeparateCompilation)
            {
                references = new[] { corlibRef, baseMetadata };
                retargetReferences = new[] { corlib2Ref, baseMetadata };
                compilationSource = source;

                verify(RetargetingView(baseCompilation, compilationSource, expectedDiagnostics: expectedDiagnostics));
            }
            else
            {
                references = new[] { corlibRef };
                retargetReferences = new[] { corlib2Ref };
                compilationSource = baseSource + source;
            }

            var comp = CreateCompilation(compilationSource, references: references, parseOptions: parseOptions, targetFramework: TargetFramework.Empty);
            comp.VerifyDiagnostics(expectedDiagnostics);

            var member = (SourceMethodSymbol)comp.GlobalNamespace.GetMember("Derived.M");
            bool useMethodImpl = member.RequiresExplicitOverride(out bool shouldWarn);
            Assert.Equal(warned, shouldWarn);
            Assert.Equal(requiresMethodImpl, useMethodImpl);

            verify(SourceView(comp, ""));
            verify(CompilationReferenceView(comp, "", references: references, withoutCorlib: true));
            if (!useCovariantReturns)
                verify(RetargetingView(comp, "", references: retargetReferences, withoutCorlib: true));
            if (!anyErrors)
                verify(MetadataView(comp, "", references: references, withoutCorlib: true));

            void verify(CSharpCompilation compilation)
            {
                var lastReference = compilation.GetAssemblyOrModuleSymbol(compilation.References.Last());
                // Due to https://github.com/dotnet/roslyn/issues/45566 retargeting methods do not resolve properly for this scenario
                var isRetargeting = lastReference is RetargetingAssemblySymbol;
                // Similarly, there is probably a corresponding bug in resolving PE method symbols
                var isMetadata = lastReference is PEAssemblySymbol;
                if (overriddenRuntimeSignatureAmbiguity && (isRetargeting || isMetadata))
                    return;
                VerifyOverride(compilation,
                    methodName: "Derived.M",
                    overridingMemberDisplay: "System.String Derived.M(ref System.String x, out System.String y)",
                    overriddenMemberDisplay: $"System.{(useCovariantReturns ? "Object" : "String")} Base1<System.String>.M(ref System.String x, out System.String y)",
                    requiresMethodimpl: requiresMethodImpl);
            }
        }

        [Theory]
        [CombinatorialData]
        public void OverrideAmbiguities_03(
            bool withCovariantReturnFeatureEnabled,
            bool withCovariantCapableRuntime,
            bool methodRuntimeOverriddenSignatureAmbiguity,
            bool overriddenRuntimeSignatureAmbiguity,
            bool useCovariantReturns,
            bool useSeparateCompilation
            )
        {
            var overriddenMethodReturnType = useCovariantReturns ? "object" : "string";
            var baseSource = $@"
public class Base<Ptring>
{{
    public virtual {overriddenMethodReturnType} M(ref Ptring x, out string y) {{ y = null; return null; }}
    {(overriddenRuntimeSignatureAmbiguity ? @$"public virtual {overriddenMethodReturnType} M(ref Ptring x, ref Ptring y) {{ return null; }}" : "")}
    public virtual string M(out string x, ref Ptring y) {{ x = null; return null; }}
    {(methodRuntimeOverriddenSignatureAmbiguity ? "public virtual string M(out string x, out string y) { x = y = null; return null; }" : "")}
}}
";
            var source = $@"
public class Derived : Base<string>
{{
    public override string M(ref string x, out string y) {{ y = null; return null; }}
}}
";
            var parseOptions = withCovariantReturnFeatureEnabled ? TestOptions.WithCovariantReturns : TestOptions.WithoutCovariantReturns;

            var corlibRef = withCovariantCapableRuntime ? CorelibraryWithCovariantReturnSupport1 : CorelibraryWithoutCovariantReturnSupport1;
            var corlib2Ref = withCovariantCapableRuntime ? CorelibraryWithCovariantReturnSupport2 : CorelibraryWithoutCovariantReturnSupport2;

            var expectedDiagnostics = new DiagnosticDescription[0];
            bool anyErrors = false;
            if (useCovariantReturns)
            {
                if (!withCovariantCapableRuntime)
                {
                    expectedDiagnostics = expectedDiagnostics.Append(
                        // (4,28): error CS8778: 'Derived.M(ref string, out string)': Target runtime doesn't support covariant return types in overrides. Return type must be 'object' to match overridden member 'Base<string>.M(ref string, out string)'
                        //     public override string M(ref string x, out string y) { y = null; return null; }
                        Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportCovariantReturnsOfClasses, "M").WithArguments("Derived.M(ref string, out string)", "Base<string>.M(ref string, out string)", "object")
                        ).ToArray();
                    anyErrors = true;
                }
                else if (!withCovariantReturnFeatureEnabled)
                {
                    expectedDiagnostics = expectedDiagnostics.Append(
                        // (12,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                        //     public override string M(ref string x, out string y) { y = null; return null; }
                        Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M").WithArguments("covariant returns", "9.0")
                        ).ToArray();
                    anyErrors = true;
                }
            }
            bool warned = false;
            if (!withCovariantCapableRuntime && !useCovariantReturns)
            {
                expectedDiagnostics = expectedDiagnostics.Append(
                    // (4,27): warning CS1957: Member 'Derived.M(ref string, out string)' overrides 'Base<string>.M(ref string, out string)'. There are multiple override candidates at run-time. It is implementation dependent which method will be called. Please use a newer runtime.
                    //     public virtual string M(ref Ptring x, out string y) { y = null; return null; }
                    Diagnostic(ErrorCode.WRN_MultipleRuntimeOverrideMatches, "M").WithArguments("Base<string>.M(ref string, out string)", "Derived.M(ref string, out string)").WithLocation(4, 27)
                    ).ToArray();
                warned = true;
            }

            // Only if we warned did we not produce a methodimpl due to https://github.com/dotnet/roslyn/issues/45453
            bool requiresMethodImpl = !warned;

            MetadataReference[] references;
            MetadataReference[] retargetReferences;
            string compilationSource;
            if (useSeparateCompilation)
            {
                var baseCompilation = CreateCompilation(baseSource, references: new[] { corlibRef }, targetFramework: TargetFramework.Empty, parseOptions: parseOptions);
                baseCompilation.VerifyDiagnostics();
                var baseMetadata = baseCompilation.ToMetadataReference();

                references = new[] { corlibRef, baseMetadata };
                retargetReferences = new[] { corlib2Ref, baseMetadata };
                compilationSource = source;

                verify(RetargetingView(baseCompilation, compilationSource, expectedDiagnostics: expectedDiagnostics));
            }
            else
            {
                references = new[] { corlibRef };
                retargetReferences = new[] { corlib2Ref };
                compilationSource = baseSource + source;
            }

            var comp = CreateCompilation(compilationSource, references: references, parseOptions: parseOptions, targetFramework: TargetFramework.Empty);
            comp.VerifyDiagnostics(expectedDiagnostics);

            var member = (SourceMethodSymbol)comp.GlobalNamespace.GetMember("Derived.M");
            bool useMethodImpl = member.RequiresExplicitOverride(out bool shouldWarn);
            Assert.Equal(warned, shouldWarn);
            Assert.Equal(requiresMethodImpl, useMethodImpl);

            verify(SourceView(comp, ""));
            verify(CompilationReferenceView(comp, "", references: references, withoutCorlib: true));
            verify(RetargetingView(comp, "", references: retargetReferences, withoutCorlib: true));
            if (!anyErrors)
                verify(MetadataView(comp, "", references: references, withoutCorlib: true));

            void verify(CSharpCompilation compilation)
            {
                VerifyOverride(compilation,
                    methodName: "Derived.M",
                    overridingMemberDisplay: "System.String Derived.M(ref System.String x, out System.String y)",
                    overriddenMemberDisplay: $"System.{(useCovariantReturns ? "Object" : "String")} Base<System.String>.M(ref System.String x, out System.String y)",
                    requiresMethodimpl: requiresMethodImpl);
            }
        }

        [Theory]
        [CombinatorialData]
        public void OverrideAmbiguities_04(
            bool withCovariantReturnFeatureEnabled,
            bool withCovariantCapableRuntime,
            bool methodRuntimeOverriddenSignatureAmbiguity,
            bool overriddenRuntimeSignatureAmbiguity,
            bool useCovariantReturns,
            bool useSeparateCompilation
            )
        {
            var overriddenMethodReturnType = useCovariantReturns ? "object" : "string";
            var baseSource = $@"
public class Base<Ptring>
{{
    {(overriddenRuntimeSignatureAmbiguity ? @$"public virtual {overriddenMethodReturnType} M(ref Ptring x, ref Ptring y) {{ return null; }}" : "")}
    public virtual {overriddenMethodReturnType} M(ref Ptring x, out string y) {{ y = null; return null; }}
    {(methodRuntimeOverriddenSignatureAmbiguity ? "public virtual string M(out string x, out string y) { x = y = null; return null; }" : "")}
    public virtual string M(out string x, ref Ptring y) {{ x = null; return null; }}
}}
";
            var source = $@"
public class Derived : Base<string>
{{
    public override string M(ref string x, out string y) {{ y = null; return null; }}
}}
";
            var parseOptions = withCovariantReturnFeatureEnabled ? TestOptions.WithCovariantReturns : TestOptions.WithoutCovariantReturns;

            var corlibRef = withCovariantCapableRuntime ? CorelibraryWithCovariantReturnSupport1 : CorelibraryWithoutCovariantReturnSupport1;
            var corlib2Ref = withCovariantCapableRuntime ? CorelibraryWithCovariantReturnSupport2 : CorelibraryWithoutCovariantReturnSupport2;

            var expectedDiagnostics = new DiagnosticDescription[0];
            bool anyErrors = false;
            if (useCovariantReturns)
            {
                if (!withCovariantCapableRuntime)
                {
                    expectedDiagnostics = expectedDiagnostics.Append(
                        // (4,28): error CS8778: 'Derived.M(ref string, out string)': Target runtime doesn't support covariant return types in overrides. Return type must be 'object' to match overridden member 'Base<string>.M(ref string, out string)'
                        //     public override string M(ref string x, out string y) { y = null; return null; }
                        Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportCovariantReturnsOfClasses, "M").WithArguments("Derived.M(ref string, out string)", "Base<string>.M(ref string, out string)", "object")
                        ).ToArray();
                    anyErrors = true;
                }
                else if (!withCovariantReturnFeatureEnabled)
                {
                    expectedDiagnostics = expectedDiagnostics.Append(
                        // (12,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                        //     public override string M(ref string x, out string y) { y = null; return null; }
                        Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M").WithArguments("covariant returns", "9.0")
                        ).ToArray();
                    anyErrors = true;
                }
            }
            bool warned = false;
            if (!withCovariantCapableRuntime && !useCovariantReturns)
            {
                expectedDiagnostics = expectedDiagnostics.Append(
                    // (5,27): warning CS1957: Member 'Derived.M(ref string, out string)' overrides 'Base<string>.M(ref string, out string)'. There are multiple override candidates at run-time. It is implementation dependent which method will be called. Please use a newer runtime.
                    //     public virtual string M(ref Ptring x, out string y) { y = null; return null; }
                    Diagnostic(ErrorCode.WRN_MultipleRuntimeOverrideMatches, "M").WithArguments("Base<string>.M(ref string, out string)", "Derived.M(ref string, out string)").WithLocation(5, 27)
                    ).ToArray();
                warned = true;
            }

            // Only if we warned did we not produce a methodimpl due to https://github.com/dotnet/roslyn/issues/45453
            bool requiresMethodImpl = !warned;
            MetadataReference[] references;
            MetadataReference[] retargetReferences;
            string compilationSource;

            if (useSeparateCompilation)
            {
                var baseCompilation = CreateCompilation(baseSource, references: new[] { corlibRef }, targetFramework: TargetFramework.Empty, parseOptions: parseOptions);
                baseCompilation.VerifyDiagnostics();
                var baseMetadata = baseCompilation.ToMetadataReference();

                references = new[] { corlibRef, baseMetadata };
                retargetReferences = new[] { corlib2Ref, baseMetadata };
                compilationSource = source;

                verify(RetargetingView(baseCompilation, compilationSource, expectedDiagnostics: expectedDiagnostics));
            }
            else
            {
                references = new[] { corlibRef };
                retargetReferences = new[] { corlib2Ref };
                compilationSource = baseSource + source;
            }

            var comp = CreateCompilation(compilationSource, references: references, parseOptions: parseOptions, targetFramework: TargetFramework.Empty);
            comp.VerifyDiagnostics(expectedDiagnostics);

            var member = (SourceMethodSymbol)comp.GlobalNamespace.GetMember("Derived.M");
            bool useMethodImpl = member.RequiresExplicitOverride(out bool shouldWarn);
            Assert.Equal(warned, shouldWarn);
            Assert.Equal(requiresMethodImpl, useMethodImpl);

            verify(SourceView(comp, ""));
            verify(CompilationReferenceView(comp, "", references: references, withoutCorlib: true));
            if (!useCovariantReturns)
                verify(RetargetingView(comp, "", references: retargetReferences, withoutCorlib: true));
            if (!anyErrors)
                verify(MetadataView(comp, "", references: references, withoutCorlib: true));

            void verify(CSharpCompilation compilation)
            {
                var lastReference = compilation.GetAssemblyOrModuleSymbol(compilation.References.Last());
                // Due to https://github.com/dotnet/roslyn/issues/45566 retargeting methods do not resolve properly for this scenario
                var isRetargeting = lastReference is RetargetingAssemblySymbol;
                // Similarly, there is probably a corresponding bug in resolving PE method symbols
                var isMetadata = lastReference is PEAssemblySymbol;
                if (isRetargeting || isMetadata)
                    return;
                VerifyOverride(compilation,
                    methodName: "Derived.M",
                    overridingMemberDisplay: "System.String Derived.M(ref System.String x, out System.String y)",
                    overriddenMemberDisplay: $"System.{(useCovariantReturns ? "Object" : "String")} Base<System.String>.M(ref System.String x, out System.String y)",
                    requiresMethodimpl: requiresMethodImpl);
            }
        }

        [Theory]
        [CombinatorialData]
        public void OverrideAmbiguities_05(
            bool withCovariantReturnFeatureEnabled,
            bool withCovariantCapableRuntime,
            bool useSeparateCompilation
            )
        {
            var baseSource = $@"
public class Base1<Ptring>
{{
    public virtual string M(out string y) {{ y = null; return null; }}
    public virtual string M(ref Ptring y) {{ return null; }}
}}
public class Base2<Ptring> : Base1<Ptring>
{{
    public virtual new object M(out string y) {{ y = null; return null; }}
}}
";
            var source = $@"
public class Derived : Base2<string>
{{
    public override string M(out string y) {{ y = null; return null; }}
}}
";
            var parseOptions = withCovariantReturnFeatureEnabled ? TestOptions.WithCovariantReturns : TestOptions.WithoutCovariantReturns;

            var corlibRef = withCovariantCapableRuntime ? CorelibraryWithCovariantReturnSupport1 : CorelibraryWithoutCovariantReturnSupport1;
            var corlib2Ref = withCovariantCapableRuntime ? CorelibraryWithCovariantReturnSupport2 : CorelibraryWithoutCovariantReturnSupport2;

            var expectedDiagnostics = new DiagnosticDescription[0];
            bool anyErrors = false;
            if (!withCovariantCapableRuntime)
            {
                expectedDiagnostics = expectedDiagnostics.Append(
                    // (4,28): error CS8778: 'Derived.M(out string)': Target runtime doesn't support covariant return types in overrides. Return type must be 'object' to match overridden member 'Base2<string>.M(out string)'
                    //     public override string M(out string y) { y = null; return null; }
                    Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportCovariantReturnsOfClasses, "M").WithArguments("Derived.M(out string)", "Base2<string>.M(out string)", "object")
                    ).ToArray();
                anyErrors = true;
            }
            else if (!withCovariantReturnFeatureEnabled)
            {
                expectedDiagnostics = expectedDiagnostics.Append(
                    // (14,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                    //     public override string M(out string y) { y = null; return null; }
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M").WithArguments("covariant returns", "9.0")
                    ).ToArray();
                anyErrors = true;
            }

            // All of the overrides in this test require a methodimpl because they are on a different class from the runtime override.
            bool requiresMethodImpl = true;

            MetadataReference[] references;
            MetadataReference[] retargetReferences;
            string compilationSource;

            if (useSeparateCompilation)
            {
                var baseCompilation = CreateCompilation(baseSource, references: new[] { corlibRef }, targetFramework: TargetFramework.Empty, parseOptions: parseOptions);
                baseCompilation.VerifyDiagnostics();
                var baseMetadata = baseCompilation.ToMetadataReference();

                references = new[] { corlibRef, baseMetadata };
                retargetReferences = new[] { corlib2Ref, baseMetadata };
                compilationSource = source;

                verify(RetargetingView(baseCompilation, compilationSource, expectedDiagnostics: expectedDiagnostics));
            }
            else
            {
                references = new[] { corlibRef };
                retargetReferences = new[] { corlib2Ref };
                compilationSource = baseSource + source;
            }

            var comp = CreateCompilation(compilationSource, references: references, parseOptions: parseOptions, targetFramework: TargetFramework.Empty);
            comp.VerifyDiagnostics(expectedDiagnostics);

            var member = (SourceMethodSymbol)comp.GlobalNamespace.GetMember("Derived.M");
            bool useMethodImpl = member.RequiresExplicitOverride(out bool shouldWarn);
            Assert.False(shouldWarn);
            Assert.Equal(requiresMethodImpl, useMethodImpl);

            verify(SourceView(comp, ""));
            verify(CompilationReferenceView(comp, "", references: references, withoutCorlib: true));
            verify(RetargetingView(comp, "", references: retargetReferences, withoutCorlib: true));
            if (!anyErrors)
                verify(MetadataView(comp, "", references: references, withoutCorlib: true));

            void verify(CSharpCompilation compilation)
            {
                VerifyOverride(compilation,
                    methodName: "Derived.M",
                    overridingMemberDisplay: "System.String Derived.M(out System.String y)",
                    overriddenMemberDisplay: "System.Object Base2<System.String>.M(out System.String y)",
                    requiresMethodimpl: requiresMethodImpl);
            }
        }

        [Theory]
        [CombinatorialData]
        public void OverrideAmbiguities_06(
            bool withCovariantReturnFeatureEnabled,
            bool withCovariantCapableRuntime,
            bool withPropertyDeclarationFirst,
            bool overrideProperty,
            bool useCovariantReturns,
            bool useSeparateCompilation
            )
        {
            var propertyDeclaration = "public virtual Pbject Prop => default(Pbject);";
            var methodDeclaration = "public virtual object get_Prop() => default(object);";
            var baseSource = $@"
public class Base<Pbject>
{{
    {(withPropertyDeclarationFirst ? propertyDeclaration : "")}
    {methodDeclaration}
    {(withPropertyDeclarationFirst ? "" : propertyDeclaration)}
}}
";
            var overrideReturnType = useCovariantReturns ? "string" : "object";
            var propertyOverride = $"public override {overrideReturnType} Prop => null;";
            var methodOverride = $"public override {overrideReturnType} get_Prop() => null;";
            var source = $@"
public class Derived : Base<object>
{{
    {(overrideProperty ? propertyOverride : methodOverride)}
}}
";
            var parseOptions = withCovariantReturnFeatureEnabled ? TestOptions.WithCovariantReturns : TestOptions.WithoutCovariantReturns;

            var corlibRef = withCovariantCapableRuntime ? CorelibraryWithCovariantReturnSupport1 : CorelibraryWithoutCovariantReturnSupport1;
            var corlib2Ref = withCovariantCapableRuntime ? CorelibraryWithCovariantReturnSupport2 : CorelibraryWithoutCovariantReturnSupport2;

            var expectedDiagnostics = new DiagnosticDescription[0];
            if (useCovariantReturns)
            {
                if (!withCovariantCapableRuntime)
                {
                    if (overrideProperty)
                    {
                        expectedDiagnostics = expectedDiagnostics.Append(
                            // (4,28): error CS8779: 'Derived.Prop': Target runtime doesn't support covariant types in overrides. Type must be 'object' to match overridden member 'Base<object>.Prop'
                            //     public override string Prop => null;
                            Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportCovariantPropertiesOfClasses, "Prop").WithArguments("Derived.Prop", "Base<object>.Prop", "object")
                            ).ToArray();
                    }
                    else
                    {
                        // This is treated as a suppressed cascaded diagnostic and therefore not reported.
                    }
                }
                else if (!withCovariantReturnFeatureEnabled)
                {
                    if (overrideProperty)
                    {
                        expectedDiagnostics = expectedDiagnostics.Append(
                            // (4,28): error CS8400: Feature 'covariant returns' is not available in C# 8.0. Please use language version 9.0 or greater.
                            //     public override string Prop => null;
                            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "Prop").WithArguments("covariant returns", "9.0")
                            ).ToArray();
                    }
                    else
                    {
                        // This is treated as a suppressed cascaded diagnostic and therefore not reported.
                    }
                }
            }

            // The ERR_AmbigOverride errors are all cascaded diagnostics, a consequence of ERR_MemberReserved in Base.
            if (overrideProperty)
            {
                if (!useCovariantReturns ||
                    useCovariantReturns && withCovariantReturnFeatureEnabled && withCovariantCapableRuntime)
                {
                    expectedDiagnostics = expectedDiagnostics.Append(
                        // (4,36): error CS0462: The inherited members 'Base<Pbject>.Prop.get' and 'Base<Pbject>.get_Prop()' have the same signature in type 'Derived', so they cannot be overridden
                        //     public override object Prop => null;
                        Diagnostic(ErrorCode.ERR_AmbigOverride, "null").WithArguments("Base<Pbject>.Prop.get", "Base<Pbject>.get_Prop()", "Derived")
                        ).ToArray();
                }
            }
            else
            {
                expectedDiagnostics = expectedDiagnostics.Append(
                    // (4,28): error CS0462: The inherited members 'Base<Pbject>.get_Prop()' and 'Base<Pbject>.Prop.get' have the same signature in type 'Derived', so they cannot be overridden
                    //     public override object get_Prop() => null;
                    Diagnostic(ErrorCode.ERR_AmbigOverride, "get_Prop").WithArguments("Base<Pbject>.get_Prop()", "Base<Pbject>.Prop.get", "Derived")
                    ).ToArray();
            }

            bool deservesAmbiguousOverrideWarning = !withCovariantCapableRuntime && !useCovariantReturns;

            if (deservesAmbiguousOverrideWarning)
            {
                if (overrideProperty)
                {
                    expectedDiagnostics = expectedDiagnostics.Append(
                    // (6,35): warning CS1957: Member 'Derived.Prop.get' overrides 'Base<object>.Prop.get'. There are multiple override candidates at run-time. It is implementation dependent which method will be called. Please use a newer runtime.
                    //     public virtual Pbject Prop => default(Pbject);
                    Diagnostic(ErrorCode.WRN_MultipleRuntimeOverrideMatches, "default(Pbject)").WithArguments("Base<object>.Prop.get", "Derived.Prop.get")
                        ).ToArray();
                }
                else
                {
                    expectedDiagnostics = expectedDiagnostics.Append(
                        // (5,27): warning CS1957: Member 'Derived.get_Prop()' overrides 'Base<object>.get_Prop()'. There are multiple override candidates at run-time. It is implementation dependent which method will be called. Please use a newer runtime.
                        //     public virtual object get_Prop() => default(object);
                        Diagnostic(ErrorCode.WRN_MultipleRuntimeOverrideMatches, "get_Prop").WithArguments("Base<object>.get_Prop()", "Derived.get_Prop()")
                        ).ToArray();
                }
            }

            bool requiresMethodImpl = useCovariantReturns || withCovariantCapableRuntime || withPropertyDeclarationFirst != overrideProperty;

            MetadataReference[] references;
            MetadataReference[] retargetReferences;
            string compilationSource;

            if (useSeparateCompilation)
            {
                var baseCompilation = CreateCompilation(baseSource, references: new[] { corlibRef }, targetFramework: TargetFramework.Empty, parseOptions: parseOptions);
                var baseDiagnostic =
                    // (6,35): error CS0082: Type 'Base<Pbject>' already reserves a member called 'get_Prop' with the same parameter types
                    //     public virtual Pbject Prop => default(Pbject);
                    Diagnostic(ErrorCode.ERR_MemberReserved, "default(Pbject)").WithArguments("get_Prop", "Base<Pbject>").WithLocation(withPropertyDeclarationFirst ? 4 : 6, 35);
                baseCompilation.VerifyDiagnostics(baseDiagnostic);
                var baseMetadata = baseCompilation.ToMetadataReference();

                references = new[] { corlibRef, baseMetadata };
                retargetReferences = new[] { corlib2Ref, baseMetadata };
                compilationSource = source;

                verify(RetargetingView(baseCompilation, compilationSource, expectedDiagnostics: expectedDiagnostics));
            }
            else
            {
                references = new[] { corlibRef };
                retargetReferences = new[] { corlib2Ref };
                compilationSource = baseSource + source;
                expectedDiagnostics = expectedDiagnostics.Prepend(
                    // (6,35): error CS0082: Type 'Base<Pbject>' already reserves a member called 'get_Prop' with the same parameter types
                    //     public virtual Pbject Prop => default(Pbject);
                    Diagnostic(ErrorCode.ERR_MemberReserved, "default(Pbject)").WithArguments("get_Prop", "Base<Pbject>").WithLocation(withPropertyDeclarationFirst ? 4 : 6, 35)
                    ).ToArray();
            }

            var comp = CreateCompilation(compilationSource, references: references, parseOptions: parseOptions, targetFramework: TargetFramework.Empty);
            comp.VerifyDiagnostics(expectedDiagnostics);

            var member = (SourceMethodSymbol)comp.GlobalNamespace.GetMember("Derived.get_Prop");
            bool useMethodImpl = member.RequiresExplicitOverride(out bool shouldWarn);
            Assert.Equal(deservesAmbiguousOverrideWarning, shouldWarn);
            Assert.Equal(requiresMethodImpl, useMethodImpl);

            verify(SourceView(comp, ""));
            verify(CompilationReferenceView(comp, "", references: references, withoutCorlib: true));
            if (overrideProperty == withPropertyDeclarationFirst)
            {
                verify(RetargetingView(comp, "", references: retargetReferences, withoutCorlib: true));
            }
            else
            {
                // retargeting tests skipped due to https://github.com/dotnet/roslyn/issues/45566
            }

            void verify(CSharpCompilation compilation)
            {
                var overrideReturnType = useCovariantReturns ? "String" : "Object";
                if (overrideProperty)
                {
                    VerifyOverride(compilation,
                        methodName: "Derived.get_Prop",
                        overridingMemberDisplay: $"System.{overrideReturnType} Derived.Prop.get",
                        overriddenMemberDisplay: "System.Object Base<System.Object>.Prop.get",
                        requiresMethodimpl: requiresMethodImpl);
                }
                else
                {
                    VerifyOverride(compilation,
                        methodName: "Derived.get_Prop",
                        overridingMemberDisplay: $"System.{overrideReturnType} Derived.get_Prop()",
                        overriddenMemberDisplay: "System.Object Base<System.Object>.get_Prop()",
                        requiresMethodimpl: requiresMethodImpl);
                }
            }
        }

        [Theory]
        [CombinatorialData]
        public void DuplicateDeclarations_01(
            bool withCovariantReturns
            )
        {
            var source = @"
class Base
{
    public virtual void M(int x) => throw null; // 1
    public virtual void M(int y) => throw null; // 2
}

class Derived : Base
{
    public override void M(int z) => throw null; // 3
}
";
            var comp = CreateCompilation(withCovariantReturns: withCovariantReturns, source);
            comp.VerifyDiagnostics(
                // (5,25): error CS0111: Type 'Base' already defines a member called 'M' with the same parameter types
                //     public virtual void M(int y) => throw null; // 2
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M").WithArguments("M", "Base").WithLocation(5, 25)
                );

            verify(SourceView(comp, ""));
            verify(CompilationReferenceView(comp, ""));
            verify(RetargetingView(comp, ""));

            void verify(CSharpCompilation compilation)
            {
                var member = (MethodSymbol)comp.GlobalNamespace.GetMember("Derived.M");
                bool useMethodImpl = member.RequiresExplicitOverride(out bool shouldWarn);
                Assert.False(shouldWarn);
                Assert.Equal(withCovariantReturns, useMethodImpl);
                Assert.Equal(withCovariantReturns, member.IsMetadataNewSlot());

                VerifyOverride(compilation,
                    methodName: "Derived.M",
                    overridingMemberDisplay: "void Derived.M(System.Int32 z)",
                    overriddenMemberDisplay: "void Base.M(System.Int32 x)",
                    requiresMethodimpl: useMethodImpl);
            }
        }

        [Theory]
        [CombinatorialData]
        public void DuplicateDeclarations_02(
            bool withCovariantReturns
            )
        {
            var source = @"
class Base<T>
{
    public virtual void M(int x) => throw null; // 1
    public virtual void M(int y) => throw null; // 2
}

class Derived : Base<int>
{
    public override void M(int z) => throw null; // 3
}
";
            var comp = CreateCompilation(withCovariantReturns: withCovariantReturns, source);
            comp.VerifyDiagnostics(
                // (5,25): error CS0111: Type 'Base<T>' already defines a member called 'M' with the same parameter types
                //     public virtual void M(int y) => throw null; // 2
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M").WithArguments("M", "Base<T>").WithLocation(5, 25),
                // (10,26): error CS0462: The inherited members 'Base<T>.M(int)' and 'Base<T>.M(int)' have the same signature in type 'Derived', so they cannot be overridden
                //     public override void M(int z) => throw null; // 3
                Diagnostic(ErrorCode.ERR_AmbigOverride, "M").WithArguments("Base<T>.M(int)", "Base<T>.M(int)", "Derived").WithLocation(10, 26)
                );

            verify(SourceView(comp, ""));
            verify(CompilationReferenceView(comp, ""));
            verify(RetargetingView(comp, ""));

            void verify(CSharpCompilation compilation)
            {
                var member = (MethodSymbol)comp.GlobalNamespace.GetMember("Derived.M");
                bool useMethodImpl = member.RequiresExplicitOverride(out bool shouldWarn);
                Assert.False(shouldWarn);
                Assert.Equal(withCovariantReturns, useMethodImpl);
                Assert.Equal(withCovariantReturns, member.IsMetadataNewSlot());

                VerifyOverride(compilation,
                    methodName: "Derived.M",
                    overridingMemberDisplay: "void Derived.M(System.Int32 z)",
                    overriddenMemberDisplay: "void Base<System.Int32>.M(System.Int32 x)",
                    requiresMethodimpl: useMethodImpl);
            }
        }

        [Theory]
        [CombinatorialData]
        public void DuplicateDeclarations_03(
            bool withCovariantReturns
            )
        {
            var source = @"
class Container<T>
{
    class Base
    {
        public virtual void M(int x) => throw null; // 1
        public virtual void M(int y) => throw null; // 2
    }

    class Derived : Base
    {
        public override void M(int z) => throw null; // 3
    }
}
";
            var comp = CreateCompilation(withCovariantReturns: withCovariantReturns, source);
            comp.VerifyDiagnostics(
                // (7,29): error CS0111: Type 'Container<T>.Base' already defines a member called 'M' with the same parameter types
                //         public virtual void M(int y) => throw null; // 2
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M").WithArguments("M", "Container<T>.Base").WithLocation(7, 29)
                );

            verify(SourceView(comp, ""));
            verify(CompilationReferenceView(comp, ""));
            verify(RetargetingView(comp, ""));

            void verify(CSharpCompilation compilation)
            {
                var member = (MethodSymbol)comp.GlobalNamespace.GetMember("Container.Derived.M");
                bool useMethodImpl = member.RequiresExplicitOverride(out bool shouldWarn);
                Assert.False(shouldWarn);
                Assert.Equal(withCovariantReturns, useMethodImpl);
                Assert.Equal(useMethodImpl, member.IsMetadataNewSlot());

                VerifyOverride(compilation,
                    methodName: "Container.Derived.M",
                    overridingMemberDisplay: "void Container<T>.Derived.M(System.Int32 z)",
                    overriddenMemberDisplay: "void Container<T>.Base.M(System.Int32 x)",
                    requiresMethodimpl: useMethodImpl);
            }
        }

        [Theory]
        [CombinatorialData]
        public void DuplicateDeclarations_04(
            bool withCovariantReturns
            )
        {
            var s0 = @"
public class Container<T>
{
    public class Base
    {
        public virtual void M(int x) => throw null; // 1
        public virtual void M(int y) => throw null; // 2
    }
}
";
            var baseCompilation = CreateCompilation(withCovariantReturns: withCovariantReturns, s0);
            baseCompilation.VerifyDiagnostics(
                // (7,29): error CS0111: Type 'Container<T>.Base' already defines a member called 'M' with the same parameter types
                //         public virtual void M(int y) => throw null; // 2
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M").WithArguments("M", "Container<T>.Base").WithLocation(7, 29)
                );
            var baseMetadata = baseCompilation.ToMetadataReference();

            var source = @"
class Derived : Container<int>.Base
{
    public override void M(int z) => throw null; // 3
}
";
            var comp = CreateCompilation(withCovariantReturns: withCovariantReturns, source, references: new[] { baseMetadata });
            comp.VerifyDiagnostics(
                // (4,26): error CS0462: The inherited members 'Container<T>.Base.M(int)' and 'Container<T>.Base.M(int)' have the same signature in type 'Derived', so they cannot be overridden
                //     public override void M(int z) => throw null; // 3
                Diagnostic(ErrorCode.ERR_AmbigOverride, "M").WithArguments("Container<T>.Base.M(int)", "Container<T>.Base.M(int)", "Derived").WithLocation(4, 26)
                );

            verify(RetargetingView(baseCompilation, source, expectedDiagnostics:
                // (4,26): error CS0462: The inherited members 'Container<T>.Base.M(int)' and 'Container<T>.Base.M(int)' have the same signature in type 'Derived', so they cannot be overridden
                //     public override void M(int z) => throw null; // 3
                Diagnostic(ErrorCode.ERR_AmbigOverride, "M").WithArguments("Container<T>.Base.M(int)", "Container<T>.Base.M(int)", "Derived").WithLocation(4, 26)
                ));

            verify(SourceView(comp, ""));
            verify(CompilationReferenceView(comp, "", new[] { baseMetadata }));
            verify(RetargetingView(comp, "", new[] { baseMetadata }));

            void verify(CSharpCompilation compilation)
            {
                var member = (MethodSymbol)comp.GlobalNamespace.GetMember("Derived.M");
                bool useMethodImpl = member.RequiresExplicitOverride(out bool shouldWarn);
                Assert.False(shouldWarn);
                Assert.Equal(withCovariantReturns, useMethodImpl);

                VerifyOverride(compilation,
                    methodName: "Derived.M",
                    overridingMemberDisplay: "void Derived.M(System.Int32 z)",
                    overriddenMemberDisplay: "void Container<System.Int32>.Base.M(System.Int32 x)",
                    requiresMethodimpl: useMethodImpl);
            }
        }

        [Theory]
        [CombinatorialData]
        public void DuplicateDeclarations_05(
            bool withCovariantReturns
            )
        {
            var s0 = @"
public class Container<T>
{
    public class Base
    {
        public virtual void M(int x) => throw null; // 1
        public virtual void M(T y) => throw null;   // 2
        public virtual void M(int z) => throw null; // 3
    }
}
";
            var baseCompilation = CreateCompilation(withCovariantReturns: withCovariantReturns, s0);
            baseCompilation.VerifyDiagnostics(
                // (8,29): error CS0111: Type 'Container<T>.Base' already defines a member called 'M' with the same parameter types
                //         public virtual void M(int z) => throw null; // 3
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M").WithArguments("M", "Container<T>.Base").WithLocation(8, 29)
                );
            var baseMetadata = baseCompilation.ToMetadataReference();

            var source = @"
class Derived : Container<int>.Base
{
    public override void M(int w) => throw null; // 4
}
";
            var comp = CreateCompilation(withCovariantReturns: withCovariantReturns, source, references: new[] { baseMetadata });
            comp.VerifyDiagnostics(
                // (4,26): error CS0462: The inherited members 'Container<T>.Base.M(int)' and 'Container<T>.Base.M(T)' have the same signature in type 'Derived', so they cannot be overridden
                //     public override void M(int w) => throw null; // 4
                Diagnostic(ErrorCode.ERR_AmbigOverride, "M").WithArguments("Container<T>.Base.M(int)", "Container<T>.Base.M(T)", "Derived").WithLocation(4, 26)
                );

            verify(RetargetingView(baseCompilation, source, expectedDiagnostics:
                // (4,26): error CS0462: The inherited members 'Container<T>.Base.M(int)' and 'Container<T>.Base.M(T)' have the same signature in type 'Derived', so they cannot be overridden
                //     public override void M(int w) => throw null; // 4
                Diagnostic(ErrorCode.ERR_AmbigOverride, "M").WithArguments("Container<T>.Base.M(int)", "Container<T>.Base.M(T)", "Derived").WithLocation(4, 26)
                ));

            verify(SourceView(comp, ""));
            verify(CompilationReferenceView(comp, "", new[] { baseMetadata }));
            verify(RetargetingView(comp, "", references: new[] { baseMetadata }));

            void verify(CSharpCompilation compilation)
            {
                var member = (MethodSymbol)comp.GlobalNamespace.GetMember("Derived.M");
                bool useMethodImpl = member.RequiresExplicitOverride(out bool shouldWarn);
                Assert.False(shouldWarn);
                Assert.Equal(withCovariantReturns, useMethodImpl);

                VerifyOverride(compilation,
                    methodName: "Derived.M",
                    overridingMemberDisplay: "void Derived.M(System.Int32 w)",
                    overriddenMemberDisplay: "void Container<System.Int32>.Base.M(System.Int32 x)",
                    requiresMethodimpl: useMethodImpl);
            }
        }

        [Theory]
        [CombinatorialData]
        public void DuplicateDeclarations_06(
            bool withCovariantReturns
            )
        {
            var s0 = @"
public class Root
{
    public virtual int get_P() => throw null; // 1
    public virtual int get_P() => throw null; // 2
}

public class Base : Root
{
    public virtual int P => 1;
}
";
            var baseCompilation = CreateCompilation(withCovariantReturns: withCovariantReturns, s0);
            baseCompilation.VerifyDiagnostics(
                // (5,24): error CS0111: Type 'Root' already defines a member called 'get_P' with the same parameter types
                //     public virtual int get_P() => throw null; // 2
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "get_P").WithArguments("get_P", "Root").WithLocation(5, 24)
                );
            var baseMetadata = baseCompilation.ToMetadataReference();

            var source = @"
class Derived : Base
{
    public override int get_P() => throw null; // 3
}
";
            var comp = CreateCompilation(withCovariantReturns: withCovariantReturns, source, references: new[] { baseMetadata });
            comp.VerifyDiagnostics(
                );

            verify(RetargetingView(baseCompilation, source));

            verify(SourceView(comp, ""));
            verify(CompilationReferenceView(comp, "", new[] { baseMetadata }));
            verify(RetargetingView(comp, "", references: new[] { baseMetadata }));

            void verify(CSharpCompilation compilation)
            {
                var member = (SourceMethodSymbol)comp.GlobalNamespace.GetMember("Derived.get_P");
                bool useMethodImpl = member.RequiresExplicitOverride(out bool shouldWarn);
                Assert.False(shouldWarn);
                Assert.True(useMethodImpl);

                VerifyOverride(compilation,
                    methodName: "Derived.get_P",
                    overridingMemberDisplay: "System.Int32 Derived.get_P()",
                    overriddenMemberDisplay: "System.Int32 Root.get_P()",
                    requiresMethodimpl: useMethodImpl);
            }
        }

        [Theory]
        [CombinatorialData]
        public void DuplicateDeclarations_07(
            bool withCovariantReturns
            )
        {
            var s0 = @"
public class Root<T>
{
    public virtual int get_P() => throw null; // 1
    public virtual int get_P() => throw null; // 2
}

public class Base : Root<int>
{
    public virtual int P => 1;
}
";
            var baseCompilation = CreateCompilation(withCovariantReturns: withCovariantReturns, s0);
            baseCompilation.VerifyDiagnostics(
                // (5,24): error CS0111: Type 'Root<T>' already defines a member called 'get_P' with the same parameter types
                //     public virtual int get_P() => throw null; // 2
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "get_P").WithArguments("get_P", "Root<T>").WithLocation(5, 24)
                );
            var baseMetadata = baseCompilation.ToMetadataReference();

            var source = @"
class Derived : Base
{
    public override int get_P() => throw null; // 3
}
";
            var comp = CreateCompilation(withCovariantReturns: withCovariantReturns, source, references: new[] { baseMetadata });
            comp.VerifyDiagnostics(
                // (4,25): error CS0462: The inherited members 'Root<T>.get_P()' and 'Root<T>.get_P()' have the same signature in type 'Derived', so they cannot be overridden
                //     public override int get_P() => throw null; // 3
                Diagnostic(ErrorCode.ERR_AmbigOverride, "get_P").WithArguments("Root<T>.get_P()", "Root<T>.get_P()", "Derived").WithLocation(4, 25)
                );

            verify(RetargetingView(baseCompilation, source, expectedDiagnostics:
                // (4,25): error CS0462: The inherited members 'Root<T>.get_P()' and 'Root<T>.get_P()' have the same signature in type 'Derived', so they cannot be overridden
                //     public override int get_P() => throw null; // 3
                Diagnostic(ErrorCode.ERR_AmbigOverride, "get_P").WithArguments("Root<T>.get_P()", "Root<T>.get_P()", "Derived").WithLocation(4, 25)
                ));

            verify(SourceView(comp, ""));
            verify(CompilationReferenceView(comp, "", new[] { baseMetadata }));
            verify(RetargetingView(comp, "", references: new[] { baseMetadata }));

            void verify(CSharpCompilation compilation)
            {
                var member = (MethodSymbol)comp.GlobalNamespace.GetMember("Derived.get_P");
                bool useMethodImpl = member.RequiresExplicitOverride(out bool shouldWarn);
                Assert.False(shouldWarn);
                Assert.True(useMethodImpl);

                VerifyOverride(compilation,
                    methodName: "Derived.get_P",
                    overridingMemberDisplay: "System.Int32 Derived.get_P()",
                    overriddenMemberDisplay: "System.Int32 Root<System.Int32>.get_P()",
                    requiresMethodimpl: useMethodImpl);
            }
        }

        [Theory]
        [CombinatorialData]
        public void DuplicateDeclarations_08(
            bool withCovariantReturns
            )
        {
            var s0 = @"
public class Root<T>
{
    public virtual void M(ref int x) => throw null; // 1
    public virtual void M(ref T y) => throw null;   // 2
    public virtual void M(ref int z) => throw null; // 3
}
public class Base : Root<int>
{
    public virtual void M(out int w) { w = 0; }     // 4
}
";
            var baseCompilation = CreateCompilation(withCovariantReturns: withCovariantReturns, s0);
            baseCompilation.VerifyDiagnostics(
                // (6,25): error CS0111: Type 'Root<T>' already defines a member called 'M' with the same parameter types
                //     public virtual void M(ref int z) => throw null; // 3
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M").WithArguments("M", "Root<T>").WithLocation(6, 25)
                );
            var baseMetadata = baseCompilation.ToMetadataReference();

            var source = @"
class Derived : Base
{
    public override void M(ref int a) => throw null; // 5
}
";
            var comp = CreateCompilation(withCovariantReturns: withCovariantReturns, source, references: new[] { baseMetadata });
            comp.VerifyDiagnostics(
                // (4,26): error CS0462: The inherited members 'Root<T>.M(ref int)' and 'Root<T>.M(ref T)' have the same signature in type 'Derived', so they cannot be overridden
                //     public override void M(ref int a) => throw null; // 5
                Diagnostic(ErrorCode.ERR_AmbigOverride, "M").WithArguments("Root<T>.M(ref int)", "Root<T>.M(ref T)", "Derived").WithLocation(4, 26)
                );

            verify(RetargetingView(baseCompilation, source, expectedDiagnostics:
                // (4,26): error CS0462: The inherited members 'Root<T>.M(ref int)' and 'Root<T>.M(ref T)' have the same signature in type 'Derived', so they cannot be overridden
                //     public override void M(ref int a) => throw null; // 5
                Diagnostic(ErrorCode.ERR_AmbigOverride, "M").WithArguments("Root<T>.M(ref int)", "Root<T>.M(ref T)", "Derived").WithLocation(4, 26)
                ));

            verify(SourceView(comp, ""));
            verify(CompilationReferenceView(comp, "", new[] { baseMetadata }));
            verify(RetargetingView(comp, "", references: new[] { baseMetadata }));

            void verify(CSharpCompilation compilation)
            {
                var member = (MethodSymbol)comp.GlobalNamespace.GetMember("Derived.M");
                bool useMethodImpl = member.RequiresExplicitOverride(out bool shouldWarn);
                Assert.False(shouldWarn);
                Assert.True(useMethodImpl);

                VerifyOverride(compilation,
                    methodName: "Derived.M",
                    overridingMemberDisplay: "void Derived.M(ref System.Int32 a)",
                    overriddenMemberDisplay: "void Root<System.Int32>.M(ref System.Int32 x)",
                    requiresMethodimpl: useMethodImpl);
            }
        }
    }
}
