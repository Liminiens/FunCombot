﻿namespace StatsBot.Client.Types

open Bolero

type ApplicationPage =
    | [<EndPoint("/")>]
      Home
    | [<EndPoint("/chat")>]
      Chat of name: string * section: string    

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
            
type DateUnit =
  | DayUnit
  | WeekUnit
  | MonthUnit
  member this.Name =
      match this with
      | DayUnit -> "day"
      | WeekUnit -> "week"
      | MonthUnit -> "month"
  
  static member FromString(str) =
       match str with
       | "day" -> Some DayUnit
       | "week" -> Some WeekUnit
       | "month" -> Some MonthUnit
       | _ -> None