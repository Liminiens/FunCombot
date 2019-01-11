namespace StatsBot.Client

open System
open Microsoft.FSharp.Reflection

[<AutoOpen>]
module Cmd =
    open Elmish
    let wrapAndBatchSub (f: 'T -> 'TTo) (commands: Sub<'T> list) =
        commands
        |> List.map (fun cmd -> cmd |> Cmd.ofSub |> Cmd.map f)
        |> Cmd.batch
    
    let wrapAndBatchCmd (f: 'T -> 'TTo) (commands: Cmd<'T> list) =
        commands
        |> List.map (fun cmd -> cmd |> Cmd.map f)       
        |> Cmd.batch
        
[<AutoOpen>]
module Common =
    let inline (^) f x = f x

    let inline isNotNull value = not <| isNull value

    let inline isNullOrWhiteSpace str = String.IsNullOrWhiteSpace(str)
    
[<AutoOpen>]    
module Reflection =
    let getUnionCaseNames<'T> =
        FSharpType.GetUnionCases typeof<'T>
        |> Array.map ^fun case -> case.Name
    
    let createUnionCase<'T> tag =
        FSharpType.GetUnionCases(typeof<'T>)
        |> Array.tryFind ^fun case -> case.Tag = tag
        |> Option.bind ^fun c -> Some (FSharpValue.MakeUnion(c, [| |]) :?> 'T)
        
    
    let getUnionCaseName (case: 'T) = 
        match FSharpValue.GetUnionFields(case, typeof<'T>) with
        | case, _ -> case.Name
    
    let getUnionCases<'T> =
        FSharpType.GetUnionCases(typeof<'T>)
        |> Array.map ^fun case -> (FSharpValue.MakeUnion(case, [| |]) :?> 'T, case.Name, case.Tag)
        
[<AutoOpen>]
module TryParse =
    let tryParseWith tryParseFunc = tryParseFunc >> function
        | true, v -> Some v
        | false, _ -> None