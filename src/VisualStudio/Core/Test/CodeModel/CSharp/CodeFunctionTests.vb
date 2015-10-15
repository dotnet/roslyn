' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel.Extenders
Imports Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodeFunctionTests
        Inherits AbstractCodeFunctionTests

#Region "Get Start Point"
        <WorkItem(1980, "https://github.com/dotnet/roslyn/issues/1980")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPointConversionOperatorFunction()
            Dim code =
<Code>
class D
{
    public static implicit operator $$D(double d)
    {
        return new D();
    }
}
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=5, lineOffset:=1, absoluteOffset:=65, lineLength:=23)))
        End Sub

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPointExplicitlyImplementedMethod()
            Dim code =
<Code>
public interface I1
{
    int f1();
}

public class C1 : I1
{
    int I1.f1$$()
    {
        return 0;
    }
}
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=8, lineOffset:=5, absoluteOffset:=67, lineLength:=15)))
        End Sub
#End Region

#Region "Get End Point"
        <WorkItem(1980, "https://github.com/dotnet/roslyn/issues/1980")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetEndPointConversionOperatorFunction()
            Dim code =
<Code>
class D
{
    public static implicit operator $$D(double d)
    {
        return new D();
    }
}
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=6, lineOffset:=1, absoluteOffset:=89, lineLength:=5)))
        End Sub

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetEndPointExplicitlyImplementedMethod()
            Dim code =
<Code>
public interface I1
{
    int f1();
}

public class C1 : I1
{
    int I1.f1$$()
    {
        return 0;
    }
}
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=11, lineOffset:=6, absoluteOffset:=108, lineLength:=5)))
        End Sub
#End Region

#Region "Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access1()
            Dim code =
<Code>
class C
{
    int $$F() { throw new System.NotImplementedException(); }
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access2()
            Dim code =
<Code>
class C
{
    private int $$F() { throw new System.NotImplementedException(); }
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access3()
            Dim code =
<Code>
class C
{
    protected int $$F() { throw new System.NotImplementedException(); }
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access4()
            Dim code =
<Code>
class C
{
    protected internal int $$F() { throw new System.NotImplementedException(); }
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access5()
            Dim code =
<Code>
class C
{
    internal int $$F() { throw new System.NotImplementedException(); }
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access6()
            Dim code =
<Code>
class C
{
    public int $$F() { throw new System.NotImplementedException(); }
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access7()
            Dim code =
<Code>
interface I
{
    int $$Foo();
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

#End Region

#Region "Attribute Tests"
        <WorkItem(2356, "https://github.com/dotnet/roslyn/issues/2356")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub PropertyGetAttribute_WithNoSet()
            Dim code =
<Code>
public class Class1
{
    public int Property1
    {
        [Obsolete]
        $$get
        {
            return 0;
        }
    }
}
</Code>

            TestAttributes(code, IsElement("Obsolete"))
        End Sub

        <WorkItem(2356, "https://github.com/dotnet/roslyn/issues/2356")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub PropertySetAttribute_WithNoGet()
            Dim code =
<Code>
public class Class1
{
    public int Property1
    {
        [Obsolete]
        $$set
        {
        }
    }
}
</Code>

            TestAttributes(code, IsElement("Obsolete"))
        End Sub

        <WorkItem(2356, "https://github.com/dotnet/roslyn/issues/2356")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub PropertyGetAttribute_WithSet()
            Dim code =
<Code>
public class Class1
{
    public int Property1
    {
        [Obsolete]
        $$get
        {
            return 0;
        }

        [Obsolete]
        set
        {
        }
    }
}
</Code>

            TestAttributes(code, IsElement("Obsolete"))
        End Sub

        <WorkItem(2356, "https://github.com/dotnet/roslyn/issues/2356")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub PropertySetAttribute_WithGet()
            Dim code =
<Code>
public class Class1
{
    public int Property1
    {
        [Obsolete]
        get
        {
            return 0;
        }

        [Obsolete]
        $$set
        {
        }
    }
}
</Code>

            TestAttributes(code, IsElement("Obsolete"))
        End Sub

        <WorkItem(2356, "https://github.com/dotnet/roslyn/issues/2356")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Attribute_1()
            Dim code =
<Code>
class Class2
{
    [Obsolete]
    void $$F()
    {

    }
}
</Code>

