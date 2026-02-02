// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;
using CommonAssemblyWellKnownAttributeData = Microsoft.CodeAnalysis.CommonAssemblyWellKnownAttributeData<Microsoft.CodeAnalysis.CSharp.Symbols.NamedTypeSymbol>;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents an assembly built by compiler.
    /// </summary>
    internal sealed partial class SourceAssemblySymbol : MetadataOrSourceAssemblySymbol, ISourceAssemblySymbolInternal, IAttributeTargetSymbol
    {
        /// <summary>
        /// A Compilation the assembly is created for.
        /// </summary>
        private readonly CSharpCompilation _compilation;

        private SymbolCompletionState _state;

        /// <summary>
        /// Assembly's identity.
        /// </summary>
        internal AssemblyIdentity lazyAssemblyIdentity;
        private readonly string _assemblySimpleName;

        // Computing the identity requires computing the public key. Computing the public key 
        // can require binding attributes that contain version or strong name information. 
        // Attribute binding will check type visibility which will possibly 
        // check IVT relationships. To correctly determine the IVT relationship requires the public key. 
        // To avoid infinite recursion, this type notes, per thread, the assembly for which the thread 
        // is actively computing the public key (assemblyForWhichCurrentThreadIsComputingKeys). Should a request to determine IVT
        // relationship occur on the thread that is computing the public key, access is optimistically
        // granted provided the simple assembly names match. When such access is granted
        // the assembly to which we have been granted access is noted (optimisticallyGrantedInternalsAccess).
        // After the public key has been computed, the set of optimistic grants is reexamined 
        // to ensure that full identities match. This may produce diagnostics.
        private StrongNameKeys _lazyStrongNameKeys;

        /// <summary>
        /// A list of modules the assembly consists of. 
        /// The first (index=0) module is a SourceModuleSymbol, which is a primary module, the rest are net-modules.
        /// </summary>
        private readonly ImmutableArray<ModuleSymbol> _modules;

        /// <summary>
        /// Bag of assembly's custom attributes and decoded well-known attribute data from source.
        /// </summary>
        private CustomAttributesBag<CSharpAttributeData> _lazySourceAttributesBag;

        /// <summary>
        /// Bag of assembly's custom attributes and decoded well-known attribute data from added netmodules.
        /// </summary>
        private CustomAttributesBag<CSharpAttributeData> _lazyNetModuleAttributesBag;

#nullable enable 

        private IDictionary<string, NamedTypeSymbol> _lazyForwardedTypesFromSource;

#nullable disable

        /// <summary>
        /// Indices of attributes that will not be emitted for one of two reasons:
        /// - They are duplicates of another attribute (i.e. attributes that bind to the same constructor and have identical arguments)
        /// - They are InternalsVisibleToAttributes with invalid assembly identities
        /// </summary>
        /// <remarks>
        /// These indices correspond to the merged assembly attributes from source and added net modules, i.e. attributes returned by <see cref="GetAttributes"/> method.
        /// </remarks>
        private ConcurrentSet<int> _lazyOmittedAttributeIndices;

        private ThreeState _lazyContainsExtensionMethods;

        /// <summary>
        /// Map for storing effectively private or effectively internal fields declared in this assembly but never initialized nor assigned.
        /// Each {symbol, bool} key-value pair in this map indicates the following:
        ///  (a) Key: Unassigned field symbol.
        ///  (b) Value: True if the unassigned field is effectively internal, false otherwise.
        /// </summary>
        private readonly ConcurrentDictionary<FieldSymbol, bool> _unassignedFieldsMap = new ConcurrentDictionary<FieldSymbol, bool>();

        /// <summary>
        /// private fields declared in this assembly but never read
        /// </summary>
        private readonly ConcurrentSet<FieldSymbol> _unreadFields = new ConcurrentSet<FieldSymbol>();

        /// <summary>
        /// We imitate the native compiler's policy of not warning about unused fields
        /// when the enclosing type is used by an extern method for a ref argument.
        /// Here we keep track of those types.
        /// </summary>
        internal ConcurrentSet<TypeSymbol> TypesReferencedInExternalMethods = new ConcurrentSet<TypeSymbol>();

        /// <summary>
        /// The warnings for unused fields.
        /// </summary>
        private ImmutableArray<Diagnostic> _unusedFieldWarnings;

        internal SourceAssemblySymbol(
            CSharpCompilation compilation,
            string assemblySimpleName,
            string moduleName,
            ImmutableArray<PEModule> netModules)
        {
            Debug.Assert(compilation != null);
            Debug.Assert(assemblySimpleName != null);
            Debug.Assert(!String.IsNullOrWhiteSpace(moduleName));
            Debug.Assert(!netModules.IsDefault);

            _compilation = compilation;
            _assemblySimpleName = assemblySimpleName;

            ArrayBuilder<ModuleSymbol> moduleBuilder = new ArrayBuilder<ModuleSymbol>(1 + netModules.Length);

            moduleBuilder.Add(new SourceModuleSymbol(this, compilation.Declarations, moduleName));

            var importOptions = (compilation.Options.MetadataImportOptions == MetadataImportOptions.All) ?
                MetadataImportOptions.All : MetadataImportOptions.Internal;

            foreach (PEModule netModule in netModules)
            {
                moduleBuilder.Add(new PEModuleSymbol(this, netModule, importOptions, moduleBuilder.Count));
                // SetReferences will be called later by the ReferenceManager (in CreateSourceAssemblyFullBind for 
                // a fresh manager, in CreateSourceAssemblyReuseData for a reused one).
            }

            _modules = moduleBuilder.ToImmutableAndFree();

            if (!compilation.Options.CryptoPublicKey.IsEmpty)
            {
                // Private key is not necessary for assembly identity, only when emitting.  For this reason, the private key can remain null.
                _lazyStrongNameKeys = StrongNameKeys.Create(compilation.Options.CryptoPublicKey, privateKey: null, hasCounterSignature: false, MessageProvider.Instance);
            }
        }

        public override string Name
        {
            get
            {
                return _assemblySimpleName;
            }
        }

        /// <remarks>
        /// This override is essential - it's a base case of the recursive definition.
        /// </remarks>
        internal sealed override CSharpCompilation DeclaringCompilation
        {
            get
            {
                return _compilation;
            }
        }

        public override bool IsInteractive
        {
            get
            {
                return _compilation.IsSubmission;
            }
        }

        internal bool MightContainNoPiaLocalTypes()
        {
            for (int i = 1; i < _modules.Length; i++)
            {
                var peModuleSymbol = (Metadata.PE.PEModuleSymbol)_modules[i];
                if (peModuleSymbol.Module.ContainsNoPiaLocalTypes())
                {
                    return true;
                }
            }

            return SourceModule.MightContainNoPiaLocalTypes();
        }

        public override AssemblyIdentity Identity
        {
            get
            {
                if (lazyAssemblyIdentity == null)
                    Interlocked.CompareExchange(ref lazyAssemblyIdentity, ComputeIdentity(), null);

                return lazyAssemblyIdentity;
            }
        }

        internal override bool HasImportedFromTypeLibAttribute
            => GetSourceDecodedWellKnownAttributeData()?.HasImportedFromTypeLibAttribute == true;

        internal override bool HasPrimaryInteropAssemblyAttribute
            => GetSourceDecodedWellKnownAttributeData()?.HasPrimaryInteropAssemblyAttribute == true;

        internal override Symbol GetSpecialTypeMember(SpecialMember member)
        {
            return _compilation.IsMemberMissing(member) ? null : base.GetSpecialTypeMember(member);
        }

#nullable enable
        private string? GetWellKnownAttributeDataStringField(Func<CommonAssemblyWellKnownAttributeData, string> fieldGetter, string? missingValue = null, QuickAttributes? attributeMatchesOpt = null)
        {
            string? fieldValue = missingValue;

            var data = attributeMatchesOpt is null
                ? GetSourceDecodedWellKnownAttributeData()
                : GetSourceDecodedWellKnownAttributeData(attributeMatchesOpt.Value);

            if (data != null)
            {
                fieldValue = fieldGetter(data);
            }

            if (fieldValue == (object?)missingValue)
            {
                data = (attributeMatchesOpt is null || _lazyNetModuleAttributesBag is not null)
                    ? GetNetModuleDecodedWellKnownAttributeData()
                    : GetLimitedNetModuleDecodedWellKnownAttributeData(attributeMatchesOpt.Value);

                if (data != null)
                {
                    fieldValue = fieldGetter(data);
                }
            }

            return fieldValue;
        }
#nullable disable

        internal bool RuntimeCompatibilityWrapNonExceptionThrows
        {
            get
            {
                var data = GetSourceDecodedWellKnownAttributeData() ?? GetNetModuleDecodedWellKnownAttributeData();

                // By default WrapNonExceptionThrows is considered to be true.
                return (data != null) ? data.RuntimeCompatibilityWrapNonExceptionThrows : CommonAssemblyWellKnownAttributeData.WrapNonExceptionThrowsDefault;
            }
        }

        internal string FileVersion
        {
            get
            {
                return GetWellKnownAttributeDataStringField(data => data.AssemblyFileVersionAttributeSetting);
            }
        }

        internal string Title
        {
            get
            {
                return GetWellKnownAttributeDataStringField(data => data.AssemblyTitleAttributeSetting);
            }
        }

        internal string Description
        {
            get
            {
                return GetWellKnownAttributeDataStringField(data => data.AssemblyDescriptionAttributeSetting);
            }
        }

        internal string Company
        {
            get
            {
                return GetWellKnownAttributeDataStringField(data => data.AssemblyCompanyAttributeSetting);
            }
        }

        internal string Product
        {
            get
            {
                return GetWellKnownAttributeDataStringField(data => data.AssemblyProductAttributeSetting);
            }
        }

        internal string InformationalVersion
        {
            get
            {
                return GetWellKnownAttributeDataStringField(data => data.AssemblyInformationalVersionAttributeSetting);
            }
        }

        internal string Copyright
        {
            get
            {
                return GetWellKnownAttributeDataStringField(data => data.AssemblyCopyrightAttributeSetting);
            }
        }

        internal string Trademark
        {
            get
            {
                return GetWellKnownAttributeDataStringField(data => data.AssemblyTrademarkAttributeSetting);
            }
        }

        private ThreeState AssemblyDelaySignAttributeSetting
        {
            get
            {
                var defaultValue = ThreeState.Unknown;
                var fieldValue = defaultValue;

                var data = GetSourceDecodedWellKnownAttributeData();
                if (data != null)
                {
                    fieldValue = data.AssemblyDelaySignAttributeSetting;
                }

                if (fieldValue == defaultValue)
                {
                    data = GetNetModuleDecodedWellKnownAttributeData();
                    if (data != null)
                    {
                        fieldValue = data.AssemblyDelaySignAttributeSetting;
                    }
                }

                return fieldValue;
            }
        }

        private string AssemblyKeyContainerAttributeSetting
        {
            get
            {
                // We mitigate circularity problems by only actively loading attributes that pass a syntactic check
                return GetWellKnownAttributeDataStringField(data => data.AssemblyKeyContainerAttributeSetting,
                    WellKnownAttributeData.StringMissingValue, QuickAttributes.AssemblyKeyName);
            }
        }

        private string AssemblyKeyFileAttributeSetting
        {
            get
            {
                // We mitigate circularity problems by only actively loading attributes that pass a syntactic check
                return GetWellKnownAttributeDataStringField(data => data.AssemblyKeyFileAttributeSetting,
                    WellKnownAttributeData.StringMissingValue, QuickAttributes.AssemblyKeyFile);
            }
        }

        private string AssemblyCultureAttributeSetting
        {
            get
            {
                return GetWellKnownAttributeDataStringField(data => data.AssemblyCultureAttributeSetting);
            }
        }

        public string SignatureKey
        {
            get
            {
                // We mitigate circularity problems by only actively loading attributes that pass a syntactic check
                return GetWellKnownAttributeDataStringField(data => data.AssemblySignatureKeyAttributeSetting,
                    missingValue: null, QuickAttributes.AssemblySignatureKey);
            }
        }

        private Version AssemblyVersionAttributeSetting
        {
            get
            {
                var defaultValue = (Version)null;
                var fieldValue = defaultValue;

                var data = GetSourceDecodedWellKnownAttributeData();
                if (data != null)
                {
                    fieldValue = data.AssemblyVersionAttributeSetting;
                }

                if (fieldValue == defaultValue)
                {
                    data = GetNetModuleDecodedWellKnownAttributeData();
                    if (data != null)
                    {
                        fieldValue = data.AssemblyVersionAttributeSetting;
                    }
                }

                return fieldValue;
            }
        }

        public override Version AssemblyVersionPattern
        {
            get
            {
                var attributeValue = AssemblyVersionAttributeSetting;
                return (object)attributeValue == null || (attributeValue.Build != ushort.MaxValue && attributeValue.Revision != ushort.MaxValue) ? null : attributeValue;
            }
        }

        public AssemblyHashAlgorithm HashAlgorithm
        {
            get
            {
                return AssemblyAlgorithmIdAttributeSetting ?? AssemblyHashAlgorithm.Sha1;
            }
        }

        internal AssemblyHashAlgorithm? AssemblyAlgorithmIdAttributeSetting
        {
            get
            {
                var fieldValue = (AssemblyHashAlgorithm?)null;

                var data = GetSourceDecodedWellKnownAttributeData();
                if (data != null)
                {
                    fieldValue = data.AssemblyAlgorithmIdAttributeSetting;
                }

                if (!fieldValue.HasValue)
                {
                    data = GetNetModuleDecodedWellKnownAttributeData();
                    if (data != null)
                    {
                        fieldValue = data.AssemblyAlgorithmIdAttributeSetting;
                    }
                }

                return fieldValue;
            }
        }

        /// <summary>
        /// This represents what the user claimed in source through the AssemblyFlagsAttribute.
        /// It may be modified as emitted due to presence or absence of the public key.
        /// </summary>
        public AssemblyFlags AssemblyFlags
        {
            get
            {
                var defaultValue = default(AssemblyFlags);
                var fieldValue = defaultValue;

                var data = GetSourceDecodedWellKnownAttributeData();
                if (data != null)
                {
                    fieldValue = data.AssemblyFlagsAttributeSetting;
                }

                data = GetNetModuleDecodedWellKnownAttributeData();
                if (data != null)
                {
                    fieldValue |= data.AssemblyFlagsAttributeSetting;
                }

                return fieldValue;
            }
        }

        private StrongNameKeys ComputeStrongNameKeys()
        {
            // when both attributes and command-line options specified, cmd line wins.
            string keyFile = _compilation.Options.CryptoKeyFile;

            // Public sign requires a keyfile
            if (DeclaringCompilation.Options.PublicSign)
            {
                // TODO(https://github.com/dotnet/roslyn/issues/9150):
                // Provide better error message if keys are provided by
                // the attributes. Right now we'll just fall through to the
                // "no key available" error.

                if (!string.IsNullOrEmpty(keyFile) && !PathUtilities.IsAbsolute(keyFile))
                {
                    // If keyFile has a relative path then there should be a diagnostic
                    // about it
                    Debug.Assert(!DeclaringCompilation.Options.Errors.IsEmpty);
                    return StrongNameKeys.None;
                }

                // If we're public signing, we don't need a strong name provider
                return StrongNameKeys.Create(keyFile, MessageProvider.Instance);
            }

            if (string.IsNullOrEmpty(keyFile))
            {
                keyFile = this.AssemblyKeyFileAttributeSetting;

                if ((object)keyFile == (object)WellKnownAttributeData.StringMissingValue)
                {
                    keyFile = null;
                }
            }

            string keyContainer = _compilation.Options.CryptoKeyContainer;

            if (string.IsNullOrEmpty(keyContainer))
            {
                keyContainer = this.AssemblyKeyContainerAttributeSetting;

                if ((object)keyContainer == (object)WellKnownAttributeData.StringMissingValue)
                {
                    keyContainer = null;
                }
            }

            var hasCounterSignature = !string.IsNullOrEmpty(this.SignatureKey);
            return StrongNameKeys.Create(DeclaringCompilation.Options.StrongNameProvider, keyFile, keyContainer, hasCounterSignature, MessageProvider.Instance);
        }

        // A collection of assemblies to which we were granted internals access by only checking matches for assembly name
        // and ignoring public key. This just acts as a set. The bool is ignored.
        private ConcurrentDictionary<AssemblySymbol, bool> _optimisticallyGrantedInternalsAccess;

        //EDMAURER please don't use thread local storage widely. This is hoped to be a one-off usage.
        [ThreadStatic]
        private static AssemblySymbol t_assemblyForWhichCurrentThreadIsComputingKeys;

        internal StrongNameKeys StrongNameKeys
        {
            get
            {
                if (_lazyStrongNameKeys == null)
                {
                    try
                    {
                        t_assemblyForWhichCurrentThreadIsComputingKeys = this;
                        Interlocked.CompareExchange(ref _lazyStrongNameKeys, ComputeStrongNameKeys(), null);
                    }
                    finally
                    {
                        t_assemblyForWhichCurrentThreadIsComputingKeys = null;
                    }
                }

                return _lazyStrongNameKeys;
            }
        }

        internal override ImmutableArray<byte> PublicKey
        {
            get { return StrongNameKeys.PublicKey; }
        }

        public override ImmutableArray<ModuleSymbol> Modules
        {
            get
            {
                return _modules;
            }
        }

        //TODO: cache
        public override ImmutableArray<Location> Locations
        {
            get
            {
                return this.Modules.SelectMany(m => m.Locations).AsImmutable();
            }
        }

        private void ValidateAttributeSemantics(BindingDiagnosticBag diagnostics)
        {
            //diagnostics that come from computing the public key.
            //If building a netmodule, strong name keys need not be validated. Dev11 didn't.
            if (StrongNameKeys.DiagnosticOpt != null && !_compilation.Options.OutputKind.IsNetModule())
            {
                diagnostics.Add(StrongNameKeys.DiagnosticOpt);
            }

            ValidateIVTPublicKeys(diagnostics);
            //diagnostics that result from IVT checks performed while in the process of computing the public key.
            CheckOptimisticIVTAccessGrants(diagnostics);

            DetectAttributeAndOptionConflicts(diagnostics);

            if (IsDelaySigned && !Identity.HasPublicKey)
            {
                diagnostics.Add(ErrorCode.WRN_DelaySignButNoKey, NoLocation.Singleton);
            }

            if (DeclaringCompilation.Options.PublicSign)
            {
                if (_compilation.Options.OutputKind.IsNetModule())
                {
                    diagnostics.Add(ErrorCode.ERR_PublicSignNetModule, NoLocation.Singleton);
                }
                else if (!Identity.HasPublicKey)
                {
                    diagnostics.Add(ErrorCode.ERR_PublicSignButNoKey, NoLocation.Singleton);
                }
            }

            // If the options and attributes applied on the compilation imply real signing,
            // but we have no private key to sign it with report an error.
            // Note that if public key is set and delay sign is off we do OSS signing, which doesn't require private key.
            // Consider: should we allow to OSS sign if the key file only contains public key?

            if (DeclaringCompilation.Options.OutputKind != OutputKind.NetModule &&
                DeclaringCompilation.Options.CryptoPublicKey.IsEmpty &&
                Identity.HasPublicKey &&
                !IsDelaySigned &&
                !DeclaringCompilation.Options.PublicSign &&
                !StrongNameKeys.CanSign &&
                StrongNameKeys.DiagnosticOpt == null)
            {
                // Since the container always contains both keys, the problem is that the key file didn't contain private key.
                diagnostics.Add(ErrorCode.ERR_SignButNoPrivateKey, NoLocation.Singleton, StrongNameKeys.KeyFilePath);
            }

            ReportDiagnosticsForSynthesizedAttributes(_compilation, diagnostics);
        }

        /// <summary>
        /// We're going to synthesize some well-known attributes for this assembly symbol.  However, at synthesis time, it is
        /// too late to report diagnostics or cancel the emit.  Instead, we check for use site errors on the types and members
        /// we know we'll need at synthesis time.
        /// </summary>
        /// <remarks>
        /// As in Dev10, we won't report anything if the attribute TYPES are missing (note: missing, not erroneous) because we won't
        /// synthesize anything in that case.  We'll only report diagnostics if the attribute TYPES are present and either they or 
        /// the attribute CONSTRUCTORS have errors.
        /// </remarks>
        private static void ReportDiagnosticsForSynthesizedAttributes(CSharpCompilation compilation, BindingDiagnosticBag diagnostics)
        {
            ReportDiagnosticsForUnsafeSynthesizedAttributes(compilation, diagnostics);

            CSharpCompilationOptions compilationOptions = compilation.Options;
            if (!compilationOptions.OutputKind.IsNetModule())
            {
                TypeSymbol compilationRelaxationsAttribute = compilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_CompilationRelaxationsAttribute);
                Debug.Assert((object)compilationRelaxationsAttribute != null, "GetWellKnownType unexpectedly returned null");
                if (!(compilationRelaxationsAttribute is MissingMetadataTypeSymbol))
                {
                    // As in Dev10 (see GlobalAttrBind::EmitCompilerGeneratedAttrs), we only synthesize this attribute if CompilationRelaxationsAttribute is found.
                    Binder.ReportUseSiteDiagnosticForSynthesizedAttribute(compilation,
                        WellKnownMember.System_Runtime_CompilerServices_CompilationRelaxationsAttribute__ctorInt32, diagnostics, NoLocation.Singleton);
                }

                TypeSymbol runtimeCompatibilityAttribute = compilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_RuntimeCompatibilityAttribute);
                Debug.Assert((object)runtimeCompatibilityAttribute != null, "GetWellKnownType unexpectedly returned null");
                if (!(runtimeCompatibilityAttribute is MissingMetadataTypeSymbol))
                {
                    // As in Dev10 (see GlobalAttrBind::EmitCompilerGeneratedAttrs), we only synthesize this attribute if RuntimeCompatibilityAttribute is found.
                    Binder.ReportUseSiteDiagnosticForSynthesizedAttribute(compilation,
                        WellKnownMember.System_Runtime_CompilerServices_RuntimeCompatibilityAttribute__ctor, diagnostics, NoLocation.Singleton);

                    Binder.ReportUseSiteDiagnosticForSynthesizedAttribute(compilation,
                        WellKnownMember.System_Runtime_CompilerServices_RuntimeCompatibilityAttribute__WrapNonExceptionThrows, diagnostics, NoLocation.Singleton);
                }
            }
        }

        /// <summary>
        /// If this compilation allows unsafe code (note: allows, not contains), then when we actually emit the assembly/module, 
        /// we're going to synthesize SecurityPermissionAttribute/UnverifiableCodeAttribute.  However, at synthesis time, it is
        /// too late to report diagnostics or cancel the emit.  Instead, we check for use site errors on the types and members
        /// we know we'll need at synthesis time.
        /// </summary>
        /// <remarks>
        /// As in Dev10, we won't report anything if the attribute TYPES are missing (note: missing, not erroneous) because we won't
        /// synthesize anything in that case.  We'll only report diagnostics if the attribute TYPES are present and either they or 
        /// the attribute CONSTRUCTORS have errors.
        /// </remarks>
        private static void ReportDiagnosticsForUnsafeSynthesizedAttributes(CSharpCompilation compilation, BindingDiagnosticBag diagnostics)
        {
            CSharpCompilationOptions compilationOptions = compilation.Options;
            if (!compilationOptions.AllowUnsafe)
            {
                return;
            }

            TypeSymbol unverifiableCodeAttribute = compilation.GetWellKnownType(WellKnownType.System_Security_UnverifiableCodeAttribute);
            Debug.Assert((object)unverifiableCodeAttribute != null, "GetWellKnownType unexpectedly returned null");
            if (unverifiableCodeAttribute is MissingMetadataTypeSymbol)
            {
                return;
            }

            // As in Dev10 (see GlobalAttrBind::EmitCompilerGeneratedAttrs), we only synthesize this attribute if
            // UnverifiableCodeAttribute is found.
            Binder.ReportUseSiteDiagnosticForSynthesizedAttribute(compilation,
                WellKnownMember.System_Security_UnverifiableCodeAttribute__ctor, diagnostics, NoLocation.Singleton);

            TypeSymbol securityPermissionAttribute = compilation.GetWellKnownType(WellKnownType.System_Security_Permissions_SecurityPermissionAttribute);
            Debug.Assert((object)securityPermissionAttribute != null, "GetWellKnownType unexpectedly returned null");
            if (securityPermissionAttribute is MissingMetadataTypeSymbol)
            {
                return;
            }

            TypeSymbol securityAction = compilation.GetWellKnownType(WellKnownType.System_Security_Permissions_SecurityAction);
            Debug.Assert((object)securityAction != null, "GetWellKnownType unexpectedly returned null");
            if (securityAction is MissingMetadataTypeSymbol)
            {
                return;
            }

            // As in Dev10 (see GlobalAttrBind::EmitCompilerGeneratedAttrs), we only synthesize this attribute if
            // UnverifiableCodeAttribute, SecurityAction, and SecurityPermissionAttribute are found.
            Binder.ReportUseSiteDiagnosticForSynthesizedAttribute(compilation,
                WellKnownMember.System_Security_Permissions_SecurityPermissionAttribute__ctor, diagnostics, NoLocation.Singleton);

            // Not actually an attribute, but the same logic applies.
            Binder.ReportUseSiteDiagnosticForSynthesizedAttribute(compilation,
                WellKnownMember.System_Security_Permissions_SecurityPermissionAttribute__SkipVerification, diagnostics, NoLocation.Singleton);
        }

        private void ValidateIVTPublicKeys(BindingDiagnosticBag diagnostics)
        {
            EnsureAttributesAreBound();

            if (!this.Identity.IsStrongName)
                return;

            if (_lazyInternalsVisibleToMap != null)
            {
                foreach (var keys in _lazyInternalsVisibleToMap.Values)
                {
                    foreach (var oneKey in keys)
                    {
                        if (oneKey.Key.IsDefaultOrEmpty)
                        {
                            diagnostics.Add(ErrorCode.ERR_FriendAssemblySNReq, oneKey.Value.Item1, oneKey.Value.Item2);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// True if internals are exposed at all.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// This property shouldn't be accessed during binding as it can lead to attribute binding cycle.
        /// </remarks>
        public bool InternalsAreVisible
        {
            get
            {
                EnsureAttributesAreBound();
                return _lazyInternalsVisibleToMap != null;
            }
        }

        private void DetectAttributeAndOptionConflicts(BindingDiagnosticBag diagnostics)
        {
            EnsureAttributesAreBound();

            ThreeState assemblyDelaySignAttributeSetting = this.AssemblyDelaySignAttributeSetting;
            if (_compilation.Options.DelaySign.HasValue && (assemblyDelaySignAttributeSetting != ThreeState.Unknown) &&
                (DeclaringCompilation.Options.DelaySign.Value != (assemblyDelaySignAttributeSetting == ThreeState.True)))
            {
                diagnostics.Add(ErrorCode.WRN_CmdOptionConflictsSource, NoLocation.Singleton, "DelaySign", AttributeDescription.AssemblyDelaySignAttribute.FullName);
            }

            if (_compilation.Options.PublicSign && assemblyDelaySignAttributeSetting == ThreeState.True)
            {
                diagnostics.Add(ErrorCode.WRN_CmdOptionConflictsSource, NoLocation.Singleton,
                    nameof(_compilation.Options.PublicSign),
                    AttributeDescription.AssemblyDelaySignAttribute.FullName);
            }

            if (!String.IsNullOrEmpty(_compilation.Options.CryptoKeyContainer))
            {
                string assemblyKeyContainerAttributeSetting = this.AssemblyKeyContainerAttributeSetting;

                if ((object)assemblyKeyContainerAttributeSetting == (object)CommonAssemblyWellKnownAttributeData.StringMissingValue)
                {
                    if (_compilation.Options.OutputKind == OutputKind.NetModule)
                    {
                        // We need to synthesize this attribute for .NET module,
                        // touch the constructor in order to generate proper use-site diagnostics
                        Binder.ReportUseSiteDiagnosticForSynthesizedAttribute(_compilation,
                            WellKnownMember.System_Reflection_AssemblyKeyNameAttribute__ctor,
                            diagnostics,
                            NoLocation.Singleton);
                    }
                }
                else if (String.Compare(_compilation.Options.CryptoKeyContainer, assemblyKeyContainerAttributeSetting, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    // Native compiler reports a warning in this case, notifying the user that attribute value from source is ignored,
                    // but it doesn't drop the attribute during emit. That might be fine if we produce an assembly because we actually sign it with correct
                    // key (the one from compilation options) without relying on the emitted attribute.
                    // If we are building a .NET module, things get more complicated. In particular, we don't sign the module, we emit an attribute with the key 
                    // information, which will be used to sign an assembly once the module is linked into it. If there is already an attribute like that in source,
                    // native compiler emits both of them, synthetic attribute is emitted after the one from source. Incidentally, ALink picks the last attribute
                    // for signing and things seem to work out. However, relying on the order of attributes feels fragile, especially given that Roslyn emits
                    // synthetic attributes before attributes from source. The behavior we settled on for .NET modules is that, if the attribute in source has the
                    // same value as the one in compilation options, we won't emit the synthetic attribute. If the value doesn't match, we report an error, which 
                    // is a breaking change. Bottom line, we will never produce a module or an assembly with two attributes, regardless whether values are the same
                    // or not.
                    if (_compilation.Options.OutputKind == OutputKind.NetModule)
                    {
                        diagnostics.Add(ErrorCode.ERR_CmdOptionConflictsSource, NoLocation.Singleton, AttributeDescription.AssemblyKeyNameAttribute.FullName, "CryptoKeyContainer");
                    }
                    else
                    {
                        diagnostics.Add(ErrorCode.WRN_CmdOptionConflictsSource, NoLocation.Singleton, "CryptoKeyContainer", AttributeDescription.AssemblyKeyNameAttribute.FullName);
                    }
                }
            }

            if (_compilation.Options.PublicSign &&
                !_compilation.Options.OutputKind.IsNetModule() &&
                (object)this.AssemblyKeyContainerAttributeSetting != (object)CommonAssemblyWellKnownAttributeData.StringMissingValue)
            {
                diagnostics.Add(ErrorCode.WRN_AttributeIgnoredWhenPublicSigning, NoLocation.Singleton, AttributeDescription.AssemblyKeyNameAttribute.FullName);
            }

            if (!String.IsNullOrEmpty(_compilation.Options.CryptoKeyFile))
            {
                string assemblyKeyFileAttributeSetting = this.AssemblyKeyFileAttributeSetting;

                if ((object)assemblyKeyFileAttributeSetting == (object)CommonAssemblyWellKnownAttributeData.StringMissingValue)
                {
                    if (_compilation.Options.OutputKind == OutputKind.NetModule)
                    {
                        // We need to synthesize this attribute for .NET module,
                        // touch the constructor in order to generate proper use-site diagnostics
                        Binder.ReportUseSiteDiagnosticForSynthesizedAttribute(_compilation,
                            WellKnownMember.System_Reflection_AssemblyKeyFileAttribute__ctor,
                            diagnostics,
                            NoLocation.Singleton);
                    }
                }
                else if (String.Compare(_compilation.Options.CryptoKeyFile, assemblyKeyFileAttributeSetting, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    // Comment in similar section for CryptoKeyContainer is applicable here as well.
                    if (_compilation.Options.OutputKind == OutputKind.NetModule)
                    {
                        diagnostics.Add(ErrorCode.ERR_CmdOptionConflictsSource, NoLocation.Singleton, AttributeDescription.AssemblyKeyFileAttribute.FullName, "CryptoKeyFile");
                    }
                    else
                    {
                        diagnostics.Add(ErrorCode.WRN_CmdOptionConflictsSource, NoLocation.Singleton, "CryptoKeyFile", AttributeDescription.AssemblyKeyFileAttribute.FullName);
                    }
                }
            }

            if (_compilation.Options.PublicSign &&
                !_compilation.Options.OutputKind.IsNetModule() &&
                (object)this.AssemblyKeyFileAttributeSetting != (object)CommonAssemblyWellKnownAttributeData.StringMissingValue)
            {
                diagnostics.Add(ErrorCode.WRN_AttributeIgnoredWhenPublicSigning, NoLocation.Singleton, AttributeDescription.AssemblyKeyFileAttribute.FullName);
            }
        }

        internal bool IsDelaySigned
        {
            get
            {
                //commandline setting trumps attribute value. Warning assumed to be given elsewhere
                if (_compilation.Options.DelaySign.HasValue)
                {
                    return _compilation.Options.DelaySign.Value;
                }

                // The public sign argument should also override the attribute
                if (_compilation.Options.PublicSign)
                {
                    return false;
                }

                return (this.AssemblyDelaySignAttributeSetting == ThreeState.True);
            }
        }

        internal SourceModuleSymbol SourceModule
        {
            get { return (SourceModuleSymbol)this.Modules[0]; }
        }

        internal override bool RequiresCompletion
        {
            get { return true; }
        }

        internal override bool HasComplete(CompletionPart part)
        {
            return _state.HasComplete(part);
        }

#nullable enable
        internal override void ForceComplete(SourceLocation? locationOpt, Predicate<Symbol>? filter, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var incompletePart = _state.NextIncompletePart;
                switch (incompletePart)
                {
                    case CompletionPart.Attributes:
                        EnsureAttributesAreBound();
                        break;
                    case CompletionPart.StartAttributeChecks:
                    case CompletionPart.FinishAttributeChecks:
                        if (_state.NotePartComplete(CompletionPart.StartAttributeChecks))
                        {
                            var diagnostics = BindingDiagnosticBag.GetInstance();
                            ValidateAttributeSemantics(diagnostics);
                            AddDeclarationDiagnostics(diagnostics);
                            var thisThreadCompleted = _state.NotePartComplete(CompletionPart.FinishAttributeChecks);
                            Debug.Assert(thisThreadCompleted);
                            diagnostics.Free();
                        }
                        break;
                    case CompletionPart.Module:
                        SourceModule.ForceComplete(locationOpt, filter, cancellationToken);
                        if (SourceModule.HasComplete(CompletionPart.MembersCompleted))
                        {
                            _state.NotePartComplete(CompletionPart.Module);
                            break;
                        }
                        else
                        {
                            Debug.Assert(locationOpt != null || filter != null, "If no location or filter was specified, then the module members should be completed");
                            // this is the last completion part we can handle if there is a location.
                            return;
                        }

                    case CompletionPart.StartValidatingAddedModules:
                    case CompletionPart.FinishValidatingAddedModules:
                        if (_state.NotePartComplete(CompletionPart.StartValidatingAddedModules))
                        {
                            ReportDiagnosticsForAddedModules();
                            var thisThreadCompleted = _state.NotePartComplete(CompletionPart.FinishValidatingAddedModules);
                            Debug.Assert(thisThreadCompleted);
                        }
                        break;

                    case CompletionPart.None:
                        return;

                    default:
                        // any other values are completion parts intended for other kinds of symbols
                        _state.NotePartComplete(CompletionPart.All & ~CompletionPart.AssemblySymbolAll);
                        break;
                }

                _state.SpinWaitComplete(incompletePart, cancellationToken);
            }
        }
#nullable disable

        private void ReportDiagnosticsForAddedModules()
        {
            var diagnostics = BindingDiagnosticBag.GetInstance();

            foreach (var pair in _compilation.GetBoundReferenceManager().ReferencedModuleIndexMap)
            {
                var fileRef = pair.Key as PortableExecutableReference;

                if ((object)fileRef != null && (object)fileRef.FilePath != null)
                {
                    string fileName = FileNameUtilities.GetFileName(fileRef.FilePath);
                    string moduleName = _modules[pair.Value].Name;

                    if (!string.Equals(fileName, moduleName, StringComparison.OrdinalIgnoreCase))
                    {
                        // Used to be ERR_ALinkFailed
                        diagnostics.Add(ErrorCode.ERR_NetModuleNameMismatch, NoLocation.Singleton, moduleName, fileName);
                    }
                }
            }

            // Alink performed these checks only when emitting an assembly.
            if (_modules.Length > 1 && !_compilation.Options.OutputKind.IsNetModule())
            {
                var assemblyMachine = this.Machine;
                bool isPlatformAgnostic = (assemblyMachine == System.Reflection.PortableExecutable.Machine.I386 && !this.Bit32Required);
                var knownModuleNames = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

                for (int i = 1; i < _modules.Length; i++)
                {
                    ModuleSymbol m = _modules[i];
                    if (!knownModuleNames.Add(m.Name))
                    {
                        diagnostics.Add(ErrorCode.ERR_NetModuleNameMustBeUnique, NoLocation.Singleton, m.Name);
                    }

                    if (!((PEModuleSymbol)m).Module.IsCOFFOnly)
                    {
                        var moduleMachine = m.Machine;

                        if (moduleMachine == System.Reflection.PortableExecutable.Machine.I386 && !m.Bit32Required)
                        {
                            // Other module is agnostic, this is always safe
                            ;
                        }
                        else if (isPlatformAgnostic)
                        {
                            diagnostics.Add(ErrorCode.ERR_AgnosticToMachineModule, NoLocation.Singleton, m);
                        }
                        else if (assemblyMachine != moduleMachine)
                        {
                            // Different machine types, and neither is agnostic
                            // So it is a conflict
                            diagnostics.Add(ErrorCode.ERR_ConflictingMachineModule, NoLocation.Singleton, m);
                        }
                    }
                }

                // Assembly main module must explicitly reference all the modules referenced by other assembly 
                // modules, i.e. all modules from transitive closure must be referenced explicitly here
                for (int i = 1; i < _modules.Length; i++)
                {
                    var m = (PEModuleSymbol)_modules[i];

                    try
                    {
                        foreach (var referencedModuleName in m.Module.GetReferencedManagedModulesOrThrow())
                        {
                            // Do not report error for this module twice
                            if (knownModuleNames.Add(referencedModuleName))
                            {
                                diagnostics.Add(ErrorCode.ERR_MissingNetModuleReference, NoLocation.Singleton, referencedModuleName);
                            }
                        }
                    }
                    catch (BadImageFormatException)
                    {
                        diagnostics.Add(new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, m), NoLocation.Singleton);
                    }
                }
            }

            ReportNameCollisionDiagnosticsForAddedModules(this.GlobalNamespace, diagnostics);
            AddDeclarationDiagnostics(diagnostics);
            diagnostics.Free();
        }

        private void ReportNameCollisionDiagnosticsForAddedModules(NamespaceSymbol ns, BindingDiagnosticBag diagnostics)
        {
            var mergedNs = ns as MergedNamespaceSymbol;

            if ((object)mergedNs == null)
            {
                return;
            }

            ImmutableArray<NamespaceSymbol> constituent = mergedNs.ConstituentNamespaces;

            if (constituent.Length > 2 || (constituent.Length == 2 && constituent[0].ContainingModule.Ordinal != 0 && constituent[1].ContainingModule.Ordinal != 0))
            {
                var topLevelTypesFromModules = ArrayBuilder<NamedTypeSymbol>.GetInstance();

                foreach (var moduleNs in constituent)
                {
                    Debug.Assert(moduleNs.Extent.Kind == NamespaceKind.Module);

                    if (moduleNs.ContainingModule.Ordinal != 0)
                    {
                        topLevelTypesFromModules.AddRange(moduleNs.GetTypeMembers());
                    }
                }

                topLevelTypesFromModules.Sort(NameCollisionForAddedModulesTypeComparer.Singleton);

                bool reportedAnError = false;

                for (int i = 0; i < topLevelTypesFromModules.Count - 1; i++)
                {
                    NamedTypeSymbol x = topLevelTypesFromModules[i];
                    NamedTypeSymbol y = topLevelTypesFromModules[i + 1];

                    if (x.Arity == y.Arity && x.Name == y.Name)
                    {
                        if (!reportedAnError)
                        {
                            // Skip synthetic <Module> type which every .NET module has.
                            if (x.Arity != 0 || !x.ContainingNamespace.IsGlobalNamespace || x.Name != "<Module>")
                            {
                                diagnostics.Add(ErrorCode.ERR_DuplicateNameInNS, y.GetFirstLocationOrNone(),
                                                y.ToDisplayString(SymbolDisplayFormat.ShortFormat),
                                                y.ContainingNamespace);
                            }

                            reportedAnError = true;
                        }
                    }
                    else
                    {
                        reportedAnError = false;
                    }
                }

                topLevelTypesFromModules.Free();

                // Descent into child namespaces.
                foreach (Symbol member in mergedNs.GetMembers())
                {
                    if (member.Kind == SymbolKind.Namespace)
                    {
                        ReportNameCollisionDiagnosticsForAddedModules((NamespaceSymbol)member, diagnostics);
                    }
                }
            }
        }

        private class NameCollisionForAddedModulesTypeComparer : IComparer<NamedTypeSymbol>
        {
            public static readonly NameCollisionForAddedModulesTypeComparer Singleton = new NameCollisionForAddedModulesTypeComparer();

            private NameCollisionForAddedModulesTypeComparer() { }

            public int Compare(NamedTypeSymbol x, NamedTypeSymbol y)
            {
                int result = String.CompareOrdinal(x.Name, y.Name);

                if (result == 0)
                {
                    result = x.Arity - y.Arity;

                    if (result == 0)
                    {
                        result = x.ContainingModule.Ordinal - y.ContainingModule.Ordinal;
                    }
                }

                return result;
            }
        }

        private bool IsKnownAssemblyAttribute(CSharpAttributeData attribute)
        {
            // TODO: This list used to include AssemblyOperatingSystemAttribute and AssemblyProcessorAttribute,
            //       but it doesn't look like they are defined, cannot find them on MSDN.
            if (attribute.IsTargetAttribute(AttributeDescription.AssemblyTitleAttribute) ||
                attribute.IsTargetAttribute(AttributeDescription.AssemblyDescriptionAttribute) ||
                attribute.IsTargetAttribute(AttributeDescription.AssemblyConfigurationAttribute) ||
                attribute.IsTargetAttribute(AttributeDescription.AssemblyCultureAttribute) ||
                attribute.IsTargetAttribute(AttributeDescription.AssemblyVersionAttribute) ||
                attribute.IsTargetAttribute(AttributeDescription.AssemblyCompanyAttribute) ||
                attribute.IsTargetAttribute(AttributeDescription.AssemblyProductAttribute) ||
                attribute.IsTargetAttribute(AttributeDescription.AssemblyInformationalVersionAttribute) ||
                attribute.IsTargetAttribute(AttributeDescription.AssemblyCopyrightAttribute) ||
                attribute.IsTargetAttribute(AttributeDescription.AssemblyTrademarkAttribute) ||
                attribute.IsTargetAttribute(AttributeDescription.AssemblyKeyFileAttribute) ||
                attribute.IsTargetAttribute(AttributeDescription.AssemblyKeyNameAttribute) ||
                attribute.IsTargetAttribute(AttributeDescription.AssemblyAlgorithmIdAttribute) ||
                attribute.IsTargetAttribute(AttributeDescription.AssemblyFlagsAttribute) ||
                attribute.IsTargetAttribute(AttributeDescription.AssemblyDelaySignAttribute) ||
                attribute.IsTargetAttribute(AttributeDescription.AssemblyFileVersionAttribute) ||
                attribute.IsTargetAttribute(AttributeDescription.SatelliteContractVersionAttribute) ||
                attribute.IsTargetAttribute(AttributeDescription.AssemblySignatureKeyAttribute))
            {
                return true;
            }

            return false;
        }

        private void AddOmittedAttributeIndex(int index)
        {
            if (_lazyOmittedAttributeIndices == null)
            {
                Interlocked.CompareExchange(ref _lazyOmittedAttributeIndices, new ConcurrentSet<int>(), null);
            }

            _lazyOmittedAttributeIndices.Add(index);
        }

        /// <summary>
        /// Gets unique source assembly attributes that should be emitted,
        /// i.e. filters out attributes with errors and duplicate attributes.
        /// </summary>
        private HashSet<CSharpAttributeData> GetUniqueSourceAssemblyAttributes()
        {
            ImmutableArray<CSharpAttributeData> appliedSourceAttributes = this.GetSourceAttributesBag().Attributes;

            HashSet<CSharpAttributeData> uniqueAttributes = null;

            for (int i = 0; i < appliedSourceAttributes.Length; i++)
            {
                CSharpAttributeData attribute = appliedSourceAttributes[i];
                if (!attribute.HasErrors)
                {
                    if (!AddUniqueAssemblyAttribute(attribute, ref uniqueAttributes))
                    {
                        AddOmittedAttributeIndex(i);
                    }
                }
            }

            return uniqueAttributes;
        }

        private static bool AddUniqueAssemblyAttribute(CSharpAttributeData attribute, ref HashSet<CSharpAttributeData> uniqueAttributes)
        {
            Debug.Assert(!attribute.HasErrors);

            if (uniqueAttributes == null)
            {
                uniqueAttributes = new HashSet<CSharpAttributeData>(comparer: CommonAttributeDataComparer.Instance);
            }

            return uniqueAttributes.Add(attribute);
        }

        private bool ValidateAttributeUsageForNetModuleAttribute(CSharpAttributeData attribute, string netModuleName, BindingDiagnosticBag diagnostics, ref HashSet<CSharpAttributeData> uniqueAttributes)
        {
            Debug.Assert(!attribute.HasErrors);

            var attributeClass = attribute.AttributeClass;

            if (attributeClass.GetAttributeUsageInfo().AllowMultiple)
            {
                // Duplicate attributes are allowed, but native compiler doesn't emit duplicate attributes, i.e. attributes with same constructor and arguments.
                return AddUniqueAssemblyAttribute(attribute, ref uniqueAttributes);
            }
            else
            {
                // Duplicate attributes with same attribute type are not allowed.
                // Check if there is an existing assembly attribute with same attribute type.
                if (uniqueAttributes == null || !uniqueAttributes.Contains((a) => TypeSymbol.Equals(a.AttributeClass, attributeClass, TypeCompareKind.ConsiderEverything2)))
                {
                    // Attribute with unique attribute type, not a duplicate.
                    bool success = AddUniqueAssemblyAttribute(attribute, ref uniqueAttributes);
                    Debug.Assert(success);
                    return true;
                }
                else
                {
                    // Duplicate attribute with same attribute type, we should report an error.

                    // Native compiler suppresses the error for
                    // (a) Duplicate well-known assembly attributes and
                    // (b) Identical duplicates, i.e. attributes with same constructor and arguments.

                    // For (a), native compiler picks the last of these duplicate well-known netmodule attributes, but these can vary based on the ordering of referenced netmodules.

                    if (IsKnownAssemblyAttribute(attribute))
                    {
                        if (!uniqueAttributes.Contains(attribute))
                        {
                            // This attribute application will be ignored.
                            diagnostics.Add(ErrorCode.WRN_AssemblyAttributeFromModuleIsOverridden, NoLocation.Singleton, attribute.AttributeClass, netModuleName);
                        }
                    }
                    else if (AddUniqueAssemblyAttribute(attribute, ref uniqueAttributes))
                    {
                        // Error
                        diagnostics.Add(ErrorCode.ERR_DuplicateAttributeInNetModule, NoLocation.Singleton, attribute.AttributeClass.Name, netModuleName);
                    }

                    return false;
                }
            }

            // CONSIDER Handling badly targeted assembly attributes from netmodules
            //if (!badDuplicateAttribute && ((attributeUsageInfo.ValidTargets & AttributeTargets.Assembly) == 0))
            //{
            //    // Error and skip
            //    diagnostics.Add(ErrorCode.ERR_AttributeOnBadSymbolTypeInNetModule, NoLocation.Singleton, attribute.AttributeClass.Name, netModuleName, attributeUsageInfo.GetValidTargetsString());
            //    return false;
            //}
        }

        private ImmutableArray<CSharpAttributeData> GetNetModuleAttributes(out ImmutableArray<string> netModuleNames)
        {
            ArrayBuilder<CSharpAttributeData> moduleAssemblyAttributesBuilder = null;
            ArrayBuilder<string> netModuleNameBuilder = null;

            for (int i = 1; i < _modules.Length; i++)
            {
                var peModuleSymbol = (Metadata.PE.PEModuleSymbol)_modules[i];
                string netModuleName = peModuleSymbol.Name;
                foreach (var attributeData in peModuleSymbol.GetAssemblyAttributes())
                {
                    if (netModuleNameBuilder == null)
                    {
                        netModuleNameBuilder = ArrayBuilder<string>.GetInstance();
                        moduleAssemblyAttributesBuilder = ArrayBuilder<CSharpAttributeData>.GetInstance();
                    }

                    netModuleNameBuilder.Add(netModuleName);
                    moduleAssemblyAttributesBuilder.Add(attributeData);
                }
            }

            if (netModuleNameBuilder == null)
            {
                netModuleNames = ImmutableArray<string>.Empty;
                return ImmutableArray<CSharpAttributeData>.Empty;
            }

            netModuleNames = netModuleNameBuilder.ToImmutableAndFree();
            return moduleAssemblyAttributesBuilder.ToImmutableAndFree();
        }

        private WellKnownAttributeData ValidateAttributeUsageAndDecodeWellKnownAttributes(
            ImmutableArray<CSharpAttributeData> attributesFromNetModules,
            ImmutableArray<string> netModuleNames,
            BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(attributesFromNetModules.Any());
            Debug.Assert(netModuleNames.Any());
            Debug.Assert(attributesFromNetModules.Length == netModuleNames.Length);

            int netModuleAttributesCount = attributesFromNetModules.Length;
            int sourceAttributesCount = this.GetSourceAttributesBag().Attributes.Length;

            // Get unique source assembly attributes.
            HashSet<CSharpAttributeData> uniqueAttributes = GetUniqueSourceAssemblyAttributes();

            var arguments = new DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation>();
            arguments.AttributesCount = netModuleAttributesCount;
            arguments.Diagnostics = diagnostics;
            arguments.SymbolPart = AttributeLocation.None;

            // Attributes from the second added module should override attributes from the first added module, etc. 
            // Attributes from source should override attributes from added modules.
            // That is why we are iterating attributes backwards.
            for (int i = netModuleAttributesCount - 1; i >= 0; i--)
            {
                var totalIndex = i + sourceAttributesCount;

                CSharpAttributeData attribute = attributesFromNetModules[i];

                diagnostics.Add(attribute.ErrorInfo, NoLocation.Singleton);

                if (!attribute.HasErrors && ValidateAttributeUsageForNetModuleAttribute(attribute, netModuleNames[i], diagnostics, ref uniqueAttributes))
                {
                    arguments.Attribute = attribute;
                    arguments.Index = i;

                    // CONSIDER: Provide usable AttributeSyntax node for diagnostics of malformed netmodule assembly attributes
                    arguments.AttributeSyntaxOpt = null;

                    this.DecodeWellKnownAttribute(ref arguments, totalIndex, isFromNetModule: true);
                }
                else
                {
                    AddOmittedAttributeIndex(totalIndex);
                }
            }

            return arguments.HasDecodedData ? arguments.DecodedData : null;
        }

        private void LoadAndValidateNetModuleAttributes(ref CustomAttributesBag<CSharpAttributeData> lazyNetModuleAttributesBag)
        {
            if (_compilation.Options.OutputKind.IsNetModule())
            {
                Interlocked.CompareExchange(ref lazyNetModuleAttributesBag, CustomAttributesBag<CSharpAttributeData>.Empty, null);
            }
            else
            {
                var diagnostics = BindingDiagnosticBag.GetInstance();

                ImmutableArray<string> netModuleNames;
                ImmutableArray<CSharpAttributeData> attributesFromNetModules = GetNetModuleAttributes(out netModuleNames);

                WellKnownAttributeData wellKnownData = null;

                if (attributesFromNetModules.Any())
                {
                    wellKnownData = ValidateAttributeUsageAndDecodeWellKnownAttributes(attributesFromNetModules, netModuleNames, diagnostics);
                }
                else
                {
                    // Compute duplicate source assembly attributes, i.e. attributes with same constructor and arguments, that must not be emitted.
                    var unused = GetUniqueSourceAssemblyAttributes();
                }

                // Load type forwarders from modules
                HashSet<NamedTypeSymbol> forwardedTypes = null;

                // Similar to attributes, type forwarders from the second added module should override type forwarders from the first added module, etc. 
                // This affects only diagnostics.
                for (int i = _modules.Length - 1; i > 0; i--)
                {
                    var peModuleSymbol = (PEModuleSymbol)_modules[i];

                    foreach (NamedTypeSymbol forwarded in peModuleSymbol.GetForwardedTypes())
                    {
                        if (forwardedTypes == null)
                        {
                            if (wellKnownData == null)
                            {
                                wellKnownData = new CommonAssemblyWellKnownAttributeData();
                            }

                            forwardedTypes = ((CommonAssemblyWellKnownAttributeData)wellKnownData).ForwardedTypes;
                            if (forwardedTypes == null)
                            {
                                forwardedTypes = new HashSet<NamedTypeSymbol>();
                                ((CommonAssemblyWellKnownAttributeData)wellKnownData).ForwardedTypes = forwardedTypes;
                            }
                        }

                        if (forwardedTypes.Add(forwarded))
                        {
                            if (forwarded.IsErrorType())
                            {
                                if (!diagnostics.ReportUseSite(forwarded, NoLocation.Singleton))
                                {
                                    DiagnosticInfo info = ((ErrorTypeSymbol)forwarded).ErrorInfo;

                                    if ((object)info != null)
                                    {
                                        diagnostics.Add(info, NoLocation.Singleton);
                                    }
                                }
                            }
                        }
                    }
                }

                CustomAttributesBag<CSharpAttributeData> netModuleAttributesBag;

                if (wellKnownData != null || attributesFromNetModules.Any())
                {
                    netModuleAttributesBag = new CustomAttributesBag<CSharpAttributeData>();

                    netModuleAttributesBag.SetEarlyDecodedWellKnownAttributeData(null);
                    netModuleAttributesBag.SetDecodedWellKnownAttributeData(wellKnownData);
                    netModuleAttributesBag.SetAttributes(attributesFromNetModules);
                    if (netModuleAttributesBag.IsEmpty) netModuleAttributesBag = CustomAttributesBag<CSharpAttributeData>.Empty;
                }
                else
                {
                    netModuleAttributesBag = CustomAttributesBag<CSharpAttributeData>.Empty;
                }

                if (Interlocked.CompareExchange(ref lazyNetModuleAttributesBag, netModuleAttributesBag, null) == null)
                {
                    this.AddDeclarationDiagnostics(diagnostics);
                }

                diagnostics.Free();

                Debug.Assert(lazyNetModuleAttributesBag.IsSealed);
            }
        }

        private CommonAssemblyWellKnownAttributeData GetLimitedNetModuleDecodedWellKnownAttributeData(QuickAttributes attributeMatches)
        {
            Debug.Assert(attributeMatches is QuickAttributes.AssemblyKeyFile
                or QuickAttributes.AssemblyKeyName
                or QuickAttributes.AssemblySignatureKey);

            if (_compilation.Options.OutputKind.IsNetModule())
            {
                return null;
            }

            ImmutableArray<string> netModuleNames;
            ImmutableArray<CSharpAttributeData> attributesFromNetModules = GetNetModuleAttributes(out netModuleNames);

            WellKnownAttributeData wellKnownData = null;

            if (attributesFromNetModules.Any())
            {
                wellKnownData = limitedDecodeWellKnownAttributes(attributesFromNetModules, netModuleNames, attributeMatches);
            }

            return (CommonAssemblyWellKnownAttributeData)wellKnownData;

            // Similar to ValidateAttributeUsageAndDecodeWellKnownAttributes, but doesn't load assembly-level attributes from source
            // and only decodes 3 specific attributes.
            WellKnownAttributeData limitedDecodeWellKnownAttributes(ImmutableArray<CSharpAttributeData> attributesFromNetModules,
                ImmutableArray<string> netModuleNames, QuickAttributes attributeMatches)
            {
                Debug.Assert(attributesFromNetModules.Any());
                Debug.Assert(netModuleNames.Any());
                Debug.Assert(attributesFromNetModules.Length == netModuleNames.Length);

                int netModuleAttributesCount = attributesFromNetModules.Length;

                HashSet<CSharpAttributeData> uniqueAttributes = null;
                CommonAssemblyWellKnownAttributeData result = null;

                // Attributes from the second added module should override attributes from the first added module, etc.
                // We don't reach here when the attribute was found in source already.
                for (int i = netModuleAttributesCount - 1; i >= 0; i--)
                {
                    CSharpAttributeData attribute = attributesFromNetModules[i];
                    if (!attribute.HasErrors && ValidateAttributeUsageForNetModuleAttribute(attribute, netModuleNames[i], BindingDiagnosticBag.Discarded, ref uniqueAttributes))
                    {
                        limitedDecodeWellKnownAttribute(attribute, attributeMatches, ref result);
                    }
                }

                WellKnownAttributeData.Seal(result);
                return result;
            }

            // Similar to DecodeWellKnownAttribute but only handles 3 specific attributes and ignores diagnostics.
            void limitedDecodeWellKnownAttribute(CSharpAttributeData attribute, QuickAttributes attributeMatches, ref CommonAssemblyWellKnownAttributeData result)
            {
                if (attributeMatches is QuickAttributes.AssemblySignatureKey &&
                    attribute.IsTargetAttribute(AttributeDescription.AssemblySignatureKeyAttribute))
                {
                    result ??= new CommonAssemblyWellKnownAttributeData();
                    result.AssemblySignatureKeyAttributeSetting = (string)attribute.CommonConstructorArguments[0].ValueInternal;
                }
                else if (attributeMatches is QuickAttributes.AssemblyKeyFile &&
                    attribute.IsTargetAttribute(AttributeDescription.AssemblyKeyFileAttribute))
                {
                    result ??= new CommonAssemblyWellKnownAttributeData();
                    result.AssemblyKeyFileAttributeSetting = (string)attribute.CommonConstructorArguments[0].ValueInternal;
                }
                else if (attributeMatches is QuickAttributes.AssemblyKeyName &&
                    attribute.IsTargetAttribute(AttributeDescription.AssemblyKeyNameAttribute))
                {
                    result ??= new CommonAssemblyWellKnownAttributeData();
                    result.AssemblyKeyContainerAttributeSetting = (string)attribute.CommonConstructorArguments[0].ValueInternal;
                }
            }
        }

        private CustomAttributesBag<CSharpAttributeData> GetNetModuleAttributesBag()
        {
            if (_lazyNetModuleAttributesBag == null)
            {
                LoadAndValidateNetModuleAttributes(ref _lazyNetModuleAttributesBag);
            }
            return _lazyNetModuleAttributesBag;
        }

        internal CommonAssemblyWellKnownAttributeData GetNetModuleDecodedWellKnownAttributeData()
        {
            var attributesBag = this.GetNetModuleAttributesBag();
            Debug.Assert(attributesBag.IsSealed);
            return (CommonAssemblyWellKnownAttributeData)attributesBag.DecodedWellKnownAttributeData;
        }

        internal ImmutableArray<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            var builder = ArrayBuilder<SyntaxList<AttributeListSyntax>>.GetInstance();
            var declarations = DeclaringCompilation.MergedRootDeclaration.Declarations;
            foreach (RootSingleNamespaceDeclaration rootNs in declarations)
            {
                if (rootNs.HasAssemblyAttributes)
                {
                    var tree = rootNs.Location.SourceTree;
                    var root = (CompilationUnitSyntax)tree.GetRoot();
                    builder.Add(root.AttributeLists);
                }
            }
            return builder.ToImmutableAndFree();
        }

        private void EnsureAttributesAreBound()
        {
            if ((_lazySourceAttributesBag == null || !_lazySourceAttributesBag.IsSealed) &&
                LoadAndValidateAttributes(OneOrMany.Create(GetAttributeDeclarations()), ref _lazySourceAttributesBag))
            {
                _state.NotePartComplete(CompletionPart.Attributes);
            }
        }

        /// <summary>
        /// Returns a bag of applied custom attributes and data decoded from well-known attributes. Returns null if there are no attributes applied on the symbol.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        private CustomAttributesBag<CSharpAttributeData> GetSourceAttributesBag()
        {
            EnsureAttributesAreBound();
            return _lazySourceAttributesBag;
        }

        /// <summary>
        /// Gets the attributes applied on this symbol.
        /// Returns an empty array if there are no attributes.
        /// </summary>
        /// <remarks>
        /// NOTE: This method should always be kept as a sealed override.
        /// If you want to override attribute binding logic for a sub-class, then override <see cref="GetSourceAttributesBag"/> method.
        /// </remarks>
        public sealed override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            var attributes = this.GetSourceAttributesBag().Attributes;
            var netmoduleAttributes = this.GetNetModuleAttributesBag().Attributes;
            Debug.Assert(!attributes.IsDefault);
            Debug.Assert(!netmoduleAttributes.IsDefault);

            if (attributes.Length > 0)
            {
                if (netmoduleAttributes.Length > 0)
                {
                    attributes = attributes.Concat(netmoduleAttributes);
                }
            }
            else
            {
                attributes = netmoduleAttributes;
            }

            Debug.Assert(!attributes.IsDefault);
            return attributes;
        }

        /// <summary>
        /// Returns true if the assembly attribute at the given index is a duplicate assembly attribute that must not be emitted.
        /// Duplicate assembly attributes are attributes that bind to the same constructor and have identical arguments.
        /// </summary>
        /// <remarks>
        /// This method must be invoked only after all the assembly attributes have been bound.
        /// </remarks>
        internal bool IsIndexOfOmittedAssemblyAttribute(int index)
        {
            Debug.Assert(_lazyOmittedAttributeIndices == null || !_lazyOmittedAttributeIndices.Any(i => i < 0 || i >= this.GetAttributes().Length));
            Debug.Assert(_lazySourceAttributesBag.IsSealed);
            Debug.Assert(_lazyNetModuleAttributesBag.IsSealed);
            Debug.Assert(index >= 0);
            Debug.Assert(index < this.GetAttributes().Length);

            return _lazyOmittedAttributeIndices != null && _lazyOmittedAttributeIndices.Contains(index);
        }

        /// <summary>
        /// Returns data decoded from source assembly attributes or null if there are none.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// TODO: We should replace methods GetSourceDecodedWellKnownAttributeData and GetNetModuleDecodedWellKnownAttributeData with
        /// a single method GetDecodedWellKnownAttributeData, which merges DecodedWellKnownAttributeData from source and netmodule attributes.
        /// </remarks>
        internal CommonAssemblyWellKnownAttributeData GetSourceDecodedWellKnownAttributeData()
        {
            var attributesBag = _lazySourceAttributesBag;
            if (attributesBag == null || !attributesBag.IsDecodedWellKnownAttributeDataComputed)
            {
                attributesBag = this.GetSourceAttributesBag();
            }

            return (CommonAssemblyWellKnownAttributeData)attributesBag.DecodedWellKnownAttributeData;
        }

#nullable enable
        /// <remarks>
        /// This implements the same logic as <see cref="GetSourceDecodedWellKnownAttributeData()"/>
        /// but loading a smaller set of attributes if possible, to reduce circularity.
        /// </remarks>
        private CommonAssemblyWellKnownAttributeData? GetSourceDecodedWellKnownAttributeData(QuickAttributes attribute)
        {
            CustomAttributesBag<CSharpAttributeData>? attributesBag = _lazySourceAttributesBag;
            if (attributesBag?.IsDecodedWellKnownAttributeDataComputed == true)
            {
                return (CommonAssemblyWellKnownAttributeData)attributesBag.DecodedWellKnownAttributeData;
            }

            attributesBag = null;
            Func<AttributeSyntax, Binder?, bool> attributeMatches = attribute switch
            {
                QuickAttributes.AssemblySignatureKey => isPossibleAssemblySignatureKeyAttribute,
                QuickAttributes.AssemblyKeyName => isPossibleAssemblyKeyNameAttribute,
                QuickAttributes.AssemblyKeyFile => isPossibleAssemblyKeyFileAttribute,
                _ => throw ExceptionUtilities.UnexpectedValue(attribute)
            };

            LoadAndValidateAttributes(OneOrMany.Create(GetAttributeDeclarations()), ref attributesBag, attributeMatchesOpt: attributeMatches);

            return (CommonAssemblyWellKnownAttributeData?)attributesBag?.DecodedWellKnownAttributeData;

            bool isPossibleAssemblySignatureKeyAttribute(AttributeSyntax node, Binder? rootBinderOpt)
            {
                QuickAttributeChecker checker = this.DeclaringCompilation.GetBinderFactory(node.SyntaxTree).GetBinder(node).QuickAttributeChecker;
                return checker.IsPossibleMatch(node, QuickAttributes.AssemblySignatureKey);
            }

            bool isPossibleAssemblyKeyNameAttribute(AttributeSyntax node, Binder? rootBinderOpt)
            {
                QuickAttributeChecker checker = this.DeclaringCompilation.GetBinderFactory(node.SyntaxTree).GetBinder(node).QuickAttributeChecker;
                return checker.IsPossibleMatch(node, QuickAttributes.AssemblyKeyName);
            }

            bool isPossibleAssemblyKeyFileAttribute(AttributeSyntax node, Binder? rootBinderOpt)
            {
                QuickAttributeChecker checker = this.DeclaringCompilation.GetBinderFactory(node.SyntaxTree).GetBinder(node).QuickAttributeChecker;
                return checker.IsPossibleMatch(node, QuickAttributes.AssemblyKeyFile);
            }
        }
#nullable disable

        //Please don't use thread local storage widely. This is hoped to be the second one-off usage.
        [ThreadStatic]
        private static PooledHashSet<AttributeSyntax> t_forwardedTypesAttributesInProgress;

        /// <summary>
        /// This only forces binding of attributes that look like they may be forwarded types attributes (syntactically).
        /// </summary>
        internal HashSet<NamedTypeSymbol> GetForwardedTypes()
        {
            CustomAttributesBag<CSharpAttributeData> attributesBag = _lazySourceAttributesBag;
            if (attributesBag?.IsDecodedWellKnownAttributeDataComputed == true)
            {
                // Use already decoded attributes
                return ((CommonAssemblyWellKnownAttributeData)attributesBag.DecodedWellKnownAttributeData)?.ForwardedTypes;
            }

            var allocate = t_forwardedTypesAttributesInProgress is null;

            if (allocate)
            {
                t_forwardedTypesAttributesInProgress = PooledHashSet<AttributeSyntax>.GetInstance();
            }

            try
            {
                attributesBag = null;
                LoadAndValidateAttributes(
                    OneOrMany.Create(GetAttributeDeclarations()), ref attributesBag,
                    attributeMatchesOpt: this.IsPossibleForwardedTypesAttribute,
                    beforeAttributePartBound: BeforePossibleForwardedTypesAttributePartBound,
                    afterAttributePartBound: AfterPossibleForwardedTypesAttributePartBound);

                var wellKnownAttributeData = (CommonAssemblyWellKnownAttributeData)attributesBag?.DecodedWellKnownAttributeData;
                return wellKnownAttributeData?.ForwardedTypes;
            }
            finally
            {
                if (allocate)
                {
                    var tofree = t_forwardedTypesAttributesInProgress;
                    t_forwardedTypesAttributesInProgress = null;
                    tofree.Free();
                }
            }
        }

        private static void BeforePossibleForwardedTypesAttributePartBound(AttributeSyntax node)
        {
            bool added = t_forwardedTypesAttributesInProgress.Add(node);
            Debug.Assert(added);
        }

        private static void AfterPossibleForwardedTypesAttributePartBound(AttributeSyntax node)
        {
            bool removed = t_forwardedTypesAttributesInProgress.Remove(node);
            Debug.Assert(removed);
        }

        private bool IsPossibleForwardedTypesAttribute(AttributeSyntax node, Binder rootBinderOpt)
        {
            QuickAttributeChecker checker =
                this.DeclaringCompilation.GetBinderFactory(node.SyntaxTree).GetBinder(node).QuickAttributeChecker;

            if (checker.IsPossibleMatch(node, QuickAttributes.TypeForwardedTo))
            {
                if (!t_forwardedTypesAttributesInProgress.Contains(node))
                {
                    return true;
                }

                // It looks like we started binding this attribute and circled back to it again.
                // Let's skip it.
                ;
            }

            return false;
        }

        private static IEnumerable<Cci.SecurityAttribute> GetSecurityAttributes(CustomAttributesBag<CSharpAttributeData> attributesBag)
        {
            Debug.Assert(attributesBag.IsSealed);

            var wellKnownAttributeData = (CommonAssemblyWellKnownAttributeData)attributesBag.DecodedWellKnownAttributeData;
            if (wellKnownAttributeData != null)
            {
                SecurityWellKnownAttributeData securityData = wellKnownAttributeData.SecurityInformation;
                if (securityData != null)
                {
                    foreach (var securityAttribute in securityData.GetSecurityAttributes<CSharpAttributeData>(attributesBag.Attributes))
                    {
                        yield return securityAttribute;
                    }
                }
            }
        }

        internal IEnumerable<Cci.SecurityAttribute> GetSecurityAttributes()
        {
            // user defined security attributes:

            foreach (var securityAttribute in GetSecurityAttributes(this.GetSourceAttributesBag()))
            {
                yield return securityAttribute;
            }

            // Net module assembly security attributes:

            foreach (var securityAttribute in GetSecurityAttributes(this.GetNetModuleAttributesBag()))
            {
                yield return securityAttribute;
            }

            // synthesized security attributes:

            if (_compilation.Options.AllowUnsafe)
            {
                // NOTE: GlobalAttrBind::EmitCompilerGeneratedAttrs skips attribute if the well-known types aren't available.
                if (!(_compilation.GetWellKnownType(WellKnownType.System_Security_UnverifiableCodeAttribute) is MissingMetadataTypeSymbol) &&
                    !(_compilation.GetWellKnownType(WellKnownType.System_Security_Permissions_SecurityPermissionAttribute) is MissingMetadataTypeSymbol))
                {
                    var securityActionType = _compilation.GetWellKnownType(WellKnownType.System_Security_Permissions_SecurityAction);
                    if (!(securityActionType is MissingMetadataTypeSymbol))
                    {
                        var fieldRequestMinimum = (FieldSymbol)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Security_Permissions_SecurityAction__RequestMinimum);

                        // NOTE: Dev10 handles missing enum value.
                        object constantValue = (object)fieldRequestMinimum == null || fieldRequestMinimum.HasUseSiteError ? 0 : fieldRequestMinimum.ConstantValue;
                        var typedConstantRequestMinimum = new TypedConstant(securityActionType, TypedConstantKind.Enum, constantValue);

                        var boolType = _compilation.GetSpecialType(SpecialType.System_Boolean);
                        Debug.Assert(!boolType.HasUseSiteError,
                            "Use site errors should have been checked ahead of time (type bool).");

                        var typedConstantTrue = new TypedConstant(boolType, TypedConstantKind.Primitive, value: true);

                        var attribute = _compilation.TrySynthesizeAttribute(
                            WellKnownMember.System_Security_Permissions_SecurityPermissionAttribute__ctor,
                            ImmutableArray.Create(typedConstantRequestMinimum),
                            ImmutableArray.Create(new KeyValuePair<WellKnownMember, TypedConstant>(
                                WellKnownMember.System_Security_Permissions_SecurityPermissionAttribute__SkipVerification,
                                typedConstantTrue)));

                        if (attribute != null)
                        {
                            yield return new Cci.SecurityAttribute((DeclarativeSecurityAction)(int)constantValue, attribute);
                        }
                    }
                }
            }
        }

        internal override ImmutableArray<AssemblySymbol> GetNoPiaResolutionAssemblies()
        {
            return _modules[0].GetReferencedAssemblySymbols();
        }

        internal override void SetNoPiaResolutionAssemblies(ImmutableArray<AssemblySymbol> assemblies)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override ImmutableArray<AssemblySymbol> GetLinkedReferencedAssemblies()
        {
            // SourceAssemblySymbol is never used directly as a reference
            // when it is or any of its references is linked.
            return default(ImmutableArray<AssemblySymbol>);
        }

        internal override void SetLinkedReferencedAssemblies(ImmutableArray<AssemblySymbol> assemblies)
        {
            // SourceAssemblySymbol is never used directly as a reference
            // when it is or any of its references is linked.
            throw ExceptionUtilities.Unreachable();
        }

        internal override bool IsLinked
        {
            get
            {
                return false;
            }
        }

        internal bool DeclaresTheObjectClass
        {
            get
            {
                if ((object)this.CorLibrary != (object)this)
                {
                    return false;
                }

                var obj = GetSpecialType(SpecialType.System_Object);

                return !obj.IsErrorType() && obj.DeclaredAccessibility == Accessibility.Public;
            }
        }

        public override bool MightContainExtensionMethods
        {
            get
            {
                // Note this method returns true until all ContainsExtensionMethods is
                // called, after which the correct value will be returned. In other words,
                // the return value may change from true to false on subsequent calls.
                if (_lazyContainsExtensionMethods.HasValue())
                {
                    return _lazyContainsExtensionMethods.Value();
                }
                return true;
            }
        }

        private bool HasDebuggableAttribute
        {
            get
            {
                CommonAssemblyWellKnownAttributeData assemblyData = this.GetSourceDecodedWellKnownAttributeData();
                return assemblyData != null && assemblyData.HasDebuggableAttribute;
            }
        }

        private bool HasReferenceAssemblyAttribute
        {
            get
            {
                CommonAssemblyWellKnownAttributeData assemblyData = this.GetSourceDecodedWellKnownAttributeData();
                return assemblyData != null && assemblyData.HasReferenceAssemblyAttribute;
            }
        }

        internal override bool GetGuidString(out string guidString)
        {
            guidString = GetSourceDecodedWellKnownAttributeData()?.GuidAttribute;
            return guidString != null;
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            CSharpCompilationOptions options = _compilation.Options;
            bool isBuildingNetModule = options.OutputKind.IsNetModule();
            bool containsExtensionMethods = this.ContainsExtensionMethods();

            if (containsExtensionMethods)
            {
                // No need to check if [Extension] attribute was explicitly set since
                // we'll issue CS1112 error in those cases and won't generate IL.
                AddSynthesizedAttribute(ref attributes, _compilation.TrySynthesizeAttribute(
                    WellKnownMember.System_Runtime_CompilerServices_ExtensionAttribute__ctor));
            }

            // Synthesize CompilationRelaxationsAttribute only if all the following requirements are met:
            // (a) We are not building a netmodule.
            // (b) There is no applied CompilationRelaxationsAttribute assembly attribute in source.
            // (c) There is no applied CompilationRelaxationsAttribute assembly attribute for any of the added PE modules.
            // Above requirements also hold for synthesizing RuntimeCompatibilityAttribute attribute.

            bool emitCompilationRelaxationsAttribute = !isBuildingNetModule && !this.Modules.Any(static m => m.HasAssemblyCompilationRelaxationsAttribute);
            if (emitCompilationRelaxationsAttribute)
            {
                // Synthesize attribute: [CompilationRelaxationsAttribute(CompilationRelaxations.NoStringInterning)]

                // NOTE: GlobalAttrBind::EmitCompilerGeneratedAttrs skips attribute if the well-known types aren't available.
                if (!(_compilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_CompilationRelaxationsAttribute) is MissingMetadataTypeSymbol))
                {
                    var int32Type = _compilation.GetSpecialType(SpecialType.System_Int32);
                    Debug.Assert(!int32Type.HasUseSiteError,
                        "Use site errors should have been checked ahead of time (type int).");

                    var typedConstantNoStringInterning = new TypedConstant(int32Type, TypedConstantKind.Primitive, Cci.Constants.CompilationRelaxations_NoStringInterning);

                    AddSynthesizedAttribute(ref attributes, _compilation.TrySynthesizeAttribute(
                        WellKnownMember.System_Runtime_CompilerServices_CompilationRelaxationsAttribute__ctorInt32,
                        ImmutableArray.Create(typedConstantNoStringInterning)));
                }
            }

            bool emitRuntimeCompatibilityAttribute = !isBuildingNetModule && !this.Modules.Any(static m => m.HasAssemblyRuntimeCompatibilityAttribute);
            if (emitRuntimeCompatibilityAttribute)
            {
                // Synthesize attribute: [RuntimeCompatibilityAttribute(WrapNonExceptionThrows = true)]

                // NOTE: GlobalAttrBind::EmitCompilerGeneratedAttrs skips attribute if the well-known types aren't available.
                if (!(_compilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_RuntimeCompatibilityAttribute) is MissingMetadataTypeSymbol))
                {
                    var boolType = _compilation.GetSpecialType(SpecialType.System_Boolean);
                    Debug.Assert(!boolType.HasUseSiteError, "Use site errors should have been checked ahead of time (type bool).");

                    var typedConstantTrue = new TypedConstant(boolType, TypedConstantKind.Primitive, value: true);

                    AddSynthesizedAttribute(ref attributes, _compilation.TrySynthesizeAttribute(
                        WellKnownMember.System_Runtime_CompilerServices_RuntimeCompatibilityAttribute__ctor,
                        ImmutableArray<TypedConstant>.Empty,
                        ImmutableArray.Create(new KeyValuePair<WellKnownMember, TypedConstant>(
                            WellKnownMember.System_Runtime_CompilerServices_RuntimeCompatibilityAttribute__WrapNonExceptionThrows,
                            typedConstantTrue))));
                }
            }

            // Synthesize DebuggableAttribute only if all the following requirements are met:
            // (a) We are not building a netmodule.
            // (b) We are emitting debug information (full or pdbonly).
            // (c) There is no applied DebuggableAttribute assembly attribute in source.

            // CONSIDER: Native VB compiler and Roslyn VB compiler also have an additional requirement: There is no applied DebuggableAttribute *module* attribute in source.
            // CONSIDER: Should we check for module DebuggableAttribute?
            if (!isBuildingNetModule && !this.HasDebuggableAttribute)
            {
                AddSynthesizedAttribute(ref attributes, _compilation.SynthesizeDebuggableAttribute());
            }

            if (_compilation.Options.OutputKind == OutputKind.NetModule)
            {
                // If the attribute is applied in source, do not add synthetic one.
                // If its value is different from the supplied through options, an error should have been reported by now.

                if (!string.IsNullOrEmpty(_compilation.Options.CryptoKeyContainer) &&
                    (object)AssemblyKeyContainerAttributeSetting == (object)CommonAssemblyWellKnownAttributeData.StringMissingValue)
                {
                    var stringType = _compilation.GetSpecialType(SpecialType.System_String);
                    Debug.Assert(!stringType.HasUseSiteError, "Use site errors should have been checked ahead of time (type string).");

                    var typedConstant = new TypedConstant(stringType, TypedConstantKind.Primitive, _compilation.Options.CryptoKeyContainer);
                    AddSynthesizedAttribute(ref attributes, _compilation.TrySynthesizeAttribute(WellKnownMember.System_Reflection_AssemblyKeyNameAttribute__ctor, ImmutableArray.Create(typedConstant)));
                }

                if (!String.IsNullOrEmpty(_compilation.Options.CryptoKeyFile) &&
                    (object)AssemblyKeyFileAttributeSetting == (object)CommonAssemblyWellKnownAttributeData.StringMissingValue)
                {
                    var stringType = _compilation.GetSpecialType(SpecialType.System_String);
                    Debug.Assert(!stringType.HasUseSiteError, "Use site errors should have been checked ahead of time (type string).");

                    var typedConstant = new TypedConstant(stringType, TypedConstantKind.Primitive, _compilation.Options.CryptoKeyFile);
                    AddSynthesizedAttribute(ref attributes, _compilation.TrySynthesizeAttribute(WellKnownMember.System_Reflection_AssemblyKeyFileAttribute__ctor, ImmutableArray.Create(typedConstant)));
                }
            }
        }

        /// <summary>
        /// Returns true if and only if at least one type within the assembly contains
        /// extension methods. Note, this method is expensive since it potentially
        /// inspects all types within the assembly. The expectation is that this method is
        /// only called at emit time, when all types have been or will be traversed anyway.
        /// </summary>
        private bool ContainsExtensionMethods()
        {
            if (!_lazyContainsExtensionMethods.HasValue())
            {
                _lazyContainsExtensionMethods = ContainsExtensionMethods(_modules).ToThreeState();
            }

            return _lazyContainsExtensionMethods.Value();
        }

        private static bool ContainsExtensionMethods(ImmutableArray<ModuleSymbol> modules)
        {
            foreach (var module in modules)
            {
                if (ContainsExtensionMethods(module.GlobalNamespace))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool ContainsExtensionMethods(NamespaceSymbol ns)
        {
            foreach (var member in ns.GetMembersUnordered())
            {
                switch (member.Kind)
                {
                    case SymbolKind.Namespace:
                        if (ContainsExtensionMethods((NamespaceSymbol)member))
                        {
                            return true;
                        }
                        break;
                    case SymbolKind.NamedType:
                        if (((NamedTypeSymbol)member).MightContainExtensionMethods)
                        {
                            return true;
                        }
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(member.Kind);
                }
            }
            return false;
        }

        //Once the computation of the AssemblyIdentity is complete, check whether
        //any of the IVT access grants that were optimistically made during AssemblyIdentity computation
        //are in fact invalid now that the full identity is known.
        private void CheckOptimisticIVTAccessGrants(BindingDiagnosticBag bag)
        {
            ConcurrentDictionary<AssemblySymbol, bool> haveGrantedAssemblies = _optimisticallyGrantedInternalsAccess;

            if (haveGrantedAssemblies != null)
            {
                foreach (var otherAssembly in haveGrantedAssemblies.Keys)
                {
                    IVTConclusion conclusion = MakeFinalIVTDetermination(otherAssembly, assertUnexpectedGiver: true);

                    Debug.Assert(conclusion != IVTConclusion.NoRelationshipClaimed);

                    if (conclusion == IVTConclusion.PublicKeyDoesntMatch)
                        bag.Add(ErrorCode.ERR_FriendRefNotEqualToThis, NoLocation.Singleton,
                                                                      otherAssembly.Identity, this.Identity);
                    else if (conclusion == IVTConclusion.OneSignedOneNot)
                        bag.Add(ErrorCode.ERR_FriendRefSigningMismatch, NoLocation.Singleton,
                                                                      otherAssembly.Identity);
                }
            }
        }

        internal override IEnumerable<ImmutableArray<byte>> GetInternalsVisibleToPublicKeys(string simpleName)
        {
            //EDMAURER assume that if EnsureAttributesAreBound() returns, then the internals visible to map has been populated.
            //Do not optimize by checking if m_lazyInternalsVisibleToMap is Nothing. It may be non-null yet still
            //incomplete because another thread is in the process of building it.

            EnsureAttributesAreBound();

            if (_lazyInternalsVisibleToMap == null)
                return SpecializedCollections.EmptyEnumerable<ImmutableArray<byte>>();

            ConcurrentDictionary<ImmutableArray<byte>, Tuple<Location, string>> result = null;

            _lazyInternalsVisibleToMap.TryGetValue(simpleName, out result);

            return (result != null) ? result.Keys : SpecializedCollections.EmptyEnumerable<ImmutableArray<byte>>();
        }

        internal override IEnumerable<string> GetInternalsVisibleToAssemblyNames()
        {
            EnsureAttributesAreBound();

            if (_lazyInternalsVisibleToMap == null)
                return SpecializedCollections.EmptyEnumerable<string>();

            return _lazyInternalsVisibleToMap.Keys;
        }

        internal override bool AreInternalsVisibleToThisAssembly(AssemblySymbol potentialGiverOfAccess)
        {
            // Ensure that optimistic IVT access is only granted to requests that originated on the thread
            //that is trying to compute the assembly identity. This gives us deterministic behavior when
            //two threads are checking IVT access but only one of them is in the process of computing identity.

            //as an optimization confirm that the identity has not yet been computed to avoid testing TLS
            if (_lazyStrongNameKeys == null)
            {
                var assemblyWhoseKeysAreBeingComputed = t_assemblyForWhichCurrentThreadIsComputingKeys;
                if ((object)assemblyWhoseKeysAreBeingComputed != null)
                {
                    //ThrowIfFalse(assemblyWhoseKeysAreBeingComputed Is Me);
                    if (!potentialGiverOfAccess.GetInternalsVisibleToPublicKeys(this.Name).IsEmpty())
                    {
                        if (_optimisticallyGrantedInternalsAccess == null)
                            Interlocked.CompareExchange(ref _optimisticallyGrantedInternalsAccess, new ConcurrentDictionary<AssemblySymbol, bool>(), null);

                        _optimisticallyGrantedInternalsAccess.TryAdd(potentialGiverOfAccess, true);
                        return true;
                    }
                    else
                        return false;
                }
            }

            IVTConclusion conclusion = MakeFinalIVTDetermination(potentialGiverOfAccess, assertUnexpectedGiver: true);

            return conclusion == IVTConclusion.Match || conclusion == IVTConclusion.OneSignedOneNot;
        }

        private AssemblyIdentity ComputeIdentity()
        {
            return new AssemblyIdentity(
                _assemblySimpleName,
                VersionHelper.GenerateVersionFromPatternAndCurrentTime(_compilation.Options.CurrentLocalTime, AssemblyVersionAttributeSetting),
                this.AssemblyCultureAttributeSetting,
                StrongNameKeys.PublicKey,
                hasPublicKey: !StrongNameKeys.PublicKey.IsDefault,
                isRetargetable: (AssemblyFlags & AssemblyFlags.Retargetable) == AssemblyFlags.Retargetable);
        }

        //This maps from assembly name to a set of public keys. It uses concurrent dictionaries because it is built,
        //one attribute at a time, in the callback that validates an attribute's application to a symbol. It is assumed
        //to be complete after a call to GetAttributes(). The second dictionary is acting like a set. The value element is
        //only used when the key is empty in which case it stores the location and value of the attribute string which
        //may be used to construct a diagnostic if the assembly being compiled is found to be strong named.
        private ConcurrentDictionary<string, ConcurrentDictionary<ImmutableArray<byte>, Tuple<Location, string>>> _lazyInternalsVisibleToMap;

        private static Location GetAssemblyAttributeLocationForDiagnostic(AttributeSyntax attributeSyntaxOpt)
        {
            return (object)attributeSyntaxOpt != null ? attributeSyntaxOpt.Location : NoLocation.Singleton;
        }

        private void DecodeTypeForwardedToAttribute(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            // this code won't be called unless we bound a well-formed, semantically correct ctor call.
            Debug.Assert(!arguments.Attribute.HasErrors);
            var diagnostics = (BindingDiagnosticBag)arguments.Diagnostics;

            TypeSymbol forwardedType = (TypeSymbol)arguments.Attribute.CommonConstructorArguments[0].ValueInternal;

            // This can happen if the argument is the null literal.
            if ((object)forwardedType == null)
            {
                diagnostics.Add(ErrorCode.ERR_InvalidFwdType, GetAssemblyAttributeLocationForDiagnostic(arguments.AttributeSyntaxOpt));
                return;
            }

            UseSiteInfo<AssemblySymbol> useSiteInfo = forwardedType.GetUseSiteInfo();
            if (useSiteInfo.DiagnosticInfo?.Code != (int)ErrorCode.ERR_UnexpectedUnboundGenericName &&
                diagnostics.Add(useSiteInfo, useSiteInfo.DiagnosticInfo is object ? GetAssemblyAttributeLocationForDiagnostic(arguments.AttributeSyntaxOpt) : Location.None))
            {
                return;
            }

            Debug.Assert(forwardedType.TypeKind != TypeKind.Error);

            if (forwardedType.ContainingAssembly == this)
            {
                diagnostics.Add(ErrorCode.ERR_ForwardedTypeInThisAssembly, GetAssemblyAttributeLocationForDiagnostic(arguments.AttributeSyntaxOpt), forwardedType);
                return;
            }

            if ((object)forwardedType.ContainingType != null)
            {
                diagnostics.Add(ErrorCode.ERR_ForwardedTypeIsNested, GetAssemblyAttributeLocationForDiagnostic(arguments.AttributeSyntaxOpt), forwardedType, forwardedType.ContainingType);
                return;
            }

            if (forwardedType.Kind != SymbolKind.NamedType)
            {
                // NOTE: Dev10 actually tests whether the forwarded type is an aggregate.  This would seem to
                // exclude nullable and void, but that shouldn't be an issue because they have to be defined in
                // corlib (since they are special types) and corlib can't refer to other assemblies (by definition).

                diagnostics.Add(ErrorCode.ERR_InvalidFwdType, GetAssemblyAttributeLocationForDiagnostic(arguments.AttributeSyntaxOpt));
                return;
            }

            // NOTE: There is no danger of the type being forwarded back to this assembly, because the type
            // won't even bind successfully unless we have a reference to an assembly that actually contains
            // the type.

            var assemblyData = arguments.GetOrCreateData<CommonAssemblyWellKnownAttributeData>();
            HashSet<NamedTypeSymbol> forwardedTypes = assemblyData.ForwardedTypes;
            if (forwardedTypes == null)
            {
                forwardedTypes = new HashSet<NamedTypeSymbol>() { (NamedTypeSymbol)forwardedType };
                assemblyData.ForwardedTypes = forwardedTypes;
            }
            else if (!forwardedTypes.Add((NamedTypeSymbol)forwardedType))
            {
                // NOTE: For the purposes of reporting this error, Dev10 considers C<int> and C<char>
                // different types.  However, it will actually emit a single forwarder for C`1 (i.e.
                // we'll have to de-dup again at emit time).
                diagnostics.Add(ErrorCode.ERR_DuplicateTypeForwarder, GetAssemblyAttributeLocationForDiagnostic(arguments.AttributeSyntaxOpt), forwardedType);
            }
        }

        private void DecodeOneInternalsVisibleToAttribute(
            AttributeSyntax nodeOpt,
            CSharpAttributeData attrData,
            BindingDiagnosticBag diagnostics,
            int index,
            ref ConcurrentDictionary<string, ConcurrentDictionary<ImmutableArray<byte>, Tuple<Location, string>>> lazyInternalsVisibleToMap)
        {
            // this code won't be called unless we bound a well-formed, semantically correct ctor call.
            Debug.Assert(!attrData.HasErrors);

            string displayName = (string)attrData.CommonConstructorArguments[0].ValueInternal;

            if (displayName == null)
            {
                diagnostics.Add(ErrorCode.ERR_CannotPassNullForFriendAssembly, GetAssemblyAttributeLocationForDiagnostic(nodeOpt));
                return;
            }

            AssemblyIdentity identity;
            AssemblyIdentityParts parts;
            if (!AssemblyIdentity.TryParseDisplayName(displayName, out identity, out parts))
            {
                diagnostics.Add(ErrorCode.WRN_InvalidAssemblyName, GetAssemblyAttributeLocationForDiagnostic(nodeOpt), displayName);
                AddOmittedAttributeIndex(index);
                return;
            }

            // Allow public key token due to compatibility reasons, but we are not going to use its value.
            const AssemblyIdentityParts allowedParts = AssemblyIdentityParts.Name | AssemblyIdentityParts.PublicKey | AssemblyIdentityParts.PublicKeyToken;

            if ((parts & ~allowedParts) != 0)
            {
                diagnostics.Add(ErrorCode.ERR_FriendAssemblyBadArgs, GetAssemblyAttributeLocationForDiagnostic(nodeOpt), displayName);
                return;
            }

            if (lazyInternalsVisibleToMap == null)
            {
                Interlocked.CompareExchange(ref lazyInternalsVisibleToMap,
                                            new ConcurrentDictionary<string, ConcurrentDictionary<ImmutableArray<byte>, Tuple<Location, String>>>(StringComparer.OrdinalIgnoreCase), null);
            }

            //later, once the identity is established we confirm that if the assembly being 
            //compiled is signed all of the IVT attributes specify a key. Stash the location for that
            //in the event that a diagnostic needs to be produced.

            Tuple<Location, string> locationAndValue = null;

            // only need to store anything when there is no public key. The only reason to store
            // this stuff is for production of errors when the assembly is signed but the IVT attrib
            // doesn't contain a public key.
            if (identity.PublicKey.IsEmpty)
            {
                locationAndValue = new Tuple<Location, string>(GetAssemblyAttributeLocationForDiagnostic(nodeOpt), displayName);
            }

            //when two threads are attempting to update the internalsVisibleToMap one of these TryAdd()
            //calls can fail. We assume that the 'other' thread in that case will successfully add the same
            //contents eventually.
            ConcurrentDictionary<ImmutableArray<byte>, Tuple<Location, string>> keys = null;
            if (lazyInternalsVisibleToMap.TryGetValue(identity.Name, out keys))
            {
                keys.TryAdd(identity.PublicKey, locationAndValue);
            }
            else
            {
                keys = new ConcurrentDictionary<ImmutableArray<byte>, Tuple<Location, String>>();
                keys.TryAdd(identity.PublicKey, locationAndValue);
                lazyInternalsVisibleToMap.TryAdd(identity.Name, keys);
            }
        }

        IAttributeTargetSymbol IAttributeTargetSymbol.AttributesOwner
        {
            get { return this; }
        }

        AttributeLocation IAttributeTargetSymbol.DefaultAttributeLocation
        {
            get { return AttributeLocation.Assembly; }
        }

        AttributeLocation IAttributeTargetSymbol.AllowedAttributeLocations
        {
            get
            {
                return IsInteractive ? AttributeLocation.None : AttributeLocation.Assembly | AttributeLocation.Module;
            }
        }

        protected override void DecodeWellKnownAttributeImpl(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            DecodeWellKnownAttribute(ref arguments, arguments.Index, isFromNetModule: false);
        }

        private void DecodeWellKnownAttribute(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments, int index, bool isFromNetModule)
        {
            var attribute = arguments.Attribute;
            Debug.Assert(!attribute.HasErrors);
            Debug.Assert(arguments.SymbolPart == AttributeLocation.None);
            int signature;
            var diagnostics = (BindingDiagnosticBag)arguments.Diagnostics;

            if (attribute is SourceAttributeData
                && ReportExplicitUseOfReservedAttributes(in arguments,
                    permitted: ReservedAttributes.DynamicAttribute
                        | ReservedAttributes.IsReadOnlyAttribute
                        | ReservedAttributes.IsUnmanagedAttribute
                        | ReservedAttributes.IsByRefLikeAttribute
                        | ReservedAttributes.TupleElementNamesAttribute
                        | ReservedAttributes.NullableAttribute
                        | ReservedAttributes.NullableContextAttribute
                        | ReservedAttributes.NullablePublicOnlyAttribute
                        | ReservedAttributes.NativeIntegerAttribute
                        | ReservedAttributes.CaseSensitiveExtensionAttribute
                        | ReservedAttributes.RequiredMemberAttribute
                        | ReservedAttributes.ScopedRefAttribute
                        | ReservedAttributes.RefSafetyRulesAttribute
                        | ReservedAttributes.RequiresLocationAttribute
                        | ReservedAttributes.ExtensionMarkerAttribute))
            {
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.InternalsVisibleToAttribute))
            {
                DecodeOneInternalsVisibleToAttribute(arguments.AttributeSyntaxOpt, attribute, diagnostics, index, ref _lazyInternalsVisibleToMap);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.AssemblySignatureKeyAttribute))
            {
                var signatureKey = (string)attribute.CommonConstructorArguments[0].ValueInternal;
                arguments.GetOrCreateData<CommonAssemblyWellKnownAttributeData>().AssemblySignatureKeyAttributeSetting = signatureKey;

                if (!StrongNameKeys.IsValidPublicKeyString(signatureKey))
                {
                    diagnostics.Add(ErrorCode.ERR_InvalidSignaturePublicKey, attribute.GetAttributeArgumentLocation(0));
                }
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.AssemblyKeyFileAttribute))
            {
                arguments.GetOrCreateData<CommonAssemblyWellKnownAttributeData>().AssemblyKeyFileAttributeSetting = (string)attribute.CommonConstructorArguments[0].ValueInternal;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.AssemblyKeyNameAttribute))
            {
                arguments.GetOrCreateData<CommonAssemblyWellKnownAttributeData>().AssemblyKeyContainerAttributeSetting = (string)attribute.CommonConstructorArguments[0].ValueInternal;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.AssemblyDelaySignAttribute))
            {
                arguments.GetOrCreateData<CommonAssemblyWellKnownAttributeData>().AssemblyDelaySignAttributeSetting = (bool)attribute.CommonConstructorArguments[0].ValueInternal ? ThreeState.True : ThreeState.False;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.AssemblyVersionAttribute))
            {
                string verString = (string)attribute.CommonConstructorArguments[0].ValueInternal;
                Version version;
                if (!VersionHelper.TryParseAssemblyVersion(verString, allowWildcard: !_compilation.IsEmitDeterministic, version: out version))
                {
                    Location attributeArgumentSyntaxLocation = attribute.GetAttributeArgumentLocation(0);
                    bool foundBadWildcard = _compilation.IsEmitDeterministic && verString?.Contains('*') == true;
                    diagnostics.Add(foundBadWildcard ? ErrorCode.ERR_InvalidVersionFormatDeterministic : ErrorCode.ERR_InvalidVersionFormat, attributeArgumentSyntaxLocation, verString ?? "<null>");
                }

                arguments.GetOrCreateData<CommonAssemblyWellKnownAttributeData>().AssemblyVersionAttributeSetting = version;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.AssemblyFileVersionAttribute))
            {
                Version dummy;
                string verString = (string)attribute.CommonConstructorArguments[0].ValueInternal;
                if (!VersionHelper.TryParse(verString, version: out dummy))
                {
                    Location attributeArgumentSyntaxLocation = attribute.GetAttributeArgumentLocation(0);
                    diagnostics.Add(ErrorCode.WRN_InvalidVersionFormat, attributeArgumentSyntaxLocation, verString ?? "<null>");
                }

                arguments.GetOrCreateData<CommonAssemblyWellKnownAttributeData>().AssemblyFileVersionAttributeSetting = verString;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.AssemblyTitleAttribute))
            {
                arguments.GetOrCreateData<CommonAssemblyWellKnownAttributeData>().AssemblyTitleAttributeSetting = (string)attribute.CommonConstructorArguments[0].ValueInternal;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.AssemblyDescriptionAttribute))
            {
                arguments.GetOrCreateData<CommonAssemblyWellKnownAttributeData>().AssemblyDescriptionAttributeSetting = (string)attribute.CommonConstructorArguments[0].ValueInternal;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.AssemblyCultureAttribute))
            {
                var cultureString = (string)attribute.CommonConstructorArguments[0].ValueInternal;
                if (!string.IsNullOrEmpty(cultureString))
                {
                    if (_compilation.Options.OutputKind.IsApplication())
                    {
                        diagnostics.Add(ErrorCode.ERR_InvalidAssemblyCultureForExe, attribute.GetAttributeArgumentLocation(0));
                    }
                    else if (!AssemblyIdentity.IsValidCultureName(cultureString))
                    {
                        diagnostics.Add(ErrorCode.ERR_InvalidAssemblyCulture, attribute.GetAttributeArgumentLocation(0));
                        cultureString = null;
                    }
                }

                arguments.GetOrCreateData<CommonAssemblyWellKnownAttributeData>().AssemblyCultureAttributeSetting = cultureString;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.AssemblyCompanyAttribute))
            {
                arguments.GetOrCreateData<CommonAssemblyWellKnownAttributeData>().AssemblyCompanyAttributeSetting = (string)attribute.CommonConstructorArguments[0].ValueInternal;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.AssemblyProductAttribute))
            {
                arguments.GetOrCreateData<CommonAssemblyWellKnownAttributeData>().AssemblyProductAttributeSetting = (string)attribute.CommonConstructorArguments[0].ValueInternal;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.AssemblyInformationalVersionAttribute))
            {
                arguments.GetOrCreateData<CommonAssemblyWellKnownAttributeData>().AssemblyInformationalVersionAttributeSetting = (string)attribute.CommonConstructorArguments[0].ValueInternal;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.SatelliteContractVersionAttribute))
            {
                //just check the format of this one, don't do anything else with it.
                Version dummy;
                string verString = (string)attribute.CommonConstructorArguments[0].ValueInternal;

                if (!VersionHelper.TryParseAssemblyVersion(verString, allowWildcard: false, version: out dummy))
                {
                    diagnostics.Add(ErrorCode.ERR_InvalidVersionFormat2, attribute.GetAttributeArgumentLocation(0), verString ?? "<null>");
                }
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.AssemblyCopyrightAttribute))
            {
                arguments.GetOrCreateData<CommonAssemblyWellKnownAttributeData>().AssemblyCopyrightAttributeSetting = (string)attribute.CommonConstructorArguments[0].ValueInternal;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.AssemblyTrademarkAttribute))
            {
                arguments.GetOrCreateData<CommonAssemblyWellKnownAttributeData>().AssemblyTrademarkAttributeSetting = (string)attribute.CommonConstructorArguments[0].ValueInternal;
            }
            else if ((signature = attribute.GetTargetAttributeSignatureIndex(AttributeDescription.AssemblyFlagsAttribute)) != -1)
            {
                object value = attribute.CommonConstructorArguments[0].ValueInternal;
                AssemblyFlags nameFlags;

                if (signature == 0 || signature == 1)
                {
                    nameFlags = (AssemblyFlags)(AssemblyNameFlags)value;
                }
                else
                {
                    nameFlags = (AssemblyFlags)(uint)value;
                }

                arguments.GetOrCreateData<CommonAssemblyWellKnownAttributeData>().AssemblyFlagsAttributeSetting = nameFlags;
            }
            else if (attribute.IsSecurityAttribute(_compilation))
            {
                attribute.DecodeSecurityAttribute<CommonAssemblyWellKnownAttributeData>(this, _compilation, ref arguments);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.ClassInterfaceAttribute))
            {
                attribute.DecodeClassInterfaceAttribute(arguments.AttributeSyntaxOpt, diagnostics);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.TypeLibVersionAttribute))
            {
                ValidateIntegralAttributeNonNegativeArguments(attribute, arguments.AttributeSyntaxOpt, diagnostics);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.ComCompatibleVersionAttribute))
            {
                ValidateIntegralAttributeNonNegativeArguments(attribute, arguments.AttributeSyntaxOpt, diagnostics);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.GuidAttribute))
            {
                string guidString = attribute.DecodeGuidAttribute(arguments.AttributeSyntaxOpt, diagnostics);
                arguments.GetOrCreateData<CommonAssemblyWellKnownAttributeData>().GuidAttribute = guidString;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.ImportedFromTypeLibAttribute))
            {
                if (attribute.CommonConstructorArguments.Length == 1)
                {
                    arguments.GetOrCreateData<CommonAssemblyWellKnownAttributeData>().HasImportedFromTypeLibAttribute = true;
                }
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.PrimaryInteropAssemblyAttribute))
            {
                if (attribute.CommonConstructorArguments.Length == 2)
                {
                    arguments.GetOrCreateData<CommonAssemblyWellKnownAttributeData>().HasPrimaryInteropAssemblyAttribute = true;
                }
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.CompilationRelaxationsAttribute))
            {
                arguments.GetOrCreateData<CommonAssemblyWellKnownAttributeData>().HasCompilationRelaxationsAttribute = true;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.ReferenceAssemblyAttribute))
            {
                arguments.GetOrCreateData<CommonAssemblyWellKnownAttributeData>().HasReferenceAssemblyAttribute = true;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.RuntimeCompatibilityAttribute))
            {
                bool wrapNonExceptionThrows = true;

                foreach (var namedArg in attribute.CommonNamedArguments)
                {
                    switch (namedArg.Key)
                    {
                        case "WrapNonExceptionThrows":
                            wrapNonExceptionThrows = namedArg.Value.DecodeValue<bool>(SpecialType.System_Boolean);
                            break;
                    }
                }

                arguments.GetOrCreateData<CommonAssemblyWellKnownAttributeData>().RuntimeCompatibilityWrapNonExceptionThrows = wrapNonExceptionThrows;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.DebuggableAttribute))
            {
                arguments.GetOrCreateData<CommonAssemblyWellKnownAttributeData>().HasDebuggableAttribute = true;
            }
            else if (!isFromNetModule && attribute.IsTargetAttribute(AttributeDescription.TypeForwardedToAttribute))
            {
                DecodeTypeForwardedToAttribute(ref arguments);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.CaseSensitiveExtensionAttribute))
            {
                if ((object)arguments.AttributeSyntaxOpt != null)
                {
                    // [Extension] attribute should not be set explicitly.
                    diagnostics.Add(ErrorCode.ERR_ExplicitExtension, arguments.AttributeSyntaxOpt.Location);
                }
            }
            else if ((signature = attribute.GetTargetAttributeSignatureIndex(AttributeDescription.AssemblyAlgorithmIdAttribute)) != -1)
            {
                object value = attribute.CommonConstructorArguments[0].ValueInternal;
                AssemblyHashAlgorithm algorithmId;

                if (signature == 0)
                {
                    algorithmId = (AssemblyHashAlgorithm)value;
                }
                else
                {
                    algorithmId = (AssemblyHashAlgorithm)(uint)value;
                }

                arguments.GetOrCreateData<CommonAssemblyWellKnownAttributeData>().AssemblyAlgorithmIdAttributeSetting = algorithmId;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.ExperimentalAttribute))
            {
                var obsoleteData = attribute.DecodeExperimentalAttribute();
                arguments.GetOrCreateData<CommonAssemblyWellKnownAttributeData>().ExperimentalAttributeData = obsoleteData;
            }
        }

        // Checks that the integral arguments for the given well-known attribute are non-negative.
        private static void ValidateIntegralAttributeNonNegativeArguments(CSharpAttributeData attribute, AttributeSyntax nodeOpt, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(!attribute.HasErrors);

            int argCount = attribute.CommonConstructorArguments.Length;
            for (int i = 0; i < argCount; i++)
            {
                int arg = attribute.GetConstructorArgument<int>(i, SpecialType.System_Int32);
                if (arg < 0)
                {
                    // CS0591: Invalid value for argument to '{0}' attribute
                    diagnostics.Add(ErrorCode.ERR_InvalidAttributeArgument, attribute.GetAttributeArgumentLocation(i), (object)nodeOpt != null ? nodeOpt.GetErrorDisplayName() : "");
                }
            }
        }

        internal void NoteFieldAccess(FieldSymbol field, bool read, bool write)
        {
            var container = field.ContainingType as SourceMemberContainerTypeSymbol;
            if ((object)container == null)
            {
                // field is not in source.
                return;
            }

            container.EnsureFieldDefinitionsNoted();

            if (_unusedFieldWarnings.IsDefault)
            {
                if (read)
                {
                    _unreadFields.Remove(field);
                }

                if (write)
                {
                    bool _;
                    _unassignedFieldsMap.TryRemove(field, out _);
                }
            }
            else
            {
                // It's acceptable to run flow analysis again after the diagnostics have been computed - just
                // make sure that the nothing is different than the first time.

                Debug.Assert(
                     !(read && _unreadFields.Remove(field)),
                     "we are already reporting unused field warnings, there could be no more changes");

                Debug.Assert(
                    !(write && _unassignedFieldsMap.ContainsKey(field)),
                    "we are already reporting unused field warnings, there could be no more changes");
            }
        }

        internal void NoteFieldDefinition(FieldSymbol field, bool isInternal, bool isUnread)
        {
            Debug.Assert(_unusedFieldWarnings.IsDefault, "We shouldn't have computed the diagnostics if we're still noting definitions.");

            _unassignedFieldsMap.TryAdd(field, isInternal);
            if (isUnread)
            {
                _unreadFields.Add(field);
            }
        }

        internal override bool IsNetModule() => this._compilation.Options.OutputKind.IsNetModule();

        /// <summary>
        /// Get the warnings for unused fields.  This should only be fetched when all method bodies have been compiled.
        /// </summary>
        internal ImmutableArray<Diagnostic> GetUnusedFieldWarnings(CancellationToken cancellationToken)
        {
            if (_unusedFieldWarnings.IsDefault)
            {
                //Our maps of unread and unassigned fields won't be done until the assembly is complete.
                this.ForceComplete(locationOpt: null, filter: null, cancellationToken: cancellationToken);

                Debug.Assert(this.HasComplete(CompletionPart.Module),
                    "Don't consume unused field information if there are still types to be processed.");

                // Build this up in a local before we assign it to this.unusedFieldWarnings (so other threads
                // can see that it's not done).
                DiagnosticBag diagnostics = DiagnosticBag.GetInstance();

                // NOTE: two threads can come in here at the same time.  If they do, then they will
                // share the diagnostic bag.  That's alright, as long as each one processes only
                // the fields that it successfully removes from the shared map/set.  Furthermore,
                // there should be no problem with re-calling this method on the same assembly,
                // since there will be nothing left in the map/set the second time.
                bool internalsAreVisible =
                    this.InternalsAreVisible ||
                    this.IsNetModule();

                HashSet<FieldSymbol> handledUnreadFields = null;

                foreach (FieldSymbol field in _unassignedFieldsMap.Keys) // Not mutating, so no snapshot required.
                {
                    bool isInternalAccessibility;
                    bool success = _unassignedFieldsMap.TryGetValue(field, out isInternalAccessibility);
                    Debug.Assert(success, "Once CompletionPart.Module is set, no-one should be modifying the map.");

                    if (isInternalAccessibility && internalsAreVisible)
                    {
                        continue;
                    }

                    if (!field.CanBeReferencedByName)
                    {
                        continue;
                    }

                    var containingType = field.ContainingType as SourceNamedTypeSymbol;
                    if ((object)containingType == null)
                    {
                        continue;
                    }

                    if (field is TupleErrorFieldSymbol)
                    {
                        continue;
                    }

                    bool unread = _unreadFields.Contains(field);
                    if (unread)
                    {
                        if (handledUnreadFields == null)
                        {
                            handledUnreadFields = new HashSet<FieldSymbol>();
                        }
                        handledUnreadFields.Add(field);
                    }

                    if (containingType.HasStructLayoutAttribute || containingType.HasExtendedLayoutAttribute || containingType.HasInlineArrayAttribute(out _))
                    {
                        continue;
                    }

                    Symbol associatedPropertyOrEvent = field.AssociatedSymbol;
                    if ((object)associatedPropertyOrEvent != null && associatedPropertyOrEvent.Kind == SymbolKind.Event)
                    {
                        if (unread)
                        {
                            diagnostics.Add(ErrorCode.WRN_UnreferencedEvent, associatedPropertyOrEvent.GetFirstLocationOrNone(), associatedPropertyOrEvent);
                        }
                    }
                    else if (unread)
                    {
                        diagnostics.Add(ErrorCode.WRN_UnreferencedField, field.GetFirstLocationOrNone(), field);
                    }
                    else if (field.RefKind != RefKind.None)
                    {
                        diagnostics.Add(ErrorCode.WRN_UnassignedInternalRefField, field.GetFirstLocationOrNone(), field);
                    }
                    else
                    {
                        diagnostics.Add(ErrorCode.WRN_UnassignedInternalField, field.GetFirstLocationOrNone(), field, DefaultValue(field.Type));
                    }
                }

                foreach (FieldSymbol field in _unreadFields) // Not mutating, so no snapshot required.
                {
                    if (handledUnreadFields != null && handledUnreadFields.Contains(field))
                    {
                        // Handled in the first foreach loop.
                        continue;
                    }

                    if (!field.CanBeReferencedByName)
                    {
                        continue;
                    }

                    var containingType = field.ContainingType as SourceNamedTypeSymbol;
                    if ((object)containingType != null && !containingType.HasStructLayoutAttribute && !containingType.HasExtendedLayoutAttribute && !containingType.HasInlineArrayAttribute(out _))
                    {
                        diagnostics.Add(ErrorCode.WRN_UnreferencedFieldAssg, field.GetFirstLocationOrNone(), field);
                    }
                }

                ImmutableInterlocked.InterlockedInitialize(ref _unusedFieldWarnings, diagnostics.ToReadOnlyAndFree());
            }

            Debug.Assert(!_unusedFieldWarnings.IsDefault);
            return _unusedFieldWarnings;
        }

        private static string DefaultValue(TypeSymbol type)
        {
            // TODO: localize these strings
            if (type.IsReferenceType) return "null";
            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                    return "false";
                case SpecialType.System_Byte:
                case SpecialType.System_Decimal:
                case SpecialType.System_Double:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_SByte:
                case SpecialType.System_Single:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                    return "0";
                default:
                    return "";
            }
        }

