namespace FunCombot.Client.Components

open Bolero.Html
open Bolero.Remoting

type IRemoteServiceProvider = 
    abstract GetService<'T when 'T :> IRemoteService> : unit -> 'T

module CommonNodes =
    let loadingIcon = i ["class" => "spinner loading icon"] []

    let loadingDiv = div ["class" => "ui active centered loader"] []

[<RequireQualifiedAccess>]
module ClientSideCache =
    open System
    open Microsoft.Extensions.Caching.Memory

    let private cache =
        let options = MemoryCacheOptions(ExpirationScanFrequency = TimeSpan.FromSeconds(20.))
        new MemoryCache(options)
    
    let getOrCreate key value (duration: TimeSpan) =
        cache.GetOrCreate(key, fun entry ->
            entry.SetSlidingExpiration(duration) |> ignore
            value)
    
    let getOrCreateAsync key value (duration: TimeSpan) =
        cache.GetOrCreateAsync(key, fun entry ->
            entry.SetSlidingExpiration(duration) |> ignore
            value |> Async.StartAsTask) |> Async.AwaitTask
    
    let getOrCreateAsyncFn fn (duration: TimeSpan) key =
        cache.GetOrCreateAsync(key, fun entry ->
            entry.SetSlidingExpiration(duration) |> ignore
            fn(key) |> Async.StartAsTask) |> Async.AwaitTask