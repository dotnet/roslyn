' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Scripting.VisualBasic
    ''' <summary> 
    ''' Represents a runtime execution context for Visual Basic scripts.
    ''' </summary>    
    Friend NotInheritable Class VisualBasicScriptEngine
        Inherits ScriptEngine

        Public Sub New(Optional metadataReferenceProvider As MetadataFileReferenceProvider = Nothing)
            MyBase.New(ScriptOptions.Default.WithReferences(TestBase.LatestVbReferences), metadataReferenceProvider)
        End Sub

        Friend Overrides Function Create(Of T)(code As String, options As ScriptOptions, globalsType As Type) As Script(Of T)
            Return VisualBasicScript.Create(Of T)(code, options).WithGlobalsType(globalsType).WithBuilder(Me.Builder)
        End Function
    End Class
End Namespace

