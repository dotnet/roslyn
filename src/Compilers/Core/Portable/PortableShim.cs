// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            Touch(HashAlgorithm.Type);
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

            internal static readonly Func<string, byte[]> ReadAllBytes = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(ReadAllBytes), paramTypes: new[] { typeof(string) })
                .CreateDelegate<Func<string, byte[]>>();

            internal static readonly Action<string, byte[]> WriteAllBytes = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(WriteAllBytes), paramTypes: new[] { typeof(string), typeof(byte[]) })
                .CreateDelegate<Action<string, byte[]>>();
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
                    contractName: $"{TypeName}, {CoreNames.System_Runtime}",
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

            internal static readonly Type Type = ReflectionUtilities.GetTypeFromEither(
                contractName: $"{TypeName}, {CoreNames.System_Security_Cryptography_Primitives}",
                desktopName: TypeName);

            private static readonly MethodInfo s_computeHash_byte = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(ComputeHash), new[] { typeof(byte[]) });

            private static readonly MethodInfo s_computeHash_byte_int_int = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(ComputeHash), new[] { typeof(byte[]), typeof(int), typeof(int) });

            private static readonly MethodInfo s_computeHash_stream = Type
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(ComputeHash), new[] { typeof(Stream) });

            internal static byte[] ComputeHash(object hashInstance, byte[] buffer)
            {
                return (byte[])s_computeHash_byte.Invoke(hashInstance, new object[] { buffer });
            }

            internal static byte[] ComputeHash(object hashInstance, byte[] buffer, int offset, int count)
            {
                return (byte[])s_computeHash_byte_int_int.Invoke(hashInstance, new object[] { buffer, offset, count });
            }

            internal static byte[] ComputeHash(object hashInstance, Stream inputStream)
            {
                return (byte[])s_computeHash_stream.Invoke(hashInstance, new object[] { inputStream });
            }
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
