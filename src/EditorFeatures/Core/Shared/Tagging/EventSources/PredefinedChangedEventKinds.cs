// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal static class PredefinedChangedEventKinds
    {
        public const string CaretPositionChanged = nameof(CaretPositionChanged);
        public const string CompletionClosed = nameof(CompletionClosed);
        public const string DiagnosticsChanged = nameof(DiagnosticsChanged);
        public const string DocumentActiveContextChanged = nameof(DocumentActiveContextChanged);
        public const string OptionChanged = nameof(OptionChanged);
        public const string ParseOptionChanged = nameof(ParseOptionChanged);
        public const string ReadOnlyRegionsChanged = nameof(ReadOnlyRegionsChanged);
        public const string RenameSessionChanged = nameof(RenameSessionChanged);
        public const string SelectionChanged = nameof(SelectionChanged);
        public const string SemanticsChanged = nameof(SemanticsChanged);
        public const string TaggerCreated = nameof(TaggerCreated);
        public const string TextChanged = nameof(TextChanged);
        public const string WorkspaceRegistrationChanged = nameof(WorkspaceRegistrationChanged);
    }
}
