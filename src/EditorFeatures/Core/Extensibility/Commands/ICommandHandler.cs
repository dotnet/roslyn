// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Editor
{
    /// <summary>
    /// This interface is implemented by a class that implements at least one strongly-typed
    /// ICommandHandler&lt;T&gt;. When implementing a strongly-typed ICommandHandler, you should MEF
    /// export this base interface.
    /// </summary>
    internal interface ICommandHandler
    {
    }

    /// <summary>
    /// Implement to handle a command before it is processed by the editor. To export this, export
    /// the non-generic ICommandHandler.
    /// </summary>
    internal interface ICommandHandler<T> : ICommandHandler where T : CommandArgs
    {
        /// <summary>
        /// Called to determine the state of the command.
        /// </summary>
        /// <param name="args">The arguments of the command, which contains data about the event
        /// that fired.</param>
        /// <param name="nextHandler">A delegate which calls the next command handler in the chain.
        /// Every command handler must invoke this delegate if they do not wish to fully handle the
        /// command themselves.</param>
        /// <returns>Return a CommandState instance.</returns>
        CommandState GetCommandState(T args, Func<CommandState> nextHandler);

        /// <summary>
        /// Called when the command is executed.
        /// </summary>
        /// <param name="args">The arguments of the command, which contains data about the event
        /// that fired.</param>
        /// <param name="nextHandler">A delegate which calls the next handler in the chain. Every
        /// command handler must invoke this delegate if they do not wish to fully handle the
        /// command themselves.</param>
        void ExecuteCommand(T args, Action nextHandler);
    }
}
