// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
        private bool _useHostCompilerIfAvailable = false;

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
            set { _useHostCompilerIfAvailable = value; }
            get { return _useHostCompilerIfAvailable; }
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
                    Log.LogErrorWithCodeFromResources("General.FrameworksFileNotFound", ToolName, ToolLocationHelper.GetDotNetFrameworkVersionFolderPrefix(TargetDotNetFrameworkVersion.VersionLatest));
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
                    Log.LogWarningWithCodeFromResources("Csc.InvalidParameterWarning", "/define:", singleIdentifier);
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
    }
}