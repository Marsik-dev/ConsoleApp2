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

        return MyValueTaskSourse.Rent(n, m).StartOperationAsync();
        
    }

    public class MyValueTaskSourse : IValueTaskSource<int>, IDisposable
    {

        private static readonly ObjectPool<MyValueTaskSourse> _pool = new DefaultObjectPool<MyValueTaskSourse>(new DefaultPooledObjectPolicy<MyValueTaskSourse>());
        private ManualResetValueTaskSourceCore<int> _core;

        private int _result;

        public static MyValueTaskSourse Rent(int n, int m)
        {
            var objPool = _pool.Get();
            objPool.m = m;
            objPool.n = n;
            objPool._core.Reset(); // Сбрасываем состояние перед повторным использованием
            return objPool;
        }

        int n, m;

        int AckermannFunc(int m, int n) => (m, n) switch
        {
            (0, _) => n + 1,
            (_, 0) => AckermannFunc(m - 1, 1),
            _ => AckermannFunc(m - 1, AckermannFunc(m, n - 1))
        };

        public async ValueTask<int> StartOperationAsync()
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {

                _result = AckermannFunc(n, m);
                Complete();

            });
            return await new ValueTask<int>(this, _core.Version);
        }
        public void Complete()
        {
            _core.SetResult(_result); // Завершаем операцию и устанавливаем результат
        }
        public int GetResult(short token)
        {
            return _core.GetResult(token); // Получаем результат операции
        }
        public ValueTaskSourceStatus GetStatus(short token)
        {
            return _core.GetStatus(token); // Получаем статус операции
        }
        public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            _core.OnCompleted(continuation, state, token, flags); // Регистрируем продолжение
        }
        public void Dispose()
        {
            // Возвращаем объект в пул после завершения
            _pool.Return(this);
        }

    }

}