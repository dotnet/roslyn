' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
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
        Public Async Function TestGetStartPointConversionOperatorFunction() As Task
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

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=5, lineOffset:=1, absoluteOffset:=65, lineLength:=23)))
        End Function

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPointExplicitlyImplementedMethod() As Task
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

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=8, lineOffset:=5, absoluteOffset:=67, lineLength:=15)))
        End Function
#End Region

#Region "Get End Point"
        <WorkItem(1980, "https://github.com/dotnet/roslyn/issues/1980")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPointConversionOperatorFunction() As Task
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

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=6, lineOffset:=1, absoluteOffset:=89, lineLength:=5)))
        End Function

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPointExplicitlyImplementedMethod() As Task
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

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=11, lineOffset:=6, absoluteOffset:=108, lineLength:=5)))
        End Function
#End Region

#Region "Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess1() As Task
            Dim code =
<Code>
class C
{
    int $$F() { throw new System.NotImplementedException(); }
}
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess2() As Task
            Dim code =
<Code>
class C
{
    private int $$F() { throw new System.NotImplementedException(); }
}
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess3() As Task
            Dim code =
<Code>
class C
{
    protected int $$F() { throw new System.NotImplementedException(); }
}
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess4() As Task
            Dim code =
<Code>
class C
{
    protected internal int $$F() { throw new System.NotImplementedException(); }
}
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess5() As Task
            Dim code =
<Code>
class C
{
    internal int $$F() { throw new System.NotImplementedException(); }
}
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess6() As Task
            Dim code =
<Code>
class C
{
    public int $$F() { throw new System.NotImplementedException(); }
}
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess7() As Task
            Dim code =
<Code>
interface I
{
    int $$Foo();
}
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

#End Region

#Region "Attribute Tests"
        <WorkItem(2356, "https://github.com/dotnet/roslyn/issues/2356")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPropertyGetAttribute_WithNoSet() As Task
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

            Await TestAttributes(code, IsElement("Obsolete"))
        End Function

        <WorkItem(2356, "https://github.com/dotnet/roslyn/issues/2356")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPropertySetAttribute_WithNoGet() As Task
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

            Await TestAttributes(code, IsElement("Obsolete"))
        End Function

        <WorkItem(2356, "https://github.com/dotnet/roslyn/issues/2356")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPropertyGetAttribute_WithSet() As Task
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

            Await TestAttributes(code, IsElement("Obsolete"))
        End Function

        <WorkItem(2356, "https://github.com/dotnet/roslyn/issues/2356")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPropertySetAttribute_WithGet() As Task
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

            Await TestAttributes(code, IsElement("Obsolete"))
        End Function

        <WorkItem(2356, "https://github.com/dotnet/roslyn/issues/2356")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAttribute_1() As Task
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

            Await TestAttributes(code, IsElement("Obsolete"))
        End Function
#End Region

#Region "CanOverride tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestCanOverride1() As Task
            Dim code =
<Code>
abstract class C
{
    protected abstract void $$Foo();
}
</Code>

            Await TestCanOverride(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestCanOverride2() As Task
            Dim code =
<Code>
interface I
{
    void $$Foo();
}
</Code>

            Await TestCanOverride(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestCanOverride3() As Task
            Dim code =
<Code>
class C
{
    protected virtual void $$Foo() { }
}
</Code>

            Await TestCanOverride(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestCanOverride4() As Task
            Dim code =
<Code>
class C
{
    protected void $$Foo() { }
}
</Code>

            Await TestCanOverride(code, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestCanOverride5() As Task
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

            Await TestCanOverride(code, False)
        End Function

#End Region

#Region "FullName tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFullName_Destructor() As Task
            Dim code =
<Code>
class C
{
    ~C$$() { }
}
</Code>

            Await TestFullName(code, "C.~C")
        End Function

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFullName_ExplicitlyImplementedMethod() As Task
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

            Await TestFullName(code, "C1.I1.f1")
        End Function

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFullName_ImplicitOperator() As Task
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

            Await TestFullName(code, "ComplexType.implicit operator ComplexType")
        End Function

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFullName_ExplicitOperator() As Task
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

            Await TestFullName(code, "ComplexType.explicit operator ComplexType")
        End Function

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFullName_OperatorOverload() As Task
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

            Await TestFullName(code, "ComplexType.operator +")
        End Function

#End Region

#Region "FunctionKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFunctionKind_Destructor() As Task
            Dim code =
<Code>
class C
{
    ~C$$() { }
}
</Code>

            Await TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionDestructor)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFunctionKind_ExplicitInterfaceImplementation() As Task
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

            Await TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionFunction)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFunctionKind_Operator() As Task
            Dim code =
<Code>
public class C
{
    public static C operator $$+(C c1, C c2)
    {
    }
}
</Code>

            Await TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionOperator)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFunctionKind_ExplicitConversion() As Task
            Dim code =
<Code>
public class C
{
    public static static $$explicit C(int x)
    {
    }
}
</Code>

            Await TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionOperator)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFunctionKind_ImplicitConversion() As Task
            Dim code =
