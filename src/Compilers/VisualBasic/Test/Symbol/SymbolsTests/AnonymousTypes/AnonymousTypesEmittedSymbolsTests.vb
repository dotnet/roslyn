' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.ExtensionMethods

    Public Class AnonymousTypesEmittedSymbolsTests : Inherits BasicTestBase

        <Fact>
        Public Sub EmitAnonymousTypeTemplate_NoNeededSymbols()
            Dim compilationDef =
    <compilation name="EmitAnonymousTypeTemplate_NoNeededSymbols">
        <file name="a.vb">
Class ModuleB
    Private v1 = New With { .aa = 1 }
End Class
        </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(source:=compilationDef, references:={})
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30002: Type 'System.Void' is not defined.
Class ModuleB
~~~~~~~~~~~~~~
BC31091: Import of type 'Object' from assembly or module 'EmitAnonymousTypeTemplate_NoNeededSymbols.dll' failed.
Class ModuleB
      ~~~~~~~
BC30002: Type 'System.Object' is not defined.
    Private v1 = New With { .aa = 1 }
            ~~
BC30002: Type 'System.Object' is not defined.
    Private v1 = New With { .aa = 1 }
                     ~~~~~~~~~~~~~~~~
BC30002: Type 'System.Int32' is not defined.
    Private v1 = New With { .aa = 1 }
                                  ~
</errors>)
        End Sub

        <Fact>
        Public Sub EmitAnonymousTypeTemplateGenericTest()
            Dim compilationDef =
    <compilation name="EmitGenericAnonymousTypeTemplate">
        <file name="a.vb">
Module ModuleB
    Structure SSS
    End Structure 
    Sub Test1(x As Integer)
        Dim v1 As Object = New With { .aa = 1, .BB = "", .CCC = new SSS() }
    End Sub
End Module
        </file>
    </compilation>

            CompileAndVerify(compilationDef,
                             references:={SystemCoreRef},
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Dim type = m.ContainingAssembly.GetTypeByMetadataName("VB$AnonymousType_0`3")
                                                  Assert.NotNull(type)
                                                  Assert.Equal(GetNamedTypeSymbol(m, "System.Object"), type.BaseType)
                                                  Assert.True(type.IsGenericType)
                                                  Assert.Equal(3, type.TypeParameters.Length)
                                              End Sub)
        End Sub

        <Fact>
        Public Sub EmitAnonymousTypeUnification01()
            Dim compilationDef =
    <compilation name="EmitAnonymousTypeUnification01">
        <file name="a.vb">
