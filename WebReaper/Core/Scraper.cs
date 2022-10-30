﻿using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using WebReaper.Sinks.Concrete;
using WebReaper.Domain.Selectors;
using WebReaper.Domain.Parsing;
using WebReaper.LinkTracker.Abstract;
using WebReaper.Sinks.Abstract;
using WebReaper.Scheduler.Abstract;
using WebReaper.Scheduler.Concrete;
using WebReaper.Proxy.Abstract;

namespace WebReaper.Core;

public class Scraper
{
    private ScraperConfigBuilder ConfigBuilder { get; } = new();
    private SpiderBuilder SpiderBuilder { get; } = new();

    private ILogger Logger { get; set; } = NullLogger.Instance;

    private IScheduler Scheduler { get; set; } = new InMemoryScheduler();

    private string SiteId { get; }

    protected IProxyProvider ProxyProvider { get; set; }

    public Scraper(string siteId)
    {
        SiteId = siteId;
    }

    public Scraper AddSink(IScraperSink sink)
    {
        SpiderBuilder.AddSink(sink);
        return this;
    }

    public Scraper Authorize(Func<CookieContainer> authorize)
    {
        SpiderBuilder.Authorize(authorize);
        return this;
    }

    public Scraper IgnoreUrls(params string[] urls)
    {
        SpiderBuilder.IgnoreUrls(urls);
        return this;
    }

    public Scraper Limit(int limit)
    {
        SpiderBuilder.Limit(limit);
        return this;
    }

    public Scraper WithLinkTracker(ICrawledLinkTracker linkTracker)
    {
        SpiderBuilder.WithLinkTracker(linkTracker);
        return this;
    }

    public Scraper WithLogger(ILogger logger)
    {
        SpiderBuilder.WithLogger(logger);

        Logger = logger;

        return this;
    }

    public Scraper WriteToConsole()
    {
        SpiderBuilder.WriteToConsole();
        return this;
    }

    public Scraper Subscribe(Action<JObject> scrapingResultHandler)
    {
        SpiderBuilder.AddSubscription(scrapingResultHandler);
        return this;
    }

    public Scraper WriteToCosmosDb(
        string endpointUrl,
        string authorizationKey,
        string databaseId,
        string containerId)
    {
        SpiderBuilder.AddSink(new CosmosSink(endpointUrl, authorizationKey, databaseId, containerId, Logger));
        return this;
    }

    public Scraper WriteToMongoDb(string connectionString, string databaseName, string collectionName)
    {
        SpiderBuilder.AddSink(new MongoDbSink(connectionString, databaseName, collectionName, Logger));
        return this;
    }

    public Scraper WriteToCsvFile(string filePath)
    {
        SpiderBuilder.AddSink(new CsvFileSink(filePath));
        return this;
    }

    public Scraper WriteToJsonFile(string filePath)
    {
        SpiderBuilder.AddSink(new JsonLinesFileSink(filePath));
        return this;
    }

    public Scraper WithProxies(IProxyProvider proxyProvider)
    {
        SpiderBuilder.WithProxies(proxyProvider);
        return this;
    }

    public Scraper Parse(Schema schema)
    {
        ConfigBuilder.WithScheme(schema);
        return this;
    }

    public Scraper WithStartUrl(string url, PageType pageType = PageType.Static, string? initScript = null)
    {
        ConfigBuilder.WithStartUrl(url, pageType, initScript);
        return this;
    }

    public Scraper FollowLinks(
        string linkSelector,
        PageType pageType = PageType.Static,
        string? script = null)
    {
        ConfigBuilder.FollowLinks(linkSelector, pageType, script);
        return this;
    }

    public Scraper FollowLinks(
        string linkSelector,
        string paginationSelector,
        PageType pageType = PageType.Static,
        string? script = null)
    {
        ConfigBuilder.FollowLinks(linkSelector, paginationSelector, pageType, script);
        return this;
    }

    public Scraper WithScheduler(IScheduler scheduler)
    {
        Scheduler = scheduler;
        return this;
    }

    public ScrapingEngine Build()
    {
        var config = ConfigBuilder.Build();
        var spider = SpiderBuilder.Build();

        return new ScrapingEngine(SiteId, config, Scheduler, spider, Logger);
    }
}
