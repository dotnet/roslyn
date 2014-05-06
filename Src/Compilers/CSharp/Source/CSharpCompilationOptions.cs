// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents various options that affect compilation, such as 
    /// whether to emit an executable or a library, whether to optimize
    /// generated code, and so on.
    /// </summary>
    [Serializable]
    public sealed class CSharpCompilationOptions : CompilationOptions, IEquatable<CSharpCompilationOptions>, ISerializable
    {
        private const string AllowUnsafeString = "AllowUnsafe";
        private const string UsingsString = "Usings";
        private const string RuntimeMetadataVersionString = "RuntimeMetadataVersion";

        /// <summary>
        /// Allow unsafe regions (i.e. unsafe modifiers on members and unsafe blocks).
        /// </summary>
        public bool AllowUnsafe { get; private set; }

        /// <summary>
        /// Global namespace usings.
        /// </summary>
        public ImmutableArray<string> Usings { get; private set; }

        internal string RuntimeMetadataVersion { get; private set; }

        // Defaults correspond to the compiler's defaults or indicate that the user did not specify when that is significant.
        // That's significant when one option depends on another's setting. SubsystemVersion depends on Platform and Target.
        public CSharpCompilationOptions(
            OutputKind outputKind,
            string moduleName = null,
            string mainTypeName = null,
            string scriptClassName = WellKnownMemberNames.DefaultScriptClassName,
            IEnumerable<string> usings = null,
            bool optimize = false,
            bool checkOverflow = false,
            bool allowUnsafe = false,
            string cryptoKeyContainer = null,
            string cryptoKeyFile = null,
            bool? delaySign = null,
            int fileAlignment = 0,
            ulong baseAddress = 0,
            Platform platform = Platform.AnyCpu,
            ReportDiagnostic generalDiagnosticOption = ReportDiagnostic.Default,
            int warningLevel = 4,
            IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions = null,
            bool highEntropyVirtualAddressSpace = false,
            DebugInformationKind debugInformationKind = DebugInformationKind.None,
            SubsystemVersion subsystemVersion = default(SubsystemVersion),
            string runtimeMetadataVersion = null,
            bool concurrentBuild = true,
            XmlReferenceResolver xmlReferenceResolver = null,
            SourceReferenceResolver sourceReferenceResolver = null,
            MetadataReferenceResolver metadataReferenceResolver = null,
            MetadataReferenceProvider metadataReferenceProvider = null,
            AssemblyIdentityComparer assemblyIdentityComparer = null,
            StrongNameProvider strongNameProvider = null)
            : this(outputKind, moduleName, mainTypeName, scriptClassName, usings, optimize, checkOverflow, allowUnsafe,
            cryptoKeyContainer, cryptoKeyFile, delaySign, fileAlignment, baseAddress, platform, generalDiagnosticOption, warningLevel,
                   specificDiagnosticOptions, highEntropyVirtualAddressSpace, debugInformationKind, subsystemVersion, runtimeMetadataVersion, concurrentBuild,
                   xmlReferenceResolver, sourceReferenceResolver, metadataReferenceResolver, metadataReferenceProvider, assemblyIdentityComparer, strongNameProvider, MetadataImportOptions.Public, features: ImmutableArray<string>.Empty)
        {
        }

        // Expects correct arguments.
        private CSharpCompilationOptions(
            OutputKind outputKind,
            string moduleName,
            string mainTypeName,
            string scriptClassName,
            IEnumerable<string> usings,
            bool optimize,
            bool checkOverflow,
            bool allowUnsafe,
            string cryptoKeyContainer,
            string cryptoKeyFile,
            bool? delaySign,
            int fileAlignment,
            ulong baseAddress,
            Platform platform,
            ReportDiagnostic generalDiagnosticOption,
            int warningLevel,
            IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions,
            bool highEntropyVirtualAddressSpace,
            DebugInformationKind debugInformationKind,
            SubsystemVersion subsystemVersion,
            string runtimeMetadataVersion,
            bool concurrentBuild,
            XmlReferenceResolver xmlReferenceResolver,
            SourceReferenceResolver sourceReferenceResolver,
            MetadataReferenceResolver metadataReferenceResolver,
            MetadataReferenceProvider metadataReferenceProvider,
            AssemblyIdentityComparer assemblyIdentityComparer,
            StrongNameProvider strongNameProvider,
            MetadataImportOptions metadataImportOptions,
            ImmutableArray<string> features)
            : base(outputKind, moduleName, mainTypeName, scriptClassName, cryptoKeyContainer, cryptoKeyFile, delaySign, optimize, checkOverflow, fileAlignment, baseAddress,
                   platform, generalDiagnosticOption, warningLevel, specificDiagnosticOptions, highEntropyVirtualAddressSpace, debugInformationKind,
                   subsystemVersion, concurrentBuild, xmlReferenceResolver, sourceReferenceResolver, metadataReferenceResolver, metadataReferenceProvider, assemblyIdentityComparer, strongNameProvider, metadataImportOptions, features)
        {
            Initialize(usings, allowUnsafe, runtimeMetadataVersion);
        }

        private CSharpCompilationOptions(CSharpCompilationOptions other) : this(
            outputKind: other.OutputKind,
            moduleName: other.ModuleName,
            mainTypeName: other.MainTypeName,
            scriptClassName: other.ScriptClassName,
            usings: other.Usings,
            optimize: other.Optimize,
            checkOverflow: other.CheckOverflow,
            allowUnsafe: other.AllowUnsafe,
            cryptoKeyContainer: other.CryptoKeyContainer,
            cryptoKeyFile: other.CryptoKeyFile,
            delaySign: other.DelaySign,
            fileAlignment: other.FileAlignment,
            baseAddress: other.BaseAddress,
            platform: other.Platform,
            generalDiagnosticOption: other.GeneralDiagnosticOption,
            warningLevel: other.WarningLevel,
            specificDiagnosticOptions: other.SpecificDiagnosticOptions,
            highEntropyVirtualAddressSpace: other.HighEntropyVirtualAddressSpace,
            debugInformationKind: other.DebugInformationKind,
            subsystemVersion: other.SubsystemVersion,
            runtimeMetadataVersion: other.RuntimeMetadataVersion,
            concurrentBuild: other.ConcurrentBuild,
            xmlReferenceResolver: other.XmlReferenceResolver,
            sourceReferenceResolver: other.SourceReferenceResolver,
            metadataReferenceResolver: other.MetadataReferenceResolver,
            metadataReferenceProvider: other.MetadataReferenceProvider,
            assemblyIdentityComparer: other.AssemblyIdentityComparer,
            strongNameProvider: other.StrongNameProvider,
            metadataImportOptions: other.MetadataImportOptions,
            features: other.Features)
        {
        }

        private void Initialize(IEnumerable<string> usings, bool allowUnsafe, string runtimeMetadataVersion)
        {
            this.Usings = usings.AsImmutableOrEmpty();
            this.AllowUnsafe = allowUnsafe;
            this.RuntimeMetadataVersion = runtimeMetadataVersion;
        }

        private CSharpCompilationOptions(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Initialize(
                (string[])info.GetValue(UsingsString, typeof(string[])),
                info.GetBoolean(AllowUnsafeString),
                info.GetString(RuntimeMetadataVersionString));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(UsingsString, this.Usings.ToArray());
            info.AddValue(AllowUnsafeString, this.AllowUnsafe);
            info.AddValue(RuntimeMetadataVersionString, this.RuntimeMetadataVersion);
        }

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

        public CSharpCompilationOptions WithSubsystemVersion(SubsystemVersion subsystemVersion)
        {
            if (subsystemVersion.Equals(this.SubsystemVersion))
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { SubsystemVersion = subsystemVersion };
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

        public CSharpCompilationOptions WithUsings(IEnumerable<string> usings)
        {
            return new CSharpCompilationOptions(this) { Usings = usings.AsImmutableOrEmpty() };
        }

        public CSharpCompilationOptions WithUsings(params string[] usings)
        {
            return WithUsings((IEnumerable<string>)usings);
        }

        public CSharpCompilationOptions WithDebugInformationKind(DebugInformationKind kind)
        {
            if (kind == this.DebugInformationKind)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { DebugInformationKind = kind };
        }

        public new CSharpCompilationOptions WithOptimizations(bool enabled)
        {
            if (enabled == this.Optimize)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { Optimize = enabled };
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

        /// <summary>
        /// Sets the byte alignment for portable executable file sections.
        /// </summary>
        /// <param name="value">Can be one of the following values: 0, 512, 1024, 2048, 4096, 8192</param>
        public CSharpCompilationOptions WithFileAlignment(int value)
        {
            if (this.FileAlignment == value)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { FileAlignment = value };
        }

        public CSharpCompilationOptions WithBaseAddress(ulong value)
        {
            if (this.BaseAddress == value)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { BaseAddress = value };
        }

        public new CSharpCompilationOptions WithPlatform(Platform platform)
        {
            if (this.Platform == platform)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { Platform = platform };
        }

        public CSharpCompilationOptions WithHighEntropyVirtualAddressSpace(bool value)
        {
            if (this.HighEntropyVirtualAddressSpace == value)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { HighEntropyVirtualAddressSpace = value };
        }

        protected override CompilationOptions CommonWithGeneralDiagnosticOption(ReportDiagnostic value)
        {
            return this.WithGeneralDiagnosticOption(value);
        }

        protected override CompilationOptions CommonWithSpecificDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic> specificDiagnosticOptions)
        {
            return this.WithSpecificDiagnosticOptions(specificDiagnosticOptions);
        }

        protected override CompilationOptions CommonWithSpecificDiagnosticOptions(IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions)
        {
            return this.WithSpecificDiagnosticOptions(specificDiagnosticOptions);
        }

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

        public new CSharpCompilationOptions WithSpecificDiagnosticOptions(IEnumerable<KeyValuePair<string, ReportDiagnostic>> values)
        {
            return new CSharpCompilationOptions(this) { SpecificDiagnosticOptions = values.ToImmutableDictionaryOrEmpty() };
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

        internal CSharpCompilationOptions WithMetadataImportOptions(MetadataImportOptions value)
        {
            if (value == this.MetadataImportOptions)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { MetadataImportOptions_internal_protected_set = value };
        }

        internal new CSharpCompilationOptions WithFeatures(ImmutableArray<string> features)
        {
            if (features == this.Features)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { Features = features };
        }

        internal CSharpCompilationOptions WithRuntimeMetadataVersion(string version)
        {
            if (RuntimeMetadataVersion == version)
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { RuntimeMetadataVersion = version };
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

        public new CSharpCompilationOptions WithMetadataReferenceProvider(MetadataReferenceProvider provider)
        {
            if (ReferenceEquals(provider, this.MetadataReferenceProvider))
            {
                return this;
            }

            return new CSharpCompilationOptions(this) { MetadataReferenceProvider = provider };
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

        protected override CompilationOptions CommonWithOutputKind(OutputKind kind)
        {
            return WithOutputKind(kind);
        }

        protected override CompilationOptions CommonWithPlatform(Platform platform)
        {
            return WithPlatform(platform);
        }

        protected override CompilationOptions CommonWithOptimizations(bool enabled)
        {
            return WithOptimizations(enabled);
        }

        protected override CompilationOptions CommonWithAssemblyIdentityComparer(AssemblyIdentityComparer comparer)
        {
            return WithAssemblyIdentityComparer(comparer);
        }

        protected override CompilationOptions CommonWithXmlReferenceResolver(XmlReferenceResolver resolver)
        {
            return WithXmlReferenceResolver(resolver);
        }

        protected override CompilationOptions CommonWithSourceReferenceResolver(SourceReferenceResolver resolver)
        {
            return WithSourceReferenceResolver(resolver);
        }

        protected override CompilationOptions CommonWithMetadataReferenceResolver(MetadataReferenceResolver resolver)
        {
            return WithMetadataReferenceResolver(resolver);
        }

        protected override CompilationOptions CommonWithMetadataReferenceProvider(MetadataReferenceProvider provider)
        {
            return WithMetadataReferenceProvider(provider);
        }

        protected override CompilationOptions CommonWithStrongNameProvider(StrongNameProvider provider)
        {
            return WithStrongNameProvider(provider);
        }

        protected override CompilationOptions CommonWithFeatures(ImmutableArray<string> features)
        {
            return WithFeatures(features);
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
                    builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadCompilationOptionValue, "MainTypeName", MainTypeName));
                }
            }

            if (FileAlignment != 0 && !IsValidFileAlignment(FileAlignment))
            {
                builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadFileAlignment, FileAlignment));
            }

            if (!Platform.IsValid())
            {
                builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadPlatformType, Platform));
            }

            if (ModuleName != null)
            {
                Exception e = MetadataHelpers.CheckAssemblyOrModuleName(ModuleName, "ModuleName");
                if (e != null)
                {
                    builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadCompilationOption, e.Message));
                }
            }

            if (!OutputKind.IsValid())
            {
                builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadCompilationOptionValue, "OutputKind", OutputKind));
            }

            if (!DebugInformationKind.IsValid())
            {
                builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadCompilationOptionValue, "DebugInformationKind", DebugInformationKind));
            }

            if (!SubsystemVersion.Equals(SubsystemVersion.None) && !SubsystemVersion.IsValid)
            {
                builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadSubsystemVersion, SubsystemVersion.ToString()));
            }

            if (ScriptClassName == null || !ScriptClassName.IsValidClrTypeName())
            {
                builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadCompilationOptionValue, "ScriptClassName", ScriptClassName ?? "null"));
            }

            if (WarningLevel < 0 || WarningLevel > 4)
            {
                builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadCompilationOptionValue, "WarningLevel", WarningLevel));
            }

            if (Usings != null && Usings.Any(u => !u.IsValidClrNamespaceName()))
            {
                builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadCompilationOptionValue, "Usings", Usings.Where(u => !u.IsValidClrNamespaceName()).First() ?? "null"));
            }

            if (Platform == Platform.AnyCpu32BitPreferred && OutputKind.IsValid() && !(OutputKind == OutputKind.ConsoleApplication || OutputKind == OutputKind.WindowsApplication || OutputKind == OutputKind.WindowsRuntimeApplication))
            {
                builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadPrefer32OnLib));
            }

            // TODO: add check for 
            //          (kind == 'arm' || kind == 'appcontainer' || kind == 'winmdobj') &&
            //          (version >= "6.2")
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
                   this.RuntimeMetadataVersion == other.RuntimeMetadataVersion &&
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
                   Hash.Combine(this.RuntimeMetadataVersion,
                   Hash.Combine(Hash.CombineValues(this.Usings, StringComparer.Ordinal), 0))));
        }
    }
}
