// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Xml.Linq;

namespace Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services.Definitions;
/// <summary>
/// Copied from https://devdiv.visualstudio.com/DevDiv/_git/CPS?path=/src/Microsoft.VisualStudio.ProjectSystem.Server/ProjectInitializationCompletionState.cs
/// </summary>
[DataContract]
internal class ProjectInitializationCompletionState
{
    [DataMember(IsRequired = true, EmitDefaultValue = false, Name = "environmentStateVersion")]
    public int EnvironmentStateVersion { get; set; }

    [DataMember(IsRequired = false, EmitDefaultValue = true, Name = "projectsLoadedCount")]
    public int ProjectsLoadedCount { get; set; }

    [DataMember(IsRequired = false, EmitDefaultValue = true, Name = "projectsFailedCount")]
    public int ProjectsFailedCount { get; set; }

    [DataMember(IsRequired = true, EmitDefaultValue = false, Name = "stateUpdateVersion")]
    public int StateUpdateVersion { get; set; }
}
