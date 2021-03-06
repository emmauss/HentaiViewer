﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using HentaiViewer.Common;
using HentaiViewer.Models;
using RestSharp;

namespace HentaiViewer.Sites {
    public class HentaiCafe {
        public static async Task<List<HentaiModel>> GetLatestAsync(string url) {
            var document = await ParseHtmlStringAsync(await GetHtmlStringAsync(url));
            var hents = new List<HentaiModel>();
            var classes = document.All.Where(c => c.LocalName == "a" && c.ClassList.Contains("entry-thumb"));
            //var a = classes.Where(x => x.OuterHtml == "img");
            foreach (var element in classes) {
                var img = await ParseHtmlStringAsync(element.InnerHtml);
                var title = element.GetAttribute("title").Split(new[] {'"'}, StringSplitOptions.RemoveEmptyEntries)[1];
                hents.Add(new HentaiModel {
                    Title = title,
                    Link = element.GetAttribute("href"),
                    Img = new Uri(img.Images[0].Source),
                    ThumbnailLink = img.Images[0].Source,
                    Site = "Hentai.cafe",
                    Seen = HistoryController.CheckHistory(element.GetAttribute("href"))
                });
            }
            return hents;
        }

        private static async Task<IHtmlDocument> ParseHtmlStringAsync(string html) {
            //We require a custom configuration
            var config = Configuration.Default.WithJavaScript();
            //Let's create a new parser using this configuration
            var parser = new HtmlParser(config);
            //Just get the DOM representation
            return await parser.ParseAsync(html);
        }

        private static async Task<string> GetHtmlStringAsync(string url) {
            var client = new RestClient {
                UserAgent =
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.2785.70 Safari/537.36",
                Encoding = Encoding.UTF8,
                Timeout = 60000,
                BaseUrl = new Uri(url)
            };
            var request = new RestRequest();
            var response = await client.ExecuteGetTaskAsync(request);
            await Task.Delay(100);
            return response.Content;
        }

        public static async Task<(List<object>, int)> CollectImagesTaskAsync(HentaiModel hentai,
            Action<int, int> setPages) {
            if (Directory.Exists(hentai.SavePath) && hentai.IsSavedGallery) {
                var files =
                    new DirectoryInfo(hentai.SavePath).GetFileSystemInfos("*.???")
                        .OrderBy(fs => int.Parse(fs.Name.Split('.')[0]));
                var paths = new List<object>();
                files.ToList().ForEach(p => paths.Add(p.FullName));
                return (new List<object>(paths), files.Count());
            }
            var url = hentai.Link;
            //Let's create a new parser using this configuration
            var parser = new HtmlParser();
            //Just get the DOM representation
            var entryPage = await parser.ParseAsync(await GetHtmlStringAsync(url));
            var entryLink =
                entryPage.All.Where(l => l.LocalName == "a" && l.ClassList.Contains("x-btn-large")).ToList()[0]
                    .GetAttribute("href");
            if (hentai.Title == "lul") {
                var firstOrDefault = entryPage.All.FirstOrDefault(h => h.LocalName == "h3");
                if (firstOrDefault != null) {
                    hentai.Title = firstOrDefault.TextContent;
                }
            }
            var blankurl = entryLink + "page/";
            if (!entryLink.EndsWith("page/1")) {
                entryLink = entryLink + "page/1";
            }

            var html = await GetHtmlStringAsync(entryLink);
            var match = Regex.Match(html,
                "<div class=\"text\">([0-9]+) ⤵</div>",
                RegexOptions.IgnoreCase);
            var retlist = new List<object>();
            var lastChapterNumber = int.Parse(match.Groups[1].Value);
            setPages(0, lastChapterNumber);
            for (var i = 1; i <= lastChapterNumber; i++) {
                var htmlpage = await parser.ParseAsync(await GetHtmlStringAsync($"{blankurl}{i}"));
                var img = htmlpage.All.First(im => im.LocalName == "img" && im.ClassList.Contains("open") &&
                                                   im.HasAttribute("src")
                                                   && im.GetAttribute("src")
                                                       .Contains("https://cdn.hentai.cafe/manga/content/comics/"))
                    .GetAttribute("src");
                retlist.Add(img);
                setPages(i, lastChapterNumber);
            }

            return (retlist, lastChapterNumber);
        }
    }
}