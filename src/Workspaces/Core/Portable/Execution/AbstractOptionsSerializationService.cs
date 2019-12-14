// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    internal abstract class AbstractOptionsSerializationService : IOptionsSerializationService
    {
        public abstract void WriteTo(CompilationOptions options, ObjectWriter writer, CancellationToken cancellationToken);
        public abstract void WriteTo(ParseOptions options, ObjectWriter writer, CancellationToken cancellationToken);
        public abstract void WriteTo(OptionSet options, ObjectWriter writer, CancellationToken cancellationToken);

        public abstract CompilationOptions ReadCompilationOptionsFrom(ObjectReader reader, CancellationToken cancellationToken);
        public abstract ParseOptions ReadParseOptionsFrom(ObjectReader reader, CancellationToken cancellationToken);
        public abstract OptionSet ReadOptionSetFrom(ObjectReader reader, CancellationToken cancellationToken);

        protected void WriteCompilationOptionsTo(CompilationOptions options, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            writer.WriteInt32((int)options.OutputKind);
            writer.WriteBoolean(options.ReportSuppressedDiagnostics);
            writer.WriteString(options.ModuleName);
            writer.WriteString(options.MainTypeName);
            writer.WriteString(options.ScriptClassName);
            writer.WriteInt32((int)options.OptimizationLevel);
            writer.WriteBoolean(options.CheckOverflow);

            // REVIEW: is it okay this being not part of snapshot?
            writer.WriteString(options.CryptoKeyContainer);
            writer.WriteString(options.CryptoKeyFile);

            writer.WriteValue(options.CryptoPublicKey.ToArray());
            writer.WriteBoolean(options.DelaySign.HasValue);
            if (options.DelaySign.HasValue)
            {
                writer.WriteBoolean(options.DelaySign.Value);
            }

            writer.WriteInt32((int)options.Platform);
            writer.WriteInt32((int)options.GeneralDiagnosticOption);

            writer.WriteInt32(options.WarningLevel);

            // REVIEW: I don't think there is a guarantee on ordering of elements in the immutable dictionary.
            //         unfortunately, we need to sort them to make it deterministic
            writer.WriteInt32(options.SpecificDiagnosticOptions.Count);
            foreach (var kv in options.SpecificDiagnosticOptions.OrderBy(o => o.Key))
            {
                writer.WriteString(kv.Key);
                writer.WriteInt32((int)kv.Value);
            }

            writer.WriteBoolean(options.ConcurrentBuild);
            writer.WriteBoolean(options.Deterministic);
            writer.WriteBoolean(options.PublicSign);

            writer.WriteByte((byte)options.MetadataImportOptions);

            // REVIEW: What should I do with these. we probably need to implement either out own one
            //         or somehow share these as service....
            //
            // XmlReferenceResolver xmlReferenceResolver
            // SourceReferenceResolver sourceReferenceResolver
            // MetadataReferenceResolver metadataReferenceResolver
            // AssemblyIdentityComparer assemblyIdentityComparer
            // StrongNameProvider strongNameProvider
        }

        protected void ReadCompilationOptionsFrom(
            ObjectReader reader,
            out OutputKind outputKind,
            out bool reportSuppressedDiagnostics,
            out string moduleName,
            out string mainTypeName,
            out string scriptClassName,
            out OptimizationLevel optimizationLevel,
            out bool checkOverflow,
            out string cryptoKeyContainer,
            out string cryptoKeyFile,
            out ImmutableArray<byte> cryptoPublicKey,
            out bool? delaySign,
            out Platform platform,
            out ReportDiagnostic generalDiagnosticOption,
            out int warningLevel,
            out IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions,
            out bool concurrentBuild,
            out bool deterministic,
            out bool publicSign,
            out MetadataImportOptions metadataImportOptions,
            out XmlReferenceResolver xmlReferenceResolver,
            out SourceReferenceResolver sourceReferenceResolver,
            out MetadataReferenceResolver metadataReferenceResolver,
            out AssemblyIdentityComparer assemblyIdentityComparer,
            out StrongNameProvider strongNameProvider,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            outputKind = (OutputKind)reader.ReadInt32();
            reportSuppressedDiagnostics = reader.ReadBoolean();
            moduleName = reader.ReadString();
            mainTypeName = reader.ReadString();

            scriptClassName = reader.ReadString();
            optimizationLevel = (OptimizationLevel)reader.ReadInt32();
            checkOverflow = reader.ReadBoolean();

            // REVIEW: is it okay this being not part of snapshot?
            cryptoKeyContainer = reader.ReadString();
            cryptoKeyFile = reader.ReadString();

            cryptoPublicKey = reader.ReadArray<byte>().ToImmutableArrayOrEmpty();

            delaySign = reader.ReadBoolean() ? (bool?)reader.ReadBoolean() : null;

            platform = (Platform)reader.ReadInt32();
            generalDiagnosticOption = (ReportDiagnostic)reader.ReadInt32();

            warningLevel = reader.ReadInt32();

            // REVIEW: I don't think there is a guarantee on ordering of elements in the immutable dictionary.
            //         unfortunately, we need to sort them to make it deterministic
            //         not sure why CompilationOptions uses SequencialEqual to check options equality
            //         when ordering can change result of it even if contents are same.
            var count = reader.ReadInt32();
            List<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptionsList = null;

            if (count > 0)
            {
                specificDiagnosticOptionsList = new List<KeyValuePair<string, ReportDiagnostic>>(count);

                for (var i = 0; i < count; i++)
                {
                    var key = reader.ReadString();
                    var value = (ReportDiagnostic)reader.ReadInt32();

                    specificDiagnosticOptionsList.Add(KeyValuePairUtil.Create(key, value));
                }
            }

            specificDiagnosticOptions = specificDiagnosticOptionsList ?? SpecializedCollections.EmptyEnumerable<KeyValuePair<string, ReportDiagnostic>>();

            concurrentBuild = reader.ReadBoolean();
            deterministic = reader.ReadBoolean();
            publicSign = reader.ReadBoolean();

            metadataImportOptions = (MetadataImportOptions)reader.ReadByte();

            // REVIEW: What should I do with these. are these service required when compilation is built ourselves, not through
            //         compiler.
            xmlReferenceResolver = XmlFileResolver.Default;
            sourceReferenceResolver = SourceFileResolver.Default;
            metadataReferenceResolver = null;
            assemblyIdentityComparer = DesktopAssemblyIdentityComparer.Default;
            strongNameProvider = new DesktopStrongNameProvider();
        }

        protected void WriteParseOptionsTo(ParseOptions options, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            writer.WriteInt32((int)options.Kind);
            writer.WriteInt32((int)options.DocumentationMode);

            // REVIEW: I don't think there is a guarantee on ordering of elements in the readonly dictionary.
            //         unfortunately, we need to sort them to make it deterministic
            writer.WriteInt32(options.Features.Count);
            foreach (var kv in options.Features.OrderBy(o => o.Key))
            {
                writer.WriteString(kv.Key);
                writer.WriteString(kv.Value);
            }
        }

        protected void ReadParseOptionsFrom(
            ObjectReader reader,
            out SourceCodeKind kind,
            out DocumentationMode documentationMode,
            out IEnumerable<KeyValuePair<string, string>> features,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            kind = (SourceCodeKind)reader.ReadInt32();
            documentationMode = (DocumentationMode)reader.ReadInt32();

            // REVIEW: I don't think there is a guarantee on ordering of elements in the immutable dictionary.
            //         unfortunately, we need to sort them to make it deterministic
            //         not sure why ParseOptions uses SequencialEqual to check options equality
            //         when ordering can change result of it even if contents are same.
            var count = reader.ReadInt32();
            List<KeyValuePair<string, string>> featuresList = null;

            if (count > 0)
            {
                featuresList = new List<KeyValuePair<string, string>>(count);

                for (var i = 0; i < count; i++)
                {
                    var key = reader.ReadString();
                    var value = reader.ReadString();

                    featuresList.Add(KeyValuePairUtil.Create(key, value));
                }
            }

            features = featuresList ?? SpecializedCollections.EmptyEnumerable<KeyValuePair<string, string>>();
        }

        protected void WriteOptionSetTo(OptionSet options, string language, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // order of options we serialize should be static so that we get deterministic checksum for options
            WriteOptionTo(options, language, CodeStyleOptions.QualifyFieldAccess, writer, cancellationToken);
            WriteOptionTo(options, language, CodeStyleOptions.QualifyPropertyAccess, writer, cancellationToken);
            WriteOptionTo(options, language, CodeStyleOptions.QualifyMethodAccess, writer, cancellationToken);
            WriteOptionTo(options, language, CodeStyleOptions.QualifyEventAccess, writer, cancellationToken);
            WriteOptionTo(options, language, CodeStyleOptions.PreferAutoProperties, writer, cancellationToken);
            WriteOptionTo(options, language, CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, writer, cancellationToken);
            WriteOptionTo(options, language, CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, writer, cancellationToken);
            WriteOptionTo(options, language, CodeStyleOptions.PreferCoalesceExpression, writer, cancellationToken);
            WriteOptionTo(options, language, CodeStyleOptions.PreferCollectionInitializer, writer, cancellationToken);
            WriteOptionTo(options, language, CodeStyleOptions.PreferExplicitTupleNames, writer, cancellationToken);
            WriteOptionTo(options, language, CodeStyleOptions.PreferNullPropagation, writer, cancellationToken);
            WriteOptionTo(options, language, CodeStyleOptions.PreferObjectInitializer, writer, cancellationToken);
            WriteOptionTo(options, language, CodeStyleOptions.RequireAccessibilityModifiers, writer, cancellationToken);
            WriteOptionTo(options, language, CodeStyleOptions.ArithmeticBinaryParentheses, writer, cancellationToken);
            WriteOptionTo(options, language, CodeStyleOptions.RelationalBinaryParentheses, writer, cancellationToken);
            WriteOptionTo(options, language, CodeStyleOptions.OtherBinaryParentheses, writer, cancellationToken);
            WriteOptionTo(options, language, CodeStyleOptions.OtherParentheses, writer, cancellationToken);
            WriteOptionTo(options, language, SimplificationOptions.NamingPreferences, writer, cancellationToken);
            WriteOptionTo(options, language, CodeStyleOptions.PreferInferredTupleNames, writer, cancellationToken);
            WriteOptionTo(options, language, CodeStyleOptions.PreferInferredAnonymousTypeMemberNames, writer, cancellationToken);
            WriteOptionTo(options, language, CodeStyleOptions.PreferReadonly, writer, cancellationToken);
        }

        protected OptionSet ReadOptionSetFrom(OptionSet options, string language, ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            options = ReadOptionFrom(options, language, CodeStyleOptions.QualifyFieldAccess, reader, cancellationToken);
            options = ReadOptionFrom(options, language, CodeStyleOptions.QualifyPropertyAccess, reader, cancellationToken);
            options = ReadOptionFrom(options, language, CodeStyleOptions.QualifyMethodAccess, reader, cancellationToken);
            options = ReadOptionFrom(options, language, CodeStyleOptions.QualifyEventAccess, reader, cancellationToken);
            options = ReadOptionFrom(options, language, CodeStyleOptions.PreferAutoProperties, reader, cancellationToken);
            options = ReadOptionFrom(options, language, CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, reader, cancellationToken);
            options = ReadOptionFrom(options, language, CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, reader, cancellationToken);
            options = ReadOptionFrom(options, language, CodeStyleOptions.PreferCoalesceExpression, reader, cancellationToken);
            options = ReadOptionFrom(options, language, CodeStyleOptions.PreferCollectionInitializer, reader, cancellationToken);
            options = ReadOptionFrom(options, language, CodeStyleOptions.PreferExplicitTupleNames, reader, cancellationToken);
            options = ReadOptionFrom(options, language, CodeStyleOptions.PreferNullPropagation, reader, cancellationToken);
            options = ReadOptionFrom(options, language, CodeStyleOptions.PreferObjectInitializer, reader, cancellationToken);
            options = ReadOptionFrom(options, language, CodeStyleOptions.RequireAccessibilityModifiers, reader, cancellationToken);
            options = ReadOptionFrom(options, language, CodeStyleOptions.ArithmeticBinaryParentheses, reader, cancellationToken);
            options = ReadOptionFrom(options, language, CodeStyleOptions.RelationalBinaryParentheses, reader, cancellationToken);
            options = ReadOptionFrom(options, language, CodeStyleOptions.OtherBinaryParentheses, reader, cancellationToken);
            options = ReadOptionFrom(options, language, CodeStyleOptions.OtherParentheses, reader, cancellationToken);
            options = ReadOptionFrom(options, language, SimplificationOptions.NamingPreferences, reader, cancellationToken);
            options = ReadOptionFrom(options, language, CodeStyleOptions.PreferInferredTupleNames, reader, cancellationToken);
            options = ReadOptionFrom(options, language, CodeStyleOptions.PreferInferredAnonymousTypeMemberNames, reader, cancellationToken);
            options = ReadOptionFrom(options, language, CodeStyleOptions.PreferReadonly, reader, cancellationToken);
            return options;
        }

        protected void WriteOptionTo<T>(OptionSet options, Option<CodeStyleOption<T>> option, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var value = options.GetOption(option);
            writer.WriteString(value.ToXElement().ToString());
        }

        protected OptionSet ReadOptionFrom<T>(OptionSet options, Option<CodeStyleOption<T>> option, ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var xmlText = reader.ReadString();
            var value = CodeStyleOption<T>.FromXElement(XElement.Parse(xmlText));

            return options.WithChangedOption(option, value);
        }

        private void WriteOptionTo<T>(OptionSet options, string language, PerLanguageOption<CodeStyleOption<T>> option, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var value = options.GetOption(option, language);
            writer.WriteString(value.ToXElement().ToString());
        }

        private OptionSet ReadOptionFrom<T>(OptionSet options, string language, PerLanguageOption<CodeStyleOption<T>> option, ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var xmlText = reader.ReadString();
            var value = CodeStyleOption<T>.FromXElement(XElement.Parse(xmlText));

            return options.WithChangedOption(option, language, value);
        }

        protected void WriteOptionTo(OptionSet options, Option<NamingStylePreferences> option, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var value = options.GetOption(option);
            writer.WriteString(value.CreateXElement().ToString());
        }

        protected OptionSet ReadOptionFrom(OptionSet options, Option<NamingStylePreferences> option, ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var xmlText = reader.ReadString();
            try
            {
                var value = NamingStylePreferences.FromXElement(XElement.Parse(xmlText));
                return options.WithChangedOption(option, value);
            }
            catch (System.Exception)
            {
                return options;
            }
        }

        private void WriteOptionTo(OptionSet options, string language, PerLanguageOption<NamingStylePreferences> option, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var value = options.GetOption(option, language);
            writer.WriteString(value.CreateXElement().ToString());
        }

        private OptionSet ReadOptionFrom(OptionSet options, string language, PerLanguageOption<NamingStylePreferences> option, ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var xmlText = reader.ReadString();
            try
            {
                var value = NamingStylePreferences.FromXElement(XElement.Parse(xmlText));
                return options.WithChangedOption(option, language, value);

            }
            catch (System.Exception)
            {
                return options;
            }
        }

        /// <summary>
        /// this is not real option set. it doesn't have all options defined in host. but only those
        /// we pre-selected.
        /// </summary>
        protected class SerializedPartialOptionSet : OptionSet
        {
            private readonly ImmutableDictionary<OptionKey, object> _values;

            public SerializedPartialOptionSet()
            {
                _values = ImmutableDictionary<OptionKey, object>.Empty;
            }

            private SerializedPartialOptionSet(ImmutableDictionary<OptionKey, object> values)
            {
                _values = values;
            }

            public override object GetOption(OptionKey optionKey)
            {
                Contract.ThrowIfFalse(_values.TryGetValue(optionKey, out var value));

                return value;
            }

            public override OptionSet WithChangedOption(OptionKey optionAndLanguage, object value)
            {
                return new SerializedPartialOptionSet(_values.SetItem(optionAndLanguage, value));
            }

            internal override IEnumerable<OptionKey> GetChangedOptions(OptionSet optionSet)
            {
                foreach (var kvp in _values)
                {
                    var currentValue = optionSet.GetOption(kvp.Key);
                    if (!object.Equals(currentValue, kvp.Value))
                    {
                        yield return kvp.Key;
                    }
                }
            }
        }
    }
}
