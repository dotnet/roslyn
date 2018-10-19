﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Reflection;
using System.Threading;
using Xunit.Sdk;

namespace Roslyn.Test.Utilities
{
    /// <summary>
    /// Apply this attribute to your test method to replace the
    /// <see cref="Thread.CurrentThread" /> <see cref="CultureInfo.CurrentCulture" /> and
    /// <see cref="CultureInfo.CurrentUICulture" /> with another culture.
    /// </summary>
    /// <remarks>
    /// This code was adapted from
    /// https://github.com/xunit/samples.xunit/blob/5de2967/UseCulture/UseCultureAttribute.cs.
    /// The original code is (c) 2014 Outercurve Foundation and licensed under the Apache License,
    /// Version 2.0.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class UseCultureAttribute : BeforeAfterTestAttribute
    {
        private readonly Lazy<CultureInfo> _culture;
        private readonly Lazy<CultureInfo> _uiCulture;
        private CultureInfo _originalCulture;
        private CultureInfo _originalUICulture;

        /// <summary>
        /// Replaces the culture and UI culture of the current thread with
        /// <paramref name="culture" />
        /// </summary>
        /// <param name="culture">The name of the culture.</param>
        /// <remarks>
        /// <para>
        /// This constructor overload uses <paramref name="culture" /> for both
        /// <see cref="Culture" /> and <see cref="UICulture" />.
        /// </para>
        /// </remarks>
        public UseCultureAttribute(string culture)
            : this(culture, culture)
        {
        }

        /// <summary>
        /// Replaces the culture and UI culture of the current thread with
        /// <paramref name="culture" /> and <paramref name="uiCulture" />
        /// </summary>
        /// <param name="culture">The name of the culture.</param>
        /// <param name="uiCulture">The name of the UI culture.</param>
        public UseCultureAttribute(string culture, string uiCulture)
        {
#if NET472
            _culture = new Lazy<CultureInfo>(() => new CultureInfo(culture, useUserOverride: false));
            _uiCulture = new Lazy<CultureInfo>(() => new CultureInfo(uiCulture, useUserOverride: false));
#elif NETCOREAPP2_0
            _culture = new Lazy<CultureInfo>(() => new CultureInfo(culture));
            _uiCulture = new Lazy<CultureInfo>(() => new CultureInfo(uiCulture));
#else
            _culture = new Lazy<CultureInfo>(() => throw new NotSupportedException());
            _uiCulture = new Lazy<CultureInfo>(() => throw new NotSupportedException());
#endif
        }

        /// <summary>
        /// Gets the culture.
        /// </summary>
        public CultureInfo Culture => _culture.Value;

        /// <summary>
        /// Gets the UI culture.
        /// </summary>
        public CultureInfo UICulture => _uiCulture.Value;

        /// <summary>
        /// Stores the current <see cref="CultureInfo.CurrentCulture" /> and <see cref="CultureInfo.CurrentUICulture" />
        /// and replaces them with the new cultures defined in the constructor.
        /// </summary>
        /// <param name="methodUnderTest">The method under test</param>
        public override void Before(MethodInfo methodUnderTest)
        {
            _originalCulture = CultureInfo.CurrentCulture;
            _originalUICulture = CultureInfo.CurrentUICulture;

#if NET472 || NETCOREAPP2_0
            CultureInfo.CurrentCulture = Culture;
            CultureInfo.CurrentUICulture = UICulture;

#if NET472
            CultureInfo.CurrentCulture.ClearCachedData();
            CultureInfo.CurrentUICulture.ClearCachedData();
#endif
#else
            throw new NotSupportedException("Cannot set the current culture on this framework target.");
#endif
        }

        /// <summary>
        /// Restores the original <see cref="CultureInfo.CurrentCulture" /> and
        /// <see cref="CultureInfo.CurrentUICulture" />.
        /// </summary>
        /// <param name="methodUnderTest">The method under test</param>
        public override void After(MethodInfo methodUnderTest)
        {
#if NET472 || NETCOREAPP2_0
            CultureInfo.CurrentCulture = _originalCulture;
            CultureInfo.CurrentUICulture = _originalUICulture;

#if NET472
            CultureInfo.CurrentCulture.ClearCachedData();
            CultureInfo.CurrentUICulture.ClearCachedData();
#endif
#else
            throw new NotSupportedException("Cannot set the current culture on this framework target.");
#endif
        }
    }
}
