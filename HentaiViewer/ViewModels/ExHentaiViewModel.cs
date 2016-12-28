﻿using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using HentaiViewer.Common;
using HentaiViewer.Models;
using HentaiViewer.Sites;
using HentaiViewer.Views;
using PropertyChanged;

namespace HentaiViewer.ViewModels {
	[ImplementPropertyChanged]
	public class ExHentaiViewModel {
		public static ExHentaiViewModel Instance;

		private readonly ObservableCollection<HentaiModel> _exHentai = new ObservableCollection<HentaiModel>();

		public ExHentaiViewModel() {
			Instance = this;
			ExHentaiItems = new ReadOnlyObservableCollection<HentaiModel>(_exHentai);

			//RefreshExHentaiAsync();
			RefreshExHentaiCommand = new ActionCommand(RefreshExHentaiAsync);
			LoadMoreExHentaiCommand = new ActionCommand(async () => {
				ExHentaiLoadedPage++;
				await LoadExHentaiPage();
			});
			LoadPrevExHentaiCommand = new ActionCommand(async () => {
				if (ExHentaiLoadedPage == 0) return;
				ExHentaiLoadedPage--;
				await LoadExHentaiPage();
			});
		}

		public int ExHentaiLoadedPage { get; set; }
		public int NextExHentaiPage { get; set; } = 1;
		public bool ExHentaiPageLoading { get; set; }
		public ReadOnlyObservableCollection<HentaiModel> ExHentaiItems { get; }

		public ICommand RefreshExHentaiCommand { get; }
		public ICommand LoadMoreExHentaiCommand { get; }
		public ICommand LoadPrevExHentaiCommand { get; }

		private async void RefreshExHentaiAsync() {
			await LoadExHentaiPage();
		}

		private async Task LoadExHentaiPage() {
			var pass = SettingsController.Settings.ExHentai.IpbPassHash;
			var memid = SettingsController.Settings.ExHentai.IpbMemberId;
			var sessip = SettingsController.Settings.ExHentai.IpbSessionId;
			if (string.IsNullOrEmpty(memid)||string.IsNullOrEmpty(sessip)||string.IsNullOrEmpty(pass)) {
				MessageBox.Show("Need Cookies for Exhentai", "Cookies missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			if (ExHentaiPageLoading) return;
			ExHentaiPageLoading = true;
			NextExHentaiPage = ExHentaiLoadedPage + 1;
			if (_exHentai.Count > 0) _exHentai.Clear();
			ExHentaiView.Instance.ScrollViewer.ScrollToTop();
			var searchQuery = string.Empty;
			if (!string.IsNullOrEmpty(SettingsController.Settings.ExHentai.SearchQuery)) {
				searchQuery = SettingsController.Settings.ExHentai.SearchQuery.Replace(" ", "" + "+");
			}
			var i = await ExHentai.GetLatest($"https://exhentai.org/?page={ExHentaiLoadedPage}" +
			                                 $"&f_doujinshi={SettingsController.Settings.ExHentai.Doujinshi}" +
			                                 $"&f_manga={SettingsController.Settings.ExHentai.Manga}" +
			                                 $"&f_artistcg={SettingsController.Settings.ExHentai.ArtistCg}" +
			                                 $"&f_gamecg={SettingsController.Settings.ExHentai.GameCg}" +
			                                 $"&f_western={SettingsController.Settings.ExHentai.Western}" +
			                                 $"&f_non-h={SettingsController.Settings.ExHentai.NonH}" +
			                                 $"&f_imageset={SettingsController.Settings.ExHentai.ImageSet}" +
			                                 $"&f_cosplay={SettingsController.Settings.ExHentai.Cosplay}" +
			                                 $"&f_asianporn={SettingsController.Settings.ExHentai.AsianPorn}" +
			                                 $"&f_misc={SettingsController.Settings.ExHentai.Misc}" +
			                                 $"&f_search={searchQuery}&f_apply=Apply+Filter");
			foreach (var hentaiModel in i) {
				_exHentai.Add(hentaiModel);
				await Task.Delay(100);
			}
			ExHentaiPageLoading = false;
		}
	}
}