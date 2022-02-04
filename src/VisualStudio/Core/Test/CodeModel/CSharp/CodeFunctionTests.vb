' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel.Extenders
Imports Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodeFunctionTests
        Inherits AbstractCodeFunctionTests

#Region "Get Start Point"
        <WorkItem(1980, "https://github.com/dotnet/roslyn/issues/1980")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPointConversionOperatorFunction()
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPointExplicitlyImplementedMethod()
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPointConversionOperatorFunction()
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPointExplicitlyImplementedMethod()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess1()
            Dim code =
<Code>
class C
{
    int $$F() { throw new System.NotImplementedException(); }
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess2()
            Dim code =
<Code>
class C
{
    private int $$F() { throw new System.NotImplementedException(); }
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess3()
            Dim code =
<Code>
class C
{
    protected int $$F() { throw new System.NotImplementedException(); }
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProtected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess4()
            Dim code =
<Code>
class C
{
    protected internal int $$F() { throw new System.NotImplementedException(); }
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess5()
            Dim code =
<Code>
class C
{
    internal int $$F() { throw new System.NotImplementedException(); }
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess6()
            Dim code =
<Code>
class C
{
    public int $$F() { throw new System.NotImplementedException(); }
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess7()
            Dim code =
<Code>
interface I
{
    int $$Goo();
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

#End Region

#Region "Attribute Tests"
        <WorkItem(2356, "https://github.com/dotnet/roslyn/issues/2356")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPropertyGetAttribute_WithNoSet()
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPropertySetAttribute_WithNoGet()
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPropertyGetAttribute_WithSet()
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPropertySetAttribute_WithGet()
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAttribute_1()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestCanOverride1()
            Dim code =
<Code>
abstract class C
{
    protected abstract void $$Goo();
}
</Code>

