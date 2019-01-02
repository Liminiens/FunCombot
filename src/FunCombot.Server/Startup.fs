namespace FunCombot.Server

open System
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration
open Serilog
open Serilog.Events
open Serilog.Exceptions
open Bolero.Remoting
open FunCombot

type Startup() =

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    member this.ConfigureServices(services: IServiceCollection) =
        ()

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    member this.Configure(app: IApplicationBuilder, env: IHostingEnvironment) =
        app.UseRemoting()
           .UseBlazor<Client.Startup>()
        |> ignore

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
