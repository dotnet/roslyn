' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend MustInherit Class SourceFieldSymbol
        Inherits FieldSymbol
        Implements IAttributeTargetSymbol

        ' Flags associated with the field
        Protected ReadOnly m_memberFlags As SourceMemberFlags

        Private ReadOnly _containingType As SourceMemberContainerTypeSymbol
        Private ReadOnly _name As String

        ' The syntax reference for this field (points to the name of the field)
        Private ReadOnly _syntaxRef As SyntaxReference

        Private _lazyDocComment As String
        Private _lazyExpandedDocComment As String
        Private _lazyCustomAttributesBag As CustomAttributesBag(Of VisualBasicAttributeData)

        ''' <summary>
        ''' See <see cref="StateFlags"/>
        ''' </summary>
        Protected _lazyState As Integer

        <Flags>
        Protected Enum StateFlags As Integer
            TypeConstraintsChecked = &H1

            EventProduced = &H2
        End Enum

        Protected Sub New(container As SourceMemberContainerTypeSymbol,
                          syntaxRef As SyntaxReference,
                          name As String,
                          memberFlags As SourceMemberFlags)

            Debug.Assert(container IsNot Nothing)
            Debug.Assert(syntaxRef IsNot Nothing)
            Debug.Assert(name IsNot Nothing)

            _name = name
            _containingType = container

            _syntaxRef = syntaxRef
            m_memberFlags = memberFlags
        End Sub

        Protected Overridable Sub GenerateDeclarationErrorsImpl(cancellationToken As CancellationToken)
            MyBase.GenerateDeclarationErrors(cancellationToken)

            Dim unusedType = Me.Type
            GetConstantValue(ConstantFieldsInProgress.Empty)
        End Sub

        Friend NotOverridable Overrides Sub GenerateDeclarationErrors(cancellationToken As CancellationToken)
            GenerateDeclarationErrorsImpl(cancellationToken)

            ' We want declaration events to be last, after all compilation analysis is done, so we produce them here
            Dim sourceModule = DirectCast(Me.ContainingModule, SourceModuleSymbol)
            If ThreadSafeFlagOperations.Set(_lazyState, StateFlags.EventProduced) AndAlso Not Me.IsImplicitlyDeclared Then
                sourceModule.DeclaringCompilation.SymbolDeclaredEvent(Me)
            End If
        End Sub

        ''' <summary>
        ''' Gets the syntax tree.
        ''' </summary>
        Friend ReadOnly Property SyntaxTree As SyntaxTree
            Get
                Return _syntaxRef.SyntaxTree
            End Get
        End Property

        Friend ReadOnly Property Syntax As VisualBasicSyntaxNode
            Get
                Return _syntaxRef.GetVisualBasicSyntax()
            End Get
        End Property

        Friend MustOverride ReadOnly Property DeclarationSyntax As VisualBasicSyntaxNode

        ''' <summary> 
        ''' Field initializer's declaration syntax node. 
        ''' It can be a EqualsValueSyntax or AsNewClauseSyntax.
        ''' </summary>
        Friend Overridable ReadOnly Property EqualsValueOrAsNewInitOpt As VisualBasicSyntaxNode
            Get
                Return Nothing
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _containingType
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Return _containingType
            End Get
        End Property

        Public ReadOnly Property ContainingSourceType As SourceMemberContainerTypeSymbol
            Get
                Return _containingType
            End Get
        End Property

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            If expandIncludes Then
                Return GetAndCacheDocumentationComment(Me, preferredCulture, expandIncludes, _lazyExpandedDocComment, cancellationToken)
            Else
                Return GetAndCacheDocumentationComment(Me, preferredCulture, expandIncludes, _lazyDocComment, cancellationToken)
            End If
        End Function

        ''' <summary>
        ''' Gets a value indicating whether this instance has declared type. This means not an inferred type.
        ''' </summary>
        ''' <value>
        ''' <c>true</c> if this instance has declared type; otherwise, <c>false</c>.
        ''' </value>
        Friend Overrides ReadOnly Property HasDeclaredType As Boolean
            Get
                Return (m_memberFlags And SourceMemberFlags.InferredFieldType) = 0
            End Get
        End Property

        Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return ImmutableArray(Of CustomModifier).Empty
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsRequired As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return CType((m_memberFlags And SourceMemberFlags.AccessibilityMask), Accessibility)
            End Get
        End Property

        Public Overrides ReadOnly Property IsReadOnly As Boolean
            Get
                Return (m_memberFlags And SourceMemberFlags.ReadOnly) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsConst As Boolean
            Get
                Return (m_memberFlags And SourceMemberFlags.Const) <> 0
            End Get
        End Property

        ''' <summary>
        ''' Gets the constant value.
        ''' </summary>
        ''' <param name="inProgress">Used to detect dependencies between constant field values.</param>
        Friend Overrides Function GetConstantValue(inProgress As ConstantFieldsInProgress) As ConstantValue
            Return Nothing
        End Function

        ''' <summary>
        ''' Helper to get a constant value of the field with cycle detection.
        ''' Also avoids deep recursion due to references to other constant fields in the value.
        ''' Derived types utilizing this helper should provide storage for the lazily calculated
        ''' <see cref="EvaluatedConstant"/> value and should implement the following APIs:
        ''' <see cref="GetLazyConstantTuple()"/>,
        ''' <see cref="SetLazyConstantTuple(EvaluatedConstant, BindingDiagnosticBag)"/>,
        ''' <see cref="MakeConstantTuple(ConstantFieldsInProgress.Dependencies, BindingDiagnosticBag)"/>.
        ''' </summary>
        Protected Function GetConstantValueImpl(inProgress As ConstantFieldsInProgress) As ConstantValue
            Dim constantTuple As EvaluatedConstant = GetLazyConstantTuple()
            If constantTuple IsNot Nothing Then
                Return constantTuple.Value
            End If

            If Not inProgress.IsEmpty Then
                ' Add this field as a dependency of the original field, and
                ' return ConstantValue.Bad. The outer caller will call
                ' this method again after evaluating any dependencies.
                inProgress.AddDependency(Me)
                Return CodeAnalysis.ConstantValue.Bad
            End If

            ' Order dependencies.
            Dim order = ArrayBuilder(Of ConstantValueUtils.FieldInfo).GetInstance()
            OrderAllDependencies(order)

            ' Evaluate fields in order.
            For Each info In order
                ' Bind the field value regardless of whether the field represents
                ' the start of a cycle. In the cycle case, there will be unevaluated
                ' dependencies and the result will be ConstantValue.Bad plus cycle error.
                info.Field.BindConstantTupleIfNecessary(info.StartsCycle)
            Next

            order.Free()

            ' Return the value of this field.
            Return GetLazyConstantTuple().Value
        End Function

        Private Sub BindConstantTupleIfNecessary(startsCycle As Boolean)
            If GetLazyConstantTuple() Is Nothing Then
                Dim builder = PooledHashSet(Of SourceFieldSymbol).GetInstance()
                Dim dependencies As New ConstantFieldsInProgress.Dependencies(builder)
                Dim diagnostics = BindingDiagnosticBag.GetInstance()
                Dim constantTuple As EvaluatedConstant = MakeConstantTuple(dependencies, diagnostics)
                dependencies.Freeze()

                If startsCycle Then
                    diagnostics.Clear()
                    diagnostics.Add(ERRID.ERR_CircularEvaluation1, GetFirstLocation(), CustomSymbolDisplayFormatter.ShortErrorName(Me))
                End If

                SetLazyConstantTuple(constantTuple, diagnostics)
                diagnostics.Free()
                builder.Free()
            End If
        End Sub

        ''' <summary>
        ''' Generate a list containing the field and all dependencies
        ''' of that field that require evaluation. The list is ordered by
        ''' dependencies, with fields with no dependencies first. Cycles are
        ''' broken at the first field lexically in the cycle. If multiple threads
        ''' call this method with the same field, the order of the fields
        ''' returned should be the same, although some fields may be missing
        ''' from the lists in some threads as other threads evaluate fields.
        ''' </summary>
        Private Sub OrderAllDependencies(order As ArrayBuilder(Of ConstantValueUtils.FieldInfo))
            Debug.Assert(order.Count = 0)

            Dim graph = PooledDictionary(Of SourceFieldSymbol, DependencyInfo).GetInstance()

            CreateGraph(graph)

            Debug.Assert(graph.Count >= 1)
            CheckGraph(graph)