<Code>
public class C
{
    public static static $$implicit C(int x)
    {
    }
}
</Code>

            Await TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionOperator)
        End Function

#End Region

#Region "MustImplement tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestMustImplement1() As Task
            Dim code =
<Code>
abstract class C
{
    protected abstract void $$Foo();
}
</Code>

            Await TestMustImplement(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestMustImplement2() As Task
            Dim code =
<Code>
interface I
{
    void $$Foo();
}
</Code>

            Await TestMustImplement(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestMustImplement3() As Task
            Dim code =
<Code>
class C
{
    protected virtual void $$Foo() { }
}
</Code>

            Await TestMustImplement(code, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestMustImplement4() As Task
            Dim code =
<Code>
class C
{
    protected void $$Foo() { }
}
</Code>

            Await TestMustImplement(code, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestMustImplement5() As Task
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

            Await TestMustImplement(code, False)
        End Function

#End Region

#Region "Name tests"

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName_ExplicitlyImplementedMethod() As Task
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

            Await TestName(code, "I1.f1")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName_Destructor() As Task
            Dim code =
<Code>
class C
{
    ~C$$() { }
}
</Code>

            Await TestName(code, "~C")
        End Function

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName_ImplicitOperator() As Task
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

            Await TestName(code, "implicit operator ComplexType")
        End Function

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName_ExplicitOperator() As Task
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

            Await TestName(code, "explicit operator ComplexType")
        End Function

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName_OperatorOverload() As Task
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

            Await TestName(code, "operator +")
        End Function

#End Region

#Region "OverrideKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_Abstract() As Task
            Dim code =
<Code>
abstract class C
{
    protected abstract void $$Foo();
}
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_Virtual() As Task
            Dim code =
<Code>
class C
{
    protected virtual void $$Foo() { }
}
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_Sealed() As Task
            Dim code =
<Code>
class C
{
    protected sealed void $$Foo() { }
}
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindSealed)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_Override() As Task
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

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_New() As Task
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

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNew)
        End Function

#End Region

#Region "Prototype tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_FullNameOnly() As Task
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

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeFullname, "A.MethodC")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_UniqueSignature() As Task
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

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeUniqueSignature, "A.MethodC(int,bool)")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_ParamTypesOnly() As Task
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

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeParamTypes, "MethodC (int, bool)")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_ParamNamesOnly() As Task
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

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeParamNames, "MethodC (intA, boolB)")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_ReturnType() As Task
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

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeType, "bool MethodC")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_ClassName1() As Task
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

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "A.MethodC")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_ClassName2() As Task
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

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "A<>.MethodC")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_ClassName3() As Task
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

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "C<>.A.MethodC")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_ClassName4() As Task
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

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "C.A<>.MethodC")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_ClassName5() As Task
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

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "C.A.MethodC")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_ClassName6() As Task
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

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "A.MethodC")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_Constructor_Unique() As Task
            Dim code =
