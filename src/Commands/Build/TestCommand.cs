namespace PostSharp.Engineering.BuildTools.Commands.Build
{
    public class TestCommand : BaseBuildCommand<TestOptions>
    {
        protected override int ExecuteCore( BuildContext buildContext, TestOptions options )
        {
            if ( !buildContext.Product.Test( buildContext, options ) )
            {
                return 2;
            }

            return 0;
        }
    }
}