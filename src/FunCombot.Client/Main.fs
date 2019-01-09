module rec FunCombot.Client.Main

open Elmish
open Bolero
open System
open Bolero.Html
open FunCombot.Client
open FunCombot.Client.Types
open FunCombot.Client.Javascript
open Bolero.Remoting

module CommonNodes =
    let loadingIcon = i ["class" => "spinner loading icon"] []

    let loadingDiv = div ["class" => "ui active centered loader"] []

[<RequireQualifiedAccess>]
module Identificators =
    let usersChartId = Guid().ToString()

module SeriesChartComponent =
   open Charting
   
   type TimeseriesChartTemplate = Template<"""frontend/templates/timeseries_chart.html""">
          
   let dateToString (date: DateTime) = 
       date.ToString("yyyy-MM-dd")

   let stringToDate (str: string) =
       DateTime.ParseExact(str, "yyyy-MM-dd", null)
   
   type TimeseriesData = list<(DateTime * int)>
   
   type SeriesChartComponentModel =
       { Id: string
         IsLoaded: bool
         FromDateMin: DateTime
         FromDateMax: DateTime
         FromDateValue: DateTime
         ToDateMin: DateTime
         ToDateMax: DateTime
         ToDateValue: DateTime
         Unit: GraphUnit }
     
   type SeriesChartComponentMessage =
       | DoNothing
       | LogError of exn
       | SetDateFrom of DateTime
       | SetDateTo of DateTime
       | SetUnit of GraphUnit
       | UnloadData
       | LoadData of TimeseriesData
   
   let unloadData id = 
        Charting.unloadData id ["x";"users"]

   let loadData id (data: TimeseriesData) =
        let (dates, values) =
            data
            |> List.fold (fun acc (date, value) -> [dateToString date :> obj, value :> obj] @ acc) []
            |> List.unzip
        Charting.loadData id [
            { name = "x"; data = dates };
            { name = "users"; data = values };
        ];

   let update message (model: SeriesChartComponentModel) =
       match message with
       | DoNothing ->
           model, []
       | LogError exn ->
           eprintfn "%O" exn
           model, []
       | SetDateFrom fromDate ->
           { model with FromDateValue = fromDate
                        ToDateMin = fromDate
                        ToDateValue = if fromDate > model.ToDateValue then fromDate.AddMonths(1) else model.ToDateValue }, []                         
       | SetDateTo date ->
           { model with ToDateValue = date }, []
       | SetUnit unitValue ->
           { model with Unit = unitValue }, []
       | LoadData data ->        
           let command =
               let loadDataForModel = loadData model.Id
               Cmd.ofAsync
                   loadDataForModel data
                   (fun _ -> DoNothing)
                   (fun e -> LogError e)
           { model with IsLoaded = true }, command
       | UnloadData ->
           let command =
               Cmd.ofAsync
                   unloadData model.Id
                   (fun _ -> DoNothing)
                   (fun e -> LogError e)
           { model with IsLoaded = false }, command
    
   type SeriesChartComponent<'TMessage>(id: string, elementId: string) =
        inherit ElmishComponent<SeriesChartComponentModel, SeriesChartComponentMessage>()
        
        let configuration = {
            x = "x"
            columns = []
            axis = {
                   x = {
                       ``type`` = "timeseries"
                       tick = {
                            format = "%Y-%m-%d"
                       }
                   }
           }
        }
        let template = TimeseriesChartTemplate()
                  
        let createDateInput labelText value min max dispatch = 
            label[] [
                concat [
                    text labelText
                    input ["type" => "date";
                           attr.value <| dateToString value;
                           attr.min <| dateToString min;
                           attr.max <| dateToString max;
                           on.input ^ fun ev ->
                               let value = ev.Value :?> string
                               let valid = not <| isNullOrWhiteSpace value
                               if valid then dispatch value ]
                ]       
            ]
        
        override this.OnAfterRender() =
            Charting.createChart id elementId configuration
        
        override this.View model dispatch =
            let graph =
                let classes = [yield "ui basic segment"; yield "chart"; if not model.IsLoaded then yield "loading"]
                div [attr.id elementId; attr.classes classes] [
                ]
            
            let units =
                forEach getUnionCases<GraphUnit> ^fun (case, _, tag) ->
                    option [attr.value tag] [text case.Name]
            
            let fromInput =
                createDateInput "From:" model.FromDateValue model.FromDateMin model.FromDateMax ^fun value -> 
                    dispatch (SetDateFrom(stringToDate value))
            
            let toInput =
                createDateInput "To:" model.ToDateValue model.ToDateMin model.ToDateMax ^fun value -> 
                    dispatch (SetDateTo(stringToDate value))
            
            let parseUnitOrDefault unit = 
                Option.defaultValue Week <| GraphUnit.FromString unit

            template
                .FromInput(fromInput)
                .ToInput(toInput)
                .Units(units)
                .SelectedUnit(model.Unit.Name, fun unit -> dispatch (SetUnit(parseUnitOrDefault unit)))
                .Graph(graph)
                .Elt()
        
        override this.Finalize() =
            Charting.destroyChart id
        
