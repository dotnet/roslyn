// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.Designers
{
    /// <summary>
    ///     Provides a base class for all project tree modifiers.
    /// </summary>
    internal abstract class AbstractProjectTreeModifier : IProjectTreeModifier, IProjectTreeModifier2
    {
        protected AbstractProjectTreeModifier()
        {
        }

        public IProjectTree ApplyModifications(IProjectTree tree, IProjectTreeProvider projectTreeProvider)
        {
            return ApplyModifications(tree, (IProjectTree)null, projectTreeProvider);
        }

        public IProjectTree ApplyModifications(IProjectTree tree, IProjectTree previousTree, IProjectTreeProvider projectTreeProvider)
        {
            Requires.NotNull(tree, nameof(tree));
            Requires.NotNull(projectTreeProvider, nameof(projectTreeProvider));

            if (tree.IsProjectRoot())
                tree = ApplyModificationsToCompletedTree(tree);

            if (previousTree == null)
                tree = ApplyInitialModifications(tree);

            return ApplyModifications(tree, previousTree);
        }

        /// <summary>
        ///     Applies modifications to the specified tree, specifying the previous tree if available.
        /// </summary>
        protected virtual IProjectTree ApplyModifications(IProjectTree node, IProjectTree previousRoot)
        {
            return node;
        }

        /// <summary>
        ///     Applies initial modifications to the specified tree.
        /// </summary>
        protected virtual IProjectTree ApplyInitialModifications(IProjectTree node)
        {
            return node;
        }

        /// <summary>
        ///     Applies modifications to the specified project root.
        /// </summary>
        protected virtual IProjectTree ApplyModificationsToCompletedTree(IProjectTree node)
        {
            return node;
        }
    }
}
