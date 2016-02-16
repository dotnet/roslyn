' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class WellKnownTypeValidationTests
        Inherits BasicTestBase

        <Fact>
        <WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")>
        Public Sub NonPublicSpecialType()
            Dim source = <![CDATA[
Namespace System
    Public Class [Object]
        Public Sub New()
        End Sub
    End Class

    Friend Class [String]
    End Class

    Public Class ValueType
    End Class

    Public Structure Void
    End Structure
End Namespace
]]>.Value.Replace(vbLf, vbCrLf).Trim

            Dim validate As Action(Of VisualBasicCompilation) =
                Sub(comp)
                    Dim special = comp.GetSpecialType(SpecialType.System_String)
                    Assert.Equal(TypeKind.Error, special.TypeKind)
                    Assert.Equal(SpecialType.System_String, special.SpecialType)
                    Assert.Equal(Accessibility.Public, special.DeclaredAccessibility)

                    Dim lookup = comp.GetTypeByMetadataName("System.String")
                    Assert.Equal(TypeKind.Class, lookup.TypeKind)
                    Assert.Equal(SpecialType.None, lookup.SpecialType)
                    Assert.Equal(Accessibility.Internal, lookup.DeclaredAccessibility)
                End Sub

            ValidateSourceAndMetadata(source, validate)
        End Sub

        <Fact>
        <WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")>
        Public Sub NonPublicSpecialTypeMember()
            Dim sourceTemplate = <![CDATA[
Namespace System
    Public Class [Object]
        Public Sub New()
        End Sub

        {0} Overridable Function ToString() As [String]
            Return Nothing
        End Function
    End Class

    {0} Class [String]
        Public Shared Function Concat(s1 As [String], s2 As [String]) As [String]
            Return Nothing
        End Function
    End Class

    Public Class ValueType
    End Class

    Public Structure Void
    End Structure
End Namespace
]]>.Value.Replace(vbLf, vbCrLf).Trim

            Dim validatePresent As Action(Of VisualBasicCompilation) =
                Sub(comp)
                    Assert.NotNull(comp.GetSpecialTypeMember(SpecialMember.System_Object__ToString))
                    Assert.NotNull(comp.GetSpecialTypeMember(SpecialMember.System_String__ConcatStringString))
                    comp.GetDiagnostics()
                End Sub

            Dim validateMissing As Action(Of VisualBasicCompilation) =
                Sub(comp)
                    Assert.Null(comp.GetSpecialTypeMember(SpecialMember.System_Object__ToString))
                    Assert.Null(comp.GetSpecialTypeMember(SpecialMember.System_String__ConcatStringString))
                    comp.GetDiagnostics()
                End Sub

            ValidateSourceAndMetadata(String.Format(sourceTemplate, "Public"), validatePresent)
            ValidateSourceAndMetadata(String.Format(sourceTemplate, "Friend"), validateMissing)
        End Sub

        ' Document the fact that we don't reject type parameters with constraints (yet?).
        <Fact>
        <WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")>
        Public Sub GenericConstraintsOnSpecialType()
            Dim source = <![CDATA[
Namespace System
    Public Class [Object]
        Public Sub New()
        End Sub
    End Class

    Public Structure Nullable(Of T As New)
    End Structure

    Public Class ValueType
    End Class

    Public Structure Void
    End Structure
End Namespace
]]>.Value.Replace(vbLf, vbCrLf).Trim

            Dim validate As Action(Of VisualBasicCompilation) =
                Sub(comp)
                    Dim special = comp.GetSpecialType(SpecialType.System_Nullable_T)
                    Assert.Equal(TypeKind.Structure, special.TypeKind)
                    Assert.Equal(SpecialType.System_Nullable_T, special.SpecialType)

                    Dim lookup = comp.GetTypeByMetadataName("System.Nullable`1")
                    Assert.Equal(TypeKind.Structure, lookup.TypeKind)
                    Assert.Equal(SpecialType.System_Nullable_T, lookup.SpecialType)
                End Sub

            ValidateSourceAndMetadata(source, validate)
        End Sub

        ' No special type members have type parameters that could (incorrectly) be constrained.

        <Fact>
        <WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")>
        Public Sub NonPublicWellKnownType()
            Dim source = <![CDATA[
Namespace System
    Public Class [Object]
        Public Sub New()
        End Sub
    End Class

    Friend Class Type
    End Class

    Public Class ValueType
    End Class

    Public Structure Void
    End Structure
End Namespace
]]>.Value.Replace(vbLf, vbCrLf).Trim

            Dim validate As Action(Of VisualBasicCompilation) =
                Sub(comp)
                    Dim wellKnown = comp.GetWellKnownType(WellKnownType.System_Type)
                    If wellKnown.DeclaringCompilation Is comp Then
                        Assert.Equal(TypeKind.Class, wellKnown.TypeKind)
                        Assert.Equal(Accessibility.Internal, wellKnown.DeclaredAccessibility)
                    Else
                        Assert.Equal(TypeKind.Error, wellKnown.TypeKind)
                        Assert.Equal(Accessibility.Public, wellKnown.DeclaredAccessibility)
                    End If

                    Dim lookup = comp.GetTypeByMetadataName("System.Type")
                    Assert.Equal(TypeKind.Class, lookup.TypeKind)
                    Assert.Equal(Accessibility.Internal, lookup.DeclaredAccessibility)
                End Sub

            ValidateSourceAndMetadata(source, validate)
        End Sub

        <Fact>
        <WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")>
        Public Sub NonPublicWellKnownType_Nested()
            Dim sourceTemplate = <![CDATA[
Namespace System.Diagnostics
    {0} Class DebuggableAttribute
        {1} Enum DebuggingModes
            Mode
        End Enum
    End Class
End Namespace

Namespace System
    Public Class [Object]
        Public Sub New()
        End Sub
    End Class

    Public Class ValueType
    End Class

    Public Class [Enum]
    End Class

    Public Structure Void
    End Structure

    Public Structure [Int32]
    End Structure
End Namespace
]]>.Value.Replace(vbLf, vbCrLf).Trim

            Dim validate As Action(Of VisualBasicCompilation) =
                Sub(comp)
                    Dim wellKnown = comp.GetWellKnownType(WellKnownType.System_Diagnostics_DebuggableAttribute__DebuggingModes)
                    Assert.Equal(If(wellKnown.DeclaringCompilation Is comp, TypeKind.Enum, TypeKind.Error), wellKnown.TypeKind)

                    Dim lookup = comp.GetTypeByMetadataName("System.Diagnostics.DebuggableAttribute+DebuggingModes")
                    Assert.Equal(TypeKind.Enum, lookup.TypeKind)
                End Sub

            ValidateSourceAndMetadata(String.Format(sourceTemplate, "Public", "Friend"), validate)
            ValidateSourceAndMetadata(String.Format(sourceTemplate, "Friend", "Public"), validate)
        End Sub

        <Fact>
        <WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")>
        Public Sub NonPublicWellKnownTypeMember()
            Dim sourceTemplate = <![CDATA[
Namespace System
    Public Class [Object]
        Public Sub New()
        End Sub
    End Class

    Public Class [String]
    End Class

    {0} Class Type
        Public Shared ReadOnly Missing As [Object]
    End Class

    Public Class FlagsAttribute
        {0} Sub New()
        End Sub
    End Class 

    Public Class ValueType
    End Class

    Public Structure Void
    End Structure
End Namespace
]]>.Value.Replace(vbLf, vbCrLf).Trim

            Dim validatePresent As Action(Of VisualBasicCompilation) =
                Sub(comp)
                    Assert.NotNull(comp.GetWellKnownTypeMember(WellKnownMember.System_Type__Missing))
                    Assert.NotNull(comp.GetWellKnownTypeMember(WellKnownMember.System_FlagsAttribute__ctor))
                    comp.GetDiagnostics()
                End Sub

            Dim validateMissing As Action(Of VisualBasicCompilation) =
                Sub(comp)
                    If comp.Assembly.CorLibrary Is comp.Assembly Then
                        Assert.NotNull(comp.GetWellKnownTypeMember(WellKnownMember.System_Type__Missing))
                        Assert.NotNull(comp.GetWellKnownTypeMember(WellKnownMember.System_FlagsAttribute__ctor))
                    Else
                        Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_Type__Missing))
                        Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_FlagsAttribute__ctor))
                    End If
                    comp.GetDiagnostics()
                End Sub

            ValidateSourceAndMetadata(String.Format(sourceTemplate, "Public"), validatePresent)
            ValidateSourceAndMetadata(String.Format(sourceTemplate, "Friend"), validateMissing)
        End Sub

        ' Document the fact that we don't reject type parameters with constraints (yet?).
        <Fact>
        <WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")>
        Public Sub GenericConstraintsOnWellKnownType()
            Dim source = <![CDATA[
Namespace System
    Public Class [Object]
        Public Sub New()
        End Sub
    End Class

    Public Class ValueType
    End Class

    Public Structure Void
    End Structure
End Namespace

Namespace System.Threading.Tasks
    Public Class Task(Of T As New)
    End Class
End Namespace
]]>.Value.Replace(vbLf, vbCrLf).Trim

            Dim validate As Action(Of VisualBasicCompilation) =
                Sub(comp)
                    Dim wellKnown = comp.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task_T)
                    Assert.Equal(TypeKind.Class, wellKnown.TypeKind)

                    Dim lookup = comp.GetTypeByMetadataName("System.Threading.Tasks.Task`1")
                    Assert.Equal(TypeKind.Class, lookup.TypeKind)
                End Sub

            ValidateSourceAndMetadata(source, validate)
        End Sub

        ' Document the fact that we don't reject type parameters with constraints (yet?).
        <Fact>
        <WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")>
        Public Sub GenericConstraintsOnWellKnownTypeMember()
            Dim sourceTemplate = <![CDATA[
Namespace System
    Public Class [Object]
        Public Sub New()
        End Sub
    End Class

    Public Class Activator
        Public Shared Function CreateInstance(Of T{0})() As T
            Throw New Exception()
        End Function
    End Class

    Public Class Exception
        Public Sub New()
        End Sub
    End Class

    Public Class ValueType
    End Class

    Public Structure Void
    End Structure
End Namespace
]]>.Value.Replace(vbLf, vbCrLf).Trim

            Dim validate As Action(Of VisualBasicCompilation) =
                Sub(comp)
                    Assert.NotNull(comp.GetWellKnownTypeMember(WellKnownMember.System_Activator__CreateInstance_T))
                    comp.GetDiagnostics()
                End Sub

            ValidateSourceAndMetadata(String.Format(sourceTemplate, ""), validate)
            ValidateSourceAndMetadata(String.Format(sourceTemplate, " As New"), validate)
        End Sub

        Private Shared Sub ValidateSourceAndMetadata(source As String, validate As Action(Of VisualBasicCompilation))
            Dim comp1 = CreateCompilationWithoutReferences(WrapInCompilationXml(source))
            validate(comp1)

            Dim reference = comp1.EmitToImageReference()
            Dim comp2 = CreateCompilationWithReferences(<compilation/>, {reference})
            validate(comp2)
        End Sub

        Private Shared Function WrapInCompilationXml(source As String) As XElement
            Return <compilation>
                       <file name="a.vb">
                           <%= source %>
                       </file>
                   </compilation>
        End Function

        <Fact>
        <WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")>
        Public Sub PublicVersusInternalWellKnownType()
            Dim corlibSource =
                <compilation>
                    <file name="a.vb">
