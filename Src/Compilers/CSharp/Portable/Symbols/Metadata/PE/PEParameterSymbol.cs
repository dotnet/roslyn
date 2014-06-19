// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    /// <summary>
    /// The class to represent all method parameters imported from a PE/module.
    /// </summary>
    internal sealed class PEParameterSymbol : ParameterSymbol
    {
        private readonly Symbol containingSymbol;
        private readonly string name;
        private readonly TypeSymbol type;
        private readonly ParameterHandle handle;
        private readonly ParameterAttributes flags;
        private readonly ImmutableArray<CustomModifier> customModifiers;
        private readonly PEModuleSymbol moduleSymbol;

        private ImmutableArray<CSharpAttributeData> lazyCustomAttributes;
        private ConstantValue lazyDefaultValue = ConstantValue.Unset;
        private ThreeState lazyIsParams;

        /// <summary>
        /// Attributes filtered out from m_lazyCustomAttributes, ParamArray, etc.
        /// </summary>
        private ImmutableArray<CSharpAttributeData> lazyHiddenAttributes;

        // Used to track the completion state and presence of well known attributes.
        //
        // layout:
        // |................|cccccccc|vvvvvvvv|
        // 
        // c = completion states for well known attributes. 1 if given attribute has been decoded, 0 otherwise. 8 bits.
        // v = decoded well known attribute values. 8 bits.
        // . = unused bit
        private int lazyWellKnownAttributeData;

        private readonly ushort ordinal;

        // Layout:
        // |.....|h|rr|
        //
        // r = RefKind - 2 bits (RefKind)
        // h = hasByRefBeforeCustomModifiers - 1 bit (bool)
        private readonly byte packed;
        private const int refKindMask = 0x3;
        private const int hasByRefBeforeCustomModifiersMask = 0x4;

        private const int WellKnownAttributeCompletionFlagOffset = 8;

        [Flags]
        private enum WellKnownAttributeFlags
        {
            HasIDispatchConstantAttribute = 0x1 << 0,
            HasIUnknownConstantAttribute = 0x1 << 1,
            HasCallerFilePathAttribute = 0x1 << 2,
            HasCallerLineNumberAttribute = 0x1 << 3,
            HasCallerMemberNameAttribute = 0x1 << 4,
            IsCallerFilePath = 0x1 << 5,
            IsCallerLineNumber = 0x1 << 6,
            IsCallerMemberName = 0x1 << 7,

            AllCompletedNoAttributes = 0xff00
        }

        internal PEParameterSymbol(
            PEModuleSymbol moduleSymbol,
            PEMethodSymbol containingSymbol,
            int ordinal,
            MetadataDecoder.ParamInfo parameter,
            out bool isBad)
            : this(moduleSymbol, containingSymbol, ordinal, parameter.IsByRef, parameter.HasByRefBeforeCustomModifiers, parameter.Type, parameter.Handle, parameter.CustomModifiers, out isBad)
        {
        }

        /// <summary>
        /// Construct a parameter symbol for a property loaded from metadata.
        /// </summary>
        /// <param name="moduleSymbol"></param>
        /// <param name="containingSymbol"></param>
        /// <param name="ordinal"></param>
        /// <param name="handle">The property parameter doesn't have a name in metadata,
        /// so this is the handle of a corresponding accessor parameter, if there is one,
        /// or of the ParamInfo passed in, otherwise).</param>
        /// <param name="isBad" />
        /// <param name="parameter"></param>
        internal PEParameterSymbol(
            PEModuleSymbol moduleSymbol,
            PEPropertySymbol containingSymbol,
            int ordinal,
            ParameterHandle handle,
            MetadataDecoder.ParamInfo parameter,
            out bool isBad)
            : this(moduleSymbol, containingSymbol, ordinal, parameter.IsByRef, parameter.HasByRefBeforeCustomModifiers, parameter.Type, handle, parameter.CustomModifiers, out isBad)
        {
        }

        private PEParameterSymbol(
            PEModuleSymbol moduleSymbol,
            Symbol containingSymbol,
            int ordinal,
            bool isByRef,
            bool hasByRefBeforeCustomModifiers,
            TypeSymbol type,
            ParameterHandle handle,
            ImmutableArray<MetadataDecoder.ModifierInfo> customModifiers,
            out bool isBad)
        {
            Debug.Assert((object)moduleSymbol != null);
            Debug.Assert((object)containingSymbol != null);
            Debug.Assert(ordinal >= 0);
            Debug.Assert((object)type != null);

            isBad = false;
            this.moduleSymbol = moduleSymbol;
            this.containingSymbol = containingSymbol;
            this.customModifiers = CSharpCustomModifier.Convert(customModifiers);
            this.ordinal = (ushort)ordinal;

            this.handle = handle;

            RefKind refKind = RefKind.None;

            if (handle.IsNil)
            {
                refKind = isByRef ? RefKind.Ref : RefKind.None;

                this.type = type;
                this.lazyCustomAttributes = ImmutableArray<CSharpAttributeData>.Empty;
                this.lazyHiddenAttributes = ImmutableArray<CSharpAttributeData>.Empty;
                this.lazyWellKnownAttributeData = (int)WellKnownAttributeFlags.AllCompletedNoAttributes;
                this.lazyDefaultValue = ConstantValue.NotAvailable;
                this.lazyIsParams = ThreeState.False;
            }
            else
            {
                try
                {
                    moduleSymbol.Module.GetParamPropsOrThrow(handle, out this.name, out this.flags);
                }
                catch (BadImageFormatException)
                {
                    isBad = true;
                }

                if (isByRef)
                {
                    ParameterAttributes inOutFlags = this.flags & (ParameterAttributes.Out | ParameterAttributes.In);
                    refKind = (inOutFlags == ParameterAttributes.Out) ? RefKind.Out : RefKind.Ref;
                }

                // CONSIDER: Can we make parameter type computation lazy?
                this.type = DynamicTypeDecoder.TransformType(type, this.customModifiers.Length, handle, moduleSymbol, refKind);
            }

            if (string.IsNullOrEmpty(this.name))
            {
                // As was done historically, if the parameter doesn't have a name, we give it the name "value".
                this.name = "value";
            }

            this.packed = Pack(refKind, hasByRefBeforeCustomModifiers);

            Debug.Assert(refKind == this.RefKind);
            Debug.Assert(hasByRefBeforeCustomModifiers == this.HasByRefBeforeCustomModifiers);
        }

        private static byte Pack(RefKind refKind, bool hasByRefBeforeCustomModifiers)
        {
            Debug.Assert(!hasByRefBeforeCustomModifiers || refKind != RefKind.None);
            int refKindBits = (int)refKind;
            Debug.Assert((refKindBits & refKindMask) == refKindBits, "If the range of RefKind changes, the mask will need to be updated.");
            int hasByRefBeforeCustomModifiersBits = hasByRefBeforeCustomModifiers ? hasByRefBeforeCustomModifiersMask : 0;
            return (byte)(refKindBits | hasByRefBeforeCustomModifiersBits);
        }

        public override RefKind RefKind
        {
            get
            {
                return (RefKind)(this.packed & refKindMask);
            }
        }

        public override string Name
        {
            get
            {
                return this.name;
            }
        }

        internal ParameterAttributes Flags
        {
            get
            {
                return flags;
            }
        }

        public override int Ordinal
        {
            get
            {
                return this.ordinal;
            }
        }

        // might be Nil
        internal ParameterHandle Handle
        {
            get
            {
                return handle;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return this.containingSymbol;
            }
        }

        internal override bool HasMetadataConstantValue
        {
            get
            {
                return (this.flags & ParameterAttributes.HasDefault) != 0;
            }
        }

        /// <remarks>
        /// Internal for testing.  Non-test code should use <see cref="ExplicitDefaultConstantValue"/>.
        /// </remarks>
        internal ConstantValue ImportConstantValue()
        {
            Debug.Assert(!this.handle.IsNil);

            // Metadata Spec 22.33: 
            //   6. If Flags.HasDefault = 1 then this row [of Param table] shall own exactly one row in the Constant table [ERROR]
            //   7. If Flags.HasDefault = 0, then there shall be no rows in the Constant table owned by this row [ERROR]
            ConstantValue value = null;

            if ((this.flags & ParameterAttributes.HasDefault) != 0)
            {
                value = this.moduleSymbol.Module.GetParamDefaultValue(this.handle);
            }

            if (value == null)
            {
                value = GetDefaultDecimalOrDateTimeValue();
            }

            return value;
        }

        internal override ConstantValue ExplicitDefaultConstantValue
        {
            get
            {
                // From the C# point of view, there is no need to import a parameter's default value
                // if the language isn't going to treat it as optional.
                // NOTE: This disrupts round-tripping, but the trade-off seems acceptable.
                if (!IsMetadataOptional)
                    return null;

                // The HasDefault flag has to be set, it doesn't suffice to mark the parameter with DefaultParameterValueAttribute.
                if (lazyDefaultValue == ConstantValue.Unset)
                {
                    ConstantValue value = ImportConstantValue();
                    Interlocked.CompareExchange(ref lazyDefaultValue, value, ConstantValue.Unset);
                }

                return lazyDefaultValue;
            }
        }

        private ConstantValue GetDefaultDecimalOrDateTimeValue()
        {
            Debug.Assert(!this.handle.IsNil);
            ConstantValue value = null;

            // It is possible in Visual Basic for a parameter of object type to have a default value of DateTime type.
            // If it's present, use it.  We'll let the call-site figure out whether it can actually be used.
            if (this.moduleSymbol.Module.HasDateTimeConstantAttribute(this.handle, out value))
            {
                return value;
            }

            // It is possible in Visual Basic for a parameter of object type to have a default value of decimal type.
            // If it's present, use it.  We'll let the call-site figure out whether it can actually be used.
            if (this.moduleSymbol.Module.HasDecimalConstantAttribute(this.handle, out value))
            {
                return value;
            }

            return value;
        }

        internal override bool IsMetadataOptional
        {
            get
            {
                return (this.flags & ParameterAttributes.Optional) != 0;
            }
        }

        private bool SetWellKnownAttribute(WellKnownAttributeFlags flag, bool value)
        {
            // a value has been decoded:
            int newFlags = (int)flag << WellKnownAttributeCompletionFlagOffset;
            if (value)
            {
                // the actual value:
                newFlags |= (int)flag;
            }

            ThreadSafeFlagOperations.Clear(ref this.lazyWellKnownAttributeData, (int)newFlags);
            return value;
        }

        private bool IsWellKnownAttributeComplete(WellKnownAttributeFlags flag)
        {
            var result = (this.lazyWellKnownAttributeData & ((int)flag << WellKnownAttributeCompletionFlagOffset)) != 0;

            // all well-known attributes are complete if we don't have handle:
            Debug.Assert(!this.handle.IsNil || result);
            return result;
        }

        private bool GetWellKnownAttribute(WellKnownAttributeFlags flag)
        {
            Debug.Assert(IsWellKnownAttributeComplete(flag));
            return (this.lazyWellKnownAttributeData & (int)flag) != 0;
        }

        internal override bool IsIDispatchConstant
        {
            get
            {
                const WellKnownAttributeFlags flag = WellKnownAttributeFlags.HasIDispatchConstantAttribute;

                return IsWellKnownAttributeComplete(flag)
                    ? GetWellKnownAttribute(flag)
                    : SetWellKnownAttribute(flag, moduleSymbol.Module.HasAttribute(this.handle,
                        AttributeDescription.IDispatchConstantAttribute));
            }
        }

        internal override bool IsIUnknownConstant
        {
            get
            {
                const WellKnownAttributeFlags flag = WellKnownAttributeFlags.HasIUnknownConstantAttribute;

                return IsWellKnownAttributeComplete(flag)
                    ? GetWellKnownAttribute(flag)
                    : SetWellKnownAttribute(flag, moduleSymbol.Module.HasAttribute(this.handle,
                        AttributeDescription.IUnknownConstantAttribute));
            }
        }

        private bool HasCallerLineNumberAttribute
        {
            get
            {
                const WellKnownAttributeFlags flag = WellKnownAttributeFlags.HasCallerLineNumberAttribute;

                return IsWellKnownAttributeComplete(flag)
                    ? GetWellKnownAttribute(flag)
                    : SetWellKnownAttribute(flag, moduleSymbol.Module.HasAttribute(this.handle,
                        AttributeDescription.CallerLineNumberAttribute));
            }
        }

        private bool HasCallerFilePathAttribute
        {
            get
            {
                const WellKnownAttributeFlags flag = WellKnownAttributeFlags.HasCallerFilePathAttribute;

                return IsWellKnownAttributeComplete(flag)
                    ? GetWellKnownAttribute(flag)
                    : SetWellKnownAttribute(flag, moduleSymbol.Module.HasAttribute(this.handle,
                        AttributeDescription.CallerFilePathAttribute));
            }
        }

        private bool HasCallerMemberNameAttribute
        {
            get
            {
                const WellKnownAttributeFlags flag = WellKnownAttributeFlags.HasCallerMemberNameAttribute;

                return IsWellKnownAttributeComplete(flag)
                    ? GetWellKnownAttribute(flag)
                    : SetWellKnownAttribute(flag, moduleSymbol.Module.HasAttribute(this.handle,
                        AttributeDescription.CallerMemberNameAttribute));
            }
        }

        internal override bool IsCallerLineNumber
        {
            get
            {
                const WellKnownAttributeFlags flag = WellKnownAttributeFlags.IsCallerLineNumber;

                if (IsWellKnownAttributeComplete(flag))
                {
                    return GetWellKnownAttribute(flag);
                }

                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                bool isCallerLineNumber = HasCallerLineNumberAttribute
                    && new TypeConversions(ContainingAssembly).HasCallerLineNumberConversion(this.Type, ref useSiteDiagnostics);

                return SetWellKnownAttribute(flag, isCallerLineNumber);
            }
        }

        internal override bool IsCallerFilePath
        {
            get
            {
                const WellKnownAttributeFlags flag = WellKnownAttributeFlags.IsCallerFilePath;

                if (IsWellKnownAttributeComplete(flag))
                {
                    return GetWellKnownAttribute(flag);
                }

                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                bool isCallerFilePath = !HasCallerLineNumberAttribute
                    && HasCallerFilePathAttribute
                    && new TypeConversions(ContainingAssembly).HasCallerInfoStringConversion(this.Type, ref useSiteDiagnostics);

                return SetWellKnownAttribute(flag, isCallerFilePath);
            }
        }

        internal override bool IsCallerMemberName
        {
            get
            {
                const WellKnownAttributeFlags flag = WellKnownAttributeFlags.IsCallerMemberName;

                if (IsWellKnownAttributeComplete(flag))
                {
                    return GetWellKnownAttribute(flag);
                }

                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                bool isCallerMemberName = !HasCallerLineNumberAttribute
                    && !HasCallerFilePathAttribute
                    && HasCallerMemberNameAttribute
                    && new TypeConversions(ContainingAssembly).HasCallerInfoStringConversion(this.Type, ref useSiteDiagnostics);

                return SetWellKnownAttribute(flag, isCallerMemberName);
            }
        }

        public override TypeSymbol Type
        {
            get
            {
                return this.type;
            }
        }

        public override ImmutableArray<CustomModifier> CustomModifiers
        {
            get
            {
                return customModifiers;
            }
        }

        internal sealed override bool HasByRefBeforeCustomModifiers
        {
            get
            {
                return (this.packed & hasByRefBeforeCustomModifiersMask) != 0;
            }
        }

        internal override bool IsMetadataIn
        {
            get { return (this.flags & ParameterAttributes.In) != 0; }
        }

        internal override bool IsMetadataOut
        {
            get { return (this.flags & ParameterAttributes.Out) != 0; }
        }

        internal override bool IsMarshalledExplicitly
        {
            get
            {
                return (flags & ParameterAttributes.HasFieldMarshal) != 0;
            }
        }

        internal override MarshalPseudoCustomAttributeData MarshallingInformation
        {
            get
            {
                // the compiler doesn't need full marshalling information, just the unmanaged type or descriptor
                return null;
            }
        }

        internal override ImmutableArray<byte> MarshallingDescriptor
        {
            get
            {
                if ((flags & ParameterAttributes.HasFieldMarshal) == 0)
                {
                    return default(ImmutableArray<byte>);
                }

                Debug.Assert(!this.handle.IsNil);
                return this.moduleSymbol.Module.GetMarshallingDescriptor(this.handle);
            }
        }

        internal override UnmanagedType MarshallingType
        {
            get
            {
                if ((flags & ParameterAttributes.HasFieldMarshal) == 0)
                {
                    return 0;
                }

                Debug.Assert(!this.handle.IsNil);
                return this.moduleSymbol.Module.GetMarshallingType(this.handle);
            }
        }

        public override bool IsParams
        {
            get
            {
                // This is also populated by loading attributes, but loading
                // attributes is more expensive, so we should only do it if
                // attributes are requested.
                if (!this.lazyIsParams.HasValue())
                {
                    this.lazyIsParams = moduleSymbol.Module.HasParamsAttribute(handle).ToThreeState();
                }
                return this.lazyIsParams.Value();
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return containingSymbol.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            if (this.lazyCustomAttributes.IsDefault)
            {
                Debug.Assert(!this.handle.IsNil);
                var containingPEModuleSymbol = (PEModuleSymbol)this.ContainingModule;

                // Filter out ParamArrayAttributes if necessary and cache
                // the attribute handle for GetCustomAttributesToEmit
                bool filterOutParamArrayAttribute = (!this.lazyIsParams.HasValue() || this.lazyIsParams.Value());

                ConstantValue defaultValue = this.ExplicitDefaultConstantValue;
                AttributeDescription filterOutConstantAttributeDescription = default(AttributeDescription);

                if ((object)defaultValue != null)
                {
                    if (defaultValue.Discriminator == ConstantValueTypeDiscriminator.DateTime)
                    {
                        filterOutConstantAttributeDescription = AttributeDescription.DateTimeConstantAttribute;
                    }
                    else if (defaultValue.Discriminator == ConstantValueTypeDiscriminator.Decimal)
                    {
                        filterOutConstantAttributeDescription = AttributeDescription.DecimalConstantAttribute;
                    }
                }

                if (filterOutParamArrayAttribute || filterOutConstantAttributeDescription.Signatures != null)
                {
                    CustomAttributeHandle paramArrayAttribute;
                    CustomAttributeHandle constantAttribute;

                    ImmutableArray<CSharpAttributeData> attributes =
                        containingPEModuleSymbol.GetCustomAttributesForToken(
                            this.handle,
                            out paramArrayAttribute,
                            filterOutParamArrayAttribute ? AttributeDescription.ParamArrayAttribute : default(AttributeDescription),
                            out constantAttribute,
                            filterOutConstantAttributeDescription);

                    if (!paramArrayAttribute.IsNil || !constantAttribute.IsNil)
                    {
                        var builder = ArrayBuilder<CSharpAttributeData>.GetInstance();

                        if (!paramArrayAttribute.IsNil)
                        {
                            builder.Add(new PEAttributeData(containingPEModuleSymbol, paramArrayAttribute));
                        }

                        if (!constantAttribute.IsNil)
                        {
                            builder.Add(new PEAttributeData(containingPEModuleSymbol, constantAttribute));
                        }

                        ImmutableInterlocked.InterlockedInitialize(ref lazyHiddenAttributes, builder.ToImmutableAndFree());
                    }
                    else
                    {
                        ImmutableInterlocked.InterlockedInitialize(ref lazyHiddenAttributes, ImmutableArray<CSharpAttributeData>.Empty);
                    }

                    if (!this.lazyIsParams.HasValue())
                    {
                        Debug.Assert(filterOutParamArrayAttribute);
                        this.lazyIsParams = (!paramArrayAttribute.IsNil).ToThreeState();
                    }

                    ImmutableInterlocked.InterlockedInitialize(
                        ref this.lazyCustomAttributes,
                        attributes);
                }
                else
                {
                    ImmutableInterlocked.InterlockedInitialize(ref lazyHiddenAttributes, ImmutableArray<CSharpAttributeData>.Empty);
                    containingPEModuleSymbol.LoadCustomAttributes(this.handle, ref this.lazyCustomAttributes);
                }
            }

            Debug.Assert(!this.lazyHiddenAttributes.IsDefault);
            return this.lazyCustomAttributes;
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(ModuleCompilationState compilationState)
        {
            foreach (CSharpAttributeData attribute in GetAttributes())
            {
                yield return attribute;
            }

            // Yield hidden attributes last, order might be important.
            foreach (CSharpAttributeData attribute in lazyHiddenAttributes)
            {
                yield return attribute;
            }
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }
    }
}
