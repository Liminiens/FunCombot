module rec FunCombot.Client.Main

open Elmish
open Bolero
open Bolero.Html
open FunCombot.Client
open FunCombot.Client.Javascript

type ApplicationPage =
    | [<EndPoint("/")>]
      Home
    | [<EndPoint("/chat")>]
      Chat of name: string * section: string    

module LineChartComponent =
    open Charting
    
    let chartData = {
        x = "x";
        columns = [
            { name = "x"; data = ["2013-01-01"; "2013-01-02"; "2013-01-03"; "2013-01-04"; "2013-01-05"; "2013-01-06"] };
            { name = "data1"; data = [30; 200; 100; 400; 150; 250] };
            { name = "data2"; data = [130; 200; 100; 200; 140; 30] };
       ];
       axis = {
           x = {
               ``type`` = "timeseries"
               tick = {
                    format = "%Y-%m-%d"
               }
           }
       }
    }
    
    type LineChartComponent() =
        inherit ElmishComponent<unit, unit>()
        
        override this.View model dispatch =
            concat [     
                div [attr.id "test"] []
                button [ on.click (fun e -> Charting.createChart "test" chartData)] [text "Elo"]             
            ] 

module HeaderComponent =
    
    type ChatName =
        | Dotnetruchat
        | NetTalks
        | Pronet
        | Fsharpchat
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
        
    type HeaderComponentMessage =
        | SetChat of ChatName
        | ChangeChat of ChatName
    
    type HeaderComponentModel = {
        CurrentChat: ChatName;
    }
    
    let update message model =
        match message with
        | ChangeChat chat ->
            { model with CurrentChat = chat }
        | SetChat chat ->
            { model with CurrentChat = chat }  
    type HeaderComponent() =
        inherit ElmishComponent<HeaderComponentModel, HeaderComponentMessage>()
        
        override this.View model dispatch =
            header ["class" => "ui inverted vertical segment"] [
               div ["class" => "ui center aligned grid"] [
                   div ["class" => "twelve wide column"] [
                       nav ["class" => "ui stackable inverted menu"] [
                           div ["class" => "header item"] [
                               a [MainComponent.router.HRef Home] [text "FunCombot"]
                           ]
                           div ["class" => "ui dropdown item" ] [
                               text "Chats"
                               i ["class" => "dropdown icon"] []
                               div ["class" => "menu transition hidden"] [
                                   forEach getUnionCases<ChatName> ^fun (case, name) ->
                                       a ["class" => "item"; on.click ^fun ev ->
                                           if model.CurrentChat <> case then dispatch (ChangeChat(case))] [
                                           text case.DisplayName
                                       ]
                               ]
                           ]
                           div ["class" => "item"] [
                               div ["class" => "text"] [
                                   text model.CurrentChat.DisplayName
                               ]
                           ]
                       ] 
                   ]
               ]     
            ]

module ChatComponent =    
    open LineChartComponent

    type ChatInfoTemplate = Template<"""frontend/templates/chat_info.html""">
    
    type SectionName =
        | Overview
        | Users
        member this.UrlName =
            match this with
            | Overview -> "overview"
            | Users -> "users"
        static member FromString(name: string) =
            match name with
            | "overview" -> Some Overview
            | "users"  -> Some Users
            | _ -> None
            
    type ChatComponentMessage =
        | DoNothing
        | SetSection of SectionName
        | ChangeSection of SectionName
        
    type ChatComponentModel = {
        CurrentSection: SectionName
    }
    
    let update message model =
        match message with
        | DoNothing ->
            model
        | SetSection name ->
            { model with CurrentSection = name }  
        | ChangeSection name ->
            { model with CurrentSection = name }   
    
    type ChatComponent() =
        inherit ElmishComponent<ChatComponentModel, ChatComponentMessage>()
        
        let chatInfo = ChatInfoTemplate()
        
        override this.View model dispatch =
            main ["class" => "ui stackable relaxed centered grid"] [
                div ["class" => "two wide column"] [   
                    div ["class" => "ui hidden divider"] []
                    div ["class" => "ui fluid secondary vertical pointing menu"] [
                        forEach getUnionCases<SectionName>
                            ^fun (case, name) ->
                                a [attr.classes [yield "item"; if model.CurrentSection = case then yield "active";]
                                   on.click ^fun ev ->
                                       if model.CurrentSection <> case then dispatch (ChangeSection(case))] [
                                    text name
                                ]
                    ]
                ]
                div ["class" => "ten wide column"] [
                    cond model.CurrentSection ^fun section ->
                        match section with
                        | Overview ->
                            chatInfo
                                .ActiveCount(string 123)
                                .UsersCountGraph(
                                     ecomp<LineChartComponent,_,_> () ^fun message -> dispatch DoNothing                   
                                )
                                .Elt()
                        | Users ->
                            div [] [
                                h1 ["class" => "ui header"] [text "Users"]
                            ]
                ]
            ]           
            
