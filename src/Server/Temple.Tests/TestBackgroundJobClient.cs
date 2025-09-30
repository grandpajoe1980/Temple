using Hangfire;
using Hangfire.Common;
using Hangfire.States;

namespace Temple.Tests;

/// <summary>
/// Test implementation of IBackgroundJobClient that does nothing.
/// Used for integration tests when Hangfire is not available.
/// </summary>
public class TestBackgroundJobClient : IBackgroundJobClient
{
    public string Create(Job job, IState state)
    {
        // Return a fake job ID for tests
        return Guid.NewGuid().ToString();
    }

    public bool ChangeState(string jobId, IState state, string expectedState)
    {
        return true;
    }
}
