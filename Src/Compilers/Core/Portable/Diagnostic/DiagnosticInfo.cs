// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Roslyn.Utilities;
using System.Threading;

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
        private readonly CommonMessageProvider messageProvider;
        private readonly int errorCode;
        private readonly DiagnosticSeverity defaultSeverity;
        private readonly DiagnosticSeverity effectiveSeverity;
        private readonly object[] arguments;

        private static ImmutableDictionary<int, DiagnosticDescriptor> errorCodeToDescriptorMap = ImmutableDictionary<int, DiagnosticDescriptor>.Empty;

        // Mark compiler errors as non-configurable to ensure they can never be suppressed or filtered.
        private static readonly ImmutableArray<string> CompilerErrorCustomTags = ImmutableArray.Create(WellKnownDiagnosticTags.Compiler, WellKnownDiagnosticTags.Telemetry, WellKnownDiagnosticTags.NotConfigurable);
        private static readonly ImmutableArray<string> CompilerNonErrorCustomTags = ImmutableArray.Create(WellKnownDiagnosticTags.Compiler, WellKnownDiagnosticTags.Telemetry);
        
        // Only the compiler creates instances.
        internal DiagnosticInfo(CommonMessageProvider messageProvider, int errorCode)
        {
            this.messageProvider = messageProvider;
            this.errorCode = errorCode;
            this.defaultSeverity = messageProvider.GetSeverity(errorCode);
            this.effectiveSeverity = this.defaultSeverity;
        }

        // Only the compiler creates instances.
        internal DiagnosticInfo(CommonMessageProvider messageProvider, int errorCode, params object[] arguments)
            : this(messageProvider, errorCode)
        {
            AssertMessageSerializable(arguments);

            this.arguments = arguments;
        }

        private DiagnosticInfo(DiagnosticInfo original, DiagnosticSeverity overridenSeverity)
        {
            this.messageProvider = original.MessageProvider;
            this.errorCode = original.errorCode;
            this.defaultSeverity = original.DefaultSeverity;
            this.arguments = original.arguments;

            this.effectiveSeverity = overridenSeverity;
        }

        internal static DiagnosticDescriptor GetDescriptor(int errorCode, CommonMessageProvider messageProvider)
        {
            var defaultSeverity = messageProvider.GetSeverity(errorCode);
            return GetOrCreateDescriptor(errorCode, defaultSeverity, messageProvider);
        }

        private static DiagnosticDescriptor GetOrCreateDescriptor(int errorCode, DiagnosticSeverity defaultSeverity, CommonMessageProvider messageProvider)
        {
            return ImmutableInterlocked.GetOrAdd(ref errorCodeToDescriptorMap, errorCode, code => CreateDescriptor(code, defaultSeverity, messageProvider));
        }

        private static DiagnosticDescriptor CreateDescriptor(int errorCode, DiagnosticSeverity defaultSeverity, CommonMessageProvider messageProvider)
        {
            var id = messageProvider.GetIdForErrorCode(errorCode);
            var title = messageProvider.GetTitle(errorCode);
            var description = messageProvider.GetDescription(errorCode);
            var messageFormat = messageProvider.GetMessageFormat(errorCode);
            var helpLink = messageProvider.GetHelpLink(errorCode);
            var category = messageProvider.GetCategory(errorCode);
            var customTags = GetCustomTags(defaultSeverity);
            return new DiagnosticDescriptor(id, title, messageFormat, category, defaultSeverity,
                isEnabledByDefault: true, description: description, helpLink: helpLink, customTags: customTags);
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
            Debug.Assert(!isWarningAsError || this.defaultSeverity == DiagnosticSeverity.Warning);

            if (isWarningAsError)
            {
                this.effectiveSeverity = DiagnosticSeverity.Error;
            }
        }

        // Create a copy of this instance with a explicit overridden severity
        internal DiagnosticInfo GetInstanceWithSeverity(DiagnosticSeverity severity)
        {
            return new DiagnosticInfo(this, severity);
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
            writer.WriteInt32((int)this.effectiveSeverity);
            writer.WriteInt32((int)this.defaultSeverity);

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
            this.effectiveSeverity = (DiagnosticSeverity)reader.ReadInt32();
            this.defaultSeverity = (DiagnosticSeverity)reader.ReadInt32();

            var count = (int)reader.ReadCompressedUInt();
            if (count == 0)
            {
                this.arguments = SpecializedCollections.EmptyObjects;
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

        public DiagnosticDescriptor Descriptor
        {
            get
            {
                return GetOrCreateDescriptor(this.errorCode, this.defaultSeverity, this.messageProvider);
            }
        }
        
        /// <summary>
         /// Returns the effective severity of the diagnostic: whether this diagnostic is informational, warning, or error.
         /// If IsWarningsAsError is true, then this returns <see cref="DiagnosticSeverity.Error"/>, while <see cref="DefaultSeverity"/> returns <see cref="DiagnosticSeverity.Warning"/>.
         /// </summary>
        public DiagnosticSeverity Severity
        {
            get
            {
                return this.effectiveSeverity;
            }
        }

        /// <summary>
        /// Returns whether this diagnostic is informational, warning, or error by default, based on the error code.
        /// To get diagnostic's effective severity, use <see cref="Severity"/>.
        /// </summary>
        public DiagnosticSeverity DefaultSeverity
        {
            get
            {
                return this.defaultSeverity;
            }
        }

        /// <summary>
        /// Gets the warning level. This is 0 for diagnostics with severity <see cref="DiagnosticSeverity.Error"/>,
        /// otherwise an integer between 1 and 4.
        /// </summary>
        public int WarningLevel
        {
            get
            {
                if (this.effectiveSeverity != this.defaultSeverity)
                {
                    return Diagnostic.GetDefaultWarningLevel(this.effectiveSeverity);
                }

                return messageProvider.GetWarningLevel(errorCode);
            }
        }

        /// <summary>
        /// Returns true if this is a warning treated as an error.
        /// </summary>
        /// <remarks>
        /// True implies <see cref="Severity"/> = <see cref="DiagnosticSeverity.Error"/> and
        /// <see cref="DefaultSeverity"/> = <see cref="DiagnosticSeverity.Warning"/>.
        /// </remarks>
        public bool IsWarningAsError
        {
            get
            {
                return this.DefaultSeverity == DiagnosticSeverity.Warning &&
                    this.Severity == DiagnosticSeverity.Error;
            }
        }

        /// <summary>
        /// Get the diagnostic category for the given diagnostic code.
        /// Default category is <see cref="Diagnostic.CompilerDiagnosticCategory"/>.
        /// </summary>
        public string Category
        {
            get
            {
                return this.messageProvider.GetCategory(this.errorCode) ;
            }
        }

        internal ImmutableArray<string> CustomTags
        {
            get
            {
                return GetCustomTags(this.defaultSeverity);
            }
        }

        private static ImmutableArray<string> GetCustomTags(DiagnosticSeverity defaultSeverity)
        {
            return defaultSeverity == DiagnosticSeverity.Error ?
                CompilerErrorCustomTags :
                CompilerNonErrorCustomTags;
        }

        internal bool IsNotConfigurable()
        {
            // Only compiler errors are non-configurable.
            return this.defaultSeverity == DiagnosticSeverity.Error;
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
        public virtual string GetMessage(IFormatProvider formatProvider = null)
        {
            var culture = formatProvider as CultureInfo;
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

            return String.Format(formatProvider, message, GetArgumentsToUse(culture));
        }

        private object[] GetArgumentsToUse(CultureInfo culture)
        {
            object[] argumentsToUse = null;
            for (int i = 0; i < arguments.Length; i++)
            {
                var embedded = arguments[i] as DiagnosticInfo;
                if (embedded != null)
                {
                    argumentsToUse = InitializeArgumentListIfNeeded(argumentsToUse);
                    argumentsToUse[i] = embedded.GetMessage(culture);
                    continue;
                }

                var symbol = arguments[i] as ISymbol;
                if (symbol != null)
                {
                    argumentsToUse = InitializeArgumentListIfNeeded(argumentsToUse);
                    argumentsToUse[i] = this.messageProvider.ConvertSymbolToString(errorCode, symbol);
                }
            }

            return argumentsToUse ?? arguments;
        }

        private object[] InitializeArgumentListIfNeeded(object[] argumentsToUse)
        {
            if (argumentsToUse != null)
            {
                return argumentsToUse;
            }

            var newArguments = new object[arguments.Length];
            Array.Copy(arguments, newArguments, newArguments.Length);

            return newArguments;
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
                this.GetMessage(formatProvider));
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
