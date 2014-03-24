// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    internal class UnionStack<T0, T1, T2>
        where T0 : struct
        where T1 : struct
        where T2 : struct
    {
        private ArrayBuilder<T0> stack0 = ArrayBuilder<T0>.GetInstance();
        private ArrayBuilder<T1> stack1 = ArrayBuilder<T1>.GetInstance();
        private ArrayBuilder<T2> stack2 = ArrayBuilder<T2>.GetInstance();

        public enum Discriminator : byte
        {
            Invalid,
            Value0,
            Value1,
            Value2
        }

        private ArrayBuilder<Discriminator> discriminatorStack = ArrayBuilder<Discriminator>.GetInstance();

        public void Push(ref T0 value)
        {
            stack0.Push(value);
            discriminatorStack.Push(Discriminator.Value0);
        }

        public void Push(T0 value)
        {
            stack0.Push(value);
            discriminatorStack.Push(Discriminator.Value0);
        }

        public void Push(ref T1 value)
        {
            stack1.Push(value);
            discriminatorStack.Push(Discriminator.Value1);
        }

        public void Push(T1 value)
        {
            stack1.Push(value);
            discriminatorStack.Push(Discriminator.Value1);
        }

        public void Push(ref T2 value)
        {
            stack2.Push(value);
            discriminatorStack.Push(Discriminator.Value2);
        }

        public void Push(T2 value)
        {
            stack2.Push(value);
            discriminatorStack.Push(Discriminator.Value2);
        }

        public bool TryPeekDiscriminator(out Discriminator discriminator)
        {
            if (discriminatorStack.Count == 0)
            {
                discriminator = Discriminator.Invalid;
                return false;
            }

            discriminator = discriminatorStack.Peek();
            return true;
        }

        public T0 PopValue0()
        {
            var discriminator = discriminatorStack.Pop();
            Debug.Assert(discriminator == Discriminator.Value0);
            return stack0.Pop();
        }

        public T1 PopValue1()
        {
            var discriminator = discriminatorStack.Pop();
            Debug.Assert(discriminator == Discriminator.Value1);
            return stack1.Pop();
        }

        public T2 PopValue2()
        {
            var discriminator = discriminatorStack.Pop();
            Debug.Assert(discriminator == Discriminator.Value2);
            return stack2.Pop();
        }

        public void FreePooledObjects()
        {
            stack0.Free();
            stack1.Free();
            stack2.Free();
            discriminatorStack.Free();
        }
    }
}