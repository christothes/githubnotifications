using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Data.Tables;
using GitHubNotifications.Models;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http.Json;
using Microsoft.JSInterop;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Azure;
using Microsoft.AspNetCore.Components.Authorization;
using System.Linq;
using GitHubNotifications.Shared;

namespace GitHubNotifications.Client
{
    public class DataService
    {
        private readonly HttpClient Http;
        private readonly IJSRuntime JS;
        private readonly AuthenticationState AuthState;
        private readonly NavigationManager NavigationManager;
        private string icon = "images/github.png";
        private object notif;
        private HubConnection hubConnection;
        private TableClient tableClient = null;
        private UserOptions userOptions = new UserOptions();
        private string userLogin = string.Empty;
        private Action ReportProgress;
        private int progressVal = 0;
        internal string IsInProgress => progressVal == 0 || progressVal >= 100 ? "collapse" : "visible";
        internal string Progress = "0%";
        internal List<string> checks = new();
        internal Dictionary<string, ClientComment> commentLookup = new();
        internal bool OnlyMyPRs
        {
            get => userOptions.OnlyMyPRs;
            set => userOptions.OnlyMyPRs = (bool)value;
        }

        public DataService(HttpClient httpClient, IJSRuntime jSRuntime, NavigationManager navigationManager, HostAuthenticationStateProvider auth)
        {
            Http = httpClient;
            JS = jSRuntime;
            NavigationManager = navigationManager;
            AuthState = auth.GetAuthenticationStateAsync().GetAwaiter().GetResult();
            userLogin = AuthState.User.GetGitHubLogin();
        }

        public async Task InitAsync(Action reportProgress)
        {
            if (reportProgress == null)
            {
                ReportProgress = () => Console.WriteLine("InitAsync progress");
            }
            else
            {
                ReportProgress = reportProgress;
            }
            hubConnection = new HubConnectionBuilder()
                .WithUrl(NavigationManager.ToAbsoluteUri("/notificationshub"))
                .WithAutomaticReconnect()
                .Build();

            hubConnection.On<DateTime, string, string, string, string, string>("CheckStatus", async (updated, id, title, message, url, author) =>
            {
                Console.WriteLine($"Check failure for {title}");
                if (author == userLogin)
                {
                    var encodedMsg = $"{updated.ToLocalTime().ToString("h:mm tt")}: {title}";
                    checks.Add(encodedMsg);
                    await CreateNotifcationAsync(updated.ToLocalTime(), id, title, message, url);
                }
            });

            hubConnection.On<ClientComment>("NewComment", async (comment) =>
            {
                if (OnlyMyPRs && comment.prAuthor != userLogin)
                {
                    return;
                }
                if (null == comment.parentId || "0" == comment.parentId)
                {
                    commentLookup[comment.id] = comment;
                }
                AddComment(comment);
                await CreateNotifcationAsync(comment.created, comment.id, comment.title, $"@{comment.author}: {comment.body}", comment.uri);
                ReportProgress();
            });


            if (!IsConnected && AuthState.User.Claims.Any())
            {
                await Connect();
            }

            try
            {
                var stringResult = await Http.GetStringAsync("Comment/GetSasUrl");
                UpdateProgress(progressVal += 20);
                ReportProgress();
                Console.WriteLine(progressVal);
                userOptions = await Http.GetFromJsonAsync<UserOptions>("User/GetOptions");
                UpdateProgress(progressVal += 20);
                ReportProgress();
                Console.WriteLine(progressVal);
                OnlyMyPRs = userOptions?.OnlyMyPRs ?? true;
                var url = new Uri(stringResult);
                tableClient = new TableClient(url, new AzureSasCredential(url.Query), null);
                await LoadComments();
                UpdateProgress(0);
            }
            catch { }
        }

        private void UpdateProgress(int val)
        {
            progressVal = val;
            Progress = progressVal.ToString() + "%";
        }

        internal async Task OnlyMyPRsClick()
        {
            // onclick happens before the binding is invoked,
            //vso do the opposite of the value.
            userOptions.OnlyMyPRs = !userOptions.OnlyMyPRs;
            UpdateProgress(0);
            ReportProgress();
            await Http.PostAsJsonAsync("user/SetOptions", userOptions);
            UpdateProgress(10);
            ReportProgress();
            await LoadComments();
            UpdateProgress(0);
        }

        private async Task CreateNotifcationAsync(DateTime updated, string id, string title, string body, string url)
        {
            var options = new
            {
                Body = body,
                Icon = icon,
                Renotify = true, // By default a notification is not overwritten, so there can only be one.
                RequireInteraction = false,
                Tag = id,
                TimeStamp = updated,
                Uri = url
            };
            try
            {
                notif = await JS.InvokeAsync<object>("createNotification", title, options);
            }
            catch (Exception)
            { }
        }

        private async Task LoadComments()
        {
            commentLookup.Clear();
            UpdateProgress(Math.Max(75, progressVal + 20));
            ReportProgress();
            Console.WriteLine(progressVal);
            var comments = new List<ClientComment>();
            var ago = DateTime.UtcNow.AddDays(-2);
            Expression<Func<PRComment, bool>> filter = c => c.Created >= ago;
            if (userOptions?.OnlyMyPRs ?? false)
            {
                filter = c => c.Created >= ago && c.PartitionKey == userLogin;
            }
            await foreach (PRComment comment in tableClient.QueryAsync<PRComment>(filter))
            {
                var cc = new ClientComment
                {
                    prAuthor = comment.PartitionKey,
                    author = comment.Author,
                    body = comment.Body,
                    created = comment.Created.ToLocalTime(),
                    id = comment.RowKey,
                    parentId = comment.ParentId,
                    parentAuthor = comment.ParentAuthor,
                    title = comment.PrTitle,
                    prNumber = comment.PrNumber,
                    uri = comment.Uri,
                    sortDate = comment.Created.ToLocalTime()
                };

                comments.Add(cc);

                if (null == cc.parentId || "0" == cc.parentId)
                {
                    commentLookup[cc.id] = cc;
                }
            }

            foreach (var comment in comments)
            {
                AddComment(comment);
            }
            UpdateProgress(100);
            ReportProgress();
        }

        private void AddComment(ClientComment comment)
        {
            comment.sortDate = comment.created.ToLocalTime();
            comment.created = comment.sortDate;
            if (comment.parentId != "0" && comment.parentId != null && commentLookup.TryGetValue(comment.parentId, out var parent))
            {
                parent.replies ??= new List<ClientComment>();
                parent.replies.Add(comment);
                if (comment.created > parent.sortDate)
                {
                    parent.sortDate = comment.created;
                }
            }
        }

        async Task Connect() { await hubConnection.StartAsync(); }


        public bool IsConnected => hubConnection?.State == HubConnectionState.Connected;

        public async ValueTask DisposeAsync() { await hubConnection.DisposeAsync(); }
    }
}