// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal static class PredefinedChangedEventKinds
    {
        public const string CaretPositionChanged = "CaretPositionChanged";
        public const string CompletionClosed = "CompletionClosed";
        public const string DiagnosticsChanged = "DiagnosticsChanged";
        public const string DocumentActiveContextChanged = "DocumentActiveContextChanged";
        public const string OptionChanged = "OptionChanged";
        public const string ParseOptionChanged = "ParseOptionChanged";
        public const string ReadOnlyRegionsChanged = "ReadOnlyRegionsChanged";
        public const string RenameSessionChanged = "RenameSessionChanged";
        public const string SelectionChanged = "SelectionChanged";
        public const string SemanticsChanged = "SemanticsChanged";
        public const string TaggerCreated = "TaggerCreated";
        public const string TextChanged = "TextChanged";
        public const string WorkspaceRegistrationChanged = "WorkspaceRegistrationChanged";
    }
}