Module ModuleB
    Sub Test1(x As Integer)
        Dim at1 As Object = New With { .aa = 1, .b1 = "", .Int = x + x, .Object=Nothing }
        Dim at2 As Object = New With { .aA = "X"c, .B1 = 0.123# * x, .int = new Object(), .objecT = Nothing }
        at1 = at2
    End Sub
End Module
        </file>
    </compilation>

            CompileAndVerify(compilationDef,
                             references:={SystemCoreRef},
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Dim types = m.ContainingAssembly.GlobalNamespace.GetTypeMembers()
                                                  Dim list = types.Where(Function(t) t.Name.StartsWith("VB$AnonymousType_", StringComparison.Ordinal)).ToList()
                                                  Assert.Equal(1, list.Count())
                                                  Dim type = list.First()
                                                  Assert.Equal("VB$AnonymousType_0", type.Name)
                                                  Assert.Equal(4, type.Arity)
                                                  Dim mems = type.GetMembers()
                                                  ' 4 fields, 4 get, 4 set, 1 ctor, 1 ToString
                                                  Assert.Equal(14, mems.Length)
                                                  Dim mlist = mems.Where(Function(mt) mt.Name = "GetHashCode" OrElse mt.Name = "Equals").Select(Function(mt) mt)
                                                  Assert.Equal(0, mlist.Count)
                                              End Sub)
        End Sub

        <Fact>
        Public Sub EmitAnonymousTypeUnification02()
            Dim compilationDef =
    <compilation name="EmitAnonymousTypeUnification02">
        <file name="b.vb">
Imports System
Imports System.Collections.Generic

Class A
    Sub Test(p As List(Of String))
        Dim local = New Char() {"a"c, "b"c}
        Dim at1 As Object = New With {.test = p, Key .key = local, Key .k2 = "QC" + p(0)}
    End Sub
End Class
        </file>
        <file name="a.vb">
Structure S
    Function Test(p As Char()) As Object
        Dim at2 As Object = New With {.TEST = New System.Collections.Generic.List(Of String)(), Key .Key = p, Key .K2 = ""}
        Return at2
    End Function
End Structure
        </file>
    </compilation>

            CompileAndVerify(compilationDef,
                             references:={SystemCoreRef},
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Dim types = m.ContainingAssembly.GlobalNamespace.GetTypeMembers()
                                                  Dim list = types.Where(Function(t) t.Name.StartsWith("VB$AnonymousType", StringComparison.Ordinal)).ToList()
                                                  Assert.Equal(1, list.Count())
                                                  Dim type = list.First()
                                                  Assert.Equal("VB$AnonymousType_0", type.Name)
                                                  Assert.Equal(3, type.Arity)
                                                  Dim mems = type.GetMembers()
                                                  ' 3 fields, 3 get, 1 set, 1 ctor, 1 ToString, 1 GetHashCode, 2 Equals
                                                  Assert.Equal(12, mems.Length)
                                                  Dim mlist = mems.Where(Function(mt) mt.Name = "GetHashCode" OrElse mt.Name = "Equals").Select(Function(mt) mt)
                                                  Assert.Equal(3, mlist.Count)
                                              End Sub)
        End Sub

        <Fact>
        Public Sub EmitAnonymousTypeUnification03()
            Dim compilationDef =
    <compilation name="EmitAnonymousTypeUnification03">
        <file name="b.vb">
Imports System
Imports System.Collections.Generic

Class A
    Sub Test(p As List(Of String))
        Dim local = New Char() {"a"c, "b"c}
        Dim at1 As Object = New With {.test = p, Key .key = local, Key .k2 = "QC" + p(0)}
    End Sub
End Class
        </file>
        <file name="a.vb">
Imports System
Imports System.Collections.Generic

Class B
    Sub Test(p As List(Of String))
        Dim local = New Char() {"a"c, "b"c}
        Dim at1 As Object = New With {.test = p, Key .key = local, .k2 = "QC" + p(0)}
    End Sub
End Class
        </file>
    </compilation>

            CompileAndVerify(compilationDef,
                             references:={SystemCoreRef},
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Dim types = m.ContainingAssembly.GlobalNamespace.GetTypeMembers()
                                                  Dim list = types.Where(Function(t) t.Name.StartsWith("VB$AnonymousType", StringComparison.Ordinal)).ToList()
                                                  ' no unification - diff in key
                                                  Assert.Equal(2, list.Count())
                                                  Dim type = m.ContainingAssembly.GlobalNamespace.GetTypeMembers("VB$AnonymousType_1").Single()

                                                  Assert.Equal("VB$AnonymousType_1", type.Name)
                                                  Assert.Equal(3, type.Arity)
                                                  Dim mems = type.GetMembers()
                                                  ' Second: 3 fields, 3 get, 2 set, 1 ctor, 1 ToString, 1 GetHashCode, 2 Equals
                                                  Assert.Equal(13, mems.Length)
                                                  Dim mlist = mems.Where(Function(mt) mt.Name = "GetHashCode" OrElse mt.Name = "Equals").Select(Function(mt) mt)
                                                  Assert.Equal(3, mlist.Count)
                                                  '
                                                  type = m.ContainingAssembly.GlobalNamespace.GetTypeMembers("VB$AnonymousType_0").Single()
                                                  Assert.Equal("VB$AnonymousType_0", type.Name)
                                                  Assert.Equal(3, type.Arity)
                                                  mems = type.GetMembers()
                                                  ' First: 3 fields, 3 get, 1 set, 1 ctor, 1 ToString, 1 GetHashCode, 2 Equals
                                                  Assert.Equal(12, mems.Length)
                                                  mlist = mems.Where(Function(mt) mt.Name = "GetHashCode" OrElse mt.Name = "Equals").Select(Function(mt) mt)
                                                  Assert.Equal(3, mlist.Count)
                                              End Sub)
        End Sub

        <Fact>
        Public Sub EmitAnonymousTypeCustomModifiers()
            Dim compilationDef =
    <compilation name="EmitAnonymousTypeCustomModifiers">
        <file name="b.vb">
Imports System
Imports System.Collections.Generic

Class A
    Sub Test(p As List(Of String))
        Dim intArrMod = Modifiers.F9()
        Dim at1 = New With { Key .f = intArrMod}
        Dim at2 = New With { Key .f = New Integer() {}}
        at1 = at2
        at2 = at1
    End Sub
End Class
        </file>
    </compilation>

            CompileAndVerify(compilationDef,
                             references:={SystemCoreRef, TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll})
        End Sub

        <Fact>
        Public Sub AnonymousTypes_MultiplyEmitOfTheSameAssembly()
            Dim compilationDef =
    <compilation name="AnonymousTypes_MultiplyEmitOfTheSameAssembly">
        <file name="b.vb">
Module ModuleB
    Sub Test1(x As Integer)
        Dim v1 As Object = New With { .aa = 1 }
        Dim v2 As Object = New With { Key .AA = "a" }
    End Sub
End Module
        </file>
        <file name="a.vb">
Module ModuleA
    Sub Test1(x As Integer)
        Dim v1 As Object = New With { .Aa = 1 }
        Dim v2 As Object = New With { Key .aA = "A" }
    End Sub
End Module
        </file>
        <file name="c.vb">
Module ModuleC
    Sub Test1(x As Integer)
        Dim v1 As Object = New With { .AA = 1 }
        Dim v2 As Object = New With { Key .aa = "A" }
    End Sub
End Module
        </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim position = compilationDef.<file>.Value.IndexOf("Dim v2", StringComparison.Ordinal) - 1

            ' The sole purpose of this is to check if there will be any asserts 
            ' or exceptions related to adjusted names/locations of anonymous types 
            ' symbols when Emit is called several times for the same compilation
            For i = 0 To 10
                Using stream As New MemoryStream()
                    compilation.Emit(stream)
                End Using

                ' do some speculative semantic query
                Dim expr1 = SyntaxFactory.ParseExpression(<text>New With { .aa = 1, .BB<%= i %> = "" }</text>.Value)
                Dim info1 = model.GetSpeculativeTypeInfo(position, expr1, SpeculativeBindingOption.BindAsExpression)
                Assert.NotNull(info1.Type)
            Next
        End Sub

        <Fact>
        Public Sub EmitCompilerGeneratedAttributeTest()
            Dim compilationDef =
    <compilation name="EmitCompilerGeneratedAttributeTest">
        <file name="a.vb">
Module Module1
    Sub Test1(x As Integer)
        Dim v As Object = New With { .a = 1 }
    End Sub
End Module
        </file>
    </compilation>

            CompileAndVerify(compilationDef,
                             references:={SystemCoreRef},
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Dim type = m.ContainingAssembly.GetTypeByMetadataName("VB$AnonymousType_0`1")
                                                  Assert.NotNull(type)

                                                  Dim attr = type.GetAttribute(
                                                      GetNamedTypeSymbol(m, "System.Runtime.CompilerServices.CompilerGeneratedAttribute"))
                                                  Assert.NotNull(attr)
                                              End Sub)
        End Sub

        <Fact>
        <WorkItem(48417, "https://github.com/dotnet/roslyn/issues/48417")>
        Public Sub CheckPropertyFieldAndAccessorsNamesTest()
            Dim compilationDef =
    <compilation name="CheckPropertyFieldAndAccessorsNamesTest">
        <file name="b.vb">
Module ModuleB
    Sub Test1(x As Integer)
        Dim v1 As Object = New With { .aab = 1 }.aab
        Dim v2 As Object = New With { Key .AAB = "a" }.aab
    End Sub
End Module
        </file>
        <file name="a.vb">
Module ModuleA
    Sub Test1(x As Integer)
        Dim v1 As Object = New With { .Aab = 1 }.aab
        Dim v2 As Object = New With { Key .aAB = "A" }.aab
    End Sub
End Module
        </file>
        <file name="c.vb">
Module ModuleC
    Sub Test1(x As Integer)
        Dim v1 As Object = New With { .AAb = 1 }.aab
        Dim v2 As Object = New With { Key .aaB = "A" }.aab
    End Sub
End Module
        </file>
    </compilation>

            ' Cycle to hopefully get different order of files
            For i = 0 To 50
                CompileAndVerify(compilationDef,
                                 references:={SystemCoreRef},
                                 options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All),
                                 symbolValidator:=Sub(m As ModuleSymbol)
                                                      Dim type = m.ContainingAssembly.GetTypeByMetadataName("VB$AnonymousType_0`1")
                                                      Assert.NotNull(type)
                                                      CheckPropertyAccessorsAndField(m, type, "aab", type.TypeParameters(0), False)

                                                      type = m.ContainingAssembly.GetTypeByMetadataName("VB$AnonymousType_1`1")
                                                      Assert.NotNull(type)
                                                      CheckPropertyAccessorsAndField(m, type, "AAB", type.TypeParameters(0), True)
                                                  End Sub)
            Next
        End Sub

        <Fact>
        Public Sub CheckEmittedSymbol_ctor()
            Dim compilationDef =
    <compilation name="CheckEmittedSymbol_ctor">
        <file name="b.vb">
