// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen;

internal sealed class DataStringHolder : DefaultTypeDef, Cci.INamespaceTypeDefinition
{
    private const string TypeNamePrefix = "<S>";

    private readonly string _name;
    private readonly CommonPEModuleBuilder _moduleBuilder;
    private readonly Cci.ITypeReference _systemObject;
    private readonly Cci.ITypeReference _systemString;
    private readonly Cci.ICustomAttribute _compilerGeneratedAttribute;
    private readonly PrivateImplementationDetails _privateImplementationDetails;

    private int _frozen;
    private ImmutableArray<SynthesizedStaticField> _orderedSynthesizedFields;

    // fields mapped to metadata blocks and the corresponding `string` fields
    private readonly ConcurrentDictionary<(ImmutableArray<byte> Data, ushort Alignment), (MappedField Bytes, StringField String)> _mappedFields =
        new ConcurrentDictionary<(ImmutableArray<byte> Data, ushort Alignment), (MappedField Bytes, StringField String)>(PrivateImplementationDetails.DataAndUShortEqualityComparer.Instance);

    // synthesized methods
    private ImmutableArray<Cci.IMethodDefinition> _orderedSynthesizedMethods;

    public DataStringHolder(
        CommonPEModuleBuilder moduleBuilder,
        string dataHash,
        Cci.ITypeReference systemObject,
        Cci.ITypeReference systemString,
        Cci.ICustomAttribute compilerGeneratedAttribute,
        PrivateImplementationDetails privateImplementationDetails)
    {
        _name = TypeNamePrefix + dataHash;
        _moduleBuilder = moduleBuilder;
        _systemObject = systemObject;
        _systemString = systemString;
        _compilerGeneratedAttribute = compilerGeneratedAttribute;
        _privateImplementationDetails = privateImplementationDetails;
    }

    public string Name => _name;

    public string NamespaceName => string.Empty;

    public bool IsPublic => false;

    internal bool IsFrozen => _frozen != 0;

    public override Cci.INamespaceTypeDefinition AsNamespaceTypeDefinition(EmitContext context) => this;

    public override Cci.INamespaceTypeReference AsNamespaceTypeReference => this;

    // TODO: Needs unique field names if called more than once.
    public Cci.IFieldReference CreateDataField(ImmutableArray<byte> data)
    {
        Cci.ITypeReference type = _privateImplementationDetails.GetOrAddProxyType(data.Length, alignment: 1);

        (_, StringField stringField) = _mappedFields.GetOrAdd((data, Alignment: 1), key =>
        {
            var mappedField = new MappedField("f", this, type, data);
            var stringField = new StringField(this, mappedField);
            return (mappedField, stringField);
        });

        return stringField;
    }

    public override void Dispatch(Cci.MetadataVisitor visitor) => visitor.Visit(this);

    public void Freeze(DiagnosticBag diagnostics)
    {
        var wasFrozen = Interlocked.Exchange(ref _frozen, 1);
        if (wasFrozen != 0)
        {
            throw new InvalidOperationException();
        }

        var fieldsBuilder = ArrayBuilder<SynthesizedStaticField>.GetInstance(_mappedFields.Count);

        foreach (var (mappedField, stringField) in _mappedFields.Values)
        {
            fieldsBuilder.Add(mappedField);
            fieldsBuilder.Add(stringField);
        }

        fieldsBuilder.Sort(PrivateImplementationDetails.FieldComparer.Instance);

        _orderedSynthesizedFields = fieldsBuilder.ToImmutableAndFree();

        _orderedSynthesizedMethods = [SynthesizeStaticConstructor(diagnostics)];
    }

    public override IEnumerable<Cci.ICustomAttribute> GetAttributes(EmitContext context)
    {
        if (_compilerGeneratedAttribute != null)
        {
            return SpecializedCollections.SingletonEnumerable(_compilerGeneratedAttribute);
        }

        return SpecializedCollections.EmptyEnumerable<Cci.ICustomAttribute>();
    }

    public override Cci.ITypeReference GetBaseClass(EmitContext context) => _systemObject;

