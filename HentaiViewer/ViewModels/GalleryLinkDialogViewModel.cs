﻿using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using HentaiViewer.Models;
using HentaiViewer.Views;

namespace HentaiViewer.ViewModels {
    public class GalleryLinkDialogViewModel : INotifyPropertyChanged {
        public GalleryLinkDialogViewModel() {
            ViewCommand = new ActionCommand(View);
        }

        public string Link { get; set; }

        public ICommand ViewCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void View() {
            var hm = new HentaiModel {
                Link = Link,
                Title = "lul"
            };
            if (Link.ToLower().Contains("hentai.org")) {
                hm.Site = "ExHentai.org";
            }
            else if (Link.ToLower().Contains("nhentai")) {
                hm.Site = "nHentai.net";
            }
            else if (Link.ToLower().Contains("hentai.cafe")) {
                hm.Site = "Hentai.cafe";
            }
            else {
                return;
            }
            var viewWindow = new HentaiViewerWindow {
                DataContext = new HentaiViewerWindowViewModel(hm),
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            viewWindow.Show();
        }
    }
}