' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class Symbol

        ' !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        ' Changes to the public interface of this class should remain synchronized with the C# version of Symbol.
        ' Do not make any changes to the public interface without making the corresponding change
        ' to the C# version.
        ' !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        ''' <summary>
        ''' Gets the attributes on this symbol. Returns an empty ImmutableArray if there are
        ''' no attributes.
        ''' </summary>
        Public Overridable Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return ImmutableArray(Of VisualBasicAttributeData).Empty
        End Function

        ''' <summary>
        ''' Build and add synthesized attributes for this symbol.
        ''' </summary>
        Friend Overridable Sub AddSynthesizedAttributes(compilationState As ModuleCompilationState, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
        End Sub

        ''' <summary>
        ''' Convenience helper called by subclasses to add a synthesized attribute to a collection of attributes.
        ''' </summary>
        Friend Shared Sub AddSynthesizedAttribute(ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData), attribute As SynthesizedAttributeData)
            If attribute IsNot Nothing Then
                If attributes Is Nothing Then
                    attributes = ArrayBuilder(Of SynthesizedAttributeData).GetInstance(4)
                End If

                attributes.Add(attribute)
            End If
        End Sub

        ''' <summary>
        ''' Returns the appropriate AttributeTarget for a symbol.  This is used to validate attribute usage when
        ''' applying an attribute to a symbol. For any symbol that does not support the application of custom
        ''' attributes 0 is returned.
        ''' </summary>
        ''' <returns>The attribute target flag for this symbol or 0 if none apply.</returns>
        ''' <remarks></remarks>
        Friend Function GetAttributeTarget() As AttributeTargets
            Select Case Kind
                Case SymbolKind.Assembly
                    Return AttributeTargets.Assembly

                Case SymbolKind.Event
                    Return AttributeTargets.Event

                Case SymbolKind.Field
                    Return AttributeTargets.Field

                Case SymbolKind.Method
                    Dim method = DirectCast(Me, MethodSymbol)

                    Select Case method.MethodKind
                        Case MethodKind.Constructor,
                             MethodKind.SharedConstructor
                            Return AttributeTargets.Constructor

                        Case MethodKind.Ordinary,
                             MethodKind.DeclareMethod,
                             MethodKind.UserDefinedOperator,
                             MethodKind.Conversion,
                             MethodKind.PropertyGet,
                             MethodKind.PropertySet,
                             MethodKind.EventAdd,
                             MethodKind.EventRaise,
                             MethodKind.EventRemove,
                             MethodKind.DelegateInvoke
                            Return AttributeTargets.Method
                    End Select

                Case SymbolKind.Property
                    Return AttributeTargets.Property

                Case SymbolKind.NamedType
                    Dim namedType = DirectCast(Me, NamedTypeSymbol)
                    Select Case namedType.TypeKind
                        Case TypeKind.Class,
                             TypeKind.Module
                            Return AttributeTargets.Class

                        Case TypeKind.Structure
                            Return AttributeTargets.Struct

                        Case TypeKind.Interface
                            Return AttributeTargets.Interface

                        Case TypeKind.Enum
                            Return AttributeTargets.Enum Or AttributeTargets.Struct

                        Case TypeKind.Delegate
                            Return AttributeTargets.Delegate

                        Case TypeKind.Submission
                            ' attributes can't be applied on a submission type
                            Throw ExceptionUtilities.UnexpectedValue(namedType.TypeKind)

                    End Select

                Case SymbolKind.NetModule
                    Return AttributeTargets.Module

                Case SymbolKind.Parameter
                    Return AttributeTargets.Parameter

                Case SymbolKind.TypeParameter
                    Return AttributeTargets.GenericParameter

            End Select

            Return 0
        End Function

        ''' <summary>
        ''' Method to early decode applied well-known attribute which can be queried by the binder.
        ''' This method is called during attribute binding after we have bound the attribute types for all attributes,
        ''' but haven't yet bound the attribute arguments/attribute constructor.
        ''' Early decoding certain well-known attributes enables the binder to use this decoded information on this symbol
        ''' when binding the attribute arguments/attribute constructor without causing attribute binding cycle.
        ''' </summary>
        Friend Overridable Function EarlyDecodeWellKnownAttribute(ByRef arguments As EarlyDecodeWellKnownAttributeArguments(Of EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation)) As VisualBasicAttributeData
            Return Nothing
        End Function

        Friend Function EarlyDecodeDeprecatedOrObsoleteAttribute(
            ByRef arguments As EarlyDecodeWellKnownAttributeArguments(Of EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation),
            <Out> ByRef boundAttribute As VisualBasicAttributeData,
            <Out> ByRef obsoleteData As ObsoleteAttributeData
        ) As Boolean

            Dim hasAnyDiagnostics As Boolean = False

            If VisualBasicAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.ObsoleteAttribute) Then
                ' Handle ObsoleteAttribute
                boundAttribute = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, hasAnyDiagnostics)
                If Not boundAttribute.HasErrors Then
                    obsoleteData = boundAttribute.DecodeObsoleteAttribute()
                    If hasAnyDiagnostics Then
                        boundAttribute = Nothing
                    End If
                Else
                    obsoleteData = Nothing
                    boundAttribute = Nothing
                End If

                Return True
            End If

            If VisualBasicAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.DeprecatedAttribute) Then
                ' Handle DeprecatedAttribute
                boundAttribute = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, hasAnyDiagnostics)
                If Not boundAttribute.HasErrors Then
                    obsoleteData = boundAttribute.DecodeDeprecatedAttribute()
                    If hasAnyDiagnostics Then
                        boundAttribute = Nothing
                    End If
                Else
                    obsoleteData = Nothing
                    boundAttribute = Nothing
                End If

                Return True
            End If

            boundAttribute = Nothing
            obsoleteData = Nothing
            Return False
        End Function

        ''' <summary>
        ''' This method is called by the binder when it is finished binding a set of attributes on the symbol so that
        ''' the symbol can extract data from the attribute arguments and potentially perform validation specific to
        ''' some well known attributes.
        ''' </summary>
        ''' <remarks>
        ''' <para>
        ''' Symbol types should override this if they want to handle a specific well-known attribute.
        ''' If the attribute is of a type that the symbol does not wish to handle, it should delegate back to
        ''' this (base) method.
        ''' </para>
        ''' </remarks>
        Friend Overridable Sub DecodeWellKnownAttribute(ByRef arguments As DecodeWellKnownAttributeArguments(Of AttributeSyntax, VisualBasicAttributeData, AttributeLocation))
            Dim compilation = Me.DeclaringCompilation
            MarkEmbeddedAttributeTypeReference(arguments.Attribute, arguments.AttributeSyntaxOpt, compilation)
            ReportExtensionAttributeUseSiteError(arguments.Attribute, arguments.AttributeSyntaxOpt, compilation, arguments.Diagnostics)
        End Sub

        ''' <summary>
        ''' Called to report attribute related diagnostics after all attributes have been bound and decoded.
        ''' Called even if there are no attributes.
        ''' </summary>
        ''' <remarks>
        ''' This method is called by the binder from <see cref="LoadAndValidateAttributes"/> after it has finished binding attributes on the symbol,
        ''' has executed <see cref="DecodeWellKnownAttribute"/> for attributes applied on the symbol and has stored the decoded data in the
        ''' lazyCustomAttributesBag on the symbol. Bound attributes haven't been stored on the bag yet.
        ''' 
        ''' Post-validation for attributes that is dependent on other attributes can be done here.
        ''' 
        ''' This method should not have any side effects on the symbol, i.e. it SHOULD NOT change the symbol state.
        ''' </remarks>
        ''' <param name="boundAttributes">Bound attributes.</param>
        ''' <param name="allAttributeSyntaxNodes">Syntax nodes of attributes in order they are specified in source.</param>
        ''' <param name="diagnostics">Diagnostic bag.</param>
        ''' <param name="symbolPart">Specific part of the symbol to which the attributes apply, or <see cref="AttributeLocation.None"/> if the attributes apply to the symbol itself.</param>
        ''' <param name="decodedData">Decoded well known attribute data.</param>
        Friend Overridable Sub PostDecodeWellKnownAttributes(boundAttributes As ImmutableArray(Of VisualBasicAttributeData),
                                                           allAttributeSyntaxNodes As ImmutableArray(Of AttributeSyntax),
                                                           diagnostics As DiagnosticBag,
                                                           symbolPart As AttributeLocation,
                                                           decodedData As WellKnownAttributeData)
        End Sub

        ''' <summary>
        ''' This method does the following set of operations in the specified order:
        ''' (1) GetAttributesToBind: Merge the given attributeBlockSyntaxList into a single list of attributes to bind.
        ''' (2) GetAttributes: Bind the attributes (attribute type, arguments and constructor).
        ''' (3) DecodeWellKnownAttributes: Decode and validate bound well-known attributes.
        ''' (4) ValidateAttributes: Perform some additional attribute validations, such as
        '''         1) Duplicate attributes,
        '''         2) Attribute usage target validation, etc.
        ''' (5) Store the bound attributes and decoded well-known attribute data in lazyCustomAttributesBag in a thread safe manner.
        ''' </summary>
        Friend Sub LoadAndValidateAttributes(attributeBlockSyntaxList As OneOrMany(Of SyntaxList(Of AttributeListSyntax)),
                                             ByRef lazyCustomAttributesBag As CustomAttributesBag(Of VisualBasicAttributeData),
                                             Optional symbolPart As AttributeLocation = 0)

            Dim diagnostics = DiagnosticBag.GetInstance()
            Dim sourceAssembly = DirectCast(If(Me.Kind = SymbolKind.Assembly, Me, Me.ContainingAssembly), SourceAssemblySymbol)
            Dim sourceModule = sourceAssembly.SourceModule
            Dim compilation = sourceAssembly.DeclaringCompilation

            Dim binders As ImmutableArray(Of Binder) = Nothing
            Dim attributesToBind = GetAttributesToBind(attributeBlockSyntaxList, symbolPart, compilation, binders)

            Dim boundAttributes As ImmutableArray(Of VisualBasicAttributeData)
            Dim wellKnownAttrData As WellKnownAttributeData

            If attributesToBind.Any() Then
                Debug.Assert(attributesToBind.Any())
                Debug.Assert(binders.Any())
                Debug.Assert(attributesToBind.Length = binders.Length)

                ' Initialize the bag so that data decoded from early attributes can be stored onto it.
                If (lazyCustomAttributesBag Is Nothing) Then
                    Interlocked.CompareExchange(lazyCustomAttributesBag, New CustomAttributesBag(Of VisualBasicAttributeData)(), Nothing)
                End If

                Dim boundAttributeTypes As ImmutableArray(Of NamedTypeSymbol) = Binder.BindAttributeTypes(binders, attributesToBind, Me, diagnostics)
                Dim attributeBuilder = New VisualBasicAttributeData(boundAttributeTypes.Length - 1) {}

                ' Early bind and decode some well-known attributes.
                Dim earlyData As EarlyWellKnownAttributeData = Me.EarlyDecodeWellKnownAttributes(binders, boundAttributeTypes, attributesToBind, attributeBuilder, symbolPart)

                ' Store data decoded from early bound well-known attributes.
                lazyCustomAttributesBag.SetEarlyDecodedWellKnownAttributeData(earlyData)

                ' Bind attributes.
                Binder.GetAttributes(binders, attributesToBind, boundAttributeTypes, attributeBuilder, Me, diagnostics)
                boundAttributes = attributeBuilder.AsImmutableOrNull

                ' Validate attribute usage and Decode remaining well-known attributes.
                wellKnownAttrData = Me.ValidateAttributeUsageAndDecodeWellKnownAttributes(binders, attributesToBind, boundAttributes, diagnostics, symbolPart)

                ' Store data decoded from remaining well-known attributes.
                lazyCustomAttributesBag.SetDecodedWellKnownAttributeData(wellKnownAttrData)
            Else
                boundAttributes = ImmutableArray(Of VisualBasicAttributeData).Empty
                wellKnownAttrData = Nothing
                Interlocked.CompareExchange(lazyCustomAttributesBag, CustomAttributesBag(Of VisualBasicAttributeData).WithEmptyData(), Nothing)
            End If

            Me.PostDecodeWellKnownAttributes(boundAttributes, attributesToBind, diagnostics, symbolPart, wellKnownAttrData)

            ' Store attributes into the bag.
            sourceModule.AtomicStoreAttributesAndDiagnostics(lazyCustomAttributesBag, boundAttributes, diagnostics)

            diagnostics.Free()
            Debug.Assert(lazyCustomAttributesBag.IsSealed)
        End Sub

        Private Function GetAttributesToBind(attributeDeclarationSyntaxLists As OneOrMany(Of SyntaxList(Of AttributeListSyntax)),
                                             symbolPart As AttributeLocation,
                                             compilation As VisualBasicCompilation,
                                             <Out> ByRef binders As ImmutableArray(Of Binder)) As ImmutableArray(Of AttributeSyntax)

            Dim attributeTarget = DirectCast(Me, IAttributeTargetSymbol)
            Dim sourceModule = DirectCast(compilation.SourceModule, SourceModuleSymbol)
            Dim syntaxBuilder As ArrayBuilder(Of AttributeSyntax) = Nothing
            Dim bindersBuilder As ArrayBuilder(Of Binder) = Nothing
            Dim attributesToBindCount As Integer = 0

            For listIndex = 0 To attributeDeclarationSyntaxLists.Count - 1
                Dim attributeDeclarationSyntaxList As SyntaxList(Of AttributeListSyntax) = attributeDeclarationSyntaxLists(listIndex)

                If attributeDeclarationSyntaxList.Any() Then
                    Dim prevCount As Integer = attributesToBindCount

                    For Each attributeDeclarationSyntax In attributeDeclarationSyntaxList
                        For Each attributeSyntax In attributeDeclarationSyntax.Attributes
                            If MatchAttributeTarget(attributeTarget, symbolPart, attributeSyntax.Target) Then
                                If syntaxBuilder Is Nothing Then
                                    syntaxBuilder = New ArrayBuilder(Of AttributeSyntax)()
                                    bindersBuilder = New ArrayBuilder(Of Binder)()
                                End If

                                syntaxBuilder.Add(attributeSyntax)
                                attributesToBindCount += 1
                            End If
                        Next
                    Next

                    If attributesToBindCount <> prevCount Then
                        Debug.Assert(attributeDeclarationSyntaxList.Node IsNot Nothing)
                        Debug.Assert(bindersBuilder IsNot Nothing)

                        Dim binder = GetAttributeBinder(attributeDeclarationSyntaxList, sourceModule)
                        For i = 0 To attributesToBindCount - prevCount - 1
                            bindersBuilder.Add(binder)
                        Next
                    End If
                End If
            Next

            If syntaxBuilder IsNot Nothing Then
                binders = bindersBuilder.ToImmutableAndFree()
                Return syntaxBuilder.ToImmutableAndFree()
            Else
                binders = ImmutableArray(Of Binder).Empty
                Return ImmutableArray(Of AttributeSyntax).Empty
            End If
        End Function

        Friend Function GetAttributeBinder(syntaxList As SyntaxList(Of AttributeListSyntax), sourceModule As SourceModuleSymbol) As Binder
            Dim syntaxTree = syntaxList.Node.SyntaxTree
            Dim parent = syntaxList.Node.Parent

            If parent.IsKind(SyntaxKind.AttributesStatement) AndAlso parent.Parent.IsKind(SyntaxKind.CompilationUnit) Then
                ' Create a binder for the file-level attributes. To avoid infinite recursion, the bound file information
                ' must be fully computed prior to trying to bind the file attributes.
                Return BinderBuilder.CreateBinderForProjectLevelNamespace(sourceModule, syntaxTree)
            Else
                Return BinderBuilder.CreateBinderForAttribute(sourceModule, syntaxTree, Me)
            End If
        End Function

        Private Shared Function MatchAttributeTarget(attributeTarget As IAttributeTargetSymbol, symbolPart As AttributeLocation, targetOpt As AttributeTargetSyntax) As Boolean
            If targetOpt Is Nothing Then
                Return True
            End If

            Dim explicitTarget As AttributeLocation

            ' Parser ensures that an error is reported for anything other than "assembly" or
            ' "module". Only assembly and module keywords can get here.
            Select Case targetOpt.AttributeModifier.Kind
                Case SyntaxKind.AssemblyKeyword
                    explicitTarget = AttributeLocation.Assembly

                Case SyntaxKind.ModuleKeyword
                    explicitTarget = AttributeLocation.Module

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(targetOpt.AttributeModifier.Kind)
            End Select

            If symbolPart = 0 Then
                Return explicitTarget = attributeTarget.DefaultAttributeLocation
            Else
                Return explicitTarget = symbolPart
            End If
        End Function

        Private Shared Function GetAttributesToBind(attributeBlockSyntaxList As SyntaxList(Of AttributeListSyntax)) As ImmutableArray(Of AttributeSyntax)
            Dim attributeSyntaxBuilder As ArrayBuilder(Of AttributeSyntax) = Nothing
            GetAttributesToBind(attributeBlockSyntaxList, attributeSyntaxBuilder)
            Return If(attributeSyntaxBuilder IsNot Nothing, attributeSyntaxBuilder.ToImmutableAndFree(), ImmutableArray(Of AttributeSyntax).Empty)
        End Function

        Friend Shared Sub GetAttributesToBind(attributeBlockSyntaxList As SyntaxList(Of AttributeListSyntax), ByRef attributeSyntaxBuilder As ArrayBuilder(Of AttributeSyntax))
            If attributeBlockSyntaxList.Count > 0 Then
                If attributeSyntaxBuilder Is Nothing Then
                    attributeSyntaxBuilder = ArrayBuilder(Of AttributeSyntax).GetInstance()
                End If

                For Each attributeBlock In attributeBlockSyntaxList
                    attributeSyntaxBuilder.AddRange(attributeBlock.Attributes)
                Next
            End If
        End Sub

        ''' <summary> 
        ''' Method to early decode certain well-known attributes which can be queried by the binder. 
        ''' This method is called during attribute binding after we have bound the attribute types for all attributes, 
        ''' but haven't yet bound the attribute arguments/attribute constructor. 
        ''' Early decoding certain well-known attributes enables the binder to use this decoded information on this symbol 
        ''' when binding the attribute arguments/attribute constructor without causing attribute binding cycle. 
        ''' </summary>
        Private Function EarlyDecodeWellKnownAttributes(binders As ImmutableArray(Of Binder),
                                                  boundAttributeTypes As ImmutableArray(Of NamedTypeSymbol),
                                                  attributesToBind As ImmutableArray(Of AttributeSyntax),
                                                  attributeBuilder As VisualBasicAttributeData(),
                                                  symbolPart As AttributeLocation) As EarlyWellKnownAttributeData
            Debug.Assert(boundAttributeTypes.Any())
            Debug.Assert(attributesToBind.Any())

            Dim arguments = New EarlyDecodeWellKnownAttributeArguments(Of EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation)()
            arguments.SymbolPart = symbolPart

            For i = 0 To boundAttributeTypes.Length - 1
                Dim attributeType As NamedTypeSymbol = boundAttributeTypes(i)
                If Not attributeType.IsErrorType() Then
                    arguments.Binder = New EarlyWellKnownAttributeBinder(Me, binders(i))
                    arguments.AttributeType = attributeType
                    arguments.AttributeSyntax = attributesToBind(i)
                    attributeBuilder(i) = Me.EarlyDecodeWellKnownAttribute(arguments)
                End If
            Next

            Return If(arguments.HasDecodedData, arguments.DecodedData, Nothing)
        End Function

        ''' <summary> 
        ''' This method validates attribute usage for each bound attribute and calls <see cref="DecodeWellKnownAttribute"/>
        ''' on attributes with valid attribute usage.
        ''' This method is called by the binder when it is finished binding a set of attributes on the symbol so that 
        ''' the symbol can extract data from the attribute arguments and potentially perform validation specific to 
        ''' some well known attributes. 
        ''' </summary>
        Friend Function ValidateAttributeUsageAndDecodeWellKnownAttributes(
            binders As ImmutableArray(Of Binder),
            attributeSyntaxList As ImmutableArray(Of AttributeSyntax),
            boundAttributes As ImmutableArray(Of VisualBasicAttributeData),
            diagnostics As DiagnosticBag,
            symbolPart As AttributeLocation) As WellKnownAttributeData

            Debug.Assert(binders.Any())
            Debug.Assert(attributeSyntaxList.Any())
            Debug.Assert(boundAttributes.Any())
            Debug.Assert(binders.Length = boundAttributes.Length)
            Debug.Assert(attributeSyntaxList.Length = boundAttributes.Length)

            Dim totalAttributesCount As Integer = boundAttributes.Length
            Dim uniqueAttributeTypes = New HashSet(Of NamedTypeSymbol)
            Dim arguments = New DecodeWellKnownAttributeArguments(Of AttributeSyntax, VisualBasicAttributeData, AttributeLocation)()
            arguments.AttributesCount = totalAttributesCount
            arguments.Diagnostics = diagnostics
            arguments.SymbolPart = symbolPart

            For i = 0 To totalAttributesCount - 1
                Dim boundAttribute As VisualBasicAttributeData = boundAttributes(i)
                Dim attributeSyntax As AttributeSyntax = attributeSyntaxList(i)
                Dim binder As Binder = binders(i)
                If Not boundAttribute.HasErrors AndAlso ValidateAttributeUsage(boundAttribute, attributeSyntax, binder.Compilation, symbolPart, diagnostics, uniqueAttributeTypes) Then
                    arguments.Attribute = boundAttribute
                    arguments.AttributeSyntaxOpt = attributeSyntax
                    arguments.Index = i
                    Me.DecodeWellKnownAttribute(arguments)
                End If
            Next

            Return If(arguments.HasDecodedData, arguments.DecodedData, Nothing)
        End Function

        ''' <summary>
        ''' Validate attribute usage target and duplicate attributes.
        ''' </summary>
        ''' <param name="attribute">Bound attribute</param>
        ''' <param name="node">Syntax node for attribute specification</param>
        ''' <param name="compilation">Compilation</param>
        ''' <param name="symbolPart">Symbol part to which the attribute has been applied</param>
        ''' <param name="diagnostics">Diagnostics</param>
        ''' <param name="uniqueAttributeTypes">Set of unique attribute types applied to the symbol</param>
        Private Function ValidateAttributeUsage(
            attribute As VisualBasicAttributeData,
            node As AttributeSyntax,
            compilation As VisualBasicCompilation,
            symbolPart As AttributeLocation,
            diagnostics As DiagnosticBag,
            uniqueAttributeTypes As HashSet(Of NamedTypeSymbol)) As Boolean

            Dim attributeType As NamedTypeSymbol = attribute.AttributeClass
            Debug.Assert(attributeType IsNot Nothing)
            Debug.Assert(Not attributeType.IsErrorType())
            Debug.Assert(attributeType.IsOrDerivedFromWellKnownClass(WellKnownType.System_Attribute, compilation, Nothing))

            ' Get attribute usage for this attribute
            Dim attributeUsage As AttributeUsageInfo = attributeType.GetAttributeUsageInfo()
            Debug.Assert(Not attributeUsage.IsNull)

            ' check if this attribute was used multiple times and attributeUsage.AllowMultiple is False.
            If Not uniqueAttributeTypes.Add(attributeType) AndAlso Not attributeUsage.AllowMultiple Then
                diagnostics.Add(ERRID.ERR_InvalidMultipleAttributeUsage1, node.GetLocation(), CustomSymbolDisplayFormatter.ShortErrorName(attributeType))
                Return False
            End If

            Dim attributeTarget As AttributeTargets
            If symbolPart = AttributeLocation.Return Then
                Debug.Assert(Me.Kind = SymbolKind.Method OrElse Me.Kind = SymbolKind.Property)
                attributeTarget = AttributeTargets.ReturnValue
            Else
                attributeTarget = Me.GetAttributeTarget()
            End If

            ' VB allows NonSerialized on events even though the NonSerialized does not have this attribute usage specified. 
            ' See Dev 10 Bindable::VerifyCustomAttributesOnSymbol
            Dim applicationIsValid As Boolean
            If attributeType Is compilation.GetWellKnownType(WellKnownType.System_NonSerializedAttribute) AndAlso
               Me.Kind = SymbolKind.Event AndAlso DirectCast(Me, SourceEventSymbol).AssociatedField IsNot Nothing Then
                applicationIsValid = True
            Else
                Dim validOn = attributeUsage.ValidTargets
                applicationIsValid = attributeTarget <> 0 AndAlso (validOn And attributeTarget) <> 0
            End If

            If Not applicationIsValid Then
                Select Case attributeTarget
                    Case AttributeTargets.Assembly
                        diagnostics.Add(ERRID.ERR_InvalidAssemblyAttribute1, node.Name.GetLocation, CustomSymbolDisplayFormatter.ShortErrorName(attributeType))

                    Case AttributeTargets.Module
                        diagnostics.Add(ERRID.ERR_InvalidModuleAttribute1, node.Name.GetLocation, CustomSymbolDisplayFormatter.ShortErrorName(attributeType))

                    Case AttributeTargets.Method
                        If Me.Kind = SymbolKind.Method Then
                            Dim method = DirectCast(Me, SourceMethodSymbol)

                            Dim accessorName = TryGetAccessorDisplayName(method.MethodKind)
                            If accessorName IsNot Nothing Then
                                Debug.Assert(method.AssociatedSymbol IsNot Nothing)

                                diagnostics.Add(ERRID.ERR_InvalidAttributeUsageOnAccessor, node.Name.GetLocation,
                                    CustomSymbolDisplayFormatter.ShortErrorName(attributeType), accessorName, CustomSymbolDisplayFormatter.ShortErrorName(method.AssociatedSymbol))

                                Exit Select
                            End If
                        End If

                        diagnostics.Add(ERRID.ERR_InvalidAttributeUsage2, node.Name.GetLocation,
                            CustomSymbolDisplayFormatter.ShortErrorName(attributeType), CustomSymbolDisplayFormatter.ShortErrorName(Me).ToString())

                    Case AttributeTargets.Field
                        Dim withEventsBackingField = TryCast(Me, SourceWithEventsBackingFieldSymbol)
                        Dim ownerName As String
                        If withEventsBackingField IsNot Nothing Then
                            ownerName = CustomSymbolDisplayFormatter.ShortErrorName(withEventsBackingField.AssociatedSymbol).ToString()
                        Else
                            ownerName = CustomSymbolDisplayFormatter.ShortErrorName(Me).ToString()
                        End If

                        diagnostics.Add(ERRID.ERR_InvalidAttributeUsage2, node.Name.GetLocation, CustomSymbolDisplayFormatter.ShortErrorName(attributeType), ownerName)

                    Case AttributeTargets.ReturnValue
                        diagnostics.Add(ERRID.ERR_InvalidAttributeUsage2, node.Name.GetLocation, CustomSymbolDisplayFormatter.ShortErrorName(attributeType),
                            New LocalizableErrorArgument(ERRID.IDS_FunctionReturnType))

                    Case Else
                        diagnostics.Add(ERRID.ERR_InvalidAttributeUsage2, node.Name.GetLocation, CustomSymbolDisplayFormatter.ShortErrorName(attributeType),
                            CustomSymbolDisplayFormatter.ShortErrorName(Me).ToString())
                End Select

                Return False
            End If

            If attribute.IsSecurityAttribute(compilation) Then
                Select Case Me.Kind
                    Case SymbolKind.Assembly, SymbolKind.NamedType, SymbolKind.Method
                        Exit Select
                    Case Else
                        ' BC36979: Security attribute '{0}' is not valid on this declaration type. Security attributes are only valid on assembly, type and method declarations.
                        diagnostics.Add(ERRID.ERR_SecurityAttributeInvalidTarget, node.Name.GetLocation, CustomSymbolDisplayFormatter.ShortErrorName(attributeType))
                        Return False
                End Select
            End If
            Return True
        End Function

        Private Sub ReportExtensionAttributeUseSiteError(attribute As VisualBasicAttributeData, nodeOpt As AttributeSyntax, compilation As VisualBasicCompilation, diagnostics As DiagnosticBag)
            ' report issues with a custom extension attribute everywhere, where the attribute is used in source
            ' (we will not report in location where it's implicitly used (like the containing module or assembly of extension methods)
            Dim useSiteError As DiagnosticInfo = Nothing
            If attribute.AttributeConstructor IsNot Nothing AndAlso
                attribute.AttributeConstructor Is compilation.GetExtensionAttributeConstructor(useSiteError) Then
                If useSiteError IsNot Nothing Then
                    diagnostics.Add(useSiteError, If(nodeOpt IsNot Nothing, nodeOpt.GetLocation(), NoLocation.Singleton))
                End If
            End If
        End Sub

        Private Sub MarkEmbeddedAttributeTypeReference(attribute As VisualBasicAttributeData, nodeOpt As AttributeSyntax, compilation As VisualBasicCompilation)
            Debug.Assert(Not attribute.HasErrors)

            ' Mark embedded attribute type reference only if the owner is itself not
            ' embedded and the attribute syntax is actually from the current compilation.
            If Not Me.IsEmbedded AndAlso
               attribute.AttributeClass.IsEmbedded AndAlso
               nodeOpt IsNot Nothing AndAlso
               compilation.ContainsSyntaxTree(nodeOpt.SyntaxTree) Then

                ' Note that none of embedded symbols from referenced 
                ' assemblies or compilations should be found/referenced.
                Debug.Assert(attribute.AttributeClass.ContainingAssembly Is compilation.Assembly)

                compilation.EmbeddedSymbolManager.MarkSymbolAsReferenced(attribute.AttributeClass)
            End If
        End Sub

        ''' <summary>
        ''' Ensure that attributes are bound and the ObsoleteState of this symbol is known.
        ''' </summary>
        Friend Sub ForceCompleteObsoleteAttribute()
            If Me.ObsoleteState = ThreeState.Unknown Then
                Me.GetAttributes()
            End If
            Debug.Assert(Me.ObsoleteState <> ThreeState.Unknown, "ObsoleteState should be true or false now.")
        End Sub
    End Class
End Namespace