Module ModuleB
    Structure SSS
    End Structure 
    Sub Test1(x As Integer)
        Dim v1 As Object = New With { .aa = 1, .BB = "", .CCC = new SSS() }
    End Sub
End Module
        </file>
    </compilation>

            CompileAndVerify(compilationDef,
                             references:={SystemCoreRef},
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Dim type = m.ContainingAssembly.GetTypeByMetadataName("VB$AnonymousType_0`3")
                                                  Assert.NotNull(type)

                                                  Assert.Equal(1, type.InstanceConstructors.Length)
                                                  Dim constr = type.InstanceConstructors(0)

                                                  CheckMethod(m, constr, ".ctor", Accessibility.Public, isSub:=True, paramCount:=3)

                                                  Assert.Equal(type.TypeParameters(0), constr.Parameters(0).Type)
                                                  Assert.Equal("aa", constr.Parameters(0).Name)

                                                  Assert.Equal(type.TypeParameters(1), constr.Parameters(1).Type)
                                                  Assert.Equal("BB", constr.Parameters(1).Name)

                                                  Assert.Equal(type.TypeParameters(2), constr.Parameters(2).Type)
                                                  Assert.Equal("CCC", constr.Parameters(2).Name)
                                              End Sub)
        End Sub

        <Fact>
        Public Sub CheckEmittedSymbol_ToString()
            Dim compilationDef =
    <compilation name="CheckEmittedSymbol_ToString">
        <file name="b.vb">
