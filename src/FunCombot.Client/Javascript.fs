namespace FunCombot.Client.Javascript

open System
open Microsoft.JSInterop

[<AutoOpen>]
module JSRuntimeExt =
    type FunctionName =
        private FunctionName of string
            static member Create(name: string) =
                FunctionName(sprintf "funcombot.%s" name)
                
    let jsInvoke<'T> args (FunctionName(func)) =
        JSRuntime.Current.InvokeAsync<'T>(func, args)
        |> Async.AwaitTask
        
    let jsInvokeIgnore args func =
        jsInvoke<obj> args func
        |> Async.Ignore
        |> Async.Start
    
    let jsInvokeUnit<'T> func =
        jsInvoke<'T> [||] func

module SemanticUi =
    let initJs () =
        async {
            do! FunctionName.Create "initDropdowns" |> jsInvokeUnit<unit>
        }

module Charting =
    let getChartFunctionName name = 
        name |> (sprintf "charting.%s" >> FunctionName.Create)
    
    type Data = {
        columns: obj list list
    }
    
    type ChartData = {
        bindto: string;
        data: Data;
    }
        
    let createChart id data =
        let chartData = {
            bindto = sprintf "#%s" id;
            data = data
        }
        
        getChartFunctionName "drawChart"
        |> jsInvokeIgnore [|chartData|]