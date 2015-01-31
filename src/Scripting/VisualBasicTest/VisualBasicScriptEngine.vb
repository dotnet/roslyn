' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports Microsoft.CodeAnalysis

Namespace Microsoft.CodeAnalysis.Scripting.VisualBasic
    ''' <summary> 
    ''' Represents a runtime execution context for Visual Basic scripts.
    ''' </summary>    
    Friend NotInheritable Class VisualBasicScriptEngine
        Inherits ScriptEngine

        Public Sub New(Optional metadataReferenceProvider As MetadataFileReferenceProvider = Nothing, Optional assemblyLoader As AssemblyLoader = Nothing)
            MyBase.New(metadataReferenceProvider, assemblyLoader)
        End Sub

        Friend Overrides Function Create(code As String, options As ScriptOptions, globalsType As Type, returnType As Type) As Script
            Return VisualBasicScript.Create(code, options).WithGlobalsType(globalsType).WithReturnType(returnType).WithBuilder(Me.Builder)
        End Function
    End Class
End Namespace

