' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.PooledObjects
#If DEBUG Then
' We use a struct rather than a class to represent the state for efficiency
' for data flow analysis, with 32 bits of data inline. Merely copying the state
' variable causes the first 32 bits to be cloned, as they are inline. This can
' hide a plethora of errors that would only be exhibited in programs with more
' than 32 variables to be tracked. However, few of our tests have that many
' variables.
'
' To help diagnose these problems, we use the preprocessor symbol REFERENCE_STATE
' to cause the data flow state be a class rather than a struct. When it is a class,
' this category of problems would be exhibited in programs with a small number of
' tracked variables. But it is slower, so we only do it in DEBUG mode.
#Const REFERENCE_STATE = True
#End If

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#If REFERENCE_STATE Then
Imports OptionalState = Microsoft.CodeAnalysis.[Optional](Of Microsoft.CodeAnalysis.VisualBasic.DataFlowPass.LocalState)
#Else
Imports OptionalState = System.Nullable(Of Microsoft.CodeAnalysis.VisualBasic.DataFlowPass.LocalState)
#End If

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class DataFlowPass
        Inherits AbstractFlowPass(Of LocalState)

        Public Enum SlotKind
            ''' <summary>
            '''  Special slot for untracked variables
            ''' </summary>
            NotTracked = -1
            ''' <summary>
            ''' Special slot for tracking whether code is reachable
            ''' </summary>
            Unreachable = 0
            ''' <summary>
            ''' Special slot for tracking the implicit local for the function return value
            ''' </summary>
            FunctionValue = 1
            ''' <summary>
            ''' The first available slot for variables
            ''' </summary>
            ''' <remarks></remarks>
            FirstAvailable = 2
        End Enum

        ''' <summary>
        ''' Some variables that should be considered initially assigned.  Used for region analysis.
        ''' </summary>
        Protected ReadOnly initiallyAssignedVariables As HashSet(Of Symbol)

        ''' <summary>
        ''' Defines whether or not fields of intrinsic type should be tracked. Such fields should 
        ''' not be tracked for error reporting purposes, but should be tracked for region flow analysis
        ''' </summary>
        Private ReadOnly _trackStructsWithIntrinsicTypedFields As Boolean

        ''' <summary>
        ''' Variables that were used anywhere, in the sense required to suppress warnings about unused variables.
        ''' </summary>
        Private ReadOnly _unusedVariables As HashSet(Of LocalSymbol) = New HashSet(Of LocalSymbol)()

        ''' <summary>
        ''' Variables that were initialized or written anywhere.
        ''' </summary>
        Private ReadOnly _writtenVariables As HashSet(Of Symbol) = New HashSet(Of Symbol)()

        ''' <summary> 
        ''' A mapping from local variables to the index of their slot in a flow analysis local state. 
        ''' WARNING: if variable identifier maps into SlotKind.NotTracked, it may mean that VariableIdentifier 
        '''          is a structure without traceable fields. This mapping is created in MakeSlotImpl(...)
        ''' </summary>
        Private ReadOnly _variableSlot As Dictionary(Of VariableIdentifier, Integer) = New Dictionary(Of VariableIdentifier, Integer)()

        ''' <summary>
        ''' A mapping from the local variable slot to the symbol for the local variable itself.  This is used in the
        ''' implementation of region analysis (support for extract method) to compute the set of variables "always
        ''' assigned" in a region of code.
        ''' </summary>
        Protected variableBySlot As VariableIdentifier() = New VariableIdentifier(10) {}

        ''' <summary>
        ''' Variable slots are allocated to local variables sequentially and never reused.  This is
        ''' the index of the next slot number to use.
        ''' </summary>
        Protected nextVariableSlot As Integer = SlotKind.FirstAvailable

        ''' <summary>
        ''' Tracks variables for which we have already reported a definite assignment error.  This
        ''' allows us to report at most one such error per variable.
        ''' </summary>
        Private _alreadyReported As BitVector

        ''' <summary>
        ''' Did we see [On Error] or [Resume] statement? Used to suppress some diagnostics.
        ''' </summary>
        Private _seenOnErrorOrResume As Boolean

        Protected _convertInsufficientExecutionStackExceptionToCancelledByStackGuardException As Boolean = False ' By default, just let the original exception to bubble up.

        Friend Sub New(info As FlowAnalysisInfo, suppressConstExpressionsSupport As Boolean, Optional trackStructsWithIntrinsicTypedFields As Boolean = False)
            MyBase.New(info, suppressConstExpressionsSupport)
            Me.initiallyAssignedVariables = Nothing
            Me._trackStructsWithIntrinsicTypedFields = trackStructsWithIntrinsicTypedFields
        End Sub

        Friend Sub New(info As FlowAnalysisInfo, region As FlowAnalysisRegionInfo,
                       suppressConstExpressionsSupport As Boolean,
                       Optional initiallyAssignedVariables As HashSet(Of Symbol) = Nothing,
                       Optional trackUnassignments As Boolean = False,
                       Optional trackStructsWithIntrinsicTypedFields As Boolean = False)

            MyBase.New(info, region, suppressConstExpressionsSupport, trackUnassignments)
            Me.initiallyAssignedVariables = initiallyAssignedVariables
            Me._trackStructsWithIntrinsicTypedFields = trackStructsWithIntrinsicTypedFields
        End Sub

        Protected Overrides Sub InitForScan()
            MyBase.InitForScan()

            For Each parameter In MethodParameters
                GetOrCreateSlot(parameter)
            Next

            Me._alreadyReported = BitVector.Empty          ' no variables yet reported unassigned
            Me.EnterParameters(MethodParameters)         ' with parameters assigned
            Dim methodMeParameter = Me.MeParameter       ' and with 'Me' assigned as well
            If methodMeParameter IsNot Nothing Then
                Me.GetOrCreateSlot(methodMeParameter)
                Me.EnterParameter(methodMeParameter)
            End If

            ' Usually we need to treat locals used without being assigned first as assigned in the beginning of the block.
            ' When data flow analysis determines that the variable is sometimes used without being assigned first, we want 
            ' to treat that variable, during region analysis, as assigned where it is introduced.
            ' However with goto statements that branch into for/foreach/using statements (error case) we need to treat them
            ' as assigned in the beginning of the method body.
            '
            ' Example:
            ' Goto label1
            ' For x = 0 to 10
            ' label1:
            '    Dim y = [| x |]  ' x should not be in dataFlowsOut.
            ' Next
            '
            ' This can only happen in VB because the labels are visible in the whole method. In C# the labels are not visible
            ' this case.            
            '
            If initiallyAssignedVariables IsNot Nothing Then
                For Each local In initiallyAssignedVariables
                    SetSlotAssigned(GetOrCreateSlot(local))
                Next
            End If
        End Sub

        Protected Overrides Function Scan() As Boolean
            If Not MyBase.Scan() Then
                Return False
            End If

            ' check unused variables
            If Not _seenOnErrorOrResume Then
                For Each local In _unusedVariables
                    ReportUnused(local)
                Next
            End If

            ' VB doesn't support "out" so it doesn't warn for unassigned parameters. However, check variables passed
            ' byref are assigned so that data flow analysis detects parameters flowing out.
            If ShouldAnalyzeByRefParameters Then
                LeaveParameters(MethodParameters)
            End If
            Dim methodMeParameter = Me.MeParameter       ' and also 'Me'
            If methodMeParameter IsNot Nothing Then
                Me.LeaveParameter(methodMeParameter)
            End If

            Return True
        End Function

        Private Sub ReportUnused(local As LocalSymbol)
            ' Never report that the function value is unused
            ' Never report that local with empty name is unused
            ' Locals with empty name can be generated in code with syntax errors and
            ' we shouldn't report such locals as unused because they were not explicitly declared
            ' by the user in the code
            If Not local.IsFunctionValue AndAlso Not String.IsNullOrEmpty(local.Name) Then
                If _writtenVariables.Contains(local) Then
                    If local.IsConst Then
                        Me.diagnostics.Add(ERRID.WRN_UnusedLocalConst, local.GetFirstLocation(), If(local.Name, "dummy"))
                    End If
                Else
                    Me.diagnostics.Add(ERRID.WRN_UnusedLocal, local.GetFirstLocation(), If(local.Name, "dummy"))
                End If
            End If
        End Sub

        Protected Overridable Sub ReportUnassignedByRefParameter(parameter As ParameterSymbol)
        End Sub

        ''' <summary>
        ''' Perform data flow analysis, reporting all necessary diagnostics.
        ''' </summary>
        Public Overloads Shared Sub Analyze(info As FlowAnalysisInfo, diagnostics As DiagnosticBag, suppressConstExpressionsSupport As Boolean)
            Dim walker = New DataFlowPass(info, suppressConstExpressionsSupport)

            If diagnostics IsNot Nothing Then
                walker._convertInsufficientExecutionStackExceptionToCancelledByStackGuardException = True
            End If

            Try
                Dim result As Boolean = walker.Analyze()
                Debug.Assert(result)
                If diagnostics IsNot Nothing Then
                    diagnostics.AddRange(walker.diagnostics)
                End If

            Catch ex As CancelledByStackGuardException When diagnostics IsNot Nothing
                ex.AddAnError(diagnostics)
            Finally
                walker.Free()
            End Try
        End Sub

        Protected Overrides Function ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException() As Boolean
            Return _convertInsufficientExecutionStackExceptionToCancelledByStackGuardException
        End Function

        Protected Overrides Sub Free()
            Me._alreadyReported = BitVector.Null
            MyBase.Free()
        End Sub

        Protected Overrides Function Dump(state As LocalState) As String
            Dim builder As New StringBuilder()
            builder.Append("[assigned ")
            AppendBitNames(state.Assigned, builder)
            builder.Append("]")
            Return builder.ToString()
        End Function

        Protected Sub AppendBitNames(a As BitVector, builder As StringBuilder)
            Dim any As Boolean = False
            For Each bit In a.TrueBits
                If any Then
                    builder.Append(", ")
                End If
                any = True
                AppendBitName(bit, builder)
            Next
        End Sub

        Protected Sub AppendBitName(bit As Integer, builder As StringBuilder)
            Dim id As VariableIdentifier = variableBySlot(bit)
            If id.ContainingSlot > 0 Then
                AppendBitName(id.ContainingSlot, builder)
                builder.Append(".")
            End If
            builder.Append(If(bit = 0, "*", id.Symbol.Name))
        End Sub

