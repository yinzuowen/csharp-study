# 第 14 章 线程同步 🟡

> 本章练习：
> - 锁：[11-race-condition](../advanced/11-race-condition.cs)、[11-lock](../advanced/11-lock.cs)、[11-monitor](../advanced/11-monitor.cs)、[11-monitor-try-enter](../advanced/11-monitor-try-enter.cs)、[11-method-impl-synchronized](../advanced/11-method-impl-synchronized.cs)、[11-interlocked](../advanced/11-interlocked.cs)、[11-volatile](../advanced/11-volatile.cs)、[11-spin-lock](../advanced/11-spin-lock.cs)
> - 信号：[12-auto-reset-event](../advanced/12-auto-reset-event.cs)、[12-manual-reset-event](../advanced/12-manual-reset-event.cs)、[12-event-wait-handle](../advanced/12-event-wait-handle.cs)、[12-countdown-event](../advanced/12-countdown-event.cs)、[12-barrier](../advanced/12-barrier.cs)
> - 信号量、互斥量、读写锁：[13-semaphore](../advanced/13-semaphore.cs)、[13-semaphore-odd-even](../advanced/13-semaphore-odd-even.cs)、[13-mutex](../advanced/13-mutex.cs)、[13-mutex-single-instance](../advanced/13-mutex-single-instance.cs)、[13-reader-writer-lock](../advanced/13-reader-writer-lock.cs)
> - 并发集合：[17-concurrent-dictionary](../advanced/17-concurrent-dictionary.cs)、[17-concurrent-queue](../advanced/17-concurrent-queue.cs)、[17-concurrent-stack](../advanced/17-concurrent-stack.cs)、[17-concurrent-bag](../advanced/17-concurrent-bag.cs)、[17-blocking-collection](../advanced/17-blocking-collection.cs)
> - 线程安全的单例：[14-singleton-double-check-lock](../advanced/14-singleton-double-check-lock.cs)、[14-singleton-lazy](../advanced/14-singleton-lazy.cs)

> 多个线程同时读写同一份数据，结果就可能出错。写代码时你会遇到这些问题：
> 余额明明有判断，却被扣成了负数；`count++` 跑了 9999 次结果只有 7901；程序 Debug 运行好好的，Release 版本却卡死在一个循环里；在 `lock` 里写 `await` 编译不过；多线程往 `Dictionary` 里写数据直接抛异常；两个线程互相等对方的锁，程序再也不动了。
> 本章讲：竞态条件是怎么发生的；`lock` / `Monitor`、`Interlocked`、`volatile`、`SpinLock`、`SemaphoreSlim`、`ReaderWriterLockSlim`、`Mutex` 各自解决什么问题；并发集合和 `Channel<T>`；死锁；最后给一张选择速查表，并和 Java 对照。
>
> 本章示例都在 .NET 11 上实际跑过，输出是真实结果（本机 CPU 8 核）。多线程的输出顺序每次运行可能不同。

## 1. 竞态条件：余额变成 -100 🟢

4 个任务同时从余额 100 的账户里取 20、40、60、80（[11-race-condition.cs](../advanced/11-race-condition.cs)）：

```csharp
internal class Person
{
    public int Balance { get; set; }
}

static Person person = new Person { Balance = 100 };

static async Task Main(string[] args)
{
    Task t1 = Task.Run(() => Withdraw(20));
    Task t2 = Task.Run(() => Withdraw(40));
    Task t3 = Task.Run(() => Withdraw(60));
    Task t4 = Task.Run(() => Withdraw(80));

    await Task.WhenAll(t1, t2, t3, t4);      // 不等的话 Main 立刻结束，任务来不及执行
    Console.WriteLine($"余额：{person.Balance}");
}

static void Withdraw(int money)
{
    if (person.Balance >= money)                     // ① 检查
    {
        Console.WriteLine($"成功取出 {money} 元");
        person.Balance -= money;                     // ② 扣钱
    }
}
```

连续跑了 10 次，**10 次都是 -100**（取款顺序每次不同）：

```
成功取出 60 元
成功取出 40 元
成功取出 20 元
成功取出 80 元
余额：-100
```

原因：①"检查"和 ②"扣钱"是两步，中间还有一个很慢的 `Console.WriteLine`。4 个线程几乎同时执行 ①，看到的余额都是 100，都通过了检查，然后各自扣钱：100 − 20 − 40 − 60 − 80 = −100。

这就是**竞态条件（race condition）**：结果取决于多个线程执行的先后时机。"先检查再操作"（check-then-act）和"读-改-写"（`count++`）是最常见的两种形式。

还有一个隐藏问题：`person.Balance -= money` 本身也不是原子的，它是"读 Balance → 减 → 写回 Balance"三步，两个线程同时做可能有一次扣减丢失（第 3 节 `count++` 的实验就是这个问题）。

## 2. lock 与 Monitor 🟢

### 2.1 lock：对应 Java 的 synchronized

把"检查 + 扣钱"放进 `lock`，同一时刻只有一个线程能进入（[11-lock.cs](../advanced/11-lock.cs)）：

```csharp
static readonly Lock locker = new();

static void Withdraw(int money)
{
    lock (locker)
    {
        if (person.Balance >= money)
        {
            Console.WriteLine($"成功取出 {money} 元");
            person.Balance -= money;
        }
    }
}
```

```
成功取出 20 元
成功取出 60 元
余额：20
```

（哪几笔成功取决于谁先拿到锁，每次可能不同，但余额永远不会是负数。）

