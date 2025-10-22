' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Basic.Reference.Assemblies
Imports CompilationCreationTestHelpers
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.CorLibrary

    Public Class CorTypes
        Inherits BasicTestBase

        <Fact()>
        Public Sub MissingCorLib()
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences({TestReferences.SymbolsTests.CorLibrary.NoMsCorLibRef})
            Dim noMsCorLibRef = assemblies(0)

            For i As Integer = 1 To SpecialType.Count
                Dim t = noMsCorLibRef.GetSpecialType(CType(i, SpecialType))
                Assert.Equal(CType(i, SpecialType), t.SpecialType)
                Assert.Equal(CType(i, ExtendedSpecialType), t.ExtendedSpecialType)
                Assert.Equal(TypeKind.Error, t.TypeKind)
                Assert.NotNull(t.ContainingAssembly)
                Assert.Equal("<Missing Core Assembly>", t.ContainingAssembly.Identity.Name)
            Next

            For i As Integer = InternalSpecialType.First To InternalSpecialType.NextAvailable - 1
                Dim t = noMsCorLibRef.GetSpecialType(CType(i, InternalSpecialType))
                Assert.Equal(SpecialType.None, t.SpecialType)
                Assert.Equal(CType(i, ExtendedSpecialType), t.ExtendedSpecialType)
                Assert.Equal(TypeKind.Error, t.TypeKind)
                Assert.NotNull(t.ContainingAssembly)
                Assert.Equal("<Missing Core Assembly>", t.ContainingAssembly.Identity.Name)
            Next

            Dim p = noMsCorLibRef.GlobalNamespace.GetTypeMembers("I1").Single().
                GetMembers("M1").OfType(Of MethodSymbol)().Single().
                Parameters(0).Type

            Assert.Equal(TypeKind.Error, p.TypeKind)
            Assert.Equal(SpecialType.System_Int32, p.SpecialType)
        End Sub

        <Fact()>
        Public Sub PresentCorLib()
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences({NetCoreApp.SystemRuntime})
            Dim msCorLibRef As MetadataOrSourceAssemblySymbol = DirectCast(assemblies(0), MetadataOrSourceAssemblySymbol)

            Dim knownMissingSpecialTypes As HashSet(Of SpecialType) = New HashSet(Of SpecialType) From {SpecialType.System_Runtime_CompilerServices_InlineArrayAttribute}
            Dim knownMissingInternalSpecialTypes As HashSet(Of InternalSpecialType) = New HashSet(Of InternalSpecialType) From
            {
                InternalSpecialType.System_Runtime_CompilerServices_AsyncHelpers,
                InternalSpecialType.System_Runtime_InteropServices_ExtendedLayoutAttribute,
                InternalSpecialType.System_Runtime_InteropServices_ExtendedLayoutKind
            }

            For i As Integer = 1 To SpecialType.Count
                Dim specialType = CType(i, SpecialType)
                Dim t = msCorLibRef.GetSpecialType(specialType)
                Assert.Equal(CType(i, SpecialType), t.SpecialType)
                Assert.Equal(CType(i, ExtendedSpecialType), t.ExtendedSpecialType)
                Assert.Same(msCorLibRef, t.ContainingAssembly)
                If knownMissingSpecialTypes.Contains(specialType) Then
                    ' not present on dotnet core 3.1
                    Assert.Equal(TypeKind.Error, t.TypeKind)
                Else
                    Assert.NotEqual(TypeKind.Error, t.TypeKind)
                End If
            Next

            For i As Integer = InternalSpecialType.First To InternalSpecialType.NextAvailable - 1
                Dim internalSpecialType = CType(i, InternalSpecialType)
                Dim t = msCorLibRef.GetSpecialType(internalSpecialType)
                Assert.Equal(SpecialType.None, t.SpecialType)
                Assert.Equal(CType(i, ExtendedSpecialType), t.ExtendedSpecialType)
                Assert.Same(msCorLibRef, t.ContainingAssembly)
                If knownMissingInternalSpecialTypes.Contains(internalSpecialType) Then
                    ' not present on dotnet core 3.1
                    Assert.Equal(TypeKind.Error, t.TypeKind)
                Else
                    Assert.NotEqual(TypeKind.Error, t.TypeKind)
                End If
            Next

            Assert.False(msCorLibRef.KeepLookingForDeclaredSpecialTypes)

            assemblies = MetadataTestHelpers.GetSymbolsForReferences({MetadataReference.CreateFromImage(Net50.Resources.SystemRuntime)})
            msCorLibRef = DirectCast(assemblies(0), MetadataOrSourceAssemblySymbol)
            Assert.True(msCorLibRef.KeepLookingForDeclaredSpecialTypes)

            Dim namespaces As New Queue(Of NamespaceSymbol)()

            namespaces.Enqueue(msCorLibRef.Modules(0).GlobalNamespace)
            Dim count As Integer = 0

            While (namespaces.Count > 0)

                For Each m In namespaces.Dequeue().GetMembers()
                    Dim ns = TryCast(m, NamespaceSymbol)

                    If (ns IsNot Nothing) Then
                        namespaces.Enqueue(ns)
                    ElseIf (DirectCast(m, NamedTypeSymbol).SpecialType <> SpecialType.None) Then
                        count += 1
                    End If

                    If (count >= SpecialType.Count) Then
                        Assert.False(msCorLibRef.KeepLookingForDeclaredSpecialTypes)
                    End If
                Next
            End While

            Assert.Equal(count + knownMissingSpecialTypes.Count, CType(SpecialType.Count, Integer))
            Assert.Equal(knownMissingSpecialTypes.Any(), msCorLibRef.KeepLookingForDeclaredSpecialTypes)
        End Sub

        <Fact()>
        Public Sub FakeCorLib()
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences({TestReferences.SymbolsTests.CorLibrary.FakeMsCorLib.dll})
            Dim msCorLibRef = DirectCast(assemblies(0), MetadataOrSourceAssemblySymbol)

            For i As Integer = 1 To SpecialType.Count
                Assert.True(msCorLibRef.KeepLookingForDeclaredSpecialTypes)
                Dim t = msCorLibRef.GetSpecialType(CType(i, SpecialType))
                Assert.Equal(CType(i, SpecialType), t.SpecialType)
                Assert.Equal(CType(i, ExtendedSpecialType), t.ExtendedSpecialType)

                If (t.SpecialType = SpecialType.System_Object) Then
                    Assert.NotEqual(TypeKind.Error, t.TypeKind)
                Else
                    Assert.Equal(TypeKind.Error, t.TypeKind)
                End If

                Assert.Same(msCorLibRef, t.ContainingAssembly)
            Next

            For i As Integer = InternalSpecialType.First To InternalSpecialType.NextAvailable - 1
                Assert.True(msCorLibRef.KeepLookingForDeclaredSpecialTypes)
                Dim t = msCorLibRef.GetSpecialType(CType(i, InternalSpecialType))
                Assert.Equal(SpecialType.None, t.SpecialType)
                Assert.Equal(CType(i, ExtendedSpecialType), t.ExtendedSpecialType)

                Assert.Equal(TypeKind.Error, t.TypeKind)
                Assert.Same(msCorLibRef, t.ContainingAssembly)
            Next

            Assert.False(msCorLibRef.KeepLookingForDeclaredSpecialTypes)
        End Sub

        <Fact()>
        Public Sub SourceCorLib()
            Dim source =
