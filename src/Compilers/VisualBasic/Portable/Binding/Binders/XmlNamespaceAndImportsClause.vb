' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Structure XmlNamespaceAndImportsClausePosition
        Public ReadOnly XmlNamespace As String
        Public ReadOnly ImportsClausePosition As Integer

        Public Sub New(xmlNamespace As String, importsClausePosition As Integer)
            Me.XmlNamespace = xmlNamespace
            Me.ImportsClausePosition = importsClausePosition
        End Sub
    End Structure
End Namespace
