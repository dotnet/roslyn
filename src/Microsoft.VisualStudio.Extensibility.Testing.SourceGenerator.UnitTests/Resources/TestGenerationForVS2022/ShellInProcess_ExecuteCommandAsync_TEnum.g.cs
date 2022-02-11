// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System;
    using System.Threading;
    using Microsoft.VisualStudio.Editor;
    using Task = System.Threading.Tasks.Task;

    internal partial class ShellInProcess
    {
        public Task ExecuteCommandAsync<TEnum>(TEnum command, CancellationToken cancellationToken)
            where TEnum : struct, Enum
        {
            var commandGuid = command switch
            {
                EditorConstants.EditorCommandID => EditorConstants.EditorCommandSet,
                _ => typeof(TEnum).GUID,
            };

            return ExecuteCommandAsync(commandGuid, Convert.ToUInt32(command), cancellationToken);
        }
    }
}
