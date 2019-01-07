namespace FunCombot.Server.Services

module Chat =
    open System
    open Microsoft.Extensions.Logging
    open Bolero.Remoting
    open FunCombot.Client.Remoting.Chat

    type ChatDataServiceHandler(logger: ILogger<ChatDataServiceHandler>) =
        inherit RemoteHandler<ChatDataService>()
        let random = new Random()

        override __.Handler = {
            GetChatData = fun name -> 
                async {
                    logger.LogInformation("Called data service")
                    return {
                        Description = name.DisplayName
                        TotalUsers = random.Next(10000)
                        ActiveInThreeDays = random.Next(10000)
                        ChangeInTotalUsersForWeek = random.Next(-1000, 1000)
                    }
                }
        }