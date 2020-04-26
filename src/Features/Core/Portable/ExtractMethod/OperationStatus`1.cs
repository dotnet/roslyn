// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    /// <summary>
    /// operation status paired with data
    /// </summary>
    internal class OperationStatus<T>
    {
        public OperationStatus(OperationStatus status, T data)
        {
            Status = status;
            Data = data;
        }

        public OperationStatus Status { get; }
        public T Data { get; }

        public OperationStatus<T> With(OperationStatus status)
            => new OperationStatus<T>(status, Data);

        public OperationStatus<TNew> With<TNew>(TNew data)
            => new OperationStatus<TNew>(Status, data);
    }
}
