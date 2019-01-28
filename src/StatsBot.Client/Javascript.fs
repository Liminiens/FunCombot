namespace StatsBot.Client.Javascript

open System
open Microsoft.JSInterop

module JSRuntimeExt =
    type FunctionName =
        private FunctionName of string
            static member Create(name: string) =
                FunctionName(sprintf "statsbot.%s" name)
                
    let jsInvoke<'T> args (FunctionName(func)) =
        JSRuntime.Current.InvokeAsync<'T>(func, args)
        |> Async.AwaitTask
    
    let jsInvokeUnit<'T> func =
        jsInvoke<'T> [||] func

module SemanticUi =
    open JSRuntimeExt
    
    let initJs () =
        async {
            do! FunctionName.Create "initDropdowns" |> jsInvokeUnit
        }

module Charting =
    open JSRuntimeExt
    
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
        
    let createChart bindTo (data: IChartConfiguration) =
        let bindTo = sprintf """[id="%s"]""" bindTo
        
        getChartFunctionName "drawChart"
        |> jsInvoke<unit> [|bindTo; data|]