| Java                         | C#                                                                                                    |
| ---------------------------- | ----------------------------------------------------------------------------------------------------- |
| `synchronized (obj) { ... }` | `lock (obj) { ... }`                                                                                  |
| `synchronized` 方法          | 没有关键字；`[MethodImpl(MethodImplOptions.Synchronized)]` 能做到但**不推荐**（锁的是 `this` 或类型） |
| 可重入                       | 可重入：同一个线程可以再次进入自己持有的锁                                                            |

`lock` 是语法糖，编译后就是 `Monitor.Enter` + `try/finally` + `Monitor.Exit`，所以锁里抛异常也一定会释放。

### 2.2 锁对象怎么选 ⚠️

| 写法                                                  | 问题                                                                                                |
| ----------------------------------------------------- | --------------------------------------------------------------------------------------------------- |
| ✅ `private readonly object locker = new();`          | 私有、专用，外面的代码锁不到它                                                                      |
| ✅ `private readonly Lock locker = new();`（.NET 9+） | 更好，见 2.3                                                                                        |
| ❌ `lock (this)`                                      | 外部代码拿到这个对象也能 `lock(obj)`，可能和你抢锁甚至死锁                                          |
| ❌ `lock (typeof(Person))`                            | `Type` 对象全进程共享，谁都能锁                                                                     |
| ❌ `lock ("myLock")`                                  | 相同内容的字符串字面量会被驻留（intern）成同一个对象，不相关的代码可能锁到同一把锁                  |
| ❌ `lock (someInt)`                                   | 值类型不能 lock，编译错误 CS0185：`'int' is not a reference type as required by the lock statement` |
| ❌ 锁对象不是 `readonly`，中途被重新赋值              | 两个线程锁的是不同对象，等于没锁                                                                    |

规则：**锁对象用私有的 `readonly` 字段，只用来当锁，别的什么都不干**。保护静态数据用 `static readonly` 锁对象，保护实例数据用实例锁对象。

### 2.3 System.Threading.Lock（.NET 9+）

.NET 9 加了专门的锁类型 `Lock`，`lock` 语句认识它，编译后走更快的专用 API，意图也更清楚：

```csharp
internal class Account
{
    private readonly Lock locker = new();      // 代替 new object()
    public int Balance { get; private set; } = 100;

    public bool Withdraw(int money)
    {
        lock (locker)
        {
            if (Balance < money) return false;
            Balance -= money;
            return true;
        }
    }
}

var account = new Account();
int[] amounts = [20, 40, 60, 80];
bool[] ok = await Task.WhenAll(amounts.Select(m => Task.Run(() => account.Withdraw(m))));
Console.WriteLine($"成功 {ok.Count(x => x)} 笔，余额 {account.Balance}");

// 也可以不用 lock 语句，直接用 EnterScope
var l = new Lock();
using (l.EnterScope())
{
    Console.WriteLine($"IsHeldByCurrentThread = {l.IsHeldByCurrentThread}");
}
```

```
成功 2 笔，余额 40
IsHeldByCurrentThread = True
```

新代码（.NET 9 及以上）锁对象优先用 `Lock`。

### 2.4 Monitor：TryEnter、Wait、Pulse

`lock` 只能"一直等到拿到锁"。需要超时、或者需要"条件不满足就等、满足了再叫醒"时，直接用 `Monitor`：

| 方法                                        | 作用                           | Java 对应                        |
| ------------------------------------------- | ------------------------------ | -------------------------------- |
| `Monitor.Enter(obj)` / `Exit(obj)`          | 加锁 / 解锁（`lock` 就是它）   | `synchronized`                   |
| `Monitor.TryEnter(obj, timeout, ref taken)` | 限时加锁，拿不到就放弃         | `ReentrantLock.tryLock(timeout)` |
| `Monitor.Wait(obj)`                         | 释放锁并等待，被唤醒后重新拿锁 | `obj.wait()`                     |
| `Monitor.Pulse(obj)` / `PulseAll(obj)`      | 唤醒一个 / 全部等待者          | `obj.notify()` / `notifyAll()`   |

```csharp
static readonly object locker = new();
static readonly Queue<int> _queue = new();

// 1. TryEnter：另一个线程占着锁 1 秒，我只等 200ms
var holder = new Thread(() => { lock (locker) { Thread.Sleep(1000); } });
holder.Start();
Thread.Sleep(100);
bool taken = false;
try
{
    Monitor.TryEnter(locker, 200, ref taken);
    Console.WriteLine($"200ms 内拿到锁了吗: {taken}");
}
finally
{
    if (taken) Monitor.Exit(locker);          // 只有拿到了才能释放
}
holder.Join();

// 2. Wait / Pulse：简单的生产者-消费者
var consumer = new Thread(() =>
{
    for (int i = 0; i < 3; i++)
    {
        lock (locker)
        {
            while (_queue.Count == 0)        // 用 while，不要用 if：被唤醒后要重新检查条件
                Monitor.Wait(locker);
            Console.WriteLine($"消费 {_queue.Dequeue()}");
        }
    }
});
consumer.Start();
for (int i = 1; i <= 3; i++)
{
    Thread.Sleep(100);
    lock (locker)
    {
        _queue.Enqueue(i);
        Console.WriteLine($"生产 {i}");
        Monitor.Pulse(locker);
    }
}
consumer.Join();
```

```
200ms 内拿到锁了吗: False
生产 1
消费 1
生产 2
消费 2
生产 3
消费 3
```

`Wait` / `Pulse` 和 Java 的 `wait` / `notify` 规则一样：必须在持有锁时调用。实际项目里很少手写，生产者-消费者直接用 `BlockingCollection` 或 `Channel<T>`（第 8 节）。

## 3. Interlocked：原子操作 🟢