#Region "Note Read/Write"

        Protected Overridable Sub NoteRead(variable As Symbol)
            If variable IsNot Nothing AndAlso variable.Kind = SymbolKind.Local Then
                _unusedVariables.Remove(DirectCast(variable, LocalSymbol))
            End If
        End Sub

        Protected Overridable Sub NoteWrite(variable As Symbol, value As BoundExpression)
            If variable IsNot Nothing Then
                _writtenVariables.Add(variable)
                Dim local = TryCast(variable, LocalSymbol)
                If value IsNot Nothing AndAlso (local IsNot Nothing AndAlso Not local.IsConst AndAlso local.Type.IsReferenceType OrElse value.HasErrors) Then
                    ' We duplicate Dev10's behavior here.  The reasoning is, I would guess, that otherwise unread
                    ' variables of reference type are useful because they keep objects alive, i.e., they are read by the
                    ' VM.  And variables that are initialized by non-constant expressions presumably are useful because
                    ' they enable us to clearly document a discarded return value from a method invocation, e.g. var
                    ' discardedValue = F(); >shrug<
                    ' Note, this rule only applies to local variables and not to const locals, i.e. we always report unused 
                    ' const locals. In addition, if the value has errors then suppress these diagnostics in all cases.
                    _unusedVariables.Remove(local)
                End If
            End If
        End Sub

        Protected Function GetNodeSymbol(node As BoundNode) As Symbol
            While node IsNot Nothing
                Select Case node.Kind

                    Case BoundKind.FieldAccess
                        Dim fieldAccess = DirectCast(node, BoundFieldAccess)
                        If FieldAccessMayRequireTracking(fieldAccess) Then
                            node = fieldAccess.ReceiverOpt
                            Continue While
                        End If
                        Return Nothing

                    Case BoundKind.PropertyAccess
                        node = DirectCast(node, BoundPropertyAccess).ReceiverOpt
                        Continue While

                    Case BoundKind.MeReference
                        Return MeParameter

                    Case BoundKind.Local
                        Return DirectCast(node, BoundLocal).LocalSymbol

                    Case BoundKind.RangeVariable
                        Return DirectCast(node, BoundRangeVariable).RangeVariable

                    Case BoundKind.Parameter
                        Return DirectCast(node, BoundParameter).ParameterSymbol

                    Case BoundKind.WithLValueExpressionPlaceholder, BoundKind.WithRValueExpressionPlaceholder
                        Dim substitute As BoundExpression = GetPlaceholderSubstitute(DirectCast(node, BoundValuePlaceholderBase))
                        If substitute IsNot Nothing Then
                            Return GetNodeSymbol(substitute)
                        End If

                        Return Nothing

                    Case BoundKind.LocalDeclaration
                        Return DirectCast(node, BoundLocalDeclaration).LocalSymbol

                    Case BoundKind.ForToStatement,
                        BoundKind.ForEachStatement
                        Return DirectCast(node, BoundForStatement).DeclaredOrInferredLocalOpt

                    Case BoundKind.ByRefArgumentWithCopyBack
                        node = DirectCast(node, BoundByRefArgumentWithCopyBack).OriginalArgument
                        Continue While

                    Case Else
                        Return Nothing

                End Select
            End While
            Return Nothing
        End Function

        Protected Overridable Sub NoteWrite(node As BoundExpression, value As BoundExpression)
            Dim symbol As Symbol = GetNodeSymbol(node)

            If symbol IsNot Nothing Then

                If symbol.Kind = SymbolKind.Local AndAlso DirectCast(symbol, LocalSymbol).DeclarationKind = LocalDeclarationKind.AmbiguousLocals Then
                    ' We have several locals grouped in AmbiguousLocalsPseudoSymbol
                    For Each local In DirectCast(symbol, AmbiguousLocalsPseudoSymbol).Locals
                        NoteWrite(local, value)
                    Next

                Else
                    NoteWrite(symbol, value)
                End If
            End If
        End Sub

        Protected Overridable Sub NoteRead(fieldAccess As BoundFieldAccess)
            Dim symbol As Symbol = GetNodeSymbol(fieldAccess)
            If symbol IsNot Nothing Then

                If symbol.Kind = SymbolKind.Local AndAlso DirectCast(symbol, LocalSymbol).DeclarationKind = LocalDeclarationKind.AmbiguousLocals Then
                    ' We have several locals grouped in AmbiguousLocalsPseudoSymbol
                    For Each local In DirectCast(symbol, AmbiguousLocalsPseudoSymbol).Locals
                        NoteRead(local)
                    Next

                Else
                    NoteRead(symbol)
                End If
            End If
        End Sub

#End Region

