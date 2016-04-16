' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class BaseClassTests
        Inherits BasicTestBase

        <Fact>
        Public Sub DirectCircularInheritance()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Class A
    Inherits A
End Class  
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC31447: Class 'A' cannot reference itself in Inherits clause.
    Inherits A
             ~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub InDirectCircularInheritance()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Class A
    Inherits B
End Class  
Class B
    Inherits A
End Class  
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30257: Class 'A' cannot inherit from itself: 
    'A' inherits from 'B'.
    'B' inherits from 'A'.
    Inherits B
             ~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub InDirectCircularInheritance1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Class A
    Inherits B
End Class  
Class B
    Inherits A
End Class  
Class C
    Inherits B
End Class 
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30257: Class 'A' cannot inherit from itself: 
    'A' inherits from 'B'.
    'B' inherits from 'A'.
    Inherits B
             ~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub ContainmentDependency()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Class A
    Inherits B
    Class B
    End Class
End Class  
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC31446: Class 'A' cannot reference its nested type 'A.B' in Inherits clause.
    Inherits B
             ~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub ContainmentDependency2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Class A
    Inherits B
    Class C
    End Class
End Class  
Class B
    Inherits A.C   
End Class  
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30907: This inheritance causes circular dependencies between class 'A' and its nested or base type '
    'A' inherits from 'B'.
    'B' inherits from 'A.C'.
    'A.C' is nested in 'A'.'.
    Inherits B
             ~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub ContainmentDependency3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Class A
    Inherits F 
    Class C
        class D
        end class
    End Class
End Class  
Class F
    Inherits A.C.D   
End Class  
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30907: This inheritance causes circular dependencies between class 'A' and its nested or base type '
    'A' inherits from 'F'.
    'F' inherits from 'A.C.D'.
    'A.C.D' is nested in 'A.C'.
    'A.C' is nested in 'A'.'.
    Inherits F 
             ~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub ContainmentDependency4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Class A
    Inherits B
    Class C
        Inherits F
        class D
        end class
    End Class
End Class  
Class B
    Inherits E  
End Class  
Class E
    Inherits A.C   
End Class
Class F
End Class
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30907: This inheritance causes circular dependencies between class 'A' and its nested or base type '
    'A' inherits from 'B'.
    'B' inherits from 'E'.
    'E' inherits from 'A.C'.
    'A.C' is nested in 'A'.'.
    Inherits B
             ~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub ContainmentDependency5()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Class A
    Inherits B
    Class C
        Inherits F
        class D
        end class
    End Class
End Class  
Class B
    Inherits E  
End Class  
Class E
    Inherits A.C   
End Class
Class F
    Inherits A.C.D
End Class
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30907: This inheritance causes circular dependencies between class 'A' and its nested or base type '
    'A' inherits from 'B'.
    'B' inherits from 'E'.
    'E' inherits from 'A.C'.
    'A.C' is nested in 'A'.'.
    Inherits B
             ~
BC30907: This inheritance causes circular dependencies between class 'A.C' and its nested or base type '
    'A.C' inherits from 'F'.
    'F' inherits from 'A.C.D'.
    'A.C.D' is nested in 'A.C'.'.
        Inherits F
                 ~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub InheritanceDependencyGeneric()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
namespace n1
    Class Program      
        Class A(of T)
            Inherits A(of Integer)  
        End Class  
    End Class
end namespace
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC31447: Class 'Program.A(Of T)' cannot reference itself in Inherits clause.
            Inherits A(of Integer)  
                     ~~~~~~~~~~~~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub ContainmentDependencyGeneric()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
    Class A
        Inherits C(Of String)
        Class C(Of T)
        End Class
    End Class 
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC31446: Class 'A' cannot reference its nested type 'A.C(Of String)' in Inherits clause.
        Inherits C(Of String)
                 ~~~~~~~~~~~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub ContainmentDependencyGeneric1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Class A(of T)
    Inherits B
    Class C(of U)
        class D
        end class
    End Class
End Class  
Class B
    Inherits E(of B)  
End Class  
Class E(of L)
    Inherits A(of Integer).C(of Long).D
End Class
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30907: This inheritance causes circular dependencies between class 'A(Of T)' and its nested or base type '
    'A(Of T)' inherits from 'B'.
    'B' inherits from 'E(Of B)'.
    'E(Of B)' inherits from 'A(Of Integer).C(Of Long).D'.
    'A(Of Integer).C(Of Long).D' is nested in 'A(Of T).C(Of U)'.
    'A(Of T).C(Of U)' is nested in 'A(Of T)'.'.
    Inherits B
             ~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub DirectCircularInheritanceInInterface1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Interface A
    Inherits A, A
End Interface  
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30296: Interface 'A' cannot inherit from itself: 
    'A' inherits from 'A'.
    Inherits A, A
             ~
BC30584: 'A' cannot be inherited more than once.
    Inherits A, A
                ~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub InDirectCircularInheritanceInInterface1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Interface x1
End interface
Interface x2
End interface        
Interface A
    Inherits B
End interface        
Interface B
    Inherits x1, B, x2
End Interface  
    </file>
</compilation>)

            Dim expectedErrors = <errors>
                                     BC30296: Interface 'B' cannot inherit from itself: 
    'B' inherits from 'B'.
    Inherits x1, B, x2
                 ~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub InterfaceContainmentDependency()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Interface A
    Inherits B
    Interface B
    End Interface
End Interface  
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30908: interface 'A' cannot inherit from a type nested within it.
    Inherits B
             ~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub InterfaceContainmentDependency2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Interface A
    Inherits B
    Interface C
    End Interface
End Interface  
Interface B
    Inherits A.C   
End Interface  
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30907: This inheritance causes circular dependencies between interface 'A' and its nested or base type '
    'A' inherits from 'B'.
    'B' inherits from 'A.C'.
    'A.C' is nested in 'A'.'.
    Inherits B
             ~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub InterfaceContainmentDependency4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Interface A
    Inherits B
    class C
        Interface D
            Inherits F
            interface E
            end interface
        End Interface
    end class
End Interface  

Interface B
    Inherits E  
End Interface  
Interface E
    Inherits A.C.D.E   
End Interface
Interface F
End Interface
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30907: This inheritance causes circular dependencies between interface 'A' and its nested or base type '
    'A' inherits from 'B'.
    'B' inherits from 'E'.
    'E' inherits from 'A.C.D.E'.
    'A.C.D.E' is nested in 'A.C.D'.
    'A.C.D' is nested in 'A.C'.
    'A.C' is nested in 'A'.'.
    Inherits B
             ~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub InterfaceImplementingDependency5()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
        Class cls1
            inherits s1.cls2
            implements s1.cls2.i2

            interface i1

            end interface
        End Class

        structure s1
            implements cls1.i1
            Class cls2
                Interface i2
                End Interface
            End Class
        end structure
            </file>
