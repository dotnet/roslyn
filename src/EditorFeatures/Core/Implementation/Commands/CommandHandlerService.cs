// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Commands
{
    /// <summary>
    /// A service representing a handlers of command handlers for a view or buffer.
    /// </summary>
    internal class CommandHandlerService : ICommandHandlerService
    {
        private readonly IEnumerable<Lazy<ICommandHandler, OrderableContentTypeMetadata>> _commandHandlers;

        /// <summary>
        /// This dictionary acts as a cache so we can avoid having to look through the full list of
        /// handlers every time we need handlers of a specific type, for a given content type. The
        /// value of each key is a class of type List&lt;ICommandHandler&lt;T&gt;&gt;, but since
        /// there is no way to express that in a generic way under .NET I must simply use "object"
        /// as the type associated with each key.
        /// </summary>
        private readonly Dictionary<Tuple<Type, string>, object> _commandHandlersByTypeAndContentType;

        public CommandHandlerService(IList<Lazy<ICommandHandler, OrderableContentTypeMetadata>> list)
        {
            _commandHandlers = list;
            _commandHandlersByTypeAndContentType = new Dictionary<Tuple<Type, string>, object>();
        }

        /// <summary>
        /// Returns a list of ICommandHandlers of a given type that apply to a given content type.
        /// The result is cached so repeated calls are fast.
        /// </summary>
        private IList<ICommandHandler<T>> GetHandlers<T>(IContentType contentType) where T : CommandArgs
        {
            Contract.ThrowIfFalse(contentType != null);

            var key = Tuple.Create(typeof(T), contentType.TypeName);
            object commandHandlerList = null;

            if (!_commandHandlersByTypeAndContentType.TryGetValue(key, out commandHandlerList))
            {
                var stronglyTypedHandlers = from handler in _commandHandlers
                                            where handler.Value is ICommandHandler<T>
                                            where handler.Metadata.ContentTypes.Any(contentType.IsOfType)
                                            select handler.Value as ICommandHandler<T>;
                commandHandlerList = new List<ICommandHandler<T>>(stronglyTypedHandlers);
                _commandHandlersByTypeAndContentType.Add(key, commandHandlerList);
            }

            return (IList<ICommandHandler<T>>)commandHandlerList;
        }

        CommandState ICommandHandlerService.GetCommandState<T>(IContentType contentType, T args, Func<CommandState> lastHandler)
        {
            using (Logger.LogBlock(FunctionId.CommandHandler_GetCommandState, CancellationToken.None))
            {
                return GetCommandState(GetHandlers<T>(contentType), args, lastHandler);
            }
        }

        void ICommandHandlerService.Execute<T>(IContentType contentType, T args, Action lastHandler)
        {
            using (Logger.LogBlock(FunctionId.CommandHandler_ExecuteHandlers, CancellationToken.None))
            {
                ExecuteHandlers(GetHandlers<T>(contentType), args, lastHandler);
            }
        }

        /// <summary>
        /// Executes the list of command handlers in order, starting at index, passing args to each
        /// one. If all handlers choose to call the nextHandler lambda, the lastHandler lambda is
        /// called.
        /// </summary>
        private static void ExecuteHandlers<T>(IList<ICommandHandler<T>> commandHandlers, T args, Action lastHandler) where T : CommandArgs
        {
            Contract.ThrowIfNull(commandHandlers);

            if (commandHandlers.Count > 0)
            {
                // Build up chain of handlers.
                var handlerChain = lastHandler ?? delegate { };
                for (int i = commandHandlers.Count - 1; i >= 1; i--)
                {
                    // Declare locals to ensure that we don't end up capturing the wrong thing
                    var nextHandler = handlerChain;
                    int j = i;
                    handlerChain = () => commandHandlers[j].ExecuteCommand(args, nextHandler);
                }

                // Kick off the first command handler.
                commandHandlers[0].ExecuteCommand(args, handlerChain);
            }
            else
            {
                // If there aren't any command handlers, just invoke the last handler (if there is one).
                lastHandler?.Invoke();
            }
        }

        /// <summary>
        /// Executes the list of command handlers in order, starting at index, passing args to each
        /// one. If all handlers choose to call the nextHandler lambda, the lastHandler lambda is
        /// called.
        /// </summary>
        private static CommandState GetCommandState<TArgs>(
            IList<ICommandHandler<TArgs>> commandHandlers,
            TArgs args,
            Func<CommandState> lastHandler) where TArgs : CommandArgs
        {
            Contract.ThrowIfNull(commandHandlers);

            if (commandHandlers.Count > 0)
            {
                // Build up chain of handlers.
                var handlerChain = lastHandler ?? delegate { return default(CommandState); };
                for (int i = commandHandlers.Count - 1; i >= 1; i--)
                {
                    // Declare locals to ensure that we don't end up capturing the wrong thing
                    var nextHandler = handlerChain;
                    int j = i;
                    handlerChain = () => commandHandlers[j].GetCommandState(args, nextHandler);
                }

                // Kick off the first command handler.
                return commandHandlers[0].GetCommandState(args, handlerChain);
            }
            else if (lastHandler != null)
            {
                // If there aren't any command handlers, just invoke the last handler (if there is one).
                return lastHandler();
            }

            return default(CommandState);
        }
    }
}
