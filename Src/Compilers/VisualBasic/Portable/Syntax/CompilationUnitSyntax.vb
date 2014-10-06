' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Partial Public NotInheritable Class CompilationUnitSyntax
        Inherits VBSyntaxNode

        ''' <summary> 
        ''' Returns #r directives specified in the compilation. 
        ''' </summary>       
        Public Function GetReferenceDirectives() As IList(Of ReferenceDirectiveTriviaSyntax)
            Return GetReferenceDirectives(Nothing)
        End Function

        Friend Function GetReferenceDirectives(filter As Func(Of ReferenceDirectiveTriviaSyntax, Boolean)) As IList(Of ReferenceDirectiveTriviaSyntax)
            ' TODO: tomat
            ' enable test ReferenceManagerReuse_WithSyntaxTrees when implemented
            ' Return SyntaxNode.GetDirectives(Me.GetFirstToken(includeZeroWidth:=True), filter)
            Return SpecializedCollections.EmptyList(Of ReferenceDirectiveTriviaSyntax)()
        End Function
    End Class
End Namespace

