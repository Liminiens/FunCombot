namespace FunCombot.Client

open System
open Microsoft.FSharp.Reflection

[<AutoOpen>]
module Common =
    let inline (^) f x = f x

    let inline isNotNull value = not <| isNull value

    let inline isNullOrWhiteSpace (str: string) = String.IsNullOrWhiteSpace(str)
    
[<AutoOpen>]    
module Reflection =
    let getUnionCaseNames<'T> =
        FSharpType.GetUnionCases typeof<'T>
        |> Array.map ^fun case -> case.Name
    
    let getUnionCaseName (case: 'T) = 
        match FSharpValue.GetUnionFields(case, typeof<'T>) with
        | case, _ -> case.Name
    
    let getUnionCases<'T> =
        FSharpType.GetUnionCases(typeof<'T>)
        |> Array.map ^fun case -> (FSharpValue.MakeUnion(case, [| |]) :?> 'T, case.Name, case.Tag)