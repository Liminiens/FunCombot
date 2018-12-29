module FunCombot.Client.Main

open FunCombot.Client.Javascript
open System
open System.Threading.Tasks
open FSharp.Control.Tasks
open Elmish
open Bolero
open Bolero.Html
open Microsoft.AspNetCore.Blazor.Routing


module Counter =
    type CounterModel = {
        value: int
    }
    
    type CounterMessage =
        | Increment
        | Decrement
           
    let update message model =
        match message with
        | Increment -> { model with value = model.value + 1 }
        | Decrement -> { model with value = model.value - 1 }
        
    type CounterComponent() =
        inherit ElmishComponent<CounterModel, CounterMessage>()
        
        override this.View model dispatch =
            concat [           
                button [on.click (fun _ -> dispatch Decrement)] [text "-"]
                span [] [textf " %i " model.value]
                button [on.click (fun _ -> dispatch Increment)] [text "+"]
            ]      

module LineChartComponent =

    type LineChartComponent() =
        inherit ElmishComponent<unit, unit>()
        
        override this.View model dispatch =
            concat [           
                div [attr.id "test"] []
                button [ on.click (fun e -> Charting.createChart "test" {
                    columns = [
                      ["data1"; 30; 200; 100; 400; 150; 250];
                      ["data2"; 50; 20; 10; 40; 15; 25];           
                    ]
                })] [text "Elo"]
            ] 

module Container = 
    open Counter
    open LineChartComponent
    
    type ContainerModel = {
        Counter: CounterModel
    }
    
    type ContainerMessage =
        | CounterMessage of Counter.CounterMessage 
        | ChartMessage of unit 
        
    let initModel = {
        Counter = { value = 0 }
    }
    
    let update message (model: ContainerModel) =
        match message with
        | CounterMessage(message) ->
            { model with Counter = Counter.update message model.Counter }
        | ChartMessage _ ->
            model
            
    let view model dispatch =
        concat [
            header [attr.``class`` "ui inverted vertical segment"; attr.style "margin-bottom: 1.5rem"] [
               nav [attr.``class`` "ui menu inverted"] [
                   div [attr.``class`` "header item"] [
                       navLink NavLinkMatch.All [attr.href "/"] [text "Home"]
                   ]
                   div [attr.``class`` "item"] [
                       text "123"
                   ]
               ] 
            ]
            main [attr.``class`` "ui stackable grid container centered"] [
                div [attr.``class`` "ten wide column"] [
                    ecomp<CounterComponent,_,_> {
                        value = model.Counter.value
                    } ^fun message -> dispatch (CounterMessage(message))
                ]
            ]
            ecomp<LineChartComponent,_,_> () ^fun message -> dispatch (ChartMessage(message))
        ]
    
    type MainComponent() =
        inherit ProgramComponent<ContainerModel, ContainerMessage>()
    
        override this.Program =
            Program.mkSimple (fun _ -> initModel) update view
