namespace StatsBot.Client.Components.Charting

open System
open Elmish
open Bolero
open Bolero.Html
open StatsBot.Client
open StatsBot.Client.Types
open StatsBot.Client.Javascript
open StatsBot.Client.Javascript.Charting 

module SeriesChartComponent =  
   open StatsBot.Client.Components

   type TimeseriesChartTemplate = Template<"""frontend/templates/timeseries_chart.html""">
          
   let dateToString (date: DateTime) = 
       date.ToString("yyyy-MM-dd")

   let stringToDate (str: string) =
       DateTime.ParseExact(str, "yyyy-MM-dd", null)
   
   type TimeseriesData = list<(DateTime * int)>
   
   type SeriesChartComponentModel =
       { ElementId: string
         FromDateMin: DateTime
         FromDateMax: DateTime
         FromDateValue: DateTime
         DebouncedFromDateValue: Debounce.Model<DateTime>
         DebouncedToDateValue: Debounce.Model<DateTime>
         ToDateMin: DateTime
         ToDateMax: DateTime
         ToDateValue: DateTime
         Unit: DateUnit }
     
   type SeriesChartComponentMessage =
       | DoNothing
       | LogError of exn
       | SearchDateTo of DateTime
       | DebounceToDate of Debounce.Msg<DateTime>
       | SearchDateFrom of DateTime
       | DebounceFromDate of Debounce.Msg<DateTime>
       | SetDateFrom of DateTime
       | SetDateTo of DateTime
       | SetUnit of DateUnit
       | DrawChart of TimeseriesData

   let drawChart elementId (data: TimeseriesData) =
        let (dates, values) =
            data
            |> List.fold (fun acc (date, value) -> [dateToString date :> obj, value :> obj] @ acc) []
            |> List.unzip
        
        let configuration = {
            x = "x"
            columns = [
                { name = "x"; data = dates };
                { name = "users"; data = values };
            ]
            axis = {
                   x = {
                       ``type`` = "timeseries"
                       tick = {
                            format = "%Y-%m-%d"
                       }
                   }
           }
        }
            
        Charting.createChart elementId configuration

   let update message model =
       match message with
       | DoNothing ->
           model, []
       | LogError exn ->
           eprintfn "%O" exn
           model, []
       | SearchDateTo date ->
           model, Debounce.inputCmd date DebounceToDate
       | DebounceToDate msg ->
           Debounce.updateWithCmd
               msg
               DebounceToDate
               model.DebouncedToDateValue
               (fun x -> { model with DebouncedToDateValue = x })
               (SetDateTo >> Cmd.ofMsg)
       | SearchDateFrom date ->
           model, Debounce.inputCmd date DebounceFromDate
       | DebounceFromDate msg ->
           Debounce.updateWithCmd
               msg
               DebounceFromDate
               model.DebouncedFromDateValue
               (fun x -> { model with DebouncedFromDateValue = x })
               (SetDateFrom >> Cmd.ofMsg)
       | SetDateFrom fromDate ->
           { model with
                       FromDateValue = fromDate
                       ToDateMin = fromDate
                       ToDateValue =
                           if fromDate > model.ToDateValue then
                               fromDate.AddMonths(1)
                           else model.ToDateValue
           }, []                         
       | SetDateTo date ->
           { model with ToDateValue = date }, []
       | SetUnit unitValue ->
           { model with Unit = unitValue }, []
       | DrawChart data ->        
           let command =
               let loadDataForModel = drawChart model.ElementId
               Cmd.ofAsync
                   loadDataForModel data
                   (fun _ -> DoNothing)
                   (fun e -> LogError e)
           model, command
           
   type SeriesChartComponent() =
        inherit ElmishComponent<SeriesChartComponentModel, SeriesChartComponentMessage>()
        
        let template = TimeseriesChartTemplate()
                  
        let createDateInput labelText value min max dispatch = 
            label[] [
                concat [
                    text labelText
                    input ["type" => "date";
                           attr.pattern "[0-9]{4}-[0-9]{2}-[0-9]{2}"
                           attr.value <| dateToString value;
                           attr.min <| dateToString min;
                           attr.max <| dateToString max;
                           on.input ^ fun ev ->
                               let value = ev.Value :?> string
                               let valid = not <| isNullOrWhiteSpace value
                               if valid then dispatch value ]
                ]       
            ]
        
        override this.View model dispatch =
            let graph =
                let classes = [yield "ui basic segment chart"]
                div [attr.id model.ElementId; attr.classes classes] []
            
            let units =
                forEach getUnionCases<DateUnit> ^fun (case, _, tag) ->
                    option [yield attr.value case.Name;
                            if model.Unit = case then yield attr.selected true] [
                        text case.Name
                    ]
            
            let fromInput =
                createDateInput "From:" model.FromDateValue model.FromDateMin model.FromDateMax ^fun value -> 
                    dispatch (SearchDateFrom(stringToDate value))
            
            let toInput =
                createDateInput "To:" model.ToDateValue model.ToDateMin model.ToDateMax ^fun value -> 
                    dispatch (SearchDateTo(stringToDate value))
            
            let parseUnitOrDefault unit =
                unit
                |> DateUnit.FromString
                |> Option.defaultValue Day

            template
                .FromInput(fromInput)
                .ToInput(toInput)
                .Units(units)
                .SelectedUnit(model.Unit.Name, fun unit -> dispatch (SetUnit(parseUnitOrDefault unit)))
                .Graph(graph)
                .Elt()
        
