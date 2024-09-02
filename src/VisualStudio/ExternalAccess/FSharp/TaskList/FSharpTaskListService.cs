// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.TaskList;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.TaskList
{
    [Shared]
    [ExportLanguageService(typeof(ITaskListService), LanguageNames.FSharp)]
    internal sealed class FSharpTaskListService : ITaskListService
    {
        private readonly IFSharpTaskListService? _impl;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpTaskListService([Import(AllowDefault = true)] IFSharpTaskListService impl)
        {
            _impl = impl;
        }

        public async Task<ImmutableArray<TaskListItem>> GetTaskListItemsAsync(Document document, ImmutableArray<TaskListItemDescriptor> descriptors, CancellationToken cancellationToken)
        {
            if (_impl == null)
                return ImmutableArray<TaskListItem>.Empty;

            var result = await _impl.GetTaskListItemsAsync(
                document,
                descriptors.SelectAsArray(d => new FSharpTaskListDescriptor(d)),
                cancellationToken).ConfigureAwait(false);

            if (result.Length == 0)
                return ImmutableArray<TaskListItem>.Empty;

            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

            return result.SelectAsArray(d =>
            {
                var priority = d.TaskDescriptor.Descriptor.Priority;
                var span = new FileLinePositionSpan(document.FilePath!, text.Lines.GetLinePositionSpan(d.Span));

                return new TaskListItem(priority, d.Message, document.Id, span, span);
            });
        }
    }
}
