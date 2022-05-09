﻿using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using WebReaper.Domain;
using WebReaper.Extensions;
using WebReaper.LinkTracker.Abstract;
using WebReaper.Abastracts.Spider;
using WebReaper.Abstractions.Parsers;
using WebReaper.Absctracts.Sinks;
using WebReaper.Domain.Selectors;
using WebReaper.Abstractions.JobQueue;
using WebReaper.Domain.Parsing;
using WebReaper.Abstractions.Loaders.PageLoader;

namespace WebReaper.Spiders;

public class WebReaperSpider : ISpider
{
    public IPageLoader StaticPageLoader { get; init; }
    public IPageLoader SpaPageLoader { get; init; }
    public ILinkParser LinkParser { get; init; }
    public IContentParser ContentParser { get; init; }
    public ICrawledLinkTracker LinkTracker { get; init; }
    public IJobQueueReader JobQueueReader { get; init; }
    public IJobQueueWriter JobQueueWriter { get; init; }

    public List<string> UrlBlackList { get; set; } = new();

    public int PageCrawlLimit { get; set; } = int.MaxValue;

    public List<IScraperSink> Sinks { get; init; } = new();

    protected ILogger Logger { get; init; }

    public WebReaperSpider(
        List<IScraperSink> sinks,
        ILinkParser linkParser,
        IContentParser contentParser,
        ICrawledLinkTracker linkTracker,
        IPageLoader staticPageLoader,
        IPageLoader spaPageLoader,
        IJobQueueReader jobQueueReader,
        IJobQueueWriter jobQueueWriter,
        ILogger logger)
    {
        Sinks = sinks;
        LinkParser = linkParser;
        ContentParser = contentParser;
        LinkTracker = linkTracker;
        StaticPageLoader = staticPageLoader;
        SpaPageLoader = spaPageLoader;
        JobQueueReader = jobQueueReader;
        JobQueueWriter = jobQueueWriter;

        Logger = logger;
    }

    public async Task CrawlAsync(Job job)
    {
        if (UrlBlackList.Contains(job.Url)) return;

        if ((await LinkTracker.GetVisitedLinksAsync(job.BaseUrl)).Count() >= PageCrawlLimit)
        {
            await JobQueueWriter.CompleteAddingAsync();
            return;
        }

        await LinkTracker.AddVisitedLinkAsync(job.BaseUrl, job.Url);

        string doc;

        if (job.pageType == PageType.Static) {
            doc = await StaticPageLoader.Load(job.Url);
        } else {
            doc = await SpaPageLoader.Load(job.Url);
        }

        if (job.PageCategory == PageCategory.TargetPage)
        {
            Logger.LogInvocationCount("Handle on target page");
            var result = ContentParser.Parse(doc, job.schema);

            var sinkTasks = Sinks.Select(sink => sink.EmitAsync(result));

            await Task.WhenAll(sinkTasks);
            return;
        }

        var newLinkPathSelectors = job.LinkPathSelectors.Dequeue(out var currentSelector);

        var links = LinkParser.GetLinks(doc, currentSelector.Selector)
            .Select(link => job.BaseUrl + link)
            .Except(await LinkTracker.GetVisitedLinksAsync(job.BaseUrl));

        await AddToQueueAsync(job.schema, job.BaseUrl, newLinkPathSelectors, links, job.DepthLevel + 1);

        if (job.PageCategory == PageCategory.PageWithPagination)
        {
            ArgumentNullException.ThrowIfNull(currentSelector.PaginationSelector);

            var linksToPaginatedPages = LinkParser.GetLinks(doc, currentSelector.PaginationSelector)
                .Select(link => job.BaseUrl + link)
                .Except(await LinkTracker.GetVisitedLinksAsync(job.BaseUrl));

            if (!linksToPaginatedPages.Any())
            {
                Logger.LogInformation("No pages with pagination found with selector {selector} on {url}", currentSelector.PaginationSelector, job.Url);
            }

            await AddToQueueAsync(job.schema, job.BaseUrl, job.LinkPathSelectors, linksToPaginatedPages, job.DepthLevel + 1);
        }
    }

    private async Task AddToQueueAsync(
        Schema schema,
        string baseUrl,
        ImmutableQueue<LinkPathSelector> selectors,
        IEnumerable<string> links,
        int depthLevel)
    {
        foreach (var link in links)
        {
            var newJob = new Job(schema, baseUrl, link, selectors, depthLevel);
            await JobQueueWriter.WriteAsync(newJob);
        }
    }
}