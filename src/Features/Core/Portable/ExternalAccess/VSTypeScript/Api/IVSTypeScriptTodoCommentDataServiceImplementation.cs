// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.TodoComments;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal readonly struct VSTypeScriptTodoCommentData
    {
        public VSTypeScriptTodoCommentData(VSTypeScriptTodoCommentDescriptorWrapper descriptor, string message, int position)
        {
            Descriptor = descriptor;
            Message = message;
            Position = position;
        }

        public VSTypeScriptTodoCommentDescriptorWrapper Descriptor { get; }
        public string Message { get; }
        public int Position { get; }
    }

    internal readonly struct VSTypeScriptTodoCommentDescriptorWrapper
    {
        internal readonly TodoCommentDescriptor Descriptor;

        public string Text => Descriptor.Text;

        internal VSTypeScriptTodoCommentDescriptorWrapper(TodoCommentDescriptor descriptor)
        {
            Descriptor = descriptor;
        }

        public static ImmutableArray<VSTypeScriptTodoCommentDescriptorWrapper> Parse(ImmutableArray<string> items)
            => TodoCommentDescriptor.Parse(items).SelectAsArray(d => new VSTypeScriptTodoCommentDescriptorWrapper(d));
    }

    internal interface IVSTypeScriptTodoCommentDataServiceImplementation
    {
        Task<ImmutableArray<VSTypeScriptTodoCommentData>> GetTodoCommentDataAsync(
            Document document, ImmutableArray<VSTypeScriptTodoCommentDescriptorWrapper> value, CancellationToken cancellationToken);
    }
}
