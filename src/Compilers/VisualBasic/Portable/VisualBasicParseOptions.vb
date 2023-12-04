' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFacts

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' Represents Visual Basic parse options.
    ''' </summary>
    Public NotInheritable Class VisualBasicParseOptions
        Inherits ParseOptions
        Implements IEquatable(Of VisualBasicParseOptions)

        Public Shared ReadOnly Property [Default] As VisualBasicParseOptions = New VisualBasicParseOptions()
        Private Shared s_defaultPreprocessorSymbols As ImmutableArray(Of KeyValuePair(Of String, Object))

        Private _features As ImmutableDictionary(Of String, String)

        Private _preprocessorSymbols As ImmutableArray(Of KeyValuePair(Of String, Object))
        Private _specifiedLanguageVersion As LanguageVersion
        Private _languageVersion As LanguageVersion

        ''' <summary>
        ''' Creates an instance of VisualBasicParseOptions.
        ''' </summary>
        ''' <param name="languageVersion">The parser language version.</param>
        ''' <param name="documentationMode">The documentation mode.</param>
        ''' <param name="kind">The kind of source code.<see cref="SourceCodeKind"/></param>
        ''' <param name="preprocessorSymbols">An enumerable sequence of KeyValuePair representing preprocessor symbols.</param>
        Public Sub New(
            Optional languageVersion As LanguageVersion = LanguageVersion.Default,
            Optional documentationMode As DocumentationMode = DocumentationMode.Parse,
            Optional kind As SourceCodeKind = SourceCodeKind.Regular,
            Optional preprocessorSymbols As IEnumerable(Of KeyValuePair(Of String, Object)) = Nothing)

            MyClass.New(languageVersion,
                        documentationMode,
                        kind,
                        If(preprocessorSymbols Is Nothing, DefaultPreprocessorSymbols, ImmutableArray.CreateRange(preprocessorSymbols)),
                        ImmutableDictionary(Of String, String).Empty)
        End Sub

        Friend Sub New(
            languageVersion As LanguageVersion,
            documentationMode As DocumentationMode,
            kind As SourceCodeKind,
            preprocessorSymbols As ImmutableArray(Of KeyValuePair(Of String, Object)),
            features As ImmutableDictionary(Of String, String))

            MyBase.New(kind, documentationMode)

            _specifiedLanguageVersion = languageVersion
            _languageVersion = languageVersion.MapSpecifiedToEffectiveVersion
            _preprocessorSymbols = preprocessorSymbols.ToImmutableArrayOrEmpty
            _features = If(features, ImmutableDictionary(Of String, String).Empty)
        End Sub

        Private Sub New(other As VisualBasicParseOptions)
            MyClass.New(
                languageVersion:=other._specifiedLanguageVersion,
                documentationMode:=other.DocumentationMode,
                kind:=other.Kind,
                preprocessorSymbols:=other._preprocessorSymbols,
                features:=other._features)
        End Sub

        Public Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Private Shared ReadOnly Property DefaultPreprocessorSymbols As ImmutableArray(Of KeyValuePair(Of String, Object))
            Get
                If s_defaultPreprocessorSymbols.IsDefaultOrEmpty Then
                    s_defaultPreprocessorSymbols = ImmutableArray.Create(KeyValuePairUtil.Create("_MYTYPE", CObj("Empty")))
                End If

                Return s_defaultPreprocessorSymbols
            End Get
        End Property

        ''' <summary>
        ''' Returns the specified language version, which is the value that was specified in the call to the
        ''' constructor, or modified using the <see cref="WithLanguageVersion"/> method, or provided on the command line.
        ''' </summary>        
        Public ReadOnly Property SpecifiedLanguageVersion As LanguageVersion
            Get
                Return _specifiedLanguageVersion
            End Get
        End Property

        ''' <summary>
        ''' Returns the effective language version, which the compiler uses to select the
        ''' language rules to apply to the program.
        ''' </summary>        
        Public ReadOnly Property LanguageVersion As LanguageVersion
            Get
                Return _languageVersion
            End Get
        End Property

        ''' <summary>
        ''' The preprocessor symbols to parse with. 
        ''' </summary>
        ''' <remarks>
        ''' May contain duplicate keys. The last one wins. 
        ''' </remarks>
        Public ReadOnly Property PreprocessorSymbols As ImmutableArray(Of KeyValuePair(Of String, Object))
            Get
                Return _preprocessorSymbols
            End Get
        End Property

        ''' <summary>
        ''' Returns a collection of preprocessor symbol names. 
        ''' </summary>
        Public Overrides ReadOnly Property PreprocessorSymbolNames As IEnumerable(Of String)
            Get
                Return _preprocessorSymbols.Select(Function(ps) ps.Key)
            End Get
        End Property

        ''' <summary>
        ''' Returns a VisualBasicParseOptions instance for a specified language version.
        ''' </summary>
        ''' <param name="version">The parser language version.</param>
        ''' <returns>A new instance of VisualBasicParseOptions if different language version is different; otherwise current instance.</returns>
        Public Shadows Function WithLanguageVersion(version As LanguageVersion) As VisualBasicParseOptions
            If version = _specifiedLanguageVersion Then
                Return Me
            End If

            Dim effectiveVersion = version.MapSpecifiedToEffectiveVersion()
            Return New VisualBasicParseOptions(Me) With {._specifiedLanguageVersion = version, ._languageVersion = effectiveVersion}
        End Function

        ''' <summary>
        ''' Returns a VisualBasicParseOptions instance for a specified source code kind.
        ''' </summary>
        ''' <param name="kind">The parser source code kind.</param>
        ''' <returns>A new instance of VisualBasicParseOptions if source code kind is different; otherwise current instance.</returns>
        Public Shadows Function WithKind(kind As SourceCodeKind) As VisualBasicParseOptions
            If kind = Me.SpecifiedKind Then
                Return Me
            End If

            Dim effectiveKind = kind.MapSpecifiedToEffectiveKind
            Return New VisualBasicParseOptions(Me) With {.SpecifiedKind = kind, .Kind = effectiveKind}
        End Function

        ''' <summary>
        ''' Returns a VisualBasicParseOptions instance for a specified documentation mode.
        ''' </summary>
        ''' <param name="documentationMode"></param>
        ''' <returns>A new instance of VisualBasicParseOptions if documentation mode is different; otherwise current instance.</returns>
        Public Overloads Function WithDocumentationMode(documentationMode As DocumentationMode) As VisualBasicParseOptions
            If documentationMode = Me.DocumentationMode Then
                Return Me
            End If

            Return New VisualBasicParseOptions(Me) With {.DocumentationMode = documentationMode}
        End Function

        ''' <summary>
        ''' Returns a VisualBasicParseOptions instance for a specified collection of KeyValuePairs representing pre-processor symbols.
        ''' </summary>
        ''' <param name="symbols">A collection representing pre-processor symbols</param>
        ''' <returns>A new instance of VisualBasicParseOptions.</returns>
        Public Shadows Function WithPreprocessorSymbols(symbols As IEnumerable(Of KeyValuePair(Of String, Object))) As VisualBasicParseOptions
            Return WithPreprocessorSymbols(symbols.AsImmutableOrNull())
        End Function

        ''' <summary>
        ''' Returns a VisualBasicParseOptions instance for a specified collection of KeyValuePairs representing pre-processor symbols.
        ''' </summary>
        ''' <param name="symbols">An parameter array of KeyValuePair representing pre-processor symbols.</param>
        ''' <returns>A new instance of VisualBasicParseOptions.</returns>
        Public Shadows Function WithPreprocessorSymbols(ParamArray symbols As KeyValuePair(Of String, Object)()) As VisualBasicParseOptions
            Return WithPreprocessorSymbols(symbols.AsImmutableOrNull())
        End Function

        ''' <summary>
        ''' Returns a VisualBasicParseOptions instance for a specified collection of KeyValuePairs representing pre-processor symbols.
        ''' </summary>
        ''' <param name="symbols">An ImmutableArray of KeyValuePair representing pre-processor symbols.</param>
        ''' <returns>A new instance of VisualBasicParseOptions.</returns>
        Public Shadows Function WithPreprocessorSymbols(symbols As ImmutableArray(Of KeyValuePair(Of String, Object))) As VisualBasicParseOptions
            If symbols.IsDefault Then
                symbols = ImmutableArray(Of KeyValuePair(Of String, Object)).Empty
            End If

            If symbols.Equals(Me.PreprocessorSymbols) Then
                Return Me
            End If

            Return New VisualBasicParseOptions(Me) With {._preprocessorSymbols = symbols}
        End Function

        ''' <summary>
        ''' Returns a ParseOptions instance for a specified Source Code Kind.
        ''' </summary>
        ''' <param name="kind">The parser source code kind.</param>
        ''' <returns>A new instance of ParseOptions.</returns>
        Public Overrides Function CommonWithKind(kind As SourceCodeKind) As ParseOptions
            Return WithKind(kind)
        End Function

        ''' <summary>
        ''' Returns a ParseOptions instance for a specified Documentation Mode.
        ''' </summary>
        ''' <param name="documentationMode">The documentation mode.</param>
        ''' <returns>A new instance of ParseOptions.</returns>
        Protected Overrides Function CommonWithDocumentationMode(documentationMode As DocumentationMode) As ParseOptions
            Return WithDocumentationMode(documentationMode)
        End Function

        Protected Overrides Function CommonWithFeatures(features As IEnumerable(Of KeyValuePair(Of String, String))) As ParseOptions
            Return WithFeatures(features)
        End Function

        ''' <summary>
        ''' Enable some experimental language features for testing.
        ''' </summary>
        Public Shadows Function WithFeatures(features As IEnumerable(Of KeyValuePair(Of String, String))) As VisualBasicParseOptions
            ' there are currently no parse options for experimental features
            If features Is Nothing Then
                Return New VisualBasicParseOptions(Me) With {._features = ImmutableDictionary(Of String, String).Empty}
            Else
                Return New VisualBasicParseOptions(Me) With {._features = features.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase)}
            End If
        End Function

        Public Overrides ReadOnly Property Features As IReadOnlyDictionary(Of String, String)
            Get
                Return _features
            End Get
        End Property

        Friend Overrides Sub ValidateOptions(builder As ArrayBuilder(Of Diagnostic))
            ValidateOptions(builder, MessageProvider.Instance)

            ' Validate LanguageVersion Not SpecifiedLanguageVersion, after Latest/Default has been converted
            If Not LanguageVersion.IsValid Then
                builder.Add(Diagnostic.Create(MessageProvider.Instance, ERRID.ERR_BadLanguageVersion, LanguageVersion.ToString))
            End If

            If Not PreprocessorSymbols.IsDefaultOrEmpty Then
                For Each symbol In PreprocessorSymbols
                    If Not IsValidIdentifier(symbol.Key) OrElse SyntaxFacts.GetKeywordKind(symbol.Key) <> SyntaxKind.None Then
                        builder.Add(Diagnostic.Create(ErrorFactory.ErrorInfo(ERRID.ERR_ConditionalCompilationConstantNotValid,
                                                                             ErrorFactory.ErrorInfo(ERRID.ERR_ExpectedIdentifier),
                                                                             symbol.Key)))
                    Else
                        Debug.Assert(SyntaxFactory.ParseTokens(symbol.Key).Select(Function(t) t.Kind).SequenceEqual({SyntaxKind.IdentifierToken, SyntaxKind.EndOfFileToken}))
                    End If

                    If InternalSyntax.CConst.TryCreate(symbol.Value) Is Nothing Then
                        builder.Add(Diagnostic.Create(MessageProvider.Instance, ERRID.ERR_InvalidPreprocessorConstantType, symbol.Key, symbol.Value.GetType))
                    End If
                Next
            End If
        End Sub

        ''' <summary>
        ''' Determines whether the current object is equal to another object of the same type.
        ''' </summary>
        ''' <param name="other">An VisualBasicParseOptions object to compare with this object</param>
        ''' <returns>A boolean value.  True if the current object is equal to the other parameter; otherwise, False.</returns>
        Public Overloads Function Equals(other As VisualBasicParseOptions) As Boolean Implements IEquatable(Of VisualBasicParseOptions).Equals
            If Me Is other Then
                Return True
            End If

            If Not MyBase.EqualsHelper(other) Then
                Return False
            End If

            If Me.SpecifiedLanguageVersion <> other.SpecifiedLanguageVersion Then
                Return False
            End If

            If Not Me.PreprocessorSymbols.SequenceEqual(other.PreprocessorSymbols) Then
                Return False
            End If

            Return True
        End Function

        ''' <summary>
        ''' Indicates whether the current object is equal to another object.
        ''' </summary>
        ''' <param name="obj">An object to compare with this object</param>
        ''' <returns>A boolean value.  True if the current object is equal to the other parameter; otherwise, False.</returns>
        Public Overrides Function Equals(obj As Object) As Boolean
            Return Equals(TryCast(obj, VisualBasicParseOptions))
        End Function

        ''' <summary>
        ''' Returns a hashcode for this instance.
        ''' </summary>
        ''' <returns>A hashcode representing this instance.</returns>
        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(MyBase.GetHashCodeHelper(), CInt(Me.SpecifiedLanguageVersion))
        End Function
    End Class
End Namespace
