' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend Module SourceDocumentationCommentUtils
        Friend Function GetAndCacheDocumentationComment(
            symbol As Symbol,
            preferredCulture As CultureInfo,
            expandIncludes As Boolean,
            ByRef lazyXmlText As String,
            cancellationToken As CancellationToken
        ) As String

            If lazyXmlText Is Nothing Then
                Dim xmlText = GetDocumentationCommentForSymbol(symbol, preferredCulture, expandIncludes, cancellationToken)
                Interlocked.CompareExchange(lazyXmlText, xmlText, Nothing)
            End If

            Return lazyXmlText
        End Function

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