module MainComponent = 
    open System
    open HeaderComponent
    open ChatComponent
      
    type MainComponentModel = {
        Page: ApplicationPage
        Header: HeaderComponentModel
        Chat: ChatComponentModel
    } 
    
    type MainComponentMessage =
        | DoNothing
        | SetPage of ApplicationPage
        | InitPageFromRouteData of ApplicationPage
        | LogError of exn
        | HeaderComponentMessage of HeaderComponentMessage
        | ChatComponentMessage of ChatComponentMessage
        | Print of string
    
    let router: Router<ApplicationPage, MainComponentModel, MainComponentMessage> =
        Router.infer SetPage (fun m -> m.Page)

    let initModel = {
        Page = Home
        Header = {
            CurrentChat = Dotnetruchat
        }
        Chat = {
            CurrentSection = Overview
        }
    }
    
    let update message model =
        match message with
        | DoNothing ->
            model, []
        | InitPageFromRouteData page ->
            let command = 
                match page with
                | Home ->
                    []
                | Chat(name, section) ->
                    let chatName = Option.defaultValue Dotnetruchat (ChatName.FromString(name))
                    let sectionName = Option.defaultValue Overview (SectionName.FromString(section))
                    Cmd.batch [
                        Cmd.ofMsg (HeaderComponentMessage(SetChat(chatName)));
                        Cmd.ofMsg (ChatComponentMessage(SetSection(sectionName)));
                    ]
            { model with Page = page }, command
        | SetPage page ->
            { model with Page = page }, []
        | LogError e ->
            eprintf "%O" e
            model, []
        | Print name ->
            printfn "%s" name
            model, []
        | ChatComponentMessage message ->
            let command =
               match message with
                | ChangeSection section ->
                    Cmd.ofMsg (SetPage(Chat(model.Header.CurrentChat.UrlName, section.UrlName)))
                | _ ->
                    Cmd.Empty
                
            { model with Chat = ChatComponent.update message model.Chat }, command
        | HeaderComponentMessage message ->
            let command =
               match message with
                | ChangeChat chat ->
                    Cmd.ofMsg (SetPage(Chat(chat.UrlName, model.Chat.CurrentSection.UrlName)))
                | _ ->
                    Cmd.Empty
                
            { model with Header = HeaderComponent.update message model.Header }, command
            
    let view model dispatch =
        concat [
            ecomp<HeaderComponent,_,_> {
                CurrentChat = model.Header.CurrentChat
            } ^fun message -> dispatch (HeaderComponentMessage(message))
            div ["class" => "ui hidden divider"] []
            ecomp<ChatComponent,_,_> {
                CurrentSection = model.Chat.CurrentSection
            } ^fun message -> dispatch (ChatComponentMessage(message))
            footer [ ] [
                
            ]
        ]
    
    type MainComponent() as this =
        inherit ProgramComponent<MainComponentModel, MainComponentMessage>()
                    
        let createInitCommand() =               
            let msg = 
                match this.GetCurrentRoute() with
                | Some(SetPage(page)) ->
                    InitPageFromRouteData(page)
                | Some(_)
                | None ->
                    HeaderComponentMessage(ChangeChat(Dotnetruchat))
                    
            Cmd.ofAsync SemanticUi.initJs () (fun _ -> msg) (fun exn -> LogError exn)
            
        member this.GetCurrentRoute() =
            let path = Uri(this.UriHelper.GetAbsoluteUri()).AbsolutePath.Trim('/')
            let route = router.setRoute path
            printfn "%O" route
            route   
        
        override this.Program =
             Program.mkProgram (fun _ -> initModel, createInitCommand()) update view
             |> Program.withRouter router