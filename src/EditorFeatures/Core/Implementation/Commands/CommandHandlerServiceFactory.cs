// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Commands
{
    /// <summary>
    /// This component manages the lifetimes of command handlers. It is a singleton that is shared
    /// between any components that import it.
    /// </summary>
    [Export(typeof(ICommandHandlerServiceFactory))]
    internal class CommandHandlerServiceFactory : ICommandHandlerServiceFactory
    {
        private readonly IEnumerable<Lazy<ICommandHandler, OrderableContentTypeMetadata>> _commandHandlers;

        [ImportingConstructor]
        public CommandHandlerServiceFactory(
            [ImportMany] IEnumerable<Lazy<ICommandHandler, OrderableContentTypeMetadata>> commandHandlers)
        {
            Contract.ThrowIfNull(commandHandlers);

            _commandHandlers = commandHandlers;
        }

        public void Initialize(string contentTypeName)
        {
            // Perf: Evaluate all the lazy command handlers.
            // This is invoked on the solution idle background task.
            foreach (var lazyCommandHandler in _commandHandlers)
            {
                // here, we just use string comparison. it is cheap and enough since we control these.
                if (!lazyCommandHandler.Metadata.ContentTypes.Any(c => string.Equals(c, contentTypeName)))
                {
                    continue;
                }

                var handler = lazyCommandHandler.Value;
            }
        }

        /// <summary>
        /// Returns a collection of ICommandHandlers that match the appropriate content types for this view.
        /// </summary>
        public CommandHandlerService CreateCollectionForView(ITextView textView)
        {
            var contentTypes = textView.GetContentTypes();
            var handlers = ExtensionOrderer.Order(_commandHandlers.SelectMatchingExtensions(contentTypes));

            return new CommandHandlerService(handlers);
        }

        /// <summary>
        /// Returns a collection of ICommandHandlers that match the appropriate content type of the given buffer.
        /// </summary>
        internal CommandHandlerService CreateCollectionForBuffer(ITextBuffer subjectBuffer)
        {
            var handlers = ExtensionOrderer.Order(_commandHandlers.SelectMatchingExtensions(subjectBuffer.ContentType));

            return new CommandHandlerService(handlers);
        }

        ICommandHandlerService ICommandHandlerServiceFactory.GetService(ITextView textView)
        {
            return CreateCollectionForView(textView);
        }

        ICommandHandlerService ICommandHandlerServiceFactory.GetService(ITextBuffer textBuffer)
        {
            return CreateCollectionForBuffer(textBuffer);
        }
    }
}
