// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Roslyn.Utilities;
using EmitContext = Microsoft.CodeAnalysis.Emit.EmitContext;

namespace Microsoft.Cci
{
    /// <summary>
    /// A container for static helper methods that are used for manipulating and computing iterators.
    /// </summary>
    internal static class IteratorHelper
    {
        /// <summary>
        /// True if the given enumerable is not null and contains at least one element.
        /// </summary>
        public static bool EnumerableIsNotEmpty<T>([NotNullWhen(returnValue: true)] IEnumerable<T>? enumerable)
        {
            if (enumerable == null)
            {
                return false;
            }

            var asIListT = enumerable as IList<T>;
            if (asIListT != null)
            {
                return asIListT.Count != 0;
            }

            var asIList = enumerable as IList;
            if (asIList != null)
            {
                return asIList.Count != 0;
            }

            return enumerable.GetEnumerator().MoveNext();
        }

        /// <summary>
        /// True if the given enumerable is null or contains no elements
        /// </summary>
        public static bool EnumerableIsEmpty<T>([NotNullWhen(returnValue: false)] IEnumerable<T>? enumerable)
        {
            return !EnumerableIsNotEmpty<T>(enumerable);
        }

        /// <summary>
        /// Returns the number of elements in the given enumerable. A null enumerable is allowed and results in 0.
        /// </summary>
        public static uint EnumerableCount<T>(IEnumerable<T>? enumerable)
        {
            // ^ ensures result >= 0;
            if (enumerable == null)
            {
                return 0;
            }

            var asIListT = enumerable as IList<T>;
            if (asIListT != null)
            {
                return (uint)asIListT.Count;
            }

            var asIList = enumerable as IList;
            if (asIList != null)
            {
                return (uint)asIList.Count;
            }

            uint result = 0;
            IEnumerator<T> enumerator = enumerable.GetEnumerator();
            while (enumerator.MoveNext())
            {
                result++;
            }

            return result & 0x7FFFFFFF;
        }
    }

    /// <summary>
    /// A declarative specification of a security action applied to a set of permissions. Used by the CLR loader to enforce security restrictions.
    /// Each security attribute represents a serialized permission or permission set for a specified security action.
    /// The union of the security attributes with identical security action, define the permission set to which the security action applies.
    /// </summary>
    internal struct SecurityAttribute
    {
        public DeclarativeSecurityAction Action { get; }
        public ICustomAttribute Attribute { get; }

        public SecurityAttribute(DeclarativeSecurityAction action, ICustomAttribute attribute)
        {
            Action = action;
            Attribute = attribute;
        }
    }

    /// <summary>
    /// Information about how values of managed types should be marshalled to and from unmanaged types.
    /// </summary>
    internal interface IMarshallingInformation
    {
        /// <summary>
        /// <see cref="ITypeReference"/> or a string (usually a fully-qualified type name of a type implementing the custom marshaller, but Dev11 allows any string).
        /// </summary>
        object GetCustomMarshaller(EmitContext context);

        /// <summary>
        /// An argument string (cookie) passed to the custom marshaller at run time.
        /// </summary>
        string CustomMarshallerRuntimeArgument
        {
            get;
        }

        /// <summary>
        /// The unmanaged element type of the unmanaged array.
        /// -1 if it should be omitted from the marshal blob.
        /// </summary>
        System.Runtime.InteropServices.UnmanagedType ElementType
        {
            get;
        }

        /// <summary>
        /// Specifies the index of the parameter that contains the value of the Interface Identifier (IID) of the marshalled object.
        /// -1 if it should be omitted from the marshal blob.
        /// </summary>
        int IidParameterIndex
        {
            get;
        }

        /// <summary>
        /// The unmanaged type to which the managed type will be marshalled. This can be UnmanagedType.CustomMarshaler, in which case the unmanaged type
        /// is decided at runtime.
        /// </summary>
        System.Runtime.InteropServices.UnmanagedType UnmanagedType { get; }

        /// <summary>
        /// The number of elements in the fixed size portion of the unmanaged array.
        /// -1 if it should be omitted from the marshal blob.
        /// </summary>
        int NumberOfElements
        {
            get;
        }

        /// <summary>
        /// The zero based index of the parameter in the unmanaged method that contains the number of elements in the variable portion of unmanaged array.
        /// If -1, the variable portion is of size zero, or the caller conveys the size of the variable portion of the array to the unmanaged method in some other way.
        /// </summary>
        short ParamIndex
        {
            get;
        }

