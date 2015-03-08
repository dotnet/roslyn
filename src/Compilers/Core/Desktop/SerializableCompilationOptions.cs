// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents compilation options common to C# and VB.
    /// </summary>
    [Serializable]
    public abstract class SerializableCompilationOptions : ISerializable
    {
        internal const string OutputKindString = "OutputKind";
        internal const string ModuleNameString = "ModuleName";
        internal const string MainTypeNameString = "MainTypeName";
        internal const string ScriptClassNameString = "ScriptClassName";
        internal const string CryptoKeyContainerString = "CryptoKeyContainer";
        internal const string CryptoKeyFileString = "CryptoKeyFile";
        internal const string CryptoPublicKeyString = "CryptoPublicKey";
        internal const string DelaySignString = "DelaySign";
        internal const string CheckOverflowString = "CheckOverflow";
        internal const string PlatformString = "Platform";
        internal const string GeneralDiagnosticOptionString = "GeneralDiagnosticOption";
        internal const string WarningLevelString = "WarningLevel";
        internal const string SpecificDiagnosticOptionsString = "SpecificDiagnosticOptions";
        internal const string DebugInformationKindString = "DebugInformationKind";
        internal const string OptimizeString = "Optimize";
        internal const string ConcurrentBuildString = "ConcurrentBuild";
        internal const string ExtendedCustomDebugInformationString = "ExtendedCustomDebugInformation";
        internal const string MetadataImportOptionsString = "MetadataImportOptions";
        internal const string FeaturesString = "Features";

        internal SerializableCompilationOptions()
        {
        }

        protected static void CommonGetObjectData(CompilationOptions options, SerializationInfo info, StreamingContext context)
        {
            info.AddValue(OutputKindString, (int)options.OutputKind);
            info.AddValue(ModuleNameString, options.ModuleName);
            info.AddValue(MainTypeNameString, options.MainTypeName);
            info.AddValue(ScriptClassNameString, options.ScriptClassName);
            info.AddValue(CryptoKeyContainerString, options.CryptoKeyContainer);
            info.AddValue(CryptoKeyFileString, options.CryptoKeyFile);
            info.AddValue(CryptoPublicKeyString, options.CryptoPublicKey.ToArray());
            info.AddValue(DelaySignString, options.DelaySign);
            info.AddValue(CheckOverflowString, options.CheckOverflow);
            info.AddValue(PlatformString, (int)options.Platform);
            info.AddValue(GeneralDiagnosticOptionString, (int)options.GeneralDiagnosticOption);
            info.AddValue(WarningLevelString, options.WarningLevel);
            info.AddValue(SpecificDiagnosticOptionsString, new Dictionary<string, ReportDiagnostic>(options.SpecificDiagnosticOptions));
            info.AddValue(OptimizeString, (int)options.OptimizationLevel);
            info.AddValue(ConcurrentBuildString, options.ConcurrentBuild);
            info.AddValue(ExtendedCustomDebugInformationString, options.ExtendedCustomDebugInformation);
            info.AddValue(MetadataImportOptionsString, (byte)options.MetadataImportOptions);
            info.AddValue(FeaturesString, options.Features.ToArray());
        }

        public CompilationOptions Options { get { return CommonOptions; } }
        protected abstract CompilationOptions CommonOptions { get; }

        public abstract void GetObjectData(SerializationInfo info, StreamingContext context);
    }
}
