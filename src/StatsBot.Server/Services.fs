namespace StatsBot.Server.Services

module Chat =
    open System
    open Microsoft.Extensions.Logging
    open Bolero.Remoting
    open StatsBot.Client.Remoting.Chat

    type ChatDataServiceHandler(logger: ILogger<ChatDataServiceHandler>) =
        inherit RemoteHandler<ChatDataService>()
        let random = new Random()

        override __.Handler = {
            GetChatData = fun name -> 
                async {
                    logger.LogInformation("GetChatData called")
                    return {
                        Description = name.DisplayName
                        TotalUsers = random.Next(10000)
                        ActiveInThreeDays = random.Next(10000)
                        ChangeInTotalUsersForWeek = random.Next(-1000, 1000)
                    }
                }
            GetUserCount = fun data ->
                async {
                    logger.LogInformation("GetUserCount called")
                    let data =
                        [
                            let mutable startDate = data.From
                            while startDate < data.To do
                                yield { Date = startDate; Count = random.Next(10, 1000) }
                                startDate <- 
                                    match data.Unit with
                                    | "week" -> startDate.AddDays(7.)
                                    | "day" -> startDate.AddDays(1.)
                                    | "month" -> startDate.AddMonths(1)
                                    | _ -> startDate.AddDays(1.)
                        ]
                    return data
                }
        }