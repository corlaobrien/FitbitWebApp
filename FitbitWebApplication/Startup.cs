using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(FitbitWebApplication.Startup))]
namespace FitbitWebApplication
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
