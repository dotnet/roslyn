// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.Internal.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences
{
    partial class NamingRuleTreeItemViewModel : IRenamePattern
    {
        public bool CanRename
        {
            get
            {
                // Don't allow rename on the root node, which is just a container for all the real
                // naming rules.
                return this.Parent != null;
            }
        }

        public IRenameItemTransaction BeginRename(object container, Func<IRenameItemTransaction, IRenameItemValidationResult> validator)
        {
            return new RenameTransaction(this, container, validator);
        }

        private class RenameTransaction : RenameItemTransaction
        {
            public RenameTransaction(NamingRuleTreeItemViewModel namingRule, object container, Func<IRenameItemTransaction, IRenameItemValidationResult> validator)
                : base(namingRule, container, validator)
            {
                this.RenameLabel = namingRule.Title;
                this.Completed += (s, e) =>
                {
                    namingRule.Title = this.RenameLabel;
                };
            }

            public override void Commit(RenameItemCompletionFocusBehavior completionFocusBehavior)
            {
                base.Commit(completionFocusBehavior);
            }
        }
    }
}
