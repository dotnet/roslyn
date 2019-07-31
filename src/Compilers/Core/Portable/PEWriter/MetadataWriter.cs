// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.DiaSymReader;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    internal abstract partial class MetadataWriter
    {
        internal static readonly Encoding s_utf8Encoding = Encoding.UTF8;

        /// <summary>
        /// This is the maximum length of a type or member name in metadata, assuming
        /// the name is in UTF-8 format and not (yet) null-terminated.
        /// </summary>
        /// <remarks>
        /// Source names may have to be shorter still to accommodate mangling.
        /// Used for event names, field names, property names, field names, method def names,
        /// member ref names, type def (full) names, type ref (full) names, exported type
        /// (full) names, parameter names, manifest resource names, and unmanaged method names
        /// (ImplMap table).
        /// 
        /// See CLI Part II, section 22.
        /// </remarks>
        internal const int NameLengthLimit = 1024 - 1; //MAX_CLASS_NAME = 1024 in dev11

        /// <summary>
        /// This is the maximum length of a path in metadata, assuming the path is in UTF-8
        /// format and not (yet) null-terminated.
        /// </summary>
        /// <remarks>
        /// Used for file names, module names, and module ref names.
        /// 
        /// See CLI Part II, section 22.
        /// </remarks>
        internal const int PathLengthLimit = 260 - 1; //MAX_PATH = 1024 in dev11

        /// <summary>
        /// This is the maximum length of a string in the PDB, assuming it is in UTF-8 format 
        /// and not (yet) null-terminated.
        /// </summary>
        /// <remarks>
        /// Used for import strings, locals, and local constants.
        /// </remarks>
        internal const int PdbLengthLimit = 2046; // Empirical, based on when ISymUnmanagedWriter2 methods start throwing.

        private readonly int _numTypeDefsEstimate;
        private readonly bool _deterministic;

        internal readonly bool MetadataOnly;
        internal readonly bool EmitTestCoverageData;

        // A map of method body before token translation to RVA. Used for deduplication of small bodies.
        private readonly Dictionary<ImmutableArray<byte>, int> _smallMethodBodies;

        private const byte TinyFormat = 2;
        private const int ThrowNullCodeSize = 2;
        private static readonly ImmutableArray<byte> ThrowNullEncodedBody =
            ImmutableArray.Create(
                (byte)((ThrowNullCodeSize << 2) | TinyFormat),
                (byte)ILOpCode.Ldnull,
                (byte)ILOpCode.Throw);

        protected MetadataWriter(
            MetadataBuilder metadata,
            MetadataBuilder debugMetadataOpt,
            DynamicAnalysisDataWriter dynamicAnalysisDataWriterOpt,
            EmitContext context,
            CommonMessageProvider messageProvider,
            bool metadataOnly,
            bool deterministic,
            bool emitTestCoverageData,
            CancellationToken cancellationToken)
        {
            Debug.Assert(metadata != debugMetadataOpt);

            this.module = context.Module;
            _deterministic = deterministic;
            this.MetadataOnly = metadataOnly;
            this.EmitTestCoverageData = emitTestCoverageData;

            // EDMAURER provide some reasonable size estimates for these that will avoid
            // much of the reallocation that would occur when growing these from empty.
            _signatureIndex = new Dictionary<ISignature, KeyValuePair<BlobHandle, ImmutableArray<byte>>>(module.HintNumberOfMethodDefinitions); //ignores field signatures

            _numTypeDefsEstimate = module.HintNumberOfMethodDefinitions / 6;
            _exportedTypeList = new List<ITypeReference>(_numTypeDefsEstimate);

            this.Context = context;
            this.messageProvider = messageProvider;
            _cancellationToken = cancellationToken;

            this.metadata = metadata;
            _debugMetadataOpt = debugMetadataOpt;
            _dynamicAnalysisDataWriterOpt = dynamicAnalysisDataWriterOpt;
            _smallMethodBodies = new Dictionary<ImmutableArray<byte>, int>(ByteSequenceComparer.Instance);
        }

        private int NumberOfTypeDefsEstimate { get { return _numTypeDefsEstimate; } }

        /// <summary>
        /// Returns true if writing full metadata, false if writing delta.
        /// </summary>
        internal bool IsFullMetadata
        {
            get { return this.Generation == 0; }
        }

        /// <summary>
        /// True if writing delta metadata in a minimal format.
        /// </summary>
        private bool IsMinimalDelta
        {
            get { return !IsFullMetadata; }
        }

        /// <summary>
        /// NetModules and EnC deltas don't have AssemblyDef record.
        /// We don't emit it for EnC deltas since assembly identity has to be preserved across generations (CLR/debugger get confused otherwise).
        /// </summary>
        private bool EmitAssemblyDefinition => module.OutputKind != OutputKind.NetModule && !IsMinimalDelta;

        /// <summary>
        /// Returns metadata generation ordinal. Zero for
        /// full metadata and non-zero for delta.
        /// </summary>
        protected abstract ushort Generation { get; }

        /// <summary>
        /// Returns unique Guid for this delta, or default(Guid)
        /// if full metadata.
        /// </summary>
        protected abstract Guid EncId { get; }

        /// <summary>
        /// Returns Guid of previous delta, or default(Guid)
        /// if full metadata or generation 1 delta.
        /// </summary>
        protected abstract Guid EncBaseId { get; }

        /// <summary>
        /// Returns true and full metadata handle of the type definition
        /// if the type definition is recognized. Otherwise returns false.
        /// </summary>
        protected abstract bool TryGetTypeDefinitionHandle(ITypeDefinition def, out TypeDefinitionHandle handle);

        /// <summary>
        /// Get full metadata handle of the type definition.
        /// </summary>
        protected abstract TypeDefinitionHandle GetTypeDefinitionHandle(ITypeDefinition def);

        /// <summary>
        /// The type definition corresponding to full metadata type handle.
        /// Deltas are only required to support indexing into current generation.
        /// </summary>
        protected abstract ITypeDefinition GetTypeDef(TypeDefinitionHandle handle);

        /// <summary>
        /// The type definitions to be emitted, in row order. These
        /// are just the type definitions from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<ITypeDefinition> GetTypeDefs();

        /// <summary>
        /// Get full metadata handle of the event definition.
        /// </summary>
        protected abstract EventDefinitionHandle GetEventDefinitionHandle(IEventDefinition def);

        /// <summary>
        /// The event definitions to be emitted, in row order. These
        /// are just the event definitions from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<IEventDefinition> GetEventDefs();

        /// <summary>
        /// Get full metadata handle of the field definition.
        /// </summary>
        protected abstract FieldDefinitionHandle GetFieldDefinitionHandle(IFieldDefinition def);

        /// <summary>
        /// The field definitions to be emitted, in row order. These
        /// are just the field definitions from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<IFieldDefinition> GetFieldDefs();

        /// <summary>
        /// Returns true and handle of the method definition
        /// if the method definition is recognized. Otherwise returns false.
        /// The index is into the full metadata.
        /// </summary>
        protected abstract bool TryGetMethodDefinitionHandle(IMethodDefinition def, out MethodDefinitionHandle handle);

        /// <summary>
        /// Get full metadata handle of the method definition.
        /// </summary>
        protected abstract MethodDefinitionHandle GetMethodDefinitionHandle(IMethodDefinition def);

        /// <summary>
        /// The method definition corresponding to full metadata method handle. 
        /// Deltas are only required to support indexing into current generation.
        /// </summary>
        protected abstract IMethodDefinition GetMethodDef(MethodDefinitionHandle handle);

        /// <summary>
        /// The method definitions to be emitted, in row order. These
        /// are just the method definitions from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<IMethodDefinition> GetMethodDefs();

        /// <summary>
        /// Get full metadata handle of the property definition.
        /// </summary>
        protected abstract PropertyDefinitionHandle GetPropertyDefIndex(IPropertyDefinition def);

        /// <summary>
        /// The property definitions to be emitted, in row order. These
        /// are just the property definitions from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<IPropertyDefinition> GetPropertyDefs();

        /// <summary>
        /// The full metadata handle of the parameter definition.
        /// </summary>
        protected abstract ParameterHandle GetParameterHandle(IParameterDefinition def);

        /// <summary>
        /// The parameter definitions to be emitted, in row order. These
        /// are just the parameter definitions from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<IParameterDefinition> GetParameterDefs();

        /// <summary>
        /// The generic parameter definitions to be emitted, in row order. These
        /// are just the generic parameter definitions from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<IGenericParameter> GetGenericParameters();

        /// <summary>
        /// The handle of the first field of the type.
        /// </summary>
        protected abstract FieldDefinitionHandle GetFirstFieldDefinitionHandle(INamedTypeDefinition typeDef);

        /// <summary>
        /// The handle of the first method of the type.
        /// </summary>
        protected abstract MethodDefinitionHandle GetFirstMethodDefinitionHandle(INamedTypeDefinition typeDef);

        /// <summary>
        /// The handle of the first parameter of the method.
        /// </summary>
        protected abstract ParameterHandle GetFirstParameterHandle(IMethodDefinition methodDef);

        /// <summary>
        /// Return full metadata handle of the assembly reference, adding
        /// the reference to the index for this generation if missing.
        /// Deltas are not required to return rows from previous generations.
        /// </summary>
        protected abstract AssemblyReferenceHandle GetOrAddAssemblyReferenceHandle(IAssemblyReference reference);

        /// <summary>
        /// The assembly references to be emitted, in row order. These
        /// are just the assembly references from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<AssemblyIdentity> GetAssemblyRefs();

        // ModuleRef table contains module names for TypeRefs that target types in netmodules (represented by IModuleReference),
        // and module names specified by P/Invokes (plain strings). Names in the table must be unique and are case sensitive.
        //
        // Spec 22.31 (ModuleRef : 0x1A)
        // "Name should match an entry in the Name column of the File table. Moreover, that entry shall enable the 
        // CLI to locate the target module (typically it might name the file used to hold the module)"
        // 
        // This is not how the Dev10 compilers and ILASM work. An entry is added to File table only for resources and netmodules.
        // Entries aren't added for P/Invoked modules.

        /// <summary>
        /// Return full metadata handle of the module reference, adding
        /// the reference to the index for this generation if missing.
        /// Deltas are not required to return rows from previous generations.
        /// </summary>
        protected abstract ModuleReferenceHandle GetOrAddModuleReferenceHandle(string reference);

        /// <summary>
        /// The module references to be emitted, in row order. These
        /// are just the module references from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<string> GetModuleRefs();

        /// <summary>
        /// Return full metadata handle of the member reference, adding
        /// the reference to the index for this generation if missing.
        /// Deltas are not required to return rows from previous generations.
        /// </summary>
        protected abstract MemberReferenceHandle GetOrAddMemberReferenceHandle(ITypeMemberReference reference);

        /// <summary>
        /// The member references to be emitted, in row order. These
        /// are just the member references from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<ITypeMemberReference> GetMemberRefs();

        /// <summary>
        /// Return full metadata handle of the method spec, adding
        /// the spec to the index for this generation if missing.
        /// Deltas are not required to return rows from previous generations.
        /// </summary>
        protected abstract MethodSpecificationHandle GetOrAddMethodSpecificationHandle(IGenericMethodInstanceReference reference);

        /// <summary>
        /// The method specs to be emitted, in row order. These
        /// are just the method specs from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<IGenericMethodInstanceReference> GetMethodSpecs();

        /// <summary>
        /// The greatest index given to any method definition.
        /// </summary>
        protected abstract int GreatestMethodDefIndex { get; }

        /// <summary>
        /// Return true and full metadata handle of the type reference
        /// if the reference is available in the current generation.
        /// Deltas are not required to return rows from previous generations.
        /// </summary>
        protected abstract bool TryGetTypeReferenceHandle(ITypeReference reference, out TypeReferenceHandle handle);

        /// <summary>
        /// Return full metadata handle of the type reference, adding
        /// the reference to the index for this generation if missing.
        /// Deltas are not required to return rows from previous generations.
        /// </summary>
        protected abstract TypeReferenceHandle GetOrAddTypeReferenceHandle(ITypeReference reference);

        /// <summary>
        /// The type references to be emitted, in row order. These
        /// are just the type references from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<ITypeReference> GetTypeRefs();

        /// <summary>
        /// Returns full metadata handle of the type spec, adding
        /// the spec to the index for this generation if missing.
        /// Deltas are not required to return rows from previous generations.
        /// </summary>
        protected abstract TypeSpecificationHandle GetOrAddTypeSpecificationHandle(ITypeReference reference);

        /// <summary>
        /// The type specs to be emitted, in row order. These
        /// are just the type specs from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<ITypeReference> GetTypeSpecs();

        /// <summary>
        /// Returns full metadata handle the standalone signature, adding
        /// the signature to the index for this generation if missing.
        /// Deltas are not required to return rows from previous generations.
        /// </summary>
        protected abstract StandaloneSignatureHandle GetOrAddStandaloneSignatureHandle(BlobHandle handle);

        /// <summary>
        /// The signature blob handles to be emitted, in row order. These
        /// are just the signature indices from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<BlobHandle> GetStandaloneSignatureBlobHandles();

        protected abstract void CreateIndicesForNonTypeMembers(ITypeDefinition typeDef);

        /// <summary>
        /// Return a visitor for traversing all references to be emitted.
        /// </summary>
        protected abstract ReferenceIndexer CreateReferenceVisitor();

        /// <summary>
        /// Populate EventMap table.
        /// </summary>
        protected abstract void PopulateEventMapTableRows();

        /// <summary>
        /// Populate PropertyMap table.
        /// </summary>
        protected abstract void PopulatePropertyMapTableRows();

        /// <summary>
        /// Populate EncLog table.
        /// </summary>
        protected abstract void PopulateEncLogTableRows(ImmutableArray<int> rowCounts);

        /// <summary>
        /// Populate EncMap table.
        /// </summary>
        protected abstract void PopulateEncMapTableRows(ImmutableArray<int> rowCounts);

        protected abstract void ReportReferencesToAddedSymbols();

        // If true, it is allowed to have methods not have bodies (for emitting metadata-only
        // assembly)
        private readonly CancellationToken _cancellationToken;
        protected readonly CommonPEModuleBuilder module;
        public readonly EmitContext Context;
        protected readonly CommonMessageProvider messageProvider;

        // progress:
        private bool _tableIndicesAreComplete;

        private EntityHandle[] _pseudoSymbolTokenToTokenMap;
        private IReference[] _pseudoSymbolTokenToReferenceMap;
        private UserStringHandle[] _pseudoStringTokenToTokenMap;
        private bool _userStringTokenOverflow;
        private List<string> _pseudoStringTokenToStringMap;
        private ReferenceIndexer _referenceVisitor;

        protected readonly MetadataBuilder metadata;

        // A builder for Portable or Embedded PDB metadata, or null if we are not emitting Portable/Embedded PDB.
        protected readonly MetadataBuilder _debugMetadataOpt;

        internal bool EmitPortableDebugMetadata => _debugMetadataOpt != null;

        private readonly DynamicAnalysisDataWriter _dynamicAnalysisDataWriterOpt;

        private readonly Dictionary<ICustomAttribute, BlobHandle> _customAttributeSignatureIndex = new Dictionary<ICustomAttribute, BlobHandle>();
        private readonly Dictionary<ITypeReference, BlobHandle> _typeSpecSignatureIndex = new Dictionary<ITypeReference, BlobHandle>();
        private readonly List<ITypeReference> _exportedTypeList;
        private readonly Dictionary<string, int> _fileRefIndex = new Dictionary<string, int>(32);  // more than enough in most cases, value is a RowId
        private readonly List<IFileReference> _fileRefList = new List<IFileReference>(32);
        private readonly Dictionary<IFieldReference, BlobHandle> _fieldSignatureIndex = new Dictionary<IFieldReference, BlobHandle>();

        // We need to keep track of both the index of the signature and the actual blob to support VB static local naming scheme.
        private readonly Dictionary<ISignature, KeyValuePair<BlobHandle, ImmutableArray<byte>>> _signatureIndex;

        private readonly Dictionary<IMarshallingInformation, BlobHandle> _marshallingDescriptorIndex = new Dictionary<IMarshallingInformation, BlobHandle>();
        protected readonly List<MethodImplementation> methodImplList = new List<MethodImplementation>();
        private readonly Dictionary<IGenericMethodInstanceReference, BlobHandle> _methodInstanceSignatureIndex = new Dictionary<IGenericMethodInstanceReference, BlobHandle>();

        // Well known dummy cor library types whose refs are used for attaching assembly attributes off within net modules
        // There is no guarantee the types actually exist in a cor library
        internal const string dummyAssemblyAttributeParentNamespace = "System.Runtime.CompilerServices";
        internal const string dummyAssemblyAttributeParentName = "AssemblyAttributesGoHere";
        internal static readonly string[,] dummyAssemblyAttributeParentQualifier = { { "", "M" }, { "S", "SM" } };
        private readonly TypeReferenceHandle[,] _dummyAssemblyAttributeParent = { { default(TypeReferenceHandle), default(TypeReferenceHandle) }, { default(TypeReferenceHandle), default(TypeReferenceHandle) } };

        internal CommonPEModuleBuilder Module => module;

        private void CreateMethodBodyReferenceIndex()
        {
            int count;
            var referencesInIL = module.ReferencesInIL(out count);

            _pseudoSymbolTokenToTokenMap = new EntityHandle[count];
            _pseudoSymbolTokenToReferenceMap = new IReference[count];

            int cur = 0;
            foreach (IReference o in referencesInIL)
            {
                _pseudoSymbolTokenToReferenceMap[cur] = o;
                cur++;
            }
        }

        private void CreateIndices()
        {
            _cancellationToken.ThrowIfCancellationRequested();

            this.CreateUserStringIndices();
            this.CreateInitialAssemblyRefIndex();
            this.CreateInitialFileRefIndex();
            this.CreateIndicesForModule();

            // Find all references and assign tokens.
            _referenceVisitor = this.CreateReferenceVisitor();
            _referenceVisitor.Visit(module);

            this.CreateMethodBodyReferenceIndex();

            this.OnIndicesCreated();
        }

        private void CreateUserStringIndices()
        {
            _pseudoStringTokenToStringMap = new List<string>();

            foreach (string str in this.module.GetStrings())
            {
                _pseudoStringTokenToStringMap.Add(str);
            }

            _pseudoStringTokenToTokenMap = new UserStringHandle[_pseudoStringTokenToStringMap.Count];
        }

        private void CreateIndicesForModule()
        {
            var nestedTypes = new Queue<INestedTypeDefinition>();

            foreach (INamespaceTypeDefinition typeDef in module.GetTopLevelTypeDefinitions(Context))
            {
                this.CreateIndicesFor(typeDef, nestedTypes);
            }

            while (nestedTypes.Count > 0)
            {
                var nestedType = nestedTypes.Dequeue();
                this.CreateIndicesFor(nestedType, nestedTypes);
            }
        }

        protected virtual void OnIndicesCreated()
        {
        }

        private void CreateIndicesFor(ITypeDefinition typeDef, Queue<INestedTypeDefinition> nestedTypes)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            this.CreateIndicesForNonTypeMembers(typeDef);

            // Metadata spec:
            // The TypeDef table has a special ordering constraint:
            // the definition of an enclosing class shall precede the definition of all classes it encloses.
            foreach (var nestedType in typeDef.GetNestedTypes(Context))
            {
                nestedTypes.Enqueue(nestedType);
            }
        }

        protected IEnumerable<IGenericTypeParameter> GetConsolidatedTypeParameters(ITypeDefinition typeDef)
        {
            INestedTypeDefinition nestedTypeDef = typeDef.AsNestedTypeDefinition(Context);
            if (nestedTypeDef == null)
            {
                if (typeDef.IsGeneric)
                {
                    return typeDef.GenericParameters;
                }

                return null;
            }

            return this.GetConsolidatedTypeParameters(typeDef, typeDef);
        }

        private List<IGenericTypeParameter> GetConsolidatedTypeParameters(ITypeDefinition typeDef, ITypeDefinition owner)
        {
            List<IGenericTypeParameter> result = null;
            INestedTypeDefinition nestedTypeDef = typeDef.AsNestedTypeDefinition(Context);
            if (nestedTypeDef != null)
            {
                result = this.GetConsolidatedTypeParameters(nestedTypeDef.ContainingTypeDefinition, owner);
            }

            if (typeDef.GenericParameterCount > 0)
            {
                ushort index = 0;
                if (result == null)
                {
                    result = new List<IGenericTypeParameter>();
                }
                else
                {
                    index = (ushort)result.Count;
                }

                if (typeDef == owner && index == 0)
                {
                    result.AddRange(typeDef.GenericParameters);
                }
                else
                {
                    foreach (IGenericTypeParameter genericParameter in typeDef.GenericParameters)
                    {
                        result.Add(new InheritedTypeParameter(index++, owner, genericParameter));
                    }
                }
            }

            return result;
        }

        protected ImmutableArray<IParameterDefinition> GetParametersToEmit(IMethodDefinition methodDef)
        {
            if (methodDef.ParameterCount == 0 && !(methodDef.ReturnValueIsMarshalledExplicitly || IteratorHelper.EnumerableIsNotEmpty(methodDef.GetReturnValueAttributes(Context))))
            {
                return ImmutableArray<IParameterDefinition>.Empty;
            }

            return GetParametersToEmitCore(methodDef);
        }

        private ImmutableArray<IParameterDefinition> GetParametersToEmitCore(IMethodDefinition methodDef)
        {
            ArrayBuilder<IParameterDefinition> builder = null;
            var parameters = methodDef.Parameters;

            if (methodDef.ReturnValueIsMarshalledExplicitly || IteratorHelper.EnumerableIsNotEmpty(methodDef.GetReturnValueAttributes(Context)))
            {
                builder = ArrayBuilder<IParameterDefinition>.GetInstance(parameters.Length + 1);
                builder.Add(new ReturnValueParameter(methodDef));
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                IParameterDefinition parDef = parameters[i];

                // No explicit param row is needed if param has no flags (other than optionally IN),
                // no name and no references to the param row, such as CustomAttribute, Constant, or FieldMarshal
                if (parDef.Name != String.Empty ||
                    parDef.HasDefaultValue || parDef.IsOptional || parDef.IsOut || parDef.IsMarshalledExplicitly ||
                    IteratorHelper.EnumerableIsNotEmpty(parDef.GetAttributes(Context)))
                {
                    if (builder != null)
                    {
                        builder.Add(parDef);
                    }
                }
                else
                {
                    // we have a parameter that does not need to be emitted (not common)
                    if (builder == null)
                    {
                        builder = ArrayBuilder<IParameterDefinition>.GetInstance(parameters.Length);
                        builder.AddRange(parameters, i);
                    }
                }
            }

            return builder?.ToImmutableAndFree() ?? parameters;
        }

        /// <summary>
        /// Returns a reference to the unit that defines the given referenced type. If the referenced type is a structural type, such as a pointer or a generic type instance,
        /// then the result is null.
        /// </summary>
        public static IUnitReference GetDefiningUnitReference(ITypeReference typeReference, EmitContext context)
        {
            INestedTypeReference nestedTypeReference = typeReference.AsNestedTypeReference;
            while (nestedTypeReference != null)
            {
                if (nestedTypeReference.AsGenericTypeInstanceReference != null)
                {
                    return null;
                }

                typeReference = nestedTypeReference.GetContainingType(context);
                nestedTypeReference = typeReference.AsNestedTypeReference;
            }

            INamespaceTypeReference namespaceTypeReference = typeReference.AsNamespaceTypeReference;
            if (namespaceTypeReference == null)
            {
                return null;
            }

            Debug.Assert(namespaceTypeReference.AsGenericTypeInstanceReference == null);

            return namespaceTypeReference.GetUnit(context);
        }

        private void CreateInitialAssemblyRefIndex()
        {
            Debug.Assert(!_tableIndicesAreComplete);
            foreach (IAssemblyReference assemblyRef in this.module.GetAssemblyReferences(Context))
            {
                this.GetOrAddAssemblyReferenceHandle(assemblyRef);
            }
        }

        private void CreateInitialFileRefIndex()
        {
            Debug.Assert(!_tableIndicesAreComplete);

            foreach (IFileReference fileRef in module.GetFiles(Context))
            {
                string key = fileRef.FileName;
                if (!_fileRefIndex.ContainsKey(key))
                {
                    _fileRefList.Add(fileRef);
                    _fileRefIndex.Add(key, _fileRefList.Count);
                }
            }
        }

        internal AssemblyReferenceHandle GetAssemblyReferenceHandle(IAssemblyReference assemblyReference)
        {
            var containingAssembly = this.module.GetContainingAssembly(Context);

            if (containingAssembly != null && ReferenceEquals(assemblyReference, containingAssembly))
            {
                return default(AssemblyReferenceHandle);
            }

            return this.GetOrAddAssemblyReferenceHandle(assemblyReference);
        }

        internal ModuleReferenceHandle GetModuleReferenceHandle(string moduleName)
        {
            return this.GetOrAddModuleReferenceHandle(moduleName);
        }

        private BlobHandle GetCustomAttributeSignatureIndex(ICustomAttribute customAttribute)
        {
            BlobHandle result;
            if (_customAttributeSignatureIndex.TryGetValue(customAttribute, out result))
            {
                return result;
            }

            var writer = PooledBlobBuilder.GetInstance();
            this.SerializeCustomAttributeSignature(customAttribute, writer);
            result = metadata.GetOrAddBlob(writer);
            _customAttributeSignatureIndex.Add(customAttribute, result);
            writer.Free();
            return result;
        }

        private EntityHandle GetCustomAttributeTypeCodedIndex(IMethodReference methodReference)
        {
            IMethodDefinition methodDef = null;
            IUnitReference definingUnit = GetDefiningUnitReference(methodReference.GetContainingType(Context), Context);
            if (definingUnit != null && ReferenceEquals(definingUnit, this.module))
            {
                methodDef = methodReference.GetResolvedMethod(Context);
            }

            return methodDef != null
                ? (EntityHandle)GetMethodDefinitionHandle(methodDef)
                : GetMemberReferenceHandle(methodReference);
        }

        public static EventAttributes GetEventAttributes(IEventDefinition eventDef)
        {
            EventAttributes result = 0;
            if (eventDef.IsSpecialName)
            {
                result |= EventAttributes.SpecialName;
            }

            if (eventDef.IsRuntimeSpecial)
            {
                result |= EventAttributes.RTSpecialName;
            }

            return result;
        }

        public static FieldAttributes GetFieldAttributes(IFieldDefinition fieldDef)
        {
            var result = (FieldAttributes)fieldDef.Visibility;
            if (fieldDef.IsStatic)
            {
                result |= FieldAttributes.Static;
            }

            if (fieldDef.IsReadOnly)
            {
                result |= FieldAttributes.InitOnly;
            }

            if (fieldDef.IsCompileTimeConstant)
            {
                result |= FieldAttributes.Literal;
            }

            if (fieldDef.IsNotSerialized)
            {
                result |= FieldAttributes.NotSerialized;
            }

            if (!fieldDef.MappedData.IsDefault)
            {
                result |= FieldAttributes.HasFieldRVA;
            }

            if (fieldDef.IsSpecialName)
            {
                result |= FieldAttributes.SpecialName;
            }

            if (fieldDef.IsRuntimeSpecial)
            {
                result |= FieldAttributes.RTSpecialName;
            }

            if (fieldDef.IsMarshalledExplicitly)
            {
                result |= FieldAttributes.HasFieldMarshal;
            }

            if (fieldDef.IsCompileTimeConstant)
            {
                result |= FieldAttributes.HasDefault;
            }

            return result;
        }

        internal BlobHandle GetFieldSignatureIndex(IFieldReference fieldReference)
        {
            BlobHandle result;
            ISpecializedFieldReference specializedFieldReference = fieldReference.AsSpecializedFieldReference;
            if (specializedFieldReference != null)
            {
                fieldReference = specializedFieldReference.UnspecializedVersion;
            }

            if (_fieldSignatureIndex.TryGetValue(fieldReference, out result))
            {
                return result;
            }

            var writer = PooledBlobBuilder.GetInstance();
            this.SerializeFieldSignature(fieldReference, writer);
            result = metadata.GetOrAddBlob(writer);
            _fieldSignatureIndex.Add(fieldReference, result);
            writer.Free();
            return result;
        }

        internal EntityHandle GetFieldHandle(IFieldReference fieldReference)
        {
            IFieldDefinition fieldDef = null;
            IUnitReference definingUnit = GetDefiningUnitReference(fieldReference.GetContainingType(Context), Context);
            if (definingUnit != null && ReferenceEquals(definingUnit, this.module))
            {
                fieldDef = fieldReference.GetResolvedField(Context);
            }

            return fieldDef != null
                ? (EntityHandle)GetFieldDefinitionHandle(fieldDef)
                : GetMemberReferenceHandle(fieldReference);
        }

        internal AssemblyFileHandle GetAssemblyFileHandle(IFileReference fileReference)
        {
            string key = fileReference.FileName;
            int index;
            if (!_fileRefIndex.TryGetValue(key, out index))
            {
                Debug.Assert(!_tableIndicesAreComplete);
                _fileRefList.Add(fileReference);
                _fileRefIndex.Add(key, index = _fileRefList.Count);
            }

            return MetadataTokens.AssemblyFileHandle(index);
        }

        private AssemblyFileHandle GetAssemblyFileHandle(IModuleReference mref)
        {
            return MetadataTokens.AssemblyFileHandle(_fileRefIndex[mref.Name]);
        }

        private static GenericParameterAttributes GetGenericParameterAttributes(IGenericParameter genPar)
        {
            GenericParameterAttributes result = 0;
            switch (genPar.Variance)
            {
                case TypeParameterVariance.Covariant:
                    result |= GenericParameterAttributes.Covariant;
                    break;
                case TypeParameterVariance.Contravariant:
                    result |= GenericParameterAttributes.Contravariant;
                    break;
            }

            if (genPar.MustBeReferenceType)
            {
                result |= GenericParameterAttributes.ReferenceTypeConstraint;
            }

            if (genPar.MustBeValueType)
            {
                result |= GenericParameterAttributes.NotNullableValueTypeConstraint;
            }

            if (genPar.MustHaveDefaultConstructor)
            {
                result |= GenericParameterAttributes.DefaultConstructorConstraint;
            }

            return result;
        }

        private EntityHandle GetExportedTypeImplementation(INamespaceTypeReference namespaceRef)
        {
            IUnitReference uref = namespaceRef.GetUnit(Context);
            if (uref is IAssemblyReference aref)
            {
                return GetAssemblyReferenceHandle(aref);
            }

            var mref = (IModuleReference)uref;
            aref = mref.GetContainingAssembly(Context);
            return aref == null || ReferenceEquals(aref, this.module.GetContainingAssembly(Context))
                ? (EntityHandle)GetAssemblyFileHandle(mref)
                : GetAssemblyReferenceHandle(aref);
        }

        private static uint GetManagedResourceOffset(ManagedResource resource, BlobBuilder resourceWriter)
        {
            if (resource.ExternalFile != null)
            {
                return resource.Offset;
            }

            int result = resourceWriter.Count;
            resource.WriteData(resourceWriter);
            return (uint)result;
        }

        private static uint GetManagedResourceOffset(BlobBuilder resource, BlobBuilder resourceWriter)
        {
            int result = resourceWriter.Count;
            resourceWriter.WriteInt32(resource.Count);
            resource.WriteContentTo(resourceWriter);
            resourceWriter.Align(8);
            return (uint)result;
        }

        public static string GetMangledName(INamedTypeReference namedType)
        {
            string unmangledName = namedType.Name;

            return namedType.MangleName
                ? MetadataHelpers.ComposeAritySuffixedMetadataName(unmangledName, namedType.GenericParameterCount)
                : unmangledName;
        }

        internal MemberReferenceHandle GetMemberReferenceHandle(ITypeMemberReference memberRef)
        {
            return this.GetOrAddMemberReferenceHandle(memberRef);
        }

        internal EntityHandle GetMemberReferenceParent(ITypeMemberReference memberRef)
        {
            ITypeDefinition parentTypeDef = memberRef.GetContainingType(Context).AsTypeDefinition(Context);
            if (parentTypeDef != null)
            {
                TypeDefinitionHandle parentTypeDefHandle;
                TryGetTypeDefinitionHandle(parentTypeDef, out parentTypeDefHandle);

                if (!parentTypeDefHandle.IsNil)
                {
                    if (memberRef is IFieldReference)
                    {
                        return parentTypeDefHandle;
                    }

                    if (memberRef is IMethodReference methodRef)
                    {
                        if (methodRef.AcceptsExtraArguments)
                        {
                            MethodDefinitionHandle methodHandle;
                            if (this.TryGetMethodDefinitionHandle(methodRef.GetResolvedMethod(Context), out methodHandle))
                            {
                                return methodHandle;
                            }
                        }

                        return parentTypeDefHandle;
                    }
                    // TODO: error
                }
            }

            // TODO: special treatment for global fields and methods. Object model support would be nice.
            var containingType = memberRef.GetContainingType(Context);
            return containingType.IsTypeSpecification()
                ? (EntityHandle)GetTypeSpecificationHandle(containingType)
                : GetTypeReferenceHandle(containingType);
        }

        internal EntityHandle GetMethodDefinitionOrReferenceHandle(IMethodReference methodReference)
        {
            IMethodDefinition methodDef = null;
            IUnitReference definingUnit = GetDefiningUnitReference(methodReference.GetContainingType(Context), Context);
            if (definingUnit != null && ReferenceEquals(definingUnit, this.module))
            {
                methodDef = methodReference.GetResolvedMethod(Context);
            }

            return methodDef != null
                ? (EntityHandle)GetMethodDefinitionHandle(methodDef)
                : GetMemberReferenceHandle(methodReference);
        }

        public static MethodAttributes GetMethodAttributes(IMethodDefinition methodDef)
        {
            var result = (MethodAttributes)methodDef.Visibility;
            if (methodDef.IsStatic)
            {
                result |= MethodAttributes.Static;
            }

            if (methodDef.IsSealed)
            {
                result |= MethodAttributes.Final;
            }

            if (methodDef.IsVirtual)
            {
                result |= MethodAttributes.Virtual;
            }

            if (methodDef.IsHiddenBySignature)
            {
                result |= MethodAttributes.HideBySig;
            }

            if (methodDef.IsNewSlot)
            {
                result |= MethodAttributes.NewSlot;
            }

            if (methodDef.IsAccessCheckedOnOverride)
            {
                result |= MethodAttributes.CheckAccessOnOverride;
            }

            if (methodDef.IsAbstract)
            {
                result |= MethodAttributes.Abstract;
            }

            if (methodDef.IsSpecialName)
            {
                result |= MethodAttributes.SpecialName;
            }

            if (methodDef.IsRuntimeSpecial)
            {
                result |= MethodAttributes.RTSpecialName;
            }

            if (methodDef.IsPlatformInvoke)
            {
                result |= MethodAttributes.PinvokeImpl;
            }

            if (methodDef.HasDeclarativeSecurity)
            {
                result |= MethodAttributes.HasSecurity;
            }

            if (methodDef.RequiresSecurityObject)
            {
                result |= MethodAttributes.RequireSecObject;
            }

            return result;
        }

        internal BlobHandle GetMethodSpecificationSignatureHandle(IGenericMethodInstanceReference methodInstanceReference)
        {
            BlobHandle result;
            if (_methodInstanceSignatureIndex.TryGetValue(methodInstanceReference, out result))
            {
                return result;
            }

            var builder = PooledBlobBuilder.GetInstance();
            var encoder = new BlobEncoder(builder).MethodSpecificationSignature(methodInstanceReference.GetGenericMethod(Context).GenericParameterCount);

            foreach (ITypeReference typeReference in methodInstanceReference.GetGenericArguments(Context))
            {
                var typeRef = typeReference;
                SerializeTypeReference(encoder.AddArgument(), typeRef);
            }

            result = metadata.GetOrAddBlob(builder);
            _methodInstanceSignatureIndex.Add(methodInstanceReference, result);
            builder.Free();
            return result;
        }

        private BlobHandle GetMarshallingDescriptorHandle(IMarshallingInformation marshallingInformation)
        {
            BlobHandle result;
            if (_marshallingDescriptorIndex.TryGetValue(marshallingInformation, out result))
            {
                return result;
            }

            var writer = PooledBlobBuilder.GetInstance();
            this.SerializeMarshallingDescriptor(marshallingInformation, writer);
            result = metadata.GetOrAddBlob(writer);
            _marshallingDescriptorIndex.Add(marshallingInformation, result);
            writer.Free();
            return result;
        }

        private BlobHandle GetMarshallingDescriptorHandle(ImmutableArray<byte> descriptor)
        {
            return metadata.GetOrAddBlob(descriptor);
        }

        private BlobHandle GetMemberReferenceSignatureHandle(ITypeMemberReference memberRef)
        {
            return memberRef switch
            {
                IFieldReference fieldReference => this.GetFieldSignatureIndex(fieldReference),
                IMethodReference methodReference => this.GetMethodSignatureHandle(methodReference),
                _ => throw ExceptionUtilities.Unreachable
            };
        }

        internal BlobHandle GetMethodSignatureHandle(IMethodReference methodReference)
        {
            ImmutableArray<byte> signatureBlob;
            return GetMethodSignatureHandleAndBlob(methodReference, out signatureBlob);
        }

        internal byte[] GetMethodSignature(IMethodReference methodReference)
        {
            ImmutableArray<byte> signatureBlob;
            GetMethodSignatureHandleAndBlob(methodReference, out signatureBlob);
            return signatureBlob.ToArray();
        }

        private BlobHandle GetMethodSignatureHandleAndBlob(IMethodReference methodReference, out ImmutableArray<byte> signatureBlob)
        {
            BlobHandle result;
            ISpecializedMethodReference specializedMethodReference = methodReference.AsSpecializedMethodReference;
            if (specializedMethodReference != null)
            {
                methodReference = specializedMethodReference.UnspecializedVersion;
            }

            KeyValuePair<BlobHandle, ImmutableArray<byte>> existing;
            if (_signatureIndex.TryGetValue(methodReference, out existing))
            {
                signatureBlob = existing.Value;
                return existing.Key;
            }

            Debug.Assert((methodReference.CallingConvention & CallingConvention.Generic) != 0 == (methodReference.GenericParameterCount > 0));

            var builder = PooledBlobBuilder.GetInstance();

            var encoder = new BlobEncoder(builder).MethodSignature(
                new SignatureHeader((byte)methodReference.CallingConvention).CallingConvention,
                methodReference.GenericParameterCount,
                isInstanceMethod: (methodReference.CallingConvention & CallingConvention.HasThis) != 0);

            SerializeReturnValueAndParameters(encoder, methodReference, methodReference.ExtraParameters);


            signatureBlob = builder.ToImmutableArray();
            result = metadata.GetOrAddBlob(signatureBlob);
            _signatureIndex.Add(methodReference, KeyValuePairUtil.Create(result, signatureBlob));
            builder.Free();
            return result;
        }

        private BlobHandle GetMethodSpecificationBlobHandle(IGenericMethodInstanceReference genericMethodInstanceReference)
        {
            var writer = PooledBlobBuilder.GetInstance();
            SerializeMethodSpecificationSignature(writer, genericMethodInstanceReference);
            BlobHandle result = metadata.GetOrAddBlob(writer);
            writer.Free();
            return result;
        }

        private MethodSpecificationHandle GetMethodSpecificationHandle(IGenericMethodInstanceReference methodSpec)
        {
            return this.GetOrAddMethodSpecificationHandle(methodSpec);
        }

        internal EntityHandle GetMethodHandle(IMethodReference methodReference)
        {
            MethodDefinitionHandle methodDefHandle;
            IMethodDefinition methodDef = null;
            IUnitReference definingUnit = GetDefiningUnitReference(methodReference.GetContainingType(Context), Context);
            if (definingUnit != null && ReferenceEquals(definingUnit, this.module))
            {
                methodDef = methodReference.GetResolvedMethod(Context);
            }

            if (methodDef != null && (methodReference == methodDef || !methodReference.AcceptsExtraArguments) && this.TryGetMethodDefinitionHandle(methodDef, out methodDefHandle))
            {
                return methodDefHandle;
            }

            IGenericMethodInstanceReference methodSpec = methodReference.AsGenericMethodInstanceReference;
            return methodSpec != null
                ? (EntityHandle)GetMethodSpecificationHandle(methodSpec)
                : GetMemberReferenceHandle(methodReference);
        }

        public static ParameterAttributes GetParameterAttributes(IParameterDefinition parDef)
        {
            ParameterAttributes result = 0;
            if (parDef.IsIn)
            {
                result |= ParameterAttributes.In;
            }

            if (parDef.IsOut)
            {
                result |= ParameterAttributes.Out;
            }

            if (parDef.IsOptional)
            {
                result |= ParameterAttributes.Optional;
            }

            if (parDef.HasDefaultValue)
            {
                result |= ParameterAttributes.HasDefault;
            }

            if (parDef.IsMarshalledExplicitly)
            {
                result |= ParameterAttributes.HasFieldMarshal;
            }

            return result;
        }

        private BlobHandle GetPermissionSetBlobHandle(ImmutableArray<ICustomAttribute> permissionSet)
        {
            var writer = PooledBlobBuilder.GetInstance();
            BlobHandle result;
            try
            {
                writer.WriteByte((byte)'.');
                writer.WriteCompressedInteger(permissionSet.Length);
                this.SerializePermissionSet(permissionSet, writer);
                result = metadata.GetOrAddBlob(writer);
            }
            finally
            {
                writer.Free();
            }

            return result;
        }

        public static PropertyAttributes GetPropertyAttributes(IPropertyDefinition propertyDef)
        {
            PropertyAttributes result = 0;
            if (propertyDef.IsSpecialName)
            {
                result |= PropertyAttributes.SpecialName;
            }

            if (propertyDef.IsRuntimeSpecial)
            {
                result |= PropertyAttributes.RTSpecialName;
            }

            if (propertyDef.HasDefaultValue)
            {
                result |= PropertyAttributes.HasDefault;
            }

            return result;
        }

        private BlobHandle GetPropertySignatureHandle(IPropertyDefinition propertyDef)
        {
            KeyValuePair<BlobHandle, ImmutableArray<byte>> existing;
            if (_signatureIndex.TryGetValue(propertyDef, out existing))
            {
                return existing.Key;
            }

            var builder = PooledBlobBuilder.GetInstance();

            var encoder = new BlobEncoder(builder).PropertySignature(
                isInstanceProperty: (propertyDef.CallingConvention & CallingConvention.HasThis) != 0);

            SerializeReturnValueAndParameters(encoder, propertyDef, ImmutableArray<IParameterTypeInformation>.Empty);

            var blob = builder.ToImmutableArray();
            var result = metadata.GetOrAddBlob(blob);

            _signatureIndex.Add(propertyDef, KeyValuePairUtil.Create(result, blob));
            builder.Free();
            return result;
        }

        private EntityHandle GetResolutionScopeHandle(IUnitReference unitReference)
        {
            if (unitReference is IAssemblyReference aref)
            {
                return GetAssemblyReferenceHandle(aref);
            }

            // If this is a module from a referenced multi-module assembly,
            // the assembly should be used as the resolution scope.
            var mref = (IModuleReference)unitReference;
            aref = mref.GetContainingAssembly(Context);

            if (aref != null && aref != module.GetContainingAssembly(Context))
            {
                return GetAssemblyReferenceHandle(aref);
            }

            return GetModuleReferenceHandle(mref.Name);
        }

        private StringHandle GetStringHandleForPathAndCheckLength(string path, INamedEntity errorEntity = null)
        {
            CheckPathLength(path, errorEntity);
            return metadata.GetOrAddString(path);
        }

        private StringHandle GetStringHandleForNameAndCheckLength(string name, INamedEntity errorEntity = null)
        {
            CheckNameLength(name, errorEntity);
            return metadata.GetOrAddString(name);
        }

        /// <summary>
        /// The Microsoft CLR requires that {namespace} + "." + {name} fit in MAX_CLASS_NAME 
        /// (even though the name and namespace are stored separately in the Microsoft
        /// implementation).  Note that the namespace name of a nested type is always blank
        /// (since comes from the container).
        /// </summary>
        /// <param name="namespaceType">We're trying to add the containing namespace of this type to the string heap.</param>
        /// <param name="mangledTypeName">Namespace names are never used on their own - this is the type that is adding the namespace name.
        /// Used only for length checking.</param>
        private StringHandle GetStringHandleForNamespaceAndCheckLength(INamespaceTypeReference namespaceType, string mangledTypeName)
        {
            string namespaceName = namespaceType.NamespaceName;
            if (namespaceName.Length == 0) // Optimization: CheckNamespaceLength is relatively expensive.
            {
                return default(StringHandle);
            }

            CheckNamespaceLength(namespaceName, mangledTypeName, namespaceType);
            return metadata.GetOrAddString(namespaceName);
        }

        private void CheckNameLength(string name, INamedEntity errorEntity)
        {
            // NOTE: ildasm shows quotes around some names (e.g. explicit implementations of members of generic interfaces)
            // but that seems to be tool-specific - they don't seem to and up in the string heap (so they don't count against
            // the length limit).

            if (IsTooLongInternal(name, NameLengthLimit))
            {
                Location location = GetNamedEntityLocation(errorEntity);
                this.Context.Diagnostics.Add(this.messageProvider.CreateDiagnostic(this.messageProvider.ERR_MetadataNameTooLong, location, name));
            }
        }

        private void CheckPathLength(string path, INamedEntity errorEntity = null)
        {
            if (IsTooLongInternal(path, PathLengthLimit))
            {
                Location location = GetNamedEntityLocation(errorEntity);
                this.Context.Diagnostics.Add(this.messageProvider.CreateDiagnostic(this.messageProvider.ERR_MetadataNameTooLong, location, path));
            }
        }

        private void CheckNamespaceLength(string namespaceName, string mangledTypeName, INamespaceTypeReference errorEntity)
        {
            // It's never useful to report that the namespace name is too long.
            // If it's too long, then the full name is too long and that string is
            // more helpful.

            // PERF: We expect to check this A LOT, so we'll aggressively inline some
            // of the helpers (esp IsTooLongInternal) in a way that allows us to forego
            // string concatenation (unless a diagnostic is actually reported).

            if (namespaceName.Length + 1 + mangledTypeName.Length > NameLengthLimit / 3)
            {
                int utf8Length =
                    s_utf8Encoding.GetByteCount(namespaceName) +
                    1 + // dot
                    s_utf8Encoding.GetByteCount(mangledTypeName);

                if (utf8Length > NameLengthLimit)
                {
                    Location location = GetNamedEntityLocation(errorEntity);
                    this.Context.Diagnostics.Add(this.messageProvider.CreateDiagnostic(this.messageProvider.ERR_MetadataNameTooLong, location, namespaceName + "." + mangledTypeName));
                }
            }
        }

        internal bool IsUsingStringTooLong(string usingString, INamedEntity errorEntity = null)
        {
            if (IsTooLongInternal(usingString, PdbLengthLimit))
            {
                Location location = GetNamedEntityLocation(errorEntity);
                this.Context.Diagnostics.Add(this.messageProvider.CreateDiagnostic(this.messageProvider.WRN_PdbUsingNameTooLong, location, usingString));
                return true;
            }

            return false;
        }

        internal bool IsLocalNameTooLong(ILocalDefinition localDefinition)
        {
            string name = localDefinition.Name;
            if (IsTooLongInternal(name, PdbLengthLimit))
            {
                this.Context.Diagnostics.Add(this.messageProvider.CreateDiagnostic(this.messageProvider.WRN_PdbLocalNameTooLong, localDefinition.Location, name));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Test the given name to see if it fits in metadata.
        /// </summary>
        /// <param name="str">String to test (non-null).</param>
        /// <param name="maxLength">Max length for name.  (Expected to be at least 5.)</param>
        /// <returns>True if the name is too long.</returns>
        /// <remarks>Internal for test purposes.</remarks>
        internal static bool IsTooLongInternal(string str, int maxLength)
        {
            Debug.Assert(str != null); // No need to handle in an internal utility.

            if (str.Length < maxLength / 3) //UTF-8 uses at most 3 bytes per char
            {
                return false;
            }

            int utf8Length = s_utf8Encoding.GetByteCount(str);
            return utf8Length > maxLength;
        }

        private static Location GetNamedEntityLocation(INamedEntity errorEntity)
        {
            return GetSymbolLocation(errorEntity as ISymbol);
        }

        protected static Location GetSymbolLocation(ISymbol symbolOpt)
        {
            return symbolOpt != null && !symbolOpt.Locations.IsDefaultOrEmpty ? symbolOpt.Locations[0] : Location.None;
        }

        internal TypeAttributes GetTypeAttributes(ITypeDefinition typeDef)
        {
            return GetTypeAttributes(typeDef, Context);
        }

        public static TypeAttributes GetTypeAttributes(ITypeDefinition typeDef, EmitContext context)
        {
            TypeAttributes result = 0;

            switch (typeDef.Layout)
            {
                case LayoutKind.Sequential:
                    result |= TypeAttributes.SequentialLayout;
                    break;

                case LayoutKind.Explicit:
                    result |= TypeAttributes.ExplicitLayout;
                    break;
            }

            if (typeDef.IsInterface)
            {
                result |= TypeAttributes.Interface;
            }

            if (typeDef.IsAbstract)
            {
                result |= TypeAttributes.Abstract;
            }

            if (typeDef.IsSealed)
            {
                result |= TypeAttributes.Sealed;
            }

            if (typeDef.IsSpecialName)
            {
                result |= TypeAttributes.SpecialName;
            }

            if (typeDef.IsRuntimeSpecial)
            {
                result |= TypeAttributes.RTSpecialName;
            }

            if (typeDef.IsComObject)
            {
                result |= TypeAttributes.Import;
            }

            if (typeDef.IsSerializable)
            {
                result |= TypeAttributes.Serializable;
            }

            if (typeDef.IsWindowsRuntimeImport)
            {
                result |= TypeAttributes.WindowsRuntime;
            }

            switch (typeDef.StringFormat)
            {
                case CharSet.Unicode:
                    result |= TypeAttributes.UnicodeClass;
                    break;

                case Constants.CharSet_Auto:
                    result |= TypeAttributes.AutoClass;
                    break;
            }

            if (typeDef.HasDeclarativeSecurity)
            {
                result |= TypeAttributes.HasSecurity;
            }

            if (typeDef.IsBeforeFieldInit)
            {
                result |= TypeAttributes.BeforeFieldInit;
            }

            INestedTypeDefinition nestedTypeDef = typeDef.AsNestedTypeDefinition(context);
            if (nestedTypeDef != null)
            {
                switch (((ITypeDefinitionMember)typeDef).Visibility)
                {
                    case TypeMemberVisibility.Public:
                        result |= TypeAttributes.NestedPublic;
                        break;
                    case TypeMemberVisibility.Private:
                        result |= TypeAttributes.NestedPrivate;
                        break;
                    case TypeMemberVisibility.Family:
                        result |= TypeAttributes.NestedFamily;
                        break;
                    case TypeMemberVisibility.Assembly:
                        result |= TypeAttributes.NestedAssembly;
                        break;
                    case TypeMemberVisibility.FamilyAndAssembly:
                        result |= TypeAttributes.NestedFamANDAssem;
                        break;
                    case TypeMemberVisibility.FamilyOrAssembly:
                        result |= TypeAttributes.NestedFamORAssem;
                        break;
                }

                return result;
            }

            INamespaceTypeDefinition namespaceTypeDef = typeDef.AsNamespaceTypeDefinition(context);
            if (namespaceTypeDef != null && namespaceTypeDef.IsPublic)
            {
                result |= TypeAttributes.Public;
            }

            return result;
        }

        private EntityHandle GetDeclaringTypeOrMethodHandle(IGenericParameter genPar)
        {
            IGenericTypeParameter genTypePar = genPar.AsGenericTypeParameter;
            if (genTypePar != null)
            {
                return GetTypeDefinitionHandle(genTypePar.DefiningType);
            }

            IGenericMethodParameter genMethPar = genPar.AsGenericMethodParameter;
            if (genMethPar != null)
            {
                return GetMethodDefinitionHandle(genMethPar.DefiningMethod);
            }

            throw ExceptionUtilities.Unreachable;
        }

        private TypeReferenceHandle GetTypeReferenceHandle(ITypeReference typeReference)
        {
            TypeReferenceHandle result;
            if (this.TryGetTypeReferenceHandle(typeReference, out result))
            {
                return result;
            }

            // NOTE: Even though CLR documentation does not explicitly specify any requirements 
            // NOTE: to the order of records in TypeRef table, some tools and/or APIs (e.g. 
            // NOTE: IMetaDataEmit::MergeEnd) assume that the containing type referenced as 
            // NOTE: ResolutionScope for its nested types should appear in TypeRef table
            // NOTE: *before* any of its nested types.
            // SEE ALSO: bug#570975 and test Bug570975()
            INestedTypeReference nestedTypeRef = typeReference.AsNestedTypeReference;
            if (nestedTypeRef != null)
            {
                GetTypeReferenceHandle(nestedTypeRef.GetContainingType(this.Context));
            }

            return this.GetOrAddTypeReferenceHandle(typeReference);
        }

        private TypeSpecificationHandle GetTypeSpecificationHandle(ITypeReference typeReference)
        {
            return this.GetOrAddTypeSpecificationHandle(typeReference);
        }

        internal ITypeDefinition GetTypeDefinition(int token)
        {
            // The token must refer to a TypeDef row since we are
            // only handling indexes into the full metadata (in EnC)
            // for def tables. Other tables contain deltas only.
            return GetTypeDef(MetadataTokens.TypeDefinitionHandle(token));
        }

        internal IMethodDefinition GetMethodDefinition(int token)
        {
            // Must be a def table. (See comment in GetTypeDefinition.)
            return GetMethodDef(MetadataTokens.MethodDefinitionHandle(token));
        }

        internal INestedTypeReference GetNestedTypeReference(int token)
        {
            // Must be a def table. (See comment in GetTypeDefinition.)
            return GetTypeDef(MetadataTokens.TypeDefinitionHandle(token)).AsNestedTypeReference;
        }

        internal BlobHandle GetTypeSpecSignatureIndex(ITypeReference typeReference)
        {
            BlobHandle result;
            if (_typeSpecSignatureIndex.TryGetValue(typeReference, out result))
            {
                return result;
            }

            var builder = PooledBlobBuilder.GetInstance();
            this.SerializeTypeReference(new BlobEncoder(builder).TypeSpecificationSignature(), typeReference);
            result = metadata.GetOrAddBlob(builder);

            _typeSpecSignatureIndex.Add(typeReference, result);
            builder.Free();
            return result;
        }

        internal EntityHandle GetTypeHandle(ITypeReference typeReference, bool treatRefAsPotentialTypeSpec = true)
        {
            TypeDefinitionHandle handle;
            var typeDefinition = typeReference.AsTypeDefinition(this.Context);
            if (typeDefinition != null && this.TryGetTypeDefinitionHandle(typeDefinition, out handle))
            {
                return handle;
            }

            return treatRefAsPotentialTypeSpec && typeReference.IsTypeSpecification()
                ? (EntityHandle)GetTypeSpecificationHandle(typeReference)
                : GetTypeReferenceHandle(typeReference);
        }

        internal EntityHandle GetDefinitionHandle(IDefinition definition)
        {
            return definition switch
            {
                ITypeDefinition typeDef => (EntityHandle)GetTypeDefinitionHandle(typeDef),
                IMethodDefinition methodDef => GetMethodDefinitionHandle(methodDef),
                IFieldDefinition fieldDef => GetFieldDefinitionHandle(fieldDef),
                IEventDefinition eventDef => GetEventDefinitionHandle(eventDef),
                IPropertyDefinition propertyDef => GetPropertyDefIndex(propertyDef),
                _ => throw ExceptionUtilities.Unreachable
            };
        }

        public void WriteMetadataAndIL(PdbWriter nativePdbWriterOpt, Stream metadataStream, Stream ilStream, Stream portablePdbStreamOpt, out MetadataSizes metadataSizes)
        {
            Debug.Assert(nativePdbWriterOpt == null ^ portablePdbStreamOpt == null);

            nativePdbWriterOpt?.SetMetadataEmitter(this);

            // TODO: we can precalculate the exact size of IL stream
            var ilBuilder = new BlobBuilder(1024);
            var metadataBuilder = new BlobBuilder(4 * 1024);
            var mappedFieldDataBuilder = new BlobBuilder(0);
            var managedResourceDataBuilder = new BlobBuilder(0);

            // Add 4B of padding to the start of the separated IL stream, 
            // so that method RVAs, which are offsets to this stream, are never 0.
            ilBuilder.WriteUInt32(0);

            // this is used to handle edit-and-continue emit, so we should have a module
            // version ID that is imposed by the caller (the same as the previous module version ID).
            // Therefore we do not have to fill in a new module version ID in the generated metadata
            // stream.
            Debug.Assert(module.SerializationProperties.PersistentIdentifier != default(Guid));

            BuildMetadataAndIL(
                nativePdbWriterOpt,
                ilBuilder,
                mappedFieldDataBuilder,
                managedResourceDataBuilder,
                out Blob mvidFixup,
                out Blob mvidStringFixup);

            var typeSystemRowCounts = metadata.GetRowCounts();
            PopulateEncTables(typeSystemRowCounts);

            Debug.Assert(mappedFieldDataBuilder.Count == 0);
            Debug.Assert(managedResourceDataBuilder.Count == 0);
            Debug.Assert(mvidFixup.IsDefault);
            Debug.Assert(mvidStringFixup.IsDefault);

            // TODO (https://github.com/dotnet/roslyn/issues/3905):
            // InterfaceImpl table emitted by Roslyn is not compliant with ECMA spec.
            // Once fixed enable validation in DEBUG builds.
            var rootBuilder = new MetadataRootBuilder(metadata, module.SerializationProperties.TargetRuntimeVersion, suppressValidation: true);

            rootBuilder.Serialize(metadataBuilder, methodBodyStreamRva: 0, mappedFieldDataStreamRva: 0);
            metadataSizes = rootBuilder.Sizes;

            try
            {
                ilBuilder.WriteContentTo(ilStream);
                metadataBuilder.WriteContentTo(metadataStream);
            }
            catch (Exception e) when (!(e is OperationCanceledException))
            {
                throw new PeWritingException(e);
            }

            if (portablePdbStreamOpt != null)
            {
                var portablePdbBuilder = GetPortablePdbBuilder(
                    typeSystemRowCounts,
                    debugEntryPoint: default(MethodDefinitionHandle),
                    deterministicIdProviderOpt: null);

                var portablePdbBlob = new BlobBuilder();
                portablePdbBuilder.Serialize(portablePdbBlob);

                try
                {
                    portablePdbBlob.WriteContentTo(portablePdbStreamOpt);
                }
                catch (Exception e) when (!(e is OperationCanceledException))
                {
                    throw new SymUnmanagedWriterException(e.Message, e);
                }
            }
        }

        public void BuildMetadataAndIL(
            PdbWriter nativePdbWriterOpt,
            BlobBuilder ilBuilder,
            BlobBuilder mappedFieldDataBuilder,
            BlobBuilder managedResourceDataBuilder,
            out Blob mvidFixup,
            out Blob mvidStringFixup)
        {
            // Extract information from object model into tables, indices and streams
            CreateIndices();

            if (_debugMetadataOpt != null)
            {
                DefineModuleImportScope();

                if (module.SourceLinkStreamOpt != null)
                {
                    EmbedSourceLink(module.SourceLinkStreamOpt);
                }
            }

            int[] methodBodyOffsets;
            if (MetadataOnly)
            {
                methodBodyOffsets = SerializeThrowNullMethodBodies(ilBuilder);
                mvidStringFixup = default(Blob);
            }
            else
            {
                methodBodyOffsets = SerializeMethodBodies(ilBuilder, nativePdbWriterOpt, out mvidStringFixup);
            }

            _cancellationToken.ThrowIfCancellationRequested();

            // method body serialization adds Stand Alone Signatures
            _tableIndicesAreComplete = true;

            ReportReferencesToAddedSymbols();

            BlobBuilder dynamicAnalysisDataOpt = null;
            if (_dynamicAnalysisDataWriterOpt != null)
            {
                dynamicAnalysisDataOpt = new BlobBuilder();
                _dynamicAnalysisDataWriterOpt.SerializeMetadataTables(dynamicAnalysisDataOpt);
            }

            PopulateTypeSystemTables(methodBodyOffsets, mappedFieldDataBuilder, managedResourceDataBuilder, dynamicAnalysisDataOpt, out mvidFixup);
        }

        public void PopulateEncTables(ImmutableArray<int> typeSystemRowCounts)
        {
            Debug.Assert(typeSystemRowCounts[(int)TableIndex.EncLog] == 0);
            Debug.Assert(typeSystemRowCounts[(int)TableIndex.EncMap] == 0);

            PopulateEncLogTableRows(typeSystemRowCounts);
            PopulateEncMapTableRows(typeSystemRowCounts);
        }

        public MetadataRootBuilder GetRootBuilder()
        {
            // TODO (https://github.com/dotnet/roslyn/issues/3905):
            // InterfaceImpl table emitted by Roslyn is not compliant with ECMA spec.
            // Once fixed enable validation in DEBUG builds.
            return new MetadataRootBuilder(metadata, module.SerializationProperties.TargetRuntimeVersion, suppressValidation: true);
        }

        public PortablePdbBuilder GetPortablePdbBuilder(ImmutableArray<int> typeSystemRowCounts, MethodDefinitionHandle debugEntryPoint, Func<IEnumerable<Blob>, BlobContentId> deterministicIdProviderOpt)
        {
            return new PortablePdbBuilder(_debugMetadataOpt, typeSystemRowCounts, debugEntryPoint, deterministicIdProviderOpt);
        }

        internal void GetEntryPoints(out MethodDefinitionHandle entryPointHandle, out MethodDefinitionHandle debugEntryPointHandle)
        {
            if (IsFullMetadata && !MetadataOnly)
            {
                // PE entry point is set for executable programs
                IMethodReference entryPoint = module.PEEntryPoint;
                entryPointHandle = entryPoint != null ? (MethodDefinitionHandle)GetMethodHandle((IMethodDefinition)entryPoint.AsDefinition(Context)) : default(MethodDefinitionHandle);

                // debug entry point may be different from PE entry point, it may also be set for libraries
                IMethodReference debugEntryPoint = module.DebugEntryPoint;
                if (debugEntryPoint != null && debugEntryPoint != entryPoint)
                {
                    debugEntryPointHandle = (MethodDefinitionHandle)GetMethodHandle((IMethodDefinition)debugEntryPoint.AsDefinition(Context));
                }
                else
                {
                    debugEntryPointHandle = entryPointHandle;
                }
            }
            else
            {
                entryPointHandle = debugEntryPointHandle = default(MethodDefinitionHandle);
            }
        }

        private ImmutableArray<IGenericParameter> GetSortedGenericParameters()
        {
            return GetGenericParameters().OrderBy((x, y) =>
            {
                // Spec: GenericParam table is sorted by Owner and then by Number.
                int result = CodedIndex.TypeOrMethodDef(GetDeclaringTypeOrMethodHandle(x)) - CodedIndex.TypeOrMethodDef(GetDeclaringTypeOrMethodHandle(y));
                if (result != 0)
                {
                    return result;
                }

                return x.Index - y.Index;
            }).ToImmutableArray();
        }

        private void PopulateTypeSystemTables(int[] methodBodyOffsets, BlobBuilder mappedFieldDataWriter, BlobBuilder resourceWriter, BlobBuilder dynamicAnalysisDataOpt, out Blob mvidFixup)
        {
            var sortedGenericParameters = GetSortedGenericParameters();

            this.PopulateAssemblyRefTableRows();
            this.PopulateAssemblyTableRows();
            this.PopulateClassLayoutTableRows();
            this.PopulateConstantTableRows();
            this.PopulateDeclSecurityTableRows();
            this.PopulateEventMapTableRows();
            this.PopulateEventTableRows();
            this.PopulateExportedTypeTableRows();
            this.PopulateFieldLayoutTableRows();
            this.PopulateFieldMarshalTableRows();
            this.PopulateFieldRvaTableRows(mappedFieldDataWriter);
            this.PopulateFieldTableRows();
            this.PopulateFileTableRows();
            this.PopulateGenericParameters(sortedGenericParameters);
            this.PopulateImplMapTableRows();
            this.PopulateInterfaceImplTableRows();
            this.PopulateManifestResourceTableRows(resourceWriter, dynamicAnalysisDataOpt);
            this.PopulateMemberRefTableRows();
            this.PopulateMethodImplTableRows();
            this.PopulateMethodTableRows(methodBodyOffsets);
            this.PopulateMethodSemanticsTableRows();
            this.PopulateMethodSpecTableRows();
            this.PopulateModuleRefTableRows();
            this.PopulateModuleTableRow(out mvidFixup);
            this.PopulateNestedClassTableRows();
            this.PopulateParamTableRows();
            this.PopulatePropertyMapTableRows();
            this.PopulatePropertyTableRows();
            this.PopulateTypeDefTableRows();
            this.PopulateTypeRefTableRows();
            this.PopulateTypeSpecTableRows();
            this.PopulateStandaloneSignatures();

            // This table is populated after the others because it depends on the order of the entries of the generic parameter table.
            this.PopulateCustomAttributeTableRows(sortedGenericParameters);
        }

        private void PopulateAssemblyRefTableRows()
        {
            var assemblyRefs = this.GetAssemblyRefs();
            metadata.SetCapacity(TableIndex.AssemblyRef, assemblyRefs.Count);

            foreach (var identity in assemblyRefs)
            {
                // reference has token, not full public key
                metadata.AddAssemblyReference(
                    name: GetStringHandleForPathAndCheckLength(identity.Name),
                    version: identity.Version,
                    culture: metadata.GetOrAddString(identity.CultureName),
                    publicKeyOrToken: metadata.GetOrAddBlob(identity.PublicKeyToken),
                    flags: (AssemblyFlags)((int)identity.ContentType << 9) | (identity.IsRetargetable ? AssemblyFlags.Retargetable : 0),
                    hashValue: default(BlobHandle));
            }
        }

        private void PopulateAssemblyTableRows()
        {
            if (!EmitAssemblyDefinition)
            {
                return;
            }

            var sourceAssembly = module.SourceAssemblyOpt;
            Debug.Assert(sourceAssembly != null);

            var flags = sourceAssembly.AssemblyFlags & ~AssemblyFlags.PublicKey;

            if (!sourceAssembly.Identity.PublicKey.IsDefaultOrEmpty)
            {
                flags |= AssemblyFlags.PublicKey;
            }

            metadata.AddAssembly(
                flags: flags,
                hashAlgorithm: sourceAssembly.HashAlgorithm,
                version: sourceAssembly.Identity.Version,
                publicKey: metadata.GetOrAddBlob(sourceAssembly.Identity.PublicKey),
                name: GetStringHandleForPathAndCheckLength(module.Name, module),
                culture: metadata.GetOrAddString(sourceAssembly.Identity.CultureName));
        }

        private void PopulateCustomAttributeTableRows(ImmutableArray<IGenericParameter> sortedGenericParameters)
        {
            if (this.IsFullMetadata)
            {
                this.AddAssemblyAttributesToTable();
            }

            this.AddCustomAttributesToTable(GetMethodDefs(), def => GetMethodDefinitionHandle(def));
            this.AddCustomAttributesToTable(GetFieldDefs(), def => GetFieldDefinitionHandle(def));

            // this.AddCustomAttributesToTable(this.typeRefList, 2);
            var typeDefs = GetTypeDefs();
            this.AddCustomAttributesToTable(typeDefs, def => GetTypeDefinitionHandle(def));
            this.AddCustomAttributesToTable(GetParameterDefs(), def => GetParameterHandle(def));

            // TODO: attributes on member reference entries 6
            if (this.IsFullMetadata)
            {
                this.AddModuleAttributesToTable(module);
            }

            // TODO: declarative security entries 8
            this.AddCustomAttributesToTable(GetPropertyDefs(), def => GetPropertyDefIndex(def));
            this.AddCustomAttributesToTable(GetEventDefs(), def => GetEventDefinitionHandle(def));

            // TODO: standalone signature entries 11

            // TODO: type spec entries 13
            // this.AddCustomAttributesToTable(this.module.AssemblyReferences, 15);
            // TODO: this.AddCustomAttributesToTable(assembly.Files, 16);
            // TODO: exported types 17
            // TODO: this.AddCustomAttributesToTable(assembly.Resources, 18);

            this.AddCustomAttributesToTable(sortedGenericParameters, TableIndex.GenericParam);
        }

        private void AddAssemblyAttributesToTable()
        {
            bool writingNetModule = module.OutputKind == OutputKind.NetModule;
            if (writingNetModule)
            {
                // When writing netmodules, assembly security attributes are not emitted by PopulateDeclSecurityTableRows().
                // Instead, here we make sure they are emitted as regular attributes, attached off the appropriate placeholder
                // System.Runtime.CompilerServices.AssemblyAttributesGoHere* type refs.  This is the contract for publishing
                // assembly attributes in netmodules so they may be migrated to containing/referencing multi-module assemblies,
                // at multi-module assembly build time.
                AddAssemblyAttributesToTable(
                    this.module.GetSourceAssemblySecurityAttributes().Select(sa => sa.Attribute),
                    needsDummyParent: true,
                    isSecurity: true);
            }

            AddAssemblyAttributesToTable(
                this.module.GetSourceAssemblyAttributes(Context.IsRefAssembly),
                needsDummyParent: writingNetModule,
                isSecurity: false);
        }

        private void AddAssemblyAttributesToTable(IEnumerable<ICustomAttribute> assemblyAttributes, bool needsDummyParent, bool isSecurity)
        {
            Debug.Assert(this.IsFullMetadata); // parentToken is not relative
            EntityHandle parentHandle = Handle.AssemblyDefinition;
            foreach (ICustomAttribute customAttribute in assemblyAttributes)
            {
                if (needsDummyParent)
                {
                    // When writing netmodules, assembly attributes are attached off the appropriate placeholder
                    // System.Runtime.CompilerServices.AssemblyAttributesGoHere* type refs.  This is the contract for publishing
                    // assembly attributes in netmodules so they may be migrated to containing/referencing multi-module assemblies,
                    // at multi-module assembly build time.
                    parentHandle = GetDummyAssemblyAttributeParent(isSecurity, customAttribute.AllowMultiple);
                }

                AddCustomAttributeToTable(parentHandle, customAttribute);
            }
        }

        private TypeReferenceHandle GetDummyAssemblyAttributeParent(bool isSecurity, bool allowMultiple)
        {
            // Lazily get or create placeholder assembly attribute parent type ref for the given combination of
            // whether isSecurity and allowMultiple.  Convert type ref row id to corresponding attribute parent tag.
            // Note that according to the defacto contract, although the placeholder type refs have CorLibrary as their
            // resolution scope, the types backing the placeholder type refs need not actually exist.
            int iS = isSecurity ? 1 : 0;
            int iM = allowMultiple ? 1 : 0;
            if (_dummyAssemblyAttributeParent[iS, iM].IsNil)
            {
                _dummyAssemblyAttributeParent[iS, iM] = metadata.AddTypeReference(
                    resolutionScope: GetResolutionScopeHandle(module.GetCorLibrary(Context)),
                    @namespace: metadata.GetOrAddString(dummyAssemblyAttributeParentNamespace),
                    name: metadata.GetOrAddString(dummyAssemblyAttributeParentName + dummyAssemblyAttributeParentQualifier[iS, iM]));
            }

            return _dummyAssemblyAttributeParent[iS, iM];
        }

        private void AddModuleAttributesToTable(CommonPEModuleBuilder module)
        {
            Debug.Assert(this.IsFullMetadata);
            foreach (ICustomAttribute customAttribute in module.GetSourceModuleAttributes())
            {
                AddCustomAttributeToTable(EntityHandle.ModuleDefinition, customAttribute);
            }
        }

        private void AddCustomAttributesToTable<T>(IEnumerable<T> parentList, TableIndex tableIndex)
            where T : IReference
        {
            int parentRowId = 1;
            foreach (var parent in parentList)
            {
                var parentHandle = MetadataTokens.Handle(tableIndex, parentRowId++);
                foreach (ICustomAttribute customAttribute in parent.GetAttributes(Context))
                {
                    AddCustomAttributeToTable(parentHandle, customAttribute);
                }
            }
        }

        private void AddCustomAttributesToTable<T>(IEnumerable<T> parentList, Func<T, EntityHandle> getDefinitionHandle)
            where T : IReference
        {
            foreach (var parent in parentList)
            {
                EntityHandle parentHandle = getDefinitionHandle(parent);
                foreach (ICustomAttribute customAttribute in parent.GetAttributes(Context))
                {
                    AddCustomAttributeToTable(parentHandle, customAttribute);
                }
            }
        }

        private void AddCustomAttributesToTable(
            EntityHandle handle,
            ImmutableArray<ICustomAttribute> attributes)
        {
            foreach (var attr in attributes)
            {
                AddCustomAttributeToTable(handle, attr);
            }
        }

        private void AddCustomAttributesToTable(IEnumerable<TypeReferenceWithAttributes> typeRefsWithAttributes)
        {
            foreach (var typeRefWithAttributes in typeRefsWithAttributes)
            {
                var ifaceHandle = GetTypeHandle(typeRefWithAttributes.TypeRef);
                foreach (var customAttribute in typeRefWithAttributes.Attributes)
                {
                    AddCustomAttributeToTable(ifaceHandle, customAttribute);
                }
            }
        }

        private void AddCustomAttributeToTable(EntityHandle parentHandle, ICustomAttribute customAttribute)
        {
            IMethodReference constructor = customAttribute.Constructor(Context, reportDiagnostics: true);

            if (constructor != null)
            {
                metadata.AddCustomAttribute(
                    parent: parentHandle,
                    constructor: GetCustomAttributeTypeCodedIndex(constructor),
                    value: GetCustomAttributeSignatureIndex(customAttribute));
            }
        }

        private void PopulateDeclSecurityTableRows()
        {
            if (module.OutputKind != OutputKind.NetModule)
            {
                this.PopulateDeclSecurityTableRowsFor(EntityHandle.AssemblyDefinition, module.GetSourceAssemblySecurityAttributes());
            }

            foreach (ITypeDefinition typeDef in this.GetTypeDefs())
            {
                if (!typeDef.HasDeclarativeSecurity)
                {
                    continue;
                }

                this.PopulateDeclSecurityTableRowsFor(GetTypeDefinitionHandle(typeDef), typeDef.SecurityAttributes);
            }

            foreach (IMethodDefinition methodDef in this.GetMethodDefs())
            {
                if (!methodDef.HasDeclarativeSecurity)
                {
                    continue;
                }

                this.PopulateDeclSecurityTableRowsFor(GetMethodDefinitionHandle(methodDef), methodDef.SecurityAttributes);
            }
        }

        private void PopulateDeclSecurityTableRowsFor(EntityHandle parentHandle, IEnumerable<SecurityAttribute> attributes)
        {
            OrderPreservingMultiDictionary<DeclarativeSecurityAction, ICustomAttribute> groupedSecurityAttributes = null;

            foreach (SecurityAttribute securityAttribute in attributes)
            {
                groupedSecurityAttributes = groupedSecurityAttributes ?? OrderPreservingMultiDictionary<DeclarativeSecurityAction, ICustomAttribute>.GetInstance();
                groupedSecurityAttributes.Add(securityAttribute.Action, securityAttribute.Attribute);
            }

            if (groupedSecurityAttributes == null)
            {
                return;
            }

            foreach (DeclarativeSecurityAction securityAction in groupedSecurityAttributes.Keys)
            {
                metadata.AddDeclarativeSecurityAttribute(
                    parent: parentHandle,
                    action: securityAction,
                    permissionSet: GetPermissionSetBlobHandle(groupedSecurityAttributes[securityAction]));
            }

            groupedSecurityAttributes.Free();
        }

        private void PopulateEventTableRows()
        {
            var eventDefs = this.GetEventDefs();
            metadata.SetCapacity(TableIndex.Event, eventDefs.Count);

            foreach (IEventDefinition eventDef in eventDefs)
            {
                metadata.AddEvent(
                    attributes: GetEventAttributes(eventDef),
                    name: GetStringHandleForNameAndCheckLength(eventDef.Name, eventDef),
                    type: GetTypeHandle(eventDef.GetType(Context)));
            }
        }

        private void PopulateExportedTypeTableRows()
        {
            if (!IsFullMetadata)
            {
                return;
            }

            var exportedTypes = module.GetExportedTypes(Context.Diagnostics);
            if (exportedTypes.Length == 0)
            {
                return;
            }

            metadata.SetCapacity(TableIndex.ExportedType, exportedTypes.Length);

            foreach (var exportedType in exportedTypes)
            {
                INestedTypeReference nestedRef;
                INamespaceTypeReference namespaceTypeRef;
                TypeAttributes attributes;
                StringHandle typeName;
                StringHandle typeNamespace;
                EntityHandle implementation;

                if ((namespaceTypeRef = exportedType.Type.AsNamespaceTypeReference) != null)
                {
                    string mangledTypeName = GetMangledName(namespaceTypeRef);
                    typeName = GetStringHandleForNameAndCheckLength(mangledTypeName, namespaceTypeRef);
                    typeNamespace = GetStringHandleForNamespaceAndCheckLength(namespaceTypeRef, mangledTypeName);
                    implementation = GetExportedTypeImplementation(namespaceTypeRef);
                    attributes = exportedType.IsForwarder ? TypeAttributes.NotPublic | Constants.TypeAttributes_TypeForwarder : TypeAttributes.Public;
                }
                else if ((nestedRef = exportedType.Type.AsNestedTypeReference) != null)
                {
                    Debug.Assert(exportedType.ParentIndex != -1);

                    typeName = GetStringHandleForNameAndCheckLength(GetMangledName(nestedRef), nestedRef);
                    typeNamespace = default(StringHandle);
                    implementation = MetadataTokens.ExportedTypeHandle(exportedType.ParentIndex + 1);
                    attributes = exportedType.IsForwarder ? TypeAttributes.NotPublic : TypeAttributes.NestedPublic;
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue(exportedType);
                }

                metadata.AddExportedType(
                    attributes: attributes,
                    @namespace: typeNamespace,
                    name: typeName,
                    implementation: implementation,
                    typeDefinitionId: exportedType.IsForwarder ? 0 : MetadataTokens.GetToken(exportedType.Type.TypeDef));
            }
        }

        private void PopulateFieldLayoutTableRows()
        {
            foreach (IFieldDefinition fieldDef in this.GetFieldDefs())
            {
                if (fieldDef.ContainingTypeDefinition.Layout != LayoutKind.Explicit || fieldDef.IsStatic)
                {
                    continue;
                }

                metadata.AddFieldLayout(
                    field: GetFieldDefinitionHandle(fieldDef),
                    offset: fieldDef.Offset);
            }
        }

        private void PopulateFieldMarshalTableRows()
        {
            foreach (IFieldDefinition fieldDef in this.GetFieldDefs())
            {
                if (!fieldDef.IsMarshalledExplicitly)
                {
                    continue;
                }

                var marshallingInformation = fieldDef.MarshallingInformation;

                BlobHandle descriptor = (marshallingInformation != null)
                    ? GetMarshallingDescriptorHandle(marshallingInformation)
                    : GetMarshallingDescriptorHandle(fieldDef.MarshallingDescriptor);

                metadata.AddMarshallingDescriptor(
                    parent: GetFieldDefinitionHandle(fieldDef),
                    descriptor: descriptor);
            }

            foreach (IParameterDefinition parDef in this.GetParameterDefs())
            {
                if (!parDef.IsMarshalledExplicitly)
                {
                    continue;
                }

                var marshallingInformation = parDef.MarshallingInformation;

                BlobHandle descriptor = (marshallingInformation != null)
                     ? GetMarshallingDescriptorHandle(marshallingInformation)
                     : GetMarshallingDescriptorHandle(parDef.MarshallingDescriptor);

                metadata.AddMarshallingDescriptor(
                    parent: GetParameterHandle(parDef),
                    descriptor: descriptor);
            }
        }

        private void PopulateFieldRvaTableRows(BlobBuilder mappedFieldDataWriter)
        {
            foreach (IFieldDefinition fieldDef in this.GetFieldDefs())
            {
                if (fieldDef.MappedData.IsDefault)
                {
                    continue;
                }

                int offset = mappedFieldDataWriter.Count;
                mappedFieldDataWriter.WriteBytes(fieldDef.MappedData);
                mappedFieldDataWriter.Align(ManagedPEBuilder.MappedFieldDataAlignment);

                metadata.AddFieldRelativeVirtualAddress(
                    field: GetFieldDefinitionHandle(fieldDef),
                    offset: offset);
            }
        }

        private void PopulateFieldTableRows()
        {
            var fieldDefs = this.GetFieldDefs();
            metadata.SetCapacity(TableIndex.Field, fieldDefs.Count);

            foreach (IFieldDefinition fieldDef in fieldDefs)
            {
                if (fieldDef.IsContextualNamedEntity)
                {
                    ((IContextualNamedEntity)fieldDef).AssociateWithMetadataWriter(this);
                }

                metadata.AddFieldDefinition(
                    attributes: GetFieldAttributes(fieldDef),
                    name: GetStringHandleForNameAndCheckLength(fieldDef.Name, fieldDef),
                    signature: GetFieldSignatureIndex(fieldDef));
            }
        }

        private void PopulateConstantTableRows()
        {
            foreach (IFieldDefinition fieldDef in this.GetFieldDefs())
            {
                var constant = fieldDef.GetCompileTimeValue(Context);
                if (constant == null)
                {
                    continue;
                }

                metadata.AddConstant(
                    parent: GetFieldDefinitionHandle(fieldDef),
                    value: constant.Value);
            }

            foreach (IParameterDefinition parDef in this.GetParameterDefs())
            {
                var defaultValue = parDef.GetDefaultValue(Context);
                if (defaultValue == null)
                {
                    continue;
                }

                metadata.AddConstant(
                    parent: GetParameterHandle(parDef),
                    value: defaultValue.Value);
            }

            foreach (IPropertyDefinition propDef in this.GetPropertyDefs())
            {
                if (!propDef.HasDefaultValue)
                {
                    continue;
                }

                metadata.AddConstant(
                    parent: GetPropertyDefIndex(propDef),
                    value: propDef.DefaultValue.Value);
            }
        }

        private void PopulateFileTableRows()
        {
            ISourceAssemblySymbolInternal assembly = module.SourceAssemblyOpt;
            if (assembly == null)
            {
                return;
            }

            var hashAlgorithm = assembly.HashAlgorithm;
            metadata.SetCapacity(TableIndex.File, _fileRefList.Count);

            foreach (IFileReference fileReference in _fileRefList)
            {
                metadata.AddAssemblyFile(
                    name: GetStringHandleForPathAndCheckLength(fileReference.FileName),
                    hashValue: metadata.GetOrAddBlob(fileReference.GetHashValue(hashAlgorithm)),
                    containsMetadata: fileReference.HasMetadata);
            }
        }


        private void PopulateGenericParameters(
            ImmutableArray<IGenericParameter> sortedGenericParameters)
        {
            foreach (IGenericParameter genericParameter in sortedGenericParameters)
            {
                // CONSIDER: The CLI spec doesn't mention a restriction on the Name column of the GenericParam table,
                // but they go in the same string heap as all the other declaration names, so it stands to reason that
                // they should be restricted in the same way.
                var genericParameterHandle = metadata.AddGenericParameter(
                    parent: GetDeclaringTypeOrMethodHandle(genericParameter),
                    attributes: GetGenericParameterAttributes(genericParameter),
                    name: GetStringHandleForNameAndCheckLength(genericParameter.Name, genericParameter),
                    index: genericParameter.Index);

                foreach (var refWithAttributes in genericParameter.GetConstraints(Context))
                {
                    var genericConstraintHandle = metadata.AddGenericParameterConstraint(
                        genericParameter: genericParameterHandle,
                        constraint: GetTypeHandle(refWithAttributes.TypeRef));
                    AddCustomAttributesToTable(genericConstraintHandle, refWithAttributes.Attributes);
                }
            }
        }

        private void PopulateImplMapTableRows()
        {
            foreach (IMethodDefinition methodDef in this.GetMethodDefs())
            {
                if (!methodDef.IsPlatformInvoke)
                {
                    continue;
                }

                var data = methodDef.PlatformInvokeData;
                string entryPointName = data.EntryPointName;

                StringHandle importName = (entryPointName != null)
                    ? GetStringHandleForNameAndCheckLength(entryPointName, methodDef)
                    : metadata.GetOrAddString(methodDef.Name); // Length checked while populating the method def table.

                metadata.AddMethodImport(
                    method: GetMethodDefinitionHandle(methodDef),
                    attributes: data.Flags,
                    name: importName,
                    module: GetModuleReferenceHandle(data.ModuleName));
            }
        }

        private void PopulateInterfaceImplTableRows()
        {
            foreach (ITypeDefinition typeDef in this.GetTypeDefs())
            {
                var typeDefHandle = GetTypeDefinitionHandle(typeDef);
                foreach (var interfaceImpl in typeDef.Interfaces(Context))
                {
                    var handle = metadata.AddInterfaceImplementation(
                        type: typeDefHandle,
                        implementedInterface: GetTypeHandle(interfaceImpl.TypeRef));
                    AddCustomAttributesToTable(handle, interfaceImpl.Attributes);
                }
            }
        }

        private void PopulateManifestResourceTableRows(BlobBuilder resourceDataWriter, BlobBuilder dynamicAnalysisDataOpt)
        {
            if (dynamicAnalysisDataOpt != null)
            {
                metadata.AddManifestResource(
                    attributes: ManifestResourceAttributes.Private,
                    name: metadata.GetOrAddString("<DynamicAnalysisData>"),
                    implementation: default(EntityHandle),
                    offset: GetManagedResourceOffset(dynamicAnalysisDataOpt, resourceDataWriter)
                );
            }

            foreach (var resource in this.module.GetResources(Context))
            {
                EntityHandle implementation;
                if (resource.ExternalFile != null)
                {
                    // Length checked on insertion into the file table.
                    implementation = GetAssemblyFileHandle(resource.ExternalFile);
                }
                else
                {
                    // This is an embedded resource, we don't support references to resources from referenced assemblies.
                    implementation = default(EntityHandle);
                }

                metadata.AddManifestResource(
                    attributes: resource.IsPublic ? ManifestResourceAttributes.Public : ManifestResourceAttributes.Private,
                    name: GetStringHandleForNameAndCheckLength(resource.Name),
                    implementation: implementation,
                    offset: GetManagedResourceOffset(resource, resourceDataWriter));
            }

            // the stream should be aligned:
            Debug.Assert((resourceDataWriter.Count % ManagedPEBuilder.ManagedResourcesDataAlignment) == 0);
        }

        private void PopulateMemberRefTableRows()
        {
            var memberRefs = this.GetMemberRefs();
            metadata.SetCapacity(TableIndex.MemberRef, memberRefs.Count);

            foreach (ITypeMemberReference memberRef in memberRefs)
            {
                metadata.AddMemberReference(
                    parent: GetMemberReferenceParent(memberRef),
                    name: GetStringHandleForNameAndCheckLength(memberRef.Name, memberRef),
                    signature: GetMemberReferenceSignatureHandle(memberRef));
            }
        }

        private void PopulateMethodImplTableRows()
        {
            metadata.SetCapacity(TableIndex.MethodImpl, methodImplList.Count);

            foreach (MethodImplementation methodImplementation in this.methodImplList)
            {
                metadata.AddMethodImplementation(
                    type: GetTypeDefinitionHandle(methodImplementation.ContainingType),
                    methodBody: GetMethodDefinitionOrReferenceHandle(methodImplementation.ImplementingMethod),
                    methodDeclaration: GetMethodDefinitionOrReferenceHandle(methodImplementation.ImplementedMethod));
            }
        }

        private void PopulateMethodSpecTableRows()
        {
            var methodSpecs = this.GetMethodSpecs();
            metadata.SetCapacity(TableIndex.MethodSpec, methodSpecs.Count);

            foreach (IGenericMethodInstanceReference genericMethodInstanceReference in methodSpecs)
            {
                metadata.AddMethodSpecification(
                    method: GetMethodDefinitionOrReferenceHandle(genericMethodInstanceReference.GetGenericMethod(Context)),
                    instantiation: GetMethodSpecificationBlobHandle(genericMethodInstanceReference));
            }
        }

        private void PopulateMethodTableRows(int[] methodBodyOffsets)
        {
            var methodDefs = this.GetMethodDefs();
            metadata.SetCapacity(TableIndex.MethodDef, methodDefs.Count);

            int i = 0;
            foreach (IMethodDefinition methodDef in methodDefs)
            {
                metadata.AddMethodDefinition(
                    attributes: GetMethodAttributes(methodDef),
                    implAttributes: methodDef.GetImplementationAttributes(Context),
                    name: GetStringHandleForNameAndCheckLength(methodDef.Name, methodDef),
                    signature: GetMethodSignatureHandle(methodDef),
                    bodyOffset: methodBodyOffsets[i],
                    parameterList: GetFirstParameterHandle(methodDef));

                i++;
            }
        }

        private void PopulateMethodSemanticsTableRows()
        {
            var propertyDefs = this.GetPropertyDefs();
            var eventDefs = this.GetEventDefs();

            // an estimate, not necessarily accurate.
            metadata.SetCapacity(TableIndex.MethodSemantics, propertyDefs.Count * 2 + eventDefs.Count * 2);

            foreach (IPropertyDefinition propertyDef in this.GetPropertyDefs())
            {
                var association = GetPropertyDefIndex(propertyDef);
                foreach (IMethodReference accessorMethod in propertyDef.GetAccessors(Context))
                {
                    MethodSemanticsAttributes semantics;
                    if (accessorMethod == propertyDef.Setter)
                    {
                        semantics = MethodSemanticsAttributes.Setter;
                    }
                    else if (accessorMethod == propertyDef.Getter)
                    {
                        semantics = MethodSemanticsAttributes.Getter;
                    }
                    else
                    {
                        semantics = MethodSemanticsAttributes.Other;
                    }

                    metadata.AddMethodSemantics(
                        association: association,
                        semantics: semantics,
                        methodDefinition: GetMethodDefinitionHandle(accessorMethod.GetResolvedMethod(Context)));
                }
            }

            foreach (IEventDefinition eventDef in this.GetEventDefs())
            {
                var association = GetEventDefinitionHandle(eventDef);
                foreach (IMethodReference accessorMethod in eventDef.GetAccessors(Context))
                {
                    MethodSemanticsAttributes semantics;
                    if (accessorMethod == eventDef.Adder)
                    {
                        semantics = MethodSemanticsAttributes.Adder;
                    }
                    else if (accessorMethod == eventDef.Remover)
                    {
                        semantics = MethodSemanticsAttributes.Remover;
                    }
                    else if (accessorMethod == eventDef.Caller)
                    {
                        semantics = MethodSemanticsAttributes.Raiser;
                    }
                    else
                    {
                        semantics = MethodSemanticsAttributes.Other;
                    }

                    metadata.AddMethodSemantics(
                        association: association,
                        semantics: semantics,
                        methodDefinition: GetMethodDefinitionHandle(accessorMethod.GetResolvedMethod(Context)));
                }
            }
        }

        private void PopulateModuleRefTableRows()
        {
            var moduleRefs = this.GetModuleRefs();
            metadata.SetCapacity(TableIndex.ModuleRef, moduleRefs.Count);

            foreach (string moduleName in moduleRefs)
            {
                metadata.AddModuleReference(GetStringHandleForPathAndCheckLength(moduleName));
            }
        }

        private void PopulateModuleTableRow(out Blob mvidFixup)
        {
            CheckPathLength(this.module.ModuleName);

            GuidHandle mvidHandle;
            Guid mvid = this.module.SerializationProperties.PersistentIdentifier;
            if (mvid != default(Guid))
            {
                // MVID is specified upfront when emitting EnC delta:
                mvidHandle = metadata.GetOrAddGuid(mvid);
                mvidFixup = default(Blob);
            }
            else
            {
                // The guid will be filled in later:
                var reservedGuid = metadata.ReserveGuid();
                mvidFixup = reservedGuid.Content;
                mvidHandle = reservedGuid.Handle;
                reservedGuid.CreateWriter().WriteBytes(0, mvidFixup.Length);
            }

            metadata.AddModule(
                generation: this.Generation,
                moduleName: metadata.GetOrAddString(this.module.ModuleName),
                mvid: mvidHandle,
                encId: metadata.GetOrAddGuid(EncId),
                encBaseId: metadata.GetOrAddGuid(EncBaseId));
        }

        private void PopulateParamTableRows()
        {
            var parameterDefs = this.GetParameterDefs();
            metadata.SetCapacity(TableIndex.Param, parameterDefs.Count);

            foreach (IParameterDefinition parDef in parameterDefs)
            {
                metadata.AddParameter(
                    attributes: GetParameterAttributes(parDef),
                    sequenceNumber: (parDef is ReturnValueParameter) ? 0 : parDef.Index + 1,
                    name: GetStringHandleForNameAndCheckLength(parDef.Name, parDef));
            }
        }

        private void PopulatePropertyTableRows()
        {
            var propertyDefs = this.GetPropertyDefs();
            metadata.SetCapacity(TableIndex.Property, propertyDefs.Count);

            foreach (IPropertyDefinition propertyDef in propertyDefs)
            {
                metadata.AddProperty(
                    attributes: GetPropertyAttributes(propertyDef),
                    name: GetStringHandleForNameAndCheckLength(propertyDef.Name, propertyDef),
                    signature: GetPropertySignatureHandle(propertyDef));
            }
        }

        private void PopulateTypeDefTableRows()
        {
            var typeDefs = this.GetTypeDefs();
            metadata.SetCapacity(TableIndex.TypeDef, typeDefs.Count);

            foreach (INamedTypeDefinition typeDef in typeDefs)
            {
                INamespaceTypeDefinition namespaceType = typeDef.AsNamespaceTypeDefinition(Context);
                string mangledTypeName = GetMangledName(typeDef);
                ITypeReference baseType = typeDef.GetBaseClass(Context);

                metadata.AddTypeDefinition(
                    attributes: GetTypeAttributes(typeDef),
                    @namespace: (namespaceType != null) ? GetStringHandleForNamespaceAndCheckLength(namespaceType, mangledTypeName) : default(StringHandle),
                    name: GetStringHandleForNameAndCheckLength(mangledTypeName, typeDef),
                    baseType: (baseType != null) ? GetTypeHandle(baseType) : default(EntityHandle),
                    fieldList: GetFirstFieldDefinitionHandle(typeDef),
                    methodList: GetFirstMethodDefinitionHandle(typeDef));
            }
        }

        private void PopulateNestedClassTableRows()
        {
            foreach (ITypeDefinition typeDef in this.GetTypeDefs())
            {
                INestedTypeDefinition nestedTypeDef = typeDef.AsNestedTypeDefinition(Context);
                if (nestedTypeDef == null)
                {
                    continue;
                }

                metadata.AddNestedType(
                    type: GetTypeDefinitionHandle(typeDef),
                    enclosingType: GetTypeDefinitionHandle(nestedTypeDef.ContainingTypeDefinition));
            }
        }

        private void PopulateClassLayoutTableRows()
        {
            foreach (ITypeDefinition typeDef in this.GetTypeDefs())
            {
                if (typeDef.Alignment == 0 && typeDef.SizeOf == 0)
                {
                    continue;
                }

                metadata.AddTypeLayout(
                    type: GetTypeDefinitionHandle(typeDef),
                    packingSize: typeDef.Alignment,
                    size: typeDef.SizeOf);
            }
        }

        private void PopulateTypeRefTableRows()
        {
            var typeRefs = this.GetTypeRefs();
            metadata.SetCapacity(TableIndex.TypeRef, typeRefs.Count);

            foreach (ITypeReference typeRef in typeRefs)
            {
                EntityHandle resolutionScope;
                StringHandle name;
                StringHandle @namespace;

                INestedTypeReference nestedTypeRef = typeRef.AsNestedTypeReference;
                if (nestedTypeRef != null)
                {
                    ITypeReference scopeTypeRef;

                    ISpecializedNestedTypeReference sneTypeRef = nestedTypeRef.AsSpecializedNestedTypeReference;
                    if (sneTypeRef != null)
                    {
                        scopeTypeRef = sneTypeRef.GetUnspecializedVersion(Context).GetContainingType(Context);
                    }
                    else
                    {
                        scopeTypeRef = nestedTypeRef.GetContainingType(Context);
                    }

                    resolutionScope = GetTypeReferenceHandle(scopeTypeRef);
                    name = this.GetStringHandleForNameAndCheckLength(GetMangledName(nestedTypeRef), nestedTypeRef);
                    @namespace = default(StringHandle);
                }
                else
                {
                    INamespaceTypeReference namespaceTypeRef = typeRef.AsNamespaceTypeReference;
                    if (namespaceTypeRef == null)
                    {
                        throw ExceptionUtilities.UnexpectedValue(typeRef);
                    }

                    resolutionScope = this.GetResolutionScopeHandle(namespaceTypeRef.GetUnit(Context));
                    string mangledTypeName = GetMangledName(namespaceTypeRef);
                    name = this.GetStringHandleForNameAndCheckLength(mangledTypeName, namespaceTypeRef);
                    @namespace = this.GetStringHandleForNamespaceAndCheckLength(namespaceTypeRef, mangledTypeName);
                }

                metadata.AddTypeReference(
                    resolutionScope: resolutionScope,
                    @namespace: @namespace,
                    name: name);
            }
        }

        private void PopulateTypeSpecTableRows()
        {
            var typeSpecs = this.GetTypeSpecs();
            metadata.SetCapacity(TableIndex.TypeSpec, typeSpecs.Count);

            foreach (ITypeReference typeSpec in typeSpecs)
            {
                metadata.AddTypeSpecification(GetTypeSpecSignatureIndex(typeSpec));
            }
        }

        private void PopulateStandaloneSignatures()
        {
            var signatures = GetStandaloneSignatureBlobHandles();

            foreach (BlobHandle signature in signatures)
            {
                metadata.AddStandaloneSignature(signature);
            }
        }

        private int[] SerializeThrowNullMethodBodies(BlobBuilder ilBuilder)
        {
            Debug.Assert(MetadataOnly);
            var methods = this.GetMethodDefs();
            int[] bodyOffsets = new int[methods.Count];

            int bodyOffsetCache = -1;
            int methodRid = 0;
            foreach (IMethodDefinition method in methods)
            {
                if (method.HasBody())
                {
                    if (bodyOffsetCache == -1)
                    {
                        bodyOffsetCache = ilBuilder.Count;
                        ilBuilder.WriteBytes(ThrowNullEncodedBody);
                    }
                    bodyOffsets[methodRid] = bodyOffsetCache;
                }
                else
                {
                    bodyOffsets[methodRid] = -1;
                }
                methodRid++;
            }

            return bodyOffsets;
        }

        private int[] SerializeMethodBodies(BlobBuilder ilBuilder, PdbWriter nativePdbWriterOpt, out Blob mvidStringFixup)
        {
            CustomDebugInfoWriter customDebugInfoWriter = (nativePdbWriterOpt != null) ? new CustomDebugInfoWriter(nativePdbWriterOpt) : null;

            var methods = this.GetMethodDefs();
            int[] bodyOffsets = new int[methods.Count];

            var lastLocalVariableHandle = default(LocalVariableHandle);
            var lastLocalConstantHandle = default(LocalConstantHandle);

            var encoder = new MethodBodyStreamEncoder(ilBuilder);

            var mvidStringHandle = default(UserStringHandle);
            mvidStringFixup = default(Blob);

            int methodRid = 1;
            foreach (IMethodDefinition method in methods)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                int bodyOffset;
                IMethodBody body;
                StandaloneSignatureHandle localSignatureHandleOpt;

                if (method.HasBody())
                {
                    body = method.GetBody(Context);

                    if (body != null)
                    {
                        localSignatureHandleOpt = this.SerializeLocalVariablesSignature(body);

                        // TODO: consider parallelizing these (local signature tokens can be piped into IL serialization & debug info generation)
                        bodyOffset = SerializeMethodBody(encoder, body, localSignatureHandleOpt, ref mvidStringHandle, ref mvidStringFixup);

                        nativePdbWriterOpt?.SerializeDebugInfo(body, localSignatureHandleOpt, customDebugInfoWriter);
                    }
                    else
                    {
                        bodyOffset = 0;
                        localSignatureHandleOpt = default(StandaloneSignatureHandle);
                    }
                }
                else
                {
                    // 0 is actually written to metadata when the row is serialized
                    bodyOffset = -1;
                    body = null;
                    localSignatureHandleOpt = default(StandaloneSignatureHandle);
                }

                if (_debugMetadataOpt != null)
                {
                    SerializeMethodDebugInfo(body, methodRid, localSignatureHandleOpt, ref lastLocalVariableHandle, ref lastLocalConstantHandle);
                }

                _dynamicAnalysisDataWriterOpt?.SerializeMethodDynamicAnalysisData(body);

                bodyOffsets[methodRid - 1] = bodyOffset;

                methodRid++;
            }

            return bodyOffsets;
        }

        private int SerializeMethodBody(MethodBodyStreamEncoder encoder, IMethodBody methodBody, StandaloneSignatureHandle localSignatureHandleOpt, ref UserStringHandle mvidStringHandle, ref Blob mvidStringFixup)
        {
            int ilLength = methodBody.IL.Length;
            var exceptionRegions = methodBody.ExceptionRegions;
            bool isSmallBody = ilLength < 64 && methodBody.MaxStack <= 8 && localSignatureHandleOpt.IsNil && exceptionRegions.Length == 0;

            // Check if an identical method body has already been serialized.
            // If so, use the RVA of the already serialized one.
            // Note that we don't need to rewrite the fake tokens in the body before looking it up.

            // Don't do small body method caching during deterministic builds until this issue is fixed
            // https://github.com/dotnet/roslyn/issues/7595
            int bodyOffset;
            if (!_deterministic && isSmallBody && _smallMethodBodies.TryGetValue(methodBody.IL, out bodyOffset))
            {
                return bodyOffset;
            }

            var encodedBody = encoder.AddMethodBody(
                codeSize: methodBody.IL.Length,
                maxStack: methodBody.MaxStack,
                exceptionRegionCount: exceptionRegions.Length,
                hasSmallExceptionRegions: MayUseSmallExceptionHeaders(exceptionRegions),
                localVariablesSignature: localSignatureHandleOpt,
                attributes: (methodBody.LocalsAreZeroed ? MethodBodyAttributes.InitLocals : 0));

            // Don't do small body method caching during deterministic builds until this issue is fixed
            // https://github.com/dotnet/roslyn/issues/7595
            if (isSmallBody && !_deterministic)
            {
                _smallMethodBodies.Add(methodBody.IL, encodedBody.Offset);
            }

            WriteInstructions(encodedBody.Instructions, methodBody.IL, ref mvidStringHandle, ref mvidStringFixup);
            SerializeMethodBodyExceptionHandlerTable(encodedBody.ExceptionRegions, exceptionRegions);

            return encodedBody.Offset;
        }

        /// <summary>
        /// Serialize the method local signature to the blob.
        /// </summary>
        /// <returns>Standalone signature token</returns>
        protected virtual StandaloneSignatureHandle SerializeLocalVariablesSignature(IMethodBody body)
        {
            Debug.Assert(!_tableIndicesAreComplete);

            var localVariables = body.LocalVariables;
            if (localVariables.Length == 0)
            {
                return default(StandaloneSignatureHandle);
            }

            var builder = PooledBlobBuilder.GetInstance();

            var encoder = new BlobEncoder(builder).LocalVariableSignature(localVariables.Length);
            foreach (ILocalDefinition local in localVariables)
            {
                SerializeLocalVariableType(encoder.AddVariable(), local);
            }

            BlobHandle blobIndex = metadata.GetOrAddBlob(builder);

            var handle = GetOrAddStandaloneSignatureHandle(blobIndex);
            builder.Free();

            return handle;
        }

        protected void SerializeLocalVariableType(LocalVariableTypeEncoder encoder, ILocalDefinition local)
        {
            if (local.CustomModifiers.Length > 0)
            {
                SerializeCustomModifiers(encoder.CustomModifiers(), local.CustomModifiers);
            }

            if (module.IsPlatformType(local.Type, PlatformType.SystemTypedReference))
            {
                encoder.TypedReference();
                return;
            }

            SerializeTypeReference(encoder.Type(local.IsReference, local.IsPinned), local.Type);
        }

        internal StandaloneSignatureHandle SerializeLocalConstantStandAloneSignature(ILocalDefinition localConstant)
        {
            var builder = PooledBlobBuilder.GetInstance();
            var typeEncoder = new BlobEncoder(builder).FieldSignature();

            if (localConstant.CustomModifiers.Length > 0)
            {
                SerializeCustomModifiers(typeEncoder.CustomModifiers(), localConstant.CustomModifiers);
            }

            SerializeTypeReference(typeEncoder, localConstant.Type);

            BlobHandle blobIndex = metadata.GetOrAddBlob(builder);
            var signatureHandle = GetOrAddStandaloneSignatureHandle(blobIndex);
            builder.Free();

            return signatureHandle;
        }

        private static byte ReadByte(ImmutableArray<byte> buffer, int pos)
        {
            return buffer[pos];
        }

        private static int ReadInt32(ImmutableArray<byte> buffer, int pos)
        {
            return buffer[pos] | buffer[pos + 1] << 8 | buffer[pos + 2] << 16 | buffer[pos + 3] << 24;
        }

        private EntityHandle GetHandle(IReference reference)
        {
            return reference switch
            {
                ITypeReference typeReference => GetTypeHandle(typeReference),
                IFieldReference fieldReference => GetFieldHandle(fieldReference),
                IMethodReference methodReference => GetMethodHandle(methodReference),
                _ => throw ExceptionUtilities.UnexpectedValue(reference)
            };
        }

        private EntityHandle ResolveEntityHandleFromPseudoToken(int pseudoSymbolToken)
        {
            int index = pseudoSymbolToken;
            var reference = _pseudoSymbolTokenToReferenceMap[index];
            if (reference != null)
            {
                // EDMAURER since method bodies are not visited as they are in CCI, the operations
                // that would have been done on them are done here.
                _referenceVisitor.VisitMethodBodyReference(reference);

                EntityHandle handle = GetHandle(reference);
                _pseudoSymbolTokenToTokenMap[index] = handle;
                _pseudoSymbolTokenToReferenceMap[index] = null; // Set to null to bypass next lookup
                return handle;
            }

            return _pseudoSymbolTokenToTokenMap[index];
        }

        private UserStringHandle ResolveUserStringHandleFromPseudoToken(int pseudoStringToken)
        {
            int index = pseudoStringToken;
            var str = _pseudoStringTokenToStringMap[index];
            if (str != null)
            {
                var handle = GetOrAddUserString(str);
                _pseudoStringTokenToTokenMap[index] = handle;
                _pseudoStringTokenToStringMap[index] = null; // Set to null to bypass next lookup
                return handle;
            }

            return _pseudoStringTokenToTokenMap[index];
        }

        private UserStringHandle GetOrAddUserString(string str)
        {
            if (!_userStringTokenOverflow)
            {
                try
                {
                    return metadata.GetOrAddUserString(str);
                }
                catch (ImageFormatLimitationException)
                {
                    this.Context.Diagnostics.Add(this.messageProvider.CreateDiagnostic(this.messageProvider.ERR_TooManyUserStrings, NoLocation.Singleton));
                    _userStringTokenOverflow = true;
                }
            }

            return default(UserStringHandle);
        }

        private ReservedBlob<UserStringHandle> ReserveUserString(int length)
        {
            if (!_userStringTokenOverflow)
            {
                try
                {
                    return metadata.ReserveUserString(length);
                }
                catch (ImageFormatLimitationException)
                {
                    this.Context.Diagnostics.Add(this.messageProvider.CreateDiagnostic(this.messageProvider.ERR_TooManyUserStrings, NoLocation.Singleton));
                    _userStringTokenOverflow = true;
                }
            }

            return default(ReservedBlob<UserStringHandle>);
        }

        internal const uint LiteralMethodDefinitionToken = 0x80000000;
        internal const uint LiteralGreatestMethodDefinitionToken = 0x40000000;
        internal const uint SourceDocumentIndex = 0x20000000;
        internal const uint ModuleVersionIdStringToken = 0x80000000;

        private void WriteInstructions(Blob finalIL, ImmutableArray<byte> generatedIL, ref UserStringHandle mvidStringHandle, ref Blob mvidStringFixup)
        {
            // write the raw body first and then patch tokens:
            var writer = new BlobWriter(finalIL);

            writer.WriteBytes(generatedIL);
            writer.Offset = 0;

            int offset = 0;
            while (offset < generatedIL.Length)
            {
                var operandType = InstructionOperandTypes.ReadOperandType(generatedIL, ref offset);
                switch (operandType)
                {
                    case OperandType.InlineField:
                    case OperandType.InlineMethod:
                    case OperandType.InlineTok:
                    case OperandType.InlineType:
                        {
                            int pseudoToken = ReadInt32(generatedIL, offset);
                            int token = 0;
                            // If any bits in the high-order byte of the pseudotoken are nonzero, replace the opcode with Ldc_i4
                            // and either clear the high-order byte in the pseudotoken or ignore the pseudotoken.
                            // This is a trick to enable loading raw metadata token indices as integers.
                            if (operandType == OperandType.InlineTok)
                            {
                                int tokenMask = pseudoToken & unchecked((int)0xff000000);
                                if (tokenMask != 0 && (uint)pseudoToken != 0xffffffff)
                                {
                                    Debug.Assert(ReadByte(generatedIL, offset - 1) == (byte)ILOpCode.Ldtoken);
                                    writer.Offset = offset - 1;
                                    writer.WriteByte((byte)ILOpCode.Ldc_i4);
                                    switch ((uint)tokenMask)
                                    {
                                        case LiteralMethodDefinitionToken:
                                            // Crash the compiler if pseudo token fails to resolve to a MethodDefinitionHandle.
                                            var handle = (MethodDefinitionHandle)ResolveEntityHandleFromPseudoToken(pseudoToken & 0x00ffffff);
                                            token = MetadataTokens.GetToken(handle) & 0x00ffffff;
                                            break;
                                        case LiteralGreatestMethodDefinitionToken:
                                            token = GreatestMethodDefIndex;
                                            break;
                                        case SourceDocumentIndex:
                                            token = _dynamicAnalysisDataWriterOpt.GetOrAddDocument(((CommonPEModuleBuilder)module).GetSourceDocumentFromIndex((uint)(pseudoToken & 0x00ffffff)));
                                            break;
                                        default:
                                            throw ExceptionUtilities.UnexpectedValue(tokenMask);
                                    }
                                }
                            }
                            writer.Offset = offset;
                            writer.WriteInt32(token == 0 ? MetadataTokens.GetToken(ResolveEntityHandleFromPseudoToken(pseudoToken)) : token);
                            offset += 4;
                            break;
                        }

                    case OperandType.InlineString:
                        {
                            writer.Offset = offset;

                            int pseudoToken = ReadInt32(generatedIL, offset);
                            UserStringHandle handle;

                            if ((uint)pseudoToken == ModuleVersionIdStringToken)
                            {
                                // The pseudotoken encoding indicates that the string should refer to a textual encoding of the
                                // current module's module version ID (such that the MVID can be realized using Guid.Parse).
                                // The value cannot be determined until very late in the compilation, so reserve a slot for it now and fill in the value later.
                                if (mvidStringHandle.IsNil)
                                {
                                    const int guidStringLength = 36;
                                    Debug.Assert(guidStringLength == default(Guid).ToString().Length);
                                    var reserved = ReserveUserString(guidStringLength);
                                    mvidStringHandle = reserved.Handle;
                                    mvidStringFixup = reserved.Content;
                                }

                                handle = mvidStringHandle;
                            }
                            else
                            {
                                handle = ResolveUserStringHandleFromPseudoToken(pseudoToken);
                            }

                            writer.WriteInt32(MetadataTokens.GetToken(handle));

                            offset += 4;
                            break;
                        }

                    case OperandType.InlineSig: // calli
                    case OperandType.InlineBrTarget:
                    case OperandType.InlineI:
                    case OperandType.ShortInlineR:
                        offset += 4;
                        break;

                    case OperandType.InlineSwitch:
                        int argCount = ReadInt32(generatedIL, offset);
                        // skip switch arguments count and arguments
                        offset += (argCount + 1) * 4;
                        break;

                    case OperandType.InlineI8:
                    case OperandType.InlineR:
                        offset += 8;
                        break;

                    case OperandType.InlineNone:
                        break;

                    case OperandType.InlineVar:
                        offset += 2;
                        break;

                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar:
                        offset += 1;
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(operandType);
                }
            }
        }

        private void SerializeMethodBodyExceptionHandlerTable(ExceptionRegionEncoder encoder, ImmutableArray<ExceptionHandlerRegion> regions)
        {
            foreach (var region in regions)
            {
                var exceptionType = region.ExceptionType;

                encoder.Add(
                    region.HandlerKind,
                    region.TryStartOffset,
                    region.TryLength,
                    region.HandlerStartOffset,
                    region.HandlerLength,
                    (exceptionType != null) ? GetTypeHandle(exceptionType) : default(EntityHandle),
                    region.FilterDecisionStartOffset);
            }
        }

        private static bool MayUseSmallExceptionHeaders(ImmutableArray<ExceptionHandlerRegion> exceptionRegions)
        {
            if (!ExceptionRegionEncoder.IsSmallRegionCount(exceptionRegions.Length))
            {
                return false;
            }

            foreach (var region in exceptionRegions)
            {
                if (!ExceptionRegionEncoder.IsSmallExceptionRegion(region.TryStartOffset, region.TryLength) ||
                    !ExceptionRegionEncoder.IsSmallExceptionRegion(region.HandlerStartOffset, region.HandlerLength))
                {
                    return false;
                }
            }

            return true;
        }

        private void SerializeParameterInformation(ParameterTypeEncoder encoder, IParameterTypeInformation parameterTypeInformation)
        {
            var type = parameterTypeInformation.GetType(Context);

            if (module.IsPlatformType(type, PlatformType.SystemTypedReference))
            {
                Debug.Assert(!parameterTypeInformation.IsByReference);
                SerializeCustomModifiers(encoder.CustomModifiers(), parameterTypeInformation.CustomModifiers);

                encoder.TypedReference();
            }
            else
            {
                Debug.Assert(parameterTypeInformation.RefCustomModifiers.Length == 0 || parameterTypeInformation.IsByReference);
                SerializeCustomModifiers(encoder.CustomModifiers(), parameterTypeInformation.RefCustomModifiers);

                var typeEncoder = encoder.Type(parameterTypeInformation.IsByReference);

                SerializeCustomModifiers(typeEncoder.CustomModifiers(), parameterTypeInformation.CustomModifiers);
                SerializeTypeReference(typeEncoder, type);
            }
        }

        private void SerializeFieldSignature(IFieldReference fieldReference, BlobBuilder builder)
        {
            var typeEncoder = new BlobEncoder(builder).FieldSignature();
            SerializeTypeReference(typeEncoder, fieldReference.GetType(Context));
        }

        private void SerializeMethodSpecificationSignature(BlobBuilder builder, IGenericMethodInstanceReference genericMethodInstanceReference)
        {
            var argsEncoder = new BlobEncoder(builder).MethodSpecificationSignature(genericMethodInstanceReference.GetGenericMethod(Context).GenericParameterCount);
            foreach (ITypeReference genericArgument in genericMethodInstanceReference.GetGenericArguments(Context))
            {
                ITypeReference typeRef = genericArgument;
                SerializeTypeReference(argsEncoder.AddArgument(), typeRef);
            }
        }

        private void SerializeCustomAttributeSignature(ICustomAttribute customAttribute, BlobBuilder builder)
        {
            var parameters = customAttribute.Constructor(Context, reportDiagnostics: false).GetParameters(Context);
            var arguments = customAttribute.GetArguments(Context);
            Debug.Assert(parameters.Length == arguments.Length);

            FixedArgumentsEncoder fixedArgsEncoder;
            CustomAttributeNamedArgumentsEncoder namedArgsEncoder;
            new BlobEncoder(builder).CustomAttributeSignature(out fixedArgsEncoder, out namedArgsEncoder);

            for (int i = 0; i < parameters.Length; i++)
            {
                SerializeMetadataExpression(fixedArgsEncoder.AddArgument(), arguments[i], parameters[i].GetType(Context));
            }

            SerializeCustomAttributeNamedArguments(namedArgsEncoder.Count(customAttribute.NamedArgumentCount), customAttribute);
        }

        private void SerializeCustomAttributeNamedArguments(NamedArgumentsEncoder encoder, ICustomAttribute customAttribute)
        {
            foreach (IMetadataNamedArgument namedArgument in customAttribute.GetNamedArguments(Context))
            {
                NamedArgumentTypeEncoder typeEncoder;
                NameEncoder nameEncoder;
                LiteralEncoder literalEncoder;
                encoder.AddArgument(namedArgument.IsField, out typeEncoder, out nameEncoder, out literalEncoder);

                SerializeNamedArgumentType(typeEncoder, namedArgument.Type);
                nameEncoder.Name(namedArgument.ArgumentName);
                SerializeMetadataExpression(literalEncoder, namedArgument.ArgumentValue, namedArgument.Type);
            }
        }

        private void SerializeNamedArgumentType(NamedArgumentTypeEncoder encoder, ITypeReference type)
        {
            if (type is IArrayTypeReference arrayType)
            {
                SerializeCustomAttributeArrayType(encoder.SZArray(), arrayType);
            }
            else if (module.IsPlatformType(type, PlatformType.SystemObject))
            {
                encoder.Object();
            }
            else
            {
                SerializeCustomAttributeElementType(encoder.ScalarType(), type);
            }
        }

        private void SerializeMetadataExpression(LiteralEncoder encoder, IMetadataExpression expression, ITypeReference targetType)
        {
            if (expression is MetadataCreateArray a)
            {
                ITypeReference targetElementType;
                VectorEncoder vectorEncoder;
                if (!(targetType is IArrayTypeReference targetArrayType))
                {
                    // implicit conversion from array to object
                    Debug.Assert(this.module.IsPlatformType(targetType, PlatformType.SystemObject));

                    CustomAttributeArrayTypeEncoder arrayTypeEncoder;
                    encoder.TaggedVector(out arrayTypeEncoder, out vectorEncoder);
                    SerializeCustomAttributeArrayType(arrayTypeEncoder, a.ArrayType);

                    targetElementType = a.ElementType;
                }
                else
                {
                    vectorEncoder = encoder.Vector();

                    // In FixedArg the element type of the parameter array has to match the element type of the argument array,
                    // but in NamedArg T[] can be assigned to object[]. In that case we need to encode the arguments using 
                    // the parameter element type not the argument element type.
                    targetElementType = targetArrayType.GetElementType(this.Context);
                }

                var literalsEncoder = vectorEncoder.Count(a.Elements.Length);

                foreach (IMetadataExpression elemValue in a.Elements)
                {
                    SerializeMetadataExpression(literalsEncoder.AddLiteral(), elemValue, targetElementType);
                }
            }
            else
            {
                ScalarEncoder scalarEncoder;
                MetadataConstant c = expression as MetadataConstant;

                if (this.module.IsPlatformType(targetType, PlatformType.SystemObject))
                {
                    CustomAttributeElementTypeEncoder typeEncoder;
                    encoder.TaggedScalar(out typeEncoder, out scalarEncoder);

                    // special case null argument assigned to Object parameter - treat as null string
                    if (c != null &&
                        c.Value == null &&
                        this.module.IsPlatformType(c.Type, PlatformType.SystemObject))
                    {
                        typeEncoder.String();
                    }
                    else
                    {
                        SerializeCustomAttributeElementType(typeEncoder, expression.Type);
                    }
                }
                else
                {
                    scalarEncoder = encoder.Scalar();
                }

                if (c != null)
                {
                    if (c.Type is IArrayTypeReference)
                    {
                        scalarEncoder.NullArray();
                        return;
                    }

                    Debug.Assert(!module.IsPlatformType(c.Type, PlatformType.SystemType) || c.Value == null);
                    scalarEncoder.Constant(c.Value);
                }
                else
                {
                    scalarEncoder.SystemType(((MetadataTypeOf)expression).TypeToGet.GetSerializedTypeName(Context));
                }
            }
        }

        private void SerializeMarshallingDescriptor(IMarshallingInformation marshallingInformation, BlobBuilder writer)
        {
            writer.WriteCompressedInteger((int)marshallingInformation.UnmanagedType);
            switch (marshallingInformation.UnmanagedType)
            {
                case UnmanagedType.ByValArray: // NATIVE_TYPE_FIXEDARRAY
                    Debug.Assert(marshallingInformation.NumberOfElements >= 0);
                    writer.WriteCompressedInteger(marshallingInformation.NumberOfElements);
                    if (marshallingInformation.ElementType >= 0)
                    {
                        writer.WriteCompressedInteger((int)marshallingInformation.ElementType);
                    }

                    break;

                case Constants.UnmanagedType_CustomMarshaler:
                    writer.WriteUInt16(0); // padding

                    object marshaller = marshallingInformation.GetCustomMarshaller(Context);
                    switch (marshaller)
                    {
                        case ITypeReference marshallerTypeRef:
                            this.SerializeTypeName(marshallerTypeRef, writer);
                            break;
                        case null:
                            writer.WriteByte(0);
                            break;
                        default:
                            writer.WriteSerializedString((string)marshaller);
                            break;
                    }

                    var arg = marshallingInformation.CustomMarshallerRuntimeArgument;
                    if (arg != null)
                    {
                        writer.WriteSerializedString(arg);
                    }
                    else
                    {
                        writer.WriteByte(0);
                    }

                    break;

                case UnmanagedType.LPArray: // NATIVE_TYPE_ARRAY
                    Debug.Assert(marshallingInformation.ElementType >= 0);
                    writer.WriteCompressedInteger((int)marshallingInformation.ElementType);
                    if (marshallingInformation.ParamIndex >= 0)
                    {
                        writer.WriteCompressedInteger(marshallingInformation.ParamIndex);
                        if (marshallingInformation.NumberOfElements >= 0)
                        {
                            writer.WriteCompressedInteger(marshallingInformation.NumberOfElements);
                            writer.WriteByte(1); // The parameter number is valid
                        }
                    }
                    else if (marshallingInformation.NumberOfElements >= 0)
                    {
                        writer.WriteByte(0); // Dummy parameter value emitted so that NumberOfElements can be in a known position
                        writer.WriteCompressedInteger(marshallingInformation.NumberOfElements);
                        writer.WriteByte(0); // The parameter number is not valid
                    }

                    break;

                case Constants.UnmanagedType_SafeArray:
                    if (marshallingInformation.SafeArrayElementSubtype >= 0)
                    {
                        writer.WriteCompressedInteger((int)marshallingInformation.SafeArrayElementSubtype);
                        var elementType = marshallingInformation.GetSafeArrayElementUserDefinedSubtype(Context);
                        if (elementType != null)
                        {
                            this.SerializeTypeName(elementType, writer);
                        }
                    }

                    break;

                case UnmanagedType.ByValTStr: // NATIVE_TYPE_FIXEDSYSSTRING
                    writer.WriteCompressedInteger(marshallingInformation.NumberOfElements);
                    break;

                case UnmanagedType.Interface:
                case Constants.UnmanagedType_IDispatch:
                case UnmanagedType.IUnknown:
                    if (marshallingInformation.IidParameterIndex >= 0)
                    {
                        writer.WriteCompressedInteger(marshallingInformation.IidParameterIndex);
                    }

                    break;
            }
        }

        private void SerializeTypeName(ITypeReference typeReference, BlobBuilder writer)
        {
            writer.WriteSerializedString(typeReference.GetSerializedTypeName(this.Context));
        }

        /// <summary>
        /// Computes the string representing the strong name of the given assembly reference.
        /// </summary>
        internal static string StrongName(IAssemblyReference assemblyReference)
        {
            var identity = assemblyReference.Identity;

            var pooled = PooledStringBuilder.GetInstance();
            StringBuilder sb = pooled.Builder;
            sb.Append(identity.Name);
            sb.AppendFormat(CultureInfo.InvariantCulture, ", Version={0}.{1}.{2}.{3}", identity.Version.Major, identity.Version.Minor, identity.Version.Build, identity.Version.Revision);
            if (!string.IsNullOrEmpty(identity.CultureName))
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, ", Culture={0}", identity.CultureName);
            }
            else
            {
                sb.Append(", Culture=neutral");
            }

            sb.Append(", PublicKeyToken=");
            if (identity.PublicKeyToken.Length > 0)
            {
                foreach (byte b in identity.PublicKeyToken)
                {
                    sb.Append(b.ToString("x2"));
                }
            }
            else
            {
                sb.Append("null");
            }

            if (identity.IsRetargetable)
            {
                sb.Append(", Retargetable=Yes");
            }

            if (identity.ContentType == AssemblyContentType.WindowsRuntime)
            {
                sb.Append(", ContentType=WindowsRuntime");
            }
            else
            {
                Debug.Assert(identity.ContentType == AssemblyContentType.Default);
            }

            return pooled.ToStringAndFree();
        }

        private void SerializePermissionSet(ImmutableArray<ICustomAttribute> permissionSet, BlobBuilder writer)
        {
            EmitContext context = this.Context;
            foreach (ICustomAttribute customAttribute in permissionSet)
            {
                bool isAssemblyQualified = true;
                string typeName = customAttribute.GetType(context).GetSerializedTypeName(context, ref isAssemblyQualified);
                if (!isAssemblyQualified)
                {
                    INamespaceTypeReference namespaceType = customAttribute.GetType(context).AsNamespaceTypeReference;
                    if (namespaceType?.GetUnit(context) is IAssemblyReference referencedAssembly)
                    {
                        typeName = typeName + ", " + StrongName(referencedAssembly);
                    }
                }

                writer.WriteSerializedString(typeName);

                var customAttributeArgsBuilder = PooledBlobBuilder.GetInstance();
                var namedArgsEncoder = new BlobEncoder(customAttributeArgsBuilder).PermissionSetArguments(customAttribute.NamedArgumentCount);
                SerializeCustomAttributeNamedArguments(namedArgsEncoder, customAttribute);
                writer.WriteCompressedInteger(customAttributeArgsBuilder.Count);

                customAttributeArgsBuilder.WriteContentTo(writer);
                customAttributeArgsBuilder.Free();
            }
            // TODO: xml for older platforms
        }

        private void SerializeReturnValueAndParameters(MethodSignatureEncoder encoder, ISignature signature, ImmutableArray<IParameterTypeInformation> varargParameters)
        {
            var declaredParameters = signature.GetParameters(Context);
            var returnType = signature.GetType(Context);

            ReturnTypeEncoder returnTypeEncoder;
            ParametersEncoder parametersEncoder;

            encoder.Parameters(declaredParameters.Length + varargParameters.Length, out returnTypeEncoder, out parametersEncoder);

            if (module.IsPlatformType(returnType, PlatformType.SystemTypedReference))
            {
                Debug.Assert(!signature.ReturnValueIsByRef);
                SerializeCustomModifiers(returnTypeEncoder.CustomModifiers(), signature.ReturnValueCustomModifiers);

                returnTypeEncoder.TypedReference();
            }
            else if (module.IsPlatformType(returnType, PlatformType.SystemVoid))
            {
                Debug.Assert(!signature.ReturnValueIsByRef);
                SerializeCustomModifiers(returnTypeEncoder.CustomModifiers(), signature.ReturnValueCustomModifiers);

                returnTypeEncoder.Void();
            }
            else
            {
                Debug.Assert(signature.RefCustomModifiers.Length == 0 || signature.ReturnValueIsByRef);
                SerializeCustomModifiers(returnTypeEncoder.CustomModifiers(), signature.RefCustomModifiers);

                var typeEncoder = returnTypeEncoder.Type(signature.ReturnValueIsByRef);

                SerializeCustomModifiers(typeEncoder.CustomModifiers(), signature.ReturnValueCustomModifiers);
                SerializeTypeReference(typeEncoder, returnType);
            }

            foreach (IParameterTypeInformation parameter in declaredParameters)
            {
                SerializeParameterInformation(parametersEncoder.AddParameter(), parameter);
            }

            if (varargParameters.Length > 0)
            {
                parametersEncoder = parametersEncoder.StartVarArgs();
                foreach (IParameterTypeInformation parameter in varargParameters)
                {
                    SerializeParameterInformation(parametersEncoder.AddParameter(), parameter);
                }
            }
        }

        private void SerializeTypeReference(SignatureTypeEncoder encoder, ITypeReference typeReference)
        {
            while (true)
            {
                // TYPEDREF is only allowed in RetType, Param, LocalVarSig signatures
                Debug.Assert(!module.IsPlatformType(typeReference, PlatformType.SystemTypedReference));

                if (typeReference is IModifiedTypeReference modifiedTypeReference)
                {
                    SerializeCustomModifiers(encoder.CustomModifiers(), modifiedTypeReference.CustomModifiers);
                    typeReference = modifiedTypeReference.UnmodifiedType;
                    continue;
                }

                var primitiveType = typeReference.TypeCode;
                if (primitiveType != PrimitiveTypeCode.Pointer && primitiveType != PrimitiveTypeCode.NotPrimitive)
                {
                    SerializePrimitiveType(encoder, primitiveType);
                    return;
                }

                if (typeReference is IPointerTypeReference pointerTypeReference)
                {
                    typeReference = pointerTypeReference.GetTargetType(Context);
                    encoder = encoder.Pointer();
                    continue;
                }

                IGenericTypeParameterReference genericTypeParameterReference = typeReference.AsGenericTypeParameterReference;
                if (genericTypeParameterReference != null)
                {
                    encoder.GenericTypeParameter(
                        GetNumberOfInheritedTypeParameters(genericTypeParameterReference.DefiningType) +
                        genericTypeParameterReference.Index);
                    return;
                }

                if (typeReference is IArrayTypeReference arrayTypeReference)
                {
                    typeReference = arrayTypeReference.GetElementType(Context);

                    if (arrayTypeReference.IsSZArray)
                    {
                        encoder = encoder.SZArray();
                        continue;
                    }
                    else
                    {
                        SignatureTypeEncoder elementType;
                        ArrayShapeEncoder arrayShape;
                        encoder.Array(out elementType, out arrayShape);
                        SerializeTypeReference(elementType, typeReference);
                        arrayShape.Shape(arrayTypeReference.Rank, arrayTypeReference.Sizes, arrayTypeReference.LowerBounds);
                        return;
                    }
                }

                if (module.IsPlatformType(typeReference, PlatformType.SystemObject))
                {
                    encoder.Object();
                    return;
                }

                IGenericMethodParameterReference genericMethodParameterReference = typeReference.AsGenericMethodParameterReference;
                if (genericMethodParameterReference != null)
                {
                    encoder.GenericMethodTypeParameter(genericMethodParameterReference.Index);
                    return;
                }

                if (typeReference.IsTypeSpecification())
                {
                    ITypeReference uninstantiatedTypeReference = typeReference.GetUninstantiatedGenericType(Context);

                    // Roslyn's uninstantiated type is the same object as the instantiated type for
                    // types closed over their type parameters, so to speak.

                    var consolidatedTypeArguments = ArrayBuilder<ITypeReference>.GetInstance();
                    typeReference.GetConsolidatedTypeArguments(consolidatedTypeArguments, this.Context);

                    var genericArgsEncoder = encoder.GenericInstantiation(
                        GetTypeHandle(uninstantiatedTypeReference, treatRefAsPotentialTypeSpec: false),
                        consolidatedTypeArguments.Count,
                        typeReference.IsValueType);

                    foreach (ITypeReference typeArgument in consolidatedTypeArguments)
                    {
                        SerializeTypeReference(genericArgsEncoder.AddArgument(), typeArgument);
                    }

                    consolidatedTypeArguments.Free();
                    return;
                }

                encoder.Type(GetTypeHandle(typeReference), typeReference.IsValueType);
                return;
            }
        }

        private static void SerializePrimitiveType(SignatureTypeEncoder encoder, PrimitiveTypeCode primitiveType)
        {
            switch (primitiveType)
            {
                case PrimitiveTypeCode.Boolean:
                    encoder.Boolean();
                    break;

                case PrimitiveTypeCode.UInt8:
                    encoder.Byte();
                    break;

                case PrimitiveTypeCode.Int8:
                    encoder.SByte();
                    break;

                case PrimitiveTypeCode.Char:
                    encoder.Char();
                    break;

                case PrimitiveTypeCode.Int16:
                    encoder.Int16();
                    break;

                case PrimitiveTypeCode.UInt16:
                    encoder.UInt16();
                    break;

                case PrimitiveTypeCode.Int32:
                    encoder.Int32();
                    break;

                case PrimitiveTypeCode.UInt32:
                    encoder.UInt32();
                    break;

                case PrimitiveTypeCode.Int64:
                    encoder.Int64();
                    break;

                case PrimitiveTypeCode.UInt64:
                    encoder.UInt64();
                    break;

                case PrimitiveTypeCode.Float32:
                    encoder.Single();
                    break;

                case PrimitiveTypeCode.Float64:
                    encoder.Double();
                    break;

                case PrimitiveTypeCode.IntPtr:
                    encoder.IntPtr();
                    break;

                case PrimitiveTypeCode.UIntPtr:
                    encoder.UIntPtr();
                    break;

                case PrimitiveTypeCode.String:
                    encoder.String();
                    break;

                case PrimitiveTypeCode.Void:
                    // "void" is handled specifically for "void*" with custom modifiers.
                    // If SignatureTypeEncoder supports such cases directly, this can
                    // be removed. See https://github.com/dotnet/corefx/issues/14571.
                    encoder.Builder.WriteByte((byte)System.Reflection.Metadata.PrimitiveTypeCode.Void);
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(primitiveType);
            }
        }

        private void SerializeCustomAttributeArrayType(CustomAttributeArrayTypeEncoder encoder, IArrayTypeReference arrayTypeReference)
        {
            // A single-dimensional, zero-based array is specified as a single byte 0x1D followed by the FieldOrPropType of the element type. 

            // only non-jagged SZ arrays are allowed in attributes 
            // (need to encode the type of the SZ array if the parameter type is Object):
            Debug.Assert(arrayTypeReference.IsSZArray);

            var elementType = arrayTypeReference.GetElementType(Context);
            Debug.Assert(!(elementType is IModifiedTypeReference));

            if (module.IsPlatformType(elementType, PlatformType.SystemObject))
            {
                encoder.ObjectArray();
            }
            else
            {
                SerializeCustomAttributeElementType(encoder.ElementType(), elementType);
            }
        }

        private void SerializeCustomAttributeElementType(CustomAttributeElementTypeEncoder encoder, ITypeReference typeReference)
        {
            // Spec:
            // The FieldOrPropType shall be exactly one of:
            // ELEMENT_TYPE_BOOLEAN, ELEMENT_TYPE_CHAR, ELEMENT_TYPE_I1, ELEMENT_TYPE_U1, ELEMENT_TYPE_I2, ELEMENT_TYPE_U2, ELEMENT_TYPE_I4, 
            // ELEMENT_TYPE_U4, ELEMENT_TYPE_I8, ELEMENT_TYPE_U8, ELEMENT_TYPE_R4, ELEMENT_TYPE_R8, ELEMENT_TYPE_STRING.
            // An enum is specified as a single byte 0x55 followed by a SerString.

            var primitiveType = typeReference.TypeCode;
            if (primitiveType != PrimitiveTypeCode.NotPrimitive)
            {
                SerializePrimitiveType(encoder, primitiveType);
            }
            else if (module.IsPlatformType(typeReference, PlatformType.SystemType))
            {
                encoder.SystemType();
            }
            else
            {
                Debug.Assert(typeReference.IsEnum);
                encoder.Enum(typeReference.GetSerializedTypeName(this.Context));
            }
        }

        private static void SerializePrimitiveType(CustomAttributeElementTypeEncoder encoder, PrimitiveTypeCode primitiveType)
        {
            switch (primitiveType)
            {
                case PrimitiveTypeCode.Boolean:
                    encoder.Boolean();
                    break;

                case PrimitiveTypeCode.UInt8:
                    encoder.Byte();
                    break;

                case PrimitiveTypeCode.Int8:
                    encoder.SByte();
                    break;

                case PrimitiveTypeCode.Char:
                    encoder.Char();
                    break;

                case PrimitiveTypeCode.Int16:
                    encoder.Int16();
                    break;

                case PrimitiveTypeCode.UInt16:
                    encoder.UInt16();
                    break;

                case PrimitiveTypeCode.Int32:
                    encoder.Int32();
                    break;

                case PrimitiveTypeCode.UInt32:
                    encoder.UInt32();
                    break;

                case PrimitiveTypeCode.Int64:
                    encoder.Int64();
                    break;

                case PrimitiveTypeCode.UInt64:
                    encoder.UInt64();
                    break;

                case PrimitiveTypeCode.Float32:
                    encoder.Single();
                    break;

                case PrimitiveTypeCode.Float64:
                    encoder.Double();
                    break;

                case PrimitiveTypeCode.String:
                    encoder.String();
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(primitiveType);
            }
        }

        private void SerializeCustomModifiers(CustomModifiersEncoder encoder, ImmutableArray<ICustomModifier> modifiers)
        {
            foreach (var modifier in modifiers)
            {
                encoder = encoder.AddModifier(GetTypeHandle(modifier.GetModifier(Context)), modifier.IsOptional);
            }
        }

        private int GetNumberOfInheritedTypeParameters(ITypeReference type)
        {
            INestedTypeReference nestedType = type.AsNestedTypeReference;
            if (nestedType == null)
            {
                return 0;
            }

            ISpecializedNestedTypeReference specializedNestedType = nestedType.AsSpecializedNestedTypeReference;
            if (specializedNestedType != null)
            {
                nestedType = specializedNestedType.GetUnspecializedVersion(Context);
            }

            int result = 0;
            type = nestedType.GetContainingType(Context);
            nestedType = type.AsNestedTypeReference;
            while (nestedType != null)
            {
                result += nestedType.GenericParameterCount;
                type = nestedType.GetContainingType(Context);
                nestedType = type.AsNestedTypeReference;
            }

            result += type.AsNamespaceTypeReference.GenericParameterCount;
            return result;
        }

        internal static EditAndContinueMethodDebugInformation GetEncMethodDebugInfo(IMethodBody methodBody)
        {
            ImmutableArray<LocalSlotDebugInfo> encLocalSlots;

            // Kickoff method of a state machine (async/iterator method) doesn't have any interesting locals,
            // so we use its EnC method debug info to store information about locals hoisted to the state machine.
            var encSlotInfo = methodBody.StateMachineHoistedLocalSlots;
            if (encSlotInfo.IsDefault)
            {
                encLocalSlots = GetLocalSlotDebugInfos(methodBody.LocalVariables);
            }
            else
            {
                encLocalSlots = GetLocalSlotDebugInfos(encSlotInfo);
            }

            return new EditAndContinueMethodDebugInformation(methodBody.MethodId.Ordinal, encLocalSlots, methodBody.ClosureDebugInfo, methodBody.LambdaDebugInfo);
        }

        internal static ImmutableArray<LocalSlotDebugInfo> GetLocalSlotDebugInfos(ImmutableArray<ILocalDefinition> locals)
        {
            if (!locals.Any(variable => !variable.SlotInfo.Id.IsNone))
            {
                return ImmutableArray<LocalSlotDebugInfo>.Empty;
            }

            return locals.SelectAsArray(variable => variable.SlotInfo);
        }

        internal static ImmutableArray<LocalSlotDebugInfo> GetLocalSlotDebugInfos(ImmutableArray<EncHoistedLocalInfo> locals)
        {
            if (!locals.Any(variable => !variable.SlotInfo.Id.IsNone))
            {
                return ImmutableArray<LocalSlotDebugInfo>.Empty;
            }

            return locals.SelectAsArray(variable => variable.SlotInfo);
        }

        protected abstract class HeapOrReferenceIndexBase<T>
        {
            private readonly MetadataWriter _writer;
            private readonly List<T> _rows;
            private readonly int _firstRowId;

            protected HeapOrReferenceIndexBase(MetadataWriter writer, int lastRowId)
            {
                _writer = writer;
                _rows = new List<T>();
                _firstRowId = lastRowId + 1;
            }

            public abstract bool TryGetValue(T item, out int index);

            public int GetOrAdd(T item)
            {
                int index;
                if (!this.TryGetValue(item, out index))
                {
                    index = Add(item);
                }

                return index;
            }

            public IReadOnlyList<T> Rows
            {
                get { return _rows; }
            }

            public int Add(T item)
            {
                Debug.Assert(!_writer._tableIndicesAreComplete);
#if DEBUG
                int i;
                Debug.Assert(!this.TryGetValue(item, out i));
#endif
                int index = _firstRowId + _rows.Count;
                this.AddItem(item, index);
                _rows.Add(item);
                return index;
            }

            protected abstract void AddItem(T item, int index);
        }

        protected sealed class HeapOrReferenceIndex<T> : HeapOrReferenceIndexBase<T>
        {
            private readonly Dictionary<T, int> _index;

            public HeapOrReferenceIndex(MetadataWriter writer, int lastRowId = 0)
                : this(writer, new Dictionary<T, int>(), lastRowId)
            {
            }

            private HeapOrReferenceIndex(MetadataWriter writer, Dictionary<T, int> index, int lastRowId)
                : base(writer, lastRowId)
            {
                Debug.Assert(index.Count == 0);
                _index = index;
            }

            public override bool TryGetValue(T item, out int index)
            {
                return _index.TryGetValue(item, out index);
            }

            protected override void AddItem(T item, int index)
            {
                _index.Add(item, index);
            }
        }

        protected sealed class InstanceAndStructuralReferenceIndex<T> : HeapOrReferenceIndexBase<T> where T : IReference
        {
            private readonly Dictionary<T, int> _instanceIndex;
            private readonly Dictionary<T, int> _structuralIndex;

            public InstanceAndStructuralReferenceIndex(MetadataWriter writer, IEqualityComparer<T> structuralComparer, int lastRowId = 0)
                : base(writer, lastRowId)
            {
                _instanceIndex = new Dictionary<T, int>();
                _structuralIndex = new Dictionary<T, int>(structuralComparer);
            }

            public override bool TryGetValue(T item, out int index)
            {
                if (_instanceIndex.TryGetValue(item, out index))
                {
                    return true;
                }
                if (_structuralIndex.TryGetValue(item, out index))
                {
                    _instanceIndex.Add(item, index);
                    return true;
                }
                return false;
            }

            protected override void AddItem(T item, int index)
            {
                _instanceIndex.Add(item, index);
                _structuralIndex.Add(item, index);
            }
        }
    }
}
