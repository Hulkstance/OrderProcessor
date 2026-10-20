using Hangfire.Server;

namespace BuildingBlocks.Jobs;

public interface IJob<in TJobOptions> where TJobOptions : IJobOptions
{
    Task Perform(TJobOptions jobOptions, PerformContext? performContext);
}