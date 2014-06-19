Imports System.Collections.Generic
Imports System.Threading
Imports Roslyn.Compilers
Imports Roslyn.Compilers.Collections
Imports Roslyn.Compilers.Internal

Namespace Roslyn.Compilers.VisualBasic
    Partial Friend NotInheritable Class AnonymousTypeManager

        Private NotInheritable Class AnonymousDelegateMethodSymbol
            Inherits SynthesizedDelegateMethodSymbol

            ''' <summary>
            ''' Initializes a new instance of the <see cref="AnonymousDelegateMethodSymbol" /> class. The parameters are not initialized and need to be set 
            ''' by using the <see cref="SetParameters" /> method.
            ''' </summary>
            ''' <param name="name">The name of this method.</param>
            ''' <param name="containingSymbol">The containing symbol.</param>
            ''' <param name="flags">The flags for this method.</param>
            ''' <param name="returnType">The return type.</param>
            Public Sub New(name As String,
                           containingSymbol As AnonymousDelegateTemplateSymbol,
                           flags As SourceMemberFlags,
                           returnType As TypeSymbol)
                MyBase.New(name, containingSymbol, flags, returnType)
            End Sub

            ''' <summary>
            ''' If True, suppresses generation of debug info in this method even if generateDebugInfo is true
            ''' </summary>
            Friend Overrides ReadOnly Property SuppressDebugInfo As Boolean
                Get
                    Return True
                End Get
            End Property
        End Class

    End Class
End Namespace