    public override IEnumerable<Cci.IFieldDefinition> GetFields(EmitContext context)
    {
        Debug.Assert(IsFrozen);
        return _orderedSynthesizedFields;
    }

    public override IEnumerable<Cci.IMethodDefinition> GetMethods(EmitContext context)
    {
        Debug.Assert(IsFrozen);
        return _orderedSynthesizedMethods;
    }

    public Cci.IUnitReference GetUnit(EmitContext context)
    {
        Debug.Assert(context.Module == _moduleBuilder);
        return _moduleBuilder;
    }

    private StaticConstructor SynthesizeStaticConstructor(DiagnosticBag diagnostics)
    {
        var ilBuilder = new ILBuilder((ITokenDeferral)_moduleBuilder, new LocalSlotManager(slotAllocator: null), OptimizationLevel.Release, areLocalsZeroed: false);

        foreach (var field in _orderedSynthesizedFields)
        {
            if (field is not StringField stringField)
            {
                continue;
            }

            // Call `Encoding.get_UTF8()`.
            ilBuilder.EmitOpCode(ILOpCode.Call, 1);
            ilBuilder.EmitToken(ilBuilder.module.GetEncodingUtf8(), null, diagnostics);

            // Push the `byte*` field's address.
            ilBuilder.EmitOpCode(ILOpCode.Ldsflda);
            ilBuilder.EmitToken(stringField.MappedField, null, diagnostics);

            // Push the byte size.
            ilBuilder.EmitIntConstant(stringField.MappedField.MappedData.Length);

            // Call `Encoding.GetString(byte*, int)`.
            ilBuilder.EmitOpCode(ILOpCode.Callvirt, -2);
            ilBuilder.EmitToken(ilBuilder.module.GetEncodingGetString(), null, diagnostics);

            // Store into the corresponding `string` field.
            ilBuilder.EmitOpCode(ILOpCode.Stsfld);
            ilBuilder.EmitToken(stringField, null, diagnostics);
        }

        ilBuilder.EmitRet(isVoid: true);
        ilBuilder.Realize();

        return new StaticConstructor(this, ilBuilder.MaxStack, ilBuilder.RealizedIL);
    }

    public override string ToString() => $"{nameof(DataStringHolder)}: {_name}";

    private sealed class StringField(
        DataStringHolder containingType,
        MappedField mappedField)
        : SynthesizedStaticField("s", containingType, containingType._systemString)
    {
        private readonly MappedField _mappedField = mappedField;

        internal MappedField MappedField => _mappedField;
        public override ImmutableArray<byte> MappedData => default;
        public override bool IsReadOnly => true;
    }

