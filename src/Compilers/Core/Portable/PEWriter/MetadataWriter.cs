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
using System.Reflection.Metadata.Ecma335.Blobs;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;
using EmitContext = Microsoft.CodeAnalysis.Emit.EmitContext;

namespace Microsoft.Cci
{
    internal abstract partial class MetadataWriter
    {
        private static readonly Encoding s_utf8Encoding = Encoding.UTF8;

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

        // If true, it is allowed to have methods not have bodies (for emitting metadata-only assembly)
        internal readonly bool allowMissingMethodBodies;

        // A map of method body before token translation to RVA. Used for deduplication of small bodies.
        private readonly Dictionary<ImmutableArray<byte>, int> _smallMethodBodies;

        protected MetadataWriter(
            MetadataBuilder metadata,
            MetadataBuilder debugMetadataOpt,
            EmitContext context,
            CommonMessageProvider messageProvider,
            bool allowMissingMethodBodies,
            bool deterministic,
            CancellationToken cancellationToken)
        {
            this.module = context.Module;
            _deterministic = deterministic;
            this.allowMissingMethodBodies = allowMissingMethodBodies;

            // EDMAURER provide some reasonable size estimates for these that will avoid
            // much of the reallocation that would occur when growing these from empty.
            _signatureIndex = new Dictionary<ISignature, KeyValuePair<BlobIdx, ImmutableArray<byte>>>(module.HintNumberOfMethodDefinitions); //ignores field signatures

            _numTypeDefsEstimate = module.HintNumberOfMethodDefinitions / 6;
            _exportedTypeIndex = new Dictionary<ITypeReference, int>(_numTypeDefsEstimate);
            _exportedTypeList = new List<ITypeReference>(_numTypeDefsEstimate);

            this.Context = context;
            this.messageProvider = messageProvider;
            _cancellationToken = cancellationToken;

            this.metadata = metadata;
            _debugMetadataOpt = debugMetadataOpt;

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
        private bool EmitAssemblyDefinition => module.AsAssembly != null && !IsMinimalDelta;

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
        /// Returns true and the 1-based index of the type definition
        /// if the type definition is recognized. Otherwise returns false.
        /// The index is into the full metadata.
        /// </summary>
        protected abstract bool TryGetTypeDefIndex(ITypeDefinition def, out int index);

        /// <summary>
        /// The 1-based index of the type definition.
        /// The index is into the full metadata.
        /// </summary>
        protected abstract int GetTypeDefIndex(ITypeDefinition def);

        /// <summary>
        /// The type definition at the 0-based index into the full set. Deltas
        /// are only required to support indexing into current generation.
        /// </summary>
        protected abstract ITypeDefinition GetTypeDef(int index);

        /// <summary>
        /// The type definitions to be emitted, in row order. These
        /// are just the type definitions from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<ITypeDefinition> GetTypeDefs();

        /// <summary>
        /// The 1-based index of the event definition.
        /// The index is into the full metadata.
        /// </summary>
        protected abstract int GetEventDefIndex(IEventDefinition def);

        /// <summary>
        /// The event definitions to be emitted, in row order. These
        /// are just the event definitions from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<IEventDefinition> GetEventDefs();

        /// <summary>
        /// The 1-based index of the field definition.
        /// The index is into the full metadata.
        /// </summary>
        protected abstract int GetFieldDefIndex(IFieldDefinition def);

        /// <summary>
        /// The field definitions to be emitted, in row order. These
        /// are just the field definitions from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<IFieldDefinition> GetFieldDefs();

        /// <summary>
        /// Returns true and the 1-based index of the method definition
        /// if the method definition is recognized. Otherwise returns false.
        /// The index is into the full metadata.
        /// </summary>
        protected abstract bool TryGetMethodDefIndex(IMethodDefinition def, out int index);

        /// <summary>
        /// The 1-based index of the method definition.
        /// The index is into the full metadata.
        /// </summary>
        protected abstract int GetMethodDefIndex(IMethodDefinition def);

        /// <summary>
        /// The method definition at the 0-based index into the full set. Deltas
        /// are only required to support indexing into current generation.
        /// </summary>
        protected abstract IMethodDefinition GetMethodDef(int index);

        /// <summary>
        /// The method definitions to be emitted, in row order. These
        /// are just the method definitions from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<IMethodDefinition> GetMethodDefs();

        /// <summary>
        /// The 1-based index of the property definition.
        /// The index is into the full metadata.
        /// </summary>
        protected abstract int GetPropertyDefIndex(IPropertyDefinition def);

        /// <summary>
        /// The property definitions to be emitted, in row order. These
        /// are just the property definitions from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<IPropertyDefinition> GetPropertyDefs();

        /// <summary>
        /// The 1-based index of the parameter definition.
        /// The index is into the full metadata.
        /// </summary>
        protected abstract int GetParameterDefIndex(IParameterDefinition def);

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
        /// The 1-based index of the first field of the type.
        /// </summary>
        protected abstract int GetFieldDefIndex(INamedTypeDefinition typeDef);

        /// <summary>
        /// The 1-based index of the first method of the type.
        /// </summary>
        protected abstract int GetMethodDefIndex(INamedTypeDefinition typeDef);

        /// <summary>
        /// The 1-based index of the first parameter of the method.
        /// </summary>
        protected abstract int GetParameterDefIndex(IMethodDefinition methodDef);

        /// <summary>
        /// Return the 1-based index of the assembly reference, adding
        /// the reference to the index for this generation if missing.
        /// The index is into the full metadata. However, deltas
        /// are not required to return rows from previous generations.
        /// </summary>
        protected abstract int GetOrAddAssemblyRefIndex(IAssemblyReference reference);

        /// <summary>
        /// The assembly references to be emitted, in row order. These
        /// are just the assembly references from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<IAssemblyReference> GetAssemblyRefs();

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
        /// Return the 1-based index of the module reference, adding
        /// the reference to the index for this generation if missing.
        /// The index is into the full metadata. However, deltas
        /// are not required to return rows from previous generations.
        /// </summary>
        protected abstract int GetOrAddModuleRefIndex(string reference);

        /// <summary>
        /// The module references to be emitted, in row order. These
        /// are just the module references from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<string> GetModuleRefs();

        /// <summary>
        /// Return the 1-based index of the member reference, adding
        /// the reference to the index for this generation if missing.
        /// The index is into the full metadata. However, deltas
        /// are not required to return rows from previous generations.
        /// </summary>
        protected abstract int GetOrAddMemberRefIndex(ITypeMemberReference reference);

        /// <summary>
        /// The member references to be emitted, in row order. These
        /// are just the member references from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<ITypeMemberReference> GetMemberRefs();

        /// <summary>
        /// Return the 1-based index of the method spec, adding
        /// the spec to the index for this generation if missing.
        /// The index is into the full metadata. However, deltas
        /// are not required to return rows from previous generations.
        /// </summary>
        protected abstract int GetOrAddMethodSpecIndex(IGenericMethodInstanceReference reference);

        /// <summary>
        /// The method specs to be emitted, in row order. These
        /// are just the method specs from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<IGenericMethodInstanceReference> GetMethodSpecs();

        /// <summary>
        /// Return true and the 1-based index of the type reference
        /// if the reference is available in the current generation.
        /// The index is into the full metadata. However, deltas
        /// are not required to return rows from previous generations.
        /// </summary>
        protected abstract bool TryGetTypeRefIndex(ITypeReference reference, out int index);

        /// <summary>
        /// Return the 1-based index of the type reference, adding
        /// the reference to the index for this generation if missing.
        /// The index is into the full metadata. However, deltas
        /// are not required to return rows from previous generations.
        /// </summary>
        protected abstract int GetOrAddTypeRefIndex(ITypeReference reference);

        /// <summary>
        /// The type references to be emitted, in row order. These
        /// are just the type references from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<ITypeReference> GetTypeRefs();

        /// <summary>
        /// Return the 1-based index of the type spec, adding
        /// the spec to the index for this generation if missing.
        /// The index is into the full metadata. However, deltas
        /// are not required to return rows from previous generations.
        /// </summary>
        protected abstract int GetOrAddTypeSpecIndex(ITypeReference reference);

        /// <summary>
        /// The type specs to be emitted, in row order. These
        /// are just the type specs from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<ITypeReference> GetTypeSpecs();

        /// <summary>
        /// Return the 1-based index of the signature index, adding
        /// the signature to the index for this generation if missing.
        /// The index is into the full metadata. However, deltas
        /// are not required to return rows from previous generations.
        /// </summary>
        protected abstract int GetOrAddStandAloneSignatureIndex(BlobIdx blobIndex);

        /// <summary>
        /// The signature indices to be emitted, in row order. These
        /// are just the signature indices from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<BlobIdx> GetStandAloneSignatures();

        protected abstract IEnumerable<INamespaceTypeDefinition> GetTopLevelTypes(IModule module);

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
        protected readonly IModule module;
        public readonly EmitContext Context;
        protected readonly CommonMessageProvider messageProvider;

        // progress:
        private bool _tableIndicesAreComplete;

        private int[] _pseudoSymbolTokenToTokenMap;
        private IReference[] _pseudoSymbolTokenToReferenceMap;
        private int[] _pseudoStringTokenToTokenMap;
        private List<string> _pseudoStringTokenToStringMap;
        private ReferenceIndexer _referenceVisitor;

        protected readonly MetadataBuilder metadata;

        // A builder distinct from type-system metadata builder if we are emitting debug information into a separate Portable PDB stream.
        // Shared builder (reference equals heaps) if we are embedding Portable PDB into the metadata stream.
        // Null otherwise.
        private readonly MetadataBuilder _debugMetadataOpt;

        internal bool EmitStandaloneDebugMetadata => _debugMetadataOpt != null && metadata != _debugMetadataOpt;

        private readonly Dictionary<ICustomAttribute, BlobIdx> _customAttributeSignatureIndex = new Dictionary<ICustomAttribute, BlobIdx>();
        private readonly Dictionary<ITypeReference, BlobIdx> _typeSpecSignatureIndex = new Dictionary<ITypeReference, BlobIdx>();
        private readonly Dictionary<ITypeReference, int> _exportedTypeIndex;
        private readonly List<ITypeReference> _exportedTypeList;
        private readonly Dictionary<string, int> _fileRefIndex = new Dictionary<string, int>(32);  //more than enough in most cases
        private readonly List<IFileReference> _fileRefList = new List<IFileReference>(32);
        private readonly Dictionary<IFieldReference, BlobIdx> _fieldSignatureIndex = new Dictionary<IFieldReference, BlobIdx>();

        // We need to keep track of both the index of the signature and the actual blob to support VB static local naming scheme.
        private readonly Dictionary<ISignature, KeyValuePair<BlobIdx, ImmutableArray<byte>>> _signatureIndex;

        private readonly Dictionary<IMarshallingInformation, BlobIdx> _marshallingDescriptorIndex = new Dictionary<IMarshallingInformation, BlobIdx>();
        protected readonly List<MethodImplementation> methodImplList = new List<MethodImplementation>();
        private readonly Dictionary<IGenericMethodInstanceReference, BlobIdx> _methodInstanceSignatureIndex = new Dictionary<IGenericMethodInstanceReference, BlobIdx>();

        // Well known dummy cor library types whose refs are used for attaching assembly attributes off within net modules
        // There is no guarantee the types actually exist in a cor library
        internal static readonly string dummyAssemblyAttributeParentNamespace = "System.Runtime.CompilerServices";
        internal static readonly string dummyAssemblyAttributeParentName = "AssemblyAttributesGoHere";
        internal static readonly string[,] dummyAssemblyAttributeParentQualifier = { { "", "M" }, { "S", "SM" } };
        private readonly uint[,] _dummyAssemblyAttributeParent = { { 0, 0 }, { 0, 0 } };

        internal IModule Module => module;

        private void CreateMethodBodyReferenceIndex()
        {
            int count;
            var referencesInIL = module.ReferencesInIL(out count);

            _pseudoSymbolTokenToTokenMap = new int[count];
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
            this.CreateInitialExportedTypeIndex();

            // Find all references and assign tokens.
            _referenceVisitor = this.CreateReferenceVisitor();
            this.module.Dispatch(_referenceVisitor);

            this.CreateMethodBodyReferenceIndex();
        }

        private void CreateUserStringIndices()
        {
            _pseudoStringTokenToStringMap = new List<string>();

            foreach (string str in this.module.GetStrings())
            {
                _pseudoStringTokenToStringMap.Add(str);
            }

            _pseudoStringTokenToTokenMap = new int[_pseudoStringTokenToStringMap.Count];
        }

        protected virtual void CreateIndicesForModule()
        {
            var nestedTypes = new Queue<ITypeDefinition>();

            foreach (INamespaceTypeDefinition typeDef in this.GetTopLevelTypes(this.module))
            {
                this.CreateIndicesFor(typeDef, nestedTypes);
            }

            while (nestedTypes.Count > 0)
            {
                this.CreateIndicesFor(nestedTypes.Dequeue(), nestedTypes);
            }
        }

        private void CreateIndicesFor(ITypeDefinition typeDef, Queue<ITypeDefinition> nestedTypes)
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
            if (methodDef.ParameterCount == 0 && !(methodDef.ReturnValueIsMarshalledExplicitly || IteratorHelper.EnumerableIsNotEmpty(methodDef.ReturnValueAttributes)))
            {
                return ImmutableArray<IParameterDefinition>.Empty;
            }

            return GetParametersToEmitCore(methodDef);
        }

