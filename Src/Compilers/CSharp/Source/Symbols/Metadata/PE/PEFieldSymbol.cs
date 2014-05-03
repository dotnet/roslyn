// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.DocumentationComments;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Roslyn.Utilities;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    /// <summary>
    /// The class to represent all fields imported from a PE/module.
    /// </summary>
    internal sealed class PEFieldSymbol : FieldSymbol
    {
        private readonly FieldHandle handle;
        private readonly string name;
        private readonly FieldAttributes flags;
        private readonly PENamedTypeSymbol containingType;
        private bool lazyIsVolatile;
        private ImmutableArray<CSharpAttributeData> lazyCustomAttributes;
        private ImmutableArray<CustomModifier> lazyCustomModifiers;
        private ConstantValue lazyConstantValue = Microsoft.CodeAnalysis.ConstantValue.Unset; // Indicates an uninitialized ConstantValue
        private Tuple<CultureInfo, string> lazyDocComment;
        private DiagnosticInfo lazyUseSiteDiagnostic = CSDiagnosticInfo.EmptyErrorInfo; // Indicates unknown state. 

        private ObsoleteAttributeData lazyObsoleteAttributeData = ObsoleteAttributeData.Uninitialized;

        private TypeSymbol lazyType;
        private int lazyFixedSize;
        private NamedTypeSymbol lazyFixedImplementationType;

        internal PEFieldSymbol(
            PEModuleSymbol moduleSymbol,
            PENamedTypeSymbol containingType,
            FieldHandle fieldDef)
        {
            Debug.Assert((object)moduleSymbol != null);
            Debug.Assert((object)containingType != null);
            Debug.Assert(!fieldDef.IsNil);

            this.handle = fieldDef;
            this.containingType = containingType;

            try
            {
                moduleSymbol.Module.GetFieldDefPropsOrThrow(fieldDef, out this.name, out this.flags);
            }
            catch (BadImageFormatException)
            {
                if ((object)this.name == null)
                {
                    this.name = String.Empty;
                }

                lazyUseSiteDiagnostic = new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this);
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return containingType;
            }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return this.containingType;
            }
        }

        public override string Name
        {
            get
            {
                return name;
            }
        }

        internal FieldAttributes Flags
        {
            get
            {
                return flags;
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                return (flags & FieldAttributes.SpecialName) != 0;
            }
        }

        internal override bool HasRuntimeSpecialName
        {
            get
            {
                return (flags & FieldAttributes.RTSpecialName) != 0;
            }
        }

        internal override bool IsNotSerialized
        {
            get
            {
                return (flags & FieldAttributes.NotSerialized) != 0;
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

        internal override bool IsMarshalledExplicitly
        {
            get
            {
                return ((flags & FieldAttributes.HasFieldMarshal) != 0);
            }
        }

        internal override UnmanagedType MarshallingType
        {
            get
            {
                if ((flags & FieldAttributes.HasFieldMarshal) == 0)
                {
                    return 0;
                }

                return this.containingType.ContainingPEModule.Module.GetMarshallingType(this.handle);
            }
        }

        internal override ImmutableArray<byte> MarshallingDescriptor
        {
            get
            {
                if ((flags & FieldAttributes.HasFieldMarshal) == 0)
                {
                    return default(ImmutableArray<byte>);
                }

                return this.containingType.ContainingPEModule.Module.GetMarshallingDescriptor(this.handle);
            }
        }

        internal override int? TypeLayoutOffset
        {
            get
            {
                return this.containingType.ContainingPEModule.Module.GetFieldOffset(this.handle);
            }
        }

        internal FieldHandle Handle
        {
            get
            {
                return this.handle;
            }
        }

        private void EnsureSignatureIsLoaded()
        {
            if ((object)lazyType == null)
            {
                var moduleSymbol = this.containingType.ContainingPEModule;
                bool isVolatile;
                ImmutableArray<MetadataDecoder.ModifierInfo> customModifiers;
                TypeSymbol type = (new MetadataDecoder(moduleSymbol, this.containingType)).DecodeFieldSignature(this.handle, out isVolatile, out customModifiers);
                ImmutableArray<CustomModifier> customModifiersArray = CSharpCustomModifier.Convert(customModifiers);
                type = DynamicTypeDecoder.TransformType(type, customModifiersArray.Length, this.handle, moduleSymbol);
                this.lazyIsVolatile = isVolatile;

                TypeSymbol fixedElementType;
                int fixedSize;
                if (customModifiersArray.IsEmpty && IsFixedBuffer(out fixedSize, out fixedElementType))
                {
                    this.lazyFixedSize = fixedSize;
                    this.lazyFixedImplementationType = type as NamedTypeSymbol;
                    type = new PointerTypeSymbol(fixedElementType);
                }

                ImmutableInterlocked.InterlockedCompareExchange(ref lazyCustomModifiers, customModifiersArray, default(ImmutableArray<CustomModifier>));
                Interlocked.CompareExchange(ref lazyType, type, null);
            }
        }

        private bool IsFixedBuffer(out int fixedSize, out TypeSymbol fixedElementType)
        {
            fixedSize = 0;
            fixedElementType = null;

            string elementTypeName;
            int bufferSize;
            PEModuleSymbol containingPEModule = this.ContainingPEModule;
            if (containingPEModule.Module.HasFixedBufferAttribute(this.handle, out elementTypeName, out bufferSize))
            {
                var decoder = new MetadataDecoder(containingPEModule);
                var elementType = decoder.GetTypeSymbolForSerializedType(elementTypeName);
                if (elementType.FixedBufferElementSizeInBytes() != 0)
                {
                    fixedSize = bufferSize;
                    fixedElementType = elementType;
                    return true;
                }
            }

            return false;
        }

        private PEModuleSymbol ContainingPEModule
        {
            get
            {
                return ((PENamespaceSymbol)ContainingNamespace).ContainingPEModule;
            }
        }

        internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            EnsureSignatureIsLoaded();
            return lazyType;
        }

        public override bool IsFixed
        {
            get
            {
                EnsureSignatureIsLoaded();
                return (object)lazyFixedImplementationType != null;
            }
        }

        public override int FixedSize
        {
            get
            {
                EnsureSignatureIsLoaded();
                return lazyFixedSize;
            }
        }

        internal override NamedTypeSymbol FixedImplementationType(PEModuleBuilder emitModule)
        {
            EnsureSignatureIsLoaded();
            return lazyFixedImplementationType;
        }

        public override ImmutableArray<CustomModifier> CustomModifiers
        {
            get
            {
                EnsureSignatureIsLoaded();
                return lazyCustomModifiers;
            }
        }

        public override Symbol AssociatedSymbol
        {
            get
            {
                return null;
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                return (flags & FieldAttributes.InitOnly) != 0;
            }
        }

        public override bool IsVolatile
        {
            get
            {
                EnsureSignatureIsLoaded();
                return lazyIsVolatile;
            }
        }

        public override bool IsConst
        {
            get
            {
                return (flags & FieldAttributes.Literal) != 0 || GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false) != null;
            }
        }

        internal override ConstantValue GetConstantValue(ConstantFieldsInProgress inProgress, bool earlyDecodingWellKnownAttributes)
        {
            if (lazyConstantValue == Microsoft.CodeAnalysis.ConstantValue.Unset)
            {
                ConstantValue value = null;

                if ((flags & FieldAttributes.Literal) != 0)
                {
                    value = this.containingType.ContainingPEModule.Module.GetConstantFieldValue(this.handle);
                }

                // If this is a Decimal, the constant value may come from DecimalConstantAttribute

                if (this.Type.SpecialType == SpecialType.System_Decimal)
                {
                    ConstantValue defaultValue;

                    if (this.containingType.ContainingPEModule.Module.HasDecimalConstantAttribute(Handle, out defaultValue))
                    {
                        value = defaultValue;
                    }
                }

                Interlocked.CompareExchange(
                    ref lazyConstantValue,
                    value,
                    Microsoft.CodeAnalysis.ConstantValue.Unset);
            }

            return lazyConstantValue;
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return containingType.ContainingPEModule.MetadataLocation.Cast<MetadataLocation, Location>();
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                var access = Accessibility.Private;

                switch (this.flags & FieldAttributes.FieldAccessMask)
                {
                    case FieldAttributes.Assembly:
                        access = Accessibility.Internal;
                        break;

                    case FieldAttributes.FamORAssem:
                        access = Accessibility.ProtectedOrInternal;
                        break;

                    case FieldAttributes.FamANDAssem:
                        access = Accessibility.ProtectedAndInternal;
                        break;

                    case FieldAttributes.Private:
                    case FieldAttributes.PrivateScope:
                        access = Accessibility.Private;
                        break;

                    case FieldAttributes.Public:
                        access = Accessibility.Public;
                        break;

                    case FieldAttributes.Family:
                        access = Accessibility.Protected;
                        break;

                    default:
                        Debug.Assert(false, "Unexpected!!!");
                        break;
                }

                return access;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return (flags & FieldAttributes.Static) != 0;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            if (this.lazyCustomAttributes.IsDefault)
            {
                var containingPEModuleSymbol = (PEModuleSymbol)this.ContainingModule;

                if (FilterOutDecimalConstantAttribute())
                {
                    // filter out DecimalConstantAttribute
                    CustomAttributeHandle ignore1;
                    CustomAttributeHandle ignore2;
                    var attributes = containingPEModuleSymbol.GetCustomAttributesForToken(
                        this.handle,
                        out ignore1,
                        AttributeDescription.DecimalConstantAttribute,
                        out ignore2,
                        default(AttributeDescription));

                    ImmutableInterlocked.InterlockedInitialize(ref this.lazyCustomAttributes, attributes);
                }
                else
                {
                    containingPEModuleSymbol.LoadCustomAttributes(this.handle, ref this.lazyCustomAttributes);
                }
            }
            return this.lazyCustomAttributes;
        }

        private bool FilterOutDecimalConstantAttribute()
        {
            ConstantValue value;
            return this.Type.SpecialType == SpecialType.System_Decimal &&
                   (object)(value = GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false)) != null &&
                   value.Discriminator == ConstantValueTypeDiscriminator.Decimal;
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(ModuleCompilationState compilationState)
        {
            foreach (CSharpAttributeData attribute in GetAttributes())
            {
                yield return attribute;
            }

            // Yield hidden attributes last, order might be important.
            if (FilterOutDecimalConstantAttribute())
            {
                var containingPEModuleSymbol = this.containingType.ContainingPEModule;
                yield return new PEAttributeData(containingPEModuleSymbol,
                                          containingPEModuleSymbol.Module.FindLastTargetAttribute(this.handle, AttributeDescription.DecimalConstantAttribute).Handle);
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return PEDocumentationCommentUtils.GetDocumentationComment(this, containingType.ContainingPEModule, preferredCulture, cancellationToken, ref lazyDocComment);
        }

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            if (ReferenceEquals(lazyUseSiteDiagnostic, CSDiagnosticInfo.EmptyErrorInfo))
            {
                DiagnosticInfo result = null;
                CalculateUseSiteDiagnostic(ref result);
                lazyUseSiteDiagnostic = result;
            }

            return lazyUseSiteDiagnostic;
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                ObsoleteAttributeHelpers.InitializeObsoleteDataFromMetadata(ref lazyObsoleteAttributeData, this.handle, (PEModuleSymbol)(this.ContainingModule));
                return lazyObsoleteAttributeData;
            }
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }
    }
}