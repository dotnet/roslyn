// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A DiagnosticInfo object has information about a diagnostic, but without any attached location information.
    /// </summary>
    /// <remarks>
    /// More specialized diagnostics with additional information (e.g., ambiguity errors) can derive from this class to
    /// provide access to additional information about the error, such as what symbols were involved in the ambiguity.
    /// </remarks>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal partial class DiagnosticInfo : IFormattable, IObjectWritable, IObjectReadable, IMessageSerializable
    {
        private static readonly object[] NoArguments = new object[0];

        private readonly CommonMessageProvider messageProvider;
        private readonly int errorCode;
        private readonly bool isWarningAsError;
        private readonly object[] arguments;

        // Only the compiler creates instances.
        internal DiagnosticInfo(CommonMessageProvider messageProvider, int errorCode)
        {
            this.messageProvider = messageProvider;
            this.errorCode = errorCode;
        }

        // Only the compiler creates instances.
        internal DiagnosticInfo(CommonMessageProvider messageProvider, int errorCode, params object[] arguments)
            : this(messageProvider, errorCode)
        {
            AssertMessageSerializable(arguments);

            this.arguments = arguments;
        }

        [Conditional("DEBUG")]
        internal static void AssertMessageSerializable(object[] args)
        {
            foreach (var arg in args)
            {
                Debug.Assert(arg != null);

                if (arg is IMessageSerializable)
                {
                    continue;
                }

                var type = arg.GetType();
                if (type == typeof(string) || type == typeof(AssemblyIdentity))
                {
                    continue;
                }

                var info = type.GetTypeInfo();
                if (info.IsPrimitive)
                {
                    continue;
                }

                Debug.Assert(false, "Unexpected type: " + type);
            }
        }

        // Only the compiler creates instances.
        internal DiagnosticInfo(CommonMessageProvider messageProvider, bool isWarningAsError, int errorCode, params object[] arguments)
            : this(messageProvider, errorCode, arguments)
        {
            Debug.Assert(!isWarningAsError || messageProvider.GetSeverity(errorCode) == DiagnosticSeverity.Warning);
            this.isWarningAsError = isWarningAsError;
        }

        // Create a copy of this instance with a WarningAsError flag
        internal virtual DiagnosticInfo GetInstanceWithReportWarning(bool isWarningAsError)
        {
            return new DiagnosticInfo(this.messageProvider, isWarningAsError, this.errorCode, this.arguments == null ? SpecializedCollections.EmptyArray<object>() : this.arguments);
        }

        // Create a copy of this instance with a explicit overridden severity
        internal DiagnosticInfo GetInstanceWithSeverity(DiagnosticSeverity severity)
        {
            return new DiagnosticInfoWithOverridenSeverity(this, severity);
        }

        #region Serialization

        void IObjectWritable.WriteTo(ObjectWriter writer)
        {
            this.WriteTo(writer);
        }

        protected virtual void WriteTo(ObjectWriter writer)
        {
            writer.WriteValue(this.messageProvider);
            writer.WriteCompressedUInt((uint)this.errorCode);
            writer.WriteBoolean(this.isWarningAsError);

            int count = (this.arguments != null) ? arguments.Length : 0;
            writer.WriteCompressedUInt((uint)count);

            if (count > 0)
            {
                foreach (var arg in this.arguments)
                {
                    writer.WriteString(arg.ToString());
                }
            }
        }

        Func<ObjectReader, object> IObjectReadable.GetReader()
        {
            return this.GetReader();
        }

        protected virtual Func<ObjectReader, object> GetReader()
        {
            return (r) => new DiagnosticInfo(r);
        }

        protected DiagnosticInfo(ObjectReader reader)
        {
            this.messageProvider = (CommonMessageProvider)reader.ReadValue();
            this.errorCode = (int)reader.ReadCompressedUInt();
            this.isWarningAsError = reader.ReadBoolean();

            var count = (int)reader.ReadCompressedUInt();
            if (count == 0)
            {
                this.arguments = NoArguments;
            }
            else if (count > 0)
            {
                this.arguments = new string[count];
                for (int i = 0; i < count; i++)
                {
                    this.arguments[i] = reader.ReadString();
                }
            }
        }

        #endregion

        /// <summary>
        /// The error code, as an integer.
        /// </summary>
        public int Code { get { return errorCode; } }

        /// <summary>
        /// Returns whether this diagnostic is informational, warning, or error.
        /// For checking if it is a warning treated as an error, use IsWarningsAsError.
        /// </summary>
        public virtual DiagnosticSeverity Severity
        {
            get
            {
                return messageProvider.GetSeverity(errorCode);
            }
        }

        /// <summary>
        /// Returns the warning level for a warning, 1 through 4. Errors have warning level 0.
        /// </summary>
        public int WarningLevel
        {
            get
            {
                return messageProvider.GetWarningLevel(errorCode);
            }
        }

        /// <summary>
        /// Returns true if this is a warning treated as an error.
        /// </summary>
        /// <remarks>
        /// True implies <see cref="Severity"/> = <see cref="DiagnosticSeverity.Warning"/>.
        /// </remarks>
        public bool IsWarningAsError
        {
            get
            {
                return isWarningAsError;
            }
        }

        /// <summary>
        /// If a derived class has additional information about other referenced symbols, it can
        /// expose the locations of those symbols in a general way, so they can be reported along
        /// with the error.
        /// </summary>
        public virtual IReadOnlyList<Location> AdditionalLocations
        {
            get
            {
                return SpecializedCollections.EmptyReadOnlyList<Location>();
            }
        }

        /// <summary>
        /// Get the message id (for example "CS1001") for the message. This includes both the error number
        /// and a prefix identifying the source.
        /// </summary>
        public string MessageIdentifier
        {
            get
            {
                return messageProvider.GetIdForErrorCode(errorCode);
            }
        }

        /// <summary>
        /// Get the text of the message in the given language.
        /// </summary>
        public virtual string GetMessage(CultureInfo culture = null)
        {
            if (culture == null)
            {
                culture = CultureInfo.InvariantCulture;
            }

            // Get the message and fill in arguments.
            string message = messageProvider.LoadMessage(errorCode, culture);
            if (string.IsNullOrEmpty(message))
            {
                return string.Empty;
            }

            if (arguments == null || arguments.Length == 0)
            {
                return message;
            }

            object[] argumentsToUse = arguments;

            for (int i = 0; i < argumentsToUse.Length; i++)
            {
                DiagnosticInfo embedded = argumentsToUse[i] as DiagnosticInfo;

                if (embedded != null)
                {
                    if (ReferenceEquals(argumentsToUse, arguments))
                    {
                        argumentsToUse = new object[argumentsToUse.Length];
                        Array.Copy(arguments, argumentsToUse, argumentsToUse.Length);
                    }

                    argumentsToUse[i] = embedded.GetMessage(culture);
                }
            }

            return String.Format(culture, message, argumentsToUse);
        }

        internal object[] Arguments
        {
            get { return arguments; }
        }

        internal CommonMessageProvider MessageProvider
        {
            get { return messageProvider; }
        }

        // TODO (tomat): remove
        public override string ToString()
        {
            return ToString(null);
        }

        public string ToString(IFormatProvider formatProvider)
        {
            return ((IFormattable)this).ToString(null, formatProvider);
        }

        string IFormattable.ToString(string format, IFormatProvider formatProvider)
        {
            return String.Format(formatProvider, "{0}: {1}",
                messageProvider.GetMessagePrefix(this.MessageIdentifier, this.Severity, this.IsWarningAsError, formatProvider as CultureInfo),
                this.GetMessage(formatProvider as CultureInfo));
        }

        public override int GetHashCode()
        {
            int hashCode = errorCode;
            if (arguments != null)
            {
                for (int i = 0; i < arguments.Length; i++)
                {
                    hashCode = Hash.Combine(this.arguments[i], hashCode);
                }
            }

            return hashCode;
        }

        public override bool Equals(object obj)
        {
            DiagnosticInfo other = obj as DiagnosticInfo;

            bool result = false;

            if (other != null &&
                other.errorCode == this.errorCode &&
                this.GetType() == obj.GetType())
            {
                if (this.arguments == null && other.arguments == null)
                {
                    result = true;
                }
                else if (this.arguments != null && other.arguments != null && this.arguments.Length == other.arguments.Length)
                {
                    result = true;
                    for (int i = 0; i < this.arguments.Length; i++)
                    {
                        if (this.arguments[i] != other.arguments[i])
                        {
                            result = false;
                            break;
                        }
                    }
                }
            }

            return result;
        }

        private string GetDebuggerDisplay()
        {
            // There aren't message resources for our internal error codes, so make
            // sure we don't call ToString for those.
            switch (Code)
            {
                case InternalErrorCode.Unknown:
                    return "Unresolved DiagnosticInfo";

                case InternalErrorCode.Void:
                    return "Void DiagnosticInfo";

                default:
                    return ToString();
            }
        }

        /// <summary>
        /// For a DiagnosticInfo that is lazily evaluated, this method evaluates it
        /// and returns a non-lazy DiagnosticInfo.
        /// </summary>
        internal virtual DiagnosticInfo GetResolvedInfo()
        {
            // We should never call GetResolvedInfo on a non-lazy DiagnosticInfo
            throw ExceptionUtilities.Unreachable;
        }
    }
}
