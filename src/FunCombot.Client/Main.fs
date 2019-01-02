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

    type LineChartComponent() =
        inherit ElmishComponent<unit, unit>()
        
        override this.View model dispatch =
            concat [
                div ["class" => "ui segment"] [            
                    div [attr.id "test"] []
                    button [ on.click (fun e -> Charting.createChart "test" {
                        columns = [
                          ["data1"; 30; 200; 100; 400; 150; 250];
                          ["data2"; 50; 20; 10; 40; 15; 25];           
                        ]
                    })] [text "Elo"]
                ]
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
    
    type HeaderComponentMessage =
        | ChangeChat of ChatName
    
    type HeaderComponentModel = {
        CurrentChat: ChatName;
    }
    
    let update message model =
        match message with
        | ChangeChat(chat) ->
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
    type ChatInfoTemplate = Template<"""frontend/templates/chat_info.html""">
    
    type SectionName =
        | Overview
        | Users
        member this.UrlName =
            match this with
            | Overview -> "overview"
            | Users -> "users"
        
    type ChatComponentMessage =
        | ChangeSection of SectionName
        
    type ChatComponentModel = {
        CurrentSection: SectionName
    }
    
    let update message model =
        match message with
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
                                .ActiveGraph(div [] [text "Eh"])
                                .Elt()
                        | Users ->
                            div [] [
                                h1 ["class" => "ui header"] [text "Users"]
                            ]
                ]
            ]           
            
module MainComponent = 
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
            { model with Chat = ChatComponent.update message model.Chat }, command
        | HeaderComponentMessage message ->
            let command =
               match message with
                | ChangeChat chat ->
                    Cmd.ofMsg (SetPage(Chat(chat.UrlName, model.Chat.CurrentSection.UrlName))) 
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
    
    type MainComponent() =
        inherit ProgramComponent<MainComponentModel, MainComponentMessage>()
        
        let initCommand =
            Cmd.ofAsync
                SemanticUi.initJs ()
                (fun _ -> HeaderComponentMessage(ChangeChat(Dotnetruchat)))
                (fun exn -> LogError exn)          
        
        override this.Program =
             Program.mkProgram (fun _ -> initModel, initCommand) update view
             |> Program.withRouter router 
