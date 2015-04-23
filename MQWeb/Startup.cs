using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(MQWeb.Startup))]
namespace MQWeb
{
    public partial class Startup {
        public void Configuration(IAppBuilder app) {
            ConfigureAuth(app);
        }
    }
}
