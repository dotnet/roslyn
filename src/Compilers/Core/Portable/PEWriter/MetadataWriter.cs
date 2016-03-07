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

        // If true, it is allowed to have methods not have bodies (for emitting metadata-only assembly)
        internal readonly bool allowMissingMethodBodies;

        // A map of method body before token translation to RVA. Used for deduplication of small bodies.
        private readonly Dictionary<ImmutableArray<byte>, int> _smallMethodBodies;

        protected MetadataWriter(
            MetadataHeapsBuilder heaps,
            MetadataHeapsBuilder debugHeapsOpt,
            DynamicAnalysisDataWriter dynamicAnalysisDataWriterOpt,
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

            this.heaps = heaps;
            _debugHeapsOpt = debugHeapsOpt;
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
        protected abstract void PopulateEventMapTableRows(List<EventMapRow> table);

        /// <summary>
        /// Populate PropertyMap table.
        /// </summary>
        protected abstract void PopulatePropertyMapTableRows(List<PropertyMapRow> table);

        /// <summary>
        /// Populate EncLog table.
        /// </summary>
        protected abstract void PopulateEncLogTableRows(List<EncLogRow> table, ImmutableArray<int> rowCounts);

        /// <summary>
        /// Populate EncMap table.
        /// </summary>
        protected abstract void PopulateEncMapTableRows(List<EncMapRow> table, ImmutableArray<int> rowCounts);

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
        private bool _userStringTokenOverflow;
        private List<string> _pseudoStringTokenToStringMap;
        private ReferenceIndexer _referenceVisitor;

        protected readonly MetadataHeapsBuilder heaps;

        // A heap builder distinct from heaps if we are emitting debug information into a separate Portable PDB stream.
        // Shared heap builder (reference equals heaps) if we are embedding Portable PDB into the metadata stream.
        // Null otherwise.
        private readonly MetadataHeapsBuilder _debugHeapsOpt;

        private readonly DynamicAnalysisDataWriter _dynamicAnalysisDataWriterOpt;

        private bool EmitStandaloneDebugMetadata => _debugHeapsOpt != null && heaps != _debugHeapsOpt;

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

        internal const int MappedFieldDataAlignment = 8;
        internal const int ManagedResourcesDataAlignment = 8;

        internal IModule Module => module;

        private ImmutableArray<int> GetRowCounts()
        {
            var rowCounts = new int[MetadataTokens.TableCount];

            rowCounts[(int)TableIndex.Assembly] = EmitAssemblyDefinition ? 1 : 0;
            rowCounts[(int)TableIndex.AssemblyRef] = _assemblyRefTable.Count;
            rowCounts[(int)TableIndex.ClassLayout] = _classLayoutTable.Count;
            rowCounts[(int)TableIndex.Constant] = _constantTable.Count;
            rowCounts[(int)TableIndex.CustomAttribute] = _customAttributeTable.Count;
            rowCounts[(int)TableIndex.DeclSecurity] = _declSecurityTable.Count;
            rowCounts[(int)TableIndex.EncLog] = _encLogTable.Count;
            rowCounts[(int)TableIndex.EncMap] = _encMapTable.Count;
            rowCounts[(int)TableIndex.EventMap] = _eventMapTable.Count;
            rowCounts[(int)TableIndex.Event] = _eventTable.Count;
            rowCounts[(int)TableIndex.ExportedType] = _exportedTypeTable.Count;
            rowCounts[(int)TableIndex.FieldLayout] = _fieldLayoutTable.Count;
            rowCounts[(int)TableIndex.FieldMarshal] = _fieldMarshalTable.Count;
            rowCounts[(int)TableIndex.FieldRva] = _fieldRvaTable.Count;
            rowCounts[(int)TableIndex.Field] = _fieldDefTable.Count;
            rowCounts[(int)TableIndex.File] = _fileTable.Count;
            rowCounts[(int)TableIndex.GenericParamConstraint] = _genericParamConstraintTable.Count;
            rowCounts[(int)TableIndex.GenericParam] = _genericParamTable.Count;
            rowCounts[(int)TableIndex.ImplMap] = _implMapTable.Count;
            rowCounts[(int)TableIndex.InterfaceImpl] = _interfaceImplTable.Count;
            rowCounts[(int)TableIndex.ManifestResource] = _manifestResourceTable.Count;
            rowCounts[(int)TableIndex.MemberRef] = _memberRefTable.Count;
            rowCounts[(int)TableIndex.MethodImpl] = _methodImplTable.Count;
            rowCounts[(int)TableIndex.MethodSemantics] = _methodSemanticsTable.Count;
            rowCounts[(int)TableIndex.MethodSpec] = _methodSpecTable.Count;
            rowCounts[(int)TableIndex.MethodDef] = _methodTable.Length;
            rowCounts[(int)TableIndex.ModuleRef] = _moduleRefTable.Count;
            rowCounts[(int)TableIndex.Module] = 1;
            rowCounts[(int)TableIndex.NestedClass] = _nestedClassTable.Count;
            rowCounts[(int)TableIndex.Param] = _paramTable.Count;
            rowCounts[(int)TableIndex.PropertyMap] = _propertyMapTable.Count;
            rowCounts[(int)TableIndex.Property] = _propertyTable.Count;
            rowCounts[(int)TableIndex.StandAloneSig] = GetStandAloneSignatures().Count;
            rowCounts[(int)TableIndex.TypeDef] = _typeDefTable.Count;
            rowCounts[(int)TableIndex.TypeRef] = _typeRefTable.Count;
            rowCounts[(int)TableIndex.TypeSpec] = _typeSpecTable.Count;

            rowCounts[(int)TableIndex.Document] = _documentTable.Count;
            rowCounts[(int)TableIndex.MethodDebugInformation] = _methodDebugInformationTable.Count;
            rowCounts[(int)TableIndex.LocalScope] = _localScopeTable.Count;
            rowCounts[(int)TableIndex.LocalVariable] = _localVariableTable.Count;
            rowCounts[(int)TableIndex.LocalConstant] = _localConstantTable.Count;
            rowCounts[(int)TableIndex.StateMachineMethod] = _stateMachineMethodTable.Count;
            rowCounts[(int)TableIndex.ImportScope] = _importScopeTable.Count;
            rowCounts[(int)TableIndex.CustomDebugInformation] = _customDebugInformationTable.Count;

            return ImmutableArray.CreateRange(rowCounts);
        }

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
            ArrayBuilder<IParameterDefinition> builder = null;
            var parameters = methodDef.Parameters;

            if (methodDef.ReturnValueIsMarshalledExplicitly || IteratorHelper.EnumerableIsNotEmpty(methodDef.ReturnValueAttributes))
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
            this.SerializeCustomAttributeSignature(customAttribute, false, writer);
            result = heaps.GetBlobIndex(writer);
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
                ? ((uint)this.GetMethodDefIndex(methodDef) << 3) | 2
                : ((uint)this.GetMemberRefIndex(methodReference) << 3) | 3;
        }

        public static ushort GetEventFlags(IEventDefinition eventDef)
        {
            ushort result = 0;
            if (eventDef.IsSpecialName)
            {
                result |= 0x0200;
            }

            if (eventDef.IsRuntimeSpecial)
            {
                result |= 0x0400;
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

        public static ushort GetFieldFlags(IFieldDefinition fieldDef)
        {
            ushort result = GetTypeMemberVisibilityFlags(fieldDef);
            if (fieldDef.IsStatic)
            {
                result |= 0x0010;
            }

            if (fieldDef.IsReadOnly)
            {
                result |= 0x0020;
            }

            if (fieldDef.IsCompileTimeConstant)
            {
                result |= 0x0040;
            }

            if (fieldDef.IsNotSerialized)
            {
                result |= 0x0080;
            }

            if (!fieldDef.MappedData.IsDefault)
            {
                result |= 0x0100;
            }

            if (fieldDef.IsSpecialName)
            {
                result |= 0x0200;
            }

            if (fieldDef.IsRuntimeSpecial)
            {
                result |= 0x0400;
            }

            if (fieldDef.IsMarshalledExplicitly)
            {
                result |= 0x1000;
            }

            if (fieldDef.IsCompileTimeConstant)
            {
                result |= 0x8000;
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
            result = heaps.GetBlobIndex(writer);
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

        private static ushort GetGenericParamFlags(IGenericParameter genPar)
        {
            ushort result = 0;
            switch (genPar.Variance)
            {
                case TypeParameterVariance.Covariant:
                    result |= 0x0001;
                    break;
                case TypeParameterVariance.Contravariant:
                    result |= 0x0002;
                    break;
            }

            if (genPar.MustBeReferenceType)
            {
                result |= 0x0004;
            }

            if (genPar.MustBeValueType)
            {
                result |= 0x0008;
            }

            if (genPar.MustHaveDefaultConstructor)
            {
                result |= 0x0010;
            }

            return result;
        }

        private uint GetImplementationCodedIndex(INamespaceTypeReference namespaceRef)
        {
            IUnitReference uref = namespaceRef.GetUnit(Context);
            IAssemblyReference aref = uref as IAssemblyReference;
            if (aref != null)
            {
                return ((uint)this.GetAssemblyRefIndex(aref) << 2) | 1;
            }

            IModuleReference mref = uref as IModuleReference;
            if (mref != null)
            {
                aref = mref.GetContainingAssembly(Context);
                return aref == null || ReferenceEquals(aref, this.module.GetContainingAssembly(Context))
                    ? ((uint)this.GetFileRefIndex(mref) << 2) | 0
                    : ((uint)this.GetAssemblyRefIndex(aref) << 2) | 1;
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

        private static uint GetManagedResourceOffset(BlobBuilder resource, BlobBuilder resourceWriter)
        {
            int result = resourceWriter.Position;
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
                    IFieldReference fieldRef = memberRef as IFieldReference;
                    if (fieldRef != null)
                    {
                        return (uint)parentTypeDefIndex << 3;
                    }

                    IMethodReference methodRef = memberRef as IMethodReference;
                    if (methodRef != null)
                    {
                        if (methodRef.AcceptsExtraArguments)
                        {
                            int methodIndex;
                            if (this.TryGetMethodDefIndex(methodRef.GetResolvedMethod(Context), out methodIndex))
                            {
                                return ((uint)methodIndex << 3) | 3;
                            }
                        }

                        return (uint)parentTypeDefIndex << 3;
                    }
                    // TODO: error
                }
            }

            // TODO: special treatment for global fields and methods. Object model support would be nice.
            return memberRef.GetContainingType(Context).IsTypeSpecification()
                ? ((uint)this.GetTypeSpecIndex(memberRef.GetContainingType(Context)) << 3) | 4
                : ((uint)this.GetTypeRefIndex(memberRef.GetContainingType(Context)) << 3) | 1;
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
                ? (uint)this.GetMethodDefIndex(methodDef) << 1
                : ((uint)this.GetMemberRefIndex(methodReference) << 1) | 1;
        }

        public static ushort GetMethodFlags(IMethodDefinition methodDef)
        {
            ushort result = GetTypeMemberVisibilityFlags(methodDef);
            if (methodDef.IsStatic)
            {
                result |= 0x0010;
            }

            if (methodDef.IsSealed)
            {
                result |= 0x0020;
            }

            if (methodDef.IsVirtual)
            {
                result |= 0x0040;
            }

            if (methodDef.IsHiddenBySignature)
            {
                result |= 0x0080;
            }

            if (methodDef.IsNewSlot)
            {
                result |= 0x0100;
            }

            if (methodDef.IsAccessCheckedOnOverride)
            {
                result |= 0x0200;
            }

            if (methodDef.IsAbstract)
            {
                result |= 0x0400;
            }

            if (methodDef.IsSpecialName)
            {
                result |= 0x0800;
            }

            if (methodDef.IsRuntimeSpecial)
            {
                result |= 0x1000;
            }

            if (methodDef.IsPlatformInvoke)
            {
                result |= 0x2000;
            }

            if (methodDef.HasDeclarativeSecurity)
            {
                result |= 0x4000;
            }

            if (methodDef.RequiresSecurityObject)
            {
                result |= 0x8000;
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

            var writer = PooledBlobBuilder.GetInstance();
            writer.WriteByte(0x0A);
            writer.WriteCompressedInteger(methodInstanceReference.GetGenericMethod(Context).GenericParameterCount);
            foreach (ITypeReference typeref in methodInstanceReference.GetGenericArguments(Context))
            {
                this.SerializeTypeReference(typeref, writer, false, true);
            }

            result = heaps.GetBlobIndex(writer);
            _methodInstanceSignatureIndex.Add(methodInstanceReference, result);
            writer.Free();
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
            result = heaps.GetBlobIndex(writer);
            _marshallingDescriptorIndex.Add(marshallingInformation, result);
            writer.Free();
            return result;
        }

        private BlobIdx GetMarshallingDescriptorIndex(ImmutableArray<byte> descriptor)
        {
            return heaps.GetBlobIndex(descriptor);
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

            var writer = PooledBlobBuilder.GetInstance();
            this.SerializeSignature(methodReference, methodReference.GenericParameterCount, methodReference.ExtraParameters, writer);

            signatureBlob = writer.ToImmutableArray();
            result = heaps.GetBlobIndex(signatureBlob);
            _signatureIndex.Add(methodReference, KeyValuePair.Create(result, signatureBlob));
            writer.Free();
            return result;
        }

        private BlobIdx GetGenericMethodInstanceIndex(IGenericMethodInstanceReference genericMethodInstanceReference)
        {
            var writer = PooledBlobBuilder.GetInstance();
            this.SerializeGenericMethodInstanceSignature(writer, genericMethodInstanceReference);
            BlobIdx result = heaps.GetBlobIndex(writer);
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

        public static ushort GetParameterFlags(IParameterDefinition parDef)
        {
            ushort result = 0;
            if (parDef.IsIn)
            {
                result |= 0x0001;
            }

            if (parDef.IsOut)
            {
                result |= 0x0002;
            }

            if (parDef.IsOptional)
            {
                result |= 0x0010;
            }

            if (parDef.HasDefaultValue)
            {
                result |= 0x1000;
            }

            if (parDef.IsMarshalledExplicitly)
            {
                result |= 0x2000;
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
                result = heaps.GetBlobIndex(writer);
            }
            finally
            {
                writer.Free();
            }

            return result;
        }

        public static ushort GetPropertyFlags(IPropertyDefinition propertyDef)
        {
            ushort result = 0;
            if (propertyDef.IsSpecialName)
            {
                result |= 0x0200;
            }

            if (propertyDef.IsRuntimeSpecial)
            {
                result |= 0x0400;
            }

            if (propertyDef.HasDefaultValue)
            {
                result |= 0x1000;
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

            var writer = PooledBlobBuilder.GetInstance();
            this.SerializeSignature(propertyDef, 0, ImmutableArray<IParameterTypeInformation>.Empty, writer);
            var blob = writer.ToImmutableArray();
            var result = heaps.GetBlobIndex(blob);
            _signatureIndex.Add(propertyDef, KeyValuePair.Create(result, blob));
            writer.Free();
            return result;
        }

        private uint GetResolutionScopeCodedIndex(ITypeReference typeReference)
        {
            return ((uint)this.GetTypeRefIndex(typeReference) << 2) | 3;
        }

        private uint GetResolutionScopeCodedIndex(IUnitReference unitReference)
        {
            IAssemblyReference aref = unitReference as IAssemblyReference;
            if (aref != null)
            {
                return ((uint)this.GetAssemblyRefIndex(aref) << 2) | 2;
            }

            IModuleReference mref = unitReference as IModuleReference;
            if (mref != null)
            {
                // If this is a module from a referenced multi-module assembly,
                // the assembly should be used as the resolution scope.
                aref = mref.GetContainingAssembly(Context);

                if (aref != null && aref != module.AsAssembly)
                {
                    return ((uint)this.GetAssemblyRefIndex(aref) << 2) | 2;
                }

                return ((uint)this.GetModuleRefIndex(mref.Name) << 2) | 1;
            }

            // TODO: error
            return 0;
        }

        private StringIdx GetStringIndexForPathAndCheckLength(string path, INamedEntity errorEntity = null)
        {
            CheckPathLength(path, errorEntity);
            return heaps.GetStringIndex(path);
        }

        private StringIdx GetStringIndexForNameAndCheckLength(string name, INamedEntity errorEntity = null)
        {
            CheckNameLength(name, errorEntity);
            return heaps.GetStringIndex(name);
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
            return heaps.GetStringIndex(namespaceName);
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

        private static SignatureTypeCode GetConstantTypeCode(object val)
        {
            if (val == null)
            {
                // The encoding of Type for the nullref value for FieldInit is ELEMENT_TYPE_CLASS with a Value of a zero.
                return Constants.SignatureTypeCode_Class;
            }

            Debug.Assert(!val.GetType().GetTypeInfo().IsEnum);

            // Perf: Note that JIT optimizes each expression val.GetType() == typeof(T) to a single register comparison.
            // Also the checks are sorted by commonality of the checked types.

            if (val.GetType() == typeof(int))
            {
                return SignatureTypeCode.Int32;
            }

            if (val.GetType() == typeof(string))
            {
                return SignatureTypeCode.String;
            }

            if (val.GetType() == typeof(bool))
            {
                return SignatureTypeCode.Boolean;
            }

            if (val.GetType() == typeof(char))
            {
                return SignatureTypeCode.Char;
            }

            if (val.GetType() == typeof(byte))
            {
                return SignatureTypeCode.Byte;
            }

            if (val.GetType() == typeof(long))
            {
                return SignatureTypeCode.Int64;
            }

            if (val.GetType() == typeof(double))
            {
                return SignatureTypeCode.Double;
            }

            if (val.GetType() == typeof(short))
            {
                return SignatureTypeCode.Int16;
            }

            if (val.GetType() == typeof(ushort))
            {
                return SignatureTypeCode.UInt16;
            }

            if (val.GetType() == typeof(uint))
            {
                return SignatureTypeCode.UInt32;
            }

            if (val.GetType() == typeof(sbyte))
            {
                return SignatureTypeCode.SByte;
            }

            if (val.GetType() == typeof(ulong))
            {
                return SignatureTypeCode.UInt64;
            }

            if (val.GetType() == typeof(float))
            {
                return SignatureTypeCode.Single;
            }

            throw ExceptionUtilities.UnexpectedValue(val);
        }

        internal uint GetTypeDefFlags(ITypeDefinition typeDef)
        {
            return GetTypeDefFlags(typeDef, Context);
        }

        public static uint GetTypeDefFlags(ITypeDefinition typeDef, EmitContext context)
        {
            TypeAttributes result = default(TypeAttributes);

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

                return (uint)result;
            }

            INamespaceTypeDefinition namespaceTypeDef = typeDef.AsNamespaceTypeDefinition(context);
            if (namespaceTypeDef != null && namespaceTypeDef.IsPublic)
            {
                result |= TypeAttributes.Public;
            }

            return (uint)result;
        }

        private uint GetTypeDefOrRefCodedIndex(ITypeReference typeReference, bool treatRefAsPotentialTypeSpec)
        {
            int typeDefIndex;
            var typeDefinition = typeReference.AsTypeDefinition(this.Context);
            if ((typeDefinition != null) && this.TryGetTypeDefIndex(typeDefinition, out typeDefIndex))
            {
                return (uint)((typeDefIndex << 2) | 0);
            }

            return treatRefAsPotentialTypeSpec && typeReference.IsTypeSpecification()
                ? ((uint)this.GetTypeSpecIndex(typeReference) << 2) | 2
                : ((uint)this.GetTypeRefIndex(typeReference) << 2) | 1;
        }

        private static ushort GetTypeMemberVisibilityFlags(ITypeDefinitionMember member)
        {
            ushort result = 0;
            switch (member.Visibility)
            {
                case TypeMemberVisibility.Private:
                    result |= 0x00000001;
                    break;
                case TypeMemberVisibility.FamilyAndAssembly:
                    result |= 0x00000002;
                    break;
                case TypeMemberVisibility.Assembly:
                    result |= 0x00000003;
                    break;
                case TypeMemberVisibility.Family:
                    result |= 0x00000004;
                    break;
                case TypeMemberVisibility.FamilyOrAssembly:
                    result |= 0x00000005;
                    break;
                case TypeMemberVisibility.Public:
                    result |= 0x00000006;
                    break;
            }

            return result;
        }

        private uint GetTypeOrMethodDefCodedIndex(IGenericParameter genPar)
        {
            IGenericTypeParameter genTypePar = genPar.AsGenericTypeParameter;
            if (genTypePar != null)
            {
                return (uint)this.GetTypeDefIndex(genTypePar.DefiningType) << 1;
            }

            IGenericMethodParameter genMethPar = genPar.AsGenericMethodParameter;
            if (genMethPar != null)
            {
                return ((uint)this.GetMethodDefIndex(genMethPar.DefiningMethod) << 1) | 1;
            }            // TODO: error

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

            var writer = PooledBlobBuilder.GetInstance();
            this.SerializeTypeReference(typeReference, writer, false, true);
            result = heaps.GetBlobIndex(writer);
            _typeSpecSignatureIndex.Add(typeReference, result);
            writer.Free();
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

        private void SerializeCustomModifiers(ImmutableArray<ICustomModifier> customModifiers, BlobBuilder writer)
        {
            foreach (ICustomModifier customModifier in customModifiers)
            {
                this.SerializeCustomModifier(customModifier, writer);
            }
        }

        private void SerializeCustomModifier(ICustomModifier customModifier, BlobBuilder writer)
        {
            if (customModifier.IsOptional)
            {
                writer.WriteByte(0x20);
            }
            else
            {
                writer.WriteByte(0x1f);
            }

            writer.WriteCompressedInteger(this.GetTypeDefOrRefCodedIndex(customModifier.GetModifier(Context), true));
        }

        private void SerializeMetadataHeader(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            int startOffset = writer.Position;

            // signature
            writer.WriteUInt32(0x424A5342);

            // major version
            writer.WriteUInt16(1);

            // minor version
            writer.WriteUInt16(1);

            // reserved
            writer.WriteUInt32(0);

            // metadata version length
            writer.WriteUInt32(MetadataSizes.MetadataVersionPaddedLength);

            string targetRuntimeVersion = metadataSizes.IsStandaloneDebugMetadata ? "PDB v1.0" : module.Properties.TargetRuntimeVersion;

            int n = Math.Min(MetadataSizes.MetadataVersionPaddedLength, targetRuntimeVersion.Length);
            for (int i = 0; i < n; i++)
            {
                writer.WriteByte((byte)targetRuntimeVersion[i]);
            }

            for (int i = n; i < MetadataSizes.MetadataVersionPaddedLength; i++)
            {
                writer.WriteByte(0);
            }

            // reserved
            writer.WriteUInt16(0);

            // number of streams
            writer.WriteUInt16((ushort)(5 + (metadataSizes.IsMinimalDelta ? 1 : 0) + (metadataSizes.IsStandaloneDebugMetadata ? 1 : 0)));

            // stream headers
            int offsetFromStartOfMetadata = metadataSizes.MetadataHeaderSize;

            // emit the #Pdb stream first so that only a single page has to be read in order to find out PDB ID
            if (metadataSizes.IsStandaloneDebugMetadata)
            {
                SerializeStreamHeader(ref offsetFromStartOfMetadata, metadataSizes.StandalonePdbStreamSize, "#Pdb", writer);
            }

            // Spec: Some compilers store metadata in a #- stream, which holds an uncompressed, or non-optimized, representation of metadata tables;
            // this includes extra metadata -Ptr tables. Such PE files do not form part of ECMA-335 standard.
            //
            // Note: EnC delta is stored as uncompressed metadata stream.
            SerializeStreamHeader(ref offsetFromStartOfMetadata, metadataSizes.MetadataTableStreamSize, (metadataSizes.IsMetadataTableStreamCompressed ? "#~" : "#-"), writer);

            SerializeStreamHeader(ref offsetFromStartOfMetadata, metadataSizes.GetAlignedHeapSize(HeapIndex.String), "#Strings", writer);
            SerializeStreamHeader(ref offsetFromStartOfMetadata, metadataSizes.GetAlignedHeapSize(HeapIndex.UserString), "#US", writer);
            SerializeStreamHeader(ref offsetFromStartOfMetadata, metadataSizes.GetAlignedHeapSize(HeapIndex.Guid), "#GUID", writer);
            SerializeStreamHeader(ref offsetFromStartOfMetadata, metadataSizes.GetAlignedHeapSize(HeapIndex.Blob), "#Blob", writer);

            if (metadataSizes.IsMinimalDelta)
            {
                SerializeStreamHeader(ref offsetFromStartOfMetadata, 0, "#JTD", writer);
            }

            int endOffset = writer.Position;
            Debug.Assert(endOffset - startOffset == metadataSizes.MetadataHeaderSize);
        }

        private static void SerializeStreamHeader(ref int offsetFromStartOfMetadata, int alignedStreamSize, string streamName, BlobBuilder writer)
        {
            // 4 for the first uint (offset), 4 for the second uint (padded size), length of stream name + 1 for null terminator (then padded)
            int sizeOfStreamHeader = MetadataSizes.GetMetadataStreamHeaderSize(streamName);
            writer.WriteInt32(offsetFromStartOfMetadata);
            writer.WriteInt32(alignedStreamSize);
            foreach (char ch in streamName)
            {
                writer.WriteByte((byte)ch);
            }

            // After offset, size, and stream name, write 0-bytes until we reach our padded size.
            for (uint i = 8 + (uint)streamName.Length; i < sizeOfStreamHeader; i++)
            {
                writer.WriteByte(0);
            }

            offsetFromStartOfMetadata += alignedStreamSize;
        }

        public void WriteMetadataAndIL(PdbWriter pdbWriterOpt, Stream metadataStream, Stream ilStream, out MetadataSizes metadataSizes)
        {
            pdbWriterOpt?.SetMetadataEmitter(this);

            // TODO: we can precalculate the exact size of IL stream
            var ilWriter = new BlobBuilder(1024);
            var metadataWriter = new BlobBuilder(4 * 1024);
            var mappedFieldDataWriter = new BlobBuilder(0);
            var managedResourceDataWriter = new BlobBuilder(0);

            // Add 4B of padding to the start of the separated IL stream, 
            // so that method RVAs, which are offsets to this stream, are never 0.
            ilWriter.WriteUInt32(0);

            // this is used to handle edit-and-continue emit, so we should have a module
            // version ID that is imposed by the caller (the same as the previous module version ID).
            // Therefore we do not have to fill in a new module version ID in the generated metadata
            // stream.
            Debug.Assert(this.module.Properties.PersistentIdentifier != default(Guid));

            int moduleVersionIdOffsetInMetadataStream;
            int pdbIdOffsetInMetadataStream;
            int entryPointToken;

            SerializeMetadataAndIL(
                metadataWriter,
                default(BlobBuilder),
                pdbWriterOpt,
                ilWriter,
                mappedFieldDataWriter,
                managedResourceDataWriter,
                methodBodyStreamRva: 0,
                calculateMappedFieldDataStreamRva: _ => 0,
                moduleVersionIdOffsetInMetadataStream: out moduleVersionIdOffsetInMetadataStream,
                pdbIdOffsetInPortablePdbStream: out pdbIdOffsetInMetadataStream,
                metadataSizes: out metadataSizes,
                entryPointToken: out entryPointToken);

            ilWriter.WriteContentTo(ilStream);
            metadataWriter.WriteContentTo(metadataStream);

            Debug.Assert(entryPointToken == 0);
            Debug.Assert(mappedFieldDataWriter.Count == 0);
            Debug.Assert(managedResourceDataWriter.Count == 0);
            Debug.Assert(pdbIdOffsetInMetadataStream == 0);
        }

        public void SerializeMetadataAndIL(
            BlobBuilder metadataWriter,
            BlobBuilder debugMetadataWriterOpt,
            PdbWriter nativePdbWriterOpt,
            BlobBuilder ilWriter,
            BlobBuilder mappedFieldDataWriter,
            BlobBuilder managedResourceDataWriter,
            int methodBodyStreamRva,
            Func<MetadataSizes, int> calculateMappedFieldDataStreamRva,
            out int moduleVersionIdOffsetInMetadataStream,
            out int pdbIdOffsetInPortablePdbStream,
            out int entryPointToken,
            out MetadataSizes metadataSizes)
        {
            // Extract information from object model into tables, indices and streams
            CreateIndices();

            if (_debugHeapsOpt != null)
            {
                DefineModuleImportScope();
            }

            int[] methodBodyRvas = SerializeMethodBodies(ilWriter, nativePdbWriterOpt);

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

            PopulateTables(methodBodyRvas, mappedFieldDataWriter, managedResourceDataWriter, dynamicAnalysisDataOpt);

            int debugEntryPointToken;
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

                // entry point can only be a MethodDef:
                Debug.Assert(entryPointToken == 0 || (entryPointToken & 0xff000000) == 0x06000000);
                Debug.Assert(debugEntryPointToken == 0 || (debugEntryPointToken & 0xff000000) == 0x06000000);

                if (debugEntryPointToken != 0)
                {
                    nativePdbWriterOpt?.SetEntryPoint((uint)debugEntryPointToken);
                }
            }
            else
            {
                entryPointToken = debugEntryPointToken = 0;
            }

            heaps.Complete();

            var tableRowCounts = GetRowCounts();

            metadataSizes = new MetadataSizes(
                rowCounts: tableRowCounts,
                heapSizes: heaps.GetHeapSizes(),
                ilStreamSize: ilWriter.Count,
                mappedFieldDataSize: mappedFieldDataWriter.Count,
                resourceDataSize: managedResourceDataWriter.Count,
                strongNameSignatureSize: CalculateStrongNameSignatureSize(module),
                isMinimalDelta: IsMinimalDelta,
                emitStandaloneDebugMetadata: EmitStandaloneDebugMetadata,
                isStandaloneDebugMetadata: false);

            int mappedFieldDataStreamRva = calculateMappedFieldDataStreamRva(metadataSizes);

            int guidHeapStartOffset;
            SerializeMetadata(metadataWriter, metadataSizes, methodBodyStreamRva, mappedFieldDataStreamRva, debugEntryPointToken, out guidHeapStartOffset, out pdbIdOffsetInPortablePdbStream);
            moduleVersionIdOffsetInMetadataStream = GetModuleVersionGuidOffsetInMetadataStream(guidHeapStartOffset);
            Debug.Assert(pdbIdOffsetInPortablePdbStream == 0);

            if (!EmitStandaloneDebugMetadata)
            {
                return;
            }

            // serialize debug metadata stream

            Debug.Assert(_debugHeapsOpt != null);
            _debugHeapsOpt.Complete();

            var debugMetadataSizes = new MetadataSizes(
                rowCounts: tableRowCounts,
                heapSizes: _debugHeapsOpt.GetHeapSizes(),
                ilStreamSize: 0,
                mappedFieldDataSize: 0,
                resourceDataSize: 0,
                strongNameSignatureSize: 0,
                isMinimalDelta: IsMinimalDelta,
                emitStandaloneDebugMetadata: true,
                isStandaloneDebugMetadata: true);

            SerializeMetadata(debugMetadataWriterOpt, debugMetadataSizes, 0, 0, debugEntryPointToken, out guidHeapStartOffset, out pdbIdOffsetInPortablePdbStream);
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

        private void SerializeMetadata(
            BlobBuilder metadataWriter,
            MetadataSizes metadataSizes,
            int methodBodyStreamRva,
            int mappedFieldDataStreamRva,
            int debugEntryPointToken,
            out int guidHeapStartOffset,
            out int pdbIdOffset)
        {
            // header:
            SerializeMetadataHeader(metadataWriter, metadataSizes);

            // #Pdb stream
            if (metadataSizes.IsStandaloneDebugMetadata)
            {
                SerializeStandalonePdbStream(metadataWriter, metadataSizes, debugEntryPointToken, out pdbIdOffset);
            }
            else
            {
                pdbIdOffset = 0;
            }

            // #~ or #- stream:
            SerializeMetadataTables(metadataWriter, metadataSizes, methodBodyStreamRva, mappedFieldDataStreamRva);

            // #Strings, #US, #Guid and #Blob streams:
            (metadataSizes.IsStandaloneDebugMetadata ? _debugHeapsOpt : heaps).WriteTo(metadataWriter, out guidHeapStartOffset);
        }

        private int GetModuleVersionGuidOffsetInMetadataStream(int guidHeapOffsetInMetadataStream)
        {
            // index of module version ID in the guidWriter stream
            int moduleVersionIdIndex = _moduleRow.ModuleVersionId;

            // offset into the guidWriter stream of the module version ID
            int moduleVersionOffsetInGuidTable = (moduleVersionIdIndex - 1) << 4;

            return guidHeapOffsetInMetadataStream + moduleVersionOffsetInGuidTable;
        }

        private void SerializeMetadataTables(
            BlobBuilder writer,
            MetadataSizes metadataSizes,
            int methodBodyStreamRva,
            int mappedFieldDataStreamRva)
        {
            int startPosition = writer.Position;

            this.SerializeTablesHeader(writer, metadataSizes);

            if (metadataSizes.IsPresent(TableIndex.Module))
            {
                SerializeModuleTable(writer, metadataSizes, heaps);
            }

            if (metadataSizes.IsPresent(TableIndex.TypeRef))
            {
                this.SerializeTypeRefTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.TypeDef))
            {
                this.SerializeTypeDefTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.Field))
            {
                this.SerializeFieldTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.MethodDef))
            {
                this.SerializeMethodDefTable(writer, metadataSizes, methodBodyStreamRva);
            }

            if (metadataSizes.IsPresent(TableIndex.Param))
            {
                this.SerializeParamTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.InterfaceImpl))
            {
                this.SerializeInterfaceImplTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.MemberRef))
            {
                this.SerializeMemberRefTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.Constant))
            {
                this.SerializeConstantTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.CustomAttribute))
            {
                this.SerializeCustomAttributeTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.FieldMarshal))
            {
                this.SerializeFieldMarshalTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.DeclSecurity))
            {
                this.SerializeDeclSecurityTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.ClassLayout))
            {
                this.SerializeClassLayoutTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.FieldLayout))
            {
                this.SerializeFieldLayoutTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.StandAloneSig))
            {
                this.SerializeStandAloneSigTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.EventMap))
            {
                this.SerializeEventMapTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.Event))
            {
                this.SerializeEventTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.PropertyMap))
            {
                this.SerializePropertyMapTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.Property))
            {
                this.SerializePropertyTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.MethodSemantics))
            {
                this.SerializeMethodSemanticsTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.MethodImpl))
            {
                this.SerializeMethodImplTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.ModuleRef))
            {
                this.SerializeModuleRefTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.TypeSpec))
            {
                this.SerializeTypeSpecTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.ImplMap))
            {
                this.SerializeImplMapTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.FieldRva))
            {
                this.SerializeFieldRvaTable(writer, metadataSizes, mappedFieldDataStreamRva);
            }

            if (metadataSizes.IsPresent(TableIndex.EncLog))
            {
                this.SerializeEncLogTable(writer);
            }

            if (metadataSizes.IsPresent(TableIndex.EncMap))
            {
                this.SerializeEncMapTable(writer);
            }

            if (metadataSizes.IsPresent(TableIndex.Assembly))
            {
                this.SerializeAssemblyTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.AssemblyRef))
            {
                this.SerializeAssemblyRefTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.File))
            {
                this.SerializeFileTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.ExportedType))
            {
                this.SerializeExportedTypeTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.ManifestResource))
            {
                this.SerializeManifestResourceTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.NestedClass))
            {
                this.SerializeNestedClassTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.GenericParam))
            {
                this.SerializeGenericParamTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.MethodSpec))
            {
                this.SerializeMethodSpecTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.GenericParamConstraint))
            {
                this.SerializeGenericParamConstraintTable(writer, metadataSizes);
            }

            // debug tables
            if (metadataSizes.IsPresent(TableIndex.Document))
            {
                this.SerializeDocumentTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.MethodDebugInformation))
            {
                this.SerializeMethodDebugInformationTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.LocalScope))
            {
                this.SerializeLocalScopeTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.LocalVariable))
            {
                this.SerializeLocalVariableTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.LocalConstant))
            {
                this.SerializeLocalConstantTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.ImportScope))
            {
                this.SerializeImportScopeTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.StateMachineMethod))
            {
                this.SerializeStateMachineMethodTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.CustomDebugInformation))
            {
                this.SerializeCustomDebugInformationTable(writer, metadataSizes);
            }

            writer.WriteByte(0);
            writer.Align(4);

            int endPosition = writer.Position;
            Debug.Assert(metadataSizes.MetadataTableStreamSize == endPosition - startPosition);
        }

        private void PopulateTables(int[] methodBodyRvas, BlobBuilder mappedFieldDataWriter, BlobBuilder resourceWriter, BlobBuilder dynamicAnalysisDataOpt)
        {
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
            this.PopulateGenericParamTableRows();
            this.PopulateGenericParamConstraintTableRows();
            this.PopulateImplMapTableRows();
            this.PopulateInterfaceImplTableRows();
            this.PopulateManifestResourceTableRows(resourceWriter, dynamicAnalysisDataOpt);
            this.PopulateMemberRefTableRows();
            this.PopulateMethodImplTableRows();
            this.PopulateMethodTableRows(methodBodyRvas);
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

            // This table is populated after the others because it depends on the order of the entries of the generic parameter table.
            this.PopulateCustomAttributeTableRows();

            ImmutableArray<int> rowCounts = GetRowCounts();
            Debug.Assert(rowCounts[(int)TableIndex.EncLog] == 0 && rowCounts[(int)TableIndex.EncMap] == 0);

            this.PopulateEncLogTableRows(_encLogTable, rowCounts);
            this.PopulateEncMapTableRows(_encMapTable, rowCounts);
        }

        private struct AssemblyRefTableRow
        {
            public Version Version;
            public BlobIdx PublicKeyToken;
            public StringIdx Name;
            public StringIdx Culture;
            public AssemblyContentType ContentType;
            public bool IsRetargetable;
        }

        private void PopulateAssemblyRefTableRows()
        {
            var assemblyRefs = this.GetAssemblyRefs();
            _assemblyRefTable.Capacity = assemblyRefs.Count;

            foreach (var assemblyRef in assemblyRefs)
            {
                AssemblyRefTableRow r = new AssemblyRefTableRow();
                var identity = assemblyRef.Identity;

                r.Version = identity.Version;
                r.PublicKeyToken = heaps.GetBlobIndex(identity.PublicKeyToken);

                Debug.Assert(!string.IsNullOrEmpty(assemblyRef.Name));
                r.Name = this.GetStringIndexForPathAndCheckLength(assemblyRef.Name, assemblyRef);

                r.Culture = heaps.GetStringIndex(identity.CultureName);

                r.IsRetargetable = identity.IsRetargetable;
                r.ContentType = identity.ContentType;
                _assemblyRefTable.Add(r);
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
                return x.Identity == y.Identity;
            }

            public int GetHashCode(IAssemblyReference reference)
            {
                return reference.Identity.GetHashCode();
            }
        }

        private readonly List<AssemblyRefTableRow> _assemblyRefTable = new List<AssemblyRefTableRow>();

        private void PopulateAssemblyTableRows()
        {
            if (!EmitAssemblyDefinition)
            {
                return;
            }

            IAssembly assembly = this.module.AsAssembly;
            _assemblyKey = heaps.GetBlobIndex(assembly.PublicKey);
            _assemblyName = this.GetStringIndexForPathAndCheckLength(assembly.Name, assembly);
            _assemblyCulture = heaps.GetStringIndex(assembly.Identity.CultureName);
        }

        private BlobIdx _assemblyKey;
        private StringIdx _assemblyName;
        private StringIdx _assemblyCulture;

        private void PopulateClassLayoutTableRows()
        {
            foreach (ITypeDefinition typeDef in this.GetTypeDefs())
            {
                if (typeDef.Alignment == 0 && typeDef.SizeOf == 0)
                {
                    continue;
                }

                uint typeDefIndex = (uint)this.GetTypeDefIndex(typeDef);
                ClassLayoutRow r = new ClassLayoutRow();
                r.PackingSize = typeDef.Alignment;
                r.ClassSize = typeDef.SizeOf;
                r.Parent = typeDefIndex;
                _classLayoutTable.Add(r);
            }
        }

        private struct ClassLayoutRow { public ushort PackingSize; public uint ClassSize; public uint Parent; }

        private readonly List<ClassLayoutRow> _classLayoutTable = new List<ClassLayoutRow>();

        private void PopulateConstantTableRows()
        {
            foreach (IFieldDefinition fieldDef in this.GetFieldDefs())
            {
                var constant = fieldDef.GetCompileTimeValue(Context);
                if (constant == null)
                {
                    continue;
                }

                uint fieldDefIndex = (uint)this.GetFieldDefIndex(fieldDef);
                _constantTable.Add(CreateConstantRow(constant.Value, parent: fieldDefIndex << 2));
            }

            int sizeWithOnlyFields = _constantTable.Count;
            foreach (IParameterDefinition parDef in this.GetParameterDefs())
            {
                var defaultValue = parDef.GetDefaultValue(Context);
                if (defaultValue == null)
                {
                    continue;
                }

                uint parameterDefIndex = (uint)this.GetParameterDefIndex(parDef);
                _constantTable.Add(CreateConstantRow(defaultValue.Value, parent: (parameterDefIndex << 2) | 1));
            }

            foreach (IPropertyDefinition propDef in this.GetPropertyDefs())
            {
                if (!propDef.HasDefaultValue)
                {
                    continue;
                }

                uint propertyDefIndex = (uint)this.GetPropertyDefIndex(propDef);
                _constantTable.Add(CreateConstantRow(propDef.DefaultValue.Value, parent: (propertyDefIndex << 2) | 2));
            }

            if (sizeWithOnlyFields > 0 && sizeWithOnlyFields < _constantTable.Count)
            {
                _constantTable.Sort(new ConstantRowComparer());
            }
        }

        private class ConstantRowComparer : Comparer<ConstantRow>
        {
            public override int Compare(ConstantRow x, ConstantRow y)
            {
                return ((int)x.Parent) - (int)y.Parent;
            }
        }

        private struct ConstantRow { public byte Type; public uint Parent; public BlobIdx Value; }

        private ConstantRow CreateConstantRow(object value, uint parent)
        {
            return new ConstantRow
            {
                Type = (byte)GetConstantTypeCode(value),
                Parent = parent,
                Value = heaps.GetConstantBlobIndex(value)
            };
        }

        private readonly List<ConstantRow> _constantTable = new List<ConstantRow>();

        private void PopulateCustomAttributeTableRows()
        {
            if (this.IsFullMetadata)
            {
                this.AddAssemblyAttributesToTable();
            }

            this.AddCustomAttributesToTable(this.GetMethodDefs(), 0, this.GetMethodDefIndex);
            this.AddCustomAttributesToTable(this.GetFieldDefs(), 1, this.GetFieldDefIndex);

            // this.AddCustomAttributesToTable(this.typeRefList, 2);
            this.AddCustomAttributesToTable(this.GetTypeDefs(), 3, this.GetTypeDefIndex);
            this.AddCustomAttributesToTable(this.GetParameterDefs(), 4, this.GetParameterDefIndex);

            // TODO: attributes on interface implementation entries 5
            // TODO: attributes on member reference entries 6
            if (this.IsFullMetadata)
            {
                this.AddModuleAttributesToTable(this.module, 7);
            }

            // TODO: declarative security entries 8
            this.AddCustomAttributesToTable(this.GetPropertyDefs(), 9, this.GetPropertyDefIndex);
            this.AddCustomAttributesToTable(this.GetEventDefs(), 10, this.GetEventDefIndex);

            // TODO: standalone signature entries 11
            if (this.IsFullMetadata)
            {
                this.AddCustomAttributesToTable(this.module.ModuleReferences, 12);
            }

            // TODO: type spec entries 13
            // this.AddCustomAttributesToTable(this.module.AssemblyReferences, 15);
            // TODO: this.AddCustomAttributesToTable(assembly.Files, 16);
            // TODO: exported types 17
            // TODO: this.AddCustomAttributesToTable(assembly.Resources, 18);

            // The indices of this.GetGenericParameters() do not correspond to the table indices because the
            // the table may be sorted after the list has been constructed.
            // Note that in all other cases, tables that are sorted are sorted in an order that depends
            // only on list indices. The generic parameter table is the sole exception.
            List<IGenericParameter> sortedGenericParameterList = new List<IGenericParameter>();
            foreach (GenericParamRow genericParamRow in _genericParamTable)
            {
                sortedGenericParameterList.Add(genericParamRow.GenericParameter);
            }

            this.AddCustomAttributesToTable(sortedGenericParameterList, 19);

            _customAttributeTable.Sort(new CustomAttributeRowComparer());
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
            uint parentToken = (1 << 5) | 14;
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
                TypeRefRow r = new TypeRefRow();
                r.ResolutionScope = this.GetResolutionScopeCodedIndex(this.module.GetCorLibrary(Context));
                r.Name = heaps.GetStringIndex(dummyAssemblyAttributeParentName + dummyAssemblyAttributeParentQualifier[iS, iM]);
                r.Namespace = heaps.GetStringIndex(dummyAssemblyAttributeParentNamespace);
                _typeRefTable.Add(r);
                _dummyAssemblyAttributeParent[iS, iM] = ((uint)_typeRefTable.Count << 5) | 2;
            }
            return _dummyAssemblyAttributeParent[iS, iM];
        }

        private void AddModuleAttributesToTable(IModule module, uint tag)
        {
            Debug.Assert(this.IsFullMetadata); // parentToken is not relative
            uint parentToken = (1 << 5) | tag;
            foreach (ICustomAttribute customAttribute in module.ModuleAttributes)
            {
                AddCustomAttributeToTable(parentToken, customAttribute);
            }
        }

        private void AddCustomAttributesToTable<T>(IEnumerable<T> parentList, uint tag)
            where T : IReference
        {
            uint parentIndex = 0;
            foreach (var parent in parentList)
            {
                parentIndex++;
                uint parentToken = (parentIndex << 5) | tag;
                foreach (ICustomAttribute customAttribute in parent.GetAttributes(Context))
                {
                    AddCustomAttributeToTable(parentToken, customAttribute);
                }
            }
        }

        private void AddCustomAttributesToTable<T>(IEnumerable<T> parentList, uint tag, Func<T, int> getDefIndex)
            where T : IReference
        {
            foreach (var parent in parentList)
            {
                uint parentIndex = (uint)getDefIndex(parent);
                uint parentToken = (parentIndex << 5) | tag;
                foreach (ICustomAttribute customAttribute in parent.GetAttributes(Context))
                {
                    AddCustomAttributeToTable(parentToken, customAttribute);
                }
            }
        }

        private void AddCustomAttributeToTable(uint parentToken, ICustomAttribute customAttribute)
        {
            CustomAttributeRow r = new CustomAttributeRow();
            r.Parent = parentToken;
            var ctor = customAttribute.Constructor(Context);
            r.Type = this.GetCustomAttributeTypeCodedIndex(ctor);
            r.Value = this.GetCustomAttributeSignatureIndex(customAttribute);
            r.OriginalPosition = _customAttributeTable.Count;
            _customAttributeTable.Add(r);
        }

        private class CustomAttributeRowComparer : Comparer<CustomAttributeRow>
        {
            public override int Compare(CustomAttributeRow x, CustomAttributeRow y)
            {
                int result = ((int)x.Parent) - (int)y.Parent;
                if (result == 0)
                {
                    result = x.OriginalPosition - y.OriginalPosition;
                }

                return result;
            }
        }

        private struct CustomAttributeRow { public uint Parent; public uint Type; public BlobIdx Value; public int OriginalPosition; }

        private readonly List<CustomAttributeRow> _customAttributeTable = new List<CustomAttributeRow>();

        private void PopulateDeclSecurityTableRows()
        {
            IAssembly assembly = this.module.AsAssembly;
            if (assembly != null)
            {
                this.PopulateDeclSecurityTableRowsFor((1 << 2) | 2, assembly.AssemblySecurityAttributes);
            }

            foreach (ITypeDefinition typeDef in this.GetTypeDefs())
            {
                if (!typeDef.HasDeclarativeSecurity)
                {
                    continue;
                }

                uint typeDefIndex = (uint)this.GetTypeDefIndex(typeDef);
                this.PopulateDeclSecurityTableRowsFor(typeDefIndex << 2, typeDef.SecurityAttributes);
            }

            foreach (IMethodDefinition methodDef in this.GetMethodDefs())
            {
                if (!methodDef.HasDeclarativeSecurity)
                {
                    continue;
                }

                uint methodDefIndex = (uint)this.GetMethodDefIndex(methodDef);
                this.PopulateDeclSecurityTableRowsFor((methodDefIndex << 2) | 1, methodDef.SecurityAttributes);
            }

            _declSecurityTable.Sort(new DeclSecurityRowComparer());
        }

        private void PopulateDeclSecurityTableRowsFor(uint parent, IEnumerable<SecurityAttribute> attributes)
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

            DeclSecurityRow r = new DeclSecurityRow();
            r.Parent = parent;

            foreach (DeclarativeSecurityAction securityAction in groupedSecurityAttributes.Keys)
            {
                r.Action = (ushort)securityAction;
                r.PermissionSet = this.GetPermissionSetIndex(groupedSecurityAttributes[securityAction]);
                r.OriginalIndex = _declSecurityTable.Count;
                _declSecurityTable.Add(r);
            }

            groupedSecurityAttributes.Free();
        }

        private class DeclSecurityRowComparer : Comparer<DeclSecurityRow>
        {
            public override int Compare(DeclSecurityRow x, DeclSecurityRow y)
            {
                int result = (int)x.Parent - (int)y.Parent;
                if (result == 0)
                {
                    result = x.OriginalIndex - y.OriginalIndex;
                }

                return result;
            }
        }

        private struct DeclSecurityRow { public ushort Action; public uint Parent; public BlobIdx PermissionSet; public int OriginalIndex; }

        private readonly List<DeclSecurityRow> _declSecurityTable = new List<DeclSecurityRow>();

        protected struct EncLogRow { public uint Token; public EncFuncCode FuncCode; }

        private readonly List<EncLogRow> _encLogTable = new List<EncLogRow>();

        protected struct EncMapRow { public uint Token; }

        private readonly List<EncMapRow> _encMapTable = new List<EncMapRow>();

        private void PopulateEventMapTableRows()
        {
            this.PopulateEventMapTableRows(_eventMapTable);
        }

        protected struct EventMapRow { public uint Parent; public uint EventList; }

        private readonly List<EventMapRow> _eventMapTable = new List<EventMapRow>();

        private void PopulateEventTableRows()
        {
            var eventDefs = this.GetEventDefs();
            _eventTable.Capacity = eventDefs.Count;

            foreach (IEventDefinition eventDef in eventDefs)
            {
                EventRow r = new EventRow();
                r.EventFlags = GetEventFlags(eventDef);
                r.Name = this.GetStringIndexForNameAndCheckLength(eventDef.Name, eventDef);
                r.EventType = this.GetTypeDefOrRefCodedIndex(eventDef.GetType(Context), true);
                _eventTable.Add(r);
            }
        }

        private struct EventRow { public ushort EventFlags; public StringIdx Name; public uint EventType; }

        private readonly List<EventRow> _eventTable = new List<EventRow>();

        private void PopulateExportedTypeTableRows()
        {
            if (this.IsFullMetadata)
            {
                _exportedTypeTable.Capacity = this.NumberOfTypeDefsEstimate;

                foreach (ITypeReference exportedType in this.module.GetExportedTypes(Context))
                {
                    INestedTypeReference nestedRef;
                    INamespaceTypeReference namespaceTypeRef;
                    ExportedTypeRow r = new ExportedTypeRow();
                    r.TypeDefId = (uint)MetadataTokens.GetToken(exportedType.TypeDef);
                    if ((namespaceTypeRef = exportedType.AsNamespaceTypeReference) != null)
                    {
                        r.Flags = TypeFlags.PublicAccess;
                        string mangledTypeName = GetMangledName(namespaceTypeRef);
                        r.TypeName = this.GetStringIndexForNameAndCheckLength(mangledTypeName, namespaceTypeRef);
                        r.TypeNamespace = this.GetStringIndexForNamespaceAndCheckLength(namespaceTypeRef, mangledTypeName);
                        r.Implementation = this.GetImplementationCodedIndex(namespaceTypeRef);
                        if ((r.Implementation & 1) == 1)
                        {
                            r.Flags = TypeFlags.PrivateAccess | TypeFlags.ForwarderImplementation;
                            r.TypeDefId = 0; // Must be cleared for type forwarders.
                        }
                    }
                    else if ((nestedRef = exportedType.AsNestedTypeReference) != null)
                    {
                        r.Flags = TypeFlags.NestedPublicAccess;
                        r.TypeName = this.GetStringIndexForNameAndCheckLength(GetMangledName(nestedRef), nestedRef);
                        r.TypeNamespace = default(StringIdx);

                        var containingType = nestedRef.GetContainingType(Context);
                        uint ci = (uint)this.GetExportedTypeIndex(containingType);
                        r.Implementation = (ci << 2) | 2;

                        var parentFlags = _exportedTypeTable[((int)ci) - 1].Flags;
                        if (parentFlags == TypeFlags.PrivateAccess)
                        {
                            r.Flags = TypeFlags.PrivateAccess;
                        }

                        ITypeReference topLevelType = containingType;
                        INestedTypeReference tmp;
                        while ((tmp = topLevelType.AsNestedTypeReference) != null)
                        {
                            topLevelType = tmp.GetContainingType(Context);
                        }

                        var topLevelFlags = _exportedTypeTable[(int)this.GetExportedTypeIndex(topLevelType) - 1].Flags;
                        if ((topLevelFlags & TypeFlags.ForwarderImplementation) != 0)
                        {
                            r.Flags = TypeFlags.PrivateAccess;
                            r.TypeDefId = 0; // Must be cleared for type forwarders and types they contain.
                        }
                    }
                    else
                    {
                        throw ExceptionUtilities.UnexpectedValue(exportedType);
                    }

                    _exportedTypeTable.Add(r);
                }
            }
        }

        private struct ExportedTypeRow { public TypeFlags Flags; public uint TypeDefId; public StringIdx TypeName; public StringIdx TypeNamespace; public uint Implementation; }

        private readonly List<ExportedTypeRow> _exportedTypeTable = new List<ExportedTypeRow>();

        private void PopulateFieldLayoutTableRows()
        {
            foreach (IFieldDefinition fieldDef in this.GetFieldDefs())
            {
                if (fieldDef.ContainingTypeDefinition.Layout != LayoutKind.Explicit || fieldDef.IsStatic)
                {
                    continue;
                }

                uint fieldDefIndex = (uint)this.GetFieldDefIndex(fieldDef);
                FieldLayoutRow r = new FieldLayoutRow();
                r.Offset = fieldDef.Offset;
                r.Field = fieldDefIndex;
                _fieldLayoutTable.Add(r);
            }
        }

        private struct FieldLayoutRow { public uint Offset; public uint Field; }

        private readonly List<FieldLayoutRow> _fieldLayoutTable = new List<FieldLayoutRow>();

        private void PopulateFieldMarshalTableRows()
        {
            foreach (IFieldDefinition fieldDef in this.GetFieldDefs())
            {
                if (!fieldDef.IsMarshalledExplicitly)
                {
                    continue;
                }

                FieldMarshalRow r = new FieldMarshalRow();

                var marshallingInformation = fieldDef.MarshallingInformation;

                r.NativeType = (marshallingInformation != null)
                    ? this.GetMarshallingDescriptorIndex(marshallingInformation)
                    : this.GetMarshallingDescriptorIndex(fieldDef.MarshallingDescriptor);

                uint fieldDefIndex = (uint)this.GetFieldDefIndex(fieldDef);
                r.Parent = fieldDefIndex << 1;
                _fieldMarshalTable.Add(r);
            }

            int sizeWithOnlyFields = _fieldMarshalTable.Count;
            foreach (IParameterDefinition parDef in this.GetParameterDefs())
            {
                if (!parDef.IsMarshalledExplicitly)
                {
                    continue;
                }

                FieldMarshalRow r = new FieldMarshalRow();

                var marshallingInformation = parDef.MarshallingInformation;

                r.NativeType = (marshallingInformation != null)
                    ? this.GetMarshallingDescriptorIndex(marshallingInformation)
                    : this.GetMarshallingDescriptorIndex(parDef.MarshallingDescriptor);

                uint parameterDefIndex = (uint)this.GetParameterDefIndex(parDef);
                r.Parent = (parameterDefIndex << 1) | 1;
                _fieldMarshalTable.Add(r);
            }

            if (sizeWithOnlyFields > 0 && sizeWithOnlyFields < _fieldMarshalTable.Count)
            {
                _fieldMarshalTable.Sort(new FieldMarshalRowComparer());
            }
        }

        private class FieldMarshalRowComparer : Comparer<FieldMarshalRow>
        {
            public override int Compare(FieldMarshalRow x, FieldMarshalRow y)
            {
                return ((int)x.Parent) - (int)y.Parent;
            }
        }

        private struct FieldMarshalRow { public uint Parent; public BlobIdx NativeType; }

        private readonly List<FieldMarshalRow> _fieldMarshalTable = new List<FieldMarshalRow>();

        private void PopulateFieldRvaTableRows(BlobBuilder mappedFieldDataWriter)
        {
            foreach (IFieldDefinition fieldDef in this.GetFieldDefs())
            {
                if (fieldDef.MappedData.IsDefault)
                {
                    continue;
                }

                uint fieldIndex = (uint)this.GetFieldDefIndex(fieldDef);
                FieldRvaRow r = new FieldRvaRow();

                r.Offset = (uint)mappedFieldDataWriter.Position;
                mappedFieldDataWriter.WriteBytes(fieldDef.MappedData);
                mappedFieldDataWriter.Align(MappedFieldDataAlignment);

                r.Field = fieldIndex;
                _fieldRvaTable.Add(r);
            }
        }

        private struct FieldRvaRow { public uint Offset; public uint Field; }

        private readonly List<FieldRvaRow> _fieldRvaTable = new List<FieldRvaRow>();

        private void PopulateFieldTableRows()
        {
            var fieldDefs = this.GetFieldDefs();
            _fieldDefTable.Capacity = fieldDefs.Count;

            foreach (IFieldDefinition fieldDef in fieldDefs)
            {
                FieldDefRow r = new FieldDefRow();
                r.Flags = GetFieldFlags(fieldDef);

                if (fieldDef.IsContextualNamedEntity)
                {
                    ((IContextualNamedEntity)fieldDef).AssociateWithMetadataWriter(this);
                }

                r.Name = this.GetStringIndexForNameAndCheckLength(fieldDef.Name, fieldDef);
                r.Signature = this.GetFieldSignatureIndex(fieldDef);
                _fieldDefTable.Add(r);
            }
        }

        private struct FieldDefRow { public ushort Flags; public StringIdx Name; public BlobIdx Signature; }

        private readonly List<FieldDefRow> _fieldDefTable = new List<FieldDefRow>();

        private void PopulateFileTableRows()
        {
            IAssembly assembly = this.module.AsAssembly;
            if (assembly == null)
            {
                return;
            }

            var hashAlgorithm = assembly.HashAlgorithm;
            _fileTable.Capacity = _fileRefList.Count;

            foreach (IFileReference fileReference in _fileRefList)
            {
                FileTableRow r = new FileTableRow();
                r.Flags = fileReference.HasMetadata ? 0u : 1u;
                r.FileName = this.GetStringIndexForPathAndCheckLength(fileReference.FileName);
                r.HashValue = heaps.GetBlobIndex(fileReference.GetHashValue(hashAlgorithm));
                _fileTable.Add(r);
            }
        }

        private struct FileTableRow { public uint Flags; public StringIdx FileName; public BlobIdx HashValue; }

        private readonly List<FileTableRow> _fileTable = new List<FileTableRow>();

        private void PopulateGenericParamConstraintTableRows()
        {
            uint genericParamIndex = 0;
            foreach (GenericParamRow genericParameterRow in _genericParamTable)
            {
                genericParamIndex++;
                GenericParamConstraintRow r = new GenericParamConstraintRow();
                r.Owner = genericParamIndex;
                foreach (ITypeReference constraint in genericParameterRow.GenericParameter.GetConstraints(Context))
                {
                    r.Constraint = this.GetTypeDefOrRefCodedIndex(constraint, true);
                    _genericParamConstraintTable.Add(r);
                }
            }
        }

        private struct GenericParamConstraintRow { public uint Owner; public uint Constraint; }

        private readonly List<GenericParamConstraintRow> _genericParamConstraintTable = new List<GenericParamConstraintRow>();

        private void PopulateGenericParamTableRows()
        {
            var genericParameters = this.GetGenericParameters();
            _genericParamTable.Capacity = genericParameters.Count;

            foreach (IGenericParameter genPar in genericParameters)
            {
                GenericParamRow r = new GenericParamRow();
                r.Number = genPar.Index;
                r.Flags = GetGenericParamFlags(genPar);
                r.Owner = this.GetTypeOrMethodDefCodedIndex(genPar);

                // CONSIDER: The CLI spec doesn't mention a restriction on the Name column of the GenericParam table,
                // but they go in the same string heap as all the other declaration names, so it stands to reason that
                // they should be restricted in the same way.
                r.Name = this.GetStringIndexForNameAndCheckLength(genPar.Name, genPar);

                r.GenericParameter = genPar;
                _genericParamTable.Add(r);
            }

            _genericParamTable.Sort(new GenericParamRowComparer());
        }

        private class GenericParamRowComparer : Comparer<GenericParamRow>
        {
            public override int Compare(GenericParamRow x, GenericParamRow y)
            {
                int result = ((int)x.Owner) - (int)y.Owner;
                if (result != 0)
                {
                    return result;
                }

                return x.Number - y.Number;
            }
        }

        private struct GenericParamRow { public ushort Number; public ushort Flags; public uint Owner; public StringIdx Name; public IGenericParameter GenericParameter; }

        private readonly List<GenericParamRow> _genericParamTable = new List<GenericParamRow>();

        private void PopulateImplMapTableRows()
        {
            foreach (IMethodDefinition methodDef in this.GetMethodDefs())
            {
                if (!methodDef.IsPlatformInvoke)
                {
                    continue;
                }

                var data = methodDef.PlatformInvokeData;
                uint methodDefIndex = (uint)this.GetMethodDefIndex(methodDef);
                var r = new ImplMapRow();
                r.MappingFlags = (ushort)data.Flags;
                r.MemberForwarded = (methodDefIndex << 1) | 1;

                string entryPointName = data.EntryPointName;
                r.ImportName = entryPointName != null
                    ? this.GetStringIndexForNameAndCheckLength(entryPointName, methodDef)
                    : heaps.GetStringIndex(methodDef.Name); // Length checked while populating the method def table.

                r.ImportScope = (uint)this.GetModuleRefIndex(data.ModuleName);
                _implMapTable.Add(r);
            }
        }

        private struct ImplMapRow { public ushort MappingFlags; public uint MemberForwarded; public StringIdx ImportName; public uint ImportScope; }

        private readonly List<ImplMapRow> _implMapTable = new List<ImplMapRow>();

        private void PopulateInterfaceImplTableRows()
        {
            foreach (ITypeDefinition typeDef in this.GetTypeDefs())
            {
                uint typeDefIndex = (uint)this.GetTypeDefIndex(typeDef);
                foreach (ITypeReference interfaceRef in typeDef.Interfaces(Context))
                {
                    InterfaceImplRow r = new InterfaceImplRow();
                    r.Class = typeDefIndex;
                    r.Interface = this.GetTypeDefOrRefCodedIndex(interfaceRef, true);
                    _interfaceImplTable.Add(r);
                }
            }
        }

        private struct InterfaceImplRow { public uint Class; public uint Interface; }

        private readonly List<InterfaceImplRow> _interfaceImplTable = new List<InterfaceImplRow>();

        private void PopulateManifestResourceTableRows(BlobBuilder resourceDataWriter, BlobBuilder dynamicAnalysisDataOpt)
        {
            if (dynamicAnalysisDataOpt != null)
            {
                _manifestResourceTable.Add(new ManifestResourceRow()
                {
                    Offset = GetManagedResourceOffset(dynamicAnalysisDataOpt, resourceDataWriter),
                    Flags = (uint)ManifestResourceAttributes.Private,
                    Name = heaps.GetStringIndex("<DynamicAnalysisData>"),
                });
            }
            
            foreach (var resource in this.module.GetResources(Context))
            {
                ManifestResourceRow r = new ManifestResourceRow();
                r.Offset = GetManagedResourceOffset(resource, resourceDataWriter);
                r.Flags = resource.IsPublic ? 1u : 2u;
                r.Name = this.GetStringIndexForNameAndCheckLength(resource.Name);

                if (resource.ExternalFile != null)
                {
                    IFileReference externalFile = resource.ExternalFile;
                    // Length checked on insertion into the file table.
                    r.Implementation = (uint)this.GetFileRefIndex(externalFile) << 2;
                }
                else
                {
                    // This is an embedded resource, we don't support references to resources from referenced assemblies.
                    r.Implementation = 0;
                }

                _manifestResourceTable.Add(r);
            }

            // the stream should be aligned:
            Debug.Assert((resourceDataWriter.Count % ManagedResourcesDataAlignment) == 0);
        }

        private struct ManifestResourceRow { public uint Offset; public uint Flags; public StringIdx Name; public uint Implementation; }

        private readonly List<ManifestResourceRow> _manifestResourceTable = new List<ManifestResourceRow>();

        private void PopulateMemberRefTableRows()
        {
            var memberRefs = this.GetMemberRefs();
            _memberRefTable.Capacity = memberRefs.Count;

            foreach (ITypeMemberReference memberRef in memberRefs)
            {
                MemberRefRow r = new MemberRefRow();
                r.Class = this.GetMemberRefParentCodedIndex(memberRef);
                r.Name = this.GetStringIndexForNameAndCheckLength(memberRef.Name, memberRef);
                r.Signature = this.GetMemberRefSignatureIndex(memberRef);
                _memberRefTable.Add(r);
            }
        }

        private struct MemberRefRow { public uint Class; public StringIdx Name; public BlobIdx Signature; }

        private readonly List<MemberRefRow> _memberRefTable = new List<MemberRefRow>();

        private void PopulateMethodImplTableRows()
        {
            _methodImplTable.Capacity = this.methodImplList.Count;

            foreach (MethodImplementation methodImplementation in this.methodImplList)
            {
                MethodImplRow r = new MethodImplRow();
                r.Class = (uint)this.GetTypeDefIndex(methodImplementation.ContainingType);
                r.MethodBody = this.GetMethodDefOrRefCodedIndex(methodImplementation.ImplementingMethod);
                r.MethodDecl = this.GetMethodDefOrRefCodedIndex(methodImplementation.ImplementedMethod);
                _methodImplTable.Add(r);
            }
        }

        private struct MethodImplRow { public uint Class; public uint MethodBody; public uint MethodDecl; }

        private readonly List<MethodImplRow> _methodImplTable = new List<MethodImplRow>();

        private void PopulateMethodSemanticsTableRows()
        {
            var propertyDefs = this.GetPropertyDefs();
            var eventDefs = this.GetEventDefs();

            //EDMAURER an estimate, not necessarily accurate.
            _methodSemanticsTable.Capacity = propertyDefs.Count * 2 + eventDefs.Count * 2;

            uint i = 0;
            foreach (IPropertyDefinition propertyDef in this.GetPropertyDefs())
            {
                uint propertyIndex = (uint)this.GetPropertyDefIndex(propertyDef);
                var r = new MethodSemanticsRow();
                r.Association = (propertyIndex << 1) | 1;
                foreach (IMethodReference accessorMethod in propertyDef.Accessors)
                {
                    if (accessorMethod == propertyDef.Setter)
                    {
                        r.Semantic = 0x0001;
                    }
                    else if (accessorMethod == propertyDef.Getter)
                    {
                        r.Semantic = 0x0002;
                    }
                    else
                    {
                        r.Semantic = 0x0004;
                    }

                    r.Method = (uint)this.GetMethodDefIndex(accessorMethod.GetResolvedMethod(Context));
                    r.OriginalIndex = i++;
                    _methodSemanticsTable.Add(r);
                }
            }

            int propertiesOnlyTableCount = _methodSemanticsTable.Count;
            foreach (IEventDefinition eventDef in this.GetEventDefs())
            {
                uint eventIndex = (uint)this.GetEventDefIndex(eventDef);
                var r = new MethodSemanticsRow();
                r.Association = eventIndex << 1;
                foreach (IMethodReference accessorMethod in eventDef.Accessors)
                {
                    r.Semantic = 0x0004;
                    if (accessorMethod == eventDef.Adder)
                    {
                        r.Semantic = 0x0008;
                    }
                    else if (accessorMethod == eventDef.Remover)
                    {
                        r.Semantic = 0x0010;
                    }
                    else if (accessorMethod == eventDef.Caller)
                    {
                        r.Semantic = 0x0020;
                    }

                    r.Method = (uint)this.GetMethodDefIndex(accessorMethod.GetResolvedMethod(Context));
                    r.OriginalIndex = i++;
                    _methodSemanticsTable.Add(r);
                }
            }

            if (_methodSemanticsTable.Count > propertiesOnlyTableCount)
            {
                _methodSemanticsTable.Sort(new MethodSemanticsRowComparer());
            }
        }

        private class MethodSemanticsRowComparer : Comparer<MethodSemanticsRow>
        {
            public override int Compare(MethodSemanticsRow x, MethodSemanticsRow y)
            {
                int result = ((int)x.Association) - (int)y.Association;
                if (result == 0)
                {
                    result = ((int)x.OriginalIndex) - (int)y.OriginalIndex;
                }

                return result;
            }
        }

        private struct MethodSemanticsRow { public ushort Semantic; public uint Method; public uint Association; public uint OriginalIndex; }

        private readonly List<MethodSemanticsRow> _methodSemanticsTable = new List<MethodSemanticsRow>();

        private void PopulateMethodSpecTableRows()
        {
            var methodSpecs = this.GetMethodSpecs();
            _methodSpecTable.Capacity = methodSpecs.Count;

            foreach (IGenericMethodInstanceReference genericMethodInstanceReference in methodSpecs)
            {
                MethodSpecRow r = new MethodSpecRow();
                r.Method = this.GetMethodDefOrRefCodedIndex(genericMethodInstanceReference.GetGenericMethod(Context));
                r.Instantiation = this.GetGenericMethodInstanceIndex(genericMethodInstanceReference);
                _methodSpecTable.Add(r);
            }
        }

        private struct MethodSpecRow { public uint Method; public BlobIdx Instantiation; }

        private readonly List<MethodSpecRow> _methodSpecTable = new List<MethodSpecRow>();

        private void PopulateMethodTableRows(int[] methodBodyRvas)
        {
            var methodDefs = this.GetMethodDefs();
            _methodTable = new MethodRow[methodDefs.Count];

            int i = 0;
            foreach (IMethodDefinition methodDef in methodDefs)
            {
                _methodTable[i] = new MethodRow
                {
                    Rva = methodBodyRvas[i],
                    ImplFlags = (ushort)methodDef.GetImplementationAttributes(Context),
                    Flags = GetMethodFlags(methodDef),
                    Name = this.GetStringIndexForNameAndCheckLength(methodDef.Name, methodDef),
                    Signature = this.GetMethodSignatureIndex(methodDef),
                    ParamList = (uint)this.GetParameterDefIndex(methodDef),
                };

                i++;
            }
        }

        private struct MethodRow { public int Rva; public ushort ImplFlags; public ushort Flags; public StringIdx Name; public BlobIdx Signature; public uint ParamList; }

        private MethodRow[] _methodTable;

        private void PopulateModuleRefTableRows()
        {
            var moduleRefs = this.GetModuleRefs();
            _moduleRefTable.Capacity = moduleRefs.Count;

            foreach (string moduleName in moduleRefs)
            {
                ModuleRefRow r = new ModuleRefRow();
                r.Name = this.GetStringIndexForPathAndCheckLength(moduleName);
                _moduleRefTable.Add(r);
            }
        }

        private struct ModuleRefRow { public StringIdx Name; }

        private readonly List<ModuleRefRow> _moduleRefTable = new List<ModuleRefRow>();

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

            _moduleRow = MakeModuleRow(heaps, mvid);
        }

        private ModuleRow MakeModuleRow(MetadataHeapsBuilder heaps, Guid mvid)
        {
            return new ModuleRow
            {
                Generation = this.Generation,
                Name = heaps.GetStringIndex(this.module.ModuleName),
                ModuleVersionId = heaps.AllocateGuid(mvid),
                EncId = heaps.GetGuidIndex(this.EncId),
                EncBaseId = heaps.GetGuidIndex(this.EncBaseId),
            };
        }

        private struct ModuleRow { public ushort Generation; public StringIdx Name; public int ModuleVersionId; public int EncId; public int EncBaseId; }

        private ModuleRow _moduleRow;

        private void PopulateNestedClassTableRows()
        {
            foreach (ITypeDefinition typeDef in this.GetTypeDefs())
            {
                INestedTypeDefinition nestedTypeDef = typeDef.AsNestedTypeDefinition(Context);
                if (nestedTypeDef == null)
                {
                    continue;
                }

                NestedClassRow r = new NestedClassRow();
                uint typeDefIndex = (uint)this.GetTypeDefIndex(typeDef);
                r.NestedClass = typeDefIndex;
                r.EnclosingClass = (uint)this.GetTypeDefIndex(nestedTypeDef.ContainingTypeDefinition);
                _nestedClassTable.Add(r);
            }
        }

        private struct NestedClassRow { public uint NestedClass; public uint EnclosingClass; }

        private readonly List<NestedClassRow> _nestedClassTable = new List<NestedClassRow>();

        private void PopulateParamTableRows()
        {
            var parameterDefs = this.GetParameterDefs();
            _paramTable.Capacity = parameterDefs.Count;

            foreach (IParameterDefinition parDef in parameterDefs)
            {
                ParamRow r = new ParamRow();
                r.Flags = GetParameterFlags(parDef);
                r.Sequence = (ushort)(parDef is ReturnValueParameter ? 0 : parDef.Index + 1);
                r.Name = this.GetStringIndexForNameAndCheckLength(parDef.Name, parDef);
                _paramTable.Add(r);
            }
        }

        private struct ParamRow { public ushort Flags; public ushort Sequence; public StringIdx Name; }

        private readonly List<ParamRow> _paramTable = new List<ParamRow>();

        private void PopulatePropertyMapTableRows()
        {
            this.PopulatePropertyMapTableRows(_propertyMapTable);
        }

        protected struct PropertyMapRow { public uint Parent; public uint PropertyList; }

        private readonly List<PropertyMapRow> _propertyMapTable = new List<PropertyMapRow>();

        private void PopulatePropertyTableRows()
        {
            var propertyDefs = this.GetPropertyDefs();
            _propertyTable.Capacity = propertyDefs.Count;

            foreach (IPropertyDefinition propertyDef in propertyDefs)
            {
                var r = new PropertyRow();
                r.PropFlags = GetPropertyFlags(propertyDef);
                r.Name = this.GetStringIndexForNameAndCheckLength(propertyDef.Name, propertyDef);
                r.Type = this.GetPropertySignatureIndex(propertyDef);
                _propertyTable.Add(r);
            }
        }

        [StructLayout(LayoutKind.Auto)]
        private struct PropertyRow { public ushort PropFlags; public StringIdx Name; public BlobIdx Type; }

        private readonly List<PropertyRow> _propertyTable = new List<PropertyRow>();

        private void PopulateTypeDefTableRows()
        {
            var typeDefs = this.GetTypeDefs();
            _typeDefTable.Capacity = typeDefs.Count;

            foreach (INamedTypeDefinition typeDef in typeDefs)
            {
                var r = new TypeDefRow();
                INamespaceTypeDefinition namespaceType = typeDef.AsNamespaceTypeDefinition(Context);
                r.Flags = GetTypeDefFlags(typeDef);
                string mangledTypeName = GetMangledName(typeDef);
                r.Name = this.GetStringIndexForNameAndCheckLength(mangledTypeName, typeDef);
                r.Namespace = namespaceType == null
                    ? default(StringIdx)
                    : this.GetStringIndexForNamespaceAndCheckLength(namespaceType, mangledTypeName);
                ITypeReference baseType = typeDef.GetBaseClass(Context);
                r.Extends = (baseType != null) ? this.GetTypeDefOrRefCodedIndex(baseType, true) : 0;

                r.FieldList = (uint)this.GetFieldDefIndex(typeDef);
                r.MethodList = (uint)this.GetMethodDefIndex(typeDef);

                _typeDefTable.Add(r);
            }
        }

        private struct TypeDefRow { public uint Flags; public StringIdx Name; public StringIdx Namespace; public uint Extends; public uint FieldList; public uint MethodList; }

        private readonly List<TypeDefRow> _typeDefTable = new List<TypeDefRow>();

        private void PopulateTypeRefTableRows()
        {
            var typeRefs = this.GetTypeRefs();
            _typeRefTable.Capacity = typeRefs.Count;

            foreach (ITypeReference typeRef in typeRefs)
            {
                TypeRefRow r = new TypeRefRow();
                INestedTypeReference nestedTypeRef = typeRef.AsNestedTypeReference;
                if (nestedTypeRef != null)
                {
                    ISpecializedNestedTypeReference sneTypeRef = nestedTypeRef.AsSpecializedNestedTypeReference;
                    if (sneTypeRef != null)
                    {
                        r.ResolutionScope = this.GetResolutionScopeCodedIndex(sneTypeRef.UnspecializedVersion.GetContainingType(Context));
                    }
                    else
                    {
                        r.ResolutionScope = this.GetResolutionScopeCodedIndex(nestedTypeRef.GetContainingType(Context));
                    }

                    r.Name = this.GetStringIndexForNameAndCheckLength(GetMangledName(nestedTypeRef), nestedTypeRef);
                    r.Namespace = default(StringIdx);
                }
                else
                {
                    INamespaceTypeReference namespaceTypeRef = typeRef.AsNamespaceTypeReference;
                    if (namespaceTypeRef == null)
                    {
                        throw ExceptionUtilities.UnexpectedValue(typeRef);
                    }

                    r.ResolutionScope = this.GetResolutionScopeCodedIndex(namespaceTypeRef.GetUnit(Context));
                    string mangledTypeName = GetMangledName(namespaceTypeRef);
                    r.Name = this.GetStringIndexForNameAndCheckLength(mangledTypeName, namespaceTypeRef);
                    r.Namespace = this.GetStringIndexForNamespaceAndCheckLength(namespaceTypeRef, mangledTypeName);
                }

                _typeRefTable.Add(r);
            }
        }

        private struct TypeRefRow { public uint ResolutionScope; public StringIdx Name; public StringIdx Namespace; }

        private readonly List<TypeRefRow> _typeRefTable = new List<TypeRefRow>();

        private void PopulateTypeSpecTableRows()
        {
            var typeSpecs = this.GetTypeSpecs();
            _typeSpecTable.Capacity = typeSpecs.Count;

            foreach (ITypeReference typeSpec in typeSpecs)
            {
                TypeSpecRow r = new TypeSpecRow();
                r.Signature = this.GetTypeSpecSignatureIndex(typeSpec);
                _typeSpecTable.Add(r);
            }
        }

        private struct TypeSpecRow { public BlobIdx Signature; }

        private readonly List<TypeSpecRow> _typeSpecTable = new List<TypeSpecRow>();

        private void SerializeTablesHeader(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            int startPosition = writer.Position;

            HeapSizeFlag heapSizes = 0;
            if (metadataSizes.StringIndexSize > 2)
            {
                heapSizes |= HeapSizeFlag.StringHeapLarge;
            }

            if (metadataSizes.GuidIndexSize > 2)
            {
                heapSizes |= HeapSizeFlag.GuidHeapLarge;
            }

            if (metadataSizes.BlobIndexSize > 2)
            {
                heapSizes |= HeapSizeFlag.BlobHeapLarge;
            }

            if (!this.IsFullMetadata)
            {
                heapSizes |= (HeapSizeFlag.EnCDeltas | HeapSizeFlag.DeletedMarks);
            }

            ulong sortedDebugTables = metadataSizes.PresentTablesMask & MetadataSizes.SortedDebugTables;

            // Consider filtering out type system tables that are not present:
            ulong sortedTables = sortedDebugTables | (metadataSizes.IsStandaloneDebugMetadata ? 0UL : 0x16003301fa00);

            writer.WriteUInt32(0); // reserved
            writer.WriteByte(module.Properties.MetadataFormatMajorVersion);
            writer.WriteByte(module.Properties.MetadataFormatMinorVersion);
            writer.WriteByte((byte)heapSizes);
            writer.WriteByte(1); // reserved
            writer.WriteUInt64(metadataSizes.PresentTablesMask);
            writer.WriteUInt64(sortedTables);
            SerializeRowCounts(writer, metadataSizes.RowCounts, metadataSizes.PresentTablesMask);

            int endPosition = writer.Position;
            Debug.Assert(metadataSizes.CalculateTableStreamHeaderSize() == endPosition - startPosition);
        }

        private static void SerializeStandalonePdbStream(BlobBuilder writer, MetadataSizes metadataSizes, int entryPointToken, out int pdbIdOffset)
        {
            int startPosition = writer.Position;

            // zero out and save position, will be filled in later
            pdbIdOffset = startPosition;
            writer.WriteBytes(0, MetadataSizes.PdbIdSize);

            writer.WriteUInt32((uint)entryPointToken);

            writer.WriteUInt64(metadataSizes.ExternalTablesMask);
            SerializeRowCounts(writer, metadataSizes.RowCounts, metadataSizes.ExternalTablesMask);

            int endPosition = writer.Position;
            Debug.Assert(metadataSizes.CalculateStandalonePdbStreamSize() == endPosition - startPosition);
        }

        private static void SerializeRowCounts(BlobBuilder writer, ImmutableArray<int> rowCounts, ulong includeTables)
        {
            for (int i = 0; i < rowCounts.Length; i++)
            {
                if (((1UL << i) & includeTables) != 0)
                {
                    int rowCount = rowCounts[i];
                    if (rowCount > 0)
                    {
                        writer.WriteInt32(rowCount);
                    }
                }
            }
        }

        private void SerializeModuleTable(BlobBuilder writer, MetadataSizes metadataSizes, MetadataHeapsBuilder heaps)
        {
            writer.WriteUInt16(_moduleRow.Generation);
            writer.WriteReference((uint)heaps.ResolveStringIndex(_moduleRow.Name), metadataSizes.StringIndexSize);
            writer.WriteReference((uint)_moduleRow.ModuleVersionId, metadataSizes.GuidIndexSize);
            writer.WriteReference((uint)_moduleRow.EncId, metadataSizes.GuidIndexSize);
            writer.WriteReference((uint)_moduleRow.EncBaseId, metadataSizes.GuidIndexSize);
        }

        private void SerializeEncLogTable(BlobBuilder writer)
        {
            foreach (EncLogRow encLog in _encLogTable)
            {
                writer.WriteUInt32(encLog.Token);
                writer.WriteUInt32((uint)encLog.FuncCode);
            }
        }

        private void SerializeEncMapTable(BlobBuilder writer)
        {
            foreach (EncMapRow encMap in _encMapTable)
            {
                writer.WriteUInt32(encMap.Token);
            }
        }

        private void SerializeTypeRefTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (TypeRefRow typeRef in _typeRefTable)
            {
                writer.WriteReference(typeRef.ResolutionScope, metadataSizes.ResolutionScopeCodedIndexSize);
                writer.WriteReference((uint)heaps.ResolveStringIndex(typeRef.Name), metadataSizes.StringIndexSize);
                writer.WriteReference((uint)heaps.ResolveStringIndex(typeRef.Namespace), metadataSizes.StringIndexSize);
            }
        }

        private void SerializeTypeDefTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (TypeDefRow typeDef in _typeDefTable)
            {
                writer.WriteUInt32(typeDef.Flags);
                writer.WriteReference((uint)heaps.ResolveStringIndex(typeDef.Name), metadataSizes.StringIndexSize);
                writer.WriteReference((uint)heaps.ResolveStringIndex(typeDef.Namespace), metadataSizes.StringIndexSize);
                writer.WriteReference(typeDef.Extends, metadataSizes.TypeDefOrRefCodedIndexSize);
                writer.WriteReference(typeDef.FieldList, metadataSizes.FieldDefIndexSize);
                writer.WriteReference(typeDef.MethodList, metadataSizes.MethodDefIndexSize);
            }
        }

        private void SerializeFieldTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (FieldDefRow fieldDef in _fieldDefTable)
            {
                writer.WriteUInt16(fieldDef.Flags);
                writer.WriteReference((uint)heaps.ResolveStringIndex(fieldDef.Name), metadataSizes.StringIndexSize);
                writer.WriteReference((uint)heaps.ResolveBlobIndex(fieldDef.Signature), metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeMethodDefTable(BlobBuilder writer, MetadataSizes metadataSizes, int methodBodyStreamRva)
        {
            foreach (MethodRow method in _methodTable)
            {
                if (method.Rva == -1)
                {
                    writer.WriteUInt32(0);
                }
                else
                {
                    writer.WriteUInt32((uint)(methodBodyStreamRva + method.Rva));
                }

                writer.WriteUInt16(method.ImplFlags);
                writer.WriteUInt16(method.Flags);
                writer.WriteReference((uint)heaps.ResolveStringIndex(method.Name), metadataSizes.StringIndexSize);
                writer.WriteReference((uint)heaps.ResolveBlobIndex(method.Signature), metadataSizes.BlobIndexSize);
                writer.WriteReference(method.ParamList, metadataSizes.ParameterIndexSize);
            }
        }

        private void SerializeParamTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (ParamRow param in _paramTable)
            {
                writer.WriteUInt16(param.Flags);
                writer.WriteUInt16(param.Sequence);
                writer.WriteReference((uint)heaps.ResolveStringIndex(param.Name), metadataSizes.StringIndexSize);
            }
        }

        private void SerializeInterfaceImplTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (InterfaceImplRow interfaceImpl in _interfaceImplTable)
            {
                writer.WriteReference(interfaceImpl.Class, metadataSizes.TypeDefIndexSize);
                writer.WriteReference(interfaceImpl.Interface, metadataSizes.TypeDefOrRefCodedIndexSize);
            }
        }

        private void SerializeMemberRefTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (MemberRefRow memberRef in _memberRefTable)
            {
                writer.WriteReference(memberRef.Class, metadataSizes.MemberRefParentCodedIndexSize);
                writer.WriteReference((uint)heaps.ResolveStringIndex(memberRef.Name), metadataSizes.StringIndexSize);
                writer.WriteReference((uint)heaps.ResolveBlobIndex(memberRef.Signature), metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeConstantTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (ConstantRow constant in _constantTable)
            {
                writer.WriteByte(constant.Type);
                writer.WriteByte(0);
                writer.WriteReference(constant.Parent, metadataSizes.HasConstantCodedIndexSize);
                writer.WriteReference((uint)heaps.ResolveBlobIndex(constant.Value), metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeCustomAttributeTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (CustomAttributeRow customAttribute in _customAttributeTable)
            {
                writer.WriteReference(customAttribute.Parent, metadataSizes.HasCustomAttributeCodedIndexSize);
                writer.WriteReference(customAttribute.Type, metadataSizes.CustomAttributeTypeCodedIndexSize);
                writer.WriteReference((uint)heaps.ResolveBlobIndex(customAttribute.Value), metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeFieldMarshalTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (FieldMarshalRow fieldMarshal in _fieldMarshalTable)
            {
                writer.WriteReference(fieldMarshal.Parent, metadataSizes.HasFieldMarshalCodedIndexSize);
                writer.WriteReference((uint)heaps.ResolveBlobIndex(fieldMarshal.NativeType), metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeDeclSecurityTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (DeclSecurityRow declSecurity in _declSecurityTable)
            {
                writer.WriteUInt16(declSecurity.Action);
                writer.WriteReference(declSecurity.Parent, metadataSizes.DeclSecurityCodedIndexSize);
                writer.WriteReference((uint)heaps.ResolveBlobIndex(declSecurity.PermissionSet), metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeClassLayoutTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (ClassLayoutRow classLayout in _classLayoutTable)
            {
                writer.WriteUInt16(classLayout.PackingSize);
                writer.WriteUInt32(classLayout.ClassSize);
                writer.WriteReference(classLayout.Parent, metadataSizes.TypeDefIndexSize);
            }
        }

        private void SerializeFieldLayoutTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (FieldLayoutRow fieldLayout in _fieldLayoutTable)
            {
                writer.WriteUInt32(fieldLayout.Offset);
                writer.WriteReference(fieldLayout.Field, metadataSizes.FieldDefIndexSize);
            }
        }

        private void SerializeStandAloneSigTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (BlobIdx blobIndex in this.GetStandAloneSignatures())
            {
                writer.WriteReference((uint)heaps.ResolveBlobIndex(blobIndex), metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeEventMapTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (EventMapRow eventMap in _eventMapTable)
            {
                writer.WriteReference(eventMap.Parent, metadataSizes.TypeDefIndexSize);
                writer.WriteReference(eventMap.EventList, metadataSizes.EventDefIndexSize);
            }
        }

        private void SerializeEventTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (EventRow eventRow in _eventTable)
            {
                writer.WriteUInt16(eventRow.EventFlags);
                writer.WriteReference((uint)heaps.ResolveStringIndex(eventRow.Name), metadataSizes.StringIndexSize);
                writer.WriteReference(eventRow.EventType, metadataSizes.TypeDefOrRefCodedIndexSize);
            }
        }

        private void SerializePropertyMapTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (PropertyMapRow propertyMap in _propertyMapTable)
            {
                writer.WriteReference(propertyMap.Parent, metadataSizes.TypeDefIndexSize);
                writer.WriteReference(propertyMap.PropertyList, metadataSizes.PropertyDefIndexSize);
            }
        }

        private void SerializePropertyTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (PropertyRow property in _propertyTable)
            {
                writer.WriteUInt16(property.PropFlags);
                writer.WriteReference((uint)heaps.ResolveStringIndex(property.Name), metadataSizes.StringIndexSize);
                writer.WriteReference((uint)heaps.ResolveBlobIndex(property.Type), metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeMethodSemanticsTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (MethodSemanticsRow methodSemantic in _methodSemanticsTable)
            {
                writer.WriteUInt16(methodSemantic.Semantic);
                writer.WriteReference(methodSemantic.Method, metadataSizes.MethodDefIndexSize);
                writer.WriteReference(methodSemantic.Association, metadataSizes.HasSemanticsCodedIndexSize);
            }
        }

        private void SerializeMethodImplTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (MethodImplRow methodImpl in _methodImplTable)
            {
                writer.WriteReference(methodImpl.Class, metadataSizes.TypeDefIndexSize);
                writer.WriteReference(methodImpl.MethodBody, metadataSizes.MethodDefOrRefCodedIndexSize);
                writer.WriteReference(methodImpl.MethodDecl, metadataSizes.MethodDefOrRefCodedIndexSize);
            }
        }

        private void SerializeModuleRefTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (ModuleRefRow moduleRef in _moduleRefTable)
            {
                writer.WriteReference((uint)heaps.ResolveStringIndex(moduleRef.Name), metadataSizes.StringIndexSize);
            }
        }

        private void SerializeTypeSpecTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (TypeSpecRow typeSpec in _typeSpecTable)
            {
                writer.WriteReference((uint)heaps.ResolveBlobIndex(typeSpec.Signature), metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeImplMapTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (ImplMapRow implMap in _implMapTable)
            {
                writer.WriteUInt16(implMap.MappingFlags);
                writer.WriteReference(implMap.MemberForwarded, metadataSizes.MemberForwardedCodedIndexSize);
                writer.WriteReference((uint)heaps.ResolveStringIndex(implMap.ImportName), metadataSizes.StringIndexSize);
                writer.WriteReference(implMap.ImportScope, metadataSizes.ModuleRefIndexSize);
            }
        }

        private void SerializeFieldRvaTable(BlobBuilder writer, MetadataSizes metadataSizes, int mappedFieldDataStreamRva)
        {
            foreach (FieldRvaRow fieldRva in _fieldRvaTable)
            {
                writer.WriteUInt32((uint)mappedFieldDataStreamRva + fieldRva.Offset);
                writer.WriteReference(fieldRva.Field, metadataSizes.FieldDefIndexSize);
            }
        }

        private void SerializeAssemblyTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            if (!EmitAssemblyDefinition)
            {
                return;
            }

            IAssembly assembly = this.module.AsAssembly;
            var identity = assembly.Identity;

            writer.WriteUInt32((uint)assembly.HashAlgorithm);
            writer.WriteUInt16((ushort)identity.Version.Major);
            writer.WriteUInt16((ushort)identity.Version.Minor);
            writer.WriteUInt16((ushort)identity.Version.Build);
            writer.WriteUInt16((ushort)identity.Version.Revision);
            writer.WriteUInt32(assembly.Flags);
            writer.WriteReference((uint)heaps.ResolveBlobIndex(_assemblyKey), metadataSizes.BlobIndexSize);

            writer.WriteReference((uint)heaps.ResolveStringIndex(_assemblyName), metadataSizes.StringIndexSize);
            writer.WriteReference((uint)heaps.ResolveStringIndex(_assemblyCulture), metadataSizes.StringIndexSize);
        }

        private void SerializeAssemblyRefTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (AssemblyRefTableRow assemblyRef in _assemblyRefTable)
            {
                writer.WriteUInt16((ushort)assemblyRef.Version.Major);
                writer.WriteUInt16((ushort)assemblyRef.Version.Minor);
                writer.WriteUInt16((ushort)assemblyRef.Version.Build);
                writer.WriteUInt16((ushort)assemblyRef.Version.Revision);

                // flags: reference has token, not full public key
                uint flags = 0;
                if (assemblyRef.IsRetargetable)
                {
                    flags |= (uint)AssemblyFlags.Retargetable;
                }

                flags |= (uint)assemblyRef.ContentType << 9;

                writer.WriteUInt32(flags);

                writer.WriteReference((uint)heaps.ResolveBlobIndex(assemblyRef.PublicKeyToken), metadataSizes.BlobIndexSize);
                writer.WriteReference((uint)heaps.ResolveStringIndex(assemblyRef.Name), metadataSizes.StringIndexSize);
                writer.WriteReference((uint)heaps.ResolveStringIndex(assemblyRef.Culture), metadataSizes.StringIndexSize);
                writer.WriteReference(0, metadataSizes.BlobIndexSize); // hash of referenced assembly. Omitted.
            }
        }

        private void SerializeFileTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (FileTableRow fileReference in _fileTable)
            {
                writer.WriteUInt32(fileReference.Flags);
                writer.WriteReference((uint)heaps.ResolveStringIndex(fileReference.FileName), metadataSizes.StringIndexSize);
                writer.WriteReference((uint)heaps.ResolveBlobIndex(fileReference.HashValue), metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeExportedTypeTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (ExportedTypeRow exportedType in _exportedTypeTable)
            {
                writer.WriteUInt32((uint)exportedType.Flags);
                writer.WriteUInt32(exportedType.TypeDefId);
                writer.WriteReference((uint)heaps.ResolveStringIndex(exportedType.TypeName), metadataSizes.StringIndexSize);
                writer.WriteReference((uint)heaps.ResolveStringIndex(exportedType.TypeNamespace), metadataSizes.StringIndexSize);
                writer.WriteReference(exportedType.Implementation, metadataSizes.ImplementationCodedIndexSize);
            }
        }

        private void SerializeManifestResourceTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (ManifestResourceRow manifestResource in _manifestResourceTable)
            {
                writer.WriteUInt32(manifestResource.Offset);
                writer.WriteUInt32(manifestResource.Flags);
                writer.WriteReference((uint)heaps.ResolveStringIndex(manifestResource.Name), metadataSizes.StringIndexSize);
                writer.WriteReference(manifestResource.Implementation, metadataSizes.ImplementationCodedIndexSize);
            }
        }

        private void SerializeNestedClassTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (NestedClassRow nestedClass in _nestedClassTable)
            {
                writer.WriteReference(nestedClass.NestedClass, metadataSizes.TypeDefIndexSize);
                writer.WriteReference(nestedClass.EnclosingClass, metadataSizes.TypeDefIndexSize);
            }
        }

        private void SerializeGenericParamTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (GenericParamRow genericParam in _genericParamTable)
            {
                writer.WriteUInt16(genericParam.Number);
                writer.WriteUInt16(genericParam.Flags);
                writer.WriteReference(genericParam.Owner, metadataSizes.TypeOrMethodDefCodedIndexSize);
                writer.WriteReference((uint)heaps.ResolveStringIndex(genericParam.Name), metadataSizes.StringIndexSize);
            }
        }

        private void SerializeMethodSpecTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (MethodSpecRow methodSpec in _methodSpecTable)
            {
                writer.WriteReference(methodSpec.Method, metadataSizes.MethodDefOrRefCodedIndexSize);
                writer.WriteReference((uint)heaps.ResolveBlobIndex(methodSpec.Instantiation), metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeGenericParamConstraintTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (GenericParamConstraintRow genericParamConstraint in _genericParamConstraintTable)
            {
                writer.WriteReference(genericParamConstraint.Owner, metadataSizes.GenericParamIndexSize);
                writer.WriteReference(genericParamConstraint.Constraint, metadataSizes.TypeDefOrRefCodedIndexSize);
            }
        }

        private int[] SerializeMethodBodies(BlobBuilder ilWriter, PdbWriter pdbWriterOpt)
        {
            CustomDebugInfoWriter customDebugInfoWriter = (pdbWriterOpt != null) ? new CustomDebugInfoWriter(pdbWriterOpt) : null;

            var methods = this.GetMethodDefs();
            int[] rvas = new int[methods.Count];

            int methodRid = 1;
            foreach (IMethodDefinition method in methods)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                int rva;
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
                        rva = this.SerializeMethodBody(body, ilWriter, localSignatureToken);

                        pdbWriterOpt?.SerializeDebugInfo(body, localSignatureToken, customDebugInfoWriter);
                    }
                    else
                    {
                        rva = 0;
                        localSignatureRid = 0;
                    }
                }
                else
                {
                    // 0 is actually written to metadata when the row is serialized
                    rva = -1;
                    body = null;
                    localSignatureRid = 0;
                }

                if (_debugHeapsOpt != null)
                {
                    SerializeMethodDebugInfo(body, methodRid, localSignatureRid);
                }

                _dynamicAnalysisDataWriterOpt?.SerializeMethodDynamicAnalysisData(body);

                rvas[methodRid - 1] = rva;

                methodRid++;
            }

            return rvas;
        }

        private int SerializeMethodBody(IMethodBody methodBody, BlobBuilder ilWriter, uint localSignatureToken)
        {
            int ilLength = methodBody.IL.Length;
            uint numberOfExceptionHandlers = (uint)methodBody.ExceptionRegions.Length;
            bool isSmallBody = ilLength < 64 && methodBody.MaxStack <= 8 && localSignatureToken == 0 && numberOfExceptionHandlers == 0;

            // Check if an identical method body has already been serialized. 
            // If so, use the RVA of the already serialized one.
            // Note that we don't need to rewrite the fake tokens in the body before looking it up.
            int bodyRva;

            // Don't do small body method caching during deterministic builds until this issue is fixed
            // https://github.com/dotnet/roslyn/issues/7595
            if (!_deterministic && isSmallBody && _smallMethodBodies.TryGetValue(methodBody.IL, out bodyRva))
            {
                return bodyRva;
            }

            if (isSmallBody)
            {
                bodyRva = ilWriter.Position;
                ilWriter.WriteByte((byte)((ilLength << 2) | 2));

                // Don't do small body method caching during deterministic builds until this issue is fixed
                // https://github.com/dotnet/roslyn/issues/7595
                if (!_deterministic)
                {
                    _smallMethodBodies.Add(methodBody.IL, bodyRva);
                }
            }
            else
            {
                ilWriter.Align(4);

                bodyRva = ilWriter.Position;

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

            return bodyRva;
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

            var writer = PooledBlobBuilder.GetInstance();
            writer.WriteByte(0x07);
            writer.WriteCompressedInteger((uint)localVariables.Length);
            foreach (ILocalDefinition local in localVariables)
            {
                this.SerializeLocalVariableSignature(writer, local);
            }

            BlobIdx blobIndex = heaps.GetBlobIndex(writer);
            int signatureIndex = this.GetOrAddStandAloneSignatureIndex(blobIndex);
            writer.Free();

            return signatureIndex;
        }

        protected void SerializeLocalVariableSignature(BlobBuilder writer, ILocalDefinition local)
        {
            if (module.IsPlatformType(local.Type, PlatformType.SystemTypedReference))
            {
                writer.WriteByte(0x16);
            }
            else
            {
                foreach (ICustomModifier customModifier in local.CustomModifiers)
                {
                    this.SerializeCustomModifier(customModifier, writer);
                }

                if (local.IsPinned)
                {
                    writer.WriteByte(0x45);
                }

                if (local.IsReference)
                {
                    writer.WriteByte(0x10);
                }

                this.SerializeTypeReference(local.Type, writer, false, true);
            }
        }

        internal int SerializeLocalConstantStandAloneSignature(ILocalDefinition localConstant)
        {
            var writer = PooledBlobBuilder.GetInstance();
            writer.WriteByte(0x06);

            foreach (ICustomModifier modifier in localConstant.CustomModifiers)
            {
                this.SerializeCustomModifier(modifier, writer);
            }

            this.SerializeTypeReference(localConstant.Type, writer, false, true);
            BlobIdx blobIndex = heaps.GetBlobIndex(writer);
            int signatureIndex = GetOrAddStandAloneSignatureIndex(blobIndex);
            writer.Free();

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
                const int overflowToken = 0x70000000; // 0x70 is a token type for a user string
                int token;
                if (!_userStringTokenOverflow)
                {
                    if (!heaps.TryGetUserStringToken(str, out token))
                    {
                        this.Context.Diagnostics.Add(this.messageProvider.CreateDiagnostic(this.messageProvider.ERR_TooManyUserStrings,
                                                                                           NoLocation.Singleton));
                        _userStringTokenOverflow = true;
                        token = overflowToken;
                    }
                }
                else
                {
                    token = overflowToken;
                }

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
            var writer = builder.ReserveBytes(methodBodyIL.Length);
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

        private void SerializeParameterInformation(IParameterTypeInformation parameterTypeInformation, BlobBuilder writer)
        {
            ushort countOfCustomModifiersPrecedingByRef = parameterTypeInformation.CountOfCustomModifiersPrecedingByRef;
            var modifiers = parameterTypeInformation.CustomModifiers;

            Debug.Assert(countOfCustomModifiersPrecedingByRef == 0 || parameterTypeInformation.IsByReference);

            if (parameterTypeInformation.IsByReference)
            {
                for (int i = 0; i < countOfCustomModifiersPrecedingByRef; i++)
                {
                    this.SerializeCustomModifier(modifiers[i], writer);
                }

                writer.WriteByte(0x10);
            }

            for (int i = countOfCustomModifiersPrecedingByRef; i < modifiers.Length; i++)
            {
                this.SerializeCustomModifier(modifiers[i], writer);
            }

            this.SerializeTypeReference(parameterTypeInformation.GetType(Context), writer, false, true);
        }

        private void SerializeFieldSignature(IFieldReference fieldReference, BlobBuilder writer)
        {
            writer.WriteByte(0x06);

            this.SerializeTypeReference(fieldReference.GetType(Context), writer, false, true);
        }

        private void SerializeGenericMethodInstanceSignature(BlobBuilder writer, IGenericMethodInstanceReference genericMethodInstanceReference)
        {
            writer.WriteByte(0x0a);
            writer.WriteCompressedInteger(genericMethodInstanceReference.GetGenericMethod(Context).GenericParameterCount);
            foreach (ITypeReference genericArgument in genericMethodInstanceReference.GetGenericArguments(Context))
            {
                this.SerializeTypeReference(genericArgument, writer, false, true);
            }
        }

        private void SerializeCustomAttributeSignature(ICustomAttribute customAttribute, bool writeOnlyNamedArguments, BlobBuilder writer)
        {
            if (!writeOnlyNamedArguments)
            {
                writer.WriteUInt16(0x0001);
                var parameters = customAttribute.Constructor(Context).GetParameters(Context).GetEnumerator();
                foreach (var argument in customAttribute.GetArguments(Context))
                {
                    var success = parameters.MoveNext();
                    Debug.Assert(success);
                    if (!success)
                    {
                        // TODO: md error    
                        break;
                    }

                    this.SerializeMetadataExpression(writer, argument, parameters.Current.GetType(Context));
                }

                Debug.Assert(!parameters.MoveNext());

                writer.WriteUInt16(customAttribute.NamedArgumentCount);
            }
            else
            {
                writer.WriteCompressedInteger(customAttribute.NamedArgumentCount);
            }

            if (customAttribute.NamedArgumentCount > 0)
            {
                foreach (IMetadataNamedArgument namedArgument in customAttribute.GetNamedArguments(Context))
                {
                    writer.WriteByte(namedArgument.IsField ? (byte)0x53 : (byte)0x54);
                    if (this.module.IsPlatformType(namedArgument.Type, PlatformType.SystemObject))
                    {
                        writer.WriteByte(0x51);
                    }
                    else
                    {
                        this.SerializeTypeReference(namedArgument.Type, writer, true, true);
                    }

                    writer.WriteSerializedString(namedArgument.ArgumentName);

                    this.SerializeMetadataExpression(writer, namedArgument.ArgumentValue, namedArgument.Type);
                }
            }
        }

        private void SerializeMetadataExpression(BlobBuilder writer, IMetadataExpression expression, ITypeReference targetType)
        {
            IMetadataCreateArray a = expression as IMetadataCreateArray;
            if (a != null)
            {
                ITypeReference targetElementType;
                var targetArrayType = targetType as IArrayTypeReference;

                if (targetArrayType == null)
                {
                    // implicit conversion from array to object
                    Debug.Assert(this.module.IsPlatformType(targetType, PlatformType.SystemObject));

                    targetElementType = a.ElementType;

                    writer.WriteByte(0x1d);
                    this.SerializeTypeReference(targetElementType, writer, true, true);
                }
                else
                {
                    targetElementType = targetArrayType.GetElementType(this.Context);
                }

                writer.WriteUInt32(a.ElementCount);

                foreach (IMetadataExpression elemValue in a.Elements)
                {
                    this.SerializeMetadataExpression(writer, elemValue, targetElementType);
                }
            }
            else
            {
                IMetadataConstant c = expression as IMetadataConstant;

                if (this.module.IsPlatformType(targetType, PlatformType.SystemObject))
                {
                    if (c != null &&
                        c.Value == null &&
                        this.module.IsPlatformType(c.Type, PlatformType.SystemObject))
                    {
                        // handle null case
                        writer.WriteByte(0x0e); // serialize string type
                        writer.WriteByte(0xFF); // null string
                        return;
                    }

                    this.SerializeTypeReference(expression.Type, writer, true, true);
                }

                if (c != null)
                {
                    if (c.Type is IArrayTypeReference)
                    {
                        writer.WriteInt32(-1); // null array
                    }
                    else if (c.Type.TypeCode(Context) == PrimitiveTypeCode.String)
                    {
                        writer.WriteSerializedString((string)c.Value);
                    }
                    else if (this.module.IsPlatformType(c.Type, PlatformType.SystemType))
                    {
                        Debug.Assert(c.Value == null);
                        writer.WriteSerializedString(null);
                    }
                    else
                    {
                        writer.WriteConstant(c.Value);
                    }
                }
                else
                {
                    IMetadataTypeOf t = expression as IMetadataTypeOf;
                    if (t != null)
                    {
                        this.SerializeTypeName(t.TypeToGet, writer);
                    }
                    else
                    {
                        // TODO: error
                    }
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
                var customAttributeWriter = PooledBlobBuilder.GetInstance();
                this.SerializeCustomAttributeSignature(customAttribute, true, customAttributeWriter);
                writer.WriteCompressedInteger((uint)customAttributeWriter.Count);
                customAttributeWriter.WriteContentTo(writer);
                customAttributeWriter.Free();
            }
            // TODO: xml for older platforms
        }

        private void SerializeSignature(ISignature signature, ushort genericParameterCount, ImmutableArray<IParameterTypeInformation> extraArgumentTypes, BlobBuilder writer)
        {
            byte header = (byte)signature.CallingConvention;
            if (signature is IPropertyDefinition)
            {
                header |= 0x08;
            }

            writer.WriteByte(header);
            if (genericParameterCount > 0)
            {
                writer.WriteCompressedInteger(genericParameterCount);
            }

            var @params = signature.GetParameters(Context);
            uint numberOfRequiredParameters = (uint)@params.Length;
            uint numberOfOptionalParameters = (uint)extraArgumentTypes.Length;
            writer.WriteCompressedInteger(numberOfRequiredParameters + numberOfOptionalParameters);

            foreach (ICustomModifier customModifier in signature.ReturnValueCustomModifiers)
            {
                this.SerializeCustomModifier(customModifier, writer);
            }

            if (signature.ReturnValueIsByRef)
            {
                writer.WriteByte(0x10);
            }

            this.SerializeTypeReference(signature.GetType(Context), writer, false, true);
            foreach (IParameterTypeInformation parameterTypeInformation in @params)
            {
                this.SerializeParameterInformation(parameterTypeInformation, writer);
            }

            if (numberOfOptionalParameters > 0)
            {
                writer.WriteByte(0x41);
                foreach (IParameterTypeInformation extraArgumentTypeInformation in extraArgumentTypes)
                {
                    this.SerializeParameterInformation(extraArgumentTypeInformation, writer);
                }
            }
        }

        private void SerializeTypeReference(ITypeReference typeReference, BlobBuilder writer, bool noTokens, bool treatRefAsPotentialTypeSpec)
        {
            while (true)
            {
                var modifiedTypeReference = typeReference as IModifiedTypeReference;
                if (modifiedTypeReference != null)
                {
                    foreach (ICustomModifier customModifier in modifiedTypeReference.CustomModifiers)
                    {
                        this.SerializeCustomModifier(customModifier, writer);
                    }

                    typeReference = modifiedTypeReference.UnmodifiedType;
                }

                switch (typeReference.TypeCode(Context))
                {
                    case PrimitiveTypeCode.Void:
                        writer.WriteByte(0x01);
                        return;
                    case PrimitiveTypeCode.Boolean:
                        writer.WriteByte(0x02);
                        return;
                    case PrimitiveTypeCode.Char:
                        writer.WriteByte(0x03);
                        return;
                    case PrimitiveTypeCode.Int8:
                        writer.WriteByte(0x04);
                        return;
                    case PrimitiveTypeCode.UInt8:
                        writer.WriteByte(0x05);
                        return;
                    case PrimitiveTypeCode.Int16:
                        writer.WriteByte(0x06);
                        return;
                    case PrimitiveTypeCode.UInt16:
                        writer.WriteByte(0x07);
                        return;
                    case PrimitiveTypeCode.Int32:
                        writer.WriteByte(0x08);
                        return;
                    case PrimitiveTypeCode.UInt32:
                        writer.WriteByte(0x09);
                        return;
                    case PrimitiveTypeCode.Int64:
                        writer.WriteByte(0x0a);
                        return;
                    case PrimitiveTypeCode.UInt64:
                        writer.WriteByte(0x0b);
                        return;
                    case PrimitiveTypeCode.Float32:
                        writer.WriteByte(0x0c);
                        return;
                    case PrimitiveTypeCode.Float64:
                        writer.WriteByte(0x0d);
                        return;
                    case PrimitiveTypeCode.String:
                        writer.WriteByte(0x0e);
                        return;
                    case PrimitiveTypeCode.Pointer:
                        var pointerTypeReference = typeReference as IPointerTypeReference;
                        if (pointerTypeReference != null)
                        {
                            if (noTokens)
                            {
                                this.SerializeTypeName(pointerTypeReference, writer);
                                return;
                            }

                            writer.WriteByte(0x0f);
                            typeReference = pointerTypeReference.GetTargetType(Context);
                            treatRefAsPotentialTypeSpec = true;
                            continue;
                        }

                        break;
                    case PrimitiveTypeCode.Reference:
                        var managedPointerTypeReference = typeReference as IManagedPointerTypeReference;
                        if (managedPointerTypeReference != null)
                        {
                            if (noTokens)
                            {
                                this.SerializeTypeName(managedPointerTypeReference, writer);
                                return;
                            }

                            writer.WriteByte(0x10);
                            typeReference = managedPointerTypeReference.GetTargetType(Context);
                            treatRefAsPotentialTypeSpec = true;
                            continue;
                        }

                        break;
                    case PrimitiveTypeCode.IntPtr:
                        writer.WriteByte(0x18);
                        return;
                    case PrimitiveTypeCode.UIntPtr:
                        writer.WriteByte(0x19);
                        return;
                }


                IGenericTypeParameterReference genericTypeParameterReference = typeReference.AsGenericTypeParameterReference;
                if (genericTypeParameterReference != null)
                {
                    writer.WriteByte(0x13);
                    uint numberOfInheritedParameters = GetNumberOfInheritedTypeParameters(genericTypeParameterReference.DefiningType);
                    writer.WriteCompressedInteger(numberOfInheritedParameters + genericTypeParameterReference.Index);
                    return;
                }

                var arrayTypeReference = typeReference as IArrayTypeReference;
                if (arrayTypeReference?.IsSZArray == false)
                {
                    Debug.Assert(noTokens == false, "Custom attributes cannot have multi-dimensional arrays");

                    writer.WriteByte(0x14);
                    this.SerializeTypeReference(arrayTypeReference.GetElementType(Context), writer, false, true);
                    writer.WriteCompressedInteger(arrayTypeReference.Rank);
                    writer.WriteCompressedInteger(IteratorHelper.EnumerableCount(arrayTypeReference.Sizes));
                    foreach (ulong size in arrayTypeReference.Sizes)
                    {
                        writer.WriteCompressedInteger((uint)size);
                    }

                    writer.WriteCompressedInteger(IteratorHelper.EnumerableCount(arrayTypeReference.LowerBounds));
                    foreach (int lowerBound in arrayTypeReference.LowerBounds)
                    {
                        writer.WriteCompressedSignedInteger(lowerBound);
                    }

                    return;
                }

                if (module.IsPlatformType(typeReference, PlatformType.SystemTypedReference))
                {
                    writer.WriteByte(0x16);
                    return;
                }

                if (module.IsPlatformType(typeReference, PlatformType.SystemObject))
                {
                    if (noTokens)
                    {
                        writer.WriteByte(0x51);
                    }
                    else
                    {
                        writer.WriteByte(0x1c);
                    }

                    return;
                }

                if (arrayTypeReference != null && arrayTypeReference.IsSZArray)
                {
                    writer.WriteByte(0x1d);
                    typeReference = arrayTypeReference.GetElementType(Context);
                    treatRefAsPotentialTypeSpec = true;
                    continue;
                }

                IGenericMethodParameterReference genericMethodParameterReference = typeReference.AsGenericMethodParameterReference;
                if (genericMethodParameterReference != null)
                {
                    writer.WriteByte(0x1e);
                    writer.WriteCompressedInteger(genericMethodParameterReference.Index);
                    return;
                }

                if (!noTokens && typeReference.IsTypeSpecification() && treatRefAsPotentialTypeSpec)
                {
                    ITypeReference uninstantiatedTypeReference = typeReference.GetUninstantiatedGenericType();

                    // Roslyn's uninstantiated type is the same object as the instantiated type for
                    // types closed over their type parameters, so to speak.

                    writer.WriteByte(0x15);
                    this.SerializeTypeReference(uninstantiatedTypeReference, writer, false, false);
                    var consolidatedTypeArguments = ArrayBuilder<ITypeReference>.GetInstance();
                    typeReference.GetConsolidatedTypeArguments(consolidatedTypeArguments, this.Context);
                    writer.WriteCompressedInteger((uint)consolidatedTypeArguments.Count);
                    foreach (ITypeReference typeArgument in consolidatedTypeArguments)
                    {
                        this.SerializeTypeReference(typeArgument, writer, false, true);
                    }

                    consolidatedTypeArguments.Free();

                    return;
                }

                if (noTokens)
                {
                    if (this.module.IsPlatformType(typeReference, PlatformType.SystemType))
                    {
                        writer.WriteByte(0x50);
                    }
                    else if (!typeReference.IsEnum)
                    {
                        writer.WriteByte(0x51);
                    }
                    else
                    {
                        writer.WriteByte(0x55);
                        this.SerializeTypeName(typeReference, writer);
                    }
                }
                else
                {
                    if (typeReference.IsValueType)
                    {
                        writer.WriteByte(0x11);
                    }
                    else
                    {
                        writer.WriteByte(0x12);
                    }

                    writer.WriteCompressedInteger(this.GetTypeDefOrRefCodedIndex(typeReference, treatRefAsPotentialTypeSpec));
                }

                return;
            }
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
