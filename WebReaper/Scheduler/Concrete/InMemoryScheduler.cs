using System.Threading.Channels;
using WebReaper.Domain;
using WebReaper.Scheduler.Abstract;

namespace WebReaper.Scheduler.Concrete;

public class InMemoryScheduler : IScheduler
{
    protected readonly Channel<Job> JobChannel = Channel.CreateUnbounded<Job>();

    public async ValueTask<Job> GetAsync(CancellationToken cancellationToken)
    {
        return await JobChannel.Reader.ReadAsync(cancellationToken);
    }

    public IAsyncEnumerable<Job> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return JobChannel.Reader.ReadAllAsync(cancellationToken);
    }

    public async ValueTask AddAsync(Job job, CancellationToken cancellationToken = default)
    {
        await JobChannel.Writer.WriteAsync(job, cancellationToken);
    }

    public async ValueTask AddAsync(IEnumerable<Job> jobs, CancellationToken cancellationToken = default)
    {
        foreach (var job in jobs)
        {
            await JobChannel.Writer.WriteAsync(job, cancellationToken);
        }
    }
}