// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        protected const string OutputKindString = "OutputKind";
        protected const string ModuleNameString = "ModuleName";
        protected const string MainTypeNameString = "MainTypeName";
        protected const string ScriptClassNameString = "ScriptClassName";
        protected const string CryptoKeyContainerString = "CryptoKeyContainer";
        protected const string CryptoKeyFileString = "CryptoKeyFile";
        protected const string DelaySignString = "DelaySign";
        protected const string CheckOverflowString = "CheckOverflow";
        protected const string PlatformString = "Platform";
        protected const string GeneralDiagnosticOptionString = "GeneralDiagnosticOption";
        protected const string WarningLevelString = "WarningLevel";
        protected const string SpecificDiagnosticOptionsString = "SpecificDiagnosticOptions";
        protected const string DebugInformationKindString = "DebugInformationKind";
        protected const string OptimizeString = "Optimize";
        protected const string ConcurrentBuildString = "ConcurrentBuild";
        internal const string ExtendedCustomDebugInformationString = "ExtendedCustomDebugInformation";
        protected const string MetadataImportOptionsString = "MetadataImportOptions";
        protected const string FeaturesString = "Features";

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
