' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
Imports Microsoft.CodeAnalysis.NamingStyles
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.NamingStyles
    Partial Public Class NamingStylesTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Private ReadOnly Property LocalNamesAreCamelCase As IDictionary(Of OptionKey, Object)
            Get
                Return Options(New OptionKey(SimplificationOptions.NamingPreferences, LanguageNames.VisualBasic), LocalNamesAreCamelCaseOption())
            End Get
        End Property

        Private ReadOnly Property ConstantsAreUpperCase As IDictionary(Of OptionKey, Object)
            Get
                Return Options(New OptionKey(SimplificationOptions.NamingPreferences, LanguageNames.VisualBasic), ConstantsAreUpperCaseOption())
            End Get
        End Property

        Private Shared Function Options([option] As OptionKey, value As Object) As IDictionary(Of OptionKey, Object)
            Return New Dictionary(Of OptionKey, Object) From
            {
                {[option], value}
            }
        End Function

        Private Shared Function LocalNamesAreCamelCaseOption() As NamingStylePreferences
            Dim _symbolSpecification = New SymbolSpecification(
                Nothing,
                "Name",
                ImmutableArray.Create(New SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Local)),
                ImmutableArray(Of Accessibility).Empty,
                ImmutableArray(Of SymbolSpecification.ModifierKind).Empty)

            Dim namingStyle = New NamingStyle(
                Guid.NewGuid(),
                capitalizationScheme:=Capitalization.CamelCase,
                name:="Name",
                prefix:="",
                suffix:="",
                wordSeparator:="")

            Dim namingRule = New SerializableNamingRule() With
            {
                .SymbolSpecificationID = _symbolSpecification.ID,
                .NamingStyleID = namingStyle.ID,
                .EnforcementLevel = DiagnosticSeverity.Error
            }

            Dim info = New NamingStylePreferences(
                ImmutableArray.Create(_symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule))

            Return info
        End Function

        Private Shared Function ConstantsAreUpperCaseOption() As NamingStylePreferences
            Dim _symbolSpecification = New SymbolSpecification(
                Nothing,
                "Name",
                ImmutableArray.Create(
                    New SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field),
                    New SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Local)),
                ImmutableArray(Of Accessibility).Empty,
                ImmutableArray.Create(New SymbolSpecification.ModifierKind(SymbolSpecification.ModifierKindEnum.IsConst)))

            Dim namingStyle = New NamingStyle(
                Guid.NewGuid(),
                capitalizationScheme:=Capitalization.AllUpper,
                name:="Name",
                prefix:="",
                suffix:="",
                wordSeparator:="")

            Dim namingRule = New SerializableNamingRule() With
            {
                .SymbolSpecificationID = _symbolSpecification.ID,
                .NamingStyleID = namingStyle.ID,
                .EnforcementLevel = DiagnosticSeverity.Error
            }

            Dim info = New NamingStylePreferences(
                ImmutableArray.Create(_symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule))

            Return info
        End Function
    End Class
End Namespace
