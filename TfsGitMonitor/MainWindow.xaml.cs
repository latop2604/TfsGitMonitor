using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;

namespace TfsGitMonitor
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private string LastCommitId { get; set; }
        private Timer _timer;

        private GitRepository SelectedGitRepo
        {
            get
            {
                GitRepository gitRepo = ReposBox.SelectedItem as GitRepository;
                return gitRepo;
            }
        }

        private GitRef SelectedGitBranch
        {
            get { return BranchesBox.SelectedItem as GitRef; }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private async Task LoadRepos()
        {
            var gitClient = GitHttpClient();
            var repos = await gitClient.GetRepositoriesAsync();

            ReposBox.SelectedValuePath = "Id";
            ReposBox.DisplayMemberPath = "Name";
            ReposBox.ItemsSource = repos;
            ReposBox.SelectedItem = repos.FirstOrDefault();
        }

        private async Task LoadBranches()
        {
            var gitRepo = SelectedGitRepo;
            if (gitRepo == null) return;

            var gitClient = GitHttpClient();
            var branches = await gitClient.GetBranchRefsAsync(gitRepo.Id);

            BranchesBox.SelectedValuePath = "ObjectId";
            BranchesBox.DisplayMemberPath = "Name";
            BranchesBox.ItemsSource = branches;
            BranchesBox.SelectedItem = branches.FirstOrDefault();
        }

        private async Task LoadCommitAsync()
        {
            var gitClient = GitHttpClient();
            //var project = gitClient.GetProjectRepositoriesAsync("Recruiting").Result.First(repo => repo.Name == "Recruiting");

            GitRepository gitRepo = SelectedGitRepo;
            if (gitRepo == null) return;

            GitRef gitBranch = SelectedGitBranch;
            if (gitBranch == null) return;


            GitVersionDescriptor gitBranchVersionDescriptor = new GitVersionDescriptor { VersionType = GitVersionType.Branch, Version = gitBranch.Name.Replace("refs/heads/", "") };
            //GitVersionDescriptor gitBranchPreviousChangeVersionDescriptor = new GitVersionDescriptor { VersionType = GitVersionType.Branch, Version = gitBranch.Name.Replace("refs/heads/", ""), VersionOptions = GitVersionOptions.PreviousChange };
            //GitVersionDescriptor gitBranchParentVersionDescriptor = new GitVersionDescriptor { VersionType = GitVersionType.Branch, Version = gitBranch.Name.Replace("refs/heads/", ""), VersionOptions = GitVersionOptions.FirstParent };

            //var branchFo = branches.First(b => b.Name.EndsWith("828_dynamicForms-fo")).ObjectId;
            //var stats = gitClient.GetBranchesStatisticsAsync(gitRepo.Id.ToString()).Result;
            //var statsFo = stats.First(s => s.Name.EndsWith("828_dynamicForms-fo"));


            var itemCollection =
                await
                    gitClient.GetItemsAsync(gitRepo.Id, "/",
                        gitBranchVersionDescriptor,
                        VersionControlRecursionType.OneLevel, true, true);

            //if (LastCommitId == null)
            //{
            //    var firstItem = itemCollection.FirstOrDefault();
            //    LastCommitId = firstItem != null ? firstItem.CommitId : null;
            //}


            //var gitTreeA = await gitClient.GetTreeAsync(gitRepo.Id, gitBranch.ObjectId, true);

            //var gitTree = await gitClient.GetTreeAsync(gitRepo.Id, itemCollection.First().ObjectId, true);

            foreach (var item in itemCollection.Where(i => i.LatestProcessedChange != null))
            {
                //item.CommitId;
                //item.GitObjectType;
                //item.ObjectId;
                //item.Path;
                //item.VersionString;
                //item.LatestProcessedChange.Comment;
                //item.LatestProcessedChange.CommentTruncated;
                //item.LatestProcessedChange.Changes.First().Item.Path;
                //item.LatestProcessedChange.Author.Name;
                //item.LatestProcessedChange.Committer.Name;
                //item.LatestProcessedChange.Comment;
                //item.LatestProcessedChange.Comment;

                string log = item.LatestProcessedChange.Comment;
                if (item.LatestProcessedChange.CommentTruncated)
                {
                    log += " ...";
                    Console.WriteLine(item.LatestProcessedChange.CommitId + " # " + log);
                }
            }

            DataGrid.ItemsSource = itemCollection;
        }

        private async Task LoadDiffAsync()
        {
            if (string.IsNullOrEmpty(LoginTextBox.Text) ||string.IsNullOrEmpty(Password.Password))
                return;

            var gitClient = GitHttpClient();

            GitRepository gitRepo = SelectedGitRepo;
            if (gitRepo == null) return;

            GitRef gitBranch = SelectedGitBranch;
            if (gitBranch == null) return;

            string branchName = gitBranch.Name.Replace("refs/heads/", "");
            GitVersionDescriptor gitBranchVersionDescriptor = new GitVersionDescriptor { VersionType = GitVersionType.Branch, Version = branchName };

            var itemCollection =
                await
                    gitClient.GetItemsAsync(gitRepo.Id, "/",
                        gitBranchVersionDescriptor,
                        VersionControlRecursionType.OneLevel, true, true);

            var firstItem = itemCollection.FirstOrDefault();

            int commitCounted = 0;

            if (LastCommitId != null && firstItem != null && firstItem.CommitId != LastCommitId)
            {
                GitVersionDescriptor gitCommitVersionDescriptor = new GitVersionDescriptor { VersionType = GitVersionType.Commit, Version = LastCommitId };
                //GitVersionDescriptor gitCommitPreviousChangeVersionDescriptor = new GitVersionDescriptor { VersionType = GitVersionType.Commit, Version = LastCommitId, VersionOptions = GitVersionOptions.PreviousChange };
                //GitVersionDescriptor gitCommitParentVersionDescriptor = new GitVersionDescriptor { VersionType = GitVersionType.Commit, Version = LastCommitId, VersionOptions = GitVersionOptions.FirstParent };

                var diff = await gitClient.GetCommitDifferencesAsync(gitRepo.Id.ToString(), gitRepo.ProjectReference.Id.ToString(), gitCommitVersionDescriptor, gitBranchVersionDescriptor);


                commitCounted = diff.AheadCount != null ? diff.AheadCount.Value : 0;
                NewCommitCount.Text = "New Commit " + commitCounted;
            }

            if (ShowAllPopupCheckBox.IsChecked == true || commitCounted > 0)
            {
                NotifyIcon.ShowCustomBalloon(new PopUp { CommitCount = commitCounted, Text = branchName },
                    PopupAnimation.Slide, TfsGitSettings.PopUpVisibleDuration);
            }

            if (firstItem != null && firstItem.CommitId != null)
            {
                LastCommitId = firstItem.CommitId;
            }
        }

        private GitHttpClient GitHttpClient()
        {
            var windCredential =
                new WindowsCredential(new NetworkCredential(LoginTextBox.Text, Password.Password, TfsGitSettings.CredentialDomain));

            var gitClient = new GitHttpClient(TfsGitSettings.TfsUrl, new VssCredentials(windCredential));
            return gitClient;
        }

        private async Task Do(Func<Task> action)
        {
            Progress.Visibility = Visibility.Visible;
            await Do(
                action, 
                ex => MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error), 
                () => Progress.Visibility = Visibility.Collapsed);
        }
        private async Task Do(Func<Task> action, Action<Exception> afterCatch, Action afterFinaly)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                if (afterCatch != null)
                {
                    afterCatch(ex);
                }
            }
            finally
            {
                if (afterFinaly != null)
                {
                    afterFinaly();
                }
            }
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(LoginTextBox.Text) && !string.IsNullOrEmpty(Password.Password))
            {
                await Do(LoadRepos);
            }
        }

        private async void ReposBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await Do(LoadBranches);
        }

        private async void BranchesBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LastCommitId = null;
            await Do(LoadCommitAsync);
        }

        private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            var cred = CredentialManager.GetCredentials();
            if (cred != null)
            {
                LoginTextBox.Text = cred.UserName;
                Password.Password = cred.Password;
                LogInButton.Visibility = Visibility.Collapsed;
                LogOutButton.Visibility = Visibility.Visible;

                await Do(LoadRepos, exception => { Password.Password = string.Empty; }, null );
            }
            else
            {
                LogInButton.Visibility = Visibility.Visible;
                LogOutButton.Visibility = Visibility.Collapsed;
            }

            Password.Focus();
            _timer = new Timer();
            _timer.Interval = TfsGitSettings.IntervalBetweenRemoteCheck;
            _timer.Elapsed += TimerOnElapsed;
            _timer.Start();
        }

        private async void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            await Dispatcher.Invoke(async () => await Do(LoadDiffAsync, exception => { }, () => { }));
        }

        private async void Password_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrEmpty(LoginTextBox.Text) && !string.IsNullOrEmpty(Password.Password))
            {
                await Do(LoadRepos);

            }
        }

        private void ButtonClick_LogIn(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(LoginTextBox.Text) || string.IsNullOrEmpty(Password.Password))
                return;

            CredentialManager.SetCredentials(new CredentialManager.UserPass
            {
                UserName = LoginTextBox.Text,
                Password = Password.Password
            });
            LogInButton.Visibility = Visibility.Collapsed;
            LogOutButton.Visibility = Visibility.Visible;
        }

        private void ButtonClick_LogOut(object sender, RoutedEventArgs e)
        {
            CredentialManager.RemoveCredentials();
            LoginTextBox.Text = string.Empty;
            Password.Password = string.Empty;
            LogInButton.Visibility = Visibility.Visible;
            LogOutButton.Visibility = Visibility.Collapsed;
        }

        public bool CloseRequested { get; set; }

        private void ShowMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Normal;
            ShowInTaskbar = true;
        }

        private void HideMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
            ShowInTaskbar = false;
        }

        private void ExitMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            CloseRequested = true;
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!CloseRequested)
            {
                WindowState = WindowState.Minimized;
                ShowInTaskbar = false;
                e.Cancel = true;
            }
            base.OnClosing(e);
        }

        private void NotifyIcon_TrayBalloonTipClicked(object sender, RoutedEventArgs e)
        {
            NotifyIcon.CloseBalloon();
        }

        private void NotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            ShowMenuItem_OnClick(sender, e);
        }
    }
}