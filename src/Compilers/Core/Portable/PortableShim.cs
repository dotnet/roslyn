// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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

        internal static class Environment
        {
            internal const string TypeName = "System.Environment";

            internal static readonly Type Type = GetTypeFromEither(
                contractName: $"${TypeName}, System.Runtime.Extensions, Version=4.0.10.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                corlibName: TypeName);

            internal static Func<string, string> ExpandEnvironmentVariables = (Func<string, string>)Type
                .GetTypeInfo()
                .GetDeclaredMethod("ExpandEnvironmentVariables")
                .CreateDelegate(typeof(Func<string, string>));
        }

        internal static class Path
        {
            internal const string TypeName = "System.IO.Path";

            internal static readonly Type Type = GetTypeFromEither(
                contractName: $"${TypeName}, System.Runtime.Extensions, Version=4.0.10.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                corlibName: TypeName);

            internal static readonly char DirectorySeparatorChar = (char)Type
                .GetTypeInfo()
                .GetDeclaredField(nameof(DirectorySeparatorChar))
                .GetValue(null);
        }
    }
}