<Code>
class A
{
    public $$A()
    {
    }
}
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeUniqueSignature, "A.#ctor()")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_Finalizer_Unique() As Task
            Dim code =
<Code>
class A
{
    ~A$$()
    {
    }
}
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeUniqueSignature, "A.#dtor()")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_Unique_InvalidCombination() As Task
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

            Await TestPrototypeThrows(Of ArgumentException)(code, EnvDTE.vsCMPrototype.vsCMPrototypeUniqueSignature Or EnvDTE.vsCMPrototype.vsCMPrototypeClassName)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_Constructor_FullName() As Task
            Dim code =
<Code>
class A
{
    public A$$()
    {
    }
}
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeFullname, "A.A")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_Finalizer_FullName() As Task
            Dim code =
<Code>
class A
{
    ~A$$()
    {
    }
}
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeFullname, "A.~A")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_Operator_FullName() As Task
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

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeFullname, "A.operator +")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_Constructor_ReturnType() As Task
            Dim code =
<Code>
class A
{
    public A$$()
    {
    }
}
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeType, "void A")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_Finalizer_ReturnType() As Task
            Dim code =
<Code>
class A
{
    ~A$$()
    {
    }
}
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeType, "void ~A")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_Operator_ReturnType() As Task
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

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeType, "A operator +")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_Constructor_ClassName() As Task
            Dim code =
<Code>
class A
{
    public A$$()
    {
    }
}
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "A.A")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_Finalizer_ClassName() As Task
            Dim code =
<Code>
class A
{
    ~A$$()
    {
    }
}
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "A.~A")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_Operator_ClassName() As Task
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

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "A.operator +")
        End Function

#End Region

#Region "Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestType_Constructor() As Task
            Dim code =
<Code>
class A
{
    public $$A()
    {
    }
}
</Code>

            Await TestTypeProp(code,
                New CodeTypeRefData With
                {
                    .AsFullName = "System.Void",
                    .AsString = "void",
                    .CodeTypeFullName = "System.Void",
                    .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefVoid
                })
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestType_Finalizer() As Task
            Dim code =
<Code>
class A
{
    $$~A()
    {
    }
}
</Code>

            Await TestTypeProp(code,
                New CodeTypeRefData With
                {
                    .AsFullName = "System.Void",
                    .AsString = "void",
                    .CodeTypeFullName = "System.Void",
                    .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefVoid
                })
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestType_Operator() As Task
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

            Await TestTypeProp(code,
                New CodeTypeRefData With
                {
                    .AsFullName = "A",
                    .AsString = "A",
                    .CodeTypeFullName = "A",
                    .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType
                })
        End Function

#End Region

#Region "RemoveParameter tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess1() As Task
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

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess2() As Task
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

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess3() As Task
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

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess4() As Task
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

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess5() As Task
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

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess6() As Task
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

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProtected, ThrowsArgumentException(Of EnvDTE.vsCMAccess)())
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess7() As Task
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

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

#End Region

