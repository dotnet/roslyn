// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class NativeIntegerTests : CSharpTestBase
    {
        [Fact]
        public void LanguageVersion()
        {
            var source =
@"interface I
{
    nint Add(nint x, nuint y);
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (3,5): error CS8652: The feature 'native-sized integers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     nint Add(nint x, nuint y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "nint").WithArguments("native-sized integers").WithLocation(3, 5),
                // (3,14): error CS8652: The feature 'native-sized integers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     nint Add(nint x, nuint y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "nint").WithArguments("native-sized integers").WithLocation(3, 14),
                // (3,22): error CS8652: The feature 'native-sized integers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     nint Add(nint x, nuint y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "nuint").WithArguments("native-sized integers").WithLocation(3, 22));

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        /// <summary>
        /// System.IntPtr and System.UIntPtr definitions from metadata.
        /// </summary>
        [Fact]
        public void TypeDefinitions_FromMetadata()
        {
            var source =
@"interface I
{
    void F1(System.IntPtr x, nint y);
    void F2(System.UIntPtr x, nuint y);
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            var type = comp.GetTypeByMetadataName("System.IntPtr");
            VerifyType(type, signed: true, isNativeInt: false);
            VerifyType(type.GetPublicSymbol(), signed: true, isNativeInt: false);

            type = comp.GetTypeByMetadataName("System.UIntPtr");
            VerifyType(type, signed: false, isNativeInt: false);
            VerifyType(type.GetPublicSymbol(), signed: false, isNativeInt: false);

            var method = comp.GetMember<MethodSymbol>("I.F1");
            Assert.Equal("void I.F1(System.IntPtr x, nint y)", method.ToTestDisplayString());
            Assert.Equal("Sub I.F1(x As System.IntPtr, y As System.IntPtr)", VisualBasic.SymbolDisplay.ToDisplayString(method.GetPublicSymbol(), SymbolDisplayFormat.TestFormat));
            VerifyTypes((NamedTypeSymbol)method.Parameters[0].Type, (NamedTypeSymbol)method.Parameters[1].Type, signed: true);

            method = comp.GetMember<MethodSymbol>("I.F2");
            Assert.Equal("void I.F2(System.UIntPtr x, nuint y)", method.ToTestDisplayString());
            Assert.Equal("Sub I.F2(x As System.UIntPtr, y As System.UIntPtr)", VisualBasic.SymbolDisplay.ToDisplayString(method.GetPublicSymbol(), SymbolDisplayFormat.TestFormat));
            VerifyTypes((NamedTypeSymbol)method.Parameters[0].Type, (NamedTypeSymbol)method.Parameters[1].Type, signed: false);
        }

        /// <summary>
        /// System.IntPtr and System.UIntPtr definitions from source.
        /// </summary>
        [Fact]
        public void TypeDefinitions_FromSource()
        {
            var sourceA =
@"namespace System
{
    public class Object
    {
        public virtual string ToString() => null;
        public virtual int GetHashCode() => 0;
        public virtual bool Equals(object obj) => false;
    }
    public class String { }
    public abstract class ValueType
    {
        public override string ToString() => null;
        public override int GetHashCode() => 0;
        public override bool Equals(object obj) => false;
    }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public interface IEquatable<T>
    {
        bool Equals(T other);
    }
    public struct IntPtr : IEquatable<IntPtr>
    {
        public override string ToString() => null;
        public override int GetHashCode() => 0;
        public override bool Equals(object obj) => false;
        bool IEquatable<IntPtr>.Equals(IntPtr other) => false;
    }
    public struct UIntPtr : IEquatable<UIntPtr>
    {
        public override string ToString() => null;
        public override int GetHashCode() => 0;
        public override bool Equals(object obj) => false;
        bool IEquatable<UIntPtr>.Equals(UIntPtr other) => false;
    }
}";
            var sourceB =
@"interface I
{
    void F1(System.IntPtr x, nint y);
    void F2(System.UIntPtr x, nuint y);
}";
            var comp = CreateEmptyCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            verify(comp);

            comp = CreateEmptyCompilation(sourceA);
            comp.VerifyDiagnostics();
            var ref1 = comp.ToMetadataReference();
            var ref2 = comp.EmitToImageReference();

            comp = CreateEmptyCompilation(sourceB, references: new[] { ref1 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            verify(comp);

            comp = CreateEmptyCompilation(sourceB, references: new[] { ref2 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                var type = comp.GetTypeByMetadataName("System.IntPtr");
                VerifyType(type, signed: true, isNativeInt: false);
                VerifyType(type.GetPublicSymbol(), signed: true, isNativeInt: false);

                type = comp.GetTypeByMetadataName("System.UIntPtr");
                VerifyType(type, signed: false, isNativeInt: false);
                VerifyType(type.GetPublicSymbol(), signed: false, isNativeInt: false);

                var method = comp.GetMember<MethodSymbol>("I.F1");
                Assert.Equal("void I.F1(System.IntPtr x, nint y)", method.ToTestDisplayString());
                VerifyTypes((NamedTypeSymbol)method.Parameters[0].Type, (NamedTypeSymbol)method.Parameters[1].Type, signed: true);

                method = comp.GetMember<MethodSymbol>("I.F2");
                Assert.Equal("void I.F2(System.UIntPtr x, nuint y)", method.ToTestDisplayString());
                VerifyTypes((NamedTypeSymbol)method.Parameters[0].Type, (NamedTypeSymbol)method.Parameters[1].Type, signed: false);
            }
        }

        private static void VerifyType(NamedTypeSymbol type, bool signed, bool isNativeInt)
        {
            Assert.Equal(signed ? SpecialType.System_IntPtr : SpecialType.System_UIntPtr, type.SpecialType);
            Assert.Equal(SymbolKind.NamedType, type.Kind);
            Assert.Equal(TypeKind.Struct, type.TypeKind);
            Assert.Same(type, type.ConstructedFrom);
            Assert.Equal(isNativeInt, type.IsNativeIntegerType);
            Assert.Equal(signed ? "IntPtr" : "UIntPtr", type.Name);

            if (isNativeInt)
            {
                VerifyMembers(type);
            }
        }

        private static void VerifyType(INamedTypeSymbol type, bool signed, bool isNativeInt)
        {
            Assert.Equal(signed ? SpecialType.System_IntPtr : SpecialType.System_UIntPtr, type.SpecialType);
            Assert.Equal(SymbolKind.NamedType, type.Kind);
            Assert.Equal(TypeKind.Struct, type.TypeKind);
            Assert.Same(type, type.ConstructedFrom);
            Assert.Equal(isNativeInt, type.IsNativeIntegerType);
            Assert.Equal(signed ? "IntPtr" : "UIntPtr", type.Name);
        }

        private static void VerifyTypes(INamedTypeSymbol underlyingType, INamedTypeSymbol nativeIntegerType, bool signed)
        {
            VerifyType(underlyingType, signed, isNativeInt: false);
            VerifyType(nativeIntegerType, signed, isNativeInt: true);

            Assert.Same(underlyingType.ContainingSymbol, nativeIntegerType.ContainingSymbol);
            Assert.Same(underlyingType.Name, nativeIntegerType.Name);

            Assert.Empty(nativeIntegerType.GetTypeMembers());

            var memberNames = nativeIntegerType.MemberNames;
            var members = nativeIntegerType.GetMembers();
            Assert.Equal(members.SelectAsArray(m => m.Name), memberNames);

            var baseMembers = members.SelectAsArray(m => ((IMethodSymbol)m).OverriddenMethod);
            Assert.Empty(baseMembers);

            verifyInterfaces(underlyingType, underlyingType.Interfaces, nativeIntegerType, nativeIntegerType.Interfaces);

            Assert.NotSame(underlyingType, nativeIntegerType);
            Assert.Same(underlyingType, nativeIntegerType.NativeIntegerUnderlyingType);
            Assert.Equal(underlyingType, nativeIntegerType);
            Assert.Equal(nativeIntegerType, underlyingType);
            Assert.True(underlyingType.Equals(nativeIntegerType));
            Assert.True(((IEquatable<ISymbol>)underlyingType).Equals(nativeIntegerType));
            Assert.True(underlyingType.Equals(nativeIntegerType, SymbolEqualityComparer.Default));
            Assert.True(underlyingType.Equals(nativeIntegerType, SymbolEqualityComparer.IncludeNullability));
            Assert.True(underlyingType.Equals(nativeIntegerType, SymbolEqualityComparer.ConsiderEverything));
            Assert.Equal(underlyingType.GetHashCode(), nativeIntegerType.GetHashCode());

            static void verifyInterfaces(INamedTypeSymbol underlyingType, ImmutableArray<INamedTypeSymbol> underlyingInterfaces, INamedTypeSymbol nativeIntegerType, ImmutableArray<INamedTypeSymbol> nativeIntegerInterfaces)
            {
                underlyingInterfaces = underlyingInterfaces.WhereAsArray(t => t.Name == "IEquatable" && t.ContainingNamespace.Name == "System");

                Assert.Equal(underlyingInterfaces.Length, nativeIntegerInterfaces.Length);

                for (int i = 0; i < underlyingInterfaces.Length; i++)
                {
                    verifyInterface(underlyingInterfaces[i], nativeIntegerInterfaces[i]);
                }

                void verifyInterface(INamedTypeSymbol underlyingInterface, INamedTypeSymbol nativeIntegerInterface)
                {
                    Assert.Equal(underlyingInterface, nativeIntegerInterface);

                    for (int i = 0; i < underlyingInterface.TypeArguments.Length; i++)
                    {
                        var underlyingTypeArgument = underlyingInterface.TypeArguments[i];
                        var nativeIntegerTypeArgument = nativeIntegerInterface.TypeArguments[i];
                        if (underlyingTypeArgument.Equals(underlyingType, TypeCompareKind.AllIgnoreOptions))
                        {
                            Assert.True(nativeIntegerTypeArgument.IsNativeIntegerType);
                        }
                    }
                }
            }
        }

        private static void VerifyTypes(NamedTypeSymbol underlyingType, NamedTypeSymbol nativeIntegerType, bool signed)
        {
            VerifyType(underlyingType, signed, isNativeInt: false);
            VerifyType(nativeIntegerType, signed, isNativeInt: true);

            Assert.Same(underlyingType.ContainingSymbol, nativeIntegerType.ContainingSymbol);
            Assert.Same(underlyingType.Name, nativeIntegerType.Name);

            Assert.Empty(nativeIntegerType.GetTypeMembers());

            var memberNames = nativeIntegerType.MemberNames;
            var members = nativeIntegerType.GetMembers();
            Assert.Equal(members.SelectAsArray(m => m.Name), memberNames);

            var baseMembers = members.SelectAsArray(m => ((MethodSymbol)m).OverriddenMethod);
            Assert.Empty(baseMembers);

            verifyInterfaces(underlyingType, underlyingType.InterfacesNoUseSiteDiagnostics(), nativeIntegerType, nativeIntegerType.InterfacesNoUseSiteDiagnostics());
            verifyInterfaces(underlyingType, underlyingType.GetDeclaredInterfaces(null), nativeIntegerType, nativeIntegerType.GetDeclaredInterfaces(null));

            Assert.Null(underlyingType.NativeIntegerUnderlyingType);
            Assert.Same(nativeIntegerType, underlyingType.AsNativeInteger());
            Assert.Same(underlyingType, nativeIntegerType.NativeIntegerUnderlyingType);
            verifyEqualButDistinct(underlyingType, underlyingType.AsNativeInteger());
            verifyEqualButDistinct(nativeIntegerType, nativeIntegerType.NativeIntegerUnderlyingType);
            verifyEqualButDistinct(underlyingType, nativeIntegerType);

            VerifyTypes(underlyingType.GetPublicSymbol(), nativeIntegerType.GetPublicSymbol(), signed);

            static void verifyEqualButDistinct(NamedTypeSymbol underlyingType, NamedTypeSymbol nativeIntegerType)
            {
                Assert.NotSame(underlyingType, nativeIntegerType);
                Assert.Equal(underlyingType, nativeIntegerType);
                Assert.Equal(nativeIntegerType, underlyingType);
                Assert.True(underlyingType.Equals(nativeIntegerType, TypeCompareKind.ConsiderEverything));
                Assert.True(nativeIntegerType.Equals(underlyingType, TypeCompareKind.ConsiderEverything));
                Assert.Equal(underlyingType.GetHashCode(), nativeIntegerType.GetHashCode());
            }

            static void verifyInterfaces(NamedTypeSymbol underlyingType, ImmutableArray<NamedTypeSymbol> underlyingInterfaces, NamedTypeSymbol nativeIntegerType, ImmutableArray<NamedTypeSymbol> nativeIntegerInterfaces)
            {
                underlyingInterfaces = underlyingInterfaces.WhereAsArray(t => t.Name == "IEquatable" && t.ContainingNamespace.Name == "System");

                Assert.Equal(underlyingInterfaces.Length, nativeIntegerInterfaces.Length);

                for (int i = 0; i < underlyingInterfaces.Length; i++)
                {
                    verifyInterface(underlyingInterfaces[i], nativeIntegerInterfaces[i]);
                }

                void verifyInterface(NamedTypeSymbol underlyingInterface, NamedTypeSymbol nativeIntegerInterface)
                {
                    Assert.Equal(underlyingInterface, nativeIntegerInterface);

                    for (int i = 0; i < underlyingInterface.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.Length; i++)
                    {
                        var underlyingTypeArgument = underlyingInterface.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[i].Type;
                        var nativeIntegerTypeArgument = nativeIntegerInterface.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[i].Type;
                        if (underlyingTypeArgument.Equals(underlyingType, TypeCompareKind.AllIgnoreOptions))
                        {
                            Assert.True(nativeIntegerTypeArgument.IsNativeIntegerType);
                        }
                    }
                }
            }
        }

        private static void VerifyMembers(NamedTypeSymbol type)
        {
            var memberNames = type.MemberNames;
            var allMembers = type.GetMembers();
            Assert.Equal(allMembers, type.GetMembers(), ReferenceEqualityComparer.Instance);

            foreach (var member in allMembers)
            {
                Assert.Contains(member.Name, memberNames);
                verifyMember(type, member);
            }

            var unorderedMembers = type.GetMembersUnordered();
            Assert.Equal(allMembers.Length, unorderedMembers.Length);
            verifyMembers(type, allMembers, unorderedMembers);

            foreach (var memberName in memberNames)
            {
                var members = type.GetMembers(memberName);
                Assert.False(members.IsDefaultOrEmpty);
                verifyMembers(type, allMembers, members);
            }

            static void verifyMembers(NamedTypeSymbol type, ImmutableArray<Symbol> allMembers, ImmutableArray<Symbol> members)
            {
                foreach (var member in members)
                {
                    Assert.Contains(member, allMembers);
                    verifyMember(type, member);
                }
            }

            static void verifyMember(NamedTypeSymbol type, Symbol member)
            {
                Assert.Same(type, member.ContainingSymbol);
                Assert.Same(type, member.ContainingType);
            }
        }

        [Fact]
        public void MissingTypes()
        {
            var sourceA =
@"namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
}";
            var sourceB =
@"interface I
{
    void F1(System.IntPtr x, nint y);
    void F2(System.UIntPtr x, nuint y);
}";
            var diagnostics = new[]
            {
                // (3,20): error CS0234: The underlyingType or namespace name 'IntPtr' does not exist in the namespace 'System' (are you missing an assembly reference?)
                //     void F1(System.IntPtr x, nint y);
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "IntPtr").WithArguments("IntPtr", "System").WithLocation(3, 20),
                // (3,30): error CS0518: Predefined underlyingType 'System.IntPtr' is not defined or imported
                //     void F1(System.IntPtr x, nint y);
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "nint").WithArguments("System.IntPtr").WithLocation(3, 30),
                // (4,20): error CS0234: The underlyingType or namespace name 'UIntPtr' does not exist in the namespace 'System' (are you missing an assembly reference?)
                //     void F2(System.UIntPtr x, nuint y);
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "UIntPtr").WithArguments("UIntPtr", "System").WithLocation(4, 20),
                // (4,31): error CS0518: Predefined underlyingType 'System.UIntPtr' is not defined or imported
                //     void F2(System.UIntPtr x, nuint y);
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "nuint").WithArguments("System.UIntPtr").WithLocation(4, 31)
            };

            var comp = CreateEmptyCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(diagnostics);
            verify(comp);

            comp = CreateEmptyCompilation(sourceA);
            comp.VerifyDiagnostics();
            var ref1 = comp.ToMetadataReference();
            var ref2 = comp.EmitToImageReference();

            comp = CreateEmptyCompilation(sourceB, references: new[] { ref1 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(diagnostics);
            verify(comp);

            comp = CreateEmptyCompilation(sourceB, references: new[] { ref2 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(diagnostics);
            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                var method = comp.GetMember<MethodSymbol>("I.F1");
                Assert.Equal("void I.F1(System.IntPtr x, nint y)", method.ToTestDisplayString());
                VerifyErrorTypes((NamedTypeSymbol)method.Parameters[0].Type, (NamedTypeSymbol)method.Parameters[1].Type, signed: true);

                method = comp.GetMember<MethodSymbol>("I.F2");
                Assert.Equal("void I.F2(System.UIntPtr x, nuint y)", method.ToTestDisplayString());
                VerifyErrorTypes((NamedTypeSymbol)method.Parameters[0].Type, (NamedTypeSymbol)method.Parameters[1].Type, signed: false);
            }
        }

        private static void VerifyErrorType(NamedTypeSymbol type, SpecialType specialType, bool isNativeInt)
        {
            Assert.Equal(SymbolKind.ErrorType, type.Kind);
            Assert.Equal(TypeKind.Error, type.TypeKind);
            Assert.Equal(isNativeInt, type.IsNativeIntegerType);
            Assert.Equal(specialType, type.SpecialType);
        }

        private static void VerifyErrorType(INamedTypeSymbol type, SpecialType specialType, bool isNativeInt)
        {
            Assert.Equal(SymbolKind.ErrorType, type.Kind);
            Assert.Equal(TypeKind.Error, type.TypeKind);
            Assert.Equal(isNativeInt, type.IsNativeIntegerType);
            Assert.Equal(specialType, type.SpecialType);
        }

        private static void VerifyErrorTypes(NamedTypeSymbol underlyingType, NamedTypeSymbol nativeIntegerType, bool signed)
        {
            var specialType = signed ? SpecialType.System_IntPtr : SpecialType.System_UIntPtr;

            VerifyErrorType(underlyingType, SpecialType.None, isNativeInt: false);
            VerifyErrorType(nativeIntegerType, specialType, isNativeInt: true);

            Assert.Null(underlyingType.NativeIntegerUnderlyingType);
            VerifyErrorType(nativeIntegerType.NativeIntegerUnderlyingType, specialType, isNativeInt: false);

            VerifyErrorTypes(underlyingType.GetPublicSymbol(), nativeIntegerType.GetPublicSymbol(), signed);
        }

        private static void VerifyErrorTypes(INamedTypeSymbol underlyingType, INamedTypeSymbol nativeIntegerType, bool signed)
        {
            var specialType = signed ? SpecialType.System_IntPtr : SpecialType.System_UIntPtr;

            VerifyErrorType(underlyingType, SpecialType.None, isNativeInt: false);
            VerifyErrorType(nativeIntegerType, specialType, isNativeInt: true);

            Assert.Null(underlyingType.NativeIntegerUnderlyingType);
            VerifyErrorType(nativeIntegerType.NativeIntegerUnderlyingType, specialType, isNativeInt: false);
        }

        [Fact]
        public void Retargeting_01()
        {
            var sourceA =
@"public class A
{
    public static nint F1 = int.MinValue;
    public static nuint F2 = int.MaxValue;
    public static nint F3 = -1;
    public static nuint F4 = 1;
}";
            var comp = CreateCompilation(sourceA, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.Mscorlib40);
            var refA = comp.ToMetadataReference();

            var typeA = comp.GetMember<FieldSymbol>("A.F1").Type;
            var corLibA = comp.Assembly.CorLibrary;
            Assert.Equal(corLibA, typeA.ContainingAssembly);

            var sourceB =
@"class B : A
{
    static void Main()
    {
        System.Console.WriteLine(""{0}, {1}, {2}, {3}"", F1, F2, F3, F4);
    }
}";
            comp = CreateCompilation(sourceB, references: new[] { refA }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.Mscorlib45);
            CompileAndVerify(comp, expectedOutput: $"{int.MinValue}, {int.MaxValue}, -1, 1");

            var corLibB = comp.Assembly.CorLibrary;
            Assert.NotEqual(corLibA, corLibB);

            var f1 = comp.GetMember<FieldSymbol>("A.F1");
            verifyField(f1, "nint A.F1", corLibB);
            var f2 = comp.GetMember<FieldSymbol>("A.F2");
            verifyField(f2, "nuint A.F2", corLibB);
            var f3 = comp.GetMember<FieldSymbol>("A.F3");
            verifyField(f3, "nint A.F3", corLibB);
            var f4 = comp.GetMember<FieldSymbol>("A.F4");
            verifyField(f4, "nuint A.F4", corLibB);

            Assert.Same(f1.Type, f3.Type);
            Assert.Same(f2.Type, f4.Type);

            static void verifyField(FieldSymbol field, string expectedSymbol, AssemblySymbol expectedAssembly)
            {
                Assert.IsType<Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting.RetargetingFieldSymbol>(field);
                Assert.Equal(expectedSymbol, field.ToTestDisplayString());
                var type = (NamedTypeSymbol)field.Type;
                Assert.True(type.IsNativeIntegerType);
                Assert.IsType<NativeIntegerTypeSymbol>(type);
                Assert.Equal(expectedAssembly, type.NativeIntegerUnderlyingType.ContainingAssembly);
            }
        }

        [Fact]
        public void Retargeting_02()
        {
            var source1 =
@"namespace System
{
    public class Object { }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public struct IntPtr { }
    public struct UIntPtr { }
}";
            var assemblyName = GetUniqueName();
            var comp = CreateCompilation(new AssemblyIdentity(assemblyName, new Version(1, 0, 0, 0)), new[] { source1 }, references: null);
            var ref1 = comp.EmitToImageReference();

            var sourceA =
@"public class A
{
    public static nint F1 = -1;
    public static nuint F2 = 1;
    public static nint F3 = -2;
    public static nuint F4 = 2;
}";
            comp = CreateEmptyCompilation(sourceA, references: new[] { ref1 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            var refA = comp.ToMetadataReference();

            var typeA = comp.GetMember<FieldSymbol>("A.F1").Type;
            var corLibA = comp.Assembly.CorLibrary;
            Assert.Equal(corLibA, typeA.ContainingAssembly);

            var source2 =
@"namespace System
{
    public class Object { }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
}";
            comp = CreateCompilation(new AssemblyIdentity(assemblyName, new Version(2, 0, 0, 0)), new[] { source2 }, references: null);
            var ref2 = comp.EmitToImageReference();

            var sourceB =
@"class B : A
{
    static void Main()
    {
        _ = F1;
        _ = F2;
    }
}";
            comp = CreateEmptyCompilation(sourceB, references: new[] { ref2, refA }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,13): error CS0518: Predefined type 'System.IntPtr' is not defined or imported
                //         _ = F1;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "F1").WithArguments("System.IntPtr").WithLocation(5, 13),
                // (6,13): error CS0518: Predefined type 'System.UIntPtr' is not defined or imported
                //         _ = F2;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "F2").WithArguments("System.UIntPtr").WithLocation(6, 13));

            var corLibB = comp.Assembly.CorLibrary;
            Assert.NotEqual(corLibA, corLibB);

            var f1 = comp.GetMember<FieldSymbol>("A.F1");
            verifyField(f1, "nint A.F1", corLibB);
            var f2 = comp.GetMember<FieldSymbol>("A.F2");
            verifyField(f2, "nuint A.F2", corLibB);
            var f3 = comp.GetMember<FieldSymbol>("A.F3");
            verifyField(f3, "nint A.F3", corLibB);
            var f4 = comp.GetMember<FieldSymbol>("A.F4");
            verifyField(f4, "nuint A.F4", corLibB);

            // MissingMetadataTypeSymbol.TopLevel instances are not reused.
            Assert.Equal(f1.Type, f3.Type);
            Assert.NotSame(f1.Type, f3.Type);
            Assert.Equal(f2.Type, f4.Type);
            Assert.NotSame(f2.Type, f4.Type);

            static void verifyField(FieldSymbol field, string expectedSymbol, AssemblySymbol expectedAssembly)
            {
                Assert.IsType<Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting.RetargetingFieldSymbol>(field);
                Assert.Equal(expectedSymbol, field.ToTestDisplayString());
                var type = (NamedTypeSymbol)field.Type;
                Assert.True(type.IsNativeIntegerType);
                Assert.IsType<MissingMetadataTypeSymbol.TopLevel>(type);
                Assert.Equal(expectedAssembly, type.NativeIntegerUnderlyingType.ContainingAssembly);
            }
        }

        [Fact]
        public void Retargeting_03()
        {
            var source1 =
@"namespace System
{
    public class Object { }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
}";
            var assemblyName = GetUniqueName();
            var comp = CreateCompilation(new AssemblyIdentity(assemblyName, new Version(1, 0, 0, 0)), new[] { source1 }, references: null);
            var ref1 = comp.EmitToImageReference();

            var sourceA =
@"public class A
{
    public static nint F1 = -1;
    public static nuint F2 = 1;
}";
            comp = CreateEmptyCompilation(sourceA, references: new[] { ref1 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (3,19): error CS0518: Predefined type 'System.IntPtr' is not defined or imported
                //     public static nint F1 = -1;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "nint").WithArguments("System.IntPtr").WithLocation(3, 19),
                // (4,19): error CS0518: Predefined type 'System.UIntPtr' is not defined or imported
                //     public static nuint F2 = 1;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "nuint").WithArguments("System.UIntPtr").WithLocation(4, 19));
            var refA = comp.ToMetadataReference();

            var typeA = comp.GetMember<FieldSymbol>("A.F1").Type;
            var corLibA = comp.Assembly.CorLibrary;
            Assert.Equal(corLibA, typeA.ContainingAssembly);

            var source2 =
@"namespace System
{
    public class Object { }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public struct IntPtr { }
    public struct UIntPtr { }
}";
            comp = CreateCompilation(new AssemblyIdentity(assemblyName, new Version(2, 0, 0, 0)), new[] { source2 }, references: null);
            var ref2 = comp.EmitToImageReference();

            var sourceB =
@"class B : A
{
    static void Main()
    {
        _ = F1;
        _ = F2;
    }
}";
            comp = CreateEmptyCompilation(sourceB, references: new[] { ref2, refA }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,13): error CS0518: Predefined type 'System.IntPtr' is not defined or imported
                //         _ = F1;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "F1").WithArguments("System.IntPtr").WithLocation(5, 13),
                // (6,13): error CS0518: Predefined type 'System.UIntPtr' is not defined or imported
                //         _ = F2;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "F2").WithArguments("System.UIntPtr").WithLocation(6, 13));

            var corLibB = comp.Assembly.CorLibrary;
            Assert.NotEqual(corLibA, corLibB);

            var f1 = comp.GetMember<FieldSymbol>("A.F1");
            verifyField(f1, "nint A.F1", corLibA);
            var f2 = comp.GetMember<FieldSymbol>("A.F2");
            verifyField(f2, "nuint A.F2", corLibA);

            static void verifyField(FieldSymbol field, string expectedSymbol, AssemblySymbol expectedAssembly)
            {
                Assert.IsType<Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting.RetargetingFieldSymbol>(field);
                Assert.Equal(expectedSymbol, field.ToTestDisplayString());
                var type = (NamedTypeSymbol)field.Type;
                Assert.True(type.IsNativeIntegerType);
                Assert.IsType<MissingMetadataTypeSymbol.TopLevel>(type);
                Assert.Equal(expectedAssembly, type.NativeIntegerUnderlyingType.ContainingAssembly);
            }
        }

        [Fact]
        public void Retargeting_04()
        {
            var sourceA =
@"public class A
{
}";
            var assemblyName = GetUniqueName();
            var references = TargetFrameworkUtil.GetReferences(TargetFramework.Standard).ToArray();
            var comp = CreateCompilation(new AssemblyIdentity(assemblyName, new Version(1, 0, 0, 0)), new[] { sourceA }, references: references);
            var refA1 = comp.EmitToImageReference();

            comp = CreateCompilation(new AssemblyIdentity(assemblyName, new Version(2, 0, 0, 0)), new[] { sourceA }, references: references);
            var refA2 = comp.EmitToImageReference();

            var sourceB =
@"public class B
{
    public static A F0 = new A();
    public static nint F1 = int.MinValue;
    public static nuint F2 = int.MaxValue;
    public static nint F3 = -1;
    public static nuint F4 = 1;
}";
            comp = CreateCompilation(sourceB, references: new[] { refA1 }, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.Standard);
            var refB = comp.ToMetadataReference();
            var f0B = comp.GetMember<FieldSymbol>("B.F0");
            var t1B = comp.GetMember<FieldSymbol>("B.F1").Type;
            var t2B = comp.GetMember<FieldSymbol>("B.F2").Type;

            var sourceC =
@"class C : B
{
    static void Main()
    {
        _ = F0;
        _ = F1;
        _ = F2;
    }
}";
            comp = CreateCompilation(sourceC, references: new[] { refA2, refB }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.Standard);
            comp.VerifyDiagnostics();

            var f0 = comp.GetMember<FieldSymbol>("B.F0");
            Assert.NotEqual(f0B.Type.ContainingAssembly, f0.Type.ContainingAssembly);
            Assert.IsType<Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting.RetargetingFieldSymbol>(f0);

            var f1 = comp.GetMember<FieldSymbol>("B.F1");
            verifyField(f1, "nint B.F1");
            var f2 = comp.GetMember<FieldSymbol>("B.F2");
            verifyField(f2, "nuint B.F2");
            var f3 = comp.GetMember<FieldSymbol>("B.F3");
            verifyField(f3, "nint B.F3");
            var f4 = comp.GetMember<FieldSymbol>("B.F4");
            verifyField(f4, "nuint B.F4");

            Assert.Same(t1B, f1.Type);
            Assert.Same(t2B, f2.Type);
            Assert.Same(f1.Type, f3.Type);
            Assert.Same(f2.Type, f4.Type);

            static void verifyField(FieldSymbol field, string expectedSymbol)
            {
                Assert.IsType<Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting.RetargetingFieldSymbol>(field);
                Assert.Equal(expectedSymbol, field.ToTestDisplayString());
                var type = (NamedTypeSymbol)field.Type;
                Assert.True(type.IsNativeIntegerType);
                Assert.IsType<NativeIntegerTypeSymbol>(type);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Retargeting_05(bool useCompilationReference)
        {
            var source1 =
@"namespace System
{
    public class Object { }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public struct Enum { }
    public class Attribute { }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets validOn) { }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
    }
    public enum AttributeTargets { }
    public struct IntPtr { }
    public struct UIntPtr { }
}";
            var comp = CreateCompilation(new AssemblyIdentity("9ef8b1e0-1ae0-4af6-b9a1-00f2078f299e", new Version(1, 0, 0, 0)), new[] { source1 }, references: null);
            var ref1 = comp.EmitToImageReference(EmitOptions.Default.WithRuntimeMetadataVersion("0.0.0.0"));

            var sourceA =
@"public abstract class A<T>
{
    public abstract void F<U>() where U : T;
}
public class B : A<nint>
{
    public override void F<U>() { }
}";
            comp = CreateEmptyCompilation(sourceA, references: new[] { ref1 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            var refA = AsReference(comp, useCompilationReference);

            var type1 = getConstraintType(comp);
            Assert.True(type1.IsNativeIntegerType);
            Assert.False(type1.IsErrorType());

            var sourceB =
@"class C : B
{
}";
            comp = CreateEmptyCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (1,7): error CS0518: Predefined type 'System.Void' is not defined or imported
                // class C : B
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "C").WithArguments("System.Void").WithLocation(1, 7),
                // (1,11): error CS0518: Predefined type 'System.IntPtr' is not defined or imported
                // class C : B
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "B").WithArguments("System.IntPtr").WithLocation(1, 11),
                // (1,11): error CS0012: The type 'Object' is defined in an assembly that is not referenced. You must add a reference to assembly '9ef8b1e0-1ae0-4af6-b9a1-00f2078f299e, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'.
                // class C : B
                Diagnostic(ErrorCode.ERR_NoTypeDef, "B").WithArguments("System.Object", "9ef8b1e0-1ae0-4af6-b9a1-00f2078f299e, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 11));

            var type2 = getConstraintType(comp);
            Assert.True(type2.ContainingAssembly.IsMissing);
            Assert.False(type2.IsNativeIntegerType);
            Assert.True(type2.IsErrorType());

            static TypeSymbol getConstraintType(CSharpCompilation comp) =>
                comp.GetMember<MethodSymbol>("B.F").TypeParameters[0].ConstraintTypesNoUseSiteDiagnostics[0].Type;
        }

        [Fact]
        public void Retargeting_06()
        {
            var source1 =
@"namespace System
{
    public class Object { }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public class IntPtr { }
    public class UIntPtr { }
}";
            var comp = CreateCompilation(new AssemblyIdentity("c804cc09-8f73-44a1-9cfe-9567bed1def6", new Version(1, 0, 0, 0)), new[] { source1 }, references: null);
            var ref1 = comp.EmitToImageReference();

            var sourceA =
@"public class A : nint
{
}";
            comp = CreateEmptyCompilation(sourceA, references: new[] { ref1 }, parseOptions: TestOptions.RegularPreview);

            var refA = comp.ToMetadataReference();
            var typeA = comp.GetMember<NamedTypeSymbol>("A").BaseTypeNoUseSiteDiagnostics;
            Assert.True(typeA.IsNativeIntegerType);
            Assert.False(typeA.IsErrorType());

            var sourceB =
@"class B : A
{
}";
            comp = CreateEmptyCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (1,7): error CS0518: Predefined type 'System.Void' is not defined or imported
                // class B : A
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "B").WithArguments("System.Void").WithLocation(1, 7),
                // (1,11): error CS0012: The type 'IntPtr' is defined in an assembly that is not referenced. You must add a reference to assembly 'c804cc09-8f73-44a1-9cfe-9567bed1def6, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'.
                // class B : A
                Diagnostic(ErrorCode.ERR_NoTypeDef, "A").WithArguments("System.IntPtr", "c804cc09-8f73-44a1-9cfe-9567bed1def6, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 11));

            var typeB = comp.GetMember<NamedTypeSymbol>("A").BaseTypeNoUseSiteDiagnostics;
            Assert.True(typeB.ContainingAssembly.IsMissing);
            Assert.False(typeB.IsNativeIntegerType);
            Assert.True(typeB.IsErrorType());
        }

        [Fact]
        public void Interfaces()
        {
            var sourceA =
@"namespace System
{
    public class Object { }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public interface ISerializable { }
    public interface IEquatable<T>
    {
        bool Equals(T other);
    }
    public interface IOther<T> { }
    public struct IntPtr : ISerializable, IEquatable<IntPtr>, IOther<IntPtr>
    {
        bool IEquatable<IntPtr>.Equals(IntPtr other) => false;
    }
}";
            var sourceB =
@"using System;
class Program
{
    static void F0(ISerializable i) { }
    static object F1(IEquatable<IntPtr> i) => default;
    static void F2(IEquatable<nint> i) { }
    static void F3<T>(IOther<T> i) { }
    static void Main()
    {
        nint n = 42;
        F0(n);
        F1(n);
        _ = F2(n);
        F3<nint>(n);
        F3<IntPtr>(n);
        F3((IntPtr)n);
    }
}";
            var comp = CreateEmptyCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (11,12): error CS1503: Argument 1: cannot convert from 'nint' to 'System.ISerializable'
                //         F0(n);
                Diagnostic(ErrorCode.ERR_BadArgType, "n").WithArguments("1", "nint", "System.ISerializable").WithLocation(11, 12),
                // (13,9): error CS8209: A value of type 'void' may not be assigned.
                //         _ = F2(n);
                Diagnostic(ErrorCode.ERR_VoidAssignment, "_").WithLocation(13, 9),
                // (14,18): error CS1503: Argument 1: cannot convert from 'nint' to 'System.IOther<nint>'
                //         F3<nint>(n);
                Diagnostic(ErrorCode.ERR_BadArgType, "n").WithArguments("1", "nint", "System.IOther<nint>").WithLocation(14, 18),
                // (15,20): error CS1503: Argument 1: cannot convert from 'nint' to 'System.IOther<System.IntPtr>'
                //         F3<IntPtr>(n);
                Diagnostic(ErrorCode.ERR_BadArgType, "n").WithArguments("1", "nint", "System.IOther<System.IntPtr>").WithLocation(15, 20));
        }

        [Fact]
        public void IEquatable()
        {
            // Minimal definitions.
            verifyAll(includesIEquatable: true,
@"namespace System
{
    public class Object { }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public interface IEquatable<T> { }
    public struct IntPtr : IEquatable<IntPtr> { }
    public struct UIntPtr : IEquatable<UIntPtr> { }
}");

            // IEquatable<T> in global namespace.
            verifyAll(includesIEquatable: false,
@"public interface IEquatable<T> { }
namespace System
{
    public class Object { }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public struct IntPtr : IEquatable<IntPtr> { }
    public struct UIntPtr : IEquatable<UIntPtr> { }
}");

            // IEquatable<T> in other namespace.
            verifyAll(includesIEquatable: false,
@"namespace Other
{
    public interface IEquatable<T> { }
}
namespace System
{
    public class Object { }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public struct IntPtr : Other.IEquatable<IntPtr> { }
    public struct UIntPtr : Other.IEquatable<UIntPtr> { }
}");

            // IEquatable<T> nested in "System" type.
            verifyAll(includesIEquatable: false,
@"namespace System
{
    public class Object { }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public class System
    {
        public interface IEquatable<T> { }
    }
    public struct IntPtr : System.IEquatable<IntPtr> { }
    public struct UIntPtr : System.IEquatable<UIntPtr> { }
}");

            // IEquatable<T> nested in other type.
            verifyAll(includesIEquatable: false,
@"namespace System
{
    public class Object { }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public class Other
    {
        public interface IEquatable<T> { }
    }
    public struct IntPtr : Other.IEquatable<IntPtr> { }
    public struct UIntPtr : Other.IEquatable<UIntPtr> { }
}");

            // IEquatable.
            verifyAll(includesIEquatable: false,
@"namespace System
{
    public class Object { }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public interface IEquatable { }
    public struct IntPtr : IEquatable { }
    public struct UIntPtr : IEquatable { }
}");

            // IEquatable<T, U>.
            verifyAll(includesIEquatable: false,
@"namespace System
{
    public class Object { }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public interface IEquatable<T, U> { }
    public struct IntPtr : IEquatable<IntPtr, IntPtr> { }
    public struct UIntPtr : IEquatable<UIntPtr, UIntPtr> { }
}");

            // IEquatable<object> and IEquatable<ValueType>
            verifyAll(includesIEquatable: false,
@"namespace System
{
    public class Object { }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public interface IEquatable<T> { }
    public struct IntPtr : IEquatable<object> { }
    public struct UIntPtr : IEquatable<ValueType> { }
}");

            // IEquatable<System.UIntPtr> and  IEquatable<System.IntPtr>.
            verifyAll(includesIEquatable: false,
@"namespace System
{
    public class Object { }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public interface IEquatable<T> { }
    public struct IntPtr : IEquatable<UIntPtr> { }
    public struct UIntPtr : IEquatable<IntPtr> { }
}");

            // IEquatable<nint> and  IEquatable<nuint>.
            var comp = CreateEmptyCompilation(
@"namespace System
{
    public class Object { }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public struct Enum { }
    public class Attribute { }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets validOn) { }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
    }
    public enum AttributeTargets { }
    public interface IEquatable<T> { }
    public struct IntPtr : IEquatable<nint> { }
    public struct UIntPtr : IEquatable<nuint> { }
}",
                parseOptions: TestOptions.RegularPreview);
            verifyReference(comp.EmitToImageReference(EmitOptions.Default.WithRuntimeMetadataVersion("0.0.0.0")), includesIEquatable: true);

            // IEquatable<nuint> and  IEquatable<nint>.
            comp = CreateEmptyCompilation(
@"namespace System
{
    public class Object { }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public struct Enum { }
    public class Attribute { }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets validOn) { }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
    }
    public enum AttributeTargets { }
    public interface IEquatable<T> { }
    public struct IntPtr : IEquatable<nuint> { }
    public struct UIntPtr : IEquatable<nint> { }
}",
                parseOptions: TestOptions.RegularPreview);
            verifyReference(comp.EmitToImageReference(EmitOptions.Default.WithRuntimeMetadataVersion("0.0.0.0")), includesIEquatable: false);

            static void verifyAll(bool includesIEquatable, string sourceA)
            {
                var sourceB =
@"interface I
{
    nint F1();
    nuint F2();
}";
                var comp = CreateEmptyCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.RegularPreview);
                comp.VerifyDiagnostics();
                verifyCompilation(comp, includesIEquatable);

                comp = CreateEmptyCompilation(sourceA);
                comp.VerifyDiagnostics();
                var ref1 = comp.ToMetadataReference();
                var ref2 = comp.EmitToImageReference();

                comp = CreateEmptyCompilation(sourceB, references: new[] { ref1 }, parseOptions: TestOptions.RegularPreview);
                comp.VerifyDiagnostics();
                verifyCompilation(comp, includesIEquatable);

                comp = CreateEmptyCompilation(sourceB, references: new[] { ref2 }, parseOptions: TestOptions.RegularPreview);
                comp.VerifyDiagnostics();
                verifyCompilation(comp, includesIEquatable);
            }

            static void verifyReference(MetadataReference reference, bool includesIEquatable)
            {
                var sourceB =
@"interface I
{
    nint F1();
    nuint F2();
}";
                var comp = CreateEmptyCompilation(sourceB, references: new[] { reference }, parseOptions: TestOptions.RegularPreview);
                comp.VerifyDiagnostics();
                verifyCompilation(comp, includesIEquatable);
            }

            static void verifyCompilation(CSharpCompilation comp, bool includesIEquatable)
            {
                verifyInterfaces(comp, (NamedTypeSymbol)comp.GetMember<MethodSymbol>("I.F1").ReturnType, SpecialType.System_IntPtr, includesIEquatable);
                verifyInterfaces(comp, (NamedTypeSymbol)comp.GetMember<MethodSymbol>("I.F2").ReturnType, SpecialType.System_UIntPtr, includesIEquatable);
            }

            static void verifyInterfaces(CSharpCompilation comp, NamedTypeSymbol type, SpecialType specialType, bool includesIEquatable)
            {
                Assert.True(type.IsNativeIntegerType);
                Assert.Equal(specialType, type.NativeIntegerUnderlyingType.SpecialType);

                var interfaces = type.InterfacesNoUseSiteDiagnostics(null);
                Assert.Equal(interfaces, type.GetDeclaredInterfaces(null));

                Assert.Equal(includesIEquatable ? 1 : 0, interfaces.Length);

                if (includesIEquatable)
                {
                    var @interface = interfaces.Single();
                    var def = comp.GetWellKnownType(WellKnownType.System_IEquatable_T);
                    Assert.NotNull(def);
                    Assert.Equal(def, @interface.OriginalDefinition);
                    Assert.Equal(type, @interface.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type);
                }
            }
        }

        [Fact]
        public void CreateNativeIntegerTypeSymbol_FromMetadata()
        {
            var comp = CreateCompilation("");
            comp.VerifyDiagnostics();
            VerifyCreateNativeIntegerTypeSymbol(comp);
        }

        [Fact]
        public void CreateNativeIntegerTypeSymbol_FromSource()
        {
            var source0 =
@"namespace System
{
    public class Object { }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public struct IntPtr { }
    public struct UIntPtr { }
}";
            var comp = CreateEmptyCompilation(source0, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            VerifyCreateNativeIntegerTypeSymbol(comp);

            var ref1 = comp.ToMetadataReference();
            var ref2 = comp.EmitToImageReference();

            comp = CreateEmptyCompilation("", references: new[] { ref1 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            VerifyCreateNativeIntegerTypeSymbol(comp);

            comp = CreateEmptyCompilation("", references: new[] { ref2 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            VerifyCreateNativeIntegerTypeSymbol(comp);
        }

        private static void VerifyCreateNativeIntegerTypeSymbol(CSharpCompilation comp)
        {
            verifyInternalType(comp, signed: true);
            verifyInternalType(comp, signed: false);
            verifyPublicType(comp, signed: true);
            verifyPublicType(comp, signed: false);

            static void verifyInternalType(CSharpCompilation comp, bool signed)
            {
                var type = comp.CreateNativeIntegerTypeSymbol(signed);
                VerifyType(type, signed, isNativeInt: true);
            }

            static void verifyPublicType(Compilation comp, bool signed)
            {
                var type = comp.CreateNativeIntegerTypeSymbol(signed);
                VerifyType(type, signed, isNativeInt: true);

                var underlyingType = type.NativeIntegerUnderlyingType;
                Assert.NotNull(underlyingType);
                Assert.Equal(CodeAnalysis.NullableAnnotation.NotAnnotated, underlyingType.NullableAnnotation);
                Assert.Same(underlyingType, ((INamedTypeSymbol)type.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.None)).NativeIntegerUnderlyingType);
                Assert.Same(underlyingType, ((INamedTypeSymbol)type.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.Annotated)).NativeIntegerUnderlyingType);
                Assert.Same(underlyingType, ((INamedTypeSymbol)type.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.NotAnnotated)).NativeIntegerUnderlyingType);
            }
        }

        [Fact]
        public void CreateNativeIntegerTypeSymbol_Missing()
        {
            var source0 =
@"namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
}";
            var comp = CreateEmptyCompilation(source0, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            verifyCreateNativeIntegerTypeSymbol(comp);

            var ref1 = comp.ToMetadataReference();
            var ref2 = comp.EmitToImageReference();

            comp = CreateEmptyCompilation("", references: new[] { ref1 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            verifyCreateNativeIntegerTypeSymbol(comp);

            comp = CreateEmptyCompilation("", references: new[] { ref2 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            verifyCreateNativeIntegerTypeSymbol(comp);

            static void verifyCreateNativeIntegerTypeSymbol(CSharpCompilation comp)
            {
                VerifyErrorType(comp.CreateNativeIntegerTypeSymbol(signed: true), SpecialType.System_IntPtr, isNativeInt: true);
                VerifyErrorType(comp.CreateNativeIntegerTypeSymbol(signed: false), SpecialType.System_UIntPtr, isNativeInt: true);
                VerifyErrorType(((Compilation)comp).CreateNativeIntegerTypeSymbol(signed: true), SpecialType.System_IntPtr, isNativeInt: true);
                VerifyErrorType(((Compilation)comp).CreateNativeIntegerTypeSymbol(signed: false), SpecialType.System_UIntPtr, isNativeInt: true);
            }
        }

        [Fact]
        public void ArrayInitialization()
        {
            var source =
@"class Program
{
    static void Main()
    {
        Report(new nint[] { int.MinValue, -1, 0, 1, int.MaxValue });
        Report(new nuint[] { 0, 1, 2, int.MaxValue, uint.MaxValue });
    }
    static void Report<T>(T[] items)
    {
        foreach (var item in items)
            System.Console.WriteLine($""{item.GetType().FullName}: {item}"");
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput:
@"System.IntPtr: -2147483648
System.IntPtr: -1
System.IntPtr: 0
System.IntPtr: 1
System.IntPtr: 2147483647
System.UIntPtr: 0
System.UIntPtr: 1
System.UIntPtr: 2
System.UIntPtr: 2147483647
System.UIntPtr: 4294967295");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size       75 (0x4b)
  .maxstack  4
  IL_0000:  ldc.i4.5
  IL_0001:  newarr     ""System.IntPtr""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4     0x80000000
  IL_000d:  conv.i
  IL_000e:  stelem.i
  IL_000f:  dup
  IL_0010:  ldc.i4.1
  IL_0011:  ldc.i4.m1
  IL_0012:  conv.i
  IL_0013:  stelem.i
  IL_0014:  dup
  IL_0015:  ldc.i4.3
  IL_0016:  ldc.i4.1
  IL_0017:  conv.i
  IL_0018:  stelem.i
  IL_0019:  dup
  IL_001a:  ldc.i4.4
  IL_001b:  ldc.i4     0x7fffffff
  IL_0020:  conv.i
  IL_0021:  stelem.i
  IL_0022:  call       ""void Program.Report<nint>(nint[])""
  IL_0027:  ldc.i4.5
  IL_0028:  newarr     ""System.UIntPtr""
  IL_002d:  dup
  IL_002e:  ldc.i4.1
  IL_002f:  ldc.i4.1
  IL_0030:  conv.i
  IL_0031:  stelem.i
  IL_0032:  dup
  IL_0033:  ldc.i4.2
  IL_0034:  ldc.i4.2
  IL_0035:  conv.i
  IL_0036:  stelem.i
  IL_0037:  dup
  IL_0038:  ldc.i4.3
  IL_0039:  ldc.i4     0x7fffffff
  IL_003e:  conv.i
  IL_003f:  stelem.i
  IL_0040:  dup
  IL_0041:  ldc.i4.4
  IL_0042:  ldc.i4.m1
  IL_0043:  conv.u
  IL_0044:  stelem.i
  IL_0045:  call       ""void Program.Report<nuint>(nuint[])""
  IL_004a:  ret
}");
        }

        [Fact]
        public void Overrides_01()
        {
            var sourceA =
@"public interface IA
{
    void F1(nint x, System.UIntPtr y);
}
public abstract class A
{
    public abstract void F2(System.IntPtr x, nuint y);
}";
            var sourceB =
@"class B1 : A, IA
{
    public void F1(nint x, System.UIntPtr y) { }
    public override void F2(System.IntPtr x, nuint y) { }
}
class B2 : A, IA
{
    public void F1(System.IntPtr x, nuint y) { }
    public override void F2(nint x, System.UIntPtr y) { }
}
class A3 : IA
{
    void IA.F1(nint x, System.UIntPtr y) { }
}
class A4 : IA
{
    void IA.F1(System.IntPtr x, nuint y) { }
}";

            var comp = CreateCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(sourceA, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            var ref1 = comp.ToMetadataReference();
            var ref2 = comp.EmitToImageReference();

            comp = CreateCompilation(sourceB, references: new[] { ref1 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(sourceB, references: new[] { ref2 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Overrides_02()
        {
            var sourceA =
@"public interface IA
{
    void F1(System.IntPtr x, System.UIntPtr y);
}
public abstract class A
{
    public abstract void F2(System.IntPtr x, System.UIntPtr y);
}";
            var sourceB =
@"class B1 : A, IA
{
    public void F1(nint x, System.UIntPtr y) { }
    public override void F2(nint x, System.UIntPtr y) { }
}
class B2 : A, IA
{
    public void F1(System.IntPtr x, nuint y) { }
    public override void F2(System.IntPtr x, nuint y) { }
}
class A3 : IA
{
    void IA.F1(nint x, System.UIntPtr y) { }
}
class A4 : IA
{
    void IA.F1(System.IntPtr x, nuint y) { }
}";

            var comp = CreateCompilation(sourceA, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
            var ref1 = comp.ToMetadataReference();
            var ref2 = comp.EmitToImageReference();

            comp = CreateCompilation(sourceB, references: new[] { ref1 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(sourceB, references: new[] { ref2 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Overrides_03()
        {
            var sourceA =
@"public interface IA
{
    void F1(nint x, System.UIntPtr y);
}
public abstract class A
{
    public abstract void F2(System.IntPtr x, nuint y);
}";
            var sourceB =
@"class B1 : A, IA
{
    public void F1(System.IntPtr x, System.UIntPtr y) { }
    public override void F2(System.IntPtr x, System.UIntPtr y) { }
}
class A2 : IA
{
    void IA.F1(System.IntPtr x, System.UIntPtr y) { }
}";

            var comp = CreateCompilation(sourceA, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            var ref1 = comp.ToMetadataReference();
            var ref2 = comp.EmitToImageReference();

            comp = CreateCompilation(sourceB, references: new[] { ref1 }, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(sourceB, references: new[] { ref2 }, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Overloads_01()
        {
            var sourceA =
@"public class A
{
    public void F1(System.IntPtr x) { }
    public void F2(nuint y) { }
}";
            var sourceB =
@"class B1 : A
{
    public void F1(nuint x) { }
    public void F2(System.IntPtr y) { }
}
class B2 : A
{
    public void F1(nint x) { base.F1(x); }
    public void F2(System.UIntPtr y) { base.F2(y); }
}
class B3 : A
{
    public new void F1(nuint x) { }
    public new void F2(System.IntPtr y) { }
}
class B4 : A
{
    public new void F1(nint x) {  base.F1(x); }
    public new void F2(System.UIntPtr y) { base.F2(y); }
}";

            var diagnostics = new[]
            {
                // (8,17): warning CS0108: 'B2.F1(nint)' hides inherited member 'A.F1(IntPtr)'. Use the new keyword if hiding was intended.
                //     public void F1(nint x) { base.F1(x); }
                Diagnostic(ErrorCode.WRN_NewRequired, "F1").WithArguments("B2.F1(nint)", "A.F1(System.IntPtr)").WithLocation(8, 17),
                // (9,17): warning CS0108: 'B2.F2(UIntPtr)' hides inherited member 'A.F2(nuint)'. Use the new keyword if hiding was intended.
                //     public void F2(System.UIntPtr y) { base.F2(y); }
                Diagnostic(ErrorCode.WRN_NewRequired, "F2").WithArguments("B2.F2(System.UIntPtr)", "A.F2(nuint)").WithLocation(9, 17),
                // (13,21): warning CS0109: The member 'B3.F1(nuint)' does not hide an accessible member. The new keyword is not required.
                //     public new void F1(nuint x) { }
                Diagnostic(ErrorCode.WRN_NewNotRequired, "F1").WithArguments("B3.F1(nuint)").WithLocation(13, 21),
                // (14,21): warning CS0109: The member 'B3.F2(IntPtr)' does not hide an accessible member. The new keyword is not required.
                //     public new void F2(System.IntPtr y) { }
                Diagnostic(ErrorCode.WRN_NewNotRequired, "F2").WithArguments("B3.F2(System.IntPtr)").WithLocation(14, 21)
            };

            var comp = CreateCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(diagnostics);

            comp = CreateCompilation(sourceA, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            var ref1 = comp.ToMetadataReference();
            var ref2 = comp.EmitToImageReference();

            comp = CreateCompilation(sourceB, references: new[] { ref1 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(diagnostics);

            comp = CreateCompilation(sourceB, references: new[] { ref2 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(diagnostics);
        }

        [Fact]
        public void Overloads_02()
        {
            var sourceA =
@"public class A
{
    public void F1(System.IntPtr x) { }
    public void F2(System.UIntPtr y) { }
}";
            var sourceB =
@"class B1 : A
{
    public void F1(nint x) { base.F1(x); }
    public void F2(nuint y) { base.F2(y); }
}
class B2 : A
{
    public void F1(nuint x) { }
    public void F2(nint y) { }
}
class B3 : A
{
    public void F1(nint x) { base.F1(x); }
    public void F2(nuint y) { base.F2(y); }
}
class B4 : A
{
    public void F1(nuint x) { }
    public void F2(nint y) { }
}";

            var comp = CreateCompilation(sourceA, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
            var ref1 = comp.ToMetadataReference();
            var ref2 = comp.EmitToImageReference();

            var diagnostics = new[]
            {
                // (3,17): warning CS0108: 'B1.F1(nint)' hides inherited member 'A.F1(IntPtr)'. Use the new keyword if hiding was intended.
                //     public void F1(nint x) { base.F1(x); }
                Diagnostic(ErrorCode.WRN_NewRequired, "F1").WithArguments("B1.F1(nint)", "A.F1(System.IntPtr)").WithLocation(3, 17),
                // (4,17): warning CS0108: 'B1.F2(nuint)' hides inherited member 'A.F2(UIntPtr)'. Use the new keyword if hiding was intended.
                //     public void F2(nuint y) { base.F2(y); }
                Diagnostic(ErrorCode.WRN_NewRequired, "F2").WithArguments("B1.F2(nuint)", "A.F2(System.UIntPtr)").WithLocation(4, 17),
                // (13,17): warning CS0108: 'B3.F1(nint)' hides inherited member 'A.F1(IntPtr)'. Use the new keyword if hiding was intended.
                //     public void F1(nint x) { base.F1(x); }
                Diagnostic(ErrorCode.WRN_NewRequired, "F1").WithArguments("B3.F1(nint)", "A.F1(System.IntPtr)").WithLocation(13, 17),
                // (14,17): warning CS0108: 'B3.F2(nuint)' hides inherited member 'A.F2(UIntPtr)'. Use the new keyword if hiding was intended.
                //     public void F2(nuint y) { base.F2(y); }
                Diagnostic(ErrorCode.WRN_NewRequired, "F2").WithArguments("B3.F2(nuint)", "A.F2(System.UIntPtr)").WithLocation(14, 17)
            };

            comp = CreateCompilation(sourceB, references: new[] { ref1 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(diagnostics);

            comp = CreateCompilation(sourceB, references: new[] { ref2 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(diagnostics);
        }

        [Fact]
        public void Overloads_03()
        {
            var sourceA =
@"public class A
{
    public void F1(nint x) { }
    public void F2(nuint y) { }
}";
            var sourceB =
@"class B1 : A
{
    public void F1(System.UIntPtr x) { }
    public void F2(System.IntPtr y) { }
}
class B2 : A
{
    public void F1(System.IntPtr x) { base.F1(x); }
    public void F2(System.UIntPtr y) { base.F2(y); }
}
class B3 : A
{
    public void F1(System.UIntPtr x) { }
    public void F2(System.IntPtr y) { }
}
class B4 : A
{
    public void F1(System.IntPtr x) { base.F1(x); }
    public void F2(System.UIntPtr y) { base.F2(y); }
}";

            var comp = CreateCompilation(sourceA, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            var ref1 = comp.ToMetadataReference();
            var ref2 = comp.EmitToImageReference();

            var diagnostics = new[]
            {
                // (8,17): warning CS0108: 'B2.F1(IntPtr)' hides inherited member 'A.F1(nint)'. Use the new keyword if hiding was intended.
                //     public void F1(System.IntPtr x) { base.F1(x); }
                Diagnostic(ErrorCode.WRN_NewRequired, "F1").WithArguments("B2.F1(System.IntPtr)", "A.F1(nint)").WithLocation(8, 17),
                // (9,17): warning CS0108: 'B2.F2(UIntPtr)' hides inherited member 'A.F2(nuint)'. Use the new keyword if hiding was intended.
                //     public void F2(System.UIntPtr y) { base.F2(y); }
                Diagnostic(ErrorCode.WRN_NewRequired, "F2").WithArguments("B2.F2(System.UIntPtr)", "A.F2(nuint)").WithLocation(9, 17),
                // (18,17): warning CS0108: 'B4.F1(IntPtr)' hides inherited member 'A.F1(nint)'. Use the new keyword if hiding was intended.
                //     public void F1(System.IntPtr x) { base.F1(x); }
                Diagnostic(ErrorCode.WRN_NewRequired, "F1").WithArguments("B4.F1(System.IntPtr)", "A.F1(nint)").WithLocation(18, 17),
                // (19,17): warning CS0108: 'B4.F2(UIntPtr)' hides inherited member 'A.F2(nuint)'. Use the new keyword if hiding was intended.
                //     public void F2(System.UIntPtr y) { base.F2(y); }
                Diagnostic(ErrorCode.WRN_NewRequired, "F2").WithArguments("B4.F2(System.UIntPtr)", "A.F2(nuint)").WithLocation(19, 17)
            };

            comp = CreateCompilation(sourceB, references: new[] { ref1 }, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(diagnostics);

            comp = CreateCompilation(sourceB, references: new[] { ref2 }, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(diagnostics);
        }

        [Fact]
        public void Overloads_04()
        {
            var source =
@"interface I
{
    void F(System.IntPtr x);
    void F(System.UIntPtr x);
    void F(nint y);
}
class C
{
    static void F(System.UIntPtr x) { }
    static void F(nint y) { }
    static void F(nuint y) { }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,10): error CS0111: Type 'I' already defines a member called 'F' with the same parameter types
                //     void F(nint y);
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "F").WithArguments("F", "I").WithLocation(5, 10),
                // (11,17): error CS0111: Type 'C' already defines a member called 'F' with the same parameter types
                //     static void F(nuint y) { }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "F").WithArguments("F", "C").WithLocation(11, 17));
        }

        [Fact]
        public void Overloads_05()
        {
            var source =
@"interface I
{
    object this[System.IntPtr x] { get; }
    object this[nint y] { get; set; }
}
class C
{
    object this[nuint x] => null;
    object this[System.UIntPtr y] { get { return null; } set { } }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,12): error CS0111: Type 'I' already defines a member called 'this' with the same parameter types
                //     object this[nint y] { get; set; }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "this").WithArguments("this", "I").WithLocation(4, 12),
                // (9,12): error CS0111: Type 'C' already defines a member called 'this' with the same parameter types
                //     object this[System.UIntPtr y] { get { return null; } set { } }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "this").WithArguments("this", "C").WithLocation(9, 12));
        }

        [Fact]
        public void Overloads_06()
        {
            var source1 =
@"public interface IA
{
    void F1(nint i);
    void F2(nuint i);
}
public interface IB
{
    void F1(System.IntPtr i);
    void F2(System.UIntPtr i);
}";
            var comp = CreateCompilation(source1, parseOptions: TestOptions.RegularPreview);
            var ref1 = comp.EmitToImageReference();

            var source2 =
@"class C : IA, IB
{
    public void F1(System.IntPtr i) { }
    public void F2(System.UIntPtr i) { }
}";
            comp = CreateCompilation(source2, references: new[] { ref1 }, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();

            var source3 =
@"class C1 : IA, IB
{
    public void F1(nint i) { }
    public void F2(System.UIntPtr i) { }
}
class C2 : IA, IB
{
    public void F1(System.IntPtr i) { }
    public void F2(nuint i) { }
}";
            comp = CreateCompilation(source3, references: new[] { ref1 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Partial_01()
        {
            var source =
@"partial class Program
{
    static partial void F1(System.IntPtr x);
    static partial void F2(System.UIntPtr x) { }
    static partial void F1(nint x) { }
    static partial void F2(nuint x);
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Constraints_01()
        {
            var sourceA =
@"public class A<T>
{
    public static void F<U>() where U : T { }
}
public class B1 : A<nint> { }
public class B2 : A<nuint> { }
public class B3 : A<System.IntPtr> { }
public class B4 : A<System.UIntPtr> { }
";
            var sourceB =
@"class Program
{
    static void Main()
    {
        B1.F<System.IntPtr>();
        B2.F<System.UIntPtr>();
        B3.F<nint>();
        B4.F<nuint>();
    }
}";

            var comp = CreateCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(sourceA, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            var ref1 = comp.ToMetadataReference();
            var ref2 = comp.EmitToImageReference();

            comp = CreateCompilation(sourceB, references: new[] { ref1 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(sourceB, references: new[] { ref2 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Constraints_02()
        {
            var sourceA =
@"public class A<T>
{
    public static void F<U>() where U : T { }
}
public class B1 : A<System.IntPtr> { }
public class B2 : A<System.UIntPtr> { }
";
            var sourceB =
@"class Program
{
    static void Main()
    {
        B1.F<nint>();
        B2.F<nuint>();
    }
}";

            var comp = CreateCompilation(sourceA, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
            var ref1 = comp.ToMetadataReference();
            var ref2 = comp.EmitToImageReference();

            comp = CreateCompilation(sourceB, references: new[] { ref1 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(sourceB, references: new[] { ref2 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Constraints_03()
        {
            var sourceA =
@"public class A<T>
{
    public static void F<U>() where U : T { }
}
public class B1 : A<nint> { }
public class B2 : A<nuint> { }
";
            var sourceB =
@"class Program
{
    static void Main()
    {
        B1.F<System.IntPtr>();
        B2.F<System.UIntPtr>();
    }
}";

            var comp = CreateCompilation(sourceA, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            var ref1 = comp.ToMetadataReference();
            var ref2 = comp.EmitToImageReference();

            comp = CreateCompilation(sourceB, references: new[] { ref1 }, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(sourceB, references: new[] { ref2 }, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ClassName()
        {
            var source =
@"class nint
{
}
interface I
{
    nint Add(nint x, nuint y);
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,22): error CS8652: The feature 'native-sized integers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     nint Add(nint x, nuint y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "nuint").WithArguments("native-sized integers").WithLocation(6, 22));
            verify(comp);

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                var tree = comp.SyntaxTrees[0];
                var nodes = tree.GetRoot().DescendantNodes().ToArray();
                var model = comp.GetSemanticModel(tree);
                var underlyingType = model.GetDeclaredSymbol(nodes.OfType<ClassDeclarationSyntax>().Single());
                Assert.Equal("nint", underlyingType.ToTestDisplayString());
                Assert.Equal(SpecialType.None, underlyingType.SpecialType);
                var method = model.GetDeclaredSymbol(nodes.OfType<MethodDeclarationSyntax>().Single());
                Assert.Equal("nint I.Add(nint x, nuint y)", method.ToTestDisplayString());
                var underlyingType0 = method.Parameters[0].Type.GetSymbol<NamedTypeSymbol>();
                var underlyingType1 = method.Parameters[1].Type.GetSymbol<NamedTypeSymbol>();
                Assert.Equal(SpecialType.None, underlyingType0.SpecialType);
                Assert.False(underlyingType0.IsNativeIntegerType);
                Assert.Equal(SpecialType.System_UIntPtr, underlyingType1.SpecialType);
                Assert.True(underlyingType1.IsNativeIntegerType);
            }
        }

        [Fact]
        public void AliasName_01()
        {
            var source =
@"using nint = System.Int16;
interface I
{
    nint Add(nint x, nuint y);
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (4,22): error CS8652: The feature 'native-sized integers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     nint Add(nint x, nuint y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "nuint").WithArguments("native-sized integers").WithLocation(4, 22));
            verify(comp);

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                var method = comp.GetMember<MethodSymbol>("I.Add");
                Assert.Equal("System.Int16 I.Add(System.Int16 x, nuint y)", method.ToTestDisplayString());
                var underlyingType0 = (NamedTypeSymbol)method.Parameters[0].Type;
                var underlyingType1 = (NamedTypeSymbol)method.Parameters[1].Type;
                Assert.Equal(SpecialType.System_Int16, underlyingType0.SpecialType);
                Assert.False(underlyingType0.IsNativeIntegerType);
                Assert.Equal(SpecialType.System_UIntPtr, underlyingType1.SpecialType);
                Assert.True(underlyingType1.IsNativeIntegerType);
            }
        }

        [WorkItem(42975, "https://github.com/dotnet/roslyn/issues/42975")]
        [Fact]
        public void AliasName_02()
        {
            var source =
@"using N = nint;
class Program
{
    N F() => default;
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (1,11): error CS8652: The feature 'native-sized integers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // using N = nint;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "nint").WithArguments("native-sized integers").WithLocation(1, 11));

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void MemberName_01()
        {
            var source =
@"namespace N
{
    class nint { }
    class Program
    {
        internal static object nuint;
        static void Main()
        {
            _ = new nint();
            _ = new @nint();
            _ = new N.nint();
            @nint i = null;
            _ = i;
            @nuint = null;
            _ = nuint;
            _ = @nuint;
            _ = Program.nuint;
        }
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
            verify(comp);

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                var tree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(tree);
                var nodes = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().ToArray();
                Assert.Equal(3, nodes.Length);
                foreach (var node in nodes)
                {
                    var type = model.GetTypeInfo(node).Type;
                    Assert.Equal("N.nint", type.ToTestDisplayString());
                    Assert.Equal(SpecialType.None, type.SpecialType);
                    Assert.False(type.IsNativeIntegerType);
                }
            }
        }

        [Fact]
        public void MemberName_02()
        {
            var source =
@"class Program
{
    static void Main()
    {
        _ = nint.Equals(0, 0);
        _ = nuint.Equals(0, 0);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (5,13): error CS8652: The feature 'native-sized integers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         _ = nint.Equals(0, 0);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "nint").WithArguments("native-sized integers").WithLocation(5, 13),
                // (6,13): error CS8652: The feature 'native-sized integers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         _ = nuint.Equals(0, 0);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "nuint").WithArguments("native-sized integers").WithLocation(6, 13));

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NameOf_01()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Console.WriteLine(nameof(nint));
        Console.WriteLine(nameof(nuint));
    }
}";

            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,34): error CS8652: The feature 'native-sized integers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Console.WriteLine(nameof(nint));
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "nint").WithArguments("native-sized integers").WithLocation(6, 34),
                // (7,34): error CS8652: The feature 'native-sized integers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Console.WriteLine(nameof(nuint));
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "nuint").WithArguments("native-sized integers").WithLocation(7, 34));

            comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput:
@"nint
nuint");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().First(n => n is IdentifierNameSyntax { Identifier: { ValueText: "nint" } });
            var symbol = (ITypeSymbol)model.GetSymbolInfo(node).Symbol;
            Assert.Equal("nint", symbol.ToTestDisplayString());
            Assert.True(symbol.IsNativeIntegerType);
        }

        [Fact]
        public void NameOf_02()
        {
            var source =
@"class Program
{
    static void F(nint nint)
    {
        _ = nameof(nint);
        _ = nameof(nuint);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (3,19): error CS8652: The feature 'native-sized integers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     static void F(nint nint)
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "nint").WithArguments("native-sized integers").WithLocation(3, 19),
                // (6,20): error CS8652: The feature 'native-sized integers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         _ = nameof(nuint);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "nuint").WithArguments("native-sized integers").WithLocation(6, 20));

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NameOf_03()
        {
            var source =
@"class Program
{
    static void F()
    {
        _ = nameof(@nint);
        _ = nameof(@nuint);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (5,20): error CS0103: The name 'nint' does not exist in the current context
                //         _ = nameof(@nint);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "@nint").WithArguments("nint").WithLocation(5, 20),
                // (6,20): error CS0103: The name 'nuint' does not exist in the current context
                //         _ = nameof(@nuint);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "@nuint").WithArguments("nuint").WithLocation(6, 20));

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,20): error CS0103: The name 'nint' does not exist in the current context
                //         _ = nameof(@nint);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "@nint").WithArguments("nint").WithLocation(5, 20),
                // (6,20): error CS0103: The name 'nuint' does not exist in the current context
                //         _ = nameof(@nuint);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "@nuint").WithArguments("nuint").WithLocation(6, 20));
        }

        [Fact]
        public void NameOf_04()
        {
            var source =
@"class Program
{
    static void F(int @nint, uint @nuint)
    {
        _ = nameof(@nint);
        _ = nameof(@nuint);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        /// <summary>
        /// sizeof(IntPtr) and sizeof(nint) require compiling with /unsafe.
        /// </summary>
        [Fact]
        public void SizeOf_01()
        {
            var source =
@"class Program
{
    static void Main()
    {
        _ = sizeof(System.IntPtr);
        _ = sizeof(System.UIntPtr);
        _ = sizeof(nint);
        _ = sizeof(nuint);
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,13): error CS0233: 'IntPtr' does not have a predefined size, therefore sizeof can only be used in an unsafe context
                //         _ = sizeof(System.IntPtr);
                Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(System.IntPtr)").WithArguments("System.IntPtr").WithLocation(5, 13),
                // (6,13): error CS0233: 'UIntPtr' does not have a predefined size, therefore sizeof can only be used in an unsafe context
                //         _ = sizeof(System.UIntPtr);
                Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(System.UIntPtr)").WithArguments("System.UIntPtr").WithLocation(6, 13),
                // (7,13): error CS0233: 'nint' does not have a predefined size, therefore sizeof can only be used in an unsafe context
                //         _ = sizeof(nint);
                Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(nint)").WithArguments("nint").WithLocation(7, 13),
                // (8,13): error CS0233: 'nuint' does not have a predefined size, therefore sizeof can only be used in an unsafe context
                //         _ = sizeof(nuint);
                Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(nuint)").WithArguments("nuint").WithLocation(8, 13));
        }

        [Fact]
        public void SizeOf_02()
        {
            var source =
@"using System;
class Program
{
    unsafe static void Main()
    {
        Console.Write(sizeof(System.IntPtr));
        Console.Write(sizeof(System.UIntPtr));
        Console.Write(sizeof(nint));
        Console.Write(sizeof(nuint));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseExe, parseOptions: TestOptions.RegularPreview);
            int size = IntPtr.Size;
            var verifier = CompileAndVerify(comp, expectedOutput: $"{size}{size}{size}{size}");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size       45 (0x2d)
  .maxstack  1
  IL_0000:  sizeof     ""System.IntPtr""
  IL_0006:  call       ""void System.Console.Write(int)""
  IL_000b:  sizeof     ""System.UIntPtr""
  IL_0011:  call       ""void System.Console.Write(int)""
  IL_0016:  sizeof     ""System.IntPtr""
  IL_001c:  call       ""void System.Console.Write(int)""
  IL_0021:  sizeof     ""System.UIntPtr""
  IL_0027:  call       ""void System.Console.Write(int)""
  IL_002c:  ret
}");
        }

        [Fact]
        public void SizeOf_03()
        {
            var source =
@"using System.Collections.Generic;
unsafe class Program
{
    static IEnumerable<int> F()
    {
        yield return sizeof(nint);
        yield return sizeof(nuint);
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (6,22): error CS1629: Unsafe code may not appear in iterators
                //         yield return sizeof(nint);
                Diagnostic(ErrorCode.ERR_IllegalInnerUnsafe, "sizeof(nint)").WithLocation(6, 22),
                // (7,22): error CS1629: Unsafe code may not appear in iterators
                //         yield return sizeof(nuint);
                Diagnostic(ErrorCode.ERR_IllegalInnerUnsafe, "sizeof(nuint)").WithLocation(7, 22));
        }

        [Fact]
        public void SizeOf_04()
        {
            var source =
@"unsafe class Program
{
    const int A = sizeof(System.IntPtr);
    const int B = sizeof(System.UIntPtr);
    const int C = sizeof(nint);
    const int D = sizeof(nuint);
}";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (3,19): error CS0133: The expression being assigned to 'Program.A' must be constant
                //     const int A = sizeof(System.IntPtr);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "sizeof(System.IntPtr)").WithArguments("Program.A").WithLocation(3, 19),
                // (4,19): error CS0133: The expression being assigned to 'Program.B' must be constant
                //     const int B = sizeof(System.UIntPtr);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "sizeof(System.UIntPtr)").WithArguments("Program.B").WithLocation(4, 19),
                // (5,19): error CS0133: The expression being assigned to 'Program.C' must be constant
                //     const int C = sizeof(nint);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "sizeof(nint)").WithArguments("Program.C").WithLocation(5, 19),
                // (6,19): error CS0133: The expression being assigned to 'Program.D' must be constant
                //     const int D = sizeof(nuint);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "sizeof(nuint)").WithArguments("Program.D").WithLocation(6, 19));
        }

        [WorkItem(42453, "https://github.com/dotnet/roslyn/issues/42453")]
        [Fact]
        public void ReadOnlyField_VirtualMethods()
        {
            string source =
@"using System;
using System.Linq.Expressions;
class MyInt
{
    private readonly nint _i;
    internal MyInt(nint i)
    {
        _i = i;
    }
    public override string ToString()
    {
        return _i.ToString();
    }
    public override int GetHashCode()
    {
        return ((Func<int>)_i.GetHashCode)();
    }
    public override bool Equals(object other)
    {
        return _i.Equals((other as MyInt)?._i);
    }
    internal string ToStringFromExpr()
    {
        Expression<Func<string>> e = () => ((Func<string>)_i.ToString)();
        return e.Compile()();
    }
    internal int GetHashCodeFromExpr()
    {
        Expression<Func<int>> e = () => _i.GetHashCode();
        return e.Compile()();
    }
}
class Program
{
    static void Main()
    {
        var m = new MyInt(42);
        Console.WriteLine(m);
        Console.WriteLine(m.GetHashCode());
        Console.WriteLine(m.Equals(null));
        Console.WriteLine(m.ToStringFromExpr());
        Console.WriteLine(m.GetHashCodeFromExpr());
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            // PEVerify fails with "MyInt::ToString][mdToken=0x6000007][offset 0x00000001] Cannot change initonly field outside its .ctor."
            var verifier = CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput:
$@"42
{42.GetHashCode()}
False
42
{42.GetHashCode()}");
            // The code generated below does not correspond to the code generated for `ToString()` when `_i` is `int`.
            // For `int _i`, we generate:
            //   ldarg.0
            //   ldfld      ""int MyInt._i""
            //   call       ""string int.ToString()""
            // The code below is valid but equivalent to code for a struct type that does not override ToString().
            // https://github.com/dotnet/roslyn/issues/42453: Should NativeIntegerTypeSymbol.GetMembers() include
            // ToString(), Equals(), GetHashCode()? That would fix this difference and avoid the PEVerify failure above.
            verifier.VerifyIL("MyInt.ToString",
@"{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""nint MyInt._i""
  IL_0006:  constrained. ""System.IntPtr""
  IL_000c:  callvirt   ""string object.ToString()""
  IL_0011:  ret
}");
            verifier.VerifyIL("MyInt.GetHashCode",
@"{
  // Code size       29 (0x1d)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""nint MyInt._i""
  IL_0006:  box        ""System.IntPtr""
  IL_000b:  dup
  IL_000c:  ldvirtftn  ""int object.GetHashCode()""
  IL_0012:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0017:  callvirt   ""int System.Func<int>.Invoke()""
  IL_001c:  ret
}");
            verifier.VerifyIL("MyInt.Equals",
@"{
  // Code size       54 (0x36)
  .maxstack  3
  .locals init (nint? V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""nint MyInt._i""
  IL_0006:  ldarg.1
  IL_0007:  isinst     ""MyInt""
  IL_000c:  dup
  IL_000d:  brtrue.s   IL_001b
  IL_000f:  pop
  IL_0010:  ldloca.s   V_0
  IL_0012:  initobj    ""nint?""
  IL_0018:  ldloc.0
  IL_0019:  br.s       IL_0025
  IL_001b:  ldfld      ""nint MyInt._i""
  IL_0020:  newobj     ""nint?..ctor(nint)""
  IL_0025:  box        ""nint?""
  IL_002a:  constrained. ""System.IntPtr""
  IL_0030:  callvirt   ""bool object.Equals(object)""
  IL_0035:  ret
}");
        }

        /// <summary>
        /// Verify there is the number of built in operators for { nint, nuint, nint?, nuint? }
        /// for each operator kind.
        /// </summary>
        [Fact]
        public void BuiltInOperators()
        {
            var source = "";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
            verifyOperators(comp);

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            verifyOperators(comp);

            static void verifyOperators(CSharpCompilation comp)
            {
                var unaryOperators = new[]
                {
                    UnaryOperatorKind.PostfixIncrement,
                    UnaryOperatorKind.PostfixDecrement,
                    UnaryOperatorKind.PrefixIncrement,
                    UnaryOperatorKind.PrefixDecrement,
                    UnaryOperatorKind.UnaryPlus,
                    UnaryOperatorKind.UnaryMinus,
                    UnaryOperatorKind.BitwiseComplement,
                };

                var binaryOperators = new[]
                {
                    BinaryOperatorKind.Addition,
                    BinaryOperatorKind.Subtraction,
                    BinaryOperatorKind.Multiplication,
                    BinaryOperatorKind.Division,
                    BinaryOperatorKind.Remainder,
                    BinaryOperatorKind.LessThan,
                    BinaryOperatorKind.LessThanOrEqual,
                    BinaryOperatorKind.GreaterThan,
                    BinaryOperatorKind.GreaterThanOrEqual,
                    BinaryOperatorKind.LeftShift,
                    BinaryOperatorKind.RightShift,
                    BinaryOperatorKind.Equal,
                    BinaryOperatorKind.NotEqual,
                    BinaryOperatorKind.Or,
                    BinaryOperatorKind.And,
                    BinaryOperatorKind.Xor,
                };

                foreach (var operatorKind in unaryOperators)
                {
                    verifyUnaryOperators(comp, operatorKind, skipNativeIntegerOperators: true);
                    verifyUnaryOperators(comp, operatorKind, skipNativeIntegerOperators: false);
                }

                foreach (var operatorKind in binaryOperators)
                {
                    verifyBinaryOperators(comp, operatorKind, skipNativeIntegerOperators: true);
                    verifyBinaryOperators(comp, operatorKind, skipNativeIntegerOperators: false);
                }

                static void verifyUnaryOperators(CSharpCompilation comp, UnaryOperatorKind operatorKind, bool skipNativeIntegerOperators)
                {
                    var builder = ArrayBuilder<UnaryOperatorSignature>.GetInstance();
                    comp.builtInOperators.GetSimpleBuiltInOperators(operatorKind, builder, skipNativeIntegerOperators);
                    var operators = builder.ToImmutableAndFree();
                    int expectedSigned = skipNativeIntegerOperators ? 0 : 1;
                    int expectedUnsigned = skipNativeIntegerOperators ? 0 : (operatorKind == UnaryOperatorKind.UnaryMinus) ? 0 : 1;
                    verifyOperators(operators, (op, signed) => isNativeInt(op.OperandType, signed), expectedSigned, expectedUnsigned);
                    verifyOperators(operators, (op, signed) => isNullableNativeInt(op.OperandType, signed), expectedSigned, expectedUnsigned);
                }

                static void verifyBinaryOperators(CSharpCompilation comp, BinaryOperatorKind operatorKind, bool skipNativeIntegerOperators)
                {
                    var builder = ArrayBuilder<BinaryOperatorSignature>.GetInstance();
                    comp.builtInOperators.GetSimpleBuiltInOperators(operatorKind, builder, skipNativeIntegerOperators);
                    var operators = builder.ToImmutableAndFree();
                    int expected = skipNativeIntegerOperators ? 0 : 1;
                    verifyOperators(operators, (op, signed) => isNativeInt(op.LeftType, signed), expected, expected);
                    verifyOperators(operators, (op, signed) => isNullableNativeInt(op.LeftType, signed), expected, expected);
                }

                static void verifyOperators<T>(ImmutableArray<T> operators, Func<T, bool, bool> predicate, int expectedSigned, int expectedUnsigned)
                {
                    Assert.Equal(expectedSigned, operators.Count(op => predicate(op, true)));
                    Assert.Equal(expectedUnsigned, operators.Count(op => predicate(op, false)));
                }

                static bool isNativeInt(TypeSymbol type, bool signed)
                {
                    return type.IsNativeIntegerType &&
                        type.SpecialType == (signed ? SpecialType.System_IntPtr : SpecialType.System_UIntPtr);
                }

                static bool isNullableNativeInt(TypeSymbol type, bool signed)
                {
                    return type.IsNullableType() && isNativeInt(type.GetNullableUnderlyingType(), signed);
                }
            }
        }

        [WorkItem(3259, "https://github.com/dotnet/csharplang/issues/3259")]
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void BuiltInConversions_CSharp8(bool useCompilationReference)
        {
            var sourceA =
@"public class A
{
    public static nint F1;
    public static nuint F2;
    public static nint? F3;
    public static nuint? F4;
}";
            var sourceB =
@"class B : A
{
    static void M1()
    {
        long x = F1;
        ulong y = F2;
        long? z = F3;
        ulong? w = F4;
    }
    static void M2(int x, uint y, int? z, uint? w)
    {
        F1 = x;
        F2 = y;
        F3 = z;
        F4 = w;
    }
}";
            var comp = CreateCompilation(sourceA, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(sourceB, references: new[] { AsReference(comp, useCompilationReference) }, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp);
            verifier.VerifyIL("B.M1",
@"{
  // Code size       59 (0x3b)
  .maxstack  1
  .locals init (nint? V_0,
                nuint? V_1)
  IL_0000:  ldsfld     ""nint A.F1""
  IL_0005:  pop
  IL_0006:  ldsfld     ""nuint A.F2""
  IL_000b:  pop
  IL_000c:  ldsfld     ""nint? A.F3""
  IL_0011:  stloc.0
  IL_0012:  ldloca.s   V_0
  IL_0014:  call       ""bool nint?.HasValue.get""
  IL_0019:  brfalse.s  IL_0023
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       ""nint nint?.GetValueOrDefault()""
  IL_0022:  pop
  IL_0023:  ldsfld     ""nuint? A.F4""
  IL_0028:  stloc.1
  IL_0029:  ldloca.s   V_1
  IL_002b:  call       ""bool nuint?.HasValue.get""
  IL_0030:  brfalse.s  IL_003a
  IL_0032:  ldloca.s   V_1
  IL_0034:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_0039:  pop
  IL_003a:  ret
}");
            verifier.VerifyIL("B.M2",
@"{
  // Code size       95 (0x5f)
  .maxstack  1
  .locals init (int? V_0,
                nint? V_1,
                uint? V_2,
                nuint? V_3)
  IL_0000:  ldarg.0
  IL_0001:  conv.i
  IL_0002:  stsfld     ""nint A.F1""
  IL_0007:  ldarg.1
  IL_0008:  conv.u
  IL_0009:  stsfld     ""nuint A.F2""
  IL_000e:  ldarg.2
  IL_000f:  stloc.0
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       ""bool int?.HasValue.get""
  IL_0017:  brtrue.s   IL_0024
  IL_0019:  ldloca.s   V_1
  IL_001b:  initobj    ""nint?""
  IL_0021:  ldloc.1
  IL_0022:  br.s       IL_0031
  IL_0024:  ldloca.s   V_0
  IL_0026:  call       ""int int?.GetValueOrDefault()""
  IL_002b:  conv.i
  IL_002c:  newobj     ""nint?..ctor(nint)""
  IL_0031:  stsfld     ""nint? A.F3""
  IL_0036:  ldarg.3
  IL_0037:  stloc.2
  IL_0038:  ldloca.s   V_2
  IL_003a:  call       ""bool uint?.HasValue.get""
  IL_003f:  brtrue.s   IL_004c
  IL_0041:  ldloca.s   V_3
  IL_0043:  initobj    ""nuint?""
  IL_0049:  ldloc.3
  IL_004a:  br.s       IL_0059
  IL_004c:  ldloca.s   V_2
  IL_004e:  call       ""uint uint?.GetValueOrDefault()""
  IL_0053:  conv.u
  IL_0054:  newobj     ""nuint?..ctor(nuint)""
  IL_0059:  stsfld     ""nuint? A.F4""
  IL_005e:  ret
}");
        }

        [WorkItem(3259, "https://github.com/dotnet/csharplang/issues/3259")]
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void BuiltInOperators_CSharp8(bool useCompilationReference)
        {
            var sourceA =
@"public class A
{
    public static nint F1;
    public static nuint F2;
    public static nint? F3;
    public static nuint? F4;
}";
            var sourceB =
@"class B : A
{
    static void Main()
    {
        _ = -F1;
        _ = +F2;
        _ = -F3;
        _ = +F4;
        _ = F1 * F1;
        _ = F2 / F2;
        _ = F3 * F1;
        _ = F4 / F2;
    }
}";
            var comp = CreateCompilation(sourceA, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(sourceB, references: new[] { AsReference(comp, useCompilationReference) }, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp);
            verifier.VerifyIL("B.Main",
@"{
  // Code size      143 (0x8f)
  .maxstack  2
  .locals init (nint? V_0,
                nuint? V_1,
                System.IntPtr V_2,
                System.UIntPtr V_3)
  IL_0000:  ldsfld     ""nint A.F1""
  IL_0005:  pop
  IL_0006:  ldsfld     ""nuint A.F2""
  IL_000b:  pop
  IL_000c:  ldsfld     ""nint? A.F3""
  IL_0011:  stloc.0
  IL_0012:  ldloca.s   V_0
  IL_0014:  call       ""bool nint?.HasValue.get""
  IL_0019:  brfalse.s  IL_0023
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       ""nint nint?.GetValueOrDefault()""
  IL_0022:  pop
  IL_0023:  ldsfld     ""nuint? A.F4""
  IL_0028:  stloc.1
  IL_0029:  ldloca.s   V_1
  IL_002b:  call       ""bool nuint?.HasValue.get""
  IL_0030:  brfalse.s  IL_003a
  IL_0032:  ldloca.s   V_1
  IL_0034:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_0039:  pop
  IL_003a:  ldsfld     ""nint A.F1""
  IL_003f:  pop
  IL_0040:  ldsfld     ""nint A.F1""
  IL_0045:  pop
  IL_0046:  ldsfld     ""nuint A.F2""
  IL_004b:  ldsfld     ""nuint A.F2""
  IL_0050:  div.un
  IL_0051:  pop
  IL_0052:  ldsfld     ""nint? A.F3""
  IL_0057:  stloc.0
  IL_0058:  ldsfld     ""nint A.F1""
  IL_005d:  stloc.2
  IL_005e:  ldloca.s   V_0
  IL_0060:  call       ""bool nint?.HasValue.get""
  IL_0065:  brfalse.s  IL_006f
  IL_0067:  ldloca.s   V_0
  IL_0069:  call       ""nint nint?.GetValueOrDefault()""
  IL_006e:  pop
  IL_006f:  ldsfld     ""nuint? A.F4""
  IL_0074:  stloc.1
  IL_0075:  ldsfld     ""nuint A.F2""
  IL_007a:  stloc.3
  IL_007b:  ldloca.s   V_1
  IL_007d:  call       ""bool nuint?.HasValue.get""
  IL_0082:  brfalse.s  IL_008e
  IL_0084:  ldloca.s   V_1
  IL_0086:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_008b:  ldloc.3
  IL_008c:  div.un
  IL_008d:  pop
  IL_008e:  ret
}");
        }

        [Fact]
        public void BuiltInConversions_UnderlyingTypes()
        {
            var source =
@"class A
{
    static System.IntPtr F1;
    static System.UIntPtr F2;
    static System.IntPtr? F3;
    static System.UIntPtr? F4;
    static void M1()
    {
        long x = F1;
        ulong y = F2;
        long? z = F3;
        ulong? w = F4;
    }
    static void M2(int x, uint y, int? z, uint? w)
    {
        F1 = x;
        F2 = y;
        F3 = z;
        F4 = w;
    }
}";
            var diagnostics = new[]
            {
                // (9,18): error CS0266: Cannot implicitly convert type 'System.IntPtr' to 'long'. An explicit conversion exists (are you missing a cast?)
                //         long x = F1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "F1").WithArguments("System.IntPtr", "long").WithLocation(9, 18),
                // (10,19): error CS0266: Cannot implicitly convert type 'System.UIntPtr' to 'ulong'. An explicit conversion exists (are you missing a cast?)
                //         ulong y = F2;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "F2").WithArguments("System.UIntPtr", "ulong").WithLocation(10, 19),
                // (11,19): error CS0266: Cannot implicitly convert type 'System.IntPtr?' to 'long?'. An explicit conversion exists (are you missing a cast?)
                //         long? z = F3;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "F3").WithArguments("System.IntPtr?", "long?").WithLocation(11, 19),
                // (12,20): error CS0266: Cannot implicitly convert type 'System.UIntPtr?' to 'ulong?'. An explicit conversion exists (are you missing a cast?)
                //         ulong? w = F4;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "F4").WithArguments("System.UIntPtr?", "ulong?").WithLocation(12, 20),
                // (16,14): error CS0266: Cannot implicitly convert type 'int' to 'System.IntPtr'. An explicit conversion exists (are you missing a cast?)
                //         F1 = x;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("int", "System.IntPtr").WithLocation(16, 14),
                // (17,14): error CS0266: Cannot implicitly convert type 'uint' to 'System.UIntPtr'. An explicit conversion exists (are you missing a cast?)
                //         F2 = y;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "y").WithArguments("uint", "System.UIntPtr").WithLocation(17, 14),
                // (18,14): error CS0266: Cannot implicitly convert type 'int?' to 'System.IntPtr?'. An explicit conversion exists (are you missing a cast?)
                //         F3 = z;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "z").WithArguments("int?", "System.IntPtr?").WithLocation(18, 14),
                // (19,14): error CS0266: Cannot implicitly convert type 'uint?' to 'System.UIntPtr?'. An explicit conversion exists (are you missing a cast?)
                //         F4 = w;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "w").WithArguments("uint?", "System.UIntPtr?").WithLocation(19, 14)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(diagnostics);

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(diagnostics);
        }

        [WorkItem(3259, "https://github.com/dotnet/csharplang/issues/3259")]
        [Fact]
        public void BuiltInOperators_UnderlyingTypes()
        {
            var source =
@"#pragma warning disable 649
class A
{
    static System.IntPtr F1;
    static System.UIntPtr F2;
    static System.IntPtr? F3;
    static System.UIntPtr? F4;
    static void Main()
    {
        F1 = -F1;
        F2 = +F2;
        F3 = -F3;
        F4 = +F4;
        F1 = F1 * F1;
        F2 = F2 / F2;
        F3 = F3 * F1;
        F4 = F4 / F2;
    }
}";
            var diagnostics = new[]
            {
                // (10,14): error CS0023: Operator '-' cannot be applied to operand of type 'IntPtr'
                //         F1 = -F1;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "-F1").WithArguments("-", "System.IntPtr").WithLocation(10, 14),
                // (11,14): error CS0023: Operator '+' cannot be applied to operand of type 'UIntPtr'
                //         F2 = +F2;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "+F2").WithArguments("+", "System.UIntPtr").WithLocation(11, 14),
                // (12,14): error CS0023: Operator '-' cannot be applied to operand of type 'IntPtr?'
                //         F3 = -F3;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "-F3").WithArguments("-", "System.IntPtr?").WithLocation(12, 14),
                // (13,14): error CS0023: Operator '+' cannot be applied to operand of type 'UIntPtr?'
                //         F4 = +F4;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "+F4").WithArguments("+", "System.UIntPtr?").WithLocation(13, 14),
                // (14,14): error CS0019: Operator '*' cannot be applied to operands of type 'IntPtr' and 'IntPtr'
                //         F1 = F1 * F1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "F1 * F1").WithArguments("*", "System.IntPtr", "System.IntPtr").WithLocation(14, 14),
                // (15,14): error CS0019: Operator '/' cannot be applied to operands of type 'UIntPtr' and 'UIntPtr'
                //         F2 = F2 / F2;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "F2 / F2").WithArguments("/", "System.UIntPtr", "System.UIntPtr").WithLocation(15, 14),
                // (16,14): error CS0019: Operator '*' cannot be applied to operands of type 'IntPtr?' and 'IntPtr'
                //         F3 = F3 * F1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "F3 * F1").WithArguments("*", "System.IntPtr?", "System.IntPtr").WithLocation(16, 14),
                // (17,14): error CS0019: Operator '/' cannot be applied to operands of type 'UIntPtr?' and 'UIntPtr'
                //         F4 = F4 / F2;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "F4 / F2").WithArguments("/", "System.UIntPtr?", "System.UIntPtr").WithLocation(17, 14)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(diagnostics);

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(diagnostics);
        }

        [WorkItem(3259, "https://github.com/dotnet/csharplang/issues/3259")]
        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public void BuiltInConversions_NativeIntegers(bool useCompilationReference, bool useLatest)
        {
            var sourceA =
@"public class A
{
    public static nint F1;
    public static nuint F2;
    public static nint? F3;
    public static nuint? F4;
}";
            var comp = CreateCompilation(sourceA, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            var sourceB =
@"class B : A
{
    static void M1()
    {
        long x = F1;
        ulong y = F2;
        long? z = F3;
        ulong? w = F4;
    }
    static void M2(int x, uint y, int? z, uint? w)
    {
        F1 = x;
        F2 = y;
        F3 = z;
        F4 = w;
    }
}";
            comp = CreateCompilation(sourceB, references: new[] { AsReference(comp, useCompilationReference) }, parseOptions: useLatest ? TestOptions.RegularPreview : TestOptions.Regular8);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp);
            verifier.VerifyIL("B.M1",
@"{
  // Code size       59 (0x3b)
  .maxstack  1
  .locals init (nint? V_0,
                nuint? V_1)
  IL_0000:  ldsfld     ""nint A.F1""
  IL_0005:  pop
  IL_0006:  ldsfld     ""nuint A.F2""
  IL_000b:  pop
  IL_000c:  ldsfld     ""nint? A.F3""
  IL_0011:  stloc.0
  IL_0012:  ldloca.s   V_0
  IL_0014:  call       ""bool nint?.HasValue.get""
  IL_0019:  brfalse.s  IL_0023
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       ""nint nint?.GetValueOrDefault()""
  IL_0022:  pop
  IL_0023:  ldsfld     ""nuint? A.F4""
  IL_0028:  stloc.1
  IL_0029:  ldloca.s   V_1
  IL_002b:  call       ""bool nuint?.HasValue.get""
  IL_0030:  brfalse.s  IL_003a
  IL_0032:  ldloca.s   V_1
  IL_0034:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_0039:  pop
  IL_003a:  ret
}");
            verifier.VerifyIL("B.M2",
@"{
  // Code size       95 (0x5f)
  .maxstack  1
  .locals init (int? V_0,
                nint? V_1,
                uint? V_2,
                nuint? V_3)
  IL_0000:  ldarg.0
  IL_0001:  conv.i
  IL_0002:  stsfld     ""nint A.F1""
  IL_0007:  ldarg.1
  IL_0008:  conv.u
  IL_0009:  stsfld     ""nuint A.F2""
  IL_000e:  ldarg.2
  IL_000f:  stloc.0
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       ""bool int?.HasValue.get""
  IL_0017:  brtrue.s   IL_0024
  IL_0019:  ldloca.s   V_1
  IL_001b:  initobj    ""nint?""
  IL_0021:  ldloc.1
  IL_0022:  br.s       IL_0031
  IL_0024:  ldloca.s   V_0
  IL_0026:  call       ""int int?.GetValueOrDefault()""
  IL_002b:  conv.i
  IL_002c:  newobj     ""nint?..ctor(nint)""
  IL_0031:  stsfld     ""nint? A.F3""
  IL_0036:  ldarg.3
  IL_0037:  stloc.2
  IL_0038:  ldloca.s   V_2
  IL_003a:  call       ""bool uint?.HasValue.get""
  IL_003f:  brtrue.s   IL_004c
  IL_0041:  ldloca.s   V_3
  IL_0043:  initobj    ""nuint?""
  IL_0049:  ldloc.3
  IL_004a:  br.s       IL_0059
  IL_004c:  ldloca.s   V_2
  IL_004e:  call       ""uint uint?.GetValueOrDefault()""
  IL_0053:  conv.u
  IL_0054:  newobj     ""nuint?..ctor(nuint)""
  IL_0059:  stsfld     ""nuint? A.F4""
  IL_005e:  ret
}");
        }

        [WorkItem(3259, "https://github.com/dotnet/csharplang/issues/3259")]
        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public void BuiltInOperators_NativeIntegers(bool useCompilationReference, bool useLatest)
        {
            var sourceA =
@"public class A
{
    public static nint F1;
    public static nuint F2;
    public static nint? F3;
    public static nuint? F4;
}";
            var comp = CreateCompilation(sourceA, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            var sourceB =
@"class B : A
{
    static void Main()
    {
        F1 = -F1;
        F2 = +F2;
        F3 = -F3;
        F4 = +F4;
        F1 = F1 * F1;
        F2 = F2 / F2;
        F3 = F3 * F1;
        F4 = F4 / F2;
    }
}";
            // https://github.com/dotnet/csharplang/blob/master/meetings/2020/LDM-2020-03-25.md: Errors should
            // be reported for uses of native integer operators with -langversion:8.
            comp = CreateCompilation(sourceB, references: new[] { AsReference(comp, useCompilationReference) }, parseOptions: useLatest ? TestOptions.RegularPreview : TestOptions.Regular8);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp);
            verifier.VerifyIL("B.Main",
@"{
  // Code size      247 (0xf7)
  .maxstack  2
  .locals init (nint? V_0,
                nint? V_1,
                nuint? V_2,
                nuint? V_3,
                System.IntPtr V_4,
                System.UIntPtr V_5)
  IL_0000:  ldsfld     ""nint A.F1""
  IL_0005:  neg
  IL_0006:  stsfld     ""nint A.F1""
  IL_000b:  ldsfld     ""nuint A.F2""
  IL_0010:  stsfld     ""nuint A.F2""
  IL_0015:  ldsfld     ""nint? A.F3""
  IL_001a:  stloc.0
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       ""bool nint?.HasValue.get""
  IL_0022:  brtrue.s   IL_002f
  IL_0024:  ldloca.s   V_1
  IL_0026:  initobj    ""nint?""
  IL_002c:  ldloc.1
  IL_002d:  br.s       IL_003c
  IL_002f:  ldloca.s   V_0
  IL_0031:  call       ""nint nint?.GetValueOrDefault()""
  IL_0036:  neg
  IL_0037:  newobj     ""nint?..ctor(nint)""
  IL_003c:  stsfld     ""nint? A.F3""
  IL_0041:  ldsfld     ""nuint? A.F4""
  IL_0046:  stloc.2
  IL_0047:  ldloca.s   V_2
  IL_0049:  call       ""bool nuint?.HasValue.get""
  IL_004e:  brtrue.s   IL_005b
  IL_0050:  ldloca.s   V_3
  IL_0052:  initobj    ""nuint?""
  IL_0058:  ldloc.3
  IL_0059:  br.s       IL_0067
  IL_005b:  ldloca.s   V_2
  IL_005d:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_0062:  newobj     ""nuint?..ctor(nuint)""
  IL_0067:  stsfld     ""nuint? A.F4""
  IL_006c:  ldsfld     ""nint A.F1""
  IL_0071:  ldsfld     ""nint A.F1""
  IL_0076:  mul
  IL_0077:  stsfld     ""nint A.F1""
  IL_007c:  ldsfld     ""nuint A.F2""
  IL_0081:  ldsfld     ""nuint A.F2""
  IL_0086:  div.un
  IL_0087:  stsfld     ""nuint A.F2""
  IL_008c:  ldsfld     ""nint? A.F3""
  IL_0091:  stloc.0
  IL_0092:  ldsfld     ""nint A.F1""
  IL_0097:  stloc.s    V_4
  IL_0099:  ldloca.s   V_0
  IL_009b:  call       ""bool nint?.HasValue.get""
  IL_00a0:  brtrue.s   IL_00ad
  IL_00a2:  ldloca.s   V_1
  IL_00a4:  initobj    ""nint?""
  IL_00aa:  ldloc.1
  IL_00ab:  br.s       IL_00bc
  IL_00ad:  ldloca.s   V_0
  IL_00af:  call       ""nint nint?.GetValueOrDefault()""
  IL_00b4:  ldloc.s    V_4
  IL_00b6:  mul
  IL_00b7:  newobj     ""nint?..ctor(nint)""
  IL_00bc:  stsfld     ""nint? A.F3""
  IL_00c1:  ldsfld     ""nuint? A.F4""
  IL_00c6:  stloc.2
  IL_00c7:  ldsfld     ""nuint A.F2""
  IL_00cc:  stloc.s    V_5
  IL_00ce:  ldloca.s   V_2
  IL_00d0:  call       ""bool nuint?.HasValue.get""
  IL_00d5:  brtrue.s   IL_00e2
  IL_00d7:  ldloca.s   V_3
  IL_00d9:  initobj    ""nuint?""
  IL_00df:  ldloc.3
  IL_00e0:  br.s       IL_00f1
  IL_00e2:  ldloca.s   V_2
  IL_00e4:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_00e9:  ldloc.s    V_5
  IL_00eb:  div.un
  IL_00ec:  newobj     ""nuint?..ctor(nuint)""
  IL_00f1:  stsfld     ""nuint? A.F4""
  IL_00f6:  ret
}");
        }

        private static MetadataReference AsReference(CSharpCompilation comp, bool useCompilationReference)
        {
            return useCompilationReference ? comp.ToMetadataReference() : comp.EmitToImageReference();
        }

        [Theory]
        [InlineData("")]
        [InlineData("unchecked")]
        [InlineData("checked")]
        public void ConstantConversions_ToNativeInt(string context)
        {
            var source =
$@"#pragma warning disable 219
class Program
{{
    static void F1()
    {{
        nint i;
        {context}
        {{
            i = sbyte.MaxValue;
            i = byte.MaxValue;
            i = char.MaxValue;
            i = short.MaxValue;
            i = ushort.MaxValue;
            i = int.MaxValue;
            i = uint.MaxValue;
            i = long.MaxValue;
            i = ulong.MaxValue;
            i = float.MaxValue;
            i = double.MaxValue;
            i = (decimal)int.MaxValue;
            i = (nint)int.MaxValue;
            i = (nuint)uint.MaxValue;
        }}
    }}
    static void F2()
    {{
        nuint u;
        {context}
        {{
            u = sbyte.MaxValue;
            u = byte.MaxValue;
            u = char.MaxValue;
            u = short.MaxValue;
            u = ushort.MaxValue;
            u = int.MaxValue;
            u = uint.MaxValue;
            u = long.MaxValue;
            u = ulong.MaxValue;
            u = float.MaxValue;
            u = double.MaxValue;
            u = (decimal)uint.MaxValue;
            u = (nint)int.MaxValue;
            u = (nuint)uint.MaxValue;
        }}
    }}
}}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (15,17): error CS0266: Cannot implicitly convert type 'uint' to 'nint'. An explicit conversion exists (are you missing a cast?)
                //             i = uint.MaxValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "uint.MaxValue").WithArguments("uint", "nint").WithLocation(15, 17),
                // (16,17): error CS0266: Cannot implicitly convert type 'long' to 'nint'. An explicit conversion exists (are you missing a cast?)
                //             i = long.MaxValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "long.MaxValue").WithArguments("long", "nint").WithLocation(16, 17),
                // (17,17): error CS0266: Cannot implicitly convert type 'ulong' to 'nint'. An explicit conversion exists (are you missing a cast?)
                //             i = ulong.MaxValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "ulong.MaxValue").WithArguments("ulong", "nint").WithLocation(17, 17),
                // (18,17): error CS0266: Cannot implicitly convert type 'float' to 'nint'. An explicit conversion exists (are you missing a cast?)
                //             i = float.MaxValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "float.MaxValue").WithArguments("float", "nint").WithLocation(18, 17),
                // (19,17): error CS0266: Cannot implicitly convert type 'double' to 'nint'. An explicit conversion exists (are you missing a cast?)
                //             i = double.MaxValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "double.MaxValue").WithArguments("double", "nint").WithLocation(19, 17),
                // (20,17): error CS0266: Cannot implicitly convert type 'decimal' to 'nint'. An explicit conversion exists (are you missing a cast?)
                //             i = (decimal)int.MaxValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(decimal)int.MaxValue").WithArguments("decimal", "nint").WithLocation(20, 17),
                // (22,17): error CS0266: Cannot implicitly convert type 'nuint' to 'nint'. An explicit conversion exists (are you missing a cast?)
                //             i = (nuint)uint.MaxValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(nuint)uint.MaxValue").WithArguments("nuint", "nint").WithLocation(22, 17),
                // (37,17): error CS0266: Cannot implicitly convert type 'long' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //             u = long.MaxValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "long.MaxValue").WithArguments("long", "nuint").WithLocation(37, 17),
                // (38,17): error CS0266: Cannot implicitly convert type 'ulong' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //             u = ulong.MaxValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "ulong.MaxValue").WithArguments("ulong", "nuint").WithLocation(38, 17),
                // (39,17): error CS0266: Cannot implicitly convert type 'float' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //             u = float.MaxValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "float.MaxValue").WithArguments("float", "nuint").WithLocation(39, 17),
                // (40,17): error CS0266: Cannot implicitly convert type 'double' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //             u = double.MaxValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "double.MaxValue").WithArguments("double", "nuint").WithLocation(40, 17),
                // (41,17): error CS0266: Cannot implicitly convert type 'decimal' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //             u = (decimal)uint.MaxValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(decimal)uint.MaxValue").WithArguments("decimal", "nuint").WithLocation(41, 17),
                // (42,17): error CS0266: Cannot implicitly convert type 'nint' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //             u = (nint)int.MaxValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(nint)int.MaxValue").WithArguments("nint", "nuint").WithLocation(42, 17));
        }

        [Theory]
        [InlineData("")]
        [InlineData("unchecked")]
        [InlineData("checked")]
        public void ConstantConversions_FromNativeInt(string context)
        {
            var source =
$@"#pragma warning disable 219
class Program
{{
    static void F1()
    {{
        const nint n = (nint)int.MaxValue;
        {context}
        {{
            sbyte sb = n;
            byte b = n;
            char c = n;
            short s = n;
            ushort us = n;
            int i = n;
            uint u = n;
            long l = n;
            ulong ul = n;
            float f = n;
            double d = n;
            decimal dec = n;
            nuint nu = n;
        }}
    }}
    static void F2()
    {{
        const nuint nu = (nuint)uint.MaxValue;
        {context}
        {{
            sbyte sb = nu;
            byte b = nu;
            char c = nu;
            short s = nu;
            ushort us = nu;
            int i = nu;
            uint u = nu;
            long l = nu;
            ulong ul = nu;
            float f = nu;
            double d = nu;
            decimal dec = nu;
            nint n = nu;
        }}
    }}
}}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (9,24): error CS0266: Cannot implicitly convert type 'nint' to 'sbyte'. An explicit conversion exists (are you missing a cast?)
                //             sbyte sb = n;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "n").WithArguments("nint", "sbyte").WithLocation(9, 24),
                // (10,22): error CS0266: Cannot implicitly convert type 'nint' to 'byte'. An explicit conversion exists (are you missing a cast?)
                //             byte b = n;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "n").WithArguments("nint", "byte").WithLocation(10, 22),
                // (11,22): error CS0266: Cannot implicitly convert type 'nint' to 'char'. An explicit conversion exists (are you missing a cast?)
                //             char c = n;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "n").WithArguments("nint", "char").WithLocation(11, 22),
                // (12,23): error CS0266: Cannot implicitly convert type 'nint' to 'short'. An explicit conversion exists (are you missing a cast?)
                //             short s = n;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "n").WithArguments("nint", "short").WithLocation(12, 23),
                // (13,25): error CS0266: Cannot implicitly convert type 'nint' to 'ushort'. An explicit conversion exists (are you missing a cast?)
                //             ushort us = n;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "n").WithArguments("nint", "ushort").WithLocation(13, 25),
                // (14,21): error CS0266: Cannot implicitly convert type 'nint' to 'int'. An explicit conversion exists (are you missing a cast?)
                //             int i = n;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "n").WithArguments("nint", "int").WithLocation(14, 21),
                // (15,22): error CS0266: Cannot implicitly convert type 'nint' to 'uint'. An explicit conversion exists (are you missing a cast?)
                //             uint u = n;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "n").WithArguments("nint", "uint").WithLocation(15, 22),
                // (17,24): error CS0266: Cannot implicitly convert type 'nint' to 'ulong'. An explicit conversion exists (are you missing a cast?)
                //             ulong ul = n;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "n").WithArguments("nint", "ulong").WithLocation(17, 24),
                // (21,24): error CS0266: Cannot implicitly convert type 'nint' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //             nuint nu = n;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "n").WithArguments("nint", "nuint").WithLocation(21, 24),
                // (29,24): error CS0266: Cannot implicitly convert type 'nuint' to 'sbyte'. An explicit conversion exists (are you missing a cast?)
                //             sbyte sb = nu;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "nu").WithArguments("nuint", "sbyte").WithLocation(29, 24),
                // (30,22): error CS0266: Cannot implicitly convert type 'nuint' to 'byte'. An explicit conversion exists (are you missing a cast?)
                //             byte b = nu;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "nu").WithArguments("nuint", "byte").WithLocation(30, 22),
                // (31,22): error CS0266: Cannot implicitly convert type 'nuint' to 'char'. An explicit conversion exists (are you missing a cast?)
                //             char c = nu;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "nu").WithArguments("nuint", "char").WithLocation(31, 22),
                // (32,23): error CS0266: Cannot implicitly convert type 'nuint' to 'short'. An explicit conversion exists (are you missing a cast?)
                //             short s = nu;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "nu").WithArguments("nuint", "short").WithLocation(32, 23),
                // (33,25): error CS0266: Cannot implicitly convert type 'nuint' to 'ushort'. An explicit conversion exists (are you missing a cast?)
                //             ushort us = nu;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "nu").WithArguments("nuint", "ushort").WithLocation(33, 25),
                // (34,21): error CS0266: Cannot implicitly convert type 'nuint' to 'int'. An explicit conversion exists (are you missing a cast?)
                //             int i = nu;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "nu").WithArguments("nuint", "int").WithLocation(34, 21),
                // (35,22): error CS0266: Cannot implicitly convert type 'nuint' to 'uint'. An explicit conversion exists (are you missing a cast?)
                //             uint u = nu;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "nu").WithArguments("nuint", "uint").WithLocation(35, 22),
                // (36,22): error CS0266: Cannot implicitly convert type 'nuint' to 'long'. An explicit conversion exists (are you missing a cast?)
                //             long l = nu;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "nu").WithArguments("nuint", "long").WithLocation(36, 22),
                // (41,22): error CS0266: Cannot implicitly convert type 'nuint' to 'nint'. An explicit conversion exists (are you missing a cast?)
                //             nint n = nu;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "nu").WithArguments("nuint", "nint").WithLocation(41, 22));
        }

        [Fact]
        public void Constants_NInt()
        {
            string source =
$@"class Program
{{
    static void Main()
    {{
        F(default);
        F(int.MinValue);
        F({short.MinValue - 1});
        F(short.MinValue);
        F(sbyte.MinValue);
        F(-2);
        F(-1);
        F(0);
        F(1);
        F(2);
        F(3);
        F(4);
        F(5);
        F(6);
        F(7);
        F(8);
        F(9);
        F(sbyte.MaxValue);
        F(byte.MaxValue);
        F(short.MaxValue);
        F(char.MaxValue);
        F(ushort.MaxValue);
        F({ushort.MaxValue + 1});
        F(int.MaxValue);
    }}
    static void F(nint n)
    {{
        System.Console.WriteLine(n);
    }}
}}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            string expectedOutput =
@"0
-2147483648
-32769
-32768
-128
-2
-1
0
1
2
3
4
5
6
7
8
9
127
255
32767
65535
65535
65536
2147483647";
            var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput);
            string expectedIL =
@"{
  // Code size      209 (0xd1)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  conv.i
  IL_0002:  call       ""void Program.F(nint)""
  IL_0007:  ldc.i4     0x80000000
  IL_000c:  conv.i
  IL_000d:  call       ""void Program.F(nint)""
  IL_0012:  ldc.i4     0xffff7fff
  IL_0017:  conv.i
  IL_0018:  call       ""void Program.F(nint)""
  IL_001d:  ldc.i4     0xffff8000
  IL_0022:  conv.i
  IL_0023:  call       ""void Program.F(nint)""
  IL_0028:  ldc.i4.s   -128
  IL_002a:  conv.i
  IL_002b:  call       ""void Program.F(nint)""
  IL_0030:  ldc.i4.s   -2
  IL_0032:  conv.i
  IL_0033:  call       ""void Program.F(nint)""
  IL_0038:  ldc.i4.m1
  IL_0039:  conv.i
  IL_003a:  call       ""void Program.F(nint)""
  IL_003f:  ldc.i4.0
  IL_0040:  conv.i
  IL_0041:  call       ""void Program.F(nint)""
  IL_0046:  ldc.i4.1
  IL_0047:  conv.i
  IL_0048:  call       ""void Program.F(nint)""
  IL_004d:  ldc.i4.2
  IL_004e:  conv.i
  IL_004f:  call       ""void Program.F(nint)""
  IL_0054:  ldc.i4.3
  IL_0055:  conv.i
  IL_0056:  call       ""void Program.F(nint)""
  IL_005b:  ldc.i4.4
  IL_005c:  conv.i
  IL_005d:  call       ""void Program.F(nint)""
  IL_0062:  ldc.i4.5
  IL_0063:  conv.i
  IL_0064:  call       ""void Program.F(nint)""
  IL_0069:  ldc.i4.6
  IL_006a:  conv.i
  IL_006b:  call       ""void Program.F(nint)""
  IL_0070:  ldc.i4.7
  IL_0071:  conv.i
  IL_0072:  call       ""void Program.F(nint)""
  IL_0077:  ldc.i4.8
  IL_0078:  conv.i
  IL_0079:  call       ""void Program.F(nint)""
  IL_007e:  ldc.i4.s   9
  IL_0080:  conv.i
  IL_0081:  call       ""void Program.F(nint)""
  IL_0086:  ldc.i4.s   127
  IL_0088:  conv.i
  IL_0089:  call       ""void Program.F(nint)""
  IL_008e:  ldc.i4     0xff
  IL_0093:  conv.i
  IL_0094:  call       ""void Program.F(nint)""
  IL_0099:  ldc.i4     0x7fff
  IL_009e:  conv.i
  IL_009f:  call       ""void Program.F(nint)""
  IL_00a4:  ldc.i4     0xffff
  IL_00a9:  conv.i
  IL_00aa:  call       ""void Program.F(nint)""
  IL_00af:  ldc.i4     0xffff
  IL_00b4:  conv.i
  IL_00b5:  call       ""void Program.F(nint)""
  IL_00ba:  ldc.i4     0x10000
  IL_00bf:  conv.i
  IL_00c0:  call       ""void Program.F(nint)""
  IL_00c5:  ldc.i4     0x7fffffff
  IL_00ca:  conv.i
  IL_00cb:  call       ""void Program.F(nint)""
  IL_00d0:  ret
}";
            verifier.VerifyIL("Program.Main", expectedIL);
        }

        [Fact]
        public void Constants_NUInt()
        {
            string source =
$@"class Program
{{
    static void Main()
    {{
        F(default);
        F(0);
        F(1);
        F(2);
        F(3);
        F(4);
        F(5);
        F(6);
        F(7);
        F(8);
        F(9);
        F(sbyte.MaxValue);
        F(byte.MaxValue);
        F(short.MaxValue);
        F(char.MaxValue);
        F(ushort.MaxValue);
        F(int.MaxValue);
        F({(uint)int.MaxValue + 1});
        F(uint.MaxValue);
    }}
    static void F(nuint n)
    {{
        System.Console.WriteLine(n);
    }}
}}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            string expectedOutput =
@"0
0
1
2
3
4
5
6
7
8
9
127
255
32767
65535
65535
2147483647
2147483648
4294967295";
            var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput);
            string expectedIL =
@"{
  // Code size      160 (0xa0)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  conv.i
  IL_0002:  call       ""void Program.F(nuint)""
  IL_0007:  ldc.i4.0
  IL_0008:  conv.i
  IL_0009:  call       ""void Program.F(nuint)""
  IL_000e:  ldc.i4.1
  IL_000f:  conv.i
  IL_0010:  call       ""void Program.F(nuint)""
  IL_0015:  ldc.i4.2
  IL_0016:  conv.i
  IL_0017:  call       ""void Program.F(nuint)""
  IL_001c:  ldc.i4.3
  IL_001d:  conv.i
  IL_001e:  call       ""void Program.F(nuint)""
  IL_0023:  ldc.i4.4
  IL_0024:  conv.i
  IL_0025:  call       ""void Program.F(nuint)""
  IL_002a:  ldc.i4.5
  IL_002b:  conv.i
  IL_002c:  call       ""void Program.F(nuint)""
  IL_0031:  ldc.i4.6
  IL_0032:  conv.i
  IL_0033:  call       ""void Program.F(nuint)""
  IL_0038:  ldc.i4.7
  IL_0039:  conv.i
  IL_003a:  call       ""void Program.F(nuint)""
  IL_003f:  ldc.i4.8
  IL_0040:  conv.i
  IL_0041:  call       ""void Program.F(nuint)""
  IL_0046:  ldc.i4.s   9
  IL_0048:  conv.i
  IL_0049:  call       ""void Program.F(nuint)""
  IL_004e:  ldc.i4.s   127
  IL_0050:  conv.i
  IL_0051:  call       ""void Program.F(nuint)""
  IL_0056:  ldc.i4     0xff
  IL_005b:  conv.i
  IL_005c:  call       ""void Program.F(nuint)""
  IL_0061:  ldc.i4     0x7fff
  IL_0066:  conv.i
  IL_0067:  call       ""void Program.F(nuint)""
  IL_006c:  ldc.i4     0xffff
  IL_0071:  conv.i
  IL_0072:  call       ""void Program.F(nuint)""
  IL_0077:  ldc.i4     0xffff
  IL_007c:  conv.i
  IL_007d:  call       ""void Program.F(nuint)""
  IL_0082:  ldc.i4     0x7fffffff
  IL_0087:  conv.i
  IL_0088:  call       ""void Program.F(nuint)""
  IL_008d:  ldc.i4     0x80000000
  IL_0092:  conv.u
  IL_0093:  call       ""void Program.F(nuint)""
  IL_0098:  ldc.i4.m1
  IL_0099:  conv.u
  IL_009a:  call       ""void Program.F(nuint)""
  IL_009f:  ret
}";
            verifier.VerifyIL("Program.Main", expectedIL);
        }

        [Fact]
        public void Constants_Locals()
        {
            var source =
@"#pragma warning disable 219
class Program
{
    static void Main()
    {
        const System.IntPtr a = default;
        const nint b = default;
        const System.UIntPtr c = default;
        const nuint d = default;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (6,15): error CS0283: The type 'IntPtr' cannot be declared const
                //         const System.IntPtr a = default;
                Diagnostic(ErrorCode.ERR_BadConstType, "System.IntPtr").WithArguments("System.IntPtr").WithLocation(6, 15),
                // (8,15): error CS0283: The type 'UIntPtr' cannot be declared const
                //         const System.UIntPtr c = default;
                Diagnostic(ErrorCode.ERR_BadConstType, "System.UIntPtr").WithArguments("System.UIntPtr").WithLocation(8, 15));
        }

        [Fact]
        public void Constants_Fields_01()
        {
            var source =
@"class Program
{
    const System.IntPtr A = default(System.IntPtr);
    const nint B = default(nint);
    const System.UIntPtr C = default(System.UIntPtr);
    const nuint D = default(nuint);
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (3,5): error CS0283: The type 'IntPtr' cannot be declared const
                //     const System.IntPtr A = default(System.IntPtr);
                Diagnostic(ErrorCode.ERR_BadConstType, "const").WithArguments("System.IntPtr").WithLocation(3, 5),
                // (3,29): error CS0133: The expression being assigned to 'Program.A' must be constant
                //     const System.IntPtr A = default(System.IntPtr);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "default(System.IntPtr)").WithArguments("Program.A").WithLocation(3, 29),
                // (5,5): error CS0283: The type 'UIntPtr' cannot be declared const
                //     const System.UIntPtr C = default(System.UIntPtr);
                Diagnostic(ErrorCode.ERR_BadConstType, "const").WithArguments("System.UIntPtr").WithLocation(5, 5),
                // (5,30): error CS0133: The expression being assigned to 'Program.C' must be constant
                //     const System.UIntPtr C = default(System.UIntPtr);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "default(System.UIntPtr)").WithArguments("Program.C").WithLocation(5, 30));
        }

        [Fact]
        public void Constants_Fields_02()
        {
            var source0 =
@"public class A
{
    public const nint C1 = -42;
    public const nuint C2 = 42;
}";
            var comp = CreateCompilation(source0, parseOptions: TestOptions.RegularPreview);
            var ref0 = comp.EmitToImageReference();
            var source1 =
@"using System;
class B
{
    static void Main()
    {
        Console.WriteLine(A.C1);
        Console.WriteLine(A.C2);
    }
}";
            comp = CreateCompilation(source1, references: new[] { ref0 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"-42
42");
        }

        [Fact]
        public void Constants_ParameterDefaults()
        {
            var source0 =
@"public class A
{
    public static System.IntPtr F1(System.IntPtr i = default) => i;
    public static nint F2(nint i = -42) => i;
    public static System.UIntPtr F3(System.UIntPtr u = default) => u;
    public static nuint F4(nuint u = 42) => u;
}";
            var comp = CreateCompilation(source0, parseOptions: TestOptions.RegularPreview);
            var ref0 = comp.EmitToImageReference();
            var source1 =
@"using System;
class B
{
    static void Main()
    {
        Console.WriteLine(A.F1());
        Console.WriteLine(A.F2());
        Console.WriteLine(A.F3());
        Console.WriteLine(A.F4());
    }
}";
            comp = CreateCompilation(source1, references: new[] { ref0 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"0
-42
0
42");
        }

        [Fact]
        public void Constants_FromMetadata()
        {
            var source0 =
@"public class Constants
{
    public const nint NIntMin = int.MinValue;
    public const nint NIntMax = int.MaxValue;
    public const nuint NUIntMin = uint.MinValue;
    public const nuint NUIntMax = uint.MaxValue;
}";
            var comp = CreateCompilation(source0, parseOptions: TestOptions.RegularPreview);
            var ref0 = comp.EmitToImageReference();

            var source1 =
@"using System;
class Program
{
    static void Main()
    {
        const nint nintMin = Constants.NIntMin;
        const nint nintMax = Constants.NIntMax;
        const nuint nuintMin = Constants.NUIntMin;
        const nuint nuintMax = Constants.NUIntMax;
        Console.WriteLine(nintMin);
        Console.WriteLine(nintMax);
        Console.WriteLine(nuintMin);
        Console.WriteLine(nuintMax);
    }
}";
            comp = CreateCompilation(source1, references: new[] { ref0 }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp, expectedOutput:
@"-2147483648
2147483647
0
4294967295");
        }

        [Fact]
        public void ConstantValue_Properties()
        {
            var source =
@"class Program
{
    const nint A = int.MinValue;
    const nint B = 0;
    const nint C = int.MaxValue;
    const nuint D = 0;
    const nuint E = uint.MaxValue;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            verify((FieldSymbol)comp.GetMember("Program.A"), int.MinValue, signed: true, negative: true);
            verify((FieldSymbol)comp.GetMember("Program.B"), 0, signed: true, negative: false);
            verify((FieldSymbol)comp.GetMember("Program.C"), int.MaxValue, signed: true, negative: false);
            verify((FieldSymbol)comp.GetMember("Program.D"), 0U, signed: false, negative: false);
            verify((FieldSymbol)comp.GetMember("Program.E"), uint.MaxValue, signed: false, negative: false);

            static void verify(FieldSymbol field, object expectedValue, bool signed, bool negative)
            {
                var value = field.GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false);
                Assert.Equal(signed ? ConstantValueTypeDiscriminator.NInt : ConstantValueTypeDiscriminator.NUInt, value.Discriminator);
                Assert.Equal(expectedValue, value.Value);
                Assert.True(value.IsIntegral);
                Assert.True(value.IsNumeric);
                Assert.Equal(negative, value.IsNegativeNumeric);
                Assert.Equal(!signed, value.IsUnsigned);
            }
        }

        /// <summary>
        /// Native integers cannot be used as attribute values.
        /// </summary>
        [Fact]
        public void AttributeValue_01()
        {
            var source0 =
@"class A : System.Attribute
{
    public A() { }
    public A(object value) { }
    public object Value;
}
[A((nint)1)]
[A(new nuint[0])]
[A(Value = (nint)3)]
[A(Value = new[] { (nuint)4 })]
class B
{
}";
            var comp = CreateCompilation(source0, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (7,4): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [A((nint)1)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "(nint)1").WithLocation(7, 4),
                // (8,4): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [A(new nuint[0])]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "new nuint[0]").WithLocation(8, 4),
                // (9,12): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [A(Value = (nint)3)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "(nint)3").WithLocation(9, 12),
                // (10,12): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [A(Value = new[] { (nuint)4 })]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "new[] { (nuint)4 }").WithLocation(10, 12));
        }

        /// <summary>
        /// Native integers cannot be used as attribute values.
        /// </summary>
        [Fact]
        public void AttributeValue_02()
        {
            var source0 =
@"class A : System.Attribute
{
    public A() { }
    public A(nint value) { }
    public nuint[] Value;
}
[A(1)]
[A(Value = default)]
class B
{
}";
            var comp = CreateCompilation(source0, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (7,2): error CS0181: Attribute constructor parameter 'value' has type 'nint', which is not a valid attribute parameter type
                // [A(1)]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "A").WithArguments("value", "nint").WithLocation(7, 2),
                // (8,4): error CS0655: 'Value' is not a valid named attribute argument because it is not a valid attribute parameter type
                // [A(Value = default)]
                Diagnostic(ErrorCode.ERR_BadNamedAttributeArgumentType, "Value").WithArguments("Value").WithLocation(8, 4));
        }

        [Fact]
        public void ParameterDefaultValue_01()
        {
            var source =
@"using System;
class A
{
    static void F0(IntPtr x = default, UIntPtr y = default)
    {
    }
    static void F1(IntPtr x = (IntPtr)(-1), UIntPtr y = (UIntPtr)2)
    {
    }
    static void F2(IntPtr? x = null, UIntPtr? y = null)
    {
    }
    static void F3(IntPtr? x = (IntPtr)(-3), UIntPtr? y = (UIntPtr)4)
    {
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,31): error CS1736: Default parameter value for 'x' must be a compile-time constant
                //     static void F1(IntPtr x = (IntPtr)(-1), UIntPtr y = (UIntPtr)2)
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "(IntPtr)(-1)").WithArguments("x").WithLocation(7, 31),
                // (7,57): error CS1736: Default parameter value for 'y' must be a compile-time constant
                //     static void F1(IntPtr x = (IntPtr)(-1), UIntPtr y = (UIntPtr)2)
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "(UIntPtr)2").WithArguments("y").WithLocation(7, 57),
                // (13,32): error CS1736: Default parameter value for 'x' must be a compile-time constant
                //     static void F3(IntPtr? x = (IntPtr)(-3), UIntPtr? y = (UIntPtr)4)
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "(IntPtr)(-3)").WithArguments("x").WithLocation(13, 32),
                // (13,59): error CS1736: Default parameter value for 'y' must be a compile-time constant
                //     static void F3(IntPtr? x = (IntPtr)(-3), UIntPtr? y = (UIntPtr)4)
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "(UIntPtr)4").WithArguments("y").WithLocation(13, 59));
        }

        [Fact]
        public void ParameterDefaultValue_02()
        {
            var sourceA =
@"public class A
{
    public static void F0(nint x = default, nuint y = default)
    {
        Report(x);
        Report(y);
    }
    public static void F1(nint x = -1, nuint y = 2)
    {
        Report(x);
        Report(y);
    }
    public static void F2(nint? x = null, nuint? y = null)
    {
        Report(x);
        Report(y);
    }
    public static void F3(nint? x = -3, nuint? y = 4)
    {
        Report(x);
        Report(y);
    }
    static void Report(object o)
    {
        System.Console.WriteLine(o ?? ""null"");
    }
}";
            var sourceB =
@"class B
{
    static void Main()
    {
        A.F0();
        A.F1();
        A.F2();
        A.F3();
    }
}";
            var expectedOutput =
@"0
0
-1
2
null
null
-3
4";

            var comp = CreateCompilation(new[] { sourceA, sourceB }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            comp = CreateCompilation(sourceA, parseOptions: TestOptions.RegularPreview);
            var ref1 = comp.ToMetadataReference();
            var ref2 = comp.EmitToImageReference();

            comp = CreateCompilation(sourceB, references: new[] { ref1 }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            comp = CreateCompilation(sourceB, references: new[] { ref2 }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            comp = CreateCompilation(sourceB, references: new[] { ref1 }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            comp = CreateCompilation(sourceB, references: new[] { ref2 }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [Fact]
        public void SwitchStatement_01()
        {
            var source =
@"using System;
class Program
{
   static nint M(nint ret)
    {
        switch (ret) {
        case 0:
            ret--; // 2
            Report(""case 0: "", ret);
            goto case 9999;
        case 2:
            ret--; // 4
            Report(""case 2: "", ret);
            goto case 255;
        case 6: // start here
            ret--; // 5
            Report(""case 6: "", ret);
            goto case 2;
        case 9999:
            ret--; // 1
            Report(""case 9999: "", ret);
            goto default;
        case 0xff:
            ret--; // 3
            Report(""case 0xff: "", ret);
            goto case 0;
        default:
            ret--;
            Report(""default: "", ret);
            if (ret > 0) {
                goto case -1;
            }
            break;
        case -1:
            ret = 999;
            Report(""case -1: "", ret);
            break;
        }
        return(ret);
    }
    static void Report(string prefix, nint value)
    {
        Console.WriteLine(prefix + value);
    }
    static void Main()
    {
        Console.WriteLine(M(6));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput:
@"case 6: 5
case 2: 4
case 0xff: 3
case 0: 2
case 9999: 1
default: 0
0");
            verifier.VerifyIL("Program.M",
@"{
  // Code size      170 (0xaa)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_0026
  IL_0003:  ldarg.0
  IL_0004:  ldc.i4.2
  IL_0005:  conv.i
  IL_0006:  beq.s      IL_0038
  IL_0008:  ldarg.0
  IL_0009:  ldc.i4.6
  IL_000a:  conv.i
  IL_000b:  beq.s      IL_004a
  IL_000d:  ldarg.0
  IL_000e:  ldc.i4     0x270f
  IL_0013:  conv.i
  IL_0014:  beq.s      IL_005c
  IL_0016:  ldarg.0
  IL_0017:  ldc.i4     0xff
  IL_001c:  conv.i
  IL_001d:  beq.s      IL_006e
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4.m1
  IL_0021:  conv.i
  IL_0022:  beq.s      IL_0095
  IL_0024:  br.s       IL_0080
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4.1
  IL_0028:  sub
  IL_0029:  starg.s    V_0
  IL_002b:  ldstr      ""case 0: ""
  IL_0030:  ldarg.0
  IL_0031:  call       ""void Program.Report(string, nint)""
  IL_0036:  br.s       IL_005c
  IL_0038:  ldarg.0
  IL_0039:  ldc.i4.1
  IL_003a:  sub
  IL_003b:  starg.s    V_0
  IL_003d:  ldstr      ""case 2: ""
  IL_0042:  ldarg.0
  IL_0043:  call       ""void Program.Report(string, nint)""
  IL_0048:  br.s       IL_006e
  IL_004a:  ldarg.0
  IL_004b:  ldc.i4.1
  IL_004c:  sub
  IL_004d:  starg.s    V_0
  IL_004f:  ldstr      ""case 6: ""
  IL_0054:  ldarg.0
  IL_0055:  call       ""void Program.Report(string, nint)""
  IL_005a:  br.s       IL_0038
  IL_005c:  ldarg.0
  IL_005d:  ldc.i4.1
  IL_005e:  sub
  IL_005f:  starg.s    V_0
  IL_0061:  ldstr      ""case 9999: ""
  IL_0066:  ldarg.0
  IL_0067:  call       ""void Program.Report(string, nint)""
  IL_006c:  br.s       IL_0080
  IL_006e:  ldarg.0
  IL_006f:  ldc.i4.1
  IL_0070:  sub
  IL_0071:  starg.s    V_0
  IL_0073:  ldstr      ""case 0xff: ""
  IL_0078:  ldarg.0
  IL_0079:  call       ""void Program.Report(string, nint)""
  IL_007e:  br.s       IL_0026
  IL_0080:  ldarg.0
  IL_0081:  ldc.i4.1
  IL_0082:  sub
  IL_0083:  starg.s    V_0
  IL_0085:  ldstr      ""default: ""
  IL_008a:  ldarg.0
  IL_008b:  call       ""void Program.Report(string, nint)""
  IL_0090:  ldarg.0
  IL_0091:  ldc.i4.0
  IL_0092:  conv.i
  IL_0093:  ble.s      IL_00a8
  IL_0095:  ldc.i4     0x3e7
  IL_009a:  conv.i
  IL_009b:  starg.s    V_0
  IL_009d:  ldstr      ""case -1: ""
  IL_00a2:  ldarg.0
  IL_00a3:  call       ""void Program.Report(string, nint)""
  IL_00a8:  ldarg.0
  IL_00a9:  ret
}");
        }

        [Fact]
        public void SwitchStatement_02()
        {
            var source =
@"using System;
class Program
{
   static nuint M(nuint ret)
    {
        switch (ret) {
        case 0:
            ret--; // 2
            Report(""case 0: "", ret);
            goto case 9999;
        case 2:
            ret--; // 4
            Report(""case 2: "", ret);
            goto case 255;
        case 6: // start here
            ret--; // 5
            Report(""case 6: "", ret);
            goto case 2;
        case 9999:
            ret--; // 1
            Report(""case 9999: "", ret);
            goto default;
        case 0xff:
            ret--; // 3
            Report(""case 0xff: "", ret);
            goto case 0;
        default:
            ret--;
            Report(""default: "", ret);
            if (ret > 0) {
                goto case int.MaxValue;
            }
            break;
        case int.MaxValue:
            ret = 999;
            Report(""case int.MaxValue: "", ret);
            break;
        }
        return(ret);
    }
    static void Report(string prefix, nuint value)
    {
        Console.WriteLine(prefix + value);
    }
    static void Main()
    {
        Console.WriteLine(M(6));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput:
@"case 6: 5
case 2: 4
case 0xff: 3
case 0: 2
case 9999: 1
default: 0
0");
            verifier.VerifyIL("Program.M",
@"{
  // Code size      174 (0xae)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_002a
  IL_0003:  ldarg.0
  IL_0004:  ldc.i4.2
  IL_0005:  conv.i
  IL_0006:  beq.s      IL_003c
  IL_0008:  ldarg.0
  IL_0009:  ldc.i4.6
  IL_000a:  conv.i
  IL_000b:  beq.s      IL_004e
  IL_000d:  ldarg.0
  IL_000e:  ldc.i4     0x270f
  IL_0013:  conv.i
  IL_0014:  beq.s      IL_0060
  IL_0016:  ldarg.0
  IL_0017:  ldc.i4     0xff
  IL_001c:  conv.i
  IL_001d:  beq.s      IL_0072
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x7fffffff
  IL_0025:  conv.i
  IL_0026:  beq.s      IL_0099
  IL_0028:  br.s       IL_0084
  IL_002a:  ldarg.0
  IL_002b:  ldc.i4.1
  IL_002c:  sub
  IL_002d:  starg.s    V_0
  IL_002f:  ldstr      ""case 0: ""
  IL_0034:  ldarg.0
  IL_0035:  call       ""void Program.Report(string, nuint)""
  IL_003a:  br.s       IL_0060
  IL_003c:  ldarg.0
  IL_003d:  ldc.i4.1
  IL_003e:  sub
  IL_003f:  starg.s    V_0
  IL_0041:  ldstr      ""case 2: ""
  IL_0046:  ldarg.0
  IL_0047:  call       ""void Program.Report(string, nuint)""
  IL_004c:  br.s       IL_0072
  IL_004e:  ldarg.0
  IL_004f:  ldc.i4.1
  IL_0050:  sub
  IL_0051:  starg.s    V_0
  IL_0053:  ldstr      ""case 6: ""
  IL_0058:  ldarg.0
  IL_0059:  call       ""void Program.Report(string, nuint)""
  IL_005e:  br.s       IL_003c
  IL_0060:  ldarg.0
  IL_0061:  ldc.i4.1
  IL_0062:  sub
  IL_0063:  starg.s    V_0
  IL_0065:  ldstr      ""case 9999: ""
  IL_006a:  ldarg.0
  IL_006b:  call       ""void Program.Report(string, nuint)""
  IL_0070:  br.s       IL_0084
  IL_0072:  ldarg.0
  IL_0073:  ldc.i4.1
  IL_0074:  sub
  IL_0075:  starg.s    V_0
  IL_0077:  ldstr      ""case 0xff: ""
  IL_007c:  ldarg.0
  IL_007d:  call       ""void Program.Report(string, nuint)""
  IL_0082:  br.s       IL_002a
  IL_0084:  ldarg.0
  IL_0085:  ldc.i4.1
  IL_0086:  sub
  IL_0087:  starg.s    V_0
  IL_0089:  ldstr      ""default: ""
  IL_008e:  ldarg.0
  IL_008f:  call       ""void Program.Report(string, nuint)""
  IL_0094:  ldarg.0
  IL_0095:  ldc.i4.0
  IL_0096:  conv.i
  IL_0097:  ble.un.s   IL_00ac
  IL_0099:  ldc.i4     0x3e7
  IL_009e:  conv.i
  IL_009f:  starg.s    V_0
  IL_00a1:  ldstr      ""case int.MaxValue: ""
  IL_00a6:  ldarg.0
  IL_00a7:  call       ""void Program.Report(string, nuint)""
  IL_00ac:  ldarg.0
  IL_00ad:  ret
}");
        }

        [Fact]
        public void Conversions()
        {
            const string convNone =
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}";
            static string conv(string conversion) =>
$@"{{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  {conversion}
  IL_0002:  ret
}}";
            static string convFromNullableT(string conversion, string sourceType) =>
$@"{{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""{sourceType} {sourceType}?.Value.get""
  IL_0007:  {conversion}
  IL_0008:  ret
}}";
            static string convToNullableT(string conversion, string destType) =>
$@"{{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  {conversion}
  IL_0002:  newobj     ""{destType}?..ctor({destType})""
  IL_0007:  ret
}}";
            static string convFromToNullableT(string conversion, string sourceType, string destType) =>
$@"{{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init ({sourceType}? V_0,
                {destType}? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool {sourceType}?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""{destType}?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""{sourceType} {sourceType}?.GetValueOrDefault()""
  IL_001c:  {conversion}
  IL_001d:  newobj     ""{destType}?..ctor({destType})""
  IL_0022:  ret
}}";
            static string convAndExplicit(string method, string conv = null) => conv is null ?
$@"{{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""{method}""
  IL_0006:  ret
}}" :
$@"{{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""{method}""
  IL_0006:  {conv}
  IL_0007:  ret
}}";
            static string convAndExplicitFromNullableT(string sourceType, string method, string conv = null) => conv is null ?
$@"{{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""{sourceType} {sourceType}?.Value.get""
  IL_0007:  call       ""{method}""
  IL_000c:  ret
}}" :
$@"{{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""{sourceType} {sourceType}?.Value.get""
  IL_0007:  call       ""{method}""
  IL_000c:  {conv}
  IL_000d:  ret
}}";
            static string convAndExplicitToNullableT(string destType, string method, string conv = null) => conv is null ?
$@"{{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""{method}""
  IL_0006:  newobj     ""{destType}?..ctor({destType})""
  IL_000b:  ret
}}" :
$@"{{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""{method}""
  IL_0006:  {conv}
  IL_0007:  newobj     ""{destType}?..ctor({destType})""
  IL_000c:  ret
}}";
            // https://github.com/dotnet/roslyn/issues/42834: Invalid code generated for nullable conversions
            // involving System.[U]IntPtr: the conversion is dropped.
            static string convAndExplicitFromToNullableT(string sourceType, string destType, string method, string conv = null) =>
$@"{{
  // Code size       39 (0x27)
  .maxstack  1
  .locals init ({sourceType}? V_0,
                {destType}? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool {sourceType}?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""{destType}?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""{sourceType} {sourceType}?.GetValueOrDefault()""
  IL_001c:  call       ""{method}""
  IL_0021:  newobj     ""{destType}?..ctor({destType})""
  IL_0026:  ret
}}";
            static string explicitAndConv(string method, string conv = null) => conv is null ?
$@"{{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""{method}""
  IL_0006:  ret
}}" :
$@"{{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  {conv}
  IL_0002:  call       ""{method}""
  IL_0007:  ret
}}";
            static string explicitAndConvFromNullableT(string sourceType, string method, string conv = null) => conv is null ?
$@"{{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""{sourceType} {sourceType}?.Value.get""
  IL_0007:  call       ""{method}""
  IL_000c:  ret
}}" :
$@"{{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""{sourceType} {sourceType}?.Value.get""
  IL_0007:  {conv}
  IL_0008:  call       ""{method}""
  IL_000d:  ret
}}";
            static string explicitAndConvToNullableT(string destType, string method, string conv = null) => conv is null ?
$@"{{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""{method}""
  IL_0006:  newobj     ""{destType}?..ctor({destType})""
  IL_000b:  ret
}}" :
$@"{{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  {conv}
  IL_0002:  call       ""{method}""
  IL_0007:  newobj     ""{destType}?..ctor({destType})""
  IL_000c:  ret
}}";
            // https://github.com/dotnet/roslyn/issues/42834: Invalid code generated for nullable conversions
            // involving System.[U]IntPtr: the conversion is dropped.
            static string explicitAndConvFromToNullableT(string sourceType, string destType, string method, string conv = null) =>
$@"{{
  // Code size       39 (0x27)
  .maxstack  1
  .locals init ({sourceType}? V_0,
                {destType}? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool {sourceType}?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""{destType}?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""{sourceType} {sourceType}?.GetValueOrDefault()""
  IL_001c:  call       ""{method}""
  IL_0021:  newobj     ""{destType}?..ctor({destType})""
  IL_0026:  ret
}}";
            void conversions(string sourceType, string destType, string expectedImplicitIL, string expectedExplicitIL, string expectedCheckedIL = null)
            {
                // https://github.com/dotnet/roslyn/issues/42834: Invalid code generated for nullable conversions
                // involving System.[U]IntPtr: the conversion is dropped. And when converting from System.[U]IntPtr,
                // an assert in LocalRewriter.MakeLiftedUserDefinedConversionConsequence fails.
                bool verify = !(sourceType.EndsWith("?") &&
                    destType.EndsWith("?") &&
                    (usesIntPtrOrUIntPtr(sourceType) || usesIntPtrOrUIntPtr(destType)));
#if DEBUG
                if (!verify) return;
#endif
                convert(
                    sourceType,
                    destType,
                    expectedImplicitIL,
                    // https://github.com/dotnet/roslyn/issues/42454: TypeInfo.ConvertedType does not include identity conversion between underlying type and native int.
                    skipTypeChecks: usesIntPtrOrUIntPtr(sourceType) || usesIntPtrOrUIntPtr(destType),
                    useExplicitCast: false,
                    useChecked: false,
                    verify: verify,
                    expectedImplicitIL is null ?
                        expectedExplicitIL is null ? ErrorCode.ERR_NoImplicitConv : ErrorCode.ERR_NoImplicitConvCast :
                        0);
                convert(
                    sourceType,
                    destType,
                    expectedExplicitIL,
                    skipTypeChecks: true,
                    useExplicitCast: true,
                    useChecked: false,
                    verify: verify,
                    expectedExplicitIL is null ? ErrorCode.ERR_NoExplicitConv : 0);
                expectedCheckedIL ??= expectedExplicitIL;
                convert(
                    sourceType,
                    destType,
                    expectedCheckedIL,
                    skipTypeChecks: true,
                    useExplicitCast: true,
                    useChecked: true,
                    verify: verify,
                    expectedCheckedIL is null ? ErrorCode.ERR_NoExplicitConv : 0);

                static bool usesIntPtrOrUIntPtr(string underlyingType) => underlyingType.Contains("IntPtr");
            }

            conversions(sourceType: "object", destType: "nint", expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""System.IntPtr""
  IL_0006:  ret
}");
            conversions(sourceType: "string", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "void*", destType: "nint", expectedImplicitIL: null,
// https://github.com/dotnet/roslyn/issues/42457: Investigate whether this conversion (and other
// conversions to/from void*) can use conv.i or conv.u instead of explicit operators on System.[U]IntPtr.
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.IntPtr System.IntPtr.op_Explicit(void*)""
  IL_0006:  ret
}");
            conversions(sourceType: "bool", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char", destType: "nint", expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            conversions(sourceType: "sbyte", destType: "nint", expectedImplicitIL: conv("conv.i"), expectedExplicitIL: conv("conv.i"));
            conversions(sourceType: "byte", destType: "nint", expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            conversions(sourceType: "short", destType: "nint", expectedImplicitIL: conv("conv.i"), expectedExplicitIL: conv("conv.i"));
            conversions(sourceType: "ushort", destType: "nint", expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            conversions(sourceType: "int", destType: "nint", expectedImplicitIL: conv("conv.i"), expectedExplicitIL: conv("conv.i"));
            conversions(sourceType: "uint", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u"), expectedCheckedIL: conv("conv.ovf.i.un"));
            conversions(sourceType: "long", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.i"));
            conversions(sourceType: "ulong", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.i.un"));
            conversions(sourceType: "nint", destType: "nint", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "nuint", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.i.un"));
            conversions(sourceType: "float", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.i"));
            conversions(sourceType: "double", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.i"));
            conversions(sourceType: "decimal", destType: "nint", expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""long decimal.op_Explicit(decimal)""
  IL_0006:  conv.i
  IL_0007:  ret
}",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""long decimal.op_Explicit(decimal)""
  IL_0006:  conv.ovf.i
  IL_0007:  ret
}");
            conversions(sourceType: "System.IntPtr", destType: "nint", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "System.UIntPtr", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "bool?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "char"));
            conversions(sourceType: "sbyte?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "sbyte"));
            conversions(sourceType: "byte?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "byte"));
            conversions(sourceType: "short?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "short"));
            conversions(sourceType: "ushort?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "ushort"));
            conversions(sourceType: "int?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "int"));
            conversions(sourceType: "uint?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "uint"), expectedCheckedIL: convFromNullableT("conv.ovf.i.un", "uint"));
            conversions(sourceType: "long?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "long"), expectedCheckedIL: convFromNullableT("conv.ovf.i", "long"));
            conversions(sourceType: "ulong?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "ulong"), expectedCheckedIL: convFromNullableT("conv.ovf.i.un", "ulong"));
            conversions(sourceType: "nint?", destType: "nint", expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nint nint?.Value.get""
  IL_0007:  ret
}");
            conversions(sourceType: "nuint?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.i.un", "nuint"));
            conversions(sourceType: "float?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "float"), expectedCheckedIL: convFromNullableT("conv.ovf.i", "float"));
            conversions(sourceType: "double?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "double"), expectedCheckedIL: convFromNullableT("conv.ovf.i", "double"));
            conversions(sourceType: "decimal?", destType: "nint", expectedImplicitIL: null,
@"{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""decimal decimal?.Value.get""
  IL_0007:  call       ""long decimal.op_Explicit(decimal)""
  IL_000c:  conv.i
  IL_000d:  ret
}",
@"{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""decimal decimal?.Value.get""
  IL_0007:  call       ""long decimal.op_Explicit(decimal)""
  IL_000c:  conv.ovf.i
  IL_000d:  ret
}");
            conversions(sourceType: "System.IntPtr?", destType: "nint", expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""System.IntPtr System.IntPtr?.Value.get""
  IL_0007:  ret
}");
            conversions(sourceType: "System.UIntPtr?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "object", destType: "nint?", expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""nint?""
  IL_0006:  ret
}");
            conversions(sourceType: "string", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "void*", destType: "nint?", expectedImplicitIL: null,
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.IntPtr System.IntPtr.op_Explicit(void*)""
  IL_0006:  newobj     ""nint?..ctor(nint)""
  IL_000b:  ret
}");
            conversions(sourceType: "bool", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char", destType: "nint?", expectedImplicitIL: convToNullableT("conv.u", "nint"), expectedExplicitIL: convToNullableT("conv.u", "nint"));
            conversions(sourceType: "sbyte", destType: "nint?", expectedImplicitIL: convToNullableT("conv.i", "nint"), expectedExplicitIL: convToNullableT("conv.i", "nint"));
            conversions(sourceType: "byte", destType: "nint?", expectedImplicitIL: convToNullableT("conv.u", "nint"), expectedExplicitIL: convToNullableT("conv.u", "nint"));
            conversions(sourceType: "short", destType: "nint?", expectedImplicitIL: convToNullableT("conv.i", "nint"), expectedExplicitIL: convToNullableT("conv.i", "nint"));
            conversions(sourceType: "ushort", destType: "nint?", expectedImplicitIL: convToNullableT("conv.u", "nint"), expectedExplicitIL: convToNullableT("conv.u", "nint"));
            conversions(sourceType: "int", destType: "nint?", expectedImplicitIL: convToNullableT("conv.i", "nint"), expectedExplicitIL: convToNullableT("conv.i", "nint"));
            conversions(sourceType: "uint", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u", "nint"), expectedCheckedIL: convToNullableT("conv.ovf.i.un", "nint"));
            conversions(sourceType: "long", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nint"), expectedCheckedIL: convToNullableT("conv.ovf.i", "nint"));
            conversions(sourceType: "ulong", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nint"), expectedCheckedIL: convToNullableT("conv.ovf.i.un", "nint"));
            conversions(sourceType: "nint", destType: "nint?",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nint?..ctor(nint)""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nint?..ctor(nint)""
  IL_0006:  ret
}");
            conversions(sourceType: "nuint", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nint"), expectedCheckedIL: convToNullableT("conv.ovf.i.un", "nint"));
            conversions(sourceType: "float", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nint"), expectedCheckedIL: convToNullableT("conv.ovf.i", "nint"));
            conversions(sourceType: "double", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nint"), expectedCheckedIL: convToNullableT("conv.ovf.i", "nint"));
            conversions(sourceType: "decimal", destType: "nint?", expectedImplicitIL: null,
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""long decimal.op_Explicit(decimal)""
  IL_0006:  conv.i
  IL_0007:  newobj     ""nint?..ctor(nint)""
  IL_000c:  ret
}",
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""long decimal.op_Explicit(decimal)""
  IL_0006:  conv.ovf.i
  IL_0007:  newobj     ""nint?..ctor(nint)""
  IL_000c:  ret
}");
            conversions(sourceType: "System.IntPtr", destType: "nint?",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nint?..ctor(nint)""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nint?..ctor(nint)""
  IL_0006:  ret
}");
            conversions(sourceType: "System.UIntPtr", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "bool?", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char?", destType: "nint?", expectedImplicitIL: convFromToNullableT("conv.u", "char", "nint"), expectedExplicitIL: convFromToNullableT("conv.u", "char", "nint"));
            conversions(sourceType: "sbyte?", destType: "nint?", expectedImplicitIL: convFromToNullableT("conv.i", "sbyte", "nint"), expectedExplicitIL: convFromToNullableT("conv.i", "sbyte", "nint"));
            conversions(sourceType: "byte?", destType: "nint?", expectedImplicitIL: convFromToNullableT("conv.u", "byte", "nint"), expectedExplicitIL: convFromToNullableT("conv.u", "byte", "nint"));
            conversions(sourceType: "short?", destType: "nint?", expectedImplicitIL: convFromToNullableT("conv.i", "short", "nint"), expectedExplicitIL: convFromToNullableT("conv.i", "short", "nint"));
            conversions(sourceType: "ushort?", destType: "nint?", expectedImplicitIL: convFromToNullableT("conv.u", "ushort", "nint"), expectedExplicitIL: convFromToNullableT("conv.u", "ushort", "nint"));
            conversions(sourceType: "int?", destType: "nint?", expectedImplicitIL: convFromToNullableT("conv.i", "int", "nint"), expectedExplicitIL: convFromToNullableT("conv.i", "int", "nint"));
            conversions(sourceType: "uint?", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u", "uint", "nint"), expectedCheckedIL: convFromToNullableT("conv.ovf.i.un", "uint", "nint"));
            conversions(sourceType: "long?", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "long", "nint"), expectedCheckedIL: convFromToNullableT("conv.ovf.i", "long", "nint"));
            conversions(sourceType: "ulong?", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "ulong", "nint"), expectedCheckedIL: convFromToNullableT("conv.ovf.i.un", "ulong", "nint"));
            conversions(sourceType: "nint?", destType: "nint?", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "nuint?", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "nuint", "nint"), expectedCheckedIL: convFromToNullableT("conv.ovf.i.un", "nuint", "nint"));
            conversions(sourceType: "float?", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "float", "nint"), expectedCheckedIL: convFromToNullableT("conv.ovf.i", "float", "nint"));
            conversions(sourceType: "double?", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "double", "nint"), expectedCheckedIL: convFromToNullableT("conv.ovf.i", "double", "nint"));
            conversions(sourceType: "decimal?", destType: "nint?", null,
@"{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (decimal? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool decimal?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""decimal decimal?.GetValueOrDefault()""
  IL_001c:  call       ""long decimal.op_Explicit(decimal)""
  IL_0021:  conv.i
  IL_0022:  newobj     ""nint?..ctor(nint)""
  IL_0027:  ret
}",
@"{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (decimal? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool decimal?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""decimal decimal?.GetValueOrDefault()""
  IL_001c:  call       ""long decimal.op_Explicit(decimal)""
  IL_0021:  conv.ovf.i
  IL_0022:  newobj     ""nint?..ctor(nint)""
  IL_0027:  ret
}");
            conversions(sourceType: "System.IntPtr?", destType: "nint?", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "System.UIntPtr?", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nint", destType: "object",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""System.IntPtr""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""System.IntPtr""
  IL_0006:  ret
}");
            conversions(sourceType: "nint", destType: "string", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nint", destType: "void*", expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""void* System.IntPtr.op_Explicit(System.IntPtr)""
  IL_0006:  ret
}");
            conversions(sourceType: "nint", destType: "bool", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nint", destType: "char", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u2"), expectedCheckedIL: conv("conv.ovf.u2"));
            conversions(sourceType: "nint", destType: "sbyte", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i1"), expectedCheckedIL: conv("conv.ovf.i1"));
            conversions(sourceType: "nint", destType: "byte", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u1"), expectedCheckedIL: conv("conv.ovf.u1"));
            conversions(sourceType: "nint", destType: "short", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i2"), expectedCheckedIL: conv("conv.ovf.i2"));
            conversions(sourceType: "nint", destType: "ushort", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u2"), expectedCheckedIL: conv("conv.ovf.u2"));
            conversions(sourceType: "nint", destType: "int", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i4"), expectedCheckedIL: conv("conv.ovf.i4"));
            conversions(sourceType: "nint", destType: "uint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u4"), expectedCheckedIL: conv("conv.ovf.u4"));
            conversions(sourceType: "nint", destType: "long", expectedImplicitIL: conv("conv.i8"), expectedExplicitIL: conv("conv.i8"));
            // https://github.com/dotnet/roslyn/issues/42457: Investigate why this conversion (and other conversions from nint to ulong and from nuint to long)
            // use differently signed opcodes for unchecked and checked conversions. (Why conv.i8 but conv.ovf.u8 here for instance?)
            conversions(sourceType: "nint", destType: "ulong", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i8"), expectedCheckedIL: conv("conv.ovf.u8"));
            conversions(sourceType: "nint", destType: "float", expectedImplicitIL: conv("conv.r4"), expectedExplicitIL: conv("conv.r4"));
            conversions(sourceType: "nint", destType: "double", expectedImplicitIL: conv("conv.r8"), expectedExplicitIL: conv("conv.r8"));
            conversions(sourceType: "nint", destType: "decimal",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i8
  IL_0002:  call       ""decimal decimal.op_Implicit(long)""
  IL_0007:  ret
}",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i8
  IL_0002:  call       ""decimal decimal.op_Implicit(long)""
  IL_0007:  ret
}");
            conversions(sourceType: "nint", destType: "System.IntPtr", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "nint", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: null); // https://github.com/dotnet/roslyn/issues/42560: Allow explicitly casting nint to UIntPtr.
            conversions(sourceType: "nint", destType: "bool?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nint", destType: "char?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u2", "char"), expectedCheckedIL: convToNullableT("conv.ovf.u2", "char"));
            conversions(sourceType: "nint", destType: "sbyte?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i1", "sbyte"), expectedCheckedIL: convToNullableT("conv.ovf.i1", "sbyte"));
            conversions(sourceType: "nint", destType: "byte?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u1", "byte"), expectedCheckedIL: convToNullableT("conv.ovf.u1", "byte"));
            conversions(sourceType: "nint", destType: "short?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i2", "short"), expectedCheckedIL: convToNullableT("conv.ovf.i2", "short"));
            conversions(sourceType: "nint", destType: "ushort?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u2", "ushort"), expectedCheckedIL: convToNullableT("conv.ovf.u2", "ushort"));
            conversions(sourceType: "nint", destType: "int?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i4", "int"), expectedCheckedIL: convToNullableT("conv.ovf.i4", "int"));
            conversions(sourceType: "nint", destType: "uint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u4", "uint"), expectedCheckedIL: convToNullableT("conv.ovf.u4", "uint"));
            conversions(sourceType: "nint", destType: "long?", expectedImplicitIL: convToNullableT("conv.i8", "long"), expectedExplicitIL: convToNullableT("conv.i8", "long"));
            conversions(sourceType: "nint", destType: "ulong?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i8", "ulong"), expectedCheckedIL: convToNullableT("conv.ovf.u8", "ulong"));
            conversions(sourceType: "nint", destType: "float?", expectedImplicitIL: convToNullableT("conv.r4", "float"), expectedExplicitIL: convToNullableT("conv.r4", "float"), null);
            conversions(sourceType: "nint", destType: "double?", expectedImplicitIL: convToNullableT("conv.r8", "double"), expectedExplicitIL: convToNullableT("conv.r8", "double"), null);
            conversions(sourceType: "nint", destType: "decimal?",
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i8
  IL_0002:  call       ""decimal decimal.op_Implicit(long)""
  IL_0007:  newobj     ""decimal?..ctor(decimal)""
  IL_000c:  ret
}",
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i8
  IL_0002:  call       ""decimal decimal.op_Implicit(long)""
  IL_0007:  newobj     ""decimal?..ctor(decimal)""
  IL_000c:  ret
}");
            conversions(sourceType: "nint", destType: "System.IntPtr?",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""System.IntPtr?..ctor(System.IntPtr)""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""System.IntPtr?..ctor(System.IntPtr)""
  IL_0006:  ret
}");
            conversions(sourceType: "nint", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: null); // https://github.com/dotnet/roslyn/issues/42560: Allow explicitly casting nint to UIntPtr.
            conversions(sourceType: "nint?", destType: "object",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""nint?""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""nint?""
  IL_0006:  ret
}");
            conversions(sourceType: "nint?", destType: "string", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nint?", destType: "void*", expectedImplicitIL: null,
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nint nint?.Value.get""
  IL_0007:  call       ""void* System.IntPtr.op_Explicit(System.IntPtr)""
  IL_000c:  ret
}");
            conversions(sourceType: "nint?", destType: "bool", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nint?", destType: "char", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u2", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.u2", "nint"));
            conversions(sourceType: "nint?", destType: "sbyte", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i1", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.i1", "nint"));
            conversions(sourceType: "nint?", destType: "byte", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u1", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.u1", "nint"));
            conversions(sourceType: "nint?", destType: "short", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i2", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.i2", "nint"));
            conversions(sourceType: "nint?", destType: "ushort", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u2", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.u2", "nint"));
            conversions(sourceType: "nint?", destType: "int", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i4", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.i4", "nint"));
            conversions(sourceType: "nint?", destType: "uint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u4", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.u4", "nint"));
            conversions(sourceType: "nint?", destType: "long", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i8", "nint"));
            conversions(sourceType: "nint?", destType: "ulong", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i8", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.u8", "nint"));
            conversions(sourceType: "nint?", destType: "float", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.r4", "nint"));
            conversions(sourceType: "nint?", destType: "double", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.r8", "nint"));
            conversions(sourceType: "nint?", destType: "decimal", expectedImplicitIL: null,
@"{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nint nint?.Value.get""
  IL_0007:  conv.i8
  IL_0008:  call       ""decimal decimal.op_Implicit(long)""
  IL_000d:  ret
}");
            conversions(sourceType: "nint?", destType: "System.IntPtr", expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nint nint?.Value.get""
  IL_0007:  ret
}");
            conversions(sourceType: "nint?", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: null); // https://github.com/dotnet/roslyn/issues/42560: Allow explicitly casting nint to UIntPtr.
            conversions(sourceType: "nint?", destType: "bool?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nint?", destType: "char?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u2", "nint", "char"), expectedCheckedIL: convFromToNullableT("conv.ovf.u2", "nint", "char"));
            conversions(sourceType: "nint?", destType: "sbyte?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i1", "nint", "sbyte"), expectedCheckedIL: convFromToNullableT("conv.ovf.i1", "nint", "sbyte"));
            conversions(sourceType: "nint?", destType: "byte?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u1", "nint", "byte"), expectedCheckedIL: convFromToNullableT("conv.ovf.u1", "nint", "byte"));
            conversions(sourceType: "nint?", destType: "short?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i2", "nint", "short"), expectedCheckedIL: convFromToNullableT("conv.ovf.i2", "nint", "short"));
            conversions(sourceType: "nint?", destType: "ushort?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u2", "nint", "ushort"), expectedCheckedIL: convFromToNullableT("conv.ovf.u2", "nint", "ushort"));
            conversions(sourceType: "nint?", destType: "int?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i4", "nint", "int"), expectedCheckedIL: convFromToNullableT("conv.ovf.i4", "nint", "int"));
            conversions(sourceType: "nint?", destType: "uint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u4", "nint", "uint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u4", "nint", "uint"));
            conversions(sourceType: "nint?", destType: "long?", expectedImplicitIL: convFromToNullableT("conv.i8", "nint", "long"), expectedExplicitIL: convFromToNullableT("conv.i8", "nint", "long"));
            conversions(sourceType: "nint?", destType: "ulong?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i8", "nint", "ulong"), expectedCheckedIL: convFromToNullableT("conv.ovf.u8", "nint", "ulong"));
            conversions(sourceType: "nint?", destType: "float?", expectedImplicitIL: convFromToNullableT("conv.r4", "nint", "float"), expectedExplicitIL: convFromToNullableT("conv.r4", "nint", "float"), null);
            conversions(sourceType: "nint?", destType: "double?", expectedImplicitIL: convFromToNullableT("conv.r8", "nint", "double"), expectedExplicitIL: convFromToNullableT("conv.r8", "nint", "double"), null);
            conversions(sourceType: "nint?", destType: "decimal?",
@"{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (nint? V_0,
                decimal? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""decimal?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""nint nint?.GetValueOrDefault()""
  IL_001c:  conv.i8
  IL_001d:  call       ""decimal decimal.op_Implicit(long)""
  IL_0022:  newobj     ""decimal?..ctor(decimal)""
  IL_0027:  ret
}",
@"{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (nint? V_0,
                decimal? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""decimal?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""nint nint?.GetValueOrDefault()""
  IL_001c:  conv.i8
  IL_001d:  call       ""decimal decimal.op_Implicit(long)""
  IL_0022:  newobj     ""decimal?..ctor(decimal)""
  IL_0027:  ret
}");
            conversions(sourceType: "nint?", destType: "System.IntPtr?", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "nint?", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: null); // https://github.com/dotnet/roslyn/issues/42560: Allow explicitly casting nint to UIntPtr.
            conversions(sourceType: "object", destType: "nuint", expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""System.UIntPtr""
  IL_0006:  ret
}");
            conversions(sourceType: "string", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "void*", destType: "nuint", expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.UIntPtr System.UIntPtr.op_Explicit(void*)""
  IL_0006:  ret
}");
            conversions(sourceType: "bool", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char", destType: "nuint", expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            conversions(sourceType: "sbyte", destType: "nuint", expectedImplicitIL: conv("conv.i"), expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "byte", destType: "nuint", expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            conversions(sourceType: "short", destType: "nuint", expectedImplicitIL: conv("conv.i"), expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "ushort", destType: "nuint", expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            conversions(sourceType: "int", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "uint", destType: "nuint", expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            conversions(sourceType: "long", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u"), expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "ulong", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u"), expectedCheckedIL: conv("conv.ovf.u.un"));
            conversions(sourceType: "nint", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u"), expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "nuint", destType: "nuint", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "float", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u"), expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "double", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u"), expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "decimal", destType: "nuint", expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_0006:  conv.u
  IL_0007:  ret
}",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_0006:  conv.ovf.u.un
  IL_0007:  ret
}");
            conversions(sourceType: "System.IntPtr", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.UIntPtr", destType: "nuint", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "bool?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "char"));
            conversions(sourceType: "sbyte?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "sbyte"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "sbyte"));
            conversions(sourceType: "byte?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "byte"));
            conversions(sourceType: "short?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "short"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "short"));
            conversions(sourceType: "ushort?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "ushort"));
            conversions(sourceType: "int?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "int"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "int"));
            conversions(sourceType: "uint?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "uint"));
            conversions(sourceType: "long?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "long"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "long"));
            conversions(sourceType: "ulong?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "ulong"), expectedCheckedIL: convFromNullableT("conv.ovf.u.un", "ulong"));
            conversions(sourceType: "nint?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "nint"));
            conversions(sourceType: "nuint?", destType: "nuint", expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nuint nuint?.Value.get""
  IL_0007:  ret
}");
            conversions(sourceType: "float?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "float"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "float"));
            conversions(sourceType: "double?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "double"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "double"));
            conversions(sourceType: "decimal?", destType: "nuint", expectedImplicitIL: null,
@"{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""decimal decimal?.Value.get""
  IL_0007:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_000c:  conv.u
  IL_000d:  ret
}",
@"{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""decimal decimal?.Value.get""
  IL_0007:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_000c:  conv.ovf.u.un
  IL_000d:  ret
}");
            conversions(sourceType: "System.IntPtr?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.UIntPtr?", destType: "nuint", expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""System.UIntPtr System.UIntPtr?.Value.get""
  IL_0007:  ret
}");
            conversions(sourceType: "object", destType: "nuint?", expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""nuint?""
  IL_0006:  ret
}");
            conversions(sourceType: "string", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "void*", destType: "nuint?", expectedImplicitIL: null,
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.UIntPtr System.UIntPtr.op_Explicit(void*)""
  IL_0006:  newobj     ""nuint?..ctor(nuint)""
  IL_000b:  ret
}");
            conversions(sourceType: "bool", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char", destType: "nuint?", expectedImplicitIL: convToNullableT("conv.u", "nuint"), expectedExplicitIL: convToNullableT("conv.u", "nuint"));
            conversions(sourceType: "sbyte", destType: "nuint?", expectedImplicitIL: convToNullableT("conv.i", "nuint"), expectedExplicitIL: convToNullableT("conv.i", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "byte", destType: "nuint?", expectedImplicitIL: convToNullableT("conv.u", "nuint"), expectedExplicitIL: convToNullableT("conv.u", "nuint"));
            conversions(sourceType: "short", destType: "nuint?", expectedImplicitIL: convToNullableT("conv.i", "nuint"), expectedExplicitIL: convToNullableT("conv.i", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "ushort", destType: "nuint?", expectedImplicitIL: convToNullableT("conv.u", "nuint"), expectedExplicitIL: convToNullableT("conv.u", "nuint"));
            conversions(sourceType: "int", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "uint", destType: "nuint?", expectedImplicitIL: convToNullableT("conv.u", "nuint"), expectedExplicitIL: convToNullableT("conv.u", "nuint"));
            conversions(sourceType: "long", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "ulong", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u.un", "nuint"));
            conversions(sourceType: "nint", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "nuint", destType: "nuint?",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nuint?..ctor(nuint)""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nuint?..ctor(nuint)""
  IL_0006:  ret
}");
            conversions(sourceType: "float", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "double", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "decimal", destType: "nuint?", expectedImplicitIL: null,
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_0006:  conv.u
  IL_0007:  newobj     ""nuint?..ctor(nuint)""
  IL_000c:  ret
}",
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_0006:  conv.ovf.u.un
  IL_0007:  newobj     ""nuint?..ctor(nuint)""
  IL_000c:  ret
}");
            conversions(sourceType: "System.IntPtr", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.UIntPtr", destType: "nuint?",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nuint?..ctor(nuint)""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nuint?..ctor(nuint)""
  IL_0006:  ret
}");
            conversions(sourceType: "bool?", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char?", destType: "nuint?", expectedImplicitIL: convFromToNullableT("conv.u", "char", "nuint"), expectedExplicitIL: convFromToNullableT("conv.u", "char", "nuint"));
            conversions(sourceType: "sbyte?", destType: "nuint?", expectedImplicitIL: convFromToNullableT("conv.i", "sbyte", "nuint"), expectedExplicitIL: convFromToNullableT("conv.i", "sbyte", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "sbyte", "nuint"));
            conversions(sourceType: "byte?", destType: "nuint?", expectedImplicitIL: convFromToNullableT("conv.u", "byte", "nuint"), expectedExplicitIL: convFromToNullableT("conv.u", "byte", "nuint"));
            conversions(sourceType: "short?", destType: "nuint?", expectedImplicitIL: convFromToNullableT("conv.i", "short", "nuint"), expectedExplicitIL: convFromToNullableT("conv.i", "short", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "short", "nuint"));
            conversions(sourceType: "ushort?", destType: "nuint?", expectedImplicitIL: convFromToNullableT("conv.u", "ushort", "nuint"), expectedExplicitIL: convFromToNullableT("conv.u", "ushort", "nuint"));
            conversions(sourceType: "int?", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "int", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "int", "nuint"));
            conversions(sourceType: "uint?", destType: "nuint?", expectedImplicitIL: convFromToNullableT("conv.u", "uint", "nuint"), expectedExplicitIL: convFromToNullableT("conv.u", "uint", "nuint"));
            conversions(sourceType: "long?", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u", "long", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "long", "nuint"));
            conversions(sourceType: "ulong?", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u", "ulong", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u.un", "ulong", "nuint"));
            conversions(sourceType: "nint?", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u", "nint", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "nint", "nuint"));
            conversions(sourceType: "nuint?", destType: "nuint?", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "float?", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u", "float", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "float", "nuint"));
            conversions(sourceType: "double?", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u", "double", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "double", "nuint"));
            conversions(sourceType: "decimal?", destType: "nuint?", null,
@"{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (decimal? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool decimal?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""decimal decimal?.GetValueOrDefault()""
  IL_001c:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_0021:  conv.u
  IL_0022:  newobj     ""nuint?..ctor(nuint)""
  IL_0027:  ret
}",
@"{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (decimal? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool decimal?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""decimal decimal?.GetValueOrDefault()""
  IL_001c:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_0021:  conv.ovf.u.un
  IL_0022:  newobj     ""nuint?..ctor(nuint)""
  IL_0027:  ret
}");
            conversions(sourceType: "System.IntPtr?", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.UIntPtr?", destType: "nuint?", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "nuint", destType: "object",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""System.UIntPtr""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""System.UIntPtr""
  IL_0006:  ret
}");
            conversions(sourceType: "nuint", destType: "string", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nuint", destType: "void*", expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""void* System.UIntPtr.op_Explicit(System.UIntPtr)""
  IL_0006:  ret
}");
            conversions(sourceType: "nuint", destType: "bool", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nuint", destType: "char", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u2"), expectedCheckedIL: conv("conv.ovf.u2.un"));
            conversions(sourceType: "nuint", destType: "sbyte", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i1"), expectedCheckedIL: conv("conv.ovf.i1.un"));
            conversions(sourceType: "nuint", destType: "byte", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u1"), expectedCheckedIL: conv("conv.ovf.u1.un"));
            conversions(sourceType: "nuint", destType: "short", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i2"), expectedCheckedIL: conv("conv.ovf.i2.un"));
            conversions(sourceType: "nuint", destType: "ushort", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u2"), expectedCheckedIL: conv("conv.ovf.u2.un"));
            conversions(sourceType: "nuint", destType: "int", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i4"), expectedCheckedIL: conv("conv.ovf.i4.un"));
            conversions(sourceType: "nuint", destType: "uint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u4"), expectedCheckedIL: conv("conv.ovf.u4.un"));
            conversions(sourceType: "nuint", destType: "long", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u8"), expectedCheckedIL: conv("conv.ovf.i8.un"));
            conversions(sourceType: "nuint", destType: "ulong", expectedImplicitIL: conv("conv.u8"), expectedExplicitIL: conv("conv.u8"));
            conversions(sourceType: "nuint", destType: "float", expectedImplicitIL: conv("conv.r4"), expectedExplicitIL: conv("conv.r4"));
            conversions(sourceType: "nuint", destType: "double", expectedImplicitIL: conv("conv.r8"), expectedExplicitIL: conv("conv.r8"));
            conversions(sourceType: "nuint", destType: "decimal",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u8
  IL_0002:  call       ""decimal decimal.op_Implicit(ulong)""
  IL_0007:  ret
}",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u8
  IL_0002:  call       ""decimal decimal.op_Implicit(ulong)""
  IL_0007:  ret
}");
            conversions(sourceType: "nuint", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: null); // https://github.com/dotnet/roslyn/issues/42560: Allow explicitly casting nuint to IntPtr.
            conversions(sourceType: "nuint", destType: "System.UIntPtr", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "nuint", destType: "bool?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nuint", destType: "char?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u2", "char"), expectedCheckedIL: convToNullableT("conv.ovf.u2.un", "char"));
            conversions(sourceType: "nuint", destType: "sbyte?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i1", "sbyte"), expectedCheckedIL: convToNullableT("conv.ovf.i1.un", "sbyte"));
            conversions(sourceType: "nuint", destType: "byte?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u1", "byte"), expectedCheckedIL: convToNullableT("conv.ovf.u1.un", "byte"));
            conversions(sourceType: "nuint", destType: "short?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i2", "short"), expectedCheckedIL: convToNullableT("conv.ovf.i2.un", "short"));
            conversions(sourceType: "nuint", destType: "ushort?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u2", "ushort"), expectedCheckedIL: convToNullableT("conv.ovf.u2.un", "ushort"));
            conversions(sourceType: "nuint", destType: "int?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i4", "int"), expectedCheckedIL: convToNullableT("conv.ovf.i4.un", "int"));
            conversions(sourceType: "nuint", destType: "uint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u4", "uint"), expectedCheckedIL: convToNullableT("conv.ovf.u4.un", "uint"));
            conversions(sourceType: "nuint", destType: "long?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u8", "long"), expectedCheckedIL: convToNullableT("conv.ovf.i8.un", "long"));
            conversions(sourceType: "nuint", destType: "ulong?", expectedImplicitIL: convToNullableT("conv.u8", "ulong"), expectedExplicitIL: convToNullableT("conv.u8", "ulong"));
            conversions(sourceType: "nuint", destType: "float?", expectedImplicitIL: convToNullableT("conv.r4", "float"), expectedExplicitIL: convToNullableT("conv.r4", "float"), null);
            conversions(sourceType: "nuint", destType: "double?", expectedImplicitIL: convToNullableT("conv.r8", "double"), expectedExplicitIL: convToNullableT("conv.r8", "double"), null);
            conversions(sourceType: "nuint", destType: "decimal?",
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u8
  IL_0002:  call       ""decimal decimal.op_Implicit(ulong)""
  IL_0007:  newobj     ""decimal?..ctor(decimal)""
  IL_000c:  ret
}",
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u8
  IL_0002:  call       ""decimal decimal.op_Implicit(ulong)""
  IL_0007:  newobj     ""decimal?..ctor(decimal)""
  IL_000c:  ret
}");
            conversions(sourceType: "nuint", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: null); // https://github.com/dotnet/roslyn/issues/42560: Allow explicitly casting nuint to IntPtr.
            conversions(sourceType: "nuint", destType: "System.UIntPtr?",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""System.UIntPtr?..ctor(System.UIntPtr)""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""System.UIntPtr?..ctor(System.UIntPtr)""
  IL_0006:  ret
}");
            conversions(sourceType: "nuint?", destType: "object",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""nuint?""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""nuint?""
  IL_0006:  ret
}");
            conversions(sourceType: "nuint?", destType: "string", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nuint?", destType: "void*", expectedImplicitIL: null,
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nuint nuint?.Value.get""
  IL_0007:  call       ""void* System.UIntPtr.op_Explicit(System.UIntPtr)""
  IL_000c:  ret
}");
            conversions(sourceType: "nuint?", destType: "bool", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nuint?", destType: "char", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u2", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.u2.un", "nuint"));
            conversions(sourceType: "nuint?", destType: "sbyte", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i1", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.i1.un", "nuint"));
            conversions(sourceType: "nuint?", destType: "byte", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u1", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.u1.un", "nuint"));
            conversions(sourceType: "nuint?", destType: "short", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i2", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.i2.un", "nuint"));
            conversions(sourceType: "nuint?", destType: "ushort", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u2", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.u2.un", "nuint"));
            conversions(sourceType: "nuint?", destType: "int", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i4", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.i4.un", "nuint"));
            conversions(sourceType: "nuint?", destType: "uint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u4", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.u4.un", "nuint"));
            conversions(sourceType: "nuint?", destType: "long", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u8", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.i8.un", "nuint"));
            conversions(sourceType: "nuint?", destType: "ulong", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u8", "nuint"));
            conversions(sourceType: "nuint?", destType: "float", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.r4", "nuint"));
            conversions(sourceType: "nuint?", destType: "double", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.r8", "nuint"));
            conversions(sourceType: "nuint?", destType: "decimal", expectedImplicitIL: null,
@"{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nuint nuint?.Value.get""
  IL_0007:  conv.u8
  IL_0008:  call       ""decimal decimal.op_Implicit(ulong)""
  IL_000d:  ret
}");
            conversions(sourceType: "nuint?", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: null); // https://github.com/dotnet/roslyn/issues/42560: Allow explicitly casting nuint to IntPtr.
            conversions(sourceType: "nuint?", destType: "System.UIntPtr", expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nuint nuint?.Value.get""
  IL_0007:  ret
}");
            conversions(sourceType: "nuint?", destType: "bool?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nuint?", destType: "char?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u2", "nuint", "char"), expectedCheckedIL: convFromToNullableT("conv.ovf.u2.un", "nuint", "char"));
            conversions(sourceType: "nuint?", destType: "sbyte?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i1", "nuint", "sbyte"), expectedCheckedIL: convFromToNullableT("conv.ovf.i1.un", "nuint", "sbyte"));
            conversions(sourceType: "nuint?", destType: "byte?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u1", "nuint", "byte"), expectedCheckedIL: convFromToNullableT("conv.ovf.u1.un", "nuint", "byte"));
            conversions(sourceType: "nuint?", destType: "short?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i2", "nuint", "short"), expectedCheckedIL: convFromToNullableT("conv.ovf.i2.un", "nuint", "short"));
            conversions(sourceType: "nuint?", destType: "ushort?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u2", "nuint", "ushort"), expectedCheckedIL: convFromToNullableT("conv.ovf.u2.un", "nuint", "ushort"));
            conversions(sourceType: "nuint?", destType: "int?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i4", "nuint", "int"), expectedCheckedIL: convFromToNullableT("conv.ovf.i4.un", "nuint", "int"));
            conversions(sourceType: "nuint?", destType: "uint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u4", "nuint", "uint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u4.un", "nuint", "uint"));
            conversions(sourceType: "nuint?", destType: "long?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u8", "nuint", "long"), expectedCheckedIL: convFromToNullableT("conv.ovf.i8.un", "nuint", "long"));
            conversions(sourceType: "nuint?", destType: "ulong?", expectedImplicitIL: convFromToNullableT("conv.u8", "nuint", "ulong"), expectedExplicitIL: convFromToNullableT("conv.u8", "nuint", "ulong"));
            conversions(sourceType: "nuint?", destType: "float?", expectedImplicitIL: convFromToNullableT("conv.r4", "nuint", "float"), expectedExplicitIL: convFromToNullableT("conv.r4", "nuint", "float"), null);
            conversions(sourceType: "nuint?", destType: "double?", expectedImplicitIL: convFromToNullableT("conv.r8", "nuint", "double"), expectedExplicitIL: convFromToNullableT("conv.r8", "nuint", "double"), null);
            conversions(sourceType: "nuint?", destType: "decimal?",
@"{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (nuint? V_0,
                decimal? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""decimal?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_001c:  conv.u8
  IL_001d:  call       ""decimal decimal.op_Implicit(ulong)""
  IL_0022:  newobj     ""decimal?..ctor(decimal)""
  IL_0027:  ret
}",
@"{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (nuint? V_0,
                decimal? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""decimal?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_001c:  conv.u8
  IL_001d:  call       ""decimal decimal.op_Implicit(ulong)""
  IL_0022:  newobj     ""decimal?..ctor(decimal)""
  IL_0027:  ret
}");
            conversions(sourceType: "nuint?", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: null); // https://github.com/dotnet/roslyn/issues/42560: Allow explicitly casting nuint to IntPtr.
            conversions(sourceType: "nuint?", destType: "System.UIntPtr?", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "System.IntPtr", destType: "object",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""System.IntPtr""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""System.IntPtr""
  IL_0006:  ret
}");
            conversions(sourceType: "System.IntPtr", destType: "string", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.IntPtr", destType: "void*", expectedImplicitIL: null, expectedExplicitIL: convAndExplicit("void* System.IntPtr.op_Explicit(System.IntPtr)"));
            conversions(sourceType: "System.IntPtr", destType: "bool", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.IntPtr", destType: "char", expectedImplicitIL: null, expectedExplicitIL: convAndExplicit("int System.IntPtr.op_Explicit(System.IntPtr)", "conv.u2"), expectedCheckedIL: convAndExplicit("int System.IntPtr.op_Explicit(System.IntPtr)", "conv.ovf.u2"));
            conversions(sourceType: "System.IntPtr", destType: "sbyte", expectedImplicitIL: null, expectedExplicitIL: convAndExplicit("int System.IntPtr.op_Explicit(System.IntPtr)", "conv.i1"), expectedCheckedIL: convAndExplicit("int System.IntPtr.op_Explicit(System.IntPtr)", "conv.ovf.i1"));
            conversions(sourceType: "System.IntPtr", destType: "byte", expectedImplicitIL: null, expectedExplicitIL: convAndExplicit("int System.IntPtr.op_Explicit(System.IntPtr)", "conv.u1"), expectedCheckedIL: convAndExplicit("int System.IntPtr.op_Explicit(System.IntPtr)", "conv.ovf.u1"));
            conversions(sourceType: "System.IntPtr", destType: "short", expectedImplicitIL: null, expectedExplicitIL: convAndExplicit("int System.IntPtr.op_Explicit(System.IntPtr)", "conv.i2"), expectedCheckedIL: convAndExplicit("int System.IntPtr.op_Explicit(System.IntPtr)", "conv.ovf.i2"));
            conversions(sourceType: "System.IntPtr", destType: "ushort", expectedImplicitIL: null, expectedExplicitIL: convAndExplicit("int System.IntPtr.op_Explicit(System.IntPtr)", "conv.u2"), expectedCheckedIL: convAndExplicit("int System.IntPtr.op_Explicit(System.IntPtr)", "conv.ovf.u2"));
            conversions(sourceType: "System.IntPtr", destType: "int", expectedImplicitIL: null, expectedExplicitIL: convAndExplicit("int System.IntPtr.op_Explicit(System.IntPtr)"));
            conversions(sourceType: "System.IntPtr", destType: "uint", expectedImplicitIL: null, expectedExplicitIL: convAndExplicit("int System.IntPtr.op_Explicit(System.IntPtr)"), expectedCheckedIL: convAndExplicit("int System.IntPtr.op_Explicit(System.IntPtr)", "conv.ovf.u4"));
            conversions(sourceType: "System.IntPtr", destType: "long", expectedImplicitIL: null, expectedExplicitIL: convAndExplicit("long System.IntPtr.op_Explicit(System.IntPtr)"));
            conversions(sourceType: "System.IntPtr", destType: "ulong", expectedImplicitIL: null, expectedExplicitIL: convAndExplicit("long System.IntPtr.op_Explicit(System.IntPtr)"), expectedCheckedIL: convAndExplicit("long System.IntPtr.op_Explicit(System.IntPtr)", "conv.ovf.u8"));
            conversions(sourceType: "System.IntPtr", destType: "float", expectedImplicitIL: null, expectedExplicitIL: convAndExplicit("long System.IntPtr.op_Explicit(System.IntPtr)", "conv.r4"));
            conversions(sourceType: "System.IntPtr", destType: "double", expectedImplicitIL: null, expectedExplicitIL: convAndExplicit("long System.IntPtr.op_Explicit(System.IntPtr)", "conv.r8"));
            conversions(sourceType: "System.IntPtr", destType: "decimal", expectedImplicitIL: null,
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""long System.IntPtr.op_Explicit(System.IntPtr)""
  IL_0006:  call       ""decimal decimal.op_Implicit(long)""
  IL_000b:  ret
}");
            conversions(sourceType: "System.IntPtr", destType: "System.IntPtr", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "System.IntPtr", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.IntPtr", destType: "bool?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.IntPtr", destType: "char?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitToNullableT("char", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.u2"), expectedCheckedIL: convAndExplicitToNullableT("char", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.ovf.u2"));
            conversions(sourceType: "System.IntPtr", destType: "sbyte?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitToNullableT("sbyte", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.i1"), expectedCheckedIL: convAndExplicitToNullableT("sbyte", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.ovf.i1"));
            conversions(sourceType: "System.IntPtr", destType: "byte?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitToNullableT("byte", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.u1"), expectedCheckedIL: convAndExplicitToNullableT("byte", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.ovf.u1"));
            conversions(sourceType: "System.IntPtr", destType: "short?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitToNullableT("short", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.i2"), expectedCheckedIL: convAndExplicitToNullableT("short", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.ovf.i2"));
            conversions(sourceType: "System.IntPtr", destType: "ushort?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitToNullableT("ushort", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.u2"), expectedCheckedIL: convAndExplicitToNullableT("ushort", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.ovf.u2"));
            conversions(sourceType: "System.IntPtr", destType: "int?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitToNullableT("int", "int System.IntPtr.op_Explicit(System.IntPtr)"));
            conversions(sourceType: "System.IntPtr", destType: "uint?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitToNullableT("uint", "int System.IntPtr.op_Explicit(System.IntPtr)"), expectedCheckedIL: convAndExplicitToNullableT("uint", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.ovf.u4"));
            conversions(sourceType: "System.IntPtr", destType: "long?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitToNullableT("long", "long System.IntPtr.op_Explicit(System.IntPtr)"));
            conversions(sourceType: "System.IntPtr", destType: "ulong?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitToNullableT("ulong", "long System.IntPtr.op_Explicit(System.IntPtr)"), expectedCheckedIL: convAndExplicitToNullableT("ulong", "long System.IntPtr.op_Explicit(System.IntPtr)", "conv.ovf.u8"));
            conversions(sourceType: "System.IntPtr", destType: "float?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitToNullableT("float", "long System.IntPtr.op_Explicit(System.IntPtr)", "conv.r4"));
            conversions(sourceType: "System.IntPtr", destType: "double?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitToNullableT("double", "long System.IntPtr.op_Explicit(System.IntPtr)", "conv.r8"));
            conversions(sourceType: "System.IntPtr", destType: "decimal?", expectedImplicitIL: null,
@"{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""long System.IntPtr.op_Explicit(System.IntPtr)""
  IL_0006:  call       ""decimal decimal.op_Implicit(long)""
  IL_000b:  newobj     ""decimal?..ctor(decimal)""
  IL_0010:  ret
}");
            conversions(sourceType: "System.IntPtr", destType: "System.IntPtr?",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""System.IntPtr?..ctor(System.IntPtr)""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""System.IntPtr?..ctor(System.IntPtr)""
  IL_0006:  ret
}");
            conversions(sourceType: "System.IntPtr", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.IntPtr?", destType: "bool", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.IntPtr?", destType: "char", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromNullableT("System.IntPtr", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.u2"), expectedCheckedIL: convAndExplicitFromNullableT("System.IntPtr", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.ovf.u2"));
            conversions(sourceType: "System.IntPtr?", destType: "sbyte", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromNullableT("System.IntPtr", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.i1"), expectedCheckedIL: convAndExplicitFromNullableT("System.IntPtr", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.ovf.i1"));
            conversions(sourceType: "System.IntPtr?", destType: "byte", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromNullableT("System.IntPtr", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.u1"), expectedCheckedIL: convAndExplicitFromNullableT("System.IntPtr", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.ovf.u1"));
            conversions(sourceType: "System.IntPtr?", destType: "short", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromNullableT("System.IntPtr", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.i2"), expectedCheckedIL: convAndExplicitFromNullableT("System.IntPtr", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.ovf.i2"));
            conversions(sourceType: "System.IntPtr?", destType: "ushort", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromNullableT("System.IntPtr", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.u2"), expectedCheckedIL: convAndExplicitFromNullableT("System.IntPtr", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.ovf.u2"));
            conversions(sourceType: "System.IntPtr?", destType: "int", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromNullableT("System.IntPtr", "int System.IntPtr.op_Explicit(System.IntPtr)"));
            conversions(sourceType: "System.IntPtr?", destType: "uint", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromNullableT("System.IntPtr", "int System.IntPtr.op_Explicit(System.IntPtr)"), expectedCheckedIL: convAndExplicitFromNullableT("System.IntPtr", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.ovf.u4"));
            conversions(sourceType: "System.IntPtr?", destType: "long", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromNullableT("System.IntPtr", "long System.IntPtr.op_Explicit(System.IntPtr)"));
            conversions(sourceType: "System.IntPtr?", destType: "ulong", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromNullableT("System.IntPtr", "long System.IntPtr.op_Explicit(System.IntPtr)"), expectedCheckedIL: convAndExplicitFromNullableT("System.IntPtr", "long System.IntPtr.op_Explicit(System.IntPtr)", "conv.ovf.u8"));
            conversions(sourceType: "System.IntPtr?", destType: "float", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromNullableT("System.IntPtr", "long System.IntPtr.op_Explicit(System.IntPtr)", "conv.r4"));
            conversions(sourceType: "System.IntPtr?", destType: "double", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromNullableT("System.IntPtr", "long System.IntPtr.op_Explicit(System.IntPtr)", "conv.r8"));
            conversions(sourceType: "System.IntPtr?", destType: "decimal", expectedImplicitIL: null,
@"{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""System.IntPtr System.IntPtr?.Value.get""
  IL_0007:  call       ""long System.IntPtr.op_Explicit(System.IntPtr)""
  IL_000c:  call       ""decimal decimal.op_Implicit(long)""
  IL_0011:  ret
}");
            conversions(sourceType: "System.IntPtr?", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL:
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""System.IntPtr System.IntPtr?.Value.get""
  IL_0007:  ret
}");
            conversions(sourceType: "System.IntPtr?", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.IntPtr?", destType: "bool?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.IntPtr?", destType: "char?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromToNullableT("System.IntPtr", "char", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.u2"), expectedCheckedIL: convAndExplicitFromToNullableT("System.IntPtr", "char", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.ovf.u2"));
            conversions(sourceType: "System.IntPtr?", destType: "sbyte?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromToNullableT("System.IntPtr", "sbyte", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.i1"), expectedCheckedIL: convAndExplicitFromToNullableT("System.IntPtr", "sbyte", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.ovf.i1"));
            conversions(sourceType: "System.IntPtr?", destType: "byte?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromToNullableT("System.IntPtr", "byte", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.u1"), expectedCheckedIL: convAndExplicitFromToNullableT("System.IntPtr", "byte", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.ovf.u1"));
            conversions(sourceType: "System.IntPtr?", destType: "short?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromToNullableT("System.IntPtr", "short", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.i2"), expectedCheckedIL: convAndExplicitFromToNullableT("System.IntPtr", "short", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.ovf.i2"));
            conversions(sourceType: "System.IntPtr?", destType: "ushort?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromToNullableT("System.IntPtr", "ushort", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.u2"), expectedCheckedIL: convAndExplicitFromToNullableT("System.IntPtr", "ushort", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.ovf.u2"));
            conversions(sourceType: "System.IntPtr?", destType: "int?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromToNullableT("System.IntPtr", "int", "int System.IntPtr.op_Explicit(System.IntPtr)"));
            conversions(sourceType: "System.IntPtr?", destType: "uint?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromToNullableT("System.IntPtr", "uint", "int System.IntPtr.op_Explicit(System.IntPtr)"), expectedCheckedIL: convAndExplicitFromToNullableT("System.IntPtr", "uint", "int System.IntPtr.op_Explicit(System.IntPtr)", "conv.ovf.u4"));
            conversions(sourceType: "System.IntPtr?", destType: "long?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromToNullableT("System.IntPtr", "long", "long System.IntPtr.op_Explicit(System.IntPtr)"));
            conversions(sourceType: "System.IntPtr?", destType: "ulong?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromToNullableT("System.IntPtr", "ulong", "long System.IntPtr.op_Explicit(System.IntPtr)"), expectedCheckedIL: convAndExplicitFromToNullableT("System.IntPtr", "ulong", "long System.IntPtr.op_Explicit(System.IntPtr)", "conv.ovf.u8"));
            conversions(sourceType: "System.IntPtr?", destType: "float?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromToNullableT("System.IntPtr", "float", "long System.IntPtr.op_Explicit(System.IntPtr)", "conv.r4"));
            conversions(sourceType: "System.IntPtr?", destType: "double?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromToNullableT("System.IntPtr", "double", "long System.IntPtr.op_Explicit(System.IntPtr)", "conv.r8"));
            conversions(sourceType: "System.IntPtr?", destType: "decimal?", expectedImplicitIL: null,
@"{
  // Code size       39 (0x27)
  .maxstack  1
  .locals init (System.IntPtr? V_0,
                decimal? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool System.IntPtr?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""decimal?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""System.IntPtr System.IntPtr?.GetValueOrDefault()""
  IL_001c:  call       ""long System.IntPtr.op_Explicit(System.IntPtr)""
  IL_0021:  newobj     ""decimal?..ctor(decimal)""
  IL_0026:  ret
}");
            conversions(sourceType: "System.IntPtr?", destType: "System.IntPtr?", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "System.IntPtr?", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "object", destType: "System.IntPtr", expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""System.IntPtr""
  IL_0006:  ret
}");
            conversions(sourceType: "string", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "void*", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConv("System.IntPtr System.IntPtr.op_Explicit(void*)"));
            conversions(sourceType: "bool", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConv("System.IntPtr System.IntPtr.op_Explicit(int)"));
            conversions(sourceType: "sbyte", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConv("System.IntPtr System.IntPtr.op_Explicit(int)"));
            conversions(sourceType: "byte", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConv("System.IntPtr System.IntPtr.op_Explicit(int)"));
            conversions(sourceType: "short", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConv("System.IntPtr System.IntPtr.op_Explicit(int)"));
            conversions(sourceType: "ushort", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConv("System.IntPtr System.IntPtr.op_Explicit(int)"));
            conversions(sourceType: "int", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConv("System.IntPtr System.IntPtr.op_Explicit(int)"));
            conversions(sourceType: "uint", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConv("System.IntPtr System.IntPtr.op_Explicit(long)", "conv.u8"));
            conversions(sourceType: "long", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConv("System.IntPtr System.IntPtr.op_Explicit(long)"));
            conversions(sourceType: "ulong", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConv("System.IntPtr System.IntPtr.op_Explicit(long)"), expectedCheckedIL: explicitAndConv("System.IntPtr System.IntPtr.op_Explicit(long)", "conv.ovf.i8.un"));
            conversions(sourceType: "float", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConv("System.IntPtr System.IntPtr.op_Explicit(long)", "conv.i8"), expectedCheckedIL: explicitAndConv("System.IntPtr System.IntPtr.op_Explicit(long)", "conv.ovf.i8"));
            conversions(sourceType: "double", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConv("System.IntPtr System.IntPtr.op_Explicit(long)", "conv.i8"), expectedCheckedIL: explicitAndConv("System.IntPtr System.IntPtr.op_Explicit(long)", "conv.ovf.i8"));
            conversions(sourceType: "decimal", destType: "System.IntPtr", expectedImplicitIL: null,
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""long decimal.op_Explicit(decimal)""
  IL_0006:  call       ""System.IntPtr System.IntPtr.op_Explicit(long)""
  IL_000b:  ret
}");
            conversions(sourceType: "bool", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvToNullableT("System.IntPtr", "System.IntPtr System.IntPtr.op_Explicit(int)"));
            conversions(sourceType: "sbyte", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvToNullableT("System.IntPtr", "System.IntPtr System.IntPtr.op_Explicit(int)"));
            conversions(sourceType: "byte", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvToNullableT("System.IntPtr", "System.IntPtr System.IntPtr.op_Explicit(int)"));
            conversions(sourceType: "short", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvToNullableT("System.IntPtr", "System.IntPtr System.IntPtr.op_Explicit(int)"));
            conversions(sourceType: "ushort", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvToNullableT("System.IntPtr", "System.IntPtr System.IntPtr.op_Explicit(int)"));
            conversions(sourceType: "int", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvToNullableT("System.IntPtr", "System.IntPtr System.IntPtr.op_Explicit(int)"));
            conversions(sourceType: "uint", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvToNullableT("System.IntPtr", "System.IntPtr System.IntPtr.op_Explicit(long)", "conv.u8"));
            conversions(sourceType: "long", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvToNullableT("System.IntPtr", "System.IntPtr System.IntPtr.op_Explicit(long)"));
            conversions(sourceType: "ulong", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvToNullableT("System.IntPtr", "System.IntPtr System.IntPtr.op_Explicit(long)"), expectedCheckedIL: explicitAndConvToNullableT("System.IntPtr", "System.IntPtr System.IntPtr.op_Explicit(long)", "conv.ovf.i8.un"));
            conversions(sourceType: "float", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvToNullableT("System.IntPtr", "System.IntPtr System.IntPtr.op_Explicit(long)", "conv.i8"), expectedCheckedIL: explicitAndConvToNullableT("System.IntPtr", "System.IntPtr System.IntPtr.op_Explicit(long)", "conv.ovf.i8"));
            conversions(sourceType: "double", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvToNullableT("System.IntPtr", "System.IntPtr System.IntPtr.op_Explicit(long)", "conv.i8"), expectedCheckedIL: explicitAndConvToNullableT("System.IntPtr", "System.IntPtr System.IntPtr.op_Explicit(long)", "conv.ovf.i8"));
            conversions(sourceType: "decimal", destType: "System.IntPtr?", expectedImplicitIL: null,
@"{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""long decimal.op_Explicit(decimal)""
  IL_0006:  call       ""System.IntPtr System.IntPtr.op_Explicit(long)""
  IL_000b:  newobj     ""System.IntPtr?..ctor(System.IntPtr)""
  IL_0010:  ret
}");
            conversions(sourceType: "bool?", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char?", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromNullableT("char", "System.IntPtr System.IntPtr.op_Explicit(int)"));
            conversions(sourceType: "sbyte?", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromNullableT("sbyte", "System.IntPtr System.IntPtr.op_Explicit(int)"));
            conversions(sourceType: "byte?", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromNullableT("byte", "System.IntPtr System.IntPtr.op_Explicit(int)"));
            conversions(sourceType: "short?", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromNullableT("short", "System.IntPtr System.IntPtr.op_Explicit(int)"));
            conversions(sourceType: "ushort?", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromNullableT("ushort", "System.IntPtr System.IntPtr.op_Explicit(int)"));
            conversions(sourceType: "int?", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromNullableT("int", "System.IntPtr System.IntPtr.op_Explicit(int)"));
            conversions(sourceType: "uint?", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromNullableT("uint", "System.IntPtr System.IntPtr.op_Explicit(long)", "conv.u8"));
            conversions(sourceType: "long?", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromNullableT("long", "System.IntPtr System.IntPtr.op_Explicit(long)"));
            conversions(sourceType: "ulong?", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromNullableT("ulong", "System.IntPtr System.IntPtr.op_Explicit(long)"), expectedCheckedIL: explicitAndConvFromNullableT("ulong", "System.IntPtr System.IntPtr.op_Explicit(long)", "conv.ovf.i8.un"));
            conversions(sourceType: "float?", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromNullableT("float", "System.IntPtr System.IntPtr.op_Explicit(long)", "conv.i8"), expectedCheckedIL: explicitAndConvFromNullableT("float", "System.IntPtr System.IntPtr.op_Explicit(long)", "conv.ovf.i8"));
            conversions(sourceType: "double?", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromNullableT("double", "System.IntPtr System.IntPtr.op_Explicit(long)", "conv.i8"), expectedCheckedIL: explicitAndConvFromNullableT("double", "System.IntPtr System.IntPtr.op_Explicit(long)", "conv.ovf.i8"));
            conversions(sourceType: "decimal?", destType: "System.IntPtr", expectedImplicitIL: null,
@"{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""decimal decimal?.Value.get""
  IL_0007:  call       ""long decimal.op_Explicit(decimal)""
  IL_000c:  call       ""System.IntPtr System.IntPtr.op_Explicit(long)""
  IL_0011:  ret
}");
            conversions(sourceType: "bool?", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char?", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromToNullableT("char", "System.IntPtr", "System.IntPtr System.IntPtr.op_Explicit(int)"));
            conversions(sourceType: "sbyte?", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromToNullableT("sbyte", "System.IntPtr", "System.IntPtr System.IntPtr.op_Explicit(int)"));
            conversions(sourceType: "byte?", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromToNullableT("byte", "System.IntPtr", "System.IntPtr System.IntPtr.op_Explicit(int)"));
            conversions(sourceType: "short?", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromToNullableT("short", "System.IntPtr", "System.IntPtr System.IntPtr.op_Explicit(int)"));
            conversions(sourceType: "ushort?", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromToNullableT("ushort", "System.IntPtr", "System.IntPtr System.IntPtr.op_Explicit(int)"));
            conversions(sourceType: "int?", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromToNullableT("int", "System.IntPtr", "System.IntPtr System.IntPtr.op_Explicit(int)"));
            conversions(sourceType: "uint?", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromToNullableT("uint", "System.IntPtr", "System.IntPtr System.IntPtr.op_Explicit(long)", "conv.u8"));
            conversions(sourceType: "long?", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromToNullableT("long", "System.IntPtr", "System.IntPtr System.IntPtr.op_Explicit(long)"));
            conversions(sourceType: "ulong?", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromToNullableT("ulong", "System.IntPtr", "System.IntPtr System.IntPtr.op_Explicit(long)"), expectedCheckedIL: explicitAndConvFromToNullableT("ulong", "System.IntPtr", "System.IntPtr System.IntPtr.op_Explicit(long)", "conv.ovf.i8.un"));
            conversions(sourceType: "float?", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromToNullableT("float", "System.IntPtr", "System.IntPtr System.IntPtr.op_Explicit(long)", "conv.i8"), expectedCheckedIL: explicitAndConvFromToNullableT("float", "System.IntPtr", "System.IntPtr System.IntPtr.op_Explicit(long)", "conv.ovf.i8"));
            conversions(sourceType: "double?", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromToNullableT("double", "System.IntPtr", "System.IntPtr System.IntPtr.op_Explicit(long)", "conv.i8"), expectedCheckedIL: explicitAndConvFromToNullableT("double", "System.IntPtr", "System.IntPtr System.IntPtr.op_Explicit(long)", "conv.ovf.i8"));
            conversions(sourceType: "decimal?", destType: "System.IntPtr?", expectedImplicitIL: null,
@"{
  // Code size       39 (0x27)
  .maxstack  1
  .locals init (decimal? V_0,
                System.IntPtr? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool decimal?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""System.IntPtr?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""decimal decimal?.GetValueOrDefault()""
  IL_001c:  call       ""System.IntPtr System.IntPtr.op_Explicit(long)""
  IL_0021:  newobj     ""System.IntPtr?..ctor(System.IntPtr)""
  IL_0026:  ret
}");
            conversions(sourceType: "System.UIntPtr", destType: "object",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""System.UIntPtr""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""System.UIntPtr""
  IL_0006:  ret
}");
            conversions(sourceType: "System.UIntPtr", destType: "string", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.UIntPtr", destType: "void*", expectedImplicitIL: null, expectedExplicitIL: convAndExplicit("void* System.UIntPtr.op_Explicit(System.UIntPtr)"));
            conversions(sourceType: "System.UIntPtr", destType: "bool", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.UIntPtr", destType: "char", expectedImplicitIL: null, expectedExplicitIL: convAndExplicit("uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.u2"), expectedCheckedIL: convAndExplicit("uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.ovf.u2.un"));
            conversions(sourceType: "System.UIntPtr", destType: "sbyte", expectedImplicitIL: null, expectedExplicitIL: convAndExplicit("uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.i1"), expectedCheckedIL: convAndExplicit("uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.ovf.i1.un"));
            conversions(sourceType: "System.UIntPtr", destType: "byte", expectedImplicitIL: null, expectedExplicitIL: convAndExplicit("uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.u1"), expectedCheckedIL: convAndExplicit("uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.ovf.u1.un"));
            conversions(sourceType: "System.UIntPtr", destType: "short", expectedImplicitIL: null, expectedExplicitIL: convAndExplicit("uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.i2"), expectedCheckedIL: convAndExplicit("uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.ovf.i2.un"));
            conversions(sourceType: "System.UIntPtr", destType: "ushort", expectedImplicitIL: null, expectedExplicitIL: convAndExplicit("uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.u2"), expectedCheckedIL: convAndExplicit("uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.ovf.u2.un"));
            conversions(sourceType: "System.UIntPtr", destType: "int", expectedImplicitIL: null, expectedExplicitIL: convAndExplicit("uint System.UIntPtr.op_Explicit(System.UIntPtr)"), expectedCheckedIL: convAndExplicit("uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.ovf.i4.un"));
            conversions(sourceType: "System.UIntPtr", destType: "uint", expectedImplicitIL: null, expectedExplicitIL: convAndExplicit("uint System.UIntPtr.op_Explicit(System.UIntPtr)"));
            conversions(sourceType: "System.UIntPtr", destType: "long", expectedImplicitIL: null, expectedExplicitIL: convAndExplicit("ulong System.UIntPtr.op_Explicit(System.UIntPtr)"), expectedCheckedIL: convAndExplicit("ulong System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.ovf.i8.un"));
            conversions(sourceType: "System.UIntPtr", destType: "ulong", expectedImplicitIL: null, expectedExplicitIL: convAndExplicit("ulong System.UIntPtr.op_Explicit(System.UIntPtr)"));
            conversions(sourceType: "System.UIntPtr", destType: "float", expectedImplicitIL: null,
@"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ulong System.UIntPtr.op_Explicit(System.UIntPtr)""
  IL_0006:  conv.r.un
  IL_0007:  conv.r4
  IL_0008:  ret
}");
            conversions(sourceType: "System.UIntPtr", destType: "double", expectedImplicitIL: null,
@"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ulong System.UIntPtr.op_Explicit(System.UIntPtr)""
  IL_0006:  conv.r.un
  IL_0007:  conv.r8
  IL_0008:  ret
}");
            conversions(sourceType: "System.UIntPtr", destType: "decimal", expectedImplicitIL: null,
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ulong System.UIntPtr.op_Explicit(System.UIntPtr)""
  IL_0006:  call       ""decimal decimal.op_Implicit(ulong)""
  IL_000b:  ret
}");
            conversions(sourceType: "System.UIntPtr", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.UIntPtr", destType: "System.UIntPtr", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "System.UIntPtr", destType: "bool?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.UIntPtr", destType: "char?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitToNullableT("char", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.u2"), expectedCheckedIL: convAndExplicitToNullableT("char", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.ovf.u2.un"));
            conversions(sourceType: "System.UIntPtr", destType: "sbyte?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitToNullableT("sbyte", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.i1"), expectedCheckedIL: convAndExplicitToNullableT("sbyte", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.ovf.i1.un"));
            conversions(sourceType: "System.UIntPtr", destType: "byte?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitToNullableT("byte", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.u1"), expectedCheckedIL: convAndExplicitToNullableT("byte", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.ovf.u1.un"));
            conversions(sourceType: "System.UIntPtr", destType: "short?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitToNullableT("short", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.i2"), expectedCheckedIL: convAndExplicitToNullableT("short", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.ovf.i2.un"));
            conversions(sourceType: "System.UIntPtr", destType: "ushort?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitToNullableT("ushort", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.u2"), expectedCheckedIL: convAndExplicitToNullableT("ushort", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.ovf.u2.un"));
            conversions(sourceType: "System.UIntPtr", destType: "int?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitToNullableT("int", "uint System.UIntPtr.op_Explicit(System.UIntPtr)"), expectedCheckedIL: convAndExplicitToNullableT("int", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.ovf.i4.un"));
            conversions(sourceType: "System.UIntPtr", destType: "uint?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitToNullableT("uint", "uint System.UIntPtr.op_Explicit(System.UIntPtr)"));
            conversions(sourceType: "System.UIntPtr", destType: "long?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitToNullableT("long", "ulong System.UIntPtr.op_Explicit(System.UIntPtr)"), expectedCheckedIL: convAndExplicitToNullableT("long", "ulong System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.ovf.i8.un"));
            conversions(sourceType: "System.UIntPtr", destType: "ulong?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitToNullableT("ulong", "ulong System.UIntPtr.op_Explicit(System.UIntPtr)"));
            conversions(sourceType: "System.UIntPtr", destType: "float?", expectedImplicitIL: null,
@"{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ulong System.UIntPtr.op_Explicit(System.UIntPtr)""
  IL_0006:  conv.r.un
  IL_0007:  conv.r4
  IL_0008:  newobj     ""float?..ctor(float)""
  IL_000d:  ret
}");
            conversions(sourceType: "System.UIntPtr", destType: "double?", expectedImplicitIL: null,
@"{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ulong System.UIntPtr.op_Explicit(System.UIntPtr)""
  IL_0006:  conv.r.un
  IL_0007:  conv.r8
  IL_0008:  newobj     ""double?..ctor(double)""
  IL_000d:  ret
}");
            conversions(sourceType: "System.UIntPtr", destType: "decimal?", expectedImplicitIL: null,
@"{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ulong System.UIntPtr.op_Explicit(System.UIntPtr)""
  IL_0006:  call       ""decimal decimal.op_Implicit(ulong)""
  IL_000b:  newobj     ""decimal?..ctor(decimal)""
  IL_0010:  ret
}");
            conversions(sourceType: "System.UIntPtr", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.UIntPtr", destType: "System.UIntPtr?",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""System.UIntPtr?..ctor(System.UIntPtr)""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""System.UIntPtr?..ctor(System.UIntPtr)""
  IL_0006:  ret
}");
            conversions(sourceType: "System.UIntPtr?", destType: "bool", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.UIntPtr?", destType: "char", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromNullableT("System.UIntPtr", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.u2"), expectedCheckedIL: convAndExplicitFromNullableT("System.UIntPtr", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.ovf.u2.un"));
            conversions(sourceType: "System.UIntPtr?", destType: "sbyte", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromNullableT("System.UIntPtr", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.i1"), expectedCheckedIL: convAndExplicitFromNullableT("System.UIntPtr", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.ovf.i1.un"));
            conversions(sourceType: "System.UIntPtr?", destType: "byte", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromNullableT("System.UIntPtr", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.u1"), expectedCheckedIL: convAndExplicitFromNullableT("System.UIntPtr", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.ovf.u1.un"));
            conversions(sourceType: "System.UIntPtr?", destType: "short", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromNullableT("System.UIntPtr", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.i2"), expectedCheckedIL: convAndExplicitFromNullableT("System.UIntPtr", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.ovf.i2.un"));
            conversions(sourceType: "System.UIntPtr?", destType: "ushort", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromNullableT("System.UIntPtr", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.u2"), expectedCheckedIL: convAndExplicitFromNullableT("System.UIntPtr", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.ovf.u2.un"));
            conversions(sourceType: "System.UIntPtr?", destType: "int", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromNullableT("System.UIntPtr", "uint System.UIntPtr.op_Explicit(System.UIntPtr)"), expectedCheckedIL: convAndExplicitFromNullableT("System.UIntPtr", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.ovf.i4.un"));
            conversions(sourceType: "System.UIntPtr?", destType: "uint", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromNullableT("System.UIntPtr", "uint System.UIntPtr.op_Explicit(System.UIntPtr)"));
            conversions(sourceType: "System.UIntPtr?", destType: "long", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromNullableT("System.UIntPtr", "ulong System.UIntPtr.op_Explicit(System.UIntPtr)"), expectedCheckedIL: convAndExplicitFromNullableT("System.UIntPtr", "ulong System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.ovf.i8.un"));
            conversions(sourceType: "System.UIntPtr?", destType: "ulong", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromNullableT("System.UIntPtr", "ulong System.UIntPtr.op_Explicit(System.UIntPtr)"));
            conversions(sourceType: "System.UIntPtr?", destType: "float", expectedImplicitIL: null,
@"{
  // Code size       15 (0xf)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""System.UIntPtr System.UIntPtr?.Value.get""
  IL_0007:  call       ""ulong System.UIntPtr.op_Explicit(System.UIntPtr)""
  IL_000c:  conv.r.un
  IL_000d:  conv.r4
  IL_000e:  ret
}");
            conversions(sourceType: "System.UIntPtr?", destType: "double", expectedImplicitIL: null,
@"{
  // Code size       15 (0xf)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""System.UIntPtr System.UIntPtr?.Value.get""
  IL_0007:  call       ""ulong System.UIntPtr.op_Explicit(System.UIntPtr)""
  IL_000c:  conv.r.un
  IL_000d:  conv.r8
  IL_000e:  ret
}");
            conversions(sourceType: "System.UIntPtr?", destType: "decimal", expectedImplicitIL: null,
@"{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""System.UIntPtr System.UIntPtr?.Value.get""
  IL_0007:  call       ""ulong System.UIntPtr.op_Explicit(System.UIntPtr)""
  IL_000c:  call       ""decimal decimal.op_Implicit(ulong)""
  IL_0011:  ret
}");
            conversions(sourceType: "System.UIntPtr?", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.UIntPtr?", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL:
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""System.UIntPtr System.UIntPtr?.Value.get""
  IL_0007:  ret
}");
            conversions(sourceType: "System.UIntPtr?", destType: "bool?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.UIntPtr?", destType: "char?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromToNullableT("System.UIntPtr", "char", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.u2"), expectedCheckedIL: convAndExplicitFromToNullableT("System.UIntPtr", "char", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.ovf.u2.un"));
            conversions(sourceType: "System.UIntPtr?", destType: "sbyte?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromToNullableT("System.UIntPtr", "sbyte", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.i1"), expectedCheckedIL: convAndExplicitFromToNullableT("System.UIntPtr", "sbyte", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.ovf.i1.un"));
            conversions(sourceType: "System.UIntPtr?", destType: "byte?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromToNullableT("System.UIntPtr", "byte", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.u1"), expectedCheckedIL: convAndExplicitFromToNullableT("System.UIntPtr", "byte", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.ovf.u1.un"));
            conversions(sourceType: "System.UIntPtr?", destType: "short?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromToNullableT("System.UIntPtr", "short", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.i2"), expectedCheckedIL: convAndExplicitFromToNullableT("System.UIntPtr", "short", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.ovf.i2.un"));
            conversions(sourceType: "System.UIntPtr?", destType: "ushort?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromToNullableT("System.UIntPtr", "ushort", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.u2"), expectedCheckedIL: convAndExplicitFromToNullableT("System.UIntPtr", "ushort", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.ovf.u2.un"));
            conversions(sourceType: "System.UIntPtr?", destType: "int?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromToNullableT("System.UIntPtr", "int", "uint System.UIntPtr.op_Explicit(System.UIntPtr)"), expectedCheckedIL: convAndExplicitFromToNullableT("System.UIntPtr", "int", "uint System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.ovf.i4.un"));
            conversions(sourceType: "System.UIntPtr?", destType: "uint?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromToNullableT("System.UIntPtr", "uint", "uint System.UIntPtr.op_Explicit(System.UIntPtr)"));
            conversions(sourceType: "System.UIntPtr?", destType: "long?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromToNullableT("System.UIntPtr", "long", "ulong System.UIntPtr.op_Explicit(System.UIntPtr)"), expectedCheckedIL: convAndExplicitFromToNullableT("System.UIntPtr", "long", "ulong System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.ovf.i8.un"));
            conversions(sourceType: "System.UIntPtr?", destType: "ulong?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromToNullableT("System.UIntPtr", "ulong", "ulong System.UIntPtr.op_Explicit(System.UIntPtr)"));
            conversions(sourceType: "System.UIntPtr?", destType: "float?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromToNullableT("System.UIntPtr", "float", "ulong System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.r4"));
            conversions(sourceType: "System.UIntPtr?", destType: "double?", expectedImplicitIL: null, expectedExplicitIL: convAndExplicitFromToNullableT("System.UIntPtr", "double", "ulong System.UIntPtr.op_Explicit(System.UIntPtr)", "conv.r8"));
            conversions(sourceType: "System.UIntPtr?", destType: "decimal?", expectedImplicitIL: null,
@"{
  // Code size       39 (0x27)
  .maxstack  1
  .locals init (System.UIntPtr? V_0,
                decimal? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool System.UIntPtr?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""decimal?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""System.UIntPtr System.UIntPtr?.GetValueOrDefault()""
  IL_001c:  call       ""ulong System.UIntPtr.op_Explicit(System.UIntPtr)""
  IL_0021:  newobj     ""decimal?..ctor(decimal)""
  IL_0026:  ret
}");
            conversions(sourceType: "System.UIntPtr?", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.UIntPtr?", destType: "System.UIntPtr?", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "object", destType: "System.UIntPtr", expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""System.UIntPtr""
  IL_0006:  ret
}");
            conversions(sourceType: "string", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "void*", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConv("System.UIntPtr System.UIntPtr.op_Explicit(void*)"));
            conversions(sourceType: "bool", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConv("System.UIntPtr System.UIntPtr.op_Explicit(uint)"));
            conversions(sourceType: "sbyte", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConv("System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.i8"), expectedCheckedIL: explicitAndConv("System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.ovf.u8"));
            conversions(sourceType: "byte", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConv("System.UIntPtr System.UIntPtr.op_Explicit(uint)"));
            conversions(sourceType: "short", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConv("System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.i8"), expectedCheckedIL: explicitAndConv("System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.ovf.u8"));
            conversions(sourceType: "ushort", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConv("System.UIntPtr System.UIntPtr.op_Explicit(uint)"));
            conversions(sourceType: "int", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConv("System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.i8"), expectedCheckedIL: explicitAndConv("System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.ovf.u8"));
            conversions(sourceType: "uint", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConv("System.UIntPtr System.UIntPtr.op_Explicit(uint)"));
            conversions(sourceType: "long", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConv("System.UIntPtr System.UIntPtr.op_Explicit(ulong)"), expectedCheckedIL: explicitAndConv("System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.ovf.u8"));
            conversions(sourceType: "ulong", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConv("System.UIntPtr System.UIntPtr.op_Explicit(ulong)"));
            conversions(sourceType: "float", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConv("System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.u8"), expectedCheckedIL: explicitAndConv("System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.ovf.u8"));
            conversions(sourceType: "double", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConv("System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.u8"), expectedCheckedIL: explicitAndConv("System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.ovf.u8"));
            conversions(sourceType: "decimal", destType: "System.UIntPtr", expectedImplicitIL: null,
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_0006:  call       ""System.UIntPtr System.UIntPtr.op_Explicit(ulong)""
  IL_000b:  ret
}");
            conversions(sourceType: "bool", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvToNullableT("System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(uint)"));
            conversions(sourceType: "sbyte", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvToNullableT("System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.i8"), expectedCheckedIL: explicitAndConvToNullableT("System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.ovf.u8"));
            conversions(sourceType: "byte", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvToNullableT("System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(uint)"));
            conversions(sourceType: "short", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvToNullableT("System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.i8"), expectedCheckedIL: explicitAndConvToNullableT("System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.ovf.u8"));
            conversions(sourceType: "ushort", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvToNullableT("System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(uint)"));
            conversions(sourceType: "int", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvToNullableT("System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.i8"), expectedCheckedIL: explicitAndConvToNullableT("System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.ovf.u8"));
            conversions(sourceType: "uint", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvToNullableT("System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(uint)"));
            conversions(sourceType: "long", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvToNullableT("System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)"), expectedCheckedIL: explicitAndConvToNullableT("System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.ovf.u8"));
            conversions(sourceType: "ulong", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvToNullableT("System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)"));
            conversions(sourceType: "float", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvToNullableT("System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.u8"), expectedCheckedIL: explicitAndConvToNullableT("System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.ovf.u8"));
            conversions(sourceType: "double", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvToNullableT("System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.u8"), expectedCheckedIL: explicitAndConvToNullableT("System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.ovf.u8"));
            conversions(sourceType: "decimal", destType: "System.UIntPtr?", expectedImplicitIL: null,
@"{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_0006:  call       ""System.UIntPtr System.UIntPtr.op_Explicit(ulong)""
  IL_000b:  newobj     ""System.UIntPtr?..ctor(System.UIntPtr)""
  IL_0010:  ret
}");
            conversions(sourceType: "bool?", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char?", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromNullableT("char", "System.UIntPtr System.UIntPtr.op_Explicit(uint)"));
            conversions(sourceType: "sbyte?", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromNullableT("sbyte", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.i8"), expectedCheckedIL: explicitAndConvFromNullableT("sbyte", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.ovf.u8"));
            conversions(sourceType: "byte?", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromNullableT("byte", "System.UIntPtr System.UIntPtr.op_Explicit(uint)"));
            conversions(sourceType: "short?", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromNullableT("short", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.i8"), expectedCheckedIL: explicitAndConvFromNullableT("short", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.ovf.u8"));
            conversions(sourceType: "ushort?", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromNullableT("ushort", "System.UIntPtr System.UIntPtr.op_Explicit(uint)"));
            conversions(sourceType: "int?", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromNullableT("int", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.i8"), expectedCheckedIL: explicitAndConvFromNullableT("int", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.ovf.u8"));
            conversions(sourceType: "uint?", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromNullableT("uint", "System.UIntPtr System.UIntPtr.op_Explicit(uint)"));
            conversions(sourceType: "long?", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromNullableT("long", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)"), expectedCheckedIL: explicitAndConvFromNullableT("long", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.ovf.u8"));
            conversions(sourceType: "ulong?", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromNullableT("ulong", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)"));
            conversions(sourceType: "float?", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromNullableT("float", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.u8"), expectedCheckedIL: explicitAndConvFromNullableT("float", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.ovf.u8"));
            conversions(sourceType: "double?", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromNullableT("double", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.u8"), expectedCheckedIL: explicitAndConvFromNullableT("double", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.ovf.u8"));
            conversions(sourceType: "decimal?", destType: "System.UIntPtr", expectedImplicitIL: null,
@"{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""decimal decimal?.Value.get""
  IL_0007:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_000c:  call       ""System.UIntPtr System.UIntPtr.op_Explicit(ulong)""
  IL_0011:  ret
}");
            conversions(sourceType: "bool?", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char?", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromToNullableT("char", "System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(uint)"));
            conversions(sourceType: "sbyte?", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromToNullableT("sbyte", "System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.i8"), expectedCheckedIL: explicitAndConvFromToNullableT("sbyte", "System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.ovf.u8"));
            conversions(sourceType: "byte?", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromToNullableT("byte", "System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(uint)"));
            conversions(sourceType: "short?", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromToNullableT("short", "System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.i8"), expectedCheckedIL: explicitAndConvFromToNullableT("short", "System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.ovf.u8"));
            conversions(sourceType: "ushort?", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromToNullableT("ushort", "System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(uint)"));
            conversions(sourceType: "int?", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromToNullableT("int", "System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.i8"), expectedCheckedIL: explicitAndConvFromToNullableT("int", "System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.ovf.u8"));
            conversions(sourceType: "uint?", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromToNullableT("uint", "System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(uint)"));
            conversions(sourceType: "long?", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromToNullableT("long", "System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.i8"), expectedCheckedIL: explicitAndConvFromToNullableT("long", "System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.ovf.u8"));
            conversions(sourceType: "ulong?", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromToNullableT("ulong", "System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)"));
            conversions(sourceType: "float?", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromToNullableT("float", "System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.u8"), expectedCheckedIL: explicitAndConvFromToNullableT("float", "System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.ovf.u8"));
            conversions(sourceType: "double?", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: explicitAndConvFromToNullableT("double", "System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.u8"), expectedCheckedIL: explicitAndConvFromToNullableT("double", "System.UIntPtr", "System.UIntPtr System.UIntPtr.op_Explicit(ulong)", "conv.ovf.u8"));
            // https://github.com/dotnet/roslyn/issues/42834: Invalid code generated for nullable conversions
            // involving System.[U]IntPtr: the conversion ulong decimal.op_Explicit(decimal) is dropped.
            conversions(sourceType: "decimal?", destType: "System.UIntPtr?", expectedImplicitIL: null,
@"{
  // Code size       39 (0x27)
  .maxstack  1
  .locals init (decimal? V_0,
                System.UIntPtr? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool decimal?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""System.UIntPtr?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""decimal decimal?.GetValueOrDefault()""
  IL_001c:  call       ""System.UIntPtr System.UIntPtr.op_Explicit(ulong)""
  IL_0021:  newobj     ""System.UIntPtr?..ctor(System.UIntPtr)""
  IL_0026:  ret
}");

            void convert(string sourceType,
                string destType,
                string expectedIL,
                bool skipTypeChecks,
                bool useExplicitCast,
                bool useChecked,
                bool verify,
                ErrorCode expectedErrorCode)
            {
                bool useUnsafeContext = useUnsafe(sourceType) || useUnsafe(destType);
                string value = "value";
                if (useExplicitCast)
                {
                    value = $"({destType})value";
                }
                var expectedDiagnostics = expectedErrorCode == 0 ?
                    Array.Empty<DiagnosticDescription>() :
                    new[] { Diagnostic(expectedErrorCode, value).WithArguments(sourceType, destType) };
                if (useChecked)
                {
                    value = $"checked({value})";
                }
                string source =
    $@"class Program
{{
    static {(useUnsafeContext ? "unsafe " : "")}{destType} Convert({sourceType} value)
    {{
        return {value};
    }}
}}";
                var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithAllowUnsafe(useUnsafeContext), parseOptions: TestOptions.RegularPreview);
                comp.VerifyDiagnostics(expectedDiagnostics);

                var tree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(tree);
                var expr = tree.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression;
                var typeInfo = model.GetTypeInfo(expr);

                if (!skipTypeChecks)
                {
                    Assert.Equal(sourceType, typeInfo.Type.ToString());
                    Assert.Equal(destType, typeInfo.ConvertedType.ToString());
                }

                if (expectedIL != null)
                {
                    var verifier = CompileAndVerify(comp, verify: useUnsafeContext || !verify ? Verification.Skipped : Verification.Passes);
                    verifier.VerifyIL("Program.Convert", expectedIL);
                }

                static bool useUnsafe(string type) => type == "void*";
            }
        }

        [Fact]
        public void UnaryOperators()
        {
            static string getComplement(uint value)
            {
                object result = (IntPtr.Size == 4) ?
                    (object)~value :
                    (object)~(ulong)value;
                return result.ToString();
            }

            void unaryOp(string op, string opType, string expectedSymbol = null, string operand = null, string expectedResult = null, string expectedIL = "", DiagnosticDescription diagnostic = null)
            {
                operand ??= "default";
                if (expectedSymbol == null && diagnostic == null)
                {
                    diagnostic = Diagnostic(ErrorCode.ERR_BadUnaryOp, $"{op}operand").WithArguments(op, opType);
                }

                unaryOperator(op, opType, opType, expectedSymbol, operand, expectedResult, expectedIL, diagnostic != null ? new[] { diagnostic } : Array.Empty<DiagnosticDescription>());
            }

            unaryOp("+", "nint", "nint nint.op_UnaryPlus(nint value)", "3", "3",
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
            unaryOp(" + ", "nuint", "nuint nuint.op_UnaryPlus(nuint value)", "3", "3",
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
            unaryOp("+", "System.IntPtr");
            unaryOp("+", "System.UIntPtr");
            unaryOp("-", "nint", "nint nint.op_UnaryNegation(nint value)", "3", "-3",
@"{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  neg
  IL_0002:  ret
}");
            unaryOp("-", "nuint");
            unaryOp("-", "System.IntPtr");
            unaryOp("-", "System.UIntPtr");
            unaryOp("!", "nint");
            unaryOp("!", "nuint");
            unaryOp("!", "System.IntPtr");
            unaryOp("!", "System.UIntPtr");
            unaryOp("~", "nint", "nint nint.op_OnesComplement(nint value)", "3", "-4",
@"{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  not
  IL_0002:  ret
}");
            unaryOp("~", "nuint", "nuint nuint.op_OnesComplement(nuint value)", "3", getComplement(3),
@"{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  not
  IL_0002:  ret
}");
            unaryOp("~", "System.IntPtr");
            unaryOp("~", "System.UIntPtr");

            unaryOp("+", "nint?", "nint nint.op_UnaryPlus(nint value)", "3", "3",
@"{
  // Code size       34 (0x22)
  .maxstack  1
  .locals init (nint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""nint nint?.GetValueOrDefault()""
  IL_001c:  newobj     ""nint?..ctor(nint)""
  IL_0021:  ret
}");
            unaryOp("+", "nuint?", "nuint nuint.op_UnaryPlus(nuint value)", "3", "3",
@"{
  // Code size       34 (0x22)
  .maxstack  1
  .locals init (nuint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_001c:  newobj     ""nuint?..ctor(nuint)""
  IL_0021:  ret
}");
            unaryOp("+", "System.IntPtr?");
            unaryOp("+", "System.UIntPtr?");
            unaryOp("-", "nint?", "nint nint.op_UnaryNegation(nint value)", "3", "-3",
@"{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (nint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""nint nint?.GetValueOrDefault()""
  IL_001c:  neg
  IL_001d:  newobj     ""nint?..ctor(nint)""
  IL_0022:  ret
}");
            // Reporting ERR_AmbigUnaryOp for `-(nuint?)value` is inconsistent with the ERR_BadUnaryOp reported
            // for `-(nuint)value`, but that difference in behavior is consistent with the pair of errors reported for
            // `-(ulong?)value` and `-(ulong)value`. See the "Special case" in Binder.UnaryOperatorOverloadResolution()
            // which handles ulong but not ulong?.
            unaryOp("-", "nuint?", null, null, null, null, Diagnostic(ErrorCode.ERR_AmbigUnaryOp, "-operand").WithArguments("-", "nuint?"));
            unaryOp("-", "System.IntPtr?");
            unaryOp("-", "System.UIntPtr?");
            unaryOp("!", "nint?");
            unaryOp("!", "nuint?");
            unaryOp("!", "System.IntPtr?");
            unaryOp("!", "System.UIntPtr?");
            unaryOp("~", "nint?", "nint nint.op_OnesComplement(nint value)", "3", "-4",
@"{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (nint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""nint nint?.GetValueOrDefault()""
  IL_001c:  not
  IL_001d:  newobj     ""nint?..ctor(nint)""
  IL_0022:  ret
}");
            unaryOp("~", "nuint?", "nuint nuint.op_OnesComplement(nuint value)", "3", getComplement(3),
@"{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (nuint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_001c:  not
  IL_001d:  newobj     ""nuint?..ctor(nuint)""
  IL_0022:  ret
}");
            unaryOp("~", "System.IntPtr?");
            unaryOp("~", "System.UIntPtr?");

            void unaryOperator(string op, string opType, string resultType, string expectedSymbol, string operand, string expectedResult, string expectedIL, DiagnosticDescription[] expectedDiagnostics)
            {
                string source =
    $@"class Program
{{
    static {resultType} Evaluate({opType} operand)
    {{
        return {op}operand;
    }}
    static void Main()
    {{
        System.Console.WriteLine(Evaluate({operand}));
    }}
}}";
                var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
                comp.VerifyDiagnostics(expectedDiagnostics);

                var tree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(tree);
                var expr = tree.GetRoot().DescendantNodes().OfType<PrefixUnaryExpressionSyntax>().Single();
                var symbolInfo = model.GetSymbolInfo(expr);
                Assert.Equal(expectedSymbol, symbolInfo.Symbol?.ToDisplayString(SymbolDisplayFormat.TestFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes)));

                if (expectedDiagnostics.Length == 0)
                {
                    var verifier = CompileAndVerify(comp, expectedOutput: expectedResult);
                    verifier.VerifyIL("Program.Evaluate", expectedIL);
                }
            }
        }

        [Fact]
        public void IncrementOperators()
        {
            void incrementOps(string op, string opType, string expectedSymbol = null, bool useChecked = false, string values = null, string expectedResult = null, string expectedIL = "", string expectedLiftedIL = "", DiagnosticDescription diagnostic = null)
            {
                incrementOperator(op, opType, isPrefix: true, expectedSymbol, useChecked, values, expectedResult, expectedIL, getDiagnostics(opType, isPrefix: true, diagnostic));
                incrementOperator(op, opType, isPrefix: false, expectedSymbol, useChecked, values, expectedResult, expectedIL, getDiagnostics(opType, isPrefix: false, diagnostic));
                opType += "?";
                incrementOperator(op, opType, isPrefix: true, expectedSymbol, useChecked, values, expectedResult, expectedLiftedIL, getDiagnostics(opType, isPrefix: true, diagnostic));
                incrementOperator(op, opType, isPrefix: false, expectedSymbol, useChecked, values, expectedResult, expectedLiftedIL, getDiagnostics(opType, isPrefix: false, diagnostic));

                DiagnosticDescription[] getDiagnostics(string opType, bool isPrefix, DiagnosticDescription diagnostic)
                {
                    if (expectedSymbol == null && diagnostic == null)
                    {
                        diagnostic = Diagnostic(ErrorCode.ERR_BadUnaryOp, isPrefix ? op + "operand" : "operand" + op).WithArguments(op, opType);
                    }
                    return diagnostic != null ? new[] { diagnostic } : Array.Empty<DiagnosticDescription>();
                }
            }

            incrementOps("++", "nint", "nint nint.op_Increment(nint value)", useChecked: false,
                values: $"{int.MinValue}, -1, 0, {int.MaxValue - 1}, {int.MaxValue}",
                expectedResult: $"-2147483647, 0, 1, 2147483647, {(IntPtr.Size == 4 ? "-2147483648" : "2147483648")}",
@"{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  add
  IL_0003:  starg.s    V_0
  IL_0005:  ldarg.0
  IL_0006:  ret
}",
@"{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (nint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0016
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  br.s       IL_0024
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""nint nint?.GetValueOrDefault()""
  IL_001d:  ldc.i4.1
  IL_001e:  add
  IL_001f:  newobj     ""nint?..ctor(nint)""
  IL_0024:  starg.s    V_0
  IL_0026:  ldarg.0
  IL_0027:  ret
}");
            incrementOps("++", "nuint", "nuint nuint.op_Increment(nuint value)", useChecked: false,
                values: $"0, {int.MaxValue}, {uint.MaxValue - 1}, {uint.MaxValue}",
                expectedResult: $"1, 2147483648, 4294967295, {(IntPtr.Size == 4 ? "0" : "4294967296")}",
@"{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  add
  IL_0003:  starg.s    V_0
  IL_0005:  ldarg.0
  IL_0006:  ret
}",
@"{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (nuint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0016
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  br.s       IL_0024
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_001d:  ldc.i4.1
  IL_001e:  add
  IL_001f:  newobj     ""nuint?..ctor(nuint)""
  IL_0024:  starg.s    V_0
  IL_0026:  ldarg.0
  IL_0027:  ret
}");
            incrementOps("++", "System.IntPtr");
            incrementOps("++", "System.UIntPtr");
            incrementOps("--", "nint", "nint nint.op_Decrement(nint value)", useChecked: false,
                values: $"{int.MinValue}, {int.MinValue + 1}, 0, 1, {int.MaxValue}",
                expectedResult: $"{(IntPtr.Size == 4 ? "2147483647" : "-2147483649")}, -2147483648, -1, 0, 2147483646",
@"{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  sub
  IL_0003:  starg.s    V_0
  IL_0005:  ldarg.0
  IL_0006:  ret
}",
@"{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (nint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0016
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  br.s       IL_0024
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""nint nint?.GetValueOrDefault()""
  IL_001d:  ldc.i4.1
  IL_001e:  sub
  IL_001f:  newobj     ""nint?..ctor(nint)""
  IL_0024:  starg.s    V_0
  IL_0026:  ldarg.0
  IL_0027:  ret
}");
            incrementOps("--", "nuint", "nuint nuint.op_Decrement(nuint value)", useChecked: false,
                values: $"0, 1, {uint.MaxValue}",
                expectedResult: $"{(IntPtr.Size == 4 ? uint.MaxValue.ToString() : ulong.MaxValue.ToString())}, 0, 4294967294",
@"{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  sub
  IL_0003:  starg.s    V_0
  IL_0005:  ldarg.0
  IL_0006:  ret
}",
@"{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (nuint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0016
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  br.s       IL_0024
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_001d:  ldc.i4.1
  IL_001e:  sub
  IL_001f:  newobj     ""nuint?..ctor(nuint)""
  IL_0024:  starg.s    V_0
  IL_0026:  ldarg.0
  IL_0027:  ret
}");
            incrementOps("--", "System.IntPtr");
            incrementOps("--", "System.UIntPtr");

            incrementOps("++", "nint", "nint nint.op_Increment(nint value)", useChecked: true,
                values: $"{int.MinValue}, -1, 0, {int.MaxValue - 1}, {int.MaxValue}",
                expectedResult: $"-2147483647, 0, 1, 2147483647, {(IntPtr.Size == 4 ? "System.OverflowException" : "2147483648")}",
@"{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  add.ovf
  IL_0003:  starg.s    V_0
  IL_0005:  ldarg.0
  IL_0006:  ret
}",
@"{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (nint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0016
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  br.s       IL_0024
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""nint nint?.GetValueOrDefault()""
  IL_001d:  ldc.i4.1
  IL_001e:  add.ovf
  IL_001f:  newobj     ""nint?..ctor(nint)""
  IL_0024:  starg.s    V_0
  IL_0026:  ldarg.0
  IL_0027:  ret
}");
            incrementOps("++", "nuint", "nuint nuint.op_Increment(nuint value)", useChecked: true,
                values: $"0, {int.MaxValue}, {uint.MaxValue - 1}, {uint.MaxValue}",
                expectedResult: $"1, 2147483648, 4294967295, {(IntPtr.Size == 4 ? "System.OverflowException" : "4294967296")}",
@"{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  add.ovf.un
  IL_0003:  starg.s    V_0
  IL_0005:  ldarg.0
  IL_0006:  ret
}",
@"{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (nuint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0016
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  br.s       IL_0024
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_001d:  ldc.i4.1
  IL_001e:  add.ovf.un
  IL_001f:  newobj     ""nuint?..ctor(nuint)""
  IL_0024:  starg.s    V_0
  IL_0026:  ldarg.0
  IL_0027:  ret
}");
            incrementOps("++", "System.IntPtr", null, useChecked: true);
            incrementOps("++", "System.UIntPtr", null, useChecked: true);
            incrementOps("--", "nint", "nint nint.op_Decrement(nint value)", useChecked: true,
                values: $"{int.MinValue}, {int.MinValue + 1}, 0, 1, {int.MaxValue}",
                expectedResult: $"{(IntPtr.Size == 4 ? "System.OverflowException" : "-2147483649")}, -2147483648, -1, 0, 2147483646",
@"{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  sub.ovf
  IL_0003:  starg.s    V_0
  IL_0005:  ldarg.0
  IL_0006:  ret
}",
@"{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (nint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0016
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  br.s       IL_0024
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""nint nint?.GetValueOrDefault()""
  IL_001d:  ldc.i4.1
  IL_001e:  sub.ovf
  IL_001f:  newobj     ""nint?..ctor(nint)""
  IL_0024:  starg.s    V_0
  IL_0026:  ldarg.0
  IL_0027:  ret
}");
            incrementOps("--", "nuint", "nuint nuint.op_Decrement(nuint value)", useChecked: true,
                values: $"0, 1, {uint.MaxValue}",
                expectedResult: $"System.OverflowException, 0, 4294967294",
@"{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  sub.ovf.un
  IL_0003:  starg.s    V_0
  IL_0005:  ldarg.0
  IL_0006:  ret
}",
@"{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (nuint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0016
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  br.s       IL_0024
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_001d:  ldc.i4.1
  IL_001e:  sub.ovf.un
  IL_001f:  newobj     ""nuint?..ctor(nuint)""
  IL_0024:  starg.s    V_0
  IL_0026:  ldarg.0
  IL_0027:  ret
}");
            incrementOps("--", "System.IntPtr", null, useChecked: true);
            incrementOps("--", "System.UIntPtr", null, useChecked: true);

            void incrementOperator(string op, string opType, bool isPrefix, string expectedSymbol, bool useChecked, string values, string expectedResult, string expectedIL, DiagnosticDescription[] expectedDiagnostics)
            {
                var source =
$@"using System;
class Program
{{
    static {opType} Evaluate({opType} operand)
    {{
        {(useChecked ? "checked" : "unchecked")}
        {{
            {(isPrefix ? op + "operand" : "operand" + op)};
            return operand;
        }}
    }}
    static void EvaluateAndReport({opType} operand)
    {{
        object result;
        try
        {{
            result = Evaluate(operand);
        }}
        catch (Exception e)
        {{
            result = e.GetType();
        }}
        Console.Write(result);
    }}
    static void Main()
    {{
        bool separator = false;
        foreach (var value in new {opType}[] {{ {values} }})
        {{
            if (separator) Console.Write("", "");
            separator = true;
            EvaluateAndReport(value);
        }}
    }}
}}";
                var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
                comp.VerifyDiagnostics(expectedDiagnostics);

                var tree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(tree);
                var kind = (op == "++") ?
                    isPrefix ? SyntaxKind.PreIncrementExpression : SyntaxKind.PostIncrementExpression :
                    isPrefix ? SyntaxKind.PreDecrementExpression : SyntaxKind.PostDecrementExpression;
                var expr = tree.GetRoot().DescendantNodes().Single(n => n.Kind() == kind);
                var symbolInfo = model.GetSymbolInfo(expr);
                Assert.Equal(expectedSymbol, symbolInfo.Symbol?.ToDisplayString(SymbolDisplayFormat.TestFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes)));

                if (expectedDiagnostics.Length == 0)
                {
                    var verifier = CompileAndVerify(comp, expectedOutput: expectedResult);
                    verifier.VerifyIL("Program.Evaluate", expectedIL);
                }
            }
        }

        [Fact]
        public void IncrementOperators_RefOperand()
        {
            void incrementOps(string op, string opType, string expectedSymbol = null, string values = null, string expectedResult = null, string expectedIL = "", string expectedLiftedIL = "", DiagnosticDescription diagnostic = null)
            {
                incrementOperator(op, opType, expectedSymbol, values, expectedResult, expectedIL, getDiagnostics(opType, diagnostic));
                opType += "?";
                incrementOperator(op, opType, expectedSymbol, values, expectedResult, expectedLiftedIL, getDiagnostics(opType, diagnostic));

                DiagnosticDescription[] getDiagnostics(string opType, DiagnosticDescription diagnostic)
                {
                    if (expectedSymbol == null && diagnostic == null)
                    {
                        diagnostic = Diagnostic(ErrorCode.ERR_BadUnaryOp, op + "operand").WithArguments(op, opType);
                    }
                    return diagnostic != null ? new[] { diagnostic } : Array.Empty<DiagnosticDescription>();
                }
            }

            incrementOps("++", "nint", "nint nint.op_Increment(nint value)",
                values: $"{int.MinValue}, -1, 0, {int.MaxValue - 1}, {int.MaxValue}",
                expectedResult: $"-2147483647, 0, 1, 2147483647, {(IntPtr.Size == 4 ? "-2147483648" : "2147483648")}",
@"{
  // Code size        7 (0x7)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldind.i
  IL_0003:  ldc.i4.1
  IL_0004:  add
  IL_0005:  stind.i
  IL_0006:  ret
}",
@"{
  // Code size       48 (0x30)
  .maxstack  3
  .locals init (nint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldobj      ""nint?""
  IL_0007:  stloc.0
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       ""bool nint?.HasValue.get""
  IL_000f:  brtrue.s   IL_001c
  IL_0011:  ldloca.s   V_1
  IL_0013:  initobj    ""nint?""
  IL_0019:  ldloc.1
  IL_001a:  br.s       IL_002a
  IL_001c:  ldloca.s   V_0
  IL_001e:  call       ""nint nint?.GetValueOrDefault()""
  IL_0023:  ldc.i4.1
  IL_0024:  add
  IL_0025:  newobj     ""nint?..ctor(nint)""
  IL_002a:  stobj      ""nint?""
  IL_002f:  ret
}");
            incrementOps("++", "nuint", "nuint nuint.op_Increment(nuint value)",
                values: $"0, {int.MaxValue}, {uint.MaxValue - 1}, {uint.MaxValue}",
                expectedResult: $"1, 2147483648, 4294967295, {(IntPtr.Size == 4 ? "0" : "4294967296")}",
@"{
  // Code size        7 (0x7)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldind.i
  IL_0003:  ldc.i4.1
  IL_0004:  add
  IL_0005:  stind.i
  IL_0006:  ret
}",
@"{
  // Code size       48 (0x30)
  .maxstack  3
  .locals init (nuint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldobj      ""nuint?""
  IL_0007:  stloc.0
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       ""bool nuint?.HasValue.get""
  IL_000f:  brtrue.s   IL_001c
  IL_0011:  ldloca.s   V_1
  IL_0013:  initobj    ""nuint?""
  IL_0019:  ldloc.1
  IL_001a:  br.s       IL_002a
  IL_001c:  ldloca.s   V_0
  IL_001e:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_0023:  ldc.i4.1
  IL_0024:  add
  IL_0025:  newobj     ""nuint?..ctor(nuint)""
  IL_002a:  stobj      ""nuint?""
  IL_002f:  ret
}");
            incrementOps("--", "nint", "nint nint.op_Decrement(nint value)",
                values: $"{int.MinValue}, {int.MinValue + 1}, 0, 1, {int.MaxValue}",
                expectedResult: $"{(IntPtr.Size == 4 ? "2147483647" : "-2147483649")}, -2147483648, -1, 0, 2147483646",
@"{
  // Code size        7 (0x7)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldind.i
  IL_0003:  ldc.i4.1
  IL_0004:  sub
  IL_0005:  stind.i
  IL_0006:  ret
}",
@"{
  // Code size       48 (0x30)
  .maxstack  3
  .locals init (nint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldobj      ""nint?""
  IL_0007:  stloc.0
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       ""bool nint?.HasValue.get""
  IL_000f:  brtrue.s   IL_001c
  IL_0011:  ldloca.s   V_1
  IL_0013:  initobj    ""nint?""
  IL_0019:  ldloc.1
  IL_001a:  br.s       IL_002a
  IL_001c:  ldloca.s   V_0
  IL_001e:  call       ""nint nint?.GetValueOrDefault()""
  IL_0023:  ldc.i4.1
  IL_0024:  sub
  IL_0025:  newobj     ""nint?..ctor(nint)""
  IL_002a:  stobj      ""nint?""
  IL_002f:  ret
}");
            incrementOps("--", "nuint", "nuint nuint.op_Decrement(nuint value)",
                values: $"0, 1, {uint.MaxValue}",
                expectedResult: $"{(IntPtr.Size == 4 ? uint.MaxValue.ToString() : ulong.MaxValue.ToString())}, 0, 4294967294",
@"{
  // Code size        7 (0x7)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldind.i
  IL_0003:  ldc.i4.1
  IL_0004:  sub
  IL_0005:  stind.i
  IL_0006:  ret
}",
@"{
  // Code size       48 (0x30)
  .maxstack  3
  .locals init (nuint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldobj      ""nuint?""
  IL_0007:  stloc.0
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       ""bool nuint?.HasValue.get""
  IL_000f:  brtrue.s   IL_001c
  IL_0011:  ldloca.s   V_1
  IL_0013:  initobj    ""nuint?""
  IL_0019:  ldloc.1
  IL_001a:  br.s       IL_002a
  IL_001c:  ldloca.s   V_0
  IL_001e:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_0023:  ldc.i4.1
  IL_0024:  sub
  IL_0025:  newobj     ""nuint?..ctor(nuint)""
  IL_002a:  stobj      ""nuint?""
  IL_002f:  ret
}");

            void incrementOperator(string op, string opType, string expectedSymbol, string values, string expectedResult, string expectedIL, DiagnosticDescription[] expectedDiagnostics)
            {
                string source =
    $@"using System;
class Program
{{
    static void Evaluate(ref {opType} operand)
    {{
        {op}operand;
    }}
    static void EvaluateAndReport({opType} operand)
    {{
        object result;
        try
        {{
            Evaluate(ref operand);
            result = operand;
        }}
        catch (Exception e)
        {{
            result = e.GetType();
        }}
        Console.Write(result);
    }}
    static void Main()
    {{
        bool separator = false;
        foreach (var value in new {opType}[] {{ {values} }})
        {{
            if (separator) Console.Write("", "");
            separator = true;
            EvaluateAndReport(value);
        }}
    }}
}}";
                var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
                comp.VerifyDiagnostics(expectedDiagnostics);

                var tree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(tree);
                var kind = (op == "++") ? SyntaxKind.PreIncrementExpression : SyntaxKind.PreDecrementExpression;
                var expr = tree.GetRoot().DescendantNodes().Single(n => n.Kind() == kind);
                var symbolInfo = model.GetSymbolInfo(expr);
                Assert.Equal(expectedSymbol, symbolInfo.Symbol?.ToDisplayString(SymbolDisplayFormat.TestFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes)));

                if (expectedDiagnostics.Length == 0)
                {
                    var verifier = CompileAndVerify(comp, expectedOutput: expectedResult);
                    verifier.VerifyIL("Program.Evaluate", expectedIL);
                }
            }
        }

        [Fact]
        public void UnaryOperators_UserDefined()
        {
            string sourceA =
@"namespace System
{
    public class Object { }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public struct Enum { }
    public class Attribute { }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets validOn) { }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
    }
    public enum AttributeTargets { }
    public struct IntPtr
    {
        public static IntPtr operator-(IntPtr i) => i;
    }
}";
            string sourceB =
@"class Program
{
    static System.IntPtr F1(System.IntPtr i) => -i;
    static nint F2(nint i) => -i;
}";
            var comp = CreateEmptyCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, emitOptions: EmitOptions.Default.WithRuntimeMetadataVersion("0.0.0.0"), verify: Verification.Skipped);
            verifier.VerifyIL("Program.F1",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.IntPtr System.IntPtr.op_UnaryNegation(System.IntPtr)""
  IL_0006:  ret
}");
            verifier.VerifyIL("Program.F2",
@"{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  neg
  IL_0002:  ret
}");
        }

        [Theory]
        [InlineData("nint")]
        [InlineData("nuint")]
        [InlineData("nint?")]
        [InlineData("nuint?")]
        public void UnaryAndBinaryOperators_UserDefinedConversions(string type)
        {
            string sourceA =
$@"class MyInt
{{
    public static implicit operator {type}(MyInt i) => throw null;
    public static implicit operator MyInt({type} i) => throw null;
}}";
            string sourceB =
@"class Program
{
    static void F(MyInt x, MyInt y)
    {
        ++x;
        x++;
        --x;
        x--;
        _ = +x;
        _ = -x;
        _ = ~x;
        _ = x + y;
        _ = x * y;
        _ = x < y;
        _ = x & y;
        _ = x << 1;
    }
}";
            var comp = CreateCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,9): error CS0023: Operator '++' cannot be applied to operand of type 'MyInt'
                //         ++x;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "++x").WithArguments("++", "MyInt").WithLocation(5, 9),
                // (6,9): error CS0023: Operator '++' cannot be applied to operand of type 'MyInt'
                //         x++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "x++").WithArguments("++", "MyInt").WithLocation(6, 9),
                // (7,9): error CS0023: Operator '--' cannot be applied to operand of type 'MyInt'
                //         --x;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "--x").WithArguments("--", "MyInt").WithLocation(7, 9),
                // (8,9): error CS0023: Operator '--' cannot be applied to operand of type 'MyInt'
                //         x--;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "x--").WithArguments("--", "MyInt").WithLocation(8, 9),
                // (9,13): error CS0023: Operator '+' cannot be applied to operand of type 'MyInt'
                //         _ = +x;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "+x").WithArguments("+", "MyInt").WithLocation(9, 13),
                // (10,13): error CS0023: Operator '-' cannot be applied to operand of type 'MyInt'
                //         _ = -x;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "-x").WithArguments("-", "MyInt").WithLocation(10, 13),
                // (11,13): error CS0023: Operator '~' cannot be applied to operand of type 'MyInt'
                //         _ = ~x;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "~x").WithArguments("~", "MyInt").WithLocation(11, 13),
                // (12,13): error CS0019: Operator '+' cannot be applied to operands of type 'MyInt' and 'MyInt'
                //         _ = x + y;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x + y").WithArguments("+", "MyInt", "MyInt").WithLocation(12, 13),
                // (13,13): error CS0019: Operator '*' cannot be applied to operands of type 'MyInt' and 'MyInt'
                //         _ = x * y;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x * y").WithArguments("*", "MyInt", "MyInt").WithLocation(13, 13),
                // (14,13): error CS0019: Operator '<' cannot be applied to operands of type 'MyInt' and 'MyInt'
                //         _ = x < y;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x < y").WithArguments("<", "MyInt", "MyInt").WithLocation(14, 13),
                // (15,13): error CS0019: Operator '&' cannot be applied to operands of type 'MyInt' and 'MyInt'
                //         _ = x & y;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x & y").WithArguments("&", "MyInt", "MyInt").WithLocation(15, 13),
                // (16,13): error CS0019: Operator '<<' cannot be applied to operands of type 'MyInt' and 'int'
                //         _ = x << 1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x << 1").WithArguments("<<", "MyInt", "int").WithLocation(16, 13));
        }

        [Fact]
        public void BinaryOperators()
        {
            void binaryOps(string op, string leftType, string rightType, string expectedSymbol1 = null, string expectedSymbol2 = "", DiagnosticDescription[] diagnostics1 = null, DiagnosticDescription[] diagnostics2 = null)
            {
                binaryOp(op, leftType, rightType, expectedSymbol1, diagnostics1);
                binaryOp(op, rightType, leftType, expectedSymbol2 == "" ? expectedSymbol1 : expectedSymbol2, diagnostics2 ?? diagnostics1);
            }

            void binaryOp(string op, string leftType, string rightType, string expectedSymbol, DiagnosticDescription[] diagnostics)
            {
                if (expectedSymbol == null && diagnostics == null)
                {
                    diagnostics = getBadBinaryOpsDiagnostics(op, leftType, rightType);
                }
                binaryOperator(op, leftType, rightType, expectedSymbol, diagnostics ?? Array.Empty<DiagnosticDescription>());
            }

            static DiagnosticDescription[] getBadBinaryOpsDiagnostics(string op, string leftType, string rightType, bool includeBadBinaryOps = true, bool includeVoidError = false)
            {
                var builder = ArrayBuilder<DiagnosticDescription>.GetInstance();
                if (includeBadBinaryOps) builder.Add(Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {op} y").WithArguments(op, leftType, rightType));
                if (includeVoidError) builder.Add(Diagnostic(ErrorCode.ERR_VoidError, $"x {op} y"));
                return builder.ToArrayAndFree();
            }

            static DiagnosticDescription[] getAmbiguousBinaryOpsDiagnostics(string op, string leftType, string rightType)
            {
                return new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {op} y").WithArguments(op, leftType, rightType) };
            }

            var arithmeticOperators = new[]
            {
                ("-", "op_Subtraction"),
                ("*", "op_Multiply"),
                ("/", "op_Division"),
                ("%", "op_Modulus"),
            };
            var additionOperators = new[]
            {
                ("+", "op_Addition"),
            };
            var comparisonOperators = new[]
            {
                ("<", "op_LessThan"),
                ("<=", "op_LessThanOrEqual"),
                (">", "op_GreaterThan"),
                (">=", "op_GreaterThanOrEqual"),
            };
            var shiftOperators = new[]
            {
                ("<<", "op_LeftShift"),
                (">>", "op_RightShift"),
            };
            var equalityOperators = new[]
            {
                ("==", "op_Equality"),
                ("!=", "op_Inequality"),
            };
            var logicalOperators = new[]
            {
                ("&", "op_BitwiseAnd"),
                ("|", "op_BitwiseOr"),
                ("^", "op_ExclusiveOr"),
            };

            foreach ((string symbol, string name) in arithmeticOperators)
            {
                bool includeBadBinaryOps = (symbol != "-");
                binaryOps(symbol, "nint", "object");
                binaryOps(symbol, "nint", "string");
                binaryOps(symbol, "nint", "void*", null, (symbol == "-") ? $"void* void*.{name}(void* left, long right)" : null, getBadBinaryOpsDiagnostics(symbol, "nint", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "nint", includeBadBinaryOps: includeBadBinaryOps, includeVoidError: true));
                binaryOps(symbol, "nint", "bool");
                binaryOps(symbol, "nint", "char", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "sbyte", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "byte", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "short", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "ushort", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "int", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "uint", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "nint", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "nuint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint"));
                binaryOps(symbol, "nint", "long", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "ulong", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "ulong"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong", "nint"));
                binaryOps(symbol, "nint", "float", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nint", "double", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nint", "decimal", $"decimal decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nint", "System.IntPtr", $"nint nint.{name}(nint left, System.IntPtr right)", $"nint System.IntPtr.{name}(System.IntPtr left, nint right)");
                binaryOps(symbol, "nint", "System.UIntPtr");
                binaryOps(symbol, "nint", "bool?");
                binaryOps(symbol, "nint", "char?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "sbyte?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "byte?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "short?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "ushort?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "int?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "uint?", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "nint?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "nuint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint"));
                binaryOps(symbol, "nint", "long?", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "ulong?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "ulong?"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong?", "nint"));
                binaryOps(symbol, "nint", "float?", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nint", "double?", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nint", "decimal?", $"decimal decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nint", "System.IntPtr?", $"nint nint.{name}(nint left, System.IntPtr right)", $"nint System.IntPtr.{name}(System.IntPtr left, nint right)");
                binaryOps(symbol, "nint", "System.UIntPtr?");
                binaryOps(symbol, "nint", "object");
                binaryOps(symbol, "nint?", "string");
                binaryOps(symbol, "nint?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nint?", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "nint?", includeVoidError: true));
                binaryOps(symbol, "nint?", "bool");
                binaryOps(symbol, "nint?", "char", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "sbyte", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "byte", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "short", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "ushort", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "int", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "uint", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "nint", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "nuint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint?"));
                binaryOps(symbol, "nint?", "long", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "ulong", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "ulong"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong", "nint?"));
                binaryOps(symbol, "nint?", "float", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nint?", "double", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nint?", "decimal", $"decimal decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nint?", "System.IntPtr", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "System.UIntPtr");
                binaryOps(symbol, "nint?", "bool?");
                binaryOps(symbol, "nint?", "char?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "sbyte?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "byte?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "short?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "ushort?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "int?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "uint?", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "nint?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "nuint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint?"));
                binaryOps(symbol, "nint?", "long?", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "ulong?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "ulong?"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong?", "nint?"));
                binaryOps(symbol, "nint?", "float?", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nint?", "double?", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nint?", "decimal?", $"decimal decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nint?", "System.IntPtr?", $"nint nint.{name}(nint left, System.IntPtr right)", $"nint System.IntPtr.{name}(System.IntPtr left, nint right)");
                binaryOps(symbol, "nint?", "System.UIntPtr?");
                binaryOps(symbol, "nuint", "object");
                binaryOps(symbol, "nuint", "string");
                binaryOps(symbol, "nuint", "void*", null, (symbol == "-") ? $"void* void*.{name}(void* left, ulong right)" : null, getBadBinaryOpsDiagnostics(symbol, "nuint", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "nuint", includeBadBinaryOps: includeBadBinaryOps, includeVoidError: true));
                binaryOps(symbol, "nuint", "bool");
                binaryOps(symbol, "nuint", "char", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "sbyte", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "byte", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "short", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "ushort", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "int", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "int"), getAmbiguousBinaryOpsDiagnostics(symbol, "int", "nuint"));
                binaryOps(symbol, "nuint", "uint", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "nint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint"));
                binaryOps(symbol, "nuint", "nuint", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "long", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "long"), getAmbiguousBinaryOpsDiagnostics(symbol, "long", "nuint"));
                binaryOps(symbol, "nuint", "ulong", $"ulong ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint", "float", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nuint", "double", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nuint", "decimal", $"decimal decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint", "System.IntPtr");
                binaryOps(symbol, "nuint", "System.UIntPtr", $"nuint nuint.{name}(nuint left, System.UIntPtr right)", $"nuint System.UIntPtr.{name}(System.UIntPtr left, nuint right)");
                binaryOps(symbol, "nuint", "bool?");
                binaryOps(symbol, "nuint", "char?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "sbyte?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "byte?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "short?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "ushort?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "int?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "int?"), getAmbiguousBinaryOpsDiagnostics(symbol, "int?", "nuint"));
                binaryOps(symbol, "nuint", "uint?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "nint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint"));
                binaryOps(symbol, "nuint", "nuint?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "long?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "long?"), getAmbiguousBinaryOpsDiagnostics(symbol, "long?", "nuint"));
                binaryOps(symbol, "nuint", "ulong?", $"ulong ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint", "float?", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nuint", "double?", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nuint", "decimal?", $"decimal decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint", "System.IntPtr?");
                binaryOps(symbol, "nuint", "System.UIntPtr?", $"nuint nuint.{name}(nuint left, System.UIntPtr right)", $"nuint System.UIntPtr.{name}(System.UIntPtr left, nuint right)");
                binaryOps(symbol, "nuint?", "object");
                binaryOps(symbol, "nuint?", "string");
                binaryOps(symbol, "nuint?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nuint?", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "nuint?", includeVoidError: true));
                binaryOps(symbol, "nuint?", "bool");
                binaryOps(symbol, "nuint?", "char", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "sbyte", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "byte", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "short", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "ushort", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "int", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "int"), getAmbiguousBinaryOpsDiagnostics(symbol, "int", "nuint?"));
                binaryOps(symbol, "nuint?", "uint", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "nint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint?"));
                binaryOps(symbol, "nuint?", "nuint", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "long", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "long"), getAmbiguousBinaryOpsDiagnostics(symbol, "long", "nuint?"));
                binaryOps(symbol, "nuint?", "ulong", $"ulong ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint?", "float", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nuint?", "double", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nuint?", "decimal", $"decimal decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint?", "System.IntPtr");
                binaryOps(symbol, "nuint?", "System.UIntPtr", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "bool?");
                binaryOps(symbol, "nuint?", "char?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "sbyte?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "byte?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "short?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "ushort?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "int?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "int?"), getAmbiguousBinaryOpsDiagnostics(symbol, "int?", "nuint?"));
                binaryOps(symbol, "nuint?", "uint?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "nint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint?"));
                binaryOps(symbol, "nuint?", "nuint?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "long?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "long?"), getAmbiguousBinaryOpsDiagnostics(symbol, "long?", "nuint?"));
                binaryOps(symbol, "nuint?", "ulong?", $"ulong ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint?", "float?", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nuint?", "double?", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nuint?", "decimal?", $"decimal decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint?", "System.IntPtr?");
                binaryOps(symbol, "nuint?", "System.UIntPtr?", $"nuint nuint.{name}(nuint left, System.UIntPtr right)", $"nuint System.UIntPtr.{name}(System.UIntPtr left, nuint right)");
                binaryOps(symbol, "System.IntPtr", "object");
                binaryOps(symbol, "System.IntPtr", "string");
                binaryOps(symbol, "System.IntPtr", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "System.IntPtr", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "System.IntPtr", includeVoidError: true));
                binaryOps(symbol, "System.IntPtr", "bool");
                binaryOps(symbol, "System.IntPtr", "char", (symbol == "-") ? $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.IntPtr", "sbyte", (symbol == "-") ? $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.IntPtr", "byte", (symbol == "-") ? $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.IntPtr", "short", (symbol == "-") ? $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.IntPtr", "ushort", (symbol == "-") ? $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.IntPtr", "int", (symbol == "-") ? $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.IntPtr", "uint");
                binaryOps(symbol, "System.IntPtr", "nint", $"nint System.IntPtr.{name}(System.IntPtr left, nint right)", $"nint nint.{name}(nint left, System.IntPtr right)");
                binaryOps(symbol, "System.IntPtr", "nuint");
                binaryOps(symbol, "System.IntPtr", "long");
                binaryOps(symbol, "System.IntPtr", "ulong");
                binaryOps(symbol, "System.IntPtr", "float");
                binaryOps(symbol, "System.IntPtr", "double");
                binaryOps(symbol, "System.IntPtr", "decimal");
                binaryOps(symbol, "System.IntPtr", "System.IntPtr");
                binaryOps(symbol, "System.IntPtr", "System.UIntPtr");
                binaryOps(symbol, "System.IntPtr", "bool?");
                binaryOps(symbol, "System.IntPtr", "char?", (symbol == "-") ? $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.IntPtr", "sbyte?", (symbol == "-") ? $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.IntPtr", "byte?", (symbol == "-") ? $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.IntPtr", "short?", (symbol == "-") ? $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.IntPtr", "ushort?", (symbol == "-") ? $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.IntPtr", "int?", (symbol == "-") ? $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.IntPtr", "uint?");
                binaryOps(symbol, "System.IntPtr", "nint?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "System.IntPtr", "nuint?");
                binaryOps(symbol, "System.IntPtr", "long?");
                binaryOps(symbol, "System.IntPtr", "ulong?");
                binaryOps(symbol, "System.IntPtr", "float?");
                binaryOps(symbol, "System.IntPtr", "double?");
                binaryOps(symbol, "System.IntPtr", "decimal?");
                binaryOps(symbol, "System.IntPtr", "System.IntPtr?");
                binaryOps(symbol, "System.IntPtr", "System.UIntPtr?");
                binaryOps(symbol, "System.IntPtr", "object");
                binaryOps(symbol, "System.IntPtr?", "string");
                binaryOps(symbol, "System.IntPtr?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "System.IntPtr?", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "System.IntPtr?", includeVoidError: true));
                binaryOps(symbol, "System.IntPtr?", "bool");
                binaryOps(symbol, "System.IntPtr?", "char", (symbol == "-") ? $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.IntPtr?", "sbyte", (symbol == "-") ? $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.IntPtr?", "byte", (symbol == "-") ? $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.IntPtr?", "short", (symbol == "-") ? $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.IntPtr?", "ushort", (symbol == "-") ? $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.IntPtr?", "int", (symbol == "-") ? $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.IntPtr?", "uint");
                binaryOps(symbol, "System.IntPtr?", "nint", $"nint System.IntPtr.{name}(System.IntPtr left, nint right)", $"nint nint.{name}(nint left, System.IntPtr right)");
                binaryOps(symbol, "System.IntPtr?", "nuint");
                binaryOps(symbol, "System.IntPtr?", "long");
                binaryOps(symbol, "System.IntPtr?", "ulong");
                binaryOps(symbol, "System.IntPtr?", "float");
                binaryOps(symbol, "System.IntPtr?", "double");
                binaryOps(symbol, "System.IntPtr?", "decimal");
                binaryOps(symbol, "System.IntPtr?", "System.IntPtr");
                binaryOps(symbol, "System.IntPtr?", "System.UIntPtr");
                binaryOps(symbol, "System.IntPtr?", "bool?");
                binaryOps(symbol, "System.IntPtr?", "char?", (symbol == "-") ? $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.IntPtr?", "sbyte?", (symbol == "-") ? $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.IntPtr?", "byte?", (symbol == "-") ? $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.IntPtr?", "short?", (symbol == "-") ? $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.IntPtr?", "ushort?", (symbol == "-") ? $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.IntPtr?", "int?", (symbol == "-") ? $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.IntPtr?", "uint?");
                binaryOps(symbol, "System.IntPtr?", "nint?", $"nint System.IntPtr.{name}(System.IntPtr left, nint right)", $"nint nint.{name}(nint left, System.IntPtr right)");
                binaryOps(symbol, "System.IntPtr?", "nuint?");
                binaryOps(symbol, "System.IntPtr?", "long?");
                binaryOps(symbol, "System.IntPtr?", "ulong?");
                binaryOps(symbol, "System.IntPtr?", "float?");
                binaryOps(symbol, "System.IntPtr?", "double?");
                binaryOps(symbol, "System.IntPtr?", "decimal?");
                binaryOps(symbol, "System.IntPtr?", "System.IntPtr?");
                binaryOps(symbol, "System.IntPtr?", "System.UIntPtr?");
                binaryOps(symbol, "System.UIntPtr", "object");
                binaryOps(symbol, "System.UIntPtr", "string");
                binaryOps(symbol, "System.UIntPtr", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "System.UIntPtr", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "System.UIntPtr", includeVoidError: true));
                binaryOps(symbol, "System.UIntPtr", "bool");
                binaryOps(symbol, "System.UIntPtr", "char", (symbol == "-") ? $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.UIntPtr", "sbyte", (symbol == "-") ? $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.UIntPtr", "byte", (symbol == "-") ? $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.UIntPtr", "short", (symbol == "-") ? $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.UIntPtr", "ushort", (symbol == "-") ? $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.UIntPtr", "int", (symbol == "-") ? $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.UIntPtr", "uint");
                binaryOps(symbol, "System.UIntPtr", "nint");
                binaryOps(symbol, "System.UIntPtr", "nuint", $"nuint System.UIntPtr.{name}(System.UIntPtr left, nuint right)", $"nuint nuint.{name}(nuint left, System.UIntPtr right)");
                binaryOps(symbol, "System.UIntPtr", "long");
                binaryOps(symbol, "System.UIntPtr", "ulong");
                binaryOps(symbol, "System.UIntPtr", "float");
                binaryOps(symbol, "System.UIntPtr", "double");
                binaryOps(symbol, "System.UIntPtr", "decimal");
                binaryOps(symbol, "System.UIntPtr", "System.IntPtr");
                binaryOps(symbol, "System.UIntPtr", "System.UIntPtr");
                binaryOps(symbol, "System.UIntPtr", "bool?");
                binaryOps(symbol, "System.UIntPtr", "char?", (symbol == "-") ? $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.UIntPtr", "sbyte?", (symbol == "-") ? $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.UIntPtr", "byte?", (symbol == "-") ? $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.UIntPtr", "short?", (symbol == "-") ? $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.UIntPtr", "ushort?", (symbol == "-") ? $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.UIntPtr", "int?", (symbol == "-") ? $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.UIntPtr", "uint?");
                binaryOps(symbol, "System.UIntPtr", "nint?");
                binaryOps(symbol, "System.UIntPtr", "nuint?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "System.UIntPtr", "long?");
                binaryOps(symbol, "System.UIntPtr", "ulong?");
                binaryOps(symbol, "System.UIntPtr", "float?");
                binaryOps(symbol, "System.UIntPtr", "double?");
                binaryOps(symbol, "System.UIntPtr", "decimal?");
                binaryOps(symbol, "System.UIntPtr", "System.IntPtr?");
                binaryOps(symbol, "System.UIntPtr", "System.UIntPtr?");
                binaryOps(symbol, "System.UIntPtr?", "object");
                binaryOps(symbol, "System.UIntPtr?", "string");
                binaryOps(symbol, "System.UIntPtr?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "System.UIntPtr?", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "System.UIntPtr?", includeVoidError: true));
                binaryOps(symbol, "System.UIntPtr?", "bool");
                binaryOps(symbol, "System.UIntPtr?", "char", (symbol == "-") ? $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.UIntPtr?", "sbyte", (symbol == "-") ? $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.UIntPtr?", "byte", (symbol == "-") ? $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.UIntPtr?", "short", (symbol == "-") ? $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.UIntPtr?", "ushort", (symbol == "-") ? $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.UIntPtr?", "int", (symbol == "-") ? $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.UIntPtr?", "uint");
                binaryOps(symbol, "System.UIntPtr?", "nint");
                binaryOps(symbol, "System.UIntPtr?", "nuint", $"nuint System.UIntPtr.{name}(System.UIntPtr left, nuint right)", $"nuint nuint.{name}(nuint left, System.UIntPtr right)");
                binaryOps(symbol, "System.UIntPtr?", "long");
                binaryOps(symbol, "System.UIntPtr?", "ulong");
                binaryOps(symbol, "System.UIntPtr?", "float");
                binaryOps(symbol, "System.UIntPtr?", "double");
                binaryOps(symbol, "System.UIntPtr?", "decimal");
                binaryOps(symbol, "System.UIntPtr?", "System.IntPtr");
                binaryOps(symbol, "System.UIntPtr?", "System.UIntPtr");
                binaryOps(symbol, "System.UIntPtr?", "bool?");
                binaryOps(symbol, "System.UIntPtr?", "char?", (symbol == "-") ? $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.UIntPtr?", "sbyte?", (symbol == "-") ? $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.UIntPtr?", "byte?", (symbol == "-") ? $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.UIntPtr?", "short?", (symbol == "-") ? $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.UIntPtr?", "ushort?", (symbol == "-") ? $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.UIntPtr?", "int?", (symbol == "-") ? $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)" : null, null);
                binaryOps(symbol, "System.UIntPtr?", "uint?");
                binaryOps(symbol, "System.UIntPtr?", "nint?");
                binaryOps(symbol, "System.UIntPtr?", "nuint?", $"nuint System.UIntPtr.{name}(System.UIntPtr left, nuint right)", $"nuint nuint.{name}(nuint left, System.UIntPtr right)");
                binaryOps(symbol, "System.UIntPtr?", "long?");
                binaryOps(symbol, "System.UIntPtr?", "ulong?");
                binaryOps(symbol, "System.UIntPtr?", "float?");
                binaryOps(symbol, "System.UIntPtr?", "double?");
                binaryOps(symbol, "System.UIntPtr?", "decimal?");
                binaryOps(symbol, "System.UIntPtr?", "System.IntPtr?");
                binaryOps(symbol, "System.UIntPtr?", "System.UIntPtr?");
            }

            foreach ((string symbol, string name) in comparisonOperators)
            {
                binaryOps(symbol, "nint", "object");
                binaryOps(symbol, "nint", "string");
                binaryOps(symbol, "nint", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nint", "void*"), getBadBinaryOpsDiagnostics(symbol, "void*", "nint"));
                binaryOps(symbol, "nint", "bool");
                binaryOps(symbol, "nint", "char", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "sbyte", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "byte", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "short", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "ushort", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "int", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "uint", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "nint", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "nuint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint"));
                binaryOps(symbol, "nint", "long", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "ulong", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "ulong"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong", "nint"));
                binaryOps(symbol, "nint", "float", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nint", "double", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nint", "decimal", $"bool decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nint", "System.IntPtr", $"bool nint.{name}(nint left, System.IntPtr right)", $"bool System.IntPtr.{name}(System.IntPtr left, nint right)");
                binaryOps(symbol, "nint", "System.UIntPtr");
                binaryOps(symbol, "nint", "bool?");
                binaryOps(symbol, "nint", "char?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "sbyte?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "byte?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "short?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "ushort?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "int?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "uint?", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "nint?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "nuint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint"));
                binaryOps(symbol, "nint", "long?", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "ulong?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "ulong?"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong?", "nint"));
                binaryOps(symbol, "nint", "float?", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nint", "double?", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nint", "decimal?", $"bool decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nint", "System.IntPtr?", $"bool nint.{name}(nint left, System.IntPtr right)", $"bool System.IntPtr.{name}(System.IntPtr left, nint right)");
                binaryOps(symbol, "nint", "System.UIntPtr?");
                binaryOps(symbol, "nint", "object");
                binaryOps(symbol, "nint?", "string");
                binaryOps(symbol, "nint?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nint?", "void*"), getBadBinaryOpsDiagnostics(symbol, "void*", "nint?"));
                binaryOps(symbol, "nint?", "bool");
                binaryOps(symbol, "nint?", "char", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "sbyte", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "byte", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "short", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "ushort", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "int", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "uint", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "nint", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "nuint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint?"));
                binaryOps(symbol, "nint?", "long", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "ulong", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "ulong"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong", "nint?"));
                binaryOps(symbol, "nint?", "float", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nint?", "double", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nint?", "decimal", $"bool decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nint?", "System.IntPtr", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "System.UIntPtr");
                binaryOps(symbol, "nint?", "bool?");
                binaryOps(symbol, "nint?", "char?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "sbyte?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "byte?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "short?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "ushort?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "int?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "uint?", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "nint?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "nuint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint?"));
                binaryOps(symbol, "nint?", "long?", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "ulong?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "ulong?"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong?", "nint?"));
                binaryOps(symbol, "nint?", "float?", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nint?", "double?", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nint?", "decimal?", $"bool decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nint?", "System.IntPtr?", $"bool nint.{name}(nint left, System.IntPtr right)", $"bool System.IntPtr.{name}(System.IntPtr left, nint right)");
                binaryOps(symbol, "nint?", "System.UIntPtr?");
                binaryOps(symbol, "nuint", "object");
                binaryOps(symbol, "nuint", "string");
                binaryOps(symbol, "nuint", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nuint", "void*"), getBadBinaryOpsDiagnostics(symbol, "void*", "nuint"));
                binaryOps(symbol, "nuint", "bool");
                binaryOps(symbol, "nuint", "char", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "sbyte", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "byte", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "short", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "ushort", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "int", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "int"), getAmbiguousBinaryOpsDiagnostics(symbol, "int", "nuint"));
                binaryOps(symbol, "nuint", "uint", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "nint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint"));
                binaryOps(symbol, "nuint", "nuint", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "long", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "long"), getAmbiguousBinaryOpsDiagnostics(symbol, "long", "nuint"));
                binaryOps(symbol, "nuint", "ulong", $"bool ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint", "float", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nuint", "double", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nuint", "decimal", $"bool decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint", "System.IntPtr");
                binaryOps(symbol, "nuint", "System.UIntPtr", $"bool nuint.{name}(nuint left, System.UIntPtr right)", $"bool System.UIntPtr.{name}(System.UIntPtr left, nuint right)");
                binaryOps(symbol, "nuint", "bool?");
                binaryOps(symbol, "nuint", "char?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "sbyte?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "byte?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "short?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "ushort?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "int?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "int?"), getAmbiguousBinaryOpsDiagnostics(symbol, "int?", "nuint"));
                binaryOps(symbol, "nuint", "uint?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "nint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint"));
                binaryOps(symbol, "nuint", "nuint?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "long?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "long?"), getAmbiguousBinaryOpsDiagnostics(symbol, "long?", "nuint"));
                binaryOps(symbol, "nuint", "ulong?", $"bool ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint", "float?", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nuint", "double?", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nuint", "decimal?", $"bool decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint", "System.IntPtr?");
                binaryOps(symbol, "nuint", "System.UIntPtr?", $"bool nuint.{name}(nuint left, System.UIntPtr right)", $"bool System.UIntPtr.{name}(System.UIntPtr left, nuint right)");
                binaryOps(symbol, "nuint?", "object");
                binaryOps(symbol, "nuint?", "string");
                binaryOps(symbol, "nuint?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nuint?", "void*"), getBadBinaryOpsDiagnostics(symbol, "void*", "nuint?"));
                binaryOps(symbol, "nuint?", "bool");
                binaryOps(symbol, "nuint?", "char", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "sbyte", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "byte", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "short", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "ushort", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "int", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "int"), getAmbiguousBinaryOpsDiagnostics(symbol, "int", "nuint?"));
                binaryOps(symbol, "nuint?", "uint", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "nint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint?"));
                binaryOps(symbol, "nuint?", "nuint", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "long", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "long"), getAmbiguousBinaryOpsDiagnostics(symbol, "long", "nuint?"));
                binaryOps(symbol, "nuint?", "ulong", $"bool ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint?", "float", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nuint?", "double", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nuint?", "decimal", $"bool decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint?", "System.IntPtr");
                binaryOps(symbol, "nuint?", "System.UIntPtr", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "bool?");
                binaryOps(symbol, "nuint?", "char?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "sbyte?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "byte?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "short?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "ushort?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "int?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "int?"), getAmbiguousBinaryOpsDiagnostics(symbol, "int?", "nuint?"));
                binaryOps(symbol, "nuint?", "uint?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "nint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint?"));
                binaryOps(symbol, "nuint?", "nuint?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "long?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "long?"), getAmbiguousBinaryOpsDiagnostics(symbol, "long?", "nuint?"));
                binaryOps(symbol, "nuint?", "ulong?", $"bool ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint?", "float?", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nuint?", "double?", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nuint?", "decimal?", $"bool decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint?", "System.IntPtr?");
                binaryOps(symbol, "nuint?", "System.UIntPtr?", $"bool nuint.{name}(nuint left, System.UIntPtr right)", $"bool System.UIntPtr.{name}(System.UIntPtr left, nuint right)");
                binaryOps(symbol, "System.IntPtr", "object");
                binaryOps(symbol, "System.IntPtr", "string");
                binaryOps(symbol, "System.IntPtr", "void*");
                binaryOps(symbol, "System.IntPtr", "bool");
                binaryOps(symbol, "System.IntPtr", "char");
                binaryOps(symbol, "System.IntPtr", "sbyte");
                binaryOps(symbol, "System.IntPtr", "byte");
                binaryOps(symbol, "System.IntPtr", "short");
                binaryOps(symbol, "System.IntPtr", "ushort");
                binaryOps(symbol, "System.IntPtr", "int");
                binaryOps(symbol, "System.IntPtr", "uint");
                binaryOps(symbol, "System.IntPtr", "nint", $"bool System.IntPtr.{name}(System.IntPtr left, nint right)", $"bool nint.{name}(nint left, System.IntPtr right)");
                binaryOps(symbol, "System.IntPtr", "nuint");
                binaryOps(symbol, "System.IntPtr", "long");
                binaryOps(symbol, "System.IntPtr", "ulong");
                binaryOps(symbol, "System.IntPtr", "float");
                binaryOps(symbol, "System.IntPtr", "double");
                binaryOps(symbol, "System.IntPtr", "decimal");
                binaryOps(symbol, "System.IntPtr", "System.IntPtr");
                binaryOps(symbol, "System.IntPtr", "System.UIntPtr");
                binaryOps(symbol, "System.IntPtr", "bool?");
                binaryOps(symbol, "System.IntPtr", "char?");
                binaryOps(symbol, "System.IntPtr", "sbyte?");
                binaryOps(symbol, "System.IntPtr", "byte?");
                binaryOps(symbol, "System.IntPtr", "short?");
                binaryOps(symbol, "System.IntPtr", "ushort?");
                binaryOps(symbol, "System.IntPtr", "int?");
                binaryOps(symbol, "System.IntPtr", "uint?");
                binaryOps(symbol, "System.IntPtr", "nint?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "System.IntPtr", "nuint?");
                binaryOps(symbol, "System.IntPtr", "long?");
                binaryOps(symbol, "System.IntPtr", "ulong?");
                binaryOps(symbol, "System.IntPtr", "float?");
                binaryOps(symbol, "System.IntPtr", "double?");
                binaryOps(symbol, "System.IntPtr", "decimal?");
                binaryOps(symbol, "System.IntPtr", "System.IntPtr?");
                binaryOps(symbol, "System.IntPtr", "System.UIntPtr?");
                binaryOps(symbol, "System.IntPtr", "object");
                binaryOps(symbol, "System.IntPtr?", "string");
                binaryOps(symbol, "System.IntPtr?", "void*");
                binaryOps(symbol, "System.IntPtr?", "bool");
                binaryOps(symbol, "System.IntPtr?", "char");
                binaryOps(symbol, "System.IntPtr?", "sbyte");
                binaryOps(symbol, "System.IntPtr?", "byte");
                binaryOps(symbol, "System.IntPtr?", "short");
                binaryOps(symbol, "System.IntPtr?", "ushort");
                binaryOps(symbol, "System.IntPtr?", "int");
                binaryOps(symbol, "System.IntPtr?", "uint");
                binaryOps(symbol, "System.IntPtr?", "nint", $"bool System.IntPtr.{name}(System.IntPtr left, nint right)", $"bool nint.{name}(nint left, System.IntPtr right)");
                binaryOps(symbol, "System.IntPtr?", "nuint");
                binaryOps(symbol, "System.IntPtr?", "long");
                binaryOps(symbol, "System.IntPtr?", "ulong");
                binaryOps(symbol, "System.IntPtr?", "float");
                binaryOps(symbol, "System.IntPtr?", "double");
                binaryOps(symbol, "System.IntPtr?", "decimal");
                binaryOps(symbol, "System.IntPtr?", "System.IntPtr");
                binaryOps(symbol, "System.IntPtr?", "System.UIntPtr");
                binaryOps(symbol, "System.IntPtr?", "bool?");
                binaryOps(symbol, "System.IntPtr?", "char?");
                binaryOps(symbol, "System.IntPtr?", "sbyte?");
                binaryOps(symbol, "System.IntPtr?", "byte?");
                binaryOps(symbol, "System.IntPtr?", "short?");
                binaryOps(symbol, "System.IntPtr?", "ushort?");
                binaryOps(symbol, "System.IntPtr?", "int?");
                binaryOps(symbol, "System.IntPtr?", "uint?");
                binaryOps(symbol, "System.IntPtr?", "nint?", $"bool System.IntPtr.{name}(System.IntPtr left, nint right)", $"bool nint.{name}(nint left, System.IntPtr right)");
                binaryOps(symbol, "System.IntPtr?", "nuint?");
                binaryOps(symbol, "System.IntPtr?", "long?");
                binaryOps(symbol, "System.IntPtr?", "ulong?");
                binaryOps(symbol, "System.IntPtr?", "float?");
                binaryOps(symbol, "System.IntPtr?", "double?");
                binaryOps(symbol, "System.IntPtr?", "decimal?");
                binaryOps(symbol, "System.IntPtr?", "System.IntPtr?");
                binaryOps(symbol, "System.IntPtr?", "System.UIntPtr?");
                binaryOps(symbol, "System.UIntPtr", "object");
                binaryOps(symbol, "System.UIntPtr", "string");
                binaryOps(symbol, "System.UIntPtr", "void*");
                binaryOps(symbol, "System.UIntPtr", "bool");
                binaryOps(symbol, "System.UIntPtr", "char");
                binaryOps(symbol, "System.UIntPtr", "sbyte");
                binaryOps(symbol, "System.UIntPtr", "byte");
                binaryOps(symbol, "System.UIntPtr", "short");
                binaryOps(symbol, "System.UIntPtr", "ushort");
                binaryOps(symbol, "System.UIntPtr", "int");
                binaryOps(symbol, "System.UIntPtr", "uint");
                binaryOps(symbol, "System.UIntPtr", "nint");
                binaryOps(symbol, "System.UIntPtr", "nuint", $"bool System.UIntPtr.{name}(System.UIntPtr left, nuint right)", $"bool nuint.{name}(nuint left, System.UIntPtr right)");
                binaryOps(symbol, "System.UIntPtr", "long");
                binaryOps(symbol, "System.UIntPtr", "ulong");
                binaryOps(symbol, "System.UIntPtr", "float");
                binaryOps(symbol, "System.UIntPtr", "double");
                binaryOps(symbol, "System.UIntPtr", "decimal");
                binaryOps(symbol, "System.UIntPtr", "System.IntPtr");
                binaryOps(symbol, "System.UIntPtr", "System.UIntPtr");
                binaryOps(symbol, "System.UIntPtr", "bool?");
                binaryOps(symbol, "System.UIntPtr", "char?");
                binaryOps(symbol, "System.UIntPtr", "sbyte?");
                binaryOps(symbol, "System.UIntPtr", "byte?");
                binaryOps(symbol, "System.UIntPtr", "short?");
                binaryOps(symbol, "System.UIntPtr", "ushort?");
                binaryOps(symbol, "System.UIntPtr", "int?");
                binaryOps(symbol, "System.UIntPtr", "uint?");
                binaryOps(symbol, "System.UIntPtr", "nint?");
                binaryOps(symbol, "System.UIntPtr", "nuint?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "System.UIntPtr", "long?");
                binaryOps(symbol, "System.UIntPtr", "ulong?");
                binaryOps(symbol, "System.UIntPtr", "float?");
                binaryOps(symbol, "System.UIntPtr", "double?");
                binaryOps(symbol, "System.UIntPtr", "decimal?");
                binaryOps(symbol, "System.UIntPtr", "System.IntPtr?");
                binaryOps(symbol, "System.UIntPtr", "System.UIntPtr?");
                binaryOps(symbol, "System.UIntPtr", "object");
                binaryOps(symbol, "System.UIntPtr?", "string");
                binaryOps(symbol, "System.UIntPtr?", "void*");
                binaryOps(symbol, "System.UIntPtr?", "bool");
                binaryOps(symbol, "System.UIntPtr?", "char");
                binaryOps(symbol, "System.UIntPtr?", "sbyte");
                binaryOps(symbol, "System.UIntPtr?", "byte");
                binaryOps(symbol, "System.UIntPtr?", "short");
                binaryOps(symbol, "System.UIntPtr?", "ushort");
                binaryOps(symbol, "System.UIntPtr?", "int");
                binaryOps(symbol, "System.UIntPtr?", "uint");
                binaryOps(symbol, "System.UIntPtr?", "nint");
                binaryOps(symbol, "System.UIntPtr?", "nuint", $"bool System.UIntPtr.{name}(System.UIntPtr left, nuint right)", $"bool nuint.{name}(nuint left, System.UIntPtr right)");
                binaryOps(symbol, "System.UIntPtr?", "long");
                binaryOps(symbol, "System.UIntPtr?", "ulong");
                binaryOps(symbol, "System.UIntPtr?", "float");
                binaryOps(symbol, "System.UIntPtr?", "double");
                binaryOps(symbol, "System.UIntPtr?", "decimal");
                binaryOps(symbol, "System.UIntPtr?", "System.IntPtr");
                binaryOps(symbol, "System.UIntPtr?", "System.UIntPtr");
                binaryOps(symbol, "System.UIntPtr?", "bool?");
                binaryOps(symbol, "System.UIntPtr?", "char?");
                binaryOps(symbol, "System.UIntPtr?", "sbyte?");
                binaryOps(symbol, "System.UIntPtr?", "byte?");
                binaryOps(symbol, "System.UIntPtr?", "short?");
                binaryOps(symbol, "System.UIntPtr?", "ushort?");
                binaryOps(symbol, "System.UIntPtr?", "int?");
                binaryOps(symbol, "System.UIntPtr?", "uint?");
                binaryOps(symbol, "System.UIntPtr?", "nint?");
                binaryOps(symbol, "System.UIntPtr?", "nuint?", $"bool System.UIntPtr.{name}(System.UIntPtr left, nuint right)", $"bool nuint.{name}(nuint left, System.UIntPtr right)");
                binaryOps(symbol, "System.UIntPtr?", "long?");
                binaryOps(symbol, "System.UIntPtr?", "ulong?");
                binaryOps(symbol, "System.UIntPtr?", "float?");
                binaryOps(symbol, "System.UIntPtr?", "double?");
                binaryOps(symbol, "System.UIntPtr?", "decimal?");
                binaryOps(symbol, "System.UIntPtr?", "System.IntPtr?");
                binaryOps(symbol, "System.UIntPtr?", "System.UIntPtr?");
            }

            foreach ((string symbol, string name) in additionOperators)
            {
                binaryOps(symbol, "nint", "object");
                binaryOps(symbol, "nint", "string", $"string string.{name}(object left, string right)", $"string string.{name}(string left, object right)");
                binaryOps(symbol, "nint", "void*", $"void* void*.{name}(long left, void* right)", $"void* void*.{name}(void* left, long right)", new[] { Diagnostic(ErrorCode.ERR_VoidError, "x + y") });
                binaryOps(symbol, "nint", "bool");
                binaryOps(symbol, "nint", "char", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "sbyte", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "byte", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "short", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "ushort", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "int", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "uint", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "nint", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "nuint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint"));
                binaryOps(symbol, "nint", "long", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "ulong", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "ulong"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong", "nint"));
                binaryOps(symbol, "nint", "float", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nint", "double", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nint", "decimal", $"decimal decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nint", "System.IntPtr", $"nint nint.{name}(nint left, System.IntPtr right)", $"nint System.IntPtr.{name}(System.IntPtr left, nint right)");
                binaryOps(symbol, "nint", "System.UIntPtr");
                binaryOps(symbol, "nint", "bool?");
                binaryOps(symbol, "nint", "char?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "sbyte?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "byte?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "short?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "ushort?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "int?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "uint?", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "nint?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "nuint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint"));
                binaryOps(symbol, "nint", "long?", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "ulong?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "ulong?"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong?", "nint"));
                binaryOps(symbol, "nint", "float?", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nint", "double?", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nint", "decimal?", $"decimal decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nint", "System.IntPtr?", $"nint nint.{name}(nint left, System.IntPtr right)", $"nint System.IntPtr.{name}(System.IntPtr left, nint right)");
                binaryOps(symbol, "nint", "System.UIntPtr?");
                binaryOps(symbol, "nint", "object");
                binaryOps(symbol, "nint?", "string", $"string string.{name}(object left, string right)", $"string string.{name}(string left, object right)");
                binaryOps(symbol, "nint?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nint?", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "nint?", includeVoidError: true));
                binaryOps(symbol, "nint?", "bool");
                binaryOps(symbol, "nint?", "char", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "sbyte", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "byte", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "short", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "ushort", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "int", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "uint", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "nint", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "nuint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint?"));
                binaryOps(symbol, "nint?", "long", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "ulong", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "ulong"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong", "nint?"));
                binaryOps(symbol, "nint?", "float", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nint?", "double", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nint?", "decimal", $"decimal decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nint?", "System.IntPtr", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "System.UIntPtr");
                binaryOps(symbol, "nint?", "bool?");
                binaryOps(symbol, "nint?", "char?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "sbyte?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "byte?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "short?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "ushort?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "int?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "uint?", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "nint?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "nuint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint?"));
                binaryOps(symbol, "nint?", "long?", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "ulong?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "ulong?"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong?", "nint?"));
                binaryOps(symbol, "nint?", "float?", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nint?", "double?", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nint?", "decimal?", $"decimal decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nint?", "System.IntPtr?", $"nint nint.{name}(nint left, System.IntPtr right)", $"nint System.IntPtr.{name}(System.IntPtr left, nint right)");
                binaryOps(symbol, "nint?", "System.UIntPtr?");
                binaryOps(symbol, "nuint", "object");
                binaryOps(symbol, "nuint", "string", $"string string.{name}(object left, string right)", $"string string.{name}(string left, object right)");
                binaryOps(symbol, "nuint", "void*", $"void* void*.{name}(ulong left, void* right)", $"void* void*.{name}(void* left, ulong right)", new[] { Diagnostic(ErrorCode.ERR_VoidError, "x + y") });
                binaryOps(symbol, "nuint", "bool");
                binaryOps(symbol, "nuint", "char", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "sbyte", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "byte", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "short", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "ushort", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "int", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "int"), getAmbiguousBinaryOpsDiagnostics(symbol, "int", "nuint"));
                binaryOps(symbol, "nuint", "uint", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "nint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint"));
                binaryOps(symbol, "nuint", "nuint", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "long", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "long"), getAmbiguousBinaryOpsDiagnostics(symbol, "long", "nuint"));
                binaryOps(symbol, "nuint", "ulong", $"ulong ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint", "float", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nuint", "double", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nuint", "decimal", $"decimal decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint", "System.IntPtr");
                binaryOps(symbol, "nuint", "System.UIntPtr", $"nuint nuint.{name}(nuint left, System.UIntPtr right)", $"nuint System.UIntPtr.{name}(System.UIntPtr left, nuint right)");
                binaryOps(symbol, "nuint", "bool?");
                binaryOps(symbol, "nuint", "char?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "sbyte?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "byte?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "short?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "ushort?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "int?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "int?"), getAmbiguousBinaryOpsDiagnostics(symbol, "int?", "nuint"));
                binaryOps(symbol, "nuint", "uint?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "nint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint"));
                binaryOps(symbol, "nuint", "nuint?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "long?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "long?"), getAmbiguousBinaryOpsDiagnostics(symbol, "long?", "nuint"));
                binaryOps(symbol, "nuint", "ulong?", $"ulong ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint", "float?", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nuint", "double?", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nuint", "decimal?", $"decimal decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint", "System.IntPtr?");
                binaryOps(symbol, "nuint", "System.UIntPtr?", $"nuint nuint.{name}(nuint left, System.UIntPtr right)", $"nuint System.UIntPtr.{name}(System.UIntPtr left, nuint right)");
                binaryOps(symbol, "nuint?", "object");
                binaryOps(symbol, "nuint?", "string", $"string string.{name}(object left, string right)", $"string string.{name}(string left, object right)");
                binaryOps(symbol, "nuint?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nuint?", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "nuint?", includeVoidError: true));
                binaryOps(symbol, "nuint?", "bool");
                binaryOps(symbol, "nuint?", "char", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "sbyte", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "byte", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "short", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "ushort", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "int", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "int"), getAmbiguousBinaryOpsDiagnostics(symbol, "int", "nuint?"));
                binaryOps(symbol, "nuint?", "uint", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "nint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint?"));
                binaryOps(symbol, "nuint?", "nuint", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "long", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "long"), getAmbiguousBinaryOpsDiagnostics(symbol, "long", "nuint?"));
                binaryOps(symbol, "nuint?", "ulong", $"ulong ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint?", "float", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nuint?", "double", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nuint?", "decimal", $"decimal decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint?", "System.IntPtr");
                binaryOps(symbol, "nuint?", "System.UIntPtr", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "bool?");
                binaryOps(symbol, "nuint?", "char?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "sbyte?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "byte?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "short?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "ushort?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "int?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "int?"), getAmbiguousBinaryOpsDiagnostics(symbol, "int?", "nuint?"));
                binaryOps(symbol, "nuint?", "uint?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "nint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint?"));
                binaryOps(symbol, "nuint?", "nuint?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "long?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "long?"), getAmbiguousBinaryOpsDiagnostics(symbol, "long?", "nuint?"));
                binaryOps(symbol, "nuint?", "ulong?", $"ulong ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint?", "float?", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nuint?", "double?", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nuint?", "decimal?", $"decimal decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint?", "System.IntPtr?");
                binaryOps(symbol, "nuint?", "System.UIntPtr?", $"nuint nuint.{name}(nuint left, System.UIntPtr right)", $"nuint System.UIntPtr.{name}(System.UIntPtr left, nuint right)");
                binaryOps(symbol, "System.IntPtr", "object");
                binaryOps(symbol, "System.IntPtr", "string", $"string string.{name}(object left, string right)", $"string string.{name}(string left, object right)");
                binaryOps(symbol, "System.IntPtr", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "System.IntPtr", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "System.IntPtr", includeVoidError: true));
                binaryOps(symbol, "System.IntPtr", "bool");
                binaryOps(symbol, "System.IntPtr", "char", $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.IntPtr", "sbyte", $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.IntPtr", "byte", $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.IntPtr", "short", $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.IntPtr", "ushort", $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.IntPtr", "int", $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.IntPtr", "uint");
                binaryOps(symbol, "System.IntPtr", "nint", $"nint System.IntPtr.{name}(System.IntPtr left, nint right)", $"nint nint.{name}(nint left, System.IntPtr right)");
                binaryOps(symbol, "System.IntPtr", "nuint");
                binaryOps(symbol, "System.IntPtr", "long");
                binaryOps(symbol, "System.IntPtr", "ulong");
                binaryOps(symbol, "System.IntPtr", "float");
                binaryOps(symbol, "System.IntPtr", "double");
                binaryOps(symbol, "System.IntPtr", "decimal");
                binaryOps(symbol, "System.IntPtr", "System.IntPtr");
                binaryOps(symbol, "System.IntPtr", "System.UIntPtr");
                binaryOps(symbol, "System.IntPtr", "bool?");
                binaryOps(symbol, "System.IntPtr", "char?", $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.IntPtr", "sbyte?", $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.IntPtr", "byte?", $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.IntPtr", "short?", $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.IntPtr", "ushort?", $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.IntPtr", "int?", $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.IntPtr", "uint?");
                binaryOps(symbol, "System.IntPtr", "nint?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "System.IntPtr", "nuint?");
                binaryOps(symbol, "System.IntPtr", "long?");
                binaryOps(symbol, "System.IntPtr", "ulong?");
                binaryOps(symbol, "System.IntPtr", "float?");
                binaryOps(symbol, "System.IntPtr", "double?");
                binaryOps(symbol, "System.IntPtr", "decimal?");
                binaryOps(symbol, "System.IntPtr", "System.IntPtr?");
                binaryOps(symbol, "System.IntPtr", "System.UIntPtr?");
                binaryOps(symbol, "System.IntPtr", "object");
                binaryOps(symbol, "System.IntPtr?", "string", $"string string.{name}(object left, string right)", $"string string.{name}(string left, object right)");
                binaryOps(symbol, "System.IntPtr?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "System.IntPtr?", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "System.IntPtr?", includeVoidError: true));
                binaryOps(symbol, "System.IntPtr?", "bool");
                binaryOps(symbol, "System.IntPtr?", "char", $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.IntPtr?", "sbyte", $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.IntPtr?", "byte", $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.IntPtr?", "short", $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.IntPtr?", "ushort", $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.IntPtr?", "int", $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.IntPtr?", "uint");
                binaryOps(symbol, "System.IntPtr?", "nint", $"nint System.IntPtr.{name}(System.IntPtr left, nint right)", $"nint nint.{name}(nint left, System.IntPtr right)");
                binaryOps(symbol, "System.IntPtr?", "nuint");
                binaryOps(symbol, "System.IntPtr?", "long");
                binaryOps(symbol, "System.IntPtr?", "ulong");
                binaryOps(symbol, "System.IntPtr?", "float");
                binaryOps(symbol, "System.IntPtr?", "double");
                binaryOps(symbol, "System.IntPtr?", "decimal");
                binaryOps(symbol, "System.IntPtr?", "System.IntPtr");
                binaryOps(symbol, "System.IntPtr?", "System.UIntPtr");
                binaryOps(symbol, "System.IntPtr?", "bool?");
                binaryOps(symbol, "System.IntPtr?", "char?", $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.IntPtr?", "sbyte?", $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.IntPtr?", "byte?", $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.IntPtr?", "short?", $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.IntPtr?", "ushort?", $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.IntPtr?", "int?", $"System.IntPtr System.IntPtr.{name}(System.IntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.IntPtr?", "uint?");
                binaryOps(symbol, "System.IntPtr?", "nint?", $"nint System.IntPtr.{name}(System.IntPtr left, nint right)", $"nint nint.{name}(nint left, System.IntPtr right)");
                binaryOps(symbol, "System.IntPtr?", "nuint?");
                binaryOps(symbol, "System.IntPtr?", "long?");
                binaryOps(symbol, "System.IntPtr?", "ulong?");
                binaryOps(symbol, "System.IntPtr?", "float?");
                binaryOps(symbol, "System.IntPtr?", "double?");
                binaryOps(symbol, "System.IntPtr?", "decimal?");
                binaryOps(symbol, "System.IntPtr?", "System.IntPtr?");
                binaryOps(symbol, "System.IntPtr?", "System.UIntPtr?");
                binaryOps(symbol, "System.UIntPtr", "object");
                binaryOps(symbol, "System.UIntPtr", "string", $"string string.{name}(object left, string right)", $"string string.{name}(string left, object right)");
                binaryOps(symbol, "System.UIntPtr", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "System.UIntPtr", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "System.UIntPtr", includeVoidError: true));
                binaryOps(symbol, "System.UIntPtr", "bool");
                binaryOps(symbol, "System.UIntPtr", "char", $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.UIntPtr", "sbyte", $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.UIntPtr", "byte", $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.UIntPtr", "short", $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.UIntPtr", "ushort", $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.UIntPtr", "int", $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.UIntPtr", "uint");
                binaryOps(symbol, "System.UIntPtr", "nint");
                binaryOps(symbol, "System.UIntPtr", "nuint", $"nuint System.UIntPtr.{name}(System.UIntPtr left, nuint right)", $"nuint nuint.{name}(nuint left, System.UIntPtr right)");
                binaryOps(symbol, "System.UIntPtr", "long");
                binaryOps(symbol, "System.UIntPtr", "ulong");
                binaryOps(symbol, "System.UIntPtr", "float");
                binaryOps(symbol, "System.UIntPtr", "double");
                binaryOps(symbol, "System.UIntPtr", "decimal");
                binaryOps(symbol, "System.UIntPtr", "System.IntPtr");
                binaryOps(symbol, "System.UIntPtr", "System.UIntPtr");
                binaryOps(symbol, "System.UIntPtr", "bool?");
                binaryOps(symbol, "System.UIntPtr", "char?", $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.UIntPtr", "sbyte?", $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.UIntPtr", "byte?", $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.UIntPtr", "short?", $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.UIntPtr", "ushort?", $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.UIntPtr", "int?", $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.UIntPtr", "uint?");
                binaryOps(symbol, "System.UIntPtr", "nint?");
                binaryOps(symbol, "System.UIntPtr", "nuint?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "System.UIntPtr", "long?");
                binaryOps(symbol, "System.UIntPtr", "ulong?");
                binaryOps(symbol, "System.UIntPtr", "float?");
                binaryOps(symbol, "System.UIntPtr", "double?");
                binaryOps(symbol, "System.UIntPtr", "decimal?");
                binaryOps(symbol, "System.UIntPtr", "System.IntPtr?");
                binaryOps(symbol, "System.UIntPtr", "System.UIntPtr?");
                binaryOps(symbol, "System.UIntPtr", "object");
                binaryOps(symbol, "System.UIntPtr?", "string", $"string string.{name}(object left, string right)", $"string string.{name}(string left, object right)");
                binaryOps(symbol, "System.UIntPtr?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "System.UIntPtr?", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "System.UIntPtr?", includeVoidError: true));
                binaryOps(symbol, "System.UIntPtr?", "bool");
                binaryOps(symbol, "System.UIntPtr?", "char", $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.UIntPtr?", "sbyte", $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.UIntPtr?", "byte", $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.UIntPtr?", "short", $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.UIntPtr?", "ushort", $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.UIntPtr?", "int", $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.UIntPtr?", "uint");
                binaryOps(symbol, "System.UIntPtr?", "nint");
                binaryOps(symbol, "System.UIntPtr?", "nuint", $"nuint System.UIntPtr.{name}(System.UIntPtr left, nuint right)", $"nuint nuint.{name}(nuint left, System.UIntPtr right)");
                binaryOps(symbol, "System.UIntPtr?", "long");
                binaryOps(symbol, "System.UIntPtr?", "ulong");
                binaryOps(symbol, "System.UIntPtr?", "float");
                binaryOps(symbol, "System.UIntPtr?", "double");
                binaryOps(symbol, "System.UIntPtr?", "decimal");
                binaryOps(symbol, "System.UIntPtr?", "System.IntPtr");
                binaryOps(symbol, "System.UIntPtr?", "System.UIntPtr");
                binaryOps(symbol, "System.UIntPtr?", "bool?");
                binaryOps(symbol, "System.UIntPtr?", "char?", $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.UIntPtr?", "sbyte?", $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.UIntPtr?", "byte?", $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.UIntPtr?", "short?", $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.UIntPtr?", "ushort?", $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.UIntPtr?", "int?", $"System.UIntPtr System.UIntPtr.{name}(System.UIntPtr pointer, int offset)", null);
                binaryOps(symbol, "System.UIntPtr?", "uint?");
                binaryOps(symbol, "System.UIntPtr?", "nint?");
                binaryOps(symbol, "System.UIntPtr?", "nuint?", $"nuint System.UIntPtr.{name}(System.UIntPtr left, nuint right)", $"nuint nuint.{name}(nuint left, System.UIntPtr right)");
                binaryOps(symbol, "System.UIntPtr?", "long?");
                binaryOps(symbol, "System.UIntPtr?", "ulong?");
                binaryOps(symbol, "System.UIntPtr?", "float?");
                binaryOps(symbol, "System.UIntPtr?", "double?");
                binaryOps(symbol, "System.UIntPtr?", "decimal?");
                binaryOps(symbol, "System.UIntPtr?", "System.IntPtr?");
                binaryOps(symbol, "System.UIntPtr?", "System.UIntPtr?");
            }

            foreach ((string symbol, string name) in shiftOperators)
            {
                binaryOps(symbol, "nint", "object");
                binaryOps(symbol, "nint", "string");
                binaryOps(symbol, "nint", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nint", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "nint", includeVoidError: true));
                binaryOps(symbol, "nint", "bool");
                binaryOps(symbol, "nint", "char", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint", "sbyte", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint", "byte", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint", "short", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint", "ushort", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint", "int", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint", "uint");
                binaryOps(symbol, "nint", "nint");
                binaryOps(symbol, "nint", "nuint");
                binaryOps(symbol, "nint", "long");
                binaryOps(symbol, "nint", "ulong");
                binaryOps(symbol, "nint", "float");
                binaryOps(symbol, "nint", "double");
                binaryOps(symbol, "nint", "decimal");
                binaryOps(symbol, "nint", "System.IntPtr");
                binaryOps(symbol, "nint", "System.UIntPtr");
                binaryOps(symbol, "nint", "bool?");
                binaryOps(symbol, "nint", "char?", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint", "sbyte?", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint", "byte?", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint", "short?", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint", "ushort?", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint", "int?", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint", "uint?");
                binaryOps(symbol, "nint", "nint?");
                binaryOps(symbol, "nint", "nuint?");
                binaryOps(symbol, "nint", "long?");
                binaryOps(symbol, "nint", "ulong?");
                binaryOps(symbol, "nint", "float?");
                binaryOps(symbol, "nint", "double?");
                binaryOps(symbol, "nint", "decimal?");
                binaryOps(symbol, "nint", "System.IntPtr?");
                binaryOps(symbol, "nint", "System.UIntPtr?");
                binaryOps(symbol, "nint", "object");
                binaryOps(symbol, "nint?", "string");
                binaryOps(symbol, "nint?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nint?", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "nint?", includeVoidError: true));
                binaryOps(symbol, "nint?", "bool");
                binaryOps(symbol, "nint?", "char", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint?", "sbyte", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint?", "byte", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint?", "short", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint?", "ushort", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint?", "int", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint?", "uint");
                binaryOps(symbol, "nint?", "nint");
                binaryOps(symbol, "nint?", "nuint");
                binaryOps(symbol, "nint?", "long");
                binaryOps(symbol, "nint?", "ulong");
                binaryOps(symbol, "nint?", "float");
                binaryOps(symbol, "nint?", "double");
                binaryOps(symbol, "nint?", "decimal");
                binaryOps(symbol, "nint?", "System.IntPtr");
                binaryOps(symbol, "nint?", "System.UIntPtr");
                binaryOps(symbol, "nint?", "bool?");
                binaryOps(symbol, "nint?", "char?", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint?", "sbyte?", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint?", "byte?", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint?", "short?", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint?", "ushort?", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint?", "int?", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint?", "uint?");
                binaryOps(symbol, "nint?", "nint?");
                binaryOps(symbol, "nint?", "nuint?");
                binaryOps(symbol, "nint?", "long?");
                binaryOps(symbol, "nint?", "ulong?");
                binaryOps(symbol, "nint?", "float?");
                binaryOps(symbol, "nint?", "double?");
                binaryOps(symbol, "nint?", "decimal?");
                binaryOps(symbol, "nint?", "System.IntPtr?");
                binaryOps(symbol, "nint?", "System.UIntPtr?");
                binaryOps(symbol, "nuint", "object");
                binaryOps(symbol, "nuint", "string");
                binaryOps(symbol, "nuint", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nuint", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "nuint", includeVoidError: true));
                binaryOps(symbol, "nuint", "bool");
                binaryOps(symbol, "nuint", "char", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint", "sbyte", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint", "byte", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint", "short", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint", "ushort", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint", "int", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint", "uint");
                binaryOps(symbol, "nuint", "nint");
                binaryOps(symbol, "nuint", "nuint");
                binaryOps(symbol, "nuint", "long");
                binaryOps(symbol, "nuint", "ulong");
                binaryOps(symbol, "nuint", "float");
                binaryOps(symbol, "nuint", "double");
                binaryOps(symbol, "nuint", "decimal");
                binaryOps(symbol, "nuint", "System.IntPtr");
                binaryOps(symbol, "nuint", "System.UIntPtr");
                binaryOps(symbol, "nuint", "bool?");
                binaryOps(symbol, "nuint", "char?", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint", "sbyte?", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint", "byte?", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint", "short?", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint", "ushort?", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint", "int?", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint", "uint?");
                binaryOps(symbol, "nuint", "nint?");
                binaryOps(symbol, "nuint", "nuint?");
                binaryOps(symbol, "nuint", "long?");
                binaryOps(symbol, "nuint", "ulong?");
                binaryOps(symbol, "nuint", "float?");
                binaryOps(symbol, "nuint", "double?");
                binaryOps(symbol, "nuint", "decimal?");
                binaryOps(symbol, "nuint", "System.IntPtr?");
                binaryOps(symbol, "nuint", "System.UIntPtr?");
                binaryOps(symbol, "nuint?", "object");
                binaryOps(symbol, "nuint?", "string");
                binaryOps(symbol, "nuint?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nuint?", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "nuint?", includeVoidError: true));
                binaryOps(symbol, "nuint?", "bool");
                binaryOps(symbol, "nuint?", "char", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint?", "sbyte", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint?", "byte", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint?", "short", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint?", "ushort", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint?", "int", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint?", "uint");
                binaryOps(symbol, "nuint?", "nint");
                binaryOps(symbol, "nuint?", "nuint");
                binaryOps(symbol, "nuint?", "long");
                binaryOps(symbol, "nuint?", "ulong");
                binaryOps(symbol, "nuint?", "float");
                binaryOps(symbol, "nuint?", "double");
                binaryOps(symbol, "nuint?", "decimal");
                binaryOps(symbol, "nuint?", "System.IntPtr");
                binaryOps(symbol, "nuint?", "System.UIntPtr");
                binaryOps(symbol, "nuint?", "bool?");
                binaryOps(symbol, "nuint?", "char?", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint?", "sbyte?", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint?", "byte?", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint?", "short?", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint?", "ushort?", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint?", "int?", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint?", "uint?");
                binaryOps(symbol, "nuint?", "nint?");
                binaryOps(symbol, "nuint?", "nuint?");
                binaryOps(symbol, "nuint?", "long?");
                binaryOps(symbol, "nuint?", "ulong?");
                binaryOps(symbol, "nuint?", "float?");
                binaryOps(symbol, "nuint?", "double?");
                binaryOps(symbol, "nuint?", "decimal?");
                binaryOps(symbol, "nuint?", "System.IntPtr?");
                binaryOps(symbol, "nuint?", "System.UIntPtr?");
                binaryOps(symbol, "System.IntPtr", "object");
                binaryOps(symbol, "System.IntPtr", "string");
                binaryOps(symbol, "System.IntPtr", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "System.IntPtr", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "System.IntPtr", includeVoidError: true));
                binaryOps(symbol, "System.IntPtr", "bool");
                binaryOps(symbol, "System.IntPtr", "char");
                binaryOps(symbol, "System.IntPtr", "sbyte");
                binaryOps(symbol, "System.IntPtr", "byte");
                binaryOps(symbol, "System.IntPtr", "short");
                binaryOps(symbol, "System.IntPtr", "ushort");
                binaryOps(symbol, "System.IntPtr", "int");
                binaryOps(symbol, "System.IntPtr", "uint");
                binaryOps(symbol, "System.IntPtr", "nint");
                binaryOps(symbol, "System.IntPtr", "nuint");
                binaryOps(symbol, "System.IntPtr", "long");
                binaryOps(symbol, "System.IntPtr", "ulong");
                binaryOps(symbol, "System.IntPtr", "float");
                binaryOps(symbol, "System.IntPtr", "double");
                binaryOps(symbol, "System.IntPtr", "decimal");
                binaryOps(symbol, "System.IntPtr", "System.IntPtr");
                binaryOps(symbol, "System.IntPtr", "System.UIntPtr");
                binaryOps(symbol, "System.IntPtr", "bool?");
                binaryOps(symbol, "System.IntPtr", "char?");
                binaryOps(symbol, "System.IntPtr", "sbyte?");
                binaryOps(symbol, "System.IntPtr", "byte?");
                binaryOps(symbol, "System.IntPtr", "short?");
                binaryOps(symbol, "System.IntPtr", "ushort?");
                binaryOps(symbol, "System.IntPtr", "int?");
                binaryOps(symbol, "System.IntPtr", "uint?");
                binaryOps(symbol, "System.IntPtr", "nint?");
                binaryOps(symbol, "System.IntPtr", "nuint?");
                binaryOps(symbol, "System.IntPtr", "long?");
                binaryOps(symbol, "System.IntPtr", "ulong?");
                binaryOps(symbol, "System.IntPtr", "float?");
                binaryOps(symbol, "System.IntPtr", "double?");
                binaryOps(symbol, "System.IntPtr", "decimal?");
                binaryOps(symbol, "System.IntPtr", "System.IntPtr?");
                binaryOps(symbol, "System.IntPtr", "System.UIntPtr?");
                binaryOps(symbol, "System.IntPtr", "object");
                binaryOps(symbol, "System.IntPtr?", "string");
                binaryOps(symbol, "System.IntPtr?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "System.IntPtr?", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "System.IntPtr?", includeVoidError: true));
                binaryOps(symbol, "System.IntPtr?", "bool");
                binaryOps(symbol, "System.IntPtr?", "char");
                binaryOps(symbol, "System.IntPtr?", "sbyte");
                binaryOps(symbol, "System.IntPtr?", "byte");
                binaryOps(symbol, "System.IntPtr?", "short");
                binaryOps(symbol, "System.IntPtr?", "ushort");
                binaryOps(symbol, "System.IntPtr?", "int");
                binaryOps(symbol, "System.IntPtr?", "uint");
                binaryOps(symbol, "System.IntPtr?", "nint");
                binaryOps(symbol, "System.IntPtr?", "nuint");
                binaryOps(symbol, "System.IntPtr?", "long");
                binaryOps(symbol, "System.IntPtr?", "ulong");
                binaryOps(symbol, "System.IntPtr?", "float");
                binaryOps(symbol, "System.IntPtr?", "double");
                binaryOps(symbol, "System.IntPtr?", "decimal");
                binaryOps(symbol, "System.IntPtr?", "System.IntPtr");
                binaryOps(symbol, "System.IntPtr?", "System.UIntPtr");
                binaryOps(symbol, "System.IntPtr?", "bool?");
                binaryOps(symbol, "System.IntPtr?", "char?");
                binaryOps(symbol, "System.IntPtr?", "sbyte?");
                binaryOps(symbol, "System.IntPtr?", "byte?");
                binaryOps(symbol, "System.IntPtr?", "short?");
                binaryOps(symbol, "System.IntPtr?", "ushort?");
                binaryOps(symbol, "System.IntPtr?", "int?");
                binaryOps(symbol, "System.IntPtr?", "uint?");
                binaryOps(symbol, "System.IntPtr?", "nint?");
                binaryOps(symbol, "System.IntPtr?", "nuint?");
                binaryOps(symbol, "System.IntPtr?", "long?");
                binaryOps(symbol, "System.IntPtr?", "ulong?");
                binaryOps(symbol, "System.IntPtr?", "float?");
                binaryOps(symbol, "System.IntPtr?", "double?");
                binaryOps(symbol, "System.IntPtr?", "decimal?");
                binaryOps(symbol, "System.IntPtr?", "System.IntPtr?");
                binaryOps(symbol, "System.IntPtr?", "System.UIntPtr?");
                binaryOps(symbol, "System.UIntPtr", "object");
                binaryOps(symbol, "System.UIntPtr", "string");
                binaryOps(symbol, "System.UIntPtr", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "System.UIntPtr", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "System.UIntPtr", includeVoidError: true));
                binaryOps(symbol, "System.UIntPtr", "bool");
                binaryOps(symbol, "System.UIntPtr", "char");
                binaryOps(symbol, "System.UIntPtr", "sbyte");
                binaryOps(symbol, "System.UIntPtr", "byte");
                binaryOps(symbol, "System.UIntPtr", "short");
                binaryOps(symbol, "System.UIntPtr", "ushort");
                binaryOps(symbol, "System.UIntPtr", "int");
                binaryOps(symbol, "System.UIntPtr", "uint");
                binaryOps(symbol, "System.UIntPtr", "nint");
                binaryOps(symbol, "System.UIntPtr", "nuint");
                binaryOps(symbol, "System.UIntPtr", "long");
                binaryOps(symbol, "System.UIntPtr", "ulong");
                binaryOps(symbol, "System.UIntPtr", "float");
                binaryOps(symbol, "System.UIntPtr", "double");
                binaryOps(symbol, "System.UIntPtr", "decimal");
                binaryOps(symbol, "System.UIntPtr", "System.IntPtr");
                binaryOps(symbol, "System.UIntPtr", "System.UIntPtr");
                binaryOps(symbol, "System.UIntPtr", "bool?");
                binaryOps(symbol, "System.UIntPtr", "char?");
                binaryOps(symbol, "System.UIntPtr", "sbyte?");
                binaryOps(symbol, "System.UIntPtr", "byte?");
                binaryOps(symbol, "System.UIntPtr", "short?");
                binaryOps(symbol, "System.UIntPtr", "ushort?");
                binaryOps(symbol, "System.UIntPtr", "int?");
                binaryOps(symbol, "System.UIntPtr", "uint?");
                binaryOps(symbol, "System.UIntPtr", "nint?");
                binaryOps(symbol, "System.UIntPtr", "nuint?");
                binaryOps(symbol, "System.UIntPtr", "long?");
                binaryOps(symbol, "System.UIntPtr", "ulong?");
                binaryOps(symbol, "System.UIntPtr", "float?");
                binaryOps(symbol, "System.UIntPtr", "double?");
                binaryOps(symbol, "System.UIntPtr", "decimal?");
                binaryOps(symbol, "System.UIntPtr", "System.IntPtr?");
                binaryOps(symbol, "System.UIntPtr", "System.UIntPtr?");
                binaryOps(symbol, "System.UIntPtr", "object");
                binaryOps(symbol, "System.UIntPtr?", "string");
                binaryOps(symbol, "System.UIntPtr?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "System.UIntPtr?", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "System.UIntPtr?", includeVoidError: true));
                binaryOps(symbol, "System.UIntPtr?", "bool");
                binaryOps(symbol, "System.UIntPtr?", "char");
                binaryOps(symbol, "System.UIntPtr?", "sbyte");
                binaryOps(symbol, "System.UIntPtr?", "byte");
                binaryOps(symbol, "System.UIntPtr?", "short");
                binaryOps(symbol, "System.UIntPtr?", "ushort");
                binaryOps(symbol, "System.UIntPtr?", "int");
                binaryOps(symbol, "System.UIntPtr?", "uint");
                binaryOps(symbol, "System.UIntPtr?", "nint");
                binaryOps(symbol, "System.UIntPtr?", "nuint");
                binaryOps(symbol, "System.UIntPtr?", "long");
                binaryOps(symbol, "System.UIntPtr?", "ulong");
                binaryOps(symbol, "System.UIntPtr?", "float");
                binaryOps(symbol, "System.UIntPtr?", "double");
                binaryOps(symbol, "System.UIntPtr?", "decimal");
                binaryOps(symbol, "System.UIntPtr?", "System.IntPtr");
                binaryOps(symbol, "System.UIntPtr?", "System.UIntPtr");
                binaryOps(symbol, "System.UIntPtr?", "bool?");
                binaryOps(symbol, "System.UIntPtr?", "char?");
                binaryOps(symbol, "System.UIntPtr?", "sbyte?");
                binaryOps(symbol, "System.UIntPtr?", "byte?");
                binaryOps(symbol, "System.UIntPtr?", "short?");
                binaryOps(symbol, "System.UIntPtr?", "ushort?");
                binaryOps(symbol, "System.UIntPtr?", "int?");
                binaryOps(symbol, "System.UIntPtr?", "uint?");
                binaryOps(symbol, "System.UIntPtr?", "nint?");
                binaryOps(symbol, "System.UIntPtr?", "nuint?");
                binaryOps(symbol, "System.UIntPtr?", "long?");
                binaryOps(symbol, "System.UIntPtr?", "ulong?");
                binaryOps(symbol, "System.UIntPtr?", "float?");
                binaryOps(symbol, "System.UIntPtr?", "double?");
                binaryOps(symbol, "System.UIntPtr?", "decimal?");
                binaryOps(symbol, "System.UIntPtr?", "System.IntPtr?");
                binaryOps(symbol, "System.UIntPtr?", "System.UIntPtr?");
            }

            foreach ((string symbol, string name) in equalityOperators)
            {
                binaryOps(symbol, "nint", "object");
                binaryOps(symbol, "nint", "string");
                binaryOps(symbol, "nint", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nint", "void*"), getBadBinaryOpsDiagnostics(symbol, "void*", "nint"));
                binaryOps(symbol, "nint", "bool");
                binaryOps(symbol, "nint", "char", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "sbyte", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "byte", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "short", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "ushort", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "int", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "uint", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "nint", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "nuint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint"));
                binaryOps(symbol, "nint", "long", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "ulong", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "ulong"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong", "nint"));
                binaryOps(symbol, "nint", "float", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nint", "double", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nint", "decimal", $"bool decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nint", "System.IntPtr", $"bool nint.{name}(nint left, System.IntPtr right)", $"bool System.IntPtr.{name}(System.IntPtr value1, System.IntPtr value2)");
                binaryOps(symbol, "nint", "System.UIntPtr");
                binaryOps(symbol, "nint", "bool?");
                binaryOps(symbol, "nint", "char?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "sbyte?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "byte?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "short?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "ushort?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "int?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "uint?", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "nint?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "nuint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint"));
                binaryOps(symbol, "nint", "long?", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "ulong?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "ulong?"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong?", "nint"));
                binaryOps(symbol, "nint", "float?", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nint", "double?", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nint", "decimal?", $"bool decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nint", "System.IntPtr?", $"bool nint.{name}(nint left, System.IntPtr right)", $"bool System.IntPtr.{name}(System.IntPtr value1, System.IntPtr value2)");
                binaryOps(symbol, "nint", "System.UIntPtr?");
                binaryOps(symbol, "nint", "object");
                binaryOps(symbol, "nint?", "string");
                binaryOps(symbol, "nint?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nint?", "void*"), getBadBinaryOpsDiagnostics(symbol, "void*", "nint?"));
                binaryOps(symbol, "nint?", "bool");
                binaryOps(symbol, "nint?", "char", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "sbyte", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "byte", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "short", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "ushort", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "int", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "uint", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "nint", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "nuint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint?"));
                binaryOps(symbol, "nint?", "long", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "ulong", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "ulong"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong", "nint?"));
                binaryOps(symbol, "nint?", "float", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nint?", "double", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nint?", "decimal", $"bool decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nint?", "System.IntPtr", $"bool nint.{name}(nint left, nint right)", $"bool System.IntPtr.{name}(System.IntPtr value1, System.IntPtr value2)");
                binaryOps(symbol, "nint?", "System.UIntPtr");
                binaryOps(symbol, "nint?", "bool?");
                binaryOps(symbol, "nint?", "char?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "sbyte?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "byte?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "short?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "ushort?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "int?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "uint?", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "nint?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "nuint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint?"));
                binaryOps(symbol, "nint?", "long?", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "ulong?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "ulong?"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong?", "nint?"));
                binaryOps(symbol, "nint?", "float?", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nint?", "double?", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nint?", "decimal?", $"bool decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nint?", "System.IntPtr?", $"bool nint.{name}(nint left, System.IntPtr right)", $"bool System.IntPtr.{name}(System.IntPtr value1, System.IntPtr value2)");
                binaryOps(symbol, "nint?", "System.UIntPtr?");
                binaryOps(symbol, "nuint", "object");
                binaryOps(symbol, "nuint", "string");
                binaryOps(symbol, "nuint", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nuint", "void*"), getBadBinaryOpsDiagnostics(symbol, "void*", "nuint"));
                binaryOps(symbol, "nuint", "bool");
                binaryOps(symbol, "nuint", "char", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "sbyte", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "byte", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "short", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "ushort", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "int", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "int"), getAmbiguousBinaryOpsDiagnostics(symbol, "int", "nuint"));
                binaryOps(symbol, "nuint", "uint", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "nint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint"));
                binaryOps(symbol, "nuint", "nuint", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "long", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "long"), getAmbiguousBinaryOpsDiagnostics(symbol, "long", "nuint"));
                binaryOps(symbol, "nuint", "ulong", $"bool ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint", "float", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nuint", "double", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nuint", "decimal", $"bool decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint", "System.IntPtr");
                binaryOps(symbol, "nuint", "System.UIntPtr", $"bool nuint.{name}(nuint left, System.UIntPtr right)", $"bool System.UIntPtr.{name}(System.UIntPtr value1, System.UIntPtr value2)");
                binaryOps(symbol, "nuint", "bool?");
                binaryOps(symbol, "nuint", "char?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "sbyte?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "byte?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "short?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "ushort?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "int?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "int?"), getAmbiguousBinaryOpsDiagnostics(symbol, "int?", "nuint"));
                binaryOps(symbol, "nuint", "uint?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "nint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint"));
                binaryOps(symbol, "nuint", "nuint?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "long?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "long?"), getAmbiguousBinaryOpsDiagnostics(symbol, "long?", "nuint"));
                binaryOps(symbol, "nuint", "ulong?", $"bool ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint", "float?", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nuint", "double?", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nuint", "decimal?", $"bool decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint", "System.IntPtr?");
                binaryOps(symbol, "nuint", "System.UIntPtr?", $"bool nuint.{name}(nuint left, System.UIntPtr right)", $"bool System.UIntPtr.{name}(System.UIntPtr value1, System.UIntPtr value2)");
                binaryOps(symbol, "nuint?", "object");
                binaryOps(symbol, "nuint?", "string");
                binaryOps(symbol, "nuint?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nuint?", "void*"), getBadBinaryOpsDiagnostics(symbol, "void*", "nuint?"));
                binaryOps(symbol, "nuint?", "bool");
                binaryOps(symbol, "nuint?", "char", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "sbyte", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "byte", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "short", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "ushort", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "int", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "int"), getAmbiguousBinaryOpsDiagnostics(symbol, "int", "nuint?"));
                binaryOps(symbol, "nuint?", "uint", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "nint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint?"));
                binaryOps(symbol, "nuint?", "nuint", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "long", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "long"), getAmbiguousBinaryOpsDiagnostics(symbol, "long", "nuint?"));
                binaryOps(symbol, "nuint?", "ulong", $"bool ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint?", "float", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nuint?", "double", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nuint?", "decimal", $"bool decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint?", "System.IntPtr");
                binaryOps(symbol, "nuint?", "System.UIntPtr", $"bool nuint.{name}(nuint left, nuint right)", $"bool System.UIntPtr.{name}(System.UIntPtr value1, System.UIntPtr value2)");
                binaryOps(symbol, "nuint?", "bool?");
                binaryOps(symbol, "nuint?", "char?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "sbyte?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "byte?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "short?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "ushort?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "int?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "int?"), getAmbiguousBinaryOpsDiagnostics(symbol, "int?", "nuint?"));
                binaryOps(symbol, "nuint?", "uint?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "nint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint?"));
                binaryOps(symbol, "nuint?", "nuint?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "long?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "long?"), getAmbiguousBinaryOpsDiagnostics(symbol, "long?", "nuint?"));
                binaryOps(symbol, "nuint?", "ulong?", $"bool ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint?", "float?", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nuint?", "double?", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nuint?", "decimal?", $"bool decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint?", "System.IntPtr?");
                binaryOps(symbol, "nuint?", "System.UIntPtr?", $"bool nuint.{name}(nuint left, System.UIntPtr right)", $"bool System.UIntPtr.{name}(System.UIntPtr value1, System.UIntPtr value2)");
                binaryOps(symbol, "System.IntPtr", "object");
                binaryOps(symbol, "System.IntPtr", "string");
                binaryOps(symbol, "System.IntPtr", "void*");
                binaryOps(symbol, "System.IntPtr", "bool");
                binaryOps(symbol, "System.IntPtr", "char");
                binaryOps(symbol, "System.IntPtr", "sbyte");
                binaryOps(symbol, "System.IntPtr", "byte");
                binaryOps(symbol, "System.IntPtr", "short");
                binaryOps(symbol, "System.IntPtr", "ushort");
                binaryOps(symbol, "System.IntPtr", "int");
                binaryOps(symbol, "System.IntPtr", "uint");
                binaryOps(symbol, "System.IntPtr", "nint", $"bool System.IntPtr.{name}(System.IntPtr value1, System.IntPtr value2)", $"bool nint.{name}(nint left, System.IntPtr right)");
                binaryOps(symbol, "System.IntPtr", "nuint");
                binaryOps(symbol, "System.IntPtr", "long");
                binaryOps(symbol, "System.IntPtr", "ulong");
                binaryOps(symbol, "System.IntPtr", "float");
                binaryOps(symbol, "System.IntPtr", "double");
                binaryOps(symbol, "System.IntPtr", "decimal");
                binaryOps(symbol, "System.IntPtr", "System.IntPtr", $"bool System.IntPtr.{name}(System.IntPtr value1, System.IntPtr value2)");
                binaryOps(symbol, "System.IntPtr", "System.UIntPtr");
                binaryOps(symbol, "System.IntPtr", "bool?");
                binaryOps(symbol, "System.IntPtr", "char?");
                binaryOps(symbol, "System.IntPtr", "sbyte?");
                binaryOps(symbol, "System.IntPtr", "byte?");
                binaryOps(symbol, "System.IntPtr", "short?");
                binaryOps(symbol, "System.IntPtr", "ushort?");
                binaryOps(symbol, "System.IntPtr", "int?");
                binaryOps(symbol, "System.IntPtr", "uint?");
                binaryOps(symbol, "System.IntPtr", "nint?", $"bool System.IntPtr.{name}(System.IntPtr value1, System.IntPtr value2)", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "System.IntPtr", "nuint?");
                binaryOps(symbol, "System.IntPtr", "long?");
                binaryOps(symbol, "System.IntPtr", "ulong?");
                binaryOps(symbol, "System.IntPtr", "float?");
                binaryOps(symbol, "System.IntPtr", "double?");
                binaryOps(symbol, "System.IntPtr", "decimal?");
                binaryOps(symbol, "System.IntPtr", "System.IntPtr?", $"bool System.IntPtr.{name}(System.IntPtr value1, System.IntPtr value2)");
                binaryOps(symbol, "System.IntPtr", "System.UIntPtr?");
                binaryOps(symbol, "System.IntPtr", "object");
                binaryOps(symbol, "System.IntPtr?", "string");
                binaryOps(symbol, "System.IntPtr?", "void*");
                binaryOps(symbol, "System.IntPtr?", "bool");
                binaryOps(symbol, "System.IntPtr?", "char");
                binaryOps(symbol, "System.IntPtr?", "sbyte");
                binaryOps(symbol, "System.IntPtr?", "byte");
                binaryOps(symbol, "System.IntPtr?", "short");
                binaryOps(symbol, "System.IntPtr?", "ushort");
                binaryOps(symbol, "System.IntPtr?", "int");
                binaryOps(symbol, "System.IntPtr?", "uint");
                binaryOps(symbol, "System.IntPtr?", "nint", $"bool System.IntPtr.{name}(System.IntPtr value1, System.IntPtr value2)", $"bool nint.{name}(nint left, System.IntPtr right)");
                binaryOps(symbol, "System.IntPtr?", "nuint");
                binaryOps(symbol, "System.IntPtr?", "long");
                binaryOps(symbol, "System.IntPtr?", "ulong");
                binaryOps(symbol, "System.IntPtr?", "float");
                binaryOps(symbol, "System.IntPtr?", "double");
                binaryOps(symbol, "System.IntPtr?", "decimal");
                binaryOps(symbol, "System.IntPtr?", "System.IntPtr", $"bool System.IntPtr.{name}(System.IntPtr value1, System.IntPtr value2)");
                binaryOps(symbol, "System.IntPtr?", "System.UIntPtr");
                binaryOps(symbol, "System.IntPtr?", "bool?");
                binaryOps(symbol, "System.IntPtr?", "char?");
                binaryOps(symbol, "System.IntPtr?", "sbyte?");
                binaryOps(symbol, "System.IntPtr?", "byte?");
                binaryOps(symbol, "System.IntPtr?", "short?");
                binaryOps(symbol, "System.IntPtr?", "ushort?");
                binaryOps(symbol, "System.IntPtr?", "int?");
                binaryOps(symbol, "System.IntPtr?", "uint?");
                binaryOps(symbol, "System.IntPtr?", "nint?", $"bool System.IntPtr.{name}(System.IntPtr value1, System.IntPtr value2)", $"bool nint.{name}(nint left, System.IntPtr right)");
                binaryOps(symbol, "System.IntPtr?", "nuint?");
                binaryOps(symbol, "System.IntPtr?", "long?");
                binaryOps(symbol, "System.IntPtr?", "ulong?");
                binaryOps(symbol, "System.IntPtr?", "float?");
                binaryOps(symbol, "System.IntPtr?", "double?");
                binaryOps(symbol, "System.IntPtr?", "decimal?");
                binaryOps(symbol, "System.IntPtr?", "System.IntPtr?", $"bool System.IntPtr.{name}(System.IntPtr value1, System.IntPtr value2)");
                binaryOps(symbol, "System.IntPtr?", "System.UIntPtr?");
                binaryOps(symbol, "System.UIntPtr", "object");
                binaryOps(symbol, "System.UIntPtr", "string");
                binaryOps(symbol, "System.UIntPtr", "void*");
                binaryOps(symbol, "System.UIntPtr", "bool");
                binaryOps(symbol, "System.UIntPtr", "char");
                binaryOps(symbol, "System.UIntPtr", "sbyte");
                binaryOps(symbol, "System.UIntPtr", "byte");
                binaryOps(symbol, "System.UIntPtr", "short");
                binaryOps(symbol, "System.UIntPtr", "ushort");
                binaryOps(symbol, "System.UIntPtr", "int");
                binaryOps(symbol, "System.UIntPtr", "uint");
                binaryOps(symbol, "System.UIntPtr", "nint");
                binaryOps(symbol, "System.UIntPtr", "nuint", $"bool System.UIntPtr.{name}(System.UIntPtr value1, System.UIntPtr value2)", $"bool nuint.{name}(nuint left, System.UIntPtr right)");
                binaryOps(symbol, "System.UIntPtr", "long");
                binaryOps(symbol, "System.UIntPtr", "ulong");
                binaryOps(symbol, "System.UIntPtr", "float");
                binaryOps(symbol, "System.UIntPtr", "double");
                binaryOps(symbol, "System.UIntPtr", "decimal");
                binaryOps(symbol, "System.UIntPtr", "System.IntPtr");
                binaryOps(symbol, "System.UIntPtr", "System.UIntPtr", $"bool System.UIntPtr.{name}(System.UIntPtr value1, System.UIntPtr value2)");
                binaryOps(symbol, "System.UIntPtr", "bool?");
                binaryOps(symbol, "System.UIntPtr", "char?");
                binaryOps(symbol, "System.UIntPtr", "sbyte?");
                binaryOps(symbol, "System.UIntPtr", "byte?");
                binaryOps(symbol, "System.UIntPtr", "short?");
                binaryOps(symbol, "System.UIntPtr", "ushort?");
                binaryOps(symbol, "System.UIntPtr", "int?");
                binaryOps(symbol, "System.UIntPtr", "uint?");
                binaryOps(symbol, "System.UIntPtr", "nint?");
                binaryOps(symbol, "System.UIntPtr", "nuint?", $"bool System.UIntPtr.{name}(System.UIntPtr value1, System.UIntPtr value2)", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "System.UIntPtr", "long?");
                binaryOps(symbol, "System.UIntPtr", "ulong?");
                binaryOps(symbol, "System.UIntPtr", "float?");
                binaryOps(symbol, "System.UIntPtr", "double?");
                binaryOps(symbol, "System.UIntPtr", "decimal?");
                binaryOps(symbol, "System.UIntPtr", "System.IntPtr?");
                binaryOps(symbol, "System.UIntPtr", "System.UIntPtr?", $"bool System.UIntPtr.{name}(System.UIntPtr value1, System.UIntPtr value2)");
                binaryOps(symbol, "System.UIntPtr", "object");
                binaryOps(symbol, "System.UIntPtr?", "string");
                binaryOps(symbol, "System.UIntPtr?", "void*");
                binaryOps(symbol, "System.UIntPtr?", "bool");
                binaryOps(symbol, "System.UIntPtr?", "char");
                binaryOps(symbol, "System.UIntPtr?", "sbyte");
                binaryOps(symbol, "System.UIntPtr?", "byte");
                binaryOps(symbol, "System.UIntPtr?", "short");
                binaryOps(symbol, "System.UIntPtr?", "ushort");
                binaryOps(symbol, "System.UIntPtr?", "int");
                binaryOps(symbol, "System.UIntPtr?", "uint");
                binaryOps(symbol, "System.UIntPtr?", "nint");
                binaryOps(symbol, "System.UIntPtr?", "nuint", $"bool System.UIntPtr.{name}(System.UIntPtr value1, System.UIntPtr value2)", $"bool nuint.{name}(nuint left, System.UIntPtr right)");
                binaryOps(symbol, "System.UIntPtr?", "long");
                binaryOps(symbol, "System.UIntPtr?", "ulong");
                binaryOps(symbol, "System.UIntPtr?", "float");
                binaryOps(symbol, "System.UIntPtr?", "double");
                binaryOps(symbol, "System.UIntPtr?", "decimal");
                binaryOps(symbol, "System.UIntPtr?", "System.IntPtr");
                binaryOps(symbol, "System.UIntPtr?", "System.UIntPtr", $"bool System.UIntPtr.{name}(System.UIntPtr value1, System.UIntPtr value2)");
                binaryOps(symbol, "System.UIntPtr?", "bool?");
                binaryOps(symbol, "System.UIntPtr?", "char?");
                binaryOps(symbol, "System.UIntPtr?", "sbyte?");
                binaryOps(symbol, "System.UIntPtr?", "byte?");
                binaryOps(symbol, "System.UIntPtr?", "short?");
                binaryOps(symbol, "System.UIntPtr?", "ushort?");
                binaryOps(symbol, "System.UIntPtr?", "int?");
                binaryOps(symbol, "System.UIntPtr?", "uint?");
                binaryOps(symbol, "System.UIntPtr?", "nint?");
                binaryOps(symbol, "System.UIntPtr?", "nuint?", $"bool System.UIntPtr.{name}(System.UIntPtr value1, System.UIntPtr value2)", $"bool nuint.{name}(nuint left, System.UIntPtr right)");
                binaryOps(symbol, "System.UIntPtr?", "long?");
                binaryOps(symbol, "System.UIntPtr?", "ulong?");
                binaryOps(symbol, "System.UIntPtr?", "float?");
                binaryOps(symbol, "System.UIntPtr?", "double?");
                binaryOps(symbol, "System.UIntPtr?", "decimal?");
                binaryOps(symbol, "System.UIntPtr?", "System.IntPtr?");
                binaryOps(symbol, "System.UIntPtr?", "System.UIntPtr?", $"bool System.UIntPtr.{name}(System.UIntPtr value1, System.UIntPtr value2)");
            }

            foreach ((string symbol, string name) in logicalOperators)
            {
                binaryOps(symbol, "nint", "object");
                binaryOps(symbol, "nint", "string");
                binaryOps(symbol, "nint", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nint", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "nint", includeVoidError: true));
                binaryOps(symbol, "nint", "bool");
                binaryOps(symbol, "nint", "char", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "sbyte", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "byte", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "short", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "ushort", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "int", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "uint", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "nint", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "nuint");
                binaryOps(symbol, "nint", "long", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "ulong");
                binaryOps(symbol, "nint", "float");
                binaryOps(symbol, "nint", "double");
                binaryOps(symbol, "nint", "decimal");
                binaryOps(symbol, "nint", "System.IntPtr", $"nint nint.{name}(nint left, System.IntPtr right)", $"nint System.IntPtr.{name}(System.IntPtr left, nint right)");
                binaryOps(symbol, "nint", "System.UIntPtr");
                binaryOps(symbol, "nint", "bool?");
                binaryOps(symbol, "nint", "char?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "sbyte?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "byte?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "short?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "ushort?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "int?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "uint?", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "nint?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "nuint?");
                binaryOps(symbol, "nint", "long?", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "ulong?");
                binaryOps(symbol, "nint", "float?");
                binaryOps(symbol, "nint", "double?");
                binaryOps(symbol, "nint", "decimal?");
                binaryOps(symbol, "nint", "System.IntPtr?", $"nint nint.{name}(nint left, System.IntPtr right)", $"nint System.IntPtr.{name}(System.IntPtr left, nint right)");
                binaryOps(symbol, "nint", "System.UIntPtr?");
                binaryOps(symbol, "nint", "object");
                binaryOps(symbol, "nint?", "string");
                binaryOps(symbol, "nint?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nint?", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "nint?", includeVoidError: true));
                binaryOps(symbol, "nint?", "bool");
                binaryOps(symbol, "nint?", "char", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "sbyte", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "byte", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "short", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "ushort", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "int", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "uint", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "nint", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "nuint");
                binaryOps(symbol, "nint?", "long", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "ulong");
                binaryOps(symbol, "nint?", "float");
                binaryOps(symbol, "nint?", "double");
                binaryOps(symbol, "nint?", "decimal");
                binaryOps(symbol, "nint?", "System.IntPtr", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "System.UIntPtr");
                binaryOps(symbol, "nint?", "bool?");
                binaryOps(symbol, "nint?", "char?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "sbyte?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "byte?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "short?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "ushort?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "int?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "uint?", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "nint?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "nuint?");
                binaryOps(symbol, "nint?", "long?", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "ulong?");
                binaryOps(symbol, "nint?", "float?");
                binaryOps(symbol, "nint?", "double?");
                binaryOps(symbol, "nint?", "decimal?");
                binaryOps(symbol, "nint?", "System.IntPtr?", $"nint nint.{name}(nint left, System.IntPtr right)", $"nint System.IntPtr.{name}(System.IntPtr left, nint right)");
                binaryOps(symbol, "nint?", "System.UIntPtr?");
                binaryOps(symbol, "nuint", "object");
                binaryOps(symbol, "nuint", "string");
                binaryOps(symbol, "nuint", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nuint", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "nuint", includeVoidError: true));
                binaryOps(symbol, "nuint", "bool");
                binaryOps(symbol, "nuint", "char", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "sbyte", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "byte", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "short", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "ushort", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "int");
                binaryOps(symbol, "nuint", "uint", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "nint");
                binaryOps(symbol, "nuint", "nuint", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "long");
                binaryOps(symbol, "nuint", "ulong", $"ulong ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint", "float");
                binaryOps(symbol, "nuint", "double");
                binaryOps(symbol, "nuint", "decimal");
                binaryOps(symbol, "nuint", "System.IntPtr");
                binaryOps(symbol, "nuint", "System.UIntPtr", $"nuint nuint.{name}(nuint left, System.UIntPtr right)", $"nuint System.UIntPtr.{name}(System.UIntPtr left, nuint right)");
                binaryOps(symbol, "nuint", "bool?");
                binaryOps(symbol, "nuint", "char?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "sbyte?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "byte?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "short?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "ushort?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "int?");
                binaryOps(symbol, "nuint", "uint?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "nint?");
                binaryOps(symbol, "nuint", "nuint?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "long?");
                binaryOps(symbol, "nuint", "ulong?", $"ulong ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint", "float?");
                binaryOps(symbol, "nuint", "double?");
                binaryOps(symbol, "nuint", "decimal?");
                binaryOps(symbol, "nuint", "System.IntPtr?");
                binaryOps(symbol, "nuint", "System.UIntPtr?", $"nuint nuint.{name}(nuint left, System.UIntPtr right)", $"nuint System.UIntPtr.{name}(System.UIntPtr left, nuint right)");
                binaryOps(symbol, "nuint?", "object");
                binaryOps(symbol, "nuint?", "string");
                binaryOps(symbol, "nuint?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nuint?", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "nuint?", includeVoidError: true));
                binaryOps(symbol, "nuint?", "bool");
                binaryOps(symbol, "nuint?", "char", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "sbyte", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "byte", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "short", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "ushort", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "int");
                binaryOps(symbol, "nuint?", "uint", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "nint");
                binaryOps(symbol, "nuint?", "nuint", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "long");
                binaryOps(symbol, "nuint?", "ulong", $"ulong ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint?", "float");
                binaryOps(symbol, "nuint?", "double");
                binaryOps(symbol, "nuint?", "decimal");
                binaryOps(symbol, "nuint?", "System.IntPtr");
                binaryOps(symbol, "nuint?", "System.UIntPtr", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "bool?");
                binaryOps(symbol, "nuint?", "char?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "sbyte?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "byte?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "short?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "ushort?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "int?");
                binaryOps(symbol, "nuint?", "uint?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "nint?");
                binaryOps(symbol, "nuint?", "nuint?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "long?");
                binaryOps(symbol, "nuint?", "ulong?", $"ulong ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint?", "float?");
                binaryOps(symbol, "nuint?", "double?");
                binaryOps(symbol, "nuint?", "decimal?");
                binaryOps(symbol, "nuint?", "System.IntPtr?");
                binaryOps(symbol, "nuint?", "System.UIntPtr?", $"nuint nuint.{name}(nuint left, System.UIntPtr right)", $"nuint System.UIntPtr.{name}(System.UIntPtr left, nuint right)");
                binaryOps(symbol, "System.IntPtr", "object");
                binaryOps(symbol, "System.IntPtr", "string");
                binaryOps(symbol, "System.IntPtr", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "System.IntPtr", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "System.IntPtr", includeVoidError: true));
                binaryOps(symbol, "System.IntPtr", "bool");
                binaryOps(symbol, "System.IntPtr", "char");
                binaryOps(symbol, "System.IntPtr", "sbyte");
                binaryOps(symbol, "System.IntPtr", "byte");
                binaryOps(symbol, "System.IntPtr", "short");
                binaryOps(symbol, "System.IntPtr", "ushort");
                binaryOps(symbol, "System.IntPtr", "int");
                binaryOps(symbol, "System.IntPtr", "uint");
                binaryOps(symbol, "System.IntPtr", "nint", $"nint System.IntPtr.{name}(System.IntPtr left, nint right)", $"nint nint.{name}(nint left, System.IntPtr right)");
                binaryOps(symbol, "System.IntPtr", "nuint");
                binaryOps(symbol, "System.IntPtr", "long");
                binaryOps(symbol, "System.IntPtr", "ulong");
                binaryOps(symbol, "System.IntPtr", "float");
                binaryOps(symbol, "System.IntPtr", "double");
                binaryOps(symbol, "System.IntPtr", "decimal");
                binaryOps(symbol, "System.IntPtr", "System.IntPtr");
                binaryOps(symbol, "System.IntPtr", "System.UIntPtr");
                binaryOps(symbol, "System.IntPtr", "bool?");
                binaryOps(symbol, "System.IntPtr", "char?");
                binaryOps(symbol, "System.IntPtr", "sbyte?");
                binaryOps(symbol, "System.IntPtr", "byte?");
                binaryOps(symbol, "System.IntPtr", "short?");
                binaryOps(symbol, "System.IntPtr", "ushort?");
                binaryOps(symbol, "System.IntPtr", "int?");
                binaryOps(symbol, "System.IntPtr", "uint?");
                binaryOps(symbol, "System.IntPtr", "nint?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "System.IntPtr", "nuint?");
                binaryOps(symbol, "System.IntPtr", "long?");
                binaryOps(symbol, "System.IntPtr", "ulong?");
                binaryOps(symbol, "System.IntPtr", "float?");
                binaryOps(symbol, "System.IntPtr", "double?");
                binaryOps(symbol, "System.IntPtr", "decimal?");
                binaryOps(symbol, "System.IntPtr", "System.IntPtr?");
                binaryOps(symbol, "System.IntPtr", "System.UIntPtr?");
                binaryOps(symbol, "System.IntPtr", "object");
                binaryOps(symbol, "System.IntPtr?", "string");
                binaryOps(symbol, "System.IntPtr?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "System.IntPtr?", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "System.IntPtr?", includeVoidError: true));
                binaryOps(symbol, "System.IntPtr?", "bool");
                binaryOps(symbol, "System.IntPtr?", "char");
                binaryOps(symbol, "System.IntPtr?", "sbyte");
                binaryOps(symbol, "System.IntPtr?", "byte");
                binaryOps(symbol, "System.IntPtr?", "short");
                binaryOps(symbol, "System.IntPtr?", "ushort");
                binaryOps(symbol, "System.IntPtr?", "int");
                binaryOps(symbol, "System.IntPtr?", "uint");
                binaryOps(symbol, "System.IntPtr?", "nint", $"nint System.IntPtr.{name}(System.IntPtr left, nint right)", $"nint nint.{name}(nint left, System.IntPtr right)");
                binaryOps(symbol, "System.IntPtr?", "nuint");
                binaryOps(symbol, "System.IntPtr?", "long");
                binaryOps(symbol, "System.IntPtr?", "ulong");
                binaryOps(symbol, "System.IntPtr?", "float");
                binaryOps(symbol, "System.IntPtr?", "double");
                binaryOps(symbol, "System.IntPtr?", "decimal");
                binaryOps(symbol, "System.IntPtr?", "System.IntPtr");
                binaryOps(symbol, "System.IntPtr?", "System.UIntPtr");
                binaryOps(symbol, "System.IntPtr?", "bool?");
                binaryOps(symbol, "System.IntPtr?", "char?");
                binaryOps(symbol, "System.IntPtr?", "sbyte?");
                binaryOps(symbol, "System.IntPtr?", "byte?");
                binaryOps(symbol, "System.IntPtr?", "short?");
                binaryOps(symbol, "System.IntPtr?", "ushort?");
                binaryOps(symbol, "System.IntPtr?", "int?");
                binaryOps(symbol, "System.IntPtr?", "uint?");
                binaryOps(symbol, "System.IntPtr?", "nint?", $"nint System.IntPtr.{name}(System.IntPtr left, nint right)", $"nint nint.{name}(nint left, System.IntPtr right)");
                binaryOps(symbol, "System.IntPtr?", "nuint?");
                binaryOps(symbol, "System.IntPtr?", "long?");
                binaryOps(symbol, "System.IntPtr?", "ulong?");
                binaryOps(symbol, "System.IntPtr?", "float?");
                binaryOps(symbol, "System.IntPtr?", "double?");
                binaryOps(symbol, "System.IntPtr?", "decimal?");
                binaryOps(symbol, "System.IntPtr?", "System.IntPtr?");
                binaryOps(symbol, "System.IntPtr?", "System.UIntPtr?");
                binaryOps(symbol, "System.UIntPtr", "object");
                binaryOps(symbol, "System.UIntPtr", "string");
                binaryOps(symbol, "System.UIntPtr", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "System.UIntPtr", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "System.UIntPtr", includeVoidError: true));
                binaryOps(symbol, "System.UIntPtr", "bool");
                binaryOps(symbol, "System.UIntPtr", "char");
                binaryOps(symbol, "System.UIntPtr", "sbyte");
                binaryOps(symbol, "System.UIntPtr", "byte");
                binaryOps(symbol, "System.UIntPtr", "short");
                binaryOps(symbol, "System.UIntPtr", "ushort");
                binaryOps(symbol, "System.UIntPtr", "int");
                binaryOps(symbol, "System.UIntPtr", "uint");
                binaryOps(symbol, "System.UIntPtr", "nint");
                binaryOps(symbol, "System.UIntPtr", "nuint", $"nuint System.UIntPtr.{name}(System.UIntPtr left, nuint right)", $"nuint nuint.{name}(nuint left, System.UIntPtr right)");
                binaryOps(symbol, "System.UIntPtr", "long");
                binaryOps(symbol, "System.UIntPtr", "ulong");
                binaryOps(symbol, "System.UIntPtr", "float");
                binaryOps(symbol, "System.UIntPtr", "double");
                binaryOps(symbol, "System.UIntPtr", "decimal");
                binaryOps(symbol, "System.UIntPtr", "System.IntPtr");
                binaryOps(symbol, "System.UIntPtr", "System.UIntPtr");
                binaryOps(symbol, "System.UIntPtr", "bool?");
                binaryOps(symbol, "System.UIntPtr", "char?");
                binaryOps(symbol, "System.UIntPtr", "sbyte?");
                binaryOps(symbol, "System.UIntPtr", "byte?");
                binaryOps(symbol, "System.UIntPtr", "short?");
                binaryOps(symbol, "System.UIntPtr", "ushort?");
                binaryOps(symbol, "System.UIntPtr", "int?");
                binaryOps(symbol, "System.UIntPtr", "uint?");
                binaryOps(symbol, "System.UIntPtr", "nint?");
                binaryOps(symbol, "System.UIntPtr", "nuint?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "System.UIntPtr", "long?");
                binaryOps(symbol, "System.UIntPtr", "ulong?");
                binaryOps(symbol, "System.UIntPtr", "float?");
                binaryOps(symbol, "System.UIntPtr", "double?");
                binaryOps(symbol, "System.UIntPtr", "decimal?");
                binaryOps(symbol, "System.UIntPtr", "System.IntPtr?");
                binaryOps(symbol, "System.UIntPtr", "System.UIntPtr?");
                binaryOps(symbol, "System.UIntPtr", "object");
                binaryOps(symbol, "System.UIntPtr?", "string");
                binaryOps(symbol, "System.UIntPtr?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "System.UIntPtr?", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "System.UIntPtr?", includeVoidError: true));
                binaryOps(symbol, "System.UIntPtr?", "bool");
                binaryOps(symbol, "System.UIntPtr?", "char");
                binaryOps(symbol, "System.UIntPtr?", "sbyte");
                binaryOps(symbol, "System.UIntPtr?", "byte");
                binaryOps(symbol, "System.UIntPtr?", "short");
                binaryOps(symbol, "System.UIntPtr?", "ushort");
                binaryOps(symbol, "System.UIntPtr?", "int");
                binaryOps(symbol, "System.UIntPtr?", "uint");
                binaryOps(symbol, "System.UIntPtr?", "nint");
                binaryOps(symbol, "System.UIntPtr?", "nuint", $"nuint System.UIntPtr.{name}(System.UIntPtr left, nuint right)", $"nuint nuint.{name}(nuint left, System.UIntPtr right)");
                binaryOps(symbol, "System.UIntPtr?", "long");
                binaryOps(symbol, "System.UIntPtr?", "ulong");
                binaryOps(symbol, "System.UIntPtr?", "float");
                binaryOps(symbol, "System.UIntPtr?", "double");
                binaryOps(symbol, "System.UIntPtr?", "decimal");
                binaryOps(symbol, "System.UIntPtr?", "System.IntPtr");
                binaryOps(symbol, "System.UIntPtr?", "System.UIntPtr");
                binaryOps(symbol, "System.UIntPtr?", "bool?");
                binaryOps(symbol, "System.UIntPtr?", "char?");
                binaryOps(symbol, "System.UIntPtr?", "sbyte?");
                binaryOps(symbol, "System.UIntPtr?", "byte?");
                binaryOps(symbol, "System.UIntPtr?", "short?");
                binaryOps(symbol, "System.UIntPtr?", "ushort?");
                binaryOps(symbol, "System.UIntPtr?", "int?");
                binaryOps(symbol, "System.UIntPtr?", "uint?");
                binaryOps(symbol, "System.UIntPtr?", "nint?");
                binaryOps(symbol, "System.UIntPtr?", "nuint?", $"nuint System.UIntPtr.{name}(System.UIntPtr left, nuint right)", $"nuint nuint.{name}(nuint left, System.UIntPtr right)");
                binaryOps(symbol, "System.UIntPtr?", "long?");
                binaryOps(symbol, "System.UIntPtr?", "ulong?");
                binaryOps(symbol, "System.UIntPtr?", "float?");
                binaryOps(symbol, "System.UIntPtr?", "double?");
                binaryOps(symbol, "System.UIntPtr?", "decimal?");
                binaryOps(symbol, "System.UIntPtr?", "System.IntPtr?");
                binaryOps(symbol, "System.UIntPtr?", "System.UIntPtr?");
            }

            void binaryOperator(string op, string leftType, string rightType, string expectedSymbol, DiagnosticDescription[] expectedDiagnostics)
            {
                bool useUnsafeContext = useUnsafe(leftType) || useUnsafe(rightType);
                string source =
    $@"class Program
{{
    static {(useUnsafeContext ? "unsafe " : "")}object Evaluate({leftType} x, {rightType} y)
    {{
        return x {op} y;
    }}
}}";
                var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithAllowUnsafe(useUnsafeContext), parseOptions: TestOptions.RegularPreview);
                comp.VerifyDiagnostics(expectedDiagnostics);

                var tree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(tree);
                var expr = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();
                var symbolInfo = model.GetSymbolInfo(expr);
                Assert.Equal(expectedSymbol, symbolInfo.Symbol?.ToDisplayString(SymbolDisplayFormat.TestFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes)));

                if (expectedDiagnostics.Length == 0)
                {
                    CompileAndVerify(comp);
                }

                static bool useUnsafe(string type) => type == "void*";
            }
        }

        [Fact]
        public void BinaryOperators_NInt()
        {
            var source =
@"using System;
class Program
{
    static nint Add(nint x, nint y) => x + y;
    static nint Subtract(nint x, nint y) => x - y;
    static nint Multiply(nint x, nint y) => x * y;
    static nint Divide(nint x, nint y) => x / y;
    static nint Mod(nint x, nint y) => x % y;
    static bool Equals(nint x, nint y) => x == y;
    static bool NotEquals(nint x, nint y) => x != y;
    static bool LessThan(nint x, nint y) => x < y;
    static bool LessThanOrEqual(nint x, nint y) => x <= y;
    static bool GreaterThan(nint x, nint y) => x > y;
    static bool GreaterThanOrEqual(nint x, nint y) => x >= y;
    static nint And(nint x, nint y) => x & y;
    static nint Or(nint x, nint y) => x | y;
    static nint Xor(nint x, nint y) => x ^ y;
    static nint ShiftLeft(nint x, int y) => x << y;
    static nint ShiftRight(nint x, int y) => x >> y;
    static void Main()
    {
        Console.WriteLine(Add(3, 4));
        Console.WriteLine(Subtract(3, 4));
        Console.WriteLine(Multiply(3, 4));
        Console.WriteLine(Divide(5, 2));
        Console.WriteLine(Mod(5, 2));
        Console.WriteLine(Equals(3, 4));
        Console.WriteLine(NotEquals(3, 4));
        Console.WriteLine(LessThan(3, 4));
        Console.WriteLine(LessThanOrEqual(3, 4));
        Console.WriteLine(GreaterThan(3, 4));
        Console.WriteLine(GreaterThanOrEqual(3, 4));
        Console.WriteLine(And(3, 5));
        Console.WriteLine(Or(3, 5));
        Console.WriteLine(Xor(3, 5));
        Console.WriteLine(ShiftLeft(35, 4));
        Console.WriteLine(ShiftRight(35, 4));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput:
@"7
-1
12
2
1
False
True
True
True
False
False
1
7
6
560
2");
            verifier.VerifyIL("Program.Add",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  add
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Subtract",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  sub
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Multiply",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  mul
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Divide",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  div
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Mod",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  rem
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Equals",
@"{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ceq
  IL_0004:  ret
}");
            verifier.VerifyIL("Program.NotEquals",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ceq
  IL_0004:  ldc.i4.0
  IL_0005:  ceq
  IL_0007:  ret
}");
            verifier.VerifyIL("Program.LessThan",
@"{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  clt
  IL_0004:  ret
}");
            verifier.VerifyIL("Program.LessThanOrEqual",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  cgt
  IL_0004:  ldc.i4.0
  IL_0005:  ceq
  IL_0007:  ret
}");
            verifier.VerifyIL("Program.GreaterThan",
@"{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  cgt
  IL_0004:  ret
}");
            verifier.VerifyIL("Program.GreaterThanOrEqual",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  clt
  IL_0004:  ldc.i4.0
  IL_0005:  ceq
  IL_0007:  ret
}");
            verifier.VerifyIL("Program.And",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  and
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Or",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  or
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Xor",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  xor
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.ShiftLeft",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  shl
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.ShiftRight",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  shr
  IL_0003:  ret
}");
        }

        [Fact]
        public void BinaryOperators_NUInt()
        {
            var source =
@"using System;
class Program
{
    static nuint Add(nuint x, nuint y) => x + y;
    static nuint Subtract(nuint x, nuint y) => x - y;
    static nuint Multiply(nuint x, nuint y) => x * y;
    static nuint Divide(nuint x, nuint y) => x / y;
    static nuint Mod(nuint x, nuint y) => x % y;
    static bool Equals(nuint x, nuint y) => x == y;
    static bool NotEquals(nuint x, nuint y) => x != y;
    static bool LessThan(nuint x, nuint y) => x < y;
    static bool LessThanOrEqual(nuint x, nuint y) => x <= y;
    static bool GreaterThan(nuint x, nuint y) => x > y;
    static bool GreaterThanOrEqual(nuint x, nuint y) => x >= y;
    static nuint And(nuint x, nuint y) => x & y;
    static nuint Or(nuint x, nuint y) => x | y;
    static nuint Xor(nuint x, nuint y) => x ^ y;
    static nuint ShiftLeft(nuint x, int y) => x << y;
    static nuint ShiftRight(nuint x, int y) => x >> y;
    static void Main()
    {
        Console.WriteLine(Add(3, 4));
        Console.WriteLine(Subtract(4, 3));
        Console.WriteLine(Multiply(3, 4));
        Console.WriteLine(Divide(5, 2));
        Console.WriteLine(Mod(5, 2));
        Console.WriteLine(Equals(3, 4));
        Console.WriteLine(NotEquals(3, 4));
        Console.WriteLine(LessThan(3, 4));
        Console.WriteLine(LessThanOrEqual(3, 4));
        Console.WriteLine(GreaterThan(3, 4));
        Console.WriteLine(GreaterThanOrEqual(3, 4));
        Console.WriteLine(And(3, 5));
        Console.WriteLine(Or(3, 5));
        Console.WriteLine(Xor(3, 5));
        Console.WriteLine(ShiftLeft(35, 4));
        Console.WriteLine(ShiftRight(35, 4));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput:
@"7
1
12
2
1
False
True
True
True
False
False
1
7
6
560
2");
            verifier.VerifyIL("Program.Add",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  add
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Subtract",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  sub
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Multiply",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  mul
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Divide",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  div.un
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Mod",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  rem.un
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Equals",
@"{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ceq
  IL_0004:  ret
}");
            verifier.VerifyIL("Program.NotEquals",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ceq
  IL_0004:  ldc.i4.0
  IL_0005:  ceq
  IL_0007:  ret
}");
            verifier.VerifyIL("Program.LessThan",
@"{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  clt.un
  IL_0004:  ret
}");
            verifier.VerifyIL("Program.LessThanOrEqual",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  cgt.un
  IL_0004:  ldc.i4.0
  IL_0005:  ceq
  IL_0007:  ret
}");
            verifier.VerifyIL("Program.GreaterThan",
@"{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  cgt.un
  IL_0004:  ret
}");
            verifier.VerifyIL("Program.GreaterThanOrEqual",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  clt.un
  IL_0004:  ldc.i4.0
  IL_0005:  ceq
  IL_0007:  ret
}");
            verifier.VerifyIL("Program.And",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  and
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Or",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  or
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Xor",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  xor
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.ShiftLeft",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  shl
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.ShiftRight",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  shr.un
  IL_0003:  ret
}");
        }

        [Fact]
        public void BinaryOperators_NInt_Checked()
        {
            var source =
@"using System;
class Program
{
    static nint Add(nint x, nint y) => checked(x + y);
    static nint Subtract(nint x, nint y) => checked(x - y);
    static nint Multiply(nint x, nint y) => checked(x * y);
    static nint Divide(nint x, nint y) => checked(x / y);
    static nint Mod(nint x, nint y) => checked(x % y);
    static void Main()
    {
        Console.WriteLine(Add(3, 4));
        Console.WriteLine(Subtract(3, 4));
        Console.WriteLine(Multiply(3, 4));
        Console.WriteLine(Divide(5, 2));
        Console.WriteLine(Mod(5, 2));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput:
@"7
-1
12
2
1");
            verifier.VerifyIL("Program.Add",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  add.ovf
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Subtract",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  sub.ovf
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Multiply",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  mul.ovf
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Divide",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  div
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Mod",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  rem
  IL_0003:  ret
}");
        }

        [Fact]
        public void BinaryOperators_NUInt_Checked()
        {
            var source =
@"using System;
class Program
{
    static nuint Add(nuint x, nuint y) => checked(x + y);
    static nuint Subtract(nuint x, nuint y) => checked(x - y);
    static nuint Multiply(nuint x, nuint y) => checked(x * y);
    static nuint Divide(nuint x, nuint y) => checked(x / y);
    static nuint Mod(nuint x, nuint y) => checked(x % y);
    static void Main()
    {
        Console.WriteLine(Add(3, 4));
        Console.WriteLine(Subtract(4, 3));
        Console.WriteLine(Multiply(3, 4));
        Console.WriteLine(Divide(5, 2));
        Console.WriteLine(Mod(5, 2));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput:
@"7
1
12
2
1");
            verifier.VerifyIL("Program.Add",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  add.ovf.un
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Subtract",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  sub.ovf.un
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Multiply",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  mul.ovf.un
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Divide",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  div.un
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Mod",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  rem.un
  IL_0003:  ret
}");
        }

        [Fact]
        public void ConstantFolding()
        {
            const string intMinValue = "-2147483648";
            const string intMaxValue = "2147483647";
            const string uintMaxValue = "4294967295";
            const string ulongMaxValue = "18446744073709551615";

            unaryOperator("nint", "+", intMinValue, intMinValue);
            unaryOperator("nint", "+", intMaxValue, intMaxValue);
            unaryOperator("nuint", "+", "0", "0");
            unaryOperator("nuint", "+", uintMaxValue, uintMaxValue);

            unaryOperator("nint", "-", "-1", "1");
            unaryOperatorCheckedOverflow("nint", "-", intMinValue, IntPtr.Size == 4 ? "-2147483648" : "2147483648");
            unaryOperator("nint", "-", "-2147483647", intMaxValue);
            unaryOperator("nint", "-", intMaxValue, "-2147483647");
            unaryOperator("nuint", "-", "0", null, getBadUnaryOpDiagnostics);
            unaryOperator("nuint", "-", "1", null, getBadUnaryOpDiagnostics);
            unaryOperator("nuint", "-", uintMaxValue, null, getBadUnaryOpDiagnostics);

            unaryOperatorNotConstant("nint", "~", "0", "-1");
            unaryOperatorNotConstant("nint", "~", "-1", "0");
            unaryOperatorNotConstant("nint", "~", intMinValue, "2147483647");
            unaryOperatorNotConstant("nint", "~", intMaxValue, "-2147483648");
            unaryOperatorNotConstant("nuint", "~", "0", IntPtr.Size == 4 ? uintMaxValue : ulongMaxValue);
            unaryOperatorNotConstant("nuint", "~", uintMaxValue, IntPtr.Size == 4 ? "0" : "18446744069414584320");

            binaryOperatorCheckedOverflow("nint", "+", "nint", intMinValue, "nint", "-1", IntPtr.Size == 4 ? "2147483647" : "-2147483649");
            binaryOperator("nint", "+", "nint", "-2147483647", "nint", "-1", intMinValue);
            binaryOperatorCheckedOverflow("nint", "+", "nint", "1", "nint", intMaxValue, IntPtr.Size == 4 ? "-2147483648" : "2147483648");
            binaryOperator("nint", "+", "nint", "1", "nint", "2147483646", intMaxValue);
            binaryOperatorCheckedOverflow("nuint", "+", "nuint", "1", "nuint", uintMaxValue, IntPtr.Size == 4 ? "0" : "4294967296");
            binaryOperator("nuint", "+", "nuint", "1", "nuint", "4294967294", uintMaxValue);

            binaryOperatorCheckedOverflow("nint", "-", "nint", intMinValue, "nint", "1", IntPtr.Size == 4 ? "2147483647" : "-2147483649");
            binaryOperator("nint", "-", "nint", intMinValue, "nint", "-1", "-2147483647");
            binaryOperator("nint", "-", "nint", "-1", "nint", intMaxValue, intMinValue);
            binaryOperatorCheckedOverflow("nint", "-", "nint", "-2", "nint", intMaxValue, IntPtr.Size == 4 ? "2147483647" : "-2147483649");
            binaryOperatorCheckedOverflow("nuint", "-", "nuint", "0", "nuint", "1", IntPtr.Size == 4 ? uintMaxValue : ulongMaxValue);
            binaryOperator("nuint", "-", "nuint", uintMaxValue, "nuint", uintMaxValue, "0");

            binaryOperatorCheckedOverflow("nint", "*", "nint", intMinValue, "nint", "2", IntPtr.Size == 4 ? "0" : "-4294967296");
            binaryOperatorCheckedOverflow("nint", "*", "nint", intMinValue, "nint", "-1", IntPtr.Size == 4 ? "-2147483648" : "2147483648");
            binaryOperator("nint", "*", "nint", "-1", "nint", intMaxValue, "-2147483647");
            binaryOperatorCheckedOverflow("nint", "*", "nint", "2", "nint", intMaxValue, IntPtr.Size == 4 ? "-2" : "4294967294");
            binaryOperatorCheckedOverflow("nuint", "*", "nuint", uintMaxValue, "nuint", "2", IntPtr.Size == 4 ? "4294967294" : "8589934590");
            binaryOperator("nuint", "*", "nuint", intMaxValue, "nuint", "2", "4294967294");

            binaryOperator("nint", "/", "nint", intMinValue, "nint", "1", intMinValue);
            binaryOperatorCheckedOverflow("nint", "/", "nint", intMinValue, "nint", "-1", IntPtr.Size == 4 ? "System.OverflowException" : "2147483648");
            binaryOperator("nint", "/", "nint", "1", "nint", "0", null, getIntDivByZeroDiagnostics);
            binaryOperator("nint", "/", "nint", "0", "nint", "0", null, getIntDivByZeroDiagnostics);
            binaryOperator("nuint", "/", "nuint", uintMaxValue, "nuint", "1", uintMaxValue);
            binaryOperator("nuint", "/", "nuint", uintMaxValue, "nuint", "2", intMaxValue);
            binaryOperator("nuint", "/", "nuint", "1", "nuint", "0", null, getIntDivByZeroDiagnostics);
            binaryOperator("nuint", "/", "nuint", "0", "nuint", "0", null, getIntDivByZeroDiagnostics);

            binaryOperator("nint", "%", "nint", intMinValue, "nint", "2", "0");
            binaryOperator("nint", "%", "nint", intMinValue, "nint", "-2", "0");
            binaryOperatorCheckedOverflow("nint", "%", "nint", intMinValue, "nint", "-1", IntPtr.Size == 4 ? "System.OverflowException" : "0");
            binaryOperator("nint", "%", "nint", "1", "nint", "0", null, getIntDivByZeroDiagnostics);
            binaryOperator("nint", "%", "nint", "0", "nint", "0", null, getIntDivByZeroDiagnostics);
            binaryOperator("nuint", "%", "nuint", uintMaxValue, "nuint", "1", "0");
            binaryOperator("nuint", "%", "nuint", uintMaxValue, "nuint", "2", "1");
            binaryOperator("nuint", "%", "nuint", "1", "nuint", "0", null, getIntDivByZeroDiagnostics);
            binaryOperator("nuint", "%", "nuint", "0", "nuint", "0", null, getIntDivByZeroDiagnostics);

            binaryOperator("bool", "<", "nint", intMinValue, "nint", intMinValue, "False");
            binaryOperator("bool", "<", "nint", intMinValue, "nint", intMaxValue, "True");
            binaryOperator("bool", "<", "nint", intMaxValue, "nint", intMaxValue, "False");
            binaryOperator("bool", "<", "nuint", "0", "nuint", "0", "False");
            binaryOperator("bool", "<", "nuint", "0", "nuint", uintMaxValue, "True");
            binaryOperator("bool", "<", "nuint", uintMaxValue, "nuint", uintMaxValue, "False");

            binaryOperator("bool", "<=", "nint", intMinValue, "nint", intMinValue, "True");
            binaryOperator("bool", "<=", "nint", intMaxValue, "nint", intMinValue, "False");
            binaryOperator("bool", "<=", "nint", intMaxValue, "nint", intMaxValue, "True");
            binaryOperator("bool", "<=", "nuint", "0", "nuint", "0", "True");
            binaryOperator("bool", "<=", "nuint", uintMaxValue, "nuint", "0", "False");
            binaryOperator("bool", "<=", "nuint", uintMaxValue, "nuint", uintMaxValue, "True");

            binaryOperator("bool", ">", "nint", intMinValue, "nint", intMinValue, "False");
            binaryOperator("bool", ">", "nint", intMaxValue, "nint", intMinValue, "True");
            binaryOperator("bool", ">", "nint", intMaxValue, "nint", intMaxValue, "False");
            binaryOperator("bool", ">", "nuint", "0", "nuint", "0", "False");
            binaryOperator("bool", ">", "nuint", uintMaxValue, "nuint", "0", "True");
            binaryOperator("bool", ">", "nuint", uintMaxValue, "nuint", uintMaxValue, "False");

            binaryOperator("bool", ">=", "nint", intMinValue, "nint", intMinValue, "True");
            binaryOperator("bool", ">=", "nint", intMinValue, "nint", intMaxValue, "False");
            binaryOperator("bool", ">=", "nint", intMaxValue, "nint", intMaxValue, "True");
            binaryOperator("bool", ">=", "nuint", "0", "nuint", "0", "True");
            binaryOperator("bool", ">=", "nuint", "0", "nuint", uintMaxValue, "False");
            binaryOperator("bool", ">=", "nuint", uintMaxValue, "nuint", uintMaxValue, "True");

            binaryOperator("bool", "==", "nint", intMinValue, "nint", intMinValue, "True");
            binaryOperator("bool", "==", "nint", intMinValue, "nint", intMaxValue, "False");
            binaryOperator("bool", "==", "nint", intMaxValue, "nint", intMaxValue, "True");
            binaryOperator("bool", "==", "nuint", "0", "nuint", "0", "True");
            binaryOperator("bool", "==", "nuint", "0", "nuint", uintMaxValue, "False");
            binaryOperator("bool", "==", "nuint", uintMaxValue, "nuint", uintMaxValue, "True");

            binaryOperator("bool", "!=", "nint", intMinValue, "nint", intMinValue, "False");
            binaryOperator("bool", "!=", "nint", intMinValue, "nint", intMaxValue, "True");
            binaryOperator("bool", "!=", "nint", intMaxValue, "nint", intMaxValue, "False");
            binaryOperator("bool", "!=", "nuint", "0", "nuint", "0", "False");
            binaryOperator("bool", "!=", "nuint", "0", "nuint", uintMaxValue, "True");
            binaryOperator("bool", "!=", "nuint", uintMaxValue, "nuint", uintMaxValue, "False");

            // https://github.com/dotnet/roslyn/issues/42460: Results of `<<` should be dependent on platform.
            binaryOperator("nint", "<<", "nint", intMinValue, "int", "0", intMinValue);
            binaryOperator("nint", "<<", "nint", intMinValue, "int", "1", "0");
            binaryOperator("nint", "<<", "nint", "-1", "int", "31", intMinValue);
            binaryOperator("nint", "<<", "nint", "-1", "int", "32", "-1");
            binaryOperator("nuint", "<<", "nuint", "0", "int", "1", "0");
            binaryOperator("nuint", "<<", "nuint", uintMaxValue, "int", "1", "4294967294");
            binaryOperator("nuint", "<<", "nuint", "1", "int", "31", "2147483648");
            binaryOperator("nuint", "<<", "nuint", "1", "int", "32", "1");

            binaryOperator("nint", ">>", "nint", intMinValue, "int", "0", intMinValue);
            binaryOperator("nint", ">>", "nint", intMinValue, "int", "1", "-1073741824");
            binaryOperator("nint", ">>", "nint", "-1", "int", "31", "-1");
            binaryOperator("nint", ">>", "nint", "-1", "int", "32", "-1");
            binaryOperator("nuint", ">>", "nuint", "0", "int", "1", "0");
            binaryOperator("nuint", ">>", "nuint", uintMaxValue, "int", "1", intMaxValue);
            binaryOperator("nuint", ">>", "nuint", "1", "int", "31", "0");
            binaryOperator("nuint", ">>", "nuint", "1", "int", "32", "1");

            binaryOperator("nint", "&", "nint", intMinValue, "nint", "0", "0");
            binaryOperator("nint", "&", "nint", intMinValue, "nint", "-1", intMinValue);
            binaryOperator("nint", "&", "nint", intMinValue, "nint", intMaxValue, "0");
            binaryOperator("nuint", "&", "nuint", "0", "nuint", uintMaxValue, "0");
            binaryOperator("nuint", "&", "nuint", intMaxValue, "nuint", uintMaxValue, intMaxValue);
            binaryOperator("nuint", "&", "nuint", intMaxValue, "nuint", "2147483648", "0");

            binaryOperator("nint", "|", "nint", intMinValue, "nint", "0", intMinValue);
            binaryOperator("nint", "|", "nint", intMinValue, "nint", "-1", "-1");
            binaryOperator("nint", "|", "nint", intMaxValue, "nint", intMaxValue, intMaxValue);
            binaryOperator("nuint", "|", "nuint", "0", "nuint", uintMaxValue, uintMaxValue);
            binaryOperator("nuint", "|", "nuint", intMaxValue, "nuint", intMaxValue, intMaxValue);
            binaryOperator("nuint", "|", "nuint", intMaxValue, "nuint", "2147483648", uintMaxValue);

            binaryOperator("nint", "^", "nint", intMinValue, "nint", "0", intMinValue);
            binaryOperator("nint", "^", "nint", intMinValue, "nint", "-1", intMaxValue);
            binaryOperator("nint", "^", "nint", intMaxValue, "nint", intMaxValue, "0");
            binaryOperator("nuint", "^", "nuint", "0", "nuint", uintMaxValue, uintMaxValue);
            binaryOperator("nuint", "^", "nuint", intMaxValue, "nuint", intMaxValue, "0");
            binaryOperator("nuint", "^", "nuint", intMaxValue, "nuint", "2147483648", uintMaxValue);

            static DiagnosticDescription[] getNoDiagnostics(string opType, string op, string operand) => Array.Empty<DiagnosticDescription>();
            static DiagnosticDescription[] getBadUnaryOpDiagnostics(string opType, string op, string operand) => new[] { Diagnostic(ErrorCode.ERR_BadUnaryOp, operand).WithArguments(op, opType) };

            static DiagnosticDescription[] getIntDivByZeroDiagnostics(string opType, string op, string operand) => new[] { Diagnostic(ErrorCode.ERR_IntDivByZero, operand) };

            void unaryOperator(string opType, string op, string operand, string expectedResult, Func<string, string, string, DiagnosticDescription[]> getDiagnostics = null)
            {
                getDiagnostics ??= getNoDiagnostics;

                var declarations = $"const {opType} A = {operand};";
                var expr = $"{op}A";
                var diagnostics = getDiagnostics(opType, op, expr);
                constantDeclaration(opType, declarations, expr, expectedResult, diagnostics);
                constantDeclaration(opType, declarations, $"checked({expr})", expectedResult, diagnostics);
                constantDeclaration(opType, declarations, $"unchecked({expr})", expectedResult, diagnostics);

                expr = $"{op}({opType})({operand})";
                diagnostics = getDiagnostics(opType, op, expr);
                constantExpression(opType, expr, expectedResult, diagnostics);
                constantExpression(opType, $"checked({expr})", expectedResult, diagnostics);
                constantExpression(opType, $"unchecked({expr})", expectedResult, diagnostics);
            }

            void unaryOperatorCheckedOverflow(string opType, string op, string operand, string expectedResult)
            {
                var declarations = $"const {opType} A = {operand};";
                var expr = $"{op}A";
                constantDeclaration(opType, declarations, expr, null, new[] { Diagnostic(ErrorCode.ERR_CheckedOverflow, expr) });
                constantDeclaration(opType, declarations, $"checked({expr})", null, new[] { Diagnostic(ErrorCode.ERR_CheckedOverflow, expr) });
                constantDeclaration(opType, declarations, $"unchecked({expr})", null, new[] { Diagnostic(ErrorCode.ERR_NotConstantExpression, $"unchecked({expr})").WithArguments("Library.F") });

                expr = $"{op}({opType})({operand})";
                constantExpression(opType, expr, null, new[] { Diagnostic(ErrorCode.ERR_CheckedOverflow, expr) });
                constantExpression(opType, $"checked({expr})", null, new[] { Diagnostic(ErrorCode.ERR_CheckedOverflow, expr) });
                constantExpression(opType, $"unchecked({expr})", expectedResult, Array.Empty<DiagnosticDescription>());
            }

            void unaryOperatorNotConstant(string opType, string op, string operand, string expectedResult)
            {
                var declarations = $"const {opType} A = {operand};";
                var expr = $"{op}A";
                constantDeclaration(opType, declarations, expr, null, new[] { Diagnostic(ErrorCode.ERR_NotConstantExpression, expr).WithArguments("Library.F") });
                constantDeclaration(opType, declarations, $"checked({expr})", null, new[] { Diagnostic(ErrorCode.ERR_NotConstantExpression, $"checked({expr})").WithArguments("Library.F") });
                constantDeclaration(opType, declarations, $"unchecked({expr})", null, new[] { Diagnostic(ErrorCode.ERR_NotConstantExpression, $"unchecked({expr})").WithArguments("Library.F") });

                expr = $"{op}({opType})({operand})";
                constantExpression(opType, expr, expectedResult, Array.Empty<DiagnosticDescription>());
                constantExpression(opType, $"checked({expr})", expectedResult, Array.Empty<DiagnosticDescription>());
                constantExpression(opType, $"unchecked({expr})", expectedResult, Array.Empty<DiagnosticDescription>());
            }

            void binaryOperator(string opType, string op, string leftType, string leftOperand, string rightType, string rightOperand, string expectedResult, Func<string, string, string, DiagnosticDescription[]> getDiagnostics = null)
            {
                getDiagnostics ??= getNoDiagnostics;

                var declarations = $"const {leftType} A = {leftOperand}; const {rightType} B = {rightOperand};";
                var expr = $"A {op} B";
                var diagnostics = getDiagnostics(opType, op, expr);
                constantDeclaration(opType, declarations, expr, expectedResult, diagnostics);
                constantDeclaration(opType, declarations, $"checked({expr})", expectedResult, diagnostics);
                constantDeclaration(opType, declarations, $"unchecked({expr})", expectedResult, diagnostics);

                expr = $"(({leftType})({leftOperand})) {op} (({rightType})({rightOperand}))";
                diagnostics = getDiagnostics(opType, op, expr);
                constantExpression(opType, expr, expectedResult, diagnostics);
                constantExpression(opType, $"checked({expr})", expectedResult, diagnostics);
                constantExpression(opType, $"unchecked({expr})", expectedResult, diagnostics);
            }

            void binaryOperatorCheckedOverflow(string opType, string op, string leftType, string leftOperand, string rightType, string rightOperand, string expectedResult)
            {
                var declarations = $"const {leftType} A = {leftOperand}; const {rightType} B = {rightOperand};";
                var expr = $"A {op} B";
                constantDeclaration(opType, declarations, expr, null, new[] { Diagnostic(ErrorCode.ERR_CheckedOverflow, expr) });
                constantDeclaration(opType, declarations, $"checked({expr})", null, new[] { Diagnostic(ErrorCode.ERR_CheckedOverflow, expr) });
                constantDeclaration(opType, declarations, $"unchecked({expr})", null, new[] { Diagnostic(ErrorCode.ERR_NotConstantExpression, $"unchecked({expr})").WithArguments("Library.F") });

                expr = $"(({leftType})({leftOperand})) {op} (({rightType})({rightOperand}))";
                constantExpression(opType, expr, null, new[] { Diagnostic(ErrorCode.ERR_CheckedOverflow, expr) });
                constantExpression(opType, $"checked({expr})", null, new[] { Diagnostic(ErrorCode.ERR_CheckedOverflow, expr) });
                constantExpression(opType, $"unchecked({expr})", expectedResult, Array.Empty<DiagnosticDescription>());
            }

            void constantDeclaration(string opType, string declarations, string expr, string expectedResult, DiagnosticDescription[] expectedDiagnostics)
            {
                string sourceA =
$@"public class Library
{{
    {declarations}
    public const {opType} F = {expr};
}}";
                var comp = CreateCompilation(sourceA, parseOptions: TestOptions.RegularPreview);
                comp.VerifyDiagnostics(expectedDiagnostics);

                if (expectedDiagnostics.Length > 0) return;

                string sourceB =
@"class Program
{
    static void Main()
    {
        System.Console.WriteLine(Library.F);
    }
}";
                var refA = comp.EmitToImageReference();
                comp = CreateCompilation(sourceB, references: new[] { refA }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
                CompileAndVerify(comp, expectedOutput: expectedResult);
            }

            // https://github.com/dotnet/csharplang/issues/3259: Should ERR_CheckedOverflow cases be evaluated
            // at runtime rather than compile time to allow operations to succeed on 64-bit platforms?
            void constantExpression(string opType, string expr, string expectedResult, DiagnosticDescription[] expectedDiagnostics)
            {
                string source =
$@"using System;
class Program
{{
    static void Main()
    {{
        object result;
        try
        {{
            {opType} value = {expr};
            result = value;
        }}
        catch (Exception e)
        {{
            result = e.GetType().FullName;
        }}
        Console.WriteLine(result);
    }}
}}";
                var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
                comp.VerifyDiagnostics(expectedDiagnostics);

                if (expectedDiagnostics.Length > 0) return;

                CompileAndVerify(comp, expectedOutput: expectedResult);
            }
        }

        // OverflowException behavior is consistent with unchecked int division.
        [Fact]
        public void UncheckedIntegerDivision()
        {
            string source =
@"using System;
class Program
{
    static void Main()
    {
        Console.WriteLine(Execute(() => IntDivision(int.MinValue + 1, -1)));
        Console.WriteLine(Execute(() => IntDivision(int.MinValue, -1)));
        Console.WriteLine(Execute(() => IntRemainder(int.MinValue + 1, -1)));
        Console.WriteLine(Execute(() => IntRemainder(int.MinValue, -1)));
        Console.WriteLine(Execute(() => NativeIntDivision(int.MinValue + 1, -1)));
        Console.WriteLine(Execute(() => NativeIntDivision(int.MinValue, -1)));
        Console.WriteLine(Execute(() => NativeIntRemainder(int.MinValue + 1, -1)));
        Console.WriteLine(Execute(() => NativeIntRemainder(int.MinValue, -1)));
    }
    static object Execute(Func<object> f)
    {
        try
        {
            return f();
        }
        catch (Exception e)
        {
            return e.GetType().FullName;
        }
    }
    static int IntDivision(int x, int y) => unchecked(x / y);
    static int IntRemainder(int x, int y) => unchecked(x % y);
    static nint NativeIntDivision(nint x, nint y) => unchecked(x / y);
    static nint NativeIntRemainder(nint x, nint y) => unchecked(x % y);
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp, expectedOutput:
$@"2147483647
System.OverflowException
0
System.OverflowException
2147483647
{(IntPtr.Size == 4 ? "System.OverflowException" : "2147483648")}
0
{(IntPtr.Size == 4 ? "System.OverflowException" : "0")}");
        }
    }
}