            TestAttributes(code, IsElement("Obsolete"))
        End Sub
#End Region

#Region "CanOverride tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub CanOverride1()
            Dim code =
<Code>
abstract class C
{
    protected abstract void $$Foo();
}
</Code>

            TestCanOverride(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub CanOverride2()
            Dim code =
<Code>
interface I
{
    void $$Foo();
}
</Code>

            TestCanOverride(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub CanOverride3()
            Dim code =
<Code>
class C
{
    protected virtual void $$Foo() { }
}
</Code>

            TestCanOverride(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub CanOverride4()
            Dim code =
<Code>
class C
{
    protected void $$Foo() { }
}
</Code>

            TestCanOverride(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub CanOverride5()
            Dim code =
<Code>
class B
{
    protected virtual void Foo()
    {
    }
}

class C : B
{
    protected override void $$Foo()
    {
        base.Foo();
    }
}
</Code>

            TestCanOverride(code, False)
        End Sub

#End Region

#Region "FullName tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FullName_Destructor()
            Dim code =
<Code>
class C
{
    ~C$$() { }
}
</Code>

            TestFullName(code, "C.~C")
        End Sub

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FullName_ExplicitlyImplementedMethod()
            Dim code =
<Code>
public interface I1
{
    int f1();
}

public class C1 : I1
{
    int I1.f1$$()
    {
        return 0;
    }
}
</Code>

            TestFullName(code, "C1.I1.f1")
        End Sub

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FullName_ImplicitOperator()
            Dim code =
<Code>
public class ComplexType
{
    public ComplexType()
    {
    }
    public static implicit operator $$ComplexType(System.Int32 input) { return new ComplexType(); }


    public static ComplexType operator +(ComplexType input0, ComplexType input1)
    {
        return default(ComplexType);
    }
}
</Code>

            TestFullName(code, "ComplexType.implicit operator ComplexType")
        End Sub

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FullName_ExplicitOperator()
            Dim code =
<Code>
public class ComplexType
{
    public ComplexType()
    {
    }
    public static explicit operator $$ComplexType(System.Int32 input) { return new ComplexType(); }


    public static ComplexType operator +(ComplexType input0, ComplexType input1)
    {
        return default(ComplexType);
    }
}
</Code>

            TestFullName(code, "ComplexType.explicit operator ComplexType")
        End Sub

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FullName_OperatorOverload()
            Dim code =
<Code>
public class ComplexType
{
    public ComplexType()
    {
    }
    public static explicit operator ComplexType(System.Int32 input) { return new ComplexType(); }


    public static ComplexType operator $$+(ComplexType input0, ComplexType input1)
    {
        return default(ComplexType);
    }
}
</Code>

            TestFullName(code, "ComplexType.operator +")
        End Sub

#End Region

#Region "FunctionKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FunctionKind_Destructor()
            Dim code =
<Code>
class C
{
    ~C$$() { }
}
</Code>

            TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionDestructor)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FunctionKind_ExplicitInterfaceImplementation()
            Dim code =
<Code>
public interface I1
{
   void f1();
}

public class C1: I1
{
    void I1.f1$$()
    {
    }
}
</Code>

            TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionFunction)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FunctionKind_Operator()
            Dim code =
<Code>
public class C
{
    public static C operator $$+(C c1, C c2)
    {
    }
}
</Code>

            TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionOperator)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FunctionKind_ExplicitConversion()
            Dim code =
<Code>
public class C
{
    public static static $$explicit C(int x)
    {
    }
}
</Code>

            TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionOperator)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FunctionKind_ImplicitConversion()
            Dim code =
<Code>
public class C
{
    public static static $$implicit C(int x)
    {
    }
}
</Code>

            TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionOperator)
        End Sub

#End Region

#Region "MustImplement tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub MustImplement1()
            Dim code =
<Code>
abstract class C
{
    protected abstract void $$Foo();
}
</Code>

