namespace StatsBot.Client.Components.Chat

open Elmish
open Bolero
open Bolero.Html
open StatsBot.Client
open StatsBot.Client.Types
open StatsBot.Client.Components

module UsersComponent =
    open StatsBot.Client.Remoting.Chat

    type TableTemplate = Template<"""frontend/templates/users_table.html""">
    
    type UsersComponentMessage =
        | LogError of exn
        | SetUsersInfoChat of Chat
        | LoadTableData
        | SetTableData of ChatUser list * ChatUserPage
    
    type UsersComponentModel = {
        Chat: Chat
        Page: ChatUserPage
        Users: DynamicModel<ChatUser list>
    }
    
    let update (provider: IRemoteServiceProvider) =
        let chatDataService = provider.GetService<ChatDataService>()
        fun message model ->
            match message with
            | LogError e ->
                eprintf "%O" e
                model, []
            | SetTableData(users, page) ->
                { model with Users = Model users; Page = page },[]
            | SetUsersInfoChat chat ->
                { model with Chat = chat }, []
            | LoadTableData ->
                model,
                    Cmd.ofAsync 
                        chatDataService.GetChatUsers (model.Chat, model.Page)
                        (fun data -> SetTableData data)
                        (fun exn -> LogError exn)
    
    type UsersComponent() =
        inherit ElmishComponent<UsersComponentModel, UsersComponentMessage>()
        
        let createUserRow (model: ChatUser) =
            TableTemplate
                .User()
                .FirstName(model.FirstName)
                .LastName(model.LastName)
                .Username(model.Username)
                .MessageCount(string model.MessageCount)
                .StickerCount(string model.StickersCount)
                .MediaCount(string model.MediaCount)
                .Elt()
        
        override this.View model dispatch =
            match model.Users with
            | Model users ->
                let table =
                    TableTemplate.UsersTable()
                        .Users(forEach users createUserRow)
                        .Elt()
                TableTemplate()
                    .Content(table)
                    .Elt()
            | NotLoaded ->
                TableTemplate()
                    .Content(CommonNodes.loadingDiv)
                    .Elt()