#Region "Operations with slots"

        ''' <summary>
        ''' Locals are given slots when their declarations are encountered.  We only need give slots to local variables, and 
        ''' the "Me" variable of a structure constructs. Other variables are not given slots, and are therefore not tracked 
        ''' by the analysis.  This returns SlotKind.NotTracked for a variable that is not tracked, for fields of structs 
        ''' that have the same assigned status as the container, and for structs that (recursively) contain no data members.
        ''' We do not need to track references to variables that occur before the variable is declared, as those are reported 
        ''' in an earlier phase as "use before declaration". That allows us to avoid giving slots to local variables before
        ''' processing their declarations.
        ''' </summary>
        Protected Function VariableSlot(symbol As Symbol, Optional containingSlot As Integer = 0) As Integer

            containingSlot = DescendThroughTupleRestFields(symbol, containingSlot, forceContainingSlotsToExist:=False)

            If symbol Is Nothing Then
                ' NOTE: This point may be hit in erroneous code, like 
                '       referencing instance members from static methods
                Return SlotKind.NotTracked
            End If

            Dim slot As Integer
            Return If((_variableSlot.TryGetValue(New VariableIdentifier(symbol, containingSlot), slot)), slot, SlotKind.NotTracked)

        End Function

        ''' <summary>
        ''' Return the slot for a variable, or SlotKind.NotTracked if it is not tracked (because, for example, it is an empty struct).
        ''' </summary>
        Protected Function MakeSlotsForExpression(node As BoundExpression) As SlotCollection
            Dim result As New SlotCollection() ' empty

            Select Case node.Kind

                Case BoundKind.MeReference, BoundKind.MyBaseReference, BoundKind.MyClassReference
                    If MeParameter IsNot Nothing Then
                        result.Append(GetOrCreateSlot(MeParameter))
                    End If

                Case BoundKind.Local
                    Dim local As LocalSymbol = DirectCast(node, BoundLocal).LocalSymbol

                    If local.DeclarationKind = LocalDeclarationKind.AmbiguousLocals Then
                        ' multiple locals
                        For Each loc In DirectCast(local, AmbiguousLocalsPseudoSymbol).Locals
                            result.Append(GetOrCreateSlot(loc))
                        Next

                    Else
                        ' just one simple local
                        result.Append(GetOrCreateSlot(local))
                    End If

                Case BoundKind.RangeVariable
                    result.Append(GetOrCreateSlot(DirectCast(node, BoundRangeVariable).RangeVariable))

                Case BoundKind.Parameter
                    result.Append(GetOrCreateSlot(DirectCast(node, BoundParameter).ParameterSymbol))

                Case BoundKind.FieldAccess
                    Dim fieldAccess = DirectCast(node, BoundFieldAccess)
                    Dim fieldsymbol = fieldAccess.FieldSymbol
                    Dim receiverOpt = fieldAccess.ReceiverOpt

                    ' do not track static fields
                    If fieldsymbol.IsShared OrElse receiverOpt Is Nothing OrElse receiverOpt.Kind = BoundKind.TypeExpression Then
                        Exit Select ' empty result
                    End If

                    ' do not track fields of non-structs
                    If receiverOpt.Type Is Nothing OrElse receiverOpt.Type.TypeKind <> TypeKind.Structure Then
                        Exit Select ' empty result
                    End If

                    result = MakeSlotsForExpression(receiverOpt)
                    ' update slots, reuse collection
                    ' NOTE: we don't filter out SlotKind.NotTracked values
                    For i = 0 To result.Count - 1
                        result(i) = GetOrCreateSlot(fieldAccess.FieldSymbol, result(0))
                    Next

                Case BoundKind.WithLValueExpressionPlaceholder, BoundKind.WithRValueExpressionPlaceholder
                    Dim substitute As BoundExpression = Me.GetPlaceholderSubstitute(DirectCast(node, BoundValuePlaceholderBase))
                    If substitute IsNot Nothing Then
                        Return MakeSlotsForExpression(substitute)
                    End If

            End Select

            Return result
        End Function

        ''' <summary>
        ''' Force a variable to have a slot.
        ''' </summary>
        ''' <param name = "symbol"></param>
        ''' <returns></returns>
        Protected Function GetOrCreateSlot(symbol As Symbol, Optional containingSlot As Integer = 0) As Integer
            containingSlot = DescendThroughTupleRestFields(symbol, containingSlot, forceContainingSlotsToExist:=True)

            If containingSlot = SlotKind.NotTracked Then
                Return SlotKind.NotTracked
            End If

            ' Check if the slot already exists
            Dim varIdentifier As New VariableIdentifier(symbol, containingSlot)

            Dim slot As Integer
            If Not _variableSlot.TryGetValue(varIdentifier, slot) Then

                If symbol.Kind = SymbolKind.Local Then
                    Me._unusedVariables.Add(DirectCast(symbol, LocalSymbol))
                End If

                Dim variableType As TypeSymbol = GetVariableType(symbol)

                If IsEmptyStructType(variableType) Then
                    Return SlotKind.NotTracked
                End If

                '  Create a slot
                slot = nextVariableSlot
                nextVariableSlot = nextVariableSlot + 1
                _variableSlot.Add(varIdentifier, slot)
                If slot >= variableBySlot.Length Then
                    Array.Resize(Me.variableBySlot, slot * 2)
                End If
                variableBySlot(slot) = varIdentifier
            End If

            Me.Normalize(Me.State)
            Return slot
        End Function

        ''' <summary>
        ''' Descends through Rest fields of a tuple if "symbol" is an extended field
        ''' As a result the "symbol" will be adjusted to be the field of the innermost tuple
        ''' and a corresponding containingSlot is returned.
        ''' Return value -1 indicates a failure which could happen for the following reasons
        ''' a) Rest field does not exist, which could happen in rare error scenarios involving broken ValueTuple types
        ''' b) Rest is not tracked already and forceSlotsToExist is false (otherwise we create slots on demand)
        ''' </summary>
        Private Function DescendThroughTupleRestFields(ByRef symbol As Symbol, containingSlot As Integer, forceContainingSlotsToExist As Boolean) As Integer
            Dim fieldSymbol = TryCast(symbol, TupleFieldSymbol)
            If fieldSymbol IsNot Nothing Then
                Dim containingType As TypeSymbol = DirectCast(symbol.ContainingType, TupleTypeSymbol).UnderlyingNamedType

                ' for tuple fields the variable identifier represents the underlying field
                symbol = fieldSymbol.TupleUnderlyingField

                ' descend through Rest fields
                ' force corresponding slots if do not exist
                While Not TypeSymbol.Equals(containingType, symbol.ContainingType, TypeCompareKind.ConsiderEverything)
                    Dim restField = TryCast(containingType.GetMembers(TupleTypeSymbol.RestFieldName).FirstOrDefault(), FieldSymbol)
                    If restField Is Nothing Then
                        Return -1
                    End If

                    If forceContainingSlotsToExist Then
                        containingSlot = GetOrCreateSlot(restField, containingSlot)
                    Else
                        If Not _variableSlot.TryGetValue(New VariableIdentifier(restField, containingSlot), containingSlot) Then
                            Return -1
                        End If
                    End If

                    containingType = restField.Type.GetTupleUnderlyingTypeOrSelf()
                End While
            End If

            Return containingSlot
        End Function

        ' In C#, we moved this cache to a separate cache held onto by the compilation, so we didn't
        ' recompute it each time.
        '
        ' This is not so simple in VB, because we'd have to move the cache for members of the 
        ' struct, and we'd need two different caches depending on _trackStructsWithIntrinsicTypedFields.
        ' So this optimization is not done for now in VB.

        Private ReadOnly _isEmptyStructType As New Dictionary(Of NamedTypeSymbol, Boolean)()

        Protected Overridable Function IsEmptyStructType(type As TypeSymbol) As Boolean
            Dim namedType = TryCast(type, NamedTypeSymbol)
            If namedType Is Nothing Then
                Return False
            End If

            If Not IsTrackableStructType(namedType) Then
                Return False
            End If

            Dim result As Boolean = False
            If _isEmptyStructType.TryGetValue(namedType, result) Then
                Return result
            End If

            ' to break cycles 
            _isEmptyStructType(namedType) = True

            For Each field In GetStructInstanceFields(namedType)
                If Not IsEmptyStructType(field.Type) Then
                    _isEmptyStructType(namedType) = False
                    Return False
                End If
            Next

            _isEmptyStructType(namedType) = True
            Return True
        End Function

        Private Shared Function IsTrackableStructType(symbol As TypeSymbol) As Boolean
            If IsNonPrimitiveValueType(symbol) Then
                Dim type = TryCast(symbol.OriginalDefinition, NamedTypeSymbol)
                Return (type IsNot Nothing) AndAlso Not type.KnownCircularStruct
            End If
            Return False
        End Function

        ''' <summary> Calculates the flag of being already reported; for structure types
        ''' the slot may be reported if ALL the children are reported </summary>
        Private Function IsSlotAlreadyReported(symbolType As TypeSymbol, slot As Integer) As Boolean
            If _alreadyReported(slot) Then
                Return True
            End If

            '  not applicable for 'special slots'
            If slot <= SlotKind.FunctionValue Then
                Return False
            End If

            If Not IsTrackableStructType(symbolType) Then
                Return False
            End If

            '  For structures - check field slots
            For Each field In GetStructInstanceFields(symbolType)

                Dim childSlot = VariableSlot(field, slot)
                If childSlot <> SlotKind.NotTracked Then

                    If Not IsSlotAlreadyReported(field.Type, childSlot) Then
                        Return False
                    End If

                Else
                    Return False ' slot not created yet 
                End If

            Next

            ' Seems to be assigned through children
            _alreadyReported(slot) = True
            Return True
        End Function

        ''' <summary> Marks slot as reported, propagates 'reported' flag to the children if necessary </summary>
        Private Sub MarkSlotAsReported(symbolType As TypeSymbol, slot As Integer)
            If _alreadyReported(slot) Then
                Return
            End If

            _alreadyReported(slot) = True

            '  not applicable for 'special slots'
            If slot <= SlotKind.FunctionValue Then
                Return
            End If

            If Not IsTrackableStructType(symbolType) Then
                Return
            End If

            '  For structures - propagate to field slots
            For Each field In GetStructInstanceFields(symbolType)

                Dim childSlot = VariableSlot(field, slot)
                If childSlot <> SlotKind.NotTracked Then
                    MarkSlotAsReported(field.Type, childSlot)
                End If

            Next
        End Sub

        ''' <summary> Unassign a slot for a regular variable </summary>
        Private Sub SetSlotUnassigned(slot As Integer)
            If slot >= Me.State.Assigned.Capacity Then
                Normalize(Me.State)
            End If

            If Me._tryState.HasValue Then
                Dim tryState = Me._tryState.Value
                SetSlotUnassigned(slot, tryState)
                Me._tryState = tryState
            End If

            SetSlotUnassigned(slot, Me.State)
        End Sub

        Private Sub SetSlotUnassigned(slot As Integer, ByRef state As LocalState)
            Dim id As VariableIdentifier = variableBySlot(slot)
            Dim type As TypeSymbol = GetVariableType(id.Symbol)
            Debug.Assert(Not IsEmptyStructType(type))

            ' If the state of the slot is 'assigned' we need to clear it 
            '    and possibly propagate it to all the parents
            state.Unassign(slot)

            'unassign struct children (if possible)
            If IsTrackableStructType(type) Then
                For Each field In GetStructInstanceFields(type)
                    ' get child's slot

                    ' NOTE: we don't need to care about possible cycles here because we took care of it 
                    '       in MakeSlotImpl when we created slots; we will just get SlotKind.NotTracked in worst case
                    Dim childSlot = VariableSlot(field, slot)

                    If childSlot >= SlotKind.FirstAvailable Then
                        SetSlotUnassigned(childSlot, state)
                    End If
                Next
            End If

            '  propagate to parents
            While id.ContainingSlot > 0
                '  check the parent
                Dim parentSlot = id.ContainingSlot
                Dim parentIdentifier = variableBySlot(parentSlot)
                Dim parentSymbol As Symbol = parentIdentifier.Symbol

                If Not state.IsAssigned(parentSlot) Then
                    Exit While
                End If

                '  unassign and continue loop
                state.Unassign(parentSlot)

                If parentSymbol.Kind = SymbolKind.Local AndAlso DirectCast(parentSymbol, LocalSymbol).IsFunctionValue Then
                    state.Unassign(SlotKind.FunctionValue)
                End If

                id = parentIdentifier
            End While
        End Sub

        ''' <summary>Assign a slot for a regular variable in a given state.</summary>
        Private Sub SetSlotAssigned(slot As Integer, ByRef state As LocalState)
            Dim id As VariableIdentifier = variableBySlot(slot)

            Dim type As TypeSymbol = GetVariableType(id.Symbol)
            Debug.Assert(Not IsEmptyStructType(type))

            If slot >= Me.State.Assigned.Capacity Then
                Normalize(Me.State)
            End If

            If Not state.IsAssigned(slot) Then
                '  assign this slot
                state.Assign(slot)

                If IsTrackableStructType(type) Then

                    '  propagate to children
                    For Each field In GetStructInstanceFields(type)
                        ' get child's slot

                        ' NOTE: we don't need to care about possible cycles here because we took care of it 
                        '       in MakeSlotImpl when we created slots; we will just get SlotKind.NotTracked in worst case
                        Dim childSlot = VariableSlot(field, slot)

                        If childSlot >= SlotKind.FirstAvailable Then
                            SetSlotAssigned(childSlot, state)
                        End If
                    Next

                End If

                ' propagate to parents
                While id.ContainingSlot > 0
                    ' check if all parent's children are set
                    Dim parentSlot = id.ContainingSlot
                    Dim parentIdentifier = variableBySlot(parentSlot)
                    Dim parentSymbol As Symbol = parentIdentifier.Symbol
                    Dim parentStructType = GetVariableType(parentSymbol)

                    '  exit while if there is at least one child with a slot which is not assigned
                    For Each childSymbol In GetStructInstanceFields(parentStructType)
                        Dim childSlot = GetOrCreateSlot(childSymbol, parentSlot)
                        If childSlot <> SlotKind.NotTracked AndAlso Not state.IsAssigned(childSlot) Then
                            Exit While
                        End If
                    Next

                    '  otherwise the parent can be set to assigned
                    state.Assign(parentSlot)
                    If parentSymbol.Kind = SymbolKind.Local AndAlso DirectCast(parentSymbol, LocalSymbol).IsFunctionValue Then
                        state.Assign(SlotKind.FunctionValue)
                    End If
                    id = parentIdentifier
                End While

            End If
        End Sub

        ''' <summary>Assign a slot for a regular variable.</summary>
        Private Sub SetSlotAssigned(slot As Integer)
            SetSlotAssigned(slot, Me.State)
        End Sub

        ''' <summary> Hash structure fields as we may query them many times </summary>
        Private _typeToMembersCache As Dictionary(Of TypeSymbol, ImmutableArray(Of FieldSymbol)) = Nothing

        Private Function ShouldIgnoreStructField(field As FieldSymbol) As Boolean
            If field.IsShared Then
                Return True
            End If

            If Me._trackStructsWithIntrinsicTypedFields Then
                ' If the flag is set we do not ignore any structure instance fields 
                Return False
            End If

            Dim fieldType As TypeSymbol = field.Type

            ' otherwise of fields of intrinsic type are ignored
            If fieldType.IsIntrinsicValueType() Then
                Return True
            End If

            ' it the type is a type parameter and also type does NOT guarantee it is 
            ' always a reference type, we ignore the field following Dev11 implementation
            If fieldType.IsTypeParameter() Then
                Dim typeParam = DirectCast(fieldType, TypeParameterSymbol)
                If Not typeParam.IsReferenceType Then
                    Return True
                End If
            End If

            ' We also do NOT ignore all non-private fields
            If field.DeclaredAccessibility <> Accessibility.Private Then
                Return False
            End If

            ' Do NOT ignore private fields from source
            ' NOTE: We should do this for all fields, but dev11 ignores private fields from
            ' metadata, so we need to as well to avoid breaking people.  That's why we're
            ' distinguishing between source and metadata, rather than between the current 
            ' compilation and another compilation.
            If field.Dangerous_IsFromSomeCompilationIncludingRetargeting Then
                ' NOTE: Dev11 ignores fields auto-generated for events
                Dim eventOrProperty As Symbol = field.AssociatedSymbol
                If eventOrProperty Is Nothing OrElse eventOrProperty.Kind <> SymbolKind.Event Then
                    Return False
                End If
            End If

            '' If the field is private we don't ignore it if it is a field of a generic struct
            If field.ContainingType.IsGenericType Then
                Return False
            End If

            Return True
        End Function

        Private Function GetStructInstanceFields(type As TypeSymbol) As ImmutableArray(Of FieldSymbol)
            Dim result As ImmutableArray(Of FieldSymbol) = Nothing
            If _typeToMembersCache Is Nothing OrElse Not _typeToMembersCache.TryGetValue(type, result) Then

                ' load and add to cache
                Dim builder = ArrayBuilder(Of FieldSymbol).GetInstance()

                For Each member In type.GetMembersUnordered()
                    ' only fields
                    If member.Kind = SymbolKind.Field Then
                        Dim field = DirectCast(member, FieldSymbol)

                        ' Do not report virtual tuple fields.
                        ' They are additional aliases to the fields of the underlying struct or nested extensions.
                        ' and as such are already accounted for via the nonvirtual fields.
                        If field.IsVirtualTupleField Then
                            Continue For
                        End If

                        ' only instance fields
                        If Not ShouldIgnoreStructField(field) Then
                            ' NOTE: DO NOT skip fields of intrinsic types if _trackStructsWithIntrinsicTypedFields is True
                            builder.Add(field)
                        End If
                    End If
                Next

                result = builder.ToImmutableAndFree()

                If _typeToMembersCache Is Nothing Then
                    _typeToMembersCache = New Dictionary(Of TypeSymbol, ImmutableArray(Of FieldSymbol))
                End If
                _typeToMembersCache.Add(type, result)
            End If

            Return result
        End Function

        Private Shared Function GetVariableType(symbol As Symbol) As TypeSymbol
            Select Case symbol.Kind

                Case SymbolKind.Local
                    Return DirectCast(symbol, LocalSymbol).Type

                Case SymbolKind.RangeVariable
                    Return DirectCast(symbol, RangeVariableSymbol).Type

                Case SymbolKind.Field
                    Return DirectCast(symbol, FieldSymbol).Type

                Case SymbolKind.Parameter
                    Return DirectCast(symbol, ParameterSymbol).Type

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(symbol.Kind)

            End Select
        End Function

        Protected Sub SetSlotState(slot As Integer, assigned As Boolean)
            Debug.Assert(slot <> SlotKind.Unreachable)

            If slot = SlotKind.NotTracked Then
                Return

            ElseIf slot = SlotKind.FunctionValue Then
                '  Function value slot is treated in a special way. For example it does not have Symbol assigned
                '  Just do a simple assign/unassign
                If assigned Then
                    Me.State.Assign(slot)
                Else
                    Me.State.Unassign(slot)
                End If

            Else

                ' The rest should be a regular slot
                If assigned Then
                    SetSlotAssigned(slot)
                Else
                    SetSlotUnassigned(slot)
                End If

            End If

        End Sub