            TestMustImplement(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub MustImplement2()
            Dim code =
<Code>
interface I
{
    void $$Foo();
}
</Code>

            TestMustImplement(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub MustImplement3()
            Dim code =
<Code>
class C
{
    protected virtual void $$Foo() { }
}
</Code>

            TestMustImplement(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub MustImplement4()
            Dim code =
<Code>
class C
{
    protected void $$Foo() { }
}
</Code>

            TestMustImplement(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub MustImplement5()
            Dim code =
<Code>
class B
{
    protected virtual void Foo()
    {
    }
}

class C : B
{
    protected override void $$Foo()
    {
        base.Foo();
    }
}
</Code>

            TestMustImplement(code, False)
        End Sub

#End Region

#Region "Name tests"

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Name_ExplicitlyImplementedMethod()
            Dim code =
<Code>
public interface I1
{
    int f1();
}

public class C1 : I1
{
    int I1.f1$$()
    {
        return 0;
    }
}
</Code>

            TestName(code, "I1.f1")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Name_Destructor()
            Dim code =
<Code>
class C
{
    ~C$$() { }
}
</Code>

            TestName(code, "~C")
        End Sub

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Name_ImplicitOperator()
            Dim code =
<Code>
public class ComplexType
{
    public ComplexType()
    {
    }
    public static implicit operator $$ComplexType(System.Int32 input) { return new ComplexType(); }


    public static ComplexType operator +(ComplexType input0, ComplexType input1)
    {
        return default(ComplexType);
    }
}
</Code>

            TestName(code, "implicit operator ComplexType")
        End Sub

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Name_ExplicitOperator()
            Dim code =
<Code>
public class ComplexType
{
    public ComplexType()
    {
    }
    public static explicit operator $$ComplexType(System.Int32 input) { return new ComplexType(); }


    public static ComplexType operator +(ComplexType input0, ComplexType input1)
    {
        return default(ComplexType);
    }
}
</Code>

            TestName(code, "explicit operator ComplexType")
        End Sub

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Name_OperatorOverload()
            Dim code =
<Code>
public class ComplexType
{
    public ComplexType()
    {
    }
    public static implicit operator ComplexType(System.Int32 input) { return new ComplexType(); }


    public static ComplexType operator $$+(ComplexType input0, ComplexType input1)
    {
        return default(ComplexType);
    }
}
</Code>

            TestName(code, "operator +")
        End Sub

#End Region

#Region "OverrideKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub OverrideKind_Abstract()
            Dim code =
<Code>
abstract class C
{
    protected abstract void $$Foo();
}
</Code>

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub OverrideKind_Virtual()
            Dim code =
<Code>
class C
{
    protected virtual void $$Foo() { }
}
</Code>

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub OverrideKind_Sealed()
            Dim code =
<Code>
class C
{
    protected sealed void $$Foo() { }
}
</Code>

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindSealed)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub OverrideKind_Override()
            Dim code =
<Code>
abstract class B
{
    protected abstract void Foo();
}

class C : B
{
    protected override void $$Foo() { }
}
</Code>

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub OverrideKind_New()
            Dim code =
<Code>
abstract class B
{
    protected void Foo();
}

class C : B
{
    protected new void $$Foo() { }
}
</Code>

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNew)
        End Sub

#End Region

#Region "Prototype tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_FullNameOnly()
            Dim code =
<Code>
class A
{
    internal static bool $$MethodC(int intA, bool boolB)
    {
        return boolB;
    }
}
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeFullname, "A.MethodC")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_UniqueSignature()
            Dim code =
<Code>
class A
{
    internal static bool $$MethodC(int intA, bool boolB)
    {
        return boolB;
    }
}
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeUniqueSignature, "A.MethodC(int,bool)")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_ParamTypesOnly()
            Dim code =
<Code>
class A
{
    internal static bool $$MethodC(int intA, bool boolB)
    {
        return boolB;
    }
}
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeParamTypes, "MethodC (int, bool)")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_ParamNamesOnly()
            Dim code =
<Code>
class A
{
    internal static bool $$MethodC(int intA, bool boolB)
    {
        return boolB;
    }
}
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeParamNames, "MethodC (intA, boolB)")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_ReturnType()
            Dim code =
<Code>
class A
{
    internal static bool $$MethodC(int intA, bool boolB)
    {
        return boolB;
    }
}
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeType, "bool MethodC")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_ClassName1()
            Dim code =
<Code>
class A
{
    internal static bool $$MethodC(int intA, bool boolB)
    {
        return boolB;
    }
}
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "A.MethodC")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_ClassName2()
            Dim code =
<Code>
class A&lt;T&gt;
{
    internal static bool $$MethodC(int intA, bool boolB)
    {
        return boolB;
    }
}
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "A<>.MethodC")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_ClassName3()
            Dim code =
<Code>
class C&lt;T&gt;
{
    class A
    {
        internal static bool $$MethodC(int intA, bool boolB)
        {
            return boolB;
        }
    }
}
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "C<>.A.MethodC")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_ClassName4()
            Dim code =
