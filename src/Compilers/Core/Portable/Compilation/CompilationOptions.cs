// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents compilation options common to C# and VB.
    /// </summary>
    public abstract class CompilationOptions
    {
        /// <summary>
        /// The kind of assembly generated when emitted.
        /// </summary>
        public OutputKind OutputKind { get; protected set; }

        /// <summary>
        /// Name of the primary module, or null if a default name should be used.
        /// </summary>
        /// <remarks>
        /// The name usually (but not necessarily) includes an extension, e.g. "MyModule.dll".
        /// 
        /// If <see cref="ModuleName"/> is null the actual name written to metadata  
        /// is derived from the name of the compilation (<see cref="Compilation.AssemblyName"/>)
        /// by appending a default extension for <see cref="OutputKind"/>.
        /// </remarks>
        public string? ModuleName { get; protected set; }

        /// <summary>
        /// The full name of a global implicit class (script class). This class implicitly encapsulates top-level statements, 
        /// type declarations, and member declarations. Could be a namespace qualified name.
        /// </summary>
        public string? ScriptClassName { get; protected set; }

        /// <summary>
        /// The full name of a type that declares static Main method. Must be a valid non-generic namespace-qualified name.
        /// Null if any static Main method is a candidate for an entry point.
        /// </summary>
        public string? MainTypeName { get; protected set; }

        // Note that we avoid using default(ImmutableArray<byte>) for unspecified value since 
        // such value is currently not serializable by JSON serializer.

        /// <summary>
        /// Specifies public key used to generate strong name for the compilation assembly, or empty if not specified.
        /// </summary>
        /// <remarks>
        /// If specified the values of <see cref="CryptoKeyFile"/> and <see cref="CryptoKeyContainer"/>
        /// must be null. If <see cref="PublicSign"/> is true the assembly is marked as fully signed
        /// but only signed with the public key (aka "OSS signing").
        /// </remarks>
        public ImmutableArray<byte> CryptoPublicKey { get; protected set; }

        /// <summary>
        /// The name of the file containing the public and private keys to use to generate strong name of the 
        /// compilation assembly and to sign it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// To sign the output supply either one of <see cref="CryptoKeyFile"/> or <see cref="CryptoKeyContainer"/>.
        /// but not both. If both are specified <see cref="CryptoKeyContainer"/> is ignored.
        /// </para>
        /// <para>
        /// If <see cref="PublicSign" /> is also set, <see cref="CryptoKeyFile"/> must be the absolute
        /// path to key file.
        /// </para>
        /// </remarks>
        public string? CryptoKeyFile { get; protected set; }

        /// <summary>
        /// The CSP container containing the key with which to sign the output.
        /// </summary>
        /// <remarks>
        /// <para>
        /// To sign the output supply either one of <see cref="CryptoKeyFile"/> or <see cref="CryptoKeyContainer"/>.
        /// but not both. If both are specified <see cref="CryptoKeyContainer"/> is ignored.
        /// </para>
        /// <para>
        /// This setting is obsolete and only supported on Microsoft Windows platform.
        /// Use <see cref="CryptoPublicKey"/> to generate assemblies with strong name and 
        /// a signing tool (Microsoft .NET Framework Strong Name Utility (sn.exe) or equivalent) to sign them.
        /// </para>
        /// </remarks>
        public string? CryptoKeyContainer { get; protected set; }

        /// <summary>
        /// Mark the compilation assembly as delay-signed.
        /// </summary>
        /// <remarks>
        /// If true the resulting assembly is marked as delay signed.
        /// 
        /// If false and <see cref="CryptoPublicKey"/>, <see cref="CryptoKeyFile"/>, or <see cref="CryptoKeyContainer"/> is specified
        /// or attribute System.Reflection.AssemblyKeyFileAttribute or System.Reflection.AssemblyKeyNameAttribute is applied to the 
        /// compilation assembly in source the resulting assembly is signed accordingly to the specified values/attributes.
        /// 
        /// If null the semantics is specified by the value of attribute System.Reflection.AssemblyDelaySignAttribute 
        /// applied to the compilation assembly in source. If the attribute is not present the value defaults to "false".
        /// </remarks>
        public bool? DelaySign { get; protected set; }

        /// <summary>
        /// Mark the compilation assembly as fully signed, but only sign with the public key.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If true, the assembly is marked as signed, but is only signed with the public key.
        /// </para>
        /// <para>
        /// The key must be provided through either an absolute path in <see cref="CryptoKeyFile"/>
        /// or directly via <see cref="CryptoPublicKey" />.
        /// </para>
        /// </remarks>
        public bool PublicSign { get; protected set; }

        /// <summary>
        /// Whether bounds checking on integer arithmetic is enforced by default or not.
        /// </summary>
        public bool CheckOverflow { get; protected set; }

        /// <summary>
        /// Specifies which version of the common language runtime (CLR) can run the assembly.
        /// </summary>
        public Platform Platform { get; protected set; }

        /// <summary>
        /// Specifies whether or not optimizations should be performed on the output IL.
        /// This is independent of whether or not PDB information is generated.
        /// </summary>
        public OptimizationLevel OptimizationLevel { get; protected set; }

        /// <summary>
        /// Global warning report option
        /// </summary>
        public ReportDiagnostic GeneralDiagnosticOption { get; protected set; }

        /// <summary>
        /// Global warning level (a non-negative integer).
        /// </summary>
        public int WarningLevel { get; protected set; }

        /// <summary>
        /// Specifies whether building compilation may use multiple threads.
        /// </summary>
        public bool ConcurrentBuild { get; protected set; }

        /// <summary>
        /// Specifies whether the compilation should be deterministic.
        /// </summary>
        public bool Deterministic { get; protected set; }

        /// <summary>
        /// Used for time-based version generation when <see cref="System.Reflection.AssemblyVersionAttribute"/> contains a wildcard.
        /// If equal to default(<see cref="DateTime"/>) the actual current local time will be used.
        /// </summary>
        internal DateTime CurrentLocalTime { get; private protected set; }

        /// <summary>
        /// Emit mode that favors debuggability. 
        /// </summary>
        internal bool DebugPlusMode { get; set; }

        /// <summary>
        /// Specifies whether to import members with accessibility other than public or protected by default. 
        /// Default value is <see cref="MetadataImportOptions.Public"/>. The value specified is not going to 
        /// affect correctness of analysis performed by compilers because all members needed for correctness 
        /// are going to be imported regardless. This setting can force compilation to import members that it 
        /// normally doesn't.
        /// </summary>
        public MetadataImportOptions MetadataImportOptions { get; protected set; }

        /// <summary>
        /// Apply additional disambiguation rules during resolution of referenced assemblies.
        /// </summary>
        internal bool ReferencesSupersedeLowerVersions { get; private protected set; }

        /// <summary>
        /// Modifies the incoming diagnostic, for example escalating its severity, or discarding it (returning null) based on the compilation options.
        /// </summary>
        /// <param name="diagnostic"></param>
        /// <returns>The modified diagnostic, or null</returns>
        internal abstract Diagnostic? FilterDiagnostic(Diagnostic diagnostic, CancellationToken cancellationToken);

        /// <summary>
        /// Warning report option for each warning.
        /// </summary>
        public ImmutableDictionary<string, ReportDiagnostic> SpecificDiagnosticOptions { get; protected set; }

        /// <summary>
        /// Provider to retrieve options for particular syntax trees.
        /// </summary>
        public SyntaxTreeOptionsProvider? SyntaxTreeOptionsProvider { get; protected set; }

        /// <summary>
        /// Whether diagnostics suppressed in source, i.e. <see cref="Diagnostic.IsSuppressed"/> is true, should be reported.
        /// </summary>
        public bool ReportSuppressedDiagnostics { get; protected set; }

        /// <summary>
        /// Resolves paths to metadata references specified in source via #r directives.
        /// Null if the compilation can't contain references to metadata other than those explicitly passed to its factory (such as #r directives in sources). 
        /// </summary>
        public MetadataReferenceResolver? MetadataReferenceResolver { get; protected set; }

        /// <summary>
        /// Gets the resolver for resolving XML document references for the compilation.
        /// Null if the compilation is not allowed to contain XML file references, such as XML doc comment include tags and permission sets stored in an XML file.
        /// </summary>
        public XmlReferenceResolver? XmlReferenceResolver { get; protected set; }

        /// <summary>
        /// Gets the resolver for resolving source document references for the compilation.
        /// Null if the compilation is not allowed to contain source file references, such as #line pragmas and #load directives.
        /// </summary>
        public SourceReferenceResolver? SourceReferenceResolver { get; protected set; }

        /// <summary>
        /// Provides strong name and signature the source assembly.
        /// Null if assembly signing is not supported.
        /// </summary>
        public StrongNameProvider? StrongNameProvider { get; protected set; }

        /// <summary>
        /// Used to compare assembly identities. May implement unification and portability policies specific to the target platform.
        /// <see cref="AssemblyIdentityComparer.Default"/> if not specified.
        /// </summary>
        public AssemblyIdentityComparer AssemblyIdentityComparer { get; protected set; }

        /// <summary>
        /// Gets the default nullable context state in this compilation.
        /// </summary>
        /// <remarks>
        /// This context does not apply to files that are marked as generated. Nullable is off
        /// by default in those locations.
        /// </remarks>
        public abstract NullableContextOptions NullableContextOptions { get; protected set; }

        /// <summary>
        /// A set of strings designating experimental compiler features that are to be enabled.
        /// </summary>
        [Obsolete]
        protected internal ImmutableArray<string> Features
        {
            get
            {
                throw new NotImplementedException();
            }
            protected set
            {
                throw new NotImplementedException();
            }
        }

        private readonly Lazy<ImmutableArray<Diagnostic>> _lazyErrors;

        private int _hashCode;

        // Expects correct arguments.
        internal CompilationOptions(
            OutputKind outputKind,
            bool reportSuppressedDiagnostics,
            string? moduleName,
            string? mainTypeName,
            string? scriptClassName,
            string? cryptoKeyContainer,
            string? cryptoKeyFile,
            ImmutableArray<byte> cryptoPublicKey,
            bool? delaySign,
            bool publicSign,
            OptimizationLevel optimizationLevel,
            bool checkOverflow,
            Platform platform,
            ReportDiagnostic generalDiagnosticOption,
            int warningLevel,
            ImmutableDictionary<string, ReportDiagnostic> specificDiagnosticOptions,
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
            bool referencesSupersedeLowerVersions)
        {
            this.OutputKind = outputKind;
            this.ModuleName = moduleName;
            this.MainTypeName = mainTypeName;
            this.ScriptClassName = scriptClassName ?? WellKnownMemberNames.DefaultScriptClassName;
            this.CryptoKeyContainer = cryptoKeyContainer;
            this.CryptoKeyFile = string.IsNullOrEmpty(cryptoKeyFile) ? null : cryptoKeyFile;
            this.CryptoPublicKey = cryptoPublicKey.NullToEmpty();
            this.DelaySign = delaySign;
            this.CheckOverflow = checkOverflow;
            this.Platform = platform;
            this.GeneralDiagnosticOption = generalDiagnosticOption;
            this.WarningLevel = warningLevel;
            this.SpecificDiagnosticOptions = specificDiagnosticOptions;
            this.ReportSuppressedDiagnostics = reportSuppressedDiagnostics;
            this.OptimizationLevel = optimizationLevel;
            this.ConcurrentBuild = concurrentBuild;
            this.Deterministic = deterministic;
            this.CurrentLocalTime = currentLocalTime;
            this.DebugPlusMode = debugPlusMode;
            this.XmlReferenceResolver = xmlReferenceResolver;
            this.SourceReferenceResolver = sourceReferenceResolver;
            this.SyntaxTreeOptionsProvider = syntaxTreeOptionsProvider;
            this.MetadataReferenceResolver = metadataReferenceResolver;
            this.StrongNameProvider = strongNameProvider;
            this.AssemblyIdentityComparer = assemblyIdentityComparer ?? AssemblyIdentityComparer.Default;
            this.MetadataImportOptions = metadataImportOptions;
            this.ReferencesSupersedeLowerVersions = referencesSupersedeLowerVersions;
            this.PublicSign = publicSign;

            _lazyErrors = new Lazy<ImmutableArray<Diagnostic>>(() =>
            {
                var builder = ArrayBuilder<Diagnostic>.GetInstance();
                ValidateOptions(builder);
                return builder.ToImmutableAndFree();
            });
        }

        internal bool CanReuseCompilationReferenceManager(CompilationOptions other)
        {
            // This condition has to include all options the Assembly Manager depends on when binding references.
            // In addition, the assembly name is determined based upon output kind. It is special for netmodules.
            // Can't reuse when file resolver or identity comparers change.
            // Can reuse even if StrongNameProvider changes. When resolving a cyclic reference only the simple name is considered, not the strong name.
            return this.MetadataImportOptions == other.MetadataImportOptions
                && this.ReferencesSupersedeLowerVersions == other.ReferencesSupersedeLowerVersions
                && this.OutputKind.IsNetModule() == other.OutputKind.IsNetModule()
                && object.Equals(this.XmlReferenceResolver, other.XmlReferenceResolver)
                && object.Equals(this.MetadataReferenceResolver, other.MetadataReferenceResolver)
                && object.Equals(this.AssemblyIdentityComparer, other.AssemblyIdentityComparer);
        }

        /// <summary>
        /// Gets the source language ("C#" or "Visual Basic").
        /// </summary>
        public abstract string Language { get; }

        internal bool EnableEditAndContinue
        {
            get
            {
                return OptimizationLevel == OptimizationLevel.Debug;
            }
        }

        internal static bool IsValidFileAlignment(int value)
        {
            switch (value)
            {
                case 512:
                case 1024:
                case 2048:
                case 4096:
                case 8192:
                    return true;

                default:
                    return false;
            }
        }

        internal abstract ImmutableArray<string> GetImports();

        /// <summary>
        /// Creates a new options instance with the specified general diagnostic option.
        /// </summary>
        public CompilationOptions WithGeneralDiagnosticOption(ReportDiagnostic value)
        {
            return CommonWithGeneralDiagnosticOption(value);
        }

        /// <summary>
        /// Creates a new options instance with the specified diagnostic-specific options.
        /// </summary>
        public CompilationOptions WithSpecificDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic>? value)
        {
            return CommonWithSpecificDiagnosticOptions(value);
        }

        /// <summary>
        /// Creates a new options instance with the specified diagnostic-specific options.
        /// </summary>
        public CompilationOptions WithSpecificDiagnosticOptions(IEnumerable<KeyValuePair<string, ReportDiagnostic>> value)
        {
            return CommonWithSpecificDiagnosticOptions(value);
        }

        /// <summary>
        /// Creates a new options instance with the specified suppressed diagnostics reporting option.
        /// </summary>
        public CompilationOptions WithReportSuppressedDiagnostics(bool value)
        {
            return CommonWithReportSuppressedDiagnostics(value);
        }

        /// <summary>
        /// Creates a new options instance with the concurrent build property set accordingly.
        /// </summary>
        public CompilationOptions WithConcurrentBuild(bool concurrent)
        {
            return CommonWithConcurrentBuild(concurrent);
        }

        /// <summary>
        /// Creates a new options instance with the deterministic property set accordingly.
        /// </summary>
        public CompilationOptions WithDeterministic(bool deterministic)
        {
            return CommonWithDeterministic(deterministic);
        }

        /// <summary>
        /// Creates a new options instance with the specified output kind.
        /// </summary>
        public CompilationOptions WithOutputKind(OutputKind kind)
        {
            return CommonWithOutputKind(kind);
        }

        /// <summary>
        /// Creates a new options instance with the specified platform.
        /// </summary>
        public CompilationOptions WithPlatform(Platform platform)
        {
            return CommonWithPlatform(platform);
        }

        /// <summary>
        /// Creates a new options instance with the specified public sign setting.
        /// </summary>
        public CompilationOptions WithPublicSign(bool publicSign) => CommonWithPublicSign(publicSign);

        /// <summary>
        /// Creates a new options instance with optimizations enabled or disabled.
        /// </summary>
        public CompilationOptions WithOptimizationLevel(OptimizationLevel value)
        {
            return CommonWithOptimizationLevel(value);
        }

        public CompilationOptions WithXmlReferenceResolver(XmlReferenceResolver? resolver)
        {
            return CommonWithXmlReferenceResolver(resolver);
        }

        public CompilationOptions WithSourceReferenceResolver(SourceReferenceResolver? resolver)
        {
            return CommonWithSourceReferenceResolver(resolver);
        }

        public CompilationOptions WithSyntaxTreeOptionsProvider(SyntaxTreeOptionsProvider? provider)
        {
            return CommonWithSyntaxTreeOptionsProvider(provider);
        }

        public CompilationOptions WithMetadataReferenceResolver(MetadataReferenceResolver? resolver)
        {
            return CommonWithMetadataReferenceResolver(resolver);
        }

        public CompilationOptions WithAssemblyIdentityComparer(AssemblyIdentityComparer comparer)
        {
            return CommonWithAssemblyIdentityComparer(comparer);
        }

        public CompilationOptions WithStrongNameProvider(StrongNameProvider? provider)
        {
            return CommonWithStrongNameProvider(provider);
        }

        public CompilationOptions WithModuleName(string? moduleName)
        {
            return CommonWithModuleName(moduleName);
        }

        public CompilationOptions WithMainTypeName(string? mainTypeName)
        {
            return CommonWithMainTypeName(mainTypeName);
        }

        public CompilationOptions WithScriptClassName(string scriptClassName)
        {
            return CommonWithScriptClassName(scriptClassName);
        }

        public CompilationOptions WithCryptoKeyContainer(string? cryptoKeyContainer)
        {
            return CommonWithCryptoKeyContainer(cryptoKeyContainer);
        }

        public CompilationOptions WithCryptoKeyFile(string? cryptoKeyFile)
        {
            return CommonWithCryptoKeyFile(cryptoKeyFile);
        }

        public CompilationOptions WithCryptoPublicKey(ImmutableArray<byte> cryptoPublicKey)
        {
            return CommonWithCryptoPublicKey(cryptoPublicKey);
        }

        public CompilationOptions WithDelaySign(bool? delaySign)
        {
            return CommonWithDelaySign(delaySign);
        }

        public CompilationOptions WithOverflowChecks(bool checkOverflow)
        {
            return CommonWithCheckOverflow(checkOverflow);
        }

        public CompilationOptions WithMetadataImportOptions(MetadataImportOptions value) => CommonWithMetadataImportOptions(value);

        protected abstract CompilationOptions CommonWithConcurrentBuild(bool concurrent);
        protected abstract CompilationOptions CommonWithDeterministic(bool deterministic);
        protected abstract CompilationOptions CommonWithOutputKind(OutputKind kind);
        protected abstract CompilationOptions CommonWithPlatform(Platform platform);
        protected abstract CompilationOptions CommonWithPublicSign(bool publicSign);
        protected abstract CompilationOptions CommonWithOptimizationLevel(OptimizationLevel value);
        protected abstract CompilationOptions CommonWithXmlReferenceResolver(XmlReferenceResolver? resolver);
        protected abstract CompilationOptions CommonWithSourceReferenceResolver(SourceReferenceResolver? resolver);
        protected abstract CompilationOptions CommonWithSyntaxTreeOptionsProvider(SyntaxTreeOptionsProvider? resolver);
        protected abstract CompilationOptions CommonWithMetadataReferenceResolver(MetadataReferenceResolver? resolver);
        protected abstract CompilationOptions CommonWithAssemblyIdentityComparer(AssemblyIdentityComparer? comparer);
        protected abstract CompilationOptions CommonWithStrongNameProvider(StrongNameProvider? provider);
        protected abstract CompilationOptions CommonWithGeneralDiagnosticOption(ReportDiagnostic generalDiagnosticOption);
        protected abstract CompilationOptions CommonWithSpecificDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic>? specificDiagnosticOptions);
        protected abstract CompilationOptions CommonWithSpecificDiagnosticOptions(IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions);
        protected abstract CompilationOptions CommonWithReportSuppressedDiagnostics(bool reportSuppressedDiagnostics);
        protected abstract CompilationOptions CommonWithModuleName(string? moduleName);
        protected abstract CompilationOptions CommonWithMainTypeName(string? mainTypeName);
        protected abstract CompilationOptions CommonWithScriptClassName(string scriptClassName);
        protected abstract CompilationOptions CommonWithCryptoKeyContainer(string? cryptoKeyContainer);
        protected abstract CompilationOptions CommonWithCryptoKeyFile(string? cryptoKeyFile);
        protected abstract CompilationOptions CommonWithCryptoPublicKey(ImmutableArray<byte> cryptoPublicKey);
        protected abstract CompilationOptions CommonWithDelaySign(bool? delaySign);
        protected abstract CompilationOptions CommonWithCheckOverflow(bool checkOverflow);
        protected abstract CompilationOptions CommonWithMetadataImportOptions(MetadataImportOptions value);

        [Obsolete]
        protected abstract CompilationOptions CommonWithFeatures(ImmutableArray<string> features);

        internal abstract DeterministicKeyBuilder CreateDeterministicKeyBuilder();

        /// <summary>
        /// Performs validation of options compatibilities and generates diagnostics if needed
        /// </summary>
        internal abstract void ValidateOptions(ArrayBuilder<Diagnostic> builder);

        internal void ValidateOptions(ArrayBuilder<Diagnostic> builder, CommonMessageProvider messageProvider)
        {
            if (!CryptoPublicKey.IsEmpty)
            {
                if (CryptoKeyFile != null)
                {
                    builder.Add(messageProvider.CreateDiagnostic(messageProvider.ERR_MutuallyExclusiveOptions,
                        Location.None, nameof(CryptoPublicKey), nameof(CryptoKeyFile)));
                }

                if (CryptoKeyContainer != null)
                {
                    builder.Add(messageProvider.CreateDiagnostic(messageProvider.ERR_MutuallyExclusiveOptions,
                        Location.None, nameof(CryptoPublicKey), nameof(CryptoKeyContainer)));
                }
            }

            if (PublicSign)
            {
                if (CryptoKeyFile != null && !PathUtilities.IsAbsolute(CryptoKeyFile))
                {
                    builder.Add(messageProvider.CreateDiagnostic(messageProvider.ERR_OptionMustBeAbsolutePath,
                        Location.None, nameof(CryptoKeyFile)));
                }

                if (CryptoKeyContainer != null)
                {
                    builder.Add(messageProvider.CreateDiagnostic(messageProvider.ERR_MutuallyExclusiveOptions,
                        Location.None, nameof(PublicSign), nameof(CryptoKeyContainer)));
                }

                if (DelaySign == true)
                {
                    builder.Add(messageProvider.CreateDiagnostic(messageProvider.ERR_MutuallyExclusiveOptions,
                        Location.None, nameof(PublicSign), nameof(DelaySign)));
                }
            }
        }

        /// <summary>
        /// Errors collection related to an incompatible set of compilation options
        /// </summary>
        public ImmutableArray<Diagnostic> Errors
        {
            get { return _lazyErrors.Value; }
        }

        public abstract override bool Equals(object? obj);

        protected bool EqualsHelper([NotNullWhen(true)] CompilationOptions? other)
        {
            if (object.ReferenceEquals(other, null))
            {
                return false;
            }

            // NOTE: StringComparison.Ordinal is used for type name comparisons, even for VB.  That's because
            // a change in the canonical case should still change the option.
            bool equal =
                   this.CheckOverflow == other.CheckOverflow &&
                   this.ConcurrentBuild == other.ConcurrentBuild &&
                   this.Deterministic == other.Deterministic &&
                   this.CurrentLocalTime == other.CurrentLocalTime &&
                   this.DebugPlusMode == other.DebugPlusMode &&
                   string.Equals(this.CryptoKeyContainer, other.CryptoKeyContainer, StringComparison.Ordinal) &&
                   string.Equals(this.CryptoKeyFile, other.CryptoKeyFile, StringComparison.Ordinal) &&
                   this.CryptoPublicKey.SequenceEqual(other.CryptoPublicKey) &&
                   this.DelaySign == other.DelaySign &&
                   this.GeneralDiagnosticOption == other.GeneralDiagnosticOption &&
                   string.Equals(this.MainTypeName, other.MainTypeName, StringComparison.Ordinal) &&
                   this.MetadataImportOptions == other.MetadataImportOptions &&
                   this.ReferencesSupersedeLowerVersions == other.ReferencesSupersedeLowerVersions &&
                   string.Equals(this.ModuleName, other.ModuleName, StringComparison.Ordinal) &&
                   this.OptimizationLevel == other.OptimizationLevel &&
                   this.OutputKind == other.OutputKind &&
                   this.Platform == other.Platform &&
                   this.ReportSuppressedDiagnostics == other.ReportSuppressedDiagnostics &&
                   string.Equals(this.ScriptClassName, other.ScriptClassName, StringComparison.Ordinal) &&
                   this.SpecificDiagnosticOptions.SequenceEqual(other.SpecificDiagnosticOptions, (left, right) => (left.Key == right.Key) && (left.Value == right.Value)) &&
                   this.WarningLevel == other.WarningLevel &&
                   object.Equals(this.MetadataReferenceResolver, other.MetadataReferenceResolver) &&
                   object.Equals(this.XmlReferenceResolver, other.XmlReferenceResolver) &&
                   object.Equals(this.SourceReferenceResolver, other.SourceReferenceResolver) &&
                   object.Equals(this.SyntaxTreeOptionsProvider, other.SyntaxTreeOptionsProvider) &&
                   object.Equals(this.StrongNameProvider, other.StrongNameProvider) &&
                   object.Equals(this.AssemblyIdentityComparer, other.AssemblyIdentityComparer) &&
                   this.PublicSign == other.PublicSign &&
                   this.NullableContextOptions == other.NullableContextOptions;

            return equal;
        }

        public sealed override int GetHashCode()
        {
            if (_hashCode == 0)
            {
                var hashCode = ComputeHashCode();
                _hashCode = hashCode == 0 ? 1 : hashCode;
            }

            return _hashCode;
        }

        protected abstract int ComputeHashCode();

        protected int GetHashCodeHelper()
        {
            return Hash.Combine(this.CheckOverflow,
                   Hash.Combine(this.ConcurrentBuild,
                   Hash.Combine(this.Deterministic,
                   Hash.Combine(this.CurrentLocalTime.GetHashCode(),
                   Hash.Combine(this.DebugPlusMode,
                   Hash.Combine(this.CryptoKeyContainer != null ? StringComparer.Ordinal.GetHashCode(this.CryptoKeyContainer) : 0,
                   Hash.Combine(this.CryptoKeyFile != null ? StringComparer.Ordinal.GetHashCode(this.CryptoKeyFile) : 0,
                   Hash.Combine(Hash.CombineValues(this.CryptoPublicKey, 16),
                   Hash.Combine((int)this.GeneralDiagnosticOption,
                   Hash.Combine(this.MainTypeName != null ? StringComparer.Ordinal.GetHashCode(this.MainTypeName) : 0,
                   Hash.Combine((int)this.MetadataImportOptions,
                   Hash.Combine(this.ReferencesSupersedeLowerVersions,
                   Hash.Combine(this.ModuleName != null ? StringComparer.Ordinal.GetHashCode(this.ModuleName) : 0,
                   Hash.Combine((int)this.OptimizationLevel,
                   Hash.Combine((int)this.OutputKind,
                   Hash.Combine((int)this.Platform,
                   Hash.Combine(this.ReportSuppressedDiagnostics,
                   Hash.Combine(this.ScriptClassName != null ? StringComparer.Ordinal.GetHashCode(this.ScriptClassName) : 0,
                   Hash.Combine(Hash.CombineValues(this.SpecificDiagnosticOptions),
                   Hash.Combine(this.WarningLevel,
                   Hash.Combine(this.MetadataReferenceResolver,
                   Hash.Combine(this.XmlReferenceResolver,
                   Hash.Combine(this.SourceReferenceResolver,
                   Hash.Combine(this.SyntaxTreeOptionsProvider,
                   Hash.Combine(this.StrongNameProvider,
                   Hash.Combine(this.AssemblyIdentityComparer,
                   Hash.Combine(this.PublicSign,
                   Hash.Combine((int)this.NullableContextOptions, 0))))))))))))))))))))))))))));
        }

        public static bool operator ==(CompilationOptions? left, CompilationOptions? right)
        {
            return object.Equals(left, right);
        }

        public static bool operator !=(CompilationOptions? left, CompilationOptions? right)
        {
            return !object.Equals(left, right);
        }
    }
}
