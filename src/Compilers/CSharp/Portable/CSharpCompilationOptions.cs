// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents various options that affect compilation, such as 
    /// whether to emit an executable or a library, whether to optimize
    /// generated code, and so on.
    /// </summary>
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode().
    public sealed class CSharpCompilationOptions : CompilationOptions, IEquatable<CSharpCompilationOptions>
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode().
    {
        /// <summary>
        /// Allow unsafe regions (i.e. unsafe modifiers on members and unsafe blocks).
        /// </summary>
        public bool AllowUnsafe { get; private set; }

        // PROTOTYPE: public API
        internal int MemorySafetyRules { get; private set; }

        internal bool HasEvolvedMemorySafetyRules => MemorySafetyRules >= 2;

        /// <summary>
        /// Global namespace usings.
        /// </summary>
        public ImmutableArray<string> Usings { get; private set; }

        /// <summary>
        /// Flags applied to the top-level binder created for each syntax tree in the compilation 
        /// as well as for the binder of global imports.
        /// </summary>
        internal BinderFlags TopLevelBinderFlags { get; private set; }

        /// <summary>
        /// Global Nullable context options.
        /// </summary>
        public override NullableContextOptions NullableContextOptions { get; protected set; }

        // Defaults correspond to the compiler's defaults or indicate that the user did not specify when that is significant.
        // That's significant when one option depends on another's setting. SubsystemVersion depends on Platform and Target.
        public CSharpCompilationOptions(
            OutputKind outputKind,
            bool reportSuppressedDiagnostics = false,
            string? moduleName = null,
            string? mainTypeName = null,
            string? scriptClassName = null,
            IEnumerable<string>? usings = null,
            OptimizationLevel optimizationLevel = OptimizationLevel.Debug,
            bool checkOverflow = false,
            bool allowUnsafe = false,
            string? cryptoKeyContainer = null,
            string? cryptoKeyFile = null,
            ImmutableArray<byte> cryptoPublicKey = default,
            bool? delaySign = null,
            Platform platform = Platform.AnyCpu,
            ReportDiagnostic generalDiagnosticOption = ReportDiagnostic.Default,
            int warningLevel = Diagnostic.DefaultWarningLevel,
            IEnumerable<KeyValuePair<string, ReportDiagnostic>>? specificDiagnosticOptions = null,
            bool concurrentBuild = true,
            bool deterministic = false,
            XmlReferenceResolver? xmlReferenceResolver = null,
            SourceReferenceResolver? sourceReferenceResolver = null,
            MetadataReferenceResolver? metadataReferenceResolver = null,
            AssemblyIdentityComparer? assemblyIdentityComparer = null,
            StrongNameProvider? strongNameProvider = null,
            bool publicSign = false,
            MetadataImportOptions metadataImportOptions = MetadataImportOptions.Public,
            NullableContextOptions nullableContextOptions = NullableContextOptions.Disable)
            : this(outputKind, reportSuppressedDiagnostics, moduleName, mainTypeName, scriptClassName,
                   usings, optimizationLevel, checkOverflow, allowUnsafe,
                   cryptoKeyContainer, cryptoKeyFile, cryptoPublicKey, delaySign, platform,
                   generalDiagnosticOption, warningLevel,
                   specificDiagnosticOptions, concurrentBuild, deterministic,
                   currentLocalTime: default,
                   debugPlusMode: false,
                   xmlReferenceResolver: xmlReferenceResolver,
                   sourceReferenceResolver: sourceReferenceResolver,
                   syntaxTreeOptionsProvider: null,
                   metadataReferenceResolver: metadataReferenceResolver,
                   assemblyIdentityComparer: assemblyIdentityComparer,
                   strongNameProvider: strongNameProvider,
                   metadataImportOptions: metadataImportOptions,
                   referencesSupersedeLowerVersions: false,
                   publicSign: publicSign,
                   topLevelBinderFlags: BinderFlags.None,
                   nullableContextOptions: nullableContextOptions)
        {
        }

        // 15.9 BACKCOMPAT OVERLOAD -- DO NOT TOUCH
        public CSharpCompilationOptions(
            OutputKind outputKind,
            bool reportSuppressedDiagnostics,
            string? moduleName,
            string? mainTypeName,
            string? scriptClassName,
            IEnumerable<string>? usings,
            OptimizationLevel optimizationLevel,
            bool checkOverflow,
            bool allowUnsafe,
            string? cryptoKeyContainer,
            string? cryptoKeyFile,
            ImmutableArray<byte> cryptoPublicKey,
            bool? delaySign,
            Platform platform,
            ReportDiagnostic generalDiagnosticOption,
            int warningLevel,
            IEnumerable<KeyValuePair<string, ReportDiagnostic>>? specificDiagnosticOptions,
            bool concurrentBuild,
            bool deterministic,
            XmlReferenceResolver? xmlReferenceResolver,
            SourceReferenceResolver? sourceReferenceResolver,
            MetadataReferenceResolver? metadataReferenceResolver,
            AssemblyIdentityComparer? assemblyIdentityComparer,
            StrongNameProvider? strongNameProvider,
            bool publicSign,
            MetadataImportOptions metadataImportOptions)
            : this(outputKind, reportSuppressedDiagnostics, moduleName, mainTypeName, scriptClassName,
                   usings, optimizationLevel, checkOverflow, allowUnsafe,
                   cryptoKeyContainer, cryptoKeyFile, cryptoPublicKey, delaySign, platform,
                   generalDiagnosticOption, warningLevel,
                   specificDiagnosticOptions, concurrentBuild, deterministic,
                   xmlReferenceResolver,
                   sourceReferenceResolver,
                   metadataReferenceResolver,
                   assemblyIdentityComparer,
                   strongNameProvider,
                   publicSign,
                   metadataImportOptions,
                   nullableContextOptions: NullableContextOptions.Disable)
        {
        }

        // 15.6 BACKCOMPAT OVERLOAD -- DO NOT TOUCH
        [EditorBrowsable(EditorBrowsableState.Never)]
        public CSharpCompilationOptions(
            OutputKind outputKind,
            bool reportSuppressedDiagnostics,
            string? moduleName,
            string? mainTypeName,
            string? scriptClassName,
            IEnumerable<string>? usings,
            OptimizationLevel optimizationLevel,
            bool checkOverflow,
            bool allowUnsafe,
            string? cryptoKeyContainer,
            string? cryptoKeyFile,
            ImmutableArray<byte> cryptoPublicKey,
            bool? delaySign,
            Platform platform,
            ReportDiagnostic generalDiagnosticOption,
            int warningLevel,
            IEnumerable<KeyValuePair<string, ReportDiagnostic>>? specificDiagnosticOptions,
            bool concurrentBuild,
            bool deterministic,
            XmlReferenceResolver? xmlReferenceResolver,
            SourceReferenceResolver? sourceReferenceResolver,
            MetadataReferenceResolver? metadataReferenceResolver,
            AssemblyIdentityComparer? assemblyIdentityComparer,
            StrongNameProvider? strongNameProvider,
            bool publicSign)
            : this(outputKind, reportSuppressedDiagnostics, moduleName, mainTypeName, scriptClassName,
                   usings, optimizationLevel, checkOverflow, allowUnsafe,
                   cryptoKeyContainer, cryptoKeyFile, cryptoPublicKey, delaySign, platform,
                   generalDiagnosticOption, warningLevel,
                   specificDiagnosticOptions, concurrentBuild, deterministic,
                   xmlReferenceResolver,
                   sourceReferenceResolver,
                   metadataReferenceResolver,
                   assemblyIdentityComparer,
                   strongNameProvider,
                   publicSign,
                   MetadataImportOptions.Public)
        {
        }

        // Expects correct arguments.
        internal CSharpCompilationOptions(
            OutputKind outputKind,
            bool reportSuppressedDiagnostics,
            string? moduleName,
            string? mainTypeName,
            string? scriptClassName,
            IEnumerable<string>? usings,
            OptimizationLevel optimizationLevel,
            bool checkOverflow,
            bool allowUnsafe,
            string? cryptoKeyContainer,
            string? cryptoKeyFile,
            ImmutableArray<byte> cryptoPublicKey,
            bool? delaySign,
            Platform platform,
            ReportDiagnostic generalDiagnosticOption,
            int warningLevel,
            IEnumerable<KeyValuePair<string, ReportDiagnostic>>? specificDiagnosticOptions,
            bool concurrentBuild,
            bool deterministic,
            DateTime currentLocalTime,
            bool debugPlusMode,
            XmlReferenceResolver? xmlReferenceResolver,
            SourceReferenceResolver? sourceReferenceResolver,
            SyntaxTreeOptionsProvider? syntaxTreeOptionsProvider,
            MetadataReferenceResolver? metadataReferenceResolver,
            AssemblyIdentityComparer? assemblyIdentityComparer,
            StrongNameProvider? strongNameProvider,
            MetadataImportOptions metadataImportOptions,
            bool referencesSupersedeLowerVersions,
            bool publicSign,
            BinderFlags topLevelBinderFlags,
            NullableContextOptions nullableContextOptions)
            : base(outputKind, reportSuppressedDiagnostics, moduleName, mainTypeName, scriptClassName,
                   cryptoKeyContainer, cryptoKeyFile, cryptoPublicKey, delaySign, publicSign, optimizationLevel, checkOverflow,
                   platform, generalDiagnosticOption, warningLevel, specificDiagnosticOptions.ToImmutableDictionaryOrEmpty(),
                   concurrentBuild, deterministic, currentLocalTime, debugPlusMode, xmlReferenceResolver,
                   sourceReferenceResolver, syntaxTreeOptionsProvider, metadataReferenceResolver,
                   assemblyIdentityComparer, strongNameProvider, metadataImportOptions, referencesSupersedeLowerVersions)
        {
            this.Usings = usings.AsImmutableOrEmpty();
            this.AllowUnsafe = allowUnsafe;
            this.TopLevelBinderFlags = topLevelBinderFlags;
            this.NullableContextOptions = nullableContextOptions;
        }

        private CSharpCompilationOptions(CSharpCompilationOptions other) : this(
            outputKind: other.OutputKind,
            moduleName: other.ModuleName,
            mainTypeName: other.MainTypeName,
            scriptClassName: other.ScriptClassName,
            usings: other.Usings,
            optimizationLevel: other.OptimizationLevel,
            checkOverflow: other.CheckOverflow,
            allowUnsafe: other.AllowUnsafe,
            cryptoKeyContainer: other.CryptoKeyContainer,
            cryptoKeyFile: other.CryptoKeyFile,
            cryptoPublicKey: other.CryptoPublicKey,
            delaySign: other.DelaySign,
            platform: other.Platform,
            generalDiagnosticOption: other.GeneralDiagnosticOption,
            warningLevel: other.WarningLevel,
            specificDiagnosticOptions: other.SpecificDiagnosticOptions,
            concurrentBuild: other.ConcurrentBuild,
            deterministic: other.Deterministic,
            currentLocalTime: other.CurrentLocalTime,
            debugPlusMode: other.DebugPlusMode,
            xmlReferenceResolver: other.XmlReferenceResolver,
            sourceReferenceResolver: other.SourceReferenceResolver,
            syntaxTreeOptionsProvider: other.SyntaxTreeOptionsProvider,
            metadataReferenceResolver: other.MetadataReferenceResolver,
            assemblyIdentityComparer: other.AssemblyIdentityComparer,
            strongNameProvider: other.StrongNameProvider,
            metadataImportOptions: other.MetadataImportOptions,
            referencesSupersedeLowerVersions: other.ReferencesSupersedeLowerVersions,
            reportSuppressedDiagnostics: other.ReportSuppressedDiagnostics,
            publicSign: other.PublicSign,
            topLevelBinderFlags: other.TopLevelBinderFlags,
            nullableContextOptions: other.NullableContextOptions)
        {
            // PROTOTYPE: should be in the constructor
            MemorySafetyRules = other.MemorySafetyRules;
        }

        public override string Language => LanguageNames.CSharp;

        internal CSharpCompilationOptions WithTopLevelBinderFlags(BinderFlags flags)
        {
            return (flags == TopLevelBinderFlags) ? this : new CSharpCompilationOptions(this) { TopLevelBinderFlags = flags };
        }

        internal override ImmutableArray<string> GetImports() => Usings;

        internal override DeterministicKeyBuilder CreateDeterministicKeyBuilder() => CSharpDeterministicKeyBuilder.Instance;

        public new CSharpCompilationOptions WithOutputKind(OutputKind kind)
        {
            if (kind == this.OutputKind)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { OutputKind = kind };
        }

        public new CSharpCompilationOptions WithModuleName(string? moduleName)
        {
            if (moduleName == this.ModuleName)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { ModuleName = moduleName };
        }

        public new CSharpCompilationOptions WithScriptClassName(string? name)
        {
            if (name == this.ScriptClassName)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { ScriptClassName = name };
        }

        public new CSharpCompilationOptions WithMainTypeName(string? name)
        {
            if (name == this.MainTypeName)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { MainTypeName = name };
        }

        public new CSharpCompilationOptions WithCryptoKeyContainer(string? name)
        {
            if (name == this.CryptoKeyContainer)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { CryptoKeyContainer = name };
        }

        public new CSharpCompilationOptions WithCryptoKeyFile(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = null;
            }

            if (path == this.CryptoKeyFile)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { CryptoKeyFile = path };
        }

        public new CSharpCompilationOptions WithCryptoPublicKey(ImmutableArray<byte> value)
        {
            if (value.IsDefault)
            {
                value = ImmutableArray<byte>.Empty;
            }

            if (value == this.CryptoPublicKey)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { CryptoPublicKey = value };
        }

        public new CSharpCompilationOptions WithDelaySign(bool? value)
        {
            if (value == this.DelaySign)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { DelaySign = value };
        }

        public CSharpCompilationOptions WithUsings(ImmutableArray<string> usings)
        {
            if (this.Usings == usings)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { Usings = usings };
        }

        public CSharpCompilationOptions WithUsings(IEnumerable<string>? usings) =>
            new CSharpCompilationOptions(this) { Usings = usings.AsImmutableOrEmpty() };

        public CSharpCompilationOptions WithUsings(params string[]? usings) => WithUsings((IEnumerable<string>?)usings);

        public new CSharpCompilationOptions WithOptimizationLevel(OptimizationLevel value)
        {
            if (value == this.OptimizationLevel)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { OptimizationLevel = value };
        }

        public new CSharpCompilationOptions WithOverflowChecks(bool enabled)
        {
            if (enabled == this.CheckOverflow)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { CheckOverflow = enabled };
        }

        public CSharpCompilationOptions WithNullableContextOptions(NullableContextOptions options)
        {
            if (options == this.NullableContextOptions)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { NullableContextOptions = options };
        }

        public CSharpCompilationOptions WithAllowUnsafe(bool enabled)
        {
            if (enabled == this.AllowUnsafe)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { AllowUnsafe = enabled };
        }

        // PROTOTYPE: public API
        internal CSharpCompilationOptions WithMemorySafetyRules(int version)
        {
            if (version == this.MemorySafetyRules)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { MemorySafetyRules = version };
        }

        public new CSharpCompilationOptions WithPlatform(Platform platform)
        {
            if (this.Platform == platform)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { Platform = platform };
        }

        public new CSharpCompilationOptions WithPublicSign(bool publicSign)
        {
            if (this.PublicSign == publicSign)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { PublicSign = publicSign };
        }

        protected override CompilationOptions CommonWithGeneralDiagnosticOption(ReportDiagnostic value) => WithGeneralDiagnosticOption(value);

        protected override CompilationOptions CommonWithSpecificDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic>? specificDiagnosticOptions) =>
            WithSpecificDiagnosticOptions(specificDiagnosticOptions);

        protected override CompilationOptions CommonWithSpecificDiagnosticOptions(IEnumerable<KeyValuePair<string, ReportDiagnostic>>? specificDiagnosticOptions) =>
            WithSpecificDiagnosticOptions(specificDiagnosticOptions);

        protected override CompilationOptions CommonWithReportSuppressedDiagnostics(bool reportSuppressedDiagnostics) =>
            WithReportSuppressedDiagnostics(reportSuppressedDiagnostics);

        public new CSharpCompilationOptions WithGeneralDiagnosticOption(ReportDiagnostic value)
        {
            if (this.GeneralDiagnosticOption == value)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { GeneralDiagnosticOption = value };
        }

        public new CSharpCompilationOptions WithSpecificDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic>? values)
        {
            if (values == null)
            {
                values = ImmutableDictionary<string, ReportDiagnostic>.Empty;
            }

            if (this.SpecificDiagnosticOptions == values)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { SpecificDiagnosticOptions = values };
        }

        public new CSharpCompilationOptions WithSpecificDiagnosticOptions(IEnumerable<KeyValuePair<string, ReportDiagnostic>>? values) =>
            new CSharpCompilationOptions(this) { SpecificDiagnosticOptions = values.ToImmutableDictionaryOrEmpty() };

        public new CSharpCompilationOptions WithReportSuppressedDiagnostics(bool reportSuppressedDiagnostics)
        {
            if (reportSuppressedDiagnostics == this.ReportSuppressedDiagnostics)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { ReportSuppressedDiagnostics = reportSuppressedDiagnostics };
        }

        public CSharpCompilationOptions WithWarningLevel(int warningLevel)
        {
            if (warningLevel == this.WarningLevel)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { WarningLevel = warningLevel };
        }

        public new CSharpCompilationOptions WithConcurrentBuild(bool concurrentBuild)
        {
            if (concurrentBuild == this.ConcurrentBuild)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { ConcurrentBuild = concurrentBuild };
        }

        public new CSharpCompilationOptions WithDeterministic(bool deterministic)
        {
            if (deterministic == this.Deterministic)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { Deterministic = deterministic };
        }

        internal CSharpCompilationOptions WithCurrentLocalTime(DateTime value)
        {
            if (value == this.CurrentLocalTime)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { CurrentLocalTime = value };
        }

        internal CSharpCompilationOptions WithDebugPlusMode(bool debugPlusMode)
        {
            if (debugPlusMode == this.DebugPlusMode)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { DebugPlusMode = debugPlusMode };
        }

        public new CSharpCompilationOptions WithMetadataImportOptions(MetadataImportOptions value)
        {
            if (value == this.MetadataImportOptions)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { MetadataImportOptions = value };
        }

        internal CSharpCompilationOptions WithReferencesSupersedeLowerVersions(bool value)
        {
            if (value == this.ReferencesSupersedeLowerVersions)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { ReferencesSupersedeLowerVersions = value };
        }

        public new CSharpCompilationOptions WithXmlReferenceResolver(XmlReferenceResolver? resolver)
        {
            if (ReferenceEquals(resolver, this.XmlReferenceResolver))
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { XmlReferenceResolver = resolver };
        }

        public new CSharpCompilationOptions WithSourceReferenceResolver(SourceReferenceResolver? resolver)
        {
            if (ReferenceEquals(resolver, this.SourceReferenceResolver))
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { SourceReferenceResolver = resolver };
        }

        public new CSharpCompilationOptions WithSyntaxTreeOptionsProvider(SyntaxTreeOptionsProvider? provider)
        {
            if (ReferenceEquals(provider, this.SyntaxTreeOptionsProvider))
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { SyntaxTreeOptionsProvider = provider };
        }

        public new CSharpCompilationOptions WithMetadataReferenceResolver(MetadataReferenceResolver? resolver)
        {
            if (ReferenceEquals(resolver, this.MetadataReferenceResolver))
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { MetadataReferenceResolver = resolver };
        }

        public new CSharpCompilationOptions WithAssemblyIdentityComparer(AssemblyIdentityComparer? comparer)
        {
            comparer = comparer ?? AssemblyIdentityComparer.Default;

            if (ReferenceEquals(comparer, this.AssemblyIdentityComparer))
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { AssemblyIdentityComparer = comparer };
        }

        public new CSharpCompilationOptions WithStrongNameProvider(StrongNameProvider? provider)
        {
            if (ReferenceEquals(provider, this.StrongNameProvider))
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { StrongNameProvider = provider };
        }

        protected override CompilationOptions CommonWithConcurrentBuild(bool concurrent) => WithConcurrentBuild(concurrent);
        protected override CompilationOptions CommonWithDeterministic(bool deterministic) => WithDeterministic(deterministic);

        protected override CompilationOptions CommonWithOutputKind(OutputKind kind) => WithOutputKind(kind);

        protected override CompilationOptions CommonWithPlatform(Platform platform) => WithPlatform(platform);

        protected override CompilationOptions CommonWithPublicSign(bool publicSign) => WithPublicSign(publicSign);

        protected override CompilationOptions CommonWithOptimizationLevel(OptimizationLevel value) => WithOptimizationLevel(value);

        protected override CompilationOptions CommonWithAssemblyIdentityComparer(AssemblyIdentityComparer? comparer) =>
            WithAssemblyIdentityComparer(comparer);

        protected override CompilationOptions CommonWithXmlReferenceResolver(XmlReferenceResolver? resolver) =>
            WithXmlReferenceResolver(resolver);

        protected override CompilationOptions CommonWithSourceReferenceResolver(SourceReferenceResolver? resolver) =>
            WithSourceReferenceResolver(resolver);

        protected override CompilationOptions CommonWithSyntaxTreeOptionsProvider(SyntaxTreeOptionsProvider? provider)
            => WithSyntaxTreeOptionsProvider(provider);

        protected override CompilationOptions CommonWithMetadataReferenceResolver(MetadataReferenceResolver? resolver) =>
            WithMetadataReferenceResolver(resolver);

        protected override CompilationOptions CommonWithStrongNameProvider(StrongNameProvider? provider) =>
            WithStrongNameProvider(provider);

        protected override CompilationOptions CommonWithMetadataImportOptions(MetadataImportOptions value) =>
            WithMetadataImportOptions(value);

        [Obsolete]
        protected override CompilationOptions CommonWithFeatures(ImmutableArray<string> features)
        {
            throw new NotImplementedException();
        }

        internal override void ValidateOptions(ArrayBuilder<Diagnostic> builder)
        {
            ValidateOptions(builder, MessageProvider.Instance);

            //  /main & /target:{library|netmodule|winmdobj}
            if (this.MainTypeName != null)
            {
                if (this.OutputKind.IsValid() && !this.OutputKind.IsApplication())
                {
                    builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_NoMainOnDLL));
                }

                if (!MainTypeName.IsValidClrTypeName())
                {
                    builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadCompilationOptionValue, nameof(MainTypeName), MainTypeName));
                }
            }

            if (!Platform.IsValid())
            {
                builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadPlatformType, Platform.ToString()));
            }

            if (ModuleName != null)
            {
                MetadataHelpers.CheckAssemblyOrModuleName(ModuleName, MessageProvider.Instance, (int)ErrorCode.ERR_BadModuleName, builder);
            }

            if (!OutputKind.IsValid())
            {
                builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadCompilationOptionValue, nameof(OutputKind), OutputKind.ToString()));
            }

            if (!OptimizationLevel.IsValid())
            {
                builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadCompilationOptionValue, nameof(OptimizationLevel), OptimizationLevel.ToString()));
            }

            if (ScriptClassName == null || !ScriptClassName.IsValidClrTypeName())
            {
                builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadCompilationOptionValue, nameof(ScriptClassName), ScriptClassName ?? "null"));
            }

            if (WarningLevel < 0)
            {
                builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadCompilationOptionValue, nameof(WarningLevel), WarningLevel));
            }

            if (Usings != null && Usings.Any(static u => !u.IsValidClrNamespaceName()))
            {
                builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadCompilationOptionValue, nameof(Usings), Usings.Where(u => !u.IsValidClrNamespaceName()).First() ?? "null"));
            }

            if (Platform == Platform.AnyCpu32BitPreferred && OutputKind.IsValid() && !(OutputKind == OutputKind.ConsoleApplication || OutputKind == OutputKind.WindowsApplication || OutputKind == OutputKind.WindowsRuntimeApplication))
            {
                builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadPrefer32OnLib));
            }

            if (!MetadataImportOptions.IsValid())
            {
                builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadCompilationOptionValue, nameof(MetadataImportOptions), MetadataImportOptions.ToString()));
            }

            // PROTOTYPE: validate the value of MemorySafetyRules?

            // TODO: add check for 
            //          (kind == 'arm' || kind == 'appcontainer' || kind == 'winmdobj') &&
            //          (version >= "6.2")
        }

        public bool Equals(CSharpCompilationOptions? other)
        {
            if (object.ReferenceEquals(this, other))
            {
                return true;
            }

            if (!base.EqualsHelper(other))
            {
                return false;
            }

            return this.AllowUnsafe == other.AllowUnsafe &&
                   this.MemorySafetyRules == other.MemorySafetyRules &&
                   this.TopLevelBinderFlags == other.TopLevelBinderFlags &&
                   (this.Usings == null ? other.Usings == null : this.Usings.SequenceEqual(other.Usings, StringComparer.Ordinal) &&
                   this.NullableContextOptions == other.NullableContextOptions);
        }

        public override bool Equals(object? obj)
        {
            return this.Equals(obj as CSharpCompilationOptions);
        }

        protected override int ComputeHashCode()
        {
            return Hash.Combine(GetHashCodeHelper(),
                   Hash.Combine(this.MemorySafetyRules,
                   Hash.Combine(this.AllowUnsafe,
                   Hash.Combine(Hash.CombineValues(this.Usings, StringComparer.Ordinal),
                   Hash.Combine(((uint)TopLevelBinderFlags).GetHashCode(), ((int)this.NullableContextOptions).GetHashCode())))));
        }

        internal override Diagnostic? FilterDiagnostic(Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            return CSharpDiagnosticFilter.Filter(
                diagnostic,
                WarningLevel,
                NullableContextOptions,
                GeneralDiagnosticOption,
                SpecificDiagnosticOptions,
                SyntaxTreeOptionsProvider,
                cancellationToken);
        }

        protected override CompilationOptions CommonWithModuleName(string? moduleName)
        {
            return WithModuleName(moduleName);
        }

        protected override CompilationOptions CommonWithMainTypeName(string? mainTypeName)
        {
            return WithMainTypeName(mainTypeName);
        }

        protected override CompilationOptions CommonWithScriptClassName(string? scriptClassName)
        {
            return WithScriptClassName(scriptClassName);
        }

        protected override CompilationOptions CommonWithCryptoKeyContainer(string? cryptoKeyContainer)
        {
            return WithCryptoKeyContainer(cryptoKeyContainer);
        }

        protected override CompilationOptions CommonWithCryptoKeyFile(string? cryptoKeyFile)
        {
            return WithCryptoKeyFile(cryptoKeyFile);
        }

        protected override CompilationOptions CommonWithCryptoPublicKey(ImmutableArray<byte> cryptoPublicKey)
        {
            return WithCryptoPublicKey(cryptoPublicKey);
        }

        protected override CompilationOptions CommonWithDelaySign(bool? delaySign)
        {
            return WithDelaySign(delaySign);
        }

        protected override CompilationOptions CommonWithCheckOverflow(bool checkOverflow)
        {
            return WithOverflowChecks(checkOverflow);
        }

        // 1.1 BACKCOMPAT OVERLOAD -- DO NOT TOUCH
        [EditorBrowsable(EditorBrowsableState.Never)]
        public CSharpCompilationOptions(
            OutputKind outputKind,
            string? moduleName,
            string? mainTypeName,
            string? scriptClassName,
            IEnumerable<string>? usings,
            OptimizationLevel optimizationLevel,
            bool checkOverflow,
            bool allowUnsafe,
            string? cryptoKeyContainer,
            string? cryptoKeyFile,
            ImmutableArray<byte> cryptoPublicKey,
            bool? delaySign,
            Platform platform,
            ReportDiagnostic generalDiagnosticOption,
            int warningLevel,
            IEnumerable<KeyValuePair<string, ReportDiagnostic>>? specificDiagnosticOptions,
            bool concurrentBuild,
            bool deterministic,
            XmlReferenceResolver? xmlReferenceResolver,
            SourceReferenceResolver? sourceReferenceResolver,
            MetadataReferenceResolver? metadataReferenceResolver,
            AssemblyIdentityComparer? assemblyIdentityComparer,
            StrongNameProvider? strongNameProvider)
            : this(outputKind, false, moduleName, mainTypeName, scriptClassName, usings,
                   optimizationLevel, checkOverflow, allowUnsafe, cryptoKeyContainer, cryptoKeyFile,
                   cryptoPublicKey, delaySign, platform, generalDiagnosticOption, warningLevel,
                   specificDiagnosticOptions, concurrentBuild, deterministic,
                   xmlReferenceResolver: xmlReferenceResolver,
                   sourceReferenceResolver: sourceReferenceResolver,
                   metadataReferenceResolver: metadataReferenceResolver,
                   assemblyIdentityComparer: assemblyIdentityComparer,
                   strongNameProvider: strongNameProvider,
                   publicSign: false)
        {
        }

        // 1.0 BACKCOMPAT OVERLOAD -- DO NOT TOUCH
        [EditorBrowsable(EditorBrowsableState.Never)]
        public CSharpCompilationOptions(
            OutputKind outputKind,
            string? moduleName,
            string? mainTypeName,
            string? scriptClassName,
            IEnumerable<string>? usings,
            OptimizationLevel optimizationLevel,
            bool checkOverflow,
            bool allowUnsafe,
            string? cryptoKeyContainer,
            string? cryptoKeyFile,
            ImmutableArray<byte> cryptoPublicKey,
            bool? delaySign,
            Platform platform,
            ReportDiagnostic generalDiagnosticOption,
            int warningLevel,
            IEnumerable<KeyValuePair<string, ReportDiagnostic>>? specificDiagnosticOptions,
            bool concurrentBuild,
            XmlReferenceResolver? xmlReferenceResolver,
            SourceReferenceResolver? sourceReferenceResolver,
            MetadataReferenceResolver? metadataReferenceResolver,
            AssemblyIdentityComparer? assemblyIdentityComparer,
            StrongNameProvider? strongNameProvider)
            : this(outputKind, moduleName, mainTypeName, scriptClassName, usings,
                   optimizationLevel, checkOverflow, allowUnsafe,
                   cryptoKeyContainer, cryptoKeyFile, cryptoPublicKey, delaySign,
                   platform, generalDiagnosticOption, warningLevel,
                   specificDiagnosticOptions, concurrentBuild,
                   deterministic: false,
                   xmlReferenceResolver: xmlReferenceResolver,
                   sourceReferenceResolver: sourceReferenceResolver,
                   metadataReferenceResolver: metadataReferenceResolver,
                   assemblyIdentityComparer: assemblyIdentityComparer,
                   strongNameProvider: strongNameProvider)
        {
        }

        // Bad constructor -- DO NOT USE
        // Violates the rules for optional parameter overloads detailed at
        // https://github.com/dotnet/roslyn/blob/e8fdb391703dcb5712ff6a5b83d768d784cba4cf/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md
        [EditorBrowsable(EditorBrowsableState.Never)]
        public CSharpCompilationOptions(
            OutputKind outputKind,
#pragma warning disable IDE0060 // Remove unused parameter
            bool reportSuppressedDiagnostics,
#pragma warning restore IDE0060 // Remove unused parameter
            string? moduleName,
            string? mainTypeName,
            string? scriptClassName,
            IEnumerable<string>? usings,
            OptimizationLevel optimizationLevel,
            bool checkOverflow,
            bool allowUnsafe,
            string? cryptoKeyContainer,
            string? cryptoKeyFile,
            ImmutableArray<byte> cryptoPublicKey,
            bool? delaySign,
            Platform platform,
            ReportDiagnostic generalDiagnosticOption,
            int warningLevel,
            IEnumerable<KeyValuePair<string, ReportDiagnostic>>? specificDiagnosticOptions,
            bool concurrentBuild,
            bool deterministic,
            XmlReferenceResolver? xmlReferenceResolver,
            SourceReferenceResolver? sourceReferenceResolver,
            MetadataReferenceResolver? metadataReferenceResolver,
            AssemblyIdentityComparer? assemblyIdentityComparer,
            StrongNameProvider? strongNameProvider)
            : this(outputKind, false, moduleName, mainTypeName, scriptClassName, usings, optimizationLevel,
                   checkOverflow, allowUnsafe, cryptoKeyContainer, cryptoKeyFile, cryptoPublicKey,
                   delaySign, platform, generalDiagnosticOption, warningLevel,
                   specificDiagnosticOptions, concurrentBuild,
                   deterministic: deterministic,
                   currentLocalTime: default,
                   debugPlusMode: false,
                   xmlReferenceResolver: xmlReferenceResolver,
                   sourceReferenceResolver: sourceReferenceResolver,
                   syntaxTreeOptionsProvider: null,
                   metadataReferenceResolver: metadataReferenceResolver,
                   assemblyIdentityComparer: assemblyIdentityComparer,
                   strongNameProvider: strongNameProvider,
                   metadataImportOptions: MetadataImportOptions.Public,
                   referencesSupersedeLowerVersions: false,
                   publicSign: false,
                   topLevelBinderFlags: BinderFlags.None,
                   nullableContextOptions: NullableContextOptions.Disable)
        {
        }
    }
}
