namespace StatsBot.Server.Services

module Chat =
    open System
    open Microsoft.Extensions.Logging
    open Bolero.Remoting
    open StatsBot.Client.Types
    open StatsBot.Client.Remoting.Chat

    type ChatDataServiceHandler(logger: ILogger<ChatDataServiceHandler>) =
        inherit RemoteHandler<ChatDataService>()
        let random = new Random()

        override __.Handler = {
            GetChatData = fun name -> 
                async {
                    return {
                        Description = name.DisplayName
                        TotalUsers = random.Next(10000)
                        ActiveInThreeDays = random.Next(10000)
                        ChangeInTotalUsersForWeek = random.Next(-1000, 1000)
                    }
                }
            GetUserCount = fun data ->
                async {
                    let data =
                        [
                            let mutable startDate = data.From
                            while startDate < data.To do
                                yield { Date = startDate; Count = random.Next(10, 1000) }
                                startDate <- 
                                    match data.Unit with
                                    | WeekUnit -> startDate.AddDays(7.)
                                    | DayUnit -> startDate.AddDays(1.)
                                    | MonthUnit -> startDate.AddMonths(1)
                        ]
                    return data
                }
        }