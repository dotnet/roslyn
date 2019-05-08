// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Testing
{
    public sealed class ReferenceAssemblies
    {
        public ReferenceAssemblies(
            ImmutableArray<MetadataReference> references,
            ImmutableDictionary<string, ImmutableArray<MetadataReference>> languageSpecificReferences)
        {
            References = references;
            LanguageSpecificReferences = languageSpecificReferences;
        }

        public static ReferenceAssemblies Default
        {
            get
            {
#if NETSTANDARD1_5
                return NetStandard.NetStandard15;
#elif NETSTANDARD2_0
                return NetStandard.NetStandard2;
#else
                return NetFramework.Net46.Default;
#endif
            }
        }

        public ImmutableArray<MetadataReference> References { get; }

        public ImmutableDictionary<string, ImmutableArray<MetadataReference>> LanguageSpecificReferences { get; }

#if false
        public string ReferenceAssembliesPath { get; }

        public string FacadeAssembliesPath { get; }
#endif

        internal IEnumerable<MetadataReference> GetMetadataReferences(string language)
        {
            if (LanguageSpecificReferences.TryGetValue(language, out var perLanguageReferences))
            {
                return References.Concat(perLanguageReferences);
            }

            return References;
        }

        public static class NetFramework
        {
            public static class Net20
            {
                public static ReferenceAssemblies Default
                {
                    get
                    {
                        return new ReferenceAssemblies(
                            ImmutableArray.Create(
                                MetadataReferences.NetFramework.Net20.Mscorlib,
                                MetadataReferences.NetFramework.Net20.System,
                                MetadataReferences.NetFramework.Net20.SystemData,
                                MetadataReferences.NetFramework.Net20.SystemXml),
                            ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>()
                                .Add(LanguageNames.VisualBasic, ImmutableArray.Create(MetadataReferences.NetFramework.Net20.MicrosoftVisualBasic)));
                    }
                }

                public static ReferenceAssemblies WindowsForms
                {
                    get
                    {
                        return new ReferenceAssemblies(
                            ImmutableArray.Create(
                                MetadataReferences.NetFramework.Net20.Mscorlib,
                                MetadataReferences.NetFramework.Net20.System,
                                MetadataReferences.NetFramework.Net20.SystemData,
                                MetadataReferences.NetFramework.Net20.SystemDrawing,
                                MetadataReferences.NetFramework.Net20.SystemWindowsForms,
                                MetadataReferences.NetFramework.Net20.SystemXml),
                            ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>()
                                .Add(LanguageNames.VisualBasic, ImmutableArray.Create(MetadataReferences.NetFramework.Net20.MicrosoftVisualBasic)));
                    }
                }
            }

            public static class Net40
            {
                public static ReferenceAssemblies Default
                {
                    get
                    {
                        return new ReferenceAssemblies(
                            ImmutableArray.Create(
                                MetadataReferences.NetFramework.Net40.Mscorlib,
                                MetadataReferences.NetFramework.Net40.System,
                                MetadataReferences.NetFramework.Net40.SystemCore,
                                MetadataReferences.NetFramework.Net40.SystemData,
                                MetadataReferences.NetFramework.Net40.SystemDataDataSetExtensions,
                                MetadataReferences.NetFramework.Net40.SystemXml,
                                MetadataReferences.NetFramework.Net40.SystemXmlLinq),
                            ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>()
                                .Add(LanguageNames.CSharp, ImmutableArray.Create(MetadataReferences.NetFramework.Net40.MicrosoftCSharp))
                                .Add(LanguageNames.VisualBasic, ImmutableArray.Create(MetadataReferences.NetFramework.Net40.MicrosoftVisualBasic)));
                    }
                }

                public static ReferenceAssemblies WindowsForms
                {
                    get
                    {
                        return new ReferenceAssemblies(
                            ImmutableArray.Create(
                                MetadataReferences.NetFramework.Net40.Mscorlib,
                                MetadataReferences.NetFramework.Net40.System,
                                MetadataReferences.NetFramework.Net40.SystemCore,
                                MetadataReferences.NetFramework.Net40.SystemData,
                                MetadataReferences.NetFramework.Net40.SystemDataDataSetExtensions,
                                MetadataReferences.NetFramework.Net40.SystemDeployment,
                                MetadataReferences.NetFramework.Net40.SystemDrawing,
                                MetadataReferences.NetFramework.Net40.SystemWindowsForms,
                                MetadataReferences.NetFramework.Net40.SystemXml,
                                MetadataReferences.NetFramework.Net40.SystemXmlLinq),
                            ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>()
                                .Add(LanguageNames.CSharp, ImmutableArray.Create(MetadataReferences.NetFramework.Net40.MicrosoftCSharp))
                                .Add(LanguageNames.VisualBasic, ImmutableArray.Create(MetadataReferences.NetFramework.Net40.MicrosoftVisualBasic)));
                    }
                }

                public static ReferenceAssemblies Wpf
                {
                    get
                    {
                        return new ReferenceAssemblies(
                            ImmutableArray.Create(
                                MetadataReferences.NetFramework.Net40.Mscorlib,
                                MetadataReferences.NetFramework.Net40.PresentationCore,
                                MetadataReferences.NetFramework.Net40.PresentationFramework,
                                MetadataReferences.NetFramework.Net40.System,
                                MetadataReferences.NetFramework.Net40.SystemCore,
                                MetadataReferences.NetFramework.Net40.SystemData,
                                MetadataReferences.NetFramework.Net40.SystemDataDataSetExtensions,
                                MetadataReferences.NetFramework.Net40.SystemXaml,
                                MetadataReferences.NetFramework.Net40.SystemXml,
                                MetadataReferences.NetFramework.Net40.SystemXmlLinq,
                                MetadataReferences.NetFramework.Net40.WindowsBase),
                            ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>()
                                .Add(LanguageNames.CSharp, ImmutableArray.Create(MetadataReferences.NetFramework.Net40.MicrosoftCSharp))
                                .Add(LanguageNames.VisualBasic, ImmutableArray.Create(MetadataReferences.NetFramework.Net40.MicrosoftVisualBasic)));
                    }
                }
            }

            public static class Net45
            {
                public static ReferenceAssemblies Default => throw new NotImplementedException();

                public static ReferenceAssemblies WindowsForms => throw new NotImplementedException();

                public static ReferenceAssemblies Wpf => throw new NotImplementedException();
            }

            public static class Net451
            {
                public static ReferenceAssemblies Default => throw new NotImplementedException();

                public static ReferenceAssemblies WindowsForms => throw new NotImplementedException();

                public static ReferenceAssemblies Wpf => throw new NotImplementedException();
            }

            public static class Net452
            {
                public static ReferenceAssemblies Default => throw new NotImplementedException();

                public static ReferenceAssemblies WindowsForms => throw new NotImplementedException();

                public static ReferenceAssemblies Wpf => throw new NotImplementedException();
            }

            public static class Net46
            {
                public static ReferenceAssemblies Default
                {
                    get
                    {
                        return new ReferenceAssemblies(
                            ImmutableArray.Create(
                                MetadataReferences.NetFramework.Net46.Mscorlib,
                                MetadataReferences.NetFramework.Net46.System,
                                MetadataReferences.NetFramework.Net46.SystemCore,
                                MetadataReferences.NetFramework.Net46.SystemData,
                                MetadataReferences.NetFramework.Net46.SystemDataDataSetExtensions,
                                MetadataReferences.NetFramework.Net46.SystemNetHttp,
                                MetadataReferences.NetFramework.Net46.SystemXml,
                                MetadataReferences.NetFramework.Net46.SystemXmlLinq),
                            ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>()
                                .Add(LanguageNames.CSharp, ImmutableArray.Create(MetadataReferences.NetFramework.Net46.MicrosoftCSharp))
                                .Add(LanguageNames.VisualBasic, ImmutableArray.Create(MetadataReferences.NetFramework.Net46.MicrosoftVisualBasic)));
                    }
                }

                public static ReferenceAssemblies WindowsForms
                {
                    get
                    {
                        return new ReferenceAssemblies(
                            ImmutableArray.Create(
                                MetadataReferences.NetFramework.Net46.Mscorlib,
                                MetadataReferences.NetFramework.Net46.System,
                                MetadataReferences.NetFramework.Net46.SystemCore,
                                MetadataReferences.NetFramework.Net46.SystemData,
                                MetadataReferences.NetFramework.Net46.SystemDataDataSetExtensions,
                                MetadataReferences.NetFramework.Net46.SystemDeployment,
                                MetadataReferences.NetFramework.Net46.SystemDrawing,
                                MetadataReferences.NetFramework.Net46.SystemNetHttp,
                                MetadataReferences.NetFramework.Net46.SystemWindowsForms,
                                MetadataReferences.NetFramework.Net46.SystemXml,
                                MetadataReferences.NetFramework.Net46.SystemXmlLinq),
                            ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>()
                                .Add(LanguageNames.CSharp, ImmutableArray.Create(MetadataReferences.NetFramework.Net46.MicrosoftCSharp))
                                .Add(LanguageNames.VisualBasic, ImmutableArray.Create(MetadataReferences.NetFramework.Net46.MicrosoftVisualBasic)));
                    }
                }

                public static ReferenceAssemblies Wpf
                {
                    get
                    {
                        return new ReferenceAssemblies(
                            ImmutableArray.Create(
                                MetadataReferences.NetFramework.Net46.Mscorlib,
                                MetadataReferences.NetFramework.Net46.PresentationCore,
                                MetadataReferences.NetFramework.Net46.PresentationFramework,
                                MetadataReferences.NetFramework.Net46.System,
                                MetadataReferences.NetFramework.Net46.SystemCore,
                                MetadataReferences.NetFramework.Net46.SystemData,
                                MetadataReferences.NetFramework.Net46.SystemDataDataSetExtensions,
                                MetadataReferences.NetFramework.Net46.SystemNetHttp,
                                MetadataReferences.NetFramework.Net46.SystemXaml,
                                MetadataReferences.NetFramework.Net46.SystemXml,
                                MetadataReferences.NetFramework.Net46.SystemXmlLinq,
                                MetadataReferences.NetFramework.Net46.WindowsBase),
                            ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>()
                                .Add(LanguageNames.CSharp, ImmutableArray.Create(MetadataReferences.NetFramework.Net46.MicrosoftCSharp))
                                .Add(LanguageNames.VisualBasic, ImmutableArray.Create(MetadataReferences.NetFramework.Net46.MicrosoftVisualBasic)));
                    }
                }
            }

            public static class Net461
            {
                public static ReferenceAssemblies Default
                {
                    get
                    {
                        return new ReferenceAssemblies(
                            ImmutableArray.Create(
                                MetadataReferences.NetFramework.Net461.Mscorlib,
                                MetadataReferences.NetFramework.Net461.System,
                                MetadataReferences.NetFramework.Net461.SystemCore,
                                MetadataReferences.NetFramework.Net461.SystemData,
                                MetadataReferences.NetFramework.Net461.SystemDataDataSetExtensions,
                                MetadataReferences.NetFramework.Net461.SystemNetHttp,
                                MetadataReferences.NetFramework.Net461.SystemXml,
                                MetadataReferences.NetFramework.Net461.SystemXmlLinq),
                            ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>()
                                .Add(LanguageNames.CSharp, ImmutableArray.Create(MetadataReferences.NetFramework.Net461.MicrosoftCSharp))
                                .Add(LanguageNames.VisualBasic, ImmutableArray.Create(MetadataReferences.NetFramework.Net461.MicrosoftVisualBasic)));
                    }
                }

                public static ReferenceAssemblies WindowsForms
                {
                    get
                    {
                        return new ReferenceAssemblies(
                            ImmutableArray.Create(
                                MetadataReferences.NetFramework.Net461.Mscorlib,
                                MetadataReferences.NetFramework.Net461.System,
                                MetadataReferences.NetFramework.Net461.SystemCore,
                                MetadataReferences.NetFramework.Net461.SystemData,
                                MetadataReferences.NetFramework.Net461.SystemDataDataSetExtensions,
                                MetadataReferences.NetFramework.Net461.SystemDeployment,
                                MetadataReferences.NetFramework.Net461.SystemDrawing,
                                MetadataReferences.NetFramework.Net461.SystemNetHttp,
                                MetadataReferences.NetFramework.Net461.SystemWindowsForms,
                                MetadataReferences.NetFramework.Net461.SystemXml,
                                MetadataReferences.NetFramework.Net461.SystemXmlLinq),
                            ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>()
                                .Add(LanguageNames.CSharp, ImmutableArray.Create(MetadataReferences.NetFramework.Net461.MicrosoftCSharp))
                                .Add(LanguageNames.VisualBasic, ImmutableArray.Create(MetadataReferences.NetFramework.Net461.MicrosoftVisualBasic)));
                    }
                }

                public static ReferenceAssemblies Wpf
                {
                    get
                    {
                        return new ReferenceAssemblies(
                            ImmutableArray.Create(
                                MetadataReferences.NetFramework.Net461.Mscorlib,
                                MetadataReferences.NetFramework.Net461.PresentationCore,
                                MetadataReferences.NetFramework.Net461.PresentationFramework,
                                MetadataReferences.NetFramework.Net461.System,
                                MetadataReferences.NetFramework.Net461.SystemCore,
                                MetadataReferences.NetFramework.Net461.SystemData,
                                MetadataReferences.NetFramework.Net461.SystemDataDataSetExtensions,
                                MetadataReferences.NetFramework.Net461.SystemNetHttp,
                                MetadataReferences.NetFramework.Net461.SystemXaml,
                                MetadataReferences.NetFramework.Net461.SystemXml,
                                MetadataReferences.NetFramework.Net461.SystemXmlLinq,
                                MetadataReferences.NetFramework.Net461.WindowsBase),
                            ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>()
                                .Add(LanguageNames.CSharp, ImmutableArray.Create(MetadataReferences.NetFramework.Net461.MicrosoftCSharp))
                                .Add(LanguageNames.VisualBasic, ImmutableArray.Create(MetadataReferences.NetFramework.Net461.MicrosoftVisualBasic)));
                    }
                }
            }

            public static class Net462
            {
                public static ReferenceAssemblies Default
                {
                    get
                    {
                        return new ReferenceAssemblies(
                            ImmutableArray.Create(
                                MetadataReferences.NetFramework.Net462.Mscorlib,
                                MetadataReferences.NetFramework.Net462.System,
                                MetadataReferences.NetFramework.Net462.SystemCore,
                                MetadataReferences.NetFramework.Net462.SystemData,
                                MetadataReferences.NetFramework.Net462.SystemDataDataSetExtensions,
                                MetadataReferences.NetFramework.Net462.SystemNetHttp,
                                MetadataReferences.NetFramework.Net462.SystemXml,
                                MetadataReferences.NetFramework.Net462.SystemXmlLinq),
                            ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>()
                                .Add(LanguageNames.CSharp, ImmutableArray.Create(MetadataReferences.NetFramework.Net462.MicrosoftCSharp))
                                .Add(LanguageNames.VisualBasic, ImmutableArray.Create(MetadataReferences.NetFramework.Net462.MicrosoftVisualBasic)));
                    }
                }

                public static ReferenceAssemblies WindowsForms
                {
                    get
                    {
                        return new ReferenceAssemblies(
                            ImmutableArray.Create(
                                MetadataReferences.NetFramework.Net462.Mscorlib,
                                MetadataReferences.NetFramework.Net462.System,
                                MetadataReferences.NetFramework.Net462.SystemCore,
                                MetadataReferences.NetFramework.Net462.SystemData,
                                MetadataReferences.NetFramework.Net462.SystemDataDataSetExtensions,
                                MetadataReferences.NetFramework.Net462.SystemDeployment,
                                MetadataReferences.NetFramework.Net462.SystemDrawing,
                                MetadataReferences.NetFramework.Net462.SystemNetHttp,
                                MetadataReferences.NetFramework.Net462.SystemWindowsForms,
                                MetadataReferences.NetFramework.Net462.SystemXml,
                                MetadataReferences.NetFramework.Net462.SystemXmlLinq),
                            ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>()
                                .Add(LanguageNames.CSharp, ImmutableArray.Create(MetadataReferences.NetFramework.Net462.MicrosoftCSharp))
                                .Add(LanguageNames.VisualBasic, ImmutableArray.Create(MetadataReferences.NetFramework.Net462.MicrosoftVisualBasic)));
                    }
                }

                public static ReferenceAssemblies Wpf
                {
                    get
                    {
                        return new ReferenceAssemblies(
                            ImmutableArray.Create(
                                MetadataReferences.NetFramework.Net462.Mscorlib,
                                MetadataReferences.NetFramework.Net462.PresentationCore,
                                MetadataReferences.NetFramework.Net462.PresentationFramework,
                                MetadataReferences.NetFramework.Net462.System,
                                MetadataReferences.NetFramework.Net462.SystemCore,
                                MetadataReferences.NetFramework.Net462.SystemData,
                                MetadataReferences.NetFramework.Net462.SystemDataDataSetExtensions,
                                MetadataReferences.NetFramework.Net462.SystemNetHttp,
                                MetadataReferences.NetFramework.Net462.SystemXaml,
                                MetadataReferences.NetFramework.Net462.SystemXml,
                                MetadataReferences.NetFramework.Net462.SystemXmlLinq,
                                MetadataReferences.NetFramework.Net462.WindowsBase),
                            ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>()
                                .Add(LanguageNames.CSharp, ImmutableArray.Create(MetadataReferences.NetFramework.Net462.MicrosoftCSharp))
                                .Add(LanguageNames.VisualBasic, ImmutableArray.Create(MetadataReferences.NetFramework.Net462.MicrosoftVisualBasic)));
                    }
                }
            }

            public static class Net47
            {
                public static ReferenceAssemblies Default
                {
                    get
                    {
                        return new ReferenceAssemblies(
                            ImmutableArray.Create(
                                MetadataReferences.NetFramework.Net47.Mscorlib,
                                MetadataReferences.NetFramework.Net47.System,
                                MetadataReferences.NetFramework.Net47.SystemCore,
                                MetadataReferences.NetFramework.Net47.SystemData,
                                MetadataReferences.NetFramework.Net47.SystemDataDataSetExtensions,
                                MetadataReferences.NetFramework.Net47.SystemNetHttp,
                                MetadataReferences.NetFramework.Net47.SystemXml,
                                MetadataReferences.NetFramework.Net47.SystemXmlLinq),
                            ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>()
                                .Add(LanguageNames.CSharp, ImmutableArray.Create(MetadataReferences.NetFramework.Net47.MicrosoftCSharp))
                                .Add(LanguageNames.VisualBasic, ImmutableArray.Create(MetadataReferences.NetFramework.Net47.MicrosoftVisualBasic)));
                    }
                }

                public static ReferenceAssemblies WindowsForms
                {
                    get
                    {
                        return new ReferenceAssemblies(
                            ImmutableArray.Create(
                                MetadataReferences.NetFramework.Net47.Mscorlib,
                                MetadataReferences.NetFramework.Net47.System,
                                MetadataReferences.NetFramework.Net47.SystemCore,
                                MetadataReferences.NetFramework.Net47.SystemData,
                                MetadataReferences.NetFramework.Net47.SystemDataDataSetExtensions,
                                MetadataReferences.NetFramework.Net47.SystemDeployment,
                                MetadataReferences.NetFramework.Net47.SystemDrawing,
                                MetadataReferences.NetFramework.Net47.SystemNetHttp,
                                MetadataReferences.NetFramework.Net47.SystemWindowsForms,
                                MetadataReferences.NetFramework.Net47.SystemXml,
                                MetadataReferences.NetFramework.Net47.SystemXmlLinq),
                            ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>()
                                .Add(LanguageNames.CSharp, ImmutableArray.Create(MetadataReferences.NetFramework.Net47.MicrosoftCSharp))
                                .Add(LanguageNames.VisualBasic, ImmutableArray.Create(MetadataReferences.NetFramework.Net47.MicrosoftVisualBasic)));
                    }
                }

                public static ReferenceAssemblies Wpf
                {
                    get
                    {
                        return new ReferenceAssemblies(
                            ImmutableArray.Create(
                                MetadataReferences.NetFramework.Net47.Mscorlib,
                                MetadataReferences.NetFramework.Net47.PresentationCore,
                                MetadataReferences.NetFramework.Net47.PresentationFramework,
                                MetadataReferences.NetFramework.Net47.System,
                                MetadataReferences.NetFramework.Net47.SystemCore,
                                MetadataReferences.NetFramework.Net47.SystemData,
                                MetadataReferences.NetFramework.Net47.SystemDataDataSetExtensions,
                                MetadataReferences.NetFramework.Net47.SystemNetHttp,
                                MetadataReferences.NetFramework.Net47.SystemXaml,
                                MetadataReferences.NetFramework.Net47.SystemXml,
                                MetadataReferences.NetFramework.Net47.SystemXmlLinq,
                                MetadataReferences.NetFramework.Net47.WindowsBase),
                            ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>()
                                .Add(LanguageNames.CSharp, ImmutableArray.Create(MetadataReferences.NetFramework.Net47.MicrosoftCSharp))
                                .Add(LanguageNames.VisualBasic, ImmutableArray.Create(MetadataReferences.NetFramework.Net47.MicrosoftVisualBasic)));
                    }
                }
            }

            public static class Net471
            {
                public static ReferenceAssemblies Default
                {
                    get
                    {
                        return new ReferenceAssemblies(
                            ImmutableArray.Create(
                                MetadataReferences.NetFramework.Net471.Mscorlib,
                                MetadataReferences.NetFramework.Net471.System,
                                MetadataReferences.NetFramework.Net471.SystemCore,
                                MetadataReferences.NetFramework.Net471.SystemData,
                                MetadataReferences.NetFramework.Net471.SystemDataDataSetExtensions,
                                MetadataReferences.NetFramework.Net471.SystemNetHttp,
                                MetadataReferences.NetFramework.Net471.SystemXml,
                                MetadataReferences.NetFramework.Net471.SystemXmlLinq),
                            ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>()
                                .Add(LanguageNames.CSharp, ImmutableArray.Create(MetadataReferences.NetFramework.Net471.MicrosoftCSharp))
                                .Add(LanguageNames.VisualBasic, ImmutableArray.Create(MetadataReferences.NetFramework.Net471.MicrosoftVisualBasic)));
                    }
                }

                public static ReferenceAssemblies WindowsForms
                {
                    get
                    {
                        return new ReferenceAssemblies(
                            ImmutableArray.Create(
                                MetadataReferences.NetFramework.Net471.Mscorlib,
                                MetadataReferences.NetFramework.Net471.System,
                                MetadataReferences.NetFramework.Net471.SystemCore,
                                MetadataReferences.NetFramework.Net471.SystemData,
                                MetadataReferences.NetFramework.Net471.SystemDataDataSetExtensions,
                                MetadataReferences.NetFramework.Net471.SystemDeployment,
                                MetadataReferences.NetFramework.Net471.SystemDrawing,
                                MetadataReferences.NetFramework.Net471.SystemNetHttp,
                                MetadataReferences.NetFramework.Net471.SystemWindowsForms,
                                MetadataReferences.NetFramework.Net471.SystemXml,
                                MetadataReferences.NetFramework.Net471.SystemXmlLinq),
                            ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>()
                                .Add(LanguageNames.CSharp, ImmutableArray.Create(MetadataReferences.NetFramework.Net471.MicrosoftCSharp))
                                .Add(LanguageNames.VisualBasic, ImmutableArray.Create(MetadataReferences.NetFramework.Net471.MicrosoftVisualBasic)));
                    }
                }

                public static ReferenceAssemblies Wpf
                {
                    get
                    {
                        return new ReferenceAssemblies(
                            ImmutableArray.Create(
                                MetadataReferences.NetFramework.Net471.Mscorlib,
                                MetadataReferences.NetFramework.Net471.PresentationCore,
                                MetadataReferences.NetFramework.Net471.PresentationFramework,
                                MetadataReferences.NetFramework.Net471.System,
                                MetadataReferences.NetFramework.Net471.SystemCore,
                                MetadataReferences.NetFramework.Net471.SystemData,
                                MetadataReferences.NetFramework.Net471.SystemDataDataSetExtensions,
                                MetadataReferences.NetFramework.Net471.SystemNetHttp,
                                MetadataReferences.NetFramework.Net471.SystemXaml,
                                MetadataReferences.NetFramework.Net471.SystemXml,
                                MetadataReferences.NetFramework.Net471.SystemXmlLinq,
                                MetadataReferences.NetFramework.Net471.WindowsBase),
                            ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>()
                                .Add(LanguageNames.CSharp, ImmutableArray.Create(MetadataReferences.NetFramework.Net471.MicrosoftCSharp))
                                .Add(LanguageNames.VisualBasic, ImmutableArray.Create(MetadataReferences.NetFramework.Net471.MicrosoftVisualBasic)));
                    }
                }
            }

            public static class Net472
            {
                public static ReferenceAssemblies Default
                {
                    get
                    {
                        return new ReferenceAssemblies(
                            ImmutableArray.Create(
                                MetadataReferences.NetFramework.Net472.Mscorlib,
                                MetadataReferences.NetFramework.Net472.System,
                                MetadataReferences.NetFramework.Net472.SystemCore,
                                MetadataReferences.NetFramework.Net472.SystemData,
                                MetadataReferences.NetFramework.Net472.SystemDataDataSetExtensions,
                                MetadataReferences.NetFramework.Net472.SystemNetHttp,
                                MetadataReferences.NetFramework.Net472.SystemXml,
                                MetadataReferences.NetFramework.Net472.SystemXmlLinq),
                            ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>()
                                .Add(LanguageNames.CSharp, ImmutableArray.Create(MetadataReferences.NetFramework.Net472.MicrosoftCSharp))
                                .Add(LanguageNames.VisualBasic, ImmutableArray.Create(MetadataReferences.NetFramework.Net472.MicrosoftVisualBasic)));
                    }
                }

                public static ReferenceAssemblies WindowsForms
                {
                    get
                    {
                        return new ReferenceAssemblies(
                            ImmutableArray.Create(
                                MetadataReferences.NetFramework.Net472.Mscorlib,
                                MetadataReferences.NetFramework.Net472.System,
                                MetadataReferences.NetFramework.Net472.SystemCore,
                                MetadataReferences.NetFramework.Net472.SystemData,
                                MetadataReferences.NetFramework.Net472.SystemDataDataSetExtensions,
                                MetadataReferences.NetFramework.Net472.SystemDeployment,
                                MetadataReferences.NetFramework.Net472.SystemDrawing,
                                MetadataReferences.NetFramework.Net472.SystemNetHttp,
                                MetadataReferences.NetFramework.Net472.SystemWindowsForms,
                                MetadataReferences.NetFramework.Net472.SystemXml,
                                MetadataReferences.NetFramework.Net472.SystemXmlLinq),
                            ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>()
                                .Add(LanguageNames.CSharp, ImmutableArray.Create(MetadataReferences.NetFramework.Net472.MicrosoftCSharp))
                                .Add(LanguageNames.VisualBasic, ImmutableArray.Create(MetadataReferences.NetFramework.Net472.MicrosoftVisualBasic)));
                    }
                }

                public static ReferenceAssemblies Wpf
                {
                    get
                    {
                        return new ReferenceAssemblies(
                            ImmutableArray.Create(
                                MetadataReferences.NetFramework.Net472.Mscorlib,
                                MetadataReferences.NetFramework.Net472.PresentationCore,
                                MetadataReferences.NetFramework.Net472.PresentationFramework,
                                MetadataReferences.NetFramework.Net472.System,
                                MetadataReferences.NetFramework.Net472.SystemCore,
                                MetadataReferences.NetFramework.Net472.SystemData,
                                MetadataReferences.NetFramework.Net472.SystemDataDataSetExtensions,
                                MetadataReferences.NetFramework.Net472.SystemNetHttp,
                                MetadataReferences.NetFramework.Net472.SystemXaml,
                                MetadataReferences.NetFramework.Net472.SystemXml,
                                MetadataReferences.NetFramework.Net472.SystemXmlLinq,
                                MetadataReferences.NetFramework.Net472.WindowsBase),
                            ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>()
                                .Add(LanguageNames.CSharp, ImmutableArray.Create(MetadataReferences.NetFramework.Net472.MicrosoftCSharp))
                                .Add(LanguageNames.VisualBasic, ImmutableArray.Create(MetadataReferences.NetFramework.Net472.MicrosoftVisualBasic)));
                    }
                }
            }

            public static class Net48
            {
                public static ReferenceAssemblies Default => throw new NotImplementedException();

                public static ReferenceAssemblies WindowsForms => throw new NotImplementedException();

                public static ReferenceAssemblies Wpf => throw new NotImplementedException();
            }
        }

        public static class NetCore
        {
            public static ReferenceAssemblies NetCore10 => throw new NotImplementedException();

            public static ReferenceAssemblies NetCore20 => throw new NotImplementedException();
        }

        public static class NetStandard
        {
            public static ReferenceAssemblies NetStandard10
            {
                get
                {
                    return new ReferenceAssemblies(
                        ImmutableArray.Create(
                            MetadataReferences.NetStandard.NetStandard10.SystemCollections,
                            MetadataReferences.NetStandard.NetStandard10.SystemDiagnosticsDebug,
                            MetadataReferences.NetStandard.NetStandard10.SystemDiagnosticsTools,
                            MetadataReferences.NetStandard.NetStandard10.SystemGlobalization,
                            MetadataReferences.NetStandard.NetStandard10.SystemIO,
                            MetadataReferences.NetStandard.NetStandard10.SystemLinq,
                            MetadataReferences.NetStandard.NetStandard10.SystemLinqExpressions,
                            MetadataReferences.NetStandard.NetStandard10.SystemNetPrimitives,
                            MetadataReferences.NetStandard.NetStandard10.SystemObjectModel,
                            MetadataReferences.NetStandard.NetStandard10.SystemReflection,
                            MetadataReferences.NetStandard.NetStandard10.SystemReflectionExtensions,
                            MetadataReferences.NetStandard.NetStandard10.SystemReflectionPrimitives,
                            MetadataReferences.NetStandard.NetStandard10.SystemResourcesResourceManager,
                            MetadataReferences.NetStandard.NetStandard10.SystemRuntime,
                            MetadataReferences.NetStandard.NetStandard10.SystemRuntimeExtensions,
                            MetadataReferences.NetStandard.NetStandard10.SystemTextEncoding,
                            MetadataReferences.NetStandard.NetStandard10.SystemTextEncodingExtensions,
                            MetadataReferences.NetStandard.NetStandard10.SystemTextRegularExpressions,
                            MetadataReferences.NetStandard.NetStandard10.SystemThreading,
                            MetadataReferences.NetStandard.NetStandard10.SystemThreadingTasks,
                            MetadataReferences.NetStandard.NetStandard10.SystemXmlReaderWriter,
                            MetadataReferences.NetStandard.NetStandard10.SystemXmlXDocument),
                        ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>());
                }
            }

            public static ReferenceAssemblies NetStandard11
            {
                get
                {
                    return new ReferenceAssemblies(
                        ImmutableArray.Create(
                            MetadataReferences.NetStandard.NetStandard11.SystemCollections,
                            MetadataReferences.NetStandard.NetStandard11.SystemCollectionsConcurrent,
                            MetadataReferences.NetStandard.NetStandard11.SystemDiagnosticsDebug,
                            MetadataReferences.NetStandard.NetStandard11.SystemDiagnosticsTools,
                            MetadataReferences.NetStandard.NetStandard11.SystemDiagnosticsTracing,
                            MetadataReferences.NetStandard.NetStandard11.SystemGlobalization,
                            MetadataReferences.NetStandard.NetStandard11.SystemIO,
                            MetadataReferences.NetStandard.NetStandard11.SystemIOCompression,
                            MetadataReferences.NetStandard.NetStandard11.SystemLinq,
                            MetadataReferences.NetStandard.NetStandard11.SystemLinqExpressions,
                            MetadataReferences.NetStandard.NetStandard11.SystemNetHttp,
                            MetadataReferences.NetStandard.NetStandard11.SystemNetPrimitives,
                            MetadataReferences.NetStandard.NetStandard11.SystemObjectModel,
                            MetadataReferences.NetStandard.NetStandard11.SystemReflection,
                            MetadataReferences.NetStandard.NetStandard11.SystemReflectionExtensions,
                            MetadataReferences.NetStandard.NetStandard11.SystemReflectionPrimitives,
                            MetadataReferences.NetStandard.NetStandard11.SystemResourcesResourceManager,
                            MetadataReferences.NetStandard.NetStandard11.SystemRuntime,
                            MetadataReferences.NetStandard.NetStandard11.SystemRuntimeExtensions,
                            MetadataReferences.NetStandard.NetStandard11.SystemRuntimeInteropServices,
                            MetadataReferences.NetStandard.NetStandard11.SystemRuntimeInteropServicesRuntimeInformation,
                            MetadataReferences.NetStandard.NetStandard11.SystemRuntimeNumerics,
                            MetadataReferences.NetStandard.NetStandard11.SystemTextEncoding,
                            MetadataReferences.NetStandard.NetStandard11.SystemTextEncodingExtensions,
                            MetadataReferences.NetStandard.NetStandard11.SystemTextRegularExpressions,
                            MetadataReferences.NetStandard.NetStandard11.SystemThreading,
                            MetadataReferences.NetStandard.NetStandard11.SystemThreadingTasks,
                            MetadataReferences.NetStandard.NetStandard11.SystemXmlReaderWriter,
                            MetadataReferences.NetStandard.NetStandard11.SystemXmlXDocument),
                        ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>());
                }
            }

            public static ReferenceAssemblies NetStandard12
            {
                get
                {
                    return new ReferenceAssemblies(
                        ImmutableArray.Create(
                            MetadataReferences.NetStandard.NetStandard12.SystemCollections,
                            MetadataReferences.NetStandard.NetStandard12.SystemCollectionsConcurrent,
                            MetadataReferences.NetStandard.NetStandard12.SystemDiagnosticsDebug,
                            MetadataReferences.NetStandard.NetStandard12.SystemDiagnosticsTools,
                            MetadataReferences.NetStandard.NetStandard12.SystemDiagnosticsTracing,
                            MetadataReferences.NetStandard.NetStandard12.SystemGlobalization,
                            MetadataReferences.NetStandard.NetStandard12.SystemIO,
                            MetadataReferences.NetStandard.NetStandard12.SystemIOCompression,
                            MetadataReferences.NetStandard.NetStandard12.SystemLinq,
                            MetadataReferences.NetStandard.NetStandard12.SystemLinqExpressions,
                            MetadataReferences.NetStandard.NetStandard12.SystemNetHttp,
                            MetadataReferences.NetStandard.NetStandard12.SystemNetPrimitives,
                            MetadataReferences.NetStandard.NetStandard12.SystemObjectModel,
                            MetadataReferences.NetStandard.NetStandard12.SystemReflection,
                            MetadataReferences.NetStandard.NetStandard12.SystemReflectionExtensions,
                            MetadataReferences.NetStandard.NetStandard12.SystemReflectionPrimitives,
                            MetadataReferences.NetStandard.NetStandard12.SystemResourcesResourceManager,
                            MetadataReferences.NetStandard.NetStandard12.SystemRuntime,
                            MetadataReferences.NetStandard.NetStandard12.SystemRuntimeExtensions,
                            MetadataReferences.NetStandard.NetStandard12.SystemRuntimeInteropServices,
                            MetadataReferences.NetStandard.NetStandard12.SystemRuntimeInteropServicesRuntimeInformation,
                            MetadataReferences.NetStandard.NetStandard12.SystemRuntimeNumerics,
                            MetadataReferences.NetStandard.NetStandard12.SystemTextEncoding,
                            MetadataReferences.NetStandard.NetStandard12.SystemTextEncodingExtensions,
                            MetadataReferences.NetStandard.NetStandard12.SystemTextRegularExpressions,
                            MetadataReferences.NetStandard.NetStandard12.SystemThreading,
                            MetadataReferences.NetStandard.NetStandard12.SystemThreadingTasks,
                            MetadataReferences.NetStandard.NetStandard12.SystemThreadingTimer,
                            MetadataReferences.NetStandard.NetStandard12.SystemXmlReaderWriter,
                            MetadataReferences.NetStandard.NetStandard12.SystemXmlXDocument),
                        ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>());
                }
            }

            public static ReferenceAssemblies NetStandard13
            {
                get
                {
                    return new ReferenceAssemblies(
                        ImmutableArray.Create(
                            MetadataReferences.NetStandard.NetStandard13.MicrosoftWin32Primitives,
                            MetadataReferences.NetStandard.NetStandard13.SystemAppContext,
                            MetadataReferences.NetStandard.NetStandard13.SystemCollections,
                            MetadataReferences.NetStandard.NetStandard13.SystemCollectionsConcurrent,
                            MetadataReferences.NetStandard.NetStandard13.SystemConsole,
                            MetadataReferences.NetStandard.NetStandard13.SystemDiagnosticsDebug,
                            MetadataReferences.NetStandard.NetStandard13.SystemDiagnosticsTools,
                            MetadataReferences.NetStandard.NetStandard13.SystemDiagnosticsTracing,
                            MetadataReferences.NetStandard.NetStandard13.SystemGlobalization,
                            MetadataReferences.NetStandard.NetStandard13.SystemGlobalizationCalendars,
                            MetadataReferences.NetStandard.NetStandard13.SystemIO,
                            MetadataReferences.NetStandard.NetStandard13.SystemIOCompression,
                            MetadataReferences.NetStandard.NetStandard13.SystemIOCompressionZipFile,
                            MetadataReferences.NetStandard.NetStandard13.SystemIOFileSystem,
                            MetadataReferences.NetStandard.NetStandard13.SystemIOFileSystemPrimitives,
                            MetadataReferences.NetStandard.NetStandard13.SystemLinq,
                            MetadataReferences.NetStandard.NetStandard13.SystemLinqExpressions,
                            MetadataReferences.NetStandard.NetStandard13.SystemNetHttp,
                            MetadataReferences.NetStandard.NetStandard13.SystemNetPrimitives,
                            MetadataReferences.NetStandard.NetStandard13.SystemNetSockets,
                            MetadataReferences.NetStandard.NetStandard13.SystemObjectModel,
                            MetadataReferences.NetStandard.NetStandard13.SystemReflection,
                            MetadataReferences.NetStandard.NetStandard13.SystemReflectionExtensions,
                            MetadataReferences.NetStandard.NetStandard13.SystemReflectionPrimitives,
                            MetadataReferences.NetStandard.NetStandard13.SystemResourcesResourceManager,
                            MetadataReferences.NetStandard.NetStandard13.SystemRuntime,
                            MetadataReferences.NetStandard.NetStandard13.SystemRuntimeExtensions,
                            MetadataReferences.NetStandard.NetStandard13.SystemRuntimeHandles,
                            MetadataReferences.NetStandard.NetStandard13.SystemRuntimeInteropServices,
                            MetadataReferences.NetStandard.NetStandard13.SystemRuntimeInteropServicesRuntimeInformation,
                            MetadataReferences.NetStandard.NetStandard13.SystemRuntimeNumerics,
                            MetadataReferences.NetStandard.NetStandard13.SystemSecurityCryptographyAlgorithms,
                            MetadataReferences.NetStandard.NetStandard13.SystemSecurityCryptographyEncoding,
                            MetadataReferences.NetStandard.NetStandard13.SystemSecurityCryptographyPrimitives,
                            MetadataReferences.NetStandard.NetStandard13.SystemSecurityCryptographyX509Certificates,
                            MetadataReferences.NetStandard.NetStandard13.SystemTextEncoding,
                            MetadataReferences.NetStandard.NetStandard13.SystemTextEncodingExtensions,
                            MetadataReferences.NetStandard.NetStandard13.SystemTextRegularExpressions,
                            MetadataReferences.NetStandard.NetStandard13.SystemThreading,
                            MetadataReferences.NetStandard.NetStandard13.SystemThreadingTasks,
                            MetadataReferences.NetStandard.NetStandard13.SystemThreadingTimer,
                            MetadataReferences.NetStandard.NetStandard13.SystemXmlReaderWriter,
                            MetadataReferences.NetStandard.NetStandard13.SystemXmlXDocument),
                        ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>());
                }
            }

            public static ReferenceAssemblies NetStandard14
            {
                get
                {
                    return new ReferenceAssemblies(
                        ImmutableArray.Create(
                            MetadataReferences.NetStandard.NetStandard14.MicrosoftWin32Primitives,
                            MetadataReferences.NetStandard.NetStandard14.SystemAppContext,
                            MetadataReferences.NetStandard.NetStandard14.SystemCollections,
                            MetadataReferences.NetStandard.NetStandard14.SystemCollectionsConcurrent,
                            MetadataReferences.NetStandard.NetStandard14.SystemConsole,
                            MetadataReferences.NetStandard.NetStandard14.SystemDiagnosticsDebug,
                            MetadataReferences.NetStandard.NetStandard14.SystemDiagnosticsTools,
                            MetadataReferences.NetStandard.NetStandard14.SystemDiagnosticsTracing,
                            MetadataReferences.NetStandard.NetStandard14.SystemGlobalization,
                            MetadataReferences.NetStandard.NetStandard14.SystemGlobalizationCalendars,
                            MetadataReferences.NetStandard.NetStandard14.SystemIO,
                            MetadataReferences.NetStandard.NetStandard14.SystemIOCompression,
                            MetadataReferences.NetStandard.NetStandard14.SystemIOCompressionZipFile,
                            MetadataReferences.NetStandard.NetStandard14.SystemIOFileSystem,
                            MetadataReferences.NetStandard.NetStandard14.SystemIOFileSystemPrimitives,
                            MetadataReferences.NetStandard.NetStandard14.SystemLinq,
                            MetadataReferences.NetStandard.NetStandard14.SystemLinqExpressions,
                            MetadataReferences.NetStandard.NetStandard14.SystemNetHttp,
                            MetadataReferences.NetStandard.NetStandard14.SystemNetPrimitives,
                            MetadataReferences.NetStandard.NetStandard14.SystemNetSockets,
                            MetadataReferences.NetStandard.NetStandard14.SystemObjectModel,
                            MetadataReferences.NetStandard.NetStandard14.SystemReflection,
                            MetadataReferences.NetStandard.NetStandard14.SystemReflectionExtensions,
                            MetadataReferences.NetStandard.NetStandard14.SystemReflectionPrimitives,
                            MetadataReferences.NetStandard.NetStandard14.SystemResourcesResourceManager,
                            MetadataReferences.NetStandard.NetStandard14.SystemRuntime,
                            MetadataReferences.NetStandard.NetStandard14.SystemRuntimeExtensions,
                            MetadataReferences.NetStandard.NetStandard14.SystemRuntimeHandles,
                            MetadataReferences.NetStandard.NetStandard14.SystemRuntimeInteropServices,
                            MetadataReferences.NetStandard.NetStandard14.SystemRuntimeInteropServicesRuntimeInformation,
                            MetadataReferences.NetStandard.NetStandard14.SystemRuntimeNumerics,
                            MetadataReferences.NetStandard.NetStandard14.SystemSecurityCryptographyAlgorithms,
                            MetadataReferences.NetStandard.NetStandard14.SystemSecurityCryptographyEncoding,
                            MetadataReferences.NetStandard.NetStandard14.SystemSecurityCryptographyPrimitives,
                            MetadataReferences.NetStandard.NetStandard14.SystemSecurityCryptographyX509Certificates,
                            MetadataReferences.NetStandard.NetStandard14.SystemTextEncoding,
                            MetadataReferences.NetStandard.NetStandard14.SystemTextEncodingExtensions,
                            MetadataReferences.NetStandard.NetStandard14.SystemTextRegularExpressions,
                            MetadataReferences.NetStandard.NetStandard14.SystemThreading,
                            MetadataReferences.NetStandard.NetStandard14.SystemThreadingTasks,
                            MetadataReferences.NetStandard.NetStandard14.SystemThreadingTimer,
                            MetadataReferences.NetStandard.NetStandard14.SystemXmlReaderWriter,
                            MetadataReferences.NetStandard.NetStandard14.SystemXmlXDocument),
                        ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>());
                }
            }

            public static ReferenceAssemblies NetStandard15
            {
                get
                {
                    return new ReferenceAssemblies(
                        ImmutableArray.Create(
                            MetadataReferences.NetStandard.NetStandard15.MicrosoftWin32Primitives,
                            MetadataReferences.NetStandard.NetStandard15.SystemAppContext,
                            MetadataReferences.NetStandard.NetStandard15.SystemCollections,
                            MetadataReferences.NetStandard.NetStandard15.SystemCollectionsConcurrent,
                            MetadataReferences.NetStandard.NetStandard15.SystemConsole,
                            MetadataReferences.NetStandard.NetStandard15.SystemDiagnosticsDebug,
                            MetadataReferences.NetStandard.NetStandard15.SystemDiagnosticsTools,
                            MetadataReferences.NetStandard.NetStandard15.SystemDiagnosticsTracing,
                            MetadataReferences.NetStandard.NetStandard15.SystemGlobalization,
                            MetadataReferences.NetStandard.NetStandard15.SystemGlobalizationCalendars,
                            MetadataReferences.NetStandard.NetStandard15.SystemIO,
                            MetadataReferences.NetStandard.NetStandard15.SystemIOCompression,
                            MetadataReferences.NetStandard.NetStandard15.SystemIOCompressionZipFile,
                            MetadataReferences.NetStandard.NetStandard15.SystemIOFileSystem,
                            MetadataReferences.NetStandard.NetStandard15.SystemIOFileSystemPrimitives,
                            MetadataReferences.NetStandard.NetStandard15.SystemLinq,
                            MetadataReferences.NetStandard.NetStandard15.SystemLinqExpressions,
                            MetadataReferences.NetStandard.NetStandard15.SystemNetHttp,
                            MetadataReferences.NetStandard.NetStandard15.SystemNetPrimitives,
                            MetadataReferences.NetStandard.NetStandard15.SystemNetSockets,
                            MetadataReferences.NetStandard.NetStandard15.SystemObjectModel,
                            MetadataReferences.NetStandard.NetStandard15.SystemReflection,
                            MetadataReferences.NetStandard.NetStandard15.SystemReflectionExtensions,
                            MetadataReferences.NetStandard.NetStandard15.SystemReflectionPrimitives,
                            MetadataReferences.NetStandard.NetStandard15.SystemResourcesResourceManager,
                            MetadataReferences.NetStandard.NetStandard15.SystemRuntime,
                            MetadataReferences.NetStandard.NetStandard15.SystemRuntimeExtensions,
                            MetadataReferences.NetStandard.NetStandard15.SystemRuntimeHandles,
                            MetadataReferences.NetStandard.NetStandard15.SystemRuntimeInteropServices,
                            MetadataReferences.NetStandard.NetStandard15.SystemRuntimeInteropServicesRuntimeInformation,
                            MetadataReferences.NetStandard.NetStandard15.SystemRuntimeNumerics,
                            MetadataReferences.NetStandard.NetStandard15.SystemSecurityCryptographyAlgorithms,
                            MetadataReferences.NetStandard.NetStandard15.SystemSecurityCryptographyEncoding,
                            MetadataReferences.NetStandard.NetStandard15.SystemSecurityCryptographyPrimitives,
                            MetadataReferences.NetStandard.NetStandard15.SystemSecurityCryptographyX509Certificates,
                            MetadataReferences.NetStandard.NetStandard15.SystemTextEncoding,
                            MetadataReferences.NetStandard.NetStandard15.SystemTextEncodingExtensions,
                            MetadataReferences.NetStandard.NetStandard15.SystemTextRegularExpressions,
                            MetadataReferences.NetStandard.NetStandard15.SystemThreading,
                            MetadataReferences.NetStandard.NetStandard15.SystemThreadingTasks,
                            MetadataReferences.NetStandard.NetStandard15.SystemThreadingTimer,
                            MetadataReferences.NetStandard.NetStandard15.SystemXmlReaderWriter,
                            MetadataReferences.NetStandard.NetStandard15.SystemXmlXDocument),
                        ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>());
                }
            }

            public static ReferenceAssemblies NetStandard16
            {
                get
                {
                    return new ReferenceAssemblies(
                        ImmutableArray.Create(
                            MetadataReferences.NetStandard.NetStandard16.MicrosoftWin32Primitives,
                            MetadataReferences.NetStandard.NetStandard16.SystemAppContext,
                            MetadataReferences.NetStandard.NetStandard16.SystemCollections,
                            MetadataReferences.NetStandard.NetStandard16.SystemCollectionsConcurrent,
                            MetadataReferences.NetStandard.NetStandard16.SystemConsole,
                            MetadataReferences.NetStandard.NetStandard16.SystemDiagnosticsDebug,
                            MetadataReferences.NetStandard.NetStandard16.SystemDiagnosticsTools,
                            MetadataReferences.NetStandard.NetStandard16.SystemDiagnosticsTracing,
                            MetadataReferences.NetStandard.NetStandard16.SystemGlobalization,
                            MetadataReferences.NetStandard.NetStandard16.SystemGlobalizationCalendars,
                            MetadataReferences.NetStandard.NetStandard16.SystemIO,
                            MetadataReferences.NetStandard.NetStandard16.SystemIOCompression,
                            MetadataReferences.NetStandard.NetStandard16.SystemIOCompressionZipFile,
                            MetadataReferences.NetStandard.NetStandard16.SystemIOFileSystem,
                            MetadataReferences.NetStandard.NetStandard16.SystemIOFileSystemPrimitives,
                            MetadataReferences.NetStandard.NetStandard16.SystemLinq,
                            MetadataReferences.NetStandard.NetStandard16.SystemLinqExpressions,
                            MetadataReferences.NetStandard.NetStandard16.SystemNetHttp,
                            MetadataReferences.NetStandard.NetStandard16.SystemNetPrimitives,
                            MetadataReferences.NetStandard.NetStandard16.SystemNetSockets,
                            MetadataReferences.NetStandard.NetStandard16.SystemObjectModel,
                            MetadataReferences.NetStandard.NetStandard16.SystemReflection,
                            MetadataReferences.NetStandard.NetStandard16.SystemReflectionExtensions,
                            MetadataReferences.NetStandard.NetStandard16.SystemReflectionPrimitives,
                            MetadataReferences.NetStandard.NetStandard16.SystemResourcesResourceManager,
                            MetadataReferences.NetStandard.NetStandard16.SystemRuntime,
                            MetadataReferences.NetStandard.NetStandard16.SystemRuntimeExtensions,
                            MetadataReferences.NetStandard.NetStandard16.SystemRuntimeHandles,
                            MetadataReferences.NetStandard.NetStandard16.SystemRuntimeInteropServices,
                            MetadataReferences.NetStandard.NetStandard16.SystemRuntimeInteropServicesRuntimeInformation,
                            MetadataReferences.NetStandard.NetStandard16.SystemRuntimeNumerics,
                            MetadataReferences.NetStandard.NetStandard16.SystemSecurityCryptographyAlgorithms,
                            MetadataReferences.NetStandard.NetStandard16.SystemSecurityCryptographyEncoding,
                            MetadataReferences.NetStandard.NetStandard16.SystemSecurityCryptographyPrimitives,
                            MetadataReferences.NetStandard.NetStandard16.SystemSecurityCryptographyX509Certificates,
                            MetadataReferences.NetStandard.NetStandard16.SystemTextEncoding,
                            MetadataReferences.NetStandard.NetStandard16.SystemTextEncodingExtensions,
                            MetadataReferences.NetStandard.NetStandard16.SystemTextRegularExpressions,
                            MetadataReferences.NetStandard.NetStandard16.SystemThreading,
                            MetadataReferences.NetStandard.NetStandard16.SystemThreadingTasks,
                            MetadataReferences.NetStandard.NetStandard16.SystemThreadingTimer,
                            MetadataReferences.NetStandard.NetStandard16.SystemXmlReaderWriter,
                            MetadataReferences.NetStandard.NetStandard16.SystemXmlXDocument),
                        ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>());
                }
            }

            public static ReferenceAssemblies NetStandard2
            {
                get
                {
                    var referenceAssembliesPath = Path.Combine(MetadataReferences.PackagesPath, "netstandard.library", "2.0.0", "build", "netstandard2.0", "ref");
                    var referenceAssemblies = Directory.GetFiles(referenceAssembliesPath, "*.dll");
                    var metadataReferences = referenceAssemblies.Select(path => MetadataReferences.CreateReferenceFromFile(path));
                    return new ReferenceAssemblies(
                        ImmutableArray.CreateRange(metadataReferences),
                        ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>());
                }
            }
        }

        public static class Runtime
        {
            public static ReferenceAssemblies Current
            {
                get
                {
                    var metadataReferences =
                        new[]
                        {
                            typeof(object).GetTypeInfo().Assembly,
                            typeof(System.Diagnostics.Debug).GetTypeInfo().Assembly,
                            typeof(Enumerable).GetTypeInfo().Assembly,
                            typeof(Microsoft.VisualBasic.Strings).GetTypeInfo().Assembly,
                        }
                        .Select(assembly => assembly.Location)
                        .Distinct()
                        .Select(location => MetadataReferences.CreateReferenceFromFile(location))
                        .ToImmutableArray();

                    return new ReferenceAssemblies(
                        metadataReferences,
                        ImmutableDictionary.Create<string, ImmutableArray<MetadataReference>>());
                }
            }
        }
    }
}
