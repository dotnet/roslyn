' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Text
Imports System.Threading
Imports System.Globalization
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend Module SourceDocumentationCommentUtils

        ''' <summary>
        ''' Returns documentation comment for a type, field, property, event or method, 
        ''' discards all the diagnostics
        ''' </summary>
        ''' <returns>
        ''' Returns Nothing if there is no documentation comment on the type or 
        ''' there were errors preventing such a comment from being generated,
        ''' XML string otherwise
        ''' </returns>
        Friend Function GetDocumentationCommentForSymbol(symbol As Symbol,
                                                         preferredCulture As CultureInfo,
                                                         expandIncludes As Boolean,
                                                         cancellationToken As CancellationToken) As String

            Return VisualBasicCompilation.DocumentationCommentCompiler.
                        GetDocumentationCommentXml(symbol, expandIncludes, preferredCulture, cancellationToken)
        End Function

    End Module
End Namespace
