' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Partial Friend Class SourceNamedTypeSymbol

        ''' <summary>
        ''' Encapsulates ComClass specific data and analysis.
        ''' </summary>
        Private Class ComClassData

            ' Attribute values.
            Public ReadOnly ClassId As String
            Public ReadOnly InterfaceId As String
            Public ReadOnly EventId As String
            Public ReadOnly InterfaceShadows As Boolean

            ' Data created in response to the attribute values

            ''' <summary>
            ''' Synthesized ComClass interfaces, can have the following values:
            '''     Null - not yet initialized,
            '''     Empty - there are no synthesized ComClass interfaces.
            '''     one interface - only class interface is synthesized.
            '''     two interfaces - both class interface and event interface are synthesized. Class interface is followed by the event interface.
            ''' </summary>
            Private _syntheticInterfaces As ImmutableArray(Of NamedTypeSymbol)

            Public Sub New(attrData As VisualBasicAttributeData)

                Dim args As ImmutableArray(Of TypedConstant) = attrData.CommonConstructorArguments

                If args.Length > 0 Then
                    Dim strVal As String = If(args(0).Kind <> TypedConstantKind.Array, TryCast(args(0).ValueInternal, String), Nothing)

                    If Not String.IsNullOrEmpty(strVal) Then
                        Me.ClassId = strVal
                    End If

                    If args.Length > 1 Then
                        strVal = If(args(1).Kind <> TypedConstantKind.Array, TryCast(args(1).ValueInternal, String), Nothing)
                        If Not String.IsNullOrEmpty(strVal) Then
                            Me.InterfaceId = strVal
                        End If

                        If args.Length > 2 Then
                            strVal = If(args(2).Kind <> TypedConstantKind.Array, TryCast(args(2).ValueInternal, String), Nothing)
                            If Not String.IsNullOrEmpty(strVal) Then
                                Me.EventId = strVal
                            End If
                        End If
                    End If
                End If

                Me.InterfaceShadows = attrData.DecodeNamedArgument("InterfaceShadows", Microsoft.CodeAnalysis.SpecialType.System_Boolean, False)
            End Sub

            Public Function GetSynthesizedInterfaces() As ImmutableArray(Of NamedTypeSymbol)
                Debug.Assert(Not _syntheticInterfaces.IsDefault)
                Return _syntheticInterfaces
            End Function

            ''' <summary>
            ''' Returns symbol for the event interface or Nothing when event interface is not synthesized.
            ''' </summary>
            Public Function GetSynthesizedEventInterface() As NamedTypeSymbol
                Debug.Assert(Not _syntheticInterfaces.IsDefault)

                If _syntheticInterfaces.Length > 1 Then
                    Return _syntheticInterfaces(1)
                End If

                Return Nothing
            End Function

            Public Function GetSynthesizedImplements() As IEnumerable(Of NamedTypeSymbol)
                Debug.Assert(Not _syntheticInterfaces.IsDefault)

                If _syntheticInterfaces.IsEmpty Then
                    Return Nothing
                End If

                Return SpecializedCollections.SingletonEnumerable(Of NamedTypeSymbol)(_syntheticInterfaces(0))
            End Function

            Public Function GetCorrespondingComClassInterfaceMethod(method As MethodSymbol) As MethodSymbol
                Debug.Assert(Not _syntheticInterfaces.IsDefault)

                If _syntheticInterfaces.IsEmpty Then
                    Return Nothing
                End If

                For Each m In _syntheticInterfaces(0).GetMembers()
                    If m.Kind = SymbolKind.Method Then
                        Dim comMethod = DirectCast(m, SynthesizedComMethod)

                        If comMethod.ClonedFrom Is method Then
                            Return comMethod
                        End If
                    End If
                Next

                Return Nothing
            End Function

            ''' <summary>
            ''' Perform ComClass specific validation and prepare for metadata generation.
            ''' </summary>
            Public Sub PerformComClassAnalysis(comClass As SourceNamedTypeSymbol)
                If Not _syntheticInterfaces.IsDefault Then
                    Return
                End If

                Dim diagnostics = BindingDiagnosticBag.GetInstance()
                Dim interfaces As ImmutableArray(Of NamedTypeSymbol) = ImmutableArray(Of NamedTypeSymbol).Empty
                Dim interfaceMembers = ArrayBuilder(Of KeyValuePair(Of Symbol, Integer)).GetInstance()
                Dim eventMembers = ArrayBuilder(Of KeyValuePair(Of EventSymbol, Integer)).GetInstance()

                ' Validate the class guid
                ValidateComClassGuid(comClass, ClassId, diagnostics)

                ' Validate the interface guid
                ' Validate the event guid
                Dim interfaceGuid As Guid
                Dim eventGuid As Guid

                ' Note, using [And] rather than [AndAlso] to make sure both guids are always validated.
                If ValidateComClassGuid(comClass, InterfaceId, diagnostics, interfaceGuid) And ValidateComClassGuid(comClass, EventId, diagnostics, eventGuid) Then
                    ' Can't specify the same value for iid and eventsid
                    ' It is not an error to reuse the classid guid though.
                    If InterfaceId IsNot Nothing AndAlso EventId IsNot Nothing AndAlso interfaceGuid = eventGuid Then
                        Binder.ReportDiagnostic(diagnostics, comClass.Locations(0), ERRID.ERR_ComClassDuplicateGuids1, comClass.Name)
                    End If
                End If

                ' Can't specify ComClass and Guid
                If comClass.HasGuidAttribute() Then
                    Binder.ReportDiagnostic(diagnostics, comClass.Locations(0), ERRID.ERR_ComClassAndReservedAttribute1, AttributeDescription.GuidAttribute.Name)
                End If

                ' Can't specify ComClass and ClassInterface
                If comClass.HasClassInterfaceAttribute() Then
                    Binder.ReportDiagnostic(diagnostics, comClass.Locations(0), ERRID.ERR_ComClassAndReservedAttribute1, AttributeDescription.ClassInterfaceAttribute.Name)
                End If

                ' Can't specify ComClass and ComSourceInterfaces
                If comClass.HasComSourceInterfacesAttribute() Then
                    Binder.ReportDiagnostic(diagnostics, comClass.Locations(0), ERRID.ERR_ComClassAndReservedAttribute1, AttributeDescription.ComSourceInterfacesAttribute.Name)
                End If

                ' Can't specify ComClass and ComVisible(False)
                If Not GetComVisibleState(comClass) Then
                    Binder.ReportDiagnostic(diagnostics, comClass.Locations(0), ERRID.ERR_ComClassAndReservedAttribute1, AttributeDescription.ComVisibleAttribute.Name & "(False)")
                End If

                'Class must be Public
                If comClass.DeclaredAccessibility <> Accessibility.Public Then
                    Binder.ReportDiagnostic(diagnostics, comClass.Locations(0), ERRID.ERR_ComClassRequiresPublicClass1, comClass.Name)
                Else
                    Dim container As NamedTypeSymbol = comClass.ContainingType

                    While container IsNot Nothing
                        If container.DeclaredAccessibility <> Accessibility.Public Then
                            Binder.ReportDiagnostic(diagnostics, comClass.Locations(0), ERRID.ERR_ComClassRequiresPublicClass2,
                                                    comClass.Name, container.Name)
                            Exit While
                        End If

                        container = container.ContainingType
                    End While
                End If

                ' Class cannot be Abstract
                If comClass.IsMustInherit Then
                    Binder.ReportDiagnostic(diagnostics, comClass.Locations(0), ERRID.ERR_ComClassCantBeAbstract0)
                End If

                ' Check for nest type name collisions on this class and
                ' on all base classes.
                CheckForNameCollisions(comClass, diagnostics)

                Dim haveDefaultProperty As Boolean
                GetComClassMembers(comClass, interfaceMembers, eventMembers, haveDefaultProperty, diagnostics)

                If interfaceMembers.Count = 0 AndAlso eventMembers.Count = 0 Then
                    Binder.ReportDiagnostic(diagnostics, comClass.Locations(0), ERRID.WRN_ComClassNoMembers1, comClass.Name)

                ElseIf Not diagnostics.HasAnyErrors() Then
                    Dim comClassInterface As NamedTypeSymbol = New SynthesizedComInterface(comClass, interfaceMembers)

                    If eventMembers.Count = 0 Then
                        interfaces = ImmutableArray.Create(comClassInterface)
                    Else
                        interfaces = ImmutableArray.Create(Of NamedTypeSymbol)(comClassInterface,
                                                                              New SynthesizedComInterface(comClass, eventMembers))
                    End If
                End If

                ' If any Id is not empty, we should be able to emit GuidAttribute.
                If ClassId IsNot Nothing OrElse
                   (InterfaceId IsNot Nothing AndAlso interfaces.Length > 0) OrElse
                   (EventId IsNot Nothing AndAlso interfaces.Length > 1) Then
                    Binder.ReportUseSiteInfoForSynthesizedAttribute(WellKnownMember.System_Runtime_InteropServices_GuidAttribute__ctor,
                                                                     comClass.DeclaringCompilation,
                                                                     comClass.Locations(0),
                                                                     diagnostics)
                End If

                ' Should be able to emit ClassInterfaceAttribute.
                Binder.ReportUseSiteInfoForSynthesizedAttribute(WellKnownMember.System_Runtime_InteropServices_ClassInterfaceAttribute__ctorClassInterfaceType,
                                                                 comClass.DeclaringCompilation,
                                                                 comClass.Locations(0),
                                                                 diagnostics)

                ' Should be able to emit ComSourceInterfacesAttribute and InterfaceTypeAttribute if there is an event interface.
                If interfaces.Length > 1 Then
                    Binder.ReportUseSiteInfoForSynthesizedAttribute(WellKnownMember.System_Runtime_InteropServices_ComSourceInterfacesAttribute__ctorString,
                                                                     comClass.DeclaringCompilation,
                                                                     comClass.Locations(0),
                                                                     diagnostics)

                    Binder.ReportUseSiteInfoForSynthesizedAttribute(WellKnownMember.System_Runtime_InteropServices_InterfaceTypeAttribute__ctorInt16,
                                                                     comClass.DeclaringCompilation,
                                                                     comClass.Locations(0),
                                                                     diagnostics)
                End If

                ' Should be able to emit ComVisibleAttribute on interfaces.
                If interfaces.Length > 0 Then
                    Binder.ReportUseSiteInfoForSynthesizedAttribute(WellKnownMember.System_Runtime_InteropServices_ComVisibleAttribute__ctor,
                                                                     comClass.DeclaringCompilation,
                                                                     comClass.Locations(0),
                                                                     diagnostics)
                End If

                Dim synthesizeDispIds As Boolean = False

                For Each pair In interfaceMembers
                    If pair.Key IsNot Nothing AndAlso pair.Value = ReservedDispId.None Then
                        synthesizeDispIds = True
                        Exit For
                    End If
                Next

                If Not synthesizeDispIds Then
                    For Each pair In eventMembers
                        Debug.Assert(pair.Key IsNot Nothing)
                        If pair.Value = ReservedDispId.None Then
                            synthesizeDispIds = True
                            Exit For
                        End If
                    Next
                End If

                If synthesizeDispIds Then
                    ' Should be able to emit DispIdAttribute on members.
                    Binder.ReportUseSiteInfoForSynthesizedAttribute(WellKnownMember.System_Runtime_InteropServices_DispIdAttribute__ctor,
                                                                     comClass.DeclaringCompilation,
                                                                     comClass.Locations(0),
                                                                     diagnostics)
                End If

                If haveDefaultProperty Then
                    ' Should be able to emit DefaultMemberAttribute.
                    Binder.ReportUseSiteInfoForSynthesizedAttribute(WellKnownMember.System_Reflection_DefaultMemberAttribute__ctor,
                                                                     comClass.DeclaringCompilation,
                                                                     comClass.Locations(0),
                                                                     diagnostics)
                End If

                interfaceMembers.Free()
                eventMembers.Free()
                comClass.ContainingSourceModule.AtomicStoreArrayAndDiagnostics(_syntheticInterfaces, interfaces, diagnostics)

                diagnostics.Free()
            End Sub

            Private Shared Function ValidateComClassGuid(comClass As SourceNamedTypeSymbol, id As String, diagnostics As BindingDiagnosticBag, <Out> Optional ByRef guidVal As Guid = Nothing) As Boolean
                If id IsNot Nothing Then
                    If Not Guid.TryParseExact(id, "D", guidVal) Then '32 digits separated by hyphens: 00000000-0000-0000-0000-000000000000 
                        Binder.ReportDiagnostic(diagnostics, comClass.Locations(0), ERRID.ERR_BadAttributeUuid2, AttributeDescription.VisualBasicComClassAttribute.Name, id)
                        Return False
                    End If
                Else
                    guidVal = Nothing
                End If

                Return True
            End Function

            ''' <summary>
            ''' Return False if ComVisibleAttribute(False) is applied to the symbol, True otherwise.
            ''' </summary>
            Private Shared Function GetComVisibleState(target As Symbol) As Boolean
                ' So far this information is used only by ComClass feature, therefore, I do not believe
                ' it is worth to intercept this attribute in DecodeWellKnownAttribute and cache the fact of attribute's
                ' presence and its value. If we start caching that information, implementation of this function 
                ' should change to take advantage of the cache.
                Dim attrData As ImmutableArray(Of VisualBasicAttributeData) = target.GetAttributes()
                Dim comVisible = attrData.IndexOfAttribute(target, AttributeDescription.ComVisibleAttribute)

                If comVisible > -1 Then
                    Dim typedValue As TypedConstant = attrData(comVisible).CommonConstructorArguments(0)
                    Dim value As Object = If(typedValue.Kind <> TypedConstantKind.Array, typedValue.ValueInternal, Nothing)

                    If value Is Nothing OrElse (TypeOf value Is Boolean AndAlso Not DirectCast(value, Boolean)) Then
                        Return False
                    Else
                        ' We will return True in case of type mismatch as well, that should be fine as an error will be reported elsewhere.
                        Return True
                    End If
                End If

                Return True
            End Function

            Private Sub CheckForNameCollisions(comClass As SourceNamedTypeSymbol, diagnostics As BindingDiagnosticBag)
                For i As Integer = 0 To 1
                    Dim interfaceName As String = If(i = 0, "_", "__") & comClass.Name

                    For Each member As Symbol In comClass.GetMembers(interfaceName)
                        ' Error--name collision on this class
                        Binder.ReportDiagnostic(diagnostics, member.Locations(0), ERRID.ERR_MemberConflictWithSynth4,
                                                SyntaxFacts.GetText(SyntaxKind.InterfaceKeyword) & " " & interfaceName,
                                                AttributeDescription.VisualBasicComClassAttribute.Name,
                                                SyntaxFacts.GetText(SyntaxKind.ClassKeyword),
                                                comClass.Name)
                    Next

                    If Not Me.InterfaceShadows Then
                        Dim container As NamedTypeSymbol = comClass.BaseTypeNoUseSiteDiagnostics
                        While container IsNot Nothing
                            For Each member As Symbol In container.GetMembers(interfaceName)
                                If member.DeclaredAccessibility <> Accessibility.Private Then
                                    ' Warning--name shadows a base class member

                                    Binder.ReportDiagnostic(diagnostics, comClass.Locations(0), ERRID.WRN_ComClassInterfaceShadows5,
                                                            comClass.Name,
                                                            SyntaxFacts.GetText(SyntaxKind.InterfaceKeyword),
                                                            interfaceName,
                                                            SyntaxFacts.GetText(SyntaxKind.ClassKeyword),
                                                            container)
                                End If
                            Next

                            container = container.BaseTypeNoUseSiteDiagnostics
                        End While
                    End If
                Next
            End Sub

            Private Sub GetComClassMembers(
                comClass As SourceNamedTypeSymbol,
                interfaceMembers As ArrayBuilder(Of KeyValuePair(Of Symbol, Integer)),
                eventMembers As ArrayBuilder(Of KeyValuePair(Of EventSymbol, Integer)),
                <Out> ByRef haveDefaultProperty As Boolean,
                diagnostics As BindingDiagnosticBag
            )
                haveDefaultProperty = False

                For Each member As Symbol In comClass.GetMembers()
                    If member.IsShared OrElse member.DeclaredAccessibility <> Accessibility.Public OrElse
                       member.IsImplicitlyDeclared Then
                        Continue For
                    End If

                    Dim memberKind As SymbolKind = member.Kind

                    ' Do some early filtering based on kind to avoid looking at attributes.
                    Select Case memberKind
                        Case SymbolKind.Field, SymbolKind.NamedType
                            Continue For
                        Case SymbolKind.Method
                            If DirectCast(member, MethodSymbol).MethodKind <> MethodKind.Ordinary Then
                                Continue For
                            End If
                    End Select

                    ' Filter out members with <ComVisible(False)>
                    If Not GetComVisibleState(member) Then
                        Continue For
                    End If

                    Select Case memberKind
                        Case SymbolKind.Property
                            Dim prop = DirectCast(member, PropertySymbol)
                            If prop.IsWithEvents Then
                                Continue For
                            End If

                            Dim getter As MethodSymbol = prop.GetMethod
                            Dim setter As MethodSymbol = prop.SetMethod

                            If getter IsNot Nothing Then
                                If getter.IsImplicitlyDeclared Then
                                    Continue For ' Note, this will exclude auto-properties (matching Dev10 behavior).
                                End If

                                If getter.DeclaredAccessibility <> Accessibility.Public OrElse Not GetComVisibleState(getter) Then
                                    getter = Nothing
                                End If
                            End If

                            If setter IsNot Nothing Then
                                If setter.IsImplicitlyDeclared Then
                                    Continue For ' Note, this will exclude auto-properties (matching Dev10 behavior).
                                End If

                                If setter.DeclaredAccessibility <> Accessibility.Public OrElse Not GetComVisibleState(setter) Then
                                    setter = Nothing
                                End If
                            End If

                            If getter Is Nothing AndAlso setter Is Nothing Then
                                Continue For
                            End If

                            ' Warn for Property X As Object : Set(ByVal o As Object)
                            If prop.Type.IsObjectType() AndAlso prop.SetMethod IsNot Nothing Then
                                Binder.ReportDiagnostic(diagnostics, prop.Locations(0), ERRID.WRN_ComClassPropertySetObject1, prop)
                            End If

                            interfaceMembers.Add(New KeyValuePair(Of Symbol, Integer)(prop, GetUserSpecifiedDispId(prop, diagnostics)))

                            If prop.IsDefault Then
                                haveDefaultProperty = True
                            End If

                            ' Accessors follow the property, first getter, then setter. 
                            ' If accessor shouldn't be cloned, Nothing is stored.
                            interfaceMembers.Add(New KeyValuePair(Of Symbol, Integer)(getter,
                                                                                      If(getter Is Nothing,
                                                                                         ReservedDispId.None,
                                                                                         GetUserSpecifiedDispId(getter, diagnostics))))
                            interfaceMembers.Add(New KeyValuePair(Of Symbol, Integer)(setter,
                                                                                      If(setter Is Nothing,
                                                                                         ReservedDispId.None,
                                                                                         GetUserSpecifiedDispId(setter, diagnostics))))

                        Case SymbolKind.Event
                            eventMembers.Add(New KeyValuePair(Of EventSymbol, Integer)(DirectCast(member, EventSymbol), GetUserSpecifiedDispId(member, diagnostics)))

                        Case SymbolKind.Method
                            If DirectCast(member, MethodSymbol).IsGenericMethod Then
                                ' Generic methods cannot be exposed to COM
                                Binder.ReportDiagnostic(diagnostics, member.Locations(0), ERRID.ERR_ComClassGenericMethod)
                            End If

                            interfaceMembers.Add(New KeyValuePair(Of Symbol, Integer)(member, GetUserSpecifiedDispId(member, diagnostics)))

                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(memberKind)
                    End Select
                Next

            End Sub

            Private Enum ReservedDispId
                None = -1
                DISPID_VALUE = 0
                DISPID_NEWENUM = -4
            End Enum

            ''' <summary>
            ''' Returns user defined DispId for a member or ReservedDispId.None if none specified.
            ''' Also reports errors for reserved DispIds.
            ''' </summary>
            Private Shared Function GetUserSpecifiedDispId(target As Symbol, diagnostics As BindingDiagnosticBag) As Integer
                ' So far this information is used only by ComClass feature, therefore, I do not believe
                ' it is worth to intercept this attribute in DecodeWellKnownAttribute and cache the fact of attribute's
                ' presence and its value. If we start caching that information, implementation of this function 
                ' should change to take advantage of the cache.
                Dim attrData As ImmutableArray(Of VisualBasicAttributeData) = target.GetAttributes()
                Dim dispIdIndex = attrData.IndexOfAttribute(target, AttributeDescription.DispIdAttribute)

                If dispIdIndex > -1 Then
                    Dim typedValue As TypedConstant = attrData(dispIdIndex).CommonConstructorArguments(0)
                    Dim value As Object = If(typedValue.Kind <> TypedConstantKind.Array, typedValue.ValueInternal, Nothing)

                    If value IsNot Nothing AndAlso TypeOf value Is Integer Then
                        Dim dispId = DirectCast(value, Integer)

                        ' Validate that the user has not used zero which is reserved
                        ' for the Default property.
                        If dispId = 0 Then
                            If target.Kind <> SymbolKind.Property OrElse Not DirectCast(target, PropertySymbol).IsDefault Then
                                Binder.ReportDiagnostic(diagnostics, target.Locations(0), ERRID.ERR_ComClassReservedDispIdZero1, target.Name)
                            End If

                            ' Validate that the user has not used negative DispId's which
                            ' are reserved by COM and the runtime.
                        ElseIf dispId < 0 Then
                            Binder.ReportDiagnostic(diagnostics, target.Locations(0), ERRID.ERR_ComClassReservedDispId1, target.Name)
                        End If

                        Return dispId
                    End If
                End If

                Return ReservedDispId.None
            End Function

            Private NotInheritable Class SynthesizedComInterface
                Inherits NamedTypeSymbol

                Private ReadOnly _comClass As SourceNamedTypeSymbol
                Private ReadOnly _isEventInterface As Boolean
                Private ReadOnly _members As ImmutableArray(Of Symbol)
                Private ReadOnly _defaultMemberName As String

                Public Sub New(comClass As SourceNamedTypeSymbol, interfaceMembers As ArrayBuilder(Of KeyValuePair(Of Symbol, Integer)))
                    Debug.Assert(Not comClass.IsGenericType)
                    _comClass = comClass
                    _isEventInterface = False

                    Dim usedDispIds As New HashSet(Of Integer)()

                    For Each pair As KeyValuePair(Of Symbol, Integer) In interfaceMembers
                        If pair.Value <> ReservedDispId.None Then
                            usedDispIds.Add(pair.Value)
                        End If
                    Next

                    Dim members = ArrayBuilder(Of Symbol).GetInstance()

                    ' Assign the dispids, avoiding the ones that the
                    ' user has already set. Start with DispId 1.
                    Dim nextDispId As Integer = 1
                    Const getEnumeratorName As String = "GetEnumerator"

                    For i As Integer = 0 To interfaceMembers.Count - 1
                        Dim pair As KeyValuePair(Of Symbol, Integer) = interfaceMembers(i)
                        Dim member As Symbol = pair.Key
                        Dim dispId As Integer = pair.Value
                        Dim synthesizedDispId As Integer

                        Select Case member.Kind
                            Case SymbolKind.Method
                                Dim method = DirectCast(member, MethodSymbol)
                                Debug.Assert(method.MethodKind = MethodKind.Ordinary)

                                If dispId = ReservedDispId.None Then
                                    synthesizedDispId = GetNextAvailableDispId(usedDispIds, nextDispId)

                                    ' Check for special dispids. Do this after incrementing NextDispId
                                    ' so we will keep the nth item as DispId n.
                                    If CaseInsensitiveComparison.Equals(method.Name, getEnumeratorName) AndAlso
                                       method.ParameterCount = 0 AndAlso
                                       method.ReturnType.SpecialType = SpecialType.System_Collections_IEnumerator Then
                                        synthesizedDispId = ReservedDispId.DISPID_NEWENUM
                                    End If
                                Else
                                    synthesizedDispId = ReservedDispId.None ' Do not synthesize.
                                End If

                                members.Add(New SynthesizedComMethod(Me, method, synthesizedDispId))
                            Case SymbolKind.Property
                                Dim prop As PropertySymbol = DirectCast(member, PropertySymbol)
                                Dim getter As SynthesizedComMethod = Nothing
                                Dim setter As SynthesizedComMethod = Nothing

                                If _defaultMemberName Is Nothing AndAlso prop.IsDefault Then
                                    _defaultMemberName = prop.Name
                                End If

                                ' Accessors follow the property, first getter, then setter. 
                                ' If accessor shouldn't be cloned, Nothing is stored.
                                i += 1
                                Dim getterPair As KeyValuePair(Of Symbol, Integer) = interfaceMembers(i)
                                i += 1
                                Dim setterPair As KeyValuePair(Of Symbol, Integer) = interfaceMembers(i)

                                If dispId = ReservedDispId.None OrElse
                                   (getterPair.Key IsNot Nothing AndAlso getterPair.Value = ReservedDispId.None) OrElse
                                   (setterPair.Key IsNot Nothing AndAlso setterPair.Value = ReservedDispId.None) Then

                                    synthesizedDispId = GetNextAvailableDispId(usedDispIds, nextDispId)

                                    ' Check for special dispids. Do this after incrementing NextDispId
                                    ' so we will keep the nth item as DispId n.
                                    If CaseInsensitiveComparison.Equals(prop.Name, getEnumeratorName) AndAlso
                                       prop.ParameterCount = 0 AndAlso
                                       prop.Type.SpecialType = SpecialType.System_Collections_IEnumerator Then
                                        synthesizedDispId = ReservedDispId.DISPID_NEWENUM
                                    ElseIf prop.IsDefault Then
                                        synthesizedDispId = ReservedDispId.DISPID_VALUE
                                    ElseIf dispId <> ReservedDispId.None Then
                                        synthesizedDispId = dispId ' use the user's Property DispId for the Get and Set
                                    End If
                                Else
                                    synthesizedDispId = ReservedDispId.None
                                End If

                                If getterPair.Key IsNot Nothing Then
                                    getter = New SynthesizedComMethod(Me, DirectCast(getterPair.Key, MethodSymbol),
                                                                      If(getterPair.Value = ReservedDispId.None, synthesizedDispId, ReservedDispId.None))
                                End If

                                If setterPair.Key IsNot Nothing Then
                                    setter = New SynthesizedComMethod(Me, DirectCast(setterPair.Key, MethodSymbol),
                                                                      If(setterPair.Value = ReservedDispId.None, synthesizedDispId, ReservedDispId.None))
                                End If

                                If getter IsNot Nothing Then
                                    If setter IsNot Nothing Then
                                        If LexicalOrderSymbolComparer.Instance.Compare(prop.GetMethod, prop.SetMethod) <= 0 Then
                                            members.Add(getter)
                                            members.Add(setter)
                                        Else
                                            members.Add(setter)
                                            members.Add(getter)
                                        End If
                                    Else
                                        members.Add(getter)
                                    End If
                                Else
                                    Debug.Assert(setter IsNot Nothing)
                                    members.Add(setter)
                                End If

                                members.Add(New SynthesizedComProperty(Me, prop, getter, setter,
                                                                       If(dispId = ReservedDispId.None, synthesizedDispId, ReservedDispId.None)))
                            Case Else
                                Throw ExceptionUtilities.UnexpectedValue(member.Kind)
                        End Select
                    Next

                    _members = members.ToImmutableAndFree()
                End Sub

                Public Overrides Function GetHashCode() As Integer
                    Return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Me)
                End Function

                Public Overrides Function Equals(other As TypeSymbol, comparison As TypeCompareKind) As Boolean
                    Return Me Is other
                End Function

                Private Shared Function GetNextAvailableDispId(
                    usedDispIds As HashSet(Of Integer),
                    <[In], Out> ByRef nextDispId As Integer
                ) As Integer
                    Dim dispId As Integer = nextDispId

                    While usedDispIds.Contains(dispId)
                        dispId += 1
                    End While

                    nextDispId = dispId + 1

                    Return dispId
                End Function

                Public Sub New(comClass As SourceNamedTypeSymbol, interfaceMembers As ArrayBuilder(Of KeyValuePair(Of EventSymbol, Integer)))
                    _comClass = comClass
                    _isEventInterface = True

                    Dim usedDispIds As New HashSet(Of Integer)()

                    For Each pair As KeyValuePair(Of EventSymbol, Integer) In interfaceMembers
                        If pair.Value <> ReservedDispId.None Then
                            usedDispIds.Add(pair.Value)
                        End If
                    Next

                    Dim members = ArrayBuilder(Of Symbol).GetInstance()

                    ' Assign the dispids, avoiding the ones that the
                    ' user has already set. Start with DispId 1.
                    Dim nextDispId As Integer = 1

                    For Each pair As KeyValuePair(Of EventSymbol, Integer) In interfaceMembers
                        Dim member As EventSymbol = pair.Key

                        If member.Type.IsDelegateType() Then
                            Dim invoke As MethodSymbol = DirectCast(member.Type, NamedTypeSymbol).DelegateInvokeMethod

                            If invoke IsNot Nothing Then
                                Dim synthesizedDispId As Integer

                                If pair.Value = ReservedDispId.None Then
                                    synthesizedDispId = GetNextAvailableDispId(usedDispIds, nextDispId)
                                Else
                                    synthesizedDispId = ReservedDispId.None ' Do not synthesize.
                                End If

                                members.Add(New SynthesizedComEventMethod(Me, member, invoke, synthesizedDispId))
                            End If
                        End If
                    Next

                    _members = members.ToImmutableAndFree()
                End Sub

                Public ReadOnly Property IsEventInterface As Boolean
                    Get
                        Return _isEventInterface
                    End Get
                End Property

                Public ReadOnly Property ComClass As SourceNamedTypeSymbol
                    Get
                        Return _comClass
                    End Get
                End Property

                Public Overrides ReadOnly Property Arity As Integer
                    Get
                        Return 0
                    End Get
                End Property

                Friend Overrides ReadOnly Property CanConstruct As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Public Overloads Overrides Function Construct(typeArguments As ImmutableArray(Of TypeSymbol)) As NamedTypeSymbol
                    Throw ExceptionUtilities.Unreachable
                End Function

                Public Overrides ReadOnly Property ConstructedFrom As NamedTypeSymbol
                    Get
                        Return Me
                    End Get
                End Property

                Public Overrides ReadOnly Property ContainingSymbol As Symbol
                    Get
                        Return _comClass
                    End Get
                End Property

                Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
                    Get
                        Return Accessibility.Public
                    End Get
                End Property

                Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
                    Get
                        Throw ExceptionUtilities.Unreachable
                    End Get
                End Property

                Friend Overrides ReadOnly Property DefaultPropertyName As String
                    Get
                        Throw ExceptionUtilities.Unreachable
                    End Get
                End Property

                Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
                    Get
                        Return Nothing
                    End Get
                End Property

                Friend Overrides Sub GenerateDeclarationErrors(cancellationToken As Threading.CancellationToken)
                    Throw ExceptionUtilities.Unreachable
                End Sub

                Public Overloads Overrides Function GetMembers() As ImmutableArray(Of Symbol)
                    Return _members
                End Function

                Public Overloads Overrides Function GetMembers(name As String) As ImmutableArray(Of Symbol)
                    Throw ExceptionUtilities.Unreachable
                End Function

                Public Overloads Overrides Function GetTypeMembers() As ImmutableArray(Of NamedTypeSymbol)
                    Return ImmutableArray(Of NamedTypeSymbol).Empty
                End Function

                Public Overloads Overrides Function GetTypeMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)
                    Throw ExceptionUtilities.Unreachable
                End Function

                Public Overloads Overrides Function GetTypeMembers(name As String, arity As Integer) As ImmutableArray(Of NamedTypeSymbol)
                    Throw ExceptionUtilities.Unreachable
                End Function

                Friend Overrides Function GetFieldsToEmit() As IEnumerable(Of FieldSymbol)
                    Return SpecializedCollections.EmptyEnumerable(Of FieldSymbol)()
                End Function

                Friend Overrides ReadOnly Property HasCodeAnalysisEmbeddedAttribute As Boolean
                    Get
                        Throw ExceptionUtilities.Unreachable
                    End Get
                End Property

                Friend Overrides ReadOnly Property HasVisualBasicEmbeddedAttribute As Boolean
                    Get
                        Throw ExceptionUtilities.Unreachable
                    End Get
                End Property

                Friend Overrides ReadOnly Property IsExtensibleInterfaceNoUseSiteDiagnostics As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Friend Overrides ReadOnly Property IsWindowsRuntimeImport As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Friend Overrides ReadOnly Property ShouldAddWinRTMembers As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Friend Overrides ReadOnly Property IsComImport As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Friend Overrides ReadOnly Property CoClassType As TypeSymbol
                    Get
                        Return Nothing
                    End Get
                End Property

                Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
                    Return ImmutableArray(Of String).Empty
                End Function

                Friend Overrides Function GetAttributeUsageInfo() As AttributeUsageInfo
                    Throw ExceptionUtilities.Unreachable
                End Function

                Friend Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Friend Overrides Function GetSecurityInformation() As IEnumerable(Of Microsoft.Cci.SecurityAttribute)
                    Throw ExceptionUtilities.Unreachable
                End Function

                Friend Overrides Function InternalSubstituteTypeParameters(substitution As TypeSubstitution) As TypeWithModifiers
                    Throw ExceptionUtilities.Unreachable
                End Function

                Public Overrides ReadOnly Property IsMustInherit As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Public Overrides ReadOnly Property IsNotInheritable As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
                    Get
                        Throw ExceptionUtilities.Unreachable
                    End Get
                End Property

                Friend Overrides Function MakeAcyclicBaseType(diagnostics As BindingDiagnosticBag) As NamedTypeSymbol
                    Return Nothing
                End Function

                Friend Overrides Function MakeAcyclicInterfaces(diagnostics As BindingDiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
                    Return ImmutableArray(Of NamedTypeSymbol).Empty
                End Function

                Friend Overrides Function MakeDeclaredBase(basesBeingResolved As BasesBeingResolved, diagnostics As BindingDiagnosticBag) As NamedTypeSymbol
                    Return Nothing
                End Function

                Friend Overrides Function MakeDeclaredInterfaces(basesBeingResolved As BasesBeingResolved, diagnostics As BindingDiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
                    Return ImmutableArray(Of NamedTypeSymbol).Empty
                End Function

                Friend Overrides ReadOnly Property MangleName As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Public Overrides ReadOnly Property MemberNames As IEnumerable(Of String)
                    Get
                        Throw ExceptionUtilities.Unreachable
                    End Get
                End Property

                Public Overrides ReadOnly Property MightContainExtensionMethods As Boolean
                    Get
                        Throw ExceptionUtilities.Unreachable
                    End Get
                End Property

                Public Overrides ReadOnly Property Name As String
                    Get
                        Return If(_isEventInterface, "__", "_") & _comClass.Name
                    End Get
                End Property

                Friend Overrides ReadOnly Property HasSpecialName As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Public Overrides ReadOnly Property IsSerializable As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Friend Overrides ReadOnly Property Layout As TypeLayout
                    Get
                        Return Nothing
                    End Get
                End Property

                Friend Overrides ReadOnly Property MarshallingCharSet As CharSet
                    Get
                        Return DefaultMarshallingCharSet
                    End Get
                End Property

                Friend Overrides ReadOnly Property TypeArgumentsNoUseSiteDiagnostics As ImmutableArray(Of TypeSymbol)
                    Get
                        Return ImmutableArray(Of TypeSymbol).Empty
                    End Get
                End Property

                Public Overrides Function GetTypeArgumentCustomModifiers(ordinal As Integer) As ImmutableArray(Of CustomModifier)
                    Return GetEmptyTypeArgumentCustomModifiers(ordinal)
                End Function

                Friend Overrides ReadOnly Property HasTypeArgumentsCustomModifiers As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Public Overrides ReadOnly Property TypeKind As TypeKind
                    Get
                        Return TypeKind.Interface
                    End Get
                End Property

                Friend Overrides ReadOnly Property IsInterface As Boolean
                    Get
                        Return True
                    End Get
                End Property

                Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
                    Get
                        Return ImmutableArray(Of TypeParameterSymbol).Empty
                    End Get
                End Property

                Friend Overrides ReadOnly Property TypeSubstitution As TypeSubstitution
                    Get
                        Return Nothing
                    End Get
                End Property

                Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
                    Return ImmutableArray(Of VisualBasicAttributeData).Empty
                End Function

                Friend Overrides Sub AddSynthesizedAttributes(moduleBuilder As PEModuleBuilder, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
                    MyBase.AddSynthesizedAttributes(moduleBuilder, attributes)

                    Dim compilation As VisualBasicCompilation = _comClass.DeclaringCompilation
                    Dim id As String = If(_isEventInterface, _comClass._comClassData.EventId, _comClass._comClassData.InterfaceId)

                    If id IsNot Nothing Then
                        AddSynthesizedAttribute(attributes, compilation.TrySynthesizeAttribute(
                            WellKnownMember.System_Runtime_InteropServices_GuidAttribute__ctor,
                            ImmutableArray.Create(
                                New TypedConstant(_comClass.GetSpecialType(SpecialType.System_String), TypedConstantKind.Primitive, id))))
                    End If

                    If _isEventInterface Then
                        AddSynthesizedAttribute(attributes, compilation.TrySynthesizeAttribute(
                            WellKnownMember.System_Runtime_InteropServices_InterfaceTypeAttribute__ctorInt16,
                            ImmutableArray.Create(
                                New TypedConstant(_comClass.GetSpecialType(SpecialType.System_Int16),
                                                        TypedConstantKind.Primitive,
                                                        CShort(Cci.Constants.ComInterfaceType_InterfaceIsIDispatch)))))
                    End If

                    AddSynthesizedAttribute(attributes, compilation.TrySynthesizeAttribute(
                        WellKnownMember.System_Runtime_InteropServices_ComVisibleAttribute__ctor,
                        ImmutableArray.Create(New TypedConstant(_comClass.GetSpecialType(SpecialType.System_Boolean),
                                                                        TypedConstantKind.Primitive,
                                                                        value:=True))))

                    If _defaultMemberName IsNot Nothing Then
                        AddSynthesizedAttribute(attributes, compilation.TrySynthesizeAttribute(
                            WellKnownMember.System_Reflection_DefaultMemberAttribute__ctor,
                            ImmutableArray.Create(New TypedConstant(_comClass.GetSpecialType(SpecialType.System_String),
                                                                            TypedConstantKind.Primitive,
                                                                            _defaultMemberName))))
                    End If
                End Sub

                Friend Overrides Function GetUnificationUseSiteDiagnosticRecursive(owner As Symbol, ByRef checkedTypes As HashSet(Of TypeSymbol)) As DiagnosticInfo
                    Return Nothing
                End Function

                Friend Overrides Function GetSynthesizedWithEventsOverrides() As IEnumerable(Of PropertySymbol)
                    Return SpecializedCollections.EmptyEnumerable(Of PropertySymbol)()
                End Function

                Friend Overrides ReadOnly Property HasAnyDeclaredRequiredMembers As Boolean
                    Get
                        Return False
                    End Get
                End Property
            End Class

            Private Class SynthesizedComMethod
                Inherits MethodSymbol

                Public ReadOnly ClonedFrom As MethodSymbol
                Private ReadOnly _synthesizedDispId As Integer ' ReservedDispId.None if shouldn't be synthesized 
                Private ReadOnly _interface As SynthesizedComInterface
                Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)

                Public Sub New(container As SynthesizedComInterface, clone As MethodSymbol, synthesizedDispId As Integer)
                    _interface = container
                    _synthesizedDispId = synthesizedDispId
                    Debug.Assert(clone.DeclaredAccessibility = Accessibility.Public)
                    ClonedFrom = clone

                    If clone.ParameterCount = 0 Then
                        _parameters = ImmutableArray(Of ParameterSymbol).Empty
                    Else
                        Dim parameters(clone.ParameterCount - 1) As ParameterSymbol

                        For i As Integer = 0 To parameters.Length - 1
                            parameters(i) = New SynthesizedComParameter(Me, clone.Parameters(i))
                        Next

                        _parameters = parameters.AsImmutable()
                    End If
                End Sub

                Protected Overridable ReadOnly Property NameAndAttributesSource As Symbol
                    Get
                        Return ClonedFrom
                    End Get
                End Property

                Public Overrides ReadOnly Property Name As String
                    Get
                        Return NameAndAttributesSource.Name
                    End Get
                End Property

                Friend Overrides ReadOnly Property HasSpecialName As Boolean
                    Get
                        Debug.Assert(NameAndAttributesSource Is ClonedFrom)
                        Return ClonedFrom.HasSpecialName
                    End Get
                End Property

                Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
                    Return ImmutableArray(Of String).Empty
                End Function

                Public Overrides ReadOnly Property Arity As Integer
                    Get
                        Return 0
                    End Get
                End Property

                Public Overrides ReadOnly Property AssociatedSymbol As Symbol
                    Get
                        Return Nothing ' Shouldn't make any difference for metadata emit.
                    End Get
                End Property

                Friend Overrides ReadOnly Property CallingConvention As Cci.CallingConvention
                    Get
                        Return ClonedFrom.CallingConvention
                    End Get
                End Property

                Public Overrides ReadOnly Property ContainingSymbol As Symbol
                    Get
                        Return _interface
                    End Get
                End Property

                Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
                    Get
                        Return _interface
                    End Get
                End Property

                Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
                    Get
                        Return Accessibility.Public
                    End Get
                End Property

                Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
                    Get
                        Throw ExceptionUtilities.Unreachable
                    End Get
                End Property

                Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
                    Get
                        Throw ExceptionUtilities.Unreachable
                    End Get
                End Property

                Public Overrides ReadOnly Property IsExtensionMethod As Boolean
                    Get
                        Throw ExceptionUtilities.Unreachable
                    End Get
                End Property

                Public Overrides ReadOnly Property IsExternalMethod As Boolean
                    Get
                        Throw ExceptionUtilities.Unreachable
                    End Get
                End Property

                Public Overrides ReadOnly Property IsMustOverride As Boolean
                    Get
                        Return True
                    End Get
                End Property

                Public Overrides ReadOnly Property IsNotOverridable As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Public Overrides ReadOnly Property IsOverloads As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Public Overrides ReadOnly Property IsOverridable As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Public Overrides ReadOnly Property IsOverrides As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Public Overrides ReadOnly Property IsShared As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Public Overrides ReadOnly Property IsSub As Boolean
                    Get
                        Return ClonedFrom.IsSub
                    End Get
                End Property

                Public Overrides ReadOnly Property IsAsync As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Public Overrides ReadOnly Property IsIterator As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Public NotOverridable Overrides ReadOnly Property IsInitOnly As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Public Overrides ReadOnly Property IsVararg As Boolean
                    Get
                        Return ClonedFrom.IsVararg
                    End Get
                End Property

                Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
                    Get
                        Throw ExceptionUtilities.Unreachable
                    End Get
                End Property

                Public Overrides ReadOnly Property MethodKind As MethodKind
                    Get
                        Select Case ClonedFrom.MethodKind
                            Case MethodKind.PropertyGet
                                Return MethodKind.PropertyGet
                            Case MethodKind.PropertySet
                                Return MethodKind.PropertySet
                            Case Else
                                Return MethodKind.Ordinary
                        End Select
                    End Get
                End Property

                Friend NotOverridable Overrides ReadOnly Property IsMethodKindBasedOnSyntax As Boolean
                    Get
                        Return ClonedFrom.IsMethodKindBasedOnSyntax
                    End Get
                End Property

                Public NotOverridable Overrides Function GetDllImportData() As DllImportData
                    Return Nothing
                End Function

                Friend NotOverridable Overrides ReadOnly Property ReturnTypeMarshallingInformation As MarshalPseudoCustomAttributeData
                    Get
                        Return ClonedFrom.ReturnTypeMarshallingInformation
                    End Get
                End Property

                Friend NotOverridable Overrides ReadOnly Property ImplementationAttributes As Reflection.MethodImplAttributes
                    Get
                        Return Nothing
                    End Get
                End Property

                Friend NotOverridable Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Friend NotOverridable Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
                    Get
                        Return Nothing
                    End Get
                End Property

                Friend NotOverridable Overrides Function GetSecurityInformation() As IEnumerable(Of Microsoft.Cci.SecurityAttribute)
                    Throw ExceptionUtilities.Unreachable
                End Function

                Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
                    Get
                        Return _parameters
                    End Get
                End Property

                Public Overrides ReadOnly Property ReturnsByRef As Boolean
                    Get
                        Return ClonedFrom.ReturnsByRef
                    End Get
                End Property

                Public Overrides ReadOnly Property ReturnType As TypeSymbol
                    Get
                        Return ClonedFrom.ReturnType
                    End Get
                End Property

                Public Overrides ReadOnly Property ReturnTypeCustomModifiers As ImmutableArray(Of CustomModifier)
                    Get
                        Return ClonedFrom.ReturnTypeCustomModifiers
                    End Get
                End Property

                Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
                    Get
                        Return ClonedFrom.RefCustomModifiers
                    End Get
                End Property

                Friend Overrides ReadOnly Property Syntax As SyntaxNode
                    Get
                        Throw ExceptionUtilities.Unreachable
                    End Get
                End Property

                Public Overrides ReadOnly Property TypeArguments As ImmutableArray(Of TypeSymbol)
                    Get
                        Return ImmutableArray(Of TypeSymbol).Empty
                    End Get
                End Property

                Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
                    Get
                        Return ImmutableArray(Of TypeParameterSymbol).Empty
                    End Get
                End Property

                Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
                    Dim attributeSource As Symbol = NameAndAttributesSource
                    Dim toClone As ImmutableArray(Of VisualBasicAttributeData) = attributeSource.GetAttributes()

                    If attributeSource.Kind = SymbolKind.Method Then
                        Return toClone
                    End If

                    Dim attributes = ArrayBuilder(Of VisualBasicAttributeData).GetInstance()

                    For Each attrData In toClone

                        Dim attributeUsage = attrData.AttributeClass.GetAttributeUsageInfo()
                        Debug.Assert(Not attributeUsage.IsNull)

                        If (attributeUsage.ValidTargets And AttributeTargets.Method) <> 0 Then
                            attributes.Add(attrData)
                        End If
                    Next

                    If attributes.Count = toClone.Length Then
                        attributes.Free()
                        Return toClone
                    End If

                    Return attributes.ToImmutableAndFree()
                End Function

                Friend Overrides Sub AddSynthesizedAttributes(moduleBuilder As PEModuleBuilder, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
                    MyBase.AddSynthesizedAttributes(moduleBuilder, attributes)

                    If _synthesizedDispId = ReservedDispId.None Then
                        Return
                    End If

                    AddSynthesizedAttribute(attributes, _interface.ComClass.DeclaringCompilation.TrySynthesizeAttribute(
                        WellKnownMember.System_Runtime_InteropServices_DispIdAttribute__ctor,
                        ImmutableArray.Create(New TypedConstant(_interface.ComClass.GetSpecialType(Microsoft.CodeAnalysis.SpecialType.System_Int32),
                                                                        TypedConstantKind.Primitive,
                                                                        _synthesizedDispId))))
                End Sub

                Public Overrides Function GetReturnTypeAttributes() As ImmutableArray(Of VisualBasicAttributeData)
                    Dim attributeSource As Symbol = NameAndAttributesSource

                    If attributeSource.Kind = SymbolKind.Method Then
                        Return DirectCast(attributeSource, MethodSymbol).GetReturnTypeAttributes()
                    End If

                    Return ImmutableArray(Of VisualBasicAttributeData).Empty
                End Function

                Friend NotOverridable Overrides Function IsMetadataNewSlot(Optional ignoreInterfaceImplementationChanges As Boolean = False) As Boolean
                    Return True
                End Function

                Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Friend Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
                    Throw ExceptionUtilities.Unreachable
                End Function

                Friend NotOverridable Overrides ReadOnly Property HasSetsRequiredMembers As Boolean
                    Get
                        Return False
                    End Get
                End Property
            End Class

            Private Class SynthesizedComEventMethod
                Inherits SynthesizedComMethod

                Private ReadOnly _event As EventSymbol

                Public Sub New(container As SynthesizedComInterface, [event] As EventSymbol, clone As MethodSymbol, synthesizedDispId As Integer)
                    MyBase.New(container, clone, synthesizedDispId)
                    _event = [event]
                End Sub

                Protected Overrides ReadOnly Property NameAndAttributesSource As Symbol
                    Get
                        Return _event
                    End Get
                End Property

                Friend Overrides ReadOnly Property HasSpecialName As Boolean
                    Get
                        Return _event.HasSpecialName
                    End Get
                End Property
            End Class

            Private NotInheritable Class SynthesizedComParameter
                Inherits ParameterSymbol

                Private ReadOnly _container As Symbol
                Private ReadOnly _clonedFrom As ParameterSymbol

                Public Sub New(container As SynthesizedComMethod, clone As ParameterSymbol)
                    _container = container
                    _clonedFrom = clone
                End Sub

                Public Sub New(container As SynthesizedComProperty, clone As ParameterSymbol)
                    _container = container
                    _clonedFrom = clone
                End Sub

                Public Overrides ReadOnly Property Name As String
                    Get
                        Return _clonedFrom.Name
                    End Get
                End Property

                Public Overrides ReadOnly Property ContainingSymbol As Symbol
                    Get
                        Return _container
                    End Get
                End Property

                Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
                    Get
                        Return _clonedFrom.CustomModifiers
                    End Get
                End Property

                Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
                    Get
                        Return _clonedFrom.RefCustomModifiers
                    End Get
                End Property

                Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
                    Get
                        Throw ExceptionUtilities.Unreachable
                    End Get
                End Property

                Public ReadOnly Property IsComEventParameter As Boolean
                    Get
                        Return DirectCast(_container.ContainingSymbol, SynthesizedComInterface).IsEventInterface
                    End Get
                End Property

                Friend Overloads Overrides ReadOnly Property ExplicitDefaultConstantValue(inProgress As SymbolsInProgress(Of ParameterSymbol)) As ConstantValue
                    Get
                        If IsComEventParameter Then
                            Return Nothing
                        End If

                        Return _clonedFrom.ExplicitDefaultConstantValue(inProgress)
                    End Get
                End Property

                Public Overrides ReadOnly Property HasExplicitDefaultValue As Boolean
                    Get
                        If IsComEventParameter Then
                            Return False
                        End If

                        Return _clonedFrom.HasExplicitDefaultValue
                    End Get
                End Property

                Public Overrides ReadOnly Property IsByRef As Boolean
                    Get
                        Return _clonedFrom.IsByRef
                    End Get
                End Property

                Friend Overrides ReadOnly Property IsExplicitByRef As Boolean
                    Get
                        Return _clonedFrom.IsExplicitByRef
                    End Get
                End Property

                Public Overrides ReadOnly Property IsOptional As Boolean
                    Get
                        If IsComEventParameter Then
                            Return False
                        End If

                        Return _clonedFrom.IsOptional
                    End Get
                End Property

                Friend Overrides ReadOnly Property IsMetadataOut As Boolean
                    Get
                        If IsComEventParameter Then
                            Return False
                        End If

                        Return _clonedFrom.IsMetadataOut
                    End Get
                End Property

                Friend Overrides ReadOnly Property IsMetadataIn As Boolean
                    Get
                        If IsComEventParameter Then
                            Return False
                        End If

                        Return _clonedFrom.IsMetadataIn
                    End Get
                End Property

                Friend Overrides ReadOnly Property HasOptionCompare As Boolean
                    Get
                        If IsComEventParameter Then
                            Return False
                        End If

                        Return _clonedFrom.HasOptionCompare
                    End Get
                End Property

                Friend Overrides ReadOnly Property IsIDispatchConstant As Boolean
                    Get
                        If IsComEventParameter Then
                            Return False
                        End If

                        Return _clonedFrom.IsIDispatchConstant
                    End Get
                End Property

                Friend Overrides ReadOnly Property IsIUnknownConstant As Boolean
                    Get
                        If IsComEventParameter Then
                            Return False
                        End If

                        Return _clonedFrom.IsIUnknownConstant
                    End Get
                End Property

                Friend Overrides ReadOnly Property IsCallerLineNumber As Boolean
                    Get
                        Throw ExceptionUtilities.Unreachable
                    End Get
                End Property

                Friend Overrides ReadOnly Property IsCallerMemberName As Boolean
                    Get
                        Throw ExceptionUtilities.Unreachable
                    End Get
                End Property

                Friend Overrides ReadOnly Property IsCallerFilePath As Boolean
                    Get
                        Throw ExceptionUtilities.Unreachable
                    End Get
                End Property

                Friend Overrides ReadOnly Property CallerArgumentExpressionParameterIndex As Integer
                    Get
                        Throw ExceptionUtilities.Unreachable
                    End Get
                End Property

                Public Overrides ReadOnly Property IsParamArray As Boolean
                    Get
                        If IsComEventParameter Then
                            Return False
                        End If

                        Return _clonedFrom.IsParamArray
                    End Get
                End Property

                Friend Overrides ReadOnly Property MarshallingInformation As MarshalPseudoCustomAttributeData
                    Get
                        If IsComEventParameter Then
                            Return Nothing
                        End If

                        Return _clonedFrom.MarshallingInformation
                    End Get
                End Property

                Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
                    Get
                        Throw ExceptionUtilities.Unreachable
                    End Get
                End Property

                Public Overrides ReadOnly Property Ordinal As Integer
                    Get
                        Return _clonedFrom.Ordinal
                    End Get
                End Property

                Public Overrides ReadOnly Property Type As TypeSymbol
                    Get
                        Return _clonedFrom.Type
                    End Get
                End Property

                Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
                    If IsComEventParameter Then
                        Return ImmutableArray(Of VisualBasicAttributeData).Empty
                    End If

                    Return _clonedFrom.GetAttributes()
                End Function

                Friend Overrides Sub AddSynthesizedAttributes(moduleBuilder As PEModuleBuilder, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
                    MyBase.AddSynthesizedAttributes(moduleBuilder, attributes)

                    If IsComEventParameter Then
                        Return
                    End If

                    Dim toClone As ArrayBuilder(Of SynthesizedAttributeData) = Nothing
                    _clonedFrom.AddSynthesizedAttributes(moduleBuilder, toClone)

                    Dim compilation = Me.DeclaringCompilation
                    Dim paramArrayAttribute As NamedTypeSymbol = compilation.GetWellKnownType(WellKnownType.System_ParamArrayAttribute)
                    Dim dateTimeConstantAttribute As NamedTypeSymbol = compilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_DateTimeConstantAttribute)
                    Dim decimalConstantAttribute As NamedTypeSymbol = compilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute)

                    If toClone IsNot Nothing Then
                        For Each attrData In toClone
                            If attrData.AttributeClass Is paramArrayAttribute OrElse
                               attrData.AttributeClass Is dateTimeConstantAttribute OrElse
                               attrData.AttributeClass Is decimalConstantAttribute Then
                                AddSynthesizedAttribute(attributes, attrData)
                            End If
                        Next

                        toClone.Free()
                    End If
                End Sub
            End Class

            Private Class SynthesizedComProperty
                Inherits PropertySymbol

                Private ReadOnly _interface As SynthesizedComInterface
                Private ReadOnly _clonedFrom As PropertySymbol
                Private ReadOnly _synthesizedDispId As Integer ' ReservedDispId.None if shouldn't be synthesized 
                Private ReadOnly _getter As SynthesizedComMethod
                Private ReadOnly _setter As SynthesizedComMethod
                Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)

                Public Sub New(
                    container As SynthesizedComInterface,
                    clone As PropertySymbol,
                    getter As SynthesizedComMethod,
                    setter As SynthesizedComMethod,
                    synthesizedDispId As Integer
                )
                    _interface = container
                    Debug.Assert(clone.DeclaredAccessibility = Accessibility.Public)
                    _clonedFrom = clone
                    _synthesizedDispId = synthesizedDispId
                    _getter = getter
                    _setter = setter

                    If clone.ParameterCount = 0 Then
                        _parameters = ImmutableArray(Of ParameterSymbol).Empty
                    Else
                        Dim parameters(clone.ParameterCount - 1) As ParameterSymbol

                        For i As Integer = 0 To parameters.Length - 1
                            parameters(i) = New SynthesizedComParameter(Me, clone.Parameters(i))
                        Next

                        _parameters = parameters.AsImmutable()
                    End If
                End Sub

                Public Overrides ReadOnly Property Name As String
                    Get
                        Return _clonedFrom.Name
                    End Get
                End Property

                Friend Overrides ReadOnly Property HasSpecialName As Boolean
                    Get
                        Return _clonedFrom.HasSpecialName
                    End Get
                End Property

                Friend Overrides ReadOnly Property CallingConvention As Microsoft.Cci.CallingConvention
                    Get
                        Return _clonedFrom.CallingConvention
                    End Get
                End Property

                Public Overrides ReadOnly Property ContainingSymbol As Symbol
                    Get
                        Return _interface
                    End Get
                End Property

                Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
                    Get
                        Return _interface
                    End Get
                End Property

                Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
                    Get
                        Return Accessibility.Public
                    End Get
                End Property

                Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
                    Get
                        Throw ExceptionUtilities.Unreachable
                    End Get
                End Property

                Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of PropertySymbol)
                    Get
                        Return ImmutableArray(Of PropertySymbol).Empty
                    End Get
                End Property

                Public Overrides ReadOnly Property GetMethod As MethodSymbol
                    Get
                        Return _getter
                    End Get
                End Property

                Friend Overrides ReadOnly Property AssociatedField As FieldSymbol
                    Get
                        Return Nothing
                    End Get
                End Property

                Public Overrides ReadOnly Property IsDefault As Boolean
                    Get
                        Throw ExceptionUtilities.Unreachable
                    End Get
                End Property

                Public Overrides ReadOnly Property IsMustOverride As Boolean
                    Get
                        Return True
                    End Get
                End Property

                Public Overrides ReadOnly Property IsNotOverridable As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Public Overrides ReadOnly Property IsOverloads As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Public Overrides ReadOnly Property IsOverridable As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Public Overrides ReadOnly Property IsOverrides As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Public Overrides ReadOnly Property IsShared As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
                    Get
                        Throw ExceptionUtilities.Unreachable
                    End Get
                End Property

                Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
                    Get
                        Return _parameters
                    End Get
                End Property

                Public Overrides ReadOnly Property SetMethod As MethodSymbol
                    Get
                        Return _setter
                    End Get
                End Property

                Public Overrides ReadOnly Property ReturnsByRef As Boolean
                    Get
                        Return _clonedFrom.ReturnsByRef
                    End Get
                End Property

                Public Overrides ReadOnly Property Type As TypeSymbol
                    Get
                        Return _clonedFrom.Type
                    End Get
                End Property

                Friend NotOverridable Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
                    Get
                        Return Nothing
                    End Get
                End Property

                Public Overrides ReadOnly Property TypeCustomModifiers As ImmutableArray(Of CustomModifier)
                    Get
                        Return _clonedFrom.TypeCustomModifiers
                    End Get
                End Property

                Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
                    Get
                        Return _clonedFrom.RefCustomModifiers
                    End Get
                End Property

                Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
                    Return _clonedFrom.GetAttributes()
                End Function

                Friend Overrides Sub AddSynthesizedAttributes(moduleBuilder As PEModuleBuilder, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
                    MyBase.AddSynthesizedAttributes(moduleBuilder, attributes)

                    If _synthesizedDispId = ReservedDispId.None Then
                        Return
                    End If

                    AddSynthesizedAttribute(attributes, _interface.ComClass.DeclaringCompilation.TrySynthesizeAttribute(
                        WellKnownMember.System_Runtime_InteropServices_DispIdAttribute__ctor,
                        ImmutableArray.Create(New TypedConstant(_interface.ComClass.GetSpecialType(Microsoft.CodeAnalysis.SpecialType.System_Int32),
                                                                        TypedConstantKind.Primitive,
                                                                        _synthesizedDispId))))
                End Sub

                Friend Overrides ReadOnly Property IsMyGroupCollectionProperty As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Public Overrides ReadOnly Property IsRequired As Boolean
                    Get
                        Return False
                    End Get
                End Property
            End Class
        End Class

        ''' <summary>
        ''' Perform ComClass specific validation and prepare for metadata generation.
        ''' </summary>
        Private Sub PerformComClassAnalysis()
            If _comClassData Is Nothing Then
                Return
            End If

            _comClassData.PerformComClassAnalysis(Me)
        End Sub

        Friend Overrides Function GetSynthesizedNestedTypes() As IEnumerable(Of Microsoft.Cci.INestedTypeDefinition)
            If _comClassData Is Nothing Then
                Return Nothing
            End If

            Dim interfaces As ImmutableArray(Of NamedTypeSymbol) = _comClassData.GetSynthesizedInterfaces()

            If interfaces.IsEmpty Then
                Return Nothing
            End If

#If DEBUG Then
            Return interfaces.Select(Function(i) i.GetCciAdapter())
#Else
            return interfaces.AsEnumerable()
#End If
        End Function

        Friend Overrides Function GetSynthesizedImplements() As IEnumerable(Of NamedTypeSymbol)
            If _comClassData Is Nothing Then
                Return Nothing
            End If

            Return _comClassData.GetSynthesizedImplements()
        End Function

        Friend Function GetCorrespondingComClassInterfaceMethod(method As MethodSymbol) As MethodSymbol
            GetAttributes()

            If _comClassData Is Nothing Then
                Return Nothing
            End If

            _comClassData.PerformComClassAnalysis(Me)

            Return _comClassData.GetCorrespondingComClassInterfaceMethod(method)
        End Function

    End Class

End Namespace

