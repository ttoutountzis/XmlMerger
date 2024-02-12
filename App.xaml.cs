using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Controls;

namespace XmlMerger
{
    public partial class App : Application
    {
        public IConfiguration Configuration { get; private set; }
        public App(IConfiguration configuration)
        {
            InitializeComponent();

            Configuration = configuration;

            MainPage = new AppShell();
        }
    }
}