<Code>
class C
{
    class A&lt;T&gt;
    {
        internal static bool $$MethodC(int intA, bool boolB)
        {
            return boolB;
        }
    }
}
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "C.A<>.MethodC")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_ClassName5()
            Dim code =
<Code>
class C
{
    class A
    {
        internal static bool $$MethodC(int intA, bool boolB)
        {
            return boolB;
        }
    }
}
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "C.A.MethodC")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_ClassName6()
            Dim code =
<Code>
namespace N
{
    class A
    {
        internal static bool $$MethodC(int intA, bool boolB)
        {
            return boolB;
        }
    }
}
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "A.MethodC")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_Constructor_Unique()
            Dim code =
<Code>
class A
{
    public $$A()
    {
    }
}
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeUniqueSignature, "A.#ctor()")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_Finalizer_Unique()
            Dim code =
<Code>
class A
{
    ~A$$()
    {
    }
}
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeUniqueSignature, "A.#dtor()")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_Unique_InvalidCombination()
            Dim code =
<Code>
class A
{
    internal static bool $$MethodC(int intA, bool boolB)
    {
        return boolB;
    }
}
</Code>

            TestPrototypeThrows(Of ArgumentException)(code, EnvDTE.vsCMPrototype.vsCMPrototypeUniqueSignature Or EnvDTE.vsCMPrototype.vsCMPrototypeClassName)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_Constructor_FullName()
            Dim code =
<Code>
class A
{
    public A$$()
    {
    }
}
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeFullname, "A.A")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_Finalizer_FullName()
            Dim code =
<Code>
class A
{
    ~A$$()
    {
    }
}
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeFullname, "A.~A")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_Operator_FullName()
            Dim code =
<Code>
class A
{
    public static A operator +$$(A a1, A a2)
    {
        return a1;
    }
}
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeFullname, "A.operator +")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_Constructor_ReturnType()
            Dim code =
<Code>
class A
{
    public A$$()
    {
    }
}
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeType, "void A")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_Finalizer_ReturnType()
            Dim code =
<Code>
class A
{
    ~A$$()
    {
    }
}
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeType, "void ~A")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_Operator_ReturnType()
            Dim code =
<Code>
class A
{
    public static A operator +$$(A a1, A a2)
    {
        return a1;
    }
}
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeType, "A operator +")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_Constructor_ClassName()
            Dim code =
<Code>
class A
{
    public A$$()
    {
    }
}
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "A.A")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_Finalizer_ClassName()
            Dim code =
<Code>
class A
{
    ~A$$()
    {
    }
}
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "A.~A")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_Operator_ClassName()
            Dim code =
<Code>
class A
{
    public static A operator +$$(A a1, A a2)
    {
        return a1;
    }
}
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "A.operator +")
        End Sub

#End Region

#Region "Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Type_Constructor()
            Dim code =
<Code>
class A
{
    public $$A()
    {
    }
}
</Code>

            TestTypeProp(code,
                New CodeTypeRefData With
                {
                    .AsFullName = "System.Void",
                    .AsString = "void",
                    .CodeTypeFullName = "System.Void",
                    .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefVoid
                })
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Type_Finalizer()
            Dim code =
<Code>
class A
{
    $$~A()
    {
    }
}
</Code>

            TestTypeProp(code,
                New CodeTypeRefData With
                {
                    .AsFullName = "System.Void",
                    .AsString = "void",
                    .CodeTypeFullName = "System.Void",
                    .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefVoid
                })
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Type_Operator()
            Dim code =
