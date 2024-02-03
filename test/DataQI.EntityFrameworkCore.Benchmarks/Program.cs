#if DEBUG 
using BenchmarkDotNet.Configs;
#else
using DataQI.EntityFrameworkCore.Benchmarks;
#endif
using Testcontainers.MsSql;
using static DataQI.EntityFrameworkCore.Benchmarks.ConsoleEx;

WriteLine();
WriteInfoLine("Welcome to DataQI.EntityFramework's LIB performance benchmark!");
WriteLine();
WriteLine("Starting Database Setup...");
WriteWarningLine("WARNING: You'll need to have a container engine like docker running!");
WriteLine("Press any key to continue...");
ReadKey();

var sqlContainer = new MsSqlBuilder().Build();
await sqlContainer.StartAsync();
Environment.SetEnvironmentVariable("ConnectionString", sqlContainer.GetConnectionString());
DatabaseEnsurer.Execute();

WriteSuccessLine("Database setup completed.");
WriteLine();

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args,
#if DEBUG 
    new DebugInProcessConfig()
#else
    new Config()
#endif
);