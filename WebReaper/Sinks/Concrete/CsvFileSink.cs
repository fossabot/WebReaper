using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;
using WebReaper.Sinks.Abstract;

namespace WebReaper.Sinks.Concrete
{
    public class CsvFileSink : IScraperSink
    {
        private object _lock = new();

        private readonly string filePath;

        BlockingCollection<JObject> entries = new();

        protected bool IsInitialized { get; set; } = false;

        public CsvFileSink(string filePath)
        {
            this.filePath = filePath;
        }

        public async Task EmitAsync(JObject scrapedData, CancellationToken cancellationToken = default)
        {
            await Init(scrapedData, cancellationToken);

            entries.Add(scrapedData, cancellationToken);
        }

        private async Task Init(JObject scrapedData, CancellationToken cancellationToken)
        {
            if (!IsInitialized)
            {

                lock (_lock)
                {
                    File.Delete(filePath);
                }

                var flattened = scrapedData
                        .Descendants()
                        .OfType<JValue>()
                        .Select(jv => jv.Path.Remove(0, jv.Path.LastIndexOf(".") + 1));

                var header = string.Join(",", flattened) + Environment.NewLine;

                await File.AppendAllTextAsync(filePath, header, cancellationToken);

                IsInitialized = true;

                _ = Task.Run(async () => await Handle(cancellationToken));
            }
        }

        protected async Task Handle(CancellationToken cancellationToken = default)
        {
            foreach (var entry in entries.GetConsumingEnumerable(cancellationToken))
            {
                var flattened = entry
                    .Descendants()
                    .OfType<JValue>()
                    .Select(p => $"\"{p.Value?.ToString()?.Replace("\"", "\"\"")}\"");

                var csvLine = string.Join(",", flattened);

                await File.AppendAllTextAsync(filePath, $"{csvLine}{Environment.NewLine}", cancellationToken);
            }
        }
    }
}