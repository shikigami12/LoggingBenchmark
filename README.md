# C# Logging Benchmark

This project demonstrates the performance impact of **String Interpolation** vs **Structured Logging** in .NET 8, specifically focusing on allocations and overhead when logging is **Disabled**.

## Scenarios

We compare 4 scenarios using `Microsoft.Extensions.Logging` with a custom `RealisticNoIoLogger` (simulates formatting cost but skips I/O).

1. **Interpolation_Enabled**: `LogInformation($"...")`
    - The string is formatted immediately.
2. **Structured_Enabled**: `LogInformation("...{...}", ...)`
    - Formatting happens inside the logger.
3. **Interpolation_Disabled**: `LogInformation($"...")` (LogLevel is Warning)
    - **The Anti-Pattern**: The string is formatted *before* the logger checks if it is enabled.
4. **Structured_Disabled**: `LogInformation("...{...}", ...)` (LogLevel is Warning)
    - **The Best Practice**: The formatting is skipped entirely.

## Latest Results (BenchmarkDotNet)

Run on .NET 8.0.17 (X64 RyuJIT).

| Method                 | Mean      | Allocated | Description |
|----------------------- |----------:|----------:|:----------- |
| **Interpolation_Enabled**  | 239.55 ns |     **176 B** | Slow & Allocating. |
| **Structured_Enabled**     | 203.99 ns |     272 B | *Note: Higher allocation due to boxing value types + formatting.* |
| **Interpolation_Disabled** | 240.65 ns |     **176 B** | **FAIL**: Allocates full string even when NOT logged! |
| **Structured_Disabled**    |  79.29 ns |      **96 B** | **WIN**: 3x Faster, ~50% less allocation.* |

*\*Note: The 96B allocation in `Structured_Disabled` comes from boxing value types (`Guid`, `DateTime`) into the `object[] params` array. This is improved further in .NET using `LoggerMessage` or source generators (ZLogger, etc.), which can achieve 0 allocation.*

## Key Takeaway

**Never use string interpolation in log methods.**

```csharp
// BAD - Allocates string even if logging is disabled
_logger.LogInformation($"Processing {id}");

// GOOD - Delays formatting until needed
_logger.LogInformation("Processing {Id}", id);
```

### How to Run

1. **Run Benchmark** (Requires Release Mode):

    ```powershell
    dotnet run -c Release
    ```

2. **Run Simulation** (Infinite loop for profiling):

    ```powershell
    dotnet run -c Release -- longrun
    ```