</compilation>)

            '    ' this would be an error if implementing was a dependency
            '    Dim expectedErrors = <errors>
            'BC30907: This inheritance causes circular dependencies between structure 's1' and its nested or base type '
            '    's1' implements 'cls1.i1'.
            '    'cls1.i1' is nested in 'cls1'.
            '    'cls1' inherits from 's1.cls2'.
            '    's1.cls2' is nested in 's1'.'.
            '    implements cls1.i1
            '               ~~~~~~~
            '                                 </errors>

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <WorkItem(850140, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850140")>
        <Fact>
        Public Sub InterfaceCycleBug850140()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
        Interface B(Of S)    
        	Interface C(Of U)        
        		Inherits B(Of C(Of C(Of U)).
            End Interface
        End Interface
                    </file>
</compilation>)

            Dim expectedErrors = <errors>
  BC30002: Type 'C.' is not defined.
        		Inherits B(Of C(Of C(Of U)).
                        ~~~~~~~~~~~~~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <WorkItem(850140, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850140")>
        <Fact>
        Public Sub InterfaceCycleBug850140_a()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
        Interface B(Of S)    
        	Interface C(Of U)        
        		Inherits B(Of C(Of C(Of U)).C(Of U)
            End Interface
        End Interface
                    </file>
</compilation>)

            Dim expectedErrors = <errors>
  BC30002: Type 'C.C' is not defined.
        		Inherits B(Of C(Of C(Of U)).C(Of U)
                        ~~~~~~~~~~~~~~~~~~~~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <WorkItem(850140, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850140")>
        <Fact>
        Public Sub InterfaceCycleBug850140_b()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
        Interface B(Of S)    
        	Interface C(Of U)        
        		Inherits B(Of C(Of C(Of U))).C(Of U)
            End Interface
        End Interface
                    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30296: Interface 'B(Of S).C(Of U)' cannot inherit from itself: 
    'B(Of S).C(Of U)' inherits from 'B(Of S).C(Of U)'.
        		Inherits B(Of C(Of C(Of U))).C(Of U)
                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub ClassCycleBug850140()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
        Class B(Of S)    
        	Class C(Of U)        
        		Inherits B(Of C(Of C(Of U)).
            End Class
        End Class
                    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30002: Type 'C.' is not defined.
        		Inherits B(Of C(Of C(Of U)).
                        ~~~~~~~~~~~~~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub ClassCycleBug850140_a()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
        Class B(Of S)    
        	Class C(Of U)        
        		Inherits B(Of C(Of C(Of U)).C(Of U)
            End Class
        End Class
                    </file>
</compilation>)

            Dim expectedErrors = <errors>
  BC30002: Type 'C.C' is not defined.
        		Inherits B(Of C(Of C(Of U)).C(Of U)
                        ~~~~~~~~~~~~~~~~~~~~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub ClassCycleBug850140_b()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
        Class B(Of S)    
        	Class C(Of U)        
        		Inherits B(Of C(Of C(Of U))).C(Of U)
            End Class
        End Class
                    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC31447: Class 'B(Of S).C(Of U)' cannot reference itself in Inherits clause.
        		Inherits B(Of C(Of C(Of U))).C(Of U)
                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub


        <Fact>
        Public Sub InterfaceClassMutualContainment()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Class cls1
    Inherits i1.cls2
    Interface i2
    End Interface
End Class

Interface i1
    Inherits cls1.i2
    Class cls2
    End Class
End Interface
            </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30907: This inheritance causes circular dependencies between class 'cls1' and its nested or base type '
    'cls1' inherits from 'i1.cls2'.
    'i1.cls2' is nested in 'i1'.
    'i1' inherits from 'cls1.i2'.
    'cls1.i2' is nested in 'cls1'.'.
    Inherits i1.cls2
             ~~~~~~~
                                             </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub InterfaceClassMutualContainmentGeneric()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Class cls1(of T)
    Inherits i1(of Integer).cls2
    Interface i2
    End Interface
End Class

Interface i1(of T)
    Inherits cls1(of T).i2
    Class cls2
    End Class
End Interface
            </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30907: This inheritance causes circular dependencies between class 'cls1(Of T)' and its nested or base type '
    'cls1(Of T)' inherits from 'i1(Of Integer).cls2'.
    'i1(Of Integer).cls2' is nested in 'i1(Of T)'.
    'i1(Of T)' inherits from 'cls1(Of T).i2'.
    'cls1(Of T).i2' is nested in 'cls1(Of T)'.'.
    Inherits i1(of Integer).cls2
             ~~~~~~~~~~~~~~~~~~~
                                             </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub ImportedCycle1()
            Dim C1 = TestReferences.SymbolsTests.CyclicInheritance.Class1
            Dim C2 = TestReferences.SymbolsTests.CyclicInheritance.Class2

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(
<compilation name="Compilation">
    <file name="a.vb">
Class C3
    Inherits C1
End Class 
    </file>
</compilation>,
{TestReferences.NetFx.v4_0_30319.mscorlib, C1, C2})

            Dim expectedErrors = <errors>
BC30916: Type 'C1' is not supported because it either directly or indirectly inherits from itself.
    Inherits C1
             ~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub ImportedCycle2()
            Dim C1 = TestReferences.SymbolsTests.CyclicInheritance.Class1
            Dim C2 = TestReferences.SymbolsTests.CyclicInheritance.Class2
            Dim C3 = TestReferences.SymbolsTests.CyclicInheritance.Class3


            Dim compilation = CompilationUtils.CreateCompilationWithReferences(
<compilation name="Compilation">
    <file name="a.vb">
            Class C3
                Inherits C1
            End Class 
                </file>
</compilation>,
{TestReferences.NetFx.v4_0_30319.mscorlib, C1, C2})

            Dim expectedErrors = <errors>
            BC30916: Type 'C1' is not supported because it either directly or indirectly inherits from itself.
                Inherits C1
                         ~~
                                             </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)

            compilation = CompilationUtils.CreateCompilationWithReferences(
<compilation name="Compilation">
    <file name="a.vb">
Class C4
    Inherits C3
End Class 
    </file>
</compilation>,
{TestReferences.NetFx.v4_0_30319.mscorlib, C1, C2, C3})

            expectedErrors = <errors>
BC30916: Type 'C3' is not supported because it either directly or indirectly inherits from itself.
    Inherits C3
             ~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub


        <Fact>
        Public Sub CyclicInterfaces3()
            Dim C1 = TestReferences.SymbolsTests.CyclicInheritance.Class1
            Dim C2 = TestReferences.SymbolsTests.CyclicInheritance.Class2

            Dim Comp = CompilationUtils.CreateCompilationWithReferences(
<compilation name="Compilation">
    <file name="a.vb">
Interface I4
    Inherits I1

End Interface
    </file>
</compilation>,
{TestReferences.NetFx.v4_0_30319.mscorlib, C1, C2})

            Dim expectedErrors = <errors>
BC30916: Type 'I1' is not supported because it either directly or indirectly inherits from itself.
    Inherits I1
             ~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(Comp, expectedErrors)

        End Sub


#If Retargeting Then
        <Fact(skip:=SkipReason.AlreadyTestingRetargeting)>
        Public Sub CyclicRetargeted4()
#Else
        <Fact>
        Public Sub CyclicRetargeted4()
#End If
            Dim ClassAv1 = TestReferences.SymbolsTests.RetargetingCycle.V1.ClassA.dll


            Dim Comp = CompilationUtils.CreateCompilationWithReferences(
<compilation name="ClassB">
    <file name="B.vb">
Public Class ClassB
    Inherits ClassA

End Class
    </file>
</compilation>,
{TestReferences.NetFx.v4_0_30319.mscorlib, ClassAv1})

            Dim global1 = Comp.GlobalNamespace
            Dim B1 = global1.GetTypeMembers("ClassB", 0).Single()
            Dim A1 = global1.GetTypeMembers("ClassA", 0).Single()
            Dim B_base = B1.BaseType
            Dim A_base = A1.BaseType
            Assert.IsAssignableFrom(Of PENamedTypeSymbol)(B_base)
            Assert.IsAssignableFrom(Of PENamedTypeSymbol)(A_base)

            Dim ClassAv2 = TestReferences.SymbolsTests.RetargetingCycle.V2.ClassA.dll
            Dim Comp2 = CompilationUtils.CreateCompilationWithReferences(
<compilation name="ClassB1">
    <file name="B.vb">
Public Class ClassC
    Inherits ClassB

End Class
    </file>
</compilation>,
New MetadataReference() {TestReferences.NetFx.v4_0_30319.mscorlib, ClassAv2, New VisualBasicCompilationReference(Comp)})


            Dim [global] = Comp2.GlobalNamespace
            Dim B2 = [global].GetTypeMembers("ClassB", 0).Single()
            Dim C = [global].GetTypeMembers("ClassC", 0).Single()
            Assert.IsType(Of Retargeting.RetargetingNamedTypeSymbol)(B2)
            Assert.Same(B1, (DirectCast(B2, Retargeting.RetargetingNamedTypeSymbol)).UnderlyingNamedType)
            Assert.Same(C.BaseType, B2)

            Dim expectedErrors = <errors>
BC30916: Type 'ClassB' is not supported because it either directly or indirectly inherits from itself.
    Inherits ClassB
             ~~~~~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(Comp2, expectedErrors)

            Dim A2 = [global].GetTypeMembers("ClassA", 0).Single()
            Dim errorBase1 = TryCast(A2.BaseType, ErrorTypeSymbol)
            Dim er = errorBase1.ErrorInfo
            Assert.Equal("error BC30916: Type 'ClassA' is not supported because it either directly or indirectly inherits from itself.", er.ToString(EnsureEnglishUICulture.PreferredOrNull))

        End Sub


        <Fact>
        Public Sub CyclicRetargeted5()
            Dim ClassAv1 = TestReferences.SymbolsTests.RetargetingCycle.V1.ClassA.dll
            Dim ClassBv1 = TestReferences.SymbolsTests.RetargetingCycle.V1.ClassB.netmodule

            Dim Comp = CompilationUtils.CreateCompilationWithReferences(
<compilation name="ClassB">
    <file name="B.vb">
'hi
    </file>
</compilation>,
            {
                TestReferences.NetFx.v4_0_30319.mscorlib,
                ClassAv1,
                ClassBv1
            })

            Dim global1 = Comp.GlobalNamespace
            Dim B1 = global1.GetTypeMembers("ClassB", 0).[Distinct]().Single()
            Dim A1 = global1.GetTypeMembers("ClassA", 0).Single()
            Dim B_base = B1.BaseType
            Dim A_base = A1.BaseType
            Assert.IsAssignableFrom(Of PENamedTypeSymbol)(B1)
            Assert.IsAssignableFrom(Of PENamedTypeSymbol)(B_base)
            Assert.IsAssignableFrom(Of PENamedTypeSymbol)(A_base)

            Dim ClassAv2 = TestReferences.SymbolsTests.RetargetingCycle.V2.ClassA.dll
            Dim Comp2 = CompilationUtils.CreateCompilationWithReferences(
<compilation name="ClassB1">
    <file name="B.vb">
Public Class ClassC
    Inherits ClassB

End Class
    </file>
</compilation>,
New MetadataReference() {TestReferences.NetFx.v4_0_30319.mscorlib, ClassAv2, New VisualBasicCompilationReference(Comp)})

            Dim [global] = Comp2.GlobalNamespace
            Dim B2 = [global].GetTypeMembers("ClassB", 0).Single()
            Dim C = [global].GetTypeMembers("ClassC", 0).Single()
            Assert.IsAssignableFrom(Of PENamedTypeSymbol)(B2)
            Assert.NotEqual(B1, B2)
            Assert.Same((DirectCast(B1.ContainingModule, PEModuleSymbol)).Module, DirectCast(B2.ContainingModule, PEModuleSymbol).Module)
            Assert.Equal(DirectCast(B1, PENamedTypeSymbol).Handle, DirectCast(B2, PENamedTypeSymbol).Handle)
            Assert.Same(C.BaseType, B2)
            Dim expectedErrors = <errors>
BC30916: Type 'ClassB' is not supported because it either directly or indirectly inherits from itself.
    Inherits ClassB
             ~~~~~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(Comp2, expectedErrors)

            Dim A2 = [global].GetTypeMembers("ClassA", 0).Single()
            Dim errorBase1 = TryCast(A2.BaseType, ErrorTypeSymbol)
            Dim er = errorBase1.ErrorInfo
            Assert.Equal("error BC30916: Type 'ClassA' is not supported because it either directly or indirectly inherits from itself.", er.ToString(EnsureEnglishUICulture.PreferredOrNull))
        End Sub


#If retargeting Then
        <Fact(skip:="Already using Feature")> 
           Public Sub CyclicRetargeted6()
#Else
        <Fact>
        Public Sub CyclicRetargeted6()
#End If
            Dim ClassAv2 = TestReferences.SymbolsTests.RetargetingCycle.V2.ClassA.dll

            Dim Comp = CompilationUtils.CreateCompilationWithReferences(
<compilation name="ClassB">
    <file name="B.vb">
Public Class ClassB
    Inherits ClassA

End Class
    </file>
</compilation>,
{TestReferences.NetFx.v4_0_30319.mscorlib, ClassAv2})


            Dim global1 = Comp.GlobalNamespace
            Dim B1 = global1.GetTypeMembers("ClassB", 0).Single()
            Dim A1 = global1.GetTypeMembers("ClassA", 0).Single()
            Dim B_base = B1.BaseType
            Dim A_base = A1.BaseType

            Dim expectedErrors = <errors>
BC30257: Class 'ClassB' cannot inherit from itself: 
    'ClassB' inherits from 'ClassA'.
    'ClassA' inherits from 'ClassB'.
    Inherits ClassA
             ~~~~~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(Comp, expectedErrors)

            Dim errorBase1 = TryCast(A_base, ErrorTypeSymbol)
            Dim er = errorBase1.ErrorInfo
            Assert.Equal("error BC30916: Type 'ClassA' is not supported because it either directly or indirectly inherits from itself.", er.ToString(EnsureEnglishUICulture.PreferredOrNull))

            Dim ClassAv1 = TestReferences.SymbolsTests.RetargetingCycle.V1.ClassA.dll
            Dim Comp2 = CompilationUtils.CreateCompilationWithReferences(
<compilation name="ClassB1">
    <file name="B.vb">
Public Class ClassC
    Inherits ClassB

End Class
    </file>
</compilation>,
New MetadataReference() {TestReferences.NetFx.v4_0_30319.mscorlib, ClassAv1, New VisualBasicCompilationReference(Comp)})

            Dim [global] = Comp2.GlobalNamespace
            Dim A2 = [global].GetTypeMembers("ClassA", 0).Single()
            Dim B2 = [global].GetTypeMembers("ClassB", 0).Single()
            Dim C = [global].GetTypeMembers("ClassC", 0).Single()

            Assert.Same(B1, (DirectCast(B2, Retargeting.RetargetingNamedTypeSymbol)).UnderlyingNamedType)
            Assert.Same(C.BaseType, B2)
            Assert.Same(B2.BaseType, A2)
        End Sub

#If retargeting Then
        <Fact(skip:="Already using Feature")> 
            Public Sub CyclicRetargeted7()
#Else
        <Fact>
        Public Sub CyclicRetargeted7()
#End If
            Dim ClassAv2 = TestReferences.SymbolsTests.RetargetingCycle.V2.ClassA.dll
            Dim ClassBv1 = TestReferences.SymbolsTests.RetargetingCycle.V1.ClassB.netmodule

            Dim Comp = CompilationUtils.CreateCompilationWithReferences(
<compilation name="ClassB">
    <file name="B.vb">
'hi
    </file>
</compilation>,
            {
                TestReferences.NetFx.v4_0_30319.mscorlib,
                ClassAv2,
                ClassBv1
            })

            Dim global1 = Comp.GlobalNamespace
            Dim B1 = global1.GetTypeMembers("ClassB", 0).[Distinct]().Single()
            Dim A1 = global1.GetTypeMembers("ClassA", 0).Single()
            Dim B_base = B1.BaseType
            Dim A_base = A1.BaseType

            Assert.IsType(Of PENamedTypeSymbol)(B1)
            Dim errorBase = TryCast(B_base, ErrorTypeSymbol)
            Dim er = errorBase.ErrorInfo
            Assert.Equal("error BC30916: Type 'ClassB' is not supported because it either directly or indirectly inherits from itself.", er.ToString(EnsureEnglishUICulture.PreferredOrNull))

            Dim errorBase1 = TryCast(A_base, ErrorTypeSymbol)
            er = errorBase1.ErrorInfo
            Assert.Equal("error BC30916: Type 'ClassA' is not supported because it either directly or indirectly inherits from itself.", er.ToString(EnsureEnglishUICulture.PreferredOrNull))


            Dim ClassAv1 = TestReferences.SymbolsTests.RetargetingCycle.V1.ClassA.dll

            Dim Comp2 = CompilationUtils.CreateCompilationWithReferences(
<compilation name="ClassB1">
    <file name="B.vb">
Public Class ClassC
    Inherits ClassB

End Class
    </file>
</compilation>,
            {
                TestReferences.NetFx.v4_0_30319.mscorlib,
                ClassAv1,
                New VisualBasicCompilationReference(Comp)
            })

            Dim [global] = Comp2.GlobalNamespace
            Dim B2 = [global].GetTypeMembers("ClassB", 0).Single()
            Dim C = [global].GetTypeMembers("ClassC", 0).Single()
            Assert.IsType(Of PENamedTypeSymbol)(B2)
            Assert.NotEqual(B1, B2)
            Assert.Same(DirectCast(B1.ContainingModule, PEModuleSymbol).Module, DirectCast(B2.ContainingModule, PEModuleSymbol).Module)
            Assert.Equal(DirectCast(B1, PENamedTypeSymbol).Handle, DirectCast(B2, PENamedTypeSymbol).Handle)
            Assert.Same(C.BaseType, B2)
            Dim A2 = [global].GetTypeMembers("ClassA", 0).Single()
            Assert.IsAssignableFrom(Of PENamedTypeSymbol)(A2.BaseType)
            Assert.IsAssignableFrom(Of PENamedTypeSymbol)(B2.BaseType)
        End Sub

#If retargeting Then
        <Fact(skip:="Already using Feature")> 
           Public Sub CyclicRetargeted8()
#Else
        <Fact>
        Public Sub CyclicRetargeted8()
#End If
            Dim ClassAv2 = TestReferences.SymbolsTests.RetargetingCycle.V2.ClassA.dll

            Dim Comp = CompilationUtils.CreateCompilationWithReferences(
    <compilation name="ClassB">
        <file name="B.vb">
Public Class ClassB
    Inherits ClassA

End Class
    </file>
    </compilation>,
    {TestReferences.NetFx.v4_0_30319.mscorlib, ClassAv2})


            Dim global1 = Comp.GlobalNamespace
            Dim B1 = global1.GetTypeMembers("ClassB", 0).Single()
            Dim A1 = global1.GetTypeMembers("ClassA", 0).Single()
            Dim B_base = B1.BaseType
            Dim A_base = A1.BaseType

            Dim expectedErrors = <errors>
BC30257: Class 'ClassB' cannot inherit from itself: 
    'ClassB' inherits from 'ClassA'.
    'ClassA' inherits from 'ClassB'.
    Inherits ClassA
             ~~~~~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(Comp, expectedErrors)

            Dim errorBase1 = TryCast(A_base, ErrorTypeSymbol)
            Dim er = errorBase1.ErrorInfo
            Assert.Equal("error BC30916: Type 'ClassA' is not supported because it either directly or indirectly inherits from itself.", er.ToString(EnsureEnglishUICulture.PreferredOrNull))

            Dim ClassAv1 = TestReferences.SymbolsTests.RetargetingCycle.V1.ClassA.dll
            Dim Comp2 = CompilationUtils.CreateCompilationWithReferences(
    <compilation name="ClassB1">
        <file name="B.vb">
Public Class ClassC
    Inherits ClassB

End Class
    </file>
    </compilation>,
    New MetadataReference() {TestReferences.NetFx.v4_0_30319.mscorlib, ClassAv1, New VisualBasicCompilationReference(Comp)})

            Dim [global] = Comp2.GlobalNamespace
            Dim A2 = [global].GetTypeMembers("ClassA", 0).Single()
            Dim B2 = [global].GetTypeMembers("ClassB", 0).Single()
            Dim C = [global].GetTypeMembers("ClassC", 0).Single()

            Assert.Same(B1, (DirectCast(B2, Retargeting.RetargetingNamedTypeSymbol)).UnderlyingNamedType)
            Assert.Same(C.BaseType, B2)
            Assert.Same(B2.BaseType, A2)
        End Sub


        <WorkItem(538503, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538503")>
        <Fact>
        Public Sub TypeFromBaseInterface()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Interface IA
 Class B
 End Class
End Interface

Interface IC
 Inherits IA
 Class D
  Inherits B
 End Class
End Interface
    </file>
</compilation>)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <WorkItem(538500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538500")>
        <Fact>
        Public Sub TypeThroughBaseInterface()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Interface IA
  Class C
  End Class
End Interface

Interface IB
 Inherits IA
End Interface

Class D
 Inherits IB.C
End Class
    </file>
</compilation>)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        '
        '  .{C1} .{C1}
        '  |    /
        '  .   /
        '   \ /
        '    .
        '    |
        '    .
        <Fact>
        Public Sub TypeFromBaseInterfaceAmbiguous()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Interface IA
    Class C
    End Class
End Interface

Interface IADerived
    Inherits IA
End Interface

Interface IA1Base
    Class C
    End Class
End Interface

Interface IA1
    inherits IA1Base, IADerived
End Interface

Interface IB
    Inherits IA1
    Class D
        Inherits C
    End Class
End Interface
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30685: 'C' is ambiguous across the inherited interfaces 'IA1Base' and 'IA'.
        Inherits C
                 ~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub TypeFromInterfaceDiamondInheritance()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Interface IA
    Class C
    End Class
End Interface

Interface IA1
    Inherits IA
End Interface

Interface IA2
    inherits IA, IA1
End Interface

Interface IA3
    inherits IA, IA1, IA2
End Interface

Interface IA4
    inherits IA, IA1, IA2, IA3
End Interface

Interface IB
    Inherits IA4, IA3, IA2, IA1, IA
    Class D
        Inherits C
    End Class
End Interface
    </file>
</compilation>)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub


        '
        ' .{C1} .{C1}
        '  \   /
        '    . {C1}
        '    |
        '    .
        '
        <Fact>
        Public Sub TypeFromInterfaceYInheritanceShadow()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Interface I0
    Shadows Class C1

    End Class
End Interface


Interface IA
    Shadows Class C1

    End Class
End Interface

Interface IA1
    Inherits IA, I0
    Shadows Class C1

    End Class
End Interface


Interface IB
    Inherits IA, IA1
    Class D
        Inherits C1
    End Class
End Interface
    </file>
</compilation>)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        '
        ' .{C1} .{C1}
        '  \   /
        '    . 
        '    |
        '    .
        '
        <Fact>
        Public Sub TypeFromInterfaceYInheritanceNoShadow()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Interface I0
    Shadows Class C1

    End Class
End Interface


Interface IA
    Shadows Class C1

    End Class
End Interface

Interface IA1
    Inherits IA, I0

End Interface


Interface IB
    Inherits IA, IA1
    Class D
        Inherits C1
    End Class
End Interface
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30685: 'C1' is ambiguous across the inherited interfaces 'IA' and 'I0'.
        Inherits C1
                 ~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub


        '
        ' .{C1}    .
        '  \      /
        '   . <- .{C1}
        '    \  /
        '      .
        '
        <Fact>
        Public Sub TypeFromInterfaceAInheritanceShadow()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Interface I0
    Shadows Class C1

    End Class
End Interface

Interface I1
End Interface

Interface IA
    Inherits I0
End Interface

Interface IA1
    Inherits IA, I1
    Shadows Class C1
    End Class
End Interface


Interface IB
    Inherits IA, IA1
    Class D
        Inherits C1
    End Class
End Interface
    </file>
</compilation>)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        '
        ' .{C1}    .{C1}
        '  \      /
        '   . <- .{C1}
        '    \  /
        '      .
        '
        <Fact>
        Public Sub TypeFromInterfaceAInheritanceShadow1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Interface I0
    Shadows Class C1

    End Class
End Interface

Interface I1
    Shadows Class C1

    End Class
End Interface

Interface IA
    Inherits I0
End Interface

Interface IA1
    Inherits IA, I1
    Shadows Class C1
    End Class
End Interface


Interface IB
    Inherits IA, IA1
    Class D
        Inherits C1
    End Class
End Interface
    </file>
</compilation>)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        '
        ' .{C1}    .{C1}
        '  \      /     \
        '   . <- .{C1}   .
        '    \  /       /
        '      . _ _ _ /
        '
        <Fact>
        Public Sub TypeFromInterfaceAInheritanceShadow2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Interface I0
    Shadows Class C1

    End Class
End Interface

Interface I1
    Shadows Class C1

    End Class
End Interface

Interface IA
    Inherits I0
End Interface

Interface IA1
    Inherits IA, I1
    Shadows Class C1
    End Class
End Interface

Interface IA2
    Inherits I1
End Interface

Interface IB
    Inherits IA, IA1, IA2
    Class D
        Inherits C1
    End Class
End Interface
    </file>
</compilation>)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        '
        ' .{C1}    .{C1}
        '  \      /     \
        '   . <- .       .
        '    \  /       /
        '      . _ _ _ /
        '
        <Fact>
        Public Sub TypeFromInterfaceAInheritanceNoShadow()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Interface I0
    Shadows Class C1

    End Class
End Interface

Interface I1
    Shadows Class C1

    End Class
End Interface

Interface IA
    Inherits I0
End Interface

Interface IA1
    Inherits IA, I1
End Interface

Interface IA2
    Inherits I1
End Interface

Interface IB
    Inherits IA, IA1, IA2
    Class D
        Inherits C1
    End Class
End Interface
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30685: 'C1' is ambiguous across the inherited interfaces 'I0' and 'I1'.
        Inherits C1
                 ~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub TypeFromInterfaceCycles()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
interface ii1 : inherits ii1,ii2
   interface ii2 : inherits ii1
   end interface
end interface
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30296: Interface 'ii1' cannot inherit from itself: 
    'ii1' inherits from 'ii1'.
interface ii1 : inherits ii1,ii2
                         ~~~
BC30908: interface 'ii1' cannot inherit from a type nested within it.
interface ii1 : inherits ii1,ii2
                             ~~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub TypeFromInterfaceCantShadowAmbiguity()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Interface Base1
    Shadows Class c1

    End Class
End Interface

Interface Base2
    Shadows Class c1

    End Class
End Interface

Interface DerivedWithAmbiguity
    Inherits Base1, Base2
End Interface

Interface DerivedWithoutAmbiguity
    Inherits Base1
End Interface

Interface Foo
    Inherits DerivedWithAmbiguity, DerivedWithoutAmbiguity
End Interface

Class Test
    Dim x as Foo.c1 = Nothing

End Class
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30685: 'c1' is ambiguous across the inherited interfaces 'Base1' and 'Base2'.
    Dim x as Foo.c1 = Nothing
             ~~~~~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub TypeFromInterfaceCantShadowAmbiguity1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Interface Base1
    Shadows Class c1

    End Class
End Interface

Interface Base2
    Shadows Class c1

    End Class
End Interface

Interface DerivedWithAmbiguity
    Inherits Base1, Base2
End Interface

Interface Base3
    Inherits Base1, Base2
    Shadows Class c1

    End Class
End Interface

Interface DerivedWithoutAmbiguity
    Inherits Base3
End Interface

Interface Foo
    Inherits DerivedWithAmbiguity, DerivedWithoutAmbiguity
End Interface

Class Test
    Inherits Foo.c1

End Class
    </file>
</compilation>)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub


        <Fact>
        Public Sub TypeFromInterfaceUnreachableAmbiguityIsOk()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Interface Base1
    Shadows Class c1

    End Class
End Interface

Interface Base2
    Shadows Class c1

    End Class
End Interface

Interface DerivedWithAmbiguity
    Inherits Base1, Base2
End Interface

Interface DerivedWithoutAmbiguity
    Inherits Base1
End Interface

Interface Foo
    Inherits DerivedWithAmbiguity, DerivedWithoutAmbiguity
    Shadows Class c1

    End Class
End Interface

Class Test
    Dim x as Foo.c1 = Nothing

End Class
    </file>
</compilation>)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact>
        Public Sub AccessibilityCheckInInherits1()

            Dim compilationDef =
