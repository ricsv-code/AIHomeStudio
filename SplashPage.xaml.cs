using AIHomeStudio.Utilities;

namespace AIHomeStudio
{
    public partial class SplashPage : ContentPage
    {
        public SplashPage()
        {
            InitializeComponent();

            UIHooks.OnSplashUpdate = UpdateStatus;
        }

        public void UpdateStatus(string message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusLabel.Text = message;
            });
        }
    }
}