`count++` 不是原子操作。9999 个任务各加 1（[11-interlocked.cs](../advanced/11-interlocked.cs)）：

```csharp
static int count, length;

for (int i = 0; i < 9999; i++)
    tasks.Add(Task.Run(() => { count++; }));
Task.WaitAll(tasks.ToArray());
Console.WriteLine($"count = {count}");

tasks.Clear();
for (int i = 0; i < 9999; i++)
    tasks.Add(Task.Run(() => { Interlocked.Increment(ref length); }));
Task.WaitAll(tasks.ToArray());
Console.WriteLine($"length = {length}");
```

```
main thread start
count = 7901
length = 9999
main thread end
```

`count` 每次运行结果不同，但几乎总是小于 9999：两个线程同时读到 100，各自加 1 写回 101，丢了一次。`Interlocked.Increment` 用一条 CPU 原子指令完成"读-加-写"，结果永远正确，而且比 `lock` 快得多。

常用方法：

| 方法                                              | 作用                                         | Java `AtomicInteger` 对应                      |
| ------------------------------------------------- | -------------------------------------------- | ---------------------------------------------- |
| `Interlocked.Increment(ref x)` / `Decrement`      | 加 1 / 减 1，返回新值                        | `incrementAndGet()`                            |
| `Interlocked.Add(ref x, n)`                       | 加 n，返回新值                               | `addAndGet(n)`                                 |
| `Interlocked.Exchange(ref x, v)`                  | 设为 v，返回旧值                             | `getAndSet(v)`                                 |
| `Interlocked.CompareExchange(ref x, v, expected)` | 如果 x == expected 就设为 v；返回 x 原来的值 | `compareAndSet(expected, v)`（Java 返回 bool） |
| `Interlocked.Read(ref long)`                      | 在 32 位系统上原子读 `long`                  | `get()`                                        |

`CompareExchange`（CAS）可以实现任何"读-算-写"的原子操作，套路是"读 → 算 → 比较并交换，失败就重试"：

```csharp
static int _count;
static long _total;
static int _max;

Parallel.For(0, 10000, i =>
{
    Interlocked.Increment(ref _count);
    Interlocked.Add(ref _total, i);
    int current;
    do
    {
        current = _max;
        if (i <= current) break;
    } while (Interlocked.CompareExchange(ref _max, i, current) != current);   // 被别人改过就重来
});
Console.WriteLine($"count={_count}, total={_total}, max={_max}");
int old = Interlocked.Exchange(ref _count, 0);
Console.WriteLine($"Exchange 返回旧值 {old}，现在 {_count}");
```

```
count=10000, total=49995000, max=9999
Exchange 返回旧值 10000，现在 0
```

- C# 没有 `AtomicInteger` 这种包装类，直接对普通的 `int`/`long` **字段**用 `ref` 调 `Interlocked`。
- ⚠️ 只能用在字段或数组元素上，**不能用在属性上**：`Interlocked.Increment(ref p.Balance)` 编译错误 CS0206 `A non ref-returning property or indexer may not be used as an out or ref value`。
- `Interlocked` 只保证**单个变量**的单个操作是原子的。"检查余额 + 扣钱"这种涉及判断的多步操作，还是要用 `lock`（或者用 CAS 循环自己写）。

## 4. volatile：只保证可见性，不保证原子性 🟡

### 4.1 实测：去掉 volatile，Release 版本死循环

[11-volatile.cs](../advanced/11-volatile.cs)：子线程不停循环，主线程 500ms 后把 `Loop` 改成 `false` 让它停下：

```csharp
internal class Person
{
    public int Balance { get; set; }
    public volatile bool Loop = true;          // ← 实验就是去掉这里的 volatile
    public void Deposit()
    {
        Console.WriteLine("loop start");
        while (Loop)
        {
            Balance += 10;
        }
        Console.WriteLine("loop end");
    }
}

static async Task Main(string[] args)
{
    Console.WriteLine("main thread start");
    Person person = new Person();
    Thread thread = new Thread(new ThreadStart(person.Deposit));   // 前台线程：它不结束，进程就不退出
    thread.Start();
    Thread.Sleep(500);
    person.Loop = false;
    Console.WriteLine("main thread end");
}
```

同一份代码去掉 `volatile`，在 Debug 和 Release 下分别运行：

```bash
dotnet run novolatile.cs                                   # Debug
dotnet build -c Release novolatile.cs -o out-novolatile    # Release，再运行编译产物
./out-novolatile/novolatile
```

|                 | Debug（`dotnet run`） | Release（编译产物）                                                    |
| --------------- | --------------------- | ---------------------------------------------------------------------- |
| 去掉 `volatile` | 正常结束              | ❌ **子线程死循环，进程永远不退出**（连跑 3 次都是，5 秒后被强制结束） |
| 加上 `volatile` | 正常结束              | 正常结束（连跑 3 次）                                                  |

去掉 `volatile` 的 Release 输出，停在这里不动：

```
main thread start
loop start
main thread end
```

加上 `volatile` 的 Release 输出：

```
main thread start
loop start
main thread end
loop end
```

原因：Release 模式下 JIT 会优化。它看到循环里没有任何代码修改 `Loop`，就把 `Loop` 只读一次，放进 CPU 寄存器，循环变成了 `while (true)`。主线程改了内存里的 `Loop`，子线程永远看不到。Debug 模式不做这种优化，所以测试时一切正常——**多线程 bug 只在 Release 出现，是这类问题最坑的地方**。

`volatile` 告诉编译器和 JIT：这个字段可能被别的线程改，每次都要从内存里重新读，不能缓存，也不能把读操作挪到循环外面。

### 4.2 volatile 不保证原子性 ⚠️

