// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;

namespace Roslyn.Utilities
{
#if !COMPILERCORE
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal static class Contract
    {
        // internal for testing
        [Serializable]
        internal class ContractFailureException : Exception
        {
            public ContractFailureException() { }
            public ContractFailureException(string message) : base(message) { }
            public ContractFailureException(string message, Exception inner) : base(message, inner) { }
            public ContractFailureException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context)
                : base(info, context)
            { }
        }

        /// <summary>
        /// Equivalent to Debug.Assert.  
        ///
        /// DevDiv 867813 covers removing this completely at a future date
        /// </summary>
        [Conditional("DEBUG")]
        [DebuggerHidden]
        public static void Requires(bool condition, string message = null)
        {
            Assert(condition, message);
        }

        [Conditional("DEBUG")]
        [DebuggerHidden]
        public static void Requires<T>(bool condition, string message = null)
            where T : Exception
        {
            Assert<T>(condition, message);
        }

        /// <summary>
        /// Equivalent to Debug.Assert.  
        ///
        /// DevDiv 867813 covers removing this completely at a future date
        /// </summary>
        [Conditional("DEBUG")]
        [DebuggerHidden]
        public static void Assert(bool condition, string message = null)
        {
            if (condition)
            {
                return;
            }

            if (string.IsNullOrEmpty(message))
            {
                Debug.Assert(condition);
            }
            else
            {
                Debug.Assert(condition, message);
            }
        }

        [Conditional("DEBUG")]
        [DebuggerHidden]
        public static void Assert<T>(bool condition, string message = null)
            where T : Exception
        {
            if (!condition)
            {
                if (string.IsNullOrEmpty(message))
                {
                    ConstructorInfo constructor = typeof(T).GetConstructor(Type.EmptyTypes);
                    throw (Exception)constructor.Invoke(null);
                }
                else
                {
                    ConstructorInfo constructor = typeof(T).GetConstructor(new[] { typeof(string) });
                    throw (Exception)constructor.Invoke(new object[] { message });
                }
            }
        }

        /// <summary>
        /// Equivalent to Debug.Assert.  
        ///
        /// DevDiv 867813 covers removing this completely at a future date
        /// </summary>
        [Conditional("DEBUG")]
        public static void Assume(bool condition, string message = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                Debug.Assert(condition);
            }
            else
            {
                Debug.Assert(condition, message);
            }
        }

        /// <summary>
        /// Throws a non-accessible exception if the provided value is null.  This method executes in
        /// all builds
        /// </summary>
        public static void ThrowIfNull<T>(T value, string message = null) where T : class
        {
            if (value == null)
            {
                message = message ?? "Unexpected Null";
                Fail(message);
            }
        }

        /// <summary>
        /// Throws a non-accessible exception if the provided value is false.  This method executes
        /// in all builds
        /// </summary>
        public static void ThrowIfFalse(bool condition, string message = null)
        {
            if (!condition)
            {
                message = message ?? "Unexpected false";
                Fail(message);
            }
        }

        /// <summary>
        /// Throws a non-accessible exception if the provided value is true. This method executes in
        /// all builds.
        /// </summary>
        public static void ThrowIfTrue(bool condition, string message = null)
        {
            if (condition)
            {
                message = message ?? "Unexpected true";
                Fail(message);
            }
        }

        [DebuggerHidden]
        public static void Fail(string message = "Unexpected")
        {
            throw new ContractFailureException(message);
        }

        [DebuggerHidden]
        public static T FailWithReturn<T>(string message = "Unexpected")
        {
            throw new ContractFailureException(message);
        }

        public static void InvalidEnumValue<T>(T value)
        {
            Fail(string.Format("Invalid Enumeration value {0}", value));
        }

        [DebuggerDisplay("Unreachable")]
        public static Exception Unreachable
        {
            get
            {
                Debug.Fail("This code path should not be reachable");
                return new InvalidOperationException();
            }
        }
    }
}