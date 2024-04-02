// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Transactions;




List<Task> tasks = new List<Task>();
Queue<int> queue  = new Queue<int>();
int finished =0, target=0;
int cnt =1;


using (var locker = new CasLock())
//using (var locker = new SemaphoreLock())
//using (var locker = new SpinLock())
//using (var locker = new SemaphoreSlimLock())
//using (var locker = new MonitorLock())  
{
    var stopwatch = Stopwatch.StartNew();
    
    tasks.Add(Task.Factory.StartNew(()=> Writer(locker,1000)));
    tasks.Add(Task.Factory.StartNew(()=> Writer(locker,2000)));
    tasks.Add(Task.Factory.StartNew(()=> Writer(locker,3000)));
    tasks.Add(Task.Factory.StartNew(()=> Writer(locker,4000)));
    tasks.Add(Task.Factory.StartNew(()=> Writer(locker,5000)));
    
    
    tasks.Add(Task.Factory.StartNew(()=> Reader(locker)));

    await Task.WhenAll(tasks.ToArray());
    
    Console.WriteLine("=====");
    Console.WriteLine(stopwatch.ElapsedMilliseconds);
}



void Writer(ILocker locker, int toAdd)
{
    target++;

    for (int i = 0; i < 1000; i++)
    {
        cnt = (cnt + 1) % 10;
        locker.Enter();
        queue.Enqueue(111*cnt+toAdd);
        locker.Release();
    }

    finished++;
}


void Reader(ILocker locker)
{
    while (finished<target || queue.Count>0)
    {
        try
        {
            locker.Enter();
            if(!queue.TryDequeue(out var value)) continue;
            Console.WriteLine(value);
        }
        finally
        {
            locker.Release();    
        }
    }
}


interface ILocker : IDisposable
{
    void Enter();
    void Release();
}


public class MonitorLock : ILocker
{

    private object _o = new ();
    
    public void Dispose()
    {
    }

    public void Enter()
    {
        Monitor.Enter(_o);
    }

    public void Release()
    {
        Monitor.Exit(_o);
    }
}


public class SemaphoreSlimLock : ILocker
{
    private SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(initialCount: 1, maxCount: 1);
    
    
    public void Dispose()
    {
        _semaphoreSlim.Dispose();
        _semaphoreSlim = null;
    }

    public void Enter()
    {
        _semaphoreSlim.Wait();
    }

    public void Release()
    {
        _semaphoreSlim.Release();
    }
}



public class SpinLock : ILocker
{

    private volatile int _sync = 0;
    private SpinWait _spin;
    
    public void Dispose()
    {
       
    }

    bool TryEnter()
    {
        return Interlocked.CompareExchange(ref _sync, 1, 0) == 0;
    }

    public void Enter()
    {
        while (true)
        {
            if(TryEnter()) return;
            _spin.SpinOnce();
        }
    }

    public void Release()
    {
        _sync = 0;
    }
}


public class SemaphoreLock : ILocker
{
    private Semaphore _semaphore = new Semaphore(initialCount: 1, maximumCount: 1);
    public bool TryEnter() => _semaphore.WaitOne(0);
    public void Enter()
    {
        _semaphore.WaitOne();
    }
    public void Release()
    {
        _semaphore.Release();
    }
    public void Dispose()
    {
        _semaphore.Dispose();
        _semaphore = null;
    }
}

public class CasLock : ILocker
{
    private int _sync = 0;
    
    public bool TryEnter() => Interlocked.CompareExchange(ref _sync, 1, 0) == 0;
    
    public void Dispose()
    {
    }

    public void Enter()
    {
        while (true)
        {
            if(TryEnter()) return;
        }
    }

    public void Release()
    {
        _sync = 0;
    }
}