        private ImmutableArray<IParameterDefinition> GetParametersToEmitCore(IMethodDefinition methodDef)
        {
            var builder = ArrayBuilder<IParameterDefinition>.GetInstance();
            if (methodDef.ReturnValueIsMarshalledExplicitly || IteratorHelper.EnumerableIsNotEmpty(methodDef.ReturnValueAttributes))
            {
                builder.Add(new ReturnValueParameter(methodDef));
            }

            foreach (IParameterDefinition parDef in methodDef.Parameters)
            {
                // No explicit param row is needed if param has no flags (other than optionally IN),
                // no name and no references to the param row, such as CustomAttribute, Constant, or FieldMarshal
                if (parDef.HasDefaultValue || parDef.IsOptional || parDef.IsOut || parDef.IsMarshalledExplicitly ||
                    parDef.Name != String.Empty ||
                    IteratorHelper.EnumerableIsNotEmpty(parDef.GetAttributes(Context)))
                {
                    builder.Add(parDef);
                }
            }

            return builder.ToImmutableAndFree();
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
                this.GetOrAddAssemblyRefIndex(assemblyRef);
            }
        }

        private void CreateInitialExportedTypeIndex()
        {
            Debug.Assert(!_tableIndicesAreComplete);

            if (this.IsFullMetadata)
            {
                foreach (ITypeReference exportedType in this.module.GetExportedTypes(Context))
                {
                    if (!_exportedTypeIndex.ContainsKey(exportedType))
                    {
                        _exportedTypeList.Add(exportedType);
                        _exportedTypeIndex.Add(exportedType, _exportedTypeList.Count);
                    }
                }
            }
        }

        private void CreateInitialFileRefIndex()
        {
            Debug.Assert(!_tableIndicesAreComplete);
            IAssembly assembly = this.module.AsAssembly;
            if (assembly == null)
            {
                return;
            }

            foreach (IFileReference fileRef in assembly.GetFiles(Context))
            {
                string key = fileRef.FileName;
                if (!_fileRefIndex.ContainsKey(key))
                {
                    _fileRefList.Add(fileRef);
                    _fileRefIndex.Add(key, _fileRefList.Count);
                }
            }
        }

        internal int GetAssemblyRefIndex(IAssemblyReference assemblyReference)
        {
            var containingAssembly = this.module.GetContainingAssembly(Context);

            if (containingAssembly != null && ReferenceEquals(assemblyReference, containingAssembly))
            {
                return 0;
            }

            return this.GetOrAddAssemblyRefIndex(assemblyReference);
        }

        internal int GetModuleRefIndex(string moduleName)
        {
            return this.GetOrAddModuleRefIndex(moduleName);
        }

        private BlobIdx GetCustomAttributeSignatureIndex(ICustomAttribute customAttribute)
        {
            BlobIdx result;
            if (_customAttributeSignatureIndex.TryGetValue(customAttribute, out result))
            {
                return result;
            }

            var writer = PooledBlobBuilder.GetInstance();
            this.SerializeCustomAttributeSignature(customAttribute, writer);
            result = metadata.GetBlobIndex(writer);
            _customAttributeSignatureIndex.Add(customAttribute, result);
            writer.Free();
            return result;
        }

