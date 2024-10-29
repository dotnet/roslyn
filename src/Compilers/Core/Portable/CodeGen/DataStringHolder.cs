// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen;

internal sealed class DataStringHolder : DefaultTypeDef, Cci.INamespaceTypeDefinition
{
    private const string TypeNamePrefix = "<S>";

    private readonly string _name;
    private readonly CommonPEModuleBuilder _moduleBuilder;
    private readonly Cci.ITypeReference _systemObject;
    private readonly Cci.ICustomAttribute _compilerGeneratedAttribute;
    private readonly PrivateImplementationDetails _privateImplementationDetails;

    private int _frozen;
    private ImmutableArray<SynthesizedStaticField> _orderedSynthesizedFields;

    // fields mapped to metadata blocks
    private readonly ConcurrentDictionary<(ImmutableArray<byte> Data, ushort Alignment), MappedField> _mappedFields =
        new ConcurrentDictionary<(ImmutableArray<byte> Data, ushort Alignment), MappedField>(PrivateImplementationDetails.DataAndUShortEqualityComparer.Instance);

    public DataStringHolder(
        CommonPEModuleBuilder moduleBuilder,
        string moduleName,
        int submissionSlotIndex,
        string nameSuffix,
        Cci.ITypeReference systemObject,
        Cci.ICustomAttribute compilerGeneratedAttribute,
        PrivateImplementationDetails privateImplementationDetails)
    {
        _moduleBuilder = moduleBuilder;
        _systemObject = systemObject;
        _compilerGeneratedAttribute = compilerGeneratedAttribute;
        _privateImplementationDetails = privateImplementationDetails;

        // we include the module name in the name of the PrivateImplementationDetails class so that more than
        // one of them can be included in an assembly as part of netmodules.    
        var name = (moduleBuilder.OutputKind == OutputKind.NetModule) ?
            $"{TypeNamePrefix}<{MetadataHelpers.MangleForTypeNameIfNeeded(moduleName)}>" : TypeNamePrefix;

        if (submissionSlotIndex >= 0)
        {
            name += submissionSlotIndex.ToString();
        }

        if (moduleBuilder.CurrentGenerationOrdinal > 0)
        {
            name += "#" + moduleBuilder.CurrentGenerationOrdinal;
        }

        name += "_" + nameSuffix;

        _name = name;
    }

    public string Name => _name;

    public string NamespaceName => string.Empty;

    public bool IsPublic => false;

    internal bool IsFrozen => _frozen != 0;

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

    public Cci.IUnitReference GetUnit(EmitContext context)
    {
        Debug.Assert(context.Module == _moduleBuilder);
        return _moduleBuilder;
    }

    public Cci.IFieldReference CreateDataField(ImmutableArray<byte> data)
    {
        Cci.ITypeReference type = _privateImplementationDetails.GetOrAddProxyType(data.Length, alignment: 1);

        return _mappedFields.GetOrAdd((data, Alignment: 1), key => new MappedField("f", this, type, data));
    }

    public void Freeze()
    {
        var wasFrozen = Interlocked.Exchange(ref _frozen, 1);
        if (wasFrozen != 0)
        {
            throw new InvalidOperationException();
        }

        var fieldsBuilder = ArrayBuilder<SynthesizedStaticField>.GetInstance(_mappedFields.Count);
        fieldsBuilder.AddRange(_mappedFields.Values);
        fieldsBuilder.Sort(PrivateImplementationDetails.FieldComparer.Instance);
        _orderedSynthesizedFields = fieldsBuilder.ToImmutableAndFree();
    }
}
