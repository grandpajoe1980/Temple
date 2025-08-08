namespace Temple.Application.Automation;

public interface IDailyContentRotationJob
{
    Task RunAsync(CancellationToken ct = default);
}
