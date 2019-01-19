namespace StatsBot.Client.Components

open Bolero.Html
open Bolero.Remoting

type IRemoteServiceProvider = 
    abstract GetService<'T when 'T :> IRemoteService> : unit -> 'T

[<RequireQualifiedAccess>]
module CommonNodes =
    let loadingIcon = i ["class" => "spinner loading icon"] []

    let loadingDiv = div ["class" => "ui active centered loader"] []

type DynamicModel<'T> =
    | NotLoaded
    | Model of 'T
        
[<RequireQualifiedAccess>]
module Debounce =
    open Elmish
    open System
    
    type DelayedModel<'a> =
        { Delay : TimeSpan
          Input : 'a * DateTime
          Output : 'a
          OutputDone : bool }
        member this.TimeUntilOutput = max (snd this.Input + this.Delay - DateTime.Now) TimeSpan.Zero
    
    let init delay value =
        { Delay = delay
          Input = value, DateTime.Now - delay
          Output = value
          OutputDone = true }
    
    type Msg<'a> =
        | Input of 'a
        | TryOutput
        | Output of 'a
    
    let private delayCmd (delay: TimeSpan) msg =
        Cmd.ofAsync
            (fun () -> async { do! Async.Sleep (int delay.TotalMilliseconds) }) ()
            (fun () -> msg)
            (fun _ -> msg)
    
    let private updateInternal msg model : DelayedModel<'a> * Cmd<Msg<'a>> =
        match msg with
        | Input value ->
            { model with Input = value, DateTime.Now; OutputDone = false }, delayCmd model.TimeUntilOutput TryOutput
        | TryOutput ->
            let cmd =
                if model.OutputDone then
                    Cmd.none
                elif model.TimeUntilOutput <= TimeSpan.Zero then
                    model.Input |> fst |> Output |> Cmd.ofMsg
                else
                    delayCmd model.TimeUntilOutput TryOutput
            model, cmd
        | Output value ->
            let newModel =
                if model.OutputDone then model
                else { model with Output = value; OutputDone = true }
            newModel, Cmd.none

    /// Create the Debounce input command
    let inputCmd value wrapMsg = value |> Input |> wrapMsg |> Cmd.ofMsg
    
    /// Update the Debounce model inside the parent model and send a command every time the debounced value changes
    let updateWithCmd msg wrapMsg model wrapModel cmdOnOutput =
        let (model, cmd) = updateInternal msg model
        let cmd' =
            [ yield Cmd.map wrapMsg cmd
              match msg with Output value -> yield cmdOnOutput value | _ -> () ]
            |> Cmd.batch
        wrapModel model, cmd'
    
    /// Update the Debounce model inside the parent model
    let update msg wrapMsg model wrapModel = updateWithCmd msg wrapMsg model wrapModel (fun _ -> Cmd.none)