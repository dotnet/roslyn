// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.CodeAnalysis.GenerateType
{
    [Flags]
    internal enum TypeKindOptions
    {
        None = 0x0,

        Class = 0x1,
        Structure = 0x2,
        Interface = 0x4,
        Enum = 0x8,
        Delegate = 0x10,
        Module = 0x20,

        // Enables class, struct, interface, enum and delegate
        AllOptions = Class | Structure | Interface | Enum | Delegate,

        // Only class is valid with Attribute
        Attribute = Class,

        // Only class, struct and interface are allowed. No Enums
        BaseList = Class | Interface,

        AllOptionsWithModule = AllOptions | Module,

        // Only Interface and Delegate cannot be part of the member access with Namespace as Left expression
        MemberAccessWithNamespace = Class | Structure | Enum | Module,

        // Enum and Modules are incompatible with Generics
        GenericInCompatibleTypes = Enum | Module
    }

    internal class TypeKindOptionsHelper
    {
        public static bool IsClass(TypeKindOptions option)
            => (option & TypeKindOptions.Class) != 0 ? true : false;

        public static bool IsStructure(TypeKindOptions option)
            => (option & TypeKindOptions.Structure) != 0 ? true : false;

        public static bool IsInterface(TypeKindOptions option)
            => (option & TypeKindOptions.Interface) != 0 ? true : false;

        public static bool IsEnum(TypeKindOptions option)
            => (option & TypeKindOptions.Enum) != 0 ? true : false;

        public static bool IsDelegate(TypeKindOptions option)
            => (option & TypeKindOptions.Delegate) != 0 ? true : false;

        public static bool IsModule(TypeKindOptions option)
            => (option & TypeKindOptions.Module) != 0 ? true : false;

        public static TypeKindOptions RemoveOptions(TypeKindOptions fromValue, params TypeKindOptions[] removeValues)
        {
            var tempReturnValue = fromValue;
            foreach (var removeValue in removeValues)
            {
                tempReturnValue &= ~removeValue;
            }

            return tempReturnValue;
        }

        internal static TypeKindOptions AddOption(TypeKindOptions toValue, TypeKindOptions addValue)
            => toValue | addValue;
    }
}
