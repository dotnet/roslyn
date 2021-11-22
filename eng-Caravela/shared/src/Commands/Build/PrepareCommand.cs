namespace PostSharp.Engineering.BuildTools.Commands.Build
{
    public class PrepareCommand : BaseBuildCommand<CommonOptions>
    {
        protected override int ExecuteCore( BuildContext buildContext, CommonOptions options )
        {
            if ( !buildContext.Product.Prepare( buildContext, options ) )
            {
                return 2;
            }

            return 0;
        }
    }
}