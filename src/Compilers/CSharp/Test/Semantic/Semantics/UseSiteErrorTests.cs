// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Tests related to use site errors.
    /// </summary>
    public class UseSiteErrorTests : CSharpTestBase
    {
        [Fact]
        public void TestFields()
        {
            var text = @"
public class C
{
    public CSharpErrors.Subclass1 Field;
}";

            CompileWithMissingReference(text).VerifyDiagnostics();
        }

        [Fact]
        public void TestOverrideMethodReturnType()
        {
            var text = @"
class C : CSharpErrors.ClassMethods
{
    public override UnavailableClass ReturnType1() { return null; }
    public override UnavailableClass[] ReturnType2() { return null; }
}";

            CompileWithMissingReference(text).VerifyDiagnostics(
                // (4,21): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
                //     public override UnavailableClass ReturnType1() { return null; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass"),
                // (5,21): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
                //     public override UnavailableClass[] ReturnType2() { return null; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass"),

                // (4,38): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override UnavailableClass ReturnType1() { return null; }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "ReturnType1").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (5,40): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override UnavailableClass[] ReturnType2() { return null; }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "ReturnType2").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [Fact]
        public void TestOverrideMethodReturnTypeModOpt()
        {
            var text = @"
class C : ILErrors.ClassMethods
{
    public override int ReturnType1() { return 0; }
    public override int[] ReturnType2() { return null; }
}";

            CompileWithMissingReference(text).VerifyDiagnostics(
                // (4,25): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override int ReturnType1() { return 0; }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "ReturnType1").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (5,27): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override int[] ReturnType2() { return null; }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "ReturnType2").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [Fact]
        public void TestOverrideMethodParameterType()
        {
            var text = @"
class C : CSharpErrors.ClassMethods
{
    public override void ParameterType1(UnavailableClass x) { }
    public override void ParameterType2(UnavailableClass[] x) { }
}";

            CompileWithMissingReference(text).VerifyDiagnostics(
                // (4,41): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
                //     public override void ParameterType1(UnavailableClass x) { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass"),
                // (5,41): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
                //     public override void ParameterType2(UnavailableClass[] x) { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass"));
        }

        [Fact]
        public void TestOverrideMethodParameterTypeModOpt()
        {
            var text = @"
class C : ILErrors.ClassMethods
{
    public override void ParameterType1(int x) { }
    public override void ParameterType2(int[] x) { }
}";

            CompileWithMissingReference(text).VerifyDiagnostics(
                // (4,26): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override void ParameterType1(int x) { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "ParameterType1").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (5,26): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override void ParameterType2(int[] x) { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "ParameterType2").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [Fact]
        public void TestImplicitlyImplementMethod()
        {
            var text = @"
class C : CSharpErrors.InterfaceMethods
{
    public UnavailableClass ReturnType1() { return null; }
    public UnavailableClass[] ReturnType2() { return null; }
    public void ParameterType1(UnavailableClass x) { }
    public void ParameterType2(UnavailableClass[] x) { }
}";

            CompileWithMissingReference(text).VerifyDiagnostics(
    // (5,12): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
    //     public UnavailableClass[] ReturnType2() { return null; }
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass").WithLocation(5, 12),
    // (6,32): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
    //     public void ParameterType1(UnavailableClass x) { }
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass").WithLocation(6, 32),
    // (7,32): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
    //     public void ParameterType2(UnavailableClass[] x) { }
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass").WithLocation(7, 32),
    // (4,12): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
    //     public UnavailableClass ReturnType1() { return null; }
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass").WithLocation(4, 12),
    // (2,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceMethods
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceMethods").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11),
    // (2,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceMethods
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceMethods").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11),
    // (2,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceMethods
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceMethods").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11),
    // (2,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceMethods
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceMethods").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11)
            );
        }

        [Fact]
        public void TestImplicitlyImplementMethodModOpt()
        {
            var text = @"
class C : ILErrors.InterfaceMethods
{
    public int ReturnType1() { return 0; }
    public int[] ReturnType2() { return null; }
    public void ParameterType1(int x) { }
    public void ParameterType2(int[] x) { }
}";

            CompileWithMissingReference(text).VerifyDiagnostics(
                // (4,16): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public int ReturnType1() { return 0; }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "ReturnType1").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (5,18): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public int[] ReturnType2() { return null; }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "ReturnType2").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (6,17): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public void ParameterType1(int x) { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "ParameterType1").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (7,17): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public void ParameterType2(int[] x) { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "ParameterType2").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [Fact]
        public void TestExplicitlyImplementMethod()
        {
            var text = @"
class C : CSharpErrors.InterfaceMethods
{
    UnavailableClass CSharpErrors.InterfaceMethods.ReturnType1() { return null; }
    UnavailableClass[] CSharpErrors.InterfaceMethods.ReturnType2() { return null; }
    void CSharpErrors.InterfaceMethods.ParameterType1(UnavailableClass x) { }
    void CSharpErrors.InterfaceMethods.ParameterType2(UnavailableClass[] x) { }
}";

            CompileWithMissingReference(text).VerifyDiagnostics(
    // (5,5): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
    //     UnavailableClass[] CSharpErrors.InterfaceMethods.ReturnType2() { return null; }
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass").WithLocation(5, 5),
    // (5,54): error CS0539: 'C.ReturnType2()' in explicit interface declaration is not a member of interface
    //     UnavailableClass[] CSharpErrors.InterfaceMethods.ReturnType2() { return null; }
    Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "ReturnType2").WithArguments("C.ReturnType2()").WithLocation(5, 54),
    // (6,55): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
    //     void CSharpErrors.InterfaceMethods.ParameterType1(UnavailableClass x) { }
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass").WithLocation(6, 55),
    // (6,40): error CS0539: 'C.ParameterType1(UnavailableClass)' in explicit interface declaration is not a member of interface
    //     void CSharpErrors.InterfaceMethods.ParameterType1(UnavailableClass x) { }
    Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "ParameterType1").WithArguments("C.ParameterType1(UnavailableClass)").WithLocation(6, 40),
    // (7,55): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
    //     void CSharpErrors.InterfaceMethods.ParameterType2(UnavailableClass[] x) { }
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass").WithLocation(7, 55),
    // (7,40): error CS0539: 'C.ParameterType2(UnavailableClass[])' in explicit interface declaration is not a member of interface
    //     void CSharpErrors.InterfaceMethods.ParameterType2(UnavailableClass[] x) { }
    Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "ParameterType2").WithArguments("C.ParameterType2(UnavailableClass[])").WithLocation(7, 40),
    // (4,5): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
    //     UnavailableClass CSharpErrors.InterfaceMethods.ReturnType1() { return null; }
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass").WithLocation(4, 5),
    // (4,52): error CS0539: 'C.ReturnType1()' in explicit interface declaration is not a member of interface
    //     UnavailableClass CSharpErrors.InterfaceMethods.ReturnType1() { return null; }
    Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "ReturnType1").WithArguments("C.ReturnType1()").WithLocation(4, 52),
    // (2,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceMethods
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceMethods").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11),
    // (2,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceMethods
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceMethods").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11),
    // (2,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceMethods
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceMethods").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11),
    // (2,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceMethods
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceMethods").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11)
            );
        }

        [Fact]
        public void TestExplicitlyImplementMethodModOpt()
        {
            var text = @"
class C : ILErrors.InterfaceMethods
{
    int ILErrors.InterfaceMethods.ReturnType1() { return 0; }
    int[] ILErrors.InterfaceMethods.ReturnType2() { return null; }
    void ILErrors.InterfaceMethods.ParameterType1(int x) { }
    void ILErrors.InterfaceMethods.ParameterType2(int[] x) { }
}";

            CompileWithMissingReference(text).VerifyDiagnostics(
                // (4,16): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public int ReturnType1() { return 0; }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "ReturnType1").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (5,18): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public int[] ReturnType2() { return null; }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "ReturnType2").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (6,17): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public void ParameterType1(int x) { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "ParameterType1").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (7,17): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public void ParameterType2(int[] x) { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "ParameterType2").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [Fact]
        public void TestOverridePropertyType()
        {
            var text = @"
class C : CSharpErrors.ClassProperties
{
    public override UnavailableClass Get1 { get { return null; } }
    public override UnavailableClass[] Get2 { get { return null; } }

    public override UnavailableClass Set1 { set { } }
    public override UnavailableClass[] Set2 { set { } }

    public override UnavailableClass GetSet1 { get { return null; } set { } }
    public override UnavailableClass[] GetSet2 { get { return null; } set { } }
}";

            CompileWithMissingReference(text).VerifyDiagnostics(
                // (4,21): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
                //     public override UnavailableClass Get1 { get { return null; } }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass"),
                // (5,21): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
                //     public override UnavailableClass[] Get2 { get { return null; } }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass"),
                // (7,21): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
                //     public override UnavailableClass Set1 { set { } }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass"),
                // (8,21): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
                //     public override UnavailableClass[] Set2 { set { } }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass"),
                // (10,21): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
                //     public override UnavailableClass GetSet1 { get { return null; } set { } }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass"),
                // (11,21): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
                //     public override UnavailableClass[] GetSet2 { get { return null; } set { } }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass"),

                // (4,38): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override UnavailableClass Get1 { get { return null; } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Get1").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (5,40): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override UnavailableClass[] Get2 { get { return null; } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Get2").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (7,38): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override UnavailableClass Set1 { set { } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Set1").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (8,40): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override UnavailableClass[] Set2 { set { } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Set2").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (10,38): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override UnavailableClass GetSet1 { get { return null; } set { } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "GetSet1").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (11,40): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override UnavailableClass[] GetSet2 { get { return null; } set { } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "GetSet2").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [Fact]
        public void TestOverridePropertyTypeModOpt()
        {
            var text = @"
class C : ILErrors.ClassProperties
{
    public override int Get1 { get { return 0; } }
    public override int[] Get2 { get { return null; } }

    public override int Set1 { set { } }
    public override int[] Set2 { set { } }

    public override int GetSet1 { get { return 0; } set { } }
    public override int[] GetSet2 { get { return null; } set { } }
}";

            CompileWithMissingReference(text).VerifyDiagnostics(
                // (4,25): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override int Get1 { get { return 0; } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Get1").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (5,27): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override int[] Get2 { get { return null; } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Get2").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (7,25): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override int Set1 { set { } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Set1").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (8,27): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override int[] Set2 { set { } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Set2").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (10,25): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override int GetSet1 { get { return 0; } set { } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "GetSet1").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (11,27): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override int[] GetSet2 { get { return null; } set { } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "GetSet2").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [Fact]
        public void TestImplicitlyImplementProperty()
        {
            var text = @"
class C : CSharpErrors.InterfaceProperties
{
    public UnavailableClass Get1 { get { return null; } }
    public UnavailableClass[] Get2 { get { return null; } }

    public UnavailableClass Set1 { set { } }
    public UnavailableClass[] Set2 { set { } }

    public UnavailableClass GetSet1 { get { return null; } set { } }
    public UnavailableClass[] GetSet2 { get { return null; } set { } }
}";

            CompileWithMissingReference(text).VerifyDiagnostics(
    // (5,12): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
    //     public UnavailableClass[] Get2 { get { return null; } }
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass").WithLocation(5, 12),
    // (7,12): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
    //     public UnavailableClass Set1 { set { } }
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass").WithLocation(7, 12),
    // (8,12): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
    //     public UnavailableClass[] Set2 { set { } }
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass").WithLocation(8, 12),
    // (10,12): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
    //     public UnavailableClass GetSet1 { get { return null; } set { } }
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass").WithLocation(10, 12),
    // (11,12): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
    //     public UnavailableClass[] GetSet2 { get { return null; } set { } }
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass").WithLocation(11, 12),
    // (4,12): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
    //     public UnavailableClass Get1 { get { return null; } }
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass").WithLocation(4, 12),
    // (2,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceProperties
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceProperties").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11),
    // (2,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceProperties
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceProperties").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11),
    // (2,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceProperties
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceProperties").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11),
    // (2,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceProperties
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceProperties").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11),
    // (2,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceProperties
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceProperties").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11),
    // (2,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceProperties
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceProperties").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11)
            );
        }

        [Fact]
        public void TestImplicitlyImplementPropertyModOpt()
        {
            var text = @"
class C : ILErrors.InterfaceProperties
{
    public int Get1 { get { return 0; } }
    public int[] Get2 { get { return null; } }

    public int Set1 { set { } }
    public int[] Set2 { set { } }

    public int GetSet1 { get { return 0; } set { } }
    public int[] GetSet2 { get { return null; } set { } }
}";

            CompileWithMissingReference(text).VerifyDiagnostics(
                // (10,44): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public int GetSet1 { get { return 0; } set { } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "set").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (11,28): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public int[] GetSet2 { get { return null; } set { } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "get").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (11,49): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public int[] GetSet2 { get { return null; } set { } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "set").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (4,23): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public int Get1 { get { return 0; } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "get").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (5,25): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public int[] Get2 { get { return null; } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "get").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (7,23): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public int Set1 { set { } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "set").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (8,25): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public int[] Set2 { set { } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "set").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (10,26): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public int GetSet1 { get { return 0; } set { } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "get").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [Fact]
        public void TestExplicitlyImplementProperty()
        {
            var text = @"
class C : CSharpErrors.InterfaceProperties
{
    UnavailableClass CSharpErrors.InterfaceProperties.Get1 { get { return null; } }
    UnavailableClass[] CSharpErrors.InterfaceProperties.Get2 { get { return null; } }

    UnavailableClass CSharpErrors.InterfaceProperties.Set1 { set { } }
    UnavailableClass[] CSharpErrors.InterfaceProperties.Set2 { set { } }

    UnavailableClass CSharpErrors.InterfaceProperties.GetSet1 { get { return null; } set { } }
    UnavailableClass[] CSharpErrors.InterfaceProperties.GetSet2 { get { return null; } set { } }
}";

            CompileWithMissingReference(text).VerifyDiagnostics(
    // (4,5): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
    //     UnavailableClass CSharpErrors.InterfaceProperties.Get1 { get { return null; } }
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass").WithLocation(4, 5),
    // (4,55): error CS0539: 'C.Get1' in explicit interface declaration is not a member of interface
    //     UnavailableClass CSharpErrors.InterfaceProperties.Get1 { get { return null; } }
    Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Get1").WithArguments("C.Get1").WithLocation(4, 55),
    // (5,5): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
    //     UnavailableClass[] CSharpErrors.InterfaceProperties.Get2 { get { return null; } }
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass").WithLocation(5, 5),
    // (5,57): error CS0539: 'C.Get2' in explicit interface declaration is not a member of interface
    //     UnavailableClass[] CSharpErrors.InterfaceProperties.Get2 { get { return null; } }
    Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Get2").WithArguments("C.Get2").WithLocation(5, 57),
    // (7,5): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
    //     UnavailableClass CSharpErrors.InterfaceProperties.Set1 { set { } }
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass").WithLocation(7, 5),
    // (7,55): error CS0539: 'C.Set1' in explicit interface declaration is not a member of interface
    //     UnavailableClass CSharpErrors.InterfaceProperties.Set1 { set { } }
    Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Set1").WithArguments("C.Set1").WithLocation(7, 55),
    // (8,5): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
    //     UnavailableClass[] CSharpErrors.InterfaceProperties.Set2 { set { } }
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass").WithLocation(8, 5),
    // (8,57): error CS0539: 'C.Set2' in explicit interface declaration is not a member of interface
    //     UnavailableClass[] CSharpErrors.InterfaceProperties.Set2 { set { } }
    Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Set2").WithArguments("C.Set2").WithLocation(8, 57),
    // (10,5): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
    //     UnavailableClass CSharpErrors.InterfaceProperties.GetSet1 { get { return null; } set { } }
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass").WithLocation(10, 5),
    // (10,55): error CS0539: 'C.GetSet1' in explicit interface declaration is not a member of interface
    //     UnavailableClass CSharpErrors.InterfaceProperties.GetSet1 { get { return null; } set { } }
    Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "GetSet1").WithArguments("C.GetSet1").WithLocation(10, 55),
    // (11,5): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
    //     UnavailableClass[] CSharpErrors.InterfaceProperties.GetSet2 { get { return null; } set { } }
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass").WithLocation(11, 5),
    // (11,57): error CS0539: 'C.GetSet2' in explicit interface declaration is not a member of interface
    //     UnavailableClass[] CSharpErrors.InterfaceProperties.GetSet2 { get { return null; } set { } }
    Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "GetSet2").WithArguments("C.GetSet2").WithLocation(11, 57),
    // (2,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceProperties
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceProperties").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11),
    // (2,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceProperties
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceProperties").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11),
    // (2,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceProperties
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceProperties").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11),
    // (2,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceProperties
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceProperties").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11),
    // (2,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceProperties
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceProperties").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11),
    // (2,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceProperties
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceProperties").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11)
            );
        }

        [Fact]
        public void TestExplicitlyImplementPropertyModOpt()
        {
            var text = @"
class C : ILErrors.InterfaceProperties
{
    int ILErrors.InterfaceProperties.Get1 { get { return 0; } }
    int[] ILErrors.InterfaceProperties.Get2 { get { return null; } }

    int ILErrors.InterfaceProperties.Set1 { set { } }
    int[] ILErrors.InterfaceProperties.Set2 { set { } }

    int ILErrors.InterfaceProperties.GetSet1 { get { return 0; } set { } }
    int[] ILErrors.InterfaceProperties.GetSet2 { get { return null; } set { } }
}";

            CompileWithMissingReference(text).VerifyDiagnostics(
                // (10,66): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     int ILErrors.InterfaceProperties.GetSet1 { get { return 0; } set { } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "set").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (11,50): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     int[] ILErrors.InterfaceProperties.GetSet2 { get { return null; } set { } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "get").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (11,71): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     int[] ILErrors.InterfaceProperties.GetSet2 { get { return null; } set { } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "set").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (4,45): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     int ILErrors.InterfaceProperties.Get1 { get { return 0; } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "get").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (5,47): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     int[] ILErrors.InterfaceProperties.Get2 { get { return null; } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "get").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (7,45): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     int ILErrors.InterfaceProperties.Set1 { set { } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "set").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (8,47): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     int[] ILErrors.InterfaceProperties.Set2 { set { } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "set").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (10,48): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     int ILErrors.InterfaceProperties.GetSet1 { get { return 0; } set { } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "get").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [Fact]
        public void TestPropertyAccessorModOpt()
        {
            var text =
@"class C : ILErrors.ClassProperties
{
    static void M(ILErrors.ClassProperties c)
    {
        c.GetSet1 = c.GetSet1;
        c.GetSet2 = c.GetSet2;
        c.GetSet3 = c.GetSet3;
    }
    void M()
    {
        GetSet3 = GetSet3;
    }
}";
            CompileWithMissingReference(text).VerifyDiagnostics(
                // (5,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         c.GetSet1 = c.GetSet1;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "GetSet1").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(5, 11),
                // (5,23): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         c.GetSet1 = c.GetSet1;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "GetSet1").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(5, 23),
                // (6,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         c.GetSet2 = c.GetSet2;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "GetSet2").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(6, 11),
                // (6,23): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         c.GetSet2 = c.GetSet2;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "GetSet2").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(6, 23),
                // (7,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         c.GetSet3 = c.GetSet3;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "GetSet3").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 11),
                // (7,23): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         c.GetSet3 = c.GetSet3;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "GetSet3").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 23),
                // (11,9): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         GetSet3 = GetSet3;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "GetSet3").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(11, 9),
                // (11,19): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         GetSet3 = GetSet3;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "GetSet3").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(11, 19));
        }

        [Fact]
        public void TestOverrideEventType_FieldLike()
        {
            var text = @"
class C : CSharpErrors.ClassEvents
{
    public override event UnavailableDelegate Event1;
    public override event CSharpErrors.EventDelegate<UnavailableClass> Event2;
    public override event CSharpErrors.EventDelegate<UnavailableClass[]> Event3;

    void UseEvent() { Event1(); Event2(); Event3(); }
}";

            CompileWithMissingReference(text).VerifyDiagnostics(
                // (4,27): error CS0246: The type or namespace name 'UnavailableDelegate' could not be found (are you missing a using directive or an assembly reference?)
                //     public override event UnavailableDelegate Event1;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableDelegate").WithArguments("UnavailableDelegate"),
                // (5,54): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
                //     public override event CSharpErrors.EventDelegate<UnavailableClass> Event2;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass"),
                // (6,54): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
                //     public override event CSharpErrors.EventDelegate<UnavailableClass[]> Event3;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass"),

                // (4,47): error CS0012: The type 'UnavailableDelegate' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override event UnavailableDelegate Event1;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Event1").WithArguments("UnavailableDelegate", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (5,72): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override event CSharpErrors.EventDelegate<UnavailableClass> Event2;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Event2").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (6,74): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override event CSharpErrors.EventDelegate<UnavailableClass[]> Event3;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Event3").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [Fact]
        public void TestOverrideEventType_Custom()
        {
            var text = @"
class C : CSharpErrors.ClassEvents
{
    public override event UnavailableDelegate Event1 { add { } remove { } }
    public override event CSharpErrors.EventDelegate<UnavailableClass> Event2 { add { } remove { } }
    public override event CSharpErrors.EventDelegate<UnavailableClass[]> Event3 { add { } remove { } }
}";

            CompileWithMissingReference(text).VerifyDiagnostics(
                // (4,27): error CS0246: The type or namespace name 'UnavailableDelegate' could not be found (are you missing a using directive or an assembly reference?)
                //     public override event UnavailableDelegate Event1;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableDelegate").WithArguments("UnavailableDelegate"),
                // (5,54): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
                //     public override event CSharpErrors.EventDelegate<UnavailableClass> Event2;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass"),
                // (6,54): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
                //     public override event CSharpErrors.EventDelegate<UnavailableClass[]> Event3;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass"),

                // (4,47): error CS0012: The type 'UnavailableDelegate' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override event UnavailableDelegate Event1;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Event1").WithArguments("UnavailableDelegate", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (5,72): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override event CSharpErrors.EventDelegate<UnavailableClass> Event2;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Event2").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (6,74): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override event CSharpErrors.EventDelegate<UnavailableClass[]> Event3;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Event3").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [Fact]
        public void TestOverrideEventTypeModOpt_FieldLike()
        {
            var text = @"
class C : ILErrors.ClassEvents
{
    public override event System.Action<int[]> Event1;

    void UseEvent() { Event1(null); }
}";

            CompileWithMissingReference(text).VerifyDiagnostics(
                // (4,48): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override event System.Action<int[]> Event1;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Event1").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [Fact]
        public void TestOverrideEventTypeModOpt_Custom()
        {
            var text = @"
class C : ILErrors.ClassEvents
{
    public override event System.Action<int[]> Event1 { add { } remove { } }
}";

            CompileWithMissingReference(text).VerifyDiagnostics(
                // (4,48): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override event System.Action<int[]> Event1;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Event1").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [Fact]
        public void TestImplicitlyImplementEvent_FieldLike()
        {
            var text = @"
class C : CSharpErrors.InterfaceEvents
{
    public event UnavailableDelegate Event1 = () => { };
    public event CSharpErrors.EventDelegate<UnavailableClass> Event2 = () => { };
    public event CSharpErrors.EventDelegate<UnavailableClass[]> Event3 = () => { };
}";

            CompileWithMissingReference(text).VerifyDiagnostics(
    // (4,18): error CS0246: The type or namespace name 'UnavailableDelegate' could not be found (are you missing a using directive or an assembly reference?)
    //     public event UnavailableDelegate Event1 = () => { };
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableDelegate").WithArguments("UnavailableDelegate").WithLocation(4, 18),
    // (5,45): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
    //     public event CSharpErrors.EventDelegate<UnavailableClass> Event2 = () => { };
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass").WithLocation(5, 45),
    // (6,45): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
    //     public event CSharpErrors.EventDelegate<UnavailableClass[]> Event3 = () => { };
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass").WithLocation(6, 45),
    // (2,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceEvents
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceEvents").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11),
    // (2,11): error CS0012: The type 'UnavailableDelegate' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceEvents
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceEvents").WithArguments("UnavailableDelegate", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11),
    // (2,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceEvents
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceEvents").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11)
            );
        }

        [Fact]
        public void TestImplicitlyImplementEvent_Custom()
        {
            var text = @"
class C : CSharpErrors.InterfaceEvents
{
    public event UnavailableDelegate Event1 { add { } remove { } }
    public event CSharpErrors.EventDelegate<UnavailableClass> Event2 { add { } remove { } }
    public event CSharpErrors.EventDelegate<UnavailableClass[]> Event3 { add { } remove { } }
}";

            CompileWithMissingReference(text).VerifyDiagnostics(
    // (4,18): error CS0246: The type or namespace name 'UnavailableDelegate' could not be found (are you missing a using directive or an assembly reference?)
    //     public event UnavailableDelegate Event1 { add { } remove { } }
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableDelegate").WithArguments("UnavailableDelegate").WithLocation(4, 18),
    // (5,45): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
    //     public event CSharpErrors.EventDelegate<UnavailableClass> Event2 { add { } remove { } }
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass").WithLocation(5, 45),
    // (6,45): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
    //     public event CSharpErrors.EventDelegate<UnavailableClass[]> Event3 { add { } remove { } }
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass").WithLocation(6, 45),
    // (2,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceEvents
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceEvents").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11),
    // (2,11): error CS0012: The type 'UnavailableDelegate' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceEvents
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceEvents").WithArguments("UnavailableDelegate", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11),
    // (2,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceEvents
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceEvents").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11)
            );
        }

        [Fact]
        public void TestImplicitlyImplementEventModOpt_FieldLike()
        {
            var text = @"
class C : ILErrors.InterfaceEvents
{
    public event System.Action<int[]> Event1 = x => { };
}";

            CompileWithMissingReference(text).VerifyDiagnostics(
                // (4,39): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public event System.Action<int[]> Event1 = x => { };
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Event1").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (4,39): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public event System.Action<int[]> Event1 = x => { };
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Event1").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [Fact]
        public void TestImplicitlyImplementEventModOpt_Custom()
        {
            var text = @"
class C : ILErrors.InterfaceEvents
{
    public event System.Action<int[]> Event1 { add { } remove { } }
}";

            CompileWithMissingReference(text).VerifyDiagnostics(
                // (4,56): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public event System.Action<int[]> Event1 { add { } remove { } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "remove").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (4,48): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public event System.Action<int[]> Event1 { add { } remove { } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "add").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [Fact]
        public void TestExplicitlyImplementEvent_Custom() //NB: can't explicitly implement with field-like
        {
            var text = @"
class C : CSharpErrors.InterfaceEvents
{
    event UnavailableDelegate CSharpErrors.InterfaceEvents.Event1 { add { } remove { } }
    event CSharpErrors.EventDelegate<UnavailableClass> CSharpErrors.InterfaceEvents.Event2 { add { } remove { } }
    event CSharpErrors.EventDelegate<UnavailableClass[]> CSharpErrors.InterfaceEvents.Event3 { add { } remove { } }
}";

            CompileWithMissingReference(text).VerifyDiagnostics(
    // (4,11): error CS0246: The type or namespace name 'UnavailableDelegate' could not be found (are you missing a using directive or an assembly reference?)
    //     event UnavailableDelegate CSharpErrors.InterfaceEvents.Event1 { add { } remove { } }
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableDelegate").WithArguments("UnavailableDelegate").WithLocation(4, 11),
    // (4,60): error CS0539: 'C.Event1' in explicit interface declaration is not a member of interface
    //     event UnavailableDelegate CSharpErrors.InterfaceEvents.Event1 { add { } remove { } }
    Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Event1").WithArguments("C.Event1").WithLocation(4, 60),
    // (5,38): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
    //     event CSharpErrors.EventDelegate<UnavailableClass> CSharpErrors.InterfaceEvents.Event2 { add { } remove { } }
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass").WithLocation(5, 38),
    // (5,85): error CS0539: 'C.Event2' in explicit interface declaration is not a member of interface
    //     event CSharpErrors.EventDelegate<UnavailableClass> CSharpErrors.InterfaceEvents.Event2 { add { } remove { } }
    Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Event2").WithArguments("C.Event2").WithLocation(5, 85),
    // (6,38): error CS0246: The type or namespace name 'UnavailableClass' could not be found (are you missing a using directive or an assembly reference?)
    //     event CSharpErrors.EventDelegate<UnavailableClass[]> CSharpErrors.InterfaceEvents.Event3 { add { } remove { } }
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnavailableClass").WithArguments("UnavailableClass").WithLocation(6, 38),
    // (6,87): error CS0539: 'C.Event3' in explicit interface declaration is not a member of interface
    //     event CSharpErrors.EventDelegate<UnavailableClass[]> CSharpErrors.InterfaceEvents.Event3 { add { } remove { } }
    Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Event3").WithArguments("C.Event3").WithLocation(6, 87),
    // (2,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceEvents
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceEvents").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11),
    // (2,11): error CS0012: The type 'UnavailableDelegate' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceEvents
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceEvents").WithArguments("UnavailableDelegate", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11),
    // (2,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    // class C : CSharpErrors.InterfaceEvents
    Diagnostic(ErrorCode.ERR_NoTypeDef, "CSharpErrors.InterfaceEvents").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 11)
            );
        }

        [Fact]
        public void TestExplicitlyImplementEventModOpt_Custom() //NB: can't explicitly implement with field-like
        {
            var text = @"
class C : ILErrors.InterfaceEvents
{
    event System.Action<int[]> ILErrors.InterfaceEvents.Event1 { add { } remove { } }
}";

            CompileWithMissingReference(text).VerifyDiagnostics(
                // (4,74): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     event System.Action<int[]> ILErrors.InterfaceEvents.Event1 { add { } remove { } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "remove").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (4,66): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     event System.Action<int[]> ILErrors.InterfaceEvents.Event1 { add { } remove { } }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "add").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [Fact]
        public void TestEventAccess()
        {
            var text =
@"class C
{
    static void M(CSharpErrors.ClassEvents c, ILErrors.ClassEvents i)
    {
        c.Event1 += null;
        i.Event1 += null;
    }
}";
            CompileWithMissingReference(text).VerifyDiagnostics(
                // (5,11): error CS0012: The type 'UnavailableDelegate' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         c.Event1 += null;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Event1").WithArguments("UnavailableDelegate", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (6,11): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         i.Event1 += null;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Event1").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [Fact]
        public void TestDelegatesWithNoInvoke()
        {
            var text =
@"class C 
{


    public static T goo<T>(DelegateWithoutInvoke.DelegateGenericFunctionWithoutInvoke<T> del)
    {
        return del(""goo""); // will show ERR_InvalidDelegateType instead of ERR_NoSuchMemberOrExtension
    }


    public static void Main() 
    {
        DelegateWithoutInvoke.DelegateSubWithoutInvoke myDelegate1 = bar;
        myDelegate1.Invoke(""goo""); // will show an ERR_NoSuchMemberOrExtension
        DelegateWithoutInvoke.DelegateSubWithoutInvoke myDelegate2 = new DelegateWithoutInvoke.DelegateSubWithoutInvoke(myDelegate1);
        object myDelegate3 = new DelegateWithoutInvoke.DelegateSubWithoutInvoke(bar2);
        DelegateWithoutInvoke.DelegateSubWithoutInvoke myDelegate4 = x => System.Console.WriteLine(""Hello World"");
        object myDelegate6 = new DelegateWithoutInvoke.DelegateFunctionWithoutInvoke( x => ""Hello World"");
    }

    public static void bar(string p)
    {
        System.Console.WriteLine(""Hello World"");
    }

    public static void bar2(int p)
    {
        System.Console.WriteLine(""Hello World 2"");
    }
}";

            var delegatesWithoutInvokeReference = TestReferences.SymbolsTests.DelegateImplementation.DelegatesWithoutInvoke;
            CreateCompilation(text, new MetadataReference[] { delegatesWithoutInvokeReference }).VerifyDiagnostics(
                // (7,16): error CS7023: Delegate 'DelegateWithoutInvoke.DelegateGenericFunctionWithoutInvoke<T>' has no invoke method or an invoke method with a return type or parameter types that are not supported.
                //         return del("goo"); // will show ERR_InvalidDelegateType instead of ERR_NoSuchMemberOrExtension
                Diagnostic(ErrorCode.ERR_InvalidDelegateType, @"del(""goo"")").WithArguments("DelegateWithoutInvoke.DelegateGenericFunctionWithoutInvoke<T>"),
                // (13,70): error CS7023: Delegate 'DelegateWithoutInvoke.DelegateSubWithoutInvoke' has no invoke method or an invoke method with a return type or parameter types that are not supported.
                //         DelegateWithoutInvoke.DelegateSubWithoutInvoke myDelegate1 = bar;
                Diagnostic(ErrorCode.ERR_InvalidDelegateType, "bar").WithArguments("DelegateWithoutInvoke.DelegateSubWithoutInvoke"),
                // (14,21): error CS1061: 'DelegateWithoutInvoke.DelegateSubWithoutInvoke' does not contain a definition for 'Invoke' and no extension method 'Invoke' accepting a first argument of type 'DelegateWithoutInvoke.DelegateSubWithoutInvoke' could be found (are you missing a using directive or an assembly reference?)
                //         myDelegate1.Invoke("goo"); // will show an ERR_NoSuchMemberOrExtension
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Invoke").WithArguments("DelegateWithoutInvoke.DelegateSubWithoutInvoke", "Invoke"),
                // (15,70): error CS7023: Delegate 'DelegateWithoutInvoke.DelegateSubWithoutInvoke' has no invoke method or an invoke method with a return type or parameter types that are not supported.
                //         DelegateWithoutInvoke.DelegateSubWithoutInvoke myDelegate2 = new DelegateWithoutInvoke.DelegateSubWithoutInvoke(myDelegate1);
                Diagnostic(ErrorCode.ERR_InvalidDelegateType, "new DelegateWithoutInvoke.DelegateSubWithoutInvoke(myDelegate1)").WithArguments("DelegateWithoutInvoke.DelegateSubWithoutInvoke"),
                // (16,30): error CS7023: Delegate 'DelegateWithoutInvoke.DelegateSubWithoutInvoke' has no invoke method or an invoke method with a return type or parameter types that are not supported.
                //         object myDelegate3 = new DelegateWithoutInvoke.DelegateSubWithoutInvoke(bar2);
                Diagnostic(ErrorCode.ERR_InvalidDelegateType, "new DelegateWithoutInvoke.DelegateSubWithoutInvoke(bar2)").WithArguments("DelegateWithoutInvoke.DelegateSubWithoutInvoke"),
                // (17,70): error CS7023: Delegate 'DelegateWithoutInvoke.DelegateSubWithoutInvoke' has no invoke method or an invoke method with a return type or parameter types that are not supported.
                //         DelegateWithoutInvoke.DelegateSubWithoutInvoke myDelegate4 = x => System.Console.WriteLine("Hello World");
                Diagnostic(ErrorCode.ERR_InvalidDelegateType, @"x => System.Console.WriteLine(""Hello World"")").WithArguments("DelegateWithoutInvoke.DelegateSubWithoutInvoke"),
                // (18,87): error CS7023: Delegate 'DelegateWithoutInvoke.DelegateFunctionWithoutInvoke' has no invoke method or an invoke method with a return type or parameter types that are not supported.
                //         object myDelegate6 = new DelegateWithoutInvoke.DelegateFunctionWithoutInvoke( x => "Hello World");
                Diagnostic(ErrorCode.ERR_InvalidDelegateType, @"x => ""Hello World""").WithArguments("DelegateWithoutInvoke.DelegateFunctionWithoutInvoke")
            );
        }

        [Fact]
        public void TestDelegatesWithUseSiteErrors()
        {
            var text =
@"class C 
{
    public static T goo<T>(CSharpErrors.DelegateParameterType3<T> del)
    {
        return del.Invoke(""goo"");
    }

    public static void Main() 
    {
        CSharpErrors.DelegateReturnType1 myDelegate1 = bar;
        myDelegate1(""goo"");
        CSharpErrors.DelegateReturnType1 myDelegate2 = new CSharpErrors.DelegateReturnType1(myDelegate1);
        object myDelegate3 = new CSharpErrors.DelegateReturnType1(bar);
        CSharpErrors.DelegateReturnType1 myDelegate4 = x => System.Console.WriteLine(""Hello World"");
        object myDelegate6 = new CSharpErrors.DelegateReturnType1( x => ""Hello World"");
    }

    public static void bar(string p)
    {
        System.Console.WriteLine(""Hello World"");
    }

    public static void bar2(int p)
    {
        System.Console.WriteLine(""Hello World 2"");
    }
}";

            var csharpAssemblyReference = TestReferences.SymbolsTests.UseSiteErrors.CSharp;
            var ilAssemblyReference = TestReferences.SymbolsTests.UseSiteErrors.IL;
            CreateCompilation(text, new MetadataReference[] { csharpAssemblyReference, ilAssemblyReference }).VerifyDiagnostics(
                // (5,16): error CS0012: The type 'UnavailableClass<>' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         return del.Invoke("goo");
                Diagnostic(ErrorCode.ERR_NoTypeDef, "del.Invoke").WithArguments("UnavailableClass<>", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (13,56): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         CSharpErrors.DelegateReturnType1 myDelegate1 = bar;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "bar").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (14,9): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         myDelegate1("goo");
                Diagnostic(ErrorCode.ERR_NoTypeDef, @"myDelegate1(""goo"")").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (15,56): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         CSharpErrors.DelegateReturnType1 myDelegate2 = new CSharpErrors.DelegateReturnType1(myDelegate1);
                Diagnostic(ErrorCode.ERR_NoTypeDef, "new CSharpErrors.DelegateReturnType1(myDelegate1)").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (16,30): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         object myDelegate3 = new CSharpErrors.DelegateReturnType1(bar);
                Diagnostic(ErrorCode.ERR_NoTypeDef, "new CSharpErrors.DelegateReturnType1(bar)").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (17,56): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         CSharpErrors.DelegateReturnType1 myDelegate4 = x => System.Console.WriteLine("Hello World");
                Diagnostic(ErrorCode.ERR_NoTypeDef, @"x => System.Console.WriteLine(""Hello World"")").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (18,68): error CS0012: The type 'UnavailableClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         object myDelegate6 = new CSharpErrors.DelegateReturnType1( x => "Hello World");
                Diagnostic(ErrorCode.ERR_NoTypeDef, @"x => ""Hello World""").WithArguments("UnavailableClass", "Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
                );
        }

        [Fact, WorkItem(531090, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531090")]
        public void Constructor()
        {
            string srcLib1 = @"
using System;

public sealed class A
{
    public A(int a, Func<string, string> example) {}
    public A(Func<string, string> example) {}
}
";

            var lib1 = CreateEmptyCompilation(
                new[] { Parse(srcLib1) },
                new[] { TestReferences.NetFx.v2_0_50727.mscorlib, TestReferences.NetFx.v3_5_30729.SystemCore },
                TestOptions.ReleaseDll.WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default));

            string srcLib2 = @"
class Program
{
    static void Main()
    {
        new A(x => x);
    }
}
";
            var lib2 = CreateEmptyCompilation(
                new[] { Parse(srcLib2) },
                new[] { MscorlibRef, new CSharpCompilationReference(lib1) },
                TestOptions.ReleaseDll.WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default));

            lib2.VerifyDiagnostics(
                // (6,13): error CS0012: The type 'System.Func<,>' is defined in an assembly that is not referenced. You must add a reference to assembly 'System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'.
                //         new A(x => x);
                Diagnostic(ErrorCode.ERR_NoTypeDef, "A").WithArguments("System.Func<,>", "System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"));
        }

        [Fact, WorkItem(530974, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530974")]
        public void SynthesizedInterfaceImplementation()
        {
            var xSource = @"
public class X {}
";
            var xRef = CreateCompilation(xSource, assemblyName: "Test").EmitToImageReference();


            var libSource = @"
public interface I
{
    void Goo(X a);
}

public class C
{
    public void Goo(X a) { }
}
";
            var lib = CreateCompilation(libSource, new[] { xRef }, assemblyName: "Test");

            var mainSource = @"
class B : C, I { }
";
            var main = CreateCompilation(mainSource, new[] { new CSharpCompilationReference(lib) }, assemblyName: "Main");

            main.VerifyDiagnostics(
                // (2,7): error CS7068: Reference to type 'X' claims it is defined in this assembly, but it is not defined in source or any added modules
                // class B : C, I { }
                Diagnostic(ErrorCode.ERR_MissingTypeInSource, "B").WithArguments("X"));
        }

        [Fact, WorkItem(530974, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530974")]
        public void NoSynthesizedInterfaceImplementation()
        {
            var xSource = @"
public class X {}
";
            var xRef = CreateCompilation(xSource, assemblyName: "X").EmitToImageReference();

            var libSource = @"
public interface I
{
    void Goo(X a);
}

public class C
{
    public virtual void Goo(X a) { }
}
";
            var lib = CreateCompilation(libSource, new[] { xRef }, assemblyName: "Lib");

            var mainSource = @"
class B : C, I { }
";
            var main = CreateCompilation(mainSource, new[] { new CSharpCompilationReference(lib) }, assemblyName: "Main");

            main.VerifyEmitDiagnostics();
        }

        [Fact, WorkItem(530974, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530974")]
        public void SynthesizedInterfaceImplementation_Indexer()
        {
            var xSource = @"
public class X {}
";
            var xRef = CreateCompilation(xSource, assemblyName: "X").EmitToImageReference();

            var libSource = @"
public interface I
{
    int this[X a] { get; set; }
}

public class C
{
    public int this[X a] { get { return 1; } set { } }
}
";
            var lib = CreateCompilation(libSource, new[] { xRef }, assemblyName: "Lib");

            var mainSource = @"
class B : C, I { }
";
            var main = CreateCompilation(mainSource, new[] { new CSharpCompilationReference(lib) }, assemblyName: "Main");

            main.VerifyDiagnostics(
                // (2,7): error CS0012: The type 'X' is defined in an assembly that is not referenced. You must add a reference to assembly 'X, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                // class B : C, I { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "B").WithArguments("X", "X, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (2,7): error CS0012: The type 'X' is defined in an assembly that is not referenced. You must add a reference to assembly 'X, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                // class B : C, I { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "B").WithArguments("X", "X, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [Fact, WorkItem(530974, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530974")]
        public void SynthesizedInterfaceImplementation_ModOpt()
        {
            var unavailableRef = TestReferences.SymbolsTests.UseSiteErrors.Unavailable;
            var ilRef = TestReferences.SymbolsTests.UseSiteErrors.IL;

            var mainSource = @"
class B : ILErrors.ClassEventsNonVirtual, ILErrors.InterfaceEvents { }
";
            var main = CreateCompilation(mainSource, new[] { ilRef, unavailableRef });

            CompileAndVerify(main);
        }

        [Fact, WorkItem(530974, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530974")]
        public void NoSynthesizedInterfaceImplementation_ModOpt()
        {
            var unavailableRef = TestReferences.SymbolsTests.UseSiteErrors.Unavailable;
            var ilRef = TestReferences.SymbolsTests.UseSiteErrors.IL;

            var mainSource = @"
class B : ILErrors.ClassEvents, ILErrors.InterfaceEvents { }
";
            var main = CreateCompilation(mainSource, new[] { ilRef, unavailableRef });

            CompileAndVerify(main);
        }

        [Fact, WorkItem(530974, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530974")]
        public void SynthesizedInterfaceImplementation_ModReq()
        {
            var unavailableRef = TestReferences.SymbolsTests.UseSiteErrors.Unavailable;
            var ilRef = TestReferences.SymbolsTests.UseSiteErrors.IL;

            var mainSource = @"
class B : ILErrors.ModReqClassEventsNonVirtual, ILErrors.ModReqInterfaceEvents { }
";
            var main = CreateCompilation(mainSource, new[] { ilRef, unavailableRef });

            main.VerifyDiagnostics(
    // (2,49): error CS0648: '' is a type not supported by the language
    // class B : ILErrors.ModReqClassEventsNonVirtual, ILErrors.ModReqInterfaceEvents { }
    Diagnostic(ErrorCode.ERR_BogusType, "ILErrors.ModReqInterfaceEvents").WithArguments("").WithLocation(2, 49),
    // (2,49): error CS0570: 'ModReqInterfaceEvents.remove_Event1(?)' is not supported by the language
    // class B : ILErrors.ModReqClassEventsNonVirtual, ILErrors.ModReqInterfaceEvents { }
    Diagnostic(ErrorCode.ERR_BindToBogus, "ILErrors.ModReqInterfaceEvents").WithArguments("ILErrors.ModReqInterfaceEvents.remove_Event1(?)").WithLocation(2, 49),
    // (2,49): error CS0570: 'ModReqInterfaceEvents.add_Event1(?)' is not supported by the language
    // class B : ILErrors.ModReqClassEventsNonVirtual, ILErrors.ModReqInterfaceEvents { }
    Diagnostic(ErrorCode.ERR_BindToBogus, "ILErrors.ModReqInterfaceEvents").WithArguments("ILErrors.ModReqInterfaceEvents.add_Event1(?)").WithLocation(2, 49)
            );
        }

        [Fact]
        public void CompilerGeneratedAttributeNotRequired()
        {
            var text =
@"class C 
{
   public int AProperty { get; set; }
}";

            var compilation = CreateEmptyCompilation(text).VerifyDiagnostics(
                // (1,7): error CS0518: Predefined type 'System.Object' is not defined or imported
                // class C 
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "C").WithArguments("System.Object"),
                // (3,11): error CS0518: Predefined type 'System.Int32' is not defined or imported
                //    public int AProperty { get; set; }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "int").WithArguments("System.Int32"),
                // (3,32): error CS0518: Predefined type 'System.Void' is not defined or imported
                //    public int AProperty { get; set; }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "set;").WithArguments("System.Void"),
                // (1,7): error CS1729: 'object' does not contain a constructor that takes 0 arguments
                // class C 
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "C").WithArguments("object", "0"));

            foreach (var diag in compilation.GetDiagnostics())
            {
                Assert.DoesNotContain("System.Runtime.CompilerServices.CompilerGeneratedAttribute", diag.GetMessage(), StringComparison.Ordinal);
            }
        }

        [Fact]
        public void UseSiteErrorsForSwitchSubsumption()
        {
            var baseSource =
@"public class Base {}";
            var baseLib = CreateCompilation(baseSource, assemblyName: "BaseAssembly");
            var derivedSource =
@"public class Derived : Base {}";
            var derivedLib = CreateCompilation(derivedSource, assemblyName: "DerivedAssembly", references: new[] { new CSharpCompilationReference(baseLib) });
            var programSource =
@"
class Program
{
    public static void Main(string[] args)
    {
        object o = args;
        switch (o)
        {
            case string s: break;
            case Derived d: break;
        }
    }
}
";
            CreateCompilation(programSource, references: new[] { new CSharpCompilationReference(derivedLib) }).VerifyDiagnostics(
                // (9,18): error CS0012: The type 'Base' is defined in an assembly that is not referenced. You must add a reference to assembly 'BaseAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             case string s: break;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "string s").WithArguments("Base", "BaseAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(9, 18)
                );
        }

        #region Attributes for unsafe code

        /// <summary>
        /// Simple test to verify that the infrastructure for the other UnsafeAttributes_* tests works correctly.
        /// </summary>
        [Fact]
        public void UnsafeAttributes_NoErrors()
        {
            var text = unsafeAttributeSystemTypes + @"
namespace System.Security
{
    public class UnverifiableCodeAttribute : Attribute { }

    namespace Permissions
    {
        public enum SecurityAction
        {
            RequestMinimum
        }

        public class CodeAccessSecurityAttribute : Attribute
        {
            public CodeAccessSecurityAttribute(SecurityAction action)
            {
            }
        }

        public class SecurityPermissionAttribute : CodeAccessSecurityAttribute
        {
            public SecurityPermissionAttribute(SecurityAction action)
                : base(action)
            {
            }

            public bool SkipVerification { get; set; }
        }
    }
}
";

            CompileUnsafeAttributesAndCheckDiagnostics(text, false,
                // (19,21): error CS0518: Predefined type 'System.Int32' is not defined or imported
                //         public enum SecurityAction
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "SecurityAction").WithArguments("System.Int32"));
            CompileUnsafeAttributesAndCheckDiagnostics(text, true,
                // (19,21): error CS0518: Predefined type 'System.Int32' is not defined or imported
                //         public enum SecurityAction
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "SecurityAction").WithArguments("System.Int32"));
        }

        /// <summary>
        /// If the attribute type is missing, just skip emitting the attributes.
        /// No diagnostics.
        /// </summary>
        [Fact]
        public void UnsafeAttributes_MissingUnverifiableCodeAttribute()
        {
            var text = unsafeAttributeSystemTypes + @"
namespace System.Security
{
    public class UnverifiableCodeAttribute : Attribute { }

    namespace Permissions
    {
        public enum SecurityAction
        {
            RequestMinimum
        }

        public class CodeAccessSecurityAttribute : Attribute
        {
            public CodeAccessSecurityAttribute(SecurityAction action)
            {
            }
        }

        public class SecurityPermissionAttribute : CodeAccessSecurityAttribute
        {
            public SecurityPermissionAttribute(SecurityAction action)
                : base(action)
            {
            }

            public bool SkipVerification { get; set; }
        }
    }
}
";

            CompileUnsafeAttributesAndCheckDiagnostics(text, false,
                // (19,21): error CS0518: Predefined type 'System.Int32' is not defined or imported
                //         public enum SecurityAction
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "SecurityAction").WithArguments("System.Int32"));
            CompileUnsafeAttributesAndCheckDiagnostics(text, true,
                // (19,21): error CS0518: Predefined type 'System.Int32' is not defined or imported
                //         public enum SecurityAction
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "SecurityAction").WithArguments("System.Int32"));
        }

        /// <summary>
        /// If the attribute type is missing, just skip emitting the attributes.
        /// No diagnostics.
        /// </summary>
        [Fact]
        public void UnsafeAttributes_MissingSecurityPermissionAttribute()
        {
            var text = unsafeAttributeSystemTypes + @"
namespace System.Security
{
    public class UnverifiableCodeAttribute : Attribute { }

    namespace Permissions
    {
        public enum SecurityAction
        {
            RequestMinimum
        }
    }
}
";

            CompileUnsafeAttributesAndCheckDiagnostics(text, false,
                // (19,21): error CS0518: Predefined type 'System.Int32' is not defined or imported
                //         public enum SecurityAction
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "SecurityAction").WithArguments("System.Int32"));
            CompileUnsafeAttributesAndCheckDiagnostics(text, true,
                // (19,21): error CS0518: Predefined type 'System.Int32' is not defined or imported
                //         public enum SecurityAction
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "SecurityAction").WithArguments("System.Int32"));
        }

        /// <summary>
        /// If the enum type is missing, just skip emitting the attributes.
        /// No diagnostics.
        /// </summary>
        [Fact]
        public void UnsafeAttributes_MissingSecurityAction()
        {
            var text = unsafeAttributeSystemTypes + @"
namespace System.Security
{
    public class UnverifiableCodeAttribute : Attribute { }

    namespace Permissions
    {
        public class CodeAccessSecurityAttribute : Attribute
        {
        }

        public class SecurityPermissionAttribute : CodeAccessSecurityAttribute
        {
            public bool SkipVerification { get; set; }
        }
    }
}
";

            CompileUnsafeAttributesAndCheckDiagnostics(text, false);
            CompileUnsafeAttributesAndCheckDiagnostics(text, true);
        }

        /// <summary>
        /// If the attribute constructor is missing, report a use site error.
        /// </summary>
        [Fact]
        public void UnsafeAttributes_MissingUnverifiableCodeAttributeCtorMissing()
        {
            var text = unsafeAttributeSystemTypes + @"
namespace System.Security
{
    public class UnverifiableCodeAttribute : Attribute
    {
        public UnverifiableCodeAttribute(object o1, object o2) { } //wrong signature, won't be found
    }

    namespace Permissions
    {
        public enum SecurityAction
        {
            RequestMinimum
        }

        public class CodeAccessSecurityAttribute : Attribute
        {
            public CodeAccessSecurityAttribute(SecurityAction action)
            {
            }
        }

        public class SecurityPermissionAttribute : CodeAccessSecurityAttribute
        {
            public SecurityPermissionAttribute(SecurityAction action)
                : base(action)
            {
            }

            public bool SkipVerification { get; set; }
        }
    }
}
";

            CompileUnsafeAttributesAndCheckDiagnostics(text, false,
                // error CS0656: Missing compiler required member 'System.Security.UnverifiableCodeAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Security.UnverifiableCodeAttribute", ".ctor"),
                // (22,21): error CS0518: Predefined type 'System.Int32' is not defined or imported
                //         public enum SecurityAction
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "SecurityAction").WithArguments("System.Int32"));
            CompileUnsafeAttributesAndCheckDiagnostics(text, true,
                // error CS0656: Missing compiler required member 'System.Security.UnverifiableCodeAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Security.UnverifiableCodeAttribute", ".ctor"),
                // (22,21): error CS0518: Predefined type 'System.Int32' is not defined or imported
                //         public enum SecurityAction
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "SecurityAction").WithArguments("System.Int32"));
        }

        /// <summary>
        /// If the attribute constructor is missing, report a use site error.
        /// </summary>
        [Fact]
        public void UnsafeAttributes_SecurityPermissionAttributeCtorMissing()
        {
            var text = unsafeAttributeSystemTypes + @"
namespace System.Security
{
    public class UnverifiableCodeAttribute : Attribute
    {
    }

    namespace Permissions
    {
        public enum SecurityAction
        {
            RequestMinimum
        }

        public class CodeAccessSecurityAttribute : Attribute
        {
            public CodeAccessSecurityAttribute(SecurityAction action)
            {
            }
        }

        public class SecurityPermissionAttribute : CodeAccessSecurityAttribute
        {
            public SecurityPermissionAttribute(SecurityAction action, object o) //extra parameter will fail to match well-known signature
                : base(action)
            {
            }

            public bool SkipVerification { get; set; }
        }
    }
}
";

            CompileUnsafeAttributesAndCheckDiagnostics(text, false,
                // error CS0656: Missing compiler required member 'System.Security.Permissions.SecurityPermissionAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Security.Permissions.SecurityPermissionAttribute", ".ctor"),
                // (21,21): error CS0518: Predefined type 'System.Int32' is not defined or imported
                //         public enum SecurityAction
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "SecurityAction").WithArguments("System.Int32"));

            CompileUnsafeAttributesAndCheckDiagnostics(text, true,
                // error CS0656: Missing compiler required member 'System.Security.Permissions.SecurityPermissionAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Security.Permissions.SecurityPermissionAttribute", ".ctor"),
                // (21,21): error CS0518: Predefined type 'System.Int32' is not defined or imported
                //         public enum SecurityAction
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "SecurityAction").WithArguments("System.Int32"));
        }

        /// <summary>
        /// If the attribute property is missing, report a use site error.
        /// </summary>
        [Fact]
        public void UnsafeAttributes_SecurityPermissionAttributePropertyMissing()
        {
            var text = unsafeAttributeSystemTypes + @"
namespace System.Security
{
    public class UnverifiableCodeAttribute : Attribute
    {
    }

    namespace Permissions
    {
        public enum SecurityAction
        {
            RequestMinimum
        }

        public class CodeAccessSecurityAttribute : Attribute
        {
            public CodeAccessSecurityAttribute(SecurityAction action)
            {
            }
        }

        public class SecurityPermissionAttribute : CodeAccessSecurityAttribute
        {
            public SecurityPermissionAttribute(SecurityAction action)
                : base(action)
            {
            }
        }
    }
}
";

            CompileUnsafeAttributesAndCheckDiagnostics(text, false,
                // error CS0656: Missing compiler required member 'System.Security.Permissions.SecurityPermissionAttribute.SkipVerification'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Security.Permissions.SecurityPermissionAttribute", "SkipVerification"),
                // (21,21): error CS0518: Predefined type 'System.Int32' is not defined or imported
                //         public enum SecurityAction
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "SecurityAction").WithArguments("System.Int32"));
            CompileUnsafeAttributesAndCheckDiagnostics(text, true,
                // error CS0656: Missing compiler required member 'System.Security.Permissions.SecurityPermissionAttribute.SkipVerification'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Security.Permissions.SecurityPermissionAttribute", "SkipVerification"),
                // (21,21): error CS0518: Predefined type 'System.Int32' is not defined or imported
                //         public enum SecurityAction
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "SecurityAction").WithArguments("System.Int32"));
        }

        [WorkItem(708169, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/708169")]
        [Fact]
        public void OverloadResolutionWithUseSiteErrors()
        {
            var missingSource = @"
public class Missing { }
";

            var libSource = @"
public class Methods
{
    public static void M1(int x) { }
    public static void M1(Missing x) { }
    
    public static void M2(Missing x) { }
    public static void M2(int x) { }
}

public class Indexer1
{
    public int this[int x] { get { return 0; } }
    public int this[Missing x] { get { return 0; } }
}

public class Indexer2
{
    public int this[Missing x] { get { return 0; } }
    public int this[int x] { get { return 0; } }
}

public class Constructor1
{
    public Constructor1(int x) { }
    public Constructor1(Missing x) { }
}

public class Constructor2
{
    public Constructor2(Missing x) { }
    public Constructor2(int x) { }
}
";

            var testSource = @"
using System;

class C
{
    static void Main()
    {
        var c1 = new Constructor1(1);
        var c2 = new Constructor2(2);

        Methods.M1(1);
        Methods.M2(2);

        Action<int> a1 = Methods.M1;
        Action<int> a2 = Methods.M2;

        var i1 = new Indexer1()[1];
        var i2 = new Indexer2()[2];
    }
}
";
            var missingRef = CreateCompilation(missingSource, assemblyName: "Missing").EmitToImageReference();
            var libRef = CreateCompilation(libSource, new[] { missingRef }).EmitToImageReference();
            CreateCompilation(testSource, new[] { libRef /* and not missingRef */ }).VerifyDiagnostics(
                // (8,22): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         var c1 = new Constructor1(1);
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Constructor1").WithArguments("Missing", "Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (9,22): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         var c2 = new Constructor2(2);
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Constructor2").WithArguments("Missing", "Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),

                // (9,9): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         Methods.M1(1);
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Methods.M1").WithArguments("Missing", "Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (10,9): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         Methods.M2(2);
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Methods.M2").WithArguments("Missing", "Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),

                // (14,26): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         Action<int> a1 = Methods.M1;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Methods.M1").WithArguments("Missing", "Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (15,26): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         Action<int> a2 = Methods.M2;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Methods.M2").WithArguments("Missing", "Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),

                // (17,18): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         var i1 = new Indexer1()[1];
                Diagnostic(ErrorCode.ERR_NoTypeDef, "new Indexer1()[1]").WithArguments("Missing", "Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (18,18): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         var i2 = new Indexer2()[2];
                Diagnostic(ErrorCode.ERR_NoTypeDef, "new Indexer2()[2]").WithArguments("Missing", "Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [WorkItem(708169, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/708169")]
        [Fact]
        public void OverloadResolutionWithUseSiteErrors_LessDerived()
        {
            var missingSource = @"
public class Missing { }
";

            var libSource = @"
public class Base
{
    public int M(int x) { return 0; }
    public int M(Missing x) { return 0; }

    public int this[int x] { get { return 0; } }
    public int this[Missing x] { get { return 0; } }
}
";

            var testSource = @"
class Derived : Base
{
    static void Main()
    {
        var d = new Derived();
        int i;

        i = d.M(1);
        i = d.M(""A"");

        i = d[1];
        i = d[""A""];
    }

    public int M(string x) { return 0; }

    public int this[string x] { get { return 0; } }
}
";
            var missingRef = CreateCompilation(missingSource, assemblyName: "Missing").EmitToImageReference();
            var libRef = CreateCompilation(libSource, new[] { missingRef }).EmitToImageReference();
            CreateCompilation(testSource, new[] { libRef, missingRef }).VerifyDiagnostics();

            // NOTE: No errors reported when the Derived member wins.
            CreateCompilation(testSource, new[] { libRef /* and not missingRef */ }).VerifyDiagnostics(
                // (9,13): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         i = d.M(1);
                Diagnostic(ErrorCode.ERR_NoTypeDef, "d.M").WithArguments("Missing", "Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (12,13): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         i = d[1];
                Diagnostic(ErrorCode.ERR_NoTypeDef, "d[1]").WithArguments("Missing", "Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [WorkItem(708169, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/708169")]
        [Fact]
        public void OverloadResolutionWithUseSiteErrors_NoCorrespondingParameter()
        {
            var missingSource = @"
public class Missing { }
";

            var libSource = @"
public class C
{
    public C(string x, int y = 1) { }
    public C(Missing x) { }

    public int M(string x, int y = 1) { return 0; }
    public int M(Missing x) { return 0; }

    public int this[string x, int y = 1] { get { return 0; } }
    public int this[Missing x] { get { return 0; } }
}
";

            var testSource = @"
class Test
{
    static void Main()
    {
        C c;
        int i;

        c = new C(null, 1); // Fine
        c = new C(""A""); // Error

        i = c.M(null, 1); // Fine
        i = c.M(""A""); // Error

        i = c[null, 1]; // Fine
        i = c[""A""]; // Error
    }
}
";
            var missingRef = CreateCompilation(missingSource, assemblyName: "Missing").EmitToImageReference();
            var libRef = CreateCompilation(libSource, new[] { missingRef }).EmitToImageReference();
            CreateCompilation(testSource, new[] { libRef, missingRef }).VerifyDiagnostics();

            CreateCompilation(testSource, new[] { libRef /* and not missingRef */ }).VerifyDiagnostics(
                // (10,17): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         c = new C("A"); // Error
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C").WithArguments("Missing", "Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (13,13): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         i = c.M("A"); // Error
                Diagnostic(ErrorCode.ERR_NoTypeDef, "c.M").WithArguments("Missing", "Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (16,13): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         i = c["A"]; // Error
                Diagnostic(ErrorCode.ERR_NoTypeDef, @"c[""A""]").WithArguments("Missing", "Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [WorkItem(708169, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/708169")]
        [Fact]
        public void OverloadResolutionWithUseSiteErrors_NameUsedForPositional()
        {
            var missingSource = @"
public class Missing { }
";

            var libSource = @"
public class C
{
    public C(string x, string y) { }
    public C(Missing y, string x) { }

    public int M(string x, string y) { return 0; }
    public int M(Missing y, string x) { return 0; }

    public int this[string x, string y] { get { return 0; } }
    public int this[Missing y, string x] { get { return 0; } }
}
";

            var testSource = @"
class Test
{
    static void Main()
    {
        C c;
        int i;

        c = new C(""A"", y: null); // Fine
        c = new C(""A"", null); // Error

        i = c.M(""A"", y: null); // Fine
        i = c.M(""A"", null); // Error

        i = c[""A"", y: null]; // Fine
        i = c[""A"", null]; // Error
    }
}
";
            var missingRef = CreateCompilation(missingSource, assemblyName: "Missing").EmitToImageReference();
            var libRef = CreateCompilation(libSource, new[] { missingRef }).EmitToImageReference();
            CreateCompilation(testSource, new[] { libRef, missingRef }).VerifyDiagnostics();

            CreateCompilation(testSource, new[] { libRef /* and not missingRef */ }).VerifyDiagnostics(
                // (10,17): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         c = new C("A", null); // Error
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C").WithArguments("Missing", "Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (13,13): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         i = c.M("A", null); // Error
                Diagnostic(ErrorCode.ERR_NoTypeDef, "c.M").WithArguments("Missing", "Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (16,13): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         i = c["A", null]; // Error
                Diagnostic(ErrorCode.ERR_NoTypeDef, @"c[""A"", null]").WithArguments("Missing", "Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [WorkItem(708169, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/708169")]
        [Fact]
        public void OverloadResolutionWithUseSiteErrors_RequiredParameterMissing()
        {
            var missingSource = @"
public class Missing { }
";

            var libSource = @"
public class C
{
    public C(string x, object y = null) { }
    public C(Missing x, string y) { }

    public int M(string x, object y = null) { return 0; }
    public int M(Missing x, string y) { return 0; }

    public int this[string x, object y = null] { get { return 0; } }
    public int this[Missing x, string y] { get { return 0; } }
}
";

            var testSource = @"
class Test
{
    static void Main()
    {
        C c;
        int i;

        c = new C(null); // Fine
        c = new C(null, ""A""); // Error

        i = c.M(null); // Fine
        i = c.M(null, ""A""); // Error

        i = c[null]; // Fine
        i = c[null, ""A""]; // Error
    }
}
";
            var missingRef = CreateCompilation(missingSource, assemblyName: "Missing").EmitToImageReference();
            var libRef = CreateCompilation(libSource, new[] { missingRef }).EmitToImageReference();
            CreateCompilation(testSource, new[] { libRef, missingRef }).VerifyDiagnostics();

            CreateCompilation(testSource, new[] { libRef /* and not missingRef */ }).VerifyDiagnostics(
                // (10,17): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         c = new C(null, "A"); // Error
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C").WithArguments("Missing", "Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (13,13): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         i = c.M(null, "A"); // Error
                Diagnostic(ErrorCode.ERR_NoTypeDef, "c.M").WithArguments("Missing", "Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (16,13): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         i = c[null, "A"]; // Error
                Diagnostic(ErrorCode.ERR_NoTypeDef, @"c[null, ""A""]").WithArguments("Missing", "Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [Fact]
        public void OverloadResolutionWithUseSiteErrors_WithParamsArguments_ReturnsUseSiteErrors()
        {
            var missingSource = @"
public class Missing { }
";

            var libSource = @"
public class C
{
    public static Missing GetMissing(params int[] args) { return null; }
    public static void SetMissing(params Missing[] args) { }
    public static Missing GetMissing(string firstArgument, params int[] args) { return null; }
    public static void SetMissing(string firstArgument, params Missing[] args) { }
}
";

            var testSource = @"
class Test
{
    static void Main()
    {
        C.GetMissing();
        C.GetMissing(1, 1);
        C.SetMissing();
        C.GetMissing(string.Empty);
        C.GetMissing(string.Empty, 1, 1);
        C.SetMissing(string.Empty);
    }
}
";
            var missingRef = CreateCompilation(missingSource, assemblyName: "Missing").EmitToImageReference();
            var libRef = CreateCompilation(libSource, new[] { missingRef }).EmitToImageReference();
            CreateCompilation(testSource, new[] { libRef, missingRef }).VerifyDiagnostics();
            var getMissingDiagnostic = Diagnostic(ErrorCode.ERR_NoTypeDef, @"C.GetMissing").WithArguments("Missing", "Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
            var setMissingDiagnostic = Diagnostic(ErrorCode.ERR_NoTypeDef, @"C.SetMissing").WithArguments("Missing", "Missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
            CreateCompilation(testSource, new[] { libRef /* and not missingRef */ }).VerifyDiagnostics(
                getMissingDiagnostic,
                getMissingDiagnostic,
                setMissingDiagnostic,
                getMissingDiagnostic,
                getMissingDiagnostic,
                setMissingDiagnostic);
        }

        [WorkItem(708169, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/708169")]
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void OverloadResolutionWithUnsupportedMetadata_UnsupportedMetadata_SupportedExists()
        {
            var il = @"
.class public auto ansi beforefieldinit Methods
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  M(int32 x) cil managed
  {
    ret
  }

  .method public hidebysig static void  M(string modreq(int16) x) cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

} // end of class Methods

.class public auto ansi beforefieldinit Indexers
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('Item')}
  .method public hidebysig specialname instance int32 
          get_Item(int32 x) cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig specialname instance int32 
          get_Item(string modreq(int16) x) cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .property instance int32 Item(int32)
  {
    .get instance int32 Indexers::get_Item(int32)
  }
  .property instance int32 Item(string modreq(int16))
  {
    .get instance int32 Indexers::get_Item(string modreq(int16))
  }
} // end of class Indexers

.class public auto ansi beforefieldinit Constructors
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(int32 x) cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string modreq(int16) x) cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

} // end of class Constructors
";

            var source = @"
using System;

class C
{
    static void Main()
    {
        var c1 = new Constructors(1);
        var c2 = new Constructors(null);

        Methods.M(1);
        Methods.M(null);

        Action<int> a1 = Methods.M;
        Action<string> a2 = Methods.M;

        var i1 = new Indexers()[1];
        var i2 = new Indexers()[null];
    }
}
";
            CreateCompilationWithILAndMscorlib40(source, il).VerifyDiagnostics(
                // (9,35): error CS1503: Argument 1: cannot convert from '<null>' to 'int'
                //         var c2 = new Constructors(null);
                Diagnostic(ErrorCode.ERR_BadArgType, "null").WithArguments("1", "<null>", "int"),
                // (12,19): error CS1503: Argument 1: cannot convert from '<null>' to 'int'
                //         Methods.M(null);
                Diagnostic(ErrorCode.ERR_BadArgType, "null").WithArguments("1", "<null>", "int"),
                // (15,37): error CS0123: No overload for 'M' matches delegate 'System.Action<string>'
                //         Action<string> a2 = Methods.M;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "M").WithArguments("M", "System.Action<string>"),
                // (18,33): error CS1503: Argument 1: cannot convert from '<null>' to 'int'
                //         var i2 = new Indexers()[null];
                Diagnostic(ErrorCode.ERR_BadArgType, "null").WithArguments("1", "<null>", "int"));
        }

        [WorkItem(708169, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/708169")]
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void OverloadResolutionWithUnsupportedMetadata_UnsupportedMetadata_SupportedDoesNotExist()
        {
            var il = @"
.class public auto ansi beforefieldinit Methods
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  M(string modreq(int16) x) cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

} // end of class Methods

.class public auto ansi beforefieldinit Indexers
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('Item')}
  .method public hidebysig specialname instance int32 
          get_Item(string modreq(int16) x) cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .property instance int32 Item(string modreq(int16))
  {
    .get instance int32 Indexers::get_Item(string modreq(int16))
  }
} // end of class Indexers

.class public auto ansi beforefieldinit Constructors
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string modreq(int16) x) cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

} // end of class Constructors
";

            var source = @"
using System;

class C
{
    static void Main()
    {
        var c2 = new Constructors(null);

        Methods.M(null);

        Action<string> a2 = Methods.M;

        var i2 = new Indexers()[null];
    }
}
";
            CreateCompilationWithILAndMscorlib40(source, il).VerifyDiagnostics(
                // (8,22): error CS0570: 'Constructors.Constructors(?)' is not supported by the language
                //         var c2 = new Constructors(null);
                Diagnostic(ErrorCode.ERR_BindToBogus, "Constructors").WithArguments("Constructors.Constructors(?)"),
                // (10,9): error CS0570: 'Methods.M(?)' is not supported by the language
                //         Methods.M(null);
                Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("Methods.M(?)"),
                // (12,29): error CS0570: 'Methods.M(?)' is not supported by the language
                //         Action<string> a2 = Methods.M;
                Diagnostic(ErrorCode.ERR_BindToBogus, "Methods.M").WithArguments("Methods.M(?)"),
                // (14,18): error CS1546: Property, indexer, or event 'Indexers.this[?]' is not supported by the language; try directly calling accessor method 'Indexers.get_Item(?)'
                //         var i2 = new Indexers()[null];
                Diagnostic(ErrorCode.ERR_BindToBogusProp1, "new Indexers()[null]").WithArguments("Indexers.this[?]", "Indexers.get_Item(?)"));
        }

        [WorkItem(939928, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939928")]
        [WorkItem(132, "CodePlex")]
        [Fact]
        public void MissingBaseTypeForCatch()
        {
            var source1 = @"
using System;
public class GeneralException : Exception {}";

            CSharpCompilation comp1 = CreateCompilation(source1, assemblyName: "Base");

            var source2 = @"
public class SpecificException : GeneralException
{}";

            CSharpCompilation comp2 = CreateCompilation(source2, new MetadataReference[] { new CSharpCompilationReference(comp1) });

            var source3 = @"
class Test
{
    static void Main(string[] args)
    {
            try 
            { 
                SpecificException e = null;
                throw e;
            }
            catch (SpecificException) 
            {
            }
    }
}";

            CSharpCompilation comp3 = CreateCompilation(source3, new MetadataReference[] { new CSharpCompilationReference(comp2) });

            DiagnosticDescription[] expected =
            {
                // (9,23): error CS0012: The type 'GeneralException' is defined in an assembly that is not referenced. You must add a reference to assembly 'Base, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //                 throw e;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "e").WithArguments("GeneralException", "Base, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(9, 23),
                // (9,23): error CS0029: Cannot implicitly convert type 'SpecificException' to 'System.Exception'
                //                 throw e;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "e").WithArguments("SpecificException", "System.Exception").WithLocation(9, 23),
                // (11,20): error CS0155: The type caught or thrown must be derived from System.Exception
                //             catch (SpecificException) 
                Diagnostic(ErrorCode.ERR_BadExceptionType, "SpecificException").WithLocation(11, 20),
                // (11,20): error CS0012: The type 'GeneralException' is defined in an assembly that is not referenced. You must add a reference to assembly 'Base, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             catch (SpecificException) 
                Diagnostic(ErrorCode.ERR_NoTypeDef, "SpecificException").WithArguments("GeneralException", "Base, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(11, 20)
            };

            comp3.VerifyDiagnostics(expected);

            comp3 = CreateCompilation(source3, new MetadataReference[] { comp2.EmitToImageReference() });

            comp3.VerifyDiagnostics(expected);
        }

        /// <summary>
        /// Trivial definitions of special types that will be required for testing use site errors in
        /// the attributes emitted for unsafe assemblies.
        /// </summary>
        private const string unsafeAttributeSystemTypes = @"
namespace System
{
    public class Object { }
    public class ValueType { }
    public class Enum { } // Diagnostic if this extends ValueType
    public struct Boolean { }
    public struct Void { }

    public class Attribute { }
}
";

        /// <summary>
        /// Compile without corlib, and then verify semantic diagnostics, emit-metadata diagnostics, and emit diagnostics.
        /// </summary>
        private static void CompileUnsafeAttributesAndCheckDiagnostics(string corLibText, bool moduleOnly, params DiagnosticDescription[] expectedDiagnostics)
        {
            CSharpCompilationOptions options = TestOptions.UnsafeReleaseDll;
            if (moduleOnly)
            {
                options = options.WithOutputKind(OutputKind.NetModule);
            }

            var compilation = CreateEmptyCompilation(
                new[] { Parse(corLibText) },
                options: options);
            compilation.VerifyDiagnostics(expectedDiagnostics);
        }

        #endregion Attributes for unsafe code

        /// <summary>
        /// First, compile the provided source with all assemblies and confirm that there are no errors.
        /// Then, compile the provided source again without the unavailable assembly and return the result.
        /// </summary>
        private static CSharpCompilation CompileWithMissingReference(string source)
        {
            var unavailableAssemblyReference = TestReferences.SymbolsTests.UseSiteErrors.Unavailable;
            var csharpAssemblyReference = TestReferences.SymbolsTests.UseSiteErrors.CSharp;
            var ilAssemblyReference = TestReferences.SymbolsTests.UseSiteErrors.IL;

            var successfulCompilation = CreateCompilation(source, new MetadataReference[] { unavailableAssemblyReference, csharpAssemblyReference, ilAssemblyReference });
            successfulCompilation.VerifyDiagnostics(); // No diagnostics when reference is present

            var failingCompilation = CreateCompilation(source, new MetadataReference[] { csharpAssemblyReference, ilAssemblyReference });
            return failingCompilation;
        }

        [Fact]
        [WorkItem(14267, "https://github.com/dotnet/roslyn/issues/14267")]
        public void MissingTypeKindBasisTypes()
        {
            var source1 = @"
public struct A {}

public enum B {}

public class C {}
public delegate void D();

public interface I1 {}
";
            var compilation1 = CreateEmptyCompilation(source1, options: TestOptions.ReleaseDll, references: new[] { MinCorlibRef });
            compilation1.VerifyEmitDiagnostics();

            Assert.Equal(TypeKind.Struct, compilation1.GetTypeByMetadataName("A").TypeKind);
            Assert.Equal(TypeKind.Enum, compilation1.GetTypeByMetadataName("B").TypeKind);
            Assert.Equal(TypeKind.Class, compilation1.GetTypeByMetadataName("C").TypeKind);
            Assert.Equal(TypeKind.Delegate, compilation1.GetTypeByMetadataName("D").TypeKind);
            Assert.Equal(TypeKind.Interface, compilation1.GetTypeByMetadataName("I1").TypeKind);

            var source2 = @"
interface I2
{
    I1 M(A a, B b, C c, D d); 
}
";

            var compilation2 = CreateEmptyCompilation(source2, options: TestOptions.ReleaseDll, references: new[] { compilation1.EmitToImageReference(), MinCorlibRef });

            compilation2.VerifyEmitDiagnostics();
            CompileAndVerify(compilation2);

            Assert.Equal(TypeKind.Struct, compilation2.GetTypeByMetadataName("A").TypeKind);
            Assert.Equal(TypeKind.Enum, compilation2.GetTypeByMetadataName("B").TypeKind);
            Assert.Equal(TypeKind.Class, compilation2.GetTypeByMetadataName("C").TypeKind);
            Assert.Equal(TypeKind.Delegate, compilation2.GetTypeByMetadataName("D").TypeKind);
            Assert.Equal(TypeKind.Interface, compilation2.GetTypeByMetadataName("I1").TypeKind);

            var compilation3 = CreateEmptyCompilation(source2, options: TestOptions.ReleaseDll, references: new[] { compilation1.ToMetadataReference(), MinCorlibRef });

            compilation3.VerifyEmitDiagnostics();
            CompileAndVerify(compilation3);

            Assert.Equal(TypeKind.Struct, compilation3.GetTypeByMetadataName("A").TypeKind);
            Assert.Equal(TypeKind.Enum, compilation3.GetTypeByMetadataName("B").TypeKind);
            Assert.Equal(TypeKind.Class, compilation3.GetTypeByMetadataName("C").TypeKind);
            Assert.Equal(TypeKind.Delegate, compilation3.GetTypeByMetadataName("D").TypeKind);
            Assert.Equal(TypeKind.Interface, compilation3.GetTypeByMetadataName("I1").TypeKind);

            var compilation4 = CreateEmptyCompilation(source2, options: TestOptions.ReleaseDll, references: new[] { compilation1.EmitToImageReference() });

            compilation4.VerifyDiagnostics(
                // (4,10): error CS0012: The type 'ValueType' is defined in an assembly that is not referenced. You must add a reference to assembly 'mincorlib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2'.
                //     I1 M(A a, B b, C c, D d); 
                Diagnostic(ErrorCode.ERR_NoTypeDef, "A").WithArguments("System.ValueType", "mincorlib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2").WithLocation(4, 10),
                // (4,15): error CS0012: The type 'Enum' is defined in an assembly that is not referenced. You must add a reference to assembly 'mincorlib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2'.
                //     I1 M(A a, B b, C c, D d); 
                Diagnostic(ErrorCode.ERR_NoTypeDef, "B").WithArguments("System.Enum", "mincorlib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2").WithLocation(4, 15),
                // (4,25): error CS0012: The type 'MulticastDelegate' is defined in an assembly that is not referenced. You must add a reference to assembly 'mincorlib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2'.
                //     I1 M(A a, B b, C c, D d); 
                Diagnostic(ErrorCode.ERR_NoTypeDef, "D").WithArguments("System.MulticastDelegate", "mincorlib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2").WithLocation(4, 25)
                );

            var a = compilation4.GetTypeByMetadataName("A");
            var b = compilation4.GetTypeByMetadataName("B");
            var c = compilation4.GetTypeByMetadataName("C");
            var d = compilation4.GetTypeByMetadataName("D");
            var i1 = compilation4.GetTypeByMetadataName("I1");
            Assert.Equal(TypeKind.Class, a.TypeKind);
            Assert.NotNull(a.GetUseSiteDiagnostic());
            Assert.Equal(TypeKind.Class, b.TypeKind);
            Assert.NotNull(b.GetUseSiteDiagnostic());
            Assert.Equal(TypeKind.Class, c.TypeKind);
            Assert.Null(c.GetUseSiteDiagnostic());
            Assert.Equal(TypeKind.Class, d.TypeKind);
            Assert.NotNull(d.GetUseSiteDiagnostic());
            Assert.Equal(TypeKind.Interface, i1.TypeKind);
            Assert.Null(i1.GetUseSiteDiagnostic());

            var compilation5 = CreateEmptyCompilation(source2, options: TestOptions.ReleaseDll, references: new[] { compilation1.ToMetadataReference() });

            compilation5.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1)
                );
            CompileAndVerify(compilation5);

            Assert.Equal(TypeKind.Struct, compilation5.GetTypeByMetadataName("A").TypeKind);
            Assert.Equal(TypeKind.Enum, compilation5.GetTypeByMetadataName("B").TypeKind);
            Assert.Equal(TypeKind.Class, compilation5.GetTypeByMetadataName("C").TypeKind);
            Assert.Equal(TypeKind.Delegate, compilation5.GetTypeByMetadataName("D").TypeKind);
            Assert.Equal(TypeKind.Interface, compilation5.GetTypeByMetadataName("I1").TypeKind);

            var compilation6 = CreateEmptyCompilation(source2, options: TestOptions.ReleaseDll, references: new[] { compilation1.EmitToImageReference(), MscorlibRef });

            compilation6.VerifyDiagnostics(
                // (4,10): error CS0012: The type 'ValueType' is defined in an assembly that is not referenced. You must add a reference to assembly 'mincorlib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2'.
                //     I1 M(A a, B b, C c, D d); 
                Diagnostic(ErrorCode.ERR_NoTypeDef, "A").WithArguments("System.ValueType", "mincorlib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2").WithLocation(4, 10),
                // (4,15): error CS0012: The type 'Enum' is defined in an assembly that is not referenced. You must add a reference to assembly 'mincorlib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2'.
                //     I1 M(A a, B b, C c, D d); 
                Diagnostic(ErrorCode.ERR_NoTypeDef, "B").WithArguments("System.Enum", "mincorlib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2").WithLocation(4, 15),
                // (4,25): error CS0012: The type 'MulticastDelegate' is defined in an assembly that is not referenced. You must add a reference to assembly 'mincorlib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2'.
                //     I1 M(A a, B b, C c, D d); 
                Diagnostic(ErrorCode.ERR_NoTypeDef, "D").WithArguments("System.MulticastDelegate", "mincorlib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2").WithLocation(4, 25)
                );

            a = compilation6.GetTypeByMetadataName("A");
            b = compilation6.GetTypeByMetadataName("B");
            c = compilation6.GetTypeByMetadataName("C");
            d = compilation6.GetTypeByMetadataName("D");
            i1 = compilation6.GetTypeByMetadataName("I1");
            Assert.Equal(TypeKind.Class, a.TypeKind);
            Assert.NotNull(a.GetUseSiteDiagnostic());
            Assert.Equal(TypeKind.Class, b.TypeKind);
            Assert.NotNull(b.GetUseSiteDiagnostic());
            Assert.Equal(TypeKind.Class, c.TypeKind);
            Assert.Null(c.GetUseSiteDiagnostic());
            Assert.Equal(TypeKind.Class, d.TypeKind);
            Assert.NotNull(d.GetUseSiteDiagnostic());
            Assert.Equal(TypeKind.Interface, i1.TypeKind);
            Assert.Null(i1.GetUseSiteDiagnostic());

            var compilation7 = CreateEmptyCompilation(source2, options: TestOptions.ReleaseDll, references: new[] { compilation1.ToMetadataReference(), MscorlibRef });

            compilation7.VerifyEmitDiagnostics();
            CompileAndVerify(compilation7);

            Assert.Equal(TypeKind.Struct, compilation7.GetTypeByMetadataName("A").TypeKind);
            Assert.Equal(TypeKind.Enum, compilation7.GetTypeByMetadataName("B").TypeKind);
            Assert.Equal(TypeKind.Class, compilation7.GetTypeByMetadataName("C").TypeKind);
            Assert.Equal(TypeKind.Delegate, compilation7.GetTypeByMetadataName("D").TypeKind);
            Assert.Equal(TypeKind.Interface, compilation7.GetTypeByMetadataName("I1").TypeKind);
        }

        [Fact, WorkItem(15435, "https://github.com/dotnet/roslyn/issues/15435")]
        public void TestGettingAssemblyIdsFromDiagnostic1()
        {
            var text = @"
class C : CSharpErrors.ClassMethods
{
    public override UnavailableClass ReturnType1() { return null; }
    public override UnavailableClass[] ReturnType2() { return null; }
}";

            var compilation = CompileWithMissingReference(text);
            var diagnostics = compilation.GetDiagnostics();
            Assert.True(diagnostics.Any(d => d.Code == (int)ErrorCode.ERR_NoTypeDef));

            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Code == (int)ErrorCode.ERR_NoTypeDef)
                {
                    var actualAssemblyId = compilation.GetUnreferencedAssemblyIdentities(diagnostic).Single();
                    AssemblyIdentity expectedAssemblyId;
                    AssemblyIdentity.TryParseDisplayName("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", out expectedAssemblyId);

                    Assert.Equal(actualAssemblyId, expectedAssemblyId);
                }
            }
        }
    }
}