|               | 保证可见性（读到最新值） | 保证原子性（读-改-写不被打断） |
| ------------- | ------------------------ | ------------------------------ |
| `volatile`    | ✅                       | ❌                             |
| `Interlocked` | ✅                       | ✅（单个变量）                 |
| `lock`        | ✅                       | ✅（整段代码）                 |

`volatile int count; count++;` 照样会丢数据：每次读到的是最新值没错，但"读 → 加 → 写"三步之间还是会被别的线程插进来。实测：

```csharp
static volatile int count;
Parallel.For(0, 100000, _ => count++);
Console.WriteLine($"volatile count++ 10 万次 = {count}");
```

```
volatile count++ 10 万次 = 42207
```

（每次运行结果不同，但远小于 100000。）`volatile` 只适合**一个线程写、其他线程读**的标志位，比如上面的 `Loop`、"是否已停止"。

和 Java 的 `volatile` 用途一样（可见性 + 禁止重排序），细节上 Java 的 `volatile` 语义更强（顺序一致），C# 的是 acquire/release 语义：

- volatile 读：后面的读写不会被重排到它前面。
- volatile 写：前面的读写不会被重排到它后面。

### 4.3 Volatile.Read / Volatile.Write

`volatile` 关键字只能加在字段上，而且该字段的**每次**读写都变成 volatile。只想在某几处保证可见性，或者变量是局部变量、数组元素时，用 `Volatile` 类：

```csharp
bool ready = false;
var setter = Task.Run(() => { Thread.Sleep(50); Volatile.Write(ref ready, true); });
while (!Volatile.Read(ref ready)) { /* 等 */ }
```

日常代码里，与其纠结 `volatile`，不如直接用 `lock`、`Interlocked`、`CancellationToken`（停止后台循环的标准做法，第 12 章）——它们都自带内存屏障。

## 5. SpinLock 与 SpinWait ⚪

### 5.1 SpinLock

普通锁（`lock`）拿不到时，线程会进入等待状态，让出 CPU，被唤醒要经过操作系统调度，开销在微秒级。**自旋锁**拿不到时不睡，而是原地循环检查（"自旋"），一直占着 CPU。适合**锁里的代码极短**（几十纳秒）的场景。

[11-spin-lock.cs](../advanced/11-spin-lock.cs)：

```csharp
static int sum = 0;
static SpinLock spinLock = new SpinLock();     // 注意：不能是 readonly，见 5.2

for (int i = 0; i < 5; i++)
{
    var task = Task.Factory.StartNew(param =>
    {
        bool lockTaken = false;
        int number = (int)param!;
        try
        {
            spinLock.TryEnter(5000, ref lockTaken);
            if (lockTaken)
            {
                Thread.Sleep(100);
                sum += number;
                Console.WriteLine($"number = {number}");
            }
        }
        finally
        {
            if (lockTaken)
            {
                spinLock.Exit();
                Console.WriteLine("release lock");
            }
        }
    }, i);
    tasks.Add(task);
}
Task.WaitAll(tasks.ToArray());
Console.WriteLine($"sum = {sum}");
```

```
main thread start
number = 0
release lock
number = 4
release lock
number = 2
release lock
number = 1
release lock
number = 3
release lock
sum = 10
main thread end
```

⚠️ 这个练习在锁里 `Thread.Sleep(100)`，其他 4 个线程就空转 100ms 以上，白白烧 CPU——这正是**不该用自旋锁的场景**，这里只是演示 API。锁里的代码稍长一点，就用 `lock`。实际上 `lock`（`Monitor`）内部已经会先自旋一小会儿再进入等待，绝大多数情况下用不着手写 `SpinLock`。

### 5.2 SpinLock 是 struct：不能复制，不能 readonly ⚠️

`SpinLock` 是**值类型**（为了不额外分配对象）。复制一份就是两把不同的锁：

```csharp
static readonly SpinLock _readonlyLock = new();   // ❌ readonly

bool taken = false;
try
{
    _readonlyLock.Enter(ref taken);
    Console.WriteLine($"Enter 成功: {taken}, 字段 IsHeld = {_readonlyLock.IsHeld}");
}
finally
{
    if (taken)
    {
        try { _readonlyLock.Exit(); }
        catch (Exception e) { Console.WriteLine($"Exit: {e.GetType().Name}: {e.Message}"); }
    }
}

var a = new SpinLock();
var b = a;                                        // ❌ 复制
bool t1 = false, t2 = false;
a.Enter(ref t1);
b.TryEnter(ref t2);
Console.WriteLine($"a 拿到: {t1}, b 也拿到: {t2}");
```

```
Enter 成功: True, 字段 IsHeld = False
Exit: SynchronizationLockException: The calling thread does not hold the lock.
a 拿到: True, b 也拿到: True
```

- `readonly` 结构体字段：每次调用方法，编译器都会先复制一份**防御性副本**再调用，`Enter` 锁的是副本，字段本身没变（`IsHeld = False`），`Exit` 时又是一份新副本，于是抛 `SynchronizationLockException`。
- 复制出来的 `b` 和 `a` 是两把锁，两个线程能同时"拿到锁"，等于没锁。
- 规则：`SpinLock` 只声明成**非 readonly 的字段**，不要赋值给局部变量，不要作为参数按值传递。

### 5.3 SpinWait

想自己"等一个条件变成 true"，又不想写死循环烧 CPU、也不想 `Thread.Sleep` 等太久，用 `SpinWait`：前几次原地自旋，之后自动改为让出 CPU。

