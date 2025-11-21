// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System;
    using System.Threading;
    using Task = System.Threading.Tasks.Task;

    internal partial class ShellInProcess
    {
        public Task ExecuteCommandAsync<TEnum>(TEnum command, CancellationToken cancellationToken)
            where TEnum : struct, Enum
        {
            return ExecuteCommandAsync(typeof(TEnum).GUID, Convert.ToUInt32(command), cancellationToken);
        }

        public Task ExecuteCommandAsync<TEnum>(TEnum command, string argument, CancellationToken cancellationToken)
            where TEnum : struct, Enum
        {
            return ExecuteCommandAsync(typeof(TEnum).GUID, Convert.ToUInt32(command), argument, cancellationToken);
        }
    }
}