<source>
namespace System
    public class Object
    End Class
ENd NAmespace
</source>

            Dim c1 = VisualBasicCompilation.Create("CorLib", syntaxTrees:={VisualBasicSyntaxTree.ParseText(source.Value)})

            Assert.Same(c1.Assembly, c1.Assembly.CorLibrary)

            Dim msCorLibRef = DirectCast(c1.Assembly, MetadataOrSourceAssemblySymbol)

            For i As Integer = 1 To SpecialType.Count
                If (i <> SpecialType.System_Object) Then
                    Assert.True(msCorLibRef.KeepLookingForDeclaredSpecialTypes)
                    Dim t = c1.Assembly.GetSpecialType(CType(i, SpecialType))
                    Assert.Equal(CType(i, SpecialType), t.SpecialType)
                    Assert.Equal(CType(i, ExtendedSpecialType), t.ExtendedSpecialType)

                    Assert.Equal(TypeKind.Error, t.TypeKind)
                    Assert.Same(msCorLibRef, t.ContainingAssembly)
                End If
            Next

            For i As Integer = InternalSpecialType.First To InternalSpecialType.NextAvailable - 1
                Assert.True(msCorLibRef.KeepLookingForDeclaredSpecialTypes)
                Dim t = c1.Assembly.GetSpecialType(CType(i, InternalSpecialType))
                Assert.Equal(SpecialType.None, t.SpecialType)
                Assert.Equal(CType(i, ExtendedSpecialType), t.ExtendedSpecialType)

                Assert.Equal(TypeKind.Error, t.TypeKind)
                Assert.Same(msCorLibRef, t.ContainingAssembly)
            Next

            Dim system_object = msCorLibRef.Modules(0).GlobalNamespace.GetMembers("System").
                Select(Function(m) DirectCast(m, NamespaceSymbol)).Single().GetTypeMembers("Object").Single()

            Assert.Equal(SpecialType.System_Object, system_object.SpecialType)

            Assert.False(msCorLibRef.KeepLookingForDeclaredSpecialTypes)

            Assert.Same(system_object, c1.Assembly.GetSpecialType(SpecialType.System_Object))

            Assert.Throws(Of ArgumentOutOfRangeException)(Function() c1.GetSpecialType(SpecialType.None))
            Assert.Throws(Of ArgumentOutOfRangeException)(Function() DirectCast(c1, Compilation).GetSpecialType(SpecialType.None))
            Assert.Throws(Of ArgumentOutOfRangeException)(Function() c1.GetSpecialType(InternalSpecialType.NextAvailable))
            Assert.Throws(Of ArgumentOutOfRangeException)(Function() DirectCast(c1, Compilation).GetSpecialType(CType(SpecialType.Count + 1, SpecialType)))
            Assert.Throws(Of ArgumentOutOfRangeException)(Function() msCorLibRef.GetSpecialType(SpecialType.None))
            Assert.Throws(Of ArgumentOutOfRangeException)(Function() msCorLibRef.GetSpecialType(InternalSpecialType.NextAvailable))
        End Sub

        <Fact()>
        Public Sub TestGetTypeByNameAndArity()

            Dim source1 =
