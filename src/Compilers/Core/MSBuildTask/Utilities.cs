﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
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

        internal static string GetLocation(Assembly assembly)
        {
            var method = typeof(Assembly).GetTypeInfo().GetDeclaredProperty("Location")?.GetMethod;
            if (method == null)
            {
                return null;
            }

            return (string)method.Invoke(assembly, parameters: null);
        }

        /// <summary>
        /// Try to get the directory this assembly is in. Returns null if assembly
        /// was in the GAC or DLL location can not be retrieved.
        /// </summary>
        public static string GenerateFullPathToTool(string toolName)
        {
            string toolLocation = null;

            var buildTask = typeof(Utilities).GetTypeInfo().Assembly;
            var inGac = (bool?)typeof(Assembly)
                .GetTypeInfo()
                .GetDeclaredProperty("GlobalAssemblyCache")
                ?.GetMethod.Invoke(buildTask, parameters: null);

            if (inGac != true)
            {
                var codeBase = (string)typeof(Assembly)
                    .GetTypeInfo()
                    .GetDeclaredProperty("CodeBase")
                    ?.GetMethod.Invoke(buildTask, parameters: null);

                if (codeBase != null)
                {
                    var uri = new Uri(codeBase);

                    string assemblyPath = null;
                    if (uri.IsFile)
                    {
                        assemblyPath = uri.LocalPath;
                    }
                    else
                    {
                        var callingAssembly = (Assembly)typeof(Assembly)
                            .GetTypeInfo()
                            .GetDeclaredMethod("GetCallingAssembly")
                            ?.Invoke(null, null);

                        var location = GetLocation(callingAssembly);

                        if (location != null)
                        {
                            assemblyPath = location;
                        }
                    }

                    if(assemblyPath != null)
                    {
                        var assemblyDirectory = Path.GetDirectoryName(assemblyPath);
                        var toolLocalLocation = Path.Combine(assemblyDirectory, toolName);

                        if (File.Exists(toolLocalLocation))
                        {
                            toolLocation = toolLocalLocation;
                        }
                    }
                }

                if (toolLocation == null)
                {
                    // Roslyn only deploys to the 32Bit folder of MSBuild, so request this path on all architectures.
                    var pathToBuildTools = ToolLocationHelper.GetPathToBuildTools(ToolLocationHelper.CurrentToolsVersion, DotNetFrameworkArchitecture.Bitness32);

                    if (pathToBuildTools != null)
                    {
                        var toolMSBuildLocation = Path.Combine(pathToBuildTools, MSBuildRoslynFolderName, toolName);

                        if (File.Exists(toolMSBuildLocation))
                        {
                            toolLocation = toolMSBuildLocation;
                        }
                    }
                }
            }

            return toolLocation;
        }
    }
}
