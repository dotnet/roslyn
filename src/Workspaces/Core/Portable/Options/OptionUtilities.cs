// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable RS0030 // Do not used banned APIs: Option<T>

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.Options;

internal static class OptionUtilities
{
    /// <summary>
    /// Allows an option of one enum type to be converted to another enum type, provided that both enums share the same underlying type.
    /// Useful for some cases in Roslyn where we have an existing shipped public option in the Workspace layer, and an internal option
    /// in the CodeStyle layer, and we want to map between them.
    /// </summary>
    public static Option<TToEnum> ConvertEnumOption<TFromEnum, TToEnum, TUnderlyingEnumType>(Option<TFromEnum> option)
        where TFromEnum : struct, Enum
        where TToEnum : struct, Enum
        where TUnderlyingEnumType : struct
    {
        // Ensure that this is only called for enums that are actually compatible with each other.
        Contract.ThrowIfTrue(typeof(TFromEnum).GetEnumUnderlyingType() != typeof(TUnderlyingEnumType));
        Contract.ThrowIfTrue(typeof(TToEnum).GetEnumUnderlyingType() != typeof(TUnderlyingEnumType));

        var definition = option.OptionDefinition;
        var newDefaultValue = ConvertEnum<TFromEnum, TToEnum, TUnderlyingEnumType>(definition.DefaultValue);
        var newSerializer = ConvertEnumSerializer<TFromEnum, TToEnum, TUnderlyingEnumType>(definition.Serializer);

        var newDefinition = new OptionDefinition<TToEnum>(
            defaultValue: newDefaultValue, newSerializer, definition.Group, definition.ConfigName, definition.StorageMapping, definition.IsEditorConfigOption);

        return new(newDefinition, option.Feature, option.Name, option.StorageLocations);
    }

    private static EditorConfigValueSerializer<TToEnum>? ConvertEnumSerializer<TFromEnum, TToEnum, TUnderlyingEnumType>(EditorConfigValueSerializer<TFromEnum> serializer)
        where TFromEnum : struct
        where TToEnum : struct
        where TUnderlyingEnumType : struct
    {
        return new(
            value => ConvertEnum<TFromEnum, TToEnum, TUnderlyingEnumType>(serializer.ParseValue(value)),
            value => serializer.SerializeValue(ConvertEnum<TToEnum, TFromEnum, TUnderlyingEnumType>(value)));
    }

    private static Optional<TToEnum> ConvertEnum<TFromEnum, TToEnum, TUnderlyingEnumType>(Optional<TFromEnum> optional)
        where TFromEnum : struct
        where TToEnum : struct
        where TUnderlyingEnumType : struct
    {
        if (!optional.HasValue)
            return default;

        return ConvertEnum<TFromEnum, TToEnum, TUnderlyingEnumType>(optional.Value);
    }

    private static TToEnum ConvertEnum<TFromEnum, TToEnum, TUnderlyingEnumType>(TFromEnum value)
        where TFromEnum : struct
        where TToEnum : struct
        where TUnderlyingEnumType : struct
    {
        return Unsafe.As<TUnderlyingEnumType, TToEnum>(ref Unsafe.As<TFromEnum, TUnderlyingEnumType>(ref value));
    }
}
