// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Templates.Editorconfig.Command;

internal sealed partial class PackageGuids
{
    public const string AddEditorConfigString = "57a6a527-1255-45c4-bcdb-6a4f50b73b3e";
    public static Guid AddEditorConfig = new Guid(AddEditorConfigString);
    public const string AddEditorConfigCmdSetString = "713dfda7-0c2f-41ee-8326-54895666044a";
    public static readonly Guid AddEditorConfigCmdSet = new(AddEditorConfigCmdSetString);
}

public static class PackageIds
{
    public const int AddGroup = 0x0001;
    public const int AddGroupAnyCode = 0x0002;
    public const int AddEditorConfigFileCommand = 0x0200;
    public const int AddEditorConfigFileAnyCodeCommand = 0x0201;
}
