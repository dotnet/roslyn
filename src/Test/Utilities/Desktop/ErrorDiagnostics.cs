// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Roslyn.Test.Utilities
{
    public sealed class ErrorDiagnostics
    {
        public enum WellKnownDll
        {
            PlatformVsEditor,
            PlatformEditor,
            TextLogic,
            TextUI,
            TextWpf,
            TextData,
            UIUndo,
            StandardClassification
        }

        public enum DllVersion
        {
            Unknown,
            Beta2,
            RC
        }

        public static List<string> DiagnoseMefProblems()
        {
            var list = new List<string>();
            var dllList = GetWellKnownDllsWithVersion().ToList();
            foreach (var tuple in dllList)
            {
                if (tuple.Item3 == DllVersion.RC)
                {
                    var assembly = tuple.Item1;
                    list.Add(string.Format("Loaded RC version of assembly {0} instead of beta2: {1} - {2}",
                        assembly.GetName().Name,
                        assembly.CodeBase,
                        assembly.Location));
                }
            }

            return list;
        }

        public static IEnumerable<Tuple<Assembly, WellKnownDll>> GetWellKnownDlls()
        {
            var list = AppDomain.CurrentDomain.GetAssemblies().ToList();
            foreach (var assembly in list)
            {
                switch (assembly.GetName().Name)
                {
                    case "Microsoft.VisualStudio.Platform.VSEditor":
                        yield return Tuple.Create(assembly, WellKnownDll.PlatformVsEditor);
                        break;
                    case "Microsoft.VisualStudio.Platform.Editor":
                        yield return Tuple.Create(assembly, WellKnownDll.PlatformEditor);
                        break;
                    case "Microsoft.VisualStudio.Text.Logic":
                        yield return Tuple.Create(assembly, WellKnownDll.TextLogic);
                        break;
                    case "Microsoft.VisualStudio.Text.UI":
                        yield return Tuple.Create(assembly, WellKnownDll.TextUI);
                        break;
                    case "Microsoft.VisualStudio.Text.Data":
                        yield return Tuple.Create(assembly, WellKnownDll.TextData);
                        break;
                    case "Microsoft.VisualStudio.Text.UI.Wpf":
                        yield return Tuple.Create(assembly, WellKnownDll.TextWpf);
                        break;
                    case "Microsoft.VisualStudio.UI.Undo":
                        yield return Tuple.Create(assembly, WellKnownDll.UIUndo);
                        break;
                    case "Microsoft.VisualStudio.Language.StandardClassification":
                        yield return Tuple.Create(assembly, WellKnownDll.StandardClassification);
                        break;
                }
            }
        }

        private static IEnumerable<Tuple<Assembly, WellKnownDll, DllVersion>> GetWellKnownDllsWithVersion()
        {
            foreach (var pair in GetWellKnownDlls())
            {
                switch (pair.Item2)
                {
                    case WellKnownDll.PlatformVsEditor:
                        {
                            var type = pair.Item1.GetType("Microsoft.VisualStudio.Text.Implementation.BaseSnapshot");
                            var ct = type.GetProperty("ContentType");
                            var version = ct == null ? DllVersion.Beta2 : DllVersion.RC;
                            yield return Tuple.Create(pair.Item1, pair.Item2, version);
                        }

                        break;
                    case WellKnownDll.TextData:
                        {
                            var type = pair.Item1.GetType("Microsoft.VisualStudio.Text.ITextSnapshot");
                            var ct = type.GetProperty("ContentType");
                            var version = ct == null ? DllVersion.Beta2 : DllVersion.RC;
                            yield return Tuple.Create(pair.Item1, pair.Item2, version);
                        }

                        break;
                    default:
                        yield return Tuple.Create(pair.Item1, pair.Item2, DllVersion.Unknown);
                        break;
                }
            }
        }
    }
}
