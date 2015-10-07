using System;

namespace Roslyn.Test.Utilities
{
    public class ConditionalWpfFactAttribute : WpfFactAttribute
    {
        public ConditionalWpfFactAttribute(Type skipCondition)
        {
            var condition = Activator.CreateInstance(skipCondition) as ExecutionCondition;

            if (condition.ShouldSkip)
            {
                Skip = condition.SkipReason;
            }
        }
    }
}
