// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Microsoft.CodeAnalysis.Options
{
    public sealed class EditorConfigFileOptionsProvider : FileOptionsProvider
    {
        public ImmutableArray<AdditionalText> EditorConfigFiles { get; }

        public EditorConfigFileOptionsProvider(ImmutableArray<AdditionalText> editorConfigFiles)
        {
            EditorConfigFiles = editorConfigFiles.NullToEmpty();
        }

        public EditorConfigFileOptionsProvider AddEditorConfigFiles(IEnumerable<AdditionalText> editorConfigFiles)
        {
            return new EditorConfigFileOptionsProvider(this.EditorConfigFiles.AddRange(editorConfigFiles));
        }

        public EditorConfigFileOptionsProvider RemoveEditorConfigFiles(IEnumerable<AdditionalText> editorConfigFiles)
        {
            return new EditorConfigFileOptionsProvider(this.EditorConfigFiles.RemoveRange(editorConfigFiles));
        }

        public override OptionSet GetOptionsForSyntaxTreePath(string path)
        {
            // PROTOTYPE: implement.
            return new EditorConfigOptionSet();
        }

        private class EditorConfigOptionSet : OptionSet
        {
            public override object GetOption(OptionKey optionKey)
            {
                // PROTOTYPE: implement.
                return optionKey.Option.DefaultValue;
            }

            public override OptionSet WithChangedOption(OptionKey optionAndLanguage, object value)
            {
                // PROTOTYPE: implement.
                throw new NotImplementedException();
            }
        }
    }
}