#Region "Set IsShared tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared1() As Task
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

            Await TestSetIsShared(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared2() As Task
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

            Await TestSetIsShared(code, expected, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetCanOverride1() As Task
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

            Await TestSetCanOverride(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetCanOverride2() As Task
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

            Await TestSetCanOverride(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetCanOverride3() As Task
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

            Await TestSetCanOverride(code, expected, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetCanOverride4() As Task
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

            Await TestSetCanOverride(code, expected, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetCanOverride5() As Task
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

            Await TestSetCanOverride(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetCanOverride6() As Task
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

            Await TestSetCanOverride(code, expected, False, ThrowsArgumentException(Of Boolean))
        End Function

#End Region

#Region "Set MustImplement tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetMustImplement1() As Task
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

            Await TestSetMustImplement(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetMustImplement2() As Task
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

            Await TestSetMustImplement(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetMustImplement3() As Task
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

            Await TestSetMustImplement(code, expected, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetMustImplement4() As Task
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

            Await TestSetMustImplement(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetMustImplement5() As Task
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

            Await TestSetMustImplement(code, expected, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetMustImplement6() As Task
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

            Await TestSetMustImplement(code, expected, True, ThrowsArgumentException(Of Boolean))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetMustImplement7() As Task
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

            Await TestSetMustImplement(code, expected, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetMustImplement8() As Task
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

            Await TestSetMustImplement(code, expected, False, ThrowsArgumentException(Of Boolean))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetMustImplement9() As Task
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

            Await TestSetMustImplement(code, expected, True)
        End Function

#End Region

#Region "Set OverrideKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetOverrideKind1() As Task
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

            Await TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetOverrideKind2() As Task
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

            Await TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindSealed)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetOverrideKind3() As Task
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

            Await TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract)
        End Function

#End Region

#Region "Set Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName1() As Task
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

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function

#End Region

#Region "Set Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType1() As Task
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

            Await TestSetTypeProp(code, expected, "System.Int32")
        End Function

#End Region

#Region "ExtensionMethodExtender"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestExtensionMethodExtender_IsExtension1() As Task
            Dim code =
<Code>
public static class C
{
    public static void $$Foo(this C c)
    {
    }
}
</Code>

            Await TestExtensionMethodExtender_IsExtension(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestExtensionMethodExtender_IsExtension2() As Task
            Dim code =
<Code>
public static class C
{
    public static void $$Foo(C c)
    {
    }
}
</Code>

            Await TestExtensionMethodExtender_IsExtension(code, False)
        End Function

#End Region

#Region "PartialMethodExtender"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPartialMethodExtender_IsPartial1() As Task
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

            Await TestPartialMethodExtender_IsPartial(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPartialMethodExtender_IsPartial2() As Task
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

            Await TestPartialMethodExtender_IsPartial(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPartialMethodExtender_IsPartial3() As Task
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

            Await TestPartialMethodExtender_IsPartial(code, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPartialMethodExtender_IsDeclaration1() As Task
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

            Await TestPartialMethodExtender_IsDeclaration(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPartialMethodExtender_IsDeclaration2() As Task
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

            Await TestPartialMethodExtender_IsDeclaration(code, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPartialMethodExtender_IsDeclaration3() As Task
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

            Await TestPartialMethodExtender_IsDeclaration(code, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPartialMethodExtender_HasOtherPart1() As Task
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

            Await TestPartialMethodExtender_HasOtherPart(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPartialMethodExtender_HasOtherPart2() As Task
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

            Await TestPartialMethodExtender_HasOtherPart(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPartialMethodExtender_HasOtherPart3() As Task
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

            Await TestPartialMethodExtender_HasOtherPart(code, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPartialMethodExtender_HasOtherPart4() As Task
            Dim code =
<Code>
public partial class C
{
    partial void $$M();
}
</Code>

            Await TestPartialMethodExtender_HasOtherPart(code, False)
        End Function

#End Region

#Region "Overloads Tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverloads1() As Task
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
            Await TestOverloadsUniqueSignatures(code, "C.Foo()", "C.Foo(C)")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverloads2() As Task
            Dim code =
<Code>
public static class C
{
    public static void $$Foo()
    {
    }
}
</Code>
            Await TestOverloadsUniqueSignatures(code, "C.Foo()")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverloads3() As Task
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
            Await TestOverloadsUniqueSignatures(code, "A.#op_Plus(A,A)")
        End Function

#End Region

#Region "Parameter name tests"

        <WorkItem(1147885)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParameterNameWithEscapeCharacters() As Task
            Dim code =
<Code>
public class C
{
    public void $$Foo(int @int)
    {
    }
}
</Code>
            Await TestAllParameterNames(code, "@int")
        End Function

        <WorkItem(1147885)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParameterNameWithEscapeCharacters_2() As Task
            Dim code =
<Code>
public class C
{
    public void $$Foo(int @int, string @string)
    {
    }
}
</Code>
            Await TestAllParameterNames(code, "@int", "@string")
        End Function

#End Region

#Region "AddAttribute tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestTypeDescriptor_GetProperties() As Task
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

            Await TestPropertyDescriptors(code, expectedPropertyNames)
        End Function

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

