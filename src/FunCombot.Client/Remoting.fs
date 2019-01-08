namespace FunCombot.Client.Remoting

open Bolero.Remoting
    
module Chat =
    open System
    open FunCombot.Client.Types

    type ChatData = {
        Description: string
        TotalUsers: int32
        ActiveInThreeDays: int32
        ChangeInTotalUsersForWeek: int32
    }
    
    type UserCount = { Date: DateTime; Count: int }
    
    type UserCountParameter = { Chat: Chat; From: DateTime; To: DateTime; Unit: GraphUnit }
    
    type ChatDataService =        
        { GetChatData: Chat -> Async<ChatData>
          GetUserCount: UserCountParameter -> Async<list<UserCount>> }

        interface IRemoteService with
            member __.BasePath = "/chat-data"   