<Code>
class A
{
    public static A operator +$$(A a1, A a2)
    {
        return a1;
    }
}
</Code>

            TestTypeProp(code,
                New CodeTypeRefData With
                {
                    .AsFullName = "A",
                    .AsString = "A",
                    .CodeTypeFullName = "A",
                    .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType
                })
        End Sub

#End Region

#Region "RemoveParameter tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveParameter1()
            Dim code =
<Code>
class C
{
    void $$M(int a) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M() { }
}
</Code>

            TestRemoveChild(code, expected, "a")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveParameter2()
            Dim code =
<Code>
class C
{
    void $$M(int a, string b) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(int a) { }
}
</Code>

            TestRemoveChild(code, expected, "b")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveParameter3()
            Dim code =
<Code>
class C
{
    void $$M(int a, string b) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(string b) { }
}
</Code>

            TestRemoveChild(code, expected, "a")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveParameter4()
            Dim code =
<Code>
class C
{
    void $$M(int a, string b, int c) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(int a, int c) { }
}
</Code>

            TestRemoveChild(code, expected, "b")
        End Sub

#End Region

#Region "AddParameter tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddParameter1()
            Dim code =
<Code>
class C
{
    void $$M() { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(int a) { }
}
</Code>

            TestAddParameter(code, expected, New ParameterData With {.Name = "a", .Type = "int"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddParameter2()
            Dim code =
<Code>
class C
{
    void $$M(int a) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(string b, int a) { }
}
</Code>

            TestAddParameter(code, expected, New ParameterData With {.Name = "b", .Type = "string"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddParameter3()
            Dim code =
<Code>
class C
{
    void $$M(int a, string b) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(int a, bool c, string b) { }
}
</Code>

            TestAddParameter(code, expected, New ParameterData With {.Name = "c", .Type = "System.Boolean", .Position = 1})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddParameter4()
            Dim code =
<Code>
class C
{
    void $$M(int a) { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void M(int a, string b) { }
}
</Code>

            TestAddParameter(code, expected, New ParameterData With {.Name = "b", .Type = "string", .Position = -1})
        End Sub
#End Region

#Region "Set Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess1()
            Dim code =
<Code>
class C
{
    int $$Foo()
    {
        throw new System.NotImplementedException();
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    public int Foo()
    {
        throw new System.NotImplementedException();
    }
}
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess2()
            Dim code =
<Code>
class C
{
    public int $$Foo()
    {
        throw new System.NotImplementedException();
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    internal int Foo()
    {
        throw new System.NotImplementedException();
    }
}
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess3()
            Dim code =
<Code>
class C
{
    protected internal int $$Foo()
    {
        throw new System.NotImplementedException();
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    public int Foo()
    {
        throw new System.NotImplementedException();
    }
}
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess4()
            Dim code =
<Code>
class C
{
    public int $$Foo()
    {
        throw new System.NotImplementedException();
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    protected internal int Foo()
    {
        throw new System.NotImplementedException();
    }
}
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess5()
            Dim code =
<Code>
class C
{
    public int $$Foo()
    {
        throw new System.NotImplementedException();
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    int Foo()
    {
        throw new System.NotImplementedException();
    }
}
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess6()
            Dim code =
<Code>
interface I
{
    int $$Foo();
}
</Code>

            Dim expected =
<Code>
interface I
{
    int Foo();
}
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProtected, ThrowsArgumentException(Of EnvDTE.vsCMAccess)())
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess7()
            Dim code =
<Code>
interface I
{
    int $$Foo();
}
</Code>

            Dim expected =
<Code>
interface I
{
    int Foo();
}
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

#End Region

#Region "Set IsShared tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared1()
            Dim code =
<Code>
class C
{
    void $$Foo()
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    static void Foo()
    {
    }
}
</Code>

            TestSetIsShared(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared2()
            Dim code =
<Code>
class C
{
    static void $$Foo()
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    void Foo()
    {
    }
}
</Code>

            TestSetIsShared(code, expected, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared3()
            Dim code =
<Code>
class C
{
    $$C()
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    static C()
    {
    }
}
</Code>

            TestSetIsShared(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared4()
            Dim code =
<Code>
class C
{
    static $$C()
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    C()
    {
    }
}
</Code>

            TestSetIsShared(code, expected, False)
        End Sub

#End Region

#Region "Set CanOverride tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetCanOverride1()
            Dim code =
<Code>
class C
{
    void $$Foo()
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    virtual void Foo()
    {
    }
}
</Code>

            TestSetCanOverride(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetCanOverride2()
            Dim code =
<Code>
class C
{
    virtual void $$Foo()
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    virtual void Foo()
    {
    }
}
</Code>

            TestSetCanOverride(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetCanOverride3()
            Dim code =
<Code>
class C
{
    void $$Foo()
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    void Foo()
    {
    }
}
</Code>

            TestSetCanOverride(code, expected, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetCanOverride4()
            Dim code =
<Code>
class C
{
    virtual void $$Foo()
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    void Foo()
    {
    }
}
</Code>

            TestSetCanOverride(code, expected, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetCanOverride5()
            Dim code =
<Code>
interface I
{
    void $$Foo();
}
</Code>

            Dim expected =
<Code>
interface I
{
    void Foo();
}
</Code>

            TestSetCanOverride(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetCanOverride6()
            Dim code =
<Code>
interface I
{
    void $$Foo();
}
</Code>

            Dim expected =
<Code>
interface I
{
    void Foo();
}
</Code>

            TestSetCanOverride(code, expected, False, ThrowsArgumentException(Of Boolean))
        End Sub

#End Region

#Region "Set MustImplement tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetMustImplement1()
            Dim code =
<Code>
abstract class C
{
    abstract void $$Foo()
    {
    }
}
</Code>

            Dim expected =
<Code>
abstract class C
{
    abstract void Foo();
}
</Code>

            TestSetMustImplement(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetMustImplement2()
            Dim code =
<Code>
abstract class C
{
    void $$Foo()
    {
        int i = 0;
    }
}
</Code>

            Dim expected =
<Code>
abstract class C
{
    abstract void Foo()
    {
        int i = 0;
    }
}
</Code>

            TestSetMustImplement(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetMustImplement3()
            Dim code =
<Code>
abstract class C
{
    abstract void $$Foo();
}
</Code>

            Dim expected =
<Code>
abstract class C
{
    void Foo()
    {

    }
}
</Code>

            TestSetMustImplement(code, expected, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetMustImplement4()
            Dim code =
<Code>
abstract class C
{
    abstract void $$Foo();
}
</Code>

            Dim expected =
<Code>
abstract class C
{
    abstract void Foo();
}
</Code>

            TestSetMustImplement(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetMustImplement5()
            Dim code =
<Code>
abstract class C
{
    void $$Foo()
    {
    }
}
</Code>

            Dim expected =
<Code>
abstract class C
{
    void Foo()
    {
    }
}
</Code>

            TestSetMustImplement(code, expected, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetMustImplement6()
            Dim code =
<Code>
class C
{
    void $$Foo()
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    void Foo()
    {
    }
}
</Code>

            TestSetMustImplement(code, expected, True, ThrowsArgumentException(Of Boolean))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetMustImplement7()
            Dim code =
<Code>
class C
{
    void $$Foo()
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    void Foo()
    {
    }
}
</Code>

            TestSetMustImplement(code, expected, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetMustImplement8()
            Dim code =
<Code>
interface I
{
    void $$Foo()
    {
    }
}
</Code>

            Dim expected =
<Code>
interface I
{
    void Foo()
    {
    }
}
</Code>

            TestSetMustImplement(code, expected, False, ThrowsArgumentException(Of Boolean))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetMustImplement9()
            Dim code =
<Code>
interface I
{
    void $$Foo()
    {
    }
}
</Code>

            Dim expected =
<Code>
interface I
{
    void Foo()
    {
    }
}
</Code>

            TestSetMustImplement(code, expected, True)
        End Sub

#End Region

#Region "Set OverrideKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetOverrideKind1()
            Dim code =
<Code>
class C
{
    void $$Foo()
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    virtual void Foo()
    {
    }
}
</Code>

            TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetOverrideKind2()
            Dim code =
<Code>
class C
{
    void $$Foo()
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    sealed void Foo()
    {
    }
}
</Code>

            TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindSealed)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetOverrideKind3()
            Dim code =
<Code>
abstract class C
{
    void $$Foo()
    {
    }
}
</Code>

            Dim expected =
<Code>
abstract class C
{
    abstract void Foo();
}
</Code>

            TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract)
        End Sub

#End Region

#Region "Set Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetName1()
            Dim code =
<Code>
class C
{
    void $$Foo()
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    void Bar()
    {
    }
}
</Code>

            TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Sub

#End Region

#Region "Set Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType1()
            Dim code =
<Code>
class C
{
    void $$Foo()
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    int Foo()
    {
    }
}
</Code>

            TestSetTypeProp(code, expected, "System.Int32")
        End Sub

#End Region

#Region "ExtensionMethodExtender"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ExtensionMethodExtender_IsExtension1()
            Dim code =
<Code>
public static class C
{
    public static void $$Foo(this C c)
    {
    }
}
</Code>

            TestExtensionMethodExtender_IsExtension(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ExtensionMethodExtender_IsExtension2()
            Dim code =
<Code>
public static class C
{
    public static void $$Foo(C c)
    {
    }
}
</Code>

            TestExtensionMethodExtender_IsExtension(code, False)
        End Sub

#End Region

#Region "PartialMethodExtender"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub PartialMethodExtender_IsPartial1()
            Dim code =
<Code>
public partial class C
{
    partial void $$M();
 
    partial void M()
    {
    }

    void M(int i)
    {
    }
}
</Code>

            TestPartialMethodExtender_IsPartial(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub PartialMethodExtender_IsPartial2()
            Dim code =
<Code>
public partial class C
{
    partial void M();
 
    partial void $$M()
    {
    }

    void M(int i)
    {
    }
}
</Code>

            TestPartialMethodExtender_IsPartial(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub PartialMethodExtender_IsPartial3()
            Dim code =
<Code>
public partial class C
{
    partial void M();
 
    partial void M()
    {
    }

    void $$M(int i)
    {
    }
}
</Code>

            TestPartialMethodExtender_IsPartial(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub PartialMethodExtender_IsDeclaration1()
            Dim code =
<Code>
public partial class C
{
    partial void $$M();
 
    partial void M()
    {
    }

    void M(int i)
    {
    }
}
</Code>

            TestPartialMethodExtender_IsDeclaration(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub PartialMethodExtender_IsDeclaration2()
            Dim code =
<Code>
public partial class C
{
    partial void M();
 
    partial void $$M()
    {
    }

    void M(int i)
    {
    }
}
</Code>

            TestPartialMethodExtender_IsDeclaration(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub PartialMethodExtender_IsDeclaration3()
            Dim code =
<Code>
public partial class C
{
    partial void M();
 
    partial void M()
    {
    }

    void $$M(int i)
    {
    }
}
</Code>

            TestPartialMethodExtender_IsDeclaration(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub PartialMethodExtender_HasOtherPart1()
            Dim code =
<Code>
public partial class C
{
    partial void $$M();
 
    partial void M()
    {
    }

    void M(int i)
    {
    }
}
</Code>

            TestPartialMethodExtender_HasOtherPart(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub PartialMethodExtender_HasOtherPart2()
            Dim code =
<Code>
public partial class C
{
    partial void M();
 
    partial void $$M()
    {
    }

    void M(int i)
    {
    }
}
</Code>

            TestPartialMethodExtender_HasOtherPart(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub PartialMethodExtender_HasOtherPart3()
            Dim code =
<Code>
public partial class C
{
    partial void M();
 
    partial void M()
    {
    }

    void $$M(int i)
    {
    }
}
</Code>

            TestPartialMethodExtender_HasOtherPart(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub PartialMethodExtender_HasOtherPart4()
            Dim code =
<Code>
public partial class C
{
    partial void $$M();
}
</Code>

            TestPartialMethodExtender_HasOtherPart(code, False)
        End Sub

#End Region

#Region "Overloads Tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverloads1()
            Dim code =
<Code>
public static class C
{
    public static void $$Foo()
    {
    }

    public static void Foo(C c)
    {
    }
}
</Code>
            TestOverloadsUniqueSignatures(code, "C.Foo()", "C.Foo(C)")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverloads2()
            Dim code =
<Code>
public static class C
{
    public static void $$Foo()
    {
    }
}
</Code>
            TestOverloadsUniqueSignatures(code, "C.Foo()")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverloads3()
            Dim code =
<Code>
class A
{
    public static A operator +$$(A a1, A a2)
    {
        return a1;
    }
}
</Code>
            TestOverloadsUniqueSignatures(code, "A.#op_Plus(A,A)")
        End Sub

#End Region

#Region "Parameter name tests"

        <WorkItem(1147885)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParameterNameWithEscapeCharacters()
            Dim code =
<Code>
public class C
{
    public void $$Foo(int @int)
    {
    }
}
</Code>
            TestAllParameterNames(code, "@int")
        End Sub

        <WorkItem(1147885)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParameterNameWithEscapeCharacters_2()
            Dim code =
<Code>
public class C
{
    public void $$Foo(int @int, string @string)
    {
    }
}
</Code>
            TestAllParameterNames(code, "@int", "@string")
        End Sub

#End Region

#Region "AddAttribute tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute1()
            Dim code =
<Code>
using System;

class C
{
    void $$M() { }
}
</Code>

            Dim expected =
<Code>
using System;

class C
{
    [Serializable()]
    void M() { }
}
</Code>
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute2()
            Dim code =
<Code>
using System;

class C
{
    [Serializable]
    void $$M() { }
}
</Code>

            Dim expected =
<Code>
using System;

class C
{
    [Serializable]
    [CLSCompliant(true)]
    void M() { }
}
</Code>
            TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true", .Position = 1})
        End Sub

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_BelowDocComment()
            Dim code =
<Code>
using System;

class C
{
    /// &lt;summary&gt;&lt;/summary&gt;
    void $$M() { }
}
</Code>

            Dim expected =
<Code>
using System;

class C
{
    /// &lt;summary&gt;&lt;/summary&gt;
    [CLSCompliant(true)]
    void M() { }
}
</Code>
            TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true"})
        End Sub

#End Region

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TypeDescriptor_GetProperties()
            Dim code =
<Code>
class C
{
    void $$M() { }
}
</Code>

            Dim expectedPropertyNames =
                {"DTE", "Collection", "Name", "FullName", "ProjectItem", "Kind", "IsCodeType",
                 "InfoLocation", "Children", "Language", "StartPoint", "EndPoint", "ExtenderNames",
                 "ExtenderCATID", "Parent", "FunctionKind", "Type", "Parameters", "Access", "IsOverloaded",
                 "IsShared", "MustImplement", "Overloads", "Attributes", "DocComment", "Comment",
                 "CanOverride", "OverrideKind", "IsGeneric"}

            TestPropertyDescriptors(code, expectedPropertyNames)
        End Sub

        Private Function GetExtensionMethodExtender(codeElement As EnvDTE80.CodeFunction2) As ICSExtensionMethodExtender
            Return CType(codeElement.Extender(ExtenderNames.ExtensionMethod), ICSExtensionMethodExtender)
        End Function

        Private Function GetPartialMethodExtender(codeElement As EnvDTE80.CodeFunction2) As ICSPartialMethodExtender
            Return CType(codeElement.Extender(ExtenderNames.PartialMethod), ICSPartialMethodExtender)
        End Function

        Protected Overrides Function ExtensionMethodExtender_GetIsExtension(codeElement As EnvDTE80.CodeFunction2) As Boolean
            Return GetExtensionMethodExtender(codeElement).IsExtension
        End Function

        Protected Overrides Function PartialMethodExtender_GetIsPartial(codeElement As EnvDTE80.CodeFunction2) As Boolean
            Return GetPartialMethodExtender(codeElement).IsPartial
        End Function

        Protected Overrides Function PartialMethodExtender_GetIsDeclaration(codeElement As EnvDTE80.CodeFunction2) As Boolean
            Return GetPartialMethodExtender(codeElement).IsDeclaration
        End Function

        Protected Overrides Function PartialMethodExtender_GetHasOtherPart(codeElement As EnvDTE80.CodeFunction2) As Boolean
            Return GetPartialMethodExtender(codeElement).HasOtherPart
        End Function

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property
    End Class
End Namespace

