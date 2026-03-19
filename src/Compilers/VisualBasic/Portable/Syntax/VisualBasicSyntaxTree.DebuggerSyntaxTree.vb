' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Public MustInherit Class VisualBasicSyntaxTree
        ''' <summary>
        ''' Use by Expression Evaluator.
        ''' </summary>
        Private NotInheritable Class DebuggerSyntaxTree
            Inherits ParsedSyntaxTree

            Friend Sub New(root As VisualBasicSyntaxNode, text As SourceText, options As VisualBasicParseOptions)
                MyBase.New(text,
                           text.Encoding,
                           text.ChecksumAlgorithm,
                           path:="",
                           options,
                           root,
                           isMyTemplate:=False,
                           diagnosticOptions:=Nothing,
                           cloneRoot:=True)
            End Sub

            Friend Overrides ReadOnly Property SupportsLocations As Boolean
                Get
                    Return True
                End Get
            End Property
        End Class
    End Class
End Namespace
