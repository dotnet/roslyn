// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#if NET46

using System;
using System.Drawing;
using System.IO;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Win32;
using Xunit;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities
{
    public class Framework35Installed : ExecutionCondition
    {
        public override bool ShouldSkip
        {
            get
            {
                try
                {
                    const string RegistryPath = @"Software\Microsoft\NET Framework Setup\NDP\v3.5";
                    var key = Registry.LocalMachine.OpenSubKey(RegistryPath);
                    if (key == null)
                    {
                        return true;
                    }

                    var value = Convert.ToInt32(key.GetValue("Install", 0) ?? 0);
                    return value == 0;
                }
                catch
                {
                    return true;
                }
            }
        }

        public override string SkipReason
        {
            get
            {
                return ".NET Framework 3.5 is not installed";
            }
        }
    }

    public class NotFramework45 : ExecutionCondition
    {
        public override bool ShouldSkip
        {
            get
            {
                // On Framework 4.5, ExtensionAttribute lives in mscorlib...
                return typeof(System.Runtime.CompilerServices.ExtensionAttribute).Assembly ==
                    typeof(object).Assembly;
            }
        }

        public override string SkipReason { get { return "Test currently not supported on Framework 4.5"; } }
    }
}

#endif