Module ModuleB
    Structure SSS
    End Structure 
    Sub Test1(x As Integer)
        Dim v1 As Object = New With { .aa = 1, .BB = "", .CCC = new SSS() }
    End Sub
End Module
        </file>
    </compilation>

            CompileAndVerify(compilationDef,
                             references:={SystemCoreRef},
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Dim type = m.ContainingAssembly.GetTypeByMetadataName("VB$AnonymousType_0`3")
                                                  Assert.NotNull(type)

                                                  Dim toStr = type.GetMember(Of MethodSymbol)("ToString")

                                                  CheckMethod(m, toStr, "ToString", Accessibility.Public,
                                                              retType:=GetNamedTypeSymbol(m, "System.String"),
                                                              isOverrides:=True, isOverloads:=True)

                                                  Assert.Equal(GetNamedTypeSymbol(m, "System.Object").GetMember("ToString"), toStr.OverriddenMethod)
                                              End Sub)
        End Sub

        <Fact>
        Public Sub CheckNoExtraMethodsAreEmittedIfThereIsNoKeyFields()
            Dim compilationDef =
    <compilation name="CheckNoExtraMethodsAreEmittedIfThereIsNoKeyFields">
        <file name="b.vb">
Module ModuleB
    Structure SSS
    End Structure 
    Sub Test1(x As Integer)
        Dim v1 As Object = New With { .aa = 1, .BB = "", .CCC = new SSS() }
    End Sub
End Module
        </file>
    </compilation>

            CompileAndVerify(compilationDef,
                             references:={SystemCoreRef},
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Dim type = m.ContainingAssembly.GetTypeByMetadataName("VB$AnonymousType_0`3")
                                                  Assert.NotNull(type)

                                                  Assert.Equal(0, type.Interfaces.Length)
                                                  Assert.Equal(0, type.GetMembers("GetHashCode").Length)
                                                  Assert.Equal(0, type.GetMembers("Equals").Length)
                                              End Sub)
        End Sub

        <Fact>
        Public Sub CheckEmittedSymbol_SystemObject_Equals()
            Dim compilationDef =
    <compilation name="CheckEmittedSymbol_SystemObject_Equals">
        <file name="b.vb">