    private sealed class StaticConstructor(
        DataStringHolder containingType,
        ushort maxStack,
        ImmutableArray<byte> il)
        : Cci.IMethodDefinition, Cci.IMethodBody
    {
        private readonly DataStringHolder _containingType = containingType;
        private readonly ushort _maxStack = maxStack;
        private readonly ImmutableArray<byte> _il = il;

        #region IMethodDefinition
        public bool HasBody => true;
        public IEnumerable<Cci.IGenericMethodParameter> GenericParameters => [];
        public bool HasDeclarativeSecurity => false;
        public bool IsAbstract => false;
        public bool IsAccessCheckedOnOverride => false;
        public bool IsConstructor => false;
        public bool IsExternal => false;
        public bool IsHiddenBySignature => true;
        public bool IsNewSlot => false;
        public bool IsPlatformInvoke => false;
        public bool IsRuntimeSpecial => true;
        public bool IsSealed => false;
        public bool IsSpecialName => true;
        public bool IsStatic => true;
        public bool IsVirtual => false;
        public ImmutableArray<Cci.IParameterDefinition> Parameters => [];
        public Cci.IPlatformInvokeInformation PlatformInvokeData => throw ExceptionUtilities.Unreachable();
        public bool RequiresSecurityObject => false;
        public bool ReturnValueIsMarshalledExplicitly => false;
        public Cci.IMarshallingInformation ReturnValueMarshallingInformation => throw ExceptionUtilities.Unreachable();
        public ImmutableArray<byte> ReturnValueMarshallingDescriptor => throw ExceptionUtilities.Unreachable();
        public IEnumerable<Cci.SecurityAttribute> SecurityAttributes => [];
        public Cci.INamespace ContainingNamespace => throw ExceptionUtilities.Unreachable();
        public Cci.ITypeDefinition ContainingTypeDefinition => _containingType;
        public Cci.TypeMemberVisibility Visibility => Cci.TypeMemberVisibility.Private;
        public bool IsEncDeleted => false;
        public bool AcceptsExtraArguments => false;
        public ushort GenericParameterCount => 0;
        public ImmutableArray<Cci.IParameterTypeInformation> ExtraParameters => [];
        public Cci.IGenericMethodInstanceReference? AsGenericMethodInstanceReference => null;
        public Cci.ISpecializedMethodReference? AsSpecializedMethodReference => null;
        public Cci.CallingConvention CallingConvention => Cci.CallingConvention.Default;
        public ushort ParameterCount => 0;
        public ImmutableArray<Cci.ICustomModifier> ReturnValueCustomModifiers => [];
        public ImmutableArray<Cci.ICustomModifier> RefCustomModifiers => [];
        public bool ReturnValueIsByRef => false;
        public string Name => WellKnownMemberNames.StaticConstructorName;

        public Cci.IMethodBody? GetBody(EmitContext context) => this;
        public Cci.IDefinition? AsDefinition(EmitContext context) => this;
        public void Dispatch(Cci.MetadataVisitor visitor) => visitor.Visit((Cci.IMethodDefinition)this);
        public IEnumerable<Cci.ICustomAttribute> GetAttributes(EmitContext context) => [];
        public Cci.ITypeReference GetContainingType(EmitContext context) => ContainingTypeDefinition;
        public MethodImplAttributes GetImplementationAttributes(EmitContext context) => default;
        public ISymbolInternal? GetInternalSymbol() => null;
        public ImmutableArray<Cci.IParameterTypeInformation> GetParameters(EmitContext context) => [];
        public Cci.IMethodDefinition? GetResolvedMethod(EmitContext context) => this;
        public IEnumerable<Cci.ICustomAttribute> GetReturnValueAttributes(EmitContext context) => [];
        public Cci.ITypeReference GetType(EmitContext context) => context.Module.GetPlatformType(Cci.PlatformType.SystemVoid, context);
        #endregion

        #region IMethodBody
        public ImmutableArray<Cci.ExceptionHandlerRegion> ExceptionRegions => [];
        public bool AreLocalsZeroed => false;
        public bool HasStackalloc => false;
        public ImmutableArray<Cci.ILocalDefinition> LocalVariables => [];
        public Cci.IMethodDefinition MethodDefinition => this;
        public StateMachineMoveNextBodyDebugInfo? MoveNextBodyInfo => null;
        public ushort MaxStack => _maxStack;
        public ImmutableArray<byte> IL => _il;
        public ImmutableArray<Cci.SequencePoint> SequencePoints => [];
        public bool HasDynamicLocalVariables => false;
        public ImmutableArray<Cci.LocalScope> LocalScopes => [];
        public Cci.IImportScope? ImportScope => null;
        public DebugId MethodId => default;
        public ImmutableArray<StateMachineHoistedLocalScope> StateMachineHoistedLocalScopes => [];
        public string? StateMachineTypeName => null;
        public ImmutableArray<EncHoistedLocalInfo> StateMachineHoistedLocalSlots => [];
        public ImmutableArray<Cci.ITypeReference?> StateMachineAwaiterSlots => [];
        public ImmutableArray<EncClosureInfo> ClosureDebugInfo => [];
        public ImmutableArray<EncLambdaInfo> LambdaDebugInfo => [];
        public ImmutableArray<LambdaRuntimeRudeEditInfo> OrderedLambdaRuntimeRudeEdits => [];
        public StateMachineStatesDebugInfo StateMachineStatesDebugInfo => default;
        public ImmutableArray<SourceSpan> CodeCoverageSpans => [];
        public bool IsPrimaryConstructor => false;
        #endregion
    }
}
