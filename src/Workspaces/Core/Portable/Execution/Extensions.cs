// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    // REVIEW: should there be auto retry mechanism such as if out of proc run fails, it automatically calls inProc one?
    internal static class Extensions
    {
        public static void WriteArray<T>(this ObjectWriter writer, T[] array)
        {
            ArrayType arrayType;
            if (!s_arrayTypeMap.TryGetValue(typeof(T), out arrayType))
            {
                throw ExceptionUtilities.UnexpectedValue(typeof(T));
            }

            // object writer supports these type of array natively
            if (arrayType == ArrayType.Byte || arrayType == ArrayType.Char)
            {
                writer.WriteValue(array);
                return;
            }

            // this could be moved into ObjectWriter itself.
            writer.WriteInt32(array.Length);
            writer.WriteInt32((int)arrayType);

            for (var i = 0; i < array.Length; i++)
            {
                writer.WriteValue(array[i]);
            }
        }

        public static T[] ReadArray<T>(this ObjectReader reader)
        {
            ArrayType arrayType;
            if (!s_arrayTypeMap.TryGetValue(typeof(T), out arrayType))
            {
                throw ExceptionUtilities.UnexpectedValue(arrayType);
            }

            // object reader supports these type of array natively
            if (arrayType == ArrayType.Byte || arrayType == ArrayType.Char)
            {
                return (T[])reader.ReadValue();
            }

            // this could be moved into ObjectReader itself
            var length = reader.ReadInt32();
            var savedType = (ArrayType)reader.ReadInt32();
            Contract.ThrowIfFalse(arrayType == savedType);

            var array = (T[])Array.CreateInstance(typeof(T), length);

            for (var i = 0; i < length; i++)
            {
                array[i] = (T)reader.ReadValue();
            }

            return array;
        }

        private static readonly ImmutableDictionary<Type, ArrayType> s_arrayTypeMap = ImmutableDictionary.CreateRange<Type, ArrayType>(
            new KeyValuePair<Type, ArrayType>[]
            {
                KeyValuePair.Create(typeof(bool), ArrayType.Bool),
                KeyValuePair.Create(typeof(int), ArrayType.Int),
                KeyValuePair.Create(typeof(string), ArrayType.String),
                KeyValuePair.Create(typeof(short), ArrayType.Short),
                KeyValuePair.Create(typeof(long), ArrayType.Long),
                KeyValuePair.Create(typeof(char), ArrayType.Char),
                KeyValuePair.Create(typeof(sbyte), ArrayType.SByte),
                KeyValuePair.Create(typeof(byte), ArrayType.Byte),
                KeyValuePair.Create(typeof(ushort), ArrayType.UShort),
                KeyValuePair.Create(typeof(uint), ArrayType.UInt),
                KeyValuePair.Create(typeof(ulong), ArrayType.ULong),
                KeyValuePair.Create(typeof(decimal), ArrayType.Decimal),
                KeyValuePair.Create(typeof(float), ArrayType.Float),
                KeyValuePair.Create(typeof(double), ArrayType.Double),
                KeyValuePair.Create(typeof(DateTime), ArrayType.DateTime)
            });

        private enum ArrayType
        {
            Bool,
            Int,
            String,
            Short,
            Long,
            Char,
            SByte,
            Byte,
            UShort,
            UInt,
            ULong,
            Decimal,
            Float,
            Double,
            DateTime
        }
    }
}
