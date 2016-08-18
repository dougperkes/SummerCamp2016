using Template10.Mvvm;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using Template10.Services.NavigationService;
using Windows.UI.Xaml.Navigation;
using Bounce.Services;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using Windows.Storage;
using System.IO;

namespace Bounce.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        const string serviceEndpoint = "https://graph.microsoft.com/v1.0/";

        public MainPageViewModel()
        {
            if (Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                Value = "Designtime value";
            }
        }

        public async Task UploadFile(StorageFile file)
        {
            //JObject jResult = null;
            HttpClient client = new HttpClient();
            var token = await AuthenticationHelper.GetTokenForUserAsync();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            var dateString = DateTime.UtcNow.ToString("yyyy-dd-M--HH-mm-ss");
            Uri uploadEndpoint = new Uri(serviceEndpoint + "/me/drive/special/approot/children/" + dateString + ".jpg/content");

            using (var content = new StreamContent((await file.OpenAsync(FileAccessMode.Read)).AsStream()))
            {
                content.Headers.Remove("Content-Type");
                content.Headers.Add("Content-Type", "application/octet-stream");
                using (var req = new HttpRequestMessage(HttpMethod.Put, uploadEndpoint))
                {
                    req.Content = content;

                    using (HttpResponseMessage resp = await client.SendAsync(req))
                    {
                        var resptext = await resp.Content.ReadAsStringAsync();
                        resp.EnsureSuccessStatusCode();

                    }

                }
            }
        }

        string _Value = "Gas";
        string _graphToken = "";
        public string Value { get { return _Value; } set { Set(ref _Value, value); } }

        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> suspensionState)
        {
            if (string.IsNullOrEmpty(_graphToken)) {
                _graphToken = await Services.AuthenticationHelper.GetTokenForUserAsync();
            };
            if (suspensionState.Any())
            {
                Value = suspensionState[nameof(Value)]?.ToString();
            }
            await Task.CompletedTask;
        }

        public override async Task OnNavigatedFromAsync(IDictionary<string, object> suspensionState, bool suspending)
        {
            if (suspending)
            {
                suspensionState[nameof(Value)] = Value;
            }
            await Task.CompletedTask;
        }

        public override async Task OnNavigatingFromAsync(NavigatingEventArgs args)
        {
            args.Cancel = false;
            await Task.CompletedTask;
        }

        public void GotoDetailsPage() =>
            NavigationService.Navigate(typeof(Views.DetailPage), Value);

        public void GotoSettings() =>
            NavigationService.Navigate(typeof(Views.SettingsPage), 0);

        public void GotoPrivacy() =>
            NavigationService.Navigate(typeof(Views.SettingsPage), 1);

        public void GotoAbout() =>
            NavigationService.Navigate(typeof(Views.SettingsPage), 2);

        //public async void DoLogin() =>
        //    await AuthenticationHelper.GetTokenForUserAsync();

        //public void DoLogout() =>
        //    AuthenticationHelper.SignOut();

    }
}

