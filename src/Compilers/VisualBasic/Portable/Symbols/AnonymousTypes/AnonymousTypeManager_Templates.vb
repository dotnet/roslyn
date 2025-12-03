' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Partial Friend NotInheritable Class AnonymousTypeManager

        ''' <summary> Cache of created anonymous types </summary>
        Private _concurrentTypesCache As ConcurrentDictionary(Of String, AnonymousTypeTemplateSymbol) = Nothing

        ''' <summary> Cache of created anonymous delegates </summary>
        Private _concurrentDelegatesCache As ConcurrentDictionary(Of String, AnonymousDelegateTemplateSymbol) = Nothing

#If DEBUG Then
        ''' <summary>
        ''' Holds a collection of all the locations of anonymous types and delegates from source
        ''' </summary>
        Private ReadOnly _sourceLocationsSeen As New ConcurrentDictionary(Of Location, Boolean)
#End If

        <Conditional("DEBUG")>
        Private Sub CheckSourceLocationSeen(anonymous As AnonymousTypeOrDelegatePublicSymbol)
#If DEBUG Then
            Dim location As Location = anonymous.GetFirstLocation()
            If location.IsInSource Then
                If Me.AreTemplatesSealed Then
                    Debug.Assert(Me._sourceLocationsSeen.ContainsKey(location))
                Else
                    Me._sourceLocationsSeen.TryAdd(location, True)
                End If
            End If
#End If
        End Sub

        Private ReadOnly Property AnonymousTypeTemplates As ConcurrentDictionary(Of String, AnonymousTypeTemplateSymbol)
            Get
                ' Lazily create a template types cache
                If Me._concurrentTypesCache Is Nothing Then

                    Dim previousSubmission As VisualBasicCompilation = Me.Compilation.PreviousSubmission
                    Dim previousCache = If(previousSubmission Is Nothing, Nothing,
                                       previousSubmission.AnonymousTypeManager._concurrentTypesCache)

                    Interlocked.CompareExchange(Me._concurrentTypesCache,
                                            If(previousCache Is Nothing,
                                               New ConcurrentDictionary(Of String, AnonymousTypeTemplateSymbol),
                                               New ConcurrentDictionary(Of String, AnonymousTypeTemplateSymbol)(previousCache)),
                                            Nothing)
                End If

                Return Me._concurrentTypesCache
            End Get
        End Property

        Private ReadOnly Property AnonymousDelegateTemplates As ConcurrentDictionary(Of String, AnonymousDelegateTemplateSymbol)
            Get
                If Me._concurrentDelegatesCache Is Nothing Then
                    Dim previousSubmission As VisualBasicCompilation = Me.Compilation.PreviousSubmission
                    Dim previousCache = If(previousSubmission Is Nothing, Nothing,
                                       previousSubmission.AnonymousTypeManager._concurrentDelegatesCache)

                    Interlocked.CompareExchange(Me._concurrentDelegatesCache,
                                            If(previousCache Is Nothing,
                                               New ConcurrentDictionary(Of String, AnonymousDelegateTemplateSymbol),
                                               New ConcurrentDictionary(Of String, AnonymousDelegateTemplateSymbol)(previousCache)),
                                            Nothing)
                End If

                Return Me._concurrentDelegatesCache
            End Get
        End Property

        ''' <summary> 
        ''' Given anonymous type public symbol construct an anonymous type symbol to be used 
        ''' in emit; the type symbol is created based on generic type generated for each 
        ''' 'unique' anonymous type structure.
        ''' </summary>
        Private Function ConstructAnonymousTypeImplementationSymbol(anonymous As AnonymousTypePublicSymbol) As NamedTypeSymbol
            CheckSourceLocationSeen(anonymous)

            Dim typeDescr As AnonymousTypeDescriptor = anonymous.TypeDescriptor
            typeDescr.AssertGood()

            ' Get anonymous type template
            Dim template As AnonymousTypeTemplateSymbol = Nothing
            Dim typeKey As String = typeDescr.Key

            If Not AnonymousTypeTemplates.TryGetValue(typeKey, template) Then
                template = AnonymousTypeTemplates.GetOrAdd(typeKey, New AnonymousTypeTemplateSymbol(Me, typeDescr))
            End If

            ' Adjust names in the template
            If template.Manager Is Me Then
                template.AdjustMetadataNames(typeDescr)
            End If

            ' Specialize anonymous type template with field types, adjusted names and locations
            Dim typeArguments = typeDescr.Fields.SelectAsArray(Function(f) f.Type)
            Return template.Construct(typeArguments)
        End Function

        ''' <summary> 
        ''' Given anonymous delegate public symbol construct an anonymous type symbol to be 
        ''' used in emit; the type symbol may be created based on generic type generated for 
        ''' each 'unique' anonymous delegate structure OR if the delegate's signature is 
        ''' 'Sub()' it will be an instance of NonGenericAnonymousDelegateSymbol type.
        ''' </summary>
        Private Function ConstructAnonymousDelegateImplementationSymbol(anonymous As AnonymousDelegatePublicSymbol) As NamedTypeSymbol
            CheckSourceLocationSeen(anonymous)

            Dim delegateDescr As AnonymousTypeDescriptor = anonymous.TypeDescriptor
            delegateDescr.AssertGood()
            Dim parameters As ImmutableArray(Of AnonymousTypeField) = delegateDescr.Parameters

            ' Get anonymous template
            Dim template As AnonymousDelegateTemplateSymbol = Nothing
            Dim delegateKey As String = delegateDescr.Key

            If Not AnonymousDelegateTemplates.TryGetValue(delegateKey, template) Then
                template = AnonymousDelegateTemplates.GetOrAdd(delegateKey, AnonymousDelegateTemplateSymbol.Create(Me, delegateDescr))
            End If

            ' Adjust names in the template
            If template.Manager Is Me Then
                template.AdjustMetadataNames(delegateDescr)
            End If

            ' 'template' may be an instance of NonGenericAnonymousDelegateSymbol if which case 
            ' we can just return it, otherwise we need to construct type using the parameters
            If template.Arity = 0 Then
                Return template
            End If

            ' Specialize anonymous delegate template with parameter types, adjusted names and locations
            Dim typeArguments() As TypeSymbol = New TypeSymbol(template.Arity - 1) {}
            For index = 0 To template.Arity - 1
                typeArguments(index) = parameters(index).Type
            Next
            Return template.Construct(typeArguments)
        End Function

        Private Sub AddFromCache(Of T As AnonymousTypeOrDelegateTemplateSymbol)(
                            builder As ArrayBuilder(Of AnonymousTypeOrDelegateTemplateSymbol),
                            cache As ConcurrentDictionary(Of String, T))

            If cache IsNot Nothing Then
                For Each template In cache.Values
                    If template.Manager Is Me Then
                        builder.Add(template)
                    End If
                Next
            End If
        End Sub

        ''' <summary>
        ''' Resets numbering in anonymous type names and compiles the
        ''' anonymous type methods. Also seals the collection of templates.
        ''' </summary>
        Public Sub AssignTemplatesNamesAndCompile(compiler As MethodCompiler, moduleBeingBuilt As Emit.PEModuleBuilder, diagnostics As BindingDiagnosticBag)
            ' Get all anonymous types owned by this manager
            Dim builder = ArrayBuilder(Of AnonymousTypeOrDelegateTemplateSymbol).GetInstance()
            GetAllCreatedTemplates(builder)

            ' If the collection is not sealed yet we should assign new indexes 
            ' to the created anonymous type and delegate templates
            If Not Me.AreTemplatesSealed Then

                Dim moduleId = GetModuleId(moduleBeingBuilt)
                Dim typeIndex = moduleBeingBuilt.GetNextAnonymousTypeIndex(fromDelegates:=False)
                Dim delegateIndex = moduleBeingBuilt.GetNextAnonymousTypeIndex(fromDelegates:=True)
                For Each template In builder
                    Dim name As String = Nothing
                    Dim index As Integer = 0
                    If Not moduleBeingBuilt.TryGetAnonymousTypeName(template, name, index) Then
                        Select Case template.TypeKind
                            Case TypeKind.Delegate
                                index = delegateIndex
                                delegateIndex += 1
                            Case TypeKind.Class
                                index = typeIndex
                                typeIndex += 1
                            Case Else
                                Throw ExceptionUtilities.UnexpectedValue(template.TypeKind)
                        End Select
                        Dim slotIndex = Compilation.GetSubmissionSlotIndex()
                        name = GeneratedNames.MakeAnonymousTypeTemplateName(template.GeneratedNamePrefix, index, slotIndex, moduleId)
                    End If
                    ' normally it should only happen once, but in case there is a race
                    ' NameAndIndex.set has an assert which guarantees that the
                    ' template name provided is the same as the one already assigned
                    template.NameAndIndex = New NameAndIndex(name, index)
                Next

                Me.SealTemplates()
            End If

            If builder.Count > 0 AndAlso Not Me.CheckAndReportMissingSymbols(builder, diagnostics) Then

                ' Process all the templates
                For Each template In builder
                    template.Accept(compiler)
                Next
            End If

            builder.Free()
        End Sub

        Function GetModuleId(moduleBeingBuilt As Emit.PEModuleBuilder) As String
            ' If we are emitting .NET module, include module's name into type's name to ensure
            ' uniqueness across added modules.

            If moduleBeingBuilt.OutputKind = OutputKind.NetModule Then
                Dim moduleId = moduleBeingBuilt.Name
                Dim extension As String = OutputKind.NetModule.GetDefaultExtension()

                If moduleId.EndsWith(extension, StringComparison.OrdinalIgnoreCase) Then
                    moduleId = moduleId.Substring(0, moduleId.Length - extension.Length)
                End If

                Return "<" & MetadataHelpers.MangleForTypeNameIfNeeded(moduleId) & ">"
            Else
                Return String.Empty
            End If
        End Function

        Friend Function GetAnonymousTypeMap() As ImmutableSegmentedDictionary(Of Microsoft.CodeAnalysis.Emit.AnonymousTypeKey, Microsoft.CodeAnalysis.Emit.AnonymousTypeValue)
            Dim templates = ArrayBuilder(Of AnonymousTypeOrDelegateTemplateSymbol).GetInstance()
            GetAllCreatedTemplates(templates)

            Dim result = templates.ToImmutableSegmentedDictionary(
                keySelector:=Function(template) template.GetAnonymousTypeKey(),
                elementSelector:=Function(template) New Microsoft.CodeAnalysis.Emit.AnonymousTypeValue(template.NameAndIndex.Name, template.NameAndIndex.Index, template.GetCciAdapter()))

            templates.Free()
            Return result
        End Function

        ''' <summary>
        ''' Translates anonymous type public symbol into an implementation type symbol to be used in emit.
        ''' </summary>
        Friend Shared Function TranslateAnonymousTypeSymbol(type As NamedTypeSymbol) As NamedTypeSymbol
            Debug.Assert(type IsNot Nothing)
            Debug.Assert(type.IsAnonymousType)

            Dim anonymous = DirectCast(type, AnonymousTypeManager.AnonymousTypeOrDelegatePublicSymbol)
            Return anonymous.MapToImplementationSymbol()
        End Function

        ''' <summary>
        ''' Translates anonymous type method symbol into an implementation method symbol to be used in emit.
        ''' </summary>
        Friend Shared Function TranslateAnonymousTypeMethodSymbol(method As MethodSymbol) As MethodSymbol
            Dim type = method.ContainingType
            Debug.Assert(type.IsAnonymousType)

            Dim anonymousType = DirectCast(type, AnonymousTypeManager.AnonymousTypeOrDelegatePublicSymbol)
            Return anonymousType.MapMethodToImplementationSymbol(method)
        End Function

        ''' <summary> Returns all templates owned by this type manager </summary>
        Friend ReadOnly Property AllCreatedTemplates As ImmutableArray(Of NamedTypeSymbol)
            Get
                ' NOTE: templates may not be sealed at this point in case metadata is being emitted without IL
                Dim builder = ArrayBuilder(Of AnonymousTypeOrDelegateTemplateSymbol).GetInstance()
                GetAllCreatedTemplates(builder)
                Return StaticCast(Of NamedTypeSymbol).From(builder.ToImmutableAndFree())
            End Get
        End Property

        Private Sub GetAllCreatedTemplates(builder As ArrayBuilder(Of AnonymousTypeOrDelegateTemplateSymbol))
            Debug.Assert(Not builder.Any())

            AddFromCache(builder, Me._concurrentTypesCache)
            AddFromCache(builder, Me._concurrentDelegatesCache)

            If builder.Any() Then
                ' Sort types and delegates using smallest location
                builder.Sort(New AnonymousTypeComparer(Me.Compilation))
            End If
        End Sub

        Friend Overrides Function GetSynthesizedTypeMaps() As SynthesizedTypeMaps
            ' VB anonymous delegates are handled as anonymous types
            Return New SynthesizedTypeMaps(
                GetAnonymousTypeMap(),
                anonymousDelegates:=Nothing,
                anonymousDelegatesWithIndexedNames:=Nothing)
        End Function

        Private NotInheritable Class AnonymousTypeComparer
            Implements IComparer(Of AnonymousTypeOrDelegateTemplateSymbol)

            Private ReadOnly _compilation As VisualBasicCompilation

            Friend Sub New(compilation As VisualBasicCompilation)
                Me._compilation = compilation
            End Sub

            Public Function Compare(x As AnonymousTypeOrDelegateTemplateSymbol, y As AnonymousTypeOrDelegateTemplateSymbol) As Integer Implements IComparer(Of AnonymousTypeOrDelegateTemplateSymbol).Compare
                If x Is y Then
                    Return 0
                End If

                ' We compare two anonymous type templates by comparing their smallest locations
                ' NOTE: If anonymous type got to this phase it must have the location set
                Dim result = CompareLocations(x.SmallestLocation, y.SmallestLocation)
                If result = 0 Then
                    ' Two templates may have same location if they are created indirectly (for example for queries) 
                    ' and reference the same syntax node, in this case we also compare 'keys' of their descriptors
                    result = x.TypeDescriptorKey.CompareTo(y.TypeDescriptorKey)
                End If

                Debug.Assert(result <> 0)
                Return result
            End Function

            Private Function CompareLocations(x As Location, y As Location) As Integer
                If x Is y Then
                    Return 0
                ElseIf x = Location.None Then
                    Return -1
                ElseIf y = Location.None Then
                    Return 1
                Else
                    Return _compilation.CompareSourceLocations(x, y)
                End If
            End Function
        End Class

    End Class

End Namespace