#End Region

#Region "Assignments: Check and Report"

        ''' <summary>
        ''' Check that the given variable is definitely assigned.  If not, produce an error.
        ''' </summary>
        Protected Sub CheckAssigned(symbol As Symbol, node As SyntaxNode, Optional rwContext As ReadWriteContext = ReadWriteContext.None)
            If symbol IsNot Nothing Then
                Dim local = TryCast(symbol, LocalSymbol)
                If local IsNot Nothing AndAlso local.IsCompilerGenerated AndAlso Not Me.ProcessCompilerGeneratedLocals Then
                    ' Do not process compiler generated temporary locals
                    Return
                End If

                ' 'local' can be a pseudo-local representing a set of real locals
                If local IsNot Nothing AndAlso local.DeclarationKind = LocalDeclarationKind.AmbiguousLocals Then
                    Dim ambiguous = DirectCast(local, AmbiguousLocalsPseudoSymbol)

                    ' Ambiguous implicit receiver is always considered to be assigned
                    VisitAmbiguousLocalSymbol(ambiguous)

                    ' Note reads of locals
                    For Each subLocal In ambiguous.Locals
                        NoteRead(subLocal)
                    Next

                Else
                    Dim slot As Integer = GetOrCreateSlot(symbol)
                    If slot >= Me.State.Assigned.Capacity Then
                        Normalize(Me.State)
                    End If

                    If slot >= SlotKind.FirstAvailable AndAlso Me.State.Reachable AndAlso Not Me.State.IsAssigned(slot) Then
                        ReportUnassigned(symbol, node, rwContext)
                    End If

                    NoteRead(symbol)
                End If
            End If
        End Sub

        ''' <summary> Version of CheckAssigned for bound field access </summary>
        Private Sub CheckAssigned(fieldAccess As BoundFieldAccess, node As SyntaxNode, Optional rwContext As ReadWriteContext = ReadWriteContext.None)
            Dim unassignedSlot As Integer

            If Me.State.Reachable AndAlso Not IsAssigned(fieldAccess, unassignedSlot) Then
                ReportUnassigned(fieldAccess.FieldSymbol, node, rwContext, unassignedSlot, fieldAccess)
            End If

            NoteRead(fieldAccess)
        End Sub

        Protected Overridable Sub VisitAmbiguousLocalSymbol(ambiguous As AmbiguousLocalsPseudoSymbol)
        End Sub

        Protected Overrides Sub VisitLvalue(node As BoundExpression, Optional dontLeaveRegion As Boolean = False)
            MyBase.VisitLvalue(node, True) ' Don't leave region

            If node.Kind = BoundKind.Local Then
                Dim symbol As LocalSymbol = DirectCast(node, BoundLocal).LocalSymbol
                If symbol.DeclarationKind = LocalDeclarationKind.AmbiguousLocals Then
                    VisitAmbiguousLocalSymbol(DirectCast(symbol, AmbiguousLocalsPseudoSymbol))
                End If
            End If

            ' NOTE: we can skip checking if Me._firstInRegion is nothing because 'node' is not nothing
            If Not dontLeaveRegion AndAlso node Is Me._lastInRegion AndAlso IsInside Then
                Me.LeaveRegion()
            End If
        End Sub

        ''' <summary> Check node for being assigned, return the value of unassigned slot in unassignedSlot </summary>
        Private Function IsAssigned(node As BoundExpression, ByRef unassignedSlot As Integer) As Boolean
            unassignedSlot = SlotKind.NotTracked

            If IsEmptyStructType(node.Type) Then
                Return True
            End If

            Select Case node.Kind

                Case BoundKind.MeReference
                    unassignedSlot = VariableSlot(MeParameter)

                Case BoundKind.Local
                    Dim local As LocalSymbol = DirectCast(node, BoundLocal).LocalSymbol

                    If local.IsCompilerGenerated AndAlso Not Me.ProcessCompilerGeneratedLocals Then
                        ' For consistency with Assign behavior, which does not process compiler generated temporary locals.
                        Return True
                    End If

                    If local.DeclarationKind <> LocalDeclarationKind.AmbiguousLocals Then
                        unassignedSlot = VariableSlot(local)

                    Else
                        ' We never report ambiguous locals pseudo-symbol as unassigned, 
                        unassignedSlot = SlotKind.NotTracked
                        VisitAmbiguousLocalSymbol(DirectCast(local, AmbiguousLocalsPseudoSymbol))
                    End If

                Case BoundKind.RangeVariable
                    ' range variables are always assigned
                    unassignedSlot = -1
                    Return True

                Case BoundKind.Parameter
                    unassignedSlot = VariableSlot(DirectCast(node, BoundParameter).ParameterSymbol)

                Case BoundKind.FieldAccess
                    Dim fieldAccess = DirectCast(node, BoundFieldAccess)
                    If Not FieldAccessMayRequireTracking(fieldAccess) OrElse IsAssigned(fieldAccess.ReceiverOpt, unassignedSlot) Then
                        Return True
                    End If
                    ' NOTE: unassignedSlot must have been set by previous call to IsAssigned(...)
                    unassignedSlot = GetOrCreateSlot(fieldAccess.FieldSymbol, unassignedSlot)

                Case BoundKind.WithLValueExpressionPlaceholder, BoundKind.WithRValueExpressionPlaceholder
                    Dim substitute As BoundExpression = Me.GetPlaceholderSubstitute(DirectCast(node, BoundValuePlaceholderBase))
                    If substitute IsNot Nothing Then
                        Return IsAssigned(substitute, unassignedSlot)
                    End If

                    unassignedSlot = SlotKind.NotTracked

                Case Else
                    ' The value is a method call return value or something else we can assume is assigned.
                    unassignedSlot = SlotKind.NotTracked

            End Select

            Return Me.State.IsAssigned(unassignedSlot)
        End Function

        ''' <summary>
        ''' Property controls Roslyn data flow analysis features which are disabled in command-line 
        ''' compiler mode to maintain backward compatibility (mostly diagnostics not reported by Dev11), 
        ''' but *enabled* in flow analysis API
        ''' </summary>
        Protected Overridable ReadOnly Property EnableBreakingFlowAnalysisFeatures As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Specifies if the analysis should process compiler generated locals. 
        ''' 
        ''' Note that data flow API should never report compiler generated variables 
        ''' as well as those should not generate any diagnostics (like being unassigned, etc...).
        ''' 
        ''' But when the analysis is used for iterators or async captures it should process 
        ''' compiler generated locals as well...
        ''' </summary>
        Protected Overridable ReadOnly Property ProcessCompilerGeneratedLocals As Boolean
            Get
                Return False
            End Get
        End Property

        Private Function GetUnassignedSymbolFirstLocation(sym As Symbol, boundFieldAccess As BoundFieldAccess) As Location
            Select Case sym.Kind
                Case SymbolKind.Parameter
                    Return Nothing

                Case SymbolKind.RangeVariable
                    Return Nothing

                Case SymbolKind.Local
                    Dim locations As ImmutableArray(Of Location) = sym.Locations
                    Return If(locations.IsEmpty, Nothing, locations(0))

                Case SymbolKind.Field
                    Debug.Assert(boundFieldAccess IsNot Nothing)
                    If sym.IsShared Then
                        Return Nothing
                    End If

                    Dim receiver As BoundExpression = boundFieldAccess.ReceiverOpt
                    Debug.Assert(receiver IsNot Nothing)

                    Select Case receiver.Kind
                        Case BoundKind.Local
                            Return GetUnassignedSymbolFirstLocation(DirectCast(receiver, BoundLocal).LocalSymbol, Nothing)

                        Case BoundKind.FieldAccess
                            Dim fieldAccess = DirectCast(receiver, BoundFieldAccess)
                            Return GetUnassignedSymbolFirstLocation(fieldAccess.FieldSymbol, fieldAccess)

                        Case Else
                            Return Nothing
                    End Select

                Case Else
                    Debug.Assert(False, "Why is this reachable?")
                    Return Nothing
            End Select
        End Function

        ''' <summary>
        ''' Report a given variable as not definitely assigned.  Once a variable has been so
        ''' reported, we suppress further reports of that variable.
        ''' </summary>
        Protected Overridable Sub ReportUnassigned(sym As Symbol,
                                                   node As SyntaxNode,
                                                   rwContext As ReadWriteContext,
                                                   Optional slot As Integer = SlotKind.NotTracked,
                                                   Optional boundFieldAccess As BoundFieldAccess = Nothing)

            If slot < SlotKind.FirstAvailable Then
                slot = VariableSlot(sym)
            End If

            If slot >= Me._alreadyReported.Capacity Then
                _alreadyReported.EnsureCapacity(Me.nextVariableSlot)
            End If

            If sym.Kind = SymbolKind.Parameter Then
                ' Because there are no Out parameters in VB, general workflow should not hit this point;
                '   but in region analysis parameters are being unassigned, so it is reachable 
                Return
            End If

            If sym.Kind = SymbolKind.RangeVariable Then
                ' Range variables are always assigned for the purpose of error reporting.
                Return
            End If

            Debug.Assert(sym.Kind = SymbolKind.Local OrElse sym.Kind = SymbolKind.Field)

            Dim localOrFieldType As TypeSymbol
            Dim isFunctionValue As Boolean
            Dim isStaticLocal As Boolean
            Dim isImplicitlyDeclared As Boolean = False

            If sym.Kind = SymbolKind.Local Then
                Dim locSym = DirectCast(sym, LocalSymbol)
                localOrFieldType = locSym.Type
                isFunctionValue = locSym.IsFunctionValue AndAlso EnableBreakingFlowAnalysisFeatures
                isStaticLocal = locSym.IsStatic
                isImplicitlyDeclared = locSym.IsImplicitlyDeclared
            Else
                isFunctionValue = False
                isStaticLocal = False
                localOrFieldType = DirectCast(sym, FieldSymbol).Type
            End If

            If Not IsSlotAlreadyReported(localOrFieldType, slot) Then

                Dim warning As ERRID = Nothing

                ' NOTE: When a variable is being used before declaration VB generates ERR_UseOfLocalBeforeDeclaration1
                ' NOTE: error, thus we don't want to generate redundant warning; we base such an analysis on node
                ' NOTE: and symbol locations
                Dim firstLocation As Location = GetUnassignedSymbolFirstLocation(sym, boundFieldAccess)
                If isImplicitlyDeclared OrElse firstLocation Is Nothing OrElse firstLocation.SourceSpan.Start < node.SpanStart Then

                    ' Because VB does not support out parameters, only locals OR fields of local structures can be unassigned.
                    If localOrFieldType IsNot Nothing AndAlso Not isStaticLocal Then

                        If localOrFieldType.IsIntrinsicValueType OrElse localOrFieldType.IsReferenceType Then
                            If localOrFieldType.IsIntrinsicValueType AndAlso Not isFunctionValue Then
                                warning = Nothing
                            Else
                                warning = If(rwContext = ReadWriteContext.ByRefArgument, ERRID.WRN_DefAsgUseNullRefByRef, ERRID.WRN_DefAsgUseNullRef)
                            End If
                        ElseIf localOrFieldType.IsValueType Then
                            ' This is only reported for structures with reference type fields.
                            warning = If(rwContext = ReadWriteContext.ByRefArgument, ERRID.WRN_DefAsgUseNullRefByRefStr, ERRID.WRN_DefAsgUseNullRefStr)
                        Else
                            Debug.Assert(localOrFieldType.IsTypeParameter)
                        End If

                        If warning <> Nothing Then
                            Me.diagnostics.Add(warning, node.GetLocation(), If(sym.Name, "dummy"))
                        End If
                    End If

                    MarkSlotAsReported(localOrFieldType, slot)
                End If
            End If

        End Sub

        Private Sub CheckAssignedFunctionValue(local As LocalSymbol, node As SyntaxNode)
            If Not Me.State.FunctionAssignedValue AndAlso Not _seenOnErrorOrResume Then
                Dim type As TypeSymbol = local.Type
                ' NOTE: Dev11 does NOT report warning on user-defined empty value types
                ' Special case: We specifically want to give a warning if the user doesn't return from a WinRT AddHandler.
                ' NOTE: Strictly speaking, dev11 actually checks whether the return type is EventRegistrationToken (see IsWinRTEventAddHandler),
                ' but the conditions should be equivalent (i.e. return EventRegistrationToken if and only if WinRT).
                If EnableBreakingFlowAnalysisFeatures OrElse Not type.IsValueType OrElse type.IsIntrinsicOrEnumType OrElse Not IsEmptyStructType(type) OrElse
                    (Me.MethodSymbol.MethodKind = MethodKind.EventAdd AndAlso DirectCast(Me.MethodSymbol.AssociatedSymbol, EventSymbol).IsWindowsRuntimeEvent) Then
                    ReportUnassignedFunctionValue(local, node)
                End If
            End If
        End Sub

        Private Sub ReportUnassignedFunctionValue(local As LocalSymbol, node As SyntaxNode)
            If Not _alreadyReported(SlotKind.FunctionValue) Then

                Dim type As TypeSymbol = Nothing
                Dim method = Me.MethodSymbol

                type = method.ReturnType
                If type IsNot Nothing AndAlso
                   Not (method.IsIterator OrElse
                        (method.IsAsync AndAlso type.Equals(compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task)))) Then

                    Dim warning As ERRID = Nothing
                    Dim localName As String = GetFunctionLocalName(method, local)

                    type = type.GetEnumUnderlyingTypeOrSelf

                    ' Actual warning depends on whether the local is a function,
                    ' property, or operator value versus reference type.
                    If type.IsIntrinsicValueType Then

                        Select Case MethodSymbol.MethodKind
                            Case MethodKind.Conversion, MethodKind.UserDefinedOperator
                                warning = ERRID.WRN_DefAsgNoRetValOpVal1
                            Case MethodKind.PropertyGet
                                warning = ERRID.WRN_DefAsgNoRetValPropVal1
                            Case Else
                                Debug.Assert(MethodSymbol.MethodKind = MethodKind.Ordinary OrElse MethodSymbol.MethodKind = MethodKind.LambdaMethod)
                                warning = ERRID.WRN_DefAsgNoRetValFuncVal1
                        End Select

                    ElseIf type.IsReferenceType Then

                        Select Case MethodSymbol.MethodKind
                            Case MethodKind.Conversion, MethodKind.UserDefinedOperator
                                warning = ERRID.WRN_DefAsgNoRetValOpRef1
                            Case MethodKind.PropertyGet
                                warning = ERRID.WRN_DefAsgNoRetValPropRef1
                            Case Else
                                Debug.Assert(MethodSymbol.MethodKind = MethodKind.Ordinary OrElse MethodSymbol.MethodKind = MethodKind.LambdaMethod)
                                warning = ERRID.WRN_DefAsgNoRetValFuncRef1
                        End Select

                    ElseIf type.TypeKind = TypeKind.TypeParameter Then
                        ' IsReferenceType was false, so this type parameter was not known to be a reference type.
                        ' Following past practice, no warning is given in this case.

                    Else
                        Debug.Assert(type.IsValueType)
                        Select Case MethodSymbol.MethodKind
                            Case MethodKind.Conversion, MethodKind.UserDefinedOperator
                                ' no warning is given in this case.
                            Case MethodKind.PropertyGet
                                warning = ERRID.WRN_DefAsgNoRetValPropRef1
                            Case MethodKind.EventAdd
                                ' In Dev11, there wasn't time to change the syntax of AddHandler to allow the user
                                ' to specify a return type (necessarily, EventRegistrationToken) for WinRT events.
                                ' DevDiv #376690 reflects the fact that this leads to an incredibly confusing user
                                ' experience if nothing is return: no diagnostics are reported, but RemoveHandler
                                ' statements will silently fail.  To prompt the user, (1) warn if there is no
                                ' explicit return, and (2) modify the error message to say "AddHandler As 
                                ' EventRegistrationToken" to hint at the nature of the problem.
                                ' Update: Dev11 just mangles an existing error message, but we'll introduce a new, 
                                ' clearer, one.

                                warning = ERRID.WRN_DefAsgNoRetValWinRtEventVal1
                                localName = MethodSymbol.AssociatedSymbol.Name
                            Case Else
                                warning = ERRID.WRN_DefAsgNoRetValFuncRef1
                        End Select
                    End If

                    If warning <> Nothing Then
                        Debug.Assert(localName IsNot Nothing)
                        Me.diagnostics.Add(warning, node.GetLocation(), localName)
                    End If

                End If

                _alreadyReported(SlotKind.FunctionValue) = True
            End If
        End Sub

        Private Shared Function GetFunctionLocalName(method As MethodSymbol, local As LocalSymbol) As String
            Select Case method.MethodKind
                Case MethodKind.LambdaMethod
                    Return StringConstants.AnonymousMethodName

                Case MethodKind.Conversion, MethodKind.UserDefinedOperator
                    ' The operator's function local is op_<something> and not
                    ' VB's operator name, so we need to take the identifier token
                    ' directly
                    Dim operatorBlock = DirectCast(DirectCast(local.ContainingSymbol, SourceMemberMethodSymbol).BlockSyntax, OperatorBlockSyntax)
                    Return operatorBlock.OperatorStatement.OperatorToken.Text

                Case Else
                    Return If(local.Name, method.Name)
            End Select
        End Function

        ''' <summary>
        ''' Mark a variable as assigned (or unassigned).
        ''' </summary>
        Protected Overridable Sub Assign(node As BoundNode, value As BoundExpression, Optional assigned As Boolean = True)
            Select Case node.Kind
                Case BoundKind.LocalDeclaration
                    Dim local = DirectCast(node, BoundLocalDeclaration)
                    Debug.Assert(local.InitializerOpt Is value OrElse local.InitializedByAsNew)
                    Dim symbol = local.LocalSymbol
                    Dim slot As Integer = GetOrCreateSlot(symbol)
                    Dim written As Boolean = assigned OrElse Not Me.State.Reachable
                    SetSlotState(slot, written)
                    ' Note write if the local has an initializer or if it is part of an as-new.
                    If assigned AndAlso (value IsNot Nothing OrElse local.InitializedByAsNew) Then NoteWrite(symbol, value)

                Case BoundKind.ForToStatement,
                    BoundKind.ForEachStatement
                    Dim forStatement = DirectCast(node, BoundForStatement)
                    Dim symbol = forStatement.DeclaredOrInferredLocalOpt
                    Debug.Assert(symbol IsNot Nothing)
                    Dim slot As Integer = GetOrCreateSlot(symbol)
                    Dim written As Boolean = assigned OrElse Not Me.State.Reachable
                    SetSlotState(slot, written)

                Case BoundKind.Local
                    Dim local = DirectCast(node, BoundLocal)
                    Dim symbol = local.LocalSymbol
                    If symbol.IsCompilerGenerated AndAlso Not Me.ProcessCompilerGeneratedLocals Then
                        ' Do not process compiler generated temporary locals.
                        Return
                    End If

                    ' Regular variable
                    Dim slot As Integer = GetOrCreateSlot(symbol)
                    SetSlotState(slot, assigned)
                    If symbol.IsFunctionValue Then
                        SetSlotState(SlotKind.FunctionValue, assigned)
                    End If
                    If assigned Then
                        NoteWrite(symbol, value)
                    End If

                Case BoundKind.Parameter
                    Dim local = DirectCast(node, BoundParameter)
                    Dim symbol = local.ParameterSymbol
                    Dim slot As Integer = GetOrCreateSlot(symbol)
                    SetSlotState(slot, assigned)
                    If assigned Then NoteWrite(symbol, value)

                Case BoundKind.MeReference
                    ' var local = node as BoundThisReference;
                    Dim slot As Integer = GetOrCreateSlot(MeParameter)
                    SetSlotState(slot, assigned)
                    If assigned Then NoteWrite(MeParameter, value)

                Case BoundKind.FieldAccess, BoundKind.PropertyAccess
                    Dim expression = DirectCast(node, BoundExpression)
                    Dim slots As SlotCollection = MakeSlotsForExpression(expression)
                    For i = 0 To slots.Count - 1
                        SetSlotState(slots(i), assigned)
                    Next
                    slots.Free()
                    If assigned Then NoteWrite(expression, value)

                Case BoundKind.WithLValueExpressionPlaceholder, BoundKind.WithRValueExpressionPlaceholder
                    Dim substitute As BoundExpression = Me.GetPlaceholderSubstitute(DirectCast(node, BoundValuePlaceholderBase))
                    If substitute IsNot Nothing Then
                        Assign(substitute, value, assigned)
                    End If

                Case BoundKind.ByRefArgumentWithCopyBack
                    Assign(DirectCast(node, BoundByRefArgumentWithCopyBack).OriginalArgument, value, assigned)

                Case Else
                    ' Other kinds of left-hand-sides either represent things not tracked (e.g. array elements)
                    ' or errors that have been reported earlier (e.g. assignment to a unary increment)
            End Select
        End Sub