<source>
namespace System

    Public Class TestClass
    End Class

    public class TestClass(Of T)
    End Class
End Namespace
</source>

            Dim source2 =
<source>
namespace System

    Public Class TestClass
    End Class
End Namespace
</source>

            Dim c1 = VisualBasicCompilation.Create("Test1",
                syntaxTrees:={VisualBasicSyntaxTree.ParseText(source1.Value)},
                references:={Net40.References.mscorlib})

            Assert.Null(c1.GetTypeByMetadataName("DoesntExist"))
            Assert.Null(c1.GetTypeByMetadataName("DoesntExist`1"))
            Assert.Null(c1.GetTypeByMetadataName("DoesntExist`2"))

            Dim c1TestClass As NamedTypeSymbol = c1.GetTypeByMetadataName("System.TestClass")
            Assert.NotNull(c1TestClass)
            Dim c1TestClassT As NamedTypeSymbol = c1.GetTypeByMetadataName("System.TestClass`1")
            Assert.NotNull(c1TestClassT)
            Assert.Null(c1.GetTypeByMetadataName("System.TestClass`2"))

            Dim c2 = VisualBasicCompilation.Create("Test2",
                        syntaxTrees:={VisualBasicSyntaxTree.ParseText(source2.Value)},
                        references:={New VisualBasicCompilationReference(c1),
                                        Net40.References.mscorlib})

            Dim c2TestClass As NamedTypeSymbol = c2.GetTypeByMetadataName("System.TestClass")
            Assert.Same(c2.Assembly, c2TestClass.ContainingAssembly)

            Dim c3 = VisualBasicCompilation.Create("Test3",
                        references:={New VisualBasicCompilationReference(c2),
                                    Net40.References.mscorlib})

            Dim c3TestClass As NamedTypeSymbol = c3.GetTypeByMetadataName("System.TestClass")
            Assert.NotSame(c2TestClass, c3TestClass)
            Assert.True(c3TestClass.ContainingAssembly.RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(c2TestClass.ContainingAssembly))

            Assert.Null(c3.GetTypeByMetadataName("System.TestClass`1"))

            Dim c4 = VisualBasicCompilation.Create("Test4",
                        references:={New VisualBasicCompilationReference(c1), New VisualBasicCompilationReference(c2),
                                    Net40.References.mscorlib})

            Dim c4TestClass As NamedTypeSymbol = c4.GetTypeByMetadataName("System.TestClass")
            Assert.Null(c4TestClass)

            Assert.Same(c1TestClassT, c4.GetTypeByMetadataName("System.TestClass`1"))
        End Sub

        <Fact>
        Public Sub System_Type__WellKnownVsSpecial_01()
            Dim source =
