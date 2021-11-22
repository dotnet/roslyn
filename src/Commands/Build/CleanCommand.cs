namespace PostSharp.Engineering.BuildTools.Commands.Build
{
    public class CleanCommand : BaseBuildCommand<CommonOptions>
    {
        protected override int ExecuteCore( BuildContext buildContext, CommonOptions options )
        {
            buildContext.Product.Clean( buildContext );
            return 0;
        }
    }
}