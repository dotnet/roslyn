' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.StackTraceExplorer
Imports Microsoft.VisualStudio.Shell.Interop
Imports Moq
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests
    Public Class StackTraceExplorerTests
        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/76758")>
        Public Sub CreatingToolWindowDoesNotCloseItsFrame()
            Dim frame = New Mock(Of IVsWindowFrame)
            Using window = New StackTraceExplorerToolWindow With {.Frame = frame.Object}
                window.OnToolWindowCreated()

                frame.Verify(Function(f) f.CloseFrame(It.IsAny(Of UInteger)()), Times.Never())
            End Using
        End Sub
    End Class
End Namespace
