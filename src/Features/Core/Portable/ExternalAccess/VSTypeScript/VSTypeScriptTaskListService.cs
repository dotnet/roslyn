// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.TaskList;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

[ExportLanguageService(typeof(ITaskListService), InternalLanguageNames.TypeScript), Shared]
internal sealed class VSTypeScriptTaskListService : ITaskListService
{
    private readonly IVSTypeScriptTaskListServiceImplementation? _impl;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VSTypeScriptTaskListService([Import(AllowDefault = true)] IVSTypeScriptTaskListServiceImplementation impl)
    {
        _impl = impl;
    }

    public async Task<ImmutableArray<TaskListItem>> GetTaskListItemsAsync(Document document, ImmutableArray<TaskListItemDescriptor> descriptors, CancellationToken cancellationToken)
    {
        if (_impl is null)
            return ImmutableArray<TaskListItem>.Empty;

        var result = await _impl.GetTaskListItemsAsync(
            document,
            descriptors.SelectAsArray(d => new VSTypeScriptTaskListItemDescriptorWrapper(d)),
            cancellationToken).ConfigureAwait(false);
        if (result.Length == 0)
            return ImmutableArray<TaskListItem>.Empty;

        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

        return result.SelectAsArray(d =>
        {
            var textSpan = new TextSpan(Math.Min(text.Length, Math.Max(0, d.Position)), 0);
            var location = Location.Create(document.FilePath!, textSpan, text.Lines.GetLinePositionSpan(textSpan));
            var span = location.GetLineSpan();

            return new TaskListItem(d.Descriptor.Descriptor.Priority, d.Message, document.Id, span, span);
        });
    }
}