<compilation>
    <file name="c.vb"><![CDATA[
class Program
    shared Sub Main()
        Dim x = GetType(Program)
        System.Console.WriteLine(x)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp = CreateCompilation(source, options:=TestOptions.DebugExe)
            comp.MakeMemberMissing(WellKnownMember.System_Type__GetTypeFromHandle)

            Assert.False(comp.GetSpecialType(InternalSpecialType.System_Type).IsErrorType())

            Dim tree = comp.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of GetTypeExpressionSyntax)().Single()
            Dim model = comp.GetSemanticModel(tree)

            Assert.Equal(InternalSpecialType.System_Type, DirectCast(model.GetTypeInfo(node).Type, TypeSymbol).ExtendedSpecialType)

            CompileAndVerify(comp, expectedOutput:="Program")

            comp = CreateCompilation(source, options:=TestOptions.DebugExe)
            comp.MakeMemberMissing(SpecialMember.System_Type__GetTypeFromHandle)
            comp.AssertTheseEmitDiagnostics(
 <expected>
BC35000: Requested operation is not available because the runtime library function 'System.Type.GetTypeFromHandle' is not defined.
        Dim x = GetType(Program)
                ~~~~~~~~~~~~~~~~
</expected>
            )
        End Sub

        <Fact>
        Public Sub System_Type__WellKnownVsSpecial_02()
            Dim corLib_v1 = "
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

    public struct Int32
    {}

    public struct Boolean
    {}

    public class Attribute
    {}

    public class Enum
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
}
"
            Dim corLib_v1_Comp = CreateCSharpCompilation(corLib_v1, referencedAssemblies:={}, assemblyName:="corLib")

            Dim typeLib_v1 = "
namespace System
{
    public class Type
    {
        public static Type GetTypeFromHandle(RuntimeTypeHandle handle) => null;
    }
}
"
            Dim corLib_v1_Ref = corLib_v1_Comp.EmitToImageReference()

            Dim typeLib_v1_Comp = CreateCSharpCompilation(typeLib_v1, referencedAssemblies:={corLib_v1_Ref}, assemblyName:="typeLib")

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
public class Test
    shared Function [TypeOf]() as System.Type
        Return GetType(Test)
    End Function
End Class
]]></file>
</compilation>
            Dim typeLib_v1_Ref = typeLib_v1_Comp.EmitToImageReference()

            Dim comp1 = CreateEmptyCompilation(source1, references:={corLib_v1_Ref, typeLib_v1_Ref})

            Assert.True(comp1.GetSpecialType(InternalSpecialType.System_Type).IsErrorType())
            comp1.MakeMemberMissing(SpecialMember.System_Type__GetTypeFromHandle)

            Dim tree = comp1.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of GetTypeExpressionSyntax)().Single()
            Dim model = comp1.GetSemanticModel(tree)

            Assert.Equal(CType(0, ExtendedSpecialType), DirectCast(model.GetTypeInfo(node).Type, TypeSymbol).ExtendedSpecialType)

            Dim comp1Ref = comp1.EmitToImageReference()

            Dim corLib_v2 = "
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Object))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(void))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.ValueType))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.RuntimeTypeHandle))]
"
            Dim corLib_v2_Comp = CreateCSharpCompilation(
                                     corLib_v2,
                                     referencedAssemblies:=TargetFrameworkUtil.GetReferences(TargetFramework.Standard),
                                     assemblyName:="corLib")

            Dim typeLib_v2 = "
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Type))]
"

            Dim typeLib_v2_Comp = CreateCSharpCompilation(
                                       typeLib_v2,
                                       referencedAssemblies:=TargetFrameworkUtil.GetReferences(TargetFramework.Standard),
                                       assemblyName:="typeLib")

            Dim source2 =
