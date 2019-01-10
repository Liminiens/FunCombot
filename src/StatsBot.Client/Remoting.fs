namespace StatsBot.Client.Remoting

open Bolero.Remoting
    
module Chat =
    open System
    open StatsBot.Client.Types

    type ChatData = {
        Description: string
        TotalUsers: int32
        ActiveInThreeDays: int32
        ChangeInTotalUsersForWeek: int32
    }
    
    type UserCount = { Date: DateTime; Count: int }

    type UserCountInfo = { Chat: Chat; From: DateTime; To: DateTime; Unit: DateUnit }
    
    type ChatDataService =        
        { GetChatData: Chat -> Async<ChatData>
          GetUserCount: UserCountInfo -> Async<list<UserCount>> }

        interface IRemoteService with
            member __.BasePath = "/chat-data"   