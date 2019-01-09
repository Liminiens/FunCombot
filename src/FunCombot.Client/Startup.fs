namespace FunCombot.Client

open Microsoft.AspNetCore.Blazor.Builder
open Microsoft.AspNetCore.Blazor.Hosting
open Microsoft.Extensions.DependencyInjection
open Bolero.Remoting
open Components.Main.MainComponent

type Startup() =

    member __.ConfigureServices(services: IServiceCollection) =
        services.AddSingleton<IRemoteProvider, ClientRemoteProvider>() |> ignore

    member __.Configure(app: IBlazorApplicationBuilder) =
        app.AddComponent<MainComponent>("#main")

module Program =

    [<EntryPoint>]
    let Main args =
        BlazorWebAssemblyHost.CreateDefaultBuilder()
            .UseBlazorStartup<Startup>()
            .Build()
            .Run()
        0