<compilation name="AccessibilityCheckInInherits1">
    <file name="a.vb">
Public Class C(Of T)
    Protected Class A
    End Class
End Class
Public Class E
    Protected Class D
        Inherits C(Of D)
        Public Class B
            Inherits A
        End Class
    End Class
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30509: 'B' cannot inherit from class 'C(Of E.D).A' because it expands the access of the base class to class 'E'.
            Inherits A
                     ~
</expected>)

        End Sub

        <Fact>
        Public Sub AccessibilityCheckInInherits2()

            Dim compilationDef =
<compilation name="AccessibilityCheckInInherits2">
    <file name="a.vb">
Public Class C(Of T)
End Class
Public Class E
    Inherits C(Of P)
    Private Class P
    End Class
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
  BC30921: 'E' cannot inherit from class 'C(Of E.P)' because it expands the access of type 'E.P' to namespace '&lt;Default&gt;'.
    Inherits C(Of P)
             ~~~~~~~
</expected>)

        End Sub

        <WorkItem(538878, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538878")>
        <Fact>
        Public Sub ProtectedNestedBase()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Class A
  Class B
  End Class
End Class
 
Class D
  Inherits A
  Protected Class B
  End Class
End Class
 
Class E
  Inherits D
  Protected Class F
    Inherits B
  End Class
End Class
    </file>
</compilation>)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <WorkItem(537949, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537949")>
        <Fact>
        Public Sub ImplementingNestedInherited()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Interface I(Of T)
End Interface
Class A(Of T)
  Class B
    Inherits A(Of B)
    Implements I(Of B.B)
  End Class
End Class
    </file>
</compilation>)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <WorkItem(538509, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538509")>
        <Fact>
        Public Sub ImplementingNestedInherited1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Class B
Class C
End Class
End Class
Class A(Of T)
Inherits B
Implements I(Of C)
End Class
Interface I(Of T)
End Interface
Class C
Sub Main()
End Sub
End Class
    </file>
</compilation>)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <WorkItem(538811, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538811")>
        <Fact>
        Public Sub OverloadedViaInterfaceInheritance()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Compilation">
    <file name="a.vb">
Interface IA(Of T)
    Function Foo() As T
End Interface

Interface IB
    Inherits IA(Of Integer)
    Overloads Sub Foo(ByVal x As Integer)
End Interface

Interface IC
    Inherits IB, IA(Of String)
End Interface

Module M
    Sub Main()
        Dim x As IC = Nothing
        Dim s As Integer = x.Foo()
    End Sub
End Module
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30521: Overload resolution failed because no accessible 'Foo' is most specific for these arguments:
    'Function IA(Of String).Foo() As String': Not most specific.
    'Function IA(Of Integer).Foo() As Integer': Not most specific.
        Dim s As Integer = x.Foo()
                             ~~~
                                 </errors>

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub OverloadedViaInterfaceInheritance1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">

Interface IA(Of T)
    Function Foo() As T
End Interface

Interface IB
    Class Foo
    End Class
End Interface

Interface IC
    Inherits IB, IA(Of String)
End Interface

Class M
    Sub Main()
        Dim x As IC
        Dim s As String = x.Foo().ToLower()
    End Sub
End Class
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC42104: Variable 'x' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim s As String = x.Foo().ToLower()
                          ~
BC30685: 'Foo' is ambiguous across the inherited interfaces 'IB' and 'IA(Of String)'.
        Dim s As String = x.Foo().ToLower()
                          ~~~~~
              </errors>

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)
        End Sub

        <WorkItem(539775, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539775")>
        <Fact>
        Public Sub AmbiguousNestedInterfaceInheritedFromMultipleGenericInstantiations()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
        Interface A(Of T)
            Interface B
                Inherits A(Of B), A(Of B())

                Interface C
                    Inherits B
                End Interface
            End Interface
        End Interface
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30685: 'B' is ambiguous across the inherited interfaces 'A(Of A(Of T).B)' and 'A(Of A(Of T).B())'.
                    Inherits B
                             ~
                                 </errors>

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)
        End Sub

        <WorkItem(538809, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538809")>
        <Fact>
        Public Sub Bug4532()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Interface IA(Of T)
  Overloads Sub Foo(ByVal x As T)
End Interface
 
Interface IC
  Inherits IA(Of Date), IA(Of Integer)
  Overloads Sub Foo()
End Interface
 
Class M
  Sub Main()
    Dim c As IC = Nothing
    c.Foo()
  End Sub
End Class
    </file>
</compilation>)

            Dim expectedErrors = <errors>
                                 </errors>

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub CircularInheritanceThroughSubstitution()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Class A(Of T)
  Class B
    Inherits A(Of E)
  End Class
  Class E
    Inherits B.E.B
  End Class
