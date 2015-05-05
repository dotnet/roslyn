// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Roslyn.Utilities
{
    /// <summary>
    /// The 4.5 portable API surface area does not contain many of the APIs Roslyn needs to fucntion.  In 
    /// particular it lacks APIs to access the file system.  The project is constrained from moving to the
    /// 4.6 framework until post VS 2015 though.  
    /// 
    /// This puts us in a difficult position.  These APIs are necessary for us to have our public API set
    /// in the DLLS we prefer (non Desktop variants) but we can't use them directly.  Putting the APIs
    /// into the Desktop variants would create instant legacy for the Roslyn project that we'd have to 
    /// maintain for the remainder of the project.
    /// 
    /// As a compromise we've decided to grab this APIs via reflection for the time being.  This is a 
    /// *very* unfortunate path to be on but it's a short term solution that sets us up for long term
    /// success. 
    /// 
    /// This is an unfortunate situation but it will all be removed fairly quickly after RTM and converted
    /// to the proper 4.5 portable contracts.  
    /// </summary>
    internal static class PortableShim
    {
        private const string System_IO_FileSystem_Name = "System.IO.FileSystem, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
        private const string System_IO_FileSystem_Primitives_Name = "System.IO.FileSystem.Primitives, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
        private const string System_Runtime_Extensions_Name = "System.Runtime.Extensions, Version=4.0.10.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";

        /// <summary>
        /// Find a <see cref="Type"/> instance by first probing the contract name and then the name as it
        /// would exist in mscorlib.  This helps satisfy both the CoreCLR and Desktop scenarios. 
        /// </summary>
        private static Type GetTypeFromEither(string contractName, string corlibName)
        {
            var type = Type.GetType(contractName, throwOnError: false);
            if (type == null)
            {
                type = Type.GetType(corlibName, throwOnError: false);
            }

            return type;
        }

        private static T FindItem<T>(IEnumerable<T> collection, params Type[] paramTypes)
            where T : MethodBase
        {
            foreach (var current in collection)
            {
                var p = current.GetParameters();
                if (p.Length != paramTypes.Length)
                {
                    continue;
                }

                bool allMatch = true;
                for (int i = 0; i < paramTypes.Length; i++)
                {
                    if (p[i].ParameterType != paramTypes[i])
                    {
                        allMatch = false;
                        break;
                    }
                }

                if (allMatch)
                {
                    return current;
                }
            }

            return null;
        }

        private static MethodInfo GetDeclaredMethod(this TypeInfo typeInfo, string name, params Type[] paramTypes)
        {
            return FindItem(typeInfo.GetDeclaredMethods(name), paramTypes);
        }

        private static ConstructorInfo GetDeclaredConstructor(this TypeInfo typeInfo, params Type[] paramTypes)
        {
            return FindItem(typeInfo.DeclaredConstructors, paramTypes);
        }

        internal static class Environment
        {
            internal const string TypeName = "System.Environment";

            internal static readonly Type Type = GetTypeFromEither(
                contractName: $"{TypeName}, {System_Runtime_Extensions_Name}",
                corlibName: TypeName);

            internal static Func<string, string> ExpandEnvironmentVariables = (Func<string, string>)Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(ExpandEnvironmentVariables))
                .CreateDelegate(typeof(Func<string, string>));

            internal static Func<string, string> GetEnvironmentVariable = (Func<string, string>)Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(GetEnvironmentVariable))
                .CreateDelegate(typeof(Func<string, string>));
        }

        internal static class Path
        {
            internal const string TypeName = "System.IO.Path";

            internal static readonly Type Type = GetTypeFromEither(
                contractName: $"{TypeName}, {System_Runtime_Extensions_Name}",
                corlibName: TypeName);

            internal static readonly char DirectorySeparatorChar = (char)Type
                .GetTypeInfo()
                .GetDeclaredField(nameof(DirectorySeparatorChar))
                .GetValue(null);

            internal static readonly char AltDirectorySeparatorChar = (char)Type
                .GetTypeInfo()
                .GetDeclaredField(nameof(AltDirectorySeparatorChar))
                .GetValue(null);

            internal static readonly Func<string, string> GetFullPath = (Func<string, string>)Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(GetFullPath), paramTypes: new[] { typeof(string) })
                .CreateDelegate(typeof(Func<string, string>));

            internal static readonly Func<string> GetTempFileName = (Func<string>)Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(GetTempFileName), paramTypes: new Type[] { })
                .CreateDelegate(typeof(Func<string>));
        }

        internal static class File
        {
            internal const string TypeName = "System.IO.File";

            internal static readonly Type Type = GetTypeFromEither(
                contractName: $"{TypeName}, {System_IO_FileSystem_Name}",
                corlibName: TypeName);

            internal static readonly Func<string, DateTime> GetLastWriteTimeUtc = (Func<string, DateTime>)Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(GetLastWriteTimeUtc), new[] { typeof(string) })
                .CreateDelegate(typeof(Func<string, DateTime>));

            internal static readonly Func<string, bool> Exists = (Func<string, bool>)Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(Exists), new[] { typeof(string) })
                .CreateDelegate(typeof(Func<string, bool>));

            internal static readonly Action<string> Delete = (Action<string>)Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(Delete), new[] { typeof(string) })
                .CreateDelegate(typeof(Action<string>));

            internal static readonly Func<string, byte[]> ReadAllBytes = (Func<string, byte[]>)Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(ReadAllBytes), paramTypes: new[] { typeof(string) })
                .CreateDelegate(typeof(Func<string, byte[]>));
        }

        internal static class FileMode
        {
            internal const string TypeName = "System.IO.FileMode";

            internal static readonly Type Type = GetTypeFromEither(
                contractName: $"{TypeName}, {System_IO_FileSystem_Primitives_Name}",
                corlibName: TypeName);

            internal static readonly object CreateNew;

            internal static readonly object Create;

            internal static readonly object Open;

            internal static readonly object OpenOrCreate;

            internal static readonly object Truncate;

            internal static readonly object Append;

            static FileMode()
            {
                foreach (object value in Enum.GetValues(Type))
                {
                    var i = (int)value;
                    switch (i)
                    {
                        case 1:
                            CreateNew = value;
                            break;
                        case 2:
                            Create = value;
                            break;
                        case 3:
                            Open = value;
                            break;
                        case 4:
                            OpenOrCreate = value;
                            break;
                        case 5:
                            Truncate = value;
                            break;
                        case 6:
                            Append = value;
                            break;
                    }
                }
            }
        }

        internal static class FileAccess
        {
            internal const string TypeName = "System.IO.FileAccess";

            internal static readonly Type Type = GetTypeFromEither(
                contractName: $"{TypeName}, {System_IO_FileSystem_Primitives_Name}",
                corlibName: TypeName);

            internal static readonly object Read;

            internal static readonly object Write;

            internal static readonly object ReadWrite;

            static FileAccess()
            {
                foreach (object value in Enum.GetValues(Type))
                {
                    var i = (int)value;
                    switch (i)
                    {
                        case 1:
                            Read = value;
                            break;
                        case 2:
                            Write = value;
                            break;
                        case 3:
                            ReadWrite = value;
                            break;
                    }
                }
            }
        }

        internal static class FileShare
        {
            internal const string TypeName = "System.IO.FileShare";

            internal static readonly Type Type = GetTypeFromEither(
                contractName: $"{TypeName}, {System_IO_FileSystem_Primitives_Name}",
                corlibName: TypeName);

            internal static readonly object None;

            internal static readonly object Read;

            internal static readonly object Write;

            internal static readonly object ReadWrite;

            internal static readonly object Delete;

            internal static readonly object Inheritable;

            static FileShare()
            {
                foreach (object value in Enum.GetValues(Type))
                {
                    var i = (int)value;
                    switch (i)
                    {
                        case 0:
                            None = value;
                            break;
                        case 1:
                            Read = value;
                            break;
                        case 2:
                            Write = value;
                            break;
                        case 3:
                            ReadWrite = value;
                            break;
                        case 4:
                            Delete = value;
                            break;
                        case 16:
                            Inheritable = value;
                            break;
                    }
                }
            }
        }

        internal static class FileOptions
        {
            internal const string TypeName = "System.IO.FileOptions";

            internal static readonly Type Type = GetTypeFromEither(
                contractName: $"{TypeName}, {System_IO_FileSystem_Name}",
                corlibName: TypeName);

            internal static readonly object Asynchronous;

            static FileOptions()
            {
                foreach (object value in Enum.GetValues(Type))
                {
                    var i = (int)value;
                    switch (i)
                    {
                        case 1073741824:
                            Asynchronous = value;
                            break;
                    }
                }
            }
        }

        internal static class FileStream
        {
            internal const string TypeName = "System.IO.FileStream";

            internal static readonly Type Type = GetTypeFromEither(
                contractName: $"${TypeName}, ${System_IO_FileSystem_Name}",
                corlibName: TypeName);

            private static ConstructorInfo s_Ctor_String_FileMode_FileAccess_FileShare = Type
                .GetTypeInfo()
                .GetDeclaredConstructor(paramTypes: new[] { typeof(string), FileMode.Type, FileAccess.Type, FileShare.Type });

            private static ConstructorInfo s_Ctor_String_FileMode = Type
                .GetTypeInfo()
                .GetDeclaredConstructor(paramTypes: new[] { typeof(string), FileMode.Type });

            private static ConstructorInfo s_Ctor_String_FileMode_FileAccess_FileShare_Int32_FileOptions = Type
                .GetTypeInfo()
                .GetDeclaredConstructor(paramTypes: new[] { typeof(string), FileMode.Type, FileAccess.Type, FileShare.Type, typeof(int), FileOptions.Type });

            public static Stream Create(string path, object mode)
            {
                return (Stream)s_Ctor_String_FileMode.Invoke(new[] { path, mode });
            }

            public static Stream Create(string path, object mode, object access, object share)
            {
                return (Stream)s_Ctor_String_FileMode_FileAccess_FileShare.Invoke(new[] { path, mode, access, share });
            }

            public static Stream Create(string path, object mode, object access, object share, int bufferSize, object options)
            {
                return (Stream)s_Ctor_String_FileMode_FileAccess_FileShare_Int32_FileOptions.Invoke(new[] { path, mode, access, share, bufferSize, options });
            }
        }
    }
}
