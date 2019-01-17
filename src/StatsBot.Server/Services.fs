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
            GetUserChartSettings = fun name -> 
                async {
                    return {
                        DateMin = DateTime.Now
                        DateMax = DateTime.Now.AddYears(15)
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
            
            GetChatUsers = fun (chat, page) ->
                async {
                    let data = 
                        [
                            let mutable i = 1
                            while i <= page.PageSize do
                               yield { LastName = sprintf "lastname%i" i
                                       FirstName = sprintf "firstname%i" i
                                       Username = sprintf "nickname%i" i
                                       ImageUrl = sprintf "url%i" i
                                       MessageCount = random.Next(0, 1000)
                                       StickersCount = random.Next(0, 1000)
                                       MediaCount = random.Next(0, 1000) }
                               i <- i + 1
                        ]
                    return data, { page with Total = page.Total + page.PageSize}
                }
        }