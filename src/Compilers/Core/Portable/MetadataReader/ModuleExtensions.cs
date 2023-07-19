// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis
{
    internal static class ModuleExtensions
    {
        private const string VTableGapMethodNamePrefix = "_VtblGap";

        /// <summary>
        /// Returns true if the field should be imported. Visibility
        /// and the value of <paramref name="importOptions"/> are considered
        /// </summary>
        public static bool ShouldImportField(this PEModule module, FieldDefinitionHandle field, MetadataImportOptions importOptions)
        {
            try
            {
                var flags = module.GetFieldDefFlagsOrThrow(field);
                return ShouldImportField(flags, importOptions);
            }
            catch (BadImageFormatException)
            {
                return true;
            }
        }

        /// <summary>
        /// Returns true if the flags represent a field that should be imported.
        /// Visibility and the value of <paramref name="importOptions"/> are considered
        /// </summary>
        public static bool ShouldImportField(FieldAttributes flags, MetadataImportOptions importOptions)
        {
            switch (flags & FieldAttributes.FieldAccessMask)
            {
                case FieldAttributes.Private:
                case FieldAttributes.PrivateScope:
                    return importOptions == MetadataImportOptions.All;

                case FieldAttributes.Assembly:
                    return importOptions >= MetadataImportOptions.Internal;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Returns true if the method should be imported. Returns false for private methods that are not
        /// explicit interface implementations. For other methods, visibility and the value of
        /// <paramref name="importOptions"/> are considered.
        /// </summary>
        public static bool ShouldImportMethod(this PEModule module, TypeDefinitionHandle typeDef, MethodDefinitionHandle methodDef, MetadataImportOptions importOptions)
        {
            try
            {
                var flags = module.GetMethodDefFlagsOrThrow(methodDef);

                // If the method is virtual, it must be accessible, although
                // it may be an explicit (private) interface implementation.
                // Otherwise, we need to check the accessibility.
                if ((flags & MethodAttributes.Virtual) == 0 && !acceptBasedOnAccessibility(importOptions, flags) &&
                    ((flags & MethodAttributes.Static) == 0 || !isMethodImpl(typeDef, methodDef)))
                {

                    return false;
                }
            }
            catch (BadImageFormatException)
            { }

            try
            {
                // As in the native C# compiler (see IMPORTER::ImportMethod), drop any method prefixed
                // with "_VtblGap".  They should be impossible to call/implement/etc.
                // BREAK: The native VB compiler does not drop such methods, but it produces unverifiable
                // code when they are called, so the break is acceptable.
                // TODO: Keep some record of vtable gaps (DevDiv #17472).
                var name = module.GetMethodDefNameOrThrow(methodDef);
                return !name.StartsWith(VTableGapMethodNamePrefix, StringComparison.Ordinal);
            }
            catch (BadImageFormatException)
            {
                return true;
            }

            static bool acceptBasedOnAccessibility(MetadataImportOptions importOptions, MethodAttributes flags)
            {
                switch (flags & MethodAttributes.MemberAccessMask)
                {
                    case MethodAttributes.Private:
                    case MethodAttributes.PrivateScope:
                        if (importOptions != MetadataImportOptions.All)
                        {
                            return false;
                        }

                        break;

                    case MethodAttributes.Assembly:
                        if (importOptions == MetadataImportOptions.Public)
                        {
                            return false;
                        }

                        break;
                }

                return true;
            }

            bool isMethodImpl(TypeDefinitionHandle typeDef, MethodDefinitionHandle methodDef)
            {
                foreach (var methodImpl in module.GetMethodImplementationsOrThrow(typeDef))
                {
                    module.GetMethodImplPropsOrThrow(methodImpl, out EntityHandle body, out _);
                    if (body == methodDef)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Returns 0 if method name doesn't represent a v-table gap.
        /// Otherwise, returns the gap size.
        /// </summary>
        public static int GetVTableGapSize(string emittedMethodName)
        {
            // From IMetaDataEmit::DefineMethod documentation (http://msdn.microsoft.com/en-us/library/ms230861(VS.100).aspx)
            // ----------------------
            // In the case where one or more slots need to be skipped, such as to preserve parity with a COM interface layout, 
            // a dummy method is defined to take up the slot or slots in the v-table; set the dwMethodFlags to the mdRTSpecialName 
            // value of the CorMethodAttr enumeration and specify the name as:
            //
            // _VtblGap<SequenceNumber><_CountOfSlots>
            //
            // where SequenceNumber is the sequence number of the method and CountOfSlots is the number of slots to skip in the v-table. 
            // If CountOfSlots is omitted, 1 is assumed.
            // ----------------------
            //
            // From "Partition II Metadata.doc"
            // ----------------------
            // For COM Interop, an additional class of method names are permitted:
            // _VtblGap<SequenceNumber><_CountOfSlots>
            // where <SequenceNumber> and <CountOfSlots> are decimal numbers
            // ----------------------
            const string prefix = VTableGapMethodNamePrefix;

            if (emittedMethodName.StartsWith(prefix, StringComparison.Ordinal))
            {
                int index;

                // Skip the SequenceNumber
                for (index = prefix.Length; index < emittedMethodName.Length; index++)
                {
                    if (!char.IsDigit(emittedMethodName, index))
                    {
                        break;
                    }
                }

                if (index == prefix.Length ||
                    index >= emittedMethodName.Length - 1 ||
                    emittedMethodName[index] != '_' ||
                    !char.IsDigit(emittedMethodName, index + 1))
                {
                    return 1;
                }

                int countOfSlots;

                if (int.TryParse(emittedMethodName.Substring(index + 1), NumberStyles.None, CultureInfo.InvariantCulture, out countOfSlots)
                    && countOfSlots > 0)
                {
                    return countOfSlots;
                }

                return 1;
            }

            return 0;
        }

        public static string GetVTableGapName(int sequenceNumber, int countOfSlots)
        {
            return string.Format("_VtblGap{0}_{1}", sequenceNumber, countOfSlots);
        }
    }
}