```csharp
bool ready = false;
var setter = Task.Run(() => { Thread.Sleep(50); Volatile.Write(ref ready, true); });
var spin = new SpinWait();
while (!Volatile.Read(ref ready)) spin.SpinOnce();
Console.WriteLine($"SpinWait 等到了，自旋 {spin.Count} 次, NextSpinWillYield = {spin.NextSpinWillYield}");
```

```
SpinWait 等到了，自旋 61 次, NextSpinWillYield = True
```

（自旋次数每次不同。）`NextSpinWillYield = True` 说明它早已从"原地自旋"切换到"让出 CPU"了。这是写底层同步代码用的，业务代码里用 `ManualResetEventSlim` 或 `await` 即可。

## 6. SemaphoreSlim：限流，以及 async 里的锁 🟢

### 6.1 限制同时执行的数量

信号量 = 有 N 个许可证，拿到才能进，出来时还回去。`lock` 相当于 N = 1。

```csharp
static readonly SemaphoreSlim _sem = new(2);   // 最多 2 个同时进入
static int _inside;

var sw = Stopwatch.StartNew();
var tasks = Enumerable.Range(1, 6).Select(async i =>
{
    await _sem.WaitAsync();                // 异步等待，不阻塞线程
    try
    {
        int now = Interlocked.Increment(ref _inside);
        Console.WriteLine($"{sw.ElapsedMilliseconds,4}ms 任务 {i} 进入，当前 {now} 个");
        await Task.Delay(300);             // 锁里可以 await
        Interlocked.Decrement(ref _inside);
    }
    finally
    {
        _sem.Release();                    // 一定放在 finally
    }
});
await Task.WhenAll(tasks);
Console.WriteLine($"全部完成 {sw.ElapsedMilliseconds}ms");
```

```
   1ms 任务 1 进入，当前 1 个
   5ms 任务 2 进入，当前 2 个
 307ms 任务 3 进入，当前 1 个
 307ms 任务 4 进入，当前 2 个
 607ms 任务 5 进入，当前 1 个
 607ms 任务 6 进入，当前 2 个
全部完成 909ms
```

6 个任务每次只放 2 个，分 3 批，每批 300ms。典型用途：限制同时调用某个外部接口的请求数、限制同时下载的文件数。

### 6.2 async 代码里不能用 lock + await ⚠️

```csharp
lock (locker)
{
    await Task.Delay(100);
}
```

```
error CS1996: Cannot await in the body of a lock statement
```

原因：`lock` 的锁属于**线程**，`Exit` 必须由 `Enter` 的那个线程调用；而 `await` 之后的代码可能在另一个线程上继续执行，就没法正确释放了。所以编译器直接禁止。

async 代码里需要互斥，用 **`SemaphoreSlim(1, 1)`** 代替 `lock`：

```csharp
private readonly SemaphoreSlim _mutex = new(1, 1);

await _mutex.WaitAsync();
try
{
    await SaveAsync();          // 可以 await
}
finally
{
    _mutex.Release();
}
```

⚠️ `SemaphoreSlim` **不可重入**，也不记录是谁拿的：同一个任务连续 `WaitAsync` 两次会把自己卡死；`Release` 多调一次会让许可证变多。

## 7. ReaderWriterLockSlim、EventWaitHandle 与 Mutex ⚪

### 7.1 ReaderWriterLockSlim：读多写少

`lock` 不区分读写，两个线程都只是读也要排队。`ReaderWriterLockSlim`：**多个读可以同时进，写独占**。

```csharp
internal class Cache
{
    private readonly ReaderWriterLockSlim _rw = new();
    private readonly Dictionary<string, string> _data = new();

    public string? Get(string key)
    {
        _rw.EnterReadLock();                  // 多个读可以同时进
        try { Thread.Sleep(100); return _data.GetValueOrDefault(key); }
        finally { _rw.ExitReadLock(); }
    }

    public void Set(string key, string value)
    {
        _rw.EnterWriteLock();                 // 写独占
        try { _data[key] = value; }
        finally { _rw.ExitWriteLock(); }
    }
}

var cache = new Cache();
cache.Set("name", "Owen");
var sw = Stopwatch.StartNew();
Parallel.For(0, 4, _ => cache.Get("name"));
Console.WriteLine($"4 个读（各 100ms）并发执行用时约 {sw.ElapsedMilliseconds}ms");
```

```
4 个读（各 100ms）并发执行用时约 107ms
```

4 个读同时进行，总共约 100ms；换成 `lock` 就要 400ms。对应 Java 的 `ReentrantReadWriteLock`。

但它本身比 `lock` 重，只有读操作**较慢**且读远多于写时才划算。像上面这种缓存，直接用 `ConcurrentDictionary` 更简单。

### 7.2 EventWaitHandle：发信号放行

前面的锁是"互斥"；有时要的是"信号"：一批线程先等着，某个时刻统一放行。`EventWaitHandle` 就是第 11 章 `ManualResetEvent` / `AutoResetEvent` 的基类，构造时用 `EventResetMode` 选模式（[12-event-wait-handle.cs](../advanced/12-event-wait-handle.cs)）：

```csharp
static EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

for (int i = 0; i < 5; i++)
{
    var task = Task.Factory.StartNew((param) =>
    {
        waitHandle.WaitOne();                    // 灯没亮就等
        Console.WriteLine($"param = {param}");
        // AutoReset 时，每个任务执行完后再调用一次 Set()，把下一个线程放进来
        // waitHandle.Set();
    }, i);
    tasks.Add(task);
}

Task.Delay(1000).ContinueWith(t => waitHandle.Set());   // 1 秒后点亮
Task.WaitAll(tasks.ToArray());
```

`ManualReset`：1 秒后一次 `Set`，5 个任务全部放行：

