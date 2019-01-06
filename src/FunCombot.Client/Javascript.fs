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
            do! FunctionName.Create "initDropdowns" |> jsInvokeUnit
        }

module Charting =
    let getChartFunctionName name = 
        name |> (sprintf "charting.%s" >> FunctionName.Create)
        
    type IAxisTick = {
        format: string
    }
    
    type IAxisData = {
        ``type``: string
        tick: IAxisTick
    }
    
    type IAxis = {
        x: IAxisData
    }
    
    type IColumnData = {
        name: string
        data: obj list
    }
    
    type IChartConfiguration = {
        x: string
        columns: IColumnData list
        axis: IAxis
    }
        
    let createChart id (data: IChartConfiguration) =
        let bindTo = sprintf "#%s" id
        
        getChartFunctionName "drawChart"
        |> jsInvokeIgnore [|bindTo; data|]