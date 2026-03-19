' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary> 
    ''' Manages anonymous types and delegates created on module level. All requests 
    ''' for anonymous type/delegate symbols go via the instance of this class.
    ''' 
    ''' Manager also is in charge of creating implementation types which are used in 
    ''' emit phase to substitute anonymous type/delegate public symbols.
    ''' </summary>
    Partial Friend NotInheritable Class AnonymousTypeManager
        Inherits CommonAnonymousTypeManager

        ''' <summary> Source module </summary>
        Public ReadOnly Property ContainingModule As SourceModuleSymbol
            Get
                Return DirectCast(Compilation.SourceModule, SourceModuleSymbol)
            End Get
        End Property

        ''' <summary> Owning compilationSource module </summary>
        Public ReadOnly Compilation As VisualBasicCompilation

        Public Sub New(compilation As VisualBasicCompilation)
            Me.Compilation = compilation
        End Sub

        ''' <summary> 
        ''' Given anonymous type descriptor provided construct an anonymous type symbol
        ''' </summary>
        Public Function ConstructAnonymousTypeSymbol(typeDescr As AnonymousTypeDescriptor, diagnostics As BindingDiagnosticBag) As AnonymousTypePublicSymbol
            If diagnostics.AccumulatesDependencies Then
                Dim dependencies = BindingDiagnosticBag.GetInstance(withDependencies:=True, withDiagnostics:=False)
                ReportMissingOrErroneousSymbols(dependencies, hasClass:=True, hasDelegate:=False, hasKeys:=typeDescr.Fields.Any(Function(f) f.IsKey))
                diagnostics.AddRange(dependencies)
                dependencies.Free()
            End If

            Return New AnonymousTypePublicSymbol(Me, typeDescr)
        End Function

        ''' <summary> 
        ''' Given anonymous delegate descriptor provided, construct an anonymous delegate symbol
        ''' </summary>
        Public Function ConstructAnonymousDelegateSymbol(delegateDescriptor As AnonymousTypeDescriptor) As AnonymousDelegatePublicSymbol
            ' ReportMissingOrErroneousSymbols reports only Special types for delegates.
            ' Therefore, we have no additional dependencies to report here.
            Return New AnonymousDelegatePublicSymbol(Me, delegateDescriptor)
        End Function

    End Class

End Namespace
