using System;

namespace Roslyn.Test.Utilities
{
    public class ConditionalStaFactAttribute : StaFactAttribute
    {
        public ConditionalStaFactAttribute(Type skipCondition)
        {
            var condition = Activator.CreateInstance(skipCondition) as ExecutionCondition;

            if (condition.ShouldSkip)
            {
                Skip = condition.SkipReason;
            }
        }
    }
}
