﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using HentaiViewer.Common;
using HentaiViewer.Models;
using RestSharp;

namespace HentaiViewer.Sites {
    public static class ExHentai {
        public static async Task<List<HentaiModel>> GetLatestAsync(string url) {
            var client = new RestClient {
                UserAgent =
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.2785.70 Safari/537.36",
                Encoding = Encoding.UTF8,
                Timeout = 60000,
                BaseUrl = new Uri(url),
                CookieContainer = GetCookies()
            };

            var response = await GetHtmlStringAsync(client);
            var document = await ParseHtmlStringAsync(response);

            var hents = new List<HentaiModel>();

            var classes = document.All.Where(c => c.LocalName == "div" && c.ClassList.Contains("id1"));
            //var a = classes.Where(x => x.OuterHtml == "img");
            foreach (var element in classes) {
                var id3 = element.Children.First(d => d.LocalName == "div" && d.ClassList.Contains("id3"));
                var atag = await ParseHtmlStringAsync(id3.InnerHtml);

                client.BaseUrl = new Uri(atag.Images[0].Source);
                var imgfromb = await client.ExecuteGetTaskAsync(new RestRequest());

                var image = BytesToBitmapImage(imgfromb.RawBytes);
                hents.Add(new HentaiModel {
                    Title = atag.Images[0].Title,
                    Link = atag.Links[0].GetAttribute("href"),
                    Img = image,
                    ThumbnailLink = atag.Images[0].Source,
                    Site = "ExHentai.org",
                    Seen = HistoryController.CheckHistory(atag.Links[0].GetAttribute("href"))
                });
                //await Task.Delay(50);
            }
            return hents;
        }

        private static async Task<IHtmlDocument> ParseHtmlStringAsync(string html) {
            //We require a custom configuration
            //var config = Configuration.Default.WithJavaScript();
            //Let's create a new parser using this configuration
            var parser = new HtmlParser();
            //Just get the DOM representation
            return await parser.ParseAsync(html);
        }

        private static async Task<string> GetHtmlStringAsync(RestClient client) {
            var request = new RestRequest();
            var response = await client.ExecuteGetTaskAsync(request);
            await Task.Delay(100);
            return response.Content;
        }

        public static BitmapImage BytesToBitmapImage(byte[] bytes) {
            if (bytes == null) {
                return null;
            }
            using (var mem = new MemoryStream(bytes, 0, bytes.Length)) {
                mem.Position = 0;
                var image = new BitmapImage();
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
                image.Freeze();
                return image;
            }
        }

        public static CookieContainer GetCookies() {
            var cookieJar = new CookieContainer();
            cookieJar.Add(new Cookie {
                Name = "ipb_member_id",
                Value = SettingsController.Settings.ExHentai.IpbMemberId,
                Domain = "exhentai.org"
            });
            cookieJar.Add(new Cookie {
                Name = "ipb_pass_hash",
                Value = SettingsController.Settings.ExHentai.IpbPassHash,
                Domain = "exhentai.org"
            });
            cookieJar.Add(new Cookie {
                Name = "igneous",
                Value = SettingsController.Settings.ExHentai.Igneous,
                Domain = "exhentai.org"
            });
            cookieJar.Add(new Cookie {
                Name = "uconfig",
                Value = "dm_t",
                Domain = "exhentai.org"
            });
            return cookieJar;
        }