#nullable enable

        internal override NamedTypeSymbol? TryLookupForwardedMetadataTypeWithCycleDetection(ref MetadataTypeName emittedName, ConsList<AssemblySymbol>? visitedAssemblies)
        {
            int forcedArity = emittedName.ForcedArity;

            if (emittedName.UseCLSCompliantNameArityEncoding)
            {
                if (forcedArity == -1)
                {
                    forcedArity = emittedName.InferredArity;
                }
                else if (forcedArity != emittedName.InferredArity)
                {
                    return null;
                }

                Debug.Assert(forcedArity == emittedName.InferredArity);
            }

            if (_lazyForwardedTypesFromSource == null)
            {
                IDictionary<string, NamedTypeSymbol> forwardedTypesFromSource;
                // Get the TypeForwardedTo attributes with minimal binding to avoid cycle problems
                HashSet<NamedTypeSymbol> forwardedTypes = GetForwardedTypes();

                if (forwardedTypes != null)
                {
                    forwardedTypesFromSource = new Dictionary<string, NamedTypeSymbol>(StringOrdinalComparer.Instance);

                    foreach (NamedTypeSymbol forwardedType in forwardedTypes)
                    {
                        NamedTypeSymbol originalDefinition = forwardedType.OriginalDefinition;
                        Debug.Assert((object)originalDefinition.ContainingType == null, "How did a nested type get forwarded?");

                        string fullEmittedName = MetadataHelpers.BuildQualifiedName(originalDefinition.ContainingSymbol.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat),
                                                                                    originalDefinition.MetadataName);
                        // Since we need to allow multiple constructions of the same generic type at the source
                        // level, we need to de-dup the original definitions.
                        forwardedTypesFromSource[fullEmittedName] = originalDefinition;
                    }
                }
                else
                {
                    forwardedTypesFromSource = SpecializedCollections.EmptyDictionary<string, NamedTypeSymbol>();
                }

                _lazyForwardedTypesFromSource = forwardedTypesFromSource;
            }

            NamedTypeSymbol? result;

            if (_lazyForwardedTypesFromSource.TryGetValue(emittedName.FullName, out result))
            {
                if ((forcedArity == -1 || result.Arity == forcedArity) &&
                    (!emittedName.UseCLSCompliantNameArityEncoding || result.Arity == 0 || result.MangleName))
                {
                    return result;
                }
            }
            else if (!_compilation.Options.OutputKind.IsNetModule())
            {
                // See if any of added modules forward the type.

                // Similar to attributes, type forwarders from the second added module should override type forwarders from the first added module, etc. 
                for (int i = _modules.Length - 1; i > 0; i--)
                {
                    var peModuleSymbol = (Metadata.PE.PEModuleSymbol)_modules[i];

                    (AssemblySymbol firstSymbol, AssemblySymbol secondSymbol) = peModuleSymbol.GetAssembliesForForwardedType(ref emittedName);

                    if ((object)firstSymbol != null)
                    {
                        if ((object)secondSymbol != null)
                        {
                            return CreateMultipleForwardingErrorTypeSymbol(ref emittedName, peModuleSymbol, firstSymbol, secondSymbol);
                        }

                        // Don't bother to check the forwarded-to assembly if we've already seen it.
                        if (visitedAssemblies != null && visitedAssemblies.Contains(firstSymbol))
                        {
                            return CreateCycleInTypeForwarderErrorTypeSymbol(ref emittedName);
                        }
                        else
                        {
                            visitedAssemblies = new ConsList<AssemblySymbol>(this, visitedAssemblies ?? ConsList<AssemblySymbol>.Empty);
                            return firstSymbol.LookupDeclaredOrForwardedTopLevelMetadataType(ref emittedName, visitedAssemblies);
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Returns data decoded from Obsolete attribute or null if there is no Obsolete attribute.
        /// This property returns ObsoleteAttributeData.Uninitialized if attribute arguments haven't been decoded yet.
        /// </summary>
        internal sealed override ObsoleteAttributeData? ObsoleteAttributeData
        {
            get
            {
                // [assembly: Experimental] may have been specified in the assembly or one of the modules
                var lazySourceAttributesBag = _lazySourceAttributesBag;
                if (lazySourceAttributesBag != null && lazySourceAttributesBag.IsDecodedWellKnownAttributeDataComputed)
                {
                    var data = (CommonAssemblyWellKnownAttributeData)lazySourceAttributesBag.DecodedWellKnownAttributeData;
                    if (data?.ExperimentalAttributeData is { } experimentalAttributeData)
                    {
                        return experimentalAttributeData;
                    }
                }

                var lazyNetModuleAttributesBag = _lazyNetModuleAttributesBag;
                if (lazyNetModuleAttributesBag != null && lazyNetModuleAttributesBag.IsDecodedWellKnownAttributeDataComputed)
                {
                    var data = (CommonAssemblyWellKnownAttributeData)lazyNetModuleAttributesBag.DecodedWellKnownAttributeData;
                    return data?.ExperimentalAttributeData;
                }

                if (GetAttributeDeclarations().IsEmpty)
                {
                    return null;
                }

                return ObsoleteAttributeData.Uninitialized;
            }
        }

#nullable disable

        internal override IEnumerable<NamedTypeSymbol> GetAllTopLevelForwardedTypes()
        {
            return PEModuleBuilder.GetForwardedTypes(this, builder: null, context: null);
        }

        public override AssemblyMetadata GetMetadata() => null;

        protected override ISymbol CreateISymbol()
        {
            return new PublicModel.SourceAssemblySymbol(this);
        }
    }
}
