// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    // REVIEW: should there be auto retry mechanism such as if out of proc run fails, it automatically calls inProc one?
    internal static class Extensions
    {
        public static THostSpecificService GetHostSpecificServiceAvailable<THostSpecificService>(this HostWorkspaceServices hostServices) where THostSpecificService : IHostSpecificService
        {
            // first get best host specific services from current context and see whether specific service exist in the services pack
            // if it doesn't, move down from outofproc to inproc to find service requested.
            var service = GetHostSpecificServices(hostServices).GetService<THostSpecificService>() as IHostSpecificService ??
                          GetHostSpecificServices(hostServices, HostKinds.OutOfProc).GetService<THostSpecificService>() as IHostSpecificService ??
                          GetHostSpecificServices(hostServices, HostKinds.InProc).GetService<THostSpecificService>() as IHostSpecificService;

            return (THostSpecificService)service;
        }

        public static THostSpecificService GetHostSpecificServiceAvailable<THostSpecificService>(this HostWorkspaceServices hostServices, string host) where THostSpecificService : IHostSpecificService
        {
            // first get host specific services from given host and see whether specific service exist in the services pack
            // if it doesn't, move down from outofproc to inproc to find service requested.
            var service = GetHostSpecificServices(hostServices, host).GetService<THostSpecificService>() as IHostSpecificService ??
                          GetHostSpecificServices(hostServices, HostKinds.OutOfProc).GetService<THostSpecificService>() as IHostSpecificService ??
                          GetHostSpecificServices(hostServices, HostKinds.InProc).GetService<THostSpecificService>() as IHostSpecificService;

            return (THostSpecificService)service;
        }

        private static THostSpecificService GetHostSpecificService<THostSpecificService>(this HostWorkspaceServices hostServices, string host) where THostSpecificService : IHostSpecificService
        {
            return GetHostSpecificServices(hostServices, host).GetService<THostSpecificService>();
        }

        private static HostSpecificServices GetHostSpecificServices(this HostWorkspaceServices hostServices)
        {
            return hostServices.GetRequiredService<IExecutionHostingService>().GetService();
        }

        private static HostSpecificServices GetHostSpecificServices(this HostWorkspaceServices hostServices, string host)
        {
            return hostServices.GetRequiredService<IExecutionHostingService>().GetService(host);
        }

        public static void WriteArray<T>(this ObjectWriter writer, T[] array)
        {
            // this could be moved into ObjectWriter itself.
            writer.WriteInt32(array.Length);

            ArrayType arrayType;
            if (!s_arrayTypeMap.TryGetValue(typeof(T), out arrayType))
            {
                throw ExceptionUtilities.UnexpectedValue(typeof(T));
            }

            writer.WriteInt32((int)arrayType);

            for (var i = 0; i < array.Length; i++)
            {
                writer.WriteValue(array[i]);
            }
        }

        public static T[] ReadArray<T>(this ObjectReader reader)
        {
            // this could be moved into ObjectReader itself
            var length = reader.ReadInt32();
            var arrayType = (ArrayType)reader.ReadInt32();

            Type type;
            if (!s_arrayTypeMap.TryGetKey(arrayType, out type))
            {
                throw ExceptionUtilities.UnexpectedValue(arrayType);
            }

            var array = (T[])Array.CreateInstance(type, length);

            for (var i = 0; i < length; i++)
            {
                array[i] = (T)reader.ReadValue();
            }

            return array;
        }

        private static readonly BidirectionalMap<Type, ArrayType> s_arrayTypeMap = new BidirectionalMap<Type, ArrayType>(
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
