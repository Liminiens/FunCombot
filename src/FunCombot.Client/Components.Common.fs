namespace FunCombot.Client.Components

open Bolero.Html
open Bolero.Remoting

type IRemoteServiceProvider = 
    abstract GetService<'T when 'T :> IRemoteService> : unit -> 'T

module CommonNodes =
    let loadingIcon = i ["class" => "spinner loading icon"] []

    let loadingDiv = div ["class" => "ui active centered loader"] []

