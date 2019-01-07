module rec FunCombot.Client.Main

open Elmish
open Bolero
open System
open Bolero.Html
open FunCombot.Client
open FunCombot.Client.Types
open FunCombot.Client.Javascript
open Bolero.Remoting

type IRemoteServiceProvider = 
    abstract GetService<'T when 'T :> IRemoteService> : unit -> 'T

type ApplicationPage =
    | [<EndPoint("/")>]
      Home
    | [<EndPoint("/chat")>]
      Chat of name: string * section: string    

module SeriesChartComponent =
   open Charting
   
   type TimeseriesChartTemplate = Template<"""frontend/templates/timeseries_chart.html""">
   
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
        
   let dateToString (date: DateTime) = 
       date.ToString("yyyy-MM-dd")

   let stringToDate (str: string) =
       DateTime.ParseExact(str, "yyyy-MM-dd", null)
        
   type SeriesChartComponentModel =
       { FromDateMin: DateTime
         FromDateMax: DateTime
         FromDateValue: DateTime
         ToDateMin: DateTime
         ToDateMax: DateTime
         ToDateValue: DateTime
         Unit: GraphUnit }
       
       static member Default =
           let now = DateTime.Now
           let dateFrom = new DateTime(now.Year, now.Month, 1)
           let dateTo = new DateTime(now.Year, now.Month + 1, 1)
           
           { FromDateMin = dateFrom
             FromDateMax = stringToDate "2030-01-01" 
             FromDateValue = dateFrom 
             ToDateMin = dateTo
             ToDateMax = stringToDate "2030-01-01" 
             ToDateValue = dateTo
             Unit = Week }
     
   type SeriesChartComponentMessage<'TMessage> =
       | SetDateFrom of DateTime
       | SetDateTo of DateTime
       | SetUnit of GraphUnit
       | Message of 'TMessage
   
   let update messageUpdateFn message model =
       match message with
       | SetDateFrom fromDate ->
           { model with FromDateValue = fromDate
                        ToDateMin = fromDate
                        ToDateValue = if fromDate > model.ToDateValue then fromDate.AddMonths(1) else model.ToDateValue }                         
       | SetDateTo date ->
           { model with ToDateValue = date }
       | SetUnit unitValue ->
           { model with Unit = unitValue }
       | Message tMessage ->
           messageUpdateFn tMessage model
    
   type SeriesChartComponent<'TMessage>(elementId: string, configuration: IChartConfiguration) =
        inherit ElmishComponent<SeriesChartComponentModel, SeriesChartComponentMessage<'TMessage>>()
        
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
            Charting.createChart elementId configuration
        
        override this.View model dispatch =
            let graph =
                div [attr.id elementId; attr.classes ["chart"]] [
                    div ["class" => "ui active centered loader"] []
                ]
            
            let units =
                forEach getUnionCases<GraphUnit> ^fun (case, _, tag) ->
                    option [ attr.value tag ] [text case.Name]
            
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

module UserDataComponent =
    open Charting
    open SeriesChartComponent
    
    let chartData = {
        x = "x";
        columns = [
            { name = "x"; data = ["2013-01-01"; "2013-01-02"; "2013-01-03"; "2013-01-04"; "2013-01-05"; "2013-01-06"] };
            { name = "users"; data = [30; 200; 100; 400; 150; 250] };
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
    
    type UserDataComponent() =
        inherit SeriesChartComponent<unit>("test", chartData)
         

module HeaderComponent =   
    type HeaderTemplate = Template<"""frontend/templates/header.html""">
        
    type HeaderComponentMessage =
        | SetChat of ChatName
        | ChangeChat of ChatName
    
    type HeaderComponentModel = {
        CurrentChat: ChatName
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
                forEach getUnionCases<ChatName> ^fun (case, name, _) ->
                    a ["class" => "item"; on.click ^fun ev ->
                        if model.CurrentChat <> case then dispatch (ChangeChat(case))] [
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
        
        type ChatOverviewTemplate = Template<"""frontend/templates/chat_overview.html""">
        
        type OverviewComponentModel = {
            UserData: SeriesChartComponentModel
        }
        
        type OverviewComponentMessage =
            | ChartMessage of SeriesChartComponentMessage<unit>
        
        let update message model =
            let messageUpdate message model = model            
            match message with
            | ChartMessage message ->
                { model with UserData = SeriesChartComponent.update messageUpdate message model.UserData }
            
        type OverviewComponent() =       
            inherit ElmishComponent<OverviewComponentModel, OverviewComponentMessage>()
            
            let chatOverviewTemplate = ChatOverviewTemplate()
            
            override this.View model dispatch =
                chatOverviewTemplate
                    .UsersCountGraph(
                         ecomp<UserDataComponent,_,_> model.UserData ^fun message -> dispatch (ChartMessage(message))                   
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
        | DoNothing
        | OverviewComponentMessage of OverviewComponentMessage
        | SetSection of SectionName
        | ChangeSection of SectionName
        
    type ChatComponentModel = {
        CurrentSection: SectionName
        Overview: OverviewComponentModel
    }
    
    let update message model =
        match message with
        | DoNothing ->
            model
        | SetSection name ->
            { model with CurrentSection = name }  
        | ChangeSection name ->
            { model with CurrentSection = name }
        | OverviewComponentMessage message ->
            { model with Overview = OverviewComponent.update message model.Overview }      
    
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
                        ecomp<OverviewComponent,_,_> model.Overview ^fun message -> dispatch (OverviewComponentMessage(message))
                    | Users ->
                        div [] [
                            h1 ["class" => "ui header"] [text "Users"]
                        ]
                        
            mainTemplate.SectionMenu(menu).Content(content).Elt()
            
module MainComponent = 
    open HeaderComponent
    open ChatComponent
    open SeriesChartComponent
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
    
    let update (remoteServiceProvider: IRemoteServiceProvider) =
        fun message model ->
            match message with
            | DoNothing ->
                model, []
            | InitPage -> 
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
                Overview = model.Chat.Overview
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
            route
        
        let getRouteData() =
            match getCurrentRoute() with
            | Some(SetPage(page & Chat(chat, section))) ->
                let chatName = Option.defaultValue Dotnetruchat (ChatName.FromString(chat))
                let sectionName = Option.defaultValue Overview (SectionName.FromString(section))
                page, chatName, sectionName
            | _ ->
                (Chat(Dotnetruchat.UrlName, Overview.UrlName)), Dotnetruchat, Overview

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
                        UserData = SeriesChartComponentModel.Default
                    }
                }
            }
                    
        let createInitCommand() = 
            Cmd.batch [            
                Cmd.ofAsync SemanticUi.initJs () (fun _ -> DoNothing) (fun exn -> LogError exn)  
            ]

        override this.Program =
            let provider =
                { new IRemoteServiceProvider with
                    member __.GetService<'T when 'T :> IRemoteService> () = this.Remote<'T>() }

            Program.mkProgram (fun _ -> createInitModel(), createInitCommand()) (update provider) view
            |> Program.withRouter router