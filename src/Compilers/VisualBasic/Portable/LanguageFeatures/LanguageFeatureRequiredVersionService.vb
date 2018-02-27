Imports Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic.LanguageFeatures


    Friend Class VisualBasicRequiredLanguageVersionService
        Private ReadOnly Property _RequiredVersionsForFeature As New Dictionary(Of Feature, VisualBasicRequiredLanguageVersion)()
        Public Shared ReadOnly Instance As New VisualBasicRequiredLanguageVersionService

        Private Sub New()
            For Each f As Feature In [Enum].GetValues(GetType(Feature))
                Dim requiredVersion = New VisualBasicRequiredLanguageVersion(FeatureExtensions.GetLanguageVersion(f))
                _RequiredVersionsForFeature.Add(f, requiredVersion)
            Next
        End Sub

        Public Function GetRequiredLanguageVersion(f As Feature) As VisualBasicRequiredLanguageVersion
            Return Me._RequiredVersionsForFeature(f)
        End Function
    End Class

    Friend Class VisualBasicRequiredLanguageVersion
        Inherits RequiredLanguageVersion

        Friend ReadOnly Property Version As LanguageVersion

        Friend Sub New(version As LanguageVersion)
            Me.Version = version
        End Sub

        Public Overrides Function ToString() As String
            Return Version.ToDisplayString()
        End Function
    End Class

    Module CheckFeatureAvailability

#Region "Feature Checking Extensions"

#Region "Checking against a Language Version"
        ''' <summary>
        ''' Check to see if a language <paramref name="feature"/> is available with the <see cref="LanguageVersion"/>
        ''' specified in the <paramref name="options"/> (<see cref="VisualBasicParseOptions"/>).
        ''' </summary>
        ''' <param name="feature">Language feature to check is available.</param>
        ''' <param name="options">The parse options being used.</param>
        ''' <returns>True if the feature's language version is compatible with the specified language version.</returns>
        <Extension>
        Private Function IsInLanguageVersion(feature As Feature, options As VisualBasicParseOptions) As Boolean
            Return CheckVersionNumbers(required:=feature.GetLanguageVersion(), current:=options.LanguageVersion)
        End Function

        <Extension>
        Private Function IsInLanguageVersion(feature As Feature, options As VisualBasicCompilation) As Boolean
            Return CheckVersionNumbers(required:=feature.GetLanguageVersion(), current:=options.LanguageVersion)
        End Function

        Private Function CheckVersionNumbers(required As LanguageVersion, current As LanguageVersion) As Boolean
            Return current >= required
        End Function
#End Region

#Region "(feature).IsAvailable"
        ''' <summary>
        ''' Check to see if a language <paramref name="feature"/> is available with these <paramref name="options"/>.
        ''' </summary>
        ''' <remarks>
        ''' Feature maybe enabled explicitly with a Feature Flag or enabled implicitly via a Language Version.
        ''' </remarks>
        ''' <param name="feature">Language feature to check is available.</param>
        ''' <param name="options">The parse options being used.</param>
        <Extension>
        Friend Function IsAvailable(feature As Feature, options As VisualBasicParseOptions) As Boolean
            Dim flag As String = Nothing
            If TryGetFeatureFlag(feature, flag) Then Return CheckFeatures(flag, options)
            Return feature.IsInLanguageVersion(options)
        End Function

        ''' <summary>
        ''' Check to see if a language <paramref name="feature"/> is available with this <paramref name="compilation"/>.
        ''' </summary>
        ''' <param name="feature">Language feature to check is available.</param>
        ''' <param name="compilation">The <see cref="VisualBasicCompilation"/> being used.</param>
        <Extension>
        Friend Function IsAvailable(feature As Feature, compilation As VisualBasicCompilation) As Boolean
            Return feature.IsInLanguageVersion(compilation)
        End Function
#End Region

#Region "Checking against Options.Features"
        Private Function TryGetFeatureFlag(f As Feature, ByRef flag As String) As Boolean
            flag = f.GetFeatureFlag()
            Return flag IsNot Nothing
        End Function

        Private Function CheckFeatures(featureFlag As String, options As VisualBasicParseOptions) As Boolean
            Debug.Assert(featureFlag IsNot Nothing, NameOf(featureFlag) & " can not be nothing.")
            Return options.Features.ContainsKey(featureFlag)
        End Function
#End Region

