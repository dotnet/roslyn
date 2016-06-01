// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Roslyn.Utilities;

namespace Microsoft.DiaSymReader.PortablePdb
{
    internal static class PortableShim
    {
        private static class CoreNames
        {
            internal const string System_IO_FileSystem = "System.IO.FileSystem, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            internal const string System_Runtime_Extensions = "System.Runtime.Extensions, Version=4.0.10.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
        }

        internal static class Environment
        {
            internal const string TypeName = "System.Environment";

            internal static readonly Type Type = ReflectionUtilities.GetTypeFromEither(
                contractName: $"{TypeName}, {CoreNames.System_Runtime_Extensions}",
                desktopName: TypeName);

            internal static Func<string, string> GetEnvironmentVariable = (Func<string, string>)Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(GetEnvironmentVariable), paramTypes: new[] { typeof(string) })
                .CreateDelegate(typeof(Func<string, string>));
        }

        internal static class File
        {
            internal const string TypeName = "System.IO.File";

            internal static readonly Type Type = ReflectionUtilities.GetTypeFromEither(
                contractName: $"{TypeName}, {CoreNames.System_IO_FileSystem}",
                desktopName: TypeName);

            internal static readonly Func<string, Stream> OpenRead = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(OpenRead), paramTypes: new[] { typeof(string) })
                .CreateDelegate<Func<string, Stream>>();

            internal static readonly Func<string, bool> Exists = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(Exists), new[] { typeof(string) })
                .CreateDelegate<Func<string, bool>>();

            internal static readonly Func<string, byte[]> ReadAllBytes = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(ReadAllBytes), paramTypes: new[] { typeof(string) })
                .CreateDelegate<Func<string, byte[]>>();
        }
    }
}
