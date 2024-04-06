' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' A walker that computes the set of local variables of an iterator 
    ''' method that must be moved to fields of the generated class.
    ''' </summary>
    Friend NotInheritable Class IteratorAndAsyncCaptureWalker
        Inherits DataFlowPass

        ' In Release builds we hoist only variables (locals And parameters) that are captured. 
        ' This set will contain such variables after the bound tree is visited.
        Private ReadOnly _variablesToHoist As OrderedSet(Of Symbol)
        Private ReadOnly _byRefLocalsInitializers As Dictionary(Of LocalSymbol, BoundExpression)

        ' Contains variables that are captured but can't be hoisted since their type can't be allocated on heap.
        ' The value is a list of all usage of each such variable.
        Private _lazyDisallowedCaptures As MultiDictionary(Of Symbol, SyntaxNode)

        Public Structure Result
            Public ReadOnly CapturedLocals As OrderedSet(Of Symbol)
            Public ReadOnly ByRefLocalsInitializers As Dictionary(Of LocalSymbol, BoundExpression)

            Friend Sub New(cl As OrderedSet(Of Symbol), initializers As Dictionary(Of LocalSymbol, BoundExpression))
                Me.CapturedLocals = cl
                Me.ByRefLocalsInitializers = initializers
            End Sub
        End Structure

        Public Sub New(info As FlowAnalysisInfo)
            MyBase.New(info, Nothing, suppressConstExpressionsSupport:=False, trackStructsWithIntrinsicTypedFields:=True, trackUnassignments:=True)

            Me._variablesToHoist = New OrderedSet(Of Symbol)()
            Me._byRefLocalsInitializers = New Dictionary(Of LocalSymbol, BoundExpression)()
        End Sub

        ' Returns deterministically ordered list of variables that ought to be hoisted.
        Public Overloads Shared Function Analyze(info As FlowAnalysisInfo, diagnostics As DiagnosticBag) As Result
            Debug.Assert(info.Symbol IsNot Nothing)
            Debug.Assert(info.Symbol.Kind = SymbolKind.Method)

            Dim walker As New IteratorAndAsyncCaptureWalker(info)

            walker._convertInsufficientExecutionStackExceptionToCancelledByStackGuardException = True

            walker.Analyze()
            Debug.Assert(Not walker.InvalidRegionDetected)

            Dim variablesToHoist = walker._variablesToHoist
            Dim allVariables = walker.variableBySlot
            Dim byRefLocalsInitializers = walker._byRefLocalsInitializers
            Dim lazyDisallowedCaptures = walker._lazyDisallowedCaptures

            walker.Free()

            If lazyDisallowedCaptures IsNot Nothing Then
                For Each variable In lazyDisallowedCaptures.Keys
                    Dim type As TypeSymbol = If(variable.Kind = SymbolKind.Local, TryCast(variable, LocalSymbol).Type, TryCast(variable, ParameterSymbol).Type)
                    For Each node In lazyDisallowedCaptures(variable)
                        ' Variable of restricted type '{0}' cannot be declared in an Async or Iterator method.
                        diagnostics.Add(ERRID.ERR_CannotLiftRestrictedTypeResumable1, node.GetLocation(), type)
                    Next
                Next
            End If

            If info.Compilation.Options.OptimizationLevel <> OptimizationLevel.Release Then
                Debug.Assert(variablesToHoist.Count = 0)

                ' In debug build we hoist all locals and parameters, except ByRef locals in iterator methods.
                ' Lifetime of ByRef locals in iterator methods never crosses statement boundaries, thus 
                ' there is no reason to hoist them and the pipeline doesn't handle this.
                Dim skipByRefLocals As Boolean = DirectCast(info.Symbol, MethodSymbol).IsIterator
                For Each v In allVariables
                    If v.Symbol IsNot Nothing AndAlso HoistInDebugBuild(v.Symbol, skipByRefLocals) Then
                        variablesToHoist.Add(v.Symbol)
                    End If
                Next
            End If

            Return New Result(variablesToHoist, byRefLocalsInitializers)
        End Function

        Private Shared Function HoistInDebugBuild(symbol As Symbol, skipByRefLocals As Boolean) As Boolean
            ' In debug build hoist all parameters that can be hoisted:
            If symbol.Kind = SymbolKind.Parameter Then
                Dim parameter = TryCast(symbol, ParameterSymbol)
                Return Not parameter.Type.IsRestrictedType()
            End If

            If symbol.Kind = SymbolKind.Local Then
                Dim local = TryCast(symbol, LocalSymbol)
                If local.IsConst Then
                    Return False
                End If

                If skipByRefLocals AndAlso local.IsByRef Then
                    Return False
                End If

                ' hoist all user-defined locals that can be hoisted
                If local.SynthesizedKind = SynthesizedLocalKind.UserDefined Then
                    Return Not local.Type.IsRestrictedType()
                End If

                ' hoist all synthesized variables that have to survive state machine suspension
                Return local.SynthesizedKind <> SynthesizedLocalKind.ConditionalBranchDiscriminator
            End If

            Return False
        End Function

        Protected Overrides Function Scan() As Boolean
            Me._variablesToHoist.Clear()
            Me._byRefLocalsInitializers.Clear()
            Me._lazyDisallowedCaptures?.Clear()

            Return MyBase.Scan()
        End Function

        Private Sub CaptureVariable(variable As Symbol, syntax As SyntaxNode)
            Dim type As TypeSymbol = If(variable.Kind = SymbolKind.Local, TryCast(variable, LocalSymbol).Type, TryCast(variable, ParameterSymbol).Type)
            If type.IsRestrictedType() Then
                ' Error has already been reported:
                If TypeOf variable Is SynthesizedLocal Then
                    Return
                End If

                If _lazyDisallowedCaptures Is Nothing Then
                    _lazyDisallowedCaptures = New MultiDictionary(Of Symbol, SyntaxNode)()
                End If

                _lazyDisallowedCaptures.Add(variable, syntax)

            ElseIf compilation.Options.OptimizationLevel = OptimizationLevel.Release Then
                ' In debug build we hoist all locals and parameters after walk:
                Me._variablesToHoist.Add(variable)
            End If

        End Sub

        Protected Overrides Sub EnterParameter(parameter As ParameterSymbol)
            ' parameters are NOT initially assigned here - if that is a problem, then
            ' the parameters must be captured.
            GetOrCreateSlot(parameter)

            ' Instead of analyzing which parameters are actually being referenced
            ' we add all of them; this might need to be revised later
            CaptureVariable(parameter, Nothing)
        End Sub

        Protected Overrides Sub ReportUnassigned(symbol As Symbol, node As SyntaxNode, rwContext As ReadWriteContext, Optional slot As Integer = -1, Optional boundFieldAccess As BoundFieldAccess = Nothing)
            If symbol.Kind = SymbolKind.Field Then
                Dim sym As Symbol = GetNodeSymbol(boundFieldAccess)

                ' Unreachable for AmbiguousLocalsPseudoSymbol: ambiguous implicit 
                ' receiver should not ever be considered unassigned
                Debug.Assert(Not TypeOf sym Is AmbiguousLocalsPseudoSymbol)

                If sym IsNot Nothing Then
                    CaptureVariable(sym, node)
                End If

            ElseIf symbol.Kind = SymbolKind.Parameter OrElse symbol.Kind = SymbolKind.Local Then
                CaptureVariable(symbol, node)
            End If
        End Sub

        Protected Overrides ReadOnly Property IgnoreOutSemantics As Boolean
            Get
                Return False
            End Get
        End Property

        Protected Overrides Function IsEmptyStructType(type As TypeSymbol) As Boolean
            Return False
        End Function

        Protected Overrides ReadOnly Property EnableBreakingFlowAnalysisFeatures As Boolean
            Get
                Return True
            End Get
        End Property

        Private Sub MarkLocalsUnassigned()
            For i = SlotKind.FirstAvailable To nextVariableSlot - 1
                Dim symbol As Symbol = variableBySlot(i).Symbol
                Select Case symbol.Kind
                    Case SymbolKind.Local
                        If Not DirectCast(symbol, LocalSymbol).IsConst Then
                            SetSlotState(i, False)
                        End If
                    Case SymbolKind.Parameter
                        SetSlotState(i, False)
                End Select
            Next
        End Sub

        Public Overrides Function VisitAwaitOperator(node As BoundAwaitOperator) As BoundNode
            MyBase.VisitAwaitOperator(node)
            MarkLocalsUnassigned()
            Return Nothing
        End Function

        Protected Overrides Sub VisitCallAfterVisitArguments(node As BoundCall)
            MyBase.VisitCallAfterVisitArguments(node)

            Dim receiverOpt As BoundExpression = node.ReceiverOpt

            If receiverOpt IsNot Nothing AndAlso Not receiverOpt.IsLValue Then
                CheckAssignedFromArgumentWrite(receiverOpt, receiverOpt.Syntax)
            End If
        End Sub

        Public Overrides Function VisitSequence(node As BoundSequence) As BoundNode
            Dim result As BoundNode = Nothing

            For Each local In node.Locals
                SetSlotState(GetOrCreateSlot(local), True)
            Next
            result = MyBase.VisitSequence(node)
            For Each local In node.Locals
                CheckAssigned(local, node.Syntax)
            Next

            Return result
        End Function

        Protected Overrides ReadOnly Property ProcessCompilerGeneratedLocals As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides Function VisitReferenceAssignment(node As BoundReferenceAssignment) As BoundNode
            Dim local As LocalSymbol = node.ByRefLocal.LocalSymbol

            Debug.Assert(Not Me._byRefLocalsInitializers.ContainsKey(local))
            Me._byRefLocalsInitializers.Add(local, node.LValue)

            Return MyBase.VisitReferenceAssignment(node)
        End Function

        Public Overrides Function VisitYieldStatement(node As BoundYieldStatement) As BoundNode
            MyBase.VisitYieldStatement(node)
            MarkLocalsUnassigned()
            Return Nothing
        End Function

        Protected Overrides Function TreatTheLocalAsAssignedWithinTheLambda(local As LocalSymbol, right As BoundExpression) As Boolean
            ' By the time this analysis is invoked, Lambda conversion 
            ' is already rewritten into an object creation
            If right.Kind = BoundKind.ObjectCreationExpression Then
                Dim objCreation = DirectCast(right, BoundObjectCreationExpression)
                If TypeOf objCreation.Type Is LambdaFrame AndAlso objCreation.Arguments.Length = 1 Then
                    Dim arg0 As BoundExpression = objCreation.Arguments(0)
                    If arg0.Kind = BoundKind.Local AndAlso DirectCast(arg0, BoundLocal).LocalSymbol Is local Then
                        Return True
                    End If
                End If
            End If
            Return False
        End Function
    End Class

End Namespace
