' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Scripting.VisualBasic
    ''' <summary> 
    ''' Represents a runtime execution context for Visual Basic scripts.
    ''' </summary>    
    Friend NotInheritable Class VisualBasicScriptEngine
        Inherits ScriptEngine

        Public Sub New(Optional metadataReferenceProvider As MetadataFileReferenceProvider = Nothing)
            MyBase.New(metadataReferenceProvider)
        End Sub

        Friend Overrides Function Create(Of T)(code As String, options As ScriptOptions, globalsType As Type) As Script(Of T)
            Return DirectCast(VisualBasicScript.Create(Of T)(code, options).WithGlobalsType(globalsType).WithBuilder(Me.Builder), Script(Of T))
        End Function
    End Class
End Namespace

