' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class VisualBasicScriptCompilationInfo
        Inherits ScriptCompilationInfo

        Public Shadows ReadOnly Property PreviousScriptCompilation As VisualBasicCompilation

        Friend Sub New(previousCompilationOpt As VisualBasicCompilation, returnType As Type, globalsType As Type)
            MyBase.New(returnType, globalsType)

            Debug.Assert(previousCompilationOpt Is Nothing OrElse previousCompilationOpt.HostObjectType Is globalsType)
            PreviousScriptCompilation = previousCompilationOpt
        End Sub

        Friend Overrides ReadOnly Property CommonPreviousScriptCompilation As Compilation
            Get
                Return PreviousScriptCompilation
            End Get
        End Property

        Public Shadows Function WithPreviousScriptCompilation(compilation As VisualBasicCompilation) As VisualBasicScriptCompilationInfo
            Return If(compilation Is PreviousScriptCompilation, Me, New VisualBasicScriptCompilationInfo(compilation, ReturnType, GlobalsType))
        End Function

        Friend Overrides Function CommonWithPreviousScriptCompilation(compilation As Compilation) As ScriptCompilationInfo
            Return WithPreviousScriptCompilation(DirectCast(compilation, VisualBasicCompilation))
        End Function
    End Class
End Namespace
