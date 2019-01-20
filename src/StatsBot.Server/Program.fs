namespace StatsBot.Server
#nowarn "0067"

open System
open Serilog
open Serilog.Events
open Serilog.Exceptions
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Hosting

module Program =
    
    let exitCode = 0
    let errorExitCode = 1
    
    [<EntryPoint>]
    let main args =
        let loggerConfiguration = new LoggerConfiguration()
        Log.Logger <-
            loggerConfiguration
                .MinimumLevel.Debug()
                #if DEBUG
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                #endif
                .Destructure.FSharpTypes()
                .Enrich.WithExceptionDetails()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger() :> ILogger
        try
            try
                Log.Information("Starting web host")                   
                WebHost
                    .CreateDefaultBuilder(args)
                    .UseKestrel()
                    .UseSerilog()
                    .UseStartup<Startup>()
                    .Build()
                    .Run()

                exitCode
            with
                | :? Exception as ex ->
                    Log.Fatal(ex, "Host terminated unexpectedly") |> ignore
                    errorExitCode
        finally
            Log.CloseAndFlush() |> ignore