#If DEBUG Then
            Dim fields = ArrayBuilder(Of SourceFieldSymbol).GetInstance()
            fields.AddRange(graph.Keys)
#End If

            OrderGraph(graph, order)

#If DEBUG Then
            ' Verify all entries in the graph are in the ordered list.
            Dim map = New HashSet(Of SourceFieldSymbol)(order.Select(Function(o) o.Field).Distinct())
            Debug.Assert(fields.All(Function(f) map.Contains(f)))
            fields.Free()
#End If

            graph.Free()
        End Sub

        Private Structure DependencyInfo
            ''' <summary>
            ''' The set of fields on which the field depends.
            ''' </summary>
            Public Dependencies As ImmutableHashSet(Of SourceFieldSymbol)

            ''' <summary>
            ''' The set of fields that depend on the field.
            ''' </summary>
            Public DependedOnBy As ImmutableHashSet(Of SourceFieldSymbol)
        End Structure

        ''' <summary>
        ''' Build a dependency graph (a map from
        ''' field to dependencies).
        ''' </summary>
        Private Sub CreateGraph(graph As Dictionary(Of SourceFieldSymbol, DependencyInfo))

            Dim pending = ArrayBuilder(Of SourceFieldSymbol).GetInstance()
            pending.Push(Me)

            While pending.Count > 0
                Dim field As SourceFieldSymbol = pending.Pop()

                Dim node As DependencyInfo = Nothing
                If graph.TryGetValue(field, node) Then
                    If node.Dependencies IsNot Nothing Then
                        ' Already visited node.
                        Continue While
                    End If
                Else
                    node = New DependencyInfo()
                    node.DependedOnBy = ImmutableHashSet(Of SourceFieldSymbol).Empty
                End If

                Dim dependencies As ImmutableHashSet(Of SourceFieldSymbol) = field.GetConstantValueDependencies()
                ' GetConstantValueDependencies will return an empty set if
                ' the constant value has already been calculated. That avoids
                ' calculating the full graph repeatedly. For instance with
                ' "Enum E : M0 = 0 : M1 = M0 + 1 : ... : Mn = Mn-1 + 1 : End Enum", we'll calculate
                ' the graph M0, ..., Mi for the first field we evaluate, Mi. But for
                ' the next field, Mj, we should only calculate the graph Mi, ..., Mj.
                node.Dependencies = dependencies
                graph(field) = node

                For Each dependency As SourceFieldSymbol In dependencies
                    pending.Push(dependency)

                    If Not graph.TryGetValue(dependency, node) Then
                        node = New DependencyInfo()
                        node.DependedOnBy = ImmutableHashSet(Of SourceFieldSymbol).Empty
                    End If

                    node.DependedOnBy = node.DependedOnBy.Add(field)
                    graph(dependency) = node
                Next
            End While

            pending.Free()
        End Sub

        ''' <summary>
        ''' Return the constant value dependencies. Compute the dependencies
        ''' if necessary by evaluating the constant value but only persist the
        ''' constant value if there were no dependencies. (If there are dependencies,
        ''' the constant value will be re-evaluated after evaluating dependencies.)
        ''' </summary>
        Private Function GetConstantValueDependencies() As ImmutableHashSet(Of SourceFieldSymbol)
            Dim valueTuple = GetLazyConstantTuple()
            If valueTuple IsNot Nothing Then
                ' Constant value already determined. No need to
                ' compute dependencies since the constant values
                ' of all dependencies should be evaluated as well.
                Return ImmutableHashSet(Of SourceFieldSymbol).Empty
            End If

            Dim builder = PooledHashSet(Of SourceFieldSymbol).GetInstance()
            Dim dependencies As New ConstantFieldsInProgress.Dependencies(builder)
            Dim diagnostics = BindingDiagnosticBag.GetInstance()
            valueTuple = MakeConstantTuple(dependencies, diagnostics)
            dependencies.Freeze()

            Dim result As ImmutableHashSet(Of SourceFieldSymbol)

            ' Only persist if there are no dependencies and the calculation
            ' completed successfully. (We could probably persist in other
            ' scenarios but it's probably not worth the added complexity.)
            If (builder.Count = 0) AndAlso
               Not valueTuple.Value.IsBad AndAlso
               Not diagnostics.HasAnyResolvedErrors() Then

                SetLazyConstantTuple(valueTuple, diagnostics)
                result = ImmutableHashSet(Of SourceFieldSymbol).Empty
            Else
                result = ImmutableHashSet(Of SourceFieldSymbol).Empty.Union(builder)
            End If

            diagnostics.Free()
            builder.Free()
            Return result
        End Function

        <Conditional("DEBUG")>
        Private Shared Sub CheckGraph(graph As Dictionary(Of SourceFieldSymbol, DependencyInfo))
            ' Avoid O(n^2) behavior by checking
            ' a maximum number of entries.
            Dim i As Integer = 10

            For Each pair In graph
                Dim field As SourceFieldSymbol = pair.Key
                Dim node As DependencyInfo = pair.Value

                Debug.Assert(node.Dependencies IsNot Nothing)
                Debug.Assert(node.DependedOnBy IsNot Nothing)

                For Each dependency As SourceFieldSymbol In node.Dependencies
                    Dim n As DependencyInfo = Nothing
                    Dim ok = graph.TryGetValue(dependency, n)
                    Debug.Assert(ok)
                    Debug.Assert(n.DependedOnBy.Contains(field))
                Next

                For Each dependedOnBy As SourceFieldSymbol In node.DependedOnBy
                    Dim n As DependencyInfo = Nothing
                    Dim ok = graph.TryGetValue(dependedOnBy, n)
                    Debug.Assert(ok)
                    Debug.Assert(n.Dependencies.Contains(field))
                Next

                i -= 1
                If i = 0 Then
                    Exit For
                End If
            Next

            Debug.Assert(graph.Values.Sum(Function(n) n.DependedOnBy.Count) = graph.Values.Sum(Function(n) n.Dependencies.Count))
        End Sub

        Private Shared Sub OrderGraph(graph As Dictionary(Of SourceFieldSymbol, DependencyInfo), order As ArrayBuilder(Of FieldInfo))
            Debug.Assert(graph.Count > 0)

            Dim lastUpdated As PooledHashSet(Of SourceFieldSymbol) = Nothing
            Dim fieldsInvolvedInCycles As ArrayBuilder(Of SourceFieldSymbol) = Nothing

            While graph.Count > 0
                ' Get the set of fields in the graph that have no dependencies.
                Dim search = If(DirectCast(lastUpdated, IEnumerable(Of SourceFieldSymbol)), graph.Keys)
                Dim [set] = ArrayBuilder(Of SourceFieldSymbol).GetInstance()
                For Each field In search
                    Dim node As DependencyInfo = Nothing
                    If graph.TryGetValue(field, node) AndAlso node.Dependencies.Count = 0 Then
                        [set].Add(field)
                    End If
                Next

                lastUpdated?.Free()
                lastUpdated = Nothing
                If [set].Count > 0 Then
                    Dim updated = PooledHashSet(Of SourceFieldSymbol).GetInstance()

                    ' Remove fields with no dependencies from the graph.
                    For Each field In [set]
                        Dim node = graph(field)

                        ' Remove the field from the Dependencies
                        ' of each field that depends on it.
                        For Each dependedOnBy In node.DependedOnBy
                            Dim n = graph(dependedOnBy)
                            n.Dependencies = n.Dependencies.Remove(field)
                            graph(dependedOnBy) = n
                            updated.Add(dependedOnBy)
                        Next

                        graph.Remove(field)
                    Next

                    CheckGraph(graph)

                    ' Add the set to the ordered list.
                    For Each item In [set]
                        order.Add(New FieldInfo(item, startsCycle:=False))
                    Next

                    lastUpdated = updated
                Else
                    ' All fields have dependencies which means all fields are involved
                    ' in cycles. Break the first cycle found. (Note some fields may have
                    ' dependencies but are not strictly part of any cycle. For instance,
                    ' B And C in: "Enum E : A = A + B : B = C : C = D : D = D : End Enum").
                    Dim field = GetStartOfFirstCycle(graph, fieldsInvolvedInCycles)

                    ' Break the dependencies.
                    Dim node = graph(field)

                    ' Remove the field from the DependedOnBy
                    ' of each field it has as a dependency.
                    For Each dependency In node.Dependencies
                        Dim n = graph(dependency)
                        n.DependedOnBy = n.DependedOnBy.Remove(field)
                        graph(dependency) = n
                    Next

                    node = graph(field)
                    Dim updated = PooledHashSet(Of SourceFieldSymbol).GetInstance()

                    ' Remove the field from the Dependencies
                    ' of each field that depends on it.
                    For Each dependedOnBy In node.DependedOnBy
                        Dim n = graph(dependedOnBy)
                        n.Dependencies = n.Dependencies.Remove(field)
                        graph(dependedOnBy) = n
                        updated.Add(dependedOnBy)
                    Next

                    graph.Remove(field)

                    CheckGraph(graph)

                    ' Add the start of the cycle to the ordered list.
                    order.Add(New FieldInfo(field, startsCycle:=True))

                    lastUpdated = updated
                End If

                [set].Free()
            End While

            lastUpdated?.Free()
            fieldsInvolvedInCycles?.Free()
        End Sub

        Private Shared Function GetStartOfFirstCycle(
            graph As Dictionary(Of SourceFieldSymbol, DependencyInfo),
            ByRef fieldsInvolvedInCycles As ArrayBuilder(Of SourceFieldSymbol)
        ) As SourceFieldSymbol
            Debug.Assert(graph.Count > 0)

            If fieldsInvolvedInCycles Is Nothing Then
                fieldsInvolvedInCycles = ArrayBuilder(Of SourceFieldSymbol).GetInstance(graph.Count)
                ' We sort fields that belong to the same compilation by location to process cycles in deterministic order.
                ' Relative order between compilations is not important, cycles do not cross compilation boundaries. 
                fieldsInvolvedInCycles.AddRange(graph.Keys.GroupBy(Function(f) f.DeclaringCompilation).
                    SelectMany(Function(g) g.OrderByDescending(Function(f1, f2) g.Key.CompareSourceLocations(f1.GetFirstLocation(), f2.GetFirstLocation()))))
            End If

            Do
                Dim field As SourceFieldSymbol = fieldsInvolvedInCycles.Pop()

                If graph.ContainsKey(field) AndAlso IsPartOfCycle(graph, field) Then
                    Return field
                End If
            Loop
        End Function

        Private Shared Function IsPartOfCycle(graph As Dictionary(Of SourceFieldSymbol, DependencyInfo), field As SourceFieldSymbol) As Boolean
            Dim [set] = PooledHashSet(Of SourceFieldSymbol).GetInstance()
            Dim stack = ArrayBuilder(Of SourceFieldSymbol).GetInstance()

            Dim stopAt As SourceFieldSymbol = field
            Dim result As Boolean = False
            stack.Push(field)

            While stack.Count > 0
                field = stack.Pop()
                Dim node = graph(field)

                If node.Dependencies.Contains(stopAt) Then
                    result = True
                    Exit While
                End If

                For Each dependency In node.Dependencies
                    If [set].Add(dependency) Then
                        stack.Push(dependency)
                    End If
                Next
            End While

            stack.Free()
            [set].Free()
            Return result
        End Function

        ''' <summary>
        ''' Should be overridden by types utilizing <see cref="GetConstantValueImpl(ConstantFieldsInProgress)"/> helper.
        ''' </summary>
        Protected Overridable Function GetLazyConstantTuple() As EvaluatedConstant
            Throw ExceptionUtilities.Unreachable
        End Function

        ''' <summary>
        ''' Should be overridden by types utilizing <see cref="GetConstantValueImpl(ConstantFieldsInProgress)"/> helper.
        ''' </summary>
        Protected Overridable Sub SetLazyConstantTuple(constantTuple As EvaluatedConstant, diagnostics As BindingDiagnosticBag)
            Throw ExceptionUtilities.Unreachable
        End Sub

        ''' <summary>
        ''' Should be overridden by types utilizing <see cref="GetConstantValueImpl(ConstantFieldsInProgress)"/> helper.
        ''' </summary>
        Protected Overridable Function MakeConstantTuple(dependencies As ConstantFieldsInProgress.Dependencies, diagnostics As BindingDiagnosticBag) As EvaluatedConstant
            Throw ExceptionUtilities.Unreachable
        End Function

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return (m_memberFlags And SourceMemberFlags.Shared) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return _containingType.AreMembersImplicitlyDeclared
            End Get
        End Property

        Friend Overrides ReadOnly Property ShadowsExplicitly As Boolean
            Get
                Return (m_memberFlags And SourceMemberFlags.Shadows) <> 0
            End Get
        End Property

        Friend Overrides Function GetLexicalSortKey() As LexicalSortKey
            ' WARNING: this should not allocate memory!
            Return New LexicalSortKey(_syntaxRef, Me.DeclaringCompilation)
        End Function

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray.Create(Of Location)(GetSymbolLocation(_syntaxRef))
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return GetDeclaringSyntaxReferenceHelper(_syntaxRef)
            End Get
        End Property

        Friend MustOverride ReadOnly Property GetAttributeDeclarations() As OneOrMany(Of SyntaxList(Of AttributeListSyntax))

        Public ReadOnly Property DefaultAttributeLocation As AttributeLocation Implements IAttributeTargetSymbol.DefaultAttributeLocation
            Get
                Return AttributeLocation.Field
            End Get
        End Property

        ''' <summary>
        ''' Gets the attributes applied on this symbol.
        ''' Returns an empty array if there are no attributes.
        ''' </summary>
        ''' <remarks>
        ''' NOTE: This method should always be kept as a NotOverridable method.
        ''' If you want to override attribute binding logic for a sub-class, then override <see cref="GetAttributesBag"/> method.
        ''' </remarks>
        Public NotOverridable Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return Me.GetAttributesBag().Attributes
        End Function

        Private Function GetAttributesBag() As CustomAttributesBag(Of VisualBasicAttributeData)
            If _lazyCustomAttributesBag Is Nothing OrElse Not _lazyCustomAttributesBag.IsSealed Then
                LoadAndValidateAttributes(GetAttributeDeclarations(), _lazyCustomAttributesBag)
            End If
            Return _lazyCustomAttributesBag
        End Function

        Private Function GetDecodedWellKnownAttributeData() As CommonFieldWellKnownAttributeData
            Dim attributesBag As CustomAttributesBag(Of VisualBasicAttributeData) = Me._lazyCustomAttributesBag
            If attributesBag Is Nothing OrElse Not attributesBag.IsDecodedWellKnownAttributeDataComputed Then
                attributesBag = Me.GetAttributesBag()
            End If

            Return DirectCast(attributesBag.DecodedWellKnownAttributeData, CommonFieldWellKnownAttributeData)
        End Function

        ' This should be called at most once after the attributes are bound.  Attributes must be bound after the class
        ' and members are fully declared to avoid infinite recursion.
        Friend Sub SetCustomAttributeData(attributeData As CustomAttributesBag(Of VisualBasicAttributeData))
            Debug.Assert(attributeData IsNot Nothing)
            Debug.Assert(_lazyCustomAttributesBag Is Nothing)

            _lazyCustomAttributesBag = attributeData
        End Sub

        Friend Overrides Sub AddSynthesizedAttributes(moduleBuilder As PEModuleBuilder, ByRef attributes As ArrayBuilder(Of VisualBasicAttributeData))
            MyBase.AddSynthesizedAttributes(moduleBuilder, attributes)

            If Me.IsConst Then
                If Me.GetConstantValue(ConstantFieldsInProgress.Empty) IsNot Nothing Then
                    Dim data = GetDecodedWellKnownAttributeData()
                    If data Is Nothing OrElse data.ConstValue = CodeAnalysis.ConstantValue.Unset Then
                        If Me.Type.SpecialType = SpecialType.System_DateTime Then
                            Dim attributeValue = DirectCast(Me.ConstantValue, DateTime)

                            Dim specialTypeInt64 = Me.ContainingAssembly.GetSpecialType(SpecialType.System_Int64)
                            ' NOTE: used from emit, so shouldn't have gotten here if there were errors
                            Debug.Assert(specialTypeInt64.GetUseSiteInfo().DiagnosticInfo Is Nothing)

                            Dim compilation = Me.DeclaringCompilation

                            AddSynthesizedAttribute(attributes, compilation.TrySynthesizeAttribute(
                            WellKnownMember.System_Runtime_CompilerServices_DateTimeConstantAttribute__ctor,
                            ImmutableArray.Create(
                                New TypedConstant(specialTypeInt64, TypedConstantKind.Primitive, attributeValue.Ticks))))

                        ElseIf Me.Type.SpecialType = SpecialType.System_Decimal Then
                            Dim attributeValue = DirectCast(Me.ConstantValue, Decimal)

                            Dim compilation = Me.DeclaringCompilation
                            AddSynthesizedAttribute(attributes, compilation.SynthesizeDecimalConstantAttribute(attributeValue))
                        End If
                    End If
                End If
            End If

            If Me.Type.ContainsTupleNames() Then
                AddSynthesizedAttribute(attributes, DeclaringCompilation.SynthesizeTupleNamesAttribute(Type))
            End If
        End Sub

        Friend NotOverridable Overrides Function EarlyDecodeWellKnownAttribute(ByRef arguments As EarlyDecodeWellKnownAttributeArguments(Of EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation)) As VisualBasicAttributeData
            Debug.Assert(arguments.AttributeType IsNot Nothing)
            Debug.Assert(Not arguments.AttributeType.IsErrorType())

            Dim BoundAttribute As VisualBasicAttributeData = Nothing
            Dim obsoleteData As ObsoleteAttributeData = Nothing

            If EarlyDecodeDeprecatedOrExperimentalOrObsoleteAttribute(arguments, BoundAttribute, obsoleteData) Then
                If obsoleteData IsNot Nothing Then
                    arguments.GetOrCreateData(Of CommonFieldEarlyWellKnownAttributeData)().ObsoleteAttributeData = obsoleteData
                End If

                Return BoundAttribute
            End If

            Return MyBase.EarlyDecodeWellKnownAttribute(arguments)
        End Function

        Friend NotOverridable Overrides Sub DecodeWellKnownAttribute(ByRef arguments As DecodeWellKnownAttributeArguments(Of AttributeSyntax, VisualBasicAttributeData, AttributeLocation))
            Debug.Assert(arguments.AttributeSyntaxOpt IsNot Nothing)

            Dim attrData = arguments.Attribute
            Debug.Assert(arguments.SymbolPart = AttributeLocation.None)
            Dim diagnostics = DirectCast(arguments.Diagnostics, BindingDiagnosticBag)

            If attrData.IsTargetAttribute(AttributeDescription.TupleElementNamesAttribute) Then
                diagnostics.Add(ERRID.ERR_ExplicitTupleElementNamesAttribute, arguments.AttributeSyntaxOpt.Location)
            End If

            If attrData.IsTargetAttribute(AttributeDescription.SpecialNameAttribute) Then
                arguments.GetOrCreateData(Of CommonFieldWellKnownAttributeData)().HasSpecialNameAttribute = True
            ElseIf attrData.IsTargetAttribute(AttributeDescription.NonSerializedAttribute) Then

                If Me.ContainingType.IsSerializable Then
                    arguments.GetOrCreateData(Of CommonFieldWellKnownAttributeData)().HasNonSerializedAttribute = True
                Else
                    diagnostics.Add(ERRID.ERR_InvalidNonSerializedUsage, arguments.AttributeSyntaxOpt.GetLocation())
                End If

            ElseIf attrData.IsTargetAttribute(AttributeDescription.FieldOffsetAttribute) Then
                Dim offset = attrData.CommonConstructorArguments(0).DecodeValue(Of Integer)(SpecialType.System_Int32)
                If offset < 0 Then
                    diagnostics.Add(ERRID.ERR_BadAttribute1, VisualBasicAttributeData.GetFirstArgumentLocation(arguments.AttributeSyntaxOpt), attrData.AttributeClass)
                    offset = 0
                End If

                arguments.GetOrCreateData(Of CommonFieldWellKnownAttributeData)().SetFieldOffset(offset)

            ElseIf attrData.IsTargetAttribute(AttributeDescription.MarshalAsAttribute) Then
                MarshalAsAttributeDecoder(Of CommonFieldWellKnownAttributeData, AttributeSyntax, VisualBasicAttributeData, AttributeLocation).Decode(arguments, AttributeTargets.Field, MessageProvider.Instance)
            ElseIf attrData.IsTargetAttribute(AttributeDescription.DateTimeConstantAttribute) Then
                VerifyConstantValueMatches(attrData.DecodeDateTimeConstantValue(), arguments)
            ElseIf attrData.IsTargetAttribute(AttributeDescription.DecimalConstantAttribute) Then
                VerifyConstantValueMatches(attrData.DecodeDecimalConstantValue(), arguments)
            Else
                MyBase.DecodeWellKnownAttribute(arguments)
            End If
        End Sub

        ''' <summary>
        ''' Verify the constant value matches the default value from any earlier attribute
        ''' (DateTimeConstantAttribute or DecimalConstantAttribute).
        ''' If not, report ERR_FieldHasMultipleDistinctConstantValues.
        ''' </summary>
        Private Sub VerifyConstantValueMatches(attrValue As ConstantValue, ByRef arguments As DecodeWellKnownAttributeArguments(Of AttributeSyntax, VisualBasicAttributeData, AttributeLocation))
            Dim data = arguments.GetOrCreateData(Of CommonFieldWellKnownAttributeData)()
            Dim constValue As ConstantValue
            Dim diagnostics = DirectCast(arguments.Diagnostics, BindingDiagnosticBag)

            If Me.IsConst Then
                If Me.Type.IsDecimalType() OrElse Me.Type.IsDateTimeType() Then
                    constValue = Me.GetConstantValue(ConstantFieldsInProgress.Empty)

                    If constValue IsNot Nothing AndAlso Not constValue.IsBad AndAlso constValue <> attrValue Then
                        diagnostics.Add(ERRID.ERR_FieldHasMultipleDistinctConstantValues, arguments.AttributeSyntaxOpt.GetLocation())
                    End If
                Else
                    diagnostics.Add(ERRID.ERR_FieldHasMultipleDistinctConstantValues, arguments.AttributeSyntaxOpt.GetLocation())
                End If

                If data.ConstValue = CodeAnalysis.ConstantValue.Unset Then
                    data.ConstValue = attrValue
                End If
            Else
                constValue = data.ConstValue

                If constValue <> CodeAnalysis.ConstantValue.Unset Then
                    If constValue <> attrValue Then
                        diagnostics.Add(ERRID.ERR_FieldHasMultipleDistinctConstantValues, arguments.AttributeSyntaxOpt.GetLocation())
                    End If
                Else
                    data.ConstValue = attrValue
                End If
            End If
        End Sub

        Friend NotOverridable Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                If HasRuntimeSpecialName Then
                    Return True
                End If

                Dim data = GetDecodedWellKnownAttributeData()
                Return data IsNot Nothing AndAlso data.HasSpecialNameAttribute
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                Return Name = WellKnownMemberNames.EnumBackingFieldName
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property IsNotSerialized As Boolean
            Get
                Dim data = GetDecodedWellKnownAttributeData()
                Return data IsNot Nothing AndAlso data.HasNonSerializedAttribute
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property MarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Dim data = GetDecodedWellKnownAttributeData()
                Return If(data IsNot Nothing, data.MarshallingInformation, Nothing)
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property TypeLayoutOffset As Integer?
            Get
                Dim data = GetDecodedWellKnownAttributeData()
                Return If(data IsNot Nothing, data.Offset, Nothing)
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                ' If there are no attributes then this symbol is not Obsolete.
                If (Not Me._containingType.AnyMemberHasAttributes) Then
                    Return Nothing
                End If

                Dim lazyCustomAttributesBag = Me._lazyCustomAttributesBag
                If (lazyCustomAttributesBag IsNot Nothing AndAlso lazyCustomAttributesBag.IsEarlyDecodedWellKnownAttributeDataComputed) Then
                    Dim data = DirectCast(_lazyCustomAttributesBag.EarlyDecodedWellKnownAttributeData, CommonFieldEarlyWellKnownAttributeData)
                    Return If(data IsNot Nothing, data.ObsoleteAttributeData, Nothing)
                End If

                Return ObsoleteAttributeData.Uninitialized
            End Get
        End Property

        ' Given a syntax ref, get the symbol location to return. We return the location of the name
        ' of the method.
        Private Shared Function GetSymbolLocation(syntaxRef As SyntaxReference) As Location
            Dim syntaxNode = syntaxRef.GetSyntax()
            Dim syntaxTree = syntaxRef.SyntaxTree

            Return syntaxTree.GetLocation(GetFieldLocationFromSyntax(DirectCast(syntaxNode, ModifiedIdentifierSyntax).Identifier))
        End Function

        ' Get the location of a field given the syntax for its modified identifier. We use the span of the base part
        ' of the identifier.
        Private Shared Function GetFieldLocationFromSyntax(node As SyntaxToken) As TextSpan
            Return node.Span
        End Function

        ' Given the syntax declaration, and a container, get the field or WithEvents property symbol declared from that syntax.
        ' This is done by lookup up the name from the declaration in the container, handling duplicates and
        ' so forth correctly.
        Friend Shared Function FindFieldOrWithEventsSymbolFromSyntax(variableName As SyntaxToken,
                                                    tree As SyntaxTree,
                                                    container As NamedTypeSymbol) As Symbol
            Dim fieldName As String = variableName.ValueText
            Dim nameSpan As TextSpan = GetFieldLocationFromSyntax(variableName)
            Return container.FindFieldOrProperty(fieldName, nameSpan, tree)
        End Function
    End Class
End Namespace
