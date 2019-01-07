namespace FunCombot.Client.Remoting

open Bolero.Remoting
    
module Chat =
    open FunCombot.Client.Types

    type ChatData = {
        Description: string
        TotalUsers: int32
        ActiveInThreeDays: int32
        ChangeInTotalUsersForWeek: int32
    }
    
    type ChatDataService =        
        { GetChatData: ChatName -> Async<ChatData> }

        interface IRemoteService with
            member __.BasePath = "/chat-data"