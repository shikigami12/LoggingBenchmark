using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace LogTest
{
    [MemoryDiagnoser]
    public class LoggingBenchmark
    {
        private ILogger<LoggingBenchmark> _enabledLogger;
        private ILogger<LoggingBenchmark> _disabledLogger;
        
        // ServiceProviders for proper cleanup
        private ServiceProvider _enabledProvider;
        private ServiceProvider _disabledProvider;

        // Data
        private readonly Guid orderId = Guid.NewGuid();

        [GlobalSetup]
        public void Setup()
        {
            // 1. Setup Enabled Logger (Log Level: Information)
            // This logger SHOULD process the message formatting.
            var enabledCollection = new ServiceCollection();
            enabledCollection.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Information);
                builder.Services.TryAddEnumerable(
                    ServiceDescriptor.Singleton<ILoggerProvider, RealisticNoIoLoggerProvider>());
            });
            _enabledProvider = enabledCollection.BuildServiceProvider();
            _enabledLogger = _enabledProvider.GetRequiredService<ILogger<LoggingBenchmark>>();

            // 2. Setup Disabled Logger (Log Level: Warning)
            // This logger filters out Information logs at the framework level.
            var disabledCollection = new ServiceCollection();
            disabledCollection.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Warning);
                builder.Services.TryAddEnumerable(
                    ServiceDescriptor.Singleton<ILoggerProvider, RealisticNoIoLoggerProvider>());
            });
            _disabledProvider = disabledCollection.BuildServiceProvider();
            _disabledLogger = _disabledProvider.GetRequiredService<ILogger<LoggingBenchmark>>();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _enabledProvider?.Dispose();
            _disabledProvider?.Dispose();
        }

        // --- BENCHMARKS ---

        // SCENARIO 1: Bad Code, Logging Enabled
        // Cost: String Interpolation (immediate) + Logger Work
        [Benchmark]
        public void Interpolation_Enabled()
        {
            _enabledLogger.LogInformation($"Order {orderId} processed at {DateTime.UtcNow}");
        }

        // SCENARIO 2: Good Code, Logging Enabled
        // Cost: Boxing params + Logger Work (Template Parsing)
        // Note: This is now realistic because RealisticNoIoLogger calls the formatter.
        [Benchmark]
        public void Structured_Enabled()
        {
            _enabledLogger.LogInformation("Order {OrderId} processed at {Timestamp}", orderId, DateTime.UtcNow);
        }

        // SCENARIO 3: Bad Code, Logging DISABLED (The Trap)
        // Cost: String Interpolation happens BEFORE the logger check.
        // Result: High Allocation for nothing.
        [Benchmark]
        public void Interpolation_Disabled()
        {
            _disabledLogger.LogInformation($"Order {orderId} processed at {DateTime.UtcNow}");
        }

        // SCENARIO 4: Good Code, Logging DISABLED (The Goal)
        // Cost: Near Zero (just params array allocation/boxing).
        // Result: Lowest Allocation.
        [Benchmark]
        public void Structured_Disabled()
        {
            _disabledLogger.LogInformation("Order {OrderId} processed at {Timestamp}", orderId, DateTime.UtcNow);
        }
    }

    // --- INFRASTRUCTURE ---

    public class RealisticNoIoLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new RealisticNoIoLogger();
        public void Dispose() { }
    }

    public class RealisticNoIoLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // CRITICAL CHANGE:
            // We explicitly call the formatter to simulate the computational cost 
            // of generating the log message (parsing the template, replacing {OrderId}, etc).
            // We do NOT write to Console/Disk to avoid I/O noise.
            
            if (formatter != null)
            {
                var logMessage = formatter(state, exception);
            }
        }
    }
}
