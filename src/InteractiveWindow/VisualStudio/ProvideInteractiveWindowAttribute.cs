// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Drawing;
using System.Globalization;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.InteractiveWindow.Shell
{
    /// <summary>
    /// This attribute declares that a package own an interactive window.  Visual Studio uses this 
    /// information to handle the positioning and persistence of your window. The attributes on a 
    /// package do not control the behavior of the package, but they can be used by registration 
    /// tools to register the proper information with Visual Studio.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class ProvideInteractiveWindowAttribute : RegistrationAttribute
    {
        public ProvideInteractiveWindowAttribute(string guid)
        {
            this.Id = new Guid(guid);
        }

        public Guid Id { get; }

        public Rectangle Position { get; set; }

        /// <summary>
        /// Default DockStyle for the ToolWindow
        /// </summary>
        public VsDockStyle Style { get; set; }

        /// <summary>
        /// Default width of the ToolWindow when docked
        /// </summary>
        public int DockedWidth { get; set; }

        /// <summary>
        /// Default height of the ToolWindow when docked
        /// </summary>
        public int DockedHeight { get; set; }

        /// <summary>
        /// Default Orientation for the ToolWindow, relative to the window specified by the Window Property
        /// </summary>
        public ToolWindowOrientation Orientation { get; set; }

        /// <summary>
        /// Default Window that the ToolWindow will be docked with
        /// </summary>
        public string Window { get; set; }

        /// <summary>
        /// Set to true if you want a tool window that behaves and has a lifetime like a document.
        /// The tool window will only be MDI or floating and will remain visible in its position across all layout changes
        /// until manually closed by the user at which point it will be destroyed.  
        /// This flag implies DontForceCreate and destructive multi instance.
        /// </summary>
        public bool DocumentLikeTool { get; set; }

        private string GetRegistryKeyName()
        {
            return "ToolWindows\\" + Id.ToString("B");
        }

        /// <summary>
        /// Called to register this attribute with the given context.  The context
        /// contains the location where the registration information should be placed.
        /// it also contains such as the type being registered, and path information.
        /// </summary>
        public override void Register(RegistrationContext context)
        {
            using (Key childKey = context.CreateKey(GetRegistryKeyName()))
            {
                // Package owning this tool window
                childKey.SetValue(string.Empty, context.ComponentType.GUID.ToString("B"));

                if (Orientation != ToolWindowOrientation.none)
                {
                    childKey.SetValue("Orientation", OrientationToString(Orientation));
                }

                if (Style != VsDockStyle.none)
                {
                    childKey.SetValue("Style", StyleToString(Style));
                }

                if (!string.IsNullOrEmpty(Window))
                {
                    childKey.SetValue("Window", Window);
                }

                if (Position.Width != 0 && Position.Height != 0)
                {
                    string positionString = string.Format(CultureInfo.InvariantCulture, "{0}, {1}, {2}, {3}",
                                Position.Left,
                                Position.Top,
                                Position.Right,
                                Position.Bottom);

                    childKey.SetValue("Float", positionString);
                }

                if (DockedWidth > 0)
                {
                    childKey.SetValue("DockedWidth", DockedWidth);
                }

                if (DockedHeight > 0)
                {
                    childKey.SetValue("DockedHeight", DockedHeight);
                }

                if (DocumentLikeTool)
                {
                    childKey.SetValue("DocumentLikeTool", 1);
                }
            }
        }

        /// <summary>
        /// Unregister this Tool Window.
        /// </summary>
        public override void Unregister(RegistrationContext context)
        {
            context.RemoveKey(GetRegistryKeyName());
        }

        private string StyleToString(VsDockStyle style)
        {
            switch (style)
            {
                case VsDockStyle.MDI: return "MDI";
                case VsDockStyle.Float: return "Float";
                case VsDockStyle.Linked: return "Linked";
                case VsDockStyle.Tabbed: return "Tabbed";
                case VsDockStyle.AlwaysFloat: return "AlwaysFloat";
                case VsDockStyle.none: return string.Empty;
                default:
                    // TODO: error message
                    throw new ArgumentException("Style");
            }
        }

        private string OrientationToString(ToolWindowOrientation position)
        {
            switch (position)
            {
                case ToolWindowOrientation.Top: return "Top";
                case ToolWindowOrientation.Left: return "Left";
                case ToolWindowOrientation.Right: return "Right";
                case ToolWindowOrientation.Bottom: return "Bottom";
                case ToolWindowOrientation.none: return string.Empty;
                default:
                    // TODO: error message
                    throw new ArgumentException("Orientation");
            }
        }
    }
}

