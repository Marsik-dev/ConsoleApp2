using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.ObjectPool;
using System.Threading.Tasks.Sources;
using static Ackermann;

public class Programm
{
    public static async Task Main()
    {
        BenchmarkRunner.Run<Ackermann>();

    }

}

[IterationCount(100)]
[MemoryDiagnoser]
public class Ackermann
{
    // AckermannFunc(3,3) = 61, общее количество рекурсивных вызовов = 2432
    [Params(1, 2, 3)]
    public int n;
    [Params(1, 2, 3)]
    public int m;
    [Benchmark(Baseline = true)]
    public int Baseline()
    {
        return AckermannFunc(m, n);
        int AckermannFunc(int m, int n) => (m, n) switch
        {
            (0, _) => n + 1,
            (_, 0) => AckermannFunc(m - 1, 1),
            _ => AckermannFunc(m - 1, AckermannFunc(m, n - 1)),
        };
    }
    //[Benchmark]
    public ValueTask<int> ValueTask()
    {
        return AckermannFunc(m, n);
        async ValueTask<int> AckermannFunc(int m, int n) => (m, n) switch
        {
            (0, _) => n + 1,
            (_, 0) => await AckermannFunc(m - 1, 1),
            _ => await AckermannFunc(m - 1, await AckermannFunc(m, n - 1)),
        };
    }
    //[Benchmark]
    public Task<int> Task()
    {
        return AckermannFunc(m, n);
        async Task<int> AckermannFunc(int m, int n) => (m, n) switch
        {
            (0, _) => n + 1,
            (_, 0) => await AckermannFunc(m - 1, 1),
            _ => await AckermannFunc(m - 1, await AckermannFunc(m, n - 1)),
        };
    }
    [Benchmark]
    public ValueTask<int> IValueTaskSource()
    {

        return new AckermannFunction().Func(m, n);
        
    }

public class AckermannFunction : IValueTaskSource<int>
{
    private int _result;
    private ValueTaskSourceStatus _status;
    private ManualResetValueTaskSourceCore<int> _core;

    public ValueTask<int> Func(int m, int n)
    {
        _status = ValueTaskSourceStatus.Pending;
        _core.Reset();

        _result = ComputeAckermann(m, n);
        _status = ValueTaskSourceStatus.Succeeded;

        return new ValueTask<int>(this, _core.Version);
    }

    private int ComputeAckermann(int m, int n)
    {
        // Реализация функции Аккермана
        if (m == 0)
        {
            return n + 1;
        }
        else if (m > 0 && n == 0)
        {
            return ComputeAckermann(m - 1, 1);
        }
        else
        {
            return ComputeAckermann(m - 1, ComputeAckermann(m, n - 1));
        }
    }

    public int GetResult(short token) => _result;

    public ValueTaskSourceStatus GetStatus(short token) => _status;

    public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
    {
        _core.OnCompleted(continuation, state, token, flags);
    }
}
}