' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' This class associates a symbol with particular custom format for display.
    ''' It can be passed as an argument for an error message in place where symbol display should go, 
    ''' which allows to defer building strings and doing many other things (like loading metadata) 
    ''' associated with that until the error message is actually requested.
    ''' </summary>
    Friend Class CustomSymbolDisplayFormatter
        Friend Shared ReadOnly QualifiedNameFormat As SymbolDisplayFormat = New SymbolDisplayFormat(
                                                                        globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.Omitted,
                                                                        typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                                                                        genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters,
                                                                        memberOptions:=SymbolDisplayMemberOptions.IncludeParameters Or
                                                                                        SymbolDisplayMemberOptions.IncludeContainingType,
                                                                        parameterOptions:=SymbolDisplayParameterOptions.IncludeName Or
                                                                                            SymbolDisplayParameterOptions.IncludeType Or
                                                                                            SymbolDisplayParameterOptions.IncludeParamsRefOut Or
                                                                                            SymbolDisplayParameterOptions.IncludeDefaultValue,
                                                                        miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers Or
                                                                                                SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

        Friend Shared ReadOnly WithContainingTypeFormat As SymbolDisplayFormat = New SymbolDisplayFormat(
                                                                        globalNamespaceStyle:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.GlobalNamespaceStyle,
                                                                        typeQualificationStyle:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.TypeQualificationStyle,
                                                                        genericsOptions:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.GenericsOptions,
                                                                        memberOptions:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.MemberOptions Or
                                                                                        SymbolDisplayMemberOptions.IncludeContainingType,
                                                                        kindOptions:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.KindOptions,
                                                                        parameterOptions:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.ParameterOptions,
                                                                        miscellaneousOptions:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.MiscellaneousOptions)

        Friend Shared ReadOnly ErrorMessageFormatNoModifiersNoReturnType As New SymbolDisplayFormat(
                                                            globalNamespaceStyle:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.GlobalNamespaceStyle,
                                                            typeQualificationStyle:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.TypeQualificationStyle,
                                                            genericsOptions:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.GenericsOptions,
                                                            memberOptions:=SymbolDisplayMemberOptions.IncludeContainingType Or
                                                                           SymbolDisplayMemberOptions.IncludeExplicitInterface Or
                                                                           SymbolDisplayMemberOptions.IncludeParameters,
                                                            parameterOptions:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.ParameterOptions,
                                                            propertyStyle:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.PropertyStyle,
                                                            localOptions:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.LocalOptions,
                                                            kindOptions:=SymbolDisplayKindOptions.None,
                                                            miscellaneousOptions:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.MiscellaneousOptions)

        Friend Shared ReadOnly ErrorNameWithKindFormat As New SymbolDisplayFormat(
                                                            globalNamespaceStyle:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.GlobalNamespaceStyle,
                                                            typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameOnly,
                                                            genericsOptions:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.GenericsOptions,
                                                            memberOptions:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.MemberOptions,
                                                            parameterOptions:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.ParameterOptions,
                                                            propertyStyle:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.PropertyStyle,
                                                            localOptions:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.LocalOptions,
                                                            kindOptions:=SymbolDisplayKindOptions.IncludeTypeKeyword Or SymbolDisplayKindOptions.IncludeNamespaceKeyword,
                                                            miscellaneousOptions:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.MiscellaneousOptions)

        ' vb error format + complete signature for delegates instead of just the name.
        Friend Shared ReadOnly DelegateSignatureFormat As New SymbolDisplayFormat(
                                                            globalNamespaceStyle:=SymbolDisplayFormat.VisualBasicShortErrorMessageFormat.GlobalNamespaceStyle,
                                                            typeQualificationStyle:=SymbolDisplayFormat.VisualBasicShortErrorMessageFormat.TypeQualificationStyle,
                                                            genericsOptions:=SymbolDisplayFormat.VisualBasicShortErrorMessageFormat.GenericsOptions,
                                                            memberOptions:=SymbolDisplayFormat.VisualBasicShortErrorMessageFormat.MemberOptions,
                                                            parameterOptions:=SymbolDisplayFormat.VisualBasicShortErrorMessageFormat.ParameterOptions,
                                                            propertyStyle:=SymbolDisplayFormat.VisualBasicShortErrorMessageFormat.PropertyStyle,
                                                            localOptions:=SymbolDisplayFormat.VisualBasicShortErrorMessageFormat.LocalOptions,
                                                            kindOptions:=SymbolDisplayKindOptions.IncludeTypeKeyword Or SymbolDisplayKindOptions.IncludeNamespaceKeyword,
                                                            delegateStyle:=SymbolDisplayDelegateStyle.NameAndSignature,
                                                            miscellaneousOptions:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.MiscellaneousOptions)

        ' Like Short format, but includes type arguments 
        Friend Shared ReadOnly ShortWithTypeArgsFormat As New SymbolDisplayFormat(
                                                            globalNamespaceStyle:=SymbolDisplayFormat.ShortFormat.GlobalNamespaceStyle,
                                                            typeQualificationStyle:=SymbolDisplayFormat.ShortFormat.TypeQualificationStyle,
                                                            genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters,
                                                            memberOptions:=SymbolDisplayFormat.ShortFormat.MemberOptions,
                                                            parameterOptions:=SymbolDisplayFormat.ShortFormat.ParameterOptions,
                                                            propertyStyle:=SymbolDisplayFormat.ShortFormat.PropertyStyle,
                                                            localOptions:=SymbolDisplayFormat.ShortFormat.LocalOptions,
                                                            kindOptions:=SymbolDisplayFormat.ShortFormat.KindOptions,
                                                            miscellaneousOptions:=SymbolDisplayFormat.ShortFormat.MiscellaneousOptions)

        ' Like Short format, but includes type arguments and containing types
        Friend Shared ReadOnly ShortWithTypeArgsAndContainingTypesFormat As New SymbolDisplayFormat(
                                                            globalNamespaceStyle:=SymbolDisplayFormat.ShortFormat.GlobalNamespaceStyle,
                                                            typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                                                            genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters,
                                                            memberOptions:=SymbolDisplayFormat.ShortFormat.MemberOptions,
                                                            parameterOptions:=SymbolDisplayFormat.ShortFormat.ParameterOptions,
                                                            propertyStyle:=SymbolDisplayFormat.ShortFormat.PropertyStyle,
                                                            localOptions:=SymbolDisplayFormat.ShortFormat.LocalOptions,
                                                            kindOptions:=SymbolDisplayFormat.ShortFormat.KindOptions,
                                                            miscellaneousOptions:=SymbolDisplayFormat.ShortFormat.MiscellaneousOptions)

        Public Shared Function QualifiedName(symbol As Symbol) As FormattedSymbol
            Return New FormattedSymbol(symbol, QualifiedNameFormat)
        End Function

        Public Shared Function WithContainingType(symbol As Symbol) As FormattedSymbol
            Return New FormattedSymbol(symbol, WithContainingTypeFormat)
        End Function

        Public Shared Function ErrorNameWithKind(symbol As Symbol) As FormattedSymbol
            Return New FormattedSymbol(symbol, ErrorNameWithKindFormat)
        End Function

        Public Shared Function ShortErrorName(symbol As Symbol) As FormattedSymbol
            Return New FormattedSymbol(symbol, SymbolDisplayFormat.ShortFormat)
        End Function

        Public Shared Function DelegateSignature(symbol As Symbol) As FormattedSymbol
            Return New FormattedSymbol(symbol, DelegateSignatureFormat)
        End Function

        Public Shared Function ShortNameWithTypeArgs(symbol As Symbol) As FormattedSymbol
            Return New FormattedSymbol(symbol, ShortWithTypeArgsFormat)
        End Function

        Public Shared Function ShortNameWithTypeArgsAndContainingTypes(symbol As Symbol) As FormattedSymbol
            Return New FormattedSymbol(symbol, ShortWithTypeArgsAndContainingTypesFormat)
        End Function

        Public Shared Function DefaultErrorFormat(symbol As Symbol) As FormattedSymbol
            Return New FormattedSymbol(symbol, SymbolDisplayFormat.VisualBasicErrorMessageFormat)
        End Function
    End Class
End Namespace
