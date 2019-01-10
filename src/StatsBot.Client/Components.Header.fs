namespace StatsBot.Client.Components.Header

open Bolero
open Bolero.Html
open StatsBot.Client
open StatsBot.Client.Types

module HeaderComponent =   
    type HeaderTemplate = Template<"""frontend/templates/header.html""">
        
    type HeaderComponentMessage =
        | ChangeChat of Chat
    
    type HeaderComponentModel = {
        Chat: Chat
    }
    
    let headerTemplate = HeaderTemplate()
    
    let update message model =
        match message with
        | ChangeChat chat ->
            { model with Chat = chat }
            
    type HeaderComponent() =
        inherit ElmishComponent<HeaderComponentModel, HeaderComponentMessage>()
        
        override this.View model dispatch =
            let dropDown =
                forEach getUnionCases<Chat> ^fun (case, name, _) ->
                    a [ attr.classes [yield "item"; if model.Chat = case then yield "active selected"];
                        on.click ^fun ev ->
                            if model.Chat <> case then dispatch (ChangeChat case)] [
                        text case.DisplayName
                    ]
            headerTemplate
                .HeaderItem(text "StatsBot")
                .DropdownItems(dropDown)
                .ChatName(text model.Chat.DisplayName)
                .Elt()
