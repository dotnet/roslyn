// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Roslyn.Utilities
{
    internal struct Union<T0, T1, T2>
    {
        T0 value0;
        T1 value1;
        T2 value2;
        byte discriminator;

        public Union(T0 value)
        {
            value0 = value;
            value1 = default(T1);
            value2 = default(T2);
            discriminator = 0;
        }

        public Union(T1 value)
        {
            value0 = default(T0);
            value1 = value;
            value2 = default(T2);
            discriminator = 1;
        }

        public Union(T2 value)
        {
            value0 = default(T0);
            value1 = default(T1);
            value2 = value;
            discriminator = 2;
        }

        public int Discriminator
        {
            get { return discriminator; }
        }

        public T0 Value0
        {
            get { return value0; }
        }

        public T1 Value1
        {
            get { return value1; }
        }

        public T2 Value2
        {
            get { return value2; }
        }

        public static implicit operator Union<T0, T1, T2>(T0 value)
        {
            return new Union<T0, T1, T2>(value);
        }

        public static implicit operator Union<T0, T1, T2>(T1 value)
        {
            return new Union<T0, T1, T2>(value);
        }

        public static implicit operator Union<T0, T1, T2>(T2 value)
        {
            return new Union<T0, T1, T2>(value);
        }
    }
}