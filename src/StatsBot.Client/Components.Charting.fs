namespace StatsBot.Client.Components.Charting

open System
open Elmish
open Bolero
open Bolero.Html
open StatsBot.Client
open StatsBot.Client.Types
open StatsBot.Client.Javascript
open StatsBot.Client.Javascript.Charting 

[<RequireQualifiedAccess>]
module Identificators =
   let usersChartId = Guid.NewGuid().ToString()
       
module SeriesChartComponent =  
   open Remoting.Chat

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
         Unit: DateUnit }
      
   type SeriesChartComponentModelContainer<'T> = {
       SeriesData: SeriesChartComponentModel
       ChartContainer: 'T
   }
     
   type SeriesChartComponentMessage =
       | DoNothing
       | LogError of exn
       | SetDateFrom of DateTime
       | SetDateTo of DateTime
       | SetUnit of DateUnit
       | UnloadData
       | LoadData of TimeseriesData
   
   let unloadData id = 
        Charting.unloadData id ["x"; "users"]

   let loadData id (data: TimeseriesData) =
        let (dates, values) =
            data
            |> List.fold (fun acc (date, value) -> [dateToString date :> obj, value :> obj] @ acc) []
            |> List.unzip
        Charting.loadData id [
            { name = "x"; data = dates };
            { name = "users"; data = values };
        ];

   let update message model =
       match message with
       | DoNothing ->
           model, []
       | LogError exn ->
           eprintfn "%O" exn
           model, []
       | SetDateFrom fromDate ->
           { model with SeriesData = {
                       model.SeriesData with
                           FromDateValue = fromDate
                           ToDateMin = fromDate
                           ToDateValue =
                               if fromDate > model.SeriesData.ToDateValue then
                                   fromDate.AddMonths(1)
                               else model.SeriesData.ToDateValue }
           }, []                         
       | SetDateTo date ->
           { model with SeriesData = {
                       model.SeriesData with ToDateValue = date
           } }, []
       | SetUnit unitValue ->
           { model with SeriesData = {
                       model.SeriesData with Unit = unitValue
           } }, []
       | LoadData data ->        
           let command =
               let loadDataForModel = loadData model.SeriesData.Id
               Cmd.ofAsync
                   loadDataForModel data
                   (fun _ -> DoNothing)
                   (fun e -> LogError e)
           { model with SeriesData = {
                       model.SeriesData with IsLoaded = true
           } }, command
       | UnloadData ->
           let command =
               Cmd.ofAsync
                   unloadData model.SeriesData.Id
                   (fun _ -> DoNothing)
                   (fun e -> LogError e)
           { model with SeriesData = {
                       model.SeriesData with IsLoaded = false
           } }, command
           
   type SeriesChartComponent<'T>(id: string, elementId: string) =
        inherit ElmishComponent<SeriesChartComponentModelContainer<'T>, SeriesChartComponentMessage>()
        
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
                let classes = [yield "ui basic segment"; yield "chart"; if not model.SeriesData.IsLoaded then yield "loading"]
                div [attr.id elementId; attr.classes classes] [
                ]
            
            let units =
                forEach getUnionCases<DateUnit> ^fun (case, _, tag) ->
                    option [yield attr.value case.Name;
                            if model.SeriesData.Unit = case then yield attr.selected true] [
                        text case.Name
                    ]
            
            let fromInput =
                createDateInput "From:" model.SeriesData.FromDateValue model.SeriesData.FromDateMin model.SeriesData.FromDateMax ^fun value -> 
                    dispatch (SetDateFrom(stringToDate value))
            
            let toInput =
                createDateInput "To:" model.SeriesData.ToDateValue model.SeriesData.ToDateMin model.SeriesData.ToDateMax ^fun value -> 
                    dispatch (SetDateTo(stringToDate value))
            
            let parseUnitOrDefault unit =
                unit
                |> DateUnit.FromString
                |> Option.defaultValue Day

            template
                .FromInput(fromInput)
                .ToInput(toInput)
                .Units(units)
                .SelectedUnit(model.SeriesData.Unit.Name, fun unit -> dispatch (SetUnit(parseUnitOrDefault unit)))
                .Graph(graph)
                .Elt()
        
        override this.Finalize() =
            Charting.destroyChart id
        
module UserDataComponent =
    open StatsBot.Client.Components
    open StatsBot.Client.Remoting.Chat
    open SeriesChartComponent
    
    type UserDataComponentModel = { Chat: Chat }
    
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
        | SetUserChartChat of Chat 
        | LoadChartDataFromService       
        | SeriesChartComponentMessage of SeriesChartComponentMessage
    
    let update (provider: IRemoteServiceProvider)=       
        let chatDataService = provider.GetService<ChatDataService>()
        let getUserCountCached =
           (chatDataService.GetUserCount, TimeSpan.FromMinutes(5.))
           ||> ClientSideCache.getOrCreateAsyncFn 
        fun message model ->
            match message with
            | LogError exn ->
                eprintfn "%O" exn
                model, []
            | SetUserChartChat chat ->
                { model with ChartContainer = {
                            model.ChartContainer with Chat = chat
                } }, []
            | LoadChartDataFromService ->
                model,
                Cmd.batch [
                    Cmd.ofMsg (SeriesChartComponentMessage UnloadData)
                    Cmd.ofAsync 
                        getUserCountCached {
                            Chat = model.ChartContainer.Chat 
                            From = model.SeriesData.FromDateValue
                            To = model.SeriesData.ToDateValue
                            Unit = model.SeriesData.Unit
                        }
                        (fun data ->
                            data
                            |> List.map ^ fun item -> item.Date, item.Count
                            |> (LoadData >> SeriesChartComponentMessage))
                        (fun exn -> LogError exn)
                ]
            | SeriesChartComponentMessage seriesMessage ->
               let loadCommand =
                   match seriesMessage with
                   | SetDateFrom _             
                   | SetDateTo _
                   | SetUnit _ -> 
                        Cmd.ofMsg LoadChartDataFromService
                   | _ -> []
               let (newModel, commands) = SeriesChartComponent.update seriesMessage model
               newModel, Cmd.batch [
                   Cmd.convertSubs SeriesChartComponentMessage commands
                   loadCommand
               ]   
    
    type UserDataComponent() =
        inherit SeriesChartComponent<UserDataComponentModel>(Identificators.usersChartId, "user_data")