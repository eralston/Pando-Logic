using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(PandoLogic.Startup))]
namespace PandoLogic
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