Module ModuleB
    Structure SSS
    End Structure 
    Sub Test1(x As Integer)
        Dim v1 As Object = New With { Key .aa = 1, .BB = "", .CCC = new SSS() }
    End Sub
End Module
        </file>
    </compilation>

            CompileAndVerify(compilationDef,
                             references:={SystemCoreRef},
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Dim type = m.ContainingAssembly.GetTypeByMetadataName("VB$AnonymousType_0`3")
                                                  Assert.NotNull(type)

                                                  Dim method = DirectCast(type.GetMembers("Equals").
                                                                                    Where(Function(s)
                                                                                              Return DirectCast(s, MethodSymbol).ExplicitInterfaceImplementations.Length = 0
                                                                                          End Function).Single(),
                                                                          MethodSymbol)

                                                  CheckMethod(m, method, "Equals", Accessibility.Public,
                                                              retType:=GetNamedTypeSymbol(m, "System.Boolean"),
                                                              isOverrides:=True, isOverloads:=True, paramCount:=1)

                                                  Assert.Equal(GetNamedTypeSymbol(m, "System.Object"), method.Parameters(0).Type)

                                                  Assert.Equal(GetNamedTypeSymbol(m, "System.Object").
                                                                    GetMembers("Equals").Where(Function(s) Not s.IsShared).Single(),
                                                               method.OverriddenMethod)
                                              End Sub)
        End Sub

        <Fact>
        Public Sub CheckEmittedSymbol_GetHashCode()
            Dim compilationDef =
    <compilation name="CheckEmittedSymbol_GetHashCode">
        <file name="b.vb">
Module ModuleB
    Structure SSS
    End Structure 
    Sub Test1(x As Integer)
        Dim v1 As Object = New With { Key .aa = 1, .BB = "", .CCC = new SSS() }
    End Sub
End Module
        </file>
    </compilation>

            CompileAndVerify(compilationDef,
                             references:={SystemCoreRef},
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Dim type = m.ContainingAssembly.GetTypeByMetadataName("VB$AnonymousType_0`3")
                                                  Assert.NotNull(type)

                                                  Dim method = type.GetMember(Of MethodSymbol)("GetHashCode")

                                                  CheckMethod(m, method, "GetHashCode", Accessibility.Public,
                                                              retType:=GetNamedTypeSymbol(m, "System.Int32"),
                                                              isOverrides:=True, isOverloads:=True)

                                                  Assert.Equal(GetNamedTypeSymbol(m, "System.Object").GetMember("GetHashCode"), method.OverriddenMethod)
                                              End Sub)
        End Sub

        <Fact>
        Public Sub CheckEmittedSymbol_IEquatableImplementation()
            Dim compilationDef =
    <compilation name="CheckEmittedSymbol_GetHashCode">
        <file name="b.vb">
Module ModuleB
    Structure SSS
    End Structure 
    Sub Test1(x As Integer)
        Dim v1 As Object = New With { Key .aa = 1, .BB = "", .CCC = new SSS() }
    End Sub
End Module
        </file>
    </compilation>

            CompileAndVerify(compilationDef,
                             references:={SystemCoreRef},
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Dim type = m.ContainingAssembly.GetTypeByMetadataName("VB$AnonymousType_0`3")
                                                  Assert.NotNull(type)

                                                  Assert.Equal(1, type.Interfaces.Length)
                                                  Dim [interface] = type.Interfaces(0)

                                                  Assert.Equal(GetNamedTypeSymbol(m, "System.IEquatable").Construct(type), [interface])

                                                  Dim method = DirectCast(type.GetMembers("Equals").
                                                                                    Where(Function(s)
                                                                                              Return DirectCast(s, MethodSymbol).ExplicitInterfaceImplementations.Length = 1
                                                                                          End Function).Single(),
                                                                          MethodSymbol)

                                                  CheckMethod(m, method, "Equals", Accessibility.Public,
                                                              retType:=GetNamedTypeSymbol(m, "System.Boolean"), paramCount:=1,
                                                              isOverloads:=True)

                                                  Assert.Equal(type, method.Parameters(0).Type)
                                                  Assert.Equal([interface].GetMember("Equals"), method.ExplicitInterfaceImplementations(0))
                                              End Sub)
        End Sub

        <Fact>
        Public Sub NotEmittingAnonymousTypeCreatedSolelyViaSemanticAPI()
            Dim compilationDef =
    <compilation name="NotEmittingAnonymousTypeCreatedSolelyViaSemanticAPI">
        <file name="a.vb">
