# Parallel.For 用法 🟢

> 对应练习：[17-concurrent-bag.cs](advanced/17-concurrent-bag.cs)、[07-parallel-for-break-stop.cs](advanced/07-parallel-for-break-stop.cs)、[07-parallel-for-thread-local.cs](advanced/07-parallel-for-thread-local.cs)、[07-parallel-foreach.cs](advanced/07-parallel-foreach.cs)
>
> 更完整的 Task / Parallel 内容见 [第 12 章 Task、并行与定时器](notes/12-task-parallel.md) §7。
>
> 本文的输出都是在 .NET 11、8 核 CPU 上实际跑出来的。并行执行的顺序和耗时每次运行都会不同。

## 1. 是什么

`Parallel.For` 是**并行版的 for 循环**：它把各次迭代分给线程池里的多个线程同时执行。

```csharp
// 普通 for
for (int i = 0; i < array.Length; i++) { bag.Add(array[i]); }

// Parallel.For(起始值（包含）, 结束值（不包含）, 循环体)
Parallel.For(0, array.Length, i =>
{
    bag.Add(array[i]);
});
```

[17-concurrent-bag.cs](advanced/17-concurrent-bag.cs) 连续跑两次的输出：

```
number = 4 number = 1 number = 2 number = 5 number = 3
number = 5 number = 2 number = 3 number = 1 number = 4
```

四个要点：

| 要点 | 说明 |
|---|---|
| 范围 `[from, to)` | 和 `for (i = from; i < to; i++)` 一样，**不包含** `to` |
| 顺序不确定 | 迭代之间没有先后顺序，所以每次输出都不一样 |
| **会阻塞** | 所有迭代执行完，`Parallel.For` 才返回，不需要再 `Wait` |
| 调用线程也参与执行 | 主线程不会闲着等，它自己也会执行一部分迭代 |

实测：1000 次迭代分到了 9 个线程上，其中主线程（id = 1）执行了 121 次：

```
主线程 id = 1，参与的线程：1(主)×121, 4×95, 6×95, 7×111, 8×125, 9×111, 10×110, 11×109, 12×123
```

## 2. 循环体里的共享数据必须线程安全 ⚠️

循环体在多个线程上**同时**执行。只要读写了同一个变量或集合，就要保证线程安全。

```csharp
long bad = 0;
Parallel.For(0, 1_000_001, i => { bad += i; });   // ❌ += 不是原子操作
```
```
不加锁直接 += 的结果 = 77190346775        <- 正确值是 500000500000
```

集合也一样：多个线程同时调用 `List<T>.Add`、`Stack<T>.Push`，会丢数据或者抛 `IndexOutOfRangeException`。正确做法有三种：

| 做法 | 例子 |
|---|---|
| 用并发集合 | `ConcurrentBag`、`ConcurrentQueue`、`ConcurrentDictionary`（17-concurrent-bag.cs 就是这么做的） |
| 每次迭代只写自己那一格 | `Parallel.For(0, n, i => result[i] = Compute(i));` 不同的 `i` 写不同的位置，互不干扰 |
| 原子操作或加锁 | `Interlocked.Add(ref total, x)`、`lock`；更好的做法见第 5 节的线程本地变量 |

## 3. 常用重载

```csharp
// 1. 基本用法
Parallel.For(0, 100, i => Work(i));

// 2. 提前退出：循环体多一个 ParallelLoopState 参数
Parallel.For(0, 100, (i, state) => { if (i == 50) state.Break(); });

// 3. 控制并行度、取消
Parallel.For(0, 100, new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = token }, i => Work(i));

// 4. 线程本地变量：localInit / body / localFinally
Parallel.For(0, 100, () => 0L, (i, state, local) => local + i, local => Interlocked.Add(ref total, local));

// 5. long 范围的版本
Parallel.For(0L, 10_000_000_000L, i => ...);

// 6. 遍历集合
Parallel.ForEach(list, item => Work(item));
```