module UserDataComponent =
    open FunCombot.Client.Remoting.Chat
    open SeriesChartComponent
    
    let getDefaultModel id = 
        let now = DateTime.Now
        let dateFrom = new DateTime(now.Year, now.Month, 1)
        let dateTo = new DateTime(now.Year, now.Month + 1, 1)
           
        { Id = id
          IsLoaded = false
          FromDateMin = dateFrom
          FromDateMax = stringToDate "2030-01-01" 
          FromDateValue = dateFrom 
          ToDateMin = dateTo
          ToDateMax = stringToDate "2030-01-01" 
          ToDateValue = dateTo
          Unit = Week }
    
    type UserDataComponentMessage = 
        | LogError of exn
        | LoadChartDataFromService of Chat        
        | SeriesChartComponentMessage of SeriesChartComponentMessage
    
    let update (provider: IRemoteServiceProvider) messageUpdateFn message model =       
        let chatDataService = provider.GetService<ChatDataService>()
        match message with
        | LogError exn ->
            eprintfn "%O" exn
            model, []
        | LoadChartDataFromService chat ->
            model,
            Cmd.batch [
                Cmd.ofMsg (SeriesChartComponentMessage UnloadData)
                Cmd.ofAsync 
                    chatDataService.GetUserCount {
                        Chat = chat 
                        From = model.FromDateValue
                        To = model.ToDateValue
                        Unit = model.Unit
                    }
                    (fun data ->
                        data
                        |> List.map ^ fun item -> item.Date, item.Count
                        |> (LoadData >> SeriesChartComponentMessage))
                    (fun exn -> LogError exn)
            ]
        | SeriesChartComponentMessage seriesMessage ->
           let (newModel, commands) = messageUpdateFn seriesMessage model
           newModel, Cmd.convertSubs SeriesChartComponentMessage commands   
    
    type UserDataComponent() =
        inherit SeriesChartComponent<UserDataComponentMessage>(Identificators.usersChartId, "user_data")
         

module HeaderComponent =   
    type HeaderTemplate = Template<"""frontend/templates/header.html""">
        
    type HeaderComponentMessage =
        | ChangeChat of Chat
    
    type HeaderComponentModel = {
        CurrentChat: Chat
    }
    
    let headerTemplate = HeaderTemplate()
    
    let update message model =
        match message with
        | ChangeChat chat ->
            { model with CurrentChat = chat }
            
    type HeaderComponent() =
        inherit ElmishComponent<HeaderComponentModel, HeaderComponentMessage>()
        
        override this.View model dispatch =
            let dropDown =
                forEach getUnionCases<Chat> ^fun (case, name, _) ->
                    a [ attr.classes [yield "item"; if model.CurrentChat = case then yield "active selected"];
                        on.click ^fun ev ->
                            if model.CurrentChat <> case then dispatch (ChangeChat case)] [
                        text case.DisplayName
                    ]
            headerTemplate
                .HeaderItem(text "StatsBot")
                .DropdownItems(dropDown)
                .ChatName(text model.CurrentChat.DisplayName)
                .Elt()

