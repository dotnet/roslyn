// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;

using Microsoft.Build.Framework;
using Microsoft.Build.Tasks.Hosting;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis.CompilerServer;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    /// <summary>
    /// This class defines the "Csc" XMake task, which enables building assemblies from C#
    /// source files by invoking the C# compiler. This is the new Roslyn XMake task,
    /// meaning that the code is compiled by using the Roslyn compiler server, rather
    /// than csc.exe. The two should be functionally identical, but the compiler server
    /// should be significantly faster with larger projects and have a smaller memory
    /// footprint.
    /// </summary>
    public class Csc : ManagedCompiler
    {
        #region Properties

        // Please keep these alphabetized.  These are the parameters specific to Csc.  The
        // ones shared between Vbc and Csc are defined in ManagedCompiler.cs, which is
        // the base class.

        public bool AllowUnsafeBlocks
        {
            set { _store["AllowUnsafeBlocks"] = value; }
            get { return _store.GetOrDefault("AllowUnsafeBlocks", false); }
        }

        public string ApplicationConfiguration
        {
            set { _store["ApplicationConfiguration"] = value; }
            get { return (string)_store["ApplicationConfiguration"]; }
        }

        public string BaseAddress
        {
            set { _store["BaseAddress"] = value; }
            get { return (string)_store["BaseAddress"]; }
        }

        public bool CheckForOverflowUnderflow
        {
            set { _store["CheckForOverflowUnderflow"] = value; }
            get { return _store.GetOrDefault("CheckForOverflowUnderflow", false); }
        }

        public string DocumentationFile
        {
            set { _store["DocumentationFile"] = value; }
            get { return (string)_store["DocumentationFile"]; }
        }

        public string DisabledWarnings
        {
            set { _store["DisabledWarnings"] = value; }
            get { return (string)_store["DisabledWarnings"]; }
        }

        public bool ErrorEndLocation
        {
            set { _store["ErrorEndLocation"] = value; }
            get { return _store.GetOrDefault("ErrorEndLocation", false); }
        }

        public string ErrorReport
        {
            set { _store["ErrorReport"] = value; }
            get { return (string)_store["ErrorReport"]; }
        }

        public bool GenerateFullPaths
        {
            set { _store["GenerateFullPaths"] = value; }
            get { return _store.GetOrDefault("GenerateFullPaths", false); }
        }

        public string LangVersion
        {
            set { _store["LangVersion"] = value; }
            get { return (string)_store["LangVersion"]; }
        }

        public string ModuleAssemblyName
        {
            set { _store["ModuleAssemblyName"] = value; }
            get { return (string)_store["ModuleAssemblyName"]; }
        }

        public bool NoStandardLib
        {
            set { _store["NoStandardLib"] = value; }
            get { return _store.GetOrDefault("NoStandardLib", false); }
        }

        public string PdbFile
        {
            set { _store["PdbFile"] = value; }
            get { return (string)_store["PdbFile"]; }
        }

        /// <summary>
        /// Name of the language passed to "/preferreduilang" compiler option.
        /// </summary>
        /// <remarks>
        /// If set to null, "/preferreduilang" option is omitted, and csc.exe uses its default setting.
        /// Otherwise, the value is passed to "/preferreduilang" as is.
        /// </remarks>
        public string PreferredUILang
        {
            set { _store["PreferredUILang"] = value; }
            get { return (string)_store["PreferredUILang"]; }
        }

        public string VsSessionGuid
        {
            set { _store["VsSessionGuid"] = value; }
            get { return (string)_store["VsSessionGuid"]; }
        }

        public bool UseHostCompilerIfAvailable
        {
            set { _store[nameof(UseHostCompilerIfAvailable)] = value; }
            get { return _store.GetOrDefault(nameof(UseHostCompilerIfAvailable), false); }
        }

        public int WarningLevel
        {
            set { _store["WarningLevel"] = value; }
            get { return _store.GetOrDefault("WarningLevel", 4); }
        }

        public string WarningsAsErrors
        {
            set { _store["WarningsAsErrors"] = value; }
            get { return (string)_store["WarningsAsErrors"]; }
        }

        public string WarningsNotAsErrors
        {
            set { _store["WarningsNotAsErrors"] = value; }
            get { return (string)_store["WarningsNotAsErrors"]; }
        }

        #endregion

        #region Tool Members

        internal override BuildProtocolConstants.RequestLanguage Language
            => BuildProtocolConstants.RequestLanguage.CSharpCompile;

        private static string[] s_separators = { "\r\n" };

        internal override void LogMessages(string output, MessageImportance messageImportance)
        {
            var lines = output.Split(s_separators, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string trimmedMessage = line.Trim();
                if (trimmedMessage != "")
                {
                    Log.LogMessageFromText(trimmedMessage, messageImportance);
                }
            }
        }

        /// <summary>
        /// Return the name of the tool to execute.
        /// </summary>
        override protected string ToolName
        {
            get
            {
                return "csc2.exe";
            }
        }

        /// <summary>
        /// Return the path to the tool to execute.
        /// </summary>
        override protected string GenerateFullPathToTool()
        {
            string pathToTool = ToolLocationHelper.GetPathToBuildToolsFile(ToolName, ToolLocationHelper.CurrentToolsVersion);

            if (null == pathToTool)
            {
                pathToTool = ToolLocationHelper.GetPathToDotNetFrameworkFile(ToolName, TargetDotNetFrameworkVersion.VersionLatest);

                if (null == pathToTool)
                {
                    Log.LogErrorWithCodeFromResources("General_FrameworksFileNotFound", ToolName, ToolLocationHelper.GetDotNetFrameworkVersionFolderPrefix(TargetDotNetFrameworkVersion.VersionLatest));
                }
            }

            return pathToTool;
        }

        /// <summary>
        /// Fills the provided CommandLineBuilderExtension with those switches and other information that can go into a response file.
        /// </summary>
        override protected internal void AddResponseFileCommands(CommandLineBuilderExtension commandLine)
        {
            commandLine.AppendSwitchIfNotNull("/lib:", this.AdditionalLibPaths, ",");
            commandLine.AppendPlusOrMinusSwitch("/unsafe", this._store, "AllowUnsafeBlocks");
            commandLine.AppendPlusOrMinusSwitch("/checked", this._store, "CheckForOverflowUnderflow");
            commandLine.AppendSwitchWithSplitting("/nowarn:", this.DisabledWarnings, ",", ';', ',');
            commandLine.AppendWhenTrue("/fullpaths", this._store, "GenerateFullPaths");
            commandLine.AppendSwitchIfNotNull("/langversion:", this.LangVersion);
            commandLine.AppendSwitchIfNotNull("/moduleassemblyname:", this.ModuleAssemblyName);
            commandLine.AppendSwitchIfNotNull("/pdb:", this.PdbFile);
            commandLine.AppendPlusOrMinusSwitch("/nostdlib", this._store, "NoStandardLib");
            commandLine.AppendSwitchIfNotNull("/platform:", this.PlatformWith32BitPreference);
            commandLine.AppendSwitchIfNotNull("/errorreport:", this.ErrorReport);
            commandLine.AppendSwitchWithInteger("/warn:", this._store, "WarningLevel");
            commandLine.AppendSwitchIfNotNull("/doc:", this.DocumentationFile);
            commandLine.AppendSwitchIfNotNull("/baseaddress:", this.BaseAddress);
            commandLine.AppendSwitchUnquotedIfNotNull("/define:", this.GetDefineConstantsSwitch(this.DefineConstants));
            commandLine.AppendSwitchIfNotNull("/win32res:", this.Win32Resource);
            commandLine.AppendSwitchIfNotNull("/main:", this.MainEntryPoint);
            commandLine.AppendSwitchIfNotNull("/appconfig:", this.ApplicationConfiguration);
            commandLine.AppendWhenTrue("/errorendlocation", this._store, "ErrorEndLocation");
            commandLine.AppendSwitchIfNotNull("/preferreduilang:", this.PreferredUILang);
            commandLine.AppendPlusOrMinusSwitch("/highentropyva", this._store, "HighEntropyVA");

            // If not design time build and the globalSessionGuid property was set then add a -globalsessionguid:<guid>
            bool designTime = false;
            if (this.HostObject != null)
            {
                var csHost = this.HostObject as ICscHostObject;
                designTime = csHost.IsDesignTime();
            }
            if (!designTime)
            {
                if (!string.IsNullOrWhiteSpace(this.VsSessionGuid))
                {
                    commandLine.AppendSwitchIfNotNull("/sqmsessionguid:", this.VsSessionGuid);
                }
            }

            this.AddReferencesToCommandLine(commandLine);

            base.AddResponseFileCommands(commandLine);

            // This should come after the "TreatWarningsAsErrors" flag is processed (in managedcompiler.cs).
            // Because if TreatWarningsAsErrors=false, then we'll have a /warnaserror- on the command-line,
            // and then any specific warnings that should be treated as errors should be specified with
            // /warnaserror+:<list> after the /warnaserror- switch.  The order of the switches on the command-line
            // does matter.
            //
            // Note that
            //      /warnaserror+
            // is just shorthand for:
            //      /warnaserror+:<all possible warnings>
            //
            // Similarly,
            //      /warnaserror-
            // is just shorthand for:
            //      /warnaserror-:<all possible warnings>
            commandLine.AppendSwitchWithSplitting("/warnaserror+:", this.WarningsAsErrors, ",", ';', ',');
            commandLine.AppendSwitchWithSplitting("/warnaserror-:", this.WarningsNotAsErrors, ",", ';', ',');

            // It's a good idea for the response file to be the very last switch passed, just 
            // from a predictability perspective.  It also solves the problem that a dogfooder
            // ran into, which is described in an email thread attached to bug VSWhidbey 146883.
            // See also bugs 177762 and 118307 for additional bugs related to response file position.
            if (this.ResponseFiles != null)
            {
                foreach (ITaskItem response in this.ResponseFiles)
                {
                    commandLine.AppendSwitchIfNotNull("@", response.ItemSpec);
                }
            }
        }

        #endregion

        /// <summary>
        /// The C# compiler (starting with Whidbey) supports assembly aliasing for references.
        /// See spec at http://devdiv/spectool/Documents/Whidbey/VCSharp/Design%20Time/M3%20DCRs/DCR%20Assembly%20aliases.doc.
        /// This method handles the necessary work of looking at the "Aliases" attribute on
        /// the incoming "References" items, and making sure to generate the correct
        /// command-line on csc.exe.  The syntax for aliasing a reference is:
        ///     csc.exe /reference:Foo=System.Xml.dll
        ///
        /// The "Aliases" attribute on the "References" items is actually a comma-separated
        /// list of aliases, and if any of the aliases specified is the string "global",
        /// then we add that reference to the command-line without an alias.
        /// </summary>
        private void AddReferencesToCommandLine
            (
            CommandLineBuilderExtension commandLine
            )
        {
            // If there were no references passed in, don't add any /reference: switches
            // on the command-line.
            if ((this.References == null) || (this.References.Length == 0))
            {
                return;
            }

            // Loop through all the references passed in.  We'll be adding separate
            // /reference: switches for each reference, and in some cases even multiple
            // /reference: switches per reference.
            foreach (ITaskItem reference in this.References)
            {
                // See if there was an "Alias" attribute on the reference.
                string aliasString = reference.GetMetadata("Aliases");


                string switchName = "/reference:";
                bool embed = Utilities.TryConvertItemMetadataToBool(reference,
                                                                    "EmbedInteropTypes");

                if (embed == true)
                {
                    switchName = "/link:";
                }

                if ((aliasString == null) || (aliasString.Length == 0))
                {
                    // If there was no "Alias" attribute, just add this as a global reference.
                    commandLine.AppendSwitchIfNotNull(switchName, reference.ItemSpec);
                }
                else
                {
                    // If there was an "Alias" attribute, it contains a comma-separated list
                    // of aliases to use for this reference.  For each one of those aliases,
                    // we're going to add a separate /reference: switch to the csc.exe
                    // command-line
                    string[] aliases = aliasString.Split(',');

                    foreach (string alias in aliases)
                    {
                        // Trim whitespace.
                        string trimmedAlias = alias.Trim();

                        if (alias.Length == 0)
                        {
                            continue;
                        }

                        // The alias should be a valid C# identifier.  Therefore it cannot
                        // contain comma, space, semicolon, or double-quote.  Let's check for
                        // the existence of those characters right here, and bail immediately
                        // if any are present.  There are a whole bunch of other characters
                        // that are not allowed in a C# identifier, but we'll just let csc.exe
                        // error out on those.  The ones we're checking for here are the ones
                        // that could seriously screw up the command-line parsing or could
                        // allow parameter injection.
                        if (trimmedAlias.IndexOfAny(new char[] { ',', ' ', ';', '"' }) != -1)
                        {
                            throw Utilities.GetLocalizedArgumentException(
                                ErrorString.Csc_AssemblyAliasContainsIllegalCharacters,
                                reference.ItemSpec,
                                trimmedAlias);
                        }

                        // The alias called "global" is special.  It means that we don't
                        // give it an alias on the command-line.
                        if (String.Compare("global", trimmedAlias, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            commandLine.AppendSwitchIfNotNull(switchName, reference.ItemSpec);
                        }
                        else
                        {
                            // We have a valid (and explicit) alias for this reference.  Add
                            // it to the command-line using the syntax:
                            //      /reference:Foo=System.Xml.dll
                            commandLine.AppendSwitchAliased(switchName, trimmedAlias, reference.ItemSpec);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Old VS projects had some pretty messed-up looking values for the
        /// "DefineConstants" property.  It worked fine in the IDE, because it
        /// effectively munged up the string so that it ended up being valid for
        /// the compiler.  We do the equivalent munging here now.
        /// 
        /// Basically, we take the incoming string, and split it on comma/semicolon/space.
        /// Then we look at the resulting list of strings, and remove any that are
        /// illegal identifiers, and pass the remaining ones through to the compiler.
        /// 
        /// Note that CSharp does support assigning a value to the constants ... in
        /// other words, a constant is either defined or not defined ... it can't have
        /// an actual value.
        /// </summary>
        internal string GetDefineConstantsSwitch(string originalDefineConstants)
        {
            if (originalDefineConstants == null)
            {
                return null;
            }

            StringBuilder finalDefineConstants = new StringBuilder();

            // Split the incoming string on comma/semicolon/space.
            string[] allIdentifiers = originalDefineConstants.Split(new char[] { ',', ';', ' ' });

            // Loop through all the parts, and for the ones that are legal C# identifiers,
            // add them to the outgoing string.
            foreach (string singleIdentifier in allIdentifiers)
            {
                if (SyntaxFacts.IsValidIdentifier(singleIdentifier))
                {
                    // Separate them with a semicolon if there's something already in
                    // the outgoing string.
                    if (finalDefineConstants.Length > 0)
                    {
                        finalDefineConstants.Append(";");
                    }

                    finalDefineConstants.Append(singleIdentifier);
                }
                else if (singleIdentifier.Length > 0)
                {
                    Log.LogWarningWithCodeFromResources("Csc_InvalidParameterWarning", "/define:", singleIdentifier);
                }
            }

            if (finalDefineConstants.Length > 0)
            {
                return finalDefineConstants.ToString();
            }
            else
            {
                // We wouldn't want to pass in an empty /define: switch on the csc.exe command-line.
                return null;
            }
        }

        /// <summary>
        /// This method will initialize the host compiler object with all the switches,
        /// parameters, resources, references, sources, etc.
        ///
        /// It returns true if everything went according to plan.  It returns false if the
        /// host compiler had a problem with one of the parameters that was passed in.
        /// 
        /// This method also sets the "this.HostCompilerSupportsAllParameters" property
        /// accordingly.
        ///
        /// Example:
        ///     If we attempted to pass in WarningLevel="9876", then this method would
        ///     set HostCompilerSupportsAllParameters=true, but it would give a
        ///     return value of "false".  This is because the host compiler fully supports
        ///     the WarningLevel parameter, but 9876 happens to be an illegal value.
        ///
        /// Example:
        ///     If we attempted to pass in NoConfig=false, then this method would set
        ///     HostCompilerSupportsAllParameters=false, because while this is a legal
        ///     thing for csc.exe, the IDE compiler cannot support it.  In this situation
        ///     the return value will also be false.
        /// </summary>
        /// <owner>RGoel</owner>
        private bool InitializeHostCompiler
            (
            // NOTE: For compat reasons this must remain ICscHostObject
            // we can dynamically test for smarter interfaces later..
            ICscHostObject cscHostObject
            )
        {
            bool success;
            this.HostCompilerSupportsAllParameters = this.UseHostCompilerIfAvailable;
            string param = "Unknown";

            try
            {
                // Need to set these separately, because they don't require a CommitChanges to the C# compiler in the IDE.
                param = "LinkResources"; this.CheckHostObjectSupport(param, cscHostObject.SetLinkResources(this.LinkResources));
                param = "References"; this.CheckHostObjectSupport(param, cscHostObject.SetReferences(this.References));
                param = "Resources"; this.CheckHostObjectSupport(param, cscHostObject.SetResources(this.Resources));
                param = "Sources"; this.CheckHostObjectSupport(param, cscHostObject.SetSources(this.Sources));

                // For host objects which support it, pass the list of analyzers.
                IAnalyzerHostObject analyzerHostObject = cscHostObject as IAnalyzerHostObject;
                if (analyzerHostObject != null)
                {
                    param = "Analyzers"; this.CheckHostObjectSupport(param, analyzerHostObject.SetAnalyzers(this.Analyzers));
                }
            }
            catch (Exception e) when (!Utilities.IsCriticalException(e))
            {
                if (this.HostCompilerSupportsAllParameters)
                {
                    // If the host compiler doesn't support everything we need, we're going to end up 
                    // shelling out to the command-line compiler anyway.  That means the command-line
                    // compiler will log the error.  So here, we only log the error if we would've
                    // tried to use the host compiler.
                    Log.LogErrorWithCodeFromResources("General_CouldNotSetHostObjectParameter", param, e.Message);
                }
                return false;
            }

            try
            {
                param = "BeginInitialization";
                cscHostObject.BeginInitialization();

                param = "AdditionalLibPaths"; this.CheckHostObjectSupport(param, cscHostObject.SetAdditionalLibPaths(this.AdditionalLibPaths));
                param = "AddModules"; this.CheckHostObjectSupport(param, cscHostObject.SetAddModules(this.AddModules));
                param = "AllowUnsafeBlocks"; this.CheckHostObjectSupport(param, cscHostObject.SetAllowUnsafeBlocks(this.AllowUnsafeBlocks));
                param = "BaseAddress"; this.CheckHostObjectSupport(param, cscHostObject.SetBaseAddress(this.BaseAddress));
                param = "CheckForOverflowUnderflow"; this.CheckHostObjectSupport(param, cscHostObject.SetCheckForOverflowUnderflow(this.CheckForOverflowUnderflow));
                param = "CodePage"; this.CheckHostObjectSupport(param, cscHostObject.SetCodePage(this.CodePage));

                // These two -- EmitDebugInformation and DebugType -- must go together, with DebugType 
                // getting set last, because it is more specific.
                param = "EmitDebugInformation"; this.CheckHostObjectSupport(param, cscHostObject.SetEmitDebugInformation(this.EmitDebugInformation));
                param = "DebugType"; this.CheckHostObjectSupport(param, cscHostObject.SetDebugType(this.DebugType));

                param = "DefineConstants"; this.CheckHostObjectSupport(param, cscHostObject.SetDefineConstants(this.GetDefineConstantsSwitch(this.DefineConstants)));
                param = "DelaySign"; this.CheckHostObjectSupport(param, cscHostObject.SetDelaySign((_store["DelaySign"] != null), this.DelaySign));
                param = "DisabledWarnings"; this.CheckHostObjectSupport(param, cscHostObject.SetDisabledWarnings(this.DisabledWarnings));
                param = "DocumentationFile"; this.CheckHostObjectSupport(param, cscHostObject.SetDocumentationFile(this.DocumentationFile));
                param = "ErrorReport"; this.CheckHostObjectSupport(param, cscHostObject.SetErrorReport(this.ErrorReport));
                param = "FileAlignment"; this.CheckHostObjectSupport(param, cscHostObject.SetFileAlignment(this.FileAlignment));
                param = "GenerateFullPaths"; this.CheckHostObjectSupport(param, cscHostObject.SetGenerateFullPaths(this.GenerateFullPaths));
                param = "KeyContainer"; this.CheckHostObjectSupport(param, cscHostObject.SetKeyContainer(this.KeyContainer));
                param = "KeyFile"; this.CheckHostObjectSupport(param, cscHostObject.SetKeyFile(this.KeyFile));
                param = "LangVersion"; this.CheckHostObjectSupport(param, cscHostObject.SetLangVersion(this.LangVersion));
                param = "MainEntryPoint"; this.CheckHostObjectSupport(param, cscHostObject.SetMainEntryPoint(this.TargetType, this.MainEntryPoint));
                param = "ModuleAssemblyName"; this.CheckHostObjectSupport(param, cscHostObject.SetModuleAssemblyName(this.ModuleAssemblyName));
                param = "NoConfig"; this.CheckHostObjectSupport(param, cscHostObject.SetNoConfig(this.NoConfig));
                param = "NoStandardLib"; this.CheckHostObjectSupport(param, cscHostObject.SetNoStandardLib(this.NoStandardLib));
                param = "Optimize"; this.CheckHostObjectSupport(param, cscHostObject.SetOptimize(this.Optimize));
                param = "OutputAssembly"; this.CheckHostObjectSupport(param, cscHostObject.SetOutputAssembly(this.OutputAssembly.ItemSpec));
                param = "PdbFile"; this.CheckHostObjectSupport(param, cscHostObject.SetPdbFile(this.PdbFile));

                // For host objects which support it, set platform with 32BitPreference, HighEntropyVA, and SubsystemVersion
                ICscHostObject4 cscHostObject4 = cscHostObject as ICscHostObject4;
                if (cscHostObject4 != null)
                {
                    param = "PlatformWith32BitPreference"; this.CheckHostObjectSupport(param, cscHostObject4.SetPlatformWith32BitPreference(this.PlatformWith32BitPreference));
                    param = "HighEntropyVA"; this.CheckHostObjectSupport(param, cscHostObject4.SetHighEntropyVA(this.HighEntropyVA));
                    param = "SubsystemVersion"; this.CheckHostObjectSupport(param, cscHostObject4.SetSubsystemVersion(this.SubsystemVersion));
                }
                else
                {
                    param = "Platform"; this.CheckHostObjectSupport(param, cscHostObject.SetPlatform(this.Platform));
                }

                // For host objects which support it, set the analyzer ruleset and additional files.
                IAnalyzerHostObject analyzerHostObject = cscHostObject as IAnalyzerHostObject;
                if (analyzerHostObject != null)
                {
                    param = "CodeAnalysisRuleSet"; this.CheckHostObjectSupport(param, analyzerHostObject.SetRuleSet(this.CodeAnalysisRuleSet));
                    param = "AdditionalFiles"; this.CheckHostObjectSupport(param, analyzerHostObject.SetAdditionalFiles(this.AdditionalFiles));
                }

                ICscHostObject5 cscHostObject5 = cscHostObject as ICscHostObject5;
                if (cscHostObject5 != null)
                {
                    param = "ErrorLog"; this.CheckHostObjectSupport(param, cscHostObject5.SetErrorLog(this.ErrorLog));
                    param = "ReportAnalyzer"; this.CheckHostObjectSupport(param, cscHostObject5.SetReportAnalyzer(this.ReportAnalyzer));
                }

                param = "ResponseFiles"; this.CheckHostObjectSupport(param, cscHostObject.SetResponseFiles(this.ResponseFiles));
                param = "TargetType"; this.CheckHostObjectSupport(param, cscHostObject.SetTargetType(this.TargetType));
                param = "TreatWarningsAsErrors"; this.CheckHostObjectSupport(param, cscHostObject.SetTreatWarningsAsErrors(this.TreatWarningsAsErrors));
                param = "WarningLevel"; this.CheckHostObjectSupport(param, cscHostObject.SetWarningLevel(this.WarningLevel));
                // This must come after TreatWarningsAsErrors.
                param = "WarningsAsErrors"; this.CheckHostObjectSupport(param, cscHostObject.SetWarningsAsErrors(this.WarningsAsErrors));
                // This must come after TreatWarningsAsErrors.
                param = "WarningsNotAsErrors"; this.CheckHostObjectSupport(param, cscHostObject.SetWarningsNotAsErrors(this.WarningsNotAsErrors));
                param = "Win32Icon"; this.CheckHostObjectSupport(param, cscHostObject.SetWin32Icon(this.Win32Icon));

                // In order to maintain compatibility with previous host compilers, we must
                // light-up for ICscHostObject2/ICscHostObject3

                if (cscHostObject is ICscHostObject2)
                {
                    ICscHostObject2 cscHostObject2 = (ICscHostObject2)cscHostObject;
                    param = "Win32Manifest"; this.CheckHostObjectSupport(param, cscHostObject2.SetWin32Manifest(this.GetWin32ManifestSwitch(this.NoWin32Manifest, this.Win32Manifest)));
                }
                else
                {
                    // If we have been given a property that the host compiler doesn't support
                    // then we need to state that we are falling back to the command line compiler
                    if (!String.IsNullOrEmpty(Win32Manifest))
                    {
                        this.CheckHostObjectSupport("Win32Manifest", false);
                    }
                }

                // This must come after Win32Manifest
                param = "Win32Resource"; this.CheckHostObjectSupport(param, cscHostObject.SetWin32Resource(this.Win32Resource));

                if (cscHostObject is ICscHostObject3)
                {
                    ICscHostObject3 cscHostObject3 = (ICscHostObject3)cscHostObject;
                    param = "ApplicationConfiguration"; this.CheckHostObjectSupport(param, cscHostObject3.SetApplicationConfiguration(this.ApplicationConfiguration));
                }
                else
                {
                    // If we have been given a property that the host compiler doesn't support
                    // then we need to state that we are falling back to the command line compiler
                    if (!String.IsNullOrEmpty(ApplicationConfiguration))
                    {
                        this.CheckHostObjectSupport("ApplicationConfiguration", false);
                    }
                }

                // If we have been given a property value that the host compiler doesn't support
                // then we need to state that we are falling back to the command line compiler.
                // Null is supported because it means that option should be omitted, and compiler default used - obviously always valid.
                // Explicitly specified name of current locale is also supported, since it is effectively a no-op.
                // Other options are not supported since in-proc compiler always uses current locale.
                if (!String.IsNullOrEmpty(PreferredUILang) && !String.Equals(PreferredUILang, System.Globalization.CultureInfo.CurrentUICulture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    this.CheckHostObjectSupport("PreferredUILang", false);
                }
            }
            catch (Exception e) when (!Utilities.IsCriticalException(e))
            {
                if (this.HostCompilerSupportsAllParameters)
                {
                    // If the host compiler doesn't support everything we need, we're going to end up 
                    // shelling out to the command-line compiler anyway.  That means the command-line
                    // compiler will log the error.  So here, we only log the error if we would've
                    // tried to use the host compiler.
                    Log.LogErrorWithCodeFromResources("General_CouldNotSetHostObjectParameter", param, e.Message);
                }
                return false;
            }
            finally
            {
                int errorCode;
                string errorMessage;

                success = cscHostObject.EndInitialization(out errorMessage, out errorCode);

                if (this.HostCompilerSupportsAllParameters)
                {
                    // If the host compiler doesn't support everything we need, we're going to end up 
                    // shelling out to the command-line compiler anyway.  That means the command-line
                    // compiler will log the error.  So here, we only log the error if we would've
                    // tried to use the host compiler.

                    // If EndInitialization returns false, then there was an error. If EndInitialization was 
                    // successful, but there is a valid 'errorMessage,' interpret it as a warning.

                    if (!success)
                    {
                        Log.LogError(null, "CS" + errorCode.ToString("D4", CultureInfo.InvariantCulture), null, null, 0, 0, 0, 0, errorMessage);
                    }
                    else if (errorMessage != null && errorMessage.Length > 0)
                    {
                        Log.LogWarning(null, "CS" + errorCode.ToString("D4", CultureInfo.InvariantCulture), null, null, 0, 0, 0, 0, errorMessage);
                    }
                }
            }

            return (success);
        }

        /// <summary>
        /// This method will get called during Execute() if a host object has been passed into the Csc
        /// task.  Returns one of the following values to indicate what the next action should be:
        ///     UseHostObjectToExecute          Host compiler exists and was initialized.
        ///     UseAlternateToolToExecute       Host compiler doesn't exist or was not appropriate.
        ///     NoActionReturnSuccess           Host compiler was already up-to-date, and we're done.
        ///     NoActionReturnFailure           Bad parameters were passed into the task.
        /// </summary>
        /// <owner>RGoel</owner>
        override protected HostObjectInitializationStatus InitializeHostObject()
        {
            if (this.HostObject != null)
            {
                // When the host object was passed into the task, it was passed in as a generic
                // "Object" (because ITask interface obviously can't have any Csc-specific stuff
                // in it, and each task is going to want to communicate with its host in a unique
                // way).  Now we cast it to the specific type that the Csc task expects.  If the
                // host object does not match this type, the host passed in an invalid host object
                // to Csc, and we error out.

                // NOTE: For compat reasons this must remain ICscHostObject
                // we can dynamically test for smarter interfaces later..
                using (RCWForCurrentContext<ICscHostObject> hostObject = new RCWForCurrentContext<ICscHostObject>(this.HostObject as ICscHostObject))
                {
                    ICscHostObject cscHostObject = hostObject.RCW;

                    if (cscHostObject != null)
                    {
                        bool hostObjectSuccessfullyInitialized = InitializeHostCompiler(cscHostObject);

                        // If we're currently only in design-time (as opposed to build-time),
                        // then we're done.  We've initialized the host compiler as best we
                        // can, and we certainly don't want to actually do the final compile.
                        // So return true, saying we're done and successful.
                        if (cscHostObject.IsDesignTime())
                        {
                            // If we are design-time then we do not want to continue the build at 
                            // this time.
                            return hostObjectSuccessfullyInitialized ?
                                HostObjectInitializationStatus.NoActionReturnSuccess :
                                HostObjectInitializationStatus.NoActionReturnFailure;
                        }

                        // Roslyn does not support compiling through the host object

                        // Since the host compiler has refused to take on the responsibility for this compilation,
                        // we're about to shell out to the command-line compiler to handle it.  If some of the
                        // references don't exist on disk, we know the command-line compiler will fail, so save
                        // the trouble, and just throw a consistent error ourselves.  This allows us to give
                        // more information than the compiler would, and also make things consistent across
                        // Vbc / Csc / etc.  Actually, the real reason is bug 275726 (ddsuites\src\vs\env\vsproject\refs\ptp3).
                        // This suite behaves differently in localized builds than on English builds because 
                        // VBC.EXE doesn't localize the word "error" when they emit errors and so we can't scan for it.
                        if (!CheckAllReferencesExistOnDisk())
                        {
                            return HostObjectInitializationStatus.NoActionReturnFailure;
                        }

                        UsedCommandLineTool = true;
                        return HostObjectInitializationStatus.UseAlternateToolToExecute;
                    }
                    else
                    {
                        Log.LogErrorWithCodeFromResources("General_IncorrectHostObject", "Csc", "ICscHostObject");
                    }
                }
            }

            // No appropriate host object was found.
            UsedCommandLineTool = true;
            return HostObjectInitializationStatus.UseAlternateToolToExecute;
        }

        /// <summary>
        /// This method will get called during Execute() if a host object has been passed into the Csc
        /// task.  Returns true if the compilation succeeded, otherwise false.  
        /// </summary>
        /// <owner>RGoel</owner>
        override protected bool CallHostObjectToExecute()
        {
            Debug.Assert(this.HostObject != null, "We should not be here if the host object has not been set.");

            ICscHostObject cscHostObject = this.HostObject as ICscHostObject;
            Debug.Assert(cscHostObject != null, "Wrong kind of host object passed in!");
            return cscHostObject.Compile();
        }
    }
}