        /// <summary>
        /// The type to which the variant values of all elements of the safe array must belong. See also SafeArrayElementUserDefinedSubtype.
        /// (The element type of a safe array is VARIANT. The "sub type" specifies the value of all of the tag fields (vt) of the element values. )
        /// -1 if it should be omitted from the marshal blob.
        /// </summary>
        VarEnum SafeArrayElementSubtype
        {
            get;
        }

        /// <summary>
        /// A reference to the user defined type to which the variant values of all elements of the safe array must belong.
        /// (The element type of a safe array is VARIANT. The tag fields will all be either VT_DISPATCH or VT_UNKNOWN or VT_RECORD.
        /// The "user defined sub type" specifies the type of value the ppdispVal/ppunkVal/pvRecord fields of the element values may point to.)
        /// </summary>
        ITypeReference GetSafeArrayElementUserDefinedSubtype(EmitContext context);
    }

    /// <summary>
    /// Implemented by any entity that has a name.
    /// </summary>
    internal interface INamedEntity
    {
        /// <summary>
        /// The name of the entity.
        /// </summary>
        string? Name { get; }
    }

    /// <summary>
    /// The name of the entity depends on other metadata (tokens, signatures) originated from
    /// PeWriter.
    /// </summary>
    internal interface IContextualNamedEntity : INamedEntity
    {
        /// <summary>
        /// Method must be called before calling INamedEntity.Name.
        /// </summary>
        void AssociateWithMetadataWriter(MetadataWriter metadataWriter);
    }

    /// <summary>
    /// Implemented by an entity that is always a member of a particular parameter list, such as an IParameterDefinition.
    /// Provides a way to determine the position where the entity appears in the parameter list.
    /// </summary>
    internal interface IParameterListEntry
    {
        /// <summary>
        /// The position in the parameter list where this instance can be found.
        /// </summary>
        ushort Index { get; }
    }

    /// <summary>
    /// Information that describes how a method from the underlying Platform is to be invoked.
    /// </summary>
    internal interface IPlatformInvokeInformation
    {
        /// <summary>
        /// Module providing the method/field.
        /// </summary>
        string? ModuleName { get; }

        /// <summary>
        /// Name of the method providing the implementation.
        /// </summary>
        string? EntryPointName { get; }

        /// <summary>
        /// Flags that determine marshalling behavior.
        /// </summary>
        MethodImportAttributes Flags { get; }
    }

    internal class ResourceSection
    {
        internal ResourceSection(byte[] sectionBytes, uint[] relocations)
        {
            RoslynDebug.Assert(sectionBytes != null);
            RoslynDebug.Assert(relocations != null);

            SectionBytes = sectionBytes;
            Relocations = relocations;
        }

        internal readonly byte[] SectionBytes;
        //This is the offset into SectionBytes that should be modified.
        //It should have the section's RVA added to it.
        internal readonly uint[] Relocations;
    }

    /// <summary>
    /// A resource file formatted according to Win32 API conventions and typically obtained from a Portable Executable (PE) file.
    /// See the Win32 UpdateResource method for more details.
    /// </summary>
    internal interface IWin32Resource
    {
        /// <summary>
        /// A string that identifies what type of resource this is. Only valid if this.TypeId &lt; 0.
        /// </summary>
        string TypeName
        {
            get;
            // ^ requires this.TypeId < 0;
        }

        /// <summary>
        /// An integer tag that identifies what type of resource this is. If the value is less than 0, this.TypeName should be used instead.
        /// </summary>
        int TypeId
        {
            get;
        }

        /// <summary>
        /// The name of the resource. Only valid if this.Id &lt; 0.
        /// </summary>
        string Name
        {
            get;
            // ^ requires this.Id < 0; 
        }

        /// <summary>
        /// An integer tag that identifies this resource. If the value is less than 0, this.Name should be used instead.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// The language for which this resource is appropriate.
        /// </summary>
        uint LanguageId { get; }

        /// <summary>
        /// The code page for which this resource is appropriate.
        /// </summary>
        uint CodePage { get; }

        /// <summary>
        /// The data of the resource.
        /// </summary>
        IEnumerable<byte> Data { get; }
    }
}
