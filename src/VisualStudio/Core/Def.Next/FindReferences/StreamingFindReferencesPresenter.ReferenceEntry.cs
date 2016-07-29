// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.LanguageServices.FindReferences
{
    internal partial class StreamingFindReferencesPresenter
    {
        private abstract class ReferenceEntry
        {
            public readonly RoslynDefinitionBucket DefinitionBucket;

            protected ReferenceEntry(RoslynDefinitionBucket definitionBucket)
            {
                DefinitionBucket = definitionBucket;
            }

            public bool TryGetValue(string keyName, out object content)
            {
                content = GetValue(keyName);
                return content != null;
            }

            private object GetValue(string keyName)
            {
                switch (keyName)
                {
                case StandardTableKeyNames2.Definition:
                    return DefinitionBucket;

                case StandardTableKeyNames2.DefinitionIcon:
                    return DefinitionBucket.DefinitionItem.Tags.GetGlyph().GetImageMoniker();
                }

                return GetValueWorker(keyName);
            }

            protected abstract object GetValueWorker(string keyName);

            public virtual bool TryCreateColumnContent(string columnName, out FrameworkElement content)
            {
                content = null;
                return false;
            }
        }
    }
}