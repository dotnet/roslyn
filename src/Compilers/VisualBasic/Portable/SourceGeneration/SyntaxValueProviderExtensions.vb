'' Licensed to the .NET Foundation under one Or more agreements.
'' The .NET Foundation licenses this file to you under the MIT license.
'' See the LICENSE file in the project root for more information.

'Imports System.Runtime.CompilerServices
'Imports Microsoft.CodeAnalysis.SourceGeneration
'Imports Microsoft.CodeAnalysis.VisualBasic

'Namespace Microsoft.CodeAnalysis.VisualBasic.SourceGeneration
'    Friend Module SyntaxValueProviderExtensions
'        <Extension>
'        Friend Function CreateSyntaxProviderForAttribute(Of T As SyntaxNode)(provider As SyntaxValueProvider, simpleName As String) As IncrementalValuesProvider(Of T)
'            Return provider.CreateSyntaxProviderForAttribute(Of T)(simpleName, VisualBasicSyntaxHelper.Instance, compilationGlobalAliases:=Nothing)
'        End Function
'    End Module
'End Namespace
