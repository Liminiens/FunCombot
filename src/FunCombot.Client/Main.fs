module FunCombot.Client.Main

open Elmish
open Bolero
open Bolero.Html
open Bolero.Html
open Bolero.Html
open Microsoft.AspNetCore.Blazor.Routing

[<AutoOpen>]
module Common =
    let inline (^) f x = f x

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
        
    type MainComponent() =
        inherit ElmishComponent<CounterModel, CounterMessage>()
        
        override this.View model dispatch =
            concat [           
                button [on.click (fun _ -> dispatch Decrement)] [text "-"]
                span [] [textf " %i " model.value]
                button [on.click (fun _ -> dispatch Increment)] [text "+"]
            ]      

module Container = 
    open Counter
    
    type ContainerModel = {
        Counter: CounterModel
    }
    
    type ContainerMessage = ContainerMessage of Counter.CounterMessage 
        
    let initModel = {
        Counter = { value = 0 }
    }
    
    let update (ContainerMessage(message)) (model: ContainerModel) =
        { model with Counter = Counter.update message model.Counter }
            
    let view model dispatch =
        concat [
            header [attr.``class`` "ui inverted vertical segment"; attr.style "margin-bottom: 25px"] [
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
                    ecomp<MainComponent,_,_> {
                        value = model.Counter.value
                    } ^fun message -> dispatch (ContainerMessage(message))
                ]
            ] 
        ]
    
    type MainComponent() =
        inherit ProgramComponent<ContainerModel, ContainerMessage>()
    
        override this.Program =
            Program.mkSimple (fun _ -> initModel) update view