```
main thread start
param = 2
param = 3
param = 4
param = 0
param = 1
main thread end
```

改成 `EventResetMode.AutoReset` 并打开任务里的 `waitHandle.Set()`：每次只放行一个，放行的任务做完再放下一个，像接力：

```
main thread start
param = 0
param = 2
param = 1
param = 3
param = 4
main thread end
```

（顺序每次可能不同。）AutoReset 模式下如果忘了在任务里接力 `Set()`，只有第一个任务能通过，其余 4 个永远等着，`Task.WaitAll` 也就永远不返回。

| 模式                         | `Set()` 之后                           | 等价类             |
| ---------------------------- | -------------------------------------- | ------------------ |
| `EventResetMode.ManualReset` | 一直亮，放行所有等待者，直到 `Reset()` | `ManualResetEvent` |
| `EventResetMode.AutoReset`   | 放行一个等待者后自动熄灭               | `AutoResetEvent`   |

进程内只用到 ManualReset 语义时，优先用更轻量的 `ManualResetEventSlim`；async 代码里等"某件事发生"，用 `TaskCompletionSource`（第 13 章）。

### 7.3 Mutex：跨进程的锁

前面的锁都只在**一个进程内**有效。命名 `Mutex` 是操作系统级别的，同一台机器上的其他进程也能看到，典型用途是"程序只允许运行一个实例"：

```csharp
using var mutex = new Mutex(initiallyOwned: false, name: "Global_NotesDemo_SingleInstance");
if (!mutex.WaitOne(TimeSpan.Zero))
{
    Console.WriteLine($"[{Environment.ProcessId}] 已经有一个实例在运行，退出");
    return;
}
try
{
    Console.WriteLine($"[{Environment.ProcessId}] 拿到 Mutex，工作 2 秒");
    Thread.Sleep(2000);
}
finally
{
    mutex.ReleaseMutex();       // 必须由拿到它的线程释放
}
```

先编译（`dotnet build mutex.cs -o out-mutex`），再在两个进程里几乎同时运行编译产物：

```
[72356] 拿到 Mutex，工作 2 秒
[72375] 已经有一个实例在运行，退出
```

（进程号每次不同。）

- `Mutex` 和 `lock` 一样属于线程：谁 `WaitOne` 拿到，谁 `ReleaseMutex`，所以也不能跨 `await` 使用。
- 每次加锁都要进内核，比 `lock` 慢得多，**进程内同步不要用 `Mutex`**。
- 跨进程的"信号量"用命名 `Semaphore`（不是 `SemaphoreSlim`，Slim 版只能进程内用）。

## 8. 并发集合与 Channel 🟢

### 8.1 普通集合不是线程安全的

`List<T>`、`Dictionary<K,V>` 和 Java 的 `ArrayList`、`HashMap` 一样，**多线程同时写会出错**：

```csharp
var dict = new Dictionary<int, int>();
try
{
    Parallel.For(0, 100000, i => dict[i] = i);
    Console.WriteLine($"Dictionary Count = {dict.Count}（应该是 100000）");
}
catch (Exception e) { Console.WriteLine($"Dictionary 出错: {e.GetType().Name}"); }

var cd = new ConcurrentDictionary<int, int>();
Parallel.For(0, 100000, i => cd[i] = i);
Console.WriteLine($"ConcurrentDictionary Count = {cd.Count}");
```

```
Dictionary 出错: AggregateException
ConcurrentDictionary Count = 100000
```

普通 `Dictionary` 直接抛了异常（被 `Parallel.For` 包成 `AggregateException`）；就算没抛，内部结构也可能已经损坏、数据丢失。多线程共享的集合，要么加锁，要么用 `System.Collections.Concurrent` 里的并发集合。

### 8.2 并发集合

| 集合                        | 特点                                           | Java 对应               |
| --------------------------- | ---------------------------------------------- | ----------------------- |
| `ConcurrentDictionary<K,V>` | 线程安全的字典，最常用                         | `ConcurrentHashMap`     |
| `ConcurrentQueue<T>`        | 先进先出，`Enqueue` / `TryDequeue`             | `ConcurrentLinkedQueue` |
| `ConcurrentStack<T>`        | 后进先出，`Push` / `TryPop`                    | `ConcurrentLinkedDeque` |
| `ConcurrentBag<T>`          | 无序，同一线程放进去再取出来最快               | —                       |
| `BlockingCollection<T>`     | 包装上面的集合，取不到时阻塞等待，可设容量上限 | `BlockingQueue`         |
| `Channel<T>`                | async 版的生产者-消费者队列                    | —                       |

`ConcurrentDictionary` 的原子方法：

```csharp
// 计数：AddOrUpdate 是原子的
var counter = new ConcurrentDictionary<string, int>();
string[] words = ["a", "b", "a", "c", "a", "b"];
Parallel.ForEach(words, w => counter.AddOrUpdate(w, 1, (_, old) => old + 1));
Console.WriteLine(string.Join(", ", counter.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}")));

// GetOrAdd：valueFactory 可能被调用多次
int calls = 0;
var cache = new ConcurrentDictionary<string, string>();
Parallel.For(0, 8, _ => cache.GetOrAdd("key", k => { Interlocked.Increment(ref calls); Thread.Sleep(50); return "v"; }));
Console.WriteLine($"GetOrAdd 的工厂方法被调用了 {calls} 次");

var queue = new ConcurrentQueue<int>();
Parallel.For(0, 1000, i => queue.Enqueue(i));
int sum = 0;
while (queue.TryDequeue(out int x)) sum += x;
Console.WriteLine($"ConcurrentQueue 出队求和 = {sum}");

var bag = new ConcurrentBag<int>();
Parallel.For(0, 1000, i => bag.Add(i));
Console.WriteLine($"ConcurrentBag Count = {bag.Count}");
```

