using System.Net;
using WebReaper.Absctracts.Sinks;
using WebReaper.Domain.Schema;
using WebReaper.Domain.Selectors;

namespace WebReaper.Abstractions.Scraper;

public interface IScraper
{
    IScraper WithStartUrl(string startUrl);
    IScraper FollowLinks(string linkSelector, SelectorType selectorType = SelectorType.Css, PageType pageType = PageType.Static);
    IScraper WriteToJsonFile(string filePath);
    IScraper WriteToCsvFile(string filePath);
    IScraper AddSink(IScraperSink sink);
    IScraper Limit(int limit);
    IScraper IgnoreUrls(params string[] urls);
    IScraper Paginate(string paginationSelector);
    IScraper Build();
    Task Run();
    IScraper WithProxy(WebProxy proxy);
    IScraper WithProxy(WebProxy[] proxies);
    IScraper WithPuppeter(WebProxy[] proxies);
    IScraper WithScheme(SchemaElement[] schema);
    IScraper WithParallelismDegree(int parallelismDegree);
    IScraper Authorize(Func<CookieContainer> authorize);
}