            TestCanOverride(code, True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestCanOverride2()
            Dim code =
<Code>
interface I
{
    void $$Goo();
}
</Code>

            TestCanOverride(code, True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestCanOverride3()
            Dim code =
<Code>
class C
{
    protected virtual void $$Goo() { }
}
</Code>

            TestCanOverride(code, True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestCanOverride4()
            Dim code =
<Code>
class C
{
    protected void $$Goo() { }
}
</Code>

            TestCanOverride(code, False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestCanOverride5()
            Dim code =
<Code>
class B
{
    protected virtual void Goo()
    {
    }
}

class C : B
{
    protected override void $$Goo()
    {
        base.Goo();
    }
}
</Code>

            TestCanOverride(code, False)
        End Sub

#End Region

#Region "FullName tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName_Destructor()
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName_ExplicitlyImplementedMethod()
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName_ImplicitOperator()
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName_ExplicitOperator()
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName_OperatorOverload()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFunctionKind_Destructor()
            Dim code =
<Code>
class C
{
    ~C$$() { }
}
</Code>

            TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionDestructor)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFunctionKind_ExplicitInterfaceImplementation()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFunctionKind_Operator()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFunctionKind_ExplicitConversion()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFunctionKind_ImplicitConversion()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestMustImplement1()
            Dim code =
<Code>
abstract class C
{
    protected abstract void $$Goo();
}
</Code>

            TestMustImplement(code, True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestMustImplement2()
            Dim code =
<Code>
interface I
{
    void $$Goo();
}
</Code>

            TestMustImplement(code, True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestMustImplement3()
            Dim code =
<Code>
class C
{
    protected virtual void $$Goo() { }
}
</Code>

            TestMustImplement(code, False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestMustImplement4()
            Dim code =
<Code>
class C
{
    protected void $$Goo() { }
}
</Code>

            TestMustImplement(code, False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestMustImplement5()
            Dim code =
<Code>
class B
{
    protected virtual void Goo()
    {
    }
}

class C : B
{
    protected override void $$Goo()
    {
        base.Goo();
    }
}
</Code>

            TestMustImplement(code, False)
        End Sub

#End Region

#Region "Name tests"

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName_ExplicitlyImplementedMethod()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName_Destructor()
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName_ImplicitOperator()
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName_ExplicitOperator()
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName_OperatorOverload()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_Abstract()
            Dim code =
<Code>
abstract class C
{
    protected abstract void $$Goo();
}
</Code>

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_Virtual()
            Dim code =
<Code>
class C
{
    protected virtual void $$Goo() { }
}
</Code>

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_Sealed()
            Dim code =
<Code>
class C
{
    protected sealed void $$Goo() { }
}
</Code>

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindSealed)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_Override()
            Dim code =
<Code>
abstract class B
{
    protected abstract void Goo();
}

class C : B
{
    protected override void $$Goo() { }
}
</Code>

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_New()
            Dim code =
<Code>
abstract class B
{
    protected void Goo();
}

class C : B
{
    protected new void $$Goo() { }
}
</Code>

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNew)
        End Sub

#End Region

#Region "Prototype tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_FullNameOnly()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_UniqueSignature()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_ParamTypesOnly()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_ParamNamesOnly()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_ReturnType()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_ClassName1()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_ClassName2()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_ClassName3()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_ClassName4()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_ClassName5()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_ClassName6()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_Constructor_Unique()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_Finalizer_Unique()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_Unique_InvalidCombination()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_Constructor_FullName()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_Finalizer_FullName()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_Operator_FullName()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_Constructor_ReturnType()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_Finalizer_ReturnType()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_Operator_ReturnType()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_Constructor_ClassName()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_Finalizer_ClassName()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_Operator_ClassName()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestType_Constructor()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestType_Finalizer()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestType_Operator()
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveParameter1() As Task
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

            Await TestRemoveChild(code, expected, "a")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveParameter2() As Task
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

            Await TestRemoveChild(code, expected, "b")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveParameter3() As Task
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

            Await TestRemoveChild(code, expected, "a")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveParameter4() As Task
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

            Await TestRemoveChild(code, expected, "b")
        End Function

#End Region

#Region "AddParameter tests"
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddParameter1() As Task
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

            Await TestAddParameter(code, expected, New ParameterData With {.Name = "a", .Type = "int"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddParameter2() As Task
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

            Await TestAddParameter(code, expected, New ParameterData With {.Name = "b", .Type = "string"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddParameter3() As Task
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

            Await TestAddParameter(code, expected, New ParameterData With {.Name = "c", .Type = "System.Boolean", .Position = 1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddParameter4() As Task
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

            Await TestAddParameter(code, expected, New ParameterData With {.Name = "b", .Type = "string", .Position = -1})
        End Function
#End Region

#Region "Set Access tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess1() As Task
            Dim code =
<Code>
class C
{
    int $$Goo()
    {
        throw new System.NotImplementedException();
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    public int Goo()
    {
        throw new System.NotImplementedException();
    }
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess2() As Task
            Dim code =
<Code>
class C
{
    public int $$Goo()
    {
        throw new System.NotImplementedException();
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    internal int Goo()
    {
        throw new System.NotImplementedException();
    }
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess3() As Task
            Dim code =
<Code>
class C
{
    protected internal int $$Goo()
    {
        throw new System.NotImplementedException();
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    public int Goo()
    {
        throw new System.NotImplementedException();
    }
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess4() As Task
            Dim code =
<Code>
class C
{
    public int $$Goo()
    {
        throw new System.NotImplementedException();
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    protected internal int Goo()
    {
        throw new System.NotImplementedException();
    }
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess5() As Task
            Dim code =
<Code>
class C
{
    public int $$Goo()
    {
        throw new System.NotImplementedException();
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    int Goo()
    {
        throw new System.NotImplementedException();
    }
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess6() As Task
            Dim code =
<Code>
interface I
{
    int $$Goo();
}
</Code>

            Dim expected =
<Code>
interface I
{
    int Goo();
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProtected, ThrowsArgumentException(Of EnvDTE.vsCMAccess)())
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess7() As Task
            Dim code =
<Code>
interface I
{
    int $$Goo();
}
</Code>

            Dim expected =
<Code>
interface I
{
    int Goo();
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

#End Region

#Region "Set IsShared tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared1() As Task
            Dim code =
<Code>
class C
{
    void $$Goo()
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    static void Goo()
    {
    }
}
</Code>

            Await TestSetIsShared(code, expected, True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared2() As Task
            Dim code =
<Code>
class C
{
    static void $$Goo()
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    void Goo()
    {
    }
}
</Code>

            Await TestSetIsShared(code, expected, False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared3() As Task
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

            Await TestSetIsShared(code, expected, True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared4() As Task
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

            Await TestSetIsShared(code, expected, False)
        End Function

#End Region

#Region "Set CanOverride tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetCanOverride1() As Task
            Dim code =
<Code>
class C
{
    void $$Goo()
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    virtual void Goo()
    {
    }
}
</Code>

            Await TestSetCanOverride(code, expected, True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetCanOverride2() As Task
            Dim code =
<Code>
class C
{
    virtual void $$Goo()
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    virtual void Goo()
    {
    }
}
</Code>

            Await TestSetCanOverride(code, expected, True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetCanOverride3() As Task
            Dim code =
<Code>
class C
{
    void $$Goo()
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    void Goo()
    {
    }
}
</Code>

            Await TestSetCanOverride(code, expected, False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetCanOverride4() As Task
            Dim code =
<Code>
class C
{
    virtual void $$Goo()
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    void Goo()
    {
    }
}
</Code>

            Await TestSetCanOverride(code, expected, False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetCanOverride5() As Task
            Dim code =
<Code>
interface I
{
    void $$Goo();
}
</Code>

            Dim expected =
<Code>
interface I
{
    void Goo();
}
</Code>

            Await TestSetCanOverride(code, expected, True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetCanOverride6() As Task
            Dim code =
<Code>
interface I
{
    void $$Goo();
}
</Code>

            Dim expected =
<Code>
interface I
{
    void Goo();
}
</Code>

            Await TestSetCanOverride(code, expected, False, ThrowsArgumentException(Of Boolean))
        End Function

#End Region

#Region "Set MustImplement tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetMustImplement1() As Task
            Dim code =
<Code>
abstract class C
{
    abstract void $$Goo()
    {
    }
}
</Code>

            Dim expected =
<Code>
abstract class C
{
    abstract void Goo();
}
</Code>

            Await TestSetMustImplement(code, expected, True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetMustImplement2() As Task
            Dim code =
<Code>
abstract class C
{
    void $$Goo()
    {
        int i = 0;
    }
}
</Code>

            Dim expected =
<Code>
abstract class C
{
    abstract void Goo()
    {
        int i = 0;
    }
}
</Code>

            Await TestSetMustImplement(code, expected, True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetMustImplement3() As Task
            Dim code =
<Code>
abstract class C
{
    abstract void $$Goo();
}
</Code>

            Dim expected =
<Code>
abstract class C
{
    void Goo()
    {

    }
}
</Code>

            Await TestSetMustImplement(code, expected, False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetMustImplement4() As Task
            Dim code =
<Code>
abstract class C
{
    abstract void $$Goo();
}
</Code>

            Dim expected =
<Code>
abstract class C
{
    abstract void Goo();
}
</Code>

            Await TestSetMustImplement(code, expected, True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetMustImplement5() As Task
            Dim code =
<Code>
abstract class C
{
    void $$Goo()
    {
    }
}
</Code>

            Dim expected =
<Code>
abstract class C
{
    void Goo()
    {
    }
}
</Code>

            Await TestSetMustImplement(code, expected, False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetMustImplement6() As Task
            Dim code =
<Code>
class C
{
    void $$Goo()
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    void Goo()
    {
    }
}
</Code>

            Await TestSetMustImplement(code, expected, True, ThrowsArgumentException(Of Boolean))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetMustImplement7() As Task
            Dim code =
<Code>
class C
{
    void $$Goo()
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    void Goo()
    {
    }
}
</Code>

            Await TestSetMustImplement(code, expected, False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetMustImplement8() As Task
            Dim code =
<Code>
interface I
{
    void $$Goo()
    {
    }
}
</Code>

            Dim expected =
<Code>
interface I
{
    void Goo()
    {
    }
}
</Code>

            Await TestSetMustImplement(code, expected, False, ThrowsArgumentException(Of Boolean))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetMustImplement9() As Task
            Dim code =
<Code>
interface I
{
    void $$Goo()
    {
    }
}
</Code>

            Dim expected =
<Code>
interface I
{
    void Goo()
    {
    }
}
</Code>

            Await TestSetMustImplement(code, expected, True)
        End Function

#End Region

#Region "Set OverrideKind tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetOverrideKind1() As Task
            Dim code =
<Code>
class C
{
    void $$Goo()
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    virtual void Goo()
    {
    }
}
</Code>

            Await TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetOverrideKind2() As Task
            Dim code =
<Code>
class C
{
    void $$Goo()
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    sealed void Goo()
    {
    }
}
</Code>

            Await TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindSealed)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetOverrideKind3() As Task
            Dim code =
<Code>
abstract class C
{
    void $$Goo()
    {
    }
}
</Code>

            Dim expected =
<Code>
abstract class C
{
    abstract void Goo();
}
</Code>

            Await TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract)
        End Function

#End Region

#Region "Set Name tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName1() As Task
            Dim code =
<Code>
class C
{
    void $$Goo()
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

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function

#End Region

#Region "Set Type tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType1() As Task
            Dim code =
<Code>
class C
{
    void $$Goo()
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    int Goo()
    {
    }
}
</Code>

            Await TestSetTypeProp(code, expected, "System.Int32")
        End Function

#End Region

#Region "ExtensionMethodExtender"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestExtensionMethodExtender_IsExtension1()
            Dim code =
<Code>
public static class C
{
    public static void $$Goo(this C c)
    {
    }
}
</Code>

            TestExtensionMethodExtender_IsExtension(code, True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestExtensionMethodExtender_IsExtension2()
            Dim code =
<Code>
public static class C
{
    public static void $$Goo(C c)
    {
    }
}
</Code>

            TestExtensionMethodExtender_IsExtension(code, False)
        End Sub

#End Region

#Region "PartialMethodExtender"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPartialMethodExtender_IsPartial1()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPartialMethodExtender_IsPartial2()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPartialMethodExtender_IsPartial3()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPartialMethodExtender_IsDeclaration1()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPartialMethodExtender_IsDeclaration2()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPartialMethodExtender_IsDeclaration3()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPartialMethodExtender_HasOtherPart1()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPartialMethodExtender_HasOtherPart2()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPartialMethodExtender_HasOtherPart3()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPartialMethodExtender_HasOtherPart4()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverloads1()
            Dim code =
<Code>
public static class C
{
    public static void $$Goo()
    {
    }

    public static void Goo(C c)
    {
    }
}
</Code>
            TestOverloadsUniqueSignatures(code, "C.Goo()", "C.Goo(C)")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverloads2()
            Dim code =
<Code>
public static class C
{
    public static void $$Goo()
    {
    }
}
</Code>
            TestOverloadsUniqueSignatures(code, "C.Goo()")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WorkItem(1147885, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1147885")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParameterNameWithEscapeCharacters()
            Dim code =
<Code>
public class C
{
    public void $$Goo(int @int)
    {
    }
}
</Code>
            TestAllParameterNames(code, "@int")
        End Sub

        <WorkItem(1147885, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1147885")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParameterNameWithEscapeCharacters_2()
            Dim code =
<Code>
public class C
{
    public void $$Goo(int @int, string @string)
    {
    }
}
</Code>
            TestAllParameterNames(code, "@int", "@string")
        End Sub

#End Region

#Region "AddAttribute tests"
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute1() As Task
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
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute2() As Task
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
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true", .Position = 1})
        End Function

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_BelowDocComment() As Task
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
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true"})
        End Function

#End Region

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestTypeDescriptor_GetProperties()
            Dim code =
<Code>
class C
{
    void $$M() { }
}
</Code>

            TestPropertyDescriptors(Of EnvDTE80.CodeFunction2)(code)
        End Sub

        Private Shared Function GetExtensionMethodExtender(codeElement As EnvDTE80.CodeFunction2) As ICSExtensionMethodExtender
            Return CType(codeElement.Extender(ExtenderNames.ExtensionMethod), ICSExtensionMethodExtender)
        End Function

        Private Shared Function GetPartialMethodExtender(codeElement As EnvDTE80.CodeFunction2) As ICSPartialMethodExtender
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