        public static async Task<(List<object>, int)> CollectImagesTaskAsync(HentaiModel hentai,
            Action<int, int> setPages) {
            if (Directory.Exists(hentai.SavePath) && !hentai.IsSavedGallery) {
                var files =
                    new DirectoryInfo(hentai.SavePath).GetFileSystemInfos("*.???")
                        .OrderBy(fs => int.Parse(fs.Name.Split('.')[0]));
                var paths = new List<object>();
                files.ToList().ForEach(p => paths.Add(p.FullName));
                return (new List<object>(paths), files.Count());
            }
            var url = hentai.Link;
            var client = new RestClient {
                UserAgent =
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.2785.70 Safari/537.36",
                Encoding = Encoding.UTF8,
                Timeout = 60000,
                BaseUrl = new Uri(url),
                CookieContainer = GetCookies()
            };
            if (hentai.Link.Contains("g.e-hentai.org")) {
                client.CookieContainer.Add(new Cookie("nw", "1", "/", "g.e-hentai.org"));
            }
            var document = await ParseHtmlStringAsync(await GetHtmlStringAsync(client));
            if (hentai.Title == "lul") {
                hentai.Title = document.Title.Replace(" - ExHentai.org", string.Empty);
            }
            var urlsplit = url.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            var galleryid = urlsplit[urlsplit.Length - 2];
            var ptag = document.All.Where(p => p.LocalName == "p" && p.ClassList.Contains("gpc"));
            var showingimages = ptag.First().TextContent.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            var pn = showingimages[showingimages.Length - 2].Trim().Replace(",", string.Empty);
            var pages = int.Parse(pn, CultureInfo.InvariantCulture);
            setPages(0, pages);
            var imgpagestd =
                document.All.Where(
                    t => t.LocalName == "a" && t.HasAttribute("href") && t.GetAttribute("href").Contains("?p="));
            var impagelink = new List<string> {url};
            foreach (var element in imgpagestd) {
                var link = element.GetAttribute("href");
                if (!impagelink.Contains(link)) {
                    impagelink.Add(link);
                }
            }

            var imgagelinkpages = new List<string>();
            foreach (var imgpage in impagelink) {
                client.BaseUrl = new Uri(imgpage);
                var html = await ParseHtmlStringAsync(await GetHtmlStringAsync(client));
                var atags =
                    html.All.Where(
                        t =>
                            t.LocalName == "a" && t.HasAttribute("href") &&
                            t.GetAttribute("href").Contains($"{galleryid}-"));
                foreach (var element in atags) {
                    if (!imgagelinkpages.Contains(element.GetAttribute("href"))) {
                        imgagelinkpages.Add(element.GetAttribute("href"));
                    }
                }

                //https://exhentai.org/s/8cbbff2c8a/1069378-1
                imgagelinkpages = new List<string>(imgagelinkpages.OrderBy(x => int.Parse(x.Split('-').Last())));
            }
            var images = new List<object>();
            for (var index = 0; index < imgagelinkpages.Count; index++) {
                var imgagelinkpage = imgagelinkpages[index];
                client.BaseUrl = new Uri(imgagelinkpage);
                var html = await ParseHtmlStringAsync(await GetHtmlStringAsync(client));
                var atags =
                    html.All.Where(
                        t => t.LocalName == "img" && t.HasAttribute("id") && t.Id.Contains("img"));
                if (atags.Count(c => c.HasAttribute("id")) == 0) {
                    atags = html.All.Where(
                        t => t.LocalName == "img" && t.HasAttribute("src") &&
                             t.GetAttribute("src").Contains("keystamp"));
                }
                var imglist = atags.Select(element => element.GetAttribute("src"));
                images.AddRange(imglist);
                setPages(index, pages);
            }
            return (images, pages);
        }
    }

    //mouseover image + name
    //<img src="https://exhentai.org/t/76/45/764554030806d19eacfc5221f21af64bc4a831fa-871301-1059-1500-png_l.jpg"
    //alt="[Inochi Wazuka] Kimi to Dorei Seikatsu (Otokonoko HEAVEN Vol. 25) [English] [mysterymeat3]" style="margin:0">

    //link + name 
    //<a href="https://exhentai.org/g/1003394/6d52edc70e/" onmouseover="show_image_pane(1003394)" 
    //onmouseout="hide_image_pane(1003394)">[Inochi Wazuka] Kimi to Dorei Seikatsu (Otokonoko HEAVEN Vol. 25) [English] [mysterymeat3]</a>
}