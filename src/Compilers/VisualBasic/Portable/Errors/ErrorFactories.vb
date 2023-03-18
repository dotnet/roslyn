' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Reflection
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Class ErrorFactory

        Private Const s_titleSuffix As String = "_Title"
        Private Const s_descriptionSuffix As String = "_Description"
        Private Shared ReadOnly s_categoriesMap As Lazy(Of ImmutableDictionary(Of ERRID, String)) = New Lazy(Of ImmutableDictionary(Of ERRID, String))(AddressOf CreateCategoriesMap)

        Private Shared Function CreateCategoriesMap() As ImmutableDictionary(Of ERRID, String)
            Dim map = New Dictionary(Of ERRID, String) From
                {   '  { ERROR_CODE,    CATEGORY }
                }

            Return map.ToImmutableDictionary
        End Function

        Public Shared ReadOnly VoidDiagnosticInfo As DiagnosticInfo = ErrorInfo(ERRID.Void)
        Public Shared ReadOnly EmptyDiagnosticInfo As DiagnosticInfo = ErrorInfo(ERRID.ERR_None)

        Public Shared ReadOnly GetErrorInfo_ERR_WithEventsRequiresClass As Func(Of DiagnosticInfo) =
            Function() ErrorInfo(ERRID.ERR_WithEventsRequiresClass)

        Public Shared ReadOnly GetErrorInfo_ERR_StrictDisallowImplicitObject As Func(Of DiagnosticInfo) =
            Function() ErrorInfo(ERRID.ERR_StrictDisallowImplicitObject)

        Public Shared ReadOnly GetErrorInfo_WRN_ObjectAssumedVar1_WRN_StaticLocalNoInference As Func(Of DiagnosticInfo) =
            Function() ErrorInfo(ERRID.WRN_ObjectAssumedVar1, ErrorInfo(ERRID.WRN_StaticLocalNoInference))

        Public Shared ReadOnly GetErrorInfo_WRN_ObjectAssumedVar1_WRN_MissingAsClauseinVarDecl As Func(Of DiagnosticInfo) =
            Function() ErrorInfo(ERRID.WRN_ObjectAssumedVar1, ErrorInfo(ERRID.WRN_MissingAsClauseinVarDecl))

        Public Shared ReadOnly GetErrorInfo_ERR_StrictDisallowsImplicitProc As Func(Of DiagnosticInfo) =
            Function() ErrorInfo(ERRID.ERR_StrictDisallowsImplicitProc)

        Public Shared ReadOnly GetErrorInfo_ERR_StrictDisallowsImplicitArgs As Func(Of DiagnosticInfo) =
            Function() ErrorInfo(ERRID.ERR_StrictDisallowsImplicitArgs)

        Public Shared ReadOnly GetErrorInfo_WRN_ObjectAssumed1_WRN_MissingAsClauseinFunction As Func(Of DiagnosticInfo) =
            Function() ErrorInfo(ERRID.WRN_ObjectAssumed1, ErrorInfo(ERRID.WRN_MissingAsClauseinFunction))

        Public Shared ReadOnly GetErrorInfo_WRN_ObjectAssumed1_WRN_MissingAsClauseinOperator As Func(Of DiagnosticInfo) =
            Function() ErrorInfo(ERRID.WRN_ObjectAssumed1, ErrorInfo(ERRID.WRN_MissingAsClauseinOperator))

        Public Shared ReadOnly GetErrorInfo_WRN_ObjectAssumedProperty1_WRN_MissingAsClauseinProperty As Func(Of DiagnosticInfo) =
            Function() ErrorInfo(ERRID.WRN_ObjectAssumedProperty1, ErrorInfo(ERRID.WRN_MissingAsClauseinProperty))

        Public Shared Function ErrorInfo(id As ERRID) As DiagnosticInfo
            Return New DiagnosticInfo(MessageProvider.Instance, id)
        End Function

        Public Shared Function ErrorInfo(id As ERRID, ParamArray arguments As Object()) As DiagnosticInfo
            Return New DiagnosticInfo(MessageProvider.Instance, id, arguments)
        End Function

        Public Shared Function ObsoleteErrorInfo(id As ERRID, data As ObsoleteAttributeData, ParamArray arguments As Object()) As CustomObsoleteDiagnosticInfo
            Return New CustomObsoleteDiagnosticInfo(MessageProvider.Instance, id, data, arguments)
        End Function

        Public Shared Function ErrorInfo(id As ERRID, ByRef syntaxToken As SyntaxToken) As DiagnosticInfo
            Return ErrorInfo(id, SyntaxFacts.GetText(syntaxToken.Kind))
        End Function

        Public Shared Function ErrorInfo(id As ERRID, ByRef syntaxTokenKind As SyntaxKind) As DiagnosticInfo
            Return ErrorInfo(id, SyntaxFacts.GetText(syntaxTokenKind))
        End Function

        Public Shared Function ErrorInfo(id As ERRID, ByRef syntaxToken As SyntaxToken, type As TypeSymbol) As DiagnosticInfo
            Return ErrorInfo(id, SyntaxFacts.GetText(syntaxToken.Kind), type)
        End Function

        Public Shared Function ErrorInfo(id As ERRID, ByRef syntaxToken As SyntaxToken, type1 As TypeSymbol, type2 As TypeSymbol) As DiagnosticInfo
            Return ErrorInfo(id, SyntaxFacts.GetText(syntaxToken.Kind), type1, type2)
        End Function

        Private Shared s_resourceManager As Resources.ResourceManager
        Friend Shared ReadOnly Property ResourceManager As Resources.ResourceManager
            Get
                If s_resourceManager Is Nothing Then
                    s_resourceManager = New Resources.ResourceManager(GetType(VBResources).FullName, GetType(ERRID).GetTypeInfo().Assembly)
                End If

                Return s_resourceManager
            End Get
        End Property

        'The function is a gigantic num->string switch, so verifying it is not interesting, but expensive.
        Friend Shared Function IdToString(id As ERRID) As String
            Return IdToString(id, CultureInfo.CurrentUICulture)
        End Function

        'The function is a gigantic num->string switch, so verifying it is not interesting, but expensive.
        Public Shared Function IdToString(id As ERRID, language As CultureInfo) As String
            Return ResourceManager.GetString(id.ToString(), language)
        End Function

        Public Shared Function GetMessageFormat(id As ERRID) As LocalizableResourceString
            Return New LocalizableResourceString(id.ToString(), ResourceManager, GetType(ErrorFactory))
        End Function

        Public Shared Function GetTitle(id As ERRID) As LocalizableResourceString
            Return New LocalizableResourceString(id.ToString() + s_titleSuffix, ResourceManager, GetType(ErrorFactory))
        End Function

        Public Shared Function GetDescription(id As ERRID) As LocalizableResourceString
            Return New LocalizableResourceString(id.ToString() + s_descriptionSuffix, ResourceManager, GetType(ErrorFactory))
        End Function

        Public Shared Function GetHelpLink(id As ERRID) As String
            Dim idWithLanguagePrefix As String = MessageProvider.Instance.GetIdForErrorCode(CInt(id))
            Return $"https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k({idWithLanguagePrefix})"
        End Function

        Public Shared Function GetCategory(id As ERRID) As String
            Dim category As String = Nothing
            If s_categoriesMap.Value.TryGetValue(id, category) Then
                Return category
            End If

            Return Diagnostic.CompilerDiagnosticCategory
        End Function
    End Class

    ''' <summary>
    ''' Concatenates messages for a set of DiagnosticInfo.
    ''' </summary>
    Friend Class CompoundDiagnosticInfo
        Inherits DiagnosticInfo

        Friend Sub New(arguments As DiagnosticInfo())
            MyBase.New(VisualBasic.MessageProvider.Instance, 0, arguments)
        End Sub

        Public Overrides Function GetMessage(Optional formatProvider As IFormatProvider = Nothing) As String
            Dim builder = PooledStringBuilder.GetInstance()

            If Arguments IsNot Nothing Then
                For Each info As DiagnosticInfo In Arguments
                    builder.Builder.Append(info.GetMessage(formatProvider))
                Next
            End If

            Dim message = builder.Builder.ToString()
            builder.Free()

            Return message
        End Function
    End Class

End Namespace
