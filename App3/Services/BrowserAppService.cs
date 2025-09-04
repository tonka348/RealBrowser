using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;

namespace App3.Services
{
    public sealed class BrowserAppService : IBackgroundTask
    {
        private BackgroundTaskDeferral? _backgroundTaskDeferral;
        private AppServiceConnection? _appServiceConnection;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // Get a deferral so the task doesn't complete immediately
            _backgroundTaskDeferral = taskInstance.GetDeferral();

            // Associate a cancellation handler with the background task
            taskInstance.Canceled += OnTaskCanceled;

            // Retrieve the app service connection and set up listeners
            var details = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            _appServiceConnection = details?.AppServiceConnection;

            if (_appServiceConnection != null)
            {
                _appServiceConnection.RequestReceived += OnRequestReceived;
                _appServiceConnection.ServiceClosed += OnServiceClosed;
            }
        }

        private async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            // Get a deferral because this handler is async
            var messageDeferral = args.GetDeferral();

            try
            {
                var message = args.Request.Message;
                var response = new ValueSet();

                // Handle different service requests
                if (message.TryGetValue("Command", out var command))
                {
                    switch (command.ToString())
                    {
                        case "GetAppState":
                            var appState = await BackgroundServiceManager.Instance.LoadAppStateAsync();
                            if (appState != null)
                            {
                                response["Status"] = "Success";
                                response["Data"] = System.Text.Json.JsonSerializer.Serialize(appState);
                            }
                            else
                            {
                                response["Status"] = "NoData";
                            }
                            break;

                        case "SaveAppState":
                            if (message.TryGetValue("Data", out var data))
                            {
                                var appStateJson = data?.ToString();
                                if (!string.IsNullOrEmpty(appStateJson))
                                {
                                    var deserializedAppState = System.Text.Json.JsonSerializer.Deserialize<AppState>(appStateJson);
                                    await BackgroundServiceManager.Instance.SaveAppStateAsync(deserializedAppState);
                                    response["Status"] = "Success";
                                }
                                else
                                {
                                    response["Status"] = "Error";
                                    response["Message"] = "No data provided";
                                }
                            }
                            else
                            {
                                response["Status"] = "Error";
                                response["Message"] = "No data provided";
                            }
                            break;

                        case "Ping":
                            response["Status"] = "Success";
                            response["Message"] = "Pong";
                            break;

                        default:
                            response["Status"] = "Error";
                            response["Message"] = "Unknown command";
                            break;
                    }
                }
                else
                {
                    response["Status"] = "Error";
                    response["Message"] = "No command specified";
                }

                // Send the response
                await args.Request.SendResponseAsync(response);
            }
            catch (Exception ex)
            {
                // Send error response
                var errorResponse = new ValueSet
                {
                    ["Status"] = "Error",
                    ["Message"] = ex.Message
                };
                await args.Request.SendResponseAsync(errorResponse);
            }
            finally
            {
                // Complete the deferral
                messageDeferral.Complete();
            }
        }

        private void OnServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            // Complete the background task
            _backgroundTaskDeferral?.Complete();
        }

        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            // Complete the background task
            _backgroundTaskDeferral?.Complete();
        }
    }
}
    // Replace the following block inside OnRequestReceived:

   