#Region "Reporting Unavailablity of a Feature."

        ''' <summary>Report the unavailability of a language feature.</summary>
        ''' <typeparam name="TNode">The <see cref="VisualBasicSyntaxNode"/> to use.</typeparam>
        ''' <param name="feature">Language feature to report as unavailable.</param>
        ''' <param name="options">The parse options being used.</param>
        ''' <param name="node">The node to attach the diagnostic to, when feature is unavailable.</param>
        ''' <returns>
        ''' If Feature is unavailable return the <paramref name="node"/> with the unavailable diagnostic attached to it.</returns>
        <Extension>
        Friend Function ReportFeatureUnavailable(Of TNode As Syntax.InternalSyntax.VisualBasicSyntaxNode)(node As TNode, feature As Feature, options As VisualBasicParseOptions) As TNode
            Dim f = feature.GetNameAndRequiredVersion()
            Return Syntax.InternalSyntax.Parser.ReportSyntaxError(node, ERRID.ERR_LanguageVersion, options.LanguageVersion.GetErrorName(), f.Info, f.Version)
        End Function

        Private Function ReportFeatureUnavailable(feature As Feature, options As VisualBasicParseOptions, location As Location) As Diagnostic
            Dim f = feature.GetNameAndRequiredVersion()
            Dim info = ErrorFactory.ErrorInfo(ERRID.ERR_LanguageVersion, options.LanguageVersion.GetErrorName(), f.Info, f.Version)
            Return New VBDiagnostic(info, location)
        End Function

#End Region

#Region "Check availability of a Feature and report is unavailable."
        <Extension>
        Private Function GetNameAndRequiredVersion(feature As Feature) As (Info As DiagnosticInfo, Version As VisualBasicRequiredLanguageVersion)
            Return (ErrorFactory.ErrorInfo(feature.GetResourceId), VisualBasicRequiredLanguageVersionService.Instance.GetRequiredLanguageVersion(feature))
        End Function
        ''' <summary>
        ''' Check to see if a language <paramref name="feature"/> is available with the <paramref name="options"/> being used.
        ''' If unavailable the function return the node with an unavailable diagnostic attached to <paramref name="node"/>.
        ''' </summary>
        ''' <returns>If <see cref="feature"/> is not available, returns node with unavailable diagnostic attached to it.</returns>
        ''' <param name="node">The node to attach the potential diagnostic (Feature Unavailable).</param>
        ''' <param name="feature">Language feature to check is available.</param>
        ''' <param name="options">The parse options being used.</param>
        <Extension>
        Friend Function CheckFeatureAvailability(Of TNode As Syntax.InternalSyntax.VisualBasicSyntaxNode)(node As TNode, feature As Feature, options As VisualBasicParseOptions) As TNode
            Return If(feature.IsAvailable(options), node, node.ReportFeatureUnavailable(feature, options))
        End Function

        ''' <summary>
        ''' Checks the availability of a language <paramref name="feature"/> with these <paramref name="options"/>.
        ''' </summary>
        ''' <returns>
        ''' A <see cref="System.Boolean"/> indicating the availability.
        ''' Should a feature be unavailable, a feature unavailable <see cref="Diagnostic"/> at <paramref name="location"/>
        ''' is placed into the <paramref name="diagBag"/>.
        ''' </returns>
        ''' <param name="feature">Language feature to check is available.</param>
        ''' <param name="options">The parse options being used.</param>
        ''' <param name="location">The location to report the diagnostic.</param>
        ''' <param name="diagbag">Where to place the <see cref="Diagnostic"/>.</param>
        <Extension>
        Friend Function CheckFeatureAvailability(feature As Feature, options As VisualBasicParseOptions, location As Location, diagBag As DiagnosticBag) As Boolean
            Dim result = feature.IsAvailable(options)
            If Not result Then diagBag.Add(ReportFeatureUnavailable(feature, options, location))
            Return result
        End Function
#End Region

#End Region

    End Module

    Module SpecificFeature

        ''' <summary>Inference of tuple element names was added in VB 15.3</summary>
        <Extension>
        Friend Function DisallowInferredTupleElementNames(self As LanguageVersion) As Boolean
            Return self < Feature.InferredTupleNames.GetLanguageVersion()
        End Function

        <Extension>
        Friend Function AllowNonTrailingNamedArguments(self As LanguageVersion) As Boolean
            Return self >= Feature.NonTrailingNamedArguments.GetLanguageVersion()
        End Function

        ''' <summary>After VB15.5 it is possible to use named arguments in non-trailing position, except in attribute lists (where it remains disallowed)</summary>
        Friend Function ReportNonTrailingNamedArgumentIfNeeded(argument As ArgumentSyntax, seenNames As Boolean, allowNonTrailingNamedArguments As Boolean) As ArgumentSyntax
            If Not seenNames OrElse allowNonTrailingNamedArguments Then
                Return argument
            End If

            Return Parser.ReportSyntaxError(argument, ERRID.ERR_ExpectedNamedArgument, VisualBasicRequiredLanguageVersionService.Instance.GetRequiredLanguageVersion(Feature.NonTrailingNamedArguments))
        End Function
    End Module
End Namespace