#End Region

#Region "Try statements"

        ' When assignments happen inside a region, they are treated as UN-assignments.
        ' Generally this is enough.
        ' 
        ' The tricky part is that any instruction in Try/Catch/Finally must be considered as potentially throwing.
        ' Even if a subsequent write outside of the region may kill an assignment inside the region, it cannot prevent escape of 
        ' the previous assignment.
        ' 
        ' === Example:
        ' 
        ' Dim x as integer  = 0
        ' Try
        '   [| region starts here
        '   x = 1
        '   Blah()              ' <- this statement may throw. (any instruction can).
        '   |] region ends here
        '   x = 2               ' <- it may seem that this assignment kills one above, where in fact "x = 1" can easily escape.
        ' Finally
        '   SomeUse(x)          ' we can see side effects of "x = 1" here
        ' End Try
        '
        ' As a result, when dealing with exception handling flows we need to maintain a separate state
        ' that collects UN-assignments only and is not affected by subsequent assignments.
        Private _tryState As OptionalState

        Protected Overrides Sub VisitTryBlock(tryBlock As BoundStatement, node As BoundTryStatement, ByRef _tryState As LocalState)
            If Me.TrackUnassignments Then

                ' store original try state
                Dim oldTryState As OptionalState = Me._tryState

                ' start with AllBitsSet (means there were no assignments)
                Me._tryState = AllBitsSet()
                ' visit try block. Any assignment inside the region will UN-assign corresponding bit in the tryState.
                MyBase.VisitTryBlock(tryBlock, node, _tryState)

                ' merge resulting tryState into tryState that we were given.
                Me.IntersectWith(_tryState, Me._tryState.Value)
                ' restore and merge old state with new changes.
                If oldTryState.HasValue Then
                    Dim tryState = Me._tryState.Value
                    Me.IntersectWith(tryState, oldTryState.Value)
                    Me._tryState = tryState
                Else
                    Me._tryState = oldTryState
                End If
            Else
                MyBase.VisitTryBlock(tryBlock, node, _tryState)
            End If
        End Sub

        Protected Overrides Sub VisitCatchBlock(catchBlock As BoundCatchBlock, ByRef finallyState As LocalState)
            If Me.TrackUnassignments Then

                Dim oldTryState As OptionalState = Me._tryState

                Me._tryState = AllBitsSet()
                Me.VisitCatchBlockInternal(catchBlock, finallyState)
                Me.IntersectWith(finallyState, Me._tryState.Value)

                If oldTryState.HasValue Then
                    Dim tryState = Me._tryState.Value
                    Me.IntersectWith(tryState, oldTryState.Value)
                    Me._tryState = tryState
                Else
                    Me._tryState = oldTryState
                End If
            Else
                Me.VisitCatchBlockInternal(catchBlock, finallyState)
            End If
        End Sub

        Private Sub VisitCatchBlockInternal(catchBlock As BoundCatchBlock, ByRef finallyState As LocalState)
            Dim local = catchBlock.LocalOpt
            If local IsNot Nothing Then
                GetOrCreateSlot(local)
            End If

            Dim exceptionSource = catchBlock.ExceptionSourceOpt
            If exceptionSource IsNot Nothing Then
                Assign(exceptionSource, value:=Nothing, assigned:=True)
            End If

            MyBase.VisitCatchBlock(catchBlock, finallyState)
        End Sub

        Protected Overrides Sub VisitFinallyBlock(finallyBlock As BoundStatement, ByRef unsetInFinally As LocalState)
            If Me.TrackUnassignments Then
                Dim oldTryState As OptionalState = Me._tryState

                Me._tryState = AllBitsSet()
                MyBase.VisitFinallyBlock(finallyBlock, unsetInFinally)
                Me.IntersectWith(unsetInFinally, Me._tryState.Value)

                If oldTryState.HasValue Then
                    Dim tryState = Me._tryState.Value
                    Me.IntersectWith(tryState, oldTryState.Value)
                    Me._tryState = tryState
                Else
                    Me._tryState = oldTryState
                End If
            Else
                MyBase.VisitFinallyBlock(finallyBlock, unsetInFinally)
            End If
        End Sub