```
a=3, b=2, c=1
GetOrAdd 的工厂方法被调用了 8 次
ConcurrentQueue 出队求和 = 499500
ConcurrentBag Count = 1000
```

- ⚠️ 不要写 `if (!cd.ContainsKey(k)) cd[k] = v;`、`cd[k] = cd[k] + 1;`：每个方法单独是线程安全的，**组合起来就不是了**（又是 check-then-act）。用 `GetOrAdd`、`AddOrUpdate`、`TryAdd`、`TryUpdate`、`TryRemove` 这些一步到位的方法。
- ⚠️ `GetOrAdd` / `AddOrUpdate` 的委托**不在锁里执行**，多个线程同时遇到同一个 key 时，委托可能被调用多次（实测 8 个线程调用了 8 次），只有一个结果被存进去。委托里不要有副作用；创建开销大时用 `ConcurrentDictionary<K, Lazy<V>>`。
- 和 Java `ConcurrentHashMap` 的 `computeIfAbsent` 不同：Java 保证函数对同一个 key 最多执行一次。

### 8.3 生产者-消费者：BlockingCollection

```csharp
using var queue = new BlockingCollection<int>(boundedCapacity: 2);   // 满了 Add 会阻塞
var producer = Task.Run(() =>
{
    for (int i = 1; i <= 5; i++)
    {
        queue.Add(i);
        Console.WriteLine($"生产 {i}");
    }
    queue.CompleteAdding();                    // 告诉消费者：不会再有了
});
var consumer = Task.Run(() =>
{
    foreach (int item in queue.GetConsumingEnumerable())   // 没数据就阻塞；CompleteAdding 后结束
    {
        Thread.Sleep(100);
        Console.WriteLine($"  消费 {item}");
    }
});
Task.WaitAll(producer, consumer);
Console.WriteLine("结束");
```

```
生产 1
生产 2
生产 3
  消费 1
生产 4
  消费 2
生产 5
  消费 3
  消费 4
  消费 5
结束
```

容量是 2，消费者慢（100ms 一个），生产者很快就被"挡住"，只能消费一个再生产一个——这叫**背压**，防止生产太快把内存撑爆。对应 Java 的 `ArrayBlockingQueue`（`put` / `take`）。

### 8.4 Channel<T>：async 版的生产者-消费者 🟢

`BlockingCollection` 的等待是**阻塞线程**的。在 async 代码里，用 `System.Threading.Channels` 的 `Channel<T>`，等待时不占线程：

```csharp
var channel = Channel.CreateBounded<int>(capacity: 2);   // 满了 WriteAsync 会异步等待

var producer = Task.Run(async () =>
{
    for (int i = 1; i <= 5; i++)
    {
        await channel.Writer.WriteAsync(i);
        Console.WriteLine($"生产 {i}");
    }
    channel.Writer.Complete();
});

var consumer = Task.Run(async () =>
{
    await foreach (int item in channel.Reader.ReadAllAsync())
    {
        await Task.Delay(100);
        Console.WriteLine($"  消费 {item}");
    }
});

await Task.WhenAll(producer, consumer);
Console.WriteLine("结束");
```

```
生产 1
生产 2
生产 3
  消费 1
生产 4
  消费 2
生产 5
  消费 3
  消费 4
  消费 5
结束
```

|             | `BlockingCollection<T>`          | `Channel<T>`                                |
| ----------- | -------------------------------- | ------------------------------------------- |
| 等待方式    | 阻塞线程                         | `await`，不占线程                           |
| 写 / 读     | `Add` / `GetConsumingEnumerable` | `Writer.WriteAsync` / `Reader.ReadAllAsync` |
| 结束        | `CompleteAdding()`               | `Writer.Complete()`                         |
| 无界 / 有界 | 构造函数传 `boundedCapacity`     | `CreateUnbounded` / `CreateBounded`         |
| 推荐        | 同步代码                         | ✅ 新代码、async 代码                       |

## 9. 死锁 🟡

两个线程各拿一把锁，再去拿对方手里的那把：

```csharp
static readonly object lockA = new();
static readonly object lockB = new();

var t1 = new Thread(() =>
{
    lock (lockA)
    {
        Thread.Sleep(100);
        Console.WriteLine("线程 1 拿到 A，等 B");
        lock (lockB) { Console.WriteLine("线程 1 拿到 B"); }
    }
}) { IsBackground = true };
var t2 = new Thread(() =>
{
    lock (lockB)
    {
        Thread.Sleep(100);
        Console.WriteLine("线程 2 拿到 B，等 A");
        lock (lockA) { Console.WriteLine("线程 2 拿到 A"); }
    }
}) { IsBackground = true };
t1.Start(); t2.Start();
bool finished = t1.Join(2000) && t2.Join(0);
Console.WriteLine(finished ? "正常结束" : "2 秒后两个线程都没结束 → 死锁");
```

```
线程 1 拿到 A，等 B
线程 2 拿到 B，等 A
2 秒后两个线程都没结束 → 死锁
```

（这里把两个线程设成后台线程，主线程才能退出；否则进程会永远卡住。）

和 MySQL 不同，**.NET 不会自动检测死锁**，线程就永远卡在那里，没有异常、没有日志。

修复：所有线程**按相同的顺序加锁**（都先 A 后 B）：

```csharp
var t1 = new Thread(() => { lock (lockA) { Thread.Sleep(100); lock (lockB) { Console.WriteLine("线程 1 完成"); } } });
var t2 = new Thread(() => { lock (lockA) { Thread.Sleep(100); lock (lockB) { Console.WriteLine("线程 2 完成"); } } });
```

