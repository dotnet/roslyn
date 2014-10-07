using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Roslyn.Workspaces
{
    public static class WorkspaceKind
    {
        public const string Host = "Host";
        public const string Editor = "Editor";
        public const string Any = "*";
    }
}