' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' A class representing Visual Basic compilation Options.
    ''' </summary>
    Public NotInheritable Class VBCompilationOptions
        Inherits CompilationOptions
        Implements IEquatable(Of VBCompilationOptions)

        Private Const GlobalImportsString = "GlobalImports"
        Private Const RootNamespaceString = "RootNamespace"
        Private Const OptionStrictString = "OptionStrict"
        Private Const OptionInferString = "OptionInfer"
        Private Const OptionExplicitString = "OptionExplicit"
        Private Const OptionCompareTextString = "OptionCompareText"
        Private Const EmbedVbCoreRuntimeString = "EmbedVbCoreRuntime"
        Private Const ParseOptionsString = "ParseOptions"

        Private _globalImports As ImmutableArray(Of GlobalImport)
        Private _rootNamespace As String
        Private _optionStrict As OptionStrict
        Private _optionInfer As Boolean
        Private _optionExplicit As Boolean
        Private _optionCompareText As Boolean
        Private _embedVbCoreRuntime As Boolean
        Private _parseOptions As VBParseOptions

        ''' <summary>
        ''' Initializes a new instance of the VBCompilationOptions type with various options.
        ''' </summary>
        ''' <param name="outputKind">The compilation output kind. <see cref="CodeAnalysis.OutputKind"/></param>
        ''' <param name="moduleName">An optional parameter to specify the name of the assembly that this module will be a part of.</param>
        ''' <param name="mainTypeName">An optional parameter to specify the class or module that contains the Sub Main procedure.</param>
        ''' <param name="scriptClassName">An optional parameter to specify an alteranate DefaultScriptClassName object to be used.</param>
        ''' <param name="globalImports">An optional collection of GlobalImports <see cref="GlobalImports"/> .</param>
        ''' <param name="rootNamespace">An optional parameter to specify the name of the default root namespace.</param>
        ''' <param name="optionStrict">An optional parameter to specify the default Option Strict behavior. <see cref="VisualBasic.OptionStrict"/></param>
        ''' <param name="optionInfer">An optional parameter to specify default Option Infer behavior.</param>
        ''' <param name="optionExplicit">An optional parameter to specify the default Option Explicit behavior.</param>
        ''' <param name="optionCompareText">An optional parameter to specify the default Option Compare Text behavior.</param>
        ''' <param name="embedVbCoreRuntime">An optional parameter to specify the embedded Visual Basic Core Runtime behavior.</param>
        ''' <param name="checkOverflow">An optional parameter to specify enabling/disabling overflow checking.</param>
        ''' <param name="concurrentBuild">An optional parameter to specify enabling/disabling concurrent build.</param>
        ''' <param name="cryptoKeyContainer">An optional parameter to specify a key container name for a key pair to give an assembly a strong name.</param>
        ''' <param name="cryptoKeyFile">An optional parameter to specify a file containing a key or key pair to give an assembly a strong name.</param>
        ''' <param name="delaySign">An optional parameter to specify whether the assembly will be fully or partially signed.</param>
        ''' <param name="platform">An optional parameter to specify which platform version of common language runtime (CLR) can run compilation. <see cref="CodeAnalysis.Platform"/></param>
        ''' <param name="generalDiagnosticOption">An optional parameter to specify the general warning level.</param>
        ''' <param name="specificDiagnosticOptions">An optional collection representing specific warnings that differ from general warning behavior.</param>
        ''' <param name="optimizationLevel">An optional parameter to enabled/disable optimizations. </param>
        ''' <param name="parseOptions">An optional parameter to specify the parse options. <see cref="VBParseOptions"/></param>
        ''' <param name="xmlReferenceResolver">An optional parameter to specify the XML file resolver.</param>
        ''' <param name="sourceReferenceResolver">An optional parameter to specify the source file resolver.</param>
        ''' <param name="metadataReferenceResolver">An optional parameter to specify <see cref="CodeAnalysis.MetadataReferenceResolver"/>.</param>
        ''' <param name="assemblyIdentityComparer">An optional parameter to specify <see cref="CodeAnalysis.AssemblyIdentityComparer"/>.</param>
        ''' <param name="strongNameProvider">An optional parameter to specify <see cref="CodeAnalysis.StrongNameProvider"/>.</param>
        Public Sub New(
            outputKind As OutputKind,
            Optional moduleName As String = Nothing,
            Optional mainTypeName As String = Nothing,
            Optional scriptClassName As String = WellKnownMemberNames.DefaultScriptClassName,
            Optional globalImports As IEnumerable(Of GlobalImport) = Nothing,
            Optional rootNamespace As String = Nothing,
            Optional optionStrict As OptionStrict = OptionStrict.Off,
            Optional optionInfer As Boolean = True,
            Optional optionExplicit As Boolean = True,
            Optional optionCompareText As Boolean = False,
            Optional parseOptions As VBParseOptions = Nothing,
            Optional embedVbCoreRuntime As Boolean = False,
            Optional optimizationLevel As OptimizationLevel = OptimizationLevel.Debug,
            Optional checkOverflow As Boolean = True,
            Optional cryptoKeyContainer As String = Nothing,
            Optional cryptoKeyFile As String = Nothing,
            Optional delaySign As Boolean? = Nothing,
            Optional platform As Platform = Platform.AnyCpu,
            Optional generalDiagnosticOption As ReportDiagnostic = ReportDiagnostic.Default,
            Optional specificDiagnosticOptions As IEnumerable(Of KeyValuePair(Of String, ReportDiagnostic)) = Nothing,
            Optional concurrentBuild As Boolean = True,
            Optional xmlReferenceResolver As XmlReferenceResolver = Nothing,
            Optional sourceReferenceResolver As SourceReferenceResolver = Nothing,
            Optional metadataReferenceResolver As MetadataReferenceResolver = Nothing,
            Optional assemblyIdentityComparer As AssemblyIdentityComparer = Nothing,
            Optional strongNameProvider As StrongNameProvider = Nothing)

            MyClass.New(
                outputKind,
                moduleName,
                mainTypeName,
                scriptClassName,
                globalImports,
                rootNamespace,
                optionStrict,
                optionInfer,
                optionExplicit,
                optionCompareText,
                parseOptions,
                embedVbCoreRuntime,
                optimizationLevel,
                checkOverflow,
                cryptoKeyContainer,
                cryptoKeyFile,
                delaySign,
                platform,
                generalDiagnosticOption,
                specificDiagnosticOptions,
                concurrentBuild,
                xmlReferenceResolver,
                sourceReferenceResolver,
                metadataReferenceResolver,
                assemblyIdentityComparer,
                strongNameProvider,
                MetadataImportOptions.Public,
                features:=ImmutableArray(Of String).Empty)

        End Sub

        Friend Sub New(
            outputKind As OutputKind,
            moduleName As String,
            mainTypeName As String,
            scriptClassName As String,
            globalImports As IEnumerable(Of GlobalImport),
            rootNamespace As String,
            optionStrict As OptionStrict,
            optionInfer As Boolean,
            optionExplicit As Boolean,
            optionCompareText As Boolean,
            parseOptions As VBParseOptions,
            embedVbCoreRuntime As Boolean,
            optimizationLevel As OptimizationLevel,
            checkOverflow As Boolean,
            cryptoKeyContainer As String,
            cryptoKeyFile As String,
            delaySign As Boolean?,
            platform As Platform,
            generalDiagnosticOption As ReportDiagnostic,
            specificDiagnosticOptions As IEnumerable(Of KeyValuePair(Of String, ReportDiagnostic)),
            concurrentBuild As Boolean,
            xmlReferenceResolver As XmlReferenceResolver,
            sourceReferenceResolver As SourceReferenceResolver,
            metadataReferenceResolver As MetadataReferenceResolver,
            assemblyIdentityComparer As AssemblyIdentityComparer,
            strongNameProvider As StrongNameProvider,
            metadataImportOptions As MetadataImportOptions,
            features As ImmutableArray(Of String))

            MyBase.New(
                outputKind:=outputKind,
                moduleName:=moduleName,
                mainTypeName:=mainTypeName,
                scriptClassName:=scriptClassName,
                cryptoKeyContainer:=cryptoKeyContainer,
                cryptoKeyFile:=cryptoKeyFile,
                delaySign:=delaySign,
                optimizationLevel:=optimizationLevel,
                checkOverflow:=checkOverflow,
                platform:=platform,
                generalDiagnosticOption:=generalDiagnosticOption,
                warningLevel:=1,
                specificDiagnosticOptions:=specificDiagnosticOptions.ToImmutableDictionaryOrEmpty(CaseInsensitiveComparison.Comparer), ' Diagnostic ids must be processed in case-insensitive fashion.
                concurrentBuild:=concurrentBuild,
                xmlReferenceResolver:=xmlReferenceResolver,
                sourceReferenceResolver:=sourceReferenceResolver,
                metadataReferenceResolver:=metadataReferenceResolver,
                assemblyIdentityComparer:=assemblyIdentityComparer,
                strongNameProvider:=strongNameProvider,
                metadataImportOptions:=metadataImportOptions,
                features:=features)

            _globalImports = globalImports.AsImmutableOrEmpty()
            _rootNamespace = If(rootNamespace, String.Empty)
            _optionStrict = optionStrict
            _optionInfer = optionInfer
            _optionExplicit = optionExplicit
            _optionCompareText = optionCompareText
            _embedVbCoreRuntime = embedVbCoreRuntime
            _parseOptions = parseOptions
        End Sub

        Private Sub New(other As VBCompilationOptions)
            MyClass.New(
                outputKind:=other.OutputKind,
                moduleName:=other.ModuleName,
                mainTypeName:=other.MainTypeName,
                scriptClassName:=other.ScriptClassName,
                globalImports:=other.GlobalImports,
                rootNamespace:=other.RootNamespace,
                optionStrict:=other.OptionStrict,
                optionInfer:=other.OptionInfer,
                optionExplicit:=other.OptionExplicit,
                optionCompareText:=other.OptionCompareText,
                parseOptions:=other.ParseOptions,
                embedVbCoreRuntime:=other.EmbedVbCoreRuntime,
                optimizationLevel:=other.OptimizationLevel,
                checkOverflow:=other.CheckOverflow,
                cryptoKeyContainer:=other.CryptoKeyContainer,
                cryptoKeyFile:=other.CryptoKeyFile,
                delaySign:=other.DelaySign,
                platform:=other.Platform,
                generalDiagnosticOption:=other.GeneralDiagnosticOption,
                specificDiagnosticOptions:=other.SpecificDiagnosticOptions,
                concurrentBuild:=other.ConcurrentBuild,
                xmlReferenceResolver:=other.XmlReferenceResolver,
                sourceReferenceResolver:=other.SourceReferenceResolver,
                metadataReferenceResolver:=other.MetadataReferenceResolver,
                assemblyIdentityComparer:=other.AssemblyIdentityComparer,
                strongNameProvider:=other.StrongNameProvider,
                metadataImportOptions:=other.MetadataImportOptions,
                features:=other.Features)
        End Sub

        ''' <summary>
        ''' Gets the global imports collection.
        ''' </summary>
        ''' <returns>The global imports.</returns>
        Public ReadOnly Property GlobalImports As ImmutableArray(Of GlobalImport)
            Get
                Return _globalImports
            End Get
        End Property

        ''' <summary>
        ''' Gets the default namespace for all source code in the project. Corresponds to the 
        ''' "RootNamespace" project option or the "/rootnamespace" command line option.
        ''' </summary>
        ''' <returns>The default namespace.</returns>
        Public ReadOnly Property RootNamespace As String
            Get
                Return _rootNamespace
            End Get
        End Property

        Friend Function GetRootNamespaceParts() As ImmutableArray(Of String)
            If String.IsNullOrEmpty(_rootNamespace) OrElse Not OptionsValidator.IsValidNamespaceName(_rootNamespace) Then
                Return ImmutableArray(Of String).Empty
            End If

            Return MetadataHelpers.SplitQualifiedName(_rootNamespace)
        End Function

        ''' <summary>
        ''' Gets the Option Strict Setting.
        ''' </summary>
        ''' <returns>The Option Strict setting.</returns>
        Public ReadOnly Property OptionStrict As OptionStrict
            Get
                Return _optionStrict
            End Get
        End Property

        ''' <summary>
        ''' Gets the Option Infer setting.
        ''' </summary>
        ''' <returns>The Option Infer setting. True if Option Infer On is in effect by default. False if Option Infer Off is on effect by default. </returns>
        Public ReadOnly Property OptionInfer As Boolean
            Get
                Return _optionInfer
            End Get
        End Property

        ''' <summary>
        ''' Gets the Option Explicit setting.
        ''' </summary>
        ''' <returns>The Option Explicit setting. True if Option Explicit On is in effect by default. False if Option Explicit Off is on by default.</returns>
        Public ReadOnly Property OptionExplicit As Boolean
            Get
                Return _optionExplicit
            End Get
        End Property

        ''' <summary>
        ''' Gets the Option Compare Text setting.
        ''' </summary>
        ''' <returns>        
        ''' The Option Compare Text Setting, True if Option Compare Text is in effect by default. False if Option Compare Binary is
        ''' in effect by default.
        ''' </returns>
        Public ReadOnly Property OptionCompareText As Boolean
            Get
                Return _optionCompareText
            End Get
        End Property

        ''' <summary>
        ''' Gets the Embed Visual Basic Core Runtime setting.
        ''' </summary>
        ''' <returns>
        ''' The EmbedVbCoreRuntime setting, True if VB core runtime should be embedded in the compilation. Equal to '/vbruntime*'
        ''' </returns>
        Public ReadOnly Property EmbedVbCoreRuntime As Boolean
            Get
                Return _embedVbCoreRuntime
            End Get
        End Property

        ''' <summary>
        ''' Gets the Parse Options setting.
        ''' Compilation level parse options.  Used when compiling synthetic embedded code such as My template
        ''' </summary>
        ''' <returns>The Parse Options Setting.</returns>
        Friend ReadOnly Property ParseOptions As VBParseOptions
            Get
                Return _parseOptions
            End Get
        End Property

        ''' <summary>
        ''' Creates a new VBCompilationOptions instance with a different OutputKind specified.
        ''' </summary>
        ''' <param name="kind">The Output Kind.</param>
        ''' <returns>A new instance of VBCompilationOptions, if the output kind is different; otherwise current instance.</returns>        
        Public Shadows Function WithOutputKind(kind As OutputKind) As VBCompilationOptions
            If kind = Me.OutputKind Then
                Return Me
            End If

            Return New VBCompilationOptions(Me) With {.OutputKind = kind}
        End Function

        ''' <summary>
        ''' Creates a new VBCompilationOptions instance With a different ModuleName specified.
        ''' </summary>
        ''' <param name="moduleName">The moduleName.</param>        
        ''' <returns>A new instance of VBCompilationOptions, if the module name is different; otherwise current instance.</returns>        
        Public Function WithModuleName(moduleName As String) As VBCompilationOptions
            If String.Equals(moduleName, Me.ModuleName, StringComparison.Ordinal) Then
                Return Me
            End If

            Return New VBCompilationOptions(Me) With {.ModuleName = moduleName}
        End Function

        ''' <summary>
        ''' Creates a new VBCompilationOptions instance with a Script Class Name specified.
        ''' </summary>
        ''' <param name="name">The name for the ScriptClassName.</param>        
        ''' <returns>A new instance of VBCompilationOptions, if the script class name is different; otherwise current instance.</returns>        
        Public Shadows Function WithScriptClassName(name As String) As VBCompilationOptions
            If String.Equals(name, Me.ScriptClassName, StringComparison.Ordinal) Then
                Return Me
            End If

            Return New VBCompilationOptions(Me) With {.ScriptClassName = name}
        End Function

        ''' <summary>
        ''' Creates a new VBCompilationOptions instance with a different Main Type name specified.
        ''' </summary>
        ''' <param name="name">The name for the MainType .</param>        
        ''' <returns>A new instance of VBCompilationOptions, if the main type name is different; otherwise current instance.</returns>        
        Public Shadows Function WithMainTypeName(name As String) As VBCompilationOptions
            If String.Equals(name, Me.MainTypeName, StringComparison.Ordinal) Then
                Return Me
            End If

            Return New VBCompilationOptions(Me) With {.MainTypeName = name}
        End Function

        ''' <summary>
        ''' Creates a new VBCompilationOptions instance with a different global imports specified.
        ''' </summary>
        ''' <param name="globalImports">A collection of Global Imports <see cref="GlobalImport"/>.</param>        
        ''' <returns>A new instance of VBCompilationOptions.</returns>        
        Public Function WithGlobalImports(globalImports As ImmutableArray(Of GlobalImport)) As VBCompilationOptions
            If Me.GlobalImports.Equals(globalImports) Then
                Return Me
            End If

            Return New VBCompilationOptions(Me) With {._globalImports = globalImports}
        End Function

        ''' <summary>
        ''' Creates a new VBCompilationOptions instance with a different global imports specified.
        ''' </summary>
        ''' <param name="globalImports">A collection of Global Imports <see cref="GlobalImport"/>.</param>        
        ''' <returns>A new instance of VBCompilationOptions.</returns>        
        Public Function WithGlobalImports(globalImports As IEnumerable(Of GlobalImport)) As VBCompilationOptions
            Return New VBCompilationOptions(Me) With {._globalImports = globalImports.AsImmutableOrEmpty()}
        End Function

        ''' <summary>
        ''' Creates a new VBCompilationOptions instance with a different global imports specified.
        ''' </summary>
        ''' <param name="globalImports">A collection of Global Imports <see cref="GlobalImport"/>.</param>        
        ''' <returns>A new instance of VBCompilationOptions.</returns>
        Public Function WithGlobalImports(ParamArray globalImports As GlobalImport()) As VBCompilationOptions
            Return WithGlobalImports(DirectCast(globalImports, IEnumerable(Of GlobalImport)))
        End Function

        ''' <summary>
        ''' Creates a new VBCompilationOptions instance with a different root namespace specified.
        ''' </summary>
        ''' <param name="rootNamespace">The root namespace.</param>        
        ''' <returns>A new instance of VBCompilationOptions, if the root namespace is different; otherwise current instance.</returns>        
        Public Function WithRootNamespace(rootNamespace As String) As VBCompilationOptions
            If String.Equals(rootNamespace, Me.RootNamespace, StringComparison.Ordinal) Then
                Return Me
            End If

            Return New VBCompilationOptions(Me) With {._rootNamespace = rootNamespace}
        End Function

        ''' <summary>
        ''' Creates a new VBCompilationOptions instance with a different option strict specified.
        ''' </summary>
        ''' <param name="value">The Option Strict setting.  <see cref="Microsoft.CodeAnalysis.VisualBasic.OptionStrict"/></param>        
        ''' <returns>A new instance of VBCompilationOptions, if the option strict is different; otherwise current instance.</returns>        
        Public Shadows Function WithOptionStrict(value As OptionStrict) As VBCompilationOptions
            If value = Me.OptionStrict Then
                Return Me
            End If

            Return New VBCompilationOptions(Me) With {._optionStrict = value}
        End Function

        ''' <summary>
        ''' Creates a new VBCompilationOptions instance with a different option infer specified.
        ''' </summary>
        ''' <param name="value">The Option infer setting. </param>        
        ''' <returns>A new instance of VBCompilationOptions, if the option infer is different; otherwise current instance.</returns>        
        Public Shadows Function WithOptionInfer(value As Boolean) As VBCompilationOptions
            If value = Me.OptionInfer Then
                Return Me
            End If

            Return New VBCompilationOptions(Me) With {._optionInfer = value}
        End Function

        ''' <summary>
        ''' Creates a new VBCompilationOptions instance with a different option explicit specified.
        ''' </summary>
        ''' <param name="value">The Option Explicit setting. </param>        
        ''' <returns>A new instance of VBCompilationOptions, if the option explicit is different; otherwise current instance.</returns>        
        Public Shadows Function WithOptionExplicit(value As Boolean) As VBCompilationOptions
            If value = Me.OptionExplicit Then
                Return Me
            End If

            Return New VBCompilationOptions(Me) With {._optionExplicit = value}
        End Function

        ''' <summary>
        ''' Creates a new VBCompilationOptions instance with a different Option Compare Text specified.
        ''' </summary>
        ''' <param name="value">The Option Compare Text setting. </param>        
        ''' <returns>A new instance of VBCompilationOptions, if the option compare text is different; otherwise current instance.</returns>        
        Public Shadows Function WithOptionCompareText(value As Boolean) As VBCompilationOptions
            If value = Me.OptionCompareText Then
                Return Me
            End If

            Return New VBCompilationOptions(Me) With {._optionCompareText = value}
        End Function

        ''' <summary>
        ''' Creates a new VBCompilationOptions instance with a different Embed VB Core Runtime specified.
        ''' </summary>
        ''' <param name="value">The Embed VB Core Runtime setting. </param>        
        ''' <returns>A new instance of VBCompilationOptions, if the embed vb core runtime is different; otherwise current instance.</returns>        
        Public Shadows Function WithEmbedVbCoreRuntime(value As Boolean) As VBCompilationOptions
            If value = Me.EmbedVbCoreRuntime Then
                Return Me
            End If

            Return New VBCompilationOptions(Me) With {._embedVbCoreRuntime = value}
        End Function

        ''' <summary>
        ''' Creates a new VBCompilationOptions instance with a different Overflow checks specified.
        ''' </summary>
        ''' <param name="enabled">The overflow check setting. </param>        
        ''' <returns>A new instance of VBCompilationOptions, if the overflow check is different; otherwise current instance.</returns>        
        Public Shadows Function WithOverflowChecks(enabled As Boolean) As VBCompilationOptions
            If enabled = Me.CheckOverflow Then
                Return Me
            End If

            Return New VBCompilationOptions(Me) With {.CheckOverflow = enabled}
        End Function

        ''' <summary>
        ''' Creates a new VBCompilationOptions instance with a different concurrent build specified.
        ''' </summary>
        ''' <param name="concurrentBuild">The concurrent build setting. </param>        
        ''' <returns>A new instance of VBCompilationOptions, if the concurrent build is different; otherwise current instance.</returns>        
        Public Shadows Function WithConcurrentBuild(concurrentBuild As Boolean) As VBCompilationOptions
            If concurrentBuild = Me.ConcurrentBuild Then
                Return Me
            End If

            Return New VBCompilationOptions(Me) With {.ConcurrentBuild = concurrentBuild}
        End Function

        ''' <summary>
        ''' Creates a new VBCompilationOptions instance with a different cryptography key container specified
        ''' </summary>
        ''' <param name="name">The name of the cryptography key container. </param>        
        ''' <returns>A new instance of VBCompilationOptions, if the cryptography key container name is different; otherwise current instance.</returns>        
        Public Shadows Function WithCryptoKeyContainer(name As String) As VBCompilationOptions
            If String.Equals(name, Me.CryptoKeyContainer, StringComparison.Ordinal) Then
                Return Me
            End If

            Return New VBCompilationOptions(Me) With {.CryptoKeyContainer = name}
        End Function

        ''' <summary>
        ''' Creates a new VBCompilationOptions instance with a different cryptography key file path specified.
        ''' </summary>
        ''' <param name="path">The cryptography key file path. </param>        
        ''' <returns>A new instance of VBCompilationOptions, if the cryptography key path is different; otherwise current instance.</returns>        
        Public Shadows Function WithCryptoKeyFile(path As String) As VBCompilationOptions
            If String.Equals(path, Me.CryptoKeyFile, StringComparison.Ordinal) Then
                Return Me
            End If

            Return New VBCompilationOptions(Me) With {.CryptoKeyFile = path}
        End Function

        ''' <summary>
        ''' Creates a new VBCompilationOptions instance with a different delay signing specified.
        ''' </summary>
        ''' <param name="value">The delay signing setting. </param>        
        ''' <returns>A new instance of VBCompilationOptions, if the delay sign is different; otherwise current instance.</returns>        
        Public Shadows Function WithDelaySign(value As Boolean?) As VBCompilationOptions
            If value = Me.DelaySign Then
                Return Me
            End If

            Return New VBCompilationOptions(Me) With {.DelaySign = value}
        End Function

        ''' <summary>
        ''' Creates a new <see cref="VBCompilationOptions"/> instance with a different platform specified.
        ''' </summary>
        ''' <param name="value">The platform setting. <see cref="Microsoft.CodeAnalysis.Platform"/></param>        
        ''' <returns>A new instance of VBCompilationOptions, if the platform is different; otherwise current instance.</returns>        
        Public Shadows Function WithPlatform(value As Platform) As VBCompilationOptions
            If value = Me.Platform Then
                Return Me
            End If

            Return New VBCompilationOptions(Me) With {.Platform = value}
        End Function

        Friend Shadows Function WithFeatures(features As ImmutableArray(Of String)) As VBCompilationOptions
            If features = Me.Features Then
                Return Me
            End If

            Return New VBCompilationOptions(Me) With {.Features = features}
        End Function

        Protected Overrides Function CommonWithGeneralDiagnosticOption(value As ReportDiagnostic) As CompilationOptions
            Return Me.WithGeneralDiagnosticOption(value)
        End Function

        Protected Overrides Function CommonWithSpecificDiagnosticOptions(specificDiagnosticOptions As ImmutableDictionary(Of String, ReportDiagnostic)) As CompilationOptions
            Return Me.WithSpecificDiagnosticOptions(specificDiagnosticOptions)
        End Function

        Protected Overrides Function CommonWithSpecificDiagnosticOptions(specificDiagnosticOptions As IEnumerable(Of KeyValuePair(Of String, ReportDiagnostic))) As CompilationOptions
            Return Me.WithSpecificDiagnosticOptions(specificDiagnosticOptions)
        End Function

        Protected Overrides Function CommonWithFeatures(features As ImmutableArray(Of String)) As CompilationOptions
            Return Me.WithFeatures(features)
        End Function

        ''' <summary>
        ''' Creates a new <see cref="VBCompilationOptions"/> instance with a different report warning specified.
        ''' </summary>
        ''' <param name="value">The Report Warning setting. <see cref="Microsoft.CodeAnalysis.ReportDiagnostic"/></param>        
        ''' <returns>A new instance of VBCompilationOptions, if the report warning is different; otherwise current instance.</returns>        
        Public Shadows Function WithGeneralDiagnosticOption(value As ReportDiagnostic) As VBCompilationOptions
            If value = Me.GeneralDiagnosticOption Then
                Return Me
            End If

            Return New VBCompilationOptions(Me) With {.GeneralDiagnosticOption = value}
        End Function

        ''' <summary>
        ''' Creates a new <see cref="VBCompilationOptions"/> instance with different specific warnings specified.
        ''' </summary>
        ''' <param name="value">Specific report warnings. <see cref="Microsoft.CodeAnalysis.ReportDiagnostic"/></param>        
        ''' <returns>A new instance of VBCompilationOptions, if the dictionary of report warning is different; otherwise current instance.</returns>        
        Public Shadows Function WithSpecificDiagnosticOptions(value As ImmutableDictionary(Of String, ReportDiagnostic)) As VBCompilationOptions
            If value Is Nothing Then
                value = ImmutableDictionary(Of String, ReportDiagnostic).Empty
            End If

            If value Is Me.SpecificDiagnosticOptions Then
                Return Me
            End If

            Return New VBCompilationOptions(Me) With {.SpecificDiagnosticOptions = value}
        End Function

        ''' <summary>
        ''' Creates a new <see cref="VBCompilationOptions"/> instance with different specific warnings specified.
        ''' </summary>
        ''' <param name="value">Specific report warnings. <see cref="Microsoft.CodeAnalysis.ReportDiagnostic"/></param>        
        ''' <returns>A new instance of VBCompilationOptions, if the dictionary of report warning is different; otherwise current instance.</returns>        
        Public Shadows Function WithSpecificDiagnosticOptions(value As IEnumerable(Of KeyValuePair(Of String, ReportDiagnostic))) As VBCompilationOptions
            Return New VBCompilationOptions(Me) With {.SpecificDiagnosticOptions = value.ToImmutableDictionaryOrEmpty()}
        End Function

        ''' <summary>
        ''' Creates a new <see cref="VBCompilationOptions"/> instance with a specified <see cref="VBCompilationOptions.OptimizationLevel"/>.
        ''' </summary>
        ''' <returns>A new instance of <see cref="VBCompilationOptions"/>, if the value is different; otherwise the current instance.</returns>        
        Public Shadows Function WithOptimizationLevel(value As OptimizationLevel) As VBCompilationOptions
            If value = Me.OptimizationLevel Then
                Return Me
            End If

            Return New VBCompilationOptions(Me) With {.OptimizationLevel = value}
        End Function

        Friend Function WithMetadataImportOptions(value As MetadataImportOptions) As VBCompilationOptions
            If value = Me.MetadataImportOptions Then
                Return Me
            End If

            Return New VBCompilationOptions(Me) With {.MetadataImportOptions_internal_protected_set = value}
        End Function

        ''' <summary>
        ''' Creates a new <see cref="VBCompilationOptions"/> instance with a different parse option specified.
        ''' </summary>
        ''' <param name="options">The parse option setting. <see cref="Microsoft.CodeAnalysis.VisualBasic.VBParseOptions"/></param>        
        ''' <returns>A new instance of VBCompilationOptions, if the parse options is different; otherwise current instance.</returns>        
        Public Function WithParseOptions(options As VBParseOptions) As VBCompilationOptions
            If options Is Me.ParseOptions Then
                Return Me
            End If

            Return New VBCompilationOptions(Me) With {._parseOptions = options}
        End Function

        Public Shadows Function WithXmlReferenceResolver(resolver As XmlReferenceResolver) As VBCompilationOptions
            If resolver Is Me.XmlReferenceResolver Then
                Return Me
            End If

            Return New VBCompilationOptions(Me) With {.XmlReferenceResolver = resolver}
        End Function

        Public Shadows Function WithSourceReferenceResolver(resolver As SourceReferenceResolver) As VBCompilationOptions
            If resolver Is Me.SourceReferenceResolver Then
                Return Me
            End If

            Return New VBCompilationOptions(Me) With {.SourceReferenceResolver = resolver}
        End Function

        Public Shadows Function WithMetadataReferenceResolver(resolver As MetadataReferenceResolver) As VBCompilationOptions
            If resolver Is Me.MetadataReferenceResolver Then
                Return Me
            End If

            Return New VBCompilationOptions(Me) With {.MetadataReferenceResolver = resolver}
        End Function

        Public Shadows Function WithAssemblyIdentityComparer(comparer As AssemblyIdentityComparer) As VBCompilationOptions
            comparer = If(comparer, AssemblyIdentityComparer.Default)

            If comparer Is Me.AssemblyIdentityComparer Then
                Return Me
            End If

            Return New VBCompilationOptions(Me) With {.AssemblyIdentityComparer = comparer}
        End Function

        Public Shadows Function WithStrongNameProvider(provider As StrongNameProvider) As VBCompilationOptions
            If provider Is Me.StrongNameProvider Then
                Return Me
            End If

            Return New VBCompilationOptions(Me) With {.StrongNameProvider = provider}
        End Function

        Protected Overrides Function CommonWithOutputKind(kind As OutputKind) As CompilationOptions
            Return WithOutputKind(kind)
        End Function

        Protected Overrides Function CommonWithPlatform(platform As Platform) As CompilationOptions
            Return WithPlatform(platform)
        End Function

        Protected Overrides Function CommonWithOptimizationLevel(value As OptimizationLevel) As CompilationOptions
            Return WithOptimizationLevel(value)
        End Function

        Protected Overrides Function CommonWithAssemblyIdentityComparer(comparer As AssemblyIdentityComparer) As CompilationOptions
            Return WithAssemblyIdentityComparer(comparer)
        End Function

        Protected Overrides Function CommonWithXmlReferenceResolver(resolver As XmlReferenceResolver) As CompilationOptions
            Return WithXmlReferenceResolver(resolver)
        End Function

        Protected Overrides Function CommonWithSourceReferenceResolver(resolver As SourceReferenceResolver) As CompilationOptions
            Return WithSourceReferenceResolver(resolver)
        End Function

        Protected Overrides Function CommonWithMetadataReferenceResolver(resolver As MetadataReferenceResolver) As CompilationOptions
            Return WithMetadataReferenceResolver(resolver)
        End Function

        Protected Overrides Function CommonWithStrongNameProvider(provider As StrongNameProvider) As CompilationOptions
            Return WithStrongNameProvider(provider)
        End Function

        Friend Overrides Sub ValidateOptions(builder As ArrayBuilder(Of Diagnostic))
            If Me.EmbedVbCoreRuntime AndAlso Me.OutputKind.IsNetModule() Then
                builder.Add(Diagnostic.Create(MessageProvider.Instance, ERRID.ERR_VBCoreNetModuleConflict))
            End If

            If Not Platform.IsValid() Then
                builder.Add(Diagnostic.Create(MessageProvider.Instance, ERRID.ERR_InvalidSwitchValue, Platform.ToString(), "Platform"))
            End If

            If ModuleName IsNot Nothing Then
                Dim e As Exception = MetadataHelpers.CheckAssemblyOrModuleName(ModuleName, "ModuleName")
                If e IsNot Nothing Then
                    builder.Add(Diagnostic.Create(MessageProvider.Instance, ERRID.ERR_BadCompilationOption, e.Message))
                End If
            End If

            If Not OutputKind.IsValid() Then
                builder.Add(Diagnostic.Create(MessageProvider.Instance, ERRID.ERR_InvalidSwitchValue, OutputKind.ToString(), "OutputKind"))
            End If

            If Not OptimizationLevel.IsValid() Then
                builder.Add(Diagnostic.Create(MessageProvider.Instance, ERRID.ERR_InvalidSwitchValue, OptimizationLevel.ToString(), "OptimizationLevel"))
            End If

            If ScriptClassName Is Nothing OrElse Not ScriptClassName.IsValidClrTypeName() Then
                builder.Add(Diagnostic.Create(MessageProvider.Instance, ERRID.ERR_InvalidSwitchValue, If(ScriptClassName, "Nothing"), "ScriptClassName"))
            End If

            If MainTypeName IsNot Nothing AndAlso Not MainTypeName.IsValidClrTypeName() Then
                builder.Add(Diagnostic.Create(MessageProvider.Instance, ERRID.ERR_InvalidSwitchValue, MainTypeName, "MainTypeName"))
            End If

            If Not String.IsNullOrEmpty(RootNamespace) AndAlso Not OptionsValidator.IsValidNamespaceName(RootNamespace) Then
                builder.Add(Diagnostic.Create(MessageProvider.Instance, ERRID.ERR_InvalidSwitchValue, RootNamespace, "RootNamespace"))
            End If

            If Not OptionStrict.IsValid Then
                builder.Add(Diagnostic.Create(MessageProvider.Instance, ERRID.ERR_InvalidSwitchValue, OptionStrict.ToString(), "OptionStrict"))
            End If

            If Platform = Platform.AnyCpu32BitPreferred AndAlso OutputKind.IsValid() AndAlso
                 Not (OutputKind = OutputKind.ConsoleApplication OrElse OutputKind = OutputKind.WindowsApplication OrElse OutputKind = OutputKind.WindowsRuntimeApplication) Then
                builder.Add(Diagnostic.Create(MessageProvider.Instance, ERRID.ERR_LibAnycpu32bitPreferredConflict, Platform.ToString(), "Platform"))
            End If

            ' TODO: add check for 
            '          (kind == 'arm' || kind == 'appcontainer' || kind == 'winmdobj') &&
            '          (version >= "6.2")
        End Sub

        ''' <summary>
        ''' Determines whether the current object is equal to another object of the same type.
        ''' </summary>
        ''' <param name="other">A VBCompilationOptions to compare with this object</param>
        ''' <returns>A boolean value.  True if the current object is equal to the other parameter; otherwise, False.</returns>
        Public Overloads Function Equals(other As VBCompilationOptions) As Boolean Implements IEquatable(Of VBCompilationOptions).Equals
            If Me Is other Then
                Return True
            End If

            If Not MyBase.EqualsHelper(other) Then
                Return False
            End If

            Return If(Me.GlobalImports.IsDefault, other.GlobalImports.IsDefault, Me.GlobalImports.SequenceEqual(other.GlobalImports)) AndAlso
                   String.Equals(Me.RootNamespace, other.RootNamespace, StringComparison.Ordinal) AndAlso
                   Me.OptionStrict = other.OptionStrict AndAlso
                   Me.OptionInfer = other.OptionInfer AndAlso
                   Me.OptionExplicit = other.OptionExplicit AndAlso
                   Me.OptionCompareText = other.OptionCompareText AndAlso
                   Me.EmbedVbCoreRuntime = other.EmbedVbCoreRuntime AndAlso
                   If(Me.ParseOptions Is Nothing, other.ParseOptions Is Nothing, Me.ParseOptions.Equals(other.ParseOptions))
        End Function

        ''' <summary>
        ''' Indicates whether the current object is equal to another object.
        ''' </summary>
        ''' <param name="obj">A object to compare with this object</param>
        ''' <returns>A boolean value.  True if the current object is equal to the other parameter; otherwise, False.</returns>
        Public Overrides Function Equals(obj As Object) As Boolean
            Return Me.Equals(TryCast(obj, VBCompilationOptions))
        End Function


        ''' <summary>
        ''' Creates a hashcode for this instance.
        ''' </summary>
        ''' <returns>A hashcode representing this instance.</returns>
        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(MyBase.GetHashCodeHelper(),
                   Hash.Combine(Hash.CombineValues(Me.GlobalImports),
                   Hash.Combine(If(Me.RootNamespace IsNot Nothing, StringComparer.Ordinal.GetHashCode(Me.RootNamespace), 0),
                   Hash.Combine(Me.OptionStrict,
                   Hash.Combine(Me.OptionInfer,
                   Hash.Combine(Me.OptionExplicit,
                   Hash.Combine(Me.OptionCompareText,
                   Hash.Combine(Me.EmbedVbCoreRuntime,
                   Hash.Combine(Me.ParseOptions, 0)))))))))
        End Function
    End Class
End Namespace