```
线程 1 完成
线程 2 完成
正常结束
```

避免死锁的办法：

| 办法                   | 说明                                                                    |
| ---------------------- | ----------------------------------------------------------------------- |
| 固定加锁顺序           | 最有效。比如转账时按账户 id 从小到大加锁                                |
| 锁的范围尽量小         | 锁里不要调用外部代码（回调、事件、虚方法），不知道它会去拿什么锁        |
| 带超时加锁             | `Monitor.TryEnter(obj, timeout, ref taken)`，拿不到就放弃、记日志、重试 |
| 不要嵌套锁             | 能一把锁解决就别用两把                                                  |
| async 代码里别阻塞等待 | `.Result` / `.Wait()` 在有同步上下文的程序里会死锁（第 13 章）          |

## 10. 选择速查表

| 需求                                   | 用什么                                                        |
| -------------------------------------- | ------------------------------------------------------------- |
| 一个计数器 / 累加                      | `Interlocked.Increment` / `Add`                               |
| 一个"停止"标志，一个线程写、其他线程读 | `CancellationToken`；或 `volatile bool`                       |
| 保护一段多步操作（检查 + 修改）        | `lock`（.NET 9+ 锁对象用 `Lock`）                             |
| 拿锁要能超时                           | `Monitor.TryEnter`                                            |
| 互斥的代码里要 `await`                 | `SemaphoreSlim(1, 1)` + `WaitAsync`                           |
| 限制同时执行的数量                     | `SemaphoreSlim(n)`                                            |
| 读远多于写，读操作较慢                 | `ReaderWriterLockSlim`                                        |
| 跨进程互斥 / 单实例                    | 命名 `Mutex`                                                  |
| 让一批线程等某个时刻统一放行           | `ManualResetEventSlim` / `EventWaitHandle`                    |
| 锁里的代码极短，追求极致性能           | `SpinLock`（一般用不上）                                      |
| 多线程共享字典 / 队列                  | `ConcurrentDictionary` / `ConcurrentQueue`                    |
| 生产者-消费者                          | async 代码用 `Channel<T>`；同步代码用 `BlockingCollection<T>` |
| 等 N 个任务完成                        | `await Task.WhenAll`；同步代码用 `CountdownEvent`（第 11 章） |

最好的同步是**不共享**：每个任务算自己的结果，最后再合并（`Task.WhenAll` 拿结果数组、`Parallel.For` 的线程局部变量，第 12 章）；或者用不可变对象（`record`、`ImmutableList`）。

## 11. 与 Java 对照

| Java                                      | C#                                                                   |
| ----------------------------------------- | -------------------------------------------------------------------- |
| `synchronized (obj) { }`                  | `lock (obj) { }`                                                     |
| `synchronized` 方法                       | 没有对应关键字，用私有锁对象 + `lock`                                |
| `obj.wait()` / `notify()` / `notifyAll()` | `Monitor.Wait` / `Pulse` / `PulseAll`                                |
| `ReentrantLock` / `tryLock(timeout)`      | `Lock`（.NET 9+）、`Monitor.TryEnter`                                |
| `ReentrantReadWriteLock`                  | `ReaderWriterLockSlim`                                               |
| `Semaphore`                               | `SemaphoreSlim`（进程内，支持 `WaitAsync`）、`Semaphore`（可跨进程） |
| `AtomicInteger.incrementAndGet()`         | `Interlocked.Increment(ref x)`                                       |
| `AtomicInteger.compareAndSet(e, v)`       | `Interlocked.CompareExchange(ref x, v, e)`（参数顺序不同，返回旧值） |
| `volatile`                                | `volatile`、`Volatile.Read/Write`                                    |
| `ConcurrentHashMap`                       | `ConcurrentDictionary`                                               |
| `ConcurrentLinkedQueue`                   | `ConcurrentQueue`                                                    |
| `BlockingQueue`（`ArrayBlockingQueue`）   | `BlockingCollection<T>`、`Channel<T>`                                |
| `CountDownLatch`                          | `CountdownEvent`                                                     |
| 跨进程锁（文件锁 `FileLock`）             | 命名 `Mutex`                                                         |

## 12. 小结

1. "先检查再操作"和 `count++` 都不是原子的，多线程下会出错：实测 4 个任务取钱余额变成 -100，9999 次 `count++` 只剩 7901。
2. `lock` 对应 `synchronized`，锁对象用私有 `readonly` 字段（.NET 9+ 用 `Lock`），不要锁 `this`、字符串、`typeof(X)`。
3. 单个变量的计数、交换用 `Interlocked`，比 `lock` 快；只能用在字段上，不能用在属性上。
4. `volatile` 只保证可见性，不保证原子性；实测去掉 `volatile` 后 Debug 正常、Release 死循环——多线程 bug 要在 Release 下测。
5. `lock` 里不能 `await`（CS1996）；async 代码用 `SemaphoreSlim.WaitAsync`，它不可重入。
6. `SpinLock` 是 struct，不能复制、不能声明为 `readonly`；锁里代码稍长就用 `lock`。
7. 多线程共享集合用 `ConcurrentDictionary` 等并发集合，用 `GetOrAdd` / `AddOrUpdate` 一步完成，注意委托可能执行多次；生产者-消费者用 `Channel<T>`。
8. .NET 不会自动检测死锁；按固定顺序加锁、缩小锁范围、用 `TryEnter` 加超时。

---

上一章：[async/await 与异常 🟢](13-async-await.md) · [目录](../README.md) · 下一章：[项目、测试与 NuGet 🟢](15-engineering.md)
