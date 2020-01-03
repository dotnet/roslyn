// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Test.Utilities.MinimalImplementations
{
    public static class IAsyncDisposable
    {
        public const string CSharp = @"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace System.Threading.Tasks
{
    public struct ValueTask
    {
        public ValueTaskAwaiter GetAwaiter() => new ValueTaskAwaiter();
    }
}

namespace System.Runtime.CompilerServices
{
    public struct ValueTaskAwaiter : INotifyCompletion
    {
        public bool IsCompleted { get; }
        public void GetResult() { }
        public void OnCompleted(Action continuation) { }
    }
}

namespace System
{
    public interface IAsyncDisposable
    {
        ValueTask DisposeAsync();
    }
}";

        public const string VisualBasic = @"
Imports System
Imports System.Runtime.CompilerServices
Imports System.Threading.Tasks

Namespace System.Threading.Tasks
    Public Structure ValueTask
        Public Function GetAwaiter() As ValueTaskAwaiter
            Return New ValueTaskAwaiter()
        End Function
    End Structure
End Namespace

Namespace System.Runtime.CompilerServices
    Public Structure ValueTaskAwaiter
        Implements INotifyCompletion

        Public ReadOnly Property IsCompleted As Boolean

        Public Sub GetResult()
        End Sub

        Public Sub OnCompleted(continuation As Action) Implements INotifyCompletion.OnCompleted
        End Sub
    End Structure
End Namespace

Namespace System
    Interface IAsyncDisposable
        Function DisposeAsync() As ValueTask
    End Interface
End Namespace";
    }
}
