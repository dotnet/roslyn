// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Commands
{
    [AttributeUsage(AttributeTargets.Class), MetadataAttribute]
    internal class LspCommandAttribute : LspMethodAttribute, ILspCommandMetadata
    {
        /// <summary>
        /// The name of the command to execute for a <see cref="Methods.WorkspaceExecuteCommandName"/> request.
        /// </summary>
        public string CommandName { get; }

        public LspCommandAttribute(string commandName, bool mutatesSolutionState) : base(Methods.WorkspaceExecuteCommandName, mutatesSolutionState)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                throw new ArgumentException(nameof(commandName));
            }

            CommandName = commandName;
        }

        public static string GetRequestNameForCommand(string commandName)
        {
            return $"{Methods.WorkspaceExecuteCommandName}/{commandName}";
        }
    }
}