        private uint GetCustomAttributeTypeCodedIndex(IMethodReference methodReference)
        {
            IMethodDefinition methodDef = null;
            IUnitReference definingUnit = GetDefiningUnitReference(methodReference.GetContainingType(Context), Context);
            if (definingUnit != null && ReferenceEquals(definingUnit, this.module))
            {
                methodDef = methodReference.GetResolvedMethod(Context);
            }

            return methodDef != null
                ? GetMethodDefIndex(methodDef).ToCodedIndex(CustomAttributeTypeTag.MethodDef)
                : GetMemberRefIndex(methodReference).ToCodedIndex(CustomAttributeTypeTag.MemberRef);
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

        private int GetExportedTypeIndex(ITypeReference typeReference)
        {
            int result;
            if (_exportedTypeIndex.TryGetValue(typeReference, out result))
            {
                return result;
            }

            Debug.Assert(!_tableIndicesAreComplete);
            _exportedTypeList.Add(typeReference);
            _exportedTypeIndex.Add(typeReference, _exportedTypeList.Count);
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

        internal BlobIdx GetFieldSignatureIndex(IFieldReference fieldReference)
        {
            BlobIdx result;
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
            result = metadata.GetBlobIndex(writer);
            _fieldSignatureIndex.Add(fieldReference, result);
            writer.Free();
            return result;
        }

        internal virtual int GetFieldToken(IFieldReference fieldReference)
        {
            IFieldDefinition fieldDef = null;
            IUnitReference definingUnit = GetDefiningUnitReference(fieldReference.GetContainingType(Context), Context);
            if (definingUnit != null && ReferenceEquals(definingUnit, this.module))
            {
                fieldDef = fieldReference.GetResolvedField(Context);
            }

            return fieldDef != null
                ? 0x04000000 | this.GetFieldDefIndex(fieldDef)
                : 0x0A000000 | this.GetMemberRefIndex(fieldReference);
        }

        internal int GetFileRefIndex(IFileReference fileReference)
        {
            string key = fileReference.FileName;
            int result;
            if (_fileRefIndex.TryGetValue(key, out result))
            {
                return result;
            }

            Debug.Assert(!_tableIndicesAreComplete);
            _fileRefList.Add(fileReference);
            _fileRefIndex.Add(key, _fileRefList.Count);
            return result;
        }

        private int GetFileRefIndex(IModuleReference mref)
        {
            string key = mref.Name;
            int result;
            if (_fileRefIndex.TryGetValue(key, out result))
            {
                return result;
            }

            Debug.Assert(false);

            // TODO: error
            return result;
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

        private uint GetImplementationCodedIndex(INamespaceTypeReference namespaceRef)
        {
            IUnitReference uref = namespaceRef.GetUnit(Context);
            IAssemblyReference aref = uref as IAssemblyReference;
            if (aref != null)
            {
                return GetAssemblyRefIndex(aref).ToCodedIndex(ImplementationTag.AssemblyRef);
            }

            IModuleReference mref = uref as IModuleReference;
            if (mref != null)
            {
                aref = mref.GetContainingAssembly(Context);
                return aref == null || ReferenceEquals(aref, this.module.GetContainingAssembly(Context))
                    ? GetFileRefIndex(mref).ToCodedIndex(ImplementationTag.File)
                    : GetAssemblyRefIndex(aref).ToCodedIndex(ImplementationTag.AssemblyRef);
            }

            Debug.Assert(false);

            // TODO: error
            return 0;
        }

        private static uint GetManagedResourceOffset(ManagedResource resource, BlobBuilder resourceWriter)
        {
            if (resource.ExternalFile != null)
            {
                return resource.Offset;
            }

            int result = resourceWriter.Position;
            resource.WriteData(resourceWriter);
            return (uint)result;
        }

        public static string GetMangledName(INamedTypeReference namedType)
        {
            string unmangledName = namedType.Name;

            return namedType.MangleName
                ? MetadataHelpers.ComposeAritySuffixedMetadataName(unmangledName, namedType.GenericParameterCount)
                : unmangledName;
        }

        internal int GetMemberRefIndex(ITypeMemberReference memberRef)
        {
            return this.GetOrAddMemberRefIndex(memberRef);
        }

        internal uint GetMemberRefParentCodedIndex(ITypeMemberReference memberRef)
        {
            ITypeDefinition parentTypeDef = memberRef.GetContainingType(Context).AsTypeDefinition(Context);
            if (parentTypeDef != null)
            {
                int parentTypeDefIndex;
                this.TryGetTypeDefIndex(parentTypeDef, out parentTypeDefIndex);
                if (parentTypeDefIndex > 0)
                {
                    if (memberRef is IFieldReference)
                    {
                        return parentTypeDefIndex.ToCodedIndex(MemberRefParentTag.TypeDef);
                    }

                    IMethodReference methodRef = memberRef as IMethodReference;
                    if (methodRef != null)
                    {
                        if (methodRef.AcceptsExtraArguments)
                        {
                            int methodIndex;
                            if (this.TryGetMethodDefIndex(methodRef.GetResolvedMethod(Context), out methodIndex))
                            {
                                return methodIndex.ToCodedIndex(MemberRefParentTag.MethodDef);
                            }
                        }

                        return parentTypeDefIndex.ToCodedIndex(MemberRefParentTag.TypeDef);
                    }
                    // TODO: error
                }
            }

            // TODO: special treatment for global fields and methods. Object model support would be nice.
            return memberRef.GetContainingType(Context).IsTypeSpecification()
                ? GetTypeSpecIndex(memberRef.GetContainingType(Context)).ToCodedIndex(MemberRefParentTag.TypeSpec)
                : GetTypeRefIndex(memberRef.GetContainingType(Context)).ToCodedIndex(MemberRefParentTag.TypeRef);
        }

        internal uint GetMethodDefOrRefCodedIndex(IMethodReference methodReference)
        {
            IMethodDefinition methodDef = null;
            IUnitReference definingUnit = GetDefiningUnitReference(methodReference.GetContainingType(Context), Context);
            if (definingUnit != null && ReferenceEquals(definingUnit, this.module))
            {
                methodDef = methodReference.GetResolvedMethod(Context);
            }

            return methodDef != null
                ? GetMethodDefIndex(methodDef).ToCodedIndex(MethodDefOrRefTag.MethodDef)
                : GetMemberRefIndex(methodReference).ToCodedIndex(MethodDefOrRefTag.MemberRef);
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

        internal BlobIdx GetMethodInstanceSignatureIndex(IGenericMethodInstanceReference methodInstanceReference)
        {
            BlobIdx result;
            if (_methodInstanceSignatureIndex.TryGetValue(methodInstanceReference, out result))
            {
                return result;
            }

            var builder = PooledBlobBuilder.GetInstance();
            var encoder = new BlobEncoder(builder).MethodSpecificationSignature(methodInstanceReference.GetGenericMethod(Context).GenericParameterCount);

            foreach (ITypeReference typeReference in methodInstanceReference.GetGenericArguments(Context))
            {
                var typeRef = typeReference;
                encoder = SerializeTypeReference(SerializeTypeReferenceModifiers(encoder.AddArgument(), ref typeRef), typeRef);
            }

            encoder.EndArguments();

            result = metadata.GetBlobIndex(builder);
            _methodInstanceSignatureIndex.Add(methodInstanceReference, result);
            builder.Free();
            return result;
        }

        private BlobIdx GetMarshallingDescriptorIndex(IMarshallingInformation marshallingInformation)
        {
            BlobIdx result;
            if (_marshallingDescriptorIndex.TryGetValue(marshallingInformation, out result))
            {
                return result;
            }

            var writer = PooledBlobBuilder.GetInstance();
            this.SerializeMarshallingDescriptor(marshallingInformation, writer);
            result = metadata.GetBlobIndex(writer);
            _marshallingDescriptorIndex.Add(marshallingInformation, result);
            writer.Free();
            return result;
        }

        private BlobIdx GetMarshallingDescriptorIndex(ImmutableArray<byte> descriptor)
        {
            return metadata.GetBlobIndex(descriptor);
        }

        private BlobIdx GetMemberRefSignatureIndex(ITypeMemberReference memberRef)
        {
            IFieldReference fieldReference = memberRef as IFieldReference;
            if (fieldReference != null)
            {
                return this.GetFieldSignatureIndex(fieldReference);
            }

            IMethodReference methodReference = memberRef as IMethodReference;
            if (methodReference != null)
            {
                return this.GetMethodSignatureIndex(methodReference);
            }

            // TODO: error
            return default(BlobIdx);
        }

        internal BlobIdx GetMethodSignatureIndex(IMethodReference methodReference)
        {
            ImmutableArray<byte> signatureBlob;
            return GetMethodSignatureIndexAndBlob(methodReference, out signatureBlob);
        }

        internal byte[] GetMethodSignature(IMethodReference methodReference)
        {
            ImmutableArray<byte> signatureBlob;
            GetMethodSignatureIndexAndBlob(methodReference, out signatureBlob);
            return signatureBlob.ToArray();
        }

        private BlobIdx GetMethodSignatureIndexAndBlob(IMethodReference methodReference, out ImmutableArray<byte> signatureBlob)
        {
            BlobIdx result;
            ISpecializedMethodReference specializedMethodReference = methodReference.AsSpecializedMethodReference;
            if (specializedMethodReference != null)
            {
                methodReference = specializedMethodReference.UnspecializedVersion;
            }

            KeyValuePair<BlobIdx, ImmutableArray<byte>> existing;
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
            result = metadata.GetBlobIndex(signatureBlob);
            _signatureIndex.Add(methodReference, KeyValuePair.Create(result, signatureBlob));
            builder.Free();
            return result;
        }

        private BlobIdx GetGenericMethodInstanceIndex(IGenericMethodInstanceReference genericMethodInstanceReference)
        {
            var writer = PooledBlobBuilder.GetInstance();
            this.SerializeGenericMethodInstanceSignature(writer, genericMethodInstanceReference);
            BlobIdx result = metadata.GetBlobIndex(writer);
            writer.Free();
            return result;
        }

        private int GetMethodSpecIndex(IGenericMethodInstanceReference methodSpec)
        {
            return this.GetOrAddMethodSpecIndex(methodSpec);
        }

        internal virtual int GetMethodToken(IMethodReference methodReference)
        {
            int methodDefIndex;
            IMethodDefinition methodDef = null;
            IUnitReference definingUnit = GetDefiningUnitReference(methodReference.GetContainingType(Context), Context);
            if (definingUnit != null && ReferenceEquals(definingUnit, this.module))
            {
                methodDef = methodReference.GetResolvedMethod(Context);
            }

            if (methodDef != null && (methodReference == methodDef || !methodReference.AcceptsExtraArguments) && this.TryGetMethodDefIndex(methodDef, out methodDefIndex))
            {
                return 0x06000000 | methodDefIndex;
            }

            IGenericMethodInstanceReference methodSpec = methodReference.AsGenericMethodInstanceReference;
            return methodSpec != null
                ? 0x2B000000 | this.GetMethodSpecIndex(methodSpec)
                : 0x0A000000 | this.GetMemberRefIndex(methodReference);
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

        internal PrimitiveTypeCode GetConstantTypeCode(ILocalDefinition constant)
        {
            return constant.CompileTimeValue.Type.TypeCode(Context);
        }

        private BlobIdx GetPermissionSetIndex(ImmutableArray<ICustomAttribute> permissionSet)
        {
            var writer = PooledBlobBuilder.GetInstance();
            BlobIdx result;
            try
            {
                writer.WriteByte((byte)'.');
                writer.WriteCompressedInteger((uint)permissionSet.Length);
                this.SerializePermissionSet(permissionSet, writer);
                result = metadata.GetBlobIndex(writer);
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

        private BlobIdx GetPropertySignatureIndex(IPropertyDefinition propertyDef)
        {
            KeyValuePair<BlobIdx, ImmutableArray<byte>> existing;
            if (_signatureIndex.TryGetValue(propertyDef, out existing))
            {
                return existing.Key;
            }

            var builder = PooledBlobBuilder.GetInstance();

            var encoder = new BlobEncoder(builder).PropertySignature(
                isInstanceProperty: (propertyDef.CallingConvention & CallingConvention.HasThis) != 0);

            SerializeReturnValueAndParameters(encoder, propertyDef, ImmutableArray<IParameterTypeInformation>.Empty);

            var blob = builder.ToImmutableArray();
            var result = metadata.GetBlobIndex(blob);

            _signatureIndex.Add(propertyDef, KeyValuePair.Create(result, blob));
            builder.Free();
            return result;
        }

        private uint GetResolutionScopeCodedIndex(IUnitReference unitReference)
        {
            IAssemblyReference aref = unitReference as IAssemblyReference;
            if (aref != null)
            {
                return GetAssemblyRefIndex(aref).ToCodedIndex(ResolutionScopeTag.AssemblyRef);
            }

            IModuleReference mref = unitReference as IModuleReference;
            if (mref != null)
            {
                // If this is a module from a referenced multi-module assembly,
                // the assembly should be used as the resolution scope.
                aref = mref.GetContainingAssembly(Context);

                if (aref != null && aref != module.AsAssembly)
                {
                    return GetAssemblyRefIndex(aref).ToCodedIndex(ResolutionScopeTag.AssemblyRef);
                }

                return GetModuleRefIndex(mref.Name).ToCodedIndex(ResolutionScopeTag.ModuleRef);
            }

            // TODO: error
            return 0;
        }

        private StringIdx GetStringIndexForPathAndCheckLength(string path, INamedEntity errorEntity = null)
        {
            CheckPathLength(path, errorEntity);
            return metadata.GetStringIndex(path);
        }

        private StringIdx GetStringIndexForNameAndCheckLength(string name, INamedEntity errorEntity = null)
        {
            CheckNameLength(name, errorEntity);
            return metadata.GetStringIndex(name);
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
        private StringIdx GetStringIndexForNamespaceAndCheckLength(INamespaceTypeReference namespaceType, string mangledTypeName)
        {
            string namespaceName = namespaceType.NamespaceName;
            if (namespaceName.Length == 0) // Optimization: CheckNamespaceLength is relatively expensive.
            {
                return default(StringIdx);
            }

            CheckNamespaceLength(namespaceName, mangledTypeName, namespaceType);
            return metadata.GetStringIndex(namespaceName);
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

        private uint GetTypeDefOrRefCodedIndex(ITypeReference typeReference, bool treatRefAsPotentialTypeSpec)
        {
            int typeDefIndex;
            var typeDefinition = typeReference.AsTypeDefinition(this.Context);
            if ((typeDefinition != null) && this.TryGetTypeDefIndex(typeDefinition, out typeDefIndex))
            {
                return typeDefIndex.ToCodedIndex(TypeDefOrRefTag.TypeDef); 
            }

            return treatRefAsPotentialTypeSpec && typeReference.IsTypeSpecification()
                ? GetTypeSpecIndex(typeReference).ToCodedIndex(TypeDefOrRefTag.TypeSpec)
                : GetTypeRefIndex(typeReference).ToCodedIndex(TypeDefOrRefTag.TypeRef);
        }

        private uint GetTypeOrMethodDefCodedIndex(IGenericParameter genPar)
        {
            IGenericTypeParameter genTypePar = genPar.AsGenericTypeParameter;
            if (genTypePar != null)
            {
                return GetTypeDefIndex(genTypePar.DefiningType).ToCodedIndex(TypeOrMethodDefTag.TypeDef);
            }

            IGenericMethodParameter genMethPar = genPar.AsGenericMethodParameter;
            if (genMethPar != null)
            {
                return GetMethodDefIndex(genMethPar.DefiningMethod).ToCodedIndex(TypeOrMethodDefTag.MethodDef);
            }  
            
            // TODO: error
            return 0;
        }

        private int GetTypeRefIndex(ITypeReference typeReference)
        {
            int result;
            if (this.TryGetTypeRefIndex(typeReference, out result))
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
                GetTypeRefIndex(nestedTypeRef.GetContainingType(this.Context));
            }

            return this.GetOrAddTypeRefIndex(typeReference);
        }

        private int GetTypeSpecIndex(ITypeReference typeReference)
        {
            return this.GetOrAddTypeSpecIndex(typeReference);
        }

        internal ITypeDefinition GetTypeDefinition(uint token)
        {
            // The token must refer to a TypeDef row since we are
            // only handling indexes into the full metadata (in EnC)
            // for def tables. Other tables contain deltas only.
            Debug.Assert(TypeOnly(token) == TokenTypeIds.TypeDef);
            int index = (int)RowOnly(token) - 1;
            return this.GetTypeDef(index);
        }

        internal IMethodDefinition GetMethodDefinition(uint token)
        {
            // Must be a def table. (See comment in GetTypeDefinition.)
            Debug.Assert(TypeOnly(token) == TokenTypeIds.MethodDef);
            int index = (int)RowOnly(token) - 1;
            return this.GetMethodDef(index);
        }

        internal INestedTypeReference GetNestedTypeReference(uint token)
        {
            // Must be a def table. (See comment in GetTypeDefinition.)
            Debug.Assert(TypeOnly(token) == TokenTypeIds.TypeDef);
            int index = (int)RowOnly(token) - 1;
            var t = this.GetTypeDef(index);
            return t.AsNestedTypeReference;
        }

        internal BlobIdx GetTypeSpecSignatureIndex(ITypeReference typeReference)
        {
            BlobIdx result;
            if (_typeSpecSignatureIndex.TryGetValue(typeReference, out result))
            {
                return result;
            }

            var builder = PooledBlobBuilder.GetInstance();
            this.SerializeTypeReference(new BlobEncoder(builder).TypeSpecificationSignature(), typeReference);
            result = metadata.GetBlobIndex(builder);

            _typeSpecSignatureIndex.Add(typeReference, result);
            builder.Free();
            return result;
        }

        internal void RecordTypeReference(ITypeReference typeReference)
        {
            var typeDefinition = typeReference.AsTypeDefinition(this.Context);
            int token;
            if ((typeDefinition != null) && this.TryGetTypeDefIndex(typeDefinition, out token))
            {
                return;
            }

            if (!typeReference.IsTypeSpecification())
            {
                this.GetTypeRefIndex(typeReference);
            }
            else
            {
                this.GetTypeSpecIndex(typeReference);
            }
        }

        internal virtual int GetTypeToken(ITypeReference typeReference)
        {
            int typeDefIndex;
            var typeDefinition = typeReference.AsTypeDefinition(this.Context);
            if (typeDefinition != null && this.TryGetTypeDefIndex(typeDefinition, out typeDefIndex))
            {
                return 0x02000000 | typeDefIndex;
            }

            return typeReference.IsTypeSpecification()
                ? 0x1B000000 | this.GetTypeSpecIndex(typeReference)
                : 0x01000000 | this.GetTypeRefIndex(typeReference);
        }

        internal int GetTokenForDefinition(IDefinition definition)
        {
            ITypeDefinition typeDef = definition as ITypeDefinition;
            if (typeDef != null)
            {
                return TokenTypeIds.TypeDef | this.GetTypeDefIndex(typeDef);
            }

            IMethodDefinition methodDef = definition as IMethodDefinition;
            if (methodDef != null)
            {
                return TokenTypeIds.MethodDef | this.GetMethodDefIndex(methodDef);
            }

            IFieldDefinition fieldDef = definition as IFieldDefinition;
            if (fieldDef != null)
            {
                return TokenTypeIds.FieldDef | this.GetFieldDefIndex(fieldDef);
            }

            IEventDefinition eventDef = definition as IEventDefinition;
            if (eventDef != null)
            {
                return TokenTypeIds.Event | this.GetEventDefIndex(eventDef);
            }

            IPropertyDefinition propertyDef = definition as IPropertyDefinition;
            if (propertyDef != null)
            {
                return TokenTypeIds.Property | this.GetPropertyDefIndex(propertyDef);
            }

            throw ExceptionUtilities.Unreachable;
        }

        public void WriteMetadataAndIL(PdbWriter pdbWriterOpt, Stream metadataStream, Stream ilStream, out MetadataSizes metadataSizes)
        {
            pdbWriterOpt?.SetMetadataEmitter(this);

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
            Debug.Assert(this.module.Properties.PersistentIdentifier != default(Guid));

            BuildMetadataAndIL(pdbWriterOpt, ilBuilder, mappedFieldDataBuilder, managedResourceDataBuilder);

            Debug.Assert(mappedFieldDataBuilder.Count == 0);
            Debug.Assert(managedResourceDataBuilder.Count == 0);

            var serializer = new TypeSystemMetadataSerializer(metadata, module.Properties.TargetRuntimeVersion, IsMinimalDelta);
            serializer.SerializeMetadata(metadataBuilder, methodBodyStreamRva: 0, mappedFieldDataStreamRva: 0);
            metadataSizes = serializer.MetadataSizes;

            ilBuilder.WriteContentTo(ilStream);
            metadataBuilder.WriteContentTo(metadataStream);
        }

        public void BuildMetadataAndIL(
            PdbWriter nativePdbWriterOpt,
            BlobBuilder ilBuilder,
            BlobBuilder mappedFieldDataBuilder,
            BlobBuilder managedResourceDataBuilder)
        {
            // Extract information from object model into tables, indices and streams
            CreateIndices();

            if (_debugMetadataOpt != null)
            {
                DefineModuleImportScope();
            }

            int[] methodBodyOffsets = SerializeMethodBodies(ilBuilder, nativePdbWriterOpt);

            _cancellationToken.ThrowIfCancellationRequested();

            // method body serialization adds Stand Alone Signatures
            _tableIndicesAreComplete = true;

            ReportReferencesToAddedSymbols();

            PopulateTables(methodBodyOffsets, mappedFieldDataBuilder, managedResourceDataBuilder);
        }

        public void SerializeManagedTextSection(
            BlobBuilder metadataBuilder,
            BlobBuilder ilBuilder,
            BlobBuilder mappedFieldDataBuilder,
            BlobBuilder managedResourceDataBuilder,
            Characteristics imageCharacteristics,
            Machine machine,
            int textSectionRva,
            string pdbPathOpt,
            out int moduleVersionIdOffsetInMetadataStream,
            out ManagedTextSection textSection,
            out MetadataSizes metadataSizes)
        {
            var serializer = new TypeSystemMetadataSerializer(metadata, module.Properties.TargetRuntimeVersion, IsMinimalDelta);

            metadataSizes = serializer.MetadataSizes;

            int methodBodyStreamRva;
            int mappedFieldDataStreamRva;

            textSection = new ManagedTextSection(
                metadataSizes.MetadataSize,
                ilStreamSize: ilBuilder.Count,
                mappedFieldDataSize: mappedFieldDataBuilder.Count,
                resourceDataSize: managedResourceDataBuilder.Count,
                strongNameSignatureSize: CalculateStrongNameSignatureSize(module),
                imageCharacteristics: imageCharacteristics,
                machine: machine,
                pdbPathOpt: pdbPathOpt,
                isDeterministic: _deterministic);

            methodBodyStreamRva = textSectionRva + textSection.OffsetToILStream;
            mappedFieldDataStreamRva = textSectionRva + textSection.CalculateOffsetToMappedFieldDataStream();

            serializer.SerializeMetadata(metadataBuilder, methodBodyStreamRva, mappedFieldDataStreamRva);
            moduleVersionIdOffsetInMetadataStream = serializer.ModuleVersionIdOffset;
        }

        public void SerializeStandaloneDebugMetadata(
            BlobBuilder debugMetadataBuilderOpt,
            MetadataSizes metadataSizes,
            int debugEntryPointToken,
            out int pdbIdOffsetInPortablePdbStream)
        {
            Debug.Assert(_debugMetadataOpt != null);

            var debugSerializer = new StandaloneDebugMetadataSerializer(_debugMetadataOpt, metadataSizes.RowCounts, debugEntryPointToken, IsMinimalDelta);
            debugSerializer.SerializeMetadata(debugMetadataBuilderOpt);
            pdbIdOffsetInPortablePdbStream = debugSerializer.PdbIdOffset;
        }

        internal void GetEntryPointTokens(out int entryPointToken, out int debugEntryPointToken)
        {
            if (IsFullMetadata)
            {
                // PE entry point is set for executable programs
                IMethodReference entryPoint = module.PEEntryPoint;
                entryPointToken = entryPoint != null ? GetMethodToken((IMethodDefinition)entryPoint.AsDefinition(Context)) : 0;

                // debug entry point may be different from PE entry point, it may also be set for libraries
                IMethodReference debugEntryPoint = module.DebugEntryPoint;
                if (debugEntryPoint != null && debugEntryPoint != entryPoint)
                {
                    debugEntryPointToken = GetMethodToken((IMethodDefinition)debugEntryPoint.AsDefinition(Context));
                }
                else
                {
                    debugEntryPointToken = entryPointToken;
                }
            }
            else
            {
                entryPointToken = debugEntryPointToken = 0;
            }
        }

        private static int CalculateStrongNameSignatureSize(IModule module)
        {
            IAssembly assembly = module.AsAssembly;
            if (assembly == null)
            {
                return 0;
            }

            // EDMAURER the count of characters divided by two because the each pair of characters will turn in to one byte.
            int keySize = (assembly.SignatureKey == null) ? 0 : assembly.SignatureKey.Length / 2;

            if (keySize == 0)
            {
                keySize = assembly.PublicKey.Length;
            }

            if (keySize == 0)
            {
                return 0;
            }

            return (keySize < 128 + 32) ? 128 : keySize - 32;
        }

        private ImmutableArray<IGenericParameter> GetSortedGenericParameters()
        {
            return GetGenericParameters().OrderBy((x, y) =>
            {
                // Spec: GenericParam table is sorted by Owner and then by Number.
                int result = (int)GetTypeOrMethodDefCodedIndex(x) - (int)GetTypeOrMethodDefCodedIndex(y);
                if (result != 0)
                {
                    return result;
                }

                return x.Index - y.Index;
            }).ToImmutableArray();
        }

        private void PopulateTables(int[] methodBodyOffsets, BlobBuilder mappedFieldDataWriter, BlobBuilder resourceWriter)
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
            this.PopulateManifestResourceTableRows(resourceWriter);
            this.PopulateMemberRefTableRows();
            this.PopulateMethodImplTableRows();
            this.PopulateMethodTableRows(methodBodyOffsets);
            this.PopulateMethodSemanticsTableRows();
            this.PopulateMethodSpecTableRows();
            this.PopulateModuleRefTableRows();
            this.PopulateModuleTableRow();
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

            ImmutableArray<int> rowCounts = metadata.GetRowCounts();
            Debug.Assert(rowCounts[(int)TableIndex.EncLog] == 0 && rowCounts[(int)TableIndex.EncMap] == 0);

            this.PopulateEncLogTableRows(rowCounts);
            this.PopulateEncMapTableRows(rowCounts);
        }

        private void PopulateAssemblyRefTableRows()
        {
            var assemblyRefs = this.GetAssemblyRefs();
            metadata.SetCapacity(TableIndex.AssemblyRef, assemblyRefs.Count);

            foreach (var assemblyRef in assemblyRefs)
            {
                Debug.Assert(assemblyRef.Version != null);
                Debug.Assert(!string.IsNullOrEmpty(assemblyRef.Name));
                
                // reference has token, not full public key
                metadata.AddAssemblyReference(
                    name: GetStringIndexForPathAndCheckLength(assemblyRef.Name, assemblyRef),
                    version: assemblyRef.Version,
                    culture: metadata.GetStringIndex(assemblyRef.Culture),
                    publicKeyOrToken: metadata.GetBlobIndex(assemblyRef.PublicKeyToken),
                    flags: (AssemblyFlags)((int)assemblyRef.ContentType << 9) | (assemblyRef.IsRetargetable ? AssemblyFlags.Retargetable : 0),
                    hashValue: default(BlobIdx));
            }
        }

        /// <summary>
        /// Compares quality of assembly references to achieve unique rows in AssemblyRef table.
        /// Metadata spec: "The AssemblyRef table shall contain no duplicates (where duplicate rows are deemed to 
        /// be those having the same MajorVersion, MinorVersion, BuildNumber, RevisionNumber, PublicKeyOrToken, 
        /// Name, and Culture)".
        /// </summary>
        protected sealed class AssemblyReferenceComparer : IEqualityComparer<IAssemblyReference>
        {
            internal static readonly AssemblyReferenceComparer Instance = new AssemblyReferenceComparer();

            public bool Equals(IAssemblyReference x, IAssemblyReference y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                return
                    x.Version.Equals(y.Version) &&
                    ByteSequenceComparer.Equals(x.PublicKeyToken, y.PublicKeyToken) &&
                    x.Name == y.Name &&
                    x.Culture == y.Culture;
            }

            public int GetHashCode(IAssemblyReference reference)
            {
                return Hash.Combine(reference.Version,
                       Hash.Combine(ByteSequenceComparer.GetHashCode(reference.PublicKeyToken),
                       Hash.Combine(reference.Name.GetHashCode(),
                       Hash.Combine(reference.Culture, 0))));
            }
        }

        private void PopulateAssemblyTableRows()
        {
            if (!EmitAssemblyDefinition)
            {
                return;
            }

            IAssembly assembly = this.module.AsAssembly;

            metadata.AddAssembly(
                flags: assembly.Flags,
                hashAlgorithm: assembly.HashAlgorithm,
                version: assembly.Version,
                publicKey: metadata.GetBlobIndex(assembly.PublicKey),
                name: GetStringIndexForPathAndCheckLength(assembly.Name, assembly),
                culture: metadata.GetStringIndex(assembly.Culture));
        }
        
        private void PopulateCustomAttributeTableRows(ImmutableArray<IGenericParameter> sortedGenericParameters)
        {
            if (this.IsFullMetadata)
            {
                this.AddAssemblyAttributesToTable();
            }

            this.AddCustomAttributesToTable(this.GetMethodDefs(), HasCustomAttributeTag.MethodDef, this.GetMethodDefIndex);
            this.AddCustomAttributesToTable(this.GetFieldDefs(), HasCustomAttributeTag.Field, this.GetFieldDefIndex);

            // this.AddCustomAttributesToTable(this.typeRefList, 2);
            this.AddCustomAttributesToTable(this.GetTypeDefs(), HasCustomAttributeTag.TypeDef, this.GetTypeDefIndex);
            this.AddCustomAttributesToTable(this.GetParameterDefs(), HasCustomAttributeTag.Param, this.GetParameterDefIndex);

            // TODO: attributes on interface implementation entries 5
            // TODO: attributes on member reference entries 6
            if (this.IsFullMetadata)
            {
                this.AddModuleAttributesToTable(this.module, HasCustomAttributeTag.Module);
            }

            // TODO: declarative security entries 8
            this.AddCustomAttributesToTable(this.GetPropertyDefs(), HasCustomAttributeTag.Property, this.GetPropertyDefIndex);
            this.AddCustomAttributesToTable(this.GetEventDefs(), HasCustomAttributeTag.Event, this.GetEventDefIndex);

            // TODO: standalone signature entries 11
            if (this.IsFullMetadata)
            {
                this.AddCustomAttributesToTable(this.module.ModuleReferences, HasCustomAttributeTag.ModuleRef);
            }

            // TODO: type spec entries 13
            // this.AddCustomAttributesToTable(this.module.AssemblyReferences, 15);
            // TODO: this.AddCustomAttributesToTable(assembly.Files, 16);
            // TODO: exported types 17
            // TODO: this.AddCustomAttributesToTable(assembly.Resources, 18);

            this.AddCustomAttributesToTable(sortedGenericParameters, HasCustomAttributeTag.GenericParam);
        }

        private void AddAssemblyAttributesToTable()
        {
            bool writingNetModule = (null == this.module.AsAssembly);
            if (writingNetModule)
            {
                // When writing netmodules, assembly security attributes are not emitted by PopulateDeclSecurityTableRows().
                // Instead, here we make sure they are emitted as regular attributes, attached off the appropriate placeholder
                // System.Runtime.CompilerServices.AssemblyAttributesGoHere* type refs.  This is the contract for publishing
                // assembly attributes in netmodules so they may be migrated to containing/referencing multi-module assemblies,
                // at multi-module assembly build time.
                AddAssemblyAttributesToTable(
                    this.module.AssemblySecurityAttributes.Select(sa => sa.Attribute),
                    true,               // needsDummyParent
                    true);              // isSecurity
            }

            AddAssemblyAttributesToTable(
                this.module.AssemblyAttributes,
                writingNetModule,   // needsDummyParent
                false);             // IsSecurity
        }

        private void AddAssemblyAttributesToTable(IEnumerable<ICustomAttribute> assemblyAttributes, bool needsDummyParent, bool isSecurity)
        {
            Debug.Assert(this.IsFullMetadata); // parentToken is not relative
            uint parentToken = 1.ToCodedIndex(HasCustomAttributeTag.Assembly);
            foreach (ICustomAttribute customAttribute in assemblyAttributes)
            {
                if (needsDummyParent)
                {
                    // When writing netmodules, assembly attributes are attached off the appropriate placeholder
                    // System.Runtime.CompilerServices.AssemblyAttributesGoHere* type refs.  This is the contract for publishing
                    // assembly attributes in netmodules so they may be migrated to containing/referencing multi-module assemblies,
                    // at multi-module assembly build time.
                    parentToken = GetDummyAssemblyAttributeParent(isSecurity, customAttribute.AllowMultiple);
                }
                AddCustomAttributeToTable(parentToken, customAttribute);
            }
        }

        private uint GetDummyAssemblyAttributeParent(bool isSecurity, bool allowMultiple)
        {
            // Lazily get or create placeholder assembly attribute parent type ref for the given combination of
            // whether isSecurity and allowMultiple.  Convert type ref row id to corresponding attribute parent tag.
            // Note that according to the defacto contract, although the placeholder type refs have CorLibrary as their
            // resolution scope, the types backing the placeholder type refs need not actually exist.
            int iS = isSecurity ? 1 : 0;
            int iM = allowMultiple ? 1 : 0;
            if (_dummyAssemblyAttributeParent[iS, iM] == 0)
            {
                int rowId = metadata.AddTypeReference(
                    resolutionScope: GetResolutionScopeCodedIndex(module.GetCorLibrary(Context)),
                    @namespace: metadata.GetStringIndex(dummyAssemblyAttributeParentNamespace),
                    name: metadata.GetStringIndex(dummyAssemblyAttributeParentName + dummyAssemblyAttributeParentQualifier[iS, iM]));

                _dummyAssemblyAttributeParent[iS, iM] = rowId.ToCodedIndex(HasCustomAttributeTag.TypeRef);
            }

            return _dummyAssemblyAttributeParent[iS, iM];
        }

        private void AddModuleAttributesToTable(IModule module, HasCustomAttributeTag tag)
        {
            Debug.Assert(this.IsFullMetadata); // parentToken is not relative
            uint parentToken = 1.ToCodedIndex(tag);
            foreach (ICustomAttribute customAttribute in module.ModuleAttributes)
            {
                AddCustomAttributeToTable(parentToken, customAttribute);
            }
        }

        private void AddCustomAttributesToTable<T>(IEnumerable<T> parentList, HasCustomAttributeTag tag)
            where T : IReference
        {
            int parentIndex = 0;
            foreach (var parent in parentList)
            {
                parentIndex++;
                uint parentToken = parentIndex.ToCodedIndex(tag);
                foreach (ICustomAttribute customAttribute in parent.GetAttributes(Context))
                {
                    AddCustomAttributeToTable(parentToken, customAttribute);
                }
            }
        }

        private void AddCustomAttributesToTable<T>(IEnumerable<T> parentList, HasCustomAttributeTag tag, Func<T, int> getDefIndex)
            where T : IReference
        {
            foreach (var parent in parentList)
            {
                uint parentToken = getDefIndex(parent).ToCodedIndex(tag);
                foreach (ICustomAttribute customAttribute in parent.GetAttributes(Context))
                {
                    AddCustomAttributeToTable(parentToken, customAttribute);
                }
            }
        }

        private void AddCustomAttributeToTable(uint parentCodedIndex, ICustomAttribute customAttribute)
        {
            metadata.AddCustomAttribute(
                parent: parentCodedIndex,
                constructor: GetCustomAttributeTypeCodedIndex(customAttribute.Constructor(Context)),
                value: GetCustomAttributeSignatureIndex(customAttribute));
        }

        private void PopulateDeclSecurityTableRows()
        {
            IAssembly assembly = this.module.AsAssembly;
            if (assembly != null)
            {
                this.PopulateDeclSecurityTableRowsFor(1, HasDeclSecurityTag.Assembly, assembly.AssemblySecurityAttributes);
            }

            foreach (ITypeDefinition typeDef in this.GetTypeDefs())
            {
                if (!typeDef.HasDeclarativeSecurity)
                {
                    continue;
                }

                this.PopulateDeclSecurityTableRowsFor(GetTypeDefIndex(typeDef), HasDeclSecurityTag.TypeDef, typeDef.SecurityAttributes);
            }

            foreach (IMethodDefinition methodDef in this.GetMethodDefs())
            {
                if (!methodDef.HasDeclarativeSecurity)
                {
                    continue;
                }

                this.PopulateDeclSecurityTableRowsFor(GetMethodDefIndex(methodDef), HasDeclSecurityTag.MethodDef, methodDef.SecurityAttributes);
            }
        }

        private void PopulateDeclSecurityTableRowsFor(int parentIndex, HasDeclSecurityTag tag, IEnumerable<SecurityAttribute> attributes)
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

            uint parent = parentIndex.ToCodedIndex(tag);
            foreach (DeclarativeSecurityAction securityAction in groupedSecurityAttributes.Keys)
            {
                metadata.AddDeclarativeSecurityAttribute(
                    parent: parent,
                    action: securityAction,
                    permissionSet: GetPermissionSetIndex(groupedSecurityAttributes[securityAction]));
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
                    name: GetStringIndexForNameAndCheckLength(eventDef.Name, eventDef),
                    type: GetTypeDefOrRefCodedIndex(eventDef.GetType(Context), true));
            }
        }

        private void PopulateExportedTypeTableRows()
        {
            if (this.IsFullMetadata)
            {
                metadata.SetCapacity(TableIndex.ExportedType, NumberOfTypeDefsEstimate);

                foreach (ITypeReference exportedType in this.module.GetExportedTypes(Context))
                {
                    INestedTypeReference nestedRef;
                    INamespaceTypeReference namespaceTypeRef;

                    TypeFlags flags;
                    int typeDefinitionId = MetadataTokens.GetToken(exportedType.TypeDef);
                    StringIdx typeName;
                    StringIdx typeNamespace;
                    uint implementation;

                    if ((namespaceTypeRef = exportedType.AsNamespaceTypeReference) != null)
                    {
                        flags = TypeFlags.PublicAccess;

                        string mangledTypeName = GetMangledName(namespaceTypeRef);
                        typeName = this.GetStringIndexForNameAndCheckLength(mangledTypeName, namespaceTypeRef);
                        typeNamespace = this.GetStringIndexForNamespaceAndCheckLength(namespaceTypeRef, mangledTypeName);
                        implementation = GetImplementationCodedIndex(namespaceTypeRef);

                        if ((implementation & 1) == (int)ImplementationTag.AssemblyRef)
                        {
                            flags = TypeFlags.PrivateAccess | TypeFlags.ForwarderImplementation;
                            typeDefinitionId = 0; // Must be cleared for type forwarders.
                        }
                    }
                    else if ((nestedRef = exportedType.AsNestedTypeReference) != null)
                    {
                        flags = TypeFlags.NestedPublicAccess;
                        typeName = this.GetStringIndexForNameAndCheckLength(GetMangledName(nestedRef), nestedRef);
                        typeNamespace = default(StringIdx);

                        ITypeReference containingType = nestedRef.GetContainingType(Context);

                        int exportedTypeIndex = GetExportedTypeIndex(containingType);
                        implementation = exportedTypeIndex.ToCodedIndex(ImplementationTag.ExportedType);

                        var parentFlags = (TypeFlags)metadata.GetExportedTypeFlags(exportedTypeIndex - 1);
                        if (parentFlags == TypeFlags.PrivateAccess)
                        {
                            flags = TypeFlags.PrivateAccess;
                        }

                        ITypeReference topLevelType = containingType;
                        INestedTypeReference tmp;
                        while ((tmp = topLevelType.AsNestedTypeReference) != null)
                        {
                            topLevelType = tmp.GetContainingType(Context);
                        }

                        var topLevelFlags = (TypeFlags)metadata.GetExportedTypeFlags(GetExportedTypeIndex(topLevelType) - 1);
                        if ((topLevelFlags & TypeFlags.ForwarderImplementation) != 0)
                        {
                            flags = TypeFlags.PrivateAccess;
                            typeDefinitionId = 0; // Must be cleared for type forwarders and types they contain.
                        }
                    }
                    else
                    {
                        throw ExceptionUtilities.UnexpectedValue(exportedType);
                    }

                    metadata.AddExportedType(
                        attributes: (TypeAttributes)flags,
                        @namespace: typeNamespace,
                        name: typeName,
                        implementation: implementation,
                        typeDefinitionId: typeDefinitionId);
                }
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
                    fieldDefinitionRowId: GetFieldDefIndex(fieldDef),
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

                BlobIdx descriptor = (marshallingInformation != null)
                    ? GetMarshallingDescriptorIndex(marshallingInformation)
                    : GetMarshallingDescriptorIndex(fieldDef.MarshallingDescriptor);

                metadata.AddMarshallingDescriptor(
                    parent: GetFieldDefIndex(fieldDef).ToCodedIndex(HasFieldMarshalTag.Field),
                    descriptor: descriptor);
            }

            foreach (IParameterDefinition parDef in this.GetParameterDefs())
            {
                if (!parDef.IsMarshalledExplicitly)
                {
                    continue;
                }

                var marshallingInformation = parDef.MarshallingInformation;

               BlobIdx descriptor = (marshallingInformation != null)
                    ? GetMarshallingDescriptorIndex(marshallingInformation)
                    : GetMarshallingDescriptorIndex(parDef.MarshallingDescriptor);

                metadata.AddMarshallingDescriptor(
                    parent: GetParameterDefIndex(parDef).ToCodedIndex(HasFieldMarshalTag.Param),
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

                int rva = mappedFieldDataWriter.Position;
                mappedFieldDataWriter.WriteBytes(fieldDef.MappedData);
                mappedFieldDataWriter.Align(ManagedTextSection.MappedFieldDataAlignment);

                metadata.AddFieldRelativeVirtualAddress(
                    fieldDefinitionRowId: GetFieldDefIndex(fieldDef),
                    relativeVirtualAddress: rva);
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
                    name: GetStringIndexForNameAndCheckLength(fieldDef.Name, fieldDef),
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
                    parent: GetFieldDefIndex(fieldDef).ToCodedIndex(HasConstantTag.Field),
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
                    parent: GetParameterDefIndex(parDef).ToCodedIndex(HasConstantTag.Param),
                    value: defaultValue.Value);
            }

            foreach (IPropertyDefinition propDef in this.GetPropertyDefs())
            {
                if (!propDef.HasDefaultValue)
                {
                    continue;
                }

                metadata.AddConstant(
                    parent: GetPropertyDefIndex(propDef).ToCodedIndex(HasConstantTag.Property),
                    value: propDef.DefaultValue.Value);
            }
        }

        private void PopulateFileTableRows()
        {
            IAssembly assembly = this.module.AsAssembly;
            if (assembly == null)
            {
                return;
            }

            var hashAlgorithm = assembly.HashAlgorithm;
            metadata.SetCapacity(TableIndex.File, _fileRefList.Count);

            foreach (IFileReference fileReference in _fileRefList)
            {
                metadata.AddAssemblyFile(
                    name: GetStringIndexForPathAndCheckLength(fileReference.FileName),
                    hashValue: metadata.GetBlobIndex(fileReference.GetHashValue(hashAlgorithm)),
                    containsMetadata: fileReference.HasMetadata);
            }
        }

        private void PopulateGenericParameters(ImmutableArray<IGenericParameter> sortedGenericParameters)
        {
            foreach (IGenericParameter genericParameter in sortedGenericParameters)
            {
                // CONSIDER: The CLI spec doesn't mention a restriction on the Name column of the GenericParam table,
                // but they go in the same string heap as all the other declaration names, so it stands to reason that
                // they should be restricted in the same way.
                int genericParameterRowId = metadata.AddGenericParameter(
                    parent: GetTypeOrMethodDefCodedIndex(genericParameter),
                    attributes: GetGenericParameterAttributes(genericParameter),
                    name: GetStringIndexForNameAndCheckLength(genericParameter.Name, genericParameter),
                    index: genericParameter.Index);

                foreach (ITypeReference constraint in genericParameter.GetConstraints(Context))
                {
                    metadata.AddGenericParameterConstraint(
                        genericParameterRowId: genericParameterRowId,
                        constraint: GetTypeDefOrRefCodedIndex(constraint, true));
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

                StringIdx importName = (entryPointName != null)
                    ? GetStringIndexForNameAndCheckLength(entryPointName, methodDef)
                    : metadata.GetStringIndex(methodDef.Name); // Length checked while populating the method def table.

                metadata.AddMethodImport(
                    member: GetMethodDefIndex(methodDef).ToCodedIndex(MemberForwardedTag.MethodDef),
                    attributes: data.Flags,
                    name: importName,
                    moduleReferenceRowId: GetModuleRefIndex(data.ModuleName));
            }
        }

        private void PopulateInterfaceImplTableRows()
        {
            foreach (ITypeDefinition typeDef in this.GetTypeDefs())
            {
                int typeDefIndex = GetTypeDefIndex(typeDef);
                foreach (ITypeReference interfaceRef in typeDef.Interfaces(Context))
                {
                    metadata.AddInterfaceImplementation(
                        typeDefinitionRowId: typeDefIndex,
                        interfaceCodedIndex: GetTypeDefOrRefCodedIndex(interfaceRef, true));
                }
            }
        }
        
        private void PopulateManifestResourceTableRows(BlobBuilder resourceDataWriter)
        {
            foreach (var resource in this.module.GetResources(Context))
            {
                uint implementation;
                if (resource.ExternalFile != null)
                {
                    // Length checked on insertion into the file table.
                    implementation = GetFileRefIndex(resource.ExternalFile).ToCodedIndex(ImplementationTag.File);
                }
                else
                {
                    // This is an embedded resource, we don't support references to resources from referenced assemblies.
                    implementation = 0;
                }

                metadata.AddManifestResource(
                    attributes: resource.IsPublic ? ManifestResourceAttributes.Public : ManifestResourceAttributes.Private,
                    name: GetStringIndexForNameAndCheckLength(resource.Name),
                    implementation: implementation,
                    offset: GetManagedResourceOffset(resource, resourceDataWriter));
            }

            // the stream should be aligned:
            Debug.Assert((resourceDataWriter.Count % ManagedTextSection.ManagedResourcesDataAlignment) == 0);
        }

        private void PopulateMemberRefTableRows()
        {
            var memberRefs = this.GetMemberRefs();
            metadata.SetCapacity(TableIndex.MemberRef, memberRefs.Count);

            foreach (ITypeMemberReference memberRef in memberRefs)
            {
                metadata.AddMemberReference(
                    type: GetMemberRefParentCodedIndex(memberRef),
                    name: GetStringIndexForNameAndCheckLength(memberRef.Name, memberRef), 
                    signature: GetMemberRefSignatureIndex(memberRef));
            }
        }
        
        private void PopulateMethodImplTableRows()
        {
            metadata.SetCapacity(TableIndex.MethodImpl, methodImplList.Count);

            foreach (MethodImplementation methodImplementation in this.methodImplList)
            {
                metadata.AddMethodImplementation(
                    typeDefinitionRowId: GetTypeDefIndex(methodImplementation.ContainingType),
                    methodBody: GetMethodDefOrRefCodedIndex(methodImplementation.ImplementingMethod),
                    methodDeclaration: GetMethodDefOrRefCodedIndex(methodImplementation.ImplementedMethod));
            }
        }
        
        private void PopulateMethodSpecTableRows()
        {
            var methodSpecs = this.GetMethodSpecs();
            metadata.SetCapacity(TableIndex.MethodSpec, methodSpecs.Count);

            foreach (IGenericMethodInstanceReference genericMethodInstanceReference in methodSpecs)
            {
                metadata.AddMethodSpecification(
                    method: GetMethodDefOrRefCodedIndex(genericMethodInstanceReference.GetGenericMethod(Context)),
                    instantiation: GetGenericMethodInstanceIndex(genericMethodInstanceReference));
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
                    name: GetStringIndexForNameAndCheckLength(methodDef.Name, methodDef),
                    signature: GetMethodSignatureIndex(methodDef),
                    bodyOffset: methodBodyOffsets[i],
                    paramList: GetParameterDefIndex(methodDef));

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
                uint association = GetPropertyDefIndex(propertyDef).ToCodedIndex(HasSemanticsTag.Property);
                foreach (IMethodReference accessorMethod in propertyDef.Accessors)
                {
                    ushort semantics;
                    if (accessorMethod == propertyDef.Setter)
                    {
                        semantics = 0x0001;
                    }
                    else if (accessorMethod == propertyDef.Getter)
                    {
                        semantics = 0x0002;
                    }
                    else
                    {
                        semantics = 0x0004;
                    }

                    metadata.AddMethodSemantics(
                        association: association,
                        semantics: semantics,
                        methodDefinitionRowId: GetMethodDefIndex(accessorMethod.GetResolvedMethod(Context)));
                }
            }

            foreach (IEventDefinition eventDef in this.GetEventDefs())
            {
                uint association = GetEventDefIndex(eventDef).ToCodedIndex(HasSemanticsTag.Event);
                foreach (IMethodReference accessorMethod in eventDef.Accessors)
                {
                    ushort semantics;
                    if (accessorMethod == eventDef.Adder)
                    {
                        semantics = 0x0008;
                    }
                    else if (accessorMethod == eventDef.Remover)
                    {
                        semantics = 0x0010;
                    }
                    else if (accessorMethod == eventDef.Caller)
                    {
                        semantics = 0x0020;
                    }
                    else
                    {
                        semantics = 0x0004;
                    }

                    metadata.AddMethodSemantics(
                        association: association,
                        semantics: semantics,
                        methodDefinitionRowId: GetMethodDefIndex(accessorMethod.GetResolvedMethod(Context)));
                }
            }
        }

        private void PopulateModuleRefTableRows()
        {
            var moduleRefs = this.GetModuleRefs();
            metadata.SetCapacity(TableIndex.ModuleRef, moduleRefs.Count);

            foreach (string moduleName in moduleRefs)
            {
                metadata.AddModuleReference(GetStringIndexForPathAndCheckLength(moduleName));
            }
        }
        
        private void PopulateModuleTableRow()
        {
            CheckPathLength(this.module.ModuleName);

            // MVID is specified upfront when emitting EnC delta:
            Guid mvid = this.module.Properties.PersistentIdentifier;

            if (mvid == default(Guid) && !_deterministic)
            {
                // If we are being nondeterministic, generate random
                mvid = Guid.NewGuid();
            }

            metadata.AddModule(
                generation: this.Generation,
                moduleName: metadata.GetStringIndex(this.module.ModuleName),
                mvid: metadata.AllocateGuid(mvid),
                encId: metadata.GetGuidIndex(EncId),
                encBaseId: metadata.GetGuidIndex(EncBaseId));
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
                    name: GetStringIndexForNameAndCheckLength(parDef.Name, parDef));
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
                    name: GetStringIndexForNameAndCheckLength(propertyDef.Name, propertyDef),
                    signature: GetPropertySignatureIndex(propertyDef));
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
                    @namespace: (namespaceType != null) ? GetStringIndexForNamespaceAndCheckLength(namespaceType, mangledTypeName) : default(StringIdx),
                    name: GetStringIndexForNameAndCheckLength(mangledTypeName, typeDef),
                    baseTypeCodedIndex: (baseType != null) ? GetTypeDefOrRefCodedIndex(baseType, true) : 0,
                    fieldList: GetFieldDefIndex(typeDef),
                    methodList: GetMethodDefIndex(typeDef));
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
                    typeDefinitionRowId: GetTypeDefIndex(typeDef),
                    enclosingTypeDefinitionRowId: GetTypeDefIndex(nestedTypeDef.ContainingTypeDefinition));
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
                    typeDefinitionRowId: GetTypeDefIndex(typeDef),
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
                uint resolutionScope;
                StringIdx name;
                StringIdx @namespace;

                INestedTypeReference nestedTypeRef = typeRef.AsNestedTypeReference;
                if (nestedTypeRef != null)
                {
                    ITypeReference scopeTypeRef;

                    ISpecializedNestedTypeReference sneTypeRef = nestedTypeRef.AsSpecializedNestedTypeReference;
                    if (sneTypeRef != null)
                    {
                        scopeTypeRef = sneTypeRef.UnspecializedVersion.GetContainingType(Context);
                    }
                    else
                    {
                        scopeTypeRef = nestedTypeRef.GetContainingType(Context);
                    }

                    resolutionScope = GetTypeRefIndex(scopeTypeRef).ToCodedIndex(ResolutionScopeTag.TypeRef);
                    name = this.GetStringIndexForNameAndCheckLength(GetMangledName(nestedTypeRef), nestedTypeRef);
                    @namespace = default(StringIdx);
                }
                else
                {
                    INamespaceTypeReference namespaceTypeRef = typeRef.AsNamespaceTypeReference;
                    if (namespaceTypeRef == null)
                    {
                        throw ExceptionUtilities.UnexpectedValue(typeRef);
                    }

                    resolutionScope = this.GetResolutionScopeCodedIndex(namespaceTypeRef.GetUnit(Context));
                    string mangledTypeName = GetMangledName(namespaceTypeRef);
                    name = this.GetStringIndexForNameAndCheckLength(mangledTypeName, namespaceTypeRef);
                    @namespace = this.GetStringIndexForNamespaceAndCheckLength(namespaceTypeRef, mangledTypeName);
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
            var signatures = GetStandAloneSignatures();

            foreach (BlobIdx signature in signatures)
            {
                metadata.AddStandaloneSignature(signature);
            }
        }

        private int[] SerializeMethodBodies(BlobBuilder ilWriter, PdbWriter pdbWriterOpt)
        {
            CustomDebugInfoWriter customDebugInfoWriter = (pdbWriterOpt != null) ? new CustomDebugInfoWriter(pdbWriterOpt) : null;

            var methods = this.GetMethodDefs();
            int[] bodyOffsets = new int[methods.Count];

            int lastLocalVariableRid = 0;
            int lastLocalConstantRid = 0;

            int methodRid = 1;
            foreach (IMethodDefinition method in methods)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                int bodyOffset;
                IMethodBody body;
                int localSignatureRid;

                if (method.HasBody())
                {
                    body = method.GetBody(Context);
                    Debug.Assert(body != null || allowMissingMethodBodies);

                    if (body != null)
                    {
                        localSignatureRid = this.SerializeLocalVariablesSignature(body);
                        uint localSignatureToken = (localSignatureRid != 0) ? (uint)(0x11000000 | localSignatureRid) : 0;

                        // TODO: consider parallelizing these (local signature tokens can be piped into IL serialization & debug info generation)
                        bodyOffset = this.SerializeMethodBody(body, ilWriter, localSignatureToken);

                        pdbWriterOpt?.SerializeDebugInfo(body, localSignatureToken, customDebugInfoWriter);
                    }
                    else
                    {
                        bodyOffset = 0;
                        localSignatureRid = 0;
                    }
                }
                else
                {
                    // 0 is actually written to metadata when the row is serialized
                    bodyOffset = -1;
                    body = null;
                    localSignatureRid = 0;
                }

                if (_debugMetadataOpt != null)
                {
                    SerializeMethodDebugInfo(body, methodRid, localSignatureRid, ref lastLocalVariableRid, ref lastLocalConstantRid);
                }

                bodyOffsets[methodRid - 1] = bodyOffset;

                methodRid++;
            }

            return bodyOffsets;
        }

        private int SerializeMethodBody(IMethodBody methodBody, BlobBuilder ilWriter, uint localSignatureToken)
        {
            int ilLength = methodBody.IL.Length;
            uint numberOfExceptionHandlers = (uint)methodBody.ExceptionRegions.Length;
            bool isSmallBody = ilLength < 64 && methodBody.MaxStack <= 8 && localSignatureToken == 0 && numberOfExceptionHandlers == 0;

            // Check if an identical method body has already been serialized. 
            // If so, use the RVA of the already serialized one.
            // Note that we don't need to rewrite the fake tokens in the body before looking it up.
            int bodyOffset;
            if (isSmallBody && _smallMethodBodies.TryGetValue(methodBody.IL, out bodyOffset))
            {
                return bodyOffset;
            }

            if (isSmallBody)
            {
                bodyOffset = ilWriter.Position;
                ilWriter.WriteByte((byte)((ilLength << 2) | 2));

                _smallMethodBodies.Add(methodBody.IL, bodyOffset);
            }
            else
            {
                ilWriter.Align(4);

                bodyOffset = ilWriter.Position;

                ushort flags = (3 << 12) | 0x3;
                if (numberOfExceptionHandlers > 0)
                {
                    flags |= 0x08;
                }

                if (methodBody.LocalsAreZeroed)
                {
                    flags |= 0x10;
                }

                ilWriter.WriteUInt16(flags);
                ilWriter.WriteUInt16(methodBody.MaxStack);
                ilWriter.WriteUInt32((uint)ilLength);
                ilWriter.WriteUInt32(localSignatureToken);
            }

            WriteMethodBodyIL(ilWriter, methodBody);

            if (numberOfExceptionHandlers > 0)
            {
                SerializeMethodBodyExceptionHandlerTable(methodBody, numberOfExceptionHandlers, ilWriter);
            }

            return bodyOffset;
        }

        /// <summary>
        /// Serialize the method local signature to the blob.
        /// </summary>
        /// <returns>Standalone signature token</returns>
        protected virtual int SerializeLocalVariablesSignature(IMethodBody body)
        {
            Debug.Assert(!_tableIndicesAreComplete);

            var localVariables = body.LocalVariables;
            if (localVariables.Length == 0)
            {
                return 0;
            }

            var builder = PooledBlobBuilder.GetInstance();

            var encoder = new BlobEncoder(builder).LocalVariableSignature(localVariables.Length);
            foreach (ILocalDefinition local in localVariables)
            {
                encoder = SerializeLocalVariableSignature(encoder.AddVariable(), local);
            }

            encoder.EndVariables();

            BlobIdx blobIndex = metadata.GetBlobIndex(builder);

            int signatureIndex = this.GetOrAddStandAloneSignatureIndex(blobIndex);
            builder.Free();

            return signatureIndex;
        }

        protected T SerializeLocalVariableSignature<T>(LocalVariableEncoder<T> encoder, ILocalDefinition local)
            where T : IBlobEncoder
        {
            var typeEncoder = SerializeCustomModifiers(encoder.ModifiedType(), local.CustomModifiers);

            if (module.IsPlatformType(local.Type, PlatformType.SystemTypedReference))
            {
                return typeEncoder.TypedReference();
            }

            return SerializeTypeReference(typeEncoder.Type(local.IsPinned, local.IsReference), local.Type);
        }

        internal int SerializeLocalConstantStandAloneSignature(ILocalDefinition localConstant)
        {
            var builder = PooledBlobBuilder.GetInstance();
            var encoder = new BlobEncoder(builder).FieldSignature();
            var typeBuilder = SerializeCustomModifiers(encoder, localConstant.CustomModifiers);
            SerializeTypeReference(typeBuilder, localConstant.Type);

            BlobIdx blobIndex = metadata.GetBlobIndex(builder);
            int signatureIndex = GetOrAddStandAloneSignatureIndex(blobIndex);
            builder.Free();

            return 0x11000000 | signatureIndex;
        }

        private static int ReadInt32(ImmutableArray<byte> buffer, int pos)
        {
            return buffer[pos] | buffer[pos + 1] << 8 | buffer[pos + 2] << 16 | buffer[pos + 3] << 24;
        }

        private static void WriteUint(byte[] buffer, uint value, int pos)
        {
            unchecked
            {
                buffer[pos] = (byte)value;
                buffer[pos + 1] = (byte)(value >> 8);
                buffer[pos + 2] = (byte)(value >> 16);
                buffer[pos + 3] = (byte)(value >> 24);
            }
        }

        private int ResolveTokenFromReference(IReference reference)
        {
            ITypeReference typeReference = reference as ITypeReference;

            if (typeReference != null)
            {
                return this.GetTypeToken(typeReference);
            }

            IFieldReference fieldReference = reference as IFieldReference;

            if (fieldReference != null)
            {
                return this.GetFieldToken(fieldReference);
            }

            IMethodReference methodReference = reference as IMethodReference;
            if (methodReference != null)
            {
                return this.GetMethodToken(methodReference);
            }

            throw ExceptionUtilities.UnexpectedValue(reference);
        }

        private int ResolveSymbolTokenFromPseudoSymbolToken(int pseudoSymbolToken)
        {
            int index = pseudoSymbolToken;
            var reference = _pseudoSymbolTokenToReferenceMap[index];
            if (reference != null)
            {
                // EDMAURER since method bodies are not visited as they are in CCI, the operations
                // that would have been done on them are done here.
                _referenceVisitor.VisitMethodBodyReference(reference);

                int token = ResolveTokenFromReference(reference);
                _pseudoSymbolTokenToTokenMap[index] = token;
                _pseudoSymbolTokenToReferenceMap[index] = null; // Set to null to bypass next lookup
                return token;
            }

            return _pseudoSymbolTokenToTokenMap[index];
        }

        private int ResolveStringTokenFromPseudoStringToken(int pseudoStringToken)
        {
            int index = pseudoStringToken;
            var str = _pseudoStringTokenToStringMap[index];
            if (str != null)
            {
                var token = metadata.GetUserStringToken(str);
                _pseudoStringTokenToTokenMap[index] = token;
                _pseudoStringTokenToStringMap[index] = null; // Set to null to bypass next lookup
                return token;
            }

            return _pseudoStringTokenToTokenMap[index];
        }

        private void WriteMethodBodyIL(BlobBuilder builder, IMethodBody methodBody)
        {
            ImmutableArray<byte> methodBodyIL = methodBody.IL;

            // write the raw body first and then patch tokens:
            var writer = new BlobWriter(builder.ReserveBytes(methodBodyIL.Length));
            writer.WriteBytes(methodBodyIL);

            int offset = 0;
            while (offset < methodBodyIL.Length)
            {
                var operandType = InstructionOperandTypes.ReadOperandType(methodBodyIL, ref offset);
                switch (operandType)
                {
                    case OperandType.InlineField:
                    case OperandType.InlineMethod:
                    case OperandType.InlineTok:
                    case OperandType.InlineType:
                        writer.Offset = offset;
                        writer.WriteInt32(ResolveSymbolTokenFromPseudoSymbolToken(ReadInt32(methodBodyIL, offset)));
                        offset += 4;
                        break;

                    case OperandType.InlineString:
                        writer.Offset = offset;
                        writer.WriteInt32(ResolveStringTokenFromPseudoStringToken(ReadInt32(methodBodyIL, offset)));
                        offset += 4;
                        break;

                    case OperandType.InlineSig: // calli
                    case OperandType.InlineBrTarget:
                    case OperandType.InlineI:
                    case OperandType.ShortInlineR:
                        offset += 4;
                        break;

                    case OperandType.InlineSwitch:
                        int argCount = ReadInt32(methodBodyIL, offset);
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

        private void SerializeMethodBodyExceptionHandlerTable(IMethodBody methodBody, uint numberOfExceptionHandlers, BlobBuilder writer)
        {
            var regions = methodBody.ExceptionRegions;
            bool useSmallExceptionHeaders = MayUseSmallExceptionHeaders(numberOfExceptionHandlers, regions);
            writer.Align(4);
            if (useSmallExceptionHeaders)
            {
                uint dataSize = numberOfExceptionHandlers * 12 + 4;
                writer.WriteByte(0x01);
                writer.WriteByte((byte)(dataSize & 0xff));
                writer.WriteUInt16(0);
            }
            else
            {
                uint dataSize = numberOfExceptionHandlers * 24 + 4;
                writer.WriteByte(0x41);
                writer.WriteByte((byte)(dataSize & 0xff));
                writer.WriteUInt16((ushort)((dataSize >> 8) & 0xffff));
            }

            foreach (var region in regions)
            {
                this.SerializeExceptionRegion(region, useSmallExceptionHeaders, writer);
            }
        }

        private void SerializeExceptionRegion(ExceptionHandlerRegion region, bool useSmallExceptionHeaders, BlobBuilder writer)
        {
            writer.WriteUInt16((ushort)region.HandlerKind);

            if (useSmallExceptionHeaders)
            {
                writer.WriteUInt16((ushort)region.TryStartOffset);
                writer.WriteByte((byte)(region.TryEndOffset - region.TryStartOffset));
                writer.WriteUInt16((ushort)region.HandlerStartOffset);
                writer.WriteByte((byte)(region.HandlerEndOffset - region.HandlerStartOffset));
            }
            else
            {
                writer.WriteUInt16(0);
                writer.WriteUInt32((uint)region.TryStartOffset);
                writer.WriteUInt32((uint)(region.TryEndOffset - region.TryStartOffset));
                writer.WriteUInt32((uint)region.HandlerStartOffset);
                writer.WriteUInt32((uint)(region.HandlerEndOffset - region.HandlerStartOffset));
            }

            if (region.HandlerKind == ExceptionRegionKind.Catch)
            {
                writer.WriteUInt32((uint)this.GetTypeToken(region.ExceptionType));
            }
            else
            {
                writer.WriteUInt32((uint)region.FilterDecisionStartOffset);
            }
        }

        private static bool MayUseSmallExceptionHeaders(uint numberOfExceptionHandlers, ImmutableArray<ExceptionHandlerRegion> exceptionRegions)
        {
            if (numberOfExceptionHandlers * 12 + 4 > 0xff)
            {
                return false;
            }

            foreach (var region in exceptionRegions)
            {
                if (region.TryStartOffset > 0xffff)
                {
                    return false;
                }

                if (region.TryEndOffset - region.TryStartOffset > 0xff)
                {
                    return false;
                }

                if (region.HandlerStartOffset > 0xffff)
                {
                    return false;
                }

                if (region.HandlerEndOffset - region.HandlerStartOffset > 0xff)
                {
                    return false;
                }
            }

            return true;
        }

        private T SerializeParameterInformation<T>(ParameterEncoder<T> encoder, IParameterTypeInformation parameterTypeInformation)
            where T : IBlobEncoder
        {
            ushort countOfCustomModifiersPrecedingByRef = parameterTypeInformation.CountOfCustomModifiersPrecedingByRef;
            var modifiers = parameterTypeInformation.CustomModifiers;

            Debug.Assert(countOfCustomModifiersPrecedingByRef == 0 || parameterTypeInformation.IsByReference);

            var modifiersEncoder = encoder.ModifiedType();
            var paramTypeEncoder = SerializeCustomModifiers(modifiersEncoder, modifiers, 0, countOfCustomModifiersPrecedingByRef);

            var type = parameterTypeInformation.GetType(Context);
            if (module.IsPlatformType(type, PlatformType.SystemTypedReference))
            {
                return paramTypeEncoder.TypedReference();
            }

            // Workaround for C++ compiler bug.
            // The spec requires all modifiers to precede BYREF marker, however C++ compiler emits them after BYREF.
            // We need to be able to serialize signatures parsed from C++ emitted assemblies.
#pragma warning disable CS0618
            var modifiedType = paramTypeEncoder.ModifiedType(parameterTypeInformation.IsByReference);
#pragma warning restore CS0618

            var typeEncoder = SerializeCustomModifiers(modifiedType, modifiers, countOfCustomModifiersPrecedingByRef, modifiers.Length - countOfCustomModifiersPrecedingByRef);

            return SerializeTypeReference(typeEncoder, type);
        }

        private void SerializeFieldSignature(IFieldReference fieldReference, BlobBuilder builder)
        {
            var modifiedTypeBuilder = new BlobEncoder(builder).FieldSignature();
            var typeReference = fieldReference.GetType(Context);
            var typeBuilder = SerializeTypeReferenceModifiers(modifiedTypeBuilder, ref typeReference);
            SerializeTypeReference(typeBuilder, typeReference);
        }

        private void SerializeGenericMethodInstanceSignature(BlobBuilder builder, IGenericMethodInstanceReference genericMethodInstanceReference)
        {
            var argsBuilder = new BlobEncoder(builder).MethodSpecificationSignature(genericMethodInstanceReference.GetGenericMethod(Context).GenericParameterCount);
            foreach (ITypeReference genericArgument in genericMethodInstanceReference.GetGenericArguments(Context))
            {
                ITypeReference typeRef = genericArgument;
                argsBuilder = SerializeTypeReference(SerializeTypeReferenceModifiers(argsBuilder.AddArgument(), ref typeRef), typeRef);
            }

            argsBuilder.EndArguments();
        }

        private void SerializeCustomAttributeSignature(ICustomAttribute customAttribute, BlobBuilder builder)
        {
            var parameters = customAttribute.Constructor(Context).GetParameters(Context);
            var arguments = customAttribute.GetArguments(Context);
            Debug.Assert(parameters.Length == arguments.Length);

            var encoder = new BlobEncoder(builder).CustomAttributeSignature(customAttribute.NamedArgumentCount);

            for (int i = 0; i < parameters.Length; i++)
            {
                encoder = SerializeMetadataExpression(encoder.AddArgument(), arguments[i], parameters[i].GetType(Context));
            }

            SerializeCustomAttributeNamedArguments(encoder.EndArguments(), customAttribute);
        }

        private T SerializeCustomAttributeNamedArguments<T>(NamedArgumentsEncoder<T> encoder, ICustomAttribute customAttribute)
            where T : IBlobEncoder
        {
            foreach (IMetadataNamedArgument namedArgument in customAttribute.GetNamedArguments(Context))
            {
                var typeEncoder = encoder.AddArgument(namedArgument.IsField);
                var nameEncoder = SerializeNamedArgumentType(typeEncoder, namedArgument.Type);
                var elementEncoder = nameEncoder.Name(namedArgument.ArgumentName);

                encoder = SerializeMetadataExpression(elementEncoder, namedArgument.ArgumentValue, namedArgument.Type);
            }

            return encoder.EndArguments();
        }

        private T SerializeNamedArgumentType<T>(NamedArgumentTypeEncoder<T> encoder, ITypeReference type)
            where T : IBlobEncoder
        {
            var arrayType = type as IArrayTypeReference;
            if (arrayType != null)
            {
                return SerializeCustomAttributeArrayType(encoder.SZArray(), arrayType);
            }

            if (module.IsPlatformType(type, PlatformType.SystemObject))
            {
                return encoder.Object();
            }

            return SerializeCustomAttributeElementType(encoder.ScalarType(), type);
        }

        private T SerializeMetadataExpression<T>(LiteralEncoder<T> encoder, IMetadataExpression expression, ITypeReference targetType)
            where T : IBlobEncoder
        {
            IMetadataCreateArray a = expression as IMetadataCreateArray;
            if (a != null)
            {
                ITypeReference targetElementType;
                var targetArrayType = targetType as IArrayTypeReference;

                VectorEncoder<T> vectorEncoder;
                if (targetArrayType == null)
                {
                    // implicit conversion from array to object
                    Debug.Assert(this.module.IsPlatformType(targetType, PlatformType.SystemObject));

                    vectorEncoder = SerializeCustomAttributeArrayType(encoder.TaggedVector(), (IArrayTypeReference)a.Type);

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

                var literalsEncoder = vectorEncoder.Count((int)a.ElementCount);

                foreach (IMetadataExpression elemValue in a.Elements)
                {
                    literalsEncoder = SerializeMetadataExpression(literalsEncoder.AddLiteral(), elemValue, targetElementType);
                }

                return literalsEncoder.EndLiterals();
            }
            else
            {
                ScalarEncoder<T> scalarEncoder;
                IMetadataConstant c = expression as IMetadataConstant;

                if (this.module.IsPlatformType(targetType, PlatformType.SystemObject))
                {
                    // special case null argument assigned to Object parameter - treat as null string
                    if (c != null &&
                        c.Value == null &&
                        this.module.IsPlatformType(c.Type, PlatformType.SystemObject))
                    {
                        scalarEncoder = encoder.TaggedScalar().String();
                    }
                    else
                    {
                        scalarEncoder = SerializeCustomAttributeElementType(encoder.TaggedScalar(), expression.Type);
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
                        return scalarEncoder.NullArray();
                    }
                 
                    Debug.Assert(!module.IsPlatformType(c.Type, PlatformType.SystemType) || c.Value == null);
                    return scalarEncoder.Constant(c.Value);
                }
                else
                {
                    return scalarEncoder.SystemType(((IMetadataTypeOf)expression).TypeToGet.GetSerializedTypeName(Context));
                }
            }
        }

        private void SerializeMarshallingDescriptor(IMarshallingInformation marshallingInformation, BlobBuilder writer)
        {
            writer.WriteCompressedInteger((uint)marshallingInformation.UnmanagedType);
            switch (marshallingInformation.UnmanagedType)
            {
                case UnmanagedType.ByValArray: // NATIVE_TYPE_FIXEDARRAY
                    Debug.Assert(marshallingInformation.NumberOfElements >= 0);
                    writer.WriteCompressedInteger((uint)marshallingInformation.NumberOfElements);
                    if (marshallingInformation.ElementType >= 0)
                    {
                        writer.WriteCompressedInteger((uint)marshallingInformation.ElementType);
                    }

                    break;

                case Constants.UnmanagedType_CustomMarshaler:
                    writer.WriteUInt16(0); // padding

                    object marshaller = marshallingInformation.GetCustomMarshaller(Context);
                    ITypeReference marshallerTypeRef = marshaller as ITypeReference;
                    if (marshallerTypeRef != null)
                    {
                        this.SerializeTypeName(marshallerTypeRef, writer);
                    }
                    else if (marshaller != null)
                    {
                        writer.WriteSerializedString((string)marshaller);
                    }
                    else
                    {
                        writer.WriteByte(0);
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
                    writer.WriteCompressedInteger((uint)marshallingInformation.ElementType);
                    if (marshallingInformation.ParamIndex >= 0)
                    {
                        writer.WriteCompressedInteger((uint)marshallingInformation.ParamIndex);
                        if (marshallingInformation.NumberOfElements >= 0)
                        {
                            writer.WriteCompressedInteger((uint)marshallingInformation.NumberOfElements);
                            writer.WriteByte(1); // The parameter number is valid
                        }
                    }
                    else if (marshallingInformation.NumberOfElements >= 0)
                    {
                        writer.WriteByte(0); // Dummy parameter value emitted so that NumberOfElements can be in a known position
                        writer.WriteCompressedInteger((uint)marshallingInformation.NumberOfElements);
                        writer.WriteByte(0); // The parameter number is not valid
                    }

                    break;

                case UnmanagedType.SafeArray:
                    if (marshallingInformation.SafeArrayElementSubtype >= 0)
                    {
                        writer.WriteCompressedInteger((uint)marshallingInformation.SafeArrayElementSubtype);
                        var elementType = marshallingInformation.GetSafeArrayElementUserDefinedSubtype(Context);
                        if (elementType != null)
                        {
                            this.SerializeTypeName(elementType, writer);
                        }
                    }

                    break;

                case UnmanagedType.ByValTStr: // NATIVE_TYPE_FIXEDSYSSTRING
                    writer.WriteCompressedInteger((uint)marshallingInformation.NumberOfElements);
                    break;

                case UnmanagedType.Interface:
                case UnmanagedType.IDispatch:
                case UnmanagedType.IUnknown:
                    if (marshallingInformation.IidParameterIndex >= 0)
                    {
                        writer.WriteCompressedInteger((uint)marshallingInformation.IidParameterIndex);
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
            var pooled = PooledStringBuilder.GetInstance();
            StringBuilder sb = pooled.Builder;
            sb.Append(assemblyReference.Name);
            sb.AppendFormat(CultureInfo.InvariantCulture, ", Version={0}.{1}.{2}.{3}", assemblyReference.Version.Major, assemblyReference.Version.Minor, assemblyReference.Version.Build, assemblyReference.Version.Revision);
            if (!string.IsNullOrEmpty(assemblyReference.Culture))
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, ", Culture={0}", assemblyReference.Culture);
            }
            else
            {
                sb.Append(", Culture=neutral");
            }

            sb.Append(", PublicKeyToken=");
            if (assemblyReference.PublicKeyToken.Length > 0)
            {
                foreach (byte b in assemblyReference.PublicKeyToken)
                {
                    sb.Append(b.ToString("x2"));
                }
            }
            else
            {
                sb.Append("null");
            }

            if (assemblyReference.IsRetargetable)
            {
                sb.Append(", Retargetable=Yes");
            }

            if (assemblyReference.ContentType == AssemblyContentType.WindowsRuntime)
            {
                sb.Append(", ContentType=WindowsRuntime");
            }
            else
            {
                Debug.Assert(assemblyReference.ContentType == AssemblyContentType.Default);
            }

            return pooled.ToStringAndFree();
        }

        private void SerializePermissionSet(IEnumerable<ICustomAttribute> permissionSet, BlobBuilder writer)
        {
            EmitContext context = this.Context;
            foreach (ICustomAttribute customAttribute in permissionSet)
            {
                bool isAssemblyQualified = true;
                string typeName = customAttribute.GetType(context).GetSerializedTypeName(context, ref isAssemblyQualified);
                if (!isAssemblyQualified)
                {
                    INamespaceTypeReference namespaceType = customAttribute.GetType(context).AsNamespaceTypeReference;
                    var referencedAssembly = namespaceType?.GetUnit(context) as IAssemblyReference;
                    if (referencedAssembly != null)
                    {
                        typeName = typeName + ", " + StrongName(referencedAssembly);
                    }
                }

                writer.WriteSerializedString(typeName);

                var customAttributeArgsBuilder = PooledBlobBuilder.GetInstance();
                var namedArgsEncoder = new BlobEncoder(customAttributeArgsBuilder).PermissionSetArguments(customAttribute.NamedArgumentCount);
                SerializeCustomAttributeNamedArguments(namedArgsEncoder, customAttribute);
                writer.WriteCompressedInteger((uint)customAttributeArgsBuilder.Count);

                customAttributeArgsBuilder.WriteContentTo(writer);
                customAttributeArgsBuilder.Free();
            }
            // TODO: xml for older platforms
        }

        private T SerializeReturnValueAndParameters<T>(MethodSignatureEncoder<T> encoder, ISignature signature, ImmutableArray<IParameterTypeInformation> varargParameters)
            where T : IBlobEncoder
        {
            var declaredParameters = signature.GetParameters(Context);
			var returnType = signature.GetType(Context);
			
            var customModifiersEncoder = encoder.Parameters(declaredParameters.Length + varargParameters.Length);
			var returnTypeEncoder = SerializeCustomModifiers(customModifiersEncoder, signature.ReturnValueCustomModifiers);

            var parametersEncoder = module.IsPlatformType(returnType, PlatformType.SystemTypedReference) ?
                returnTypeEncoder.TypedReference() :
                SerializeTypeReference(returnTypeEncoder.Type(signature.ReturnValueIsByRef), returnType);

            foreach (IParameterTypeInformation parameter in declaredParameters)
            {
                parametersEncoder = SerializeParameterInformation(parametersEncoder.AddParameter(), parameter);
            }

            if (varargParameters.Length > 0)
            {
                parametersEncoder = parametersEncoder.StartVarArgs();
                foreach (IParameterTypeInformation parameter in varargParameters)
                {
                    parametersEncoder = SerializeParameterInformation(parametersEncoder.AddParameter(), parameter);
                }
            }

            return parametersEncoder.EndParameters();
        }

        private T SerializeTypeReference<T>(SignatureTypeEncoder<T> encoder, ITypeReference typeReference)
            where T : IBlobEncoder
        {
            while (true)
            {
                // BYREF is specified directly in RetType, Param, LocalVarSig signatures
                Debug.Assert(!(typeReference is IManagedPointerTypeReference));

                // TYPEDREF is only allowed in RetType, Param, LocalVarSig signatures
                Debug.Assert(!module.IsPlatformType(typeReference, PlatformType.SystemTypedReference));
				
                Debug.Assert(!(typeReference is IModifiedTypeReference));

                var primitiveType = typeReference.TypeCode(Context);
                if (primitiveType != PrimitiveTypeCode.Pointer && primitiveType != PrimitiveTypeCode.NotPrimitive)
                {
                    return encoder.PrimitiveType(primitiveType);
                }

                var pointerTypeReference = typeReference as IPointerTypeReference;
                if (pointerTypeReference != null)
                {
                    typeReference = pointerTypeReference.GetTargetType(Context);
                    encoder = SerializeTypeReferenceModifiers(encoder.Pointer(), ref typeReference);
                    continue;
                }

                IGenericTypeParameterReference genericTypeParameterReference = typeReference.AsGenericTypeParameterReference;
                if (genericTypeParameterReference != null)
                {
                    return encoder.GenericTypeParameter(
                        GetNumberOfInheritedTypeParameters(genericTypeParameterReference.DefiningType) +
                        genericTypeParameterReference.Index);
                }

                var arrayTypeReference = typeReference as IArrayTypeReference;
                if (arrayTypeReference != null)
                {
                    typeReference = arrayTypeReference.GetElementType(Context);

                    if (arrayTypeReference.IsSZArray)
                    {
                        encoder = SerializeTypeReferenceModifiers(encoder.SZArray(), ref typeReference);
                        continue;
                    }
                    else
                    {
                        var elementTypeEncoder = SerializeTypeReferenceModifiers(encoder.Array(), ref typeReference);
                        var shapeEncoder = SerializeTypeReference(elementTypeEncoder, typeReference);
                        return shapeEncoder.Shape(arrayTypeReference.Rank, arrayTypeReference.Sizes, arrayTypeReference.LowerBounds);
                    }
                }

                if (module.IsPlatformType(typeReference, PlatformType.SystemObject))
                {
                    return encoder.Object();
                }

                IGenericMethodParameterReference genericMethodParameterReference = typeReference.AsGenericMethodParameterReference;
                if (genericMethodParameterReference != null)
                {
                    return encoder.GenericMethodTypeParameter(genericMethodParameterReference.Index);
                }

                if (typeReference.IsTypeSpecification())
                {
                    ITypeReference uninstantiatedTypeReference = typeReference.GetUninstantiatedGenericType();

                    // Roslyn's uninstantiated type is the same object as the instantiated type for
                    // types closed over their type parameters, so to speak.

                    var consolidatedTypeArguments = ArrayBuilder<ITypeReference>.GetInstance();
                    typeReference.GetConsolidatedTypeArguments(consolidatedTypeArguments, this.Context);

                    var genericArgsEncoder = encoder.GenericInstantiation(
                        typeReference.IsValueType,
                        GetTypeDefOrRefCodedIndex(uninstantiatedTypeReference, treatRefAsPotentialTypeSpec: false),
                        consolidatedTypeArguments.Count);

                    foreach (ITypeReference typeArgument in consolidatedTypeArguments)
                    {
                        ITypeReference typeArg = typeArgument;

                        genericArgsEncoder = SerializeTypeReference(
                            SerializeTypeReferenceModifiers(genericArgsEncoder.AddArgument(), ref typeArg), 
                            typeArg);
                    }

                    consolidatedTypeArguments.Free();
                    return genericArgsEncoder.EndArguments();
                }

                return encoder.TypeDefOrRefOrSpec(typeReference.IsValueType, GetTypeDefOrRefCodedIndex(typeReference, true));
            }
        }

        private T SerializeCustomAttributeArrayType<T>(CustomAttributeArrayTypeEncoder<T> encoder, IArrayTypeReference arrayTypeReference)
            where T : IBlobEncoder
        {
            // A single-dimensional, zero-based array is specified as a single byte 0x1D followed by the FieldOrPropType of the element type. 
        
            // only non-jagged SZ arrays are allowed in attributes 
            // (need to encode the type of the SZ array if the parameter type is Object):
            Debug.Assert(arrayTypeReference.IsSZArray);

            var elementType = arrayTypeReference.GetElementType(Context);
            Debug.Assert(!(elementType is IModifiedTypeReference));

            if (module.IsPlatformType(elementType, PlatformType.SystemObject))
            {
                return encoder.ObjectArray();
            }
            else
            {
                return SerializeCustomAttributeElementType(encoder.ElementType(), elementType);
            }
        }

        private T SerializeCustomAttributeElementType<T>(CustomAttributeElementTypeEncoder<T> encoder, ITypeReference typeReference)
            where T : IBlobEncoder
        {
            // Spec:
            // The FieldOrPropType shall be exactly one of:
            // ELEMENT_TYPE_BOOLEAN, ELEMENT_TYPE_CHAR, ELEMENT_TYPE_I1, ELEMENT_TYPE_U1, ELEMENT_TYPE_I2, ELEMENT_TYPE_U2, ELEMENT_TYPE_I4, 
            // ELEMENT_TYPE_U4, ELEMENT_TYPE_I8, ELEMENT_TYPE_U8, ELEMENT_TYPE_R4, ELEMENT_TYPE_R8, ELEMENT_TYPE_STRING.
            // An enum is specified as a single byte 0x55 followed by a SerString.
            
            var primitiveType = typeReference.TypeCode(Context);
            if (primitiveType != PrimitiveTypeCode.NotPrimitive)
            {
                return encoder.PrimitiveType(primitiveType);
            }
            else if (module.IsPlatformType(typeReference, PlatformType.SystemType))
            {
                return encoder.SystemType();
            }
            else
            {
                Debug.Assert(typeReference.IsEnum);
                return encoder.Enum(typeReference.GetSerializedTypeName(this.Context));
            }
        }

        private T SerializeTypeReferenceModifiers<T>(CustomModifiersEncoder<T> encoder, ref ITypeReference typeReference)
            where T : IBlobEncoder
        {
            var modifiedTypeReference = typeReference as IModifiedTypeReference;
            if (modifiedTypeReference == null)
            {
                return encoder.EndModifiers();
            }

            typeReference = modifiedTypeReference.UnmodifiedType;
            return SerializeCustomModifiers(encoder, modifiedTypeReference.CustomModifiers);
        }

        private T SerializeCustomModifiers<T>(CustomModifiersEncoder<T> encoder, ImmutableArray<ICustomModifier> modifiers)
            where T : IBlobEncoder
        {
            return SerializeCustomModifiers<T>(encoder, modifiers, 0, modifiers.Length);
        }

        private T SerializeCustomModifiers<T>(CustomModifiersEncoder<T> encoder, ImmutableArray<ICustomModifier> modifiers, int start, int count)
            where T : IBlobEncoder
        {
            for (int i = 0; i < count; i++)
            {
                var modifier = modifiers[start + i];
                encoder = encoder.AddModifier(modifier.IsOptional, GetTypeDefOrRefCodedIndex(modifier.GetModifier(Context), true));
            }

            return encoder.EndModifiers();
        }

        private uint GetNumberOfInheritedTypeParameters(ITypeReference type)
        {
            INestedTypeReference nestedType = type.AsNestedTypeReference;
            if (nestedType == null)
            {
                return 0;
            }

            ISpecializedNestedTypeReference specializedNestedType = nestedType.AsSpecializedNestedTypeReference;
            if (specializedNestedType != null)
            {
                nestedType = specializedNestedType.UnspecializedVersion;
            }

            uint result = 0;
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

        protected static uint RowOnly(uint token)
        {
            return token & 0xFFFFFF;
        }

        protected static uint TypeOnly(uint token)
        {
            return token & 0xFF000000;
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

            public HeapOrReferenceIndex(MetadataWriter writer, IEqualityComparer<T> comparer, int lastRowId = 0)
                : this(writer, new Dictionary<T, int>(comparer), lastRowId)
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
