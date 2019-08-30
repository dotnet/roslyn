// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    /// <summary>
    /// General utilities.
    /// </summary>
    internal static class Utilities
    {
        private const string MSBuildRoslynFolderName = "Roslyn";

        /// <summary>
        /// Copied from msbuild. ItemSpecs are normalized using this method.
        /// </summary>
        public static string FixFilePath(string path)
            => string.IsNullOrEmpty(path) || Path.DirectorySeparatorChar == '\\' ? path : path.Replace('\\', '/');

        /// <summary>
        /// Convert a task item metadata to bool. Throw an exception if the string is badly formed and can't
        /// be converted.
        /// 
        /// If the metadata is not found, then set metadataFound to false and then return false.
        /// </summary>
        /// <param name="item">The item that contains the metadata.</param>
        /// <param name="itemMetadataName">The name of the metadata.</param>
        /// <returns>The resulting boolean value.</returns>
        internal static bool TryConvertItemMetadataToBool(ITaskItem item, string itemMetadataName)
        {
            string metadataValue = item.GetMetadata(itemMetadataName);
            if (metadataValue == null || metadataValue.Length == 0)
            {
                return false;
            }

            try
            {
                return ConvertStringToBool(metadataValue);
            }
            catch (System.ArgumentException e)
            {
                throw Utilities.GetLocalizedArgumentException(
                    e,
                    ErrorString.General_InvalidAttributeMetadata,
                    item.ItemSpec, itemMetadataName, metadataValue, "bool");
            }
        }

        /// <summary>
        /// Converts a string to a bool.  We consider "true/false", "on/off", and 
        /// "yes/no" to be valid boolean representations in the XML.
        /// </summary>
        /// <param name="parameterValue">The string to convert.</param>
        /// <returns>Boolean true or false, corresponding to the string.</returns>
        internal static bool ConvertStringToBool(string parameterValue)
        {
            if (ValidBooleanTrue(parameterValue))
            {
                return true;
            }
            else if (ValidBooleanFalse(parameterValue))
            {
                return false;
            }
            else
            {
                // Unsupported boolean representation.
                throw Utilities.GetLocalizedArgumentException(
                    ErrorString.General_CannotConvertStringToBool,
                    parameterValue);
            }
        }
        /// <summary>
        /// Returns true if the string can be successfully converted to a bool,
        /// such as "on" or "yes"
        /// </summary>
        internal static bool CanConvertStringToBool(string parameterValue) =>
            ValidBooleanTrue(parameterValue) || ValidBooleanFalse(parameterValue);

        /// <summary>
        /// Returns true if the string represents a valid MSBuild boolean true value,
        /// such as "on", "!false", "yes"
        /// </summary>
        private static bool ValidBooleanTrue(string parameterValue) =>
            String.Compare(parameterValue, "true", StringComparison.OrdinalIgnoreCase) == 0 ||
            String.Compare(parameterValue, "on", StringComparison.OrdinalIgnoreCase) == 0 ||
            String.Compare(parameterValue, "yes", StringComparison.OrdinalIgnoreCase) == 0 ||
            String.Compare(parameterValue, "!false", StringComparison.OrdinalIgnoreCase) == 0 ||
            String.Compare(parameterValue, "!off", StringComparison.OrdinalIgnoreCase) == 0 ||
            String.Compare(parameterValue, "!no", StringComparison.OrdinalIgnoreCase) == 0;

        /// <summary>
        /// Returns true if the string represents a valid MSBuild boolean false value,
        /// such as "!on" "off" "no" "!true"
        /// </summary>
        private static bool ValidBooleanFalse(string parameterValue) =>
            String.Compare(parameterValue, "false", StringComparison.OrdinalIgnoreCase) == 0 ||
            String.Compare(parameterValue, "off", StringComparison.OrdinalIgnoreCase) == 0 ||
            String.Compare(parameterValue, "no", StringComparison.OrdinalIgnoreCase) == 0 ||
            String.Compare(parameterValue, "!true", StringComparison.OrdinalIgnoreCase) == 0 ||
            String.Compare(parameterValue, "!on", StringComparison.OrdinalIgnoreCase) == 0 ||
            String.Compare(parameterValue, "!yes", StringComparison.OrdinalIgnoreCase) == 0;

        internal static string GetFullPathNoThrow(string path)
        {
            try
            {
                path = Path.GetFullPath(path);
            }
            catch (Exception e) when (IsIoRelatedException(e)) { }
            return path;
        }

        internal static void DeleteNoThrow(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception e) when (IsIoRelatedException(e)) { }
        }

        internal static bool IsIoRelatedException(Exception e) =>
            e is UnauthorizedAccessException ||
            e is NotSupportedException ||
            (e is ArgumentException && !(e is ArgumentNullException)) ||
            e is SecurityException ||
            e is IOException;

        internal static Exception GetLocalizedArgumentException(Exception e,
                                                                string errorString,
                                                                params object[] args)
        {
            return new ArgumentException(string.Format(CultureInfo.CurrentCulture, errorString, args), e);
        }

        internal static Exception GetLocalizedArgumentException(string errorString,
                                                                params object[] args)
        {
            return new ArgumentException(string.Format(CultureInfo.CurrentCulture, errorString, args));
        }

        internal static string TryGetAssemblyPath(Assembly assembly)
        {
            if ((bool?)typeof(Assembly).GetTypeInfo()
                .GetDeclaredProperty("GlobalAssemblyCache")
                ?.GetMethod.Invoke(assembly, parameters: null) == true)
            {
                return null;
            }

            var codebase = (string)typeof(Assembly)
                .GetTypeInfo()
                .GetDeclaredProperty("CodeBase")
                ?.GetMethod.Invoke(assembly, parameters: null);

            if (codebase != null)
            {
                var uri = new Uri(codebase);
                if (uri.IsFile)
                {
                    return uri.LocalPath;
                }
                else
                {
                    var callingAssembly = (Assembly)typeof(Assembly)
                        .GetTypeInfo()
                        .GetDeclaredMethod("GetCallingAssembly")
                        ?.Invoke(null, null);

                    var location = (string)typeof(Assembly).GetTypeInfo()
                        .GetDeclaredProperty("Location")
                        ?.GetMethod.Invoke(assembly, parameters: null);

                    if (location != null)
                    {
                        return location;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Generate the full path to the tool that is deployed with our build tasks.
        /// </summary>
        internal static string GenerateFullPathToTool(string toolName)
        {
            var buildTask = typeof(Utilities).GetTypeInfo().Assembly;
            var assemblyPath = buildTask.Location;
            var assemblyDirectory = Path.GetDirectoryName(assemblyPath);

            return RuntimeHostInfo.IsDesktopRuntime
                ? Path.Combine(assemblyDirectory, toolName)
                : Path.Combine(assemblyDirectory, "bincore", toolName);
        }
    }
}