End Class
    </file>
</compilation>)

            Dim expectedErrors =
<errors>
BC31447: Class 'A(Of T).E' cannot reference itself in Inherits clause.
    Inherits B.E.B
             ~~~
</errors>

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)
        End Sub

        ''' <summary>
        ''' The base type of a nested type should not change depending on
        ''' whether or not the base type of the containing type has been
        ''' evaluated.
        ''' </summary>
        <WorkItem(539744, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539744")>
        <Fact>
        Public Sub BaseTypeEvaluationOrder()
            Dim text =
                <compilation name="Compilation">
                    <file name="a.vb">
Class A(Of T)
    Public Class X
    End Class
End Class
Class B
    Inherits A(Of B.Y.Error)
    Public Class Y
        Inherits X
    End Class
End Class
                    </file>
                </compilation>

            If True Then

                Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(text)

                Dim classB = CType(compilation.GlobalNamespace.GetMembers("B").Single(), NamedTypeSymbol)
                Dim classY = CType(classB.GetMembers("Y").Single(), NamedTypeSymbol)

                Dim baseB = classB.BaseType
                Assert.Equal("?", baseB.ToDisplayString())
                Assert.True(baseB.IsErrorType())

                Dim baseY = classY.BaseType
                Assert.Equal("X", baseY.ToDisplayString())
                Assert.True(baseY.IsErrorType())

            End If

            If True Then

                Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(text)

                Dim classB = CType(compilation.GlobalNamespace.GetMembers("B").Single(), NamedTypeSymbol)
                Dim classY = CType(classB.GetMembers("Y").Single(), NamedTypeSymbol)

                Dim baseY = classY.BaseType
                Assert.Equal("X", baseY.ToDisplayString())
                Assert.True(baseY.IsErrorType())

                Dim baseB = classB.BaseType
                Assert.Equal("?", baseB.ToDisplayString())
                Assert.True(baseB.IsErrorType())

            End If

        End Sub

        <Fact>
        Public Sub CyclicBases2()
            Dim text =
                <compilation name="Compilation">
                    <file name="a.vb">
Class X 
    Inherits Y.N
End Class
Class Y 
    Inherits X.N
End Class
                    </file>
                </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(text)
            Dim g = compilation.GlobalNamespace
            Dim x = g.GetTypeMembers("X").Single()
            Dim y = g.GetTypeMembers("Y").Single()
            Assert.NotEqual(y, x.BaseType)
            Assert.NotEqual(x, y.BaseType)
            Assert.Equal(SymbolKind.ErrorType, x.BaseType.Kind)
            Assert.Equal(SymbolKind.ErrorType, y.BaseType.Kind)
            Assert.Equal("", x.BaseType.Name)
            Assert.Equal("X.N", y.BaseType.Name)
        End Sub

        <Fact>
        Public Sub CyclicBases4()
            Dim text =
                <compilation name="Compilation">
                    <file name="a.vb">
Class A(Of T)
    Inherits B(Of A(Of T))
End Class
Class B(Of T)
    Inherits A(Of B(Of T))
    Public Function F() As A(Of T)
        Return Nothing
    End Function
End Class
                    </file>
                </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(text)
            Assert.Equal(1, compilation.GetDeclarationDiagnostics().Length)
        End Sub

        <Fact>
        Public Sub CyclicBases5()
            ' bases are cyclic, but you can still find members when binding bases
            Dim text =
                <compilation name="Compilation">
                    <file name="a.vb">
Class A
    Inherits B
    Public Class X
    End Class
End Class
Class B
    Inherits A
    Public Class Y
    End Class
End Class
Class Z
    Inherits A.Y
End Class
Class W
    Inherits B.X
End Class
                    </file>
                </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(text)
            Dim g = compilation.GlobalNamespace
            Dim z = g.GetTypeMembers("Z").Single()
            Dim w = g.GetTypeMembers("W").Single()
            Dim zBase = z.BaseType
            Assert.Equal("Y", zBase.Name)
            Dim wBase = w.BaseType
            Assert.Equal("X", wBase.Name)
        End Sub

        <Fact>
        Public Sub EricLiCase1()
            Dim text =
                <compilation name="Compilation">
                    <file name="a.vb">
Interface I(Of T)
End Interface
Class A
    Public Class B
    End Class
End Class
Class C
    Inherits A
    Implements I(Of C.B)
End Class
                    </file>
                </compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithMscorlib(text)
            CompilationUtils.AssertNoErrors(compilation0)  ' same as in Dev10

        End Sub

        <WorkItem(544454, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544454")>
        <Fact()>
        Public Sub InterfaceImplementedWithPrivateType()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Imports System
Imports System.Collections
Imports System.Collections.Generic

Public Class A
    Implements IEnumerable(Of A.MyPrivateType)

    Private Class MyPrivateType
    End Class

    Private Function GetEnumerator() As IEnumerator(Of MyPrivateType) Implements IEnumerable(Of MyPrivateType).GetEnumerator
        Throw New NotImplementedException()
    End Function

    Private Function GetEnumerator1() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function
End Class  
    </file>
</compilation>)

            Dim c2Source =
<compilation name="C2">
    <file name="b.vb">
Imports System.Collections.Generic

Class Z
  Public Function foo(a As A) As IEnumerable(Of Object)
    Return a
  End Function
End Class  
    </file>
</compilation>

            Dim c2 = CompilationUtils.CreateCompilationWithMscorlibAndReferences(c2Source, {New VisualBasicCompilationReference(compilation)})
            'Works this way, but doesn't when compilation is supplied as metadata
            compilation.VerifyDiagnostics()
            c2.VerifyDiagnostics()

            Dim compilationImage = compilation.EmitToArray(options:=New EmitOptions(metadataOnly:=True))
            CompilationUtils.CreateCompilationWithMscorlibAndReferences(c2Source, {MetadataReference.CreateFromImage(compilationImage)}).VerifyDiagnostics()
        End Sub

        <WorkItem(792711, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/792711")>
        <Fact>
        Public Sub Repro792711()
            Dim source =
                <compilation>
                    <file name="a.vb">
Public Class Base(Of T)
End Class

Public Class Derived(Of T) : Inherits Base(Of Derived(Of T))
End Class
    </file>
                </compilation>

            Dim metadataRef = CreateCompilationWithMscorlib(source).EmitToImageReference(embedInteropTypes:=True)

            Dim comp = CreateCompilationWithMscorlibAndReferences(<compilation/>, {metadataRef})
            Dim derived = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Derived")
            Assert.Equal(TypeKind.Class, derived.TypeKind)
        End Sub

        <WorkItem(862536, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/862536")>
        <Fact>
        Public Sub Repro862536()
            Dim source =
                <compilation>
                    <file name="a.vb">
Interface A(Of T)
    Interface B(Of S) : Inherits A(Of B(Of S).B(Of S))
        Interface B(Of U) : Inherits B(Of B(Of U))
        End Interface
    End Interface
End Interface
    </file>
                </compilation>

            Dim comp = CreateCompilationWithMscorlib(source)
            comp.AssertTheseDiagnostics(<errors><![CDATA[
BC30002: Type 'B.B' is not defined.
    Interface B(Of S) : Inherits A(Of B(Of S).B(Of S))
                                      ~~~~~~~~~~~~~~~
BC40004: interface 'B' conflicts with interface 'B' in the base interface 'A' and should be declared 'Shadows'.
        Interface B(Of U) : Inherits B(Of B(Of U))
                  ~
BC30296: Interface 'A(Of T).B(Of S).B(Of U)' cannot inherit from itself: 
    'A(Of T).B(Of S).B(Of U)' inherits from 'A(Of T).B(Of S).B(Of U)'.
        Interface B(Of U) : Inherits B(Of B(Of U))
                                     ~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <WorkItem(862536, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/862536")>
        <Fact>
        Public Sub ExpandingBaseInterface()
            Dim source =
                <compilation>
                    <file name="a.vb">
Interface C(Of T) : Inherits C(Of C(Of T))
End Interface

Interface B : Inherits C(Of Integer).NotFound
End Interface
    </file>
                </compilation>

            ' Can't find NotFound in C(Of Integer), so we check the base type C(Of C(Of Integer)), etc.
            Dim comp = CreateCompilationWithMscorlib(source)
            comp.AssertTheseDiagnostics(<errors><![CDATA[
BC30296: Interface 'C(Of T)' cannot inherit from itself: 
    'C(Of T)' inherits from 'C(Of T)'.
Interface C(Of T) : Inherits C(Of C(Of T))
                             ~~~~~~~~~~~~~
BC30002: Type 'C.NotFound' is not defined.
Interface B : Inherits C(Of Integer).NotFound
                       ~~~~~~~~~~~~~~~~~~~~~~
]]></errors>)

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees.Single())
            Dim typeC = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C").Construct(comp.GetSpecialType(SpecialType.System_Int32))
            Assert.Equal(0, model.LookupSymbols(0, typeC, "NotFound").Length)
        End Sub

        <WorkItem(862536, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/862536")>
        <Fact>
        Public Sub ExpandingBaseInterfaceChain()
            Dim source =
                <compilation>
                    <file name="a.vb">
Interface C(Of T) : Inherits D(Of C(Of T))
End Interface

Interface D(Of T) : Inherits C(Of D(Of T))
End Interface

Interface B : Inherits C(Of Integer).NotFound
End Interface
    </file>
                </compilation>

            ' Can't find NotFound in C(Of Integer), so we check the base type D(Of C(Of Integer)), etc.
            Dim comp = CreateCompilationWithMscorlib(source)
            comp.AssertTheseDiagnostics(<errors><![CDATA[
BC30296: Interface 'C(Of T)' cannot inherit from itself: 
    'C(Of T)' inherits from 'D(Of C(Of T))'.
    'D(Of C(Of T))' inherits from 'C(Of T)'.
Interface C(Of T) : Inherits D(Of C(Of T))
                             ~~~~~~~~~~~~~
BC30002: Type 'C.NotFound' is not defined.
Interface B : Inherits C(Of Integer).NotFound
                       ~~~~~~~~~~~~~~~~~~~~~~
]]></errors>)

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees.Single())
            Dim typeC = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C").Construct(comp.GetSpecialType(SpecialType.System_Int32))
            Assert.Equal(0, model.LookupSymbols(0, typeC, "NotFound").Length)
        End Sub

        <WorkItem(862536, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/862536")>
        <Fact>
        Public Sub ExpandingBaseClass()
            Dim source =
                <compilation>
                    <file name="a.vb">
Class C(Of T) : Inherits C(Of C(Of T))
End Class

Class B : Inherits C(Of Integer).NotFound
End Class
    </file>
                </compilation>

            ' Can't find NotFound in C(Of Integer), so we check the base type C(Of C(Of Integer)), etc.
            Dim comp = CreateCompilationWithMscorlib(source)
            comp.AssertTheseDiagnostics(<errors><![CDATA[
BC31447: Class 'C(Of T)' cannot reference itself in Inherits clause.
Class C(Of T) : Inherits C(Of C(Of T))
                         ~~~~~~~~~~~~~
BC30002: Type 'C.NotFound' is not defined.
Class B : Inherits C(Of Integer).NotFound
                   ~~~~~~~~~~~~~~~~~~~~~~
]]></errors>)

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees.Single())
            Dim typeC = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C").Construct(comp.GetSpecialType(SpecialType.System_Int32))
            Assert.Equal(0, model.LookupSymbols(0, typeC, "NotFound").Length)
        End Sub

        <WorkItem(1036374, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1036374")>
        <Fact()>
        Public Sub InterfaceCircularInheritance_01()
            Dim source =
                <compilation>
                    <file name="a.vb">
Interface A(Of T)
    Inherits A(Of A(Of T))
    Interface B
        Inherits A(Of B)
    End Interface
End Interface
    </file>
                </compilation>
            Dim comp = CreateCompilationWithMscorlib(source)

            Dim a = comp.GetTypeByMetadataName("A`1")
            Dim interfaces = a.AllInterfaces

            Assert.True(interfaces.Single.IsErrorType())

            comp.AssertTheseDiagnostics(<errors><![CDATA[
BC30296: Interface 'A(Of T)' cannot inherit from itself: 
    'A(Of T)' inherits from 'A(Of T)'.
    Inherits A(Of A(Of T))
             ~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <WorkItem(1036374, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1036374")>
        <Fact()>
        Public Sub InterfaceCircularInheritance_02()
            Dim source =
                <compilation>
                    <file name="a.vb">
Interface A(Of T)
    Inherits C(Of T)
    Interface B
        Inherits A(Of B)
    End Interface
End Interface

Interface C(Of T)
    Inherits A(Of A(Of T))
End Interface

    </file>
                </compilation>
            Dim comp = CreateCompilationWithMscorlib(source)

            Dim a = comp.GetTypeByMetadataName("A`1")
            Dim interfaces = a.AllInterfaces

            Assert.True(interfaces.Single.IsErrorType())

            comp.AssertTheseDiagnostics(<errors><![CDATA[
BC30296: Interface 'A(Of T)' cannot inherit from itself: 
    'A(Of T)' inherits from 'C(Of T)'.
    'C(Of T)' inherits from 'A(Of T)'.
    Inherits C(Of T)
             ~~~~~~~
]]></errors>)
        End Sub

    End Class

End Namespace
