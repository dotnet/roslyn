using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Roslyn.MSBuild.Util
{
    public sealed class ValidateBuildEnvironment : Task
    {
        [Required]
        public string MSBuildBinPath { get; set; }

        /// <summary>
        /// The minimum file version for MSBuild.exe. The task will fail if the MSBuild version exceeds 
        /// this value.
        /// 
        /// Use the following page to map releases to file versions 
        ///
        /// https://github.com/Microsoft/msbuild/releases
        /// </summary>
        [Required]
        public string MSBuildMinimumFileVersion { get; set; }

        /// <summary>
        /// Friendly developer facing version of the MSBuild message. 
        /// </summary>
        [Required]
        public string MSBuildMinimumDisplayVersion { get; set; }

        public override bool Execute()
        {
            var allGood = true;

            if (MSBuildMinimumFileVersion != null)
            {
                var msbuildFileName = "MSBuild." + (Environment.NewLine == "\r\n" ? "exe" : "dll");
                var msbuildFilePath = Path.Combine(MSBuildBinPath, msbuildFileName);
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(msbuildFilePath);
                var fileVersion = new Version(fileVersionInfo.FileVersion);
                var minimumVersion = new Version(MSBuildMinimumFileVersion);
                if (fileVersion < minimumVersion)
                {
                    Log.LogError($"MSBuild version {fileVersion} is less than the required minimum version {minimumVersion} ({MSBuildMinimumDisplayVersion})");
                    allGood = false;
                }
            }

            return allGood;
        }
    }
}
