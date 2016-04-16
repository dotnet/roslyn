// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ClsComplianceTests : CSharpTestBase
    {
        [Fact]
        public void WRN_CLS_AssemblyNotCLS()
        {
            var source = @"
using System;

[CLSCompliant(true)]
public class C
{
    [CLSCompliant(true)] public void M() { }
    [CLSCompliant(true)] public int P { get; set; }
    [CLSCompliant(true)] public event D E;
    [CLSCompliant(true)] public int F;
    
    [CLSCompliant(true)] private class NC { }
    [CLSCompliant(true)] public interface NI { }
    [CLSCompliant(true)] public struct NS { }
    [CLSCompliant(true)] public delegate void ND();
}

[CLSCompliant(true)]
public interface I { }

[CLSCompliant(true)]
public struct S { }

[CLSCompliant(true)]
public delegate void D();
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (5,14): warning CS3014: 'C' cannot be marked as CLS-compliant because the assembly does not have a CLSCompliant attribute
                // public class C
                Diagnostic(ErrorCode.WRN_CLS_AssemblyNotCLS, "C").WithArguments("C"),
                // (7,38): warning CS3014: 'C.M()' cannot be marked as CLS-compliant because the assembly does not have a CLSCompliant attribute
                //     [CLSCompliant(true)] public void M() { }
                Diagnostic(ErrorCode.WRN_CLS_AssemblyNotCLS, "M").WithArguments("C.M()"),
                // (8,37): warning CS3014: 'C.P' cannot be marked as CLS-compliant because the assembly does not have a CLSCompliant attribute
                //     [CLSCompliant(true)] public int P { get; set; }
                Diagnostic(ErrorCode.WRN_CLS_AssemblyNotCLS, "P").WithArguments("C.P"),
                // (9,41): warning CS3014: 'C.E' cannot be marked as CLS-compliant because the assembly does not have a CLSCompliant attribute
                //     [CLSCompliant(true)] public event D E;
                Diagnostic(ErrorCode.WRN_CLS_AssemblyNotCLS, "E").WithArguments("C.E"),
                // (10,37): warning CS3014: 'C.F' cannot be marked as CLS-compliant because the assembly does not have a CLSCompliant attribute
                //     [CLSCompliant(true)] public int F;
                Diagnostic(ErrorCode.WRN_CLS_AssemblyNotCLS, "F").WithArguments("C.F"),
                // (12,40): warning CS3014: 'C.NC' cannot be marked as CLS-compliant because the assembly does not have a CLSCompliant attribute
                //     [CLSCompliant(true)] private class NC { }
                Diagnostic(ErrorCode.WRN_CLS_AssemblyNotCLS, "NC").WithArguments("C.NC"),
                // (12,43): warning CS3014: 'C.NI' cannot be marked as CLS-compliant because the assembly does not have a CLSCompliant attribute
                //     [CLSCompliant(true)] public interface NI { }
                Diagnostic(ErrorCode.WRN_CLS_AssemblyNotCLS, "NI").WithArguments("C.NI"),
                // (13,40): warning CS3014: 'C.NS' cannot be marked as CLS-compliant because the assembly does not have a CLSCompliant attribute
                //     [CLSCompliant(true)] public struct NS { }
                Diagnostic(ErrorCode.WRN_CLS_AssemblyNotCLS, "NS").WithArguments("C.NS"),
                // (14,47): warning CS3014: 'C.ND' cannot be marked as CLS-compliant because the assembly does not have a CLSCompliant attribute
                //     [CLSCompliant(true)] public delegate void ND();
                Diagnostic(ErrorCode.WRN_CLS_AssemblyNotCLS, "ND").WithArguments("C.ND"),
                // (18,18): warning CS3014: 'I' cannot be marked as CLS-compliant because the assembly does not have a CLSCompliant attribute
                // public interface I { }
                Diagnostic(ErrorCode.WRN_CLS_AssemblyNotCLS, "I").WithArguments("I"),
                // (21,15): warning CS3014: 'S' cannot be marked as CLS-compliant because the assembly does not have a CLSCompliant attribute
                // public struct S { }
                Diagnostic(ErrorCode.WRN_CLS_AssemblyNotCLS, "S").WithArguments("S"),
                // (24,22): warning CS3014: 'D' cannot be marked as CLS-compliant because the assembly does not have a CLSCompliant attribute
                // public delegate void D();
                Diagnostic(ErrorCode.WRN_CLS_AssemblyNotCLS, "D").WithArguments("D"),

                // (9,41): warning CS0067: The event 'C.E' is never used
                //     [CLSCompliant(true)] public event D E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E"));
        }

        [Fact]
        public void WRN_CLS_AssemblyNotCLS2()
        {
            var source = @"
using System;

[CLSCompliant(false)]
public class C
{
    [CLSCompliant(false)] public void M() { }
    [CLSCompliant(false)] public int P { get; set; }
    [CLSCompliant(false)] public event D E;
    [CLSCompliant(false)] public int F;
    
    [CLSCompliant(true)] private class NC { }
    [CLSCompliant(false)] public interface NI { }
    [CLSCompliant(false)] public struct NS { }
    [CLSCompliant(false)] public delegate void ND();
}

[CLSCompliant(false)]
public interface I { }

[CLSCompliant(false)]
public struct S { }

[CLSCompliant(false)]
public delegate void D();
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (5,14): warning CS3021: 'C' does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
                // public class C
                Diagnostic(ErrorCode.WRN_CLS_AssemblyNotCLS2, "C").WithArguments("C"),
                // (7,39): warning CS3021: 'C.M()' does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
                //     [CLSCompliant(false)] public void M() { }
                Diagnostic(ErrorCode.WRN_CLS_AssemblyNotCLS2, "M").WithArguments("C.M()"),
                // (8,38): warning CS3021: 'C.P' does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
                //     [CLSCompliant(false)] public int P { get; set; }
                Diagnostic(ErrorCode.WRN_CLS_AssemblyNotCLS2, "P").WithArguments("C.P"),
                // (9,42): warning CS3021: 'C.E' does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
                //     [CLSCompliant(false)] public event D E;
                Diagnostic(ErrorCode.WRN_CLS_AssemblyNotCLS2, "E").WithArguments("C.E"),
                // (10,38): warning CS3021: 'C.F' does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
                //     [CLSCompliant(false)] public int F;
                Diagnostic(ErrorCode.WRN_CLS_AssemblyNotCLS2, "F").WithArguments("C.F"),
                // (12,40): warning CS3014: 'C.NC' cannot be marked as CLS-compliant because the assembly does not have a CLSCompliant attribute
                //     [CLSCompliant(true)] private class NC { }
                Diagnostic(ErrorCode.WRN_CLS_AssemblyNotCLS, "NC").WithArguments("C.NC"),
                // (12,44): warning CS3021: 'C.NI' does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
                //     [CLSCompliant(false)] public interface NI { }
                Diagnostic(ErrorCode.WRN_CLS_AssemblyNotCLS2, "NI").WithArguments("C.NI"),
                // (13,41): warning CS3021: 'C.NS' does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
                //     [CLSCompliant(false)] public struct NS { }
                Diagnostic(ErrorCode.WRN_CLS_AssemblyNotCLS2, "NS").WithArguments("C.NS"),
                // (14,48): warning CS3021: 'C.ND' does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
                //     [CLSCompliant(false)] public delegate void ND();
                Diagnostic(ErrorCode.WRN_CLS_AssemblyNotCLS2, "ND").WithArguments("C.ND"),
                // (18,18): warning CS3021: 'I' does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
                // public interface I { }
                Diagnostic(ErrorCode.WRN_CLS_AssemblyNotCLS2, "I").WithArguments("I"),
                // (21,15): warning CS3021: 'S' does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
                // public struct S { }
                Diagnostic(ErrorCode.WRN_CLS_AssemblyNotCLS2, "S").WithArguments("S"),
                // (24,22): warning CS3021: 'D' does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
                // public delegate void D();
                Diagnostic(ErrorCode.WRN_CLS_AssemblyNotCLS2, "D").WithArguments("D"),

                // (9,41): warning CS0067: The event 'C.E' is never used
                //     [CLSCompliant(true)] public event D E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E"));
        }

        [Fact]
        public void WRN_CLS_MeaninglessOnPrivateType_True()
        {
            var source = @"
using System;

[assembly:CLSCompliant(true)]

internal class Outer1
{
    [CLSCompliant(true)] public class Inner1 { }
}

public class Outer2
{
    [CLSCompliant(true)] internal class Inner2 { }
}

public class Kinds
{
    [CLSCompliant(true)] private void M() { }
    [CLSCompliant(true)] private int P { get; set; }
    [CLSCompliant(true)] private event ND E;
    [CLSCompliant(true)] private int F;
                         
    [CLSCompliant(true)] private class NC { }
    [CLSCompliant(true)] private interface NI { }
    [CLSCompliant(true)] private struct NS { }
    [CLSCompliant(true)] private delegate void ND();
}

public class Levels
{
    [CLSCompliant(true)] private int F1;
    [CLSCompliant(true)] internal int F2;
    [CLSCompliant(true)] protected internal int F3;
    [CLSCompliant(true)] protected int F4;
    [CLSCompliant(true)] public int F5;
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (8,39): warning CS3019: CLS compliance checking will not be performed on 'Outer1.Inner1' because it is not visible from outside this assembly
                //     [CLSCompliant(true)] public class Inner1 { }
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnPrivateType, "Inner1").WithArguments("Outer1.Inner1"),
                // (13,41): warning CS3019: CLS compliance checking will not be performed on 'Outer2.Inner2' because it is not visible from outside this assembly
                //     [CLSCompliant(true)] internal class Inner2 { }
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnPrivateType, "Inner2").WithArguments("Outer2.Inner2"),
                // (18,39): warning CS3019: CLS compliance checking will not be performed on 'Kinds.M()' because it is not visible from outside this assembly
                //     [CLSCompliant(true)] private void M() { }
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnPrivateType, "M").WithArguments("Kinds.M()"),
                // (19,38): warning CS3019: CLS compliance checking will not be performed on 'Kinds.P' because it is not visible from outside this assembly
                //     [CLSCompliant(true)] private int P { get; set; }
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnPrivateType, "P").WithArguments("Kinds.P"),
                // (20,43): warning CS3019: CLS compliance checking will not be performed on 'Kinds.E' because it is not visible from outside this assembly
                //     [CLSCompliant(true)] private event ND E;
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnPrivateType, "E").WithArguments("Kinds.E"),
                // (21,38): warning CS3019: CLS compliance checking will not be performed on 'Kinds.F' because it is not visible from outside this assembly
                //     [CLSCompliant(true)] private int F;
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnPrivateType, "F").WithArguments("Kinds.F"),
                // (23,40): warning CS3019: CLS compliance checking will not be performed on 'Kinds.NC' because it is not visible from outside this assembly
                //     [CLSCompliant(true)] private class NC { }
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnPrivateType, "NC").WithArguments("Kinds.NC"),
                // (24,44): warning CS3019: CLS compliance checking will not be performed on 'Kinds.NI' because it is not visible from outside this assembly
                //     [CLSCompliant(true)] private interface NI { }
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnPrivateType, "NI").WithArguments("Kinds.NI"),
                // (25,41): warning CS3019: CLS compliance checking will not be performed on 'Kinds.NS' because it is not visible from outside this assembly
                //     [CLSCompliant(true)] private struct NS { }
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnPrivateType, "NS").WithArguments("Kinds.NS"),
                // (26,48): warning CS3019: CLS compliance checking will not be performed on 'Kinds.ND' because it is not visible from outside this assembly
                //     [CLSCompliant(true)] private delegate void ND();
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnPrivateType, "ND").WithArguments("Kinds.ND"),
                // (31,38): warning CS3019: CLS compliance checking will not be performed on 'Levels.F1' because it is not visible from outside this assembly
                //     [CLSCompliant(true)] private int F1;
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnPrivateType, "F1").WithArguments("Levels.F1"),
                // (32,39): warning CS3019: CLS compliance checking will not be performed on 'Levels.F2' because it is not visible from outside this assembly
                //     [CLSCompliant(true)] internal int F2;
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnPrivateType, "F2").WithArguments("Levels.F2"),

                // (32,39): warning CS0649: Field 'Levels.F2' is never assigned to, and will always have its default value 0
                //     [CLSCompliant(true)] internal int F2;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F2").WithArguments("Levels.F2", "0"),
                // (31,38): warning CS0169: The field 'Levels.F1' is never used
                //     [CLSCompliant(true)] private int F1;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "F1").WithArguments("Levels.F1"),
                // (21,38): warning CS0169: The field 'Kinds.F' is never used
                //     [CLSCompliant(true)] private int F;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("Kinds.F"),
                // (20,43): warning CS0067: The event 'Kinds.E' is never used
                //     [CLSCompliant(true)] private event ND E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("Kinds.E"));
        }

        [Fact]
        public void WRN_CLS_IllegalTrueInFalse_Explicit()
        {
            var source = @"
using System;

[assembly:CLSCompliant(true)]

[CLSCompliant(false)]
public class Kinds
{
    [CLSCompliant(true)] public void M() { }
    [CLSCompliant(true)] public int P { get; set; }
    [CLSCompliant(true)] public event ND E { add { } remove { } }
    [CLSCompliant(true)] public int F;
                  
    [CLSCompliant(true)] public class NC { }
    [CLSCompliant(true)] public interface NI { }
    [CLSCompliant(true)] public struct NS { }
    [CLSCompliant(true)] public delegate void ND();
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,38): warning CS3018: 'Kinds.M()' cannot be marked as CLS-compliant because it is a member of non-CLS-compliant type 'Kinds'
                //     [CLSCompliant(true)] public void M() { }
                Diagnostic(ErrorCode.WRN_CLS_IllegalTrueInFalse, "M").WithArguments("Kinds.M()", "Kinds"),
                // (10,37): warning CS3018: 'Kinds.P' cannot be marked as CLS-compliant because it is a member of non-CLS-compliant type 'Kinds'
                //     [CLSCompliant(true)] public int P { get; set; }
                Diagnostic(ErrorCode.WRN_CLS_IllegalTrueInFalse, "P").WithArguments("Kinds.P", "Kinds"),
                // (11,42): warning CS3018: 'Kinds.E' cannot be marked as CLS-compliant because it is a member of non-CLS-compliant type 'Kinds'
                //     [CLSCompliant(true)] public event ND E;
                Diagnostic(ErrorCode.WRN_CLS_IllegalTrueInFalse, "E").WithArguments("Kinds.E", "Kinds"),
                // (12,37): warning CS3018: 'Kinds.F' cannot be marked as CLS-compliant because it is a member of non-CLS-compliant type 'Kinds'
                //     [CLSCompliant(true)] public int F;
                Diagnostic(ErrorCode.WRN_CLS_IllegalTrueInFalse, "F").WithArguments("Kinds.F", "Kinds"),
                // (14,39): warning CS3018: 'Kinds.NC' cannot be marked as CLS-compliant because it is a member of non-CLS-compliant type 'Kinds'
                //     [CLSCompliant(true)] public class NC { }
                Diagnostic(ErrorCode.WRN_CLS_IllegalTrueInFalse, "NC").WithArguments("Kinds.NC", "Kinds"),
                // (15,43): warning CS3018: 'Kinds.NI' cannot be marked as CLS-compliant because it is a member of non-CLS-compliant type 'Kinds'
                //     [CLSCompliant(true)] public interface NI { }
                Diagnostic(ErrorCode.WRN_CLS_IllegalTrueInFalse, "NI").WithArguments("Kinds.NI", "Kinds"),
                // (16,40): warning CS3018: 'Kinds.NS' cannot be marked as CLS-compliant because it is a member of non-CLS-compliant type 'Kinds'
                //     [CLSCompliant(true)] public struct NS { }
                Diagnostic(ErrorCode.WRN_CLS_IllegalTrueInFalse, "NS").WithArguments("Kinds.NS", "Kinds"),
                // (17,47): warning CS3018: 'Kinds.ND' cannot be marked as CLS-compliant because it is a member of non-CLS-compliant type 'Kinds'
                //     [CLSCompliant(true)] public delegate void ND();
                Diagnostic(ErrorCode.WRN_CLS_IllegalTrueInFalse, "ND").WithArguments("Kinds.ND", "Kinds"));
        }

        [Fact]
        public void WRN_CLS_IllegalTrueInFalse_Implicit()
        {
            var source = @"
using System;

[assembly:CLSCompliant(false)]

public class Kinds
{
    [CLSCompliant(true)] public void M() { }
    [CLSCompliant(true)] public int P { get; set; }
    [CLSCompliant(true)] public event ND E { add { } remove { } }
    [CLSCompliant(true)] public int F;
                  
    [CLSCompliant(true)] public class NC { }
    [CLSCompliant(true)] public interface NI { }
    [CLSCompliant(true)] public struct NS { }
    [CLSCompliant(true)] public delegate void ND();
}
";
            // No warnings, since assembly is marked false.
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void WRN_CLS_IllegalTrueInFalse_Alternating()
        {
            var source = @"
using System;

[assembly:CLSCompliant(true)]

[CLSCompliant(true)]
public class A
{
    [CLSCompliant(false)]
    public class B
    {
        [CLSCompliant(true)]
        public class C
        {
            [CLSCompliant(false)]
            public class D
            {
                [CLSCompliant(true)]
                public class E
                {
                }
            }
        }
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (13,22): warning CS3018: 'A.B.C' cannot be marked as CLS-compliant because it is a member of non-CLS-compliant type 'A.B'
                //         public class C
                Diagnostic(ErrorCode.WRN_CLS_IllegalTrueInFalse, "C").WithArguments("A.B.C", "A.B"),
                // (19,30): warning CS3018: 'A.B.C.D.E' cannot be marked as CLS-compliant because it is a member of non-CLS-compliant type 'A.B.C.D'
                //                 public class E
                Diagnostic(ErrorCode.WRN_CLS_IllegalTrueInFalse, "E").WithArguments("A.B.C.D.E", "A.B.C.D"));
        }

        [Fact]
        public void WRN_CLS_BadBase()
        {
            var source = @"
using System;

[assembly:CLSCompliant(true)]

public class A : Bad
{
}

public class B : Generic<int*[]>
{
}

[CLSCompliant(false)]
public class Bad
{
}

public class Generic<T>
{
}
";
            CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (6,14): warning CS3009: 'A': base type 'Bad' is not CLS-compliant
                // public class A : Bad
                Diagnostic(ErrorCode.WRN_CLS_BadBase, "A").WithArguments("A", "Bad"),
                // (10,14): warning CS3009: 'B': base type 'Generic<int*[]>' is not CLS-compliant
                // public class B : Generic<int*[]>
                Diagnostic(ErrorCode.WRN_CLS_BadBase, "B").WithArguments("B", "Generic<int*[]>"));
        }

        [Fact]
        public void WRN_CLS_BadBase_OtherAssemblies()
        {
            var libSource1 = @"
public class Bad1
{
}
";

            var libSource2 = @"
using System;

[assembly:CLSCompliant(true)]

[CLSCompliant(false)]
public class Bad2
{
}
";

            var libSource3 = @"
using System;

[assembly:CLSCompliant(false)]

public class Bad3
{
}
";

            var source = @"
using System;

[assembly:CLSCompliant(true)]

public class A1 : Bad1 { }
public class A2 : Bad2 { }
public class A3 : Bad3 { }

public class B1 : Generic<int*[]> { }
public class B2 : Generic<Bad2> { }

public class Generic<T> { }
";
            var lib1 = CreateCompilationWithMscorlib(libSource1, assemblyName: "lib1").EmitToImageReference();
            var lib2 = CreateCompilationWithMscorlib(libSource2, assemblyName: "lib2").EmitToImageReference();
            var lib3 = CreateCompilationWithMscorlib(libSource3, assemblyName: "lib3").EmitToImageReference();

            CreateCompilationWithMscorlib(source, new[] { lib1, lib2, lib3 }, TestOptions.ReleaseDll).VerifyDiagnostics(
                // (6,14): warning CS3009: 'A1': base type 'Bad1' is not CLS-compliant
                // public class A1 : Bad1 { }
                Diagnostic(ErrorCode.WRN_CLS_BadBase, "A1").WithArguments("A1", "Bad1"),
                // (7,14): warning CS3009: 'A2': base type 'Bad2' is not CLS-compliant
                // public class A2 : Bad2 { }
                Diagnostic(ErrorCode.WRN_CLS_BadBase, "A2").WithArguments("A2", "Bad2"),
                // (8,14): warning CS3009: 'A3': base type 'Bad3' is not CLS-compliant
                // public class A3 : Bad3 { }
                Diagnostic(ErrorCode.WRN_CLS_BadBase, "A3").WithArguments("A3", "Bad3"),
                // (10,14): warning CS3009: 'B1': base type 'Generic<int*[]>' is not CLS-compliant
                // public class B1 : Generic<int*[]> { }
                Diagnostic(ErrorCode.WRN_CLS_BadBase, "B1").WithArguments("B1", "Generic<int*[]>"),
                // (11,14): warning CS3009: 'B2': base type 'Generic<Bad2>' is not CLS-compliant
                // public class B2 : Generic<Bad2> { }
                Diagnostic(ErrorCode.WRN_CLS_BadBase, "B2").WithArguments("B2", "Generic<Bad2>"));
        }

        [Fact]
        public void WRN_CLS_BadInterface_Interface()
        {
            var source = @"
using System;

[assembly:CLSCompliant(true)]

public interface A : Bad { }

public interface B : Generic<int*[]> { }

public interface C : Good, Bad { }

public interface D : Bad, Good { }

public interface E : Bad, Generic<int*[]> { }

[CLSCompliant(true)]
public interface Good { }

[CLSCompliant(false)]
public interface Bad { }

public interface Generic<T> { }
";
            CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (6,18): warning CS3027: 'A' is not CLS-compliant because base interface 'Bad' is not CLS-compliant
                // public interface A : Bad { }
                Diagnostic(ErrorCode.WRN_CLS_BadInterface, "A").WithArguments("A", "Bad"),
                // (8,18): warning CS3027: 'B' is not CLS-compliant because base interface 'Generic<int*[]>' is not CLS-compliant
                // public interface B : Generic<int*[]> { }
                Diagnostic(ErrorCode.WRN_CLS_BadInterface, "B").WithArguments("B", "Generic<int*[]>"),
                // (10,18): warning CS3027: 'C' is not CLS-compliant because base interface 'Bad' is not CLS-compliant
                // public interface C : Good, Bad { }
                Diagnostic(ErrorCode.WRN_CLS_BadInterface, "C").WithArguments("C", "Bad"),
                // (12,18): warning CS3027: 'D' is not CLS-compliant because base interface 'Bad' is not CLS-compliant
                // public interface D : Bad, Good { }
                Diagnostic(ErrorCode.WRN_CLS_BadInterface, "D").WithArguments("D", "Bad"),
                // (14,18): warning CS3027: 'E' is not CLS-compliant because base interface 'Bad' is not CLS-compliant
                // public interface E : Bad, Generic<int*[]> { }
                Diagnostic(ErrorCode.WRN_CLS_BadInterface, "E").WithArguments("E", "Bad"),
                // (14,18): warning CS3027: 'E' is not CLS-compliant because base interface 'Generic<int*[]>' is not CLS-compliant
                // public interface E : Bad, Generic<int*[]> { }
                Diagnostic(ErrorCode.WRN_CLS_BadInterface, "E").WithArguments("E", "Generic<int*[]>"));
        }

        [Fact]
        public void WRN_CLS_BadInterface_Class()
        {
            var source = @"
using System;

[assembly:CLSCompliant(true)]

public class A : Bad { }

public class B : Generic<int*[]> { }

public class C : Good, Bad { }

public class D : Bad, Good { }

public class E : Bad, Generic<int*[]> { }

[CLSCompliant(true)]
public interface Good { }

[CLSCompliant(false)]
public interface Bad { }

public interface Generic<T> { }
";
            // Implemented interfaces are not required to be compliant - only inherited ones.
            CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void WRN_CLS_BadInterfaceMember()
        {
            var source = @"
using System;

[assembly:CLSCompliant(true)]

public interface A
{
    Bad M1();
    
    [CLSCompliant(false)] 
    void M2();
}

public interface Kinds
{
    [CLSCompliant(false)] void M();
    [CLSCompliant(false)] int P { get; set; }
    [CLSCompliant(false)] event Action E;
}

[CLSCompliant(false)]
public interface Bad { }
";
            // NOTE: only reported for member DECLARED to be non-compliant.
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (11,10): warning CS3010: 'A.M2()': CLS-compliant interfaces must have only CLS-compliant members
                //     void M2();
                Diagnostic(ErrorCode.WRN_CLS_BadInterfaceMember, "M2").WithArguments("A.M2()"),
                // (16,32): warning CS3010: 'Kinds.M()': CLS-compliant interfaces must have only CLS-compliant members
                //     [CLSCompliant(false)] void M();
                Diagnostic(ErrorCode.WRN_CLS_BadInterfaceMember, "M").WithArguments("Kinds.M()"),
                // (17,31): warning CS3010: 'Kinds.P': CLS-compliant interfaces must have only CLS-compliant members
                //     [CLSCompliant(false)] int P { get; set; }
                Diagnostic(ErrorCode.WRN_CLS_BadInterfaceMember, "P").WithArguments("Kinds.P"),
                // (18,40): warning CS3010: 'Kinds.E': CLS-compliant interfaces must have only CLS-compliant members
                //     [CLSCompliant(false)] event Action E;
                Diagnostic(ErrorCode.WRN_CLS_BadInterfaceMember, "E").WithArguments("Kinds.E"),

                // (8,9): warning CS3002: Return type of 'A.M1()' is not CLS-compliant
                //     Bad M1();
                Diagnostic(ErrorCode.WRN_CLS_BadReturnType, "M1").WithArguments("A.M1()"));
        }

        [Fact]
        public void WRN_CLS_NoAbstractMembers()
        {
            var source = @"
using System;

[assembly:CLSCompliant(true)]

public abstract class A
{
    // NOTE: not reported for members that fail to be compliant (but are not declared non-compliant).
    public abstract Bad M1();
    
    [CLSCompliant(false)] 
    public abstract void M2();
}

public abstract class Kinds
{
    [CLSCompliant(false)] public abstract void M();
    [CLSCompliant(false)] public abstract int P { get; set; }
    [CLSCompliant(false)] public abstract event Action E;

    // NOTE: not reported for classes.
    [CLSCompliant(false)] public abstract class NC { }
}

[CLSCompliant(false)]
public interface Bad { }
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,26): warning CS3011: 'A.M2()': only CLS-compliant members can be abstract
                //     public abstract void M2();
                Diagnostic(ErrorCode.WRN_CLS_NoAbstractMembers, "M2").WithArguments("A.M2()"),
                // (17,48): warning CS3011: 'Kinds.M()': only CLS-compliant members can be abstract
                //     [CLSCompliant(false)] public abstract void M();
                Diagnostic(ErrorCode.WRN_CLS_NoAbstractMembers, "M").WithArguments("Kinds.M()"),
                // (18,47): warning CS3011: 'Kinds.P': only CLS-compliant members can be abstract
                //     [CLSCompliant(false)] public abstract int P { get; set; }
                Diagnostic(ErrorCode.WRN_CLS_NoAbstractMembers, "P").WithArguments("Kinds.P"),
                // (19,56): warning CS3011: 'Kinds.E': only CLS-compliant members can be abstract
                //     [CLSCompliant(false)] public abstract event Action E;
                Diagnostic(ErrorCode.WRN_CLS_NoAbstractMembers, "E").WithArguments("Kinds.E"),

                // (9,25): warning CS3002: Return type of 'A.M1()' is not CLS-compliant
                //     public abstract Bad M1();
                Diagnostic(ErrorCode.WRN_CLS_BadReturnType, "M1").WithArguments("A.M1()"));
        }

        [Fact]
        public void WRN_CLS_VolatileField()
        {
            var source = @"
using System;

[assembly:CLSCompliant(true)]

public class A
{
    public volatile int F1;

    [CLSCompliant(false)]
    public volatile int F2;
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (8,25): warning CS3026: CLS-compliant field 'A.F1' cannot be volatile
                //     public volatile int F1;
                Diagnostic(ErrorCode.WRN_CLS_VolatileField, "F1").WithArguments("A.F1"));
        }

        [Fact]
        public void WRN_CLS_BadTypeVar()
        {
            var source = @"
using System;

[assembly:CLSCompliant(true)]

public class C1<T, U>
    where T : Good, Bad 
    where U : Bad, Good
{
}

[CLSCompliant(false)]
public class C2<T, U>
    where T : Good, Bad 
    where U : Bad, Good
{
}

public delegate void D1<T, U>()
    where T : Good, Bad 
    where U : Bad, Good;

[CLSCompliant(false)]
public delegate void D2<T, U>()
    where T : Good, Bad 
    where U : Bad, Good;

public class C
{
    public void M1<T, U>()
        where T : Good, Bad 
        where U : Bad, Good
    {
    }

    [CLSCompliant(false)]
    public void M2<T, U>()
        where T : Good, Bad 
        where U : Bad, Good
    {
    }
}

[CLSCompliant(true)]
public interface Good { }

[CLSCompliant(false)]
public interface Bad { }
";

            // NOTE: Dev11 reports all of these on the declaration of Bad, which seems
            // less than helpful.
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,17): warning CS3024: Constraint type 'Bad' is not CLS-compliant
                // public class C1<T, U>
                Diagnostic(ErrorCode.WRN_CLS_BadTypeVar, "T").WithArguments("Bad"),
                // (6,20): warning CS3024: Constraint type 'Bad' is not CLS-compliant
                // public class C1<T, U>
                Diagnostic(ErrorCode.WRN_CLS_BadTypeVar, "U").WithArguments("Bad"),
                // (19,25): warning CS3024: Constraint type 'Bad' is not CLS-compliant
                // public delegate void D1<T, U>()
                Diagnostic(ErrorCode.WRN_CLS_BadTypeVar, "T").WithArguments("Bad"),
                // (19,28): warning CS3024: Constraint type 'Bad' is not CLS-compliant
                // public delegate void D1<T, U>()
                Diagnostic(ErrorCode.WRN_CLS_BadTypeVar, "U").WithArguments("Bad"),
                // (30,20): warning CS3024: Constraint type 'Bad' is not CLS-compliant
                //     public void M1<T, U>()
                Diagnostic(ErrorCode.WRN_CLS_BadTypeVar, "T").WithArguments("Bad"),
                // (30,23): warning CS3024: Constraint type 'Bad' is not CLS-compliant
                //     public void M1<T, U>()
                Diagnostic(ErrorCode.WRN_CLS_BadTypeVar, "U").WithArguments("Bad"));
        }

        [Fact]
        public void WRN_CLS_NoVarArgs()
        {
            var source = @"
using System;

[assembly:CLSCompliant(true)]

public class C
{
    public void M1(__arglist) { }
    public void M1(int x, __arglist) { }

    [CLSCompliant(false)] public void M2(__arglist) { }
    [CLSCompliant(false)] public void M2(int x, __arglist) { }

    public int this[__arglist] { get { return 1; } set { } }
    public int this[int x, __arglist] { get { return 1; } set { } }
    public delegate void D1(__arglist);
    public delegate void D2(int x, __arglist);
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (8,17): warning CS3000: Methods with variable arguments are not CLS-compliant
                //     public void M1(__arglist) { }
                Diagnostic(ErrorCode.WRN_CLS_NoVarArgs, "M1"),
                // (9,17): warning CS3000: Methods with variable arguments are not CLS-compliant
                //     public void M1(int x, __arglist) { }
                Diagnostic(ErrorCode.WRN_CLS_NoVarArgs, "M1"),

                // (14,21): error CS1669: __arglist is not valid in this context
                //     public int this[__arglist] { get { return 1; } set { } }
                Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist"),
                // (15,28): error CS1669: __arglist is not valid in this context
                //     public int this[int x, __arglist] { get { return 1; } set { } }
                Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist"),
                // (17,36): error CS1669: __arglist is not valid in this context
                //     public delegate void D2(int x, __arglist);
                Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist"),
                // (16,29): error CS1669: __arglist is not valid in this context
                //     public delegate void D1(__arglist);
                Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist"));
        }

        [Fact]
        public void WRN_CLS_BadFieldPropType()
        {
            var source = @"
using System;

[assembly:CLSCompliant(true)]

public class Kinds1
{
    public Bad P { get; set; }
    public Bad F;
    public event BadD E1;
    public event BadD E2 { add { } remove { } }
}

[CLSCompliant(false)]
public class Kinds2
{
    public Bad P { get; set; }
    public Bad F;
    public event BadD E1;
    public event BadD E2 { add { } remove { } }
}

[CLSCompliant(false)]
public interface Bad { }

[CLSCompliant(false)]
public delegate void BadD();
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (8,16): warning CS3003: Type of 'Kinds1.P' is not CLS-compliant
                //     public Bad P { get; set; }
                Diagnostic(ErrorCode.WRN_CLS_BadFieldPropType, "P").WithArguments("Kinds1.P"),
                // (9,16): warning CS3003: Type of 'Kinds1.F' is not CLS-compliant
                //     public Bad F;
                Diagnostic(ErrorCode.WRN_CLS_BadFieldPropType, "F").WithArguments("Kinds1.F"),
                // (10,23): warning CS3003: Type of 'Kinds1.E1' is not CLS-compliant
                //     public event BadD E1;
                Diagnostic(ErrorCode.WRN_CLS_BadFieldPropType, "E1").WithArguments("Kinds1.E1"),
                // (11,23): warning CS3003: Type of 'Kinds1.E2' is not CLS-compliant
                //     public event BadD E2 { add { } remove { } }
                Diagnostic(ErrorCode.WRN_CLS_BadFieldPropType, "E2").WithArguments("Kinds1.E2"),

                // (10,23): warning CS0067: The event 'Kinds1.E1' is never used
                //     public event BadD E1;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E1").WithArguments("Kinds1.E1"),
                // (19,23): warning CS0067: The event 'Kinds2.E1' is never used
                //     public event BadD E1;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E1").WithArguments("Kinds2.E1"));
        }

        [Fact]
        public void WRN_CLS_BadReturnType()
        {
            var source = @"
using System;

[assembly:CLSCompliant(true)]

public unsafe class C1
{
    public Bad M1() { throw null; }
    public Generic<Bad> M2() { throw null; }
    public Generic<Generic<Bad>> M3() { throw null; }
    public Bad[] M4() { throw null; }
    public Bad[][] M5() { throw null; }
    public Bad[,] M6() { throw null; }
    public int* M7() { throw null; }
}

[CLSCompliant(false)]
public unsafe class C2
{
    public Bad M1() { throw null; }
    public Generic<Bad> M2() { throw null; }
    public Generic<Generic<Bad>> M3() { throw null; }
    public Bad[] M4() { throw null; }
    public Bad[][] M5() { throw null; }
    public Bad[,] M6() { throw null; }
    public int* M7() { throw null; }
}

public class Generic<T> { }

[CLSCompliant(false)]
public interface Bad { }
";

            CreateCompilationWithMscorlib(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,16): warning CS3002: Return type of 'C1.M1()' is not CLS-compliant
                //     public Bad M1() { throw null; }
                Diagnostic(ErrorCode.WRN_CLS_BadReturnType, "M1").WithArguments("C1.M1()"),
                // (9,25): warning CS3002: Return type of 'C1.M2()' is not CLS-compliant
                //     public Generic<Bad> M2() { throw null; }
                Diagnostic(ErrorCode.WRN_CLS_BadReturnType, "M2").WithArguments("C1.M2()"),
                // (10,34): warning CS3002: Return type of 'C1.M3()' is not CLS-compliant
                //     public Generic<Generic<Bad>> M3() { throw null; }
                Diagnostic(ErrorCode.WRN_CLS_BadReturnType, "M3").WithArguments("C1.M3()"),
                // (11,18): warning CS3002: Return type of 'C1.M4()' is not CLS-compliant
                //     public Bad[] M4() { throw null; }
                Diagnostic(ErrorCode.WRN_CLS_BadReturnType, "M4").WithArguments("C1.M4()"),
                // (12,20): warning CS3002: Return type of 'C1.M5()' is not CLS-compliant
                //     public Bad[][] M5() { throw null; }
                Diagnostic(ErrorCode.WRN_CLS_BadReturnType, "M5").WithArguments("C1.M5()"),
                // (13,19): warning CS3002: Return type of 'C1.M6()' is not CLS-compliant
                //     public Bad[,] M6() { throw null; }
                Diagnostic(ErrorCode.WRN_CLS_BadReturnType, "M6").WithArguments("C1.M6()"),
                // (14,17): warning CS3002: Return type of 'C1.M7()' is not CLS-compliant
                //     public int* M7() { throw null; }
                Diagnostic(ErrorCode.WRN_CLS_BadReturnType, "M7").WithArguments("C1.M7()"));
        }

        [Fact]
        public void WRN_CLS_BadReturnType_Delegate()
        {
            var source = @"
using System;

[assembly:CLSCompliant(true)]

public unsafe class C1
{
    public delegate Bad D();
}

[CLSCompliant(false)]
public unsafe class C2
{
    public delegate Bad D();
}

[CLSCompliant(false)]
public interface Bad { }
";

            CreateCompilationWithMscorlib(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,25): warning CS3002: Return type of 'C1.D' is not CLS-compliant
                //     public delegate Bad D();
                Diagnostic(ErrorCode.WRN_CLS_BadReturnType, "D").WithArguments("C1.D"));
        }

        [Fact]
        public void WRN_CLS_BadArgType()
        {
            var source = @"
using System;

[assembly:CLSCompliant(true)]

public unsafe class C1
{
    public void M1(Bad b) { }
    public void M2(Generic<Bad> b) { }
    public void M3(Generic<Generic<Bad>> b) { }
    public void M4(Bad[] b) { }
    public void M5(Bad[][] b) { }
    public void M6(Bad[,] b) { }
    public void M7(int* b) { }
}

[CLSCompliant(false)]
public unsafe class C2
{
    public void M1(Bad b) { }
    public void M2(Generic<Bad> b) { }
    public void M3(Generic<Generic<Bad>> b) { }
    public void M4(Bad[] b) { }
    public void M5(Bad[][] b) { }
    public void M6(Bad[,] b) { }
    public void M7(int* b) { }
}

public class Generic<T> { }

[CLSCompliant(false)]
public interface Bad { }
";

            CreateCompilationWithMscorlib(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,23): warning CS3001: Argument type 'Bad' is not CLS-compliant
                //     public void M1(Bad b) { }
                Diagnostic(ErrorCode.WRN_CLS_BadArgType, "b").WithArguments("Bad"),
                // (9,32): warning CS3001: Argument type 'Generic<Bad>' is not CLS-compliant
                //     public void M2(Generic<Bad> b) { }
                Diagnostic(ErrorCode.WRN_CLS_BadArgType, "b").WithArguments("Generic<Bad>"),
                // (10,41): warning CS3001: Argument type 'Generic<Generic<Bad>>' is not CLS-compliant
                //     public void M3(Generic<Generic<Bad>> b) { }
                Diagnostic(ErrorCode.WRN_CLS_BadArgType, "b").WithArguments("Generic<Generic<Bad>>"),
                // (11,25): warning CS3001: Argument type 'Bad[]' is not CLS-compliant
                //     public void M4(Bad[] b) { }
                Diagnostic(ErrorCode.WRN_CLS_BadArgType, "b").WithArguments("Bad[]"),
                // (12,27): warning CS3001: Argument type 'Bad[][]' is not CLS-compliant
                //     public void M5(Bad[][] b) { }
                Diagnostic(ErrorCode.WRN_CLS_BadArgType, "b").WithArguments("Bad[][]"),
                // (13,26): warning CS3001: Argument type 'Bad[*,*]' is not CLS-compliant
                //     public void M6(Bad[,] b) { }
                Diagnostic(ErrorCode.WRN_CLS_BadArgType, "b").WithArguments("Bad[*,*]"),
                // (14,24): warning CS3001: Argument type 'int*' is not CLS-compliant
                //     public void M7(int* b) { }
                Diagnostic(ErrorCode.WRN_CLS_BadArgType, "b").WithArguments("int*"));
        }

        [Fact]
        public void WRN_CLS_BadArgType_Kinds()
        {
            var source = @"
using System;

[assembly:CLSCompliant(true)]

public class C1
{
    public void M(Bad b) { }
    public int this[Bad b] { get { return 0; } set { } }
    public delegate void D(Bad b);
}

[CLSCompliant(false)]
public class C2
{
    public void M(Bad b) { }
    public int this[Bad b] { get { return 0; } set { } }
    public delegate void D(Bad b);
}

[CLSCompliant(false)]
public interface Bad { }
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (8,23): warning CS3001: Argument type 'Bad' is not CLS-compliant
                //     public void M(Bad b) { }
                Diagnostic(ErrorCode.WRN_CLS_BadArgType, "b").WithArguments("Bad"),
                // (9,25): warning CS3001: Argument type 'Bad' is not CLS-compliant
                //     public int this[Bad b] { get { return 0; } set { } }
                Diagnostic(ErrorCode.WRN_CLS_BadArgType, "b").WithArguments("Bad"),
                // (10,32): warning CS3001: Argument type 'Bad' is not CLS-compliant
                //     public delegate void D(Bad b);
                Diagnostic(ErrorCode.WRN_CLS_BadArgType, "b").WithArguments("Bad"));
        }

        // From LegacyTest\CSharp\Source\csharp\Source\ClsCompliance\generics\Rule_E_01.cs
        [Fact]
        public void WRN_CLS_BadArgType_ConstructedTypeAccessibility()
        {
            var source = @"
[assembly: System.CLSCompliant(true)]

public class C<T>
{
    protected class N { }
    protected void M1(C<int>.N n) { }	// Not CLS-compliant - C<int>.N not 
    // accessible from within C<T> in all languages
    protected void M2(C<T>.N n) { }	    // CLS-compliant - C<T>.N accessible inside C<T>

    protected class N2
    {
        protected void M1(C<ulong>.N n) { } // Not CLS-compliant
    }
}

public class D : C<long>
{
    protected void M3(C<int>.N n) { }	// Not CLS-compliant - C<int>.N is not
    // accessible in D (extends C<long>)
    protected void M4(C<long>.N n) { }	// CLS-compliant, C<long>.N is
    // accessible in D (extends C<long>)
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (7,32): warning CS3001: Argument type 'C<int>.N' is not CLS-compliant
                //     protected void M1(C<int>.N n) { }	// Not CLS-compliant - C<int>.N not 
                Diagnostic(ErrorCode.WRN_CLS_BadArgType, "n").WithArguments("C<int>.N"),
                // (13,38): warning CS3001: Argument type 'C<ulong>.N' is not CLS-compliant
                //         protected void M1(C<ulong>.N n) { }
                Diagnostic(ErrorCode.WRN_CLS_BadArgType, "n").WithArguments("C<ulong>.N"),
                // (19,32): warning CS3001: Argument type 'C<int>.N' is not CLS-compliant
                //     protected void M3(C<int>.N n) { }	// Not CLS-compliant - C<int>.N is not
                Diagnostic(ErrorCode.WRN_CLS_BadArgType, "n").WithArguments("C<int>.N"));
        }

        [Fact]
        public void WRN_CLS_BadArgType_ProtectedContainer()
        {
            var source = @"
using System;

[assembly: CLSCompliant(true)]

public class C1<T>
{
    protected class C2<U>
    {
        public class C3<V>
        {
            public void M<W>(C1<int>.C2<U> p) { } // CS3001
            public void M<W>(C1<int>.C2<U>.C3<V> p) { } // Roslyn reports CS3001, dev11 accepts
        }
    }
}
";

            // BREAK: dev11 incorrectly accepts the second method.
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,44): warning CS3001: Argument type 'C1<int>.C2<U>' is not CLS-compliant
                //             public void M<W>(C1<int>.C2<U> p) { } // CS3001
                Diagnostic(ErrorCode.WRN_CLS_BadArgType, "p").WithArguments("C1<int>.C2<U>"),
                // (13,50): warning CS3001: Argument type 'C1<int>.C2<U>.C3<V>' is not CLS-compliant
                //             public void M<W>(C1<int>.C2<U>.C3<V> p) { } // Roslyn reports CS3001, dev11 accepts
                Diagnostic(ErrorCode.WRN_CLS_BadArgType, "p").WithArguments("C1<int>.C2<U>.C3<V>"));
        }

        [Fact]
        public void WRN_CLS_MeaninglessOnParam()
        {
            var source = @"
using System;

[assembly:CLSCompliant(true)]

public class C1
{
    public void M([CLSCompliant(true)]int b) { }
    public delegate void D([CLSCompliant(true)]int b);

    public int this[[CLSCompliant(true)]int b]
    { 
        get { return 0; } 
        [param:CLSCompliant(true)] set { } 
    }

    public int P
    {
        get;
        [param:CLSCompliant(true)] set;
    }

    public event Action E
    {
        [param:CLSCompliant(true)] add { }
        [param:CLSCompliant(true)] remove { }
    }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (8,20): warning CS3022: CLSCompliant attribute has no meaning when applied to parameters. Try putting it on the method instead.
                //     public void M([CLSCompliant(true)]int b) { }
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnParam, "CLSCompliant(true)"),
                // (9,29): warning CS3022: CLSCompliant attribute has no meaning when applied to parameters. Try putting it on the method instead.
                //     public delegate void D([CLSCompliant(true)]int b);
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnParam, "CLSCompliant(true)"),
                // (11,22): warning CS3022: CLSCompliant attribute has no meaning when applied to parameters. Try putting it on the method instead.
                //     public int this[[CLSCompliant(true)]int b]
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnParam, "CLSCompliant(true)"),
                // (14,16): warning CS3022: CLSCompliant attribute has no meaning when applied to parameters. Try putting it on the method instead.
                //         [param:CLSCompliant(true)] set { } 
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnParam, "CLSCompliant(true)"),
                // (20,16): warning CS3022: CLSCompliant attribute has no meaning when applied to parameters. Try putting it on the method instead.
                //         [param:CLSCompliant(true)] set;
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnParam, "CLSCompliant(true)"),
                // (25,16): warning CS3022: CLSCompliant attribute has no meaning when applied to parameters. Try putting it on the method instead.
                //         [param:CLSCompliant(true)] add { }
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnParam, "CLSCompliant(true)"),
                // (26,16): warning CS3022: CLSCompliant attribute has no meaning when applied to parameters. Try putting it on the method instead.
                //         [param:CLSCompliant(true)] remove { }
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnParam, "CLSCompliant(true)"));
        }

        [Fact]
        public void WRN_CLS_MeaninglessOnParam_InNonCompliant()
        {
            var source = @"
using System;

[assembly:CLSCompliant(true)]

[CLSCompliant(false)]
public class C1
{
    public void M([CLSCompliant(true)]int b) { }
    public delegate void D([CLSCompliant(true)]int b);

    public int this[[CLSCompliant(true)]int b]
    { 
        get { return 0; } 
        [param:CLSCompliant(true)] set { } 
    }

    public int P
    {
        get;
        [param:CLSCompliant(true)] set;
    }

    public event Action E
    {
        [param:CLSCompliant(true)] add { }
        [param:CLSCompliant(true)] remove { }
    }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,20): warning CS3022: CLSCompliant attribute has no meaning when applied to parameters. Try putting it on the method instead.
                //     public void M([CLSCompliant(true)]int b) { }
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnParam, "CLSCompliant(true)"),
                // (10,29): warning CS3022: CLSCompliant attribute has no meaning when applied to parameters. Try putting it on the method instead.
                //     public delegate void D([CLSCompliant(true)]int b);
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnParam, "CLSCompliant(true)"),
                // (12,22): warning CS3022: CLSCompliant attribute has no meaning when applied to parameters. Try putting it on the method instead.
                //     public int this[[CLSCompliant(true)]int b]
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnParam, "CLSCompliant(true)"),
                // (15,16): warning CS3022: CLSCompliant attribute has no meaning when applied to parameters. Try putting it on the method instead.
                //         [param:CLSCompliant(true)] set { } 
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnParam, "CLSCompliant(true)"),
                // (21,16): warning CS3022: CLSCompliant attribute has no meaning when applied to parameters. Try putting it on the method instead.
                //         [param:CLSCompliant(true)] set;
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnParam, "CLSCompliant(true)"),
                // (26,16): warning CS3022: CLSCompliant attribute has no meaning when applied to parameters. Try putting it on the method instead.
                //         [param:CLSCompliant(true)] add { }
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnParam, "CLSCompliant(true)"),
                // (27,16): warning CS3022: CLSCompliant attribute has no meaning when applied to parameters. Try putting it on the method instead.
                //         [param:CLSCompliant(true)] remove { }
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnParam, "CLSCompliant(true)"));
        }

        [Fact]
        public void WRN_CLS_MeaninglessOnReturn()
        {
            var source = @"
using System;

[assembly:CLSCompliant(true)]

public class C1
{
    [return:CLSCompliant(true)]
    public void M() { }

    [return:CLSCompliant(true)]
    public delegate void D();

    public int this[int b]
    { 
        [return:CLSCompliant(true)] get { return 0; } 
        [return:CLSCompliant(true)] set { } 
    }

    public int P
    {
        [return:CLSCompliant(true)] get;
        [return:CLSCompliant(true)] set;
    }

    public event Action E
    {
        [return:CLSCompliant(true)] add { }
        [return:CLSCompliant(true)] remove { }
    }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (8,13): warning CS3023: CLSCompliant attribute has no meaning when applied to return types. Try putting it on the method instead.
                //     [return:CLSCompliant(true)]
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnReturn, "CLSCompliant(true)"),
                // (11,13): warning CS3023: CLSCompliant attribute has no meaning when applied to return types. Try putting it on the method instead.
                //     [return:CLSCompliant(true)]
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnReturn, "CLSCompliant(true)"),
                // (16,17): warning CS3023: CLSCompliant attribute has no meaning when applied to return types. Try putting it on the method instead.
                //         [return:CLSCompliant(true)] get { return 0; } 
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnReturn, "CLSCompliant(true)"),
                // (17,17): warning CS3023: CLSCompliant attribute has no meaning when applied to return types. Try putting it on the method instead.
                //         [return:CLSCompliant(true)] set { } 
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnReturn, "CLSCompliant(true)"),
                // (22,17): warning CS3023: CLSCompliant attribute has no meaning when applied to return types. Try putting it on the method instead.
                //         [return:CLSCompliant(true)] get;
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnReturn, "CLSCompliant(true)"),
                // (23,17): warning CS3023: CLSCompliant attribute has no meaning when applied to return types. Try putting it on the method instead.
                //         [return:CLSCompliant(true)] set;
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnReturn, "CLSCompliant(true)"),
                // (28,17): warning CS3023: CLSCompliant attribute has no meaning when applied to return types. Try putting it on the method instead.
                //         [return:CLSCompliant(true)] add { }
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnReturn, "CLSCompliant(true)"),
                // (29,17): warning CS3023: CLSCompliant attribute has no meaning when applied to return types. Try putting it on the method instead.
                //         [return:CLSCompliant(true)] remove { }
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnReturn, "CLSCompliant(true)"));
        }

        [Fact]
        public void WRN_CLS_MeaninglessOnReturn_InNonCompliant()
        {
            var source = @"
using System;

[assembly:CLSCompliant(true)]

[CLSCompliant(false)]
public class C1
{
    [return:CLSCompliant(true)]
    public void M() { }

    [return:CLSCompliant(true)]
    public delegate void D();

    public int this[int b]
    { 
        [return:CLSCompliant(true)] get { return 0; } 
        [return:CLSCompliant(true)] set { } 
    }

    public int P
    {
        [return:CLSCompliant(true)] get;
        [return:CLSCompliant(true)] set;
    }

    public event Action E
    {
        [return:CLSCompliant(true)] add { }
        [return:CLSCompliant(true)] remove { }
    }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,13): warning CS3023: CLSCompliant attribute has no meaning when applied to return types. Try putting it on the method instead.
                //     [return:CLSCompliant(true)]
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnReturn, "CLSCompliant(true)"),
                // (12,13): warning CS3023: CLSCompliant attribute has no meaning when applied to return types. Try putting it on the method instead.
                //     [return:CLSCompliant(true)]
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnReturn, "CLSCompliant(true)"),
                // (17,17): warning CS3023: CLSCompliant attribute has no meaning when applied to return types. Try putting it on the method instead.
                //         [return:CLSCompliant(true)] get { return 0; } 
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnReturn, "CLSCompliant(true)"),
                // (18,17): warning CS3023: CLSCompliant attribute has no meaning when applied to return types. Try putting it on the method instead.
                //         [return:CLSCompliant(true)] set { } 
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnReturn, "CLSCompliant(true)"),
                // (23,17): warning CS3023: CLSCompliant attribute has no meaning when applied to return types. Try putting it on the method instead.
                //         [return:CLSCompliant(true)] get;
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnReturn, "CLSCompliant(true)"),
                // (24,17): warning CS3023: CLSCompliant attribute has no meaning when applied to return types. Try putting it on the method instead.
                //         [return:CLSCompliant(true)] set;
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnReturn, "CLSCompliant(true)"),
                // (29,17): warning CS3023: CLSCompliant attribute has no meaning when applied to return types. Try putting it on the method instead.
                //         [return:CLSCompliant(true)] add { }
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnReturn, "CLSCompliant(true)"),
                // (30,17): warning CS3023: CLSCompliant attribute has no meaning when applied to return types. Try putting it on the method instead.
                //         [return:CLSCompliant(true)] remove { }
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnReturn, "CLSCompliant(true)"));
        }

        [Fact]
        public void WRN_CLS_BadAttributeType()
        {
            var source = @"
using System;

[assembly:CLSCompliant(true)]

public class EmptyAttribute : Attribute
{
    // Fine since there is an implicit default constructor.
}

public class PublicAttribute : Attribute
{
    // No good - not accessible.
    internal PublicAttribute() { }

    // No good - not compliant.
    [CLSCompliant(false)]
    public PublicAttribute(int x) { }

    // No good - array argument.
    public PublicAttribute(int[,] a) { }

    // No good - array argument.
    public PublicAttribute(params char[] a) { }
}

internal class InternalAttribute : Attribute
{
    // Fine, since type isn't accessible.
    public InternalAttribute(int[] array) { }
}

[CLSCompliant(false)]
public class BadAttribute : Attribute
{
    // Fine, since type isn't compliant.
    public BadAttribute(int[] array) { }
}

public class NotAnAttribute
{
    // Fine, since type isn't an attribute type.
    public NotAnAttribute(int[] array) { }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (11,14): warning CS3015: 'PublicAttribute' has no accessible constructors which use only CLS-compliant types
                // public class PublicAttribute : Attribute
                Diagnostic(ErrorCode.WRN_CLS_BadAttributeType, "PublicAttribute").WithArguments("PublicAttribute"));
        }

        [Fact]
        public void WRN_CLS_BadAttributeType_NonArray()
        {
            var source = @"
using System;

[assembly:CLSCompliant(true)]

public class MyAttribute : Attribute
{
    public MyAttribute(MyAttribute a) { }
}
";
            // CLS only allows System.Type, string, char, bool, byte, short, int, long, float, double, and enums.
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,14): warning CS3015: 'MyAttribute' has no accessible constructors which use only CLS-compliant types
                // public class MyAttribute : Attribute
                Diagnostic(ErrorCode.WRN_CLS_BadAttributeType, "MyAttribute").WithArguments("MyAttribute"));
        }

        [Fact]
        public void WRN_CLS_ArrayArgumentToAttribute()
        {
            var source = @"
using System;

[assembly:CLSCompliant(true)]

[CLSCompliant(false)]
public class ArrayAttribute : Attribute
{
    public ArrayAttribute(int[] array) { }
}

internal class InternalArrayAttribute : Attribute
{
    public InternalArrayAttribute(int[] array) { }
}

public class ObjectAttribute : Attribute
{
    public ObjectAttribute(object array) { }
}

public class NamedArgumentAttribute : Attribute
{
    public object O { get; set; }
}

[Array(new int[] { 1 })]
public class A { }

[Object(new int[] { 1 })] // Parameter type doesn't matter.
public class B { }

[InternalArray(new int[] { 1 })] // Accessibility doesn't matter.
public class C { }

[NamedArgument(O = new int[] { 1 })] // Applies to named arguments.
public class D { }
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (27,2): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                // [Array(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Array(new int[] { 1 })"),
                // (30,2): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                // [Object(new int[] { 1 })] // Parameter type doesn't matter.
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Object(new int[] { 1 })"),
                // (33,2): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                // [InternalArray(new int[] { 1 })] // Accessibility doesn't matter.
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "InternalArray(new int[] { 1 })"),
                // (36,2): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                // [NamedArgument(O = new int[] { 1 })] // Applies to named arguments.
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "NamedArgument(O = new int[] { 1 })"));
        }

        [Fact]
        public void WRN_CLS_ArrayArgumentToAttribute_Locations()
        {
            var source = @"
using System;

[assembly: CLSCompliant(true)]

[assembly: Object(new int[] { 1 })]
[module: Object(new int[] { 1 })]

public class ObjectAttribute : Attribute
{
    public ObjectAttribute(object array) { }
}

[Object(new int[] { 1 })]
public class Kinds
{
    [Object(new int[] { 1 })]
    [return: Object(new int[] { 1 })]
    public void M([Object(new int[] { 1 })] int x) { }

    [Object(new int[] { 1 })]
    public int this[[Object(new int[] { 1 })] int x]
    {
        [Object(new int[] { 1 })]
        [return: Object(new int[] { 1 })]
        get { return 0; }

        [Object(new int[] { 1 })]
        [param: Object(new int[] { 1 })]
        [return: Object(new int[] { 1 })]
        set { }
    }

    [Object(new int[] { 1 })]
    public int P
    {
        [Object(new int[] { 1 })]
        [return: Object(new int[] { 1 })]
        get;

        [Object(new int[] { 1 })]
        [param: Object(new int[] { 1 })]
        [return: Object(new int[] { 1 })]
        set;
    }

    [Object(new int[] { 1 })]
    [field: Object(new int[] { 1 })]
    [method: Object(new int[] { 1 })]
    public event ND E1;

    [Object(new int[] { 1 })]
    public event ND E2
    {
        [Object(new int[] { 1 })]
        [param: Object(new int[] { 1 })]
        [return: Object(new int[] { 1 })]
        add { }

        [Object(new int[] { 1 })]
        [param: Object(new int[] { 1 })]
        [return: Object(new int[] { 1 })]
        remove { }
    }

    [Object(new int[] { 1 })]
    public int F;

    [Object(new int[] { 1 })]
    public class NC { }

    [Object(new int[] { 1 })]
    public interface NI { }

    [Object(new int[] { 1 })]
    public struct NS { }

    [Object(new int[] { 1 })]
    [return: Object(new int[] { 1 })]
    public delegate void ND();
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // Assembly:

                // (6,12): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                // [assembly: Object(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Object(new int[] { 1 })"),

                // Module:

                // (7,10): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                // [module: Object(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Object(new int[] { 1 })"),

                // Declarations:

                // (14,2): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                // [Object(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Object(new int[] { 1 })"),
                // (17,6): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                //     [Object(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Object(new int[] { 1 })"),
                // (18,14): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                //     [return: Object(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Object(new int[] { 1 })"),
                // (21,6): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                //     [Object(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Object(new int[] { 1 })"),
                // (24,10): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                //         [Object(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Object(new int[] { 1 })"),
                // (25,18): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                //         [return: Object(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Object(new int[] { 1 })"),
                // (28,10): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                //         [Object(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Object(new int[] { 1 })"),
                // (30,18): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                //         [return: Object(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Object(new int[] { 1 })"),
                // (34,6): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                //     [Object(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Object(new int[] { 1 })"),
                // (37,10): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                //         [Object(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Object(new int[] { 1 })"),
                // (38,18): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                //         [return: Object(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Object(new int[] { 1 })"),
                // (41,10): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                //         [Object(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Object(new int[] { 1 })"),
                // (43,18): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                //         [return: Object(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Object(new int[] { 1 })"),
                // (47,6): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                //     [Object(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Object(new int[] { 1 })"),
                // (52,6): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                //     [Object(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Object(new int[] { 1 })"),
                // (55,10): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                //         [Object(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Object(new int[] { 1 })"),
                // (57,18): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                //         [return: Object(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Object(new int[] { 1 })"),
                // (60,10): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                //         [Object(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Object(new int[] { 1 })"),
                // (62,18): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                //         [return: Object(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Object(new int[] { 1 })"),
                // (66,6): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                //     [Object(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Object(new int[] { 1 })"),
                // (69,6): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                //     [Object(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Object(new int[] { 1 })"),
                // (72,6): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                //     [Object(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Object(new int[] { 1 })"),
                // (75,6): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                //     [Object(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Object(new int[] { 1 })"),
                // (78,6): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                //     [Object(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Object(new int[] { 1 })"),

                // Not interesting:

                // (50,21): warning CS0067: The event 'Kinds.E1' is never used
                //     public event ND E1;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E1").WithArguments("Kinds.E1"));
        }

        [Fact]
        public void WRN_CLS_BadIdentifier()
        {
            var source = @"
[assembly:System.CLSCompliant(true)]

public class _A { }
public class \u005FB { }
public class C_ { }
public class D\u005F { }
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (4,14): warning CS3008: Identifier '_A' is not CLS-compliant
                // public class _A { }
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifier, "_A").WithArguments("_A"),
                // (5,14): warning CS3008: Identifier '_B' is not CLS-compliant
                // public class \u005FB { }
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifier, @"\u005FB").WithArguments("_B"));
        }

        [Fact]
        public void WRN_CLS_BadIdentifier_Kinds()
        {
            var source = @"
[assembly:System.CLSCompliant(true)]

public class Kinds
{
    public void _M() { }
    public int _P { get; set; }
    public event _ND _E;
    public int _F;
    
    public class _NC { }
    public interface _NI { }
    public struct _NS { }
    public delegate void _ND();

    private int _Private;
    
    [System.CLSCompliant(false)]
    public int _NonCompliant;
}

namespace _NS1 { }
namespace NS1._NS2 { }
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (22,11): warning CS3008: Identifier '_NS1' is not CLS-compliant
                // namespace _NS1 { }
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifier, "_NS1").WithArguments("_NS1"),
                // (23,15): warning CS3008: Identifier '_NS2' is not CLS-compliant
                // namespace NS1._NS2 { }
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifier, "_NS2").WithArguments("_NS2"),
                // (6,17): warning CS3008: Identifier '_M' is not CLS-compliant
                //     public void _M() { }
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifier, "_M").WithArguments("_M"),
                // (7,16): warning CS3008: Identifier '_P' is not CLS-compliant
                //     public int _P { get; set; }
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifier, "_P").WithArguments("_P"),
                // (8,22): warning CS3008: Identifier '_E' is not CLS-compliant
                //     public event _ND _E;
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifier, "_E").WithArguments("_E"),
                // (9,16): warning CS3008: Identifier '_F' is not CLS-compliant
                //     public int _F;
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifier, "_F").WithArguments("_F"),
                // (11,18): warning CS3008: Identifier '_NC' is not CLS-compliant
                //     public class _NC { }
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifier, "_NC").WithArguments("_NC"),
                // (12,22): warning CS3008: Identifier '_NI' is not CLS-compliant
                //     public interface _NI { }
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifier, "_NI").WithArguments("_NI"),
                // (13,19): warning CS3008: Identifier '_NS' is not CLS-compliant
                //     public struct _NS { }
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifier, "_NS").WithArguments("_NS"),
                // (14,26): warning CS3008: Identifier '_ND' is not CLS-compliant
                //     public delegate void _ND();
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifier, "_ND").WithArguments("_ND"),

                // Not interesting:

                // (16,17): warning CS0169: The field 'Kinds._Private' is never used
                //     private int _Private;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "_Private").WithArguments("Kinds._Private"),
                // (8,22): warning CS0067: The event 'Kinds._E' is never used
                //     public event _ND _E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "_E").WithArguments("Kinds._E"));
        }

        [Fact]
        public void WRN_CLS_BadIdentifier_Overrides()
        {
            var source = @"
[assembly:System.CLSCompliant(true)]

public class Base
{
    public virtual void _M() { }
}

public class Derived : Base
{
    public override void _M() { } // Not reported
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,25): warning CS3008: Identifier '_M' is not CLS-compliant
                //     public virtual void _M() { }
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifier, "_M").WithArguments("_M"));
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void WRN_CLS_BadIdentifier_NotReferencable()
        {
            var il = @"
.class public abstract auto ansi B
{
  .custom instance void [mscorlib]System.CLSCompliantAttribute::.ctor(bool) = {bool(true)}

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  _getter() cil managed
  {
  }

  .property instance int32 P()
  {
    .get instance int32 B::_getter()
  }
}
";

            var source = @"
[assembly:System.CLSCompliant(true)]

public class C : B
{
    public override int P { get { return 0; } }
}
";

            var comp = CreateCompilationWithCustomILSource(source, il);
            comp.VerifyDiagnostics();

            var accessor = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<PropertySymbol>("P").GetMethod;
            Assert.True(accessor.Name[0] == '_');
        }

        [Fact]
        public void WRN_CLS_BadIdentifier_Parameter()
        {
            var source = @"
[assembly:System.CLSCompliant(true)]

public class C
{
    public void M(int _p) { }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void WRN_CLS_BadIdentifierCase()
        {
            var source = @"
[assembly:System.CLSCompliant(true)]

public class A { }
public class a { }
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (5,14): warning CS3005: Identifier 'a' differing only in case is not CLS-compliant
                // public class a { }
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifierCase, "a").WithArguments("a"));
        }

        [Fact]
        public void WRN_CLS_BadIdentifierCase_Arity()
        {
            var source = @"
[assembly:System.CLSCompliant(true)]

public class A { }
public class a<T> { } //CS3005

public class B { }
public class B<T> { } //Fine (since identical name)
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (5,14): warning CS3005: Identifier 'a<T>' differing only in case is not CLS-compliant
                // public class a<T> { } //CS3005
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifierCase, "a").WithArguments("a<T>"));
        }

        [Fact]
        public void WRN_CLS_BadIdentifierCase_Methods()
        {
            var source = @"
using System;

[assembly: CLSCompliant(true)]

public class C
{
    public void M() { }
    public void m() { }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,17): warning CS3005: Identifier 'C.m()' differing only in case is not CLS-compliant
                //     public void m() { }
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifierCase, "m").WithArguments("C.m()"));
        }

        [Fact]
        public void WRN_CLS_BadIdentifierCase_Properties()
        {
            var source = @"
using System;

[assembly: CLSCompliant(true)]

public class C
{
    public int P { get; set; }
    public int p { get; set; }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,16): warning CS3005: Identifier 'C.p' differing only in case is not CLS-compliant
                //     public int p { get; set; }
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifierCase, "p").WithArguments("C.p"),
                // (9,20): warning CS3005: Identifier 'C.p.get' differing only in case is not CLS-compliant
                //     public int p { get; set; }
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifierCase, "get").WithArguments("C.p.get"),
                // (9,25): warning CS3005: Identifier 'C.p.set' differing only in case is not CLS-compliant
                //     public int p { get; set; }
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifierCase, "set").WithArguments("C.p.set"));
        }

        [Fact]
        public void WRN_CLS_BadIdentifierCase_SpecialMethodNames()
        {
            var source = @"
using System;

[assembly: CLSCompliant(true)]

public class C
{
    ~C() { }
    public void finalize() { }

    public static explicit operator int(C c) { throw null; }
    public static int op_explicit(C c) { throw null; }

    public static implicit operator char(C c) { throw null; }
    public static char op_implicit(C c) { throw null; }

    public static C operator +(C c) {  throw null; }
    public static C op_unaryplus(C c) { throw null; }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,17): warning CS3005: Identifier 'C.finalize()' differing only in case is not CLS-compliant
                //     public void finalize() { }
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifierCase, "finalize").WithArguments("C.finalize()"),
                // (12,23): warning CS3005: Identifier 'C.op_explicit(C)' differing only in case is not CLS-compliant
                //     public static int op_explicit(C c) { throw null; }
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifierCase, "op_explicit").WithArguments("C.op_explicit(C)"),
                // (15,24): warning CS3005: Identifier 'C.op_implicit(C)' differing only in case is not CLS-compliant
                //     public static char op_implicit(C c) { throw null; }
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifierCase, "op_implicit").WithArguments("C.op_implicit(C)"),
                // (18,21): warning CS3005: Identifier 'C.op_unaryplus(C)' differing only in case is not CLS-compliant
                //     public static C op_unaryplus(C c) { throw null; }
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifierCase, "op_unaryplus").WithArguments("C.op_unaryplus(C)"));
        }

        [Fact]
        public void WRN_CLS_BadIdentifierCase_Accessors()
        {
            var source = @"
using System;

[assembly: CLSCompliant(true)]

public class C
{
    public int this[int x] { get { return 0; } set { } }
    public void get_item() { } // NOTE: signature doesn't match
    public void set_item() { } // NOTE: signature doesn't match

    public int P { get; set; }
    public void get_p() { } // NOTE: signature doesn't match
    public void set_p() { } // NOTE: signature doesn't match
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,17): warning CS3005: Identifier 'C.get_item()' differing only in case is not CLS-compliant
                //     public void get_item() { } // NOTE: signature doesn't match
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifierCase, "get_item").WithArguments("C.get_item()"),
                // (10,17): warning CS3005: Identifier 'C.set_item()' differing only in case is not CLS-compliant
                //     public void set_item() { } // NOTE: signature doesn't match
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifierCase, "set_item").WithArguments("C.set_item()"),
                // (13,17): warning CS3005: Identifier 'C.get_p()' differing only in case is not CLS-compliant
                //     public void get_p() { } // NOTE: signature doesn't match
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifierCase, "get_p").WithArguments("C.get_p()"),
                // (14,17): warning CS3005: Identifier 'C.set_p()' differing only in case is not CLS-compliant
                //     public void set_p() { } // NOTE: signature doesn't match
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifierCase, "set_p").WithArguments("C.set_p()"));
        }

        [WorkItem(717146, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/717146")]
        [Fact]
        public void WRN_CLS_BadIdentifierCase_Accessors2()
        {
            var source = @"
using System;

[assembly: CLSCompliant(true)]

public class C
{
    [CLSCompliant(false)]
    public int P { get; set; }

    public int p { get; set; }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void WRN_CLS_BadIdentifierCase_MethodVersusProperty()
        {
            var source = @"
using System;

[assembly: CLSCompliant(true)]

public class C
{
    public int P { get; set; }
    public void p(int x) { } // NOTE: signature doesn't match
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,17): warning CS3005: Identifier 'C.p(int)' differing only in case is not CLS-compliant
                //     public void p(int x) { } // NOTE: signature doesn't match
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifierCase, "p").WithArguments("C.p(int)"));
        }

        [Fact]
        public void WRN_CLS_NotOnModules()
        {
            CreateCompilationWithMscorlib("[module:System.CLSCompliant(true)]").VerifyDiagnostics(
                // (1,9): warning CS3012: You must specify the CLSCompliant attribute on the assembly, not the module, to enable CLS compliance checking
                // [module:System.CLSCompliant(true)]
                Diagnostic(ErrorCode.WRN_CLS_NotOnModules, "System.CLSCompliant(true)"));

            CreateCompilationWithMscorlib("[module:System.CLSCompliant(false)]").VerifyDiagnostics(
                // (1,9): warning CS3012: You must specify the CLSCompliant attribute on the assembly, not the module, to enable CLS compliance checking
                // [module:System.CLSCompliant(false)]
                Diagnostic(ErrorCode.WRN_CLS_NotOnModules, "System.CLSCompliant(false)"));
        }

        [Fact]
        public void WRN_CLS_NotOnModules2()
        {
            var sourceTemplate = @"
[assembly:System.CLSCompliant({0})]
[module:System.CLSCompliant({1})]
";

            CreateCompilationWithMscorlib(string.Format(sourceTemplate, "true", "true")).VerifyDiagnostics();
            CreateCompilationWithMscorlib(string.Format(sourceTemplate, "false", "false")).VerifyDiagnostics();

            CreateCompilationWithMscorlib(string.Format(sourceTemplate, "true", "false")).VerifyDiagnostics(
                // (3,9): warning CS3017: You cannot specify the CLSCompliant attribute on a module that differs from the CLSCompliant attribute on the assembly
                // [module:System.CLSCompliant(false)]
                Diagnostic(ErrorCode.WRN_CLS_NotOnModules2, "System.CLSCompliant(false)"));
            CreateCompilationWithMscorlib(string.Format(sourceTemplate, "false", "true")).VerifyDiagnostics(); // No warnings, since false.
        }

        [Fact]
        public void WRN_CLS_ModuleMissingCLS()
        {
            var trueModuleRef = CreateCompilationWithMscorlib("[assembly:System.CLSCompliant(true)][module:System.CLSCompliant(true)]", options: TestOptions.ReleaseModule, assemblyName: "true").EmitToImageReference();
            var falseModuleRef = CreateCompilationWithMscorlib("[assembly:System.CLSCompliant(false)][module:System.CLSCompliant(false)]", options: TestOptions.ReleaseModule, assemblyName: "false").EmitToImageReference();
            var noneModuleRef = CreateCompilationWithMscorlib("", options: TestOptions.ReleaseModule, assemblyName: "none").EmitToImageReference();

            // Assembly is marked compliant.
            CreateCompilationWithMscorlib("[assembly:System.CLSCompliant(true)]", new[] { trueModuleRef }).VerifyDiagnostics();
            CreateCompilationWithMscorlib("[assembly:System.CLSCompliant(true)]", new[] { falseModuleRef }).VerifyDiagnostics(
                // CONSIDER: dev11 actually reports CS0647 (failure to emit duplicate)
                // error CS7061: Duplicate 'CLSCompliantAttribute' attribute in 'false.netmodule'
                Diagnostic(ErrorCode.ERR_DuplicateAttributeInNetModule).WithArguments("CLSCompliantAttribute", "false.netmodule"),
                // false.netmodule: warning CS3017: You cannot specify the CLSCompliant attribute on a module that differs from the CLSCompliant attribute on the assembly
                Diagnostic(ErrorCode.WRN_CLS_NotOnModules2));

            CreateCompilationWithMscorlib("[assembly:System.CLSCompliant(true)]", new[] { noneModuleRef }).VerifyDiagnostics(
                // none.netmodule: warning CS3013: Added modules must be marked with the CLSCompliant attribute to match the assembly
                Diagnostic(ErrorCode.WRN_CLS_ModuleMissingCLS));

            // Assembly is marked non-compliant.
            CreateCompilationWithMscorlib("[assembly:System.CLSCompliant(false)]", new[] { trueModuleRef }).VerifyDiagnostics(
                // CONSIDER: dev11 actually reports CS0647 (failure to emit duplicate)
                // error CS7061: Duplicate 'CLSCompliantAttribute' attribute in 'true.netmodule'
                Diagnostic(ErrorCode.ERR_DuplicateAttributeInNetModule).WithArguments("CLSCompliantAttribute", "true.netmodule"));

            CreateCompilationWithMscorlib("[assembly:System.CLSCompliant(false)]", new[] { falseModuleRef }).VerifyDiagnostics(); //CONSIDER: dev11 reports WRN_CLS_NotOnModules (don't know why)
            CreateCompilationWithMscorlib("[assembly:System.CLSCompliant(false)]", new[] { noneModuleRef }).VerifyDiagnostics();

            // Assembly is unmarked.
            CreateCompilationWithMscorlib("", new[] { trueModuleRef }).VerifyDiagnostics();
            CreateCompilationWithMscorlib("", new[] { falseModuleRef }).VerifyDiagnostics();
            CreateCompilationWithMscorlib("", new[] { noneModuleRef }).VerifyDiagnostics();
        }

        [Fact]
        public void WRN_CLS_ModuleMissingCLS_AssemblyLevelOnly()
        {
            var trueModuleRef = CreateCompilationWithMscorlib("[assembly:System.CLSCompliant(true)]", options: TestOptions.ReleaseModule, assemblyName: "true").EmitToImageReference();
            var falseModuleRef = CreateCompilationWithMscorlib("[assembly:System.CLSCompliant(false)]", options: TestOptions.ReleaseModule, assemblyName: "false").EmitToImageReference();
            var noneModuleRef = CreateCompilationWithMscorlib("", options: TestOptions.ReleaseModule, assemblyName: "none").EmitToImageReference();

            // Assembly is marked compliant.
            CreateCompilationWithMscorlib("[assembly:System.CLSCompliant(true)]", new[] { trueModuleRef }).VerifyDiagnostics();
            CreateCompilationWithMscorlib("[assembly:System.CLSCompliant(true)]", new[] { falseModuleRef }).VerifyDiagnostics(
                // CONSIDER: dev11 actually reports CS0647 (failure to emit duplicate)
                // error CS7061: Duplicate 'CLSCompliantAttribute' attribute in 'false.netmodule'
                Diagnostic(ErrorCode.ERR_DuplicateAttributeInNetModule).WithArguments("CLSCompliantAttribute", "false.netmodule"));

            CreateCompilationWithMscorlib("[assembly:System.CLSCompliant(true)]", new[] { noneModuleRef }).VerifyDiagnostics(
                // none.netmodule: warning CS3013: Added modules must be marked with the CLSCompliant attribute to match the assembly
                Diagnostic(ErrorCode.WRN_CLS_ModuleMissingCLS));

            // Assembly is marked non-compliant.
            CreateCompilationWithMscorlib("[assembly:System.CLSCompliant(false)]", new[] { trueModuleRef }).VerifyDiagnostics(
                // CONSIDER: dev11 actually reports CS0647 (failure to emit duplicate)
                // error CS7061: Duplicate 'CLSCompliantAttribute' attribute in 'true.netmodule'
                Diagnostic(ErrorCode.ERR_DuplicateAttributeInNetModule).WithArguments("CLSCompliantAttribute", "true.netmodule"));

            CreateCompilationWithMscorlib("[assembly:System.CLSCompliant(false)]", new[] { falseModuleRef }).VerifyDiagnostics(); //CONSIDER: dev11 reports WRN_CLS_NotOnModules (don't know why)
            CreateCompilationWithMscorlib("[assembly:System.CLSCompliant(false)]", new[] { noneModuleRef }).VerifyDiagnostics();

            // Assembly is unmarked.
            CreateCompilationWithMscorlib("", new[] { trueModuleRef }).VerifyDiagnostics();
            CreateCompilationWithMscorlib("", new[] { falseModuleRef }).VerifyDiagnostics();
            CreateCompilationWithMscorlib("", new[] { noneModuleRef }).VerifyDiagnostics();
        }

        [Fact]
        public void MultipleDisagreeingModules()
        {
            var trueModuleRef = CreateCompilationWithMscorlib("[assembly:System.CLSCompliant(true)][module:System.CLSCompliant(true)]", options: TestOptions.ReleaseModule, assemblyName: "true").EmitToImageReference();
            var falseModuleRef = CreateCompilationWithMscorlib("[assembly:System.CLSCompliant(false)][module:System.CLSCompliant(false)]", options: TestOptions.ReleaseModule, assemblyName: "false").EmitToImageReference();

            CreateCompilationWithMscorlib("[assembly:System.CLSCompliant(true)]", new[] { trueModuleRef, falseModuleRef }).VerifyDiagnostics(
                // CONSIDER: dev11 actually reports CS0647 (failure to emit duplicate)
                // error CS7061: Duplicate 'CLSCompliantAttribute' attribute in 'false.netmodule'
                Diagnostic(ErrorCode.ERR_DuplicateAttributeInNetModule).WithArguments("CLSCompliantAttribute", "false.netmodule"),
                // false.netmodule: warning CS3017: You cannot specify the CLSCompliant attribute on a module that differs from the CLSCompliant attribute on the assembly
                Diagnostic(ErrorCode.WRN_CLS_NotOnModules2));

            CreateCompilationWithMscorlib("[assembly:System.CLSCompliant(false)]", new[] { trueModuleRef, falseModuleRef }).VerifyDiagnostics(
                // CONSIDER: dev11 actually reports CS0647 (failure to emit duplicate)
                // error CS7061: Duplicate 'CLSCompliantAttribute' attribute in 'true.netmodule'
                Diagnostic(ErrorCode.ERR_DuplicateAttributeInNetModule).WithArguments("CLSCompliantAttribute", "true.netmodule"));

            CreateCompilationWithMscorlib("", new[] { trueModuleRef, falseModuleRef }).VerifyDiagnostics(
                // CONSIDER: dev11 actually reports CS0647 (failure to emit duplicate)
                // error CS7061: Duplicate 'CLSCompliantAttribute' attribute in 'true.netmodule'
                Diagnostic(ErrorCode.ERR_DuplicateAttributeInNetModule).WithArguments("CLSCompliantAttribute", "true.netmodule"),
                // false.netmodule: warning CS3017: You cannot specify the CLSCompliant attribute on a module that differs from the CLSCompliant attribute on the assembly
                Diagnostic(ErrorCode.WRN_CLS_NotOnModules2));
        }

        [Fact]
        public void WRN_CLS_OverloadRefOut_RefKind()
        {
            var source = @"
using System;

[assembly: CLSCompliant(true)]

public class Compliant
{
    public void M1(int x) { }
    public void M1(ref int x) { } //CS3006

    public void M2(out int x) { throw null; }
    public void M2(int x) { } //CS3006

    public void M3(ref int x) { }
    private void M3(int x) { } // Fine, since inaccessible.

    public void M4(ref int x) { }
    [CLSCompliant(false)]
    public void M4(int x) { } // Fine, since flagged.
}

internal class Internal
{
    public void M1(int x) { }
    public void M1(ref int x) { } // Fine, since inaccessible.
}

[CLSCompliant(false)]
public class NonCompliant
{
    public void M1(int x) { }
    public void M1(ref int x) { } // Fine, since container is flagged.
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,17): warning CS3006: Overloaded method 'Compliant.M1(ref int)' differing only in ref or out, or in array rank, is not CLS-compliant
                //     public void M1(ref int x) { } //CS3006
                Diagnostic(ErrorCode.WRN_CLS_OverloadRefOut, "M1").WithArguments("Compliant.M1(ref int)"),
                // (12,17): warning CS3006: Overloaded method 'Compliant.M2(int)' differing only in ref or out, or in array rank, is not CLS-compliant
                //     public void M2(int x) { } //CS3006
                Diagnostic(ErrorCode.WRN_CLS_OverloadRefOut, "M2").WithArguments("Compliant.M2(int)"));
        }

        [Fact]
        public void WRN_CLS_OverloadRefOut_ArrayRank()
        {
            var source = @"
using System;

[assembly: CLSCompliant(true)]

public class Compliant
{
    public void M1(int[] x) { }
    public void M1(int[,] x) { } //CS3006

    public void M2(int[,,] x) { }
    public void M2(int[,] x) { } //CS3006

    public void M3(int[] x) { }
    private void M3(int[,] x) { } // Fine, since inaccessible.

    public void M4(int[] x) { }
    [CLSCompliant(false)]
    public void M4(int[,] x) { } // Fine, since flagged.
}

internal class Internal
{
    public void M1(int[] x) { }
    public void M1(int[,] x) { } // Fine, since inaccessible.
}

[CLSCompliant(false)]
public class NonCompliant
{
    public void M1(int[] x) { }
    public void M1(int[,] x) { } // Fine, since container is flagged.
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,17): warning CS3006: Overloaded method 'Compliant.M1(int[*,*])' differing only in ref or out, or in array rank, is not CLS-compliant
                //     public void M1(int[,] x) { } //CS3006
                Diagnostic(ErrorCode.WRN_CLS_OverloadRefOut, "M1").WithArguments("Compliant.M1(int[*,*])"),
                // (12,17): warning CS3006: Overloaded method 'Compliant.M2(int[*,*])' differing only in ref or out, or in array rank, is not CLS-compliant
                //     public void M2(int[,] x) { } //CS3006
                Diagnostic(ErrorCode.WRN_CLS_OverloadRefOut, "M2").WithArguments("Compliant.M2(int[*,*])"));
        }

        [Fact]
        public void WRN_CLS_OverloadUnnamed()
        {
            var source = @"
using System;

[assembly: CLSCompliant(true)]

public class Compliant
{
    public void M1(long[][] x) { }
    public void M1(char[][] x) { } //CS3007

    public void M2(int[][][] x) { }
    public void M2(int[][] x) { } //CS3007

    public void M3(int[][] x) { }
    public void M3(int[] x) { } //CS3007

    public void M4(int[,][,] x) { }
    public void M4(int[][,] x) { } //CS3007

    public void M5(int[,][,] x) { }
    public void M5(int[,][] x) { } //CS3006 (Dev11 reports CS3007)

    public void M6(long[][] x) { }
    private void M6(char[][] x) { } // Fine, since inaccessible.

    public void M7(long[][] x) { }
    [CLSCompliant(false)]
    public void M7(char[][] x) { } // Fine, since flagged.
}

internal class Internal
{
    public void M1(long[][] x) { }
    public void M1(char[][] x) { } // Fine, since inaccessible.
}

[CLSCompliant(false)]
public class NonCompliant
{
    public void M1(long[][] x) { }
    public void M1(char[][] x) { } // Fine, since container is flagged.
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,17): warning CS3007: Overloaded method 'Compliant.M1(char[][])' differing only by unnamed array types is not CLS-compliant
                //     public void M1(char[][] x) { } //CS3007
                Diagnostic(ErrorCode.WRN_CLS_OverloadUnnamed, "M1").WithArguments("Compliant.M1(char[][])"),
                // (12,17): warning CS3007: Overloaded method 'Compliant.M2(int[][])' differing only by unnamed array types is not CLS-compliant
                //     public void M2(int[][] x) { } //CS3007
                Diagnostic(ErrorCode.WRN_CLS_OverloadUnnamed, "M2").WithArguments("Compliant.M2(int[][])"),
                // (15,17): warning CS3007: Overloaded method 'Compliant.M3(int[])' differing only by unnamed array types is not CLS-compliant
                //     public void M3(int[] x) { } //CS3007
                Diagnostic(ErrorCode.WRN_CLS_OverloadUnnamed, "M3").WithArguments("Compliant.M3(int[])"),
                // (18,17): warning CS3006: Overloaded method 'Compliant.M4(int[][*,*])' differing only in ref or out, or in array rank, is not CLS-compliant
                //     public void M4(int[][,] x) { } //CS3007
                Diagnostic(ErrorCode.WRN_CLS_OverloadRefOut, "M4").WithArguments("Compliant.M4(int[][*,*])"),
                // (21,17): warning CS3007: Overloaded method 'Compliant.M5(int[*,*][])' differing only by unnamed array types is not CLS-compliant
                //     public void M5(int[,][] x) { } //CS3006 (Dev11 reports CS3007)
                Diagnostic(ErrorCode.WRN_CLS_OverloadUnnamed, "M5").WithArguments("Compliant.M5(int[*,*][])"));
        }

        [Fact]
        public void Overloading_Ties()
        {
            var source = @"
using System;

[assembly: CLSCompliant(true)]

public class Compliant
{
    // unnamed vs ref
    public void M1(long[][] x, ref int y) { }
    public void M1(char[][] x, int y) { } //CS3007

    // ref vs unnamed
    public void M2(ref int x, long[][] y) { }
    public void M2(int x, char[][] y) { } //CS3007

    // unnamed vs rank
    public void M3(long[][] x, int[,] y) { }
    public void M3(char[][] x, int[] y) { } //CS3007

    // rank vs unnamed
    public void M4(int[,] x, long[][] y) { }
    public void M4(int[] x, char[][] y) { } //CS3007

    // rank vs ref
    public void M5(long[,] x, ref int y) { }
    public void M5(long[,,] x, int y) { } //CS3006

    // ref vs rank
    public void M6(ref int x, long[,,] y) { }
    public void M6(int x, long[,] y) { } //CS3006
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (10,17): warning CS3007: Overloaded method 'Compliant.M1(char[][], int)' differing only by unnamed array types is not CLS-compliant
                //     public void M1(char[][] x, int y) { } //CS3007
                Diagnostic(ErrorCode.WRN_CLS_OverloadUnnamed, "M1").WithArguments("Compliant.M1(char[][], int)"),
                // (14,17): warning CS3007: Overloaded method 'Compliant.M2(int, char[][])' differing only by unnamed array types is not CLS-compliant
                //     public void M2(int x, char[][] y) { } //CS3007
                Diagnostic(ErrorCode.WRN_CLS_OverloadUnnamed, "M2").WithArguments("Compliant.M2(int, char[][])"),
                // (18,17): warning CS3007: Overloaded method 'Compliant.M3(char[][], int[])' differing only by unnamed array types is not CLS-compliant
                //     public void M3(char[][] x, int[] y) { } //CS3007
                Diagnostic(ErrorCode.WRN_CLS_OverloadUnnamed, "M3").WithArguments("Compliant.M3(char[][], int[])"),
                // (22,17): warning CS3007: Overloaded method 'Compliant.M4(int[], char[][])' differing only by unnamed array types is not CLS-compliant
                //     public void M4(int[] x, char[][] y) { } //CS3007
                Diagnostic(ErrorCode.WRN_CLS_OverloadUnnamed, "M4").WithArguments("Compliant.M4(int[], char[][])"),
                // (26,17): warning CS3006: Overloaded method 'Compliant.M5(long[*,*,*], int)' differing only in ref or out, or in array rank, is not CLS-compliant
                //     public void M5(long[,,] x, int y) { } //CS3006
                Diagnostic(ErrorCode.WRN_CLS_OverloadRefOut, "M5").WithArguments("Compliant.M5(long[*,*,*], int)"),
                // (30,17): warning CS3006: Overloaded method 'Compliant.M6(int, long[*,*])' differing only in ref or out, or in array rank, is not CLS-compliant
                //     public void M6(int x, long[,] y) { } //CS3006
                Diagnostic(ErrorCode.WRN_CLS_OverloadRefOut, "M6").WithArguments("Compliant.M6(int, long[*,*])"));
        }

        [Fact]
        public void Overloading_Indexers()
        {
            var source = @"
using System;

[assembly: CLSCompliant(true)]

public class Compliant
{
    // unnamed
    public int this[long[][] x] { get { return 0; } }
    public int this[char[][] x] { get { return 0; } }

    // rank
    public int this[bool b, string[] x] { get { return 0; } }
    public int this[bool b, string[,] x] { get { return 0; } }

    // Can't differ by ref since RefKind must be None.
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (10,16): warning CS3007: Overloaded method 'Compliant.this[char[][]]' differing only by unnamed array types is not CLS-compliant
                //     public int this[char[][] x] { get { return 0; } }
                Diagnostic(ErrorCode.WRN_CLS_OverloadUnnamed, "this").WithArguments("Compliant.this[char[][]]"),
                // (14,16): warning CS3006: Overloaded method 'Compliant.this[bool, string[*,*]]' differing only in ref or out, or in array rank, is not CLS-compliant
                //     public int this[bool b, string[,] x] { get { return 0; } }
                Diagnostic(ErrorCode.WRN_CLS_OverloadRefOut, "this").WithArguments("Compliant.this[bool, string[*,*]]"));
        }

        [Fact]
        public void Overloading_MethodKind()
        {
            var source = @"
using System;

[assembly: CLSCompliant(true)]

public class C
{
    public C(long[][] p) { }
    public C(char[][] p) { } //CS3007

    public static implicit operator C(long[][] p) { return null; }
    public static implicit operator C(char[][] p) { return null; } //CS3007

    public static explicit operator C(bool[][] p) { return null; }
    public static explicit operator C(byte[][] p) { return null; } //CS3007

    public static int operator+(C c, long[][] p) { return 0; }
    public static int operator+(C c, char[][] p) { return 0; } //CS3007

    // Static constructors can't be overloaded
    // Destructors can't be overloaded.
    // Explicit interface implementations can't be public.
    // Accessors are tested separately.
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,12): warning CS3007: Overloaded method 'C.C(char[][])' differing only by unnamed array types is not CLS-compliant
                //     public C(char[][] p) { } //CS3007
                Diagnostic(ErrorCode.WRN_CLS_OverloadUnnamed, "C").WithArguments("C.C(char[][])"),
                // (12,37): warning CS3007: Overloaded method 'C.implicit operator C(char[][])' differing only by unnamed array types is not CLS-compliant
                //     public static implicit operator C(char[][] p) { return null; } //CS3007
                Diagnostic(ErrorCode.WRN_CLS_OverloadUnnamed, "C").WithArguments("C.implicit operator C(char[][])"),
                // (15,37): warning CS3007: Overloaded method 'C.explicit operator C(byte[][])' differing only by unnamed array types is not CLS-compliant
                //     public static explicit operator C(byte[][] p) { return null; } //CS3007
                Diagnostic(ErrorCode.WRN_CLS_OverloadUnnamed, "C").WithArguments("C.explicit operator C(byte[][])"),
                // (18,31): warning CS3007: Overloaded method 'C.operator +(C, char[][])' differing only by unnamed array types is not CLS-compliant
                //     public static int operator+(C c, char[][] p) { return 0; } //CS3007
                Diagnostic(ErrorCode.WRN_CLS_OverloadUnnamed, "+").WithArguments("C.operator +(C, char[][])"));
        }

        [Fact]
        public void Overloading_Conversions()
        {
            var source = @"
using System;

[assembly: CLSCompliant(true)]

public class C
{
    public static implicit operator int(C c) { return 0; }
    public static implicit operator byte(C c) { return 0; }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void Overloading_InterfaceMember()
        {
            var source = @"
using System;

[assembly: CLSCompliant(true)]

public interface I
{
    void M(long[][] p);
}

public class Implicit : I
{
    public void M(long[][] p) { }
    public void M(char[][] p) { } //CS3007
}

public class Explicit : I
{
    void I.M(long[][] p) { }
    public void M(char[][] p) { } //CS3007
}

public class Base : I
{
    void I.M(long[][] p) { }
}

public class Derived1 : Base, I
{
    public void M(char[][] p) { } //CS3007
}

public class Derived2 : Base
{
    public void M(char[][] p) { } // Mimic dev11 bug - don't report conflict with interface member.
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (14,17): warning CS3007: Overloaded method 'Implicit.M(char[][])' differing only by unnamed array types is not CLS-compliant
                //     public void M(char[][] p) { } //CS3007
                Diagnostic(ErrorCode.WRN_CLS_OverloadUnnamed, "M").WithArguments("Implicit.M(char[][])"),
                // (20,17): warning CS3007: Overloaded method 'Explicit.M(char[][])' differing only by unnamed array types is not CLS-compliant
                //     public void M(char[][] p) { } //CS3007
                Diagnostic(ErrorCode.WRN_CLS_OverloadUnnamed, "M").WithArguments("Explicit.M(char[][])"),
                // (30,17): warning CS3007: Overloaded method 'Derived1.M(char[][])' differing only by unnamed array types is not CLS-compliant
                //     public void M(char[][] p) { } //CS3007
                Diagnostic(ErrorCode.WRN_CLS_OverloadUnnamed, "M").WithArguments("Derived1.M(char[][])"));
        }

        [Fact]
        public void Overloading_BaseMember()
        {
            var source = @"
using System;

[assembly: CLSCompliant(true)]

public class Base
{
    public virtual void M(long[][] p) { }
    public virtual int this[long[][] p] { get { return 0; } set { } }
}

public class Derived_Overload : Base
{
    public void M(char[][] p) { } //CS3007
    public int this[char[][] p] { get { return 0; } set { } } //CS3007
}

public class Derived_Hide : Base
{
    public new void M(long[][] p) { }
    public new int this[long[][] p] { get { return 0; } set { } }
}

public class Derived_Override : Base
{
    public override void M(long[][] p) { }
    public override int this[long[][] p] { get { return 0; } set { } }
}

public class Derived1 : Base
{
}

public class Derived2 : Derived1
{
    public void M(char[][] p) { } //CS3007
    public int this[char[][] p] { get { return 0; } set { } } //CS3007
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (14,17): warning CS3007: Overloaded method 'Derived_Overload.M(char[][])' differing only by unnamed array types is not CLS-compliant
                //     public void M(char[][] p) { } //CS3007
                Diagnostic(ErrorCode.WRN_CLS_OverloadUnnamed, "M").WithArguments("Derived_Overload.M(char[][])"),
                // (15,16): warning CS3007: Overloaded method 'Derived_Overload.this[char[][]]' differing only by unnamed array types is not CLS-compliant
                //     public int this[char[][] p] { get { return 0; } set { } } //CS3007
                Diagnostic(ErrorCode.WRN_CLS_OverloadUnnamed, "this").WithArguments("Derived_Overload.this[char[][]]"),
                // (36,17): warning CS3007: Overloaded method 'Derived2.M(char[][])' differing only by unnamed array types is not CLS-compliant
                //     public void M(char[][] p) { } //CS3007
                Diagnostic(ErrorCode.WRN_CLS_OverloadUnnamed, "M").WithArguments("Derived2.M(char[][])"),
                // (37,16): warning CS3007: Overloaded method 'Derived2.this[char[][]]' differing only by unnamed array types is not CLS-compliant
                //     public int this[char[][] p] { get { return 0; } set { } } //CS3007
                Diagnostic(ErrorCode.WRN_CLS_OverloadUnnamed, "this").WithArguments("Derived2.this[char[][]]"));
        }

        [WorkItem(717146, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/717146")]
        [Fact]
        public void Overloading_TypeParameterArray()
        {
            var source = @"
using System;

[assembly: CLSCompliant(true)]

public class C<T>
{
    public void M1(T[] t) {}
    public void M1(int[] t) {}

    public void M2<U>(U[] t) {}
    public void M2<U>(int[] t) {}
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [WorkItem(717146, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/717146")]
        [Fact]
        public void Overloading_DynamicArray()
        {
            var source = @"
using System;

[assembly: CLSCompliant(true)]

public class C
{
    public void M(dynamic[] t) {}
    public void M(int[] t) {}
}
";

            CreateCompilationWithMscorlibAndSystemCore(source).VerifyDiagnostics();
        }

        [WorkItem(717146, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/717146")]
        [Fact]
        public void Overloading_PointerArray()
        {
            var source = @"
using System;

[assembly: CLSCompliant(true)]

public unsafe class C
{
    public void M(int*[] t) {}
    public void M(int[] t) {}
}
";

            // NOTE: don't cascade to WRN_CLS_OverloadUnnamed.
            CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,26): warning CS3001: Argument type 'int*[]' is not CLS-compliant
                //     public void M(int*[] t) {}
                Diagnostic(ErrorCode.WRN_CLS_BadArgType, "t").WithArguments("int*[]"));
        }

        [WorkItem(717146, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/717146")]
        [Fact]
        public void Overloading_ConsiderAllInheritedMembers()
        {
            var sourceTemplate = @"
using System;

[assembly: CLSCompliant(true)]

public class Base
{{
    public virtual void M() {{ }}
}}

public class Derived : Base, I
{{
    public virtual void m() {{ }}
}}

public interface I
{{
    {0}
}}
";

            // Interface empty - report conflict (with base type).
            CreateCompilationWithMscorlib(string.Format(sourceTemplate, "")).VerifyDiagnostics(
                // (13,25): warning CS3005: Identifier 'Derived.m()' differing only in case is not CLS-compliant
                //     public virtual void m() { }
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifierCase, "m").WithArguments("Derived.m()"));

            // Interface has conflict - report conflict.
            CreateCompilationWithMscorlib(string.Format(sourceTemplate, "void M();")).VerifyDiagnostics(
                // (13,25): warning CS3005: Identifier 'Derived.m()' differing only in case is not CLS-compliant
                //     public virtual void m() { }
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifierCase, "m").WithArguments("Derived.m()"));

            // Interface has identical method - report conflict (with base type).
            // BREAK: Dev11 does not report this - it sees that there is no conflict with the interface method and stops.
            CreateCompilationWithMscorlib(string.Format(sourceTemplate, "void m();")).VerifyDiagnostics(
                // (13,25): warning CS3005: Identifier 'Derived.m()' differing only in case is not CLS-compliant
                //     public virtual void m() { }
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifierCase, "m").WithArguments("Derived.m()"));
        }

        [Fact]
        public void TopLevelMethod_NoAssemblyAttribute()
        {
            var source = @"
using System;

[CLSCompliant(true)]
public void M() { }
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (5,13): error CS0116: A namespace does not directly contain members such as fields or methods
                // public void M() { }
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "M"));
        }

        [Fact]
        public void TopLevelMethod_AttributeTrue()
        {
            var source = @"
using System;

[assembly:CLSCompliant(true)]

[CLSCompliant(true)]
public void M() { }
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (5,13): error CS0116: A namespace does not directly contain members such as fields or methods
                // public void M() { }
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "M"));
        }

        [Fact]
        public void TopLevelMethod_AttributeFalse()
        {
            var source = @"
using System;

[assembly:CLSCompliant(false)]

[CLSCompliant(true)]
public void M() { }
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (7,13): error CS0116: A namespace does not directly contain members such as fields or methods
                // public void M() { }
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "M"));
        }

        [Fact]
        public void AbstractInNonCompliantAssembly()
        {
            var source = @"
using System;

[assembly:CLSCompliant(false)]

public abstract class C
{
    public abstract void M();
}

public interface I
{
    void M();
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void NonCompliantInaccessible()
        {
            var source = @"
using System;

[assembly:CLSCompliant(true)]

public class C
{
    private Bad M(Bad b) { return b; }
    private Bad this[Bad b] {  get { return b; } set { } }
    private Bad P { get; set; }
}

[CLSCompliant(false)]
public class Bad
{
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void NonCompliantAbstractInNonCompliantType()
        {
            var source = @"
using System;

[assembly:CLSCompliant(true)]

[CLSCompliant(false)]
public abstract class Bad
{
    public abstract Bad M(Bad b);
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void SymbolsFromAddedModule()
        {
            var moduleSource = @"
using System;

[assembly:CLSCompliant(true)]
[module:CLSCompliant(true)]

[CLSCompliant(false)] // No effect, since not public.
internal class C
{
}
";

            var source = @"
[assembly:System.CLSCompliant(true)]
";

            var moduleRef = CreateCompilationWithMscorlib(moduleSource, assemblyName: "module").EmitToImageReference(expectedWarnings: new[]
            {
                // (8,16): warning CS3019: CLS compliance checking will not be performed on 'C' because it is not visible from outside this assembly
                // internal class C
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnPrivateType, "C").WithArguments("C")
            });

            // No diagnostics about added module.
            CreateCompilationWithMscorlib(source, new[] { moduleRef }).VerifyDiagnostics();
        }

        [Fact]
        public void SpecialTypes()
        {
            var sourceTemplate = @"
using System;

[assembly:CLSCompliant(true)]

public class C
{{
    public void M({0} p) {{ throw null; }}
}}
";

            var helper = CreateCompilationWithMscorlib45("");
            var intType = helper.GetSpecialType(SpecialType.System_Int32);

            foreach (SpecialType st in Enum.GetValues(typeof(SpecialType)))
            {
                switch (st)
                {
                    case SpecialType.None:
                    case SpecialType.System_Void:
                    case SpecialType.System_Runtime_CompilerServices_IsVolatile: // static
                        continue;
                }

                var type = helper.GetSpecialType(st);
                if (type.Arity > 0)
                {
                    type = type.Construct(ArrayBuilder<TypeSymbol>.GetInstance(type.Arity, intType).ToImmutableAndFree());
                }
                var qualifiedName = type.ToTestDisplayString();

                var source = string.Format(sourceTemplate, qualifiedName);
                var comp = CreateCompilationWithMscorlib45(source);

                switch (st)
                {
                    case SpecialType.System_SByte:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_UInt64:
                    case SpecialType.System_UIntPtr:
                    case SpecialType.System_TypedReference:
                        Assert.Equal(ErrorCode.WRN_CLS_BadArgType, (ErrorCode)comp.GetDeclarationDiagnostics().Single().Code);
                        break;
                    default:
                        comp.VerifyDiagnostics();
                        break;
                }
            }
        }

        [WorkItem(697178, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/697178")]
        [Fact]
        public void ConstructedSpecialTypes()
        {
            var source = @"
using System;
using System.Collections.Generic;

[assembly: CLSCompliant(true)]

[CLSCompliant(false)]
public class Bad { }

public class Test
{
    public IEnumerable<Bad> M() { throw null; }
}
";
            // BREAK: Dev11 doesn't inspect the type parameters of special types.
            // Presumably, when the code was written, there were no generic special types.
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,29): warning CS3002: Return type of 'Test.M()' is not CLS-compliant
                //     public IEnumerable<Bad> M() { throw null; }
                Diagnostic(ErrorCode.WRN_CLS_BadReturnType, "M").WithArguments("Test.M()"));
        }

        [Fact]
        public void ParamArrayAttribute()
        {
            var source = @"
using System;

[assembly: CLSCompliant(true)]

public class ParamArrayAttribute : Attribute
{
    public ParamArrayAttribute(char dummy) { } // Need a CLS-compliant constructor.
    public ParamArrayAttribute(params int[] array) { }
}

[ParamArray(null)] // pass null to array parameter
public class Test1 { }

[ParamArray(1, 2)] // pass array of parameters
public class Test2 { }
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,2): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                // [ParamArray(null)] // pass null to array parameter
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "ParamArray(null)"),
                // (15,2): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                // [ParamArray(1, 2)] // pass array of parameters
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "ParamArray(1, 2)"));
        }

        [Fact]
        public void ArrayAttributeArgumentOnInaccessible()
        {
            var source = @"
using System;

[assembly: CLSCompliant(true)]

public class ArrayAttribute : Attribute
{
    public ArrayAttribute(object o) { }
}

[Array(new int[] { 1 })]
class Test { }
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (11,2): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                // [Array(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "Array(new int[] { 1 })"));
        }

        [Fact]
        public void MissingAttributeType()
        {
            var source = @"
using System;

[assembly: CLSCompliant(true)]

[Missing]
public class Test { }
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (6,2): error CS0246: The type or namespace name 'MissingAttribute' could not be found (are you missing a using directive or an assembly reference?)
                // [Missing]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Missing").WithArguments("MissingAttribute").WithLocation(6, 2),
                // (6,2): error CS0246: The type or namespace name 'Missing' could not be found (are you missing a using directive or an assembly reference?)
                // [Missing]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Missing").WithArguments("Missing").WithLocation(6, 2));
        }

        [Fact]
        public void WindowsRuntimeEvent()
        {
            var source = @"
using System;

[assembly: CLSCompliant(true)]

public delegate void D();

public sealed class C
{
    public event D E;
}
";

            var comp = CreateCompilationWithMscorlib(source, WinRtRefs, options: TestOptions.ReleaseWinMD);

            // CONSIDER: The CLS spec requires that event accessors have a certain shape and WinRT event
            // accessors do not.  However, dev11 does not report a diagnostic.
            comp.VerifyDiagnostics(
                // (10,20): warning CS0067: The event 'C.E' is never used
                //     public event D E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E"));

            var @event = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<EventSymbol>("E");
            Assert.True(@event.IsWindowsRuntimeEvent);
        }

        [Fact]
        public void MeaninglessOnAccessor()
        {
            var source = @"
using System;

[assembly: CLSCompliant(true)]

public class C
{
    public event Action E
    {
		[CLSCompliant(false)]//CS1667
        add { }

        [CLSCompliant(false)]//CS1667
        remove { }
    }

    public int P
    {
		[CLSCompliant(false)]//CS1667
		get
		{
			return 1;
		}
    }

    public int this[int x]
    {
		[CLSCompliant(false)]//CS1667
		get
		{
			return 1;
		}
    }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (10,4): error CS1667: Attribute 'CLSCompliantAttribute' is not valid on property or event accessors. It is only valid on 'assembly, module, class, struct, enum, constructor, method, property, indexer, field, event, interface, parameter, delegate, return, type parameter' declarations.
                // 		[CLSCompliant(false)]//CS1667
                Diagnostic(ErrorCode.ERR_AttributeNotOnAccessor, "CLSCompliant(false)").WithArguments("CLSCompliantAttribute", "assembly, module, class, struct, enum, constructor, method, property, indexer, field, event, interface, parameter, delegate, return, type parameter"),
                // (28,4): error CS1667: Attribute 'CLSCompliantAttribute' is not valid on property or event accessors. It is only valid on 'assembly, module, class, struct, enum, constructor, method, property, indexer, field, event, interface, parameter, delegate, return, type parameter' declarations.
                // 		[CLSCompliant(false)]//CS1667
                Diagnostic(ErrorCode.ERR_AttributeNotOnAccessor, "CLSCompliant(false)").WithArguments("CLSCompliantAttribute", "assembly, module, class, struct, enum, constructor, method, property, indexer, field, event, interface, parameter, delegate, return, type parameter"),
                // (19,4): error CS1667: Attribute 'CLSCompliantAttribute' is not valid on property or event accessors. It is only valid on 'assembly, module, class, struct, enum, constructor, method, property, indexer, field, event, interface, parameter, delegate, return, type parameter' declarations.
                // 		[CLSCompliant(false)]//CS1667
                Diagnostic(ErrorCode.ERR_AttributeNotOnAccessor, "CLSCompliant(false)").WithArguments("CLSCompliantAttribute", "assembly, module, class, struct, enum, constructor, method, property, indexer, field, event, interface, parameter, delegate, return, type parameter"),
                // (13,10): error CS1667: Attribute 'CLSCompliantAttribute' is not valid on property or event accessors. It is only valid on 'assembly, module, class, struct, enum, constructor, method, property, indexer, field, event, interface, parameter, delegate, return, type parameter' declarations.
                //         [CLSCompliant(false)]//CS1667
                Diagnostic(ErrorCode.ERR_AttributeNotOnAccessor, "CLSCompliant(false)").WithArguments("CLSCompliantAttribute", "assembly, module, class, struct, enum, constructor, method, property, indexer, field, event, interface, parameter, delegate, return, type parameter"));
        }

        [Fact]
        public void MeaninglessOnAccessor_Internal()
        {
            var source = @"
using System;

[assembly: CLSCompliant(true)]

internal class C
{
    internal event Action E
    {
		[CLSCompliant(false)]//CS1667
        add { }

        [CLSCompliant(false)]//CS1667
        remove { }
    }

    internal int P
    {
		[CLSCompliant(false)]//CS1667
		get
		{
			return 1;
		}
    }

    internal int this[int x]
    {
		[CLSCompliant(false)]//CS1667
		get
		{
			return 1;
		}
    }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (10,4): error CS1667: Attribute 'CLSCompliantAttribute' is not valid on property or event accessors. It is only valid on 'assembly, module, class, struct, enum, constructor, method, property, indexer, field, event, interface, parameter, delegate, return, type parameter' declarations.
                // 		[CLSCompliant(false)]//CS1667
                Diagnostic(ErrorCode.ERR_AttributeNotOnAccessor, "CLSCompliant(false)").WithArguments("CLSCompliantAttribute", "assembly, module, class, struct, enum, constructor, method, property, indexer, field, event, interface, parameter, delegate, return, type parameter"),
                // (28,4): error CS1667: Attribute 'CLSCompliantAttribute' is not valid on property or event accessors. It is only valid on 'assembly, module, class, struct, enum, constructor, method, property, indexer, field, event, interface, parameter, delegate, return, type parameter' declarations.
                // 		[CLSCompliant(false)]//CS1667
                Diagnostic(ErrorCode.ERR_AttributeNotOnAccessor, "CLSCompliant(false)").WithArguments("CLSCompliantAttribute", "assembly, module, class, struct, enum, constructor, method, property, indexer, field, event, interface, parameter, delegate, return, type parameter"),
                // (19,4): error CS1667: Attribute 'CLSCompliantAttribute' is not valid on property or event accessors. It is only valid on 'assembly, module, class, struct, enum, constructor, method, property, indexer, field, event, interface, parameter, delegate, return, type parameter' declarations.
                // 		[CLSCompliant(false)]//CS1667
                Diagnostic(ErrorCode.ERR_AttributeNotOnAccessor, "CLSCompliant(false)").WithArguments("CLSCompliantAttribute", "assembly, module, class, struct, enum, constructor, method, property, indexer, field, event, interface, parameter, delegate, return, type parameter"),
                // (13,10): error CS1667: Attribute 'CLSCompliantAttribute' is not valid on property or event accessors. It is only valid on 'assembly, module, class, struct, enum, constructor, method, property, indexer, field, event, interface, parameter, delegate, return, type parameter' declarations.
                //         [CLSCompliant(false)]//CS1667
                Diagnostic(ErrorCode.ERR_AttributeNotOnAccessor, "CLSCompliant(false)").WithArguments("CLSCompliantAttribute", "assembly, module, class, struct, enum, constructor, method, property, indexer, field, event, interface, parameter, delegate, return, type parameter"));
        }

        [WorkItem(709317, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/709317")]
        [Fact]
        public void Repro709317()
        {
            var libSource = @"
using System;

[assembly: CLSCompliant(true)]

public class C
{
}
";

            var source = @"
using System;

[assembly: CLSCompliant(true)]

public class D
{
    public C M() { return null; }
}
";
            var libRef = CreateCompilationWithMscorlib(libSource).EmitToImageReference();
            var comp = CreateCompilationWithMscorlib(source, new[] { libRef });
            var tree = comp.SyntaxTrees.Single();
            comp.GetDiagnosticsForSyntaxTree(CompilationStage.Declare, tree, null, includeEarlierStages: false, cancellationToken: CancellationToken.None);
        }

        [WorkItem(709317, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/709317")]
        [Fact]
        public void FilterTree()
        {
            var sourceTemplate = @"
using System;

[assembly:CLSCompliant(true)]

namespace N{0}
{{
    [CLSCompliant(false)]
    public class NonCompliant {{ }}

    [CLSCompliant(false)]
    public interface INonCompliant {{ }}

    public class Compliant : NonCompliant, INonCompliant
    {{
        public NonCompliant M<T>(NonCompliant n) where T : NonCompliant {{ throw null; }}
        public NonCompliant F;
        public NonCompliant P {{ get; set; }}
    }}

    [My(new int[] {{ 1 }})]
    public class MyAttribute : Attribute
    {{
        public MyAttribute(int[] i) {{ }}
    }}
}}
";

            var tree1 = SyntaxFactory.ParseSyntaxTree(string.Format(sourceTemplate, 1), path: "a.cs");
            var tree2 = SyntaxFactory.ParseSyntaxTree(string.Format(sourceTemplate, 2), path: "b.cs");
            var comp = CreateCompilationWithMscorlib(new[] { tree1, tree2 });

            comp.VerifyDiagnostics(
                // (21,6): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                //     [My(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "My(new int[] { 1 })"),
                // (21,6): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                //     [My(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "My(new int[] { 1 })"),
                // (14,18): warning CS3009: 'N1.Compliant': base type 'N1.NonCompliant' is not CLS-compliant
                //     public class Compliant : NonCompliant, INonCompliant
                Diagnostic(ErrorCode.WRN_CLS_BadBase, "Compliant").WithArguments("N1.Compliant", "N1.NonCompliant"),
                // (14,18): warning CS3009: 'N2.Compliant': base type 'N2.NonCompliant' is not CLS-compliant
                //     public class Compliant : NonCompliant, INonCompliant
                Diagnostic(ErrorCode.WRN_CLS_BadBase, "Compliant").WithArguments("N2.Compliant", "N2.NonCompliant"),
                // (17,29): warning CS3003: Type of 'N1.Compliant.F' is not CLS-compliant
                //         public NonCompliant F;
                Diagnostic(ErrorCode.WRN_CLS_BadFieldPropType, "F").WithArguments("N1.Compliant.F"),
                // (17,29): warning CS3003: Type of 'N2.Compliant.F' is not CLS-compliant
                //         public NonCompliant F;
                Diagnostic(ErrorCode.WRN_CLS_BadFieldPropType, "F").WithArguments("N2.Compliant.F"),
                // (16,29): warning CS3002: Return type of 'N1.Compliant.M<T>(N1.NonCompliant)' is not CLS-compliant
                //         public NonCompliant M<T>(NonCompliant n) where T : NonCompliant { throw null; }
                Diagnostic(ErrorCode.WRN_CLS_BadReturnType, "M").WithArguments("N1.Compliant.M<T>(N1.NonCompliant)"),
                // (16,29): warning CS3002: Return type of 'N2.Compliant.M<T>(N2.NonCompliant)' is not CLS-compliant
                //         public NonCompliant M<T>(NonCompliant n) where T : NonCompliant { throw null; }
                Diagnostic(ErrorCode.WRN_CLS_BadReturnType, "M").WithArguments("N2.Compliant.M<T>(N2.NonCompliant)"),
                // (16,31): warning CS3024: Constraint type 'N1.NonCompliant' is not CLS-compliant
                //         public NonCompliant M<T>(NonCompliant n) where T : NonCompliant { throw null; }
                Diagnostic(ErrorCode.WRN_CLS_BadTypeVar, "T").WithArguments("N1.NonCompliant"),
                // (16,31): warning CS3024: Constraint type 'N2.NonCompliant' is not CLS-compliant
                //         public NonCompliant M<T>(NonCompliant n) where T : NonCompliant { throw null; }
                Diagnostic(ErrorCode.WRN_CLS_BadTypeVar, "T").WithArguments("N2.NonCompliant"),
                // (16,47): warning CS3001: Argument type 'N1.NonCompliant' is not CLS-compliant
                //         public NonCompliant M<T>(NonCompliant n) where T : NonCompliant { throw null; }
                Diagnostic(ErrorCode.WRN_CLS_BadArgType, "n").WithArguments("N1.NonCompliant"),
                // (16,47): warning CS3001: Argument type 'N2.NonCompliant' is not CLS-compliant
                //         public NonCompliant M<T>(NonCompliant n) where T : NonCompliant { throw null; }
                Diagnostic(ErrorCode.WRN_CLS_BadArgType, "n").WithArguments("N2.NonCompliant"),
                // (18,29): warning CS3003: Type of 'N2.Compliant.P' is not CLS-compliant
                //         public NonCompliant P { get; set; }
                Diagnostic(ErrorCode.WRN_CLS_BadFieldPropType, "P").WithArguments("N2.Compliant.P"),
                // (18,29): warning CS3003: Type of 'N1.Compliant.P' is not CLS-compliant
                //         public NonCompliant P { get; set; }
                Diagnostic(ErrorCode.WRN_CLS_BadFieldPropType, "P").WithArguments("N1.Compliant.P"),
                // (22,18): warning CS3015: 'N1.MyAttribute' has no accessible constructors which use only CLS-compliant types
                //     public class MyAttribute : Attribute
                Diagnostic(ErrorCode.WRN_CLS_BadAttributeType, "MyAttribute").WithArguments("N1.MyAttribute"),
                // (22,18): warning CS3015: 'N2.MyAttribute' has no accessible constructors which use only CLS-compliant types
                //     public class MyAttribute : Attribute
                Diagnostic(ErrorCode.WRN_CLS_BadAttributeType, "MyAttribute").WithArguments("N2.MyAttribute"),

                // Not interesting:

                // (4,11): error CS0579: Duplicate 'CLSCompliant' attribute
                // [assembly:CLSCompliant(true)]
                Diagnostic(ErrorCode.ERR_DuplicateAttribute, "CLSCompliant").WithArguments("CLSCompliant"));

            comp.GetDiagnosticsForSyntaxTree(CompilationStage.Declare, tree1, null, includeEarlierStages: false, cancellationToken: CancellationToken.None).Verify(
                // a.cs(21,6): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                //     [My(new int[] { 1 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "My(new int[] { 1 })"),
                // a.cs(14,18): warning CS3009: 'N1.Compliant': base type 'N1.NonCompliant' is not CLS-compliant
                //     public class Compliant : NonCompliant, INonCompliant
                Diagnostic(ErrorCode.WRN_CLS_BadBase, "Compliant").WithArguments("N1.Compliant", "N1.NonCompliant"),
                // a.cs(17,29): warning CS3003: Type of 'N1.Compliant.F' is not CLS-compliant
                //         public NonCompliant F;
                Diagnostic(ErrorCode.WRN_CLS_BadFieldPropType, "F").WithArguments("N1.Compliant.F"),
                // a.cs(18,29): warning CS3003: Type of 'N1.Compliant.P' is not CLS-compliant
                //         public NonCompliant P { get; set; }
                Diagnostic(ErrorCode.WRN_CLS_BadFieldPropType, "P").WithArguments("N1.Compliant.P"),
                // a.cs(16,29): warning CS3002: Return type of 'N1.Compliant.M<T>(N1.NonCompliant)' is not CLS-compliant
                //         public NonCompliant M<T>(NonCompliant n) where T : NonCompliant { throw null; }
                Diagnostic(ErrorCode.WRN_CLS_BadReturnType, "M").WithArguments("N1.Compliant.M<T>(N1.NonCompliant)"),
                // a.cs(16,47): warning CS3001: Argument type 'N1.NonCompliant' is not CLS-compliant
                //         public NonCompliant M<T>(NonCompliant n) where T : NonCompliant { throw null; }
                Diagnostic(ErrorCode.WRN_CLS_BadArgType, "n").WithArguments("N1.NonCompliant"),
                // a.cs(16,31): warning CS3024: Constraint type 'N1.NonCompliant' is not CLS-compliant
                //         public NonCompliant M<T>(NonCompliant n) where T : NonCompliant { throw null; }
                Diagnostic(ErrorCode.WRN_CLS_BadTypeVar, "T").WithArguments("N1.NonCompliant"),
                // a.cs(22,18): warning CS3015: 'N1.MyAttribute' has no accessible constructors which use only CLS-compliant types
                //     public class MyAttribute : Attribute
                Diagnostic(ErrorCode.WRN_CLS_BadAttributeType, "MyAttribute").WithArguments("N1.MyAttribute"));
        }

        [Fact]
        [WorkItem(701013, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/701013")]
        public void AssemblyLevelAttribute()
        {
            var source = @"
[System.CLSCompliant(false)]
public class C
{
    [System.CLSCompliant(true)]
    [return: System.CLSCompliant(true)]
    public void M() {} 
}
";

            // No assembly-level attribute: warn about absence of assembly-level attribute.
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (3,14): warning CS3021: 'C' does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
                // public class C
                Diagnostic(ErrorCode.WRN_CLS_AssemblyNotCLS2, "C").WithArguments("C"),
                // (7,17): warning CS3014: 'C.M()' cannot be marked as CLS-compliant because the assembly does not have a CLSCompliant attribute
                //     public void M() {} 
                Diagnostic(ErrorCode.WRN_CLS_AssemblyNotCLS, "M").WithArguments("C.M()"));

            // Assembly-level true: warn about non-compliance.
            CreateCompilationWithMscorlib("[assembly:System.CLSCompliant(true)]" + source).VerifyDiagnostics(
                // (7,17): warning CS3018: 'C.M()' cannot be marked as CLS-compliant because it is a member of non-CLS-compliant type 'C'
                //     public void M() {} 
                Diagnostic(ErrorCode.WRN_CLS_IllegalTrueInFalse, "M").WithArguments("C.M()", "C"),
                // (6,14): warning CS3023: CLSCompliant attribute has no meaning when applied to return types. Try putting it on the method instead.
                //     [return: System.CLSCompliant(true)]
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnReturn, "System.CLSCompliant(true)"));

            // Assembly-level true: suppress all warnings.
            CreateCompilationWithMscorlib("[assembly:System.CLSCompliant(false)]" + source).VerifyDiagnostics();
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void InheritedCompliance1()
        {
            var libSource = @"
using System;

[assembly: CLSCompliant(false)]

[CLSCompliant(true)]
public class Base{ }

public class Derived : Base{ }
";

            var source = @"
using System;

[assembly: CLSCompliant(true)]

public class C
{
	public Base b;
	public Derived d;
}
";
            // NOTE: As in dev11, we ignore the fact that Derived inherits CLSCompliantAttribute from Base.
            var libRef = CreateCompilationWithMscorlib(libSource).EmitToImageReference();
            CreateCompilationWithMscorlib(source, new[] { libRef }).VerifyDiagnostics(
                // (9,17): warning CS3003: Type of 'C.d' is not CLS-compliant
                // 	public Derived d;
                Diagnostic(ErrorCode.WRN_CLS_BadFieldPropType, "d").WithArguments("C.d"));
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void InheritedCompliance2()
        {
            var libIL = @"
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly a
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
  .custom instance void [mscorlib]System.CLSCompliantAttribute::.ctor(bool) = {bool(true)}
}
.module a.dll

.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.CLSCompliantAttribute::.ctor(bool) = {bool(false)}
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit Derived
       extends Base
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}
";

            var source = @"
using System;

[assembly: CLSCompliant(true)]

public class C
{
	public Base b;
	public Derived d;
}
";
            // NOTE: As in dev11, we ignore the fact that Derived inherits CLSCompliantAttribute from Base.
            var libRef = CompileIL(libIL, appendDefaultHeader: false);
            CreateCompilationWithMscorlib(source, new[] { libRef }).VerifyDiagnostics(
                // (8,14): warning CS3003: Type of 'C.b' is not CLS-compliant
                // 	public Base b;
                Diagnostic(ErrorCode.WRN_CLS_BadFieldPropType, "b").WithArguments("C.b"));
        }

        [Fact]
        [WorkItem(718503, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/718503")]
        public void ErrorTypeAccessibility()
        {
            var source = @"
[assembly:System.CLSCompliant(true)]

public class C : object, IError
{
    public void M() {} 
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (4,26): error CS0246: The type or namespace name 'IError' could not be found (are you missing a using directive or an assembly reference?)
                // public class C : object, IError
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "IError").WithArguments("IError"));
        }

        [Fact]
        public void GlobalNamespaceContainingAssembly()
        {
            var libSource = @"
namespace A
{
    public class Base { }
}
";

            var source = @"
[assembly:System.CLSCompliant(true)]

namespace A
{
    namespace B
    {
        [System.CLSCompliant(true)]
        public class C : A.Base { }
    }
}
";
            var libRef = CreateCompilationWithMscorlib(libSource, assemblyName: "lib").EmitToImageReference();

            CreateCompilationWithMscorlibAndSystemCore(source, new[] { libRef }).GetDiagnostics();
        }

        [Fact]
        [WorkItem(741721, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/741721")]
        public void WRN_CLS_MeaninglessOnReturn_Inaccessible()
        {
            var source = @"
[assembly: System.CLSCompliant(true)]
class Test
{
    [return: System.CLSCompliant(true)] // CS3023
    public static int Main()
    {
        return 0;
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (5,14): warning CS3023: CLSCompliant attribute has no meaning when applied to return types. Try putting it on the method instead.
                //     [return: System.CLSCompliant(true)] // CS3023
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnReturn, "System.CLSCompliant(true)"));
        }

        [Fact]
        [WorkItem(741720, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/741720")]
        public void WRN_CLS_MeaninglessOnParam_Inaccessible()
        {
            var source = @"
[assembly: System.CLSCompliant(true)]
class Test
{
    public int Func([param: System.CLSCompliant(true)] int i) // CS3022
    {
        return i;
    }
    public static int Main()
    {
        return 0;
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (5,29): warning CS3022: CLSCompliant attribute has no meaning when applied to parameters. Try putting it on the method instead.
                //     public int Func([param: System.CLSCompliant(true)] int i) // CS3022
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnParam, "System.CLSCompliant(true)"));
        }

        [Fact]
        [WorkItem(741718, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/741718")]
        public void WRN_CLS_ArrayArgumentToAttribute_Inaccessible()
        {
            var source = @"
using System;
[assembly: CLSCompliant(true)]
[My(new int[] { 1, 2 })]
class MyAttribute : Attribute
{
    public MyAttribute()
    {
    }
    public MyAttribute(int[] a)
    {
    }
    public static int Main()
    {
        return 0;
    }
}

";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (4,2): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                // [My(new int[] { 1, 2 })]
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "My(new int[] { 1, 2 })"));
        }

        [Fact]
        [WorkItem(749432, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/749432")]
        public void InvalidAttributeArgument()
        {
            var source = @"
using System;
[assembly: CLSCompliant(true)]

public class C
{
    [CLSCompliant(new { field = false }.field)]
    public void Test()
    {
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (7,19): error CS0836: Cannot use anonymous type in a constant expression
                //     [CLSCompliant(new { field = false }.field)]
                Diagnostic(ErrorCode.ERR_AnonymousTypeNotAvailable, "new"));
        }

        [Fact, WorkItem(1026453, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1026453")]
        public void Bug1026453()
        {
            var source1 = @"
namespace N1
{
    public class A { }
}
";
            var comp1 = CreateCompilationWithMscorlib(source1, options: TestOptions.ReleaseModule);

            var source2 = @"
using System;

[assembly: CLSCompliant(true)] 
[module: CLSCompliant(true)] 

namespace N1
{
    public class B { }
}
";
            var comp2 = CreateCompilationWithMscorlib(source2, new[] { comp1.EmitToImageReference() }, TestOptions.ReleaseDll.WithConcurrentBuild(false));
            comp2.VerifyDiagnostics(
    // warning CS3013: Added modules must be marked with the CLSCompliant attribute to match the assembly
    Diagnostic(ErrorCode.WRN_CLS_ModuleMissingCLS).WithLocation(1, 1)
                );

            comp2.WithOptions(TestOptions.ReleaseDll.WithConcurrentBuild(true)).VerifyDiagnostics(
    // warning CS3013: Added modules must be marked with the CLSCompliant attribute to match the assembly
    Diagnostic(ErrorCode.WRN_CLS_ModuleMissingCLS).WithLocation(1, 1)
                );

            var comp3 = comp2.WithOptions(TestOptions.ReleaseModule.WithConcurrentBuild(false));
            comp3.VerifyDiagnostics(
    // warning CS3013: Added modules must be marked with the CLSCompliant attribute to match the assembly
    Diagnostic(ErrorCode.WRN_CLS_ModuleMissingCLS).WithLocation(1, 1)
                );

            comp3.WithOptions(TestOptions.ReleaseModule.WithConcurrentBuild(true)).VerifyDiagnostics(
    // warning CS3013: Added modules must be marked with the CLSCompliant attribute to match the assembly
    Diagnostic(ErrorCode.WRN_CLS_ModuleMissingCLS).WithLocation(1, 1)
                );
        }
    }
}
