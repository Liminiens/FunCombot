module StatsBot.Client.Components.Main

open Elmish
open Bolero
open System
open Bolero.Html
open StatsBot.Client
open StatsBot.Client.Types
open StatsBot.Client.Javascript
open StatsBot.Client.Components
open StatsBot.Client.Components.Charting.UserDataComponent
open StatsBot.Client.Components.Header
open StatsBot.Client.Components.Header.HeaderComponent
open StatsBot.Client.Components.Chat
open StatsBot.Client.Components.Chat.UsersComponent
open StatsBot.Client.Components.Chat.ChatComponent
open Bolero.Remoting
            
module MainComponent = 
    open StatsBot.Client.Components.Charting
    
    type RootTemplate = Template<"frontend/templates/root.html">
      
    type MainComponentModel = {
        Page: ApplicationPage
        Header: HeaderComponentModel
        Chat: ChatComponentModel
    } 
    
    type MainComponentMessage =
        | DoNothing
        | InitPage
        | LogError of exn
        | SetPage of ApplicationPage
        | HeaderComponentMessage of HeaderComponentMessage
        | ChatComponentMessage of ChatComponentMessage
    
    let rootTemplate = RootTemplate()
    
    let router = Router.infer SetPage (fun m -> m.Page)
    
    let usersMessage message = 
        ChatComponentMessage(UsersComponentMessage(message))
        
    let overviewMessage message = 
        ChatComponentMessage(OverviewComponentMessage(message))
        
    let update (provider: IRemoteServiceProvider) =
        fun message model ->
            match message with
            | DoNothing ->
                model, []
            | LogError e ->
                eprintf "%O" e
                model, []
            | InitPage -> 
                let dataLoadCommand =
                    match model.Chat.CurrentSection with
                    | Overview ->
                        Cmd.ofMsg <| overviewMessage (LoadOverviewData model.Header.Chat)
                    | Users ->
                        Cmd.ofMsg <| usersMessage LoadTableData
                model, dataLoadCommand
            | SetPage page ->
                { model with Page = page }, []
            | ChatComponentMessage message ->
                let sectionChangeCommand =
                   match message with
                   | ChangeSection section ->
                        Cmd.batch [
                            yield Cmd.ofMsg <| SetPage(Chat(model.Header.Chat.UrlName, section.UrlName))
                            yield
                                match section with
                                | Overview ->
                                    Cmd.ofMsg <| overviewMessage (LoadOverviewData model.Header.Chat)
                                | Users ->
                                    Cmd.ofMsg <| usersMessage LoadTableData
                        ]
                   | _ -> []
                let (newModel, commands) = ChatComponent.update provider message model.Chat
                let command =
                    Cmd.batch [
                        sectionChangeCommand
                        Cmd.wrapAndBatchSub ChatComponentMessage commands
                    ]
                { model with Chat = newModel }, command
            | HeaderComponentMessage message ->
                let command =
                   match message with
                    | ChangeChat chat ->
                        let loadCommand = 
                            match model.Chat.CurrentSection with
                            | Overview ->
                                Cmd.batch [
                                    Cmd.ofMsg <| overviewMessage (UserDataComponentMessage(SetUserChartChat chat))
                                    Cmd.ofMsg <| overviewMessage (LoadOverviewData chat)
                                ]
                            | Users ->
                                Cmd.ofMsg <| usersMessage (SetUsersInfoChat chat)
                        Cmd.batch [
                            Cmd.ofMsg (SetPage(Chat(chat.UrlName, model.Chat.CurrentSection.UrlName)))
                            loadCommand
                        ]              
                { model with Header = HeaderComponent.update message model.Header }, command
            
    let view model dispatch =
        let header =
            ecomp<HeaderComponent,_,_> model.Header ^fun message ->
                dispatch (HeaderComponentMessage message)
        let chatInfo =
            ecomp<ChatComponent,_,_> model.Chat ^fun message ->
                dispatch (ChatComponentMessage message)
            
        rootTemplate
            .Header(header)
            .ChatInfo(chatInfo)
            .Elt()
    
    type MainComponent() as this =
        inherit ProgramComponent<MainComponentModel, MainComponentMessage>()
        
        let getCurrentRoute() =
            let path = Uri(this.UriHelper.GetAbsoluteUri()).AbsolutePath.Trim('/')
            let route = router.setRoute path
            route
        
        let getRouteData() =
            match getCurrentRoute() with
            | Some(SetPage(page & Chat(chat, section))) ->
                let chatName = Option.defaultValue Fsharpchat (Chat.FromString(chat))
                let sectionName = Option.defaultValue Overview (SectionName.FromString(section))
                page, chatName, sectionName
            | _ ->
                Chat(Fsharpchat.UrlName, Overview.UrlName), Fsharpchat, Overview

        let createInitModel () =                             
            let (page, chatName, sectionName) = getRouteData()            
            {
                Page = page
                Header = {
                    Chat = chatName
                }
                Chat = {
                    CurrentSection = sectionName
                    Overview = {
                        UserData = {
                            Series = UserDataComponent.getDefaultModel
                            Chat = chatName
                        }
                        Description = {
                            Data = NotLoaded
                        }
                    }
                    Users = {
                        Chat = chatName
                        Users = NotLoaded
                        Page = {
                            PageSize = 50
                            Total = 0
                            Current = 0
                            PageNumber = 1
                        }
                    }
                }
            }
                    
        let createInitCommand() = 
            Cmd.batch [            
                Cmd.ofAsync SemanticUi.initJs () (fun _ -> DoNothing) (fun exn -> LogError exn)
                Cmd.ofMsg InitPage
            ]

        override this.Program =
            let provider =
                { new IRemoteServiceProvider with
                    member __.GetService<'T when 'T :> IRemoteService> () = this.Remote<'T>() }

            Program.mkProgram (fun _ -> createInitModel(), createInitCommand()) (update provider) view
            |> Program.withRouter router