返回值 `ParallelLoopResult` 里有两个属性：`IsCompleted` 表示是否全部执行完，`LowestBreakIteration` 是调用 `Break` 的最小迭代号。

## 4. 提前退出：Break 和 Stop 🟡

并行循环里不能用 `break`，要调用 `state.Break()` 或者 `state.Stop()`：

| | `state.Break()` | `state.Stop()` |
|---|---|---|
| 含义 | 不再开始**大于**当前 i 的迭代 | 尽快停止，不再开始任何新的迭代 |
| 小于当前 i 的迭代 | **保证全部执行** | 不保证 |
| `LowestBreakIteration` | 当前的 i | `null` |
| 适合 | “找到第一个满足条件的位置”，前面的都必须处理完 | “找到任意一个就行”，比如搜索 |

```csharp
var r1 = Parallel.For(0, 1000, (i, s) => { if (i == 500) s.Break(); ... });
var r2 = Parallel.For(0, 1000, (i, s) => { if (i == 500) s.Stop();  ... });
```
```
Break: IsCompleted=False, LowestBreakIteration=500
Break 后 0..499 是否全部执行：True
Stop:  IsCompleted=False, LowestBreakIteration=null, 执行了 615 次
```

注意 Stop 执行了 615 次，比 500 还多。原因是调用 `Stop` 时，其他线程上已经开始的迭代会继续执行完。`Break` 也一样：大于 500 的迭代可能已经执行了一部分。

## 5. 线程本地变量：求和、计数的正确写法 🟡

如果每次迭代都调用一次 `Interlocked.Add` 或者加一次 `lock`，线程之间会频繁争抢。更好的做法是：**每个线程先在自己的局部变量里累加，最后再合并一次**。

```csharp
long total = 0;
Parallel.For(0, 1_000_001,
    () => 0L,                                         // localInit：每个线程开始时执行一次，给出初始值
    (i, state, local) => local + i,                   // body：每次迭代执行，返回新的 local，不需要加锁
    local => Interlocked.Add(ref total, local));      // localFinally：每个线程结束时执行一次，合并结果
```
```
0..1000000 求和 = 500000500000（正确值 500000500000）
```

这样 100 万次迭代，只需要大约“线程数”次 `Interlocked.Add`。

## 6. ParallelOptions：并行度与取消

```csharp
var options = new ParallelOptions
{
    MaxDegreeOfParallelism = 2,      // 最多同时用 2 个线程；默认 -1，表示不限制
    CancellationToken = cts.Token,   // 取消后抛 OperationCanceledException
};
```

实测：

```
MaxDegreeOfParallelism = 2：用到 2 个线程
取消：OperationCanceledException，共执行 163 次（第 100 次时取消）
取消：OperationCanceledException，共执行 102 次（第 100 次时取消）
取消：OperationCanceledException，共执行 100 次（第 100 次时取消）
```

取消也不是立即生效的：已经开始的迭代会执行完，所以实际执行的次数会比 100 多一些。

## 7. 异常

循环体里抛出的异常，会被包装成 **`AggregateException`** 抛出。抛异常后，其他迭代也会尽快停止。

```csharp
try
{
    Parallel.For(0, 10, i => { if (i == 3) throw new InvalidOperationException("i = 3 出错"); });
}
catch (AggregateException ex)
{
    Console.WriteLine(ex.InnerExceptions[0].Message);
}
```
```
异常：AggregateException -> i = 3 出错
```

同时可能有多个迭代抛异常，所以要从 `InnerExceptions` 里取出全部异常。

## 8. 什么时候用，什么时候不用

| ✅ 适合 | ❌ 不适合 |
|---|---|
| **CPU 密集**：每次迭代计算量大，比如图片处理、加密、大量数值计算 | 每次迭代的工作量很小：拆分和调度的开销比工作本身还大 |
| 迭代之间互相独立 | 迭代之间有先后依赖，比如 `a[i] = a[i-1] + 1` |
| | **I/O 操作**：HTTP 请求、读写文件、查数据库，这些要用 async/await |

