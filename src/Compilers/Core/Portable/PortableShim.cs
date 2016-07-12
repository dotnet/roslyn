﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Xml.Linq;

namespace Roslyn.Utilities
{
    /// <summary>
    /// The 4.5 portable API surface area does not contain many of the APIs Roslyn needs to function.  In 
    /// particular it lacks APIs to access the file system.  The Roslyn project though is constrained 
    /// from moving to the 4.6 framework until post VS 2015.
    /// 
    /// This puts us in a difficult position.  These APIs are necessary for us to have our public API set
    /// in the DLLS we prefer (non Desktop variants) but we can't use them directly when targeting 
    /// the 4.5 framework.  Putting the APIs into the Desktop variants would create instant legacy for 
    /// the Roslyn project that we'd have to maintain forever (even if it was just as assemblies with
    /// only type forward entries).  This is not a place we'd like to be in.  
    /// 
    /// As a compromise we've decided to grab these APIs via reflection for the time being.  This is a 
    /// *very* unfortunate path to be on but it's a short term solution that sets us up for long term
    /// success.  
    /// 
    /// This is an unfortunate situation but it will all be removed fairly quickly after RTM and converted
    /// to the proper 4.6 portable contracts.  
    ///
    /// Note: Only portable APIs should be present here.
    /// </summary>
    internal static class PortableShim
    {
        internal static void Initialize()
        {
            // This method provides a way to force the static initializers of each type below
            // to run. This ensures that the static field values will be computed eagerly
            // rather than lazily on demand. If you add a new nested class below to access API
            // surface area, be sure to "touch" the Type field here.

            Touch(Assembly.Type);
            Touch(Directory.Type);
            Touch(Encoding.Type);
            Touch(Environment.Type);
            Touch(FileAttributes.Type);
            Touch(File.Type);
            Touch(FileAccess.Type);
            Touch(FileMode.Type);
            Touch(FileOptions.Type);
            Touch(FileShare.Type);
            Touch(FileStream.Type);
            Touch(FileVersionInfo.Type);
            Touch(MemoryStream.Type);
            Touch(Path.Type);
            Touch(RuntimeHelpers.Type);
            Touch(SearchOption.Type);
            Touch(StackTrace.Type);
            Touch(Thread.Type);
            Touch(XPath.Extensions.Type);
            Touch(SHA1.Type);
            Touch(SHA256.Type);
            Touch(SHA512.Type);
            Touch(SHA384.Type);
            Touch(MD5.Type);
        }

        private static void Touch(Type type)
        {
            // Do nothing.
        }

        private static class CoreNames
        {
            internal const string System_Diagnostics_FileVersionInfo = "System.Diagnostics.FileVersionInfo, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            internal const string System_Diagnostics_StackTrace = "System.Diagnostics.StackTrace, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            internal const string System_Diagnostics_Process = "System.Diagnostics.Process, Version=4.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            internal const string System_IO_FileSystem = "System.IO.FileSystem, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            internal const string System_IO_FileSystem_Primitives = "System.IO.FileSystem.Primitives, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            internal const string System_Reflection = "System.Reflection, Version=4.0.10.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            internal const string System_Runtime = "System.Runtime, Version=4.0.20.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            internal const string System_Runtime_Extensions = "System.Runtime.Extensions, Version=4.0.10.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            internal const string System_Security_Cryptography_Primitives = "System.Security.Cryptography.Primitives, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            internal const string System_Security_Cryptography_Algorithms = "System.Security.Cryptography.Algorithms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            internal const string System_Threading_Thread = "System.Threading.Thread, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            internal const string System_Xml_XPath_XDocument = "System.Xml.XPath.XDocument, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
        }

        private static class DesktopNames
        {
            internal const string System_Xml_Linq = "System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
        }

        internal static class Environment
        {
            internal const string TypeName = "System.Environment";

            internal static readonly Type Type = ReflectionUtilities.GetTypeFromEither(
                contractName: $"{TypeName}, {CoreNames.System_Runtime_Extensions}",
                desktopName: TypeName);