Module ModuleB
    Structure SSS
    End Structure 
    Sub Test1(x As Integer)
        Dim v1 As Object = New With { .aa = 1, .BB = "", .CCC = new SSS() } 
        'POSITION
    End Sub
End Module
        </file>
    </compilation>

            Dim position = compilationDef.<file>.Value.IndexOf("'POSITION", StringComparison.Ordinal)

            CompileAndVerify(compilationDef,
                             references:={SystemCoreRef},
                             sourceSymbolValidator:=Sub(m As ModuleSymbol)
                                                        Dim compilation = m.DeclaringCompilation
                                                        Dim tree = compilation.SyntaxTrees(0)

                                                        Dim model = compilation.GetSemanticModel(tree)
                                                        Dim node0 = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.AnonymousObjectCreationExpression).AsNode(), ExpressionSyntax)
                                                        Dim info0 = model.GetSemanticInfoSummary(node0)
                                                        Assert.NotNull(info0.Type)
                                                        Assert.IsType(GetType(AnonymousTypeManager.AnonymousTypePublicSymbol), info0.Type)
                                                        Assert.False(DirectCast(info0.Type, INamedTypeSymbol).IsSerializable)

                                                        Dim expr1 = SyntaxFactory.ParseExpression(<text>New With { .aa = 1, .BB = "", .CCC = new SSS() }</text>.Value)
                                                        Dim info1 = model.GetSpeculativeTypeInfo(position, expr1, SpeculativeBindingOption.BindAsExpression)
                                                        Assert.NotNull(info1.Type)
                                                        Assert.Equal(info0.Type.OriginalDefinition, info1.Type.OriginalDefinition)

                                                        Dim expr2 = SyntaxFactory.ParseExpression(<text>New With { .aa = 1, Key .BB = "", .CCC = new SSS() }</text>.Value)
                                                        Dim info2 = model.GetSpeculativeTypeInfo(position, expr2, SpeculativeBindingOption.BindAsExpression)
                                                        Assert.NotNull(info2.Type)
                                                        Assert.NotEqual(info0.Type.OriginalDefinition, info2.Type.OriginalDefinition)

                                                    End Sub,
                             symbolValidator:=Sub(m As ModuleSymbol)

                                                  Dim type = m.ContainingAssembly.GetTypeByMetadataName("VB$AnonymousType_0`3")
                                                  Assert.NotNull(type)
                                                  Assert.Equal(GetNamedTypeSymbol(m, "System.Object"), type.BaseType)
                                                  Assert.True(type.IsGenericType)
                                                  Assert.Equal(3, type.TypeParameters.Length)

                                                  ' Only one type should be emitted!!
                                                  Dim type2 = m.ContainingAssembly.GetTypeByMetadataName("VB$AnonymousType_1`3")
                                                  Assert.Null(type2)
                                              End Sub)
        End Sub

        <Fact>
        <WorkItem(641639, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/641639")>
        Public Sub Bug641639()
            Dim moduleDef =
    <compilation name="TestModule">
        <file name="a.vb">
Module ModuleB
    Sub Test1(x As Integer)
        Dim v1 As Object = New With { .aa = 1 } 
        Dim v2 As Object = Function(y as Integer) y + 1
    End Sub
End Module
        </file>
    </compilation>

            Dim testModule = CreateCompilationWithMscorlib40AndVBRuntime(moduleDef, TestOptions.ReleaseModule)

            Dim compilationDef =
    <compilation>
        <file name="a.vb">
Module ModuleA
End Module
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(moduleDef, {testModule.EmitToImageReference()}, TestOptions.ReleaseDll)

            Assert.Equal(1, compilation.Assembly.Modules(1).GlobalNamespace.GetTypeMembers("VB$AnonymousDelegate_0<TestModule>", 2).Length)
            Assert.Equal(1, compilation.Assembly.Modules(1).GlobalNamespace.GetTypeMembers("VB$AnonymousType_0<TestModule>", 1).Length)
        End Sub

#Region "Utils"

        Private Shared Sub CheckPropertyAccessorsAndField(m As ModuleSymbol, type As NamedTypeSymbol, propName As String, propType As TypeSymbol, isKey As Boolean)
            Dim prop = type.GetMembers().OfType(Of PropertySymbol)().Single()
            Assert.Equal(propType, prop.Type)
            Assert.Equal(propName, prop.Name)
            Assert.Equal(propName, prop.MetadataName)
            Assert.Equal(Accessibility.Public, prop.DeclaredAccessibility)
            Assert.False(prop.IsDefault)
            Assert.False(prop.IsMustOverride)
            Assert.False(prop.IsNotOverridable)
            Assert.False(prop.IsOverridable)
            Assert.False(prop.IsOverrides)
            Assert.False(prop.IsOverloads)
            Assert.Equal(isKey, prop.IsReadOnly)
            Assert.False(prop.IsShared)
            Assert.False(prop.IsWriteOnly)
            Assert.Equal(0, prop.Parameters.Length)
            Assert.False(prop.ShadowsExplicitly)

            Dim getter = prop.GetMethod
            CheckMethod(m, getter, "get_" & prop.Name,
                        Accessibility.Public,
                        retType:=getter.ReturnType)

            If Not isKey Then
                Dim setter = prop.SetMethod
                CheckMethod(m, setter, "set_" & prop.Name,
                            Accessibility.Public,
                            paramCount:=1,
                            isSub:=True)

                Assert.Equal(propType, setter.Parameters(0).Type)
            Else
                Assert.Null(prop.SetMethod)
            End If

            Dim field = type.GetMembers().OfType(Of FieldSymbol)().Single()
            Assert.Equal(propType, field.Type)
            Assert.Equal("$" & propName, field.Name)
            Assert.Equal("$" & propName, field.MetadataName)
            Assert.Equal(Accessibility.Private, field.DeclaredAccessibility)

            Dim parameter = type.Constructors.Single().Parameters(0)
            Assert.Equal(propType, parameter.Type)
            Assert.Equal(propName, parameter.Name)
            Assert.Equal(propName, parameter.MetadataName)
        End Sub

        Private Shared Sub CheckMethod(m As ModuleSymbol, method As MethodSymbol,
                                       name As String, accessibility As Accessibility,
                                       Optional paramCount As Integer = 0,
                                       Optional retType As TypeSymbol = Nothing,
                                       Optional isSub As Boolean = False,
                                       Optional isOverloads As Boolean = False,
                                       Optional isOverrides As Boolean = False,
                                       Optional isOverridable As Boolean = False,
                                       Optional isNotOverridable As Boolean = False)

            Assert.NotNull(method)
            Assert.Equal(name, method.Name)
            Assert.Equal(name, method.MetadataName)
            Assert.Equal(paramCount, method.ParameterCount)

            If isSub Then
                Assert.Null(retType)
                Assert.Equal(GetNamedTypeSymbol(m, "System.Void"), method.ReturnType)
                Assert.True(method.IsSub)
            End If

            If retType IsNot Nothing Then
                Assert.False(isSub)
                Assert.Equal(retType, method.ReturnType)
                Assert.False(method.IsSub)
            End If

            Assert.Equal(accessibility, method.DeclaredAccessibility)
            Assert.Equal(isOverloads, method.IsOverloads)
            Assert.Equal(isOverrides, method.IsOverrides)
            Assert.Equal(isOverridable, method.IsOverridable)
            Assert.Equal(isNotOverridable, method.IsNotOverridable)

            Assert.False(method.IsShared)
            Assert.False(method.IsMustOverride)
            Assert.False(method.IsGenericMethod)
            Assert.False(method.ShadowsExplicitly)
        End Sub

        Private Shared Function GetNamedTypeSymbol(m As ModuleSymbol, namedTypeName As String) As NamedTypeSymbol
            Dim nameParts = namedTypeName.Split("."c)

            Dim peAssembly = DirectCast(m.ContainingAssembly, PEAssemblySymbol)
            Dim nsSymbol As NamespaceSymbol = Nothing
            For Each ns In nameParts.Take(nameParts.Length - 1)
                nsSymbol = DirectCast(If(nsSymbol Is Nothing,
                                         m.ContainingAssembly.CorLibrary.GlobalNamespace.GetMember(ns),
                                         nsSymbol.GetMember(ns)), NamespaceSymbol)
            Next
            Return DirectCast(nsSymbol.GetTypeMember(nameParts(nameParts.Length - 1)), NamedTypeSymbol)
        End Function

#End Region

        <WorkItem(1319, "https://github.com/dotnet/roslyn/issues/1319")>
        <ConditionalFact(GetType(DesktopOnly), Reason:=ConditionalSkipReason.NetModulesNeedDesktop)>
        Public Sub MultipleNetmodulesWithAnonymousTypes()
            Dim compilationDef1 =
    <compilation>
        <file name="a.vb">
Class A
    Friend o1 As Object = new with { .hello = 1, .world = 2 }
    Friend d1 As Object = Function() 1
    public shared Function M1() As String
        return "Hello, "
    End Function
End Class
        </file>
    </compilation>

            Dim compilationDef2 =
    <compilation>
        <file name="a.vb">
Class B
    Inherits A

    Friend o2 As Object = new with { .hello = 1, .world = 2 }
    Friend d2 As Object = Function() 1
    public shared Function M2() As String
        return "world!"
    End Function
End Class
        </file>
    </compilation>

            Dim compilationDef3 =
    <compilation>
        <file name="a.vb">
Class Module1
    Friend o3 As Object = new with { .hello = 1, .world = 2 }
    Friend d3 As Object = Function() 1

    public shared Sub Main()
        System.Console.Write(A.M1())
        System.Console.WriteLine(B.M2())
    End Sub
End Class
        </file>
    </compilation>

            Dim comp1 = CreateCompilationWithMscorlib40(compilationDef1, options:=TestOptions.ReleaseModule.WithModuleName("A"))
            comp1.VerifyDiagnostics()
            Dim ref1 = comp1.EmitToImageReference()

            Dim comp2 = CreateCompilationWithMscorlib40AndReferences(compilationDef2, {ref1}, options:=TestOptions.ReleaseModule.WithModuleName("B"))
            comp2.VerifyDiagnostics()
            Dim ref2 = comp2.EmitToImageReference()

            Dim comp3 = CreateCompilationWithMscorlib40AndReferences(compilationDef3, {ref1, ref2}, options:=TestOptions.ReleaseExe.WithModuleName("C"))
            comp3.VerifyDiagnostics()

            Dim mA = comp3.Assembly.Modules(1)
            Assert.Equal("VB$AnonymousType_0<A>`2", mA.GlobalNamespace.GetTypeMembers().Where(Function(t) t.Name.StartsWith("VB$AnonymousType")).Single().MetadataName)
            Assert.Equal("VB$AnonymousDelegate_0<A>`1", mA.GlobalNamespace.GetTypeMembers().Where(Function(t) t.Name.StartsWith("VB$AnonymousDelegate")).Single().MetadataName)

            Dim mB = comp3.Assembly.Modules(2)
            Assert.Equal("VB$AnonymousType_0<B>`2", mB.GlobalNamespace.GetTypeMembers().Where(Function(t) t.Name.StartsWith("VB$AnonymousType")).Single().MetadataName)
            Assert.Equal("VB$AnonymousDelegate_0<B>`1", mB.GlobalNamespace.GetTypeMembers().Where(Function(t) t.Name.StartsWith("VB$AnonymousDelegate")).Single().MetadataName)

            CompileAndVerify(comp3, expectedOutput:="Hello, world!", symbolValidator:=
                             Sub(m)
                                 Dim mC = DirectCast(m, PEModuleSymbol)
                                 Assert.Equal("VB$AnonymousType_0`2", mC.GlobalNamespace.GetTypeMembers().Where(Function(t) t.Name.StartsWith("VB$AnonymousType")).Single().MetadataName)
                                 Assert.Equal("VB$AnonymousDelegate_0`1", mC.GlobalNamespace.GetTypeMembers().Where(Function(t) t.Name.StartsWith("VB$AnonymousDelegate")).Single().MetadataName)
                             End Sub)
        End Sub

    End Class

End Namespace

