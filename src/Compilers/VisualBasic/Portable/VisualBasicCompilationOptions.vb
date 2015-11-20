' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' A class representing Visual Basic compilation Options.
    ''' </summary>
    Public NotInheritable Class VisualBasicCompilationOptions
        Inherits CompilationOptions
        Implements IEquatable(Of VisualBasicCompilationOptions)

        Private Const s_globalImportsString = "GlobalImports"
        Private Const s_rootNamespaceString = "RootNamespace"
        Private Const s_optionStrictString = "OptionStrict"
        Private Const s_optionInferString = "OptionInfer"
        Private Const s_optionExplicitString = "OptionExplicit"
        Private Const s_optionCompareTextString = "OptionCompareText"
        Private Const s_embedVbCoreRuntimeString = "EmbedVbCoreRuntime"
        Private Const s_parseOptionsString = "ParseOptions"

        Private _globalImports As ImmutableArray(Of GlobalImport)
        Private _rootNamespace As String
        Private _optionStrict As OptionStrict
        Private _optionInfer As Boolean
        Private _optionExplicit As Boolean
        Private _optionCompareText As Boolean
        Private _embedVbCoreRuntime As Boolean
        Private _parseOptions As VisualBasicParseOptions

        ' The assemblies emitted by the expression compiler should never contain embedded declarations -
        ' those should come from the user's code.
        Private _suppressEmbeddedDeclarations As Boolean

        ''' <summary>
        ''' Initializes a new instance of the VisualBasicCompilationOptions type with various options.
        ''' </summary>
        ''' <param name="outputKind">The compilation output kind. <see cref="CodeAnalysis.OutputKind"/></param>
        ''' <param name="moduleName">An optional parameter to specify the name of the assembly that this module will be a part of.</param>
        ''' <param name="mainTypeName">An optional parameter to specify the class or module that contains the Sub Main procedure.</param>
        ''' <param name="scriptClassName">An optional parameter to specify an alternate DefaultScriptClassName object to be used.</param>
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
        ''' <param name="cryptoPublicKey">An optional parameter to specify a public key used to give an assembly a strong name.</param>
        ''' <param name="delaySign">An optional parameter to specify whether the assembly will be fully or partially signed.</param>
        ''' <param name="platform">An optional parameter to specify which platform version of common language runtime (CLR) can run compilation. <see cref="CodeAnalysis.Platform"/></param>
        ''' <param name="generalDiagnosticOption">An optional parameter to specify the general warning level.</param>
        ''' <param name="specificDiagnosticOptions">An optional collection representing specific warnings that differ from general warning behavior.</param>
        ''' <param name="optimizationLevel">An optional parameter to enabled/disable optimizations. </param>
        ''' <param name="parseOptions">An optional parameter to specify the parse options. <see cref="VisualBasicParseOptions"/></param>
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
            Optional parseOptions As VisualBasicParseOptions = Nothing,
            Optional embedVbCoreRuntime As Boolean = False,
            Optional optimizationLevel As OptimizationLevel = OptimizationLevel.Debug,
            Optional checkOverflow As Boolean = True,
            Optional cryptoKeyContainer As String = Nothing,
            Optional cryptoKeyFile As String = Nothing,
            Optional cryptoPublicKey As ImmutableArray(Of Byte) = Nothing,
            Optional delaySign As Boolean? = Nothing,
            Optional platform As Platform = Platform.AnyCpu,
            Optional generalDiagnosticOption As ReportDiagnostic = ReportDiagnostic.Default,
            Optional specificDiagnosticOptions As IEnumerable(Of KeyValuePair(Of String, ReportDiagnostic)) = Nothing,
            Optional concurrentBuild As Boolean = True,
            Optional deterministic As Boolean = False, ' TODO(5431): Enable deterministic mode by default
            Optional xmlReferenceResolver As XmlReferenceResolver = Nothing,
            Optional sourceReferenceResolver As SourceReferenceResolver = Nothing,
            Optional metadataReferenceResolver As MetadataReferenceResolver = Nothing,
            Optional assemblyIdentityComparer As AssemblyIdentityComparer = Nothing,
            Optional strongNameProvider As StrongNameProvider = Nothing)

            MyClass.New(
                outputKind,
                False,
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
                cryptoPublicKey,
                delaySign,
                platform,
                generalDiagnosticOption,
                specificDiagnosticOptions,
                concurrentBuild,
                deterministic:=False,' TODO: fix this
                suppressEmbeddedDeclarations:=False,
                extendedCustomDebugInformation:=True,
                debugPlusMode:=False,
                xmlReferenceResolver:=xmlReferenceResolver,
                sourceReferenceResolver:=sourceReferenceResolver,
                metadataReferenceResolver:=metadataReferenceResolver,
                assemblyIdentityComparer:=assemblyIdentityComparer,
                strongNameProvider:=strongNameProvider,
                metadataImportOptions:=MetadataImportOptions.Public)

        End Sub

        Friend Sub New(
            outputKind As OutputKind,
            reportSuppressedDiagnostics As Boolean,
            ModuleName As String,
            mainTypeName As String,
            scriptClassName As String,
            globalImports As IEnumerable(Of GlobalImport),
            rootNamespace As String,
            optionStrict As OptionStrict,
            optionInfer As Boolean,
            optionExplicit As Boolean,
            optionCompareText As Boolean,
            parseOptions As VisualBasicParseOptions,
            embedVbCoreRuntime As Boolean,
            optimizationLevel As OptimizationLevel,
            checkOverflow As Boolean,
            cryptoKeyContainer As String,
            cryptoKeyFile As String,
            cryptoPublicKey As ImmutableArray(Of Byte),
            delaySign As Boolean?,
            platform As Platform,
            generalDiagnosticOption As ReportDiagnostic,
            specificDiagnosticOptions As IEnumerable(Of KeyValuePair(Of String, ReportDiagnostic)),
            concurrentBuild As Boolean,
            deterministic As Boolean,
            suppressEmbeddedDeclarations As Boolean,
            extendedCustomDebugInformation As Boolean,
            debugPlusMode As Boolean,
            xmlReferenceResolver As XmlReferenceResolver,
            sourceReferenceResolver As SourceReferenceResolver,
            metadataReferenceResolver As MetadataReferenceResolver,
            assemblyIdentityComparer As AssemblyIdentityComparer,
            strongNameProvider As StrongNameProvider,
            metadataImportOptions As MetadataImportOptions)

            MyBase.New(
                outputKind:=outputKind,
                reportSuppressedDiagnostics:=reportSuppressedDiagnostics,
                moduleName:=ModuleName,
                mainTypeName:=mainTypeName,
                scriptClassName:=scriptClassName,
                cryptoKeyContainer:=cryptoKeyContainer,
                cryptoKeyFile:=cryptoKeyFile,
                cryptoPublicKey:=cryptoPublicKey,
                delaySign:=delaySign,
                optimizationLevel:=optimizationLevel,
                checkOverflow:=checkOverflow,
                platform:=platform,
                generalDiagnosticOption:=generalDiagnosticOption,
                warningLevel:=1,
                specificDiagnosticOptions:=specificDiagnosticOptions.ToImmutableDictionaryOrEmpty(CaseInsensitiveComparison.Comparer), ' Diagnostic ids must be processed in case-insensitive fashion.
                concurrentBuild:=concurrentBuild,
                deterministic:=deterministic,
                extendedCustomDebugInformation:=extendedCustomDebugInformation,
                debugPlusMode:=debugPlusMode,
                xmlReferenceResolver:=xmlReferenceResolver,
                sourceReferenceResolver:=sourceReferenceResolver,
                metadataReferenceResolver:=metadataReferenceResolver,
                assemblyIdentityComparer:=assemblyIdentityComparer,
                strongNameProvider:=strongNameProvider,
                metadataImportOptions:=metadataImportOptions)

            _globalImports = globalImports.AsImmutableOrEmpty()
            _rootNamespace = If(rootNamespace, String.Empty)
            _optionStrict = optionStrict
            _optionInfer = optionInfer
            _optionExplicit = optionExplicit
            _optionCompareText = optionCompareText
            _embedVbCoreRuntime = embedVbCoreRuntime
            _suppressEmbeddedDeclarations = suppressEmbeddedDeclarations
            _parseOptions = parseOptions

            Debug.Assert(Not (_embedVbCoreRuntime AndAlso _suppressEmbeddedDeclarations),
                         "_embedVbCoreRuntime and _suppressEmbeddedDeclarations are mutually exclusive")
        End Sub

        Private Sub New(other As VisualBasicCompilationOptions)
            MyClass.New(
                outputKind:=other.OutputKind,
                reportSuppressedDiagnostics:=other.ReportSuppressedDiagnostics,
                ModuleName:=other.ModuleName,
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
                suppressEmbeddedDeclarations:=other.SuppressEmbeddedDeclarations,
                optimizationLevel:=other.OptimizationLevel,
                checkOverflow:=other.CheckOverflow,
                cryptoKeyContainer:=other.CryptoKeyContainer,
                cryptoKeyFile:=other.CryptoKeyFile,
                cryptoPublicKey:=other.CryptoPublicKey,
                delaySign:=other.DelaySign,
                platform:=other.Platform,
                generalDiagnosticOption:=other.GeneralDiagnosticOption,
                specificDiagnosticOptions:=other.SpecificDiagnosticOptions,
                concurrentBuild:=other.ConcurrentBuild,
                deterministic:=other.Deterministic,
                extendedCustomDebugInformation:=other.ExtendedCustomDebugInformation,
                debugPlusMode:=other.DebugPlusMode,
                xmlReferenceResolver:=other.XmlReferenceResolver,
                sourceReferenceResolver:=other.SourceReferenceResolver,
                metadataReferenceResolver:=other.MetadataReferenceResolver,
                assemblyIdentityComparer:=other.AssemblyIdentityComparer,
                strongNameProvider:=other.StrongNameProvider,
                metadataImportOptions:=other.MetadataImportOptions)
        End Sub

        Friend Overrides Function GetImports() As ImmutableArray(Of String)
            Dim names As New ArrayBuilder(Of String)(GlobalImports.Length)
            names.AddRange(GlobalImports.Select(Function(go) go.Name))
            Return names.ToImmutableAndFree()
        End Function

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
        ''' Gets the embedded declaration suppression setting.
        ''' </summary>
        ''' <returns>
        ''' The embedded declaration suppression setting.
        ''' </returns>
        Friend ReadOnly Property SuppressEmbeddedDeclarations As Boolean
            Get
                Return _suppressEmbeddedDeclarations
            End Get
        End Property

        ''' <summary>
        ''' Gets the Parse Options setting.
        ''' Compilation level parse options.  Used when compiling synthetic embedded code such as My template
        ''' </summary>
        ''' <returns>The Parse Options Setting.</returns>
        Friend ReadOnly Property ParseOptions As VisualBasicParseOptions
            Get
                Return _parseOptions
            End Get
        End Property

        ''' <summary>
        ''' Creates a new VisualBasicCompilationOptions instance with a different OutputKind specified.
        ''' </summary>
        ''' <param name="kind">The Output Kind.</param>
        ''' <returns>A new instance of VisualBasicCompilationOptions, if the output kind is different; otherwise current instance.</returns>        
        Public Shadows Function WithOutputKind(kind As OutputKind) As VisualBasicCompilationOptions
            If kind = Me.OutputKind Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {.OutputKind = kind}
        End Function

        ''' <summary>
        ''' Creates a new VisualBasicCompilationOptions instance With a different ModuleName specified.
        ''' </summary>
        ''' <param name="moduleName">The moduleName.</param>        
        ''' <returns>A new instance of VisualBasicCompilationOptions, if the module name is different; otherwise current instance.</returns>        
        Public Function WithModuleName(moduleName As String) As VisualBasicCompilationOptions
            If String.Equals(moduleName, Me.ModuleName, StringComparison.Ordinal) Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {.ModuleName = moduleName}
        End Function

        ''' <summary>
        ''' Creates a new VisualBasicCompilationOptions instance with a Script Class Name specified.
        ''' </summary>
        ''' <param name="name">The name for the ScriptClassName.</param>        
        ''' <returns>A new instance of VisualBasicCompilationOptions, if the script class name is different; otherwise current instance.</returns>        
        Public Shadows Function WithScriptClassName(name As String) As VisualBasicCompilationOptions
            If String.Equals(name, Me.ScriptClassName, StringComparison.Ordinal) Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {.ScriptClassName = name}
        End Function

        ''' <summary>
        ''' Creates a new VisualBasicCompilationOptions instance with a different Main Type name specified.
        ''' </summary>
        ''' <param name="name">The name for the MainType .</param>        
        ''' <returns>A new instance of VisualBasicCompilationOptions, if the main type name is different; otherwise current instance.</returns>        
        Public Shadows Function WithMainTypeName(name As String) As VisualBasicCompilationOptions
            If String.Equals(name, Me.MainTypeName, StringComparison.Ordinal) Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {.MainTypeName = name}
        End Function

        ''' <summary>
        ''' Creates a new VisualBasicCompilationOptions instance with a different global imports specified.
        ''' </summary>
        ''' <param name="globalImports">A collection of Global Imports <see cref="GlobalImport"/>.</param>        
        ''' <returns>A new instance of VisualBasicCompilationOptions.</returns>        
        Public Function WithGlobalImports(globalImports As ImmutableArray(Of GlobalImport)) As VisualBasicCompilationOptions
            If Me.GlobalImports.Equals(globalImports) Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {._globalImports = globalImports}
        End Function

        ''' <summary>
        ''' Creates a new VisualBasicCompilationOptions instance with a different global imports specified.
        ''' </summary>
        ''' <param name="globalImports">A collection of Global Imports <see cref="GlobalImport"/>.</param>        
        ''' <returns>A new instance of VisualBasicCompilationOptions.</returns>        
        Public Function WithGlobalImports(globalImports As IEnumerable(Of GlobalImport)) As VisualBasicCompilationOptions
            Return New VisualBasicCompilationOptions(Me) With {._globalImports = globalImports.AsImmutableOrEmpty()}
        End Function

        ''' <summary>
        ''' Creates a new VisualBasicCompilationOptions instance with a different global imports specified.
        ''' </summary>
        ''' <param name="globalImports">A collection of Global Imports <see cref="GlobalImport"/>.</param>        
        ''' <returns>A new instance of VisualBasicCompilationOptions.</returns>
        Public Function WithGlobalImports(ParamArray globalImports As GlobalImport()) As VisualBasicCompilationOptions
            Return WithGlobalImports(DirectCast(globalImports, IEnumerable(Of GlobalImport)))
        End Function

        ''' <summary>
        ''' Creates a new VisualBasicCompilationOptions instance with a different root namespace specified.
        ''' </summary>
        ''' <param name="rootNamespace">The root namespace.</param>        
        ''' <returns>A new instance of VisualBasicCompilationOptions, if the root namespace is different; otherwise current instance.</returns>        
        Public Function WithRootNamespace(rootNamespace As String) As VisualBasicCompilationOptions
            If String.Equals(rootNamespace, Me.RootNamespace, StringComparison.Ordinal) Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {._rootNamespace = rootNamespace}
        End Function

        ''' <summary>
        ''' Creates a new VisualBasicCompilationOptions instance with a different option strict specified.
        ''' </summary>
        ''' <param name="value">The Option Strict setting.  <see cref="Microsoft.CodeAnalysis.VisualBasic.OptionStrict"/></param>        
        ''' <returns>A new instance of VisualBasicCompilationOptions, if the option strict is different; otherwise current instance.</returns>        
        Public Shadows Function WithOptionStrict(value As OptionStrict) As VisualBasicCompilationOptions
            If value = Me.OptionStrict Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {._optionStrict = value}
        End Function

        ''' <summary>
        ''' Creates a new VisualBasicCompilationOptions instance with a different option infer specified.
        ''' </summary>
        ''' <param name="value">The Option infer setting. </param>        
        ''' <returns>A new instance of VisualBasicCompilationOptions, if the option infer is different; otherwise current instance.</returns>        
        Public Shadows Function WithOptionInfer(value As Boolean) As VisualBasicCompilationOptions
            If value = Me.OptionInfer Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {._optionInfer = value}
        End Function

        ''' <summary>
        ''' Creates a new VisualBasicCompilationOptions instance with a different option explicit specified.
        ''' </summary>
        ''' <param name="value">The Option Explicit setting. </param>        
        ''' <returns>A new instance of VisualBasicCompilationOptions, if the option explicit is different; otherwise current instance.</returns>        
        Public Shadows Function WithOptionExplicit(value As Boolean) As VisualBasicCompilationOptions
            If value = Me.OptionExplicit Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {._optionExplicit = value}
        End Function

        ''' <summary>
        ''' Creates a new VisualBasicCompilationOptions instance with a different Option Compare Text specified.
        ''' </summary>
        ''' <param name="value">The Option Compare Text setting. </param>        
        ''' <returns>A new instance of VisualBasicCompilationOptions, if the option compare text is different; otherwise current instance.</returns>        
        Public Shadows Function WithOptionCompareText(value As Boolean) As VisualBasicCompilationOptions
            If value = Me.OptionCompareText Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {._optionCompareText = value}
        End Function

        ''' <summary>
        ''' Creates a new VisualBasicCompilationOptions instance with a different Embed VB Core Runtime specified.
        ''' </summary>
        ''' <param name="value">The Embed VB Core Runtime setting. </param>        
        ''' <returns>A new instance of VisualBasicCompilationOptions, if the embed vb core runtime is different; otherwise current instance.</returns>        
        Public Shadows Function WithEmbedVbCoreRuntime(value As Boolean) As VisualBasicCompilationOptions
            If value = Me.EmbedVbCoreRuntime Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {._embedVbCoreRuntime = value}
        End Function

        ''' <summary>
        ''' Creates a new VisualBasicCompilationOptions instance with a different Overflow checks specified.
        ''' </summary>
        ''' <param name="enabled">The overflow check setting. </param>        
        ''' <returns>A new instance of VisualBasicCompilationOptions, if the overflow check is different; otherwise current instance.</returns>        
        Public Shadows Function WithOverflowChecks(enabled As Boolean) As VisualBasicCompilationOptions
            If enabled = Me.CheckOverflow Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {.CheckOverflow = enabled}
        End Function

        ''' <summary>
        ''' Creates a new VisualBasicCompilationOptions instance with a different concurrent build specified.
        ''' </summary>
        ''' <param name="concurrentBuild">The concurrent build setting. </param>        
        ''' <returns>A new instance of VisualBasicCompilationOptions, if the concurrent build is different; otherwise current instance.</returns>        
        Public Shadows Function WithConcurrentBuild(concurrentBuild As Boolean) As VisualBasicCompilationOptions
            If concurrentBuild = Me.ConcurrentBuild Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {.ConcurrentBuild = concurrentBuild}
        End Function

        ''' <summary>
        ''' Creates a new VisualBasicCompilationOptions instance with a different deterministic mode specified.
        ''' <param name="deterministic"> The deterministic mode. </param>
        ''' <returns> A new instance of VisualBasicCompilationOptions, if the concurrent build is different; otherwise the current instance.</returns>
        ''' </summary>
        Public Shadows Function WithDeterministic(deterministic As Boolean) As VisualBasicCompilationOptions
            If deterministic = Me.Deterministic Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {.Deterministic = deterministic}
        End Function

        ''' <summary>
        ''' Creates a new VisualBasicCompilationOptions instance with a different extended custom debug information specified.
        ''' </summary>
        ''' <param name="extendedCustomDebugInformation">The extended custom debug information setting. </param>        
        ''' <returns>A new instance of VisualBasicCompilationOptions, if the extended custom debug information is different; otherwise current instance.</returns>        
        Friend Function WithExtendedCustomDebugInformation(extendedCustomDebugInformation As Boolean) As VisualBasicCompilationOptions
            If extendedCustomDebugInformation = Me.ExtendedCustomDebugInformation Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {.ExtendedCustomDebugInformation_internal_protected_set = extendedCustomDebugInformation}
        End Function

        ''' <summary>
        ''' Creates a new VisualBasicCompilationOptions instance with a different extended custom debug information specified.
        ''' </summary>
        ''' <param name="debugPlusMode">The extended custom debug information setting. </param>        
        ''' <returns>A new instance of VisualBasicCompilationOptions, if the extended custom debug information is different; otherwise current instance.</returns>        
        Friend Function WithDebugPlusMode(debugPlusMode As Boolean) As VisualBasicCompilationOptions
            If debugPlusMode = Me.DebugPlusMode Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {.DebugPlusMode_internal_protected_set = debugPlusMode}
        End Function

        ''' <summary>
        ''' Creates a new VisualBasicCompilationOptions instance with different embedded declaration suppression setting specified.
        ''' </summary>
        ''' <param name="suppressEmbeddedDeclarations">The embedded declaration suppression setting. </param>        
        ''' <returns>A new instance of VisualBasicCompilationOptions, if the embedded declaration suppression setting is different; otherwise current instance.</returns>
        ''' <remarks>Only expected to be called from the expression compiler.</remarks>
        Friend Function WithSuppressEmbeddedDeclarations(suppressEmbeddedDeclarations As Boolean) As VisualBasicCompilationOptions
            If suppressEmbeddedDeclarations = _suppressEmbeddedDeclarations Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {._suppressEmbeddedDeclarations = suppressEmbeddedDeclarations}
        End Function

        ''' <summary>
        ''' Creates a new VisualBasicCompilationOptions instance with a different cryptography key container specified
        ''' </summary>
        ''' <param name="name">The name of the cryptography key container. </param>        
        ''' <returns>A new instance of VisualBasicCompilationOptions, if the cryptography key container name is different; otherwise current instance.</returns>        
        Public Shadows Function WithCryptoKeyContainer(name As String) As VisualBasicCompilationOptions
            If String.Equals(name, Me.CryptoKeyContainer, StringComparison.Ordinal) Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {.CryptoKeyContainer = name}
        End Function

        ''' <summary>
        ''' Creates a new VisualBasicCompilationOptions instance with a different cryptography key file path specified.
        ''' </summary>
        ''' <param name="path">The cryptography key file path. </param>        
        ''' <returns>A new instance of VisualBasicCompilationOptions, if the cryptography key path is different; otherwise current instance.</returns>        
        Public Shadows Function WithCryptoKeyFile(path As String) As VisualBasicCompilationOptions
            If String.Equals(path, Me.CryptoKeyFile, StringComparison.Ordinal) Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {.CryptoKeyFile = path}
        End Function

        ''' <summary>
        ''' Creates a new VisualBasicCompilationOptions instance with a different public key.
        ''' </summary>
        ''' <param name="value">The cryptography key file path. </param>        
        ''' <returns>A new instance of VisualBasicCompilationOptions, if the public key is different; otherwise current instance.</returns>        
        Public Shadows Function WithCryptoPublicKey(value As ImmutableArray(Of Byte)) As VisualBasicCompilationOptions
            If value.IsDefault Then
                value = ImmutableArray(Of Byte).Empty
            End If

            If value = Me.CryptoPublicKey Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {.CryptoPublicKey = value}
        End Function

        ''' <summary>
        ''' Creates a new VisualBasicCompilationOptions instance with a different delay signing specified.
        ''' </summary>
        ''' <param name="value">The delay signing setting. </param>        
        ''' <returns>A new instance of VisualBasicCompilationOptions, if the delay sign is different; otherwise current instance.</returns>        
        Public Shadows Function WithDelaySign(value As Boolean?) As VisualBasicCompilationOptions
            If value = Me.DelaySign Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {.DelaySign = value}
        End Function

        ''' <summary>
        ''' Creates a new <see cref="VisualBasicCompilationOptions"/> instance with a different platform specified.
        ''' </summary>
        ''' <param name="value">The platform setting. <see cref="Microsoft.CodeAnalysis.Platform"/></param>        
        ''' <returns>A new instance of VisualBasicCompilationOptions, if the platform is different; otherwise current instance.</returns>        
        Public Shadows Function WithPlatform(value As Platform) As VisualBasicCompilationOptions
            If value = Me.Platform Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {.Platform = value}
        End Function

        Protected Overrides Function CommonWithDeterministic(deterministic As Boolean) As CompilationOptions
            Return Me.WithDeterministic(deterministic)
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

        Protected Overrides Function CommonWithReportSuppressedDiagnostics(reportSuppressedDiagnostics As Boolean) As CompilationOptions
            Return Me.WithReportSuppressedDiagnostics(reportSuppressedDiagnostics)
        End Function

        <Obsolete>
        Protected Overrides Function CommonWithFeatures(features As ImmutableArray(Of String)) As CompilationOptions
            Throw New NotImplementedException()
        End Function

        ''' <summary>
        ''' Creates a new <see cref="VisualBasicCompilationOptions"/> instance with a different report warning specified.
        ''' </summary>
        ''' <param name="value">The Report Warning setting. <see cref="Microsoft.CodeAnalysis.ReportDiagnostic"/></param>        
        ''' <returns>A new instance of VisualBasicCompilationOptions, if the report warning is different; otherwise current instance.</returns>        
        Public Shadows Function WithGeneralDiagnosticOption(value As ReportDiagnostic) As VisualBasicCompilationOptions
            If value = Me.GeneralDiagnosticOption Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {.GeneralDiagnosticOption = value}
        End Function

        ''' <summary>
        ''' Creates a new <see cref="VisualBasicCompilationOptions"/> instance with different specific warnings specified.
        ''' </summary>
        ''' <param name="value">Specific report warnings. <see cref="Microsoft.CodeAnalysis.ReportDiagnostic"/></param>        
        ''' <returns>A new instance of VisualBasicCompilationOptions, if the dictionary of report warning is different; otherwise current instance.</returns>        
        Public Shadows Function WithSpecificDiagnosticOptions(value As ImmutableDictionary(Of String, ReportDiagnostic)) As VisualBasicCompilationOptions
            If value Is Nothing Then
                value = ImmutableDictionary(Of String, ReportDiagnostic).Empty
            End If

            If value Is Me.SpecificDiagnosticOptions Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {.SpecificDiagnosticOptions = value}
        End Function

        ''' <summary>
        ''' Creates a new <see cref="VisualBasicCompilationOptions"/> instance with different specific warnings specified.
        ''' </summary>
        ''' <param name="value">Specific report warnings. <see cref="Microsoft.CodeAnalysis.ReportDiagnostic"/></param>        
        ''' <returns>A new instance of VisualBasicCompilationOptions, if the dictionary of report warning is different; otherwise current instance.</returns>        
        Public Shadows Function WithSpecificDiagnosticOptions(value As IEnumerable(Of KeyValuePair(Of String, ReportDiagnostic))) As VisualBasicCompilationOptions
            Return New VisualBasicCompilationOptions(Me) With {.SpecificDiagnosticOptions = value.ToImmutableDictionaryOrEmpty()}
        End Function

        ''' <summary>
        ''' Creates a new <see cref="VisualBasicCompilationOptions"/> instance with specified suppress diagnostics reporting option.
        ''' </summary>
        ''' <param name="value">Report suppressed diagnostics setting.</param>        
        ''' <returns>A new instance of VisualBasicCompilationOptions, if the value is different from the current value; otherwise current instance.</returns>        
        Public Shadows Function WithReportSuppressedDiagnostics(value As Boolean) As VisualBasicCompilationOptions
            If value = Me.ReportSuppressedDiagnostics Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {.ReportSuppressedDiagnostics = value}
        End Function

        ''' <summary>
        ''' Creates a new <see cref="VisualBasicCompilationOptions"/> instance with a specified <see cref="VisualBasicCompilationOptions.OptimizationLevel"/>.
        ''' </summary>
        ''' <returns>A new instance of <see cref="VisualBasicCompilationOptions"/>, if the value is different; otherwise the current instance.</returns>        
        Public Shadows Function WithOptimizationLevel(value As OptimizationLevel) As VisualBasicCompilationOptions
            If value = Me.OptimizationLevel Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {.OptimizationLevel = value}
        End Function

        Friend Function WithMetadataImportOptions(value As MetadataImportOptions) As VisualBasicCompilationOptions
            If value = Me.MetadataImportOptions Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {.MetadataImportOptions_internal_protected_set = value}
        End Function

        ''' <summary>
        ''' Creates a new <see cref="VisualBasicCompilationOptions"/> instance with a different parse option specified.
        ''' </summary>
        ''' <param name="options">The parse option setting. <see cref="Microsoft.CodeAnalysis.VisualBasic.VisualBasicParseOptions"/></param>        
        ''' <returns>A new instance of VisualBasicCompilationOptions, if the parse options is different; otherwise current instance.</returns>        
        Public Function WithParseOptions(options As VisualBasicParseOptions) As VisualBasicCompilationOptions
            If options Is Me.ParseOptions Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {._parseOptions = options}
        End Function

        Public Shadows Function WithXmlReferenceResolver(resolver As XmlReferenceResolver) As VisualBasicCompilationOptions
            If resolver Is Me.XmlReferenceResolver Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {.XmlReferenceResolver = resolver}
        End Function

        Public Shadows Function WithSourceReferenceResolver(resolver As SourceReferenceResolver) As VisualBasicCompilationOptions
            If resolver Is Me.SourceReferenceResolver Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {.SourceReferenceResolver = resolver}
        End Function

        Public Shadows Function WithMetadataReferenceResolver(resolver As MetadataReferenceResolver) As VisualBasicCompilationOptions
            If resolver Is Me.MetadataReferenceResolver Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {.MetadataReferenceResolver = resolver}
        End Function

        Public Shadows Function WithAssemblyIdentityComparer(comparer As AssemblyIdentityComparer) As VisualBasicCompilationOptions
            comparer = If(comparer, AssemblyIdentityComparer.Default)

            If comparer Is Me.AssemblyIdentityComparer Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {.AssemblyIdentityComparer = comparer}
        End Function

        Public Shadows Function WithStrongNameProvider(provider As StrongNameProvider) As VisualBasicCompilationOptions
            If provider Is Me.StrongNameProvider Then
                Return Me
            End If

            Return New VisualBasicCompilationOptions(Me) With {.StrongNameProvider = provider}
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
                builder.Add(Diagnostic.Create(MessageProvider.Instance, ERRID.ERR_InvalidSwitchValue, NameOf(Platform), Platform.ToString()))
            End If

            If ModuleName IsNot Nothing Then
                Dim e As Exception = MetadataHelpers.CheckAssemblyOrModuleName(ModuleName, NameOf(ModuleName))
                If e IsNot Nothing Then
                    builder.Add(Diagnostic.Create(MessageProvider.Instance, ERRID.ERR_BadCompilationOption, e.Message))
                End If
            End If

            If Not OutputKind.IsValid() Then
                builder.Add(Diagnostic.Create(MessageProvider.Instance, ERRID.ERR_InvalidSwitchValue, NameOf(OutputKind), OutputKind.ToString()))
            End If

            If Not OptimizationLevel.IsValid() Then
                builder.Add(Diagnostic.Create(MessageProvider.Instance, ERRID.ERR_InvalidSwitchValue, NameOf(OptimizationLevel), OptimizationLevel.ToString()))
            End If

            If ScriptClassName Is Nothing OrElse Not ScriptClassName.IsValidClrTypeName() Then
                builder.Add(Diagnostic.Create(MessageProvider.Instance, ERRID.ERR_InvalidSwitchValue, NameOf(ScriptClassName), If(ScriptClassName, "Nothing")))
            End If

            If MainTypeName IsNot Nothing AndAlso Not MainTypeName.IsValidClrTypeName() Then
                builder.Add(Diagnostic.Create(MessageProvider.Instance, ERRID.ERR_InvalidSwitchValue, NameOf(MainTypeName), MainTypeName))
            End If

            If Not String.IsNullOrEmpty(RootNamespace) AndAlso Not OptionsValidator.IsValidNamespaceName(RootNamespace) Then
                builder.Add(Diagnostic.Create(MessageProvider.Instance, ERRID.ERR_InvalidSwitchValue, NameOf(RootNamespace), RootNamespace))
            End If

            If Not OptionStrict.IsValid Then
                builder.Add(Diagnostic.Create(MessageProvider.Instance, ERRID.ERR_InvalidSwitchValue, NameOf(OptionStrict), OptionStrict.ToString()))
            End If

            If Platform = Platform.AnyCpu32BitPreferred AndAlso OutputKind.IsValid() AndAlso
                 Not (OutputKind = OutputKind.ConsoleApplication OrElse OutputKind = OutputKind.WindowsApplication OrElse OutputKind = OutputKind.WindowsRuntimeApplication) Then
                builder.Add(Diagnostic.Create(MessageProvider.Instance, ERRID.ERR_LibAnycpu32bitPreferredConflict, NameOf(Platform), Platform.ToString()))
            End If

            ' TODO: add check for 
            '          (kind == 'arm' || kind == 'appcontainer' || kind == 'winmdobj') &&
            '          (version >= "6.2")

            If Not CryptoPublicKey.IsEmpty Then
                If CryptoKeyFile IsNot Nothing Then
                    builder.Add(Diagnostic.Create(MessageProvider.Instance, ERRID.ERR_MutuallyExclusiveOptions, NameOf(CryptoPublicKey), NameOf(CryptoKeyFile)))
                End If

                If CryptoKeyContainer IsNot Nothing Then
                    builder.Add(Diagnostic.Create(MessageProvider.Instance, ERRID.ERR_MutuallyExclusiveOptions, NameOf(CryptoPublicKey), NameOf(CryptoKeyContainer)))
                End If
            End If
        End Sub

        ''' <summary>
        ''' Determines whether the current object is equal to another object of the same type.
        ''' </summary>
        ''' <param name="other">A VisualBasicCompilationOptions to compare with this object</param>
        ''' <returns>A boolean value.  True if the current object is equal to the other parameter; otherwise, False.</returns>
        Public Overloads Function Equals(other As VisualBasicCompilationOptions) As Boolean Implements IEquatable(Of VisualBasicCompilationOptions).Equals
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
                   Me.SuppressEmbeddedDeclarations = other.SuppressEmbeddedDeclarations AndAlso
                   If(Me.ParseOptions Is Nothing, other.ParseOptions Is Nothing, Me.ParseOptions.Equals(other.ParseOptions))
        End Function

        ''' <summary>
        ''' Indicates whether the current object is equal to another object.
        ''' </summary>
        ''' <param name="obj">A object to compare with this object</param>
        ''' <returns>A boolean value.  True if the current object is equal to the other parameter; otherwise, False.</returns>
        Public Overrides Function Equals(obj As Object) As Boolean
            Return Me.Equals(TryCast(obj, VisualBasicCompilationOptions))
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
                   Hash.Combine(Me.SuppressEmbeddedDeclarations,
                   Hash.Combine(Me.ParseOptions, 0))))))))))
        End Function

        Friend Overrides Function FilterDiagnostic(diagnostic As Diagnostic) As Diagnostic
            Return VisualBasicDiagnosticFilter.Filter(diagnostic, GeneralDiagnosticOption, SpecificDiagnosticOptions)
        End Function

        ' 1.0 BACKCOMPAT OVERLOAD -- DO NOT TOUCH
        Public Sub New(
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
            parseOptions As VisualBasicParseOptions,
            embedVbCoreRuntime As Boolean,
            optimizationLevel As OptimizationLevel,
            checkOverflow As Boolean,
            cryptoKeyContainer As String,
            cryptoKeyFile As String,
            cryptoPublicKey As ImmutableArray(Of Byte),
            delaySign As Boolean?,
            platform As Platform,
            generalDiagnosticOption As ReportDiagnostic,
            specificDiagnosticOptions As IEnumerable(Of KeyValuePair(Of String, ReportDiagnostic)),
            concurrentBuild As Boolean,
            xmlReferenceResolver As XmlReferenceResolver,
            sourceReferenceResolver As SourceReferenceResolver,
            metadataReferenceResolver As MetadataReferenceResolver,
            assemblyIdentityComparer As AssemblyIdentityComparer,
            strongNameProvider As StrongNameProvider)

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
                cryptoPublicKey,
                delaySign,
                platform,
                generalDiagnosticOption,
                specificDiagnosticOptions,
                concurrentBuild,
                deterministic:=False,' TODO: fix this
                xmlReferenceResolver:=xmlReferenceResolver,
                sourceReferenceResolver:=sourceReferenceResolver,
                metadataReferenceResolver:=metadataReferenceResolver,
                assemblyIdentityComparer:=assemblyIdentityComparer,
                strongNameProvider:=strongNameProvider)
        End Sub

        '' Bad constructor, do not use!
        '' Violates the rules for optional parameter overloads detailed at
        '' https://github.com/dotnet/roslyn/blob/e8fdb391703dcb5712ff6a5b83d768d784cba4cf/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md
        Public Sub New(
            outputKind As OutputKind,
            reportSuppressedDiagnostics As Boolean,
            Optional moduleName As String = Nothing,
            Optional mainTypeName As String = Nothing,
            Optional scriptClassName As String = WellKnownMemberNames.DefaultScriptClassName,
            Optional globalImports As IEnumerable(Of GlobalImport) = Nothing,
            Optional rootNamespace As String = Nothing,
            Optional optionStrict As OptionStrict = OptionStrict.Off,
            Optional optionInfer As Boolean = True,
            Optional optionExplicit As Boolean = True,
            Optional optionCompareText As Boolean = False,
            Optional parseOptions As VisualBasicParseOptions = Nothing,
            Optional embedVbCoreRuntime As Boolean = False,
            Optional optimizationLevel As OptimizationLevel = OptimizationLevel.Debug,
            Optional checkOverflow As Boolean = True,
            Optional cryptoKeyContainer As String = Nothing,
            Optional cryptoKeyFile As String = Nothing,
            Optional cryptoPublicKey As ImmutableArray(Of Byte) = Nothing,
            Optional delaySign As Boolean? = Nothing,
            Optional platform As Platform = Platform.AnyCpu,
            Optional generalDiagnosticOption As ReportDiagnostic = ReportDiagnostic.Default,
            Optional specificDiagnosticOptions As IEnumerable(Of KeyValuePair(Of String, ReportDiagnostic)) = Nothing,
            Optional concurrentBuild As Boolean = True,
            Optional deterministic As Boolean = False,
            Optional xmlReferenceResolver As XmlReferenceResolver = Nothing,
            Optional sourceReferenceResolver As SourceReferenceResolver = Nothing,
            Optional metadataReferenceResolver As MetadataReferenceResolver = Nothing,
            Optional assemblyIdentityComparer As AssemblyIdentityComparer = Nothing,
            Optional strongNameProvider As StrongNameProvider = Nothing)

            MyClass.New(
                outputKind,
                reportSuppressedDiagnostics,
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
                cryptoPublicKey,
                delaySign,
                platform,
                generalDiagnosticOption,
                specificDiagnosticOptions,
                concurrentBuild,
                deterministic,
                suppressEmbeddedDeclarations:=False,
                extendedCustomDebugInformation:=True,
                debugPlusMode:=False,
                xmlReferenceResolver:=xmlReferenceResolver,
                sourceReferenceResolver:=sourceReferenceResolver,
                metadataReferenceResolver:=metadataReferenceResolver,
                assemblyIdentityComparer:=assemblyIdentityComparer,
                strongNameProvider:=strongNameProvider,
                metadataImportOptions:=MetadataImportOptions.Public)

        End Sub
    End Class
End Namespace
