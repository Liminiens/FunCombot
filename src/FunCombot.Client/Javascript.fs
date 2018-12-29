namespace FunCombot.Client.Javascript

open System
open Microsoft.JSInterop

[<AutoOpen>]
module JSRuntimeExt =
    let jsInvokeUnit func args =
        JSRuntime.Current.InvokeAsync<obj>(func, args)
        |> Async.AwaitTask
        |> Async.Ignore
        |> Async.Start

module Charting =    
    type Data = {
        columns: obj list list
    }
    
    type ChartData = {
        bindto: string;
        data: Data;
    }
        
    let createChart id data=
        let chartData = {
            bindto = sprintf "#%s" id;
            data = data
        }
        
        jsInvokeUnit "funcombot.charting.drawChart" [|chartData|]