// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor;

/// <summary>
///  A <see cref="FactAttribute"/> that only executes if each of the given conditions are met.
/// </summary>
/// <remarks>
///  Conditions can be provided using the <see cref="Is"/> type. For example, <see cref="Is.Windows"/> -or- 
///  <see cref="Is.Not.Linux"/>, <see cref="Is.Not.MacOS"/>.
/// </remarks>
public sealed class ConditionalFactAttribute : FactAttribute
{
    public ConditionalFactAttribute(params string[] conditions)
    {
        if (!Conditions.AllTrue(conditions))
        {
            base.Skip = Reason ?? Conditions.GetSkipReason(conditions);
        }
    }

    /// <summary>
    ///  This property exists to prevent users of <see cref="ConditionalFactAttribute"/>
    ///  from accidentally putting documentation in the <see cref="Skip"/> property instead of Reason.
    ///  Setting <see cref="Skip"/> would cause the test to be unconditionally skip.
    /// </summary>
    [Obsolete($"{nameof(ConditionalFactAttribute)} should always use {nameof(Reason)} or {nameof(AlwaysSkip)}", error: true)]
    public new string Skip
    {
        get { return base.Skip; }
        set { base.Skip = value; }
    }

    /// <summary>
    ///  Use to unconditionally skip a test.
    /// </summary>
    /// <remarks>
    ///  This is useful in the rare occasion when a conditional test needs to be skipped unconditionally.
    ///  Typically, this is for a short term reason, such as working on a bug fix.
    /// </remarks>
    public string AlwaysSkip
    {
        get { return base.Skip; }
        set { base.Skip = value; }
    }

    public string? Reason { get; set; }
}

/// <summary>
///  A <see cref="TheoryAttribute"/> that only executes if each of the given conditions are met.
/// </summary>
/// <remarks>
///  Conditions can be provided using the <see cref="Is"/> type. For example, <see cref="Is.Windows"/> -or- 
///  <see cref="Is.Not.Linux"/>, <see cref="Is.Not.MacOS"/>.
/// </remarks>
public sealed class ConditionalTheoryAttribute : TheoryAttribute
{
    public ConditionalTheoryAttribute(params string[] conditions)
    {
        if (!Conditions.AllTrue(conditions))
        {
            base.Skip = Reason ?? Conditions.GetSkipReason(conditions);
        }
    }

    /// <summary>
    ///  This property exists to prevent users of <see cref="ConditionalFactAttribute"/>
    ///  from accidentally putting documentation in the <see cref="Skip"/> property instead of Reason.
    ///  Setting <see cref="Skip"/> would cause the test to be unconditionally skip.
    /// </summary>
    [Obsolete($"{nameof(ConditionalFactAttribute)} should always use {nameof(Reason)} or {nameof(AlwaysSkip)}", error: true)]
    public new string Skip
    {
        get { return base.Skip; }
        set { base.Skip = value; }
    }

    /// <summary>
    ///  Use to unconditionally skip a test.
    /// </summary>
    /// <remarks>
    ///  This is useful in the rare occasion when a conditional test needs to be skipped unconditionally.
    ///  Typically, this is for a short term reason, such as working on a bug fix.
    /// </remarks>
    public string AlwaysSkip
    {
        get { return base.Skip; }
        set { base.Skip = value; }
    }

    public string? Reason { get; set; }
}

public static class Is
{
    /// <summary>
    ///  Only execute if the current operating system platform is Windows.
    /// </summary>
    public const string Windows = nameof(Windows);

    /// <summary>
    ///  Only execute if the current operating system platform is Linux.
    /// </summary>
    public const string Linux = nameof(Linux);

    /// <summary>
    ///  Only execute if the current operating system platform is MacOS.
    /// </summary>
    public const string MacOS = nameof(MacOS);

    /// <summary>
    ///  Only execute if the current operating system platform is FreeBSD.
    /// </summary>
    public const string FreeBSD = nameof(FreeBSD);

    /// <summary>
    ///  Only execute if the current operating system platform is Unix-based.
    /// </summary>
    public const string AnyUnix = nameof(AnyUnix);

    public static class Not
    {
        /// <summary>
        ///  Only execute if the current operating system platform is not Windows.
        /// </summary>
        public const string Windows = $"!{nameof(Windows)}";

        /// <summary>
        ///  Only execute if the current operating system platform is Linux.
        /// </summary>
        public const string Linux = $"!{nameof(Linux)}";

        /// <summary>
        ///  Only execute if the current operating system platform is not MacOS.
        /// </summary>
        public const string MacOS = $"!{nameof(MacOS)}";

        /// <summary>
        ///  Only execute if the current operating system platform is not FreeBSD.
        /// </summary>
        public const string FreeBSD = $"!{nameof(FreeBSD)}";

        /// <summary>
        ///  Only execute if the current operating system platform is not Unix-based.
        /// </summary>
        public const string AnyUnix = $"!{nameof(AnyUnix)}";

    }
}

public static class Conditions
{
    private static readonly FrozenDictionary<string, Func<bool>> s_conditionMap = CreateConditionMap();

    private static FrozenDictionary<string, Func<bool>> CreateConditionMap()
    {
        var map = new Dictionary<string, Func<bool>>(StringComparer.OrdinalIgnoreCase);

        Add(Is.Windows, static () => PlatformInformation.IsWindows);
        Add(Is.Linux, static () => PlatformInformation.IsLinux);
        Add(Is.MacOS, static () => PlatformInformation.IsMacOS);
        Add(Is.FreeBSD, static () => PlatformInformation.IsFreeBSD);
        Add(Is.AnyUnix, static () => PlatformInformation.IsLinux ||
                                     PlatformInformation.IsMacOS ||
                                     PlatformInformation.IsFreeBSD);

        return map.ToFrozenDictionary();

        void Add(string name, Func<bool> predicate)
        {
            map.Add(name, predicate);

            // Add negated condition
            map.Add($"!{name}", () => !predicate());
        }
    }

    public static bool AllTrue(string[] conditions)
    {
        foreach (var condition in conditions)
        {
            if (!s_conditionMap.TryGetValue(condition, out var predicate))
            {
                throw new NotSupportedException($"Encountered unexpected condition: {condition}");
            }

            if (!predicate())
            {
                return false;
            }
        }

        return true;
    }

    public static string GetSkipReason(string[] conditions)
        => $"The following conditions are not all true: {string.Join(", ", conditions)}";
}