module ChatComponent =
    open UsersComponent

    [<AutoOpen>]
    module DescriptionComponent = 
        open StatsBot.Client.Remoting.Chat
    
        type DescriptionTemplate = Template<"""frontend/templates/chat_overview_description.html""">
    
        type Description = 
            { Description: string
              TotalUsers: int32
              ActiveUsers: int32
              ChangeInTotalUsersForWeek: int32 }
            static member FromServiceData(data: ChatData) = 
                { Description = data.Description
                  TotalUsers = data.TotalUsers
                  ActiveUsers = data.ActiveInThreeDays
                  ChangeInTotalUsersForWeek = data.ChangeInTotalUsersForWeek }
    
        type DescriptionModel = {
            Data: DynamicModel<Description>
        }
    
        type DescriptionComponentMessage =
            | LogError of exn
            | SetDescription of DynamicModel<Description>
            | LoadDescriptionDataFromService of Chat
            
        let update (provider: IRemoteServiceProvider) =
            let chatDataService = provider.GetService<ChatDataService>()
            fun message model ->
                match message with
                | LogError exn ->
                     eprintfn "%O" exn
                     model, []
                | SetDescription description ->
                     { model with Data = description }, []
                | LoadDescriptionDataFromService chat ->
                    model,
                    Cmd.batch [
                        SetDescription(NotLoaded) |> Cmd.ofMsg
                        Cmd.ofAsync 
                            chatDataService.GetChatData chat
                            (fun data ->
                                Description.FromServiceData(data)
                                |> (Model >> SetDescription)) 
                            (fun exn -> LogError exn)
                    ]
    
        type DescriptionComponent() =       
            inherit ElmishComponent<DescriptionModel, DescriptionComponentMessage>()
            
            let template = DescriptionTemplate()
    
            override this.View model dispatch =
                match model.Data with
                | NotLoaded ->
                    template
                        .Description(CommonNodes.loadingDiv)
                        .ActiveUsers(CommonNodes.loadingIcon)
                        .TotalUsers(CommonNodes.loadingIcon)
                        .Change(CommonNodes.loadingIcon)
                        .Elt()
                | Model description ->
                    let changeNode =
                        let classes = if description.ChangeInTotalUsersForWeek <= 0 then "count-change-minus" else "count-change-plus"
                        span ["class" => classes] [
                            description.ChangeInTotalUsersForWeek
                            |> (string >> text)
                        ]
                    template
                        .Description(pre [] [text description.Description])
                        .ActiveUsers(span [] [text <| string description.ActiveUsers])
                        .TotalUsers(span [] [text <| string description.TotalUsers])
                        .Change(changeNode)
                        .Elt()
                        
    [<AutoOpen>]
    module OverviewComponent = 
        open DescriptionComponent
        open StatsBot.Client.Components.Charting
        open StatsBot.Client.Components.Charting.UserDataComponent
        
        type ChatOverviewTemplate = Template<"""frontend/templates/chat_overview.html""">
    
        type OverviewComponentModel = {
            UserData: UserDataComponentModel
            Description: DescriptionModel
        }
        
        type OverviewComponentMessage =
            | UserDataComponentMessage of UserDataComponentMessage
            | DescriptionComponentMessage of DescriptionComponentMessage
            | LoadOverviewData of Chat
        
        let update (provider: IRemoteServiceProvider) message model =
            match message with
            | UserDataComponentMessage message ->
                let (newModel, commands) = UserDataComponent.update provider message model.UserData
                { model with UserData = newModel }, wrapAndBatchSub UserDataComponentMessage commands
            | DescriptionComponentMessage message ->
                let (newModel, commands) = DescriptionComponent.update provider message model.Description
                { model with Description = newModel }, wrapAndBatchSub DescriptionComponentMessage commands           
            | LoadOverviewData chat->
                model, Cmd.batch [
                    UserDataComponentMessage(LoadUserChartSettings) |> Cmd.ofMsg
                    DescriptionComponentMessage(LoadDescriptionDataFromService chat) |> Cmd.ofMsg
                ]
            
        type OverviewComponent() =       
            inherit ElmishComponent<OverviewComponentModel, OverviewComponentMessage>()
            
            let chatOverviewTemplate = ChatOverviewTemplate()
            
            override this.View model dispatch =
                chatOverviewTemplate
                    .Description(
                        ecomp<DescriptionComponent,_,_> model.Description ^fun message -> 
                            dispatch (DescriptionComponentMessage message) 
                    )
                    .UsersCountGraph(
                        ecomp<UserDataComponent,_,_> model.UserData ^fun message -> 
                            dispatch (UserDataComponentMessage message)                   
                    )
                    .Elt()
    
    type ChatMainTemplate = Template<"""frontend/templates/main.html""">
    
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
        | UsersComponentMessage of UsersComponentMessage
        | OverviewComponentMessage of OverviewComponentMessage
        | ChangeSection of SectionName
        
    type ChatComponentModel = {
        CurrentSection: SectionName
        Overview: OverviewComponentModel
        Users: UsersComponentModel
    }
    
    let update (provider: IRemoteServiceProvider) message model =
        match message with
        | ChangeSection name ->
            { model with CurrentSection = name }, []
        | UsersComponentMessage message ->
            let (newModel, commands) = UsersComponent.update provider message model.Users
            { model with Users = newModel }, Cmd.wrapAndBatchSub UsersComponentMessage commands
        | OverviewComponentMessage message ->
            let (newModel, commands) = OverviewComponent.update provider message model.Overview
            { model with Overview = newModel }, Cmd.wrapAndBatchSub OverviewComponentMessage commands      
    
    type ChatComponent() =
        inherit ElmishComponent<ChatComponentModel, ChatComponentMessage>()
        
        let chatMainTemplate = ChatMainTemplate()
        
        override this.View model dispatch =
            let menu =
                forEach getUnionCases<SectionName> ^fun (case, name, _) ->
                    a [attr.classes [yield "item"; if model.CurrentSection = case then yield "active";]
                       on.click ^fun _ ->
                           if model.CurrentSection <> case then dispatch (ChangeSection case)] [
                        text name
                    ]       
            
            let content =
                cond model.CurrentSection ^fun section ->
                    match section with
                    | Overview ->
                        ecomp<OverviewComponent,_,_> model.Overview ^fun message -> 
                            dispatch (OverviewComponentMessage message)
                    | Users ->
                        ecomp<UsersComponent,_,_> model.Users ^fun message -> 
                            dispatch (UsersComponentMessage message)
                        
            chatMainTemplate
                .SectionMenu(menu)
                .Content(content)
                .Elt()