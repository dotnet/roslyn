' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Basic.Reference.Assemblies
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Metadata.PE

    Public Class TypeKindTests : Inherits BasicTestBase

        <Fact>
        Public Sub Test1()
            Dim assembly = MetadataTestHelpers.LoadFromBytes(Net40.Resources.mscorlib)

            TestTypeKindHelper(assembly)
        End Sub

        Private Sub TestTypeKindHelper(assembly As AssemblySymbol)

            Dim module0 = assembly.Modules(0)

            Dim system = (From n In module0.GlobalNamespace.GetMembers()
                          Where n.Name.Equals("System")).Cast(Of NamespaceSymbol)().Single()

            Dim obj = (From t In system.GetTypeMembers()
                       Where t.Name.Equals("Object")).Single()

            Assert.Equal(TypeKind.Class, obj.TypeKind)

            Dim [enum] = (From t In system.GetTypeMembers()
                          Where t.Name.Equals("Enum")).Single()

            Assert.Equal(TypeKind.Class, [enum].TypeKind)

            Dim int32 = (From t In system.GetTypeMembers()
                         Where t.Name.Equals("Int32")).Single()

            Assert.Equal(TypeKind.Structure, int32.TypeKind)

            Dim func = (From t In system.GetTypeMembers()
                        Where t.Name.Equals("Func") AndAlso t.Arity = 1).Single()

            Assert.Equal(TypeKind.Delegate, func.TypeKind)

            Dim collections = (From n In system.GetMembers()
                               Where n.Name.Equals("Collections")).Cast(Of NamespaceSymbol)().Single()

            Dim ienumerable = (From t In collections.GetTypeMembers()
                               Where t.Name.Equals("IEnumerable")).Single()

            Assert.Equal(TypeKind.Interface, ienumerable.TypeKind)
            Assert.Null(ienumerable.BaseType)

            Dim typeCode = (From t In system.GetTypeMembers()
                            Where t.Name.Equals("TypeCode")).Single()

            Assert.Equal(TypeKind.Enum, typeCode.TypeKind)

            Assert.False(obj.IsMustInherit)
            Assert.False(obj.IsNotInheritable)
            Assert.False(obj.IsShared)

            Assert.True([enum].IsMustInherit)
            Assert.False([enum].IsNotInheritable)
            Assert.False([enum].IsShared)

            Assert.False(func.IsMustInherit)
            Assert.True(func.IsNotInheritable)
            Assert.False(func.IsShared)

            Dim console = system.GetTypeMembers("Console").Single()

            Assert.False(console.IsMustInherit)
            Assert.True(console.IsNotInheritable)
            Assert.False(console.IsShared)
        End Sub

        <Fact>
        <WorkItem(546314, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546314")>
        Public Sub Bug15562()
            Dim assembly = MetadataTestHelpers.LoadFromBytes(Net40.Resources.mscorlib)
            Dim module0 = assembly.Modules(0)
            Dim system = (From n In module0.GlobalNamespace.GetMembers()
                          Where n.Name.Equals("System")).Cast(Of NamespaceSymbol)().Single()
            Dim multicastDelegate = system.GetTypeMembers("MulticastDelegate").Single()
            Assert.Equal(TypeKind.Class, multicastDelegate.TypeKind)
        End Sub
    End Class
End Namespace

