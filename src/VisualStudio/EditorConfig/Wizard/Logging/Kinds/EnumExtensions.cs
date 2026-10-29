// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Kinds;

internal static class EnumExtensions
{
    public static int AsInt(this EventId id)
        => 1000 + (int)id;

    public static int AsInt(this OperationId id)
        => 2000 + (int)id;

    public static int AsInt(this UserTask id)
        => 3000 + (int)id;
}
