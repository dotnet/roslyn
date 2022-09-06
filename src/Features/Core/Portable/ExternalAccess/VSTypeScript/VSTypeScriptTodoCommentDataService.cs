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
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.TodoComments;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    [ExportLanguageService(typeof(ITodoCommentDataService), InternalLanguageNames.TypeScript), Shared]
    internal sealed class VSTypeScriptTodoCommentDataService : ITodoCommentDataService
    {
        private readonly IVSTypeScriptTodoCommentDataServiceImplementation? _impl;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VSTypeScriptTodoCommentDataService([Import(AllowDefault = true)] IVSTypeScriptTodoCommentDataServiceImplementation impl)
        {
            _impl = impl;
        }

        public async Task<ImmutableArray<TodoCommentData>> GetTodoCommentDataAsync(Document document, ImmutableArray<TodoCommentDescriptor> commentDescriptors, CancellationToken cancellationToken)
        {
            if (_impl is null)
                return ImmutableArray<TodoCommentData>.Empty;

            var result = await _impl.GetTodoCommentDataAsync(
                document,
                commentDescriptors.SelectAsArray(d => new VSTypeScriptTodoCommentDescriptor(d)),
                cancellationToken).ConfigureAwait(false);
            if (result.Length == 0)
                return ImmutableArray<TodoCommentData>.Empty;

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            return result.SelectAsArray(d =>
            {
                var textSpan = new TextSpan(Math.Min(text.Length, Math.Max(0, d.Position)), 0);
                var location = Location.Create(document.FilePath!, textSpan, text.Lines.GetLinePositionSpan(textSpan));
                var span = location.GetLineSpan();

                return new TodoCommentData(d.Descriptor.Descriptor.Priority, d.Message, document.Id, span, span);
            });
        }
    }
}
