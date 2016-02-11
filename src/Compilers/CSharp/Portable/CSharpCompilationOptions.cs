// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;
using System.Diagnostics;
using System.ComponentModel;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents various options that affect compilation, such as 
    /// whether to emit an executable or a library, whether to optimize
    /// generated code, and so on.
    /// </summary>
    public sealed class CSharpCompilationOptions : CompilationOptions, IEquatable<CSharpCompilationOptions>
    {
        /// <summary>
        /// Allow unsafe regions (i.e. unsafe modifiers on members and unsafe blocks).
        /// </summary>
        public bool AllowUnsafe { get; private set; }

        /// <summary>
        /// Global namespace usings.
        /// </summary>
        public ImmutableArray<string> Usings { get; private set; }

        /// <summary>
        /// Flags applied to the top-level binder created for each syntax tree in the compilation 
        /// as well as for the binder of global imports.
        /// </summary>
        internal BinderFlags TopLevelBinderFlags { get; private set; }

        // Defaults correspond to the compiler's defaults or indicate that the user did not specify when that is significant.
        // That's significant when one option depends on another's setting. SubsystemVersion depends on Platform and Target.
        public CSharpCompilationOptions(
            OutputKind outputKind,
            bool reportSuppressedDiagnostics = false,
            string moduleName = null,
            string mainTypeName = null,
            string scriptClassName = null,
            IEnumerable<string> usings = null,
            OptimizationLevel optimizationLevel = OptimizationLevel.Debug,
            bool checkOverflow = false,
            bool allowUnsafe = false,
            string cryptoKeyContainer = null,
            string cryptoKeyFile = null,
            ImmutableArray<byte> cryptoPublicKey = default(ImmutableArray<byte>),
            bool? delaySign = null,
            Platform platform = Platform.AnyCpu,
            ReportDiagnostic generalDiagnosticOption = ReportDiagnostic.Default,
            int warningLevel = 4,
            IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions = null,
            bool concurrentBuild = true,
            bool deterministic = false,
            XmlReferenceResolver xmlReferenceResolver = null,
            SourceReferenceResolver sourceReferenceResolver = null,
            MetadataReferenceResolver metadataReferenceResolver = null,
            AssemblyIdentityComparer assemblyIdentityComparer = null,
            StrongNameProvider strongNameProvider = null,
            bool publicSign = false)
            : this(outputKind, reportSuppressedDiagnostics, moduleName, mainTypeName, scriptClassName,
                   usings, optimizationLevel, checkOverflow, allowUnsafe,
                   cryptoKeyContainer, cryptoKeyFile, cryptoPublicKey, delaySign, platform,
                   generalDiagnosticOption, warningLevel,
                   specificDiagnosticOptions, concurrentBuild, deterministic,
                   extendedCustomDebugInformation: true,
                   debugPlusMode: false,
                   xmlReferenceResolver: xmlReferenceResolver,
                   sourceReferenceResolver: sourceReferenceResolver,
                   metadataReferenceResolver: metadataReferenceResolver,
                   assemblyIdentityComparer: assemblyIdentityComparer,
                   strongNameProvider: strongNameProvider,
                   metadataImportOptions: MetadataImportOptions.Public,
                   referencesSupersedeLowerVersions: false,
                   publicSign: publicSign,
                   topLevelBinderFlags: BinderFlags.None)
        {
        }

        // Expects correct arguments.
        internal CSharpCompilationOptions(
            OutputKind outputKind,
            bool reportSuppressedDiagnostics,
            string moduleName,
            string mainTypeName,
            string scriptClassName,
            IEnumerable<string> usings,
            OptimizationLevel optimizationLevel,
            bool checkOverflow,
            bool allowUnsafe,
            string cryptoKeyContainer,
            string cryptoKeyFile,
            ImmutableArray<byte> cryptoPublicKey,
            bool? delaySign,
            Platform platform,
            ReportDiagnostic generalDiagnosticOption,
            int warningLevel,
            IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions,
            bool concurrentBuild,
            bool deterministic,
            bool extendedCustomDebugInformation,
            bool debugPlusMode,
            XmlReferenceResolver xmlReferenceResolver,
            SourceReferenceResolver sourceReferenceResolver,
            MetadataReferenceResolver metadataReferenceResolver,
            AssemblyIdentityComparer assemblyIdentityComparer,
            StrongNameProvider strongNameProvider,
            MetadataImportOptions metadataImportOptions,
            bool referencesSupersedeLowerVersions,
            bool publicSign,
            BinderFlags topLevelBinderFlags)
            : base(outputKind, reportSuppressedDiagnostics, moduleName, mainTypeName, scriptClassName,
                   cryptoKeyContainer, cryptoKeyFile, cryptoPublicKey, delaySign, publicSign, optimizationLevel, checkOverflow,
                   platform, generalDiagnosticOption, warningLevel, specificDiagnosticOptions.ToImmutableDictionaryOrEmpty(),
                   concurrentBuild, deterministic, extendedCustomDebugInformation, debugPlusMode, xmlReferenceResolver,
                   sourceReferenceResolver, metadataReferenceResolver, assemblyIdentityComparer,
                   strongNameProvider, metadataImportOptions, referencesSupersedeLowerVersions)
        {
            this.Usings = usings.AsImmutableOrEmpty();
            this.AllowUnsafe = allowUnsafe;
            this.TopLevelBinderFlags = topLevelBinderFlags;
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
            extendedCustomDebugInformation: other.ExtendedCustomDebugInformation,
            debugPlusMode: other.DebugPlusMode,
            xmlReferenceResolver: other.XmlReferenceResolver,
            sourceReferenceResolver: other.SourceReferenceResolver,
            metadataReferenceResolver: other.MetadataReferenceResolver,
            assemblyIdentityComparer: other.AssemblyIdentityComparer,
            strongNameProvider: other.StrongNameProvider,
            metadataImportOptions: other.MetadataImportOptions,
            referencesSupersedeLowerVersions: other.ReferencesSupersedeLowerVersions,
            reportSuppressedDiagnostics: other.ReportSuppressedDiagnostics,
            publicSign: other.PublicSign,
            topLevelBinderFlags: other.TopLevelBinderFlags)
        {
        }

        internal CSharpCompilationOptions WithTopLevelBinderFlags(BinderFlags flags)
        {
            return (flags == TopLevelBinderFlags) ? this : new CSharpCompilationOptions(this) { TopLevelBinderFlags = flags };
        }

        internal override ImmutableArray<string> GetImports() => Usings;

        public new CSharpCompilationOptions WithOutputKind(OutputKind kind)
        {
            if (kind == this.OutputKind)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { OutputKind = kind };
        }

        public CSharpCompilationOptions WithModuleName(string moduleName)
        {
            if (moduleName == this.ModuleName)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { ModuleName = moduleName };
        }

        public CSharpCompilationOptions WithScriptClassName(string name)
        {
            if (name == this.ScriptClassName)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { ScriptClassName = name };
        }

        public CSharpCompilationOptions WithMainTypeName(string name)
        {
            if (name == this.MainTypeName)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { MainTypeName = name };
        }

        public CSharpCompilationOptions WithCryptoKeyContainer(string name)
        {
            if (name == this.CryptoKeyContainer)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { CryptoKeyContainer = name };
        }

        public CSharpCompilationOptions WithCryptoKeyFile(string path)
        {
            if (path == this.CryptoKeyFile)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { CryptoKeyFile = path };
        }

        public CSharpCompilationOptions WithCryptoPublicKey(ImmutableArray<byte> value)
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

        public CSharpCompilationOptions WithDelaySign(bool? value)
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

        public CSharpCompilationOptions WithUsings(IEnumerable<string> usings) =>
            new CSharpCompilationOptions(this) { Usings = usings.AsImmutableOrEmpty() };

        public CSharpCompilationOptions WithUsings(params string[] usings) => WithUsings((IEnumerable<string>)usings);

        public new CSharpCompilationOptions WithOptimizationLevel(OptimizationLevel value)
        {
            if (value == this.OptimizationLevel)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { OptimizationLevel = value };
        }

        public CSharpCompilationOptions WithOverflowChecks(bool enabled)
        {
            if (enabled == this.CheckOverflow)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { CheckOverflow = enabled };
        }

        public CSharpCompilationOptions WithAllowUnsafe(bool enabled)
        {
            if (enabled == this.AllowUnsafe)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { AllowUnsafe = enabled };
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

        protected override CompilationOptions CommonWithSpecificDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic> specificDiagnosticOptions) =>
            WithSpecificDiagnosticOptions(specificDiagnosticOptions);

        protected override CompilationOptions CommonWithSpecificDiagnosticOptions(IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions) =>
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

        public new CSharpCompilationOptions WithSpecificDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic> values)
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

        public new CSharpCompilationOptions WithSpecificDiagnosticOptions(IEnumerable<KeyValuePair<string, ReportDiagnostic>> values) =>
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

        public CSharpCompilationOptions WithConcurrentBuild(bool concurrentBuild)
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

        internal CSharpCompilationOptions WithExtendedCustomDebugInformation(bool extendedCustomDebugInformation)
        {
            if (extendedCustomDebugInformation == this.ExtendedCustomDebugInformation)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { ExtendedCustomDebugInformation_internal_protected_set = extendedCustomDebugInformation };
        }

        internal CSharpCompilationOptions WithDebugPlusMode(bool debugPlusMode)
        {
            if (debugPlusMode == this.DebugPlusMode)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { DebugPlusMode_internal_protected_set = debugPlusMode };
        }

        internal CSharpCompilationOptions WithMetadataImportOptions(MetadataImportOptions value)
        {
            if (value == this.MetadataImportOptions)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { MetadataImportOptions_internal_protected_set = value };
        }

        internal CSharpCompilationOptions WithReferencesSupersedeLowerVersions(bool value)
        {
            if (value == this.ReferencesSupersedeLowerVersions)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { ReferencesSupersedeLowerVersions_internal_protected_set = value };
        }

        public new CSharpCompilationOptions WithXmlReferenceResolver(XmlReferenceResolver resolver)
        {
            if (ReferenceEquals(resolver, this.XmlReferenceResolver))
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { XmlReferenceResolver = resolver };
        }

        public new CSharpCompilationOptions WithSourceReferenceResolver(SourceReferenceResolver resolver)
        {
            if (ReferenceEquals(resolver, this.SourceReferenceResolver))
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { SourceReferenceResolver = resolver };
        }

        public new CSharpCompilationOptions WithMetadataReferenceResolver(MetadataReferenceResolver resolver)
        {
            if (ReferenceEquals(resolver, this.MetadataReferenceResolver))
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { MetadataReferenceResolver = resolver };
        }

        public new CSharpCompilationOptions WithAssemblyIdentityComparer(AssemblyIdentityComparer comparer)
        {
            comparer = comparer ?? AssemblyIdentityComparer.Default;

            if (ReferenceEquals(comparer, this.AssemblyIdentityComparer))
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { AssemblyIdentityComparer = comparer };
        }

        public new CSharpCompilationOptions WithStrongNameProvider(StrongNameProvider provider)
        {
            if (ReferenceEquals(provider, this.StrongNameProvider))
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { StrongNameProvider = provider };
        }

        protected override CompilationOptions CommonWithDeterministic(bool deterministic) => WithDeterministic(deterministic);

        protected override CompilationOptions CommonWithOutputKind(OutputKind kind) => WithOutputKind(kind);

        protected override CompilationOptions CommonWithPlatform(Platform platform) => WithPlatform(platform);

        protected override CompilationOptions CommonWithPublicSign(bool publicSign) => WithPublicSign(publicSign);

        protected override CompilationOptions CommonWithOptimizationLevel(OptimizationLevel value) => WithOptimizationLevel(value);

        protected override CompilationOptions CommonWithAssemblyIdentityComparer(AssemblyIdentityComparer comparer) =>
            WithAssemblyIdentityComparer(comparer);

        protected override CompilationOptions CommonWithXmlReferenceResolver(XmlReferenceResolver resolver) =>
            WithXmlReferenceResolver(resolver);

        protected override CompilationOptions CommonWithSourceReferenceResolver(SourceReferenceResolver resolver) =>
            WithSourceReferenceResolver(resolver);

        protected override CompilationOptions CommonWithMetadataReferenceResolver(MetadataReferenceResolver resolver) =>
            WithMetadataReferenceResolver(resolver);

        protected override CompilationOptions CommonWithStrongNameProvider(StrongNameProvider provider) =>
            WithStrongNameProvider(provider);

        [Obsolete]
        protected override CompilationOptions CommonWithFeatures(ImmutableArray<string> features)
        {
            throw new NotImplementedException();
        }

        internal override void ValidateOptions(ArrayBuilder<Diagnostic> builder)
        {
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
                Exception e = MetadataHelpers.CheckAssemblyOrModuleName(ModuleName, nameof(ModuleName));
                if (e != null)
                {
                    builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadCompilationOption, e.Message));
                }
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

            if (WarningLevel < 0 || WarningLevel > 4)
            {
                builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadCompilationOptionValue, nameof(WarningLevel), WarningLevel));
            }

            if (Usings != null && Usings.Any(u => !u.IsValidClrNamespaceName()))
            {
                builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadCompilationOptionValue, nameof(Usings), Usings.Where(u => !u.IsValidClrNamespaceName()).First() ?? "null"));
            }

            if (Platform == Platform.AnyCpu32BitPreferred && OutputKind.IsValid() && !(OutputKind == OutputKind.ConsoleApplication || OutputKind == OutputKind.WindowsApplication || OutputKind == OutputKind.WindowsRuntimeApplication))
            {
                builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadPrefer32OnLib));
            }

            // TODO: add check for 
            //          (kind == 'arm' || kind == 'appcontainer' || kind == 'winmdobj') &&
            //          (version >= "6.2")

            if (!CryptoPublicKey.IsEmpty)
            {
                if (CryptoKeyFile != null)
                {
                    builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_MutuallyExclusiveOptions, nameof(CryptoPublicKey), nameof(CryptoKeyFile)));
                }

                if (CryptoKeyContainer != null)
                {
                    builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_MutuallyExclusiveOptions, nameof(CryptoPublicKey), nameof(CryptoKeyContainer)));
                }
            }

            if (PublicSign && DelaySign == true)
            {
                builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_MutuallyExclusiveOptions, nameof(PublicSign), nameof(DelaySign)));
            }
        }

        public bool Equals(CSharpCompilationOptions other)
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
                   this.TopLevelBinderFlags == other.TopLevelBinderFlags &&
                   (this.Usings == null ? other.Usings == null : this.Usings.SequenceEqual(other.Usings, StringComparer.Ordinal));
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as CSharpCompilationOptions);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(base.GetHashCodeHelper(),
                   Hash.Combine(this.AllowUnsafe,
                   Hash.Combine(Hash.CombineValues(this.Usings, StringComparer.Ordinal),
                   Hash.Combine(TopLevelBinderFlags.GetHashCode(), 0))));
        }

        internal override Diagnostic FilterDiagnostic(Diagnostic diagnostic)
        {
            return CSharpDiagnosticFilter.Filter(diagnostic, WarningLevel, GeneralDiagnosticOption, SpecificDiagnosticOptions);
        }

        // 1.1 BACKCOMPAT OVERLOAD -- DO NOT TOUCH
        [EditorBrowsable(EditorBrowsableState.Never)]
        public CSharpCompilationOptions(
            OutputKind outputKind,
            string moduleName,
            string mainTypeName,
            string scriptClassName,
            IEnumerable<string> usings,
            OptimizationLevel optimizationLevel,
            bool checkOverflow,
            bool allowUnsafe,
            string cryptoKeyContainer,
            string cryptoKeyFile,
            ImmutableArray<byte> cryptoPublicKey,
            bool? delaySign,
            Platform platform,
            ReportDiagnostic generalDiagnosticOption,
            int warningLevel,
            IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions,
            bool concurrentBuild,
            bool deterministic,
            XmlReferenceResolver xmlReferenceResolver,
            SourceReferenceResolver sourceReferenceResolver,
            MetadataReferenceResolver metadataReferenceResolver,
            AssemblyIdentityComparer assemblyIdentityComparer,
            StrongNameProvider strongNameProvider)
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
            string moduleName,
            string mainTypeName,
            string scriptClassName,
            IEnumerable<string> usings,
            OptimizationLevel optimizationLevel,
            bool checkOverflow,
            bool allowUnsafe,
            string cryptoKeyContainer,
            string cryptoKeyFile,
            ImmutableArray<byte> cryptoPublicKey,
            bool? delaySign,
            Platform platform,
            ReportDiagnostic generalDiagnosticOption,
            int warningLevel,
            IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions,
            bool concurrentBuild,
            XmlReferenceResolver xmlReferenceResolver,
            SourceReferenceResolver sourceReferenceResolver,
            MetadataReferenceResolver metadataReferenceResolver,
            AssemblyIdentityComparer assemblyIdentityComparer,
            StrongNameProvider strongNameProvider)
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
            bool reportSuppressedDiagnostics,
            string moduleName,
            string mainTypeName,
            string scriptClassName,
            IEnumerable<string> usings,
            OptimizationLevel optimizationLevel,
            bool checkOverflow,
            bool allowUnsafe,
            string cryptoKeyContainer,
            string cryptoKeyFile,
            ImmutableArray<byte> cryptoPublicKey,
            bool? delaySign,
            Platform platform,
            ReportDiagnostic generalDiagnosticOption,
            int warningLevel,
            IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions,
            bool concurrentBuild,
            bool deterministic,
            XmlReferenceResolver xmlReferenceResolver,
            SourceReferenceResolver sourceReferenceResolver,
            MetadataReferenceResolver metadataReferenceResolver,
            AssemblyIdentityComparer assemblyIdentityComparer,
            StrongNameProvider strongNameProvider)
            : this(outputKind, false, moduleName, mainTypeName, scriptClassName, usings, optimizationLevel,
                   checkOverflow, allowUnsafe, cryptoKeyContainer, cryptoKeyFile, cryptoPublicKey,
                   delaySign, platform, generalDiagnosticOption, warningLevel,
                   specificDiagnosticOptions, concurrentBuild,
                   deterministic: deterministic,
                   extendedCustomDebugInformation: true,
                   debugPlusMode: false,
                   xmlReferenceResolver: xmlReferenceResolver,
                   sourceReferenceResolver: sourceReferenceResolver,
                   metadataReferenceResolver: metadataReferenceResolver,
                   assemblyIdentityComparer: assemblyIdentityComparer,
                   strongNameProvider: strongNameProvider,
                   metadataImportOptions: MetadataImportOptions.Public,
                   referencesSupersedeLowerVersions: false,
                   publicSign: false,
                   topLevelBinderFlags: BinderFlags.None)
        {
        }
    }
}
