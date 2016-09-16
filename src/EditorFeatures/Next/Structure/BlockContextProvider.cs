// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Structure;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Structure
{
    [Name(nameof(RoslynBlockContextProvider)), Order]
    [Export(typeof(IBlockContextProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal class RoslynBlockContextProvider : ForegroundThreadAffinitizedObject, IBlockContextProvider
    {
        public Task<IBlockContextSource> TryCreateBlockContextSourceAsync(
            ITextBuffer textBuffer, CancellationToken token)
        {
            this.AssertIsForeground();

            var result = textBuffer.Properties.GetOrCreateSingletonProperty(
                () => new BlockContextSource());
            return Task.FromResult<IBlockContextSource>(result);
        }

        private class BlockContextSource : IBlockContextSource
        {
            public void Dispose()
            {
            }

            public Task<IBlockContext> GetBlockContextAsync(
                IBlockTag blockTag, ITextView view, CancellationToken token)
            {
                if (blockTag is RoslynOutliningRegionTag)
                {
                    var result = new RoslynBlockContext(blockTag, view);
                    return Task.FromResult<IBlockContext>(result);
                }

                return SpecializedTasks.Default<IBlockContext>();
            }
        }

        private class RoslynBlockContext : ForegroundThreadAffinitizedObject, IBlockContext
        {
            public IBlockTag BlockTag { get; }

            public ITextView TextView { get; }

            public RoslynBlockContext(IBlockTag blockTag, ITextView textView)
            {
                BlockTag = blockTag;
                TextView = textView;
            }

            public object Content
            {
                get
                {
                    this.AssertIsForeground();
                    return BlockTag.CollapsedHintForm;
                }
            }
        }
    }
}