<compilation>
    <file name="c.vb"><![CDATA[
class Program
    shared Sub Main()
        System.Console.WriteLine(Test.TypeOf())
    End Sub
End Class
]]></file>
</compilation>

            Dim comp = CreateCompilation(source2,
                                         references:={corLib_v2_Comp.EmitToImageReference(), typeLib_v2_Comp.EmitToImageReference(), comp1Ref},
                                         options:=TestOptions.DebugExe)
            CompileAndVerify(comp, expectedOutput:="Test")

            comp1 = CreateEmptyCompilation(source1, references:={corLib_v1_Ref, typeLib_v1_Ref})

            comp1.MakeMemberMissing(WellKnownMember.System_Type__GetTypeFromHandle)
            comp1.AssertTheseEmitDiagnostics(
<expected>
BC35000: Requested operation is not available because the runtime library function 'System.Type.GetTypeFromHandle' is not defined.
        Return GetType(Test)
               ~~~~~~~~~~~~~
</expected>
            )
        End Sub

        <Fact>
        Public Sub CreateDelegate__MethodInfoVsDelegate_01()
            Dim source =
<compilation>
    <file name="c.vb"><![CDATA[
class Program
    shared Sub Main()
        Dim x As System.Linq.Expressions.Expression(Of System.Func(Of System.Action))= Function() AddressOf C1.M1
        System.Console.WriteLine(x)
    End Sub
End Class

class C1
    public Shared Sub M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp = CreateCompilation(source, targetFramework:=TargetFramework.Mscorlib40AndSystemCore, options:=TestOptions.DebugExe)
            comp.MakeMemberMissing(WellKnownMember.System_Reflection_MethodInfo__CreateDelegate)
            comp.MakeMemberMissing(SpecialMember.System_Reflection_MethodBase__GetMethodFromHandle2)
            comp.MakeMemberMissing(WellKnownMember.System_Reflection_MethodBase__GetMethodFromHandle)
            comp.MakeMemberMissing(WellKnownMember.System_Reflection_MethodBase__GetMethodFromHandle2)

            CompileAndVerify(comp, expectedOutput:="() => Convert(CreateDelegate(System.Action, null, Void M1(), False)" +
                                      If(ExecutionConditionUtil.IsMonoOrCoreClr, ", Action", "") +
                                      ")")

            comp = CreateCompilation(source, targetFramework:=TargetFramework.Mscorlib40AndSystemCore, options:=TestOptions.DebugExe)
            comp.MakeMemberMissing(SpecialMember.System_Delegate__CreateDelegate4)
            comp.AssertTheseEmitDiagnostics(
<expected>
BC35000: Requested operation is not available because the runtime library function 'System.Delegate.CreateDelegate' is not defined.
        Dim x As System.Linq.Expressions.Expression(Of System.Func(Of System.Action))= Function() AddressOf C1.M1
                                                                                                  ~~~~~~~~~~~~~~~
</expected>
            )

            comp = CreateCompilation(source, targetFramework:=TargetFramework.Mscorlib40AndSystemCore, options:=TestOptions.DebugExe)
            comp.MakeMemberMissing(SpecialMember.System_Reflection_MethodBase__GetMethodFromHandle)
            comp.AssertTheseEmitDiagnostics(
<expected>
BC35000: Requested operation is not available because the runtime library function 'System.Reflection.MethodBase.GetMethodFromHandle' is not defined.
        Dim x As System.Linq.Expressions.Expression(Of System.Func(Of System.Action))= Function() AddressOf C1.M1
                                                                                                  ~~~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Reflection.MethodBase.GetMethodFromHandle' is not defined.
        Dim x As System.Linq.Expressions.Expression(Of System.Func(Of System.Action))= Function() AddressOf C1.M1
                                                                                                  ~~~~~~~~~~~~~~~
</expected>
            )
        End Sub

        <Fact>
        Public Sub CreateDelegate__MethodInfoVsDelegate_02()
            Dim source =
<compilation>
    <file name="c.vb"><![CDATA[
class Program
    shared Sub Main()
        Dim x As System.Linq.Expressions.Expression(Of System.Func(Of System.Action))= Function() AddressOf C1(Of Integer).M1
        System.Console.WriteLine(x)
    End Sub
End Class

class C1(Of T)
    public Shared Sub M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp = CreateCompilation(source, options:=TestOptions.DebugExe)
            comp.MakeMemberMissing(SpecialMember.System_Delegate__CreateDelegate)
            comp.MakeMemberMissing(WellKnownMember.System_Reflection_MethodBase__GetMethodFromHandle)
            comp.MakeMemberMissing(WellKnownMember.System_Reflection_MethodBase__GetMethodFromHandle2)

            CompileAndVerify(
                comp, expectedOutput:="() => Convert(Void M1().CreateDelegate(System.Action, null)" +
                                       If(ExecutionConditionUtil.IsMonoOrCoreClr, ", Action", "") +
                                      ")")

            comp = CreateCompilation(source, options:=TestOptions.DebugExe)
            comp.MakeMemberMissing(SpecialMember.System_Reflection_MethodBase__GetMethodFromHandle2)
            comp.AssertTheseEmitDiagnostics(
<expected>
BC35000: Requested operation is not available because the runtime library function 'System.Reflection.MethodBase.GetMethodFromHandle' is not defined.
        Dim x As System.Linq.Expressions.Expression(Of System.Func(Of System.Action))= Function() AddressOf C1(Of Integer).M1
                                                                                                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>
            )
        End Sub

        <Fact>
        Public Sub GetMethodFromHandle_WellKnown_01()
            Dim corLib_v1 = "
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

    public struct IntPtr
    {}

    public struct Boolean
    {}

    public class Attribute
    {}

    public class Enum
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
}

namespace System.Collections.Generic
{
    public interface IEnumerable<T>
    {}
}
"
            Dim corLib_v1_Comp = CreateCSharpCompilation(corLib_v1, referencedAssemblies:={}, assemblyName:="corLib")

            Dim typeLib_v1 = "
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
"
            Dim corLib_v1_Ref = corLib_v1_Comp.EmitToImageReference()

            Dim typeLib_v1_Comp = CreateCSharpCompilation(typeLib_v1, referencedAssemblies:={corLib_v1_Ref}, assemblyName:="typeLib")

            typeLib_v1_Comp.VerifyDiagnostics()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
public class Test
    shared Function Expression() As System.Linq.Expressions.Expression(Of System.Func(Of System.Action))
        return Function() AddressOf C1.M1
    End Function
End Class

class C1
    public Shared Sub M1()
    End Sub
End Class
]]></file>
</compilation>
            Dim typeLib_v1_Ref = typeLib_v1_Comp.EmitToImageReference()
            Dim comp1 = CreateEmptyCompilation(source1, references:={corLib_v1_Ref, typeLib_v1_Ref})

            comp1.MakeMemberMissing(SpecialMember.System_Reflection_MethodBase__GetMethodFromHandle)
            comp1.MakeMemberMissing(SpecialMember.System_Reflection_MethodBase__GetMethodFromHandle2)
            comp1.MakeMemberMissing(WellKnownMember.System_Reflection_MethodBase__GetMethodFromHandle2)

            Dim comp1Ref = comp1.EmitToImageReference()

            Dim corLib_v2 = "
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
"
            Dim corLib_v2_Comp = CreateCSharpCompilation(
                                     corLib_v2,
                                     referencedAssemblies:=TargetFrameworkUtil.GetReferences(TargetFramework.Standard),
                                     assemblyName:="corLib")

            Dim typeLib_v2 = "
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
"

            Dim typeLib_v2_Comp = CreateCSharpCompilation(
                                      typeLib_v2,
                                      referencedAssemblies:=TargetFrameworkUtil.GetReferences(TargetFramework.Standard),
                                      assemblyName:="typeLib")

            Dim source2 =
<compilation>
    <file name="c.vb"><![CDATA[
class Program
    shared Sub Main()
        System.Console.WriteLine(Test.Expression())
    End Sub
End Class
]]></file>
</compilation>

            Dim comp = CreateCompilation(source2, references:={corLib_v2_Comp.EmitToImageReference(), typeLib_v2_Comp.EmitToImageReference(), comp1Ref}, options:=TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:="() => Convert(Void M1().CreateDelegate(System.Action, null)" +
                                                   If(ExecutionConditionUtil.IsMonoOrCoreClr, ", Action", "") +
                                                   ")")

            comp1 = CreateEmptyCompilation(
                source1, references:={corLib_v1_Ref, typeLib_v1_Ref})

            comp1.MakeMemberMissing(WellKnownMember.System_Reflection_MethodBase__GetMethodFromHandle)
            comp1.AssertTheseEmitDiagnostics(
<expected>
BC35000: Requested operation is not available because the runtime library function 'System.Reflection.MethodBase.GetMethodFromHandle' is not defined.
        return Function() AddressOf C1.M1
                          ~~~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Reflection.MethodBase.GetMethodFromHandle' is not defined.
        return Function() AddressOf C1.M1
                          ~~~~~~~~~~~~~~~
</expected>
            )
        End Sub

        <Fact>
        Public Sub GetMethodFromHandle_WellKnown_02()
            Dim corLib_v1 = "
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

    public struct IntPtr
    {}

    public struct Boolean
    {}

    public class Attribute
    {}

    public class Enum
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
}

