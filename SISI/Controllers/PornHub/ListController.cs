﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Lampac.Engine.CORE;
using Lampac.Models.SISI;
using Microsoft.Extensions.Caching.Memory;
using System;
using Shared.Engine.SISI;
using Shared.Engine.CORE;
using SISI;

namespace Lampac.Controllers.PornHub
{
    public class ListController : BaseSisiController
    {
        #region httpheaders
        public static List<(string name, string val)> httpheaders(string cookie = null)
        {
            return new List<(string name, string val)>()
            {
                ("accept-language", "ru-RU,ru;q=0.9"),
                ("sec-ch-ua", "\"Chromium\";v=\"94\", \"Google Chrome\";v=\"94\", \";Not A Brand\";v=\"99\""),
                ("sec-ch-ua-mobile", "?0"),
                ("sec-ch-ua-platform", "\"Windows\""),
                ("sec-fetch-dest", "document"),
                ("sec-fetch-mode", "navigate"),
                ("sec-fetch-site", "none"),
                ("sec-fetch-user", "?1"),
                ("upgrade-insecure-requests", "1"),
                ("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/94.0.4606.81 Safari/537.36"),
                ("cookie", cookie ?? "platform=pc; bs=ukbqk2g03joiqzu68gitadhx5bhkm48j; ss=250837987735652383; fg_0d2ec4cbd943df07ec161982a603817e=56239.100000; atatusScript=hide; _gid=GA1.2.309162272.1686472069; d_fs=1; d_uidb=2f5e522a-fa28-a0fe-0ab2-fd90f45d96c0; d_uid=2f5e522a-fa28-a0fe-0ab2-fd90f45d96c0; d_uidb=2f5e522a-fa28-a0fe-0ab2-fd90f45d96c0; accessAgeDisclaimerPH=1; cookiesBannerSeen=1; _gat=1; __s=64858645-42FE722901BBA6E6-125476E1; __l=64858645-42FE722901BBA6E6-125476E1; hasVisited=1; fg_f916a4d27adf4fc066cd2d778b4d388e=78731.100000; fg_fa3f0973fd973fca3dfabc86790b408b=12606.100000; _ga_B39RFFWGYY=GS1.1.1686472069.1.1.1686472268.0.0.0; _ga=GA1.1.1515398043.1686472069"),
            };
        }
        #endregion


        [HttpGet]
        [Route("phub")]
        async public Task<JsonResult> Index(string search, string sort, int pg = 1)
        {
            if (!AppInit.conf.PornHub.enable)
                return OnError("disable");

            string memKey = $"PornHub:list:{search}:{sort}:{pg}";
            if (!memoryCache.TryGetValue(memKey, out List<PlaylistItem> playlists))
            {
                var proxyManager = new ProxyManager("phub", AppInit.conf.PornHub);
                var proxy = proxyManager.Get();

                string html = await PornHubTo.InvokeHtml(AppInit.conf.PornHub.host, search, sort, null, pg, url => HttpClient.Get(url, timeoutSeconds: 10, proxy: proxy, httpversion: 2, addHeaders: httpheaders()));
                if (html == null)
                    return OnError("html", proxyManager, string.IsNullOrEmpty(search));

                playlists = PornHubTo.Playlist($"{host}/phub/vidosik", html);

                if (playlists.Count == 0)
                    return OnError("playlists", proxyManager, string.IsNullOrEmpty(search));

                memoryCache.Set(memKey, playlists, DateTime.Now.AddMinutes(AppInit.conf.multiaccess ? 10 : 2));
            }

            return OnResult(playlists, PornHubTo.Menu(host, sort));
        }


        [HttpGet]
        [Route("pornhubpremium")]
        async public Task<JsonResult> Prem(string search, string sort, string hd, int pg = 1)
        {
            if (!AppInit.conf.PornHubPremium.enable)
                return OnError("disable");

            string memKey = $"pornhubpremium:list:{search}:{sort}:{hd}:{pg}";
            if (!memoryCache.TryGetValue(memKey, out List<PlaylistItem> playlists))
            {
                var proxyManager = new ProxyManager("pornhubpremium", AppInit.conf.PornHubPremium);
                var proxy = proxyManager.Get();

                string html = await PornHubTo.InvokeHtml(AppInit.conf.PornHubPremium.host, search, sort, hd, pg, url => HttpClient.Get(url, timeoutSeconds: 14, proxy: proxy, httpversion: 2, addHeaders: httpheaders(AppInit.conf.PornHubPremium.cookie)));
                if (html == null)
                    return OnError("html", proxyManager, string.IsNullOrEmpty(search));

                playlists = PornHubTo.Playlist($"{host}/pornhubpremium/vidosik", html, prem: true);

                if (playlists.Count == 0)
                    return OnError("playlists", proxyManager, string.IsNullOrEmpty(search));

                memoryCache.Set(memKey, playlists, DateTime.Now.AddMinutes(AppInit.conf.multiaccess ? 10 : 2));
            }

            return OnResult(playlists, PornHubTo.Menu(host, sort, hd, prem: true));
        }
    }
}