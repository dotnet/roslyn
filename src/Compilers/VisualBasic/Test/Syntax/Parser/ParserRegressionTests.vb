' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities
Imports Roslyn.Test.Utilities.Syntax

Public Class ParserRegressionTests : Inherits BasicTestBase

    <WorkItem(540022, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540022")>
    <Fact>
    Public Sub VB000011()
        Dim text = <![CDATA[
                Public Function methodOperator()
        [CStr]  =  [Unicode]   *   [CULng]  -  [RaiseEvent]  *  [Imports]  \ Friend  i Mod i And i Or i Handles   Strict  i CSByte  '+ - * / % & | ^
            b  <  Not While  b '!
        i [Public]   &=  i  .  5 [Resume]  '<<
        i  ;  i >>  [Set]  '>>
        ]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540023, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540023")>
    <Fact>
    Public Sub VB000103()
        Dim text = <![CDATA[
        Public  [MustOverride]  = Sub( -->  Console.WriteLine [xml] ("Sub Statement 2")
    Public Get  L4  )  Function(y [IsTrue]  As Integer) As Boolean
        ]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540023, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540023")>
    <Fact>
    Public Sub VB000103_minimal()
        Dim text = <![CDATA[
        Public foo = Sub( Console.WriteLine()
    Public Get ) 
        ]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540023, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540023")>
    <Fact>
    Public Sub VB000103_related()
        Dim text = <![CDATA[
        Public foo = Sub( Console.WriteLine()
    Public Set ) 
        ]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540024, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540024")>
    <Fact>
    Public Sub VB000126_01()
        Dim text = "         Property  con As [String] = [char] +  [Assembly]   <?  hexchar EndIf  +  [Yield] . [OrElse] ( [True] ) Case  & [double].ToString()  ?  float ) ToString( ;  &  [Dim] "
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540023, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540023")>
    <Fact>
    Public Sub VB000126_02()
        Dim text = <![CDATA[     Using  Function
     Dim  Sub  [CDec]  { )
        ]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(542660, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542660")>
    <Fact>
    Public Sub VB000126_03()
        Dim text = <![CDATA[     Using  Function
     Public  Sub  [CDec]  { )
        ]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540027, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540027")>
    <Fact>
    Public Sub VB000139()
        Dim text = <![CDATA[        Function  [Distinct] ()
     ExternalSource  Function
    MustInherit Class B(Of T)]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540025")>
    <Fact>
    Public Sub VB000147()
        Dim text = <![CDATA[    Sub Overloads  Method1a * )
     Binary  Sub
    Dim Get   [xml]  As [Inherits]  MyDelegate
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540026, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540026")>
    <Fact>
    Public Sub VB000211()
        Dim text = <![CDATA[        Dim s As Object = Function(x <[CDATA[   [And]  + 1 '=>
End  ' 
#End Region
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540024, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540024")>
    <Fact>
    Public Sub VB000365_01()
        Dim text = "     Property  Sub delfoo1( [Stop] ByVal Erase   [With]  As [Shadows]  Object -->   GetType  e As System ]]> EventArgs ]]> "
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540027, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540027")>
    <Fact>
    Public Sub VB000365_02()
        Dim text = <![CDATA[    Public Function "   [When]  /  << 
     Until  Function
 Dim  Class
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540026, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540026")>
    <Fact>
    Public Sub VB000705()
        Dim text = <![CDATA[Next  Function
 MustOverride   Shared 
#
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540026, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540026")>
    <Fact>
    Public Sub VB001507()
        Dim text = <![CDATA[Function  [For]  << ) As  SByte 
         Option 
If %>  (  [Nothing] .LoopingMethod <> 0) then Property 
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540030, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540030")>
    <Fact>
    Public Sub VB001690()
        Dim text = <![CDATA[Function RunTests( !   Interface  Integer [SyncLock] 
	 By   Function 
        Dim Namespace  [partial] &   ?  0
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540029, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540029")>
    <Fact>
    Public Sub VB001726()
        Dim text = <![CDATA[
             Static  Sub
            GC. Decimal SuppressFinalize(Me +=  , 
         Compare  Sub
        Public  NotInheritable  Operator Yield  IsTrue [Boolean]  ( ByVal CInt   [Enum]   Overridable   [Event] ) As Boolean
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540028, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540028")>
    <Fact>
    Public Sub VB001874()
        Dim text = <![CDATA[   Function MyClass  Foo(ByVal  [Preserve]  As  [Protected]  /=  For  T [CStr] ) :  As  Enum 
     Call  Function
     MustInherit   Enum 

]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540025")>
    <Fact>
    Public Sub VB002423()
        Dim text = <![CDATA[               Sub  i2 As  Select ,
     Skip  Function
        Dim Event  b As Boolean = True  CUInt  False Or True  In  False '& | ^
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540031, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540031")>
    <Fact>
    Public Sub VB003108()
        Dim text = <![CDATA[Function RunTests </ ) As Integer Char 
If (  [Continue] .FooExtension /= "Scenario 13_2", 13)  <[CDATA[   [End]  & "test2") then Event 
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540031, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540031")>
    <Fact>
    Public Sub VB003108_minimal()
        Dim text = <![CDATA[Function RunTests As Integer
if true then Event
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540031, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540031")>
    <Fact>
    Public Sub VB003108_related()
        Dim text = <![CDATA[Function RunTests As Integer
if true then else Event
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540024, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540024")>
    <Fact>
    Public Sub VB003272()
        Dim text = "    Property  [Catch]  ' ) Interface   Interface  IVariance( Take  Animals [Protected] ) [Descending]"
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540024, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540024")>
    <Fact>
    Public Sub VB003272_02()
        Dim text = "         MustInherit  Property Variant  item( Nothing   [Date]   Async   Await  .   OrElse  Integer CObj "
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540027, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540027")>
    <Fact>
    Public Sub VB003272_03()
        Dim text = <![CDATA[         Shared  Sub CChar 
     Off  Sub
    MustInherit Class B(Of T)]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540023, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540023")>
    <Fact>
    Public Sub VB004298()
        Dim text = <![CDATA[If Sub  (  [Option]  & AnonymousType <>  WriteOnly  @   Overloads 
    MustInherit Class B(Of T)
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540025")>
    <Fact>
    Public Sub VB004742()
        Dim text = <![CDATA[        Function Overridable  SelectionMethods " )  When  Integer
            If [Decimal]  icount [Overloads]   !   Aggregate  Then [CShort] 
         CUInt  Function
             EndIf  a As AndAlso  Boolean  +  True
            Dim [IsNot]  b As  Select   )  False Unicode 
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <Fact>
    Public Sub VB004742_minimal()
        Dim text = <![CDATA[        
            Function SelectionMethods()
            If true Then
         CUInt  Function
             endif         
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <Fact>
    Public Sub VB004742_related()
        Dim text = <![CDATA[        
            Function SelectionMethods()
            while true 
         CUInt  Function
             wend           
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540022, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540022")>
    <Fact>
    Public Sub VB006755()
        Dim text = "     NotInheritable  Function  [Me] (ByVal  [IsFalse]  As Integer) As  Error " + Environment.NewLine
        text = text + "    retVal  <  0" + Environment.NewLine + "                 [Set]   ]]>  1 [Of] "
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540023, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540023")>
    <Fact>
    Public Sub VB006755_02()
        Dim text = <![CDATA[     Erase  Function
    MustInherit Class B(Of T)
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540025")>
    <Fact>
    Public Sub VB010019()
        Dim text = <![CDATA[             Sub  Case x. Select  [UShort] 
                    If x MyClass . [CStr]  [Not]  <> 4  Double 
     AddHandler  Function
 EndIf   While 
Public Delegate Sub MyDelegate( [Object] ByVal message  EndIf   Partial )
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540025")>
    <Fact>
    Public Sub VB013380()
        Dim text = <![CDATA[        Function Operators As () As [Or]  Integer
            If ( [Mod]  Or
            Return  [Iterator] 
         Type  Function
         EndIf  AnonymousType ?>  <  <   MustOverride   Key 
 Operator   ExternalSource 
     Select   Like  Statements(ByVal ByRef   [GoTo]  As  Event  %>  As Or   Object 
        Dim retVal As Integer = [Xor]   ULong 
        Dim thislock =  ExternalChecksum  Object [SyncLock]  @  { 
                 Class   Order 
        Public Property  [Me] ( [RaiseEvent] ) #  As  Wend 
            Get Public 
             Decimal   Stop 
            Set( SByte  value  Catch  String [Yield]  ( 
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540024, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540024")>
    <Fact>
    Public Sub VB013706()
        Dim text = "    Property Integer   [NotOverridable] ( [Ansi] ) From  As IVariance MustInherit  ; Of CDate  Cheetah >> "
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540025")>
    <Fact>
    Public Sub VB013706_02()
        Dim text = <![CDATA[sub [Unicode]  Main /= ( <> 
if( [False] runTests GoTo  <> Single  <? ) -   Off 
 CObj  Sub
EndIf  1
            Dim [IsTrue]  s42 />  As %>  New ByRef   [OrElse]  ! Of  TypeOf ) [Call] . [CByte]  [Strict]  <> Of Integer) [MyBase]
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(541735, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541735")>
    <Fact>
    Public Sub VB029482()
        Dim text = <![CDATA[Dim  [Each]  =  <[CDATA[ 1, Select 
                    #  ]
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540025")>
    <Fact>
    Public Sub VB038799()
        Dim text = <![CDATA[        Public Sub Declare  Dispose [By]  (  <[CDATA[  Implements IDisposable.Dispose
            If  <[CDATA[ aa < 10)  Long 
                Return  Sub 
         EndIf   Explicit 
         Sub  Shared Operator [Descending]  +(ByVal  [Where]  As  [Mid]  >  ByVal second CSng  As A) As Integer
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540025")>
    <Fact>
    Public Sub VB020620()
        Dim text = <![CDATA[sub Main( Select  \= 
if [MustInherit] ( [Namespace]  <> [Enum] 0 [WriteOnly]  (   Next 
 Me  Sub
             EndIf   [Catch]  As  WriteOnly   [RemoveHandler] 
If AddressOf   ^   [Key]  -= LoopingMethod @   )  0 [WithEvents]  />  then
	return 1 [Auto] 
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540023, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540023")>
    <Fact>
    Public Sub VB021387()
        Dim text = <![CDATA[            Return 0 * 
         Function  Function
    MustInherit Class B(Of T)
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540025")>
    <Fact>
    Public Sub VB024372()
        Dim text = <![CDATA[                 Public  Sub [Like] 
                    If As   [Event]   <<=   To  < 10  If 
                 With  Function
 EndIf   [Imports] 
    Friend Function Statements( False  i [Like]  As Integer Try ) As [Case]  Integer
            If ( := aa  <--  10 '  Then
         Get  Property  [Select]   Sub  String [Shadows] 
         MustInherit  Property item(ByVal  [Shadows]   Unicode  Integer True ) As [Alias]  Integer
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540027, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540027")>
    <Fact>
    Public Sub VB030545()
        Dim text = <![CDATA[             Function  If [Do] 
                 [Narrowing]   Function 
 Const  Class With 
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540025")>
    <Fact>
    Public Sub VB030545_02()
        Dim text = <![CDATA[        Function =   [IsFalse]  \=  &=   Equals  Integer
         Assembly  Function
            Private  Set 
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540025")>
    <Fact>
    Public Sub VB016274()
        Dim text = <![CDATA[        Function LoopingMethod( @ ) As Integer
            While  [As]  <= 100 [Into] 
         Ascending  Function
 Wend  Class
            If  [Overloads]   xml ]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540025")>
    <Fact>
    Public Sub VB027868()
        Dim text = <![CDATA[    Function  [Lib] ( Until )
        If {1, Char  2 -   [Loop]  /> . [Return] Count = 2 Then
     CUInt  Function
         EndIf  b  Get  Boolean  <--  True [CDec]   ByVal   DirectCast  Or [CBool]  True  Implements  False '& | ^
        i  <  1 '++
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540025")>
    <Fact>
    Public Sub VB025825()
        Dim text = <![CDATA[        Function [Interface]  Operators( [TryCast]  <?  As Integer
            If CSByte  ( [End]  Or
            Return 0
         ExternalChecksum  Function
             EndIf   " 
        Dim [Long]   [Friend]   >=  From c In customers [CLng] 
                             Interface  New With [Me]   ) Key .Address = [SByte]  Address WriteOnly , [AndAlso]  Key  <-- CustCount  <--   [TryCast]  <  [ParamArray]  >>=  \=  <[CDATA[ 
         Structure   Double  x In  [Boolean] 
 CObj  Delegate Sub [Continue]  MyDelegate( MyBase   [Friend]  As  Module )
Class MyClass1
    Custom Event  [Widening]  As MyDelegate
         CShort (ByVal  [ReDim]  As Function  MyDelegate)
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540025")>
    <Fact>
    Public Sub VB062475()
        Dim text = <![CDATA[Function
If
CShort Sub
EndIf
o e()r
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540027, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540027")>
    <Fact>
    Public Sub VB069460()
        Dim text = <![CDATA[sub Main Byte  ]]&lt; )
     Assembly  Function
 MustInherit  Class Bar Iterator ]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540025")>
    <Fact>
    Public Sub VB101269()
        Dim text = <![CDATA[        Function  [CLng] ()  CULng  Integer As 
            If icount [End]   ;  1  Xor 
         Me  Function
         EndIf  Operators() As Integer EndIf 
        Dim ia3 [Until]  As Const  Integer() = [Ascending]  { Module , [AddHandler] 
                                 <--  ) 
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540025")>
    <Fact>
    Public Sub VB097187()
        Dim text = <![CDATA[sub  [Next] ( Where  /= 
if(runTests [MustOverride]  <>0 +=  then
 Text  Sub
         EndIf 
If  :  Statements += 8) <> 0) [Integer]  then [Yield] 
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540025")>
    <Fact>
    Public Sub VB070370()
        Dim text = <![CDATA[             Sub  Pclass [GetXmlNamespace]  As [Const]  New ClsPPMTest003
If [Yield]   >>  s42 Object . [Infer] AnonymousType  }   DirectCast  <?   ParamArray 
	 Iterator   Sub 
If <<   \=   [CObj]  <>  [Compare] ) Ansi  then
end if
 EndIf   <>  Lambdas Like  "  [Global]  <<= invoke NotInheritable (2 >>  <> 0 Boolean  <<   Widening 
 Partial  (  [Infer]  > 8) [Function]  <> [Single]  0 Shadows ) [Off]   True 
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540025")>
    <Fact>
    Public Sub VB075281()
        Dim text = <![CDATA[    Sub Method1a Mod  " )
                    If y [When]   &=   [Then]   >>  10 Then Inherits 
                     Join  If Xor 
                 CULng  Function
         EndIf   [Call] ( {  =  . "Beverages" <   [Ascending]  {   GoTo , [CObj]  "Dairy Products", "Seafood" - 
        Dim  [IsTrue]  = From [Mod]  c In [Take]  categories [IsFalse]  _
                    Group !  Join p In productList On c Equals [Event]  p. [Not]  Into  If  _
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(542668, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542668")>
    <Fact>
    Public Sub VB063291()
        Dim text = <![CDATA[ Function RunTests() As Integer
        Try
            s20.item(5) =  Function 
If ( s20.p <> "A") then Catch 
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(542668, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542668")>
    <Fact>
    Public Sub VB063291_minimal()
        Dim text = <![CDATA[ 
        Function RunTests() As Integer
        Try
            dim x = Function 
            If true then catch
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(542668, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542668")>
    <Fact>
    Public Sub VB063291_related()
        Dim text = <![CDATA[ 
        Function RunTests() As Integer
        Try
            dim x = Function 
            If true then else catch
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540032, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540032")>
    <Fact>
    Public Sub VB087373()
        Dim text = <![CDATA[ Function RunTests </  <<=  As  False 
	 Try   [Erase] 
If [NotInheritable]  ( [Type]  ModuleEx. [Finally] FooExtension [Operator] ("Scenario 13_1" </  <> +=   RaiseEvent  & [In]  "test" [IsNot]  +=  then
	 Where   Sub 
	 Else   Finally 
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540032, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540032")>
    <Fact>
    Public Sub VB087373_minimal()
        Dim text = <![CDATA[ Function RunTests() As Boolean
	 Try 
If true then
	 where sub
	 Else   Finally 
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540032, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540032")>
    <Fact>
    Public Sub VB087373_related1()
        Dim text = <![CDATA[Function RunTests() As Boolean
	 Try 
If true then
	 where sub
	 Elseif true   Finally  
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540032, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540032")>
    <Fact>
    Public Sub VB087373_related2()
        Dim text = <![CDATA[Function RunTests() As Boolean
	 Try 
      select case true
        case true
          where sub
            case false   
        end select
   Finally 
   end try
end function 
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540022, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540022")>
    <Fact>
    Public Sub VB087839()
        Dim text = <![CDATA[    Friend Function  [Date] ( Or ByVal i As (  Integer) As  Take 
            retVal  <--  1
        Else ; 

]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540025")>
    <Fact>
    Public Sub VB087839_02()
        Dim text = <![CDATA[             Operator  icount [As]   }  1 Then
            If WriteOnly  ( </  [UInteger]  Or
         Return  Function
         EndIf   Off 
    Sub MyHandler( MyClass  Compare  message  Object  String)
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540025")>
    <Fact>
    Public Sub VB074167()
        Dim text = <![CDATA[Function RunTests >>=  &   Enum  Integer
If (  [Join]  <<=  [TypeOf]  <[CDATA[  0) then
If Function  ( s42 {  [Case]  <>  >>=  /=  then
 EndIf   RemoveHandler 
If ( [Custom]   [Custom] .x ! . Friend GetType.Name <> "Int323" [CSByte]  ?   Yield 
end if
end if
If  {  Nullable( Group )  )  0 &   If 
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540026, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540026")>
    <Fact>
    Public Sub VB137731()
        Dim text = <![CDATA[    Function  [Unicode]  <<= ) As Integer
     Call  Function
 Custom   Overloads 
#Region "Co Contra Variance Variance2"
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540024, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540024")>
    <Fact>
    Public Sub VB121067()
        Dim text = "         Property   Stop   [AddressOf]  = ByVal NotOverridable  index  CDate   DirectCast  '  As  CObj "
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540025")>
    <Fact>
    Public Sub VB121067_02()
        Dim text = <![CDATA[             Operator  a And MustInherit 
         If  ia7 = { Exit ({ Step 1 Strict }) [Resume]  &  ( Ascending  : 3 =  4 \ ), ( %> 5, 6 <--  2})} 'jagged array
     xml  Function
         [Property]   <[CDATA[   [Implements]   .   [Structure]  '<<
         [CStr]  %>  [IsFalse]  = 10
         Event  p2  MustOverride  New  [While]  With [Lib]  { Operator .X = [Handles]   And ,  # Y  <[CDATA[  12} Order 
         EndIf  i [Error]   .  0 >>=   To   , Not b) [From]  AndAlso CULng  f [Optional]  =  Unicode   CByte 
        Dim  [Equals]   &   GetXmlNamespace  Object <? ) Infer 
        Using temp Class  As New Type   [Aggregate] ( Finally )
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540023, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540023")>
    <Fact>
    Public Sub VB142448()
        Dim text = <![CDATA[     Public  [Key]  = Function( CULng  [RaiseEvent]  As  Let  {  As <<=  Boolean
        Dim Function  ia4() As Integer  "   /  Private ,
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(540025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540025")>
    <Fact>
    Public Sub VB138211()
        Dim text = <![CDATA[        Function [On]  Operators &  =  As Integer [ByVal] 
            If  Me  a  IsNot 
             If   Function 
 EndIf   Like   [Mod] (Of Double  In Throw  T) " ( [ExternalSource] ByVal  [Declare]  As [By]   [ParamArray]  * 
    Function Foo( <<  As [CShort]  T
Public Class  [Continue] (Of T As  Unicode ) : Implements IVariance2( Await  T [Join] ) Default 
Module Lambdas
     With   [Compare]   >=  Function( [UInteger]   Binary  Integer :  As Integer   ElseIf 
]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <WorkItem(545424, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545424")>
    <Fact()>
    Public Sub CaseStatementOutsideMethodBody()
        Dim text = <![CDATA[        Dim number As Integer = 8
Select Case number
    Case 1 To 5
        Debug.WriteLine("Between 1 and 5, inclusive")
        ' The following is the only Case clause that evaluates to True.
    Case 6, 7, 8
        Debug.WriteLine("Between 6 and 8, inclusive")
    Case 9 To 10
        Debug.WriteLine("Equal to 9 or 10")
    Case Else
        Debug.WriteLine("Not between 1 and 10, inclusive")
End Select

]]>.Value
        VisualBasicSyntaxTree.ParseText(text)
    End Sub

    <Fact, WorkItem(658140, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/658140")>
    <WorkItem(103047, "https://devdiv.visualstudio.com/defaultcollection/DevDiv/_workitems?_a=edit&id=103047")>
    Public Sub ParseFileOnBinaryFile()
        ' This is doing the same thing as ParseFile, but using a MemoryStream
        ' instead of FileStream (because I don't want to write a file to disk).
        Using data As New MemoryStream(TestResources.NetFX.v4_0_30319.mscorlib)
            Const bug103047IsFixed = False

            If bug103047IsFixed Then
                Dim tree As SyntaxTree = VisualBasicSyntaxTree.ParseText(EncodedStringText.Create(data))
                tree.GetDiagnostics().VerifyErrorCodes(Diagnostic(ERRID.ERR_BinaryFile))
            Else
                Assert.Throws(Of System.IO.InvalidDataException)(Sub() VisualBasicSyntaxTree.ParseText(EncodedStringText.Create(data)))
            End If
        End Using
    End Sub

    <Fact(), WorkItem(675589, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/675589")>
    Public Sub ParseBadLambda()
        ParseAndVerify(<![CDATA[
Module M
    Function F1()
        Dim x = Sub()
            If True Then Function F2()
End Module
]]>,
    <errors>
        <error id="30027"/>
        <error id="36673"/>
        <error id="30289"/>
    </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Function F1()
        Dim x = Sub()
            If True Then Else Function F2()
End Module
]]>,
    <errors>
        <error id="30027"/>
        <error id="36673"/>
        <error id="30289"/>
    </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Function F1()
        Dim x = Sub()
            If True Then F1() : Function F2()
End Module
]]>,
    <errors>
        <error id="30027"/>
        <error id="36673"/>
        <error id="30289"/>
    </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Function F()
        Dim x = Sub()
            If True Then Interface I
End Module
]]>,
    <errors>
        <error id="30027"/>
        <error id="36673"/>
        <error id="30289"/>
    </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Function F()
        Return Function() Sub() Interface I
End Module
]]>,
    <errors>
        <error id="30027"/>
        <error id="30289"/>
    </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Function F()
        Return Function() Sub() If True Then Else Interface I
End Module
]]>,
    <errors>
        <error id="30027"/>
        <error id="30289"/>
    </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Function F()
        Return Function()
            Return Sub()
                Interface I
End Module
]]>,
    <errors>
        <error id="30027"/>
        <error id="36674"/>
        <error id="36673"/>
        <error id="30289"/>
        <error id="30253"/>
    </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Function F()
        Return Function()
            Return Sub()
                If True Then Else Interface I
End Module
]]>,
    <errors>
        <error id="30027"/>
        <error id="36674"/>
        <error id="36673"/>
        <error id="30289"/>
    </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Function F()
        Return Function()
            Return Sub()
                If True Then Dim x = Sub() If False Then Else Interface I
End Module
]]>,
    <errors>
        <error id="30027"/>
        <error id="36674"/>
        <error id="36673"/>
        <error id="30289"/>
    </errors>)
    End Sub

    ''' <summary>
    ''' An identifier followed by a colon in an Enum block is only
    ''' treated as a label if there are no attributes. If there are
    ''' attributes, the identifier is considered an Enum member.
    ''' This matches the native compiler.
    ''' </summary>
    <Fact(), WorkItem(675486, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/675486")>
    Public Sub ParseAttributeOnIdentifierFollowedByColonInEnum()
        ' Label.
        ParseAndVerify(<![CDATA[
Enum E
    L : A
End Enum
]]>,
<errors>
    <error id="31001"/>
</errors>)
        ' Enum members.
        ParseAndVerify(<![CDATA[
Enum E
<A L : A
End Enum
]]>,
    <errors>
        <error id="30636"/>
    </errors>)
        ' Enum members.
        ParseAndVerify(<![CDATA[
Enum E
    <A>
    L:
End Enum
]]>)
        ' Other declarations.
        ParseAndVerify(<![CDATA[
Enum E
    Private L : A
End Enum
]]>,
    <errors>
        <error id="30185"/>
        <error id="30619"/>
        <error id="30689"/>
        <error id="30184"/>
    </errors>)
    End Sub

    <WorkItem(2867, "https://github.com/dotnet/roslyn/issues/2867")>
    <ConditionalFact(GetType(IsRelease))>
    Public Sub TestBinary()
        Dim tree = VisualBasicSyntaxTree.ParseText(New RandomizedSourceText())
        Assert.Equal(Syntax.InternalSyntax.Scanner.BadTokenCountLimit, tree.GetDiagnostics().Where(Function(d) d.Code = ERRID.ERR_IllegalChar).Count())
    End Sub

End Class
