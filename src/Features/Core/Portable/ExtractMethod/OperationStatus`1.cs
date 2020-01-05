// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        {
            return new OperationStatus<T>(status, Data);
        }

        public OperationStatus<TNew> With<TNew>(TNew data)
        {
            return new OperationStatus<TNew>(Status, data);
        }
    }
}