namespace System.Collections.Generic
{
    public interface IEnumerable<T>
    {}
}
"
            Dim corLib_v1_Comp = CreateCSharpCompilation(corLib_v1, referencedAssemblies:={}, assemblyName:="corLib")

            Dim typeLib_v1 = "
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
"
            Dim corLib_v1_Ref = corLib_v1_Comp.EmitToImageReference()

            Dim typeLib_v1_Comp = CreateCSharpCompilation(typeLib_v1, referencedAssemblies:={corLib_v1_Ref}, assemblyName:="typeLib")

            typeLib_v1_Comp.VerifyDiagnostics()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
public class Test
    shared Function Expression() As System.Linq.Expressions.Expression(Of System.Func(Of System.Action))
        return Function() AddressOf C1(Of Integer).M1
    End Function
End Class

class C1(Of T)
    public Shared Sub M1()
    End Sub
End Class
]]></file>
</compilation>
            Dim typeLib_v1_Ref = typeLib_v1_Comp.EmitToImageReference()
            Dim comp1 = CreateEmptyCompilation(source1, references:={corLib_v1_Ref, typeLib_v1_Ref})

            comp1.MakeMemberMissing(SpecialMember.System_Reflection_MethodBase__GetMethodFromHandle)
            comp1.MakeMemberMissing(SpecialMember.System_Reflection_MethodBase__GetMethodFromHandle2)

            Dim comp1Ref = comp1.EmitToImageReference()

            Dim corLib_v2 = "
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
"
            Dim corLib_v2_Comp = CreateCSharpCompilation(
                                     corLib_v2,
                                     referencedAssemblies:=TargetFrameworkUtil.GetReferences(TargetFramework.Standard),
                                     assemblyName:="corLib")

            Dim typeLib_v2 = "
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
"

            Dim typeLib_v2_Comp = CreateCSharpCompilation(
                                      typeLib_v2,
                                      referencedAssemblies:=TargetFrameworkUtil.GetReferences(TargetFramework.Standard),
                                      assemblyName:="typeLib")

            Dim source2 =
<compilation>
    <file name="c.vb"><![CDATA[
class Program
    shared Sub Main()
        System.Console.WriteLine(Test.Expression())
    End Sub
End Class
]]></file>
</compilation>

            Dim comp = CreateCompilation(source2, references:={corLib_v2_Comp.EmitToImageReference(), typeLib_v2_Comp.EmitToImageReference(), comp1Ref}, options:=TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:="() => Convert(Void M1().CreateDelegate(System.Action, null)" +
                                                   If(ExecutionConditionUtil.IsMonoOrCoreClr, ", Action", "") +
                                                   ")")

            comp1 = CreateEmptyCompilation(
                source1, references:={corLib_v1_Ref, typeLib_v1_Ref})

            comp1.MakeMemberMissing(WellKnownMember.System_Reflection_MethodBase__GetMethodFromHandle2)
            comp1.AssertTheseEmitDiagnostics(
<expected>
BC35000: Requested operation is not available because the runtime library function 'System.Reflection.MethodBase.GetMethodFromHandle' is not defined.
        return Function() AddressOf C1(Of Integer).M1
                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>
            )
        End Sub

    End Class

End Namespace
