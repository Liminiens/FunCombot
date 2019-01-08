namespace FunCombot.Client.Types

open Bolero
open Bolero.Remoting

type IRemoteServiceProvider = 
    abstract GetService<'T when 'T :> IRemoteService> : unit -> 'T

type ApplicationPage =
    | [<EndPoint("/")>]
      Home
    | [<EndPoint("/chat")>]
      Chat of name: string * section: string    

type DynamicModel<'T> =
    | NotLoaded
    | Model of 'T

type Chat =
    | Fsharpchat
    | Dotnetruchat
    | NetTalks
    | Pronet
    | MicrosoftStackJobs
    member this.DisplayName =
        match this with
        | Dotnetruchat -> "DotNetRuChat"
        | NetTalks -> ".NET Talks"
        | Pronet -> "pro .net"
        | Fsharpchat -> "FSharp Chat"
        | MicrosoftStackJobs -> "Microsoft Stack Jobs"
            
    member this.UrlName =
        match this with
        | Dotnetruchat -> "dotnet-ru-chat"
        | NetTalks -> "net-talks"
        | Pronet -> "pro-net"
        | Fsharpchat -> "fsharp-chat"
        | MicrosoftStackJobs -> "ms-stack-jobs"
        
    static member FromString(name: string) =
        match name with
        | "dotnet-ru-chat" -> Some Dotnetruchat
        | "net-talks"  -> Some NetTalks
        | "pro-net" -> Some Pronet
        | "fsharp-chat"  -> Some Fsharpchat
        | "ms-stack-jobs" -> Some MicrosoftStackJobs
        | _ -> None
        
type GraphUnit =
   | Day
   | Week
   | Month
   member this.Name =
       match this with
       | Day -> "day"
       | Week -> "week"
       | Month -> "month"
       
   static member FromString(str) =
       match str with
       | "day" -> Some Day
       | "week" -> Some Week
       | "month" -> Some Month
       | _ -> None