Namespace System
    Public Class [Object]
        Public Sub New()
        End Sub
    End Class

    Public Class [String]
    End Class

    Public Class Attribute
    End Class

    Public Class ValueType
    End Class

    Public Structure Void
    End Structure
End Namespace

Namespace System.Runtime.CompilerServices
    Public Class InternalsVisibleToAttribute : Inherits System.Attribute
        Public Sub New(s As [String])
        End Sub
    End Class
End Namespace
                    </file>
                </compilation>

            If True Then
                Dim libSourceTemplate = <![CDATA[
Namespace System
    {0} Class Type
    End Class
End Namespace
]]>.Value.Replace(vbLf, vbCrLf).Trim

                Dim corlibRef = CreateCompilationWithoutReferences(corlibSource).EmitToImageReference()
                Dim publicLibRef = CreateCompilationWithReferences(WrapInCompilationXml(String.Format(libSourceTemplate, "Public")), {corlibRef}).EmitToImageReference()
                Dim internalLibRef = CreateCompilationWithReferences(WrapInCompilationXml(String.Format(libSourceTemplate, "Friend")), {corlibRef}).EmitToImageReference()

                Dim comp = CreateCompilationWithReferences({}, {corlibRef, publicLibRef, internalLibRef}, assemblyName:="Test")

                Dim wellKnown = comp.GetWellKnownType(WellKnownType.System_Type)
                Assert.NotNull(wellKnown)
                Assert.Equal(TypeKind.Class, wellKnown.TypeKind)
                Assert.Equal(Accessibility.Public, wellKnown.DeclaredAccessibility)

                Dim Lookup = comp.GetTypeByMetadataName("System.Type")
                Assert.Null(Lookup) ' Ambiguous
            End If

            If True Then
                Dim libSourceTemplate = <![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Test")>

Namespace System
    {0} Class Type
    End Class
End Namespace
]]>.Value.Replace(vbLf, vbCrLf).Trim

                Dim corlibRef = CreateCompilationWithoutReferences(corlibSource).EmitToImageReference()
                Dim publicLibRef = CreateCompilationWithReferences(WrapInCompilationXml(String.Format(libSourceTemplate, "Public")), {corlibRef}).EmitToImageReference()
                Dim internalLibRef = CreateCompilationWithReferences(WrapInCompilationXml(String.Format(libSourceTemplate, "Friend")), {corlibRef}).EmitToImageReference()

                Dim comp = CreateCompilationWithReferences({}, {corlibRef, publicLibRef, internalLibRef}, assemblyName:="Test")

                Dim wellKnown = comp.GetWellKnownType(WellKnownType.System_Type)
                Assert.NotNull(wellKnown)
                Assert.Equal(TypeKind.Error, wellKnown.TypeKind)

                Dim Lookup = comp.GetTypeByMetadataName("System.Type")
                Assert.Null(Lookup) ' Ambiguous
            End If
        End Sub

        <Fact>
        <WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")>
        Public Sub AllSpecialTypes()
            Dim comp = CreateCompilationWithReferences((<compilation/>), {MscorlibRef_v4_0_30316_17626})

            For special As SpecialType = CType(SpecialType.None + 1, SpecialType) To SpecialType.Count
                Dim symbol = comp.GetSpecialType(special)
                Assert.NotNull(symbol)
                Assert.NotEqual(SymbolKind.ErrorType, symbol.Kind)
            Next
        End Sub

        <Fact>
        <WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")>
        Public Sub AllSpecialTypeMembers()
            Dim comp = CreateCompilationWithReferences((<compilation/>), {MscorlibRef_v4_0_30316_17626})

            For Each special As SpecialMember In [Enum].GetValues(GetType(SpecialMember))
                Select Case special
                    Case SpecialMember.System_IntPtr__op_Explicit_ToPointer,
                         SpecialMember.System_IntPtr__op_Explicit_FromPointer,
                         SpecialMember.System_UIntPtr__op_Explicit_ToPointer,
                         SpecialMember.System_UIntPtr__op_Explicit_FromPointer
                        ' VB doesn't have pointer types.
                        Continue For
                    Case SpecialMember.Count
                        ' Not a real value.
                        Continue For
                End Select

                Dim symbol = comp.GetSpecialTypeMember(special)
                Assert.NotNull(symbol)
            Next
        End Sub

        <Fact>
        <WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")>
        Public Sub AllWellKnownTypes()
            Dim refs As MetadataReference() =
            {
                MscorlibRef_v4_0_30316_17626,
                SystemRef_v4_0_30319_17929,
                SystemCoreRef_v4_0_30319_17929,
                CSharpRef,
                SystemXmlRef,
                SystemXmlLinqRef,
                SystemWindowsFormsRef
            }.Concat(WinRtRefs).ToArray()

            Dim comp = CreateCompilationWithReferences((<compilation/>), refs.Concat(MsvbRef_v4_0_30319_17929).ToArray())
            For wkt = WellKnownType.First To WellKnownType.Last
                Select Case wkt
                    Case WellKnownType.Microsoft_VisualBasic_CompilerServices_EmbeddedOperators
                        ' Only present when embedding VB Core.
                        Continue For
                    Case WellKnownType.System_FormattableString,
                         WellKnownType.System_Runtime_CompilerServices_FormattableStringFactory,
                         WellKnownType.System_Runtime_CompilerServices_NullableAttribute
                        ' Not available on all platforms.
                        Continue For
                End Select

                Dim symbol = comp.GetWellKnownType(wkt)
                Assert.NotNull(symbol)
                Assert.NotEqual(SymbolKind.ErrorType, symbol.Kind)
            Next

            comp = CreateCompilationWithReferences(<compilation/>, refs, TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(True))
            For wkt = WellKnownType.First To WellKnownType.Last
                Select Case wkt
                    Case WellKnownType.Microsoft_VisualBasic_CallType,
                         WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,
                         WellKnownType.Microsoft_VisualBasic_CompilerServices_NewLateBinding,
                         WellKnownType.Microsoft_VisualBasic_CompilerServices_LikeOperator,
                         WellKnownType.Microsoft_VisualBasic_CompilerServices_StringType,
                         WellKnownType.Microsoft_VisualBasic_CompilerServices_Versioned,
                         WellKnownType.Microsoft_VisualBasic_CompareMethod,
                         WellKnownType.Microsoft_VisualBasic_ErrObject,
                         WellKnownType.Microsoft_VisualBasic_FileSystem,
                         WellKnownType.Microsoft_VisualBasic_ApplicationServices_ApplicationBase,
                         WellKnownType.Microsoft_VisualBasic_ApplicationServices_WindowsFormsApplicationBase,
                         WellKnownType.Microsoft_VisualBasic_Information,
                         WellKnownType.Microsoft_VisualBasic_Interaction
                        ' Not embedded, so not available.
                        Continue For
                    Case WellKnownType.System_FormattableString,
                         WellKnownType.System_Runtime_CompilerServices_FormattableStringFactory,
                         WellKnownType.System_Runtime_CompilerServices_NullableAttribute
                        ' Not available on all platforms.
                        Continue For
                End Select

                Dim symbol = comp.GetWellKnownType(wkt)
                Assert.NotNull(symbol)
                Assert.NotEqual(SymbolKind.ErrorType, symbol.Kind)
            Next
        End Sub

        <Fact>
        <WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")>
        Public Sub AllWellKnownTypeMembers()
            Dim refs As MetadataReference() =
            {
                MscorlibRef_v4_0_30316_17626,
                SystemRef_v4_0_30319_17929,
                SystemCoreRef_v4_0_30319_17929,
                CSharpRef,
                SystemXmlRef,
                SystemXmlLinqRef,
                SystemWindowsFormsRef
            }.Concat(WinRtRefs).ToArray()

            Dim comp = CreateCompilationWithReferences((<compilation/>), refs.Concat(MsvbRef_v4_0_30319_17929).ToArray())
            For Each wkm As WellKnownMember In [Enum].GetValues(GetType(WellKnownMember))
                Select Case wkm
                    Case WellKnownMember.Microsoft_VisualBasic_CompilerServices_EmbeddedOperators__CompareStringStringStringBoolean
                        ' Only present when embedding VB Core.
                        Continue For
                    Case WellKnownMember.Count
                        ' Not a real value.
                        Continue For
                    Case WellKnownMember.System_Array__Empty,
                         WellKnownMember.System_Runtime_CompilerServices_NullableAttribute__ctor,
                         WellKnownMember.System_Runtime_CompilerServices_NullableAttribute__ctorTransformFlags
                        ' Not available yet, but will be in upcoming release.
                        Continue For
                End Select

                Dim symbol = comp.GetWellKnownTypeMember(wkm)
                Assert.NotNull(symbol)
            Next

            comp = CreateCompilationWithReferences(<compilation/>, refs, TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(True))
            For Each wkm As WellKnownMember In [Enum].GetValues(GetType(WellKnownMember))
                Select Case wkm
                    Case WellKnownMember.Count
                        ' Not a real value.
                        Continue For
                    Case WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ChangeType,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_ObjectFlowControl_ForLoopControl__ForLoopInitObj,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_ObjectFlowControl_ForLoopControl__ForNextCheckObj,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_ObjectFlowControl__CheckForSyncLockOnValueType,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_ProjectData__CreateProjectError,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_ProjectData__EndApp,
                         WellKnownMember.Microsoft_VisualBasic_Strings__AscCharInt32,
                         WellKnownMember.Microsoft_VisualBasic_Strings__AscStringInt32,
                         WellKnownMember.Microsoft_VisualBasic_Strings__ChrInt32Char
                        ' Even though the containing type is embedded, the specific member is not.
                        Continue For
                    Case WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__PlusObjectObject,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__NegateObjectObject,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__NotObjectObject,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__AndObjectObjectObject,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__OrObjectObjectObject,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__XorObjectObjectObject,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__AddObjectObjectObject,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__SubtractObjectObjectObject,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__MultiplyObjectObjectObject,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__DivideObjectObjectObject,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ExponentObjectObjectObject,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ModObjectObjectObject,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__IntDivideObjectObjectObject,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__LeftShiftObjectObjectObject,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__RightShiftObjectObjectObject,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ConcatenateObjectObjectObject,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectEqualObjectObjectBoolean,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectNotEqualObjectObjectBoolean,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectLessObjectObjectBoolean,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectLessEqualObjectObjectBoolean,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectGreaterEqualObjectObjectBoolean,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectGreaterObjectObjectBoolean,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectEqualObjectObjectBoolean,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectNotEqualObjectObjectBoolean,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectLessObjectObjectBoolean,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectLessEqualObjectObjectBoolean,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectGreaterEqualObjectObjectBoolean,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectGreaterObjectObjectBoolean,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareStringStringStringBoolean,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateCall,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateGet,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateSet,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateSetComplex,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateIndexGet,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateIndexSet,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateIndexSetComplex,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_StringType__MidStmtStr,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_LikeOperator__LikeStringStringStringCompareMethod,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_LikeOperator__LikeObjectObjectObjectCompareMethod,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Versioned__CallByName,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Versioned__IsNumeric,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Versioned__SystemTypeName,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Versioned__TypeName,
                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Versioned__VbTypeName,
                         WellKnownMember.Microsoft_VisualBasic_Information__IsNumeric,
                         WellKnownMember.Microsoft_VisualBasic_Information__SystemTypeName,
                         WellKnownMember.Microsoft_VisualBasic_Information__TypeName,
                         WellKnownMember.Microsoft_VisualBasic_Information__VbTypeName,
                         WellKnownMember.Microsoft_VisualBasic_Interaction__CallByName
                        ' The type is not embedded, so the member is not available.
                        Continue For
                    Case WellKnownMember.System_Array__Empty,
                         WellKnownMember.System_Runtime_CompilerServices_NullableAttribute__ctor,
                         WellKnownMember.System_Runtime_CompilerServices_NullableAttribute__ctorTransformFlags
                        ' Not available yet, but will be in upcoming release.
                        Continue For
                End Select

                Dim symbol = comp.GetWellKnownTypeMember(wkm)
                Assert.NotNull(symbol)
            Next
        End Sub
    End Class
End Namespace