#End Region

        Protected Overrides Function ReachableState() As LocalState
            Return New LocalState(BitVector.Empty)
        End Function

        Private Sub EnterParameters(parameters As ImmutableArray(Of ParameterSymbol))
            For Each parameter In parameters
                EnterParameter(parameter)
            Next
        End Sub

        Protected Overridable Sub EnterParameter(parameter As ParameterSymbol)
            ' this code has no effect except in the region analysis APIs, which assign
            ' variable slots to all parameters.
            Dim slot As Integer = VariableSlot(parameter)
            If slot >= SlotKind.FirstAvailable Then
                Me.State.Assign(slot)
            End If
            NoteWrite(parameter, Nothing)
        End Sub

        Private Sub LeaveParameters(parameters As ImmutableArray(Of ParameterSymbol))
            If Me.State.Reachable Then
                For Each parameter In parameters
                    LeaveParameter(parameter)
                Next
            End If
        End Sub

        Private Sub LeaveParameter(parameter As ParameterSymbol)
            If parameter.IsByRef Then
                Dim slot = VariableSlot(parameter)
                If Not Me.State.IsAssigned(slot) Then
                    ReportUnassignedByRefParameter(parameter)
                End If
                NoteRead(parameter)
            End If
        End Sub

        Protected Overrides Function UnreachableState() As LocalState
            '  at this point we don't know what size of the bitarray to use, so we only set 
            '  'reachable' flag which mean 'all bits are set' in intersect/union
            Return New LocalState(UnreachableBitsSet)
        End Function

        Protected Overrides Function AllBitsSet() As LocalState
            Dim result = New LocalState(BitVector.AllSet(nextVariableSlot))
            result.Unassign(SlotKind.Unreachable)
            Return result
        End Function

        Protected Overrides Sub VisitLocalInReadWriteContext(node As BoundLocal, rwContext As ReadWriteContext)
            MyBase.VisitLocalInReadWriteContext(node, rwContext)
            CheckAssigned(node.LocalSymbol, node.Syntax, rwContext)
        End Sub

        Public Overrides Function VisitLocal(node As BoundLocal) As BoundNode
            CheckAssigned(node.LocalSymbol, node.Syntax, ReadWriteContext.None)
            Return Nothing
        End Function

        Public Overrides Function VisitRangeVariable(node As BoundRangeVariable) As BoundNode
            ' Sometimes query expressions refer to query range variable just to 
            ' copy its value to a new compound variable. There is no reference
            ' to the range variable in code and, from user point of view, there is
            ' no access to it.
            If Not node.WasCompilerGenerated Then
                CheckAssigned(node.RangeVariable, node.Syntax, ReadWriteContext.None)
            End If

            Return Nothing
        End Function

        ' Should this local variable be considered assigned at first? It is if in the set of initially
        ' assigned variables, or else the current state is not reachable.
        Protected Function ConsiderLocalInitiallyAssigned(variable As LocalSymbol) As Boolean
            Return Not Me.State.Reachable OrElse
                   initiallyAssignedVariables IsNot Nothing AndAlso initiallyAssignedVariables.Contains(variable)
        End Function

        Public Overrides Function VisitLocalDeclaration(node As BoundLocalDeclaration) As BoundNode
            Dim placeholder As BoundValuePlaceholderBase = Nothing
            Dim variableIsUsedDirectlyAndIsAlwaysAssigned = DeclaredVariableIsAlwaysAssignedBeforeInitializer(node.Syntax.Parent, node.InitializerOpt, placeholder)

            Dim local As LocalSymbol = node.LocalSymbol

            If variableIsUsedDirectlyAndIsAlwaysAssigned Then
                SetPlaceholderSubstitute(placeholder, New BoundLocal(node.Syntax, local, local.Type))
            End If

            ' Create a slot for the declared variable to be able to track it.
            Dim slot As Integer = GetOrCreateSlot(local)

            'If initializer is a lambda, we need to treat the local as assigned within the lambda.
            Assign(node, node.InitializerOpt,
                   ConsiderLocalInitiallyAssigned(local) OrElse
                   variableIsUsedDirectlyAndIsAlwaysAssigned OrElse
                   TreatTheLocalAsAssignedWithinTheLambda(local, node.InitializerOpt))

            If node.InitializerOpt IsNot Nothing OrElse node.InitializedByAsNew Then
                ' Assign must be called after the initializer is analyzed, because the variable needs to be
                ' consider unassigned which the initializer is being analyzed.
                MyBase.VisitLocalDeclaration(node)
            End If

            AssignLocalOnDeclaration(local, node)

            If variableIsUsedDirectlyAndIsAlwaysAssigned Then
                RemovePlaceholderSubstitute(placeholder)
            End If

            Return Nothing
        End Function

        Protected Overridable Function TreatTheLocalAsAssignedWithinTheLambda(local As LocalSymbol, right As BoundExpression) As Boolean
            Return IsConvertedLambda(right)
        End Function

        Private Shared Function IsConvertedLambda(value As BoundExpression) As Boolean
            If value Is Nothing Then
                Return False
            End If

            Do
                Select Case value.Kind
                    Case BoundKind.Lambda
                        Return True

                    Case BoundKind.Conversion
                        value = DirectCast(value, BoundConversion).Operand

                    Case BoundKind.DirectCast
                        value = DirectCast(value, BoundDirectCast).Operand

                    Case BoundKind.TryCast
                        value = DirectCast(value, BoundTryCast).Operand

                    Case BoundKind.Parenthesized
                        value = DirectCast(value, BoundParenthesized).Expression

                    Case Else
                        Return False
                End Select
            Loop
        End Function

        Friend Overridable Sub AssignLocalOnDeclaration(local As LocalSymbol, node As BoundLocalDeclaration)
            If node.InitializerOpt IsNot Nothing OrElse node.InitializedByAsNew Then
                Assign(node, node.InitializerOpt)
            End If
        End Sub

        Public Overrides Function VisitBlock(node As BoundBlock) As BoundNode
            ' Implicitly declared local variables don't have LocalDeclarations nodes for them. Thus,
            ' we need to create slots for any implicitly declared local variables in this block.
            ' For flow analysis purposes, we treat an implicit declared local as equivalent to a variable declared at the 
            ' start of the method body (this block) with no initializer.

            Dim localVariables = node.Locals
            For Each local In localVariables
                If local.IsImplicitlyDeclared Then
                    SetSlotState(GetOrCreateSlot(local), ConsiderLocalInitiallyAssigned(local))
                End If
            Next

            Return MyBase.VisitBlock(node)
        End Function

        Public Overrides Function VisitLambda(node As BoundLambda) As BoundNode
            Dim oldPending As SavedPending = SavePending()
            Dim oldSymbol = Me.symbol
            Me.symbol = node.LambdaSymbol
            Dim finalState As LocalState = Me.State
            Me.SetState(If(finalState.Reachable, finalState.Clone(), Me.AllBitsSet()))
            Me.State.Assigned(SlotKind.FunctionValue) = False
            Dim save_alreadyReportedFunctionValue = _alreadyReported(SlotKind.FunctionValue)
            _alreadyReported(SlotKind.FunctionValue) = False
            EnterParameters(node.LambdaSymbol.Parameters)
            VisitBlock(node.Body)
            LeaveParameters(node.LambdaSymbol.Parameters)
            Me.symbol = oldSymbol
            _alreadyReported(SlotKind.FunctionValue) = save_alreadyReportedFunctionValue
            Me.State.Assigned(SlotKind.FunctionValue) = True
            RestorePending(oldPending)
            Me.IntersectWith(finalState, Me.State)
            Me.SetState(finalState)
            Return Nothing
        End Function

        Public Overrides Function VisitQueryLambda(node As BoundQueryLambda) As BoundNode
            Dim oldSymbol = Me.symbol
            Me.symbol = node.LambdaSymbol
            Dim finalState As LocalState = Me.State.Clone()

            VisitRvalue(node.Expression)

            Me.symbol = oldSymbol
            Me.IntersectWith(finalState, Me.State)
            Me.SetState(finalState)
            Return Nothing
        End Function

        Public Overrides Function VisitQueryExpression(node As BoundQueryExpression) As BoundNode
            VisitRvalue(node.LastOperator)
            Return Nothing
        End Function

        Public Overrides Function VisitQuerySource(node As BoundQuerySource) As BoundNode
            VisitRvalue(node.Expression)
            Return Nothing
        End Function

        Public Overrides Function VisitQueryableSource(node As BoundQueryableSource) As BoundNode
            VisitRvalue(node.Source)
            Return Nothing
        End Function

        Public Overrides Function VisitToQueryableCollectionConversion(node As BoundToQueryableCollectionConversion) As BoundNode
            VisitRvalue(node.ConversionCall)
            Return Nothing
        End Function

        Public Overrides Function VisitQueryClause(node As BoundQueryClause) As BoundNode
            VisitRvalue(node.UnderlyingExpression)
            Return Nothing
        End Function

        Public Overrides Function VisitAggregateClause(node As BoundAggregateClause) As BoundNode
            If node.CapturedGroupOpt IsNot Nothing Then
                VisitRvalue(node.CapturedGroupOpt)
            End If

            VisitRvalue(node.UnderlyingExpression)
            Return Nothing
        End Function

        Public Overrides Function VisitOrdering(node As BoundOrdering) As BoundNode
            VisitRvalue(node.UnderlyingExpression)
            Return Nothing
        End Function

        Public Overrides Function VisitRangeVariableAssignment(node As BoundRangeVariableAssignment) As BoundNode
            VisitRvalue(node.Value)
            Return Nothing
        End Function

        Public Overrides Function VisitMeReference(node As BoundMeReference) As BoundNode
            CheckAssigned(MeParameter, node.Syntax)
            Return Nothing
        End Function

        Public Overrides Function VisitParameter(node As BoundParameter) As BoundNode
            If Not node.WasCompilerGenerated Then
                CheckAssigned(node.ParameterSymbol, node.Syntax)
            End If
            Return Nothing
        End Function

        Public Overrides Function VisitFieldAccess(node As BoundFieldAccess) As BoundNode
            Dim result = MyBase.VisitFieldAccess(node)

            If FieldAccessMayRequireTracking(node) Then
                CheckAssigned(node, node.Syntax, ReadWriteContext.None)
            End If

            Return result
        End Function

        Protected Overrides Sub VisitFieldAccessInReadWriteContext(node As BoundFieldAccess, rwContext As ReadWriteContext)
            MyBase.VisitFieldAccessInReadWriteContext(node, rwContext)

            If FieldAccessMayRequireTracking(node) Then
                CheckAssigned(node, node.Syntax, rwContext)
            End If
        End Sub

        Public Overrides Function VisitAssignmentOperator(node As BoundAssignmentOperator) As BoundNode
            Dim left As BoundExpression = node.Left

            'If right expression is a lambda, we need to treat the left side as assigned within the lambda.
            Dim treatLocalAsAssigned As Boolean = False
            If left.Kind = BoundKind.Local Then
                treatLocalAsAssigned = TreatTheLocalAsAssignedWithinTheLambda(
                                            DirectCast(left, BoundLocal).LocalSymbol, node.Right)
            End If

            If treatLocalAsAssigned Then
                Assign(left, node.Right)
            End If

            MyBase.VisitAssignmentOperator(node)

            If Not treatLocalAsAssigned Then
                Assign(left, node.Right)
            End If

            Return Nothing
        End Function

        Protected Overrides ReadOnly Property SuppressRedimOperandRvalueOnPreserve As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides Function VisitRedimClause(node As BoundRedimClause) As BoundNode
            MyBase.VisitRedimClause(node)
            Assign(node.Operand, Nothing)
            Return Nothing
        End Function

        Public Overrides Function VisitReferenceAssignment(node As BoundReferenceAssignment) As BoundNode
            MyBase.VisitReferenceAssignment(node)
            Assign(node.ByRefLocal, node.LValue)
            Return Nothing
        End Function

        Protected Overrides Sub VisitForControlInitialization(node As BoundForToStatement)
            MyBase.VisitForControlInitialization(node)
            Assign(node.ControlVariable, node.InitialValue)
        End Sub

        Protected Overrides Sub VisitForControlInitialization(node As BoundForEachStatement)
            MyBase.VisitForControlInitialization(node)
            Assign(node.ControlVariable, Nothing)
        End Sub

        Protected Overrides Sub VisitForStatementVariableDeclaration(node As BoundForStatement)
            ' if the for each statement declared a variable explicitly or by using local type inference we'll
            ' need to create a new slot for the new variable that is initially unassigned.
            If node.DeclaredOrInferredLocalOpt IsNot Nothing Then
                Dim slot As Integer = GetOrCreateSlot(node.DeclaredOrInferredLocalOpt) 'not initially assigned
                Assign(node, Nothing, ConsiderLocalInitiallyAssigned(node.DeclaredOrInferredLocalOpt))
            End If
        End Sub

        Public Overrides Function VisitAnonymousTypeCreationExpression(node As BoundAnonymousTypeCreationExpression) As BoundNode
            MyBase.VisitAnonymousTypeCreationExpression(node)
            Return Nothing
        End Function

        Public Overrides Function VisitWithStatement(node As BoundWithStatement) As BoundNode
            Me.SetPlaceholderSubstitute(node.ExpressionPlaceholder, node.DraftPlaceholderSubstitute)
            MyBase.VisitWithStatement(node)
            Me.RemovePlaceholderSubstitute(node.ExpressionPlaceholder)
            Return Nothing
        End Function

        Public Overrides Function VisitUsingStatement(node As BoundUsingStatement) As BoundNode

            If node.ResourceList.IsDefaultOrEmpty Then
                ' only an expression. Let the base class' implementation deal with digging into the expression
                Return MyBase.VisitUsingStatement(node)
            End If

            ' all locals are considered initially assigned, even if the initialization expression is missing in source (error case)
            For Each variableDeclarations In node.ResourceList
                If variableDeclarations.Kind = BoundKind.AsNewLocalDeclarations Then
                    For Each variableDeclaration In DirectCast(variableDeclarations, BoundAsNewLocalDeclarations).LocalDeclarations
                        Dim local = variableDeclaration.LocalSymbol
                        Dim slot = GetOrCreateSlot(local)
                        If slot >= 0 Then
                            SetSlotAssigned(slot)
                            NoteWrite(local, Nothing)
                        Else
                            Debug.Assert(IsEmptyStructType(local.Type))
                        End If
                    Next
                Else
                    Dim local = DirectCast(variableDeclarations, BoundLocalDeclaration).LocalSymbol
                    Dim slot = GetOrCreateSlot(local)
                    If slot >= 0 Then
                        SetSlotAssigned(slot)
                        NoteWrite(local, Nothing)
                    Else
                        Debug.Assert(IsEmptyStructType(local.Type))
                    End If
                End If
            Next

            Dim result = MyBase.VisitUsingStatement(node)

            ' all locals are being read in the finally block.
            For Each variableDeclarations In node.ResourceList
                If variableDeclarations.Kind = BoundKind.AsNewLocalDeclarations Then
                    For Each variableDeclaration In DirectCast(variableDeclarations, BoundAsNewLocalDeclarations).LocalDeclarations
                        NoteRead(variableDeclaration.LocalSymbol)
                    Next
                Else
                    NoteRead(DirectCast(variableDeclarations, BoundLocalDeclaration).LocalSymbol)
                End If
            Next

            Return result
        End Function

        Protected Overridable ReadOnly Property IgnoreOutSemantics As Boolean
            Get
                Return True
            End Get
        End Property

        Protected NotOverridable Overrides Sub VisitArgument(arg As BoundExpression, p As ParameterSymbol)
            Debug.Assert(arg IsNot Nothing)
            If p.IsByRef Then
                If p.IsOut And Not Me.IgnoreOutSemantics Then
                    VisitLvalue(arg)
                Else
                    VisitRvalue(arg, rwContext:=ReadWriteContext.ByRefArgument)
                End If
            Else
                VisitRvalue(arg)
            End If
        End Sub

        Protected Overrides Sub WriteArgument(arg As BoundExpression, isOut As Boolean)
            If Not isOut Then
                CheckAssignedFromArgumentWrite(arg, arg.Syntax)
            End If

            Assign(arg, Nothing)
            MyBase.WriteArgument(arg, isOut)
        End Sub

        Protected Sub CheckAssignedFromArgumentWrite(expr As BoundExpression, node As SyntaxNode)
            If Not Me.State.Reachable Then
                Return
            End If

            Select Case expr.Kind
                Case BoundKind.Local
                    CheckAssigned(DirectCast(expr, BoundLocal).LocalSymbol, node)

                Case BoundKind.Parameter
                    CheckAssigned(DirectCast(expr, BoundParameter).ParameterSymbol, node)

                Case BoundKind.FieldAccess
                    Dim fieldAccess = DirectCast(expr, BoundFieldAccess)
                    Dim field As FieldSymbol = fieldAccess.FieldSymbol
                    If FieldAccessMayRequireTracking(fieldAccess) Then
                        CheckAssigned(fieldAccess, node, ReadWriteContext.ByRefArgument)
                    End If

                Case BoundKind.EventAccess
                    Throw ExceptionUtilities.UnexpectedValue(expr.Kind) ' TODO: is this reachable at all?

                Case BoundKind.MeReference,
                     BoundKind.MyClassReference,
                     BoundKind.MyBaseReference
                    CheckAssigned(MeParameter, node)

            End Select
        End Sub

        Protected Overrides Sub VisitLateBoundArgument(arg As BoundExpression, isByRef As Boolean)
            If isByRef Then
                VisitRvalue(arg, rwContext:=ReadWriteContext.ByRefArgument)
            Else
                VisitRvalue(arg)
            End If
        End Sub

        Public Overrides Function VisitReturnStatement(node As BoundReturnStatement) As BoundNode
            If Not node.IsEndOfMethodReturn Then
                ' We should mark it as set disregarding whether or not there is an expression; if there 
                ' is no expression, error 30654 will be generated which in Dev10 suppresses this error
                SetSlotState(SlotKind.FunctionValue, True)

            Else
                Dim functionLocal = TryCast(node.ExpressionOpt, BoundLocal)
                If functionLocal IsNot Nothing Then
                    CheckAssignedFunctionValue(functionLocal.LocalSymbol, node.Syntax)
                End If
            End If

            MyBase.VisitReturnStatement(node)
            Return Nothing
        End Function

        Public Overrides Function VisitMyBaseReference(node As BoundMyBaseReference) As BoundNode
            CheckAssigned(MeParameter, node.Syntax)
            Return Nothing
        End Function

        Public Overrides Function VisitMyClassReference(node As BoundMyClassReference) As BoundNode
            CheckAssigned(MeParameter, node.Syntax)
            Return Nothing
        End Function

        ''' <summary>
        ''' A variable declared with As New can be considered assigned before the initializer is executed in case the variable
        ''' is a value type. The reason is that in this case the initialization happens in place (not in a temporary) and 
        ''' the variable already got the object creation expression assigned.
        ''' </summary>
        Private Function DeclaredVariableIsAlwaysAssignedBeforeInitializer(syntax As SyntaxNode, boundInitializer As BoundExpression,
                                                                           <Out> ByRef placeholder As BoundValuePlaceholderBase) As Boolean
            placeholder = Nothing
            If boundInitializer IsNot Nothing AndAlso
                (boundInitializer.Kind = BoundKind.ObjectCreationExpression OrElse boundInitializer.Kind = BoundKind.NewT) Then
                Dim boundInitializerBase = DirectCast(boundInitializer, BoundObjectCreationExpressionBase).InitializerOpt

                If boundInitializerBase IsNot Nothing AndAlso boundInitializerBase.Kind = BoundKind.ObjectInitializerExpression Then
                    Dim objectInitializer = DirectCast(boundInitializerBase, BoundObjectInitializerExpression)

                    If syntax IsNot Nothing AndAlso syntax.Kind = SyntaxKind.VariableDeclarator Then
                        Dim declarator = DirectCast(syntax, VariableDeclaratorSyntax)

                        If declarator.AsClause IsNot Nothing AndAlso declarator.AsClause.Kind = SyntaxKind.AsNewClause Then
                            placeholder = objectInitializer.PlaceholderOpt
                            Return Not objectInitializer.CreateTemporaryLocalForInitialization
                        End If
                    End If
                End If
            End If

            Return False
        End Function

        Public Overrides Function VisitAsNewLocalDeclarations(node As BoundAsNewLocalDeclarations) As BoundNode
            Dim placeholder As BoundValuePlaceholderBase = Nothing
            Dim variableIsUsedDirectlyAndIsAlwaysAssigned =
                DeclaredVariableIsAlwaysAssignedBeforeInitializer(node.Syntax, node.Initializer, placeholder)

            Dim declarations As ImmutableArray(Of BoundLocalDeclaration) = node.LocalDeclarations
            If declarations.IsEmpty Then
                Return Nothing
            End If

            ' An initializer might access the declared variables in the initializer and therefore need to create a slot to 
            ' track them
            For Each localDeclaration In declarations
                GetOrCreateSlot(localDeclaration.LocalSymbol)
            Next

            ' NOTE: in case 'variableIsUsedDirectlyAndIsAlwaysAssigned' is set it must be 
            '           Dim a[, b, ...] As New Struct(...) With { ... }. 
            '
            '       In this case Roslyn (following Dev10/Dev11) consecutively calls constructors 
            '       and executes initializers for all declared variables. Thus, when the first 
            '       initializer is being executed, all other locals are not assigned yet.
            '
            '       This behavior is emulated below by assigning the first local and visiting
            '       the initializer before assigning all the other locals. We don't really need
            '       to revisit initializer after because all errors/warnings will be property 
            '       reported in the first visit.
            Dim localToUseAsSubstitute As LocalSymbol = CreateLocalSymbolForVariables(declarations)

            ' process the first local declaration once
            Dim localDecl As BoundLocalDeclaration = declarations(0)

            ' A valuetype variable declared with AsNew and initialized by an object initializer is considered
            ' assigned when visiting the initializer, because the initialization happens inplace and the
            ' variable already got the object creation expression assigned (this assignment will be created 
            ' in the local rewriter).
            Assign(localDecl, node.Initializer, ConsiderLocalInitiallyAssigned(localDecl.LocalSymbol) OrElse variableIsUsedDirectlyAndIsAlwaysAssigned)

            ' if needed, replace the placeholders with the bound local for the current declared variable.
            If variableIsUsedDirectlyAndIsAlwaysAssigned Then
                Me.SetPlaceholderSubstitute(placeholder, New BoundLocal(localDecl.Syntax, localToUseAsSubstitute, localToUseAsSubstitute.Type))
            End If

            VisitRvalue(node.Initializer)
            Visit(localDecl)  ' TODO: do we really need that?

            ' Visit all other declarations
            For index = 1 To declarations.Length - 1
                localDecl = declarations(index)

                ' A valuetype variable declared with AsNew and initialized by an object initializer is considered
                ' assigned when visiting the initializer, because the initialization happens inplace and the
                ' variable already got the object creation expression assigned (this assignment will be created 
                'in the local rewriter).
                Assign(localDecl, node.Initializer, ConsiderLocalInitiallyAssigned(localDecl.LocalSymbol) OrElse variableIsUsedDirectlyAndIsAlwaysAssigned)

                Visit(localDecl) ' TODO: do we really need that?
            Next

            ' We also need to mark all 

            If variableIsUsedDirectlyAndIsAlwaysAssigned Then
                Me.RemovePlaceholderSubstitute(placeholder)
            End If

            Return Nothing
        End Function

        Protected Overridable Function CreateLocalSymbolForVariables(declarations As ImmutableArray(Of BoundLocalDeclaration)) As LocalSymbol
            Debug.Assert(declarations.Length > 0)
            Return declarations(0).LocalSymbol
        End Function

        Protected Overrides Sub VisitObjectCreationExpressionInitializer(node As BoundObjectInitializerExpressionBase)
            Dim resetPlaceholder As Boolean = node IsNot Nothing AndAlso
                                              node.Kind = BoundKind.ObjectInitializerExpression AndAlso
                                              DirectCast(node, BoundObjectInitializerExpression).CreateTemporaryLocalForInitialization

            If resetPlaceholder Then
                Me.SetPlaceholderSubstitute(DirectCast(node, BoundObjectInitializerExpression).PlaceholderOpt, Nothing) ' Override substitute
            End If

            MyBase.VisitObjectCreationExpressionInitializer(node)

            If resetPlaceholder Then
                Me.RemovePlaceholderSubstitute(DirectCast(node, BoundObjectInitializerExpression).PlaceholderOpt)
            End If
        End Sub

        Public Overrides Function VisitOnErrorStatement(node As BoundOnErrorStatement) As BoundNode
            _seenOnErrorOrResume = True
            Return MyBase.VisitOnErrorStatement(node)
        End Function

        Public Overrides Function VisitResumeStatement(node As BoundResumeStatement) As BoundNode
            _seenOnErrorOrResume = True
            Return MyBase.VisitResumeStatement(node)
        End Function
    End Class

End Namespace
