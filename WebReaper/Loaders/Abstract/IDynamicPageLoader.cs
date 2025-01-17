namespace WebReaper.Loaders.Abstract;

public interface IDynamicPageLoader
{
    Task<string> Load(string url, string? script);
}
