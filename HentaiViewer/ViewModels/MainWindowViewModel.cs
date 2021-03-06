﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using HentaiViewer.Common;
using HentaiViewer.Models;
using HentaiViewer.Views;

namespace HentaiViewer.ViewModels {
    public class MainWindowViewModel : INotifyPropertyChanged {
        /// <summary>
        ///     Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainWindowViewModel() {
            OpenLinkCommand = new ActionCommand(OpenDialog);
            UpdateCommand = new ActionCommand(OpenUpdateLink);
            CloseCommand = new ActionCommand(Application.Current.Shutdown);
            CheckUpdateAsync();
            UniqueId = Common.UniqueId.Create();
            //Console.WriteLine(new List<string>()[5]);
        }

        public IEnumerable<object> DrawerItems
            =>
                new List<object> {
                    new DrawerListBoxItem {Name = "Home"},
                    new DrawerListBoxItem {Separator = true, IsEnabled = false},
                    new DrawerListBoxItem {Name = "ExHentai", Icon = "/Assets/faviconExHentai.ico"},
                    new DrawerListBoxItem {Name = "nHentai", Icon = "/Assets/faviconnHentai.ico"},
                    //new DrawerListBoxItem {Name = "Pururin", Icon = "/Assets/faviconPururin.ico"},
                    new DrawerListBoxItem {Name = "Cafe"},
                    new DrawerListBoxItem {Separator = true, IsEnabled = false},
                    new DrawerListBoxItem {Name = "Favorites"},
                    new DrawerListBoxItem {Name = "Saved Galleries"},
                    new DrawerListBoxItem {Name = "History"}
                };

        public string Version => GithubController._tag.ToString(CultureInfo.InvariantCulture);

        public int SelectedSite { get; set; }

        public bool IsUpdateAvailable { get; set; }

        public ICommand OpenLinkCommand { get; }

        public ICommand UpdateCommand { get; }

        public ICommand CloseCommand { get; }

        public string UniqueId { get; set; }

        public bool Nsfw { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private async void CheckUpdateAsync() {
            IsUpdateAvailable = await GithubController.CheckForUpdateAsync();
        }

        private void OpenUpdateLink() {
            if (string.IsNullOrEmpty(GithubController.GithubUrl)) {
                return;
            }
            Process.Start(GithubController.GithubUrl);
        }

        //private bool _dialogIsOpen = false;
        private void OpenDialog() {
            //if (_dialogIsOpen) {
            //	return;
            //}
            //_dialogIsOpen = true;
            //var dia = new GalleryLinkDialog();
            //await DialogHost.Show(dia);
            //_dialogIsOpen = false;

            if (!Clipboard.ContainsText() && !Clipboard.GetText().StartsWith("http")) {
                return;
            }
            var link = Clipboard.GetText();
            var hm = new HentaiModel {
                Link = link,
                Title = "lul"
            };
            if (link.ToLower().Contains("hentai.org/g/")) {
                hm.Site = "ExHentai.org";
            }
            else if (link.ToLower().Contains("nhentai.net/g/")) {
                hm.Site = "nHentai.net";
            }
            else if (link.ToLower().Contains("hentai.cafe")) {
                hm.Site = "Hentai.cafe";
            }
            else if (link.ToLower().Contains("pururin.us/gallery/")) {
                hm.Site = "Pururin.us";
            }
            else if (link.ToLower().Contains("imgur.com/a/")) {
                hm.Site = "Imgur.com";
            }
            else {
                return;
            }
            var viewWindow = new HentaiViewerWindow {
                DataContext = new HentaiViewerWindowViewModel(hm),
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            viewWindow.Show();
            viewWindow.Focus();
        }
    }
}