            internal static Func<string, string> ExpandEnvironmentVariables = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(ExpandEnvironmentVariables), paramTypes: new[] { typeof(string) })
                .CreateDelegate<Func<string, string>>();

            internal static Func<string, string> GetEnvironmentVariable = (Func<string, string>)Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(GetEnvironmentVariable), paramTypes: new[] { typeof(string) })
                .CreateDelegate(typeof(Func<string, string>));
        }

        internal static class Path
        {
            internal const string TypeName = "System.IO.Path";

            internal static readonly Type Type = ReflectionUtilities.GetTypeFromEither(
                contractName: $"{TypeName}, {CoreNames.System_Runtime_Extensions}",
                desktopName: TypeName);

            internal static readonly char DirectorySeparatorChar = (char)Type
                .GetTypeInfo()
                .GetDeclaredField(nameof(DirectorySeparatorChar))
                .GetValue(null);

            internal static readonly char AltDirectorySeparatorChar = (char)Type
                .GetTypeInfo()
                .GetDeclaredField(nameof(AltDirectorySeparatorChar))
                .GetValue(null);

            internal static readonly Func<string, string> GetFullPath = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(GetFullPath), paramTypes: new[] { typeof(string) })
                .CreateDelegate<Func<string, string>>();

            internal static readonly Func<string> GetTempFileName = (Func<string>)Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(GetTempFileName), paramTypes: new Type[] { })
                .CreateDelegate(typeof(Func<string>));
        }

        internal static class FileAttributes
        {
            private const string TypeName = "System.IO.FileAttributes";

            internal static readonly Type Type = ReflectionUtilities.GetTypeFromEither(
               contractName: $"{TypeName}, {CoreNames.System_IO_FileSystem_Primitives}",
               desktopName: TypeName);

            public static object Hidden = Enum.ToObject(Type, 2);
        }

        internal static class File
        {
            internal const string TypeName = "System.IO.File";

            internal static readonly Type Type = ReflectionUtilities.GetTypeFromEither(
                contractName: $"{TypeName}, {CoreNames.System_IO_FileSystem}",
                desktopName: TypeName);

            internal static readonly Func<string, DateTime> GetLastWriteTimeUtc = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(GetLastWriteTimeUtc), new[] { typeof(string) })
                .CreateDelegate<Func<string, DateTime>>();

            internal static readonly Func<string, Stream> Create = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(Create), paramTypes: new[] { typeof(string) })
                .CreateDelegate<Func<string, Stream>>();

            internal static readonly Func<string, Stream> OpenRead = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(OpenRead), paramTypes: new[] { typeof(string) })
                .CreateDelegate<Func<string, Stream>>();

            internal static readonly Func<string, bool> Exists = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(Exists), new[] { typeof(string) })
                .CreateDelegate<Func<string, bool>>();

            internal static readonly Action<string> Delete = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(Delete), new[] { typeof(string) })
                .CreateDelegate<Action<string>>();

            internal static readonly Action<string, string> Move = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(Move), new[] { typeof(string), typeof(string) })
                .CreateDelegate<Action<string, string>>();

            internal static readonly Func<string, byte[]> ReadAllBytes = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(ReadAllBytes), paramTypes: new[] { typeof(string) })
                .CreateDelegate<Func<string, byte[]>>();

            internal static readonly Action<string, byte[]> WriteAllBytes = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(WriteAllBytes), paramTypes: new[] { typeof(string), typeof(byte[]) })
                .CreateDelegate<Action<string, byte[]>>();

            internal static readonly Action<string, string, System.Text.Encoding> WriteAllText = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(WriteAllText), paramTypes: new[] { typeof(string), typeof(string), typeof(System.Text.Encoding) })
                .CreateDelegate<Action<string, string, System.Text.Encoding>>();

            private static readonly MethodInfo SetAttributesMethod = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(SetAttributes), paramTypes: new[] { typeof(string), FileAttributes.Type });

            public static void SetAttributes(string path, object attributes)
            {
                SetAttributesMethod.Invoke(null, new[] { path, attributes });
            }
        }

        internal static class Directory
        {
            internal const string TypeName = "System.IO.Directory";

            internal static readonly Type Type = ReflectionUtilities.GetTypeFromEither(
                contractName: $"{TypeName}, {CoreNames.System_IO_FileSystem}",
                desktopName: TypeName);

            private static readonly MethodInfo s_enumerateDirectories = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(EnumerateDirectories), new[] { typeof(string), typeof(string), SearchOption.Type });

            private static readonly MethodInfo s_enumerateFiles = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(EnumerateFiles), new[] { typeof(string), typeof(string), SearchOption.Type });

            internal static IEnumerable<string> EnumerateDirectories(string path, string searchPattern, object searchOption)
            {
                return (IEnumerable<string>)s_enumerateDirectories.Invoke(null, new object[] { path, searchPattern, searchOption });
            }

            internal static readonly Func<string, bool> Exists = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(Exists), new[] { typeof(string) })
                .CreateDelegate<Func<string, bool>>();

            internal static IEnumerable<string> EnumerateFiles(string path, string searchPattern, object searchOption)
            {
                return (IEnumerable<string>)s_enumerateFiles.Invoke(null, new object[] { path, searchPattern, searchOption });
            }
        }

        internal static class FileMode
        {
            internal const string TypeName = "System.IO.FileMode";

            internal static readonly Type Type = ReflectionUtilities.GetTypeFromEither(
                contractName: $"{TypeName}, {CoreNames.System_IO_FileSystem_Primitives}",
                desktopName: TypeName);

            internal static readonly object CreateNew = Enum.ToObject(Type, 1);

            internal static readonly object Create = Enum.ToObject(Type, 2);

            internal static readonly object Open = Enum.ToObject(Type, 3);

            internal static readonly object OpenOrCreate = Enum.ToObject(Type, 4);

            internal static readonly object Truncate = Enum.ToObject(Type, 5);

            internal static readonly object Append = Enum.ToObject(Type, 6);
        }

        internal static class FileAccess
        {
            internal const string TypeName = "System.IO.FileAccess";

            internal static readonly Type Type = ReflectionUtilities.GetTypeFromEither(
                contractName: $"{TypeName}, {CoreNames.System_IO_FileSystem_Primitives}",
                desktopName: TypeName);

            internal static readonly object Read = Enum.ToObject(Type, 1);

            internal static readonly object Write = Enum.ToObject(Type, 2);

            internal static readonly object ReadWrite = Enum.ToObject(Type, 3);
        }

        internal static class FileShare
        {
            internal const string TypeName = "System.IO.FileShare";

            internal static readonly Type Type = ReflectionUtilities.GetTypeFromEither(
                contractName: $"{TypeName}, {CoreNames.System_IO_FileSystem_Primitives}",
                desktopName: TypeName);

            internal static readonly object None = Enum.ToObject(Type, 0);

            internal static readonly object Read = Enum.ToObject(Type, 1);

            internal static readonly object Write = Enum.ToObject(Type, 2);

            internal static readonly object ReadWrite = Enum.ToObject(Type, 3);

            internal static readonly object Delete = Enum.ToObject(Type, 4);

            internal static readonly object Inheritable = Enum.ToObject(Type, 16);

            internal static readonly object ReadWriteBitwiseOrDelete = Enum.ToObject(Type, 3 | 4);
        }

        internal static class FileOptions
        {
            internal const string TypeName = "System.IO.FileOptions";

            internal static readonly Type Type = ReflectionUtilities.GetTypeFromEither(
                contractName: $"{TypeName}, {CoreNames.System_IO_FileSystem}",
                desktopName: TypeName);

            internal static readonly object None = Enum.ToObject(Type, 0);

            internal static readonly object Asynchronous = Enum.ToObject(Type, 1073741824);
        }

        internal static class SearchOption
        {
            internal const string TypeName = "System.IO.SearchOption";

            internal static readonly Type Type = ReflectionUtilities.GetTypeFromEither(
                contractName: $"{TypeName}, {CoreNames.System_IO_FileSystem}",
                desktopName: TypeName);

            internal static readonly object TopDirectoryOnly = Enum.ToObject(Type, 0);

            internal static readonly object AllDirectories = Enum.ToObject(Type, 1);
        }

        internal static class FileStream
        {
            internal const string TypeName = "System.IO.FileStream";

            internal static readonly Type Type = ReflectionUtilities.GetTypeFromEither(
                contractName: $"{TypeName}, {CoreNames.System_IO_FileSystem}",
                desktopName: TypeName);

            internal static readonly PropertyInfo Name = Type
                .GetTypeInfo()
                .GetDeclaredProperty(nameof(Name));

            private static ConstructorInfo s_Ctor_String_FileMode_FileAccess_FileShare = Type
                .GetTypeInfo()
                .GetDeclaredConstructor(paramTypes: new[] { typeof(string), FileMode.Type, FileAccess.Type, FileShare.Type });

            private static ConstructorInfo s_Ctor_String_FileMode_FileAccess = Type
                .GetTypeInfo()
                .GetDeclaredConstructor(paramTypes: new[] { typeof(string), FileMode.Type, FileAccess.Type });

            private static ConstructorInfo s_Ctor_String_FileMode = Type
                .GetTypeInfo()
                .GetDeclaredConstructor(paramTypes: new[] { typeof(string), FileMode.Type });

            private static ConstructorInfo s_Ctor_String_FileMode_FileAccess_FileShare_Int32_FileOptions = Type
                .GetTypeInfo()
                .GetDeclaredConstructor(paramTypes: new[] { typeof(string), FileMode.Type, FileAccess.Type, FileShare.Type, typeof(int), FileOptions.Type });

            internal static Stream Create(string path, object mode)
            {
                return s_Ctor_String_FileMode.InvokeConstructor<Stream>(path, mode);
            }

            internal static Stream Create(string path, object mode, object access)
            {
                return s_Ctor_String_FileMode_FileAccess.InvokeConstructor<Stream>(path, mode, access);
            }

            internal static Stream Create(string path, object mode, object access, object share)
            {
                return s_Ctor_String_FileMode_FileAccess_FileShare.InvokeConstructor<Stream>(path, mode, access, share);
            }

            internal static Stream Create(string path, object mode, object access, object share, int bufferSize, object options)
            {
                return s_Ctor_String_FileMode_FileAccess_FileShare_Int32_FileOptions.InvokeConstructor<Stream>(path, mode, access, share, bufferSize, options);
            }

            internal static Stream Create_String_FileMode_FileAccess_FileShare(string path, object mode, object access, object share)
            {
                return Create(path, mode, access, share);
            }
        }

        internal static class FileVersionInfo
        {
            internal const string TypeName = "System.Diagnostics.FileVersionInfo";

            internal static readonly string DesktopName = $"{TypeName}, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";

            internal static readonly Type Type = ReflectionUtilities.GetTypeFromEither(
                contractName: $"{TypeName}, {CoreNames.System_Diagnostics_FileVersionInfo}",
                desktopName: DesktopName);

            internal static readonly Func<string, object> GetVersionInfo = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(GetVersionInfo), new[] { typeof(string) })
                .CreateDelegate<Func<string, object>>();

            internal static readonly PropertyInfo FileVersion = Type
                .GetTypeInfo()
                .GetDeclaredProperty(nameof(FileVersion));
        }

        internal static class Thread
        {
            internal const string TypeName = "System.Threading.Thread";

            internal static readonly Type Type = ReflectionUtilities.GetTypeFromEither(
                contractName: $"{TypeName}, {CoreNames.System_Threading_Thread}",
                desktopName: TypeName);

            internal static readonly PropertyInfo CurrentThread = Type
                .GetTypeInfo()
                .GetDeclaredProperty(nameof(CurrentThread));

            internal static readonly PropertyInfo CurrentUICulture = Type
                .GetTypeInfo()
                .GetDeclaredProperty(nameof(CurrentUICulture));

            internal static readonly PropertyInfo ManagedThreadId = Type
                .GetTypeInfo()
                .GetDeclaredProperty(nameof(ManagedThreadId));
        }

        internal static class Process
        {
            internal const string TypeName = "System.Diagnostics.Process";

            internal static readonly Type Type = ReflectionUtilities.GetTypeFromEither(
                contractName: $"{TypeName}, {CoreNames.System_Diagnostics_Process}",
                desktopName: TypeName);

            internal static readonly PropertyInfo Id = Type
                .GetTypeInfo()
                .GetDeclaredProperty(nameof(Id));

            internal static readonly Func<object> GetCurrentProcess = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(GetCurrentProcess), paramTypes: new Type[] { })
                .CreateDelegate<Func<object>>();
        }

        internal static class RuntimeHelpers
        {
            internal const string TypeName = "System.Runtime.CompilerServices.RuntimeHelpers";

            internal static readonly Type Type = ReflectionUtilities.GetTypeFromEither(
                contractName: $"{TypeName}, {CoreNames.System_Runtime}",
                desktopName: TypeName);

            internal static readonly Action EnsureSufficientExecutionStack = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(EnsureSufficientExecutionStack), paramTypes: new Type[] { })
                .CreateDelegate<Action>();
        }

        internal static class StackTrace
        {
            internal const string TypeName = "System.Diagnostics.StackTrace";

            internal static readonly Type Type = ReflectionUtilities.GetTypeFromEither(
                contractName: $"{TypeName}, {CoreNames.System_Diagnostics_StackTrace}",
                desktopName: TypeName);

            private static readonly ConstructorInfo s_Ctor = Type
                .GetTypeInfo()
                .GetDeclaredConstructor(new Type[] { });

            private static readonly MethodInfo s_ToString = Type
                .GetTypeInfo()
                .GetDeclaredMethod("ToString", new Type[] { });

            internal static string GetString()
            {
                var stackTrace = s_Ctor.InvokeConstructor();

                return s_ToString.Invoke<string>(stackTrace) ?? "StackTrace unavailable.";
            }
        }

        internal static class Encoding
        {
            internal static readonly Type Type = typeof(System.Text.Encoding);

            internal static readonly Func<int, System.Text.Encoding> GetEncoding = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(GetEncoding), paramTypes: new[] { typeof(int) })
                .CreateDelegate<Func<int, System.Text.Encoding>>();
        }

        internal static class MemoryStream
        {
            internal static readonly Type Type = typeof(System.IO.MemoryStream);

            internal static readonly MethodInfo GetBuffer = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(GetBuffer));
        }

        internal static class XPath
        {
            internal static class Extensions
            {
                internal const string TypeName = "System.Xml.XPath.Extensions";

                internal static readonly Type Type = ReflectionUtilities.GetTypeFromEither(
                    contractName: $"{TypeName}, {CoreNames.System_Xml_XPath_XDocument}",
                    desktopName: $"{TypeName}, {DesktopNames.System_Xml_Linq}");

                internal static readonly Func<XNode, string, IEnumerable<XElement>> XPathSelectElements = Type
                    .GetTypeInfo()
                    .GetDeclaredMethod(nameof(XPathSelectElements), new[] { typeof(XNode), typeof(string) })
                    .CreateDelegate<Func<XNode, string, IEnumerable<XElement>>>();
            }
        }

        /// <summary>
        /// APIs contained here are proposed for CoreFX but not yet finalized.  Their contracts are
        /// subject to change. 
        /// </summary>
        internal static class Proposed
        {
        }

        internal static class Misc
        {
            internal static string GetFileVersion(string path)
            {
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(path);
                return (string)FileVersionInfo.FileVersion.GetValue(fileVersionInfo);
            }

            internal static void SetCurrentUICulture(CultureInfo cultureInfo)
            {
                // TODO: CoreClr needs to go through CultureInfo.CurrentUICulture
                var thread = Thread.CurrentThread.GetValue(null);
                Thread.CurrentUICulture.SetValue(thread, cultureInfo);
            }
        }

        internal static class Assembly
        {
            private const string TypeName = "System.Reflection.Assembly";

            internal static readonly Type Type = ReflectionUtilities.GetTypeFromEither(
                contractName: $"{TypeName}, {CoreNames.System_Reflection}",
                desktopName: TypeName);

            internal static readonly Func<System.Reflection.Assembly, string, bool, bool, Type> GetType_string_bool_bool = Type
                .GetTypeInfo()
                .GetDeclaredMethod("GetType", typeof(string), typeof(bool), typeof(bool))
                .CreateDelegate<Func<System.Reflection.Assembly, string, bool, bool, Type>>();
        }

        internal static class HashAlgorithm
        {
            private const string TypeName = "System.Security.Cryptography.HashAlgorithm";

            private static Type s_lazyType;

            internal static Type TypeOpt => ReflectionUtilities.GetTypeFromEither(ref s_lazyType,
                contractName: TypeName + ", " + CoreNames.System_Security_Cryptography_Primitives,
                desktopName: TypeName);

            private static MethodInfo s_lazyTransformBlock, s_lazyTransformFinalBlock, s_lazyComputeHashByte, s_lazyComputeHashByteIntInt, s_lazyComputeHashStream;
            private static PropertyInfo s_lazyHash;
            
            internal static int TransformBlock(object hashAlgorithm, byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
            {
                if (s_lazyTransformBlock == null)
                {
                    s_lazyTransformBlock = TypeOpt.GetTypeInfo().GetDeclaredMethod(nameof(TransformBlock), new[] { typeof(byte[]), typeof(int), typeof(int), typeof(byte[]), typeof(int) });
                }

                return (int)s_lazyTransformBlock.Invoke(hashAlgorithm, new object[] { inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset });
            }

            internal static byte[] TransformFinalBlock(object hashAlgorithm, byte[] inputBuffer, int inputOffset, int inputCount)
            {
                if (s_lazyTransformFinalBlock == null)
                {
                    s_lazyTransformFinalBlock = TypeOpt.GetTypeInfo().GetDeclaredMethod(nameof(TransformFinalBlock), new[] { typeof(byte[]), typeof(int), typeof(int) });
                }

                return (byte[])s_lazyTransformFinalBlock.Invoke(hashAlgorithm, new object[] { inputBuffer, inputOffset, inputCount });
            }

            internal static byte[] Hash(object hashAlgorithm)
            {
                if (s_lazyHash == null)
                {
                    s_lazyHash = TypeOpt.GetTypeInfo().GetDeclaredProperty(nameof(Hash));
                }

                return (byte[])s_lazyHash.GetValue(hashAlgorithm);
            }

            internal static byte[] ComputeHash(object hashInstance, byte[] buffer)
            {
                if (s_lazyComputeHashByte == null)
                {
                    s_lazyComputeHashByte = TypeOpt.GetTypeInfo().GetDeclaredMethod(nameof(ComputeHash), new[] { typeof(byte[]) });
                }

                return (byte[])s_lazyComputeHashByte.Invoke(hashInstance, new object[] { buffer });
            }

            internal static byte[] ComputeHash(object hashInstance, byte[] buffer, int offset, int count)
            {
                if (s_lazyComputeHashByteIntInt == null)
                {
                    s_lazyComputeHashByteIntInt = TypeOpt.GetTypeInfo().GetDeclaredMethod(nameof(ComputeHash), new[] { typeof(byte[]), typeof(int), typeof(int) });
                }

                return (byte[])s_lazyComputeHashByteIntInt.Invoke(hashInstance, new object[] { buffer, offset, count });
            }

            internal static byte[] ComputeHash(object hashInstance, Stream inputStream)
            {
                if (s_lazyComputeHashStream == null)
                {
                    s_lazyComputeHashStream = TypeOpt.GetTypeInfo().GetDeclaredMethod(nameof(ComputeHash), new[] { typeof(Stream) });
                }

                return (byte[])s_lazyComputeHashStream.Invoke(hashInstance, new object[] { inputStream });
            }
        }

        internal static class IncrementalHash
        {
            private const string TypeName = "System.Security.Cryptography.IncrementalHash";

            private static Type s_lazyType;
            private static MethodInfo s_lazyCreateHash, s_lazyAppendData, s_lazyHashAndReset;

            internal static Type TypeOpt => ReflectionUtilities.TryGetType(ref s_lazyType,
                TypeName + ", " + CoreNames.System_Security_Cryptography_Algorithms);

            internal static IDisposable CreateHash(object hashAlgorithmName)
            {
                if (s_lazyCreateHash == null)
                {
                    s_lazyCreateHash = TypeOpt.GetTypeInfo().GetDeclaredMethod(nameof(CreateHash), new[] { HashAlgorithmName.TypeOpt });
                }

                return (IDisposable)s_lazyCreateHash.Invoke(null, new[] { hashAlgorithmName });
            }

            internal static void AppendData(object incrementalHash, byte[] data, int offset, int count)
            {
                if (s_lazyAppendData == null)
                {
                    s_lazyAppendData = TypeOpt.GetTypeInfo().GetDeclaredMethod(nameof(AppendData), new[] { typeof(byte[]), typeof(int), typeof(int) });
                }

                s_lazyAppendData.Invoke(incrementalHash, new object[] { data, offset, count });
            }

            internal static byte[] GetHashAndReset(object incrementalHash)
            {
                if (s_lazyHashAndReset == null)
                {
                    s_lazyHashAndReset = TypeOpt.GetTypeInfo().GetDeclaredMethod(nameof(GetHashAndReset), new Type[0]);
                }

                return (byte[])s_lazyHashAndReset.Invoke(incrementalHash, new object[0]);
            }
        }

        internal static class HashAlgorithmName
        {
            private const string TypeName = "System.Security.Cryptography.HashAlgorithmName";

            private static Type s_lazyType;
            private static object s_lazySHA1;
            
            internal static Type TypeOpt => ReflectionUtilities.GetTypeFromEither(ref s_lazyType,
                contractName: TypeName + ", " + CoreNames.System_Security_Cryptography_Primitives,
                desktopName: TypeName);

            internal static object SHA1 => s_lazySHA1 ?? (s_lazySHA1 = TypeOpt.GetTypeInfo().GetDeclaredProperty("SHA1").GetValue(null));
        }

        internal static class SHA1
        {
            private const string TypeName = "System.Security.Cryptography.SHA1";

            internal static readonly Type Type = ReflectionUtilities.GetTypeFromEither(
                contractName: $"{TypeName}, {CoreNames.System_Security_Cryptography_Algorithms}",
                desktopName: TypeName);

            internal static readonly Func<IDisposable> Create = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(Create), new Type[] { })
                .CreateDelegate<Func<IDisposable>>();
        }

        internal static class SHA256
        {
            private const string TypeName = "System.Security.Cryptography.SHA256";

            internal static readonly Type Type = ReflectionUtilities.GetTypeFromEither(
                contractName: $"{TypeName}, {CoreNames.System_Security_Cryptography_Algorithms}",
                desktopName: TypeName);

            internal static readonly Func<IDisposable> Create = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(Create), new Type[] { })
                .CreateDelegate<Func<IDisposable>>();
        }

        internal static class SHA384
        {
            private const string TypeName = "System.Security.Cryptography.SHA384";

            internal static readonly Type Type = ReflectionUtilities.GetTypeFromEither(
                contractName: $"{TypeName}, {CoreNames.System_Security_Cryptography_Algorithms}",
                desktopName: TypeName);

            internal static readonly Func<IDisposable> Create = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(Create), new Type[] { })
                .CreateDelegate<Func<IDisposable>>();
        }

        internal static class SHA512
        {
            private const string TypeName = "System.Security.Cryptography.SHA512";

            internal static readonly Type Type = ReflectionUtilities.GetTypeFromEither(
                contractName: $"{TypeName}, {CoreNames.System_Security_Cryptography_Algorithms}",
                desktopName: TypeName);

            internal static readonly Func<IDisposable> Create = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(Create), new Type[] { })
                .CreateDelegate<Func<IDisposable>>();
        }

        internal static class MD5
        {
            private const string TypeName = "System.Security.Cryptography.MD5";

            internal static readonly Type Type = ReflectionUtilities.GetTypeFromEither(
                contractName: $"{TypeName}, {CoreNames.System_Security_Cryptography_Algorithms}",
                desktopName: TypeName);

            internal static readonly Func<IDisposable> Create = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(Create), new Type[] { })
                .CreateDelegate<Func<IDisposable>>();
        }
    }
}
