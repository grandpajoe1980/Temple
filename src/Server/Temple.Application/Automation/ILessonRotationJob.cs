using System.Threading;
using System.Threading.Tasks;

namespace Temple.Application.Automation;

public interface ILessonRotationJob
{
    Task RunAsync(CancellationToken ct = default);
}