module UserDataComponent =
    open StatsBot.Client.Components
    open StatsBot.Client.Remoting.Chat
    open SeriesChartComponent
    
    let getDefaultModel = 
        let now = DateTime.Now
        let dateFrom = new DateTime(now.Year, now.Month, 1)
        let dateTo = new DateTime(now.Year, now.Month + 1, 1)
           
        { ElementId = Guid.NewGuid().ToString()
          DebouncedFromDateValue = Debounce.init (TimeSpan.FromMilliseconds 225.) dateFrom
          DebouncedToDateValue = Debounce.init (TimeSpan.FromMilliseconds 225.) dateTo
          FromDateMin = dateFrom
          FromDateMax = stringToDate "2030-01-01" 
          FromDateValue = dateFrom 
          ToDateMin = dateTo
          ToDateMax = stringToDate "2030-01-01" 
          ToDateValue = dateTo
          Unit = Day }
    
    type UserDataComponentModel = {
        Series: SeriesChartComponentModel
        Chat: Chat
    }
    
    type UserDataComponentMessage =
        | LogError of exn
        | SetUserChartChat of Chat
        | SetUserChartSettings of UserChartSettings
        | LoadUserChartSettings
        | LoadUserChartData
        | SeriesChartComponentMessage of SeriesChartComponentMessage
     
    let update (provider: IRemoteServiceProvider)=       
        let chatDataService = provider.GetService<ChatDataService>()
        fun message model ->
            match message with
            | LogError exn ->
                eprintfn "%O" exn
                model, []
            | SetUserChartSettings settings ->
                let maxMinDate = settings.DateMin.AddMonths(1)
                { model with Series = {
                              model.Series with 
                                  FromDateMin = settings.DateMin
                                  FromDateMax = settings.DateMax
                                  FromDateValue = settings.DateMin
                                  ToDateMin = maxMinDate
                                  ToDateMax = settings.DateMax
                                  ToDateValue = maxMinDate 
                 } }, Cmd.ofMsg LoadUserChartData 
            | SetUserChartChat chat ->
                { model with Chat = chat }, []
            | LoadUserChartSettings ->
                model, Cmd.ofAsync 
                    chatDataService.GetUserChartSettings model.Chat 
                    (fun data -> SetUserChartSettings data)
                    (fun exn -> LogError exn)
            | LoadUserChartData ->
                model, Cmd.ofAsync 
                    chatDataService.GetUserCount {
                        Chat = model.Chat 
                        From = model.Series.FromDateValue
                        To = model.Series.ToDateValue
                        Unit = model.Series.Unit
                    }
                    (fun data ->
                        data
                        |> List.map ^ fun item -> item.Date, item.Count
                        |> (DrawChart >> SeriesChartComponentMessage))
                    (fun exn -> LogError exn)
            | SeriesChartComponentMessage seriesMessage ->
               let loadCommand =
                   match seriesMessage with
                   | SetDateFrom _             
                   | SetDateTo _
                   | SetUnit _ ->
                       Cmd.ofMsg LoadUserChartData
                   | _ -> []
               let (newModel, commands) = SeriesChartComponent.update seriesMessage model.Series
               {model with Series = newModel}, Cmd.batch [
                   Cmd.wrapAndBatchSub SeriesChartComponentMessage commands
                   loadCommand
               ]   
    
    type UserDataComponent() =
        inherit ElmishComponent<UserDataComponentModel, UserDataComponentMessage>()
        
        override this.View model dispatch =
            ecomp<SeriesChartComponent,_,_> model.Series ^fun message -> 
                dispatch (SeriesChartComponentMessage message)
            