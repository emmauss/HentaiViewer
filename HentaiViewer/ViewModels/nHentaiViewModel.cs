﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using HentaiViewer.Common;
using HentaiViewer.Models;
using HentaiViewer.Sites;
using HentaiViewer.Views;
using PropertyChanged;

namespace HentaiViewer.ViewModels {
	[ImplementPropertyChanged]
	public class nHentaiViewModel {
		public static nHentaiViewModel Instance;

		private readonly ObservableCollection<HentaiModel> _nHentai = new ObservableCollection<HentaiModel>();

		public nHentaiViewModel() {
			Instance = this;
			NHentaiItems = new ReadOnlyObservableCollection<HentaiModel>(_nHentai);

			//RefreshnHentaiAsync();
			RefreshnHentaiCommand = new ActionCommand(RefreshnHentaiAsync);
			LoadMorenHentaiCommand = new ActionCommand(async () => {
				nHentaiLoadedPage++;
				await LoadnHentaiPage();
			});
			LoadPrevnHentaiCommand = new ActionCommand(async () => {
				if (nHentaiLoadedPage == 1) return;
				nHentaiLoadedPage--;
				await LoadnHentaiPage();
			});
		}

		public int nHentaiLoadedPage { get; set; } = 1;
		public int NextnHentaiPage { get; set; } = 2;
		public bool nHentaiPageLoading { get; set; }
		public ReadOnlyObservableCollection<HentaiModel> NHentaiItems { get; }

		public ICommand RefreshnHentaiCommand { get; }
		public ICommand LoadMorenHentaiCommand { get; }
		public ICommand LoadPrevnHentaiCommand { get; }

		public List<string> SortItems => new List<string> {"Date", "Popular"};

		public int SelectedSort { get; set; } = 0;


		public async Task LoadnHentaiPage(bool delete = true) {
			if (nHentaiPageLoading) return;
			nHentaiPageLoading = true;
			NextnHentaiPage = nHentaiLoadedPage + 1;
			if (_nHentai.Count > 0 && delete) _nHentai.Clear();
			nHentaiView.Instance.ScrollViewer.ScrollToTop();
			string searchquery = SettingsController.Settings.nHentai.SearchQuery;
			List<HentaiModel> i;
			if (string.IsNullOrEmpty(searchquery)) {
				i = await nHentai.GetLatest($"https://nhentai.net/?page={nHentaiLoadedPage}");
			}
			else {
				i = await nHentai.GetLatest(
				$"https://nhentai.net/search/?q={searchquery.Replace(" ", "+")}&sort={SortItems[SelectedSort].ToLower()}&page={nHentaiLoadedPage}");	
			}
			foreach (var hentaiModel in i) {
				_nHentai.Add(hentaiModel);
				await Task.Delay(100);
			}
			nHentaiPageLoading = false;
		}

		private async void RefreshnHentaiAsync() {
			await LoadnHentaiPage();
		}
	}
}