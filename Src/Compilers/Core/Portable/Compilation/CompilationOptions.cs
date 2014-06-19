// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
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
        /// is derived from the name of the compilation (<see cref="P:Compilation.Name"/>)
        /// by appending a default extension for <see cref="P:OutputKind"/>.
        /// </remarks>
        public string ModuleName { get; protected set; }

        /// <summary>
        /// Subsystem version
        /// </summary>
        public SubsystemVersion SubsystemVersion { get; protected set; }

        /// <summary>
        /// The full name of a global implicit class (script class). This class implicitly encapsulates top-level statements, 
        /// type declarations, and member declarations. Could be a namespace qualified name.
        /// </summary>
        public string ScriptClassName { get; protected set; }

        /// <summary>
        /// The full name of a type that declares static Main method. Must be a valid non-generic namespace-qualified name.
        /// Null if any static Main method is a candidate for an entry point.
        /// </summary>
        public string MainTypeName { get; protected set; }

        /// <summary>
        /// The name of the file containing the key with which to sign the output.
        /// </summary>
        /// <remarks>
        /// To sign the output supply either one of <see cref="P:CryptoKeyContainer"/> or <see cref="P:CryptoKeyFile"/>.
        /// but not both.
        /// </remarks>
        public string CryptoKeyFile { get; protected set; }

        /// <summary>
        /// The CSP container containing the key with which to sign the output.
        /// </summary>
        public string CryptoKeyContainer { get; protected set; }

        /// <summary>
        /// Turn off strong name signing when you have supplied a key either through
        /// attributes or  <see cref="P:CryptoKeyContainer"/> or <see cref="P:CryptoKeyFile"/>.
        /// </summary>
        public bool? DelaySign { get; protected set; }

        /// <summary>
        /// Whether bounds checking on integer arithmetic is enforced by default or not.
        /// </summary>
        public bool CheckOverflow { get; protected set; }

        /// <summary>
        /// Specifies the size of sections in the output file. 
        /// </summary>
        /// <remarks>
        /// Valid values are 0, 512, 1024, 2048, 4096 and 8192.
        /// If the value is 0 the file alignment is determined based upon the value of <see cref="Platform"/>.
        /// </remarks>
        public int FileAlignment { get; protected set; }

        /// <summary>
        /// Specifies the preferred base address at which to load the output DLL.
        /// </summary>
        public ulong BaseAddress { get; protected set; }

        /// <summary>
        /// Specifies which version of the common language runtime (CLR) can run the assembly.
        /// </summary>
        public Platform Platform { get; protected set; }

        public bool HighEntropyVirtualAddressSpace { get; protected set; }

        /// <summary>
        /// Specifies the kind of debug information to be emitted.
        /// </summary>
        /// <remarks>
        /// This value is set based on the "/debug", "/debug+", "/debug-" and "/debug:{full|pdbonly}" command line switches.
        /// </remarks>
        public DebugInformationKind DebugInformationKind { get; protected set; }

        /// <summary>
        /// Specifies whether or not optimizations should be performed on the output IL.
        /// This is independent of whether or not PDB information is generated.
        /// </summary>
        public bool Optimize { get; protected set; }

        /// <summary>
        /// Global warning report option
        /// </summary>
        public ReportDiagnostic GeneralDiagnosticOption { get; protected set; }

        /// <summary>
        /// Global warning level (from 0 to 4).
        /// </summary>
        public int WarningLevel { get; protected set; }

        /// <summary>
        /// Specifies whether building compilation may use multiple threads.
        /// </summary>
        public bool ConcurrentBuild { get; protected set; }

        /// <summary>
        /// Import internal/private members from all references regardless of "internals-visible-to" relationship.
        /// </summary>
        internal MetadataImportOptions MetadataImportOptions { get; private set; }

        // TODO: change visibility of the MetadataImportOptions setter to internal & protected
        internal MetadataImportOptions MetadataImportOptions_internal_protected_set { set { MetadataImportOptions = value; } }

        /// <summary>
        /// Warning report option for each warning.
        /// </summary>
        public ImmutableDictionary<string, ReportDiagnostic> SpecificDiagnosticOptions { get; protected set; }

        /// <summary>
        /// Translates a resolved assembly reference path to an actual <see cref="PortableExecutableReference"/>.
        /// Null if the compilation can't contain references to metadata other than those explicitly passed to its factory (such as #r directives in sources). 
        /// </summary>
        public MetadataReferenceProvider MetadataReferenceProvider { get; protected set; }

        /// <summary>
        /// Resolves paths to metadata references specified in source via #r directives.
        /// Null if the compilation can't contain references to metadata other than those explicitly passed to its factory (such as #r directives in sources). 
        /// </summary>
        public MetadataReferenceResolver MetadataReferenceResolver { get; protected set; }

        /// <summary>
        /// Gets the resolver for resolving XML document references for the compilation.
        /// Null if the compilation is not allowed to contain XML file references, such as XML doc comment include tags and permission sets stored in an XML file.
        /// </summary>
        public XmlReferenceResolver XmlReferenceResolver { get; protected set; }

        /// <summary>
        /// Gets the resolver for resolving source document references for the compilation.
        /// Null if the compilation is not allowed to contain source file references, such as #line pragmas and #load directives.
        /// </summary>
        public SourceReferenceResolver SourceReferenceResolver { get; protected set; }

        /// <summary>
        /// Provides strong name and signature the source assembly.
        /// Null if assembly signing is not supported.
        /// </summary>
        public StrongNameProvider StrongNameProvider { get; protected set; }

        /// <summary>
        /// Used to compare assembly identities. May implement unification and portability policies specific to the target platform.
        /// <see cref="AssemblyIdentityComparer.Default"/> if not specified.
        /// </summary>
        public AssemblyIdentityComparer AssemblyIdentityComparer { get; protected set; }

        /// <summary>
        /// A set of strings designating experimental compiler features that are to be enabled.
        /// </summary>
        protected internal ImmutableArray<string> Features { get; protected set; }

        private Lazy<ImmutableArray<Diagnostic>> lazyErrors = null;

        // Expects correct arguments.
        internal CompilationOptions(
            OutputKind outputKind,
            string moduleName,
            string mainTypeName,
            string scriptClassName,
            string cryptoKeyContainer,
            string cryptoKeyFile,
            bool? delaySign,
            bool optimize,
            bool checkOverflow,
            int fileAlignment,
            ulong baseAddress,
            Platform platform,
            ReportDiagnostic generalDiagnosticOption,
            int warningLevel,
            IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions,
            bool highEntropyVirtualAddressSpace,
            DebugInformationKind debugInformationKind,
            SubsystemVersion subsystemVersion,
            bool concurrentBuild,
            XmlReferenceResolver xmlReferenceResolver,
            SourceReferenceResolver sourceReferenceResolver,
            MetadataReferenceResolver metadataReferenceResolver,
            MetadataReferenceProvider metadataReferenceProvider,
            AssemblyIdentityComparer assemblyIdentityComparer,
            StrongNameProvider strongNameProvider,
            MetadataImportOptions metadataImportOptions,
            ImmutableArray<string> features)
        {
            this.OutputKind = outputKind;
            this.ModuleName = moduleName;
            this.MainTypeName = mainTypeName;
            this.ScriptClassName = scriptClassName;
            this.CryptoKeyContainer = cryptoKeyContainer;
            this.CryptoKeyFile = cryptoKeyFile;
            this.DelaySign = delaySign;
            this.CheckOverflow = checkOverflow;
            this.FileAlignment = fileAlignment;
            this.BaseAddress = baseAddress;
            this.Platform = platform;
            this.GeneralDiagnosticOption = generalDiagnosticOption;
            this.WarningLevel = warningLevel;
            this.SpecificDiagnosticOptions = specificDiagnosticOptions.ToImmutableDictionaryOrEmpty();
            this.HighEntropyVirtualAddressSpace = highEntropyVirtualAddressSpace;
            this.DebugInformationKind = debugInformationKind;
            this.Optimize = optimize;
            this.ConcurrentBuild = concurrentBuild;
            this.SubsystemVersion = subsystemVersion;
            this.XmlReferenceResolver = xmlReferenceResolver;
            this.SourceReferenceResolver = sourceReferenceResolver;
            this.MetadataReferenceResolver = metadataReferenceResolver;
            this.MetadataReferenceProvider = metadataReferenceProvider;
            this.StrongNameProvider = strongNameProvider;
            this.AssemblyIdentityComparer = assemblyIdentityComparer ?? AssemblyIdentityComparer.Default;
            this.MetadataImportOptions = metadataImportOptions;
            this.Features = features;
            
            this.lazyErrors = new Lazy<ImmutableArray<Diagnostic>>(() =>
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
                && this.OutputKind.IsNetModule() == other.OutputKind.IsNetModule()
                && object.Equals(this.XmlReferenceResolver, other.XmlReferenceResolver)
                && object.Equals(this.MetadataReferenceResolver, other.MetadataReferenceResolver)
                && object.Equals(this.MetadataReferenceProvider, other.MetadataReferenceProvider)
                && object.Equals(this.AssemblyIdentityComparer, other.AssemblyIdentityComparer);
        }

        internal bool EnableEditAndContinue
        {
            get
            {
                return DebugInformationKind == DebugInformationKind.Full && !Optimize;
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
        public CompilationOptions WithSpecificDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic> value)
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
        /// Creates a new options instance with optimizations enabled or disabled.
        /// </summary>
        public CompilationOptions WithOptimizations(bool enabled)
        {
            return CommonWithOptimizations(enabled);
        }

        public CompilationOptions WithXmlReferenceResolver(XmlReferenceResolver resolver)
        {
            return CommonWithXmlReferenceResolver(resolver);
        }

        public CompilationOptions WithSourceReferenceResolver(SourceReferenceResolver resolver)
        {
            return CommonWithSourceReferenceResolver(resolver);
        }

        public CompilationOptions WithMetadataReferenceProvider(MetadataReferenceProvider provider)
        {
            return CommonWithMetadataReferenceProvider(provider);
        }

        public CompilationOptions WithMetadataReferenceResolver(MetadataReferenceResolver resolver)
        {
            return CommonWithMetadataReferenceResolver(resolver);
        }

        public CompilationOptions WithAssemblyIdentityComparer(AssemblyIdentityComparer comparer)
        {
            return CommonWithAssemblyIdentityComparer(comparer);
        }

        public CompilationOptions WithStrongNameProvider(StrongNameProvider provider)
        {
            return CommonWithStrongNameProvider(provider);
        }

        internal CompilationOptions WithFeatures(ImmutableArray<string> features)
        {
            return CommonWithFeatures(features);
        }

        protected abstract CompilationOptions CommonWithOutputKind(OutputKind kind);
        protected abstract CompilationOptions CommonWithPlatform(Platform platform);
        protected abstract CompilationOptions CommonWithOptimizations(bool enabled);
        protected abstract CompilationOptions CommonWithXmlReferenceResolver(XmlReferenceResolver resolver);
        protected abstract CompilationOptions CommonWithSourceReferenceResolver(SourceReferenceResolver resolver);
        protected abstract CompilationOptions CommonWithMetadataReferenceResolver(MetadataReferenceResolver resolver);
        protected abstract CompilationOptions CommonWithMetadataReferenceProvider(MetadataReferenceProvider provider);
        protected abstract CompilationOptions CommonWithAssemblyIdentityComparer(AssemblyIdentityComparer comparer);
        protected abstract CompilationOptions CommonWithStrongNameProvider(StrongNameProvider provider);
        protected abstract CompilationOptions CommonWithGeneralDiagnosticOption(ReportDiagnostic generalDiagnosticOption);
        protected abstract CompilationOptions CommonWithSpecificDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic> specificDiagnosticOptions);
        protected abstract CompilationOptions CommonWithSpecificDiagnosticOptions(IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions);
        protected abstract CompilationOptions CommonWithFeatures(ImmutableArray<string> features);

        /// <summary>
        /// Performs validation of options compatibilities and generates diagnostics if needed
        /// </summary>
        internal abstract void ValidateOptions(ArrayBuilder<Diagnostic> builder);

        /// <summary>
        /// Errors collection related to an incompatible set of compilation options
        /// </summary>
        public ImmutableArray<Diagnostic> Errors
        {
            get { return this.lazyErrors.Value; }
        }

        public abstract override bool Equals(object obj);

        protected bool EqualsHelper(CompilationOptions other)
        {
            if (object.ReferenceEquals(other, null))
            {
                return false;
            }

            // NOTE: StringComparison.Ordinal is used for type name comparisons, even for VB.  That's because
            // a change in the canonical case should still change the option.
            bool equal =
                   this.BaseAddress == other.BaseAddress &&
                   this.CheckOverflow == other.CheckOverflow &&
                   this.ConcurrentBuild == other.ConcurrentBuild &&
                   string.Equals(this.CryptoKeyContainer, other.CryptoKeyContainer, StringComparison.Ordinal) &&
                   string.Equals(this.CryptoKeyFile, other.CryptoKeyFile, StringComparison.Ordinal) &&
                   this.DebugInformationKind == other.DebugInformationKind &&
                   this.DelaySign == other.DelaySign &&
                   this.FileAlignment == other.FileAlignment &&
                   this.GeneralDiagnosticOption == other.GeneralDiagnosticOption &&
                   this.HighEntropyVirtualAddressSpace == other.HighEntropyVirtualAddressSpace &&
                   string.Equals(this.MainTypeName, other.MainTypeName, StringComparison.Ordinal) &&
                   this.MetadataImportOptions == other.MetadataImportOptions &&
                   string.Equals(this.ModuleName, other.ModuleName, StringComparison.Ordinal) &&
                   this.Optimize == other.Optimize &&
                   this.OutputKind == other.OutputKind &&
                   this.Platform == other.Platform &&
                   string.Equals(this.ScriptClassName, other.ScriptClassName, StringComparison.Ordinal) &&
                   this.SpecificDiagnosticOptions.SequenceEqual(other.SpecificDiagnosticOptions, (left, right) => (left.Key == right.Key) && (left.Value == right.Value)) &&
                   this.SubsystemVersion.Equals(other.SubsystemVersion) &&
                   this.WarningLevel == other.WarningLevel &&
                   object.Equals(this.MetadataReferenceProvider, other.MetadataReferenceProvider) &&
                   object.Equals(this.MetadataReferenceResolver, other.MetadataReferenceResolver) &&
                   object.Equals(this.XmlReferenceResolver, other.XmlReferenceResolver) &&
                   object.Equals(this.SourceReferenceResolver, other.SourceReferenceResolver) &&
                   object.Equals(this.StrongNameProvider, other.StrongNameProvider) &&
                   object.Equals(this.AssemblyIdentityComparer, other.AssemblyIdentityComparer) &&
                   this.Features.SequenceEqual(other.Features, StringComparer.Ordinal);

            return equal;
        }

        public abstract override int GetHashCode();

        protected int GetHashCodeHelper()
        {
            return Hash.Combine(this.BaseAddress.GetHashCode(),
                   Hash.Combine(this.CheckOverflow,
                   Hash.Combine(this.ConcurrentBuild,
                   Hash.Combine(this.CryptoKeyContainer != null ? StringComparer.Ordinal.GetHashCode(this.CryptoKeyContainer) : 0,
                   Hash.Combine(this.CryptoKeyFile != null ? StringComparer.Ordinal.GetHashCode(this.CryptoKeyFile) : 0,
                   Hash.Combine((int)this.DebugInformationKind,
                   Hash.Combine(this.DelaySign.HasValue ? this.DelaySign.Value : false,
                   Hash.Combine(this.FileAlignment,
                   Hash.Combine((int)this.GeneralDiagnosticOption,
                   Hash.Combine(this.HighEntropyVirtualAddressSpace,
                   Hash.Combine(this.MainTypeName != null ? StringComparer.Ordinal.GetHashCode(this.MainTypeName) : 0,
                   Hash.Combine((int)this.MetadataImportOptions,
                   Hash.Combine(this.ModuleName != null ? StringComparer.Ordinal.GetHashCode(this.ModuleName) : 0,
                   Hash.Combine(this.Optimize,
                   Hash.Combine((int)this.OutputKind,
                   Hash.Combine((int)this.Platform,
                   Hash.Combine(this.ScriptClassName != null ? StringComparer.Ordinal.GetHashCode(this.ScriptClassName) : 0,
                   Hash.Combine(Hash.CombineValues(this.SpecificDiagnosticOptions),
                   Hash.Combine(this.SubsystemVersion.GetHashCode(),
                   Hash.Combine(this.WarningLevel,
                   Hash.Combine(this.MetadataReferenceResolver,
                   Hash.Combine(this.MetadataReferenceProvider,
                   Hash.Combine(this.XmlReferenceResolver,
                   Hash.Combine(this.SourceReferenceResolver,
                   Hash.Combine(this.StrongNameProvider,
                   Hash.Combine(this.AssemblyIdentityComparer,
                   Hash.Combine(Hash.CombineValues(this.Features, StringComparer.Ordinal), 0)))))))))))))))))))))))))));
        }

        public static bool operator ==(CompilationOptions left, CompilationOptions right)
        {
            return object.Equals(left, right);
        }

        public static bool operator !=(CompilationOptions left, CompilationOptions right)
        {
            return !object.Equals(left, right);
        }
    }
}
