// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Runtime.Versioning;

[assembly: TargetFramework(".NETFramework,Version=v2.0")]

namespace Microsoft.CodeAnalysis
{
    internal static class ReflectionTypeExtensions
    {
        // Replaces a missing 4.5 method.
        internal static Type GetTypeInfo(this Type type)
        {
            return type;
        }

        // Replaces a missing 4.5 method.
        public static FieldInfo GetDeclaredField(this Type type, string name)
        {
            return type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
        }

        // Replaces a missing 4.5 method.
        public static MethodInfo GetDeclaredMethod(this Type type, string name, Type[] parameterTypes)
        {
            return type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly, null, parameterTypes, null);
        }
    }

    /// <summary>
    /// Required by <see cref="SymbolDisplayPartKind"/>.
    /// </summary>
    internal static class IErrorTypeSymbol
    {
    }

    /// <summary>
    /// Required by <see cref="Microsoft.CodeAnalysis.FailFast"/>
    /// </summary>
    internal static class Environment
    {
        public static void FailFast(string message) => System.Environment.FailFast(message);
        public static void FailFast(string message, Exception exception) => System.Environment.FailFast(exception.ToString());
        public static string NewLine => System.Environment.NewLine;
        public static int ProcessorCount => System.Environment.ProcessorCount;
    }
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    // To allow this dll to use extension methods even though we are targeting CLR v2, re-define ExtensionAttribute
    internal class ExtensionAttribute : Attribute
    {
    }

    /// <summary>
    /// This satisfies a cref on <see cref="Microsoft.CodeAnalysis.ExpressionEvaluator.DynamicFlagsCustomTypeInfo.CopyTo"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    internal class DynamicAttribute : Attribute
    {
    }

    /// <summary>
    /// This satisfies a cref on <see cref="Microsoft.CodeAnalysis.WellKnownMemberNames"/>.
    /// </summary>
    internal interface INotifyCompletion
    {
        void OnCompleted();
    }
}

namespace System.Text
{
    internal static class StringBuilderExtensions
    {
        public static void Clear(this StringBuilder builder)
        {
            builder.Length = 0; // Matches the real definition.
        }
    }
}

namespace System.Runtime.Versioning
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    internal sealed class TargetFrameworkAttribute : Attribute
    {
        public string FrameworkName { get; }
        public string FrameworkDisplayName { get; set; }

        public TargetFrameworkAttribute(string frameworkName)
            => FrameworkName = frameworkName;
    }
}
