using Microsoft.Owin;
using Owin;
using Common;

[assembly: OwinStartupAttribute(typeof(VideoStore.WebClient.Startup))]
namespace VideoStore.WebClient
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);

            Logging.AddFile("videostore.webclient");
        }
    }
}
