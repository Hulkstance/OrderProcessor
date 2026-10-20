using Hangfire;

namespace BuildingBlocks.Jobs.Extensions;

public static class BatchRunnerExtensions
{
    public static void Batch<TJob, TJobOptions>(List<TJobOptions> options)
        where TJob : IJob<TJobOptions>
        where TJobOptions : IJobOptions
    {
        BatchJob.StartNew(b =>
        {
            foreach (var option in options)
            {
                b.Enqueue<TJob>(job => job.Perform(option, null));
            }
        });
    }

    public static void Batch<TJob, TJobOptions>(TJobOptions options)
        where TJob : IJob<TJobOptions>
        where TJobOptions : IJobOptions
    {
        BatchJob.StartNew(b =>
        {
            b.Enqueue<TJob>(job => job.Perform(options, null));
        });
    }

    public static void Batch<TJob, TJobOptions>(TJobOptions options, string description)
        where TJob : IJob<TJobOptions>
        where TJobOptions : IJobOptions
    {
        BatchJob.StartNew(b =>
        {
            b.Enqueue<TJob>(job => job.Perform(options, null));
        }, description);
    }
}