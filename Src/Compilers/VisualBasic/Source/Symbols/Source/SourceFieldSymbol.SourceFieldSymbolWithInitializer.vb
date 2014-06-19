Imports System.Collections.Generic
Imports System.Globalization
Imports System.Threading
Imports Roslyn.Compilers.Common
Imports System.Runtime.InteropServices

Namespace Roslyn.Compilers.VisualBasic
    Partial Friend Class SourceFieldSymbol
        ''' <summary>
        ''' A source field with an explicit initializer. In a declaration declaring multiple fields,
        ''' such as "Dim a, b, c = d", this class is used for the first field only. (Other fields in
        ''' the declaration are instances of SourceFieldSymbolSiblingInitializer.)
        ''' </summary>
        Private NotInheritable Class SourceFieldSymbolWithInitializer
            Inherits SourceFieldSymbol

            ' reference to the the initialization syntax of this field,
            ' can be an EqualsValue or AsNew syntax node
            Private ReadOnly m_equalsValueOrAsNewInitOpt As SyntaxReference

            ' a tuple consisting of the evaluated constant value and type
            Private m_constantTuple As EvaluatedConstant

            Public Sub New(container As SourceNamedTypeSymbol,
                declRef As SyntaxReference,
                syntaxRef As SyntaxReference,
                name As String,
                type As TypeSymbol,
                memberFlags As SourceMemberFlags,
                equalsValueOrAsNewInitOpt As SyntaxReference)
                MyBase.New(container, declRef, syntaxRef, name, type, memberFlags)
                m_equalsValueOrAsNewInitOpt = equalsValueOrAsNewInitOpt
            End Sub

            Friend Overrides ReadOnly Property EqualsValueOrAsNewInitOpt As SyntaxNode
                Get
                    Return If(m_equalsValueOrAsNewInitOpt IsNot Nothing, m_equalsValueOrAsNewInitOpt.GetSyntax(), Nothing)
                End Get
            End Property

            Friend Overrides Function GetConstantValue(inProgress As SymbolsInProgress(Of FieldSymbol)) As ConstantValue
                If m_constantTuple Is Nothing Then
                    Dim sourceModule = DirectCast(Me.ContainingModule, SourceModuleSymbol)
                    Dim initializer = If(Me.IsConst, m_equalsValueOrAsNewInitOpt, Nothing)

                    If initializer IsNot Nothing Then
                        Dim diagnostics = DiagnosticBag.GetInstance()
                        Dim constantTuple = ConstantValueUtils.EvaluateFieldConstant(Me, initializer, inProgress, diagnostics)
                        sourceModule.AtomicStoreReferenceAndDiagnostics(m_constantTuple, constantTuple, diagnostics, CompilationStage.Declare)
                        diagnostics.Free()
                    Else
                        sourceModule.AtomicStoreReferenceAndDiagnostics(m_constantTuple, EvaluatedConstant.None, Nothing, CompilationStage.Declare)
                    End If
                End If

                Return m_constantTuple.Value
            End Function

            Protected Overrides Function GetInferredConstantType() As TypeSymbol
                Return m_constantTuple.Type
            End Function

        End Class
    End Class
End Namespace