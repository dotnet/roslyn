// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

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