实测对比：

```
每次只乘 2（1000 万次 ×3）：     for 147 ms，Parallel.For 175 ms     <- 并行反而更慢
每次算 2 万次开方（2000 次）：    for 240 ms，Parallel.For  39 ms，CPU 8 核   <- 快了约 6 倍
```

17-concurrent-bag.cs 里只有 5 个元素，每次迭代只是 `Add` 一个数。这种规模适合用来学习用法，实际项目里直接用普通 for 循环更快。

## 9. 不要把 async lambda 传给 Parallel.For ⚠️

`Parallel.For` 的参数是 `Action<int>`。传一个 async lambda 进去，它会变成 **async void**：Parallel.For 只会等到第一个 `await` 为止，不会等异步操作真正完成。async lambda 里抛出的异常，还会让进程崩溃。

```csharp
int a = 0;
Parallel.For(0, 5, async i => { await Task.Delay(200); Interlocked.Increment(ref a); });   // ❌
Console.WriteLine(a);
```
```
async lambda：Parallel.For 返回时完成了 0 个
再等 500ms 后：5 个（任务在 Parallel.For 返回后才陆续完成）
```

异步操作要用 .NET 6 新增的 `Parallel.ForEachAsync`：

```csharp
await Parallel.ForEachAsync(Enumerable.Range(0, 5), async (i, ct) =>
{
    await Task.Delay(200, ct);
    Interlocked.Increment(ref b);
});   // ✅
```
```
Parallel.ForEachAsync：返回时完成了 5 个
```

也可以用 `Task.WhenAll(items.Select(async x => ...))`，见[第 13 章](notes/13-async-await.md)。

## 10. 相关 API 对比

| API | 用途 |
|---|---|
| `Parallel.For` | 按下标并行，CPU 密集 |
| `Parallel.ForEach` | 遍历集合并行，CPU 密集 |
| `Parallel.Invoke` | 同时执行几个不同的方法 |
| `Parallel.ForEachAsync` | 异步操作并行，可以限制并发数 |
| PLINQ `.AsParallel()` | 用 LINQ 的写法并行查询，比如 `list.AsParallel().Where(...).Select(...)` |
| `Task.WhenAll` | 同时等待多个异步任务 |

## 11. 和 Java 对比

| Java | C# |
|---|---|
| `IntStream.range(0, n).parallel().forEach(i -> ...)` | `Parallel.For(0, n, i => ...)` |
| `list.parallelStream().forEach(...)` | `Parallel.ForEach(list, ...)` |
| `ForkJoinPool.commonPool()` | 线程池（`ThreadPool`） |
| `parallelStream().reduce(...)` / `collect(...)` | 线程本地变量重载，或者 PLINQ 的 `.AsParallel().Sum()` |

## 12. 小结

1. `Parallel.For(from, to, i => ...)` 是并行版的 for，范围是 `[from, to)`，**执行顺序不确定**。
2. 它会**阻塞**到所有迭代执行完，调用它的线程也参与执行。
3. 循环体里的共享变量、集合必须线程安全：用并发集合、每次迭代只写自己的下标，或者 `Interlocked` / `lock`。
4. 提前退出用 `state.Break()`（保证前面的迭代都执行完）或 `state.Stop()`（尽快停止）。
5. 求和、计数用线程本地变量重载，避免每次迭代都加锁。
6. 循环体的异常被包装成 `AggregateException`；取消后抛 `OperationCanceledException`。
7. 只用于 **CPU 密集、每次迭代工作量够大**的场景；工作量太小时，并行反而更慢。
8. **不要传 async lambda**，异步操作用 `Parallel.ForEachAsync` 或 `Task.WhenAll`。
