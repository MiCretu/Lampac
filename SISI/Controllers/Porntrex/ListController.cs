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

namespace Lampac.Controllers.Porntrex
{
    public class ListController : BaseSisiController
    {
        [HttpGet]
        [Route("ptx")]
        async public Task<JsonResult> Index(string search, string sort, int pg = 1)
        {
            if (!AppInit.conf.Porntrex.enable)
                return OnError("disable");

            string memKey = $"ptx:{search}:{sort}:{pg}";
            if (!memoryCache.TryGetValue(memKey, out List<PlaylistItem> playlists))
            {
                var proxyManager = new ProxyManager("ptx", AppInit.conf.Porntrex);
                var proxy = proxyManager.Get();

                string html = await PorntrexTo.InvokeHtml(AppInit.conf.Porntrex.host, search, sort, pg, url => HttpClient.Get(url, timeoutSeconds: 10, proxy: proxy));
                if (html == null)
                    return OnError("html", proxyManager, string.IsNullOrEmpty(search));

                playlists = PorntrexTo.Playlist($"{host}/ptx/vidosik", html);

                if (playlists.Count == 0)
                    return OnError("playlists", proxyManager, string.IsNullOrEmpty(search));

                memoryCache.Set(memKey, playlists, DateTime.Now.AddMinutes(AppInit.conf.multiaccess ? 10 : 2));
            }

            return OnResult(playlists, PorntrexTo.Menu(host, sort), headers: new List<(string name, string val)> { ("referer", AppInit.conf.Porntrex.host) });
        }
    }
}