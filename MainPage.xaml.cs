using AIHomeStudio.Models;

namespace AIHomeStudio
{
    public partial class MainPage : ContentPage
    {
        public MainPage(AIHomeStudioCore core)
        {
            InitializeComponent();
            BindingContext = core;
        }
    }
}
