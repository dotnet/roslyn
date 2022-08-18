// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeStyle;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditorConfigSettings
{
    internal partial class EditorConfigSettingsValueHolder
    {
        private static readonly BidirectionalMap<string, bool> UseTabsMap =
           new(new[]
           {
                KeyValuePairUtil.Create("tab", true),
                KeyValuePairUtil.Create("space", false),
           });

        private static readonly BidirectionalMap<string, string> NewLineMap =
           new(new[]
           {
                KeyValuePairUtil.Create("lf", "\n"),
                KeyValuePairUtil.Create("cr", "\r"),
                KeyValuePairUtil.Create("crlf", "\r\n"),
           });

        private static readonly BidirectionalMap<string, OperatorPlacementWhenWrappingPreference> OperatorPlacementWhenWrappingMap =
            new(new[]
            {
                KeyValuePairUtil.Create("end_of_line", OperatorPlacementWhenWrappingPreference.EndOfLine),
                KeyValuePairUtil.Create("beginning_of_line", OperatorPlacementWhenWrappingPreference.BeginningOfLine),
            });

        public static EditorConfigData<bool> UseTabs = new BooleanEditorConfigData("indent_style", CompilerExtensionsResources.Use_Tabs, UseTabsMap);
        public static EditorConfigData<int> TabSize = new IntegerEditorConfigData("tab_width", CompilerExtensionsResources.Tab_Size);
        public static EditorConfigData<int> IndentationSize = new IntegerEditorConfigData("indent_size", CompilerExtensionsResources.Indentation_Size);
        public static EditorConfigData<string> NewLine = new StringEditorConfigData("end_of_line", CompilerExtensionsResources.New_Line, NewLineMap, "unset", Environment.NewLine);
        public static EditorConfigData<bool> InsertFinalNewLine = new BooleanEditorConfigData("insert_final_newline", CompilerExtensionsResources.Insert_Final_Newline);
        public static EditorConfigData<OperatorPlacementWhenWrappingPreference> OperatorPlacementWhenWrapping = new EnumEditorConfigData<OperatorPlacementWhenWrappingPreference>("dotnet_style_operator_placement_when_wrapping", CompilerExtensionsResources.Operator_placement_when_wrapping, OperatorPlacementWhenWrappingMap);
    }
}
