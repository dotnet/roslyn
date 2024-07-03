// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class NumericIntPtrTests : CSharpTestBase
    {
        private static string IncludeExpectedOutput(string expectedOutput) => ExecutionConditionUtil.IsMonoOrCoreClr ? expectedOutput : null;

        internal static readonly ConversionKind[] Identity = new[] { ConversionKind.Identity };
        internal static readonly ConversionKind[] NoConversion = new[] { ConversionKind.NoConversion };
        internal static readonly ConversionKind[] Boxing = new[] { ConversionKind.Boxing };
        internal static readonly ConversionKind[] Unboxing = new[] { ConversionKind.Unboxing };
        internal static readonly ConversionKind[] IntPtrConversion = new[] { ConversionKind.IntPtr };
        internal static readonly ConversionKind[] ImplicitNumeric = new[] { ConversionKind.ImplicitNumeric };
        internal static readonly ConversionKind[] ExplicitIntegerToPointer = new[] { ConversionKind.ExplicitIntegerToPointer };
        internal static readonly ConversionKind[] ExplicitPointerToInteger = new[] { ConversionKind.ExplicitPointerToInteger };
        internal static readonly ConversionKind[] ExplicitEnumeration = new[] { ConversionKind.ExplicitEnumeration };
        internal static readonly ConversionKind[] ExplicitNumeric = new[] { ConversionKind.ExplicitNumeric };
        internal static readonly ConversionKind[] ExplicitUserDefined = new[] { ConversionKind.ExplicitUserDefined };

        internal static readonly ConversionKind[] ImplicitNullableNumeric = new[] { ConversionKind.ImplicitNullable, ConversionKind.ImplicitNumeric };
        internal static readonly ConversionKind[] ImplicitNullableIdentity = new[] { ConversionKind.ImplicitNullable, ConversionKind.Identity };

        internal static readonly ConversionKind[] ExplicitNullableEnumeration = new[] { ConversionKind.ExplicitNullable, ConversionKind.ExplicitEnumeration };
        internal static readonly ConversionKind[] ExplicitNullableImplicitNumeric = new[] { ConversionKind.ExplicitNullable, ConversionKind.ImplicitNumeric };
        internal static readonly ConversionKind[] ExplicitNullableNumeric = new[] { ConversionKind.ExplicitNullable, ConversionKind.ExplicitNumeric };
        internal static readonly ConversionKind[] ExplicitNullablePointerToInteger = new[] { ConversionKind.ExplicitNullable, ConversionKind.ExplicitPointerToInteger };
        internal static readonly ConversionKind[] ExplicitNullableIdentity = new[] { ConversionKind.ExplicitNullable, ConversionKind.Identity };

        internal static bool IsNoConversion(ConversionKind[] conversionKinds)
        {
            return conversionKinds is [ConversionKind.NoConversion];
        }

        internal static void AssertMatches(ConversionKind[] expected, Conversion conversion)
        {
            IEnumerable<ConversionKind> actualConversionKinds = new[] { conversion.Kind };
            if (!conversion.UnderlyingConversions.IsDefault)
            {
                actualConversionKinds = actualConversionKinds.Concat(conversion.UnderlyingConversions.Select(c => c.Kind));
            }
            Assert.Equal(expected, actualConversionKinds);
        }

        private static int SymbolComparison(Symbol x, Symbol y) => SymbolComparison(x.ToTestDisplayString(), y.ToTestDisplayString());

        private static int SymbolComparison(string x, string y)
        {
            return string.CompareOrdinal(normalizeDisplayString(x), normalizeDisplayString(y));

            static string normalizeDisplayString(string s) => s.Replace("System.IntPtr", "nint").Replace("System.UIntPtr", "nuint");
        }

        [Theory]
        [InlineData("System.IntPtr")]
        [InlineData("nint")]
        public void Interfaces(string type)
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
}
" + RuntimeFeature_NumericIntPtr;

            var sourceB = $$"""
using System;
class Program
{
    static void F0(ISerializable i) { }
    static object F1(IEquatable<IntPtr> i) => default;
    static void F2(IEquatable<nint> i) { }
    static void F3<T>(IOther<T> i) { }
    static void Main()
    {
        {{type}} n = 42;
        F0(n);
        F1(n);
        F2(n);
        F3<nint>(n);
        F3<IntPtr>(n);
    }
}
""";
            var comp = CreateEmptyCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
        }

        [Theory]
        [InlineData("System.UIntPtr")]
        [InlineData("nuint")]
        public void Interfaces_UIntPtr(string type)
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
    public struct UIntPtr : ISerializable, IEquatable<UIntPtr>, IOther<UIntPtr>
    {
        bool IEquatable<UIntPtr>.Equals(UIntPtr other) => false;
    }
}
" + RuntimeFeature_NumericIntPtr;

            var sourceB = $$"""
using System;
class Program
{
    static void F0(ISerializable i) { }
    static object F1(IEquatable<UIntPtr> i) => default;
    static void F2(IEquatable<nuint> i) { }
    static void F3<T>(IOther<T> i) { }
    static void Main()
    {
        {{type}} n = 42;
        F0(n);
        F1(n);
        F2(n);
        F3<nuint>(n);
        F3<UIntPtr>(n);
    }
}
""";
            var comp = CreateEmptyCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void StaticMembers(bool useCompilationReference)
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
    public struct IntPtr
    {
        public static readonly IntPtr Zero;
        public static int Size => 0;
        public static IntPtr MaxValue => default;
        public static IntPtr MinValue => default;
        public static IntPtr Add(IntPtr ptr, int offset) => default;
        public static IntPtr Subtract(IntPtr ptr, int offset) => default;
        public static IntPtr Parse(string s) => default;
        public static bool TryParse(string s, out IntPtr value)
        {
            value = default;
            return false;
        }
    }
    public struct UIntPtr
    {
        public static readonly UIntPtr Zero;
        public static int Size => 0;
        public static UIntPtr MaxValue => default;
        public static UIntPtr MinValue => default;
        public static UIntPtr Add(UIntPtr ptr, int offset) => default;
        public static UIntPtr Subtract(UIntPtr ptr, int offset) => default;
        public static UIntPtr Parse(string s) => default;
        public static bool TryParse(string s, out UIntPtr value)
        {
            value = default;
            return false;
        }
    }
    public class Attribute { }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets validOn) { }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
    }
    public struct Enum { }
    public enum AttributeTargets { }
}
" + RuntimeFeature_NumericIntPtr;

            var comp = CreateEmptyCompilation(sourceA);
            comp.VerifyDiagnostics();
            var refA = AsReference(comp, useCompilationReference);

            var sourceB =
@"class Program
{
    static nint F1()
    {
        _ = nint.Zero;
        _ = nint.Size;
        var x1 = nint.MaxValue;
        var x2 = nint.MinValue;
        _ = nint.Add(x1, 2);
        _ = nint.Subtract(x1, 3);
        var x3 = nint.Parse(null);
        _ = nint.TryParse(null, out var x4);
        return 0;
    }
    static nuint F2()
    {
        _ = nuint.Zero;
        _ = nuint.Size;
        var y1 = nuint.MaxValue;
        var y2 = nuint.MinValue;
        _ = nuint.Add(y1, 2);
        _ = nuint.Subtract(y1, 3);
        var y3 = nuint.Parse(null);
        _ = nuint.TryParse(null, out var y4);
        return 0;
    }
}";

            comp = CreateEmptyCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void InstanceMembers(bool useCompilationReference)
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
    public struct Int64 { }
    public struct UInt32 { }
    public struct UInt64 { }
    public interface IFormatProvider { }
    public struct IntPtr
    {
        public int ToInt32() => default;
        public long ToInt64() => default;
        public uint ToUInt32() => default;
        public ulong ToUInt64() => default;
        unsafe public void* ToPointer() => default;
        public int CompareTo(object other) => default;
        public int CompareTo(IntPtr other) => default;
        public bool Equals(IntPtr other) => default;
        public string ToString(string format) => default;
        public string ToString(IFormatProvider provider) => default;
        public string ToString(string format, IFormatProvider provider) => default;
    }
    public struct UIntPtr
    {
        public int ToInt32() => default;
        public long ToInt64() => default;
        public uint ToUInt32() => default;
        public ulong ToUInt64() => default;
        unsafe public void* ToPointer() => default;
        public int CompareTo(object other) => default;
        public int CompareTo(UIntPtr other) => default;
        public bool Equals(UIntPtr other) => default;
        public string ToString(string format) => default;
        public string ToString(IFormatProvider provider) => default;
        public string ToString(string format, IFormatProvider provider) => default;
    }
    public class Attribute { }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets t) { }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
    }
    public struct Enum { }
    public enum AttributeTargets { }
}
" + RuntimeFeature_NumericIntPtr;

            var comp = CreateEmptyCompilation(sourceA, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics();
            var refA = AsReference(comp, useCompilationReference);

            var sourceB =
@"using System;
class Program
{
    unsafe static void F1(nint i)
    {
        _ = i.ToInt32();
        _ = i.ToInt64();
        _ = i.ToUInt32();
        _ = i.ToUInt64();
        _ = i.ToPointer();
        _ = i.CompareTo(null);
        _ = i.CompareTo(i);
        _ = i.Equals(i);
        _ = i.ToString((string)null);
        _ = i.ToString((IFormatProvider)null);
        _ = i.ToString((string)null, (IFormatProvider)null);
    }
    unsafe static void F2(nuint u)
    {
        _ = u.ToInt32();
        _ = u.ToInt64();
        _ = u.ToUInt32();
        _ = u.ToUInt64();
        _ = u.ToPointer();
        _ = u.CompareTo(null);
        _ = u.CompareTo(u);
        _ = u.Equals(u);
        _ = u.ToString((string)null);
        _ = u.ToString((IFormatProvider)null);
        _ = u.ToString((string)null, (IFormatProvider)null);
    }
}";
            comp = CreateEmptyCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.Regular9, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ConstructorsAndOperators(bool useCompilationReference)
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
    public struct Int64 { }
    public struct UInt32 { }
    public struct UInt64 { }
    public unsafe struct IntPtr
    {
        public IntPtr(int i) { }
        public IntPtr(long l) { }
        public IntPtr(void* p) { }
        public static explicit operator IntPtr(int i) => default;
        public static explicit operator IntPtr(long l) => default;
        public static explicit operator IntPtr(void* p) => default;
        public static explicit operator int(IntPtr i) => default;
        public static explicit operator long(IntPtr i) => default;
        public static explicit operator void*(IntPtr i) => default;
        public static IntPtr operator+(IntPtr x, int y) => default;
        public static IntPtr operator-(IntPtr x, int y) => default;
        public static bool operator==(IntPtr x, IntPtr y) => default;
        public static bool operator!=(IntPtr x, IntPtr y) => default;
    }
    public unsafe struct UIntPtr
    {
        public UIntPtr(uint i) { }
        public UIntPtr(ulong l) { }
        public UIntPtr(void* p) { }
        public static explicit operator UIntPtr(uint i) => default;
        public static explicit operator UIntPtr(ulong l) => default;
        public static explicit operator UIntPtr(void* p) => default;
        public static explicit operator uint(UIntPtr i) => default;
        public static explicit operator ulong(UIntPtr i) => default;
        public static explicit operator void*(UIntPtr i) => default;
        public static UIntPtr operator+(UIntPtr x, int y) => default;
        public static UIntPtr operator-(UIntPtr x, int y) => default;
        public static bool operator==(UIntPtr x, UIntPtr y) => default;
        public static bool operator!=(UIntPtr x, UIntPtr y) => default;
    }
    public class Attribute { }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets t) { }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
    }
    public struct Enum { }
    public enum AttributeTargets { }
}
" + RuntimeFeature_NumericIntPtr;
            var comp = CreateEmptyCompilation(sourceA, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics(
                // (12,26): warning CS0660: 'nint' defines operator == or operator != but does not override Object.Equals(object o)
                //     public unsafe struct IntPtr
                Diagnostic(ErrorCode.WRN_EqualityOpWithoutEquals, "IntPtr").WithArguments("nint").WithLocation(12, 26),
                // (12,26): warning CS0661: 'nint' defines operator == or operator != but does not override Object.GetHashCode()
                //     public unsafe struct IntPtr
                Diagnostic(ErrorCode.WRN_EqualityOpWithoutGetHashCode, "IntPtr").WithArguments("nint").WithLocation(12, 26),
                // (28,26): warning CS0660: 'nuint' defines operator == or operator != but does not override Object.Equals(object o)
                //     public unsafe struct UIntPtr
                Diagnostic(ErrorCode.WRN_EqualityOpWithoutEquals, "UIntPtr").WithArguments("nuint").WithLocation(28, 26),
                // (28,26): warning CS0661: 'nuint' defines operator == or operator != but does not override Object.GetHashCode()
                //     public unsafe struct UIntPtr
                Diagnostic(ErrorCode.WRN_EqualityOpWithoutGetHashCode, "UIntPtr").WithArguments("nuint").WithLocation(28, 26));
            var refA = AsReference(comp, useCompilationReference);

            var sourceB =
@"class Program
{
    unsafe static void F1(nint x, nint y)
    {
        void* p = default;
        _ = new nint();
        _ = new nint(1);
        _ = new nint(2L);
        _ = new nint(p);
        _ = (nint)1;
        _ = (nint)2L;
        _ = (nint)p;
        _ = (int)x;
        _ = (long)x;
        _ = (void*)x;
        _ = x + 1;
        _ = x - 2;
        _ = x == y;
        _ = x != y;
    }
    unsafe static void F2(nuint x, nuint y)
    {
        void* p = default;
        _ = new nuint();
        _ = new nuint(1);
        _ = new nuint(2UL);
        _ = new nuint(p);
        _ = (nuint)1;
        _ = (nuint)2UL;
        _ = (nuint)p;
        _ = (uint)x;
        _ = (ulong)x;
        _ = (void*)x;
        _ = x + 1;
        _ = x - 2;
        _ = x == y;
        _ = x != y;
    }
}";
            comp = CreateEmptyCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.Regular9, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics();
        }

        /// <summary>
        /// Overrides from IntPtr and UIntPtr are implicitly included on nint and nuint.
        /// </summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void OverriddenMembers(bool useCompilationReference)
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
    public struct IntPtr
    {
        public override string ToString() => null;
        public override int GetHashCode() => 0;
        public override bool Equals(object obj) => false;
    }
    public struct UIntPtr
    {
        public override string ToString() => null;
        public override int GetHashCode() => 0;
        public override bool Equals(object obj) => false;
    }
    public class Attribute { }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets t) { }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
    }
    public struct Enum { }
    public enum AttributeTargets { }
}
" + RuntimeFeature_NumericIntPtr;

            var comp = CreateEmptyCompilation(sourceA);
            comp.VerifyDiagnostics();
            var refA = AsReference(comp, useCompilationReference);

            var sourceB =
@"class Program
{
    static void F1(nint x, nint y)
    {
        _ = x.ToString();
        _ = x.GetHashCode();
        _ = x.Equals(y);
    }
    static void F2(nuint x, nuint y)
    {
        _ = x.ToString();
        _ = x.GetHashCode();
        _ = x.Equals(y);
    }
}";
            comp = CreateEmptyCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();

            verifyType((NamedTypeSymbol)comp.GetMember<MethodSymbol>("Program.F1").Parameters[0].Type, signed: true);
            verifyType((NamedTypeSymbol)comp.GetMember<MethodSymbol>("Program.F2").Parameters[0].Type, signed: false);

            static void verifyType(NamedTypeSymbol type, bool signed)
            {
                var members = type.GetMembers().Sort(SymbolComparison);
                var actualMembers = members.SelectAsArray(m => m.ToTestDisplayString());
                var expectedMembers = new[]
                {
                    $"System.Boolean {type}.Equals(System.Object obj)",
                    $"System.Int32 {type}.GetHashCode()",
                    $"System.String {type}.ToString()",
                    $"{type}..ctor()",
                };
                AssertEx.Equal(expectedMembers, actualMembers);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ExplicitImplementations_01(bool useCompilationReference)
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
    public struct Int64 { }
    public struct UInt32 { }
    public struct UInt64 { }
    public interface I<T>
    {
        T P { get; }
        T F();
    }
    public struct IntPtr : I<IntPtr>
    {
        IntPtr I<IntPtr>.P => this;
        IntPtr I<IntPtr>.F() => this;
    }
    public struct UIntPtr : I<UIntPtr>
    {
        UIntPtr I<UIntPtr>.P => this;
        UIntPtr I<UIntPtr>.F() => this;
    }
    public class Attribute { }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets t) { }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
    }
    public struct Enum { }
    public enum AttributeTargets { }
}
" + RuntimeFeature_NumericIntPtr;

            var comp = CreateEmptyCompilation(sourceA);
            comp.VerifyDiagnostics();
            var refA = AsReference(comp, useCompilationReference);

            var sourceB =
@"using System;
class Program
{
    static T F1<T>(I<T> t)
    {
        return default;
    }
    static I<T> F2<T>(I<T> t)
    {
        return t;
    }
    static void M1(nint x)
    {
        var x1 = F1(x);
        var x2 = F2(x).P;
        _ = x.P;
        _ = x.F();
    }
    static void M2(nuint y)
    {
        var y1 = F1(y);
        var y2 = F2(y).P;
        _ = y.P;
        _ = y.F();
    }
    static void M3(System.IntPtr x)
    {
        var z1 = F1(x);
        var z2 = F2(x).P;
        _ = x.P;
        _ = x.F();
    }
    static void M4(System.UIntPtr y)
    {
        var t1 = F1(y);
        var t2 = F2(y).P;
        _ = y.P;
        _ = y.F();
    }
}";
            comp = CreateEmptyCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (16,15): error CS1061: 'nint' does not contain a definition for 'P' and no accessible extension method 'P' accepting a first argument of type 'nint' could be found (are you missing a using directive or an assembly reference?)
                //         _ = x.P;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "P").WithArguments("nint", "P").WithLocation(16, 15),
                // (17,15): error CS1061: 'nint' does not contain a definition for 'F' and no accessible extension method 'F' accepting a first argument of type 'nint' could be found (are you missing a using directive or an assembly reference?)
                //         _ = x.F();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "F").WithArguments("nint", "F").WithLocation(17, 15),
                // (23,15): error CS1061: 'nuint' does not contain a definition for 'P' and no accessible extension method 'P' accepting a first argument of type 'nuint' could be found (are you missing a using directive or an assembly reference?)
                //         _ = y.P;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "P").WithArguments("nuint", "P").WithLocation(23, 15),
                // (24,15): error CS1061: 'nuint' does not contain a definition for 'F' and no accessible extension method 'F' accepting a first argument of type 'nuint' could be found (are you missing a using directive or an assembly reference?)
                //         _ = y.F();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "F").WithArguments("nuint", "F").WithLocation(24, 15),
                // (30,15): error CS1061: 'nint' does not contain a definition for 'P' and no accessible extension method 'P' accepting a first argument of type 'nint' could be found (are you missing a using directive or an assembly reference?)
                //         _ = x.P;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "P").WithArguments("nint", "P").WithLocation(30, 15),
                // (31,15): error CS1061: 'nint' does not contain a definition for 'F' and no accessible extension method 'F' accepting a first argument of type 'nint' could be found (are you missing a using directive or an assembly reference?)
                //         _ = x.F();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "F").WithArguments("nint", "F").WithLocation(31, 15),
                // (37,15): error CS1061: 'nuint' does not contain a definition for 'P' and no accessible extension method 'P' accepting a first argument of type 'nuint' could be found (are you missing a using directive or an assembly reference?)
                //         _ = y.P;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "P").WithArguments("nuint", "P").WithLocation(37, 15),
                // (38,15): error CS1061: 'nuint' does not contain a definition for 'F' and no accessible extension method 'F' accepting a first argument of type 'nuint' could be found (are you missing a using directive or an assembly reference?)
                //         _ = y.F();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "F").WithArguments("nuint", "F").WithLocation(38, 15));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var actualLocals = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Select(d => model.GetDeclaredSymbol(d).ToTestDisplayString());
            var expectedLocals = new[]
            {
                "nint x1",
                "nint x2",
                "nuint y1",
                "nuint y2",
                "nint z1",
                "nint z2",
                "nuint t1",
                "nuint t2",
            };
            AssertEx.Equal(expectedLocals, actualLocals);
        }

        [Fact]
        public void NonPublicMembers_InternalUse()
        {
            var source =
@"namespace System
{
    public class Object { }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public struct IntPtr
    {
        private static IntPtr F1() => default;
        internal IntPtr F2() => default;
        public static IntPtr F3()
        {
            nint i = 0;
            _ = nint.F1();
            _ = i.F2();
            return nint.F3();
        }
        public static nint F4()
        {
            IntPtr i = 0;
            _ = IntPtr.F1();
            _ = i.F2();
            return IntPtr.F3();
        }
    }
    public struct UIntPtr
    {
        private static UIntPtr F1() => default;
        internal UIntPtr F2() => default;
        public static UIntPtr F3()
        {
            nuint i = 0;
            _ = nuint.F1();
            _ = i.F2();
            return nuint.F3();
        }
        public static nuint F4()
        {
            UIntPtr i = 0;
            _ = UIntPtr.F1();
            _ = i.F2();
            return UIntPtr.F3();
        }
    }
}
" + RuntimeFeature_NumericIntPtr;

            var comp = CreateEmptyCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void NonPublicMembers(bool useCompilationReference)
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
    public struct IntPtr
    {
        private static IntPtr F1() => default;
        internal IntPtr F2() => default;
        public static IntPtr F3() => default;
    }
    public struct UIntPtr
    {
        private static UIntPtr F1() => default;
        internal UIntPtr F2() => default;
        public static UIntPtr F3() => default;
    }
    public class Attribute { }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets t) { }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
    }
    public struct Enum { }
    public enum AttributeTargets { }
}
" + RuntimeFeature_NumericIntPtr;

            var comp = CreateEmptyCompilation(sourceA);
            comp.VerifyDiagnostics();
            var refA = AsReference(comp, useCompilationReference);

            var sourceB =
@"using System;
class Program
{
    static void F1(nint x)
    {
        _ = nint.F1();
        _ = x.F2();
        _ = nint.F3();
    }
    static void F2(nuint y)
    {
        _ = nuint.F1();
        _ = y.F2();
        _ = nuint.F3();
    }
    static void F3(IntPtr x)
    {
        _ = IntPtr.F1();
        _ = x.F2();
        _ = IntPtr.F3();
    }
    static void F4(UIntPtr y)
    {
        _ = UIntPtr.F1();
        _ = y.F2();
        _ = UIntPtr.F3();
    }
}";
            comp = CreateEmptyCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.Regular9);
            if (useCompilationReference)
            {
                comp.VerifyDiagnostics(
                    // (6,18): error CS0122: 'nint.F1()' is inaccessible due to its protection level
                    //         _ = nint.F1();
                    Diagnostic(ErrorCode.ERR_BadAccess, "F1").WithArguments("nint.F1()").WithLocation(6, 18),
                    // (7,15): error CS0122: 'nint.F2()' is inaccessible due to its protection level
                    //         _ = x.F2();
                    Diagnostic(ErrorCode.ERR_BadAccess, "F2").WithArguments("nint.F2()").WithLocation(7, 15),
                    // (12,19): error CS0122: 'nuint.F1()' is inaccessible due to its protection level
                    //         _ = nuint.F1();
                    Diagnostic(ErrorCode.ERR_BadAccess, "F1").WithArguments("nuint.F1()").WithLocation(12, 19),
                    // (13,15): error CS0122: 'nuint.F2()' is inaccessible due to its protection level
                    //         _ = y.F2();
                    Diagnostic(ErrorCode.ERR_BadAccess, "F2").WithArguments("nuint.F2()").WithLocation(13, 15),
                    // (18,20): error CS0122: 'nint.F1()' is inaccessible due to its protection level
                    //         _ = IntPtr.F1();
                    Diagnostic(ErrorCode.ERR_BadAccess, "F1").WithArguments("nint.F1()").WithLocation(18, 20),
                    // (19,15): error CS0122: 'nint.F2()' is inaccessible due to its protection level
                    //         _ = x.F2();
                    Diagnostic(ErrorCode.ERR_BadAccess, "F2").WithArguments("nint.F2()").WithLocation(19, 15),
                    // (24,21): error CS0122: 'nuint.F1()' is inaccessible due to its protection level
                    //         _ = UIntPtr.F1();
                    Diagnostic(ErrorCode.ERR_BadAccess, "F1").WithArguments("nuint.F1()").WithLocation(24, 21),
                    // (25,15): error CS0122: 'nuint.F2()' is inaccessible due to its protection level
                    //         _ = y.F2();
                    Diagnostic(ErrorCode.ERR_BadAccess, "F2").WithArguments("nuint.F2()").WithLocation(25, 15));
            }
            else
            {
                comp.VerifyDiagnostics(
                    // (6,18): error CS0117: 'nint' does not contain a definition for 'F1'
                    //         _ = nint.F1();
                    Diagnostic(ErrorCode.ERR_NoSuchMember, "F1").WithArguments("nint", "F1").WithLocation(6, 18),
                    // (7,15): error CS1061: 'nint' does not contain a definition for 'F2' and no accessible extension method 'F2' accepting a first argument of type 'nint' could be found (are you missing a using directive or an assembly reference?)
                    //         _ = x.F2();
                    Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "F2").WithArguments("nint", "F2").WithLocation(7, 15),
                    // (12,19): error CS0117: 'nuint' does not contain a definition for 'F1'
                    //         _ = nuint.F1();
                    Diagnostic(ErrorCode.ERR_NoSuchMember, "F1").WithArguments("nuint", "F1").WithLocation(12, 19),
                    // (13,15): error CS1061: 'nuint' does not contain a definition for 'F2' and no accessible extension method 'F2' accepting a first argument of type 'nuint' could be found (are you missing a using directive or an assembly reference?)
                    //         _ = y.F2();
                    Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "F2").WithArguments("nuint", "F2").WithLocation(13, 15),
                    // (18,20): error CS0117: 'nint' does not contain a definition for 'F1'
                    //         _ = IntPtr.F1();
                    Diagnostic(ErrorCode.ERR_NoSuchMember, "F1").WithArguments("nint", "F1").WithLocation(18, 20),
                    // (19,15): error CS1061: 'nint' does not contain a definition for 'F2' and no accessible extension method 'F2' accepting a first argument of type 'nint' could be found (are you missing a using directive or an assembly reference?)
                    //         _ = x.F2();
                    Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "F2").WithArguments("nint", "F2").WithLocation(19, 15),
                    // (24,21): error CS0117: 'nuint' does not contain a definition for 'F1'
                    //         _ = UIntPtr.F1();
                    Diagnostic(ErrorCode.ERR_NoSuchMember, "F1").WithArguments("nuint", "F1").WithLocation(24, 21),
                    // (25,15): error CS1061: 'nuint' does not contain a definition for 'F2' and no accessible extension method 'F2' accepting a first argument of type 'nuint' could be found (are you missing a using directive or an assembly reference?)
                    //         _ = y.F2();
                    Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "F2").WithArguments("nuint", "F2").WithLocation(25, 15));
            }
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

            var comp = CreateCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(sourceA, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();
            var ref1 = comp.ToMetadataReference();
            var ref2 = comp.EmitToImageReference();

            comp = CreateCompilation(sourceB, references: new[] { ref1 }, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(sourceB, references: new[] { ref2 }, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
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
                // (8,17): warning CS0108: 'B2.F1(nint)' hides inherited member 'A.F1(nint)'. Use the new keyword if hiding was intended.
                //     public void F1(nint x) { base.F1(x); }
                Diagnostic(ErrorCode.WRN_NewRequired, "F1").WithArguments("B2.F1(nint)", "A.F1(nint)").WithLocation(8, 17),
                // (9,17): warning CS0108: 'B2.F2(nuint)' hides inherited member 'A.F2(nuint)'. Use the new keyword if hiding was intended.
                //     public void F2(System.UIntPtr y) { base.F2(y); }
                Diagnostic(ErrorCode.WRN_NewRequired, "F2").WithArguments("B2.F2(nuint)", "A.F2(nuint)").WithLocation(9, 17),
                // (13,21): warning CS0109: The member 'B3.F1(nuint)' does not hide an accessible member. The new keyword is not required.
                //     public new void F1(nuint x) { }
                Diagnostic(ErrorCode.WRN_NewNotRequired, "F1").WithArguments("B3.F1(nuint)").WithLocation(13, 21),
                // (14,21): warning CS0109: The member 'B3.F2(nint)' does not hide an accessible member. The new keyword is not required.
                //     public new void F2(System.IntPtr y) { }
                Diagnostic(ErrorCode.WRN_NewNotRequired, "F2").WithArguments("B3.F2(nint)").WithLocation(14, 21)
            };

            var comp = CreateCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(diagnostics);

            comp = CreateCompilation(sourceA, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();
            var ref1 = comp.ToMetadataReference();
            var ref2 = comp.EmitToImageReference();

            comp = CreateCompilation(sourceB, references: new[] { ref1 }, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(diagnostics);

            comp = CreateCompilation(sourceB, references: new[] { ref2 }, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(diagnostics);
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
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(5), parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(6), parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(63860, "https://github.com/dotnet/roslyn/issues/63860")]
        public void AddUIntPtrAndInt()
        {
            var source = @"
using System;

class C
{
    UIntPtr M(UIntPtr i, int j) => i + j;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(
                // (6,36): error CS0034: Operator '+' is ambiguous on operands of type 'nuint' and 'int'
                //     UIntPtr M(UIntPtr i, int j) => i + j;
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "i + j").WithArguments("+", "nuint", "int").WithLocation(6, 36)
                );

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp);
            verifier.VerifyIL("C.M", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldarg.2
  IL_0002:  call       ""System.UIntPtr System.UIntPtr.op_Addition(System.UIntPtr, int)""
  IL_0007:  ret
}
");
        }

        [Fact, WorkItem(63860, "https://github.com/dotnet/roslyn/issues/63860")]
        public void AddNUIntAndInt()
        {
            var source = @"
class C
{
    nuint M(nuint i, int j) => i + j;
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(
                // (4,32): error CS0034: Operator '+' is ambiguous on operands of type 'nuint' and 'int'
                //     nuint M(nuint i, int j) => i + j;
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "i + j").WithArguments("+", "nuint", "int").WithLocation(4, 32)
                );

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,32): error CS0034: Operator '+' is ambiguous on operands of type 'nuint' and 'int'
                //     nuint M(nuint i, int j) => i + j;
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "i + j").WithArguments("+", "nuint", "int").WithLocation(4, 32)
                );
        }

        [Fact, WorkItem(63860, "https://github.com/dotnet/roslyn/issues/63860")]
        public void AddIntPtrAndInt()
        {
            var source = @"
using System;

class C
{
    IntPtr M(IntPtr i, int j) => i + j;
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, verify: Verification.FailsPEVerify);
            verifier.VerifyIL("C.M", @"
{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldarg.2
  IL_0002:  conv.i
  IL_0003:  add
  IL_0004:  ret
}
");
            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            verifier = CompileAndVerify(comp);
            verifier.VerifyIL("C.M", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldarg.2
  IL_0002:  call       ""System.IntPtr System.IntPtr.op_Addition(System.IntPtr, int)""
  IL_0007:  ret
}
");
        }

        [Fact, WorkItem(63860, "https://github.com/dotnet/roslyn/issues/63860")]
        public void AddNIntAndInt()
        {
            var source = @"
class C
{
    nint M(nint i, int j) => i + j;
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, verify: Verification.FailsPEVerify);
            verifier.VerifyIL("C.M", @"
{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldarg.2
  IL_0002:  conv.i
  IL_0003:  add
  IL_0004:  ret
}
");

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            verifier = CompileAndVerify(comp);
            verifier.VerifyIL("C.M", @"
{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldarg.2
  IL_0002:  conv.i
  IL_0003:  add
  IL_0004:  ret
}
");
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

            var comp = CreateCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(sourceA, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();
            var ref1 = comp.ToMetadataReference();
            var ref2 = comp.EmitToImageReference();

            comp = CreateCompilation(sourceB, references: new[] { ref1 }, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(sourceB, references: new[] { ref2 }, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AttributeType_01()
        {
            var source =
@"[nint]
[A, nuint()]
class Program
{
}
class AAttribute : System.Attribute
{
}";
            var expectedDiagnostics = new[]
            {
                // (1,2): error CS0246: The type or namespace name 'nintAttribute' could not be found (are you missing a using directive or an assembly reference?)
                // [nint]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "nint").WithArguments("nintAttribute").WithLocation(1, 2),
                // (1,2): error CS0246: The type or namespace name 'nint' could not be found (are you missing a using directive or an assembly reference?)
                // [nint]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "nint").WithArguments("nint").WithLocation(1, 2),
                // (2,5): error CS0246: The type or namespace name 'nuintAttribute' could not be found (are you missing a using directive or an assembly reference?)
                // [A, nuint]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "nuint").WithArguments("nuintAttribute").WithLocation(2, 5),
                // (2,5): error CS0246: The type or namespace name 'nuint' could not be found (are you missing a using directive or an assembly reference?)
                // [A, nuint]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "nuint").WithArguments("nuint").WithLocation(2, 5)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(expectedDiagnostics);
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(
                // (6,34): error CS0103: The name 'nint' does not exist in the current context
                //         Console.WriteLine(nameof(nint));
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nint").WithArguments("nint").WithLocation(6, 34),
                // (7,34): error CS0103: The name 'nuint' does not exist in the current context
                //         Console.WriteLine(nameof(nuint));
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nuint").WithArguments("nuint").WithLocation(7, 34));
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();
        }

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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);

            comp.VerifyDiagnostics(
                // (5,13): error CS0233: 'nint' does not have a predefined size, therefore sizeof can only be used in an unsafe context
                //         _ = sizeof(System.IntPtr);
                Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(System.IntPtr)").WithArguments("nint").WithLocation(5, 13),
                // (6,13): error CS0233: 'nuint' does not have a predefined size, therefore sizeof can only be used in an unsafe context
                //         _ = sizeof(System.UIntPtr);
                Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(System.UIntPtr)").WithArguments("nuint").WithLocation(6, 13),
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
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            int size = IntPtr.Size;
            var verifier = CompileAndVerify(comp, expectedOutput: IncludeExpectedOutput($"{size}{size}{size}{size}"), verify: Verification.FailsPEVerify);
            verifier.VerifyIL("Program.Main",
@"{
  // Code size       45 (0x2d)
  .maxstack  1
  IL_0000:  sizeof     ""nint""
  IL_0006:  call       ""void System.Console.Write(int)""
  IL_000b:  sizeof     ""nuint""
  IL_0011:  call       ""void System.Console.Write(int)""
  IL_0016:  sizeof     ""nint""
  IL_001c:  call       ""void System.Console.Write(int)""
  IL_0021:  sizeof     ""nuint""
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
        yield return sizeof(System.IntPtr);
        yield return sizeof(System.UIntPtr);
    }
}";
            // https://github.com/dotnet/roslyn/issues/73280 - should not be a langversion error since this remains an error in C# 13
            CreateCompilation(source, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70).VerifyDiagnostics(
                // (6,22): error CS8773: Feature 'ref and unsafe in async and iterator methods' is not available in C# 9.0. Please use language version 13.0 or greater.
                //         yield return sizeof(nint);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "sizeof(nint)").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(6, 22),
                // (7,22): error CS8773: Feature 'ref and unsafe in async and iterator methods' is not available in C# 9.0. Please use language version 13.0 or greater.
                //         yield return sizeof(nuint);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "sizeof(nuint)").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(7, 22),
                // (8,22): error CS8773: Feature 'ref and unsafe in async and iterator methods' is not available in C# 9.0. Please use language version 13.0 or greater.
                //         yield return sizeof(System.IntPtr);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "sizeof(System.IntPtr)").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(8, 22),
                // (9,22): error CS8773: Feature 'ref and unsafe in async and iterator methods' is not available in C# 9.0. Please use language version 13.0 or greater.
                //         yield return sizeof(System.UIntPtr);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "sizeof(System.UIntPtr)").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(9, 22)
                );
            CreateCompilation(source, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular12, targetFramework: TargetFramework.Net70).VerifyDiagnostics(
                // (6,22): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         yield return sizeof(nint);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "sizeof(nint)").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(6, 22),
                // (7,22): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         yield return sizeof(nuint);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "sizeof(nuint)").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(7, 22),
                // (8,22): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         yield return sizeof(System.IntPtr);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "sizeof(System.IntPtr)").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(8, 22),
                // (9,22): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         yield return sizeof(System.UIntPtr);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "sizeof(System.UIntPtr)").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(9, 22)
                );

            var expectedDiagnostics = new[]
            {
                // (6,22): error CS0233: 'nint' does not have a predefined size, therefore sizeof can only be used in an unsafe context
                //         yield return sizeof(nint);
                Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(nint)").WithArguments("nint").WithLocation(6, 22),
                // (7,22): error CS0233: 'nuint' does not have a predefined size, therefore sizeof can only be used in an unsafe context
                //         yield return sizeof(nuint);
                Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(nuint)").WithArguments("nuint").WithLocation(7, 22),
                // (8,22): error CS0233: 'nint' does not have a predefined size, therefore sizeof can only be used in an unsafe context
                //         yield return sizeof(System.IntPtr);
                Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(System.IntPtr)").WithArguments("nint").WithLocation(8, 22),
                // (9,22): error CS0233: 'nuint' does not have a predefined size, therefore sizeof can only be used in an unsafe context
                //         yield return sizeof(System.UIntPtr);
                Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(System.UIntPtr)").WithArguments("nuint").WithLocation(9, 22)
            };

            CreateCompilation(source, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular13, targetFramework: TargetFramework.Net70).VerifyDiagnostics(expectedDiagnostics);
            CreateCompilation(source, options: TestOptions.UnsafeReleaseDll, targetFramework: TargetFramework.Net70).VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void TypeOf()
        {
            var source =
@"using static System.Console;
class Program
{
    static void Main()
    {
        var t1 = typeof(nint);
        var t2 = typeof(nuint);
        var t3 = typeof(System.IntPtr);
        var t4 = typeof(System.UIntPtr);
        WriteLine(t1.FullName);
        WriteLine(t2.FullName);
        WriteLine((object)t1 == t2);
        WriteLine((object)t1 == t3);
        WriteLine((object)t2 == t4);
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            CompileAndVerify(comp, verify: Verification.FailsPEVerify, expectedOutput: IncludeExpectedOutput(
@"System.IntPtr
System.UIntPtr
False
True
True"));
        }

        [Fact]
        public void Volatile()
        {
            var source =
@"using System;
class Program
{
    static volatile IntPtr F1 = -1;
    static volatile UIntPtr F2 = 2;
    static IntPtr F() => F1 + (IntPtr)F2;
    static void Main()
    {
        System.Console.WriteLine(F());
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net70);
            var verifier = CompileAndVerify(comp, expectedOutput: IncludeExpectedOutput(@"1"), verify: Verification.FailsPEVerify);
            verifier.VerifyIL("Program.F",
@"{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  volatile.
  IL_0002:  ldsfld     ""nint Program.F1""
  IL_0007:  volatile.
  IL_0009:  ldsfld     ""nuint Program.F2""
  IL_000e:  add
  IL_000f:  ret
}");
        }

        [Fact]
        public void MultipleTypeRefs_01()
        {
            string source =
@"class Program
{
    static string F1(nint i)
    {
        return i.ToString();
    }
    static object F2(nint i)
    {
        return i;
    }
    static void Main()
    {
        System.Console.WriteLine(F1(-42));
        System.Console.WriteLine(F2(42));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            var verifier = CompileAndVerify(comp, verify: Verification.FailsPEVerify, expectedOutput: IncludeExpectedOutput(
@"-42
42"));
            verifier.VerifyIL("Program.F1",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""string nint.ToString()""
  IL_0007:  ret
}");
            verifier.VerifyIL("Program.F2",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""nint""
  IL_0006:  ret
}");
        }

        /// <summary>
        /// Verify there is the number of built in operators for { IntPtr, UIntPtr, IntPtr?, UIntPtr? }
        /// for each operator kind.
        /// </summary>
        [Fact]
        public void BuiltInOperators()
        {
            var comp = CreateCompilation("", parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);

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
                BinaryOperatorKind.UnsignedRightShift,
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
                    comp.BuiltInOperators.GetSimpleBuiltInOperators(operatorKind, builder, skipNativeIntegerOperators);
                    var operators = builder.ToImmutableAndFree();
                    int expectedSigned = skipNativeIntegerOperators ? 0 : 1;
                    int expectedUnsigned = skipNativeIntegerOperators ? 0 : (operatorKind == UnaryOperatorKind.UnaryMinus) ? 0 : 1;
                    verifyOperators(operators, (op, signed) => isNativeInt(op.OperandType, signed), expectedSigned, expectedUnsigned);
                    verifyOperators(operators, (op, signed) => isNullableNativeInt(op.OperandType, signed), expectedSigned, expectedUnsigned);
                }

                static void verifyBinaryOperators(CSharpCompilation comp, BinaryOperatorKind operatorKind, bool skipNativeIntegerOperators)
                {
                    var builder = ArrayBuilder<BinaryOperatorSignature>.GetInstance();
                    comp.BuiltInOperators.GetSimpleBuiltInOperators(operatorKind, builder, skipNativeIntegerOperators);
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
                    return type.SpecialType == (signed ? SpecialType.System_IntPtr : SpecialType.System_UIntPtr);
                }

                static bool isNullableNativeInt(TypeSymbol type, bool signed)
                {
                    return type.IsNullableType() && isNativeInt(type.GetNullableUnderlyingType(), signed);
                }
            }
        }

        [Theory, CombinatorialData]
        public void BuiltInConversions_NativeIntegers(bool useCompilationReference, bool useLatest, bool useSystemTypes)
        {
            var nintType = useSystemTypes ? "System.IntPtr" : "nint";
            var nuintType = useSystemTypes ? "System.UIntPtr" : "nuint";

            var sourceA = $$"""
public class A
{
    public static {{nintType}} F1;
    public static {{nuintType}} F2;
    public static {{nintType}}? F3;
    public static {{nuintType}}? F4;
}
""";
            var comp = CreateCompilation(sourceA, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
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
            comp = CreateCompilation(sourceB, references: new[] { AsReference(comp, useCompilationReference) }, parseOptions: useLatest ? TestOptions.Regular9 : TestOptions.Regular8, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, verify: Verification.FailsPEVerify);
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
  IL_0014:  call       ""readonly bool nint?.HasValue.get""
  IL_0019:  brfalse.s  IL_0023
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       ""readonly nint nint?.GetValueOrDefault()""
  IL_0022:  pop
  IL_0023:  ldsfld     ""nuint? A.F4""
  IL_0028:  stloc.1
  IL_0029:  ldloca.s   V_1
  IL_002b:  call       ""readonly bool nuint?.HasValue.get""
  IL_0030:  brfalse.s  IL_003a
  IL_0032:  ldloca.s   V_1
  IL_0034:  call       ""readonly nuint nuint?.GetValueOrDefault()""
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
  IL_0012:  call       ""readonly bool int?.HasValue.get""
  IL_0017:  brtrue.s   IL_0024
  IL_0019:  ldloca.s   V_1
  IL_001b:  initobj    ""nint?""
  IL_0021:  ldloc.1
  IL_0022:  br.s       IL_0031
  IL_0024:  ldloca.s   V_0
  IL_0026:  call       ""readonly int int?.GetValueOrDefault()""
  IL_002b:  conv.i
  IL_002c:  newobj     ""nint?..ctor(nint)""
  IL_0031:  stsfld     ""nint? A.F3""
  IL_0036:  ldarg.3
  IL_0037:  stloc.2
  IL_0038:  ldloca.s   V_2
  IL_003a:  call       ""readonly bool uint?.HasValue.get""
  IL_003f:  brtrue.s   IL_004c
  IL_0041:  ldloca.s   V_3
  IL_0043:  initobj    ""nuint?""
  IL_0049:  ldloc.3
  IL_004a:  br.s       IL_0059
  IL_004c:  ldloca.s   V_2
  IL_004e:  call       ""readonly uint uint?.GetValueOrDefault()""
  IL_0053:  conv.u
  IL_0054:  newobj     ""nuint?..ctor(nuint)""
  IL_0059:  stsfld     ""nuint? A.F4""
  IL_005e:  ret
}");
        }

        [Theory, CombinatorialData]
        public void BuiltInOperators_NativeIntegers(bool useCSharp9, bool useCompilationReference, bool useSystemTypes)
        {
            var nintType = useSystemTypes ? "System.IntPtr" : "nint";
            var nuintType = useSystemTypes ? "System.UIntPtr" : "nuint";

            var sourceA = $$"""
public class A
{
    public static {{nintType}} F1;
    public static {{nuintType}} F2;
    public static {{nintType}}? F3;
    public static {{nuintType}}? F4;
}
""";
            var comp = CreateCompilation(sourceA, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();
            var refA = AsReference(comp, useCompilationReference);

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
            comp = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: useCSharp9 ? TestOptions.Regular9 : TestOptions.Regular8, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, verify: Verification.FailsPEVerify);
            verifier.VerifyIL("B.Main",
@"{
  // Code size      247 (0xf7)
  .maxstack  2
  .locals init (nint? V_0,
                nint? V_1,
                nuint? V_2,
                nuint? V_3,
                nint V_4,
                nuint V_5)
  IL_0000:  ldsfld     ""nint A.F1""
  IL_0005:  neg
  IL_0006:  stsfld     ""nint A.F1""
  IL_000b:  ldsfld     ""nuint A.F2""
  IL_0010:  stsfld     ""nuint A.F2""
  IL_0015:  ldsfld     ""nint? A.F3""
  IL_001a:  stloc.0
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       ""readonly bool nint?.HasValue.get""
  IL_0022:  brtrue.s   IL_002f
  IL_0024:  ldloca.s   V_1
  IL_0026:  initobj    ""nint?""
  IL_002c:  ldloc.1
  IL_002d:  br.s       IL_003c
  IL_002f:  ldloca.s   V_0
  IL_0031:  call       ""readonly nint nint?.GetValueOrDefault()""
  IL_0036:  neg
  IL_0037:  newobj     ""nint?..ctor(nint)""
  IL_003c:  stsfld     ""nint? A.F3""
  IL_0041:  ldsfld     ""nuint? A.F4""
  IL_0046:  stloc.2
  IL_0047:  ldloca.s   V_2
  IL_0049:  call       ""readonly bool nuint?.HasValue.get""
  IL_004e:  brtrue.s   IL_005b
  IL_0050:  ldloca.s   V_3
  IL_0052:  initobj    ""nuint?""
  IL_0058:  ldloc.3
  IL_0059:  br.s       IL_0067
  IL_005b:  ldloca.s   V_2
  IL_005d:  call       ""readonly nuint nuint?.GetValueOrDefault()""
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
  IL_009b:  call       ""readonly bool nint?.HasValue.get""
  IL_00a0:  brtrue.s   IL_00ad
  IL_00a2:  ldloca.s   V_1
  IL_00a4:  initobj    ""nint?""
  IL_00aa:  ldloc.1
  IL_00ab:  br.s       IL_00bc
  IL_00ad:  ldloca.s   V_0
  IL_00af:  call       ""readonly nint nint?.GetValueOrDefault()""
  IL_00b4:  ldloc.s    V_4
  IL_00b6:  mul
  IL_00b7:  newobj     ""nint?..ctor(nint)""
  IL_00bc:  stsfld     ""nint? A.F3""
  IL_00c1:  ldsfld     ""nuint? A.F4""
  IL_00c6:  stloc.2
  IL_00c7:  ldsfld     ""nuint A.F2""
  IL_00cc:  stloc.s    V_5
  IL_00ce:  ldloca.s   V_2
  IL_00d0:  call       ""readonly bool nuint?.HasValue.get""
  IL_00d5:  brtrue.s   IL_00e2
  IL_00d7:  ldloca.s   V_3
  IL_00d9:  initobj    ""nuint?""
  IL_00df:  ldloc.3
  IL_00e0:  br.s       IL_00f1
  IL_00e2:  ldloca.s   V_2
  IL_00e4:  call       ""readonly nuint nuint?.GetValueOrDefault()""
  IL_00e9:  ldloc.s    V_5
  IL_00eb:  div.un
  IL_00ec:  newobj     ""nuint?..ctor(nuint)""
  IL_00f1:  stsfld     ""nuint? A.F4""
  IL_00f6:  ret
}");
        }

        [Fact, WorkItem(63860, "https://github.com/dotnet/roslyn/issues/63860")]
        public void IntPtrOperator_OnPlatformWithNumericIntPtr()
        {
            var source = """
using System;

class C
{
    public static void M()
    {
        IntPtr a = default;
        bool b = a == IntPtr.Zero;
        a += 1;
        _ = a + 1;
    }
}
""";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(63860, "https://github.com/dotnet/roslyn/issues/63860")]
        public void IntPtrOperator_OnPlatformWithoutNumericIntPtr()
        {
            var source = """
using System;

class C
{
    public static void M()
    {
        IntPtr a = default;
        bool b = a == IntPtr.Zero;
        a += 1;
        _ = a + 1;
    }
}
""";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void SemanticModel_UnaryOperators(bool lifted)
        {
            string typeQualifier = lifted ? "?" : "";
            var source =
$@"class Program
{{
    static void F(nint{typeQualifier} x, nuint{typeQualifier} y)
    {{
        _ = +x;
        _ = -x;
        _ = ~x;
        _ = +y;
        _ = ~y;
    }}
}}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics();
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetRoot().DescendantNodes().OfType<PrefixUnaryExpressionSyntax>();
            var actualOperators = nodes.Select(n => model.GetSymbolInfo(n).Symbol.ToTestDisplayString()).ToArray();
            var expectedOperators = new[]
            {
            "nint nint.op_UnaryPlus(nint value)",
            "nint nint.op_UnaryNegation(nint value)",
            "nint nint.op_OnesComplement(nint value)",
            "nuint nuint.op_UnaryPlus(nuint value)",
            "nuint nuint.op_OnesComplement(nuint value)",
        };
            AssertEx.Equal(expectedOperators, actualOperators);
        }

        [Theory]
        [InlineData("nint", false)]
        [InlineData("nuint", false)]
        [InlineData("nint", true)]
        [InlineData("nuint", true)]
        [InlineData("System.IntPtr", false)]
        [InlineData("System.UIntPtr", false)]
        [InlineData("System.IntPtr", true)]
        [InlineData("System.UIntPtr", true)]
        public void SemanticModel_BinaryOperators(string type, bool lifted)
        {
            string typeQualifier = lifted ? "?" : "";
            var source =
$@"class Program
{{
    static void F({type}{typeQualifier} x, {type}{typeQualifier} y)
    {{
        _ = x + y;
        _ = x - y;
        _ = x * y;
        _ = x / y;
        _ = x % y;
        _ = x < y;
        _ = x <= y;
        _ = x > y;
        _ = x >= y;
        _ = x == y;
        _ = x != y;
        _ = x & y;
        _ = x | y;
        _ = x ^ y;
        _ = x << 1;
        _ = x >> 1;
    }}
}}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics();
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>();
            var actualOperators = nodes.Select(n => model.GetSymbolInfo(n).Symbol.ToTestDisplayString()).ToArray();
            var nativeType = AsNative(type);
            var expectedOperators = new[]
            {
                $"{nativeType} {nativeType}.op_Addition({nativeType} left, {nativeType} right)",
                $"{nativeType} {nativeType}.op_Subtraction({nativeType} left, {nativeType} right)",
                $"{nativeType} {nativeType}.op_Multiply({nativeType} left, {nativeType} right)",
                $"{nativeType} {nativeType}.op_Division({nativeType} left, {nativeType} right)",
                $"{nativeType} {nativeType}.op_Modulus({nativeType} left, {nativeType} right)",
                $"System.Boolean {nativeType}.op_LessThan({nativeType} left, {nativeType} right)",
                $"System.Boolean {nativeType}.op_LessThanOrEqual({nativeType} left, {nativeType} right)",
                $"System.Boolean {nativeType}.op_GreaterThan({nativeType} left, {nativeType} right)",
                $"System.Boolean {nativeType}.op_GreaterThanOrEqual({nativeType} left, {nativeType} right)",
                $"System.Boolean {nativeType}.op_Equality({nativeType} left, {nativeType} right)",
                $"System.Boolean {nativeType}.op_Inequality({nativeType} left, {nativeType} right)",
                $"{nativeType} {nativeType}.op_BitwiseAnd({nativeType} left, {nativeType} right)",
                $"{nativeType} {nativeType}.op_BitwiseOr({nativeType} left, {nativeType} right)",
                $"{nativeType} {nativeType}.op_ExclusiveOr({nativeType} left, {nativeType} right)",
                $"{nativeType} {nativeType}.op_LeftShift({nativeType} left, System.Int32 right)",
                $"{nativeType} {nativeType}.op_RightShift({nativeType} left, System.Int32 right)",
            };
            AssertEx.Equal(expectedOperators, actualOperators);
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
        System.IntPtr i;
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
        System.UIntPtr u;
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);

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
                // (30,17): error CS0266: Cannot implicitly convert type 'sbyte' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //             u = sbyte.MaxValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "sbyte.MaxValue").WithArguments("sbyte", "nuint").WithLocation(30, 17),
                // (33,17): error CS0266: Cannot implicitly convert type 'short' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //             u = short.MaxValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "short.MaxValue").WithArguments("short", "nuint").WithLocation(33, 17),
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
        const System.IntPtr n = (System.IntPtr)int.MaxValue;
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
        const System.UIntPtr nu = (System.UIntPtr)uint.MaxValue;
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);

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
        public void ConstantConversions_01()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        const long x = 0xFFFFFFFFFFFFFFFL;
        const IntPtr y = checked((IntPtr)x);
        Console.WriteLine(y);
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);

            comp.VerifyDiagnostics(
                // (7,26): error CS0133: The expression being assigned to 'y' must be constant
                //         const IntPtr y = checked((IntPtr)x);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "checked((IntPtr)x)").WithArguments("y").WithLocation(7, 26),
                // (7,34): warning CS8778: Constant value '1152921504606846975' may overflow 'nint' at runtime (use 'unchecked' syntax to override)
                //         const IntPtr y = checked((IntPtr)x);
                Diagnostic(ErrorCode.WRN_ConstOutOfRangeChecked, "(IntPtr)x").WithArguments("1152921504606846975", "nint").WithLocation(7, 34));

            source =
@"using System;
class Program
{
    static void Main()
    {
        const long x = 0xFFFFFFFFFFFFFFFL;
        try
        {
            nint y = checked((nint)x);
            Console.WriteLine(y);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.GetType());
        }
    }
}";
            comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(
                // (9,30): warning CS8778: Constant value '1152921504606846975' may overflow 'nint' at runtime (use 'unchecked' syntax to override)
                //             nint y = checked((nint)x);
                Diagnostic(ErrorCode.WRN_ConstOutOfRangeChecked, "(nint)x").WithArguments("1152921504606846975", "nint").WithLocation(9, 30));
            CompileAndVerify(comp, expectedOutput: IncludeExpectedOutput(IntPtr.Size == 4 ? "System.OverflowException" : "1152921504606846975"), verify: Verification.FailsPEVerify);
        }

        [WorkItem(45531, "https://github.com/dotnet/roslyn/issues/45531")]
        [Fact]
        public void ConstantConversions_02()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        const long x = 0xFFFFFFFFFFFFFFFL;
        const IntPtr y = unchecked((IntPtr)x);
        Console.WriteLine(y);
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);

            comp.VerifyDiagnostics(
                // (7,26): error CS0133: The expression being assigned to 'y' must be constant
                //         const IntPtr y = unchecked((IntPtr)x);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "unchecked((IntPtr)x)").WithArguments("y").WithLocation(7, 26));

            source =
@"using System;
class Program
{
    static void Main()
    {
        const long x = 0xFFFFFFFFFFFFFFFL;
        IntPtr y = unchecked((IntPtr)x);
        Console.WriteLine(y);
    }
}";
            comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, verify: Verification.FailsPEVerify, expectedOutput: IncludeExpectedOutput(IntPtr.Size == 4 ? "-1" : "1152921504606846975"));
        }

        [Fact]
        public void ConstantConversions_03()
        {
            using var _ = new EnsureInvariantCulture();

            constantConversions("sbyte", "IntPtr", "-1", null, "-1", "-1", null, "-1", "-1");
            constantConversions("sbyte", "IntPtr", "sbyte.MinValue", null, "-128", "-128", null, "-128", "-128");
            constantConversions("sbyte", "IntPtr", "sbyte.MaxValue", null, "127", "127", null, "127", "127");
            constantConversions("byte", "IntPtr", "byte.MaxValue", null, "255", "255", null, "255", "255");
            constantConversions("short", "IntPtr", "-1", null, "-1", "-1", null, "-1", "-1");
            constantConversions("short", "IntPtr", "short.MinValue", null, "-32768", "-32768", null, "-32768", "-32768");
            constantConversions("short", "IntPtr", "short.MaxValue", null, "32767", "32767", null, "32767", "32767");
            constantConversions("ushort", "IntPtr", "ushort.MaxValue", null, "65535", "65535", null, "65535", "65535");
            constantConversions("char", "IntPtr", "char.MaxValue", null, "65535", "65535", null, "65535", "65535");
            constantConversions("int", "IntPtr", "int.MinValue", null, "-2147483648", "-2147483648", null, "-2147483648", "-2147483648");
            constantConversions("int", "IntPtr", "int.MaxValue", null, "2147483647", "2147483647", null, "2147483647", "2147483647");
            constantConversions("uint", "IntPtr", "(int.MaxValue + 1U)", warningOutOfRangeChecked("IntPtr", "2147483648"), "System.OverflowException", "2147483648", null, "-2147483648", "2147483648");
            constantConversions("uint", "IntPtr", "uint.MaxValue", warningOutOfRangeChecked("IntPtr", "4294967295"), "System.OverflowException", "4294967295", null, "-1", "4294967295");
            constantConversions("long", "IntPtr", "(int.MinValue - 1L)", warningOutOfRangeChecked("IntPtr", "-2147483649"), "System.OverflowException", "-2147483649", null, "2147483647", "-2147483649");
            constantConversions("long", "IntPtr", "(int.MaxValue + 1L)", warningOutOfRangeChecked("IntPtr", "2147483648"), "System.OverflowException", "2147483648", null, "-2147483648", "2147483648");
            constantConversions("long", "IntPtr", "long.MinValue", warningOutOfRangeChecked("IntPtr", "-9223372036854775808"), "System.OverflowException", "-9223372036854775808", null, "0", "-9223372036854775808");
            constantConversions("long", "IntPtr", "long.MaxValue", warningOutOfRangeChecked("IntPtr", "9223372036854775807"), "System.OverflowException", "9223372036854775807", null, "-1", "9223372036854775807");
            constantConversions("ulong", "IntPtr", "(int.MaxValue + 1UL)", warningOutOfRangeChecked("IntPtr", "2147483648"), "System.OverflowException", "2147483648", null, "-2147483648", "2147483648");
            constantConversions("ulong", "IntPtr", "ulong.MaxValue", errorOutOfRangeChecked("IntPtr", "18446744073709551615"), "System.OverflowException", "System.OverflowException", null, "-1", "-1");
            constantConversions("decimal", "IntPtr", "(int.MinValue - 1M)", errorOutOfRange("IntPtr", "-2147483649M"), "System.OverflowException", "-2147483649", errorOutOfRange("IntPtr", "-2147483649M"), "2147483647", "-2147483649");
            constantConversions("decimal", "IntPtr", "(int.MaxValue + 1M)", errorOutOfRange("IntPtr", "2147483648M"), "System.OverflowException", "2147483648", errorOutOfRange("IntPtr", "2147483648M"), "-2147483648", "2147483648");
            constantConversions("decimal", "IntPtr", "decimal.MinValue", errorOutOfRange("IntPtr", "-79228162514264337593543950335M"), "System.OverflowException", "System.OverflowException", errorOutOfRange("IntPtr", "-79228162514264337593543950335M"), "-1", "-1");
            constantConversions("decimal", "IntPtr", "decimal.MaxValue", errorOutOfRange("IntPtr", "79228162514264337593543950335M"), "System.OverflowException", "System.OverflowException", errorOutOfRange("IntPtr", "79228162514264337593543950335M"), "-1", "-1");
            constantConversions("IntPtr", "IntPtr", "int.MinValue", null, "-2147483648", "-2147483648", null, "-2147483648", "-2147483648");
            constantConversions("IntPtr", "IntPtr", "int.MaxValue", null, "2147483647", "2147483647", null, "2147483647", "2147483647");
            constantConversions("UIntPtr", "IntPtr", "(int.MaxValue + (nuint)1)", warningOutOfRangeChecked("IntPtr", "2147483648"), "System.OverflowException", "2147483648", null, "-2147483648", "2147483648");
            constantConversions("sbyte", "UIntPtr", "-1", errorOutOfRangeChecked("UIntPtr", "-1"), "System.OverflowException", "System.OverflowException", null, "4294967295", "18446744073709551615");
            constantConversions("sbyte", "UIntPtr", "sbyte.MinValue", errorOutOfRangeChecked("UIntPtr", "-128"), "System.OverflowException", "System.OverflowException", null, "4294967168", "18446744073709551488");
            constantConversions("sbyte", "UIntPtr", "sbyte.MaxValue", null, "127", "127", null, "127", "127");
            constantConversions("byte", "UIntPtr", "byte.MaxValue", null, "255", "255", null, "255", "255");
            constantConversions("short", "UIntPtr", "-1", errorOutOfRangeChecked("UIntPtr", "-1"), "System.OverflowException", "System.OverflowException", null, "4294967295", "18446744073709551615");
            constantConversions("short", "UIntPtr", "short.MinValue", errorOutOfRangeChecked("UIntPtr", "-32768"), "System.OverflowException", "System.OverflowException", null, "4294934528", "18446744073709518848");
            constantConversions("short", "UIntPtr", "short.MaxValue", null, "32767", "32767", null, "32767", "32767");
            constantConversions("ushort", "UIntPtr", "ushort.MaxValue", null, "65535", "65535", null, "65535", "65535");
            constantConversions("char", "UIntPtr", "char.MaxValue", null, "65535", "65535", null, "65535", "65535");
            constantConversions("int", "UIntPtr", "-1", errorOutOfRangeChecked("UIntPtr", "-1"), "System.OverflowException", "System.OverflowException", null, "4294967295", "18446744073709551615");
            constantConversions("int", "UIntPtr", "int.MinValue", errorOutOfRangeChecked("UIntPtr", "-2147483648"), "System.OverflowException", "System.OverflowException", null, "2147483648", "18446744071562067968");
            constantConversions("int", "UIntPtr", "int.MaxValue", null, "2147483647", "2147483647", null, "2147483647", "2147483647");
            constantConversions("uint", "UIntPtr", "uint.MaxValue", null, "4294967295", "4294967295", null, "4294967295", "4294967295");
            constantConversions("long", "UIntPtr", "-1", errorOutOfRangeChecked("UIntPtr", "-1"), "System.OverflowException", "System.OverflowException", null, "4294967295", "18446744073709551615");
            constantConversions("long", "UIntPtr", "uint.MaxValue + 1L", warningOutOfRangeChecked("UIntPtr", "4294967296"), "System.OverflowException", "4294967296", null, "0", "4294967296");
            constantConversions("long", "UIntPtr", "long.MinValue", errorOutOfRangeChecked("UIntPtr", "-9223372036854775808"), "System.OverflowException", "System.OverflowException", null, "0", "9223372036854775808");
            constantConversions("long", "UIntPtr", "long.MaxValue", warningOutOfRangeChecked("UIntPtr", "9223372036854775807"), "System.OverflowException", "9223372036854775807", null, "4294967295", "9223372036854775807");
            constantConversions("ulong", "UIntPtr", "uint.MaxValue + 1UL", warningOutOfRangeChecked("UIntPtr", "4294967296"), "System.OverflowException", "4294967296", null, "0", "4294967296");
            constantConversions("ulong", "UIntPtr", "ulong.MaxValue", warningOutOfRangeChecked("UIntPtr", "18446744073709551615"), "System.OverflowException", "18446744073709551615", null, "4294967295", "18446744073709551615");
            constantConversions("decimal", "UIntPtr", "-1", errorOutOfRange("UIntPtr", "-1M"), "System.OverflowException", "System.OverflowException", errorOutOfRange("UIntPtr", "-1M"), "System.OverflowException", "System.OverflowException");
            constantConversions("decimal", "UIntPtr", "(uint.MaxValue + 1M)", errorOutOfRange("UIntPtr", "4294967296M"), "System.OverflowException", "4294967296", errorOutOfRange("UIntPtr", "4294967296M"), "-1", "4294967296");
            constantConversions("decimal", "UIntPtr", "decimal.MinValue", errorOutOfRange("UIntPtr", "-79228162514264337593543950335M"), "System.OverflowException", "System.OverflowException", errorOutOfRange("UIntPtr", "-79228162514264337593543950335M"), "-1", "-1");
            constantConversions("decimal", "UIntPtr", "decimal.MaxValue", errorOutOfRange("UIntPtr", "79228162514264337593543950335M"), "System.OverflowException", "System.OverflowException", errorOutOfRange("UIntPtr", "79228162514264337593543950335M"), "-1", "-1");
            constantConversions("IntPtr", "UIntPtr", "-1", errorOutOfRangeChecked("UIntPtr", "-1"), "System.OverflowException", "System.OverflowException", null, "4294967295", "18446744073709551615");
            constantConversions("UIntPtr", "UIntPtr", "uint.MaxValue", null, "4294967295", "4294967295", null, "4294967295", "4294967295");
            if (!ExecutionConditionUtil.IsWindowsDesktop)
            {
                // There are differences in floating point precision across platforms
                // so floating point tests are limited to one platform.
                return;
            }
            constantConversions("float", "IntPtr", "(int.MinValue - 10000F)", warningOutOfRangeChecked("IntPtr", "-2.147494E+09"), "System.OverflowException", "-2147493632", null, "-2147483648", "-2147493632");
            constantConversions("float", "IntPtr", "(int.MaxValue + 10000F)", warningOutOfRangeChecked("IntPtr", "2.147494E+09"), "System.OverflowException", "2147493632", null, "-2147483648", "2147493632");
            constantConversions("float", "IntPtr", "float.MinValue", errorOutOfRangeChecked("IntPtr", "-3.402823E+38"), "System.OverflowException", "System.OverflowException", null, "-2147483648", "-9223372036854775808");
            constantConversions("float", "IntPtr", "float.MaxValue", errorOutOfRangeChecked("IntPtr", "3.402823E+38"), "System.OverflowException", "System.OverflowException", null, "-2147483648", "-9223372036854775808");
            constantConversions("double", "IntPtr", "(int.MinValue - 1D)", warningOutOfRangeChecked("IntPtr", "-2147483649"), "System.OverflowException", "-2147483649", null, "-2147483648", "-2147483649");
            constantConversions("double", "IntPtr", "(int.MaxValue + 1D)", warningOutOfRangeChecked("IntPtr", "2147483648"), "System.OverflowException", "2147483648", null, "-2147483648", "2147483648");
            constantConversions("double", "IntPtr", "double.MinValue", errorOutOfRangeChecked("IntPtr", "-1.79769313486232E+308"), "System.OverflowException", "System.OverflowException", null, "-2147483648", "-9223372036854775808");
            constantConversions("double", "IntPtr", "double.MaxValue", errorOutOfRangeChecked("IntPtr", "1.79769313486232E+308"), "System.OverflowException", "System.OverflowException", null, "-2147483648", "-9223372036854775808");
            constantConversions("float", "UIntPtr", "-1", errorOutOfRangeChecked("UIntPtr", "-1"), "System.OverflowException", "System.OverflowException", null, "4294967295", "18446744073709551615");
            constantConversions("float", "UIntPtr", "(uint.MaxValue + 1F)", warningOutOfRangeChecked("UIntPtr", "4.294967E+09"), "System.OverflowException", "4294967296", null, "0", "4294967296");
            constantConversions("float", "UIntPtr", "float.MinValue", errorOutOfRangeChecked("UIntPtr", "-3.402823E+38"), "System.OverflowException", "System.OverflowException", null, "0", "9223372036854775808");
            constantConversions("float", "UIntPtr", "float.MaxValue", errorOutOfRangeChecked("UIntPtr", "3.402823E+38"), "System.OverflowException", "System.OverflowException", null, "0", "0");
            constantConversions("double", "UIntPtr", "-1", errorOutOfRangeChecked("UIntPtr", "-1"), "System.OverflowException", "System.OverflowException", null, "4294967295", "18446744073709551615");
            constantConversions("double", "UIntPtr", "(uint.MaxValue + 1D)", warningOutOfRangeChecked("UIntPtr", "4294967296"), "System.OverflowException", "4294967296", null, "0", "4294967296");
            constantConversions("double", "UIntPtr", "double.MinValue", errorOutOfRangeChecked("UIntPtr", "-1.79769313486232E+308"), "System.OverflowException", "System.OverflowException", null, "0", "9223372036854775808");
            constantConversions("double", "UIntPtr", "double.MaxValue", errorOutOfRangeChecked("UIntPtr", "1.79769313486232E+308"), "System.OverflowException", "System.OverflowException", null, "0", "0");

            static DiagnosticDescription errorOutOfRangeChecked(string destinationType, string value) => Diagnostic(ErrorCode.ERR_ConstOutOfRangeChecked, $"({destinationType})x").WithArguments(value, AsNative(destinationType));
            static DiagnosticDescription errorOutOfRange(string destinationType, string value) => Diagnostic(ErrorCode.ERR_ConstOutOfRange, $"({destinationType})x").WithArguments(value, AsNative(destinationType));
            static DiagnosticDescription warningOutOfRangeChecked(string destinationType, string value) => Diagnostic(ErrorCode.WRN_ConstOutOfRangeChecked, $"({destinationType})x").WithArguments(value, AsNative(destinationType));

            void constantConversions(string sourceType, string destinationType, string sourceValue, DiagnosticDescription checkedError, string checked32, string checked64, DiagnosticDescription uncheckedError, string unchecked32, string unchecked64)
            {
                constantConversion(sourceType, destinationType, sourceValue, useChecked: true, checkedError, IntPtr.Size == 4 ? checked32 : checked64);
                constantConversion(sourceType, destinationType, sourceValue, useChecked: false, uncheckedError, IntPtr.Size == 4 ? unchecked32 : unchecked64);
            }

            void constantConversion(string sourceType, string destinationType, string sourceValue, bool useChecked, DiagnosticDescription expectedError, string expectedOutput)
            {
                var source =
$@"using System;
class Program
{{
    static void Main()
    {{
        const {sourceType} x = {sourceValue};
        object y;
        try
        {{
            y = {(useChecked ? "checked" : "unchecked")}(({destinationType})x);
        }}
        catch (Exception e)
        {{
            y = e.GetType();
        }}
        Console.Write(y);
    }}
}}";
                var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);

                comp.VerifyDiagnostics(expectedError is null ? Array.Empty<DiagnosticDescription>() : new[] { expectedError });
                if (expectedError == null || ErrorFacts.IsWarning((ErrorCode)expectedError.Code))
                {
                    CompileAndVerify(comp, verify: Verification.FailsPEVerify, expectedOutput: IncludeExpectedOutput(expectedOutput));
                }
            }
        }

        [Theory]
        [InlineData("nint")]
        [InlineData("System.IntPtr")]
        public void Constants_NInt(string type)
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
    static void F({type} n)
    {{
        System.Console.WriteLine(n);
    }}
}}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
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
            var verifier = CompileAndVerify(comp, expectedOutput: IncludeExpectedOutput(expectedOutput), verify: Verification.FailsPEVerify);
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

        [Theory]
        [InlineData("nuint")]
        [InlineData("System.UIntPtr")]
        public void Constants_NUInt(string type)
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
        F(byte.MaxValue);
        F(char.MaxValue);
        F(ushort.MaxValue);
        F(int.MaxValue);
        F({(uint)int.MaxValue + 1});
        F(uint.MaxValue);
    }}
    static void F({type} n)
    {{
        System.Console.WriteLine(n);
    }}
}}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
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
255
65535
65535
2147483647
2147483648
4294967295";
            var verifier = CompileAndVerify(comp, expectedOutput: IncludeExpectedOutput(expectedOutput), verify: Verification.FailsPEVerify);
            string expectedIL =
@"{
  // Code size      141 (0x8d)
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
  IL_004e:  ldc.i4     0xff
  IL_0053:  conv.i
  IL_0054:  call       ""void Program.F(nuint)""
  IL_0059:  ldc.i4     0xffff
  IL_005e:  conv.i
  IL_005f:  call       ""void Program.F(nuint)""
  IL_0064:  ldc.i4     0xffff
  IL_0069:  conv.i
  IL_006a:  call       ""void Program.F(nuint)""
  IL_006f:  ldc.i4     0x7fffffff
  IL_0074:  conv.i
  IL_0075:  call       ""void Program.F(nuint)""
  IL_007a:  ldc.i4     0x80000000
  IL_007f:  conv.u
  IL_0080:  call       ""void Program.F(nuint)""
  IL_0085:  ldc.i4.m1
  IL_0086:  conv.u
  IL_0087:  call       ""void Program.F(nuint)""
  IL_008c:  ret
}";
            verifier.VerifyIL("Program.Main", expectedIL);
        }

        [Fact]
        public void Constants_ConvertToUnsigned()
        {
            string source =
@"class Program
{
    static void Main()
    {
        F<ushort>(sbyte.MaxValue); // 1
        F<ushort>(short.MaxValue); // 2
        F<ushort>(int.MaxValue); // 3
        F<ushort>(long.MaxValue); // 4
        F<uint>(sbyte.MaxValue); // 5
        F<uint>(short.MaxValue); // 6
        F<uint>(int.MaxValue); // 7
        F<uint>(long.MaxValue); // 8
        F<System.UIntPtr>(sbyte.MaxValue); // 9
        F<System.UIntPtr>(short.MaxValue); // 10
        F<System.UIntPtr>(int.MaxValue); // 11
        F<System.UIntPtr>(long.MaxValue); // 12
        F<nuint>(sbyte.MaxValue); // 13
        F<nuint>(short.MaxValue); // 14
        F<nuint>(int.MaxValue); // 15
        F<nuint>(long.MaxValue); // 16
        F<ulong>(sbyte.MaxValue); // 17
        F<ulong>(short.MaxValue); // 18
        F<ulong>(int.MaxValue); // 19
        F<ulong>(long.MaxValue); // 20
    }
    static void F<T>(T n)
    {
    }
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(
                // (5,19): error CS1503: Argument 1: cannot convert from 'sbyte' to 'ushort'
                //         F<ushort>(sbyte.MaxValue); // 1
                Diagnostic(ErrorCode.ERR_BadArgType, "sbyte.MaxValue").WithArguments("1", "sbyte", "ushort").WithLocation(5, 19),
                // (6,19): error CS1503: Argument 1: cannot convert from 'short' to 'ushort'
                //         F<ushort>(short.MaxValue); // 2
                Diagnostic(ErrorCode.ERR_BadArgType, "short.MaxValue").WithArguments("1", "short", "ushort").WithLocation(6, 19),
                // (7,19): error CS1503: Argument 1: cannot convert from 'int' to 'ushort'
                //         F<ushort>(int.MaxValue); // 3
                Diagnostic(ErrorCode.ERR_BadArgType, "int.MaxValue").WithArguments("1", "int", "ushort").WithLocation(7, 19),
                // (8,19): error CS1503: Argument 1: cannot convert from 'long' to 'ushort'
                //         F<ushort>(long.MaxValue); // 4
                Diagnostic(ErrorCode.ERR_BadArgType, "long.MaxValue").WithArguments("1", "long", "ushort").WithLocation(8, 19),
                // (9,17): error CS1503: Argument 1: cannot convert from 'sbyte' to 'uint'
                //         F<uint>(sbyte.MaxValue); // 5
                Diagnostic(ErrorCode.ERR_BadArgType, "sbyte.MaxValue").WithArguments("1", "sbyte", "uint").WithLocation(9, 17),
                // (10,17): error CS1503: Argument 1: cannot convert from 'short' to 'uint'
                //         F<uint>(short.MaxValue); // 6
                Diagnostic(ErrorCode.ERR_BadArgType, "short.MaxValue").WithArguments("1", "short", "uint").WithLocation(10, 17),
                // (12,17): error CS1503: Argument 1: cannot convert from 'long' to 'uint'
                //         F<uint>(long.MaxValue); // 8
                Diagnostic(ErrorCode.ERR_BadArgType, "long.MaxValue").WithArguments("1", "long", "uint").WithLocation(12, 17),
                // (13,27): error CS1503: Argument 1: cannot convert from 'sbyte' to 'nuint'
                //         F<System.UIntPtr>(sbyte.MaxValue); // 9
                Diagnostic(ErrorCode.ERR_BadArgType, "sbyte.MaxValue").WithArguments("1", "sbyte", "nuint").WithLocation(13, 27),
                // (14,27): error CS1503: Argument 1: cannot convert from 'short' to 'nuint'
                //         F<System.UIntPtr>(short.MaxValue); // 10
                Diagnostic(ErrorCode.ERR_BadArgType, "short.MaxValue").WithArguments("1", "short", "nuint").WithLocation(14, 27),
                // (16,27): error CS1503: Argument 1: cannot convert from 'long' to 'nuint'
                //         F<System.UIntPtr>(long.MaxValue); // 12
                Diagnostic(ErrorCode.ERR_BadArgType, "long.MaxValue").WithArguments("1", "long", "nuint").WithLocation(16, 27),
                // (17,18): error CS1503: Argument 1: cannot convert from 'sbyte' to 'nuint'
                //         F<nuint>(sbyte.MaxValue); // 13
                Diagnostic(ErrorCode.ERR_BadArgType, "sbyte.MaxValue").WithArguments("1", "sbyte", "nuint").WithLocation(17, 18),
                // (18,18): error CS1503: Argument 1: cannot convert from 'short' to 'nuint'
                //         F<nuint>(short.MaxValue); // 14
                Diagnostic(ErrorCode.ERR_BadArgType, "short.MaxValue").WithArguments("1", "short", "nuint").WithLocation(18, 18),
                // (20,18): error CS1503: Argument 1: cannot convert from 'long' to 'nuint'
                //         F<nuint>(long.MaxValue); // 16
                Diagnostic(ErrorCode.ERR_BadArgType, "long.MaxValue").WithArguments("1", "long", "nuint").WithLocation(20, 18),
                // (21,18): error CS1503: Argument 1: cannot convert from 'sbyte' to 'ulong'
                //         F<ulong>(sbyte.MaxValue); // 17
                Diagnostic(ErrorCode.ERR_BadArgType, "sbyte.MaxValue").WithArguments("1", "sbyte", "ulong").WithLocation(21, 18),
                // (22,18): error CS1503: Argument 1: cannot convert from 'short' to 'ulong'
                //         F<ulong>(short.MaxValue); // 18
                Diagnostic(ErrorCode.ERR_BadArgType, "short.MaxValue").WithArguments("1", "short", "ulong").WithLocation(22, 18)
                );
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();
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

    public static void Main()
    {
        System.Console.Write($""{A}, {B}, {C}, {D}"");
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net70);

            comp.VerifyDiagnostics();
            CompileAndVerify(comp, verify: Verification.FailsPEVerify, expectedOutput: IncludeExpectedOutput("0, 0, 0, 0"));
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
            var comp = CreateCompilation(source0, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
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
            comp = CreateCompilation(source1, references: new[] { ref0 }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net70);
            CompileAndVerify(comp, verify: Verification.FailsPEVerify, expectedOutput: IncludeExpectedOutput(
@"-42
42"));
        }

        [Fact]
        public void Constants_ParameterDefaults()
        {
            var source0 =
@"public class A
{
    public static System.IntPtr F1(System.IntPtr i = -42) => i;
    public static nint F2(nint i = -42) => i;
    public static System.UIntPtr F3(System.UIntPtr u = 42) => u;
    public static nuint F4(nuint u = 42) => u;
}";
            var comp = CreateCompilation(source0, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);

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
            comp = CreateCompilation(source1, references: new[] { ref0 }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net70);

            CompileAndVerify(comp, verify: Verification.FailsPEVerify, expectedOutput: IncludeExpectedOutput(
@"-42
-42
42
42"));
        }

        [Fact]
        public void ConstantValue_Properties()
        {
            var source =
@"using System;
class Program
{
    const IntPtr A = int.MinValue;
    const IntPtr B = 0;
    const IntPtr C = int.MaxValue;
    const UIntPtr D = 0;
    const UIntPtr E = uint.MaxValue;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);

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
@"using System;
class A : System.Attribute
{
    public A() { }
    public A(object value) { }
    public object Value;
}
[A((IntPtr)1)]
[A(new IntPtr[0])]
[A(Value = (IntPtr)3)]
[A(Value = new[] { (IntPtr)4 })]
class B
{
}";
            var comp = CreateCompilation(source0, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);

            comp.VerifyDiagnostics(
                // (8,4): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [A((IntPtr)1)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "(IntPtr)1").WithLocation(8, 4),
                // (9,4): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [A(new IntPtr[0])]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "new IntPtr[0]").WithLocation(9, 4),
                // (10,12): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [A(Value = (IntPtr)3)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "(IntPtr)3").WithLocation(10, 12),
                // (11,12): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [A(Value = new[] { (IntPtr)4 })]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "new[] { (IntPtr)4 }").WithLocation(11, 12));
        }

        [Fact]
        public void AttributeValue_02()
        {
            var source0 =
@"using System;
class A : System.Attribute
{
    public A() { }
    public A(IntPtr value) { }
    public IntPtr[] Value;
}
[A(1)]
[A(Value = default)]
class B
{
}";
            var comp = CreateCompilation(source0, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);

            comp.VerifyDiagnostics(
                // (8,2): error CS0181: Attribute constructor parameter 'value' has type 'nint', which is not a valid attribute parameter type
                // [A(1)]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "A").WithArguments("value", "nint").WithLocation(8, 2),
                // (9,4): error CS0655: 'Value' is not a valid named attribute argument because it is not a valid attribute parameter type
                // [A(Value = default)]
                Diagnostic(ErrorCode.ERR_BadNamedAttributeArgumentType, "Value").WithArguments("Value").WithLocation(9, 4));
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
@"using System;
public class A
{
    public static void F0(IntPtr x = default, UIntPtr y = default)
    {
        Report(x);
        Report(y);
    }
    public static void F1(IntPtr x = -1, UIntPtr y = 2)
    {
        Report(x);
        Report(y);
    }
    public static void F2(IntPtr? x = null, UIntPtr? y = null)
    {
        Report(x);
        Report(y);
    }
    public static void F3(IntPtr? x = -3, UIntPtr? y = 4)
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

            var comp = CreateCompilation(new[] { sourceA, sourceB }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            CompileAndVerify(comp, expectedOutput: IncludeExpectedOutput(expectedOutput), verify: Verification.FailsPEVerify);

            comp = CreateCompilation(sourceA, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            var ref1 = comp.ToMetadataReference();
            var ref2 = comp.EmitToImageReference();

            comp = CreateCompilation(sourceB, references: new[] { ref1 }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            CompileAndVerify(comp, expectedOutput: IncludeExpectedOutput(expectedOutput), verify: Verification.FailsPEVerify);

            comp = CreateCompilation(sourceB, references: new[] { ref2 }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            CompileAndVerify(comp, expectedOutput: IncludeExpectedOutput(expectedOutput), verify: Verification.FailsPEVerify);

            comp = CreateCompilation(sourceB, references: new[] { ref1 }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular8, targetFramework: TargetFramework.Net70);
            CompileAndVerify(comp, expectedOutput: IncludeExpectedOutput(expectedOutput), verify: Verification.FailsPEVerify);

            comp = CreateCompilation(sourceB, references: new[] { ref2 }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular8, targetFramework: TargetFramework.Net70);
            CompileAndVerify(comp, expectedOutput: IncludeExpectedOutput(expectedOutput), verify: Verification.FailsPEVerify);
        }

        [Fact]
        public void SwitchStatement_01()
        {
            var source =
@"using System;
class Program
{
   static IntPtr M(IntPtr ret)
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
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);

            var verifier = CompileAndVerify(comp, verify: Verification.FailsPEVerify, expectedOutput: IncludeExpectedOutput(
@"case 6: 5
case 2: 4
case 0xff: 3
case 0: 2
case 9999: 1
default: 0
0"));
            verifier.VerifyIL("Program.M", @"
    {
      // Code size      201 (0xc9)
      .maxstack  3
      .locals init (long V_0)
      IL_0000:  ldarg.0
      IL_0001:  conv.i8
      IL_0002:  stloc.0
      IL_0003:  ldloc.0
      IL_0004:  ldc.i4.6
      IL_0005:  conv.i8
      IL_0006:  bgt.s      IL_0031
      IL_0008:  ldloc.0
      IL_0009:  ldc.i4.m1
      IL_000a:  conv.i8
      IL_000b:  sub
      IL_000c:  dup
      IL_000d:  ldc.i4.3
      IL_000e:  conv.i8
      IL_000f:  ble.un.s   IL_0014
      IL_0011:  pop
      IL_0012:  br.s       IL_002a
      IL_0014:  conv.u4
      IL_0015:  switch    (
            IL_00b4,
            IL_0045,
            IL_009f,
            IL_0057)
      IL_002a:  ldloc.0
      IL_002b:  ldc.i4.6
      IL_002c:  conv.i8
      IL_002d:  beq.s      IL_0069
      IL_002f:  br.s       IL_009f
      IL_0031:  ldloc.0
      IL_0032:  ldc.i4     0xff
      IL_0037:  conv.i8
      IL_0038:  beq.s      IL_008d
      IL_003a:  ldloc.0
      IL_003b:  ldc.i4     0x270f
      IL_0040:  conv.i8
      IL_0041:  beq.s      IL_007b
      IL_0043:  br.s       IL_009f
      IL_0045:  ldarg.0
      IL_0046:  ldc.i4.1
      IL_0047:  sub
      IL_0048:  starg.s    V_0
      IL_004a:  ldstr      ""case 0: ""
      IL_004f:  ldarg.0
      IL_0050:  call       ""void Program.Report(string, nint)""
      IL_0055:  br.s       IL_007b
      IL_0057:  ldarg.0
      IL_0058:  ldc.i4.1
      IL_0059:  sub
      IL_005a:  starg.s    V_0
      IL_005c:  ldstr      ""case 2: ""
      IL_0061:  ldarg.0
      IL_0062:  call       ""void Program.Report(string, nint)""
      IL_0067:  br.s       IL_008d
      IL_0069:  ldarg.0
      IL_006a:  ldc.i4.1
      IL_006b:  sub
      IL_006c:  starg.s    V_0
      IL_006e:  ldstr      ""case 6: ""
      IL_0073:  ldarg.0
      IL_0074:  call       ""void Program.Report(string, nint)""
      IL_0079:  br.s       IL_0057
      IL_007b:  ldarg.0
      IL_007c:  ldc.i4.1
      IL_007d:  sub
      IL_007e:  starg.s    V_0
      IL_0080:  ldstr      ""case 9999: ""
      IL_0085:  ldarg.0
      IL_0086:  call       ""void Program.Report(string, nint)""
      IL_008b:  br.s       IL_009f
      IL_008d:  ldarg.0
      IL_008e:  ldc.i4.1
      IL_008f:  sub
      IL_0090:  starg.s    V_0
      IL_0092:  ldstr      ""case 0xff: ""
      IL_0097:  ldarg.0
      IL_0098:  call       ""void Program.Report(string, nint)""
      IL_009d:  br.s       IL_0045
      IL_009f:  ldarg.0
      IL_00a0:  ldc.i4.1
      IL_00a1:  sub
      IL_00a2:  starg.s    V_0
      IL_00a4:  ldstr      ""default: ""
      IL_00a9:  ldarg.0
      IL_00aa:  call       ""void Program.Report(string, nint)""
      IL_00af:  ldarg.0
      IL_00b0:  ldc.i4.0
      IL_00b1:  conv.i
      IL_00b2:  ble.s      IL_00c7
      IL_00b4:  ldc.i4     0x3e7
      IL_00b9:  conv.i
      IL_00ba:  starg.s    V_0
      IL_00bc:  ldstr      ""case -1: ""
      IL_00c1:  ldarg.0
      IL_00c2:  call       ""void Program.Report(string, nint)""
      IL_00c7:  ldarg.0
      IL_00c8:  ret
    }
    ");
        }

        [Fact]
        public void SwitchStatement_02()
        {
            var source =
@"using System;
class Program
{
   static UIntPtr M(UIntPtr ret)
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
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);

            var verifier = CompileAndVerify(comp, verify: Verification.FailsPEVerify, expectedOutput: IncludeExpectedOutput(
@"case 6: 5
case 2: 4
case 0xff: 3
case 0: 2
case 9999: 1
default: 0
0"));
            verifier.VerifyIL("Program.M", @"
    {
      // Code size      184 (0xb8)
      .maxstack  2
      .locals init (ulong V_0)
      IL_0000:  ldarg.0
      IL_0001:  conv.u8
      IL_0002:  stloc.0
      IL_0003:  ldloc.0
      IL_0004:  ldc.i4.6
      IL_0005:  conv.i8
      IL_0006:  bgt.un.s   IL_0017
      IL_0008:  ldloc.0
      IL_0009:  brfalse.s  IL_0034
      IL_000b:  ldloc.0
      IL_000c:  ldc.i4.2
      IL_000d:  conv.i8
      IL_000e:  beq.s      IL_0046
      IL_0010:  ldloc.0
      IL_0011:  ldc.i4.6
      IL_0012:  conv.i8
      IL_0013:  beq.s      IL_0058
      IL_0015:  br.s       IL_008e
      IL_0017:  ldloc.0
      IL_0018:  ldc.i4     0xff
      IL_001d:  conv.i8
      IL_001e:  beq.s      IL_007c
      IL_0020:  ldloc.0
      IL_0021:  ldc.i4     0x270f
      IL_0026:  conv.i8
      IL_0027:  beq.s      IL_006a
      IL_0029:  ldloc.0
      IL_002a:  ldc.i4     0x7fffffff
      IL_002f:  conv.i8
      IL_0030:  beq.s      IL_00a3
      IL_0032:  br.s       IL_008e
      IL_0034:  ldarg.0
      IL_0035:  ldc.i4.1
      IL_0036:  sub
      IL_0037:  starg.s    V_0
      IL_0039:  ldstr      ""case 0: ""
      IL_003e:  ldarg.0
      IL_003f:  call       ""void Program.Report(string, nuint)""
      IL_0044:  br.s       IL_006a
      IL_0046:  ldarg.0
      IL_0047:  ldc.i4.1
      IL_0048:  sub
      IL_0049:  starg.s    V_0
      IL_004b:  ldstr      ""case 2: ""
      IL_0050:  ldarg.0
      IL_0051:  call       ""void Program.Report(string, nuint)""
      IL_0056:  br.s       IL_007c
      IL_0058:  ldarg.0
      IL_0059:  ldc.i4.1
      IL_005a:  sub
      IL_005b:  starg.s    V_0
      IL_005d:  ldstr      ""case 6: ""
      IL_0062:  ldarg.0
      IL_0063:  call       ""void Program.Report(string, nuint)""
      IL_0068:  br.s       IL_0046
      IL_006a:  ldarg.0
      IL_006b:  ldc.i4.1
      IL_006c:  sub
      IL_006d:  starg.s    V_0
      IL_006f:  ldstr      ""case 9999: ""
      IL_0074:  ldarg.0
      IL_0075:  call       ""void Program.Report(string, nuint)""
      IL_007a:  br.s       IL_008e
      IL_007c:  ldarg.0
      IL_007d:  ldc.i4.1
      IL_007e:  sub
      IL_007f:  starg.s    V_0
      IL_0081:  ldstr      ""case 0xff: ""
      IL_0086:  ldarg.0
      IL_0087:  call       ""void Program.Report(string, nuint)""
      IL_008c:  br.s       IL_0034
      IL_008e:  ldarg.0
      IL_008f:  ldc.i4.1
      IL_0090:  sub
      IL_0091:  starg.s    V_0
      IL_0093:  ldstr      ""default: ""
      IL_0098:  ldarg.0
      IL_0099:  call       ""void Program.Report(string, nuint)""
      IL_009e:  ldarg.0
      IL_009f:  ldc.i4.0
      IL_00a0:  conv.i
      IL_00a1:  ble.un.s   IL_00b6
      IL_00a3:  ldc.i4     0x3e7
      IL_00a8:  conv.i
      IL_00a9:  starg.s    V_0
      IL_00ab:  ldstr      ""case int.MaxValue: ""
      IL_00b0:  ldarg.0
      IL_00b1:  call       ""void Program.Report(string, nuint)""
      IL_00b6:  ldarg.0
      IL_00b7:  ret
    }
");
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
            static string convRUn(string conversion) =>
$@"{{
      // Code size        4 (0x4)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.r.un
  IL_0002:  {conversion}
  IL_0003:  ret
}}";
            static string convFromNullableT(string conversion, string sourceType) =>
$@"{{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly {sourceType} {sourceType}?.Value.get""
  IL_0007:  {conversion}
  IL_0008:  ret
}}";
            static string convRUnFromNullableT(string conversion, string sourceType) =>
$@"{{
  // Code size       10 (0xa)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly {sourceType} {sourceType}?.Value.get""
  IL_0007:  conv.r.un
  IL_0008:  {conversion}
  IL_0009:  ret
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
            static string convRUnToNullableT(string conversion, string destType) =>
$@"{{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.r.un
  IL_0002:  {conversion}
  IL_0003:  newobj     ""{destType}?..ctor({destType})""
  IL_0008:  ret
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
  IL_0004:  call       ""readonly bool {sourceType}?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""{destType}?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly {sourceType} {sourceType}?.GetValueOrDefault()""
  IL_001c:  {conversion}
  IL_001d:  newobj     ""{destType}?..ctor({destType})""
  IL_0022:  ret
}}";
            static string convRUnFromToNullableT(string conversion, string sourceType, string destType) =>
$@"{{
  // Code size       36 (0x24)
  .maxstack  1
  .locals init ({sourceType}? V_0,
                {destType}? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool {sourceType}?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""{destType}?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly {sourceType} {sourceType}?.GetValueOrDefault()""
  IL_001c:  conv.r.un
  IL_001d:  {conversion}
  IL_001e:  newobj     ""{destType}?..ctor({destType})""
  IL_0023:  ret
}}";
            static string convExplicitFromNullableT(string sourceType, string method) =>
$@"{{
  // Code size       27 (0x1b)
  .maxstack  1
  .locals init ({sourceType}? V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool {sourceType}?.HasValue.get""
  IL_0009:  brtrue.s   IL_000e
  IL_000b:  ldc.i4.0
  IL_000c:  conv.u
  IL_000d:  ret
  IL_000e:  ldloca.s   V_0
  IL_0010:  call       ""readonly {sourceType} {sourceType}?.GetValueOrDefault()""
  IL_0015:  call       ""{method}""
  IL_001a:  ret
}}";

            void conversions(string sourceType, string destType, ConversionKind[] expectedConversions, string expectedImplicitIL, string expectedExplicitIL, string expectedCheckedIL = null)
            {
                if (expectedExplicitIL is not null)
                {
                    Assert.False(IsNoConversion(expectedConversions));
                }

                convert(
                    sourceType,
                    destType,
                    expectedImplicitIL,
                    useExplicitCast: false,
                    useChecked: false,
                    expectedConversions: expectedConversions,
                    expectedErrorCode: expectedImplicitIL is null ?
                        expectedExplicitIL is null ? ErrorCode.ERR_NoImplicitConv : ErrorCode.ERR_NoImplicitConvCast :
                        0);

                // Explicit cast
                convert(
                    sourceType,
                    destType,
                    expectedExplicitIL,
                    useExplicitCast: true,
                    useChecked: false,
                    expectedConversions: null,
                    expectedErrorCode: expectedExplicitIL is null ? ErrorCode.ERR_NoExplicitConv : 0);

                // Explicit cast and checked
                expectedCheckedIL ??= expectedExplicitIL;
                convert(
                    sourceType,
                    destType,
                    expectedCheckedIL,
                    useExplicitCast: true,
                    useChecked: true,
                    expectedConversions: null,
                    expectedErrorCode: expectedCheckedIL is null ? ErrorCode.ERR_NoExplicitConv : 0);
            }

            // Test start:
            // type to nint
            conversions(sourceType: "object", destType: "nint", Unboxing, expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""nint""
  IL_0006:  ret
}");
            conversions(sourceType: "string", destType: "nint", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "void*", destType: "nint", ExplicitPointerToInteger, expectedImplicitIL: null, expectedExplicitIL: convNone, expectedCheckedIL: conv("conv.ovf.i.un"));
            conversions(sourceType: "delegate*<void>", destType: "nint", ExplicitPointerToInteger, expectedImplicitIL: null, expectedExplicitIL: convNone, expectedCheckedIL: conv("conv.ovf.i.un"));
            conversions(sourceType: "E", destType: "nint", ExplicitEnumeration, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"));
            conversions(sourceType: "bool", destType: "nint", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char", destType: "nint", ImplicitNumeric, expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            conversions(sourceType: "sbyte", destType: "nint", ImplicitNumeric, expectedImplicitIL: conv("conv.i"), expectedExplicitIL: conv("conv.i"));
            conversions(sourceType: "byte", destType: "nint", ImplicitNumeric, expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            conversions(sourceType: "short", destType: "nint", ImplicitNumeric, expectedImplicitIL: conv("conv.i"), expectedExplicitIL: conv("conv.i"));
            conversions(sourceType: "ushort", destType: "nint", ImplicitNumeric, expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            conversions(sourceType: "int", destType: "nint", ImplicitNumeric, expectedImplicitIL: conv("conv.i"), expectedExplicitIL: conv("conv.i"));
            conversions(sourceType: "uint", destType: "nint", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.u"), expectedCheckedIL: conv("conv.ovf.i.un"));
            conversions(sourceType: "long", destType: "nint", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.i"));
            conversions(sourceType: "ulong", destType: "nint", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.i.un"));
            conversions(sourceType: "nint", destType: "nint", Identity, expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "nuint", destType: "nint", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convNone, expectedCheckedIL: conv("conv.ovf.i.un"));
            conversions(sourceType: "float", destType: "nint", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.i"));
            conversions(sourceType: "double", destType: "nint", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.i"));
            conversions(sourceType: "decimal", destType: "nint", ExplicitNumeric, expectedImplicitIL: null,
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
            conversions(sourceType: "System.IntPtr", destType: "nint", Identity, expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "System.UIntPtr", destType: "nint", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convNone, expectedCheckedIL: conv("conv.ovf.i.un"));

            // nullable type to nint
            conversions(sourceType: "E?", destType: "nint", ExplicitNullableEnumeration, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "E"));
            conversions(sourceType: "bool?", destType: "nint", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char?", destType: "nint", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "char"));
            conversions(sourceType: "sbyte?", destType: "nint", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "sbyte"));
            conversions(sourceType: "byte?", destType: "nint", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "byte"));
            conversions(sourceType: "short?", destType: "nint", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "short"));
            conversions(sourceType: "ushort?", destType: "nint", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "ushort"));
            conversions(sourceType: "int?", destType: "nint", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "int"));
            conversions(sourceType: "uint?", destType: "nint", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "uint"), expectedCheckedIL: convFromNullableT("conv.ovf.i.un", "uint"));
            conversions(sourceType: "long?", destType: "nint", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "long"), expectedCheckedIL: convFromNullableT("conv.ovf.i", "long"));
            conversions(sourceType: "ulong?", destType: "nint", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "ulong"), expectedCheckedIL: convFromNullableT("conv.ovf.i.un", "ulong"));
            conversions(sourceType: "nint?", destType: "nint", ExplicitNullableIdentity, expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly nint nint?.Value.get""
  IL_0007:  ret
}");
            conversions(sourceType: "nuint?", destType: "nint", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly nuint nuint?.Value.get""
  IL_0007:  ret
}
",
                expectedCheckedIL: convFromNullableT("conv.ovf.i.un", "nuint"));
            conversions(sourceType: "float?", destType: "nint", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "float"), expectedCheckedIL: convFromNullableT("conv.ovf.i", "float"));
            conversions(sourceType: "double?", destType: "nint", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "double"), expectedCheckedIL: convFromNullableT("conv.ovf.i", "double"));
            conversions(sourceType: "decimal?", destType: "nint", ExplicitNullableNumeric, expectedImplicitIL: null,
@"{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly decimal decimal?.Value.get""
  IL_0007:  call       ""long decimal.op_Explicit(decimal)""
  IL_000c:  conv.i
  IL_000d:  ret
}",
@"{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly decimal decimal?.Value.get""
  IL_0007:  call       ""long decimal.op_Explicit(decimal)""
  IL_000c:  conv.ovf.i
  IL_000d:  ret
}");
            conversions(sourceType: "System.IntPtr?", destType: "nint", ExplicitNullableIdentity, expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly nint nint?.Value.get""
  IL_0007:  ret
}");
            conversions(sourceType: "System.UIntPtr?", destType: "nint", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly nuint nuint?.Value.get""
  IL_0007:  ret
}
",
                expectedCheckedIL: convFromNullableT("conv.ovf.i.un", "nuint"));

            // type to nullable nint
            conversions(sourceType: "object", destType: "nint?", Unboxing, expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""nint?""
  IL_0006:  ret
}");
            conversions(sourceType: "string", destType: "nint?", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "void*", destType: "nint?", ExplicitNullablePointerToInteger, expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nint?..ctor(nint)""
  IL_0006:  ret
}",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i.un
  IL_0002:  newobj     ""nint?..ctor(nint)""
  IL_0007:  ret
}");
            conversions(sourceType: "delegate*<void>", destType: "nint?", ExplicitNullablePointerToInteger, expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nint?..ctor(nint)""
  IL_0006:  ret
}",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i.un
  IL_0002:  newobj     ""nint?..ctor(nint)""
  IL_0007:  ret
}");
            conversions(sourceType: "E", destType: "nint?", ExplicitNullableEnumeration, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nint"));
            conversions(sourceType: "bool", destType: "nint?", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char", destType: "nint?", ImplicitNullableNumeric, expectedImplicitIL: convToNullableT("conv.u", "nint"), expectedExplicitIL: convToNullableT("conv.u", "nint"));
            conversions(sourceType: "sbyte", destType: "nint?", ImplicitNullableNumeric, expectedImplicitIL: convToNullableT("conv.i", "nint"), expectedExplicitIL: convToNullableT("conv.i", "nint"));
            conversions(sourceType: "byte", destType: "nint?", ImplicitNullableNumeric, expectedImplicitIL: convToNullableT("conv.u", "nint"), expectedExplicitIL: convToNullableT("conv.u", "nint"));
            conversions(sourceType: "short", destType: "nint?", ImplicitNullableNumeric, expectedImplicitIL: convToNullableT("conv.i", "nint"), expectedExplicitIL: convToNullableT("conv.i", "nint"));
            conversions(sourceType: "ushort", destType: "nint?", ImplicitNullableNumeric, expectedImplicitIL: convToNullableT("conv.u", "nint"), expectedExplicitIL: convToNullableT("conv.u", "nint"));
            conversions(sourceType: "int", destType: "nint?", ImplicitNullableNumeric, expectedImplicitIL: convToNullableT("conv.i", "nint"), expectedExplicitIL: convToNullableT("conv.i", "nint"));
            conversions(sourceType: "uint", destType: "nint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u", "nint"), expectedCheckedIL: convToNullableT("conv.ovf.i.un", "nint"));
            conversions(sourceType: "long", destType: "nint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nint"), expectedCheckedIL: convToNullableT("conv.ovf.i", "nint"));
            conversions(sourceType: "ulong", destType: "nint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nint"), expectedCheckedIL: convToNullableT("conv.ovf.i.un", "nint"));
            conversions(sourceType: "nint", destType: "nint?", ImplicitNullableIdentity,
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
            conversions(sourceType: "nuint", destType: "nint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nint?..ctor(nint)""
  IL_0006:  ret
}
",
                expectedCheckedIL: convToNullableT("conv.ovf.i.un", "nint"));
            conversions(sourceType: "float", destType: "nint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nint"), expectedCheckedIL: convToNullableT("conv.ovf.i", "nint"));
            conversions(sourceType: "double", destType: "nint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nint"), expectedCheckedIL: convToNullableT("conv.ovf.i", "nint"));
            conversions(sourceType: "decimal", destType: "nint?", ExplicitNullableNumeric, expectedImplicitIL: null,
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
            conversions(sourceType: "System.IntPtr", destType: "nint?", ImplicitNullableIdentity,
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
            conversions(sourceType: "System.UIntPtr", destType: "nint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nint?..ctor(nint)""
  IL_0006:  ret
}
",
                expectedCheckedIL: convToNullableT("conv.ovf.i.un", "nint"));

            // nullable type to nullable nint
            conversions(sourceType: "E?", destType: "nint?", ExplicitNullableEnumeration, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "E", "nint"));
            conversions(sourceType: "bool?", destType: "nint?", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char?", destType: "nint?", ImplicitNullableNumeric, expectedImplicitIL: convFromToNullableT("conv.u", "char", "nint"), expectedExplicitIL: convFromToNullableT("conv.u", "char", "nint"));
            conversions(sourceType: "sbyte?", destType: "nint?", ImplicitNullableNumeric, expectedImplicitIL: convFromToNullableT("conv.i", "sbyte", "nint"), expectedExplicitIL: convFromToNullableT("conv.i", "sbyte", "nint"));
            conversions(sourceType: "byte?", destType: "nint?", ImplicitNullableNumeric, expectedImplicitIL: convFromToNullableT("conv.u", "byte", "nint"), expectedExplicitIL: convFromToNullableT("conv.u", "byte", "nint"));
            conversions(sourceType: "short?", destType: "nint?", ImplicitNullableNumeric, expectedImplicitIL: convFromToNullableT("conv.i", "short", "nint"), expectedExplicitIL: convFromToNullableT("conv.i", "short", "nint"));
            conversions(sourceType: "ushort?", destType: "nint?", ImplicitNullableNumeric, expectedImplicitIL: convFromToNullableT("conv.u", "ushort", "nint"), expectedExplicitIL: convFromToNullableT("conv.u", "ushort", "nint"));
            conversions(sourceType: "int?", destType: "nint?", ImplicitNullableNumeric, expectedImplicitIL: convFromToNullableT("conv.i", "int", "nint"), expectedExplicitIL: convFromToNullableT("conv.i", "int", "nint"));
            conversions(sourceType: "uint?", destType: "nint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u", "uint", "nint"), expectedCheckedIL: convFromToNullableT("conv.ovf.i.un", "uint", "nint"));
            conversions(sourceType: "long?", destType: "nint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "long", "nint"), expectedCheckedIL: convFromToNullableT("conv.ovf.i", "long", "nint"));
            conversions(sourceType: "ulong?", destType: "nint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "ulong", "nint"), expectedCheckedIL: convFromToNullableT("conv.ovf.i.un", "ulong", "nint"));
            conversions(sourceType: "nint?", destType: "nint?", Identity, expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "nuint?", destType: "nint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size       34 (0x22)
  .maxstack  1
  .locals init (nuint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly nuint nuint?.GetValueOrDefault()""
  IL_001c:  newobj     ""nint?..ctor(nint)""
  IL_0021:  ret
}
",
                expectedCheckedIL: convFromToNullableT("conv.ovf.i.un", "nuint", "nint"));
            conversions(sourceType: "float?", destType: "nint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "float", "nint"), expectedCheckedIL: convFromToNullableT("conv.ovf.i", "float", "nint"));
            conversions(sourceType: "double?", destType: "nint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "double", "nint"), expectedCheckedIL: convFromToNullableT("conv.ovf.i", "double", "nint"));
            conversions(sourceType: "decimal?", destType: "nint?", ExplicitNullableNumeric, null,
@"{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (decimal? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool decimal?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly decimal decimal?.GetValueOrDefault()""
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
  IL_0004:  call       ""readonly bool decimal?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly decimal decimal?.GetValueOrDefault()""
  IL_001c:  call       ""long decimal.op_Explicit(decimal)""
  IL_0021:  conv.ovf.i
  IL_0022:  newobj     ""nint?..ctor(nint)""
  IL_0027:  ret
}");
            conversions(sourceType: "System.IntPtr?", destType: "nint?", Identity, expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "System.UIntPtr?", destType: "nint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size       34 (0x22)
  .maxstack  1
  .locals init (nuint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly nuint nuint?.GetValueOrDefault()""
  IL_001c:  newobj     ""nint?..ctor(nint)""
  IL_0021:  ret
}
",
                expectedCheckedIL: convFromToNullableT("conv.ovf.i.un", "nuint", "nint"));

            // nint to type
            conversions(sourceType: "nint", destType: "object", Boxing,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""nint""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""nint""
  IL_0006:  ret
}");
            conversions(sourceType: "nint", destType: "string", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nint", destType: "void*", ExplicitIntegerToPointer, expectedImplicitIL: null, expectedExplicitIL: convNone, expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "nint", destType: "delegate*<void>", ExplicitIntegerToPointer, expectedImplicitIL: null, expectedExplicitIL: convNone, expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "nint", destType: "E", ExplicitEnumeration, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i4"), expectedCheckedIL: conv("conv.ovf.i4"));
            conversions(sourceType: "nint", destType: "bool", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nint", destType: "char", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.u2"), expectedCheckedIL: conv("conv.ovf.u2"));
            conversions(sourceType: "nint", destType: "sbyte", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i1"), expectedCheckedIL: conv("conv.ovf.i1"));
            conversions(sourceType: "nint", destType: "byte", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.u1"), expectedCheckedIL: conv("conv.ovf.u1"));
            conversions(sourceType: "nint", destType: "short", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i2"), expectedCheckedIL: conv("conv.ovf.i2"));
            conversions(sourceType: "nint", destType: "ushort", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.u2"), expectedCheckedIL: conv("conv.ovf.u2"));
            conversions(sourceType: "nint", destType: "int", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i4"), expectedCheckedIL: conv("conv.ovf.i4"));
            conversions(sourceType: "nint", destType: "uint", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.u4"), expectedCheckedIL: conv("conv.ovf.u4"));
            conversions(sourceType: "nint", destType: "long", ImplicitNumeric, expectedImplicitIL: conv("conv.i8"), expectedExplicitIL: conv("conv.i8"));
            conversions(sourceType: "nint", destType: "ulong", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i8"), expectedCheckedIL: conv("conv.ovf.u8"));
            conversions(sourceType: "nint", destType: "float", ImplicitNumeric, expectedImplicitIL: conv("conv.r4"), expectedExplicitIL: conv("conv.r4"));
            conversions(sourceType: "nint", destType: "double", ImplicitNumeric, expectedImplicitIL: conv("conv.r8"), expectedExplicitIL: conv("conv.r8"));
            conversions(sourceType: "nint", destType: "decimal", ImplicitNumeric,
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
            conversions(sourceType: "nint", destType: "System.IntPtr", Identity, expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "nint", destType: "System.UIntPtr", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convNone, expectedCheckedIL: conv("conv.ovf.u"));

            // nint to nullable type
            conversions(sourceType: "nint", destType: "E?", ExplicitNullableEnumeration, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i4", "E"), expectedCheckedIL: convToNullableT("conv.ovf.i4", "E"));
            conversions(sourceType: "nint", destType: "bool?", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nint", destType: "char?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u2", "char"), expectedCheckedIL: convToNullableT("conv.ovf.u2", "char"));
            conversions(sourceType: "nint", destType: "sbyte?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i1", "sbyte"), expectedCheckedIL: convToNullableT("conv.ovf.i1", "sbyte"));
            conversions(sourceType: "nint", destType: "byte?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u1", "byte"), expectedCheckedIL: convToNullableT("conv.ovf.u1", "byte"));
            conversions(sourceType: "nint", destType: "short?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i2", "short"), expectedCheckedIL: convToNullableT("conv.ovf.i2", "short"));
            conversions(sourceType: "nint", destType: "ushort?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u2", "ushort"), expectedCheckedIL: convToNullableT("conv.ovf.u2", "ushort"));
            conversions(sourceType: "nint", destType: "int?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i4", "int"), expectedCheckedIL: convToNullableT("conv.ovf.i4", "int"));
            conversions(sourceType: "nint", destType: "uint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u4", "uint"), expectedCheckedIL: convToNullableT("conv.ovf.u4", "uint"));
            conversions(sourceType: "nint", destType: "long?", ImplicitNullableNumeric, expectedImplicitIL: convToNullableT("conv.i8", "long"), expectedExplicitIL: convToNullableT("conv.i8", "long"));
            conversions(sourceType: "nint", destType: "ulong?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i8", "ulong"), expectedCheckedIL: convToNullableT("conv.ovf.u8", "ulong"));
            conversions(sourceType: "nint", destType: "float?", ImplicitNullableNumeric, expectedImplicitIL: convToNullableT("conv.r4", "float"), expectedExplicitIL: convToNullableT("conv.r4", "float"), null);
            conversions(sourceType: "nint", destType: "double?", ImplicitNullableNumeric, expectedImplicitIL: convToNullableT("conv.r8", "double"), expectedExplicitIL: convToNullableT("conv.r8", "double"), null);
            conversions(sourceType: "nint", destType: "decimal?", ImplicitNullableNumeric,
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
            conversions(sourceType: "nint", destType: "System.IntPtr?", ImplicitNullableIdentity,
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
            conversions(sourceType: "nint", destType: "System.UIntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nuint?..ctor(nuint)""
  IL_0006:  ret
}
",
                expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));

            // nullable nint to type
            conversions(sourceType: "nint?", destType: "object", Boxing,
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
            conversions(sourceType: "nint?", destType: "string", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nint?", destType: "void*", ExplicitUserDefined, expectedImplicitIL: null, expectedExplicitIL: convExplicitFromNullableT("nint", "void* nint.op_Explicit(nint)"));
            conversions(sourceType: "nint?", destType: "delegate*<void>", ExplicitUserDefined, expectedImplicitIL: null, expectedExplicitIL: convExplicitFromNullableT("nint", "void* nint.op_Explicit(nint)"));
            conversions(sourceType: "nint?", destType: "E", ExplicitNullableEnumeration, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i4", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.i4", "nint"));
            conversions(sourceType: "nint?", destType: "bool", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nint?", destType: "char", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u2", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.u2", "nint"));
            conversions(sourceType: "nint?", destType: "sbyte", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i1", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.i1", "nint"));
            conversions(sourceType: "nint?", destType: "byte", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u1", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.u1", "nint"));
            conversions(sourceType: "nint?", destType: "short", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i2", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.i2", "nint"));
            conversions(sourceType: "nint?", destType: "ushort", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u2", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.u2", "nint"));
            conversions(sourceType: "nint?", destType: "int", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i4", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.i4", "nint"));
            conversions(sourceType: "nint?", destType: "uint", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u4", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.u4", "nint"));
            conversions(sourceType: "nint?", destType: "long", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i8", "nint"));
            conversions(sourceType: "nint?", destType: "ulong", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i8", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.u8", "nint"));
            conversions(sourceType: "nint?", destType: "float", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.r4", "nint"));
            conversions(sourceType: "nint?", destType: "double", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.r8", "nint"));
            conversions(sourceType: "nint?", destType: "decimal", ExplicitNullableImplicitNumeric, expectedImplicitIL: null,
@"{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly nint nint?.Value.get""
  IL_0007:  conv.i8
  IL_0008:  call       ""decimal decimal.op_Implicit(long)""
  IL_000d:  ret
}");
            conversions(sourceType: "nint?", destType: "System.IntPtr", ExplicitNullableIdentity, expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly nint nint?.Value.get""
  IL_0007:  ret
}");
            conversions(sourceType: "nint?", destType: "System.UIntPtr", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly nint nint?.Value.get""
  IL_0007:  ret
}
",
                expectedCheckedIL: convFromNullableT("conv.ovf.u", "nint"));

            // nullable nint to nullable type
            conversions(sourceType: "nint?", destType: "E?", ExplicitNullableEnumeration, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i4", "nint", "E"), expectedCheckedIL: convFromToNullableT("conv.ovf.i4", "nint", "E"));
            conversions(sourceType: "nint?", destType: "bool?", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nint?", destType: "char?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u2", "nint", "char"), expectedCheckedIL: convFromToNullableT("conv.ovf.u2", "nint", "char"));
            conversions(sourceType: "nint?", destType: "sbyte?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i1", "nint", "sbyte"), expectedCheckedIL: convFromToNullableT("conv.ovf.i1", "nint", "sbyte"));
            conversions(sourceType: "nint?", destType: "byte?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u1", "nint", "byte"), expectedCheckedIL: convFromToNullableT("conv.ovf.u1", "nint", "byte"));
            conversions(sourceType: "nint?", destType: "short?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i2", "nint", "short"), expectedCheckedIL: convFromToNullableT("conv.ovf.i2", "nint", "short"));
            conversions(sourceType: "nint?", destType: "ushort?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u2", "nint", "ushort"), expectedCheckedIL: convFromToNullableT("conv.ovf.u2", "nint", "ushort"));
            conversions(sourceType: "nint?", destType: "int?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i4", "nint", "int"), expectedCheckedIL: convFromToNullableT("conv.ovf.i4", "nint", "int"));
            conversions(sourceType: "nint?", destType: "uint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u4", "nint", "uint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u4", "nint", "uint"));
            conversions(sourceType: "nint?", destType: "long?", ImplicitNullableNumeric, expectedImplicitIL: convFromToNullableT("conv.i8", "nint", "long"), expectedExplicitIL: convFromToNullableT("conv.i8", "nint", "long"));
            conversions(sourceType: "nint?", destType: "ulong?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i8", "nint", "ulong"), expectedCheckedIL: convFromToNullableT("conv.ovf.u8", "nint", "ulong"));
            conversions(sourceType: "nint?", destType: "float?", ImplicitNullableNumeric, expectedImplicitIL: convFromToNullableT("conv.r4", "nint", "float"), expectedExplicitIL: convFromToNullableT("conv.r4", "nint", "float"), null);
            conversions(sourceType: "nint?", destType: "double?", ImplicitNullableNumeric, expectedImplicitIL: convFromToNullableT("conv.r8", "nint", "double"), expectedExplicitIL: convFromToNullableT("conv.r8", "nint", "double"), null);
            conversions(sourceType: "nint?", destType: "decimal?", ImplicitNullableNumeric,
@"{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (nint? V_0,
                decimal? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""decimal?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly nint nint?.GetValueOrDefault()""
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
  IL_0004:  call       ""readonly bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""decimal?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly nint nint?.GetValueOrDefault()""
  IL_001c:  conv.i8
  IL_001d:  call       ""decimal decimal.op_Implicit(long)""
  IL_0022:  newobj     ""decimal?..ctor(decimal)""
  IL_0027:  ret
}");
            conversions(sourceType: "nint?", destType: "System.IntPtr?", Identity, expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "nint?", destType: "System.UIntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size       34 (0x22)
  .maxstack  1
  .locals init (nint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly nint nint?.GetValueOrDefault()""
  IL_001c:  newobj     ""nuint?..ctor(nuint)""
  IL_0021:  ret
}
",
                expectedCheckedIL: convFromToNullableT("conv.ovf.u", "nint", "nuint"));

            // type to nuint
            conversions(sourceType: "object", destType: "nuint", Unboxing, expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""nuint""
  IL_0006:  ret
}");
            conversions(sourceType: "string", destType: "nuint", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "void*", destType: "nuint", ExplicitPointerToInteger, expectedImplicitIL: null, expectedExplicitIL: convNone);
            conversions(sourceType: "delegate*<void>", destType: "nuint", ExplicitPointerToInteger, expectedImplicitIL: null, expectedExplicitIL: convNone);
            conversions(sourceType: "E", destType: "nuint", ExplicitEnumeration, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "bool", destType: "nuint", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char", destType: "nuint", ImplicitNumeric, expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            conversions(sourceType: "sbyte", destType: "nuint", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "byte", destType: "nuint", ImplicitNumeric, expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            conversions(sourceType: "short", destType: "nuint", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "ushort", destType: "nuint", ImplicitNumeric, expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            conversions(sourceType: "int", destType: "nuint", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "uint", destType: "nuint", ImplicitNumeric, expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            conversions(sourceType: "long", destType: "nuint", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.u"), expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "ulong", destType: "nuint", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.u"), expectedCheckedIL: conv("conv.ovf.u.un"));
            conversions(sourceType: "nint", destType: "nuint", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convNone, expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "nuint", destType: "nuint", Identity, expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "float", destType: "nuint", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.u"), expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "double", destType: "nuint", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.u"), expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "decimal", destType: "nuint", ExplicitNumeric, expectedImplicitIL: null,
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
            conversions(sourceType: "System.IntPtr", destType: "nuint", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convNone, expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "System.UIntPtr", destType: "nuint", Identity, expectedImplicitIL: convNone, expectedExplicitIL: convNone);

            // nullable type to nuint
            conversions(sourceType: "E?", destType: "nuint", ExplicitNullableEnumeration, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "E"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "E"));
            conversions(sourceType: "bool?", destType: "nuint", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char?", destType: "nuint", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "char"));
            conversions(sourceType: "sbyte?", destType: "nuint", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "sbyte"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "sbyte"));
            conversions(sourceType: "byte?", destType: "nuint", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "byte"));
            conversions(sourceType: "short?", destType: "nuint", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "short"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "short"));
            conversions(sourceType: "ushort?", destType: "nuint", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "ushort"));
            conversions(sourceType: "int?", destType: "nuint", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "int"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "int"));
            conversions(sourceType: "uint?", destType: "nuint", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "uint"));
            conversions(sourceType: "long?", destType: "nuint", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "long"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "long"));
            conversions(sourceType: "ulong?", destType: "nuint", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "ulong"), expectedCheckedIL: convFromNullableT("conv.ovf.u.un", "ulong"));
            conversions(sourceType: "nint?", destType: "nuint", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly nint nint?.Value.get""
  IL_0007:  ret
}
",
                expectedCheckedIL: convFromNullableT("conv.ovf.u", "nint"));
            conversions(sourceType: "nuint?", destType: "nuint", ExplicitNullableIdentity, expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly nuint nuint?.Value.get""
  IL_0007:  ret
}");
            conversions(sourceType: "float?", destType: "nuint", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "float"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "float"));
            conversions(sourceType: "double?", destType: "nuint", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "double"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "double"));
            conversions(sourceType: "decimal?", destType: "nuint", ExplicitNullableNumeric, expectedImplicitIL: null,
@"{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly decimal decimal?.Value.get""
  IL_0007:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_000c:  conv.u
  IL_000d:  ret
}",
@"{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly decimal decimal?.Value.get""
  IL_0007:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_000c:  conv.ovf.u.un
  IL_000d:  ret
}");
            conversions(sourceType: "System.IntPtr?", destType: "nuint", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly nint nint?.Value.get""
  IL_0007:  ret
}",
                expectedCheckedIL: convFromNullableT("conv.ovf.u", "nint"));
            conversions(sourceType: "System.UIntPtr?", destType: "nuint", ExplicitNullableIdentity, expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly nuint nuint?.Value.get""
  IL_0007:  ret
}");

            // type to nullable nuint
            conversions(sourceType: "object", destType: "nuint?", Unboxing, expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""nuint?""
  IL_0006:  ret
}");
            conversions(sourceType: "string", destType: "nuint?", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "void*", destType: "nuint?", ExplicitNullablePointerToInteger, expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nuint?..ctor(nuint)""
  IL_0006:  ret
}");
            conversions(sourceType: "delegate*<void>", destType: "nuint?", ExplicitNullablePointerToInteger, expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nuint?..ctor(nuint)""
  IL_0006:  ret
}");
            conversions(sourceType: "E", destType: "nuint?", ExplicitNullableEnumeration, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "bool", destType: "nuint?", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char", destType: "nuint?", ImplicitNullableNumeric, expectedImplicitIL: convToNullableT("conv.u", "nuint"), expectedExplicitIL: convToNullableT("conv.u", "nuint"));
            conversions(sourceType: "sbyte", destType: "nuint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "byte", destType: "nuint?", ImplicitNullableNumeric, expectedImplicitIL: convToNullableT("conv.u", "nuint"), expectedExplicitIL: convToNullableT("conv.u", "nuint"));
            conversions(sourceType: "short", destType: "nuint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "ushort", destType: "nuint?", ImplicitNullableNumeric, expectedImplicitIL: convToNullableT("conv.u", "nuint"), expectedExplicitIL: convToNullableT("conv.u", "nuint"));
            conversions(sourceType: "int", destType: "nuint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "uint", destType: "nuint?", ImplicitNullableNumeric, expectedImplicitIL: convToNullableT("conv.u", "nuint"), expectedExplicitIL: convToNullableT("conv.u", "nuint"));
            conversions(sourceType: "long", destType: "nuint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "ulong", destType: "nuint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u.un", "nuint"));
            conversions(sourceType: "nint", destType: "nuint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nuint?..ctor(nuint)""
  IL_0006:  ret
}
",
                expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "nuint", destType: "nuint?", ImplicitNullableIdentity,
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
            conversions(sourceType: "float", destType: "nuint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "double", destType: "nuint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "decimal", destType: "nuint?", ExplicitNullableNumeric, expectedImplicitIL: null,
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
            conversions(sourceType: "System.IntPtr", destType: "nuint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nuint?..ctor(nuint)""
  IL_0006:  ret
}
",
                expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "System.UIntPtr", destType: "nuint?", ImplicitNullableIdentity,
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

            // nullable type to nullable nuint
            conversions(sourceType: "E?", destType: "nuint?", ExplicitNullableEnumeration, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "E", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "E", "nuint"));
            conversions(sourceType: "bool?", destType: "nuint?", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char?", destType: "nuint?", ImplicitNullableNumeric, expectedImplicitIL: convFromToNullableT("conv.u", "char", "nuint"), expectedExplicitIL: convFromToNullableT("conv.u", "char", "nuint"));
            conversions(sourceType: "sbyte?", destType: "nuint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "sbyte", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "sbyte", "nuint"));
            conversions(sourceType: "byte?", destType: "nuint?", ImplicitNullableNumeric, expectedImplicitIL: convFromToNullableT("conv.u", "byte", "nuint"), expectedExplicitIL: convFromToNullableT("conv.u", "byte", "nuint"));
            conversions(sourceType: "short?", destType: "nuint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "short", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "short", "nuint"));
            conversions(sourceType: "ushort?", destType: "nuint?", ImplicitNullableNumeric, expectedImplicitIL: convFromToNullableT("conv.u", "ushort", "nuint"), expectedExplicitIL: convFromToNullableT("conv.u", "ushort", "nuint"));
            conversions(sourceType: "int?", destType: "nuint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "int", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "int", "nuint"));
            conversions(sourceType: "uint?", destType: "nuint?", ImplicitNullableNumeric, expectedImplicitIL: convFromToNullableT("conv.u", "uint", "nuint"), expectedExplicitIL: convFromToNullableT("conv.u", "uint", "nuint"));
            conversions(sourceType: "long?", destType: "nuint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u", "long", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "long", "nuint"));
            conversions(sourceType: "ulong?", destType: "nuint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u", "ulong", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u.un", "ulong", "nuint"));
            conversions(sourceType: "nint?", destType: "nuint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size       34 (0x22)
  .maxstack  1
  .locals init (nint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly nint nint?.GetValueOrDefault()""
  IL_001c:  newobj     ""nuint?..ctor(nuint)""
  IL_0021:  ret
}
",
                expectedCheckedIL: convFromToNullableT("conv.ovf.u", "nint", "nuint"));
            conversions(sourceType: "nuint?", destType: "nuint?", Identity, expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "float?", destType: "nuint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u", "float", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "float", "nuint"));
            conversions(sourceType: "double?", destType: "nuint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u", "double", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "double", "nuint"));
            conversions(sourceType: "decimal?", destType: "nuint?", ExplicitNullableNumeric, null,
@"{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (decimal? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool decimal?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly decimal decimal?.GetValueOrDefault()""
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
  IL_0004:  call       ""readonly bool decimal?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly decimal decimal?.GetValueOrDefault()""
  IL_001c:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_0021:  conv.ovf.u.un
  IL_0022:  newobj     ""nuint?..ctor(nuint)""
  IL_0027:  ret
}");
            conversions(sourceType: "System.IntPtr?", destType: "nuint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size       34 (0x22)
  .maxstack  1
  .locals init (nint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly nint nint?.GetValueOrDefault()""
  IL_001c:  newobj     ""nuint?..ctor(nuint)""
  IL_0021:  ret
}
",
                expectedCheckedIL: convFromToNullableT("conv.ovf.u", "nint", "nuint"));
            conversions(sourceType: "System.UIntPtr?", destType: "nuint?", Identity, expectedImplicitIL: convNone, expectedExplicitIL: convNone);

            // nuint to type
            conversions(sourceType: "nuint", destType: "object", Boxing,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""nuint""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""nuint""
  IL_0006:  ret
}");
            conversions(sourceType: "nuint", destType: "string", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nuint", destType: "void*", ExplicitIntegerToPointer, expectedImplicitIL: null, expectedExplicitIL: convNone);
            conversions(sourceType: "nuint", destType: "delegate*<void>", ExplicitIntegerToPointer, expectedImplicitIL: null, expectedExplicitIL: convNone);
            conversions(sourceType: "nuint", destType: "E", ExplicitEnumeration, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i4"), expectedCheckedIL: conv("conv.ovf.i4.un"));
            conversions(sourceType: "nuint", destType: "bool", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nuint", destType: "char", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.u2"), expectedCheckedIL: conv("conv.ovf.u2.un"));
            conversions(sourceType: "nuint", destType: "sbyte", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i1"), expectedCheckedIL: conv("conv.ovf.i1.un"));
            conversions(sourceType: "nuint", destType: "byte", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.u1"), expectedCheckedIL: conv("conv.ovf.u1.un"));
            conversions(sourceType: "nuint", destType: "short", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i2"), expectedCheckedIL: conv("conv.ovf.i2.un"));
            conversions(sourceType: "nuint", destType: "ushort", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.u2"), expectedCheckedIL: conv("conv.ovf.u2.un"));
            conversions(sourceType: "nuint", destType: "int", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i4"), expectedCheckedIL: conv("conv.ovf.i4.un"));
            conversions(sourceType: "nuint", destType: "uint", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.u4"), expectedCheckedIL: conv("conv.ovf.u4.un"));
            conversions(sourceType: "nuint", destType: "long", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.u8"), expectedCheckedIL: conv("conv.ovf.i8.un"));
            conversions(sourceType: "nuint", destType: "ulong", ImplicitNumeric, expectedImplicitIL: conv("conv.u8"), expectedExplicitIL: conv("conv.u8"));
            conversions(sourceType: "nuint", destType: "float", ImplicitNumeric, expectedImplicitIL: convRUn("conv.r4"), expectedExplicitIL: convRUn("conv.r4"));
            conversions(sourceType: "nuint", destType: "double", ImplicitNumeric, expectedImplicitIL: convRUn("conv.r8"), expectedExplicitIL: convRUn("conv.r8"));
            conversions(sourceType: "nuint", destType: "decimal", ImplicitNumeric,
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
            conversions(sourceType: "nuint", destType: "System.IntPtr", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convNone, expectedCheckedIL: conv("conv.ovf.i.un"));
            conversions(sourceType: "nuint", destType: "System.UIntPtr", Identity, expectedImplicitIL: convNone, expectedExplicitIL: convNone);

            // nuint to nullable type
            conversions(sourceType: "nuint", destType: "E?", ExplicitNullableEnumeration, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i4", "E"), expectedCheckedIL: convToNullableT("conv.ovf.i4.un", "E"));
            conversions(sourceType: "nuint", destType: "bool?", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nuint", destType: "char?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u2", "char"), expectedCheckedIL: convToNullableT("conv.ovf.u2.un", "char"));
            conversions(sourceType: "nuint", destType: "sbyte?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i1", "sbyte"), expectedCheckedIL: convToNullableT("conv.ovf.i1.un", "sbyte"));
            conversions(sourceType: "nuint", destType: "byte?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u1", "byte"), expectedCheckedIL: convToNullableT("conv.ovf.u1.un", "byte"));
            conversions(sourceType: "nuint", destType: "short?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i2", "short"), expectedCheckedIL: convToNullableT("conv.ovf.i2.un", "short"));
            conversions(sourceType: "nuint", destType: "ushort?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u2", "ushort"), expectedCheckedIL: convToNullableT("conv.ovf.u2.un", "ushort"));
            conversions(sourceType: "nuint", destType: "int?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i4", "int"), expectedCheckedIL: convToNullableT("conv.ovf.i4.un", "int"));
            conversions(sourceType: "nuint", destType: "uint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u4", "uint"), expectedCheckedIL: convToNullableT("conv.ovf.u4.un", "uint"));
            conversions(sourceType: "nuint", destType: "long?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u8", "long"), expectedCheckedIL: convToNullableT("conv.ovf.i8.un", "long"));
            conversions(sourceType: "nuint", destType: "ulong?", ImplicitNullableNumeric, expectedImplicitIL: convToNullableT("conv.u8", "ulong"), expectedExplicitIL: convToNullableT("conv.u8", "ulong"));
            conversions(sourceType: "nuint", destType: "float?", ImplicitNullableNumeric, expectedImplicitIL: convRUnToNullableT("conv.r4", "float"), expectedExplicitIL: convRUnToNullableT("conv.r4", "float"), null);
            conversions(sourceType: "nuint", destType: "double?", ImplicitNullableNumeric, expectedImplicitIL: convRUnToNullableT("conv.r8", "double"), expectedExplicitIL: convRUnToNullableT("conv.r8", "double"), null);
            conversions(sourceType: "nuint", destType: "decimal?", ImplicitNullableNumeric,
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
            conversions(sourceType: "nuint", destType: "System.IntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nint?..ctor(nint)""
  IL_0006:  ret
}
",
                expectedCheckedIL: convToNullableT("conv.ovf.i.un", "nint"));
            conversions(sourceType: "nuint", destType: "System.UIntPtr?", ImplicitNullableIdentity,
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

            // nullable nuint to type
            conversions(sourceType: "nuint?", destType: "object", Boxing,
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
            conversions(sourceType: "nuint?", destType: "string", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nuint?", destType: "void*", ExplicitUserDefined, expectedImplicitIL: null, expectedExplicitIL: convExplicitFromNullableT("nuint", "void* nuint.op_Explicit(nuint)"));
            conversions(sourceType: "nuint?", destType: "delegate*<void>", ExplicitUserDefined, expectedImplicitIL: null, expectedExplicitIL: convExplicitFromNullableT("nuint", "void* nuint.op_Explicit(nuint)"));
            conversions(sourceType: "nuint?", destType: "E", ExplicitNullableEnumeration, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i4", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.i4.un", "nuint"));
            conversions(sourceType: "nuint?", destType: "bool", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nuint?", destType: "char", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u2", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.u2.un", "nuint"));
            conversions(sourceType: "nuint?", destType: "sbyte", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i1", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.i1.un", "nuint"));
            conversions(sourceType: "nuint?", destType: "byte", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u1", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.u1.un", "nuint"));
            conversions(sourceType: "nuint?", destType: "short", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i2", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.i2.un", "nuint"));
            conversions(sourceType: "nuint?", destType: "ushort", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u2", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.u2.un", "nuint"));
            conversions(sourceType: "nuint?", destType: "int", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i4", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.i4.un", "nuint"));
            conversions(sourceType: "nuint?", destType: "uint", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u4", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.u4.un", "nuint"));
            conversions(sourceType: "nuint?", destType: "long", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u8", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.i8.un", "nuint"));
            conversions(sourceType: "nuint?", destType: "ulong", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u8", "nuint"));
            conversions(sourceType: "nuint?", destType: "float", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convRUnFromNullableT("conv.r4", "nuint"));
            conversions(sourceType: "nuint?", destType: "double", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convRUnFromNullableT("conv.r8", "nuint"));
            conversions(sourceType: "nuint?", destType: "decimal", ExplicitNullableImplicitNumeric, expectedImplicitIL: null,
@"{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly nuint nuint?.Value.get""
  IL_0007:  conv.u8
  IL_0008:  call       ""decimal decimal.op_Implicit(ulong)""
  IL_000d:  ret
}");
            conversions(sourceType: "nuint?", destType: "System.IntPtr", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly nuint nuint?.Value.get""
  IL_0007:  ret
}
",
                expectedCheckedIL: convFromNullableT("conv.ovf.i.un", "nuint"));
            conversions(sourceType: "nuint?", destType: "System.UIntPtr", ExplicitNullableIdentity, expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly nuint nuint?.Value.get""
  IL_0007:  ret
}");

            // nullable nuint to nullable type
            conversions(sourceType: "nuint?", destType: "E?", ExplicitNullableEnumeration, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i4", "nuint", "E"), expectedCheckedIL: convFromToNullableT("conv.ovf.i4.un", "nuint", "E"));
            conversions(sourceType: "nuint?", destType: "bool?", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nuint?", destType: "char?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u2", "nuint", "char"), expectedCheckedIL: convFromToNullableT("conv.ovf.u2.un", "nuint", "char"));
            conversions(sourceType: "nuint?", destType: "sbyte?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i1", "nuint", "sbyte"), expectedCheckedIL: convFromToNullableT("conv.ovf.i1.un", "nuint", "sbyte"));
            conversions(sourceType: "nuint?", destType: "byte?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u1", "nuint", "byte"), expectedCheckedIL: convFromToNullableT("conv.ovf.u1.un", "nuint", "byte"));
            conversions(sourceType: "nuint?", destType: "short?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i2", "nuint", "short"), expectedCheckedIL: convFromToNullableT("conv.ovf.i2.un", "nuint", "short"));
            conversions(sourceType: "nuint?", destType: "ushort?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u2", "nuint", "ushort"), expectedCheckedIL: convFromToNullableT("conv.ovf.u2.un", "nuint", "ushort"));
            conversions(sourceType: "nuint?", destType: "int?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i4", "nuint", "int"), expectedCheckedIL: convFromToNullableT("conv.ovf.i4.un", "nuint", "int"));
            conversions(sourceType: "nuint?", destType: "uint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u4", "nuint", "uint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u4.un", "nuint", "uint"));
            conversions(sourceType: "nuint?", destType: "long?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u8", "nuint", "long"), expectedCheckedIL: convFromToNullableT("conv.ovf.i8.un", "nuint", "long"));
            conversions(sourceType: "nuint?", destType: "ulong?", ImplicitNullableNumeric, expectedImplicitIL: convFromToNullableT("conv.u8", "nuint", "ulong"), expectedExplicitIL: convFromToNullableT("conv.u8", "nuint", "ulong"));
            conversions(sourceType: "nuint?", destType: "float?", ImplicitNullableNumeric, expectedImplicitIL: convRUnFromToNullableT("conv.r4", "nuint", "float"), expectedExplicitIL: convRUnFromToNullableT("conv.r4", "nuint", "float"), null);
            conversions(sourceType: "nuint?", destType: "double?", ImplicitNullableNumeric, expectedImplicitIL: convRUnFromToNullableT("conv.r8", "nuint", "double"), expectedExplicitIL: convRUnFromToNullableT("conv.r8", "nuint", "double"), null);
            conversions(sourceType: "nuint?", destType: "decimal?", ImplicitNullableNumeric,
@"{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (nuint? V_0,
                decimal? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""decimal?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly nuint nuint?.GetValueOrDefault()""
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
  IL_0004:  call       ""readonly bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""decimal?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly nuint nuint?.GetValueOrDefault()""
  IL_001c:  conv.u8
  IL_001d:  call       ""decimal decimal.op_Implicit(ulong)""
  IL_0022:  newobj     ""decimal?..ctor(decimal)""
  IL_0027:  ret
}");
            conversions(sourceType: "nuint?", destType: "System.IntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size       34 (0x22)
  .maxstack  1
  .locals init (nuint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly nuint nuint?.GetValueOrDefault()""
  IL_001c:  newobj     ""nint?..ctor(nint)""
  IL_0021:  ret
}
",
                expectedCheckedIL: convFromToNullableT("conv.ovf.i.un", "nuint", "nint"));
            conversions(sourceType: "nuint?", destType: "System.UIntPtr?", Identity, expectedImplicitIL: convNone, expectedExplicitIL: convNone);

            // System.IntPtr to type
            conversions(sourceType: "System.IntPtr", destType: "object", Boxing,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""nint""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""nint""
  IL_0006:  ret
}");
            conversions(sourceType: "System.IntPtr", destType: "string", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.IntPtr", destType: "void*", ExplicitIntegerToPointer, expectedImplicitIL: null, expectedExplicitIL: convNone, expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "System.IntPtr", destType: "delegate*<void>", ExplicitIntegerToPointer, expectedImplicitIL: null, expectedExplicitIL: convNone, expectedCheckedIL: conv("conv.ovf.u"));

            conversions(sourceType: "System.IntPtr", destType: "void*", ExplicitIntegerToPointer, expectedImplicitIL: null, expectedExplicitIL: convNone, expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "System.IntPtr", destType: "delegate*<void>", ExplicitIntegerToPointer, expectedImplicitIL: null, expectedExplicitIL: convNone, expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "System.IntPtr", destType: "E", ExplicitEnumeration, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i4"), expectedCheckedIL: conv("conv.ovf.i4"));
            conversions(sourceType: "System.IntPtr", destType: "bool", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.IntPtr", destType: "char", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.u2"), expectedCheckedIL: conv("conv.ovf.u2"));
            conversions(sourceType: "System.IntPtr", destType: "sbyte", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i1"), expectedCheckedIL: conv("conv.ovf.i1"));
            conversions(sourceType: "System.IntPtr", destType: "byte", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.u1"), expectedCheckedIL: conv("conv.ovf.u1"));
            conversions(sourceType: "System.IntPtr", destType: "short", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i2"), expectedCheckedIL: conv("conv.ovf.i2"));
            conversions(sourceType: "System.IntPtr", destType: "ushort", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.u2"), expectedCheckedIL: conv("conv.ovf.u2"));
            conversions(sourceType: "System.IntPtr", destType: "int", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i4"), expectedCheckedIL: conv("conv.ovf.i4"));
            conversions(sourceType: "System.IntPtr", destType: "uint", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.u4"), expectedCheckedIL: conv("conv.ovf.u4"));
            conversions(sourceType: "System.IntPtr", destType: "long", ImplicitNumeric, expectedImplicitIL: conv("conv.i8"), expectedExplicitIL: conv("conv.i8"));
            conversions(sourceType: "System.IntPtr", destType: "ulong", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i8"), expectedCheckedIL: conv("conv.ovf.u8"));
            conversions(sourceType: "System.IntPtr", destType: "float", ImplicitNumeric, expectedImplicitIL: conv("conv.r4"), expectedExplicitIL: conv("conv.r4"));
            conversions(sourceType: "System.IntPtr", destType: "double", ImplicitNumeric, expectedImplicitIL: conv("conv.r8"), expectedExplicitIL: conv("conv.r8"));
            conversions(sourceType: "System.IntPtr", destType: "decimal", ImplicitNumeric,
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
            conversions(sourceType: "System.IntPtr", destType: "System.IntPtr", Identity, expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "System.IntPtr", destType: "System.UIntPtr", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convNone, expectedCheckedIL: conv("conv.ovf.u"));

            // System.IntPtr to nullable type
            conversions(sourceType: "System.IntPtr", destType: "E?", ExplicitNullableEnumeration, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i4", "E"), expectedCheckedIL: convToNullableT("conv.ovf.i4", "E"));
            conversions(sourceType: "System.IntPtr", destType: "bool?", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.IntPtr", destType: "char?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u2", "char"), expectedCheckedIL: convToNullableT("conv.ovf.u2", "char"));
            conversions(sourceType: "System.IntPtr", destType: "sbyte?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i1", "sbyte"), expectedCheckedIL: convToNullableT("conv.ovf.i1", "sbyte"));
            conversions(sourceType: "System.IntPtr", destType: "byte?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u1", "byte"), expectedCheckedIL: convToNullableT("conv.ovf.u1", "byte"));
            conversions(sourceType: "System.IntPtr", destType: "short?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i2", "short"), expectedCheckedIL: convToNullableT("conv.ovf.i2", "short"));
            conversions(sourceType: "System.IntPtr", destType: "ushort?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u2", "ushort"), expectedCheckedIL: convToNullableT("conv.ovf.u2", "ushort"));
            conversions(sourceType: "System.IntPtr", destType: "int?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i4", "int"), expectedCheckedIL: convToNullableT("conv.ovf.i4", "int"));
            conversions(sourceType: "System.IntPtr", destType: "uint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u4", "uint"), expectedCheckedIL: convToNullableT("conv.ovf.u4", "uint"));
            conversions(sourceType: "System.IntPtr", destType: "long?", ImplicitNullableNumeric, expectedImplicitIL: convToNullableT("conv.i8", "long"), expectedExplicitIL: convToNullableT("conv.i8", "long"));
            conversions(sourceType: "System.IntPtr", destType: "ulong?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i8", "ulong"), expectedCheckedIL: convToNullableT("conv.ovf.u8", "ulong"));
            conversions(sourceType: "System.IntPtr", destType: "float?", ImplicitNullableNumeric, expectedImplicitIL: convToNullableT("conv.r4", "float"), expectedExplicitIL: convToNullableT("conv.r4", "float"), null);
            conversions(sourceType: "System.IntPtr", destType: "double?", ImplicitNullableNumeric, expectedImplicitIL: convToNullableT("conv.r8", "double"), expectedExplicitIL: convToNullableT("conv.r8", "double"), null);
            conversions(sourceType: "System.IntPtr", destType: "decimal?", ImplicitNullableNumeric,
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
            conversions(sourceType: "System.IntPtr", destType: "System.IntPtr?", ImplicitNullableIdentity,
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
            conversions(sourceType: "System.IntPtr", destType: "System.UIntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nuint?..ctor(nuint)""
  IL_0006:  ret
}
",
                expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));

            // nullable System.IntPtr to type
            conversions(sourceType: "System.IntPtr?", destType: "object", Boxing,
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
            conversions(sourceType: "System.IntPtr?", destType: "string", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.IntPtr?", destType: "void*", ExplicitUserDefined, expectedImplicitIL: null, expectedExplicitIL: convExplicitFromNullableT("nint", "void* nint.op_Explicit(nint)"));
            conversions(sourceType: "System.IntPtr?", destType: "delegate*<void>", ExplicitUserDefined, expectedImplicitIL: null, expectedExplicitIL: convExplicitFromNullableT("nint", "void* nint.op_Explicit(nint)"));
            conversions(sourceType: "System.IntPtr?", destType: "E", ExplicitNullableEnumeration, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i4", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.i4", "nint"));
            conversions(sourceType: "System.IntPtr?", destType: "bool", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.IntPtr?", destType: "char", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u2", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.u2", "nint"));
            conversions(sourceType: "System.IntPtr?", destType: "sbyte", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i1", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.i1", "nint"));
            conversions(sourceType: "System.IntPtr?", destType: "byte", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u1", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.u1", "nint"));
            conversions(sourceType: "System.IntPtr?", destType: "short", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i2", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.i2", "nint"));
            conversions(sourceType: "System.IntPtr?", destType: "ushort", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u2", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.u2", "nint"));
            conversions(sourceType: "System.IntPtr?", destType: "int", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i4", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.i4", "nint"));
            conversions(sourceType: "System.IntPtr?", destType: "uint", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u4", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.u4", "nint"));
            conversions(sourceType: "System.IntPtr?", destType: "long", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i8", "nint"));
            conversions(sourceType: "System.IntPtr?", destType: "ulong", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i8", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.u8", "nint"));
            conversions(sourceType: "System.IntPtr?", destType: "float", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.r4", "nint"));
            conversions(sourceType: "System.IntPtr?", destType: "double", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.r8", "nint"));
            conversions(sourceType: "System.IntPtr?", destType: "decimal", ExplicitNullableImplicitNumeric, expectedImplicitIL: null,
@"{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly nint nint?.Value.get""
  IL_0007:  conv.i8
  IL_0008:  call       ""decimal decimal.op_Implicit(long)""
  IL_000d:  ret
}");
            conversions(sourceType: "System.IntPtr?", destType: "System.IntPtr", ExplicitNullableIdentity, expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly nint nint?.Value.get""
  IL_0007:  ret
}");
            conversions(sourceType: "System.IntPtr?", destType: "System.UIntPtr", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly nint nint?.Value.get""
  IL_0007:  ret
}
",
                expectedCheckedIL: convFromNullableT("conv.ovf.u", "nint"));

            // nullable System.IntPtr to nullable type
            conversions(sourceType: "System.IntPtr?", destType: "E?", ExplicitNullableEnumeration, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i4", "nint", "E"), expectedCheckedIL: convFromToNullableT("conv.ovf.i4", "nint", "E"));
            conversions(sourceType: "System.IntPtr?", destType: "bool?", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.IntPtr?", destType: "char?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u2", "nint", "char"), expectedCheckedIL: convFromToNullableT("conv.ovf.u2", "nint", "char"));
            conversions(sourceType: "System.IntPtr?", destType: "sbyte?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i1", "nint", "sbyte"), expectedCheckedIL: convFromToNullableT("conv.ovf.i1", "nint", "sbyte"));
            conversions(sourceType: "System.IntPtr?", destType: "byte?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u1", "nint", "byte"), expectedCheckedIL: convFromToNullableT("conv.ovf.u1", "nint", "byte"));
            conversions(sourceType: "System.IntPtr?", destType: "short?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i2", "nint", "short"), expectedCheckedIL: convFromToNullableT("conv.ovf.i2", "nint", "short"));
            conversions(sourceType: "System.IntPtr?", destType: "ushort?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u2", "nint", "ushort"), expectedCheckedIL: convFromToNullableT("conv.ovf.u2", "nint", "ushort"));
            conversions(sourceType: "System.IntPtr?", destType: "int?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i4", "nint", "int"), expectedCheckedIL: convFromToNullableT("conv.ovf.i4", "nint", "int"));
            conversions(sourceType: "System.IntPtr?", destType: "uint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u4", "nint", "uint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u4", "nint", "uint"));
            conversions(sourceType: "System.IntPtr?", destType: "long?", ImplicitNullableNumeric, expectedImplicitIL: convFromToNullableT("conv.i8", "nint", "long"), expectedExplicitIL: convFromToNullableT("conv.i8", "nint", "long"));
            conversions(sourceType: "System.IntPtr?", destType: "ulong?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i8", "nint", "ulong"), expectedCheckedIL: convFromToNullableT("conv.ovf.u8", "nint", "ulong"));
            conversions(sourceType: "System.IntPtr?", destType: "float?", ImplicitNullableNumeric, expectedImplicitIL: convFromToNullableT("conv.r4", "nint", "float"), expectedExplicitIL: convFromToNullableT("conv.r4", "nint", "float"), null);
            conversions(sourceType: "System.IntPtr?", destType: "double?", ImplicitNullableNumeric, expectedImplicitIL: convFromToNullableT("conv.r8", "nint", "double"), expectedExplicitIL: convFromToNullableT("conv.r8", "nint", "double"), null);
            conversions(sourceType: "System.IntPtr?", destType: "decimal?", ImplicitNullableNumeric,
@"{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (nint? V_0,
                decimal? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""decimal?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly nint nint?.GetValueOrDefault()""
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
  IL_0004:  call       ""readonly bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""decimal?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly nint nint?.GetValueOrDefault()""
  IL_001c:  conv.i8
  IL_001d:  call       ""decimal decimal.op_Implicit(long)""
  IL_0022:  newobj     ""decimal?..ctor(decimal)""
  IL_0027:  ret
}");
            conversions(sourceType: "System.IntPtr?", destType: "System.IntPtr?", Identity, expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "System.IntPtr?", destType: "System.UIntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size       34 (0x22)
  .maxstack  1
  .locals init (nint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly nint nint?.GetValueOrDefault()""
  IL_001c:  newobj     ""nuint?..ctor(nuint)""
  IL_0021:  ret
}
",
                expectedCheckedIL: convFromToNullableT("conv.ovf.u", "nint", "nuint"));

            // type to System.IntPtr
            conversions(sourceType: "object", destType: "System.IntPtr", Unboxing, expectedImplicitIL: null,
 @"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""nint""
  IL_0006:  ret
}");
            conversions(sourceType: "string", destType: "System.IntPtr", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "void*", destType: "System.IntPtr", ExplicitPointerToInteger, expectedImplicitIL: null, expectedExplicitIL: convNone, expectedCheckedIL: conv("conv.ovf.i.un"));
            conversions(sourceType: "delegate*<void>", destType: "System.IntPtr", ExplicitPointerToInteger, expectedImplicitIL: null, expectedExplicitIL: convNone, expectedCheckedIL: conv("conv.ovf.i.un"));
            conversions(sourceType: "E", destType: "System.IntPtr", ExplicitEnumeration, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"));
            conversions(sourceType: "bool", destType: "System.IntPtr", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char", destType: "System.IntPtr", ImplicitNumeric, expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            conversions(sourceType: "sbyte", destType: "System.IntPtr", ImplicitNumeric, expectedImplicitIL: conv("conv.i"), expectedExplicitIL: conv("conv.i"));
            conversions(sourceType: "byte", destType: "System.IntPtr", ImplicitNumeric, expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            conversions(sourceType: "short", destType: "System.IntPtr", ImplicitNumeric, expectedImplicitIL: conv("conv.i"), expectedExplicitIL: conv("conv.i"));
            conversions(sourceType: "ushort", destType: "System.IntPtr", ImplicitNumeric, expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            conversions(sourceType: "int", destType: "System.IntPtr", ImplicitNumeric, expectedImplicitIL: conv("conv.i"), expectedExplicitIL: conv("conv.i"));
            conversions(sourceType: "uint", destType: "System.IntPtr", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.u"), expectedCheckedIL: conv("conv.ovf.i.un"));
            conversions(sourceType: "long", destType: "System.IntPtr", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.i"));
            conversions(sourceType: "ulong", destType: "System.IntPtr", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.i.un"));
            conversions(sourceType: "nint", destType: "System.IntPtr", Identity, expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "nuint", destType: "System.IntPtr", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convNone, expectedCheckedIL: conv("conv.ovf.i.un"));
            conversions(sourceType: "float", destType: "System.IntPtr", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.i"));
            conversions(sourceType: "double", destType: "System.IntPtr", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.i"));
            conversions(sourceType: "decimal", destType: "System.IntPtr", ExplicitNumeric, expectedImplicitIL: null,
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
            conversions(sourceType: "System.UIntPtr", destType: "System.IntPtr", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convNone, expectedCheckedIL: conv("conv.ovf.i.un"));

            // type to nullable System.IntPtr
            conversions(sourceType: "object", destType: "System.IntPtr?", Unboxing, expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""nint?""
  IL_0006:  ret
}");
            conversions(sourceType: "string", destType: "System.IntPtr?", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "void*", destType: "System.IntPtr?", ExplicitNullablePointerToInteger, expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nint?..ctor(nint)""
  IL_0006:  ret
}",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i.un
  IL_0002:  newobj     ""nint?..ctor(nint)""
  IL_0007:  ret
}");
            conversions(sourceType: "delegate*<void>", destType: "System.IntPtr?", ExplicitNullablePointerToInteger, expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nint?..ctor(nint)""
  IL_0006:  ret
}",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i.un
  IL_0002:  newobj     ""nint?..ctor(nint)""
  IL_0007:  ret
}");
            conversions(sourceType: "E", destType: "System.IntPtr?", ExplicitNullableEnumeration, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nint"));
            conversions(sourceType: "bool", destType: "System.IntPtr?", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char", destType: "System.IntPtr?", ImplicitNullableNumeric, expectedImplicitIL: convToNullableT("conv.u", "nint"), expectedExplicitIL: convToNullableT("conv.u", "nint"));
            conversions(sourceType: "sbyte", destType: "System.IntPtr?", ImplicitNullableNumeric, expectedImplicitIL: convToNullableT("conv.i", "nint"), expectedExplicitIL: convToNullableT("conv.i", "nint"));
            conversions(sourceType: "byte", destType: "System.IntPtr?", ImplicitNullableNumeric, expectedImplicitIL: convToNullableT("conv.u", "nint"), expectedExplicitIL: convToNullableT("conv.u", "nint"));
            conversions(sourceType: "short", destType: "System.IntPtr?", ImplicitNullableNumeric, expectedImplicitIL: convToNullableT("conv.i", "nint"), expectedExplicitIL: convToNullableT("conv.i", "nint"));
            conversions(sourceType: "ushort", destType: "System.IntPtr?", ImplicitNullableNumeric, expectedImplicitIL: convToNullableT("conv.u", "nint"), expectedExplicitIL: convToNullableT("conv.u", "nint"));
            conversions(sourceType: "int", destType: "System.IntPtr?", ImplicitNullableNumeric, expectedImplicitIL: convToNullableT("conv.i", "nint"), expectedExplicitIL: convToNullableT("conv.i", "nint"));
            conversions(sourceType: "uint", destType: "System.IntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u", "nint"), expectedCheckedIL: convToNullableT("conv.ovf.i.un", "nint"));
            conversions(sourceType: "long", destType: "System.IntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nint"), expectedCheckedIL: convToNullableT("conv.ovf.i", "nint"));
            conversions(sourceType: "ulong", destType: "System.IntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nint"), expectedCheckedIL: convToNullableT("conv.ovf.i.un", "nint"));
            conversions(sourceType: "nint", destType: "System.IntPtr?", ImplicitNullableIdentity,
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
            conversions(sourceType: "nuint", destType: "System.IntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nint?..ctor(nint)""
  IL_0006:  ret
}
",
                expectedCheckedIL: convToNullableT("conv.ovf.i.un", "nint"));
            conversions(sourceType: "float", destType: "System.IntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nint"), expectedCheckedIL: convToNullableT("conv.ovf.i", "nint"));
            conversions(sourceType: "double", destType: "System.IntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nint"), expectedCheckedIL: convToNullableT("conv.ovf.i", "nint"));
            conversions(sourceType: "decimal", destType: "System.IntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null,
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
            conversions(sourceType: "System.IntPtr", destType: "System.IntPtr?", ImplicitNullableIdentity,
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
            conversions(sourceType: "System.UIntPtr", destType: "System.IntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nint?..ctor(nint)""
  IL_0006:  ret
}
",
                expectedCheckedIL: convToNullableT("conv.ovf.i.un", "nint"));

            // nullable type to System.IntPtr
            conversions(sourceType: "E?", destType: "System.IntPtr", ExplicitNullableEnumeration, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "E"));
            conversions(sourceType: "bool?", destType: "System.IntPtr", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char?", destType: "System.IntPtr", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "char"));
            conversions(sourceType: "sbyte?", destType: "System.IntPtr", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "sbyte"));
            conversions(sourceType: "byte?", destType: "System.IntPtr", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "byte"));
            conversions(sourceType: "short?", destType: "System.IntPtr", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "short"));
            conversions(sourceType: "ushort?", destType: "System.IntPtr", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "ushort"));
            conversions(sourceType: "int?", destType: "System.IntPtr", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "int"));
            conversions(sourceType: "uint?", destType: "System.IntPtr", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "uint"), expectedCheckedIL: convFromNullableT("conv.ovf.i.un", "uint"));
            conversions(sourceType: "long?", destType: "System.IntPtr", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "long"), expectedCheckedIL: convFromNullableT("conv.ovf.i", "long"));
            conversions(sourceType: "ulong?", destType: "System.IntPtr", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "ulong"), expectedCheckedIL: convFromNullableT("conv.ovf.i.un", "ulong"));
            conversions(sourceType: "nint?", destType: "System.IntPtr", ExplicitNullableIdentity, expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly nint nint?.Value.get""
  IL_0007:  ret
}");
            conversions(sourceType: "nuint?", destType: "System.IntPtr", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly nuint nuint?.Value.get""
  IL_0007:  ret
}
",
                expectedCheckedIL: convFromNullableT("conv.ovf.i.un", "nuint"));
            conversions(sourceType: "float?", destType: "System.IntPtr", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "float"), expectedCheckedIL: convFromNullableT("conv.ovf.i", "float"));
            conversions(sourceType: "double?", destType: "System.IntPtr", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "double"), expectedCheckedIL: convFromNullableT("conv.ovf.i", "double"));
            conversions(sourceType: "decimal?", destType: "System.IntPtr", ExplicitNullableNumeric, expectedImplicitIL: null,
@"{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly decimal decimal?.Value.get""
  IL_0007:  call       ""long decimal.op_Explicit(decimal)""
  IL_000c:  conv.i
  IL_000d:  ret
}",
@"{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly decimal decimal?.Value.get""
  IL_0007:  call       ""long decimal.op_Explicit(decimal)""
  IL_000c:  conv.ovf.i
  IL_000d:  ret
}");
            conversions(sourceType: "System.IntPtr?", destType: "System.IntPtr", ExplicitNullableIdentity, expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly nint nint?.Value.get""
  IL_0007:  ret
}");
            conversions(sourceType: "System.UIntPtr?", destType: "System.IntPtr", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly nuint nuint?.Value.get""
  IL_0007:  ret
}
",
                expectedCheckedIL: convFromNullableT("conv.ovf.i.un", "nuint"));

            // nullable type to nullable System.IntPtr
            conversions(sourceType: "E?", destType: "System.IntPtr?", ExplicitNullableEnumeration, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "E", "nint"));
            conversions(sourceType: "bool?", destType: "System.IntPtr?", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char?", destType: "System.IntPtr?", ImplicitNullableNumeric, expectedImplicitIL: convFromToNullableT("conv.u", "char", "nint"), expectedExplicitIL: convFromToNullableT("conv.u", "char", "nint"));
            conversions(sourceType: "sbyte?", destType: "System.IntPtr?", ImplicitNullableNumeric, expectedImplicitIL: convFromToNullableT("conv.i", "sbyte", "nint"), expectedExplicitIL: convFromToNullableT("conv.i", "sbyte", "nint"));
            conversions(sourceType: "byte?", destType: "System.IntPtr?", ImplicitNullableNumeric, expectedImplicitIL: convFromToNullableT("conv.u", "byte", "nint"), expectedExplicitIL: convFromToNullableT("conv.u", "byte", "nint"));
            conversions(sourceType: "short?", destType: "System.IntPtr?", ImplicitNullableNumeric, expectedImplicitIL: convFromToNullableT("conv.i", "short", "nint"), expectedExplicitIL: convFromToNullableT("conv.i", "short", "nint"));
            conversions(sourceType: "ushort?", destType: "System.IntPtr?", ImplicitNullableNumeric, expectedImplicitIL: convFromToNullableT("conv.u", "ushort", "nint"), expectedExplicitIL: convFromToNullableT("conv.u", "ushort", "nint"));
            conversions(sourceType: "int?", destType: "System.IntPtr?", ImplicitNullableNumeric, expectedImplicitIL: convFromToNullableT("conv.i", "int", "nint"), expectedExplicitIL: convFromToNullableT("conv.i", "int", "nint"));
            conversions(sourceType: "uint?", destType: "System.IntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u", "uint", "nint"), expectedCheckedIL: convFromToNullableT("conv.ovf.i.un", "uint", "nint"));
            conversions(sourceType: "long?", destType: "System.IntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "long", "nint"), expectedCheckedIL: convFromToNullableT("conv.ovf.i", "long", "nint"));
            conversions(sourceType: "ulong?", destType: "System.IntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "ulong", "nint"), expectedCheckedIL: convFromToNullableT("conv.ovf.i.un", "ulong", "nint"));
            conversions(sourceType: "nint?", destType: "System.IntPtr?", Identity, expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "nuint?", destType: "System.IntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size       34 (0x22)
  .maxstack  1
  .locals init (nuint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly nuint nuint?.GetValueOrDefault()""
  IL_001c:  newobj     ""nint?..ctor(nint)""
  IL_0021:  ret
}
",
                expectedCheckedIL: convFromToNullableT("conv.ovf.i.un", "nuint", "nint"));
            conversions(sourceType: "float?", destType: "System.IntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "float", "nint"), expectedCheckedIL: convFromToNullableT("conv.ovf.i", "float", "nint"));
            conversions(sourceType: "double?", destType: "System.IntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "double", "nint"), expectedCheckedIL: convFromToNullableT("conv.ovf.i", "double", "nint"));
            conversions(sourceType: "decimal?", destType: "System.IntPtr?", ExplicitNullableNumeric, null,
@"{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (decimal? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool decimal?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly decimal decimal?.GetValueOrDefault()""
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
  IL_0004:  call       ""readonly bool decimal?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly decimal decimal?.GetValueOrDefault()""
  IL_001c:  call       ""long decimal.op_Explicit(decimal)""
  IL_0021:  conv.ovf.i
  IL_0022:  newobj     ""nint?..ctor(nint)""
  IL_0027:  ret
}");
            conversions(sourceType: "System.IntPtr?", destType: "System.IntPtr?", Identity, expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "System.UIntPtr?", destType: "System.IntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size       34 (0x22)
  .maxstack  1
  .locals init (nuint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly nuint nuint?.GetValueOrDefault()""
  IL_001c:  newobj     ""nint?..ctor(nint)""
  IL_0021:  ret
}
",
                expectedCheckedIL: convFromToNullableT("conv.ovf.i.un", "nuint", "nint"));

            // System.UIntPtr to type
            conversions(sourceType: "System.UIntPtr", destType: "object", Boxing,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""nuint""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""nuint""
  IL_0006:  ret
}");
            conversions(sourceType: "System.UIntPtr", destType: "string", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.UIntPtr", destType: "void*", ExplicitIntegerToPointer, expectedImplicitIL: null, expectedExplicitIL: convNone);
            conversions(sourceType: "System.UIntPtr", destType: "delegate*<void>", ExplicitIntegerToPointer, expectedImplicitIL: null, expectedExplicitIL: convNone);
            conversions(sourceType: "System.UIntPtr", destType: "E", ExplicitEnumeration, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i4"), expectedCheckedIL: conv("conv.ovf.i4.un"));
            conversions(sourceType: "System.UIntPtr", destType: "bool", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.UIntPtr", destType: "char", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.u2"), expectedCheckedIL: conv("conv.ovf.u2.un"));
            conversions(sourceType: "System.UIntPtr", destType: "sbyte", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i1"), expectedCheckedIL: conv("conv.ovf.i1.un"));
            conversions(sourceType: "System.UIntPtr", destType: "byte", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.u1"), expectedCheckedIL: conv("conv.ovf.u1.un"));
            conversions(sourceType: "System.UIntPtr", destType: "short", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i2"), expectedCheckedIL: conv("conv.ovf.i2.un"));
            conversions(sourceType: "System.UIntPtr", destType: "ushort", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.u2"), expectedCheckedIL: conv("conv.ovf.u2.un"));
            conversions(sourceType: "System.UIntPtr", destType: "int", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i4"), expectedCheckedIL: conv("conv.ovf.i4.un"));
            conversions(sourceType: "System.UIntPtr", destType: "uint", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.u4"), expectedCheckedIL: conv("conv.ovf.u4.un"));
            conversions(sourceType: "System.UIntPtr", destType: "long", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.u8"), expectedCheckedIL: conv("conv.ovf.i8.un"));
            conversions(sourceType: "System.UIntPtr", destType: "ulong", ImplicitNumeric, expectedImplicitIL: conv("conv.u8"), expectedExplicitIL: conv("conv.u8"));
            conversions(sourceType: "System.UIntPtr", destType: "float", ImplicitNumeric, expectedImplicitIL: convRUn("conv.r4"), expectedExplicitIL: convRUn("conv.r4"));
            conversions(sourceType: "System.UIntPtr", destType: "double", ImplicitNumeric, expectedImplicitIL: convRUn("conv.r8"), expectedExplicitIL: convRUn("conv.r8"));
            conversions(sourceType: "System.UIntPtr", destType: "decimal", ImplicitNumeric,
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
            conversions(sourceType: "System.UIntPtr", destType: "System.IntPtr", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convNone, expectedCheckedIL: conv("conv.ovf.i.un"));
            conversions(sourceType: "System.UIntPtr", destType: "System.UIntPtr", Identity, expectedImplicitIL: convNone, expectedExplicitIL: convNone);

            // System.UIntPtr to nullable type
            conversions(sourceType: "System.UIntPtr", destType: "E?", ExplicitNullableEnumeration, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i4", "E"), expectedCheckedIL: convToNullableT("conv.ovf.i4.un", "E"));
            conversions(sourceType: "System.UIntPtr", destType: "bool?", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.UIntPtr", destType: "char?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u2", "char"), expectedCheckedIL: convToNullableT("conv.ovf.u2.un", "char"));
            conversions(sourceType: "System.UIntPtr", destType: "sbyte?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i1", "sbyte"), expectedCheckedIL: convToNullableT("conv.ovf.i1.un", "sbyte"));
            conversions(sourceType: "System.UIntPtr", destType: "byte?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u1", "byte"), expectedCheckedIL: convToNullableT("conv.ovf.u1.un", "byte"));
            conversions(sourceType: "System.UIntPtr", destType: "short?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i2", "short"), expectedCheckedIL: convToNullableT("conv.ovf.i2.un", "short"));
            conversions(sourceType: "System.UIntPtr", destType: "ushort?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u2", "ushort"), expectedCheckedIL: convToNullableT("conv.ovf.u2.un", "ushort"));
            conversions(sourceType: "System.UIntPtr", destType: "int?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i4", "int"), expectedCheckedIL: convToNullableT("conv.ovf.i4.un", "int"));
            conversions(sourceType: "System.UIntPtr", destType: "uint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u4", "uint"), expectedCheckedIL: convToNullableT("conv.ovf.u4.un", "uint"));
            conversions(sourceType: "System.UIntPtr", destType: "long?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u8", "long"), expectedCheckedIL: convToNullableT("conv.ovf.i8.un", "long"));
            conversions(sourceType: "System.UIntPtr", destType: "ulong?", ImplicitNullableNumeric, expectedImplicitIL: convToNullableT("conv.u8", "ulong"), expectedExplicitIL: convToNullableT("conv.u8", "ulong"));
            conversions(sourceType: "System.UIntPtr", destType: "float?", ImplicitNullableNumeric, expectedImplicitIL: convRUnToNullableT("conv.r4", "float"), expectedExplicitIL: convRUnToNullableT("conv.r4", "float"), null);
            conversions(sourceType: "System.UIntPtr", destType: "double?", ImplicitNullableNumeric, expectedImplicitIL: convRUnToNullableT("conv.r8", "double"), expectedExplicitIL: convRUnToNullableT("conv.r8", "double"), null);
            conversions(sourceType: "System.UIntPtr", destType: "decimal?", ImplicitNullableNumeric,
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
            conversions(sourceType: "System.UIntPtr", destType: "System.IntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nint?..ctor(nint)""
  IL_0006:  ret
}
",
                expectedCheckedIL: convToNullableT("conv.ovf.i.un", "nint"));
            conversions(sourceType: "System.UIntPtr", destType: "System.UIntPtr?", ImplicitNullableIdentity,
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

            // nullable System.UIntPtr to type
            conversions(sourceType: "System.UIntPtr?", destType: "object", Boxing,
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
            conversions(sourceType: "System.UIntPtr?", destType: "string", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.UIntPtr?", destType: "void*", ExplicitUserDefined, expectedImplicitIL: null, expectedExplicitIL: convExplicitFromNullableT("nuint", "void* nuint.op_Explicit(nuint)"));
            conversions(sourceType: "System.UIntPtr?", destType: "delegate*<void>", ExplicitUserDefined, expectedImplicitIL: null, expectedExplicitIL: convExplicitFromNullableT("nuint", "void* nuint.op_Explicit(nuint)"));
            conversions(sourceType: "System.UIntPtr?", destType: "E", ExplicitNullableEnumeration, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i4", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.i4.un", "nuint"));
            conversions(sourceType: "System.UIntPtr?", destType: "bool", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.UIntPtr?", destType: "char", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u2", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.u2.un", "nuint"));
            conversions(sourceType: "System.UIntPtr?", destType: "sbyte", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i1", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.i1.un", "nuint"));
            conversions(sourceType: "System.UIntPtr?", destType: "byte", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u1", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.u1.un", "nuint"));
            conversions(sourceType: "System.UIntPtr?", destType: "short", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i2", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.i2.un", "nuint"));
            conversions(sourceType: "System.UIntPtr?", destType: "ushort", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u2", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.u2.un", "nuint"));
            conversions(sourceType: "System.UIntPtr?", destType: "int", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i4", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.i4.un", "nuint"));
            conversions(sourceType: "System.UIntPtr?", destType: "uint", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u4", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.u4.un", "nuint"));
            conversions(sourceType: "System.UIntPtr?", destType: "long", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u8", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.i8.un", "nuint"));
            conversions(sourceType: "System.UIntPtr?", destType: "ulong", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u8", "nuint"));
            conversions(sourceType: "System.UIntPtr?", destType: "float", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convRUnFromNullableT("conv.r4", "nuint"));
            conversions(sourceType: "System.UIntPtr?", destType: "double", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convRUnFromNullableT("conv.r8", "nuint"));
            conversions(sourceType: "System.UIntPtr?", destType: "decimal", ExplicitNullableImplicitNumeric, expectedImplicitIL: null,
@"{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly nuint nuint?.Value.get""
  IL_0007:  conv.u8
  IL_0008:  call       ""decimal decimal.op_Implicit(ulong)""
  IL_000d:  ret
}");
            conversions(sourceType: "System.UIntPtr?", destType: "System.IntPtr", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly nuint nuint?.Value.get""
  IL_0007:  ret
}
",
                expectedCheckedIL: convFromNullableT("conv.ovf.i.un", "nuint"));
            conversions(sourceType: "System.UIntPtr?", destType: "System.UIntPtr", ExplicitNullableIdentity, expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly nuint nuint?.Value.get""
  IL_0007:  ret
}");

            // nullable System.UIntPtr to nullable type
            conversions(sourceType: "System.UIntPtr?", destType: "E?", ExplicitNullableEnumeration, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i4", "nuint", "E"), expectedCheckedIL: convFromToNullableT("conv.ovf.i4.un", "nuint", "E"));
            conversions(sourceType: "System.UIntPtr?", destType: "bool?", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.UIntPtr?", destType: "char?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u2", "nuint", "char"), expectedCheckedIL: convFromToNullableT("conv.ovf.u2.un", "nuint", "char"));
            conversions(sourceType: "System.UIntPtr?", destType: "sbyte?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i1", "nuint", "sbyte"), expectedCheckedIL: convFromToNullableT("conv.ovf.i1.un", "nuint", "sbyte"));
            conversions(sourceType: "System.UIntPtr?", destType: "byte?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u1", "nuint", "byte"), expectedCheckedIL: convFromToNullableT("conv.ovf.u1.un", "nuint", "byte"));
            conversions(sourceType: "System.UIntPtr?", destType: "short?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i2", "nuint", "short"), expectedCheckedIL: convFromToNullableT("conv.ovf.i2.un", "nuint", "short"));
            conversions(sourceType: "System.UIntPtr?", destType: "ushort?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u2", "nuint", "ushort"), expectedCheckedIL: convFromToNullableT("conv.ovf.u2.un", "nuint", "ushort"));
            conversions(sourceType: "System.UIntPtr?", destType: "int?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i4", "nuint", "int"), expectedCheckedIL: convFromToNullableT("conv.ovf.i4.un", "nuint", "int"));
            conversions(sourceType: "System.UIntPtr?", destType: "uint?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u4", "nuint", "uint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u4.un", "nuint", "uint"));
            conversions(sourceType: "System.UIntPtr?", destType: "long?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u8", "nuint", "long"), expectedCheckedIL: convFromToNullableT("conv.ovf.i8.un", "nuint", "long"));
            conversions(sourceType: "System.UIntPtr?", destType: "ulong?", ImplicitNullableNumeric, expectedImplicitIL: convFromToNullableT("conv.u8", "nuint", "ulong"), expectedExplicitIL: convFromToNullableT("conv.u8", "nuint", "ulong"));
            conversions(sourceType: "System.UIntPtr?", destType: "float?", ImplicitNullableNumeric, expectedImplicitIL: convRUnFromToNullableT("conv.r4", "nuint", "float"), expectedExplicitIL: convRUnFromToNullableT("conv.r4", "nuint", "float"), null);
            conversions(sourceType: "System.UIntPtr?", destType: "double?", ImplicitNullableNumeric, expectedImplicitIL: convRUnFromToNullableT("conv.r8", "nuint", "double"), expectedExplicitIL: convRUnFromToNullableT("conv.r8", "nuint", "double"), null);
            conversions(sourceType: "System.UIntPtr?", destType: "decimal?", ImplicitNullableNumeric,
@"{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (nuint? V_0,
                decimal? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""decimal?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly nuint nuint?.GetValueOrDefault()""
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
  IL_0004:  call       ""readonly bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""decimal?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly nuint nuint?.GetValueOrDefault()""
  IL_001c:  conv.u8
  IL_001d:  call       ""decimal decimal.op_Implicit(ulong)""
  IL_0022:  newobj     ""decimal?..ctor(decimal)""
  IL_0027:  ret
}");
            conversions(sourceType: "System.UIntPtr?", destType: "System.IntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size       34 (0x22)
  .maxstack  1
  .locals init (nuint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly nuint nuint?.GetValueOrDefault()""
  IL_001c:  newobj     ""nint?..ctor(nint)""
  IL_0021:  ret
}
",
                expectedCheckedIL: convFromToNullableT("conv.ovf.i.un", "nuint", "nint"));
            conversions(sourceType: "System.UIntPtr?", destType: "System.UIntPtr?", Identity, expectedImplicitIL: convNone, expectedExplicitIL: convNone);

            // type to System.UIntPtr
            conversions(sourceType: "object", destType: "System.UIntPtr", Unboxing, expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""nuint""
  IL_0006:  ret
}");
            conversions(sourceType: "string", destType: "System.UIntPtr", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "void*", destType: "System.UIntPtr", ExplicitPointerToInteger, expectedImplicitIL: null, expectedExplicitIL: convNone);
            conversions(sourceType: "delegate*<void>", destType: "System.UIntPtr", ExplicitPointerToInteger, expectedImplicitIL: null, expectedExplicitIL: convNone);
            conversions(sourceType: "E", destType: "System.UIntPtr", ExplicitEnumeration, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "bool", destType: "System.UIntPtr", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char", destType: "System.UIntPtr", ImplicitNumeric, expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            conversions(sourceType: "sbyte", destType: "System.UIntPtr", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "byte", destType: "System.UIntPtr", ImplicitNumeric, expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            conversions(sourceType: "short", destType: "System.UIntPtr", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "ushort", destType: "System.UIntPtr", ImplicitNumeric, expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            conversions(sourceType: "int", destType: "System.UIntPtr", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "uint", destType: "System.UIntPtr", ImplicitNumeric, expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            conversions(sourceType: "long", destType: "System.UIntPtr", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.u"), expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "ulong", destType: "System.UIntPtr", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.u"), expectedCheckedIL: conv("conv.ovf.u.un"));
            conversions(sourceType: "nint", destType: "System.UIntPtr", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convNone, expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "nuint", destType: "System.UIntPtr", Identity, expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "float", destType: "System.UIntPtr", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.u"), expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "double", destType: "System.UIntPtr", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: conv("conv.u"), expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "decimal", destType: "System.UIntPtr", ExplicitNumeric, expectedImplicitIL: null,
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
            conversions(sourceType: "System.IntPtr", destType: "System.UIntPtr", ExplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convNone, expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "System.UIntPtr", destType: "System.UIntPtr", Identity, expectedImplicitIL: convNone, expectedExplicitIL: convNone);

            // type to nullable System.UIntPtr
            conversions(sourceType: "object", destType: "System.UIntPtr?", Unboxing, expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""nuint?""
  IL_0006:  ret
}");
            conversions(sourceType: "string", destType: "System.UIntPtr?", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "void*", destType: "System.UIntPtr?", ExplicitNullablePointerToInteger, expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nuint?..ctor(nuint)""
  IL_0006:  ret
}");
            conversions(sourceType: "delegate*<void>", destType: "System.UIntPtr?", ExplicitNullablePointerToInteger, expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nuint?..ctor(nuint)""
  IL_0006:  ret
}");
            conversions(sourceType: "E", destType: "System.UIntPtr?", ExplicitNullableEnumeration, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "bool", destType: "System.UIntPtr?", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char", destType: "System.UIntPtr?", ImplicitNullableNumeric, expectedImplicitIL: convToNullableT("conv.u", "nuint"), expectedExplicitIL: convToNullableT("conv.u", "nuint"));
            conversions(sourceType: "sbyte", destType: "System.UIntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "byte", destType: "System.UIntPtr?", ImplicitNullableNumeric, expectedImplicitIL: convToNullableT("conv.u", "nuint"), expectedExplicitIL: convToNullableT("conv.u", "nuint"));
            conversions(sourceType: "short", destType: "System.UIntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "ushort", destType: "System.UIntPtr?", ImplicitNullableNumeric, expectedImplicitIL: convToNullableT("conv.u", "nuint"), expectedExplicitIL: convToNullableT("conv.u", "nuint"));
            conversions(sourceType: "int", destType: "System.UIntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "uint", destType: "System.UIntPtr?", ImplicitNullableNumeric, expectedImplicitIL: convToNullableT("conv.u", "nuint"), expectedExplicitIL: convToNullableT("conv.u", "nuint"));
            conversions(sourceType: "long", destType: "System.UIntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "ulong", destType: "System.UIntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u.un", "nuint"));
            conversions(sourceType: "nint", destType: "System.UIntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nuint?..ctor(nuint)""
  IL_0006:  ret
}
",
                expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "nuint", destType: "System.UIntPtr?", ImplicitNullableIdentity,
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
            conversions(sourceType: "float", destType: "System.UIntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "double", destType: "System.UIntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "decimal", destType: "System.UIntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null,
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
            conversions(sourceType: "System.IntPtr", destType: "System.UIntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nuint?..ctor(nuint)""
  IL_0006:  ret
}
",
                expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "System.UIntPtr", destType: "System.UIntPtr?", ImplicitNullableIdentity,
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

            // nullable type to System.UIntPtr
            conversions(sourceType: "E?", destType: "System.UIntPtr", ExplicitNullableEnumeration, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "E"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "E"));
            conversions(sourceType: "bool?", destType: "System.UIntPtr", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char?", destType: "System.UIntPtr", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "char"));
            conversions(sourceType: "sbyte?", destType: "System.UIntPtr", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "sbyte"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "sbyte"));
            conversions(sourceType: "byte?", destType: "System.UIntPtr", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "byte"));
            conversions(sourceType: "short?", destType: "System.UIntPtr", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "short"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "short"));
            conversions(sourceType: "ushort?", destType: "System.UIntPtr", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "ushort"));
            conversions(sourceType: "int?", destType: "System.UIntPtr", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "int"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "int"));
            conversions(sourceType: "uint?", destType: "System.UIntPtr", ExplicitNullableImplicitNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "uint"));
            conversions(sourceType: "long?", destType: "System.UIntPtr", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "long"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "long"));
            conversions(sourceType: "ulong?", destType: "System.UIntPtr", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "ulong"), expectedCheckedIL: convFromNullableT("conv.ovf.u.un", "ulong"));
            conversions(sourceType: "nint?", destType: "System.UIntPtr", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly nint nint?.Value.get""
  IL_0007:  ret
}
",
                expectedCheckedIL: convFromNullableT("conv.ovf.u", "nint"));
            conversions(sourceType: "nuint?", destType: "System.UIntPtr", ExplicitNullableIdentity, expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly nuint nuint?.Value.get""
  IL_0007:  ret
}");
            conversions(sourceType: "float?", destType: "System.UIntPtr", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "float"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "float"));
            conversions(sourceType: "double?", destType: "System.UIntPtr", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "double"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "double"));
            conversions(sourceType: "decimal?", destType: "System.UIntPtr", ExplicitNullableNumeric, expectedImplicitIL: null,
@"{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly decimal decimal?.Value.get""
  IL_0007:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_000c:  conv.u
  IL_000d:  ret
}",
@"{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly decimal decimal?.Value.get""
  IL_0007:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_000c:  conv.ovf.u.un
  IL_000d:  ret
}");
            conversions(sourceType: "System.IntPtr?", destType: "System.UIntPtr", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly nint nint?.Value.get""
  IL_0007:  ret
}
",
                expectedCheckedIL: convFromNullableT("conv.ovf.u", "nint"));
            conversions(sourceType: "System.UIntPtr?", destType: "System.UIntPtr", ExplicitNullableIdentity, expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly nuint nuint?.Value.get""
  IL_0007:  ret
}");

            // nullable type to nullable System.UIntPtr
            conversions(sourceType: "E?", destType: "System.UIntPtr?", ExplicitNullableEnumeration, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "E", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "E", "nuint"));
            conversions(sourceType: "bool?", destType: "System.UIntPtr?", NoConversion, expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char?", destType: "System.UIntPtr?", ImplicitNullableNumeric, expectedImplicitIL: convFromToNullableT("conv.u", "char", "nuint"), expectedExplicitIL: convFromToNullableT("conv.u", "char", "nuint"));
            conversions(sourceType: "sbyte?", destType: "System.UIntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "sbyte", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "sbyte", "nuint"));
            conversions(sourceType: "byte?", destType: "System.UIntPtr?", ImplicitNullableNumeric, expectedImplicitIL: convFromToNullableT("conv.u", "byte", "nuint"), expectedExplicitIL: convFromToNullableT("conv.u", "byte", "nuint"));
            conversions(sourceType: "short?", destType: "System.UIntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "short", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "short", "nuint"));
            conversions(sourceType: "ushort?", destType: "System.UIntPtr?", ImplicitNullableNumeric, expectedImplicitIL: convFromToNullableT("conv.u", "ushort", "nuint"), expectedExplicitIL: convFromToNullableT("conv.u", "ushort", "nuint"));
            conversions(sourceType: "int?", destType: "System.UIntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "int", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "int", "nuint"));
            conversions(sourceType: "uint?", destType: "System.UIntPtr?", ImplicitNullableNumeric, expectedImplicitIL: convFromToNullableT("conv.u", "uint", "nuint"), expectedExplicitIL: convFromToNullableT("conv.u", "uint", "nuint"));
            conversions(sourceType: "long?", destType: "System.UIntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u", "long", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "long", "nuint"));
            conversions(sourceType: "ulong?", destType: "System.UIntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u", "ulong", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u.un", "ulong", "nuint"));
            conversions(sourceType: "nint?", destType: "System.UIntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size       34 (0x22)
  .maxstack  1
  .locals init (nint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly nint nint?.GetValueOrDefault()""
  IL_001c:  newobj     ""nuint?..ctor(nuint)""
  IL_0021:  ret
}
",
                expectedCheckedIL: convFromToNullableT("conv.ovf.u", "nint", "nuint"));
            conversions(sourceType: "nuint?", destType: "System.UIntPtr?", Identity, expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "float?", destType: "System.UIntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u", "float", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "float", "nuint"));
            conversions(sourceType: "double?", destType: "System.UIntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u", "double", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "double", "nuint"));
            conversions(sourceType: "decimal?", destType: "System.UIntPtr?", ExplicitNullableNumeric, null,
@"{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (decimal? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool decimal?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly decimal decimal?.GetValueOrDefault()""
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
  IL_0004:  call       ""readonly bool decimal?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly decimal decimal?.GetValueOrDefault()""
  IL_001c:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_0021:  conv.ovf.u.un
  IL_0022:  newobj     ""nuint?..ctor(nuint)""
  IL_0027:  ret
}");
            conversions(sourceType: "System.IntPtr?", destType: "System.UIntPtr?", ExplicitNullableNumeric, expectedImplicitIL: null, expectedExplicitIL:
@"
{
  // Code size       34 (0x22)
  .maxstack  1
  .locals init (nint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly nint nint?.GetValueOrDefault()""
  IL_001c:  newobj     ""nuint?..ctor(nuint)""
  IL_0021:  ret
}
",
                expectedCheckedIL: convFromToNullableT("conv.ovf.u", "nint", "nuint"));
            conversions(sourceType: "System.UIntPtr?", destType: "System.UIntPtr?", Identity, expectedImplicitIL: convNone, expectedExplicitIL: convNone);

            return;

            void convert(string sourceType,
                string destType,
                string expectedIL,
                bool useExplicitCast,
                bool useChecked,
                ConversionKind[] expectedConversions,
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
                    new[] { Diagnostic(expectedErrorCode, value).WithArguments(AsNative(sourceType), AsNative(destType)) };
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
}}
enum E {{ }}
";
                var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithAllowUnsafe(useUnsafeContext), parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);

                comp.VerifyDiagnostics(expectedDiagnostics);

                var tree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(tree);
                var expr = tree.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression;
                var typeInfo = model.GetTypeInfo(expr);

                if (!useExplicitCast)
                {
                    var destTypeSymbol = ((MethodSymbol)comp.GetMember("Program.Convert")).ReturnType.GetPublicSymbol();
                    AssertMatches(expectedConversions, model.ClassifyConversion(expr, destTypeSymbol));
                    Assert.Equal(AsNative(sourceType), typeInfo.Type.ToString());
                    Assert.Equal(AsNative(destType), typeInfo.ConvertedType.ToString());
                }

                if (expectedIL != null)
                {
                    var verifier = CompileAndVerify(comp, verify: useUnsafeContext ? Verification.Skipped : Verification.FailsPEVerify);
                    verifier.VerifyIL("Program.Convert", expectedIL);
                }

                static bool useUnsafe(string type) => type == "void*" || type == "delegate*<void>";
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

            void unifiedUnaryOp(string op, string opType, string expectedSymbol = null, string operand = null, string expectedResult = null, string expectedIL = "", DiagnosticDescription diagnostic = null)
            {
                Assert.True(opType is "System.IntPtr" or "System.UIntPtr" or "System.IntPtr?" or "System.UIntPtr?");
                unaryOp(op, opType, expectedSymbol, operand, expectedResult, expectedIL, diagnostic);
                unaryOp(op, AsNative(opType), expectedSymbol, operand, expectedResult, expectedIL, diagnostic);
            }

            void unaryOp(string op, string opType, string expectedSymbol, string operand, string expectedResult, string expectedIL, DiagnosticDescription diagnostic)
            {
                operand ??= "default";
                if (expectedSymbol == null && diagnostic == null)
                {
                    diagnostic = Diagnostic(ErrorCode.ERR_BadUnaryOp, $"{op}operand").WithArguments(op, AsNative(opType));
                }

                unaryOperator(op, opType, opType, expectedSymbol, operand, expectedResult, expectedIL, diagnostic != null ? new[] { diagnostic } : Array.Empty<DiagnosticDescription>());
            }

            // unary operator+
            unifiedUnaryOp("+", "System.IntPtr", "nint nint.op_UnaryPlus(nint value)", "3", "3",
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
            unifiedUnaryOp("+", "System.UIntPtr", "nuint nuint.op_UnaryPlus(nuint value)", "3", "3",
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");

            // unary operator-
            unifiedUnaryOp("-", "System.IntPtr", "nint nint.op_UnaryNegation(nint value)", "3", "-3",
@"{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  neg
  IL_0002:  ret
}");
            unifiedUnaryOp("-", "System.UIntPtr");

            // unary operator!
            unifiedUnaryOp("!", "System.IntPtr");
            unifiedUnaryOp("!", "System.UIntPtr");

            // unary operator~
            unifiedUnaryOp("~", "System.IntPtr", "nint nint.op_OnesComplement(nint value)", "3", "-4",
@"{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  not
  IL_0002:  ret
}");
            unifiedUnaryOp("~", "System.UIntPtr", "nuint nuint.op_OnesComplement(nuint value)", "3", getComplement(3),
@"{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  not
  IL_0002:  ret
}");

            // lifted unary operator+
            unifiedUnaryOp("+", "System.IntPtr?", "nint nint.op_UnaryPlus(nint value)", "3", "3",
@"{
  // Code size       34 (0x22)
  .maxstack  1
  .locals init (nint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly nint nint?.GetValueOrDefault()""
  IL_001c:  newobj     ""nint?..ctor(nint)""
  IL_0021:  ret
}");
            unifiedUnaryOp("+", "System.UIntPtr?", "nuint nuint.op_UnaryPlus(nuint value)", "3", "3",
@"{
  // Code size       34 (0x22)
  .maxstack  1
  .locals init (nuint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly nuint nuint?.GetValueOrDefault()""
  IL_001c:  newobj     ""nuint?..ctor(nuint)""
  IL_0021:  ret
}");

            // lifted unary operator-
            unifiedUnaryOp("-", "System.IntPtr?", "nint nint.op_UnaryNegation(nint value)", "3", "-3",
@"{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (nint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly nint nint?.GetValueOrDefault()""
  IL_001c:  neg
  IL_001d:  newobj     ""nint?..ctor(nint)""
  IL_0022:  ret
}");
            // Reporting ERR_AmbigUnaryOp for `-(nuint?)value` is inconsistent with the ERR_BadUnaryOp reported
            // for `-(nuint)value`, but that difference in behavior is consistent with the pair of errors reported for
            // `-(ulong?)value` and `-(ulong)value`. See the "Special case" in Binder.UnaryOperatorOverloadResolution()
            // which handles ulong but not ulong?.
            unifiedUnaryOp("-", "System.UIntPtr?", null, null, null, null, Diagnostic(ErrorCode.ERR_AmbigUnaryOp, "-operand").WithArguments("-", "nuint?"));

            // lifted unary operator!
            unifiedUnaryOp("!", "System.IntPtr?");
            unifiedUnaryOp("!", "System.UIntPtr?");

            // lifted unary operator~
            unifiedUnaryOp("~", "System.IntPtr?", "nint nint.op_OnesComplement(nint value)", "3", "-4",
@"{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (nint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly nint nint?.GetValueOrDefault()""
  IL_001c:  not
  IL_001d:  newobj     ""nint?..ctor(nint)""
  IL_0022:  ret
}");
            unifiedUnaryOp("~", "System.UIntPtr?", "nuint nuint.op_OnesComplement(nuint value)", "3", getComplement(3),
@"{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (nuint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly nuint nuint?.GetValueOrDefault()""
  IL_001c:  not
  IL_001d:  newobj     ""nuint?..ctor(nuint)""
  IL_0022:  ret
}");

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
                var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
                comp.VerifyDiagnostics(expectedDiagnostics);

                var tree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(tree);
                var expr = tree.GetRoot().DescendantNodes().OfType<PrefixUnaryExpressionSyntax>().Single();
                var symbolInfo = model.GetSymbolInfo(expr);
                Assert.Equal(expectedSymbol, symbolInfo.Symbol?.ToDisplayString(SymbolDisplayFormat.TestFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes)));

                if (expectedDiagnostics.Length == 0)
                {
                    var verifier = CompileAndVerify(comp, verify: Verification.FailsPEVerify, expectedOutput: IncludeExpectedOutput(expectedResult));
                    verifier.VerifyIL("Program.Evaluate", expectedIL);
                }
            }
        }

        [Fact]
        public void IncrementOperators()
        {
            void unifiedIncrementOps(string op, string opType, string expectedSymbol = null, bool useChecked = false, string values = null, string expectedResult = null, string expectedIL = "", string expectedLiftedIL = "", DiagnosticDescription diagnostic = null)
            {
                Assert.True(opType is "System.IntPtr" or "System.UIntPtr" or "System.IntPtr?" or "System.UIntPtr?");
                incrementOps(op, opType, expectedSymbol, useChecked, values, expectedResult, expectedIL, expectedLiftedIL, diagnostic);
                incrementOps(op, AsNative(opType), expectedSymbol, useChecked, values, expectedResult, expectedIL, expectedLiftedIL, diagnostic);
            }

            void incrementOps(string op, string opType, string expectedSymbol, bool useChecked, string values, string expectedResult, string expectedIL, string expectedLiftedIL, DiagnosticDescription diagnostic)
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

            unifiedIncrementOps("++", "System.IntPtr", "nint nint.op_Increment(nint value)", useChecked: false,
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
  IL_0004:  call       ""readonly bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0016
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  br.s       IL_0024
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""readonly nint nint?.GetValueOrDefault()""
  IL_001d:  ldc.i4.1
  IL_001e:  add
  IL_001f:  newobj     ""nint?..ctor(nint)""
  IL_0024:  starg.s    V_0
  IL_0026:  ldarg.0
  IL_0027:  ret
}");
            unifiedIncrementOps("++", "System.UIntPtr", "nuint nuint.op_Increment(nuint value)", useChecked: false,
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
  IL_0004:  call       ""readonly bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0016
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  br.s       IL_0024
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""readonly nuint nuint?.GetValueOrDefault()""
  IL_001d:  ldc.i4.1
  IL_001e:  add
  IL_001f:  newobj     ""nuint?..ctor(nuint)""
  IL_0024:  starg.s    V_0
  IL_0026:  ldarg.0
  IL_0027:  ret
}");
            unifiedIncrementOps("--", "System.IntPtr", "nint nint.op_Decrement(nint value)", useChecked: false,
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
  IL_0004:  call       ""readonly bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0016
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  br.s       IL_0024
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""readonly nint nint?.GetValueOrDefault()""
  IL_001d:  ldc.i4.1
  IL_001e:  sub
  IL_001f:  newobj     ""nint?..ctor(nint)""
  IL_0024:  starg.s    V_0
  IL_0026:  ldarg.0
  IL_0027:  ret
}");
            unifiedIncrementOps("--", "System.UIntPtr", "nuint nuint.op_Decrement(nuint value)", useChecked: false,
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
  IL_0004:  call       ""readonly bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0016
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  br.s       IL_0024
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""readonly nuint nuint?.GetValueOrDefault()""
  IL_001d:  ldc.i4.1
  IL_001e:  sub
  IL_001f:  newobj     ""nuint?..ctor(nuint)""
  IL_0024:  starg.s    V_0
  IL_0026:  ldarg.0
  IL_0027:  ret
}");

            unifiedIncrementOps("++", "System.IntPtr", "nint nint.op_CheckedIncrement(nint value)", useChecked: true,
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
  IL_0004:  call       ""readonly bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0016
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  br.s       IL_0024
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""readonly nint nint?.GetValueOrDefault()""
  IL_001d:  ldc.i4.1
  IL_001e:  add.ovf
  IL_001f:  newobj     ""nint?..ctor(nint)""
  IL_0024:  starg.s    V_0
  IL_0026:  ldarg.0
  IL_0027:  ret
}");
            unifiedIncrementOps("++", "System.UIntPtr", "nuint nuint.op_CheckedIncrement(nuint value)", useChecked: true,
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
  IL_0004:  call       ""readonly bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0016
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  br.s       IL_0024
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""readonly nuint nuint?.GetValueOrDefault()""
  IL_001d:  ldc.i4.1
  IL_001e:  add.ovf.un
  IL_001f:  newobj     ""nuint?..ctor(nuint)""
  IL_0024:  starg.s    V_0
  IL_0026:  ldarg.0
  IL_0027:  ret
}");
            unifiedIncrementOps("--", "System.IntPtr", "nint nint.op_CheckedDecrement(nint value)", useChecked: true,
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
  IL_0004:  call       ""readonly bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0016
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  br.s       IL_0024
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""readonly nint nint?.GetValueOrDefault()""
  IL_001d:  ldc.i4.1
  IL_001e:  sub.ovf
  IL_001f:  newobj     ""nint?..ctor(nint)""
  IL_0024:  starg.s    V_0
  IL_0026:  ldarg.0
  IL_0027:  ret
}");
            unifiedIncrementOps("--", "System.UIntPtr", "nuint nuint.op_CheckedDecrement(nuint value)", useChecked: true,
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
  IL_0004:  call       ""readonly bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0016
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  br.s       IL_0024
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""readonly nuint nuint?.GetValueOrDefault()""
  IL_001d:  ldc.i4.1
  IL_001e:  sub.ovf.un
  IL_001f:  newobj     ""nuint?..ctor(nuint)""
  IL_0024:  starg.s    V_0
  IL_0026:  ldarg.0
  IL_0027:  ret
}");

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
                var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
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
                    var verifier = CompileAndVerify(comp, verify: Verification.FailsPEVerify, expectedOutput: IncludeExpectedOutput(expectedResult));
                    verifier.VerifyIL("Program.Evaluate", expectedIL);
                }
            }
        }

        [Fact]
        public void IncrementOperators_RefOperand()
        {
            void unifiedIncrementOps(string op, string opType, string expectedSymbol = null, string values = null, string expectedResult = null, string expectedIL = "", string expectedLiftedIL = "", DiagnosticDescription diagnostic = null)
            {
                Assert.True(opType is "System.IntPtr" or "System.UIntPtr" or "System.IntPtr?" or "System.UIntPtr?");
                incrementOps(op, opType, expectedSymbol, values, expectedResult, expectedIL, expectedLiftedIL, diagnostic);
                incrementOps(op, AsNative(opType), expectedSymbol, values, expectedResult, expectedIL, expectedLiftedIL, diagnostic);
            }

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

            unifiedIncrementOps("++", "System.IntPtr", "nint nint.op_Increment(nint value)",
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
  IL_000a:  call       ""readonly bool nint?.HasValue.get""
  IL_000f:  brtrue.s   IL_001c
  IL_0011:  ldloca.s   V_1
  IL_0013:  initobj    ""nint?""
  IL_0019:  ldloc.1
  IL_001a:  br.s       IL_002a
  IL_001c:  ldloca.s   V_0
  IL_001e:  call       ""readonly nint nint?.GetValueOrDefault()""
  IL_0023:  ldc.i4.1
  IL_0024:  add
  IL_0025:  newobj     ""nint?..ctor(nint)""
  IL_002a:  stobj      ""nint?""
  IL_002f:  ret
}");
            unifiedIncrementOps("++", "System.UIntPtr", "nuint nuint.op_Increment(nuint value)",
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
  IL_000a:  call       ""readonly bool nuint?.HasValue.get""
  IL_000f:  brtrue.s   IL_001c
  IL_0011:  ldloca.s   V_1
  IL_0013:  initobj    ""nuint?""
  IL_0019:  ldloc.1
  IL_001a:  br.s       IL_002a
  IL_001c:  ldloca.s   V_0
  IL_001e:  call       ""readonly nuint nuint?.GetValueOrDefault()""
  IL_0023:  ldc.i4.1
  IL_0024:  add
  IL_0025:  newobj     ""nuint?..ctor(nuint)""
  IL_002a:  stobj      ""nuint?""
  IL_002f:  ret
}");
            unifiedIncrementOps("--", "System.IntPtr", "nint nint.op_Decrement(nint value)",
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
  IL_000a:  call       ""readonly bool nint?.HasValue.get""
  IL_000f:  brtrue.s   IL_001c
  IL_0011:  ldloca.s   V_1
  IL_0013:  initobj    ""nint?""
  IL_0019:  ldloc.1
  IL_001a:  br.s       IL_002a
  IL_001c:  ldloca.s   V_0
  IL_001e:  call       ""readonly nint nint?.GetValueOrDefault()""
  IL_0023:  ldc.i4.1
  IL_0024:  sub
  IL_0025:  newobj     ""nint?..ctor(nint)""
  IL_002a:  stobj      ""nint?""
  IL_002f:  ret
}");
            unifiedIncrementOps("--", "System.UIntPtr", "nuint nuint.op_Decrement(nuint value)",
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
  IL_000a:  call       ""readonly bool nuint?.HasValue.get""
  IL_000f:  brtrue.s   IL_001c
  IL_0011:  ldloca.s   V_1
  IL_0013:  initobj    ""nuint?""
  IL_0019:  ldloc.1
  IL_001a:  br.s       IL_002a
  IL_001c:  ldloca.s   V_0
  IL_001e:  call       ""readonly nuint nuint?.GetValueOrDefault()""
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
                var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
                comp.VerifyDiagnostics(expectedDiagnostics);

                var tree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(tree);
                var kind = (op == "++") ? SyntaxKind.PreIncrementExpression : SyntaxKind.PreDecrementExpression;
                var expr = tree.GetRoot().DescendantNodes().Single(n => n.Kind() == kind);
                var symbolInfo = model.GetSymbolInfo(expr);
                Assert.Equal(expectedSymbol, symbolInfo.Symbol?.ToDisplayString(SymbolDisplayFormat.TestFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes)));

                if (expectedDiagnostics.Length == 0)
                {
                    var verifier = CompileAndVerify(comp, verify: Verification.FailsPEVerify, expectedOutput: IncludeExpectedOutput(expectedResult));
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
            var comp = CreateEmptyCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.Regular9);
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

        [Fact]
        public void UnaryAndBinaryOperators_UserDefinedConversions()
        {
            verifyBoth("System.IntPtr",
               // (5,9): error CS0266: Cannot implicitly convert type 'long' to 'MyInt'. An explicit conversion exists (are you missing a cast?)
               //         ++x;
               Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "++x").WithArguments("long", "MyInt").WithLocation(5, 9),
               // (6,9): error CS0266: Cannot implicitly convert type 'long' to 'MyInt'. An explicit conversion exists (are you missing a cast?)
               //         x++;
               Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x++").WithArguments("long", "MyInt").WithLocation(6, 9),
               // (7,9): error CS0266: Cannot implicitly convert type 'long' to 'MyInt'. An explicit conversion exists (are you missing a cast?)
               //         --x;
               Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "--x").WithArguments("long", "MyInt").WithLocation(7, 9),
               // (8,9): error CS0266: Cannot implicitly convert type 'long' to 'MyInt'. An explicit conversion exists (are you missing a cast?)
               //         x--;
               Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x--").WithArguments("long", "MyInt").WithLocation(8, 9)
               );

            verifyBoth("System.IntPtr?",
                // (5,9): error CS0266: Cannot implicitly convert type 'long?' to 'MyInt'. An explicit conversion exists (are you missing a cast?)
                //         ++x;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "++x").WithArguments("long?", "MyInt").WithLocation(5, 9),
                // (6,9): error CS0266: Cannot implicitly convert type 'long?' to 'MyInt'. An explicit conversion exists (are you missing a cast?)
                //         x++;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x++").WithArguments("long?", "MyInt").WithLocation(6, 9),
                // (7,9): error CS0266: Cannot implicitly convert type 'long?' to 'MyInt'. An explicit conversion exists (are you missing a cast?)
                //         --x;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "--x").WithArguments("long?", "MyInt").WithLocation(7, 9),
                // (8,9): error CS0266: Cannot implicitly convert type 'long?' to 'MyInt'. An explicit conversion exists (are you missing a cast?)
                //         x--;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x--").WithArguments("long?", "MyInt").WithLocation(8, 9)
                );

            verifyBoth("System.UIntPtr",
                // (5,9): error CS0266: Cannot implicitly convert type 'ulong' to 'MyInt'. An explicit conversion exists (are you missing a cast?)
                //         ++x;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "++x").WithArguments("ulong", "MyInt").WithLocation(5, 9),
                // (6,9): error CS0266: Cannot implicitly convert type 'ulong' to 'MyInt'. An explicit conversion exists (are you missing a cast?)
                //         x++;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x++").WithArguments("ulong", "MyInt").WithLocation(6, 9),
                // (7,9): error CS0266: Cannot implicitly convert type 'ulong' to 'MyInt'. An explicit conversion exists (are you missing a cast?)
                //         --x;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "--x").WithArguments("ulong", "MyInt").WithLocation(7, 9),
                // (8,9): error CS0266: Cannot implicitly convert type 'ulong' to 'MyInt'. An explicit conversion exists (are you missing a cast?)
                //         x--;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x--").WithArguments("ulong", "MyInt").WithLocation(8, 9),
                // (10,13): error CS0035: Operator '-' is ambiguous on an operand of type 'MyInt'
                //         _ = -x;
                Diagnostic(ErrorCode.ERR_AmbigUnaryOp, "-x").WithArguments("-", "MyInt").WithLocation(10, 13)
                );

            verifyBoth("System.UIntPtr?",
                // (5,9): error CS0266: Cannot implicitly convert type 'ulong?' to 'MyInt'. An explicit conversion exists (are you missing a cast?)
                //         ++x;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "++x").WithArguments("ulong?", "MyInt").WithLocation(5, 9),
                // (6,9): error CS0266: Cannot implicitly convert type 'ulong?' to 'MyInt'. An explicit conversion exists (are you missing a cast?)
                //         x++;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x++").WithArguments("ulong?", "MyInt").WithLocation(6, 9),
                // (7,9): error CS0266: Cannot implicitly convert type 'ulong?' to 'MyInt'. An explicit conversion exists (are you missing a cast?)
                //         --x;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "--x").WithArguments("ulong?", "MyInt").WithLocation(7, 9),
                // (8,9): error CS0266: Cannot implicitly convert type 'ulong?' to 'MyInt'. An explicit conversion exists (are you missing a cast?)
                //         x--;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x--").WithArguments("ulong?", "MyInt").WithLocation(8, 9),
                // (10,13): error CS0035: Operator '-' is ambiguous on an operand of type 'MyInt'
                //         _ = -x;
                Diagnostic(ErrorCode.ERR_AmbigUnaryOp, "-x").WithArguments("-", "MyInt").WithLocation(10, 13)
                );

            void verifyBoth(string type, params DiagnosticDescription[] expected)
            {
                verify(type, expected);
                verify(AsNative(type), expected);
            }

            void verify(string type, params DiagnosticDescription[] expected)
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
                var comp = CreateCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
                comp.VerifyDiagnostics(expected);
            }
        }

        [Fact]
        public void BinaryOperators()
        {
            void unifiedBinaryOps(string op, string leftType, string rightType, string expectedSymbol1 = null, string expectedSymbol2 = "", DiagnosticDescription[] diagnostics1 = null, DiagnosticDescription[] diagnostics2 = null)
            {
                binaryOps(op, leftType, rightType, expectedSymbol1, expectedSymbol2, diagnostics1, diagnostics2);

                var fullLeftType = leftType switch
                {
                    "nint" => "System.IntPtr",
                    "nint?" => "System.IntPtr?",
                    "nuint" => "System.UIntPtr",
                    "nuint?" => "System.UIntPtr?",
                    _ => throw ExceptionUtilities.Unreachable()
                };
                binaryOps(op, fullLeftType, rightType, expectedSymbol1, expectedSymbol2, diagnostics1, diagnostics2);
            }

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
                if (includeBadBinaryOps) builder.Add(Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {op} y").WithArguments(op, AsNative(leftType), AsNative(rightType)));
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

                // nint arithmeticOp type
                unifiedBinaryOps(symbol, "nint", "object");
                unifiedBinaryOps(symbol, "nint", "string");
                unifiedBinaryOps(symbol, "nint", "void*", null, (symbol == "-") ? $"void* void*.{name}(void* left, long right)" : null, getBadBinaryOpsDiagnostics(symbol, "nint", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "nint", includeBadBinaryOps: includeBadBinaryOps, includeVoidError: true));
                unifiedBinaryOps(symbol, "nint", "bool");
                unifiedBinaryOps(symbol, "nint", "char", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "sbyte", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "byte", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "short", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "ushort", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "int", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "uint", $"long long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint", "nint", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "nuint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint"));
                unifiedBinaryOps(symbol, "nint", "long", $"long long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint", "ulong", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "ulong"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong", "nint"));
                unifiedBinaryOps(symbol, "nint", "float", $"float float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nint", "double", $"double double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nint", "decimal", $"decimal decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nint", "System.IntPtr", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "System.UIntPtr", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint"));

                // nint arithmeticOp type?
                unifiedBinaryOps(symbol, "nint", "bool?");
                unifiedBinaryOps(symbol, "nint", "char?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "sbyte?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "byte?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "short?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "ushort?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "int?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "uint?", $"long long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint", "nint?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "nuint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint"));
                unifiedBinaryOps(symbol, "nint", "long?", $"long long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint", "ulong?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "ulong?"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong?", "nint"));
                unifiedBinaryOps(symbol, "nint", "float?", $"float float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nint", "double?", $"double double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nint", "decimal?", $"decimal decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nint", "System.IntPtr?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "System.UIntPtr?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint"));
                unifiedBinaryOps(symbol, "nint", "object");

                // nint? arithmeticOp type
                unifiedBinaryOps(symbol, "nint?", "string");
                unifiedBinaryOps(symbol, "nint?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nint?", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "nint?", includeVoidError: true));
                unifiedBinaryOps(symbol, "nint?", "bool");
                unifiedBinaryOps(symbol, "nint?", "char", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "sbyte", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "byte", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "short", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "ushort", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "int", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "uint", $"long long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint?", "nint", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "nuint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint?"));
                unifiedBinaryOps(symbol, "nint?", "long", $"long long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint?", "ulong", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "ulong"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong", "nint?"));
                unifiedBinaryOps(symbol, "nint?", "float", $"float float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nint?", "double", $"double double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nint?", "decimal", $"decimal decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nint?", "System.IntPtr", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "System.UIntPtr", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint?"));

                // nint? arithmeticOp type?
                unifiedBinaryOps(symbol, "nint?", "bool?");
                unifiedBinaryOps(symbol, "nint?", "char?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "sbyte?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "byte?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "short?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "ushort?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "int?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "uint?", $"long long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint?", "nint?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "nuint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint?"));
                unifiedBinaryOps(symbol, "nint?", "long?", $"long long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint?", "ulong?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "ulong?"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong?", "nint?"));
                unifiedBinaryOps(symbol, "nint?", "float?", $"float float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nint?", "double?", $"double double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nint?", "decimal?", $"decimal decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nint?", "System.IntPtr?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "System.UIntPtr?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint?"));

                // nuint arithmeticOp type
                unifiedBinaryOps(symbol, "nuint", "object");
                unifiedBinaryOps(symbol, "nuint", "string");
                unifiedBinaryOps(symbol, "nuint", "void*", null, (symbol == "-") ? $"void* void*.{name}(void* left, ulong right)" : null, getBadBinaryOpsDiagnostics(symbol, "nuint", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "nuint", includeBadBinaryOps: includeBadBinaryOps, includeVoidError: true));
                unifiedBinaryOps(symbol, "nuint", "bool");
                unifiedBinaryOps(symbol, "nuint", "char", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "sbyte", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "sbyte"), getAmbiguousBinaryOpsDiagnostics(symbol, "sbyte", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "byte", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "short", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "short"), getAmbiguousBinaryOpsDiagnostics(symbol, "short", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "ushort", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "int", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "int"), getAmbiguousBinaryOpsDiagnostics(symbol, "int", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "uint", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "nint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "nuint", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "long", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "long"), getAmbiguousBinaryOpsDiagnostics(symbol, "long", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "ulong", $"ulong ulong.{name}(ulong left, ulong right)");
                unifiedBinaryOps(symbol, "nuint", "float", $"float float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nuint", "double", $"double double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nuint", "decimal", $"decimal decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nuint", "System.IntPtr", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "System.UIntPtr", $"nuint nuint.{name}(nuint left, nuint right)");

                // nuint arithmeticOp type?
                unifiedBinaryOps(symbol, "nuint", "bool?");
                unifiedBinaryOps(symbol, "nuint", "char?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "sbyte?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "sbyte?"), getAmbiguousBinaryOpsDiagnostics(symbol, "sbyte?", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "byte?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "short?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "short?"), getAmbiguousBinaryOpsDiagnostics(symbol, "short?", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "ushort?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "int?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "int?"), getAmbiguousBinaryOpsDiagnostics(symbol, "int?", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "uint?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "nint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "nuint?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "long?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "long?"), getAmbiguousBinaryOpsDiagnostics(symbol, "long?", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "ulong?", $"ulong ulong.{name}(ulong left, ulong right)");
                unifiedBinaryOps(symbol, "nuint", "float?", $"float float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nuint", "double?", $"double double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nuint", "decimal?", $"decimal decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nuint", "System.IntPtr?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "System.UIntPtr?", $"nuint nuint.{name}(nuint left, nuint right)");

                // nuint? arithmeticOp type
                unifiedBinaryOps(symbol, "nuint?", "object");
                unifiedBinaryOps(symbol, "nuint?", "string");
                unifiedBinaryOps(symbol, "nuint?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nuint?", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "nuint?", includeVoidError: true));
                unifiedBinaryOps(symbol, "nuint?", "bool");
                unifiedBinaryOps(symbol, "nuint?", "char", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "sbyte", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "sbyte"), getAmbiguousBinaryOpsDiagnostics(symbol, "sbyte", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "byte", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "short", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "short"), getAmbiguousBinaryOpsDiagnostics(symbol, "short", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "ushort", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "int", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "int"), getAmbiguousBinaryOpsDiagnostics(symbol, "int", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "uint", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "nint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "nuint", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "long", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "long"), getAmbiguousBinaryOpsDiagnostics(symbol, "long", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "ulong", $"ulong ulong.{name}(ulong left, ulong right)");
                unifiedBinaryOps(symbol, "nuint?", "float", $"float float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nuint?", "double", $"double double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nuint?", "decimal", $"decimal decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nuint?", "System.IntPtr", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "System.UIntPtr", $"nuint nuint.{name}(nuint left, nuint right)");

                // nuint? arithmeticOp type?
                unifiedBinaryOps(symbol, "nuint?", "bool?");
                unifiedBinaryOps(symbol, "nuint?", "char?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "sbyte?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "sbyte?"), getAmbiguousBinaryOpsDiagnostics(symbol, "sbyte?", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "byte?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "short?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "short?"), getAmbiguousBinaryOpsDiagnostics(symbol, "short?", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "ushort?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "int?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "int?"), getAmbiguousBinaryOpsDiagnostics(symbol, "int?", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "uint?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "nint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "nuint?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "long?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "long?"), getAmbiguousBinaryOpsDiagnostics(symbol, "long?", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "ulong?", $"ulong ulong.{name}(ulong left, ulong right)");
                unifiedBinaryOps(symbol, "nuint?", "float?", $"float float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nuint?", "double?", $"double double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nuint?", "decimal?", $"decimal decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nuint?", "System.IntPtr?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "System.UIntPtr?", $"nuint nuint.{name}(nuint left, nuint right)");
            }

            foreach ((string symbol, string name) in comparisonOperators)
            {
                // nint comparisonOp type
                unifiedBinaryOps(symbol, "nint", "object");
                unifiedBinaryOps(symbol, "nint", "string");
                unifiedBinaryOps(symbol, "nint", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nint", "void*"), getBadBinaryOpsDiagnostics(symbol, "void*", "nint"));
                unifiedBinaryOps(symbol, "nint", "bool");
                unifiedBinaryOps(symbol, "nint", "char", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "sbyte", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "byte", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "short", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "ushort", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "int", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "uint", $"bool long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint", "nint", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "nuint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint"));
                unifiedBinaryOps(symbol, "nint", "long", $"bool long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint", "ulong", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "ulong"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong", "nint"));
                unifiedBinaryOps(symbol, "nint", "float", $"bool float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nint", "double", $"bool double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nint", "decimal", $"bool decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nint", "System.IntPtr", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "System.UIntPtr", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint"));

                // nint comparisonOp type?
                unifiedBinaryOps(symbol, "nint", "bool?");
                unifiedBinaryOps(symbol, "nint", "char?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "sbyte?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "byte?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "short?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "ushort?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "int?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "uint?", $"bool long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint", "nint?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "nuint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint"));
                unifiedBinaryOps(symbol, "nint", "long?", $"bool long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint", "ulong?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "ulong?"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong?", "nint"));
                unifiedBinaryOps(symbol, "nint", "float?", $"bool float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nint", "double?", $"bool double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nint", "decimal?", $"bool decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nint", "System.IntPtr?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "System.UIntPtr?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint"));
                unifiedBinaryOps(symbol, "nint", "object");

                // nint? comparisonOp type
                unifiedBinaryOps(symbol, "nint?", "string");
                unifiedBinaryOps(symbol, "nint?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nint?", "void*"), getBadBinaryOpsDiagnostics(symbol, "void*", "nint?"));
                unifiedBinaryOps(symbol, "nint?", "bool");
                unifiedBinaryOps(symbol, "nint?", "char", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "sbyte", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "byte", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "short", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "ushort", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "int", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "uint", $"bool long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint?", "nint", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "nuint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint?"));
                unifiedBinaryOps(symbol, "nint?", "long", $"bool long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint?", "ulong", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "ulong"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong", "nint?"));
                unifiedBinaryOps(symbol, "nint?", "float", $"bool float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nint?", "double", $"bool double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nint?", "decimal", $"bool decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nint?", "System.IntPtr", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "System.UIntPtr", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint?"));

                // nint? comparisonOp type?
                unifiedBinaryOps(symbol, "nint?", "bool?");
                unifiedBinaryOps(symbol, "nint?", "char?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "sbyte?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "byte?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "short?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "ushort?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "int?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "uint?", $"bool long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint?", "nint?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "nuint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint?"));
                unifiedBinaryOps(symbol, "nint?", "long?", $"bool long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint?", "ulong?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "ulong?"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong?", "nint?"));
                unifiedBinaryOps(symbol, "nint?", "float?", $"bool float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nint?", "double?", $"bool double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nint?", "decimal?", $"bool decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nint?", "System.IntPtr?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "System.UIntPtr?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint?"));

                // nuint comparisonOp type
                unifiedBinaryOps(symbol, "nuint", "object");
                unifiedBinaryOps(symbol, "nuint", "string");
                unifiedBinaryOps(symbol, "nuint", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nuint", "void*"), getBadBinaryOpsDiagnostics(symbol, "void*", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "bool");
                unifiedBinaryOps(symbol, "nuint", "char", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "sbyte", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "sbyte"), getAmbiguousBinaryOpsDiagnostics(symbol, "sbyte", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "byte", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "short", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "short"), getAmbiguousBinaryOpsDiagnostics(symbol, "short", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "ushort", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "int", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "int"), getAmbiguousBinaryOpsDiagnostics(symbol, "int", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "uint", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "nint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "nuint", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "long", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "long"), getAmbiguousBinaryOpsDiagnostics(symbol, "long", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "ulong", $"bool ulong.{name}(ulong left, ulong right)");
                unifiedBinaryOps(symbol, "nuint", "float", $"bool float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nuint", "double", $"bool double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nuint", "decimal", $"bool decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nuint", "System.IntPtr", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "System.UIntPtr", $"bool nuint.{name}(nuint left, nuint right)");

                // nuint comparisonOp type?
                unifiedBinaryOps(symbol, "nuint", "bool?");
                unifiedBinaryOps(symbol, "nuint", "char?", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "sbyte?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "sbyte?"), getAmbiguousBinaryOpsDiagnostics(symbol, "sbyte?", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "byte?", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "short?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "short?"), getAmbiguousBinaryOpsDiagnostics(symbol, "short?", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "ushort?", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "int?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "int?"), getAmbiguousBinaryOpsDiagnostics(symbol, "int?", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "uint?", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "nint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "nuint?", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "long?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "long?"), getAmbiguousBinaryOpsDiagnostics(symbol, "long?", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "ulong?", $"bool ulong.{name}(ulong left, ulong right)");
                unifiedBinaryOps(symbol, "nuint", "float?", $"bool float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nuint", "double?", $"bool double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nuint", "decimal?", $"bool decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nuint", "System.IntPtr?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "System.UIntPtr?", $"bool nuint.{name}(nuint left, nuint right)");

                // nuint? comparisonOp type
                unifiedBinaryOps(symbol, "nuint?", "object");
                unifiedBinaryOps(symbol, "nuint?", "string");
                unifiedBinaryOps(symbol, "nuint?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nuint?", "void*"), getBadBinaryOpsDiagnostics(symbol, "void*", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "bool");
                unifiedBinaryOps(symbol, "nuint?", "char", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "sbyte", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "sbyte"), getAmbiguousBinaryOpsDiagnostics(symbol, "sbyte", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "byte", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "short", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "short"), getAmbiguousBinaryOpsDiagnostics(symbol, "short", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "ushort", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "int", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "int"), getAmbiguousBinaryOpsDiagnostics(symbol, "int", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "uint", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "nint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "nuint", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "long", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "long"), getAmbiguousBinaryOpsDiagnostics(symbol, "long", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "ulong", $"bool ulong.{name}(ulong left, ulong right)");
                unifiedBinaryOps(symbol, "nuint?", "float", $"bool float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nuint?", "double", $"bool double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nuint?", "decimal", $"bool decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nuint?", "System.IntPtr", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "System.UIntPtr", $"bool nuint.{name}(nuint left, nuint right)");

                // nuint comparisonOp type?
                unifiedBinaryOps(symbol, "nuint?", "bool?");
                unifiedBinaryOps(symbol, "nuint?", "char?", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "sbyte?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "sbyte?"), getAmbiguousBinaryOpsDiagnostics(symbol, "sbyte?", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "byte?", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "short?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "short?"), getAmbiguousBinaryOpsDiagnostics(symbol, "short?", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "ushort?", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "int?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "int?"), getAmbiguousBinaryOpsDiagnostics(symbol, "int?", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "uint?", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "nint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "nuint?", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "long?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "long?"), getAmbiguousBinaryOpsDiagnostics(symbol, "long?", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "ulong?", $"bool ulong.{name}(ulong left, ulong right)");
                unifiedBinaryOps(symbol, "nuint?", "float?", $"bool float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nuint?", "double?", $"bool double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nuint?", "decimal?", $"bool decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nuint?", "System.IntPtr?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "System.UIntPtr?", $"bool nuint.{name}(nuint left, nuint right)");
            }

            foreach ((string symbol, string name) in additionOperators)
            {
                // nint additionOperator type
                unifiedBinaryOps(symbol, "nint", "object");
                unifiedBinaryOps(symbol, "nint", "string", $"string string.{name}(object left, string right)", $"string string.{name}(string left, object right)");
                unifiedBinaryOps(symbol, "nint", "void*", $"void* void*.{name}(long left, void* right)", $"void* void*.{name}(void* left, long right)", new[] { Diagnostic(ErrorCode.ERR_VoidError, "x + y") });
                unifiedBinaryOps(symbol, "nint", "bool");
                unifiedBinaryOps(symbol, "nint", "char", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "sbyte", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "byte", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "short", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "ushort", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "int", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "uint", $"long long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint", "nint", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "nuint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint"));
                unifiedBinaryOps(symbol, "nint", "long", $"long long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint", "ulong", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "ulong"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong", "nint"));
                unifiedBinaryOps(symbol, "nint", "float", $"float float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nint", "double", $"double double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nint", "decimal", $"decimal decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nint", "System.IntPtr", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "System.UIntPtr", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint"));

                // nint additionOperator type?
                unifiedBinaryOps(symbol, "nint", "bool?");
                unifiedBinaryOps(symbol, "nint", "char?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "sbyte?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "byte?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "short?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "ushort?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "int?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "uint?", $"long long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint", "nint?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "nuint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint"));
                unifiedBinaryOps(symbol, "nint", "long?", $"long long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint", "ulong?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "ulong?"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong?", "nint"));
                unifiedBinaryOps(symbol, "nint", "float?", $"float float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nint", "double?", $"double double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nint", "decimal?", $"decimal decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nint", "System.IntPtr?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "System.UIntPtr?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint"));
                unifiedBinaryOps(symbol, "nint", "object");

                // nint? additionOperator type
                unifiedBinaryOps(symbol, "nint?", "string", $"string string.{name}(object left, string right)", $"string string.{name}(string left, object right)");
                unifiedBinaryOps(symbol, "nint?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nint?", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "nint?", includeVoidError: true));
                unifiedBinaryOps(symbol, "nint?", "bool");
                unifiedBinaryOps(symbol, "nint?", "char", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "sbyte", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "byte", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "short", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "ushort", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "int", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "uint", $"long long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint?", "nint", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "nuint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint?"));
                unifiedBinaryOps(symbol, "nint?", "long", $"long long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint?", "ulong", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "ulong"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong", "nint?"));
                unifiedBinaryOps(symbol, "nint?", "float", $"float float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nint?", "double", $"double double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nint?", "decimal", $"decimal decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nint?", "System.IntPtr", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "System.UIntPtr", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint?"));

                // nint? additionOperator type?
                unifiedBinaryOps(symbol, "nint?", "bool?");
                unifiedBinaryOps(symbol, "nint?", "char?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "sbyte?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "byte?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "short?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "ushort?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "int?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "uint?", $"long long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint?", "nint?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "nuint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint?"));
                unifiedBinaryOps(symbol, "nint?", "long?", $"long long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint?", "ulong?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "ulong?"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong?", "nint?"));
                unifiedBinaryOps(symbol, "nint?", "float?", $"float float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nint?", "double?", $"double double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nint?", "decimal?", $"decimal decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nint?", "System.IntPtr?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "System.UIntPtr?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint?"));

                // nuint additionOperator type
                unifiedBinaryOps(symbol, "nuint", "object");
                unifiedBinaryOps(symbol, "nuint", "string", $"string string.{name}(object left, string right)", $"string string.{name}(string left, object right)");
                unifiedBinaryOps(symbol, "nuint", "void*", $"void* void*.{name}(ulong left, void* right)", $"void* void*.{name}(void* left, ulong right)", new[] { Diagnostic(ErrorCode.ERR_VoidError, "x + y") });
                unifiedBinaryOps(symbol, "nuint", "bool");
                unifiedBinaryOps(symbol, "nuint", "char", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "sbyte", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "sbyte"), getAmbiguousBinaryOpsDiagnostics(symbol, "sbyte", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "byte", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "short", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "short"), getAmbiguousBinaryOpsDiagnostics(symbol, "short", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "ushort", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "int", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "int"), getAmbiguousBinaryOpsDiagnostics(symbol, "int", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "uint", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "nint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "nuint", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "long", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "long"), getAmbiguousBinaryOpsDiagnostics(symbol, "long", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "ulong", $"ulong ulong.{name}(ulong left, ulong right)");
                unifiedBinaryOps(symbol, "nuint", "float", $"float float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nuint", "double", $"double double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nuint", "decimal", $"decimal decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nuint", "System.IntPtr", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "System.UIntPtr", $"nuint nuint.{name}(nuint left, nuint right)");

                // nuint additionOperator type?
                unifiedBinaryOps(symbol, "nuint", "bool?");
                unifiedBinaryOps(symbol, "nuint", "char?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "sbyte?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "sbyte?"), getAmbiguousBinaryOpsDiagnostics(symbol, "sbyte?", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "byte?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "short?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "short?"), getAmbiguousBinaryOpsDiagnostics(symbol, "short?", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "ushort?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "int?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "int?"), getAmbiguousBinaryOpsDiagnostics(symbol, "int?", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "uint?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "nint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "nuint?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "long?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "long?"), getAmbiguousBinaryOpsDiagnostics(symbol, "long?", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "ulong?", $"ulong ulong.{name}(ulong left, ulong right)");
                unifiedBinaryOps(symbol, "nuint", "float?", $"float float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nuint", "double?", $"double double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nuint", "decimal?", $"decimal decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nuint", "System.IntPtr?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "System.UIntPtr?", $"nuint nuint.{name}(nuint left, nuint right)");

                // nuint? additionOperator type
                unifiedBinaryOps(symbol, "nuint?", "object");
                unifiedBinaryOps(symbol, "nuint?", "string", $"string string.{name}(object left, string right)", $"string string.{name}(string left, object right)");
                unifiedBinaryOps(symbol, "nuint?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nuint?", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "nuint?", includeVoidError: true));
                unifiedBinaryOps(symbol, "nuint?", "bool");
                unifiedBinaryOps(symbol, "nuint?", "char", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "sbyte", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "sbyte"), getAmbiguousBinaryOpsDiagnostics(symbol, "sbyte", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "byte", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "short", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "short"), getAmbiguousBinaryOpsDiagnostics(symbol, "short", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "ushort", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "int", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "int"), getAmbiguousBinaryOpsDiagnostics(symbol, "int", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "uint", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "nint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "nuint", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "long", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "long"), getAmbiguousBinaryOpsDiagnostics(symbol, "long", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "ulong", $"ulong ulong.{name}(ulong left, ulong right)");
                unifiedBinaryOps(symbol, "nuint?", "float", $"float float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nuint?", "double", $"double double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nuint?", "decimal", $"decimal decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nuint?", "System.IntPtr", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "System.UIntPtr", $"nuint nuint.{name}(nuint left, nuint right)");

                // nuint? additionOperator type?
                unifiedBinaryOps(symbol, "nuint?", "bool?");
                unifiedBinaryOps(symbol, "nuint?", "char?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "sbyte?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "sbyte?"), getAmbiguousBinaryOpsDiagnostics(symbol, "sbyte?", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "byte?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "short?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "short?"), getAmbiguousBinaryOpsDiagnostics(symbol, "short?", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "ushort?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "int?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "int?"), getAmbiguousBinaryOpsDiagnostics(symbol, "int?", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "uint?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "nint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "nuint?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "long?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "long?"), getAmbiguousBinaryOpsDiagnostics(symbol, "long?", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "ulong?", $"ulong ulong.{name}(ulong left, ulong right)");
                unifiedBinaryOps(symbol, "nuint?", "float?", $"float float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nuint?", "double?", $"double double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nuint?", "decimal?", $"decimal decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nuint?", "System.IntPtr?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "System.UIntPtr?", $"nuint nuint.{name}(nuint left, nuint right)");
            }

            foreach ((string symbol, string name) in shiftOperators)
            {
                // nint shiftOp type
                unifiedBinaryOps(symbol, "nint", "object");
                unifiedBinaryOps(symbol, "nint", "string");
                unifiedBinaryOps(symbol, "nint", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nint", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "nint", includeVoidError: true));
                unifiedBinaryOps(symbol, "nint", "bool");
                unifiedBinaryOps(symbol, "nint", "char", $"nint nint.{name}(nint left, int right)", null);
                unifiedBinaryOps(symbol, "nint", "sbyte", $"nint nint.{name}(nint left, int right)", null);
                unifiedBinaryOps(symbol, "nint", "byte", $"nint nint.{name}(nint left, int right)", null);
                unifiedBinaryOps(symbol, "nint", "short", $"nint nint.{name}(nint left, int right)", null);
                unifiedBinaryOps(symbol, "nint", "ushort", $"nint nint.{name}(nint left, int right)", null);
                unifiedBinaryOps(symbol, "nint", "int", $"nint nint.{name}(nint left, int right)", null);
                unifiedBinaryOps(symbol, "nint", "uint");
                unifiedBinaryOps(symbol, "nint", "nint");
                unifiedBinaryOps(symbol, "nint", "nuint");
                unifiedBinaryOps(symbol, "nint", "long");
                unifiedBinaryOps(symbol, "nint", "ulong");
                unifiedBinaryOps(symbol, "nint", "float");
                unifiedBinaryOps(symbol, "nint", "double");
                unifiedBinaryOps(symbol, "nint", "decimal");
                unifiedBinaryOps(symbol, "nint", "System.IntPtr");
                unifiedBinaryOps(symbol, "nint", "System.UIntPtr");

                // nint shiftOp type?
                unifiedBinaryOps(symbol, "nint", "bool?");
                unifiedBinaryOps(symbol, "nint", "char?", $"nint nint.{name}(nint left, int right)", null);
                unifiedBinaryOps(symbol, "nint", "sbyte?", $"nint nint.{name}(nint left, int right)", null);
                unifiedBinaryOps(symbol, "nint", "byte?", $"nint nint.{name}(nint left, int right)", null);
                unifiedBinaryOps(symbol, "nint", "short?", $"nint nint.{name}(nint left, int right)", null);
                unifiedBinaryOps(symbol, "nint", "ushort?", $"nint nint.{name}(nint left, int right)", null);
                unifiedBinaryOps(symbol, "nint", "int?", $"nint nint.{name}(nint left, int right)", null);
                unifiedBinaryOps(symbol, "nint", "uint?");
                unifiedBinaryOps(symbol, "nint", "nint?");
                unifiedBinaryOps(symbol, "nint", "nuint?");
                unifiedBinaryOps(symbol, "nint", "long?");
                unifiedBinaryOps(symbol, "nint", "ulong?");
                unifiedBinaryOps(symbol, "nint", "float?");
                unifiedBinaryOps(symbol, "nint", "double?");
                unifiedBinaryOps(symbol, "nint", "decimal?");
                unifiedBinaryOps(symbol, "nint", "System.IntPtr?");
                unifiedBinaryOps(symbol, "nint", "System.UIntPtr?");
                unifiedBinaryOps(symbol, "nint", "object");

                // nint? shiftOp type
                unifiedBinaryOps(symbol, "nint?", "string");
                unifiedBinaryOps(symbol, "nint?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nint?", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "nint?", includeVoidError: true));
                unifiedBinaryOps(symbol, "nint?", "bool");
                unifiedBinaryOps(symbol, "nint?", "char", $"nint nint.{name}(nint left, int right)", null);
                unifiedBinaryOps(symbol, "nint?", "sbyte", $"nint nint.{name}(nint left, int right)", null);
                unifiedBinaryOps(symbol, "nint?", "byte", $"nint nint.{name}(nint left, int right)", null);
                unifiedBinaryOps(symbol, "nint?", "short", $"nint nint.{name}(nint left, int right)", null);
                unifiedBinaryOps(symbol, "nint?", "ushort", $"nint nint.{name}(nint left, int right)", null);
                unifiedBinaryOps(symbol, "nint?", "int", $"nint nint.{name}(nint left, int right)", null);
                unifiedBinaryOps(symbol, "nint?", "uint");
                unifiedBinaryOps(symbol, "nint?", "nint");
                unifiedBinaryOps(symbol, "nint?", "nuint");
                unifiedBinaryOps(symbol, "nint?", "long");
                unifiedBinaryOps(symbol, "nint?", "ulong");
                unifiedBinaryOps(symbol, "nint?", "float");
                unifiedBinaryOps(symbol, "nint?", "double");
                unifiedBinaryOps(symbol, "nint?", "decimal");
                unifiedBinaryOps(symbol, "nint?", "System.IntPtr");
                unifiedBinaryOps(symbol, "nint?", "System.UIntPtr");

                // nint? shiftOp type?
                unifiedBinaryOps(symbol, "nint?", "bool?");
                unifiedBinaryOps(symbol, "nint?", "char?", $"nint nint.{name}(nint left, int right)", null);
                unifiedBinaryOps(symbol, "nint?", "sbyte?", $"nint nint.{name}(nint left, int right)", null);
                unifiedBinaryOps(symbol, "nint?", "byte?", $"nint nint.{name}(nint left, int right)", null);
                unifiedBinaryOps(symbol, "nint?", "short?", $"nint nint.{name}(nint left, int right)", null);
                unifiedBinaryOps(symbol, "nint?", "ushort?", $"nint nint.{name}(nint left, int right)", null);
                unifiedBinaryOps(symbol, "nint?", "int?", $"nint nint.{name}(nint left, int right)", null);
                unifiedBinaryOps(symbol, "nint?", "uint?");
                unifiedBinaryOps(symbol, "nint?", "nint?");
                unifiedBinaryOps(symbol, "nint?", "nuint?");
                unifiedBinaryOps(symbol, "nint?", "long?");
                unifiedBinaryOps(symbol, "nint?", "ulong?");
                unifiedBinaryOps(symbol, "nint?", "float?");
                unifiedBinaryOps(symbol, "nint?", "double?");
                unifiedBinaryOps(symbol, "nint?", "decimal?");
                unifiedBinaryOps(symbol, "nint?", "System.IntPtr?");
                unifiedBinaryOps(symbol, "nint?", "System.UIntPtr?");

                // nuint shiftOp type
                unifiedBinaryOps(symbol, "nuint", "object");
                unifiedBinaryOps(symbol, "nuint", "string");
                unifiedBinaryOps(symbol, "nuint", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nuint", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "nuint", includeVoidError: true));
                unifiedBinaryOps(symbol, "nuint", "bool");
                unifiedBinaryOps(symbol, "nuint", "char", $"nuint nuint.{name}(nuint left, int right)", null);
                unifiedBinaryOps(symbol, "nuint", "sbyte", $"nuint nuint.{name}(nuint left, int right)", null);
                unifiedBinaryOps(symbol, "nuint", "byte", $"nuint nuint.{name}(nuint left, int right)", null);
                unifiedBinaryOps(symbol, "nuint", "short", $"nuint nuint.{name}(nuint left, int right)", null);
                unifiedBinaryOps(symbol, "nuint", "ushort", $"nuint nuint.{name}(nuint left, int right)", null);
                unifiedBinaryOps(symbol, "nuint", "int", $"nuint nuint.{name}(nuint left, int right)", null);
                unifiedBinaryOps(symbol, "nuint", "uint");
                unifiedBinaryOps(symbol, "nuint", "nint");
                unifiedBinaryOps(symbol, "nuint", "nuint");
                unifiedBinaryOps(symbol, "nuint", "long");
                unifiedBinaryOps(symbol, "nuint", "ulong");
                unifiedBinaryOps(symbol, "nuint", "float");
                unifiedBinaryOps(symbol, "nuint", "double");
                unifiedBinaryOps(symbol, "nuint", "decimal");
                unifiedBinaryOps(symbol, "nuint", "System.IntPtr");
                unifiedBinaryOps(symbol, "nuint", "System.UIntPtr");

                // nuint shiftOp type?
                unifiedBinaryOps(symbol, "nuint", "bool?");
                unifiedBinaryOps(symbol, "nuint", "char?", $"nuint nuint.{name}(nuint left, int right)", null);
                unifiedBinaryOps(symbol, "nuint", "sbyte?", $"nuint nuint.{name}(nuint left, int right)", null);
                unifiedBinaryOps(symbol, "nuint", "byte?", $"nuint nuint.{name}(nuint left, int right)", null);
                unifiedBinaryOps(symbol, "nuint", "short?", $"nuint nuint.{name}(nuint left, int right)", null);
                unifiedBinaryOps(symbol, "nuint", "ushort?", $"nuint nuint.{name}(nuint left, int right)", null);
                unifiedBinaryOps(symbol, "nuint", "int?", $"nuint nuint.{name}(nuint left, int right)", null);
                unifiedBinaryOps(symbol, "nuint", "uint?");
                unifiedBinaryOps(symbol, "nuint", "nint?");
                unifiedBinaryOps(symbol, "nuint", "nuint?");
                unifiedBinaryOps(symbol, "nuint", "long?");
                unifiedBinaryOps(symbol, "nuint", "ulong?");
                unifiedBinaryOps(symbol, "nuint", "float?");
                unifiedBinaryOps(symbol, "nuint", "double?");
                unifiedBinaryOps(symbol, "nuint", "decimal?");
                unifiedBinaryOps(symbol, "nuint", "System.IntPtr?");
                unifiedBinaryOps(symbol, "nuint", "System.UIntPtr?");

                // nuint? shiftOp type
                unifiedBinaryOps(symbol, "nuint?", "object");
                unifiedBinaryOps(symbol, "nuint?", "string");
                unifiedBinaryOps(symbol, "nuint?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nuint?", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "nuint?", includeVoidError: true));
                unifiedBinaryOps(symbol, "nuint?", "bool");
                unifiedBinaryOps(symbol, "nuint?", "char", $"nuint nuint.{name}(nuint left, int right)", null);
                unifiedBinaryOps(symbol, "nuint?", "sbyte", $"nuint nuint.{name}(nuint left, int right)", null);
                unifiedBinaryOps(symbol, "nuint?", "byte", $"nuint nuint.{name}(nuint left, int right)", null);
                unifiedBinaryOps(symbol, "nuint?", "short", $"nuint nuint.{name}(nuint left, int right)", null);
                unifiedBinaryOps(symbol, "nuint?", "ushort", $"nuint nuint.{name}(nuint left, int right)", null);
                unifiedBinaryOps(symbol, "nuint?", "int", $"nuint nuint.{name}(nuint left, int right)", null);
                unifiedBinaryOps(symbol, "nuint?", "uint");
                unifiedBinaryOps(symbol, "nuint?", "nint");
                unifiedBinaryOps(symbol, "nuint?", "nuint");
                unifiedBinaryOps(symbol, "nuint?", "long");
                unifiedBinaryOps(symbol, "nuint?", "ulong");
                unifiedBinaryOps(symbol, "nuint?", "float");
                unifiedBinaryOps(symbol, "nuint?", "double");
                unifiedBinaryOps(symbol, "nuint?", "decimal");
                unifiedBinaryOps(symbol, "nuint?", "System.IntPtr");
                unifiedBinaryOps(symbol, "nuint?", "System.UIntPtr");

                // nuint? shiftOp type?
                unifiedBinaryOps(symbol, "nuint?", "bool?");
                unifiedBinaryOps(symbol, "nuint?", "char?", $"nuint nuint.{name}(nuint left, int right)", null);
                unifiedBinaryOps(symbol, "nuint?", "sbyte?", $"nuint nuint.{name}(nuint left, int right)", null);
                unifiedBinaryOps(symbol, "nuint?", "byte?", $"nuint nuint.{name}(nuint left, int right)", null);
                unifiedBinaryOps(symbol, "nuint?", "short?", $"nuint nuint.{name}(nuint left, int right)", null);
                unifiedBinaryOps(symbol, "nuint?", "ushort?", $"nuint nuint.{name}(nuint left, int right)", null);
                unifiedBinaryOps(symbol, "nuint?", "int?", $"nuint nuint.{name}(nuint left, int right)", null);
                unifiedBinaryOps(symbol, "nuint?", "uint?");
                unifiedBinaryOps(symbol, "nuint?", "nint?");
                unifiedBinaryOps(symbol, "nuint?", "nuint?");
                unifiedBinaryOps(symbol, "nuint?", "long?");
                unifiedBinaryOps(symbol, "nuint?", "ulong?");
                unifiedBinaryOps(symbol, "nuint?", "float?");
                unifiedBinaryOps(symbol, "nuint?", "double?");
                unifiedBinaryOps(symbol, "nuint?", "decimal?");
                unifiedBinaryOps(symbol, "nuint?", "System.IntPtr?");
                unifiedBinaryOps(symbol, "nuint?", "System.UIntPtr?");
            }

            foreach ((string symbol, string name) in equalityOperators)
            {
                // nint equalityOp type
                unifiedBinaryOps(symbol, "nint", "object");
                unifiedBinaryOps(symbol, "nint", "string");
                unifiedBinaryOps(symbol, "nint", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nint", "void*"), getBadBinaryOpsDiagnostics(symbol, "void*", "nint"));
                unifiedBinaryOps(symbol, "nint", "bool");
                unifiedBinaryOps(symbol, "nint", "char", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "sbyte", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "byte", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "short", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "ushort", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "int", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "uint", $"bool long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint", "nint", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "nuint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint"));
                unifiedBinaryOps(symbol, "nint", "long", $"bool long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint", "ulong", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "ulong"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong", "nint"));
                unifiedBinaryOps(symbol, "nint", "float", $"bool float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nint", "double", $"bool double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nint", "decimal", $"bool decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nint", "System.IntPtr", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "System.UIntPtr", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint"));

                // nint equalityOp type?
                unifiedBinaryOps(symbol, "nint", "bool?");
                unifiedBinaryOps(symbol, "nint", "char?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "sbyte?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "byte?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "short?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "ushort?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "int?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "uint?", $"bool long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint", "nint?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "nuint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint"));
                unifiedBinaryOps(symbol, "nint", "long?", $"bool long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint", "ulong?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "ulong?"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong?", "nint"));
                unifiedBinaryOps(symbol, "nint", "float?", $"bool float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nint", "double?", $"bool double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nint", "decimal?", $"bool decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nint", "System.IntPtr?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "System.UIntPtr?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint"));
                unifiedBinaryOps(symbol, "nint", "object");

                // nint? equalityOp type
                unifiedBinaryOps(symbol, "nint?", "string");
                unifiedBinaryOps(symbol, "nint?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nint?", "void*"), getBadBinaryOpsDiagnostics(symbol, "void*", "nint?"));
                unifiedBinaryOps(symbol, "nint?", "bool");
                unifiedBinaryOps(symbol, "nint?", "char", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "sbyte", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "byte", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "short", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "ushort", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "int", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "uint", $"bool long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint?", "nint", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "nuint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint?"));
                unifiedBinaryOps(symbol, "nint?", "long", $"bool long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint?", "ulong", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "ulong"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong", "nint?"));
                unifiedBinaryOps(symbol, "nint?", "float", $"bool float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nint?", "double", $"bool double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nint?", "decimal", $"bool decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nint?", "System.IntPtr", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "System.UIntPtr", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint?"));

                // nint? equalityOp type?
                unifiedBinaryOps(symbol, "nint?", "bool?");
                unifiedBinaryOps(symbol, "nint?", "char?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "sbyte?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "byte?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "short?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "ushort?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "int?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "uint?", $"bool long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint?", "nint?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "nuint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint?"));
                unifiedBinaryOps(symbol, "nint?", "long?", $"bool long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint?", "ulong?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "ulong?"), getAmbiguousBinaryOpsDiagnostics(symbol, "ulong?", "nint?"));
                unifiedBinaryOps(symbol, "nint?", "float?", $"bool float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nint?", "double?", $"bool double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nint?", "decimal?", $"bool decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nint?", "System.IntPtr?", $"bool nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "System.UIntPtr?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint?"));

                // nuint equalityOp type
                unifiedBinaryOps(symbol, "nuint", "object");
                unifiedBinaryOps(symbol, "nuint", "string");
                unifiedBinaryOps(symbol, "nuint", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nuint", "void*"), getBadBinaryOpsDiagnostics(symbol, "void*", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "bool");
                unifiedBinaryOps(symbol, "nuint", "char", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "sbyte", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "sbyte"), getAmbiguousBinaryOpsDiagnostics(symbol, "sbyte", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "byte", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "short", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "short"), getAmbiguousBinaryOpsDiagnostics(symbol, "short", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "ushort", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "int", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "int"), getAmbiguousBinaryOpsDiagnostics(symbol, "int", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "uint", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "nint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "nuint", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "long", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "long"), getAmbiguousBinaryOpsDiagnostics(symbol, "long", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "ulong", $"bool ulong.{name}(ulong left, ulong right)");
                unifiedBinaryOps(symbol, "nuint", "float", $"bool float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nuint", "double", $"bool double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nuint", "decimal", $"bool decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nuint", "System.IntPtr", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "System.UIntPtr", $"bool nuint.{name}(nuint left, nuint right)");

                // nuint equalityOp type?
                unifiedBinaryOps(symbol, "nuint", "bool?");
                unifiedBinaryOps(symbol, "nuint", "char?", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "sbyte?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "sbyte?"), getAmbiguousBinaryOpsDiagnostics(symbol, "sbyte?", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "byte?", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "short?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "short?"), getAmbiguousBinaryOpsDiagnostics(symbol, "short?", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "ushort?", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "int?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "int?"), getAmbiguousBinaryOpsDiagnostics(symbol, "int?", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "uint?", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "nint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "nuint?", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "long?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "long?"), getAmbiguousBinaryOpsDiagnostics(symbol, "long?", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "ulong?", $"bool ulong.{name}(ulong left, ulong right)");
                unifiedBinaryOps(symbol, "nuint", "float?", $"bool float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nuint", "double?", $"bool double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nuint", "decimal?", $"bool decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nuint", "System.IntPtr?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint", "nint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint"));
                unifiedBinaryOps(symbol, "nuint", "System.UIntPtr?", $"bool nuint.{name}(nuint left, nuint right)");

                // nuint? equalityOp type
                unifiedBinaryOps(symbol, "nuint?", "object");
                unifiedBinaryOps(symbol, "nuint?", "string");
                unifiedBinaryOps(symbol, "nuint?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nuint?", "void*"), getBadBinaryOpsDiagnostics(symbol, "void*", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "bool");
                unifiedBinaryOps(symbol, "nuint?", "char", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "sbyte", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "sbyte"), getAmbiguousBinaryOpsDiagnostics(symbol, "sbyte", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "byte", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "short", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "short"), getAmbiguousBinaryOpsDiagnostics(symbol, "short", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "ushort", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "int", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "int"), getAmbiguousBinaryOpsDiagnostics(symbol, "int", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "uint", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "nint", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "nuint", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "long", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "long"), getAmbiguousBinaryOpsDiagnostics(symbol, "long", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "ulong", $"bool ulong.{name}(ulong left, ulong right)");
                unifiedBinaryOps(symbol, "nuint?", "float", $"bool float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nuint?", "double", $"bool double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nuint?", "decimal", $"bool decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nuint?", "System.IntPtr", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "System.UIntPtr", $"bool nuint.{name}(nuint left, nuint right)");

                // nuint? equalityOp type?
                unifiedBinaryOps(symbol, "nuint?", "bool?");
                unifiedBinaryOps(symbol, "nuint?", "char?", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "sbyte?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "sbyte?"), getAmbiguousBinaryOpsDiagnostics(symbol, "sbyte?", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "byte?", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "short?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "short?"), getAmbiguousBinaryOpsDiagnostics(symbol, "short?", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "ushort?", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "int?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "int?"), getAmbiguousBinaryOpsDiagnostics(symbol, "int?", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "uint?", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "nint?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "nuint?", $"bool nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "long?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "long?"), getAmbiguousBinaryOpsDiagnostics(symbol, "long?", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "ulong?", $"bool ulong.{name}(ulong left, ulong right)");
                unifiedBinaryOps(symbol, "nuint?", "float?", $"bool float.{name}(float left, float right)");
                unifiedBinaryOps(symbol, "nuint?", "double?", $"bool double.{name}(double left, double right)");
                unifiedBinaryOps(symbol, "nuint?", "decimal?", $"bool decimal.{name}(decimal left, decimal right)");
                unifiedBinaryOps(symbol, "nuint?", "System.IntPtr?", null, null, getAmbiguousBinaryOpsDiagnostics(symbol, "nuint?", "nint?"), getAmbiguousBinaryOpsDiagnostics(symbol, "nint?", "nuint?"));
                unifiedBinaryOps(symbol, "nuint?", "System.UIntPtr?", $"bool nuint.{name}(nuint left, nuint right)");
            }

            foreach ((string symbol, string name) in logicalOperators)
            {
                // nint logicalOp type
                unifiedBinaryOps(symbol, "nint", "object");
                unifiedBinaryOps(symbol, "nint", "string");
                unifiedBinaryOps(symbol, "nint", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nint", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "nint", includeVoidError: true));
                unifiedBinaryOps(symbol, "nint", "bool");
                unifiedBinaryOps(symbol, "nint", "char", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "sbyte", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "byte", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "short", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "ushort", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "int", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "uint", $"long long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint", "nint", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "nuint");
                unifiedBinaryOps(symbol, "nint", "long", $"long long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint", "ulong");
                unifiedBinaryOps(symbol, "nint", "float");
                unifiedBinaryOps(symbol, "nint", "double");
                unifiedBinaryOps(symbol, "nint", "decimal");
                unifiedBinaryOps(symbol, "nint", "System.IntPtr", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "System.UIntPtr");

                // nint logicalOp type?
                unifiedBinaryOps(symbol, "nint", "bool?");
                unifiedBinaryOps(symbol, "nint", "char?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "sbyte?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "byte?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "short?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "ushort?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "int?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "uint?", $"long long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint", "nint?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "nuint?");
                unifiedBinaryOps(symbol, "nint", "long?", $"long long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint", "ulong?");
                unifiedBinaryOps(symbol, "nint", "float?");
                unifiedBinaryOps(symbol, "nint", "double?");
                unifiedBinaryOps(symbol, "nint", "decimal?");
                unifiedBinaryOps(symbol, "nint", "System.IntPtr?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint", "System.UIntPtr?");
                unifiedBinaryOps(symbol, "nint", "object");

                // nint? logicalOp type
                unifiedBinaryOps(symbol, "nint?", "string");
                unifiedBinaryOps(symbol, "nint?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nint?", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "nint?", includeVoidError: true));
                unifiedBinaryOps(symbol, "nint?", "bool");
                unifiedBinaryOps(symbol, "nint?", "char", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "sbyte", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "byte", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "short", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "ushort", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "int", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "uint", $"long long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint?", "nint", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "nuint");
                unifiedBinaryOps(symbol, "nint?", "long", $"long long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint?", "ulong");
                unifiedBinaryOps(symbol, "nint?", "float");
                unifiedBinaryOps(symbol, "nint?", "double");
                unifiedBinaryOps(symbol, "nint?", "decimal");
                unifiedBinaryOps(symbol, "nint?", "System.IntPtr", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "System.UIntPtr");

                // nint? logicalOp type?
                unifiedBinaryOps(symbol, "nint?", "bool?");
                unifiedBinaryOps(symbol, "nint?", "char?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "sbyte?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "byte?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "short?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "ushort?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "int?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "uint?", $"long long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint?", "nint?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "nuint?");
                unifiedBinaryOps(symbol, "nint?", "long?", $"long long.{name}(long left, long right)");
                unifiedBinaryOps(symbol, "nint?", "ulong?");
                unifiedBinaryOps(symbol, "nint?", "float?");
                unifiedBinaryOps(symbol, "nint?", "double?");
                unifiedBinaryOps(symbol, "nint?", "decimal?");
                unifiedBinaryOps(symbol, "nint?", "System.IntPtr?", $"nint nint.{name}(nint left, nint right)");
                unifiedBinaryOps(symbol, "nint?", "System.UIntPtr?");

                // nuint logicalOp type
                unifiedBinaryOps(symbol, "nuint", "object");
                unifiedBinaryOps(symbol, "nuint", "string");
                unifiedBinaryOps(symbol, "nuint", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nuint", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "nuint", includeVoidError: true));
                unifiedBinaryOps(symbol, "nuint", "bool");
                unifiedBinaryOps(symbol, "nuint", "char", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "sbyte");
                unifiedBinaryOps(symbol, "nuint", "byte", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "short");
                unifiedBinaryOps(symbol, "nuint", "ushort", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "int");
                unifiedBinaryOps(symbol, "nuint", "uint", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "nint");
                unifiedBinaryOps(symbol, "nuint", "nuint", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "long");
                unifiedBinaryOps(symbol, "nuint", "ulong", $"ulong ulong.{name}(ulong left, ulong right)");
                unifiedBinaryOps(symbol, "nuint", "float");
                unifiedBinaryOps(symbol, "nuint", "double");
                unifiedBinaryOps(symbol, "nuint", "decimal");
                unifiedBinaryOps(symbol, "nuint", "System.IntPtr");
                unifiedBinaryOps(symbol, "nuint", "System.UIntPtr", $"nuint nuint.{name}(nuint left, nuint right)");

                // nuint logicalOp type?
                unifiedBinaryOps(symbol, "nuint", "bool?");
                unifiedBinaryOps(symbol, "nuint", "char?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "sbyte?");
                unifiedBinaryOps(symbol, "nuint", "byte?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "short?");
                unifiedBinaryOps(symbol, "nuint", "ushort?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "int?");
                unifiedBinaryOps(symbol, "nuint", "uint?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "nint?");
                unifiedBinaryOps(symbol, "nuint", "nuint?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint", "long?");
                unifiedBinaryOps(symbol, "nuint", "ulong?", $"ulong ulong.{name}(ulong left, ulong right)");
                unifiedBinaryOps(symbol, "nuint", "float?");
                unifiedBinaryOps(symbol, "nuint", "double?");
                unifiedBinaryOps(symbol, "nuint", "decimal?");
                unifiedBinaryOps(symbol, "nuint", "System.IntPtr?");
                unifiedBinaryOps(symbol, "nuint", "System.UIntPtr?", $"nuint nuint.{name}(nuint left, nuint right)");

                // nuint? logicalOp type
                unifiedBinaryOps(symbol, "nuint?", "object");
                unifiedBinaryOps(symbol, "nuint?", "string");
                unifiedBinaryOps(symbol, "nuint?", "void*", null, null, getBadBinaryOpsDiagnostics(symbol, "nuint?", "void*", includeVoidError: true), getBadBinaryOpsDiagnostics(symbol, "void*", "nuint?", includeVoidError: true));
                unifiedBinaryOps(symbol, "nuint?", "bool");
                unifiedBinaryOps(symbol, "nuint?", "char", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "sbyte");
                unifiedBinaryOps(symbol, "nuint?", "byte", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "short");
                unifiedBinaryOps(symbol, "nuint?", "ushort", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "int");
                unifiedBinaryOps(symbol, "nuint?", "uint", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "nint");
                unifiedBinaryOps(symbol, "nuint?", "nuint", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "long");
                unifiedBinaryOps(symbol, "nuint?", "ulong", $"ulong ulong.{name}(ulong left, ulong right)");
                unifiedBinaryOps(symbol, "nuint?", "float");
                unifiedBinaryOps(symbol, "nuint?", "double");
                unifiedBinaryOps(symbol, "nuint?", "decimal");
                unifiedBinaryOps(symbol, "nuint?", "System.IntPtr");
                unifiedBinaryOps(symbol, "nuint?", "System.UIntPtr", $"nuint nuint.{name}(nuint left, nuint right)");

                // nuint? logicalOp type?
                unifiedBinaryOps(symbol, "nuint?", "bool?");
                unifiedBinaryOps(symbol, "nuint?", "char?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "sbyte?");
                unifiedBinaryOps(symbol, "nuint?", "byte?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "short?");
                unifiedBinaryOps(symbol, "nuint?", "ushort?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "int?");
                unifiedBinaryOps(symbol, "nuint?", "uint?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "nint?");
                unifiedBinaryOps(symbol, "nuint?", "nuint?", $"nuint nuint.{name}(nuint left, nuint right)");
                unifiedBinaryOps(symbol, "nuint?", "long?");
                unifiedBinaryOps(symbol, "nuint?", "ulong?", $"ulong ulong.{name}(ulong left, ulong right)");
                unifiedBinaryOps(symbol, "nuint?", "float?");
                unifiedBinaryOps(symbol, "nuint?", "double?");
                unifiedBinaryOps(symbol, "nuint?", "decimal?");
                unifiedBinaryOps(symbol, "nuint?", "System.IntPtr?");
                unifiedBinaryOps(symbol, "nuint?", "System.UIntPtr?", $"nuint nuint.{name}(nuint left, nuint right)");
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
                var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithAllowUnsafe(useUnsafeContext), parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
                comp.VerifyDiagnostics(expectedDiagnostics);

                var tree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(tree);
                var expr = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();
                var symbolInfo = model.GetSymbolInfo(expr);
                Assert.Equal(expectedSymbol, symbolInfo.Symbol?.ToDisplayString(SymbolDisplayFormat.TestFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes)));

                if (expectedDiagnostics.Length == 0)
                {
                    CompileAndVerify(comp, verify: Verification.FailsPEVerify);
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
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);

            var verifier = CompileAndVerify(comp, verify: Verification.FailsPEVerify, expectedOutput: IncludeExpectedOutput(
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
2"));
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
  // Code size       15 (0xf)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  sizeof     ""nint""
  IL_0008:  ldc.i4.8
  IL_0009:  mul
  IL_000a:  ldc.i4.1
  IL_000b:  sub
  IL_000c:  and
  IL_000d:  shl
  IL_000e:  ret
}");
            verifier.VerifyIL("Program.ShiftRight",
@"{
  // Code size       15 (0xf)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  sizeof     ""nint""
  IL_0008:  ldc.i4.8
  IL_0009:  mul
  IL_000a:  ldc.i4.1
  IL_000b:  sub
  IL_000c:  and
  IL_000d:  shr
  IL_000e:  ret
}");
        }

        [Fact]
        public void BinaryOperators_NUInt()
        {
            var source =
@"using System;
class Program
{
    static UIntPtr Add(UIntPtr x, UIntPtr y) => x + y;
    static UIntPtr Subtract(UIntPtr x, UIntPtr y) => x - y;
    static UIntPtr Multiply(UIntPtr x, UIntPtr y) => x * y;
    static UIntPtr Divide(UIntPtr x, UIntPtr y) => x / y;
    static UIntPtr Mod(UIntPtr x, UIntPtr y) => x % y;
    static bool Equals(UIntPtr x, UIntPtr y) => x == y;
    static bool NotEquals(UIntPtr x, UIntPtr y) => x != y;
    static bool LessThan(UIntPtr x, UIntPtr y) => x < y;
    static bool LessThanOrEqual(UIntPtr x, UIntPtr y) => x <= y;
    static bool GreaterThan(UIntPtr x, UIntPtr y) => x > y;
    static bool GreaterThanOrEqual(UIntPtr x, UIntPtr y) => x >= y;
    static UIntPtr And(UIntPtr x, UIntPtr y) => x & y;
    static UIntPtr Or(UIntPtr x, UIntPtr y) => x | y;
    static UIntPtr Xor(UIntPtr x, UIntPtr y) => x ^ y;
    static UIntPtr ShiftLeft(UIntPtr x, int y) => x << y;
    static UIntPtr ShiftRight(UIntPtr x, int y) => x >> y;
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
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);

            var verifier = CompileAndVerify(comp, verify: Verification.FailsPEVerify, expectedOutput: IncludeExpectedOutput(
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
2"));
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
  // Code size       15 (0xf)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  sizeof     ""nuint""
  IL_0008:  ldc.i4.8
  IL_0009:  mul
  IL_000a:  ldc.i4.1
  IL_000b:  sub
  IL_000c:  and
  IL_000d:  shl
  IL_000e:  ret
}");
            verifier.VerifyIL("Program.ShiftRight",
@"{
  // Code size       15 (0xf)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  sizeof     ""nuint""
  IL_0008:  ldc.i4.8
  IL_0009:  mul
  IL_000a:  ldc.i4.1
  IL_000b:  sub
  IL_000c:  and
  IL_000d:  shr.un
  IL_000e:  ret
}");
        }

        [Fact]
        public void BinaryOperators_NInt_Checked()
        {
            var source =
@"using System;
class Program
{
    static IntPtr Add(IntPtr x, IntPtr y) => checked(x + y);
    static IntPtr Subtract(IntPtr x, IntPtr y) => checked(x - y);
    static IntPtr Multiply(IntPtr x, IntPtr y) => checked(x * y);
    static IntPtr Divide(IntPtr x, IntPtr y) => checked(x / y);
    static IntPtr Mod(IntPtr x, IntPtr y) => checked(x % y);
    static void Main()
    {
        Console.WriteLine(Add(3, 4));
        Console.WriteLine(Subtract(3, 4));
        Console.WriteLine(Multiply(3, 4));
        Console.WriteLine(Divide(5, 2));
        Console.WriteLine(Mod(5, 2));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);

            var verifier = CompileAndVerify(comp, verify: Verification.FailsPEVerify, expectedOutput: IncludeExpectedOutput(
@"7
-1
12
2
1"));
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
    static UIntPtr Add(UIntPtr x, UIntPtr y) => checked(x + y);
    static UIntPtr Subtract(UIntPtr x, UIntPtr y) => checked(x - y);
    static UIntPtr Multiply(UIntPtr x, UIntPtr y) => checked(x * y);
    static UIntPtr Divide(UIntPtr x, UIntPtr y) => checked(x / y);
    static UIntPtr Mod(UIntPtr x, UIntPtr y) => checked(x % y);
    static void Main()
    {
        Console.WriteLine(Add(3, 4));
        Console.WriteLine(Subtract(4, 3));
        Console.WriteLine(Multiply(3, 4));
        Console.WriteLine(Divide(5, 2));
        Console.WriteLine(Mod(5, 2));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);

            var verifier = CompileAndVerify(comp, verify: Verification.FailsPEVerify, expectedOutput: IncludeExpectedOutput(
@"7
1
12
2
1"));
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
        public void ConstantFolding_01()
        {
            const string intMinValue = "-2147483648";
            const string intMaxValue = "2147483647";
            const string uintMaxValue = "4294967295";
            const string ulongMaxValue = "18446744073709551615";

            unaryOperator("System.IntPtr", "+", intMinValue, intMinValue);
            unaryOperator("System.IntPtr", "+", intMaxValue, intMaxValue);
            unaryOperator("System.UIntPtr", "+", "0", "0");
            unaryOperator("System.UIntPtr", "+", uintMaxValue, uintMaxValue);

            unaryOperator("System.IntPtr", "-", "-1", "1");
            unaryOperatorCheckedOverflow("System.IntPtr", "-", intMinValue, IntPtr.Size == 4 ? "-2147483648" : "2147483648", IntPtr.Size == 4 ? "System.OverflowException" : "2147483648");
            unaryOperator("System.IntPtr", "-", "-2147483647", intMaxValue);
            unaryOperator("System.IntPtr", "-", intMaxValue, "-2147483647");
            unaryOperator("System.UIntPtr", "-", "0", null, getBadUnaryOpDiagnostics);
            unaryOperator("System.UIntPtr", "-", "1", null, getBadUnaryOpDiagnostics);
            unaryOperator("System.UIntPtr", "-", uintMaxValue, null, getBadUnaryOpDiagnostics);

            unaryOperatorNotConstant("System.IntPtr", "~", "0", "-1");
            unaryOperatorNotConstant("System.IntPtr", "~", "-1", "0");
            unaryOperatorNotConstant("System.IntPtr", "~", intMinValue, "2147483647");
            unaryOperatorNotConstant("System.IntPtr", "~", intMaxValue, "-2147483648");
            unaryOperatorNotConstant("System.UIntPtr", "~", "0", IntPtr.Size == 4 ? uintMaxValue : ulongMaxValue);
            unaryOperatorNotConstant("System.UIntPtr", "~", uintMaxValue, IntPtr.Size == 4 ? "0" : "18446744069414584320");

            binaryOperatorCheckedOverflow("System.IntPtr", "+", "System.IntPtr", intMinValue, "System.IntPtr", "-1", IntPtr.Size == 4 ? "2147483647" : "-2147483649", IntPtr.Size == 4 ? "System.OverflowException" : "-2147483649");
            binaryOperator("System.IntPtr", "+", "System.IntPtr", "-2147483647", "System.IntPtr", "-1", intMinValue);
            binaryOperatorCheckedOverflow("System.IntPtr", "+", "System.IntPtr", "1", "System.IntPtr", intMaxValue, IntPtr.Size == 4 ? "-2147483648" : "2147483648", IntPtr.Size == 4 ? "System.OverflowException" : "2147483648");
            binaryOperator("System.IntPtr", "+", "System.IntPtr", "1", "System.IntPtr", "2147483646", intMaxValue);
            binaryOperatorCheckedOverflow("System.UIntPtr", "+", "System.UIntPtr", "1", "System.UIntPtr", uintMaxValue, IntPtr.Size == 4 ? "0" : "4294967296", IntPtr.Size == 4 ? "System.OverflowException" : "4294967296");
            binaryOperator("System.UIntPtr", "+", "System.UIntPtr", "1", "System.UIntPtr", "4294967294", uintMaxValue);

            binaryOperatorCheckedOverflow("System.IntPtr", "-", "System.IntPtr", intMinValue, "System.IntPtr", "1", IntPtr.Size == 4 ? "2147483647" : "-2147483649", IntPtr.Size == 4 ? "System.OverflowException" : "-2147483649");
            binaryOperator("System.IntPtr", "-", "System.IntPtr", intMinValue, "System.IntPtr", "-1", "-2147483647");
            binaryOperator("System.IntPtr", "-", "System.IntPtr", "-1", "System.IntPtr", intMaxValue, intMinValue);
            binaryOperatorCheckedOverflow("System.IntPtr", "-", "System.IntPtr", "-2", "System.IntPtr", intMaxValue, IntPtr.Size == 4 ? "2147483647" : "-2147483649", IntPtr.Size == 4 ? "System.OverflowException" : "-2147483649");
            binaryOperatorCheckedOverflow("System.UIntPtr", "-", "System.UIntPtr", "0", "System.UIntPtr", "1", IntPtr.Size == 4 ? uintMaxValue : ulongMaxValue, "System.OverflowException");
            binaryOperator("System.UIntPtr", "-", "System.UIntPtr", uintMaxValue, "System.UIntPtr", uintMaxValue, "0");

            binaryOperatorCheckedOverflow("System.IntPtr", "*", "System.IntPtr", intMinValue, "System.IntPtr", "2", IntPtr.Size == 4 ? "0" : "-4294967296", IntPtr.Size == 4 ? "System.OverflowException" : "-4294967296");
            binaryOperatorCheckedOverflow("System.IntPtr", "*", "System.IntPtr", intMinValue, "System.IntPtr", "-1", IntPtr.Size == 4 ? "-2147483648" : "2147483648", IntPtr.Size == 4 ? "System.OverflowException" : "2147483648");
            binaryOperator("System.IntPtr", "*", "System.IntPtr", "-1", "System.IntPtr", intMaxValue, "-2147483647");
            binaryOperatorCheckedOverflow("System.IntPtr", "*", "System.IntPtr", "2", "System.IntPtr", intMaxValue, IntPtr.Size == 4 ? "-2" : "4294967294", IntPtr.Size == 4 ? "System.OverflowException" : "4294967294");
            binaryOperatorCheckedOverflow("System.UIntPtr", "*", "System.UIntPtr", uintMaxValue, "System.UIntPtr", "2", IntPtr.Size == 4 ? "4294967294" : "8589934590", IntPtr.Size == 4 ? "System.OverflowException" : "8589934590");
            binaryOperator("System.UIntPtr", "*", "System.UIntPtr", intMaxValue, "System.UIntPtr", "2", "4294967294");

            binaryOperator("System.IntPtr", "/", "System.IntPtr", intMinValue, "System.IntPtr", "1", intMinValue);
            binaryOperatorCheckedOverflow("System.IntPtr", "/", "System.IntPtr", intMinValue, "System.IntPtr", "-1", IntPtr.Size == 4 ? "System.OverflowException" : "2147483648", IntPtr.Size == 4 ? "System.OverflowException" : "2147483648");
            binaryOperator("System.IntPtr", "/", "System.IntPtr", "1", "System.IntPtr", "0", null, getIntDivByZeroDiagnostics);
            binaryOperator("System.IntPtr", "/", "System.IntPtr", "0", "System.IntPtr", "0", null, getIntDivByZeroDiagnostics);
            binaryOperator("System.UIntPtr", "/", "System.UIntPtr", uintMaxValue, "System.UIntPtr", "1", uintMaxValue);
            binaryOperator("System.UIntPtr", "/", "System.UIntPtr", uintMaxValue, "System.UIntPtr", "2", intMaxValue);
            binaryOperator("System.UIntPtr", "/", "System.UIntPtr", "1", "System.UIntPtr", "0", null, getIntDivByZeroDiagnostics);
            binaryOperator("System.UIntPtr", "/", "System.UIntPtr", "0", "System.UIntPtr", "0", null, getIntDivByZeroDiagnostics);

            binaryOperator("System.IntPtr", "%", "System.IntPtr", intMinValue, "System.IntPtr", "2", "0");
            binaryOperator("System.IntPtr", "%", "System.IntPtr", intMinValue, "System.IntPtr", "-2", "0");
            binaryOperatorCheckedOverflow("System.IntPtr", "%", "System.IntPtr", intMinValue, "System.IntPtr", "-1", IntPtr.Size == 4 ? "System.OverflowException" : "0", IntPtr.Size == 4 ? "System.OverflowException" : "0");
            binaryOperator("System.IntPtr", "%", "System.IntPtr", "1", "System.IntPtr", "0", null, getIntDivByZeroDiagnostics);
            binaryOperator("System.IntPtr", "%", "System.IntPtr", "0", "System.IntPtr", "0", null, getIntDivByZeroDiagnostics);
            binaryOperator("System.UIntPtr", "%", "System.UIntPtr", uintMaxValue, "System.UIntPtr", "1", "0");
            binaryOperator("System.UIntPtr", "%", "System.UIntPtr", uintMaxValue, "System.UIntPtr", "2", "1");
            binaryOperator("System.UIntPtr", "%", "System.UIntPtr", "1", "System.UIntPtr", "0", null, getIntDivByZeroDiagnostics);
            binaryOperator("System.UIntPtr", "%", "System.UIntPtr", "0", "System.UIntPtr", "0", null, getIntDivByZeroDiagnostics);

            binaryOperator("bool", "<", "System.IntPtr", intMinValue, "System.IntPtr", intMinValue, "False");
            binaryOperator("bool", "<", "System.IntPtr", intMinValue, "System.IntPtr", intMaxValue, "True");
            binaryOperator("bool", "<", "System.IntPtr", intMaxValue, "System.IntPtr", intMaxValue, "False");
            binaryOperator("bool", "<", "System.UIntPtr", "0", "System.UIntPtr", "0", "False");
            binaryOperator("bool", "<", "System.UIntPtr", "0", "System.UIntPtr", uintMaxValue, "True");
            binaryOperator("bool", "<", "System.UIntPtr", uintMaxValue, "System.UIntPtr", uintMaxValue, "False");

            binaryOperator("bool", "<=", "System.IntPtr", intMinValue, "System.IntPtr", intMinValue, "True");
            binaryOperator("bool", "<=", "System.IntPtr", intMaxValue, "System.IntPtr", intMinValue, "False");
            binaryOperator("bool", "<=", "System.IntPtr", intMaxValue, "System.IntPtr", intMaxValue, "True");
            binaryOperator("bool", "<=", "System.UIntPtr", "0", "System.UIntPtr", "0", "True");
            binaryOperator("bool", "<=", "System.UIntPtr", uintMaxValue, "System.UIntPtr", "0", "False");
            binaryOperator("bool", "<=", "System.UIntPtr", uintMaxValue, "System.UIntPtr", uintMaxValue, "True");

            binaryOperator("bool", ">", "System.IntPtr", intMinValue, "System.IntPtr", intMinValue, "False");
            binaryOperator("bool", ">", "System.IntPtr", intMaxValue, "System.IntPtr", intMinValue, "True");
            binaryOperator("bool", ">", "System.IntPtr", intMaxValue, "System.IntPtr", intMaxValue, "False");
            binaryOperator("bool", ">", "System.UIntPtr", "0", "System.UIntPtr", "0", "False");
            binaryOperator("bool", ">", "System.UIntPtr", uintMaxValue, "System.UIntPtr", "0", "True");
            binaryOperator("bool", ">", "System.UIntPtr", uintMaxValue, "System.UIntPtr", uintMaxValue, "False");

            binaryOperator("bool", ">=", "System.IntPtr", intMinValue, "System.IntPtr", intMinValue, "True");
            binaryOperator("bool", ">=", "System.IntPtr", intMinValue, "System.IntPtr", intMaxValue, "False");
            binaryOperator("bool", ">=", "System.IntPtr", intMaxValue, "System.IntPtr", intMaxValue, "True");
            binaryOperator("bool", ">=", "System.UIntPtr", "0", "System.UIntPtr", "0", "True");
            binaryOperator("bool", ">=", "System.UIntPtr", "0", "System.UIntPtr", uintMaxValue, "False");
            binaryOperator("bool", ">=", "System.UIntPtr", uintMaxValue, "System.UIntPtr", uintMaxValue, "True");

            binaryOperator("bool", "==", "System.IntPtr", intMinValue, "System.IntPtr", intMinValue, "True");
            binaryOperator("bool", "==", "System.IntPtr", intMinValue, "System.IntPtr", intMaxValue, "False");
            binaryOperator("bool", "==", "System.IntPtr", intMaxValue, "System.IntPtr", intMaxValue, "True");
            binaryOperator("bool", "==", "System.UIntPtr", "0", "System.UIntPtr", "0", "True");
            binaryOperator("bool", "==", "System.UIntPtr", "0", "System.UIntPtr", uintMaxValue, "False");
            binaryOperator("bool", "==", "System.UIntPtr", uintMaxValue, "System.UIntPtr", uintMaxValue, "True");

            binaryOperator("bool", "!=", "System.IntPtr", intMinValue, "System.IntPtr", intMinValue, "False");
            binaryOperator("bool", "!=", "System.IntPtr", intMinValue, "System.IntPtr", intMaxValue, "True");
            binaryOperator("bool", "!=", "System.IntPtr", intMaxValue, "System.IntPtr", intMaxValue, "False");
            binaryOperator("bool", "!=", "System.UIntPtr", "0", "System.UIntPtr", "0", "False");
            binaryOperator("bool", "!=", "System.UIntPtr", "0", "System.UIntPtr", uintMaxValue, "True");
            binaryOperator("bool", "!=", "System.UIntPtr", uintMaxValue, "System.UIntPtr", uintMaxValue, "False");

            binaryOperator("System.IntPtr", "<<", "System.IntPtr", intMinValue, "int", "0", intMinValue);
            binaryOperatorNotConstant("System.IntPtr", "<<", "System.IntPtr", intMinValue, "int", "1", IntPtr.Size == 4 ? "0" : "-4294967296");
            binaryOperator("System.IntPtr", "<<", "System.IntPtr", "-1", "int", "31", intMinValue);
            binaryOperatorNotConstant("System.IntPtr", "<<", "System.IntPtr", "-1", "int", "32", IntPtr.Size == 4 ? "-1" : "-4294967296");
            binaryOperator("System.UIntPtr", "<<", "System.UIntPtr", "0", "int", "1", "0");
            binaryOperatorNotConstant("System.UIntPtr", "<<", "System.UIntPtr", uintMaxValue, "int", "1", IntPtr.Size == 4 ? "4294967294" : "8589934590");
            binaryOperator("System.UIntPtr", "<<", "System.UIntPtr", "1", "int", "31", "2147483648");
            binaryOperatorNotConstant("System.UIntPtr", "<<", "System.UIntPtr", "1", "int", "32", IntPtr.Size == 4 ? "1" : "4294967296");

            binaryOperator("System.IntPtr", ">>", "System.IntPtr", intMinValue, "int", "0", intMinValue);
            binaryOperator("System.IntPtr", ">>", "System.IntPtr", intMinValue, "int", "1", "-1073741824");
            binaryOperator("System.IntPtr", ">>", "System.IntPtr", "-1", "int", "31", "-1");
            binaryOperator("System.IntPtr", ">>", "System.IntPtr", "-1", "int", "32", "-1");
            binaryOperator("System.UIntPtr", ">>", "System.UIntPtr", "0", "int", "1", "0");
            binaryOperator("System.UIntPtr", ">>", "System.UIntPtr", uintMaxValue, "int", "1", intMaxValue);
            binaryOperator("System.UIntPtr", ">>", "System.UIntPtr", "1", "int", "31", "0");
            binaryOperator("System.UIntPtr", ">>", "System.UIntPtr", "1", "int", "32", "1");

            binaryOperator("System.IntPtr", "&", "System.IntPtr", intMinValue, "System.IntPtr", "0", "0");
            binaryOperator("System.IntPtr", "&", "System.IntPtr", intMinValue, "System.IntPtr", "-1", intMinValue);
            binaryOperator("System.IntPtr", "&", "System.IntPtr", intMinValue, "System.IntPtr", intMaxValue, "0");
            binaryOperator("System.UIntPtr", "&", "System.UIntPtr", "0", "System.UIntPtr", uintMaxValue, "0");
            binaryOperator("System.UIntPtr", "&", "System.UIntPtr", intMaxValue, "System.UIntPtr", uintMaxValue, intMaxValue);
            binaryOperator("System.UIntPtr", "&", "System.UIntPtr", intMaxValue, "System.UIntPtr", "2147483648", "0");

            binaryOperator("System.IntPtr", "|", "System.IntPtr", intMinValue, "System.IntPtr", "0", intMinValue);
            binaryOperator("System.IntPtr", "|", "System.IntPtr", intMinValue, "System.IntPtr", "-1", "-1");
            binaryOperator("System.IntPtr", "|", "System.IntPtr", intMaxValue, "System.IntPtr", intMaxValue, intMaxValue);
            binaryOperator("System.UIntPtr", "|", "System.UIntPtr", "0", "System.UIntPtr", uintMaxValue, uintMaxValue);
            binaryOperator("System.UIntPtr", "|", "System.UIntPtr", intMaxValue, "System.UIntPtr", intMaxValue, intMaxValue);
            binaryOperator("System.UIntPtr", "|", "System.UIntPtr", intMaxValue, "System.UIntPtr", "2147483648", uintMaxValue);

            binaryOperator("System.IntPtr", "^", "System.IntPtr", intMinValue, "System.IntPtr", "0", intMinValue);
            binaryOperator("System.IntPtr", "^", "System.IntPtr", intMinValue, "System.IntPtr", "-1", intMaxValue);
            binaryOperator("System.IntPtr", "^", "System.IntPtr", intMaxValue, "System.IntPtr", intMaxValue, "0");
            binaryOperator("System.UIntPtr", "^", "System.UIntPtr", "0", "System.UIntPtr", uintMaxValue, uintMaxValue);
            binaryOperator("System.UIntPtr", "^", "System.UIntPtr", intMaxValue, "System.UIntPtr", intMaxValue, "0");
            binaryOperator("System.UIntPtr", "^", "System.UIntPtr", intMaxValue, "System.UIntPtr", "2147483648", uintMaxValue);

            static DiagnosticDescription[] getNoDiagnostics(string opType, string op, string operand) => Array.Empty<DiagnosticDescription>();
            static DiagnosticDescription[] getBadUnaryOpDiagnostics(string opType, string op, string operand) => new[] { Diagnostic(ErrorCode.ERR_BadUnaryOp, operand).WithArguments(op, AsNative(opType)) };

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

            void unaryOperatorCheckedOverflow(string opType, string op, string operand, string expectedResultUnchecked, string expectedResultChecked)
            {
                var declarations = $"const {opType} A = {operand};";
                var expr = $"{op}A";
                constantDeclaration(opType, declarations, expr, null,
                    new[] {
                        Diagnostic(ErrorCode.WRN_CompileTimeCheckedOverflow, expr).WithArguments(AsNative( opType)),
                        Diagnostic(ErrorCode.ERR_NotConstantExpression, expr).WithArguments("Library.F")
                    });
                constantDeclaration(opType, declarations, $"checked({expr})", null,
                    new[] {
                        Diagnostic(ErrorCode.WRN_CompileTimeCheckedOverflow, expr).WithArguments(AsNative(opType)),
                        Diagnostic(ErrorCode.ERR_NotConstantExpression, $"checked({expr})").WithArguments("Library.F")
                    });
                constantDeclaration(opType, declarations, $"unchecked({expr})", null, new[] { Diagnostic(ErrorCode.ERR_NotConstantExpression, $"unchecked({expr})").WithArguments("Library.F") });

                expr = $"{op}({opType})({operand})";
                constantExpression(opType, expr, expectedResultUnchecked, new[] { Diagnostic(ErrorCode.WRN_CompileTimeCheckedOverflow, expr).WithArguments(AsNative(opType)) });
                constantExpression(opType, $"checked({expr})", expectedResultChecked, new[] { Diagnostic(ErrorCode.WRN_CompileTimeCheckedOverflow, expr).WithArguments(AsNative(opType)) });
                constantExpression(opType, $"unchecked({expr})", expectedResultUnchecked, Array.Empty<DiagnosticDescription>());
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

            void binaryOperatorCheckedOverflow(string opType, string op, string leftType, string leftOperand, string rightType, string rightOperand, string expectedResultUnchecked, string expectedResultChecked)
            {
                var declarations = $"const {leftType} A = {leftOperand}; const {rightType} B = {rightOperand};";
                var expr = $"A {op} B";
                constantDeclaration(opType, declarations, expr, null,
                    new[] {
                        Diagnostic(ErrorCode.WRN_CompileTimeCheckedOverflow, expr).WithArguments(AsNative(opType)),
                        Diagnostic(ErrorCode.ERR_NotConstantExpression, expr).WithArguments("Library.F")
                    });
                constantDeclaration(opType, declarations, $"checked({expr})", null,
                    new[] {
                        Diagnostic(ErrorCode.WRN_CompileTimeCheckedOverflow, expr).WithArguments(AsNative(opType)),
                        Diagnostic(ErrorCode.ERR_NotConstantExpression, $"checked({expr})").WithArguments("Library.F")
                    });
                constantDeclaration(opType, declarations, $"unchecked({expr})", null, new[] { Diagnostic(ErrorCode.ERR_NotConstantExpression, $"unchecked({expr})").WithArguments("Library.F") });

                expr = $"(({leftType})({leftOperand})) {op} (({rightType})({rightOperand}))";
                constantExpression(opType, expr, expectedResultUnchecked, new[] { Diagnostic(ErrorCode.WRN_CompileTimeCheckedOverflow, expr).WithArguments(AsNative(opType)) });
                constantExpression(opType, $"checked({expr})", expectedResultChecked, new[] { Diagnostic(ErrorCode.WRN_CompileTimeCheckedOverflow, expr).WithArguments(AsNative(opType)) });
                constantExpression(opType, $"unchecked({expr})", expectedResultUnchecked, Array.Empty<DiagnosticDescription>());
            }

            void binaryOperatorNotConstant(string opType, string op, string leftType, string leftOperand, string rightType, string rightOperand, string expectedResult)
            {
                var declarations = $"const {leftType} A = {leftOperand}; const {rightType} B = {rightOperand};";
                var expr = $"A {op} B";
                constantDeclaration(opType, declarations, expr, null, new[] { Diagnostic(ErrorCode.ERR_NotConstantExpression, expr).WithArguments("Library.F") });
                constantDeclaration(opType, declarations, $"checked({expr})", null, new[] { Diagnostic(ErrorCode.ERR_NotConstantExpression, $"checked({expr})").WithArguments("Library.F") });
                constantDeclaration(opType, declarations, $"unchecked({expr})", null, new[] { Diagnostic(ErrorCode.ERR_NotConstantExpression, $"unchecked({expr})").WithArguments("Library.F") });

                expr = $"(({leftType})({leftOperand})) {op} (({rightType})({rightOperand}))";
                constantExpression(opType, expr, expectedResult, Array.Empty<DiagnosticDescription>());
                constantExpression(opType, $"checked({expr})", expectedResult, Array.Empty<DiagnosticDescription>());
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
                var comp = CreateCompilation(sourceA, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);

                comp.VerifyDiagnostics(expectedDiagnostics);

                if (expectedDiagnostics.Any(d => ErrorFacts.GetSeverity((ErrorCode)d.Code) == DiagnosticSeverity.Error))
                {
                    Assert.Null(expectedResult);
                    return;
                }

                string sourceB =
@"class Program
{
    static void Main()
    {
        System.Console.WriteLine(Library.F);
    }
}";
                var refA = comp.EmitToImageReference();
                comp = CreateCompilation(sourceB, references: new[] { refA }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);

                // Investigating flaky IL verification issue. Tracked by https://github.com/dotnet/roslyn/issues/63782
                CompileAndVerify(comp, verify: new Verification() { Status = VerificationStatus.PassesOrFailFast | VerificationStatus.FailsPEVerify }, expectedOutput: IncludeExpectedOutput(expectedResult));
                Assert.NotNull(expectedResult);
            }

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
                var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);

                if (expectedDiagnostics.Any(d => ErrorFacts.GetSeverity((ErrorCode)d.Code) == DiagnosticSeverity.Error))
                {
                    comp.VerifyDiagnostics(expectedDiagnostics);
                    Assert.Null(expectedResult);
                    return;
                }

                // Investigating flaky IL verification issue. Tracked by https://github.com/dotnet/roslyn/issues/63782
                CompileAndVerify(comp, verify: new Verification() { Status = VerificationStatus.FailsPEVerify | VerificationStatus.PassesOrFailFast }, expectedOutput: IncludeExpectedOutput(expectedResult)).VerifyDiagnostics(expectedDiagnostics);
                Assert.NotNull(expectedResult);
            }
        }

        [Fact]
        public void ConstantFolding_02()
        {
            var source = @"
using System;

class Program
{
    static void Main()
    {
        const UIntPtr x = unchecked(uint.MaxValue + (UIntPtr)42);
        const UIntPtr y = checked(uint.MaxValue + (UIntPtr)42);
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net70);

            comp.VerifyDiagnostics(
                // (8,27): error CS0133: The expression being assigned to 'x' must be constant
                //         const UIntPtr x = unchecked(uint.MaxValue + (UIntPtr)42);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "unchecked(uint.MaxValue + (UIntPtr)42)").WithArguments("x").WithLocation(8, 27),
                // (9,27): error CS0133: The expression being assigned to 'y' must be constant
                //         const UIntPtr y = checked(uint.MaxValue + (UIntPtr)42);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "checked(uint.MaxValue + (UIntPtr)42)").WithArguments("y").WithLocation(9, 27),
                // (9,35): warning CS8973: The operation may overflow 'nuint' at runtime (use 'unchecked' syntax to override)
                //         const UIntPtr y = checked(uint.MaxValue + (UIntPtr)42);
                Diagnostic(ErrorCode.WRN_CompileTimeCheckedOverflow, "uint.MaxValue + (UIntPtr)42").WithArguments("nuint").WithLocation(9, 35)
                );

            source = @"
using System;

class Program
{
    static void Main()
    {
        try
        {
            var y = checked(uint.MaxValue + (UIntPtr)42);
            System.Console.WriteLine(y);
        }
        catch (System.Exception e)
        {
            System.Console.WriteLine(e.GetType());
        }
    }
}";
            comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net70);
            CompileAndVerify(comp, verify: Verification.FailsPEVerify, expectedOutput: IncludeExpectedOutput(IntPtr.Size == 4 ? "System.OverflowException" : "4294967337")).VerifyDiagnostics(
                // (10,29): warning CS8973: The operation may overflow 'nuint' at runtime (use 'unchecked' syntax to override)
                //             var y = checked(uint.MaxValue + (UIntPtr)42);
                Diagnostic(ErrorCode.WRN_CompileTimeCheckedOverflow, "uint.MaxValue + (UIntPtr)42").WithArguments("nuint").WithLocation(10, 29)
                );

            source = @"
using System;

class Program
{
    static void Main()
    {
        var y = unchecked(uint.MaxValue + (UIntPtr)42);
        System.Console.WriteLine(y);
    }
}";
            comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net70);
            CompileAndVerify(comp, verify: Verification.FailsPEVerify, expectedOutput: IncludeExpectedOutput(IntPtr.Size == 4 ? "41" : "4294967337")).VerifyDiagnostics();
        }

        [Fact]
        public void ConstantFolding_03()
        {
            var source = @"
using System;

class Program
{
    static void Main()
    {
        const IntPtr x = unchecked(-(IntPtr)int.MinValue);
        const IntPtr y = checked(-(IntPtr)int.MinValue);
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(
                // (8,26): error CS0133: The expression being assigned to 'x' must be constant
                //         const IntPtr x = unchecked(-(IntPtr)int.MinValue);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "unchecked(-(IntPtr)int.MinValue)").WithArguments("x").WithLocation(8, 26),
                // (9,26): error CS0133: The expression being assigned to 'y' must be constant
                //         const IntPtr y = checked(-(IntPtr)int.MinValue);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "checked(-(IntPtr)int.MinValue)").WithArguments("y").WithLocation(9, 26),
                // (9,34): warning CS8973: The operation may overflow 'nint' at runtime (use 'unchecked' syntax to override)
                //         const IntPtr y = checked(-(IntPtr)int.MinValue);
                Diagnostic(ErrorCode.WRN_CompileTimeCheckedOverflow, "-(IntPtr)int.MinValue").WithArguments("nint").WithLocation(9, 34)
                );

            source = @"
using System;

class Program
{
    static void Main()
    {
        try
        {
            var y = checked(-(IntPtr)int.MinValue);
            System.Console.WriteLine(y);
        }
        catch (System.Exception e)
        {
            System.Console.WriteLine(e.GetType());
        }
    }
}";
            comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net70);
            CompileAndVerify(comp, verify: Verification.FailsPEVerify, expectedOutput: IncludeExpectedOutput(IntPtr.Size == 4 ? "System.OverflowException" : "2147483648")).VerifyDiagnostics(
                // (10,29): warning CS8973: The operation may overflow 'nint' at runtime (use 'unchecked' syntax to override)
                //             var y = checked(-(IntPtr)int.MinValue);
                Diagnostic(ErrorCode.WRN_CompileTimeCheckedOverflow, "-(IntPtr)int.MinValue").WithArguments("nint").WithLocation(10, 29)
                );

            source = @"
using System;

class Program
{
    static void Main()
    {
        var y = unchecked(-(IntPtr)int.MinValue);
        System.Console.WriteLine(y);
    }
}";
            comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net70);
            CompileAndVerify(comp, verify: Verification.FailsPEVerify, expectedOutput: IncludeExpectedOutput(IntPtr.Size == 4 ? "-2147483648" : "2147483648")).VerifyDiagnostics();
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
    static nint NativeIntDivision(IntPtr x, IntPtr y) => unchecked(x / y);
    static nint NativeIntRemainder(IntPtr x, IntPtr y) => unchecked(x % y);
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);

            CompileAndVerify(comp, verify: Verification.FailsPEVerify, expectedOutput: IncludeExpectedOutput(
$@"2147483647
System.OverflowException
0
System.OverflowException
2147483647
{(IntPtr.Size == 4 ? "System.OverflowException" : "2147483648")}
0
{(IntPtr.Size == 4 ? "System.OverflowException" : "0")}"));
        }

        [Fact]
        public void UncheckedLeftShift_01()
        {
            string source =
@"using System;
class Program
{
    static void Main()
    {
        const nint x = 0x7fffffff;
        Report(x << 1);
        Report(LeftShift(x, 1));
    }
    static nint LeftShift(nint x, int y) => unchecked(x << y);
    static void Report(long l) => Console.WriteLine(""{0:x}"", l);
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);

            var expectedValue = IntPtr.Size == 4 ? "fffffffffffffffe" : "fffffffe";
            CompileAndVerify(comp, verify: Verification.FailsPEVerify, expectedOutput: IncludeExpectedOutput(
$@"{expectedValue}
{expectedValue}"));
        }

        [Fact]
        public void UncheckedLeftShift_02()
        {
            string source =
@"using System;
class Program
{
    static void Main()
    {
        const nuint x = 0xffffffff;
        Report(x << 1);
        Report(LeftShift(x, 1));
    }
    static nuint LeftShift(nuint x, int y) => unchecked(x << y);
    static void Report(ulong u) => Console.WriteLine(""{0:x}"", u);
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);

            var expectedValue = IntPtr.Size == 4 ? "fffffffe" : "1fffffffe";
            CompileAndVerify(comp, verify: Verification.FailsPEVerify, expectedOutput: IncludeExpectedOutput(
$@"{expectedValue}
{expectedValue}"));
        }

        [Fact]
        public void ExplicitImplementationReturnTypeDifferences()
        {
            string source =
@"struct S<T>
{
}
interface I
{
    S<nint> F1();
    S<System.IntPtr> F2();
    S<nint> F3();
    S<System.IntPtr> F4();
}
class C : I
{
    S<System.IntPtr> I.F1() => default;
    S<nint> I.F2() => default;
    S<nint> I.F3() => default;
    S<System.IntPtr> I.F4() => default;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);

            comp.VerifyEmitDiagnostics();

            var type = comp.GetTypeByMetadataName("I");
            Assert.Equal("S<nint> I.F1()", type.GetMember("F1").ToTestDisplayString());
            Assert.Equal("S<nint> I.F2()", type.GetMember("F2").ToTestDisplayString());
            Assert.Equal("S<nint> I.F3()", type.GetMember("F3").ToTestDisplayString());
            Assert.Equal("S<nint> I.F4()", type.GetMember("F4").ToTestDisplayString());

            type = comp.GetTypeByMetadataName("C");
            Assert.Equal("S<nint> C.I.F1()", type.GetMember("I.F1").ToTestDisplayString());
            Assert.Equal("S<nint> C.I.F2()", type.GetMember("I.F2").ToTestDisplayString());
            Assert.Equal("S<nint> C.I.F3()", type.GetMember("I.F3").ToTestDisplayString());
            Assert.Equal("S<nint> C.I.F4()", type.GetMember("I.F4").ToTestDisplayString());
        }

        [Fact]
        public void OverrideReturnTypeDifferences()
        {
            string source =
@"class A
{
    public virtual nint[] F1() => null;
    public virtual System.IntPtr[] F2() => null;
    public virtual nint[] F3() => null;
    public virtual System.IntPtr[] F4() => null;
}
class B : A
{
    public override System.IntPtr[] F1() => null;
    public override nint[] F2() => null;
    public override nint[] F3() => null;
    public override System.IntPtr[] F4() => null;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics();

            var type = comp.GetTypeByMetadataName("A");
            Assert.Equal("nint[] A.F1()", type.GetMember("F1").ToTestDisplayString());
            Assert.Equal("nint[] A.F2()", type.GetMember("F2").ToTestDisplayString());
            Assert.Equal("nint[] A.F3()", type.GetMember("F3").ToTestDisplayString());
            Assert.Equal("nint[] A.F4()", type.GetMember("F4").ToTestDisplayString());

            type = comp.GetTypeByMetadataName("B");
            Assert.Equal("nint[] B.F1()", type.GetMember("F1").ToTestDisplayString());
            Assert.Equal("nint[] B.F2()", type.GetMember("F2").ToTestDisplayString());
            Assert.Equal("nint[] B.F3()", type.GetMember("F3").ToTestDisplayString());
            Assert.Equal("nint[] B.F4()", type.GetMember("F4").ToTestDisplayString());
        }

        [Fact]
        public void Int64Conversions()
        {
            convert(fromType: "nint", toType: "ulong", "int.MinValue", "18446744071562067968", "conv.i8", "System.OverflowException", "conv.ovf.u8");
            convert(fromType: "nint", toType: "ulong", "int.MaxValue", "2147483647", "conv.i8", "2147483647", "conv.ovf.u8");
            convert(fromType: "nint", toType: "nuint", "int.MinValue", IntPtr.Size == 4 ? "2147483648" : "18446744071562067968", null, "System.OverflowException", "conv.ovf.u");
            convert(fromType: "nint", toType: "nuint", "int.MaxValue", "2147483647", null, "2147483647", "conv.ovf.u");

            convert(fromType: "nuint", toType: "long", "uint.MaxValue", "4294967295", "conv.u8", "4294967295", "conv.ovf.i8.un");
            convert(fromType: "nuint", toType: "nint", "uint.MaxValue", IntPtr.Size == 4 ? "-1" : "4294967295", null, IntPtr.Size == 4 ? "System.OverflowException" : "4294967295", "conv.ovf.i.un");

            string nintMinValue = IntPtr.Size == 4 ? int.MinValue.ToString() : long.MinValue.ToString();
            string nintMaxValue = IntPtr.Size == 4 ? int.MaxValue.ToString() : long.MaxValue.ToString();
            string nuintMaxValue = IntPtr.Size == 4 ? uint.MaxValue.ToString() : ulong.MaxValue.ToString();

            convert(fromType: "nint", toType: "ulong", nintMinValue, IntPtr.Size == 4 ? "18446744071562067968" : "9223372036854775808", "conv.i8", "System.OverflowException", "conv.ovf.u8");
            convert(fromType: "nint", toType: "ulong", nintMaxValue, IntPtr.Size == 4 ? "2147483647" : "9223372036854775807", "conv.i8", IntPtr.Size == 4 ? "2147483647" : "9223372036854775807", "conv.ovf.u8");
            convert(fromType: "nint", toType: "nuint", nintMinValue, IntPtr.Size == 4 ? "2147483648" : "9223372036854775808", null, "System.OverflowException", "conv.ovf.u");
            convert(fromType: "nint", toType: "nuint", nintMaxValue, IntPtr.Size == 4 ? "2147483647" : "9223372036854775807", null, IntPtr.Size == 4 ? "2147483647" : "9223372036854775807", "conv.ovf.u");

            convert(fromType: "nuint", toType: "long", nuintMaxValue, IntPtr.Size == 4 ? "4294967295" : "-1", "conv.u8", IntPtr.Size == 4 ? "4294967295" : "System.OverflowException", "conv.ovf.i8.un");
            convert(fromType: "nuint", toType: "nint", nuintMaxValue, "-1", null, "System.OverflowException", "conv.ovf.i.un");

            void convert(string fromType, string toType, string fromValue, string toValueUnchecked, string toConvUnchecked, string toValueChecked, string toConvChecked)
            {
                string source =
$@"using System;
class Program
{{
    static {toType} Convert({fromType} value) => ({toType})(value);
    static {toType} ConvertChecked({fromType} value) => checked(({toType})(value));
    static object Execute(Func<object> f)
    {{
        try
        {{
            return f();
        }}
        catch (Exception e)
        {{
            return e.GetType().FullName;
        }}
    }}
    static void Main()
    {{
        {fromType} value = ({fromType})({fromValue});
        Console.WriteLine(Execute(() => Convert(value)));
        Console.WriteLine(Execute(() => ConvertChecked(value)));
    }}
}}";
                var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);

                var verifier = CompileAndVerify(comp, verify: Verification.FailsPEVerify, expectedOutput: IncludeExpectedOutput(
$@"{toValueUnchecked}
{toValueChecked}"));

                verifier.VerifyIL("Program.Convert", toConvUnchecked is null ?
@"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}" :
$@"{{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  {toConvUnchecked}
  IL_0002:  ret
}}");
                verifier.VerifyIL("Program.ConvertChecked",
$@"{{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  {toConvChecked}
  IL_0002:  ret
}}");
            }
        }

        [Theory]
        [InlineData("void*")]
        [InlineData("byte*")]
        [InlineData("delegate*<void>")]
        public void PointerConversions(string pointerType)
        {
            string source =
$@"using System;
unsafe class Program
{{
    static {pointerType} ToPointer1(nint i) => ({pointerType})i;
    static {pointerType} ToPointer2(nuint u) => ({pointerType})u;
    static {pointerType} ToPointer3(nint i) => checked(({pointerType})i);
    static {pointerType} ToPointer4(nuint u) => checked(({pointerType})u);
    static nint FromPointer1({pointerType} p) => (nint)p;
    static nuint FromPointer2({pointerType} p) => (nuint)p;
    static nint FromPointer3({pointerType} p) => checked((nint)p);
    static nuint FromPointer4({pointerType} p) => checked((nuint)p);
    static object Execute(Func<object> f)
    {{
        try
        {{
            return f();
        }}
        catch (Exception e)
        {{
            return e.GetType().FullName;
        }}
    }}
    static void Execute({pointerType} p)
    {{
        Console.WriteLine((int)p);
        Console.WriteLine(Execute(() => FromPointer1(p)));
        Console.WriteLine(Execute(() => FromPointer2(p)));
        Console.WriteLine(Execute(() => FromPointer3(p)));
        Console.WriteLine(Execute(() => FromPointer4(p)));
    }}
    static void Main()
    {{
        Execute(ToPointer1(-42));
        Execute(ToPointer2(42));
        Execute(ToPointer1(int.MinValue));
        Execute(ToPointer2(uint.MaxValue));
        Console.WriteLine(Execute(() => (ulong)ToPointer3(-42)));
        Console.WriteLine(Execute(() => (ulong)ToPointer4(42)));
        Console.WriteLine(Execute(() => (ulong)ToPointer3(int.MinValue)));
        Console.WriteLine(Execute(() => (ulong)ToPointer4(uint.MaxValue)));
    }}
}}";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseExe, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            string expectedOutput =
$@"-42
-42
{(IntPtr.Size == 4 ? "4294967254" : "18446744073709551574")}
System.OverflowException
{(IntPtr.Size == 4 ? "4294967254" : "18446744073709551574")}
42
42
42
42
42
-2147483648
-2147483648
{(IntPtr.Size == 4 ? "2147483648" : "18446744071562067968")}
System.OverflowException
{(IntPtr.Size == 4 ? "2147483648" : "18446744071562067968")}
-1
{(IntPtr.Size == 4 ? "-1" : "4294967295")}
4294967295
{(IntPtr.Size == 4 ? "System.OverflowException" : "4294967295")}
4294967295
System.OverflowException
42
System.OverflowException
4294967295";
            var verifier = CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput(expectedOutput));
            verifier.VerifyIL("Program.ToPointer1",
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
            verifier.VerifyIL("Program.ToPointer2",
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
            verifier.VerifyIL("Program.ToPointer3",
@"{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u
  IL_0002:  ret
}");
            verifier.VerifyIL("Program.ToPointer4",
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
            verifier.VerifyIL("Program.FromPointer1",
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
            verifier.VerifyIL("Program.FromPointer2",
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
            verifier.VerifyIL("Program.FromPointer3",
@"{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i.un
  IL_0002:  ret
}");
            verifier.VerifyIL("Program.FromPointer4",
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("sbyte")]
        [InlineData("byte")]
        [InlineData("short")]
        [InlineData("ushort")]
        [InlineData("int")]
        [InlineData("uint")]
        [InlineData("long")]
        [InlineData("ulong")]
        public void EnumConversions_01(string baseType)
        {
            if (baseType != null) baseType = " : " + baseType;
            string sourceA =
$@"enum E{baseType} {{ A = 0, B = 1 }}";

            string sourceB =
@"#pragma warning disable 219
class Program
{
    static void F1()
    {
        E e;
        const nint i0 = 0;
        const nint i1 = 1;
        e = i0;
        e = i1;
    }
    static void F2()
    {
        E e;
        const nuint u0 = 0;
        const nuint u1 = 1;
        e = u0;
        e = u1;
    }
    static void F3()
    {
        nint i;
        i = default(E);
        i = E.A;
        i = E.B;
    }
    static void F4()
    {
        nuint u;
        u = default(E);
        u = E.A;
        u = E.B;
    }
}";
            var comp = CreateCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(
                // (10,13): error CS0266: Cannot implicitly convert type 'nint' to 'E'. An explicit conversion exists (are you missing a cast?)
                //         e = i1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "i1").WithArguments("nint", "E").WithLocation(10, 13),
                // (18,13): error CS0266: Cannot implicitly convert type 'nuint' to 'E'. An explicit conversion exists (are you missing a cast?)
                //         e = u1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "u1").WithArguments("nuint", "E").WithLocation(18, 13),
                // (23,13): error CS0266: Cannot implicitly convert type 'E' to 'nint'. An explicit conversion exists (are you missing a cast?)
                //         i = default(E);
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "default(E)").WithArguments("E", "nint").WithLocation(23, 13),
                // (24,13): error CS0266: Cannot implicitly convert type 'E' to 'nint'. An explicit conversion exists (are you missing a cast?)
                //         i = E.A;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "E.A").WithArguments("E", "nint").WithLocation(24, 13),
                // (25,13): error CS0266: Cannot implicitly convert type 'E' to 'nint'. An explicit conversion exists (are you missing a cast?)
                //         i = E.B;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "E.B").WithArguments("E", "nint").WithLocation(25, 13),
                // (30,13): error CS0266: Cannot implicitly convert type 'E' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //         u = default(E);
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "default(E)").WithArguments("E", "nuint").WithLocation(30, 13),
                // (31,13): error CS0266: Cannot implicitly convert type 'E' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //         u = E.A;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "E.A").WithArguments("E", "nuint").WithLocation(31, 13),
                // (32,13): error CS0266: Cannot implicitly convert type 'E' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //         u = E.B;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "E.B").WithArguments("E", "nuint").WithLocation(32, 13));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("sbyte")]
        [InlineData("short")]
        [InlineData("int")]
        [InlineData("long")]
        public void EnumConversions_02(string baseType)
        {
            if (baseType != null) baseType = " : " + baseType;
            string sourceA =
$@"enum E{baseType} {{ A = -1, B = 1 }}";

            string sourceB =
@"using static System.Console;
class Program
{
    static E F1(nint i) => (E)i;
    static E F2(nuint u) => (E)u;
    static nint F3(E e) => (nint)e;
    static nuint F4(E e) => (nuint)e;
    static void Main()
    {
        WriteLine(F1(-1));
        WriteLine(F2(1));
        WriteLine(F3(E.A));
        WriteLine(F4(E.B));
    }
}";
            var comp = CreateCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net70);
            CompileAndVerify(comp, verify: Verification.FailsPEVerify, expectedOutput: IncludeExpectedOutput(
@"A
B
-1
1"));
        }

        [Fact]
        public void EnumConversions_03()
        {
            convert(baseType: null, fromType: "E", toType: "nint", "int.MinValue", "-2147483648", "conv.i", "-2147483648", "conv.i");
            convert(baseType: null, fromType: "E", toType: "nint", "int.MaxValue", "2147483647", "conv.i", "2147483647", "conv.i");
            convert(baseType: null, fromType: "E", toType: "nuint", "int.MinValue", IntPtr.Size == 4 ? "2147483648" : "18446744071562067968", "conv.i", "System.OverflowException", "conv.ovf.u");
            convert(baseType: null, fromType: "E", toType: "nuint", "int.MaxValue", "2147483647", "conv.i", "2147483647", "conv.ovf.u");
            convert(baseType: null, fromType: "nint", toType: "E", "int.MinValue", "-2147483648", "conv.i4", "-2147483648", "conv.ovf.i4");
            convert(baseType: null, fromType: "nint", toType: "E", "int.MaxValue", "2147483647", "conv.i4", "2147483647", "conv.ovf.i4");
            convert(baseType: null, fromType: "nuint", toType: "E", "uint.MaxValue", "-1", "conv.i4", "System.OverflowException", "conv.ovf.i4.un");
            convert(baseType: null, fromType: "System.IntPtr", toType: "E", "int.MinValue", "-2147483648", "conv.i4", "-2147483648", "conv.ovf.i4");
            convert(baseType: null, fromType: "System.IntPtr", toType: "E", "int.MaxValue", "2147483647", "conv.i4", "2147483647", "conv.ovf.i4");
            convert(baseType: null, fromType: "System.UIntPtr", toType: "E", "uint.MaxValue", "-1", "conv.i4", "System.OverflowException", "conv.ovf.i4.un");

            convert(baseType: "sbyte", fromType: "E", toType: "nint", "sbyte.MinValue", "-128", "conv.i", "-128", "conv.i");
            convert(baseType: "sbyte", fromType: "E", toType: "nint", "sbyte.MaxValue", "127", "conv.i", "127", "conv.i");
            convert(baseType: "sbyte", fromType: "E", toType: "nuint", "sbyte.MinValue", IntPtr.Size == 4 ? "4294967168" : "18446744073709551488", "conv.i", "System.OverflowException", "conv.ovf.u");
            convert(baseType: "sbyte", fromType: "E", toType: "nuint", "sbyte.MaxValue", "127", "conv.i", "127", "conv.ovf.u");
            convert(baseType: "sbyte", fromType: "nint", toType: "E", "int.MinValue", "A", "conv.i1", "System.OverflowException", "conv.ovf.i1");
            convert(baseType: "sbyte", fromType: "nint", toType: "E", "int.MaxValue", "-1", "conv.i1", "System.OverflowException", "conv.ovf.i1");
            convert(baseType: "sbyte", fromType: "nuint", toType: "E", "uint.MaxValue", "-1", "conv.i1", "System.OverflowException", "conv.ovf.i1.un");
            convert(baseType: "sbyte", fromType: "System.IntPtr", toType: "E", "int.MinValue", "A", "conv.i1", "System.OverflowException", "conv.ovf.i1");
            convert(baseType: "sbyte", fromType: "System.IntPtr", toType: "E", "int.MaxValue", "-1", "conv.i1", "System.OverflowException", "conv.ovf.i1");
            convert(baseType: "sbyte", fromType: "System.UIntPtr", toType: "E", "uint.MaxValue", "-1", "conv.i1", "System.OverflowException", "conv.ovf.i1.un");

            convert(baseType: "byte", fromType: "E", toType: "nint", "byte.MaxValue", "255", "conv.u", "255", "conv.u");
            convert(baseType: "byte", fromType: "E", toType: "nuint", "byte.MaxValue", "255", "conv.u", "255", "conv.u");
            convert(baseType: "byte", fromType: "nint", toType: "E", "int.MinValue", "A", "conv.u1", "System.OverflowException", "conv.ovf.u1");
            convert(baseType: "byte", fromType: "nint", toType: "E", "int.MaxValue", "255", "conv.u1", "System.OverflowException", "conv.ovf.u1");
            convert(baseType: "byte", fromType: "nuint", toType: "E", "uint.MaxValue", "255", "conv.u1", "System.OverflowException", "conv.ovf.u1.un");
            convert(baseType: "byte", fromType: "System.IntPtr", toType: "E", "int.MinValue", "A", "conv.u1", "System.OverflowException", "conv.ovf.u1");
            convert(baseType: "byte", fromType: "System.IntPtr", toType: "E", "int.MaxValue", "255", "conv.u1", "System.OverflowException", "conv.ovf.u1");
            convert(baseType: "byte", fromType: "System.UIntPtr", toType: "E", "uint.MaxValue", "255", "conv.u1", "System.OverflowException", "conv.ovf.u1.un");

            convert(baseType: "short", fromType: "E", toType: "nint", "short.MinValue", "-32768", "conv.i", "-32768", "conv.i");
            convert(baseType: "short", fromType: "E", toType: "nint", "short.MaxValue", "32767", "conv.i", "32767", "conv.i");
            convert(baseType: "short", fromType: "E", toType: "nuint", "short.MinValue", IntPtr.Size == 4 ? "4294934528" : "18446744073709518848", "conv.i", "System.OverflowException", "conv.ovf.u");
            convert(baseType: "short", fromType: "E", toType: "nuint", "short.MaxValue", "32767", "conv.i", "32767", "conv.ovf.u");
            convert(baseType: "short", fromType: "nint", toType: "E", "int.MinValue", "A", "conv.i2", "System.OverflowException", "conv.ovf.i2");
            convert(baseType: "short", fromType: "nint", toType: "E", "int.MaxValue", "-1", "conv.i2", "System.OverflowException", "conv.ovf.i2");
            convert(baseType: "short", fromType: "nuint", toType: "E", "uint.MaxValue", "-1", "conv.i2", "System.OverflowException", "conv.ovf.i2.un");
            convert(baseType: "short", fromType: "System.IntPtr", toType: "E", "int.MinValue", "A", "conv.i2", "System.OverflowException", "conv.ovf.i2");
            convert(baseType: "short", fromType: "System.IntPtr", toType: "E", "int.MaxValue", "-1", "conv.i2", "System.OverflowException", "conv.ovf.i2");
            convert(baseType: "short", fromType: "System.UIntPtr", toType: "E", "uint.MaxValue", "-1", "conv.i2", "System.OverflowException", "conv.ovf.i2.un");

            convert(baseType: "ushort", fromType: "E", toType: "nint", "ushort.MaxValue", "65535", "conv.u", "65535", "conv.u");
            convert(baseType: "ushort", fromType: "E", toType: "nuint", "ushort.MaxValue", "65535", "conv.u", "65535", "conv.u");
            convert(baseType: "ushort", fromType: "nint", toType: "E", "int.MinValue", "A", "conv.u2", "System.OverflowException", "conv.ovf.u2");
            convert(baseType: "ushort", fromType: "nint", toType: "E", "int.MaxValue", "65535", "conv.u2", "System.OverflowException", "conv.ovf.u2");
            convert(baseType: "ushort", fromType: "nuint", toType: "E", "uint.MaxValue", "65535", "conv.u2", "System.OverflowException", "conv.ovf.u2.un");
            convert(baseType: "ushort", fromType: "System.IntPtr", toType: "E", "int.MinValue", "A", "conv.u2", "System.OverflowException", "conv.ovf.u2");
            convert(baseType: "ushort", fromType: "System.IntPtr", toType: "E", "int.MaxValue", "65535", "conv.u2", "System.OverflowException", "conv.ovf.u2");
            convert(baseType: "ushort", fromType: "System.UIntPtr", toType: "E", "uint.MaxValue", "65535", "conv.u2", "System.OverflowException", "conv.ovf.u2.un");

            convert(baseType: "int", fromType: "E", toType: "nint", "int.MinValue", "-2147483648", "conv.i", "-2147483648", "conv.i");
            convert(baseType: "int", fromType: "E", toType: "nint", "int.MaxValue", "2147483647", "conv.i", "2147483647", "conv.i");
            convert(baseType: "int", fromType: "E", toType: "nuint", "int.MinValue", IntPtr.Size == 4 ? "2147483648" : "18446744071562067968", "conv.i", "System.OverflowException", "conv.ovf.u");
            convert(baseType: "int", fromType: "E", toType: "nuint", "int.MaxValue", "2147483647", "conv.i", "2147483647", "conv.ovf.u");
            convert(baseType: "int", fromType: "nint", toType: "E", "int.MinValue", "-2147483648", "conv.i4", "-2147483648", "conv.ovf.i4");
            convert(baseType: "int", fromType: "nint", toType: "E", "int.MaxValue", "2147483647", "conv.i4", "2147483647", "conv.ovf.i4");
            convert(baseType: "int", fromType: "nuint", toType: "E", "uint.MaxValue", "-1", "conv.i4", "System.OverflowException", "conv.ovf.i4.un");
            convert(baseType: "int", fromType: "System.IntPtr", toType: "E", "int.MinValue", "-2147483648", "conv.i4", "-2147483648", "conv.ovf.i4");
            convert(baseType: "int", fromType: "System.IntPtr", toType: "E", "int.MaxValue", "2147483647", "conv.i4", "2147483647", "conv.ovf.i4");
            convert(baseType: "int", fromType: "System.UIntPtr", toType: "E", "uint.MaxValue", "-1", "conv.i4", "System.OverflowException", "conv.ovf.i4.un");

            convert(baseType: "uint", fromType: "E", toType: "nint", "uint.MaxValue", IntPtr.Size == 4 ? "-1" : "4294967295", "conv.u", IntPtr.Size == 4 ? "System.OverflowException" : "4294967295", "conv.ovf.i.un");
            convert(baseType: "uint", fromType: "E", toType: "nuint", "uint.MaxValue", "4294967295", "conv.u", "4294967295", "conv.u");
            convert(baseType: "uint", fromType: "nint", toType: "E", "int.MinValue", "2147483648", "conv.u4", "System.OverflowException", "conv.ovf.u4");
            convert(baseType: "uint", fromType: "nint", toType: "E", "int.MaxValue", "2147483647", "conv.u4", "2147483647", "conv.ovf.u4");
            convert(baseType: "uint", fromType: "nuint", toType: "E", "uint.MaxValue", "4294967295", "conv.u4", "4294967295", "conv.ovf.u4.un");
            convert(baseType: "uint", fromType: "System.IntPtr", toType: "E", "int.MinValue", "2147483648", "conv.u4", "System.OverflowException", "conv.ovf.u4");
            convert(baseType: "uint", fromType: "System.IntPtr", toType: "E", "int.MaxValue", "2147483647", "conv.u4", "2147483647", "conv.ovf.u4");
            convert(baseType: "uint", fromType: "System.UIntPtr", toType: "E", "uint.MaxValue", "4294967295", "conv.u4", "4294967295", "conv.ovf.u4.un");

            convert(baseType: "long", fromType: "E", toType: "nint", "long.MinValue", IntPtr.Size == 4 ? "0" : "-9223372036854775808", "conv.i", IntPtr.Size == 4 ? "System.OverflowException" : "-9223372036854775808", "conv.ovf.i");
            convert(baseType: "long", fromType: "E", toType: "nint", "long.MaxValue", IntPtr.Size == 4 ? "-1" : "9223372036854775807", "conv.i", IntPtr.Size == 4 ? "System.OverflowException" : "9223372036854775807", "conv.ovf.i");
            convert(baseType: "long", fromType: "E", toType: "nuint", "long.MinValue", IntPtr.Size == 4 ? "0" : "9223372036854775808", "conv.u", "System.OverflowException", "conv.ovf.u");
            convert(baseType: "long", fromType: "E", toType: "nuint", "long.MaxValue", IntPtr.Size == 4 ? "4294967295" : "9223372036854775807", "conv.u", IntPtr.Size == 4 ? "System.OverflowException" : "9223372036854775807", "conv.ovf.u");
            convert(baseType: "long", fromType: "nint", toType: "E", "int.MinValue", "-2147483648", "conv.i8", "-2147483648", "conv.i8");
            convert(baseType: "long", fromType: "nint", toType: "E", "int.MaxValue", "2147483647", "conv.i8", "2147483647", "conv.i8");
            convert(baseType: "long", fromType: "nuint", toType: "E", "uint.MaxValue", "4294967295", "conv.u8", "4294967295", "conv.ovf.i8.un");
            convert(baseType: "long", fromType: "System.IntPtr", toType: "E", "int.MinValue", "-2147483648", "conv.i8", "-2147483648", "conv.i8");
            convert(baseType: "long", fromType: "System.IntPtr", toType: "E", "int.MaxValue", "2147483647", "conv.i8", "2147483647", "conv.i8");
            convert(baseType: "long", fromType: "System.UIntPtr", toType: "E", "uint.MaxValue", "4294967295", "conv.u8", "4294967295", "conv.ovf.i8.un");

            convert(baseType: "ulong", fromType: "E", toType: "nint", "ulong.MaxValue", "-1", "conv.i", "System.OverflowException", "conv.ovf.i.un");
            convert(baseType: "ulong", fromType: "E", toType: "nuint", "ulong.MaxValue", IntPtr.Size == 4 ? "4294967295" : "18446744073709551615", "conv.u", IntPtr.Size == 4 ? "System.OverflowException" : "18446744073709551615", "conv.ovf.u.un");
            convert(baseType: "ulong", fromType: "nint", toType: "E", "int.MinValue", "18446744071562067968", "conv.i8", "System.OverflowException", "conv.ovf.u8");
            convert(baseType: "ulong", fromType: "nint", toType: "E", "int.MaxValue", "2147483647", "conv.i8", "2147483647", "conv.ovf.u8");
            convert(baseType: "ulong", fromType: "nuint", toType: "E", "uint.MaxValue", "4294967295", "conv.u8", "4294967295", "conv.u8");
            convert(baseType: "ulong", fromType: "System.IntPtr", toType: "E", "int.MinValue", "18446744071562067968", "conv.i8", "System.OverflowException", "conv.ovf.u8");
            convert(baseType: "ulong", fromType: "System.IntPtr", toType: "E", "int.MaxValue", "2147483647", "conv.i8", "2147483647", "conv.ovf.u8");
            convert(baseType: "ulong", fromType: "System.UIntPtr", toType: "E", "uint.MaxValue", "4294967295", "conv.u8", "4294967295", "conv.u8");

            void convert(string baseType, string fromType, string toType, string fromValue, string toValueUnchecked, string toConvUnchecked, string toValueChecked, string toConvChecked)
            {
                if (baseType != null) baseType = " : " + baseType;
                string source =
$@"using System;
enum E{baseType} {{ A, B }}
class Program
{{
    static {toType} Convert({fromType} value) => ({toType})(value);
    static {toType} ConvertChecked({fromType} value) => checked(({toType})(value));
    static object Execute(Func<object> f)
    {{
        try
        {{
            return f();
        }}
        catch (Exception e)
        {{
            return e.GetType().FullName;
        }}
    }}
    static void Main()
    {{
        {fromType} value = ({fromType})({fromValue});
        Console.WriteLine(Execute(() => Convert(value)));
        Console.WriteLine(Execute(() => ConvertChecked(value)));
    }}
}}";
                var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net70);

                var verifier = CompileAndVerify(comp, verify: Verification.FailsPEVerify, expectedOutput: IncludeExpectedOutput(
$@"{toValueUnchecked}
{toValueChecked}"));

                verifier.VerifyIL("Program.Convert",
$@"{{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  {toConvUnchecked}
  IL_0002:  ret
}}");
                verifier.VerifyIL("Program.ConvertChecked",
$@"{{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  {toConvChecked}
  IL_0002:  ret
}}");
            }
        }

        [Theory]
        [InlineData("nint", "System.IntPtr")]
        [InlineData("nuint", "System.UIntPtr")]
        public void MethodTypeInference(string nativeIntegerType, string underlyingType)
        {
            var source =
$@"interface I<T>
{{
    T P {{ get; }}
}}
unsafe class Program
{{
    static T F0<T>(T x, T y) => x;
    static void F1({nativeIntegerType} x, {underlyingType} y)
    {{
        var z = ({nativeIntegerType})y;
        F0(x, z).ToPointer();
        F0(x, y).ToPointer();
        F0(y, x).ToPointer();
        F0<{nativeIntegerType}>(x, y).
            ToPointer();
        F0<{underlyingType}>(x, y).
            ToPointer();
    }}
    static void F2({nativeIntegerType}[] x, {underlyingType}[] y)
    {{
        var z = ({nativeIntegerType}[])y;
        F0(x, z)[0].ToPointer();
        F0(x, y)[0].ToPointer();
        F0(y, x)[0].ToPointer();
        F0<{nativeIntegerType}[]>(x, y)[0].
            ToPointer();
        F0<{underlyingType}[]>(x, y)[0].
            ToPointer();
    }}
    static void F3(I<{nativeIntegerType}> x, I<{underlyingType}> y)
    {{
        var z = (I<{nativeIntegerType}>)y;
        F0(x, z).P.ToPointer();
        F0(x, y).P.ToPointer();
        F0(y, x).P.ToPointer();
        F0<I<{nativeIntegerType}>>(x, y).P.
            ToPointer();
        F0<I<{underlyingType}>>(x, y).P.
            ToPointer();
    }}
}}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.UnsafeReleaseDll, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DuplicateConstraint()
        {
            var source =
@"interface I<T> { }
class C1<T, U> where U : I<nint>, I<nint> { }
class C2<T, U> where U : I<nint>, I<System.IntPtr> { }
class C3<T, U> where U : I<System.UIntPtr>, I<System.IntPtr>, I<nuint> { }
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(
                // (2,35): error CS0405: Duplicate constraint 'I<nint>' for type parameter 'U'
                // class C1<T, U> where U : I<nint>, I<nint> { }
                Diagnostic(ErrorCode.ERR_DuplicateBound, "I<nint>").WithArguments("I<nint>", "U").WithLocation(2, 35),
                // (3,35): error CS0405: Duplicate constraint 'I<nint>' for type parameter 'U'
                // class C2<T, U> where U : I<nint>, I<System.IntPtr> { }
                Diagnostic(ErrorCode.ERR_DuplicateBound, "I<System.IntPtr>").WithArguments("I<nint>", "U").WithLocation(3, 35),
                // (4,63): error CS0405: Duplicate constraint 'I<nuint>' for type parameter 'U'
                // class C3<T, U> where U : I<System.UIntPtr>, I<System.IntPtr>, I<nuint> { }
                Diagnostic(ErrorCode.ERR_DuplicateBound, "I<nuint>").WithArguments("I<nuint>", "U").WithLocation(4, 63));
        }

        [Fact]
        public void DuplicateInterface_01()
        {
            var source =
@"interface I<T> { }
class C1 : I<nint>, I<nint> { }
class C2 : I<nint>, I<System.IntPtr> { }
class C3 : I<System.UIntPtr>, I<System.IntPtr>, I<nuint> { }
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(
                // (2,21): error CS0528: 'I<nint>' is already listed in interface list
                // class C1 : I<nint>, I<nint> { }
                Diagnostic(ErrorCode.ERR_DuplicateInterfaceInBaseList, "I<nint>").WithArguments("I<nint>").WithLocation(2, 21),
                // (3,21): error CS0528: 'I<nint>' is already listed in interface list
                // class C2 : I<nint>, I<System.IntPtr> { }
                Diagnostic(ErrorCode.ERR_DuplicateInterfaceInBaseList, "I<System.IntPtr>").WithArguments("I<nint>").WithLocation(3, 21),
                // (4,49): error CS0528: 'I<nuint>' is already listed in interface list
                // class C3 : I<System.UIntPtr>, I<System.IntPtr>, I<nuint> { }
                Diagnostic(ErrorCode.ERR_DuplicateInterfaceInBaseList, "I<nuint>").WithArguments("I<nuint>").WithLocation(4, 49));
        }

        [Theory]
        [InlineData("nuint")]
        [InlineData("System.UIntPtr")]
        public void SignedToUnsignedConversions_Implicit(string type)
        {
            string source =
$@"static class NativeInts
{{
    static {type} Implicit1(sbyte x) => x; // 1
    static {type} Implicit2(short x) => x; // 2
    static {type} Implicit3(int x) => x; // 3
    static {type} Implicit4(long x) => x; // 4
    static {type} Implicit5(nint x) => x; // 5
    static {type} Checked1(sbyte x) => checked(x); // 6
    static {type} Checked2(short x) => checked(x); // 7
    static {type} Checked3(int x) => checked(x); // 8
    static {type} Checked4(long x) => checked(x); // 9
    static {type} Checked5(nint x) => checked(x); // 10
}}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(
                // (3,40): error CS0266: Cannot implicitly convert type 'sbyte' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //     static nuint Implicit1(sbyte x) => x; // 1
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("sbyte", "nuint"),
                // (4,40): error CS0266: Cannot implicitly convert type 'short' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //     static nuint Implicit2(short x) => x; // 2
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("short", "nuint"),
                // (5,38): error CS0266: Cannot implicitly convert type 'int' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //     static nuint Implicit3(int x) => x; // 3
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("int", "nuint"),
                // (6,39): error CS0266: Cannot implicitly convert type 'long' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //     static nuint Implicit4(long x) => x; // 4
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("long", "nuint"),
                // (7,39): error CS0266: Cannot implicitly convert type 'nint' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //     static nuint Implicit5(nint x) => x; // 5
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("nint", "nuint"),
                // (8,47): error CS0266: Cannot implicitly convert type 'sbyte' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //     static nuint Checked1(sbyte x) => checked(x); // 6
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("sbyte", "nuint"),
                // (9,47): error CS0266: Cannot implicitly convert type 'short' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //     static nuint Checked2(short x) => checked(x); // 7
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("short", "nuint"),
                // (10,45): error CS0266: Cannot implicitly convert type 'int' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //     static nuint Checked3(int x) => checked(x); // 8
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("int", "nuint"),
                // (11,46): error CS0266: Cannot implicitly convert type 'long' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //     static nuint Checked4(long x) => checked(x); // 9
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("long", "nuint"),
                // (12,46): error CS0266: Cannot implicitly convert type 'nint' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //     static nuint Checked5(nint x) => checked(x); // 10
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("nint", "nuint"));
        }

        [Theory]
        [InlineData("nuint")]
        [InlineData("System.UIntPtr")]
        public void SignedToUnsignedConversions_Explicit(string type)
        {
            string source =
$@"static class NativeInts
{{
    static {type} Explicit1(sbyte x) => ({type})x;
    static {type} Explicit2(short x) => ({type})x;
    static {type} Explicit3(int x) => ({type})x;
    static {type} Explicit4(long x) => ({type})x;
    static {type} Explicit5(nint x) => ({type})x;
    static {type} Checked1(sbyte x) => checked(({type})x);
    static {type} Checked2(short x) => checked(({type})x);
    static {type} Checked3(int x) => checked(({type})x);
    static {type} Checked4(long x) => checked(({type})x);
    static {type} Checked5(nint x) => checked(({type})x);
}}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            var verifier = CompileAndVerify(comp, verify: Verification.FailsPEVerify);
            string expectedExplicitILNop =
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}";
            string expectedExplicitILA =
@"{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i
  IL_0002:  ret
}";
            string expectedExplicitILB =
@"{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u
  IL_0002:  ret
}";
            string expectedCheckedIL =
@"{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u
  IL_0002:  ret
}";
            verifier.VerifyIL("NativeInts.Explicit1", expectedExplicitILA);
            verifier.VerifyIL("NativeInts.Explicit2", expectedExplicitILA);
            verifier.VerifyIL("NativeInts.Explicit3", expectedExplicitILA);
            verifier.VerifyIL("NativeInts.Explicit4", expectedExplicitILB);
            verifier.VerifyIL("NativeInts.Explicit5", expectedExplicitILNop);
            verifier.VerifyIL("NativeInts.Checked1", expectedCheckedIL);
            verifier.VerifyIL("NativeInts.Checked2", expectedCheckedIL);
            verifier.VerifyIL("NativeInts.Checked3", expectedCheckedIL);
            verifier.VerifyIL("NativeInts.Checked4", expectedCheckedIL);
            verifier.VerifyIL("NativeInts.Checked5", expectedCheckedIL);
        }

        [Fact]
        public void StandardConversions()
        {
            // Note: A standard explicit conversion is derived from opposite standard implicit conversion

            // type to nint
            verify(sourceType: "object", destType: "nint", isExplicit: true);
            verify(sourceType: "string", destType: "nint", noConversion: true);
            verify(sourceType: "void*", destType: "nint", noConversion: true);
            verify(sourceType: "delegate*<void>", destType: "nint", noConversion: true);
            verify(sourceType: "E", destType: "nint", noConversion: true);
            verify(sourceType: "bool", destType: "nint", noConversion: true);
            verify(sourceType: "sbyte", destType: "nint");
            verify(sourceType: "byte", destType: "nint");
            verify(sourceType: "short", destType: "nint");
            verify(sourceType: "ushort", destType: "nint");
            verify(sourceType: "int", destType: "nint");
            verify(sourceType: "uint", destType: "nint", noConversion: true);
            verify(sourceType: "long", destType: "nint", isExplicit: true);
            verify(sourceType: "ulong", destType: "nint", noConversion: true);
            verify(sourceType: "char", destType: "nint");
            verify(sourceType: "float", destType: "nint", isExplicit: true);
            verify(sourceType: "double", destType: "nint", isExplicit: true);
            verify(sourceType: "decimal", destType: "nint", isExplicit: true);
            verify(sourceType: "nint", destType: "nint");
            verify(sourceType: "nuint", destType: "nint", noConversion: true);
            verify(sourceType: "System.IntPtr", destType: "nint");
            verify(sourceType: "System.UIntPtr", destType: "nint", noConversion: true);

            // nint to type
            verify(sourceType: "nint", destType: "string", noConversion: true);
            verify(sourceType: "nint", destType: "void*", noConversion: true);
            verify(sourceType: "nint", destType: "delegate*<void>", noConversion: true);
            verify(sourceType: "nint", destType: "E", noConversion: true);
            verify(sourceType: "nint", destType: "bool", noConversion: true);
            verify(sourceType: "nint", destType: "sbyte", isExplicit: true);
            verify(sourceType: "nint", destType: "byte", isExplicit: true);
            verify(sourceType: "nint", destType: "short", isExplicit: true);
            verify(sourceType: "nint", destType: "ushort", isExplicit: true);
            verify(sourceType: "nint", destType: "int", isExplicit: true);
            verify(sourceType: "nint", destType: "uint", noConversion: true);
            verify(sourceType: "nint", destType: "long");
            verify(sourceType: "nint", destType: "ulong", noConversion: true);
            verify(sourceType: "nint", destType: "char", isExplicit: true);
            verify(sourceType: "nint", destType: "float");
            verify(sourceType: "nint", destType: "double");
            verify(sourceType: "nint", destType: "decimal");
            verify(sourceType: "nint", destType: "nint");
            verify(sourceType: "nint", destType: "nuint", noConversion: true);
            verify(sourceType: "nint", destType: "System.IntPtr");
            verify(sourceType: "nint", destType: "System.UIntPtr", noConversion: true);

            // type to nuint
            verify(sourceType: "object", destType: "nuint", isExplicit: true);
            verify(sourceType: "string", destType: "nuint", noConversion: true);
            verify(sourceType: "void*", destType: "nuint", noConversion: true);
            verify(sourceType: "delegate*<void>", destType: "nuint", noConversion: true);
            verify(sourceType: "E", destType: "nuint", noConversion: true);
            verify(sourceType: "bool", destType: "nuint", noConversion: true);
            verify(sourceType: "sbyte", destType: "nuint", noConversion: true);
            verify(sourceType: "byte", destType: "nuint");
            verify(sourceType: "short", destType: "nuint", noConversion: true);
            verify(sourceType: "ushort", destType: "nuint");
            verify(sourceType: "int", destType: "nuint", noConversion: true);
            verify(sourceType: "uint", destType: "nuint");
            verify(sourceType: "long", destType: "nuint", noConversion: true);
            verify(sourceType: "ulong", destType: "nuint", isExplicit: true);
            verify(sourceType: "char", destType: "nuint");
            verify(sourceType: "float", destType: "nuint", isExplicit: true);
            verify(sourceType: "double", destType: "nuint", isExplicit: true);
            verify(sourceType: "decimal", destType: "nuint", isExplicit: true);
            verify(sourceType: "nint", destType: "nuint", noConversion: true);
            verify(sourceType: "nuint", destType: "nuint");
            verify(sourceType: "System.IntPtr", destType: "nuint", noConversion: true);
            verify(sourceType: "System.UIntPtr", destType: "nuint");

            // nuint to type
            verify(sourceType: "nuint", destType: "string", noConversion: true);
            verify(sourceType: "nuint", destType: "void*", noConversion: true);
            verify(sourceType: "nuint", destType: "delegate*<void>", noConversion: true);
            verify(sourceType: "nuint", destType: "E", noConversion: true);
            verify(sourceType: "nuint", destType: "bool", noConversion: true);
            verify(sourceType: "nuint", destType: "sbyte", noConversion: true);
            verify(sourceType: "nuint", destType: "byte", isExplicit: true);
            verify(sourceType: "nuint", destType: "short", noConversion: true);
            verify(sourceType: "nuint", destType: "ushort", isExplicit: true);
            verify(sourceType: "nuint", destType: "int", noConversion: true);
            verify(sourceType: "nuint", destType: "uint", isExplicit: true);
            verify(sourceType: "nuint", destType: "long", noConversion: true);
            verify(sourceType: "nuint", destType: "ulong");
            verify(sourceType: "nuint", destType: "char", isExplicit: true);
            verify(sourceType: "nuint", destType: "float");
            verify(sourceType: "nuint", destType: "double");
            verify(sourceType: "nuint", destType: "decimal");
            verify(sourceType: "nuint", destType: "nint", noConversion: true);
            verify(sourceType: "nuint", destType: "nuint");
            verify(sourceType: "nuint", destType: "System.IntPtr", noConversion: true);
            verify(sourceType: "nuint", destType: "System.UIntPtr");

            // type to IntPtr
            verify(sourceType: "object", destType: "System.IntPtr", isExplicit: true);
            verify(sourceType: "string", destType: "System.IntPtr", noConversion: true);
            verify(sourceType: "void*", destType: "System.IntPtr", noConversion: true);
            verify(sourceType: "delegate*<void>", destType: "System.IntPtr", noConversion: true);
            verify(sourceType: "E", destType: "System.IntPtr", noConversion: true);
            verify(sourceType: "bool", destType: "System.IntPtr", noConversion: true);
            verify(sourceType: "sbyte", destType: "System.IntPtr");
            verify(sourceType: "byte", destType: "System.IntPtr");
            verify(sourceType: "short", destType: "System.IntPtr");
            verify(sourceType: "ushort", destType: "System.IntPtr");
            verify(sourceType: "int", destType: "System.IntPtr");
            verify(sourceType: "uint", destType: "System.IntPtr", noConversion: true);
            verify(sourceType: "long", destType: "System.IntPtr", isExplicit: true);
            verify(sourceType: "ulong", destType: "System.IntPtr", noConversion: true);
            verify(sourceType: "char", destType: "System.IntPtr");
            verify(sourceType: "float", destType: "System.IntPtr", isExplicit: true);
            verify(sourceType: "double", destType: "System.IntPtr", isExplicit: true);
            verify(sourceType: "decimal", destType: "System.IntPtr", isExplicit: true);
            verify(sourceType: "nint", destType: "System.IntPtr");
            verify(sourceType: "nuint", destType: "System.IntPtr", noConversion: true);
            verify(sourceType: "System.IntPtr", destType: "System.IntPtr");
            verify(sourceType: "System.UIntPtr", destType: "System.IntPtr", noConversion: true);

            // IntPtr to type
            verify(sourceType: "System.IntPtr", destType: "string", noConversion: true);
            verify(sourceType: "System.IntPtr", destType: "void*", noConversion: true);
            verify(sourceType: "System.IntPtr", destType: "delegate*<void>", noConversion: true);
            verify(sourceType: "System.IntPtr", destType: "E", noConversion: true);
            verify(sourceType: "System.IntPtr", destType: "bool", noConversion: true);
            verify(sourceType: "System.IntPtr", destType: "sbyte", isExplicit: true);
            verify(sourceType: "System.IntPtr", destType: "byte", isExplicit: true);
            verify(sourceType: "System.IntPtr", destType: "short", isExplicit: true);
            verify(sourceType: "System.IntPtr", destType: "ushort", isExplicit: true);
            verify(sourceType: "System.IntPtr", destType: "int", isExplicit: true);
            verify(sourceType: "System.IntPtr", destType: "uint", noConversion: true);
            verify(sourceType: "System.IntPtr", destType: "long");
            verify(sourceType: "System.IntPtr", destType: "ulong", noConversion: true);
            verify(sourceType: "System.IntPtr", destType: "char", isExplicit: true);
            verify(sourceType: "System.IntPtr", destType: "float");
            verify(sourceType: "System.IntPtr", destType: "double");
            verify(sourceType: "System.IntPtr", destType: "decimal");
            verify(sourceType: "System.IntPtr", destType: "nint");
            verify(sourceType: "System.IntPtr", destType: "nuint", noConversion: true);
            verify(sourceType: "System.IntPtr", destType: "System.IntPtr");
            verify(sourceType: "System.IntPtr", destType: "System.UIntPtr", noConversion: true);

            // type to UIntPtr
            verify(sourceType: "object", destType: "System.UIntPtr", isExplicit: true);
            verify(sourceType: "string", destType: "System.UIntPtr", noConversion: true);
            verify(sourceType: "void*", destType: "System.UIntPtr", noConversion: true);
            verify(sourceType: "delegate*<void>", destType: "System.UIntPtr", noConversion: true);
            verify(sourceType: "E", destType: "System.UIntPtr", noConversion: true);
            verify(sourceType: "bool", destType: "System.UIntPtr", noConversion: true);
            verify(sourceType: "sbyte", destType: "System.UIntPtr", noConversion: true);
            verify(sourceType: "byte", destType: "System.UIntPtr");
            verify(sourceType: "short", destType: "System.UIntPtr", noConversion: true);
            verify(sourceType: "ushort", destType: "System.UIntPtr");
            verify(sourceType: "int", destType: "System.UIntPtr", noConversion: true);
            verify(sourceType: "uint", destType: "System.UIntPtr");
            verify(sourceType: "long", destType: "System.UIntPtr", noConversion: true);
            verify(sourceType: "ulong", destType: "System.UIntPtr", isExplicit: true);
            verify(sourceType: "char", destType: "System.UIntPtr");
            verify(sourceType: "float", destType: "System.UIntPtr", isExplicit: true);
            verify(sourceType: "double", destType: "System.UIntPtr", isExplicit: true);
            verify(sourceType: "decimal", destType: "System.UIntPtr", isExplicit: true);
            verify(sourceType: "nint", destType: "System.UIntPtr", noConversion: true);
            verify(sourceType: "nuint", destType: "System.UIntPtr");
            verify(sourceType: "System.IntPtr", destType: "System.UIntPtr", noConversion: true);
            verify(sourceType: "System.UIntPtr", destType: "System.UIntPtr");

            // UIntPtr to type
            verify(sourceType: "System.UIntPtr", destType: "string", noConversion: true);
            verify(sourceType: "System.UIntPtr", destType: "void*", noConversion: true);
            verify(sourceType: "System.UIntPtr", destType: "delegate*<void>", noConversion: true);
            verify(sourceType: "System.UIntPtr", destType: "E", noConversion: true);
            verify(sourceType: "System.UIntPtr", destType: "bool", noConversion: true);
            verify(sourceType: "System.UIntPtr", destType: "sbyte", noConversion: true);
            verify(sourceType: "System.UIntPtr", destType: "byte", isExplicit: true);
            verify(sourceType: "System.UIntPtr", destType: "short", noConversion: true);
            verify(sourceType: "System.UIntPtr", destType: "ushort", isExplicit: true);
            verify(sourceType: "System.UIntPtr", destType: "int", noConversion: true);
            verify(sourceType: "System.UIntPtr", destType: "uint", isExplicit: true);
            verify(sourceType: "System.UIntPtr", destType: "long", noConversion: true);
            verify(sourceType: "System.UIntPtr", destType: "ulong");
            verify(sourceType: "System.UIntPtr", destType: "char", isExplicit: true);
            verify(sourceType: "System.UIntPtr", destType: "float");
            verify(sourceType: "System.UIntPtr", destType: "double");
            verify(sourceType: "System.UIntPtr", destType: "decimal");
            verify(sourceType: "System.UIntPtr", destType: "nint", noConversion: true);
            verify(sourceType: "System.UIntPtr", destType: "nuint");
            verify(sourceType: "System.UIntPtr", destType: "System.IntPtr", noConversion: true);
            verify(sourceType: "System.UIntPtr", destType: "System.UIntPtr");

            void verify(string sourceType, string destType, bool noConversion = false, bool isExplicit = false)
            {
                var source = $$"""
unsafe class FinalType
{
    FinalType M({{sourceType}} x) => x;
    FinalType M2({{sourceType}} x) => (FinalType)x;
    public static implicit operator FinalType({{destType}} i) => throw null;
}
enum E { }
""";
                var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net70);

                if (noConversion)
                {
                    comp.VerifyDiagnostics(
                        // (3,30): error CS0029: Cannot implicitly convert type 'sourceType' to 'FinalType'
                        //     FinalType M(sourceType x) => x;
                        Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments(AsNative(sourceType), "FinalType"),
                        // (4,31): error CS0030: Cannot convert type 'sourceType' to 'FinalType'
                        //     FinalType M2(sourceType x) => (FinalType)x;
                        Diagnostic(ErrorCode.ERR_NoExplicitConv, "(FinalType)x").WithArguments(AsNative(sourceType), "FinalType")
                        );
                }
                else if (isExplicit)
                {
                    comp.VerifyDiagnostics(
                        // (3,30): error CS0266: Cannot implicitly convert type 'sourceType' to 'FinalType'. An explicit conversion exists (are you missing a cast?)
                        //     FinalType M(sourceType x) => x;
                        Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments(AsNative(sourceType), "FinalType")
                        );
                }
                else
                {
                    comp.VerifyDiagnostics();
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void VerifyUnifiedSymbols(bool useCSharp11)
        {
            var corlib_cs = RuntimeFeature_NumericIntPtr + @"
namespace System
{
    public struct IntPtr { }
    public struct UIntPtr { }

    public class Object { }
    public class String { }
    public class ValueType { }
    public struct Void { }
}
";

            var source = @"
interface I
{
    void M(nint x1, System.IntPtr x2, nuint x3, System.UIntPtr x4);
}
";
            var parseOptions = (useCSharp11 ? TestOptions.Regular11 : TestOptions.Regular10).WithNoRefSafetyRulesAttribute();

            var comp = CreateEmptyCompilation(new[] { source, corlib_cs }, parseOptions: parseOptions);
            verify(comp);

            var corlib = CreateEmptyCompilation(corlib_cs, parseOptions: parseOptions);
            corlib.VerifyDiagnostics();

            comp = CreateEmptyCompilation(source, references: new[] { corlib.ToMetadataReference() }, parseOptions: parseOptions);
            verify(comp);

            comp = CreateEmptyCompilation(source, references: new[] { corlib.EmitToImageReference() }, parseOptions: parseOptions);
            verify(comp);

            void verify(CSharpCompilation comp)
            {
                comp.VerifyDiagnostics();

                var emitOptions = new EmitOptions(runtimeMetadataVersion: "v5.1", debugInformationFormat: DebugInformationFormat.PortablePdb);
                comp.VerifyEmitDiagnostics(emitOptions);

                var method = (MethodSymbol)comp.GlobalNamespace.GetMember("I.M");

                var nintType = method.Parameters[0].Type;
                verifyIntPtr(nintType);
                verifyCommon(nintType);

                var intPtrType = method.Parameters[1].Type;
                verifyIntPtr(intPtrType);
                verifyCommon(nintType);
                Assert.Same(nintType, intPtrType);
                Assert.Same(nintType, comp.GetSpecialType(SpecialType.System_IntPtr));
                var fromAPI = comp.CreateNativeIntegerTypeSymbol(signed: true);
                Assert.Same(nintType, fromAPI);
                Assert.False(fromAPI.IsNativeIntegerWrapperType);

                var nuintType = method.Parameters[2].Type;
                verifyUIntPtr(nuintType);
                verifyCommon(nintType);

                var uintPtrType = method.Parameters[3].Type;
                verifyUIntPtr(uintPtrType);
                verifyCommon(nintType);
                Assert.Same(nuintType, uintPtrType);
                Assert.Same(nuintType, comp.GetSpecialType(SpecialType.System_UIntPtr));
                fromAPI = comp.CreateNativeIntegerTypeSymbol(signed: false);
                Assert.Same(nuintType, fromAPI);
                Assert.False(fromAPI.IsNativeIntegerWrapperType);

                VerifyNoNativeIntegerAttributeEmitted(comp);
            }

            static void verifyIntPtr(TypeSymbol type)
            {
                Assert.Equal(SpecialType.System_IntPtr, type.SpecialType);
                Assert.Equal("nint", type.ToTestDisplayString());
                Assert.Equal("nint", type.ToDisplayString(SymbolDisplayFormat.TestFormat));
                Assert.Equal("System.IntPtr", type.ToDisplayString(SymbolDisplayFormat.TestFormat.WithCompilerInternalOptions(SymbolDisplayCompilerInternalOptions.UseNativeIntegerUnderlyingType)));
                Assert.Equal("nint", type.ToDisplayString(SymbolDisplayFormat.TestFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes)));
            }

            static void verifyUIntPtr(TypeSymbol type)
            {
                Assert.Equal(SpecialType.System_UIntPtr, type.SpecialType);
                Assert.Equal("nuint", type.ToTestDisplayString());
                Assert.Equal("nuint", type.ToDisplayString(SymbolDisplayFormat.TestFormat));
                Assert.Equal("System.UIntPtr", type.ToDisplayString(SymbolDisplayFormat.TestFormat.WithCompilerInternalOptions(SymbolDisplayCompilerInternalOptions.UseNativeIntegerUnderlyingType)));
                Assert.Equal("nuint", type.ToDisplayString(SymbolDisplayFormat.TestFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes)));
            }

            static void verifyCommon(TypeSymbol type)
            {
                Assert.True(type.IsDefinition);
                Assert.False(type.IsNativeIntegerWrapperType);
                Assert.Null(((NamedTypeSymbol)type).NativeIntegerUnderlyingType);
            }
        }

        [Fact]
        public void NoAttributesEmitted()
        {
            var source = @"
interface I<T> { }

class C1 : I<nint> { }
class C2 : I<System.IntPtr> { }
class C3 : I<nuint> { }
class C4 : I<System.UIntPtr> { }

class Base<T> { }

class D1 : Base<nint> { }
class D2 : Base<System.IntPtr> { }
class D3 : Base<nuint> { }
class D4 : Base<System.UIntPtr> { }

class Misc
{
    Misc(nint x1, System.IntPtr x2, nuint x3, System.UIntPtr x4) { }

    nint M1() => throw null;
    System.IntPtr M2() => throw null;
    nuint M3() => throw null;
    System.UIntPtr M4() => throw null;

    void Lambdas()
    {
        var lambda1 = nint () => throw null;
        var lambda2 = System.IntPtr () => throw null;
        var lambda3 = nuint () => throw null;
        var lambda4 = System.UIntPtr () => throw null;

        var lambda5 = void (nint x1, System.IntPtr x2, nuint x3, System.UIntPtr x4) => throw null;
    }

    void LocalFunctions()
    {
        local1();
        local2();
        local3();
        local4();
        local5(0, 0, 0, 0);

        nint local1() => throw null;
        System.IntPtr local2() => throw null;
        nuint local3() => throw null;
        System.UIntPtr local4() => throw null;

        void local5(nint x1, System.IntPtr x2, nuint x3, System.UIntPtr x4) { }
    }
}

delegate nint Delegate1(nint x);
delegate System.IntPtr Delegate2(System.IntPtr x);
delegate nuint Delegate3(nuint x);
delegate System.UIntPtr Delegate4(System.UIntPtr x);

class Operators
{
    public static implicit operator Operators(nint x) => throw null;
    public static implicit operator Operators(nuint x) => throw null;
}
class Operators2
{
    public static implicit operator Operators2(System.IntPtr x) => throw null;
    public static implicit operator Operators2(System.UIntPtr x) => throw null;
}

class Constraints<T1, T2, T3, T4>
    where T1 : I<nint>
    where T2 : I<System.IntPtr>
    where T3 : I<nuint>
    where T4 : I<System.UIntPtr>
{
}

class Properties
{
    nint Property1 { get; set; }
    System.IntPtr Property2 { get; set; }
    nuint Property3 { get; set; }
    System.UIntPtr Property4 { get; set; }
}

class Indexers
{
    nint this[nint x] => throw null;
    nuint this[nuint x] => throw null;
}
class Indexers2
{
    System.IntPtr Property2 => throw null;
    System.UIntPtr Property4 => throw null;
}
";
            var comp = CreateCompilation(new[] { source }, targetFramework: TargetFramework.Net70);

            comp.VerifyDiagnostics();
            VerifyNoNativeIntegerAttributeEmitted(comp);
        }

        [Fact]
        public void UserDefinedConversionOnSystemIntPtr()
        {
            var corlib_cs = RuntimeFeature_NumericIntPtr + @"
namespace System
{
    public struct IntPtr
    {
        public static implicit operator IntPtr(String s) { return 0; }
    }
    public struct UIntPtr
    {
        public static implicit operator UIntPtr(String s) { return 0; }
    }

    public class Object { }
    public class String { }
    public class ValueType { }
    public struct Void { }
    public struct Int32 { }

    public class Exception { }
}
";
            var source = @"
class C
{
    nint M1(string s)
    {
        return s;
    }
    nuint M2(string s)
    {
        return s;
    }
}
";
            var comp = CreateEmptyCompilation(new[] { source, corlib_cs });
            comp.VerifyDiagnostics();
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var returnStatements = tree.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>().ToArray();
            Assert.Equal("nint nint.op_Implicit(System.String s)", model.GetConversion(returnStatements[0].Expression).Method.ToTestDisplayString());
            Assert.Equal("nuint nuint.op_Implicit(System.String s)", model.GetConversion(returnStatements[1].Expression).Method.ToTestDisplayString());
        }

        [Fact]
        public void GenericAttribute_AttributeDependentTypes()
        {
            var source = @"
#nullable enable
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class Attr<T> : Attribute { }

[Attr<nint>]
[Attr<D<nint>>]
[Attr<nuint>]
[Attr<D<nuint>>]
class C1 { }

[Attr<IntPtr>]
[Attr<D<IntPtr>>]
[Attr<UIntPtr>]
[Attr<D<UIntPtr>>]
class C2 { }

class D<T> { }
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void UnmanagedConstraint()
        {
            var source = @"
class C<T> where T : unmanaged { }
class D : C<System.IntPtr> { }
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);

            comp.VerifyDiagnostics();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RecordStructIntPtr(bool useCompilationReference)
        {
            var sourceA =
@"namespace System
{
    public class Object
    {
        public virtual bool Equals(object other) => default;
        public virtual int GetHashCode() => default;
        public virtual string ToString() => default;
    }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public struct Char { }
    public class Exception { }
    public interface IEquatable<T>
    {
        bool Equals(T other);
    }
    public class Attribute { }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets validOn) { }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
    }
    public class Enum { }
    public enum AttributeTargets { }
    public record struct IntPtr
    {
        public static readonly IntPtr Zero;
        public static int Size => 0;
        public static IntPtr MaxValue => default;
        public static IntPtr MinValue => default;
        public static IntPtr Add(IntPtr ptr, int offset) => default;
        public static IntPtr Subtract(IntPtr ptr, int offset) => default;
        public static IntPtr Parse(string s) => default;
        public static bool TryParse(string s, out IntPtr value)
        {
            value = default;
            return false;
        }
    }
    public record struct UIntPtr
    {
        public static readonly UIntPtr Zero;
        public static int Size => 0;
        public static UIntPtr MaxValue => default;
        public static UIntPtr MinValue => default;
        public static UIntPtr Add(UIntPtr ptr, int offset) => default;
        public static UIntPtr Subtract(UIntPtr ptr, int offset) => default;
        public static UIntPtr Parse(string s) => default;
        public static bool TryParse(string s, out UIntPtr value)
        {
            value = default;
            return false;
        }
    }
}
namespace System.Text
{
    public class StringBuilder
    {
        public StringBuilder Append(string s) => null;
        public StringBuilder Append(char c) => null;
        public StringBuilder Append(object o) => null;
    }
}
" + RuntimeFeature_NumericIntPtr;

            var comp = CreateEmptyCompilation(sourceA);
            comp.VerifyDiagnostics();
            var refA = AsReference(comp, useCompilationReference);

            var sourceB =
@"class Program
{
    static nint F1() => throw null;
    static nuint F2() => throw null;
    static System.IntPtr F3() => throw null;
    static System.UIntPtr F4() => throw null;
}";

            comp = CreateEmptyCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var methods = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().ToArray();
            Assert.Equal(4, methods.Count());
            foreach (var method in methods)
            {
                var returnType = model.GetDeclaredSymbol(method).ReturnType;
                // record structs are erased in metadata
                Assert.Equal(useCompilationReference || returnType.IsReferenceType, returnType.IsRecord);
            }
        }

        [Fact]
        public void ArrayAccess()
        {
            var source = @"
var array = new[] { 1, 2, 3, 4 };
nint x1 = 0;
System.IntPtr x2 = 1;
nuint x3 = 2;
System.UIntPtr x4 = 3;

System.Console.Write($""{array[x1]}, {array[x2]}, {array[x3]}, {array[x4]}"");
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);

            CompileAndVerify(comp, expectedOutput: IncludeExpectedOutput("1, 2, 3, 4"), verify: Verification.FailsPEVerify);
        }

        [Fact]
        public void NativeIntegerAttributeFromMetadata()
        {
            var source = @"
public class C
{
    public nint M() { throw null; }
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            var image = comp.EmitToImageReference();

            var comp2 = CreateCompilation(source, references: new[] { image }, targetFramework: TargetFramework.Net70);

            CompileAndVerify(comp2, sourceSymbolValidator: verify, symbolValidator: verify, verify: Verification.FailsPEVerify);

            static void verify(ModuleSymbol module)
            {
                var m = (MethodSymbol)module.GlobalNamespace.GetMember("C.M");
                Assert.Equal("nint C.M()", m.ToTestDisplayString());
                Assert.False(m.ReturnType.IsNativeIntegerWrapperType);
            }
        }

        [Fact]
        public void PatternExplainer()
        {
            var source = @"
nint x = 0;
_ = x switch // 1
{
    <= int.MaxValue => 0
};

_ = x switch // 2
{
    >= int.MinValue => 0
};

nuint y = 0;
_ = y switch // 3
{
    <= uint.MaxValue => 0
};

";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(
                // (3,7): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '> (nint)int.MaxValue' is not covered.
                // _ = x switch // 1
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("> (nint)int.MaxValue").WithLocation(3, 7),
                // (8,7): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '< (nint)int.MinValue' is not covered.
                // _ = x switch // 2
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("< (nint)int.MinValue").WithLocation(8, 7),
                // (14,7): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '> (nuint)uint.MaxValue' is not covered.
                // _ = y switch // 3
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("> (nuint)uint.MaxValue").WithLocation(14, 7)
                );
        }

        [Theory, CombinatorialData]
        public void BetterConversionTarget(bool nullable1, bool nullable2)
        {
            // Given two types T1 and T2, T1 is a better conversion target than T2 if one of the following holds:
            // 1. An implicit conversion from T1 to T2 exists and no implicit conversion from T2 to T1 exists
            // ...
            // 3. T1 is S1 or S1? where S1 is a signed integral type, and T2 is S2 or S2? where S2 is an unsigned integral type.

            string s1Nullable = nullable1 ? "?" : "";
            string s2Nullable = nullable2 ? "?" : "";

            string source = $$"""
using static System.Console;

C.M1(0);
C.M2(0);
C.M3(0);
C.M4(0);
C.M5(0);
C.M6(0);
C.M7(0);
C.M8(0);
C.M9(0);

public class C
{
    public static void M1(sbyte{{s1Nullable}} x) { Write("M1 ");  }
    public static void M1(nuint{{s2Nullable}} x) { }

    public static void M2(short{{s1Nullable}} x) { Write("M2 "); }
    public static void M2(nuint{{s2Nullable}} x) { }

    public static void M3(int{{s1Nullable}} x) { Write("M3 "); }
    public static void M3(nuint{{s2Nullable}} x) { }

    public static void M4(long{{s1Nullable}} x) { Write("M4 "); }
    public static void M4(nuint{{s2Nullable}} x) { }

    public static void M5(nint{{s1Nullable}} x) { Write("M5(nint) "); }
    public static void M5(ushort{{s2Nullable}} x) { Write("M5(ushort) ");  }

    public static void M6(nint{{s1Nullable}} x) { Write("M6 "); }
    public static void M6(uint{{s2Nullable}} x) { }

    public static void M7(nint{{s1Nullable}} x) { Write("M7 "); }
    public static void M7(ulong{{s2Nullable}} x) { }

    public static void M8(nint{{s1Nullable}} x) { Write("M8 "); }
    public static void M8(nuint{{s2Nullable}} x) { }

    public static void M9(nint{{s1Nullable}} x) { Write("M9(nint)"); }
    public static void M9(byte{{s2Nullable}} x) { Write("M9(byte)"); }
}
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);

            // Note: conversions ushort->nint, ushort?->nint?, ushort->nint? are implicit (so rule 1 kicks in), but ushort?->nint is explicit (so rule 3 kicks in)
            var expected = (nullable1, nullable2) is (false, true)
                ? "M1 M2 M3 M4 M5(nint) M6 M7 M8 M9(nint)"
                : "M1 M2 M3 M4 M5(ushort) M6 M7 M8 M9(byte)";
            CompileAndVerify(comp, expectedOutput: IncludeExpectedOutput(expected), verify: Verification.FailsPEVerify);
        }

        [Theory, CombinatorialData]
        public void BetterConversionTarget_IntPtr(bool nullable1, bool nullable2)
        {
            // Given two types T1 and T2, T1 is a better conversion target than T2 if one of the following holds:
            // 1. An implicit conversion from T1 to T2 exists and no implicit conversion from T2 to T1 exists
            // ...
            // 3. T1 is S1 or S1? where S1 is a signed integral type, and T2 is S2 or S2? where S2 is an unsigned integral type.

            string s1Nullable = nullable1 ? "?" : "";
            string s2Nullable = nullable2 ? "?" : "";

            string source = $$"""
using System;
using static System.Console;

C.M1(0);
C.M2(0);
C.M3(0);
C.M4(0);
C.M5(0);
C.M6(0);
C.M7(0);
C.M8(0);
C.M9(0);
C.M10(0);

public class C
{
    public static void M1(sbyte{{s1Nullable}} x) { Write("M1 ");  }
    public static void M1(UIntPtr{{s2Nullable}} x) { }

    public static void M2(short{{s1Nullable}} x) { Write("M2 "); }
    public static void M2(UIntPtr{{s2Nullable}} x) { }

    public static void M3(int{{s1Nullable}} x) { Write("M3 "); }
    public static void M3(UIntPtr{{s2Nullable}} x) { }

    public static void M4(long{{s1Nullable}} x) { Write("M4 "); }
    public static void M4(UIntPtr{{s2Nullable}} x) { }

    public static void M5(nint{{s1Nullable}} x) { Write("M5 "); }
    public static void M5(UIntPtr{{s2Nullable}} x) { }

    public static void M6(IntPtr{{s1Nullable}} x) { Write("M6(IntPtr) "); }
    public static void M6(ushort{{s2Nullable}} x) { Write("M6(ushort) "); }

    public static void M7(IntPtr{{s1Nullable}} x) { Write("M7 "); }
    public static void M7(uint{{s2Nullable}} x) { }

    public static void M8(IntPtr{{s1Nullable}} x) { Write("M8 "); }
    public static void M8(ulong{{s2Nullable}} x) { }

    public static void M9(IntPtr{{s1Nullable}} x) { Write("M9 "); }
    public static void M9(nuint{{s2Nullable}} x) { }

    public static void M10(IntPtr{{s1Nullable}} x) { Write("M10(IntPtr)"); }
    public static void M10(byte{{s2Nullable}} x) { Write("M10(byte)"); }
}
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);

            // Note: conversions ushort->nint, ushort?->nint?, ushort->nint? are implicit (so rule 1 kicks in), but ushort?->nint is explicit (so rule 3 kicks in)
            var expected = (nullable1, nullable2) is (false, true)
                ? "M1 M2 M3 M4 M5 M6(IntPtr) M7 M8 M9 M10(IntPtr)"
                : "M1 M2 M3 M4 M5 M6(ushort) M7 M8 M9 M10(byte)";
            CompileAndVerify(comp, expectedOutput: IncludeExpectedOutput(expected), verify: Verification.FailsPEVerify);
        }

        [Fact]
        public void RetargetingFromNonNumericToNumericIntPtrCorlib()
        {
            string lib_cs = """
public class Base
{
    public virtual nint M() => 0;
}
""";
            var libComp = CreateEmptyCompilation(lib_cs, references: new[] { MscorlibRef_v20 }, assemblyName: "lib");
            libComp.VerifyDiagnostics();

            string source = """
public class Derived : Base
{
    public override nint M() => 0;
}
""";
            var comp = CreateCompilation(source, references: new[] { libComp.ToMetadataReference() }, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();

            var baseM = (RetargetingMethodSymbol)comp.GlobalNamespace.GetMember("Base.M");
            var baseNint = (PENamedTypeSymbol)baseM.ReturnType;

            var derivedM = (MethodSymbol)comp.GlobalNamespace.GetMember("Derived.M");
            var derivedNint = (PENamedTypeSymbol)derivedM.ReturnType;

            Assert.Equal("nint", derivedNint.ToTestDisplayString());
            Assert.Same(baseNint, derivedNint);
        }

        [Fact]
        public void RetargetingFromNumericIntPtrToNonNumericCorlib()
        {
            string lib_cs = """
public class Base
{
    public virtual nint M() => 0;
}
""";
            var libComp = CreateCompilation(lib_cs, assemblyName: "lib", targetFramework: TargetFramework.Net70);
            libComp.VerifyDiagnostics();

            string source = """
public class Derived : Base
{
    public override nint M() => 0;
}
""";
            var comp = CreateEmptyCompilation(source, references: new[] { libComp.ToMetadataReference(), MscorlibRef_v46 });
            comp.VerifyDiagnostics(
                // (1,24): error CS0012: The type 'Object' is defined in an assembly that is not referenced. You must add a reference to assembly 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'.
                // public class Derived : Base
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Base").WithArguments("System.Object", "System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a").WithLocation(1, 24)
                );

            var baseM = (RetargetingMethodSymbol)comp.GlobalNamespace.GetMember("Base.M");
            var baseNint = (PENamedTypeSymbol)baseM.ReturnType;

            var derivedM = (MethodSymbol)comp.GlobalNamespace.GetMember("Derived.M");
            var derivedNint = (NativeIntegerTypeSymbol)derivedM.ReturnType;

            Assert.Equal("System.IntPtr", baseNint.ToTestDisplayString());
            Assert.Equal("nint", derivedNint.ToTestDisplayString());
            Assert.Same(baseNint, derivedNint.UnderlyingNamedType);
        }

        [Fact]
        public void UnsignedRightShift()
        {
            string source = """
public class C
{
    nint M1(nint x, int count) => x >>> count;
    nuint M2(nuint x, int count) => x >>> count;
    nint M3(nint x, int count) => checked(x >>> count);
    nuint M4(nuint x, int count) => checked(x >>> count);

    System.IntPtr M5(System.IntPtr x, int count) => x >>> count;
    System.UIntPtr M6(System.UIntPtr x, int count) => x >>> count;
}
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, verify: Verification.FailsPEVerify);
            verifier.VerifyIL("C.M1", shiftRight("nint"));
            verifier.VerifyIL("C.M2", shiftRight("nuint"));
            verifier.VerifyIL("C.M3", shiftRight("nint"));
            verifier.VerifyIL("C.M4", shiftRight("nuint"));
            verifier.VerifyIL("C.M5", shiftRight("nint"));
            verifier.VerifyIL("C.M6", shiftRight("nuint"));

            return;

            static string shiftRight(string type)
            {
                return $$"""
{
  // Code size       15 (0xf)
  .maxstack  4
  IL_0000:  ldarg.1
  IL_0001:  ldarg.2
  IL_0002:  sizeof     "{{type}}"
  IL_0008:  ldc.i4.8
  IL_0009:  mul
  IL_000a:  ldc.i4.1
  IL_000b:  sub
  IL_000c:  and
  IL_000d:  shr.un
  IL_000e:  ret
}
""";
            }
        }

        [Fact]
        public void OverflowPointerConversion()
        {
            // Breaking change
            string source = """
using System;
class C
{
    public unsafe static void Main()
    {
        void* ptr = (void*)ulong.MaxValue;

        try
        {
            IntPtr i = checked((IntPtr)ptr);
        }
        catch (System.OverflowException)
        {
            Console.Write("OVERFLOW ");
        }

        IntPtr j = unchecked((IntPtr)ptr);
        if (j == (IntPtr)(-1))
        {
            Console.Write("RAN");
        }
    }
}
""";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseExe, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: IncludeExpectedOutput("OVERFLOW RAN"), verify: Verification.Skipped);

            comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: IncludeExpectedOutput("RAN"), verify: Verification.Skipped);
        }

        [Fact]
        public void VariousMembersToDecodeOnRuntimeFeatureType()
        {
            var corlib_cs = @"
namespace System
{
    public struct IntPtr { }
    public struct UIntPtr { }

    public struct Boolean { }
    public class Object { }
    public class String { }
    public class ValueType { }
    public struct Void { }
    public struct Int32 { }

    public class Exception { }

    public class Delegate
    {
        public static Delegate CreateDelegate(Type type, object firstArgument, Reflection.MethodInfo method) => null;
        public static Delegate Combine(Delegate a, Delegate b) => null;
        public static Delegate Remove(Delegate source, Delegate value) => null;
    }
    public class Type
    {
        public Reflection.FieldInfo GetField(string name) => null;
        public static Type GetType(string name) => null;
        public static Type GetTypeFromHandle(RuntimeTypeHandle handle) => null;
    }
    public struct RuntimeMethodHandle { }
    public struct RuntimeTypeHandle { }
    public class MulticastDelegate : Delegate { }
    public class Attribute { }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets t) { }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
    }
    public class Enum { }
    public enum AttributeTargets { }

    public delegate void Action();

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
            public virtual Delegate CreateDelegate(Type delegateType, object target) => throw null;
        }
        public abstract class FieldInfo : MemberInfo
        {
            public abstract object GetValue(object obj);
        }
    }
}
namespace System.Runtime.CompilerServices
{
    public static class RuntimeFeature
    {
        public const string NumericIntPtr = nameof(NumericIntPtr);
        public static bool IsDynamicCodeCompiled => false;
        public static event Action MyEvent;
        public static bool Method(bool x) => true;

        class Nested
        {
            public const string A = nameof(NumericIntPtr);
            public static bool B => false;
            public static event Action C;
            public static bool D(bool x) => true;
        }
    }
}
";
            var source = @"
public class C
{
    public nint field;
    public nuint field2;
}
";
            var corlib = CreateEmptyCompilation(corlib_cs);
            var comp = CreateEmptyCompilation(source, references: new[] { corlib.EmitToImageReference() });
            comp.VerifyDiagnostics();
        }

        [Theory]
        [InlineData("nint")]
        [InlineData("nuint")]
        [InlineData("System.IntPtr")]
        [InlineData("System.UIntPtr")]
        public void XmlDoc_Cref(string type)
        {
            var src = $$"""
/// <summary>Summary <see cref="{{type}}"/>.</summary>
class C { }
""";
            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularWithDocumentationComments, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var docComments = tree.GetCompilationUnitRoot().DescendantTrivia().Select(trivia => trivia.GetStructure()).OfType<DocumentationCommentTriviaSyntax>();
            var cref = docComments.First().DescendantNodes().OfType<XmlCrefAttributeSyntax>().First().Cref;
            Assert.Equal(type, cref.ToString());

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nintSymbol = (INamedTypeSymbol)model.GetSymbolInfo(cref).Symbol;
            Assert.True(nintSymbol.IsNativeIntegerType);
        }

        [Fact]
        public void XmlDoc_Cref_Alias()
        {
            var src = """
using @nint = System.String;

/// <summary>Summary <see cref="nint"/>.</summary>
class C { }
""";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularWithDocumentationComments, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var docComments = tree.GetCompilationUnitRoot().DescendantTrivia().Select(trivia => trivia.GetStructure()).OfType<DocumentationCommentTriviaSyntax>();
            var cref = docComments.First().DescendantNodes().OfType<XmlCrefAttributeSyntax>().First().Cref;
            Assert.Equal("nint", cref.ToString());

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var symbol = (INamedTypeSymbol)model.GetSymbolInfo(cref).Symbol;
            Assert.False(symbol.IsNativeIntegerType);
            Assert.Equal("System.String", symbol.ToTestDisplayString());
        }

        [Theory]
        [InlineData("nint")]
        [InlineData("nuint")]
        public void XmlDoc_Cref_Member(string fieldName)
        {
            var src = $$"""
/// <summary>Summary <see cref="{{fieldName}}"/>.</summary>
public class C
{
    /// <summary></summary>
    public int {{fieldName}};
}
""";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularWithDocumentationComments, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var docComments = tree.GetCompilationUnitRoot().DescendantTrivia().Select(trivia => trivia.GetStructure()).OfType<DocumentationCommentTriviaSyntax>();
            var cref = docComments.First().DescendantNodes().OfType<XmlCrefAttributeSyntax>().First().Cref;
            Assert.Equal(fieldName, cref.ToString());

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var symbol = (IFieldSymbol)model.GetSymbolInfo(cref).Symbol;
            Assert.Equal($"System.Int32 C.{fieldName}", symbol.ToTestDisplayString());
        }

        [Fact]
        public void XmlDoc_Cref_Member_Escaped()
        {
            var src = """
/// <summary>Summary <see cref="@nint"/>.</summary>
public class C
{
    /// <summary></summary>
    public int nint;
}
""";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularWithDocumentationComments, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var docComments = tree.GetCompilationUnitRoot().DescendantTrivia().Select(trivia => trivia.GetStructure()).OfType<DocumentationCommentTriviaSyntax>();
            var cref = docComments.First().DescendantNodes().OfType<XmlCrefAttributeSyntax>().First().Cref;
            Assert.Equal("@nint", cref.ToString());

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var symbol = (IFieldSymbol)model.GetSymbolInfo(cref).Symbol;
            Assert.Equal("System.Int32 C.nint", symbol.ToTestDisplayString());
        }

        [Theory]
        [InlineData("@nint")]
        [InlineData("@nuint")]
        public void XmlDoc_Cref_Escaped(string type)
        {
            var src = $$"""
/// <summary>Summary <see cref="{{type}}"/>.</summary>
public class C
{
}
""";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularWithDocumentationComments, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(
                // (1,33): warning CS1574: XML comment has cref attribute 'type' that could not be resolved
                // /// <summary>Summary <see cref="type"/>.</summary>
                Diagnostic(ErrorCode.WRN_BadXMLRef, type).WithArguments(type).WithLocation(1, 33)
                );
        }

        [Fact]
        public void XmlDoc_Cref_Member_NintZero()
        {
            var src = """
/// <summary>Summary <see cref="nint.Zero"/>.</summary>
public class C
{
    /// <summary></summary>
    public int nint;
}
""";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularWithDocumentationComments, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var docComments = tree.GetCompilationUnitRoot().DescendantTrivia().Select(trivia => trivia.GetStructure()).OfType<DocumentationCommentTriviaSyntax>();
            var cref = docComments.First().DescendantNodes().OfType<XmlCrefAttributeSyntax>().First().Cref;
            Assert.Equal("nint.Zero", cref.ToString());

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var symbol = (IFieldSymbol)model.GetSymbolInfo(cref).Symbol;
            Assert.Equal("nint nint.Zero", symbol.ToTestDisplayString());
        }

        [Fact, WorkItem(43347, "https://github.com/dotnet/roslyn/issues/43347")]
        public void MaskShiftCount()
        {
            // positive nint shift right
            validate("nint", "NIntMaxValue", ">> 0", "0x7FFF_FFFF", "0x7FFF_FFFF_FFFF_FFFF", nint_shr(0));
            validate("nint", "NIntMaxValue", ">> 1", "0x3FFF_FFFF", "0x3FFF_FFFF_FFFF_FFFF", nint_shr(1));
            validate("nint", "NIntMaxValue", ">> 31", "0x0", "0xFFFF_FFFF", nint_shr(31));
            validate("nint", "NIntMaxValue", ">> 32", "0x7FFF_FFFF", "0x7FFF_FFFF", nint_shr(32));
            validate("nint", "NIntMaxValue", ">> 33", "0x3FFF_FFFF", "0x3FFF_FFFF", nint_shr(33));
            validate("nint", "NIntMaxValue", ">> 63", "0x0", "0x0", nint_shr(63));
            validate("nint", "NIntMaxValue", ">> 64", "0x7FFF_FFFF", "0x7FFF_FFFF_FFFF_FFFF", nint_shr(64));
            validate("nint", "NIntMaxValue", ">> 65", "0x3FFF_FFFF", "0x3FFF_FFFF_FFFF_FFFF", nint_shr(65));

            // negative nint shift right
            validate("nint", "NIntNegativeValue", ">> 1", "0xE000_0000", "0xE000_0000_0000_0000", nint_shr(1));
            validate("nint", "NIntNegativeValue", ">> 31", "0xFFFF_FFFF", "0xFFFF_FFFF_8000_0000", nint_shr(31));
            validate("nint", "NIntNegativeValue", ">> 32", "0xC000_0001", "0xFFFF_FFFF_C000_0000", nint_shr(32));
            validate("nint", "NIntNegativeValue", ">> 33", "0xE000_0000", "0xFFFF_FFFF_E000_0000", nint_shr(33));
            validate("nint", "NIntNegativeValue", ">> 63", "0xFFFF_FFFF", "0xFFFF_FFFF_FFFF_FFFF", nint_shr(63));
            validate("nint", "NIntNegativeValue", ">> 64", "0xC000_0001", "0xC000_0000_0000_0001", nint_shr(64));

            // positive nint shift left
            validate("nint", "NIntMaxValue", "<< 0", "0x7FFF_FFFF", "0x7FFF_FFFF_FFFF_FFFF", nint_shl(0));
            validate("nint", "NIntMaxValue", "<< 1", "0xFFFF_FFFE", "0xFFFF_FFFF_FFFF_FFFE", nint_shl(1));
            validate("nint", "NIntMaxValue", "<< 31", "0x8000_0000", "0xFFFF_FFFF_8000_0000", nint_shl(31));
            validate("nint", "NIntMaxValue", "<< 32", "0x7FFF_FFFF", "0xFFFF_FFFF_0000_0000", nint_shl(32));
            validate("nint", "NIntMaxValue", "<< 63", "0x8000_0000", "0x8000_0000_0000_0000", nint_shl(63));
            validate("nint", "NIntMaxValue", "<< 64", "0x7FFF_FFFF", "0x7FFF_FFFF_FFFF_FFFF", nint_shl(64));

            // negative nint shift left
            validate("nint", "NIntNegativeValue", "<< 63", "0x8000_0000", "0x8000_0000_0000_0000", nint_shl(63));

            // nuint shift right
            validate("nuint", "NUintMaxValue", ">> 0", "0xFFFF_FFFF", "0xFFFF_FFFF_FFFF_FFFF", nuint_shr_un(0));
            validate("nuint", "NUintMaxValue", ">> 1", "0x7FFF_FFFF", "0x7FFF_FFFF_FFFF_FFFF", nuint_shr_un(1));
            validate("nuint", "NUintMaxValue", ">> 31", "0x0000_0001", "0x0000_0001_FFFF_FFFF", nuint_shr_un(31));
            validate("nuint", "NUintMaxValue", ">> 32", "0xFFFF_FFFF", "0x0000_0000_FFFF_FFFF", nuint_shr_un(32));
            validate("nuint", "NUintMaxValue", ">> 63", "0x0000_0001", "0x0000_0000_0000_0001", nuint_shr_un(63));
            validate("nuint", "NUintMaxValue", ">> 64", "0xFFFF_FFFF", "0xFFFF_FFFF_FFFF_FFFF", nuint_shr_un(64));

            // nuint shift left
            validate("nuint", "NUintMaxValue", "<< 0", "0xFFFF_FFFF", "0xFFFF_FFFF_FFFF_FFFF", nuint_shl(0));
            validate("nuint", "NUintMaxValue", "<< 1", "0xFFFF_FFFE", "0xFFFF_FFFF_FFFF_FFFE", nuint_shl(1));
            validate("nuint", "NUintMaxValue", "<< 31", "0x8000_0000", "0xFFFF_FFFF_8000_0000", nuint_shl(31));
            validate("nuint", "NUintMaxValue", "<< 32", "0xFFFF_FFFF", "0xFFFF_FFFF_0000_0000", nuint_shl(32));
            validate("nuint", "NUintMaxValue", "<< 63", "0x8000_0000", "0x8000_0000_0000_0000", nuint_shl(63));
            validate("nuint", "NUintMaxValue", "<< 64", "0xFFFF_FFFF", "0xFFFF_FFFF_FFFF_FFFF", nuint_shl(64));

            // positive nint unsigned shift right
            validate("nint", "NIntMaxValue", ">>> 0", "0x7FFF_FFFF", "0x7FFF_FFFF_FFFF_FFFF", nint_shr_un(0));
            validate("nint", "NIntMaxValue", ">>> 1", "0x3FFF_FFFF", "0x3FFF_FFFF_FFFF_FFFF", nint_shr_un(1));
            validate("nint", "NIntMaxValue", ">>> 31", "0x0", "0xFFFF_FFFF", nint_shr_un(31));
            validate("nint", "NIntMaxValue", ">>> 32", "0x7FFF_FFFF", "0x7FFF_FFFF", nint_shr_un(32));
            validate("nint", "NIntMaxValue", ">>> 63", "0x0", "0x0", nint_shr_un(63));
            validate("nint", "NIntMaxValue", ">>> 64", "0x7FFF_FFFF", "0x7FFF_FFFF_FFFF_FFFF", nint_shr_un(64));

            // negative nint unsigned shift right
            validate("nint", "NIntNegativeValue", ">>> 0", "0xC000_0001", "0xC000_0000_0000_0001", nint_shr_un(0));
            validate("nint", "NIntNegativeValue", ">>> 1", "0x6000_0000", "0x6000_0000_0000_0000", nint_shr_un(1));
            validate("nint", "NIntNegativeValue", ">>> 31", "0x1", "0x0000_0001_8000_0000", nint_shr_un(31));
            validate("nint", "NIntNegativeValue", ">>> 32", "0xC000_0001", "0x0000_0000_C000_0000", nint_shr_un(32));
            validate("nint", "NIntNegativeValue", ">>> 63", "0x1", "0x1", nint_shr_un(63));
            validate("nint", "NIntNegativeValue", ">>> 64", "0xC000_0001", "0xC000_0000_0000_0001", nint_shr_un(64));

            // nuint unsigned shift right
            validate("nuint", "NUintMaxValue", ">>> 0", "0xFFFF_FFFF", "0xFFFF_FFFF_FFFF_FFFF", nuint_shr_un(0));
            validate("nuint", "NUintMaxValue", ">>> 1", "0x7FFF_FFFF", "0x7FFF_FFFF_FFFF_FFFF", nuint_shr_un(1));
            validate("nuint", "NUintMaxValue", ">>> 31", "0x0000_0001", "0x0000_0001_FFFF_FFFF", nuint_shr_un(31));
            validate("nuint", "NUintMaxValue", ">>> 32", "0xFFFF_FFFF", "0x0000_0000_FFFF_FFFF", nuint_shr_un(32));
            validate("nuint", "NUintMaxValue", ">>> 63", "0x0000_0001", "0x0000_0000_0000_0001", nuint_shr_un(63));
            validate("nuint", "NUintMaxValue", ">>> 64", "0xFFFF_FFFF", "0xFFFF_FFFF_FFFF_FFFF", nuint_shr_un(64));

            // lifted value
            validate("nint?", "NIntMaxValue", ">> 0", "0x7FFF_FFFF", "0x7FFF_FFFF_FFFF_FFFF", liftedValue(0, "nint?", "shr"));
            validate("nint?", "NIntMaxValue", ">> 1", "0x3FFF_FFFF", "0x3FFF_FFFF_FFFF_FFFF", liftedValue(1, "nint?", "shr"));
            validate("nint?", "NIntMaxValue", ">> 65", "0x3FFF_FFFF", "0x3FFF_FFFF_FFFF_FFFF", liftedValue(65, "nint?", "shr"));
            validate("nint?", "NIntNegativeValue", ">> 65", "0xE000_0000", "0xE000_0000_0000_0000", liftedValue(65, "nint?", "shr"));
            validate("nint?", "NIntMaxValue", "<< 65", "0xFFFF_FFFE", "0xFFFF_FFFF_FFFF_FFFE", liftedValue(65, "nint?", "shl"));
            validate("nint?", "NIntNegativeValue", "<< 65", "0x8000_0002", "0x8000_0000_0000_0002", liftedValue(65, "nint?", "shl"));

            validate("nuint?", "NUintMaxValue", ">> 65", "0x7FFF_FFFF", "0x7FFF_FFFF_FFFF_FFFF", liftedValue(65, "nuint?", "shr.un"));
            validate("nuint?", "NUintMaxValue", "<< 65", "0xFFFF_FFFE", "0xFFFF_FFFF_FFFF_FFFE", liftedValue(65, "nuint?", "shl"));

            validate("nint?", "NIntMaxValue", ">>> 65", "0x3FFF_FFFF", "0x3FFF_FFFF_FFFF_FFFF", liftedValue(65, "nint?", "shr.un"));
            validate("nint?", "NIntNegativeValue", ">>> 65", "0x6000_0000", "0x6000_0000_0000_0000", liftedValue(65, "nint?", "shr.un"));
            validate("nuint?", "NUintMaxValue", ">>> 65", "0x7FFF_FFFF", "0x7FFF_FFFF_FFFF_FFFF", liftedValue(65, "nuint?", "shr.un"));

            // lifted count
            compileAndVerify("""
class C
{
    nint? M(nint value, int? count) => value >> count;
}
""")
                .VerifyIL("C.M", @"
{
  // Code size       49 (0x31)
  .maxstack  4
  .locals init (nint V_0,
                int? V_1,
                nint? V_2)
  IL_0000:  ldarg.1
  IL_0001:  stloc.0
  IL_0002:  ldarg.2
  IL_0003:  stloc.1
  IL_0004:  ldloca.s   V_1
  IL_0006:  call       ""readonly bool int?.HasValue.get""
  IL_000b:  brtrue.s   IL_0017
  IL_000d:  ldloca.s   V_2
  IL_000f:  initobj    ""nint?""
  IL_0015:  ldloc.2
  IL_0016:  ret
  IL_0017:  ldloc.0
  IL_0018:  ldloca.s   V_1
  IL_001a:  call       ""readonly int int?.GetValueOrDefault()""
  IL_001f:  sizeof     ""nint""
  IL_0025:  ldc.i4.8
  IL_0026:  mul
  IL_0027:  ldc.i4.1
  IL_0028:  sub
  IL_0029:  and
  IL_002a:  shr
  IL_002b:  newobj     ""nint?..ctor(nint)""
  IL_0030:  ret
}
");

            // lifted value and lifted count
            compileAndVerify("""
class C
{
    nint? M(nint? value, int? count) => value >> count;
}
""")
                .VerifyIL("C.M", @"
{
  // Code size       63 (0x3f)
  .maxstack  4
  .locals init (nint? V_0,
                int? V_1,
                nint? V_2)
  IL_0000:  ldarg.1
  IL_0001:  stloc.0
  IL_0002:  ldarg.2
  IL_0003:  stloc.1
  IL_0004:  ldloca.s   V_0
  IL_0006:  call       ""readonly bool nint?.HasValue.get""
  IL_000b:  ldloca.s   V_1
  IL_000d:  call       ""readonly bool int?.HasValue.get""
  IL_0012:  and
  IL_0013:  brtrue.s   IL_001f
  IL_0015:  ldloca.s   V_2
  IL_0017:  initobj    ""nint?""
  IL_001d:  ldloc.2
  IL_001e:  ret
  IL_001f:  ldloca.s   V_0
  IL_0021:  call       ""readonly nint nint?.GetValueOrDefault()""
  IL_0026:  ldloca.s   V_1
  IL_0028:  call       ""readonly int int?.GetValueOrDefault()""
  IL_002d:  sizeof     ""nint""
  IL_0033:  ldc.i4.8
  IL_0034:  mul
  IL_0035:  ldc.i4.1
  IL_0036:  sub
  IL_0037:  and
  IL_0038:  shr
  IL_0039:  newobj     ""nint?..ctor(nint)""
  IL_003e:  ret
}
");
            return;

            static string nint_shr(int count) => shift(count, "nint", "shr");
            static string nint_shr_un(int count) => shift(count, "nint", "shr.un");
            static string nint_shl(int count) => shift(count, "nint", "shl");
            static string nuint_shr_un(int count) => shift(count, "nuint", "shr.un");
            static string nuint_shl(int count) => shift(count, "nuint", "shl");

            static string shift(int count, string type, string op)
            {
                if (count == 0)
                {
                    return $@"
{{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}}
";
                }

                if (count == 1)
                {
                    return $@"
{{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  {op}
  IL_0003:  ret
}}
";
                }

                if (count <= 31)
                {
                    return $@"
{{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   {count}
  IL_0003:  {op}
  IL_0004:  ret
}}
";
                }

                return $@"
{{
  // Code size       16 (0x10)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   {count}
  IL_0003:  sizeof     ""{type}""
  IL_0009:  ldc.i4.8
  IL_000a:  mul
  IL_000b:  ldc.i4.1
  IL_000c:  sub
  IL_000d:  and
  IL_000e:  {op}
  IL_000f:  ret
}}
";
            }

            static string liftedValue(int count, string type, string op)
            {
                var strippedType = type.Trim('?');
                if (count == 0)
                {
                    return $@"
{{
  // Code size       34 (0x22)
  .maxstack  1
  .locals init ({type} V_0,
                {type} V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool {type}.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""{type}""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly {strippedType} {type}.GetValueOrDefault()""
  IL_001c:  newobj     ""{type}..ctor({strippedType})""
  IL_0021:  ret
}}";
                }

                if (count == 1)
                {
                    return $@"
{{
  // Code size       36 (0x24)
  .maxstack  2
  .locals init ({type} V_0,
                {type} V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool {type}.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""{type}""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly {strippedType} {type}.GetValueOrDefault()""
  IL_001c:  ldc.i4.1
  IL_001d:  {op}
  IL_001e:  newobj     ""{type}..ctor({strippedType})""
  IL_0023:  ret
}}
";
                }

                Assert.True(count == 65);

                return $@"
{{
  // Code size       48 (0x30)
  .maxstack  4
  .locals init ({type} V_0,
                {type} V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""readonly bool {type}.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""{type}""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""readonly {strippedType} {type}.GetValueOrDefault()""
  IL_001c:  ldc.i4.s   {count}
  IL_001e:  sizeof     ""{strippedType}""
  IL_0024:  ldc.i4.8
  IL_0025:  mul
  IL_0026:  ldc.i4.1
  IL_0027:  sub
  IL_0028:  and
  IL_0029:  {op}
  IL_002a:  newobj     ""{type}..ctor({strippedType})""
  IL_002f:  ret
}}
";
            }

            CompilationVerifier compileAndVerify(CSharpTestSource source)
            {
                var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
                return CompileAndVerify(comp, verify: Verification.FailsPEVerify);
            }

            void validate(string type, string value, string binaryOp, string result32Bits, string result64Bits, string expectedIL)
            {
                validateWithCheckedOrUnchecked(type, value, binaryOp, expectedIL, result32Bits, result64Bits, isChecked: true);
                validateWithCheckedOrUnchecked(type, value, binaryOp, expectedIL, result32Bits, result64Bits, isChecked: false);
            }

            void validateWithCheckedOrUnchecked(string type, string value, string binaryOp, string expectedIL, string result32Bits, string result64Bits, bool isChecked)
            {
                var checkedKeyword = isChecked ? "checked" : "unchecked";
                var strippedType = type.Trim('?');

                var source = $$"""
class C
{
    public static unsafe nint NIntMaxValue
        => (sizeof(nint) == 4) ? (nint)0x7FFF_FFFF : (nint)0x7FFF_FFFF_FFFF_FFFF;

    public static unsafe nint NIntNegativeValue
        => (sizeof(nint) == 4) ? (nint)0xC000_0001 : unchecked((nint)0xC000_0000_0000_0001);

    public static unsafe nuint NUintMaxValue
        => (sizeof(nint) == 4) ? (nuint)0xFFFF_FFFF : (nuint)0xFFFF_FFFF_FFFF_FFFF;

    public static {{type}} M({{type}} value)
    {
        return {{checkedKeyword}}(value {{binaryOp}});
    }
    public static unsafe void Main()
    {
        if (sizeof(nint) == 4)
        {
            if (unchecked(({{type}}){{result32Bits}}) == ({{value}} {{binaryOp}}))
            {
                System.Console.Write("RAN");
            }
            else
            {
                System.Console.Write($"Actual for '{{value}} {{binaryOp}}' (32-bit): {{{value}} {{binaryOp}}}");
            }
        }
        else if (sizeof(nint) == 8)
        {
            if (unchecked(({{type}}){{result64Bits}}) == ({{value}} {{binaryOp}}))
            {
                System.Console.Write("RAN");
            }
            else
            {
                System.Console.Write($"Actual for '{{value}} {{binaryOp}}' (64-bit): {{{value}} {{binaryOp}}}");
            }
        }
    }
}
""";
                var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseExe, targetFramework: TargetFramework.Net70);
                var verifier = CompileAndVerify(comp, verify: Verification.FailsPEVerify, expectedOutput: IncludeExpectedOutput("RAN"));
                verifier.VerifyIL("C.M", expectedIL);
            }
        }

        [Fact]
        public void MaskShiftCount_NegativeCount()
        {
            var source = """
System.Console.WriteLine(C.ShiftRight(255));

static class C
{
    public static nint ShiftRight(nint x) => x >> (-62);
}
""";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseExe, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: IncludeExpectedOutput("63"), verify: Verification.FailsPEVerify);
            verifier.VerifyIL("C.ShiftRight", @"
{
  // Code size       16 (0x10)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   -62
  IL_0003:  sizeof     ""nint""
  IL_0009:  ldc.i4.8
  IL_000a:  mul
  IL_000b:  ldc.i4.1
  IL_000c:  sub
  IL_000d:  and
  IL_000e:  shr
  IL_000f:  ret
}
");
        }

        [WorkItem(63348, "https://github.com/dotnet/roslyn/issues/63348")]
        [Fact]
        public void ConditionalStackalloc()
        {
            var source =
@"using System;
class Program
{
    static int F(int n)
    {
        Span<char> s = n < 10 ? stackalloc char[n] : new char[n];
        return s[n - 1];
    }
    static void Main()
    {
        Console.Write(F(1));
        Console.Write(F(11));
    }
}";

            var comp = CreateCompilation(new[] { SpanSource, source }, options: TestOptions.UnsafeReleaseExe);
            verify(comp);

            comp = CreateCompilation(new[] { source }, options: TestOptions.UnsafeReleaseExe, targetFramework: TargetFramework.Net70);
            verify(comp);

            void verify(CSharpCompilation comp)
            {
                comp.VerifyEmitDiagnostics();
                var verifier = CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput("00"));
                verifier.VerifyIL("Program.F", """
{
  // Code size       48 (0x30)
  .maxstack  3
  .locals init (System.Span<char> V_0, //s
                System.Span<char> V_1,
                int V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   10
  IL_0003:  bge.s      IL_0016
  IL_0005:  ldarg.0
  IL_0006:  stloc.2
  IL_0007:  ldloc.2
  IL_0008:  conv.u
  IL_0009:  ldc.i4.2
  IL_000a:  mul.ovf.un
  IL_000b:  localloc
  IL_000d:  ldloc.2
  IL_000e:  newobj     "System.Span<char>..ctor(void*, int)"
  IL_0013:  stloc.1
  IL_0014:  br.s       IL_0022
  IL_0016:  ldarg.0
  IL_0017:  newarr     "char"
  IL_001c:  call       "System.Span<char> System.Span<char>.op_Implicit(char[])"
  IL_0021:  stloc.1
  IL_0022:  ldloc.1
  IL_0023:  stloc.0
  IL_0024:  ldloca.s   V_0
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4.1
  IL_0028:  sub
  IL_0029:  call       "ref char System.Span<char>.this[int].get"
  IL_002e:  ldind.u2
  IL_002f:  ret
}
""");
            }
        }

        [WorkItem(67041, "https://github.com/dotnet/roslyn/issues/67041")]
        [Theory]
        [CombinatorialData]
        public void PointerSubtraction(bool useNumericIntPtr)
        {
            var source = """
                class Program
                {
                    static unsafe long F(int* p1)
                    {
                        byte* p2 = (byte*)p1;
                        p2++;
                        return p2 - (byte*)p1;
                    }
                }
                """;
            var comp = CreateCompilation(
                source,
                options: TestOptions.UnsafeReleaseDll,
                targetFramework: useNumericIntPtr ? TargetFramework.Net60 : TargetFramework.Net70);
            comp.VerifyEmitDiagnostics();
        }

        private void VerifyNoNativeIntegerAttributeEmitted(CSharpCompilation comp)
        {
            // PEVerify is skipped because it reports "Type load failed" because of the above corlib,
            // not because of duplicate TypeRefs in this assembly. Replace the above corlib with the
            // actual corlib when that assembly contains UIntPtr.MaxValue or if we decide to support
            // nuint.MaxValue (since MaxValue could be used in this test instead).
            CompileAndVerify(comp,
                emitOptions: new EmitOptions(runtimeMetadataVersion: "v5.1", debugInformationFormat: DebugInformationFormat.PortablePdb),
                symbolValidator: module => Assert.Equal("", NativeIntegerAttributesVisitor.GetString((PEModuleSymbol)module)),
                verify: Verification.Skipped);
        }

        const string RuntimeFeature_NumericIntPtr = @"
namespace System.Runtime.CompilerServices
{
    public static class RuntimeFeature
    {
        public const string NumericIntPtr = nameof(NumericIntPtr);
    }
}
";

        static string AsNative(string type) => type switch
        {
            "IntPtr" or "System.IntPtr" => "nint",
            "IntPtr?" or "System.IntPtr?" => "nint?",
            "UIntPtr" or "System.UIntPtr" => "nuint",
            "UIntPtr?" or "System.UIntPtr?" => "nuint?",
            var t => t
        };
    }
}
