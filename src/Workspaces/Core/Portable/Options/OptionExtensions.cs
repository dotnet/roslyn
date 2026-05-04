// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable RS0030 // Do not used banned APIs: Option<T>

using System;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Options;

internal static class OptionExtensions
{
    /// <summary>
    /// Allows an option of one enum type to be converted to another enum type, provided that both enums share the same underlying type.
    /// Useful for some cases in Roslyn where we have an existing shipped public option in the Workspace layer, and an internal option
    /// in the CodeStyle layer, and we want to map between them.
    /// </summary>
    public static Option<TToEnum> ConvertEnumOption<TFromEnum, TToEnum>(this Option<TFromEnum> option)
        where TFromEnum : struct, Enum
        where TToEnum : struct, Enum
    {
        var definition = option.OptionDefinition;
        var newDefaultValue = EnumValueUtilities.ConvertEnum<TFromEnum, TToEnum>(definition.DefaultValue);
        var newSerializer = EditorConfigValueSerializer.ConvertEnumSerializer<TFromEnum, TToEnum>(definition.Serializer);

        var newDefinition = new OptionDefinition<TToEnum>(
            defaultValue: newDefaultValue, newSerializer, definition.Group, definition.ConfigName, definition.StorageMapping, definition.IsEditorConfigOption);

        return new(newDefinition, option.Feature, option.Name, option.StorageLocations);
    }
}