module ChatComponent =
    [<AutoOpen>]
    module OverviewComponent = 
        open UserDataComponent
        open SeriesChartComponent

        [<AutoOpen>]
        module DescriptionComponent = 
            open FunCombot.Client.Remoting.Chat

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
                | LoadDescriptionData of Chat
                
            let update (provider: IRemoteServiceProvider) message model =
                let chatDataService = provider.GetService<ChatDataService>()
                match message with
                | LogError exn ->
                     eprintfn "%O" exn
                     model, []
                | SetDescription description ->
                     { model with Data = description }, []
                | LoadDescriptionData chat ->
                    model,
                    Cmd.batch [
                        SetDescription(NotLoaded) |> Cmd.ofMsg;
                        Cmd.ofAsync 
                            chatDataService.GetChatData (chat)
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
                        template
                            .Description(pre [] [text description.Description])
                            .ActiveUsers(span [] [text <| string description.ActiveUsers])
                            .TotalUsers(span [] [text <| string description.TotalUsers])
                            .Change(span [] [text <| string description.ChangeInTotalUsersForWeek])
                            .Elt()
        
        type ChatOverviewTemplate = Template<"""frontend/templates/chat_overview.html""">

        type OverviewComponentModel = {
            UserData: SeriesChartComponentModel
            Description: DescriptionModel
        }
        
        type OverviewComponentMessage =
            | ChartComponentMessage of UserDataComponentMessage
            | DescriptionComponentMessage of DescriptionComponentMessage
            | LoadOverviewData of Chat
        
        let update (provider: IRemoteServiceProvider) message model =
            let messageUpdate message model = SeriesChartComponent.update message model        
            match message with
            | ChartComponentMessage message ->
                let (newModel, commands) = UserDataComponent.update provider messageUpdate message model.UserData
                { model with UserData = newModel }, convertSubs ChartComponentMessage commands
            | DescriptionComponentMessage message ->
                let (newModel, commands) = DescriptionComponent.update provider message model.Description
                { model with Description = newModel }, convertSubs DescriptionComponentMessage commands           
            | LoadOverviewData chat ->
                model, Cmd.batch [
                    ChartComponentMessage(LoadChartDataFromService chat) |> Cmd.ofMsg
                    DescriptionComponentMessage(LoadDescriptionData chat) |> Cmd.ofMsg
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
                            dispatch (ChartComponentMessage(SeriesChartComponentMessage message))                   
                    )
                    .Elt()
    
    type MainTemplate = Template<"""frontend/templates/main.html""">
    
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
        | OverviewComponentMessage of OverviewComponentMessage
        | ChangeSection of SectionName
        
    type ChatComponentModel = {
        CurrentSection: SectionName
        Overview: OverviewComponentModel
    }
    
    let update (provider: IRemoteServiceProvider) message model =
        match message with
        | ChangeSection name ->
            { model with CurrentSection = name }, []
        | OverviewComponentMessage message ->
            let (newModel, commands) = OverviewComponent.update provider message model.Overview
            { model with Overview = newModel }, Cmd.convertSubs OverviewComponentMessage commands      
    
    type ChatComponent() =
        inherit ElmishComponent<ChatComponentModel, ChatComponentMessage>()
        
        let mainTemplate = MainTemplate()
        
        override this.View model dispatch =
            let menu =
                forEach getUnionCases<SectionName> ^fun (case, name, _) ->
                    a [attr.classes [yield "item"; if model.CurrentSection = case then yield "active";]
                       on.click ^fun _ ->
                           if model.CurrentSection <> case then dispatch (ChangeSection(case))] [
                        text name
                    ]       
            
            let content =
                cond model.CurrentSection ^fun section ->
                    match section with
                    | Overview ->
                        ecomp<OverviewComponent,_,_> model.Overview ^fun message -> 
                            dispatch (OverviewComponentMessage(message))
                    | Users ->
                        div [] [
                            h1 ["class" => "ui header"] [text "Users"]
                        ]
                        
            mainTemplate
                .SectionMenu(menu)
                .Content(content)
                .Elt()
            
module MainComponent = 
    open HeaderComponent
    open ChatComponent
    open FunCombot.Client.Remoting.Chat
    
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
    
    let router: Router<ApplicationPage, MainComponentModel, MainComponentMessage> =
        Router.infer SetPage (fun m -> m.Page)
        
    let overviewMessage message = 
        ChatComponentMessage(OverviewComponentMessage(message))
        
    let update (provider: IRemoteServiceProvider) =
        fun message (model: MainComponentModel) ->
            match message with
            | DoNothing ->
                model, []
            | LogError e ->
                eprintf "%O" e
                model, []
            | InitPage -> 
                let command =
                    match model.Chat.CurrentSection with
                    | Overview ->
                        Cmd.ofMsg <| overviewMessage (LoadOverviewData model.Header.CurrentChat)
                    | _ -> []
                model, command
            | SetPage page ->
                { model with Page = page }, []
            | ChatComponentMessage message ->
                let sectionChangeCommand =
                   match message with
                    | ChangeSection section ->
                        match section with
                        | Overview ->
                            Cmd.batch [
                                Cmd.ofMsg <| overviewMessage (LoadOverviewData model.Header.CurrentChat)
                                Cmd.ofMsg (SetPage(Chat(model.Header.CurrentChat.UrlName, section.UrlName)))
                            ]
                        | _ -> 
                            Cmd.ofMsg (SetPage(Chat(model.Header.CurrentChat.UrlName, section.UrlName)))
                    | _ -> []
                let (newModel, commands) = ChatComponent.update provider message model.Chat
                let command =
                    Cmd.batch [
                        sectionChangeCommand
                        Cmd.convertSubs ChatComponentMessage commands
                    ]
                { model with Chat = newModel }, command
            | HeaderComponentMessage message ->
                let command =
                   match message with
                    | ChangeChat chat ->
                        let loadCommand = 
                            match model.Chat.CurrentSection with
                            | Overview ->
                                Cmd.ofMsg <| overviewMessage (LoadOverviewData model.Header.CurrentChat)
                            | _ -> []
                        Cmd.batch [
                            loadCommand
                            Cmd.ofMsg (SetPage(Chat(chat.UrlName, model.Chat.CurrentSection.UrlName)))
                        ]              
                { model with Header = HeaderComponent.update message model.Header }, command
            
    let view model dispatch =
        let header =
            ecomp<HeaderComponent,_,_> {
                CurrentChat = model.Header.CurrentChat
            } ^fun message -> dispatch (HeaderComponentMessage message)
        let chatInfo =
            ecomp<ChatComponent,_,_> {
                CurrentSection = model.Chat.CurrentSection
                Overview = model.Chat.Overview
            } ^fun message -> dispatch (ChatComponentMessage message)
            
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
                (Chat(Fsharpchat.UrlName, Overview.UrlName)), Fsharpchat, Overview

        let createInitModel () =                             
            let (page, chatName, sectionName) = getRouteData()            
            {
                Page = page
                Header = {
                    CurrentChat = chatName
                }
                Chat = {
                    CurrentSection = sectionName
                    Overview = {
                        UserData = UserDataComponent.getDefaultModel Identificators.usersChartId
                        Description = {
                            Data = NotLoaded
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