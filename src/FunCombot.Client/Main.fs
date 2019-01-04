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
    open Bolero.Html
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
                div [attr.id "test"; attr.classes ["chart"]] [
                    div ["class" => "ui active centered loader"] []
                ]
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
    
    type HeaderTemplate = Template<"""frontend/templates/header.html""">
        
    type HeaderComponentMessage =
        | SetChat of ChatName
        | ChangeChat of ChatName
    
    type HeaderComponentModel = {
        CurrentChat: ChatName;
    }
    
    let headerTemplate = HeaderTemplate()
    
    let update message model =
        match message with
        | ChangeChat chat ->
            { model with CurrentChat = chat }
        | SetChat chat ->
            { model with CurrentChat = chat }
            
    type HeaderComponent() =
        inherit ElmishComponent<HeaderComponentModel, HeaderComponentMessage>()
        
        override this.View model dispatch =
            let dropDown =
                forEach getUnionCases<ChatName> ^fun (case, name) ->
                    a ["class" => "item"; on.click ^fun ev ->
                        if model.CurrentChat <> case then dispatch (ChangeChat(case))] [ text case.DisplayName ]
            headerTemplate
                .HeaderItem(text "FunCombot")
                .DropdownItems(dropDown)
                .ChatName(text model.CurrentChat.DisplayName)
                .Elt()

module ChatComponent =    
    open LineChartComponent

    type MainTemplate = Template<"""frontend/templates/main.html""">
    type ChatOverviewTemplate = Template<"""frontend/templates/chat_overview.html""">
    
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
        
        let chatOverviewTemplate = ChatOverviewTemplate()
        let mainTemplate = MainTemplate()
        
        override this.View model dispatch =
            let menu =
                forEach getUnionCases<SectionName> ^fun (case, name) ->
                    a [attr.classes [yield "item"; if model.CurrentSection = case then yield "active";]
                       on.click ^fun ev ->
                           if model.CurrentSection <> case then dispatch (ChangeSection(case))] [
                        text name
                    ]       
            
            let chatOverview =
                chatOverviewTemplate
                    .UsersCountGraph(
                         ecomp<LineChartComponent,_,_> () ^fun message -> dispatch DoNothing                   
                    )
                    .Elt()
            
            let content =
                cond model.CurrentSection ^fun section ->
                    match section with
                    | Overview ->
                        chatOverview
                    | Users ->
                        div [] [
                            h1 ["class" => "ui header"] [text "Users"]
                        ]
                        
            mainTemplate.SectionMenu(menu).Content(content).Elt()
            
module MainComponent = 
    open System
    open HeaderComponent
    open ChatComponent
    
    type RootTemplate = Template<"frontend/templates/root.html">
      
    type MainComponentModel = {
        Page: ApplicationPage
        Header: HeaderComponentModel
        Chat: ChatComponentModel
    } 
    
    type MainComponentMessage =
        | DoNothing
        | LogError of exn
        | SetPage of ApplicationPage
        | HeaderComponentMessage of HeaderComponentMessage
        | ChatComponentMessage of ChatComponentMessage
    
    let rootTemplate = RootTemplate()
    
    let router: Router<ApplicationPage, MainComponentModel, MainComponentMessage> =
        Router.infer SetPage (fun m -> m.Page)
    
    let update message model =
        match message with
        | DoNothing ->
            model, []
        | SetPage page ->
            { model with Page = page }, []
        | LogError e ->
            eprintf "%O" e
            model, []
        | ChatComponentMessage message ->
            let command =
               match message with
                | ChangeSection section ->
                    Cmd.ofMsg (SetPage(Chat(model.Header.CurrentChat.UrlName, section.UrlName)))
                | _ ->
                    []
                
            { model with Chat = ChatComponent.update message model.Chat }, command
        | HeaderComponentMessage message ->
            let command =
               match message with
                | ChangeChat chat ->
                    Cmd.ofMsg (SetPage(Chat(chat.UrlName, model.Chat.CurrentSection.UrlName)))
                | _ ->
                    []
                
            { model with Header = HeaderComponent.update message model.Header }, command
            
    let view model dispatch =
        let header =
            ecomp<HeaderComponent,_,_> {
                CurrentChat = model.Header.CurrentChat
            } ^fun message -> dispatch (HeaderComponentMessage(message))
        let chatInfo =
            ecomp<ChatComponent,_,_> {
                CurrentSection = model.Chat.CurrentSection
            } ^fun message -> dispatch (ChatComponentMessage(message))
            
        rootTemplate
            .Header(header)
            .ChatInfo(chatInfo)
            .Elt()
    
    type MainComponent() as this =
        inherit ProgramComponent<MainComponentModel, MainComponentMessage>()
        
        let getCurrentRoute() =
            let path = Uri(this.UriHelper.GetAbsoluteUri()).AbsolutePath.Trim('/')
            let route = router.setRoute path
            printfn "%O" route
            route
        
        let createInitModel () =                             
            let (page, chatName, sectionName) =
                match getCurrentRoute() with
                | Some(SetPage(Chat(chat, section) as page)) ->
                    let chatName = Option.defaultValue Dotnetruchat (ChatName.FromString(chat))
                    let sectionName = Option.defaultValue Overview (SectionName.FromString(section))
                    page, chatName, sectionName
                | _ ->
                    (Chat(Dotnetruchat.UrlName, Overview.UrlName)), Dotnetruchat, Overview
                
            {
                Page = page
                Header = {
                    CurrentChat = chatName
                }
                Chat = {
                    CurrentSection = sectionName
                }
            }
                    
        let createInitCommand() =                      
            Cmd.ofAsync SemanticUi.initJs () (fun _ -> DoNothing) (fun exn -> LogError exn)                    
        
        override this.Program =
             Program.mkProgram (fun _ -> createInitModel(), createInitCommand()) update view
             |> Program.withRouter router